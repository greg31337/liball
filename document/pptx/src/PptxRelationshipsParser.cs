using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace pptx;

internal static class PptxRelationshipsParser
{
    internal static PptxRelationshipCollection ParseRootString(string xml)
    {
        return ParseString("", xml);
    }

    internal static PptxRelationshipCollection ParseRootBytes(byte[] bytes)
    {
        return ParseBytes("", bytes);
    }

    internal static PptxRelationshipCollection ParsePartString(string sourcePartName, string xml)
    {
        return ParseString(PptxUriCore.NormalizePartNameText(sourcePartName), xml);
    }

    internal static PptxRelationshipCollection ParsePartBytes(string sourcePartName, byte[] bytes)
    {
        return ParseBytes(PptxUriCore.NormalizePartNameText(sourcePartName), bytes);
    }

    internal static PptxRelationshipCollection ParseBytes(string sourcePartName, byte[] bytes)
    {
        PptxRelationshipCollection relationships = new PptxRelationshipCollection(PptxDiagnostic.CleanText(sourcePartName));

        if (bytes == null)
        {
            relationships.Diagnostics.AddError("REL001", "Relationships XML bytes are required.", "", "", "");
            return relationships;
        }

        return ParseString(sourcePartName, Encoding.UTF8.GetString(bytes));
    }

    internal static PptxRelationshipCollection ParseString(string sourcePartName, string xml)
    {
        PptxRelationshipCollection relationships = new PptxRelationshipCollection(PptxDiagnostic.CleanText(sourcePartName));

        if (string.IsNullOrWhiteSpace(xml))
        {
            relationships.Diagnostics.AddError("REL001", "Relationships XML is required.", "", "", "");
            return relationships;
        }

        XmlDocument document = PptxContentTypesParser.LoadXml(xml);
        XmlElement? root = document.DocumentElement;

        if (root == null)
        {
            relationships.Diagnostics.AddError("REL002", "Relationships XML must have a root element.", "", "", "");
            return relationships;
        }

        if (root.LocalName != "Relationships" || root.NamespaceURI != PptxNamespaces.PackageRelationships)
        {
            relationships.Diagnostics.AddError("REL003", "Relationships XML root element must be Relationships.", "", "", "");
            return relationships;
        }

        ParseChildren(root, relationships);
        return relationships;
    }

    internal static void ParseChildren(XmlElement root, PptxRelationshipCollection relationships)
    {
        Dictionary<string, string> seenIds = new Dictionary<string, string>();

        for (int index = 0; index < root.ChildNodes.Count; index++)
        {
            XmlNode? child = root.ChildNodes[index];

            if (child == null)
            {
                continue;
            }

            if (child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI != PptxNamespaces.PackageRelationships)
            {
                relationships.Diagnostics.AddWarning("REL004", "Unknown relationships element namespace was ignored.", relationships.SourcePartName, "", "");
                continue;
            }

            if (element.LocalName != "Relationship")
            {
                relationships.Diagnostics.AddWarning("REL005", "Unknown relationships element was ignored.", relationships.SourcePartName, "", "");
                continue;
            }

            ParseRelationship(element, relationships, seenIds);
        }
    }

    internal static void ParseRelationship(XmlElement element, PptxRelationshipCollection relationships, Dictionary<string, string> seenIds)
    {
        string id = PptxContentTypesParser.GetAttribute(element, "Id");
        string type = PptxContentTypesParser.GetAttribute(element, "Type");
        string target = PptxContentTypesParser.GetAttribute(element, "Target");
        string targetModeText = PptxContentTypesParser.GetAttribute(element, "TargetMode");

        if (!ValidateRelationshipId(id, relationships))
        {
            return;
        }

        if (seenIds.ContainsKey(id))
        {
            relationships.Diagnostics.AddError("REL015", "Duplicate relationship Id.", relationships.SourcePartName, id, "");
            return;
        }

        if (!ValidateRelationshipType(type, relationships))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(target))
        {
            relationships.Diagnostics.AddError("REL014", "Relationship Target is required.", relationships.SourcePartName, id, "");
            return;
        }

        PptxRelationshipTargetMode targetMode = ParseTargetMode(targetModeText, relationships, id);
        if (relationships.Diagnostics.Failed() && targetModeText.Length > 0 && targetModeText != "Internal" && targetModeText != "External")
        {
            return;
        }

        if (targetMode == PptxRelationshipTargetMode.External)
        {
            if (!ValidateExternalTarget(target, relationships, id))
            {
                return;
            }

            seenIds.Add(id, id);
            relationships.Add(new PptxRelationship(relationships.SourcePartName, id, type, target, targetMode, "", ""));
            return;
        }

        if (!ValidateInternalTarget(relationships.SourcePartName, target, relationships, id))
        {
            return;
        }

        string targetPartName = NormalizeInternalTargetPartName(relationships.SourcePartName, target);
        string targetFragment = PptxUriCore.ExtractFragment(target);
        seenIds.Add(id, id);
        relationships.Add(new PptxRelationship(relationships.SourcePartName, id, type, target, targetMode, targetPartName, targetFragment));
    }

    internal static PptxRelationshipTargetMode ParseTargetMode(string targetModeText, PptxRelationshipCollection relationships, string id)
    {
        if (targetModeText.Length == 0)
        {
            return PptxRelationshipTargetMode.Internal;
        }

        if (targetModeText == "Internal")
        {
            return PptxRelationshipTargetMode.Internal;
        }

        if (targetModeText == "External")
        {
            return PptxRelationshipTargetMode.External;
        }

        relationships.Diagnostics.AddError("REL016", "Relationship TargetMode must be Internal or External.", relationships.SourcePartName, id, "");
        return PptxRelationshipTargetMode.Internal;
    }

    internal static bool ValidateRelationshipId(string id, PptxRelationshipCollection relationships)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            relationships.Diagnostics.AddError("REL010", "Relationship Id is required.", relationships.SourcePartName, "", "");
            return false;
        }

        if (!IsRelationshipIdStart(id[0]))
        {
            relationships.Diagnostics.AddError("REL011", "Relationship Id contains invalid characters.", relationships.SourcePartName, id, "");
            return false;
        }

        for (int index = 1; index < id.Length; index++)
        {
            if (!IsRelationshipIdCharacter(id[index]))
            {
                relationships.Diagnostics.AddError("REL011", "Relationship Id contains invalid characters.", relationships.SourcePartName, id, "");
                return false;
            }
        }

        return true;
    }

    internal static bool ValidateRelationshipType(string type, PptxRelationshipCollection relationships)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            relationships.Diagnostics.AddError("REL012", "Relationship Type is required.", relationships.SourcePartName, "", "");
            return false;
        }

        if (!PptxUriCore.IsExternalTarget(type))
        {
            relationships.Diagnostics.AddError("REL013", "Relationship Type must be an absolute URI.", relationships.SourcePartName, "", "");
            return false;
        }

        for (int index = 0; index < type.Length; index++)
        {
            if (type[index] <= ' ')
            {
                relationships.Diagnostics.AddError("REL013", "Relationship Type contains invalid characters.", relationships.SourcePartName, "", "");
                return false;
            }
        }

        return true;
    }

    internal static bool ValidateExternalTarget(string target, PptxRelationshipCollection relationships, string id)
    {
        if (!PptxUriCore.IsExternalTarget(target))
        {
            relationships.Diagnostics.AddError("REL017", "External relationship Target must be an absolute URI.", relationships.SourcePartName, id, "");
            return false;
        }

        for (int index = 0; index < target.Length; index++)
        {
            if (target[index] <= ' ')
            {
                relationships.Diagnostics.AddError("REL018", "External relationship Target contains invalid characters.", relationships.SourcePartName, id, "");
                return false;
            }
        }

        return true;
    }

    internal static bool ValidateInternalTarget(string sourcePartName, string target, PptxRelationshipCollection relationships, string id)
    {
        if (sourcePartName.Length == 0)
        {
            return ValidateRootInternalTarget(target, relationships, id);
        }

        PptxResult result = PptxUriCore.ValidateInternalRelationshipTarget(sourcePartName, target);
        if (result.Failed())
        {
            PptxUriCore.CopyDiagnostics(result, relationships.Diagnostics);
            return false;
        }

        return true;
    }

    internal static bool ValidateRootInternalTarget(string target, PptxRelationshipCollection relationships, string id)
    {
        if (PptxUriCore.IsExternalTarget(target))
        {
            relationships.Diagnostics.AddError("URI021", "Relationship target is external and cannot be normalized as an internal package target.", relationships.SourcePartName, id, "");
            return false;
        }

        if (target.IndexOf('?') >= 0)
        {
            relationships.Diagnostics.AddError("URI022", "Relationship target query strings are not allowed for internal package targets.", relationships.SourcePartName, id, "");
            return false;
        }

        if (PptxUriCore.HasInvalidFragment(target))
        {
            relationships.Diagnostics.AddError("URI023", "Relationship target fragment contains invalid characters.", relationships.SourcePartName, id, "");
            return false;
        }

        string path = PptxUriCore.ExtractTargetPath(target);
        if (path.Length == 0)
        {
            relationships.Diagnostics.AddError("URI024", "Root relationship target path is required.", relationships.SourcePartName, id, "");
            return false;
        }

        if (PptxUriCore.HasEmptyInternalTargetSegment(path))
        {
            relationships.Diagnostics.AddError("URI025", "Relationship target cannot contain empty path segments.", relationships.SourcePartName, id, "");
            return false;
        }

        if (path[path.Length - 1] == '/' || path[path.Length - 1] == '\\')
        {
            relationships.Diagnostics.AddError("URI026", "Relationship target cannot end with '/'.", relationships.SourcePartName, id, "");
            return false;
        }

        PptxResult partNameResult = PptxUriCore.ValidatePartName(path);
        if (partNameResult.Failed())
        {
            PptxUriCore.CopyDiagnostics(partNameResult, relationships.Diagnostics);
            return false;
        }

        return true;
    }

    internal static string NormalizeInternalTargetPartName(string sourcePartName, string target)
    {
        string path = PptxUriCore.ExtractTargetPath(target);

        if (sourcePartName.Length == 0)
        {
            return PptxUriCore.NormalizePartName(path);
        }

        string normalized = PptxUriCore.NormalizeInternalRelationshipTarget(sourcePartName, target);
        return PptxUriCore.NormalizePartName(PptxUriCore.ExtractTargetPath(normalized));
    }

    internal static bool IsRelationshipIdStart(char value)
    {
        if (PptxUriCore.IsAsciiLetter(value))
        {
            return true;
        }

        if (value == '_')
        {
            return true;
        }

        return false;
    }

    internal static bool IsRelationshipIdCharacter(char value)
    {
        if (IsRelationshipIdStart(value))
        {
            return true;
        }

        if (value >= '0' && value <= '9')
        {
            return true;
        }

        if (value == '-' || value == '.')
        {
            return true;
        }

        return false;
    }
}
