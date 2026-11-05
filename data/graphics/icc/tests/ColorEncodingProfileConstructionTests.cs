using icc;

namespace icc.Tests;

public class ColorEncodingProfileConstructionTests
{
    [Test]
    public void BuildsMinimalVersionFiveAbbreviatedProfileFromRegisteredEncodingData()
    {
        IccProfileBuildOptions options = AbbreviatedOptions();
        options.Descriptions = new[]
        {
            new IccLocalizedString
            {
                Language = "en",
                Country = "US",
                Text = "Registered RGB encoding"
            }
        };
        options.ColorEncodingDefinition!.Parameters = Parameters();
        options.ColorEncodingDefinition.Parameters.Metadata = new[]
        {
            new IccMetadataEntry
            {
                Name = "org.example.source",
                Value = "registry",
                HasValue = true
            }
        };
        IIccProfileBuilder builder = new IccProfileBuilder();

        IccProfile profile = builder.CreateColorEncodingProfile(options);
        IccProfile repeated = builder.CreateColorEncodingProfile(options);
        byte[] rewritten = new IccProfileWriter().WriteProfileBytes(profile,
            new IccWriteOptions());
        options.ColorEncodingDefinition.Parameters.Matrix!.Values[0] = 9.0;
        options.ColorEncodingDefinition.Parameters.ChannelRanges[0].Maximum = 2.0;
        IccTag parameters = FindTag(profile,
            IccTagSignatures.ColorEncodingParameters);
        IccValidationReport validation = new IccProfileValidator()
            .ValidateProfileDataReport(profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.Version.Kind,
                Is.EqualTo(IccVersionKind.Version5));
            Assert.That(profile.Header.ProfileClass,
                Is.EqualTo(IccProfileClass.ColorEncodingSpace));
            Assert.That(profile.Header.ProfileConnectionSpaceSignature,
                Is.Zero);
            Assert.That(profile.Header.CreationDateTime.Year, Is.Zero);
            Assert.That(profile.Header.PcsIlluminantX, Is.Zero);
            Assert.That(profile.Header.ProfileId.IsPresent, Is.False);
            Assert.That(rewritten.Skip(84).Take(16),
                Is.All.EqualTo((byte)0));
            Assert.That(profile.Tags.Select(tag => tag.Signature),
                Is.EquivalentTo(new[]
                {
                    IccTagSignatures.ReferenceName,
                    IccTagSignatures.ColorSpaceName,
                    IccTagSignatures.ColorEncodingParameters,
                    IccTagSignatures.Metadata,
                    IccTagSignatures.ProfileDescription
                }));
            Assert.That(FindTag(profile, IccTagSignatures.ReferenceName)
                .Data.Text!.Text, Is.EqualTo("ISO 22028-1"));
            Assert.That(FindTag(profile, IccTagSignatures.ColorSpaceName)
                .Data.Text!.Text, Is.EqualTo("Example RGB"));
            Assert.That(parameters.Data.ColorEncodingParameters!.ChannelRanges
                .Single().Maximum, Is.EqualTo(1.0));
            Assert.That(parameters.Data.ColorEncodingParameters.Matrix!.Values[0],
                Is.EqualTo(1.0));
            Assert.That(parameters.Data.ColorEncodingParameters.Curves.Single().Kind,
                Is.EqualTo(IccCurveKind.Segmented));
            Assert.That(FindTag(profile, IccTagSignatures.Metadata)
                .Data.Metadata.Single().Value, Is.EqualTo("registry"));
            Assert.That(validation.IsValid, Is.True);
            Assert.That(profile.OriginalBytes, Is.EqualTo(repeated.OriginalBytes));
        });
    }

    [Test]
    public void BuildsFullColorSpaceProfileWithSpectralPccAndFallbackTransforms()
    {
        IccProfileBuildOptions options = FullOptions();
        IccColorEncodingProfileDefinition definition =
            options.ColorEncodingDefinition!;
        definition.SpectralPcsSignature =
            IccColorSpaceSignatures.ReflectanceSpectralPrefix | 3u;
        definition.SpectralPcsRange = new IccSpectralRange
        {
            StartWavelength = 400.0,
            EndWavelength = 600.0,
            Steps = 3
        };
        definition.ProfileConnectionConditions = Conditions();
        definition.ForwardFallbackTransform = IdentityPipeline();
        definition.BackwardFallbackTransform = IdentityPipeline();

        IccProfile profile = new IccProfileBuilder()
            .CreateColorEncodingProfile(options);
        definition.ProfileConnectionConditions.SpectralWhitePoint[0] = 7.0;
        definition.ForwardFallbackTransform.Elements[0].Matrix!.Values[0] = 7.0;
        IccValidationReport validation = new IccProfileValidator()
            .ValidateProfileDataReport(profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.ProfileClass,
                Is.EqualTo(IccProfileClass.ColorSpace));
            Assert.That(profile.Header.SpectralPcs,
                Is.EqualTo(IccColorSpaceKind.Spectral));
            Assert.That(profile.Header.SpectralPcsRangeSteps, Is.EqualTo(3));
            Assert.That(FindTag(profile, IccTagSignatures.SpectralWhitePoint)
                .Data.FloatingPointValues[0], Is.EqualTo(1.0));
            Assert.That(FindTag(profile, IccTagSignatures.SpectralViewingConditions)
                .Data.SpectralViewingConditions!.Observer,
                Is.EqualTo(IccMeasurementObserver.Cie1931TwoDegree));
            Assert.That(FindTag(profile, IccTagSignatures.MediaWhitePoint)
                .Data.XyzValues.Single().Y, Is.EqualTo(0.9).Within(0.00002));
            Assert.That(FindTag(profile, IccTagSignatures.CustomToStandardPcc)
                .Data.MultiProcessElements!.Elements[0].Matrix!.Values[0],
                Is.EqualTo(1.0));
            Assert.That(FindTag(profile, IccTagSignatures.StandardToCustomPcc)
                .Data.MultiProcessElements, Is.Not.Null);
            Assert.That(FindTag(profile, IccTagSignatures.AToB0)
                .Data.MultiProcessElements, Is.Not.Null);
            Assert.That(FindTag(profile, IccTagSignatures.BToA0)
                .Data.MultiProcessElements, Is.Not.Null);
            Assert.That(validation.IsValid, Is.True);
        });
    }

    [Test]
    public void BuildsReferenceOnlyProfileWithoutOptionalParametersOrFallbacks()
    {
        IccProfileBuildOptions options = AbbreviatedOptions();
        options.ColorEncodingDefinition =
            new IccColorEncodingProfileDefinition
            {
                ReferenceName = "urn:example:registered-encoding"
            };

        IccProfile profile = new IccProfileBuilder()
            .CreateColorEncodingProfile(options);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Tags, Has.Length.EqualTo(1));
            Assert.That(profile.Tags[0].Signature,
                Is.EqualTo(IccTagSignatures.ReferenceName));
            Assert.That(profile.Tags.Any(tag => tag.Signature
                == IccTagSignatures.ColorEncodingParameters), Is.False);
            Assert.That(profile.Tags.Any(tag => tag.Signature
                == IccTagSignatures.AToB0), Is.False);
        });
    }

    [Test]
    public void RejectsInvalidModesNamesRangesAndUnpairedTransforms()
    {
        IccProfileBuilder builder = new IccProfileBuilder();
        IccProfileBuildOptions wrongVersion = AbbreviatedOptions();
        wrongVersion.Version.Kind = IccVersionKind.Version4;
        wrongVersion.Version.Major = 4;
        IccProfileBuildOptions wrongClass = AbbreviatedOptions();
        wrongClass.ProfileClass = IccProfileClass.Display;
        IccProfileBuildOptions incompleteIso = AbbreviatedOptions();
        incompleteIso.ColorEncodingDefinition!.ColorSpaceName = string.Empty;
        incompleteIso.ColorEncodingDefinition.Parameters!.Name = string.Empty;
        IccProfileBuildOptions abbreviatedPcc = AbbreviatedOptions();
        abbreviatedPcc.ColorEncodingDefinition!.ProfileConnectionConditions =
            Conditions();
        IccProfileBuildOptions unpairedFallback = FullOptions();
        unpairedFallback.ColorEncodingDefinition!.ForwardFallbackTransform =
            IdentityPipeline();
        IccProfileBuildOptions badSpectralCount = FullOptions();
        badSpectralCount.ColorEncodingDefinition!.SpectralPcsSignature =
            IccColorSpaceSignatures.ReflectanceSpectralPrefix | 4u;
        badSpectralCount.ColorEncodingDefinition.SpectralPcsRange =
            new IccSpectralRange
            {
                StartWavelength = 400.0,
                EndWavelength = 600.0,
                Steps = 3
            };

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(() =>
                builder.CreateColorEncodingProfile(wrongVersion));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.CreateColorEncodingProfile(wrongClass));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.CreateColorEncodingProfile(incompleteIso));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.CreateColorEncodingProfile(abbreviatedPcc));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.CreateColorEncodingProfile(unpairedFallback));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.CreateColorEncodingProfile(badSpectralCount));
        });
    }

    private static IccProfileBuildOptions AbbreviatedOptions()
    {
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.Version.Kind = IccVersionKind.Version5;
        options.Version.Major = 5;
        options.Version.Minor = 0;
        options.ProfileClass = IccProfileClass.ColorEncodingSpace;
        options.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        options.ColorEncodingDefinition =
            new IccColorEncodingProfileDefinition
            {
                ReferenceName = "ISO 22028-1",
                ColorSpaceName = "Example RGB",
                Parameters = Parameters()
            };
        return options;
    }

    private static IccProfileBuildOptions FullOptions()
    {
        IccProfileBuildOptions options = AbbreviatedOptions();
        options.ProfileClass = IccProfileClass.ColorSpace;
        options.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        return options;
    }

    private static IccColorEncodingParameters Parameters()
    {
        return new IccColorEncodingParameters
        {
            Name = "Example RGB",
            ChannelRanges = new[]
            {
                new IccEncodingRange { Minimum = 0.0, Maximum = 1.0 }
            },
            ReferenceWhite = new IccXyzColor
            {
                X = 0.95047,
                Y = 1.0,
                Z = 1.08883
            },
            Curves = new[]
            {
                new IccCurve { Kind = IccCurveKind.Gamma, Gamma = 2.2 }
            },
            Matrix = new IccMatrix
            {
                Rows = 3,
                Columns = 3,
                Values = new[]
                {
                    1.0, 0.0, 0.0,
                    0.0, 1.0, 0.0,
                    0.0, 0.0, 1.0
                }
            }
        };
    }

    private static IccProfileConnectionConditions Conditions()
    {
        IccProfileConnectionConditions conditions =
            new IccProfileConnectionConditions
            {
                ViewingConditions = new IccSpectralViewingConditions
                {
                    Observer = IccMeasurementObserver.Cie1931TwoDegree,
                    Illuminant = IccStandardIlluminant.D50,
                    IlluminantColor = new IccXyzColor
                    {
                        X = IccStandardColorimetry.D50X,
                        Y = IccStandardColorimetry.D50Y,
                        Z = IccStandardColorimetry.D50Z
                    },
                    SurroundColor = new IccXyzColor
                    {
                        X = 0.2,
                        Y = 0.2,
                        Z = 0.2
                    }
                },
                MediaWhitePoint = new IccXyzColor
                {
                    X = 0.8,
                    Y = 0.9,
                    Z = 0.7
                },
                SpectralWhitePoint = new[] { 1.0, 1.0, 1.0 },
                ToStandardPcs = IdentityPipeline(),
                FromStandardPcs = IdentityPipeline()
            };
        return conditions;
    }

    private static IccMultiProcessElements IdentityPipeline()
    {
        IccProcessingElement matrix = new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.Matrix,
            Kind = IccProcessingElementKind.Matrix,
            InputChannels = 3,
            OutputChannels = 3,
            Matrix = new IccMatrix
            {
                Rows = 3,
                Columns = 4,
                Values = new[]
                {
                    1.0, 0.0, 0.0, 0.0,
                    0.0, 1.0, 0.0, 0.0,
                    0.0, 0.0, 1.0, 0.0
                }
            }
        };
        return new IccMultiProcessElements
        {
            InputChannels = 3,
            OutputChannels = 3,
            InputRanges = Ranges(3),
            OutputRanges = Ranges(3),
            Elements = new[] { matrix },
            IsExecutable = true
        };
    }

    private static IccEncodingRange[] Ranges(int count)
    {
        return Enumerable.Range(0, count)
            .Select(index => new IccEncodingRange())
            .ToArray();
    }

    private static IccTag FindTag(IccProfile profile, uint signature)
    {
        return profile.Tags.Single(tag => tag.Signature == signature);
    }
}
