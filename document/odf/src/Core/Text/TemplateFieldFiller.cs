using System;
using System.Collections.Generic;
using System.Text;

namespace odf;

internal class TemplateRunOwner
{
    internal OdfTextBlock? Block;
    internal OdfTextRun? Run;
    internal int Index = -1;
}

internal class TemplateRunPosition
{
    internal int Offset;
    internal int MatchOffset = -1;
}

internal class TemplateTextState
{
    internal bool Assigned;
    internal string Value = "";
}

internal static class TemplateFieldFiller
{
    internal static OdfEditResult FillPlaceholders(
        OdfDocument document,
        string key,
        string value,
        bool retainFields)
    {
        RequireDocumentAndValue(document, value);
        if (key == null || key.Length == 0)
        {
            throw new OdfEditException(
                "A placeholder key cannot be empty.");
        }
        List<OdfTextField> matches = new List<OdfTextField>();
        int index = 0;
        while (index < document.TextFields.Length)
        {
            OdfTextField field = document.TextFields[index];
            if (field.Kind == OdfTextFieldKind.Placeholder
                && (field.Name == key
                    || field.Description == key
                    || field.DisplayText == key))
            {
                matches.Add(field);
            }
            index = index + 1;
        }
        return FillMatchedFields(
            document,
            matches,
            value,
            retainFields,
            "placeholder");
    }

    internal static OdfEditResult FillUserField(
        OdfDocument document,
        string name,
        string value,
        bool retainFields)
    {
        RequireDocumentAndValue(document, value);
        if (name == null || name.Length == 0)
        {
            throw new OdfEditException(
                "A user-field name cannot be empty.");
        }
        List<OdfTextField> matches = new List<OdfTextField>();
        int index = 0;
        while (index < document.TextFields.Length)
        {
            OdfTextField field = document.TextFields[index];
            if (field.Kind == OdfTextFieldKind.UserField
                && field.Name == name)
            {
                matches.Add(field);
            }
            index = index + 1;
        }
        bool declarationChanged = retainFields
            && UpdateUserDeclarations(
                document,
                name,
                value);
        OdfEditResult result = FillMatchedFields(
            document,
            matches,
            value,
            retainFields,
            "user field");
        if (!retainFields)
        {
            declarationChanged = RemoveUnusedUserDeclarations(
                document,
                name);
        }
        if (declarationChanged && !result.Changed)
        {
            result.Status = OdfEditStatus.Success;
            result.Changed = true;
            result.AffectedCount = 1;
            result.Message = "Filled 1 user-field declaration.";
        }
        TextFieldDocumentModel.ResolveDeclarations(document);
        return result;
    }

    static OdfEditResult FillMatchedFields(
        OdfDocument document,
        List<OdfTextField> fields,
        string value,
        bool retainFields,
        string description)
    {
        List<OdfSearchMatch> searchMatches = new List<OdfSearchMatch>();
        bool changed = false;
        int affected = 0;
        int index = 0;
        while (index < fields.Count)
        {
            OdfTextField field = fields[index];
            searchMatches.Add(CreateMatch(field));
            if (!retainFields || field.DisplayText != value)
            {
                FillFieldContent(document, field, value);
                changed = true;
                affected = affected + 1;
            }
            index = index + 1;
        }
        if (!retainFields && fields.Count > 0)
        {
            RemoveFilledFields(document, fields);
        }
        OdfEditResult result = new OdfEditResult();
        result.Status = changed
            ? OdfEditStatus.Success
            : OdfEditStatus.NoChange;
        result.Changed = changed;
        result.AffectedCount = affected;
        result.Message = changed
            ? $"Filled {affected} {description} occurrence(s)."
            : $"No matching {description} required filling.";
        result.SearchMatches = searchMatches.ToArray();
        if (result.SearchMatches.Length > 0)
        {
            result.Location = result.SearchMatches[0].Location;
        }
        return result;
    }

    static void FillFieldContent(
        OdfDocument document,
        OdfTextField field,
        string value)
    {
        OdfTextBlock? block = field.TextBlock;
        OdfTextRun? fieldRun = field.Run;
        if (block != null && fieldRun != null)
        {
            TemplateRunPosition position = FindRunPosition(
                block.Runs,
                fieldRun);
            if (position.MatchOffset >= 0)
            {
                TextEditingModel.TrackTextReplacement(
                    document,
                    block,
                    position.MatchOffset,
                    fieldRun.Text.Length,
                    value.Length);
            }
            ReplaceFormattedRunText(fieldRun, value);
            block.Text = RefreshRunTexts(block.Runs);
            block.OriginalText = block.Text;
        }
        else if (field.Node != null)
        {
            ReplaceFormattedNodeText(field.Node, value);
        }
        field.DisplayText = value;
        field.OriginalDisplayText = value;
    }

    static void ReplaceFormattedRunText(OdfTextRun run, string value)
    {
        TemplateTextState state = new TemplateTextState();
        state.Value = value;
        ReplaceRunLeaves(run.Runs, state);
        if (!state.Assigned)
        {
            OdfTextRun text = new OdfTextRun();
            text.Kind = OdfTextRunKind.Text;
            text.Text = value;
            run.Runs = [text];
            state.Assigned = true;
        }
        run.Text = RefreshRunTexts(run.Runs);
    }

    static void ReplaceRunLeaves(
        OdfTextRun[] runs,
        TemplateTextState state)
    {
        int index = 0;
        while (index < runs.Length)
        {
            OdfTextRun run = runs[index];
            if (run.Runs.Length > 0)
            {
                ReplaceRunLeaves(run.Runs, state);
                run.Text = RefreshRunTexts(run.Runs);
            }
            else if (run.Text.Length > 0
                || run.Kind == OdfTextRunKind.Text)
            {
                run.Kind = OdfTextRunKind.Text;
                run.Node = null;
                run.Text = state.Assigned ? "" : state.Value;
                state.Assigned = true;
            }
            index = index + 1;
        }
    }

    static string RefreshRunTexts(OdfTextRun[] runs)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < runs.Length)
        {
            OdfTextRun run = runs[index];
            if (run.Runs.Length > 0)
            {
                run.Text = RefreshRunTexts(run.Runs);
            }
            result.Append(run.Text);
            index = index + 1;
        }
        return result.ToString();
    }

    static TemplateRunPosition FindRunPosition(
        OdfTextRun[] runs,
        OdfTextRun target)
    {
        TemplateRunPosition result = new TemplateRunPosition();
        FindRunPositionInTree(runs, target, result);
        return result;
    }

    static bool FindRunPositionInTree(
        OdfTextRun[] runs,
        OdfTextRun target,
        TemplateRunPosition position)
    {
        int index = 0;
        while (index < runs.Length)
        {
            OdfTextRun run = runs[index];
            if (run == target)
            {
                position.MatchOffset = position.Offset;
                return true;
            }
            if (run.Runs.Length > 0)
            {
                if (FindRunPositionInTree(run.Runs, target, position))
                {
                    return true;
                }
            }
            else
            {
                position.Offset = position.Offset + run.Text.Length;
            }
            index = index + 1;
        }
        return false;
    }

    static void ReplaceFormattedNodeText(OdfNode node, string value)
    {
        TemplateTextState state = new TemplateTextState();
        state.Value = value;
        ReplaceNodeLeaves(node.Children, state);
        if (!state.Assigned)
        {
            OdfNode text = new OdfNode();
            text.Kind = OdfNodeKind.Text;
            text.Value = value;
            text.Parent = node;
            text.Location.PartPath = node.Location.PartPath;
            node.Children = [text];
        }
    }

    static void ReplaceNodeLeaves(
        OdfNode[] nodes,
        TemplateTextState state)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            OdfNode node = nodes[index];
            if (node.Kind == OdfNodeKind.Text)
            {
                node.Value = state.Assigned ? "" : state.Value;
                state.Assigned = true;
            }
            else
            {
                ReplaceNodeLeaves(node.Children, state);
            }
            index = index + 1;
        }
    }

    static void RemoveFilledFields(
        OdfDocument document,
        List<OdfTextField> fields)
    {
        int index = 0;
        while (index < fields.Count)
        {
            OdfTextField field = fields[index];
            if (field.TextBlock != null && field.Run != null)
            {
                UnwrapFieldRun(field.TextBlock, field);
            }
            else
            {
                UnwrapFieldNode(field);
            }
            index = index + 1;
        }
        List<OdfTextField> retained = new List<OdfTextField>();
        index = 0;
        while (index < document.TextFields.Length)
        {
            if (!ContainsField(fields, document.TextFields[index]))
            {
                retained.Add(document.TextFields[index]);
            }
            index = index + 1;
        }
        document.TextFields = retained.ToArray();
    }

    static void UnwrapFieldRun(
        OdfTextBlock block,
        OdfTextField field)
    {
        OdfTextRun target = field.Run ?? new OdfTextRun();
        TemplateRunOwner? owner = FindRunOwner(block, target);
        if (owner == null)
        {
            return;
        }
        OdfTextRun[] replacements = target.Runs;
        if (field.TextStyleName.Length > 0)
        {
            OdfTextRun span = new OdfTextRun();
            span.Kind = OdfTextRunKind.Span;
            span.StyleName = field.TextStyleName;
            span.Runs = target.Runs;
            span.Text = RefreshRunTexts(span.Runs);
            replacements = [span];
        }
        OdfTextRun[] source = owner.Block == null
            ? (owner.Run ?? new OdfTextRun()).Runs
            : owner.Block.Runs;
        OdfTextRun[] changed = ReplaceRunAt(
            source,
            owner.Index,
            replacements);
        if (owner.Block != null)
        {
            owner.Block.Runs = changed;
        }
        else if (owner.Run != null)
        {
            owner.Run.Runs = changed;
        }
        block.Text = RefreshRunTexts(block.Runs);
        block.OriginalText = block.Text;
    }

    static TemplateRunOwner? FindRunOwner(
        OdfTextBlock block,
        OdfTextRun target)
    {
        int index = 0;
        while (index < block.Runs.Length)
        {
            if (block.Runs[index] == target)
            {
                TemplateRunOwner result = new TemplateRunOwner();
                result.Block = block;
                result.Index = index;
                return result;
            }
            TemplateRunOwner? nested = FindNestedRunOwner(
                block.Runs[index],
                target);
            if (nested != null)
            {
                return nested;
            }
            index = index + 1;
        }
        return null;
    }

    static TemplateRunOwner? FindNestedRunOwner(
        OdfTextRun parent,
        OdfTextRun target)
    {
        int index = 0;
        while (index < parent.Runs.Length)
        {
            if (parent.Runs[index] == target)
            {
                TemplateRunOwner result = new TemplateRunOwner();
                result.Run = parent;
                result.Index = index;
                return result;
            }
            TemplateRunOwner? nested = FindNestedRunOwner(
                parent.Runs[index],
                target);
            if (nested != null)
            {
                return nested;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfTextRun[] ReplaceRunAt(
        OdfTextRun[] source,
        int index,
        OdfTextRun[] replacements)
    {
        OdfTextRun[] result = new OdfTextRun[
            source.Length - 1 + replacements.Length];
        Array.Copy(source, 0, result, 0, index);
        Array.Copy(replacements, 0, result, index, replacements.Length);
        Array.Copy(
            source,
            index + 1,
            result,
            index + replacements.Length,
            source.Length - index - 1);
        return result;
    }

    static void UnwrapFieldNode(OdfTextField field)
    {
        OdfNode? node = field.Node;
        if (node == null || node.Parent == null)
        {
            return;
        }
        OdfNode parent = node.Parent;
        OdfNode[] replacements = node.Children;
        if (field.TextStyleName.Length > 0)
        {
            OdfNode span = new OdfNode();
            span.Kind = OdfNodeKind.Element;
            span.Name = new OdfQualifiedName();
            span.Name.NamespaceName = OdfNamespaces.Text;
            span.Name.LocalName = "span";
            span.Name.Prefix = OdfNamespaces.TextPrefix;
            span.Location.PartPath = node.Location.PartPath;
            span.Children = node.Children;
            int childIndex = 0;
            while (childIndex < span.Children.Length)
            {
                span.Children[childIndex].Parent = span;
                childIndex = childIndex + 1;
            }
            StyleDocumentModel.WriteAttribute(
                span,
                OdfNamespaces.Text,
                "style-name",
                OdfNamespaces.TextPrefix,
                field.TextStyleName);
            replacements = [span];
        }
        int nodeIndex = FindNodeIndex(parent.Children, node);
        OdfNode[] changed = new OdfNode[
            parent.Children.Length - 1 + replacements.Length];
        Array.Copy(parent.Children, 0, changed, 0, nodeIndex);
        Array.Copy(replacements, 0, changed, nodeIndex, replacements.Length);
        Array.Copy(
            parent.Children,
            nodeIndex + 1,
            changed,
            nodeIndex + replacements.Length,
            parent.Children.Length - nodeIndex - 1);
        int index = 0;
        while (index < replacements.Length)
        {
            replacements[index].Parent = parent;
            index = index + 1;
        }
        parent.Children = changed;
        node.Parent = null;
    }

    static int FindNodeIndex(OdfNode[] nodes, OdfNode target)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index] == target)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static bool UpdateUserDeclarations(
        OdfDocument document,
        string name,
        string value)
    {
        bool changed = false;
        int index = 0;
        while (index < document.TextFieldDeclarations.Length)
        {
            OdfTextFieldDeclaration declaration =
                document.TextFieldDeclarations[index];
            if (declaration.Kind == OdfTextFieldDeclarationKind.UserField
                && declaration.Name == name)
            {
                if (declaration.ValueType != "string"
                    || declaration.StringValue != value)
                {
                    declaration.ValueType = "string";
                    declaration.StringValue = value;
                    declaration.Value = "";
                    declaration.Currency = "";
                    declaration.DateValue = "";
                    declaration.TimeValue = "";
                    declaration.BooleanValue = "";
                    changed = true;
                }
            }
            index = index + 1;
        }
        return changed;
    }

    static bool RemoveUnusedUserDeclarations(
        OdfDocument document,
        string name)
    {
        List<OdfTextFieldDeclaration> retained =
            new List<OdfTextFieldDeclaration>();
        bool changed = false;
        int index = 0;
        while (index < document.TextFieldDeclarations.Length)
        {
            OdfTextFieldDeclaration declaration =
                document.TextFieldDeclarations[index];
            bool remove = declaration.Kind
                    == OdfTextFieldDeclarationKind.UserField
                && declaration.Name == name
                && !HasUserField(document.TextFields, name);
            if (remove)
            {
                changed = true;
            }
            else
            {
                retained.Add(declaration);
            }
            index = index + 1;
        }
        document.TextFieldDeclarations = retained.ToArray();
        return changed;
    }

    static bool HasUserField(OdfTextField[] fields, string name)
    {
        int index = 0;
        while (index < fields.Length)
        {
            if (fields[index].Kind == OdfTextFieldKind.UserField
                && fields[index].Name == name)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ContainsField(
        List<OdfTextField> fields,
        OdfTextField target)
    {
        int index = 0;
        while (index < fields.Count)
        {
            if (fields[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfSearchMatch CreateMatch(OdfTextField field)
    {
        OdfSearchMatch result = new OdfSearchMatch();
        result.MatchedValue = field.DisplayText;
        result.Node = field.Node;
        result.IsExact = true;
        OdfLocation source = field.Node == null
            ? new OdfLocation()
            : field.Node.Location;
        OdfLocation location = new OdfLocation();
        location.PartPath = field.PartPath;
        location.NodePath = source.NodePath;
        location.XmlId = source.XmlId;
        location.Line = source.Line;
        location.Column = source.Column;
        location.ByteOffset = source.ByteOffset;
        if (field.TextBlock != null && field.Run != null)
        {
            TemplateRunPosition position = FindRunPosition(
                field.TextBlock.Runs,
                field.Run);
            location.TextOffset = position.MatchOffset;
            location.TextLength = field.Run.Text.Length;
            result.Context = field.TextBlock.Text;
        }
        result.Location = location;
        return result;
    }

    static void RequireDocumentAndValue(
        OdfDocument document,
        string value)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for template filling.");
        }
        if (value == null)
        {
            throw new OdfEditException("A template value cannot be null.");
        }
    }
}
