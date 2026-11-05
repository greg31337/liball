using System;

namespace pdf;

internal static class PdfTaggedDocumentConfiguration
{
    internal static void Validate(PdfDocument document)
    {
        if (!document.HasTaggedPdf)
        {
            return;
        }

        if (document.Language.Length == 0)
        {
            throw new InvalidOperationException("Tagged PDF output requires a document language.");
        }

        if (document.StructureRoots.Count == 0)
        {
            throw new InvalidOperationException("Tagged PDF output requires at least one root structure element.");
        }

        foreach (PdfAccessibilityNode node in document.AccessibilityNodes)
        {
            PdfDocument.ValidateStructureRole(node.Role);
        }

        foreach (PdfPage page in document.Pages)
        {
            if (page.Canvas.MarkedContentDepth != 0)
            {
                throw new InvalidOperationException("Every marked-content span must be closed before saving.");
            }

            foreach (PdfAccessibilityNode node in page.Canvas.MarkedContentNodes)
            {
                if (node.OwnerDocument != document || node.Page != page)
                {
                    throw new InvalidOperationException("Marked content must reference a structure element on the same document page.");
                }
            }
        }
    }
}
