namespace units;

/// <summary>
/// Provides dimension-aware arithmetic for dynamic quantity values.
/// </summary>
/// <remarks>
/// Addition and subtraction preserve the left unit by default. Explicit-target operations return typed failures and use the target unit to distinguish affine points from intervals. Multiplication and division create compound units.
/// </remarks>
public class QuantityMath : IQuantityMath
{
    /// <summary>
    /// Initializes a quantity arithmetic service.
    /// </summary>
    public QuantityMath()
    {
    }

    /// <summary>
    /// Adds compatible quantities after converting the right operand to the left operand's unit.
    /// </summary>
    /// <param name="left">The left quantity operand.</param>
    /// <param name="right">The right quantity operand.</param>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue Add(QuantityValue left, QuantityValue right)
    {
        ValidateRequiredValue(left, "left");
        ValidateRequiredValue(right, "right");
        RequireNonAffineQuantity(left, "addition");
        RequireNonAffineQuantity(right, "addition");

        QuantityValue convertedRight = ConvertRightToLeftUnit(left, right);
        return new QuantityValue(left.GetValue() + convertedRight.GetValue(), left.GetUnit());
    }

    /// <summary>
    /// Subtracts compatible quantities after converting the right operand to the left operand's unit.
    /// </summary>
    /// <param name="left">The left quantity operand.</param>
    /// <param name="right">The right quantity operand.</param>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue Subtract(QuantityValue left, QuantityValue right)
    {
        ValidateRequiredValue(left, "left");
        ValidateRequiredValue(right, "right");
        RequireNonAffineQuantity(left, "subtraction");
        RequireNonAffineQuantity(right, "subtraction");

        QuantityValue convertedRight = ConvertRightToLeftUnit(left, right);
        return new QuantityValue(left.GetValue() - convertedRight.GetValue(), left.GetUnit());
    }

    /// <summary>
    /// Adds compatible quantities and expresses the result in an explicit target unit.
    /// </summary>
    /// <param name="left">The left quantity operand.</param>
    /// <param name="right">The right quantity operand.</param>
    /// <param name="targetUnit">The unit in which to express the result.</param>
    /// <returns>A result containing the quantity or a typed arithmetic failure.</returns>
    public QuantityArithmeticResult AddToUnit(QuantityValue left, QuantityValue right, UnitDefinition targetUnit)
    {
        ValidateArithmeticInputs(left, right, targetUnit);

        QuantityArithmeticResult? validation = ValidateDimensionsAndKinds(left, right, targetUnit);
        if (validation != null)
        {
            return validation;
        }

        bool leftPoint = IsAffineQuantity(left);
        bool rightPoint = IsAffineQuantity(right);
        bool targetPoint = IsAffineUnit(targetUnit);
        if (leftPoint && rightPoint)
        {
            return QuantityArithmeticResult.CreateFailure(UnitErrorCode.ArithmeticInvalidOperation, "Adding two affine quantity points is not meaningful.");
        }

        bool resultPoint = leftPoint || rightPoint;
        if (resultPoint != targetPoint)
        {
            return QuantityArithmeticResult.CreateFailure(UnitErrorCode.ArithmeticInvalidOperation, "Addition target unit has the wrong affine point or interval kind.");
        }

        double systemValue = ToArithmeticSystemValue(left) + ToArithmeticSystemValue(right);
        return QuantityArithmeticResult.CreateSuccess(CreateFromArithmeticSystemValue(systemValue, targetUnit));
    }

    /// <summary>
    /// Subtracts compatible quantities and expresses the result in an explicit target unit.
    /// </summary>
    /// <param name="left">The left quantity operand.</param>
    /// <param name="right">The right quantity operand.</param>
    /// <param name="targetUnit">The unit in which to express the result.</param>
    /// <returns>A result containing the quantity or a typed arithmetic failure.</returns>
    public QuantityArithmeticResult SubtractToUnit(QuantityValue left, QuantityValue right, UnitDefinition targetUnit)
    {
        ValidateArithmeticInputs(left, right, targetUnit);

        QuantityArithmeticResult? validation = ValidateDimensionsAndKinds(left, right, targetUnit);
        if (validation != null)
        {
            return validation;
        }

        bool leftPoint = IsAffineQuantity(left);
        bool rightPoint = IsAffineQuantity(right);
        bool targetPoint = IsAffineUnit(targetUnit);
        if (!leftPoint && rightPoint)
        {
            return QuantityArithmeticResult.CreateFailure(UnitErrorCode.ArithmeticInvalidOperation, "An affine quantity point cannot be subtracted from an interval.");
        }

        bool resultPoint = leftPoint && !rightPoint;
        if (resultPoint != targetPoint)
        {
            return QuantityArithmeticResult.CreateFailure(UnitErrorCode.ArithmeticInvalidOperation, "Subtraction target unit has the wrong affine point or interval kind.");
        }

        double systemValue = ToArithmeticSystemValue(left) - ToArithmeticSystemValue(right);
        return QuantityArithmeticResult.CreateSuccess(CreateFromArithmeticSystemValue(systemValue, targetUnit));
    }

    /// <summary>
    /// Multiplies the supplied quantities and combines their unit dimensions.
    /// </summary>
    /// <param name="left">The left quantity operand.</param>
    /// <param name="right">The right quantity operand.</param>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue Multiply(QuantityValue left, QuantityValue right)
    {
        ValidateRequiredValue(left, "left");
        ValidateRequiredValue(right, "right");

        return new QuantityValue(left.GetValue() * right.GetValue(), left.GetUnit().Multiply(right.GetUnit()));
    }

    /// <summary>
    /// Divides the supplied quantities and subtracts the divisor's unit dimensions.
    /// </summary>
    /// <param name="left">The left quantity operand.</param>
    /// <param name="right">The right quantity operand.</param>
    /// <returns>The resulting quantity value.</returns>
    /// <exception cref="System.DivideByZeroException">The right quantity has a zero numeric value.</exception>
    public QuantityValue Divide(QuantityValue left, QuantityValue right)
    {
        ValidateRequiredValue(left, "left");
        ValidateRequiredValue(right, "right");

        UnitDefinition resultUnit = left.GetUnit().Divide(right.GetUnit());
        if (right.GetValue() == 0.0d)
        {
            throw new System.DivideByZeroException("Cannot divide by a quantity with a zero numeric value.");
        }

        return new QuantityValue(left.GetValue() / right.GetValue(), resultUnit);
    }

    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="value">The quantity to scale.</param>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue Scale(QuantityValue value, double factor)
    {
        ValidateRequiredValue(value, "value");
        RequireNonAffineQuantity(value, "scaling");

        return new QuantityValue(value.GetValue() * factor, value.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <param name="value">The quantity to negate.</param>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue Negate(QuantityValue value)
    {
        ValidateRequiredValue(value, "value");
        RequireNonAffineQuantity(value, "negation");

        return new QuantityValue(0.0d - value.GetValue(), value.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <param name="value">The quantity whose absolute value is returned.</param>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue AbsoluteValue(QuantityValue value)
    {
        ValidateRequiredValue(value, "value");
        RequireNonAffineQuantity(value, "absolute value");

        return new QuantityValue(System.Math.Abs(value.GetValue()), value.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <param name="value">The quantity whose absolute value is returned.</param>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue Abs(QuantityValue value)
    {
        return AbsoluteValue(value);
    }

    /// <summary>
    /// Raises the value and its unit dimensions to the supplied integer power.
    /// </summary>
    /// <param name="value">The quantity to raise to a power.</param>
    /// <param name="power">The integer exponent to apply.</param>
    /// <returns>The resulting quantity value.</returns>
    /// <exception cref="System.DivideByZeroException">The quantity has a zero numeric value and the power is negative.</exception>
    public QuantityValue Pow(QuantityValue value, int power)
    {
        ValidateRequiredValue(value, "value");
        RequireNonAffineQuantity(value, "power");

        UnitDefinition resultUnit = value.GetUnit().Pow(power);
        if (value.GetValue() == 0.0d && power < 0)
        {
            throw new System.DivideByZeroException("Cannot raise a zero quantity value to a negative power.");
        }

        return new QuantityValue(System.Math.Pow(value.GetValue(), power), resultUnit);
    }

    /// <summary>
    /// Attempts to take an exact integer root of the value and unit dimensions.
    /// </summary>
    /// <param name="value">The quantity whose root is requested.</param>
    /// <param name="root">The positive integer root to calculate.</param>
    /// <returns>A result that describes success or failure.</returns>
    public QuantityRootResult Root(QuantityValue value, int root)
    {
        ValidateRequiredValue(value, "value");

        if (IsAffineQuantity(value))
        {
            return QuantityRootResult.CreateFailure(UnitErrorCode.ArithmeticInvalidOperation, "Affine quantity points cannot be used in root arithmetic.");
        }

        if (root <= 0)
        {
            return QuantityRootResult.CreateFailure(UnitErrorCode.ArithmeticInvalidOperation, "Root must be greater than zero.");
        }

        if (value.GetValue() < 0.0d && root % 2 == 0)
        {
            return QuantityRootResult.CreateFailure(UnitErrorCode.ArithmeticInvalidOperation, "Quantity value must not be negative for an even root.");
        }

        UnitRootResult unitRoot = value.GetUnit().Root(root);
        if (unitRoot.IsFailure())
        {
            return QuantityRootResult.CreateFailure(unitRoot.GetErrorCode(), unitRoot.GetMessage());
        }

        return QuantityRootResult.CreateSuccess(new QuantityValue(RootValue(value.GetValue(), root), unitRoot.GetUnit()));
    }

    internal static QuantityValue ConvertRightToLeftUnit(QuantityValue left, QuantityValue right)
    {
        if (!left.IsCompatibleWith(right))
        {
            throw new System.InvalidOperationException("Quantities have incompatible dimensions.");
        }

        return right.ToUnit(left.GetUnit());
    }

    internal static void RequireNonAffineQuantity(QuantityValue value, string operation)
    {
        if (IsAffineQuantity(value))
        {
            throw new System.InvalidOperationException("Affine quantity points cannot be used in " + operation + ".");
        }
    }

    internal static bool IsAffineQuantity(QuantityValue value)
    {
        UnitDefinition unit = value.GetUnit();
        return unit.GetConversionKind() == UnitConversionKind.Affine || !unit.GetOffsetToSystemUnit().IsZero();
    }

    internal static bool IsAffineUnit(UnitDefinition unit)
    {
        return unit.GetConversionKind() == UnitConversionKind.Affine || !unit.GetOffsetToSystemUnit().IsZero();
    }

    internal static void ValidateArithmeticInputs(QuantityValue left, QuantityValue right, UnitDefinition targetUnit)
    {
        ValidateRequiredValue(left, "left");
        ValidateRequiredValue(right, "right");
        ValidateRequiredValue(targetUnit, "targetUnit");
    }

    internal static QuantityArithmeticResult? ValidateDimensionsAndKinds(QuantityValue left, QuantityValue right, UnitDefinition targetUnit)
    {
        if (!left.IsCompatibleWith(right) || !left.IsCompatibleWithUnit(targetUnit))
        {
            return QuantityArithmeticResult.CreateFailure(UnitErrorCode.ArithmeticIncompatibleDimensions, "Quantity operands and target unit must have compatible dimensions.");
        }

        if (!CanUseAdditiveUnit(left.GetUnit()) || !CanUseAdditiveUnit(right.GetUnit()) || !CanUseAdditiveUnit(targetUnit))
        {
            return QuantityArithmeticResult.CreateFailure(UnitErrorCode.ArithmeticInvalidOperation, "Addition and subtraction require scale or affine units.");
        }

        return null;
    }

    internal static bool CanUseAdditiveUnit(UnitDefinition unit)
    {
        return unit.GetConversionKind() == UnitConversionKind.Scale || unit.GetConversionKind() == UnitConversionKind.Affine;
    }

    internal static double ToArithmeticSystemValue(QuantityValue quantity)
    {
        UnitDefinition unit = quantity.GetUnit();
        double systemValue = quantity.GetValue() * unit.GetScaleToSystemUnit().ToDouble();
        if (IsAffineUnit(unit))
        {
            systemValue = systemValue + unit.GetOffsetToSystemUnit().ToDouble();
        }

        return systemValue;
    }

    internal static QuantityValue CreateFromArithmeticSystemValue(double systemValue, UnitDefinition targetUnit)
    {
        double adjustedValue = systemValue;
        if (IsAffineUnit(targetUnit))
        {
            adjustedValue = adjustedValue - targetUnit.GetOffsetToSystemUnit().ToDouble();
        }

        return new QuantityValue(adjustedValue / targetUnit.GetScaleToSystemUnit().ToDouble(), targetUnit);
    }

    internal static double RootValue(double value, int root)
    {
        if (value < 0.0d)
        {
            return 0.0d - System.Math.Pow(0.0d - value, 1.0d / (double)root);
        }

        return System.Math.Pow(value, 1.0d / (double)root);
    }

    internal static void ValidateRequiredValue(object value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }
}
