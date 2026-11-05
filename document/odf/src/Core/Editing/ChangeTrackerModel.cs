using System;

namespace odf;

internal static class ChangeTrackerModel
{
    internal static OdfChange[] GetTextChanges(OdfDocument document)
    {
        RequireDocument(document);
        OdfChange[] result = new OdfChange[document.Changes.Length];
        Array.Copy(document.Changes, result, result.Length);
        return result;
    }

    internal static OdfSpreadsheetChange[] GetSpreadsheetChanges(
        OdfDocument document)
    {
        RequireDocument(document);
        OdfSpreadsheetChange[] result =
            new OdfSpreadsheetChange[document.SpreadsheetChanges.Length];
        Array.Copy(document.SpreadsheetChanges, result, result.Length);
        return result;
    }

    internal static OdfOperation[] GetOperations(OdfDocument document)
    {
        RequireDocument(document);
        if (document.Operations == null)
        {
            throw new OdfDocumentException(
                "The document operation collection is required.");
        }
        OdfOperation[] result = new OdfOperation[document.Operations.Length];
        Array.Copy(document.Operations, result, result.Length);
        return result;
    }

    internal static OdfOperation RecordInsertNode(
        OdfDocument document,
        OdfNode parent,
        int childIndex,
        OdfNode payload)
    {
        OdfOperation operation = OperationLogModel.CreateInsertNodeOperation(
            document, parent, childIndex, payload, NextSequence(document));
        AppendOperation(document, operation);
        return operation;
    }

    internal static OdfOperation RecordRemoveNode(
        OdfDocument document,
        OdfNode target)
    {
        OdfOperation operation = OperationLogModel.CreateRemoveNodeOperation(
            document, target, NextSequence(document));
        AppendOperation(document, operation);
        return operation;
    }

    internal static OdfOperation RecordMoveNode(
        OdfDocument document,
        OdfNode target,
        OdfNode destinationParent,
        int destinationIndex)
    {
        OdfOperation operation = OperationLogModel.CreateMoveNodeOperation(
            document,
            target,
            destinationParent,
            destinationIndex,
            NextSequence(document));
        AppendOperation(document, operation);
        return operation;
    }

    internal static OdfOperation RecordSetAttribute(
        OdfDocument document,
        OdfNode target,
        OdfQualifiedName attributeName,
        string value)
    {
        OdfOperation operation =
            OperationLogModel.CreateSetAttributeOperation(
                document,
                target,
                attributeName,
                value,
                NextSequence(document));
        AppendOperation(document, operation);
        return operation;
    }

    internal static OdfOperation RecordRemoveAttribute(
        OdfDocument document,
        OdfNode target,
        OdfQualifiedName attributeName)
    {
        OdfOperation operation =
            OperationLogModel.CreateRemoveAttributeOperation(
                document,
                target,
                attributeName,
                NextSequence(document));
        AppendOperation(document, operation);
        return operation;
    }

    internal static OdfOperation RecordSetText(
        OdfDocument document,
        OdfNode target,
        string value)
    {
        OdfOperation operation = OperationLogModel.CreateSetTextOperation(
            document, target, value, NextSequence(document));
        AppendOperation(document, operation);
        return operation;
    }

    internal static OdfOperation RecordOperation(
        OdfDocument document,
        OdfOperation operation)
    {
        RequireDocument(document);
        if (operation == null)
        {
            throw new OdfEditException("An operation is required for recording.");
        }
        if (operation.Id.Length == 0)
        {
            operation.Id = "odf-operation-"
                + Guid.NewGuid().ToString("N");
        }
        AppendOperation(document, operation);
        return operation;
    }

    internal static OdfEditResult ReplayOperations(
        OdfDocument document,
        OdfOperation[] operations)
    {
        OdfEditResult result = OperationLogModel.ReplayOperations(
            document, operations);
        if (result.Status == OdfEditStatus.Success)
        {
            RefreshDocument(document);
        }
        return result;
    }

    internal static OdfEditResult ReplayRecordedOperations(
        OdfDocument document)
    {
        return ReplayOperations(document, GetOperations(document));
    }

    internal static byte[] ExportOperations(
        OdfDocument document,
        OdfSaveOptions options)
    {
        RequireDocument(document);
        if (options == null)
        {
            throw new OdfEditException(
                "Save options are required for operation export.");
        }
        OdfNode root = OperationExtensionModel.SerializeOperationExtension(
            GetOperations(document));
        return OdfTreeWriter.WriteBytes([root], options);
    }

    internal static OdfOperation[] ImportOperationsBytes(
        byte[] data,
        OdfLoadOptions options)
    {
        if (data == null || options == null)
        {
            throw new OdfEditException(
                "Operation XML bytes and load options are required.");
        }
        OdfNode[] roots = OdfTreeBuilder.ReadBytes(
            data, "operations.xml", options);
        if (roots.Length != 1)
        {
            throw new OdfEditException(
                "Operation extension XML requires exactly one root element.");
        }
        return OperationExtensionModel.DeserializeOperationExtension(roots[0]);
    }

    static void AppendOperation(
        OdfDocument document,
        OdfOperation operation)
    {
        RequireDocument(document);
        if (document.Operations == null)
        {
            throw new OdfDocumentException(
                "The document operation collection is required.");
        }
        if (operation.Id.Length == 0 || ContainsOperationId(
                document.Operations, operation.Id))
        {
            throw new OdfEditException(
                "A recorded operation requires a unique non-empty identifier.");
        }
        OdfOperation[] operations =
            new OdfOperation[document.Operations.Length + 1];
        Array.Copy(document.Operations, operations, document.Operations.Length);
        operations[operations.Length - 1] = operation;
        document.Operations = operations;
    }

    static long NextSequence(OdfDocument document)
    {
        RequireDocument(document);
        if (document.Operations == null)
        {
            throw new OdfDocumentException(
                "The document operation collection is required.");
        }
        long result = 0;
        int index = 0;
        while (index < document.Operations.Length)
        {
            OdfOperation operation = document.Operations[index];
            if (operation == null)
            {
                throw new OdfDocumentException(
                    "The document operation collection contains a null operation.");
            }
            if (operation.Sequence >= result)
            {
                if (operation.Sequence == long.MaxValue)
                {
                    throw new OdfEditException(
                        "The operation sequence cannot be incremented.");
                }
                result = operation.Sequence + 1;
            }
            index = index + 1;
        }
        return result;
    }

    static bool ContainsOperationId(
        OdfOperation[] operations,
        string id)
    {
        int index = 0;
        while (index < operations.Length)
        {
            OdfOperation operation = operations[index];
            if (operation != null && operation.Id == id)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void RefreshDocument(OdfDocument document)
    {
        OdfDocumentModel.ApplyContext(
            document,
            document.LoadOptions,
            document.SaveOptions,
            document.MediaType);
    }

    static void RequireDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException("A document is required.");
        }
    }
}
