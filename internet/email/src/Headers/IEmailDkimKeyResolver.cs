using System;
using System.Collections.Generic;

namespace email;

internal class EmailDkimVerificationKeyListResolver : IEmailDkimKeyResolver
{
    List<EmailDkimVerificationKey> keys;

    internal EmailDkimVerificationKeyListResolver(List<EmailDkimVerificationKey> keys)
    {
        if (keys == null) throw new ArgumentNullException("keys");
        int index = 0;
        while (index < keys.Count)
        {
            if (keys[index] == null) throw new ArgumentException("A verification key list cannot contain null entries.", "keys");
            index++;
        }

        this.keys = keys;
    }

    public EmailDkimVerificationKey? Resolve(string domain, string selector)
    {
        int index = 0;
        while (index < keys.Count)
        {
            EmailDkimVerificationKey candidate = keys[index];
            if (string.Equals(candidate.GetDomain(), domain, StringComparison.OrdinalIgnoreCase) && string.Equals(candidate.GetSelector(), selector, StringComparison.OrdinalIgnoreCase)) return candidate;
            index++;
        }

        return null;
    }
}

internal static class EmailDkimKeyResolution
{
    internal static EmailDkimVerificationKey? Resolve(IEmailDkimKeyResolver resolver, string domain, string selector)
    {
        EmailDkimVerificationKey? binding = resolver.Resolve(domain, selector);
        if (binding == null) return null;
        if (!string.Equals(binding.GetDomain(), domain, StringComparison.OrdinalIgnoreCase) || !string.Equals(binding.GetSelector(), selector, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The key resolver returned a binding that does not match the requested signing domain and selector.");
        }

        return binding;
    }
}
