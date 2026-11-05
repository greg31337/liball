namespace units;

/// <summary>
/// Carries either a quantity arithmetic value or a typed arithmetic failure.
/// </summary>
public class QuantityArithmeticResult : IQuantityArithmeticResult
{
    internal bool _success;
    internal QuantityValue? _quantity;
    internal UnitErrorCode _errorCode;
    internal string _message;

    internal QuantityArithmeticResult(bool success, QuantityValue? quantity, UnitErrorCode errorCode, string? message)
    {
        if (success && quantity == null)
        {
            throw new System.ArgumentNullException("quantity");
        }

        if (success && errorCode != UnitErrorCode.None)
        {
            throw new System.ArgumentException("Successful quantity arithmetic results must use UnitErrorCode.None.");
        }

        if (!success && errorCode == UnitErrorCode.None)
        {
            throw new System.ArgumentException("Failed quantity arithmetic results must use a non-success error code.");
        }

        if (!success && quantity != null)
        {
            throw new System.ArgumentException("Failed quantity arithmetic results must not carry a quantity.");
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

    internal static QuantityArithmeticResult CreateSuccess(QuantityValue quantity)
    {
        return new QuantityArithmeticResult(true, quantity, UnitErrorCode.None, "");
    }

    internal static QuantityArithmeticResult CreateFailure(UnitErrorCode errorCode, string? message)
    {
        return new QuantityArithmeticResult(false, null, errorCode, message);
    }

    /// <summary>
    /// Reports whether quantity arithmetic succeeded.
    /// </summary>
    /// <returns>True when arithmetic succeeded; otherwise, false.</returns>
    public bool IsSuccess()
    {
        return _success;
    }

    /// <summary>
    /// Reports whether quantity arithmetic failed.
    /// </summary>
    /// <returns>True when arithmetic failed; otherwise, false.</returns>
    public bool IsFailure()
    {
        return !_success;
    }

    /// <summary>
    /// Gets the resulting quantity.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    /// <exception cref="System.InvalidOperationException">The result represents a failure.</exception>
    public QuantityValue GetQuantity()
    {
        if (!_success || _quantity == null)
        {
            throw new System.InvalidOperationException("Failed quantity arithmetic results do not carry a quantity.");
        }

        return _quantity;
    }

    /// <summary>
    /// Gets the stable error code.
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
