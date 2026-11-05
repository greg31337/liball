using System;

namespace xlsx;

/// <summary>Controls resource limits and handling of security-sensitive workbook content.</summary>
public class XlsxSecurityOptions
{
    /// <summary>Stores the maximum uncompressed byte count permitted for one package part.</summary>
    public long MaxPartBytes;
    /// <summary>Stores the maximum uncompressed-to-compressed ratio permitted for one package part.</summary>
    public int MaxCompressionRatio;
    /// <summary>Stores the maximum attribute count permitted on one XML element.</summary>
    public int MaxXmlAttributesPerElement;
    /// <summary>Stores the maximum cumulative text characters permitted in one XML part.</summary>
    public long MaxXmlTotalTextCharacters;
    /// <summary>Stores the maximum number of image parts permitted in a workbook.</summary>
    public int MaxImages;
    /// <summary>Stores the maximum number of legacy and threaded comments permitted in a workbook.</summary>
    public int MaxComments;
    /// <summary>Stores the maximum number of formula cells permitted in a workbook.</summary>
    public int MaxFormulas;
    /// <summary>Stores the maximum number of external relationships permitted in a workbook.</summary>
    public int MaxExternalRelationships;
    /// <summary>Controls VBA and macro-enabled content.</summary>
    public XlsxSecurityContentPolicy MacroPolicy;
    /// <summary>Controls OLE objects and embedded packages.</summary>
    public XlsxSecurityContentPolicy OleObjectPolicy;
    /// <summary>Controls externally targeted image relationships.</summary>
    public XlsxSecurityContentPolicy ExternalImagePolicy;
    /// <summary>Controls external-link parts and relationships.</summary>
    public XlsxSecurityContentPolicy ExternalLinkPolicy;
    /// <summary>Controls workbook connections.</summary>
    public XlsxSecurityContentPolicy ConnectionPolicy;
    /// <summary>Controls query-table parts.</summary>
    public XlsxSecurityContentPolicy QueryTablePolicy;
    /// <summary>Controls custom XML parts.</summary>
    public XlsxSecurityContentPolicy CustomXmlPolicy;
    /// <summary>Controls unrecognized parts whose type or path indicates executable or scripted content.</summary>
    public XlsxSecurityContentPolicy UnknownActiveContentPolicy;

    /// <summary>Initializes conservative resource limits with content preservation enabled.</summary>
    public XlsxSecurityOptions()
    {
        MaxPartBytes = 268435456;
        MaxCompressionRatio = 1000;
        MaxXmlAttributesPerElement = 10000;
        MaxXmlTotalTextCharacters = 67108864;
        MaxImages = 100000;
        MaxComments = 1000000;
        MaxFormulas = 10000000;
        MaxExternalRelationships = 10000;
        MacroPolicy = XlsxSecurityContentPolicy.Preserve;
        OleObjectPolicy = XlsxSecurityContentPolicy.Preserve;
        ExternalImagePolicy = XlsxSecurityContentPolicy.Preserve;
        ExternalLinkPolicy = XlsxSecurityContentPolicy.Preserve;
        ConnectionPolicy = XlsxSecurityContentPolicy.Preserve;
        QueryTablePolicy = XlsxSecurityContentPolicy.Preserve;
        CustomXmlPolicy = XlsxSecurityContentPolicy.Preserve;
        UnknownActiveContentPolicy = XlsxSecurityContentPolicy.Preserve;
    }

    internal XlsxValidationResult Validate()
    {
        XlsxValidationResult result = new XlsxValidationResult();
        if (MaxPartBytes <= 0) { result.AddError("security.maxPartBytes", "MaxPartBytes must be greater than zero."); }
        if (MaxCompressionRatio <= 0) { result.AddError("security.maxCompressionRatio", "MaxCompressionRatio must be greater than zero."); }
        if (MaxXmlAttributesPerElement <= 0) { result.AddError("security.maxXmlAttributesPerElement", "MaxXmlAttributesPerElement must be greater than zero."); }
        if (MaxXmlTotalTextCharacters <= 0) { result.AddError("security.maxXmlTotalTextCharacters", "MaxXmlTotalTextCharacters must be greater than zero."); }
        if (MaxImages <= 0) { result.AddError("security.maxImages", "MaxImages must be greater than zero."); }
        if (MaxComments <= 0) { result.AddError("security.maxComments", "MaxComments must be greater than zero."); }
        if (MaxFormulas <= 0) { result.AddError("security.maxFormulas", "MaxFormulas must be greater than zero."); }
        if (MaxExternalRelationships <= 0) { result.AddError("security.maxExternalRelationships", "MaxExternalRelationships must be greater than zero."); }
        ValidatePolicy(result, MacroPolicy, "security.macroPolicy");
        ValidatePolicy(result, OleObjectPolicy, "security.oleObjectPolicy");
        ValidatePolicy(result, ExternalImagePolicy, "security.externalImagePolicy");
        ValidatePolicy(result, ExternalLinkPolicy, "security.externalLinkPolicy");
        ValidatePolicy(result, ConnectionPolicy, "security.connectionPolicy");
        ValidatePolicy(result, QueryTablePolicy, "security.queryTablePolicy");
        ValidatePolicy(result, CustomXmlPolicy, "security.customXmlPolicy");
        ValidatePolicy(result, UnknownActiveContentPolicy, "security.unknownActiveContentPolicy");
        return result;
    }

    internal static void ValidatePolicy(XlsxValidationResult result, XlsxSecurityContentPolicy policy, string code)
    {
        if (policy != XlsxSecurityContentPolicy.Preserve && policy != XlsxSecurityContentPolicy.Remove && policy != XlsxSecurityContentPolicy.Reject)
        {
            result.AddError(code, "Security content policy value is invalid.");
        }
    }
}
