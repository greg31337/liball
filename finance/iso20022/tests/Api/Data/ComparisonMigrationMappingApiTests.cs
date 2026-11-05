using iso20022;

namespace iso20022.Tests;

public class ComparisonMigrationMappingApiTests
{
    [Test]
    public void ComparisonDataCarriesMatchesDifferencesCompatibilityAndOverrides()
    {
        RepositorySnapshot source = new RepositorySnapshot
        {
            Identifier = "source-repository",
            Edition = "2025"
        };
        RepositorySnapshot target = new RepositorySnapshot
        {
            Identifier = "target-repository",
            Edition = "2026"
        };
        MessageIdentifier sourceMessage = Identifier("pacs", "008", "001", "13");
        MessageIdentifier targetMessage = Identifier("pacs", "008", "001", "14");
        MappingRuleDefinition overrideRule = new MappingRuleDefinition
        {
            Identifier = "override.account",
            Action = MappingActionKind.Rename,
            SourceIdentifier = "old-id",
            TargetIdentifier = "new-id",
            EntityKind = RepositoryEntityKind.MessageElement,
            SourceMember = "OldAccount",
            TargetMember = "Account",
            Required = true,
            Rationale = "The repository identifiers changed during remodeling."
        };
        MappingDefinition overrides = new MappingDefinition
        {
            Identifier = "repository-overrides",
            Version = "1"
        };
        overrides.Rules.Add(overrideRule);
        RepositoryComparisonRequest request = new RepositoryComparisonRequest
        {
            Source = source,
            Target = target,
            SourceMessageIdentifier = sourceMessage,
            TargetMessageIdentifier = targetMessage,
            BaselineProfile = new ProfileDefinition { Identifier = "baseline" },
            LocalProfile = new ProfileDefinition { Identifier = "local" },
            TargetProfile = new ProfileDefinition { Identifier = "target" },
            MatchOverrides = overrides,
            MinimumMatchConfidence = 0.90m,
            IncludeUnchanged = true
        };
        request.MatchOverrideRules.Add(overrideRule);
        request.Limits.MaxCollectionCount = 25;
        DifferenceItem difference = new DifferenceItem
        {
            Kind = DifferenceKind.Renamed,
            EntityKind = RepositoryEntityKind.MessageElement,
            SourceIdentifier = "old-id",
            TargetIdentifier = "new-id",
            SourceName = "OldAccount",
            TargetName = "Account",
            PropertyName = "Name",
            SourceValue = "OldAccount",
            TargetValue = "Account",
            MatchConfidence = 0.97m,
            SemanticFallbackMatch = true,
            OverrideMatch = true,
            BreakingChange = true,
            Rationale = "Qualified type and containing component match."
        };
        difference.SourcePath.Value = "/Document/OldAccount";
        difference.TargetPath.Value = "/Document/Account";
        difference.AlternativeTargetIdentifiers.Add("alternate-id");
        CompatibilityAssessment compatibility = new CompatibilityAssessment
        {
            Kind = CompatibilityKind.ConditionallyCompatible,
            ReaderCompatible = false,
            WriterCompatible = true,
            SchemaCompatible = false,
            Lossless = true,
            SemanticallyCompatible = true,
            Confidence = 0.95m
        };
        compatibility.AffectedPaths.Add(difference.TargetPath);
        compatibility.Differences.Add(difference);
        compatibility.Rationale.Add("The renamed element requires migration.");
        RepositoryDiff diff = new RepositoryDiff
        {
            Success = true,
            SourceEdition = source.Edition,
            TargetEdition = target.Edition,
            HasAmbiguousMatches = true
        };
        diff.Differences.Add(difference);
        diff.Compatibility.Add(compatibility);
        RepositoryComparisonRequest defaults = new RepositoryComparisonRequest();

        Assert.Multiple(() =>
        {
            Assert.That(request.Source, Is.SameAs(source));
            Assert.That(request.Target, Is.SameAs(target));
            Assert.That(request.SourceMessageIdentifier, Is.SameAs(sourceMessage));
            Assert.That(request.TargetMessageIdentifier, Is.SameAs(targetMessage));
            Assert.That(request.MatchOverrides, Is.SameAs(overrides));
            Assert.That(request.MatchOverrideRules.Single(), Is.SameAs(overrideRule));
            Assert.That(overrideRule.SourceIdentifier, Is.EqualTo("old-id"));
            Assert.That(overrideRule.TargetIdentifier, Is.EqualTo("new-id"));
            Assert.That(overrideRule.EntityKind,
                Is.EqualTo(RepositoryEntityKind.MessageElement));
            Assert.That(request.MinimumMatchConfidence, Is.EqualTo(0.90m));
            Assert.That(request.Limits.MaxCollectionCount, Is.EqualTo(25));
            Assert.That(defaults.MinimumMatchConfidence, Is.EqualTo(0.80m));
            Assert.That(defaults.DetectRenames, Is.True);
            Assert.That(defaults.DetectMoves, Is.True);
            Assert.That(defaults.AssessCompatibility, Is.True);
            Assert.That(defaults.MatchOverrideRules,
                Is.Not.SameAs(request.MatchOverrideRules));
            Assert.That(defaults.Limits, Is.Not.SameAs(request.Limits));
            Assert.That(diff.Differences.Single(), Is.SameAs(difference));
            Assert.That(diff.Compatibility.Single(), Is.SameAs(compatibility));
            Assert.That(difference.AlternativeTargetIdentifiers.Single(),
                Is.EqualTo("alternate-id"));
            Assert.That(compatibility.Differences.Single(), Is.SameAs(difference));
            Assert.That(compatibility.AffectedPaths.Single(),
                Is.SameAs(difference.TargetPath));
        });
    }

    [Test]
    public void MigrationDataCarriesPlanExecutionUnmappedContentAndValidation()
    {
        MessageIdentifier sourceIdentifier =
            Identifier("pacs", "008", "001", "13");
        MessageIdentifier targetIdentifier =
            Identifier("pacs", "008", "001", "14");
        DifferenceItem cause = new DifferenceItem
        {
            Kind = DifferenceKind.CodeChanged,
            SourceIdentifier = "old-status",
            TargetIdentifier = "new-status"
        };
        MappingRuleDefinition mappingRule = new MappingRuleDefinition
        {
            Identifier = "map.status",
            Action = MappingActionKind.CodeMap,
            Required = true
        };
        mappingRule.SourceCodeValues.Add("OLD");
        mappingRule.TargetCodeValues.Add("NEW");
        MigrationStep step = new MigrationStep
        {
            Sequence = 0,
            Action = MigrationActionKind.CodeMap,
            SourceIdentifier = "old-status",
            TargetIdentifier = "new-status",
            SourceType = "StatusCode13",
            TargetType = "StatusCode14",
            Required = true,
            Difference = cause,
            MappingRule = mappingRule,
            Rationale = "The target renamed the status code."
        };
        step.SourcePaths.Add(new XmlPathData { Value = "/Document/Status" });
        step.TargetPaths.Add(new XmlPathData { Value = "/Document/Status" });
        step.SourceCodeValues.Add("OLD");
        step.TargetCodeValues.Add("NEW");
        CompatibilityAssessment assessment = new CompatibilityAssessment
        {
            Kind = CompatibilityKind.CompatibleWithWarnings,
            ReaderCompatible = true,
            WriterCompatible = true,
            SchemaCompatible = true,
            Lossless = true,
            SemanticallyCompatible = true
        };
        MigrationPlan plan = new MigrationPlan
        {
            Success = true,
            SourceMessageIdentifier = sourceIdentifier,
            TargetMessageIdentifier = targetIdentifier,
            Difference = new RepositoryDiff { Success = true },
            Compatibility = assessment,
            SupplementaryFallbackAuthorized = true
        };
        plan.Steps.Add(step);
        MigrationRequest request = new MigrationRequest
        {
            SourceRepository = new RepositorySnapshot { Edition = "2025" },
            TargetRepository = new RepositorySnapshot { Edition = "2026" },
            SourceMessageIdentifier = sourceIdentifier,
            TargetMessageIdentifier = targetIdentifier,
            Xml = "<Document><Status>OLD</Status></Document>"u8.ToArray(),
            Difference = plan.Difference,
            Plan = plan,
            AllowSupplementaryFallback = true,
            Validator = new StubMessageValidator(new ValidationReport
            {
                Success = true,
                Valid = true
            })
        };
        ValidationReport validation = new ValidationReport
        {
            Success = true,
            Valid = true,
            MessageIdentifier = targetIdentifier
        };
        RawXmlFragment unmapped = new RawXmlFragment
        {
            Xml = "<Extra>retained</Extra>"
        };
        MigrationResult result = new MigrationResult
        {
            Success = true,
            SourceMessageIdentifier = sourceIdentifier,
            TargetMessageIdentifier = targetIdentifier,
            Plan = plan,
            Xml = "<Document><Status>NEW</Status></Document>"u8.ToArray(),
            SupplementaryFallbackUsed = true,
            TargetValidation = validation
        };
        result.AppliedSteps.Add(step);
        result.UnmappedSourcePaths.Add(new XmlPathData
        {
            Value = "/Document/Extra"
        });
        result.UnmappedContent.Add(unmapped);
        MigrationRequest defaults = new MigrationRequest();

        Assert.Multiple(() =>
        {
            Assert.That(request.Plan, Is.SameAs(plan));
            Assert.That(request.AllowSupplementaryFallback, Is.True);
            Assert.That(request.ValidateTarget, Is.True);
            Assert.That(request.TargetValidationPolicy, Is.Not.Null);
            Assert.That(request.ValidationContext, Is.Not.Null);
            Assert.That(plan.Steps.Single(), Is.SameAs(step));
            Assert.That(plan.Compatibility, Is.SameAs(assessment));
            Assert.That(step.Difference, Is.SameAs(cause));
            Assert.That(step.MappingRule, Is.SameAs(mappingRule));
            Assert.That(step.SourceCodeValues.Single(), Is.EqualTo("OLD"));
            Assert.That(step.TargetCodeValues.Single(), Is.EqualTo("NEW"));
            Assert.That(result.AppliedSteps.Single(), Is.SameAs(step));
            Assert.That(result.UnmappedSourcePaths.Single().Value,
                Is.EqualTo("/Document/Extra"));
            Assert.That(result.UnmappedContent.Single(), Is.SameAs(unmapped));
            Assert.That(result.TargetValidation, Is.SameAs(validation));
            Assert.That(defaults.Xml, Is.Empty);
            Assert.That(defaults.ValidateTarget, Is.True);
            Assert.That(defaults.Limits, Is.Not.SameAs(request.Limits));
            Assert.That(defaults.TargetValidationPolicy,
                Is.Not.SameAs(request.TargetValidationPolicy));
        });
    }

    [Test]
    public void MappingDataCarriesExplicitRulesOutputsAndMissingContent()
    {
        MappingRuleDefinition copy = new MappingRuleDefinition
        {
            Identifier = "copy.amount",
            Sequence = 0,
            Action = MappingActionKind.Copy,
            SourceMember = "Amount",
            TargetMember = "Amount",
            Required = true
        };
        copy.SourcePaths.Add(new XmlPathData { Value = "/Source/Amount" });
        copy.TargetPaths.Add(new XmlPathData { Value = "/Target/Amount" });
        MappingRuleDefinition constant = new MappingRuleDefinition
        {
            Identifier = "default.currency",
            Sequence = 1,
            Action = MappingActionKind.Constant,
            ConstantValue = "USD"
        };
        MappingDefinition definition = new MappingDefinition
        {
            Identifier = "canonical-payment",
            Version = "2",
            Definition = "Maps an ISO payment into a caller canonical model.",
            SourceMessageIdentifier = Identifier("pain", "001", "001", "11"),
            TargetModelName = "CanonicalPayment",
            AllowUnmappedSource = true,
            Artifact = new ArtifactManifest
            {
                Coordinate = new ArtifactCoordinate
                {
                    Identifier = "mapping-artifact"
                }
            }
        };
        definition.Rules.Add(copy);
        definition.Rules.Add(constant);
        StubMessageValidator validator = new StubMessageValidator(
            new ValidationReport { Success = true, Valid = true });
        MappingRequest request = new MappingRequest
        {
            Xml = "<Document><Amount>10</Amount></Document>"u8.ToArray(),
            Mapping = definition,
            Validator = validator,
            TargetMessageIdentifier = Identifier("pacs", "008", "001", "14")
        };
        MappingResult result = new MappingResult
        {
            Success = true,
            Mapping = definition,
            TargetMessageIdentifier = request.TargetMessageIdentifier,
            Model = new object(),
            DataLoss = true,
            TargetValidation = validator.Report
        };
        result.AppliedRules.Add(copy);
        result.FailedRules.Add(constant);
        result.UnmappedSourcePaths.Add(new XmlPathData { Value = "/Source/Extra" });
        result.MissingTargetPaths.Add(new XmlPathData { Value = "/Target/Currency" });
        result.Diagnostics.Add(new ValidationDiagnostic
        {
            Code = "mapping.target_missing",
            Stage = ValidationStage.Structural
        });
        MappingDefinition defaults = new MappingDefinition();
        MappingRequest defaultRequest = new MappingRequest();

        Assert.Multiple(() =>
        {
            Assert.That(definition.Rules, Is.EqualTo(new[] { copy, constant }));
            Assert.That(definition.Artifact!.Coordinate.Identifier,
                Is.EqualTo("mapping-artifact"));
            Assert.That(definition.RequireAllTargets, Is.True);
            Assert.That(copy.SourcePaths.Single().Value,
                Is.EqualTo("/Source/Amount"));
            Assert.That(copy.TargetPaths.Single().Value,
                Is.EqualTo("/Target/Amount"));
            Assert.That(request.Mapping, Is.SameAs(definition));
            Assert.That(request.Validator, Is.SameAs(validator));
            Assert.That(result.AppliedRules.Single(), Is.SameAs(copy));
            Assert.That(result.FailedRules.Single(), Is.SameAs(constant));
            Assert.That(result.UnmappedSourcePaths.Single().Value,
                Is.EqualTo("/Source/Extra"));
            Assert.That(result.MissingTargetPaths.Single().Value,
                Is.EqualTo("/Target/Currency"));
            Assert.That(result.TargetValidation, Is.SameAs(validator.Report));
            Assert.That(defaults.Rules, Is.Not.SameAs(definition.Rules));
            Assert.That(defaultRequest.ValidateTarget, Is.True);
            Assert.That(defaultRequest.Limits, Is.Not.SameAs(request.Limits));
        });
    }

    [Test]
    public void ComparisonMigrationAndMappingContractsReturnExactResults()
    {
        RepositoryComparisonRequest comparisonRequest =
            new RepositoryComparisonRequest();
        RepositoryDiff repositoryDiff = new RepositoryDiff { Success = true };
        RepositoryDiff messageDiff = new RepositoryDiff { Success = true };
        StubRepositoryComparer repository =
            new StubRepositoryComparer(repositoryDiff);
        StubMessageVersionComparer message =
            new StubMessageVersionComparer(messageDiff);
        MigrationRequest migrationRequest = new MigrationRequest();
        MigrationPlan plan = new MigrationPlan { Success = true };
        MigrationResult migrationResult = new MigrationResult { Success = true };
        StubMigrationPlanner planner = new StubMigrationPlanner(plan);
        StubMessageMigrator migrator = new StubMessageMigrator(migrationResult);
        MappingRequest mappingRequest = new MappingRequest();
        MappingResult mappingResult = new MappingResult { Success = true };
        StubMessageMapper mapper = new StubMessageMapper(mappingResult);
        IRepositoryComparer repositoryContract = repository;
        IMessageVersionComparer messageContract = message;
        IMigrationPlanner plannerContract = planner;
        IMessageMigrator migratorContract = migrator;
        IMessageMapper mapperContract = mapper;

        RepositoryDiff actualRepository =
            repositoryContract.CompareRepositories(comparisonRequest);
        RepositoryDiff actualMessage =
            messageContract.CompareMessageVersions(comparisonRequest);
        MigrationPlan actualPlan = plannerContract.PlanMigration(migrationRequest);
        MigrationResult actualMigration =
            migratorContract.MigrateMessage(migrationRequest);
        MappingResult actualMapping = mapperContract.MapMessage(mappingRequest);

        Assert.Multiple(() =>
        {
            Assert.That(actualRepository, Is.SameAs(repositoryDiff));
            Assert.That(repository.Request, Is.SameAs(comparisonRequest));
            Assert.That(actualMessage, Is.SameAs(messageDiff));
            Assert.That(message.Request, Is.SameAs(comparisonRequest));
            Assert.That(actualPlan, Is.SameAs(plan));
            Assert.That(planner.Request, Is.SameAs(migrationRequest));
            Assert.That(actualMigration, Is.SameAs(migrationResult));
            Assert.That(migrator.Request, Is.SameAs(migrationRequest));
            Assert.That(actualMapping, Is.SameAs(mappingResult));
            Assert.That(mapper.Request, Is.SameAs(mappingRequest));
        });
    }

    private static MessageIdentifier Identifier(
        string area,
        string functionality,
        string variant,
        string version)
    {
        return new MessageIdentifier
        {
            BusinessArea = area,
            MessageFunctionality = functionality,
            Variant = variant,
            Version = version,
            Value = area + "." + functionality + "." + variant + "." + version
        };
    }

    private class StubRepositoryComparer : IRepositoryComparer
    {
        internal RepositoryComparisonRequest? Request;
        internal RepositoryDiff Result;

        internal StubRepositoryComparer(RepositoryDiff result)
        {
            Result = result;
        }

        public RepositoryDiff CompareRepositories(
            RepositoryComparisonRequest? request)
        {
            Request = request;
            return Result;
        }
    }

    private class StubMessageVersionComparer : IMessageVersionComparer
    {
        internal RepositoryComparisonRequest? Request;
        internal RepositoryDiff Result;

        internal StubMessageVersionComparer(RepositoryDiff result)
        {
            Result = result;
        }

        public RepositoryDiff CompareMessageVersions(
            RepositoryComparisonRequest? request)
        {
            Request = request;
            return Result;
        }
    }

    private class StubMigrationPlanner : IMigrationPlanner
    {
        internal MigrationRequest? Request;
        internal MigrationPlan Result;

        internal StubMigrationPlanner(MigrationPlan result)
        {
            Result = result;
        }

        public MigrationPlan PlanMigration(MigrationRequest? request)
        {
            Request = request;
            return Result;
        }
    }

    private class StubMessageMigrator : IMessageMigrator
    {
        internal MigrationRequest? Request;
        internal MigrationResult Result;

        internal StubMessageMigrator(MigrationResult result)
        {
            Result = result;
        }

        public MigrationResult MigrateMessage(MigrationRequest? request)
        {
            Request = request;
            return Result;
        }
    }

    private class StubMessageMapper : IMessageMapper
    {
        internal MappingRequest? Request;
        internal MappingResult Result;

        internal StubMessageMapper(MappingResult result)
        {
            Result = result;
        }

        public MappingResult MapMessage(MappingRequest? request)
        {
            Request = request;
            return Result;
        }
    }

    private class StubMessageValidator : IMessageValidator
    {
        internal ValidationReport Report;

        internal StubMessageValidator(ValidationReport report)
        {
            Report = report;
        }

        public ValidationReport ValidateMessage(ValidationRequest? request)
        {
            return Report;
        }
    }
}
