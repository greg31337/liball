using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class AccountSelector
{
    internal ValueValidator Values = new ValueValidator();

    internal string SelectAccountId(
        JmapSession? session,
        JmapClientOptions? options,
        string? explicitAccountId,
        string? capability,
        string? methodName,
        bool requireWritable)
    {
        ValidateSelectionInputs(session, options, methodName);
        if (session == null || options == null)
        {
            throw ValidationFailure("A Session and client options are required.", "session");
        }

        string selectedCapability = EffectiveCapability(options, capability);
        ValidateSessionCollections(session);
        if (!session.Capabilities.ContainsKey(selectedCapability))
        {
            throw CapabilityFailure(
                "The server does not advertise the required capability.",
                selectedCapability,
                methodName,
                null);
        }

        ValidateServerCapabilityObject(
            session.Capabilities[selectedCapability],
            selectedCapability,
            "session/capabilities/" + selectedCapability);

        string? selected = explicitAccountId;
        string selectedPath = "accountId";
        if (selected == null)
        {
            selected = options.DefaultAccountId;
            selectedPath = "defaultAccountId";
        }

        if (selected == null)
        {
            selected = FindPrimaryAccountId(session, selectedCapability);
            selectedPath = "primaryAccounts/" + selectedCapability;
        }

        if (selected == null)
        {
            throw CapabilityFailure(
                "No account is available for the required capability.",
                selectedCapability,
                methodName,
                null);
        }

        if (!Values.IsId(selected))
        {
            throw ValidationFailure("The selected account identifier is invalid.", selectedPath);
        }

        if (!session.Accounts.ContainsKey(selected))
        {
            throw CapabilityFailure(
                "The selected account is not available in the current Session.",
                selectedCapability,
                methodName,
                selected);
        }

        JmapAccount account = session.Accounts[selected];
        ValidateAccountObject(account, selected);
        if (!account.AccountCapabilities.ContainsKey(selectedCapability))
        {
            throw CapabilityFailure(
                "The selected account does not support the required capability.",
                selectedCapability,
                methodName,
                selected);
        }

        ValidateCapabilityObject(
            account.AccountCapabilities[selectedCapability],
            selectedCapability,
            "session/accounts/" + selected + "/accountCapabilities/" + selectedCapability);
        if (requireWritable && account.IsReadOnly)
        {
            throw CapabilityFailure(
                "The selected account is read-only.",
                selectedCapability,
                methodName,
                selected);
        }

        return selected;
    }

    internal string? FindPrimaryAccountId(JmapSession? session, string? capability)
    {
        if (session == null)
        {
            throw ValidationFailure("A Session is required.", "session");
        }

        if (!Values.IsCapability(capability))
        {
            throw ValidationFailure("A valid account capability URI is required.", "capability");
        }

        ValidateSessionCollections(session);
        string selectedCapability = capability == null ? "" : capability;
        if (!session.PrimaryAccounts.ContainsKey(selectedCapability))
        {
            return null;
        }

        if (!session.Capabilities.ContainsKey(selectedCapability))
        {
            throw ProtocolFailure(
                "The primary account maps a capability the server does not advertise.",
                "primaryAccounts/" + selectedCapability);
        }

        ValidateServerCapabilityObject(
            session.Capabilities[selectedCapability],
            selectedCapability,
            "session/capabilities/" + selectedCapability);
        string accountId = session.PrimaryAccounts[selectedCapability];
        if (!Values.IsId(accountId) || !session.Accounts.ContainsKey(accountId))
        {
            throw ProtocolFailure(
                "The Session primary account mapping is invalid.",
                "primaryAccounts/" + selectedCapability);
        }

        JmapAccount account = session.Accounts[accountId];
        ValidateAccountObject(account, accountId);
        if (!account.AccountCapabilities.ContainsKey(selectedCapability))
        {
            throw ProtocolFailure(
                "The primary account does not advertise its mapped capability.",
                "accounts/" + accountId + "/accountCapabilities/" + selectedCapability);
        }

        ValidateCapabilityObject(
            account.AccountCapabilities[selectedCapability],
            selectedCapability,
            "session/accounts/" + accountId + "/accountCapabilities/" + selectedCapability);
        return accountId;
    }

    internal string EffectiveCapability(JmapClientOptions options, string? capability)
    {
        string? selected = capability == null ? options.DefaultAccountCapability : capability;
        if (!Values.IsCapability(selected))
        {
            throw ValidationFailure("A valid account capability URI is required.", "capability");
        }

        return selected == null ? "" : selected;
    }

    internal void ValidateSelectionInputs(
        JmapSession? session,
        JmapClientOptions? options,
        string? methodName)
    {
        if (session == null)
        {
            throw ValidationFailure("A Session is required for account selection.", "session");
        }

        if (options == null)
        {
            throw ValidationFailure("JMAP client options are required for account selection.", "options");
        }

        if (methodName != null && !Values.IsMethodName(methodName))
        {
            throw ValidationFailure("The method name is invalid.", "methodName");
        }

    }

    internal void ValidateSessionCollections(JmapSession session)
    {
        if (session.Capabilities == null || session.Accounts == null || session.PrimaryAccounts == null)
        {
            throw ProtocolFailure("The Session account-selection collections are invalid.", "session");
        }
    }

    internal void ValidateAccountObject(JmapAccount? account, string accountId)
    {
        if (account == null || account.AccountCapabilities == null)
        {
            throw ProtocolFailure(
                "The Session contains invalid account data.",
                "accounts/" + accountId);
        }
    }

    internal void ValidateCapabilityObject(
        JmapCapability? value,
        string capability,
        string path)
    {
        if (value == null || value.UnknownProperties == null)
        {
            throw ProtocolFailure("The Session contains invalid capability data.", path);
        }

        if (capability == JmapCapabilities.Core && value.Core == null ||
            capability == JmapCapabilities.Mail && value.Mail == null ||
            capability == JmapCapabilities.Submission && value.Submission == null ||
            capability == JmapCapabilities.VacationResponse && value.VacationResponse == null)
        {
            throw ProtocolFailure("The Session capability data does not match its URI.", path);
        }
    }

    internal void ValidateServerCapabilityObject(
        JmapCapability? value,
        string capability,
        string path)
    {
        if (value == null || value.UnknownProperties == null ||
            capability == JmapCapabilities.Core && value.Core == null)
        {
            throw ProtocolFailure("The Session contains invalid server capability data.", path);
        }
    }

    internal JmapValidationException ValidationFailure(string message, string path)
    {
        return new JmapValidationException(
            message,
            new List<string> { path },
            default(JsonElement));
    }

    internal JmapCapabilityException CapabilityFailure(
        string message,
        string capability,
        string? methodName,
        string? accountId)
    {
        return new JmapCapabilityException(message, capability, methodName, accountId);
    }

    internal JmapProtocolException ProtocolFailure(string message, string path)
    {
        return new JmapProtocolException(message, path, default(JsonElement));
    }
}
