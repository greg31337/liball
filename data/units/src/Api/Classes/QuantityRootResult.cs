namespace units;

/// <summary>
/// Carries either a rooted quantity or an error.
/// </summary>
public class QuantityRootResult : IQuantityRootResult
{
    internal bool _success;
    internal QuantityValue? _quantity;
    internal UnitErrorCode _errorCode;
    internal string _message;

    internal QuantityRootResult(bool success, QuantityValue? quantity, UnitErrorCode errorCode, string? message)
    {
        if (success && quantity == null)
        {
            throw new System.ArgumentNullException("quantity");
        }

        if (success && errorCode != UnitErrorCode.None)
        {
            throw new System.ArgumentException("Successful quantity root results must use UnitErrorCode.None.");
        }

        if (!success && errorCode == UnitErrorCode.None)
        {
            throw new System.ArgumentException("Failed quantity root results must use a non-success error code.");
        }

        if (!success && quantity != null)
        {
            throw new System.ArgumentException("Failed quantity root results must not carry a quantity.");
        }

        _success = success;
        _quantity = quantity;

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

    internal static QuantityRootResult CreateSuccess(QuantityValue quantity)
    {
        return new QuantityRootResult(true, quantity, UnitErrorCode.None, "");
    }

    internal static QuantityRootResult CreateFailure(UnitErrorCode errorCode, string? message)
    {
        return new QuantityRootResult(false, null, errorCode, message);
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
    /// Gets the quantity.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    /// <exception cref="System.InvalidOperationException">The result represents a failure and therefore has no successful value.</exception>
    public QuantityValue GetQuantity()
    {
        if (!_success || _quantity == null)
        {
            throw new System.InvalidOperationException("Failed root results do not carry a quantity.");
        }

        return _quantity;
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
