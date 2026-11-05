using System;
using System.Collections.Generic;
using System.Text;

namespace pdf;

/// <summary>
/// Validates emitted PDF syntax, classic cross references, object reachability, page trees, forms, and signature placeholders.
/// </summary>
public class PdfStructureValidator : IPdfStructureValidator
{
    /// <summary>Creates a PDF structure validator.</summary>
    public PdfStructureValidator()
    {
    }

    /// <summary>Validates a complete PDF byte array.</summary>
    /// <param name="pdfData">The complete PDF bytes.</param>
    /// <returns>The ordered validation findings.</returns>
    public PdfValidationResult ValidateBytes(byte[] pdfData)
    {
        if (pdfData == null)
        {
            throw new ArgumentNullException("pdfData");
        }

        PdfValidationResult result = new PdfValidationResult();
        string text = Encoding.ASCII.GetString(pdfData);
        if (!text.StartsWith("%PDF-", StringComparison.Ordinal))
        {
            result.AddError("PDF_SYNTAX_HEADER_INVALID", "The byte stream does not begin with a PDF header.", 0);
            return result;
        }

        if (!text.EndsWith("%%EOF\n", StringComparison.Ordinal))
        {
            result.AddError("PDF_SYNTAX_EOF_MISSING", "The byte stream does not end with a PDF end-of-file marker.", 0);
        }

        PdfParsedDocument parsed = ParseCrossReference(text, result);
        if (parsed.ObjectCount == 0)
        {
            return result;
        }

        ValidateIndirectObjects(parsed, result);
        ValidateReferencedObjects(parsed, result);
        ValidatePageTree(parsed, result);
        ValidateForms(parsed, result);
        return result;
    }

    /// <summary>Serializes and validates a document when its model is saveable.</summary>
    /// <param name="document">The document to validate.</param>
    /// <returns>The ordered validation findings.</returns>
    public PdfValidationResult ValidateDocument(PdfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        PdfValidationResult result = new PdfValidationResult();
        if (!PdfDocumentValidationPreparation.CheckSaveability(document, result))
        {
            return result;
        }

        PdfValidationResult emitted = ValidateBytes(document.ToBytes());
        CopyIssues(emitted, result);
        return result;
    }

    internal static PdfParsedDocument ParseCrossReference(string text, PdfValidationResult result)
    {
        PdfParsedDocument parsed = new PdfParsedDocument(text);
        int marker = text.LastIndexOf("startxref\n", StringComparison.Ordinal);
        if (marker < 0)
        {
            result.AddError("PDF_XREF_START_MISSING", "The startxref marker is missing.", 0);
            return parsed;
        }

        int numberStart = marker + 10;
        int numberEnd = text.IndexOf('\n', numberStart);
        int xrefOffset = ParseInteger(text, numberStart, numberEnd);
        if (xrefOffset < 0 || xrefOffset >= text.Length || !HasTextAt(text, xrefOffset, "xref\n"))
        {
            result.AddError("PDF_XREF_OFFSET_INVALID", "The startxref value does not point to a classic cross-reference table.", 0);
            return parsed;
        }

        parsed.XrefOffset = xrefOffset;
        int position = xrefOffset + 5;
        int subsectionEnd = text.IndexOf('\n', position);
        if (subsectionEnd < 0)
        {
            result.AddError("PDF_XREF_SUBSECTION_INVALID", "The cross-reference subsection header is incomplete.", 0);
            return parsed;
        }

        string subsection = text.Substring(position, subsectionEnd - position);
        int separator = subsection.IndexOf(' ');
        int firstObject = ParseInteger(subsection, 0, separator);
        int objectCount = ParseInteger(subsection, separator + 1, subsection.Length);
        if (firstObject != 0 || objectCount < 2)
        {
            result.AddError("PDF_XREF_SUBSECTION_INVALID", "The emitted cross-reference table must describe objects starting at zero.", 0);
            return parsed;
        }

        parsed.ObjectCount = objectCount;
        parsed.Offsets = new int[objectCount];
        parsed.Bodies = new string[objectCount];
        parsed.References = new List<int>[objectCount];
        position = subsectionEnd + 1;
        for (int objectNumber = 0; objectNumber < objectCount; objectNumber++)
        {
            int entryEnd = text.IndexOf('\n', position);
            if (entryEnd < 0 || entryEnd - position < 18)
            {
                result.AddError("PDF_XREF_ENTRY_INVALID", "A cross-reference entry is missing or malformed.", objectNumber);
                parsed.ObjectCount = 0;
                return parsed;
            }

            int offset = ParseInteger(text, position, position + 10);
            char state = text[position + 17];
            if (objectNumber == 0)
            {
                if (state != 'f')
                {
                    result.AddError("PDF_XREF_FREE_ENTRY_INVALID", "Cross-reference object zero must be free.", 0);
                }
            }
            else
            {
                if (state != 'n' || offset <= 0 || offset >= xrefOffset)
                {
                    result.AddError("PDF_XREF_ENTRY_INVALID", "An in-use cross-reference entry has an invalid offset or state.", objectNumber);
                }

                parsed.Offsets[objectNumber] = offset;
            }

            position = entryEnd + 1;
        }

        if (!HasTextAt(text, position, "trailer\n"))
        {
            result.AddError("PDF_TRAILER_MISSING", "The cross-reference table is not followed by a trailer.", 0);
            parsed.ObjectCount = 0;
            return parsed;
        }

        int trailerStart = position + 8;
        int trailerEnd = text.IndexOf("\nstartxref\n", trailerStart, StringComparison.Ordinal);
        if (trailerEnd < 0)
        {
            result.AddError("PDF_TRAILER_INVALID", "The trailer dictionary is incomplete.", 0);
            parsed.ObjectCount = 0;
            return parsed;
        }

        string trailer = text.Substring(trailerStart, trailerEnd - trailerStart);
        int declaredSize = FindIntegerAfterKey(trailer, "/Size");
        if (declaredSize != objectCount)
        {
            result.AddError("PDF_TRAILER_SIZE_MISMATCH", "The trailer Size value does not match the cross-reference table.", 0);
        }

        parsed.RootObject = FindReferenceAfterKey(trailer, "/Root");
        if (parsed.RootObject <= 0 || parsed.RootObject >= objectCount)
        {
            result.AddError("PDF_TRAILER_ROOT_INVALID", "The trailer does not contain a valid catalog reference.", 0);
        }

        return parsed;
    }

    internal static void ValidateIndirectObjects(PdfParsedDocument parsed, PdfValidationResult result)
    {
        for (int objectNumber = 1; objectNumber < parsed.ObjectCount; objectNumber++)
        {
            int offset = parsed.Offsets[objectNumber];
            string declaration = objectNumber.ToString() + " 0 obj\n";
            if (!HasTextAt(parsed.Text, offset, declaration))
            {
                result.AddError("PDF_XREF_OBJECT_MISMATCH", "A cross-reference offset does not point to its declared indirect object.", objectNumber);
                parsed.Bodies[objectNumber] = string.Empty;
                parsed.References[objectNumber] = new List<int>();
                continue;
            }

            int boundary = FindPhysicalObjectBoundary(parsed, offset);
            int bodyStart = offset + declaration.Length;
            int endObject = parsed.Text.LastIndexOf("\nendobj\n", boundary - 1, boundary - bodyStart, StringComparison.Ordinal);
            if (endObject < bodyStart)
            {
                result.AddError("PDF_SYNTAX_OBJECT_END_MISSING", "An indirect object is not terminated by endobj.", objectNumber);
                parsed.Bodies[objectNumber] = string.Empty;
                parsed.References[objectNumber] = new List<int>();
                continue;
            }

            string body = parsed.Text.Substring(bodyStart, endObject - bodyStart);
            if (!body.StartsWith("<<", StringComparison.Ordinal))
            {
                result.AddError("PDF_SYNTAX_OBJECT_BODY_INVALID", "An emitted indirect object does not begin with a dictionary.", objectNumber);
            }

            parsed.Bodies[objectNumber] = body;
            parsed.References[objectNumber] = CollectReferences(GetDictionaryText(body));
        }
    }

    internal static void ValidateReferencedObjects(PdfParsedDocument parsed, PdfValidationResult result)
    {
        bool[] reachable = new bool[parsed.ObjectCount];
        List<int> pending = new List<int>();
        if (parsed.RootObject > 0 && parsed.RootObject < parsed.ObjectCount)
        {
            pending.Add(parsed.RootObject);
        }

        int trailer = parsed.Text.IndexOf("trailer\n", parsed.XrefOffset, StringComparison.Ordinal);
        int startxref = parsed.Text.IndexOf("\nstartxref\n", trailer, StringComparison.Ordinal);
        if (trailer >= 0 && startxref > trailer)
        {
            string trailerText = parsed.Text.Substring(trailer, startxref - trailer);
            int info = FindReferenceAfterKey(trailerText, "/Info");
            int encryption = FindReferenceAfterKey(trailerText, "/Encrypt");
            AddPendingReference(pending, info, parsed.ObjectCount);
            AddPendingReference(pending, encryption, parsed.ObjectCount);
        }

        int pendingIndex = 0;
        while (pendingIndex < pending.Count)
        {
            int objectNumber = pending[pendingIndex];
            pendingIndex++;
            if (!reachable[objectNumber])
            {
                reachable[objectNumber] = true;
                foreach (int reference in parsed.References[objectNumber])
                {
                    if (reference <= 0 || reference >= parsed.ObjectCount || parsed.Offsets[reference] == 0)
                    {
                        result.AddError("PDF_REFERENCE_MISSING", "An indirect reference points to an object absent from the cross-reference table.", objectNumber);
                    }
                    else if (!reachable[reference])
                    {
                        pending.Add(reference);
                    }
                }
            }
        }

        for (int objectNumber = 1; objectNumber < parsed.ObjectCount; objectNumber++)
        {
            if (parsed.Offsets[objectNumber] > 0 && !reachable[objectNumber])
            {
                result.AddError("PDF_RESOURCE_UNREACHABLE", "An in-use indirect object is not reachable from the trailer root, Info, or Encrypt entry.", objectNumber);
            }
        }
    }

    internal static void ValidatePageTree(PdfParsedDocument parsed, PdfValidationResult result)
    {
        if (parsed.RootObject <= 0 || parsed.RootObject >= parsed.ObjectCount)
        {
            return;
        }

        string catalog = parsed.Bodies[parsed.RootObject];
        if (catalog.IndexOf("/Type /Catalog", StringComparison.Ordinal) < 0)
        {
            result.AddError("PDF_CATALOG_TYPE_INVALID", "The trailer root does not reference a catalog dictionary.", parsed.RootObject);
            return;
        }

        int pagesObject = FindReferenceAfterKey(catalog, "/Pages");
        if (!IsObjectNumberValid(parsed, pagesObject))
        {
            result.AddError("PDF_PAGE_TREE_ROOT_MISSING", "The catalog does not reference a valid page-tree root.", parsed.RootObject);
            return;
        }

        string pages = parsed.Bodies[pagesObject];
        if (pages.IndexOf("/Type /Pages", StringComparison.Ordinal) < 0)
        {
            result.AddError("PDF_PAGE_TREE_TYPE_INVALID", "The page-tree root does not have type Pages.", pagesObject);
        }

        int declaredCount = FindIntegerAfterKey(pages, "/Count");
        List<int> kids = FindArrayReferences(pages, "/Kids");
        if (declaredCount != kids.Count)
        {
            result.AddError("PDF_PAGE_TREE_COUNT_MISMATCH", "The page-tree Count does not match its Kids array.", pagesObject);
        }

        foreach (int pageObject in kids)
        {
            if (!IsObjectNumberValid(parsed, pageObject))
            {
                result.AddError("PDF_PAGE_TREE_KID_MISSING", "A page-tree child reference is invalid.", pagesObject);
            }
            else
            {
                string page = parsed.Bodies[pageObject];
                if (page.IndexOf("/Type /Page ", StringComparison.Ordinal) < 0)
                {
                    result.AddError("PDF_PAGE_TYPE_INVALID", "A page-tree child does not have type Page.", pageObject);
                }

                if (FindReferenceAfterKey(page, "/Parent") != pagesObject)
                {
                    result.AddError("PDF_PAGE_PARENT_INVALID", "A page does not reference its owning page-tree node.", pageObject);
                }
            }
        }
    }

    internal static void ValidateForms(PdfParsedDocument parsed, PdfValidationResult result)
    {
        if (parsed.RootObject <= 0 || parsed.RootObject >= parsed.ObjectCount)
        {
            return;
        }

        string catalog = parsed.Bodies[parsed.RootObject];
        int formObject = FindReferenceAfterKey(catalog, "/AcroForm");
        if (formObject == 0)
        {
            return;
        }

        if (!IsObjectNumberValid(parsed, formObject))
        {
            result.AddError("PDF_FORM_ROOT_MISSING", "The catalog AcroForm reference is invalid.", parsed.RootObject);
            return;
        }

        List<int> fields = FindArrayReferences(parsed.Bodies[formObject], "/Fields");
        foreach (int fieldObject in fields)
        {
            if (!IsObjectNumberValid(parsed, fieldObject))
            {
                result.AddError("PDF_FORM_FIELD_MISSING", "The AcroForm Fields array contains an invalid reference.", formObject);
            }
            else
            {
                ValidateFormField(parsed, result, fieldObject);
            }
        }

        for (int objectNumber = 1; objectNumber < parsed.ObjectCount; objectNumber++)
        {
            string body = parsed.Bodies[objectNumber];
            if (body.Length > 0 && body.IndexOf("/Subtype /Widget", StringComparison.Ordinal) >= 0)
            {
                ValidateWidget(parsed, result, objectNumber);
            }
        }
    }

    internal static void ValidateFormField(PdfParsedDocument parsed, PdfValidationResult result, int fieldObject)
    {
        string field = parsed.Bodies[fieldObject];
        if (field.IndexOf("/FT /", StringComparison.Ordinal) < 0)
        {
            result.AddError("PDF_FORM_FIELD_TYPE_MISSING", "A top-level form field does not declare an FT entry.", fieldObject);
        }

        if (field.IndexOf("/FT /Sig", StringComparison.Ordinal) >= 0)
        {
            int byteRange = field.IndexOf("/ByteRange [", StringComparison.Ordinal);
            int contents = field.IndexOf("/Contents <", StringComparison.Ordinal);
            if (byteRange < 0 || !HasFourArrayIntegers(field, byteRange + 12))
            {
                result.AddError("PDF_SIGNATURE_BYTE_RANGE_INVALID", "A signature field does not contain four valid ByteRange integers.", fieldObject);
            }

            if (contents < 0 || !HasEvenHexString(field, contents + 11))
            {
                result.AddError("PDF_SIGNATURE_CONTENTS_INVALID", "A signature field does not contain an even-length hexadecimal Contents placeholder.", fieldObject);
            }
        }
    }

    internal static void ValidateWidget(PdfParsedDocument parsed, PdfValidationResult result, int widgetObject)
    {
        string widget = parsed.Bodies[widgetObject];
        int pageObject = FindReferenceAfterKey(widget, "/P");
        if (!IsObjectNumberValid(parsed, pageObject) || parsed.Bodies[pageObject].IndexOf("/Type /Page ", StringComparison.Ordinal) < 0)
        {
            result.AddError("PDF_FORM_WIDGET_PAGE_INVALID", "A form widget does not reference a valid page.", widgetObject);
        }

        if (widget.IndexOf("/FT /", StringComparison.Ordinal) < 0)
        {
            int parentObject = FindReferenceAfterKey(widget, "/Parent");
            if (!IsObjectNumberValid(parsed, parentObject) || parsed.Bodies[parentObject].IndexOf("/FT /", StringComparison.Ordinal) < 0)
            {
                result.AddError("PDF_FORM_WIDGET_PARENT_INVALID", "A child form widget does not reference a valid parent field.", widgetObject);
            }
        }

        if (widget.IndexOf("/AP <<", StringComparison.Ordinal) < 0)
        {
            result.AddWarning("PDF_FORM_WIDGET_APPEARANCE_MISSING", "A form widget has no appearance dictionary.", widgetObject);
        }
    }

    internal static int FindPhysicalObjectBoundary(PdfParsedDocument parsed, int offset)
    {
        int boundary = parsed.XrefOffset;
        for (int objectNumber = 1; objectNumber < parsed.ObjectCount; objectNumber++)
        {
            int candidate = parsed.Offsets[objectNumber];
            if (candidate > offset && candidate < boundary)
            {
                boundary = candidate;
            }
        }

        return boundary;
    }

    internal static string GetDictionaryText(string body)
    {
        int stream = body.IndexOf("\nstream\n", StringComparison.Ordinal);
        if (stream >= 0)
        {
            return body.Substring(0, stream);
        }

        return body;
    }

    internal static List<int> CollectReferences(string text)
    {
        List<int> references = new List<int>();
        int position = 0;
        int literalDepth = 0;
        bool escaped = false;
        bool hexadecimal = false;
        while (position < text.Length)
        {
            char current = text[position];
            if (literalDepth > 0)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (current == '\\')
                {
                    escaped = true;
                }
                else if (current == '(')
                {
                    literalDepth++;
                }
                else if (current == ')')
                {
                    literalDepth--;
                }
            }
            else if (hexadecimal)
            {
                if (current == '>')
                {
                    hexadecimal = false;
                }
            }
            else if (current == '(')
            {
                literalDepth = 1;
            }
            else if (current == '<')
            {
                if (position + 1 < text.Length && text[position + 1] == '<')
                {
                    position++;
                }
                else
                {
                    hexadecimal = true;
                }
            }
            else if (current >= '0' && current <= '9')
            {
                int numberEnd = position;
                while (numberEnd < text.Length && text[numberEnd] >= '0' && text[numberEnd] <= '9')
                {
                    numberEnd++;
                }

                int next = SkipSpaces(text, numberEnd);
                if (next < text.Length && text[next] == '0')
                {
                    int referenceMarker = SkipSpaces(text, next + 1);
                    if (referenceMarker < text.Length && text[referenceMarker] == 'R')
                    {
                        int value = ParseInteger(text, position, numberEnd);
                        if (value >= 0)
                        {
                            references.Add(value);
                        }
                    }
                }

                position = numberEnd - 1;
            }

            position++;
        }

        return references;
    }

    internal static List<int> FindArrayReferences(string text, string key)
    {
        List<int> references = new List<int>();
        int keyPosition = text.IndexOf(key, StringComparison.Ordinal);
        if (keyPosition < 0)
        {
            return references;
        }

        int start = text.IndexOf('[', keyPosition + key.Length);
        int end = text.IndexOf(']', start + 1);
        if (start < 0 || end < 0)
        {
            return references;
        }

        return CollectReferences(text.Substring(start + 1, end - start - 1));
    }

    internal static int FindReferenceAfterKey(string text, string key)
    {
        int keyPosition = text.IndexOf(key, StringComparison.Ordinal);
        if (keyPosition < 0)
        {
            return 0;
        }

        int position = SkipSpaces(text, keyPosition + key.Length);
        int numberEnd = position;
        while (numberEnd < text.Length && text[numberEnd] >= '0' && text[numberEnd] <= '9')
        {
            numberEnd++;
        }

        int generation = SkipSpaces(text, numberEnd);
        int marker = SkipSpaces(text, generation + 1);
        if (numberEnd == position || generation >= text.Length || text[generation] != '0' || marker >= text.Length || text[marker] != 'R')
        {
            return 0;
        }

        return ParseInteger(text, position, numberEnd);
    }

    internal static int FindIntegerAfterKey(string text, string key)
    {
        int keyPosition = text.IndexOf(key, StringComparison.Ordinal);
        if (keyPosition < 0)
        {
            return -1;
        }

        int position = SkipSpaces(text, keyPosition + key.Length);
        int numberEnd = position;
        while (numberEnd < text.Length && text[numberEnd] >= '0' && text[numberEnd] <= '9')
        {
            numberEnd++;
        }

        return ParseInteger(text, position, numberEnd);
    }

    internal static int ParseInteger(string text, int start, int end)
    {
        if (start < 0 || end <= start || end > text.Length)
        {
            return -1;
        }

        int value = 0;
        for (int index = start; index < end; index++)
        {
            char digit = text[index];
            if (digit < '0' || digit > '9' || value > (int.MaxValue - (digit - '0')) / 10)
            {
                return -1;
            }

            value = value * 10 + digit - '0';
        }

        return value;
    }

    internal static int SkipSpaces(string text, int position)
    {
        while (position < text.Length && (text[position] == ' ' || text[position] == '\r' || text[position] == '\n' || text[position] == '\t'))
        {
            position++;
        }

        return position;
    }

    internal static bool HasTextAt(string text, int position, string expected)
    {
        return position >= 0 && position + expected.Length <= text.Length && string.CompareOrdinal(text, position, expected, 0, expected.Length) == 0;
    }

    internal static bool IsObjectNumberValid(PdfParsedDocument parsed, int objectNumber)
    {
        return objectNumber > 0 && objectNumber < parsed.ObjectCount && parsed.Offsets[objectNumber] > 0 && parsed.Bodies[objectNumber].Length > 0;
    }

    internal static void AddPendingReference(List<int> pending, int objectNumber, int objectCount)
    {
        if (objectNumber > 0 && objectNumber < objectCount)
        {
            pending.Add(objectNumber);
        }
    }

    internal static bool HasFourArrayIntegers(string text, int position)
    {
        int count = 0;
        position = SkipSpaces(text, position);
        while (position < text.Length && text[position] != ']' && count < 5)
        {
            int end = position;
            while (end < text.Length && text[end] >= '0' && text[end] <= '9')
            {
                end++;
            }

            if (end == position || ParseInteger(text, position, end) < 0)
            {
                return false;
            }

            count++;
            position = SkipSpaces(text, end);
        }

        return count == 4 && position < text.Length && text[position] == ']';
    }

    internal static bool HasEvenHexString(string text, int position)
    {
        int end = text.IndexOf('>', position);
        if (end < position || (end - position) % 2 != 0)
        {
            return false;
        }

        for (int index = position; index < end; index++)
        {
            char value = text[index];
            bool digit = value >= '0' && value <= '9';
            bool upper = value >= 'A' && value <= 'F';
            bool lower = value >= 'a' && value <= 'f';
            if (!digit && !upper && !lower)
            {
                return false;
            }
        }

        return true;
    }

    internal static void CopyIssues(PdfValidationResult source, PdfValidationResult target)
    {
        foreach (PdfValidationIssue issue in source.GetIssues())
        {
            if (issue.Severity == PdfValidationIssueSeverity.Error)
            {
                target.AddError(issue.Code, issue.Message, issue.ObjectNumber);
            }
            else
            {
                target.AddWarning(issue.Code, issue.Message, issue.ObjectNumber);
            }
        }
    }
}
