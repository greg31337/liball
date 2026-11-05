using languagecodes;

namespace languagecodes.Tests;

public class LanguageLookupDataTests
{
    [Test]
    public void LookupOptionsStoreEveryLookupPolicy()
    {
        LanguageLookupOptions options = new LanguageLookupOptions
        {
            TrimWhitespace = true,
            NormalizeCase = true,
            NormalizeNames = true,
            IncludeRetiredCodes = true,
            NameLanguage = "fr",
            SearchMode = LanguageSearchMode.Prefix,
            ResultLimit = 25
        };

        Assert.Multiple(() =>
        {
            Assert.That(options.TrimWhitespace, Is.True);
            Assert.That(options.NormalizeCase, Is.True);
            Assert.That(options.NormalizeNames, Is.True);
            Assert.That(options.IncludeRetiredCodes, Is.True);
            Assert.That(options.NameLanguage, Is.EqualTo("fr"));
            Assert.That(options.SearchMode, Is.EqualTo(LanguageSearchMode.Prefix));
            Assert.That(options.ResultLimit, Is.EqualTo(25));
        });
    }

    [Test]
    public void LookupResultRepresentsAUniqueMatch()
    {
        LanguageCodeEntry entry = new LanguageCodeEntry
        {
            Set3Code = "eng",
            ReferenceName = "English",
            Status = LanguageCodeStatus.Active
        };
        LanguageMatch match = new LanguageMatch
        {
            Entry = entry,
            MatchedValue = "en",
            Kind = LanguageMatchKind.Alpha2,
            Rank = 0
        };
        LanguageLookupResult result = new LanguageLookupResult
        {
            Status = LanguageLookupStatus.Found,
            Entry = entry,
            Candidates = new[] { match },
            NormalizedQuery = "en"
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(result.Entry, Is.SameAs(entry));
            Assert.That(result.Candidates, Is.EqualTo(new[] { match }));
            Assert.That(result.Candidates[0].Entry, Is.SameAs(entry));
            Assert.That(result.Candidates[0].MatchedValue, Is.EqualTo("en"));
            Assert.That(result.Candidates[0].Kind, Is.EqualTo(LanguageMatchKind.Alpha2));
            Assert.That(result.Candidates[0].Rank, Is.Zero);
            Assert.That(result.NormalizedQuery, Is.EqualTo("en"));
        });
    }

    [Test]
    public void LookupResultRetainsRankedCandidatesForAmbiguity()
    {
        LanguageMatch first = new LanguageMatch
        {
            Entry = new LanguageCodeEntry { Set3Code = "aaa" },
            MatchedValue = "Shared name",
            Kind = LanguageMatchKind.ExactName,
            Rank = 0
        };
        LanguageMatch second = new LanguageMatch
        {
            Entry = new LanguageCodeEntry { Set3Code = "bbb" },
            MatchedValue = "Shared name",
            Kind = LanguageMatchKind.ExactName,
            Rank = 1
        };
        LanguageLookupResult result = new LanguageLookupResult
        {
            Status = LanguageLookupStatus.Ambiguous,
            Candidates = new[] { first, second },
            NormalizedQuery = "shared name"
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageLookupStatus.Ambiguous));
            Assert.That(result.Entry, Is.Null);
            Assert.That(result.Candidates, Is.EqualTo(new[] { first, second }));
            Assert.That(result.Candidates[0].Rank, Is.LessThan(result.Candidates[1].Rank));
        });
    }

    [TestCase(LanguageLookupStatus.NotFound)]
    [TestCase(LanguageLookupStatus.Reserved)]
    public void LookupResultRepresentsOutcomesWithoutAnEntry(LanguageLookupStatus status)
    {
        LanguageLookupResult result = new LanguageLookupResult
        {
            Status = status,
            NormalizedQuery = "qaa"
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(status));
            Assert.That(result.Entry, Is.Null);
            Assert.That(result.Candidates, Is.Empty);
        });
    }

    [Test]
    public void LookupResultRepresentsARetiredEntry()
    {
        LanguageCodeEntry retiredEntry = new LanguageCodeEntry
        {
            Set3Code = "fri",
            Status = LanguageCodeStatus.Retired,
            Retirement = new LanguageRetirement
            {
                RetiredCode = "fri",
                Reason = LanguageRetirementReason.Change,
                ReplacementCodes = new[] { "fry" }
            }
        };
        LanguageLookupResult result = new LanguageLookupResult
        {
            Status = LanguageLookupStatus.Retired,
            Entry = retiredEntry,
            Candidates = new[]
            {
                new LanguageMatch
                {
                    Entry = retiredEntry,
                    MatchedValue = "fri",
                    Kind = LanguageMatchKind.RetiredSet3,
                    Rank = 0
                }
            }
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageLookupStatus.Retired));
            Assert.That(result.Entry, Is.SameAs(retiredEntry));
            Assert.That(result.Entry?.Retirement?.ReplacementCodes, Is.EqualTo(new[] { "fry" }));
            Assert.That(result.Candidates[0].Kind, Is.EqualTo(LanguageMatchKind.RetiredSet3));
        });
    }

    [Test]
    public void LookupResultInstancesDoNotShareCandidateArrays()
    {
        LanguageLookupResult first = new LanguageLookupResult();
        LanguageLookupResult second = new LanguageLookupResult();

        Assert.Multiple(() =>
        {
            Assert.That(first.Candidates, Is.Empty);
            Assert.That(second.Candidates, Is.Empty);
            Assert.That(first.Candidates, Is.Not.SameAs(second.Candidates));
        });
    }

    [Test]
    public void LookupOperationRowKeepsOptionsAndResultTogether()
    {
        LanguageLookupOptions options = new LanguageLookupOptions { ResultLimit = 10 };
        LanguageLookupResult result = new LanguageLookupResult { Status = LanguageLookupStatus.NotFound };
        LookupOperationRow operation = new LookupOperationRow(options, result);

        Assert.Multiple(() =>
        {
            Assert.That(operation.Options, Is.SameAs(options));
            Assert.That(operation.Result, Is.SameAs(result));
        });
    }
}
