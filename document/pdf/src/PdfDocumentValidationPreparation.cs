namespace pdf;

internal static class PdfDocumentValidationPreparation
{
    internal static bool CheckSaveability(PdfDocument document, PdfValidationResult result)
    {
        bool saveable = true;
        if (document.Pages.Count == 0)
        {
            result.AddError("PDF_DOCUMENT_NO_PAGES", "The document must contain at least one page before its emitted structure can be validated.", 0);
            saveable = false;
        }

        if (document.HasTaggedPdf)
        {
            if (document.Language.Length == 0)
            {
                result.AddError("PDFUA_LANGUAGE_MISSING", "Tagged PDF output requires a document language.", 0);
                saveable = false;
            }

            if (document.StructureRoots.Count == 0)
            {
                result.AddError("PDFUA_STRUCTURE_TREE_MISSING", "Tagged PDF output requires at least one root structure element.", 0);
                saveable = false;
            }

            foreach (PdfAccessibilityNode node in document.AccessibilityNodes)
            {
                if (!PdfDocument.IsStandardStructureRole(node.Role))
                {
                    result.AddError("PDFUA_STRUCTURE_ROLE_INVALID", "A tagged structure element uses a non-standard role.", 0);
                    saveable = false;
                }
            }

            foreach (PdfPage page in document.Pages)
            {
                if (page.Canvas.MarkedContentDepth != 0)
                {
                    result.AddError("PDFUA_MARKED_CONTENT_UNCLOSED", "Every tagged marked-content span must be closed before validation.", 0);
                    saveable = false;
                    break;
                }

                foreach (PdfAccessibilityNode node in page.Canvas.MarkedContentNodes)
                {
                    if (node.OwnerDocument != document || node.Page != page)
                    {
                        result.AddError("PDFUA_MARKED_CONTENT_OWNER_INVALID", "Tagged marked content refers to a structure element on another document or page.", 0);
                        saveable = false;
                    }
                }
            }
        }

        foreach (PdfPage page in document.Pages)
        {
            foreach (PdfInternalLink link in page.InternalLinks)
            {
                if (!PdfWriter.HasNamedDestination(document, link.DestinationName))
                {
                    result.AddError("PDF_LINK_DESTINATION_MISSING", "An internal link refers to an undefined named destination.", 0);
                    saveable = false;
                }
            }
        }

        foreach (PdfAcroFormField field in document.AcroForm.Fields)
        {
            if (field.FieldType == "Sig" && !field.HasSignaturePlaceholder)
            {
                result.AddError("PDF_SIGNATURE_PLACEHOLDER_MISSING", "A signature field has no configured signature placeholder.", 0);
                saveable = false;
            }
        }

        return saveable;
    }
}
