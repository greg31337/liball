using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class PathFingerprintTests
{
    [Test]
    public void BitFingerprint_SetGetCountAndClearBits()
    {
        BitFingerprint fingerprint = new BitFingerprint(16);

        fingerprint.SetBit(3);
        fingerprint.SetBit(3);
        fingerprint.SetBit(15);

        Assert.That(fingerprint.GetBit(3), Is.True);
        Assert.That(fingerprint.GetBit(15), Is.True);
        Assert.That(fingerprint.GetBit(16), Is.False);
        Assert.That(fingerprint.CountOnBits(), Is.EqualTo(2));
        Assert.That(fingerprint.ToBitString().Length, Is.EqualTo(16));

        fingerprint.Clear();

        Assert.That(fingerprint.CountOnBits(), Is.EqualTo(0));
    }

    [Test]
    public void Create_ReturnsStableFingerprintUnderAtomOrderChanges()
    {
        PathFingerprint generator = new PathFingerprint();
        FingerprintOptions options = BuildOptions();

        BitFingerprint forward = generator.Create(BuildCarbonOxygenNitrogen(), options);
        BitFingerprint reverse = generator.Create(BuildNitrogenOxygenCarbon(), options);

        Assert.That(forward.Size, Is.EqualTo(512));
        Assert.That(forward.CountOnBits(), Is.GreaterThan(0));
        Assert.That(forward.ToBitString(), Is.EqualTo(reverse.ToBitString()));
    }

    [Test]
    public void Create_DistinguishesDifferentPathFeatures()
    {
        PathFingerprint generator = new PathFingerprint();
        FingerprintOptions options = BuildOptions();

        BitFingerprint carbonOxygen = generator.Create(BuildCarbonOxygen(), options);
        BitFingerprint carbonNitrogen = generator.Create(BuildCarbonNitrogen(), options);

        Assert.That(carbonOxygen.ToBitString(), Is.Not.EqualTo(carbonNitrogen.ToBitString()));
    }

    [Test]
    public void Create_RespectsMaximumPathLength()
    {
        PathFingerprint generator = new PathFingerprint();
        FingerprintOptions shortOptions = BuildOptions();
        FingerprintOptions longOptions = BuildOptions();
        shortOptions.MaximumPathLength = 1;
        longOptions.MaximumPathLength = 2;

        BitFingerprint shortFingerprint = generator.Create(BuildCarbonOxygenNitrogen(), shortOptions);
        BitFingerprint longFingerprint = generator.Create(BuildCarbonOxygenNitrogen(), longOptions);

        Assert.That(longFingerprint.CountOnBits(), Is.GreaterThanOrEqualTo(shortFingerprint.CountOnBits()));
        Assert.That(longFingerprint.ToBitString(), Is.Not.EqualTo(shortFingerprint.ToBitString()));
    }

    [Test]
    public void Create_CanIgnoreBondOrder()
    {
        PathFingerprint generator = new PathFingerprint();
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
    public void FingerprintGenerator_CreatePathFingerprintDelegatesToPathGenerator()
    {
        FingerprintGenerator generator = new FingerprintGenerator();
        FingerprintOptions options = BuildOptions();

        BitFingerprint fingerprint = generator.CreatePathFingerprint(BuildCarbonOxygen(), options);

        Assert.That(fingerprint.Size, Is.EqualTo(512));
        Assert.That(fingerprint.CountOnBits(), Is.GreaterThan(0));
    }

    [Test]
    public void Create_ReturnsEmptyFingerprintForNullMolecule()
    {
        PathFingerprint generator = new PathFingerprint();
        FingerprintOptions options = BuildOptions();

        BitFingerprint fingerprint = generator.Create(null, options);

        Assert.That(fingerprint.Size, Is.EqualTo(512));
        Assert.That(fingerprint.CountOnBits(), Is.EqualTo(0));
    }

    static FingerprintOptions BuildOptions()
    {
        FingerprintOptions options = new FingerprintOptions();
        options.BitCount = 512;
        options.MinimumPathLength = 0;
        options.MaximumPathLength = 3;
        return options;
    }

    static Molecule BuildCarbonOxygen()
    {
        return BuildCarbonOxygenWithBond(BondOrder.Single);
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
