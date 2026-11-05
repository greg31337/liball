namespace json;

/// <summary>Validates publicly mutable JSON model graphs.</summary>
public class JsonValueValidator : IJsonValueValidator
{
    /// <summary>Initializes a reusable JSON value validator.</summary>
    public JsonValueValidator()
    {
    }

    /// <summary>Validates one JSON value graph and its resource limits.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="limits">The resource limits to apply.</param>
    /// <returns>The validation outcome.</returns>
    public JsonOperationResult ValidateValue(JsonValue value, JsonLimits limits)
    {
        JsonOperationResult? result = null;
        JsonExceptionCapture capture = new JsonExceptionCapture(() =>
        {
            JsonModelValidator validator = new JsonModelValidator(limits);
            result = validator.ValidateValueGraph(value);
        });
        capture.ExecuteOperation();
        return CompleteValidation(result, capture.GetFailure());
    }

    /// <summary>Validates one JSON document and its resource limits.</summary>
    /// <param name="document">The document to validate.</param>
    /// <param name="limits">The resource limits to apply.</param>
    /// <returns>The validation outcome.</returns>
    public JsonOperationResult ValidateDocument(JsonDocument document, JsonLimits limits)
    {
        JsonOperationResult? result = null;
        JsonExceptionCapture capture = new JsonExceptionCapture(() =>
        {
            JsonModelValidator validator = new JsonModelValidator(limits);
            result = validator.ValidateDocumentModel(document);
        });
        capture.ExecuteOperation();
        return CompleteValidation(result, capture.GetFailure());
    }

    JsonOperationResult CompleteValidation(
        JsonOperationResult? result,
        global::System.Exception? failure)
    {
        if (failure == null)
        {
            return result
                ?? throw new global::System.InvalidOperationException("Model validation did not produce a result.");
        }

        JsonLimitException? limitFailure = failure as JsonLimitException;
        if (limitFailure != null)
        {
            JsonOperationResult failed = new JsonOperationResult();
            failed.Success = false;
            failed.Diagnostic = limitFailure.Diagnostic;
            return failed;
        }

        JsonParseException? parseFailure = failure as JsonParseException;
        if (parseFailure != null)
        {
            JsonOperationResult failed = new JsonOperationResult();
            failed.Success = false;
            failed.Diagnostic = parseFailure.Diagnostic;
            return failed;
        }

        throw failure;
    }
}
