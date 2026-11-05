namespace svg;

internal static class SvgCompositionProcessJobs
{
    internal static void FlattenReferences(SvgProcessContext context, SvgFlattenReferencesProcessOptions options)
    {
        if (options == null || options.UseOptions == null || options.NamespacePrefix == null || options.NamespacePrefix.Length == 0 || options.MaximumNodes < 1)
        {
            context.Fail("Reference flattening settings are invalid.");
            return;
        }
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document.DocumentNode);
        if (nodes.Count > options.MaximumNodes)
        {
            context.Fail("Reference flattening exceeded the configured node bound.");
            return;
        }
        FlattenInheritedResources(context, nodes);
        int ordinal = 1;
        int index = nodes.Count - 1;
        while (index >= 0 && !context.IsFailed())
        {
            SvgNode node = nodes[index];
            if (node.ElementKind == SvgElementKind.Use && node.Parent != null)
            {
                SvgUseResult use = new SvgUseResolver().ResolveWithNodeAndOptions(node, options.UseOptions);
                if (use.Success && use.Reference != null && use.Reference.IsSameDocument && use.Reference.Target != null)
                {
                    SvgNode? replacement = UseReplacement(context, node, use);
                    if (replacement != null)
                    {
                        SvgMutationResult mutation = context.Mutations.ReplaceChild(node.Parent, node, replacement);
                        if (!mutation.Success) context.Fail(mutation.Message);
                        else
                        {
                            string prefix = UniquePrefix(context.Document, replacement, options.NamespacePrefix + ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-");
                            PrefixSubtree(context, replacement, prefix);
                            ordinal = ordinal + 1;
                        }
                    }
                }
            }
            index = index - 1;
        }
    }

    internal static void ExtractFragment(SvgProcessContext context, SvgFragmentExtractionProcessOptions options)
    {
        if (options == null || options.FragmentIdentifier == null || options.MaximumNodes < 1 || options.NamespacePrefix == null)
        {
            context.Fail("Fragment extraction settings are invalid.");
            return;
        }
        string identifier = new SvgUri().DecodeFragment(options.FragmentIdentifier);
        if (identifier.Length == 0)
        {
            context.Fail("Fragment extraction requires an identifier.");
            return;
        }
        System.Collections.Generic.List<SvgNode> all = CollectNodes(context.Document.DocumentNode);
        if (all.Count > options.MaximumNodes)
        {
            context.Fail("Fragment extraction exceeded the configured node bound.");
            return;
        }
        SvgNode? target = context.Document.Index.GetById(identifier);
        if (target == null) return;
        SvgNode? root = context.Document.DocumentElement;
        System.Collections.Generic.List<SvgNode> graphics = GraphicalChildren(root);
        if (target.Parent == root && graphics.Count == 1 && object.ReferenceEquals(graphics[0], target)) return;
        SvgNode replacement = new SvgNodeApi().CloneNode((root ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), false);
        int index = 0;
        while (index < root.Children.Count && !context.IsFailed())
        {
            SvgNode child = root.Children[index];
            bool descriptive = child.ElementKind == SvgElementKind.Title || child.ElementKind == SvgElementKind.Desc || child.ElementKind == SvgElementKind.Metadata;
            bool resource = options.RetainDefinitions && (child.ElementKind == SvgElementKind.Defs || child.ElementKind == SvgElementKind.Style);
            if (descriptive || resource) AppendDetached(context, replacement, new SvgNodeApi().CloneNode(child, true));
            index = index + 1;
        }
        AppendDetached(context, replacement, new SvgNodeApi().CloneNode(target, true));
        if (context.IsFailed()) return;
        SvgMutationResult mutation = context.Mutations.ReplaceChild(root.Parent, root, replacement);
        if (!mutation.Success) context.Fail(mutation.Message);
        else if (options.NamespacePrefix.Length != 0) PrefixSubtree(context, replacement, UniquePrefix(context.Document, replacement, options.NamespacePrefix));
    }

    internal static void SplitSymbols(SvgProcessContext context, SvgSymbolSplitProcessOptions options)
    {
        if (options == null || options.OutputDocuments == null || options.NamespacePrefix == null || options.NamespacePrefix.Length == 0 || options.MaximumSymbols < 1)
        {
            context.Fail("Symbol split settings are invalid.");
            return;
        }
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document.DocumentNode);
        System.Collections.Generic.List<SvgNode> symbols = new System.Collections.Generic.List<SvgNode>();
        int index = 0;
        while (index < nodes.Count)
        {
            if (nodes[index].ElementKind == SvgElementKind.Symbol) symbols.Add(nodes[index]);
            index = index + 1;
        }
        if (symbols.Count > options.MaximumSymbols)
        {
            context.Fail("Symbol splitting exceeded the configured symbol bound.");
            return;
        }
        options.OutputDocuments.Clear();
        index = 0;
        while (index < symbols.Count && !context.IsFailed())
        {
            SvgDocument output = SymbolDocument(context.Document, symbols[index]);
            string identifier = Id(symbols[index]);
            string prefix = options.NamespacePrefix + (identifier.Length == 0 ? (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) : identifier) + "-";
            SvgProcessOptions process = new SvgProcessOptions();
            SvgIdPrefixOptions ids = new SvgIdPrefixOptions();
            ids.Prefix = prefix;
            process.Jobs.Add(new SvgProcessJobs().PrefixIdsWithOptions(ids));
            SvgProcessResult result = output.ProcessWithOptions(process);
            if (!result.Success) context.Fail("A split symbol document could not isolate its identifier namespace.");
            else options.OutputDocuments.Add(output);
            index = index + 1;
        }
    }

    internal static void MergeDocuments(SvgProcessContext context, SvgDocumentMergeProcessOptions options)
    {
        if (!ValidMerge(options))
        {
            context.Fail("Document merge settings are invalid.");
            return;
        }
        SvgNode? root = context.Document.DocumentElement;
        int importedNodes = 0;
        int index = 0;
        while (index < options.Documents.Count && !context.IsFailed())
        {
            SvgDocument source = options.Documents[index];
            if (source == null || source.DocumentElement == null)
            {
                context.Fail("Every merged document must have a root element.");
                break;
            }
            string marker = (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!HasMarker(root, "data-svg-merge-index", marker))
            {
                System.Collections.Generic.List<SvgNode> children = ImportableChildren(source.DocumentElement);
                importedNodes = importedNodes + CountNodes(children);
                if (importedNodes > options.MaximumNodes)
                {
                    context.Fail("Document merge exceeded the configured node bound.");
                    break;
                }
                SvgNode group = context.Documents.CreateElement(SvgElementNames.G, "", SvgElementNames.G, (root ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).NamespaceUri);
                SvgMutationResult appended = context.Mutations.AppendChild(root, group);
                if (!appended.Success) context.Fail(appended.Message);
                int childIndex = 0;
                while (childIndex < children.Count && !context.IsFailed())
                {
                    SvgNode imported = context.Documents.ImportNode(context.Document, children[childIndex], true);
                    SvgMutationResult childResult = context.Mutations.AppendChild(group, imported);
                    if (!childResult.Success) context.Fail(childResult.Message);
                    childIndex = childIndex + 1;
                }
                if (!context.IsFailed())
                {
                    string prefix = UniquePrefix(context.Document, group, options.NamespacePrefix + marker + "-");
                    PrefixSubtree(context, group, prefix);
                    SetAttribute(context, group, SvgAttributeNames.Id, UniqueId(context.Document, options.NamespacePrefix + "document-" + marker));
                    SetAttribute(context, group, "data-svg-merge-index", marker);
                }
            }
            index = index + 1;
        }
    }

    internal static void BuildSprite(SvgProcessContext context, SvgSpriteProcessOptions options)
    {
        if (!ValidSprite(options))
        {
            context.Fail("Sprite construction settings are invalid.");
            return;
        }
        int total = options.Documents.Count + (options.IncludeCurrentDocument ? 1 : 0);
        if (total > options.MaximumSymbols)
        {
            context.Fail("Sprite construction exceeded the configured symbol bound.");
            return;
        }
        SvgNode? root = context.Document.DocumentElement;
        SvgNode? defs = DirectChild(root, SvgElementKind.Defs);
        if (defs == null)
        {
            defs = context.Documents.CreateElement(SvgElementNames.Defs, "", SvgElementNames.Defs, (root ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).NamespaceUri);
            SvgMutationResult appended = context.Mutations.AppendChild(root, defs);
            if (!appended.Success)
            {
                context.Fail(appended.Message);
                return;
            }
        }
        int generatedNodes = 0;
        int outputIndex = options.IncludeCurrentDocument ? 1 : 0;
        if (options.IncludeCurrentDocument && !HasMarker(defs, "data-svg-sprite-index", "0"))
        {
            System.Collections.Generic.List<SvgNode> current = GraphicalChildren(root);
            generatedNodes = generatedNodes + CountNodes(current);
            SvgNode? symbol = CreateSymbol(context, root, current, "0", options, defs);
            if (symbol != null)
            {
                int removeIndex = current.Count - 1;
                while (removeIndex >= 0 && !context.IsFailed())
                {
                    SvgMutationResult removed = context.Mutations.RemoveChild(root, current[removeIndex]);
                    if (!removed.Success) context.Fail(removed.Message);
                    removeIndex = removeIndex - 1;
                }
            }
        }
        int index = 0;
        while (index < options.Documents.Count && !context.IsFailed())
        {
            string marker = outputIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!HasMarker(defs, "data-svg-sprite-index", marker))
            {
                SvgDocument source = options.Documents[index];
                if (source == null || source.DocumentElement == null) context.Fail("Every sprite input must have a root element.");
                else
                {
                    System.Collections.Generic.List<SvgNode> children = ImportableChildren(source.DocumentElement);
                    generatedNodes = generatedNodes + CountNodes(children);
                    if (generatedNodes > options.MaximumNodes) context.Fail("Sprite construction exceeded the configured node bound.");
                    else CreateSymbol(context, source.DocumentElement, children, marker, options, defs);
                }
            }
            outputIndex = outputIndex + 1;
            index = index + 1;
        }
    }

    internal static void IsolateNamespace(SvgProcessContext context, SvgNamespaceIsolationProcessOptions options)
    {
        if (options == null || options.IdOptions == null)
        {
            context.Fail("Namespace isolation settings are invalid.");
            return;
        }
        SvgIdRewriter.Prefix(context, options.IdOptions);
    }

    static void FlattenInheritedResources(SvgProcessContext context, System.Collections.Generic.List<SvgNode> nodes)
    {
        int index = nodes.Count - 1;
        while (index >= 0 && !context.IsFailed())
        {
            SvgNode node = nodes[index];
            SvgAttribute? href = Href(node);
            if (href != null && node.ElementKind != SvgElementKind.Use && node.ElementKind != SvgElementKind.Image && node.ElementKind != SvgElementKind.A)
            {
                string identifier = LocalFragment(href.Value);
                SvgNode? target = identifier.Length == 0 ? null : context.Document.Index.GetById(identifier);
                if (target != null && target.ElementKind == node.ElementKind && !object.ReferenceEquals(target, node))
                {
                    CopyMissingAttributes(context, target, node);
                    if (node.Children.Count == 0) CopyChildren(context, target, node);
                    RemoveHrefAttributes(context, node);
                }
            }
            index = index - 1;
        }
    }

    static SvgNode? UseReplacement(SvgProcessContext context, SvgNode useNode, SvgUseResult use)
    {
        SvgNode? target = (use.Reference ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Target;
        SvgNode group = context.Documents.CreateElement(SvgElementNames.G, "", SvgElementNames.G, useNode.NamespaceUri);
        CopyUseAttributes(context, useNode, group);
        if (!Identity(use.Transform)) AppendDetachedAttribute(context, group, SvgAttributeNames.Transform, MatrixText(use.Transform));
        if ((target ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ElementKind == SvgElementKind.Symbol)
        {
            CopyMissingAttributesDetached(context, target, group);
            CopyChildren(context, target, group);
        }
        else AppendDetached(context, group, new SvgNodeApi().CloneNode(target, true));
        return context.IsFailed() ? null : group;
    }

    static SvgNode? CreateSymbol(SvgProcessContext context, SvgNode? sourceRoot, System.Collections.Generic.List<SvgNode> children, string marker, SvgSpriteProcessOptions options, SvgNode defs)
    {
        SvgNode symbol = context.Documents.CreateElement(SvgElementNames.Symbol, "", SvgElementNames.Symbol, defs.NamespaceUri);
        CopyNamedAttributeDetached(context, sourceRoot, symbol, SvgAttributeNames.ViewBox);
        CopyNamedAttributeDetached(context, sourceRoot, symbol, SvgAttributeNames.PreserveAspectRatio);
        int index = 0;
        while (index < children.Count && !context.IsFailed())
        {
            SvgNode imported = object.ReferenceEquals((sourceRoot ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).OwnerDocument, context.Document) ? new SvgNodeApi().CloneNode(children[index], true) : context.Documents.ImportNode(context.Document, children[index], true);
            AppendDetached(context, symbol, imported);
            index = index + 1;
        }
        if (context.IsFailed()) return null;
        SvgMutationResult appended = context.Mutations.AppendChild(defs, symbol);
        if (!appended.Success)
        {
            context.Fail(appended.Message);
            return null;
        }
        string prefix = UniquePrefix(context.Document, symbol, options.SymbolPrefix + marker + "-");
        PrefixSubtree(context, symbol, prefix);
        SetAttribute(context, symbol, SvgAttributeNames.Id, UniqueId(context.Document, options.SymbolPrefix + marker));
        SetAttribute(context, symbol, "data-svg-sprite-index", marker);
        return symbol;
    }

    static SvgDocument SymbolDocument(SvgDocument source, SvgNode symbol)
    {
        SvgDocument output = new SvgDocument();
        SvgDocumentApi documents = new SvgDocumentApi();
        SvgNode root = documents.CreateElement(SvgElementNames.Svg, "", SvgElementNames.Svg, SvgNamespaces.Svg);
        new SvgNodeApi().AppendChild(output.DocumentNode, root);
        AppendOwnedAttribute(output, root, "xmlns", "", "xmlns", SvgNamespaces.Xmlns, SvgNamespaces.Svg);
        CopyOwnedAttribute(output, symbol, root, SvgAttributeNames.ViewBox);
        CopyOwnedAttribute(output, symbol, root, SvgAttributeNames.PreserveAspectRatio);
        SvgNode? sourceDefs = DirectChild(source.DocumentElement, SvgElementKind.Defs);
        if (sourceDefs != null)
        {
            SvgNode defs = documents.CreateElement(SvgElementNames.Defs, "", SvgElementNames.Defs, SvgNamespaces.Svg);
            int resourceIndex = 0;
            while (resourceIndex < sourceDefs.Children.Count)
            {
                if (sourceDefs.Children[resourceIndex].ElementKind != SvgElementKind.Symbol) new SvgNodeApi().AppendChild(defs, documents.ImportNode(output, sourceDefs.Children[resourceIndex], true));
                resourceIndex = resourceIndex + 1;
            }
            if (defs.Children.Count != 0) new SvgNodeApi().AppendChild(root, defs);
        }
        int index = 0;
        while (index < symbol.Children.Count)
        {
            new SvgNodeApi().AppendChild(root, documents.ImportNode(output, symbol.Children[index], true));
            index = index + 1;
        }
        return output;
    }

    static void PrefixSubtree(SvgProcessContext context, SvgNode root, string prefix)
    {
        SvgIdPrefixOptions ids = new SvgIdPrefixOptions();
        ids.Prefix = prefix;
        SvgIdRewriter.PrefixSubtree(context, root, ids);
    }

    static string UniquePrefix(SvgDocument document, SvgNode subtree, string requested)
    {
        string prefix = requested;
        System.Collections.Generic.HashSet<SvgNode> inside = new System.Collections.Generic.HashSet<SvgNode>(CollectNodes(subtree));
        System.Collections.Generic.HashSet<string> outside = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        System.Collections.Generic.List<SvgNode> all = CollectNodes(document.DocumentNode);
        int index = 0;
        while (index < all.Count)
        {
            if (!inside.Contains(all[index]))
            {
                string identifier = Id(all[index]);
                if (identifier.Length != 0) outside.Add(identifier);
            }
            index = index + 1;
        }
        bool collision = true;
        while (collision)
        {
            collision = false;
            System.Collections.Generic.List<SvgNode> nodes = CollectNodes(subtree);
            index = 0;
            while (index < nodes.Count && !collision)
            {
                string identifier = Id(nodes[index]);
                if (identifier.Length != 0 && outside.Contains(identifier.StartsWith(prefix, System.StringComparison.Ordinal) ? identifier : prefix + identifier)) collision = true;
                index = index + 1;
            }
            if (collision) prefix = prefix + "x-";
        }
        return prefix;
    }

    static string UniqueId(SvgDocument document, string requested)
    {
        string result = requested;
        int ordinal = 2;
        while (document.Index.GetById(result) != null)
        {
            result = requested + "-" + ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ordinal = ordinal + 1;
        }
        return result;
    }

    static void CopyUseAttributes(SvgProcessContext context, SvgNode source, SvgNode target)
    {
        int index = 0;
        while (index < source.Attributes.Count)
        {
            SvgAttribute attribute = source.Attributes[index];
            string name = attribute.LocalName;
            bool omit = name == SvgAttributeNames.Href || attribute.NamespaceUri.Length == 0 && (name == SvgAttributeNames.X || name == SvgAttributeNames.Y || name == SvgAttributeNames.Width || name == SvgAttributeNames.Height || name == SvgAttributeNames.Transform);
            if (!omit) AppendDetachedAttribute(context, target, attribute.QualifiedName, attribute.Prefix, name, attribute.NamespaceUri, attribute.Value);
            index = index + 1;
        }
    }

    static void CopyMissingAttributes(SvgProcessContext context, SvgNode source, SvgNode target)
    {
        int index = 0;
        while (index < source.Attributes.Count && !context.IsFailed())
        {
            SvgAttribute attribute = source.Attributes[index];
            if (attribute.LocalName != SvgAttributeNames.Id && attribute.LocalName != SvgAttributeNames.Href && FindAttribute(target, attribute.LocalName, attribute.NamespaceUri) == null)
            {
                SvgAttribute copy = context.Documents.CreateAttribute(attribute.QualifiedName, attribute.Prefix, attribute.LocalName, attribute.NamespaceUri, attribute.Value);
                SvgMutationResult mutation = context.Mutations.AppendAttribute(target, copy);
                if (!mutation.Success) context.Fail(mutation.Message);
            }
            index = index + 1;
        }
    }

    static void CopyMissingAttributesDetached(SvgProcessContext context, SvgNode source, SvgNode target)
    {
        int index = 0;
        while (index < source.Attributes.Count && !context.IsFailed())
        {
            SvgAttribute attribute = source.Attributes[index];
            if (attribute.LocalName != SvgAttributeNames.Id && attribute.LocalName != SvgAttributeNames.Href && FindAttribute(target, attribute.LocalName, attribute.NamespaceUri) == null) AppendDetachedAttribute(context, target, attribute.QualifiedName, attribute.Prefix, attribute.LocalName, attribute.NamespaceUri, attribute.Value);
            index = index + 1;
        }
    }

    static void CopyChildren(SvgProcessContext context, SvgNode source, SvgNode target)
    {
        int index = 0;
        while (index < source.Children.Count && !context.IsFailed())
        {
            AppendDetached(context, target, new SvgNodeApi().CloneNode(source.Children[index], true));
            index = index + 1;
        }
    }

    static void RemoveHrefAttributes(SvgProcessContext context, SvgNode node)
    {
        int index = node.Attributes.Count - 1;
        while (index >= 0 && !context.IsFailed())
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.LocalName == SvgAttributeNames.Href && (attribute.NamespaceUri.Length == 0 || attribute.NamespaceUri == SvgNamespaces.XLink))
            {
                SvgMutationResult mutation = context.Mutations.RemoveAttribute(node, attribute);
                if (!mutation.Success) context.Fail(mutation.Message);
            }
            index = index - 1;
        }
    }

    static System.Collections.Generic.List<SvgNode> ImportableChildren(SvgNode root)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        int index = 0;
        while (index < root.Children.Count)
        {
            SvgNode child = root.Children[index];
            if (child.Kind == SvgNodeKind.Element && child.ElementKind != SvgElementKind.Title && child.ElementKind != SvgElementKind.Desc && child.ElementKind != SvgElementKind.Metadata) result.Add(child);
            index = index + 1;
        }
        return result;
    }

    static System.Collections.Generic.List<SvgNode> GraphicalChildren(SvgNode? root)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        int index = 0;
        while (index < (root ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Children.Count)
        {
            SvgNode child = root.Children[index];
            if (child.Kind == SvgNodeKind.Element && child.ElementKind != SvgElementKind.Title && child.ElementKind != SvgElementKind.Desc && child.ElementKind != SvgElementKind.Metadata && child.ElementKind != SvgElementKind.Defs && child.ElementKind != SvgElementKind.Style) result.Add(child);
            index = index + 1;
        }
        return result;
    }

    static int CountNodes(System.Collections.Generic.List<SvgNode> roots)
    {
        int count = 0;
        int index = 0;
        while (index < roots.Count)
        {
            count = count + CollectNodes(roots[index]).Count;
            index = index + 1;
        }
        return count;
    }

    static bool HasMarker(SvgNode? parent, string name, string value)
    {
        int index = 0;
        while (index < (parent ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Children.Count)
        {
            SvgAttribute? marker = FindAttribute(parent.Children[index], name, "");
            if (marker != null && marker.Value == value) return true;
            index = index + 1;
        }
        return false;
    }

    static SvgNode? DirectChild(SvgNode? parent, SvgElementKind kind)
    {
        if (parent == null) return null;
        int index = 0;
        while (index < parent.Children.Count)
        {
            if (parent.Children[index].ElementKind == kind) return parent.Children[index];
            index = index + 1;
        }
        return null;
    }

    static SvgAttribute? Href(SvgNode node)
    {
        SvgAttribute? result = FindAttribute(node, SvgAttributeNames.Href, "");
        return result == null ? FindAttribute(node, SvgAttributeNames.Href, SvgNamespaces.XLink) : result;
    }

    static string LocalFragment(string value)
    {
        string trimmed = value.Trim();
        return trimmed.Length > 1 && trimmed[0] == '#' ? new SvgUri().DecodeFragment(trimmed) : "";
    }

    static string Id(SvgNode node)
    {
        SvgAttribute? id = FindAttribute(node, SvgAttributeNames.Id, "");
        return id == null ? "" : id.Value;
    }

    static bool Identity(SvgMatrix matrix)
    {
        return matrix == null || matrix.A == 1 && matrix.B == 0 && matrix.C == 0 && matrix.D == 1 && matrix.E == 0 && matrix.F == 0;
    }

    static string MatrixText(SvgMatrix matrix)
    {
        return "matrix(" + Number(matrix.A) + " " + Number(matrix.B) + " " + Number(matrix.C) + " " + Number(matrix.D) + " " + Number(matrix.E) + " " + Number(matrix.F) + ")";
    }

    static string Number(double value)
    {
        SvgNumberFormatResult formatted = SvgNumberFormatter.FormatCompact(value);
        return formatted.Success ? formatted.Text : "0";
    }

    static bool ValidMerge(SvgDocumentMergeProcessOptions options)
    {
        return options != null && options.Documents != null && options.NamespacePrefix != null && options.NamespacePrefix.Length != 0 && options.MaximumDocuments > 0 && options.Documents.Count <= options.MaximumDocuments && options.MaximumNodes > 0;
    }

    static bool ValidSprite(SvgSpriteProcessOptions options)
    {
        return options != null && options.Documents != null && options.SymbolPrefix != null && options.SymbolPrefix.Length != 0 && options.MaximumSymbols > 0 && options.MaximumNodes > 0;
    }

    static void AppendDetached(SvgProcessContext context, SvgNode parent, SvgNode child)
    {
        SvgMutationResult mutation = new SvgNodeApi().AppendChild(parent, child);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static void AppendDetachedAttribute(SvgProcessContext context, SvgNode node, string name, string value)
    {
        AppendDetachedAttribute(context, node, name, "", name, "", value);
    }

    static void AppendDetachedAttribute(SvgProcessContext context, SvgNode node, string qualifiedName, string prefix, string localName, string namespaceUri, string value)
    {
        SvgAttribute attribute = context.Documents.CreateAttribute(qualifiedName, prefix, localName, namespaceUri, value);
        SvgMutationResult mutation = new SvgNodeApi().AppendAttribute(node, attribute);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static void SetAttribute(SvgProcessContext context, SvgNode node, string name, string value)
    {
        SvgAttribute? current = FindAttribute(node, name, "");
        if (current != null && current.Value == value) return;
        SvgAttribute replacement = context.Documents.CreateAttribute(name, "", name, "", value);
        SvgMutationResult mutation = current == null ? context.Mutations.AppendAttribute(node, replacement) : context.Mutations.ReplaceAttribute(node, current, replacement);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static void CopyNamedAttributeDetached(SvgProcessContext context, SvgNode? source, SvgNode target, string name)
    {
        SvgAttribute? attribute = FindAttribute(source, name, "");
        if (attribute != null) AppendDetachedAttribute(context, target, attribute.QualifiedName, attribute.Prefix, attribute.LocalName, attribute.NamespaceUri, attribute.Value);
    }

    static void AppendOwnedAttribute(SvgDocument document, SvgNode node, string qualifiedName, string prefix, string localName, string namespaceUri, string value)
    {
        SvgAttribute attribute = new SvgDocumentApi().CreateAttribute(qualifiedName, prefix, localName, namespaceUri, value);
        new SvgNodeApi().AppendAttribute(node, attribute);
    }

    static void CopyOwnedAttribute(SvgDocument document, SvgNode source, SvgNode target, string name)
    {
        SvgAttribute? attribute = FindAttribute(source, name, "");
        if (attribute != null) AppendOwnedAttribute(document, target, attribute.QualifiedName, attribute.Prefix, attribute.LocalName, attribute.NamespaceUri, attribute.Value);
    }

    static SvgAttribute? FindAttribute(SvgNode? node, string name, string namespaceUri)
    {
        if (node == null) return null;
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.LocalName == name && attribute.NamespaceUri == namespaceUri) return attribute;
            index = index + 1;
        }
        return null;
    }

    static System.Collections.Generic.List<SvgNode> CollectNodes(SvgNode root)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        pending.Add(root);
        while (pending.Count != 0)
        {
            SvgNode node = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            result.Add(node);
            int index = node.Children.Count - 1;
            while (index >= 0)
            {
                pending.Add(node.Children[index]);
                index = index - 1;
            }
        }
        return result;
    }
}
