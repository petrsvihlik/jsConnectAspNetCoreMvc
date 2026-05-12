namespace jsConnect.Exceptions;

/// <summary>Raised when a configuration value is invalid (e.g. secret too short, unsupported algorithm).</summary>
public class InvalidValueException : JsConnectException
{
    public InvalidValueException(string message) : base(message) { }
}
