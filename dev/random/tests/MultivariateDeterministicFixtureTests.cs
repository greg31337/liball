namespace random.Tests;

public class MultivariateDeterministicFixtureTests
{
    [Test]
    public void PortableVectorSamplersMatchPinnedComponentOrderFixtures()
    {
        DirichletSampler dirichlet = new DirichletSampler(Create(101UL), [0.5, 1.5, 3.0]);
        MultivariateNormalSampler normal = MultivariateNormalSampler.FromCholesky(
            Create(202UL),
            [10.0, 20.0, 30.0],
            [2.0, 0.0, 0.0, -1.0, 3.0, 0.0, 0.5, 4.0, 5.0]);
        NormalGammaSampler normalGamma = new NormalGammaSampler(Create(101UL), 1.25, 3.5, 4.5, 2.25);
        double[] normalBatch = new double[normal.RequiredOutputLength(2)];

        normal.Fill(2, normalBatch);

        Assert.Multiple(() =>
        {
            AssertBits(
                dirichlet.Sample(),
                [0x3F798155C4FAF9C0UL, 0x3FC37A33BF6B7CDCUL, 0x3FEAEE70649B2AD6UL]);
            AssertBits(
                normalBatch,
                [
                    0x402531A148242C51UL, 0x402CEC3142B7E821UL, 0x403648D5231C3FDBUL,
                    0x4025C75E3F976C4CUL, 0x402C805ABC5E5DEFUL, 0x4035DBCC4916EBFBUL
                ]);
            AssertBits(normalGamma.Sample(), [0x3FE7367579C80ECAUL, 0x3FF59990C536E30AUL]);
        });
    }

    [Test]
    public void PortableMatrixSamplersMatchPinnedRowMajorFixtures()
    {
        MatrixNormalSampler matrixNormal = MatrixNormalSampler.FromCholesky(
            Create(101UL),
            [10.0, 20.0, 30.0, 40.0, 50.0, 60.0],
            2,
            3,
            [2.0, 0.0, 1.0, 3.0],
            [1.0, 0.0, 0.0, 2.0, 2.0, 0.0, -1.0, 1.0, 3.0]);
        WishartSampler wishart = new WishartSampler(Create(101UL), 6.5, [4.0, 2.0, 2.0, 5.0], 2);
        InverseWishartSampler inverseWishart = new InverseWishartSampler(
            Create(202UL),
            7.5,
            [4.0, 2.0, 2.0, 5.0],
            2);

        Assert.Multiple(() =>
        {
            AssertBits(
                matrixNormal.Sample(),
                [
                    0x40218CEB3C7EC1EAUL, 0x402B858701C1792BUL, 0x40367CEB71F6DD3FUL,
                    0x4042579D48F28B16UL, 0x40469200B7E74973UL, 0x40511968FC47B44BUL
                ]);
            AssertBits(
                wishart.Sample(),
                [0x402FDA81B1E2AA56UL, 0xBFF2369F30645178UL, 0xBFF2369F30645178UL, 0x402478099DDFF0F0UL]);
            AssertBits(
                inverseWishart.Sample(),
                [0x3FE0125008F8DAFDUL, 0x3FD231A42B5316FCUL, 0x3FD231A42B5316FCUL, 0x3FE62C5D5EF9D902UL]);
        });
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }

    static void AssertBits(double[] actual, ulong[] expected)
    {
        Assert.That(actual.Length, Is.EqualTo(expected.Length));
        for (int index = 0; index < actual.Length; index++)
        {
            Assert.That(
                PortableMath.DoubleBits(actual[index]),
                Is.EqualTo(expected[index]),
                $"component {index}");
        }
    }
}
