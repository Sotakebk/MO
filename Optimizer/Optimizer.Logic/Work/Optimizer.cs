using Microsoft.Extensions.Logging;
using Optimizer.Logic.Work.Heuristics;
using Optimizer.Logic.Work.Rules;

namespace Optimizer.Logic.Work;

internal sealed class Optimizer
{
    private readonly ILogger _logger;
    private readonly Input _input;
    private readonly OptimizationState _state;
    private readonly Stack<Level> _levels;
    private readonly PartialSolution _cleanPartialSolution;
    private int _scoreEvaluationCounter = 0;

    public Optimizer(ILoggerFactory loggerFactory, Input input, OptimizationState state)
    {
        _logger = loggerFactory.CreateLogger(GetType().Name);
        _input = input;
        _state = state;
        _cleanPartialSolution = new PartialSolution(input);
        var expectedDepth = _input.Combinations.Sum(c => c.TotalCount);
        _state.MaxDepth = expectedDepth;
        _levels = new Stack<Level>(expectedDepth);
    }

    private bool StopIfCancelled()
    {
        if (!_state.CancellationToken.IsCancellationRequested)
            return false;

        _state.IsWorking = false;
        return true;
    }

    public void Optimize()
    {
        _state.IsWorking = true;

        var root = new Level();
        root.Depth = 0;
        root.LastPickedFollowingState = -1;
        _state.CurrentDepth = 0;
        root.FollowingActions = GetFollowingActions(_cleanPartialSolution);
        root.CurrentPartialSolution = _cleanPartialSolution;

        _levels.Push(root);

        while (!StopIfCancelled())
        {
            _state.OperationsDone++;
            if (_levels.Count <= 0)
            {
                break;
            }

            var level = _levels.Peek();
            _state.CurrentDepth = level.Depth;
            _state.PartialScore = level.CurrentPartialSolution.Score;
            double multiplier = 1;
            double sum = 0;
            foreach (var l in _levels.Reverse())
            {
                sum += (Math.Max(0, l.LastPickedFollowingState) / (double)l.FollowingActions.Length) * multiplier;
                multiplier /= l.FollowingActions.Length;
            }

            _state.PercentDomainSeen = (float)(sum * 100);

            level.LastPickedFollowingState++;
            if (level.LastPickedFollowingState < level.FollowingActions.Length)
            {
                // let's go deeper
                var action = level.FollowingActions[level.LastPickedFollowingState];
                var state = level.CurrentPartialSolution.CreateDeepCopy();
                action.Apply(ref state);

                if (CheckIfFinishedSolution(ref state))
                {
                    SaveIfBetter(ref state);
                    // don't collect actions, this schedule is complete
                    // try next action of this level or go back
                    continue;
                }

                var newLevel = new Level()
                {
                    FollowingActions = GetFollowingActions(state),
                    CurrentPartialSolution = state,
                    Depth = level.Depth + 1,
                    LastPickedFollowingState = -1
                };
                _levels.Push(newLevel);
            }
            else
            {
                // we have to go back
                _levels.Pop();
            }
        }

        _state.IsWorking = false;
    }

    private bool CheckIfFinishedSolution(ref PartialSolution solution)
    {
        if (solution.SupervisorAndReviewerIdToAssignmentsLeft.Any(pair => pair.Value > 0))
            return false;

        return true;
    }

    private void SaveIfBetter(ref PartialSolution solution)
    {
        if (solution.Score > (_state.Result?.Score ?? float.MinValue))
        {
            _state.Result = CreateSolutionFromPartialSolution(solution);
        }
    }

    private float GetScoreForSolution(ref PartialSolution solution)
    {
        Interlocked.Increment(ref _scoreEvaluationCounter);
        _state.Evaluations = _scoreEvaluationCounter;
        return GeneralPeopleHeuristic.CalculateScore(solution);
    }

    private static bool PassesAllRules(AvailableAction action, ref PartialSolution solution)
    {
        return SingleAssignmentRule.PassesRule(action, solution);
    }

    private static Solution CreateSolutionFromPartialSolution(PartialSolution partialSolution)
    {
#if DEBUG
        GeneralPeopleHeuristic.CalculateScore(partialSolution);
#endif
        var s = new Solution
        {
            Score = partialSolution.Score,
            Days = new SolutionDay[partialSolution.Days.Length]
        };

        for (var i = 0; i < s.Days.Length; i++)
        {
            var partialSolutionDay = partialSolution.Days[i];
            s.Days[i].DayId = partialSolutionDay.DayId;
            s.Days[i].Classrooms = new SolutionClassroom[partialSolutionDay.Classrooms.Length];
            var vbs = s.Days[i].Classrooms;

            for (var j = 0; j < partialSolutionDay.Classrooms.Length; j++)
            {
                var partialSolutionClassroom = partialSolutionDay.Classrooms[j];
                vbs[j].RoomId = partialSolutionClassroom.RoomId;
                vbs[j].Assignments = new SolutionAssignment[partialSolutionClassroom.Assignments.Length];

                var assignments = partialSolutionClassroom.Assignments;
                for (var k = 0; k < partialSolutionClassroom.Assignments.Length; k++)
                {
                    if (assignments[k].HasValuesSet())
                        vbs[j].Assignments[k] = new SolutionAssignment()
                        {
                            ChairPersonId = assignments[k].ChairPersonId,
                            ReviewerId = assignments[k].ReviewerId,
                            SupervisorId = assignments[k].SupervisorId,
                        };
                }
            }
        }

        return s;
    }

    private AvailableAction[] GetFollowingActions(PartialSolution solution)
    {
        var actions = CollectAvailableActionsMultithreaded(solution);
        var temp = solution.CurrentDepth / ((float)solution.MaxDepth);
        temp = temp * temp * temp * temp * temp * temp * temp;
        //var depth = Math.Min((int)(solution.MaxDepth * temp), solution.MaxDepth - solution.CurrentDepth);
        //var persistence = solution.CurrentDepth / (float)solution.MaxDepth;

        // x^5
        //persistence = persistence * persistence * persistence * persistence * persistence;
        //var proportion = (1 + solution.CurrentDepth) / (float)solution.MaxDepth;
        // x^9
        //proportion = proportion * proportion * proportion * proportion * proportion * proportion * proportion * proportion * proportion;

        /*
        var tempProportionForPrinting = proportion;
        _logger.LogDebug($"Evaluating {actions.Length} actions");
        for (int i = 0; i <= depth; i++)
        {
            _logger.LogDebug($"At child level {i} evaluating actions with {(100*tempProportionForPrinting):F4}% of children");
            tempProportionForPrinting *= persistence;
        }*/

        var depth = Math.Max(0, solution.CurrentDepth - solution.MaxDepth + 15);
        var proportion = 0.01f;
        var persistence = 1;

        var complete = 0;
        Parallel.For(0, actions.Length, i =>
        {
            //for(int i = 0; i < actions.Length; i++
            actions[i].Score = DeepSearchForMaxScore(actions[i], ref solution, depth, proportion, persistence) ?? float.NegativeInfinity;

            Interlocked.Increment(ref complete);
            _state.CurrentDepthCompleteness = complete / (float)actions.Length;
        });

        var value = actions.Where(a => !float.IsNegativeInfinity(a.Score))
            .OrderByDescending(a => a.Score)
            .ToArray();

        return value;
    }

    /// <summary>
    /// Collects actions that pass rules, but doesn't calculate score.
    /// </summary>
    private AvailableAction[] CollectAvailableActions(PartialSolution solution)
    {
        var assignments = SolutionWalkingHelper.EnumerableEmptyAssignments(solution).ToArray();

        var actions = new List<AvailableAction>();

        for (var index = 0; index < assignments.Length; index++)
        {
            var assignmentIndex = assignments[index];

            foreach (var keyValuePair in solution.SupervisorAndReviewerIdToAssignmentsLeft)
            {
                if (keyValuePair.Value <= 0)
                    continue;

                foreach (var chairPerson in _input.ChairPersonIds)
                {
                    if (chairPerson == keyValuePair.Key.reviewerId || chairPerson == keyValuePair.Key.supervisorId)
                        continue;

                    var action = new AvailableAction(
                        keyValuePair.Key.supervisorId,
                        keyValuePair.Key.reviewerId,
                        (byte)chairPerson,
                        assignmentIndex);

                    if (PassesAllRules(action, ref solution))
                        actions.Add(action);
                }
            }
        }

        return actions.ToArray();
    }

    private static void Shuffle<T>(T[] array)
    {
        var rng = new Random();
        var n = array.Length;
        while (n > 1)
        {
            var k = rng.Next(n);
            n--;
            (array[n], array[k]) = (array[k], array[n]);
        }
    }

    /// <summary>
    /// Collects actions that pass rules, but doesn't calculate score.
    /// </summary>
    private AvailableAction[] CollectAvailableActionsMultithreaded(PartialSolution solution)
    {
        var assignments = SolutionWalkingHelper.EnumerableEmptyAssignments(solution).ToArray();

        var actions = new List<AvailableAction>();

        Parallel.For(0, assignments.Length, index =>
        {
            var assignmentIndex = assignments[index];

            foreach (var keyValuePair in solution.SupervisorAndReviewerIdToAssignmentsLeft)
            {
                if (keyValuePair.Value <= 0)
                    continue;

                foreach (var chairPerson in _input.ChairPersonIds)
                {
                    if (chairPerson == keyValuePair.Key.reviewerId || chairPerson == keyValuePair.Key.supervisorId)
                        continue;

                    var action = new AvailableAction(
                        keyValuePair.Key.supervisorId,
                        keyValuePair.Key.reviewerId,
                        (byte)chairPerson,
                        assignmentIndex);

                    if (PassesAllRules(action, ref solution))
                        lock (actions)
                            actions.Add(action);
                }
            }
        });

        var value = actions.ToArray();

        return value;
    }

    private float? DeepSearchForMaxScore(AvailableAction action, ref PartialSolution solution, int depth, float proportion, float persistence)
    {
        proportion = MathF.Max(0, MathF.Min(proportion, 1));
        var copy = solution.CreateDeepCopy();
        action.Apply(ref copy);
        return DeepSearchForMaxScore(ref copy, depth, proportion, persistence);
    }

    private float? DeepSearchForMaxScore(ref PartialSolution solution, int depth, float proportion, float persistence)
    {
        if (solution.CurrentDepth == solution.MaxDepth)
        {
            // solution is complete, return a score
            return GetScoreForSolution(ref solution);
        }

        if (depth <= 0)
        {
            // solution is incomplete but not a dead-end
            // return a score
            return GetScoreForSolution(ref solution);
        }

        var actions = CollectAvailableActions(solution);

        if (actions.Length == 0)
        {
            // no valid moves, we're at a dead-end
            return null;
        }

        IEnumerable<AvailableAction> actionsToEvaluate;
        if (proportion >= 1)
        {
            actionsToEvaluate = actions;
        }
        else
        {
            var actionsToEvaluateCount = Math.Max(1, (int)(proportion * actions.Length));

            if (actionsToEvaluateCount == 1)
            {
                actionsToEvaluate = new[] { actions[new Random().Next(actions.Length)] };
            }
            else
            {
                Shuffle(actions);
                actionsToEvaluate = actions.Take(actionsToEvaluateCount);
            }
        }

        float newProportion = 0;
        if (persistence > 1)
        {
            // increasingly more actions
            newProportion = 1f - (1 - newProportion) * (persistence - 1);
        }
        else
        {
            // calculate less values
            newProportion = proportion * persistence;
        }

        var validScoreCount = 0;
        var accumulator = 0f;
        var maxValue = float.MinValue;
        // check max value of all available actions
        foreach (var action in actionsToEvaluate)
        {
            var score = DeepSearchForMaxScore(action, ref solution, depth - 1, newProportion, persistence);

            if (score != null)
            {
                validScoreCount++;
                accumulator += score.Value;
                maxValue = MathF.Max(maxValue, score.Value);
            }
        }

        if (validScoreCount == 0)
            return null;

        // how many results were calculated out of the available and non-dead-end ones
        proportion = (validScoreCount / (float)actions.Length);

        // weighted average between average of averages and current state score with maxValue from evaluated actions
        return (1 - proportion) * ((accumulator / (float)validScoreCount) + GetScoreForSolution(ref solution)) / 2f + proportion * maxValue;
    }
}

internal class Level
{
    public int Depth;
    public PartialSolution CurrentPartialSolution;
    public AvailableAction[] FollowingActions = Array.Empty<AvailableAction>();
    public int LastPickedFollowingState = -1;
}