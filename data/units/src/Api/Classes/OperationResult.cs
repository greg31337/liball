namespace units;

/// <summary>
/// Reports the success or failure of an operation that has no value payload.
/// </summary>
public class OperationResult : IOperationResult
{
    internal bool _success;
    internal UnitErrorCode _errorCode;
    internal string _message;

    internal OperationResult(bool success, UnitErrorCode errorCode, string? message)
    {
        if (success && errorCode != UnitErrorCode.None)
        {
            throw new System.ArgumentException("Successful results must use UnitErrorCode.None.");
        }

        if (!success && errorCode == UnitErrorCode.None)
        {
            throw new System.ArgumentException("Failure results must use a non-success error code.");
        }

        _success = success;
        _errorCode = errorCode;
        if (message == null)
        {
            _message = "";
        }
        else
        {
            _message = message;
        }
    }

    internal static OperationResult CreateSuccess()
    {
        return new OperationResult(true, UnitErrorCode.None, "");
    }

    internal static OperationResult CreateFailure(UnitErrorCode errorCode, string? message)
    {
        return new OperationResult(false, errorCode, message);
    }

    /// <summary>
    /// Reports whether this result represents a successful operation.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsSuccess()
    {
        return _success;
    }

    /// <summary>
    /// Reports whether this result represents a failed operation.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsFailure()
    {
        return !_success;
    }

    /// <summary>
    /// Gets the error code.
    /// </summary>
    /// <returns>The resulting unit error code.</returns>
    public UnitErrorCode GetErrorCode()
    {
        return _errorCode;
    }

    /// <summary>
    /// Gets the developer-readable result message, which is empty for success.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetMessage()
    {
        return _message;
    }
}
