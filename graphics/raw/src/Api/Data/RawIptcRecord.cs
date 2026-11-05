namespace raw;

/// <summary>Represents one exact IPTC IIM DataSet with a typed interpretation when its definition is known.</summary>
public class RawIptcDataset
{
    /// <summary>The IIM record number.</summary>
    public byte RecordNumber;
    /// <summary>The IIM DataSet number.</summary>
    public byte DatasetNumber;
    /// <summary>The standardized DataSet name, or a stable numeric fallback.</summary>
    public string Name;
    /// <summary>The recognized semantic value form.</summary>
    public RawIptcValueKind ValueKind;
    /// <summary>The exact DataSet value bytes without its tag and length.</summary>
    public byte[] RawBytes;
    /// <summary>The decoded text for text, date, and time values, or an empty string.</summary>
    public string Text;
    /// <summary>The character encoding used for a textual value.</summary>
    public RawTextEncoding Encoding;
    /// <summary>The decoded unsigned integer, or zero for other value forms.</summary>
    public ulong UnsignedValue;
    /// <summary>The decoded year, or zero when the value is not a valid date.</summary>
    public int Year;
    /// <summary>The decoded month, or zero when the value is not a valid date.</summary>
    public int Month;
    /// <summary>The decoded day, or zero when the value is not a valid date.</summary>
    public int Day;
    /// <summary>The decoded hour, or zero when the value is not a valid time.</summary>
    public int Hour;
    /// <summary>The decoded minute, or zero when the value is not a valid time.</summary>
    public int Minute;
    /// <summary>The decoded second, or zero when the value is not a valid time.</summary>
    public int Second;
    /// <summary>Whether a decoded time contains an explicit UTC offset.</summary>
    public bool HasUtcOffset;
    /// <summary>The signed decoded UTC offset in minutes.</summary>
    public int UtcOffsetMinutes;
    /// <summary>Whether the typed interpretation is valid.</summary>
    public bool Valid;

    internal RawIptcDataset(byte recordNumber, byte datasetNumber, string name, RawIptcValueKind valueKind, byte[] rawBytes, string text, RawTextEncoding encoding, ulong unsignedValue, int year, int month, int day, int hour, int minute, int second, bool hasUtcOffset, int utcOffsetMinutes, bool valid)
    {
        RecordNumber = recordNumber;
        DatasetNumber = datasetNumber;
        Name = name;
        ValueKind = valueKind;
        RawBytes = RawTextValue.Copy(rawBytes);
        Text = text;
        Encoding = encoding;
        UnsignedValue = unsignedValue;
        Year = year;
        Month = month;
        Day = day;
        Hour = hour;
        Minute = minute;
        Second = second;
        HasUtcOffset = hasUtcOffset;
        UtcOffsetMinutes = utcOffsetMinutes;
        Valid = valid;
    }

    /// <summary>Creates an uninterpreted binary DataSet.</summary>
    /// <param name="recordNumber">The IIM record number.</param>
    /// <param name="datasetNumber">The IIM DataSet number.</param>
    /// <param name="bytes">The exact value bytes.</param>
    /// <returns>The created DataSet.</returns>
    /// <exception cref="RawFormatException">A record number or value is invalid.</exception>
    internal static RawIptcDataset CreateBinary(byte recordNumber, byte datasetNumber, byte[] bytes)
    {
        return IptcCodec.CreateBinary(recordNumber, datasetNumber, bytes);
    }

    /// <summary>Creates a character DataSet encoded as UTF-8 or seven-bit ASCII.</summary>
    /// <param name="recordNumber">The IIM record number.</param>
    /// <param name="datasetNumber">The IIM DataSet number.</param>
    /// <param name="text">The text value.</param>
    /// <param name="utf8">Whether to encode UTF-8 instead of seven-bit ASCII.</param>
    /// <returns>The created DataSet.</returns>
    /// <exception cref="RawFormatException">The record, text, encoding, or known value form is invalid.</exception>
    internal static RawIptcDataset CreateText(byte recordNumber, byte datasetNumber, string text, bool utf8)
    {
        return IptcCodec.CreateText(recordNumber, datasetNumber, text, utf8);
    }

    /// <summary>Creates a two-octet unsigned integer DataSet.</summary>
    /// <param name="recordNumber">The IIM record number.</param>
    /// <param name="datasetNumber">The IIM DataSet number.</param>
    /// <param name="value">The unsigned value.</param>
    /// <returns>The created DataSet.</returns>
    /// <exception cref="RawFormatException">The record or DataSet does not have the requested value form.</exception>
    internal static RawIptcDataset CreateUnsignedInteger(byte recordNumber, byte datasetNumber, ushort value)
    {
        return IptcCodec.CreateUnsignedInteger(recordNumber, datasetNumber, value);
    }

    /// <summary>Creates an eight-digit date DataSet.</summary>
    /// <param name="recordNumber">The IIM record number.</param>
    /// <param name="datasetNumber">The IIM DataSet number.</param>
    /// <param name="value">The calendar date.</param>
    /// <returns>The created DataSet.</returns>
    /// <exception cref="RawFormatException">The record or DataSet does not have the requested value form.</exception>
    internal static RawIptcDataset CreateDate(byte recordNumber, byte datasetNumber, System.DateTime value)
    {
        return IptcCodec.CreateDate(recordNumber, datasetNumber, value);
    }

    /// <summary>Creates an IPTC time DataSet.</summary>
    /// <param name="recordNumber">The IIM record number.</param>
    /// <param name="datasetNumber">The IIM DataSet number.</param>
    /// <param name="hour">The local hour from zero through twenty-four.</param>
    /// <param name="minute">The minute.</param>
    /// <param name="second">The second.</param>
    /// <param name="hasUtcOffset">Whether to append an explicit UTC offset.</param>
    /// <param name="utcOffsetMinutes">The signed UTC offset in minutes.</param>
    /// <returns>The created DataSet.</returns>
    /// <exception cref="RawFormatException">The record, DataSet, time, or offset is invalid.</exception>
    internal static RawIptcDataset CreateTime(byte recordNumber, byte datasetNumber, int hour, int minute, int second, bool hasUtcOffset, int utcOffsetMinutes)
    {
        return IptcCodec.CreateTime(recordNumber, datasetNumber, hour, minute, second, hasUtcOffset, utcOffsetMinutes);
    }

    /// <summary>Returns an independent copy of the exact value bytes.</summary>
    /// <returns>The copied bytes.</returns>
    internal byte[] CopyBytes()
    {
        return RawTextValue.Copy(RawBytes);
    }
}

/// <summary>Retains one exact IPTC IIM payload and its ordered typed DataSets.</summary>
public class RawIptcRecord
{
    /// <summary>The exact encoded IIM payload bytes.</summary>
    public byte[]? RawBytes;
    /// <summary>Trailing zero padding retained after the final DataSet.</summary>
    public byte[]? PaddingBytes;
    /// <summary>The ordered decoded DataSets.</summary>
    public RawIptcDataset[]? Datasets;
    /// <summary>Whether DataSet 1:90 announces Unicode UTF-8 for character DataSets.</summary>
    public bool Utf8;
    /// <summary>The absolute payload offset in the containing source, or <c>-1</c>.</summary>
    public long Offset;
    /// <summary>The containing stable IFD path, or an empty string.</summary>
    public string? IfdPath;

    /// <summary>Parses a bounded standalone IPTC IIM payload.</summary>
    /// <param name="rawBytes">The exact payload bytes.</param>
    /// <param name="limits">The resource limits.</param>
    /// <exception cref="RawFormatException">The payload or a DataSet is malformed.</exception>
    /// <exception cref="RawLimitException">The payload exceeds a configured resource limit.</exception>
    /// <returns>The parsed record.</returns>
    internal static RawIptcRecord Parse(byte[] rawBytes, RawLimits limits)
    {
        return IptcCodec.Parse(rawBytes, limits, -1, string.Empty);
    }

    internal RawIptcRecord(byte[] rawBytes, byte[] paddingBytes, RawIptcDataset[] datasets, bool utf8, long offset, string ifdPath)
    {
        RawBytes = rawBytes;
        PaddingBytes = paddingBytes;
        Datasets = datasets;
        Utf8 = utf8;
        Offset = offset;
        IfdPath = ifdPath;
    }

    /// <summary>Creates and validates a canonical IIM payload from ordered DataSets.</summary>
    /// <param name="datasets">The ordered DataSets.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>The created record.</returns>
    /// <exception cref="RawFormatException">A DataSet or its character-set declaration is inconsistent.</exception>
    /// <exception cref="RawLimitException">The encoded payload exceeds a configured resource limit.</exception>
    internal static RawIptcRecord Create(RawIptcDataset[] datasets, RawLimits limits)
    {
        byte[] bytes = IptcCodec.Encode(datasets, limits);
        return IptcCodec.Parse(bytes, limits, -1, string.Empty);
    }

    /// <summary>Returns an independent copy of the exact original payload, including padding.</summary>
    /// <returns>The copied payload.</returns>
    internal byte[] CopyBytes()
    {
        return RawTextValue.Copy(RawBytes);
    }

    /// <summary>Encodes the current ordered DataSets using canonical IIM lengths.</summary>
    /// <param name="limits">The resource limits.</param>
    /// <returns>The canonical encoded payload without container padding.</returns>
    /// <exception cref="RawFormatException">A DataSet is invalid.</exception>
    /// <exception cref="RawLimitException">The encoded payload exceeds a configured resource limit.</exception>
    internal byte[] Encode(RawLimits limits)
    {
        return IptcCodec.Encode(Datasets, limits);
    }

    /// <summary>Creates a TIFF UNDEFINED value suitable for tag 33723.</summary>
    /// <param name="limits">The resource limits.</param>
    /// <returns>The encoded typed tag value.</returns>
    internal RawTagValue ToTagValue(RawLimits limits)
    {
        return RawTagValue.CreateUndefined(Encode(limits));
    }

    /// <summary>Finds the first matching DataSet.</summary>
    /// <param name="recordNumber">The IIM record number.</param>
    /// <param name="datasetNumber">The IIM DataSet number.</param>
    /// <returns>The first match, or <c>null</c>.</returns>
    internal RawIptcDataset? GetDataset(byte recordNumber, byte datasetNumber)
    {
        int index = 0;
        while (index < (Datasets ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            RawIptcDataset dataset = Datasets[index];
            if (dataset != null && dataset.RecordNumber == recordNumber && dataset.DatasetNumber == datasetNumber)
            {
                return dataset;
            }
            index++;
        }
        return null;
    }

    /// <summary>Returns every matching repeatable DataSet in encounter order.</summary>
    /// <param name="recordNumber">The IIM record number.</param>
    /// <param name="datasetNumber">The IIM DataSet number.</param>
    /// <returns>The matching DataSets.</returns>
    internal RawIptcDataset[] GetDatasets(byte recordNumber, byte datasetNumber)
    {
        System.Collections.Generic.List<RawIptcDataset> matches = new System.Collections.Generic.List<RawIptcDataset>();
        int index = 0;
        while (index < (Datasets ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            RawIptcDataset dataset = Datasets[index];
            if (dataset != null && dataset.RecordNumber == recordNumber && dataset.DatasetNumber == datasetNumber)
            {
                matches.Add(dataset);
            }
            index++;
        }
        return matches.ToArray();
    }

}

/// <summary>Provides an IPTC Core or Extension view of one field extracted from XMP.</summary>
public class RawIptcXmpField
{
    /// <summary>The IPTC schema represented by the field.</summary>
    public RawIptcSchema Schema;
    /// <summary>The source XMP namespace URI.</summary>
    public string NamespaceUri;
    /// <summary>The source XMP namespace prefix.</summary>
    public string Prefix;
    /// <summary>The source XMP local property name.</summary>
    public string Name;
    /// <summary>The decoded value.</summary>
    public string Value;
    /// <summary>The inherited XML language, or an empty string.</summary>
    public string Language;
    /// <summary>Whether the source value was an RDF description attribute.</summary>
    public bool Attribute;
    /// <summary>The stable source XML path.</summary>
    public string Path;
    /// <summary>The zero-based source XMP packet index.</summary>
    public int PacketIndex;

    /// <summary>Creates an IPTC view over an extracted XMP field.</summary>
    /// <param name="schema">The represented IPTC schema.</param>
    /// <param name="field">The source XMP field.</param>
    /// <param name="packetIndex">The source packet index.</param>
    public RawIptcXmpField(RawIptcSchema schema, RawXmpField field, int packetIndex)
    {
        Schema = schema;
        NamespaceUri = field == null ? string.Empty : field.NamespaceUri;
        Prefix = field == null ? string.Empty : field.Prefix;
        Name = field == null ? string.Empty : field.Name;
        Value = field == null ? string.Empty : field.Value;
        Language = field == null ? string.Empty : field.Language;
        Attribute = field != null && field.Attribute;
        Path = field == null ? string.Empty : field.Path;
        PacketIndex = packetIndex;
    }
}
