using System.Globalization;
using fake;

namespace fake.Tests;

[TestFixture]
public class GitProviderTests
{
    static string[] Branches = new string[]
    {
        "chore/update-fixtures", "docs/api-guide", "feature/add-search",
        "feature/sample-export", "fix/cache-timeout",
        "fix/example-validation", "refactor/order-service", "release/1.2.3"
    };

    static string[] Messages = new string[]
    {
        "Add sample search support", "Document example API usage",
        "Fix synthetic cache timeout", "Prepare synthetic release",
        "Refactor sample order flow", "Remove obsolete fixture alias",
        "Update reviewed fixtures", "Validate example configuration"
    };

    static string[] Authors = new string[]
    {
        "Author: Alex Example <alex@example.com>",
        "Author: Casey Sample <casey@example.com>",
        "Author: Jordan Fixture <jordan@example.com>",
        "Author: Morgan Test <morgan@example.com>",
        "Author: Riley Demo <riley@example.com>",
        "Author: Taylor Mock <taylor@example.com>"
    };

    static string[] Packages = new string[]
    {
        "example-core", "example-parser", "fixture-tools", "sample-client",
        "sample-runtime", "synthetic-data", "test-support"
    };

    static string[] Languages = new string[]
    {
        "C#", "Go", "Java", "JavaScript", "Kotlin", "Python", "Ruby",
        "Rust", "TypeScript"
    };

    static string[] Licenses = new string[]
    {
        "Apache-2.0", "BSD-2-Clause", "BSD-3-Clause", "ISC", "MIT",
        "MPL-2.0", "Unlicense"
    };

    [Test]
    public void BranchTagShaAndMessagesAreDeterministicAndValid()
    {
        GitProvider first = Fake.Create(11900).Git();
        GitProvider second = Fake.Create(11900).Git();

        int index = 0;
        while (index < 128)
        {
            string branch = first.Branch();
            string tag = first.Tag();
            string sha = first.CommitSha();
            string message = first.CommitMessage();

            Assert.Multiple(() =>
            {
                Assert.That(branch, Is.EqualTo(second.Branch()));
                Assert.That(tag, Is.EqualTo(second.Tag()));
                Assert.That(sha, Is.EqualTo(second.CommitSha()));
                Assert.That(message, Is.EqualTo(second.CommitMessage()));
                Assert.That(branch, Is.AnyOf(Branches));
                Assert.That(branch, Does.Match(
                    "^[a-z0-9][a-z0-9.-]*(/[a-z0-9][a-z0-9.-]*)+$"));
                Assert.That(branch, Does.Not.Contain(".."));
                Assert.That(branch, Does.Not.EndWith(".lock"));
                Assert.That(tag, Does.Match(
                    "^v(?:[0-9]|[1-9][0-9])\\.(?:[0-9]|[1-9][0-9])\\.(?:[0-9]|[1-9][0-9])$"));
                Assert.That(sha, Does.Match("^[0-9a-f]{40}$"));
                Assert.That(message, Is.AnyOf(Messages));
            });
            index++;
        }
    }

    [Test]
    public void CommitDatesUseTheCapturedReferenceWindow()
    {
        DateTimeOffset reference = new DateTimeOffset(
            2034,
            5,
            6,
            7,
            8,
            9,
            TimeSpan.FromHours(5.5));
        FakeOptions firstOptions = FakeOptions.Create(11901);
        firstOptions.ReferenceTime = reference;
        FakeOptions secondOptions = FakeOptions.Create(11901);
        secondOptions.ReferenceTime = reference;
        GitProvider first = Fake.Create(firstOptions).Git();
        GitProvider second = Fake.Create(secondOptions).Git();
        firstOptions.ReferenceTime = reference.AddYears(10);

        int index = 0;
        while (index < 128)
        {
            DateTimeOffset value = first.CommitDate();
            Assert.Multiple(() =>
            {
                Assert.That(value, Is.EqualTo(second.CommitDate()));
                Assert.That(value.Offset, Is.EqualTo(TimeSpan.Zero));
                Assert.That(value, Is.GreaterThanOrEqualTo(
                    reference.AddDays(-365.0)));
                Assert.That(value, Is.LessThanOrEqualTo(reference));
            });
            index++;
        }
    }

    [Test]
    public void CommitDateHandlesTheMinimumRepresentableReference()
    {
        FakeOptions options = FakeOptions.Create(11902);
        options.ReferenceTime = DateTimeOffset.MinValue;

        Assert.That(Fake.Create(options).Git().CommitDate(),
            Is.EqualTo(DateTimeOffset.MinValue));
    }

    [Test]
    public void CommitEntriesContainCorrelatedSyntheticFields()
    {
        DateTimeOffset reference = new DateTimeOffset(
            2035,
            6,
            7,
            8,
            9,
            10,
            TimeSpan.Zero);
        FakeOptions firstOptions = FakeOptions.Create(11903);
        firstOptions.ReferenceTime = reference;
        FakeOptions secondOptions = FakeOptions.Create(11903);
        secondOptions.ReferenceTime = reference;
        GitProvider first = Fake.Create(firstOptions).Git();
        GitProvider second = Fake.Create(secondOptions).Git();

        int index = 0;
        while (index < 128)
        {
            string value = first.CommitEntry();
            string[] lines = value.Split('\n');
            DateTimeOffset date = DateTimeOffset.ParseExact(
                lines[2].Substring("Date:   ".Length),
                "O",
                CultureInfo.InvariantCulture);

            Assert.Multiple(() =>
            {
                Assert.That(value, Is.EqualTo(second.CommitEntry()));
                Assert.That(lines, Has.Length.EqualTo(5));
                Assert.That(lines[0], Does.Match("^commit [0-9a-f]{40}$"));
                Assert.That(lines[1], Is.AnyOf(Authors));
                Assert.That(lines[2], Does.StartWith("Date:   "));
                Assert.That(date, Is.GreaterThanOrEqualTo(
                    reference.AddDays(-365.0)));
                Assert.That(date, Is.LessThanOrEqualTo(reference));
                Assert.That(lines[3], Is.Empty);
                Assert.That(lines[4].Substring(4), Is.AnyOf(Messages));
                Assert.That(lines[4], Does.StartWith("    "));
            });
            index++;
        }
    }

    [Test]
    public void RepositoryReferencesAndDeveloperVocabularyHaveReviewedShapes()
    {
        const long seed = 11904;
        GitProvider first = Fake.Create(seed).Git();
        GitProvider second = Fake.Create(seed).Git();

        int index = 0;
        while (index < 128)
        {
            string repository = first.Repository();
            string issue = first.Issue();
            string pullRequest = first.PullRequest();
            string package = first.PackageName();
            string language = first.ProgrammingLanguage();
            string license = first.License();

            Assert.Multiple(() =>
            {
                Assert.That(repository, Is.EqualTo(second.Repository()));
                Assert.That(issue, Is.EqualTo(second.Issue()));
                Assert.That(pullRequest, Is.EqualTo(second.PullRequest()));
                Assert.That(package, Is.EqualTo(second.PackageName()));
                Assert.That(language, Is.EqualTo(
                    second.ProgrammingLanguage()));
                Assert.That(license, Is.EqualTo(second.License()));
                Assert.That(repository, Does.Match(
                    "^[a-z][a-z-]*/[a-z][a-z-]*$"));
                Assert.That(issue, Does.Match(
                    "^[a-z][a-z-]*/[a-z][a-z-]*#[1-9][0-9]{0,3}$"));
                Assert.That(pullRequest, Does.Match(
                    "^[a-z][a-z-]*/[a-z][a-z-]*![1-9][0-9]{0,3}$"));
                Assert.That(package, Is.AnyOf(Packages));
                Assert.That(language, Is.AnyOf(Languages));
                Assert.That(license, Is.AnyOf(Licenses));
            });
            index++;
        }
    }

    [Test]
    public void LimitsFailBeforeConsumingGitEntropy()
    {
        const long seed = 11905;
        FakeOptions tinyOptions = FakeOptions.Create(seed);
        tinyOptions.MaximumOutputSize = 1;
        GitProvider tinyInspected = Fake.Create(tinyOptions).Git();
        GitProvider tinyUntouched = Fake.Create(tinyOptions).Git();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 4;
        GitProvider countInspected = Fake.Create(countOptions).Git();
        GitProvider countUntouched = Fake.Create(countOptions).Git();

        Assert.Multiple(() =>
        {
            Assert.That(() => tinyInspected.Branch(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Tag(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.CommitSha(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.CommitMessage(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.CommitEntry(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Repository(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Issue(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.PullRequest(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.PackageName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.ProgrammingLanguage(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.License(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Branch(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.CommitMessage(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.CommitEntry(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Repository(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Issue(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.PullRequest(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.PackageName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.ProgrammingLanguage(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.License(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(tinyInspected.RandomValue.Long(),
                Is.EqualTo(tinyUntouched.RandomValue.Long()));
            Assert.That(countInspected.RandomValue.Long(),
                Is.EqualTo(countUntouched.RandomValue.Long()));
        });
    }

    [Test]
    public void GitGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(11906);
        Fake untouched = Fake.Create(11906);

        int index = 0;
        while (index < 32)
        {
            inspected.Git().Branch();
            inspected.Git().Tag();
            inspected.Git().CommitSha();
            inspected.Git().CommitMessage();
            inspected.Git().CommitDate();
            inspected.Git().CommitEntry();
            inspected.Git().Repository();
            inspected.Git().Issue();
            inspected.Git().PullRequest();
            inspected.Git().PackageName();
            inspected.Git().ProgrammingLanguage();
            inspected.Git().License();
            index++;
        }

        Assert.That(inspected.Person().FullName(),
            Is.EqualTo(untouched.Person().FullName()));
    }
}
