namespace units;

/// <summary>
/// Carries the outcome and candidates of a unit-system lookup.
/// </summary>
public class UnitSystemLookupResult : IUnitSystemLookupResult
{
    internal bool _success;
    internal bool _ambiguous;
    internal UnitSystemDefinition[] _unitSystems;
    internal UnitErrorCode _errorCode;
    internal string _message;

    internal UnitSystemLookupResult(bool success, bool ambiguous, UnitSystemDefinition[] unitSystems, UnitErrorCode errorCode, string? message)
    {
        ValidateUnitSystems(unitSystems);

        if (success && errorCode != UnitErrorCode.None)
        {
            throw new System.ArgumentException("Successful unit-system lookup results must use UnitErrorCode.None.");
        }

        if (!success && errorCode == UnitErrorCode.None)
        {
            throw new System.ArgumentException("Failed unit-system lookup results must use a non-success error code.");
        }

        _success = success;
        _ambiguous = ambiguous;
        _unitSystems = CopyUnitSystems(unitSystems);
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

    internal static UnitSystemLookupResult FromMatches(UnitSystemDefinition[] unitSystems, string text)
    {
        ValidateUnitSystems(unitSystems);

        if (unitSystems.Length == 1)
        {
            return new UnitSystemLookupResult(true, false, unitSystems, UnitErrorCode.None, "");
        }

        if (unitSystems.Length == 0)
        {
            return new UnitSystemLookupResult(false, false, unitSystems, UnitErrorCode.LookupNotFound, "Unit system was not found: " + text);
        }

        return new UnitSystemLookupResult(false, true, unitSystems, UnitErrorCode.LookupAmbiguous, "Unit-system lookup was ambiguous: " + text);
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
    /// Gets the unit system.
    /// </summary>
    /// <returns>The resulting unit-system definition.</returns>
    public UnitSystemDefinition GetUnitSystem()
    {
        if (!_success)
        {
            throw new System.InvalidOperationException("Failed unit-system lookup results do not carry a single unit system.");
        }

        return _unitSystems[0];
    }

    /// <summary>
    /// Returns a copy of the unit systems.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitSystemDefinition[] GetUnitSystems()
    {
        return CopyUnitSystems(_unitSystems);
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

    internal static void ValidateUnitSystems(UnitSystemDefinition[] unitSystems)
    {
        if (unitSystems == null)
        {
            throw new System.ArgumentNullException("unitSystems");
        }

        int index = 0;
        while (index < unitSystems.Length)
        {
            if (unitSystems[index] == null)
            {
                throw new System.ArgumentNullException("unitSystems");
            }

            index = index + 1;
        }
    }

    internal static UnitSystemDefinition[] CopyUnitSystems(UnitSystemDefinition[] unitSystems)
    {
        UnitSystemDefinition[] copy = new UnitSystemDefinition[unitSystems.Length];
        int index = 0;

        while (index < unitSystems.Length)
        {
            copy[index] = unitSystems[index];
            index = index + 1;
        }

        return copy;
    }
}
