namespace units;

/// <summary>
/// Carries the outcome and candidates of a unit lookup.
/// </summary>
public class UnitLookupResult : IUnitLookupResult
{
    internal bool _success;
    internal bool _ambiguous;
    internal UnitDefinition[] _units;
    internal UnitErrorCode _errorCode;
    internal string _message;

    internal UnitLookupResult(bool success, bool ambiguous, UnitDefinition[] units, UnitErrorCode errorCode, string? message)
    {
        ValidateUnits(units);

        if (success && errorCode != UnitErrorCode.None)
        {
            throw new System.ArgumentException("Successful unit lookup results must use UnitErrorCode.None.");
        }

        if (!success && errorCode == UnitErrorCode.None)
        {
            throw new System.ArgumentException("Failed unit lookup results must use a non-success error code.");
        }

        _success = success;
        _ambiguous = ambiguous;
        _units = CopyUnits(units);
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

    internal static UnitLookupResult FromMatches(UnitDefinition[] units, string text)
    {
        ValidateUnits(units);

        if (units.Length == 1)
        {
            return new UnitLookupResult(true, false, units, UnitErrorCode.None, "");
        }

        if (units.Length == 0)
        {
            return new UnitLookupResult(false, false, units, UnitErrorCode.LookupNotFound, "Unit was not found: " + text);
        }

        return new UnitLookupResult(false, true, units, UnitErrorCode.LookupAmbiguous, "Unit lookup was ambiguous: " + text);
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
    /// Gets the unit paired with the stored numeric value.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    /// <exception cref="System.InvalidOperationException">The result represents a failure and therefore has no successful value.</exception>
    public UnitDefinition GetUnit()
    {
        if (!_success)
        {
            throw new System.InvalidOperationException("Failed unit lookup results do not carry a single unit.");
        }

        return _units[0];
    }

    /// <summary>
    /// Returns a copy of the units.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] GetUnits()
    {
        return CopyUnits(_units);
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

    internal static void ValidateUnits(UnitDefinition[] units)
    {
        if (units == null)
        {
            throw new System.ArgumentNullException("units");
        }

        int index = 0;
        while (index < units.Length)
        {
            if (units[index] == null)
            {
                throw new System.ArgumentNullException("units");
            }

            index = index + 1;
        }
    }

    internal static UnitDefinition[] CopyUnits(UnitDefinition[] units)
    {
        UnitDefinition[] copy = new UnitDefinition[units.Length];
        int index = 0;

        while (index < units.Length)
        {
            copy[index] = units[index];
            index = index + 1;
        }

        return copy;
    }
}
