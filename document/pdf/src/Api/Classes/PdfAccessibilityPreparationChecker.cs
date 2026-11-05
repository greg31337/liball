using System;

namespace pdf;

/// <summary>
/// Checks whether a document contains the metadata and reading-order information needed before accessibility work.
/// </summary>
public class PdfAccessibilityPreparationChecker : IPdfAccessibilityPreparationChecker
{
    /// <summary>
    /// Creates an accessibility preparation checker.
    /// </summary>
    public PdfAccessibilityPreparationChecker()
    {
    }

    /// <summary>
    /// Checks language metadata, reading-order nodes, descriptions, links, annotations, and form fields for accessibility preparation concerns.
    /// </summary>
    /// <param name="document">The PDF document to inspect, modify, or render.</param>
    /// <returns>The resulting accessibility preparation result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfAccessibilityPreparationResult Check(PdfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        PdfAccessibilityPreparationResult result = new PdfAccessibilityPreparationResult();

        CheckDocumentShape(document, result);
        CheckLanguage(document, result);
        CheckTitle(document, result);
        CheckReadingOrder(document, result);
        CheckImages(document, result);
        CheckForms(document, result);
        CheckLinks(document, result);
        CheckAnnotations(document, result);

        return result;
    }

    internal static void CheckDocumentShape(PdfDocument document, PdfAccessibilityPreparationResult result)
    {
        if (document.Pages.Count == 0)
        {
            result.AddError("ACCESSIBILITY_NO_PAGES", "Accessibility preparation requires at least one page.");
        }
    }

    internal static void CheckLanguage(PdfDocument document, PdfAccessibilityPreparationResult result)
    {
        if (string.IsNullOrEmpty(document.Language))
        {
            result.AddError("ACCESSIBILITY_LANGUAGE_MISSING", "Accessibility preparation requires a document language.");
        }
    }

    internal static void CheckTitle(PdfDocument document, PdfAccessibilityPreparationResult result)
    {
        if (document.Metadata == null || string.IsNullOrEmpty(document.Metadata.Title))
        {
            result.AddError("ACCESSIBILITY_TITLE_MISSING", "Accessibility preparation requires a document title.");
        }
    }

    internal static void CheckReadingOrder(PdfDocument document, PdfAccessibilityPreparationResult result)
    {
        if (document.AccessibilityNodes.Count == 0)
        {
            result.AddError("ACCESSIBILITY_READING_ORDER_MISSING", "Accessibility preparation requires logical reading-order nodes.");
        }
    }

    internal static void CheckImages(PdfDocument document, PdfAccessibilityPreparationResult result)
    {
        foreach (PdfImage image in document.Images)
        {
            if (!image.IsDecorative && image.AlternateText.Length == 0)
            {
                result.AddError("ACCESSIBILITY_IMAGE_ALT_TEXT_MISSING", "Non-decorative images require alternate text.");
                return;
            }
        }
    }

    internal static void CheckForms(PdfDocument document, PdfAccessibilityPreparationResult result)
    {
        if (!document.HasAcroForm)
        {
            return;
        }

        foreach (PdfAcroFormField field in document.AcroForm.Fields)
        {
            if (field.AccessibilityName.Length == 0)
            {
                result.AddError("ACCESSIBILITY_FORM_FIELD_DESCRIPTIONS_REQUIRED", "Interactive form fields require accessible names before tagging.");
                return;
            }
        }

        foreach (PdfRadioButtonGroup group in document.AcroForm.RadioButtonGroups)
        {
            if (group.AccessibilityName.Length == 0)
            {
                result.AddError("ACCESSIBILITY_FORM_FIELD_DESCRIPTIONS_REQUIRED", "Interactive form fields require accessible names before tagging.");
                return;
            }
        }
    }

    internal static void CheckLinks(PdfDocument document, PdfAccessibilityPreparationResult result)
    {
        foreach (PdfPage page in document.Pages)
        {
            if (page.InternalLinks.Count > 0 || page.ExternalLinks.Count > 0)
            {
                result.AddWarning("ACCESSIBILITY_LINK_REVIEW", "Links should be reviewed for meaningful text and tagging order.");
                return;
            }
        }
    }

    internal static void CheckAnnotations(PdfDocument document, PdfAccessibilityPreparationResult result)
    {
        foreach (PdfPage page in document.Pages)
        {
            if (page.Annotations.Count > 0)
            {
                result.AddWarning("ACCESSIBILITY_ANNOTATION_REVIEW", "Annotations should be reviewed for accessible contents and appearance.");
                return;
            }
        }
    }
}
