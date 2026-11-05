using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace pdf;

internal static class PdfWriter
{
    internal static readonly Encoding PdfEncoding = Encoding.ASCII;

    internal static void Write(string path, PdfDocument document)
    {
        using (Stream output = File.Create(path))
        {
            Write(output, document);
        }
    }

    internal static void Write(Stream output, PdfDocument document)
    {
        const int catalogId = 1;
        const int pagesId = 2;
        PdfFontRegistry fontRegistry = document.FontRegistry;
        List<PdfFont> fonts = new List<PdfFont>();
        Dictionary<PdfFont, int> fontObjectIds = new Dictionary<PdfFont, int>();
        List<PdfGraphicsState> graphicsStates = new List<PdfGraphicsState>();
        List<PdfTrueTypeFontUsage> trueTypeFontUsages = new List<PdfTrueTypeFontUsage>();

        if (!document.HasPdfA)
        {
            AddUsedFont(fonts, fontRegistry, PdfFont.Regular);
            AddUsedFont(fonts, fontRegistry, PdfFont.Bold);
        }

        foreach (PdfPage page in document.Pages)
        {
            AddUsedFonts(fonts, fontRegistry, page.Canvas);
            AddUsedGraphicsStates(graphicsStates, page.Canvas);
            AddUsedTrueTypeFonts(trueTypeFontUsages, page.Canvas);
        }

        foreach (PdfFormXObject form in document.Forms)
        {
            AddUsedFonts(fonts, fontRegistry, form.Canvas);
            AddUsedGraphicsStates(graphicsStates, form.Canvas);
            AddUsedTrueTypeFonts(trueTypeFontUsages, form.Canvas);
        }

        int nextObjectId = 3;
        foreach (PdfFont font in fonts)
        {
            fontObjectIds[font] = nextObjectId;
            nextObjectId++;
        }

        List<PdfEmbeddedTrueTypeFont> embeddedFonts = new List<PdfEmbeddedTrueTypeFont>();
        foreach (PdfTrueTypeFontUsage usage in trueTypeFontUsages)
        {
            PdfEmbeddedTrueTypeFont embeddedFont = new PdfEmbeddedTrueTypeFont(usage);
            embeddedFont.FontProgram = PdfTrueTypeSubsetter.CreateFontProgram(usage);
            embeddedFont.Type0ObjectId = nextObjectId;
            nextObjectId++;
            embeddedFont.DescendantObjectId = nextObjectId;
            nextObjectId++;
            embeddedFont.DescriptorObjectId = nextObjectId;
            nextObjectId++;
            embeddedFont.FontFileObjectId = nextObjectId;
            nextObjectId++;
            embeddedFont.ToUnicodeObjectId = nextObjectId;
            nextObjectId++;
            embeddedFonts.Add(embeddedFont);
        }

        Dictionary<PdfGraphicsState, int> graphicsStateObjectIds = new Dictionary<PdfGraphicsState, int>();
        foreach (PdfGraphicsState graphicsState in graphicsStates)
        {
            graphicsStateObjectIds[graphicsState] = nextObjectId;
            nextObjectId++;
        }

        Dictionary<PdfImage, int> imageObjectIds = new Dictionary<PdfImage, int>();
        Dictionary<PdfImage, int> imageSoftMaskObjectIds = new Dictionary<PdfImage, int>();
        int infoId = 0;

        if (document.Metadata.HasInfoValues())
        {
            infoId = nextObjectId;
            nextObjectId++;
        }

        int metadataId = 0;
        if (document.Metadata.HasXmpMetadata())
        {
            metadataId = nextObjectId;
            nextObjectId++;
        }

        int outputIntentId = 0;
        int iccProfileId = 0;
        if (document.HasPdfA)
        {
            outputIntentId = nextObjectId;
            nextObjectId++;
            iccProfileId = nextObjectId;
            nextObjectId++;
        }

        foreach (PdfImage image in document.Images)
        {
            imageObjectIds[image] = nextObjectId;
            nextObjectId++;
            if (image.HasSoftMask)
            {
                imageSoftMaskObjectIds[image] = nextObjectId;
                nextObjectId++;
            }
        }

        Dictionary<PdfFormXObject, int> formObjectIds = new Dictionary<PdfFormXObject, int>();
        foreach (PdfFormXObject form in document.Forms)
        {
            formObjectIds[form] = nextObjectId;
            nextObjectId++;
        }

        Dictionary<PdfAttachment, int> attachmentEmbeddedFileObjectIds = new Dictionary<PdfAttachment, int>();
        Dictionary<PdfAttachment, int> attachmentFileSpecObjectIds = new Dictionary<PdfAttachment, int>();
        foreach (PdfAttachment attachment in document.Attachments)
        {
            attachmentEmbeddedFileObjectIds[attachment] = nextObjectId;
            nextObjectId++;
            attachmentFileSpecObjectIds[attachment] = nextObjectId;
            nextObjectId++;
        }

        Dictionary<PdfInternalLink, int> internalLinkObjectIds = new Dictionary<PdfInternalLink, int>();
        Dictionary<PdfExternalLink, int> externalLinkObjectIds = new Dictionary<PdfExternalLink, int>();
        Dictionary<PdfAnnotation, int> annotationObjectIds = new Dictionary<PdfAnnotation, int>();
        if (document.HasTaggedPdf)
        {
            foreach (PdfPage page in document.Pages)
            {
                foreach (PdfInternalLink link in page.InternalLinks)
                {
                    internalLinkObjectIds[link] = nextObjectId;
                    nextObjectId++;
                }

                foreach (PdfExternalLink link in page.ExternalLinks)
                {
                    externalLinkObjectIds[link] = nextObjectId;
                    nextObjectId++;
                }

                foreach (PdfAnnotation annotation in page.Annotations)
                {
                    annotationObjectIds[annotation] = nextObjectId;
                    nextObjectId++;
                }
            }
        }

        int encryptionId = 0;
        if (document.HasEncryption)
        {
            encryptionId = nextObjectId;
            nextObjectId++;
        }

        int acroFormId = 0;
        Dictionary<PdfAcroFormField, int> fieldObjectIds = new Dictionary<PdfAcroFormField, int>();
        Dictionary<PdfAcroFormField, int> fieldOnAppearanceObjectIds = new Dictionary<PdfAcroFormField, int>();
        Dictionary<PdfAcroFormField, int> fieldOffAppearanceObjectIds = new Dictionary<PdfAcroFormField, int>();
        Dictionary<PdfRadioButtonGroup, int> radioGroupObjectIds = new Dictionary<PdfRadioButtonGroup, int>();
        if (document.HasAcroForm)
        {
            acroFormId = nextObjectId;
            nextObjectId++;
            foreach (PdfAcroFormField field in document.AcroForm.Fields)
            {
                fieldObjectIds[field] = nextObjectId;
                nextObjectId++;
                fieldOnAppearanceObjectIds[field] = nextObjectId;
                nextObjectId++;
                if (field.FieldType == "Btn")
                {
                    fieldOffAppearanceObjectIds[field] = nextObjectId;
                    nextObjectId++;
                }
            }

            foreach (PdfRadioButtonGroup group in document.AcroForm.RadioButtonGroups)
            {
                radioGroupObjectIds[group] = nextObjectId;
                nextObjectId++;
                foreach (PdfAcroFormField option in group.Options)
                {
                    fieldObjectIds[option] = nextObjectId;
                    nextObjectId++;
                    fieldOnAppearanceObjectIds[option] = nextObjectId;
                    nextObjectId++;
                    fieldOffAppearanceObjectIds[option] = nextObjectId;
                    nextObjectId++;
                }
            }
        }

        int structureRootId = 0;
        int parentTreeId = 0;
        Dictionary<PdfAccessibilityNode, int> structureObjectIds = new Dictionary<PdfAccessibilityNode, int>();
        if (document.HasTaggedPdf)
        {
            structureRootId = nextObjectId;
            nextObjectId++;
            parentTreeId = nextObjectId;
            nextObjectId++;
            foreach (PdfAccessibilityNode node in document.AccessibilityNodes)
            {
                structureObjectIds[node] = nextObjectId;
                nextObjectId++;
            }
        }

        int outlineRootId = 0;
        Dictionary<PdfOutlineItem, int> outlineObjectIds = new Dictionary<PdfOutlineItem, int>();
        if (document.Outlines.Count > 0)
        {
            outlineRootId = nextObjectId;
            nextObjectId++;
            nextObjectId = AddOutlineObjectIds(document.Outlines, outlineObjectIds, nextObjectId);
        }

        List<int> pageObjectIds = new List<int>();
        List<int> contentObjectIds = new List<int>();
        Dictionary<PdfPage, int> pageObjectIdMap = new Dictionary<PdfPage, int>();

        foreach (PdfPage page in document.Pages)
        {
            pageObjectIds.Add(nextObjectId);
            pageObjectIdMap[page] = nextObjectId;
            nextObjectId++;
            contentObjectIds.Add(nextObjectId);
            nextObjectId++;
        }

        int parentTreeNextKey = document.Pages.Count;
        if (document.HasTaggedPdf)
        {
            parentTreeNextKey = AssignStructureParentKeys(document, parentTreeNextKey);
        }

        ValidateInternalLinks(document);
        ValidateFormFields(document);

        string documentId = GetTrailerDocumentId(document);
        PdfEncryptionData encryptionData = new PdfEncryptionData();
        if (document.HasEncryption)
        {
            encryptionData = PdfEncryption.Create(
                document.EncryptionOptions.UserPassword,
                document.EncryptionOptions.OwnerPassword,
                document.EncryptionOptions.Permissions.ToPdfPermissionValue(),
                documentId);
        }

        long[] offsets = new long[nextObjectId];

        string version = GetVersionText(document.WriterOptions.Version);
        WriteAscii(output, "%PDF-" + version + "\n");
        WriteAscii(output, "% Generated without external libraries\n");

        WriteObject(output, offsets, catalogId, () =>
        {
            WriteAscii(output, "<< /Type /Catalog /Pages " + pagesId.ToString(CultureInfo.InvariantCulture) + " 0 R /Version /" + version);
            WriteNamedDestinations(output, document, pageObjectIdMap);
            WritePageLabels(output, document);
            WriteAttachmentCatalogReference(output, document.Attachments, attachmentFileSpecObjectIds);
            WriteAssociatedFilesCatalogReference(output, document, attachmentFileSpecObjectIds);
            WriteDocumentLanguage(output, document);
            WriteOutlineCatalogReference(output, outlineRootId);
            WriteDocumentViewCatalogEntries(output, document, outlineRootId);
            WriteAcroFormCatalogReference(output, acroFormId);
            WriteMetadataCatalogReference(output, metadataId);
            WriteOutputIntentCatalogReference(output, outputIntentId);
            WriteTaggedCatalogReference(output, structureRootId);
            WriteAscii(output, " >>");
        });

        WriteObject(output, offsets, pagesId, () =>
        {
            WriteAscii(output, "<< /Type /Pages /Count " + document.Pages.Count.ToString(CultureInfo.InvariantCulture) + " /Kids [");
            foreach (int pageId in pageObjectIds)
            {
                WriteAscii(output, pageId.ToString(CultureInfo.InvariantCulture) + " 0 R ");
            }

            WriteAscii(output, "] >>");
        });

        foreach (PdfFont font in fonts)
        {
            WriteFontObject(output, offsets, fontObjectIds[font], fontRegistry.GetBaseFontName(font));
        }

        foreach (PdfEmbeddedTrueTypeFont embeddedFont in embeddedFonts)
        {
            WriteEmbeddedTrueTypeFontObjects(output, offsets, embeddedFont, document.HasEncryption, encryptionData);
        }

        foreach (PdfGraphicsState graphicsState in graphicsStates)
        {
            WriteGraphicsStateObject(output, offsets, graphicsStateObjectIds[graphicsState], graphicsState);
        }

        if (infoId != 0)
        {
            WriteInfoObject(output, offsets, infoId, document.Metadata);
        }

        if (metadataId != 0)
        {
            WriteMetadataObject(output, offsets, metadataId, document.Metadata, document.HasEncryption, encryptionData);
        }

        if (outputIntentId != 0)
        {
            WriteOutputIntentObject(output, offsets, outputIntentId, iccProfileId, document.PdfAOptions);
            WriteIccProfileObject(output, offsets, iccProfileId, document.PdfAOptions.IccProfileData);
        }

        foreach (PdfImage image in document.Images)
        {
            int softMaskObjectId = 0;
            if (image.HasSoftMask)
            {
                softMaskObjectId = imageSoftMaskObjectIds[image];
            }

            WriteImageObject(output, offsets, imageObjectIds[image], softMaskObjectId, image, document.HasEncryption, encryptionData);
            if (image.HasSoftMask)
            {
                WriteImageSoftMaskObject(output, offsets, softMaskObjectId, image, document.HasEncryption, encryptionData);
            }
        }

        foreach (PdfFormXObject form in document.Forms)
        {
            WriteFormObject(output, offsets, formObjectIds[form], form, fontRegistry, fonts, fontObjectIds, embeddedFonts, graphicsStates, graphicsStateObjectIds, document.HasEncryption, encryptionData);
        }

        foreach (PdfAttachment attachment in document.Attachments)
        {
            WriteAttachmentObjects(output, offsets, attachment, attachmentEmbeddedFileObjectIds[attachment], attachmentFileSpecObjectIds[attachment], document.HasEncryption, encryptionData);
        }

        if (encryptionId != 0)
        {
            WriteEncryptionObject(output, offsets, encryptionId, encryptionData);
        }

        if (acroFormId != 0)
        {
            WriteAcroFormObject(output, offsets, acroFormId, document.AcroForm, fieldObjectIds, radioGroupObjectIds, fontObjectIds[PdfFont.Regular]);
            WriteAcroFormFieldObjects(output, offsets, document.AcroForm.Fields, fieldObjectIds, fieldOnAppearanceObjectIds, fieldOffAppearanceObjectIds, pageObjectIdMap, fontObjectIds[PdfFont.Regular], document.HasEncryption, encryptionData);
            WriteRadioButtonObjects(output, offsets, document.AcroForm.RadioButtonGroups, radioGroupObjectIds, fieldObjectIds, fieldOnAppearanceObjectIds, fieldOffAppearanceObjectIds, pageObjectIdMap, fontObjectIds[PdfFont.Regular], document.HasEncryption, encryptionData);
        }

        if (outlineRootId != 0)
        {
            WriteOutlineObjects(output, offsets, outlineRootId, document.Outlines, outlineObjectIds, pageObjectIdMap);
        }

        if (structureRootId != 0)
        {
            WriteStructureObjects(output, offsets, document, structureRootId, parentTreeId, parentTreeNextKey, structureObjectIds, pageObjectIdMap, internalLinkObjectIds, externalLinkObjectIds, annotationObjectIds, fieldObjectIds);
            WriteTaggedAnnotationObjects(output, offsets, document, pageObjectIdMap, internalLinkObjectIds, externalLinkObjectIds, annotationObjectIds);
        }

        for (int index = 0; index < document.Pages.Count; index++)
        {
            PdfPage page = document.Pages[index];
            byte[] contentBytes = PdfEncoding.GetBytes(page.Canvas.ToPdfString());
            if (document.WriterOptions.CompressContentStreams)
            {
                contentBytes = CompressBytes(contentBytes);
            }

            if (document.HasEncryption)
            {
                contentBytes = PdfEncryption.EncryptObjectBytes(contentBytes, encryptionData, contentObjectIds[index]);
            }

            WriteObject(output, offsets, pageObjectIds[index], () =>
            {
                WriteAscii(output, "<< /Type /Page ");
                WriteAscii(output, "/Parent " + pagesId.ToString(CultureInfo.InvariantCulture) + " 0 R ");
                WritePageBoxes(output, page);
                if (document.HasTaggedPdf)
                {
                    WriteAscii(output, "/StructParents " + index.ToString(CultureInfo.InvariantCulture) + " ");
                }
                WriteAscii(output, "/Resources << ");
                WriteFontResources(output, fontRegistry, fonts, fontObjectIds, embeddedFonts);
                WriteGraphicsStateResources(output, graphicsStates, graphicsStateObjectIds);
                WriteAscii(output, " ");
                WriteAscii(output, "/XObject << ");

                foreach (PdfImage image in document.Images)
                {
                    WriteAscii(output, "/" + image.Name + " " + imageObjectIds[image].ToString(CultureInfo.InvariantCulture) + " 0 R ");
                }

                foreach (PdfFormXObject form in document.Forms)
                {
                    WriteAscii(output, "/" + form.Name + " " + formObjectIds[form].ToString(CultureInfo.InvariantCulture) + " 0 R ");
                }

                WriteAscii(output, ">> >> ");
                WriteAscii(output, "/Contents " + contentObjectIds[index].ToString(CultureInfo.InvariantCulture) + " 0 R");
                WritePageAnnotations(output, document, page, document.AcroForm, fieldObjectIds, internalLinkObjectIds, externalLinkObjectIds, annotationObjectIds);
                WriteAscii(output, " >>");
            });

            WriteStreamObject(output, offsets, contentObjectIds[index], contentBytes, document.WriterOptions.CompressContentStreams);
        }

        long startXref = output.Position;
        WriteAscii(output, "xref\n0 " + nextObjectId.ToString(CultureInfo.InvariantCulture) + "\n");
        WriteAscii(output, "0000000000 65535 f \n");

        for (int objectId = 1; objectId < nextObjectId; objectId++)
        {
            WriteAscii(output, offsets[objectId].ToString("0000000000", CultureInfo.InvariantCulture) + " 00000 n \n");
        }

        WriteAscii(output, "trailer\n<< /Size " + nextObjectId.ToString(CultureInfo.InvariantCulture) + " /Root " + catalogId.ToString(CultureInfo.InvariantCulture) + " 0 R");
        if (infoId != 0)
        {
            WriteAscii(output, " /Info " + infoId.ToString(CultureInfo.InvariantCulture) + " 0 R");
        }

        if (encryptionId != 0)
        {
            WriteAscii(output, " /Encrypt " + encryptionId.ToString(CultureInfo.InvariantCulture) + " 0 R");
        }

        if (document.WriterOptions.Deterministic || document.HasEncryption)
        {
            WriteAscii(output, " /ID [<" + documentId + "><" + documentId + ">]");
        }

        WriteAscii(output, " >>\n");
        WriteAscii(output, "startxref\n" + startXref.ToString(CultureInfo.InvariantCulture) + "\n%%EOF\n");
    }

    internal static void WriteInfoObject(Stream output, long[] offsets, int objectId, PdfMetadata metadata)
    {
        WriteObject(output, offsets, objectId, () =>
        {
            WriteAscii(output, "<<");
            WriteOptionalInfoText(output, "Title", metadata.Title);
            WriteOptionalInfoText(output, "Author", metadata.Author);
            WriteOptionalInfoText(output, "Subject", metadata.Subject);
            WriteOptionalInfoText(output, "Keywords", metadata.Keywords);
            WriteOptionalInfoText(output, "Creator", metadata.Creator);
            WriteOptionalInfoText(output, "Producer", metadata.Producer);
            WriteOptionalInfoDate(output, "CreationDate", metadata.CreationDate, metadata.HasCreationDateValue);
            WriteOptionalInfoDate(output, "ModDate", metadata.ModificationDate, metadata.HasModificationDateValue);
            WriteAscii(output, " >>");
        });
    }

    internal static string GetVersionText(PdfVersion version)
    {
        if (version == PdfVersion.Pdf14)
        {
            return "1.4";
        }

        if (version == PdfVersion.Pdf15)
        {
            return "1.5";
        }

        if (version == PdfVersion.Pdf16)
        {
            return "1.6";
        }

        if (version == PdfVersion.Pdf17)
        {
            return "1.7";
        }

        if (version == PdfVersion.Pdf20)
        {
            return "2.0";
        }

        throw new ArgumentException("PDF version is not supported.", "version");
    }

    internal static void WriteOptionalInfoDate(Stream output, string key, DateTime value, bool hasValue)
    {
        if (!hasValue)
        {
            return;
        }

        WriteAscii(output, " /" + key + " " + PdfUnicodeMap.EncodeTextString(FormatPdfDate(value)));
    }

    internal static string FormatPdfDate(DateTime value)
    {
        string result = "D:" + value.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        if (value.Kind == DateTimeKind.Utc)
        {
            return result + "Z";
        }

        if (value.Kind == DateTimeKind.Unspecified)
        {
            return result;
        }

        TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(value);
        string sign = "+";
        if (offset < TimeSpan.Zero)
        {
            sign = "-";
            offset = offset.Duration();
        }

        return result + sign + offset.Hours.ToString("00", CultureInfo.InvariantCulture) + "'" + offset.Minutes.ToString("00", CultureInfo.InvariantCulture) + "'";
    }

    internal static void WriteMetadataObject(Stream output, long[] offsets, int objectId, PdfMetadata metadata, bool encrypted, PdfEncryptionData encryptionData)
    {
        string xmpMetadata = metadata.XmpMetadata ?? string.Empty;
        byte[] metadataBytes = Encoding.UTF8.GetBytes(xmpMetadata);
        if (encrypted)
        {
            metadataBytes = PdfEncryption.EncryptObjectBytes(metadataBytes, encryptionData, objectId);
        }

        WriteObject(output, offsets, objectId, () =>
        {
            WriteAscii(output, "<< /Type /Metadata /Subtype /XML /Length " + metadataBytes.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
            output.Write(metadataBytes);
            WriteAscii(output, "\nendstream");
        });
    }

    internal static void WriteMetadataCatalogReference(Stream output, int metadataId)
    {
        if (metadataId != 0)
        {
            WriteAscii(output, " /Metadata " + metadataId.ToString(CultureInfo.InvariantCulture) + " 0 R");
        }
    }

    internal static void WriteOutputIntentCatalogReference(Stream output, int outputIntentId)
    {
        if (outputIntentId != 0)
        {
            WriteAscii(output, " /OutputIntents [" + outputIntentId.ToString(CultureInfo.InvariantCulture) + " 0 R]");
        }
    }

    internal static void WriteTaggedCatalogReference(Stream output, int structureRootId)
    {
        if (structureRootId == 0)
        {
            return;
        }

        WriteAscii(output, " /StructTreeRoot " + structureRootId.ToString(CultureInfo.InvariantCulture) + " 0 R");
        WriteAscii(output, " /MarkInfo << /Marked true >>");
    }

    internal static void WriteOutputIntentObject(Stream output, long[] offsets, int objectId, int profileObjectId, PdfAPreparationOptions options)
    {
        WriteObject(output, offsets, objectId, () =>
        {
            WriteAscii(output, "<< /Type /OutputIntent /S /GTS_PDFA1");
            WriteAscii(output, " /OutputConditionIdentifier " + PdfUnicodeMap.EncodeTextString(options.OutputConditionIdentifier));
            WriteAscii(output, " /OutputCondition " + PdfUnicodeMap.EncodeTextString(options.OutputCondition));
            WriteAscii(output, " /RegistryName " + PdfUnicodeMap.EncodeTextString(options.RegistryName));
            WriteAscii(output, " /Info " + PdfUnicodeMap.EncodeTextString(options.Info));
            WriteAscii(output, " /DestOutputProfile " + profileObjectId.ToString(CultureInfo.InvariantCulture) + " 0 R >>");
        });
    }

    internal static void WriteIccProfileObject(Stream output, long[] offsets, int objectId, byte[] profile)
    {
        int componentCount = PdfAConfiguration.GetIccComponentCount(profile);
        WriteObject(output, offsets, objectId, () =>
        {
            WriteAscii(output, "<< /N " + componentCount.ToString(CultureInfo.InvariantCulture) + " /Length " + profile.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
            output.Write(profile);
            WriteAscii(output, "\nendstream");
        });
    }

    internal static void WriteOptionalInfoText(Stream output, string key, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        WriteAscii(output, " /" + key + " " + PdfUnicodeMap.EncodeTextString(value));
    }

    internal static void WriteImageObject(Stream output, long[] offsets, int objectId, int softMaskObjectId, PdfImage image, bool encrypted, PdfEncryptionData encryptionData)
    {
        if (image.Format == PdfImageFormat.Png)
        {
            WritePngImageObject(output, offsets, objectId, softMaskObjectId, image, encrypted, encryptionData);
            return;
        }

        if (image.Format == PdfImageFormat.Jpeg)
        {
            WriteJpegImageObject(output, offsets, objectId, image, encrypted, encryptionData);
            return;
        }

        string hexData = Convert.ToHexString(image.RgbData);
        byte[] imageBytes = PdfEncoding.GetBytes(hexData + ">");
        if (encrypted)
        {
            imageBytes = PdfEncryption.EncryptObjectBytes(imageBytes, encryptionData, objectId);
        }

        WriteObject(output, offsets, objectId, () =>
        {
            WriteAscii(output, "<< /Type /XObject /Subtype /Image ");
            WriteAscii(output, "/Width " + image.Width.ToString(CultureInfo.InvariantCulture) + " /Height " + image.Height.ToString(CultureInfo.InvariantCulture) + " ");
            WriteAscii(output, "/ColorSpace " + GetImageColorSpaceName(image.ColorSpace) + " /BitsPerComponent 8 ");
            if (softMaskObjectId != 0)
            {
                WriteAscii(output, "/SMask " + softMaskObjectId.ToString(CultureInfo.InvariantCulture) + " 0 R ");
            }

            WriteAscii(output, "/Filter /ASCIIHexDecode ");
            WriteAscii(output, "/Length " + imageBytes.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
            output.Write(imageBytes);
            WriteAscii(output, "\nendstream");
        });
    }

    internal static void WriteJpegImageObject(Stream output, long[] offsets, int objectId, PdfImage image, bool encrypted, PdfEncryptionData encryptionData)
    {
        byte[] imageBytes = image.EncodedData;
        if (encrypted)
        {
            imageBytes = PdfEncryption.EncryptObjectBytes(imageBytes, encryptionData, objectId);
        }

        WriteObject(output, offsets, objectId, () =>
        {
            WriteAscii(output, "<< /Type /XObject /Subtype /Image ");
            WriteAscii(output, "/Width " + image.Width.ToString(CultureInfo.InvariantCulture) + " /Height " + image.Height.ToString(CultureInfo.InvariantCulture) + " ");
            WriteAscii(output, "/ColorSpace " + GetJpegColorSpaceName(image.JpegColorSpace) + " /BitsPerComponent " + image.BitsPerComponent.ToString(CultureInfo.InvariantCulture) + " ");
            if (image.HasInvertedCmykSamples)
            {
                WriteAscii(output, "/Decode [1 0 1 0 1 0 1 0] ");
            }

            WriteAscii(output, "/Filter /DCTDecode ");
            WriteAscii(output, "/Length " + imageBytes.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
            output.Write(imageBytes);
            WriteAscii(output, "\nendstream");
        });
    }

    internal static string GetJpegColorSpaceName(PdfJpegColorSpace colorSpace)
    {
        if (colorSpace == PdfJpegColorSpace.Grayscale)
        {
            return "/DeviceGray";
        }

        if (colorSpace == PdfJpegColorSpace.Cmyk)
        {
            return "/DeviceCMYK";
        }

        return "/DeviceRGB";
    }

    internal static void WritePngImageObject(Stream output, long[] offsets, int objectId, int softMaskObjectId, PdfImage image, bool encrypted, PdfEncryptionData encryptionData)
    {
        byte[] imageBytes = image.CompressedData;
        if (encrypted)
        {
            imageBytes = PdfEncryption.EncryptObjectBytes(imageBytes, encryptionData, objectId);
        }

        WriteObject(output, offsets, objectId, () =>
        {
            WriteAscii(output, "<< /Type /XObject /Subtype /Image ");
            WriteAscii(output, "/Width " + image.Width.ToString(CultureInfo.InvariantCulture) + " /Height " + image.Height.ToString(CultureInfo.InvariantCulture) + " ");
            WriteAscii(output, "/ColorSpace " + GetPngColorSpaceName(image.PngColorSpace) + " /BitsPerComponent " + image.BitsPerComponent.ToString(CultureInfo.InvariantCulture) + " ");
            if (softMaskObjectId != 0)
            {
                WriteAscii(output, "/SMask " + softMaskObjectId.ToString(CultureInfo.InvariantCulture) + " 0 R ");
            }

            WriteAscii(output, "/Filter /FlateDecode ");
            WriteAscii(output, "/Length " + imageBytes.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
            output.Write(imageBytes);
            WriteAscii(output, "\nendstream");
        });
    }

    internal static void WritePngSoftMaskObject(Stream output, long[] offsets, int objectId, PdfImage image, bool encrypted, PdfEncryptionData encryptionData)
    {
        byte[] maskBytes = image.SoftMaskData;
        if (encrypted)
        {
            maskBytes = PdfEncryption.EncryptObjectBytes(maskBytes, encryptionData, objectId);
        }

        WriteObject(output, offsets, objectId, () =>
        {
            WriteAscii(output, "<< /Type /XObject /Subtype /Image ");
            WriteAscii(output, "/Width " + image.Width.ToString(CultureInfo.InvariantCulture) + " /Height " + image.Height.ToString(CultureInfo.InvariantCulture) + " ");
            WriteAscii(output, "/ColorSpace /DeviceGray /BitsPerComponent " + image.SoftMaskBitsPerComponent.ToString(CultureInfo.InvariantCulture) + " ");
            WriteAscii(output, "/Filter /FlateDecode /Length " + maskBytes.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
            output.Write(maskBytes);
            WriteAscii(output, "\nendstream");
        });
    }

    internal static void WriteImageSoftMaskObject(Stream output, long[] offsets, int objectId, PdfImage image, bool encrypted, PdfEncryptionData encryptionData)
    {
        if (image.Format == PdfImageFormat.Png)
        {
            WritePngSoftMaskObject(output, offsets, objectId, image, encrypted, encryptionData);
            return;
        }

        string hexData = Convert.ToHexString(image.SoftMaskData);
        byte[] maskBytes = PdfEncoding.GetBytes(hexData + ">");
        if (encrypted)
        {
            maskBytes = PdfEncryption.EncryptObjectBytes(maskBytes, encryptionData, objectId);
        }

        WriteObject(output, offsets, objectId, () =>
        {
            WriteAscii(output, "<< /Type /XObject /Subtype /Image ");
            WriteAscii(output, "/Width " + image.Width.ToString(CultureInfo.InvariantCulture) + " /Height " + image.Height.ToString(CultureInfo.InvariantCulture) + " ");
            WriteAscii(output, "/ColorSpace /DeviceGray /BitsPerComponent " + image.SoftMaskBitsPerComponent.ToString(CultureInfo.InvariantCulture) + " ");
            WriteAscii(output, "/Filter /ASCIIHexDecode /Length " + maskBytes.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
            output.Write(maskBytes);
            WriteAscii(output, "\nendstream");
        });
    }

    internal static string GetPngColorSpaceName(PdfPngColorSpace colorSpace)
    {
        if (colorSpace == PdfPngColorSpace.Grayscale)
        {
            return "/DeviceGray";
        }

        return "/DeviceRGB";
    }

    internal static string GetImageColorSpaceName(PdfImageColorSpace colorSpace)
    {
        if (colorSpace == PdfImageColorSpace.Grayscale)
        {
            return "/DeviceGray";
        }

        if (colorSpace == PdfImageColorSpace.Cmyk)
        {
            return "/DeviceCMYK";
        }

        return "/DeviceRGB";
    }

    internal static void WriteFontObject(Stream output, long[] offsets, int objectId, string baseFontName)
    {
        WriteObject(output, offsets, objectId, () =>
        {
            WriteAscii(output, "<< /Type /Font /Subtype /Type1 /BaseFont /" + baseFontName + " >>");
        });
    }

    internal static void WriteEmbeddedTrueTypeFontObjects(Stream output, long[] offsets, PdfEmbeddedTrueTypeFont embeddedFont, bool encrypted, PdfEncryptionData encryptionData)
    {
        WriteObject(output, offsets, embeddedFont.Type0ObjectId, () =>
        {
            WriteAscii(output, "<< /Type /Font /Subtype /Type0 /BaseFont /" + embeddedFont.BaseFontName + " ");
            WriteAscii(output, "/Encoding /Identity-H /DescendantFonts [" + embeddedFont.DescendantObjectId.ToString(CultureInfo.InvariantCulture) + " 0 R] ");
            WriteAscii(output, "/ToUnicode " + embeddedFont.ToUnicodeObjectId.ToString(CultureInfo.InvariantCulture) + " 0 R >>");
        });

        WriteObject(output, offsets, embeddedFont.DescendantObjectId, () =>
        {
            PdfTrueTypeFont font = embeddedFont.Usage.Font;
            int defaultWidth = NormalizeFontMetric(font.AdvanceWidths[0], font.UnitsPerEm);
            WriteAscii(output, "<< /Type /Font /Subtype /CIDFontType2 /BaseFont /" + embeddedFont.BaseFontName + " ");
            WriteAscii(output, "/CIDSystemInfo << /Registry (Adobe) /Ordering (Identity) /Supplement 0 >> ");
            WriteAscii(output, "/FontDescriptor " + embeddedFont.DescriptorObjectId.ToString(CultureInfo.InvariantCulture) + " 0 R ");
            WriteAscii(output, "/CIDToGIDMap /Identity /DW " + defaultWidth.ToString(CultureInfo.InvariantCulture));
            WriteTrueTypeWidths(output, embeddedFont.Usage);
            WriteAscii(output, " >>");
        });

        WriteObject(output, offsets, embeddedFont.DescriptorObjectId, () =>
        {
            PdfTrueTypeFont font = embeddedFont.Usage.Font;
            int flags = GetFontDescriptorFlags(font);
            int capHeight = font.CapHeight == 0 ? font.Ascent : font.CapHeight;
            int italicAngle = IsItalicFont(font) ? -12 : 0;
            WriteAscii(output, "<< /Type /FontDescriptor /FontName /" + embeddedFont.BaseFontName + " ");
            WriteAscii(output, "/Flags " + flags.ToString(CultureInfo.InvariantCulture) + " ");
            WriteAscii(output, "/FontBBox [" + NormalizeFontMetric(font.XMin, font.UnitsPerEm).ToString(CultureInfo.InvariantCulture) + " ");
            WriteAscii(output, NormalizeFontMetric(font.YMin, font.UnitsPerEm).ToString(CultureInfo.InvariantCulture) + " ");
            WriteAscii(output, NormalizeFontMetric(font.XMax, font.UnitsPerEm).ToString(CultureInfo.InvariantCulture) + " ");
            WriteAscii(output, NormalizeFontMetric(font.YMax, font.UnitsPerEm).ToString(CultureInfo.InvariantCulture) + "] ");
            WriteAscii(output, "/ItalicAngle " + italicAngle.ToString(CultureInfo.InvariantCulture) + " ");
            WriteAscii(output, "/Ascent " + NormalizeFontMetric(font.Ascent, font.UnitsPerEm).ToString(CultureInfo.InvariantCulture) + " ");
            WriteAscii(output, "/Descent " + NormalizeFontMetric(font.Descent, font.UnitsPerEm).ToString(CultureInfo.InvariantCulture) + " ");
            WriteAscii(output, "/CapHeight " + NormalizeFontMetric(capHeight, font.UnitsPerEm).ToString(CultureInfo.InvariantCulture) + " ");
            WriteAscii(output, "/StemV 80 /FontFile2 " + embeddedFont.FontFileObjectId.ToString(CultureInfo.InvariantCulture) + " 0 R >>");
        });

        byte[] fontProgram = embeddedFont.FontProgram;
        if (encrypted)
        {
            fontProgram = PdfEncryption.EncryptObjectBytes(fontProgram, encryptionData, embeddedFont.FontFileObjectId);
        }

        WriteObject(output, offsets, embeddedFont.FontFileObjectId, () =>
        {
            WriteAscii(output, "<< /Length " + fontProgram.Length.ToString(CultureInfo.InvariantCulture) + " /Length1 " + embeddedFont.FontProgram.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
            output.Write(fontProgram);
            WriteAscii(output, "\nendstream");
        });

        byte[] toUnicodeData = PdfEncoding.GetBytes(CreateTrueTypeToUnicodeCMap(embeddedFont.Usage));
        if (encrypted)
        {
            toUnicodeData = PdfEncryption.EncryptObjectBytes(toUnicodeData, encryptionData, embeddedFont.ToUnicodeObjectId);
        }

        WriteObject(output, offsets, embeddedFont.ToUnicodeObjectId, () =>
        {
            WriteAscii(output, "<< /Length " + toUnicodeData.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
            output.Write(toUnicodeData);
            WriteAscii(output, "\nendstream");
        });
    }

    internal static void WriteTrueTypeWidths(Stream output, PdfTrueTypeFontUsage usage)
    {
        List<int> glyphs = SortGlyphIndices(usage.GlyphIndices);
        WriteAscii(output, " /W [");
        foreach (int glyphIndex in glyphs)
        {
            int width = NormalizeFontMetric(usage.Font.AdvanceWidths[glyphIndex], usage.Font.UnitsPerEm);
            WriteAscii(output, glyphIndex.ToString(CultureInfo.InvariantCulture) + " [" + width.ToString(CultureInfo.InvariantCulture) + "] ");
        }

        WriteAscii(output, "]");
    }

    internal static void WriteInternalLinkAnnotation(Stream output, PdfInternalLink link, int pageObjectId)
    {
        WriteAscii(output, "<< /Type /Annot /Subtype /Link ");
        WriteAnnotationRectangle(output, link.Rectangle);
        WriteAscii(output, "/Border [0 0 0] /Dest /" + EncodeName(link.DestinationName));
        if (pageObjectId != 0)
        {
            WriteAscii(output, " /P " + pageObjectId.ToString(CultureInfo.InvariantCulture) + " 0 R /F 4");
        }

        if (link.StructParent >= 0)
        {
            WriteAscii(output, " /StructParent " + link.StructParent.ToString(CultureInfo.InvariantCulture));
        }

        if (link.StructureTarget != null)
        {
            string description = link.StructureTarget.Description;
            if (description.Length == 0)
            {
                description = link.StructureTarget.Role;
            }

            WriteAscii(output, " /Contents " + PdfUnicodeMap.EncodeTextString(description));
        }

        WriteAscii(output, " >>");
    }

    internal static void WriteExternalLinkAnnotation(Stream output, PdfExternalLink link, int pageObjectId)
    {
        WriteAscii(output, "<< /Type /Annot /Subtype /Link ");
        WriteAnnotationRectangle(output, link.Rectangle);
        WriteAscii(output, "/Border [0 0 0] /A << /S /URI /URI " + PdfUnicodeMap.EncodeTextString(link.Uri) + " >>");
        if (pageObjectId != 0)
        {
            WriteAscii(output, " /P " + pageObjectId.ToString(CultureInfo.InvariantCulture) + " 0 R /F 4");
        }

        if (link.StructParent >= 0)
        {
            WriteAscii(output, " /StructParent " + link.StructParent.ToString(CultureInfo.InvariantCulture));
        }

        WriteAscii(output, " >>");
    }

    internal static void WriteGeneralAnnotation(Stream output, PdfAnnotation annotation, int pageObjectId)
    {
        WriteAscii(output, "<< /Type /Annot /Subtype /" + annotation.Subtype + " ");
        WriteAnnotationRectangle(output, annotation.Rectangle);
        WriteAscii(output, "/Contents " + PdfUnicodeMap.EncodeTextString(annotation.Contents) + " ");
        if (pageObjectId != 0)
        {
            WriteAscii(output, "/P " + pageObjectId.ToString(CultureInfo.InvariantCulture) + " 0 R /F 4 ");
        }

        if (annotation.StructParent >= 0)
        {
            WriteAscii(output, "/StructParent " + annotation.StructParent.ToString(CultureInfo.InvariantCulture) + " ");
        }

        if (annotation.Subtype == "Text")
        {
            WriteAscii(output, "/Name /" + EncodeName(annotation.IconName) + " ");
            if (annotation.Open)
            {
                WriteAscii(output, "/Open true ");
            }
        }

        if (annotation.HasColor)
        {
            WriteAnnotationColor(output, annotation.Color);
        }

        WriteAscii(output, ">>");
    }

    internal static void WriteTaggedAnnotationObjects(Stream output, long[] offsets, PdfDocument document, Dictionary<PdfPage, int> pageObjectIdMap, Dictionary<PdfInternalLink, int> internalLinkObjectIds, Dictionary<PdfExternalLink, int> externalLinkObjectIds, Dictionary<PdfAnnotation, int> annotationObjectIds)
    {
        foreach (PdfPage page in document.Pages)
        {
            foreach (PdfInternalLink link in page.InternalLinks)
            {
                WriteObject(output, offsets, internalLinkObjectIds[link], () =>
                {
                    WriteInternalLinkAnnotation(output, link, pageObjectIdMap[page]);
                });
            }

            foreach (PdfExternalLink link in page.ExternalLinks)
            {
                WriteObject(output, offsets, externalLinkObjectIds[link], () =>
                {
                    WriteExternalLinkAnnotation(output, link, pageObjectIdMap[page]);
                });
            }

            foreach (PdfAnnotation annotation in page.Annotations)
            {
                WriteObject(output, offsets, annotationObjectIds[annotation], () =>
                {
                    WriteGeneralAnnotation(output, annotation, pageObjectIdMap[page]);
                });
            }
        }
    }

    internal static string CreateTrueTypeToUnicodeCMap(PdfTrueTypeFontUsage usage)
    {
        PdfUnicodeMap map = new PdfUnicodeMap();
        List<int> glyphs = SortGlyphIndices(usage.GlyphIndices);
        foreach (int glyphIndex in glyphs)
        {
            for (int index = 0; index < usage.GlyphIndices.Count; index++)
            {
                if (usage.GlyphIndices[index] == glyphIndex)
                {
                    map.Add(glyphIndex, usage.UnicodeValues[index]);
                }
            }
        }

        return map.ToPdfCMap();
    }

    internal static List<int> SortGlyphIndices(IReadOnlyList<int> glyphIndices)
    {
        List<int> sorted = new List<int>();
        foreach (int glyphIndex in glyphIndices)
        {
            int position = 0;
            while (position < sorted.Count && sorted[position] < glyphIndex)
            {
                position++;
            }

            if (position >= sorted.Count || sorted[position] != glyphIndex)
            {
                sorted.Insert(position, glyphIndex);
            }
        }

        return sorted;
    }

    internal static int NormalizeFontMetric(int value, int unitsPerEm)
    {
        return (int)Math.Round(value * 1000.0 / unitsPerEm);
    }

    internal static int GetFontDescriptorFlags(PdfTrueTypeFont font)
    {
        int flags = 32;
        bool fixedPitch = true;
        int width = font.AdvanceWidths[0];
        foreach (int advanceWidth in font.AdvanceWidths)
        {
            if (advanceWidth != width)
            {
                fixedPitch = false;
            }
        }

        if (fixedPitch)
        {
            flags += 1;
        }

        if (font.FamilyName.IndexOf("Serif", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            flags += 2;
        }

        if (IsItalicFont(font))
        {
            flags += 64;
        }

        return flags;
    }

    internal static bool IsItalicFont(PdfTrueTypeFont font)
    {
        return font.SubfamilyName.IndexOf("Italic", StringComparison.OrdinalIgnoreCase) >= 0 || font.SubfamilyName.IndexOf("Oblique", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    internal static void WriteFormObject(Stream output, long[] offsets, int objectId, PdfFormXObject form, PdfFontRegistry fontRegistry, List<PdfFont> fonts, Dictionary<PdfFont, int> fontObjectIds, List<PdfEmbeddedTrueTypeFont> embeddedFonts, List<PdfGraphicsState> graphicsStates, Dictionary<PdfGraphicsState, int> graphicsStateObjectIds, bool encrypted, PdfEncryptionData encryptionData)
    {
        byte[] contentBytes = PdfEncoding.GetBytes(form.Canvas.ToPdfString());
        if (encrypted)
        {
            contentBytes = PdfEncryption.EncryptObjectBytes(contentBytes, encryptionData, objectId);
        }

        WriteObject(output, offsets, objectId, () =>
        {
            WriteAscii(output, "<< /Type /XObject /Subtype /Form /FormType 1 ");
            WriteAscii(output, "/BBox [0 0 " + Number(form.Width) + " " + Number(form.Height) + "] ");
            WriteAscii(output, "/Resources << ");
            WriteFontResources(output, fontRegistry, fonts, fontObjectIds, embeddedFonts);
            WriteGraphicsStateResources(output, graphicsStates, graphicsStateObjectIds);
            WriteAscii(output, " >> ");
            WriteAscii(output, "/Length " + contentBytes.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
            output.Write(contentBytes);
            WriteAscii(output, "endstream");
        });
    }

    internal static void WriteEncryptionObject(Stream output, long[] offsets, int objectId, PdfEncryptionData encryptionData)
    {
        WriteObject(output, offsets, objectId, () =>
        {
            WriteAscii(output, "<< /Filter /Standard /V 1 /R 2 /Length 40 ");
            WriteAscii(output, "/O <" + PdfEncryption.ToHex(encryptionData.OwnerKey) + "> ");
            WriteAscii(output, "/U <" + PdfEncryption.ToHex(encryptionData.UserKey) + "> ");
            WriteAscii(output, "/P " + encryptionData.Permissions.ToString(CultureInfo.InvariantCulture) + " >>");
        });
    }

    internal static void WriteAttachmentObjects(Stream output, long[] offsets, PdfAttachment attachment, int embeddedFileObjectId, int fileSpecObjectId, bool encrypted, PdfEncryptionData encryptionData)
    {
        byte[] data = attachment.Data;
        if (encrypted)
        {
            data = PdfEncryption.EncryptObjectBytes(data, encryptionData, embeddedFileObjectId);
        }

        WriteObject(output, offsets, embeddedFileObjectId, () =>
        {
            WriteAscii(output, "<< /Type /EmbeddedFile ");
            if (attachment.MimeType.Length > 0)
            {
                WriteAscii(output, "/Subtype /" + EncodeName(attachment.MimeType) + " ");
            }

            WriteAscii(output, "/Params << /Size " + attachment.Data.Length.ToString(CultureInfo.InvariantCulture) + " >> ");
            WriteAscii(output, "/Length " + data.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
            output.Write(data);
            WriteAscii(output, "endstream");
        });

        WriteObject(output, offsets, fileSpecObjectId, () =>
        {
            WriteAscii(output, "<< /Type /Filespec ");
            WriteAscii(output, "/F " + PdfUnicodeMap.EncodeTextString(attachment.FileName) + " ");
            WriteAscii(output, "/UF " + PdfUnicodeMap.EncodeTextString(attachment.FileName) + " ");
            WriteAscii(output, "/EF << /F " + embeddedFileObjectId.ToString(CultureInfo.InvariantCulture) + " 0 R ");
            WriteAscii(output, "/UF " + embeddedFileObjectId.ToString(CultureInfo.InvariantCulture) + " 0 R >> ");
            if (attachment.Description.Length > 0)
            {
                WriteAscii(output, "/Desc " + PdfUnicodeMap.EncodeTextString(attachment.Description) + " ");
            }

            if (attachment.IsAssociatedFile)
            {
                WriteAscii(output, "/AFRelationship /" + PdfAConfiguration.GetRelationshipName(attachment.Relationship) + " ");
            }

            WriteAscii(output, ">>");
        });
    }

    internal static void WriteFontResources(Stream output, PdfFontRegistry fontRegistry, List<PdfFont> fonts, Dictionary<PdfFont, int> fontObjectIds, List<PdfEmbeddedTrueTypeFont> embeddedFonts)
    {
        WriteAscii(output, "/Font << ");
        foreach (PdfFont font in fonts)
        {
            WriteAscii(output, "/" + fontRegistry.GetResourceName(font) + " " + fontObjectIds[font].ToString(CultureInfo.InvariantCulture) + " 0 R ");
        }

        foreach (PdfEmbeddedTrueTypeFont embeddedFont in embeddedFonts)
        {
            WriteAscii(output, "/" + embeddedFont.Usage.Font.ResourceName + " " + embeddedFont.Type0ObjectId.ToString(CultureInfo.InvariantCulture) + " 0 R ");
        }

        WriteAscii(output, ">>");
    }

    internal static void WriteGraphicsStateObject(Stream output, long[] offsets, int objectId, PdfGraphicsState graphicsState)
    {
        WriteObject(output, offsets, objectId, () =>
        {
            WriteAscii(output, "<< /Type /ExtGState");
            if (graphicsState.HasFillOpacity)
            {
                WriteAscii(output, " /ca " + Number(graphicsState.FillOpacity));
            }

            if (graphicsState.HasStrokeOpacity)
            {
                WriteAscii(output, " /CA " + Number(graphicsState.StrokeOpacity));
            }

            if (graphicsState.HasBlendMode)
            {
                WriteAscii(output, " /BM /" + graphicsState.GetBlendModeName());
            }

            WriteAscii(output, " >>");
        });
    }

    internal static void WriteGraphicsStateResources(Stream output, List<PdfGraphicsState> graphicsStates, Dictionary<PdfGraphicsState, int> graphicsStateObjectIds)
    {
        if (graphicsStates.Count == 0)
        {
            return;
        }

        WriteAscii(output, " /ExtGState << ");
        foreach (PdfGraphicsState graphicsState in graphicsStates)
        {
            WriteAscii(output, "/" + graphicsState.Name + " " + graphicsStateObjectIds[graphicsState].ToString(CultureInfo.InvariantCulture) + " 0 R ");
        }

        WriteAscii(output, ">>");
    }

    internal static void WriteNamedDestinations(Stream output, PdfDocument document, Dictionary<PdfPage, int> pageObjectIdMap)
    {
        if (document.NamedDestinations.Count == 0)
        {
            return;
        }

        WriteAscii(output, " /Dests << ");
        foreach (PdfNamedDestination destination in document.NamedDestinations)
        {
            WriteAscii(output, "/" + EncodeName(destination.Name) + " [");
            WriteAscii(output, pageObjectIdMap[destination.Page].ToString(CultureInfo.InvariantCulture) + " 0 R /XYZ ");
            WriteAscii(output, Number(destination.X) + " " + Number(destination.Y) + " 0] ");
        }

        WriteAscii(output, ">>");
    }

    internal static void WritePageLabels(Stream output, PdfDocument document)
    {
        if (document.PageLabelRanges.Count == 0)
        {
            return;
        }

        WriteAscii(output, " /PageLabels << /Nums [");
        for (int pageIndex = 0; pageIndex < document.Pages.Count; pageIndex++)
        {
            foreach (PdfPageLabelRange range in document.PageLabelRanges)
            {
                if (range.Page == document.Pages[pageIndex])
                {
                    WriteAscii(output, pageIndex.ToString(CultureInfo.InvariantCulture) + " <<");
                    if (range.Style != PdfPageLabelStyle.None)
                    {
                        WriteAscii(output, " /S /" + GetPageLabelStyleName(range.Style));
                        WriteAscii(output, " /St " + range.StartNumber.ToString(CultureInfo.InvariantCulture));
                    }

                    if (range.Prefix.Length > 0)
                    {
                        WriteAscii(output, " /P " + PdfUnicodeMap.EncodeTextString(range.Prefix));
                    }

                    WriteAscii(output, " >> ");
                }
            }
        }

        WriteAscii(output, "] >>");
    }

    internal static string GetPageLabelStyleName(PdfPageLabelStyle style)
    {
        if (style == PdfPageLabelStyle.Decimal)
        {
            return "D";
        }

        if (style == PdfPageLabelStyle.UpperRoman)
        {
            return "R";
        }

        if (style == PdfPageLabelStyle.LowerRoman)
        {
            return "r";
        }

        if (style == PdfPageLabelStyle.UpperLetters)
        {
            return "A";
        }

        if (style == PdfPageLabelStyle.LowerLetters)
        {
            return "a";
        }

        throw new ArgumentException("Page-label style cannot be serialized.", "style");
    }

    internal static void WriteAttachmentCatalogReference(Stream output, IReadOnlyList<PdfAttachment> attachments, Dictionary<PdfAttachment, int> fileSpecObjectIds)
    {
        if (attachments.Count == 0)
        {
            return;
        }

        List<PdfAttachment> sortedAttachments = SortAttachmentsByFileName(attachments);

        WriteAscii(output, " /Names << /EmbeddedFiles << /Names [");
        foreach (PdfAttachment attachment in sortedAttachments)
        {
            WriteAscii(output, PdfUnicodeMap.EncodeTextString(attachment.FileName) + " ");
            WriteAscii(output, fileSpecObjectIds[attachment].ToString(CultureInfo.InvariantCulture) + " 0 R ");
        }

        WriteAscii(output, "] >> >>");
    }

    internal static void WriteAssociatedFilesCatalogReference(Stream output, PdfDocument document, Dictionary<PdfAttachment, int> fileSpecObjectIds)
    {
        if (document.HasPdfA && document.PdfAOptions.Conformance != PdfAConformance.PdfA3B)
        {
            return;
        }

        List<PdfAttachment> sortedAttachments = SortAttachmentsByFileName(document.Attachments);
        bool hasAssociatedFile = false;
        foreach (PdfAttachment attachment in sortedAttachments)
        {
            if (attachment.IsAssociatedFile)
            {
                hasAssociatedFile = true;
            }
        }

        if (!hasAssociatedFile)
        {
            return;
        }

        WriteAscii(output, " /AF [");
        foreach (PdfAttachment attachment in sortedAttachments)
        {
            if (attachment.IsAssociatedFile)
            {
                WriteAscii(output, fileSpecObjectIds[attachment].ToString(CultureInfo.InvariantCulture) + " 0 R ");
            }
        }

        WriteAscii(output, "]");
    }

    internal static void WriteDocumentLanguage(Stream output, PdfDocument document)
    {
        if (document.Language.Length == 0)
        {
            return;
        }

        WriteAscii(output, " /Lang " + PdfUnicodeMap.EncodeTextString(document.Language));
    }

    internal static void WriteAcroFormCatalogReference(Stream output, int acroFormId)
    {
        if (acroFormId == 0)
        {
            return;
        }

        WriteAscii(output, " /AcroForm " + acroFormId.ToString(CultureInfo.InvariantCulture) + " 0 R");
    }

    internal static void WriteAcroFormObject(Stream output, long[] offsets, int objectId, PdfAcroForm acroForm, Dictionary<PdfAcroFormField, int> fieldObjectIds, Dictionary<PdfRadioButtonGroup, int> radioGroupObjectIds, int regularFontObjectId)
    {
        WriteObject(output, offsets, objectId, () =>
        {
            WriteAscii(output, "<< /Fields [");
            foreach (PdfAcroFormField field in acroForm.Fields)
            {
                WriteAscii(output, fieldObjectIds[field].ToString(CultureInfo.InvariantCulture) + " 0 R ");
            }

            foreach (PdfRadioButtonGroup group in acroForm.RadioButtonGroups)
            {
                WriteAscii(output, radioGroupObjectIds[group].ToString(CultureInfo.InvariantCulture) + " 0 R ");
            }

            WriteAscii(output, "] ");
            WriteAscii(output, "/DR << /Font << /Helv " + regularFontObjectId.ToString(CultureInfo.InvariantCulture) + " 0 R >> >> ");
            WriteAscii(output, "/DA " + PdfUnicodeMap.EncodeTextString(acroForm.DefaultAppearance));
            if (acroForm.NeedAppearances)
            {
                WriteAscii(output, " /NeedAppearances true");
            }

            WriteAscii(output, " >>");
        });
    }

    internal static void WriteAcroFormFieldObjects(Stream output, long[] offsets, IReadOnlyList<PdfAcroFormField> fields, Dictionary<PdfAcroFormField, int> fieldObjectIds, Dictionary<PdfAcroFormField, int> fieldOnAppearanceObjectIds, Dictionary<PdfAcroFormField, int> fieldOffAppearanceObjectIds, Dictionary<PdfPage, int> pageObjectIdMap, int regularFontObjectId, bool encrypted, PdfEncryptionData encryptionData)
    {
        foreach (PdfAcroFormField field in fields)
        {
            WriteObject(output, offsets, fieldObjectIds[field], () =>
            {
                WriteAscii(output, "<< /Type /Annot /Subtype /Widget ");
                WriteAnnotationRectangle(output, field.Rectangle);
                WriteAscii(output, "/P " + pageObjectIdMap[field.Page].ToString(CultureInfo.InvariantCulture) + " 0 R ");
                WriteAscii(output, "/FT /" + field.FieldType + " ");
                WriteAscii(output, "/T " + PdfUnicodeMap.EncodeTextString(field.Name) + " ");
                WriteAscii(output, "/F 4 ");
                WriteFieldAccessibilityName(output, field.AccessibilityName);
                if (field.StructParent >= 0)
                {
                    WriteAscii(output, "/StructParent " + field.StructParent.ToString(CultureInfo.InvariantCulture) + " ");
                }
                if (field.FieldType == "Tx")
                {
                    WriteAscii(output, "/V " + PdfUnicodeMap.EncodeTextString(field.Value) + " ");
                    WriteAscii(output, "/DA (/Helv 0 Tf 0 g) ");
                    WriteAscii(output, "/AP << /N " + fieldOnAppearanceObjectIds[field].ToString(CultureInfo.InvariantCulture) + " 0 R >> ");
                }
                else if (field.FieldType == "Btn")
                {
                    WriteCheckboxState(output, field, fieldOnAppearanceObjectIds[field], fieldOffAppearanceObjectIds[field]);
                }
                else if (field.FieldType == "Sig")
                {
                    WriteSignaturePlaceholder(output, field);
                    WriteAscii(output, "/AP << /N " + fieldOnAppearanceObjectIds[field].ToString(CultureInfo.InvariantCulture) + " 0 R >> ");
                }

                WriteAscii(output, ">>");
            });

            WriteFieldAppearanceObject(output, offsets, fieldOnAppearanceObjectIds[field], field, true, regularFontObjectId, encrypted, encryptionData);
            if (field.FieldType == "Btn")
            {
                WriteFieldAppearanceObject(output, offsets, fieldOffAppearanceObjectIds[field], field, false, regularFontObjectId, encrypted, encryptionData);
            }
        }
    }

    internal static void WriteCheckboxState(Stream output, PdfAcroFormField field, int onAppearanceObjectId, int offAppearanceObjectId)
    {
        if (field.Checked)
        {
            WriteAscii(output, "/V /Yes /AS /Yes ");
        }
        else
        {
            WriteAscii(output, "/V /Off /AS /Off ");
        }

        WriteAscii(output, "/AP << /N << /Off " + offAppearanceObjectId.ToString(CultureInfo.InvariantCulture) + " 0 R /Yes " + onAppearanceObjectId.ToString(CultureInfo.InvariantCulture) + " 0 R >> >> ");
    }

    internal static void WriteRadioButtonObjects(Stream output, long[] offsets, IReadOnlyList<PdfRadioButtonGroup> groups, Dictionary<PdfRadioButtonGroup, int> radioGroupObjectIds, Dictionary<PdfAcroFormField, int> fieldObjectIds, Dictionary<PdfAcroFormField, int> fieldOnAppearanceObjectIds, Dictionary<PdfAcroFormField, int> fieldOffAppearanceObjectIds, Dictionary<PdfPage, int> pageObjectIdMap, int regularFontObjectId, bool encrypted, PdfEncryptionData encryptionData)
    {
        foreach (PdfRadioButtonGroup group in groups)
        {
            WriteObject(output, offsets, radioGroupObjectIds[group], () =>
            {
                WriteAscii(output, "<< /FT /Btn /Ff 32768 /T " + PdfUnicodeMap.EncodeTextString(group.Name) + " ");
                WriteFieldAccessibilityName(output, group.AccessibilityName);
                if (group.SelectedValue.Length > 0)
                {
                    WriteAscii(output, "/V /" + EncodeName(group.SelectedValue) + " ");
                }
                else
                {
                    WriteAscii(output, "/V /Off ");
                }

                WriteAscii(output, "/Kids [");
                foreach (PdfAcroFormField option in group.Options)
                {
                    WriteAscii(output, fieldObjectIds[option].ToString(CultureInfo.InvariantCulture) + " 0 R ");
                }

                WriteAscii(output, "] >>");
            });

            foreach (PdfAcroFormField option in group.Options)
            {
                WriteObject(output, offsets, fieldObjectIds[option], () =>
                {
                    WriteAscii(output, "<< /Type /Annot /Subtype /Widget ");
                    WriteAnnotationRectangle(output, option.Rectangle);
                    WriteAscii(output, "/P " + pageObjectIdMap[option.Page].ToString(CultureInfo.InvariantCulture) + " 0 R ");
                    WriteAscii(output, "/Parent " + radioGroupObjectIds[group].ToString(CultureInfo.InvariantCulture) + " 0 R /F 4 ");
                    WriteFieldAccessibilityName(output, option.AccessibilityName);
                    if (option.StructParent >= 0)
                    {
                        WriteAscii(output, "/StructParent " + option.StructParent.ToString(CultureInfo.InvariantCulture) + " ");
                    }
                    if (option.Checked)
                    {
                        WriteAscii(output, "/AS /" + EncodeName(option.ExportValue) + " ");
                    }
                    else
                    {
                        WriteAscii(output, "/AS /Off ");
                    }

                    WriteAscii(output, "/AP << /N << /Off " + fieldOffAppearanceObjectIds[option].ToString(CultureInfo.InvariantCulture) + " 0 R /");
                    WriteAscii(output, EncodeName(option.ExportValue) + " " + fieldOnAppearanceObjectIds[option].ToString(CultureInfo.InvariantCulture) + " 0 R >> >> >>");
                });

                WriteFieldAppearanceObject(output, offsets, fieldOnAppearanceObjectIds[option], option, true, regularFontObjectId, encrypted, encryptionData);
                WriteFieldAppearanceObject(output, offsets, fieldOffAppearanceObjectIds[option], option, false, regularFontObjectId, encrypted, encryptionData);
            }
        }
    }

    internal static void WriteFieldAccessibilityName(Stream output, string accessibilityName)
    {
        if (accessibilityName.Length > 0)
        {
            WriteAscii(output, "/TU " + PdfUnicodeMap.EncodeTextString(accessibilityName) + " ");
        }
    }

    internal static void WriteFieldAppearanceObject(Stream output, long[] offsets, int objectId, PdfAcroFormField field, bool onState, int regularFontObjectId, bool encrypted, PdfEncryptionData encryptionData)
    {
        PdfCanvas canvas = CreateFieldAppearanceCanvas(field, onState);
        byte[] contentBytes = PdfEncoding.GetBytes(canvas.ToPdfString());
        if (encrypted)
        {
            contentBytes = PdfEncryption.EncryptObjectBytes(contentBytes, encryptionData, objectId);
        }

        WriteObject(output, offsets, objectId, () =>
        {
            WriteAscii(output, "<< /Type /XObject /Subtype /Form /FormType 1 /BBox [0 0 " + Number(field.Rectangle.Width) + " " + Number(field.Rectangle.Height) + "] ");
            WriteAscii(output, "/Resources << /Font << /F1 " + regularFontObjectId.ToString(CultureInfo.InvariantCulture) + " 0 R >> >> ");
            WriteAscii(output, "/Length " + contentBytes.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
            output.Write(contentBytes);
            WriteAscii(output, "endstream");
        });
    }

    internal static PdfCanvas CreateFieldAppearanceCanvas(PdfAcroFormField field, bool onState)
    {
        PdfCanvas canvas = new PdfCanvas();
        double width = field.Rectangle.Width;
        double height = field.Rectangle.Height;
        if (field.IsRadioOption)
        {
            double radius = Math.Min(width, height) / 2;
            canvas.StrokeCircle(width / 2, height / 2, radius);
            if (onState)
            {
                canvas.FillCircle(width / 2, height / 2, radius / 2);
            }

            return canvas;
        }

        canvas.StrokeRectangle(0, 0, width, height);
        if (field.FieldType == "Tx" && field.Value.Length > 0)
        {
            canvas.DrawText(field.Value, 2, (height - 10) / 2, 10);
        }
        else if (field.FieldType == "Btn" && onState)
        {
            canvas.DrawLine(0, 0, width, height);
            canvas.DrawLine(width, 0, 0, height);
        }
        else if (field.FieldType == "Sig")
        {
            string label = field.AccessibilityName;
            if (label.Length == 0)
            {
                label = "Signature";
            }

            canvas.DrawText(label, 2, (height - 10) / 2, 10);
        }

        return canvas;
    }

    internal static void WriteSignaturePlaceholder(Stream output, PdfAcroFormField field)
    {
        WriteAscii(output, "/V << /Type /Sig ");
        WriteAscii(output, "/Filter /" + EncodeName(field.SignatureFilter) + " ");
        WriteAscii(output, "/SubFilter /" + EncodeName(field.SignatureSubFilter) + " ");
        WriteAscii(output, "/ByteRange [0 0000000000 0000000000 0000000000] ");
        WriteAscii(output, "/Contents <" + ZeroHex(field.SignatureContentsLength) + ">");
        WriteOptionalSignatureText(output, "Reason", field.SignatureReason);
        WriteOptionalSignatureText(output, "Location", field.SignatureLocation);
        WriteOptionalSignatureText(output, "ContactInfo", field.SignatureContactInfo);
        WriteAscii(output, " >> ");
    }

    internal static void WriteOptionalSignatureText(Stream output, string key, string value)
    {
        if (value.Length == 0)
        {
            return;
        }

        WriteAscii(output, "/" + key + " " + PdfUnicodeMap.EncodeTextString(value) + " ");
    }

    internal static string ZeroHex(int byteCount)
    {
        if (byteCount < 0 || byteCount > int.MaxValue / 2)
        {
            throw new ArgumentOutOfRangeException("byteCount", "Byte count must be nonnegative and small enough to encode as hexadecimal text.");
        }

        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < byteCount; index++)
        {
            builder.Append("00");
        }

        return builder.ToString();
    }

    internal static void WriteOutlineCatalogReference(Stream output, int outlineRootId)
    {
        if (outlineRootId == 0)
        {
            return;
        }

        WriteAscii(output, " /Outlines " + outlineRootId.ToString(CultureInfo.InvariantCulture) + " 0 R");
    }

    internal static void WriteDocumentViewCatalogEntries(Stream output, PdfDocument document, int outlineRootId)
    {
        if (document.HasInitialPageLayout)
        {
            WriteAscii(output, " /PageLayout /" + PageLayoutName(document.InitialPageLayout));
        }

        if (document.HasInitialPageMode)
        {
            WriteAscii(output, " /PageMode /" + PageModeName(document.InitialPageMode));
        }
        else if (outlineRootId != 0)
        {
            WriteAscii(output, " /PageMode /UseOutlines");
        }

        if (document.HasViewerPreferences)
        {
            PdfViewerPreferences preferences = document.ViewerPreferences;
            WriteAscii(output, " /ViewerPreferences <<");
            WriteAscii(output, " /HideToolbar " + BooleanName(preferences.HideToolbar));
            WriteAscii(output, " /HideMenubar " + BooleanName(preferences.HideMenubar));
            WriteAscii(output, " /HideWindowUI " + BooleanName(preferences.HideWindowUi));
            WriteAscii(output, " /FitWindow " + BooleanName(preferences.FitWindow));
            WriteAscii(output, " /CenterWindow " + BooleanName(preferences.CenterWindow));
            WriteAscii(output, " /DisplayDocTitle " + BooleanName(preferences.DisplayDocumentTitle));
            WriteAscii(output, " /NonFullScreenPageMode /" + PageModeName(preferences.NonFullScreenPageMode));
            WriteAscii(output, " /Direction /" + ReadingDirectionName(preferences.ReadingDirection));
            WriteAscii(output, " >>");
        }
    }

    internal static void WritePageBoxes(Stream output, PdfPage page)
    {
        WritePageBox(output, "MediaBox", page.MediaBox);
        if (page.HasCropBox)
        {
            WritePageBox(output, "CropBox", page.CropBox);
        }

        if (page.HasBleedBox)
        {
            WritePageBox(output, "BleedBox", page.BleedBox);
        }

        if (page.HasTrimBox)
        {
            WritePageBox(output, "TrimBox", page.TrimBox);
        }

        if (page.HasArtBox)
        {
            WritePageBox(output, "ArtBox", page.ArtBox);
        }
    }

    internal static void WritePageBox(Stream output, string name, PdfRectangle box)
    {
        WriteAscii(output, "/" + name + " [" + Number(box.X) + " " + Number(box.Y) + " " + Number(box.X + box.Width) + " " + Number(box.Y + box.Height) + "] ");
    }

    internal static string BooleanName(bool value)
    {
        if (value)
        {
            return "true";
        }

        return "false";
    }

    internal static string PageModeName(PdfPageMode mode)
    {
        if (mode == PdfPageMode.UseOutlines)
        {
            return "UseOutlines";
        }

        if (mode == PdfPageMode.UseThumbnails)
        {
            return "UseThumbs";
        }

        if (mode == PdfPageMode.FullScreen)
        {
            return "FullScreen";
        }

        if (mode == PdfPageMode.UseOptionalContent)
        {
            return "UseOC";
        }

        if (mode == PdfPageMode.UseAttachments)
        {
            return "UseAttachments";
        }

        return "UseNone";
    }

    internal static string PageLayoutName(PdfPageLayout layout)
    {
        if (layout == PdfPageLayout.OneColumn)
        {
            return "OneColumn";
        }

        if (layout == PdfPageLayout.TwoColumnLeft)
        {
            return "TwoColumnLeft";
        }

        if (layout == PdfPageLayout.TwoColumnRight)
        {
            return "TwoColumnRight";
        }

        if (layout == PdfPageLayout.TwoPageLeft)
        {
            return "TwoPageLeft";
        }

        if (layout == PdfPageLayout.TwoPageRight)
        {
            return "TwoPageRight";
        }

        return "SinglePage";
    }

    internal static string ReadingDirectionName(PdfReadingDirection direction)
    {
        if (direction == PdfReadingDirection.RightToLeft)
        {
            return "R2L";
        }

        return "L2R";
    }

    internal static void WriteOutlineObjects(Stream output, long[] offsets, int outlineRootId, IReadOnlyList<PdfOutlineItem> outlines, Dictionary<PdfOutlineItem, int> outlineObjectIds, Dictionary<PdfPage, int> pageObjectIdMap)
    {
        WriteObject(output, offsets, outlineRootId, () =>
        {
            WriteAscii(output, "<< /Type /Outlines ");
            WriteAscii(output, "/First " + outlineObjectIds[outlines[0]].ToString(CultureInfo.InvariantCulture) + " 0 R ");
            WriteAscii(output, "/Last " + outlineObjectIds[outlines[outlines.Count - 1]].ToString(CultureInfo.InvariantCulture) + " 0 R ");
            WriteAscii(output, "/Count " + CountOutlineItems(outlines).ToString(CultureInfo.InvariantCulture) + " >>");
        });

        WriteOutlineItemObjects(output, offsets, outlines, outlineRootId, outlineObjectIds, pageObjectIdMap);
    }

    internal static void WriteOutlineItemObjects(Stream output, long[] offsets, IReadOnlyList<PdfOutlineItem> items, int parentObjectId, Dictionary<PdfOutlineItem, int> outlineObjectIds, Dictionary<PdfPage, int> pageObjectIdMap)
    {
        for (int index = 0; index < items.Count; index++)
        {
            PdfOutlineItem item = items[index];
            int objectId = outlineObjectIds[item];

            WriteObject(output, offsets, objectId, () =>
            {
                WriteAscii(output, "<< /Title " + PdfUnicodeMap.EncodeTextString(item.Title) + " ");
                WriteAscii(output, "/Parent " + parentObjectId.ToString(CultureInfo.InvariantCulture) + " 0 R ");
                if (index > 0)
                {
                    WriteAscii(output, "/Prev " + outlineObjectIds[items[index - 1]].ToString(CultureInfo.InvariantCulture) + " 0 R ");
                }

                if (index + 1 < items.Count)
                {
                    WriteAscii(output, "/Next " + outlineObjectIds[items[index + 1]].ToString(CultureInfo.InvariantCulture) + " 0 R ");
                }

                WriteAscii(output, "/Dest [");
                WriteAscii(output, pageObjectIdMap[item.Page].ToString(CultureInfo.InvariantCulture) + " 0 R /XYZ ");
                WriteAscii(output, Number(item.X) + " " + Number(item.Y) + " 0] ");

                if (item.Children.Count > 0)
                {
                    WriteAscii(output, "/First " + outlineObjectIds[item.Children[0]].ToString(CultureInfo.InvariantCulture) + " 0 R ");
                    WriteAscii(output, "/Last " + outlineObjectIds[item.Children[item.Children.Count - 1]].ToString(CultureInfo.InvariantCulture) + " 0 R ");
                    WriteAscii(output, "/Count " + CountOutlineItems(item.Children).ToString(CultureInfo.InvariantCulture) + " ");
                }

                WriteAscii(output, ">>");
            });

            if (item.Children.Count > 0)
            {
                WriteOutlineItemObjects(output, offsets, item.Children, objectId, outlineObjectIds, pageObjectIdMap);
            }
        }
    }

    internal static int AddOutlineObjectIds(IReadOnlyList<PdfOutlineItem> items, Dictionary<PdfOutlineItem, int> outlineObjectIds, int nextObjectId)
    {
        foreach (PdfOutlineItem item in items)
        {
            outlineObjectIds[item] = nextObjectId;
            nextObjectId++;
            nextObjectId = AddOutlineObjectIds(item.Children, outlineObjectIds, nextObjectId);
        }

        return nextObjectId;
    }

    internal static int CountOutlineItems(IReadOnlyList<PdfOutlineItem> items)
    {
        int count = 0;
        foreach (PdfOutlineItem item in items)
        {
            count++;
            count += CountOutlineItems(item.Children);
        }

        return count;
    }

    internal static void WritePageAnnotations(Stream output, PdfDocument document, PdfPage page, PdfAcroForm acroForm, Dictionary<PdfAcroFormField, int> fieldObjectIds, Dictionary<PdfInternalLink, int> internalLinkObjectIds, Dictionary<PdfExternalLink, int> externalLinkObjectIds, Dictionary<PdfAnnotation, int> annotationObjectIds)
    {
        if (page.InternalLinks.Count == 0 && page.ExternalLinks.Count == 0 && page.Annotations.Count == 0 && !HasFormFieldsOnPage(acroForm, page))
        {
            return;
        }

        WriteAscii(output, " /Annots [");
        foreach (PdfAcroFormField field in acroForm.Fields)
        {
            if (field.Page == page)
            {
                WriteAscii(output, fieldObjectIds[field].ToString(CultureInfo.InvariantCulture) + " 0 R ");
            }
        }

        foreach (PdfRadioButtonGroup group in acroForm.RadioButtonGroups)
        {
            foreach (PdfAcroFormField option in group.Options)
            {
                if (option.Page == page)
                {
                    WriteAscii(output, fieldObjectIds[option].ToString(CultureInfo.InvariantCulture) + " 0 R ");
                }
            }
        }

        foreach (PdfInternalLink link in page.InternalLinks)
        {
            if (document.HasTaggedPdf)
            {
                WriteAscii(output, internalLinkObjectIds[link].ToString(CultureInfo.InvariantCulture) + " 0 R ");
            }
            else
            {
                WriteInternalLinkAnnotation(output, link, 0);
            }
        }

        foreach (PdfExternalLink link in page.ExternalLinks)
        {
            if (document.HasTaggedPdf)
            {
                WriteAscii(output, externalLinkObjectIds[link].ToString(CultureInfo.InvariantCulture) + " 0 R ");
            }
            else
            {
                WriteExternalLinkAnnotation(output, link, 0);
            }
        }

        foreach (PdfAnnotation annotation in page.Annotations)
        {
            if (document.HasTaggedPdf)
            {
                WriteAscii(output, annotationObjectIds[annotation].ToString(CultureInfo.InvariantCulture) + " 0 R ");
            }
            else
            {
                WriteGeneralAnnotation(output, annotation, 0);
            }
        }

        WriteAscii(output, "]");
    }

    internal static bool HasFormFieldsOnPage(PdfAcroForm acroForm, PdfPage page)
    {
        foreach (PdfAcroFormField field in acroForm.Fields)
        {
            if (field.Page == page)
            {
                return true;
            }
        }

        foreach (PdfRadioButtonGroup group in acroForm.RadioButtonGroups)
        {
            foreach (PdfAcroFormField option in group.Options)
            {
                if (option.Page == page)
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static void WriteAnnotationRectangle(Stream output, PdfRectangle rectangle)
    {
        WriteAscii(output, "/Rect [" + Number(rectangle.X) + " " + Number(rectangle.Y) + " ");
        WriteAscii(output, Number(rectangle.X + rectangle.Width) + " " + Number(rectangle.Y + rectangle.Height) + "] ");
    }

    internal static void WriteAnnotationColor(Stream output, PdfColor color)
    {
        if (color.Mode == "GRAY")
        {
            WriteAscii(output, "/C [" + Number(color.First) + " " + Number(color.First) + " " + Number(color.First) + "] ");
        }
        else if (color.Mode == "CMYK")
        {
            WriteAscii(output, "/C [" + Number(color.First) + " " + Number(color.Second) + " " + Number(color.Third) + " " + Number(color.Fourth) + "] ");
        }
        else
        {
            WriteAscii(output, "/C [" + Number(color.First) + " " + Number(color.Second) + " " + Number(color.Third) + "] ");
        }
    }

    internal static void ValidateInternalLinks(PdfDocument document)
    {
        foreach (PdfPage page in document.Pages)
        {
            foreach (PdfInternalLink link in page.InternalLinks)
            {
                if (!HasNamedDestination(document, link.DestinationName))
                {
                    throw new InvalidOperationException("Internal link destination '" + link.DestinationName + "' is not defined.");
                }
            }
        }
    }

    internal static int AssignStructureParentKeys(PdfDocument document, int nextKey)
    {
        foreach (PdfPage page in document.Pages)
        {
            foreach (PdfInternalLink link in page.InternalLinks)
            {
                if (link.TagNode != null)
                {
                    link.StructParent = nextKey;
                    nextKey++;
                }
            }

            foreach (PdfExternalLink link in page.ExternalLinks)
            {
                if (link.TagNode != null)
                {
                    link.StructParent = nextKey;
                    nextKey++;
                }
            }

            foreach (PdfAnnotation annotation in page.Annotations)
            {
                if (annotation.TagNode != null)
                {
                    annotation.StructParent = nextKey;
                    nextKey++;
                }
            }

            foreach (PdfAcroFormField field in document.AcroForm.Fields)
            {
                if (field.Page == page && field.TagNode != null)
                {
                    field.StructParent = nextKey;
                    nextKey++;
                }
            }

            foreach (PdfRadioButtonGroup group in document.AcroForm.RadioButtonGroups)
            {
                foreach (PdfAcroFormField option in group.Options)
                {
                    if (option.Page == page && option.TagNode != null)
                    {
                        option.StructParent = nextKey;
                        nextKey++;
                    }
                }
            }
        }

        return nextKey;
    }

    internal static void WriteStructureObjects(Stream output, long[] offsets, PdfDocument document, int structureRootId, int parentTreeId, int parentTreeNextKey, Dictionary<PdfAccessibilityNode, int> structureObjectIds, Dictionary<PdfPage, int> pageObjectIdMap, Dictionary<PdfInternalLink, int> internalLinkObjectIds, Dictionary<PdfExternalLink, int> externalLinkObjectIds, Dictionary<PdfAnnotation, int> annotationObjectIds, Dictionary<PdfAcroFormField, int> fieldObjectIds)
    {
        WriteObject(output, offsets, structureRootId, () =>
        {
            WriteAscii(output, "<< /Type /StructTreeRoot /K [");
            foreach (PdfAccessibilityNode root in document.StructureRoots)
            {
                WriteAscii(output, structureObjectIds[root].ToString(CultureInfo.InvariantCulture) + " 0 R ");
            }

            WriteAscii(output, "] /ParentTree " + parentTreeId.ToString(CultureInfo.InvariantCulture) + " 0 R");
            WriteAscii(output, " /ParentTreeNextKey " + parentTreeNextKey.ToString(CultureInfo.InvariantCulture) + " >>");
        });

        WriteParentTreeObject(output, offsets, document, parentTreeId, structureObjectIds);
        foreach (PdfAccessibilityNode node in document.AccessibilityNodes)
        {
            WriteStructureElementObject(output, offsets, document, node, structureRootId, structureObjectIds, pageObjectIdMap, internalLinkObjectIds, externalLinkObjectIds, annotationObjectIds, fieldObjectIds);
        }
    }

    internal static void WriteParentTreeObject(Stream output, long[] offsets, PdfDocument document, int parentTreeId, Dictionary<PdfAccessibilityNode, int> structureObjectIds)
    {
        WriteObject(output, offsets, parentTreeId, () =>
        {
            WriteAscii(output, "<< /Nums [");
            for (int pageIndex = 0; pageIndex < document.Pages.Count; pageIndex++)
            {
                PdfPage page = document.Pages[pageIndex];
                WriteAscii(output, pageIndex.ToString(CultureInfo.InvariantCulture) + " [");
                foreach (PdfAccessibilityNode node in page.Canvas.MarkedContentNodes)
                {
                    WriteAscii(output, structureObjectIds[node].ToString(CultureInfo.InvariantCulture) + " 0 R ");
                }

                WriteAscii(output, "] ");
            }

            WriteObjectParentTreeEntries(output, document, structureObjectIds);
            WriteAscii(output, "] >>");
        });
    }

    internal static void WriteObjectParentTreeEntries(Stream output, PdfDocument document, Dictionary<PdfAccessibilityNode, int> structureObjectIds)
    {
        foreach (PdfPage page in document.Pages)
        {
            foreach (PdfInternalLink link in page.InternalLinks)
            {
                WriteStructureParentEntry(output, link.StructParent, link.TagNode, structureObjectIds);
            }

            foreach (PdfExternalLink link in page.ExternalLinks)
            {
                WriteStructureParentEntry(output, link.StructParent, link.TagNode, structureObjectIds);
            }

            foreach (PdfAnnotation annotation in page.Annotations)
            {
                WriteStructureParentEntry(output, annotation.StructParent, annotation.TagNode, structureObjectIds);
            }

            foreach (PdfAcroFormField field in document.AcroForm.Fields)
            {
                if (field.Page == page)
                {
                    WriteStructureParentEntry(output, field.StructParent, field.TagNode, structureObjectIds);
                }
            }

            foreach (PdfRadioButtonGroup group in document.AcroForm.RadioButtonGroups)
            {
                foreach (PdfAcroFormField option in group.Options)
                {
                    if (option.Page == page)
                    {
                        WriteStructureParentEntry(output, option.StructParent, option.TagNode, structureObjectIds);
                    }
                }
            }
        }
    }

    internal static void WriteStructureParentEntry(Stream output, int key, PdfAccessibilityNode? node, Dictionary<PdfAccessibilityNode, int> structureObjectIds)
    {
        if (key >= 0 && node != null)
        {
            WriteAscii(output, key.ToString(CultureInfo.InvariantCulture) + " " + structureObjectIds[node].ToString(CultureInfo.InvariantCulture) + " 0 R ");
        }
    }

    internal static void WriteStructureElementObject(Stream output, long[] offsets, PdfDocument document, PdfAccessibilityNode node, int structureRootId, Dictionary<PdfAccessibilityNode, int> structureObjectIds, Dictionary<PdfPage, int> pageObjectIdMap, Dictionary<PdfInternalLink, int> internalLinkObjectIds, Dictionary<PdfExternalLink, int> externalLinkObjectIds, Dictionary<PdfAnnotation, int> annotationObjectIds, Dictionary<PdfAcroFormField, int> fieldObjectIds)
    {
        WriteObject(output, offsets, structureObjectIds[node], () =>
        {
            WriteAscii(output, "<< /Type /StructElem /S /" + node.Role + " /P ");
            if (node.Parent == null)
            {
                WriteAscii(output, structureRootId.ToString(CultureInfo.InvariantCulture) + " 0 R ");
            }
            else
            {
                WriteAscii(output, structureObjectIds[node.Parent].ToString(CultureInfo.InvariantCulture) + " 0 R ");
            }

            WriteAscii(output, "/Pg " + pageObjectIdMap[node.Page].ToString(CultureInfo.InvariantCulture) + " 0 R ");
            if (node.AlternateText.Length > 0)
            {
                WriteAscii(output, "/Alt " + PdfUnicodeMap.EncodeTextString(node.AlternateText) + " ");
            }

            if (node.ActualText.Length > 0)
            {
                WriteAscii(output, "/ActualText " + PdfUnicodeMap.EncodeTextString(node.ActualText) + " ");
            }

            if (node.Language.Length > 0)
            {
                WriteAscii(output, "/Lang " + PdfUnicodeMap.EncodeTextString(node.Language) + " ");
            }

            if (node.HasTableHeaderScope)
            {
                WriteAscii(output, "/A << /O /Table /Scope /" + GetTableHeaderScopeName(node.TableHeaderScope) + " >> ");
            }

            WriteAscii(output, "/K [");
            foreach (int markedContentId in node.MarkedContentIds)
            {
                WriteAscii(output, markedContentId.ToString(CultureInfo.InvariantCulture) + " ");
            }

            WriteStructureObjectReferences(output, document, node, pageObjectIdMap, internalLinkObjectIds, externalLinkObjectIds, annotationObjectIds, fieldObjectIds);
            foreach (PdfAccessibilityNode child in node.Children)
            {
                WriteAscii(output, structureObjectIds[child].ToString(CultureInfo.InvariantCulture) + " 0 R ");
            }

            WriteAscii(output, "] >>");
        });
    }

    internal static void WriteStructureObjectReferences(Stream output, PdfDocument document, PdfAccessibilityNode node, Dictionary<PdfPage, int> pageObjectIdMap, Dictionary<PdfInternalLink, int> internalLinkObjectIds, Dictionary<PdfExternalLink, int> externalLinkObjectIds, Dictionary<PdfAnnotation, int> annotationObjectIds, Dictionary<PdfAcroFormField, int> fieldObjectIds)
    {
        foreach (PdfInternalLink link in node.Page.InternalLinks)
        {
            if (link.TagNode == node)
            {
                WriteObjectReference(output, internalLinkObjectIds[link], pageObjectIdMap[node.Page]);
            }
        }

        foreach (PdfExternalLink link in node.Page.ExternalLinks)
        {
            if (link.TagNode == node)
            {
                WriteObjectReference(output, externalLinkObjectIds[link], pageObjectIdMap[node.Page]);
            }
        }

        foreach (PdfAnnotation annotation in node.Page.Annotations)
        {
            if (annotation.TagNode == node)
            {
                WriteObjectReference(output, annotationObjectIds[annotation], pageObjectIdMap[node.Page]);
            }
        }

        foreach (PdfAcroFormField field in document.AcroForm.Fields)
        {
            if (field.TagNode == node)
            {
                WriteObjectReference(output, fieldObjectIds[field], pageObjectIdMap[node.Page]);
            }
        }

        foreach (PdfRadioButtonGroup group in document.AcroForm.RadioButtonGroups)
        {
            foreach (PdfAcroFormField option in group.Options)
            {
                if (option.TagNode == node)
                {
                    WriteObjectReference(output, fieldObjectIds[option], pageObjectIdMap[node.Page]);
                }
            }
        }
    }

    internal static void WriteObjectReference(Stream output, int objectId, int pageObjectId)
    {
        WriteAscii(output, "<< /Type /OBJR /Obj " + objectId.ToString(CultureInfo.InvariantCulture) + " 0 R /Pg " + pageObjectId.ToString(CultureInfo.InvariantCulture) + " 0 R >> ");
    }

    internal static string GetTableHeaderScopeName(PdfTableHeaderScope scope)
    {
        if (scope == PdfTableHeaderScope.Row)
        {
            return "Row";
        }

        if (scope == PdfTableHeaderScope.Column)
        {
            return "Column";
        }

        return "Both";
    }

    internal static void ValidateFormFields(PdfDocument document)
    {
        if (!document.HasAcroForm)
        {
            return;
        }

        foreach (PdfAcroFormField field in document.AcroForm.Fields)
        {
            if (!HasPage(document, field.Page))
            {
                throw new InvalidOperationException("Form field page does not belong to the document.");
            }

            if (field.FieldType == "Sig" && !field.HasSignaturePlaceholder)
            {
                throw new InvalidOperationException("Signature fields must configure a placeholder before saving.");
            }
        }

        foreach (PdfRadioButtonGroup group in document.AcroForm.RadioButtonGroups)
        {
            if (group.Options.Count == 0)
            {
                throw new InvalidOperationException("Radio-button groups must contain at least one option before saving.");
            }

            bool foundSelectedValue = group.SelectedValue.Length == 0;
            foreach (PdfAcroFormField option in group.Options)
            {
                if (!HasPage(document, option.Page))
                {
                    throw new InvalidOperationException("Radio-button option page does not belong to the document.");
                }

                if (option.ExportValue == group.SelectedValue && option.Checked)
                {
                    foundSelectedValue = true;
                }
                else if (option.Checked)
                {
                    throw new InvalidOperationException("Radio-button checked state does not match its group value.");
                }
            }

            if (!foundSelectedValue)
            {
                throw new InvalidOperationException("Radio-button selected value is not registered in its group.");
            }
        }
    }

    internal static bool HasPage(PdfDocument document, PdfPage page)
    {
        foreach (PdfPage existingPage in document.Pages)
        {
            if (existingPage == page)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool HasNamedDestination(PdfDocument document, string name)
    {
        foreach (PdfNamedDestination destination in document.NamedDestinations)
        {
            if (destination.Name == name)
            {
                return true;
            }
        }

        return false;
    }

    internal static List<PdfAttachment> SortAttachmentsByFileName(IReadOnlyList<PdfAttachment> attachments)
    {
        List<PdfAttachment> sorted = new List<PdfAttachment>();
        foreach (PdfAttachment attachment in attachments)
        {
            int insertIndex = 0;
            while (insertIndex < sorted.Count && string.CompareOrdinal(sorted[insertIndex].FileName, attachment.FileName) <= 0)
            {
                insertIndex++;
            }

            sorted.Insert(insertIndex, attachment);
        }

        return sorted;
    }

    internal static void AddUsedFonts(List<PdfFont> fonts, PdfFontRegistry fontRegistry, PdfCanvas canvas)
    {
        foreach (PdfFont font in canvas.Fonts)
        {
            AddUsedFont(fonts, fontRegistry, font);
        }
    }

    internal static void AddUsedGraphicsStates(List<PdfGraphicsState> graphicsStates, PdfCanvas canvas)
    {
        foreach (PdfGraphicsState sourceState in canvas.GraphicsStates)
        {
            bool found = false;
            foreach (PdfGraphicsState graphicsState in graphicsStates)
            {
                if (graphicsState.Name == sourceState.Name)
                {
                    found = true;
                }
            }

            if (!found)
            {
                graphicsStates.Add(sourceState);
            }
        }
    }

    internal static void AddUsedTrueTypeFonts(List<PdfTrueTypeFontUsage> usages, PdfCanvas canvas)
    {
        foreach (PdfTrueTypeFontUsage sourceUsage in canvas.TrueTypeFontUsages)
        {
            PdfTrueTypeFontUsage? existingUsage = null;
            foreach (PdfTrueTypeFontUsage usage in usages)
            {
                if (usage.Font.Identity == sourceUsage.Font.Identity)
                {
                    existingUsage = usage;
                }
            }

            if (existingUsage == null)
            {
                existingUsage = new PdfTrueTypeFontUsage(sourceUsage.Font);
                usages.Add(existingUsage);
            }

            existingUsage.Merge(sourceUsage);
        }
    }

    internal static void AddUsedFont(List<PdfFont> fonts, PdfFontRegistry fontRegistry, PdfFont font)
    {
        string resourceName = fontRegistry.GetResourceName(font);

        foreach (PdfFont existingFont in fonts)
        {
            if (fontRegistry.GetResourceName(existingFont) == resourceName)
            {
                return;
            }
        }

        fonts.Add(font);
    }

    internal static void WriteStreamObject(Stream output, long[] offsets, int objectId, byte[] contentBytes, bool flateDecode)
    {
        WriteObject(output, offsets, objectId, () =>
        {
            WriteAscii(output, "<< /Length " + contentBytes.Length.ToString(CultureInfo.InvariantCulture));
            if (flateDecode)
            {
                WriteAscii(output, " /Filter /FlateDecode");
            }

            WriteAscii(output, " >>\nstream\n");
            output.Write(contentBytes);
            WriteAscii(output, "endstream");
        });
    }

    internal static void WriteObject(Stream output, long[] offsets, int objectId, Action writeBody)
    {
        offsets[objectId] = output.Position;
        WriteAscii(output, objectId.ToString(CultureInfo.InvariantCulture) + " 0 obj\n");
        writeBody();
        WriteAscii(output, "\nendobj\n");
    }

    internal static void WriteAscii(Stream output, string value)
    {
        output.Write(PdfEncoding.GetBytes(value));
    }

    internal static byte[] CompressBytes(byte[] bytes)
    {
        using (MemoryStream output = new MemoryStream())
        {
            using (ZLibStream compressed = new ZLibStream(output, CompressionLevel.Optimal, true))
            {
                compressed.Write(bytes);
            }

            return output.ToArray();
        }
    }

    internal static string Number(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    internal static string GetTrailerDocumentId(PdfDocument document)
    {
        if (document.WriterOptions.Deterministic)
        {
            return document.WriterOptions.DeterministicDocumentId;
        }

        return Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
    }

    internal static string EncodeName(string value)
    {
        StringBuilder builder = new StringBuilder();
        byte[] bytes = Encoding.UTF8.GetBytes(value);

        foreach (byte item in bytes)
        {
            if (IsRegularNameByte(item))
            {
                builder.Append((char)item);
            }
            else
            {
                builder.Append("#");
                builder.Append(item.ToString("X2", CultureInfo.InvariantCulture));
            }
        }

        return builder.ToString();
    }

    internal static bool IsRegularNameByte(byte value)
    {
        if (value <= 32 || value >= 127)
        {
            return false;
        }

        if (value == 35 || value == 37 || value == 40 || value == 41 || value == 47)
        {
            return false;
        }

        if (value == 60 || value == 62 || value == 91 || value == 93 || value == 123 || value == 125)
        {
            return false;
        }

        return true;
    }
}
