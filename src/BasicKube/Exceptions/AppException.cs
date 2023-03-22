namespace BasicKube.Api.Exceptions;

public class AppException : Exception
{
    public AppException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}

public class InvalidParamterException : AppException
{
    public string? Cmd { get; set; }

    public InvalidParamterException(string message, string? cmd = null)
        : base(message, null)
    {
        this.Cmd = cmd;
    }
}
