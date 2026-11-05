using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace config;

internal class ConfigXmlParser
{
    internal static ConfigValue ParseDefault(string text)
    {
        return Parse(text, "xml", 0);
    }

    internal static ConfigValue Parse(string text, string sourceName, int precedenceRank)
    {
        return ParseWithCaseMode(text, sourceName, precedenceRank, ConfigCaseMode.Sensitive);
    }

    internal static ConfigValue ParseWithCaseMode(string text, string sourceName, int precedenceRank, ConfigCaseMode caseMode)
    {
        XDocument document = XDocument.Parse(text);
        return FromDocument(document, sourceName, precedenceRank, caseMode);
    }

    internal static ConfigValue ParseWithDiagnostics(string text, string sourceName, int precedenceRank, ConfigDiagnostics diagnostics)
    {
        Task<XDocument> parseTask = Task.Run(() => XDocument.Parse(text, LoadOptions.SetLineInfo));
        SpinWait wait = new SpinWait();
        while (!parseTask.IsCompleted)
        {
            wait.SpinOnce();
        }

        if (parseTask.IsFaulted)
        {
            int lineNumber = 0;
            int columnNumber = 0;
            AggregateException? aggregate = parseTask.Exception;
            if (aggregate != null)
            {
                Exception failure = aggregate.GetBaseException();
                XmlException? xmlFailure = failure as XmlException;
                if (xmlFailure != null)
                {
                    lineNumber = xmlFailure.LineNumber;
                    columnNumber = xmlFailure.LinePosition;
                }
            }

            diagnostics.AddParseError(sourceName, "", "malformed XML input", lineNumber, columnNumber);
            return ConfigValue.Object(new ConfigMap()).SetSource(sourceName, precedenceRank, "", 0, 0);
        }

        return FromDocument(parseTask.Result, sourceName, precedenceRank, ConfigCaseMode.Sensitive);
    }

    internal static ConfigValue FromDocument(XDocument document, string sourceName, int precedenceRank, ConfigCaseMode caseMode)
    {
        ConfigMap map = ConfigMap.Create(caseMode);

        if (document.Root != null)
        {
            FillMapFromElement(map, document.Root, sourceName, precedenceRank, "", "", caseMode);
        }

        return ConfigValue.Object(map).SetSource(sourceName, precedenceRank, "", 0, 0);
    }

    internal static ConfigValue ParseFile(string path, string sourceName, int precedenceRank)
    {
        string text = File.ReadAllText(path);
        return Parse(text, sourceName, precedenceRank);
    }

    internal static ConfigValue ParseOptionalFile(string path, string sourceName, int precedenceRank)
    {
        if (!File.Exists(path))
        {
            ConfigMap map = new ConfigMap();
            return ConfigValue.Object(map).SetSource(sourceName, precedenceRank, "", 0, 0);
        }

        return ParseFile(path, sourceName, precedenceRank);
    }

    internal static void FillMapFromElement(ConfigMap map, XElement element, string sourceName, int precedenceRank, string metadataParentPath, string mapParentPath, ConfigCaseMode caseMode)
    {
        foreach (XAttribute attribute in element.Attributes())
        {
            string key = "@" + attribute.Name.LocalName;
            string metadataPath = ConfigJsonParser.ChildPath(metadataParentPath, key);
            string mapPath = ConfigJsonParser.ChildPath(mapParentPath, key);
            ConfigValue value = ConfigValue.String(attribute.Value).SetSource(sourceName, precedenceRank, metadataPath, 0, 0);
            map.SetPathDefault(mapPath, value);
        }

        foreach (XElement child in element.Elements())
        {
            string key = child.Name.LocalName;
            string metadataPath = ConfigJsonParser.ChildPath(metadataParentPath, key);
            string mapPath = ConfigJsonParser.ChildPath(mapParentPath, key);
            ConfigValue value = FromElement(child, sourceName, precedenceRank, metadataPath, caseMode);
            AddElementValue(map, mapPath, value);
        }

        string directText = DirectText(element).Trim();
        if (directText.Length > 0 && (HasElementChildren(element) || HasAttributes(element)))
        {
            string metadataTextPath = ConfigJsonParser.ChildPath(metadataParentPath, "#text");
            string mapTextPath = ConfigJsonParser.ChildPath(mapParentPath, "#text");
            ConfigValue textValue = ConfigValue.String(directText).SetSource(sourceName, precedenceRank, metadataTextPath, 0, 0);
            map.SetPathDefault(mapTextPath, textValue);
        }
    }

    internal static ConfigValue FromElement(XElement element, string sourceName, int precedenceRank, string path, ConfigCaseMode caseMode)
    {
        if (!HasElementChildren(element) && !HasAttributes(element))
        {
            return ConfigValue.String(element.Value).SetSource(sourceName, precedenceRank, path, 0, 0);
        }

        ConfigMap map = ConfigMap.Create(caseMode);
        FillMapFromElement(map, element, sourceName, precedenceRank, path, "", caseMode);
        return ConfigValue.Object(map).SetSource(sourceName, precedenceRank, path, 0, 0);
    }

    internal static void AddElementValue(ConfigMap map, string path, ConfigValue value)
    {
        ConfigValue existing = map.GetPathDefault(path);
        if (existing.IsMissing())
        {
            map.SetPathDefault(path, value);
            return;
        }

        if (existing.Kind == ConfigValueKind.Array && existing.ArrayValue != null)
        {
            existing.ArrayValue.Add(value);
            return;
        }

        ConfigArray array = new ConfigArray();
        array.Add(existing);
        array.Add(value);
        map.SetPathDefault(path, ConfigValue.Array(array).SetSource(value.SourceName == null ? "" : value.SourceName, value.PrecedenceRank, path, value.LineNumber, value.ColumnNumber));
    }

    internal static bool HasElementChildren(XElement element)
    {
        foreach (XElement child in element.Elements())
        {
            if (child != null)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool HasAttributes(XElement element)
    {
        foreach (XAttribute attribute in element.Attributes())
        {
            if (attribute != null)
            {
                return true;
            }
        }

        return false;
    }

    internal static string DirectText(XElement element)
    {
        string text = "";

        foreach (XNode node in element.Nodes())
        {
            XText? textNode = node as XText;
            if (textNode != null)
            {
                text = text + textNode.Value;
            }
        }

        return text;
    }
}
