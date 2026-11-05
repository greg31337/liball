namespace units;

/// <summary>
/// Describes the ordinary formula or explicit special strategy used to convert between two units.
/// </summary>
public class ConversionPlan : IConversionPlan
{
    internal UnitDefinition _fromUnit;
    internal UnitDefinition _toUnit;
    internal UnitConversionKind _conversionKind;
    internal RationalNumber _valueScale;
    internal RationalNumber _valueOffset;
    internal bool _supported;
    internal UnitErrorCode _errorCode;
    internal string _message;
    internal SpecialConversionDefinition? _specialConversion;

    internal ConversionPlan(UnitDefinition fromUnit, UnitDefinition toUnit, UnitConversionKind conversionKind, RationalNumber valueScale, RationalNumber valueOffset, bool supported, UnitErrorCode errorCode, string? message, SpecialConversionDefinition? specialConversion)
    {
        ValidateRequiredValue(fromUnit, "fromUnit");
        ValidateRequiredValue(toUnit, "toUnit");
        ValidateRequiredValue(valueScale, "valueScale");
        ValidateRequiredValue(valueOffset, "valueOffset");

        if (!UnitDefinition.IsValidConversionKind(conversionKind))
        {
            throw new System.ArgumentException("Conversion plan kind is not supported.");
        }

        if (supported && errorCode != UnitErrorCode.None)
        {
            throw new System.ArgumentException("Supported conversion plans must use UnitErrorCode.None.");
        }

        if (!supported && errorCode == UnitErrorCode.None)
        {
            throw new System.ArgumentException("Unsupported conversion plans must use a non-success error code.");
        }

        if (supported && conversionKind == UnitConversionKind.Special && specialConversion == null)
        {
            throw new System.ArgumentNullException("specialConversion");
        }

        if (conversionKind != UnitConversionKind.Special && specialConversion != null)
        {
            throw new System.ArgumentException("Only special conversion plans may carry special conversion metadata.");
        }

        if (!supported && specialConversion != null)
        {
            throw new System.ArgumentException("Unsupported conversion plans must not carry executable special conversion metadata.");
        }

        if (supported && conversionKind == UnitConversionKind.Unsupported)
        {
            throw new System.ArgumentException("Supported conversion plans must use an executable conversion kind.");
        }

        if (supported && valueScale.IsZero())
        {
            throw new System.ArgumentException("Supported conversion plans must use a nonzero value scale.");
        }

        if (supported && conversionKind == UnitConversionKind.Scale && !valueOffset.IsZero())
        {
            throw new System.ArgumentException("Scale conversion plans must use a zero value offset.");
        }

        _fromUnit = fromUnit;
        _toUnit = toUnit;
        _conversionKind = conversionKind;
        _valueScale = valueScale;
        _valueOffset = valueOffset;
        _supported = supported;
        _errorCode = errorCode;
        if (message == null)
        {
            _message = "";
        }
        else
        {
            _message = message;
        }

        _specialConversion = specialConversion;
    }

    internal static ConversionPlan Create(UnitDefinition fromUnit, UnitDefinition toUnit)
    {
        ValidateRequiredValue(fromUnit, "fromUnit");
        ValidateRequiredValue(toUnit, "toUnit");

        if (!fromUnit.IsCompatibleWith(toUnit))
        {
            return CreateUnsupported(fromUnit, toUnit, UnitConversionKind.Unsupported, UnitErrorCode.ConversionIncompatibleDimensions, "Units have incompatible dimensions.");
        }

        if (fromUnit.GetConversionKind() == UnitConversionKind.Unsupported || toUnit.GetConversionKind() == UnitConversionKind.Unsupported)
        {
            return CreateUnsupported(fromUnit, toUnit, UnitConversionKind.Unsupported, UnitErrorCode.ConversionUnsupported, "Unit conversion kind is unsupported.");
        }

        if (fromUnit.GetConversionKind() == UnitConversionKind.Special || toUnit.GetConversionKind() == UnitConversionKind.Special)
        {
            return CreateUnsupported(fromUnit, toUnit, UnitConversionKind.Special, UnitErrorCode.ConversionUnsupported, "Special conversions require an explicit converter.");
        }

        if (IsTemperatureDimension(fromUnit.GetDimension()) && IsAffineUnit(fromUnit) != IsAffineUnit(toUnit))
        {
            return CreateUnsupported(fromUnit, toUnit, UnitConversionKind.Affine, UnitErrorCode.ConversionInvalidAffineOperation, "Temperature points and temperature intervals cannot be mixed.");
        }

        UnitConversionKind conversionKind = UnitConversionKind.Scale;
        if (fromUnit.GetConversionKind() == UnitConversionKind.Affine || toUnit.GetConversionKind() == UnitConversionKind.Affine || !fromUnit.GetOffsetToSystemUnit().IsZero() || !toUnit.GetOffsetToSystemUnit().IsZero())
        {
            conversionKind = UnitConversionKind.Affine;
        }

        RationalNumber valueScale = fromUnit.GetScaleToSystemUnit().Divide(toUnit.GetScaleToSystemUnit());
        RationalNumber valueOffset = fromUnit.GetOffsetToSystemUnit().Subtract(toUnit.GetOffsetToSystemUnit()).Divide(toUnit.GetScaleToSystemUnit());

        return new ConversionPlan(fromUnit, toUnit, conversionKind, valueScale, valueOffset, true, UnitErrorCode.None, "", null);
    }

    internal static ConversionPlan CreateSpecial(UnitDefinition fromUnit, UnitDefinition toUnit, SpecialConversionDefinition specialConversion)
    {
        ValidateRequiredValue(fromUnit, "fromUnit");
        ValidateRequiredValue(toUnit, "toUnit");
        ValidateRequiredValue(specialConversion, "specialConversion");

        if (!fromUnit.IsCompatibleWith(toUnit))
        {
            return CreateUnsupported(fromUnit, toUnit, UnitConversionKind.Unsupported, UnitErrorCode.ConversionIncompatibleDimensions, "Special conversions require units with compatible dimensions.");
        }

        bool fromMatches = specialConversion.MatchesUnit(fromUnit);
        bool toMatches = specialConversion.MatchesUnit(toUnit);
        if (!fromMatches && !toMatches)
        {
            return CreateUnsupported(fromUnit, toUnit, UnitConversionKind.Special, UnitErrorCode.ConversionUnsupported, "Special conversion metadata does not match either unit.");
        }

        if ((fromMatches && !CanUseSpecialUnit(fromUnit)) || (toMatches && !CanUseSpecialUnit(toUnit)))
        {
            return CreateUnsupported(fromUnit, toUnit, UnitConversionKind.Special, UnitErrorCode.ConversionUnsupported, "Special units must use neutral scale and offset metadata.");
        }

        if ((!fromMatches && !CanUseOrdinarySpecialPeer(fromUnit)) || (!toMatches && !CanUseOrdinarySpecialPeer(toUnit)))
        {
            return CreateUnsupported(fromUnit, toUnit, UnitConversionKind.Special, UnitErrorCode.ConversionUnsupported, "Special conversions require a scale-only ordinary peer unit.");
        }

        return new ConversionPlan(fromUnit, toUnit, UnitConversionKind.Special, new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitErrorCode.None, "", specialConversion);
    }

    /// <summary>
    /// Gets the from unit.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition GetFromUnit()
    {
        return _fromUnit;
    }

    /// <summary>
    /// Gets the to unit.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition GetToUnit()
    {
        return _toUnit;
    }

    /// <summary>
    /// Gets the conversion kind.
    /// </summary>
    /// <returns>The resulting unit conversion kind.</returns>
    public UnitConversionKind GetConversionKind()
    {
        return _conversionKind;
    }

    /// <summary>
    /// Gets the value scale.
    /// </summary>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber GetValueScale()
    {
        return _valueScale;
    }

    /// <summary>
    /// Gets the value offset.
    /// </summary>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber GetValueOffset()
    {
        return _valueOffset;
    }

    /// <summary>
    /// Reports whether the conversion plan can be executed.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsSupported()
    {
        return _supported;
    }

    /// <summary>
    /// Reports whether the conversion plan represents an unsupported conversion.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsUnsupported()
    {
        return !_supported;
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

    /// <summary>
    /// Reports whether the plan carries explicit special conversion metadata.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool HasSpecialConversion()
    {
        return _specialConversion != null;
    }

    /// <summary>
    /// Gets the explicit special conversion metadata.
    /// </summary>
    /// <returns>The resulting special conversion definition.</returns>
    /// <exception cref="System.InvalidOperationException">The plan does not carry special conversion metadata.</exception>
    public SpecialConversionDefinition GetSpecialConversion()
    {
        if (_specialConversion == null)
        {
            throw new System.InvalidOperationException("Conversion plan does not carry special conversion metadata.");
        }

        return _specialConversion;
    }

    internal static ConversionPlan CreateUnsupported(UnitDefinition fromUnit, UnitDefinition toUnit, UnitConversionKind conversionKind, UnitErrorCode errorCode, string message)
    {
        return new ConversionPlan(
            fromUnit,
            toUnit,
            conversionKind,
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            false,
            errorCode,
            message,
            null);
    }

    internal static bool CanUseSpecialUnit(UnitDefinition unit)
    {
        return unit.GetConversionKind() == UnitConversionKind.Special
            && unit.GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1))
            && unit.GetOffsetToSystemUnit().IsZero();
    }

    internal static bool CanUseOrdinarySpecialPeer(UnitDefinition unit)
    {
        return unit.GetConversionKind() == UnitConversionKind.Scale && unit.GetOffsetToSystemUnit().IsZero();
    }

    internal static bool IsAffineUnit(UnitDefinition unit)
    {
        return unit.GetConversionKind() == UnitConversionKind.Affine || !unit.GetOffsetToSystemUnit().IsZero();
    }

    internal static bool IsTemperatureDimension(DimensionVector dimension)
    {
        return dimension.GetLengthExponent() == 0
            && dimension.GetMassExponent() == 0
            && dimension.GetTimeExponent() == 0
            && dimension.GetCurrentExponent() == 0
            && dimension.GetTemperatureExponent() == 1
            && dimension.GetAmountExponent() == 0
            && dimension.GetLuminousIntensityExponent() == 0
            && dimension.GetAngleExponent() == 0
            && dimension.GetSolidAngleExponent() == 0
            && dimension.GetInformationExponent() == 0
            && dimension.GetCurrencyExponent() == 0
            && dimension.GetCountExponent() == 0;
    }

    internal static void ValidateRequiredValue(object value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }
}
