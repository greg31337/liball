using System.Collections.Generic;

namespace iso20022;

/// <summary>Controls generic or typed ISO 20022 XML writing.</summary>
public class MessageWriteOptions
{
    /// <summary>The requested XML formatting mode.</summary>
    public XmlFormattingMode FormattingMode = XmlFormattingMode.Compact;
    /// <summary>The policy for unknown content in the supplied document.</summary>
    public UnknownContentMode UnknownContentMode = UnknownContentMode.Preserve;
    /// <summary>The resource bounds applied while writing.</summary>
    public ResourceLimits Limits = new ResourceLimits();
    /// <summary>Whether to emit an XML declaration.</summary>
    public bool WriteXmlDeclaration = true;
    /// <summary>Whether a UTF-8 byte output starts with a byte-order mark.</summary>
    public bool WriteByteOrderMark;
    /// <summary>Whether available source or preferred prefixes are retained.</summary>
    public bool PreservePrefixes = true;
    /// <summary>Whether available primitive lexical forms are retained.</summary>
    public bool PreserveLexicalValues = true;
    /// <summary>The text used for one indentation level.</summary>
    public string IndentText = "  ";
    /// <summary>The text used between indented lines.</summary>
    public string NewLineText = "\n";
    /// <summary>Caller-selected namespace declarations and preferred prefixes.</summary>
    public List<NamespaceDeclarationData> NamespaceDeclarations = new List<NamespaceDeclarationData>();
}
