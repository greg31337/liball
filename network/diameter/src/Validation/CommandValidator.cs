using System.Collections.Generic;

namespace diameter;

internal class CommandValidator
{
    internal DictionaryIndexes Indexes;
    internal List<DiameterCommandDefinition> Commands =
        new List<DiameterCommandDefinition>();

    internal CommandValidator(DiameterDictionarySnapshot? snapshot)
    {
        Indexes = new DictionaryIndexes(snapshot);
        if (snapshot != null)
        {
            Commands = snapshot.Commands;
        }
    }

    internal CommandValidationResult ValidateCommand(DiameterMessage? message)
    {
        CommandValidationResult result = new CommandValidationResult();
        if (message == null)
        {
            AddFailure(result, "message-required", 0U, 0U, -1);
            CompleteResult(result);
            return result;
        }

        bool request = (message.Flags & DiameterMessageFlags.Request) != 0;
        DiameterCommandDefinition? definition = Indexes.FindCommandByCode(
            message.ApplicationId,
            message.CommandCode,
            request);
        if (definition == null)
        {
            definition = FindCommandByIdentity(message.ApplicationId, message.CommandCode);
        }

        if (definition == null)
        {
            if (FindCommandByCode(message.CommandCode) != null)
            {
                AddFailure(result, "application-id-mismatch", 0U, 0U, -1);
            }
            else
            {
                AddFailure(result, "unknown-command", 0U, 0U, -1);
            }

            CompleteResult(result);
            return result;
        }

        result.Definition = definition;
        ValidateHeaderFlags(message, definition, result);
        ValidateOccurrenceRules(message, definition, result);
        ValidateFixedHeadRules(message, definition, result);
        ValidateOrderingRules(message, definition, result);
        ValidateUnlistedAvps(message, definition, result);
        CompleteResult(result);
        return result;
    }

    internal DiameterCommandDefinition? FindCommandByIdentity(
        uint applicationId,
        uint commandCode)
    {
        int index = 0;
        while (index < Commands.Count)
        {
            DiameterCommandDefinition command = Commands[index];
            if (command.ApplicationId == applicationId && command.Code == commandCode)
            {
                return command;
            }

            index = index + 1;
        }

        return null;
    }

    internal DiameterCommandDefinition? FindCommandByCode(uint commandCode)
    {
        int index = 0;
        while (index < Commands.Count)
        {
            DiameterCommandDefinition command = Commands[index];
            if (command.Code == commandCode)
            {
                return command;
            }

            index = index + 1;
        }

        return null;
    }

    internal void ValidateHeaderFlags(
        DiameterMessage message,
        DiameterCommandDefinition definition,
        CommandValidationResult result)
    {
        DiameterMessageFlags missing = definition.RequiredFlags & ~message.Flags;
        if (missing != DiameterMessageFlags.None)
        {
            CommandValidationFailure failure = AddFailure(
                result,
                "required-header-flags-missing",
                0U,
                0U,
                -1);
            failure.ExpectedFlags = definition.RequiredFlags;
            failure.ActualFlags = message.Flags;
        }

        DiameterMessageFlags prohibited = definition.ProhibitedFlags & message.Flags;
        if (!definition.IsRequest &&
            (message.Flags & DiameterMessageFlags.Error) != DiameterMessageFlags.None)
        {
            prohibited = prohibited & ~DiameterMessageFlags.Error;
        }

        if (prohibited != DiameterMessageFlags.None)
        {
            CommandValidationFailure failure = AddFailure(
                result,
                "prohibited-header-flags-present",
                0U,
                0U,
                -1);
            failure.ExpectedFlags = definition.ProhibitedFlags;
            failure.ActualFlags = message.Flags;
        }
    }

    internal void ValidateOccurrenceRules(
        DiameterMessage message,
        DiameterCommandDefinition definition,
        CommandValidationResult result)
    {
        int ruleIndex = 0;
        while (ruleIndex < definition.Rules.Count)
        {
            DiameterAvpRuleDefinition rule = definition.Rules[ruleIndex];
            int count = CountOccurrences(message.Avps, rule);
            if (!definition.IsRequest &&
                rule.AvpVendorId == DiameterVendorIds.Ietf &&
                rule.AvpCode == DiameterAvpCodes.ResultCode &&
                count == 0 &&
                CountOccurrencesByIdentity(
                    message.Avps,
                    DiameterVendorIds.Ietf,
                    DiameterAvpCodes.ExperimentalResult) > 0)
            {
                count = 1;
            }

            if (count < rule.MinimumCount)
            {
                CommandValidationFailure failure = AddFailure(
                    result,
                    "missing-required-avp",
                    rule.AvpCode,
                    rule.AvpVendorId,
                    -1);
                failure.ExpectedMinimum = rule.MinimumCount;
                failure.ExpectedMaximum = rule.MaximumCount;
                failure.ActualCount = count;
            }

            if (rule.MaximumCount >= 0 && count > rule.MaximumCount)
            {
                CommandValidationFailure failure = AddFailure(
                    result,
                    "too-many-avp-occurrences",
                    rule.AvpCode,
                    rule.AvpVendorId,
                    FindOccurrenceIndex(message.Avps, rule, rule.MaximumCount));
                failure.ExpectedMinimum = rule.MinimumCount;
                failure.ExpectedMaximum = rule.MaximumCount;
                failure.ActualCount = count;
            }

            ruleIndex = ruleIndex + 1;
        }
    }

    internal int CountOccurrencesByIdentity(
        List<DiameterAvp> avps,
        uint vendorId,
        uint code)
    {
        int count = 0;
        int index = 0;
        while (index < avps.Count)
        {
            DiameterAvp avp = avps[index];
            if (avp.VendorId == vendorId && avp.Code == code)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal void ValidateFixedHeadRules(
        DiameterMessage message,
        DiameterCommandDefinition definition,
        CommandValidationResult result)
    {
        int ruleIndex = 0;
        while (ruleIndex < definition.Rules.Count)
        {
            DiameterAvpRuleDefinition rule = definition.Rules[ruleIndex];
            if (rule.FixedHeadPosition >= 0)
            {
                int actualIndex = FindOccurrenceIndex(message.Avps, rule, 0);
                if (actualIndex >= 0 && actualIndex != rule.FixedHeadPosition)
                {
                    CommandValidationFailure failure = AddFailure(
                        result,
                        "fixed-head-avp-misordered",
                        rule.AvpCode,
                        rule.AvpVendorId,
                        actualIndex);
                    failure.ExpectedPosition = rule.FixedHeadPosition;
                }
            }

            ruleIndex = ruleIndex + 1;
        }
    }

    internal void ValidateOrderingRules(
        DiameterMessage message,
        DiameterCommandDefinition definition,
        CommandValidationResult result)
    {
        int greatestGroup = -1;
        int avpIndex = 0;
        while (avpIndex < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[avpIndex];
            DiameterAvpRuleDefinition? rule = FindRule(definition.Rules, avp);
            if (rule != null && rule.OrderingGroup >= 0)
            {
                if (rule.OrderingGroup < greatestGroup)
                {
                    AddFailure(
                        result,
                        "avp-ordering-violation",
                        avp.Code,
                        avp.VendorId,
                        avpIndex);
                }
                else
                {
                    greatestGroup = rule.OrderingGroup;
                }
            }

            avpIndex = avpIndex + 1;
        }
    }

    internal void ValidateUnlistedAvps(
        DiameterMessage message,
        DiameterCommandDefinition definition,
        CommandValidationResult result)
    {
        if (definition.AllowUnlistedAvps)
        {
            return;
        }

        int avpIndex = 0;
        while (avpIndex < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[avpIndex];
            if (FindRule(definition.Rules, avp) == null)
            {
                AddFailure(
                    result,
                    "avp-not-allowed",
                    avp.Code,
                    avp.VendorId,
                    avpIndex);
            }

            avpIndex = avpIndex + 1;
        }
    }

    internal int CountOccurrences(
        List<DiameterAvp> avps,
        DiameterAvpRuleDefinition rule)
    {
        int count = 0;
        int index = 0;
        while (index < avps.Count)
        {
            if (AvpMatchesRule(avps[index], rule))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int FindOccurrenceIndex(
        List<DiameterAvp> avps,
        DiameterAvpRuleDefinition rule,
        int occurrence)
    {
        int matched = 0;
        int index = 0;
        while (index < avps.Count)
        {
            if (AvpMatchesRule(avps[index], rule))
            {
                if (matched == occurrence)
                {
                    return index;
                }

                matched = matched + 1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal DiameterAvpRuleDefinition? FindRule(
        List<DiameterAvpRuleDefinition> rules,
        DiameterAvp avp)
    {
        int index = 0;
        while (index < rules.Count)
        {
            if (AvpMatchesRule(avp, rules[index]))
            {
                return rules[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool AvpMatchesRule(DiameterAvp avp, DiameterAvpRuleDefinition rule)
    {
        return avp.Code == rule.AvpCode && avp.VendorId == rule.AvpVendorId;
    }

    internal CommandValidationFailure AddFailure(
        CommandValidationResult result,
        string code,
        uint avpCode,
        uint avpVendorId,
        int avpIndex)
    {
        CommandValidationFailure failure = new CommandValidationFailure();
        failure.Code = code;
        failure.AvpCode = avpCode;
        failure.AvpVendorId = avpVendorId;
        failure.AvpIndex = avpIndex;
        result.Failures.Add(failure);
        return failure;
    }

    internal void CompleteResult(CommandValidationResult result)
    {
        result.Status = DiameterValidationStatus.Valid;
        if (result.Failures.Count > 0)
        {
            result.Status = DiameterValidationStatus.Invalid;
        }
    }
}
