namespace avif;

/// <summary>
/// Collects reconciled HDR container properties and AV1 metadata OBU values.
/// </summary>
public class AvifHdrMetadata : IAvifHdrMetadata
{
    AvifContentLightLevel? contentLightLevel;
    AvifMasteringDisplay? masteringDisplay;
    AvifContentColorVolume? contentColorVolume;
    AvifAmbientViewingEnvironment? ambientViewingEnvironment;
    AvifReferenceViewingEnvironment? referenceViewingEnvironment;
    AvifCameraIntrinsicMatrix? cameraIntrinsicMatrix;
    AvifCameraExtrinsicMatrix? cameraExtrinsicMatrix;
    bool nominalDiffuseWhitePresent;
    uint nominalDiffuseWhiteLuminance;
    bool ituT35Present;
    int ituT35CountryCode;
    bool ituT35CountryCodeExtensionPresent;
    int ituT35CountryCodeExtension;
    byte[] ituT35Payload;
    bool hdr10PlusPresent;
    int terminalProviderCode;
    int terminalProviderOrientedCode;
    int applicationIdentifier;
    int applicationMode;
    byte[] hdr10PlusPayload;

    /// <summary>Creates an empty HDR metadata collection.</summary>
    public AvifHdrMetadata()
    {
        ituT35Payload = System.Array.Empty<byte>();
        hdr10PlusPayload = System.Array.Empty<byte>();
    }

    /// <summary>Sets static content-light-level metadata.</summary>
    /// <param name="maximumContentLightLevel">The maximum content light level.</param>
    /// <param name="maximumFrameAverageLightLevel">The maximum frame-average light level.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetContentLightLevel(
        int maximumContentLightLevel,
        int maximumFrameAverageLightLevel)
    {
        if (maximumContentLightLevel < 0
            || maximumContentLightLevel > ushort.MaxValue
            || maximumFrameAverageLightLevel < 0
            || maximumFrameAverageLightLevel > ushort.MaxValue)
        {
            return AvifResultCode.InvalidArgument;
        }
        contentLightLevel = new AvifContentLightLevel(
            maximumContentLightLevel,
            maximumFrameAverageLightLevel);
        return AvifResultCode.Success;
    }

    /// <summary>Sets mastering-display chromaticities and luminance bounds.</summary>
    /// <param name="primaryChromaticityX">Three X coordinates in units of 0.00002.</param>
    /// <param name="primaryChromaticityY">Three Y coordinates in units of 0.00002.</param>
    /// <param name="whitePointChromaticityX">The white-point X coordinate.</param>
    /// <param name="whitePointChromaticityY">The white-point Y coordinate.</param>
    /// <param name="maximumLuminance">Maximum luminance in units of 0.0001.</param>
    /// <param name="minimumLuminance">Minimum luminance in units of 0.0001.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetMasteringDisplay(
        int[] primaryChromaticityX,
        int[] primaryChromaticityY,
        int whitePointChromaticityX,
        int whitePointChromaticityY,
        uint maximumLuminance,
        uint minimumLuminance)
    {
        if (primaryChromaticityX == null
            || primaryChromaticityY == null
            || primaryChromaticityX.Length != 3
            || primaryChromaticityY.Length != 3
            || whitePointChromaticityX < 0
            || whitePointChromaticityX > 50000
            || whitePointChromaticityY < 0
            || whitePointChromaticityY > 50000
            || minimumLuminance >= maximumLuminance)
        {
            return AvifResultCode.InvalidArgument;
        }
        int index = 0;
        while (index < 3)
        {
            if (primaryChromaticityX[index] < 0
                || primaryChromaticityX[index] > 50000
                || primaryChromaticityY[index] < 0
                || primaryChromaticityY[index] > 50000)
            {
                return AvifResultCode.InvalidArgument;
            }
            index++;
        }
        masteringDisplay = new AvifMasteringDisplay(
            primaryChromaticityX,
            primaryChromaticityY,
            whitePointChromaticityX,
            whitePointChromaticityY,
            maximumLuminance,
            minimumLuminance);
        return AvifResultCode.Success;
    }

    /// <summary>Sets optional content-colour-volume primaries and luminance statistics.</summary>
    /// <param name="primaryChromaticityX">Three X coordinates, or null to omit primaries.</param>
    /// <param name="primaryChromaticityY">Three Y coordinates, or null to omit primaries.</param>
    /// <param name="minimumLuminance">Minimum luminance.</param>
    /// <param name="maximumLuminance">Maximum luminance.</param>
    /// <param name="averageLuminance">Average luminance.</param>
    /// <param name="luminanceFields">The optional luminance fields, combined with bitwise OR.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetContentColorVolume(
        int[]? primaryChromaticityX,
        int[]? primaryChromaticityY,
        uint minimumLuminance,
        uint maximumLuminance,
        uint averageLuminance,
        AvifContentColorVolumeFields luminanceFields)
    {
        int luminancePresence = (int)luminanceFields;
        bool primariesPresent = primaryChromaticityX != null
            || primaryChromaticityY != null;
        bool minimumPresent = (luminancePresence & 1) != 0;
        bool maximumPresent = (luminancePresence & 2) != 0;
        bool averagePresent = (luminancePresence & 4) != 0;
        if ((luminancePresence & ~7) != 0
            || !primariesPresent && luminancePresence == 0
            || primariesPresent && (primaryChromaticityX == null
                || primaryChromaticityY == null
                || primaryChromaticityX.Length != 3
                || primaryChromaticityY.Length != 3)
            || minimumPresent && averagePresent
                && minimumLuminance > averageLuminance
            || averagePresent && maximumPresent
                && averageLuminance > maximumLuminance
            || minimumPresent && maximumPresent
                && minimumLuminance > maximumLuminance)
        {
            return AvifResultCode.InvalidArgument;
        }
        int[] x = primariesPresent
            ? (primaryChromaticityX ?? System.Array.Empty<int>()) : new int[3];
        int[] y = primariesPresent
            ? (primaryChromaticityY ?? System.Array.Empty<int>()) : new int[3];
        int index = 0;
        while (index < 3)
        {
            if (x[index] < -5000000 || x[index] > 5000000
                || y[index] < -5000000 || y[index] > 5000000)
            {
                return AvifResultCode.InvalidArgument;
            }
            index++;
        }
        contentColorVolume = new AvifContentColorVolume(
            primariesPresent,
            minimumPresent,
            maximumPresent,
            averagePresent,
            x,
            y,
            minimumLuminance,
            maximumLuminance,
            averageLuminance);
        return AvifResultCode.Success;
    }

    /// <summary>Sets ambient-viewing-environment metadata.</summary>
    /// <param name="ambientIlluminance">Illuminance in units of 0.0001 lux.</param>
    /// <param name="ambientLightX">Light X chromaticity in units of 0.00002.</param>
    /// <param name="ambientLightY">Light Y chromaticity in units of 0.00002.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetAmbientViewingEnvironment(
        uint ambientIlluminance,
        int ambientLightX,
        int ambientLightY)
    {
        if (ambientIlluminance == 0
            || ambientLightX < 0 || ambientLightX > 50000
            || ambientLightY < 0 || ambientLightY > 50000)
        {
            return AvifResultCode.InvalidArgument;
        }
        ambientViewingEnvironment = new AvifAmbientViewingEnvironment(
            ambientIlluminance,
            ambientLightX,
            ambientLightY);
        return AvifResultCode.Success;
    }

    /// <summary>Sets reference-viewing-environment metadata.</summary>
    /// <param name="surroundLuminance">Surround luminance.</param>
    /// <param name="surroundLightX">Surround X chromaticity.</param>
    /// <param name="surroundLightY">Surround Y chromaticity.</param>
    /// <param name="peripheryLuminance">Periphery luminance.</param>
    /// <param name="peripheryLightX">Periphery X chromaticity.</param>
    /// <param name="peripheryLightY">Periphery Y chromaticity.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetReferenceViewingEnvironment(
        uint surroundLuminance,
        int surroundLightX,
        int surroundLightY,
        uint peripheryLuminance,
        int peripheryLightX,
        int peripheryLightY)
    {
        if (surroundLightX < 0 || surroundLightX > 10000
            || surroundLightY < 0 || surroundLightY > 10000
            || peripheryLightX < 0 || peripheryLightX > 10000
            || peripheryLightY < 0 || peripheryLightY > 10000)
        {
            return AvifResultCode.InvalidArgument;
        }
        referenceViewingEnvironment = new AvifReferenceViewingEnvironment(
            surroundLuminance,
            surroundLightX,
            surroundLightY,
            peripheryLuminance,
            peripheryLightX,
            peripheryLightY);
        return AvifResultCode.Success;
    }

    /// <summary>Sets nominal diffuse white luminance.</summary>
    /// <param name="luminance">The encoded luminance, or zero for the standard default.</param>
    /// <returns>Success.</returns>
    public AvifResultCode SetNominalDiffuseWhite(uint luminance)
    {
        nominalDiffuseWhitePresent = true;
        nominalDiffuseWhiteLuminance = luminance;
        return AvifResultCode.Success;
    }

    /// <summary>Sets a camera intrinsic matrix.</summary>
    /// <param name="matrix">The validated matrix to snapshot.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetCameraIntrinsicMatrix(
        AvifCameraIntrinsicMatrix matrix)
    {
        if (matrix == null || matrix.GetCode() != AvifResultCode.Success)
        {
            return AvifResultCode.InvalidArgument;
        }
        cameraIntrinsicMatrix = matrix.CreateCopy();
        return AvifResultCode.Success;
    }

    /// <summary>Sets a camera extrinsic matrix.</summary>
    /// <param name="matrix">The validated matrix to snapshot.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetCameraExtrinsicMatrix(
        AvifCameraExtrinsicMatrix matrix)
    {
        if (matrix == null || matrix.GetCode() != AvifResultCode.Success)
        {
            return AvifResultCode.InvalidArgument;
        }
        cameraExtrinsicMatrix = matrix.CreateCopy();
        return AvifResultCode.Success;
    }

    /// <summary>Sets an opaque ITU-T T.35 value.</summary>
    /// <param name="countryCode">The eight-bit country code.</param>
    /// <param name="countryCodeExtension">The extension for country code 255, otherwise zero.</param>
    /// <param name="payload">Bytes following the country-code fields.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetItuT35Payload(
        int countryCode,
        int countryCodeExtension,
        byte[]? payload)
    {
        if (payload == null || countryCode < 0 || countryCode > 255
            || countryCode == 255
                && (countryCodeExtension < 0 || countryCodeExtension > 255)
            || countryCode != 255 && countryCodeExtension != 0)
        {
            return AvifResultCode.InvalidArgument;
        }
        Av1ItuT35Metadata value = new Av1ItuT35Metadata();
        value.CountryCode = countryCode;
        value.HasCountryCodeExtension = countryCode == 255;
        value.CountryCodeExtension = countryCodeExtension;
        value.Payload = (byte[])payload.Clone();
        PopulateHdr10Plus(value);
        AssignItuT35(value);
        return AvifResultCode.Success;
    }

    /// <summary>Sets an HDR10+ application payload in its standardized T.35 envelope.</summary>
    /// <param name="payload">Bytes following the HDR10+ envelope.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetHdr10PlusPayload(byte[]? payload)
    {
        if (payload == null || payload.Length > int.MaxValue - 6)
        {
            return AvifResultCode.InvalidArgument;
        }
        byte[] envelope = new byte[payload.Length + 6];
        envelope[1] = 60;
        envelope[3] = 1;
        envelope[4] = 4;
        envelope[5] = 1;
        payload.CopyTo(envelope, 6);
        return SetItuT35Payload(181, 0, envelope);
    }

    internal static AvifHdrMetadata CreateCopy(AvifHdrMetadata source)
    {
        AvifHdrMetadata result = new AvifHdrMetadata();
        result.contentLightLevel = source.contentLightLevel == null
            ? null
            : source.contentLightLevel.CreateCopy();
        result.masteringDisplay = source.masteringDisplay == null
            ? null
            : source.masteringDisplay.CreateCopy();
        result.contentColorVolume = source.contentColorVolume == null
            ? null
            : source.contentColorVolume.CreateCopy();
        result.ambientViewingEnvironment = source.ambientViewingEnvironment == null
            ? null
            : source.ambientViewingEnvironment.CreateCopy();
        result.referenceViewingEnvironment = source.referenceViewingEnvironment == null
            ? null
            : source.referenceViewingEnvironment.CreateCopy();
        result.cameraIntrinsicMatrix = source.cameraIntrinsicMatrix == null
            ? null : source.cameraIntrinsicMatrix.CreateCopy();
        result.cameraExtrinsicMatrix = source.cameraExtrinsicMatrix == null
            ? null : source.cameraExtrinsicMatrix.CreateCopy();
        result.nominalDiffuseWhitePresent = source.nominalDiffuseWhitePresent;
        result.nominalDiffuseWhiteLuminance = source.nominalDiffuseWhiteLuminance;
        result.ituT35Present = source.ituT35Present;
        result.ituT35CountryCode = source.ituT35CountryCode;
        result.ituT35CountryCodeExtensionPresent = source.ituT35CountryCodeExtensionPresent;
        result.ituT35CountryCodeExtension = source.ituT35CountryCodeExtension;
        result.ituT35Payload = (byte[])source.ituT35Payload.Clone();
        result.hdr10PlusPresent = source.hdr10PlusPresent;
        result.terminalProviderCode = source.terminalProviderCode;
        result.terminalProviderOrientedCode = source.terminalProviderOrientedCode;
        result.applicationIdentifier = source.applicationIdentifier;
        result.applicationMode = source.applicationMode;
        result.hdr10PlusPayload = (byte[])source.hdr10PlusPayload.Clone();
        return result;
    }

    /// <summary>Parses and reconciles one complete AV1 metadata OBU payload.</summary>
    /// <param name="payload">The metadata OBU payload, including metadata type and trailing bits.</param>
    /// <returns>Success, a bitstream error, or nonconforming data when a retained value conflicts.</returns>
    public AvifResultCode AddAv1MetadataObuPayload(byte[]? payload)
    {
        if (payload == null)
        {
            return AvifResultCode.InvalidArgument;
        }

        HdrMetadataParseResult parsed = HdrMetadataParser.ParseAv1MetadataObuPayload(
            payload);
        if (parsed.Code != AvifResultCode.Success)
        {
            return parsed.Code;
        }

        return MetadataResolver.Merge(this, parsed.Metadata);
    }

    /// <summary>Reports whether any supported HDR or viewing metadata is present.</summary>
    /// <returns><c>true</c> when at least one supported value is present.</returns>
    public bool HasAnyMetadata()
    {
        return contentLightLevel != null
            || masteringDisplay != null
            || contentColorVolume != null
            || ambientViewingEnvironment != null
            || referenceViewingEnvironment != null
            || cameraIntrinsicMatrix != null
            || cameraExtrinsicMatrix != null
            || nominalDiffuseWhitePresent
            || ituT35Present;
    }

    /// <summary>Reports whether content-light-level metadata is present.</summary>
    /// <returns><c>true</c> when content-light-level metadata is present.</returns>
    public bool HasContentLightLevel()
    {
        return contentLightLevel != null;
    }

    /// <summary>Returns a copy of the content-light-level metadata.</summary>
    /// <returns>The metadata, or null when absent.</returns>
    public AvifContentLightLevel? GetContentLightLevel()
    {
        return contentLightLevel == null ? null : contentLightLevel.CreateCopy();
    }

    /// <summary>Reports whether mastering-display metadata is present.</summary>
    /// <returns><c>true</c> when mastering-display metadata is present.</returns>
    public bool HasMasteringDisplay()
    {
        return masteringDisplay != null;
    }

    /// <summary>Returns a copy of the mastering-display metadata.</summary>
    /// <returns>The metadata, or null when absent.</returns>
    public AvifMasteringDisplay? GetMasteringDisplay()
    {
        return masteringDisplay == null ? null : masteringDisplay.CreateCopy();
    }

    /// <summary>Reports whether content-colour-volume metadata is present.</summary>
    /// <returns><c>true</c> when content-colour-volume metadata is present.</returns>
    public bool HasContentColorVolume()
    {
        return contentColorVolume != null;
    }

    /// <summary>Returns a copy of the content-colour-volume metadata.</summary>
    /// <returns>The metadata, or null when absent.</returns>
    public AvifContentColorVolume? GetContentColorVolume()
    {
        return contentColorVolume == null ? null : contentColorVolume.CreateCopy();
    }

    /// <summary>Reports whether ambient-viewing-environment metadata is present.</summary>
    /// <returns><c>true</c> when ambient-viewing metadata is present.</returns>
    public bool HasAmbientViewingEnvironment()
    {
        return ambientViewingEnvironment != null;
    }

    /// <summary>Returns a copy of the ambient-viewing-environment metadata.</summary>
    /// <returns>The metadata, or null when absent.</returns>
    public AvifAmbientViewingEnvironment? GetAmbientViewingEnvironment()
    {
        return ambientViewingEnvironment == null
            ? null
            : ambientViewingEnvironment.CreateCopy();
    }

    /// <summary>Reports whether reference-viewing-environment metadata is present.</summary>
    /// <returns><c>true</c> when reference-viewing metadata is present.</returns>
    public bool HasReferenceViewingEnvironment()
    {
        return referenceViewingEnvironment != null;
    }

    /// <summary>Returns a copy of the reference-viewing-environment metadata.</summary>
    /// <returns>The metadata, or null when absent.</returns>
    public AvifReferenceViewingEnvironment? GetReferenceViewingEnvironment()
    {
        return referenceViewingEnvironment == null
            ? null
            : referenceViewingEnvironment.CreateCopy();
    }

    /// <summary>Reports whether a camera intrinsic matrix is present.</summary>
    /// <returns><c>true</c> when an intrinsic matrix is present.</returns>
    public bool HasCameraIntrinsicMatrix()
    {
        return cameraIntrinsicMatrix != null;
    }

    /// <summary>Returns a copy of the camera intrinsic matrix.</summary>
    /// <returns>The matrix, or null when absent.</returns>
    public AvifCameraIntrinsicMatrix? GetCameraIntrinsicMatrix()
    {
        return cameraIntrinsicMatrix == null
            ? null : cameraIntrinsicMatrix.CreateCopy();
    }

    /// <summary>Reports whether a camera extrinsic matrix is present.</summary>
    /// <returns><c>true</c> when an extrinsic matrix is present.</returns>
    public bool HasCameraExtrinsicMatrix()
    {
        return cameraExtrinsicMatrix != null;
    }

    /// <summary>Returns a copy of the camera extrinsic matrix.</summary>
    /// <returns>The matrix, or null when absent.</returns>
    public AvifCameraExtrinsicMatrix? GetCameraExtrinsicMatrix()
    {
        return cameraExtrinsicMatrix == null
            ? null : cameraExtrinsicMatrix.CreateCopy();
    }

    /// <summary>Reports whether nominal-diffuse-white metadata is present.</summary>
    /// <returns><c>true</c> when nominal diffuse white is present.</returns>
    public bool HasNominalDiffuseWhite()
    {
        return nominalDiffuseWhitePresent;
    }

    /// <summary>Returns nominal diffuse white luminance in units of 0.0001 candela per square metre.</summary>
    /// <returns>The encoded luminance, or zero when absent or when the default is requested.</returns>
    public uint GetNominalDiffuseWhiteLuminance()
    {
        return nominalDiffuseWhiteLuminance;
    }

    /// <summary>Reports whether an ITU-T T.35 metadata payload is present.</summary>
    /// <returns><c>true</c> when a T.35 payload is present.</returns>
    public bool HasItuT35Payload()
    {
        return ituT35Present;
    }

    /// <summary>Returns the ITU-T T.35 country code.</summary>
    /// <returns>The country code, or zero when absent.</returns>
    public int GetItuT35CountryCode()
    {
        return ituT35CountryCode;
    }

    /// <summary>Reports whether an extended ITU-T T.35 country code is present.</summary>
    /// <returns><c>true</c> when the extension is present.</returns>
    public bool HasItuT35CountryCodeExtension()
    {
        return ituT35CountryCodeExtensionPresent;
    }

    /// <summary>Returns the extended ITU-T T.35 country code.</summary>
    /// <returns>The extension, or zero when absent.</returns>
    public int GetItuT35CountryCodeExtension()
    {
        return ituT35CountryCodeExtension;
    }

    /// <summary>Returns a copy of the raw T.35 payload following its country code fields.</summary>
    /// <returns>The raw payload, or an empty array when absent.</returns>
    public byte[] GetItuT35Payload()
    {
        return (byte[])ituT35Payload.Clone();
    }

    /// <summary>Reports whether the T.35 payload has a recognized HDR10+ envelope.</summary>
    /// <returns><c>true</c> for recognized HDR10+ metadata.</returns>
    public bool HasHdr10PlusPayload()
    {
        return hdr10PlusPresent;
    }

    /// <summary>Returns the HDR10+ terminal provider code.</summary>
    /// <returns>The provider code, or zero when HDR10+ is absent.</returns>
    public int GetHdr10PlusTerminalProviderCode()
    {
        return terminalProviderCode;
    }

    /// <summary>Returns the HDR10+ terminal provider oriented code.</summary>
    /// <returns>The oriented code, or zero when HDR10+ is absent.</returns>
    public int GetHdr10PlusTerminalProviderOrientedCode()
    {
        return terminalProviderOrientedCode;
    }

    /// <summary>Returns the HDR10+ application identifier.</summary>
    /// <returns>The application identifier, or zero when HDR10+ is absent.</returns>
    public int GetHdr10PlusApplicationIdentifier()
    {
        return applicationIdentifier;
    }

    /// <summary>Returns the HDR10+ application mode.</summary>
    /// <returns>The application mode, or zero when HDR10+ is absent.</returns>
    public int GetHdr10PlusApplicationMode()
    {
        return applicationMode;
    }

    /// <summary>Returns a copy of the raw HDR10+ payload following its T.35 envelope.</summary>
    /// <returns>The HDR10+ payload, or an empty array when absent.</returns>
    public byte[] GetHdr10PlusPayload()
    {
        return (byte[])hdr10PlusPayload.Clone();
    }

    internal AvifContentLightLevel? GetContentLightLevelValue()
    {
        return contentLightLevel;
    }

    internal AvifMasteringDisplay? GetMasteringDisplayValue()
    {
        return masteringDisplay;
    }

    internal AvifContentColorVolume? GetContentColorVolumeValue()
    {
        return contentColorVolume;
    }

    internal AvifAmbientViewingEnvironment? GetAmbientViewingEnvironmentValue()
    {
        return ambientViewingEnvironment;
    }

    internal AvifReferenceViewingEnvironment? GetReferenceViewingEnvironmentValue()
    {
        return referenceViewingEnvironment;
    }

    internal AvifCameraIntrinsicMatrix? GetCameraIntrinsicMatrixValue()
    {
        return cameraIntrinsicMatrix;
    }

    internal AvifCameraExtrinsicMatrix? GetCameraExtrinsicMatrixValue()
    {
        return cameraExtrinsicMatrix;
    }

    internal bool HasNominalDiffuseWhiteValue()
    {
        return nominalDiffuseWhitePresent;
    }

    internal uint GetNominalDiffuseWhiteValue()
    {
        return nominalDiffuseWhiteLuminance;
    }

    internal bool HasItuT35Value()
    {
        return ituT35Present;
    }

    internal void AssignContentLightLevel(AvifContentLightLevel value)
    {
        contentLightLevel = value;
    }

    internal void AssignMasteringDisplay(AvifMasteringDisplay value)
    {
        masteringDisplay = value;
    }

    internal void AssignContentColorVolume(AvifContentColorVolume value)
    {
        contentColorVolume = value;
    }

    internal void AssignAmbientViewingEnvironment(AvifAmbientViewingEnvironment value)
    {
        ambientViewingEnvironment = value;
    }

    internal void AssignReferenceViewingEnvironment(AvifReferenceViewingEnvironment value)
    {
        referenceViewingEnvironment = value;
    }

    internal void AssignNominalDiffuseWhite(uint value)
    {
        nominalDiffuseWhitePresent = true;
        nominalDiffuseWhiteLuminance = value;
    }

    internal void AssignCameraIntrinsicMatrix(AvifCameraIntrinsicMatrix value)
    {
        cameraIntrinsicMatrix = value;
    }

    internal void AssignCameraExtrinsicMatrix(AvifCameraExtrinsicMatrix value)
    {
        cameraExtrinsicMatrix = value;
    }

    internal void AssignItuT35(Av1ItuT35Metadata value)
    {
        ituT35Present = true;
        ituT35CountryCode = value.CountryCode;
        ituT35CountryCodeExtensionPresent = value.HasCountryCodeExtension;
        ituT35CountryCodeExtension = value.CountryCodeExtension;
        ituT35Payload = (byte[])value.Payload.Clone();
        hdr10PlusPresent = value.HasHdr10PlusEnvelope;
        terminalProviderCode = value.TerminalProviderCode;
        terminalProviderOrientedCode = value.TerminalProviderOrientedCode;
        applicationIdentifier = value.ApplicationIdentifier;
        applicationMode = value.ApplicationMode;
        hdr10PlusPayload = (byte[])value.Hdr10PlusPayload.Clone();
    }

    static void PopulateHdr10Plus(Av1ItuT35Metadata value)
    {
        if (value.CountryCode != 181 || value.Payload.Length < 6)
        {
            return;
        }
        value.TerminalProviderCode = value.Payload[0] << 8 | value.Payload[1];
        value.TerminalProviderOrientedCode = value.Payload[2] << 8
            | value.Payload[3];
        value.ApplicationIdentifier = value.Payload[4];
        value.ApplicationMode = value.Payload[5];
        value.HasHdr10PlusEnvelope = value.TerminalProviderCode == 60
            && value.TerminalProviderOrientedCode == 1
            && value.ApplicationIdentifier == 4
            && value.ApplicationMode == 1;
        if (value.HasHdr10PlusEnvelope)
        {
            value.Hdr10PlusPayload = new byte[value.Payload.Length - 6];
            System.Array.Copy(
                value.Payload,
                6,
                value.Hdr10PlusPayload,
                0,
                value.Hdr10PlusPayload.Length);
        }
    }

    internal void CopyItuT35From(AvifHdrMetadata value)
    {
        ituT35Present = value.ituT35Present;
        ituT35CountryCode = value.ituT35CountryCode;
        ituT35CountryCodeExtensionPresent = value.ituT35CountryCodeExtensionPresent;
        ituT35CountryCodeExtension = value.ituT35CountryCodeExtension;
        ituT35Payload = (byte[])value.ituT35Payload.Clone();
        hdr10PlusPresent = value.hdr10PlusPresent;
        terminalProviderCode = value.terminalProviderCode;
        terminalProviderOrientedCode = value.terminalProviderOrientedCode;
        applicationIdentifier = value.applicationIdentifier;
        applicationMode = value.applicationMode;
        hdr10PlusPayload = (byte[])value.hdr10PlusPayload.Clone();
    }

    internal bool ItuT35ValuesEqual(AvifHdrMetadata other)
    {
        if (other == null
            || ituT35Present != other.ituT35Present
            || ituT35CountryCode != other.ituT35CountryCode
            || ituT35CountryCodeExtensionPresent
                != other.ituT35CountryCodeExtensionPresent
            || ituT35CountryCodeExtension != other.ituT35CountryCodeExtension
            || hdr10PlusPresent != other.hdr10PlusPresent
            || terminalProviderCode != other.terminalProviderCode
            || terminalProviderOrientedCode != other.terminalProviderOrientedCode
            || applicationIdentifier != other.applicationIdentifier
            || applicationMode != other.applicationMode
            || !BytesEqual(ituT35Payload, other.ituT35Payload)
            || !BytesEqual(hdr10PlusPayload, other.hdr10PlusPayload))
        {
            return false;
        }

        return true;
    }

    internal static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }
}
