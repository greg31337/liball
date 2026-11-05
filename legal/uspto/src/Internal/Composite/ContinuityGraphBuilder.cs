namespace uspto;

internal static class ContinuityGraphBuilder
{
    internal static PatentContinuityGraph Build(PatentContinuity[] continuities)
    {
        if (continuities == null)
        {
            throw new ValidationException("Continuity records are required for graph construction.", "continuities", "REQUIRED", null);
        }

        PatentContinuityGraph graph = new PatentContinuityGraph();
        graph.Provenance.Source = DataProvenance.ClientDerived;
        graph.Provenance.RetrievedAt = global::System.DateTimeOffset.UtcNow;
        graph.Provenance.HasRetrievedAt = true;
        graph.Provenance.IsAuthoritative = false;
        global::System.Collections.Generic.List<PatentContinuityGraphNode> nodes = new global::System.Collections.Generic.List<PatentContinuityGraphNode>();
        global::System.Collections.Generic.Dictionary<string, PatentContinuityGraphNode> nodeIndex = new global::System.Collections.Generic.Dictionary<string, PatentContinuityGraphNode>(global::System.StringComparer.Ordinal);
        global::System.Collections.Generic.List<PatentContinuityGraphEdge> edges = new global::System.Collections.Generic.List<PatentContinuityGraphEdge>();
        global::System.Collections.Generic.HashSet<string> edgeKeys = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal);
        int index;
        for (index = 0; index < continuities.Length; index += 1)
        {
            PatentContinuity continuity = continuities[index];
            if (continuity == null)
            {
                throw new ValidationException("A continuity record cannot be null.", "continuities[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]", "REQUIRED", null);
            }

            ProvenanceLabeler.AddPatentField(graph.Provenance, "Nodes", continuity.Provenance);
            ProvenanceLabeler.AddPatentField(graph.Provenance, "Edges", continuity.Provenance);

            string applicationNumber = NormalizeRequiredApplicationNumber(continuity.ApplicationNumber, "continuities[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "].ApplicationNumber");
            PatentContinuityGraphNode node = GetOrCreateNode(nodes, nodeIndex, applicationNumber);
            node.IsMissing = false;
            if (graph.RootApplicationNumber == null)
            {
                graph.RootApplicationNumber = applicationNumber;
                node.IsRoot = true;
            }
        }

        int duplicateCount = 0;
        for (index = 0; index < continuities.Length; index += 1)
        {
            PatentContinuity continuity = continuities[index];
            string applicationNumber = NormalizeRequiredApplicationNumber(continuity.ApplicationNumber, "continuities[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "].ApplicationNumber");
            duplicateCount += AddParentEdges(continuity, applicationNumber, nodes, nodeIndex, edges, edgeKeys, index);
            duplicateCount += AddChildEdges(continuity, applicationNumber, nodes, nodeIndex, edges, edgeKeys, index);
        }

        MarkCycleEdges(edges, graph);
        int missingCount = 0;
        for (index = 0; index < nodes.Count; index += 1)
        {
            if (nodes[index].IsMissing)
            {
                missingCount += 1;
            }
        }

        graph.Nodes = nodes.ToArray();
        graph.Edges = edges.ToArray();
        graph.MissingNodeCount = missingCount;
        graph.DuplicateEdgeCount = duplicateCount;
        return graph;
    }

    internal static int AddParentEdges(PatentContinuity continuity, string applicationNumber, global::System.Collections.Generic.List<PatentContinuityGraphNode> nodes, global::System.Collections.Generic.Dictionary<string, PatentContinuityGraphNode> nodeIndex, global::System.Collections.Generic.List<PatentContinuityGraphEdge> edges, global::System.Collections.Generic.HashSet<string> edgeKeys, int continuityIndex)
    {
        PatentParentContinuity[] parents = continuity.Parents;
        if (parents == null)
        {
            throw new ValidationException("A continuity parent collection cannot be null.", "continuities[" + continuityIndex.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "].Parents", "REQUIRED", null);
        }

        int duplicates = 0;
        int index;
        for (index = 0; index < parents.Length; index += 1)
        {
            PatentParentContinuity relationship = parents[index];
            if (relationship == null)
            {
                throw new ValidationException("A parent continuity relationship cannot be null.", "continuities[" + continuityIndex.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "].Parents[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]", "REQUIRED", null);
            }

            string? parentNumber = NormalizeOptionalApplicationNumber(relationship.ParentApplicationNumber, "ParentApplicationNumber");
            string? childNumber = NormalizeOptionalApplicationNumber(relationship.ChildApplicationNumber, "ChildApplicationNumber");
            if (childNumber == null)
            {
                childNumber = applicationNumber;
            }

            if (parentNumber != null)
            {
                PatentContinuityGraphNode parentNode = GetOrCreateNode(nodes, nodeIndex, parentNumber);
                GetOrCreateNode(nodes, nodeIndex, childNumber);
                ApplyParentMetadata(parentNode, relationship);
                if (AddUniqueEdge(edges, edgeKeys, parentNumber, childNumber, relationship.RelationshipCode, relationship.RelationshipDescription))
                {
                    duplicates += 1;
                }
            }
        }

        return duplicates;
    }

    internal static int AddChildEdges(PatentContinuity continuity, string applicationNumber, global::System.Collections.Generic.List<PatentContinuityGraphNode> nodes, global::System.Collections.Generic.Dictionary<string, PatentContinuityGraphNode> nodeIndex, global::System.Collections.Generic.List<PatentContinuityGraphEdge> edges, global::System.Collections.Generic.HashSet<string> edgeKeys, int continuityIndex)
    {
        PatentChildContinuity[] children = continuity.Children;
        if (children == null)
        {
            throw new ValidationException("A continuity child collection cannot be null.", "continuities[" + continuityIndex.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "].Children", "REQUIRED", null);
        }

        int duplicates = 0;
        int index;
        for (index = 0; index < children.Length; index += 1)
        {
            PatentChildContinuity relationship = children[index];
            if (relationship == null)
            {
                throw new ValidationException("A child continuity relationship cannot be null.", "continuities[" + continuityIndex.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "].Children[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]", "REQUIRED", null);
            }

            string? parentNumber = NormalizeOptionalApplicationNumber(relationship.ParentApplicationNumber, "ParentApplicationNumber");
            string? childNumber = NormalizeOptionalApplicationNumber(relationship.ChildApplicationNumber, "ChildApplicationNumber");
            if (parentNumber == null)
            {
                parentNumber = applicationNumber;
            }

            if (childNumber != null)
            {
                GetOrCreateNode(nodes, nodeIndex, parentNumber);
                PatentContinuityGraphNode childNode = GetOrCreateNode(nodes, nodeIndex, childNumber);
                ApplyChildMetadata(childNode, relationship);
                if (AddUniqueEdge(edges, edgeKeys, parentNumber, childNumber, relationship.RelationshipCode, relationship.RelationshipDescription))
                {
                    duplicates += 1;
                }
            }
        }

        return duplicates;
    }

    internal static PatentContinuityGraphNode GetOrCreateNode(global::System.Collections.Generic.List<PatentContinuityGraphNode> nodes, global::System.Collections.Generic.Dictionary<string, PatentContinuityGraphNode> nodeIndex, string applicationNumber)
    {
        if (nodeIndex.ContainsKey(applicationNumber))
        {
            return nodeIndex[applicationNumber];
        }

        PatentContinuityGraphNode node = new PatentContinuityGraphNode();
        node.ApplicationNumber = applicationNumber;
        node.IsMissing = true;
        nodeIndex[applicationNumber] = node;
        nodes.Add(node);
        return node;
    }

    internal static void ApplyParentMetadata(PatentContinuityGraphNode node, PatentParentContinuity relationship)
    {
        if (node.PatentNumber == null && relationship.ParentPatentNumber != null)
        {
            node.PatentNumber = relationship.ParentPatentNumber;
        }

        if (!node.HasApplicationStatusCode && relationship.HasParentApplicationStatusCode)
        {
            node.ApplicationStatusCode = relationship.ParentApplicationStatusCode;
            node.HasApplicationStatusCode = true;
        }

        if (node.ApplicationStatusDescription == null && relationship.ParentApplicationStatusDescription != null)
        {
            node.ApplicationStatusDescription = relationship.ParentApplicationStatusDescription;
        }

        if (!node.HasFilingDate && relationship.HasParentApplicationFilingDate)
        {
            node.FilingDate = relationship.ParentApplicationFilingDate;
            node.HasFilingDate = true;
        }
    }

    internal static void ApplyChildMetadata(PatentContinuityGraphNode node, PatentChildContinuity relationship)
    {
        if (node.PatentNumber == null && relationship.ChildPatentNumber != null)
        {
            node.PatentNumber = relationship.ChildPatentNumber;
        }

        if (!node.HasApplicationStatusCode && relationship.HasChildApplicationStatusCode)
        {
            node.ApplicationStatusCode = relationship.ChildApplicationStatusCode;
            node.HasApplicationStatusCode = true;
        }

        if (node.ApplicationStatusDescription == null && relationship.ChildApplicationStatusDescription != null)
        {
            node.ApplicationStatusDescription = relationship.ChildApplicationStatusDescription;
        }

        if (!node.HasFilingDate && relationship.HasChildApplicationFilingDate)
        {
            node.FilingDate = relationship.ChildApplicationFilingDate;
            node.HasFilingDate = true;
        }
    }

    internal static bool AddUniqueEdge(global::System.Collections.Generic.List<PatentContinuityGraphEdge> edges, global::System.Collections.Generic.HashSet<string> edgeKeys, string parentNumber, string childNumber, string? relationshipCode, string? relationshipDescription)
    {
        string normalizedCode = string.Empty;
        if (!global::System.String.IsNullOrWhiteSpace(relationshipCode))
        {
            normalizedCode = relationshipCode.Trim().ToUpperInvariant();
        }

        string key = parentNumber + "\u001f" + childNumber + "\u001f" + normalizedCode;
        if (edgeKeys.Contains(key))
        {
            return true;
        }

        edgeKeys.Add(key);
        PatentContinuityGraphEdge edge = new PatentContinuityGraphEdge();
        edge.ParentApplicationNumber = parentNumber;
        edge.ChildApplicationNumber = childNumber;
        edge.RelationshipCode = relationshipCode;
        edge.RelationshipDescription = relationshipDescription;
        edges.Add(edge);
        return false;
    }

    internal static void MarkCycleEdges(global::System.Collections.Generic.List<PatentContinuityGraphEdge> edges, PatentContinuityGraph graph)
    {
        int index;
        for (index = 0; index < edges.Count; index += 1)
        {
            PatentContinuityGraphEdge edge = edges[index];
            if (edge.ParentApplicationNumber != null && edge.ChildApplicationNumber != null && HasDirectedPath(edge.ChildApplicationNumber, edge.ParentApplicationNumber, edges))
            {
                edge.IsCycle = true;
                graph.HasCycle = true;
            }
        }
    }

    internal static bool HasDirectedPath(string start, string target, global::System.Collections.Generic.List<PatentContinuityGraphEdge> edges)
    {
        if (global::System.String.Equals(start, target, global::System.StringComparison.Ordinal))
        {
            return true;
        }

        global::System.Collections.Generic.Queue<string> pending = new global::System.Collections.Generic.Queue<string>();
        global::System.Collections.Generic.HashSet<string> visited = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal);
        pending.Enqueue(start);
        visited.Add(start);
        while (pending.Count > 0)
        {
            string current = pending.Dequeue();
            int index;
            for (index = 0; index < edges.Count; index += 1)
            {
                PatentContinuityGraphEdge edge = edges[index];
                if (global::System.String.Equals(edge.ParentApplicationNumber, current, global::System.StringComparison.Ordinal) && edge.ChildApplicationNumber != null)
                {
                    if (global::System.String.Equals(edge.ChildApplicationNumber, target, global::System.StringComparison.Ordinal))
                    {
                        return true;
                    }

                    if (!visited.Contains(edge.ChildApplicationNumber))
                    {
                        visited.Add(edge.ChildApplicationNumber);
                        pending.Enqueue(edge.ChildApplicationNumber);
                    }
                }
            }
        }

        return false;
    }

    internal static string NormalizeRequiredApplicationNumber(string? value, string fieldName)
    {
        if (global::System.String.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException("A continuity application number is required.", fieldName, "REQUIRED", null);
        }

        PatentApplicationNumberResult parsed = PatentIdentifierParser.ParseApplicationNumber(value);
        if (!parsed.IsValid)
        {
            throw new ValidationException("A continuity application number is invalid.", fieldName, parsed.FailureCode, null);
        }

        return parsed.CanonicalNumber;
    }

    internal static string? NormalizeOptionalApplicationNumber(string? value, string fieldName)
    {
        if (value == null)
        {
            return null;
        }

        PatentApplicationNumberResult parsed = PatentIdentifierParser.ParseApplicationNumber(value);
        if (!parsed.IsValid)
        {
            throw new ValidationException("A continuity relationship application number is invalid.", fieldName, parsed.FailureCode, null);
        }

        return parsed.CanonicalNumber;
    }
}
