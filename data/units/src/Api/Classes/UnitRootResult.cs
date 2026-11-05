namespace units;

/// <summary>
/// Carries either a rooted unit definition or an error.
/// </summary>
public class UnitRootResult : IUnitRootResult
{
    internal bool _success;
    internal UnitDefinition? _unit;
    internal UnitErrorCode _errorCode;
    internal string _message;

    internal UnitRootResult(bool success, UnitDefinition? unit, UnitErrorCode errorCode, string? message)
    {
        if (success && unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (success && errorCode != UnitErrorCode.None)
        {
            throw new System.ArgumentException("Successful unit root results must use UnitErrorCode.None.");
        }

        if (!success && errorCode == UnitErrorCode.None)
        {
            throw new System.ArgumentException("Failed unit root results must use a non-success error code.");
        }

        if (!success && unit != null)
        {
            throw new System.ArgumentException("Failed unit root results must not carry a unit.");
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
    }

    internal static UnitRootResult CreateSuccess(UnitDefinition unit)
    {
        return new UnitRootResult(true, unit, UnitErrorCode.None, "");
    }

    internal static UnitRootResult CreateFailure(UnitErrorCode errorCode, string? message)
    {
        return new UnitRootResult(false, null, errorCode, message);
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
    /// Gets the unit paired with the stored numeric value.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    /// <exception cref="System.InvalidOperationException">The result represents a failure and therefore has no successful value.</exception>
    public UnitDefinition GetUnit()
    {
        if (!_success || _unit == null)
        {
            throw new System.InvalidOperationException("Failed root results do not carry a unit.");
        }

        return _unit;
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
