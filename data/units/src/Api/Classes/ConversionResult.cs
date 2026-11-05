namespace units;

/// <summary>
/// Carries either a converted value and unit or a conversion error.
/// </summary>
public class ConversionResult : IConversionResult
{
    internal bool _success;
    internal double _value;
    internal UnitDefinition? _unit;
    internal UnitErrorCode _errorCode;
    internal string _message;

    internal ConversionResult(bool success, double value, UnitDefinition? unit, UnitErrorCode errorCode, string? message)
    {
        if (success && unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (success && errorCode != UnitErrorCode.None)
        {
            throw new System.ArgumentException("Successful conversion results must use UnitErrorCode.None.");
        }

        if (!success && errorCode == UnitErrorCode.None)
        {
            throw new System.ArgumentException("Failed conversion results must use a non-success error code.");
        }

        if (!success && unit != null)
        {
            throw new System.ArgumentException("Failed conversion results must not carry a unit.");
        }

        _success = success;
        _value = value;
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

    internal static ConversionResult CreateSuccess(double value, UnitDefinition unit)
    {
        return new ConversionResult(true, value, unit, UnitErrorCode.None, "");
    }

    internal static ConversionResult CreateFailure(UnitErrorCode errorCode, string? message)
    {
        return new ConversionResult(false, 0.0d, null, errorCode, message);
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
    /// Gets the numeric value expressed in the stored unit.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    /// <exception cref="System.InvalidOperationException">The result represents a failure and therefore has no successful value.</exception>
    public double GetValue()
    {
        if (!_success)
        {
            throw new System.InvalidOperationException("Failed conversion results do not carry a value.");
        }

        return _value;
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
            throw new System.InvalidOperationException("Failed conversion results do not carry a unit.");
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
