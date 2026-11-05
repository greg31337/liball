using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace CurrencyCodes.DataGenerator;

internal sealed class IsoListThreeReader
{
    internal IsoListThreeData Read(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A List Three XML path is required.", nameof(path));
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

        XElement root = document.Root ?? throw new InvalidDataException("List Three XML has no root element.");
        RequireElementName(root, "ISO_4217");
        string publicationDate = ReadPublicationDate(root);

        List<XElement> rootElements = GetChildElements(root, "ISO_4217");
        if (rootElements.Count != 1)
        {
            throw Invalid(root, "ISO_4217 must contain exactly one HstrcCcyTbl element.");
        }

        XElement table = rootElements[0];
        RequireElementName(table, "HstrcCcyTbl");
        RequireNoAttributes(table);

        List<XElement> rowElements = GetChildElements(table, "HstrcCcyTbl");
        if (rowElements.Count == 0)
        {
            throw Invalid(table, "HstrcCcyTbl must contain at least one HstrcCcyNtry element.");
        }

        List<IsoListThreeRow> rows = new List<IsoListThreeRow>(rowElements.Count);
        for (int index = 0; index < rowElements.Count; index++)
        {
            XElement rowElement = rowElements[index];
            RequireElementName(rowElement, "HstrcCcyNtry");
            rows.Add(ReadRow(rowElement, index + 1));
        }

        return new IsoListThreeData(publicationDate, rows.AsReadOnly());
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

    private static IsoListThreeRow ReadRow(XElement row, int rowNumber)
    {
        RequireNoAttributes(row);
        List<XElement> fields = GetChildElements(row, $"HstrcCcyNtry row {rowNumber}");

        XElement? entityElement = null;
        XElement? nameElement = null;
        XElement? alphabeticElement = null;
        XElement? numericElement = null;
        XElement? minorUnitElement = null;
        XElement? withdrawalElement = null;
        int previousOrder = 0;

        foreach (XElement field in fields)
        {
            int order = GetFieldOrder(field);
            if (order <= previousOrder)
            {
                throw Invalid(field, $"Duplicate or out-of-order '{field.Name}' in HstrcCcyNtry row {rowNumber}.");
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
            else if (order == 5)
            {
                minorUnitElement = field;
            }
            else
            {
                withdrawalElement = field;
            }
        }

        if (entityElement == null)
        {
            throw Invalid(row, $"HstrcCcyNtry row {rowNumber} is missing CtryNm.");
        }

        if (nameElement == null)
        {
            throw Invalid(row, $"HstrcCcyNtry row {rowNumber} is missing CcyNm.");
        }

        if (alphabeticElement == null)
        {
            throw Invalid(row, $"HstrcCcyNtry row {rowNumber} is missing Ccy.");
        }

        if (withdrawalElement == null)
        {
            throw Invalid(row, $"HstrcCcyNtry row {rowNumber} is missing WthdrwlDt.");
        }

        RequireNoAttributes(entityElement);
        string entityName = ReadLeafText(entityElement);
        string officialName = ReadCurrencyName(nameElement, out string? fundIndicator);
        string alphabeticCode = ReadRequiredLeafText(alphabeticElement);
        string? numericCode = ReadOptionalLeafText(numericElement);
        ReadMinorUnit(minorUnitElement, out bool hasMinorUnit, out int minorUnitDigits);
        RequireNoAttributes(withdrawalElement);
        string withdrawalText = ReadLeafText(withdrawalElement);
        IsoPartialDate withdrawalDate = ParsePartialDate(withdrawalElement, withdrawalText);

        return new IsoListThreeRow(
            entityName,
            officialName,
            fundIndicator,
            alphabeticCode,
            numericCode,
            hasMinorUnit,
            minorUnitDigits,
            withdrawalText,
            withdrawalDate);
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

        if (field.Name == "WthdrwlDt")
        {
            return 6;
        }

        throw Invalid(field, $"Unexpected element '{field.Name}' in HstrcCcyNtry.");
    }

    private static string ReadCurrencyName(XElement element, out string? fundIndicator)
    {
        fundIndicator = null;
        foreach (XAttribute attribute in element.Attributes())
        {
            if (attribute.Name != "IsFund")
            {
                throw Invalid(attribute, $"Unexpected attribute '{attribute.Name}' on CcyNm.");
            }

            if (string.IsNullOrWhiteSpace(attribute.Value))
            {
                throw Invalid(attribute, "CcyNm IsFund must not be empty.");
            }

            fundIndicator = attribute.Value;
        }

        return ReadLeafText(element);
    }

    private static string ReadRequiredLeafText(XElement element)
    {
        RequireNoAttributes(element);
        return ReadLeafText(element);
    }

    private static string? ReadOptionalLeafText(XElement? element)
    {
        if (element == null)
        {
            return null;
        }

        return ReadRequiredLeafText(element);
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

    private static IsoPartialDate ParsePartialDate(XElement source, string text)
    {
        if (HasAsciiDateShape(text, 4, -1, -1))
        {
            if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int year)
                && year >= 1
                && year <= 9999)
            {
                return new IsoPartialDate(true, year, 0, 0, IsoDatePrecision.Year);
            }

            throw Invalid(source, "WthdrwlDt contains an invalid year.");
        }

        if (HasAsciiDateShape(text, 7, 4, -1))
        {
            if (DateOnly.TryParseExact(
                    text + "-01",
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateOnly monthDate))
            {
                return new IsoPartialDate(true, monthDate.Year, monthDate.Month, 0, IsoDatePrecision.Month);
            }

            throw Invalid(source, "WthdrwlDt contains an invalid year-month.");
        }

        if (HasAsciiDateShape(text, 10, 4, 7))
        {
            if (DateOnly.TryParseExact(
                    text,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateOnly dayDate))
            {
                return new IsoPartialDate(true, dayDate.Year, dayDate.Month, dayDate.Day, IsoDatePrecision.Day);
            }

            throw Invalid(source, "WthdrwlDt contains an invalid date.");
        }

        return new IsoPartialDate(false, 0, 0, 0, IsoDatePrecision.Unknown);
    }

    private static bool HasAsciiDateShape(string text, int length, int firstHyphen, int secondHyphen)
    {
        if (text.Length != length)
        {
            return false;
        }

        for (int index = 0; index < text.Length; index++)
        {
            if (index == firstHyphen || index == secondHyphen)
            {
                if (text[index] != '-')
                {
                    return false;
                }
            }
            else if (text[index] < '0' || text[index] > '9')
            {
                return false;
            }
        }

        return true;
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
                throw Invalid(node, "Unexpected content outside the List Three root element.");
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
