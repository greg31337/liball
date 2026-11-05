using System;

namespace avif;

internal class ParsedItemProperty
{
    int canonicalIndex;
    string type;
    uint typeValue;
    bool uuid;
    byte[] userType;
    byte[] rawData;
    ParsedAv1Configuration? av1Configuration;
    ParsedImageSpatialExtents? spatialExtents;
    ParsedPixelInformation? pixelInformation;
    ParsedColourInformation? colourInformation;
    ParsedPixelAspectRatio? pixelAspectRatio;
    ParsedCleanAperture? cleanAperture;
    ParsedImageRotation? imageRotation;
    ParsedImageMirror? imageMirror;
    ParsedAuxiliaryType? auxiliaryType;
    ParsedOperatingPointSelector? operatingPointSelector;
    ParsedLayerSelector? layerSelector;
    ParsedAv1LayeredImageIndex? layeredImageIndex;
    ParsedContentLightLevel? contentLightLevel;
    ParsedMasteringDisplayColourVolume? masteringDisplayColourVolume;
    ParsedContentColourVolume? contentColourVolume;
    ParsedAmbientViewingEnvironment? ambientViewingEnvironment;
    ParsedReferenceViewingEnvironment? referenceViewingEnvironment;
    ParsedNominalDiffuseWhite? nominalDiffuseWhite;
    ParsedCameraIntrinsicMatrix? cameraIntrinsicMatrix;
    ParsedCameraExtrinsicMatrix? cameraExtrinsicMatrix;

    internal ParsedItemProperty(
        int canonicalIndex,
        string type,
        uint typeValue,
        bool uuid,
        byte[] userType,
        byte[] rawData)
    {
        this.canonicalIndex = canonicalIndex;
        this.type = type;
        this.typeValue = typeValue;
        this.uuid = uuid;
        this.userType = (byte[])userType.Clone();
        this.rawData = (byte[])rawData.Clone();
        av1Configuration = null;
        spatialExtents = null;
        pixelInformation = null;
        colourInformation = null;
        pixelAspectRatio = null;
        cleanAperture = null;
        imageRotation = null;
        imageMirror = null;
        auxiliaryType = null;
        operatingPointSelector = null;
        layerSelector = null;
        layeredImageIndex = null;
        contentLightLevel = null;
        masteringDisplayColourVolume = null;
        contentColourVolume = null;
        ambientViewingEnvironment = null;
        referenceViewingEnvironment = null;
        nominalDiffuseWhite = null;
        cameraIntrinsicMatrix = null;
        cameraExtrinsicMatrix = null;
    }

    internal int GetCanonicalIndex()
    {
        return canonicalIndex;
    }

    internal string GetBoxType()
    {
        return type;
    }

    internal uint GetTypeValue()
    {
        return typeValue;
    }

    internal bool IsUuid()
    {
        return uuid;
    }

    internal byte[] GetUserType()
    {
        return (byte[])userType.Clone();
    }

    internal byte[] GetRawData()
    {
        return (byte[])rawData.Clone();
    }

    internal bool HasAv1Configuration()
    {
        return av1Configuration != null;
    }

    internal ParsedAv1Configuration? GetAv1Configuration()
    {
        return av1Configuration;
    }

    internal void SetAv1Configuration(ParsedAv1Configuration? configuration)
    {
        av1Configuration = configuration;
    }

    internal bool HasSpatialExtents()
    {
        return spatialExtents != null;
    }

    internal ParsedImageSpatialExtents? GetSpatialExtents()
    {
        return spatialExtents;
    }

    internal void SetSpatialExtents(ParsedImageSpatialExtents? extents)
    {
        spatialExtents = extents;
    }

    internal bool HasPixelInformation()
    {
        return pixelInformation != null;
    }

    internal ParsedPixelInformation? GetPixelInformation()
    {
        return pixelInformation;
    }

    internal void SetPixelInformation(ParsedPixelInformation? information)
    {
        pixelInformation = information;
    }

    internal ParsedColourInformation? GetColourInformation()
    {
        return colourInformation;
    }

    internal ParsedPixelAspectRatio? GetPixelAspectRatio()
    {
        return pixelAspectRatio;
    }

    internal ParsedCleanAperture? GetCleanAperture()
    {
        return cleanAperture;
    }

    internal ParsedImageRotation? GetImageRotation()
    {
        return imageRotation;
    }

    internal ParsedImageMirror? GetImageMirror()
    {
        return imageMirror;
    }

    internal ParsedAuxiliaryType? GetAuxiliaryType()
    {
        return auxiliaryType;
    }

    internal ParsedOperatingPointSelector? GetOperatingPointSelector()
    {
        return operatingPointSelector;
    }

    internal ParsedLayerSelector? GetLayerSelector()
    {
        return layerSelector;
    }

    internal ParsedAv1LayeredImageIndex? GetLayeredImageIndex()
    {
        return layeredImageIndex;
    }

    internal void SetAdditionalProperties(AdditionalItemPropertyParseResult? parsed)
    {
        if (parsed == null)
        {
            return;
        }

        colourInformation = parsed.ColourInformation;
        pixelAspectRatio = parsed.PixelAspectRatio;
        cleanAperture = parsed.CleanAperture;
        imageRotation = parsed.ImageRotation;
        imageMirror = parsed.ImageMirror;
        auxiliaryType = parsed.AuxiliaryType;
        operatingPointSelector = parsed.OperatingPointSelector;
        layerSelector = parsed.LayerSelector;
        layeredImageIndex = parsed.LayeredImageIndex;
    }

    internal ParsedContentLightLevel? GetContentLightLevel()
    {
        return contentLightLevel;
    }

    internal ParsedMasteringDisplayColourVolume? GetMasteringDisplayColourVolume()
    {
        return masteringDisplayColourVolume;
    }

    internal ParsedContentColourVolume? GetContentColourVolume()
    {
        return contentColourVolume;
    }

    internal ParsedAmbientViewingEnvironment? GetAmbientViewingEnvironment()
    {
        return ambientViewingEnvironment;
    }

    internal ParsedReferenceViewingEnvironment? GetReferenceViewingEnvironment()
    {
        return referenceViewingEnvironment;
    }

    internal ParsedNominalDiffuseWhite? GetNominalDiffuseWhite()
    {
        return nominalDiffuseWhite;
    }

    internal ParsedCameraIntrinsicMatrix? GetCameraIntrinsicMatrix()
    {
        return cameraIntrinsicMatrix;
    }

    internal ParsedCameraExtrinsicMatrix? GetCameraExtrinsicMatrix()
    {
        return cameraExtrinsicMatrix;
    }

    internal void SetHdrCameraProperties(HdrCameraPropertyParseResult? parsed)
    {
        if (parsed == null)
        {
            return;
        }

        contentLightLevel = parsed.ContentLightLevel;
        masteringDisplayColourVolume = parsed.MasteringDisplayColourVolume;
        contentColourVolume = parsed.ContentColourVolume;
        ambientViewingEnvironment = parsed.AmbientViewingEnvironment;
        referenceViewingEnvironment = parsed.ReferenceViewingEnvironment;
        nominalDiffuseWhite = parsed.NominalDiffuseWhite;
        cameraIntrinsicMatrix = parsed.CameraIntrinsicMatrix;
        cameraExtrinsicMatrix = parsed.CameraExtrinsicMatrix;
    }

    internal bool RawDataEquals(ReadOnlySpan<byte> candidate)
    {
        if (candidate.Length != rawData.Length)
        {
            return false;
        }

        int index = 0;
        while (index < rawData.Length)
        {
            if (candidate[index] != rawData[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }
}

internal class ParsedPropertyAssociation
{
    int propertyIndex;
    bool essential;
    ParsedItemProperty property;

    internal ParsedPropertyAssociation(
        int propertyIndex,
        bool essential,
        ParsedItemProperty property)
    {
        this.propertyIndex = propertyIndex;
        this.essential = essential;
        this.property = property;
    }

    internal int GetPropertyIndex()
    {
        return propertyIndex;
    }

    internal bool IsEssential()
    {
        return essential;
    }

    internal ParsedItemProperty GetProperty()
    {
        return property;
    }
}
