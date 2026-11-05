namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="QuantityMath"/>.
/// </summary>
public interface IQuantityMath
{
    /// <summary>
    /// Adds compatible quantities after converting the right operand to the left operand's unit.
    /// </summary>
    /// <param name="left">The left quantity operand.</param>
    /// <param name="right">The right quantity operand.</param>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue Add(QuantityValue left, QuantityValue right);
    /// <summary>
    /// Subtracts compatible quantities after converting the right operand to the left operand's unit.
    /// </summary>
    /// <param name="left">The left quantity operand.</param>
    /// <param name="right">The right quantity operand.</param>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue Subtract(QuantityValue left, QuantityValue right);
    /// <summary>
    /// Adds compatible quantities and expresses the result in an explicit target unit.
    /// </summary>
    /// <param name="left">The left quantity operand.</param>
    /// <param name="right">The right quantity operand.</param>
    /// <param name="targetUnit">The unit in which to express the result.</param>
    /// <returns>A result containing the quantity or a typed arithmetic failure.</returns>
    public QuantityArithmeticResult AddToUnit(QuantityValue left, QuantityValue right, UnitDefinition targetUnit);
    /// <summary>
    /// Subtracts compatible quantities and expresses the result in an explicit target unit.
    /// </summary>
    /// <param name="left">The left quantity operand.</param>
    /// <param name="right">The right quantity operand.</param>
    /// <param name="targetUnit">The unit in which to express the result.</param>
    /// <returns>A result containing the quantity or a typed arithmetic failure.</returns>
    public QuantityArithmeticResult SubtractToUnit(QuantityValue left, QuantityValue right, UnitDefinition targetUnit);
    /// <summary>
    /// Multiplies the supplied quantities and combines their unit dimensions.
    /// </summary>
    /// <param name="left">The left quantity operand.</param>
    /// <param name="right">The right quantity operand.</param>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue Multiply(QuantityValue left, QuantityValue right);
    /// <summary>
    /// Divides the supplied quantities and subtracts the divisor's unit dimensions.
    /// </summary>
    /// <param name="left">The left quantity operand.</param>
    /// <param name="right">The right quantity operand.</param>
    /// <returns>The resulting quantity value.</returns>
    /// <exception cref="System.DivideByZeroException">The right quantity has a zero numeric value.</exception>
    public QuantityValue Divide(QuantityValue left, QuantityValue right);
    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="value">The quantity to scale.</param>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue Scale(QuantityValue value, double factor);
    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <param name="value">The quantity to negate.</param>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue Negate(QuantityValue value);
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <param name="value">The quantity whose absolute value is returned.</param>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue AbsoluteValue(QuantityValue value);
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <param name="value">The quantity whose absolute value is returned.</param>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue Abs(QuantityValue value);
    /// <summary>
    /// Raises the value and its unit dimensions to the supplied integer power.
    /// </summary>
    /// <param name="value">The quantity to raise to a power.</param>
    /// <param name="power">The integer exponent to apply.</param>
    /// <returns>The resulting quantity value.</returns>
    /// <exception cref="System.DivideByZeroException">The quantity has a zero numeric value and the power is negative.</exception>
    public QuantityValue Pow(QuantityValue value, int power);
    /// <summary>
    /// Attempts to take an exact integer root of the value and unit dimensions.
    /// </summary>
    /// <param name="value">The quantity whose root is requested.</param>
    /// <param name="root">The positive integer root to calculate.</param>
    /// <returns>A result that describes success or failure.</returns>
    public QuantityRootResult Root(QuantityValue value, int root);
}
