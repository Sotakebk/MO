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
        root.FollowingActions = CollectPossibleNextActions(CleanPartialSolution);
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
                    FollowingActions = CollectPossibleNextActions(state),
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
            GeneralPeopleHeuristic.CalculateScore(solution); // TODO: for people score metrics
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
        var s = new Solution
        {
            Score = partialSolution.Score,
            PeopleMetrics = new Dictionary<int, float[]>(partialSolution.PeopleMetrics),
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

    private void CalculateSumAndAddIfPassesRules(List<AvailableAction> list, AvailableAction action,
        PartialSolution solution)
    {
        if (!PassesAllRules(action, ref solution))
            return;

        var copy = solution.CreateDeepCopy();
        action.Apply(ref copy);
        if (CheckIfFinishedSolution(ref copy))
        {
        }
        action.Score = GetScoreForSolution(ref copy);
        lock (list) // bad idea, shrug
        {
            list.Add(action);
        }
    }

    private AvailableAction[] CollectPossibleNextActions(PartialSolution solution)
    {
        var list = new List<AvailableAction>();

        var assignments = SolutionWalkingHelper.EnumerableAssignments(solution)
            .Where(pair => !pair.assignment.HasValuesSet());

        Parallel.ForEach(assignments, (value, state, index) =>
        {
            var (assignmentIndex, assignment) = value;
            if (assignment.HasValuesSet())
                return;

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

                    CalculateSumAndAddIfPassesRules(list, action, solution);
                }
            }
        });

        // make sure it's ordered right, and each run is the same
        var value = list.OrderByDescending(a => a.Score)
            .ThenBy(a => a.ChairPersonId)
            .ThenBy(a => a.ReviewerId)
            .ThenBy(a => a.SupervisorId)
            .ThenBy(a => a.AssignmentId.Index)
            .ToArray();

        if (value.Length == 0)
            _state.DeadEnds++;

        return value;
    }
}

internal class Level
{
    public int Depth;
    public PartialSolution CurrentPartialSolution;
    public AvailableAction[] FollowingActions = Array.Empty<AvailableAction>();
    public int LastPickedFollowingState = -1;
}