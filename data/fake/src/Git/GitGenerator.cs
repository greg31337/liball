using System;
using System.Globalization;

namespace fake;

internal static class GitGenerator
{
    const int CommitShaLength = 40;
    const int CommitNumberMaximum = 9999;
    const int CommitDateTextLength = 33;

    internal static string Branch(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, GitData.Branches, "branchData");
        Guard.OutputSize(
            MaximumLength(GitData.Branches),
            context.CapturedOptions.MaximumOutputSize,
            "branch");
        return ChooseValue(random, GitData.Branches);
    }

    internal static string Tag(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.OutputSize(
            "v99.99.99".Length,
            context.CapturedOptions.MaximumOutputSize,
            "tag");
        return TagUnchecked(random);
    }

    internal static string CommitSha(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.OutputSize(
            CommitShaLength,
            context.CapturedOptions.MaximumOutputSize,
            "commitSha");
        return CommitShaUnchecked(context, random);
    }

    internal static string CommitMessage(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, GitData.CommitMessages, "commitMessageData");
        Guard.OutputSize(
            MaximumLength(GitData.CommitMessages),
            context.CapturedOptions.MaximumOutputSize,
            "commitMessage");
        return ChooseValue(random, GitData.CommitMessages);
    }

    internal static DateTimeOffset CommitDate(
        GenerationContext context,
        FakeRandom random)
    {
        return CommitDateUnchecked(context, random);
    }

    internal static string CommitEntry(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, GitData.CommitMessages, "commitMessageData");
        ValidateAuthorData(context);
        long maximum = 7L + CommitShaLength + 9L +
            MaximumLength(GitData.AuthorNames) + 2L +
            MaximumLength(GitData.AuthorEmails) + 1L + 9L +
            CommitDateTextLength + 6L +
            MaximumLength(GitData.CommitMessages);
        Guard.OutputSize(
            maximum,
            context.CapturedOptions.MaximumOutputSize,
            "commitEntry");
        string sha = CommitShaUnchecked(context, random);
        int authorIndex = random.IntFromMaximumExclusive(GitData.AuthorNames.Length);
        DateTimeOffset date = CommitDateUnchecked(context, random);
        string message = ChooseValue(random, GitData.CommitMessages);
        return "commit " + sha + "\nAuthor: " +
            GitData.AuthorNames[authorIndex] + " <" +
            GitData.AuthorEmails[authorIndex] + ">\nDate:   " +
            date.ToString("O", CultureInfo.InvariantCulture) +
            "\n\n    " + message;
    }

    internal static string Repository(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateRepositoryData(context);
        Guard.OutputSize(
            (long)MaximumLength(GitData.RepositoryOwners) + 1L +
                MaximumLength(GitData.RepositoryNames),
            context.CapturedOptions.MaximumOutputSize,
            "repository");
        return RepositoryUnchecked(random);
    }

    internal static string Issue(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateRepositoryData(context);
        Guard.OutputSize(
            (long)MaximumLength(GitData.RepositoryOwners) + 1L +
                MaximumLength(GitData.RepositoryNames) + 1L + 4L,
            context.CapturedOptions.MaximumOutputSize,
            "issue");
        return RepositoryUnchecked(random) + "#" + random.IntFromMinimumAndMaximumAndMaximumInclusive(
            1,
            CommitNumberMaximum,
            true).ToString(CultureInfo.InvariantCulture);
    }

    internal static string PullRequest(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateRepositoryData(context);
        Guard.OutputSize(
            (long)MaximumLength(GitData.RepositoryOwners) + 1L +
                MaximumLength(GitData.RepositoryNames) + 1L + 4L,
            context.CapturedOptions.MaximumOutputSize,
            "pullRequest");
        return RepositoryUnchecked(random) + "!" + random.IntFromMinimumAndMaximumAndMaximumInclusive(
            1,
            CommitNumberMaximum,
            true).ToString(CultureInfo.InvariantCulture);
    }

    internal static string PackageName(
        GenerationContext context,
        FakeRandom random)
    {
        return ReviewedValue(
            context,
            random,
            GitData.PackageNames,
            "packageNameData",
            "packageName");
    }

    internal static string ProgrammingLanguage(
        GenerationContext context,
        FakeRandom random)
    {
        return ReviewedValue(
            context,
            random,
            GitData.ProgrammingLanguages,
            "programmingLanguageData",
            "programmingLanguage");
    }

    internal static string License(
        GenerationContext context,
        FakeRandom random)
    {
        return ReviewedValue(
            context,
            random,
            GitData.LicenseIdentifiers,
            "licenseData",
            "license");
    }

    static string ReviewedValue(
        GenerationContext context,
        FakeRandom random,
        string[] values,
        string dataName,
        string outputName)
    {
        ValidateValues(context, values, dataName);
        Guard.OutputSize(
            MaximumLength(values),
            context.CapturedOptions.MaximumOutputSize,
            outputName);
        return ChooseValue(random, values);
    }

    static string TagUnchecked(FakeRandom random)
    {
        return "v" + random.IntFromMaximumExclusive(100).ToString(
            CultureInfo.InvariantCulture) + "." +
            random.IntFromMaximumExclusive(100).ToString(CultureInfo.InvariantCulture) + "." +
            random.IntFromMaximumExclusive(100).ToString(CultureInfo.InvariantCulture);
    }

    static string CommitShaUnchecked(
        GenerationContext context,
        FakeRandom random)
    {
        return SecretGenerator.Hex(
            random,
            CommitShaLength,
            context.CapturedOptions.MaximumOutputSize);
    }

    static DateTimeOffset CommitDateUnchecked(
        GenerationContext context,
        FakeRandom random)
    {
        return RelativeDateGenerator.Recent(
            random,
            context.ReferenceTime(),
            TimeSpan.FromDays(365.0));
    }

    static string RepositoryUnchecked(FakeRandom random)
    {
        return ChooseValue(random, GitData.RepositoryOwners) + "/" +
            ChooseValue(random, GitData.RepositoryNames);
    }

    static string ChooseValue(FakeRandom random, string[] values)
    {
        return values[random.IntFromMaximumExclusive(values.Length)];
    }

    static void ValidateAuthorData(GenerationContext context)
    {
        if (GitData.AuthorNames.Length != GitData.AuthorEmails.Length)
        {
            throw FakeException.Create(
                "The project-authored Git author table has inconsistent column lengths.");
        }
        ValidateValues(context, GitData.AuthorNames, "authorNameData");
        ValidateValues(context, GitData.AuthorEmails, "authorEmailData");
    }

    static void ValidateRepositoryData(GenerationContext context)
    {
        ValidateValues(context, GitData.RepositoryOwners, "repositoryOwnerData");
        ValidateValues(context, GitData.RepositoryNames, "repositoryNameData");
    }

    static void ValidateValues(
        GenerationContext context,
        string[] values,
        string dataName)
    {
        Guard.Count(
            values.Length,
            context.CapturedOptions.MaximumCount,
            dataName);
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null || values[index].Length == 0)
            {
                throw FakeException.Create(
                    "A project-authored Git data table is malformed.");
            }
            index = index + 1;
        }
    }

    static int MaximumLength(string[] values)
    {
        int maximum = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Length > maximum)
            {
                maximum = values[index].Length;
            }
            index = index + 1;
        }
        return maximum;
    }
}
