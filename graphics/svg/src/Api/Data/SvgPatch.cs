namespace svg;

/// <summary>Describes one preconditioned structural patch operation.</summary>
public class SvgPatchOperation
{
    /// <summary>The patch operation kind.</summary>
    public SvgPatchOperationKind Kind;
    /// <summary>The stable affected node or element path.</summary>
    public string Path;
    /// <summary>The stable parent path for node insertion.</summary>
    public string ParentPath;
    /// <summary>The expected prior node subtree, or <c>null</c>.</summary>
    public SvgNode? ExpectedNode;
    /// <summary>The desired node subtree, or <c>null</c>.</summary>
    public SvgNode? Node;
    /// <summary>The expected prior attribute, or <c>null</c>.</summary>
    public SvgAttribute? ExpectedAttribute;
    /// <summary>The desired attribute, or <c>null</c>.</summary>
    public SvgAttribute? Attribute;
    /// <summary>The expected prior textual value, or an empty string.</summary>
    public string ExpectedValue;
    /// <summary>The desired textual value, or an empty string.</summary>
    public string Value;
    /// <summary>The expected node or attribute index, or <c>-1</c>.</summary>
    public int ExpectedIndex;
    /// <summary>The desired node or attribute index, or <c>-1</c>.</summary>
    public int Index;
    /// <summary>The expected parent child or attribute count, or <c>-1</c>.</summary>
    public int ExpectedCount;
    /// <summary>The original source span associated with the operation, or <c>null</c>.</summary>
    public SvgSourceSpan? Source;

    /// <summary>Creates an empty patch operation.</summary>
    public SvgPatchOperation()
    {
        Kind = SvgPatchOperationKind.None;
        Path = "";
        ParentPath = "";
        ExpectedNode = null;
        Node = null;
        ExpectedAttribute = null;
        Attribute = null;
        ExpectedValue = "";
        Value = "";
        ExpectedIndex = -1;
        Index = -1;
        ExpectedCount = -1;
        Source = null;
    }

    /// <summary>Creates an operation that reverses this operation.</summary>
    /// <returns>A newly allocated inverse operation.</returns>
    internal SvgPatchOperation CreateInverse()
    {
        SvgPatchOperation inverse = new SvgPatchOperation();
        inverse.Kind = InverseKind(Kind);
        inverse.Path = Path;
        inverse.ParentPath = ParentPath;
        inverse.ExpectedNode = CloneNode(Node);
        inverse.Node = CloneNode(ExpectedNode);
        if (Kind == SvgPatchOperationKind.AttributeMove)
        {
            inverse.ExpectedAttribute = CloneAttribute(ExpectedAttribute);
            inverse.Attribute = null;
        }
        else
        {
            inverse.ExpectedAttribute = CloneAttribute(Attribute);
            inverse.Attribute = CloneAttribute(ExpectedAttribute);
        }
        inverse.ExpectedValue = Value;
        inverse.Value = ExpectedValue;
        inverse.ExpectedIndex = Index;
        inverse.Index = ExpectedIndex;
        inverse.ExpectedCount = InverseExpectedCount();
        inverse.Source = Source;
        return inverse;
    }

    internal int InverseExpectedCount()
    {
        if (ExpectedCount < 0) return -1;
        if (Kind == SvgPatchOperationKind.NodeInsert || Kind == SvgPatchOperationKind.AttributeInsert) return ExpectedCount + 1;
        if (Kind == SvgPatchOperationKind.NodeRemove || Kind == SvgPatchOperationKind.AttributeRemove) return ExpectedCount - 1;
        return ExpectedCount;
    }

    internal static SvgPatchOperationKind InverseKind(SvgPatchOperationKind kind)
    {
        if (kind == SvgPatchOperationKind.NodeInsert) return SvgPatchOperationKind.NodeRemove;
        if (kind == SvgPatchOperationKind.NodeRemove) return SvgPatchOperationKind.NodeInsert;
        if (kind == SvgPatchOperationKind.AttributeInsert) return SvgPatchOperationKind.AttributeRemove;
        if (kind == SvgPatchOperationKind.AttributeRemove) return SvgPatchOperationKind.AttributeInsert;
        return kind;
    }

    internal static SvgNode? CloneNode(SvgNode? node)
    {
        if (node == null) return null;
        return new SvgNodeApi().CloneNode(node, true);
    }

    internal static SvgAttribute? CloneAttribute(SvgAttribute? attribute)
    {
        if (attribute == null) return null;
        return new SvgAttribute(attribute.QualifiedName, attribute.Prefix, attribute.LocalName, attribute.NamespaceUri, attribute.Value);
    }
}

/// <summary>Contains one atomic patch application result.</summary>
public class SvgPatchResult
{
    /// <summary>Whether every patch operation was applied and committed.</summary>
    public bool Success;
    /// <summary>The stable patch failure.</summary>
    public SvgPatchError Error;
    /// <summary>The zero-based failing operation index, or <c>-1</c>.</summary>
    public int OperationIndex;
    /// <summary>The number of operations committed after success.</summary>
    public int AppliedOperations;
    /// <summary>The ordered conflict diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The inverse patch after success, or <c>null</c>.</summary>
    public SvgPatch? Inverse;

    /// <summary>Creates an empty failed patch result.</summary>
    public SvgPatchResult()
    {
        Success = false;
        Error = SvgPatchError.None;
        OperationIndex = -1;
        AppliedOperations = 0;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Inverse = null;
    }

    internal static SvgPatchResult InvalidArgument(string message)
    {
        SvgPatchResult result = new SvgPatchResult();
        result.Error = SvgPatchError.InvalidArgument;
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.PatchConflict, message, null));
        return result;
    }
}

/// <summary>Stores an ordered deterministic structural document patch.</summary>
public class SvgPatch
{
    /// <summary>The required target document version, or <c>-1</c> to ignore version.</summary>
    public long ExpectedVersion;
    /// <summary>The ordered preconditioned patch operations.</summary>
    public System.Collections.Generic.List<SvgPatchOperation> Operations;
    /// <summary>The diagnostics from the most recent validation or application.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    /// <summary>Creates an empty patch without a version precondition.</summary>
    public SvgPatch()
    {
        ExpectedVersion = -1;
        Operations = new System.Collections.Generic.List<SvgPatchOperation>();
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
    }

    /// <summary>Creates a deterministic structural diff between documents.</summary>
    /// <param name="before">The expected source document.</param>
    /// <param name="after">The desired target document.</param>
    /// <returns>A newly allocated preconditioned patch.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    internal static SvgPatch CreateDiff(SvgDocument before, SvgDocument after)
    {
        if (before == null) throw new System.ArgumentNullException("before");
        if (after == null) throw new System.ArgumentNullException("after");
        SvgPatch patch = new SvgPatch();
        patch.ExpectedVersion = before.Version;
        DiffNode(before.DocumentNode, after.DocumentNode, "/", patch);
        return patch;
    }

    /// <summary>Applies every operation atomically after checking its preconditions.</summary>
    /// <param name="document">The target document.</param>
    /// <returns>The application result and inverse patch after success.</returns>
    internal SvgPatchResult Apply(SvgDocument? document)
    {
        if (Diagnostics == null) Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        else Diagnostics.Clear();
        if (Operations == null) return Failure(SvgPatchError.InvalidArgument, -1, "The patch operation collection is required.", null, "");
        if (document == null) return Failure(SvgPatchError.InvalidArgument, -1, "A target document is required.", null, "");
        if (document.ActiveChangeSet != null && !document.ActiveChangeSet.Completed) return Failure(SvgPatchError.ActiveTransaction, -1, "A patch cannot run inside another active transaction.", null, "");
        if (ExpectedVersion >= 0 && document.Version != ExpectedVersion) return Failure(SvgPatchError.VersionConflict, -1, "The target document version does not match the patch precondition.", document.DocumentNode.SourceSpan, "/");
        if (document.Version > System.Int64.MaxValue - Operations.Count) return Failure(SvgPatchError.PreconditionFailed, -1, "The patch would overflow the document version.", document.DocumentNode.SourceSpan, "/");
        SvgDocumentApi documents = new SvgDocumentApi();
        SvgChangeSet transaction = documents.BeginChangeSet(document);
        int index = 0;
        while (index < Operations.Count)
        {
            SvgPatchResult operationResult = ApplyOperation(document, Operations[index], index);
            if (!operationResult.Success)
            {
                transaction.Rollback();
                return operationResult;
            }
            index = index + 1;
        }
        transaction.Commit();
        SvgPatchResult result = new SvgPatchResult();
        result.Success = true;
        result.AppliedOperations = Operations.Count;
        result.Inverse = CreateInverse();
        result.Inverse.ExpectedVersion = document.Version;
        return result;
    }

    /// <summary>Checks every patch precondition against a semantic document copy.</summary>
    /// <param name="document">The document to validate without mutation.</param>
    /// <returns>A success result or the first deterministic conflict.</returns>
    internal SvgPatchResult Validate(SvgDocument? document)
    {
        if (Diagnostics == null) Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        else Diagnostics.Clear();
        if (Operations == null) return Failure(SvgPatchError.InvalidArgument, -1, "The patch operation collection is required.", null, "");
        if (document == null) return Failure(SvgPatchError.InvalidArgument, -1, "A target document is required.", null, "");
        SvgDocument copy = CloneDocument(document);
        SvgPatchResult result = Apply(copy);
        if (result.Success)
        {
            result.AppliedOperations = 0;
            result.Inverse = null;
        }
        return result;
    }

    /// <summary>Creates a patch with reversed operations and swapped preconditions.</summary>
    /// <returns>A newly allocated inverse patch.</returns>
    internal SvgPatch CreateInverse()
    {
        SvgPatch inverse = new SvgPatch();
        int index = Operations.Count - 1;
        while (index >= 0)
        {
            inverse.Operations.Add(Operations[index].CreateInverse());
            index = index - 1;
        }
        return inverse;
    }

    internal static void DiffNode(SvgNode before, SvgNode after, string path, SvgPatch patch)
    {
        if (!SameIdentity(before, after))
        {
            SvgPatchOperation replacement = new SvgPatchOperation();
            replacement.Kind = SvgPatchOperationKind.NodeReplace;
            replacement.Path = path;
            replacement.ExpectedNode = CloneNode(before);
            replacement.Node = CloneNode(after);
            replacement.Source = before.SourceSpan;
            patch.Operations.Add(replacement);
            return;
        }
        if (before.Kind != SvgNodeKind.Document && before.Kind != SvgNodeKind.Element && before.Value != after.Value)
        {
            if (before.Kind == SvgNodeKind.Doctype)
            {
                SvgPatchOperation replacement = new SvgPatchOperation();
                replacement.Kind = SvgPatchOperationKind.NodeReplace;
                replacement.Path = path;
                replacement.ExpectedNode = CloneNode(before);
                replacement.Node = CloneNode(after);
                replacement.Source = before.SourceSpan;
                patch.Operations.Add(replacement);
                return;
            }
            SvgPatchOperation value = new SvgPatchOperation();
            value.Kind = SvgPatchOperationKind.NodeValue;
            value.Path = path;
            value.ExpectedValue = before.Value;
            value.Value = after.Value;
            value.Source = before.SourceSpan;
            patch.Operations.Add(value);
        }
        if (before.Kind == SvgNodeKind.Element) DiffAttributes(before, after, path, patch);
        int common = before.Children.Count < after.Children.Count ? before.Children.Count : after.Children.Count;
        int childIndex = 0;
        while (childIndex < common)
        {
            DiffNode(before.Children[childIndex], after.Children[childIndex], new SvgPatchPath().Append(path, childIndex), patch);
            childIndex = childIndex + 1;
        }
        int workingCount = before.Children.Count;
        childIndex = before.Children.Count - 1;
        while (childIndex >= after.Children.Count)
        {
            SvgPatchOperation removal = new SvgPatchOperation();
            removal.Kind = SvgPatchOperationKind.NodeRemove;
            removal.Path = new SvgPatchPath().Append(path, childIndex);
            removal.ParentPath = path;
            removal.ExpectedNode = CloneNode(before.Children[childIndex]);
            removal.ExpectedIndex = childIndex;
            removal.ExpectedCount = workingCount;
            removal.Source = before.Children[childIndex].SourceSpan;
            patch.Operations.Add(removal);
            workingCount = workingCount - 1;
            childIndex = childIndex - 1;
        }
        childIndex = common;
        while (childIndex < after.Children.Count)
        {
            SvgPatchOperation insertion = new SvgPatchOperation();
            insertion.Kind = SvgPatchOperationKind.NodeInsert;
            insertion.Path = new SvgPatchPath().Append(path, childIndex);
            insertion.ParentPath = path;
            insertion.Node = CloneNode(after.Children[childIndex]);
            insertion.Index = childIndex;
            insertion.ExpectedCount = workingCount;
            insertion.Source = after.Children[childIndex].SourceSpan;
            patch.Operations.Add(insertion);
            workingCount = workingCount + 1;
            childIndex = childIndex + 1;
        }
    }

    internal static void DiffAttributes(SvgNode before, SvgNode after, string path, SvgPatch patch)
    {
        System.Collections.Generic.List<SvgAttribute> working = new System.Collections.Generic.List<SvgAttribute>(before.Attributes);
        int targetIndex = 0;
        while (targetIndex < after.Attributes.Count)
        {
            SvgAttribute desired = after.Attributes[targetIndex];
            int found = FindExpandedAttribute(working, desired, targetIndex);
            if (found < 0)
            {
                SvgPatchOperation insertion = new SvgPatchOperation();
                insertion.Kind = SvgPatchOperationKind.AttributeInsert;
                insertion.Path = path;
                insertion.Attribute = CloneAttribute(desired);
                insertion.Index = targetIndex;
                insertion.ExpectedCount = working.Count;
                insertion.Source = desired.SourceSpan;
                patch.Operations.Add(insertion);
                working.Insert(targetIndex, desired);
            }
            else
            {
                if (found != targetIndex)
                {
                    SvgPatchOperation move = new SvgPatchOperation();
                    move.Kind = SvgPatchOperationKind.AttributeMove;
                    move.Path = path;
                    move.ExpectedAttribute = CloneAttribute(working[found]);
                    move.ExpectedIndex = found;
                    move.Index = targetIndex;
                    move.ExpectedCount = working.Count;
                    move.Source = working[found].SourceSpan;
                    patch.Operations.Add(move);
                    SvgAttribute moved = working[found];
                    working.RemoveAt(found);
                    working.Insert(targetIndex, moved);
                }
                if (working[targetIndex].Value != desired.Value)
                {
                    SvgPatchOperation replacement = new SvgPatchOperation();
                    replacement.Kind = SvgPatchOperationKind.AttributeReplace;
                    replacement.Path = path;
                    replacement.ExpectedAttribute = CloneAttribute(working[targetIndex]);
                    replacement.Attribute = CloneAttribute(desired);
                    replacement.ExpectedIndex = targetIndex;
                    replacement.Index = targetIndex;
                    replacement.ExpectedCount = working.Count;
                    replacement.Source = working[targetIndex].SourceSpan;
                    patch.Operations.Add(replacement);
                    working[targetIndex] = desired;
                }
            }
            targetIndex = targetIndex + 1;
        }
        int removeIndex = working.Count - 1;
        while (removeIndex >= after.Attributes.Count)
        {
            SvgPatchOperation removal = new SvgPatchOperation();
            removal.Kind = SvgPatchOperationKind.AttributeRemove;
            removal.Path = path;
            removal.ExpectedAttribute = CloneAttribute(working[removeIndex]);
            removal.ExpectedIndex = removeIndex;
            removal.ExpectedCount = working.Count;
            removal.Source = working[removeIndex].SourceSpan;
            patch.Operations.Add(removal);
            working.RemoveAt(removeIndex);
            removeIndex = removeIndex - 1;
        }
    }

    internal SvgPatchResult ApplyOperation(SvgDocument document, SvgPatchOperation operation, int operationIndex)
    {
        if (operation == null) return Failure(SvgPatchError.InvalidArgument, operationIndex, "A patch operation is missing.", null, "");
        SvgNodeApi nodes = new SvgNodeApi();
        SvgDocumentApi documents = new SvgDocumentApi();
        if (operation.Kind == SvgPatchOperationKind.NodeInsert)
        {
            SvgNode? parent = new SvgPatchPath().Resolve(document, operation.ParentPath);
            if (parent == null) return Failure(SvgPatchError.PathConflict, operationIndex, "The node insertion parent path does not exist.", operation.Source, operation.ParentPath);
            if (operation.Node == null || operation.Index < 0 || parent.Children.Count != operation.ExpectedCount || operation.Index > parent.Children.Count) return Failure(SvgPatchError.PreconditionFailed, operationIndex, "The node insertion count or index precondition failed.", parent.SourceSpan, operation.ParentPath);
            SvgNode imported = documents.ImportNode(document, operation.Node, true);
            SvgMutationResult inserted = nodes.InsertChild(parent, operation.Index, imported);
            if (!inserted.Success) return MutationFailure(operationIndex, inserted.Message, parent.SourceSpan, operation.ParentPath);
            return SuccessOperation();
        }
        SvgNode? current = new SvgPatchPath().Resolve(document, operation.Path);
        if (current == null) return Failure(SvgPatchError.PathConflict, operationIndex, "The patch path does not exist.", operation.Source, operation.Path);
        if (operation.Kind == SvgPatchOperationKind.NodeRemove)
        {
            if (current.Parent == null || operation.ExpectedNode == null || current.Parent.Children.Count != operation.ExpectedCount || !nodes.DeepEquals(current, operation.ExpectedNode)) return Failure(SvgPatchError.PreconditionFailed, operationIndex, "The node removal precondition failed.", current.SourceSpan, operation.Path);
            SvgMutationResult removed = nodes.RemoveChild(current.Parent, current);
            if (!removed.Success) return MutationFailure(operationIndex, removed.Message, current.SourceSpan, operation.Path);
            return SuccessOperation();
        }
        if (operation.Kind == SvgPatchOperationKind.NodeReplace)
        {
            if (current.Parent == null || operation.ExpectedNode == null || operation.Node == null || !nodes.DeepEquals(current, operation.ExpectedNode)) return Failure(SvgPatchError.PreconditionFailed, operationIndex, "The node replacement precondition failed.", current.SourceSpan, operation.Path);
            SvgNode imported = documents.ImportNode(document, operation.Node, true);
            SvgMutationResult replaced = nodes.ReplaceChild(current.Parent, current, imported);
            if (!replaced.Success) return MutationFailure(operationIndex, replaced.Message, current.SourceSpan, operation.Path);
            return SuccessOperation();
        }
        if (operation.Kind == SvgPatchOperationKind.NodeValue)
        {
            if (current.Value != operation.ExpectedValue) return Failure(SvgPatchError.PreconditionFailed, operationIndex, "The node value precondition failed.", current.SourceSpan, operation.Path);
            SvgMutationResult changed = nodes.SetTextContent(current, operation.Value);
            if (!changed.Success) return MutationFailure(operationIndex, changed.Message, current.SourceSpan, operation.Path);
            return SuccessOperation();
        }
        if (current.Kind != SvgNodeKind.Element) return Failure(SvgPatchError.PreconditionFailed, operationIndex, "An attribute operation requires an element path.", current.SourceSpan, operation.Path);
        if (current.Attributes.Count != operation.ExpectedCount) return Failure(SvgPatchError.PreconditionFailed, operationIndex, "The attribute count precondition failed.", current.SourceSpan, operation.Path);
        if (operation.Kind == SvgPatchOperationKind.AttributeInsert)
        {
            if (operation.Attribute == null || operation.Index < 0 || operation.Index > current.Attributes.Count) return Failure(SvgPatchError.PreconditionFailed, operationIndex, "The attribute insertion precondition failed.", current.SourceSpan, operation.Path);
            SvgAttribute insertedAttribute = CloneAttribute(operation.Attribute);
            SvgMutationResult inserted = nodes.InsertAttribute(current, operation.Index, insertedAttribute);
            if (!inserted.Success) return MutationFailure(operationIndex, inserted.Message, current.SourceSpan, operation.Path);
            return SuccessOperation();
        }
        if (operation.ExpectedIndex < 0 || operation.ExpectedIndex >= current.Attributes.Count || operation.ExpectedAttribute == null || !EqualAttribute(current.Attributes[operation.ExpectedIndex], operation.ExpectedAttribute)) return Failure(SvgPatchError.PreconditionFailed, operationIndex, "The attribute identity precondition failed.", current.SourceSpan, operation.Path);
        SvgAttribute existing = current.Attributes[operation.ExpectedIndex];
        if (operation.Kind == SvgPatchOperationKind.AttributeRemove)
        {
            SvgMutationResult removed = nodes.RemoveAttribute(current, existing);
            if (!removed.Success) return MutationFailure(operationIndex, removed.Message, current.SourceSpan, operation.Path);
            return SuccessOperation();
        }
        if (operation.Kind == SvgPatchOperationKind.AttributeReplace)
        {
            if (operation.Attribute == null) return Failure(SvgPatchError.InvalidArgument, operationIndex, "The replacement attribute is missing.", current.SourceSpan, operation.Path);
            SvgMutationResult replaced = nodes.ReplaceAttribute(current, existing, CloneAttribute(operation.Attribute));
            if (!replaced.Success) return MutationFailure(operationIndex, replaced.Message, current.SourceSpan, operation.Path);
            return SuccessOperation();
        }
        if (operation.Kind == SvgPatchOperationKind.AttributeMove)
        {
            if (operation.Index < 0 || operation.Index >= current.Attributes.Count) return Failure(SvgPatchError.PreconditionFailed, operationIndex, "The attribute move index precondition failed.", current.SourceSpan, operation.Path);
            SvgMutationResult moved = nodes.MoveAttribute(existing, current, operation.Index);
            if (!moved.Success) return MutationFailure(operationIndex, moved.Message, current.SourceSpan, operation.Path);
            return SuccessOperation();
        }
        return Failure(SvgPatchError.InvalidArgument, operationIndex, "The patch operation kind is invalid.", operation.Source, operation.Path);
    }

    internal SvgPatchResult MutationFailure(int operationIndex, string message, SvgSourceSpan? source, string path)
    {
        return Failure(SvgPatchError.MutationFailed, operationIndex, message.Length == 0 ? "The document mutation failed." : message, source, path);
    }

    internal SvgPatchResult Failure(SvgPatchError error, int operationIndex, string message, SvgSourceSpan? source, string path)
    {
        SvgPatchResult result = new SvgPatchResult();
        result.Error = error;
        result.OperationIndex = operationIndex;
        SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.PatchConflict, message, source);
        diagnostic.InstancePath = path == null ? "" : path;
        Diagnostics.Add(diagnostic);
        result.Diagnostics.Add(diagnostic);
        return result;
    }

    internal static SvgPatchResult SuccessOperation()
    {
        SvgPatchResult result = new SvgPatchResult();
        result.Success = true;
        return result;
    }

    internal static bool SameIdentity(SvgNode before, SvgNode after)
    {
        if (before.Kind != after.Kind) return false;
        if (before.Kind == SvgNodeKind.Element) return before.NamespaceUri == after.NamespaceUri && before.LocalName == after.LocalName;
        if (before.Kind == SvgNodeKind.ProcessingInstruction || before.Kind == SvgNodeKind.Doctype) return before.LocalName == after.LocalName;
        return true;
    }

    internal static int FindExpandedAttribute(System.Collections.Generic.List<SvgAttribute> attributes, SvgAttribute desired, int start)
    {
        int index = start;
        while (index < attributes.Count)
        {
            if (attributes[index].NamespaceUri == desired.NamespaceUri && attributes[index].LocalName == desired.LocalName) return index;
            index = index + 1;
        }
        return -1;
    }

    internal static bool EqualAttribute(SvgAttribute left, SvgAttribute right)
    {
        return left.NamespaceUri == right.NamespaceUri && left.LocalName == right.LocalName && left.Value == right.Value;
    }

    internal static SvgNode CloneNode(SvgNode node)
    {
        return new SvgNodeApi().CloneNode(node, true);
    }

    internal static SvgAttribute CloneAttribute(SvgAttribute attribute)
    {
        return new SvgAttribute(attribute.QualifiedName, attribute.Prefix, attribute.LocalName, attribute.NamespaceUri, attribute.Value);
    }

    internal static SvgDocument CloneDocument(SvgDocument document)
    {
        SvgDocument copy = new SvgDocument();
        SvgDocumentApi documents = new SvgDocumentApi();
        SvgNodeApi nodes = new SvgNodeApi();
        int childIndex = 0;
        while (childIndex < document.DocumentNode.Children.Count)
        {
            SvgNode imported = documents.ImportNode(copy, document.DocumentNode.Children[childIndex], true);
            nodes.AppendChild(copy.DocumentNode, imported);
            childIndex = childIndex + 1;
        }
        copy.BaseUri = document.BaseUri;
        copy.ContentType = document.ContentType;
        copy.ProcessingMode = document.ProcessingMode;
        copy.IsFragment = document.IsFragment;
        copy.Version = document.Version;
        copy.IsDirty = document.IsDirty;
        copy.Index.Invalidate();
        return copy;
    }
}
