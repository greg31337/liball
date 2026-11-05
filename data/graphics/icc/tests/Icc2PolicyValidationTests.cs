using icc;

namespace icc.Tests;

public class Icc2PolicyValidationTests
{
    [Test]
    public void TagDeclarationsRequirePermitAndForbidConfiguredSignatures()
    {
        IccProfile profile = CreateProfile(IccVersionKind.Version5);
        profile.Tags = new IccTag[]
        {
            CreateTag(IccTagSignatures.ProfileDescription),
            CreateTag(IccTagSignatures.Metadata),
            CreateTag(IccTagSignatures.Technology)
        };
        IccValidationPolicy policy = new IccValidationPolicy
        {
            Icc2RequiredTagSignatures = new uint[] { IccTagSignatures.Copyright },
            Icc2OptionalTagSignatures = new uint[]
            {
                IccTagSignatures.ProfileDescription
            },
            Icc2ForbiddenTagSignatures = new uint[] { IccTagSignatures.Metadata },
            Icc2ForbidUnlistedTags = true
        };

        IccValidationIssue[] issues = Icc2PolicyValidationCore.Validate(profile, policy);
        IccValidationReport report = new IccProfileValidator()
            .ValidateProfileDataReport(profile, policy);

        Assert.Multiple(() =>
        {
            Assert.That(issues.Count(issue => issue.RuleId
                == IccValidationRuleIds.RequiredTag), Is.EqualTo(1));
            Assert.That(issues.Count(issue => issue.RuleId
                == IccValidationRuleIds.ForbiddenTag), Is.EqualTo(2));
            Assert.That(issues.Any(issue => issue.Message.Contains("'meta'")
                && issue.Message.Contains("explicitly forbidden")), Is.True);
            Assert.That(issues.Any(issue => issue.Message.Contains("'tech'")
                && issue.Message.Contains("absent")), Is.True);
            Assert.That(issues.Any(issue => issue.Message.Contains("'desc'")), Is.False);
            Assert.That(report.Issues.Any(issue => issue.RuleId
                == IccValidationRuleIds.RequiredTag
                && issue.Message.Contains("selected ICC.2 policy")), Is.True);
        });
    }

    [Test]
    public void SupportedElementPolicyTraversesHierarchiesAndCalculatorSubElements()
    {
        IccProcessingElement allowed = CreateElement(
            IccProcessingElementSignatures.CurveSet, 2, 2);
        IccProcessingElement calculator = CreateElement(
            IccProcessingElementSignatures.Calculator, 2, 2);
        calculator.Calculator = new IccCalculatorProgram
        {
            SubElements = new IccProcessingElement[]
            {
                CreateElement(IccProcessingElementSignatures.Matrix, 2, 2)
            }
        };
        IccTagData nested = new IccTagData
        {
            Kind = IccTagDataKind.MultiProcessElements,
            MultiProcessElements = CreatePipeline(2, 2,
                new IccProcessingElement[] { allowed, calculator })
        };
        IccTag tag = CreateTag(0x706f6c79u);
        tag.Data.Kind = IccTagDataKind.TagStructure;
        tag.Data.TagStructure = new IccTagStructure
        {
            Members = new IccStructureMember[]
            {
                new IccStructureMember { Signature = 0x6d706520u, Data = nested }
            }
        };
        IccProfile profile = CreateProfile(IccVersionKind.Version5);
        profile.Tags = new IccTag[] { tag };
        IccValidationPolicy policy = new IccValidationPolicy
        {
            Icc2SupportedProcessingElementSignatures = new uint[]
            {
                IccProcessingElementSignatures.CurveSet,
                IccProcessingElementSignatures.Calculator
            }
        };

        IccValidationIssue[] issues = Icc2PolicyValidationCore.Validate(profile, policy);
        IccValidationPolicy noneSupported = new IccValidationPolicy
        {
            Icc2ForbidUnlistedProcessingElements = true
        };
        IccValidationIssue[] noneIssues = Icc2PolicyValidationCore.Validate(profile,
            noneSupported);

        Assert.Multiple(() =>
        {
            Assert.That(issues.Count(issue => issue.RuleId
                == IccValidationRuleIds.ForbiddenTag), Is.EqualTo(1));
            Assert.That(issues[0].Message, Does.Contain("calculator sub-element"));
            Assert.That(issues[0].Message, Does.Contain("'matf'"));
            Assert.That(issues[0].Path, Does.Contain("Calculator.SubElements[0]"));
            Assert.That(noneIssues.Count(issue => issue.RuleId
                == IccValidationRuleIds.ForbiddenTag), Is.EqualTo(3));
        });
    }

    [Test]
    public void ChannelRulesApplySeparatelyToPipelinesAndMatchingElements()
    {
        IccProcessingElement matrix = CreateElement(
            IccProcessingElementSignatures.Matrix, 3, 3);
        IccTag tag = CreateTag(IccTagSignatures.AToB0);
        tag.Data.Kind = IccTagDataKind.MultiProcessElements;
        tag.Data.MultiProcessElements = CreatePipeline(3, 3,
            new IccProcessingElement[] { matrix });
        IccProfile profile = CreateProfile(IccVersionKind.Version5);
        profile.Tags = new IccTag[] { tag };
        IccValidationPolicy policy = new IccValidationPolicy
        {
            Icc2ChannelRules = new IccValidationChannelRule[]
            {
                new IccValidationChannelRule
                {
                    TagSignature = IccTagSignatures.AToB0,
                    InputChannels = 4,
                    OutputChannels = 3
                },
                new IccValidationChannelRule
                {
                    TagSignature = IccTagSignatures.AToB0,
                    ProcessingElementSignature = IccProcessingElementSignatures.Matrix,
                    InputChannels = 3,
                    OutputChannels = 4
                }
            }
        };

        IccValidationIssue[] issues = Icc2PolicyValidationCore.Validate(profile, policy);

        Assert.Multiple(() =>
        {
            Assert.That(issues.Count(issue => issue.RuleId
                == IccValidationRuleIds.ChannelCount), Is.EqualTo(2));
            Assert.That(issues.Any(issue => issue.Message.Contains("pipeline channels 4-to-3")),
                Is.True);
            Assert.That(issues.Any(issue => issue.Message.Contains("'matf'")
                && issue.Message.Contains("3-to-4")), Is.True);
        });
    }

    [Test]
    public void SubclassPolicyDistinguishesMissingUnsupportedAndContextMismatch()
    {
        uint signature = 0x73756231u;
        IccValidationPolicy policy = new IccValidationPolicy
        {
            Icc2RequireProfileSubclass = true,
            Icc2ProfileSubclassRules = new IccValidationSubclassRule[]
            {
                new IccValidationSubclassRule
                {
                    ProfileSubclassSignature = signature,
                    MinimumMajorVersion = 1,
                    MinimumMinorVersion = 2,
                    MaximumMajorVersion = 1,
                    MaximumMinorVersion = 4,
                    ProfileClass = IccProfileClass.Output,
                    DataColorSpaceSignature = IccColorSpaceSignatures.Cmyk,
                    ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Lab
                }
            }
        };
        IccProfile missing = CreateProfile(IccVersionKind.Version5);
        IccProfile unsupported = CreateProfile(IccVersionKind.Version5);
        unsupported.Header.ProfileSubclassSignature = 0x6f746872u;
        unsupported.Header.Version.SubclassMajor = 1;
        unsupported.Header.Version.SubclassMinor = 3;
        IccProfile mismatch = CreateProfile(IccVersionKind.Version5);
        mismatch.Header.ProfileSubclassSignature = signature;
        mismatch.Header.Version.SubclassMajor = 1;
        mismatch.Header.Version.SubclassMinor = 1;
        IccProfile accepted = CreateProfile(IccVersionKind.Version5);
        accepted.Header.ProfileSubclassSignature = signature;
        accepted.Header.Version.SubclassMajor = 1;
        accepted.Header.Version.SubclassMinor = 3;
        accepted.Header.ProfileClass = IccProfileClass.Output;
        accepted.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Cmyk;
        accepted.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Lab;

        IccValidationIssue[] missingIssues = Icc2PolicyValidationCore.Validate(missing, policy);
        IccValidationIssue[] unsupportedIssues = Icc2PolicyValidationCore.Validate(unsupported, policy);
        IccValidationIssue[] mismatchIssues = Icc2PolicyValidationCore.Validate(mismatch, policy);
        IccValidationIssue[] acceptedIssues = Icc2PolicyValidationCore.Validate(accepted, policy);

        Assert.Multiple(() =>
        {
            Assert.That(missingIssues.Single().Message, Does.Contain("requires a profile subclass"));
            Assert.That(unsupportedIssues.Single().Message, Does.Contain("not supported"));
            Assert.That(mismatchIssues.Single().Message, Does.Contain("does not satisfy"));
            Assert.That(acceptedIssues, Is.Empty);
        });
    }

    [Test]
    public void Icc2PolicyDeclarationsDoNotApplyToEarlierProfileGenerations()
    {
        IccProfile profile = CreateProfile(IccVersionKind.Version4);
        profile.Tags = new IccTag[] { CreateTag(IccTagSignatures.Metadata) };
        IccValidationPolicy policy = new IccValidationPolicy
        {
            Icc2RequiredTagSignatures = new uint[] { IccTagSignatures.Copyright },
            Icc2ForbiddenTagSignatures = new uint[] { IccTagSignatures.Metadata },
            Icc2RequireProfileSubclass = true
        };

        IccValidationIssue[] issues = Icc2PolicyValidationCore.Validate(profile, policy);

        Assert.That(issues, Is.Empty);
    }

    [Test]
    public void PublicValidationRejectsConflictsAndClonesIcc2PolicyDeclarations()
    {
        IccProfile profile = CreateProfile(IccVersionKind.Version5);
        IccTag copyright = CreateTag(IccTagSignatures.Copyright);
        profile.Tags = new IccTag[] { copyright };
        IccValidationChannelRule channelRule = new IccValidationChannelRule
        {
            TagSignature = IccTagSignatures.AToB0,
            InputChannels = 3
        };
        IccValidationSubclassRule subclassRule = new IccValidationSubclassRule
        {
            ProfileSubclassSignature = 0x73756231u
        };
        IccValidationPolicy policy = new IccValidationPolicy
        {
            Icc2RequiredTagSignatures = new uint[] { IccTagSignatures.Copyright },
            Icc2OptionalTagSignatures = new uint[] { IccTagSignatures.Metadata },
            Icc2SupportedProcessingElementSignatures = new uint[]
            {
                IccProcessingElementSignatures.Matrix
            },
            Icc2ChannelRules = new IccValidationChannelRule[] { channelRule },
            Icc2ProfileSubclassRules = new IccValidationSubclassRule[] { subclassRule },
            SuppressedRuleIds = new string[] { IccValidationRuleIds.RequiredTag }
        };

        IccValidationReport report = new IccProfileValidator()
            .ValidateProfileDataReport(profile, policy);

        IccValidationPolicy conflicting = new IccValidationPolicy
        {
            Icc2RequiredTagSignatures = new uint[] { IccTagSignatures.Metadata },
            Icc2ForbiddenTagSignatures = new uint[] { IccTagSignatures.Metadata }
        };
        Assert.Multiple(() =>
        {
            Assert.That(report.Policy.Icc2RequiredTagSignatures,
                Is.EqualTo(policy.Icc2RequiredTagSignatures)
                    .And.Not.SameAs(policy.Icc2RequiredTagSignatures));
            Assert.That(report.Policy.Icc2SupportedProcessingElementSignatures,
                Is.Not.SameAs(policy.Icc2SupportedProcessingElementSignatures));
            Assert.That(report.Policy.Icc2ChannelRules[0], Is.Not.SameAs(channelRule));
            Assert.That(report.Policy.Icc2ProfileSubclassRules[0],
                Is.Not.SameAs(subclassRule));
            Assert.Throws<IccProfileValidationException>(() =>
                new IccProfileValidator().ValidateProfileDataReport(profile, conflicting));
        });
    }

    private static IccProfile CreateProfile(IccVersionKind version)
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = version;
        profile.Header.Version.Major = version == IccVersionKind.Version5 ? 5 : 4;
        profile.Header.Version.IsSupported = true;
        profile.Header.ProfileClass = IccProfileClass.Display;
        profile.Header.ProfileClassSignature = IccProfileClassSignatures.Display;
        profile.Header.DataColorSpace = IccColorSpaceKind.Rgb;
        profile.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Xyz;
        profile.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        return profile;
    }

    private static IccTag CreateTag(uint signature)
    {
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry.Signature = signature;
        return tag;
    }

    private static IccProcessingElement CreateElement(uint signature,
        int inputChannels, int outputChannels)
    {
        return new IccProcessingElement
        {
            Signature = signature,
            InputChannels = inputChannels,
            OutputChannels = outputChannels
        };
    }

    private static IccMultiProcessElements CreatePipeline(int inputChannels,
        int outputChannels, IccProcessingElement[] elements)
    {
        return new IccMultiProcessElements
        {
            InputChannels = inputChannels,
            OutputChannels = outputChannels,
            Elements = elements
        };
    }
}
