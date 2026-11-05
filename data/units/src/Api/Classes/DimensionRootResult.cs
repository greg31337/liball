namespace units;

/// <summary>
/// Reports whether a dimension vector has an exact integer root.
/// </summary>
public class DimensionRootResult : IDimensionRootResult
{
    internal bool _success;
    internal DimensionVector? _dimension;
    internal UnitErrorCode _errorCode;
    internal string _message;

    internal DimensionRootResult(bool success, DimensionVector? dimension, UnitErrorCode errorCode, string? message)
    {
        if (success && dimension == null)
        {
            throw new System.ArgumentNullException("dimension");
        }

        if (success && errorCode != UnitErrorCode.None)
        {
            throw new System.ArgumentException("Successful root results must use UnitErrorCode.None.");
        }

        if (!success && errorCode == UnitErrorCode.None)
        {
            throw new System.ArgumentException("Failed root results must use a non-success error code.");
        }

        if (!success && dimension != null)
        {
            throw new System.ArgumentException("Failed root results must not carry a dimension.");
        }

        _success = success;
        _dimension = dimension;

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

    internal static DimensionRootResult CreateSuccess(DimensionVector dimension)
    {
        return new DimensionRootResult(true, dimension, UnitErrorCode.None, "");
    }

    internal static DimensionRootResult CreateFailure(UnitErrorCode errorCode, string? message)
    {
        return new DimensionRootResult(false, null, errorCode, message);
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
    /// Gets the dimension.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    /// <exception cref="System.InvalidOperationException">The result represents a failure and therefore has no successful value.</exception>
    public DimensionVector GetDimension()
    {
        if (!_success || _dimension == null)
        {
            throw new System.InvalidOperationException("Failed root results do not carry a dimension.");
        }

        return _dimension;
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
