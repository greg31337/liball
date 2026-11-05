using System;
using System.Collections.Generic;

namespace odf;

internal static class StyleMaintenanceModel
{
    internal static OdfStyle CreateStyle(
        OdfDocument document,
        OdfStyle definition)
    {
        RequireDocumentAndStyle(document, definition);
        ValidateNewStyleDefinition(document, definition);
        OdfStyle result = CloneStyle(definition);
        if (result.IsDefault)
        {
            result.Name = "";
            result.IsNamed = false;
            result.IsAutomatic = false;
            result.IsCommon = true;
        }
        else
        {
            result.IsNamed = true;
            result.IsCommon = !result.IsAutomatic;
        }
        if (result.PartPath.Length == 0)
        {
            result.PartPath = result.IsAutomatic
                ? AutomaticPartPath(document, definition)
                : CommonPartPath(document);
        }
        AppendStyle(document, result);
        StyleResolver.ResolveRelationships(document);
        return result;
    }

    internal static OdfStyle ModifyStyle(
        OdfDocument document,
        OdfStyle style,
        OdfStyle replacement)
    {
        RequireDocumentAndStyle(document, style);
        if (replacement == null)
        {
            throw new OdfDocumentException(
                "A replacement style is required for style modification.");
        }
        if (!ContainsStyle(document, style))
        {
            throw new OdfEditException(
                "The style to modify does not belong to the document.");
        }
        if (!FamiliesMatch(style, replacement))
        {
            throw new OdfEditException(
                "A style modification cannot change the style family.");
        }
        if (style.IsDefault != replacement.IsDefault)
        {
            throw new OdfEditException(
                "A style modification cannot change whether a style is the default.");
        }
        OdfStyle changes = CloneStyle(replacement);
        if (!style.IsDefault && changes.Name != style.Name)
        {
            RenameStyle(document, style, changes.Name);
        }
        ApplyStyleChanges(style, changes);
        StyleResolver.ResolveRelationships(document);
        return style;
    }

    internal static OdfStyle SynthesizeAutomaticStyle(
        OdfDocument document,
        OdfStyle source,
        string preferredName)
    {
        RequireDocumentAndStyle(document, source);
        OdfStyle candidate = CloneStyle(source);
        candidate.Name = CreateUniqueStyleName(
            document,
            source.Family,
            source.FamilyName,
            preferredName.Length == 0 ? "Auto" : preferredName);
        candidate.DisplayName = "";
        candidate.PartPath = AutomaticPartPath(document, source);
        candidate.IsNamed = true;
        candidate.IsDefault = false;
        candidate.IsCommon = false;
        candidate.IsAutomatic = true;
        OdfStyle? equivalent = FindEquivalentAutomaticStyle(
            document,
            candidate);
        if (equivalent != null)
        {
            return equivalent;
        }
        AppendStyle(document, candidate);
        StyleResolver.ResolveRelationships(document);
        return candidate;
    }

    internal static void RenameStyle(
        OdfDocument document,
        OdfStyle style,
        string newName)
    {
        RequireDocumentAndStyle(document, style);
        if (!ContainsStyle(document, style))
        {
            throw new OdfEditException(
                "The style to rename does not belong to the document.");
        }
        if (!style.IsNamed || style.IsDefault)
        {
            throw new OdfEditException("Only a named style can be renamed.");
        }
        if (newName.Length == 0)
        {
            throw new OdfEditException("A style name cannot be empty.");
        }
        if (style.Name == newName)
        {
            return;
        }
        if (FindStyleByIdentity(
            document,
            newName,
            style.Family,
            style.FamilyName,
            style) != null)
        {
            throw new OdfEditException(
                "A style with the requested name and family already exists.");
        }
        string oldName = style.Name;
        UpdateTypedStyleReferences(document, style, oldName, newName);
        UpdateTreeStyleReferences(document, style, oldName, newName);
        style.Name = newName;
        StyleResolver.ResolveRelationships(document);
    }

    internal static OdfStyle[] BuildDependencyClosure(
        OdfDocument document,
        OdfStyle style)
    {
        RequireDocumentAndStyle(document, style);
        if (!ContainsStyle(document, style))
        {
            throw new OdfEditException(
                "The style dependency root does not belong to the document.");
        }
        List<OdfStyle> result = new List<OdfStyle>();
        AddDependencyStyle(document, style, result);
        return result.ToArray();
    }

    internal static OdfStyle CopyStyleAcrossDocuments(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        OdfStyle sourceStyle,
        string preferredName)
    {
        RequireDocumentAndStyle(sourceDocument, sourceStyle);
        if (targetDocument == null)
        {
            throw new OdfDocumentException(
                "A target document is required for style copying.");
        }
        OdfStyle[] closure = BuildDependencyClosure(sourceDocument, sourceStyle);
        OdfStyle[] copies = new OdfStyle[closure.Length];
        string[] sourceNames = new string[closure.Length];
        string[] targetNames = new string[closure.Length];
        int index = 0;
        while (index < closure.Length)
        {
            OdfStyle source = closure[index];
            OdfStyle copy = CloneStyle(source);
            string requested = source.Name;
            if (source == sourceStyle && preferredName.Length > 0)
            {
                requested = preferredName;
            }
            copy.Name = CreateUniqueCopyName(
                targetDocument,
                source.Family,
                source.FamilyName,
                requested.Length == 0 ? "Style" : requested,
                closure,
                targetNames,
                index);
            copy.PartPath = CopyPartPath(targetDocument, source);
            sourceNames[index] = source.Name;
            targetNames[index] = copy.Name;
            copies[index] = copy;
            index = index + 1;
        }
        index = 0;
        while (index < copies.Length)
        {
            RemapCopiedRelationships(
                copies[index],
                closure,
                sourceNames,
                targetNames);
            AppendStyle(targetDocument, copies[index]);
            index = index + 1;
        }
        StyleResolver.ResolveRelationships(targetDocument);
        return copies[0];
    }

    internal static OdfStyle[] DeduplicateAutomaticStyles(
        OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for style deduplication.");
        }
        List<OdfStyle> removed = new List<OdfStyle>();
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle canonical = document.Styles[index];
            if (canonical.IsAutomatic
                && canonical.IsNamed
                && !ListContainsStyle(removed, canonical))
            {
                int candidateIndex = index + 1;
                while (candidateIndex < document.Styles.Length)
                {
                    OdfStyle candidate = document.Styles[candidateIndex];
                    if (candidate.IsAutomatic
                        && candidate.IsNamed
                        && !ListContainsStyle(removed, candidate)
                        && StylesEquivalent(canonical, candidate))
                    {
                        UpdateTypedStyleReferences(
                            document,
                            candidate,
                            candidate.Name,
                            canonical.Name);
                        UpdateTreeStyleReferences(
                            document,
                            candidate,
                            candidate.Name,
                            canonical.Name);
                        removed.Add(candidate);
                    }
                    candidateIndex = candidateIndex + 1;
                }
            }
            index = index + 1;
        }
        if (removed.Count > 0)
        {
            document.Styles = RemoveStyles(document.Styles, removed);
            StyleResolver.ResolveRelationships(document);
        }
        return removed.ToArray();
    }

    internal static OdfStyle[] FindUnusedStyles(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for unused-style detection.");
        }
        List<OdfStyle> result = new List<OdfStyle>();
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (style.IsNamed && !StyleHasReference(document, style))
            {
                result.Add(style);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal static OdfStyle[] RemoveUnusedStyles(OdfDocument document)
    {
        OdfStyle[] unused = FindUnusedStyles(document);
        if (unused.Length > 0)
        {
            List<OdfStyle> removed = new List<OdfStyle>();
            int index = 0;
            while (index < unused.Length)
            {
                removed.Add(unused[index]);
                index = index + 1;
            }
            document.Styles = RemoveStyles(document.Styles, removed);
            StyleResolver.ResolveRelationships(document);
        }
        return unused;
    }

    static void RequireDocumentAndStyle(
        OdfDocument document,
        OdfStyle style)
    {
        if (document == null || style == null)
        {
            throw new OdfDocumentException(
                "A document and style are required for style maintenance.");
        }
    }

    static void ValidateNewStyleDefinition(
        OdfDocument document,
        OdfStyle definition)
    {
        if (definition.Family == OdfStyleFamily.Unknown
            && definition.FamilyName.Length == 0)
        {
            throw new OdfEditException("A style family is required.");
        }
        if (definition.IsDefault)
        {
            int index = 0;
            while (index < document.Styles.Length)
            {
                OdfStyle style = document.Styles[index];
                if (style.IsDefault && FamiliesMatch(style, definition))
                {
                    throw new OdfEditException(
                        "A default style already exists for the requested family.");
                }
                index = index + 1;
            }
            return;
        }
        if (definition.Name.Length == 0)
        {
            throw new OdfEditException("A named style requires a name.");
        }
        if (FindStyleByIdentity(
            document,
            definition.Name,
            definition.Family,
            definition.FamilyName,
            null) != null)
        {
            throw new OdfEditException(
                "A style with the requested name and family already exists.");
        }
    }

    static string CommonPartPath(OdfDocument document)
    {
        if (document.PackageKind == OdfPackageKind.Flat)
        {
            return "";
        }
        return OdfPartNames.Styles;
    }

    static bool ContainsStyle(OdfDocument document, OdfStyle target)
    {
        int index = 0;
        while (index < document.Styles.Length)
        {
            if (document.Styles[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfStyle? FindStyleByIdentity(
        OdfDocument document,
        string name,
        OdfStyleFamily family,
        string familyName,
        OdfStyle? excluded)
    {
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (style != excluded
                && style.IsNamed
                && style.Name == name
                && FamilyIdentityMatches(style, family, familyName))
            {
                return style;
            }
            index = index + 1;
        }
        return null;
    }

    static bool FamilyIdentityMatches(
        OdfStyle style,
        OdfStyleFamily family,
        string familyName)
    {
        if (style.Family != family)
        {
            return false;
        }
        return family != OdfStyleFamily.Unknown
            || style.FamilyName == familyName;
    }

    static bool FamiliesMatch(OdfStyle first, OdfStyle second)
    {
        return FamilyIdentityMatches(
            first,
            second.Family,
            second.FamilyName);
    }

    static string CreateUniqueStyleName(
        OdfDocument document,
        OdfStyleFamily family,
        string familyName,
        string requested)
    {
        if (FindStyleByIdentity(
            document,
            requested,
            family,
            familyName,
            null) == null)
        {
            return requested;
        }
        int suffix = 2;
        while (FindStyleByIdentity(
            document,
            requested + suffix,
            family,
            familyName,
            null) != null)
        {
            suffix = suffix + 1;
        }
        return requested + suffix;
    }

    static string CreateUniqueCopyName(
        OdfDocument document,
        OdfStyleFamily family,
        string familyName,
        string requested,
        OdfStyle[] sources,
        string[] assignedNames,
        int assignedCount)
    {
        string candidate = requested;
        int suffix = 2;
        while (FindStyleByIdentity(
            document,
            candidate,
            family,
            familyName,
            null) != null
            || CopyNameIsAssigned(
                candidate,
                family,
                familyName,
                sources,
                assignedNames,
                assignedCount))
        {
            candidate = requested + suffix;
            suffix = suffix + 1;
        }
        return candidate;
    }

    static bool CopyNameIsAssigned(
        string candidate,
        OdfStyleFamily family,
        string familyName,
        OdfStyle[] sources,
        string[] assignedNames,
        int assignedCount)
    {
        int index = 0;
        while (index < assignedCount)
        {
            if (assignedNames[index] == candidate
                && FamilyIdentityMatches(
                    sources[index],
                    family,
                    familyName))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static string AutomaticPartPath(
        OdfDocument document,
        OdfStyle source)
    {
        if (document.PackageKind == OdfPackageKind.Flat)
        {
            return "";
        }
        if (source.PartPath == OdfPartNames.Content
            || source.PartPath == OdfPartNames.Styles)
        {
            return source.PartPath;
        }
        return OdfPartNames.Content;
    }

    static string CopyPartPath(OdfDocument targetDocument, OdfStyle source)
    {
        if (targetDocument.PackageKind == OdfPackageKind.Flat)
        {
            return "";
        }
        if (source.IsAutomatic)
        {
            return OdfPartNames.Content;
        }
        return OdfPartNames.Styles;
    }

    static void AppendStyle(OdfDocument document, OdfStyle style)
    {
        OdfStyle[] styles = new OdfStyle[document.Styles.Length + 1];
        Array.Copy(document.Styles, styles, document.Styles.Length);
        styles[styles.Length - 1] = style;
        document.Styles = styles;
    }

    static void AddDependencyStyle(
        OdfDocument document,
        OdfStyle style,
        List<OdfStyle> result)
    {
        if (ListContainsStyle(result, style))
        {
            return;
        }
        result.Add(style);
        OdfStyle? parent = FindRelatedStyle(
            document,
            style,
            style.ParentStyleName);
        if (parent != null)
        {
            AddDependencyStyle(document, parent, result);
        }
        OdfStyle? next = FindRelatedStyle(
            document,
            style,
            style.NextStyleName);
        if (next != null)
        {
            AddDependencyStyle(document, next, result);
        }
    }

    static bool ListContainsStyle(List<OdfStyle> styles, OdfStyle target)
    {
        int index = 0;
        while (index < styles.Count)
        {
            if (styles[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfStyle? FindRelatedStyle(
        OdfDocument document,
        OdfStyle target,
        string name)
    {
        if (name.Length == 0)
        {
            return null;
        }
        OdfStyle? fallback = null;
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (style.Name == name && FamiliesMatch(style, target))
            {
                if (style.IsCommon)
                {
                    return style;
                }
                if (fallback == null)
                {
                    fallback = style;
                }
            }
            index = index + 1;
        }
        return fallback;
    }

    static void RemapCopiedRelationships(
        OdfStyle copy,
        OdfStyle[] sources,
        string[] sourceNames,
        string[] targetNames)
    {
        int index = 0;
        while (index < sources.Length)
        {
            if (FamiliesMatch(copy, sources[index]))
            {
                if (copy.ParentStyleName == sourceNames[index])
                {
                    copy.ParentStyleName = targetNames[index];
                }
                if (copy.NextStyleName == sourceNames[index])
                {
                    copy.NextStyleName = targetNames[index];
                }
            }
            index = index + 1;
        }
    }

    static void UpdateMasterPageDrawingStyles(
        OdfDocument document,
        string oldName,
        string newName)
    {
        int index = 0;
        while (index < document.MasterPages.Length)
        {
            OdfMasterPage masterPage = document.MasterPages[index];
            if (masterPage.DrawingStyleName == oldName)
            {
                masterPage.DrawingStyleName = newName;
            }
            index = index + 1;
        }
    }

    static void UpdateListLevelTextStyles(
        OdfDocument document,
        string oldName,
        string newName)
    {
        int styleIndex = 0;
        while (styleIndex < document.ListStyles.Length)
        {
            OdfListStyle listStyle = document.ListStyles[styleIndex];
            int levelIndex = 0;
            while (levelIndex < listStyle.Levels.Length)
            {
                OdfListLevel level = listStyle.Levels[levelIndex];
                if (level.StyleName == oldName)
                {
                    level.StyleName = newName;
                }
                levelIndex = levelIndex + 1;
            }
            styleIndex = styleIndex + 1;
        }
    }

    static void UpdateTypedStyleReferences(
        OdfDocument document,
        OdfStyle renamed,
        string oldName,
        string newName)
    {
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (FamiliesMatch(style, renamed))
            {
                if (style.ParentStyleName == oldName)
                {
                    style.ParentStyleName = newName;
                }
                if (style.NextStyleName == oldName)
                {
                    style.NextStyleName = newName;
                }
            }
            index = index + 1;
        }
        if (renamed.Family == OdfStyleFamily.Graphic)
        {
            UpdateMasterPageDrawingStyles(document, oldName, newName);
        }
        if (renamed.Family == OdfStyleFamily.Text)
        {
            UpdateListLevelTextStyles(document, oldName, newName);
        }
        if (renamed.Family == OdfStyleFamily.Chart)
        {
            ChartReferenceUpdateModel.RenameStyleReferences(
                document,
                oldName,
                newName);
        }
        UpdateTextFlowStyles(
            document,
            renamed.Family,
            oldName,
            newName);
        UpdateNoteConfigurationStyles(
            document,
            renamed.Family,
            oldName,
            newName);
        UpdateTextIndexStyles(
            document,
            renamed.Family,
            oldName,
            newName);
        if (renamed.Family == OdfStyleFamily.Paragraph
            || renamed.Family == OdfStyleFamily.Text
            || renamed.Family == OdfStyleFamily.Ruby)
        {
            UpdateTextContentStyles(document, renamed.Family, oldName, newName);
        }
    }

    static void UpdateNoteConfigurationStyles(
        OdfDocument document,
        OdfStyleFamily family,
        string oldName,
        string newName)
    {
        int index = 0;
        while (index < document.NoteConfigurations.Length)
        {
            OdfNoteConfiguration configuration =
                document.NoteConfigurations[index];
            if (family == OdfStyleFamily.Text)
            {
                if (configuration.CitationStyleName == oldName)
                {
                    configuration.CitationStyleName = newName;
                }
                if (configuration.CitationBodyStyleName == oldName)
                {
                    configuration.CitationBodyStyleName = newName;
                }
            }
            if (family == OdfStyleFamily.Paragraph
                && configuration.DefaultStyleName == oldName)
            {
                configuration.DefaultStyleName = newName;
            }
            index = index + 1;
        }
    }

    static void UpdateTextIndexStyles(
        OdfDocument document,
        OdfStyleFamily family,
        string oldName,
        string newName)
    {
        int index = 0;
        while (index < document.TextIndexes.Length)
        {
            OdfTextIndex item = document.TextIndexes[index];
            if (family == OdfStyleFamily.Section
                && item.StyleName == oldName)
            {
                item.StyleName = newName;
            }
            OdfTextIndexSource? source = item.Source;
            if (source != null)
            {
                if (family == OdfStyleFamily.Paragraph
                    && source.TitleStyleName == oldName)
                {
                    source.TitleStyleName = newName;
                }
                if (family == OdfStyleFamily.Text
                    && source.MainEntryStyleName == oldName)
                {
                    source.MainEntryStyleName = newName;
                }
                int templateIndex = 0;
                while (templateIndex < source.Templates.Length)
                {
                    OdfIndexEntryTemplate template =
                        source.Templates[templateIndex];
                    if (family == OdfStyleFamily.Paragraph
                        && template.StyleName == oldName)
                    {
                        template.StyleName = newName;
                    }
                    int componentIndex = 0;
                    while (componentIndex < template.Components.Length)
                    {
                        OdfIndexEntryComponent component =
                            template.Components[componentIndex];
                        if (family == OdfStyleFamily.Text
                            && component.StyleName == oldName)
                        {
                            component.StyleName = newName;
                        }
                        componentIndex = componentIndex + 1;
                    }
                    templateIndex = templateIndex + 1;
                }
                if (family == OdfStyleFamily.Paragraph)
                {
                    int groupIndex = 0;
                    while (groupIndex < source.SourceStyles.Length)
                    {
                        OdfIndexSourceStyle group =
                            source.SourceStyles[groupIndex];
                        int styleIndex = 0;
                        while (styleIndex < group.StyleNames.Length)
                        {
                            if (group.StyleNames[styleIndex] == oldName)
                            {
                                group.StyleNames[styleIndex] = newName;
                            }
                            styleIndex = styleIndex + 1;
                        }
                        groupIndex = groupIndex + 1;
                    }
                }
            }
            index = index + 1;
        }
    }

    static void UpdateTextFlowStyles(
        OdfDocument document,
        OdfStyleFamily family,
        string oldName,
        string newName)
    {
        int index = 0;
        while (index < document.TextSections.Length)
        {
            OdfTextSection section = document.TextSections[index];
            if (family == OdfStyleFamily.Section
                && section.StyleName == oldName)
            {
                section.StyleName = newName;
            }
            index = index + 1;
        }
        index = 0;
        while (index < document.TextFrames.Length)
        {
            OdfTextFrame frame = document.TextFrames[index];
            if (family == OdfStyleFamily.Graphic
                && frame.StyleName == oldName)
            {
                frame.StyleName = newName;
            }
            if (family == OdfStyleFamily.Paragraph
                && frame.TextStyleName == oldName)
            {
                frame.TextStyleName = newName;
            }
            index = index + 1;
        }
    }

    static void UpdateTextContentStyles(
        OdfDocument document,
        OdfStyleFamily family,
        string oldName,
        string newName)
    {
        int index = 0;
        while (index < document.TextBlocks.Length)
        {
            OdfTextBlock block = document.TextBlocks[index];
            if (family == OdfStyleFamily.Paragraph)
            {
                if (block.StyleName == oldName)
                {
                    block.StyleName = newName;
                }
                if (block.ConditionalStyleName == oldName)
                {
                    block.ConditionalStyleName = newName;
                }
            }
            else
            {
                UpdateInlineTextStyles(
                    block.Runs,
                    family,
                    oldName,
                    newName);
            }
            index = index + 1;
        }
        if (family == OdfStyleFamily.Text)
        {
            index = 0;
            while (index < document.TextFields.Length)
            {
                OdfTextField field = document.TextFields[index];
                if (field.TextStyleName == oldName)
                {
                    field.TextStyleName = newName;
                }
                index = index + 1;
            }
        }
    }

    static void UpdateInlineTextStyles(
        OdfTextRun[] runs,
        OdfStyleFamily family,
        string oldName,
        string newName)
    {
        int index = 0;
        while (index < runs.Length)
        {
            OdfTextRun run = runs[index];
            if (family == OdfStyleFamily.Ruby
                && run.Kind == OdfTextRunKind.Ruby
                && run.StyleName == oldName)
            {
                run.StyleName = newName;
            }
            if (family == OdfStyleFamily.Text
                && run.Kind != OdfTextRunKind.Ruby
                && run.StyleName == oldName)
            {
                run.StyleName = newName;
            }
            if (family == OdfStyleFamily.Text
                && run.VisitedStyleName == oldName)
            {
                run.VisitedStyleName = newName;
            }
            if (family == OdfStyleFamily.Text
                && run.RubyTextStyleName == oldName)
            {
                run.RubyTextStyleName = newName;
            }
            UpdateInlineTextStyles(
                run.Runs,
                family,
                oldName,
                newName);
            index = index + 1;
        }
    }

    static void UpdateTreeStyleReferences(
        OdfDocument document,
        OdfStyle style,
        string oldName,
        string newName)
    {
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfNode[] nodes = new OdfTreeEditor(
                document.Parts[partIndex].Nodes).TraverseAllNodes();
            int nodeIndex = 0;
            while (nodeIndex < nodes.Length)
            {
                OdfNode node = nodes[nodeIndex];
                if (!IsInsideStyleDefinition(node))
                {
                    int attributeIndex = 0;
                    while (attributeIndex < node.Attributes.Length)
                    {
                        OdfAttribute attribute = node.Attributes[attributeIndex];
                        if (attribute.Value == oldName
                            && AttributeReferencesFamily(node, attribute, style.Family))
                        {
                            attribute.Value = newName;
                        }
                        attributeIndex = attributeIndex + 1;
                    }
                }
                nodeIndex = nodeIndex + 1;
            }
            partIndex = partIndex + 1;
        }
    }

    static bool StyleHasReference(OdfDocument document, OdfStyle style)
    {
        int styleIndex = 0;
        while (styleIndex < document.Styles.Length)
        {
            OdfStyle candidate = document.Styles[styleIndex];
            if (candidate != style && FamiliesMatch(candidate, style)
                && (candidate.ParentStyleName == style.Name
                    || candidate.NextStyleName == style.Name))
            {
                return true;
            }
            styleIndex = styleIndex + 1;
        }
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfNode[] nodes = new OdfTreeEditor(
                document.Parts[partIndex].Nodes).TraverseAllNodes();
            int nodeIndex = 0;
            while (nodeIndex < nodes.Length)
            {
                OdfNode node = nodes[nodeIndex];
                if (!IsInsideStyleDefinition(node))
                {
                    int attributeIndex = 0;
                    while (attributeIndex < node.Attributes.Length)
                    {
                        OdfAttribute attribute = node.Attributes[attributeIndex];
                        if (attribute.Value == style.Name
                            && AttributeReferencesFamily(node, attribute, style.Family))
                        {
                            return true;
                        }
                        attributeIndex = attributeIndex + 1;
                    }
                }
                nodeIndex = nodeIndex + 1;
            }
            partIndex = partIndex + 1;
        }
        return false;
    }

    static bool IsInsideStyleDefinition(OdfNode node)
    {
        OdfNode? current = node;
        while (current != null)
        {
            if (current.Name != null
                && current.Name.NamespaceName == OdfNamespaces.Style
                && (current.Name.LocalName == "style"
                    || current.Name.LocalName == "default-style"))
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    static bool AttributeReferencesFamily(
        OdfNode node,
        OdfAttribute attribute,
        OdfStyleFamily family)
    {
        if (node.Name == null)
        {
            return false;
        }
        string elementNamespace = node.Name.NamespaceName;
        string elementName = node.Name.LocalName;
        string attributeNamespace = attribute.Name.NamespaceName;
        string attributeName = attribute.Name.LocalName;
        if (family == OdfStyleFamily.Paragraph)
        {
            return elementNamespace == OdfNamespaces.Text
                && elementName == "notes-configuration"
                && attributeNamespace == OdfNamespaces.Text
                && attributeName == "default-style-name"
                || elementNamespace == OdfNamespaces.Text
                && attributeNamespace == OdfNamespaces.Text
                && attributeName == "style-name"
                && (elementName == "index-title-template"
                    || elementName == "index-source-style"
                    || elementName.EndsWith("-entry-template"))
                || attributeNamespace == OdfNamespaces.Text
                && attributeName == "style-name"
                && (elementName == "p" || elementName == "h")
                || attributeNamespace == OdfNamespaces.Table
                && attributeName == "paragraph-style-name"
                || elementNamespace == OdfNamespaces.Drawing
                && elementName == "frame"
                && attributeNamespace == OdfNamespaces.Drawing
                && attributeName == "text-style-name";
        }
        if (family == OdfStyleFamily.Text)
        {
            return elementNamespace == OdfNamespaces.Text
                && elementName == "notes-configuration"
                && attributeNamespace == OdfNamespaces.Text
                && (attributeName == "citation-style-name"
                    || attributeName == "citation-body-style-name")
                || elementNamespace == OdfNamespaces.Text
                && attributeNamespace == OdfNamespaces.Text
                && (attributeName == "main-entry-style-name"
                    || attributeName == "style-name"
                    && elementName.StartsWith("index-entry-"))
                || attributeNamespace == OdfNamespaces.Text
                && attributeName == "style-name"
                && elementName != "p"
                && elementName != "h"
                && elementName != "section"
                && elementName != "list"
                && elementName != "numbered-paragraph"
                && elementName != "ruby";
        }
        if (family == OdfStyleFamily.Section)
        {
            return elementNamespace == OdfNamespaces.Text
                && (elementName == "section"
                    || elementName == "table-of-content"
                    || elementName == "alphabetical-index"
                    || elementName == "bibliography"
                    || elementName == "user-index"
                    || elementName == "table-index"
                    || elementName == "illustration-index"
                    || elementName == "object-index")
                && attributeNamespace == OdfNamespaces.Text
                && attributeName == "style-name";
        }
        if (family == OdfStyleFamily.Table)
        {
            return elementNamespace == OdfNamespaces.Table
                && elementName == "table"
                && attributeNamespace == OdfNamespaces.Table
                && attributeName == "style-name";
        }
        if (family == OdfStyleFamily.TableColumn)
        {
            return elementNamespace == OdfNamespaces.Table
                && (elementName == "table-column"
                    || elementName == "table-column-group")
                && attributeNamespace == OdfNamespaces.Table
                && attributeName == "style-name";
        }
        if (family == OdfStyleFamily.TableRow)
        {
            return elementNamespace == OdfNamespaces.Table
                && (elementName == "table-row"
                    || elementName == "table-row-group")
                && attributeNamespace == OdfNamespaces.Table
                && attributeName == "style-name";
        }
        if (family == OdfStyleFamily.TableCell)
        {
            return attributeNamespace == OdfNamespaces.Table
                && (attributeName == "style-name"
                    && (elementName == "table-cell"
                        || elementName == "covered-table-cell")
                    || attributeName == "default-cell-style-name");
        }
        if (family == OdfStyleFamily.Graphic)
        {
            return attributeNamespace == OdfNamespaces.Drawing
                && attributeName == "style-name"
                && elementName != "page";
        }
        if (family == OdfStyleFamily.Chart)
        {
            return attributeNamespace == OdfNamespaces.Chart
                && attributeName == "style-name";
        }
        if (family == OdfStyleFamily.DrawingPage)
        {
            return elementNamespace == OdfNamespaces.Drawing
                && elementName == "page"
                && attributeNamespace == OdfNamespaces.Drawing
                && attributeName == "style-name";
        }
        if (family == OdfStyleFamily.Presentation)
        {
            return attributeNamespace == OdfNamespaces.Presentation
                && attributeName == "style-name";
        }
        if (family == OdfStyleFamily.Ruby)
        {
            return elementNamespace == OdfNamespaces.Text
                && elementName == "ruby"
                && attributeNamespace == OdfNamespaces.Text
                && attributeName == "style-name";
        }
        return false;
    }

    static OdfStyle? FindEquivalentAutomaticStyle(
        OdfDocument document,
        OdfStyle candidate)
    {
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (style.IsAutomatic
                && style.IsNamed
                && StylesEquivalent(style, candidate))
            {
                return style;
            }
            index = index + 1;
        }
        return null;
    }

    static bool StylesEquivalent(OdfStyle first, OdfStyle second)
    {
        return FamiliesMatch(first, second)
            && first.PartPath == second.PartPath
            && first.ParentStyleName == second.ParentStyleName
            && first.NextStyleName == second.NextStyleName
            && first.ListStyleName == second.ListStyleName
            && first.MasterPageName == second.MasterPageName
            && first.DataStyleName == second.DataStyleName
            && PropertyArraysEqual(first.TextProperties, second.TextProperties)
            && PropertyArraysEqual(
                first.ParagraphProperties,
                second.ParagraphProperties)
            && PropertyArraysEqual(first.SectionProperties, second.SectionProperties)
            && PropertyArraysEqual(first.TableProperties, second.TableProperties)
            && PropertyArraysEqual(
                first.TableColumnProperties,
                second.TableColumnProperties)
            && PropertyArraysEqual(
                first.TableRowProperties,
                second.TableRowProperties)
            && PropertyArraysEqual(
                first.TableCellProperties,
                second.TableCellProperties)
            && PropertyArraysEqual(first.GraphicProperties, second.GraphicProperties)
            && PropertyArraysEqual(first.ChartProperties, second.ChartProperties)
            && PropertyArraysEqual(
                first.DrawingPageProperties,
                second.DrawingPageProperties)
            && PropertyArraysEqual(first.RubyProperties, second.RubyProperties)
            && PropertyArraysEqual(first.ListProperties, second.ListProperties)
            && NodeArraysEqual(
                first.TextPropertyChildren,
                second.TextPropertyChildren)
            && NodeArraysEqual(
                first.ParagraphPropertyChildren,
                second.ParagraphPropertyChildren)
            && NodeArraysEqual(
                first.SectionPropertyChildren,
                second.SectionPropertyChildren)
            && NodeArraysEqual(
                first.TablePropertyChildren,
                second.TablePropertyChildren)
            && NodeArraysEqual(
                first.TableColumnPropertyChildren,
                second.TableColumnPropertyChildren)
            && NodeArraysEqual(
                first.TableRowPropertyChildren,
                second.TableRowPropertyChildren)
            && NodeArraysEqual(
                first.TableCellPropertyChildren,
                second.TableCellPropertyChildren)
            && NodeArraysEqual(
                first.GraphicPropertyChildren,
                second.GraphicPropertyChildren)
            && NodeArraysEqual(
                first.ChartPropertyChildren,
                second.ChartPropertyChildren)
            && NodeArraysEqual(
                first.DrawingPagePropertyChildren,
                second.DrawingPagePropertyChildren)
            && NodeArraysEqual(
                first.RubyPropertyChildren,
                second.RubyPropertyChildren)
            && NodeArraysEqual(
                first.ListPropertyChildren,
                second.ListPropertyChildren);
    }

    static bool PropertyArraysEqual(
        OdfStyleProperty[] first,
        OdfStyleProperty[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            OdfStyleProperty left = first[index];
            OdfStyleProperty right = second[index];
            if (left.Name.NamespaceName != right.Name.NamespaceName
                || left.Name.LocalName != right.Name.LocalName
                || left.LexicalValue != right.LexicalValue)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool NodeArraysEqual(OdfNode[] first, OdfNode[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (!NodesEqual(first[index], second[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool NodesEqual(OdfNode first, OdfNode second)
    {
        if (first.Kind != second.Kind
            || first.Value != second.Value
            || first.IsSignificantWhitespace != second.IsSignificantWhitespace
            || first.WasEmptyElement != second.WasEmptyElement)
        {
            return false;
        }
        if (first.Name == null || second.Name == null)
        {
            if (first.Name != second.Name)
            {
                return false;
            }
        }
        else if (first.Name.NamespaceName != second.Name.NamespaceName
            || first.Name.LocalName != second.Name.LocalName)
        {
            return false;
        }
        if (first.Attributes.Length != second.Attributes.Length
            || first.Children.Length != second.Children.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Attributes.Length)
        {
            OdfAttribute left = first.Attributes[index];
            OdfAttribute right = second.Attributes[index];
            if (left.Name.NamespaceName != right.Name.NamespaceName
                || left.Name.LocalName != right.Name.LocalName
                || left.Value != right.Value)
            {
                return false;
            }
            index = index + 1;
        }
        index = 0;
        while (index < first.Children.Length)
        {
            if (!NodesEqual(first.Children[index], second.Children[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static OdfStyle CloneStyle(OdfStyle source)
    {
        OdfStyle result = new OdfStyle();
        result.Name = source.Name;
        result.DisplayName = source.DisplayName;
        result.Family = source.Family;
        result.FamilyName = source.FamilyName;
        result.PartPath = source.PartPath;
        result.ParentStyleName = source.ParentStyleName;
        result.NextStyleName = source.NextStyleName;
        result.ListStyleName = source.ListStyleName;
        result.MasterPageName = source.MasterPageName;
        result.DataStyleName = source.DataStyleName;
        result.TextProperties = CloneProperties(source.TextProperties);
        result.TextPropertyChildren = CloneNodes(source.TextPropertyChildren);
        result.ParagraphProperties = CloneProperties(source.ParagraphProperties);
        result.ParagraphPropertyChildren = CloneNodes(
            source.ParagraphPropertyChildren);
        result.SectionProperties = CloneProperties(source.SectionProperties);
        result.SectionPropertyChildren = CloneNodes(
            source.SectionPropertyChildren);
        result.TableProperties = CloneProperties(source.TableProperties);
        result.TablePropertyChildren = CloneNodes(source.TablePropertyChildren);
        result.TableColumnProperties = CloneProperties(
            source.TableColumnProperties);
        result.TableColumnPropertyChildren = CloneNodes(
            source.TableColumnPropertyChildren);
        result.TableRowProperties = CloneProperties(source.TableRowProperties);
        result.TableRowPropertyChildren = CloneNodes(
            source.TableRowPropertyChildren);
        result.TableCellProperties = CloneProperties(source.TableCellProperties);
        result.TableCellPropertyChildren = CloneNodes(
            source.TableCellPropertyChildren);
        result.GraphicProperties = CloneProperties(source.GraphicProperties);
        result.GraphicPropertyChildren = CloneNodes(
            source.GraphicPropertyChildren);
        result.ChartProperties = CloneProperties(source.ChartProperties);
        result.ChartPropertyChildren = CloneNodes(source.ChartPropertyChildren);
        result.DrawingPageProperties = CloneProperties(
            source.DrawingPageProperties);
        result.DrawingPagePropertyChildren = CloneNodes(
            source.DrawingPagePropertyChildren);
        result.RubyProperties = CloneProperties(source.RubyProperties);
        result.RubyPropertyChildren = CloneNodes(source.RubyPropertyChildren);
        result.ListProperties = CloneProperties(source.ListProperties);
        result.ListPropertyChildren = CloneNodes(source.ListPropertyChildren);
        result.IsNamed = source.IsNamed;
        result.IsDefault = source.IsDefault;
        result.IsCommon = source.IsCommon;
        result.IsAutomatic = source.IsAutomatic;
        return result;
    }

    static void ApplyStyleChanges(OdfStyle target, OdfStyle changes)
    {
        target.DisplayName = changes.DisplayName;
        target.ParentStyleName = changes.ParentStyleName;
        target.NextStyleName = changes.NextStyleName;
        target.ListStyleName = changes.ListStyleName;
        target.MasterPageName = changes.MasterPageName;
        target.DataStyleName = changes.DataStyleName;
        target.TextProperties = changes.TextProperties;
        target.TextPropertyChildren = changes.TextPropertyChildren;
        target.ParagraphProperties = changes.ParagraphProperties;
        target.ParagraphPropertyChildren = changes.ParagraphPropertyChildren;
        target.SectionProperties = changes.SectionProperties;
        target.SectionPropertyChildren = changes.SectionPropertyChildren;
        target.TableProperties = changes.TableProperties;
        target.TablePropertyChildren = changes.TablePropertyChildren;
        target.TableColumnProperties = changes.TableColumnProperties;
        target.TableColumnPropertyChildren = changes.TableColumnPropertyChildren;
        target.TableRowProperties = changes.TableRowProperties;
        target.TableRowPropertyChildren = changes.TableRowPropertyChildren;
        target.TableCellProperties = changes.TableCellProperties;
        target.TableCellPropertyChildren = changes.TableCellPropertyChildren;
        target.GraphicProperties = changes.GraphicProperties;
        target.GraphicPropertyChildren = changes.GraphicPropertyChildren;
        target.ChartProperties = changes.ChartProperties;
        target.ChartPropertyChildren = changes.ChartPropertyChildren;
        target.DrawingPageProperties = changes.DrawingPageProperties;
        target.DrawingPagePropertyChildren = changes.DrawingPagePropertyChildren;
        target.RubyProperties = changes.RubyProperties;
        target.RubyPropertyChildren = changes.RubyPropertyChildren;
        target.ListProperties = changes.ListProperties;
        target.ListPropertyChildren = changes.ListPropertyChildren;
        target.IsAutomatic = !target.IsDefault && changes.IsAutomatic;
        target.IsCommon = !target.IsAutomatic;
        target.IsNamed = !target.IsDefault && target.Name.Length > 0;
        if (changes.PartPath.Length > 0)
        {
            target.PartPath = changes.PartPath;
        }
    }

    static OdfStyleProperty[] CloneProperties(OdfStyleProperty[] source)
    {
        OdfStyleProperty[] result = new OdfStyleProperty[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            OdfStyleProperty property = new OdfStyleProperty();
            property.Name.NamespaceName = source[index].Name.NamespaceName;
            property.Name.LocalName = source[index].Name.LocalName;
            property.Name.Prefix = source[index].Name.Prefix;
            property.LexicalValue = source[index].LexicalValue;
            result[index] = property;
            index = index + 1;
        }
        return result;
    }

    static OdfNode[] CloneNodes(OdfNode[] source)
    {
        OdfNode[] result = new OdfNode[source.Length];
        OdfTreeEditor editor = new OdfTreeEditor([]);
        int index = 0;
        while (index < source.Length)
        {
            result[index] = editor.CloneSubtree(source[index]);
            index = index + 1;
        }
        return result;
    }

    static OdfStyle[] RemoveStyles(
        OdfStyle[] source,
        List<OdfStyle> removed)
    {
        List<OdfStyle> result = new List<OdfStyle>();
        int index = 0;
        while (index < source.Length)
        {
            if (!ListContainsStyle(removed, source[index]))
            {
                result.Add(source[index]);
            }
            index = index + 1;
        }
        return result.ToArray();
    }
}
