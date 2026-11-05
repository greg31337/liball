using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace pptx;

internal static class PptxRelationshipsWriter
{
    internal static byte[] WriteBytes(PptxRelationshipCollection relationships)
    {
        PptxResult result = ValidateCollection(relationships);
        if (result.Failed())
        {
            throw new System.ArgumentException(result.FormatDiagnostics());
        }

        List<PptxRelationship> ordered = CreateOrderedRelationships(relationships);

        using (MemoryStream stream = new MemoryStream())
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = new UTF8Encoding(false);
            settings.OmitXmlDeclaration = false;
            settings.Indent = false;
            settings.CloseOutput = false;

            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Relationships", PptxNamespaces.PackageRelationships);

                for (int index = 0; index < ordered.Count; index++)
                {
                    WriteRelationship(writer, relationships.SourcePartName, ordered[index]);
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            return stream.ToArray();
        }
    }

    internal static string WriteString(PptxRelationshipCollection relationships)
    {
        return Encoding.UTF8.GetString(WriteBytes(relationships));
    }

    internal static PptxResult ValidateCollection(PptxRelationshipCollection relationships)
    {
        PptxResult result = new PptxResult();

        if (relationships == null)
        {
            result.AddError("RELW001", "Relationship collection is required.", "", "", "");
            return result;
        }

        if (relationships.SourcePartName.Length > 0)
        {
            PptxResult sourceResult = PptxUriCore.ValidatePartName(relationships.SourcePartName);
            if (sourceResult.Failed())
            {
                PptxUriCore.CopyDiagnostics(sourceResult, result);
            }
        }

        Dictionary<string, string> seenIds = new Dictionary<string, string>();
        PptxRelationshipCollection validation = new PptxRelationshipCollection(relationships.SourcePartName);
        validation.Diagnostics = result;

        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = relationships.Relationships[index];
            if (relationship == null)
            {
                result.AddError("RELW002", "Relationship cannot be null.", relationships.SourcePartName, "", "");
                continue;
            }

            if (!PptxRelationshipsParser.ValidateRelationshipId(relationship.Id, validation))
            {
                continue;
            }

            if (seenIds.ContainsKey(relationship.Id))
            {
                result.AddError("REL015", "Duplicate relationship Id.", relationships.SourcePartName, relationship.Id, "");
                continue;
            }

            if (!PptxRelationshipsParser.ValidateRelationshipType(relationship.Type, validation))
            {
                continue;
            }

            if (relationship.TargetMode == PptxRelationshipTargetMode.External)
            {
                if (!PptxRelationshipsParser.ValidateExternalTarget(relationship.Target, validation, relationship.Id))
                {
                    continue;
                }
            }
            else
            {
                if (!ValidateInternalRelationshipForWrite(relationships.SourcePartName, relationship, validation))
                {
                    continue;
                }
            }

            seenIds.Add(relationship.Id, relationship.Id);
        }

        return result;
    }

    internal static bool ValidateInternalRelationshipForWrite(string sourcePartName, PptxRelationship relationship, PptxRelationshipCollection validation)
    {
        if (relationship.Target.Length > 0)
        {
            return PptxRelationshipsParser.ValidateInternalTarget(sourcePartName, relationship.Target, validation, relationship.Id);
        }

        if (relationship.TargetPartName.Length == 0)
        {
            validation.Diagnostics.AddError("REL014", "Relationship Target is required.", sourcePartName, relationship.Id, "");
            return false;
        }

        PptxResult partNameResult = PptxUriCore.ValidatePartName(relationship.TargetPartName);
        if (partNameResult.Failed())
        {
            PptxUriCore.CopyDiagnostics(partNameResult, validation.Diagnostics);
            return false;
        }

        if (relationship.TargetFragment.Length > 0 && PptxUriCore.HasInvalidFragment("#" + relationship.TargetFragment))
        {
            validation.Diagnostics.AddError("URI023", "Relationship target fragment contains invalid characters.", sourcePartName, relationship.Id, "");
            return false;
        }

        return true;
    }

    internal static List<PptxRelationship> CreateOrderedRelationships(PptxRelationshipCollection relationships)
    {
        List<PptxRelationship> ordered = new List<PptxRelationship>();

        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            ordered.Add(relationships.Relationships[index]);
        }

        SortRelationships(ordered);
        return ordered;
    }

    internal static void SortRelationships(List<PptxRelationship> relationships)
    {
        for (int index = 1; index < relationships.Count; index++)
        {
            PptxRelationship current = relationships[index];
            int position = index - 1;

            while (position >= 0 && CompareRelationshipIds(relationships[position].Id, current.Id) > 0)
            {
                relationships[position + 1] = relationships[position];
                position = position - 1;
            }

            relationships[position + 1] = current;
        }
    }

    internal static int CompareRelationshipIds(string left, string right)
    {
        bool leftNumbered = IsNumberedRelationshipId(left);
        bool rightNumbered = IsNumberedRelationshipId(right);

        if (leftNumbered && rightNumbered)
        {
            int leftStart = GetRelationshipIdSignificantDigitStart(left);
            int rightStart = GetRelationshipIdSignificantDigitStart(right);
            int leftLength = left.Length - leftStart;
            int rightLength = right.Length - rightStart;

            if (leftLength < rightLength)
            {
                return -1;
            }

            if (leftLength > rightLength)
            {
                return 1;
            }

            for (int index = 0; index < leftLength; index++)
            {
                char leftDigit = left[leftStart + index];
                char rightDigit = right[rightStart + index];
                if (leftDigit < rightDigit)
                {
                    return -1;
                }

                if (leftDigit > rightDigit)
                {
                    return 1;
                }
            }
        }

        return string.Compare(left, right, System.StringComparison.Ordinal);
    }

    internal static bool IsNumberedRelationshipId(string id)
    {
        if (id.Length <= 3)
        {
            return false;
        }

        if (id[0] != 'r' || id[1] != 'I' || id[2] != 'd')
        {
            return false;
        }

        for (int index = 3; index < id.Length; index++)
        {
            if (id[index] < '0' || id[index] > '9')
            {
                return false;
            }
        }

        return true;
    }

    internal static int GetRelationshipIdSignificantDigitStart(string id)
    {
        int index = 3;

        while (index < id.Length - 1 && id[index] == '0')
        {
            index = index + 1;
        }

        return index;
    }

    internal static void WriteRelationship(XmlWriter writer, string sourcePartName, PptxRelationship relationship)
    {
        writer.WriteStartElement("Relationship", PptxNamespaces.PackageRelationships);
        writer.WriteAttributeString("Id", relationship.Id);
        writer.WriteAttributeString("Type", relationship.Type);
        writer.WriteAttributeString("Target", GetTargetForWrite(sourcePartName, relationship));

        if (relationship.TargetMode == PptxRelationshipTargetMode.External)
        {
            writer.WriteAttributeString("TargetMode", "External");
        }

        writer.WriteEndElement();
    }

    internal static string GetTargetForWrite(string sourcePartName, PptxRelationship relationship)
    {
        if (relationship.Target.Length > 0)
        {
            return relationship.Target;
        }

        string target = CreateRelationshipTarget(sourcePartName, relationship.TargetPartName);

        if (relationship.TargetFragment.Length > 0)
        {
            return target + "#" + relationship.TargetFragment;
        }

        return target;
    }

    internal static string CreateRelationshipTarget(string sourcePartName, string targetPartName)
    {
        string target = PptxUriCore.NormalizePartName(targetPartName);

        if (sourcePartName.Length == 0)
        {
            return target.Substring(1);
        }

        string sourceDirectory = PptxUriCore.GetParentPartDirectory(sourcePartName);
        List<string> sourceSegments = GetPathSegments(sourceDirectory);
        List<string> targetSegments = GetPathSegments(target);
        int common = 0;

        while (common < sourceSegments.Count && common < targetSegments.Count && sourceSegments[common] == targetSegments[common])
        {
            common = common + 1;
        }

        string result = "";

        for (int index = common; index < sourceSegments.Count; index++)
        {
            result = result + "../";
        }

        for (int index = common; index < targetSegments.Count; index++)
        {
            if (result.Length > 0 && result[result.Length - 1] != '/')
            {
                result = result + "/";
            }

            result = result + targetSegments[index];
        }

        return result;
    }

    internal static List<string> GetPathSegments(string partName)
    {
        List<string> segments = new List<string>();

        if (partName.Length == 0)
        {
            return segments;
        }

        string normalized = PptxUriCore.NormalizePartName(partName);
        string current = "";

        for (int index = 1; index < normalized.Length; index++)
        {
            char value = normalized[index];

            if (value == '/')
            {
                if (current.Length > 0)
                {
                    segments.Add(current);
                }

                current = "";
            }
            else
            {
                current = current + value;
            }
        }

        if (current.Length > 0)
        {
            segments.Add(current);
        }

        return segments;
    }
}
