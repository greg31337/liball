namespace odf;

/// <summary>
/// Describes a typed item or container in an application-settings tree.
/// </summary>
public class OdfSetting
{
    /// <summary>The configuration node kind.</summary>
    public OdfSettingKind Kind;
    /// <summary>The inherited portable or view scope.</summary>
    public OdfSettingScope Scope;
    /// <summary>The configuration name.</summary>
    public string Name = "";
    /// <summary>The exact declared configuration type token.</summary>
    public string ValueType = "";
    /// <summary>The recognized declared value kind.</summary>
    public OdfSettingValueKind ValueKind;
    /// <summary>The exact character-data representation of the value.</summary>
    public string LexicalValue = "";
    /// <summary>The string or date-time value.</summary>
    public string StringValue = "";
    /// <summary>The parsed Boolean value.</summary>
    public bool BooleanValue;
    /// <summary>The parsed signed integer value.</summary>
    public long IntegerValue;
    /// <summary>The parsed floating-point value.</summary>
    public double NumericValue;
    /// <summary>The parsed binary value.</summary>
    public byte[] BinaryValue = [];
    /// <summary>Whether the lexical value is valid for its declared type.</summary>
    public bool IsValid;
    /// <summary>The child settings in document order.</summary>
    public OdfSetting[] Children = [];
    /// <summary>All qualified attributes, including preserved extensions.</summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>The XML part containing the setting.</summary>
    public string PartPath = "";
    /// <summary>The underlying configuration element.</summary>
    public OdfNode? Node;
    internal string OriginalName = "";
    internal string OriginalValueType = "";
    internal string OriginalLexicalValue = "";
    internal string OriginalStringValue = "";
    internal bool OriginalBooleanValue;
    internal long OriginalIntegerValue;
    internal double OriginalNumericValue;
    internal byte[] OriginalBinaryValue = [];
}
