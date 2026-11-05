namespace uspto;

internal class XmlRecord
{
    internal long RecordNumber = 0L;
    internal string LocalName = string.Empty;
    internal string NamespaceUri = string.Empty;
    internal string Prefix = string.Empty;
    internal string Path = "/";
    internal int StartLineNumber = 0;
    internal int StartLinePosition = 0;
    internal bool HasLineInfo = false;
    internal SecureXmlAttribute[] Attributes = new SecureXmlAttribute[0];
    internal string RawXml = string.Empty;

    internal SecureXmlAttribute ReadAttribute(string localName, string? namespaceUri)
    {
        if (localName == null)
        {
            throw new ValidationException("An XML attribute local name is required.", "localName", "REQUIRED", null);
        }

        foreach (SecureXmlAttribute attribute in Attributes)
        {
            bool namespaceMatches = namespaceUri == null || attribute.NamespaceUri.Equals(namespaceUri, global::System.StringComparison.Ordinal);
            if (namespaceMatches && attribute.LocalName.Equals(localName, global::System.StringComparison.Ordinal))
            {
                return attribute;
            }
        }

        SecureXmlAttribute missing = new SecureXmlAttribute();
        missing.Path = Path + "/@" + localName;
        return missing;
    }
}

internal class XmlRecordReader
{
    internal SecureXmlReader Reader;
    internal string RootLocalName;
    internal string? RootNamespaceUri;
    internal string RecordLocalName;
    internal string? RecordNamespaceUri;
    internal string? SourceName;
    internal int MaximumBatchRecords;
    internal int MaximumRawCharacters;
    internal global::System.Threading.CancellationToken CancellationToken;
    internal long RecordNumber = 0L;
    internal int RootDepth = -1;
    internal string ActualRootNamespaceUri = string.Empty;
    internal bool RootFound = false;
    internal bool RootClosed = false;
    internal bool EndOfInput = false;

    internal XmlRecordReader(global::System.IO.Stream input, string rootLocalName, string? rootNamespaceUri, string recordLocalName, string? recordNamespaceUri, string? sourceName, UsptoLimits limits, global::System.Threading.CancellationToken cancellationToken)
    {
        if (global::System.String.IsNullOrWhiteSpace(rootLocalName))
        {
            throw new ValidationException("An XML root local name is required.", "rootLocalName", "REQUIRED", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(recordLocalName))
        {
            throw new ValidationException("An XML record local name is required.", "recordLocalName", "REQUIRED", null);
        }

        if (limits == null)
        {
            throw new ValidationException("XML record parsing limits are required.", "limits", "REQUIRED", null);
        }

        if (limits.MaximumResults <= 0)
        {
            throw new ValidationException("The maximum XML batch size must be positive.", "MaximumResults", "INVALID_LIMIT", null);
        }

        if (limits.MaximumRawResponseBytes <= 0L || limits.MaximumRawResponseBytes > global::System.Int32.MaxValue)
        {
            throw new ValidationException("The maximum raw XML record length must fit in a supported string.", "MaximumRawResponseBytes", "INVALID_LIMIT", null);
        }

        RootLocalName = rootLocalName;
        RootNamespaceUri = rootNamespaceUri;
        RecordLocalName = recordLocalName;
        RecordNamespaceUri = recordNamespaceUri;
        SourceName = sourceName;
        MaximumBatchRecords = limits.MaximumResults;
        MaximumRawCharacters = (int)limits.MaximumRawResponseBytes;
        CancellationToken = cancellationToken;
        Reader = new SecureXmlReader(input, sourceName, limits, false);
        Reader.CancellationToken = cancellationToken;
    }

    internal XmlRecord? ReadRecord()
    {
        CancellationToken.ThrowIfCancellationRequested();
        if (EndOfInput)
        {
            return null;
        }

        while (Reader.Read())
        {
            SecureXmlNode node = Reader.Current;
            if (!RootFound)
            {
                if (node.NodeType != global::System.Xml.XmlNodeType.Element)
                {
                    continue;
                }

                if (!MatchesElement(node, RootLocalName, RootNamespaceUri))
                {
                    ThrowRootMismatch(node);
                }

                RootFound = true;
                RootDepth = node.Depth;
                ActualRootNamespaceUri = node.NamespaceUri;
                if (node.IsEmptyElement)
                {
                    RootClosed = true;
                }

                continue;
            }

            if (RootClosed)
            {
                if (node.NodeType == global::System.Xml.XmlNodeType.Element)
                {
                    ThrowTrailingRoot(node);
                }

                continue;
            }

            if (node.NodeType == global::System.Xml.XmlNodeType.EndElement && node.Depth == RootDepth)
            {
                RootClosed = true;
                continue;
            }

            if (node.NodeType == global::System.Xml.XmlNodeType.Element && node.Depth > RootDepth && MatchesElement(node, RecordLocalName, RecordNamespaceUri))
            {
                return CaptureRecord(node);
            }
        }

        EndOfInput = true;
        if (!RootFound)
        {
            ThrowMissingRoot();
        }

        return null;
    }

    internal XmlRecord[] ReadBatch(int maximumRecords)
    {
        if (maximumRecords <= 0 || maximumRecords > MaximumBatchRecords)
        {
            throw new ValidationException("The XML batch size is outside the configured range.", "maximumRecords", "BATCH_SIZE_OUT_OF_RANGE", null);
        }

        global::System.Collections.Generic.List<XmlRecord> records = new global::System.Collections.Generic.List<XmlRecord>();
        while (records.Count < maximumRecords)
        {
            XmlRecord? record = ReadRecord();
            if (record == null)
            {
                break;
            }

            records.Add(record);
        }

        XmlRecord[] result = new XmlRecord[records.Count];
        records.CopyTo(result, 0);
        return result;
    }

    internal void Close()
    {
        Reader.Close();
    }

    internal XmlRecord CaptureRecord(SecureXmlNode firstNode)
    {
        XmlRecord record = new XmlRecord();
        record.RecordNumber = RecordNumber + 1L;
        record.LocalName = firstNode.LocalName;
        record.NamespaceUri = firstNode.NamespaceUri;
        record.Prefix = firstNode.Prefix;
        record.Path = firstNode.Path;
        record.StartLineNumber = firstNode.LineNumber;
        record.StartLinePosition = firstNode.LinePosition;
        record.HasLineInfo = firstNode.HasLineInfo;
        record.Attributes = firstNode.Attributes;

        global::System.Text.StringBuilder raw = new global::System.Text.StringBuilder();
        global::System.Xml.XmlWriterSettings settings = new global::System.Xml.XmlWriterSettings();
        settings.CheckCharacters = true;
        settings.CloseOutput = false;
        settings.ConformanceLevel = global::System.Xml.ConformanceLevel.Fragment;
        settings.Indent = false;
        settings.NamespaceHandling = global::System.Xml.NamespaceHandling.OmitDuplicates;
        settings.OmitXmlDeclaration = true;
        using (global::System.Xml.XmlWriter writer = global::System.Xml.XmlWriter.Create(raw, settings))
        {
            WriteNode(writer, firstNode);
            EnsureRawLimit(writer, raw, firstNode.Path, firstNode.LineNumber, firstNode.HasLineInfo);
            if (!firstNode.IsEmptyElement)
            {
                bool complete = false;
                while (!complete && Reader.Read())
                {
                    SecureXmlNode node = Reader.Current;
                    WriteNode(writer, node);
                    EnsureRawLimit(writer, raw, node.Path, node.LineNumber, node.HasLineInfo);
                    if (node.NodeType == global::System.Xml.XmlNodeType.EndElement && node.Depth == firstNode.Depth)
                    {
                        complete = true;
                    }
                }

                if (!complete)
                {
                    ThrowTruncatedRecord(firstNode);
                }
            }

            writer.Flush();
            record.RawXml = raw.ToString();
        }

        RecordNumber += 1L;
        return record;
    }

    internal void WriteNode(global::System.Xml.XmlWriter writer, SecureXmlNode node)
    {
        CancellationToken.ThrowIfCancellationRequested();
        if (node.NodeType == global::System.Xml.XmlNodeType.Element)
        {
            writer.WriteStartElement(node.Prefix, node.LocalName, node.NamespaceUri);
            foreach (SecureXmlAttribute attribute in node.Attributes)
            {
                writer.WriteAttributeString(attribute.Prefix, attribute.LocalName, attribute.NamespaceUri, attribute.Value);
            }

            if (node.IsEmptyElement)
            {
                writer.WriteEndElement();
            }

            return;
        }

        if (node.NodeType == global::System.Xml.XmlNodeType.EndElement)
        {
            writer.WriteFullEndElement();
        }
        else if (node.NodeType == global::System.Xml.XmlNodeType.Text)
        {
            writer.WriteString(node.Value);
        }
        else if (node.NodeType == global::System.Xml.XmlNodeType.CDATA)
        {
            writer.WriteCData(node.Value);
        }
        else if (node.NodeType == global::System.Xml.XmlNodeType.Whitespace || node.NodeType == global::System.Xml.XmlNodeType.SignificantWhitespace)
        {
            writer.WriteWhitespace(node.Value);
        }
        else if (node.NodeType == global::System.Xml.XmlNodeType.Comment)
        {
            writer.WriteComment(node.Value);
        }
        else if (node.NodeType == global::System.Xml.XmlNodeType.ProcessingInstruction)
        {
            writer.WriteProcessingInstruction(node.Name, node.Value);
        }
    }

    internal bool MatchesElement(SecureXmlNode node, string localName, string? namespaceUri)
    {
        if (!node.LocalName.Equals(localName, global::System.StringComparison.Ordinal))
        {
            return false;
        }

        return namespaceUri == null || node.NamespaceUri.Equals(namespaceUri, global::System.StringComparison.Ordinal);
    }

    internal void EnsureRawLimit(global::System.Xml.XmlWriter writer, global::System.Text.StringBuilder raw, string path, int lineNumber, bool hasLineNumber)
    {
        writer.Flush();
        if (raw.Length > MaximumRawCharacters)
        {
            throw new ParseException("An XML record exceeds the configured raw character limit.", DocumentFormat.Xml, SourceName, path, 0L, false, lineNumber, hasLineNumber, null);
        }
    }

    internal void ThrowRootMismatch(SecureXmlNode node)
    {
        throw new ParseException("The XML document root does not match the configured root name and namespace.", DocumentFormat.Xml, SourceName, node.Path, 0L, false, node.LineNumber, node.HasLineInfo, null);
    }

    internal void ThrowTrailingRoot(SecureXmlNode node)
    {
        throw new ParseException("Unexpected XML content follows the configured document root.", DocumentFormat.Xml, SourceName, node.Path, 0L, false, node.LineNumber, node.HasLineInfo, null);
    }

    internal void ThrowMissingRoot()
    {
        throw new ParseException("The XML input does not contain the configured document root.", DocumentFormat.Xml, SourceName, "/", 0L, false, 0, false, null);
    }

    internal void ThrowTruncatedRecord(SecureXmlNode node)
    {
        throw new ParseException("The XML input ended before the current record was complete.", DocumentFormat.Xml, SourceName, node.Path, 0L, false, node.LineNumber, node.HasLineInfo, null);
    }
}
