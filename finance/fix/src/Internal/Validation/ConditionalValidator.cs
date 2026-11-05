using System.Collections.Generic;
using System.Text;

namespace fix;

internal static class ConditionalValidator
{
    internal static List<ValidationIssue> ValidateConditionalRules(FixMessage message, FixDictionary dictionary, ValidationOptions options, ValidationContext context)
    {
        List<ValidationIssue> issues = [];
        if (!ValidationCoordinator.IsLayerEnabled(options, ValidationLayer.ConditionalRule))
        {
            return issues;
        }

        FixMessageDefinition? messageDefinition = DictionaryStructureValidator.FindMessageForValidation(dictionary, message.MsgType, context.Scenario);
        List<FixScenarioDefinition> scenarioChain = ResolveScenarioChain(context.Scenario, dictionary, issues, message.MsgType);
        List<FixMemberDefinition> effectiveMembers = [];
        if (messageDefinition != null)
        {
            ApplyMemberOverrides(effectiveMembers, messageDefinition.Members, scenarioChain, dictionary.Options.NormalizeNames);
        }
        for (int scenarioIndex = 0; scenarioIndex < scenarioChain.Count; scenarioIndex++)
        {
            ApplyMemberOverrides(effectiveMembers, scenarioChain[scenarioIndex].Members, scenarioChain, dictionary.Options.NormalizeNames);
        }

        ValidateMemberRules(effectiveMembers, message, null, dictionary, context, scenarioChain, issues, "", 0);
        ValidateScenarioRules(scenarioChain, message, dictionary, context, issues);
        ValidateMessageDiscriminators(message, dictionary, issues);
        return issues;
    }

    internal static List<FixScenarioDefinition> ResolveScenarioChain(string selectedName, FixDictionary dictionary, List<ValidationIssue> issues, string msgType)
    {
        List<FixScenarioDefinition> chain = [];
        if (selectedName.Length == 0)
        {
            return chain;
        }
        FixScenarioDefinition? selected = FindScenarioDefinition(selectedName, dictionary);
        if (selected == null)
        {
            if (string.Equals(selectedName, "base", System.StringComparison.OrdinalIgnoreCase) && dictionary.Scenarios.Count == 0)
            {
                return chain;
            }
            AddConditionalIssue(issues, ValidationSeverity.Error, "unsupported-scenario", "The selected dictionary scenario is not defined.", false, 0, null, "", msgType);
            return chain;
        }
        List<string> path = [];
        AppendScenarioInheritance(selected, dictionary, chain, path, issues, msgType, 0);
        return chain;
    }

    internal static void AppendScenarioInheritance(FixScenarioDefinition scenario, FixDictionary dictionary, List<FixScenarioDefinition> chain, List<string> path, List<ValidationIssue> issues, string msgType, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            AddConditionalIssue(issues, ValidationSeverity.Error, "scenario-inheritance-limit", "Scenario inheritance exceeds the supported depth.", false, 0, null, "", msgType);
            return;
        }
        string key = DictionaryNormalizer.CreateNameKey(scenario.Name, dictionary.Options.NormalizeNames);
        if (ContainsText(path, key))
        {
            AddConditionalIssue(issues, ValidationSeverity.Error, "recursive-scenario-reference", "Scenario inheritance contains a cycle.", false, 0, null, "", msgType);
            return;
        }
        path.Add(key);
        if (scenario.ParentName.Length != 0)
        {
            FixScenarioDefinition? parent = FindScenarioDefinition(scenario.ParentName, dictionary);
            if (parent == null)
            {
                AddConditionalIssue(issues, ValidationSeverity.Error, "unresolved-parent-scenario", "A parent scenario cannot be resolved.", false, 0, null, "", msgType);
            }
            else
            {
                AppendScenarioInheritance(parent, dictionary, chain, path, issues, msgType, depth + 1);
            }
        }
        path.RemoveAt(path.Count - 1);
        if (!ContainsScenario(chain, scenario))
        {
            chain.Add(scenario);
        }
    }

    internal static FixScenarioDefinition? FindScenarioDefinition(string name, FixDictionary dictionary)
    {
        string key = DictionaryNormalizer.CreateNameKey(name, dictionary.Options.NormalizeNames);
        for (int index = 0; index < dictionary.Scenarios.Count; index++)
        {
            FixScenarioDefinition scenario = dictionary.Scenarios[index];
            if (!scenario.IsRemoved && DictionaryNormalizer.CreateNameKey(scenario.Name, dictionary.Options.NormalizeNames) == key)
            {
                return scenario;
            }
        }
        return null;
    }

    internal static void ApplyMemberOverrides(List<FixMemberDefinition> destination, List<FixMemberDefinition> source, List<FixScenarioDefinition> scenarioChain, bool normalizeNames)
    {
        for (int index = 0; index < source.Count; index++)
        {
            FixMemberDefinition member = source[index];
            if (member.IsRemoved || !MemberScenarioApplies(member, scenarioChain, normalizeNames))
            {
                continue;
            }
            int match = FindEffectiveMember(destination, member);
            if (match < 0)
            {
                destination.Add(member);
            }
            else
            {
                destination[match] = member;
            }
        }
    }

    internal static bool MemberScenarioApplies(FixMemberDefinition member, List<FixScenarioDefinition> scenarioChain, bool normalizeNames)
    {
        if (member.Scenario.Length == 0)
        {
            return true;
        }
        for (int index = 0; index < scenarioChain.Count; index++)
        {
            if (DictionaryNormalizer.CreateNameKey(member.Scenario, normalizeNames) == DictionaryNormalizer.CreateNameKey(scenarioChain[index].Name, normalizeNames))
            {
                return true;
            }
        }
        return false;
    }

    internal static int FindEffectiveMember(List<FixMemberDefinition> members, FixMemberDefinition sought)
    {
        for (int index = 0; index < members.Count; index++)
        {
            if (members[index].Kind == sought.Kind && members[index].ReferenceId == sought.ReferenceId)
            {
                return index;
            }
        }
        return -1;
    }

    internal static void ValidateMemberRules(List<FixMemberDefinition> members, FixMessage message, FixGroupEntry? entry, FixDictionary dictionary, ValidationContext context, List<FixScenarioDefinition> scenarioChain, List<ValidationIssue> issues, string groupPath, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            AddConditionalIssueOnce(issues, "conditional-expansion-limit", "Conditional member expansion exceeds the supported depth.", groupPath, message.MsgType);
            return;
        }
        for (int index = 0; index < members.Count; index++)
        {
            FixMemberDefinition member = members[index];
            if (member.IsRemoved || !MemberScenarioApplies(member, scenarioChain, dictionary.Options.NormalizeNames))
            {
                continue;
            }
            ValidateMemberPresenceRule(member, message, entry, dictionary, context, issues, groupPath, depth);
            if (member.Kind == FixMemberKind.Component)
            {
                FixComponentDefinition? component = DictionaryStructureValidator.FindComponentForValidation(dictionary, member.ReferenceId);
                if (component != null)
                {
                    ValidateMemberRules(component.Members, message, entry, dictionary, context, scenarioChain, issues, groupPath, depth + 1);
                }
            }
            else if (member.Kind == FixMemberKind.Group)
            {
                FixGroupDefinition? definition = DictionaryStructureValidator.FindGroupForValidation(dictionary, member.ReferenceId);
                if (definition == null)
                {
                    continue;
                }
                FixGroup? group = entry == null ? ScoreEvaluator.FindMessageGroup(message, definition.CountTag) : ScoreEvaluator.FindEntryGroup(entry, definition.CountTag);
                if (group == null)
                {
                    continue;
                }
                for (int entryIndex = 0; entryIndex < group.Entries.Count; entryIndex++)
                {
                    string entryPath = groupPath.Length == 0 ? definition.CountTag.ToString() + "[" + entryIndex.ToString() + "]" : groupPath + "/" + definition.CountTag.ToString() + "[" + entryIndex.ToString() + "]";
                    ValidateMemberRules(definition.Members, message, group.Entries[entryIndex], dictionary, context, scenarioChain, issues, entryPath, depth + 1);
                }
            }
        }
    }

    internal static void ValidateMemberPresenceRule(FixMemberDefinition member, FixMessage message, FixGroupEntry? entry, FixDictionary dictionary, ValidationContext context, List<ValidationIssue> issues, string groupPath, int depth)
    {
        bool hasRule = member.PresenceRule.Length != 0;
        if (!hasRule && member.Presence != FixPresence.Conditional)
        {
            return;
        }
        if (!hasRule)
        {
            AddMemberRuleIssue(issues, ValidationSeverity.Error, "unsupported-conditional-rule", "A conditional member has no rule expression.", member, message, entry, dictionary, groupPath);
            return;
        }
        RuleEvaluationResult evaluation;
        if (entry == null)
        {
            evaluation = ScoreEvaluator.EvaluateScoreRule(member.PresenceRule, member.RulePresence, dictionary, message, context);
        }
        else
        {
            evaluation = ScoreEvaluator.EvaluateScoreRuleInEntry(member.PresenceRule, member.RulePresence, dictionary, message, context, entry);
        }
        if (!evaluation.WasEvaluated)
        {
            AddEvaluationIssue(issues, evaluation, member, message, entry, dictionary, groupPath);
            return;
        }
        FixPresence selected = evaluation.ConditionMatched ? member.RulePresence : member.Presence;
        if (!evaluation.ConditionMatched && selected == FixPresence.Conditional)
        {
            selected = FixPresence.Optional;
        }
        bool present = MemberIsPresent(member, message, entry, dictionary, depth + 1);
        if ((selected == FixPresence.Required || selected == FixPresence.Constant) && !present)
        {
            AddMemberRuleIssue(issues, ValidationSeverity.Error, "conditionally-required-member-missing", "A conditional rule requires a member that is absent.", member, message, entry, dictionary, groupPath);
        }
        else if (selected == FixPresence.Forbidden && present)
        {
            AddMemberRuleIssue(issues, ValidationSeverity.Error, "conditionally-forbidden-member-present", "A conditional rule forbids a member that is present.", member, message, entry, dictionary, groupPath);
        }
    }

    internal static void ValidateScenarioRules(List<FixScenarioDefinition> scenarioChain, FixMessage message, FixDictionary dictionary, ValidationContext context, List<ValidationIssue> issues)
    {
        for (int scenarioIndex = 0; scenarioIndex < scenarioChain.Count; scenarioIndex++)
        {
            FixScenarioDefinition scenario = scenarioChain[scenarioIndex];
            for (int ruleIndex = 0; ruleIndex < scenario.Rules.Count; ruleIndex++)
            {
                RuleEvaluationResult evaluation = ScoreEvaluator.EvaluateScoreRule(scenario.Rules[ruleIndex], FixPresence.Required, dictionary, message, context);
                if (!evaluation.WasEvaluated)
                {
                    AddScenarioEvaluationIssue(issues, evaluation, scenario, message.MsgType);
                }
                else if (!evaluation.ConditionMatched)
                {
                    AddConditionalIssue(issues, ValidationSeverity.Error, "scenario-rule-failed", "A constraint for scenario '" + scenario.Name + "' evaluated to false.", false, 0, null, "", message.MsgType);
                }
            }
        }
    }

    internal static void ValidateMessageDiscriminators(FixMessage message, FixDictionary dictionary, List<ValidationIssue> issues)
    {
        ValidateFlatDiscriminators(message.HeaderFields, message, null, dictionary, issues, "", message.MsgType);
        ValidateFlatDiscriminators(message.BodyFields, message, null, dictionary, issues, "", message.MsgType);
        ValidateFlatDiscriminators(message.TrailerFields, message, null, dictionary, issues, "", message.MsgType);
        ValidateGroupDiscriminators(message.HeaderGroups, dictionary, issues, message.MsgType, "", 0);
        ValidateGroupDiscriminators(message.BodyGroups, dictionary, issues, message.MsgType, "", 0);
        ValidateGroupDiscriminators(message.TrailerGroups, dictionary, issues, message.MsgType, "", 0);
    }

    internal static void ValidateGroupDiscriminators(List<FixGroup> groups, FixDictionary dictionary, List<ValidationIssue> issues, string msgType, string parentPath, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            AddConditionalIssueOnce(issues, "conditional-expansion-limit", "Discriminator validation exceeds the supported group depth.", parentPath, msgType);
            return;
        }
        for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            FixGroup group = groups[groupIndex];
            for (int entryIndex = 0; entryIndex < group.Entries.Count; entryIndex++)
            {
                FixGroupEntry entry = group.Entries[entryIndex];
                string path = parentPath.Length == 0 ? group.CountTag.ToString() + "[" + entryIndex.ToString() + "]" : parentPath + "/" + group.CountTag.ToString() + "[" + entryIndex.ToString() + "]";
                ValidateFlatDiscriminators(entry.Fields, null, entry, dictionary, issues, path, msgType);
                ValidateGroupDiscriminators(entry.Groups, dictionary, issues, msgType, path, depth + 1);
            }
        }
    }

    internal static void ValidateFlatDiscriminators(List<FixField> fields, FixMessage? message, FixGroupEntry? entry, FixDictionary dictionary, List<ValidationIssue> issues, string groupPath, string msgType)
    {
        for (int index = 0; index < fields.Count; index++)
        {
            FixField dependent = fields[index];
            FixFieldDefinition? dependentDefinition = ScoreEvaluator.FindFieldDefinitionByTag(dependent.Tag, dictionary);
            if (dependentDefinition == null || !dependentDefinition.HasDiscriminatorTag)
            {
                continue;
            }
            FixFieldDefinition? discriminatorDefinition = ScoreEvaluator.FindFieldDefinitionByTag(dependentDefinition.DiscriminatorTag, dictionary);
            if (discriminatorDefinition == null)
            {
                AddConditionalIssue(issues, ValidationSeverity.Error, "unresolved-field-discriminator", "A field's discriminator definition cannot be resolved.", true, dependentDefinition.DiscriminatorTag, null, groupPath, msgType);
                continue;
            }
            FixField? discriminator = entry == null ? FindMessageDiscriminator(message, dependentDefinition.DiscriminatorTag) : ScoreEvaluator.FindEntryField(entry, dependentDefinition.DiscriminatorTag);
            if (discriminator == null)
            {
                AddConditionalIssue(issues, ValidationSeverity.Error, "field-discriminator-missing", "A field is present without its required discriminator field.", true, dependentDefinition.DiscriminatorTag, null, groupPath, msgType);
                continue;
            }
            if (discriminatorDefinition.CodeSetName.Length == 0)
            {
                AddConditionalIssue(issues, ValidationSeverity.Error, "unsupported-discriminator-code-set", "The discriminator field has no code-set definition.", true, discriminator.Tag, discriminator, groupPath, msgType);
                continue;
            }
            FixCodeSetDefinition? codeSet = ScalarValueValidator.FindCodeSet(dictionary, discriminatorDefinition.CodeSetName);
            if (codeSet == null)
            {
                AddConditionalIssue(issues, ValidationSeverity.Error, "unsupported-discriminator-code-set", "The discriminator field's code set cannot be resolved.", true, discriminator.Tag, discriminator, groupPath, msgType);
                continue;
            }
            string value = Encoding.Latin1.GetString(discriminator.Value);
            if (!ScalarValueValidator.ContainsCode(codeSet, value))
            {
                AddConditionalIssue(issues, ValidationSeverity.Error, "invalid-field-discriminator", "The discriminator field value is not allowed by its code set.", true, discriminator.Tag, discriminator, groupPath, msgType);
            }
        }
    }

    internal static FixField? FindMessageDiscriminator(FixMessage? message, int tag)
    {
        if (message == null)
        {
            return null;
        }
        return ScoreEvaluator.FindMessageField(message, tag);
    }

    internal static bool MemberIsPresent(FixMemberDefinition member, FixMessage message, FixGroupEntry? entry, FixDictionary dictionary, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            return false;
        }
        if (member.Kind == FixMemberKind.Field)
        {
            return entry == null ? ScoreEvaluator.FindMessageField(message, member.ReferenceId) != null : ScoreEvaluator.FindEntryField(entry, member.ReferenceId) != null;
        }
        if (member.Kind == FixMemberKind.Group)
        {
            FixGroupDefinition? groupDefinition = DictionaryStructureValidator.FindGroupForValidation(dictionary, member.ReferenceId);
            if (groupDefinition == null) return false;
            return entry == null ? ScoreEvaluator.FindMessageGroup(message, groupDefinition.CountTag) != null : ScoreEvaluator.FindEntryGroup(entry, groupDefinition.CountTag) != null;
        }
        if (member.Kind == FixMemberKind.Component)
        {
            FixComponentDefinition? component = DictionaryStructureValidator.FindComponentForValidation(dictionary, member.ReferenceId);
            if (component == null) return false;
            for (int index = 0; index < component.Members.Count; index++)
            {
                if (MemberIsPresent(component.Members[index], message, entry, dictionary, depth + 1)) return true;
            }
        }
        return false;
    }

    internal static void AddEvaluationIssue(List<ValidationIssue> issues, RuleEvaluationResult evaluation, FixMemberDefinition member, FixMessage message, FixGroupEntry? entry, FixDictionary dictionary, string groupPath)
    {
        bool indeterminate = IsUnavailableInput(evaluation.ErrorCode);
        string code = indeterminate ? "conditional-rule-indeterminate" : "unsupported-conditional-rule";
        ValidationSeverity severity = indeterminate ? ValidationSeverity.Warning : ValidationSeverity.Error;
        string description = indeterminate ? "A conditional rule could not be evaluated because an input is unavailable (" + evaluation.ErrorCode + ")." : "A conditional rule is unsupported or invalid (" + evaluation.ErrorCode + ").";
        AddMemberRuleIssue(issues, severity, code, description, member, message, entry, dictionary, groupPath);
    }

    internal static void AddScenarioEvaluationIssue(List<ValidationIssue> issues, RuleEvaluationResult evaluation, FixScenarioDefinition scenario, string msgType)
    {
        bool indeterminate = IsUnavailableInput(evaluation.ErrorCode);
        string code = indeterminate ? "scenario-rule-indeterminate" : "unsupported-scenario-rule";
        ValidationSeverity severity = indeterminate ? ValidationSeverity.Warning : ValidationSeverity.Error;
        string description = indeterminate ? "A rule for scenario '" + scenario.Name + "' has an unavailable input (" + evaluation.ErrorCode + ")." : "A rule for scenario '" + scenario.Name + "' is unsupported or invalid (" + evaluation.ErrorCode + ").";
        AddConditionalIssue(issues, severity, code, description, false, 0, null, "", msgType);
    }

    internal static bool IsUnavailableInput(string code)
    {
        return code == "external-state-unavailable" || code == "field-unavailable" || code == "group-entry-unavailable";
    }

    internal static void AddMemberRuleIssue(List<ValidationIssue> issues, ValidationSeverity severity, string code, string description, FixMemberDefinition member, FixMessage message, FixGroupEntry? entry, FixDictionary dictionary, string groupPath)
    {
        bool hasTag = false;
        int tag = 0;
        FixField? field = null;
        if (member.Kind == FixMemberKind.Field)
        {
            hasTag = true;
            tag = member.ReferenceId;
            field = entry == null ? ScoreEvaluator.FindMessageField(message, tag) : ScoreEvaluator.FindEntryField(entry, tag);
        }
        else if (member.Kind == FixMemberKind.Group)
        {
            FixGroupDefinition? group = DictionaryStructureValidator.FindGroupForValidation(dictionary, member.ReferenceId);
            if (group != null)
            {
                hasTag = true;
                tag = group.CountTag;
                field = entry == null ? ScoreEvaluator.FindMessageField(message, tag) : ScoreEvaluator.FindEntryField(entry, tag);
            }
        }
        AddConditionalIssue(issues, severity, code, description, hasTag, tag, field, groupPath, message.MsgType);
    }

    internal static void AddConditionalIssueOnce(List<ValidationIssue> issues, string code, string description, string groupPath, string msgType)
    {
        for (int index = 0; index < issues.Count; index++)
        {
            if (issues[index].Code == code && issues[index].GroupPath == groupPath)
            {
                return;
            }
        }
        AddConditionalIssue(issues, ValidationSeverity.Error, code, description, false, 0, null, groupPath, msgType);
    }

    internal static void AddConditionalIssue(List<ValidationIssue> issues, ValidationSeverity severity, string code, string description, bool hasTag, int tag, FixField? field, string groupPath, string msgType)
    {
        ValidationIssue issue = new ValidationIssue();
        issue.Layer = ValidationLayer.ConditionalRule;
        issue.Severity = severity;
        issue.Code = code;
        issue.Message = description;
        issue.HasTag = hasTag;
        issue.Tag = tag;
        issue.GroupPath = groupPath;
        issue.MsgType = msgType;
        if (field != null && field.SourceOffset >= 0)
        {
            issue.HasSourceOffset = true;
            issue.SourceOffset = field.SourceOffset;
        }
        issues.Add(issue);
    }

    internal static bool ContainsText(List<string> values, string sought)
    {
        for (int index = 0; index < values.Count; index++)
        {
            if (values[index] == sought) return true;
        }
        return false;
    }

    internal static bool ContainsScenario(List<FixScenarioDefinition> scenarios, FixScenarioDefinition sought)
    {
        for (int index = 0; index < scenarios.Count; index++)
        {
            if (scenarios[index] == sought) return true;
        }
        return false;
    }
}
