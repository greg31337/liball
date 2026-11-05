using System;
using System.Text.Json;

namespace jmap;

internal enum MailboxRightKind
{
    ReadItems = 0,
    AddItems = 1,
    RemoveItems = 2,
    SetSeen = 3,
    SetKeywords = 4,
    CreateChild = 5,
    Rename = 6,
    Delete = 7,
    Submit = 8
}

internal class MethodContext
{
    internal ValueValidator Values = new ValueValidator();

    internal JmapMethodError? ValidateBeforeDispatch(
        JmapInvocation invocation,
        RegisteredJmapMethod registered,
        JmapSession session)
    {
        if (registered.VendorHandler != null)
        {
            return null;
        }

        string? accountCapability = AccountCapability(registered);
        bool blobCopy = invocation.Name == JmapMethods.BlobCopy;
        if (accountCapability == null && !blobCopy)
        {
            return null;
        }

        JsonElement accountId = ReadUniqueMember(
            invocation.Arguments, JmapProperties.AccountId);
        if (accountId.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        string? id = accountId.GetString();
        if (!Values.IsId(id))
        {
            return null;
        }

        if (invocation.Name == JmapMethods.BlobCopy ||
            invocation.Name == JmapMethods.EmailCopy)
        {
            JsonElement fromAccountId = ReadUniqueMember(
                invocation.Arguments, JmapProperties.FromAccountId);
            if (fromAccountId.ValueKind == JsonValueKind.String)
            {
                if (!Values.IsId(fromAccountId.GetString()))
                {
                    return null;
                }

                JmapMethodError? sourceError = ValidateAccount(
                    session, fromAccountId.GetString(), accountCapability, true);
                if (sourceError != null)
                {
                    return sourceError;
                }
            }
        }

        JmapMethodError? targetError = ValidateAccount(
            session, id, accountCapability, false);
        if (targetError != null)
        {
            return targetError;
        }

        if (IsMutation(invocation.Name) && id != null && session.Accounts[id].IsReadOnly)
        {
            return MethodError(
                JmapErrorCodes.AccountReadOnly,
                JmapMethodErrorKind.AccountReadOnly,
                "The account is read-only.");
        }

        return null;
    }

    internal void ValidateAfterDispatch(
        JmapInvocation invocation,
        RegisteredJmapMethod registered,
        MethodDispatchResult result)
    {
        if (registered.VendorHandler != null)
        {
            return;
        }

        JmapInvocation first = result.Responses[0];
        if (first.Name == ResponseMapper.ErrorResponseName)
        {
            if (result.Responses.Count != 1)
            {
                throw ProtocolFailure(
                    "A failed method returned additional responses.",
                    "methodResponses");
            }

            return;
        }

        if (first.Name != invocation.Name)
        {
            throw ProtocolFailure(
                "The primary response name does not match the dispatched method.",
                "methodResponses/0/0");
        }

        ValidateImplicitResponses(invocation, result);
        string? accountCapability = AccountCapability(registered);
        bool blobCopy = invocation.Name == JmapMethods.BlobCopy;
        if (accountCapability != null || blobCopy)
        {
            ValidateResponseAccount(invocation.Arguments, first.Arguments);
            ValidateResponseSourceAccount(invocation, first);
            if (UsesIfInState(invocation.Name))
            {
                ValidateStatePrecondition(
                    invocation.Arguments, first.Arguments,
                    JmapProperties.IfInState, JmapProperties.OldState);
            }

            if (UsesSinceState(invocation.Name))
            {
                ValidateStatePrecondition(
                    invocation.Arguments, first.Arguments,
                    JmapProperties.SinceState, JmapProperties.OldState);
            }

            if (UsesSinceQueryState(invocation.Name))
            {
                ValidateStatePrecondition(
                    invocation.Arguments, first.Arguments,
                    JmapProperties.SinceQueryState, JmapProperties.OldQueryState);
            }
        }
    }

    internal JmapMethodError? ValidateAccount(
        JmapSession session,
        string? accountId,
        string? capability,
        bool source)
    {
        string id = accountId == null ? "" : accountId;
        if (!Values.IsId(accountId) || !session.Accounts.ContainsKey(id))
        {
            return source ? MethodError(
                JmapErrorCodes.FromAccountNotFound,
                JmapMethodErrorKind.FromAccountNotFound,
                "The source account was not found or is not accessible.") :
                MethodError(
                    JmapErrorCodes.AccountNotFound,
                    JmapMethodErrorKind.AccountNotFound,
                    "The account was not found or is not accessible.");
        }

        JmapAccount account = session.Accounts[id];
        if (account == null || account.AccountCapabilities == null)
        {
            throw ProtocolFailure(
                "The Session contains invalid account data.",
                "session/accounts/" + id);
        }

        if (capability != null && !account.AccountCapabilities.ContainsKey(capability))
        {
            return source ? MethodError(
                JmapErrorCodes.FromAccountNotSupportedByMethod,
                JmapMethodErrorKind.FromAccountNotSupportedByMethod,
                "The source account does not support the method.") :
                MethodError(
                    JmapErrorCodes.AccountNotSupportedByMethod,
                    JmapMethodErrorKind.AccountNotSupportedByMethod,
                    "The account does not support the method.");
        }

        if (capability != null)
        {
            JmapCapability accountData = account.AccountCapabilities[capability];
            if (!HasTypedCapability(accountData, capability))
            {
                throw ProtocolFailure(
                    "The account contains invalid capability data.",
                    "session/accounts/" + id + "/accountCapabilities/" + capability);
            }
        }

        return null;
    }

    internal bool HasMailboxRight(Mailbox? mailbox, MailboxRightKind right)
    {
        if (mailbox == null || mailbox.MyRights == null)
        {
            return false;
        }

        MailboxRights rights = mailbox.MyRights;
        switch (right)
        {
            case MailboxRightKind.ReadItems:
                return rights.MayReadItems;
            case MailboxRightKind.AddItems:
                return rights.MayAddItems;
            case MailboxRightKind.RemoveItems:
                return rights.MayRemoveItems;
            case MailboxRightKind.SetSeen:
                return rights.MaySetSeen;
            case MailboxRightKind.SetKeywords:
                return rights.MaySetKeywords;
            case MailboxRightKind.CreateChild:
                return rights.MayCreateChild;
            case MailboxRightKind.Rename:
                return rights.MayRename;
            case MailboxRightKind.Delete:
                return rights.MayDelete;
            case MailboxRightKind.Submit:
                return rights.MaySubmit;
            default:
                return false;
        }
    }

    internal string? AccountCapability(RegisteredJmapMethod registered)
    {
        foreach (string capability in registered.RequiredCapabilities)
        {
            if (capability != JmapCapabilities.Core)
            {
                return capability;
            }
        }

        return null;
    }

    internal bool HasTypedCapability(JmapCapability? capability, string name)
    {
        if (capability == null)
        {
            return false;
        }

        if (name == JmapCapabilities.Mail)
        {
            return capability.Mail != null;
        }

        if (name == JmapCapabilities.Submission)
        {
            return capability.Submission != null;
        }

        if (name == JmapCapabilities.VacationResponse)
        {
            return capability.VacationResponse != null;
        }

        return true;
    }

    internal bool IsMutation(string? methodName)
    {
        return methodName == JmapMethods.BlobCopy ||
            methodName == JmapMethods.MailboxSet ||
            methodName == JmapMethods.EmailSet ||
            methodName == JmapMethods.EmailCopy ||
            methodName == JmapMethods.EmailImport ||
            methodName == JmapMethods.IdentitySet ||
            methodName == JmapMethods.EmailSubmissionSet ||
            methodName == JmapMethods.VacationResponseSet;
    }

    internal bool UsesIfInState(string? methodName)
    {
        return methodName == JmapMethods.MailboxSet ||
            methodName == JmapMethods.EmailSet ||
            methodName == JmapMethods.EmailCopy ||
            methodName == JmapMethods.EmailImport ||
            methodName == JmapMethods.IdentitySet ||
            methodName == JmapMethods.EmailSubmissionSet ||
            methodName == JmapMethods.VacationResponseSet;
    }

    internal bool UsesSinceState(string? methodName)
    {
        return methodName == JmapMethods.MailboxChanges ||
            methodName == JmapMethods.ThreadChanges ||
            methodName == JmapMethods.EmailChanges ||
            methodName == JmapMethods.IdentityChanges ||
            methodName == JmapMethods.EmailSubmissionChanges;
    }

    internal bool UsesSinceQueryState(string? methodName)
    {
        return methodName == JmapMethods.MailboxQueryChanges ||
            methodName == JmapMethods.EmailQueryChanges ||
            methodName == JmapMethods.EmailSubmissionQueryChanges;
    }

    internal void ValidateImplicitResponses(
        JmapInvocation invocation,
        MethodDispatchResult result)
    {
        if (result.Responses.Count == 1)
        {
            return;
        }

        if (invocation.Name != JmapMethods.EmailSubmissionSet ||
            result.Responses.Count != 2 ||
            result.Responses[1].Name != JmapMethods.EmailSet)
        {
            throw ProtocolFailure(
                "The method returned an unexpected implicit response.",
                "methodResponses");
        }
    }

    internal void ValidateResponseAccount(
        JsonElement requestArguments,
        JsonElement responseArguments)
    {
        JsonElement requested = ReadUniqueMember(
            requestArguments, JmapProperties.AccountId);
        if (requested.ValueKind != JsonValueKind.String)
        {
            return;
        }

        JsonElement returned = ReadUniqueMember(
            responseArguments, JmapProperties.AccountId);
        if (returned.ValueKind != JsonValueKind.String ||
            returned.GetString() != requested.GetString())
        {
            throw ProtocolFailure(
                "The backend response account does not match the request.",
                "methodResponses/1/accountId");
        }
    }

    internal void ValidateResponseSourceAccount(
        JmapInvocation invocation,
        JmapInvocation response)
    {
        if (invocation.Name != JmapMethods.BlobCopy &&
            invocation.Name != JmapMethods.EmailCopy)
        {
            return;
        }

        JsonElement requested = ReadUniqueMember(
            invocation.Arguments, JmapProperties.FromAccountId);
        if (requested.ValueKind != JsonValueKind.String)
        {
            return;
        }

        JsonElement returned = ReadUniqueMember(
            response.Arguments, JmapProperties.FromAccountId);
        if (returned.ValueKind != JsonValueKind.String ||
            returned.GetString() != requested.GetString())
        {
            throw ProtocolFailure(
                "The backend response source account does not match the request.",
                "methodResponses/1/fromAccountId");
        }
    }

    internal void ValidateStatePrecondition(
        JsonElement requestArguments,
        JsonElement responseArguments,
        string requestName,
        string responseName)
    {
        JsonElement requested = ReadUniqueMember(requestArguments, requestName);
        if (requested.ValueKind != JsonValueKind.String)
        {
            return;
        }

        JsonElement returned = ReadUniqueMember(responseArguments, responseName);
        if (returned.ValueKind != JsonValueKind.String ||
            returned.GetString() != requested.GetString())
        {
            throw ProtocolFailure(
                "The backend response does not preserve the request state precondition.",
                "methodResponses/1/" + responseName);
        }
    }

    internal JsonElement ReadUniqueMember(JsonElement value, string name)
    {
        JsonElement result = default(JsonElement);
        bool found = false;
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Name == name)
            {
                if (found)
                {
                    return default(JsonElement);
                }

                result = property.Value;
                found = true;
            }
        }

        return result;
    }

    internal JmapMethodError MethodError(
        string type,
        JmapMethodErrorKind kind,
        string description)
    {
        JmapMethodError error = new JmapMethodError();
        error.Type = type;
        error.Kind = kind;
        error.Description = description;
        return error;
    }

    internal JmapProtocolException ProtocolFailure(string message, string path)
    {
        return new JmapProtocolException(message, path, default(JsonElement));
    }
}
