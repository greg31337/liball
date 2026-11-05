namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="ConversionPlan"/>.
/// </summary>
public interface IConversionPlan
{
    /// <summary>
    /// Gets the from unit.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition GetFromUnit();
    /// <summary>
    /// Gets the to unit.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition GetToUnit();
    /// <summary>
    /// Gets the conversion kind.
    /// </summary>
    /// <returns>The resulting unit conversion kind.</returns>
    public UnitConversionKind GetConversionKind();
    /// <summary>
    /// Gets the value scale.
    /// </summary>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber GetValueScale();
    /// <summary>
    /// Gets the value offset.
    /// </summary>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber GetValueOffset();
    /// <summary>
    /// Reports whether the conversion plan can be executed.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsSupported();
    /// <summary>
    /// Reports whether the conversion plan represents an unsupported conversion.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsUnsupported();
    /// <summary>
    /// Gets the error code.
    /// </summary>
    /// <returns>The resulting unit error code.</returns>
    public UnitErrorCode GetErrorCode();
    /// <summary>
    /// Gets the developer-readable result message, which is empty for success.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetMessage();
    /// <summary>
    /// Reports whether the plan carries explicit special conversion metadata.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool HasSpecialConversion();
    /// <summary>
    /// Gets the explicit special conversion metadata.
    /// </summary>
    /// <returns>The resulting special conversion definition.</returns>
    /// <exception cref="System.InvalidOperationException">The plan does not carry special conversion metadata.</exception>
    public SpecialConversionDefinition GetSpecialConversion();
}
