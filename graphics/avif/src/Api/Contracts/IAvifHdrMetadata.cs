namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifHdrMetadata"/>.</summary>
public interface IAvifHdrMetadata
{
    /// <summary>Sets static content-light-level metadata.</summary>
    /// <param name="maximumContentLightLevel">The maximum content light level.</param>
    /// <param name="maximumFrameAverageLightLevel">The maximum frame-average light level.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetContentLightLevel(
        int maximumContentLightLevel,
        int maximumFrameAverageLightLevel);

    /// <summary>Sets mastering-display chromaticities and luminance bounds.</summary>
    /// <param name="primaryChromaticityX">Three X coordinates in units of 0.00002.</param>
    /// <param name="primaryChromaticityY">Three Y coordinates in units of 0.00002.</param>
    /// <param name="whitePointChromaticityX">The white-point X coordinate.</param>
    /// <param name="whitePointChromaticityY">The white-point Y coordinate.</param>
    /// <param name="maximumLuminance">Maximum luminance in units of 0.0001.</param>
    /// <param name="minimumLuminance">Minimum luminance in units of 0.0001.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetMasteringDisplay(
        int[] primaryChromaticityX,
        int[] primaryChromaticityY,
        int whitePointChromaticityX,
        int whitePointChromaticityY,
        uint maximumLuminance,
        uint minimumLuminance);

    /// <summary>Sets optional content-colour-volume primaries and luminance statistics.</summary>
    /// <param name="primaryChromaticityX">Three X coordinates, or null to omit primaries.</param>
    /// <param name="primaryChromaticityY">Three Y coordinates, or null to omit primaries.</param>
    /// <param name="minimumLuminance">Minimum luminance.</param>
    /// <param name="maximumLuminance">Maximum luminance.</param>
    /// <param name="averageLuminance">Average luminance.</param>
    /// <param name="luminanceFields">The optional luminance fields, combined with bitwise OR.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetContentColorVolume(
        int[]? primaryChromaticityX,
        int[]? primaryChromaticityY,
        uint minimumLuminance,
        uint maximumLuminance,
        uint averageLuminance,
        AvifContentColorVolumeFields luminanceFields);

    /// <summary>Sets ambient-viewing-environment metadata.</summary>
    /// <param name="ambientIlluminance">Illuminance in units of 0.0001 lux.</param>
    /// <param name="ambientLightX">Light X chromaticity in units of 0.00002.</param>
    /// <param name="ambientLightY">Light Y chromaticity in units of 0.00002.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetAmbientViewingEnvironment(
        uint ambientIlluminance,
        int ambientLightX,
        int ambientLightY);

    /// <summary>Sets reference-viewing-environment metadata.</summary>
    /// <param name="surroundLuminance">Surround luminance.</param>
    /// <param name="surroundLightX">Surround X chromaticity.</param>
    /// <param name="surroundLightY">Surround Y chromaticity.</param>
    /// <param name="peripheryLuminance">Periphery luminance.</param>
    /// <param name="peripheryLightX">Periphery X chromaticity.</param>
    /// <param name="peripheryLightY">Periphery Y chromaticity.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetReferenceViewingEnvironment(
        uint surroundLuminance,
        int surroundLightX,
        int surroundLightY,
        uint peripheryLuminance,
        int peripheryLightX,
        int peripheryLightY);

    /// <summary>Sets nominal diffuse white luminance.</summary>
    /// <param name="luminance">The encoded luminance, or zero for the standard default.</param>
    /// <returns>Success.</returns>
    AvifResultCode SetNominalDiffuseWhite(uint luminance);

    /// <summary>Sets a camera intrinsic matrix.</summary>
    /// <param name="matrix">The validated matrix to snapshot.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetCameraIntrinsicMatrix(AvifCameraIntrinsicMatrix matrix);

    /// <summary>Sets a camera extrinsic matrix.</summary>
    /// <param name="matrix">The validated matrix to snapshot.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetCameraExtrinsicMatrix(AvifCameraExtrinsicMatrix matrix);

    /// <summary>Sets an opaque ITU-T T.35 value.</summary>
    /// <param name="countryCode">The eight-bit country code.</param>
    /// <param name="countryCodeExtension">The extension for country code 255, otherwise zero.</param>
    /// <param name="payload">Bytes following the country-code fields.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetItuT35Payload(
        int countryCode,
        int countryCodeExtension,
        byte[]? payload);

    /// <summary>Sets an HDR10+ application payload in its standardized T.35 envelope.</summary>
    /// <param name="payload">Bytes following the HDR10+ envelope.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetHdr10PlusPayload(byte[]? payload);

    /// <summary>Parses and reconciles one complete AV1 metadata OBU payload.</summary>
    /// <param name="payload">The metadata OBU payload, including metadata type and trailing bits.</param>
    /// <returns>Success, a bitstream error, or nonconforming data when a retained value conflicts.</returns>
    AvifResultCode AddAv1MetadataObuPayload(byte[]? payload);

    /// <summary>Reports whether any supported HDR or viewing metadata is present.</summary>
    /// <returns><c>true</c> when at least one supported value is present.</returns>
    bool HasAnyMetadata();

    /// <summary>Reports whether content-light-level metadata is present.</summary>
    /// <returns><c>true</c> when content-light-level metadata is present.</returns>
    bool HasContentLightLevel();

    /// <summary>Returns a copy of the content-light-level metadata.</summary>
    /// <returns>The metadata, or null when absent.</returns>
    AvifContentLightLevel? GetContentLightLevel();

    /// <summary>Reports whether mastering-display metadata is present.</summary>
    /// <returns><c>true</c> when mastering-display metadata is present.</returns>
    bool HasMasteringDisplay();

    /// <summary>Returns a copy of the mastering-display metadata.</summary>
    /// <returns>The metadata, or null when absent.</returns>
    AvifMasteringDisplay? GetMasteringDisplay();

    /// <summary>Reports whether content-colour-volume metadata is present.</summary>
    /// <returns><c>true</c> when content-colour-volume metadata is present.</returns>
    bool HasContentColorVolume();

    /// <summary>Returns a copy of the content-colour-volume metadata.</summary>
    /// <returns>The metadata, or null when absent.</returns>
    AvifContentColorVolume? GetContentColorVolume();

    /// <summary>Reports whether ambient-viewing-environment metadata is present.</summary>
    /// <returns><c>true</c> when ambient-viewing metadata is present.</returns>
    bool HasAmbientViewingEnvironment();

    /// <summary>Returns a copy of the ambient-viewing-environment metadata.</summary>
    /// <returns>The metadata, or null when absent.</returns>
    AvifAmbientViewingEnvironment? GetAmbientViewingEnvironment();

    /// <summary>Reports whether reference-viewing-environment metadata is present.</summary>
    /// <returns><c>true</c> when reference-viewing metadata is present.</returns>
    bool HasReferenceViewingEnvironment();

    /// <summary>Returns a copy of the reference-viewing-environment metadata.</summary>
    /// <returns>The metadata, or null when absent.</returns>
    AvifReferenceViewingEnvironment? GetReferenceViewingEnvironment();

    /// <summary>Reports whether a camera intrinsic matrix is present.</summary>
    /// <returns><c>true</c> when an intrinsic matrix is present.</returns>
    bool HasCameraIntrinsicMatrix();

    /// <summary>Returns a copy of the camera intrinsic matrix.</summary>
    /// <returns>The matrix, or null when absent.</returns>
    AvifCameraIntrinsicMatrix? GetCameraIntrinsicMatrix();

    /// <summary>Reports whether a camera extrinsic matrix is present.</summary>
    /// <returns><c>true</c> when an extrinsic matrix is present.</returns>
    bool HasCameraExtrinsicMatrix();

    /// <summary>Returns a copy of the camera extrinsic matrix.</summary>
    /// <returns>The matrix, or null when absent.</returns>
    AvifCameraExtrinsicMatrix? GetCameraExtrinsicMatrix();

    /// <summary>Reports whether nominal-diffuse-white metadata is present.</summary>
    /// <returns><c>true</c> when nominal diffuse white is present.</returns>
    bool HasNominalDiffuseWhite();

    /// <summary>Returns nominal diffuse white luminance in units of 0.0001 candela per square metre.</summary>
    /// <returns>The encoded luminance, or zero when absent or when the default is requested.</returns>
    uint GetNominalDiffuseWhiteLuminance();

    /// <summary>Reports whether an ITU-T T.35 metadata payload is present.</summary>
    /// <returns><c>true</c> when a T.35 payload is present.</returns>
    bool HasItuT35Payload();

    /// <summary>Returns the ITU-T T.35 country code.</summary>
    /// <returns>The country code, or zero when absent.</returns>
    int GetItuT35CountryCode();

    /// <summary>Reports whether an extended ITU-T T.35 country code is present.</summary>
    /// <returns><c>true</c> when the extension is present.</returns>
    bool HasItuT35CountryCodeExtension();

    /// <summary>Returns the extended ITU-T T.35 country code.</summary>
    /// <returns>The extension, or zero when absent.</returns>
    int GetItuT35CountryCodeExtension();

    /// <summary>Returns a copy of the raw T.35 payload following its country code fields.</summary>
    /// <returns>The raw payload, or an empty array when absent.</returns>
    byte[] GetItuT35Payload();

    /// <summary>Reports whether the T.35 payload has a recognized HDR10+ envelope.</summary>
    /// <returns><c>true</c> for recognized HDR10+ metadata.</returns>
    bool HasHdr10PlusPayload();

    /// <summary>Returns the HDR10+ terminal provider code.</summary>
    /// <returns>The provider code, or zero when HDR10+ is absent.</returns>
    int GetHdr10PlusTerminalProviderCode();

    /// <summary>Returns the HDR10+ terminal provider oriented code.</summary>
    /// <returns>The oriented code, or zero when HDR10+ is absent.</returns>
    int GetHdr10PlusTerminalProviderOrientedCode();

    /// <summary>Returns the HDR10+ application identifier.</summary>
    /// <returns>The application identifier, or zero when HDR10+ is absent.</returns>
    int GetHdr10PlusApplicationIdentifier();

    /// <summary>Returns the HDR10+ application mode.</summary>
    /// <returns>The application mode, or zero when HDR10+ is absent.</returns>
    int GetHdr10PlusApplicationMode();

    /// <summary>Returns a copy of the raw HDR10+ payload following its T.35 envelope.</summary>
    /// <returns>The HDR10+ payload, or an empty array when absent.</returns>
    byte[] GetHdr10PlusPayload();
}
