using System.Collections.Generic;

namespace cbor;

internal class CddlExtensionMerger
{
    internal CddlRule MergeEntry(CddlRuleTableEntry entry)
    {
        CddlRule first = entry.Definitions[0];
        CddlRule merged = new CddlRule();
        merged.Name = entry.Name;
        merged.Kind = entry.Kind;
        merged.SourceOrder = first.SourceOrder;
        merged.Span = first.Span;
        merged.IsPrelude = true;
        int preludeIndex = 0;
        while (preludeIndex < entry.Definitions.Count)
        {
            if (!entry.Definitions[preludeIndex].IsPrelude)
            {
                merged.IsPrelude = false;
            }
            preludeIndex = preludeIndex + 1;
        }
        CddlRule parameterSource = entry.BaseRule ?? first;
        CopyGenericParameters(parameterSource, merged);
        if (entry.Kind == CddlRuleKind.Type)
        {
            merged.Value = CreateTypeValue(entry.Definitions);
        }
        else
        {
            merged.Value = CreateGroupValue(entry.Definitions);
        }
        return merged;
    }

    internal CddlNode CreateTypeValue(List<CddlRule> definitions)
    {
        if (definitions.Count == 1)
        {
            return definitions[0].Value ?? new CddlNode();
        }
        CddlNode choice = new CddlNode();
        choice.Kind = CddlNodeKind.TypeChoice;
        choice.Span = definitions[0].Value?.Span ?? definitions[0].Span;
        int index = 0;
        while (index < definitions.Count)
        {
            CddlNode value = definitions[index].Value ?? new CddlNode();
            AddTypeAlternatives(value, choice);
            index = index + 1;
        }
        return choice;
    }

    internal void AddTypeAlternatives(CddlNode value, CddlNode choice)
    {
        if (value.Kind == CddlNodeKind.TypeChoice)
        {
            int index = 0;
            while (index < value.Children.Count)
            {
                choice.Children.Add(value.Children[index]);
                index = index + 1;
            }
        }
        else
        {
            choice.Children.Add(value);
        }
    }

    internal CddlNode CreateGroupValue(List<CddlRule> definitions)
    {
        if (definitions.Count == 1)
        {
            return definitions[0].Value ?? new CddlNode();
        }
        CddlNode choice = new CddlNode();
        choice.Kind = CddlNodeKind.GroupChoice;
        choice.Span = definitions[0].Value?.Span ?? definitions[0].Span;
        int index = 0;
        while (index < definitions.Count)
        {
            CddlNode value = definitions[index].Value ?? new CddlNode();
            CddlNode alternative = new CddlNode();
            alternative.Kind = CddlNodeKind.Group;
            alternative.Span = value.Span;
            alternative.Children.Add(value);
            choice.Children.Add(alternative);
            index = index + 1;
        }
        return choice;
    }

    internal void CopyGenericParameters(CddlRule source, CddlRule destination)
    {
        int index = 0;
        while (index < source.GenericParameters.Count)
        {
            destination.GenericParameters.Add(source.GenericParameters[index]);
            index = index + 1;
        }
    }
}
