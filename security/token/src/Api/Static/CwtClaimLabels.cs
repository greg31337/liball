using System;
namespace token;

/// <summary>
/// Provides integer labels from the CWT Claims registry.
/// </summary>
public static class CwtClaimLabels
{
    /// <summary>The issuer claim.</summary>
    public const int Issuer = 1;

    /// <summary>The subject claim.</summary>
    public const int Subject = 2;

    /// <summary>The audience claim.</summary>
    public const int Audience = 3;

    /// <summary>The expiration-time claim.</summary>
    public const int ExpirationTime = 4;

    /// <summary>The not-before claim.</summary>
    public const int NotBefore = 5;

    /// <summary>The issued-at claim.</summary>
    public const int IssuedAt = 6;

    /// <summary>The CWT identifier claim.</summary>
    public const int CwtId = 7;

    /// <summary>The confirmation claim.</summary>
    public const int Confirmation = 8;

    /// <summary>The scope claim.</summary>
    public const int Scope = 9;

    /// <summary>The nonce claim.</summary>
    public const int Nonce = 10;

    /// <summary>The verifiable credential type claim.</summary>
    public const int VerifiableCredentialType = 11;

    /// <summary>The ACE profile claim.</summary>
    public const int AceProfile = 38;

    /// <summary>The client nonce claim.</summary>
    public const int ClientNonce = 39;

    /// <summary>The expires-in claim.</summary>
    public const int ExpiresIn = 40;

    /// <summary>The identity data claim.</summary>
    public const int IdentityData = 169;

    /// <summary>The universal entity identifier claim.</summary>
    public const int UniversalEntityIdentifier = 256;

    /// <summary>The submodule entity identifiers claim.</summary>
    public const int SubmoduleEntityIdentifiers = 257;

    /// <summary>The hardware original-equipment-manufacturer identifier claim.</summary>
    public const int HardwareOemIdentifier = 258;

    /// <summary>The hardware model claim.</summary>
    public const int HardwareModel = 259;

    /// <summary>The hardware version claim.</summary>
    public const int HardwareVersion = 260;

    /// <summary>The uptime claim.</summary>
    public const int Uptime = 261;

    /// <summary>The OEM-authorized-boot claim.</summary>
    public const int OemAuthorizedBoot = 262;

    /// <summary>The debug status claim.</summary>
    public const int DebugStatus = 263;

    /// <summary>The location claim.</summary>
    public const int Location = 264;

    /// <summary>The EAT profile claim.</summary>
    public const int EatProfile = 265;

    /// <summary>The submodules section claim.</summary>
    public const int Submodules = 266;

    /// <summary>The boot count claim.</summary>
    public const int BootCount = 267;

    /// <summary>The boot seed claim.</summary>
    public const int BootSeed = 268;

    /// <summary>The degrees-of-license-assurance claim.</summary>
    public const int DegreesOfLicenseAssurance = 269;

    /// <summary>The software name claim.</summary>
    public const int SoftwareName = 270;

    /// <summary>The software version claim.</summary>
    public const int SoftwareVersion = 271;

    /// <summary>The software manifests claim.</summary>
    public const int SoftwareManifests = 272;

    /// <summary>The measurements claim.</summary>
    public const int Measurements = 273;

    /// <summary>The software measurement results claim.</summary>
    public const int SoftwareMeasurementResults = 274;

    /// <summary>The intended-use claim.</summary>
    public const int IntendedUse = 275;

    /// <summary>The geohash claim.</summary>
    public const int Geohash = 282;

    /// <summary>The composite-message wrapper claim.</summary>
    public const int CompositeMessageWrapper = 299;

    /// <summary>The watermark token version claim.</summary>
    public const int WatermarkVersion = 300;

    /// <summary>The watermark technology vendor claim.</summary>
    public const int WatermarkVendor = 301;

    /// <summary>The watermark pattern length claim.</summary>
    public const int WatermarkPatternLength = 302;

    /// <summary>The watermark segment duration claim.</summary>
    public const int WatermarkSegmentDuration = 303;

    /// <summary>The watermark pattern claim.</summary>
    public const int WatermarkPattern = 304;

    /// <summary>The watermark identifier claim.</summary>
    public const int WatermarkIdentifier = 305;

    /// <summary>The watermark operator identifier claim.</summary>
    public const int WatermarkOperatorIdentifier = 306;

    /// <summary>The watermark key version claim.</summary>
    public const int WatermarkKeyVersion = 307;

    /// <summary>The status-list claim.</summary>
    public const int StatusList = 65533;

    /// <summary>The time-to-live claim.</summary>
    public const int TimeToLive = 65534;

    /// <summary>The status claim.</summary>
    public const int Status = 65535;

    /// <summary>The GlobalPlatform component claim.</summary>
    public const int GlobalPlatformComponent = -261;

    /// <summary>The health-certificate claim.</summary>
    public const int HealthCertificate = -260;

    /// <summary>The EAT unprotected-header nonce claim.</summary>
    public const int EatUnprotectedHeaderNonce = -259;

    /// <summary>The EAT measured-artifact reference object prefix claim.</summary>
    public const int EatMeasuredArtifactPrefix = -258;

    /// <summary>The EAT FIDO Device Onboard claim.</summary>
    public const int EatFidoDeviceOnboard = -257;
}
