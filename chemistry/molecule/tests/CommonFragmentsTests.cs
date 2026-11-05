using System.Collections.Generic;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class CommonFragmentsTests
{
    [Test]
    public void Constructor_LoadsBuiltInSaltsSolventsAndCounterions()
    {
        CommonFragments fragments = new CommonFragments();

        Assert.That(fragments.Fragments.Count, Is.GreaterThanOrEqualTo(30));
        Assert.That(fragments.ContainsName("chloride"), Is.True);
        Assert.That(fragments.ContainsName("water"), Is.True);
        Assert.That(fragments.ContainsName("trifluoroacetate"), Is.True);
        Assert.That(fragments.ContainsName("dimethyl sulfoxide"), Is.True);
        Assert.That(fragments.ContainsSmiles("[Cl-]"), Is.True);
        Assert.That(fragments.ContainsSmiles("CC#N"), Is.True);
    }

    [Test]
    public void FindByName_ReturnsRecordWithFormulaSmilesAndFlags()
    {
        CommonFragments fragments = new CommonFragments();

        CommonFragmentRecord chloride = fragments.FindByName(" Chloride ");
        CommonFragmentRecord ethanol = fragments.FindByName("ETHANOL");

        Assert.That(chloride.Name, Is.EqualTo("chloride"));
        Assert.That(chloride.Smiles, Is.EqualTo("[Cl-]"));
        Assert.That(chloride.Formula, Is.EqualTo("Cl"));
        Assert.That(chloride.IsSalt, Is.True);
        Assert.That(chloride.IsCounterion, Is.True);
        Assert.That(chloride.IsSolvent, Is.False);
        Assert.That(ethanol.Name, Is.EqualTo("ethanol"));
        Assert.That(ethanol.Smiles, Is.EqualTo("CCO"));
        Assert.That(ethanol.Formula, Is.EqualTo("C2H6O"));
        Assert.That(ethanol.IsSolvent, Is.True);
    }

    [Test]
    public void FindBySmiles_ReturnsExactSmilesMatch()
    {
        CommonFragments fragments = new CommonFragments();

        CommonFragmentRecord acetate = fragments.FindBySmiles("CC(=O)[O-]");
        CommonFragmentRecord missing = fragments.FindBySmiles("not-present");

        Assert.That(acetate.Name, Is.EqualTo("acetate"));
        Assert.That(acetate.IsSalt, Is.True);
        Assert.That(acetate.IsCounterion, Is.True);
        Assert.That(missing.Name, Is.EqualTo(""));
    }

    [Test]
    public void CategoryFilters_ReturnOnlyRequestedRecords()
    {
        CommonFragments fragments = new CommonFragments();

        List<CommonFragmentRecord> salts = fragments.GetSalts();
        List<CommonFragmentRecord> solvents = fragments.GetSolvents();
        List<CommonFragmentRecord> counterions = fragments.GetCounterions();

        Assert.That(salts.Count, Is.GreaterThan(0));
        Assert.That(solvents.Count, Is.GreaterThan(0));
        Assert.That(counterions.Count, Is.GreaterThan(0));
        Assert.That(AllSalts(salts), Is.True);
        Assert.That(AllSolvents(solvents), Is.True);
        Assert.That(AllCounterions(counterions), Is.True);
        Assert.That(ContainsRecordName(salts, "chloride"), Is.True);
        Assert.That(ContainsRecordName(solvents, "water"), Is.True);
        Assert.That(ContainsRecordName(counterions, "sodium"), Is.True);
    }

    [Test]
    public void GetByCategory_UsesStableCategoryNames()
    {
        CommonFragments fragments = new CommonFragments();

        Assert.That(CommonFragments.SaltCategory, Is.EqualTo("salt"));
        Assert.That(CommonFragments.SolventCategory, Is.EqualTo("solvent"));
        Assert.That(CommonFragments.CounterionCategory, Is.EqualTo("counterion"));
        Assert.That(fragments.GetByCategory(" SALT ").Count, Is.EqualTo(fragments.GetSalts().Count));
        Assert.That(fragments.GetByCategory("solvent").Count, Is.EqualTo(fragments.GetSolvents().Count));
        Assert.That(fragments.GetByCategory("counterion").Count, Is.EqualTo(fragments.GetCounterions().Count));
        Assert.That(fragments.GetByCategory("unknown").Count, Is.EqualTo(0));
    }

    [Test]
    public void NameHelpers_ReturnCategoryMembership()
    {
        CommonFragments fragments = new CommonFragments();

        Assert.That(fragments.IsSaltName("tosylate"), Is.True);
        Assert.That(fragments.IsCounterionName("tosylate"), Is.True);
        Assert.That(fragments.IsSolventName("tosylate"), Is.False);
        Assert.That(fragments.IsSolventName("acetonitrile"), Is.True);
        Assert.That(fragments.IsSaltName("acetonitrile"), Is.False);
        Assert.That(fragments.IsSaltName("missing"), Is.False);
    }

    static bool AllSalts(List<CommonFragmentRecord> records)
    {
        int index = 0;
        while (index < records.Count)
        {
            if (!records[index].IsSalt)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool AllSolvents(List<CommonFragmentRecord> records)
    {
        int index = 0;
        while (index < records.Count)
        {
            if (!records[index].IsSolvent)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool AllCounterions(List<CommonFragmentRecord> records)
    {
        int index = 0;
        while (index < records.Count)
        {
            if (!records[index].IsCounterion)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool ContainsRecordName(List<CommonFragmentRecord> records, string name)
    {
        int index = 0;
        while (index < records.Count)
        {
            if (records[index].Name == name)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
