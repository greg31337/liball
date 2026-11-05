using languagecodes;

namespace languagecodes.Tests;

public class LanguageFastValidationEngineTests
{
    [TestCase("en", LanguageCodeForm.Unknown, true)]
    [TestCase("ENG", LanguageCodeForm.Set3, true)]
    [TestCase("fre", LanguageCodeForm.Set2Bibliographic, true)]
    [TestCase("iw", LanguageCodeForm.IanaLanguage, true)]
    [TestCase("fri", LanguageCodeForm.RetiredSet3, true)]
    [TestCase("agp", LanguageCodeForm.Unknown, true)]
    [TestCase("qaa", LanguageCodeForm.Unknown, false)]
    [TestCase("zzz", LanguageCodeForm.Unknown, false)]
    [TestCase("e1g", LanguageCodeForm.Unknown, false)]
    [TestCase("eng", LanguageCodeForm.Alpha2, false)]
    [TestCase("en", LanguageCodeForm.Set3, false)]
    [TestCase(null, LanguageCodeForm.Unknown, false)]
    public void AssignmentCheckRecognizesEntitiesWithoutApplyingAcceptancePolicy(
        string? code,
        LanguageCodeForm form,
        bool expected)
    {
        Assert.That(
            LanguageValidationEngine.IsAssignedCode(code, form),
            Is.EqualTo(expected));
    }

    [Test]
    public void SpanValidationMatchesDetailedStringValidationAcrossPolicies()
    {
        string[] codes =
        {
            string.Empty,
            "e",
            "e1",
            "e-g",
            "éé",
            "EN",
            "ENG",
            "en",
            "eng",
            "fre",
            "iw",
            "fri",
            "agp",
            "aaa",
            "zzz",
            "ara",
            "afa",
            "mis",
            "qaa",
            "QAA"
        };
        LanguageValidationOptions[] policies = CreatePolicies();

        int policyIndex;
        for (policyIndex = 0; policyIndex < policies.Length; policyIndex++)
        {
            LanguageValidationOptions policy = policies[policyIndex];
            foreach (string code in codes)
            {
                bool detailed =
                    LanguageValidationEngine.ValidateCode(code, policy).IsValid;
                bool fast = LanguageValidationEngine.ValidateCodeSpan(
                    code.AsSpan(),
                    policy);

                Assert.That(
                    fast,
                    Is.EqualTo(detailed),
                    $"Fast validation differed for '{code}' under policy {policyIndex}.");
            }
        }
    }

    [Test]
    public void FastChecksMatchDetailedResultsForEveryIndexedValue()
    {
        LanguageValidationOptions options = new LanguageValidationOptions();

        foreach (string code in CodeIndex.EntriesByValue.Keys)
        {
            bool expectedAssigned =
                LanguageValidationEngine.ValidateCodeRegistry(code).IsAssigned;
            bool expectedValid =
                LanguageValidationEngine.ValidateCode(code, options).IsValid;
            bool assigned = LanguageValidationEngine.IsAssignedCode(
                code,
                LanguageCodeForm.Unknown);
            bool valid = LanguageValidationEngine.ValidateCodeSpan(
                code.AsSpan(),
                options);

            if (assigned != expectedAssigned)
            {
                Assert.Fail($"Assignment differed for indexed value '{code}'.");
            }

            if (valid != expectedValid)
            {
                Assert.Fail($"Validation differed for indexed value '{code}'.");
            }
        }
    }

    [Test]
    public void SpanValidationReadsOnlyTheRequestedSlice()
    {
        char[] buffer = { 'x', 'F', 'R', 'E', 'x' };
        ReadOnlySpan<char> code = new ReadOnlySpan<char>(buffer, 1, 3);

        bool accepted = LanguageValidationEngine.ValidateCodeSpan(
            code,
            new LanguageValidationOptions());
        bool rejectedAsNoncanonical = LanguageValidationEngine.ValidateCodeSpan(
            code,
            new LanguageValidationOptions
            {
                RequireCanonical = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(rejectedAsNoncanonical, Is.False);
        });
    }

    [Test]
    public void FastChecksDoNotAllocatePerCallAfterInitialization()
    {
        LanguageValidationOptions options = new LanguageValidationOptions();
        string code = "eng";
        LanguageValidationEngine.ValidateCodeSpan(code.AsSpan(), options);
        LanguageValidationEngine.IsAssignedCode(code, LanguageCodeForm.Set3);

        long before = GC.GetAllocatedBytesForCurrentThread();
        int index;
        for (index = 0; index < 1000; index++)
        {
            LanguageValidationEngine.ValidateCodeSpan(code.AsSpan(), options);
            LanguageValidationEngine.IsAssignedCode(code, LanguageCodeForm.Set3);
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.EqualTo(0));
    }

    private static LanguageValidationOptions[] CreatePolicies()
    {
        return new[]
        {
            new LanguageValidationOptions(),
            new LanguageValidationOptions
            {
                AllowMacrolanguages = true,
                AllowGroupCodes = true,
                AllowSpecialCodes = true,
                AllowPrivateUseCodes = true
            },
            new LanguageValidationOptions
            {
                RequiredSet = Iso639Set.Set2
            },
            new LanguageValidationOptions
            {
                RequiredForm = LanguageCodeForm.Set2Bibliographic
            },
            new LanguageValidationOptions
            {
                AllowedStatuses = new[] { LanguageCodeStatus.Active }
            },
            new LanguageValidationOptions
            {
                AllowedStatuses = new[] { LanguageCodeStatus.Retired }
            },
            new LanguageValidationOptions
            {
                AllowedScopes = new[] { LanguageScope.Individual },
                AllowedTypes = new[] { LanguageType.Living }
            },
            new LanguageValidationOptions
            {
                AllowPrivateUseCodes = true,
                RequiredSet = Iso639Set.Set3,
                RequiredForm = LanguageCodeForm.PrivateUse,
                AllowedStatuses = new[] { LanguageCodeStatus.PrivateUse }
            },
            new LanguageValidationOptions
            {
                RequireCanonical = true
            },
            new LanguageValidationOptions
            {
                RequireCanonical = true,
                RequiredSet = Iso639Set.Set3
            }
        };
    }
}
