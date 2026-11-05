using System;
using System.Collections.Generic;

namespace odf;

internal static class EditSessionModel
{
    internal static EditSessionState BeginSession(
        OdfDocument document,
        string sessionId,
        OdfValidationOptions validationOptions,
        int maximumOperations)
    {
        if (document == null || sessionId == null
            || sessionId.Length == 0 || validationOptions == null
            || maximumOperations <= 0)
        {
            throw new OdfEditException(
                "A document, session identifier, validation options, and positive operation limit are required.");
        }
        return new EditSessionState(
            document,
            sessionId,
            CopyValidationOptions(validationOptions),
            maximumOperations);
    }

    internal static OdfEditResult StageOperation(
        EditSessionState session,
        OdfOperation operation)
    {
        RequireSession(session);
        if (operation == null)
        {
            throw new OdfEditException(
                "An operation is required for staging.");
        }
        if (session.Finished)
        {
            return ConflictResult(
                "A completed edit session cannot stage operations.");
        }
        if (IsCancelled(session))
        {
            return CancelledResult(session);
        }
        if (operation.Id.Length == 0)
        {
            return ConflictResult(
                "A staged operation requires a non-empty identifier.");
        }
        if (operation.SessionId.Length > 0
            && operation.SessionId != session.SessionId)
        {
            return ConflictResult(
                "The operation belongs to a different edit session.");
        }
        if (session.Operations.Count >= session.MaximumOperations)
        {
            return ConflictResult(
                "The edit session operation limit was reached.");
        }
        if (ContainsOperationId(session, operation.Id)
            || DocumentContainsOperationId(
                session.Document,
                operation.Id))
        {
            return ConflictResult(
                "The operation identifier is already present.");
        }
        OdfOperation staged = CopyOperation(operation);
        staged.SessionId = session.SessionId;
        session.Operations.Add(staged);
        return Result(
            OdfEditStatus.Success,
            false,
            1,
            "The operation was staged.",
            [],
            []);
    }

    internal static OdfOperation[] GetStagedOperations(
        EditSessionState session)
    {
        RequireSession(session);
        OdfOperation[] result =
            new OdfOperation[session.Operations.Count];
        int index = 0;
        while (index < session.Operations.Count)
        {
            result[index] = CopyOperation(session.Operations[index]);
            index = index + 1;
        }
        return result;
    }

    internal static OdfEditResult CommitSession(EditSessionState session)
    {
        RequireSession(session);
        if (session.Finished)
        {
            return ConflictResult(
                "A completed edit session cannot be committed again.");
        }
        if (IsCancelled(session))
        {
            return CancelledResult(session);
        }
        if (session.Operations.Count == 0)
        {
            session.Finished = true;
            return Result(
                OdfEditStatus.NoChange,
                false,
                0,
                "The edit session contained no operations.",
                [],
                []);
        }
        if (HasDocumentOperationConflict(session))
        {
            return ConflictResult(
                "A staged operation identifier was added to the document before commit.");
        }
        OdfOperation[] operations = OrderedOperations(session.Operations);
        if (!AllOperationsInvertible(operations))
        {
            return Result(
                OdfEditStatus.Failed,
                false,
                0,
                "A staged operation cannot be safely committed and rolled back.",
                [],
                []);
        }
        OdfOperation[] inverse =
            OperationLogModel.InvertOperations(operations);
        OdfEditResult replay = OperationLogModel.ReplayOperations(
            session.Document,
            operations);
        if (replay.Status != OdfEditStatus.Success)
        {
            return ReplayFailureResult(replay);
        }
        ValidationPipelineResult validation =
            new OdfValidationPipeline(session.ValidationOptions)
                .ValidateDocument(session.Document);
        if (!validation.IsValid)
        {
            OdfEditResult rollback = OperationLogModel.ReplayOperations(
                session.Document,
                inverse);
            if (rollback.Status != OdfEditStatus.Success)
            {
                session.Finished = true;
                return Result(
                    OdfEditStatus.Failed,
                    true,
                    operations.Length,
                    "Validation failed and the edit session could not be rolled back.",
                    validation.Diagnostics,
                    CreateMutations(operations));
            }
            return Result(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "The staged operations were rejected by validation.",
                validation.Diagnostics,
                []);
        }
        AppendCommittedOperations(session.Document, operations);
        RefreshDocument(session.Document);
        session.Finished = true;
        return Result(
            OdfEditStatus.Success,
            true,
            operations.Length,
            "The edit session was committed.",
            validation.Diagnostics,
            CreateMutations(operations));
    }

    internal static OdfEditResult DiscardSession(EditSessionState session)
    {
        RequireSession(session);
        if (session.Finished)
        {
            return ConflictResult(
                "A completed edit session cannot be discarded again.");
        }
        int affected = session.Operations.Count;
        session.Operations.Clear();
        session.Finished = true;
        session.Discarded = true;
        return Result(
            affected == 0
                ? OdfEditStatus.NoChange
                : OdfEditStatus.Success,
            false,
            affected,
            affected == 0
                ? "The edit session contained no operations."
                : "The staged operations were discarded.",
            [],
            []);
    }

    static OdfEditResult ReplayFailureResult(OdfEditResult replay)
    {
        OdfDiagnostic[] diagnostics = replay.Diagnostics;
        if (diagnostics.Length == 0)
        {
            diagnostics =
            [
                Diagnostic(
                    OdfDiagnosticCodes.EditConflict,
                    replay.Message,
                    OdfValidationSeverity.Error)
            ];
        }
        return Result(
            replay.Status,
            false,
            0,
            replay.Message,
            diagnostics,
            []);
    }

    static OdfEditResult ConflictResult(string message)
    {
        return Result(
            OdfEditStatus.Conflict,
            false,
            0,
            message,
            [
                Diagnostic(
                    OdfDiagnosticCodes.EditConflict,
                    message,
                    OdfValidationSeverity.Error)
            ],
            []);
    }

    static OdfEditResult CancelledResult(EditSessionState session)
    {
        string message = "The edit session was cancelled.";
        if (session.ValidationOptions.Cancellation.Reason.Length > 0)
        {
            message = message + " "
                + session.ValidationOptions.Cancellation.Reason;
        }
        return Result(
            OdfEditStatus.Cancelled,
            false,
            0,
            message,
            [
                Diagnostic(
                    OdfDiagnosticCodes.OperationCancelled,
                    message,
                    OdfValidationSeverity.Fatal)
            ],
            []);
    }

    static OdfEditResult Result(
        OdfEditStatus status,
        bool changed,
        int affected,
        string message,
        OdfDiagnostic[] diagnostics,
        OdfEditMutation[] mutations)
    {
        OdfEditResult result = new OdfEditResult();
        result.Status = status;
        result.Changed = changed;
        result.AffectedCount = affected;
        result.Message = message;
        result.Diagnostics = diagnostics;
        result.Mutations = mutations;
        return result;
    }

    static OdfDiagnostic Diagnostic(
        string code,
        string message,
        OdfValidationSeverity severity)
    {
        OdfDiagnostic result = new OdfDiagnostic();
        result.Code = code;
        result.Message = message;
        result.Severity = severity;
        result.IsRecoverable = false;
        return result;
    }

    static OdfEditMutation[] CreateMutations(OdfOperation[] operations)
    {
        OdfEditMutation[] result =
            new OdfEditMutation[operations.Length];
        int index = 0;
        while (index < operations.Length)
        {
            result[index] = CreateMutation(operations[index]);
            index = index + 1;
        }
        return result;
    }

    static OdfEditMutation CreateMutation(OdfOperation operation)
    {
        OdfEditMutation result = new OdfEditMutation();
        result.OperationId = operation.Id;
        result.Kind = operation.Kind;
        result.PartPath = operation.PartPath;
        result.TargetNodeId = operation.TargetNodeId;
        result.BeforeValue = MutationBeforeValue(operation);
        result.AfterValue = MutationAfterValue(operation);
        result.Description = MutationDescription(operation.Kind);
        result.Location.PartPath = operation.PartPath;
        result.Location.XmlId = operation.TargetNodeId;
        return result;
    }

    static string MutationBeforeValue(OdfOperation operation)
    {
        if (operation.Kind == OdfOperationKind.SetAttribute
            || operation.Kind == OdfOperationKind.RemoveAttribute
            || operation.Kind == OdfOperationKind.SetText)
        {
            return operation.HadPreviousValue
                ? operation.PreviousValue : "";
        }
        if (operation.Kind == OdfOperationKind.RemoveNode)
        {
            return operation.TargetNodeId;
        }
        if (operation.Kind == OdfOperationKind.MoveNode)
        {
            return operation.PreviousParentNodeId + "#"
                + operation.PreviousChildIndex;
        }
        return "";
    }

    static string MutationAfterValue(OdfOperation operation)
    {
        if (operation.Kind == OdfOperationKind.SetAttribute
            || operation.Kind == OdfOperationKind.SetText)
        {
            return operation.Value;
        }
        if (operation.Kind == OdfOperationKind.InsertNode)
        {
            return operation.TargetNodeId;
        }
        if (operation.Kind == OdfOperationKind.MoveNode)
        {
            return operation.ParentNodeId + "#" + operation.ChildIndex;
        }
        return "";
    }

    static string MutationDescription(OdfOperationKind kind)
    {
        if (kind == OdfOperationKind.InsertNode)
        {
            return "Inserted an XML node.";
        }
        if (kind == OdfOperationKind.RemoveNode)
        {
            return "Removed an XML node.";
        }
        if (kind == OdfOperationKind.MoveNode)
        {
            return "Moved an XML node.";
        }
        if (kind == OdfOperationKind.SetAttribute)
        {
            return "Set an XML attribute.";
        }
        if (kind == OdfOperationKind.RemoveAttribute)
        {
            return "Removed an XML attribute.";
        }
        if (kind == OdfOperationKind.SetText)
        {
            return "Set XML text content.";
        }
        return "Applied an edit operation.";
    }

    static OdfOperation[] OrderedOperations(
        List<OdfOperation> operations)
    {
        OdfOperation[] result = operations.ToArray();
        int index = 1;
        while (index < result.Length)
        {
            OdfOperation current = result[index];
            int insertion = index;
            while (insertion > 0
                && result[insertion - 1].Sequence > current.Sequence)
            {
                result[insertion] = result[insertion - 1];
                insertion = insertion - 1;
            }
            result[insertion] = current;
            index = index + 1;
        }
        return result;
    }

    static bool AllOperationsInvertible(OdfOperation[] operations)
    {
        int index = 0;
        while (index < operations.Length)
        {
            OdfOperation operation = operations[index];
            if (operation.Kind == OdfOperationKind.RemoveNode
                && operation.Payload == null)
            {
                return false;
            }
            if (operation.Kind == OdfOperationKind.MoveNode
                && (operation.PreviousParentNodeId.Length == 0
                    || operation.PreviousChildIndex < 0))
            {
                return false;
            }
            if ((operation.Kind == OdfOperationKind.SetAttribute
                    || operation.Kind
                        == OdfOperationKind.RemoveAttribute)
                && operation.AttributeName == null)
            {
                return false;
            }
            if (operation.Kind == OdfOperationKind.RemoveAttribute
                && !operation.HadPreviousValue)
            {
                return false;
            }
            if (operation.Kind == OdfOperationKind.SetText
                && !operation.HadPreviousValue)
            {
                return false;
            }
            if (operation.Kind != OdfOperationKind.InsertNode
                && operation.Kind != OdfOperationKind.RemoveNode
                && operation.Kind != OdfOperationKind.MoveNode
                && operation.Kind != OdfOperationKind.SetAttribute
                && operation.Kind != OdfOperationKind.RemoveAttribute
                && operation.Kind != OdfOperationKind.SetText)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static void AppendCommittedOperations(
        OdfDocument document,
        OdfOperation[] operations)
    {
        if (document.Operations == null)
        {
            throw new OdfDocumentException(
                "The document operation collection is required.");
        }
        OdfOperation[] updated = new OdfOperation[
            document.Operations.Length + operations.Length];
        Array.Copy(
            document.Operations,
            updated,
            document.Operations.Length);
        int index = 0;
        while (index < operations.Length)
        {
            updated[document.Operations.Length + index] =
                CopyOperation(operations[index]);
            index = index + 1;
        }
        document.Operations = updated;
    }

    static bool HasDocumentOperationConflict(EditSessionState session)
    {
        int index = 0;
        while (index < session.Operations.Count)
        {
            if (DocumentContainsOperationId(
                session.Document,
                session.Operations[index].Id))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ContainsOperationId(
        EditSessionState session,
        string operationId)
    {
        int index = 0;
        while (index < session.Operations.Count)
        {
            if (session.Operations[index].Id == operationId)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool DocumentContainsOperationId(
        OdfDocument document,
        string operationId)
    {
        if (document.Operations == null)
        {
            throw new OdfDocumentException(
                "The document operation collection is required.");
        }
        int index = 0;
        while (index < document.Operations.Length)
        {
            OdfOperation? operation = document.Operations[index];
            if (operation != null && operation.Id == operationId)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool IsCancelled(EditSessionState session)
    {
        return session.ValidationOptions.Cancellation
            .IsCancellationRequested;
    }

    static void RefreshDocument(OdfDocument document)
    {
        OdfDocumentModel.ApplyContext(
            document,
            document.LoadOptions,
            document.SaveOptions,
            document.MediaType);
    }

    static OdfValidationOptions CopyValidationOptions(
        OdfValidationOptions source)
    {
        OdfValidationOptions result = new OdfValidationOptions();
        result.Mode = source.Mode;
        result.SecurityLimits = source.SecurityLimits;
        result.Cancellation = source.Cancellation;
        result.Version = source.Version;
        result.ConformanceClass = source.ConformanceClass;
        result.CompatibilityProfile = source.CompatibilityProfile;
        result.ValidatePackage = source.ValidatePackage;
        result.ValidateXml = source.ValidateXml;
        result.ValidateSemantics = source.ValidateSemantics;
        result.ValidateFormulas = source.ValidateFormulas;
        result.ValidateSecurity = source.ValidateSecurity;
        result.IncludeWarnings = source.IncludeWarnings;
        result.IncludeInformation = source.IncludeInformation;
        return result;
    }

    static OdfOperation CopyOperation(OdfOperation source)
    {
        OdfOperation result = new OdfOperation();
        result.Id = source.Id;
        result.Sequence = source.Sequence;
        result.Kind = source.Kind;
        result.PartPath = source.PartPath;
        result.TargetNodeId = source.TargetNodeId;
        result.ParentNodeId = source.ParentNodeId;
        result.ChildIndex = source.ChildIndex;
        result.PreviousParentNodeId = source.PreviousParentNodeId;
        result.PreviousChildIndex = source.PreviousChildIndex;
        result.AttributeName = CopyName(source.AttributeName);
        result.Value = source.Value;
        result.HadPreviousValue = source.HadPreviousValue;
        result.PreviousValue = source.PreviousValue;
        result.Author = source.Author;
        result.SessionId = source.SessionId;
        result.Date = source.Date;
        result.Preconditions = CopyPreconditions(source.Preconditions);
        result.Payload = CopyNode(source.Payload);
        result.ExtensionNode = CopyNode(source.ExtensionNode);
        return result;
    }

    static OdfEditPrecondition[] CopyPreconditions(
        OdfEditPrecondition[] source)
    {
        if (source == null)
        {
            return [];
        }
        OdfEditPrecondition[] result =
            new OdfEditPrecondition[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            OdfEditPrecondition? item = source[index];
            if (item != null)
            {
                OdfEditPrecondition copy = new OdfEditPrecondition();
                copy.Kind = item.Kind;
                copy.TargetNodeId = item.TargetNodeId;
                copy.ParentNodeId = item.ParentNodeId;
                copy.ChildIndex = item.ChildIndex;
                copy.AttributeName = CopyName(item.AttributeName);
                copy.ExpectedValuePresent = item.ExpectedValuePresent;
                copy.ExpectedValue = item.ExpectedValue;
                result[index] = copy;
            }
            else
            {
                result[index] = new OdfEditPrecondition();
            }
            index = index + 1;
        }
        return result;
    }

    static OdfQualifiedName? CopyName(OdfQualifiedName? source)
    {
        if (source == null)
        {
            return null;
        }
        OdfQualifiedName result = new OdfQualifiedName();
        result.NamespaceName = source.NamespaceName;
        result.LocalName = source.LocalName;
        result.Prefix = source.Prefix;
        return result;
    }

    static OdfNode? CopyNode(OdfNode? source)
    {
        if (source == null)
        {
            return null;
        }
        return new OdfTreeEditor([]).CloneSubtree(source);
    }

    static void RequireSession(EditSessionState session)
    {
        if (session == null)
        {
            throw new OdfEditException(
                "An edit session is required.");
        }
    }
}
