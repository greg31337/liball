namespace asn1;

/// <summary>
/// Exposes PER instruction registration through a composed registry implementation.
/// </summary>
public class PerInstructionRegistry : IPerInstructionRegistry
{
    internal IPerInstructionRegistry Implementation;

    /// <summary>
    /// Initializes a PER instruction registry facade over an operation implementation.
    /// </summary>
    /// <param name="implementation">The implementation that stores and resolves registrations.</param>
    public PerInstructionRegistry(IPerInstructionRegistry implementation)
    {
        this.Implementation = implementation == null ? throw new System.ArgumentNullException("implementation") : implementation;
    }

    /// <summary>
    /// Registers one instruction and handler pair.
    /// </summary>
    /// <param name="registration">The registration metadata.</param>
    /// <param name="handler">The application handler.</param>
    public void RegisterHandler(AsnPerInstructionRegistration registration, IPerInstructionHandler handler)
    {
        if (registration == null)
        {
            throw new System.ArgumentNullException("registration");
        }

        if (handler == null)
        {
            throw new System.ArgumentNullException("handler");
        }

        if (registration.Keyword == null || registration.Keyword.Length == 0)
        {
            throw new System.ArgumentException(
                "A PER instruction registration requires an identifying keyword.",
                "registration");
        }

        AsnPerInstructionRegistration[]? registrations =
            this.Implementation.GetRegistrations();
        if (registrations == null)
        {
            throw new System.InvalidOperationException(
                "The PER instruction registry returned no registration collection.");
        }

        int index = 0;
        while (index < registrations.Length)
        {
            AsnPerInstructionRegistration? existing = registrations[index];
            if (existing == null)
            {
                throw new System.InvalidOperationException(
                    "The PER instruction registry returned a null registration.");
            }

            if (existing.Keyword == registration.Keyword)
            {
                throw new System.ArgumentException(
                    "A PER instruction handler is already registered for keyword: "
                        + registration.Keyword,
                    "registration");
            }

            index = index + 1;
        }

        this.Implementation.RegisterHandler(registration, handler);
    }

    /// <summary>
    /// Resolves a handler by identifying keyword.
    /// </summary>
    /// <param name="keyword">The identifying keyword.</param>
    /// <returns>The handler, or null when no handler matches.</returns>
    public IPerInstructionHandler? ResolveHandler(string keyword)
    {
        if (keyword == null)
        {
            throw new System.ArgumentNullException("keyword");
        }

        if (keyword.Length == 0)
        {
            throw new System.ArgumentException(
                "A PER instruction lookup requires an identifying keyword.",
                "keyword");
        }

        return this.Implementation.ResolveHandler(keyword);
    }

    /// <summary>
    /// Returns registration metadata in deterministic keyword order.
    /// </summary>
    /// <returns>The registration metadata.</returns>
    public AsnPerInstructionRegistration[] GetRegistrations()
    {
        AsnPerInstructionRegistration[]? registrations =
            this.Implementation.GetRegistrations();
        if (registrations == null)
        {
            throw new System.InvalidOperationException(
                "The PER instruction registry returned no registration collection.");
        }

        int index = 0;
        while (index < registrations.Length)
        {
            if (registrations[index] == null)
            {
                throw new System.InvalidOperationException(
                    "The PER instruction registry returned a null registration.");
            }

            index = index + 1;
        }

        return registrations;
    }
}
