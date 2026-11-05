using System.Collections.Generic;

namespace cbor;

/// <summary>
/// Stores semantic handlers by unsigned CBOR tag number.
/// </summary>
public class CborTagRegistry : ICborTagRegistry
{
    internal List<CborTagRegistration> Registrations = new List<CborTagRegistration>();
    internal object Sync = new object();

    /// <summary>Creates a semantic tag registry with standard built-in fallback handlers.</summary>
    public CborTagRegistry()
    {
    }

    /// <summary>Adds a semantic tag registration.</summary>
    /// <param name="registration">The registration to add.</param>
    public void AddTag(CborTagRegistration registration)
    {
        if (registration == null)
        {
            throw new System.ArgumentNullException("registration");
        }
        lock (Sync)
        {
            int index = 0;
            while (index < Registrations.Count)
            {
                if (Registrations[index].TagNumber == registration.TagNumber)
                {
                    throw new System.ArgumentException(
                        $"Semantic tag {registration.TagNumber} is already registered.",
                        "registration"
                    );
                }
                index = index + 1;
            }
            Registrations.Add(CopyRegistration(registration));
        }
    }

    /// <summary>Finds a semantic tag registration.</summary>
    /// <param name="tagNumber">The unsigned tag number to find.</param>
    /// <returns>The matching custom or built-in registration, or null when the tag is not registered.</returns>
    public CborTagRegistration? FindTag(ulong tagNumber)
    {
        lock (Sync)
        {
            int index = 0;
            while (index < Registrations.Count)
            {
                CborTagRegistration registration = Registrations[index];
                if (registration.TagNumber == tagNumber)
                {
                    return CopyRegistration(registration);
                }
                index = index + 1;
            }
        }
        CborTagRegistration? builtIn = CborBuiltInTags.FindRegistration(tagNumber);
        if (builtIn == null)
        {
            return null;
        }
        return CopyRegistration(builtIn);
    }

    internal CborTagRegistration CopyRegistration(CborTagRegistration registration)
    {
        CborTagRegistration copy = new CborTagRegistration();
        copy.TagNumber = registration.TagNumber;
        copy.Name = registration.Name;
        copy.Handler = registration.Handler;
        copy.ValidateContent = registration.ValidateContent;
        return copy;
    }
}
