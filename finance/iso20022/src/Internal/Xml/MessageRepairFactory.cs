using System.Collections.Generic;
using System.Text;

namespace iso20022;

internal static class MessageRepairFactory
{
    internal static MessageRepair Create(string code, string description, string originalValue, string repairedValue, DynamicXmlNode? node)
    {
        MessageRepair repair = new MessageRepair();
        repair.Code = code;
        repair.Description = description;
        repair.OriginalValue = originalValue;
        repair.RepairedValue = repairedValue;
        if (node != null)
        {
            repair.Path = CreateNodePath(node);
            repair.Location = XmlPathTracker.CopyLocation(node.Location);
        }

        return repair;
    }

    internal static MessageRepair CreateNamespace(NamespaceRepair source, DynamicXmlNode? node)
    {
        string code = "namespace.repair";
        string description = "A namespace value was normalized during message processing.";
        if (source.Kind == NamespaceRepairKind.TrimXmlWhitespace)
        {
            code = "namespace.whitespace_trimmed";
            description = "Surrounding XML whitespace was removed from a namespace declaration.";
        }
        else if (source.Kind == NamespaceRepairKind.RemoveTrailingSlash)
        {
            code = "namespace.trailing_slash_removed";
            description = "A trailing slash was removed from an ISO 20022 namespace declaration.";
        }
        else if (source.Kind == NamespaceRepairKind.NormalizeTechnicalPrefixCase)
        {
            code = "namespace.technical_prefix_case_normalized";
            description = "The ISO 20022 technical namespace prefix was restored to its registered casing.";
        }
        else if (source.Kind == NamespaceRepairKind.NormalizeBusinessAreaCase)
        {
            code = "namespace.business_area_case_normalized";
            description = "The message business-area component was normalized to lowercase ASCII.";
        }

        return Create(code, description, source.OriginalValue, source.NormalizedValue, node);
    }

    internal static MessageRepair Copy(MessageRepair source)
    {
        MessageRepair copy = new MessageRepair();
        copy.Code = source.Code;
        copy.Description = source.Description;
        copy.OriginalValue = source.OriginalValue;
        copy.RepairedValue = source.RepairedValue;
        if (source.Path != null)
        {
            copy.Path = CopyPath(source.Path);
        }

        if (source.Location != null)
        {
            copy.Location = XmlPathTracker.CopyLocation(source.Location);
        }

        return copy;
    }

    internal static void AddCopies(List<MessageRepair> destination, List<MessageRepair> source, ResourceLimits limits)
    {
        int index = 0;
        while (index < source.Count)
        {
            if (destination.Count >= limits.MaxDiagnosticCount)
            {
                throw new System.Xml.XmlException("The repair diagnostic collection exceeds MaxDiagnosticCount.");
            }

            destination.Add(Copy(source[index]));
            index++;
        }
    }

    internal static void PrependCopies(List<MessageRepair> destination, List<MessageRepair> source, ResourceLimits limits)
    {
        if (destination.Count + source.Count > limits.MaxDiagnosticCount)
        {
            throw new System.Xml.XmlException("The repair diagnostic collection exceeds MaxDiagnosticCount.");
        }

        int index = source.Count - 1;
        while (index >= 0)
        {
            destination.Insert(0, Copy(source[index]));
            index--;
        }
    }

    internal static XmlPathData CreateNodePath(DynamicXmlNode node)
    {
        XmlPathData path = new XmlPathData();
        path.Segments.Add(XmlPathTracker.CopyName(node.Name));
        StringBuilder display = new StringBuilder();
        display.Append('/');
        if (node.Name.NamespaceUri.Length > 0)
        {
            display.Append("Q{");
            display.Append(node.Name.NamespaceUri);
            display.Append('}');
        }

        display.Append(node.Name.LocalName);
        path.Value = display.ToString();
        return path;
    }

    internal static XmlPathData CopyPath(XmlPathData source)
    {
        XmlPathData copy = new XmlPathData();
        copy.Value = source.Value;
        int index = 0;
        while (index < source.Segments.Count)
        {
            copy.Segments.Add(XmlPathTracker.CopyName(source.Segments[index]));
            index++;
        }

        return copy;
    }
}
