using System.Collections.Generic;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class TautomerRulesTests
{
    [Test]
    public void Constructor_LoadsBuiltInTautomerTransformRules()
    {
        TautomerRules rules = new TautomerRules();

        Assert.That(rules.TransformRules.Count, Is.GreaterThanOrEqualTo(10));
        Assert.That(rules.ContainsTransformRule("keto-enol"), Is.True);
        Assert.That(rules.ContainsTransformRule("imine-enamine"), Is.True);
        Assert.That(rules.ContainsTransformRule("amide-imidic-acid"), Is.True);
        Assert.That(rules.ContainsTransformRule("thio-keto-enol"), Is.True);
        Assert.That(rules.ContainsTransformRule("nitroso-oxime"), Is.True);
        Assert.That(rules.ContainsTransformRule("azo-hydrazone"), Is.True);
        Assert.That(rules.ContainsTransformRule("phosphonate"), Is.True);
        Assert.That(rules.ContainsTransformRule("heteroaromatic-proton-shift"), Is.True);
    }

    [Test]
    public void FindTransformRule_ReturnsPatternsAtomRolesAndBondOrders()
    {
        TautomerRules rules = new TautomerRules();

        TautomerTransformRule ketoEnol = rules.FindTransformRule(" Keto-Enol ");

        Assert.That(ketoEnol.Name, Is.EqualTo("keto-enol"));
        Assert.That(ketoEnol.Category, Is.EqualTo(TautomerRules.TransformCategory));
        Assert.That(ketoEnol.Priority, Is.EqualTo(10));
        Assert.That(ketoEnol.ReactantPattern, Is.EqualTo("O-C=C"));
        Assert.That(ketoEnol.ProductPattern, Is.EqualTo("O=C-C"));
        Assert.That(ketoEnol.DonorAtomicNumber, Is.EqualTo(8));
        Assert.That(ketoEnol.PivotAtomicNumber, Is.EqualTo(6));
        Assert.That(ketoEnol.AcceptorAtomicNumber, Is.EqualTo(6));
        Assert.That(ketoEnol.DonorBondOrder, Is.EqualTo(BondOrder.Single));
        Assert.That(ketoEnol.AcceptorBondOrder, Is.EqualTo(BondOrder.Double));
        Assert.That(ketoEnol.ProductDonorBondOrder, Is.EqualTo(BondOrder.Double));
        Assert.That(ketoEnol.ProductAcceptorBondOrder, Is.EqualTo(BondOrder.Single));
        Assert.That(ketoEnol.IsReversible, Is.True);
    }

    [Test]
    public void FindTransformRule_ReturnsEmptyRecordForMissingRule()
    {
        TautomerRules rules = new TautomerRules();

        TautomerTransformRule missing = rules.FindTransformRule("missing");

        Assert.That(missing.Name, Is.EqualTo(""));
        Assert.That(missing.ReactantPattern, Is.EqualTo(""));
        Assert.That(rules.ContainsTransformRule("missing"), Is.False);
    }

    [Test]
    public void GetRulesByCategory_ReturnsTransformRulesOnlyForTransformCategory()
    {
        TautomerRules rules = new TautomerRules();

        List<TautomerTransformRule> transformRules = rules.GetRulesByCategory(" Tautomer-Transform ");
        List<TautomerTransformRule> unknownRules = rules.GetRulesByCategory("unknown");

        Assert.That(TautomerRules.TransformCategory, Is.EqualTo("tautomer-transform"));
        Assert.That(transformRules.Count, Is.EqualTo(rules.TransformRules.Count));
        Assert.That(unknownRules.Count, Is.EqualTo(0));
        Assert.That(AllTransformRules(transformRules), Is.True);
    }

    [Test]
    public void GetOrderedTransformRules_SortsByPriorityThenName()
    {
        TautomerRules rules = new TautomerRules();
        rules.AddTransformRule("early-b", 5, "N-C=O", "N=C-O", 7, 6, 8, BondOrder.Single, BondOrder.Double, BondOrder.Double, BondOrder.Single, true, "test");
        rules.AddTransformRule("early-a", 5, "O-C=N", "O=C-N", 8, 6, 7, BondOrder.Single, BondOrder.Double, BondOrder.Double, BondOrder.Single, true, "test");

        List<TautomerTransformRule> ordered = rules.GetOrderedTransformRules();

        Assert.That(ordered[0].Name, Is.EqualTo("early-a"));
        Assert.That(ordered[1].Name, Is.EqualTo("early-b"));
        Assert.That(ordered[2].Name, Is.EqualTo("keto-enol"));
        Assert.That(IsSortedByPriorityThenName(ordered), Is.True);
        Assert.That(rules.TransformRules[0].Name, Is.EqualTo("keto-enol"));
    }

    [Test]
    public void GetTransformRules_ReturnsCopyOfRuleList()
    {
        TautomerRules rules = new TautomerRules();

        List<TautomerTransformRule> copy = rules.GetTransformRules();
        copy.Clear();

        Assert.That(copy.Count, Is.EqualTo(0));
        Assert.That(rules.TransformRules.Count, Is.GreaterThan(0));
    }

    [Test]
    public void GetRulesForDonorAndAcceptor_FilterByAtomRoles()
    {
        TautomerRules rules = new TautomerRules();

        List<TautomerTransformRule> oxygenDonorRules = rules.GetRulesForDonor(8);
        List<TautomerTransformRule> nitrogenAcceptorRules = rules.GetRulesForAcceptor(7);

        Assert.That(oxygenDonorRules.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(nitrogenAcceptorRules.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(AllRulesHaveDonor(oxygenDonorRules, 8), Is.True);
        Assert.That(AllRulesHaveAcceptor(nitrogenAcceptorRules, 7), Is.True);
    }

    static bool AllTransformRules(List<TautomerTransformRule> rules)
    {
        int index = 0;
        while (index < rules.Count)
        {
            if (rules[index].Category != TautomerRules.TransformCategory)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsSortedByPriorityThenName(List<TautomerTransformRule> rules)
    {
        int index = 1;
        while (index < rules.Count)
        {
            if (rules[index - 1].Priority > rules[index].Priority)
            {
                return false;
            }

            if (rules[index - 1].Priority == rules[index].Priority && string.CompareOrdinal(rules[index - 1].Name, rules[index].Name) > 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool AllRulesHaveDonor(List<TautomerTransformRule> rules, int atomicNumber)
    {
        int index = 0;
        while (index < rules.Count)
        {
            if (rules[index].DonorAtomicNumber != atomicNumber)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool AllRulesHaveAcceptor(List<TautomerTransformRule> rules, int atomicNumber)
    {
        int index = 0;
        while (index < rules.Count)
        {
            if (rules[index].AcceptorAtomicNumber != atomicNumber)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
