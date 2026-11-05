namespace svg;

internal static class SvgReferenceUri
{
    internal static string Resolve(string baseUri, string reference)
    {
        SvgUriResult result = new SvgUri().ResolveWithBaseUriAndReferenceAndMaxCharacters(baseUri, reference, 268435455);
        return result.Success ? result.NormalizedIri : reference;
    }

    internal static string RemoveFragment(string value)
    {
        int fragment = value.IndexOf('#');
        return fragment < 0 ? value : value.Substring(0, fragment);
    }

    internal static bool Valid(string? value)
    {
        if (value == null || value.Length == 0) return true;
        return new SvgUri().NormalizeWithValueAndMaxCharacters(value, 268435455).Success;
    }

}
