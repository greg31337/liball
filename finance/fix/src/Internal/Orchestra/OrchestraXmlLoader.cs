using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace fix;

internal static class OrchestraXmlLoader
{
    internal const string RepositoryVersion10Namespace = "http://fixprotocol.io/2020/orchestra/repository";
    internal const string RepositoryVersion11Namespace = "http://fixprotocol.io/2026/orchestra/repository";
    internal const string InterfacesVersion10Namespace = "http://fixprotocol.io/2020/orchestra/interfaces";
    internal const string InterfacesVersion11Namespace = "http://fixprotocol.io/2026/orchestra/interfaces";
    internal const string XIncludeNamespace = "http://www.w3.org/2001/XInclude";

    internal static OrchestraXmlLoadResult LoadDocument(Stream source, OrchestraReadOptions options)
    {
        OrchestraXmlLoadResult invalid = ValidateInputs(source, options);
        if (!invalid.Success) return invalid;
        OrchestraXmlLoadBudget budget = new OrchestraXmlLoadBudget(options.MaxSourceSize);
        string baseDirectory = ResolveBaseDirectory(options);
        string sourceName = options.BaseUri == null ? "" : options.BaseUri;
        return LoadDocumentCore(source, options, budget, baseDirectory, baseDirectory, sourceName, true);
    }

    internal static OrchestraXmlLoadResult ValidateInputs(Stream source, OrchestraReadOptions options)
    {
        OrchestraXmlLoadResult result = new OrchestraXmlLoadResult();
        result.Success = true;
        if (source == null || !source.CanRead)
        {
            AddDiagnostic(result, ValidationSeverity.Error, "unreadable-orchestra-source", "The Orchestra XML source is not readable.", "", 0, 0, "");
        }
        if (options == null)
        {
            AddDiagnostic(result, ValidationSeverity.Error, "missing-orchestra-read-options", "The Orchestra XML read options are missing.", "", 0, 0, "");
            return Complete(result);
        }
        if (options.MaxSourceSize <= 0)
        {
            AddDiagnostic(result, ValidationSeverity.Error, "invalid-orchestra-source-limit", "The maximum Orchestra XML source size must be positive.", "", 0, 0, "");
        }
        if (options.MaxXmlDepth <= 0)
        {
            AddDiagnostic(result, ValidationSeverity.Error, "invalid-orchestra-depth-limit", "The maximum Orchestra XML depth must be positive.", "", 0, 0, "");
        }
        if (options.AllowExternalEntities)
        {
            AddDiagnostic(result, ValidationSeverity.Error, "external-entities-prohibited", "External XML entities are prohibited for Orchestra documents.", "", 0, 0, "");
        }
        if (options.ResolveXInclude && (options.BaseUri == null || options.BaseUri.Length == 0))
        {
            AddDiagnostic(result, ValidationSeverity.Error, "missing-xinclude-base-uri", "Controlled XInclude resolution requires an absolute base directory.", "", 0, 0, "");
        }
        else if (options.ResolveXInclude && !Path.IsPathFullyQualified(options.BaseUri))
        {
            AddDiagnostic(result, ValidationSeverity.Error, "invalid-xinclude-base-uri", "The XInclude base directory must be an absolute filesystem path.", options.BaseUri, 0, 0, "");
        }
        return Complete(result);
    }

    internal static OrchestraXmlLoadResult LoadDocumentCore(Stream source, OrchestraReadOptions options, OrchestraXmlLoadBudget budget, string allowedBaseDirectory, string currentBaseDirectory, string sourceName, bool requireDocumentRoot)
    {
        OrchestraXmlLoadResult result = new OrchestraXmlLoadResult();
        byte[] bytes = ReadBoundedSource(source, budget, result, sourceName);
        if (bytes.Length == 0 && result.Diagnostics.Count != 0) return Complete(result);
        if (ContainsAsciiIgnoreCase(bytes, "<!DOCTYPE") || ContainsAsciiIgnoreCase(bytes, "<!ENTITY"))
        {
            AddDiagnostic(result, ValidationSeverity.Error, "external-entities-prohibited", "Document type declarations and XML entities are prohibited for Orchestra documents.", sourceName, 0, 0, "");
            return Complete(result);
        }

        XmlReaderSettings settings = CreateReaderSettings(options);
        XmlDocument document = new XmlDocument();
        document.XmlResolver = null;
        result.Document = document;
        using (MemoryStream input = new MemoryStream(bytes, false))
        using (XmlReader reader = XmlReader.Create(input, settings, sourceName))
        {
            BuildDocumentSafely(reader, document, result, options, sourceName);
        }
        if (result.Diagnostics.Count != 0) return Complete(result);
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            AddDiagnostic(result, ValidationSeverity.Error, "missing-orchestra-root", "The Orchestra XML document has no root element.", sourceName, 0, 0, "");
            return Complete(result);
        }
        result.Root = root;
        if (!DispatchNamespace(root, result, requireDocumentRoot)) return Complete(result);
        if (options.ResolveXInclude)
        {
            ResolveIncludes(root, result, options, budget, allowedBaseDirectory, currentBaseDirectory);
        }
        return Complete(result);
    }

    internal static XmlReaderSettings CreateReaderSettings(OrchestraReadOptions options)
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        settings.IgnoreComments = false;
        settings.IgnoreWhitespace = false;
        settings.CloseInput = false;
        settings.MaxCharactersInDocument = options.MaxSourceSize;
        settings.MaxCharactersFromEntities = 0;
        return settings;
    }

    internal static void BuildDocumentSafely(XmlReader reader, XmlDocument document, OrchestraXmlLoadResult result, OrchestraReadOptions options, string sourceName)
    {
        Task task = Task.Run(() => BuildDocument(reader, document, result, options, sourceName));
        while (!task.IsCompleted) Thread.Yield();
        if (!task.IsFaulted) return;
        AggregateException? aggregate = task.Exception;
        Exception? exception = aggregate == null ? null : aggregate.GetBaseException();
        XmlException? xmlException = exception as XmlException;
        int line = xmlException == null ? 0 : xmlException.LineNumber;
        int column = xmlException == null ? 0 : xmlException.LinePosition;
        string message = exception == null ? "The Orchestra XML document is malformed." : exception.Message;
        AddDiagnostic(result, ValidationSeverity.Error, "invalid-orchestra-xml", message, sourceName, line, column, "");
    }

    internal static void BuildDocument(XmlReader reader, XmlDocument document, OrchestraXmlLoadResult result, OrchestraReadOptions options, string sourceName)
    {
        List<XmlElement> parents = [];
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                if (reader.Depth + 1 > options.MaxXmlDepth)
                {
                    IXmlLineInfo depthLine = (IXmlLineInfo)reader;
                    AddDiagnostic(result, ValidationSeverity.Error, "orchestra-xml-depth-exceeded", "The Orchestra XML element depth exceeds the configured limit.", sourceName, ReadLine(depthLine), ReadColumn(depthLine), "");
                    return;
                }
                XmlElement element = document.CreateElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                CopyAttributes(reader, document, element);
                if (parents.Count == 0) document.AppendChild(element);
                else parents[parents.Count - 1].AppendChild(element);
                IXmlLineInfo line = (IXmlLineInfo)reader;
                OrchestraXmlSourceLocation location = new OrchestraXmlSourceLocation();
                location.Source = sourceName;
                location.Line = ReadLine(line);
                location.Column = ReadColumn(line);
                result.Locations[element] = location;
                if (!reader.IsEmptyElement) parents.Add(element);
            }
            else if (reader.NodeType == XmlNodeType.EndElement)
            {
                if (parents.Count != 0) parents.RemoveAt(parents.Count - 1);
            }
            else if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.SignificantWhitespace || reader.NodeType == XmlNodeType.Whitespace)
            {
                AppendTextNode(document, parents, reader.Value);
            }
            else if (reader.NodeType == XmlNodeType.CDATA)
            {
                if (parents.Count != 0) parents[parents.Count - 1].AppendChild(document.CreateCDataSection(reader.Value));
            }
            else if (reader.NodeType == XmlNodeType.Comment)
            {
                XmlComment comment = document.CreateComment(reader.Value);
                if (parents.Count == 0) document.AppendChild(comment);
                else parents[parents.Count - 1].AppendChild(comment);
            }
            else if (reader.NodeType == XmlNodeType.ProcessingInstruction)
            {
                XmlProcessingInstruction instruction = document.CreateProcessingInstruction(reader.Name, reader.Value);
                if (parents.Count == 0) document.AppendChild(instruction);
                else parents[parents.Count - 1].AppendChild(instruction);
            }
        }
    }

    internal static void CopyAttributes(XmlReader reader, XmlDocument document, XmlElement element)
    {
        if (!reader.HasAttributes) return;
        bool available = reader.MoveToFirstAttribute();
        while (available)
        {
            XmlAttribute attribute = document.CreateAttribute(reader.Prefix, reader.LocalName, reader.NamespaceURI);
            attribute.Value = reader.Value;
            element.Attributes.Append(attribute);
            available = reader.MoveToNextAttribute();
        }
        reader.MoveToElement();
    }

    internal static void AppendTextNode(XmlDocument document, List<XmlElement> parents, string value)
    {
        if (parents.Count != 0) parents[parents.Count - 1].AppendChild(document.CreateTextNode(value));
    }

    internal static bool DispatchNamespace(XmlElement root, OrchestraXmlLoadResult result, bool requireDocumentRoot)
    {
        string namespaceUri = root.NamespaceURI;
        result.NamespaceUri = namespaceUri;
        if (namespaceUri == RepositoryVersion10Namespace)
        {
            result.Version = OrchestraVersion.Version10;
            result.InterfacesDocument = false;
        }
        else if (namespaceUri == RepositoryVersion11Namespace)
        {
            result.Version = OrchestraVersion.Version11;
            result.InterfacesDocument = false;
        }
        else if (namespaceUri == InterfacesVersion10Namespace)
        {
            result.Version = OrchestraVersion.Version10;
            result.InterfacesDocument = true;
        }
        else if (namespaceUri == InterfacesVersion11Namespace)
        {
            result.Version = OrchestraVersion.Version11;
            result.InterfacesDocument = true;
        }
        else
        {
            AddElementDiagnostic(result, root, ValidationSeverity.Error, "unsupported-orchestra-namespace", "The document namespace is not a supported Orchestra repository or interfaces namespace.");
            return false;
        }
        if (!requireDocumentRoot) return true;
        string expectedRoot = result.InterfacesDocument ? "interfaces" : "repository";
        if (root.LocalName != expectedRoot)
        {
            AddElementDiagnostic(result, root, ValidationSeverity.Error, "invalid-orchestra-root", "The root element does not match the Orchestra namespace document kind.");
            return false;
        }
        return true;
    }

    internal static void ResolveIncludes(XmlElement parent, OrchestraXmlLoadResult result, OrchestraReadOptions options, OrchestraXmlLoadBudget budget, string allowedBaseDirectory, string currentBaseDirectory)
    {
        int index = 0;
        while (index < parent.ChildNodes.Count)
        {
            XmlNode? child = parent.ChildNodes[index];
            if (child == null)
            {
                index++;
                continue;
            }
            XmlElement? element = child as XmlElement;
            if (element == null)
            {
                index++;
            }
            else if (element.NamespaceURI == XIncludeNamespace && element.LocalName == "include")
            {
                if (!ResolveOneInclude(parent, element, result, options, budget, allowedBaseDirectory, currentBaseDirectory)) index++;
            }
            else
            {
                ResolveIncludes(element, result, options, budget, allowedBaseDirectory, currentBaseDirectory);
                index++;
            }
        }
    }

    internal static bool ResolveOneInclude(XmlElement parent, XmlElement include, OrchestraXmlLoadResult result, OrchestraReadOptions options, OrchestraXmlLoadBudget budget, string allowedBaseDirectory, string currentBaseDirectory)
    {
        string href = include.GetAttribute("href").Trim();
        string parse = include.GetAttribute("parse").Trim();
        if (href.Length == 0 || parse.Length != 0 && parse != "xml" || include.HasAttribute("xpointer") || !IsSafeRelativeReference(href))
        {
            AddElementDiagnostic(result, include, ValidationSeverity.Error, "invalid-xinclude-reference", "XInclude permits only a relative local XML href without XPointer.");
            return false;
        }
        string path = Path.GetFullPath(Path.Combine(currentBaseDirectory, href));
        if (!IsContainedPath(path, allowedBaseDirectory))
        {
            AddElementDiagnostic(result, include, ValidationSeverity.Error, "xinclude-outside-base-uri", "The XInclude reference resolves outside the configured base directory.");
            return false;
        }
        if (!File.Exists(path))
        {
            AddElementDiagnostic(result, include, ValidationSeverity.Error, "xinclude-source-not-found", "The XInclude source file does not exist.");
            return false;
        }
        OrchestraXmlLoadResult included;
        using (FileStream source = File.OpenRead(path))
        {
            string includedBase = Path.GetDirectoryName(path) ?? currentBaseDirectory;
            included = LoadDocumentCore(source, options, budget, allowedBaseDirectory, includedBase, path, false);
        }
        CopyDiagnostics(result, included);
        if (!included.Success || included.Root == null || result.Document == null) return false;
        int combinedDepth = ElementDepth(include) + SubtreeDepth(included.Root) - 1;
        if (combinedDepth > options.MaxXmlDepth)
        {
            AddElementDiagnostic(result, include, ValidationSeverity.Error, "orchestra-xml-depth-exceeded", "The resolved XInclude content exceeds the configured XML depth.");
            return false;
        }
        XmlNode imported = result.Document.ImportNode(included.Root, true);
        CopyImportedLocations(imported, included.Root, result, included);
        parent.ReplaceChild(imported, include);
        return true;
    }

    internal static byte[] ReadBoundedSource(Stream source, OrchestraXmlLoadBudget budget, OrchestraXmlLoadResult result, string sourceName)
    {
        using (MemoryStream buffer = new MemoryStream())
        {
            byte[] block = new byte[8192];
            bool complete = false;
            while (!complete)
            {
                int count = source.Read(block, 0, block.Length);
                if (count == 0)
                {
                    complete = true;
                }
                else if (count > budget.RemainingBytes)
                {
                    AddDiagnostic(result, ValidationSeverity.Error, "orchestra-source-size-exceeded", "The Orchestra XML source and includes exceed the configured byte limit.", sourceName, 0, 0, "");
                    return [];
                }
                else
                {
                    buffer.Write(block, 0, count);
                    budget.RemainingBytes -= count;
                }
            }
            byte[] bytes = buffer.ToArray();
            if (bytes.Length == 0) AddDiagnostic(result, ValidationSeverity.Error, "empty-orchestra-source", "The Orchestra XML source is empty.", sourceName, 0, 0, "");
            return bytes;
        }
    }

    internal static bool ContainsAsciiIgnoreCase(byte[] source, string text)
    {
        byte[] pattern = Encoding.ASCII.GetBytes(text.ToUpperInvariant());
        for (int index = 0; index + pattern.Length <= source.Length; index++)
        {
            bool match = true;
            for (int offset = 0; offset < pattern.Length; offset++)
            {
                byte value = source[index + offset];
                if (value >= 97 && value <= 122) value = (byte)(value - 32);
                if (value != pattern[offset]) match = false;
            }
            if (match) return true;
        }
        return false;
    }

    internal static string ResolveBaseDirectory(OrchestraReadOptions options)
    {
        if (!options.ResolveXInclude) return "";
        return Path.GetFullPath(options.BaseUri);
    }

    internal static bool IsSafeRelativeReference(string href)
    {
        if (href.Length == 0 || href.IndexOf('\0') >= 0 || Path.IsPathFullyQualified(href)) return false;
        if (href.IndexOf(':') >= 0 || href.IndexOf('?') >= 0 || href.IndexOf('#') >= 0) return false;
        return true;
    }

    internal static bool IsContainedPath(string path, string baseDirectory)
    {
        string root = Path.GetFullPath(baseDirectory);
        string separator = Path.DirectorySeparatorChar.ToString();
        if (!root.EndsWith(separator, StringComparison.Ordinal)) root += separator;
        return path.StartsWith(root, StringComparison.Ordinal) && path.Length > root.Length;
    }

    internal static int ElementDepth(XmlElement element)
    {
        int depth = 1;
        XmlNode? parent = element.ParentNode;
        XmlElement? parentElement = parent as XmlElement;
        while (parentElement != null)
        {
            depth++;
            parent = parentElement.ParentNode;
            parentElement = parent as XmlElement;
        }
        return depth;
    }

    internal static int SubtreeDepth(XmlElement element)
    {
        int maximum = 1;
        for (int index = 0; index < element.ChildNodes.Count; index++)
        {
            XmlElement? child = element.ChildNodes[index] as XmlElement;
            if (child != null)
            {
                int depth = 1 + SubtreeDepth(child);
                if (depth > maximum) maximum = depth;
            }
        }
        return maximum;
    }

    internal static void CopyImportedLocations(XmlNode imported, XmlNode original, OrchestraXmlLoadResult destination, OrchestraXmlLoadResult source)
    {
        OrchestraXmlSourceLocation? location = source.GetLocation(original);
        if (location != null)
        {
            OrchestraXmlSourceLocation copied = new OrchestraXmlSourceLocation();
            copied.Source = location.Source;
            copied.Line = location.Line;
            copied.Column = location.Column;
            destination.Locations[imported] = copied;
        }
        int count = imported.ChildNodes.Count < original.ChildNodes.Count ? imported.ChildNodes.Count : original.ChildNodes.Count;
        for (int index = 0; index < count; index++)
        {
            XmlNode? importedChild = imported.ChildNodes[index];
            XmlNode? originalChild = original.ChildNodes[index];
            if (importedChild != null && originalChild != null) CopyImportedLocations(importedChild, originalChild, destination, source);
        }
    }

    internal static string CreateElementPath(XmlElement element)
    {
        List<string> segments = [];
        XmlElement? current = element;
        while (current != null)
        {
            string segment = current.LocalName;
            XmlNode? parent = current.ParentNode;
            XmlElement? parentElement = parent as XmlElement;
            if (parentElement != null)
            {
                int position = 0;
                int total = 0;
                for (int index = 0; index < parentElement.ChildNodes.Count; index++)
                {
                    XmlElement? sibling = parentElement.ChildNodes[index] as XmlElement;
                    if (sibling != null && sibling.LocalName == current.LocalName && sibling.NamespaceURI == current.NamespaceURI)
                    {
                        total++;
                        if (sibling == current) position = total;
                    }
                }
                if (total > 1) segment += "[" + position + "]";
            }
            segments.Insert(0, segment);
            current = parent as XmlElement;
        }
        StringBuilder path = new StringBuilder();
        for (int index = 0; index < segments.Count; index++)
        {
            path.Append('/');
            path.Append(segments[index]);
        }
        return path.ToString();
    }

    internal static void CopyDiagnostics(OrchestraXmlLoadResult destination, OrchestraXmlLoadResult source)
    {
        for (int index = 0; index < source.Diagnostics.Count; index++) destination.Diagnostics.Add(source.Diagnostics[index]);
    }

    internal static void AddElementDiagnostic(OrchestraXmlLoadResult result, XmlElement element, ValidationSeverity severity, string code, string message)
    {
        OrchestraXmlSourceLocation? location = result.GetLocation(element);
        string source = location == null ? "" : location.Source;
        int line = location == null ? 0 : location.Line;
        int column = location == null ? 0 : location.Column;
        AddDiagnostic(result, severity, code, message, source, line, column, CreateElementPath(element));
    }

    internal static void AddDiagnostic(OrchestraXmlLoadResult result, ValidationSeverity severity, string code, string message, string source, int line, int column, string path)
    {
        OrchestraDiagnostic diagnostic = new OrchestraDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Source = source;
        diagnostic.Line = line;
        diagnostic.Column = column;
        diagnostic.ElementPath = path;
        result.Diagnostics.Add(diagnostic);
    }

    internal static OrchestraXmlLoadResult Complete(OrchestraXmlLoadResult result)
    {
        result.Success = true;
        for (int index = 0; index < result.Diagnostics.Count; index++)
        {
            if (result.Diagnostics[index].Severity == ValidationSeverity.Error || result.Diagnostics[index].Severity == ValidationSeverity.Fatal) result.Success = false;
        }
        return result;
    }

    internal static int ReadLine(IXmlLineInfo line)
    {
        return line.HasLineInfo() ? line.LineNumber : 0;
    }

    internal static int ReadColumn(IXmlLineInfo line)
    {
        return line.HasLineInfo() ? line.LinePosition : 0;
    }
}
