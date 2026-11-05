using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class SparseCountFingerprintTests
{
    [Test]
    public void DefaultFactories_CreateEmpty1024BitFingerprints()
    {
        BitFingerprint bits = BitFingerprint.CreateDefault();
        SparseFingerprint sparse = SparseFingerprint.CreateDefault();
        CountFingerprint counts = CountFingerprint.CreateDefault();

        Assert.That(bits.Size, Is.EqualTo(1024));
        Assert.That(bits.CountOnBits(), Is.EqualTo(0));
        Assert.That(sparse.Size, Is.EqualTo(1024));
        Assert.That(sparse.CountOnBits(), Is.EqualTo(0));
        Assert.That(counts.Size, Is.EqualTo(1024));
        Assert.That(counts.TotalCount(), Is.EqualTo(0));
    }

    [Test]
    public void SparseFingerprint_AddsSortedUniqueIndicesAndConvertsToBits()
    {
        SparseFingerprint sparse = new SparseFingerprint(16);

        sparse.AddIndex(7);
        sparse.AddIndex(2);
        sparse.AddIndex(7);

        BitFingerprint bits = sparse.ToBitFingerprint();

        Assert.That(sparse.CountOnBits(), Is.EqualTo(2));
        Assert.That(sparse.Indices, Is.EqualTo(new int[] { 2, 7 }));
        Assert.That(bits.GetBit(2), Is.True);
        Assert.That(bits.GetBit(7), Is.True);
    }

    [Test]
    public void CountFingerprint_AccumulatesCountsAndConvertsToSparseAndBits()
    {
        CountFingerprint counts = new CountFingerprint(16);

        counts.Increment(5);
        counts.AddCount(5, 2);
        counts.Increment(1);

        SparseFingerprint sparse = counts.ToSparseFingerprint();
        BitFingerprint bits = counts.ToBitFingerprint();

        Assert.That(counts.NonZeroCount(), Is.EqualTo(2));
        Assert.That(counts.TotalCount(), Is.EqualTo(4));
        Assert.That(counts.GetCount(5), Is.EqualTo(3));
        Assert.That(sparse.Indices, Is.EqualTo(new int[] { 1, 5 }));
        Assert.That(bits.GetBit(1), Is.True);
        Assert.That(bits.GetBit(5), Is.True);
    }

    [Test]
    public void PathCountFingerprint_FoldsToSameBitsAsPathBitFingerprint()
    {
        FingerprintGenerator generator = new FingerprintGenerator();
        FingerprintOptions options = BuildOptions();

        BitFingerprint bits = generator.CreatePathFingerprint(BuildEthanol(), options);
        CountFingerprint counts = generator.CreatePathCountFingerprint(BuildEthanol(), options);

        Assert.That(counts.TotalCount(), Is.GreaterThanOrEqualTo(bits.CountOnBits()));
        Assert.That(counts.ToBitFingerprint().ToBitString(), Is.EqualTo(bits.ToBitString()));
    }

    [Test]
    public void CircularCountFingerprint_FoldsToSameBitsAsCircularBitFingerprint()
    {
        FingerprintGenerator generator = new FingerprintGenerator();
        FingerprintOptions options = BuildOptions();

        BitFingerprint bits = generator.CreateCircularFingerprint(BuildEthanol(), options);
        CountFingerprint counts = generator.CreateCircularCountFingerprint(BuildEthanol(), options);

        Assert.That(counts.TotalCount(), Is.GreaterThanOrEqualTo(bits.CountOnBits()));
        Assert.That(counts.ToBitFingerprint().ToBitString(), Is.EqualTo(bits.ToBitString()));
    }

    [Test]
    public void PatternCountFingerprint_UsesSmartsMatchCounts()
    {
        FingerprintGenerator generator = new FingerprintGenerator();
        FingerprintOptions options = BuildPatternOptions();

        CountFingerprint counts = generator.CreatePatternCountFingerprint(BuildEthanol(), options);

        Assert.That(counts.GetCount(0), Is.EqualTo(2));
        Assert.That(counts.GetCount(2), Is.EqualTo(1));
        Assert.That(counts.GetCount(11), Is.EqualTo(1));
        Assert.That(counts.GetCount(12), Is.EqualTo(1));
    }

    [Test]
    public void Generator_CreatesSparseVariantsFromBitFingerprints()
    {
        FingerprintGenerator generator = new FingerprintGenerator();
        FingerprintOptions pathOptions = BuildOptions();
        FingerprintOptions patternOptions = BuildPatternOptions();

        SparseFingerprint path = generator.CreatePathSparseFingerprint(BuildEthanol(), pathOptions);
        SparseFingerprint circular = generator.CreateCircularSparseFingerprint(BuildEthanol(), pathOptions);
        SparseFingerprint pattern = generator.CreatePatternSparseFingerprint(BuildEthanol(), patternOptions);

        Assert.That(path.CountOnBits(), Is.GreaterThan(0));
        Assert.That(circular.CountOnBits(), Is.GreaterThan(0));
        Assert.That(pattern.ContainsIndex(0), Is.True);
        Assert.That(pattern.ContainsIndex(2), Is.True);
    }

    static FingerprintOptions BuildOptions()
    {
        FingerprintOptions options = new FingerprintOptions();
        options.BitCount = 512;
        options.MinimumPathLength = 0;
        options.MaximumPathLength = 3;
        options.CircularRadius = 2;
        return options;
    }

    static FingerprintOptions BuildPatternOptions()
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
}
