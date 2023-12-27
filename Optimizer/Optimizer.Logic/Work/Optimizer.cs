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
    private readonly IRule[] _rules;
    private readonly IHeuristic[] _heuristics;
    public PartialSolution CleanPartialSolution;

    public Optimizer(LoggerFactory loggerFactory, Input input, OptimizationState state, IRule[] rules, IHeuristic[] heuristics)
    {
        _logger = loggerFactory.CreateLogger<Optimizer>();
        _input = input;
        _state = state;
        CleanPartialSolution = BuildCleanPartialSolution(input);
        _rules = rules;
        _heuristics = heuristics;
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

        var expectedDepth = _input.Combinations.Sum(c => c.TotalCount);
        var levels = new Stack<Level>(expectedDepth);

        var root = new Level();
        root.Depth = 0;
        root.CurrentScore = 0;
        root.LastPickedActionId = -1;
        root.AvailableActions = CollectAvailableActions(CleanPartialSolution);

        levels.Push(root);

        while (!StopIfCancelled())
        {
            _state.OperationsDone++;
            if (levels.Count <= 0)
            {
                break;
            }

            var level = levels.Peek();
            _state.CurrentDepth = level.Depth;

            level.LastPickedActionId++;
            if (level.LastPickedActionId < level.AvailableActions.Length)
            {
                // let's go deeper
                var action = level.AvailableActions[level.LastPickedActionId];

                var newPartialSolution = level.CurrentPartialSolution.CreateDeepCopy();
                action.Apply(newPartialSolution);

                if (CheckIfFinishedSolutionAndSaveIfBetter(newPartialSolution))
                {
                    // don't collect actions, this schedule is complete
                    // try next action of this level or go back
                    continue;
                }

                var newLevel = new Level()
                {
                    AvailableActions = CollectAvailableActions(newPartialSolution),
                    CurrentPartialSolution = newPartialSolution,
                    CurrentScore = action.Score,
                    Depth = level.Depth + 1,
                    LastPickedActionId = -1
                };
                levels.Push(newLevel);
            }
            else
            {
                // we have to go back
                levels.Pop();
            }
        }

        _state.IsWorking = false;
    }

    private bool CheckIfFinishedSolutionAndSaveIfBetter(PartialSolution solution)
    {
        if (solution.SupervisorAndReviewerIdToAssignmentsLeft.Any(s => s.Value > 0))
            return false;

        decimal score = 0;
        foreach (var heuristic in _heuristics)
        {
            score += heuristic.CalculateScore(solution);
        }

        if (score > (_state.Result?.Score ?? decimal.MinValue))
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
                s.Days[i].VacantBlocks = new SolutionBlock[d.Blocks.Length];
                var vbs = s.Days[i].VacantBlocks;

                for (var j = 0; j < d.Blocks.Length; j++)
                {
                    var b = d.Blocks[j];
                    vbs[j].Offset = b.Offset;
                    vbs[j].RoomId = b.BlockId;
                    vbs[j].Assignments = new SolutionAssignment[b.Assignments.Length];

                    var assignments = vbs[j].Assignments;
                    for (var k = 0; k < b.Assignments.Length; k++)
                    {
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

    private AvailableAction[] CollectAvailableActions(PartialSolution solution)
    {
        var bag = new ConcurrentBag<AvailableAction>();
        foreach (var (id, assignment) in SolutionWalkingHelper.EnumerableAssignments(solution))
        {
            if (assignment.IsAllSet)
                continue;

            void CalculateSumAndAddIfPassesRules(
                ConcurrentBag<AvailableAction> _bag,
                AvailableAction action)
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
                    var score = PeekActionScore(action, solution);
                    action.Score = score;
                    _bag.Add(action);
                }
            }

            if (!assignment.IsChairPersonSet)
            {
                foreach (var person in _input.ChairPersonIds)
                {
                    var action = new AvailableAction(person, id);
                    CalculateSumAndAddIfPassesRules(bag, action);
                }
            }

            if (!assignment.IsSupervisorAndReviewerSet)
            {
                foreach (var keyValuePair in solution.SupervisorAndReviewerIdToAssignmentsLeft)
                {
                    if (keyValuePair.Value <= 0)
                        continue;

                    var action = new AvailableAction(keyValuePair.Key.supervisorId, keyValuePair.Key.reviewerId, id);
                    CalculateSumAndAddIfPassesRules(bag, action);
                }
            }
        }

        return bag.OrderByDescending(a => a.Score).ToArray();
    }

    private decimal PeekActionScore(AvailableAction action, PartialSolution solution)
    {
        decimal sum = 0;
        foreach (var heuristic in _heuristics)
        {
            sum += heuristic.CalculateScore(action, solution);
        }

        return sum;
    }

    private static PartialSolution BuildCleanPartialSolution(Input input)
    {
        var partialSolution = new PartialSolution();
        partialSolution.Days = new Day[input.Days.Length];
        for (var i = 0; i < input.Days.Length; i++)
        {
            var od = input.Days[i];
            partialSolution.Days[i] = new Day()
            {
                DayId = od.Id,
                Blocks = new Block[od.VacantBlocks.Length]
            };

            for (var j = 0; j < od.VacantBlocks.Length; j++)
            {
                var ovb = od.VacantBlocks[j];
                partialSolution.Days[i].Blocks[j] = new Block()
                {
                    BlockId = ovb.RoomId,
                    Offset = ovb.Offset,
                    Assignments = new Assignment[ovb.SlotCount]
                };
            }
        }

        var dict = new Dictionary<(byte, byte), int>();

        foreach (var pair in input.Combinations)
        {
            dict.Add(((byte)pair.PromoterId, (byte)pair.ReviewerId), pair.TotalCount);
        }

        partialSolution.SupervisorAndReviewerIdToAssignmentsLeft = dict;

        return partialSolution;
    }

    internal class Level
    {
        public int Depth;
        public PartialSolution CurrentPartialSolution;
        public decimal CurrentScore;
        public AvailableAction[] AvailableActions;
        public int LastPickedActionId = -1;
    }
}