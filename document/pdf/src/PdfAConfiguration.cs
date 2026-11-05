using System;
using System.Globalization;
using System.Text;

namespace pdf;

internal static class PdfAConfiguration
{
    internal static void Prepare(PdfDocument document, PdfAPreparationOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        ValidateOptions(options);
        PdfAPreparationOptions copied = options.Copy();
        ValidateDocument(document, copied);
        document.HasPdfA = true;
        document.PdfAOptions = copied;
        document.WriterOptions.Version = GetRequiredVersion(copied.Conformance);
        AssociateAttachments(document);
        UpdateIdentificationMetadata(document);
    }

    internal static void ValidateOptions(PdfAPreparationOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        ValidateConformance(options.Conformance);
        ValidateRequiredText(options.OutputConditionIdentifier, "OutputConditionIdentifier");
        ValidateRequiredText(options.OutputCondition, "OutputCondition");
        ValidateRequiredText(options.RegistryName, "RegistryName");
        ValidateRequiredText(options.Info, "Info");
        ValidateRelationship(options.DefaultAssociatedFileRelationship);
        GetIccComponentCount(options.IccProfileData);
    }

    internal static void ValidateDocument(PdfDocument document, PdfAPreparationOptions options)
    {
        if (document.HasEncryption)
        {
            throw new InvalidOperationException("PDF/A output cannot be encrypted.");
        }

        if (HasStandardFontUsage(document))
        {
            throw new InvalidOperationException("PDF/A output requires embedded TrueType fonts instead of standard Base-14 fonts.");
        }

        if (document.HasAcroForm)
        {
            throw new InvalidOperationException("Interactive forms must be flattened before preparing PDF/A output.");
        }

        if (HasUnsupportedAnnotations(document))
        {
            throw new InvalidOperationException("Annotations without archival appearance streams are not supported in PDF/A output.");
        }

        if (options.Conformance != PdfAConformance.PdfA3B && document.Attachments.Count > 0)
        {
            throw new InvalidOperationException("The selected PDF/A profile does not permit the document's embedded files.");
        }

        if (options.Conformance == PdfAConformance.PdfA1B && HasTransparency(document))
        {
            throw new InvalidOperationException("PDF/A-1b does not permit transparency or image soft masks.");
        }

        ValidateColorProfileUsage(document, GetIccComponentCount(options.IccProfileData));
    }

    internal static void ValidatePreparedDocument(PdfDocument document)
    {
        if (!document.HasPdfA)
        {
            return;
        }

        ValidateOptions(document.PdfAOptions);
        ValidateDocument(document, document.PdfAOptions);
        PdfVersion requiredVersion = GetRequiredVersion(document.PdfAOptions.Conformance);
        if (document.WriterOptions.Version != requiredVersion)
        {
            throw new InvalidOperationException("The PDF version does not match the prepared PDF/A profile.");
        }

        if (document.PdfAOptions.Conformance == PdfAConformance.PdfA3B)
        {
            foreach (PdfAttachment attachment in document.Attachments)
            {
                if (!attachment.IsAssociatedFile)
                {
                    throw new InvalidOperationException("Every PDF/A-3b embedded file must be associated with the document catalog.");
                }

                ValidateRelationship(attachment.Relationship);
                if (attachment.MimeType.Length == 0)
                {
                    throw new InvalidOperationException("PDF/A-3b associated files require a MIME type.");
                }
            }
        }

        PdfFacturXConfiguration.ValidatePreparedDocument(document);

        UpdateIdentificationMetadata(document);
    }

    internal static void AssociateAttachments(PdfDocument document)
    {
        if (document.PdfAOptions.Conformance != PdfAConformance.PdfA3B)
        {
            return;
        }

        foreach (PdfAttachment attachment in document.Attachments)
        {
            if (attachment.MimeType.Length == 0)
            {
                throw new InvalidOperationException("PDF/A-3b associated files require a MIME type.");
            }

            if (!attachment.IsAssociatedFile)
            {
                attachment.IsAssociatedFile = true;
                attachment.Relationship = document.PdfAOptions.DefaultAssociatedFileRelationship;
            }
        }
    }

    internal static void ConfigureNewAttachment(PdfDocument document, PdfAttachment attachment)
    {
        if (!document.HasPdfA)
        {
            return;
        }

        if (document.PdfAOptions.Conformance != PdfAConformance.PdfA3B)
        {
            throw new InvalidOperationException("The selected PDF/A profile does not permit embedded files.");
        }

        if (attachment.MimeType.Length == 0)
        {
            throw new InvalidOperationException("PDF/A-3b associated files require a MIME type.");
        }

        attachment.IsAssociatedFile = true;
        attachment.Relationship = document.PdfAOptions.DefaultAssociatedFileRelationship;
    }

    internal static void UpdateIdentificationMetadata(PdfDocument document)
    {
        int part = (int)document.PdfAOptions.Conformance;
        StringBuilder xmp = new StringBuilder();
        xmp.Append("<?xpacket begin=\"");
        xmp.Append('\uFEFF');
        xmp.AppendLine("\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>");
        xmp.AppendLine("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">");
        xmp.AppendLine("<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">");
        xmp.Append("<rdf:Description rdf:about=\"\" xmlns:pdfaid=\"http://www.aiim.org/pdfa/ns/id/\" pdfaid:part=\"");
        xmp.Append(part);
        xmp.AppendLine("\" pdfaid:conformance=\"B\"/>");
        if (!string.IsNullOrEmpty(document.Metadata.Producer))
        {
            xmp.Append("<rdf:Description rdf:about=\"\" xmlns:pdf=\"http://ns.adobe.com/pdf/1.3/\" pdf:Producer=\"");
            xmp.Append(EscapeXml(document.Metadata.Producer));
            xmp.AppendLine("\"/>");
        }

        if (!string.IsNullOrEmpty(document.Metadata.Title))
        {
            xmp.AppendLine("<rdf:Description rdf:about=\"\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\">");
            xmp.Append("<dc:title><rdf:Alt><rdf:li xml:lang=\"x-default\">");
            xmp.Append(EscapeXml(document.Metadata.Title));
            xmp.AppendLine("</rdf:li></rdf:Alt></dc:title></rdf:Description>");
        }

        if (!string.IsNullOrEmpty(document.Metadata.Author))
        {
            xmp.AppendLine("<rdf:Description rdf:about=\"\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\">");
            xmp.Append("<dc:creator><rdf:Seq><rdf:li>");
            xmp.Append(EscapeXml(document.Metadata.Author));
            xmp.AppendLine("</rdf:li></rdf:Seq></dc:creator></rdf:Description>");
        }

        if (!string.IsNullOrEmpty(document.Metadata.Subject))
        {
            xmp.AppendLine("<rdf:Description rdf:about=\"\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\">");
            xmp.Append("<dc:description><rdf:Alt><rdf:li xml:lang=\"x-default\">");
            xmp.Append(EscapeXml(document.Metadata.Subject));
            xmp.AppendLine("</rdf:li></rdf:Alt></dc:description></rdf:Description>");
        }

        if (!string.IsNullOrEmpty(document.Metadata.Keywords))
        {
            xmp.Append("<rdf:Description rdf:about=\"\" xmlns:pdf=\"http://ns.adobe.com/pdf/1.3/\" pdf:Keywords=\"");
            xmp.Append(EscapeXml(document.Metadata.Keywords));
            xmp.AppendLine("\"/>");
        }

        if (!string.IsNullOrEmpty(document.Metadata.Creator))
        {
            xmp.Append("<rdf:Description rdf:about=\"\" xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\" xmp:CreatorTool=\"");
            xmp.Append(EscapeXml(document.Metadata.Creator));
            xmp.AppendLine("\"/>");
        }

        if (document.Metadata.HasCreationDateValue)
        {
            xmp.Append("<rdf:Description rdf:about=\"\" xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\" xmp:CreateDate=\"");
            xmp.Append(FormatXmpDate(document.Metadata.CreationDate));
            xmp.AppendLine("\"/>");
        }

        if (document.Metadata.HasModificationDateValue)
        {
            xmp.Append("<rdf:Description rdf:about=\"\" xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\" xmp:ModifyDate=\"");
            xmp.Append(FormatXmpDate(document.Metadata.ModificationDate));
            xmp.AppendLine("\"/>");
        }

        if (document.HasFacturX)
        {
            PdfFacturXConfiguration.AppendXmp(xmp, document.FacturXMetadata);
        }

        xmp.AppendLine("</rdf:RDF>");
        xmp.AppendLine("</x:xmpmeta>");
        xmp.Append("<?xpacket end=\"w\"?>");
        document.Metadata.XmpMetadata = xmp.ToString();
    }

    internal static int GetIccComponentCount(byte[] profile)
    {
        if (profile == null)
        {
            throw new ArgumentNullException("IccProfileData");
        }

        if (profile.Length < 128)
        {
            throw new ArgumentException("ICC profile data must contain a complete 128-byte header.", "IccProfileData");
        }

        int declaredLength = ReadInt32(profile, 0);
        if (declaredLength != profile.Length)
        {
            throw new ArgumentException("ICC profile length does not match its header.", "IccProfileData");
        }

        if (profile[36] != 97 || profile[37] != 99 || profile[38] != 115 || profile[39] != 112)
        {
            throw new ArgumentException("ICC profile header does not contain the required acsp signature.", "IccProfileData");
        }

        string colorSpace = Encoding.ASCII.GetString(profile, 16, 4);
        if (colorSpace == "GRAY")
        {
            return 1;
        }

        if (colorSpace == "RGB ")
        {
            return 3;
        }

        if (colorSpace == "CMYK")
        {
            return 4;
        }

        throw new ArgumentException("ICC profile color space must be Gray, RGB, or CMYK.", "IccProfileData");
    }

    internal static int ReadInt32(byte[] data, int offset)
    {
        return data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3];
    }

    internal static bool HasStandardFontUsage(PdfDocument document)
    {
        foreach (PdfPage page in document.Pages)
        {
            if (page.Canvas.Fonts.Count > 0)
            {
                return true;
            }
        }

        foreach (PdfFormXObject form in document.Forms)
        {
            if (form.Canvas.Fonts.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool HasUnsupportedAnnotations(PdfDocument document)
    {
        foreach (PdfPage page in document.Pages)
        {
            if (page.Annotations.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool HasTransparency(PdfDocument document)
    {
        foreach (PdfImage image in document.Images)
        {
            if (image.HasSoftMask)
            {
                return true;
            }
        }

        foreach (PdfPage page in document.Pages)
        {
            if (page.Canvas.GraphicsStates.Count > 0)
            {
                return true;
            }
        }

        foreach (PdfFormXObject form in document.Forms)
        {
            if (form.Canvas.GraphicsStates.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    internal static void ValidateColorProfileUsage(PdfDocument document, int componentCount)
    {
        foreach (PdfImage image in document.Images)
        {
            if (image.ColorSpace == PdfImageColorSpace.Rgb && componentCount != 3)
            {
                throw new InvalidOperationException("DeviceRGB images require an RGB output-intent profile.");
            }

            if (image.ColorSpace == PdfImageColorSpace.Cmyk && componentCount != 4)
            {
                throw new InvalidOperationException("DeviceCMYK images require a CMYK output-intent profile.");
            }
        }

        foreach (PdfPage page in document.Pages)
        {
            ValidateCanvasColorProfile(page.Canvas, componentCount);
        }

        foreach (PdfFormXObject form in document.Forms)
        {
            ValidateCanvasColorProfile(form.Canvas, componentCount);
        }
    }

    internal static void ValidateCanvasColorProfile(PdfCanvas canvas, int componentCount)
    {
        if (canvas.UsesDeviceRgb && componentCount != 3)
        {
            throw new InvalidOperationException("DeviceRGB drawing colors require an RGB output-intent profile.");
        }

        if (canvas.UsesDeviceCmyk && componentCount != 4)
        {
            throw new InvalidOperationException("DeviceCMYK drawing colors require a CMYK output-intent profile.");
        }
    }

    internal static PdfVersion GetRequiredVersion(PdfAConformance conformance)
    {
        if (conformance == PdfAConformance.PdfA1B)
        {
            return PdfVersion.Pdf14;
        }

        return PdfVersion.Pdf17;
    }

    internal static string GetRelationshipName(PdfAssociatedFileRelationship relationship)
    {
        ValidateRelationship(relationship);
        if (relationship == PdfAssociatedFileRelationship.Source)
        {
            return "Source";
        }

        if (relationship == PdfAssociatedFileRelationship.Data)
        {
            return "Data";
        }

        if (relationship == PdfAssociatedFileRelationship.Alternative)
        {
            return "Alternative";
        }

        if (relationship == PdfAssociatedFileRelationship.Supplement)
        {
            return "Supplement";
        }

        return "Unspecified";
    }

    internal static void ValidateConformance(PdfAConformance conformance)
    {
        if (conformance == PdfAConformance.PdfA1B || conformance == PdfAConformance.PdfA2B || conformance == PdfAConformance.PdfA3B)
        {
            return;
        }

        throw new ArgumentException("PDF/A conformance profile is not supported.", "Conformance");
    }

    internal static void ValidateRelationship(PdfAssociatedFileRelationship relationship)
    {
        if (relationship == PdfAssociatedFileRelationship.Source || relationship == PdfAssociatedFileRelationship.Data || relationship == PdfAssociatedFileRelationship.Alternative || relationship == PdfAssociatedFileRelationship.Supplement || relationship == PdfAssociatedFileRelationship.Unspecified)
        {
            return;
        }

        throw new ArgumentException("Associated-file relationship is not supported.", "relationship");
    }

    internal static void ValidateRequiredText(string value, string name)
    {
        if (value == null)
        {
            throw new ArgumentNullException(name);
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("PDF/A output-intent text cannot be empty.", name);
        }
    }

    internal static string EscapeXml(string value)
    {
        return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
    }

    internal static string FormatXmpDate(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return value.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return value.ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture);
        }

        return value.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
    }
}
