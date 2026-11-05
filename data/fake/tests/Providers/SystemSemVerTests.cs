using fake;

namespace fake.Tests;

[TestFixture]
public class SystemSemVerTests
{
    [Test]
    public void GeneratedVersionsAreDeterministicValidAndHonorOptionalChoices()
    {
        const long seed = 11500;
        SystemProvider first = Fake.Create(seed).System();
        SystemProvider second = Fake.Create(seed).System();

        int index = 0;
        while (index < 128)
        {
            string value = first.SemVer();
            Assert.Multiple(() =>
            {
                Assert.That(value, Is.EqualTo(second.SemVer()));
                Assert.That(first.IsSemVer(value), Is.True);
            });
            index++;
        }

        SystemProvider choices = Fake.Create(11501).System();
        string core = choices.SemVerFromIncludePreReleaseAndIncludeBuildMetadata(false, false);
        string preRelease = choices.SemVerFromIncludePreReleaseAndIncludeBuildMetadata(true, false);
        string build = choices.SemVerFromIncludePreReleaseAndIncludeBuildMetadata(false, true);
        string complete = choices.SemVerFromIncludePreReleaseAndIncludeBuildMetadata(true, true);
        SemanticVersion coreParts = choices.ParseSemVer(core);
        SemanticVersion preReleaseParts = choices.ParseSemVer(preRelease);
        SemanticVersion buildParts = choices.ParseSemVer(build);
        SemanticVersion completeParts = choices.ParseSemVer(complete);
        Assert.Multiple(() =>
        {
            Assert.That(core, Does.Match("^[0-9]+\\.[0-9]+\\.[0-9]+$"));
            Assert.That(coreParts.HasPreRelease, Is.False);
            Assert.That(coreParts.HasBuildMetadata, Is.False);
            Assert.That(preReleaseParts.HasPreRelease, Is.True);
            Assert.That(preReleaseParts.HasBuildMetadata, Is.False);
            Assert.That(buildParts.HasPreRelease, Is.False);
            Assert.That(buildParts.HasBuildMetadata, Is.True);
            Assert.That(completeParts.HasPreRelease, Is.True);
            Assert.That(completeParts.HasBuildMetadata, Is.True);
            Assert.That(choices.IsSemVer(core), Is.True);
            Assert.That(choices.IsSemVer(preRelease), Is.True);
            Assert.That(choices.IsSemVer(build), Is.True);
            Assert.That(choices.IsSemVer(complete), Is.True);
        });
    }

    [Test]
    public void ExplicitComponentsAndIdentifierGeneratorsConformToSemVer()
    {
        SystemProvider system = Fake.Create(11502).System();
        string preRelease = system.SemVerPreRelease();
        string build = system.SemVerBuildMetadata();

        Assert.Multiple(() =>
        {
            Assert.That(system.SemVerFromMajorAndMinorAndPatch(0, 1, 2), Is.EqualTo("0.1.2"));
            Assert.That(system.SemVerFromMajorAndMinorAndPatchAndPreReleaseAndBuildMetadata(
                1,
                2,
                3,
                "alpha.1",
                "exp.sha.5114f85"),
                Is.EqualTo("1.2.3-alpha.1+exp.sha.5114f85"));
            Assert.That(system.SemVerFromMajorAndMinorAndPatchAndPreReleaseAndBuildMetadata(
                1,
                0,
                0,
                "x-y-z.--",
                "001"),
                Is.EqualTo("1.0.0-x-y-z.--+001"));
            Assert.That(system.IsSemVer("1.0.0-" + preRelease), Is.True);
            Assert.That(system.IsSemVer("1.0.0+" + build), Is.True);
        });
    }

    [Test]
    public void ValidatorAcceptsSpecificationExamplesAndUnboundedNumerics()
    {
        SystemProvider system = Fake.Create(11503).System();
        string[] valid = new string[]
        {
            "0.0.0",
            "1.0.0-alpha",
            "1.0.0-alpha.1",
            "1.0.0-0.3.7",
            "1.0.0-x.7.z.92",
            "1.0.0-x-y-z.--",
            "1.0.0-alpha+001",
            "1.0.0+20130313144700",
            "1.0.0-beta+exp.sha.5114f85",
            "1.0.0+21AF26D3----117B344092BD",
            "999999999999999999999999999999.0.1"
        };

        int index = 0;
        while (index < valid.Length)
        {
            Assert.That(system.IsSemVer(valid[index]), Is.True, valid[index]);
            index++;
        }
    }

    [Test]
    public void ValidatorRejectsMalformedCoreAndIdentifierCases()
    {
        SystemProvider system = Fake.Create(11504).System();
        string[] invalid = new string[]
        {
            "",
            "1",
            "1.2",
            "1.2.3.4",
            "01.2.3",
            "1.02.3",
            "1.2.03",
            "1.2.3-",
            "1.2.3+",
            "1.2.3-alpha..1",
            "1.2.3+build..1",
            "1.2.3-01",
            "1.2.3-alpha_beta",
            "1.2.3+build+again",
            "v1.2.3",
            "1.2.3-α"
        };

        int index = 0;
        while (index < invalid.Length)
        {
            Assert.That(system.IsSemVer(invalid[index]), Is.False, invalid[index]);
            index++;
        }
    }

    [Test]
    public void ParsingPreservesEveryComponentAndReturnsIndependentRecords()
    {
        SystemProvider system = Fake.Create(11505).System();
        const string value =
            "999999999999999999999.20.3-rc.1+exp.sha.5114f85";
        SemanticVersion first = system.ParseSemVer(value);
        SemanticVersion second = system.ParseSemVer(value);
        SemanticVersion core = system.ParseSemVer("1.2.3");

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Value, Is.EqualTo(value));
            Assert.That(first.Major, Is.EqualTo("999999999999999999999"));
            Assert.That(first.Minor, Is.EqualTo("20"));
            Assert.That(first.Patch, Is.EqualTo("3"));
            Assert.That(first.PreRelease, Is.EqualTo("rc.1"));
            Assert.That(first.BuildMetadata, Is.EqualTo("exp.sha.5114f85"));
            Assert.That(first.HasPreRelease, Is.True);
            Assert.That(first.HasBuildMetadata, Is.True);
            Assert.That(first.SpecificationVersion, Is.EqualTo("2.0.0"));
            Assert.That(core.PreRelease, Is.Empty);
            Assert.That(core.BuildMetadata, Is.Empty);
            Assert.That(core.HasPreRelease, Is.False);
            Assert.That(core.HasBuildMetadata, Is.False);
        });

        first.Major = "changed";
        Assert.That(second.Major, Is.EqualTo("999999999999999999999"));
    }

    [Test]
    public void ComparisonMatchesOfficialSemVerPrecedenceVector()
    {
        SystemProvider system = Fake.Create(11506).System();
        string[] ascending = new string[]
        {
            "1.0.0-alpha",
            "1.0.0-alpha.1",
            "1.0.0-alpha.beta",
            "1.0.0-beta",
            "1.0.0-beta.2",
            "1.0.0-beta.11",
            "1.0.0-rc.1",
            "1.0.0"
        };

        int index = 1;
        while (index < ascending.Length)
        {
            Assert.Multiple(() =>
            {
                Assert.That(system.CompareSemVer(
                    ascending[index - 1],
                    ascending[index]), Is.EqualTo(-1));
                Assert.That(system.CompareSemVer(
                    ascending[index],
                    ascending[index - 1]), Is.EqualTo(1));
            });
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(system.CompareSemVer("1.0.0", "2.0.0"),
                Is.EqualTo(-1));
            Assert.That(system.CompareSemVer("2.0.0", "2.1.0"),
                Is.EqualTo(-1));
            Assert.That(system.CompareSemVer("2.1.0", "2.1.1"),
                Is.EqualTo(-1));
            Assert.That(system.CompareSemVer(
                "999999999999999999999.0.0",
                "1000000000000000000000.0.0"), Is.EqualTo(-1));
            Assert.That(system.CompareSemVer("1.0.0-9", "1.0.0-alpha"),
                Is.EqualTo(-1));
            Assert.That(system.CompareSemVer("1.0.0-alpha", "1.0.0-alpha.1"),
                Is.EqualTo(-1));
            Assert.That(system.CompareSemVer("1.0.0-alpha.B", "1.0.0-alpha.a"),
                Is.EqualTo(-1));
            Assert.That(system.CompareSemVer(
                "1.0.0+build.1",
                "1.0.0+build.2"), Is.EqualTo(0));
        });
    }

    [Test]
    public void InvalidFixturesCoverEveryNamedDefectAndAreDeterministic()
    {
        const long seed = 11507;
        SystemProvider system = Fake.Create(seed).System();
        SemVerInvalidKind[] kinds = Enum.GetValues<SemVerInvalidKind>();

        int index = 0;
        while (index < kinds.Length)
        {
            string value = system.InvalidSemVerFromKind(kinds[index]);
            Assert.That(system.IsSemVer(value), Is.False, kinds[index].ToString());
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(system.InvalidSemVerFromKind(SemVerInvalidKind.LeadingZero),
                Is.EqualTo("01.2.3"));
            Assert.That(system.InvalidSemVerFromKind(SemVerInvalidKind.MissingComponent),
                Is.EqualTo("1.2"));
            Assert.That(system.InvalidSemVerFromKind(SemVerInvalidKind.EmptyIdentifier),
                Is.EqualTo("1.2.3-alpha..1"));
            Assert.That(system.InvalidSemVerFromKind(SemVerInvalidKind.InvalidCharacter),
                Is.EqualTo("1.2.3-alpha_beta"));
            Assert.That(system.InvalidSemVerFromKind(
                SemVerInvalidKind.NumericPreReleaseLeadingZero),
                Is.EqualTo("1.2.3-01"));
            Assert.That(system.InvalidSemVerFromKind(SemVerInvalidKind.ExtraCoreComponent),
                Is.EqualTo("1.2.3.4"));
        });

        SystemProvider first = Fake.Create(seed).System();
        SystemProvider second = Fake.Create(seed).System();
        index = 0;
        while (index < 64)
        {
            string value = first.InvalidSemVer();
            Assert.Multiple(() =>
            {
                Assert.That(value, Is.EqualTo(second.InvalidSemVer()));
                Assert.That(first.IsSemVer(value), Is.False);
            });
            index++;
        }
    }

    [Test]
    public void InvalidArgumentsAndLimitsFailBeforeEntropy()
    {
        const long seed = 11508;
        SystemProvider inspected = Fake.Create(seed).System();
        SystemProvider untouched = Fake.Create(seed).System();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 7;
        FakeOptions outputOptions = FakeOptions.Create(seed);
        outputOptions.MaximumOutputSize = 44;
        SystemProvider outputInspected = Fake.Create(outputOptions).System();
        SystemProvider outputUntouched = Fake.Create(outputOptions).System();
        FakeOptions parseOptions = FakeOptions.Create(seed);
        parseOptions.MaximumOutputSize = 12;

        Assert.Multiple(() =>
        {
            Assert.That(() => inspected.SemVerFromMajorAndMinorAndPatch(-1, 0, 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.SemVerFromMajorAndMinorAndPatchAndPreReleaseAndBuildMetadata(1, 2, 3, "01", ""),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => inspected.SemVerFromMajorAndMinorAndPatchAndPreReleaseAndBuildMetadata(1, 2, 3, "alpha..1", ""),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => inspected.SemVerFromMajorAndMinorAndPatchAndPreReleaseAndBuildMetadata(1, 2, 3, "alpha", "bad_meta"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => inspected.SemVerFromMajorAndMinorAndPatchAndPreReleaseAndBuildMetadata(1, 2, 3, null, "build"),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => inspected.ParseSemVer("1.2"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => inspected.CompareSemVer("1.2", "1.2.3"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => inspected.InvalidSemVerFromKind((SemVerInvalidKind)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Fake.Create(countOptions).System().SemVer(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => outputInspected.SemVer(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Fake.Create(parseOptions).System().ParseSemVer("1.2.3"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(inspected.RandomValue.Long(),
                Is.EqualTo(untouched.RandomValue.Long()));
            Assert.That(outputInspected.RandomValue.Long(),
                Is.EqualTo(outputUntouched.RandomValue.Long()));
        });
    }

    [Test]
    public void SemVerGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(11509);
        Fake untouched = Fake.Create(11509);

        int index = 0;
        while (index < 32)
        {
            inspected.System().SemVer();
            inspected.System().SemVerFromIncludePreReleaseAndIncludeBuildMetadata(true, true);
            inspected.System().SemVerPreRelease();
            inspected.System().SemVerBuildMetadata();
            inspected.System().InvalidSemVer();
            index++;
        }

        Assert.That(inspected.Person().FullName(),
            Is.EqualTo(untouched.Person().FullName()));
    }
}
