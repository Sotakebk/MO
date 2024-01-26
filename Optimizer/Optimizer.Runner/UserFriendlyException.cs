namespace Optimizer.Runner;

class UserFriendlyException : Exception
{
    // ReSharper disable once InconsistentNaming
    public string MessagePL { get; }

    public UserFriendlyException(string exceptionEn, string exceptionPl) : base(exceptionEn)
    {
        MessagePL = exceptionPl;
    }
}