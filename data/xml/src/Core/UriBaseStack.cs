using System;
using System.Collections.Generic;

namespace xml;

internal class UriBaseStack
{
    internal List<Uri> Bases;

    internal UriBaseStack(Uri initialBaseUri)
    {
        Guard.NotNull(initialBaseUri, "initialBaseUri");
        if (!initialBaseUri.IsAbsoluteUri)
        {
            throw new ArgumentException("The initial base URI must be absolute.", "initialBaseUri");
        }

        if (UriTools.HasFragment(initialBaseUri.OriginalString))
        {
            throw new UriFormatException("A base URI must not contain a fragment identifier.");
        }

        Bases = new List<Uri>();
        Bases.Add(UriTools.CreateAbsolute(UriTools.CanonicalAbsoluteUri(initialBaseUri)));
    }

    internal Uri CurrentBaseUri()
    {
        return Bases[Bases.Count - 1];
    }

    internal Uri PushBaseReference(string reference)
    {
        Uri resolved = UriTools.ResolveReferenceWithFragmentPolicy(CurrentBaseUri(), reference, UriFragmentPolicy.Reject);
        Bases.Add(resolved);
        return resolved;
    }

    internal Uri PopBaseUri()
    {
        if (Bases.Count <= 1)
        {
            throw new InvalidOperationException("The initial base URI cannot be removed.");
        }

        int index = Bases.Count - 1;
        Uri removed = Bases[index];
        Bases.RemoveAt(index);
        return removed;
    }

    internal int BaseUriDepth()
    {
        return Bases.Count;
    }

    internal void ResetBaseUris()
    {
        while (Bases.Count > 1)
        {
            Bases.RemoveAt(Bases.Count - 1);
        }
    }
}
