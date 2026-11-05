using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class ElementTableTests
{
    [Test]
    public void ElementTable_ContainsAllElementsInAtomicNumberOrder()
    {
        ElementTable table = new ElementTable();

        Assert.That(table.Elements.Count, Is.EqualTo(118));
        int atomicNumber = 1;
        while (atomicNumber <= 118)
        {
            Element element = table.GetElement(atomicNumber);
            Isotope majorIsotope = table.GetMajorIsotope(atomicNumber);
            Assert.That(element.AtomicNumber, Is.EqualTo(atomicNumber));
            Assert.That(element.Symbol, Is.Not.Empty);
            Assert.That(element.Name, Is.Not.Empty);
            Assert.That(element.AverageMass, Is.GreaterThan(0.0));
            Assert.That(element.MonoisotopicMass, Is.GreaterThan(0.0));
            Assert.That(element.CovalentRadius, Is.GreaterThan(0.0));
            Assert.That(majorIsotope.AtomicNumber, Is.EqualTo(atomicNumber));
            Assert.That(element.MonoisotopicMass, Is.EqualTo(majorIsotope.ExactMass));
            Assert.That(table.GetElementBySymbol(element.Symbol).AtomicNumber, Is.EqualTo(atomicNumber));
            atomicNumber = atomicNumber + 1;
        }
    }

    [Test]
    public void ElementTable_LooksUpCommonElementsByAtomicNumberAndSymbol()
    {
        ElementTable table = new ElementTable();

        Element carbon = table.GetElement(6);
        Element chlorine = table.GetElementBySymbol("Cl");
        Element iodine = table.GetElementBySymbol("I");

        Assert.That(table.Elements.Count, Is.GreaterThanOrEqualTo(25));
        Assert.That(carbon.AtomicNumber, Is.EqualTo(6));
        Assert.That(carbon.Symbol, Is.EqualTo("C"));
        Assert.That(carbon.Name, Is.EqualTo("Carbon"));
        Assert.That(carbon.AverageMass, Is.EqualTo(12.011).Within(0.001));
        Assert.That(carbon.MonoisotopicMass, Is.EqualTo(12.0).Within(0.000001));
        Assert.That(carbon.CovalentRadius, Is.EqualTo(0.76).Within(0.001));
        Assert.That(carbon.IsOrganicSubset, Is.True);
        Assert.That(carbon.HasCommonValence(4), Is.True);
        Assert.That(carbon.HasCommonValence(2), Is.False);

        Assert.That(chlorine.AtomicNumber, Is.EqualTo(17));
        Assert.That(chlorine.IsOrganicSubset, Is.True);
        Assert.That(chlorine.HasCommonValence(7), Is.True);

        Assert.That(iodine.AtomicNumber, Is.EqualTo(53));
        Assert.That(iodine.AverageMass, Is.EqualTo(126.90447).Within(0.00001));
    }

    [Test]
    public void ElementTable_ExposesReferenceDataAcrossThePeriodicTable()
    {
        ElementTable table = new ElementTable();

        Element scandium = table.GetElement(21);
        Element uranium = table.GetElementBySymbol("U");
        Element oganesson = table.GetElement(118);

        Assert.That(scandium.Name, Is.EqualTo("Scandium"));
        Assert.That(scandium.AverageMass, Is.EqualTo(44.955908).Within(0.000001));
        Assert.That(scandium.CovalentRadius, Is.EqualTo(1.70).Within(0.001));
        Assert.That(uranium.MonoisotopicMass, Is.EqualTo(238.0507884).Within(0.0000001));
        Assert.That(oganesson.Symbol, Is.EqualTo("Og"));
        Assert.That(oganesson.AverageMass, Is.EqualTo(294.0));
        Assert.That(oganesson.CovalentRadius, Is.EqualTo(1.57).Within(0.001));
    }

    [Test]
    public void ElementTable_ReturnsEmptyRecordsForUnknownElements()
    {
        ElementTable table = new ElementTable();

        Element unknownNumber = table.GetElement(999);
        Element unknownSymbol = table.GetElementBySymbol("Xx");
        Isotope unknownIsotope = table.GetIsotope(6, 99);

        Assert.That(table.IsKnownElement(999), Is.False);
        Assert.That(table.IsKnownSymbol("Xx"), Is.False);
        Assert.That(unknownNumber.AtomicNumber, Is.EqualTo(0));
        Assert.That(unknownSymbol.AtomicNumber, Is.EqualTo(0));
        Assert.That(unknownIsotope.AtomicNumber, Is.EqualTo(0));
        Assert.That(table.GetAverageMass(999), Is.EqualTo(0.0));
        Assert.That(table.GetCovalentRadius(999), Is.EqualTo(0.0));
    }

    [Test]
    public void IsotopeTable_ReturnsKnownIsotopesAndMajorIsotopes()
    {
        ElementTable table = new ElementTable();

        List<Isotope> oxygenIsotopes = table.GetIsotopes(8);
        Isotope carbon13 = table.GetIsotope(6, 13);
        Isotope oxygenMajor = table.GetMajorIsotope(8);
        Isotope iodineMajor = table.GetMajorIsotope(53);

        Assert.That(oxygenIsotopes.Count, Is.EqualTo(3));
        Assert.That(carbon13.AtomicNumber, Is.EqualTo(6));
        Assert.That(carbon13.MassNumber, Is.EqualTo(13));
        Assert.That(carbon13.ExactMass, Is.EqualTo(13.003355).Within(0.000001));
        Assert.That(carbon13.IsMajor, Is.False);
        Assert.That(oxygenMajor.MassNumber, Is.EqualTo(16));
        Assert.That(oxygenMajor.IsMajor, Is.True);
        Assert.That(iodineMajor.MassNumber, Is.EqualTo(127));
        Assert.That(iodineMajor.NaturalAbundance, Is.EqualTo(100.0).Within(0.001));
    }

    [Test]
    public void IsotopeTable_ContainsEveryRecordFromTheReferenceData()
    {
        ElementTable table = new ElementTable();

        Assert.That(table.Isotopes.Count, Is.EqualTo(354));
        Assert.That(table.GetIsotopes(1).Count, Is.EqualTo(3));
        Assert.That(table.GetIsotopes(50).Count, Is.EqualTo(10));
        Assert.That(table.GetIsotopes(118).Count, Is.EqualTo(1));
        Assert.That(table.GetIsotope(1, 3).ExactMass, Is.EqualTo(3.0160492779).Within(0.0000000001));
        Assert.That(table.GetIsotope(92, 238).NaturalAbundance, Is.EqualTo(99.2742).Within(0.0001));
        Assert.That(table.GetMajorIsotope(92).MassNumber, Is.EqualTo(238));
    }

    [Test]
    public void AtomicMasses_ExposeAverageMonoisotopicAndIsotopeMasses()
    {
        AtomicMasses masses = new AtomicMasses();

        Assert.That(masses.GetAverageMass(1), Is.EqualTo(1.008).Within(0.001));
        Assert.That(masses.GetMonoisotopicMass(8), Is.EqualTo(15.994915).Within(0.000001));
        Assert.That(masses.GetIsotopeMass(17, 37), Is.EqualTo(36.965903).Within(0.000001));
        Assert.That(masses.GetIsotopeMass(17, 39), Is.EqualTo(0.0));
    }

    [Test]
    public void AtomicMasses_UseDesignatedIsotopesWhenStandardAtomicWeightsDoNotExist()
    {
        ElementTable table = new ElementTable();
        AtomicMasses masses = new AtomicMasses();

        Assert.That(masses.GetAverageMass(43), Is.EqualTo(98.0));
        Assert.That(masses.GetMonoisotopicMass(43), Is.EqualTo(97.9072124).Within(0.0000001));
        Assert.That(table.GetMajorIsotope(43).MassNumber, Is.EqualTo(98));
        Assert.That(masses.GetAverageMass(95), Is.EqualTo(243.0));
        Assert.That(table.GetMajorIsotope(95).MassNumber, Is.EqualTo(243));
        Assert.That(masses.GetAverageMass(118), Is.EqualTo(294.0));
        Assert.That(table.GetMajorIsotope(118).MassNumber, Is.EqualTo(294));
    }

    [Test]
    public void ElementAndIsotopeLookups_CopyCallerOwnedCollections()
    {
        int[] valences = new int[] { 2, 4 };
        Element element = new Element(16, "S", "Sulfur", 32.06, 31.972071, 1.05, false, valences);
        ElementTable table = new ElementTable();

        List<Isotope> first = table.GetIsotopes(8);
        first.Clear();
        valences[0] = 99;

        Assert.That(element.Symbol, Is.EqualTo("S"));
        Assert.That(element.Name, Is.EqualTo("Sulfur"));
        Assert.That(element.CommonValences, Is.EqualTo(new int[] { 2, 4 }));
        Assert.That(table.GetIsotopes(8), Has.Count.EqualTo(3));
        Assert.That(table.GetIsotopes(999), Is.Empty);
        Assert.That(table.GetMajorIsotope(999).AtomicNumber, Is.EqualTo(0));
    }

    [Test]
    public void MassLookups_ReturnZeroForUnknownElementsAndIsotopes()
    {
        ElementTable table = new ElementTable();
        AtomicMasses masses = new AtomicMasses();

        Assert.That(table.GetMonoisotopicMass(-1), Is.EqualTo(0.0));
        Assert.That(masses.GetAverageMass(-1), Is.EqualTo(0.0));
        Assert.That(masses.GetMonoisotopicMass(999), Is.EqualTo(0.0));
        Assert.That(masses.GetIsotopeMass(6, -1), Is.EqualTo(0.0));
    }

    [Test]
    public void ValenceTable_ReturnsCopiesOfCommonValences()
    {
        ValenceTable valences = new ValenceTable();

        List<int> sulfurValences = valences.GetCommonValences(16);
        sulfurValences.Add(99);

        List<int> sulfurValencesAgain = valences.GetCommonValences(16);

        Assert.That(sulfurValencesAgain, Is.EqualTo(new int[] { 2, 4, 6 }));
        Assert.That(valences.IsAllowedValence(16, 6), Is.True);
        Assert.That(valences.IsAllowedValence(16, 5), Is.False);
        Assert.That(valences.IsAllowedValence(999, 1), Is.False);
    }

    [Test]
    public void ValenceTable_CoversRepresentativeMainGroupTransitionAndActinideElements()
    {
        ValenceTable valences = new ValenceTable();

        Assert.That(valences.GetCommonValences(22), Is.EqualTo(new int[] { 2, 3, 4 }));
        Assert.That(valences.GetCommonValences(54), Is.Empty);
        Assert.That(valences.GetCommonValences(92), Is.EqualTo(new int[] { 3, 4, 5, 6 }));
        Assert.That(valences.IsAllowedValence(106, 6), Is.True);
        Assert.That(valences.GetCommonValences(118), Is.Empty);
    }
}
