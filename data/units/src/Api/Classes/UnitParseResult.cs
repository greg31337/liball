namespace units;

/// <summary>
/// Carries the outcome, source position, and candidates of unit parsing.
/// </summary>
public class UnitParseResult : IUnitParseResult
{
    internal bool _success;
    internal UnitDefinition? _unit;
    internal UnitErrorCode _errorCode;
    internal string _message;
    internal int _position;
    internal UnitDefinition[] _candidates;

    internal UnitParseResult(bool success, UnitDefinition? unit, UnitErrorCode errorCode, string? message, int position, UnitDefinition[] candidates)
    {
        ValidateCandidates(candidates);

        if (success && unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (success && errorCode != UnitErrorCode.None)
        {
            throw new System.ArgumentException("Successful unit parse results must use UnitErrorCode.None.");
        }

        if (!success && errorCode == UnitErrorCode.None)
        {
            throw new System.ArgumentException("Failed unit parse results must use a non-success error code.");
        }

        if (success && position != -1)
        {
            throw new System.ArgumentException("Successful unit parse results must use source position -1.");
        }

        if (!success && position < 0)
        {
            throw new System.ArgumentException("Failed unit parse results must use a non-negative source position.");
        }

        _success = success;
        _unit = unit;
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

    internal static UnitParseResult CreateSuccess(UnitDefinition unit)
    {
        return new UnitParseResult(true, unit, UnitErrorCode.None, "", -1, new UnitDefinition[0]);
    }

    internal static UnitParseResult CreateFailure(UnitErrorCode errorCode, string? message, int position, UnitDefinition[] candidates)
    {
        return new UnitParseResult(false, null, errorCode, message, position, candidates);
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
    /// Gets the parsed unit.
    /// </summary>
    /// <returns>The parsed unit definition.</returns>
    /// <exception cref="System.InvalidOperationException">The result represents a failure.</exception>
    public UnitDefinition GetUnit()
    {
        if (!_success || _unit == null)
        {
            throw new System.InvalidOperationException("Failed unit parse results do not carry a unit.");
        }

        return _unit;
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
    /// Returns a copy of ambiguity candidates.
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
