namespace BasicKube.Api.Exceptions;

public class AppException : Exception
{
    public int Code { get; set; } = -1;
    public AppException(string? message, 
        int code = -1,
        Exception? innerException = null
        ) : base(message, innerException)
    {
        this.Code = code;
    }
}

public class InvalidParamterException : AppException
{
    public string? Cmd { get; set; }

    public InvalidParamterException(string message, string? cmd = null)
        : base(message)
    {
        this.Cmd = cmd;
    }
}
