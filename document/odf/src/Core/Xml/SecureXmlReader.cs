using System.IO;
using System.Xml;

namespace odf;

internal static class SecureXmlReader
{
    internal static XmlReader Create(
        Stream input,
        OdfSecurityLimits limits,
        bool leaveInputOpen)
    {
        ValidateLimits(limits);
        if (input == null)
        {
            throw new OdfXmlException("An XML input stream is required.");
        }

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.CheckCharacters = true;
        settings.CloseInput = !leaveInputOpen;
        settings.ConformanceLevel = ConformanceLevel.Document;
        settings.DtdProcessing = DtdProcessing.Prohibit;
        settings.IgnoreComments = false;
        settings.IgnoreProcessingInstructions = false;
        settings.IgnoreWhitespace = false;
        settings.MaxCharactersFromEntities = limits.MaximumXmlTextLength;
        settings.MaxCharactersInDocument = limits.MaximumSingleEntryBytes;
        settings.XmlResolver = null;
        return XmlReader.Create(input, settings);
    }

    internal static bool Read(
        XmlReader reader,
        OdfSecurityLimits limits)
    {
        ValidateLimits(limits);
        if (reader == null)
        {
            throw new OdfXmlException("An XML reader is required.");
        }
        if (!reader.Read())
        {
            return false;
        }

        if (reader.NodeType == XmlNodeType.Element)
        {
            if (reader.Depth + 1 > limits.MaximumXmlDepth)
            {
                throw new OdfXmlException("The XML input exceeds the configured element-depth limit.");
            }
            if (reader.AttributeCount > limits.MaximumXmlAttributesPerElement)
            {
                throw new OdfXmlException("The XML input exceeds the configured per-element attribute limit.");
            }
            int attributeIndex = 0;
            while (attributeIndex < reader.AttributeCount)
            {
                reader.MoveToAttribute(attributeIndex);
                if (reader.Value.Length > limits.MaximumXmlTextLength)
                {
                    throw new OdfXmlException("The XML input exceeds the configured text-value limit.");
                }
                attributeIndex++;
            }
            reader.MoveToElement();
        }
        else if (IsCharacterData(reader.NodeType)
            && reader.Value.Length > limits.MaximumXmlTextLength)
        {
            throw new OdfXmlException("The XML input exceeds the configured text-value limit.");
        }
        return true;
    }

    static void ValidateLimits(OdfSecurityLimits limits)
    {
        if (limits == null
            || limits.MaximumSingleEntryBytes <= 0
            || limits.MaximumXmlDepth <= 0
            || limits.MaximumXmlAttributesPerElement < 0
            || limits.MaximumXmlTextLength < 0)
        {
            throw new OdfXmlException("XML reader limits must be non-negative and bounded.");
        }
    }

    static bool IsCharacterData(XmlNodeType nodeType)
    {
        return nodeType == XmlNodeType.Text
            || nodeType == XmlNodeType.CDATA
            || nodeType == XmlNodeType.Whitespace
            || nodeType == XmlNodeType.SignificantWhitespace
            || nodeType == XmlNodeType.Comment
            || nodeType == XmlNodeType.ProcessingInstruction
            || nodeType == XmlNodeType.XmlDeclaration;
    }
}
