namespace barcode;

/// <summary>
/// Assembles decoded Data Matrix or Macro PDF417 structured-append segments.
/// </summary>
public interface IStructuredAppendAssembler
{
    /// <summary>
    /// Assembles decoded structured-append segments in sequence order.
    /// </summary>
    /// <param name="request">The decoded segments and assembly policy.</param>
    /// <returns>The complete or partial assembly result.</returns>
    StructuredAppendResult AssembleStructuredAppend(StructuredAppendRequest request);
}
