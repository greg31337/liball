using System;
using System.Collections.Generic;

namespace htmlgui;

internal class HydrationComparisonState
{
    internal List<UiDiagnostic> Diagnostics = new List<UiDiagnostic>();
    internal int MaximumDiagnostics = 100;
    internal bool Repairable = true;
}

internal class HydrationComparer
{
    internal UiHydrationResult CompareHydration(DomSnapshot? expected, DomSnapshot? actual, UiHydrationManifest? manifest, UiHydrationMode mode)
    {
        if (expected == null)
        {
            throw new ArgumentNullException("expected");
        }

        if (actual == null)
        {
            throw new ArgumentNullException("actual");
        }

        if (manifest == null)
        {
            throw new ArgumentNullException("manifest");
        }

        string rootSeed = Guard.ArgumentNotWhiteSpace(expected.RootSeed, "expected");
        if (expected.Roots == null)
        {
            throw new ArgumentException("The expected hydration roots are required.", "expected");
        }

        if (actual.Roots == null)
        {
            throw new ArgumentException("The actual hydration roots are required.", "actual");
        }

        if (manifest.Version < 0 || string.IsNullOrWhiteSpace(manifest.Hash))
        {
            throw new ArgumentException("The hydration manifest version and parity hash are invalid.", "manifest");
        }

        RenderTreeAnnotationResult serverTree = new RenderTreeAnnotator().AnnotateTree(expected.Roots, rootSeed, 100);
        if (!serverTree.Success)
        {
            throw new ArgumentException("The expected hydration tree is invalid.", "expected");
        }

        RenderTreeAnnotationResult clientTree = new RenderTreeAnnotator().AnnotateTree(actual.Roots, rootSeed, 100);
        HydrationComparisonState state = new HydrationComparisonState();
        if (!clientTree.Success)
        {
            state.Repairable = false;
            AddAnnotationHydrationDiagnostics(clientTree.Diagnostics, state);
        }

        if (!string.Equals(manifest.Hash, serverTree.ContentHash, StringComparison.Ordinal))
        {
            AddHydrationMismatch(state, "hydration.server-hash", "The manifest parity hash does not match the expected server tree.", "/hydration/hash");
        }

        bool rootSeedMatched = string.Equals(expected.RootSeed, actual.RootSeed, StringComparison.Ordinal);
        if (!rootSeedMatched)
        {
            AddHydrationMismatch(state, "hydration.root-seed", "The client snapshot uses a different deterministic root seed.", "/hydration/root-seed");
        }

        if (!string.Equals(serverTree.ContentHash, clientTree.ContentHash, StringComparison.Ordinal))
        {
            AddHydrationMismatch(state, "hydration.parity-hash", "The client tree parity hash does not match the server tree.", "/hydration/hash");
        }

        CompareHydrationRoots(serverTree.Roots, clientTree.Roots, state);
        DomSnapshot canonicalServer = CreateCanonicalHydrationSnapshot(expected, serverTree);
        DomSnapshot canonicalClient = CreateCanonicalHydrationSnapshot(actual, clientTree);
        DomMutation[] repairMutations = new DomTreeDiffer().CreateMutations(canonicalClient, canonicalServer);
        DomMutation[] replacementMutations = CreateHydrationReplacementMutations(canonicalClient, canonicalServer, repairMutations);
        bool matched = state.Diagnostics.Count == 0;
        HydrationPlanner planner = new HydrationPlanner();
        UiHydrationResult result = planner.PlanHydrationMode(mode, matched, state.Repairable, repairMutations, replacementMutations, state.Diagnostics.ToArray());
        result.ServerHash = serverTree.ContentHash;
        result.ClientHash = clientTree.ContentHash;
        result.ParityMatched = rootSeedMatched && string.Equals(result.ServerHash, result.ClientHash, StringComparison.Ordinal);
        if (IsSuccessfulHydrationStatus(result.Status))
        {
            result.State = planner.CopyAndSortHydrationMarkers(manifest.State, "state");
            result.EventAttachments = planner.CopyAndSortHydrationMarkers(manifest.Events, "events");
            result.BindingAttachments = planner.CopyAndSortHydrationMarkers(manifest.Bindings, "bindings");
            result.LifecycleNotifications = CreateHydrationLifecycleNotifications(manifest);
        }

        return result;
    }

    internal DomSnapshot CreateCanonicalHydrationSnapshot(DomSnapshot source, RenderTreeAnnotationResult tree)
    {
        return new DomSnapshot
        {
            SessionId = source.SessionId,
            RootSeed = source.RootSeed,
            Revision = source.Revision,
            Roots = tree.Roots,
            ContentHash = tree.ContentHash
        };
    }

    internal void CompareHydrationRoots(DomNode[] expected, DomNode[] actual, HydrationComparisonState state)
    {
        if (expected.Length != actual.Length)
        {
            AddHydrationMismatch(state, "hydration.root-count", "The client and server root counts differ.", "/roots");
        }

        int commonCount = expected.Length < actual.Length ? expected.Length : actual.Length;
        int index = 0;
        while (index < commonCount)
        {
            CompareHydrationNode(expected[index], actual[index], "/roots/" + InvariantFormatter.FormatInt32(index), state);
            index++;
        }

        while (index < expected.Length)
        {
            AddHydrationMismatch(state, "hydration.node-missing", "An expected server node is missing from the client tree.", "/roots/" + InvariantFormatter.FormatInt32(index));
            index++;
        }

        while (index < actual.Length)
        {
            AddHydrationMismatch(state, "hydration.node-extra", "The client tree contains an unexpected node.", "/roots/" + InvariantFormatter.FormatInt32(index));
            index++;
        }
    }

    internal void CompareHydrationNode(DomNode expected, DomNode actual, string location, HydrationComparisonState state)
    {
        if (!string.Equals(expected.Id, actual.Id, StringComparison.Ordinal))
        {
            AddHydrationMismatch(state, "hydration.node-id", "The client node identifier differs from the server node identifier.", location + "/id");
        }

        if (!string.Equals(expected.Key, actual.Key, StringComparison.Ordinal))
        {
            AddHydrationMismatch(state, "hydration.node-key", "The client node key differs from the server node key.", location + "/key");
        }

        if (expected.Kind != actual.Kind)
        {
            AddHydrationMismatch(state, "hydration.node-kind", "The client node kind differs from the server node kind.", location + "/kind");
        }

        if (!string.Equals(expected.NamespaceUri, actual.NamespaceUri, StringComparison.Ordinal))
        {
            AddHydrationMismatch(state, "hydration.namespace", "The client node namespace differs from the server node namespace.", location + "/namespace");
        }

        if (!string.Equals(expected.Name, actual.Name, StringComparison.Ordinal))
        {
            AddHydrationMismatch(state, "hydration.tag", "The client element name differs from the server element name.", location + "/name");
        }

        if (!string.Equals(expected.Text, actual.Text, StringComparison.Ordinal))
        {
            AddHydrationMismatch(state, "hydration.text", "The client text differs from the server text.", location + "/text");
        }

        CompareHydrationAttributes(expected.Attributes, actual.Attributes, location, state);
        DomNode[] expectedChildren = expected.Children ?? Array.Empty<DomNode>();
        DomNode[] actualChildren = actual.Children ?? Array.Empty<DomNode>();
        if (expectedChildren.Length != actualChildren.Length)
        {
            AddHydrationMismatch(state, "hydration.child-count", "The client and server child counts differ.", location + "/children");
        }

        int commonCount = expectedChildren.Length < actualChildren.Length ? expectedChildren.Length : actualChildren.Length;
        int index = 0;
        while (index < commonCount)
        {
            DomNode? expectedChild = expectedChildren[index];
            DomNode? actualChild = actualChildren[index];
            string childLocation = location + "/children/" + InvariantFormatter.FormatInt32(index);
            if (expectedChild == null)
            {
                throw new ArgumentException("The expected hydration tree contains a null node.", "expected");
            }

            if (actualChild == null)
            {
                state.Repairable = false;
                AddHydrationMismatch(state, "hydration.node-missing", "An expected server node is missing from the client tree.", childLocation);
            }
            else
            {
                CompareHydrationNode(expectedChild, actualChild, childLocation, state);
            }

            index++;
        }
    }

    internal void CompareHydrationAttributes(DomAttribute[]? expected, DomAttribute[]? actual, string location, HydrationComparisonState state)
    {
        DomTreeDiffer differ = new DomTreeDiffer();
        DomAttribute[] expectedValues = differ.CloneDomAttributes(expected);
        DomAttribute[] actualValues = differ.CloneDomAttributes(actual);
        differ.SortDomAttributes(expectedValues);
        differ.SortDomAttributes(actualValues);
        if (expectedValues.Length != actualValues.Length)
        {
            AddHydrationMismatch(state, "hydration.attribute-count", "The client and server attribute counts differ.", location + "/attributes");
        }

        int expectedIndex = 0;
        while (expectedIndex < expectedValues.Length)
        {
            DomAttribute expectedAttribute = expectedValues[expectedIndex];
            int actualIndex = differ.FindDomAttributeIndex(actualValues, expectedAttribute.NamespaceUri, expectedAttribute.Name);
            string attributeLocation = location + "/attributes/" + expectedAttribute.Name;
            if (actualIndex < 0)
            {
                AddHydrationMismatch(state, "hydration.attribute-missing", "An expected server attribute is missing from the client node.", attributeLocation);
            }
            else if (!differ.DomAttributesEqual(expectedAttribute, actualValues[actualIndex]))
            {
                AddHydrationMismatch(state, "hydration.attribute-value", "The client attribute value differs from the server attribute value.", attributeLocation);
            }

            expectedIndex++;
        }

        int actualIndexValue = 0;
        while (actualIndexValue < actualValues.Length)
        {
            DomAttribute actualAttribute = actualValues[actualIndexValue];
            if (differ.FindDomAttributeIndex(expectedValues, actualAttribute.NamespaceUri, actualAttribute.Name) < 0)
            {
                AddHydrationMismatch(state, "hydration.attribute-extra", "The client node contains an unexpected attribute.", location + "/attributes/" + actualAttribute.Name);
            }

            actualIndexValue++;
        }
    }

    internal void AddAnnotationHydrationDiagnostics(UiDiagnostic[] diagnostics, HydrationComparisonState state)
    {
        int index = 0;
        while (index < diagnostics.Length)
        {
            UiDiagnostic diagnostic = diagnostics[index];
            AddHydrationMismatch(state, "hydration.invalid-client-tree", diagnostic.Message, diagnostic.Location ?? "/roots");
            index++;
        }
    }

    internal void AddHydrationMismatch(HydrationComparisonState state, string code, string message, string location)
    {
        if (state.Diagnostics.Count >= state.MaximumDiagnostics)
        {
            return;
        }

        state.Diagnostics.Add(new UiDiagnostic
        {
            Severity = UiDiagnosticSeverity.Error,
            Code = code,
            Message = message,
            Location = location
        });
    }

    internal DomMutation[] CreateHydrationReplacementMutations(DomSnapshot actual, DomSnapshot expected, DomMutation[] repairMutations)
    {
        if (actual.Roots.Length == 1 && expected.Roots.Length == 1)
        {
            DomTreeDiffer differ = new DomTreeDiffer();
            return new DomMutation[]
            {
                new DomMutation
                {
                    Kind = UiMutationKind.Replace,
                    Sequence = 0,
                    TargetId = actual.Roots[0].Id,
                    Node = differ.CloneDomNode(expected.Roots[0]),
                    HasIndex = true,
                    Index = 0
                }
            };
        }

        return new HydrationPlanner().CopyHydrationMutations(repairMutations);
    }

    internal bool IsSuccessfulHydrationStatus(UiHydrationStatus status)
    {
        return status == UiHydrationStatus.Matched || status == UiHydrationStatus.Repaired || status == UiHydrationStatus.Replaced;
    }

    internal UiLifecycleNotification[] CreateHydrationLifecycleNotifications(UiHydrationManifest manifest)
    {
        UiValueEntry[] components = new HydrationPlanner().CopyAndSortHydrationMarkers(manifest.Components, "components");
        SortHydrationComponentsBySequence(components, manifest.Nodes);
        UiLifecycleNotification[] result = new UiLifecycleNotification[components.Length * 2];
        int index = 0;
        while (index < components.Length)
        {
            result[index] = new UiLifecycleNotification
            {
                ComponentId = components[index].Key,
                Stage = UiLifecycleStage.HydrationStart,
                Generation = manifest.NavigationGeneration
            };
            result[components.Length + index] = new UiLifecycleNotification
            {
                ComponentId = components[index].Key,
                Stage = UiLifecycleStage.HydrationComplete,
                Generation = manifest.NavigationGeneration
            };
            index++;
        }

        return result;
    }

    internal void SortHydrationComponentsBySequence(UiValueEntry[] components, UiValueEntry[]? nodes)
    {
        int index = 1;
        while (index < components.Length)
        {
            UiValueEntry candidate = components[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && CompareHydrationComponentOrder(components[insertionIndex], candidate, nodes) > 0)
            {
                components[insertionIndex + 1] = components[insertionIndex];
                insertionIndex--;
            }

            components[insertionIndex + 1] = candidate;
            index++;
        }
    }

    internal int CompareHydrationComponentOrder(UiValueEntry left, UiValueEntry right, UiValueEntry[]? nodes)
    {
        int leftSequence = FindHydrationComponentSequence(left, nodes);
        int rightSequence = FindHydrationComponentSequence(right, nodes);
        int comparison = leftSequence.CompareTo(rightSequence);
        if (comparison != 0)
        {
            return comparison;
        }

        return string.CompareOrdinal(left.Key, right.Key);
    }

    internal int FindHydrationComponentSequence(UiValueEntry component, UiValueEntry[]? nodes)
    {
        string nodeId = ReadHydrationStringField(component.Value, "nodeId");
        UiValueEntry[] values = nodes ?? Array.Empty<UiValueEntry>();
        int index = 0;
        while (index < values.Length)
        {
            if (string.Equals(values[index].Key, nodeId, StringComparison.Ordinal))
            {
                return ReadHydrationIntegerField(values[index].Value, "sequence");
            }

            index++;
        }

        return int.MaxValue;
    }

    internal string ReadHydrationStringField(UiValue value, string key)
    {
        UiValueEntry[] fields = value.ObjectValue ?? Array.Empty<UiValueEntry>();
        int index = 0;
        while (index < fields.Length)
        {
            if (string.Equals(fields[index].Key, key, StringComparison.Ordinal) && fields[index].Value.Kind == UiValueKind.String)
            {
                return fields[index].Value.StringValue ?? string.Empty;
            }

            index++;
        }

        return string.Empty;
    }

    internal int ReadHydrationIntegerField(UiValue value, string key)
    {
        UiValueEntry[] fields = value.ObjectValue ?? Array.Empty<UiValueEntry>();
        int index = 0;
        while (index < fields.Length)
        {
            if (string.Equals(fields[index].Key, key, StringComparison.Ordinal) && fields[index].Value.Kind == UiValueKind.Integer)
            {
                long number = fields[index].Value.IntegerValue;
                if (number >= int.MinValue && number <= int.MaxValue)
                {
                    return (int)number;
                }
            }

            index++;
        }

        return int.MaxValue;
    }
}
