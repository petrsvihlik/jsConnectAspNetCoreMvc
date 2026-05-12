namespace jsConnect.Exceptions;

/// <summary>
/// Base exception type for all jsConnect protocol failures.
/// </summary>
public class JsConnectException : Exception
{
    public JsConnectException(string message) : base(message) { }
    public JsConnectException(string message, Exception inner) : base(message, inner) { }
}
