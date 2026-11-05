namespace barcode;

/// <summary>
/// Assembles decoded Data Matrix or Macro PDF417 structured-append segments.
/// </summary>
public class StructuredAppendAssembler : IStructuredAppendAssembler
{
    internal BarcodeDispatcher Dispatcher;

    /// <summary>
    /// Creates a structured-append assembler backed by the shared operation dispatcher.
    /// </summary>
    public StructuredAppendAssembler()
    {
        Dispatcher = new BarcodeDispatcher();
    }

    /// <summary>
    /// Assembles decoded structured-append segments in sequence order.
    /// </summary>
    /// <param name="request">The decoded segments and assembly policy.</param>
    /// <returns>The complete or partial assembly result.</returns>
    /// <exception cref="BarcodeValidationException">Assembly is unavailable or cannot be performed.</exception>
    public StructuredAppendResult AssembleStructuredAppend(StructuredAppendRequest request)
    {
        return Dispatcher.AssembleStructuredAppend(request);
    }
}
