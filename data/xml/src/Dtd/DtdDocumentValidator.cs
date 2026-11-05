using System;
using System.Collections.Generic;

namespace xml;

internal interface IDtdDocumentDeclarationSource
{
    XmlElementDeclaration? FindDtdDocumentElementDeclaration(string name);
    string[] FindDtdDuplicateElementDeclarationNames();
    string[] FindDtdDuplicateNotationDeclarationNames();
    string[] FindDtdDeclarationValidityErrors();
    XmlEntityDeclaration[] FindAllDtdDocumentEntityDeclarations();
    XmlNotationDeclaration[] FindAllDtdDocumentNotationDeclarations();
    bool HasDtdDocumentNotationDeclaration(string name);
    bool IsDtdDocumentElementDeclarationExternal(string name);
    bool IsDtdDocumentEntityDeclarationExternal(string name);
}

internal class DtdAttributeDefaultEntityObserver : IXmlEntityReferenceObserver
{
    internal DtdParser Parser;
    internal string AttributeKey;

    internal DtdAttributeDefaultEntityObserver(DtdParser parser)
    {
        Guard.NotNull(parser, "parser");
        Parser = parser;
        AttributeKey = "";
    }

    public void ObserveXmlEntityReference(string name, int offset)
    {
        if (Parser.IsDtdDocumentEntityDeclarationExternal(name) && !Parser.ExternalAttributeDefaultEntityDependencies.ContainsKey(AttributeKey))
        {
            Parser.ExternalAttributeDefaultEntityDependencies.Add(AttributeKey, true);
        }
    }
}

internal class DtdEntityWellFormednessObserver : IXmlEntityReferenceObserver
{
    internal IDtdDocumentDeclarationSource Source;
    internal SourceTracker Tracker;
    internal IXmlErrorReporter? ErrorReporter;
    internal bool Standalone;

    internal DtdEntityWellFormednessObserver(IDtdDocumentDeclarationSource source, SourceTracker tracker, IXmlErrorReporter? errorReporter, bool standalone)
    {
        Guard.NotNull(source, "source");
        Guard.NotNull(tracker, "tracker");
        Source = source;
        Tracker = tracker;
        ErrorReporter = errorReporter;
        Standalone = standalone;
    }

    public void ObserveXmlEntityReference(string name, int offset)
    {
        if (Standalone && Source.IsDtdDocumentEntityDeclarationExternal(name))
        {
            throw XmlParserDiagnosticFactory.CreateXmlWellFormednessError("A standalone document cannot reference externally declared entity '" + name + "'", offset, Tracker.SourceUri, Tracker, ErrorReporter);
        }
    }
}

internal class DtdDocumentValidator : IXmlEntityReferenceObserver
{
    internal IDtdDocumentDeclarationSource Source;
    internal DtdValidationContext ValidationContext;
    internal string? DocumentTypeName;
    internal bool Standalone;
    internal bool SawDocumentElement;
    internal List<string> OpenElements;

    internal DtdDocumentValidator(IDtdDocumentDeclarationSource source, DtdValidationContext validationContext, string? documentTypeName, bool standalone)
    {
        Guard.NotNull(source, "source");
        Guard.NotNull(validationContext, "validationContext");
        Source = source;
        ValidationContext = validationContext;
        DocumentTypeName = documentTypeName;
        Standalone = standalone;
        SawDocumentElement = false;
        OpenElements = new List<string>();
        ValidateDtdUniqueDeclarations();
        ValidateDtdDeclarationConstraints();
        ValidateDtdUnparsedEntityDeclarations();
    }

    internal void ValidateDtdDeclarationConstraints()
    {
        string[] errors = Source.FindDtdDeclarationValidityErrors();
        int index = 0;
        while (index < errors.Length)
        {
            ValidationContext.ReportDtdValidationError(errors[index], 0);
            index = index + 1;
        }
    }

    internal void ValidateDtdUniqueDeclarations()
    {
        string[] elementNames = Source.FindDtdDuplicateElementDeclarationNames();
        int elementIndex = 0;
        while (elementIndex < elementNames.Length)
        {
            ValidationContext.ReportDtdValidationError("Element type '" + elementNames[elementIndex] + "' is declared more than once", 0);
            elementIndex = elementIndex + 1;
        }

        string[] notationNames = Source.FindDtdDuplicateNotationDeclarationNames();
        int notationIndex = 0;
        while (notationIndex < notationNames.Length)
        {
            ValidationContext.ReportDtdValidationError("Notation '" + notationNames[notationIndex] + "' is declared more than once", 0);
            notationIndex = notationIndex + 1;
        }
    }

    internal void ValidateDtdUnparsedEntityDeclarations()
    {
        XmlEntityDeclaration[] declarations = Source.FindAllDtdDocumentEntityDeclarations();
        int index = 0;
        while (index < declarations.Length)
        {
            XmlEntityDeclaration declaration = declarations[index];
            if (!declaration.IsParameter && !declaration.IsParsed)
            {
                string entityName = declaration.Name == null ? "" : declaration.Name;
                string notationName = declaration.NotationName == null ? "" : declaration.NotationName;
                if (notationName.Length == 0 || !Source.HasDtdDocumentNotationDeclaration(notationName))
                {
                    ValidationContext.ReportDtdValidationError("Unparsed entity '" + entityName + "' names undeclared notation '" + notationName + "'", 0);
                }
            }

            index = index + 1;
        }
    }

    internal void BeginDtdDocumentElement(string elementName, int offset, bool emptyElement)
    {
        Guard.NotNullOrEmpty(elementName, "elementName");
        if (!SawDocumentElement)
        {
            SawDocumentElement = true;
            if (DocumentTypeName != null && DocumentTypeName != elementName)
            {
                ValidationContext.ReportDtdValidationError("Document element '" + elementName + "' does not match DOCTYPE name '" + DocumentTypeName + "'", offset);
            }
        }

        if (Source.FindDtdDocumentElementDeclaration(elementName) == null)
        {
            ValidationContext.ReportDtdValidationError("Element '" + elementName + "' has no DTD declaration", offset);
        }

        if (!emptyElement)
        {
            OpenElements.Add(elementName);
        }
    }

    internal void EndDtdDocumentElement(string elementName)
    {
        if (OpenElements.Count > 0)
        {
            OpenElements.RemoveAt(OpenElements.Count - 1);
        }
    }

    internal void ObserveDtdDocumentCharacters(string text, int offset)
    {
        if (!Standalone || OpenElements.Count == 0 || !IsDtdDocumentWhitespace(text))
        {
            return;
        }

        string elementName = OpenElements[OpenElements.Count - 1];
        XmlElementDeclaration? declaration = Source.FindDtdDocumentElementDeclaration(elementName);
        if (declaration != null && !declaration.IsEmpty && !declaration.IsAny && !declaration.IsMixed && Source.IsDtdDocumentElementDeclarationExternal(elementName))
        {
            ValidationContext.ReportDtdValidationError("Standalone document element '" + elementName + "' contains whitespace governed by an external element-content declaration", offset);
        }
    }

    internal bool IsDtdDocumentWhitespace(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            if (!XmlLexicalCharacterRules.IsXmlSpaceCharacter(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    public void ObserveXmlEntityReference(string name, int offset)
    {
        if (Standalone && Source.IsDtdDocumentEntityDeclarationExternal(name))
        {
            ValidationContext.ReportDtdValidationError("Standalone document references externally declared entity '" + name + "'", offset);
        }
    }
}
