using System.IO;
using System.Text;
using System.Text.Json;

namespace pptx;

internal static class PptxJsonSummaryExporterCore
{
    internal static string ExportPackage(PptxPackage package)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        using (MemoryStream stream = new MemoryStream())
        {
            using (Utf8JsonWriter writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteString("summaryType", "package");
                writer.WritePropertyName("package");
                WritePackageObject(writer, package);
                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
    internal static string ExportPresentation(PptxPresentation presentation)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        using (MemoryStream stream = new MemoryStream())
        {
            using (Utf8JsonWriter writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteString("summaryType", "presentation");
                writer.WritePropertyName("package");
                WritePackageObject(writer, presentation.Package);
                writer.WritePropertyName("model");
                WritePresentationObject(writer, presentation);
                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    internal static void WritePackageObject(Utf8JsonWriter writer, PptxPackage package)
    {
        writer.WriteStartObject();
        writer.WriteNumber("partCount", package.PartCount());
        writer.WriteNumber("rawEntryCount", package.RawEntryCount());
        writer.WriteNumber("rootRelationshipCount", package.RootRelationships.Count());
        writer.WriteNumber("contentTypeDefaultCount", package.ContentTypes.Defaults.Count);
        writer.WriteNumber("contentTypeOverrideCount", package.ContentTypes.Overrides.Count);
        writer.WriteBoolean("isDirty", package.IsDirty);
        writer.WritePropertyName("parts");
        writer.WriteStartArray();
        for (int index = 0; index < package.Parts.Count; index++)
        {
            WritePartObject(writer, package, package.Parts[index]);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    internal static void WritePartObject(Utf8JsonWriter writer, PptxPackage package, PptxPart part)
    {
        writer.WriteStartObject();
        writer.WriteString("partName", part.PartName);
        writer.WriteString("contentType", part.ContentType);
        writer.WriteNumber("byteCount", package.GetPartBytesForSave(part).Length);
        writer.WriteNumber("relationshipCount", part.Relationships.Count());
        writer.WriteBoolean("isXml", part.IsXmlContentType());
        writer.WriteEndObject();
    }

    internal static void WritePresentationObject(Utf8JsonWriter writer, PptxPresentation presentation)
    {
        writer.WriteStartObject();
        writer.WriteNumber("slideCount", presentation.SlideCount());
        writer.WriteNumber("slideWidth", presentation.SlideWidth);
        writer.WriteNumber("slideHeight", presentation.SlideHeight);
        writer.WriteString("slideSizeType", presentation.SlideSizeType);
        writer.WriteNumber("slideMasterCount", presentation.SlideMasterCount());
        writer.WriteNumber("slideLayoutCount", presentation.SlideLayoutCount());
        writer.WriteNumber("themeCount", presentation.ThemeCount());
        writer.WriteNumber("sectionCount", presentation.Sections.Count);
        writer.WriteNumber("customShowCount", presentation.CustomShows.Count);
        writer.WriteBoolean("hasNotesMaster", presentation.NotesMaster != null);
        writer.WriteBoolean("hasHandoutMaster", presentation.HandoutMaster != null);
        writer.WriteBoolean("hasMacros", presentation.HasMacros());
        writer.WriteNumber("embeddedObjectCount", presentation.GetEmbeddedObjects().Count);
        writer.WriteNumber("mediaCount", presentation.GetMedia().Count);
        writer.WriteNumber("modernCommentCount", presentation.GetModernComments().Count);
        writer.WriteNumber("animationCount", presentation.GetAnimations().Count);
        writer.WriteNumber("transitionCount", presentation.GetTransitions().Count);
        writer.WritePropertyName("slides");
        writer.WriteStartArray();
        for (int index = 0; index < presentation.SlideCount(); index++)
        {
            WriteSlideObject(writer, presentation.GetSlide(index), index);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    internal static void WriteSlideObject(Utf8JsonWriter writer, PptxSlide slide, int index)
    {
        writer.WriteStartObject();
        writer.WriteNumber("index", index);
        writer.WriteNumber("slideId", slide.SlideId);
        writer.WriteString("partName", slide.PartName);
        writer.WriteString("relationshipId", slide.RelationshipId);
        writer.WriteBoolean("hidden", slide.Hidden);
        if (slide.Layout != null)
        {
            writer.WriteString("layoutPartName", slide.Layout.PartName);
        }
        else
        {
            writer.WriteString("layoutPartName", "");
        }

        writer.WriteNumber("relationshipCount", slide.Part.Relationships.Count());
        writer.WriteNumber("placeholderCount", slide.PlaceholderCount());
        writer.WriteNumber("shapeCount", slide.GetShapeTree().GetAllShapes().Count);
        writer.WriteNumber("imageCount", slide.ExtractImages().Count);
        writer.WriteNumber("mediaCount", slide.GetMedia().Count);
        writer.WriteNumber("tableCount", slide.GetTables().Count);
        writer.WriteNumber("chartCount", slide.GetCharts().Count);
        writer.WriteNumber("diagramCount", slide.GetDiagrams().Count);
        writer.WriteNumber("embeddedObjectCount", slide.GetEmbeddedObjects().Count);
        writer.WriteNumber("commentCount", slide.GetComments().Count);
        writer.WriteNumber("modernCommentCount", slide.GetModernComments().Count);
        writer.WriteNumber("tagCount", slide.TagCount());
        writer.WriteNumber("animationCount", slide.GetAnimations().Count);
        writer.WriteBoolean("hasNotes", slide.GetNotes() != null);
        writer.WriteEndObject();
    }
}

