namespace Optimizer.Logic.Work;

internal interface ICopyable<out T>
{
    T CreateCopy();
}

internal interface IState
{
    float? Score { get; set; }
    int Depth { get; }
    int MaxDepth { get; }

    bool CheckIfIsCompleteSolution();
}

internal interface IAction<TState>
    where TState : struct
{
    public float? Score { get; set; }

    void ApplyToState(ref TState state);
}

internal abstract class BaseOptimizer<TState, TAction, TOutput>
    where TState : struct, IState, ICopyable<TState>
    where TAction : struct, IAction<TState>
    where TOutput : IOutput
{
    internal sealed class Level
    {
        public int Depth;
        public TState State;
        public TAction[] FollowingActions;
        public int LastPickedFollowingState = -1;

        public Level(int depth, TState state, TAction[] followingActions)
        {
            Depth = depth;
            State = state;
            FollowingActions = followingActions;
        }

        public TAction? TakeNextAction()
        {
            var nextIndex = LastPickedFollowingState + 1;
            if (nextIndex < FollowingActions.Length)
            {
                LastPickedFollowingState = nextIndex;
                return FollowingActions[nextIndex];
            }

            return null;
        }

        public float PercentageSeen()
        {
            return Math.Max(0, LastPickedFollowingState) / (float)FollowingActions.Length;
        }
    }

    public IOptimizerStateDetails<TOutput> StateDetails => OptimizerStateDetails;
    protected Stack<Level> Levels { get; private set; }
    protected BaseOptimizerStateDetails<TOutput> OptimizerStateDetails;
    protected int ScoreEvaluationCounter;
    protected CancellationToken CancellationToken;

    internal BaseOptimizer(CancellationToken cancellationToken)
    {
        OptimizerStateDetails = new BaseOptimizerStateDetails<TOutput>();
        Levels = new Stack<Level>();
        CancellationToken = cancellationToken;
    }

    protected internal bool StopIfCancelled()
    {
        return CancellationToken.IsCancellationRequested;
    }

    protected void IncrementOperationsDone()
    {
        OptimizerStateDetails.OperationsDone++;
    }

    public void Optimize()
    {
        OptimizerStateDetails.Complete = false;

        var startState = CreateStartState();
        var startLevel = new Level(0, startState, GetFollowingActions(startState));
        Levels.Push(startLevel);

        while (!StopIfCancelled())
        {
            if (Levels.Count <= 0)
            {
                break;
            }

            var level = Levels.Peek();
            UpdateStatisticsAfterPickingUpLevel(level);

            var action = level.TakeNextAction();
            if (action != null)
            {
                // let's go deeper
                var state = level.State.CreateCopy();
                action.Value.ApplyToState(ref state);

                if (state.CheckIfIsCompleteSolution())
                {
                    SaveIfBetter(ref state);
                }
                else
                {
                    var newLevel = CreateNewLevel(level, state);
                    if (newLevel != null)
                        Levels.Push(newLevel);
                    else
                        OptimizerStateDetails.DeadEnds++;
                }
            }
            else
            {
                // we have to go back
                Levels.Pop();
            }

            IncrementOperationsDone();
        }

        OptimizerStateDetails.Complete = true;
    }

    protected virtual void UpdateStatisticsAfterPickingUpLevel(Level level)
    {
        OptimizerStateDetails.CurrentDepth = level.Depth;
        OptimizerStateDetails.PartialScore = level.State.Score ?? CalculateScoreAndIncrementEvaluationCount(level.State);

        double multiplier = 1;
        double sum = 0;
        foreach (var l in Levels.Reverse())
        {
            sum += l.PercentageSeen() * multiplier;
            multiplier /= l.FollowingActions.Length;
        }

        OptimizerStateDetails.PercentDomainSeen = (float)(sum * 100);
    }

    protected virtual void SaveIfBetter(ref TState state)
    {
        if (state.Score > (StateDetails.Result?.Score ?? float.MinValue))
        {
            var output = CreateOutputFromState(state);
            OptimizerStateDetails.Result = output;
            OptimizerStateDetails.InvokeBetterSolutionFoundEvent(this, output);
        }
    }

    protected virtual Level? CreateNewLevel(Level previous, TState newState)
    {
        var actions = GetFollowingActions(newState);
        if(actions.Length > 0)
            return new Level(previous.Depth + 1, newState, actions);
        return null;
    }

    protected virtual (int depth, float proportion, float persistence) GetSearchingStrategyForState(TState state)
    {
        return (0, 1, 1);
    }

    protected virtual TAction[] GetFollowingActions(TState state)
    {
        var actions = CollectAvailableActionsMultithreaded(state);

        if (actions.Length < state.MaxDepth - state.Depth)
            return Array.Empty<TAction>();

        var (depth, proportion, persistence) = GetSearchingStrategyForState(state);
        var complete = 0;
        Parallel.For(0, actions.Length, i =>
        {
            //for(int i = 0; i < actions.Length; i++
            actions[i].Score = DeepSearchForMaxScore(actions[i], ref state, depth, proportion, persistence);

            Interlocked.Increment(ref complete);
            OptimizerStateDetails.CurrentDepthCompleteness = complete / (float)actions.Length;
        });

        var value = actions.Where(a => a.Score != null)
            .OrderByDescending(a => a.Score)
            .ToArray();

        return value;
    }

    private float? DeepSearchForMaxScore(TAction action, ref TState state, int depth, float proportion, float persistence)
    {
        proportion = MathF.Max(0, MathF.Min(proportion, 1));
        var copy = state.CreateCopy();
        action.ApplyToState(ref copy);
        return DeepSearchForMaxScore(ref copy, depth, proportion, persistence);
    }

    private float? DeepSearchForMaxScore(ref TState state, int depth, float proportion, float persistence)
    {
        if (state.CheckIfIsCompleteSolution())
        {
            // if complete, return score for state
            return state.Score ??= CalculateScoreAndIncrementEvaluationCount(state);
        }

        if (depth <= 0)
        {
            // solution is incomplete but not a dead-end
            // return a score
            return state.Score ??= CalculateScoreAndIncrementEvaluationCount(state);
        }

        var actions = CollectAvailableActions(state);

        if (actions.Length < state.MaxDepth - state.Depth)
        {
            // not enough moves, we're at a dead-end
            return null;
        }

        IEnumerable<TAction> actionsToEvaluate;
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
            var score = DeepSearchForMaxScore(action, ref state, depth - 1, newProportion, persistence);

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
        proportion = validScoreCount / (float)actions.Length;

        // weighted average between average of averages and current state score with maxValue from evaluated actions
        return (1 - proportion) * (accumulator / validScoreCount + (state.Score ??= CalculateScoreAndIncrementEvaluationCount(state))) / 2f + proportion * maxValue;
    }

    protected static void Shuffle<T>(T[] array)
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
    /// Collect actions that pass rules, but do not bother calculating score.
    /// </summary>
    protected abstract TAction[] CollectAvailableActions(TState state);

    /// <summary>
    /// Collect actions that pass rules, but do not bother calculating score.
    /// </summary>
    protected abstract TAction[] CollectAvailableActionsMultithreaded(TState state);

    /// <summary>
    /// Create start state where there were no moves done yet.
    /// </summary>
    /// <returns></returns>
    protected abstract TState CreateStartState();

    protected abstract TOutput CreateOutputFromState(TState state);

    protected float CalculateScoreAndIncrementEvaluationCount(TState state)
    {
        var score = CalculateScore(state);
        Interlocked.Increment(ref ScoreEvaluationCounter);
        OptimizerStateDetails.Evaluations = ScoreEvaluationCounter;
        return score;
    }

    protected abstract float CalculateScore(TState state);
}
