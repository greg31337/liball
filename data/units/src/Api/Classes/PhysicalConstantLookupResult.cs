namespace units;

/// <summary>
/// Carries the outcome and candidates of a physical-constant lookup.
/// </summary>
public class PhysicalConstantLookupResult : IPhysicalConstantLookupResult
{
    internal bool _success;
    internal bool _ambiguous;
    internal PhysicalConstantDefinition[] _physicalConstants;
    internal UnitErrorCode _errorCode;
    internal string _message;

    internal PhysicalConstantLookupResult(bool success, bool ambiguous, PhysicalConstantDefinition[] physicalConstants, UnitErrorCode errorCode, string? message)
    {
        ValidatePhysicalConstants(physicalConstants);

        if (success && errorCode != UnitErrorCode.None)
        {
            throw new System.ArgumentException("Successful physical-constant lookup results must use UnitErrorCode.None.");
        }

        if (!success && errorCode == UnitErrorCode.None)
        {
            throw new System.ArgumentException("Failed physical-constant lookup results must use a non-success error code.");
        }

        _success = success;
        _ambiguous = ambiguous;
        _physicalConstants = CopyPhysicalConstants(physicalConstants);
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

    internal static PhysicalConstantLookupResult FromMatches(PhysicalConstantDefinition[] physicalConstants, string text)
    {
        ValidatePhysicalConstants(physicalConstants);

        if (physicalConstants.Length == 1)
        {
            return new PhysicalConstantLookupResult(true, false, physicalConstants, UnitErrorCode.None, "");
        }

        if (physicalConstants.Length == 0)
        {
            return new PhysicalConstantLookupResult(false, false, physicalConstants, UnitErrorCode.LookupNotFound, "Physical constant was not found: " + text);
        }

        return new PhysicalConstantLookupResult(false, true, physicalConstants, UnitErrorCode.LookupAmbiguous, "Physical-constant lookup was ambiguous: " + text);
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
    /// Gets the physical constant.
    /// </summary>
    /// <returns>The resulting physical-constant definition.</returns>
    public PhysicalConstantDefinition GetPhysicalConstant()
    {
        if (!_success)
        {
            throw new System.InvalidOperationException("Failed physical-constant lookup results do not carry a single physical constant.");
        }

        return _physicalConstants[0];
    }

    /// <summary>
    /// Returns a copy of the physical constants.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public PhysicalConstantDefinition[] GetPhysicalConstants()
    {
        return CopyPhysicalConstants(_physicalConstants);
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

    internal static void ValidatePhysicalConstants(PhysicalConstantDefinition[] physicalConstants)
    {
        if (physicalConstants == null)
        {
            throw new System.ArgumentNullException("physicalConstants");
        }

        int index = 0;
        while (index < physicalConstants.Length)
        {
            if (physicalConstants[index] == null)
            {
                throw new System.ArgumentNullException("physicalConstants");
            }

            index = index + 1;
        }
    }

    internal static PhysicalConstantDefinition[] CopyPhysicalConstants(PhysicalConstantDefinition[] physicalConstants)
    {
        PhysicalConstantDefinition[] copy = new PhysicalConstantDefinition[physicalConstants.Length];
        int index = 0;

        while (index < physicalConstants.Length)
        {
            copy[index] = physicalConstants[index];
            index = index + 1;
        }

        return copy;
    }
}
