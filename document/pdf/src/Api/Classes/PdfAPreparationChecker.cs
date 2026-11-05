using System;

namespace pdf;

/// <summary>
/// Checks whether a document is prepared for later conversion to a PDF/A archival profile.
/// </summary>
public class PdfAPreparationChecker : IPdfAPreparationChecker
{
    /// <summary>
    /// Creates a PDF/A preparation checker.
    /// </summary>
    public PdfAPreparationChecker()
    {
    }

    /// <summary>
    /// Checks document structure, metadata, security, embedded content, fonts, forms, and annotations for known PDF/A preparation concerns.
    /// </summary>
    /// <param name="document">The PDF document to inspect, modify, or render.</param>
    /// <returns>A result containing every error and warning found by the preparation checks.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfAPreparationResult Check(PdfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        PdfAPreparationResult result = new PdfAPreparationResult();

        CheckDocumentShape(document, result);
        CheckMetadata(document, result);
        CheckSecurity(document, result);
        CheckEmbeddedContent(document, result);
        CheckFonts(document, result);
        CheckForms(document, result);
        CheckAnnotations(document, result);

        return result;
    }

    internal static void CheckDocumentShape(PdfDocument document, PdfAPreparationResult result)
    {
        if (document.Pages.Count == 0)
        {
            result.AddError("PDFA_NO_PAGES", "PDF/A preparation requires at least one page.");
        }
    }

    internal static void CheckMetadata(PdfDocument document, PdfAPreparationResult result)
    {
        if (document.Metadata == null)
        {
            result.AddWarning("PDFA_METADATA_MISSING", "PDF/A preparation should include document metadata before external validation.");
            return;
        }

        if (string.IsNullOrEmpty(document.Metadata.Title))
        {
            result.AddWarning("PDFA_METADATA_TITLE_MISSING", "PDF/A preparation should include a document title.");
        }

        if (string.IsNullOrEmpty(document.Metadata.Producer))
        {
            result.AddWarning("PDFA_METADATA_PRODUCER_MISSING", "PDF/A preparation should include a producer value.");
        }
    }

    internal static void CheckSecurity(PdfDocument document, PdfAPreparationResult result)
    {
        if (document.HasEncryption)
        {
            result.AddError("PDFA_ENCRYPTION_NOT_ALLOWED", "PDF/A preparation does not allow encrypted output.");
        }
    }

    internal static void CheckEmbeddedContent(PdfDocument document, PdfAPreparationResult result)
    {
        if (document.Attachments.Count > 0 && (!document.HasPdfA || document.PdfAOptions.Conformance != PdfAConformance.PdfA3B))
        {
            result.AddError("PDFA_ATTACHMENTS_NOT_ALLOWED", "The selected PDF/A preparation profile does not allow these embedded file attachments.");
        }

        if (document.Images.Count > 0 && !document.HasPdfA)
        {
            result.AddWarning("PDFA_IMAGE_COLOR_PROFILE_REVIEW", "Images should be reviewed with the document output intent during PDF/A validation.");
        }
    }

    internal static void CheckFonts(PdfDocument document, PdfAPreparationResult result)
    {
        if (HasUsedFonts(document))
        {
            result.AddError("PDFA_EMBED_FONTS", "PDF/A preparation requires all used fonts to be embedded.");
        }
    }

    internal static bool HasUsedFonts(PdfDocument document)
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

    internal static void CheckForms(PdfDocument document, PdfAPreparationResult result)
    {
        if (!document.HasAcroForm)
        {
            return;
        }

        if (document.AcroForm.Fields.Count > 0)
        {
            result.AddError("PDFA_FORM_APPEARANCES_REQUIRED", "PDF/A preparation requires interactive form fields to be flattened or supplied with appearance streams.");
        }
    }

    internal static void CheckAnnotations(PdfDocument document, PdfAPreparationResult result)
    {
        foreach (PdfPage page in document.Pages)
        {
            if (page.Annotations.Count > 0)
            {
                result.AddWarning("PDFA_ANNOTATION_REVIEW", "Annotations should be reviewed for PDF/A appearance and content requirements.");
                return;
            }
        }
    }
}
