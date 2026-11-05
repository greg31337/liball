namespace units;

/// <summary>
/// Carries the outcome and ambiguity candidates of dynamic quantity construction.
/// </summary>
public class QuantityConstructionResult : IQuantityConstructionResult
{
    internal bool _success;
    internal QuantityValue? _quantity;
    internal UnitErrorCode _errorCode;
    internal string _message;
    internal UnitDefinition[] _candidates;

    internal QuantityConstructionResult(bool success, QuantityValue? quantity, UnitErrorCode errorCode, string? message, UnitDefinition[] candidates)
    {
        UnitLookupResult.ValidateUnits(candidates);

        if (success && quantity == null)
        {
            throw new System.ArgumentNullException("quantity");
        }

        if (success && errorCode != UnitErrorCode.None)
        {
            throw new System.ArgumentException("Successful quantity construction results must use UnitErrorCode.None.");
        }

        if (!success && errorCode == UnitErrorCode.None)
        {
            throw new System.ArgumentException("Failed quantity construction results must use a non-success error code.");
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

        _candidates = UnitLookupResult.CopyUnits(candidates);
    }

    internal static QuantityConstructionResult CreateSuccess(QuantityValue quantity)
    {
        return new QuantityConstructionResult(true, quantity, UnitErrorCode.None, "", new UnitDefinition[0]);
    }

    internal static QuantityConstructionResult CreateFailure(UnitErrorCode errorCode, string? message, UnitDefinition[] candidates)
    {
        return new QuantityConstructionResult(false, null, errorCode, message, candidates);
    }

    /// <summary>
    /// Reports whether quantity construction succeeded.
    /// </summary>
    /// <returns>True when construction succeeded; otherwise, false.</returns>
    public bool IsSuccess()
    {
        return _success;
    }

    /// <summary>
    /// Reports whether quantity construction failed.
    /// </summary>
    /// <returns>True when construction failed; otherwise, false.</returns>
    public bool IsFailure()
    {
        return !_success;
    }

    /// <summary>
    /// Gets the constructed quantity.
    /// </summary>
    /// <returns>The constructed quantity value.</returns>
    /// <exception cref="System.InvalidOperationException">The result represents a failure.</exception>
    public QuantityValue GetQuantity()
    {
        if (!_success || _quantity == null)
        {
            throw new System.InvalidOperationException("Failed quantity construction results do not carry a quantity.");
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

    /// <summary>
    /// Returns a copy of unit ambiguity candidates.
    /// </summary>
    /// <returns>A copy of the candidate unit definitions.</returns>
    public UnitDefinition[] GetCandidates()
    {
        return UnitLookupResult.CopyUnits(_candidates);
    }
}
