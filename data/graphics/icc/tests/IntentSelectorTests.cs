using icc;

namespace icc.Tests;

public class IntentSelectorTests
{
    [TestCase(IccTransformDirection.DeviceToPcs, IccRenderingIntent.Perceptual, IccTagSignatures.AToB0)]
    [TestCase(IccTransformDirection.DeviceToPcs, IccRenderingIntent.MediaRelativeColorimetric, IccTagSignatures.AToB1)]
    [TestCase(IccTransformDirection.DeviceToPcs, IccRenderingIntent.Saturation, IccTagSignatures.AToB2)]
    [TestCase(IccTransformDirection.DeviceToPcs, IccRenderingIntent.IccAbsoluteColorimetric, IccTagSignatures.AToB3)]
    [TestCase(IccTransformDirection.PcsToDevice, IccRenderingIntent.Perceptual, IccTagSignatures.BToA0)]
    [TestCase(IccTransformDirection.PcsToDevice, IccRenderingIntent.MediaRelativeColorimetric, IccTagSignatures.BToA1)]
    [TestCase(IccTransformDirection.PcsToDevice, IccRenderingIntent.Saturation, IccTagSignatures.BToA2)]
    [TestCase(IccTransformDirection.PcsToDevice, IccRenderingIntent.IccAbsoluteColorimetric, IccTagSignatures.BToA3)]
    public void VersionFiveSelectsExactColorimetricIntentTags(IccTransformDirection direction,
        IccRenderingIntent intent, uint signature)
    {
        IccProfile profile = CreateProfile(IccVersionKind.Version5, IccProfileClass.Display,
            IccColorSpaceSignatures.Rgb, IccPcsKind.Xyz, CreateExecutableVersionFiveTag(signature));

        IntentSelection selection = IntentSelector.SelectVersionFiveTransform(profile, direction,
            intent, IccIntentFallback.None);

        Assert.Multiple(() =>
        {
            Assert.That(selection.Success, Is.True);
            Assert.That(selection.TagSignature, Is.EqualTo(signature));
            Assert.That(selection.SelectedIntent, Is.EqualTo(intent));
            Assert.That(selection.UsedFallback, Is.False);
        });
    }

    [Test]
    public void VersionFiveFallbackSkipsNonExecutableExactTag()
    {
        IccProfile profile = CreateProfile(IccVersionKind.Version5, IccProfileClass.Display,
            IccColorSpaceSignatures.Rgb, IccPcsKind.Xyz,
            CreateTag(IccTagSignatures.AToB2),
            CreateExecutableVersionFiveTag(IccTagSignatures.AToB1));

        IntentSelection exact = IntentSelector.SelectVersionFiveTransform(profile,
            IccTransformDirection.DeviceToPcs, IccRenderingIntent.Saturation,
            IccIntentFallback.None);
        IntentSelection fallback = IntentSelector.SelectVersionFiveTransform(profile,
            IccTransformDirection.DeviceToPcs, IccRenderingIntent.Saturation,
            IccIntentFallback.AllowColorimetric);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.False);
            Assert.That(fallback.TagSignature, Is.EqualTo(IccTagSignatures.AToB1));
            Assert.That(fallback.SelectedIntent, Is.EqualTo(IccRenderingIntent.MediaRelativeColorimetric));
            Assert.That(fallback.UsedFallback, Is.True);
        });
    }

    [TestCase(IccTransformDirection.DeviceToPcs, IccRenderingIntent.Saturation, IccTagSignatures.DToB2, IccTagSignatures.AToB2)]
    [TestCase(IccTransformDirection.PcsToDevice, IccRenderingIntent.Perceptual, IccTagSignatures.BToD0, IccTagSignatures.BToA0)]
    [TestCase(IccTransformDirection.DeviceToPcs, IccRenderingIntent.Perceptual, IccTagSignatures.DToB0, IccTagSignatures.AToB0)]
    [TestCase(IccTransformDirection.DeviceToPcs, IccRenderingIntent.MediaRelativeColorimetric, IccTagSignatures.DToB1, IccTagSignatures.AToB1)]
    [TestCase(IccTransformDirection.DeviceToPcs, IccRenderingIntent.IccAbsoluteColorimetric, IccTagSignatures.DToB3, IccTagSignatures.AToB1)]
    [TestCase(IccTransformDirection.PcsToDevice, IccRenderingIntent.MediaRelativeColorimetric, IccTagSignatures.BToD1, IccTagSignatures.BToA1)]
    [TestCase(IccTransformDirection.PcsToDevice, IccRenderingIntent.Saturation, IccTagSignatures.BToD2, IccTagSignatures.BToA2)]
    [TestCase(IccTransformDirection.PcsToDevice, IccRenderingIntent.IccAbsoluteColorimetric, IccTagSignatures.BToD3, IccTagSignatures.BToA1)]
    public void VersionFourPrefersDTransformsOverMatchingATransforms(IccTransformDirection direction, IccRenderingIntent intent, uint dSignature, uint aSignature)
    {
        IccProfile profile = CreateProfile(IccVersionKind.Version4, IccProfileClass.Output, IccColorSpaceSignatures.Cmyk, IccPcsKind.Lab, CreateTag(aSignature), CreateExecutableDTag(dSignature));

        IntentSelection selection = IntentSelector.SelectIcc1Transform(profile, direction, intent, IccIntentFallback.None, false);

        Assert.Multiple(() =>
        {
            Assert.That(selection.Success, Is.True);
            Assert.That(selection.TagSignature, Is.EqualTo(dSignature));
            Assert.That(selection.UsedFallback, Is.False);
            Assert.That(selection.SelectedIntent, Is.EqualTo(intent));
        });
    }

    [Test]
    public void VersionTwoIgnoresDTransformsAndSelectsTheMatchingATransform()
    {
        IccProfile profile = CreateProfile(IccVersionKind.Version2, IccProfileClass.Output, IccColorSpaceSignatures.Cmyk, IccPcsKind.Lab,
            CreateTag(IccTagSignatures.DToB0), CreateTag(IccTagSignatures.AToB0));

        IntentSelection selection = IntentSelector.SelectIcc1Transform(profile, IccTransformDirection.DeviceToPcs, IccRenderingIntent.Perceptual, IccIntentFallback.None, false);

        Assert.That(selection.TagSignature, Is.EqualTo(IccTagSignatures.AToB0));
    }

    [Test]
    public void AbsoluteIntentPrefersD3WithoutSynthesis()
    {
        IccProfile profile = CreateProfile(IccVersionKind.Version4, IccProfileClass.Output, IccColorSpaceSignatures.Cmyk, IccPcsKind.Lab,
            CreateExecutableDTag(IccTagSignatures.DToB3), CreateExecutableDTag(IccTagSignatures.DToB1), CreateTag(IccTagSignatures.AToB1));

        IntentSelection selection = IntentSelector.SelectIcc1Transform(profile, IccTransformDirection.DeviceToPcs, IccRenderingIntent.IccAbsoluteColorimetric, IccIntentFallback.None, false);

        Assert.Multiple(() =>
        {
            Assert.That(selection.Success, Is.True);
            Assert.That(selection.TagSignature, Is.EqualTo(IccTagSignatures.DToB3));
            Assert.That(selection.SelectedIntent, Is.EqualTo(IccRenderingIntent.IccAbsoluteColorimetric));
            Assert.That(selection.ConvertAbsoluteBefore, Is.False);
            Assert.That(selection.ConvertAbsoluteAfter, Is.False);
            Assert.That(selection.MediaWhiteTag, Is.Null);
        });
    }

    [Test]
    public void AbsoluteIntentSynthesizesAroundRelativeForwardAndReverseTransforms()
    {
        IccTag mediaWhite = CreateTag(IccTagSignatures.MediaWhitePoint);
        IccProfile forward = CreateProfile(IccVersionKind.Version4, IccProfileClass.Output, IccColorSpaceSignatures.Cmyk, IccPcsKind.Lab,
            CreateTag(IccTagSignatures.AToB1), CreateExecutableDTag(IccTagSignatures.DToB1), mediaWhite);
        IccProfile reverse = CreateProfile(IccVersionKind.Version4, IccProfileClass.Output, IccColorSpaceSignatures.Cmyk, IccPcsKind.Lab,
            CreateTag(IccTagSignatures.BToA1), mediaWhite);

        IntentSelection forwardSelection = IntentSelector.SelectIcc1Transform(forward, IccTransformDirection.DeviceToPcs, IccRenderingIntent.IccAbsoluteColorimetric, IccIntentFallback.None, false);
        IntentSelection reverseSelection = IntentSelector.SelectIcc1Transform(reverse, IccTransformDirection.PcsToDevice, IccRenderingIntent.IccAbsoluteColorimetric, IccIntentFallback.None, false);

        Assert.Multiple(() =>
        {
            Assert.That(forwardSelection.TagSignature, Is.EqualTo(IccTagSignatures.DToB1));
            Assert.That(forwardSelection.SelectedIntent, Is.EqualTo(IccRenderingIntent.MediaRelativeColorimetric));
            Assert.That(forwardSelection.UsedFallback, Is.False);
            Assert.That(forwardSelection.ConvertAbsoluteAfter, Is.True);
            Assert.That(forwardSelection.ConvertAbsoluteBefore, Is.False);
            Assert.That(forwardSelection.MediaWhiteTag, Is.SameAs(mediaWhite));
            Assert.That(reverseSelection.TagSignature, Is.EqualTo(IccTagSignatures.BToA1));
            Assert.That(reverseSelection.ConvertAbsoluteBefore, Is.True);
            Assert.That(reverseSelection.ConvertAbsoluteAfter, Is.False);
        });
    }

    [Test]
    public void AbsoluteSynthesisFailsWithoutMediaWhitePointData()
    {
        IccProfile profile = CreateProfile(IccVersionKind.Version4, IccProfileClass.Display, IccColorSpaceSignatures.Rgb, IccPcsKind.Xyz,
            CreateTag(IccTagSignatures.AToB1));

        IntentSelection selection = IntentSelector.SelectIcc1Transform(profile, IccTransformDirection.DeviceToPcs, IccRenderingIntent.IccAbsoluteColorimetric, IccIntentFallback.None, false);

        Assert.Multiple(() =>
        {
            Assert.That(selection.Success, Is.False);
            Assert.That(selection.Message, Does.Contain("media-white-point"));
        });
    }

    [Test]
    public void AbsoluteAndAbstractSelectionSkipNonExecutableDTransforms()
    {
        IccTag mediaWhite = CreateTag(IccTagSignatures.MediaWhitePoint);
        IccProfile absolute = CreateProfile(IccVersionKind.Version4, IccProfileClass.Output,
            IccColorSpaceSignatures.Cmyk, IccPcsKind.Lab, CreateTag(IccTagSignatures.DToB3),
            CreateTag(IccTagSignatures.AToB1), mediaWhite);
        IccProfile abstractProfile = CreateProfile(IccVersionKind.Version4, IccProfileClass.Abstract,
            IccColorSpaceSignatures.Lab, IccPcsKind.Lab, CreateTag(IccTagSignatures.DToB0),
            CreateTag(IccTagSignatures.AToB0));

        IntentSelection absoluteSelection = IntentSelector.SelectIcc1Transform(absolute,
            IccTransformDirection.DeviceToPcs, IccRenderingIntent.IccAbsoluteColorimetric,
            IccIntentFallback.None, false);
        IntentSelection abstractSelection = IntentSelector.SelectIcc1Transform(abstractProfile,
            IccTransformDirection.DeviceToPcs, IccRenderingIntent.Perceptual,
            IccIntentFallback.None, false);

        Assert.Multiple(() =>
        {
            Assert.That(absoluteSelection.TagSignature, Is.EqualTo(IccTagSignatures.AToB1));
            Assert.That(absoluteSelection.ConvertAbsoluteAfter, Is.True);
            Assert.That(abstractSelection.TagSignature, Is.EqualTo(IccTagSignatures.AToB0));
        });
    }

    [Test]
    public void FallbackPolicyDistinguishesExactColorimetricAndAnyIntentChoices()
    {
        IccProfile profile = CreateProfile(IccVersionKind.Version4, IccProfileClass.Output, IccColorSpaceSignatures.Cmyk, IccPcsKind.Lab,
            CreateTag(IccTagSignatures.AToB0), CreateTag(IccTagSignatures.AToB1));

        IntentSelection exact = IntentSelector.SelectIcc1Transform(profile, IccTransformDirection.DeviceToPcs, IccRenderingIntent.Saturation, IccIntentFallback.None, false);
        IntentSelection colorimetric = IntentSelector.SelectIcc1Transform(profile, IccTransformDirection.DeviceToPcs, IccRenderingIntent.Saturation, IccIntentFallback.AllowColorimetric, false);
        IntentSelection any = IntentSelector.SelectIcc1Transform(profile, IccTransformDirection.DeviceToPcs, IccRenderingIntent.Saturation, IccIntentFallback.AllowAny, false);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.False);
            Assert.That(colorimetric.TagSignature, Is.EqualTo(IccTagSignatures.AToB1));
            Assert.That(colorimetric.SelectedIntent, Is.EqualTo(IccRenderingIntent.MediaRelativeColorimetric));
            Assert.That(colorimetric.UsedFallback, Is.True);
            Assert.That(any.TagSignature, Is.EqualTo(IccTagSignatures.AToB0));
            Assert.That(any.SelectedIntent, Is.EqualTo(IccRenderingIntent.Perceptual));
            Assert.That(any.UsedFallback, Is.True);
        });
    }

    [Test]
    public void CompleteRgbAndGrayMatrixTrcSetsProvideColorimetricFallbacks()
    {
        IccTag mediaWhite = CreateTag(IccTagSignatures.MediaWhitePoint);
        IccProfile rgb = CreateProfile(IccVersionKind.Version4, IccProfileClass.Display, IccColorSpaceSignatures.Rgb, IccPcsKind.Xyz,
            CreateTag(IccTagSignatures.RedTrc), CreateTag(IccTagSignatures.GreenTrc), CreateTag(IccTagSignatures.BlueTrc),
            CreateTag(IccTagSignatures.RedMatrixColumn), CreateTag(IccTagSignatures.GreenMatrixColumn), CreateTag(IccTagSignatures.BlueMatrixColumn), mediaWhite);
        IccProfile grayOutput = CreateProfile(IccVersionKind.Version2, IccProfileClass.Output, IccColorSpaceSignatures.Gray, IccPcsKind.Xyz,
            CreateTag(IccTagSignatures.GrayTrc));

        IntentSelection relative = IntentSelector.SelectIcc1Transform(rgb, IccTransformDirection.DeviceToPcs, IccRenderingIntent.MediaRelativeColorimetric, IccIntentFallback.None, false);
        IntentSelection fallback = IntentSelector.SelectIcc1Transform(rgb, IccTransformDirection.PcsToDevice, IccRenderingIntent.Perceptual, IccIntentFallback.AllowColorimetric, false);
        IntentSelection absolute = IntentSelector.SelectIcc1Transform(rgb, IccTransformDirection.DeviceToPcs, IccRenderingIntent.IccAbsoluteColorimetric, IccIntentFallback.None, false);
        IntentSelection gray = IntentSelector.SelectIcc1Transform(grayOutput, IccTransformDirection.PcsToDevice, IccRenderingIntent.MediaRelativeColorimetric, IccIntentFallback.None, false);

        Assert.Multiple(() =>
        {
            Assert.That(relative.UsesMatrixTrc, Is.True);
            Assert.That(relative.UsedFallback, Is.False);
            Assert.That(fallback.UsesMatrixTrc, Is.True);
            Assert.That(fallback.UsedFallback, Is.True);
            Assert.That(absolute.UsesMatrixTrc, Is.True);
            Assert.That(absolute.ConvertAbsoluteAfter, Is.True);
            Assert.That(gray.UsesMatrixTrc, Is.True);
        });
    }

    [Test]
    public void IncompleteOrClassIneligibleMatrixTrcSetsAreNotSelected()
    {
        IccTag[] incompleteTags = new IccTag[]
        {
            CreateTag(IccTagSignatures.RedTrc), CreateTag(IccTagSignatures.GreenTrc), CreateTag(IccTagSignatures.BlueTrc),
            CreateTag(IccTagSignatures.RedMatrixColumn), CreateTag(IccTagSignatures.GreenMatrixColumn)
        };
        IccProfile incomplete = CreateProfile(IccVersionKind.Version4, IccProfileClass.Display, IccColorSpaceSignatures.Rgb, IccPcsKind.Xyz, incompleteTags);
        IccProfile output = CreateProfile(IccVersionKind.Version4, IccProfileClass.Output, IccColorSpaceSignatures.Rgb, IccPcsKind.Xyz,
            incompleteTags.Concat(new IccTag[] { CreateTag(IccTagSignatures.BlueMatrixColumn) }).ToArray());

        IntentSelection incompleteSelection = IntentSelector.SelectIcc1Transform(incomplete, IccTransformDirection.DeviceToPcs, IccRenderingIntent.MediaRelativeColorimetric, IccIntentFallback.None, false);
        IntentSelection outputSelection = IntentSelector.SelectIcc1Transform(output, IccTransformDirection.DeviceToPcs, IccRenderingIntent.MediaRelativeColorimetric, IccIntentFallback.None, false);

        Assert.Multiple(() =>
        {
            Assert.That(incompleteSelection.Success, Is.False);
            Assert.That(outputSelection.Success, Is.False);
        });
    }

    [Test]
    public void GamutSelectionUsesOnlyTheGamutTagAndItsPcsToGamutDirection()
    {
        IccProfile profile = CreateProfile(IccVersionKind.Version4, IccProfileClass.Output, IccColorSpaceSignatures.Cmyk, IccPcsKind.Lab,
            CreateTag(IccTagSignatures.BToA0), CreateTag(IccTagSignatures.Gamut));

        IntentSelection selected = IntentSelector.SelectIcc1Transform(profile, IccTransformDirection.PcsToDevice, IccRenderingIntent.Perceptual, IccIntentFallback.AllowAny, true);
        IntentSelection wrongDirection = IntentSelector.SelectIcc1Transform(profile, IccTransformDirection.DeviceToPcs, IccRenderingIntent.Perceptual, IccIntentFallback.AllowAny, true);

        Assert.Multiple(() =>
        {
            Assert.That(selected.Success, Is.True);
            Assert.That(selected.IsGamut, Is.True);
            Assert.That(selected.TagSignature, Is.EqualTo(IccTagSignatures.Gamut));
            Assert.That(wrongDirection.Success, Is.False);
        });
    }

    [Test]
    public void DeviceLinkAndAbstractProfilesEnforceTheirDefinedDirections()
    {
        IccProfile deviceLink = CreateProfile(IccVersionKind.Version4, IccProfileClass.DeviceLink, IccColorSpaceSignatures.Rgb, IccPcsKind.None,
            CreateTag(IccTagSignatures.AToB0));
        deviceLink.Header.RenderingIntentKind = IccRenderingIntent.Saturation;
        IccProfile abstractProfile = CreateProfile(IccVersionKind.Version4, IccProfileClass.Abstract, IccColorSpaceSignatures.Lab, IccPcsKind.Lab,
            CreateTag(IccTagSignatures.AToB0));

        IntentSelection linkExactRequired = IntentSelector.SelectIcc1Transform(deviceLink, IccTransformDirection.DeviceToDevice, IccRenderingIntent.Perceptual, IccIntentFallback.None, false);
        IntentSelection linkFallback = IntentSelector.SelectIcc1Transform(deviceLink, IccTransformDirection.DeviceToDevice, IccRenderingIntent.Perceptual, IccIntentFallback.AllowAny, false);
        IntentSelection linkWrongDirection = IntentSelector.SelectIcc1Transform(deviceLink, IccTransformDirection.DeviceToPcs, IccRenderingIntent.Saturation, IccIntentFallback.AllowAny, false);
        IntentSelection abstractForward = IntentSelector.SelectIcc1Transform(abstractProfile, IccTransformDirection.DeviceToPcs, IccRenderingIntent.IccAbsoluteColorimetric, IccIntentFallback.None, false);
        IntentSelection abstractReverse = IntentSelector.SelectIcc1Transform(abstractProfile, IccTransformDirection.PcsToDevice, IccRenderingIntent.Perceptual, IccIntentFallback.None, false);

        Assert.Multiple(() =>
        {
            Assert.That(linkExactRequired.Success, Is.False);
            Assert.That(linkFallback.TagSignature, Is.EqualTo(IccTagSignatures.AToB0));
            Assert.That(linkFallback.SelectedIntent, Is.EqualTo(IccRenderingIntent.Saturation));
            Assert.That(linkFallback.UsedFallback, Is.True);
            Assert.That(linkWrongDirection.Success, Is.False);
            Assert.That(abstractForward.TagSignature, Is.EqualTo(IccTagSignatures.AToB0));
            Assert.That(abstractForward.ConvertAbsoluteAfter, Is.False);
            Assert.That(abstractReverse.Success, Is.False);
        });
    }

    [Test]
    public void InvalidSelectionInputsAreRejected()
    {
        IccProfile versionFive = CreateProfile(IccVersionKind.Version5, IccProfileClass.Display, IccColorSpaceSignatures.Rgb, IccPcsKind.Xyz);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccTransformException>(() => IntentSelector.SelectIcc1Transform(null!, IccTransformDirection.DeviceToPcs, IccRenderingIntent.Perceptual, IccIntentFallback.None, false));
            Assert.Throws<IccTransformException>(() => IntentSelector.SelectIcc1Transform(versionFive, IccTransformDirection.DeviceToPcs, IccRenderingIntent.Perceptual, IccIntentFallback.None, false));
            Assert.Throws<IccTransformException>(() => IntentSelector.SelectIcc1Transform(CreateProfile(IccVersionKind.Version4, IccProfileClass.Display, IccColorSpaceSignatures.Rgb, IccPcsKind.Xyz), (IccTransformDirection)99, IccRenderingIntent.Perceptual, IccIntentFallback.None, false));
            Assert.Throws<IccTransformException>(() => IntentSelector.SelectIcc1Transform(CreateProfile(IccVersionKind.Version4, IccProfileClass.Display, IccColorSpaceSignatures.Rgb, IccPcsKind.Xyz), IccTransformDirection.DeviceToPcs, (IccRenderingIntent)99, IccIntentFallback.None, false));
        });
    }

    private static IccProfile CreateProfile(IccVersionKind version, IccProfileClass profileClass, uint dataColorSpace, IccPcsKind pcsKind, params IccTag[] tags)
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = version;
        profile.Header.ProfileClass = profileClass;
        profile.Header.DataColorSpaceSignature = dataColorSpace;
        profile.Header.ProfileConnectionSpace = pcsKind;
        profile.Header.ProfileConnectionSpaceSignature = pcsKind == IccPcsKind.Xyz ? IccColorSpaceSignatures.Xyz : pcsKind == IccPcsKind.Lab ? IccColorSpaceSignatures.Lab : 0;
        profile.Tags = tags;
        return profile;
    }

    private static IccTag CreateTag(uint signature)
    {
        return new IccTag { Signature = signature };
    }

    private static IccTag CreateExecutableDTag(uint signature)
    {
        return new IccTag
        {
            Signature = signature,
            Data = new IccTagData
            {
                MultiProcessElements = new IccMultiProcessElements { IsExecutable = true }
            }
        };
    }

    private static IccTag CreateExecutableVersionFiveTag(uint signature)
    {
        return new IccTag
        {
            Signature = signature,
            Data = new IccTagData
            {
                MultiProcessElements = new IccMultiProcessElements { IsExecutable = true }
            }
        };
    }
}
