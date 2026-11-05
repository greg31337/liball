using System;
using System.Collections.Generic;

namespace odf;

internal static class ScriptDocumentModel
{
    internal static void LoadScripts(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for script inspection.");
        }
        List<OdfScript> scripts = new List<OdfScript>();
        List<OdfEventListener> listeners =
            new List<OdfEventListener>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            if (part.Scripts != null)
            {
                LoadEmbeddedScripts(part.Scripts, scripts);
            }
            if (part.Root != null)
            {
                FindEventListeners(document, part.Root, listeners);
            }
            partIndex = partIndex + 1;
        }
        LoadPackageScripts(document, scripts);
        document.Scripts = scripts.ToArray();
        document.EventListeners = listeners.ToArray();
        ScriptDocumentState state = new ScriptDocumentState();
        state.Scripts = document.Scripts;
        state.EventListeners = document.EventListeners;
        document.ScriptState = state;
    }

    internal static void SynchronizeScripts(OdfDocument document)
    {
        if (document == null
            || document.Scripts == null
            || document.EventListeners == null)
        {
            throw new OdfDocumentException(
                "A document, scripts, and event listeners are required for writing.");
        }
        ScriptDocumentState? state = document.ScriptState;
        if (state == null)
        {
            state = new ScriptDocumentState();
        }
        RemoveDiscardedScripts(document, state.Scripts, document.Scripts);
        SynchronizeCurrentScripts(document, document.Scripts);
        RemoveDiscardedListeners(
            state.EventListeners,
            document.EventListeners);
        SynchronizeCurrentListeners(document, document.EventListeners);
        RefreshPackage(document);
        state.Scripts = document.Scripts;
        state.EventListeners = document.EventListeners;
        document.ScriptState = state;
    }

    static void LoadEmbeddedScripts(
        OdfNode section,
        List<OdfScript> scripts)
    {
        int index = 0;
        while (index < section.Children.Length)
        {
            OdfNode node = section.Children[index];
            if (IsElement(node, OdfNamespaces.Office, "script"))
            {
                OdfScript script = new OdfScript();
                script.Kind = OdfScriptKind.Embedded;
                script.Language = Read(
                    node,
                    OdfNamespaces.Script,
                    "language");
                script.Text = DirectText(node);
                script.Attributes = node.Attributes;
                script.ChildNodes = node.Children;
                script.PartPath = node.Location.PartPath;
                script.Classification =
                    OdfActiveContentClassification.EmbeddedCode;
                script.IsActiveContent = true;
                script.Node = node;
                CaptureScript(script);
                scripts.Add(script);
            }
            index = index + 1;
        }
    }

    static void LoadPackageScripts(
        OdfDocument document,
        List<OdfScript> scripts)
    {
        if (document.Package == null
            || document.Package.EntryIndex == null)
        {
            return;
        }
        PackagePartCatalog catalog =
            document.Package.EntryIndex.DiscoverParts();
        EditablePackageEntry[] entries =
            document.Package.EntryIndex.GetEditableEntries();
        int index = 0;
        while (index < catalog.ScriptParts.Length)
        {
            DiscoveredPackagePart part = catalog.ScriptParts[index];
            if (!part.Entry.IsDirectory)
            {
                OdfScript script = new OdfScript();
                script.Kind = OdfScriptKind.PackageEntry;
                script.PackagePath = part.Entry.Path;
                script.MediaType = part.MediaType;
                script.IsEncrypted = part.IsEncrypted;
                EditablePackageEntry? entry = FindEditableEntry(
                    entries,
                    part.Entry.Path);
                if (entry != null && entry.IsContentAvailable)
                {
                    script.Data = CopyBytes(entry.Data);
                    script.IsContentAvailable = true;
                }
                script.Classification =
                    OdfActiveContentClassification.EmbeddedCode;
                script.IsActiveContent = true;
                CaptureScript(script);
                scripts.Add(script);
            }
            index = index + 1;
        }
    }

    static void FindEventListeners(
        OdfDocument document,
        OdfNode node,
        List<OdfEventListener> listeners)
    {
        OdfEventListenerKind kind = EventKind(node);
        if (kind != OdfEventListenerKind.Unknown)
        {
            listeners.Add(LoadEventListener(document, node, kind));
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            FindEventListeners(document, node.Children[index], listeners);
            index = index + 1;
        }
    }

    static OdfEventListener LoadEventListener(
        OdfDocument document,
        OdfNode node,
        OdfEventListenerKind kind)
    {
        OdfEventListener result = new OdfEventListener();
        result.Kind = kind;
        result.EventName = Read(
            node,
            OdfNamespaces.Script,
            "event-name");
        result.Language = Read(
            node,
            OdfNamespaces.Script,
            "language");
        result.MacroName = Read(
            node,
            OdfNamespaces.Script,
            "macro-name");
        result.Href = Read(node, OdfNamespaces.XLink, "href");
        result.LinkType = Read(node, OdfNamespaces.XLink, "type");
        result.Show = Read(node, OdfNamespaces.XLink, "show");
        result.Actuate = Read(node, OdfNamespaces.XLink, "actuate");
        result.Action = Read(
            node,
            OdfNamespaces.Presentation,
            "action");
        result.Effect = Read(
            node,
            OdfNamespaces.Presentation,
            "effect");
        result.Direction = Read(
            node,
            OdfNamespaces.Presentation,
            "direction");
        result.Speed = Read(
            node,
            OdfNamespaces.Presentation,
            "speed");
        result.StartScale = Read(
            node,
            OdfNamespaces.Presentation,
            "start-scale");
        result.Verb = Read(
            node,
            OdfNamespaces.Presentation,
            "verb");
        result.Attributes = node.Attributes;
        result.ChildNodes = node.Children;
        result.ContainerNode = node.Parent;
        result.OwnerNode = node.Parent == null ? null : node.Parent.Parent;
        result.PartPath = node.Location.PartPath;
        result.Node = node;
        RefreshEventClassification(document, result);
        CaptureEvent(result);
        return result;
    }

    static void RemoveDiscardedScripts(
        OdfDocument document,
        OdfScript[] original,
        OdfScript[] current)
    {
        int index = 0;
        while (index < original.Length)
        {
            OdfScript script = original[index];
            if (!ContainsScript(current, script))
            {
                if (script.Kind == OdfScriptKind.Embedded
                    && script.Node != null)
                {
                    StyleDocumentModel.RemoveNodeFromParent(script.Node);
                }
                else if (script.Kind == OdfScriptKind.PackageEntry)
                {
                    RemovePackageScript(document, script.PackagePath);
                }
            }
            index = index + 1;
        }
    }

    static void SynchronizeCurrentScripts(
        OdfDocument document,
        OdfScript[] scripts)
    {
        int index = 0;
        while (index < scripts.Length)
        {
            OdfScript script = scripts[index];
            if (script == null)
            {
                throw new OdfDocumentException(
                    "Script collections cannot contain null entries.");
            }
            if (script.Kind == OdfScriptKind.Embedded)
            {
                SynchronizeEmbeddedScript(document, script);
            }
            else if (script.Kind == OdfScriptKind.PackageEntry)
            {
                SynchronizePackageScript(document, script);
            }
            else
            {
                throw new OdfDocumentException(
                    "A script requires a recognized storage kind.");
            }
            script.Classification =
                OdfActiveContentClassification.EmbeddedCode;
            script.IsActiveContent = true;
            CaptureScript(script);
            index = index + 1;
        }
    }

    static void SynchronizeEmbeddedScript(
        OdfDocument document,
        OdfScript script)
    {
        bool isNew = script.Node == null;
        OdfNode node = script.Node ?? CreateElement(
            OdfNamespaces.Office,
            "script",
            OdfNamespaces.OfficePrefix,
            script.PartPath);
        if (isNew)
        {
            if (script.Language.Length == 0)
            {
                throw new OdfDocumentException(
                    "A new embedded script requires a language.");
            }
            OdfNode section = EnsureScriptsSection(
                document,
                script.PartPath);
            AttachScriptChildren(node, script);
            InsertEmbeddedScript(section, node);
            script.Node = node;
            script.PartPath = section.Location.PartPath;
        }
        WriteChanged(
            node,
            OdfNamespaces.Script,
            "language",
            OdfNamespaces.ScriptPrefix,
            script.Language,
            script.OriginalLanguage,
            isNew);
        if (!isNew && script.Text != script.OriginalText)
        {
            SetDirectText(node, script.Text);
        }
        script.Attributes = node.Attributes;
        script.ChildNodes = node.Children;
    }

    static void AttachScriptChildren(OdfNode node, OdfScript script)
    {
        if (script.Text.Length > 0)
        {
            StyleDocumentModel.AppendNode(node, CreateText(node, script.Text));
        }
        int index = 0;
        while (index < script.ChildNodes.Length)
        {
            OdfNode child = script.ChildNodes[index];
            if (child != null)
            {
                StyleDocumentModel.AppendNode(node, child);
            }
            index = index + 1;
        }
    }

    static void SynchronizePackageScript(
        OdfDocument document,
        OdfScript script)
    {
        if (document.Package == null
            || document.Package.EntryIndex == null)
        {
            throw new OdfDocumentException(
                "Package scripts require an editable package document.");
        }
        if (!script.PackagePath.StartsWith(
            OdfPartNames.ScriptsDirectory,
            StringComparison.Ordinal)
            || script.PackagePath.EndsWith("/", StringComparison.Ordinal))
        {
            throw new OdfDocumentException(
                "A package script requires a file below the Scripts directory.");
        }
        ZipEntryIndex entryIndex = document.Package.EntryIndex;
        bool isNew = script.OriginalPackagePath.Length == 0;
        if (isNew)
        {
            RequirePackageEdit(entryIndex.AddEntry(
                script.PackagePath,
                script.Data,
                OdfCompressionMethod.Deflated));
            script.IsContentAvailable = true;
        }
        else
        {
            if (script.IsEncrypted
                && (!SameBytes(script.Data, script.OriginalData)
                    || script.PackagePath != script.OriginalPackagePath))
            {
                throw new OdfDocumentException(
                    "Encrypted package scripts cannot be modified without decryption.");
            }
            if (script.PackagePath != script.OriginalPackagePath)
            {
                RequirePackageEdit(entryIndex.MoveEntry(
                    script.OriginalPackagePath,
                    script.PackagePath));
            }
            if (!SameBytes(script.Data, script.OriginalData))
            {
                RequirePackageEdit(entryIndex.ReplaceEntry(
                    script.PackagePath,
                    script.Data));
                script.IsContentAvailable = true;
            }
        }
        UpdateManifestMediaType(document, script);
    }

    static void RemoveDiscardedListeners(
        OdfEventListener[] original,
        OdfEventListener[] current)
    {
        int index = 0;
        while (index < original.Length)
        {
            OdfEventListener listener = original[index];
            if (!ContainsListener(current, listener)
                && listener.Node != null)
            {
                OdfNode? container = listener.Node.Parent;
                StyleDocumentModel.RemoveNodeFromParent(listener.Node);
                RemoveEmptyListenerContainer(container);
            }
            index = index + 1;
        }
    }

    static void SynchronizeCurrentListeners(
        OdfDocument document,
        OdfEventListener[] listeners)
    {
        int index = 0;
        while (index < listeners.Length)
        {
            OdfEventListener listener = listeners[index];
            if (listener == null)
            {
                throw new OdfDocumentException(
                    "Event-listener collections cannot contain null entries.");
            }
            SynchronizeEventListener(document, listener);
            RefreshEventClassification(document, listener);
            CaptureEvent(listener);
            index = index + 1;
        }
    }

    static void SynchronizeEventListener(
        OdfDocument document,
        OdfEventListener listener)
    {
        bool isNew = listener.Node == null;
        if (listener.Kind == OdfEventListenerKind.Unknown)
        {
            throw new OdfDocumentException(
                "An event listener requires a recognized kind.");
        }
        OdfNode node = listener.Node ?? CreateListenerNode(listener);
        if (isNew)
        {
            OdfNode owner = listener.OwnerNode
                ?? EnsureScriptsSection(document, listener.PartPath);
            OdfNode container = EnsureListenerContainer(owner);
            AttachListenerChildren(node, listener.ChildNodes);
            StyleDocumentModel.AppendNode(container, node);
            listener.Node = node;
            listener.ContainerNode = container;
            listener.OwnerNode = owner;
            listener.PartPath = container.Location.PartPath;
        }
        WriteChanged(node, OdfNamespaces.Script, "event-name",
            OdfNamespaces.ScriptPrefix, listener.EventName,
            listener.OriginalEventName, isNew);
        WriteChanged(node, OdfNamespaces.Script, "language",
            OdfNamespaces.ScriptPrefix, listener.Language,
            listener.OriginalLanguage, isNew);
        WriteChanged(node, OdfNamespaces.Script, "macro-name",
            OdfNamespaces.ScriptPrefix, listener.MacroName,
            listener.OriginalMacroName, isNew);
        WriteChanged(node, OdfNamespaces.XLink, "href",
            OdfNamespaces.XLinkPrefix, listener.Href,
            listener.OriginalHref, isNew);
        WriteChanged(node, OdfNamespaces.XLink, "type",
            OdfNamespaces.XLinkPrefix, listener.LinkType,
            listener.OriginalLinkType, isNew);
        WriteChanged(node, OdfNamespaces.XLink, "show",
            OdfNamespaces.XLinkPrefix, listener.Show,
            listener.OriginalShow, isNew);
        WriteChanged(node, OdfNamespaces.XLink, "actuate",
            OdfNamespaces.XLinkPrefix, listener.Actuate,
            listener.OriginalActuate, isNew);
        WriteChanged(node, OdfNamespaces.Presentation, "action",
            OdfNamespaces.PresentationPrefix, listener.Action,
            listener.OriginalAction, isNew);
        WriteChanged(node, OdfNamespaces.Presentation, "effect",
            OdfNamespaces.PresentationPrefix, listener.Effect,
            listener.OriginalEffect, isNew);
        WriteChanged(node, OdfNamespaces.Presentation, "direction",
            OdfNamespaces.PresentationPrefix, listener.Direction,
            listener.OriginalDirection, isNew);
        WriteChanged(node, OdfNamespaces.Presentation, "speed",
            OdfNamespaces.PresentationPrefix, listener.Speed,
            listener.OriginalSpeed, isNew);
        WriteChanged(node, OdfNamespaces.Presentation, "start-scale",
            OdfNamespaces.PresentationPrefix, listener.StartScale,
            listener.OriginalStartScale, isNew);
        WriteChanged(node, OdfNamespaces.Presentation, "verb",
            OdfNamespaces.PresentationPrefix, listener.Verb,
            listener.OriginalVerb, isNew);
        listener.Attributes = node.Attributes;
        listener.ChildNodes = node.Children;
    }

    static OdfNode CreateListenerNode(OdfEventListener listener)
    {
        string namespaceName = listener.Kind == OdfEventListenerKind.Script
            ? OdfNamespaces.Script
            : OdfNamespaces.Presentation;
        string prefix = listener.Kind == OdfEventListenerKind.Script
            ? OdfNamespaces.ScriptPrefix
            : OdfNamespaces.PresentationPrefix;
        OdfNode result = CreateElement(
            namespaceName,
            "event-listener",
            prefix,
            listener.PartPath);
        result.NamespaceDeclarations =
        [
            NamespaceDeclaration(
                OdfNamespaces.ScriptPrefix,
                OdfNamespaces.Script,
                listener.PartPath),
            NamespaceDeclaration(
                OdfNamespaces.PresentationPrefix,
                OdfNamespaces.Presentation,
                listener.PartPath),
            NamespaceDeclaration(
                OdfNamespaces.XLinkPrefix,
                OdfNamespaces.XLink,
                listener.PartPath)
        ];
        return result;
    }

    static void AttachListenerChildren(
        OdfNode listener,
        OdfNode[] children)
    {
        int index = 0;
        while (index < children.Length)
        {
            if (children[index] != null)
            {
                StyleDocumentModel.AppendNode(listener, children[index]);
            }
            index = index + 1;
        }
    }

    static OdfNode EnsureScriptsSection(
        OdfDocument document,
        string requestedPartPath)
    {
        OdfPart? part = FindScriptPart(document, requestedPartPath);
        if (part == null || part.Root == null)
        {
            throw new OdfDocumentException(
                "An embedded script requires a materialized content or flat XML part.");
        }
        if (part.Scripts != null)
        {
            return part.Scripts;
        }
        OdfNode section = CreateElement(
            OdfNamespaces.Office,
            "scripts",
            OdfNamespaces.OfficePrefix,
            part.Path);
        section.NamespaceDeclarations =
        [
            NamespaceDeclaration(
                OdfNamespaces.ScriptPrefix,
                OdfNamespaces.Script,
                part.Path)
        ];
        StyleDocumentModel.InsertSection(
            part.Root,
            section,
            StyleDocumentModel.SectionRank("scripts"));
        StyleDocumentModel.RefreshPartMapping(document, part);
        return section;
    }

    static OdfPart? FindScriptPart(
        OdfDocument document,
        string requestedPartPath)
    {
        OdfPart? fallback = null;
        int index = 0;
        while (index < document.Parts.Length)
        {
            OdfPart part = document.Parts[index];
            if (part.Path == requestedPartPath
                && part.Root != null)
            {
                return part;
            }
            if (fallback == null
                && part.Root != null
                && (part.IsFlatDocument
                    || part.Path == OdfPartNames.Content))
            {
                fallback = part;
            }
            index = index + 1;
        }
        return fallback;
    }

    static OdfNode EnsureListenerContainer(OdfNode owner)
    {
        int index = 0;
        while (index < owner.Children.Length)
        {
            OdfNode child = owner.Children[index];
            if (IsElement(
                child,
                OdfNamespaces.Office,
                "event-listeners"))
            {
                return child;
            }
            index = index + 1;
        }
        OdfNode result = CreateElement(
            OdfNamespaces.Office,
            "event-listeners",
            OdfNamespaces.OfficePrefix,
            owner.Location.PartPath);
        StyleDocumentModel.AppendNode(owner, result);
        return result;
    }

    static void InsertEmbeddedScript(OdfNode section, OdfNode script)
    {
        int insertion = section.Children.Length;
        int index = 0;
        while (index < section.Children.Length)
        {
            if (IsElement(
                section.Children[index],
                OdfNamespaces.Office,
                "event-listeners"))
            {
                insertion = index;
                index = section.Children.Length;
            }
            else
            {
                index = index + 1;
            }
        }
        OdfNode[] children = new OdfNode[section.Children.Length + 1];
        index = 0;
        while (index < insertion)
        {
            children[index] = section.Children[index];
            index = index + 1;
        }
        children[insertion] = script;
        script.Parent = section;
        script.Location.PartPath = section.Location.PartPath;
        index = insertion;
        while (index < section.Children.Length)
        {
            children[index + 1] = section.Children[index];
            index = index + 1;
        }
        section.Children = children;
        section.WasEmptyElement = false;
    }

    static void RemoveEmptyListenerContainer(OdfNode? container)
    {
        if (container != null
            && IsElement(
                container,
                OdfNamespaces.Office,
                "event-listeners")
            && container.Children.Length == 0)
        {
            StyleDocumentModel.RemoveNodeFromParent(container);
        }
    }

    static void RefreshEventClassification(
        OdfDocument document,
        OdfEventListener listener)
    {
        listener.IsExternal = IsExternal(document, listener.Href);
        listener.IsActiveContent = false;
        listener.Classification = OdfActiveContentClassification.Inert;
        if (listener.Href.Length > 0)
        {
            listener.IsActiveContent = true;
            listener.Classification = listener.IsExternal
                ? OdfActiveContentClassification.ExternalReference
                : OdfActiveContentClassification.LinkedScript;
            return;
        }
        if (listener.Kind == OdfEventListenerKind.Script
            && listener.MacroName.Length > 0)
        {
            listener.IsActiveContent = true;
            listener.Classification =
                OdfActiveContentClassification.MacroInvocation;
            return;
        }
        if (listener.Kind == OdfEventListenerKind.Presentation
            && listener.Action.Length > 0
            && listener.Action != "none")
        {
            listener.IsActiveContent = true;
            listener.Classification =
                OdfActiveContentClassification.PresentationAction;
        }
    }

    static bool IsExternal(OdfDocument document, string href)
    {
        if (href.Length == 0)
        {
            return false;
        }
        if (OdfIriResolver.IsAbsoluteIri(href)
            || href.StartsWith("//", StringComparison.Ordinal))
        {
            return true;
        }
        return document.PackageKind == OdfPackageKind.Flat
            && !href.StartsWith("#", StringComparison.Ordinal);
    }

    static OdfEventListenerKind EventKind(OdfNode node)
    {
        if (IsElement(node, OdfNamespaces.Script, "event-listener"))
        {
            return OdfEventListenerKind.Script;
        }
        if (IsElement(
            node,
            OdfNamespaces.Presentation,
            "event-listener"))
        {
            return OdfEventListenerKind.Presentation;
        }
        return OdfEventListenerKind.Unknown;
    }

    static bool IsElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == namespaceName
            && node.Name.LocalName == localName;
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

    static string DirectText(OdfNode node)
    {
        string result = "";
        int index = 0;
        while (index < node.Children.Length)
        {
            if (node.Children[index].Kind == OdfNodeKind.Text)
            {
                result = result + node.Children[index].Value;
            }
            index = index + 1;
        }
        return result;
    }

    static void SetDirectText(OdfNode node, string text)
    {
        List<OdfNode> children = new List<OdfNode>();
        bool inserted = false;
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (child.Kind == OdfNodeKind.Text)
            {
                if (!inserted && text.Length > 0)
                {
                    children.Add(CreateText(node, text));
                    inserted = true;
                }
                child.Parent = null;
            }
            else
            {
                children.Add(child);
            }
            index = index + 1;
        }
        if (!inserted && text.Length > 0)
        {
            children.Add(CreateText(node, text));
        }
        node.Children = children.ToArray();
    }

    static OdfNode CreateText(OdfNode parent, string text)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Text;
        result.Value = text;
        result.Parent = parent;
        result.Location.PartPath = parent.Location.PartPath;
        return result;
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
        return result;
    }

    static OdfNamespaceDeclaration NamespaceDeclaration(
        string prefix,
        string namespaceName,
        string partPath)
    {
        OdfNamespaceDeclaration result = new OdfNamespaceDeclaration();
        result.Prefix = prefix;
        result.NamespaceName = namespaceName;
        result.Location.PartPath = partPath;
        return result;
    }

    static void WriteChanged(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value,
        string original,
        bool force)
    {
        if (!force && value == original)
        {
            return;
        }
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

    static void RemovePackageScript(
        OdfDocument document,
        string path)
    {
        if (document.Package == null
            || document.Package.EntryIndex == null)
        {
            return;
        }
        RequirePackageEdit(document.Package.EntryIndex.RemoveEntry(path));
    }

    static void RequirePackageEdit(OdfEditResult result)
    {
        if (result.Status != OdfEditStatus.Success
            && result.Status != OdfEditStatus.NoChange)
        {
            throw new OdfDocumentException(result.Message);
        }
    }

    static void UpdateManifestMediaType(
        OdfDocument document,
        OdfScript script)
    {
        if (script.MediaType.Length == 0 || document.Manifest == null)
        {
            return;
        }
        int index = 0;
        while (index < document.Manifest.Entries.Length)
        {
            OdfManifestEntry entry = document.Manifest.Entries[index];
            if (entry.FullPath == script.PackagePath)
            {
                entry.MediaType = script.MediaType;
                return;
            }
            index = index + 1;
        }
    }

    static void RefreshPackage(OdfDocument document)
    {
        if (document.Package != null
            && document.Package.EntryIndex != null)
        {
            PackageApiModel.RefreshPackage(document.Package);
            document.PackageEntries = document.Package.Entries;
            document.Manifest = document.Package.Manifest;
        }
    }

    static EditablePackageEntry? FindEditableEntry(
        EditablePackageEntry[] entries,
        string path)
    {
        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index].Entry.Path == path)
            {
                return entries[index];
            }
            index = index + 1;
        }
        return null;
    }

    static bool ContainsScript(OdfScript[] scripts, OdfScript target)
    {
        int index = 0;
        while (index < scripts.Length)
        {
            if (scripts[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ContainsListener(
        OdfEventListener[] listeners,
        OdfEventListener target)
    {
        int index = 0;
        while (index < listeners.Length)
        {
            if (listeners[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool SameBytes(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }
        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    static void CaptureScript(OdfScript script)
    {
        script.OriginalLanguage = script.Language;
        script.OriginalText = script.Text;
        script.OriginalPackagePath = script.PackagePath;
        script.OriginalMediaType = script.MediaType;
        script.OriginalData = CopyBytes(script.Data);
    }

    static void CaptureEvent(OdfEventListener listener)
    {
        listener.OriginalEventName = listener.EventName;
        listener.OriginalLanguage = listener.Language;
        listener.OriginalMacroName = listener.MacroName;
        listener.OriginalHref = listener.Href;
        listener.OriginalLinkType = listener.LinkType;
        listener.OriginalShow = listener.Show;
        listener.OriginalActuate = listener.Actuate;
        listener.OriginalAction = listener.Action;
        listener.OriginalEffect = listener.Effect;
        listener.OriginalDirection = listener.Direction;
        listener.OriginalSpeed = listener.Speed;
        listener.OriginalStartScale = listener.StartScale;
        listener.OriginalVerb = listener.Verb;
    }
}
