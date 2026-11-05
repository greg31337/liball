using System;
using System.Collections.Generic;

namespace xml;

/// <summary>Provides the baseline XML byte collector.</summary>
public class XmlByteCollector : IXmlByteSink, IXmlByteCollector
{
    internal const int DefaultMaximumByteCount = 16777216;
    internal int Count;
    internal List<byte> Bytes;
    internal int MaximumByteCount;

    /// <summary>Creates an XML byte collector.</summary>
    public XmlByteCollector()
    {
        Count = 0;
        Bytes = new List<byte>();
        MaximumByteCount = DefaultMaximumByteCount;
    }

    /// <summary>Accepts a serialized byte block.</summary>
    public void WriteXmlBytes(ReadOnlyMemory<byte> bytes)
    {
        if (bytes.Length > MaximumByteCount - Count)
        {
            throw CreateXmlByteCollectionLimit();
        }

        int index = 0;
        while (index < bytes.Length)
        {
            Bytes.Add(bytes.Span[index]);
            index = index + 1;
        }

        Count = Count + bytes.Length;
    }

    /// <summary>Sets the maximum retained byte count and clears retained bytes.</summary>
    public void ConfigureXmlByteCollection(int maximumCount)
    {
        Guard.Positive(maximumCount, "maximumCount");
        Bytes.Clear();
        Count = 0;
        MaximumByteCount = maximumCount;
    }

    /// <summary>Returns a snapshot of retained bytes in arrival order.</summary>
    public byte[] SnapshotCollectedXmlBytes()
    {
        return Bytes.ToArray();
    }

    /// <summary>Returns the retained byte count.</summary>
    public int CountCollectedXmlBytes()
    {
        return Count;
    }

    /// <summary>Clears retained bytes while preserving the configured maximum.</summary>
    public void ClearCollectedXmlBytes()
    {
        Bytes.Clear();
        Count = 0;
    }

    /// <summary>Clears retained bytes and restores the default maximum.</summary>
    public void ResetXmlByteCollection()
    {
        Bytes.Clear();
        Count = 0;
        MaximumByteCount = DefaultMaximumByteCount;
    }

    internal XmlLimitException CreateXmlByteCollectionLimit()
    {
        string message = "The XML byte collector limit of " + MaximumByteCount + " bytes was exceeded.";
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.LimitExceeded;
        diagnostic.Category = XmlErrorCategory.Limit;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XmlLimitException(message, diagnostic, null);
    }
}
