using System.Collections.Generic;

namespace fix;

internal static class DictionaryStructureValidator
{
    internal static List<ValidationIssue> ValidateDictionaryStructure(FixMessage message, FixDictionary dictionary, ValidationOptions options, ValidationContext context)
    {
        List<ValidationIssue> issues = [];
        if (!ValidationCoordinator.IsLayerEnabled(options, ValidationLayer.DictionaryGrammar))
        {
            return issues;
        }

        DictionarySectionPlan header = CreateHeaderPlan(dictionary, issues, message.MsgType);
        DictionarySectionPlan trailer = CreateTrailerPlan(dictionary, issues, message.MsgType);
        FixMessageDefinition? definition = FindMessageForValidation(dictionary, message.MsgType, context.Scenario);
        DictionarySectionPlan body = new DictionarySectionPlan();
        if (definition == null)
        {
            AddStructureIssue(issues, ValidationSeverity.Error, "unsupported-message-type", "The selected dictionary does not define the message type and scenario.", false, 0, null, message.MsgType);
        }
        else
        {
            ExpandMembersForPlan(definition.Members, dictionary, body, issues, message.MsgType, 0, false);
        }

        ValidateSectionPlacement(message.HeaderFields, header, body, trailer, dictionary, options, issues, message.MsgType, false);
        ValidateSectionPlacement(message.TrailerFields, trailer, header, body, dictionary, options, issues, message.MsgType, false);
        if (options.CheckFieldOrder)
        {
            ValidateSectionOrder(message.HeaderFields, header, issues, message.MsgType, "header-field-order-invalid");
            ValidateSectionOrder(message.TrailerFields, trailer, issues, message.MsgType, "trailer-field-order-invalid");
        }
        if (definition != null)
        {
            ValidateSectionPlacement(message.BodyFields, body, header, trailer, dictionary, options, issues, message.MsgType, true);
            ValidateRequiredMembers(definition.Members, message.BodyFields, dictionary, issues, message.MsgType, true, 0, options.CheckRequiredFields);
            ValidateBodyDuplicates(message.BodyFields, body, options, issues, message.MsgType);
            if (options.CheckFieldOrder)
            {
                ValidateSectionOrder(message.BodyFields, body, issues, message.MsgType, "body-field-order-invalid");
            }
        }
        return issues;
    }

    internal static DictionarySectionPlan CreateHeaderPlan(FixDictionary dictionary, List<ValidationIssue> issues, string msgType)
    {
        DictionarySectionPlan plan = new DictionarySectionPlan();
        AddPlanTag(plan.AllowedTags, FixTags.BeginString);
        AddPlanTag(plan.OrderedTags, FixTags.BeginString);
        AddPlanTag(plan.AllowedTags, FixTags.BodyLength);
        AddPlanTag(plan.OrderedTags, FixTags.BodyLength);
        AddPlanTag(plan.AllowedTags, FixTags.MsgType);
        AddPlanTag(plan.OrderedTags, FixTags.MsgType);
        ExpandMembersForPlan(dictionary.Header, dictionary, plan, issues, msgType, 0, false);
        return plan;
    }

    internal static DictionarySectionPlan CreateTrailerPlan(FixDictionary dictionary, List<ValidationIssue> issues, string msgType)
    {
        DictionarySectionPlan plan = new DictionarySectionPlan();
        ExpandMembersForPlan(dictionary.Trailer, dictionary, plan, issues, msgType, 0, false);
        AddPlanTag(plan.AllowedTags, FixTags.CheckSum);
        AddPlanTag(plan.OrderedTags, FixTags.CheckSum);
        return plan;
    }

    internal static void ExpandMembersForPlan(List<FixMemberDefinition> members, FixDictionary dictionary, DictionarySectionPlan plan, List<ValidationIssue> issues, string msgType, int depth, bool insideGroup)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            AddStructureIssueOnce(issues, "dictionary-expansion-limit", "Dictionary member expansion exceeded the supported nesting depth.", false, 0, msgType);
            return;
        }
        for (int index = 0; index < members.Count; index++)
        {
            FixMemberDefinition member = members[index];
            if (member.Kind == FixMemberKind.Field)
            {
                AddPlanTag(plan.AllowedTags, member.ReferenceId);
                if (insideGroup)
                {
                    AddPlanTag(plan.GroupTags, member.ReferenceId);
                }
                else
                {
                    AddPlanTag(plan.OrderedTags, member.ReferenceId);
                }
            }
            else if (member.Kind == FixMemberKind.Component)
            {
                FixComponentDefinition? component = FindComponentForValidation(dictionary, member.ReferenceId);
                if (component == null)
                {
                    AddStructureIssueOnce(issues, "unresolved-component-reference", "A dictionary component reference cannot be resolved.", false, member.ReferenceId, msgType);
                }
                else
                {
                    ExpandMembersForPlan(component.Members, dictionary, plan, issues, msgType, depth + 1, insideGroup);
                }
            }
            else if (member.Kind == FixMemberKind.Group)
            {
                FixGroupDefinition? group = FindGroupForValidation(dictionary, member.ReferenceId);
                if (group == null)
                {
                    AddStructureIssueOnce(issues, "unresolved-group-reference", "A dictionary group reference cannot be resolved.", false, member.ReferenceId, msgType);
                }
                else
                {
                    AddPlanTag(plan.AllowedTags, group.CountTag);
                    AddPlanTag(plan.GroupTags, group.CountTag);
                    if (!insideGroup)
                    {
                        AddPlanTag(plan.OrderedTags, group.CountTag);
                    }
                    ExpandMembersForPlan(group.Members, dictionary, plan, issues, msgType, depth + 1, true);
                }
            }
        }
    }

    internal static FixMessageDefinition? FindMessageForValidation(FixDictionary dictionary, string msgType, string scenario)
    {
        FixMessageDefinition? fallback = null;
        for (int index = 0; index < dictionary.Messages.Count; index++)
        {
            FixMessageDefinition definition = dictionary.Messages[index];
            if (definition.MsgType == msgType)
            {
                if (definition.Scenario == scenario)
                {
                    return definition;
                }
                if (definition.Scenario.Length == 0)
                {
                    fallback = definition;
                }
            }
        }
        return fallback;
    }

    internal static FixComponentDefinition? FindComponentForValidation(FixDictionary dictionary, int id)
    {
        for (int index = 0; index < dictionary.Components.Count; index++)
        {
            if (dictionary.Components[index].Id == id)
            {
                return dictionary.Components[index];
            }
        }
        return null;
    }

    internal static FixGroupDefinition? FindGroupForValidation(FixDictionary dictionary, int id)
    {
        for (int index = 0; index < dictionary.Groups.Count; index++)
        {
            if (dictionary.Groups[index].Id == id)
            {
                return dictionary.Groups[index];
            }
        }
        return null;
    }

    internal static void ValidateSectionPlacement(List<FixField> fields, DictionarySectionPlan current, DictionarySectionPlan otherFirst, DictionarySectionPlan otherSecond, FixDictionary dictionary, ValidationOptions options, List<ValidationIssue> issues, string msgType, bool body)
    {
        for (int index = 0; index < fields.Count; index++)
        {
            FixField field = fields[index];
            bool userDefined = dictionary.Options.UserDefinedTagMinimum > 0 && field.Tag >= dictionary.Options.UserDefinedTagMinimum;
            if (body && userDefined && options.AllowUserDefinedFields)
            {
                continue;
            }
            FixFieldDefinition? definition = MessageBuilder.FindFieldDefinition(dictionary, field.Tag);
            if (definition == null)
            {
                if (!body && userDefined && options.AllowUserDefinedFields)
                {
                    AddStructureIssue(issues, ValidationSeverity.Error, "user-defined-field-in-session-section", "An undefined user field cannot be placed in the session header or trailer.", true, field.Tag, field, msgType);
                }
                else if (options.RejectUnknownFields)
                {
                    AddStructureIssue(issues, ValidationSeverity.Error, "unknown-field", "The selected dictionary does not define this field tag.", true, field.Tag, field, msgType);
                }
                continue;
            }
            if (ContainsPlanTag(current.AllowedTags, field.Tag))
            {
                continue;
            }
            if (ContainsPlanTag(otherFirst.AllowedTags, field.Tag) || ContainsPlanTag(otherSecond.AllowedTags, field.Tag))
            {
                AddStructureIssue(issues, ValidationSeverity.Error, "field-in-wrong-section", "A known field appears in the wrong message section.", true, field.Tag, field, msgType);
            }
            else
            {
                AddStructureIssue(issues, ValidationSeverity.Error, "field-not-allowed", "The message definition does not allow this known field.", true, field.Tag, field, msgType);
            }
        }
    }

    internal static void ValidateRequiredMembers(List<FixMemberDefinition> members, List<FixField> fields, FixDictionary dictionary, List<ValidationIssue> issues, string msgType, bool parentActive, int depth, bool checkRequired)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            AddStructureIssueOnce(issues, "dictionary-expansion-limit", "Dictionary member expansion exceeded the supported nesting depth.", false, 0, msgType);
            return;
        }
        for (int index = 0; index < members.Count; index++)
        {
            FixMemberDefinition member = members[index];
            bool conditional = member.PresenceRule.Length != 0 || member.Presence == FixPresence.Conditional;
            bool required = !conditional && (member.Presence == FixPresence.Required || member.Presence == FixPresence.Constant);
            if (member.Kind == FixMemberKind.Field)
            {
                FixField? field = FindFieldInSection(fields, member.ReferenceId);
                if (checkRequired && parentActive && required && field == null)
                {
                    AddStructureIssue(issues, ValidationSeverity.Error, "required-field-missing", "A required message field is missing.", true, member.ReferenceId, null, msgType);
                }
                if (!conditional && member.Presence == FixPresence.Forbidden && field != null)
                {
                    AddStructureIssue(issues, ValidationSeverity.Error, "forbidden-field-present", "A forbidden message field is present.", true, member.ReferenceId, field, msgType);
                }
            }
            else if (member.Kind == FixMemberKind.Component)
            {
                FixComponentDefinition? component = FindComponentForValidation(dictionary, member.ReferenceId);
                if (component != null)
                {
                    bool present = ComponentHasField(component, fields, dictionary, depth + 1);
                    if (!conditional && member.Presence == FixPresence.Forbidden && present)
                    {
                        AddStructureIssue(issues, ValidationSeverity.Error, "forbidden-component-present", "A forbidden component has fields in the message.", false, member.ReferenceId, null, msgType);
                    }
                    bool active = parentActive && required || present;
                    ValidateRequiredMembers(component.Members, fields, dictionary, issues, msgType, active, depth + 1, checkRequired);
                }
            }
            else if (member.Kind == FixMemberKind.Group)
            {
                FixGroupDefinition? group = FindGroupForValidation(dictionary, member.ReferenceId);
                if (group != null)
                {
                    FixField? count = FindFieldInSection(fields, group.CountTag);
                    if (checkRequired && parentActive && required && count == null)
                    {
                        AddStructureIssue(issues, ValidationSeverity.Error, "required-field-missing", "A required repeating-group count field is missing.", true, group.CountTag, null, msgType);
                    }
                    if (!conditional && member.Presence == FixPresence.Forbidden && count != null)
                    {
                        AddStructureIssue(issues, ValidationSeverity.Error, "forbidden-group-present", "A forbidden repeating group is present.", true, group.CountTag, count, msgType);
                    }
                }
            }
        }
    }

    internal static bool ComponentHasField(FixComponentDefinition component, List<FixField> fields, FixDictionary dictionary, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            return false;
        }
        for (int index = 0; index < component.Members.Count; index++)
        {
            FixMemberDefinition member = component.Members[index];
            if (member.Kind == FixMemberKind.Field && FindFieldInSection(fields, member.ReferenceId) != null)
            {
                return true;
            }
            if (member.Kind == FixMemberKind.Component)
            {
                FixComponentDefinition? nested = FindComponentForValidation(dictionary, member.ReferenceId);
                if (nested != null && ComponentHasField(nested, fields, dictionary, depth + 1))
                {
                    return true;
                }
            }
            if (member.Kind == FixMemberKind.Group)
            {
                FixGroupDefinition? group = FindGroupForValidation(dictionary, member.ReferenceId);
                if (group != null && FindFieldInSection(fields, group.CountTag) != null)
                {
                    return true;
                }
            }
        }
        return false;
    }

    internal static void ValidateBodyDuplicates(List<FixField> fields, DictionarySectionPlan body, ValidationOptions options, List<ValidationIssue> issues, string msgType)
    {
        if (!options.RejectDuplicateFields)
        {
            return;
        }
        for (int index = 0; index < fields.Count; index++)
        {
            FixField field = fields[index];
            if (ContainsPlanTag(body.GroupTags, field.Tag) || HasEarlierBodyTag(fields, index, field.Tag))
            {
                continue;
            }
            if (CountBodyTag(fields, field.Tag) > 1)
            {
                AddStructureIssue(issues, ValidationSeverity.Error, "duplicate-body-field", "A non-group body field occurs more than once.", true, field.Tag, field, msgType);
            }
        }
    }

    internal static void ValidateSectionOrder(List<FixField> fields, DictionarySectionPlan plan, List<ValidationIssue> issues, string msgType, string code)
    {
        int lastPosition = -1;
        for (int index = 0; index < fields.Count; index++)
        {
            FixField field = fields[index];
            int position = FindPlanPosition(plan.OrderedTags, field.Tag);
            if (position < 0)
            {
                continue;
            }
            if (position < lastPosition)
            {
                AddStructureIssue(issues, ValidationSeverity.Error, code, "A field appears before an earlier dictionary member.", true, field.Tag, field, msgType);
            }
            else
            {
                lastPosition = position;
            }
        }
    }

    internal static FixField? FindFieldInSection(List<FixField> fields, int tag)
    {
        for (int index = 0; index < fields.Count; index++)
        {
            if (fields[index].Tag == tag)
            {
                return fields[index];
            }
        }
        return null;
    }

    internal static bool ContainsPlanTag(List<int> tags, int tag)
    {
        for (int index = 0; index < tags.Count; index++)
        {
            if (tags[index] == tag)
            {
                return true;
            }
        }
        return false;
    }

    internal static void AddPlanTag(List<int> tags, int tag)
    {
        if (tag > 0 && !ContainsPlanTag(tags, tag))
        {
            tags.Add(tag);
        }
    }

    internal static int FindPlanPosition(List<int> tags, int tag)
    {
        for (int index = 0; index < tags.Count; index++)
        {
            if (tags[index] == tag)
            {
                return index;
            }
        }
        return -1;
    }

    internal static int CountBodyTag(List<FixField> fields, int tag)
    {
        int count = 0;
        for (int index = 0; index < fields.Count; index++)
        {
            if (fields[index].Tag == tag)
            {
                count++;
            }
        }
        return count;
    }

    internal static bool HasEarlierBodyTag(List<FixField> fields, int exclusiveEnd, int tag)
    {
        for (int index = 0; index < exclusiveEnd; index++)
        {
            if (fields[index].Tag == tag)
            {
                return true;
            }
        }
        return false;
    }

    internal static void AddStructureIssueOnce(List<ValidationIssue> issues, string code, string description, bool hasTag, int tag, string msgType)
    {
        for (int index = 0; index < issues.Count; index++)
        {
            if (issues[index].Code == code && issues[index].Tag == tag)
            {
                return;
            }
        }
        AddStructureIssue(issues, ValidationSeverity.Fatal, code, description, hasTag, tag, null, msgType);
    }

    internal static void AddStructureIssue(List<ValidationIssue> issues, ValidationSeverity severity, string code, string description, bool hasTag, int tag, FixField? field, string msgType)
    {
        ValidationIssue issue = new ValidationIssue();
        issue.Layer = ValidationLayer.DictionaryGrammar;
        issue.Severity = severity;
        issue.Code = code;
        issue.Message = description;
        issue.HasTag = hasTag;
        issue.Tag = tag;
        issue.MsgType = msgType;
        if (field != null && field.SourceOffset >= 0)
        {
            issue.HasSourceOffset = true;
            issue.SourceOffset = field.SourceOffset;
        }
        issues.Add(issue);
    }
}
