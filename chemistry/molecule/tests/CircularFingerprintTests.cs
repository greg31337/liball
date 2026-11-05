using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class CircularFingerprintTests
{
    [Test]
    public void Create_ReturnsStableFingerprintUnderAtomOrderChanges()
    {
        CircularFingerprint generator = new CircularFingerprint();
        FingerprintOptions options = BuildOptions();

        BitFingerprint forward = generator.Create(BuildCarbonOxygenNitrogen(), options);
        BitFingerprint reverse = generator.Create(BuildNitrogenOxygenCarbon(), options);

        Assert.That(forward.Size, Is.EqualTo(2048));
        Assert.That(forward.CountOnBits(), Is.GreaterThan(0));
        Assert.That(forward.ToBitString(), Is.EqualTo(reverse.ToBitString()));
    }

    [Test]
    public void Create_ExpandsEnvironmentsByRadius()
    {
        CircularFingerprint generator = new CircularFingerprint();
        FingerprintOptions radiusZero = BuildOptions();
        FingerprintOptions radiusTwo = BuildOptions();
        radiusZero.CircularRadius = 0;
        radiusTwo.CircularRadius = 2;

        BitFingerprint zero = generator.Create(BuildCarbonOxygenNitrogen(), radiusZero);
        BitFingerprint two = generator.Create(BuildCarbonOxygenNitrogen(), radiusTwo);

        Assert.That(two.CountOnBits(), Is.GreaterThanOrEqualTo(zero.CountOnBits()));
        Assert.That(two.ToBitString(), Is.Not.EqualTo(zero.ToBitString()));
    }

    [Test]
    public void Create_DistinguishesDifferentCircularEnvironments()
    {
        CircularFingerprint generator = new CircularFingerprint();
        FingerprintOptions options = BuildOptions();

        BitFingerprint carbonOxygen = generator.Create(BuildCarbonOxygenWithBond(BondOrder.Single), options);
        BitFingerprint carbonNitrogen = generator.Create(BuildCarbonNitrogen(), options);

        Assert.That(carbonOxygen.ToBitString(), Is.Not.EqualTo(carbonNitrogen.ToBitString()));
    }

    [Test]
    public void Create_CanIgnoreBondOrderInCircularEnvironment()
    {
        CircularFingerprint generator = new CircularFingerprint();
        FingerprintOptions exactOptions = BuildOptions();
        FingerprintOptions bondInsensitiveOptions = BuildOptions();
        bondInsensitiveOptions.IncludeBondOrder = false;

        BitFingerprint singleExact = generator.Create(BuildCarbonOxygenWithBond(BondOrder.Single), exactOptions);
        BitFingerprint doubleExact = generator.Create(BuildCarbonOxygenWithBond(BondOrder.Double), exactOptions);
        BitFingerprint singleInsensitive = generator.Create(BuildCarbonOxygenWithBond(BondOrder.Single), bondInsensitiveOptions);
        BitFingerprint doubleInsensitive = generator.Create(BuildCarbonOxygenWithBond(BondOrder.Double), bondInsensitiveOptions);

        Assert.That(singleExact.ToBitString(), Is.Not.EqualTo(doubleExact.ToBitString()));
        Assert.That(singleInsensitive.ToBitString(), Is.EqualTo(doubleInsensitive.ToBitString()));
    }

    [Test]
    public void FingerprintGenerator_CreateCircularFingerprintDelegatesToCircularGenerator()
    {
        FingerprintGenerator generator = new FingerprintGenerator();
        FingerprintOptions options = BuildOptions();

        BitFingerprint fingerprint = generator.CreateCircularFingerprint(BuildCarbonOxygenWithBond(BondOrder.Single), options);

        Assert.That(fingerprint.Size, Is.EqualTo(2048));
        Assert.That(fingerprint.CountOnBits(), Is.GreaterThan(0));
    }

    [Test]
    public void Create_ReturnsEmptyFingerprintForNullMolecule()
    {
        CircularFingerprint generator = new CircularFingerprint();
        FingerprintOptions options = BuildOptions();

        BitFingerprint fingerprint = generator.Create(null, options);

        Assert.That(fingerprint.Size, Is.EqualTo(2048));
        Assert.That(fingerprint.CountOnBits(), Is.EqualTo(0));
    }

    static FingerprintOptions BuildOptions()
    {
        FingerprintOptions options = new FingerprintOptions();
        options.BitCount = 2048;
        options.CircularRadius = 2;
        return options;
    }

    static Molecule BuildCarbonNitrogen()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        builder.AddBond(carbon, nitrogen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildCarbonOxygenWithBond(BondOrder order)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(order, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildCarbonOxygenNitrogen()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(oxygen, nitrogen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildNitrogenOxygenCarbon()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(nitrogen, oxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(oxygen, carbon, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }
}
