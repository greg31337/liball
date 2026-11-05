using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace pptx;

internal static class PptxContentTypesWriter
{
    internal static byte[] WriteBytes(PptxContentTypeManifest manifest)
    {
        PptxResult result = ValidateManifest(manifest);
        if (result.Failed())
        {
            throw new System.ArgumentException(result.FormatDiagnostics());
        }

        List<PptxContentTypeDefault> defaults = CreateOrderedDefaults(manifest);
        List<PptxContentTypeOverride> overrides = CreateOrderedOverrides(manifest);

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
                writer.WriteStartElement("Types", PptxNamespaces.ContentTypes);

                for (int index = 0; index < defaults.Count; index++)
                {
                    WriteDefault(writer, defaults[index]);
                }

                for (int index = 0; index < overrides.Count; index++)
                {
                    WriteOverride(writer, overrides[index]);
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            return stream.ToArray();
        }
    }

    internal static string WriteString(PptxContentTypeManifest manifest)
    {
        return Encoding.UTF8.GetString(WriteBytes(manifest));
    }

    internal static PptxResult ValidateManifest(PptxContentTypeManifest manifest)
    {
        PptxResult result = new PptxResult();

        if (manifest == null)
        {
            result.AddError("CTW001", "Content type manifest is required.", PptxPartNames.ContentTypesEntry, "", "");
            return result;
        }

        Dictionary<string, string> defaultExtensions = new Dictionary<string, string>();
        Dictionary<string, string> overridePartNames = new Dictionary<string, string>();
        PptxContentTypeManifest validationManifest = new PptxContentTypeManifest();
        validationManifest.Diagnostics = result;

        for (int index = 0; index < manifest.Defaults.Count; index++)
        {
            PptxContentTypeDefault item = manifest.Defaults[index];
            string extension = PptxContentTypesParser.NormalizeExtension(item.Extension);

            if (!PptxContentTypesParser.ValidateExtension(extension, validationManifest))
            {
                continue;
            }

            if (!PptxContentTypesParser.ValidateContentType(item.ContentType, validationManifest))
            {
                continue;
            }

            if (defaultExtensions.ContainsKey(extension))
            {
                result.AddError("CT020", "Duplicate content type default extension.", PptxPartNames.ContentTypesEntry, "", "");
                continue;
            }

            defaultExtensions.Add(extension, item.ContentType);
        }

        for (int index = 0; index < manifest.Overrides.Count; index++)
        {
            PptxContentTypeOverride item = manifest.Overrides[index];
            PptxResult partNameResult = PptxUriCore.ValidatePartName(item.PartName);

            if (partNameResult.Failed())
            {
                PptxUriCore.CopyDiagnostics(partNameResult, result);
                continue;
            }

            if (!PptxContentTypesParser.ValidateContentType(item.ContentType, validationManifest))
            {
                continue;
            }

            string partName = PptxUriCore.NormalizePartName(item.PartName);
            if (overridePartNames.ContainsKey(partName))
            {
                result.AddError("CT021", "Duplicate content type override part name.", partName, "", "");
                continue;
            }

            overridePartNames.Add(partName, item.ContentType);
        }

        return result;
    }

    internal static List<PptxContentTypeDefault> CreateOrderedDefaults(PptxContentTypeManifest manifest)
    {
        List<PptxContentTypeDefault> defaults = new List<PptxContentTypeDefault>();

        for (int index = 0; index < manifest.Defaults.Count; index++)
        {
            PptxContentTypeDefault source = manifest.Defaults[index];
            defaults.Add(new PptxContentTypeDefault(PptxContentTypesParser.NormalizeExtension(source.Extension), source.ContentType));
        }

        SortDefaults(defaults);
        return defaults;
    }

    internal static List<PptxContentTypeOverride> CreateOrderedOverrides(PptxContentTypeManifest manifest)
    {
        List<PptxContentTypeOverride> overrides = new List<PptxContentTypeOverride>();

        for (int index = 0; index < manifest.Overrides.Count; index++)
        {
            PptxContentTypeOverride source = manifest.Overrides[index];
            overrides.Add(new PptxContentTypeOverride(PptxUriCore.NormalizePartName(source.PartName), source.ContentType));
        }

        SortOverrides(overrides);
        return overrides;
    }

    internal static void SortDefaults(List<PptxContentTypeDefault> defaults)
    {
        for (int index = 1; index < defaults.Count; index++)
        {
            PptxContentTypeDefault current = defaults[index];
            int position = index - 1;

            while (position >= 0 && CompareText(defaults[position].Extension, current.Extension) > 0)
            {
                defaults[position + 1] = defaults[position];
                position = position - 1;
            }

            defaults[position + 1] = current;
        }
    }

    internal static void SortOverrides(List<PptxContentTypeOverride> overrides)
    {
        for (int index = 1; index < overrides.Count; index++)
        {
            PptxContentTypeOverride current = overrides[index];
            int position = index - 1;

            while (position >= 0 && CompareText(overrides[position].PartName, current.PartName) > 0)
            {
                overrides[position + 1] = overrides[position];
                position = position - 1;
            }

            overrides[position + 1] = current;
        }
    }

    internal static int CompareText(string left, string right)
    {
        return string.Compare(left, right, System.StringComparison.Ordinal);
    }

    internal static void WriteDefault(XmlWriter writer, PptxContentTypeDefault item)
    {
        writer.WriteStartElement("Default", PptxNamespaces.ContentTypes);
        writer.WriteAttributeString("Extension", item.Extension);
        writer.WriteAttributeString("ContentType", item.ContentType);
        writer.WriteEndElement();
    }

    internal static void WriteOverride(XmlWriter writer, PptxContentTypeOverride item)
    {
        writer.WriteStartElement("Override", PptxNamespaces.ContentTypes);
        writer.WriteAttributeString("PartName", item.PartName);
        writer.WriteAttributeString("ContentType", item.ContentType);
        writer.WriteEndElement();
    }
}
