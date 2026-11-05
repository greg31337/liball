using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace odf;

internal class RepairEngine
{
    List<RepairProposal> Proposals = new List<RepairProposal>();
    List<RepairMutation> Mutations = new List<RepairMutation>();
    List<OdfDiagnostic> Diagnostics = new List<OdfDiagnostic>();
    int ConflictCount;

    internal RepairAnalysisResult ProposeRepairs(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfValidationException(
                "A document is required for repair analysis.");
        }
        Proposals = new List<RepairProposal>();
        AnalyzePackage(document);
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            AnalyzeNamespaces(document, part);
            AnalyzeIds(document, part);
            partIndex = partIndex + 1;
        }
        RepairAnalysisResult result = new RepairAnalysisResult();
        result.Proposals = Proposals.ToArray();
        int index = 0;
        while (index < result.Proposals.Length)
        {
            if (result.Proposals[index].IsSafe)
            {
                result.SafeProposalCount = result.SafeProposalCount + 1;
            }
            else
            {
                result.UnsafeProposalCount =
                    result.UnsafeProposalCount + 1;
            }
            index = index + 1;
        }
        return result;
    }

    internal RepairApplicationResult ApplyRepairs(
        OdfDocument document,
        RepairProposal[] proposals)
    {
        if (document == null || proposals == null)
        {
            throw new OdfValidationException(
                "A document and repair proposals are required.");
        }
        Mutations = new List<RepairMutation>();
        Diagnostics = new List<OdfDiagnostic>();
        ConflictCount = 0;
        int applied = 0;
        int skipped = 0;
        int index = 0;
        while (index < proposals.Length)
        {
            RepairProposal proposal = proposals[index];
            if (proposal == null
                || proposal.Document != document
                || proposal.IsApplied)
            {
                AddConflict(
                    proposal,
                    "The repair proposal does not apply to the current document state.");
                skipped = skipped + 1;
            }
            else if (!proposal.IsSafe)
            {
                skipped = skipped + 1;
            }
            else if (ApplyProposal(document, proposal))
            {
                proposal.IsApplied = true;
                applied = applied + 1;
            }
            else
            {
                skipped = skipped + 1;
            }
            index = index + 1;
        }
        RepairApplicationResult result = new RepairApplicationResult();
        result.Changed = Mutations.Count > 0;
        result.AppliedProposalCount = applied;
        result.SkippedProposalCount = skipped;
        result.Mutations = Mutations.ToArray();
        result.Diagnostics = Diagnostics.ToArray();
        if (ConflictCount > 0)
        {
            result.Status = OdfEditStatus.Conflict;
        }
        else if (result.Changed)
        {
            result.Status = OdfEditStatus.Success;
        }
        else
        {
            result.Status = OdfEditStatus.NoChange;
        }
        return result;
    }

    void AnalyzePackage(OdfDocument document)
    {
        OdfPackage? package = document.Package;
        if (package == null || package.EntryIndex == null)
        {
            return;
        }
        string mediaType = SelectReliableMediaType(document, package);
        if (MimetypeNeedsRepair(package, mediaType))
        {
            RepairProposal proposal = CreateProposal(
                document,
                RepairKind.PackageMimetype,
                "Normalize or restore the package mimetype entry.",
                PartLocation(OdfPartNames.Mimetype),
                mediaType.Length > 0,
                "",
                mediaType);
            proposal.Package = package;
        }
        bool hasManifestEntry = HasPackageEntry(
            package,
            OdfPartNames.Manifest);
        if (package.Manifest == null || !hasManifestEntry)
        {
            RepairProposal proposal = CreateProposal(
                document,
                RepairKind.PackageManifest,
                "Create or restore META-INF/manifest.xml from package state.",
                PartLocation(OdfPartNames.Manifest),
                mediaType.Length > 0,
                "",
                mediaType);
            proposal.Package = package;
        }
        else if (ManifestNeedsSynchronization(package, mediaType))
        {
            RepairProposal proposal = CreateProposal(
                document,
                RepairKind.ManifestSynchronization,
                "Synchronize manifest entries and media types with package state.",
                PartLocation(OdfPartNames.Manifest),
                true,
                "out-of-sync",
                "synchronized");
            proposal.Package = package;
        }
    }

    bool MimetypeNeedsRepair(OdfPackage package, string mediaType)
    {
        OdfPackageEntry? entry = FindPackageEntry(
            package,
            OdfPartNames.Mimetype);
        if (entry == null)
        {
            return true;
        }
        if (!entry.IsFirstEntry
            || entry.CompressionMethod != OdfCompressionMethod.Stored
            || entry.LocalExtraFieldLength != 0
            || entry.CentralExtraFieldLength != 0)
        {
            return true;
        }
        if (mediaType.Length == 0 || package.EntryIndex == null)
        {
            return false;
        }
        string current = ReadEntryText(
            package.EntryIndex,
            OdfPartNames.Mimetype);
        return current != mediaType;
    }

    bool ManifestNeedsSynchronization(
        OdfPackage package,
        string mediaType)
    {
        if (package.Manifest == null)
        {
            return true;
        }
        OdfManifest manifest = package.Manifest;
        int rootCount = 0;
        Dictionary<string, bool> paths =
            new Dictionary<string, bool>(StringComparer.Ordinal);
        int manifestIndex = 0;
        while (manifestIndex < manifest.Entries.Length)
        {
            OdfManifestEntry entry = manifest.Entries[manifestIndex];
            if (paths.ContainsKey(entry.FullPath))
            {
                return true;
            }
            paths.Add(entry.FullPath, true);
            if (entry.FullPath == OdfPartNames.Root)
            {
                rootCount = rootCount + 1;
                if (mediaType.Length > 0
                    && entry.MediaType != mediaType)
                {
                    return true;
                }
            }
            else if (entry.FullPath == OdfPartNames.Mimetype
                || entry.FullPath == OdfPartNames.Manifest
                || !ManifestTargetExists(package, entry.FullPath))
            {
                return true;
            }
            manifestIndex = manifestIndex + 1;
        }
        if (rootCount != 1)
        {
            return true;
        }
        int packageIndex = 0;
        while (packageIndex < package.Entries.Length)
        {
            OdfPackageEntry entry = package.Entries[packageIndex];
            if (!entry.IsDirectory
                && entry.Path != OdfPartNames.Mimetype
                && entry.Path != OdfPartNames.Manifest
                && !paths.ContainsKey(entry.Path))
            {
                return true;
            }
            packageIndex = packageIndex + 1;
        }
        return false;
    }

    void AnalyzeNamespaces(OdfDocument document, OdfPart part)
    {
        OdfNode[] nodes = new OdfTreeEditor(part.Nodes).TraverseAllNodes();
        int index = 0;
        while (index < nodes.Length)
        {
            OdfNode node = nodes[index];
            if (node.Name != null)
            {
                AnalyzeQualifiedName(
                    document,
                    part,
                    node,
                    node.Name);
            }
            int attributeIndex = 0;
            while (attributeIndex < node.Attributes.Length)
            {
                AnalyzeQualifiedName(
                    document,
                    part,
                    node,
                    node.Attributes[attributeIndex].Name);
                attributeIndex = attributeIndex + 1;
            }
            index = index + 1;
        }
    }

    void AnalyzeQualifiedName(
        OdfDocument document,
        OdfPart part,
        OdfNode node,
        OdfQualifiedName name)
    {
        if (name.Prefix.Length == 0
            || name.NamespaceName.Length == 0
            || name.Prefix == OdfNamespaces.XmlPrefix)
        {
            return;
        }
        string binding = FindNamespaceBinding(node, name.Prefix);
        if (binding == name.NamespaceName)
        {
            return;
        }
        RepairProposal? existing = FindNamespaceProposal(
            node,
            name.Prefix);
        if (existing != null)
        {
            if (existing.NewValue != name.NamespaceName)
            {
                existing.IsSafe = false;
                existing.Description =
                    "The same prefix is used for conflicting namespace names.";
            }
            return;
        }
        RepairProposal proposal = CreateProposal(
            document,
            RepairKind.NamespaceBinding,
            "Declare the namespace binding required by a qualified name.",
            NodeLocation(node, part.Path),
            true,
            binding,
            name.NamespaceName);
        proposal.Part = part;
        proposal.Node = node;
        proposal.Prefix = name.Prefix;
    }

    void AnalyzeIds(OdfDocument document, OdfPart part)
    {
        OdfNode[] nodes = new OdfTreeEditor(part.Nodes).TraverseAllNodes();
        Dictionary<string, int> counts = CountXmlIds(nodes);
        HashSet<string> reserved = ReserveXmlIds(nodes);
        Dictionary<string, int> occurrences =
            new Dictionary<string, int>(StringComparer.Ordinal);
        int index = 0;
        while (index < nodes.Length)
        {
            OdfNode node = nodes[index];
            OdfAttribute? attribute = FindXmlIdAttribute(node);
            if (attribute != null)
            {
                AnalyzeXmlIdAttribute(
                    document,
                    part,
                    node,
                    attribute,
                    counts,
                    reserved,
                    occurrences);
            }
            else if (node.XmlId.Length > 0)
            {
                AnalyzeCachedXmlId(
                    document,
                    part,
                    node,
                    counts,
                    reserved);
            }
            AnalyzeBrokenReferences(document, part, node, counts);
            index = index + 1;
        }
    }

    void AnalyzeXmlIdAttribute(
        OdfDocument document,
        OdfPart part,
        OdfNode node,
        OdfAttribute attribute,
        Dictionary<string, int> counts,
        HashSet<string> reserved,
        Dictionary<string, int> occurrences)
    {
        string value = attribute.Value;
        if (node.XmlId != value)
        {
            RepairProposal cache = CreateProposal(
                document,
                RepairKind.XmlIdCache,
                "Synchronize the XML ID cache with the xml:id attribute.",
                AttributeLocation(attribute, node, part.Path),
                true,
                node.XmlId,
                value);
            cache.Part = part;
            cache.Node = node;
            cache.Attribute = attribute;
        }
        int occurrence = 1;
        if (occurrences.ContainsKey(value))
        {
            occurrence = occurrences[value] + 1;
        }
        occurrences[value] = occurrence;
        bool valid = OdfNameRules.IsXmlId(value);
        bool unique = counts.ContainsKey(value) && counts[value] == 1;
        if (!valid || !unique && occurrence > 1)
        {
            string replacement = GenerateUniqueXmlId(reserved);
            bool safe = !valid && unique;
            RepairProposal proposal = CreateProposal(
                document,
                RepairKind.XmlIdValue,
                safe
                    ? "Replace an invalid unique xml:id and update its references."
                    : "Rename an ambiguous duplicate xml:id.",
                AttributeLocation(attribute, node, part.Path),
                safe,
                value,
                replacement);
            proposal.Part = part;
            proposal.Node = node;
            proposal.Attribute = attribute;
            if (safe)
            {
                proposal.RelatedAttributes = FindReferences(part, value);
            }
        }
    }

    void AnalyzeCachedXmlId(
        OdfDocument document,
        OdfPart part,
        OdfNode node,
        Dictionary<string, int> counts,
        HashSet<string> reserved)
    {
        string value = node.XmlId;
        bool safeToAdd = OdfNameRules.IsXmlId(value)
            && !counts.ContainsKey(value)
            && !reserved.Contains(value);
        string newValue = safeToAdd ? value : "";
        RepairProposal proposal = CreateProposal(
            document,
            RepairKind.XmlIdCache,
            safeToAdd
                ? "Create the missing xml:id attribute from its valid cache."
                : "Clear an invalid or ambiguous XML ID cache value.",
            NodeLocation(node, part.Path),
            true,
            value,
            newValue);
        proposal.Part = part;
        proposal.Node = node;
        if (safeToAdd)
        {
            reserved.Add(value);
        }
    }

    void AnalyzeBrokenReferences(
        OdfDocument document,
        OdfPart part,
        OdfNode node,
        Dictionary<string, int> counts)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            string value = attribute.Value.Trim();
            if (IsGenericIdReference(attribute.Name.LocalName)
                && value.Length > 1
                && value[0] == '#')
            {
                string target = value.Substring(1);
                if (counts.ContainsKey(target) && counts[target] == 1)
                {
                    RepairProposal proposal = CreateProposal(
                        document,
                        RepairKind.BrokenReference,
                        "Remove a fragment marker from a local ID reference.",
                        AttributeLocation(attribute, node, part.Path),
                        true,
                        attribute.Value,
                        target);
                    proposal.Part = part;
                    proposal.Node = node;
                    proposal.Attribute = attribute;
                }
            }
            index = index + 1;
        }
    }

    bool ApplyProposal(OdfDocument document, RepairProposal proposal)
    {
        if (proposal.Kind == RepairKind.PackageMimetype)
        {
            return ApplyMimetype(document, proposal);
        }
        if (proposal.Kind == RepairKind.PackageManifest)
        {
            return ApplyManifest(document, proposal);
        }
        if (proposal.Kind == RepairKind.ManifestSynchronization)
        {
            return ApplyManifestSynchronization(document, proposal);
        }
        if (proposal.Kind == RepairKind.NamespaceBinding)
        {
            return ApplyNamespace(proposal);
        }
        if (proposal.Kind == RepairKind.XmlIdCache)
        {
            return ApplyXmlIdCache(proposal);
        }
        if (proposal.Kind == RepairKind.XmlIdValue)
        {
            return ApplyXmlIdValue(proposal);
        }
        if (proposal.Kind == RepairKind.BrokenReference)
        {
            return ApplyBrokenReference(proposal);
        }
        AddConflict(proposal, "The repair proposal kind is not supported.");
        return false;
    }

    bool ApplyMimetype(OdfDocument document, RepairProposal proposal)
    {
        if (proposal.Package == null || proposal.Package.EntryIndex == null)
        {
            AddConflict(proposal, "The package is unavailable for mimetype repair.");
            return false;
        }
        OdfEditResult edit = proposal.Package.EntryIndex
            .RepairMimetypeEntry(proposal.NewValue);
        if (edit.Status != OdfEditStatus.Success
            && edit.Status != OdfEditStatus.NoChange)
        {
            AddConflict(proposal, edit.Message);
            return false;
        }
        proposal.Package.MediaType = proposal.NewValue;
        document.MediaType = proposal.NewValue;
        RefreshPackageDocument(document, proposal.Package);
        AddMutation(proposal, proposal.OldValue, proposal.NewValue);
        return true;
    }

    bool ApplyManifest(OdfDocument document, RepairProposal proposal)
    {
        if (proposal.Package == null || proposal.Package.EntryIndex == null)
        {
            AddConflict(proposal, "The package is unavailable for manifest repair.");
            return false;
        }
        OdfManifest manifest = proposal.Package.Manifest
            ?? CreateManifest(document, proposal.NewValue);
        OdfEditResult edit = proposal.Package.EntryIndex
            .RepairManifestEntry(manifest);
        if (edit.Status != OdfEditStatus.Success
            && edit.Status != OdfEditStatus.NoChange)
        {
            AddConflict(proposal, edit.Message);
            return false;
        }
        proposal.Package.Manifest = manifest;
        RefreshPackageDocument(document, proposal.Package);
        AddMutation(proposal, proposal.OldValue, "manifest.xml");
        return true;
    }

    bool ApplyManifestSynchronization(
        OdfDocument document,
        RepairProposal proposal)
    {
        if (proposal.Package == null || proposal.Package.EntryIndex == null)
        {
            AddConflict(proposal, "The package is unavailable for manifest synchronization.");
            return false;
        }
        OdfEditResult edit = proposal.Package.EntryIndex.SynchronizeManifest();
        if (edit.Status != OdfEditStatus.Success
            && edit.Status != OdfEditStatus.NoChange)
        {
            AddConflict(proposal, edit.Message);
            return false;
        }
        RefreshPackageDocument(document, proposal.Package);
        AddMutation(proposal, proposal.OldValue, proposal.NewValue);
        return true;
    }

    bool ApplyNamespace(RepairProposal proposal)
    {
        if (proposal.Node == null)
        {
            AddConflict(proposal, "The namespace repair target is unavailable.");
            return false;
        }
        string current = FindNamespaceBinding(
            proposal.Node,
            proposal.Prefix);
        if (current != proposal.OldValue)
        {
            AddConflict(proposal, "The namespace binding changed after analysis.");
            return false;
        }
        SetLocalNamespace(
            proposal.Node,
            proposal.Prefix,
            proposal.NewValue);
        AddMutation(proposal, current, proposal.NewValue);
        return true;
    }

    bool ApplyXmlIdCache(RepairProposal proposal)
    {
        if (proposal.Node == null || proposal.Part == null)
        {
            AddConflict(proposal, "The XML ID repair target is unavailable.");
            return false;
        }
        if (proposal.Attribute != null)
        {
            if (proposal.Node.XmlId != proposal.OldValue
                || proposal.Attribute.Value != proposal.NewValue)
            {
                AddConflict(proposal, "The XML ID cache changed after analysis.");
                return false;
            }
            proposal.Node.XmlId = proposal.NewValue;
            proposal.Node.Location.XmlId = proposal.NewValue;
        }
        else if (proposal.Node.XmlId != proposal.OldValue
            || FindXmlIdAttribute(proposal.Node) != null)
        {
            AddConflict(proposal, "The cached XML ID changed after analysis.");
            return false;
        }
        else if (proposal.NewValue.Length > 0)
        {
            OdfTreeEditor editor = new OdfTreeEditor(proposal.Part.Nodes);
            editor.SetAttribute(
                proposal.Node,
                XmlIdName(),
                proposal.NewValue);
        }
        else
        {
            proposal.Node.XmlId = "";
            proposal.Node.Location.XmlId = "";
        }
        AddMutation(proposal, proposal.OldValue, proposal.NewValue);
        return true;
    }

    bool ApplyXmlIdValue(RepairProposal proposal)
    {
        if (proposal.Node == null
            || proposal.Part == null
            || proposal.Attribute == null
            || proposal.Attribute.Value != proposal.OldValue)
        {
            AddConflict(proposal, "The xml:id value changed after analysis.");
            return false;
        }
        OdfTreeEditor editor = new OdfTreeEditor(proposal.Part.Nodes);
        editor.SetAttribute(
            proposal.Node,
            XmlIdName(),
            proposal.NewValue);
        AddMutation(proposal, proposal.OldValue, proposal.NewValue);
        int index = 0;
        while (index < proposal.RelatedAttributes.Length)
        {
            OdfAttribute reference = proposal.RelatedAttributes[index];
            string before = reference.Value;
            string after = ReplaceReferenceValue(
                reference,
                proposal.OldValue,
                proposal.NewValue);
            if (before != after)
            {
                reference.Value = after;
                AddRelatedMutation(proposal, reference, before, after);
            }
            index = index + 1;
        }
        return true;
    }

    bool ApplyBrokenReference(RepairProposal proposal)
    {
        if (proposal.Attribute == null
            || proposal.Attribute.Value != proposal.OldValue)
        {
            AddConflict(proposal, "The reference value changed after analysis.");
            return false;
        }
        proposal.Attribute.Value = proposal.NewValue;
        AddMutation(proposal, proposal.OldValue, proposal.NewValue);
        return true;
    }

    RepairProposal CreateProposal(
        OdfDocument document,
        RepairKind kind,
        string description,
        OdfLocation location,
        bool safe,
        string oldValue,
        string newValue)
    {
        RepairProposal result = new RepairProposal();
        result.Id = "repair-" + (Proposals.Count + 1);
        result.Kind = kind;
        result.Description = description;
        result.Location = OdfDiagnosticFactory.CopyLocation(location);
        result.IsSafe = safe;
        result.OldValue = oldValue;
        result.NewValue = newValue;
        result.Document = document;
        Proposals.Add(result);
        return result;
    }

    RepairProposal? FindNamespaceProposal(OdfNode node, string prefix)
    {
        int index = 0;
        while (index < Proposals.Count)
        {
            RepairProposal proposal = Proposals[index];
            if (proposal.Kind == RepairKind.NamespaceBinding
                && proposal.Node == node
                && proposal.Prefix == prefix)
            {
                return proposal;
            }
            index = index + 1;
        }
        return null;
    }

    Dictionary<string, int> CountXmlIds(OdfNode[] nodes)
    {
        Dictionary<string, int> result =
            new Dictionary<string, int>(StringComparer.Ordinal);
        int index = 0;
        while (index < nodes.Length)
        {
            OdfAttribute? attribute = FindXmlIdAttribute(nodes[index]);
            if (attribute != null)
            {
                if (!result.ContainsKey(attribute.Value))
                {
                    result.Add(attribute.Value, 0);
                }
                result[attribute.Value] = result[attribute.Value] + 1;
            }
            index = index + 1;
        }
        return result;
    }

    HashSet<string> ReserveXmlIds(OdfNode[] nodes)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < nodes.Length)
        {
            OdfAttribute? attribute = FindXmlIdAttribute(nodes[index]);
            if (attribute != null)
            {
                result.Add(attribute.Value);
            }
            index = index + 1;
        }
        return result;
    }

    string GenerateUniqueXmlId(HashSet<string> reserved)
    {
        int suffix = 1;
        string candidate = "repair-id-1";
        while (reserved.Contains(candidate))
        {
            suffix = suffix + 1;
            candidate = "repair-id-" + suffix;
        }
        reserved.Add(candidate);
        return candidate;
    }

    OdfAttribute[] FindReferences(OdfPart part, string target)
    {
        List<OdfAttribute> result = new List<OdfAttribute>();
        OdfNode[] nodes = new OdfTreeEditor(part.Nodes).TraverseAllNodes();
        int nodeIndex = 0;
        while (nodeIndex < nodes.Length)
        {
            int attributeIndex = 0;
            while (attributeIndex < nodes[nodeIndex].Attributes.Length)
            {
                OdfAttribute attribute =
                    nodes[nodeIndex].Attributes[attributeIndex];
                if (ReferenceTargets(attribute, target))
                {
                    result.Add(attribute);
                }
                attributeIndex = attributeIndex + 1;
            }
            nodeIndex = nodeIndex + 1;
        }
        return result.ToArray();
    }

    bool ReferenceTargets(OdfAttribute attribute, string target)
    {
        string value = attribute.Value.Trim();
        if (attribute.Name.NamespaceName == OdfNamespaces.XLink
            && attribute.Name.LocalName == "href")
        {
            return value == "#" + target;
        }
        return IsGenericIdReference(attribute.Name.LocalName)
            && value == target;
    }

    string ReplaceReferenceValue(
        OdfAttribute attribute,
        string oldValue,
        string newValue)
    {
        string value = attribute.Value.Trim();
        if (attribute.Name.NamespaceName == OdfNamespaces.XLink
            && attribute.Name.LocalName == "href"
            && value == "#" + oldValue)
        {
            return "#" + newValue;
        }
        if (IsGenericIdReference(attribute.Name.LocalName)
            && value == oldValue)
        {
            return newValue;
        }
        return attribute.Value;
    }

    string FindNamespaceBinding(OdfNode node, string prefix)
    {
        OdfNode? current = node;
        while (current != null)
        {
            int index = current.NamespaceDeclarations.Length - 1;
            while (index >= 0)
            {
                OdfNamespaceDeclaration declaration =
                    current.NamespaceDeclarations[index];
                if (declaration.Prefix == prefix)
                {
                    return declaration.NamespaceName;
                }
                index = index - 1;
            }
            current = current.Parent;
        }
        if (prefix == OdfNamespaces.XmlPrefix)
        {
            return OdfNamespaces.Xml;
        }
        return "";
    }

    void SetLocalNamespace(
        OdfNode node,
        string prefix,
        string namespaceName)
    {
        int index = 0;
        while (index < node.NamespaceDeclarations.Length)
        {
            if (node.NamespaceDeclarations[index].Prefix == prefix)
            {
                node.NamespaceDeclarations[index].NamespaceName =
                    namespaceName;
                return;
            }
            index = index + 1;
        }
        OdfNamespaceDeclaration[] declarations =
            new OdfNamespaceDeclaration[
                node.NamespaceDeclarations.Length + 1];
        Array.Copy(
            node.NamespaceDeclarations,
            declarations,
            node.NamespaceDeclarations.Length);
        declarations[declarations.Length - 1] =
            new OdfNamespaceDeclaration
            {
                Prefix = prefix,
                NamespaceName = namespaceName
            };
        node.NamespaceDeclarations = declarations;
    }

    OdfAttribute? FindXmlIdAttribute(OdfNode node)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == OdfNamespaces.Xml
                && attribute.Name.LocalName == "id")
            {
                return attribute;
            }
            index = index + 1;
        }
        return null;
    }

    OdfQualifiedName XmlIdName()
    {
        OdfQualifiedName result = new OdfQualifiedName();
        result.NamespaceName = OdfNamespaces.Xml;
        result.LocalName = "id";
        result.Prefix = OdfNamespaces.XmlPrefix;
        return result;
    }

    bool IsGenericIdReference(string localName)
    {
        return localName == "start-shape"
            || localName == "end-shape"
            || localName == "shape-id"
            || localName == "target-id"
            || localName == "idref"
            || localName == "for"
            || localName == "control"
            || localName == "start-page"
            || localName == "ref-name";
    }

    string SelectReliableMediaType(
        OdfDocument document,
        OdfPackage package)
    {
        string result = "";
        result = SelectMatchingMediaType(result, document.MediaType);
        if (result == "#conflict")
        {
            return "";
        }
        result = SelectMatchingMediaType(result, package.MediaType);
        if (result == "#conflict")
        {
            return "";
        }
        if (package.Manifest != null)
        {
            int index = 0;
            while (index < package.Manifest.Entries.Length)
            {
                OdfManifestEntry entry = package.Manifest.Entries[index];
                if (entry.FullPath == OdfPartNames.Root)
                {
                    result = SelectMatchingMediaType(
                        result,
                        entry.MediaType);
                    if (result == "#conflict")
                    {
                        return "";
                    }
                }
                index = index + 1;
            }
        }
        return result;
    }

    string SelectMatchingMediaType(string current, string candidate)
    {
        if (candidate.Length == 0)
        {
            return current;
        }
        OdfMediaTypeValue parsed = OdfMediaTypeRules.ParseMediaType(candidate);
        if (!parsed.IsValid
            || !candidate.StartsWith(
                "application/vnd.oasis.opendocument.",
                StringComparison.Ordinal))
        {
            return current;
        }
        if (current.Length == 0 || current == candidate)
        {
            return candidate;
        }
        return "#conflict";
    }

    OdfManifest CreateManifest(OdfDocument document, string mediaType)
    {
        OdfManifest result = new OdfManifest();
        result.Version = VersionText(document.VersionInfo);
        result.IsValid = true;
        result.Root.Name.NamespaceName = OdfNamespaces.Manifest;
        result.Root.Name.LocalName = "manifest";
        result.Root.Name.Prefix = OdfNamespaces.ManifestPrefix;
        result.Root.IsManifestElement = true;
        OdfManifestEntry root = new OdfManifestEntry();
        root.FullPath = OdfPartNames.Root;
        root.MediaType = mediaType;
        root.Version = result.Version;
        root.Node.Name.NamespaceName = OdfNamespaces.Manifest;
        root.Node.Name.LocalName = "file-entry";
        root.Node.Name.Prefix = OdfNamespaces.ManifestPrefix;
        root.Node.IsManifestElement = true;
        result.Entries = [root];
        return result;
    }

    string VersionText(OdfVersionInfo? version)
    {
        string result = OdfVersionRules.FormatVersion(version);
        if (result.Length == 0)
        {
            return "1.4";
        }
        return result;
    }

    void RefreshPackageDocument(
        OdfDocument document,
        OdfPackage package)
    {
        PackageApiModel.RefreshPackage(package);
        document.PackageEntries = package.Entries;
        document.Manifest = package.Manifest;
    }

    bool HasPackageEntry(OdfPackage package, string path)
    {
        return FindPackageEntry(package, path) != null;
    }

    OdfPackageEntry? FindPackageEntry(OdfPackage package, string path)
    {
        int index = 0;
        while (index < package.Entries.Length)
        {
            if (package.Entries[index].Path == path)
            {
                return package.Entries[index];
            }
            index = index + 1;
        }
        return null;
    }

    bool ManifestTargetExists(OdfPackage package, string path)
    {
        if (HasPackageEntry(package, path))
        {
            return true;
        }
        if (!path.EndsWith("/", StringComparison.Ordinal))
        {
            return false;
        }
        int index = 0;
        while (index < package.Entries.Length)
        {
            if (package.Entries[index].Path.StartsWith(
                path,
                StringComparison.Ordinal))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    string ReadEntryText(ZipEntryIndex index, string path)
    {
        using (Stream stream = index.OpenEntry(path))
        {
            using (MemoryStream output = new MemoryStream())
            {
                stream.CopyTo(output);
                return Encoding.UTF8.GetString(output.ToArray());
            }
        }
    }

    OdfLocation PartLocation(string partPath)
    {
        OdfLocation result = new OdfLocation();
        result.PartPath = partPath;
        return result;
    }

    OdfLocation NodeLocation(OdfNode node, string partPath)
    {
        OdfLocation result = OdfDiagnosticFactory.CopyLocation(node.Location);
        result.PartPath = partPath;
        result.XmlId = node.XmlId;
        return result;
    }

    OdfLocation AttributeLocation(
        OdfAttribute attribute,
        OdfNode node,
        string partPath)
    {
        OdfLocation result = OdfDiagnosticFactory.CopyLocation(
            attribute.Location);
        result.PartPath = partPath;
        result.XmlId = node.XmlId;
        return result;
    }

    void AddMutation(
        RepairProposal proposal,
        string before,
        string after)
    {
        RepairMutation mutation = new RepairMutation();
        mutation.ProposalId = proposal.Id;
        mutation.Kind = proposal.Kind;
        mutation.Description = proposal.Description;
        mutation.Location = OdfDiagnosticFactory.CopyLocation(
            proposal.Location);
        mutation.BeforeValue = before;
        mutation.AfterValue = after;
        Mutations.Add(mutation);
    }

    void AddRelatedMutation(
        RepairProposal proposal,
        OdfAttribute attribute,
        string before,
        string after)
    {
        RepairMutation mutation = new RepairMutation();
        mutation.ProposalId = proposal.Id;
        mutation.Kind = RepairKind.BrokenReference;
        mutation.Description =
            "Update a reference after XML ID normalization.";
        mutation.Location = OdfDiagnosticFactory.CopyLocation(
            attribute.Location);
        if (mutation.Location.PartPath.Length == 0)
        {
            mutation.Location.PartPath = proposal.Location.PartPath;
        }
        mutation.BeforeValue = before;
        mutation.AfterValue = after;
        Mutations.Add(mutation);
    }

    void AddConflict(RepairProposal? proposal, string message)
    {
        OdfLocation? location = null;
        OdfVersion version = OdfVersion.Unknown;
        if (proposal != null)
        {
            location = proposal.Location;
            if (proposal.Document != null
                && proposal.Document.VersionInfo != null)
            {
                version = proposal.Document.VersionInfo.Version;
            }
        }
        Diagnostics.Add(OdfDiagnosticFactory.Create(
            OdfDiagnosticCodes.EditConflict,
            message,
            OdfValidationSeverity.Error,
            location,
            null,
            version));
        ConflictCount = ConflictCount + 1;
    }
}
