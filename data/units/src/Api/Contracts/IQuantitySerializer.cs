namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="QuantitySerializer"/>.
/// </summary>
public interface IQuantitySerializer
{
    /// <summary>
    /// Serializes the quantity as culture-invariant canonical text.
    /// </summary>
    /// <param name="quantity">The quantity to use.</param>
    /// <returns>The canonical serialized text.</returns>
    public string SerializeCanonical(QuantityValue quantity);
    /// <summary>
    /// Parses culture-invariant canonical text into a quantity.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue DeserializeCanonical(string text, IUnitCatalog registry);
    /// <summary>
    /// Parses culture-invariant canonical text without throwing for expected deserialization failures.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>A result containing the quantity or structured failure details.</returns>
    public QuantityParseResult DeserializeCanonicalResult(string text, IUnitCatalog registry);
    /// <summary>
    /// Creates a data-transfer object from the supplied quantity.
    /// </summary>
    /// <param name="quantity">The quantity to use.</param>
    /// <returns>The resulting quantity dto.</returns>
    public QuantityDto ToDto(QuantityValue quantity);
    /// <summary>
    /// Creates a data-transfer object from the supplied quantity.
    /// </summary>
    /// <param name="quantity">The quantity to use.</param>
    /// <param name="quantityKind">The quantity kind used to validate or narrow the operation.</param>
    /// <returns>The resulting quantity dto.</returns>
    public QuantityDto ToDtoWithQuantityKind(QuantityValue quantity, string? quantityKind);
    /// <summary>
    /// Creates a quantity from the supplied data-transfer object.
    /// </summary>
    /// <param name="dto">The data-transfer object to read.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue FromDto(QuantityDto dto, IUnitCatalog registry);
    /// <summary>
    /// Creates a quantity from a data-transfer object without throwing for expected deserialization failures.
    /// </summary>
    /// <param name="dto">The data-transfer object to read.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>A result containing the quantity or structured failure details.</returns>
    public QuantityConstructionResult FromDtoResult(QuantityDto dto, IUnitCatalog registry);
}
