using System;
using System.Collections.Generic;
using System.Text;

namespace odf;

internal class TextFieldDocumentState
{
    internal OdfNode[] OriginalFieldNodes = [];
    internal OdfNode[] OriginalDeclarationNodes = [];
}

internal static class TextFieldDocumentModel
{
    internal static void LoadTextFields(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for text-field materialization.");
        }
        List<OdfTextField> fields = new List<OdfTextField>();
        List<OdfTextFieldDeclaration> declarations =
            new List<OdfTextFieldDeclaration>();
        List<OdfNode> fieldNodes = new List<OdfNode>();
        List<OdfNode> declarationNodes = new List<OdfNode>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            if (part.Body != null)
            {
                CollectFields(
                    document,
                    part.Body,
                    part.Path,
                    fields,
                    declarations,
                    fieldNodes,
                    declarationNodes);
            }
            partIndex = partIndex + 1;
        }
        int masterIndex = 0;
        while (masterIndex < document.MasterPages.Length)
        {
            OdfMasterPage master = document.MasterPages[masterIndex];
            if (master.Node != null)
            {
                CollectFields(
                    document,
                    master.Node,
                    master.PartPath,
                    fields,
                    declarations,
                    fieldNodes,
                    declarationNodes);
            }
            masterIndex = masterIndex + 1;
        }
        document.TextFields = fields.ToArray();
        document.TextFieldDeclarations = declarations.ToArray();
        ResolveDeclarations(document);
        TextFieldDocumentState state = new TextFieldDocumentState();
        state.OriginalFieldNodes = fieldNodes.ToArray();
        state.OriginalDeclarationNodes = declarationNodes.ToArray();
        document.TextFieldState = state;
    }

    internal static void SynchronizeTextFields(OdfDocument document)
    {
        if (document == null
            || document.TextFields == null
            || document.TextFieldDeclarations == null)
        {
            throw new OdfDocumentException(
                "Text-field and declaration collections are required for writing.");
        }
        TextFieldDocumentState state = document.TextFieldState
            ?? new TextFieldDocumentState();
        RemoveDeletedFields(
            document.TextFields,
            state.OriginalFieldNodes);
        RemoveDeletedDeclarations(
            document.TextFieldDeclarations,
            state.OriginalDeclarationNodes);
        List<OdfNode> declarationNodes = new List<OdfNode>();
        int index = 0;
        while (index < document.TextFieldDeclarations.Length)
        {
            OdfTextFieldDeclaration declaration =
                document.TextFieldDeclarations[index];
            if (declaration == null)
            {
                throw new OdfDocumentException(
                    "The text-field declaration collection contains a null declaration.");
            }
            SynchronizeDeclaration(document, declaration);
            if (declaration.Node != null)
            {
                declarationNodes.Add(declaration.Node);
            }
            index = index + 1;
        }
        List<OdfNode> fieldNodes = new List<OdfNode>();
        index = 0;
        while (index < document.TextFields.Length)
        {
            OdfTextField field = document.TextFields[index];
            if (field == null)
            {
                throw new OdfEditException(
                    "A text field requires an underlying XML element.");
            }
            if (field.Run != null && field.Run.Node != null)
            {
                field.Node = field.Run.Node;
            }
            if (field.Node == null)
            {
                throw new OdfEditException(
                    "A text field requires an underlying XML element.");
            }
            ApplyFieldFields(field);
            fieldNodes.Add(field.Node);
            index = index + 1;
        }
        state.OriginalFieldNodes = fieldNodes.ToArray();
        state.OriginalDeclarationNodes = declarationNodes.ToArray();
        document.TextFieldState = state;
        ResolveDeclarations(document);
    }

    internal static void ResolveDeclarations(OdfDocument document)
    {
        int index = 0;
        while (index < document.TextFields.Length)
        {
            OdfTextField field = document.TextFields[index];
            field.Declaration = null;
            field.HasAmbiguousDeclaration = false;
            OdfTextFieldDeclarationKind kind = DeclarationKindForField(
                field.ElementName);
            if (kind != OdfTextFieldDeclarationKind.Unknown
                && field.Name.Length > 0)
            {
                int count = 0;
                int declarationIndex = 0;
                while (declarationIndex
                    < document.TextFieldDeclarations.Length)
                {
                    OdfTextFieldDeclaration declaration =
                        document.TextFieldDeclarations[declarationIndex];
                    if (declaration.Kind == kind
                        && declaration.Name == field.Name
                        && declaration.PartPath == field.PartPath)
                    {
                        field.Declaration = declaration;
                        count = count + 1;
                    }
                    declarationIndex = declarationIndex + 1;
                }
                if (count != 1)
                {
                    field.Declaration = null;
                    field.HasAmbiguousDeclaration = count > 1;
                }
            }
            RefreshDerivedFieldValues(field);
            index = index + 1;
        }
    }

    static void CollectFields(
        OdfDocument document,
        OdfNode node,
        string partPath,
        List<OdfTextField> fields,
        List<OdfTextFieldDeclaration> declarations,
        List<OdfNode> fieldNodes,
        List<OdfNode> declarationNodes)
    {
        OdfTextFieldDeclarationKind declarationKind =
            ClassifyDeclaration(node);
        if (declarationKind != OdfTextFieldDeclarationKind.Unknown)
        {
            declarations.Add(CreateDeclaration(
                node,
                partPath,
                declarationKind));
            declarationNodes.Add(node);
        }
        else
        {
            OdfTextFieldKind fieldKind = ClassifyField(node);
            if (fieldKind != OdfTextFieldKind.Unknown)
            {
                fields.Add(CreateField(
                    document,
                    node,
                    partPath,
                    fieldKind));
                fieldNodes.Add(node);
            }
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            CollectFields(
                document,
                node.Children[index],
                partPath,
                fields,
                declarations,
                fieldNodes,
                declarationNodes);
            index = index + 1;
        }
    }

    static OdfTextField CreateField(
        OdfDocument document,
        OdfNode node,
        string partPath,
        OdfTextFieldKind kind)
    {
        OdfTextField result = new OdfTextField();
        result.Kind = kind;
        result.ElementName = node.Name == null ? "" : node.Name.LocalName;
        result.Name = Read(node, OdfNamespaces.Text, "name");
        if (result.Name.Length == 0
            && result.ElementName == "dde-connection")
        {
            result.Name = Read(
                node,
                OdfNamespaces.Office,
                "name");
        }
        result.Description = Read(
            node,
            OdfNamespaces.Text,
            "description");
        result.Formula = Read(node, OdfNamespaces.Text, "formula");
        result.Condition = Read(node, OdfNamespaces.Text, "condition");
        result.TrueValue = Read(
            node,
            OdfNamespaces.Text,
            "string-value-if-true");
        result.FalseValue = Read(
            node,
            OdfNamespaces.Text,
            "string-value-if-false");
        result.CurrentValue = Read(
            node,
            OdfNamespaces.Text,
            "current-value");
        result.Display = Read(node, OdfNamespaces.Text, "display");
        result.Hidden = Read(node, OdfNamespaces.Text, "is-hidden");
        result.Fixed = Read(node, OdfNamespaces.Text, "fixed");
        result.ValueType = Read(
            node,
            OdfNamespaces.Office,
            "value-type");
        result.StringValue = Read(
            node,
            OdfNamespaces.Office,
            "string-value");
        if (result.StringValue.Length == 0)
        {
            result.StringValue = Read(
                node,
                OdfNamespaces.Text,
                "string-value");
        }
        result.Value = Read(node, OdfNamespaces.Office, "value");
        result.Currency = Read(node, OdfNamespaces.Office, "currency");
        result.DateValue = Read(
            node,
            OdfNamespaces.Office,
            "date-value");
        result.TimeValue = Read(
            node,
            OdfNamespaces.Office,
            "time-value");
        result.BooleanValue = Read(
            node,
            OdfNamespaces.Office,
            "boolean-value");
        result.DataStyleName = Read(
            node,
            OdfNamespaces.Style,
            "data-style-name");
        result.TextStyleName = Read(
            node,
            OdfNamespaces.Text,
            "style-name");
        result.ReferenceName = Read(
            node,
            OdfNamespaces.Text,
            "ref-name");
        result.ReferenceFormat = Read(
            node,
            OdfNamespaces.Text,
            "reference-format");
        result.PlaceholderType = Read(
            node,
            OdfNamespaces.Text,
            "placeholder-type");
        result.DatabaseName = Read(
            node,
            OdfNamespaces.Text,
            "database-name");
        result.TableName = Read(
            node,
            OdfNamespaces.Text,
            "table-name");
        result.TableType = Read(
            node,
            OdfNamespaces.Text,
            "table-type");
        result.ColumnName = Read(
            node,
            OdfNamespaces.Text,
            "column-name");
        result.RowNumber = Read(
            node,
            OdfNamespaces.Text,
            "row-number");
        result.Attributes = node.Attributes;
        FindFieldRun(document.TextBlocks, node, result);
        result.DisplayText = result.Run == null
            ? ExtractNodeText(node)
            : TextExtractor.ExtractRunText(result.Run.Runs);
        result.OriginalDisplayText = result.DisplayText;
        result.PartPath = partPath;
        result.Node = node;
        RefreshDerivedFieldValues(result);
        result.OriginalIsHidden = result.IsHidden;
        return result;
    }

    static OdfTextFieldDeclaration CreateDeclaration(
        OdfNode node,
        string partPath,
        OdfTextFieldDeclarationKind kind)
    {
        OdfTextFieldDeclaration result = new OdfTextFieldDeclaration();
        result.Kind = kind;
        result.Name = Read(node, OdfNamespaces.Text, "name");
        if (kind == OdfTextFieldDeclarationKind.DdeConnection)
        {
            result.Name = Read(node, OdfNamespaces.Office, "name");
        }
        result.ValueType = Read(
            node,
            OdfNamespaces.Office,
            "value-type");
        result.StringValue = Read(
            node,
            OdfNamespaces.Office,
            "string-value");
        result.Value = Read(node, OdfNamespaces.Office, "value");
        result.Currency = Read(node, OdfNamespaces.Office, "currency");
        result.DateValue = Read(
            node,
            OdfNamespaces.Office,
            "date-value");
        result.TimeValue = Read(
            node,
            OdfNamespaces.Office,
            "time-value");
        result.BooleanValue = Read(
            node,
            OdfNamespaces.Office,
            "boolean-value");
        result.Formula = Read(node, OdfNamespaces.Text, "formula");
        result.DisplayOutlineLevel = Read(
            node,
            OdfNamespaces.Text,
            "display-outline-level");
        result.SeparationCharacter = Read(
            node,
            OdfNamespaces.Text,
            "separation-character");
        result.DdeApplication = Read(
            node,
            OdfNamespaces.Office,
            "dde-application");
        result.DdeTopic = Read(
            node,
            OdfNamespaces.Office,
            "dde-topic");
        result.DdeItem = Read(
            node,
            OdfNamespaces.Office,
            "dde-item");
        result.AutomaticUpdate = Read(
            node,
            OdfNamespaces.Office,
            "automatic-update");
        result.Attributes = node.Attributes;
        result.PartPath = partPath;
        result.Node = node;
        return result;
    }

    static void SynchronizeDeclaration(
        OdfDocument document,
        OdfTextFieldDeclaration declaration)
    {
        OdfNode? node = declaration.Node;
        if (node == null)
        {
            OdfNode container = EnsureDeclarationContainer(
                document,
                declaration);
            node = CreateTextElement(
                DeclarationElementName(declaration.Kind),
                container.Location.PartPath);
            StyleDocumentModel.AppendNode(container, node);
            declaration.Node = node;
            declaration.PartPath = node.Location.PartPath;
        }
        ApplyPreservedAttributes(node, declaration.Attributes);
        string nameNamespace = declaration.Kind
                == OdfTextFieldDeclarationKind.DdeConnection
            ? OdfNamespaces.Office
            : OdfNamespaces.Text;
        string namePrefix = nameNamespace == OdfNamespaces.Office
            ? OdfNamespaces.OfficePrefix
            : OdfNamespaces.TextPrefix;
        WriteOptional(
            node,
            nameNamespace,
            "name",
            namePrefix,
            declaration.Name);
        WriteValueAttributes(
            node,
            declaration.ValueType,
            declaration.StringValue,
            declaration.Value,
            declaration.Currency,
            declaration.DateValue,
            declaration.TimeValue,
            declaration.BooleanValue);
        WriteOptional(node, OdfNamespaces.Text, "formula", OdfNamespaces.TextPrefix, declaration.Formula);
        WriteOptional(node, OdfNamespaces.Text, "display-outline-level", OdfNamespaces.TextPrefix, declaration.DisplayOutlineLevel);
        WriteOptional(node, OdfNamespaces.Text, "separation-character", OdfNamespaces.TextPrefix, declaration.SeparationCharacter);
        WriteOptional(node, OdfNamespaces.Office, "dde-application", OdfNamespaces.OfficePrefix, declaration.DdeApplication);
        WriteOptional(node, OdfNamespaces.Office, "dde-topic", OdfNamespaces.OfficePrefix, declaration.DdeTopic);
        WriteOptional(node, OdfNamespaces.Office, "dde-item", OdfNamespaces.OfficePrefix, declaration.DdeItem);
        WriteOptional(node, OdfNamespaces.Office, "automatic-update", OdfNamespaces.OfficePrefix, declaration.AutomaticUpdate);
        declaration.Attributes = node.Attributes;
    }

    static void ApplyFieldFields(OdfTextField field)
    {
        OdfNode node = field.Node ?? new OdfNode();
        ApplyPreservedAttributes(node, field.Attributes);
        WriteOptional(node, OdfNamespaces.Text, "name", OdfNamespaces.TextPrefix, field.Name);
        if (field.ElementName == "dde-connection")
        {
            StyleDocumentModel.RemoveAttribute(node, OdfNamespaces.Text, "name");
            WriteOptional(node, OdfNamespaces.Office, "name", OdfNamespaces.OfficePrefix, field.Name);
        }
        WriteOptional(node, OdfNamespaces.Text, "description", OdfNamespaces.TextPrefix, field.Description);
        WriteOptional(node, OdfNamespaces.Text, "formula", OdfNamespaces.TextPrefix, field.Formula);
        WriteOptional(node, OdfNamespaces.Text, "condition", OdfNamespaces.TextPrefix, field.Condition);
        WriteOptional(node, OdfNamespaces.Text, "string-value-if-true", OdfNamespaces.TextPrefix, field.TrueValue);
        WriteOptional(node, OdfNamespaces.Text, "string-value-if-false", OdfNamespaces.TextPrefix, field.FalseValue);
        WriteOptional(node, OdfNamespaces.Text, "current-value", OdfNamespaces.TextPrefix, field.CurrentValue);
        WriteOptional(node, OdfNamespaces.Text, "display", OdfNamespaces.TextPrefix, field.Display);
        string hidden = field.Hidden;
        if (field.IsHidden != field.OriginalIsHidden)
        {
            hidden = field.IsHidden ? "true" : "false";
        }
        WriteOptional(node, OdfNamespaces.Text, "is-hidden", OdfNamespaces.TextPrefix, hidden);
        field.Hidden = hidden;
        field.IsHidden = hidden == "true";
        field.OriginalIsHidden = field.IsHidden;
        WriteOptional(node, OdfNamespaces.Text, "fixed", OdfNamespaces.TextPrefix, field.Fixed);
        WriteValueAttributes(node, field.ValueType, field.StringValue, field.Value, field.Currency, field.DateValue, field.TimeValue, field.BooleanValue);
        WriteOptional(node, OdfNamespaces.Style, "data-style-name", OdfNamespaces.StylePrefix, field.DataStyleName);
        WriteOptional(node, OdfNamespaces.Text, "style-name", OdfNamespaces.TextPrefix, field.TextStyleName);
        WriteOptional(node, OdfNamespaces.Text, "ref-name", OdfNamespaces.TextPrefix, field.ReferenceName);
        WriteOptional(node, OdfNamespaces.Text, "reference-format", OdfNamespaces.TextPrefix, field.ReferenceFormat);
        WriteOptional(node, OdfNamespaces.Text, "placeholder-type", OdfNamespaces.TextPrefix, field.PlaceholderType);
        WriteOptional(node, OdfNamespaces.Text, "database-name", OdfNamespaces.TextPrefix, field.DatabaseName);
        WriteOptional(node, OdfNamespaces.Text, "table-name", OdfNamespaces.TextPrefix, field.TableName);
        WriteOptional(node, OdfNamespaces.Text, "table-type", OdfNamespaces.TextPrefix, field.TableType);
        WriteOptional(node, OdfNamespaces.Text, "column-name", OdfNamespaces.TextPrefix, field.ColumnName);
        WriteOptional(node, OdfNamespaces.Text, "row-number", OdfNamespaces.TextPrefix, field.RowNumber);
        if (field.StringValue.Length > 0
            && UsesTextStringValue(field.ElementName))
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                OdfNamespaces.Office,
                "string-value");
            WriteOptional(node, OdfNamespaces.Text, "string-value", OdfNamespaces.TextPrefix, field.StringValue);
        }
        if (field.DisplayText != field.OriginalDisplayText)
        {
            ReplaceDisplayText(node, field.DisplayText);
            field.OriginalDisplayText = field.DisplayText;
        }
        field.Attributes = node.Attributes;
        RefreshDerivedFieldValues(field);
    }

    static void WriteValueAttributes(
        OdfNode node,
        string valueType,
        string stringValue,
        string value,
        string currency,
        string dateValue,
        string timeValue,
        string booleanValue)
    {
        WriteOptional(node, OdfNamespaces.Office, "value-type", OdfNamespaces.OfficePrefix, valueType);
        WriteOptional(node, OdfNamespaces.Office, "string-value", OdfNamespaces.OfficePrefix, stringValue);
        WriteOptional(node, OdfNamespaces.Office, "value", OdfNamespaces.OfficePrefix, value);
        WriteOptional(node, OdfNamespaces.Office, "currency", OdfNamespaces.OfficePrefix, currency);
        WriteOptional(node, OdfNamespaces.Office, "date-value", OdfNamespaces.OfficePrefix, dateValue);
        WriteOptional(node, OdfNamespaces.Office, "time-value", OdfNamespaces.OfficePrefix, timeValue);
        WriteOptional(node, OdfNamespaces.Office, "boolean-value", OdfNamespaces.OfficePrefix, booleanValue);
    }

    static void RefreshDerivedFieldValues(OdfTextField field)
    {
        field.SelectedText = "";
        if (field.Kind == OdfTextFieldKind.ConditionalText)
        {
            field.SelectedText = field.CurrentValue == "true"
                ? field.TrueValue
                : field.FalseValue;
        }
        field.IsHidden = field.Hidden == "true"
            || field.Display == "none";
    }

    static OdfTextFieldKind ClassifyField(OdfNode node)
    {
        if (!IsTextElement(node))
        {
            return OdfTextFieldKind.Unknown;
        }
        string name = node.Name == null ? "" : node.Name.LocalName;
        if (name == "page-number"
            || name == "page-continuation"
            || name == "page-variable-get"
            || name == "page-variable-set")
        {
            return OdfTextFieldKind.PageNumber;
        }
        if (name == "page-count")
        {
            return OdfTextFieldKind.PageCount;
        }
        if (name == "date"
            || name == "creation-date"
            || name == "print-date"
            || name == "modification-date")
        {
            return OdfTextFieldKind.Date;
        }
        if (name == "time"
            || name == "creation-time"
            || name == "print-time"
            || name == "modification-time")
        {
            return OdfTextFieldKind.Time;
        }
        if (name == "author-name" || name == "author-initials")
        {
            return OdfTextFieldKind.AuthorName;
        }
        if (name.StartsWith("sender-", StringComparison.Ordinal))
        {
            return OdfTextFieldKind.Sender;
        }
        if (name == "file-name")
        {
            return OdfTextFieldKind.FileName;
        }
        if (name == "template-name")
        {
            return OdfTextFieldKind.TemplateName;
        }
        if (name == "chapter")
        {
            return OdfTextFieldKind.Chapter;
        }
        if (name == "word-count")
        {
            return OdfTextFieldKind.WordCount;
        }
        if (name == "character-count")
        {
            return OdfTextFieldKind.CharacterCount;
        }
        if (name == "paragraph-count")
        {
            return OdfTextFieldKind.ParagraphCount;
        }
        if (name == "table-count")
        {
            return OdfTextFieldKind.TableCount;
        }
        if (name == "image-count")
        {
            return OdfTextFieldKind.ImageCount;
        }
        if (name == "object-count")
        {
            return OdfTextFieldKind.ObjectCount;
        }
        if (name == "variable-set"
            || name == "variable-get"
            || name == "variable-input")
        {
            return OdfTextFieldKind.Variable;
        }
        if (name == "user-field-get" || name == "user-field-input")
        {
            return OdfTextFieldKind.UserField;
        }
        if (name == "sequence")
        {
            return OdfTextFieldKind.Sequence;
        }
        if (name == "expression" || name == "table-formula")
        {
            return OdfTextFieldKind.Expression;
        }
        if (name == "placeholder")
        {
            return OdfTextFieldKind.Placeholder;
        }
        if (name == "text-input"
            || name == "drop-down"
            || name == "execute-macro")
        {
            return OdfTextFieldKind.Input;
        }
        if (name == "conditional-text")
        {
            return OdfTextFieldKind.ConditionalText;
        }
        if (name == "hidden-text")
        {
            return OdfTextFieldKind.HiddenText;
        }
        if (name == "hidden-paragraph")
        {
            return OdfTextFieldKind.HiddenParagraph;
        }
        if (name.StartsWith("database-", StringComparison.Ordinal)
            || name == "dde-connection")
        {
            return OdfTextFieldKind.Database;
        }
        if (name == "reference-ref"
            || name == "bookmark-ref"
            || name == "sequence-ref"
            || name == "note-ref")
        {
            return OdfTextFieldKind.Reference;
        }
        if (name == "sheet-name")
        {
            return OdfTextFieldKind.SheetName;
        }
        if (name == "cell-address")
        {
            return OdfTextFieldKind.CellAddress;
        }
        if (IsMetadataFieldName(name))
        {
            return OdfTextFieldKind.Metadata;
        }
        return OdfTextFieldKind.Unknown;
    }

    static bool IsMetadataFieldName(string name)
    {
        return name == "meta-field"
            || name == "document-title"
            || name == "subject"
            || name == "description"
            || name == "keywords"
            || name == "creator"
            || name == "initial-creator"
            || name == "printed-by"
            || name == "editing-duration"
            || name == "editing-cycles"
            || name == "user-defined"
            || name == "measure";
    }

    static OdfTextFieldDeclarationKind ClassifyDeclaration(OdfNode node)
    {
        if (!IsTextElement(node) || node.Name == null)
        {
            return OdfTextFieldDeclarationKind.Unknown;
        }
        string name = node.Name.LocalName;
        if (name == "variable-decl")
        {
            return OdfTextFieldDeclarationKind.Variable;
        }
        if (name == "user-field-decl")
        {
            return OdfTextFieldDeclarationKind.UserField;
        }
        if (name == "sequence-decl")
        {
            return OdfTextFieldDeclarationKind.Sequence;
        }
        if (name == "dde-connection-decl")
        {
            return OdfTextFieldDeclarationKind.DdeConnection;
        }
        return OdfTextFieldDeclarationKind.Unknown;
    }

    static OdfTextFieldDeclarationKind DeclarationKindForField(
        string elementName)
    {
        if (elementName == "variable-set"
            || elementName == "variable-get"
            || elementName == "variable-input")
        {
            return OdfTextFieldDeclarationKind.Variable;
        }
        if (elementName == "user-field-get"
            || elementName == "user-field-input")
        {
            return OdfTextFieldDeclarationKind.UserField;
        }
        if (elementName == "sequence")
        {
            return OdfTextFieldDeclarationKind.Sequence;
        }
        if (elementName == "dde-connection")
        {
            return OdfTextFieldDeclarationKind.DdeConnection;
        }
        return OdfTextFieldDeclarationKind.Unknown;
    }

    static void FindFieldRun(
        OdfTextBlock[] blocks,
        OdfNode node,
        OdfTextField field)
    {
        int index = 0;
        while (index < blocks.Length && field.Run == null)
        {
            OdfTextRun? run = FindRunByNode(blocks[index].Runs, node);
            if (run != null)
            {
                field.Run = run;
                field.TextBlock = blocks[index];
            }
            index = index + 1;
        }
    }

    static OdfTextRun? FindRunByNode(
        OdfTextRun[] runs,
        OdfNode node)
    {
        int index = 0;
        while (index < runs.Length)
        {
            OdfTextRun run = runs[index];
            if (run.Node == node)
            {
                return run;
            }
            OdfTextRun? nested = FindRunByNode(run.Runs, node);
            if (nested != null)
            {
                return nested;
            }
            index = index + 1;
        }
        return null;
    }

    static string ExtractNodeText(OdfNode node)
    {
        StringBuilder result = new StringBuilder();
        AppendNodeText(result, node);
        return result.ToString();
    }

    static void AppendNodeText(StringBuilder result, OdfNode node)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Append(node.Value);
            return;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AppendNodeText(result, node.Children[index]);
            index = index + 1;
        }
    }

    static void ReplaceDisplayText(OdfNode node, string value)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            node.Children[index].Parent = null;
            index = index + 1;
        }
        if (value.Length == 0)
        {
            node.Children = [];
            return;
        }
        OdfNode text = new OdfNode();
        text.Kind = OdfNodeKind.Text;
        text.Value = value;
        text.Parent = node;
        text.Location.PartPath = node.Location.PartPath;
        node.Children = [text];
        node.WasEmptyElement = false;
    }

    static OdfNode EnsureDeclarationContainer(
        OdfDocument document,
        OdfTextFieldDeclaration declaration)
    {
        string containerName = DeclarationContainerName(declaration.Kind);
        OdfNode? root = FindTextRoot(document, declaration.PartPath);
        if (root == null)
        {
            throw new OdfEditException(
                "A new text-field declaration requires a materialized text body.");
        }
        int index = 0;
        while (index < root.Children.Length)
        {
            OdfNode child = root.Children[index];
            if (IsTextElement(child)
                && child.Name != null
                && child.Name.LocalName == containerName)
            {
                return child;
            }
            index = index + 1;
        }
        OdfNode created = CreateTextElement(
            containerName,
            root.Location.PartPath);
        PrependChild(root, created);
        return created;
    }

    static OdfNode? FindTextRoot(
        OdfDocument document,
        string partPath)
    {
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            bool pathMatches = document.PackageKind == OdfPackageKind.Flat
                || part.Path == partPath
                || partPath.Length == 0
                    && part.Path == OdfPartNames.Content;
            if (pathMatches && part.Body != null)
            {
                int index = 0;
                while (index < part.Body.Children.Length)
                {
                    OdfNode child = part.Body.Children[index];
                    if (child.Kind == OdfNodeKind.Element
                        && child.Name != null
                        && child.Name.NamespaceName == OdfNamespaces.Office
                        && child.Name.LocalName == "text")
                    {
                        return child;
                    }
                    index = index + 1;
                }
            }
            partIndex = partIndex + 1;
        }
        return null;
    }

    static string DeclarationContainerName(
        OdfTextFieldDeclarationKind kind)
    {
        if (kind == OdfTextFieldDeclarationKind.Variable)
        {
            return "variable-decls";
        }
        if (kind == OdfTextFieldDeclarationKind.UserField)
        {
            return "user-field-decls";
        }
        if (kind == OdfTextFieldDeclarationKind.Sequence)
        {
            return "sequence-decls";
        }
        return "dde-connection-decls";
    }

    static string DeclarationElementName(
        OdfTextFieldDeclarationKind kind)
    {
        if (kind == OdfTextFieldDeclarationKind.Variable)
        {
            return "variable-decl";
        }
        if (kind == OdfTextFieldDeclarationKind.UserField)
        {
            return "user-field-decl";
        }
        if (kind == OdfTextFieldDeclarationKind.Sequence)
        {
            return "sequence-decl";
        }
        return "dde-connection-decl";
    }

    static OdfNode CreateTextElement(string localName, string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            OdfNamespaces.Text,
            localName,
            OdfNamespaces.TextPrefix);
        result.Location.PartPath = partPath;
        return result;
    }

    static void PrependChild(OdfNode parent, OdfNode child)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        children[0] = child;
        Array.Copy(parent.Children, 0, children, 1, parent.Children.Length);
        parent.Children = children;
        child.Parent = parent;
        child.Location.PartPath = parent.Location.PartPath;
        parent.WasEmptyElement = false;
    }

    static void ApplyPreservedAttributes(
        OdfNode node,
        OdfAttribute[] attributes)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            OdfAttribute attribute = attributes[index];
            StyleDocumentModel.WriteAttribute(
                node,
                attribute.Name.NamespaceName,
                attribute.Name.LocalName,
                attribute.Name.Prefix,
                attribute.Value);
            index = index + 1;
        }
    }

    static bool UsesTextStringValue(string elementName)
    {
        return elementName == "conditional-text"
            || elementName == "hidden-text"
            || elementName == "user-defined";
    }

    static bool IsTextElement(OdfNode node)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Text;
    }

    static string Read(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return StyleDocumentModel.ReadAttribute(
            node,
            namespaceName,
            localName);
    }

    static void WriteOptional(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                namespaceName,
                localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                namespaceName,
                localName,
                prefix,
                value);
        }
    }

    static void RemoveDeletedFields(
        OdfTextField[] fields,
        OdfNode[] originalNodes)
    {
        int index = 0;
        while (index < originalNodes.Length)
        {
            OdfNode node = originalNodes[index];
            bool retained = false;
            int fieldIndex = 0;
            while (fieldIndex < fields.Length && !retained)
            {
                retained = fields[fieldIndex] != null
                    && fields[fieldIndex].Node == node;
                fieldIndex = fieldIndex + 1;
            }
            if (!retained)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }

    static void RemoveDeletedDeclarations(
        OdfTextFieldDeclaration[] declarations,
        OdfNode[] originalNodes)
    {
        int index = 0;
        while (index < originalNodes.Length)
        {
            OdfNode node = originalNodes[index];
            bool retained = false;
            int declarationIndex = 0;
            while (declarationIndex < declarations.Length && !retained)
            {
                retained = declarations[declarationIndex] != null
                    && declarations[declarationIndex].Node == node;
                declarationIndex = declarationIndex + 1;
            }
            if (!retained)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }
}
