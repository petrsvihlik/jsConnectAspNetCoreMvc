namespace jsConnect.Exceptions;

/// <summary>Raised when a request JWT fails HMAC signature verification.</summary>
public class SignatureInvalidException : JsConnectException
{
    public SignatureInvalidException(string message) : base(message) { }
    public SignatureInvalidException(string message, Exception inner) : base(message, inner) { }
}
