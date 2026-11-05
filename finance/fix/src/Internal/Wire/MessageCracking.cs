using System.Collections.Generic;

namespace fix;

internal static class MessageCracking
{
    internal static MessageCrackResult CrackMessage(FixMessage message, FixDictionary transportDictionary, FixDictionary applicationDictionary, string scenario, IMessageCrackHandler handler)
    {
        if (message == null) return CreateCrackFailure("missing-crack-message", "The message to dispatch is missing.");
        if (handler == null) return CreateCrackFailure("missing-crack-handler", "The message crack handler is missing.");
        string msgType = ReadCrackMessageType(message);
        bool administrative = IsAdministrativeMessageType(msgType);
        FixDictionary selectedDictionary = administrative ? transportDictionary : applicationDictionary;
        string selectedScenario = administrative ? "" : scenario;
        ApplicationMessageResult identified = ApplicationMessageOperations.IdentifyApplicationMessage(message, selectedDictionary, selectedScenario);
        if (!identified.Success) return CreateCrackFailure(identified.ErrorCode, identified.ErrorMessage);
        if (identified.Definition == null) return CreateCrackFailure("missing-crack-definition", "The identified message has no dictionary definition.");

        MessageCrackContext context = new MessageCrackContext();
        context.Message = message;
        context.Definition = identified.Definition;
        context.Identity = identified.Identity;
        context.Name = identified.Definition.Name;
        context.Category = identified.Definition.Category;
        context.IsAdministrative = administrative;
        ApplicationDecision decision = administrative ? handler.HandleAdministrativeMessage(context) : handler.HandleApplicationMessage(context);
        if (decision == null) return CreateCrackFailure("missing-crack-decision", "The selected message handler did not return a decision.");

        MessageCrackResult result = new MessageCrackResult();
        result.Success = true;
        result.Context = context;
        result.Decision = decision;
        return result;
    }

    internal static MessageLookupResult LookupMessageName(FixDictionary dictionary, string name, string scenario)
    {
        if (dictionary == null) return CreateLookupFailure("missing-lookup-dictionary", "The dictionary to query is missing.");
        if (name == null || name.Trim().Length == 0) return CreateLookupFailure("missing-message-name", "The message name to query is missing.");
        if (scenario == null) return CreateLookupFailure("missing-lookup-scenario", "The message scenario selection is missing.");

        string selectedScenario = SelectNameLookupScenario(dictionary, name, scenario);
        MessageLookupResult result = CreateLookupResult(dictionary, selectedScenario);
        List<string> candidates = ApplicationMessageOperations.BuildApplicationScenarioCandidates(dictionary, selectedScenario);
        for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
        {
            int matches = 0;
            FixMessageDefinition? found = null;
            for (int index = 0; index < dictionary.Messages.Count; index++)
            {
                FixMessageDefinition definition = dictionary.Messages[index];
                if (!definition.IsRemoved && ApplicationMessageOperations.ApplicationNamesEqual(definition.Name, name, dictionary.Options.NormalizeNames) && ApplicationMessageOperations.ApplicationNamesEqual(definition.Scenario, candidates[candidateIndex], dictionary.Options.NormalizeNames))
                {
                    if (found == null) found = definition;
                    matches++;
                }
            }
            if (matches > 1) return CreateLookupFailure("ambiguous-message-name", "More than one message definition matches the selected name and scenario.");
            if (found != null)
            {
                result.Definitions.Add(found);
                result.Success = true;
                return result;
            }
        }
        return CreateLookupFailure("unknown-message-name", "No message definition has the requested name and scenario.");
    }

    internal static MessageLookupResult LookupMessageCategory(FixDictionary dictionary, string category, string scenario)
    {
        if (dictionary == null) return CreateLookupFailure("missing-lookup-dictionary", "The dictionary to query is missing.");
        if (category == null || category.Trim().Length == 0) return CreateLookupFailure("missing-message-category", "The message category to query is missing.");
        if (scenario == null) return CreateLookupFailure("missing-lookup-scenario", "The message scenario selection is missing.");

        string selectedScenario = SelectCategoryLookupScenario(dictionary, category, scenario);
        MessageLookupResult result = CreateLookupResult(dictionary, selectedScenario);
        List<string> selectedTypes = [];
        List<string> candidates = ApplicationMessageOperations.BuildApplicationScenarioCandidates(dictionary, selectedScenario);
        for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
        {
            for (int index = 0; index < dictionary.Messages.Count; index++)
            {
                FixMessageDefinition definition = dictionary.Messages[index];
                bool categoryMatches = ApplicationMessageOperations.ApplicationNamesEqual(definition.Category, category, dictionary.Options.NormalizeNames);
                bool scenarioMatches = ApplicationMessageOperations.ApplicationNamesEqual(definition.Scenario, candidates[candidateIndex], dictionary.Options.NormalizeNames);
                if (!definition.IsRemoved && categoryMatches && scenarioMatches && !ContainsCrackText(selectedTypes, definition.MsgType))
                {
                    if (CountCategoryTypeMatches(dictionary, category, candidates[candidateIndex], definition.MsgType) > 1) return CreateLookupFailure("ambiguous-message-category", "A category contains duplicate MsgType definitions for the selected scenario.");
                    result.Definitions.Add(definition);
                    selectedTypes.Add(definition.MsgType);
                }
            }
        }
        if (result.Definitions.Count == 0) return CreateLookupFailure("unknown-message-category", "No message definitions have the requested category and scenario.");
        result.Success = true;
        return result;
    }

    internal static string SelectNameLookupScenario(FixDictionary dictionary, string name, string scenario)
    {
        string selected = scenario.Trim();
        if (selected.Length == 0) selected = dictionary.Identity.Scenario.Trim();
        if (selected.Length != 0) return selected;
        for (int index = 0; index < dictionary.Messages.Count; index++)
        {
            FixMessageDefinition definition = dictionary.Messages[index];
            if (!definition.IsRemoved && ApplicationMessageOperations.ApplicationNamesEqual(definition.Name, name, dictionary.Options.NormalizeNames) && ApplicationMessageOperations.ApplicationNamesEqual(definition.Scenario, "base", dictionary.Options.NormalizeNames)) return "base";
        }
        return "";
    }

    internal static string SelectCategoryLookupScenario(FixDictionary dictionary, string category, string scenario)
    {
        string selected = scenario.Trim();
        if (selected.Length == 0) selected = dictionary.Identity.Scenario.Trim();
        if (selected.Length != 0) return selected;
        for (int index = 0; index < dictionary.Messages.Count; index++)
        {
            FixMessageDefinition definition = dictionary.Messages[index];
            if (!definition.IsRemoved && ApplicationMessageOperations.ApplicationNamesEqual(definition.Category, category, dictionary.Options.NormalizeNames) && ApplicationMessageOperations.ApplicationNamesEqual(definition.Scenario, "base", dictionary.Options.NormalizeNames)) return "base";
        }
        return "";
    }

    internal static int CountCategoryTypeMatches(FixDictionary dictionary, string category, string scenario, string msgType)
    {
        int result = 0;
        for (int index = 0; index < dictionary.Messages.Count; index++)
        {
            FixMessageDefinition definition = dictionary.Messages[index];
            bool categoryMatches = ApplicationMessageOperations.ApplicationNamesEqual(definition.Category, category, dictionary.Options.NormalizeNames);
            bool scenarioMatches = ApplicationMessageOperations.ApplicationNamesEqual(definition.Scenario, scenario, dictionary.Options.NormalizeNames);
            if (!definition.IsRemoved && definition.MsgType == msgType && categoryMatches && scenarioMatches) result++;
        }
        return result;
    }

    internal static string ReadCrackMessageType(FixMessage message)
    {
        if (message.MsgType != null && message.MsgType.Length != 0) return message.MsgType;
        DictionaryTagValue field = ApplicationMessageOperations.ReadApplicationTag(message, FixTags.MsgType);
        return ApplicationMessageOperations.ReadApplicationText(field);
    }

    internal static bool IsAdministrativeMessageType(string msgType)
    {
        return msgType == FixAdminMessageTypes.Heartbeat || msgType == FixAdminMessageTypes.TestRequest || msgType == FixAdminMessageTypes.ResendRequest || msgType == FixAdminMessageTypes.Reject || msgType == FixAdminMessageTypes.SequenceReset || msgType == FixAdminMessageTypes.Logout || msgType == FixAdminMessageTypes.Logon || msgType == FixAdminMessageTypes.BusinessMessageReject;
    }

    internal static bool ContainsCrackText(List<string> source, string value)
    {
        for (int index = 0; index < source.Count; index++) if (source[index] == value) return true;
        return false;
    }

    internal static MessageLookupResult CreateLookupResult(FixDictionary dictionary, string scenario)
    {
        MessageLookupResult result = new MessageLookupResult();
        result.Identity = DictionaryComposition.CloneIdentity(dictionary.Identity);
        result.Identity.Scenario = scenario;
        return result;
    }

    internal static MessageLookupResult CreateLookupFailure(string code, string description)
    {
        MessageLookupResult result = new MessageLookupResult();
        result.ErrorCode = code;
        result.ErrorMessage = description;
        return result;
    }

    internal static MessageCrackResult CreateCrackFailure(string code, string description)
    {
        MessageCrackResult result = new MessageCrackResult();
        result.ErrorCode = code;
        result.ErrorMessage = description;
        return result;
    }
}
