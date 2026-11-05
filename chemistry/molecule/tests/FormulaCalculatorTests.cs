using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class FormulaCalculatorTests
{
    [Test]
    public void Formula_UsesHillOrderingForCarbonContainingMolecules()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        Molecule molecule = builder.Build();
        FormulaCalculator calculator = new FormulaCalculator();

        Assert.That(calculator.Formula(molecule), Is.EqualTo("C2H6O"));
    }

    [Test]
    public void Formula_UsesAlphabeticalOrderingWhenCarbonIsAbsent()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(11, 0, 1, 0, 0, 0, 0, false, "", "Na"));
        builder.AddAtom(new AtomSpec(17, 0, -1, 0, 0, 0, 0, false, "", "Cl"));
        builder.AddAtom(new AtomSpec(8, 0, 0, 0, 2, 0, 0, false, "", "O"));
        Molecule molecule = builder.Build();
        FormulaCalculator calculator = new FormulaCalculator();

        Assert.That(calculator.Formula(molecule), Is.EqualTo("ClH2NaO"));
    }

    [Test]
    public void IsotopeFormula_DistinguishesIsotopicAtoms()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 13, 0, 0, 4, 0, 0, false, "", "C13"));
        builder.AddAtom(new AtomSpec(1, 2, 0, 0, 0, 0, 0, false, "", "D"));
        builder.AddAtom(new AtomSpec(8, 18, 0, 0, 0, 0, 0, false, "", "O18"));
        Molecule molecule = builder.Build();
        FormulaCalculator calculator = new FormulaCalculator();

        Assert.That(calculator.Formula(molecule), Is.EqualTo("CH5O"));
        Assert.That(calculator.IsotopeFormula(molecule), Is.EqualTo("[13C]H4[2H][18O]"));
    }

    [Test]
    public void Formula_SumsDisconnectedFragmentsIntoOneFormula()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(11, 0, 1, 0, 0, 0, 0, false, "", "Na"));
        builder.AddAtom(new AtomSpec(17, 0, -1, 0, 0, 0, 0, false, "", "Cl"));
        builder.AddAtom(new AtomSpec(8, 0, 0, 0, 2, 0, 0, false, "", "O"));
        builder.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", "H"));
        Molecule molecule = builder.Build();
        FormulaCalculator calculator = new FormulaCalculator();

        Assert.That(calculator.Formula(molecule), Is.EqualTo("ClH3NaO"));
    }

    [Test]
    public void Formula_ReturnsEmptyTextForEmptyOrUnknownOnlyMolecules()
    {
        FormulaCalculator calculator = new FormulaCalculator();
        Molecule empty = new Molecule();
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(999, 0, 0, 0, 0, 0, 0, false, "", "Xx"));

        Assert.That(calculator.Formula(empty), Is.EqualTo(""));
        Assert.That(calculator.Formula(builder.Build()), Is.EqualTo(""));
    }

    [Test]
    public void IsotopeFormula_NormalizesNullEmptyAndUnknownInputs()
    {
        FormulaCalculator calculator = new FormulaCalculator();
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(0, 13, 0, 0, 0, 0, 0, false, "", "unknown"));

        Assert.That(calculator.IsotopeFormula(null), Is.Empty);
        Assert.That(calculator.IsotopeFormula(new Molecule()), Is.Empty);
        Assert.That(calculator.IsotopeFormula(builder.Build()), Is.Empty);
    }

    [Test]
    public void CreateTerms_AggregatesMatchingTerms()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, false, "", "C"));
        FormulaCalculator calculator = new FormulaCalculator();

        List<FormulaTerm> terms = calculator.CreateTerms(builder.Build(), false);
        calculator.SortTerms(terms, calculator.ContainsCarbon(terms));

        Assert.That(terms.Count, Is.EqualTo(2));
        Assert.That(terms[0].Symbol, Is.EqualTo("C"));
        Assert.That(terms[0].Count, Is.EqualTo(2));
        Assert.That(terms[1].Symbol, Is.EqualTo("H"));
        Assert.That(terms[1].Count, Is.EqualTo(2));
        Assert.That(calculator.WriteFormula(terms), Is.EqualTo("C2H2"));
    }
}
