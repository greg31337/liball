using System;

namespace html;
/// <summary>Reports the encoding selected by HTML byte sniffing and the signal that selected it.</summary>
public class HtmlEncodingDetectionResult : IHtmlEncodingDetectionResult
{
    internal string encodingName;
    internal HtmlEncodingConfidence confidence;
    internal HtmlEncodingSource source;
    internal int bomLength;
    internal int declarationByteOffset;
    internal HtmlEncodingDetectionResult(string encodingName, HtmlEncodingConfidence confidence, HtmlEncodingSource source, int bomLength, int declarationByteOffset)
    {
        if (encodingName == null)
        {
            throw new ArgumentNullException("encodingName");
        }

        if (encodingName.Length == 0)
        {
            throw new ArgumentException("An encoding name is required.", "encodingName");
        }

        if (bomLength < 0 || declarationByteOffset < -1)
        {
            throw new ArgumentOutOfRangeException("bomLength");
        }

        this.encodingName = encodingName;
        this.confidence = confidence;
        this.source = source;
        this.bomLength = bomLength;
        this.declarationByteOffset = declarationByteOffset;
    }

    /// <summary>Gets the canonical web encoding name selected for decoding.</summary>
    /// <returns>A canonical name such as <c>UTF-8</c> or <c>windows-1252</c>.</returns>
    public string GetEncodingName()
    {
        return this.encodingName;
    }

    /// <summary>Reports whether later input is allowed to replace the selected encoding.</summary>
    /// <returns><see cref = "HtmlEncodingConfidence.Certain"/> for an authoritative selection; otherwise, <see cref = "HtmlEncodingConfidence.Tentative"/>.</returns>
    public HtmlEncodingConfidence GetConfidence()
    {
        return this.confidence;
    }

    /// <summary>Gets which signal selected the reported encoding.</summary>
    /// <returns>The fallback, XML declaration, transport label, byte-order mark, meta declaration, or caller override source.</returns>
    public HtmlEncodingSource GetSource()
    {
        return this.source;
    }

    /// <summary>Gets the number of leading bytes consumed as a byte-order mark.</summary>
    /// <returns><c>0</c> when no byte-order mark was present; otherwise, the byte-order mark length.</returns>
    public int GetByteOrderMarkLength()
    {
        return this.bomLength;
    }

    /// <summary>Indicates whether this object has the declaration byte offset.</summary>
    /// <returns><c>true</c> when the encoding declaration has a known byte offset; otherwise, <c>false</c>.</returns>
    public bool HasDeclarationByteOffset()
    {
        return this.declarationByteOffset >= 0;
    }

    /// <summary>Gets where the encoding declaration begins in the original byte input.</summary>
    /// <returns>The zero-based byte offset, or <c>-1</c> when <see cref = "HasDeclarationByteOffset"/> is <c>false</c>.</returns>
    public int GetDeclarationByteOffset()
    {
        return this.declarationByteOffset;
    }
}
