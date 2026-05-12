namespace jsConnect.Exceptions;

/// <summary>Raised when a request JWT is past its <c>exp</c> claim.</summary>
public class ExpiredException : JsConnectException
{
    public ExpiredException(string message) : base(message) { }
    public ExpiredException(string message, Exception inner) : base(message, inner) { }
}
