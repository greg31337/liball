using iso20022;

namespace iso20022.Tests;

public class CodeSetProviderTests
{
    [Test]
    public void InternalLookupsSelectPinnedOrLatestEditionByIdentifierOrNameWithProvenance()
    {
        RepositorySnapshot older = CreateSnapshot("2025-06-01", "OlderName");
        RepositorySnapshot newer = CreateSnapshot("2026-06-26", "NewerName");
        ICodeSetProvider provider = new CodeSetProvider(
            new List<RepositorySnapshot> { newer, older },
            new List<ExternalCodeCatalog>());

        CodeLookupResult pinned = provider.LookupCode(new CodeLookupRequest
        {
            Kind = CodeSetKind.Internal,
            CodeSetIdentifier = "internal-status-id",
            Value = "ACTV",
            Edition = "2025-06-01"
        });
        CodeLookupResult latest = provider.LookupCode(new CodeLookupRequest
        {
            Kind = CodeSetKind.Internal,
            CodeSetIdentifier = "InternalStatusCode",
            Value = "ACTV",
            SelectLatestEdition = true
        });

        Assert.Multiple(() =>
        {
            Assert.That(pinned.Success, Is.True, pinned.ErrorMessage);
            Assert.That(pinned.Found, Is.True);
            Assert.That(pinned.Edition, Is.EqualTo("2025-06-01"));
            Assert.That(pinned.Artifact, Is.SameAs(older.Provenance.Artifact));
            Assert.That(pinned.InternalCodeSet, Is.SameAs(older.CodeSets[0]));
            Assert.That(pinned.InternalCodeValue?.Name, Is.EqualTo("OlderName"));
            Assert.That(pinned.ExternalCodeSet, Is.Null);
            Assert.That(pinned.ExternalCodeValue, Is.Null);
            Assert.That(latest.Success, Is.True, latest.ErrorMessage);
            Assert.That(latest.Found, Is.True);
            Assert.That(latest.Edition, Is.EqualTo("2026-06-26"));
            Assert.That(latest.Artifact, Is.SameAs(newer.Provenance.Artifact));
            Assert.That(latest.InternalCodeValue?.Name, Is.EqualTo("NewerName"));
        });
    }

    [Test]
    public void ExternalLookupsSelectIndependentEditionsAndNeverCrossInternalBoundaries()
    {
        RepositorySnapshot snapshot = CreateSnapshot("2026-06-26", "InternalName");
        ExternalCodeCatalog first = CreateExternalCatalog("2026-Q1", "FirstExternalName");
        ExternalCodeCatalog second = CreateExternalCatalog("2026-Q2", "SecondExternalName");
        ICodeSetProvider provider = new CodeSetProvider(
            new List<RepositorySnapshot> { snapshot },
            new List<ExternalCodeCatalog> { second, first });

        CodeLookupResult pinned = provider.LookupCode(new CodeLookupRequest
        {
            Kind = CodeSetKind.External,
            CodeSetIdentifier = "ExternalStatus1Code",
            Value = "ACTV",
            Edition = "2026-Q1"
        });
        CodeLookupResult latest = provider.LookupCode(new CodeLookupRequest
        {
            Kind = CodeSetKind.External,
            CodeSetIdentifier = "ExternalStatus1Code",
            Value = "ACTV",
            SelectLatestEdition = true
        });
        CodeLookupResult wrongSource = provider.LookupCode(new CodeLookupRequest
        {
            Kind = CodeSetKind.Internal,
            CodeSetIdentifier = "ExternalStatus1Code",
            Value = "ACTV",
            Edition = "2026-06-26"
        });

        Assert.Multiple(() =>
        {
            Assert.That(pinned.Success, Is.True, pinned.ErrorMessage);
            Assert.That(pinned.Found, Is.True);
            Assert.That(pinned.Edition, Is.EqualTo("2026-Q1"));
            Assert.That(pinned.Artifact, Is.SameAs(first.Artifact));
            Assert.That(pinned.ExternalCodeValue?.Name, Is.EqualTo("FirstExternalName"));
            Assert.That(pinned.InternalCodeSet, Is.Null);
            Assert.That(latest.Success, Is.True, latest.ErrorMessage);
            Assert.That(latest.Found, Is.True);
            Assert.That(latest.Edition, Is.EqualTo("2026-Q2"));
            Assert.That(latest.ExternalCodeValue?.Name, Is.EqualTo("SecondExternalName"));
            Assert.That(wrongSource.Success, Is.True);
            Assert.That(wrongSource.Found, Is.False);
            Assert.That(wrongSource.Edition, Is.EqualTo("2026-06-26"));
            Assert.That(wrongSource.Artifact, Is.SameAs(snapshot.Provenance.Artifact));
        });
    }

    [Test]
    public void ActiveDateFilteringUsesInclusiveStartsExclusiveEndsAndInactivePolicy()
    {
        RepositorySnapshot snapshot = CreateSnapshot("2026-06-26", "ActiveInternal");
        CodeValueDefinition internalValue = snapshot.CodeSets[0].Codes[0];
        internalValue.Registration.RegistrationDate = CreateDate("2025-01-01");
        internalValue.Registration.RemovalDate = CreateDate("2026-01-01");
        CodeValueDefinition obsoleteInternal = new CodeValueDefinition
        {
            Identifier = "old-internal",
            Value = "OLD",
            Name = "OldInternal",
            Registration = new RegistrationMetadata
            {
                Status = RegistrationStatus.Obsolete
            }
        };
        snapshot.CodeSets[0].Codes.Add(obsoleteInternal);

        ExternalCodeCatalog catalog = CreateExternalCatalog("2026-Q1", "ActiveExternal");
        ExternalCodeValue externalValue = catalog.CodeSets[0].Values[0];
        externalValue.ActivationDate = CreateDate("2025-01-01");
        externalValue.DeactivationDate = CreateDate("2026-01-01");
        ExternalCodeValue obsoleteExternal = new ExternalCodeValue
        {
            Value = "OLD",
            Name = "OldExternal",
            RegistrationStatus = "Obsolete"
        };
        catalog.CodeSets[0].Values.Add(obsoleteExternal);
        ICodeSetProvider provider = new CodeSetProvider(
            new List<RepositorySnapshot> { snapshot },
            new List<ExternalCodeCatalog> { catalog });

        CodeLookupResult before = provider.LookupCode(CreateInternalDateRequest("2024-12-31", "ACTV"));
        CodeLookupResult start = provider.LookupCode(CreateInternalDateRequest("2025-01-01", "ACTV"));
        CodeLookupResult end = provider.LookupCode(CreateInternalDateRequest("2026-01-01", "ACTV"));
        CodeLookupResult externalStart = provider.LookupCode(
            CreateExternalDateRequest("2025-01-01", "ACTV", false));
        CodeLookupResult externalEndEvenWhenInactiveIncluded = provider.LookupCode(
            CreateExternalDateRequest("2026-01-01", "ACTV", true));
        CodeLookupResult inactiveExcluded = provider.LookupCode(new CodeLookupRequest
        {
            Kind = CodeSetKind.External,
            CodeSetIdentifier = "ExternalStatus1Code",
            Value = "OLD",
            Edition = "2026-Q1"
        });
        CodeLookupResult inactiveIncluded = provider.LookupCode(new CodeLookupRequest
        {
            Kind = CodeSetKind.External,
            CodeSetIdentifier = "ExternalStatus1Code",
            Value = "OLD",
            Edition = "2026-Q1",
            IncludeInactive = true
        });
        CodeLookupResult internalInactiveIncluded = provider.LookupCode(new CodeLookupRequest
        {
            Kind = CodeSetKind.Internal,
            CodeSetIdentifier = "InternalStatusCode",
            Value = "OLD",
            Edition = "2026-06-26",
            IncludeInactive = true
        });

        Assert.Multiple(() =>
        {
            Assert.That(before.Found, Is.False);
            Assert.That(start.Found, Is.True);
            Assert.That(start.InternalCodeValue, Is.SameAs(internalValue));
            Assert.That(end.Found, Is.False);
            Assert.That(externalStart.Found, Is.True);
            Assert.That(externalEndEvenWhenInactiveIncluded.Found, Is.False);
            Assert.That(inactiveExcluded.Found, Is.False);
            Assert.That(inactiveIncluded.Found, Is.True);
            Assert.That(inactiveIncluded.ExternalCodeValue, Is.SameAs(obsoleteExternal));
            Assert.That(internalInactiveIncluded.Found, Is.True);
            Assert.That(internalInactiveIncluded.InternalCodeValue, Is.SameAs(obsoleteInternal));
        });
    }

    [Test]
    public void DuplicateValuesAndEditionsAreReportedInsteadOfSelectedSilently()
    {
        ExternalCodeCatalog catalog = CreateExternalCatalog("2026-Q1", "Current");
        ExternalCodeValue obsolete = new ExternalCodeValue
        {
            Value = "CTP",
            Name = "Historical",
            RegistrationStatus = "Obsolete"
        };
        ExternalCodeValue registered = new ExternalCodeValue
        {
            Value = "CTP",
            Name = "Current",
            RegistrationStatus = "Registered"
        };
        catalog.CodeSets[0].Values.Clear();
        catalog.CodeSets[0].Values.Add(obsolete);
        catalog.CodeSets[0].Values.Add(registered);
        ICodeSetProvider provider = new CodeSetProvider(
            new List<RepositorySnapshot>(),
            new List<ExternalCodeCatalog> { catalog });

        CodeLookupResult active = provider.LookupCode(new CodeLookupRequest
        {
            Kind = CodeSetKind.External,
            CodeSetIdentifier = "ExternalStatus1Code",
            Value = "CTP",
            Edition = "2026-Q1"
        });
        CodeLookupResult ambiguousValue = provider.LookupCode(new CodeLookupRequest
        {
            Kind = CodeSetKind.External,
            CodeSetIdentifier = "ExternalStatus1Code",
            Value = "CTP",
            Edition = "2026-Q1",
            IncludeInactive = true
        });

        ExternalCodeCatalog duplicateEdition = CreateExternalCatalog("2026-Q1", "Duplicate");
        ICodeSetProvider ambiguousProvider = new CodeSetProvider(
            new List<RepositorySnapshot>(),
            new List<ExternalCodeCatalog> { catalog, duplicateEdition });
        CodeLookupResult ambiguousEdition = ambiguousProvider.LookupCode(new CodeLookupRequest
        {
            Kind = CodeSetKind.External,
            CodeSetIdentifier = "ExternalStatus1Code",
            Value = "CTP",
            SelectLatestEdition = true
        });

        Assert.Multiple(() =>
        {
            Assert.That(active.Success, Is.True, active.ErrorMessage);
            Assert.That(active.Found, Is.True);
            Assert.That(active.ExternalCodeValue, Is.SameAs(registered));
            Assert.That(ambiguousValue.Success, Is.False);
            Assert.That(ambiguousValue.ErrorCode, Is.EqualTo("code.lookup.value_ambiguous"));
            Assert.That(ambiguousValue.Edition, Is.EqualTo("2026-Q1"));
            Assert.That(ambiguousValue.Artifact, Is.SameAs(catalog.Artifact));
            Assert.That(ambiguousEdition.Success, Is.False);
            Assert.That(ambiguousEdition.ErrorCode, Is.EqualTo("code.lookup.edition_ambiguous"));
        });
    }

    [Test]
    public void InvalidPoliciesAndDatesFailWhileMissingEditionsAreSuccessfulNotFoundResults()
    {
        ICodeSetProvider provider = new CodeSetProvider(
            new List<RepositorySnapshot>(),
            new List<ExternalCodeCatalog>());
        CodeLookupResult missingRequest = provider.LookupCode(null);
        CodeLookupResult kind = provider.LookupCode(new CodeLookupRequest
        {
            Kind = CodeSetKind.Derived,
            CodeSetIdentifier = "Any",
            Value = "ANY",
            Edition = "2026"
        });
        CodeLookupResult conflictingEdition = provider.LookupCode(new CodeLookupRequest
        {
            Kind = CodeSetKind.External,
            CodeSetIdentifier = "Any",
            Value = "ANY",
            Edition = "2026-Q1",
            SelectLatestEdition = true
        });
        CodeLookupResult invalidDate = provider.LookupCode(new CodeLookupRequest
        {
            Kind = CodeSetKind.External,
            CodeSetIdentifier = "Any",
            Value = "ANY",
            Edition = "2026-Q1",
            ActiveOn = new IsoDateValue { Year = 2026, Month = 2, Day = 30 }
        });
        CodeLookupResult missingEdition = provider.LookupCode(new CodeLookupRequest
        {
            Kind = CodeSetKind.External,
            CodeSetIdentifier = "Any",
            Value = "ANY",
            Edition = "2026-Q1"
        });

        Assert.Multiple(() =>
        {
            Assert.That(missingRequest.ErrorCode, Is.EqualTo("code.lookup.request_missing"));
            Assert.That(kind.ErrorCode, Is.EqualTo("code.lookup.kind_invalid"));
            Assert.That(conflictingEdition.ErrorCode,
                Is.EqualTo("code.lookup.edition_policy_invalid"));
            Assert.That(invalidDate.ErrorCode, Is.EqualTo("code.lookup.active_date_invalid"));
            Assert.That(missingEdition.Success, Is.True);
            Assert.That(missingEdition.Found, Is.False);
            Assert.That(missingEdition.Edition, Is.Empty);
            Assert.That(
                () => new CodeSetProvider(null!, new List<ExternalCodeCatalog>()),
                Throws.InstanceOf<ArgumentNullException>());
            Assert.That(
                () => new CodeSetProvider(new List<RepositorySnapshot>(), null!),
                Throws.InstanceOf<ArgumentNullException>());
        });
    }

    [Test]
    public void MalformedCataloguesAndHelperBoundariesFailDeterministically()
    {
        CodeLookupRequest request = new CodeLookupRequest
        {
            Kind = CodeSetKind.Internal,
            CodeSetIdentifier = "set",
            Value = "value",
            Edition = "edition"
        };
        Assert.Multiple(() =>
        {
            Assert.That(CodeSetProvider.ValidateLookupRequest(new CodeLookupRequest
                { Value = "v", Edition = "e" })?.ErrorCode,
                Is.EqualTo("code.lookup.value_invalid"));
            Assert.That(CodeSetProvider.ValidateLookupRequest(new CodeLookupRequest
                { CodeSetIdentifier = "s", Edition = "e" })?.ErrorCode,
                Is.EqualTo("code.lookup.value_invalid"));
            Assert.That(CodeSetProvider.ValidateLookupRequest(new CodeLookupRequest
                { CodeSetIdentifier = "s", Value = "v", Edition = null! })?.ErrorCode,
                Is.EqualTo("code.lookup.value_invalid"));
            Assert.That(CodeSetProvider.ValidateLookupRequest(new CodeLookupRequest
                { CodeSetIdentifier = "", Value = "v", Edition = "e" })?.ErrorCode,
                Is.EqualTo("code.lookup.value_invalid"));
            Assert.That(CodeSetProvider.ValidateLookupRequest(new CodeLookupRequest
                { CodeSetIdentifier = "s", Value = "", Edition = "e" })?.ErrorCode,
                Is.EqualTo("code.lookup.value_invalid"));
            Assert.That(CodeSetProvider.ValidateLookupRequest(new CodeLookupRequest
                { Kind = CodeSetKind.Internal, CodeSetIdentifier = "s", Value = "v" })?
                .ErrorCode, Is.EqualTo("code.lookup.edition_policy_invalid"));
        });

        RepositorySnapshot invalidSnapshot = new RepositorySnapshot
            { Edition = "edition", Provenance = null! };
        CodeSetProvider invalidInternal = new CodeSetProvider(
            new List<RepositorySnapshot> { invalidSnapshot }, []);
        Assert.That(invalidInternal.LookupCode(request).ErrorCode,
            Is.EqualTo("code.lookup.catalog_invalid"));

        RepositorySnapshot duplicateSets = CreateSnapshot("edition", "one");
        duplicateSets.CodeSets.Add(duplicateSets.CodeSets[0]);
        request.CodeSetIdentifier = "internal-status-id";
        request.Value = "ACTV";
        Assert.That(new CodeSetProvider([duplicateSets], []).LookupCode(request).ErrorCode,
            Is.EqualTo("code.lookup.code_set_ambiguous"));

        RepositorySnapshot duplicateValues = CreateSnapshot("edition", "one");
        duplicateValues.CodeSets[0].Codes.Add(duplicateValues.CodeSets[0].Codes[0]);
        Assert.That(new CodeSetProvider([duplicateValues], []).LookupCode(request).ErrorCode,
            Is.EqualTo("code.lookup.value_ambiguous"));

        RepositorySnapshot badValues = CreateSnapshot("edition", "one");
        badValues.CodeSets[0].Codes.Add(null!);
        Assert.That(new CodeSetProvider([badValues], []).LookupCode(request).ErrorCode,
            Is.EqualTo("code.lookup.catalog_invalid"));

        RepositorySnapshot badCodeSet = CreateSnapshot("edition", "one");
        badCodeSet.CodeSets[0].Registration = null!;
        Assert.That(new CodeSetProvider([badCodeSet], []).LookupCode(request).ErrorCode,
            Is.EqualTo("code.lookup.catalog_invalid"));

        ExternalCodeCatalog external = CreateExternalCatalog("edition", "one");
        CodeLookupRequest externalRequest = new CodeLookupRequest
        {
            Kind = CodeSetKind.External,
            CodeSetIdentifier = "ExternalStatus1Code",
            Value = "ACTV",
            Edition = "edition"
        };
        external.CodeSets.Add(external.CodeSets[0]);
        Assert.That(new CodeSetProvider([], [external]).LookupCode(externalRequest).ErrorCode,
            Is.EqualTo("code.lookup.code_set_ambiguous"));
        external.CodeSets.RemoveAt(1);
        external.CodeSets[0].Values.Add(external.CodeSets[0].Values[0]);
        Assert.That(new CodeSetProvider([], [external]).LookupCode(externalRequest).ErrorCode,
            Is.EqualTo("code.lookup.value_ambiguous"));
        external.CodeSets[0].Values.Add(null!);
        Assert.That(new CodeSetProvider([], [external]).LookupCode(externalRequest).ErrorCode,
            Is.EqualTo("code.lookup.catalog_invalid"));

        AssertSelectionBoundaries(request, externalRequest);
        AssertLifecycleBoundaries();

        CodeSetProvider empty = new CodeSetProvider([], []);
        Assert.That(empty.SelectRepositorySnapshot(new CodeLookupRequest
            { SelectLatestEdition = true }).Found, Is.False);
        Assert.That(empty.SelectExternalCatalog(new CodeLookupRequest
            { SelectLatestEdition = true }).Found, Is.False);
        Assert.That(new CodeSetProvider([null!], []).LookupCode(request).ErrorCode,
            Is.EqualTo("code.lookup.catalog_invalid"));
        Assert.That(new CodeSetProvider([], [null!]).LookupCode(externalRequest).ErrorCode,
            Is.EqualTo("code.lookup.catalog_invalid"));
        Assert.That(empty.LookupCode(request).Found, Is.False);
        Assert.That(empty.LookupCode(externalRequest).Found, Is.False);

        CodeSetProvider latest = new CodeSetProvider(
            [CreateSnapshot("edition", "one")],
            [CreateExternalCatalog("edition", "one")]);
        Assert.Multiple(() =>
        {
            Assert.That(latest.SelectRepositorySnapshot(new CodeLookupRequest
                { SelectLatestEdition = true }).Found, Is.True);
            Assert.That(latest.SelectExternalCatalog(new CodeLookupRequest
                { SelectLatestEdition = true }).Found, Is.True);
        });

        ExternalCodeCatalog invalidExternal = CreateExternalCatalog("edition", "one");
        invalidExternal.Artifact = null!;
        Assert.That(new CodeSetProvider([], [invalidExternal]).LookupCode(externalRequest)
            .ErrorCode, Is.EqualTo("code.lookup.catalog_invalid"));
        ExternalCodeCatalog noExternalSet = CreateExternalCatalog("edition", "one");
        noExternalSet.CodeSets.Clear();
        Assert.That(new CodeSetProvider([], [noExternalSet]).LookupCode(externalRequest)
            .Found, Is.False);

        ExternalCodeCatalog inactiveExternal = CreateExternalCatalog("edition", "one");
        inactiveExternal.CodeSets[0].ActivationDate = CreateDate("2027-01-01");
        externalRequest.ActiveOn = CreateDate("2026-01-01");
        Assert.That(new CodeSetProvider([], [inactiveExternal]).LookupCode(externalRequest)
            .Found, Is.False);
    }

    private static void AssertSelectionBoundaries(
        CodeLookupRequest internalRequest,
        CodeLookupRequest externalRequest)
    {
        CodeSetProvider nullInternal = new CodeSetProvider([null!], []);
        CodeSetProvider emptyInternal = new CodeSetProvider(
            [new RepositorySnapshot { Edition = "" }], []);
        CodeSetProvider duplicateInternal = new CodeSetProvider(
            [CreateSnapshot("edition", "a"), CreateSnapshot("edition", "b")], []);
        CodeSetProvider nullExternal = new CodeSetProvider([], [null!]);
        CodeSetProvider emptyExternal = new CodeSetProvider([],
            [new ExternalCodeCatalog { Edition = "" }]);
        CodeSetProvider duplicateExternal = new CodeSetProvider([],
            [CreateExternalCatalog("edition", "a"),
                CreateExternalCatalog("edition", "b")]);
        CodeLookupRequest latestInternal = new CodeLookupRequest
        {
            Kind = CodeSetKind.Internal,
            CodeSetIdentifier = "set",
            Value = "value",
            SelectLatestEdition = true
        };
        CodeLookupRequest latestExternal = new CodeLookupRequest
        {
            Kind = CodeSetKind.External,
            CodeSetIdentifier = "set",
            Value = "value",
            SelectLatestEdition = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(nullInternal.SelectRepositorySnapshot(latestInternal).ErrorCode,
                Is.EqualTo("code.lookup.catalog_invalid"));
            Assert.That(emptyInternal.SelectRepositorySnapshot(latestInternal).ErrorCode,
                Is.EqualTo("code.lookup.catalog_invalid"));
            Assert.That(duplicateInternal.SelectRepositorySnapshot(internalRequest).ErrorCode,
                Is.EqualTo("code.lookup.edition_ambiguous"));
            Assert.That(nullExternal.SelectExternalCatalog(latestExternal).ErrorCode,
                Is.EqualTo("code.lookup.catalog_invalid"));
            Assert.That(emptyExternal.SelectExternalCatalog(latestExternal).ErrorCode,
                Is.EqualTo("code.lookup.catalog_invalid"));
            Assert.That(duplicateExternal.SelectExternalCatalog(externalRequest).ErrorCode,
                Is.EqualTo("code.lookup.edition_ambiguous"));
        });
    }

    private static void AssertLifecycleBoundaries()
    {
        IsoDateValue early = CreateDate("2024-01-01");
        IsoDateValue middle = CreateDate("2025-02-02");
        IsoDateValue late = CreateDate("2026-03-03");
        IsoDateValue invalid = new IsoDateValue { Year = 2025, Month = 2, Day = 30 };
        CodeLookupResult result = new CodeLookupResult();
        CodeLookupRequest request = new CodeLookupRequest { ActiveOn = middle };
        CodeSetDefinition internalSet = new CodeSetDefinition
        {
            Registration = new RegistrationMetadata
                { RegistrationDate = late }
        };
        ExternalCodeSet externalSet = new ExternalCodeSet
        {
            Identifier = "set",
            Name = "set",
            RegistrationStatus = "Registered",
            ActivationDate = late
        };
        Assert.Multiple(() =>
        {
            Assert.That(CodeSetProvider.SelectEarlierLookupEndDate(null, late), Is.SameAs(late));
            Assert.That(CodeSetProvider.SelectEarlierLookupEndDate(early, null), Is.SameAs(early));
            Assert.That(CodeSetProvider.SelectEarlierLookupEndDate(early, late), Is.SameAs(early));
            Assert.That(CodeSetProvider.SelectEarlierLookupEndDate(late, early), Is.SameAs(early));
            Assert.That(CodeSetProvider.LifecycleDatesAreValid(invalid, null, null), Is.False);
            Assert.That(CodeSetProvider.LifecycleDatesAreValid(null, invalid, null), Is.False);
            Assert.That(CodeSetProvider.LifecycleDatesAreValid(null, null, invalid), Is.False);
            Assert.That(CodeSetProvider.LifecycleDatesAreValid(late, early, null), Is.False);
            Assert.That(CodeSetProvider.LifecycleDatesAreValid(late, null, early), Is.False);
            Assert.That(CodeSetProvider.LifecycleDateMatches(late, null, middle), Is.False);
            Assert.That(CodeSetProvider.LifecycleDateMatches(null, early, middle), Is.False);
            Assert.That(CodeSetProvider.CompareLookupDates(early, late), Is.LessThan(0));
            Assert.That(CodeSetProvider.CompareLookupDates(late, early), Is.GreaterThan(0));
            Assert.That(CodeSetProvider.CompareLookupDates(middle,
                new IsoDateValue { Year = 2025, Month = 3, Day = 2 }), Is.LessThan(0));
            Assert.That(CodeSetProvider.CompareLookupDates(middle,
                new IsoDateValue { Year = 2025, Month = 2, Day = 3 }), Is.LessThan(0));
            Assert.That(CodeSetProvider.InternalRegistrationIsInactive(
                new RegistrationMetadata { IsDeprecated = true }), Is.True);
            Assert.That(CodeSetProvider.ExternalStatusIsInactive("Inactive"), Is.True);
            Assert.That(CodeSetProvider.InternalCodeSetIsEligible(
                new CodeSetDefinition { Registration = null! }, request, result), Is.False);
            Assert.That(CodeSetProvider.ExternalCodeSetIsEligible(
                new ExternalCodeSet { Identifier = null! }, request, result), Is.False);
            Assert.That(CodeSetProvider.InternalCodeSetIsEligible(
                internalSet, request, result), Is.False);
            Assert.That(CodeSetProvider.ExternalCodeSetIsEligible(
                externalSet, request, result), Is.False);
        });
    }

    private static RepositorySnapshot CreateSnapshot(string edition, string valueName)
    {
        RepositorySnapshot snapshot = new RepositorySnapshot
        {
            Edition = edition,
            Provenance = new RepositoryProvenance
            {
                Artifact = CreateManifest(edition, "repository")
            }
        };
        CodeSetDefinition codeSet = new CodeSetDefinition
        {
            Identifier = "internal-status-id",
            Kind = CodeSetKind.Internal,
            Name = "InternalStatusCode",
            Registration = new RegistrationMetadata
            {
                Status = RegistrationStatus.Registered
            }
        };
        codeSet.Codes.Add(new CodeValueDefinition
        {
            Identifier = "internal-active",
            Value = "ACTV",
            Name = valueName,
            Registration = new RegistrationMetadata
            {
                Status = RegistrationStatus.Registered
            }
        });
        snapshot.CodeSets.Add(codeSet);
        return snapshot;
    }

    private static ExternalCodeCatalog CreateExternalCatalog(string edition, string valueName)
    {
        ExternalCodeCatalog catalog = new ExternalCodeCatalog
        {
            Edition = edition,
            SourceFormat = "XLSX",
            Artifact = CreateManifest(edition, "external")
        };
        ExternalCodeSet codeSet = new ExternalCodeSet
        {
            Identifier = "ExternalStatus1Code",
            Name = "ExternalStatus1Code",
            RegistrationStatus = "Registered"
        };
        codeSet.Values.Add(new ExternalCodeValue
        {
            Value = "ACTV",
            Name = valueName,
            RegistrationStatus = "Registered"
        });
        catalog.CodeSets.Add(codeSet);
        return catalog;
    }

    private static ArtifactManifest CreateManifest(string edition, string identifier)
    {
        return new ArtifactManifest
        {
            Coordinate = new ArtifactCoordinate
            {
                Kind = identifier == "repository" ? ArtifactKind.Repository : ArtifactKind.ExternalCodeSet,
                Edition = edition,
                Identifier = identifier,
                Version = "1"
            },
            OriginUri = "https://example.test/" + identifier + "/" + edition,
            IsApproved = true
        };
    }

    private static IsoDateValue CreateDate(string lexical)
    {
        string[] parts = lexical.Split('-');
        return new IsoDateValue
        {
            Year = int.Parse(parts[0]),
            Month = int.Parse(parts[1]),
            Day = int.Parse(parts[2]),
            LexicalValue = lexical
        };
    }

    private static CodeLookupRequest CreateInternalDateRequest(string activeOn, string value)
    {
        return new CodeLookupRequest
        {
            Kind = CodeSetKind.Internal,
            CodeSetIdentifier = "InternalStatusCode",
            Value = value,
            Edition = "2026-06-26",
            ActiveOn = CreateDate(activeOn)
        };
    }

    private static CodeLookupRequest CreateExternalDateRequest(
        string activeOn,
        string value,
        bool includeInactive)
    {
        return new CodeLookupRequest
        {
            Kind = CodeSetKind.External,
            CodeSetIdentifier = "ExternalStatus1Code",
            Value = value,
            Edition = "2026-Q1",
            ActiveOn = CreateDate(activeOn),
            IncludeInactive = includeInactive
        };
    }
}
