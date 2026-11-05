using icc;

namespace icc.Tests;

public class InspectionBuilderTests
{
    [Test]
    public void StructuredInspectionAppliesEveryDetailLevelWithoutLeakingRawData()
    {
        IIccProfileInspector inspector = new IccProfileInspector();
        IccProfile profile = CreateProfile();
        IccProfileSummary summary = inspector.InspectProfile(profile,
            Options(IccInspectionDetail.Summary, false));
        IccProfileSummary normal = inspector.InspectProfile(profile,
            Options(IccInspectionDetail.Normal, false));
        IccProfileSummary detailed = inspector.InspectProfile(profile,
            Options(IccInspectionDetail.Detailed, false));
        IccProfileSummary raw = inspector.InspectProfile(profile,
            Options(IccInspectionDetail.Raw, false));

        Assert.Multiple(() =>
        {
            Assert.That(summary.Root.Children.Select(value => value.Name),
                Is.EqualTo(new string[] { "Header" }));
            Assert.That(FindPath(normal.Root, "Header").Children, Has.Length.EqualTo(10));
            Assert.That(FindPath(detailed.Root, "Header").Children,
                Has.Length.EqualTo(19));
            Assert.That(normal.Root.RawBytes, Is.Empty);
            Assert.That(detailed.Root.RawBytes, Is.Empty);
            Assert.That(raw.Root.RawBytes, Is.EqualTo(profile.OriginalBytes));
            Assert.That(raw.Root.RawBytes, Is.Not.SameAs(profile.OriginalBytes));
            Assert.That(summary.Options.Detail, Is.EqualTo(IccInspectionDetail.Summary));
            Assert.That(raw.Options.Detail, Is.EqualTo(IccInspectionDetail.Raw));
        });
    }

    [Test]
    public void PartialProfileRetainsNullPlaceholdersAcrossTreeSummaryAndText()
    {
        IIccProfileInspector inspector = new IccProfileInspector();
        IccProfile profile = CreateProfile();
        profile.Header.Version = null!;
        profile.TagTableEntries = null!;
        IccTag partial = new IccTag();
        partial.Signature = 0x70617274u;
        partial.TableEntry = null!;
        partial.Data = null!;
        partial.DecodeResult = null!;
        profile.Tags = new IccTag[] { null!, partial };
        IccInspectionOptions options = Options(IccInspectionDetail.Detailed, false);

        IccProfileSummary first = inspector.InspectProfile(profile, options);
        IccProfileSummary second = inspector.InspectProfile(profile, options);
        string text = inspector.InspectProfileText(profile,
            IccInspectionDetail.Detailed, 32);

        Assert.Multiple(() =>
        {
            Assert.That(first.Version, Is.EqualTo("Unknown ICC version"));
            Assert.That(first.Headline, Does.Contain("Unknown ICC version"));
            Assert.That(FindPath(first.Root, "TagTableEntries").Value,
                Is.EqualTo("0 entries"));
            Assert.That(FindPath(first.Root, "Tags[0]").Value, Is.EqualTo("null tag"));
            Assert.That(FindPath(first.Root, "Tags[1]").Value,
                Does.Contain("data unavailable"));
            Assert.That(FindPath(first.Root, "Tags[1].Data").Value, Is.EqualTo("null"));
            Assert.That(first.Validation.IsValid, Is.False);
            Assert.That(text, Does.Contain("Tag table: 0 entries"));
            Assert.That(text, Does.Contain("\n- null tag"));
            Assert.That(text, Does.Contain("Unknown/private tag signature ('part'"));
            Assert.That(text, Does.Contain("data unavailable"));
            Assert.That(text, Does.Contain("The profile version is unavailable."));
            Assert.That(Serialize(first.Root), Is.EqualTo(Serialize(second.Root)));
        });
    }

    [Test]
    public void StructuredInspectionCoversProfileDataAndTransformPrecedenceDeterministically()
    {
        IIccProfileInspector inspector = new IccProfileInspector();
        IccProfile profile = CreateProfile();
        IccInspectionOptions options = new IccInspectionOptions
        {
            Detail = IccInspectionDetail.Raw,
            MaximumDepth = 16,
            MaximumChildren = 64,
            MaximumRawBytes = 4,
            IncludeRawBytes = true
        };

        IccProfileSummary first = inspector.InspectProfile(profile, options);
        IccProfileSummary second = inspector.InspectProfile(profile, options);

        IccInspectionNode header = FindPath(first.Root, "Header");
        IccInspectionNode entry = FindPath(first.Root, "TagTableEntries[0]");
        IccInspectionNode rawTag = FindPath(first.Root, "Tags[0]");
        IccInspectionNode samples = FindPath(first.Root, "Tags[0].Data.Curve.Samples");
        IccInspectionNode mpe = FindPath(first.Root, "Tags[6].Data.MultiProcessElements");
        IccInspectionNode clut = FindPath(first.Root,
            "Tags[6].Data.MultiProcessElements.Elements[0].Clut");
        IccInspectionNode nestedCurve = FindPath(first.Root,
            "Tags[7].Data.TagStructure.Members[0].Data.TagArray.Values[0].Curve");
        IccInspectionNode validation = FindPath(first.Root, "Validation.Issues");
        IccInspectionNode transforms = FindPath(first.Root, "Transforms");

        Assert.Multiple(() =>
        {
            Assert.That(first.Headline, Does.Contain("Display device profile"));
            Assert.That(first.TagCount, Is.EqualTo(8));
            Assert.That(first.Options, Is.Not.SameAs(options));
            Assert.That(first.Root.Children.Select(value => value.Name), Is.EqualTo(new string[]
            {
                "Header", "TagTable", "Tags", "ValidationIssues", "TransformPrecedence"
            }));
            Assert.That(header.Children.Any(value => value.Name == "ProfileClass"), Is.True);
            Assert.That(entry.Children.Single().Value, Does.Contain("identity 7"));
            Assert.That(rawTag.RawBytes, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(rawTag.RawBytes, Is.Not.SameAs(profile.Tags[0].Data.RawData.Bytes));
            Assert.That(rawTag.IsTruncated, Is.True);
            Assert.That(samples.Children, Has.Length.EqualTo(12));
            Assert.That(mpe.Value, Does.Contain("1 elements"));
            Assert.That(clut.Value, Does.Contain("3 to 3 channels"));
            Assert.That(nestedCurve.Value, Is.EqualTo(IccCurveKind.Gamma.ToString()));
            Assert.That(validation.Children, Is.Not.Empty);
            Assert.That(transforms.Children, Has.Length.EqualTo(8));
            Assert.That(transforms.Children.Any(value => value.Value.Contains("A2B1")), Is.True);
            Assert.That(Serialize(first.Root), Is.EqualTo(Serialize(second.Root)));
        });
    }

    [Test]
    public void ChildDepthAndRawLimitsTruncateWithoutMutatingSourceData()
    {
        IIccProfileInspector inspector = new IccProfileInspector();
        IccProfile profile = CreateProfile();
        IccInspectionOptions limitedOptions = new IccInspectionOptions
        {
            Detail = IccInspectionDetail.Raw,
            MaximumDepth = 0,
            MaximumChildren = 1,
            MaximumRawBytes = 2,
            IncludeRawBytes = true
        };
        IccInspectionOptions valueOptions = new IccInspectionOptions
        {
            Detail = IccInspectionDetail.Detailed,
            MaximumDepth = 16,
            MaximumChildren = 5,
            MaximumRawBytes = 0,
            IncludeRawBytes = false
        };

        IccProfileSummary limited = inspector.InspectProfile(profile, limitedOptions);
        IccProfileSummary values = inspector.InspectProfile(profile, valueOptions);
        IccInspectionNode samples = FindPath(values.Root, "Tags[0].Data.Curve.Samples");

        Assert.Multiple(() =>
        {
            Assert.That(limited.Root.Children, Is.Empty);
            Assert.That(limited.Root.IsTruncated, Is.True);
            Assert.That(limited.Root.RawBytes, Is.EqualTo(new byte[] { 20, 21 }));
            Assert.That(samples.Children, Has.Length.EqualTo(5));
            Assert.That(samples.IsTruncated, Is.True);
            Assert.That(profile.OriginalBytes, Is.EqualTo(Enumerable.Range(20, 12)
                .Select(value => (byte)value).ToArray()));
            Assert.That(profile.Tags[0].Data.RawData.Bytes,
                Is.EqualTo(Enumerable.Range(1, 10).Select(value => (byte)value).ToArray()));
        });
    }

    [Test]
    public void InspectionRejectsInvalidBounds()
    {
        IIccProfileInspector inspector = new IccProfileInspector();
        IccProfile profile = CreateProfile();
        IccInspectionOptions depth = new IccInspectionOptions { MaximumDepth = -1 };
        IccInspectionOptions children = new IccInspectionOptions { MaximumChildren = -1 };
        IccInspectionOptions raw = new IccInspectionOptions { MaximumRawBytes = -1 };

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(() => inspector.InspectProfile(profile, null!));
            Assert.Throws<IccProfileValidationException>(() => inspector.InspectProfile(profile, depth));
            Assert.Throws<IccProfileValidationException>(() => inspector.InspectProfile(profile, children));
            Assert.Throws<IccProfileValidationException>(() => inspector.InspectProfile(profile, raw));
        });
    }

    private static IccProfile CreateProfile()
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = IccVersionKind.Version4;
        profile.Header.Version.Major = 4;
        profile.Header.Version.Minor = 4;
        profile.Header.ProfileClass = IccProfileClass.Display;
        profile.Header.ProfileClassSignature = IccProfileClassSignatures.Display;
        profile.Header.DataColorSpace = IccColorSpaceKind.Rgb;
        profile.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Xyz;
        profile.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        profile.Header.ProfileFileSignature = IccConstants.ProfileFileSignature;
        profile.Header.RenderingIntentKind = IccRenderingIntent.MediaRelativeColorimetric;
        profile.Header.RenderingIntent = 1;
        profile.OriginalBytes = Enumerable.Range(20, 12).Select(value => (byte)value).ToArray();
        profile.Tags = new IccTag[]
        {
            CurveTag(IccTagSignatures.RedTrc, true),
            CurveTag(IccTagSignatures.GreenTrc, false),
            CurveTag(IccTagSignatures.BlueTrc, false),
            XyzTag(IccTagSignatures.RedMatrixColumn, 1.0, 0.0, 0.0),
            XyzTag(IccTagSignatures.GreenMatrixColumn, 0.0, 1.0, 0.0),
            XyzTag(IccTagSignatures.BlueMatrixColumn, 0.0, 0.0, 1.0),
            MpeTag(),
            StructureTag()
        };
        profile.TagTableEntries = new IccTagTableEntry[profile.Tags.Length];
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTagTableEntry entry = profile.Tags[index].TableEntry;
            entry.Offset = 200 + (index * 32);
            entry.Size = profile.Tags[index].Data.RawData.Bytes.Length;
            entry.OriginalIndex = index;
            entry.SharedDataId = index < 2 ? 7 : -1;
            profile.TagTableEntries[index] = entry;
            index = index + 1;
        }
        return profile;
    }

    private static IccInspectionOptions Options(IccInspectionDetail detail,
        bool includeRawBytes)
    {
        return new IccInspectionOptions
        {
            Detail = detail,
            MaximumDepth = 32,
            MaximumChildren = 64,
            MaximumRawBytes = 64,
            IncludeRawBytes = includeRawBytes
        };
    }

    private static IccTag CurveTag(uint signature, bool sampled)
    {
        IccTag tag = BaseTag(signature, IccTagDataKind.Curve, IccTypeSignatures.Curve);
        tag.Data.Curve = sampled
            ? new IccCurve
            {
                Kind = IccCurveKind.Sampled,
                Samples = Enumerable.Range(0, 12).Select(value => value / 11.0).ToArray()
            }
            : new IccCurve { Kind = IccCurveKind.Gamma, Gamma = 1.0 };
        return tag;
    }

    private static IccTag XyzTag(uint signature, double x, double y, double z)
    {
        IccTag tag = BaseTag(signature, IccTagDataKind.XyzArray, IccTypeSignatures.Xyz);
        tag.Data.XyzValues = new IccXyzNumber[] { new IccXyzNumber { X = x, Y = y, Z = z } };
        return tag;
    }

    private static IccTag MpeTag()
    {
        IccTag tag = BaseTag(IccTagSignatures.AToB1,
            IccTagDataKind.MultiProcessElements, IccTypeSignatures.MultiProcessElements);
        tag.Data.MultiProcessElements = new IccMultiProcessElements
        {
            InputChannels = 3,
            OutputChannels = 3,
            IsExecutable = true,
            Elements = new IccProcessingElement[]
            {
                new IccProcessingElement
                {
                    Signature = IccProcessingElementSignatures.Clut,
                    Kind = IccProcessingElementKind.Clut,
                    InputChannels = 3,
                    OutputChannels = 3,
                    Clut = new IccClut
                    {
                        InputChannels = 3,
                        OutputChannels = 3,
                        GridPoints = new int[] { 2, 2, 2 },
                        Values = Enumerable.Range(0, 24).Select(value => value / 23.0).ToArray()
                    }
                }
            }
        };
        return tag;
    }

    private static IccTag StructureTag()
    {
        IccTag tag = BaseTag(0x74657374, IccTagDataKind.TagStructure,
            IccTypeSignatures.TagStructure);
        IccTagData curve = new IccTagData();
        curve.Kind = IccTagDataKind.Curve;
        curve.Curve = new IccCurve { Kind = IccCurveKind.Gamma, Gamma = 2.2 };
        IccTagData array = new IccTagData();
        array.Kind = IccTagDataKind.TagArray;
        array.TagArray = new IccTagArray
        {
            ArraySignature = 0x74657374,
            Values = new IccTagData[] { curve }
        };
        tag.Data.TagStructure = new IccTagStructure
        {
            StructureSignature = 0x74657374,
            Members = new IccStructureMember[]
            {
                new IccStructureMember { Signature = 0x64617461, Data = array }
            }
        };
        return tag;
    }

    private static IccTag BaseTag(uint signature, IccTagDataKind kind, uint type)
    {
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry.Signature = signature;
        tag.Data.Kind = kind;
        tag.Data.RawData.HasTypeSignature = true;
        tag.Data.RawData.TypeSignature = type;
        tag.Data.RawData.Bytes = Enumerable.Range(1, 10).Select(value => (byte)value).ToArray();
        return tag;
    }

    private static IccInspectionNode FindPath(IccInspectionNode node, string path)
    {
        if (node.Path == path)
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            IccInspectionNode? found = FindPathOrNull(node.Children[index], path);
            if (found != null)
            {
                return found;
            }
            index = index + 1;
        }
        throw new AssertionException($"Inspection path '{path}' was not found.");
    }

    private static IccInspectionNode? FindPathOrNull(IccInspectionNode node, string path)
    {
        if (node.Path == path)
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            IccInspectionNode? found = FindPathOrNull(node.Children[index], path);
            if (found != null)
            {
                return found;
            }
            index = index + 1;
        }
        return null;
    }

    private static string Serialize(IccInspectionNode node)
    {
        return $"{node.Name}|{node.Value}|{node.Path}|{node.Location.Offset}|{node.Location.Length}|{node.IsTruncated}|{System.Convert.ToHexString(node.RawBytes)}["
            + string.Join(",", node.Children.Select(Serialize)) + "]";
    }
}
