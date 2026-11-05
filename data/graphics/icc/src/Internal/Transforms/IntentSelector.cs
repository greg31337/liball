namespace icc;

internal class IntentSelection
{
    internal bool Success;
    internal IccTag? Tag;
    internal IccTag? MediaWhiteTag;
    internal uint TagSignature;
    internal IccRenderingIntent RequestedIntent;
    internal IccRenderingIntent SelectedIntent;
    internal bool UsedFallback;
    internal bool UsesMatrixTrc;
    internal bool IsGamut;
    internal bool ConvertAbsoluteBefore;
    internal bool ConvertAbsoluteAfter;
    internal string Message;

    internal IntentSelection()
    {
        Success = false;
        Tag = null;
        MediaWhiteTag = null;
        TagSignature = 0;
        RequestedIntent = IccRenderingIntent.Perceptual;
        SelectedIntent = IccRenderingIntent.Perceptual;
        UsedFallback = false;
        UsesMatrixTrc = false;
        IsGamut = false;
        ConvertAbsoluteBefore = false;
        ConvertAbsoluteAfter = false;
        Message = string.Empty;
    }
}

internal static class IntentSelector
{
    internal static IntentSelection SelectVersionFiveTransform(IccProfile profile, IccTransformDirection direction,
        IccRenderingIntent intent, IccIntentFallback fallback)
    {
        ValidateVersionFiveSelectionRequest(profile, direction, intent, fallback);
        IccProfileClass profileClass = profile.Header.ProfileClass;
        if (profileClass == IccProfileClass.NamedColor)
        {
            return CreateFailedSelection(intent, "Named-colour profiles do not provide a numeric ICC.2 transform pipeline.");
        }
        if (profileClass == IccProfileClass.DeviceLink || profileClass == IccProfileClass.MultiplexIdentification
            || profileClass == IccProfileClass.MultiplexLink || profileClass == IccProfileClass.MultiplexVisualization)
        {
            return CreateFailedSelection(intent, "The ICC.2 profile class requires a dedicated connection workflow.");
        }
        if (profileClass == IccProfileClass.Abstract && direction != IccTransformDirection.DeviceToPcs)
        {
            return CreateFailedSelection(intent, "An abstract ICC.2 profile can be applied only in its forward PCS-to-PCS direction.");
        }
        if (profileClass != IccProfileClass.Input && profileClass != IccProfileClass.Display
            && profileClass != IccProfileClass.Output && profileClass != IccProfileClass.ColorSpace
            && profileClass != IccProfileClass.Abstract && profileClass != IccProfileClass.ColorEncodingSpace)
        {
            return CreateFailedSelection(intent, "The ICC.2 profile class does not define a standard colorimetric transform pipeline.");
        }

        IntentSelection exact = SelectVersionFiveIntentCandidate(profile, direction, intent, intent, false);
        if (exact.Success || fallback == IccIntentFallback.None)
        {
            return exact;
        }
        if (fallback == IccIntentFallback.AllowColorimetric)
        {
            if (intent == IccRenderingIntent.Perceptual || intent == IccRenderingIntent.Saturation)
            {
                return SelectVersionFiveIntentCandidate(profile, direction, intent,
                    IccRenderingIntent.MediaRelativeColorimetric, true);
            }
            return CreateFailedSelection(intent, "No exact or compatible colorimetric ICC.2 transform is available.");
        }

        if (intent != IccRenderingIntent.Perceptual)
        {
            IntentSelection perceptual = SelectVersionFiveIntentCandidate(profile, direction, intent,
                IccRenderingIntent.Perceptual, true);
            if (perceptual.Success)
            {
                return perceptual;
            }
        }
        if (intent != IccRenderingIntent.MediaRelativeColorimetric
            && intent != IccRenderingIntent.IccAbsoluteColorimetric)
        {
            IntentSelection colorimetric = SelectVersionFiveIntentCandidate(profile, direction, intent,
                IccRenderingIntent.MediaRelativeColorimetric, true);
            if (colorimetric.Success)
            {
                return colorimetric;
            }
        }
        if (intent != IccRenderingIntent.Saturation)
        {
            IntentSelection saturation = SelectVersionFiveIntentCandidate(profile, direction, intent,
                IccRenderingIntent.Saturation, true);
            if (saturation.Success)
            {
                return saturation;
            }
        }
        return CreateFailedSelection(intent, "No ICC.2 colorimetric transform is available for the requested direction and rendering intent.");
    }

    internal static IntentSelection SelectVersionFiveIntentCandidate(IccProfile profile,
        IccTransformDirection direction, IccRenderingIntent requestedIntent,
        IccRenderingIntent candidateIntent, bool usedFallback)
    {
        uint signature = GetVersionFiveTransformSignature(direction, candidateIntent);
        IccTag? tag = FindTag(profile, signature);
        if (tag == null || !IsExecutableVersionFiveTag(tag))
        {
            return CreateFailedSelection(requestedIntent, "The requested ICC.2 transform candidate is unavailable.");
        }
        return CreateTagSelection(tag, requestedIntent, candidateIntent, usedFallback, direction, false, null);
    }

    internal static uint GetVersionFiveTransformSignature(IccTransformDirection direction, IccRenderingIntent intent)
    {
        uint baseSignature = direction == IccTransformDirection.DeviceToPcs
            ? IccTagSignatures.AToB0 : IccTagSignatures.BToA0;
        if (intent == IccRenderingIntent.Perceptual)
        {
            return baseSignature;
        }
        if (intent == IccRenderingIntent.MediaRelativeColorimetric)
        {
            return baseSignature + 1;
        }
        if (intent == IccRenderingIntent.Saturation)
        {
            return baseSignature + 2;
        }
        return baseSignature + 3;
    }

    internal static bool IsExecutableVersionFiveTag(IccTag tag)
    {
        if (tag.Data == null)
        {
            return false;
        }
        if (tag.Data.MultiProcessElements != null)
        {
            return tag.Data.MultiProcessElements.IsExecutable;
        }
        return tag.Data.MultiStageLut != null && tag.Data.Lut == null;
    }

    internal static IntentSelection SelectIcc1Transform(IccProfile profile, IccTransformDirection direction, IccRenderingIntent intent, IccIntentFallback fallback, bool selectGamut)
    {
        ValidateSelectionRequest(profile, direction, intent, fallback);
        if (selectGamut)
        {
            return SelectGamutTransform(profile, direction, intent);
        }

        IccProfileClass profileClass = profile.Header.ProfileClass;
        if (profileClass == IccProfileClass.DeviceLink)
        {
            return SelectDeviceLinkTransform(profile, direction, intent, fallback);
        }
        if (profileClass == IccProfileClass.Abstract)
        {
            return SelectAbstractTransform(profile, direction, intent);
        }
        if (profileClass == IccProfileClass.NamedColor)
        {
            return CreateFailedSelection(intent, "Named-colour profiles do not provide a numeric ICC.1 transform pipeline.");
        }
        if (profileClass != IccProfileClass.Input && profileClass != IccProfileClass.Display && profileClass != IccProfileClass.Output && profileClass != IccProfileClass.ColorSpace)
        {
            return CreateFailedSelection(intent, "The ICC.1 profile class does not define a selectable transform pipeline.");
        }
        if (direction == IccTransformDirection.DeviceToDevice)
        {
            return CreateFailedSelection(intent, "A non-DeviceLink ICC.1 profile cannot directly provide a device-to-device transform.");
        }

        IntentSelection exact = SelectRequestedIntent(profile, direction, intent);
        if (exact.Success)
        {
            return exact;
        }
        if (fallback == IccIntentFallback.None)
        {
            return exact;
        }

        if (fallback == IccIntentFallback.AllowColorimetric)
        {
            if (intent == IccRenderingIntent.Perceptual || intent == IccRenderingIntent.Saturation)
            {
                IntentSelection colorimetric = SelectIntentCandidate(profile, direction, intent, IccRenderingIntent.MediaRelativeColorimetric, true);
                if (colorimetric.Success)
                {
                    return colorimetric;
                }
            }

            return CreateFailedSelection(intent, "No exact or compatible colorimetric ICC.1 transform is available.");
        }

        if (fallback == IccIntentFallback.AllowAny)
        {
            if (intent != IccRenderingIntent.Perceptual)
            {
                IntentSelection perceptual = SelectIntentCandidate(profile, direction, intent, IccRenderingIntent.Perceptual, true);
                if (perceptual.Success)
                {
                    return perceptual;
                }
            }
            if (intent != IccRenderingIntent.MediaRelativeColorimetric && intent != IccRenderingIntent.IccAbsoluteColorimetric)
            {
                IntentSelection colorimetric = SelectIntentCandidate(profile, direction, intent, IccRenderingIntent.MediaRelativeColorimetric, true);
                if (colorimetric.Success)
                {
                    return colorimetric;
                }
            }
            if (intent != IccRenderingIntent.Saturation)
            {
                IntentSelection saturation = SelectIntentCandidate(profile, direction, intent, IccRenderingIntent.Saturation, true);
                if (saturation.Success)
                {
                    return saturation;
                }
            }
        }

        return CreateFailedSelection(intent, "No ICC.1 transform is available for the requested direction and rendering intent.");
    }

    internal static IntentSelection SelectRequestedIntent(IccProfile profile, IccTransformDirection direction, IccRenderingIntent intent)
    {
        if (intent != IccRenderingIntent.IccAbsoluteColorimetric)
        {
            return SelectIntentCandidate(profile, direction, intent, intent, false);
        }

        IccTag? absoluteTag = null;
        if (profile.Header.Version.Kind == IccVersionKind.Version4)
        {
            uint absoluteSignature = GetDTransformSignature(direction, IccRenderingIntent.IccAbsoluteColorimetric);
            if (absoluteSignature != 0)
            {
                absoluteTag = FindTag(profile, absoluteSignature);
            }
        }
        if (absoluteTag != null && IsExecutableDTag(absoluteTag))
        {
            return CreateTagSelection(absoluteTag, intent, IccRenderingIntent.IccAbsoluteColorimetric, false, direction, false, null);
        }

        IntentSelection relative = SelectIntentCandidate(profile, direction, intent, IccRenderingIntent.MediaRelativeColorimetric, false);
        if (!relative.Success)
        {
            return relative;
        }

        IccTag? mediaWhite = FindTag(profile, IccTagSignatures.MediaWhitePoint);
        if (mediaWhite == null)
        {
            return CreateFailedSelection(intent, "ICC-absolute synthesis requires a media-white-point tag.");
        }

        relative.MediaWhiteTag = mediaWhite;
        relative.ConvertAbsoluteBefore = direction == IccTransformDirection.PcsToDevice;
        relative.ConvertAbsoluteAfter = direction == IccTransformDirection.DeviceToPcs;
        relative.Message = "Selected a media-relative transform with ICC-absolute conversion synthesis.";
        return relative;
    }

    internal static IntentSelection SelectIntentCandidate(IccProfile profile, IccTransformDirection direction, IccRenderingIntent requestedIntent, IccRenderingIntent candidateIntent, bool usedFallback)
    {
        IccTag? tag = FindPreferredTransformTag(profile, direction, candidateIntent);
        if (tag != null)
        {
            return CreateTagSelection(tag, requestedIntent, candidateIntent, usedFallback, direction, false, null);
        }

        if (candidateIntent == IccRenderingIntent.MediaRelativeColorimetric && HasMatrixTrcTransform(profile))
        {
            IntentSelection matrix = new IntentSelection();
            matrix.Success = true;
            matrix.RequestedIntent = requestedIntent;
            matrix.SelectedIntent = candidateIntent;
            matrix.UsedFallback = usedFallback;
            matrix.UsesMatrixTrc = true;
            matrix.Message = usedFallback ? "Selected the matrix/TRC transform as a colorimetric fallback." : "Selected the matrix/TRC colorimetric transform.";
            return matrix;
        }

        return CreateFailedSelection(requestedIntent, "The requested transform candidate is unavailable.");
    }

    internal static IccTag? FindPreferredTransformTag(IccProfile profile, IccTransformDirection direction, IccRenderingIntent intent)
    {
        if (profile.Header.Version.Kind == IccVersionKind.Version4)
        {
            uint dSignature = GetDTransformSignature(direction, intent);
            if (dSignature != 0)
            {
                IccTag? dTag = FindTag(profile, dSignature);
                if (dTag != null && IsExecutableDTag(dTag))
                {
                    return dTag;
                }
            }
        }

        uint aSignature = GetATransformSignature(direction, intent);
        if (aSignature == 0)
        {
            return null;
        }

        return FindTag(profile, aSignature);
    }

    internal static uint GetDTransformSignature(IccTransformDirection direction, IccRenderingIntent intent)
    {
        if (direction == IccTransformDirection.DeviceToPcs)
        {
            switch (intent)
            {
                case IccRenderingIntent.Perceptual:
                    return IccTagSignatures.DToB0;
                case IccRenderingIntent.MediaRelativeColorimetric:
                    return IccTagSignatures.DToB1;
                case IccRenderingIntent.Saturation:
                    return IccTagSignatures.DToB2;
                case IccRenderingIntent.IccAbsoluteColorimetric:
                    return IccTagSignatures.DToB3;
                default:
                    return 0;
            }
        }
        if (direction == IccTransformDirection.PcsToDevice)
        {
            switch (intent)
            {
                case IccRenderingIntent.Perceptual:
                    return IccTagSignatures.BToD0;
                case IccRenderingIntent.MediaRelativeColorimetric:
                    return IccTagSignatures.BToD1;
                case IccRenderingIntent.Saturation:
                    return IccTagSignatures.BToD2;
                case IccRenderingIntent.IccAbsoluteColorimetric:
                    return IccTagSignatures.BToD3;
                default:
                    return 0;
            }
        }

        return 0;
    }

    internal static uint GetATransformSignature(IccTransformDirection direction, IccRenderingIntent intent)
    {
        int index;
        switch (intent)
        {
            case IccRenderingIntent.Perceptual:
                index = 0;
                break;
            case IccRenderingIntent.MediaRelativeColorimetric:
            case IccRenderingIntent.IccAbsoluteColorimetric:
                index = 1;
                break;
            case IccRenderingIntent.Saturation:
                index = 2;
                break;
            default:
                return 0;
        }

        if (direction == IccTransformDirection.DeviceToPcs)
        {
            if (index == 0)
            {
                return IccTagSignatures.AToB0;
            }
            if (index == 1)
            {
                return IccTagSignatures.AToB1;
            }

            return IccTagSignatures.AToB2;
        }
        if (direction == IccTransformDirection.PcsToDevice)
        {
            if (index == 0)
            {
                return IccTagSignatures.BToA0;
            }
            if (index == 1)
            {
                return IccTagSignatures.BToA1;
            }

            return IccTagSignatures.BToA2;
        }

        return 0;
    }

    internal static bool HasMatrixTrcTransform(IccProfile profile)
    {
        if (profile.Header.ProfileConnectionSpace != IccPcsKind.Xyz && profile.Header.ProfileConnectionSpaceSignature != IccColorSpaceSignatures.Xyz)
        {
            return false;
        }

        uint dataSpace = profile.Header.DataColorSpaceSignature;
        IccProfileClass profileClass = profile.Header.ProfileClass;
        if (dataSpace == IccColorSpaceSignatures.Gray)
        {
            if (profileClass != IccProfileClass.Input && profileClass != IccProfileClass.Display && profileClass != IccProfileClass.Output)
            {
                return false;
            }

            return FindTag(profile, IccTagSignatures.GrayTrc) != null;
        }
        if (dataSpace != IccColorSpaceSignatures.Rgb || (profileClass != IccProfileClass.Input
            && profileClass != IccProfileClass.Display && profileClass != IccProfileClass.ColorSpace))
        {
            return false;
        }

        return FindTag(profile, IccTagSignatures.RedTrc) != null
            && FindTag(profile, IccTagSignatures.GreenTrc) != null
            && FindTag(profile, IccTagSignatures.BlueTrc) != null
            && FindTag(profile, IccTagSignatures.RedMatrixColumn) != null
            && FindTag(profile, IccTagSignatures.GreenMatrixColumn) != null
            && FindTag(profile, IccTagSignatures.BlueMatrixColumn) != null;
    }

    internal static IntentSelection SelectGamutTransform(IccProfile profile, IccTransformDirection direction, IccRenderingIntent intent)
    {
        IccProfileClass profileClass = profile.Header.ProfileClass;
        if (direction != IccTransformDirection.PcsToDevice)
        {
            return CreateFailedSelection(intent, "An ICC.1 gamut transform operates from PCS values to one gamut channel.");
        }
        if (profileClass != IccProfileClass.Input && profileClass != IccProfileClass.Display && profileClass != IccProfileClass.Output && profileClass != IccProfileClass.ColorSpace)
        {
            return CreateFailedSelection(intent, "The ICC.1 profile class does not permit a gamut tag.");
        }

        IccTag? gamut = FindTag(profile, IccTagSignatures.Gamut);
        if (gamut == null)
        {
            return CreateFailedSelection(intent, "The profile does not contain an executable gamut tag.");
        }

        IntentSelection result = CreateTagSelection(gamut, intent, intent, false, direction, false, null);
        result.IsGamut = true;
        result.Message = "Selected the ICC.1 gamut tag.";
        return result;
    }

    internal static IntentSelection SelectDeviceLinkTransform(IccProfile profile, IccTransformDirection direction, IccRenderingIntent intent, IccIntentFallback fallback)
    {
        if (direction != IccTransformDirection.DeviceToDevice)
        {
            return CreateFailedSelection(intent, "A DeviceLink profile can be applied only in its device-to-device direction.");
        }

        IccRenderingIntent linkIntent = profile.Header.RenderingIntentKind;
        if (linkIntent != IccRenderingIntent.Perceptual && linkIntent != IccRenderingIntent.MediaRelativeColorimetric && linkIntent != IccRenderingIntent.Saturation && linkIntent != IccRenderingIntent.IccAbsoluteColorimetric)
        {
            return CreateFailedSelection(intent, "The DeviceLink header rendering intent is invalid.");
        }
        if (linkIntent != intent && fallback == IccIntentFallback.None)
        {
            return CreateFailedSelection(intent, "The DeviceLink profile was created for a different rendering intent.");
        }
        if (linkIntent != intent && fallback == IccIntentFallback.AllowColorimetric)
        {
            bool requestedColorimetric = intent == IccRenderingIntent.MediaRelativeColorimetric
                || intent == IccRenderingIntent.IccAbsoluteColorimetric;
            bool linkColorimetric = linkIntent == IccRenderingIntent.MediaRelativeColorimetric
                || linkIntent == IccRenderingIntent.IccAbsoluteColorimetric;
            if (!requestedColorimetric || !linkColorimetric)
            {
                return CreateFailedSelection(intent, "The DeviceLink header intent is not a compatible colorimetric fallback.");
            }
        }

        IccTag? tag = null;
        if (profile.Header.Version.Kind == IccVersionKind.Version4)
        {
            IccTag? dToB = FindTag(profile, IccTagSignatures.DToB0);
            if (dToB != null && dToB.Data != null && dToB.Data.MultiProcessElements != null
                && dToB.Data.MultiProcessElements.IsExecutable)
            {
                tag = dToB;
            }
        }
        if (tag == null)
        {
            tag = FindTag(profile, IccTagSignatures.AToB0);
        }
        if (tag == null)
        {
            return CreateFailedSelection(intent, "The DeviceLink profile does not contain an executable DToB0 or required AToB0 transform.");
        }

        return CreateTagSelection(tag, intent, linkIntent, linkIntent != intent, direction, false, null);
    }

    internal static IntentSelection SelectAbstractTransform(IccProfile profile, IccTransformDirection direction, IccRenderingIntent intent)
    {
        if (direction != IccTransformDirection.DeviceToPcs)
        {
            return CreateFailedSelection(intent, "An abstract ICC.1 profile can be applied only in its forward PCS-to-PCS direction.");
        }

        IccTag? tag = null;
        if (profile.Header.Version.Kind == IccVersionKind.Version4
            && intent == IccRenderingIntent.Perceptual)
        {
            IccTag? dToB = FindTag(profile, IccTagSignatures.DToB0);
            if (dToB != null && IsExecutableDTag(dToB))
            {
                tag = dToB;
            }
        }
        if (tag == null)
        {
            tag = FindTag(profile, IccTagSignatures.AToB0);
        }
        if (tag == null)
        {
            return CreateFailedSelection(intent, "The abstract profile does not contain its required AToB0 transform.");
        }

        return CreateTagSelection(tag, intent, intent, false, direction, false, null);
    }

    internal static IccTag? FindTag(IccProfile profile, uint signature)
    {
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null && tag.Signature == signature)
            {
                return tag;
            }

            index = index + 1;
        }

        return null;
    }

    internal static bool IsExecutableDTag(IccTag tag)
    {
        return tag.Data != null && tag.Data.MultiProcessElements != null
            && tag.Data.MultiProcessElements.IsExecutable;
    }

    internal static IntentSelection CreateTagSelection(IccTag tag, IccRenderingIntent requestedIntent, IccRenderingIntent selectedIntent, bool usedFallback, IccTransformDirection direction, bool synthesizeAbsolute, IccTag? mediaWhite)
    {
        IntentSelection result = new IntentSelection();
        result.Success = true;
        result.Tag = tag;
        result.MediaWhiteTag = mediaWhite;
        result.TagSignature = tag.Signature;
        result.RequestedIntent = requestedIntent;
        result.SelectedIntent = selectedIntent;
        result.UsedFallback = usedFallback;
        result.ConvertAbsoluteBefore = synthesizeAbsolute && direction == IccTransformDirection.PcsToDevice;
        result.ConvertAbsoluteAfter = synthesizeAbsolute && direction == IccTransformDirection.DeviceToPcs;
        result.Message = usedFallback ? "Selected an available ICC.1 rendering-intent fallback." : "Selected the requested ICC.1 transform.";
        return result;
    }

    internal static IntentSelection CreateFailedSelection(IccRenderingIntent requestedIntent, string message)
    {
        IntentSelection result = new IntentSelection();
        result.RequestedIntent = requestedIntent;
        result.SelectedIntent = requestedIntent;
        result.Message = message;
        return result;
    }

    internal static void ValidateSelectionRequest(IccProfile profile, IccTransformDirection direction, IccRenderingIntent intent, IccIntentFallback fallback)
    {
        if (profile == null || profile.Header == null || profile.Tags == null || profile.Header.Version == null)
        {
            throw new IccTransformException("Transform selection requires a profile with header, version, and tag data.");
        }
        if (profile.Header.Version.Kind != IccVersionKind.Version2 && profile.Header.Version.Kind != IccVersionKind.Version4)
        {
            throw new IccTransformException("ICC.1 transform selection requires a version 2 or version 4 profile.");
        }
        if (direction != IccTransformDirection.DeviceToPcs && direction != IccTransformDirection.PcsToDevice && direction != IccTransformDirection.DeviceToDevice)
        {
            throw new IccTransformException("The transform direction is not defined.");
        }
        if (intent != IccRenderingIntent.Perceptual && intent != IccRenderingIntent.MediaRelativeColorimetric && intent != IccRenderingIntent.Saturation && intent != IccRenderingIntent.IccAbsoluteColorimetric)
        {
            throw new IccTransformException("The rendering intent is not defined.");
        }
        if (fallback != IccIntentFallback.None && fallback != IccIntentFallback.AllowColorimetric && fallback != IccIntentFallback.AllowAny)
        {
            throw new IccTransformException("The rendering-intent fallback policy is not defined.");
        }
    }

    internal static void ValidateVersionFiveSelectionRequest(IccProfile profile, IccTransformDirection direction,
        IccRenderingIntent intent, IccIntentFallback fallback)
    {
        if (profile == null || profile.Header == null || profile.Tags == null || profile.Header.Version == null
            || profile.Header.Version.Kind != IccVersionKind.Version5)
        {
            throw new IccTransformException("ICC.2 transform selection requires a version 5 profile with header and tag data.");
        }
        if (direction != IccTransformDirection.DeviceToPcs && direction != IccTransformDirection.PcsToDevice)
        {
            throw new IccTransformException("A standard ICC.2 colorimetric transform requires a device-to-PCS or PCS-to-device direction.");
        }
        if (intent != IccRenderingIntent.Perceptual && intent != IccRenderingIntent.MediaRelativeColorimetric
            && intent != IccRenderingIntent.Saturation && intent != IccRenderingIntent.IccAbsoluteColorimetric)
        {
            throw new IccTransformException("The ICC.2 rendering intent is not defined.");
        }
        if (fallback != IccIntentFallback.None && fallback != IccIntentFallback.AllowColorimetric
            && fallback != IccIntentFallback.AllowAny)
        {
            throw new IccTransformException("The ICC.2 rendering-intent fallback policy is not defined.");
        }
    }
}
