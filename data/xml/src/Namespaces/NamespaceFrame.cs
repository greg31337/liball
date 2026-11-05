using System;

namespace xml;

internal class NamespaceFrame
{
    string[] Prefixes;
    string[] NamespaceUris;
    bool[] Undeclarations;
    SourceRange?[] Ranges;

    internal NamespaceFrame(XmlNamespaceBinding[] declarations)
    {
        Guard.NotNull(declarations, "declarations");
        Prefixes = new string[declarations.Length];
        NamespaceUris = new string[declarations.Length];
        Undeclarations = new bool[declarations.Length];
        Ranges = new SourceRange?[declarations.Length];
        int index = 0;
        while (index < declarations.Length)
        {
            XmlNamespaceBinding? optionalBinding = declarations[index];
            if (optionalBinding == null)
            {
                throw new ArgumentException("A namespace declaration cannot be null.", "declarations");
            }

            string? optionalPrefix = optionalBinding.Prefix;
            if (optionalPrefix == null)
            {
                throw new ArgumentException("A namespace declaration requires a prefix.", "declarations");
            }

            string? optionalNamespaceUri = optionalBinding.NamespaceUri;
            if (optionalNamespaceUri == null)
            {
                throw new ArgumentException("A namespace declaration requires a namespace URI.", "declarations");
            }

            Prefixes[index] = optionalPrefix;
            NamespaceUris[index] = optionalNamespaceUri;
            Undeclarations[index] = optionalBinding.IsUndeclaration || optionalNamespaceUri.Length == 0;
            Ranges[index] = CopyXmlNamespaceRange(optionalBinding.Range);
            index = index + 1;
        }
    }

    internal XmlNamespaceBinding? LookupLocalXmlNamespace(string prefix)
    {
        Guard.NotNull(prefix, "prefix");
        int index = 0;
        while (index < Prefixes.Length)
        {
            if (string.Equals(Prefixes[index], prefix, StringComparison.Ordinal))
            {
                return CreateXmlNamespaceBindingAt(index);
            }

            index = index + 1;
        }

        return null;
    }

    internal XmlNamespaceBinding[] CopyLocalXmlNamespaces()
    {
        XmlNamespaceBinding[] declarations = new XmlNamespaceBinding[Prefixes.Length];
        int index = 0;
        while (index < Prefixes.Length)
        {
            declarations[index] = CreateXmlNamespaceBindingAt(index);
            index = index + 1;
        }

        return declarations;
    }

    internal XmlNamespaceBinding CreateXmlNamespaceBindingAt(int index)
    {
        if (index < 0 || index >= Prefixes.Length)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        XmlNamespaceBinding binding = new XmlNamespaceBinding();
        binding.Prefix = Prefixes[index];
        binding.NamespaceUri = NamespaceUris[index];
        binding.IsUndeclaration = Undeclarations[index];
        binding.Range = CopyXmlNamespaceRange(Ranges[index]);
        return binding;
    }

    internal SourceRange? CopyXmlNamespaceRange(SourceRange? range)
    {
        if (range == null)
        {
            return null;
        }

        SourceRange copy = new SourceRange();
        copy.Start = CopyXmlNamespaceLocation(range.Start);
        copy.End = CopyXmlNamespaceLocation(range.End);
        return copy;
    }

    internal SourceLocation? CopyXmlNamespaceLocation(SourceLocation? location)
    {
        if (location == null)
        {
            return null;
        }

        SourceLocation copy = new SourceLocation();
        copy.SourceUri = location.SourceUri;
        copy.CharacterOffset = location.CharacterOffset;
        copy.ByteOffset = location.ByteOffset;
        copy.Line = location.Line;
        copy.Column = location.Column;
        return copy;
    }
}
