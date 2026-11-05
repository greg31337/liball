namespace cbor;

/// <summary>
/// Stores semantic handlers by unsigned CBOR tag number.
/// </summary>
public interface ICborTagRegistry
{
    /// <summary>Adds a semantic tag registration.</summary>
    /// <param name="registration">The registration to add.</param>
    void AddTag(CborTagRegistration registration);

    /// <summary>Finds a semantic tag registration.</summary>
    /// <param name="tagNumber">The unsigned tag number to find.</param>
    /// <returns>The matching custom or built-in registration, or null when the tag is not registered.</returns>
    CborTagRegistration? FindTag(ulong tagNumber);
}
