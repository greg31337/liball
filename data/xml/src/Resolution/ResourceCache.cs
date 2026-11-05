using System.Collections.Generic;

namespace xml;

internal class ResourceCache
{
    internal Dictionary<string, XmlResolvedResource> Resources;

    internal ResourceCache()
    {
        Resources = new Dictionary<string, XmlResolvedResource>(System.StringComparer.Ordinal);
    }

    internal XmlResolvedResource? FindXmlResource(string key)
    {
        Guard.NotNull(key, "key");
        if (!Resources.ContainsKey(key))
        {
            return null;
        }

        return CopyCachedXmlResource(Resources[key]);
    }

    internal void StoreXmlResource(string key, XmlResolvedResource resource)
    {
        Guard.NotNull(key, "key");
        Guard.NotNull(resource, "resource");
        XmlInput? input = resource.Input;
        if (resource.Access != XmlResourceAccess.Allowed || input == null || !CanCacheXmlInput(input))
        {
            return;
        }

        XmlResolvedResource snapshot = CopyCachedXmlResource(resource);
        if (Resources.ContainsKey(key))
        {
            Resources[key] = snapshot;
        }
        else
        {
            Resources.Add(key, snapshot);
        }
    }

    internal void ClearXmlResources()
    {
        Resources.Clear();
    }

    internal bool CanCacheXmlInput(XmlInput input)
    {
        return input.Kind == XmlInputKind.String || input.Kind == XmlInputKind.CharacterMemory || input.Kind == XmlInputKind.ByteMemory;
    }

    internal XmlResolvedResource CopyCachedXmlResource(XmlResolvedResource resource)
    {
        XmlResolvedResource copy = new XmlResolvedResource();
        copy.Kind = resource.Kind;
        copy.Access = resource.Access;
        copy.Input = resource.Input == null ? null : CopyCachedXmlInput(resource.Input);
        copy.RequestedUri = resource.RequestedUri;
        copy.AbsoluteUri = resource.AbsoluteUri;
        copy.PublicIdentifier = resource.PublicIdentifier;
        copy.SystemIdentifier = resource.SystemIdentifier;
        copy.MediaType = resource.MediaType;
        copy.EncodingName = resource.EncodingName;
        copy.OwnsResource = false;
        return copy;
    }

    internal XmlInput CopyCachedXmlInput(XmlInput input)
    {
        XmlInput copy = new XmlInput();
        copy.Kind = input.Kind;
        copy.Text = input.Text;
        copy.Characters = input.Kind == XmlInputKind.CharacterMemory ? input.Characters.ToArray() : input.Characters;
        copy.Bytes = input.Kind == XmlInputKind.ByteMemory ? input.Bytes.ToArray() : input.Bytes;
        copy.BaseUri = input.BaseUri;
        copy.MediaType = input.MediaType;
        copy.EncodingName = input.EncodingName;
        copy.Encoding = input.Encoding;
        copy.OwnsResource = false;
        return copy;
    }
}
