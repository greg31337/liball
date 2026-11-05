using System.Collections.Generic;

namespace cbor;

internal class CborTagLookup
{
    internal List<CborTagRegistration> Registrations = new List<CborTagRegistration>();
    internal string? ErrorCode;
    internal string? ErrorMessage;

    internal CborTagLookup(List<CborTagRegistration>? registrations)
    {
        if (registrations == null)
        {
            ErrorCode = "CBOR_TAG_REGISTRATIONS_MISSING";
            ErrorMessage = "The tag-registration collection is required.";
            return;
        }
        int index = 0;
        while (index < registrations.Count)
        {
            CborTagRegistration? registration = registrations[index];
            if (registration == null)
            {
                ErrorCode = "CBOR_TAG_REGISTRATION_MISSING";
                ErrorMessage = "A tag registration is required.";
                return;
            }
            if (FindCustomTag(registration.TagNumber) != null)
            {
                ErrorCode = "CBOR_TAG_REGISTRATION_DUPLICATE";
                ErrorMessage = $"Semantic tag {registration.TagNumber} is registered more than once.";
                return;
            }
            CborTagRegistration snapshot = new CborTagRegistration();
            snapshot.TagNumber = registration.TagNumber;
            snapshot.Name = registration.Name;
            snapshot.Handler = registration.Handler;
            snapshot.ValidateContent = registration.ValidateContent;
            Registrations.Add(snapshot);
            index = index + 1;
        }
    }

    internal CborTagRegistration? FindTag(ulong tagNumber)
    {
        return FindCustomTag(tagNumber);
    }

    internal CborTagRegistration? FindCustomTag(ulong tagNumber)
    {
        int index = 0;
        while (index < Registrations.Count)
        {
            CborTagRegistration registration = Registrations[index];
            if (registration.TagNumber == tagNumber)
            {
                return registration;
            }
            index = index + 1;
        }
        return null;
    }
}
