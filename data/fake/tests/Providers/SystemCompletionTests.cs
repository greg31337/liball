using fake;

namespace fake.Tests;

[TestFixture]
public class SystemCompletionTests
{
    [Test]
    public void FrameworkCatalogProducesReviewedCanonicalVersions()
    {
        SystemProvider system = Fake.Create(12100).System();
        Dictionary<string, string[]> versions = FrameworkVersions();

        foreach (KeyValuePair<string, string[]> entry in versions)
        {
            int index = 0;
            while (index < 32)
            {
                string value = system.FrameworkVersionFromName(entry.Key.ToUpperInvariant());
                Assert.Multiple(() =>
                {
                    Assert.That(entry.Value, Does.Contain(value));
                    Assert.That(system.IsApplicationVersion(value), Is.True);
                });
                index++;
            }
        }

        int generatedIndex = 0;
        while (generatedIndex < 128)
        {
            Assert.Multiple(() =>
            {
                Assert.That(versions.Keys, Does.Contain(system.FrameworkName()));
                Assert.That(system.IsApplicationVersion(system.FrameworkVersion()), Is.True);
            });
            generatedIndex++;
        }
    }

    [Test]
    public void ApplicationVersionsHaveCanonicalValidAndInvalidShapes()
    {
        const long seed = 12101;
        SystemProvider first = Fake.Create(seed).System();
        SystemProvider second = Fake.Create(seed).System();
        string[] valid = { "0.0.0", "1.2.3", "99.0.42", "123.456.789" };
        string[] invalid =
        {
            "",
            "1",
            "1.2",
            "1.2.3.4",
            ".1.2",
            "1..2",
            "1.2.",
            "01.2.3",
            "1.02.3",
            "1.2.03",
            "-1.2.3",
            "1.a.3",
            " 1.2.3",
            "1.2.3 "
        };

        int generatedIndex = 0;
        while (generatedIndex < 256)
        {
            string value = first.ApplicationVersion();
            Assert.Multiple(() =>
            {
                Assert.That(value, Is.EqualTo(second.ApplicationVersion()));
                Assert.That(value, Does.Match("^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)$"));
                Assert.That(first.IsApplicationVersion(value), Is.True);
            });
            generatedIndex++;
        }

        foreach (string value in valid)
        {
            Assert.That(first.IsApplicationVersion(value), Is.True, value);
        }
        foreach (string value in invalid)
        {
            Assert.That(first.IsApplicationVersion(value), Is.False, value);
        }
    }

    [Test]
    public void PackageMetadataCorrelatesFrameworkAndPlatformPathFields()
    {
        const long seed = 12102;
        SystemProvider first = Fake.Create(seed).System();
        SystemProvider second = Fake.Create(seed).System();
        Dictionary<string, string> managers = FrameworkManagers();
        Dictionary<string, string> targets = FrameworkTargets();

        int index = 0;
        while (index < 128)
        {
            PackageMetadataRecord unix = first.PackageMetadata();
            PackageMetadataRecord sameUnix = second.PackageMetadata();
            PackageMetadataRecord windows = first.PackageMetadataFromPathStyle(FilePathStyle.Windows);
            PackageMetadataRecord sameWindows = second.PackageMetadataFromPathStyle(FilePathStyle.Windows);

            AssertPackage(unix, sameUnix, '/', FilePathStyle.Unix, managers, targets, first);
            AssertPackage(windows, sameWindows, '\\', FilePathStyle.Windows, managers, targets, first);
            index++;
        }
    }

    [Test]
    public void LineEndingVariantsAreExactAndRandomChoiceIsDeterministic()
    {
        const long seed = 12103;
        SystemProvider first = Fake.Create(seed).System();
        SystemProvider second = Fake.Create(seed).System();

        Assert.Multiple(() =>
        {
            Assert.That(first.LineFeed(), Is.EqualTo("\n"));
            Assert.That(first.CarriageReturnLineFeed(), Is.EqualTo("\r\n"));
            Assert.That(first.CarriageReturn(), Is.EqualTo("\r"));
            Assert.That(first.LineEndingFromStyle(LineEndingStyle.LineFeed), Is.EqualTo("\n"));
            Assert.That(first.LineEndingFromStyle(LineEndingStyle.CarriageReturnLineFeed), Is.EqualTo("\r\n"));
            Assert.That(first.LineEndingFromStyle(LineEndingStyle.CarriageReturn), Is.EqualTo("\r"));
        });

        string[] variants = { "\n", "\r\n", "\r" };
        int index = 0;
        while (index < 128)
        {
            string value = first.LineEnding();
            Assert.Multiple(() =>
            {
                Assert.That(value, Is.EqualTo(second.LineEnding()));
                Assert.That(variants, Does.Contain(value));
            });
            index++;
        }
    }

    [Test]
    public void InvalidArgumentsAndConfiguredLimitsFailBeforeEntropy()
    {
        const long seed = 12104;
        SystemProvider inspected = Fake.Create(seed).System();
        SystemProvider untouched = Fake.Create(seed).System();
        FakeOptions outputOptions = FakeOptions.Create(seed);
        outputOptions.MaximumOutputSize = 7;
        SystemProvider outputInspected = Fake.Create(outputOptions).System();
        SystemProvider outputUntouched = Fake.Create(outputOptions).System();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 4;
        SystemProvider countInspected = Fake.Create(countOptions).System();
        SystemProvider countUntouched = Fake.Create(countOptions).System();
        FakeOptions lineOptions = FakeOptions.Create(seed);
        lineOptions.MaximumOutputSize = 1;
        SystemProvider lineSystem = Fake.Create(lineOptions).System();

        Assert.Multiple(() =>
        {
            Assert.That(() => inspected.FrameworkVersionFromName(null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => inspected.FrameworkVersionFromName("unknown"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => inspected.IsApplicationVersion(null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => inspected.PackageMetadataFromPathStyle((FilePathStyle)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.LineEndingFromStyle((LineEndingStyle)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(inspected.ApplicationVersion(), Is.EqualTo(untouched.ApplicationVersion()));

            Assert.That(() => outputInspected.ApplicationVersion(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => outputInspected.PackageMetadata(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(outputInspected.FrameworkName(), Is.EqualTo(outputUntouched.FrameworkName()));

            Assert.That(() => countInspected.FrameworkName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.IsApplicationVersion("1.2.3"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(countInspected.LineEnding(), Is.EqualTo(countUntouched.LineEnding()));

            Assert.That(() => lineSystem.LineEnding(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => lineSystem.CarriageReturnLineFeed(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(lineSystem.LineFeed(), Is.EqualTo("\n"));
            Assert.That(lineSystem.CarriageReturn(), Is.EqualTo("\r"));
        });
    }

    static void AssertPackage(
        PackageMetadataRecord actual,
        PackageMetadataRecord expected,
        char separator,
        FilePathStyle style,
        Dictionary<string, string> managers,
        Dictionary<string, string> targets,
        SystemProvider system)
    {
        string frameworkKey = actual.FrameworkName + "|" + actual.FrameworkVersion;
        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.Not.SameAs(expected));
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.Version, Is.EqualTo(expected.Version));
            Assert.That(actual.FrameworkName, Is.EqualTo(expected.FrameworkName));
            Assert.That(actual.FrameworkVersion, Is.EqualTo(expected.FrameworkVersion));
            Assert.That(actual.TargetFramework, Is.EqualTo(expected.TargetFramework));
            Assert.That(actual.PackageManager, Is.EqualTo(expected.PackageManager));
            Assert.That(actual.PathStyle, Is.EqualTo(expected.PathStyle));
            Assert.That(actual.InstallPath, Is.EqualTo(expected.InstallPath));
            Assert.That(actual.IsSynthetic, Is.True);
            Assert.That(actual.TestMarker, Is.EqualTo("fake.synthetic-package-metadata.v1"));
            Assert.That(actual.RepresentsPublishedPackage, Is.False);
            Assert.That(actual.RepresentsInstalledPackage, Is.False);
            Assert.That(actual.VersionsAreCurrent, Is.False);
            Assert.That(system.IsApplicationVersion(actual.Version), Is.True);
            Assert.That(system.IsApplicationVersion(actual.FrameworkVersion), Is.True);
            Assert.That(actual.PackageManager, Is.EqualTo(managers[actual.FrameworkName]));
            Assert.That(actual.TargetFramework, Is.EqualTo(targets[frameworkKey]));
            Assert.That(actual.PathStyle, Is.EqualTo(style));
            Assert.That(actual.InstallPath,
                Is.EqualTo("packages" + separator + actual.Name + separator + actual.Version));
            Assert.That(actual.Name, Does.Match("^[a-z]+(?:-[a-z]+)+$"));
        });
    }

    static Dictionary<string, string[]> FrameworkVersions()
    {
        return new Dictionary<string, string[]>
        {
            [".NET"] = new[] { "6.0.0", "7.0.0", "8.0.0" },
            ["Java"] = new[] { "17.0.0", "21.0.0", "22.0.0" },
            ["Node.js"] = new[] { "18.0.0", "20.0.0", "22.0.0" },
            ["Python"] = new[] { "3.10.0", "3.11.0", "3.12.0" },
            ["Ruby"] = new[] { "3.1.0", "3.2.0", "3.3.0" }
        };
    }

    static Dictionary<string, string> FrameworkManagers()
    {
        return new Dictionary<string, string>
        {
            [".NET"] = "NuGet",
            ["Java"] = "Maven",
            ["Node.js"] = "npm",
            ["Python"] = "PyPI",
            ["Ruby"] = "RubyGems"
        };
    }

    static Dictionary<string, string> FrameworkTargets()
    {
        return new Dictionary<string, string>
        {
            [".NET|6.0.0"] = "net6.0",
            [".NET|7.0.0"] = "net7.0",
            [".NET|8.0.0"] = "net8.0",
            ["Java|17.0.0"] = "java17",
            ["Java|21.0.0"] = "java21",
            ["Java|22.0.0"] = "java22",
            ["Node.js|18.0.0"] = "node18",
            ["Node.js|20.0.0"] = "node20",
            ["Node.js|22.0.0"] = "node22",
            ["Python|3.10.0"] = "py310",
            ["Python|3.11.0"] = "py311",
            ["Python|3.12.0"] = "py312",
            ["Ruby|3.1.0"] = "ruby31",
            ["Ruby|3.2.0"] = "ruby32",
            ["Ruby|3.3.0"] = "ruby33"
        };
    }
}
