using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace CountryCodes.DataGenerator.Parsers;

internal static class CldrXmlParser
{
    internal static CldrNamesResult ParseNames(Stream input, string sourceName)
    {
        XDocument document = LoadDocument(input, sourceName);
        XElement root = ReadRoot(document, "ldml", sourceName);
        string locale = ReadLocale(root, sourceName);
        List<CldrNameRecord> records = new List<CldrNameRecord>();

        foreach (XElement element in root.Descendants())
        {
            CldrNameTarget target;
            if (string.Equals(element.Name.LocalName, "territory", StringComparison.Ordinal))
            {
                target = CldrNameTarget.Territory;
            }
            else if (string.Equals(element.Name.LocalName, "subdivision", StringComparison.Ordinal))
            {
                target = CldrNameTarget.Subdivision;
            }
            else
            {
                continue;
            }

            string code = ReadRequiredAttribute(element, "type", sourceName);
            string value = element.Value;
            if (value.Length == 0)
            {
                throw new SourceParseException(
                    sourceName,
                    $"{element.Name.LocalName} '{code}' has an empty display value");
            }

            records.Add(new CldrNameRecord(
                target,
                code,
                value,
                ReadOptionalAttribute(element, "alt"),
                ReadOptionalAttribute(element, "draft")));
        }

        if (records.Count == 0)
        {
            throw new SourceParseException(sourceName, "no territory or subdivision names found");
        }

        return new CldrNamesResult(locale, records);
    }

    internal static IReadOnlyList<CldrAliasRecord> ParseAliases(Stream input, string sourceName)
    {
        XDocument document = LoadDocument(input, sourceName);
        XElement root = ReadRoot(document, "supplementalData", sourceName);
        List<CldrAliasRecord> records = new List<CldrAliasRecord>();

        foreach (XElement element in root.Descendants())
        {
            CldrAliasTarget target;
            if (string.Equals(element.Name.LocalName, "territoryAlias", StringComparison.Ordinal))
            {
                target = CldrAliasTarget.Territory;
            }
            else if (string.Equals(element.Name.LocalName, "subdivisionAlias", StringComparison.Ordinal))
            {
                target = CldrAliasTarget.Subdivision;
            }
            else
            {
                continue;
            }

            string code = ReadRequiredAttribute(element, "type", sourceName);
            string replacementText = ReadRequiredAttribute(element, "replacement", sourceName);
            string reason = ReadRequiredAttribute(element, "reason", sourceName);
            string[] replacements = SplitTokens(replacementText, sourceName, element.Name.LocalName);
            records.Add(new CldrAliasRecord(target, code, replacements, reason));
        }

        if (records.Count == 0)
        {
            throw new SourceParseException(sourceName, "no territory or subdivision aliases found");
        }

        return records;
    }

    internal static IReadOnlyList<CldrContainmentRecord> ParseContainment(
        Stream input,
        string sourceName)
    {
        XDocument document = LoadDocument(input, sourceName);
        XElement root = ReadRoot(document, "supplementalData", sourceName);
        List<CldrContainmentRecord> records = new List<CldrContainmentRecord>();

        foreach (XElement element in root.Descendants())
        {
            if (!string.Equals(element.Name.LocalName, "subgroup", StringComparison.Ordinal))
            {
                continue;
            }

            string parent = ReadRequiredAttribute(element, "type", sourceName);
            string contains = ReadRequiredAttribute(element, "contains", sourceName);
            string[] children = SplitTokens(contains, sourceName, $"subgroup '{parent}'");
            records.Add(new CldrContainmentRecord(parent, children));
        }

        if (records.Count == 0)
        {
            throw new SourceParseException(sourceName, "no subdivision containment rows found");
        }

        return records;
    }

    private static XDocument LoadDocument(Stream input, string sourceName)
    {
        XmlReaderSettings settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            XmlResolver = null,
            MaxCharactersInDocument = 64_000_000
        };

        try
        {
            using XmlReader reader = XmlReader.Create(input, settings);
            return XDocument.Load(reader, LoadOptions.SetLineInfo);
        }
        catch (XmlException exception)
        {
            throw new SourceParseException(sourceName, "invalid XML", exception);
        }
    }

    private static XElement ReadRoot(XDocument document, string expectedName, string sourceName)
    {
        XElement? root = document.Root;
        if (root == null || !string.Equals(root.Name.LocalName, expectedName, StringComparison.Ordinal))
        {
            throw new SourceParseException(sourceName, $"root element must be '{expectedName}'");
        }

        return root;
    }

    private static string ReadLocale(XElement root, string sourceName)
    {
        foreach (XElement element in root.Descendants())
        {
            if (!string.Equals(element.Name.LocalName, "identity", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (XElement child in element.Elements())
            {
                if (string.Equals(child.Name.LocalName, "language", StringComparison.Ordinal))
                {
                    return ReadRequiredAttribute(child, "type", sourceName);
                }
            }
        }

        throw new SourceParseException(sourceName, "missing identity/language locale");
    }

    private static string ReadRequiredAttribute(
        XElement element,
        string attributeName,
        string sourceName)
    {
        XAttribute? attribute = element.Attribute(attributeName);
        if (attribute == null || attribute.Value.Length == 0)
        {
            throw new SourceParseException(
                sourceName,
                $"element '{element.Name.LocalName}' requires non-empty attribute '{attributeName}'");
        }

        return attribute.Value;
    }

    private static string? ReadOptionalAttribute(XElement element, string attributeName)
    {
        XAttribute? attribute = element.Attribute(attributeName);
        return attribute == null ? null : attribute.Value;
    }

    private static string[] SplitTokens(string value, string sourceName, string context)
    {
        string[] tokens = value.Split(
            new[] { ' ', '\t', '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            throw new SourceParseException(sourceName, $"{context} contains no tokens");
        }

        return tokens;
    }
}

internal sealed class CldrNamesResult
{
    internal CldrNamesResult(string locale, IReadOnlyList<CldrNameRecord> names)
    {
        Locale = locale;
        Names = names;
    }

    internal string Locale { get; }

    internal IReadOnlyList<CldrNameRecord> Names { get; }
}
