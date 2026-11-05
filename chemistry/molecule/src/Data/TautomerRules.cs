using System.Collections.Generic;

namespace molecule;

internal class TautomerRules
{
    internal const string TransformCategory = "tautomer-transform";

    internal List<TautomerTransformRule> TransformRules;

    internal TautomerRules()
    {
        TransformRules = new List<TautomerTransformRule>();
        RegisterDefaults();
    }

    internal void RegisterDefaults()
    {
        TransformRules.Clear();
        AddTransformRule("keto-enol", 10, "O-C=C", "O=C-C", 8, 6, 6, BondOrder.Single, BondOrder.Double, BondOrder.Double, BondOrder.Single, true, "Carbonyl and enol proton shift.");
        AddTransformRule("imine-enamine", 20, "N-C=C", "N=C-C", 7, 6, 6, BondOrder.Single, BondOrder.Double, BondOrder.Double, BondOrder.Single, true, "Imine and enamine proton shift.");
        AddTransformRule("amide-imidic-acid", 30, "O-C=N", "O=C-N", 8, 6, 7, BondOrder.Single, BondOrder.Double, BondOrder.Double, BondOrder.Single, true, "Amide and imidic acid proton shift.");
        AddTransformRule("thio-keto-enol", 40, "S-C=C", "S=C-C", 16, 6, 6, BondOrder.Single, BondOrder.Double, BondOrder.Double, BondOrder.Single, true, "Thiocarbonyl and thioenol proton shift.");
        AddTransformRule("nitroso-oxime", 50, "O-N=C", "O=N-C", 8, 7, 6, BondOrder.Single, BondOrder.Double, BondOrder.Double, BondOrder.Single, true, "Nitroso and oxime proton shift.");
        AddTransformRule("azo-hydrazone", 60, "N-N=C", "N=N-C", 7, 7, 6, BondOrder.Single, BondOrder.Double, BondOrder.Double, BondOrder.Single, true, "Azo and hydrazone proton shift.");
        AddTransformRule("amidrazone", 70, "N-C=N", "N=C-N", 7, 6, 7, BondOrder.Single, BondOrder.Double, BondOrder.Double, BondOrder.Single, true, "Amidrazone proton shift.");
        AddTransformRule("phosphonate", 80, "O-P=O", "O=P-O", 8, 15, 8, BondOrder.Single, BondOrder.Double, BondOrder.Double, BondOrder.Single, true, "Phosphonate oxygen proton shift.");
        AddTransformRule("sulfonamide", 90, "N-S=O", "N=S-O", 7, 16, 8, BondOrder.Single, BondOrder.Double, BondOrder.Double, BondOrder.Single, true, "Sulfonamide heteroatom proton shift.");
        AddTransformRule("heteroaromatic-proton-shift", 100, "N-C=N", "N=C-N", 7, 6, 7, BondOrder.Single, BondOrder.Aromatic, BondOrder.Aromatic, BondOrder.Single, true, "Heteroaromatic mobile hydrogen shift.");
    }

    internal void AddTransformRule(string name, int priority, string reactantPattern, string productPattern, int donorAtomicNumber, int pivotAtomicNumber, int acceptorAtomicNumber, BondOrder donorBondOrder, BondOrder acceptorBondOrder, BondOrder productDonorBondOrder, BondOrder productAcceptorBondOrder, bool isReversible, string description)
    {
        TautomerTransformRule rule = new TautomerTransformRule();
        rule.Name = NormalizeString(name);
        rule.Category = TransformCategory;
        rule.Priority = priority;
        rule.ReactantPattern = NormalizeString(reactantPattern);
        rule.ProductPattern = NormalizeString(productPattern);
        rule.DonorAtomicNumber = donorAtomicNumber;
        rule.PivotAtomicNumber = pivotAtomicNumber;
        rule.AcceptorAtomicNumber = acceptorAtomicNumber;
        rule.DonorBondOrder = donorBondOrder;
        rule.AcceptorBondOrder = acceptorBondOrder;
        rule.ProductDonorBondOrder = productDonorBondOrder;
        rule.ProductAcceptorBondOrder = productAcceptorBondOrder;
        rule.IsReversible = isReversible;
        rule.Description = NormalizeString(description);
        TransformRules.Add(rule);
    }

    internal List<TautomerTransformRule> GetTransformRules()
    {
        List<TautomerTransformRule> rules = new List<TautomerTransformRule>();
        int index = 0;
        while (index < TransformRules.Count)
        {
            rules.Add(TransformRules[index]);
            index = index + 1;
        }

        return rules;
    }

    internal List<TautomerTransformRule> GetOrderedTransformRules()
    {
        List<TautomerTransformRule> rules = GetTransformRules();
        SortTransformRules(rules);
        return rules;
    }

    internal List<TautomerTransformRule> GetRulesByCategory(string category)
    {
        string normalized = NormalizeKey(category);
        if (normalized != TransformCategory)
        {
            return new List<TautomerTransformRule>();
        }

        return GetTransformRules();
    }

    internal List<TautomerTransformRule> GetRulesForDonor(int donorAtomicNumber)
    {
        List<TautomerTransformRule> rules = new List<TautomerTransformRule>();
        int index = 0;
        while (index < TransformRules.Count)
        {
            if (TransformRules[index].DonorAtomicNumber == donorAtomicNumber)
            {
                rules.Add(TransformRules[index]);
            }

            index = index + 1;
        }

        return rules;
    }

    internal List<TautomerTransformRule> GetRulesForAcceptor(int acceptorAtomicNumber)
    {
        List<TautomerTransformRule> rules = new List<TautomerTransformRule>();
        int index = 0;
        while (index < TransformRules.Count)
        {
            if (TransformRules[index].AcceptorAtomicNumber == acceptorAtomicNumber)
            {
                rules.Add(TransformRules[index]);
            }

            index = index + 1;
        }

        return rules;
    }

    internal TautomerTransformRule FindTransformRule(string name)
    {
        string normalized = NormalizeKey(name);
        int index = 0;
        while (index < TransformRules.Count)
        {
            if (NormalizeKey(TransformRules[index].Name) == normalized)
            {
                return TransformRules[index];
            }

            index = index + 1;
        }

        return new TautomerTransformRule();
    }

    internal bool ContainsTransformRule(string name)
    {
        return FindTransformRule(name).Name.Length > 0;
    }

    internal void SortTransformRules(List<TautomerTransformRule> rules)
    {
        int index = 1;
        while (index < rules.Count)
        {
            TautomerTransformRule value = rules[index];
            int scan = index - 1;

            while (scan >= 0 && CompareTransformRules(rules[scan], value) > 0)
            {
                rules[scan + 1] = rules[scan];
                scan = scan - 1;
            }

            rules[scan + 1] = value;
            index = index + 1;
        }
    }

    internal int CompareTransformRules(TautomerTransformRule left, TautomerTransformRule right)
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

internal class TautomerTransformRule
{
    internal string Name;
    internal string Category;
    internal int Priority;
    internal string ReactantPattern;
    internal string ProductPattern;
    internal int DonorAtomicNumber;
    internal int PivotAtomicNumber;
    internal int AcceptorAtomicNumber;
    internal BondOrder DonorBondOrder;
    internal BondOrder AcceptorBondOrder;
    internal BondOrder ProductDonorBondOrder;
    internal BondOrder ProductAcceptorBondOrder;
    internal bool IsReversible;
    internal string Description;

    internal TautomerTransformRule()
    {
        Name = "";
        Category = "";
        Priority = 0;
        ReactantPattern = "";
        ProductPattern = "";
        DonorAtomicNumber = 0;
        PivotAtomicNumber = 0;
        AcceptorAtomicNumber = 0;
        DonorBondOrder = BondOrder.Unknown;
        AcceptorBondOrder = BondOrder.Unknown;
        ProductDonorBondOrder = BondOrder.Unknown;
        ProductAcceptorBondOrder = BondOrder.Unknown;
        IsReversible = false;
        Description = "";
    }
}
