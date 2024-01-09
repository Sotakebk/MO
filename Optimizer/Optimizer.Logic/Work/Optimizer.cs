using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Optimizer.Logic.Work.Score.Heuristics;
using Optimizer.Logic.Work.Score.Rules;

namespace Optimizer.Logic.Work;

internal class Optimizer
{
    private readonly ILogger<Optimizer> _logger;
    private readonly Input _input;
    private readonly OptimizationState _state;
    private readonly Stack<Level> _levels;
    private readonly IRule[] _rules;
    private readonly IHeuristic[] _heuristics;
    public PartialSolution CleanPartialSolution;

    public Optimizer(ILoggerFactory loggerFactory, Input input, OptimizationState state, IRule[] rules, IHeuristic[] heuristics)
    {
        _logger = loggerFactory.CreateLogger<Optimizer>();
        _input = input;
        _state = state;
        CleanPartialSolution = new PartialSolution(input);
        _rules = rules;
        _heuristics = heuristics;
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
        root.FollowingStates = CollectPossibleNextStates(CleanPartialSolution);

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
            if (level.LastPickedFollowingState < level.FollowingStates.Length)
            {
                // let's go deeper
                var newState = level.FollowingStates[level.LastPickedFollowingState];

                if (CheckIfFinishedSolutionAndSaveIfBetter(newState))
                {
                    // don't collect actions, this schedule is complete
                    // try next action of this level or go back
                    continue;
                }

                var newLevel = new Level()
                {
                    FollowingStates = CollectPossibleNextStates(newState),
                    CurrentPartialSolution = newState,
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
#if DEBUG
            // _logger.LogTrace(string.Join("->", _levels.Select(l=>$"({l.LastPickedFollowingState}/{l.FollowingStates.Length})")));
#endif
        }

        _state.IsWorking = false;
    }

    private bool CheckIfFinishedSolutionAndSaveIfBetter(PartialSolution solution)
    {
        if (solution.SupervisorAndReviewerIdToAssignmentsLeft.Any(pair => pair.Value > 0))
            return false;

        float score = 0;
        foreach (var heuristic in _heuristics)
        {
            score += heuristic.CalculateScore(solution);
        }

        if (score > (_state.Result?.Score ?? float.MinValue))
        {
            var s = new Solution()
            {
                Score = score
            };

            s.Days = new SolutionDay[solution.Days.Length];

            for (var i = 0; i < s.Days.Length; i++)
            {
                var d = solution.Days[i];
                s.Days[i].DayId = d.DayId;
                s.Days[i].Classrooms = new SolutionClassroom[d.Classrooms.Length];
                var vbs = s.Days[i].Classrooms;

                for (var j = 0; j < d.Classrooms.Length; j++)
                {
                    var b = d.Classrooms[j];
                    vbs[j].RoomId = b.RoomId;
                    vbs[j].Assignments = new SolutionAssignment?[b.Assignments.Length];

                    var assignments = b.Assignments;
                    for (var k = 0; k < b.Assignments.Length; k++)
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

            _state.Result = s;
        }

        return true;
    }

    private PartialSolution[] CollectPossibleNextStates(PartialSolution solution)
    {
        var bag = new ConcurrentBag<PartialSolution>();

        void CalculateSumAndAddIfPassesRules(ConcurrentBag<PartialSolution> _bag, AvailableAction action)
        {
            var passes = true;
            foreach (var rule in _rules)
            {
                if (!rule.PassesRule(action, solution))
                {
                    passes = false;
                    break;
                }
            }

            if (passes)
            {
                var copy = solution.CreateDeepCopy();
                action.Apply(copy);

                float sum = 0;
                foreach (var heuristic in _heuristics)
                {
                    sum += heuristic.CalculateScore(solution);
                }

                copy.Score = sum;
                _bag.Add(copy);
            }
        }

        var assignments = SolutionWalkingHelper.EnumerableAssignments(solution).Where(pair => !pair.assignment.HasValuesSet());

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

                    CalculateSumAndAddIfPassesRules(bag, action);
                }
            }
        });

        return bag.OrderByDescending(a => a.Score).ToArray();
    }

    internal class Level
    {
        public int Depth;
        public PartialSolution CurrentPartialSolution;
        public PartialSolution[] FollowingStates = Array.Empty<PartialSolution>();
        public int LastPickedFollowingState = -1;
    }
}