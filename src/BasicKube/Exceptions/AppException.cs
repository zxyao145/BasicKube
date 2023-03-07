namespace BasicKube.Api.Exceptions;

public class AppException : Exception
{

    public AppException(string? message, Exception? innerException) 
        : base(message, innerException)
    {

    }
}

public class InvalidCmdException : Exception
{
    public string? Cmd { get; set; }

    public InvalidCmdException(string message, string? cmd = null)
        : base(message)
    {
        this.Cmd = cmd;
    }
}
