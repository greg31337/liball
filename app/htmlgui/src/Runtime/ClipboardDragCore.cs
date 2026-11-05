using System;
using System.Collections.Generic;

namespace htmlgui;

internal class TransferPolicy
{
    internal bool AllowCopy = true;
    internal bool AllowCut = true;
    internal bool AllowPaste = true;
    internal bool AllowKeyboardDrag = true;
    internal bool RequireTrusted = true;
    internal bool RequireMainFrame = true;
    internal string ExpectedOrigin = string.Empty;
    internal string[] AllowedMimeTypes = new string[] { "application/json", "application/octet-stream", "text/html", "text/plain" };
    internal int MaximumEntryCount = 16;
    internal int MaximumValueDepth = 32;
    internal int MaximumCollectionCount = 1024;
    internal int MaximumTextLength = 262144;
    internal int MaximumBinaryLength = 1048576;
}

internal class TransferValidationState
{
    internal int CollectionCount;
    internal List<UiValue> Ancestors = new List<UiValue>();
}

internal class DragTransferState
{
    internal bool Active;
    internal bool DropCompleted;
    internal bool Canceled;
    internal string SourceId = string.Empty;
    internal string TargetId = string.Empty;
    internal string Effect = "none";
    internal string AllowedEffect = "none";
    internal UiValueEntry[] Data = Array.Empty<UiValueEntry>();
    internal bool IsKeyboard;
}

internal class TransferOperationResult
{
    internal bool Success;
    internal bool Allowed;
    internal bool Canceled;
    internal bool DropCompleted;
    internal string Code = string.Empty;
    internal UiClipboardData? Clipboard;
    internal UiDragData? Drag;
}

internal class ClipboardDragCore
{
    internal TransferPolicy Policy = new TransferPolicy();
    internal DragTransferState DragState = new DragTransferState();

    internal TransferOperationResult ConfigureTransferPolicy(TransferPolicy? policy)
    {
        if (policy == null)
        {
            return CreateTransferFailure("transfer.null-policy");
        }

        if (policy.MaximumEntryCount < 0 || policy.MaximumValueDepth < 0 || policy.MaximumValueDepth > 128 || policy.MaximumCollectionCount < 0 || policy.MaximumTextLength < 0 || policy.MaximumBinaryLength < 0)
        {
            return CreateTransferFailure("transfer.invalid-limit");
        }

        string? expectedOrigin = policy.ExpectedOrigin;
        if (expectedOrigin == null || expectedOrigin.Length > 0 && expectedOrigin.Trim().Length == 0)
        {
            return CreateTransferFailure("transfer.invalid-origin");
        }

        string[]? normalizedTypes = NormalizeAllowedMimeTypes(policy.AllowedMimeTypes);
        if (normalizedTypes == null)
        {
            return CreateTransferFailure("transfer.invalid-policy-mime");
        }

        TransferPolicy copy = new TransferPolicy();
        copy.AllowCopy = policy.AllowCopy;
        copy.AllowCut = policy.AllowCut;
        copy.AllowPaste = policy.AllowPaste;
        copy.AllowKeyboardDrag = policy.AllowKeyboardDrag;
        copy.RequireTrusted = policy.RequireTrusted;
        copy.RequireMainFrame = policy.RequireMainFrame;
        copy.ExpectedOrigin = expectedOrigin;
        copy.AllowedMimeTypes = normalizedTypes;
        copy.MaximumEntryCount = policy.MaximumEntryCount;
        copy.MaximumValueDepth = policy.MaximumValueDepth;
        copy.MaximumCollectionCount = policy.MaximumCollectionCount;
        copy.MaximumTextLength = policy.MaximumTextLength;
        copy.MaximumBinaryLength = policy.MaximumBinaryLength;
        Policy = copy;
        ResetDragTransferState();
        return new TransferOperationResult { Success = true, Allowed = true };
    }

    internal TransferOperationResult ProcessClipboardTransfer(UiEventKind kind, UiClipboardData? clipboard, bool isTrusted, bool isMainFrame, string? origin)
    {
        string operation = GetClipboardTransferOperation(kind);
        if (operation.Length == 0)
        {
            return CreateTransferFailure("clipboard.invalid-operation");
        }

        TransferOperationResult security = ValidateTransferSecurity(isTrusted, isMainFrame, origin);
        if (!security.Success)
        {
            return security;
        }

        if (!IsClipboardOperationAllowed(operation))
        {
            TransferOperationResult denied = new TransferOperationResult();
            denied.Success = true;
            denied.Code = "clipboard.operation-denied";
            denied.Clipboard = new UiClipboardData { Operation = operation, IsAllowed = false };
            return denied;
        }

        if (clipboard == null)
        {
            return CreateTransferFailure("clipboard.null-data");
        }

        TransferOperationResult payload = NormalizeTransferPayload(clipboard.Data);
        if (!payload.Success || payload.Drag == null)
        {
            return payload;
        }

        UiClipboardData result = new UiClipboardData();
        result.Operation = operation;
        result.Data = payload.Drag.Data;
        result.IsAllowed = true;
        return new TransferOperationResult { Success = true, Allowed = true, Clipboard = result };
    }

    internal TransferOperationResult BeginKeyboardDrag(string? sourceId, UiValueEntry[]? data, string? allowedEffect, bool isTrusted, bool isMainFrame, string? origin)
    {
        UiDragData drag = new UiDragData();
        drag.SourceId = sourceId;
        drag.AllowedEffect = allowedEffect ?? string.Empty;
        drag.Data = data ?? Array.Empty<UiValueEntry>();
        drag.IsKeyboard = true;
        return ProcessDragTransfer(UiEventKind.DragStart, drag, isTrusted, isMainFrame, origin, false);
    }

    internal TransferOperationResult ProcessDragTransfer(UiEventKind kind, UiDragData? drag, bool isTrusted, bool isMainFrame, string? origin, bool canceled)
    {
        if (!IsDragTransferKind(kind))
        {
            return CreateTransferFailure("drag.invalid-operation");
        }

        TransferOperationResult security = ValidateTransferSecurity(isTrusted, isMainFrame, origin);
        if (!security.Success)
        {
            return security;
        }

        if (canceled)
        {
            if (!DragState.Active)
            {
                return CreateTransferFailure("drag.not-active");
            }

            DragState.Active = false;
            DragState.Canceled = true;
            DragState.Effect = "none";
            return CreateDragTransferResult(true, false, true, false, string.Empty);
        }

        switch (kind)
        {
            case UiEventKind.DragStart:
                return StartDragTransfer(drag);
            case UiEventKind.Drag:
                return ContinueDragTransfer(drag);
            case UiEventKind.DragEnter:
            case UiEventKind.DragOver:
                return EnterDragTransferTarget(drag);
            case UiEventKind.DragLeave:
                return LeaveDragTransferTarget(drag);
            case UiEventKind.Drop:
                return CompleteDragTransfer(drag);
            case UiEventKind.DragEnd:
                return EndDragTransfer(drag);
            default:
                return CreateTransferFailure("drag.invalid-operation");
        }
    }

    internal TransferOperationResult StartDragTransfer(UiDragData? drag)
    {
        if (DragState.Active)
        {
            return CreateTransferFailure("drag.already-active");
        }

        if (drag == null)
        {
            return CreateTransferFailure("drag.null-data");
        }

        if (string.IsNullOrWhiteSpace(drag.SourceId))
        {
            return CreateTransferFailure("drag.invalid-source");
        }

        if (drag.IsKeyboard && !Policy.AllowKeyboardDrag)
        {
            TransferOperationResult denied = new TransferOperationResult();
            denied.Success = true;
            denied.Code = "drag.keyboard-denied";
            return denied;
        }

        string allowedEffect = NormalizeAllowedDragEffect(drag.AllowedEffect);
        if (allowedEffect.Length == 0)
        {
            return CreateTransferFailure("drag.invalid-allowed-effect");
        }

        TransferOperationResult payload = NormalizeTransferPayload(drag.Data);
        if (!payload.Success || payload.Drag == null)
        {
            return payload;
        }

        DragState = new DragTransferState();
        DragState.Active = true;
        DragState.SourceId = drag.SourceId;
        DragState.AllowedEffect = allowedEffect;
        DragState.Data = payload.Drag.Data;
        DragState.IsKeyboard = drag.IsKeyboard;
        return CreateDragTransferResult(true, true, false, false, string.Empty);
    }

    internal TransferOperationResult ContinueDragTransfer(UiDragData? drag)
    {
        TransferOperationResult identity = ValidateActiveDragIdentity(drag, false);
        if (!identity.Success)
        {
            return identity;
        }

        return CreateDragTransferResult(true, DragState.Effect != "none", false, false, string.Empty);
    }

    internal TransferOperationResult EnterDragTransferTarget(UiDragData? drag)
    {
        TransferOperationResult identity = ValidateActiveDragIdentity(drag, true);
        if (!identity.Success || drag == null)
        {
            return identity;
        }

        string effect = NormalizeDragEffect(drag.Effect);
        if (effect.Length == 0)
        {
            return CreateTransferFailure("drag.invalid-effect");
        }

        DragState.TargetId = drag.TargetId ?? string.Empty;
        if (!IsDragEffectAllowed(DragState.AllowedEffect, effect))
        {
            DragState.Effect = "none";
            return CreateDragTransferResult(true, false, false, false, "drag.effect-not-allowed");
        }

        DragState.Effect = effect;
        return CreateDragTransferResult(true, effect != "none", false, false, string.Empty);
    }

    internal TransferOperationResult LeaveDragTransferTarget(UiDragData? drag)
    {
        TransferOperationResult identity = ValidateActiveDragIdentity(drag, true);
        if (!identity.Success || drag == null)
        {
            return identity;
        }

        if (!ValueComparer.TextEquals(DragState.TargetId, drag.TargetId))
        {
            return CreateTransferFailure("drag.target-mismatch");
        }

        DragState.TargetId = string.Empty;
        DragState.Effect = "none";
        return CreateDragTransferResult(true, false, false, false, string.Empty);
    }

    internal TransferOperationResult CompleteDragTransfer(UiDragData? drag)
    {
        TransferOperationResult identity = ValidateActiveDragIdentity(drag, true);
        if (!identity.Success || drag == null)
        {
            return identity;
        }

        if (!ValueComparer.TextEquals(DragState.TargetId, drag.TargetId))
        {
            return CreateTransferFailure("drag.target-mismatch");
        }

        if (DragState.Effect == "none")
        {
            return CreateDragTransferResult(true, false, false, false, "drag.drop-denied");
        }

        DragState.DropCompleted = true;
        DragState.Active = false;
        return CreateDragTransferResult(true, true, false, true, string.Empty);
    }

    internal TransferOperationResult EndDragTransfer(UiDragData? drag)
    {
        if (!DragState.Active && !DragState.DropCompleted && !DragState.Canceled)
        {
            return CreateTransferFailure("drag.not-active");
        }

        if (drag != null && drag.SourceId != null && !ValueComparer.TextEquals(drag.SourceId, DragState.SourceId))
        {
            return CreateTransferFailure("drag.source-mismatch");
        }

        bool completed = DragState.DropCompleted;
        TransferOperationResult result = CreateDragTransferResult(true, completed, DragState.Canceled || !completed, completed, string.Empty);
        ResetDragTransferState();
        return result;
    }

    internal TransferOperationResult ValidateActiveDragIdentity(UiDragData? drag, bool requireTarget)
    {
        if (!DragState.Active)
        {
            return CreateTransferFailure("drag.not-active");
        }

        if (drag == null)
        {
            return CreateTransferFailure("drag.null-data");
        }

        if (drag.SourceId != null && !ValueComparer.TextEquals(drag.SourceId, DragState.SourceId))
        {
            return CreateTransferFailure("drag.source-mismatch");
        }

        if (requireTarget && string.IsNullOrWhiteSpace(drag.TargetId))
        {
            return CreateTransferFailure("drag.invalid-target");
        }

        return new TransferOperationResult { Success = true, Allowed = true };
    }

    internal TransferOperationResult ValidateTransferSecurity(bool isTrusted, bool isMainFrame, string? origin)
    {
        if (Policy.RequireTrusted && !isTrusted)
        {
            return CreateTransferFailure("transfer.untrusted");
        }

        if (Policy.RequireMainFrame && !isMainFrame)
        {
            return CreateTransferFailure("transfer.main-frame-required");
        }

        if (Policy.ExpectedOrigin.Length > 0 && !ValueComparer.TextEquals(Policy.ExpectedOrigin, origin))
        {
            return CreateTransferFailure("transfer.origin-mismatch");
        }

        return new TransferOperationResult { Success = true, Allowed = true };
    }

    internal TransferOperationResult NormalizeTransferPayload(UiValueEntry[]? data)
    {
        UiValueEntry[] source = data ?? Array.Empty<UiValueEntry>();
        if (source.Length > Policy.MaximumEntryCount)
        {
            return CreateTransferFailure("transfer.too-many-items");
        }

        List<UiValueEntry> normalized = new List<UiValueEntry>();
        int index = 0;
        while (index < source.Length)
        {
            UiValueEntry? entry = source[index];
            if (entry == null)
            {
                return CreateTransferFailure("transfer.null-entry");
            }

            string mimeType = NormalizeMimeType(entry.Key);
            if (mimeType.Length == 0 || mimeType == "*/*" || mimeType.EndsWith("/*", StringComparison.Ordinal))
            {
                return CreateTransferFailure("transfer.invalid-mime");
            }

            if (ContainsTransferMimeType(normalized, mimeType))
            {
                return CreateTransferFailure("transfer.duplicate-mime");
            }

            if (!IsTransferMimeTypeAllowed(mimeType))
            {
                return CreateTransferFailure("transfer.mime-not-allowed");
            }

            TransferValidationState state = new TransferValidationState();
            string valueCode = ValidateTransferValue(entry.Value, 0, state);
            if (valueCode.Length > 0)
            {
                return CreateTransferFailure(valueCode);
            }

            UiValueEntry copy = new UiValueEntry();
            copy.Key = mimeType;
            copy.Value = ValueJsonCodec.CloneValue(entry.Value, Policy.MaximumValueDepth, Policy.MaximumCollectionCount, Policy.MaximumTextLength, Policy.MaximumBinaryLength);
            InsertTransferEntry(normalized, copy);
            index++;
        }

        UiDragData payload = new UiDragData();
        payload.Data = normalized.ToArray();
        return new TransferOperationResult { Success = true, Allowed = true, Drag = payload };
    }

    internal string ValidateTransferValue(UiValue? value, int depth, TransferValidationState state)
    {
        if (value == null)
        {
            return "transfer.invalid-value";
        }

        if (depth > Policy.MaximumValueDepth)
        {
            return "transfer.value-too-deep";
        }

        int ancestorIndex = 0;
        while (ancestorIndex < state.Ancestors.Count)
        {
            if (object.ReferenceEquals(state.Ancestors[ancestorIndex], value))
            {
                return "transfer.value-cycle";
            }

            ancestorIndex++;
        }

        state.Ancestors.Add(value);
        string shapeCode = ValidateTransferValueShape(value);
        if (shapeCode.Length > 0)
        {
            return shapeCode;
        }

        if (value.Kind == UiValueKind.Array)
        {
            UiValue[] items = value.ArrayValue ?? Array.Empty<UiValue>();
            if (!AddTransferCollectionCount(items.Length, state))
            {
                return "transfer.value-too-large";
            }

            int itemIndex = 0;
            while (itemIndex < items.Length)
            {
                string itemCode = ValidateTransferValue(items[itemIndex], depth + 1, state);
                if (itemCode.Length > 0)
                {
                    return itemCode;
                }

                itemIndex++;
            }
        }
        else if (value.Kind == UiValueKind.Object)
        {
            UiValueEntry[] entries = value.ObjectValue ?? Array.Empty<UiValueEntry>();
            if (!AddTransferCollectionCount(entries.Length, state))
            {
                return "transfer.value-too-large";
            }

            int entryIndex = 0;
            while (entryIndex < entries.Length)
            {
                UiValueEntry? entry = entries[entryIndex];
                if (entry == null || entry.Key == null || entry.Key.Length > Policy.MaximumTextLength)
                {
                    return "transfer.invalid-value";
                }

                int priorIndex = 0;
                while (priorIndex < entryIndex)
                {
                    if (ValueComparer.TextEquals(entries[priorIndex].Key, entry.Key))
                    {
                        return "transfer.invalid-value";
                    }

                    priorIndex++;
                }

                string entryCode = ValidateTransferValue(entry.Value, depth + 1, state);
                if (entryCode.Length > 0)
                {
                    return entryCode;
                }

                entryIndex++;
            }
        }

        state.Ancestors.RemoveAt(state.Ancestors.Count - 1);
        return string.Empty;
    }

    internal string ValidateTransferValueShape(UiValue value)
    {
        bool hasReference = value.StringValue != null || value.DateTimeValue != null || value.BinaryValue != null || value.ArrayValue != null || value.ObjectValue != null;
        bool hasPrimitive = value.BooleanValue || value.IntegerValue != 0L || value.FloatingPointValue != 0.0 || value.DecimalValue != 0m;
        switch (value.Kind)
        {
            case UiValueKind.Null:
                return hasReference || hasPrimitive ? "transfer.invalid-value" : string.Empty;
            case UiValueKind.Boolean:
                return hasReference || value.IntegerValue != 0L || value.FloatingPointValue != 0.0 || value.DecimalValue != 0m ? "transfer.invalid-value" : string.Empty;
            case UiValueKind.Integer:
                return hasReference || value.BooleanValue || value.FloatingPointValue != 0.0 || value.DecimalValue != 0m ? "transfer.invalid-value" : string.Empty;
            case UiValueKind.FloatingPoint:
                if (hasReference || value.BooleanValue || value.IntegerValue != 0L || value.DecimalValue != 0m || double.IsNaN(value.FloatingPointValue) || double.IsInfinity(value.FloatingPointValue))
                {
                    return "transfer.invalid-value";
                }

                return string.Empty;
            case UiValueKind.Decimal:
                return hasReference || value.BooleanValue || value.IntegerValue != 0L || value.FloatingPointValue != 0.0 ? "transfer.invalid-value" : string.Empty;
            case UiValueKind.String:
                if (value.StringValue == null || value.DateTimeValue != null || value.BinaryValue != null || value.ArrayValue != null || value.ObjectValue != null || hasPrimitive)
                {
                    return "transfer.invalid-value";
                }

                return value.StringValue.Length > Policy.MaximumTextLength ? "transfer.value-too-large" : string.Empty;
            case UiValueKind.DateTime:
                if (value.StringValue != null || value.DateTimeValue == null || value.BinaryValue != null || value.ArrayValue != null || value.ObjectValue != null || hasPrimitive)
                {
                    return "transfer.invalid-value";
                }

                return value.DateTimeValue.Length > Policy.MaximumTextLength ? "transfer.value-too-large" : string.Empty;
            case UiValueKind.Binary:
                if (value.StringValue != null || value.DateTimeValue != null || value.BinaryValue == null || value.ArrayValue != null || value.ObjectValue != null || hasPrimitive)
                {
                    return "transfer.invalid-value";
                }

                return value.BinaryValue.Length > Policy.MaximumBinaryLength ? "transfer.value-too-large" : string.Empty;
            case UiValueKind.Array:
                return value.StringValue != null || value.DateTimeValue != null || value.BinaryValue != null || value.ArrayValue == null || value.ObjectValue != null || hasPrimitive ? "transfer.invalid-value" : string.Empty;
            case UiValueKind.Object:
                return value.StringValue != null || value.DateTimeValue != null || value.BinaryValue != null || value.ArrayValue != null || value.ObjectValue == null || hasPrimitive ? "transfer.invalid-value" : string.Empty;
            default:
                return "transfer.invalid-value";
        }
    }

    internal bool AddTransferCollectionCount(int count, TransferValidationState state)
    {
        if (count > Policy.MaximumCollectionCount - state.CollectionCount)
        {
            return false;
        }

        state.CollectionCount += count;
        return true;
    }

    internal string[]? NormalizeAllowedMimeTypes(string[]? mimeTypes)
    {
        if (mimeTypes == null)
        {
            return null;
        }

        List<string> result = new List<string>();
        int index = 0;
        while (index < mimeTypes.Length)
        {
            string normalized = NormalizeMimeType(mimeTypes[index]);
            if (normalized.Length == 0 || ContainsTransferText(result, normalized))
            {
                return null;
            }

            InsertTransferText(result, normalized);
            index++;
        }

        return result.ToArray();
    }

    internal string NormalizeMimeType(string? mimeType)
    {
        if (mimeType == null)
        {
            return string.Empty;
        }

        string result = mimeType.Trim().ToLowerInvariant();
        if (result == "*/*")
        {
            return result;
        }

        int slashIndex = result.IndexOf('/');
        if (slashIndex <= 0 || slashIndex != result.LastIndexOf('/') || slashIndex == result.Length - 1)
        {
            return string.Empty;
        }

        int index = 0;
        while (index < result.Length)
        {
            char character = result[index];
            if (character == '/')
            {
                index++;
                continue;
            }

            if (!IsMimeTokenCharacter(character))
            {
                if (character != '*' || index != slashIndex + 1 || index != result.Length - 1)
                {
                    return string.Empty;
                }
            }

            index++;
        }

        return result;
    }

    internal bool IsMimeTokenCharacter(char character)
    {
        if (character >= 'a' && character <= 'z' || character >= '0' && character <= '9')
        {
            return true;
        }

        return character == '!' || character == '#' || character == '$' || character == '&' || character == '^' || character == '_' || character == '.' || character == '+' || character == '-';
    }

    internal bool IsTransferMimeTypeAllowed(string mimeType)
    {
        int index = 0;
        while (index < Policy.AllowedMimeTypes.Length)
        {
            string allowed = Policy.AllowedMimeTypes[index];
            if (allowed == "*/*" || ValueComparer.TextEquals(allowed, mimeType))
            {
                return true;
            }

            if (allowed.EndsWith("/*", StringComparison.Ordinal))
            {
                string prefix = allowed.Substring(0, allowed.Length - 1);
                if (mimeType.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            index++;
        }

        return false;
    }

    internal bool ContainsTransferMimeType(List<UiValueEntry> entries, string mimeType)
    {
        int index = 0;
        while (index < entries.Count)
        {
            if (ValueComparer.TextEquals(entries[index].Key, mimeType))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool ContainsTransferText(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (ValueComparer.TextEquals(values[index], value))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal void InsertTransferEntry(List<UiValueEntry> entries, UiValueEntry entry)
    {
        int index = 0;
        while (index < entries.Count && string.CompareOrdinal(entries[index].Key, entry.Key) < 0)
        {
            index++;
        }

        entries.Insert(index, entry);
    }

    internal void InsertTransferText(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count && string.CompareOrdinal(values[index], value) < 0)
        {
            index++;
        }

        values.Insert(index, value);
    }

    internal string GetClipboardTransferOperation(UiEventKind kind)
    {
        switch (kind)
        {
            case UiEventKind.Copy:
                return "copy";
            case UiEventKind.Cut:
                return "cut";
            case UiEventKind.Paste:
                return "paste";
            default:
                return string.Empty;
        }
    }

    internal bool IsClipboardOperationAllowed(string operation)
    {
        switch (operation)
        {
            case "copy":
                return Policy.AllowCopy;
            case "cut":
                return Policy.AllowCut;
            case "paste":
                return Policy.AllowPaste;
            default:
                return false;
        }
    }

    internal bool IsDragTransferKind(UiEventKind kind)
    {
        switch (kind)
        {
            case UiEventKind.DragStart:
            case UiEventKind.Drag:
            case UiEventKind.DragEnter:
            case UiEventKind.DragOver:
            case UiEventKind.DragLeave:
            case UiEventKind.Drop:
            case UiEventKind.DragEnd:
                return true;
            default:
                return false;
        }
    }

    internal string NormalizeDragEffect(string? effect)
    {
        switch ((effect ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "none":
                return "none";
            case "copy":
                return "copy";
            case "link":
                return "link";
            case "move":
                return "move";
            default:
                return string.Empty;
        }
    }

    internal string NormalizeAllowedDragEffect(string? effect)
    {
        switch ((effect ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "none":
                return "none";
            case "copy":
                return "copy";
            case "copylink":
                return "copyLink";
            case "copymove":
                return "copyMove";
            case "link":
                return "link";
            case "linkmove":
                return "linkMove";
            case "move":
                return "move";
            case "all":
            case "uninitialized":
                return "all";
            default:
                return string.Empty;
        }
    }

    internal bool IsDragEffectAllowed(string allowedEffect, string effect)
    {
        if (effect == "none")
        {
            return true;
        }

        if (allowedEffect == "all" || ValueComparer.TextEquals(allowedEffect, effect))
        {
            return true;
        }

        if (allowedEffect == "copyLink")
        {
            return effect == "copy" || effect == "link";
        }

        if (allowedEffect == "copyMove")
        {
            return effect == "copy" || effect == "move";
        }

        if (allowedEffect == "linkMove")
        {
            return effect == "link" || effect == "move";
        }

        return false;
    }

    internal TransferOperationResult CreateDragTransferResult(bool success, bool allowed, bool canceled, bool dropCompleted, string code)
    {
        UiDragData drag = new UiDragData();
        drag.SourceId = DragState.SourceId.Length == 0 ? null : DragState.SourceId;
        drag.TargetId = DragState.TargetId.Length == 0 ? null : DragState.TargetId;
        drag.Effect = DragState.Effect;
        drag.AllowedEffect = DragState.AllowedEffect;
        drag.Data = CopyTransferPayload(DragState.Data);
        drag.IsKeyboard = DragState.IsKeyboard;
        TransferOperationResult result = new TransferOperationResult();
        result.Success = success;
        result.Allowed = allowed;
        result.Canceled = canceled;
        result.DropCompleted = dropCompleted;
        result.Code = code;
        result.Drag = drag;
        return result;
    }

    internal UiValueEntry[] CopyTransferPayload(UiValueEntry[] source)
    {
        UiValueEntry[] result = new UiValueEntry[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            UiValueEntry copy = new UiValueEntry();
            copy.Key = source[index].Key;
            copy.Value = ValueJsonCodec.CloneValue(source[index].Value, Policy.MaximumValueDepth, Policy.MaximumCollectionCount, Policy.MaximumTextLength, Policy.MaximumBinaryLength);
            result[index] = copy;
            index++;
        }

        return result;
    }

    internal void ResetDragTransferState()
    {
        DragState = new DragTransferState();
    }

    internal TransferOperationResult CreateTransferFailure(string code)
    {
        return new TransferOperationResult { Success = false, Code = code };
    }
}
