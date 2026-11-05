namespace units;

/// <summary>
/// Carries the outcome, source position, and candidates of quantity parsing.
/// </summary>
public class QuantityParseResult : IQuantityParseResult
{
    internal bool _success;
    internal QuantityValue? _quantity;
    internal UnitErrorCode _errorCode;
    internal string _message;
    internal int _position;
    internal UnitDefinition[] _candidates;

    internal QuantityParseResult(bool success, QuantityValue? quantity, UnitErrorCode errorCode, string? message, int position, UnitDefinition[] candidates)
    {
        ValidateCandidates(candidates);

        if (success && quantity == null)
        {
            throw new System.ArgumentNullException("quantity");
        }

        if (success && errorCode != UnitErrorCode.None)
        {
            throw new System.ArgumentException("Successful quantity parse results must use UnitErrorCode.None.");
        }

        if (!success && errorCode == UnitErrorCode.None)
        {
            throw new System.ArgumentException("Failed quantity parse results must use a non-success error code.");
        }

        if (success && position != -1)
        {
            throw new System.ArgumentException("Successful quantity parse results must use source position -1.");
        }

        if (!success && position < 0)
        {
            throw new System.ArgumentException("Failed quantity parse results must use a non-negative source position.");
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

        _position = position;
        _candidates = CopyCandidates(candidates);
    }

    internal static QuantityParseResult CreateSuccess(QuantityValue quantity)
    {
        return new QuantityParseResult(true, quantity, UnitErrorCode.None, "", -1, new UnitDefinition[0]);
    }

    internal static QuantityParseResult CreateFailure(UnitErrorCode errorCode, string? message, int position, UnitDefinition[] candidates)
    {
        return new QuantityParseResult(false, null, errorCode, message, position, candidates);
    }

    /// <summary>
    /// Reports whether parsing succeeded.
    /// </summary>
    /// <returns>True when parsing succeeded; otherwise, false.</returns>
    public bool IsSuccess()
    {
        return _success;
    }

    /// <summary>
    /// Reports whether parsing failed.
    /// </summary>
    /// <returns>True when parsing failed; otherwise, false.</returns>
    public bool IsFailure()
    {
        return !_success;
    }

    /// <summary>
    /// Gets the parsed quantity.
    /// </summary>
    /// <returns>The parsed quantity value.</returns>
    /// <exception cref="System.InvalidOperationException">The result represents a failure.</exception>
    public QuantityValue GetQuantity()
    {
        if (!_success || _quantity == null)
        {
            throw new System.InvalidOperationException("Failed quantity parse results do not carry a quantity.");
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
    /// Gets the zero-based source position of the failure, or -1 for success.
    /// </summary>
    /// <returns>The source position.</returns>
    public int GetPosition()
    {
        return _position;
    }

    /// <summary>
    /// Returns a copy of unit ambiguity candidates.
    /// </summary>
    /// <returns>A copy of the candidate unit definitions.</returns>
    public UnitDefinition[] GetCandidates()
    {
        return CopyCandidates(_candidates);
    }

    internal static void ValidateCandidates(UnitDefinition[] candidates)
    {
        if (candidates == null)
        {
            throw new System.ArgumentNullException("candidates");
        }

        int index = 0;
        while (index < candidates.Length)
        {
            if (candidates[index] == null)
            {
                throw new System.ArgumentNullException("candidates");
            }

            index = index + 1;
        }
    }

    internal static UnitDefinition[] CopyCandidates(UnitDefinition[] candidates)
    {
        UnitDefinition[] copy = new UnitDefinition[candidates.Length];
        int index = 0;

        while (index < candidates.Length)
        {
            copy[index] = candidates[index];
            index = index + 1;
        }

        return copy;
    }
}
