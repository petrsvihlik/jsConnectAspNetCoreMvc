namespace jsConnect.Exceptions;

/// <summary>Raised when a required field is missing from the request payload (e.g. the <c>jwt</c> query parameter).</summary>
public class FieldNotFoundException : JsConnectException
{
    public string FieldName { get; }
    public string CollectionName { get; }

    public FieldNotFoundException(string field, string collection)
        : base($"Field not found: {collection}[{field}].")
    {
        FieldName = field;
        CollectionName = collection;
    }
}
