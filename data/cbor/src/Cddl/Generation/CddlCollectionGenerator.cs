using System.Collections.Generic;
using System.Numerics;

namespace cbor;

internal class CddlCollectionGenerator
{
    internal CddlValueGeneratorCore Core;
    internal CddlGenerationState State;

    internal CddlCollectionGenerator(CddlValueGeneratorCore core)
    {
        Core = core;
        State = core.State;
    }

    internal CborValue? GenerateArrayValue(CddlNode node)
    {
        if (node.Children.Count != 1)
        {
            State.FailGeneration(
                "CDDL_GENERATION_ARRAY_INVALID",
                "A CDDL array requires one group expression.",
                node
            );
            return null;
        }
        if (!State.EnterNesting(node))
        {
            return null;
        }
        List<CborValue> items = new List<CborValue>();
        bool generated = GenerateArrayGroup(node.Children[0], items);
        State.LeaveNesting();
        if (!generated)
        {
            return null;
        }
        CborValue value = new CborValue();
        value.Kind = CborValueKind.Array;
        value.ArrayItems = items;
        value.LengthKind = CborLengthKind.Definite;
        return value;
    }

    internal CborValue? GenerateMapValue(CddlNode node)
    {
        if (node.Children.Count != 1)
        {
            State.FailGeneration(
                "CDDL_GENERATION_MAP_INVALID",
                "A CDDL map requires one group expression.",
                node
            );
            return null;
        }
        if (!State.EnterNesting(node))
        {
            return null;
        }
        List<CborMapEntry> entries = new List<CborMapEntry>();
        bool generated = GenerateMapGroup(node.Children[0], entries);
        State.LeaveNesting();
        if (!generated)
        {
            return null;
        }
        CborValue value = new CborValue();
        value.Kind = CborValueKind.Map;
        value.MapEntries = entries;
        value.LengthKind = CborLengthKind.Definite;
        return value;
    }

    internal CborValue? GenerateGroupEnumerationValue(CddlNode node)
    {
        List<CddlNode> candidates = new List<CddlNode>();
        if (node.Name != null)
        {
            CddlRule? rule = State.FindReferencedRule(node);
            if (rule == null || rule.Kind != CddlRuleKind.Group
                || rule.Value == null)
            {
                State.FailGeneration(
                    "CDDL_GENERATION_ENUMERATION_INVALID",
                    "The group-enumeration target is unavailable.",
                    node
                );
                return null;
            }
            CollectEnumerationCandidates(
                rule.Value,
                candidates,
                new List<CddlRule>()
            );
        }
        else if (node.Children.Count > 0)
        {
            CollectEnumerationCandidates(
                node.Children[0],
                candidates,
                new List<CddlRule>()
            );
        }
        if (candidates.Count == 0)
        {
            State.FailGeneration(
                "CDDL_GENERATION_ENUMERATION_EMPTY",
                "The group enumeration contains no value alternatives.",
                node
            );
            return null;
        }
        int start = Core.SelectChoiceStart(candidates.Count);
        int attempt = 0;
        while (attempt < candidates.Count)
        {
            if (!State.TakeAttempt(node))
            {
                return null;
            }
            int index = (start + attempt) % candidates.Count;
            State.ClearFailure();
            CborValue? value = Core.GenerateNodeValue(candidates[index]);
            if (value != null)
            {
                return value;
            }
            if (State.RemainingSteps <= 0
                || (attempt + 1 < candidates.Count
                    && !State.HasAttemptBudget(node)))
            {
                return null;
            }
            attempt = attempt + 1;
        }
        State.FailGeneration(
            "CDDL_GENERATION_ENUMERATION_FAILED",
            "No group-enumeration candidate could be generated.",
            node
        );
        return null;
    }

    internal CborValue? GenerateArrayRepresentationValue(CddlNode node)
    {
        int length = SelectRepresentationLength(node);
        if (length < 0)
        {
            return null;
        }
        if (!State.EnterNesting(node))
        {
            return null;
        }
        List<CborValue> items = new List<CborValue>();
        int index = 0;
        while (index < length)
        {
            CborValue? item = Core.GenerateAnyScalar(node);
            if (item == null)
            {
                State.LeaveNesting();
                return null;
            }
            items.Add(item);
            index = index + 1;
        }
        State.LeaveNesting();
        CborValue value = new CborValue();
        value.Kind = CborValueKind.Array;
        value.ArrayItems = items;
        value.LengthKind = node.HasArgumentValue
                && node.ArgumentValue == new BigInteger(31)
            ? CborLengthKind.Indefinite
            : CborLengthKind.Definite;
        return value;
    }

    internal CborValue? GenerateMapRepresentationValue(CddlNode node)
    {
        int length = SelectRepresentationLength(node);
        if (length < 0)
        {
            return null;
        }
        if (!State.EnterNesting(node))
        {
            return null;
        }
        List<CborMapEntry> entries = new List<CborMapEntry>();
        int index = 0;
        while (index < length)
        {
            CborMapEntry entry = new CborMapEntry();
            entry.Key = Core.CreateIntegerValue(new BigInteger(index));
            entry.Value = Core.GenerateAnyScalar(node);
            if (entry.Value == null)
            {
                State.LeaveNesting();
                return null;
            }
            entries.Add(entry);
            index = index + 1;
        }
        State.LeaveNesting();
        CborValue value = new CborValue();
        value.Kind = CborValueKind.Map;
        value.MapEntries = entries;
        value.LengthKind = node.HasArgumentValue
                && node.ArgumentValue == new BigInteger(31)
            ? CborLengthKind.Indefinite
            : CborLengthKind.Definite;
        return value;
    }

    internal int SelectRepresentationLength(CddlNode node)
    {
        int length = 0;
        if (node.HasArgumentValue)
        {
            int additional = (int)node.ArgumentValue;
            if (additional >= 0 && additional <= 23)
            {
                length = additional;
            }
            else if (additional == 24)
            {
                length = 24;
            }
            else if (additional == 25)
            {
                length = 256;
            }
            else if (additional != 31)
            {
                State.FailGeneration(
                    "CDDL_GENERATION_REPRESENTATION_LIMIT",
                    "The requested container representation is too large for generation.",
                    node
                );
                return -1;
            }
        }
        else if (State.Options.Strategy == CddlGenerationStrategy.Boundary)
        {
            length = 23;
        }
        else if (State.Options.Strategy
            == CddlGenerationStrategy.Representative)
        {
            length = 1 + State.NextIndex(8);
        }
        if (length > State.Options.MaxCollectionItems)
        {
            State.FailGeneration(
                "CDDL_GENERATION_COLLECTION_LIMIT",
                "The generated container exceeds the configured collection-item limit.",
                node
            );
            return -1;
        }
        return length;
    }

    internal bool GenerateArrayGroup(
        CddlNode node,
        List<CborValue> items)
    {
        if (!State.TakeStep(node))
        {
            return false;
        }
        if (node.Kind == CddlNodeKind.Group)
        {
            int index = 0;
            while (index < node.Children.Count)
            {
                if (!GenerateArrayGroup(node.Children[index], items))
                {
                    return false;
                }
                index = index + 1;
            }
            return true;
        }
        if (node.Kind == CddlNodeKind.GroupChoice)
        {
            return GenerateArrayGroupChoice(node, items);
        }
        if (node.Kind == CddlNodeKind.GroupEntry)
        {
            return GenerateArrayEntryOccurrences(node, items);
        }
        if (node.Kind == CddlNodeKind.ParenthesizedGroup
            && node.Children.Count == 1)
        {
            return GenerateArrayGroup(node.Children[0], items);
        }
        if (IsGroupProducingNode(node))
        {
            return GenerateIncludedArrayGroup(node, items);
        }
        State.FailGeneration(
            "CDDL_GENERATION_GROUP_INVALID",
            "The schema node cannot generate an ordered group.",
            node
        );
        return false;
    }

    internal bool GenerateArrayGroupChoice(
        CddlNode node,
        List<CborValue> items)
    {
        if (node.Children.Count == 0)
        {
            return true;
        }
        List<int> branches = new List<int>();
        int branchIndex = 0;
        while (branchIndex < node.Children.Count)
        {
            if ((State.ProductiveOnlyDepth == 0
                    && State.Options.Strategy
                        != CddlGenerationStrategy.Minimal)
                || Core.Productivity.CanGenerateNodeWithoutRules(
                    node.Children[branchIndex],
                    Core.ActiveRules
                ))
            {
                branches.Add(branchIndex);
            }
            branchIndex = branchIndex + 1;
        }
        if (branches.Count == 0)
        {
            State.FailGeneration(
                "CDDL_GENERATION_NO_FINITE_VALUE",
                "No finite ordered group branch is available.",
                node
            );
            return false;
        }
        int start = Core.SelectChoiceStart(branches.Count);
        int attempt = 0;
        int originalCount = items.Count;
        while (attempt < branches.Count)
        {
            if (!State.TakeAttempt(node))
            {
                return false;
            }
            RestoreArrayCount(items, originalCount);
            State.ClearFailure();
            int index = (start + attempt) % branches.Count;
            if (GenerateArrayGroup(
                node.Children[branches[index]],
                items
            ))
            {
                return true;
            }
            if (State.RemainingSteps <= 0
                || (attempt + 1 < branches.Count
                    && !State.HasAttemptBudget(node)))
            {
                return false;
            }
            attempt = attempt + 1;
        }
        RestoreArrayCount(items, originalCount);
        State.FailGeneration(
            "CDDL_GENERATION_GROUP_CHOICE_FAILED",
            "No ordered group-choice alternative could be generated.",
            node
        );
        return false;
    }

    internal bool GenerateArrayEntryOccurrences(
        CddlNode entry,
        List<CborValue> items)
    {
        if (entry.Children.Count == 0)
        {
            State.FailGeneration(
                "CDDL_GENERATION_GROUP_ENTRY_INVALID",
                "The ordered group entry has no value expression.",
                entry
            );
            return false;
        }
        CddlNode valueNode = entry.Children[entry.Children.Count - 1];
        bool included = IsGroupProducingNode(valueNode);
        int count = SelectOccurrenceCount(entry, items.Count, included);
        if (count < 0)
        {
            return false;
        }
        int occurrence = 0;
        while (occurrence < count)
        {
            int before = items.Count;
            if (included)
            {
                if (!GenerateIncludedArrayGroup(valueNode, items))
                {
                    RestoreArrayCount(items, before);
                    if (CanUseCompletedOccurrences(entry, occurrence))
                    {
                        State.ClearFailure();
                        return true;
                    }
                    return false;
                }
            }
            else
            {
                if (!EnsureArrayCapacity(items.Count + 1, entry))
                {
                    return false;
                }
                CborValue? value = Core.GenerateNodeValue(valueNode);
                if (value == null)
                {
                    RestoreArrayCount(items, before);
                    if (CanUseCompletedOccurrences(entry, occurrence))
                    {
                        State.ClearFailure();
                        return true;
                    }
                    return false;
                }
                items.Add(value);
            }
            if (items.Count > State.Options.MaxCollectionItems)
            {
                State.FailGeneration(
                    "CDDL_GENERATION_COLLECTION_LIMIT",
                    "The generated array exceeds the configured collection-item limit.",
                    entry
                );
                return false;
            }
            occurrence = occurrence + 1;
            if (included && items.Count == before
                && occurrence >= entry.MinimumOccurrence)
            {
                occurrence = count;
            }
        }
        return true;
    }

    internal bool GenerateIncludedArrayGroup(
        CddlNode node,
        List<CborValue> items)
    {
        if (node.Kind == CddlNodeKind.Group
            || node.Kind == CddlNodeKind.GroupChoice
            || node.Kind == CddlNodeKind.GroupEntry
            || node.Kind == CddlNodeKind.ParenthesizedGroup)
        {
            return GenerateArrayGroup(node, items);
        }
        if (node.Kind == CddlNodeKind.Unwrap)
        {
            return GenerateUnwrappedArrayGroup(node, items);
        }
        CddlRule? rule = State.FindReferencedRule(node);
        if (rule == null)
        {
            if (IsOptionalGroupSocket(node.Name))
            {
                return true;
            }
            State.FailGeneration(
                "CDDL_GENERATION_GROUP_REFERENCE_INVALID",
                "The included array group is unavailable.",
                node
            );
            return false;
        }
        if (rule.Kind != CddlRuleKind.Group || rule.Value == null)
        {
            State.FailGeneration(
                "CDDL_GENERATION_GROUP_REFERENCE_INVALID",
                "The included array rule is not a complete group rule.",
                node
            );
            return false;
        }
        bool recursive = Core.ContainsActiveRule(rule);
        if (recursive && !Core.Productivity.CanGenerateRuleValue(rule))
        {
            State.FailGeneration(
                "CDDL_GENERATION_NO_FINITE_VALUE",
                "The recursive array group cannot produce a finite value.",
                node
            );
            return false;
        }
        bool productiveOnly = recursive
            && Core.ShouldForceProductiveRule(rule);
        if (productiveOnly)
        {
            State.ProductiveOnlyDepth = State.ProductiveOnlyDepth + 1;
        }
        Core.ActiveRules.Add(rule);
        bool generated = GenerateArrayGroup(rule.Value, items);
        Core.ActiveRules.RemoveAt(Core.ActiveRules.Count - 1);
        if (productiveOnly)
        {
            State.ProductiveOnlyDepth = State.ProductiveOnlyDepth - 1;
        }
        return generated;
    }

    internal bool GenerateUnwrappedArrayGroup(
        CddlNode node,
        List<CborValue> items)
    {
        CddlRule? rule = State.FindReferencedRule(node);
        if (rule == null || rule.Value == null)
        {
            State.FailGeneration(
                "CDDL_GENERATION_UNWRAP_INVALID",
                "The unwrapped array target is unavailable.",
                node
            );
            return false;
        }
        CddlNode? content = FindUnwrappedContent(
            rule.Value,
            new List<CddlRule>()
        );
        if (content == null || !IsDirectGroupNode(content))
        {
            State.FailGeneration(
                "CDDL_GENERATION_UNWRAP_INVALID",
                "The unwrap target does not expose an array group.",
                node
            );
            return false;
        }
        bool recursive = Core.ContainsActiveRule(rule);
        if (recursive && !Core.Productivity.CanGenerateRuleValue(rule))
        {
            State.FailGeneration(
                "CDDL_GENERATION_NO_FINITE_VALUE",
                "The recursive unwrap cannot produce a finite value.",
                node
            );
            return false;
        }
        bool productiveOnly = recursive
            && Core.ShouldForceProductiveRule(rule);
        if (productiveOnly)
        {
            State.ProductiveOnlyDepth = State.ProductiveOnlyDepth + 1;
        }
        Core.ActiveRules.Add(rule);
        bool generated = GenerateArrayGroup(content, items);
        Core.ActiveRules.RemoveAt(Core.ActiveRules.Count - 1);
        if (productiveOnly)
        {
            State.ProductiveOnlyDepth = State.ProductiveOnlyDepth - 1;
        }
        return generated;
    }

    internal bool GenerateMapGroup(
        CddlNode node,
        List<CborMapEntry> entries)
    {
        if (!State.TakeStep(node))
        {
            return false;
        }
        if (node.Kind == CddlNodeKind.Group)
        {
            int index = 0;
            while (index < node.Children.Count)
            {
                if (!GenerateMapGroup(node.Children[index], entries))
                {
                    return false;
                }
                index = index + 1;
            }
            return true;
        }
        if (node.Kind == CddlNodeKind.GroupChoice)
        {
            return GenerateMapGroupChoice(node, entries);
        }
        if (node.Kind == CddlNodeKind.GroupEntry)
        {
            return GenerateMapEntryOccurrences(node, entries);
        }
        if (node.Kind == CddlNodeKind.ParenthesizedGroup
            && node.Children.Count == 1)
        {
            return GenerateMapGroup(node.Children[0], entries);
        }
        if (IsGroupProducingNode(node))
        {
            return GenerateIncludedMapGroup(node, entries);
        }
        State.FailGeneration(
            "CDDL_GENERATION_GROUP_INVALID",
            "The schema node cannot generate an unordered group.",
            node
        );
        return false;
    }

    internal bool GenerateMapGroupChoice(
        CddlNode node,
        List<CborMapEntry> entries)
    {
        if (node.Children.Count == 0)
        {
            return true;
        }
        List<int> branches = new List<int>();
        int branchIndex = 0;
        while (branchIndex < node.Children.Count)
        {
            if ((State.ProductiveOnlyDepth == 0
                    && State.Options.Strategy
                        != CddlGenerationStrategy.Minimal)
                || Core.Productivity.CanGenerateNodeWithoutRules(
                    node.Children[branchIndex],
                    Core.ActiveRules
                ))
            {
                branches.Add(branchIndex);
            }
            branchIndex = branchIndex + 1;
        }
        if (branches.Count == 0)
        {
            State.FailGeneration(
                "CDDL_GENERATION_NO_FINITE_VALUE",
                "No finite unordered group branch is available.",
                node
            );
            return false;
        }
        int start = Core.SelectChoiceStart(branches.Count);
        int attempt = 0;
        int originalCount = entries.Count;
        while (attempt < branches.Count)
        {
            if (!State.TakeAttempt(node))
            {
                return false;
            }
            RestoreMapCount(entries, originalCount);
            State.ClearFailure();
            int index = (start + attempt) % branches.Count;
            if (GenerateMapGroup(
                node.Children[branches[index]],
                entries
            ))
            {
                return true;
            }
            if (State.RemainingSteps <= 0
                || (attempt + 1 < branches.Count
                    && !State.HasAttemptBudget(node)))
            {
                return false;
            }
            attempt = attempt + 1;
        }
        RestoreMapCount(entries, originalCount);
        State.FailGeneration(
            "CDDL_GENERATION_GROUP_CHOICE_FAILED",
            "No unordered group-choice alternative could be generated.",
            node
        );
        return false;
    }

    internal bool GenerateMapEntryOccurrences(
        CddlNode entry,
        List<CborMapEntry> entries)
    {
        if (entry.Children.Count == 0)
        {
            State.FailGeneration(
                "CDDL_GENERATION_GROUP_ENTRY_INVALID",
                "The unordered group entry has no value expression.",
                entry
            );
            return false;
        }
        CddlNode valueNode = entry.Children[entry.Children.Count - 1];
        bool keyed = entry.Name != null || entry.Children.Count == 2;
        bool included = !keyed && IsGroupProducingNode(valueNode);
        if (!keyed && !included)
        {
            State.FailGeneration(
                "CDDL_GENERATION_MAP_ENTRY_INVALID",
                "A generated map member requires a key or included group.",
                entry
            );
            return false;
        }
        int count = SelectOccurrenceCount(entry, entries.Count, included);
        if (count < 0)
        {
            return false;
        }
        int occurrence = 0;
        while (occurrence < count)
        {
            int before = entries.Count;
            if (included)
            {
                if (!GenerateIncludedMapGroup(valueNode, entries))
                {
                    RestoreMapCount(entries, before);
                    if (CanUseCompletedOccurrences(entry, occurrence))
                    {
                        State.ClearFailure();
                        return true;
                    }
                    return false;
                }
            }
            else
            {
                if (!EnsureMapCapacity(entries.Count + 1, entry))
                {
                    return false;
                }
                CborValue? key;
                if (entry.Name != null)
                {
                    key = new CborValue();
                    key.Kind = CborValueKind.TextString;
                    key.TextStringValue = entry.Name;
                    key.LengthKind = CborLengthKind.Definite;
                }
                else
                {
                    key = Core.GenerateNodeValue(entry.Children[0]);
                }
                CborValue? value = Core.GenerateNodeValue(valueNode);
                if (key == null || value == null)
                {
                    RestoreMapCount(entries, before);
                    if (CanUseCompletedOccurrences(entry, occurrence))
                    {
                        State.ClearFailure();
                        return true;
                    }
                    return false;
                }
                CborMapEntry mapEntry = new CborMapEntry();
                mapEntry.Key = key;
                mapEntry.Value = value;
                entries.Add(mapEntry);
            }
            if (entries.Count > State.Options.MaxCollectionItems)
            {
                State.FailGeneration(
                    "CDDL_GENERATION_COLLECTION_LIMIT",
                    "The generated map exceeds the configured collection-item limit.",
                    entry
                );
                return false;
            }
            occurrence = occurrence + 1;
            if (included && entries.Count == before
                && occurrence >= entry.MinimumOccurrence)
            {
                occurrence = count;
            }
        }
        return true;
    }

    internal bool GenerateIncludedMapGroup(
        CddlNode node,
        List<CborMapEntry> entries)
    {
        if (IsDirectGroupNode(node))
        {
            return GenerateMapGroup(node, entries);
        }
        if (node.Kind == CddlNodeKind.Unwrap)
        {
            return GenerateUnwrappedMapGroup(node, entries);
        }
        CddlRule? rule = State.FindReferencedRule(node);
        if (rule == null)
        {
            if (IsOptionalGroupSocket(node.Name))
            {
                return true;
            }
            State.FailGeneration(
                "CDDL_GENERATION_GROUP_REFERENCE_INVALID",
                "The included map group is unavailable.",
                node
            );
            return false;
        }
        if (rule.Kind != CddlRuleKind.Group || rule.Value == null)
        {
            State.FailGeneration(
                "CDDL_GENERATION_GROUP_REFERENCE_INVALID",
                "The included map rule is not a complete group rule.",
                node
            );
            return false;
        }
        bool recursive = Core.ContainsActiveRule(rule);
        if (recursive && !Core.Productivity.CanGenerateRuleValue(rule))
        {
            State.FailGeneration(
                "CDDL_GENERATION_NO_FINITE_VALUE",
                "The recursive map group cannot produce a finite value.",
                node
            );
            return false;
        }
        bool productiveOnly = recursive
            && Core.ShouldForceProductiveRule(rule);
        if (productiveOnly)
        {
            State.ProductiveOnlyDepth = State.ProductiveOnlyDepth + 1;
        }
        Core.ActiveRules.Add(rule);
        bool generated = GenerateMapGroup(rule.Value, entries);
        Core.ActiveRules.RemoveAt(Core.ActiveRules.Count - 1);
        if (productiveOnly)
        {
            State.ProductiveOnlyDepth = State.ProductiveOnlyDepth - 1;
        }
        return generated;
    }

    internal bool GenerateUnwrappedMapGroup(
        CddlNode node,
        List<CborMapEntry> entries)
    {
        CddlRule? rule = State.FindReferencedRule(node);
        if (rule == null || rule.Value == null)
        {
            State.FailGeneration(
                "CDDL_GENERATION_UNWRAP_INVALID",
                "The unwrapped map target is unavailable.",
                node
            );
            return false;
        }
        CddlNode? content = FindUnwrappedContent(
            rule.Value,
            new List<CddlRule>()
        );
        if (content == null || !IsDirectGroupNode(content))
        {
            State.FailGeneration(
                "CDDL_GENERATION_UNWRAP_INVALID",
                "The unwrap target does not expose a map group.",
                node
            );
            return false;
        }
        bool recursive = Core.ContainsActiveRule(rule);
        if (recursive && !Core.Productivity.CanGenerateRuleValue(rule))
        {
            State.FailGeneration(
                "CDDL_GENERATION_NO_FINITE_VALUE",
                "The recursive unwrap cannot produce a finite value.",
                node
            );
            return false;
        }
        bool productiveOnly = recursive
            && Core.ShouldForceProductiveRule(rule);
        if (productiveOnly)
        {
            State.ProductiveOnlyDepth = State.ProductiveOnlyDepth + 1;
        }
        Core.ActiveRules.Add(rule);
        bool generated = GenerateMapGroup(content, entries);
        Core.ActiveRules.RemoveAt(Core.ActiveRules.Count - 1);
        if (productiveOnly)
        {
            State.ProductiveOnlyDepth = State.ProductiveOnlyDepth - 1;
        }
        return generated;
    }

    internal int SelectOccurrenceCount(
        CddlNode entry,
        int collectionCount,
        bool included)
    {
        int maximum = entry.MaximumOccurrence;
        if (maximum < 0 || maximum > State.Options.MaxCollectionItems)
        {
            maximum = State.Options.MaxCollectionItems;
        }
        if (!included)
        {
            int remaining = State.Options.MaxCollectionItems
                - collectionCount;
            if (maximum > remaining)
            {
                maximum = remaining;
            }
        }
        if (entry.MinimumOccurrence > maximum)
        {
            State.FailGeneration(
                "CDDL_GENERATION_COLLECTION_LIMIT",
                "The required occurrence count exceeds the configured collection-item limit.",
                entry
            );
            return -1;
        }
        if (State.ProductiveOnlyDepth > 0
            || State.Options.Strategy == CddlGenerationStrategy.Minimal)
        {
            return entry.MinimumOccurrence;
        }
        if (State.Options.Strategy == CddlGenerationStrategy.Boundary)
        {
            return State.NextIndex(2) == 0
                ? entry.MinimumOccurrence
                : maximum;
        }
        return entry.MinimumOccurrence + State.NextIndex(
            maximum - entry.MinimumOccurrence + 1
        );
    }

    internal bool CanUseCompletedOccurrences(
        CddlNode entry,
        int completed)
    {
        if (completed < entry.MinimumOccurrence)
        {
            return false;
        }
        string? code = State.Result.ErrorCode;
        return code == "CDDL_GENERATION_DEPTH_LIMIT"
            || code == "CDDL_GENERATION_COLLECTION_LIMIT"
            || code == "CDDL_GENERATION_NO_FINITE_VALUE";
    }

    internal bool EnsureArrayCapacity(int count, CddlNode node)
    {
        if (count <= State.Options.MaxCollectionItems)
        {
            return true;
        }
        State.FailGeneration(
            "CDDL_GENERATION_COLLECTION_LIMIT",
            "The generated array exceeds the configured collection-item limit.",
            node
        );
        return false;
    }

    internal bool EnsureMapCapacity(int count, CddlNode node)
    {
        if (count <= State.Options.MaxCollectionItems)
        {
            return true;
        }
        State.FailGeneration(
            "CDDL_GENERATION_COLLECTION_LIMIT",
            "The generated map exceeds the configured collection-item limit.",
            node
        );
        return false;
    }

    internal void RestoreArrayCount(
        List<CborValue> items,
        int count)
    {
        while (items.Count > count)
        {
            items.RemoveAt(items.Count - 1);
        }
    }

    internal void RestoreMapCount(
        List<CborMapEntry> entries,
        int count)
    {
        while (entries.Count > count)
        {
            entries.RemoveAt(entries.Count - 1);
        }
    }

    internal bool IsGroupProducingNode(CddlNode node)
    {
        if (IsDirectGroupNode(node) || node.Kind == CddlNodeKind.Unwrap)
        {
            return true;
        }
        if (node.Kind != CddlNodeKind.RuleReference)
        {
            return false;
        }
        CddlRule? rule = State.FindReferencedRule(node);
        if (rule != null)
        {
            return rule.Kind == CddlRuleKind.Group;
        }
        return IsOptionalGroupSocket(node.Name);
    }

    internal bool IsDirectGroupNode(CddlNode node)
    {
        return node.Kind == CddlNodeKind.Group
            || node.Kind == CddlNodeKind.GroupChoice
            || node.Kind == CddlNodeKind.GroupEntry
            || node.Kind == CddlNodeKind.ParenthesizedGroup;
    }

    internal bool IsOptionalGroupSocket(string? name)
    {
        return name != null && name.Length > 1
            && name[0] == '$' && name[1] == '$';
    }

    internal CddlNode? FindUnwrappedContent(
        CddlNode node,
        List<CddlRule> visited)
    {
        if (node.Kind == CddlNodeKind.RuleReference && node.Name != null)
        {
            CddlRule? rule = State.FindReferencedRule(node);
            if (rule == null || rule.Value == null
                || Core.ContainsVisitedRule(visited, rule))
            {
                return null;
            }
            visited.Add(rule);
            return FindUnwrappedContent(rule.Value, visited);
        }
        if (node.Kind == CddlNodeKind.Tag && node.Children.Count > 0)
        {
            return node.Children[node.Children.Count - 1];
        }
        if ((node.Kind == CddlNodeKind.Array
                || node.Kind == CddlNodeKind.Map)
            && node.Children.Count == 1)
        {
            return node.Children[0];
        }
        return null;
    }

    internal void CollectEnumerationCandidates(
        CddlNode node,
        List<CddlNode> candidates,
        List<CddlRule> visited)
    {
        if (node.Kind == CddlNodeKind.ParenthesizedGroup
            && node.Children.Count == 1)
        {
            CollectEnumerationCandidates(
                node.Children[0],
                candidates,
                visited
            );
            return;
        }
        if (node.Kind == CddlNodeKind.Group
            || node.Kind == CddlNodeKind.GroupChoice)
        {
            int index = 0;
            while (index < node.Children.Count)
            {
                CollectEnumerationCandidates(
                    node.Children[index],
                    candidates,
                    visited
                );
                index = index + 1;
            }
            return;
        }
        if (node.Kind == CddlNodeKind.GroupEntry)
        {
            if (node.Children.Count > 0)
            {
                CollectEnumerationEntry(
                    node.Children[node.Children.Count - 1],
                    candidates,
                    visited
                );
            }
            return;
        }
        CollectEnumerationEntry(node, candidates, visited);
    }

    internal void CollectEnumerationEntry(
        CddlNode node,
        List<CddlNode> candidates,
        List<CddlRule> visited)
    {
        if (IsDirectGroupNode(node))
        {
            CollectEnumerationCandidates(node, candidates, visited);
            return;
        }
        if (node.Kind == CddlNodeKind.RuleReference && node.Name != null)
        {
            CddlRule? rule = State.FindReferencedRule(node);
            if (rule != null && rule.Kind == CddlRuleKind.Group
                && rule.Value != null
                && !Core.ContainsVisitedRule(visited, rule))
            {
                visited.Add(rule);
                CollectEnumerationCandidates(
                    rule.Value,
                    candidates,
                    visited
                );
                visited.RemoveAt(visited.Count - 1);
                return;
            }
        }
        candidates.Add(node);
    }
}
