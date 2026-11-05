using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class PatternFingerprintTests
{
    [Test]
    public void MaccsKeys_LoadsBuiltInPatternDefinitions()
    {
        MaccsKeys keys = new MaccsKeys();

        Assert.That(keys.Keys.Count, Is.GreaterThanOrEqualTo(16));
        Assert.That(keys.ContainsKey("carbon"), Is.True);
        Assert.That(keys.ContainsKey("oxygen"), Is.True);
        Assert.That(keys.ContainsKey("halogen"), Is.True);
        Assert.That(keys.ContainsKey("carbonyl"), Is.True);
        Assert.That(keys.FindKey(" Carbonyl ").Index, Is.EqualTo(10));
        Assert.That(keys.FindKeyByIndex(12).Name, Is.EqualTo("carbon-oxygen-single"));
        Assert.That(keys.FindKey("missing").Name, Is.EqualTo(""));
    }

    [Test]
    public void Create_SetsBitsForMatchedMaccsLikeKeys()
    {
        PatternFingerprint generator = new PatternFingerprint();
        FingerprintOptions options = BuildOptions();

        BitFingerprint fingerprint = generator.Create(BuildEthanol(), options);

        Assert.That(fingerprint.GetBit(0), Is.True);
        Assert.That(fingerprint.GetBit(2), Is.True);
        Assert.That(fingerprint.GetBit(6), Is.True);
        Assert.That(fingerprint.GetBit(11), Is.True);
        Assert.That(fingerprint.GetBit(12), Is.True);
        Assert.That(fingerprint.GetBit(1), Is.False);
        Assert.That(fingerprint.GetBit(9), Is.False);
    }

    [Test]
    public void Create_DetectsCarbonylAndHalogenPatterns()
    {
        PatternFingerprint generator = new PatternFingerprint();
        FingerprintOptions options = BuildOptions();

        BitFingerprint carbonyl = generator.Create(BuildCarbonyl(), options);
        BitFingerprint halogen = generator.Create(BuildChloromethane(), options);

        Assert.That(carbonyl.GetBit(9), Is.True);
        Assert.That(carbonyl.GetBit(5), Is.False);
        Assert.That(halogen.GetBit(5), Is.True);
        Assert.That(halogen.GetBit(9), Is.False);
    }

    [Test]
    public void MatchesKey_UsesSmartsMatcher()
    {
        PatternFingerprint generator = new PatternFingerprint();
        MaccsKeys keys = new MaccsKeys();

        Assert.That(generator.MatchesKey(BuildCarbonyl(), keys.FindKey("carbonyl")), Is.True);
        Assert.That(generator.MatchesKey(BuildEthanol(), keys.FindKey("carbonyl")), Is.False);
    }

    [Test]
    public void FingerprintGenerator_CreatePatternFingerprintDelegatesToPatternGenerator()
    {
        FingerprintGenerator generator = new FingerprintGenerator();
        FingerprintOptions options = BuildOptions();

        BitFingerprint fingerprint = generator.CreatePatternFingerprint(BuildEthanol(), options);

        Assert.That(fingerprint.Size, Is.EqualTo(166));
        Assert.That(fingerprint.GetBit(0), Is.True);
        Assert.That(fingerprint.GetBit(2), Is.True);
    }

    [Test]
    public void Create_ReturnsEmptyFingerprintForNullMolecule()
    {
        PatternFingerprint generator = new PatternFingerprint();
        FingerprintOptions options = BuildOptions();

        BitFingerprint fingerprint = generator.Create(null, options);

        Assert.That(fingerprint.Size, Is.EqualTo(166));
        Assert.That(fingerprint.CountOnBits(), Is.EqualTo(0));
    }

    static FingerprintOptions BuildOptions()
    {
        FingerprintOptions options = new FingerprintOptions();
        options.BitCount = 166;
        return options;
    }

    static Molecule BuildEthanol()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(carbonA, carbonB, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonB, oxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildCarbonyl()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildChloromethane()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int chlorine = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        builder.AddBond(carbon, chlorine, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }
}
