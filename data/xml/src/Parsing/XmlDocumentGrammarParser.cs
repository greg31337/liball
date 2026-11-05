using System;

namespace xml;

internal enum XmlDocumentPhase
{
    Initial = 0,
    Prolog = 1,
    ElementContent = 2,
    Epilog = 3,
    Complete = 4
}

internal class XmlDocumentStructure
{
    internal bool HasXmlDeclaration;
    internal XmlVersion Version;
    internal string? EncodingName;
    internal XmlStandalone Standalone;
    internal bool HasDocumentType;
    internal string? DocumentTypeName;
    internal string? DocumentTypePublicIdentifier;
    internal string? DocumentTypeSystemIdentifier;
    internal string? DocumentTypeInternalSubset;
    internal int DocumentTypeStartUtf16Offset;
    internal int DocumentTypeUtf16Length;
    internal string RootName;
    internal int PrologMiscCount;
    internal int EpilogMiscCount;

    internal XmlDocumentStructure(bool hasXmlDeclaration, XmlVersion version, string? encodingName, XmlStandalone standalone, bool hasDocumentType, string? documentTypeName, string? documentTypePublicIdentifier, string? documentTypeSystemIdentifier, string? documentTypeInternalSubset, int documentTypeStartUtf16Offset, int documentTypeUtf16Length, string rootName, int prologMiscCount, int epilogMiscCount)
    {
        HasXmlDeclaration = hasXmlDeclaration;
        Version = version;
        EncodingName = encodingName;
        Standalone = standalone;
        HasDocumentType = hasDocumentType;
        DocumentTypeName = documentTypeName;
        DocumentTypePublicIdentifier = documentTypePublicIdentifier;
        DocumentTypeSystemIdentifier = documentTypeSystemIdentifier;
        DocumentTypeInternalSubset = documentTypeInternalSubset;
        DocumentTypeStartUtf16Offset = documentTypeStartUtf16Offset;
        DocumentTypeUtf16Length = documentTypeUtf16Length;
        RootName = rootName;
        PrologMiscCount = prologMiscCount;
        EpilogMiscCount = epilogMiscCount;
    }
}

internal class XmlDocumentGrammarParser
{
    internal XmlScanner Scanner;
    internal XmlElementTagParser ElementParser;
    internal XmlDocumentPhase Phase;
    internal bool HasXmlDeclaration;
    internal string? EncodingName;
    internal XmlStandalone Standalone;
    internal bool HasDocumentType;
    internal string? DocumentTypeName;
    internal string? DocumentTypePublicIdentifier;
    internal string? DocumentTypeSystemIdentifier;
    internal string? DocumentTypeInternalSubset;
    internal int DocumentTypeStartUtf16Offset;
    internal int DocumentTypeUtf16Length;
    internal string? RootName;
    internal int PrologMiscCount;
    internal int EpilogMiscCount;

    internal XmlDocumentGrammarParser(XmlScanner scanner, bool preserveAttributeQuotes, IXmlAttributeDuplicateDetector? duplicateDetector)
    {
        Guard.NotNull(scanner, "scanner");
        if (scanner.EntityKind != XmlEncodingEntityKind.Document)
        {
            throw new ArgumentException("Document grammar requires a document entity scanner.", "scanner");
        }

        Scanner = scanner;
        ElementParser = new XmlElementTagParser(scanner, preserveAttributeQuotes, duplicateDetector);
        Phase = XmlDocumentPhase.Initial;
        HasXmlDeclaration = false;
        EncodingName = null;
        Standalone = XmlStandalone.Unspecified;
        HasDocumentType = false;
        DocumentTypeName = null;
        DocumentTypePublicIdentifier = null;
        DocumentTypeSystemIdentifier = null;
        DocumentTypeInternalSubset = null;
        DocumentTypeStartUtf16Offset = 0;
        DocumentTypeUtf16Length = 0;
        RootName = null;
        PrologMiscCount = 0;
        EpilogMiscCount = 0;
    }

    internal XmlDocumentStructure ParseXmlDocumentStructure()
    {
        if (Phase != XmlDocumentPhase.Initial)
        {
            throw new InvalidOperationException("The XML document grammar parser has already started.");
        }

        Phase = XmlDocumentPhase.Prolog;
        while (Phase != XmlDocumentPhase.Complete)
        {
            XmlToken token = Scanner.ScanNextXmlToken();
            if (Phase == XmlDocumentPhase.Prolog)
            {
                ProcessXmlPrologToken(token);
            }
            else if (Phase == XmlDocumentPhase.ElementContent)
            {
                ProcessXmlElementContentToken(token);
            }
            else
            {
                ProcessXmlEpilogToken(token);
            }
        }

        string completedRootName = RootName == null ? "" : RootName;
        return new XmlDocumentStructure(HasXmlDeclaration, Scanner.CurrentXmlScannerVersion(), EncodingName, Standalone, HasDocumentType, DocumentTypeName, DocumentTypePublicIdentifier, DocumentTypeSystemIdentifier, DocumentTypeInternalSubset, DocumentTypeStartUtf16Offset, DocumentTypeUtf16Length, completedRootName, PrologMiscCount, EpilogMiscCount);
    }

    internal XmlDocumentPhase CurrentXmlDocumentPhase()
    {
        return Phase;
    }

    internal void ProcessXmlPrologToken(XmlToken token)
    {
        if (token.Kind == XmlTokenKind.XmlDeclaration)
        {
            ApplyXmlDocumentDeclaration(token);
            return;
        }

        if (token.Kind == XmlTokenKind.Text && IsXmlDocumentMiscWhitespace(token))
        {
            PrologMiscCount = PrologMiscCount + 1;
            return;
        }

        if (token.Kind == XmlTokenKind.Comment || token.Kind == XmlTokenKind.ProcessingInstruction)
        {
            PrologMiscCount = PrologMiscCount + 1;
            return;
        }

        if (token.Kind == XmlTokenKind.MarkupDeclarationOpen)
        {
            ParseXmlDocumentType(token);
            return;
        }

        if (token.Kind == XmlTokenKind.StartTagOpen)
        {
            XmlElementTag root = ElementParser.ParseXmlStartOrEmptyElementTag(token);
            RootName = root.Name;
            Phase = root.IsEmptyElement ? XmlDocumentPhase.Epilog : XmlDocumentPhase.ElementContent;
            return;
        }

        if (token.Kind == XmlTokenKind.EndOfInput)
        {
            throw CreateXmlDocumentGrammarError("An XML document requires exactly one root element", token.StartUtf16Offset);
        }

        throw CreateXmlDocumentGrammarError("Only XML declarations, Misc content, a DOCTYPE, or the root element may appear in the prolog", token.StartUtf16Offset);
    }

    internal void ProcessXmlElementContentToken(XmlToken token)
    {
        if (token.Kind == XmlTokenKind.StartTagOpen)
        {
            ElementParser.ParseXmlStartOrEmptyElementTag(token);
            return;
        }

        if (token.Kind == XmlTokenKind.EndTagOpen)
        {
            ElementParser.ParseXmlEndElementTag(token);
            if (ElementParser.CurrentXmlElementDepth() == 0)
            {
                Phase = XmlDocumentPhase.Epilog;
            }

            return;
        }

        if (token.Kind == XmlTokenKind.MarkupDeclarationOpen)
        {
            throw CreateXmlDocumentGrammarError("A DOCTYPE may appear only in the document prolog", token.StartUtf16Offset);
        }

        if (token.Kind == XmlTokenKind.EndOfInput)
        {
            throw CreateXmlDocumentGrammarError("The XML document ended before the root element was closed", token.StartUtf16Offset);
        }

        if (token.Kind == XmlTokenKind.XmlDeclaration || token.Kind == XmlTokenKind.TextDeclaration)
        {
            throw CreateXmlDocumentGrammarError("An XML declaration may appear only at the beginning of the document", token.StartUtf16Offset);
        }
    }

    internal void ProcessXmlEpilogToken(XmlToken token)
    {
        if (token.Kind == XmlTokenKind.Text && IsXmlDocumentMiscWhitespace(token))
        {
            EpilogMiscCount = EpilogMiscCount + 1;
            return;
        }

        if (token.Kind == XmlTokenKind.Comment || token.Kind == XmlTokenKind.ProcessingInstruction)
        {
            EpilogMiscCount = EpilogMiscCount + 1;
            return;
        }

        if (token.Kind == XmlTokenKind.EndOfInput)
        {
            Phase = XmlDocumentPhase.Complete;
            return;
        }

        if (token.Kind == XmlTokenKind.StartTagOpen)
        {
            throw CreateXmlDocumentGrammarError("An XML document cannot contain more than one root element", token.StartUtf16Offset);
        }

        if (token.Kind == XmlTokenKind.MarkupDeclarationOpen)
        {
            throw CreateXmlDocumentGrammarError("A DOCTYPE may not appear after the root element", token.StartUtf16Offset);
        }

        throw CreateXmlDocumentGrammarError("Only Misc content may follow the XML document root", token.StartUtf16Offset);
    }

    internal void ApplyXmlDocumentDeclaration(XmlToken token)
    {
        if (HasXmlDeclaration || token.StartUtf16Offset != 0 || PrologMiscCount != 0 || HasDocumentType || RootName != null)
        {
            throw CreateXmlDocumentGrammarError("An XML declaration may appear only once at the beginning of the document", token.StartUtf16Offset);
        }

        XmlDeclarationInfo? optionalDeclaration = token.Declaration;
        if (optionalDeclaration == null || !optionalDeclaration.HasDeclaration || optionalDeclaration.IsTextDeclaration)
        {
            throw CreateXmlDocumentGrammarError("The XML declaration token has no document declaration metadata", token.StartUtf16Offset);
        }

        HasXmlDeclaration = true;
        EncodingName = optionalDeclaration.EncodingName;
        Standalone = optionalDeclaration.Standalone;
    }

    internal void ParseXmlDocumentType(XmlToken opening)
    {
        if (HasDocumentType)
        {
            throw CreateXmlDocumentGrammarError("An XML document may contain at most one DOCTYPE", opening.StartUtf16Offset);
        }

        XmlDocumentTypeInfo information = new XmlDocumentTypeParser(Scanner).ParseXmlDocumentTypeDeclaration(opening);
        HasDocumentType = true;
        DocumentTypeName = information.Name;
        DocumentTypePublicIdentifier = information.PublicIdentifier;
        DocumentTypeSystemIdentifier = information.SystemIdentifier;
        DocumentTypeInternalSubset = information.InternalSubset;
        DocumentTypeStartUtf16Offset = information.StartUtf16Offset;
        DocumentTypeUtf16Length = information.Utf16Length;
    }

    internal bool IsXmlDocumentMiscWhitespace(XmlToken token)
    {
        string? optionalValue = token.Value;
        if (optionalValue == null || optionalValue.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < optionalValue.Length)
        {
            if (!XmlLexicalCharacterRules.IsXmlSpaceCharacter(optionalValue[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal Exception CreateXmlDocumentGrammarError(string message, int offset)
    {
        return Scanner.CreateXmlScannerError(message, offset);
    }
}
