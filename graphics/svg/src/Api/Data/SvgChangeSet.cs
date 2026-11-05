namespace svg;

/// <summary>Describes one ordered semantic document mutation.</summary>
public class SvgChange
{
    /// <summary>The recorded mutation kind.</summary>
    public SvgChangeKind Kind;
    /// <summary>The affected node, or <c>null</c>.</summary>
    public SvgNode? Node;
    /// <summary>The affected attribute, or <c>null</c>.</summary>
    public SvgAttribute? Attribute;
    /// <summary>The node displaced by replacement, or <c>null</c>.</summary>
    public SvgNode? RemovedNode;
    /// <summary>The attribute displaced by replacement, or <c>null</c>.</summary>
    public SvgAttribute? RemovedAttribute;
    /// <summary>The parent before the change, or <c>null</c>.</summary>
    public SvgNode? OldParent;
    /// <summary>The parent after the change, or <c>null</c>.</summary>
    public SvgNode? NewParent;
    /// <summary>The zero-based position before the change, or <c>-1</c>.</summary>
    public int OldIndex;
    /// <summary>The zero-based position after the change, or <c>-1</c>.</summary>
    public int NewIndex;
    /// <summary>The textual value before the change, or an empty string.</summary>
    public string OldValue;
    /// <summary>The textual value after the change, or an empty string.</summary>
    public string NewValue;
    /// <summary>The stable path identity when available, or an empty string.</summary>
    public string Path;
    /// <summary>Whether the mutation intentionally lost semantics.</summary>
    public bool SemanticLoss;
    /// <summary>The explicit impact classification for this mutation.</summary>
    public SvgProcessChangeSeverity Severity;
    /// <summary>The stable reason for a transformation, or an empty string.</summary>
    public string Reason;
    /// <summary>The affected original source span, or <c>null</c>.</summary>
    public SvgSourceSpan? Source;
    /// <summary>The document version before the mutation.</summary>
    public long BeforeVersion;
    /// <summary>The document version after the mutation.</summary>
    public long AfterVersion;

    /// <summary>Creates an empty change description.</summary>
    public SvgChange()
    {
        Kind = SvgChangeKind.None;
        Node = null;
        Attribute = null;
        RemovedNode = null;
        RemovedAttribute = null;
        OldParent = null;
        NewParent = null;
        OldIndex = -1;
        NewIndex = -1;
        OldValue = "";
        NewValue = "";
        Path = "";
        SemanticLoss = false;
        Severity = SvgProcessChangeSeverity.None;
        Reason = "";
        Source = null;
        BeforeVersion = 0;
        AfterVersion = 0;
    }

    /// <summary>Creates a description that reverses this change.</summary>
    /// <returns>A newly allocated inverse change.</returns>
    internal SvgChange CreateInverse()
    {
        SvgChange inverse = new SvgChange();
        inverse.Kind = InverseKind(Kind);
        inverse.Node = Kind == SvgChangeKind.NodeReplace ? RemovedNode : Node;
        inverse.Attribute = Kind == SvgChangeKind.AttributeReplace ? RemovedAttribute : Attribute;
        inverse.RemovedNode = Kind == SvgChangeKind.NodeReplace ? Node : RemovedNode;
        inverse.RemovedAttribute = Kind == SvgChangeKind.AttributeReplace ? Attribute : RemovedAttribute;
        inverse.OldParent = NewParent;
        inverse.NewParent = OldParent;
        inverse.OldIndex = NewIndex;
        inverse.NewIndex = OldIndex;
        inverse.OldValue = NewValue;
        inverse.NewValue = OldValue;
        inverse.Path = Path;
        inverse.SemanticLoss = SemanticLoss;
        inverse.Severity = Severity;
        inverse.Reason = Reason;
        inverse.Source = Source;
        inverse.BeforeVersion = AfterVersion;
        inverse.AfterVersion = BeforeVersion;
        return inverse;
    }

    internal static SvgChangeKind InverseKind(SvgChangeKind kind)
    {
        if (kind == SvgChangeKind.NodeInsert) return SvgChangeKind.NodeRemove;
        if (kind == SvgChangeKind.NodeRemove) return SvgChangeKind.NodeInsert;
        if (kind == SvgChangeKind.AttributeInsert) return SvgChangeKind.AttributeRemove;
        if (kind == SvgChangeKind.AttributeRemove) return SvgChangeKind.AttributeInsert;
        return kind;
    }
}

/// <summary>Records one exclusive editable-document transaction.</summary>
public class SvgChangeSet
{
    /// <summary>The primary transaction document.</summary>
    public SvgDocument Document;
    /// <summary>The ordered semantic changes recorded by successful mutations.</summary>
    public System.Collections.Generic.List<SvgChange> Changes;
    /// <summary>Whether the change set was committed or rolled back.</summary>
    public bool Completed;
    /// <summary>Whether the completed change set was committed.</summary>
    public bool Committed;
    /// <summary>Whether the completed change set was rolled back.</summary>
    public bool RolledBack;
    /// <summary>The primary document version at transaction start.</summary>
    public long StartVersion;
    /// <summary>The primary document version at completion, or the current version while active.</summary>
    public long EndVersion;
    System.Collections.Generic.List<SvgDocumentTransactionState> states;
    System.Collections.Generic.List<SvgExternalNodeTransactionState> externalNodes;
    System.Collections.Generic.List<SvgExternalAttributeTransactionState> externalAttributes;

    internal SvgChangeSet(SvgDocument document, bool completed)
    {
        Document = document;
        Changes = new System.Collections.Generic.List<SvgChange>();
        Completed = completed;
        Committed = completed;
        RolledBack = false;
        StartVersion = document.Version;
        EndVersion = document.Version;
        states = new System.Collections.Generic.List<SvgDocumentTransactionState>();
        externalNodes = new System.Collections.Generic.List<SvgExternalNodeTransactionState>();
        externalAttributes = new System.Collections.Generic.List<SvgExternalAttributeTransactionState>();
        if (!completed)
        {
            states.Add(new SvgDocumentTransactionState(document));
            document.ActiveChangeSet = this;
        }
    }

    /// <summary>Commits all recorded edits and ends the transaction.</summary>
    /// <returns><c>true</c> when this call completed an active transaction.</returns>
    internal bool Commit()
    {
        if (Completed) return false;
        if (object.ReferenceEquals(Document.ActiveChangeSet, this)) Document.ActiveChangeSet = null;
        Completed = true;
        Committed = true;
        EndVersion = Document.Version;
        return true;
    }

    /// <summary>Restores every captured document to its transaction-start state.</summary>
    /// <returns><c>true</c> when this call rolled back an active transaction.</returns>
    internal bool Rollback()
    {
        if (Completed) return false;
        if (object.ReferenceEquals(Document.ActiveChangeSet, this)) Document.ActiveChangeSet = null;
        int index = states.Count - 1;
        while (index >= 0)
        {
            SvgDocument stateDocument = states[index].Document;
            if (object.ReferenceEquals(stateDocument.ActiveChangeSet, this)) stateDocument.ActiveChangeSet = null;
            states[index].Restore();
            index = index - 1;
        }
        index = externalNodes.Count - 1;
        while (index >= 0)
        {
            externalNodes[index].Restore();
            index = index - 1;
        }
        index = externalAttributes.Count - 1;
        while (index >= 0)
        {
            externalAttributes[index].Restore();
            index = index - 1;
        }
        Completed = true;
        RolledBack = true;
        EndVersion = Document.Version;
        return true;
    }

    /// <summary>Creates a completed inverse change description in reverse operation order.</summary>
    /// <returns>A newly allocated inverse change set.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown while this change set is active.</exception>
    internal SvgChangeSet CreateInverse()
    {
        if (!Completed) throw new System.InvalidOperationException("An active change set cannot create an inverse.");
        SvgChangeSet inverse = new SvgChangeSet(Document, true);
        int index = Changes.Count - 1;
        while (index >= 0)
        {
            inverse.Changes.Add(Changes[index].CreateInverse());
            index = index - 1;
        }
        inverse.StartVersion = EndVersion;
        inverse.EndVersion = StartVersion;
        return inverse;
    }

    internal void CaptureDocument(SvgDocument? document)
    {
        if (document == null || ContainsDocument(document)) return;
        states.Add(new SvgDocumentTransactionState(document));
    }

    internal void Record(SvgChange change)
    {
        if (!Completed) Changes.Add(change);
    }

    internal void CaptureExternalNode(SvgNode? node)
    {
        if (node == null || ContainsCapturedNode(node)) return;
        externalNodes.Add(new SvgExternalNodeTransactionState(node));
    }

    internal void CaptureExternalAttribute(SvgAttribute? attribute)
    {
        if (attribute == null || ContainsCapturedAttribute(attribute)) return;
        externalAttributes.Add(new SvgExternalAttributeTransactionState(attribute));
    }

    internal bool ContainsDocument(SvgDocument document)
    {
        int index = 0;
        while (index < states.Count)
        {
            if (object.ReferenceEquals(states[index].Document, document)) return true;
            index = index + 1;
        }
        return false;
    }

    internal bool ContainsCapturedNode(SvgNode node)
    {
        int index = 0;
        while (index < states.Count)
        {
            if (states[index].ContainsNode(node)) return true;
            index = index + 1;
        }
        index = 0;
        while (index < externalNodes.Count)
        {
            if (externalNodes[index].ContainsNode(node)) return true;
            index = index + 1;
        }
        return false;
    }

    internal bool ContainsCapturedAttribute(SvgAttribute attribute)
    {
        int index = 0;
        while (index < states.Count)
        {
            if (states[index].ContainsAttribute(attribute)) return true;
            index = index + 1;
        }
        index = 0;
        while (index < externalNodes.Count)
        {
            if (externalNodes[index].ContainsAttribute(attribute)) return true;
            index = index + 1;
        }
        index = 0;
        while (index < externalAttributes.Count)
        {
            if (object.ReferenceEquals(externalAttributes[index].State.Attribute, attribute)) return true;
            index = index + 1;
        }
        return false;
    }
}
