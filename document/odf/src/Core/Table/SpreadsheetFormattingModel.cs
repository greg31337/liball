using System;
using System.Collections.Generic;
using System.Text;

namespace odf;

internal class SpreadsheetFormattingState
{
    internal OdfNode? SpreadsheetNode;
    internal OdfContentValidation[] Validations = [];
    internal OdfConditionalStyle[] ConditionalStyles = [];
}

internal static class SpreadsheetFormattingModel
{
    internal static void LoadSpreadsheetFormatting(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for spreadsheet-formatting materialization.");
        }
        SpreadsheetFormattingState state = new SpreadsheetFormattingState();
        List<OdfContentValidation> validations =
            new List<OdfContentValidation>();
        List<OdfConditionalStyle> conditionalStyles =
            new List<OdfConditionalStyle>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            if (part.Root != null)
            {
                CollectFormatting(
                    part.Root,
                    part.Path,
                    part.AutomaticStyles,
                    state,
                    validations,
                    conditionalStyles);
            }
            partIndex = partIndex + 1;
        }
        document.ContentValidations = validations.ToArray();
        document.ConditionalStyles = conditionalStyles.ToArray();
        state.Validations = document.ContentValidations;
        state.ConditionalStyles = document.ConditionalStyles;
        document.SpreadsheetFormattingState = state;
        ResolveConditionalStyles(document);
    }

    internal static void SynchronizeSpreadsheetFormatting(
        OdfDocument document)
    {
        if (document == null
            || document.ContentValidations == null
            || document.ConditionalStyles == null)
        {
            throw new OdfDocumentException(
                "Spreadsheet validation and conditional-style collections are required for writing.");
        }
        SpreadsheetFormattingState? state =
            document.SpreadsheetFormattingState;
        if (state == null)
        {
            LoadSpreadsheetFormatting(document);
            state = document.SpreadsheetFormattingState;
        }
        if (state == null)
        {
            return;
        }
        RemoveDeletedValidations(
            state.Validations,
            document.ContentValidations);
        RemoveDeletedConditionalStyles(
            state.ConditionalStyles,
            document.ConditionalStyles);
        SynchronizeValidations(
            document.ContentValidations,
            state.SpreadsheetNode);
        SynchronizeConditionalStyles(document);
        state.Validations = document.ContentValidations;
        state.ConditionalStyles = document.ConditionalStyles;
        ResolveConditionalStyles(document);
    }

    internal static OdfContentValidation? FindValidation(
        OdfDocument document,
        string name)
    {
        if (name.Length == 0)
        {
            return null;
        }
        OdfContentValidation? result = null;
        int matches = 0;
        int index = 0;
        while (index < document.ContentValidations.Length)
        {
            OdfContentValidation validation =
                document.ContentValidations[index];
            if (validation != null && validation.Name == name)
            {
                result = validation;
                matches = matches + 1;
            }
            index = index + 1;
        }
        return matches == 1 ? result : null;
    }

    static void CollectFormatting(
        OdfNode node,
        string partPath,
        OdfNode? automaticStyles,
        SpreadsheetFormattingState state,
        List<OdfContentValidation> validations,
        List<OdfConditionalStyle> conditionalStyles)
    {
        if (IsOfficeElement(node, "spreadsheet")
            && state.SpreadsheetNode == null)
        {
            state.SpreadsheetNode = node;
        }
        if (IsTableElement(node, "content-validation"))
        {
            validations.Add(CreateValidation(node));
        }
        else if (IsStyleElement(node, "map")
            && node.Parent != null
            && IsOrdinaryStyle(node.Parent))
        {
            conditionalStyles.Add(CreateConditionalStyle(
                node,
                partPath,
                automaticStyles));
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            CollectFormatting(
                node.Children[index],
                partPath,
                automaticStyles,
                state,
                validations,
                conditionalStyles);
            index = index + 1;
        }
    }

    static OdfContentValidation CreateValidation(OdfNode node)
    {
        OdfContentValidation result = new OdfContentValidation();
        result.Node = node;
        result.OriginalNode = node;
        result.Name = ReadAttribute(node, OdfNamespaces.Table, "name");
        result.Condition = ReadAttribute(
            node,
            OdfNamespaces.Table,
            "condition");
        result.BaseCellAddress = ReadAttribute(
            node,
            OdfNamespaces.Table,
            "base-cell-address");
        result.AllowEmptyCell = ReadBooleanAttribute(
            node,
            "allow-empty-cell",
            true);
        result.DisplayList = ReadAttribute(
            node,
            OdfNamespaces.Table,
            "display-list");
        if (result.DisplayList.Length == 0)
        {
            result.DisplayList = "none";
        }
        result.HelpMessageNode = FindDirectTableChild(node, "help-message");
        result.HasHelpMessage = result.HelpMessageNode != null;
        if (result.HelpMessageNode != null)
        {
            result.DisplayHelp = ReadBooleanAttribute(
                result.HelpMessageNode,
                "display",
                false);
            result.HelpTitle = ReadAttribute(
                result.HelpMessageNode,
                OdfNamespaces.Table,
                "title");
            result.HelpText = ExtractMessageText(result.HelpMessageNode);
        }
        result.ErrorMessageNode = FindDirectTableChild(node, "error-message");
        result.HasErrorMessage = result.ErrorMessageNode != null;
        if (result.ErrorMessageNode != null)
        {
            result.DisplayError = ReadBooleanAttribute(
                result.ErrorMessageNode,
                "display",
                false);
            result.ErrorTitle = ReadAttribute(
                result.ErrorMessageNode,
                OdfNamespaces.Table,
                "title");
            result.ErrorText = ExtractMessageText(result.ErrorMessageNode);
            result.ErrorMessageType = ReadAttribute(
                result.ErrorMessageNode,
                OdfNamespaces.Table,
                "message-type");
            if (result.ErrorMessageType.Length == 0)
            {
                result.ErrorMessageType = "stop";
            }
        }
        SnapshotValidation(result);
        return result;
    }

    static OdfConditionalStyle CreateConditionalStyle(
        OdfNode node,
        string partPath,
        OdfNode? automaticStyles)
    {
        OdfConditionalStyle result = new OdfConditionalStyle();
        OdfNode owner = node.Parent ?? new OdfNode();
        result.OwnerStyleName = ReadAttribute(
            owner,
            OdfNamespaces.Style,
            "name");
        result.OwnerStyleFamily = ReadAttribute(
            owner,
            OdfNamespaces.Style,
            "family");
        result.PartPath = partPath;
        result.IsAutomatic = IsDescendantOf(owner, automaticStyles);
        result.Condition = ReadAttribute(
            node,
            OdfNamespaces.Style,
            "condition");
        result.ApplyStyleName = ReadAttribute(
            node,
            OdfNamespaces.Style,
            "apply-style-name");
        result.BaseCellAddress = ReadAttribute(
            node,
            OdfNamespaces.Style,
            "base-cell-address");
        result.Node = node;
        result.OriginalNode = node;
        result.OriginalOwnerNode = owner;
        return result;
    }

    static void SynchronizeValidations(
        OdfContentValidation[] validations,
        OdfNode? spreadsheet)
    {
        OdfNode? container = FindDirectTableChild(
            spreadsheet,
            "content-validations");
        int index = 0;
        while (index < validations.Length)
        {
            OdfContentValidation validation = validations[index];
            if (validation == null || validation.Name.Length == 0)
            {
                throw new OdfDocumentException(
                    "Each content validation requires a non-empty name.");
            }
            bool isNew = validation.OriginalNode == null;
            OdfNode? node = validation.OriginalNode;
            if (node == null)
            {
                if (spreadsheet == null)
                {
                    throw new OdfDocumentException(
                        "A spreadsheet body is required to add content validation.");
                }
                if (container == null)
                {
                    container = CreateTableElement(
                        "content-validations",
                        spreadsheet.Location.PartPath);
                    InsertValidationContainer(spreadsheet, container);
                }
                node = CreateTableElement(
                    "content-validation",
                    container.Location.PartPath);
                AppendChild(container, node);
                validation.OriginalNode = node;
            }
            WriteValidationAttributes(validation, node, isNew);
            SynchronizeValidationMessages(validation, node, isNew);
            validation.Node = node;
            SnapshotValidation(validation);
            index = index + 1;
        }
        if (container != null && container.Children.Length == 0)
        {
            RemoveNode(container);
        }
    }

    static void WriteValidationAttributes(
        OdfContentValidation validation,
        OdfNode node,
        bool isNew)
    {
        WriteRequiredTableAttribute(node, "name", validation.Name);
        WriteChangedOptionalTableAttribute(
            node,
            "condition",
            validation.Condition,
            validation.OriginalCondition,
            isNew);
        WriteChangedOptionalTableAttribute(
            node,
            "base-cell-address",
            validation.BaseCellAddress,
            validation.OriginalBaseCellAddress,
            isNew);
        WriteChangedBooleanTableAttribute(
            node,
            "allow-empty-cell",
            validation.AllowEmptyCell,
            validation.OriginalAllowEmptyCell,
            isNew,
            true);
        WriteChangedTokenTableAttribute(
            node,
            "display-list",
            validation.DisplayList,
            validation.OriginalDisplayList,
            isNew,
            "none");
    }

    static void SynchronizeValidationMessages(
        OdfContentValidation validation,
        OdfNode node,
        bool isNew)
    {
        OdfNode? help = validation.HelpMessageNode;
        if (!validation.HasHelpMessage)
        {
            if (help != null)
            {
                RemoveNode(help);
            }
            validation.HelpMessageNode = null;
        }
        else
        {
            if (help == null)
            {
                help = CreateTableElement(
                    "help-message",
                    node.Location.PartPath);
                InsertMessageChild(node, help, true);
                validation.HelpMessageNode = help;
            }
            WriteMessage(
                help,
                validation.HelpTitle,
                validation.OriginalHelpTitle,
                validation.DisplayHelp,
                validation.OriginalDisplayHelp,
                validation.HelpText,
                validation.OriginalHelpText,
                isNew || !validation.OriginalHasHelpMessage,
                "",
                "");
        }
        OdfNode? error = validation.ErrorMessageNode;
        if (!validation.HasErrorMessage)
        {
            if (error != null)
            {
                RemoveNode(error);
            }
            validation.ErrorMessageNode = null;
        }
        else
        {
            if (error == null)
            {
                error = CreateTableElement(
                    "error-message",
                    node.Location.PartPath);
                InsertMessageChild(node, error, false);
                validation.ErrorMessageNode = error;
            }
            WriteMessage(
                error,
                validation.ErrorTitle,
                validation.OriginalErrorTitle,
                validation.DisplayError,
                validation.OriginalDisplayError,
                validation.ErrorText,
                validation.OriginalErrorText,
                isNew || !validation.OriginalHasErrorMessage,
                validation.ErrorMessageType,
                validation.OriginalErrorMessageType);
        }
    }

    static void WriteMessage(
        OdfNode node,
        string title,
        string originalTitle,
        bool display,
        bool originalDisplay,
        string message,
        string originalMessage,
        bool isNew,
        string messageType,
        string originalMessageType)
    {
        WriteChangedOptionalTableAttribute(
            node,
            "title",
            title,
            originalTitle,
            isNew);
        WriteChangedBooleanTableAttribute(
            node,
            "display",
            display,
            originalDisplay,
            isNew,
            false);
        if (messageType.Length > 0)
        {
            WriteChangedTokenTableAttribute(
                node,
                "message-type",
                messageType,
                originalMessageType,
                isNew,
                "stop");
        }
        if (isNew || message != originalMessage)
        {
            ReplaceMessageText(node, message);
        }
    }

    static void SynchronizeConditionalStyles(OdfDocument document)
    {
        int index = 0;
        while (index < document.ConditionalStyles.Length)
        {
            OdfConditionalStyle conditional =
                document.ConditionalStyles[index];
            if (conditional == null
                || conditional.Condition.Length == 0
                || conditional.ApplyStyleName.Length == 0)
            {
                throw new OdfDocumentException(
                    "Each conditional style requires a condition and applied style name.");
            }
            OdfStyle? owner = FindOwnerStyle(document, conditional);
            if (owner == null || owner.Node == null)
            {
                throw new OdfDocumentException(
                    $"The owning style '{conditional.OwnerStyleName}' in family '{conditional.OwnerStyleFamily}' could not be resolved.");
            }
            OdfNode? node = conditional.OriginalNode;
            if (node == null)
            {
                node = CreateStyleElement("map", owner.Node.Location.PartPath);
                AppendChild(owner.Node, node);
                conditional.OriginalNode = node;
            }
            else if (node.Parent != owner.Node)
            {
                RemoveNode(node);
                AppendChild(owner.Node, node);
            }
            WriteRequiredStyleAttribute(
                node,
                "condition",
                conditional.Condition);
            WriteRequiredStyleAttribute(
                node,
                "apply-style-name",
                conditional.ApplyStyleName);
            WriteOptionalStyleAttribute(
                node,
                "base-cell-address",
                conditional.BaseCellAddress);
            conditional.OwnerStyle = owner;
            conditional.Node = node;
            conditional.OriginalOwnerNode = owner.Node;
            conditional.OwnerStyleName = owner.Name;
            conditional.OwnerStyleFamily = owner.FamilyName;
            conditional.PartPath = owner.PartPath;
            conditional.IsAutomatic = owner.IsAutomatic;
            index = index + 1;
        }
    }

    static void ResolveConditionalStyles(OdfDocument document)
    {
        int index = 0;
        while (index < document.ConditionalStyles.Length)
        {
            OdfConditionalStyle conditional = document.ConditionalStyles[index];
            conditional.OwnerStyle = FindOwnerStyle(document, conditional);
            conditional.ApplyStyle = FindAppliedStyle(
                document,
                conditional.ApplyStyleName,
                conditional.OwnerStyleFamily,
                conditional.PartPath);
            index = index + 1;
        }
    }

    static OdfStyle? FindOwnerStyle(
        OdfDocument document,
        OdfConditionalStyle conditional)
    {
        OdfStyle? result = null;
        int matches = 0;
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            bool nodeMatch = conditional.OriginalOwnerNode != null
                && style.Node == conditional.OriginalOwnerNode;
            bool partMatch = document.PackageKind == OdfPackageKind.Flat
                || style.PartPath == conditional.PartPath;
            bool fieldMatch = style.Name == conditional.OwnerStyleName
                && style.FamilyName == conditional.OwnerStyleFamily
                && partMatch
                && style.IsAutomatic == conditional.IsAutomatic;
            if (nodeMatch || fieldMatch)
            {
                result = style;
                matches = matches + 1;
            }
            index = index + 1;
        }
        return matches == 1 ? result : null;
    }

    static OdfStyle? FindAppliedStyle(
        OdfDocument document,
        string name,
        string family,
        string partPath)
    {
        OdfStyle? common = null;
        OdfStyle? samePart = null;
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (style.Name == name && style.FamilyName == family)
            {
                if (document.PackageKind == OdfPackageKind.Flat
                    || style.PartPath == partPath)
                {
                    samePart = style;
                }
                if (style.IsCommon)
                {
                    common = style;
                }
            }
            index = index + 1;
        }
        return samePart ?? common;
    }

    static void RemoveDeletedValidations(
        OdfContentValidation[] originals,
        OdfContentValidation[] current)
    {
        int index = 0;
        while (index < originals.Length)
        {
            OdfNode? node = originals[index].OriginalNode;
            if (node != null && !ContainsValidationNode(current, node))
            {
                RemoveNode(node);
            }
            index = index + 1;
        }
    }

    static bool ContainsValidationNode(
        OdfContentValidation[] validations,
        OdfNode node)
    {
        int index = 0;
        while (index < validations.Length)
        {
            OdfContentValidation validation = validations[index];
            if (validation != null && validation.OriginalNode == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void RemoveDeletedConditionalStyles(
        OdfConditionalStyle[] originals,
        OdfConditionalStyle[] current)
    {
        int index = 0;
        while (index < originals.Length)
        {
            OdfNode? node = originals[index].OriginalNode;
            if (node != null && !ContainsConditionalNode(current, node))
            {
                RemoveNode(node);
            }
            index = index + 1;
        }
    }

    static bool ContainsConditionalNode(
        OdfConditionalStyle[] conditionals,
        OdfNode node)
    {
        int index = 0;
        while (index < conditionals.Length)
        {
            OdfConditionalStyle conditional = conditionals[index];
            if (conditional != null && conditional.OriginalNode == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void SnapshotValidation(OdfContentValidation validation)
    {
        validation.OriginalName = validation.Name;
        validation.OriginalCondition = validation.Condition;
        validation.OriginalBaseCellAddress = validation.BaseCellAddress;
        validation.OriginalAllowEmptyCell = validation.AllowEmptyCell;
        validation.OriginalDisplayList = validation.DisplayList;
        validation.OriginalHasHelpMessage = validation.HasHelpMessage;
        validation.OriginalDisplayHelp = validation.DisplayHelp;
        validation.OriginalHelpTitle = validation.HelpTitle;
        validation.OriginalHelpText = validation.HelpText;
        validation.OriginalHasErrorMessage = validation.HasErrorMessage;
        validation.OriginalDisplayError = validation.DisplayError;
        validation.OriginalErrorTitle = validation.ErrorTitle;
        validation.OriginalErrorText = validation.ErrorText;
        validation.OriginalErrorMessageType = validation.ErrorMessageType;
    }

    static string ExtractMessageText(OdfNode node)
    {
        StringBuilder result = new StringBuilder();
        int paragraphs = 0;
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (child.Kind == OdfNodeKind.Element
                && child.Name != null
                && child.Name.NamespaceName == OdfNamespaces.Text
                && child.Name.LocalName == "p")
            {
                if (paragraphs > 0)
                {
                    result.Append('\n');
                }
                AppendText(result, child);
                paragraphs = paragraphs + 1;
            }
            index = index + 1;
        }
        return result.ToString();
    }

    static void AppendText(StringBuilder result, OdfNode node)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Append(node.Value);
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AppendText(result, node.Children[index]);
            index = index + 1;
        }
    }

    static void ReplaceMessageText(OdfNode messageNode, string value)
    {
        int index = 0;
        while (index < messageNode.Children.Length)
        {
            messageNode.Children[index].Parent = null;
            index = index + 1;
        }
        if (value.Length == 0)
        {
            messageNode.Children = [];
            messageNode.WasEmptyElement = true;
            return;
        }
        OdfNode paragraph = CreateTextElement(
            "p",
            messageNode.Location.PartPath);
        OdfNode text = new OdfNode();
        text.Kind = OdfNodeKind.Text;
        text.Value = value;
        text.Location.PartPath = messageNode.Location.PartPath;
        text.Parent = paragraph;
        paragraph.Children = [text];
        paragraph.WasEmptyElement = false;
        paragraph.Parent = messageNode;
        messageNode.Children = [paragraph];
        messageNode.WasEmptyElement = false;
    }

    static void InsertValidationContainer(OdfNode spreadsheet, OdfNode container)
    {
        int insertion = spreadsheet.Children.Length;
        int index = 0;
        while (index < spreadsheet.Children.Length)
        {
            OdfNode child = spreadsheet.Children[index];
            if (IsTableElement(child, "label-ranges")
                || IsTableElement(child, "table")
                || IsTableElement(child, "named-expressions")
                || IsTableElement(child, "database-ranges")
                || IsTableElement(child, "consolidation"))
            {
                insertion = index;
                index = spreadsheet.Children.Length;
            }
            else
            {
                index = index + 1;
            }
        }
        InsertChildAt(spreadsheet, container, insertion);
    }

    static void InsertMessageChild(
        OdfNode validation,
        OdfNode message,
        bool isHelp)
    {
        int insertion = validation.Children.Length;
        if (isHelp)
        {
            int index = 0;
            while (index < validation.Children.Length)
            {
                if (IsTableElement(validation.Children[index], "error-message")
                    || IsTableElement(validation.Children[index], "error-macro"))
                {
                    insertion = index;
                    index = validation.Children.Length;
                }
                else
                {
                    index = index + 1;
                }
            }
        }
        InsertChildAt(validation, message, insertion);
    }

    static void InsertChildAt(OdfNode parent, OdfNode child, int insertion)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        Array.Copy(parent.Children, 0, children, 0, insertion);
        children[insertion] = child;
        Array.Copy(
            parent.Children,
            insertion,
            children,
            insertion + 1,
            parent.Children.Length - insertion);
        parent.Children = children;
        child.Parent = parent;
        child.Location.PartPath = parent.Location.PartPath;
        parent.WasEmptyElement = false;
    }

    static void AppendChild(OdfNode parent, OdfNode child)
    {
        InsertChildAt(parent, child, parent.Children.Length);
    }

    static void RemoveNode(OdfNode node)
    {
        OdfNode? parent = node.Parent;
        if (parent == null)
        {
            return;
        }
        int found = -1;
        int index = 0;
        while (index < parent.Children.Length)
        {
            if (parent.Children[index] == node)
            {
                found = index;
                index = parent.Children.Length;
            }
            else
            {
                index = index + 1;
            }
        }
        if (found < 0)
        {
            return;
        }
        OdfNode[] children = new OdfNode[parent.Children.Length - 1];
        Array.Copy(parent.Children, 0, children, 0, found);
        Array.Copy(
            parent.Children,
            found + 1,
            children,
            found,
            parent.Children.Length - found - 1);
        parent.Children = children;
        parent.WasEmptyElement = children.Length == 0;
        node.Parent = null;
    }

    static void WriteRequiredTableAttribute(
        OdfNode node,
        string localName,
        string value)
    {
        StyleDocumentModel.WriteAttribute(
            node,
            OdfNamespaces.Table,
            localName,
            OdfNamespaces.TablePrefix,
            value);
    }

    static void WriteChangedOptionalTableAttribute(
        OdfNode node,
        string localName,
        string value,
        string original,
        bool isNew)
    {
        if (!isNew && value == original)
        {
            return;
        }
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                OdfNamespaces.Table,
                localName);
        }
        else
        {
            WriteRequiredTableAttribute(node, localName, value);
        }
    }

    static void WriteChangedBooleanTableAttribute(
        OdfNode node,
        string localName,
        bool value,
        bool original,
        bool isNew,
        bool defaultValue)
    {
        if (!isNew && value == original)
        {
            return;
        }
        if (isNew && value == defaultValue)
        {
            return;
        }
        WriteRequiredTableAttribute(node, localName, value ? "true" : "false");
    }

    static void WriteChangedTokenTableAttribute(
        OdfNode node,
        string localName,
        string value,
        string original,
        bool isNew,
        string defaultValue)
    {
        if (!isNew && value == original)
        {
            return;
        }
        if (value.Length == 0 || isNew && value == defaultValue)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                OdfNamespaces.Table,
                localName);
        }
        else
        {
            WriteRequiredTableAttribute(node, localName, value);
        }
    }

    static void WriteRequiredStyleAttribute(
        OdfNode node,
        string localName,
        string value)
    {
        StyleDocumentModel.WriteAttribute(
            node,
            OdfNamespaces.Style,
            localName,
            OdfNamespaces.StylePrefix,
            value);
    }

    static void WriteOptionalStyleAttribute(
        OdfNode node,
        string localName,
        string value)
    {
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                OdfNamespaces.Style,
                localName);
        }
        else
        {
            WriteRequiredStyleAttribute(node, localName, value);
        }
    }

    static string ReadAttribute(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return StyleDocumentModel.ReadAttribute(
            node,
            namespaceName,
            localName);
    }

    static bool ReadBooleanAttribute(
        OdfNode node,
        string localName,
        bool defaultValue)
    {
        string value = ReadAttribute(node, OdfNamespaces.Table, localName);
        if (value == "true" || value == "1")
        {
            return true;
        }
        if (value == "false" || value == "0")
        {
            return false;
        }
        return defaultValue;
    }

    static OdfNode? FindDirectTableChild(OdfNode? parent, string localName)
    {
        if (parent == null)
        {
            return null;
        }
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode child = parent.Children[index];
            if (IsTableElement(child, localName))
            {
                return child;
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsDescendantOf(OdfNode node, OdfNode? ancestor)
    {
        if (ancestor == null)
        {
            return false;
        }
        OdfNode? current = node;
        while (current != null)
        {
            if (current == ancestor)
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    static bool IsOrdinaryStyle(OdfNode node)
    {
        return IsStyleElement(node, "style")
            || IsStyleElement(node, "default-style");
    }

    static bool IsOfficeElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Office
            && node.Name.LocalName == localName;
    }

    static bool IsTableElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Table
            && node.Name.LocalName == localName;
    }

    static bool IsStyleElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Style
            && node.Name.LocalName == localName;
    }

    static OdfNode CreateTableElement(string localName, string partPath)
    {
        return CreateElement(
            OdfNamespaces.Table,
            localName,
            OdfNamespaces.TablePrefix,
            partPath);
    }

    static OdfNode CreateStyleElement(string localName, string partPath)
    {
        return CreateElement(
            OdfNamespaces.Style,
            localName,
            OdfNamespaces.StylePrefix,
            partPath);
    }

    static OdfNode CreateTextElement(string localName, string partPath)
    {
        return CreateElement(
            OdfNamespaces.Text,
            localName,
            OdfNamespaces.TextPrefix,
            partPath);
    }

    static OdfNode CreateElement(
        string namespaceName,
        string localName,
        string prefix,
        string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            namespaceName,
            localName,
            prefix);
        result.Location.PartPath = partPath;
        result.WasEmptyElement = true;
        return result;
    }
}
