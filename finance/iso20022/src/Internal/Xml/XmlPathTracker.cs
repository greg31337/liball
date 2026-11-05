using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace iso20022;

internal class XmlPathTracker
{
    List<QualifiedNameData> names = new List<QualifiedNameData>();
    List<SourceLocation> locations = new List<SourceLocation>();

    internal XmlPathTracker()
    {
    }

    internal void EnterElement(QualifiedNameData? name, SourceLocation? location)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (location == null)
        {
            throw new ArgumentNullException("location");
        }

        ValidateName(name);
        names.Add(CopyName(name));
        locations.Add(CopyLocation(location));
    }

    internal void EnterReaderElement(BoundedXmlReader? reader, string? sourceName)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (reader.GetNodeType() != XmlNodeType.Element)
        {
            throw new InvalidOperationException("The bounded reader is not positioned on an element.");
        }

        QualifiedNameData name = new QualifiedNameData();
        name.NamespaceUri = reader.GetNamespaceUri();
        name.LocalName = reader.GetLocalName();
        name.Prefix = reader.GetPrefix();
        EnterElement(name, reader.CaptureLocation(sourceName));
    }

    internal void ExitElement()
    {
        if (names.Count == 0)
        {
            throw new InvalidOperationException("No XML path element is available to exit.");
        }

        names.RemoveAt(names.Count - 1);
        locations.RemoveAt(locations.Count - 1);
    }

    internal XmlPathData CapturePath()
    {
        XmlPathData path = new XmlPathData();
        StringBuilder display = new StringBuilder();
        int index = 0;
        while (index < names.Count)
        {
            QualifiedNameData name = names[index];
            path.Segments.Add(CopyName(name));
            display.Append('/');
            if (name.NamespaceUri.Length > 0)
            {
                display.Append("Q{");
                display.Append(name.NamespaceUri);
                display.Append('}');
            }

            display.Append(name.LocalName);
            index++;
        }

        path.Value = display.ToString();
        return path;
    }

    internal SourceLocation? CaptureCurrentLocation()
    {
        if (locations.Count == 0)
        {
            return null;
        }

        return CopyLocation(locations[locations.Count - 1]);
    }

    internal int GetPathDepth()
    {
        return names.Count;
    }

    internal static void ValidateName(QualifiedNameData name)
    {
        if (!XmlNameLexical.IsNcName(name.LocalName.AsSpan()))
        {
            throw new XmlException("An XML path segment must have a valid local name.");
        }

        if (name.Prefix.Length > 0 && !XmlNameLexical.IsNcName(name.Prefix.AsSpan()))
        {
            throw new XmlException("An XML path segment must have a valid prefix.");
        }

        if (name.Prefix.Length > 0 && name.NamespaceUri.Length == 0)
        {
            throw new XmlException("A prefixed XML path segment must have a namespace URI.");
        }

        if (name.NamespaceUri.Length > 0 && !UriLexical.IsNamespaceUri(name.NamespaceUri.AsSpan()))
        {
            throw new XmlException("An XML path segment must have a valid namespace URI.");
        }
    }

    internal static QualifiedNameData CopyName(QualifiedNameData name)
    {
        QualifiedNameData copy = new QualifiedNameData();
        copy.NamespaceUri = name.NamespaceUri;
        copy.LocalName = name.LocalName;
        copy.Prefix = name.Prefix;
        return copy;
    }

    internal static SourceLocation CopyLocation(SourceLocation location)
    {
        SourceLocation copy = new SourceLocation();
        copy.SourceName = location.SourceName;
        copy.HasLineInformation = location.HasLineInformation;
        copy.LineNumber = location.LineNumber;
        copy.ColumnNumber = location.ColumnNumber;
        copy.HasCharacterOffset = location.HasCharacterOffset;
        copy.CharacterOffset = location.CharacterOffset;
        return copy;
    }
}
