using System.Collections.Generic;

namespace cbor;

internal class CddlPreludeLoader
{
    internal const string PreludeSourceName = "<standard-prelude>";

    internal CddlRule[] LoadRules(
        CddlDocument document,
        CddlCompileOptions options)
    {
        List<CddlRule> rules = new List<CddlRule>();
        int sourceIndex = 0;
        while (sourceIndex < document.Rules.Count)
        {
            rules.Add(document.Rules[sourceIndex]);
            sourceIndex = sourceIndex + 1;
        }
        if (!options.IncludeStandardPrelude)
        {
            return rules.ToArray();
        }

        CddlDocument prelude = ParsePreludeDocument();
        int preludeIndex = 0;
        while (preludeIndex < prelude.Rules.Count)
        {
            CddlRule rule = prelude.Rules[preludeIndex];
            if (!HasSourceBaseDefinition(document, rule.Name))
            {
                rule.IsPrelude = true;
                rules.Add(rule);
            }
            preludeIndex = preludeIndex + 1;
        }
        return rules.ToArray();
    }

    internal CddlDocument ParsePreludeDocument()
    {
        CddlParseOptions options = new CddlParseOptions();
        options.SourceName = PreludeSourceName;
        options.Recover = false;
        return new CddlParser().ParseDocument(
            CddlPrelude.Standard,
            options
        );
    }

    internal bool HasSourceBaseDefinition(CddlDocument document, string name)
    {
        int index = 0;
        while (index < document.Rules.Count)
        {
            CddlRule sourceRule = document.Rules[index];
            if (!sourceRule.IsPrelude
                && sourceRule.Name == name
                && (sourceRule.Kind == CddlRuleKind.Type
                    || sourceRule.Kind == CddlRuleKind.Group))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }
}
