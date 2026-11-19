namespace jsConnect.Exceptions;

/// <summary>Raised when a required field is missing from the request payload (e.g. the <c>jwt</c> query parameter).</summary>
public class FieldNotFoundException(string field, string collection)
    : JsConnectException($"Field not found: {collection}[{field}].")
{
    public string FieldName { get; } = field;
    public string CollectionName { get; } = collection;
}
