using System;

namespace xml;

internal enum XmlFragmentPhase
{
    Initial = 0,
    Content = 1,
    Complete = 2
}

internal class XmlFragmentContext
{
    internal XmlNamespaceBinding[] NamespaceBindings;
    internal string? BaseUri;
    internal string? Language;
    internal XmlSpaceMode SpaceMode;

    internal XmlFragmentContext(XmlNamespaceBinding[] namespaceBindings, string? baseUri, string? language, XmlSpaceMode spaceMode)
    {
        Guard.NotNull(namespaceBindings, "namespaceBindings");
        if (spaceMode != XmlSpaceMode.Default && spaceMode != XmlSpaceMode.Preserve)
        {
            throw new ArgumentOutOfRangeException("spaceMode");
        }

        int index = 0;
        while (index < namespaceBindings.Length)
        {
            if (namespaceBindings[index] == null)
            {
                throw new ArgumentException("An inherited namespace binding cannot be null.", "namespaceBindings");
            }

            index = index + 1;
        }

        NamespaceBindings = new XmlNamespaceBinding[namespaceBindings.Length];
        Array.Copy(namespaceBindings, NamespaceBindings, namespaceBindings.Length);
        BaseUri = baseUri;
        Language = language;
        SpaceMode = spaceMode;
    }
}

internal class XmlFragmentStructure
{
    internal XmlFragmentContext InheritedContext;
    internal bool HasTextDeclaration;
    internal XmlVersion Version;
    internal string? EncodingName;
    internal int TopLevelItemCount;
    internal int TopLevelElementCount;
    internal int MaximumElementDepth;

    internal XmlFragmentStructure(XmlFragmentContext inheritedContext, bool hasTextDeclaration, XmlVersion version, string? encodingName, int topLevelItemCount, int topLevelElementCount, int maximumElementDepth)
    {
        InheritedContext = inheritedContext;
        HasTextDeclaration = hasTextDeclaration;
        Version = version;
        EncodingName = encodingName;
        TopLevelItemCount = topLevelItemCount;
        TopLevelElementCount = topLevelElementCount;
        MaximumElementDepth = maximumElementDepth;
    }
}

internal class XmlFragmentGrammarParser
{
    internal XmlScanner Scanner;
    internal XmlElementTagParser ElementParser;
    internal XmlFragmentContext InheritedContext;
    internal XmlFragmentPhase Phase;
    internal bool HasTextDeclaration;
    internal string? EncodingName;
    internal bool HasContent;
    internal int TopLevelItemCount;
    internal int TopLevelElementCount;
    internal int MaximumElementDepth;

    internal XmlFragmentGrammarParser(XmlScanner scanner, XmlFragmentContext inheritedContext, bool preserveAttributeQuotes, IXmlAttributeDuplicateDetector? duplicateDetector)
    {
        Guard.NotNull(scanner, "scanner");
        Guard.NotNull(inheritedContext, "inheritedContext");
        if (scanner.EntityKind != XmlEncodingEntityKind.ExternalParsedEntity)
        {
            throw new ArgumentException("Fragment grammar requires an external parsed entity scanner.", "scanner");
        }

        Scanner = scanner;
        ElementParser = new XmlElementTagParser(scanner, preserveAttributeQuotes, duplicateDetector);
        InheritedContext = inheritedContext;
        Phase = XmlFragmentPhase.Initial;
        HasTextDeclaration = false;
        EncodingName = null;
        HasContent = false;
        TopLevelItemCount = 0;
        TopLevelElementCount = 0;
        MaximumElementDepth = 0;
    }

    internal XmlFragmentStructure ParseXmlFragmentStructure()
    {
        if (Phase != XmlFragmentPhase.Initial)
        {
            throw new InvalidOperationException("The XML fragment grammar parser has already started.");
        }

        Phase = XmlFragmentPhase.Content;
        while (Phase != XmlFragmentPhase.Complete)
        {
            XmlToken token = Scanner.ScanNextXmlToken();
            ProcessXmlFragmentToken(token);
        }

        return new XmlFragmentStructure(InheritedContext, HasTextDeclaration, Scanner.CurrentXmlScannerVersion(), EncodingName, TopLevelItemCount, TopLevelElementCount, MaximumElementDepth);
    }

    internal XmlFragmentPhase CurrentXmlFragmentPhase()
    {
        return Phase;
    }

    internal void ProcessXmlFragmentToken(XmlToken token)
    {
        if (token.Kind == XmlTokenKind.TextDeclaration)
        {
            ApplyXmlFragmentTextDeclaration(token);
            return;
        }

        if (token.Kind == XmlTokenKind.StartTagOpen)
        {
            int startingDepth = ElementParser.CurrentXmlElementDepth();
            XmlElementTag element = ElementParser.ParseXmlStartOrEmptyElementTag(token);
            if (startingDepth == 0)
            {
                TopLevelItemCount = TopLevelItemCount + 1;
                TopLevelElementCount = TopLevelElementCount + 1;
            }

            int elementDepth = element.Depth + 1;
            if (elementDepth > MaximumElementDepth)
            {
                MaximumElementDepth = elementDepth;
            }

            HasContent = true;
            return;
        }

        if (token.Kind == XmlTokenKind.EndTagOpen)
        {
            ElementParser.ParseXmlEndElementTag(token);
            HasContent = true;
            return;
        }

        if (token.Kind == XmlTokenKind.MarkupDeclarationOpen)
        {
            throw CreateXmlFragmentGrammarError("A DOCTYPE or other markup declaration cannot appear in an XML fragment", token.StartUtf16Offset);
        }

        if (token.Kind == XmlTokenKind.XmlDeclaration)
        {
            throw CreateXmlFragmentGrammarError("An XML fragment cannot contain an XML document declaration", token.StartUtf16Offset);
        }

        if (token.Kind == XmlTokenKind.EndOfInput)
        {
            if (ElementParser.CurrentXmlElementDepth() != 0)
            {
                throw CreateXmlFragmentGrammarError("The XML fragment ended before all elements were closed", token.StartUtf16Offset);
            }

            Phase = XmlFragmentPhase.Complete;
            return;
        }

        if (ElementParser.CurrentXmlElementDepth() == 0)
        {
            TopLevelItemCount = TopLevelItemCount + 1;
        }

        HasContent = true;
    }

    internal void ApplyXmlFragmentTextDeclaration(XmlToken token)
    {
        if (HasTextDeclaration || HasContent || token.StartUtf16Offset != 0)
        {
            throw CreateXmlFragmentGrammarError("A text declaration may appear only once at the beginning of an XML fragment", token.StartUtf16Offset);
        }

        XmlDeclarationInfo? optionalDeclaration = token.Declaration;
        if (optionalDeclaration == null || !optionalDeclaration.HasDeclaration || !optionalDeclaration.IsTextDeclaration)
        {
            throw CreateXmlFragmentGrammarError("The text declaration token has no external-entity declaration metadata", token.StartUtf16Offset);
        }

        HasTextDeclaration = true;
        EncodingName = optionalDeclaration.EncodingName;
    }

    internal Exception CreateXmlFragmentGrammarError(string message, int offset)
    {
        return Scanner.CreateXmlScannerError(message, offset);
    }
}
