namespace qrcode;

/// <summary>Defines the QR alphanumeric repertoire and canonical character-set names.</summary>
public static class QrCharacterSets
{
    /// <summary>The exact 45-character QR alphanumeric repertoire in value order.</summary>
    public const string Alphanumeric = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

    /// <summary>The canonical IBM Code Page 437 name.</summary>
    public const string Cp437 = "CP437";

    /// <summary>The canonical ISO/IEC 8859-1 name.</summary>
    public const string Iso8859_1 = "ISO-8859-1";

    /// <summary>The canonical Shift JIS name.</summary>
    public const string ShiftJis = "Shift_JIS";

    /// <summary>The canonical Windows-1252 name.</summary>
    public const string Windows1252 = "windows-1252";

    /// <summary>The canonical big-endian UTF-16 name.</summary>
    public const string Utf16BigEndian = "UTF-16BE";

    /// <summary>The canonical UTF-8 name.</summary>
    public const string Utf8 = "UTF-8";

    /// <summary>The canonical US-ASCII name.</summary>
    public const string UsAscii = "US-ASCII";

    /// <summary>The canonical Big5 name.</summary>
    public const string Big5 = "Big5";

    /// <summary>The canonical GB2312 name.</summary>
    public const string Gb2312 = "GB2312";

    /// <summary>The canonical KS X 1001 name.</summary>
    public const string KsX1001 = "KS X 1001";

    /// <summary>The canonical GBK name.</summary>
    public const string Gbk = "GBK";

    /// <summary>The canonical GB18030 name.</summary>
    public const string Gb18030 = "GB18030";

    /// <summary>The canonical little-endian UTF-16 name.</summary>
    public const string Utf16LittleEndian = "UTF-16LE";

    /// <summary>The canonical big-endian UTF-32 name.</summary>
    public const string Utf32BigEndian = "UTF-32BE";

    /// <summary>The canonical little-endian UTF-32 name.</summary>
    public const string Utf32LittleEndian = "UTF-32LE";
}
