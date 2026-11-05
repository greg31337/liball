using System.Globalization;

namespace image;

internal class MakerNoteNames
{
    internal static string KeyNameFor(string vendor, int tagId)
    {
        string name = NameFor(vendor, tagId);
        if (name.Length > 0)
        {
            return name;
        }

        return "Tag" + tagId.ToString(CultureInfo.InvariantCulture);
    }

    internal static string DisplayNameFor(string vendor, int tagId)
    {
        string name = NameFor(vendor, tagId);
        if (name.Length > 0)
        {
            return name;
        }

        return "Tag " + tagId.ToString(CultureInfo.InvariantCulture);
    }

    internal static string NameFor(string vendor, int tagId)
    {
        if (vendor == "Apple")
        {
            return AppleNameFor(tagId);
        }

        if (vendor == "Canon")
        {
            return CanonNameFor(tagId);
        }

        if (vendor == "Nikon")
        {
            return NikonNameFor(tagId);
        }

        if (vendor == "Sony")
        {
            return SonyNameFor(tagId);
        }

        if (vendor == "Fujifilm")
        {
            return FujifilmNameFor(tagId);
        }

        if (vendor == "Olympus")
        {
            return OlympusNameFor(tagId);
        }

        if (vendor == "Panasonic" || vendor == "Leica")
        {
            return PanasonicNameFor(tagId);
        }

        if (vendor == "Pentax")
        {
            return PentaxNameFor(tagId);
        }

        if (vendor == "Samsung")
        {
            return SamsungNameFor(tagId);
        }

        if (vendor == "DJI")
        {
            return DjiNameFor(tagId);
        }

        if (vendor == "GoPro")
        {
            return GoProNameFor(tagId);
        }

        if (vendor == "Ricoh")
        {
            return RicohNameFor(tagId);
        }

        if (vendor == "Minolta")
        {
            return MinoltaNameFor(tagId);
        }

        if (vendor == "Kodak")
        {
            return KodakNameFor(tagId);
        }

        if (vendor == "Sigma" || vendor == "Foveon")
        {
            return SigmaNameFor(tagId);
        }

        if (vendor == "FLIR")
        {
            return FlirNameFor(tagId);
        }

        if (vendor == "Hasselblad")
        {
            return HasselbladNameFor(tagId);
        }

        return "";
    }

    internal static string AppleNameFor(int tagId)
    {
        switch (tagId)
        {
            case 3:
                return "RunTime";
            case 10:
                return "HDRImageType";
            case 11:
                return "BurstUUID";
            case 17:
                return "ContentIdentifier";
            default:
                return "";
        }
    }

    internal static string CanonNameFor(int tagId)
    {
        switch (tagId)
        {
            case 1:
                return "CameraSettings";
            case 2:
                return "FocalLength";
            case 4:
                return "ShotInfo";
            case 6:
                return "ImageType";
            case 7:
                return "FirmwareVersion";
            case 9:
                return "OwnerName";
            case 12:
                return "SerialNumber";
            case 16:
                return "ModelID";
            case 149:
                return "LensModel";
            default:
                return "";
        }
    }

    internal static string NikonNameFor(int tagId)
    {
        switch (tagId)
        {
            case 1:
                return "MakerNoteVersion";
            case 2:
                return "ISO";
            case 4:
                return "Quality";
            case 5:
                return "WhiteBalance";
            case 6:
                return "Sharpening";
            case 7:
                return "FocusMode";
            case 8:
                return "FlashSetting";
            case 9:
                return "FlashType";
            case 11:
                return "WhiteBalanceFineTune";
            case 17:
                return "PreviewIFD";
            case 132:
                return "LensType";
            case 133:
                return "Lens";
            case 134:
                return "ManualFocusDistance";
            case 135:
                return "DigitalZoom";
            case 136:
                return "FlashMode";
            case 137:
                return "AFInfo";
            case 138:
                return "ShootingMode";
            case 139:
                return "LensFStops";
            case 141:
                return "ColorHue";
            case 143:
                return "SceneMode";
            case 149:
                return "NoiseReduction";
            case 160:
                return "SerialNumber";
            case 165:
                return "ImageCount";
            case 167:
                return "ShutterCount";
            default:
                return "";
        }
    }

    internal static string SonyNameFor(int tagId)
    {
        switch (tagId)
        {
            case 258:
                return "Quality";
            case 260:
                return "FlashExposureComp";
            case 274:
                return "LensType";
            case 276:
                return "DigitalZoomRatio";
            case 277:
                return "WhiteBalance";
            case 4099:
                return "CreativeStyle";
            case 8192:
                return "ImageQuality";
            case 8201:
                return "SonyModelID";
            default:
                return "";
        }
    }

    internal static string FujifilmNameFor(int tagId)
    {
        switch (tagId)
        {
            case 0:
                return "Version";
            case 4096:
                return "Quality";
            case 4097:
                return "Sharpness";
            case 4098:
                return "WhiteBalance";
            case 4099:
                return "Color";
            case 4100:
                return "Tone";
            case 4112:
                return "FlashMode";
            case 4128:
                return "FocusMode";
            case 4144:
                return "SlowSync";
            case 4352:
                return "AutoBracketing";
            case 4864:
                return "BlurWarning";
            case 4865:
                return "FocusWarning";
            case 4866:
                return "ExposureWarning";
            default:
                return "";
        }
    }

    internal static string OlympusNameFor(int tagId)
    {
        switch (tagId)
        {
            case 0:
                return "MakerNoteVersion";
            case 512:
                return "SpecialMode";
            case 513:
                return "Quality";
            case 514:
                return "Macro";
            case 515:
                return "BWMode";
            case 516:
                return "DigitalZoom";
            case 517:
                return "FocalPlaneDiagonal";
            case 518:
                return "LensDistortionParams";
            case 519:
                return "CameraType";
            case 770:
                return "OneTouchWB";
            case 772:
                return "WhiteBalanceBracket";
            default:
                return "";
        }
    }

    internal static string PanasonicNameFor(int tagId)
    {
        switch (tagId)
        {
            case 1:
                return "ImageQuality";
            case 2:
                return "FirmwareVersion";
            case 3:
                return "WhiteBalance";
            case 7:
                return "FocusMode";
            case 15:
                return "AFMode";
            case 26:
                return "ImageStabilization";
            case 28:
                return "MacroMode";
            case 31:
                return "ShootingMode";
            case 40:
                return "Contrast";
            case 41:
                return "NoiseReduction";
            case 42:
                return "SelfTimer";
            case 46:
                return "Rotation";
            case 51:
                return "BabyAge";
            case 59:
                return "Transform";
            case 69:
                return "WorldTimeLocation";
            default:
                return "";
        }
    }

    internal static string PentaxNameFor(int tagId)
    {
        switch (tagId)
        {
            case 1:
                return "CaptureMode";
            case 2:
                return "Quality";
            case 3:
                return "FocusMode";
            case 4:
                return "FlashMode";
            case 7:
                return "WhiteBalance";
            case 10:
                return "DigitalZoom";
            case 11:
                return "Sharpness";
            case 12:
                return "Contrast";
            case 13:
                return "Saturation";
            case 20:
                return "ISO";
            case 23:
                return "Color";
            case 26:
                return "PrintIM";
            default:
                return "";
        }
    }

    internal static string SamsungNameFor(int tagId)
    {
        switch (tagId)
        {
            case 1:
                return "MakerNoteVersion";
            case 2:
                return "DeviceType";
            case 3:
                return "SamsungModelID";
            case 256:
                return "FaceDetect";
            default:
                return "";
        }
    }

    internal static string DjiNameFor(int tagId)
    {
        switch (tagId)
        {
            case 1:
                return "MakerNoteVersion";
            case 3:
                return "SpeedX";
            case 4:
                return "SpeedY";
            case 5:
                return "SpeedZ";
            case 6:
                return "Pitch";
            case 7:
                return "Yaw";
            case 8:
                return "Roll";
            case 9:
                return "CameraPitch";
            case 10:
                return "CameraYaw";
            case 11:
                return "CameraRoll";
            default:
                return "";
        }
    }

    internal static string GoProNameFor(int tagId)
    {
        switch (tagId)
        {
            case 1:
                return "FirmwareVersion";
            case 2:
                return "SerialNumber";
            case 3:
                return "CameraModel";
            case 4:
                return "LensModel";
            default:
                return "";
        }
    }

    internal static string RicohNameFor(int tagId)
    {
        switch (tagId)
        {
            case 1:
                return "MakerNoteType";
            case 2:
                return "FirmwareVersion";
            case 5:
                return "SerialNumber";
            default:
                return "";
        }
    }

    internal static string MinoltaNameFor(int tagId)
    {
        switch (tagId)
        {
            case 0:
                return "MakerNoteVersion";
            case 1:
                return "CameraSettingsOld";
            case 3:
                return "CameraSettings";
            case 64:
                return "CompressedImageSize";
            case 129:
                return "Thumbnail";
            case 136:
                return "ThumbnailOffset";
            default:
                return "";
        }
    }

    internal static string KodakNameFor(int tagId)
    {
        switch (tagId)
        {
            case 0:
                return "KodakMaker";
            case 9:
                return "Quality";
            case 10:
                return "BurstMode";
            case 12:
                return "ImageWidth";
            case 14:
                return "ImageHeight";
            case 16:
                return "YearCreated";
            case 18:
                return "MonthDayCreated";
            case 20:
                return "TimeCreated";
            default:
                return "";
        }
    }

    internal static string SigmaNameFor(int tagId)
    {
        switch (tagId)
        {
            case 2:
                return "SerialNumber";
            case 3:
                return "DriveMode";
            case 4:
                return "ResolutionMode";
            case 5:
                return "AutoFocusMode";
            case 6:
                return "FocusSetting";
            case 7:
                return "WhiteBalance";
            case 8:
                return "ExposureMode";
            case 9:
                return "MeteringMode";
            case 10:
                return "LensRange";
            default:
                return "";
        }
    }

    internal static string FlirNameFor(int tagId)
    {
        switch (tagId)
        {
            case 1:
                return "RawThermalImageWidth";
            case 2:
                return "RawThermalImageHeight";
            case 16:
                return "Emissivity";
            case 32:
                return "ObjectDistance";
            case 33:
                return "ReflectedApparentTemperature";
            default:
                return "";
        }
    }

    internal static string HasselbladNameFor(int tagId)
    {
        switch (tagId)
        {
            case 1:
                return "Version";
            case 2:
                return "SerialNumber";
            case 3:
                return "CameraModel";
            case 4:
                return "LensModel";
            default:
                return "";
        }
    }
}
