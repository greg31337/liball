namespace units;

/// <summary>
/// Carries the outcome and candidates of a quantity-kind lookup.
/// </summary>
public class QuantityKindLookupResult : IQuantityKindLookupResult
{
    internal bool _success;
    internal bool _ambiguous;
    internal QuantityKindDefinition[] _quantityKinds;
    internal UnitErrorCode _errorCode;
    internal string _message;

    internal QuantityKindLookupResult(bool success, bool ambiguous, QuantityKindDefinition[] quantityKinds, UnitErrorCode errorCode, string? message)
    {
        ValidateQuantityKinds(quantityKinds);

        if (success && errorCode != UnitErrorCode.None)
        {
            throw new System.ArgumentException("Successful quantity kind lookup results must use UnitErrorCode.None.");
        }

        if (!success && errorCode == UnitErrorCode.None)
        {
            throw new System.ArgumentException("Failed quantity kind lookup results must use a non-success error code.");
        }

        _success = success;
        _ambiguous = ambiguous;
        _quantityKinds = CopyQuantityKinds(quantityKinds);
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

    internal static QuantityKindLookupResult FromMatches(QuantityKindDefinition[] quantityKinds, string text)
    {
        ValidateQuantityKinds(quantityKinds);

        if (quantityKinds.Length == 1)
        {
            return new QuantityKindLookupResult(true, false, quantityKinds, UnitErrorCode.None, "");
        }

        if (quantityKinds.Length == 0)
        {
            return new QuantityKindLookupResult(false, false, quantityKinds, UnitErrorCode.LookupNotFound, "Quantity kind was not found: " + text);
        }

        return new QuantityKindLookupResult(false, true, quantityKinds, UnitErrorCode.LookupAmbiguous, "Quantity kind lookup was ambiguous: " + text);
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
    /// Reports whether lookup produced more than one candidate.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsAmbiguous()
    {
        return _ambiguous;
    }

    /// <summary>
    /// Gets the quantity kind.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition GetQuantityKind()
    {
        if (!_success)
        {
            throw new System.InvalidOperationException("Failed quantity kind lookup results do not carry a single quantity kind.");
        }

        return _quantityKinds[0];
    }

    /// <summary>
    /// Returns a copy of the quantity kinds.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public QuantityKindDefinition[] GetQuantityKinds()
    {
        return CopyQuantityKinds(_quantityKinds);
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

    internal static void ValidateQuantityKinds(QuantityKindDefinition[] quantityKinds)
    {
        if (quantityKinds == null)
        {
            throw new System.ArgumentNullException("quantityKinds");
        }

        int index = 0;
        while (index < quantityKinds.Length)
        {
            if (quantityKinds[index] == null)
            {
                throw new System.ArgumentNullException("quantityKinds");
            }

            index = index + 1;
        }
    }

    internal static QuantityKindDefinition[] CopyQuantityKinds(QuantityKindDefinition[] quantityKinds)
    {
        QuantityKindDefinition[] copy = new QuantityKindDefinition[quantityKinds.Length];
        int index = 0;

        while (index < quantityKinds.Length)
        {
            copy[index] = quantityKinds[index];
            index = index + 1;
        }

        return copy;
    }
}
