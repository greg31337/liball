namespace asn1;

/// <summary>
/// Registers and resolves application-provided PER instruction handlers.
/// </summary>
public interface IPerInstructionRegistry
{
    /// <summary>
    /// Registers one instruction and handler pair.
    /// </summary>
    /// <param name="registration">The immutable registration metadata.</param>
    /// <param name="handler">The application handler.</param>
    public void RegisterHandler(AsnPerInstructionRegistration registration, IPerInstructionHandler handler);

    /// <summary>
    /// Resolves a handler by identifying keyword.
    /// </summary>
    /// <param name="keyword">The identifying keyword.</param>
    /// <returns>The handler, or null when the keyword is not registered.</returns>
    public IPerInstructionHandler? ResolveHandler(string keyword);

    /// <summary>
    /// Returns registration metadata in deterministic keyword order.
    /// </summary>
    /// <returns>The registration metadata.</returns>
    public AsnPerInstructionRegistration[] GetRegistrations();
}
