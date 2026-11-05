using System;
using System.Text;

namespace pdf;

/// <summary>
/// Validates document preparation and emitted structures for PDF/A and PDF/UA workflows.
/// </summary>
public class PdfConformanceValidator : IPdfConformanceValidator
{
    /// <summary>Creates a PDF conformance validator.</summary>
    public PdfConformanceValidator()
    {
    }

    /// <summary>Validates PDF/A preparation and emitted archival structures.</summary>
    /// <param name="document">The document to validate.</param>
    /// <returns>The ordered validation findings.</returns>
    public PdfValidationResult ValidatePdfA(PdfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        PdfValidationResult result = new PdfValidationResult();
        bool saveable = PdfDocumentValidationPreparation.CheckSaveability(document, result);
        if (!document.HasPdfA)
        {
            result.AddError("PDFA_NOT_PREPARED", "The document has not been prepared for a supported PDF/A profile.", 0);
            return result;
        }

        if (!IsPdfAProfileValid(document.PdfAOptions.Conformance))
        {
            result.AddError("PDFA_PROFILE_INVALID", "The configured PDF/A profile is not supported.", 0);
            saveable = false;
        }

        if (document.HasEncryption)
        {
            result.AddError("PDFA_ENCRYPTION_NOT_ALLOWED", "PDF/A output cannot be encrypted.", 0);
            saveable = false;
        }

        PdfVersion expectedVersion = PdfVersion.Pdf17;
        if (document.PdfAOptions.Conformance == PdfAConformance.PdfA1B)
        {
            expectedVersion = PdfVersion.Pdf14;
        }

        if (document.WriterOptions.Version != expectedVersion)
        {
            result.AddError("PDFA_VERSION_MISMATCH", "The selected PDF version does not match the prepared PDF/A profile.", 0);
            saveable = false;
        }

        int iccComponents = 0;
        if (document.PdfAOptions.IccProfileData == null || document.PdfAOptions.IccProfileData.Length < 128)
        {
            result.AddError("PDFA_OUTPUT_INTENT_INVALID", "PDF/A output requires a complete ICC output-intent profile.", 0);
            saveable = false;
        }
        else
        {
            iccComponents = GetIccComponentCount(document.PdfAOptions.IccProfileData);
            if (iccComponents == 0)
            {
                result.AddError("PDFA_OUTPUT_INTENT_INVALID", "The PDF/A ICC output-intent profile header is invalid or unsupported.", 0);
                saveable = false;
            }
        }

        if (iccComponents > 0 && !HasCompatibleColorUsage(document, iccComponents))
        {
            result.AddError("PDFA_COLOR_SPACE_MISMATCH", "Document device colors do not match the ICC output-intent color space.", 0);
            saveable = false;
        }

        if (PdfAConfiguration.HasStandardFontUsage(document))
        {
            result.AddError("PDFA_FONT_NOT_EMBEDDED", "PDF/A output cannot use the standard Base-14 fonts.", 0);
            saveable = false;
        }

        if (document.HasAcroForm)
        {
            result.AddError("PDFA_INTERACTIVE_FORM_NOT_ALLOWED", "Interactive forms must be flattened before PDF/A validation.", 0);
            saveable = false;
        }

        if (PdfAConfiguration.HasUnsupportedAnnotations(document))
        {
            result.AddError("PDFA_ANNOTATION_APPEARANCE_MISSING", "Annotations without archival appearance streams are not supported.", 0);
            saveable = false;
        }

        if (document.PdfAOptions.Conformance != PdfAConformance.PdfA3B && document.Attachments.Count > 0)
        {
            result.AddError("PDFA_ATTACHMENT_NOT_ALLOWED", "The selected PDF/A profile does not permit embedded files.", 0);
            saveable = false;
        }

        if (document.PdfAOptions.Conformance == PdfAConformance.PdfA1B && PdfAConfiguration.HasTransparency(document))
        {
            result.AddError("PDFA_TRANSPARENCY_NOT_ALLOWED", "PDF/A-1b does not permit transparency.", 0);
            saveable = false;
        }

        if (document.PdfAOptions.Conformance == PdfAConformance.PdfA3B)
        {
            foreach (PdfAttachment attachment in document.Attachments)
            {
                if (!attachment.IsAssociatedFile || attachment.MimeType.Length == 0)
                {
                    result.AddError("PDFA_ASSOCIATED_FILE_INVALID", "PDF/A-3b attachments must be associated files with MIME types.", 0);
                    saveable = false;
                }
            }
        }

        if (!saveable)
        {
            return result;
        }

        byte[] pdfData = document.ToBytes();
        PdfValidationResult structure = new PdfStructureValidator().ValidateBytes(pdfData);
        PdfStructureValidator.CopyIssues(structure, result);
        string text = Encoding.ASCII.GetString(pdfData);
        if (text.IndexOf("/Metadata ", StringComparison.Ordinal) < 0 || text.IndexOf("pdfaid:conformance=\"B\"", StringComparison.Ordinal) < 0)
        {
            result.AddError("PDFA_IDENTIFICATION_MISSING", "The emitted PDF does not contain PDF/A identification metadata.", 0);
        }

        if (text.IndexOf("/OutputIntents [", StringComparison.Ordinal) < 0 || text.IndexOf("/S /GTS_PDFA1", StringComparison.Ordinal) < 0)
        {
            result.AddError("PDFA_OUTPUT_INTENT_MISSING", "The emitted PDF does not contain a PDF/A output intent.", 0);
        }

        if (text.IndexOf("/Encrypt ", StringComparison.Ordinal) >= 0)
        {
            result.AddError("PDFA_ENCRYPTION_DICTIONARY_PRESENT", "The emitted PDF/A trailer contains an encryption dictionary.", 0);
        }

        return result;
    }

    /// <summary>Validates PDF/UA preparation and emitted tagged structures.</summary>
    /// <param name="document">The document to validate.</param>
    /// <returns>The ordered validation findings.</returns>
    public PdfValidationResult ValidatePdfUa(PdfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        PdfValidationResult result = new PdfValidationResult();
        bool saveable = PdfDocumentValidationPreparation.CheckSaveability(document, result);
        if (!document.HasTaggedPdf)
        {
            result.AddError("PDFUA_TAGGED_OUTPUT_DISABLED", "PDF/UA preparation requires tagged PDF output.", 0);
            return result;
        }

        if (string.IsNullOrEmpty(document.Metadata.Title))
        {
            result.AddError("PDFUA_TITLE_MISSING", "PDF/UA preparation requires a document title.", 0);
        }

        if (document.StructureRoots.Count != 1 || document.StructureRoots[0].Role != "Document")
        {
            result.AddError("PDFUA_DOCUMENT_ROOT_INVALID", "PDF/UA preparation requires exactly one Document structure root.", 0);
        }

        CheckTaggedImages(document, result);
        CheckTaggedLinksAndAnnotations(document, result);
        CheckTaggedForms(document, result);
        CheckTaggedFigures(document, result);
        if (!HasDisplayDocumentTitle(document))
        {
            result.AddWarning("PDFUA_DISPLAY_TITLE_RECOMMENDED", "PDF/UA viewers should be instructed to display the document title.", 0);
        }

        if (!saveable)
        {
            return result;
        }

        byte[] pdfData = document.ToBytes();
        PdfValidationResult structure = new PdfStructureValidator().ValidateBytes(pdfData);
        PdfStructureValidator.CopyIssues(structure, result);
        string text = Encoding.ASCII.GetString(pdfData);
        if (text.IndexOf("/MarkInfo << /Marked true >>", StringComparison.Ordinal) < 0)
        {
            result.AddError("PDFUA_MARK_INFO_MISSING", "The emitted catalog does not declare marked content.", 0);
        }

        if (text.IndexOf("/StructTreeRoot ", StringComparison.Ordinal) < 0 || text.IndexOf("/ParentTree ", StringComparison.Ordinal) < 0)
        {
            result.AddError("PDFUA_STRUCTURE_OUTPUT_MISSING", "The emitted PDF does not contain a complete structure tree and parent tree.", 0);
        }

        if (text.IndexOf("/Lang ", StringComparison.Ordinal) < 0)
        {
            result.AddError("PDFUA_LANGUAGE_OUTPUT_MISSING", "The emitted catalog does not contain a document language.", 0);
        }

        return result;
    }

    internal static bool IsPdfAProfileValid(PdfAConformance conformance)
    {
        return conformance == PdfAConformance.PdfA1B || conformance == PdfAConformance.PdfA2B || conformance == PdfAConformance.PdfA3B;
    }

    internal static void CheckTaggedImages(PdfDocument document, PdfValidationResult result)
    {
        foreach (PdfImage image in document.Images)
        {
            if (!image.IsDecorative && image.AlternateText.Length == 0)
            {
                result.AddError("PDFUA_IMAGE_ALT_TEXT_MISSING", "Every informative image requires alternate text.", 0);
            }
        }
    }

    internal static void CheckTaggedLinksAndAnnotations(PdfDocument document, PdfValidationResult result)
    {
        foreach (PdfPage page in document.Pages)
        {
            foreach (PdfInternalLink link in page.InternalLinks)
            {
                if (link.TagNode == null)
                {
                    result.AddError("PDFUA_LINK_NOT_TAGGED", "Every internal link annotation must be associated with a structure element.", 0);
                }
            }

            foreach (PdfExternalLink link in page.ExternalLinks)
            {
                if (link.TagNode == null)
                {
                    result.AddError("PDFUA_LINK_NOT_TAGGED", "Every external link annotation must be associated with a structure element.", 0);
                }
            }

            foreach (PdfAnnotation annotation in page.Annotations)
            {
                if (annotation.TagNode == null)
                {
                    result.AddError("PDFUA_ANNOTATION_NOT_TAGGED", "Every annotation must be associated with a structure element.", 0);
                }
            }
        }
    }

    internal static void CheckTaggedForms(PdfDocument document, PdfValidationResult result)
    {
        foreach (PdfAcroFormField field in document.AcroForm.Fields)
        {
            if (field.AccessibilityName.Length == 0)
            {
                result.AddError("PDFUA_FORM_NAME_MISSING", "Every form field requires an accessibility name.", 0);
            }

            if (field.TagNode == null)
            {
                result.AddError("PDFUA_FORM_NOT_TAGGED", "Every form widget must be associated with a structure element.", 0);
            }
        }

        foreach (PdfRadioButtonGroup group in document.AcroForm.RadioButtonGroups)
        {
            if (group.AccessibilityName.Length == 0)
            {
                result.AddError("PDFUA_FORM_NAME_MISSING", "Every radio-button group requires an accessibility name.", 0);
            }

            foreach (PdfAcroFormField option in group.Options)
            {
                if (option.TagNode == null)
                {
                    result.AddError("PDFUA_FORM_NOT_TAGGED", "Every radio-button widget must be associated with a structure element.", 0);
                }
            }
        }
    }

    internal static void CheckTaggedFigures(PdfDocument document, PdfValidationResult result)
    {
        foreach (PdfAccessibilityNode node in document.AccessibilityNodes)
        {
            if (node.Role == "Figure" && node.AlternateText.Length == 0 && node.ActualText.Length == 0)
            {
                result.AddError("PDFUA_FIGURE_ALT_TEXT_MISSING", "Every Figure structure element requires alternate or actual text.", 0);
            }
        }
    }

    internal static bool HasDisplayDocumentTitle(PdfDocument document)
    {
        return document.HasViewerPreferences && document.ViewerPreferences.DisplayDocumentTitle;
    }

    internal static int GetIccComponentCount(byte[] profile)
    {
        if (profile.Length < 128 || profile[36] != 97 || profile[37] != 99 || profile[38] != 115 || profile[39] != 112)
        {
            return 0;
        }

        if (profile[16] == 71 && profile[17] == 82 && profile[18] == 65 && profile[19] == 89)
        {
            return 1;
        }

        if (profile[16] == 82 && profile[17] == 71 && profile[18] == 66 && profile[19] == 32)
        {
            return 3;
        }

        if (profile[16] == 67 && profile[17] == 77 && profile[18] == 89 && profile[19] == 75)
        {
            return 4;
        }

        return 0;
    }

    internal static bool HasCompatibleColorUsage(PdfDocument document, int componentCount)
    {
        foreach (PdfImage image in document.Images)
        {
            if (image.ColorSpace == PdfImageColorSpace.Rgb && componentCount != 3)
            {
                return false;
            }

            if (image.ColorSpace == PdfImageColorSpace.Cmyk && componentCount != 4)
            {
                return false;
            }
        }

        foreach (PdfPage page in document.Pages)
        {
            if (!HasCompatibleCanvasColor(page.Canvas, componentCount))
            {
                return false;
            }
        }

        foreach (PdfFormXObject form in document.Forms)
        {
            if (!HasCompatibleCanvasColor(form.Canvas, componentCount))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool HasCompatibleCanvasColor(PdfCanvas canvas, int componentCount)
    {
        if (canvas.UsesDeviceRgb && componentCount != 3)
        {
            return false;
        }

        if (canvas.UsesDeviceCmyk && componentCount != 4)
        {
            return false;
        }

        return true;
    }
}
