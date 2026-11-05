using fake;

namespace fake.Tests;

[TestFixture]
public class SystemPathTests
{
    [Test]
    public void DefaultPathsAreSafeRelativeUnixValues()
    {
        const long seed = 11400;
        SystemProvider first = Fake.Create(seed).System();
        SystemProvider second = Fake.Create(seed).System();

        int index = 0;
        while (index < 128)
        {
            string directory = first.DirectoryPath();
            string sameDirectory = second.DirectoryPath();
            string file = first.FilePath();
            string sameFile = second.FilePath();
            Assert.Multiple(() =>
            {
                Assert.That(directory, Is.EqualTo(sameDirectory));
                Assert.That(file, Is.EqualTo(sameFile));
                Assert.That(directory, Does.Not.StartWith("/"));
                Assert.That(directory, Does.Not.Contain("\\"));
                Assert.That(directory.Split('/'), Has.Length.InRange(1, 4));
                Assert.That(file, Does.Not.StartWith("/"));
                Assert.That(file, Does.Not.Contain("\\"));
                Assert.That(file, Does.Not.Contain("/../"));
                Assert.That(file.Split('/'), Has.Length.InRange(2, 5));
                Assert.That(file, Does.Match(
                    "^[a-z]+(?:/[a-z]+){0,3}/[a-z]+\\.[a-z0-9]+$"));
            });
            index++;
        }
    }

    [Test]
    public void RequestedStylesRootsAndDepthsHaveExactSyntax()
    {
        SystemProvider system = Fake.Create(11401).System();
        string unixRelative = system.DirectoryPathFromStyleAndRootedAndDepth(
            FilePathStyle.Unix,
            false,
            3);
        string unixRooted = system.FilePathFromStyleAndRootedAndDepth(FilePathStyle.Unix, true, 2);
        string windowsRelative = system.DirectoryPathFromStyleAndRootedAndDepth(
            FilePathStyle.Windows,
            false,
            4);
        string windowsRooted = system.FilePathFromStyleAndRootedAndDepth(
            FilePathStyle.Windows,
            true,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(system.PathSeparator(FilePathStyle.Unix), Is.EqualTo('/'));
            Assert.That(system.PathSeparator(FilePathStyle.Windows), Is.EqualTo('\\'));
            Assert.That(unixRelative, Does.Match("^[a-z]+/[a-z]+/[a-z]+$"));
            Assert.That(unixRooted, Does.Match(
                "^/[a-z]+/[a-z]+/[a-z]+\\.[a-z0-9]+$"));
            Assert.That(windowsRelative, Does.Match(
                "^[a-z]+\\\\[a-z]+\\\\[a-z]+\\\\[a-z]+$"));
            Assert.That(windowsRooted, Does.Match(
                "^[C-E]:\\\\[a-z]+\\\\[a-z]+\\.[a-z0-9]+$"));
            Assert.That(unixRelative, Does.Not.Contain(".."));
            Assert.That(unixRooted, Does.Not.Contain(".."));
            Assert.That(windowsRelative, Does.Not.Contain(".."));
            Assert.That(windowsRooted, Does.Not.Contain(".."));
        });
    }

    [Test]
    public void InvalidCharacterApisDescribeSegmentsWithoutAffectingNormalPaths()
    {
        const long seed = 11402;
        SystemProvider system = Fake.Create(seed).System();
        char[] unix = system.InvalidPathCharacters(FilePathStyle.Unix);
        char[] windows = system.InvalidPathCharacters(FilePathStyle.Windows);
        char unixSelected = system.InvalidPathCharacter(FilePathStyle.Unix);
        char windowsSelected = system.InvalidPathCharacter(FilePathStyle.Windows);

        Assert.Multiple(() =>
        {
            Assert.That(unix, Is.EqualTo(new char[] { '\0', '/' }));
            Assert.That(windows, Has.Length.EqualTo(41));
            Assert.That(windows, Does.Contain('\0'));
            Assert.That(windows, Does.Contain('\u001f'));
            Assert.That(windows, Does.Contain('"'));
            Assert.That(windows, Does.Contain('<'));
            Assert.That(windows, Does.Contain('>'));
            Assert.That(windows, Does.Contain('|'));
            Assert.That(windows, Does.Contain(':'));
            Assert.That(windows, Does.Contain('*'));
            Assert.That(windows, Does.Contain('?'));
            Assert.That(windows, Does.Contain('/'));
            Assert.That(windows, Does.Contain('\\'));
            Assert.That(unix, Does.Contain(unixSelected));
            Assert.That(windows, Does.Contain(windowsSelected));
            Assert.That(system.FilePathFromStyleAndRooted(FilePathStyle.Unix, false)
                .IndexOf('\0'), Is.EqualTo(-1));
        });

        unix[0] = 'x';
        windows[0] = 'x';
        Assert.Multiple(() =>
        {
            Assert.That(system.InvalidPathCharacters(FilePathStyle.Unix)[0],
                Is.EqualTo('\0'));
            Assert.That(system.InvalidPathCharacters(FilePathStyle.Windows)[0],
                Is.EqualTo('\0'));
        });
    }

    [Test]
    public void TraversalPathsRequireExplicitAdversarialModeBeforeEntropy()
    {
        const long seed = 11403;
        SystemProvider inspected = Fake.Create(seed).System();
        SystemProvider untouched = Fake.Create(seed).System();

        Assert.Multiple(() =>
        {
            Assert.That(() => inspected.AdversarialTraversalPath(
                FilePathStyle.Unix), Throws.TypeOf<InvalidOperationException>());
            Assert.That(() => inspected.AdversarialTraversalPathFromStyleAndRooted(
                FilePathStyle.Windows, true),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(inspected.RandomValue.Long(),
                Is.EqualTo(untouched.RandomValue.Long()));
        });
    }

    [Test]
    public void AdversarialModeProducesInertTraversalStringsOnlyThroughExplicitApi()
    {
        FakeOptions options = FakeOptions.Create(11404);
        options.GenerationMode = GenerationMode.Adversarial;
        SystemProvider system = Fake.Create(options).System();
        string unixRelative = system.AdversarialTraversalPath(
            FilePathStyle.Unix);
        string unixRooted = system.AdversarialTraversalPathFromStyleAndRooted(
            FilePathStyle.Unix,
            true);
        string windowsRelative = system.AdversarialTraversalPath(
            FilePathStyle.Windows);
        string windowsRooted = system.AdversarialTraversalPathFromStyleAndRooted(
            FilePathStyle.Windows,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(unixRelative, Does.Match(
                "^[a-z]+/\\.\\./\\.\\./[a-z]+\\.[a-z0-9]+$"));
            Assert.That(unixRooted, Does.Match(
                "^/[a-z]+/\\.\\./\\.\\./[a-z]+\\.[a-z0-9]+$"));
            Assert.That(windowsRelative, Does.Match(
                "^[a-z]+\\\\\\.\\.\\\\\\.\\.\\\\[a-z]+\\.[a-z0-9]+$"));
            Assert.That(windowsRooted, Does.Match(
                "^[C-E]:\\\\[a-z]+\\\\\\.\\.\\\\\\.\\.\\\\[a-z]+\\.[a-z0-9]+$"));
            Assert.That(system.FilePathFromStyleAndRooted(FilePathStyle.Unix, false),
                Does.Not.Contain("/../"));
            Assert.That(system.FilePathFromStyleAndRooted(FilePathStyle.Windows, true),
                Does.Not.Contain("\\..\\"));
        });
    }

    [Test]
    public void PathValidationAndLimitsFailBeforeEntropy()
    {
        const long seed = 11405;
        SystemProvider inspected = Fake.Create(seed).System();
        SystemProvider untouched = Fake.Create(seed).System();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 9;
        FakeOptions characterCountOptions = FakeOptions.Create(seed);
        characterCountOptions.MaximumCount = 40;
        FakeOptions outputOptions = FakeOptions.Create(seed);
        outputOptions.MaximumOutputSize = 41;
        SystemProvider outputInspected = Fake.Create(outputOptions).System();
        SystemProvider outputUntouched = Fake.Create(outputOptions).System();

        Assert.Multiple(() =>
        {
            Assert.That(() => inspected.DirectoryPathFromStyleAndRooted(
                (FilePathStyle)99,
                false), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.DirectoryPathFromStyleAndRootedAndDepth(
                FilePathStyle.Unix,
                false,
                0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Fake.Create(countOptions).System().DirectoryPath(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Fake.Create(characterCountOptions).System()
                .InvalidPathCharacters(FilePathStyle.Windows),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => outputInspected.DirectoryPathFromStyleAndRootedAndDepth(
                FilePathStyle.Windows,
                true,
                4), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(inspected.RandomValue.Long(),
                Is.EqualTo(untouched.RandomValue.Long()));
            Assert.That(outputInspected.RandomValue.Long(),
                Is.EqualTo(outputUntouched.RandomValue.Long()));
        });
    }

    [Test]
    public void PathGenerationDoesNotPerturbOtherProviderStreams()
    {
        FakeOptions options = FakeOptions.Create(11406);
        options.GenerationMode = GenerationMode.Adversarial;
        Fake inspected = Fake.Create(options);
        Fake untouched = Fake.Create(options);

        int index = 0;
        while (index < 32)
        {
            inspected.System().DirectoryPathFromStyleAndRooted(FilePathStyle.Unix, false);
            inspected.System().FilePathFromStyleAndRooted(FilePathStyle.Windows, true);
            inspected.System().InvalidPathCharacter(FilePathStyle.Windows);
            inspected.System().AdversarialTraversalPath(FilePathStyle.Unix);
            index++;
        }

        Assert.That(inspected.Person().FullName(),
            Is.EqualTo(untouched.Person().FullName()));
    }
}
