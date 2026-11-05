namespace ttf;

internal class SfntSvgDocumentData
{
    byte[] encodedBytes;
    byte[] xmlBytes;
    bool gzipCompressed;

    internal SfntSvgDocumentData(
        byte[] encodedBytes,
        byte[] xmlBytes,
        bool gzipCompressed)
    {
        this.encodedBytes = encodedBytes;
        this.xmlBytes = xmlBytes;
        this.gzipCompressed = gzipCompressed;
    }

    internal bool IsGzipCompressed()
    {
        return gzipCompressed;
    }

    internal int GetEncodedLength()
    {
        return encodedBytes.Length;
    }

    internal int GetXmlLength()
    {
        return xmlBytes.Length;
    }

    internal byte[] CopyEncodedBytes()
    {
        return encodedBytes.AsSpan().ToArray();
    }

    internal byte[] CopyXmlBytes()
    {
        return xmlBytes.AsSpan().ToArray();
    }

    internal System.Xml.XmlDocument LoadXml()
    {
        System.Xml.XmlReaderSettings settings =
            new System.Xml.XmlReaderSettings();
        settings.DtdProcessing = System.Xml.DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        settings.MaxCharactersFromEntities = 0;
        settings.MaxCharactersInDocument = xmlBytes.Length;
        settings.CloseInput = false;
        settings.CheckCharacters = true;

        System.Xml.XmlDocument document = new System.Xml.XmlDocument();
        document.XmlResolver = null;
        document.PreserveWhitespace = true;

        using (System.IO.MemoryStream input =
            new System.IO.MemoryStream(xmlBytes, false))
        {
            using (System.Xml.XmlReader reader =
                System.Xml.XmlReader.Create(input, settings))
            {
                document.Load(reader);
            }
        }

        System.Xml.XmlElement? root = document.DocumentElement;

        if (root == null ||
            root.LocalName != "svg" ||
            root.NamespaceURI != "http://www.w3.org/2000/svg")
        {
            throw FontFormatException.Create(
                "An SVG document must have an SVG root in the SVG namespace.");
        }

        ValidateSafeContent(document);

        return document;
    }

    internal void ValidateXml()
    {
        LoadXml();
    }

    static void ValidateSafeContent(System.Xml.XmlDocument document)
    {
        System.Xml.XmlNode? node = document.FirstChild;

        while (node != null)
        {
            ValidateNode(node);

            if (node.FirstChild != null)
            {
                node = node.FirstChild;
                continue;
            }

            while (node != null &&
                node != document &&
                node.NextSibling == null)
            {
                node = node.ParentNode;
            }

            if (node == null || node == document)
            {
                break;
            }

            node = node.NextSibling;
        }
    }

    static void ValidateNode(System.Xml.XmlNode node)
    {
        if (node.NodeType == System.Xml.XmlNodeType.ProcessingInstruction)
        {
            ThrowUnsafeReference();
        }

        if (node.NodeType != System.Xml.XmlNodeType.Element)
        {
            return;
        }

        System.Xml.XmlElement element = (System.Xml.XmlElement)node;

        if (string.Equals(
            element.LocalName,
            "script",
            System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                element.LocalName,
                "foreignObject",
                System.StringComparison.OrdinalIgnoreCase))
        {
            ThrowUnsafeReference();
        }

        if (string.Equals(
            element.LocalName,
            "style",
            System.StringComparison.OrdinalIgnoreCase))
        {
            ValidateCssReferences(element.InnerText);
        }

        System.Xml.XmlAttributeCollection attributes = element.Attributes;
        int index = 0;

        while (index < attributes.Count)
        {
            System.Xml.XmlAttribute attribute = attributes[index];

            if (attribute.LocalName.Length > 2 &&
                attribute.LocalName.StartsWith(
                    "on",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                ThrowUnsafeReference();
            }

            if (string.Equals(
                attribute.LocalName,
                "href",
                System.StringComparison.OrdinalIgnoreCase))
            {
                ValidateFragmentReference(attribute.Value);
            }

            if (MayContainCssReference(attribute.LocalName))
            {
                ValidateCssReferences(attribute.Value);
            }

            index += 1;
        }
    }

    static bool MayContainCssReference(string name)
    {
        return string.Equals(name, "style",
                System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "fill",
                System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "stroke",
                System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "filter",
                System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "clip-path",
                System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "mask",
                System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "cursor",
                System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "marker",
                System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "marker-start",
                System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "marker-mid",
                System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "marker-end",
                System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "src",
                System.StringComparison.OrdinalIgnoreCase);
    }

    static void ValidateFragmentReference(string value)
    {
        int start = 0;
        int end = value.Length;

        while (start < end && char.IsWhiteSpace(value[start]))
        {
            start += 1;
        }

        while (end > start && char.IsWhiteSpace(value[end - 1]))
        {
            end -= 1;
        }

        if (start == end || value[start] != '#')
        {
            ThrowUnsafeReference();
        }
    }

    static void ValidateCssReferences(string value)
    {
        if (value.IndexOf('\\') >= 0 ||
            value.IndexOf('@') >= 0 ||
            value.IndexOf(
                "expression",
                System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            ThrowUnsafeReference();
        }

        int searchStart = 0;

        while (searchStart < value.Length)
        {
            int urlStart = value.IndexOf(
                "url",
                searchStart,
                System.StringComparison.OrdinalIgnoreCase);

            if (urlStart < 0)
            {
                return;
            }

            int openParenthesis = urlStart + 3;

            while (openParenthesis < value.Length &&
                char.IsWhiteSpace(value[openParenthesis]))
            {
                openParenthesis += 1;
            }

            if (openParenthesis >= value.Length ||
                value[openParenthesis] != '(')
            {
                searchStart = urlStart + 3;
                continue;
            }

            int closeParenthesis = value.IndexOf(')', openParenthesis + 1);

            if (closeParenthesis < 0)
            {
                ThrowUnsafeReference();
            }

            int referenceStart = openParenthesis + 1;
            int referenceEnd = closeParenthesis;

            while (referenceStart < referenceEnd &&
                char.IsWhiteSpace(value[referenceStart]))
            {
                referenceStart += 1;
            }

            while (referenceEnd > referenceStart &&
                char.IsWhiteSpace(value[referenceEnd - 1]))
            {
                referenceEnd -= 1;
            }

            if (referenceStart < referenceEnd &&
                (value[referenceStart] == '\'' ||
                    value[referenceStart] == '"'))
            {
                char quote = value[referenceStart];
                referenceStart += 1;

                if (referenceEnd <= referenceStart ||
                    value[referenceEnd - 1] != quote)
                {
                    ThrowUnsafeReference();
                }

                referenceEnd -= 1;
            }

            while (referenceStart < referenceEnd &&
                char.IsWhiteSpace(value[referenceStart]))
            {
                referenceStart += 1;
            }

            while (referenceEnd > referenceStart &&
                char.IsWhiteSpace(value[referenceEnd - 1]))
            {
                referenceEnd -= 1;
            }

            if (referenceStart == referenceEnd ||
                value[referenceStart] != '#')
            {
                ThrowUnsafeReference();
            }

            searchStart = closeParenthesis + 1;
        }
    }

    static void ThrowUnsafeReference()
    {
        throw FontFormatException.Create(
            "An SVG document contains unsafe active or external content.");
    }
}

internal class SfntSvgDocument
{
    int startGlyphId;
    int endGlyphId;
    int documentOffset;
    SfntSvgDocumentData data;

    internal SfntSvgDocument(
        int startGlyphId,
        int endGlyphId,
        int documentOffset,
        SfntSvgDocumentData data)
    {
        this.startGlyphId = startGlyphId;
        this.endGlyphId = endGlyphId;
        this.documentOffset = documentOffset;
        this.data = data;
    }

    internal int GetStartGlyphId()
    {
        return startGlyphId;
    }

    internal int GetEndGlyphId()
    {
        return endGlyphId;
    }

    internal int GetDocumentOffset()
    {
        return documentOffset;
    }

    internal bool CoversGlyph(int glyphId)
    {
        return glyphId >= startGlyphId && glyphId <= endGlyphId;
    }

    internal bool IsGzipCompressed()
    {
        return data.IsGzipCompressed();
    }

    internal int GetEncodedLength()
    {
        return data.GetEncodedLength();
    }

    internal int GetXmlLength()
    {
        return data.GetXmlLength();
    }

    internal byte[] CopyEncodedBytes()
    {
        return data.CopyEncodedBytes();
    }

    internal byte[] CopyXmlBytes()
    {
        return data.CopyXmlBytes();
    }

    internal System.Xml.XmlDocument LoadXml()
    {
        return data.LoadXml();
    }
}

internal class SfntSvgTable
{
    int glyphCount;
    SfntSvgDocument[] documents;

    internal SfntSvgTable(int glyphCount, SfntSvgDocument[] documents)
    {
        this.glyphCount = glyphCount;
        this.documents = documents;
    }

    internal int GetDocumentCount()
    {
        return documents.Length;
    }

    internal SfntSvgDocument GetDocument(int index)
    {
        if (index < 0 || index >= documents.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return documents[index];
    }

    internal SfntSvgDocument? FindDocument(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphCount)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }

        int low = 0;
        int high = documents.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            SfntSvgDocument candidate = documents[middle];

            if (glyphId < candidate.GetStartGlyphId())
            {
                high = middle - 1;
            }
            else if (glyphId > candidate.GetEndGlyphId())
            {
                low = middle + 1;
            }
            else
            {
                return candidate;
            }
        }

        return null;
    }
}

internal class SfntSvgParsingBudget
{
    int maximumDecodedBytes;
    long decodedBytes;

    internal SfntSvgParsingBudget(FontLimits limits)
    {
        maximumDecodedBytes = limits.GetMaxDecompressedBytes();
    }

    internal void AddDecodedBytes(int count)
    {
        decodedBytes += count;

        if (decodedBytes > maximumDecodedBytes)
        {
            throw FontLimitException.Create(
                "SVG documents exceed the configured cumulative decoded-byte limit.");
        }
    }
}

internal static class SfntSvgTableParser
{
    const int HeaderLength = 10;
    const int RecordLength = 12;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Svg,
        0,
        0,
        0,
        ReadVersion,
        Parse);

    internal static SfntTableParser GetParser()
    {
        return parser;
    }

    internal static SfntParsedTableView? FindView(SfntTableCache cache)
    {
        if (cache == null)
        {
            throw new System.ArgumentNullException("cache");
        }

        return cache.FindParsedTable(parser);
    }

    internal static SfntSvgTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntSvgTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        return new SfntTableVersion(reader.ReadUInt16(), 0);
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        FontDataSlice tableData = table.GetDataSlice();

        if (tableData.GetLength() < HeaderLength)
        {
            throw FontFormatException.Create("The SVG table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int encodedVersion = reader.ReadUInt16();
        uint listOffsetValue = reader.ReadUInt32();
        uint reserved = reader.ReadUInt32();

        if (encodedVersion != 0)
        {
            throw FontFormatException.Create("The SVG table version is not supported.");
        }

        if (reserved != 0)
        {
            throw FontFormatException.Create("The SVG table reserved field is not zero.");
        }

        int listOffset = ConvertRequiredOffset(
            listOffsetValue,
            tableData.GetLength(),
            "The SVG document-list offset is invalid.");
        FontDataSlice listData = tableData.GetSlice(
            listOffset,
            tableData.GetLength() - listOffset);
        BigEndianReader listReader = new BigEndianReader(listData);
        int documentCount = listReader.ReadUInt16();

        if (documentCount == 0)
        {
            throw FontFormatException.Create("The SVG document list is empty.");
        }

        if (documentCount > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "The SVG document count exceeds the configured coverage-entry limit.");
        }

        int recordsLength = CheckedFontMath.AddOffset(
            2,
            CheckedFontMath.MultiplyCount(documentCount, RecordLength));

        if (recordsLength > listData.GetLength())
        {
            throw FontFormatException.Create("The SVG document list is truncated.");
        }

        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);

        if (maxp == null)
        {
            throw FontFormatException.Create(
                "The SVG table requires a parsed maxp table.");
        }

        SfntSvgDocument[] documents = new SfntSvgDocument[documentCount];
        System.Collections.Generic.Dictionary<long, SfntSvgDocumentData> dataCache =
            new System.Collections.Generic.Dictionary<long, SfntSvgDocumentData>();
        SfntSvgParsingBudget budget = new SfntSvgParsingBudget(limits);
        int previousEndGlyphId = -1;
        int index = 0;

        while (index < documents.Length)
        {
            int startGlyphId = listReader.ReadUInt16();
            int endGlyphId = listReader.ReadUInt16();
            uint relativeOffset = listReader.ReadUInt32();
            uint documentLengthValue = listReader.ReadUInt32();

            if (startGlyphId > endGlyphId || startGlyphId <= previousEndGlyphId)
            {
                throw FontFormatException.Create(
                    "SVG document glyph ranges are invalid or overlap.");
            }

            if (endGlyphId >= maxp.GetGlyphCount())
            {
                throw FontFormatException.Create(
                    "An SVG document range references a missing glyph.");
            }

            int documentLength = ConvertRequiredLength(
                documentLengthValue,
                "An SVG document length is invalid.");
            int documentOffset = ResolveDocumentOffset(
                tableData.GetLength(),
                listOffset,
                relativeOffset,
                documentLength);
            long cacheKey = ((long)documentOffset << 32) |
                (uint)documentLength;
            SfntSvgDocumentData documentData;

            if (dataCache.ContainsKey(cacheKey))
            {
                documentData = dataCache[cacheKey];
            }
            else
            {
                byte[] encodedBytes = tableData.GetSlice(
                    documentOffset,
                    documentLength).CopyBytes();
                documentData = DecodeDocument(encodedBytes, limits, budget);
                dataCache.Add(cacheKey, documentData);
            }

            documents[index] = new SfntSvgDocument(
                startGlyphId,
                endGlyphId,
                documentOffset,
                documentData);
            previousEndGlyphId = endGlyphId;
            index += 1;
        }

        return new SfntSvgTable(maxp.GetGlyphCount(), documents);
    }

    static SfntSvgDocumentData DecodeDocument(
        byte[] encodedBytes,
        FontLimits limits,
        SfntSvgParsingBudget budget)
    {
        bool gzipCompressed = encodedBytes.Length >= 2 &&
            encodedBytes[0] == 0x1F && encodedBytes[1] == 0x8B;
        byte[] xmlBytes;

        if (gzipCompressed)
        {
            ValidateGzipExpandedLength(encodedBytes, limits);
            xmlBytes = BoundedInflater.InflateGzip(encodedBytes, limits);
        }
        else
        {
            if (encodedBytes.Length > limits.GetMaxStringBytes())
            {
                throw FontLimitException.Create(
                    "An SVG document exceeds the configured string-byte limit.");
            }

            xmlBytes = encodedBytes.AsSpan().ToArray();
        }

        if (xmlBytes.Length > limits.GetMaxStringBytes())
        {
            throw FontLimitException.Create(
                "An expanded SVG document exceeds the configured string-byte limit.");
        }

        budget.AddDecodedBytes(xmlBytes.Length);
        SfntSvgDocumentData document = new SfntSvgDocumentData(
            encodedBytes,
            xmlBytes,
            gzipCompressed);
        document.ValidateXml();
        return document;
    }

    static void ValidateGzipExpandedLength(
        byte[] encodedBytes,
        FontLimits limits)
    {
        if (encodedBytes.Length < 4)
        {
            throw FontFormatException.Create("An SVG gzip document is truncated.");
        }

        int offset = encodedBytes.Length - 4;
        uint length = encodedBytes[offset] |
            ((uint)encodedBytes[offset + 1] << 8) |
            ((uint)encodedBytes[offset + 2] << 16) |
            ((uint)encodedBytes[offset + 3] << 24);

        if (length > limits.GetMaxStringBytes())
        {
            throw FontLimitException.Create(
                "An expanded SVG document exceeds the configured string-byte limit.");
        }
    }

    static int ConvertRequiredOffset(
        uint value,
        int tableLength,
        string message)
    {
        if (value == 0 || value > int.MaxValue || value >= tableLength)
        {
            throw FontFormatException.Create(message);
        }

        return (int)value;
    }

    static int ConvertRequiredLength(uint value, string message)
    {
        if (value == 0 || value > int.MaxValue)
        {
            throw FontFormatException.Create(message);
        }

        return (int)value;
    }

    static int ResolveDocumentOffset(
        int tableLength,
        int listOffset,
        uint relativeOffset,
        int documentLength)
    {
        if (relativeOffset == 0 || relativeOffset > int.MaxValue)
        {
            throw FontFormatException.Create("An SVG document offset is invalid.");
        }

        long documentOffset = (long)listOffset + relativeOffset;
        long documentEnd = documentOffset + documentLength;

        if (documentOffset > int.MaxValue || documentEnd > tableLength)
        {
            throw FontFormatException.Create(
                "An SVG document is outside the SVG table.");
        }

        return (int)documentOffset;
    }
}
