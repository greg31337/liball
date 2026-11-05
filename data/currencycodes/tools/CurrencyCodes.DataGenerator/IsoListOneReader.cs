using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace CurrencyCodes.DataGenerator;

internal sealed class IsoListOneReader
{
    internal IsoListOneData Read(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A List One XML path is required.", nameof(path));
        }

        XmlReaderSettings settings = new XmlReaderSettings
        {
            CloseInput = true,
            DtdProcessing = DtdProcessing.Prohibit,
            IgnoreComments = false,
            IgnoreProcessingInstructions = false,
            IgnoreWhitespace = false,
            XmlResolver = null
        };

        XDocument document;
        using (FileStream stream = File.OpenRead(path))
        using (XmlReader reader = XmlReader.Create(stream, settings))
        {
            document = XDocument.Load(reader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        }

        ValidateDocumentNodes(document);

        XElement root = document.Root ?? throw new InvalidDataException("List One XML has no root element.");
        RequireElementName(root, "ISO_4217");
        string publicationDate = ReadPublicationDate(root);

        List<XElement> rootElements = GetChildElements(root, "ISO_4217");
        if (rootElements.Count != 1)
        {
            throw Invalid(root, "ISO_4217 must contain exactly one CcyTbl element.");
        }

        XElement table = rootElements[0];
        RequireElementName(table, "CcyTbl");
        RequireNoAttributes(table);

        List<XElement> rowElements = GetChildElements(table, "CcyTbl");
        if (rowElements.Count == 0)
        {
            throw Invalid(table, "CcyTbl must contain at least one CcyNtry element.");
        }

        List<IsoListOneRow> rows = new List<IsoListOneRow>(rowElements.Count);
        for (int index = 0; index < rowElements.Count; index++)
        {
            XElement rowElement = rowElements[index];
            RequireElementName(rowElement, "CcyNtry");
            rows.Add(ReadRow(rowElement, index + 1));
        }

        return new IsoListOneData(publicationDate, rows.AsReadOnly());
    }

    private static string ReadPublicationDate(XElement root)
    {
        XAttribute? publicationAttribute = null;
        foreach (XAttribute attribute in root.Attributes())
        {
            if (attribute.Name == "Pblshd" && publicationAttribute == null)
            {
                publicationAttribute = attribute;
            }
            else
            {
                throw Invalid(attribute, $"Unexpected attribute '{attribute.Name}' on ISO_4217.");
            }
        }

        if (publicationAttribute == null)
        {
            throw Invalid(root, "ISO_4217 is missing the Pblshd attribute.");
        }

        string publicationDate = publicationAttribute.Value;
        if (!DateOnly.TryParseExact(
                publicationDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly _))
        {
            throw Invalid(publicationAttribute, "Pblshd must be a valid date in yyyy-MM-dd form.");
        }

        return publicationDate;
    }

    private static IsoListOneRow ReadRow(XElement row, int rowNumber)
    {
        RequireNoAttributes(row);
        List<XElement> fields = GetChildElements(row, $"CcyNtry row {rowNumber}");

        XElement? entityElement = null;
        XElement? nameElement = null;
        XElement? alphabeticElement = null;
        XElement? numericElement = null;
        XElement? minorUnitElement = null;
        int previousOrder = 0;

        foreach (XElement field in fields)
        {
            int order = GetFieldOrder(field);
            if (order <= previousOrder)
            {
                throw Invalid(field, $"Duplicate or out-of-order '{field.Name}' in CcyNtry row {rowNumber}.");
            }

            previousOrder = order;
            if (order == 1)
            {
                entityElement = field;
            }
            else if (order == 2)
            {
                nameElement = field;
            }
            else if (order == 3)
            {
                alphabeticElement = field;
            }
            else if (order == 4)
            {
                numericElement = field;
            }
            else
            {
                minorUnitElement = field;
            }
        }

        if (entityElement == null)
        {
            throw Invalid(row, $"CcyNtry row {rowNumber} is missing CtryNm.");
        }

        if (nameElement == null)
        {
            throw Invalid(row, $"CcyNtry row {rowNumber} is missing CcyNm.");
        }

        RequireNoAttributes(entityElement);
        string entityName = ReadLeafText(entityElement);
        string officialName = ReadCurrencyName(nameElement, out bool isFund);
        string? alphabeticCode = ReadOptionalLeafText(alphabeticElement);
        string? numericCode = ReadOptionalLeafText(numericElement);
        ReadMinorUnit(minorUnitElement, out bool hasMinorUnit, out int minorUnitDigits);

        return new IsoListOneRow(
            entityName,
            officialName,
            isFund,
            alphabeticCode,
            numericCode,
            hasMinorUnit,
            minorUnitDigits);
    }

    private static int GetFieldOrder(XElement field)
    {
        if (field.Name == "CtryNm")
        {
            return 1;
        }

        if (field.Name == "CcyNm")
        {
            return 2;
        }

        if (field.Name == "Ccy")
        {
            return 3;
        }

        if (field.Name == "CcyNbr")
        {
            return 4;
        }

        if (field.Name == "CcyMnrUnts")
        {
            return 5;
        }

        throw Invalid(field, $"Unexpected element '{field.Name}' in CcyNtry.");
    }

    private static string ReadCurrencyName(XElement element, out bool isFund)
    {
        isFund = false;
        foreach (XAttribute attribute in element.Attributes())
        {
            if (attribute.Name != "IsFund")
            {
                throw Invalid(attribute, $"Unexpected attribute '{attribute.Name}' on CcyNm.");
            }

            if (attribute.Value == "true")
            {
                isFund = true;
            }
            else if (attribute.Value != "false")
            {
                throw Invalid(attribute, "CcyNm IsFund must be 'true' or 'false'.");
            }
        }

        return ReadLeafText(element);
    }

    private static string? ReadOptionalLeafText(XElement? element)
    {
        if (element == null)
        {
            return null;
        }

        RequireNoAttributes(element);
        return ReadLeafText(element);
    }

    private static void ReadMinorUnit(XElement? element, out bool hasMinorUnit, out int minorUnitDigits)
    {
        hasMinorUnit = false;
        minorUnitDigits = 0;
        if (element == null)
        {
            return;
        }

        RequireNoAttributes(element);
        string text = ReadLeafText(element);
        if (text == "N.A.")
        {
            return;
        }

        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out minorUnitDigits)
            || minorUnitDigits < 0)
        {
            throw Invalid(element, "CcyMnrUnts must be a non-negative integer or 'N.A.'.");
        }

        hasMinorUnit = true;
    }

    private static string ReadLeafText(XElement element)
    {
        foreach (XNode node in element.Nodes())
        {
            if (node is not XText)
            {
                throw Invalid(node, $"Element '{element.Name}' must contain text only.");
            }
        }

        string text = element.Value;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw Invalid(element, $"Element '{element.Name}' must not be empty.");
        }

        return text;
    }

    private static List<XElement> GetChildElements(XContainer container, string context)
    {
        List<XElement> elements = new List<XElement>();
        foreach (XNode node in container.Nodes())
        {
            if (node is XElement element)
            {
                elements.Add(element);
            }
            else if (node is XText text && string.IsNullOrWhiteSpace(text.Value))
            {
            }
            else
            {
                throw Invalid(node, $"Unexpected content in {context}.");
            }
        }

        return elements;
    }

    private static void ValidateDocumentNodes(XDocument document)
    {
        foreach (XNode node in document.Nodes())
        {
            if (node is XElement)
            {
            }
            else if (node is XText text && string.IsNullOrWhiteSpace(text.Value))
            {
            }
            else
            {
                throw Invalid(node, "Unexpected content outside the List One root element.");
            }
        }
    }

    private static void RequireElementName(XElement element, string expectedName)
    {
        if (element.Name != expectedName)
        {
            throw Invalid(element, $"Expected '{expectedName}' but found '{element.Name}'.");
        }
    }

    private static void RequireNoAttributes(XElement element)
    {
        foreach (XAttribute attribute in element.Attributes())
        {
            throw Invalid(attribute, $"Unexpected attribute '{attribute.Name}' on {element.Name}.");
        }
    }

    private static InvalidDataException Invalid(XObject source, string message)
    {
        if (source is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
        {
            return new InvalidDataException($"{message} Line {lineInfo.LineNumber}, position {lineInfo.LinePosition}.");
        }

        return new InvalidDataException(message);
    }
}
