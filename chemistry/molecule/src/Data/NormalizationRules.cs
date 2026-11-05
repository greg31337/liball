using System.Collections.Generic;

namespace molecule;

internal class NormalizationRules
{
    internal const string ChargeCategory = "charge";
    internal const string AcidBaseCategory = "acid-base";

    internal List<ChargeNormalizationRule> ChargeRules;
    internal List<AcidBaseRule> AcidBaseRules;

    internal NormalizationRules()
    {
        ChargeRules = new List<ChargeNormalizationRule>();
        AcidBaseRules = new List<AcidBaseRule>();
        RegisterDefaults();
    }

    internal void RegisterDefaults()
    {
        ChargeRules.Clear();
        AcidBaseRules.Clear();
        AddChargeRule("nitro", 10, "N(=O)=O", "[N+](=O)[O-]", "Normalize neutral nitro groups to charge-separated nitro.", 7, 8);
        AddChargeRule("n-oxide", 20, "N->O", "[N+][O-]", "Normalize amine oxide notation to charge-separated N-oxide.", 7, 8);
        AddChargeRule("sulfoxide", 30, "S(=O)", "[S+][O-]", "Normalize hypervalent sulfoxide notation to charge-separated sulfoxide.", 16, 8);
        AddChargeRule("phosphate", 40, "P(=O)(O)(O)O", "P(=O)([O-])([O-])[O-]", "Normalize phosphate oxygens to an anionic representation.", 15, 8);
        AddChargeRule("carboxylate", 50, "C(=O)[O-]", "C(=O)[O-]", "Preserve carboxylate groups in charge-separated form.", 6, 8);
        AddChargeRule("azide", 60, "N=N=N", "[N-]=[N+]=N", "Normalize azide groups to charge-separated form.", 7, 7);
        AddChargeRule("diazo", 70, "C=N=N", "[C-]=[N+]=N", "Normalize diazo groups to charge-separated form.", 6, 7);
        AddChargeRule("amino-acid-zwitterion", 80, "NCC(=O)O", "[NH3+]CC(=O)[O-]", "Normalize simple amino acid zwitterion form.", 7, 8);
        AddAcidBaseRule("sulfonic-acid", 10, 8, 16, true, "Sulfonic acid oxygen.");
        AddAcidBaseRule("phosphoric-acid", 20, 8, 15, true, "Phosphoric acid oxygen.");
        AddAcidBaseRule("carboxylic-acid", 30, 8, 6, true, "Carboxylic acid oxygen.");
        AddAcidBaseRule("phenol", 60, 8, 6, false, "Phenolic oxygen.");
        AddAcidBaseRule("thiol", 70, 16, 6, false, "Thiol sulfur.");
        AddAcidBaseRule("alcohol", 80, 8, 6, false, "Alcohol oxygen.");
        AddAcidBaseRule("ammonium", 90, 7, 6, false, "Ammonium nitrogen.");
    }

    internal void AddChargeRule(string name, int priority, string reactantPattern, string productPattern, string description, int positiveAtomicNumber, int negativeAtomicNumber)
    {
        ChargeNormalizationRule rule = new ChargeNormalizationRule();
        rule.Name = NormalizeString(name);
        rule.Category = ChargeCategory;
        rule.Priority = priority;
        rule.ReactantPattern = NormalizeString(reactantPattern);
        rule.ProductPattern = NormalizeString(productPattern);
        rule.Description = NormalizeString(description);
        rule.PositiveAtomicNumber = positiveAtomicNumber;
        rule.NegativeAtomicNumber = negativeAtomicNumber;
        ChargeRules.Add(rule);
    }

    internal void AddAcidBaseRule(string name, int priority, int acidAtomicNumber, int neighborAtomicNumber, bool requiresDoubleOxygenNeighbor, string description)
    {
        AcidBaseRule rule = new AcidBaseRule();
        rule.Name = NormalizeString(name);
        rule.Category = AcidBaseCategory;
        rule.Priority = priority;
        rule.AcidAtomicNumber = acidAtomicNumber;
        rule.NeighborAtomicNumber = neighborAtomicNumber;
        rule.RequiresDoubleOxygenNeighbor = requiresDoubleOxygenNeighbor;
        rule.Description = NormalizeString(description);
        AcidBaseRules.Add(rule);
    }

    internal List<ChargeNormalizationRule> GetChargeRules()
    {
        List<ChargeNormalizationRule> rules = new List<ChargeNormalizationRule>();
        int index = 0;
        while (index < ChargeRules.Count)
        {
            rules.Add(ChargeRules[index]);
            index = index + 1;
        }

        return rules;
    }

    internal List<ChargeNormalizationRule> GetOrderedChargeRules()
    {
        List<ChargeNormalizationRule> rules = GetChargeRules();
        SortChargeRules(rules);
        return rules;
    }

    internal List<AcidBaseRule> GetAcidBaseRules()
    {
        List<AcidBaseRule> rules = new List<AcidBaseRule>();
        int index = 0;
        while (index < AcidBaseRules.Count)
        {
            rules.Add(AcidBaseRules[index]);
            index = index + 1;
        }

        return rules;
    }

    internal List<AcidBaseRule> GetOrderedAcidBaseRules()
    {
        List<AcidBaseRule> rules = GetAcidBaseRules();
        SortAcidBaseRules(rules);
        return rules;
    }

    internal List<ChargeNormalizationRule> GetRulesByCategory(string category)
    {
        string normalized = NormalizeKey(category);
        if (normalized != ChargeCategory)
        {
            return new List<ChargeNormalizationRule>();
        }

        return GetChargeRules();
    }

    internal List<AcidBaseRule> GetAcidBaseRulesByCategory(string category)
    {
        string normalized = NormalizeKey(category);
        if (normalized != AcidBaseCategory)
        {
            return new List<AcidBaseRule>();
        }

        return GetAcidBaseRules();
    }

    internal ChargeNormalizationRule FindChargeRule(string name)
    {
        string normalized = NormalizeKey(name);
        int index = 0;
        while (index < ChargeRules.Count)
        {
            if (NormalizeKey(ChargeRules[index].Name) == normalized)
            {
                return ChargeRules[index];
            }

            index = index + 1;
        }

        return new ChargeNormalizationRule();
    }

    internal bool ContainsChargeRule(string name)
    {
        return FindChargeRule(name).Name.Length > 0;
    }

    internal AcidBaseRule FindAcidBaseRule(string name)
    {
        string normalized = NormalizeKey(name);
        int index = 0;
        while (index < AcidBaseRules.Count)
        {
            if (NormalizeKey(AcidBaseRules[index].Name) == normalized)
            {
                return AcidBaseRules[index];
            }

            index = index + 1;
        }

        return new AcidBaseRule();
    }

    internal bool ContainsAcidBaseRule(string name)
    {
        return FindAcidBaseRule(name).Name.Length > 0;
    }

    internal void SortChargeRules(List<ChargeNormalizationRule> rules)
    {
        int index = 1;
        while (index < rules.Count)
        {
            ChargeNormalizationRule value = rules[index];
            int scan = index - 1;

            while (scan >= 0 && CompareChargeRules(rules[scan], value) > 0)
            {
                rules[scan + 1] = rules[scan];
                scan = scan - 1;
            }

            rules[scan + 1] = value;
            index = index + 1;
        }
    }

    internal int CompareChargeRules(ChargeNormalizationRule left, ChargeNormalizationRule right)
    {
        if (left.Priority != right.Priority)
        {
            return left.Priority - right.Priority;
        }

        return string.CompareOrdinal(left.Name, right.Name);
    }

    internal void SortAcidBaseRules(List<AcidBaseRule> rules)
    {
        int index = 1;
        while (index < rules.Count)
        {
            AcidBaseRule value = rules[index];
            int scan = index - 1;

            while (scan >= 0 && CompareAcidBaseRules(rules[scan], value) > 0)
            {
                rules[scan + 1] = rules[scan];
                scan = scan - 1;
            }

            rules[scan + 1] = value;
            index = index + 1;
        }
    }

    internal int CompareAcidBaseRules(AcidBaseRule left, AcidBaseRule right)
    {
        if (left.Priority != right.Priority)
        {
            return left.Priority - right.Priority;
        }

        return string.CompareOrdinal(left.Name, right.Name);
    }

    internal string NormalizeKey(string value)
    {
        return NormalizeString(value).Trim().ToLowerInvariant();
    }

    internal string NormalizeString(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }
}

internal class ChargeNormalizationRule
{
    internal string Name;
    internal string Category;
    internal int Priority;
    internal string ReactantPattern;
    internal string ProductPattern;
    internal string Description;
    internal int PositiveAtomicNumber;
    internal int NegativeAtomicNumber;

    internal ChargeNormalizationRule()
    {
        Name = "";
        Category = "";
        Priority = 0;
        ReactantPattern = "";
        ProductPattern = "";
        Description = "";
        PositiveAtomicNumber = 0;
        NegativeAtomicNumber = 0;
    }
}

internal class AcidBaseRule
{
    internal string Name;
    internal string Category;
    internal int Priority;
    internal int AcidAtomicNumber;
    internal int NeighborAtomicNumber;
    internal bool RequiresDoubleOxygenNeighbor;
    internal string Description;

    internal AcidBaseRule()
    {
        Name = "";
        Category = "";
        Priority = 0;
        AcidAtomicNumber = 0;
        NeighborAtomicNumber = 0;
        RequiresDoubleOxygenNeighbor = false;
        Description = "";
    }
}
