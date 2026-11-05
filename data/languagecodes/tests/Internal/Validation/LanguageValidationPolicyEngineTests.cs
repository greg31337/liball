using languagecodes;

namespace languagecodes.Tests;

public class LanguageValidationPolicyEngineTests
{
    [Test]
    public void DefaultPolicyAcceptsIndividualsAndRejectsOptInCategories()
    {
        LanguageValidationResult individual =
            LanguageValidationEngine.ValidateCode("en", new LanguageValidationOptions());
        LanguageValidationResult macrolanguage =
            LanguageValidationEngine.ValidateCode("ara", new LanguageValidationOptions());
        LanguageValidationResult group =
            LanguageValidationEngine.ValidateCode("afa", new LanguageValidationOptions());
        LanguageValidationResult special =
            LanguageValidationEngine.ValidateCode("mis", new LanguageValidationOptions());
        LanguageValidationResult privateUse =
            LanguageValidationEngine.ValidateCode("qaa", new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(individual.IsValid, Is.True);
            Assert.That(individual.Entry?.Scope, Is.EqualTo(LanguageScope.Individual));
            Assert.That(individual.Issues, Is.Empty);
            Assert.That(macrolanguage.IsValid, Is.False);
            Assert.That(macrolanguage.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.DisallowedScope }));
            Assert.That(group.IsValid, Is.False);
            Assert.That(group.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.GroupCodeNotAllowed }));
            Assert.That(special.IsValid, Is.False);
            Assert.That(special.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.SpecialCodeNotAllowed }));
            Assert.That(privateUse.IsValid, Is.False);
            Assert.That(privateUse.IsAssigned, Is.False);
            Assert.That(privateUse.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.PrivateUseNotAllowed }));
        });
    }

    [Test]
    public void ExplicitCategoryPoliciesAcceptMacrosGroupsSpecialsAndPrivateUse()
    {
        LanguageValidationOptions macroOptions = new LanguageValidationOptions
        {
            AllowMacrolanguages = true
        };
        LanguageValidationOptions groupOptions = new LanguageValidationOptions
        {
            AllowGroupCodes = true
        };
        LanguageValidationOptions specialOptions = new LanguageValidationOptions
        {
            AllowSpecialCodes = true
        };
        LanguageValidationOptions privateOptions = new LanguageValidationOptions
        {
            AllowPrivateUseCodes = true
        };

        LanguageValidationResult macrolanguage =
            LanguageValidationEngine.ValidateCode("ara", macroOptions);
        LanguageValidationResult group =
            LanguageValidationEngine.ValidateCode("afa", groupOptions);
        LanguageValidationResult special =
            LanguageValidationEngine.ValidateCode("mis", specialOptions);
        LanguageValidationResult privateUse =
            LanguageValidationEngine.ValidateCode("qaa", privateOptions);

        Assert.Multiple(() =>
        {
            Assert.That(macrolanguage.IsValid, Is.True);
            Assert.That(group.IsValid, Is.True);
            Assert.That(special.IsValid, Is.True);
            Assert.That(privateUse.IsValid, Is.True);
            Assert.That(privateUse.IsAssigned, Is.False);
            Assert.That(privateUse.IsCanonical, Is.True);
            Assert.That(privateUse.Entry, Is.Null);
            Assert.That(privateUse.Issues, Has.Length.EqualTo(1));
            Assert.That(privateUse.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Warning));
            Assert.That(privateUse.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.ReservedCode));
        });
    }

    [Test]
    public void RequiredSetEvaluatesTheLiteralCodeMembership()
    {
        LanguageValidationResult fraSet2 = LanguageValidationEngine.ValidateCode(
            "fra",
            CreateSetOptions(Iso639Set.Set2));
        LanguageValidationResult fraSet3 = LanguageValidationEngine.ValidateCode(
            "fra",
            CreateSetOptions(Iso639Set.Set3));
        LanguageValidationResult engSet1 = LanguageValidationEngine.ValidateCode(
            "eng",
            CreateSetOptions(Iso639Set.Set1));
        LanguageValidationResult freSet3 = LanguageValidationEngine.ValidateCode(
            "fre",
            CreateSetOptions(Iso639Set.Set3));

        Assert.Multiple(() =>
        {
            Assert.That(fraSet2.IsValid, Is.True);
            Assert.That(fraSet3.IsValid, Is.True);
            Assert.That(engSet1.IsValid, Is.False);
            Assert.That(engSet1.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.WrongSet }));
            Assert.That(freSet3.IsValid, Is.False);
            Assert.That(freSet3.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.WrongSet }));
            Assert.That(engSet1.Entry, Is.Null);
        });
    }

    [Test]
    public void RequiredFormUsesExactIndexedFormsEvenWhenSpellingsOverlap()
    {
        LanguageValidationResult terminology = LanguageValidationEngine.ValidateCode(
            "fra",
            CreateFormOptions(LanguageCodeForm.Set2Terminology));
        LanguageValidationResult set3 = LanguageValidationEngine.ValidateCode(
            "fra",
            CreateFormOptions(LanguageCodeForm.Set3));
        LanguageValidationResult bibliographic = LanguageValidationEngine.ValidateCode(
            "fre",
            CreateFormOptions(LanguageCodeForm.Set2Bibliographic));
        LanguageValidationResult wrongSet3 = LanguageValidationEngine.ValidateCode(
            "fre",
            CreateFormOptions(LanguageCodeForm.Set3));
        LanguageValidationResult wrongAlpha2 = LanguageValidationEngine.ValidateCode(
            "eng",
            CreateFormOptions(LanguageCodeForm.Alpha2));

        Assert.Multiple(() =>
        {
            Assert.That(terminology.IsValid, Is.True);
            Assert.That(set3.IsValid, Is.True);
            Assert.That(bibliographic.IsValid, Is.True);
            Assert.That(wrongSet3.IsValid, Is.False);
            Assert.That(wrongSet3.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.WrongCodeForm));
            Assert.That(wrongAlpha2.IsValid, Is.False);
            Assert.That(wrongAlpha2.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.WrongCodeForm));
        });
    }

    [Test]
    public void AllowedStatusesControlLifecycleAcceptance()
    {
        LanguageValidationOptions activeOnly = new LanguageValidationOptions
        {
            AllowedStatuses = new[] { LanguageCodeStatus.Active }
        };
        LanguageValidationOptions retiredOnly = new LanguageValidationOptions
        {
            AllowedStatuses = new[] { LanguageCodeStatus.Retired }
        };
        LanguageValidationOptions deprecatedOnly = new LanguageValidationOptions
        {
            AllowedStatuses = new[] { LanguageCodeStatus.Deprecated }
        };

        LanguageValidationResult active =
            LanguageValidationEngine.ValidateCode("eng", activeOnly);
        LanguageValidationResult retiredRejected =
            LanguageValidationEngine.ValidateCode("fri", activeOnly);
        LanguageValidationResult retiredAccepted =
            LanguageValidationEngine.ValidateCode("fri", retiredOnly);
        LanguageValidationResult deprecatedRejected =
            LanguageValidationEngine.ValidateCode("iw", activeOnly);
        LanguageValidationResult deprecatedAccepted =
            LanguageValidationEngine.ValidateCode("iw", deprecatedOnly);
        LanguageValidationResult activeRejected =
            LanguageValidationEngine.ValidateCode("eng", retiredOnly);

        Assert.Multiple(() =>
        {
            Assert.That(active.IsValid, Is.True);
            Assert.That(retiredRejected.IsValid, Is.False);
            Assert.That(retiredRejected.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.RetiredCode));
            Assert.That(retiredRejected.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Error));
            Assert.That(retiredAccepted.IsValid, Is.True);
            Assert.That(retiredAccepted.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Warning));
            Assert.That(deprecatedRejected.IsValid, Is.False);
            Assert.That(deprecatedRejected.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.DeprecatedCode));
            Assert.That(deprecatedRejected.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Error));
            Assert.That(deprecatedAccepted.IsValid, Is.True);
            Assert.That(deprecatedAccepted.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Warning));
            Assert.That(activeRejected.IsValid, Is.False);
            Assert.That(activeRejected.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.UnknownCode));
        });
    }

    [Test]
    public void ScopeAndTypeFiltersRemainIndependentFromCategoryFlags()
    {
        LanguageValidationOptions individualLiving = new LanguageValidationOptions
        {
            AllowedScopes = new[] { LanguageScope.Individual },
            AllowedTypes = new[] { LanguageType.Living }
        };
        LanguageValidationOptions wrongScope = new LanguageValidationOptions
        {
            AllowedScopes = new[] { LanguageScope.Macrolanguage }
        };
        LanguageValidationOptions wrongType = new LanguageValidationOptions
        {
            AllowedTypes = new[] { LanguageType.Historical }
        };
        LanguageValidationOptions macroButIndividualOnly = new LanguageValidationOptions
        {
            AllowedScopes = new[] { LanguageScope.Individual },
            AllowMacrolanguages = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(LanguageValidationEngine.ValidateCode("eng", individualLiving)
                .IsValid, Is.True);
            Assert.That(LanguageValidationEngine.ValidateCode("eng", wrongScope)
                .Issues[0].Error, Is.EqualTo(LanguageValidationError.DisallowedScope));
            Assert.That(LanguageValidationEngine.ValidateCode("eng", wrongType)
                .Issues[0].Error, Is.EqualTo(LanguageValidationError.DisallowedType));
            Assert.That(LanguageValidationEngine.ValidateCode("ara", macroButIndividualOnly)
                .Issues[0].Error, Is.EqualTo(LanguageValidationError.DisallowedScope));
        });
    }

    [Test]
    public void CategoryFlagsDoNotOverrideExplicitScopeOrTypeFilters()
    {
        LanguageValidationOptions groupOptions = new LanguageValidationOptions
        {
            AllowGroupCodes = true,
            AllowedScopes = new[] { LanguageScope.Individual }
        };
        LanguageValidationOptions specialOptions = new LanguageValidationOptions
        {
            AllowSpecialCodes = true,
            AllowedTypes = new[] { LanguageType.Living }
        };

        LanguageValidationResult group =
            LanguageValidationEngine.ValidateCode("afa", groupOptions);
        LanguageValidationResult special =
            LanguageValidationEngine.ValidateCode("mis", specialOptions);

        Assert.Multiple(() =>
        {
            Assert.That(group.IsValid, Is.False);
            Assert.That(group.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.DisallowedScope }));
            Assert.That(special.IsValid, Is.False);
            Assert.That(special.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.DisallowedType }));
        });
    }

    [Test]
    public void PrivateUsePolicyComposesWithSetFormStatusAndCasingRules()
    {
        LanguageValidationOptions accepted = new LanguageValidationOptions
        {
            RequiredSet = Iso639Set.Set3,
            RequiredForm = LanguageCodeForm.PrivateUse,
            AllowedStatuses = new[] { LanguageCodeStatus.PrivateUse },
            AllowPrivateUseCodes = true
        };
        LanguageValidationOptions wrongSet = new LanguageValidationOptions
        {
            RequiredSet = Iso639Set.Set2,
            AllowPrivateUseCodes = true
        };

        LanguageValidationResult lower =
            LanguageValidationEngine.ValidateCode("qaa", accepted);
        LanguageValidationResult upper =
            LanguageValidationEngine.ValidateCode("QAA", accepted);
        LanguageValidationResult rejectedSet =
            LanguageValidationEngine.ValidateCode("qaa", wrongSet);

        Assert.Multiple(() =>
        {
            Assert.That(lower.IsValid, Is.True);
            Assert.That(lower.IsAssigned, Is.False);
            Assert.That(lower.IsCanonical, Is.True);
            Assert.That(lower.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.ReservedCode }));
            Assert.That(upper.IsValid, Is.True);
            Assert.That(upper.IsCanonical, Is.False);
            Assert.That(upper.CanonicalValue, Is.EqualTo("qaa"));
            Assert.That(upper.Issues.Select(issue => issue.Error), Is.EqualTo(new[]
            {
                LanguageValidationError.ReservedCode,
                LanguageValidationError.NonCanonical
            }));
            Assert.That(rejectedSet.IsValid, Is.False);
            Assert.That(rejectedSet.Issues.Select(issue => issue.Error), Is.EqualTo(new[]
            {
                LanguageValidationError.WrongSet,
                LanguageValidationError.ReservedCode
            }));
        });
    }

    [Test]
    public void MultiplePolicyFailuresUseStableDiagnosticOrder()
    {
        LanguageValidationOptions options = new LanguageValidationOptions
        {
            RequiredSet = Iso639Set.Set1,
            RequiredForm = LanguageCodeForm.Alpha2,
            AllowedStatuses = new[] { LanguageCodeStatus.Retired },
            AllowedScopes = new[] { LanguageScope.Individual },
            AllowedTypes = new[] { LanguageType.Ancient }
        };

        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCode("ara", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.True);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Entry, Is.Null);
            Assert.That(result.Issues.Select(issue => issue.Error), Is.EqualTo(new[]
            {
                LanguageValidationError.UnknownCode,
                LanguageValidationError.WrongSet,
                LanguageValidationError.WrongCodeForm,
                LanguageValidationError.DisallowedScope,
                LanguageValidationError.DisallowedType
            }));
            Assert.That(result.Issues.All(issue =>
                issue.Severity == LanguageValidationSeverity.Error), Is.True);
        });
    }

    [Test]
    public void MalformedAndUnassignedInputsDoNotGainPolicyDiagnostics()
    {
        LanguageValidationOptions options = new LanguageValidationOptions
        {
            RequiredSet = Iso639Set.Set1,
            RequiredForm = LanguageCodeForm.Alpha2,
            AllowedStatuses = new[] { LanguageCodeStatus.Active },
            AllowedScopes = new[] { LanguageScope.Individual },
            AllowedTypes = new[] { LanguageType.Living }
        };
        LanguageValidationResult malformed =
            LanguageValidationEngine.ValidateCode("e1g", options);
        LanguageValidationResult unassigned =
            LanguageValidationEngine.ValidateCode("zzz", options);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.InvalidCharacter }));
            Assert.That(unassigned.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.UnassignedCode }));
            Assert.That(malformed.Entry, Is.Null);
            Assert.That(unassigned.Entry, Is.Null);
        });
    }

    [Test]
    public void PolicyResultsAreRecursiveDefensiveSnapshots()
    {
        LanguageValidationOptions options = new LanguageValidationOptions
        {
            AllowedStatuses = new[] { LanguageCodeStatus.Retired }
        };
        LanguageValidationResult first =
            LanguageValidationEngine.ValidateCode("fri", options);
        LanguageValidationResult second =
            LanguageValidationEngine.ValidateCode("fri", options);

        first.Entry!.Set3Code = "changed";
        first.Entry.Retirement!.ReplacementCodes[0] = "changed";
        first.Issues[0].Message = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(second.Entry?.Set3Code, Is.EqualTo("fri"));
            Assert.That(second.Entry?.Retirement?.ReplacementCodes[0], Is.EqualTo("fry"));
            Assert.That(second.Issues[0].Message, Is.EqualTo(
                "The code is retired but remains an authoritative historical identifier."));
            Assert.That(LanguageValidationEngine.ValidateCode("fri", options)
                .Entry?.Retirement?.ReplacementCodes[0], Is.EqualTo("fry"));
        });
    }

    private static LanguageValidationOptions CreateSetOptions(Iso639Set set)
    {
        return new LanguageValidationOptions
        {
            RequiredSet = set
        };
    }

    private static LanguageValidationOptions CreateFormOptions(LanguageCodeForm form)
    {
        return new LanguageValidationOptions
        {
            RequiredForm = form
        };
    }
}
