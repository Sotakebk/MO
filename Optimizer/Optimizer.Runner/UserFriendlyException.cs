namespace Optimizer.Runner;

class UserFriendlyException : Exception
{
    // ReSharper disable once InconsistentNaming
    private string ExceptionPL { get; set; }

    public UserFriendlyException(string exceptionEn, string exceptionPl) : base(exceptionEn)
    {
        ExceptionPL = exceptionPl;
    }
}