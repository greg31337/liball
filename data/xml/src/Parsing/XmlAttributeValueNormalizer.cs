using System;

namespace xml;

internal class XmlAttributeValueNormalizer
{
    internal XmlVersion Version;
    internal EntityExpander EntityExpander;

    internal XmlAttributeValueNormalizer(XmlVersion version)
    {
        Version = version;
        EntityExpander = new EntityExpander(version, XmlEntityHandling.Expand, false, null);
    }

    internal void ConfigureXmlAttributeEntities(IXmlGeneralEntitySource? entitySource, ParserWorkLimiter? workLimiter, SourceTracker? tracker, IXmlErrorReporter? errorReporter)
    {
        EntityExpander = new EntityExpander(Version, XmlEntityHandling.Expand, false, entitySource);
        if (workLimiter != null)
        {
            EntityExpander.ConfigureXmlEntityWork(workLimiter, null);
        }

        if (tracker != null)
        {
            EntityExpander.ConfigureXmlEntityErrors(tracker, errorReporter);
        }
    }

    internal void ConfigureXmlAttributeEntityObserver(IXmlEntityReferenceObserver? observer)
    {
        EntityExpander.ConfigureXmlEntityReferenceObserver(observer);
    }

    internal string NormalizeXmlAttributeValue(string rawValue)
    {
        Guard.NotNull(rawValue, "rawValue");
        XmlScanner valueScanner = new XmlScanner(rawValue.AsMemory(), Version, XmlEncodingEntityKind.ExternalParsedEntity);
        ValueBuffer value = new ValueBuffer(rawValue.Length);
        while (true)
        {
            XmlToken token = valueScanner.ScanNextXmlToken();
            if (token.Kind == XmlTokenKind.EndOfInput)
            {
                break;
            }

            if (token.Kind == XmlTokenKind.Text && token.Value != null)
            {
                AppendNormalizedXmlAttributeText(value, token.Value);
            }
            else if (token.Kind == XmlTokenKind.EntityReference || token.Kind == XmlTokenKind.CharacterReference)
            {
                XmlEntityDispatchResult dispatch = EntityExpander.DispatchXmlReference(token);
                int index = 0;
                while (index < dispatch.Items.Length)
                {
                    XmlEntityDispatchItem item = dispatch.Items[index];
                    string? optionalValue = item.Value;
                    if (optionalValue != null)
                    {
                        if (item.Kind == XmlEntityDispatchKind.Character)
                        {
                            value.AppendText(optionalValue);
                        }
                        else if (item.Kind == XmlEntityDispatchKind.ReplacementText)
                        {
                            if (optionalValue.IndexOf('<') >= 0)
                            {
                                throw EntityExpander.CreateXmlEntityExpansionError("An entity replacement used in an attribute value cannot contain '<'", token.StartUtf16Offset);
                            }

                            AppendNormalizedXmlAttributeText(value, optionalValue);
                        }
                    }

                    index = index + 1;
                }
            }
        }

        return value.GetText();
    }

    internal string NormalizeDtdAttributeValue(string rawValue, string typeName)
    {
        Guard.NotNull(rawValue, "rawValue");
        Guard.NotNull(typeName, "typeName");
        string normalized = NormalizeXmlAttributeValue(rawValue);
        if (typeName == "CDATA")
        {
            return normalized;
        }

        return CollapseDtdAttributeSpaces(normalized);
    }

    internal string CollapseDtdAttributeSpaces(string normalizedValue)
    {
        Guard.NotNull(normalizedValue, "normalizedValue");
        ValueBuffer collapsed = new ValueBuffer(normalizedValue.Length);
        bool pendingSpace = false;
        int index = 0;
        while (index < normalizedValue.Length)
        {
            char character = normalizedValue[index];
            if (character == ' ')
            {
                if (collapsed.CharacterCount() > 0)
                {
                    pendingSpace = true;
                }
            }
            else
            {
                if (pendingSpace)
                {
                    collapsed.AppendCharacter(' ');
                    pendingSpace = false;
                }

                collapsed.AppendCharacter(character);
            }

            index = index + 1;
        }

        return collapsed.GetText();
    }

    internal void AppendNormalizedXmlAttributeText(ValueBuffer destination, string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            destination.AppendCharacter(character == '\t' || character == '\n' || character == '\r' ? ' ' : character);
            index = index + 1;
        }
    }
}
