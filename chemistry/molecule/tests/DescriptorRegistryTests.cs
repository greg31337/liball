using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class DescriptorRegistryTests
{
    [Test]
    public void GetNames_ReturnsRegisteredCanonicalDescriptorNames()
    {
        DescriptorRegistry registry = new DescriptorRegistry();

        List<string> names = registry.GetNames();

        Assert.That(names, Does.Contain("Formula"));
        Assert.That(names, Does.Contain("MolecularWeight"));
        Assert.That(names, Does.Contain("TopologicalPolarSurfaceArea"));
        Assert.That(names, Does.Contain("FractionCsp3"));
        Assert.That(names, Does.Contain("LogP"));
        Assert.That(names, Does.Contain("MolarRefractivity"));
    }

    [Test]
    public void Contains_AcceptsCanonicalNamesAndAliases()
    {
        DescriptorRegistry registry = new DescriptorRegistry();

        Assert.That(registry.Contains(" molecular_weight "), Is.True);
        Assert.That(registry.Contains("mw"), Is.True);
        Assert.That(registry.Contains("hbd"), Is.True);
        Assert.That(registry.Contains("hba"), Is.True);
        Assert.That(registry.Contains("TPSA"), Is.True);
        Assert.That(registry.Contains("fraction-sp3"), Is.True);
        Assert.That(registry.Contains("xlogp"), Is.True);
        Assert.That(registry.Contains("molar_refractivity"), Is.True);
        Assert.That(registry.Contains("mr"), Is.True);
        Assert.That(registry.Contains("not-a-descriptor"), Is.False);
    }

    [Test]
    public void CalculateText_ReturnsFormulaByName()
    {
        DescriptorRegistry registry = new DescriptorRegistry();

        string formula = registry.CalculateText(BuildEthanol(), " formula ");

        Assert.That(formula, Is.EqualTo("C2H6O"));
    }

    [Test]
    public void CalculateNumber_ReturnsNumericDescriptorByAlias()
    {
        DescriptorRegistry registry = new DescriptorRegistry();
        Molecule molecule = BuildEthanol();

        Assert.That(registry.CalculateNumber(molecule, "mw"), Is.EqualTo(46.069).Within(0.001));
        Assert.That(registry.CalculateNumber(molecule, "hbd"), Is.EqualTo(1.0));
        Assert.That(registry.CalculateNumber(molecule, "hba"), Is.EqualTo(1.0));
        Assert.That(registry.CalculateNumber(molecule, "tpsa"), Is.EqualTo(17.07).Within(0.0001));
        Assert.That(registry.CalculateNumber(molecule, "fraction-sp3"), Is.EqualTo(1.0));
    }

    [Test]
    public void Calculate_ReturnsDescriptorValueWithCanonicalName()
    {
        DescriptorRegistry registry = new DescriptorRegistry();

        DescriptorValue value = registry.Calculate(BuildEthanol(), "heavy_atoms");

        Assert.That(value.Found, Is.True);
        Assert.That(value.IsNumber, Is.True);
        Assert.That(value.Available, Is.True);
        Assert.That(value.Name, Is.EqualTo("HeavyAtomCount"));
        Assert.That(value.Number, Is.EqualTo(3.0));
    }

    [Test]
    public void Calculate_ReturnsRegisteredUnavailableDescriptorDiagnostics()
    {
        DescriptorRegistry registry = new DescriptorRegistry();

        DescriptorValue logP = registry.Calculate(BuildEthanol(), "cLogP");
        DescriptorValue refractivity = registry.Calculate(BuildEthanol(), "mr");

        Assert.That(logP.Found, Is.True);
        Assert.That(logP.Available, Is.False);
        Assert.That(logP.IsNumber, Is.False);
        Assert.That(logP.Name, Is.EqualTo("LogP"));
        Assert.That(logP.DiagnosticCode, Is.EqualTo("DESCRIPTOR_LOGP_UNAVAILABLE"));
        Assert.That(logP.DiagnosticMessage, Does.Contain("validated atom-fragment coefficient model"));
        Assert.That(refractivity.Found, Is.True);
        Assert.That(refractivity.Available, Is.False);
        Assert.That(refractivity.Name, Is.EqualTo("MolarRefractivity"));
        Assert.That(refractivity.DiagnosticCode, Is.EqualTo("DESCRIPTOR_MOLAR_REFRACTIVITY_UNAVAILABLE"));
        Assert.That(registry.CalculateNumber(BuildEthanol(), "logp"), Is.EqualTo(0.0));
        Assert.That(registry.CalculateText(BuildEthanol(), "molar-refractivity"), Is.EqualTo(""));
    }

    [Test]
    public void UnknownDescriptorsReturnEmptyValues()
    {
        DescriptorRegistry registry = new DescriptorRegistry();

        DescriptorValue value = registry.Calculate(BuildEthanol(), "missing");

        Assert.That(value.Found, Is.False);
        Assert.That(value.Available, Is.False);
        Assert.That(value.DiagnosticCode, Is.EqualTo(""));
        Assert.That(registry.CalculateText(BuildEthanol(), "missing"), Is.EqualTo(""));
        Assert.That(registry.CalculateNumber(BuildEthanol(), "missing"), Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateAllAndCalculate_CoverEveryRegisteredDescriptor()
    {
        DescriptorRegistry registry = new DescriptorRegistry();
        Molecule molecule = BuildEthanol();
        DescriptorSet all = registry.CalculateAll(molecule);
        List<string> names = registry.GetNames();

        Assert.That(all.Formula, Is.EqualTo("C2H6O"));
        Assert.That(all.MolecularWeight, Is.EqualTo(46.069).Within(0.001));
        Assert.That(names, Has.Count.EqualTo(20));

        int index = 0;
        while (index < names.Count)
        {
            DescriptorValue value = registry.Calculate(molecule, names[index]);
            Assert.That(value.Found, Is.True, names[index]);
            Assert.That(value.Name, Is.EqualTo(names[index]));
            if (names[index] == "LogP" || names[index] == "MolarRefractivity")
            {
                Assert.That(value.Available, Is.False, names[index]);
                Assert.That(value.DiagnosticCode, Is.Not.Empty, names[index]);
            }
            else
            {
                Assert.That(value.Available, Is.True, names[index]);
            }

            index = index + 1;
        }
    }

    [Test]
    public void CalculateText_FormatsNumericDescriptorsAndNamesAreCopied()
    {
        DescriptorRegistry registry = new DescriptorRegistry();
        List<string> names = registry.GetNames();
        names.Clear();

        Assert.That(registry.CalculateText(BuildEthanol(), "AtomCount"), Is.EqualTo("9"));
        Assert.That(registry.CalculateText(BuildEthanol(), "MolecularWeight"), Is.EqualTo("46.069"));
        Assert.That(registry.GetNames(), Has.Count.EqualTo(20));
        Assert.That(registry.Contains(null), Is.False);
    }

    static Molecule BuildEthanol()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(carbonA, carbonB, new BondSpec());
        builder.AddBond(carbonB, oxygen, new BondSpec());
        return builder.Build();
    }
}
