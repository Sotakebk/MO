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
    private readonly PartialSolution CleanPartialSolution;
    private readonly int SearchDepth = 0; // TODO be more sensible

    public Optimizer(ILoggerFactory loggerFactory, Input input, OptimizationState state)
    {
        _logger = loggerFactory.CreateLogger(GetType().Name);
        _input = input;
        _state = state;
        CleanPartialSolution = new PartialSolution(input);
        var expectedDepth = _input.Combinations.Sum(c => c.TotalCount);
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
        root.FollowingActions = GetFollowingActions(CleanPartialSolution);
        root.CurrentPartialSolution = CleanPartialSolution;

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

    private static float GetScoreForSolution(ref PartialSolution solution)
    {
        return GeneralPeopleHeuristic.CalculateScore(solution);
    }

    private static bool PassesAllRules(AvailableAction action, ref PartialSolution solution)
    {
        return SingleAssignmentRule.PassesRule(action, solution);
    }

    private static Solution CreateSolutionFromPartialSolution(PartialSolution partialSolution)
    {
        var s = new Solution()
        {
            Score = partialSolution.Score
        };

        s.Days = new SolutionDay[partialSolution.Days.Length];

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
                vbs[j].Assignments = new SolutionAssignment?[partialSolutionClassroom.Assignments.Length];

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
        var depth = solution.CurrentDepth - 30;

        Parallel.For(0, actions.Length, i =>
        {
        //for(int i = 0; i < actions.Length; i++
            actions[i].Score = DeepSearchForMaxScore(actions[i], ref solution, depth) ?? float.NegativeInfinity;
        });

        var value = actions.Where(a => !float.IsNegativeInfinity(a.Score))
            .OrderByDescending(a => a.Score)
            .ThenBy(a => a.ChairPersonId)
            .ThenBy(a => a.ReviewerId)
            .ThenBy(a => a.SupervisorId)
            .ThenBy(a => a.AssignmentId.Index)
            .ToArray();

        return value;
    }

    /// <summary>
    /// Collects actions that pass rules, but doesn't calculate score.
    /// </summary>
    private AvailableAction[] CollectAvailableActions(PartialSolution solution)
    {
        var assignments = SolutionWalkingHelper.EnumerableAssignments(solution)
            .Where(pair => !pair.assignment.HasValuesSet());

        var actions = new List<AvailableAction>();

        foreach (var (assignmentIndex, assignment) in assignments)
        {
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

    /// <summary>
    /// Collects actions that pass rules, but doesn't calculate score.
    /// </summary>
    private AvailableAction[] CollectAvailableActionsMultithreaded(PartialSolution solution)
    {
        var assignments = SolutionWalkingHelper.EnumerableAssignments(solution)
            .Where(pair => !pair.assignment.HasValuesSet());

        var actions = new List<AvailableAction>();

        Parallel.ForEach(assignments, (value, state, index) =>
        {
            var (assignmentIndex, _) = value;

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

        var value = actions.OrderByDescending(a => a.Score)
            .ThenBy(a => a.ChairPersonId)
            .ThenBy(a => a.ReviewerId)
            .ThenBy(a => a.SupervisorId)
            .ThenBy(a => a.AssignmentId.Index)
            .ToArray();

        return value;
    }

    private float? DeepSearchForMaxScore(AvailableAction action, ref PartialSolution solution, int depth)
    {
        var copy = solution.CreateDeepCopy();
        action.Apply(ref copy);
        return DeepSearchForMaxScore(ref copy, depth);
    }

    private float? DeepSearchForMaxScore(ref PartialSolution solution, int depth)
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

        float? score = null;
        void SaveIfGreater(float? value)
        {
            if (value != null)
                score = MathF.Max(value.Value, score ?? float.MinValue);
        }

        // check max value of all available actions
        foreach (var action in actions)
        {
            SaveIfGreater(DeepSearchForMaxScore(action, ref solution, depth - 1));
        }

        return score;
    }
}

internal class Level
{
    public int Depth;
    public PartialSolution CurrentPartialSolution;
    public AvailableAction[] FollowingActions = Array.Empty<AvailableAction>();
    public int LastPickedFollowingState = -1;
}