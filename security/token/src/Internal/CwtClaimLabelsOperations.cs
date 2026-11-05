using System;
namespace token;

/// <summary>
/// Provides integer labels and names from the CWT Claims registry.
/// </summary>
internal static class CwtClaimLabelsOperations
{
    /// <summary>The issuer claim.</summary>
    internal const int Issuer = CwtClaimLabels.Issuer;
    /// <summary>The subject claim.</summary>
    internal const int Subject = CwtClaimLabels.Subject;
    /// <summary>The audience claim.</summary>
    internal const int Audience = CwtClaimLabels.Audience;
    /// <summary>The expiration-time claim.</summary>
    internal const int ExpirationTime = CwtClaimLabels.ExpirationTime;
    /// <summary>The not-before claim.</summary>
    internal const int NotBefore = CwtClaimLabels.NotBefore;
    /// <summary>The issued-at claim.</summary>
    internal const int IssuedAt = CwtClaimLabels.IssuedAt;
    /// <summary>The CWT identifier claim.</summary>
    internal const int CwtId = CwtClaimLabels.CwtId;
    /// <summary>The confirmation claim.</summary>
    internal const int Confirmation = CwtClaimLabels.Confirmation;
    /// <summary>The scope claim.</summary>
    internal const int Scope = CwtClaimLabels.Scope;
    /// <summary>The nonce claim.</summary>
    internal const int Nonce = CwtClaimLabels.Nonce;
    /// <summary>The verifiable credential type claim.</summary>
    internal const int VerifiableCredentialType = CwtClaimLabels.VerifiableCredentialType;
    /// <summary>The ACE profile claim.</summary>
    internal const int AceProfile = CwtClaimLabels.AceProfile;
    /// <summary>The client nonce claim.</summary>
    internal const int ClientNonce = CwtClaimLabels.ClientNonce;
    /// <summary>The expires-in claim.</summary>
    internal const int ExpiresIn = CwtClaimLabels.ExpiresIn;
    /// <summary>The identity data claim.</summary>
    internal const int IdentityData = CwtClaimLabels.IdentityData;
    /// <summary>The universal entity identifier claim.</summary>
    internal const int UniversalEntityIdentifier = CwtClaimLabels.UniversalEntityIdentifier;
    /// <summary>The submodule entity identifiers claim.</summary>
    internal const int SubmoduleEntityIdentifiers = CwtClaimLabels.SubmoduleEntityIdentifiers;
    /// <summary>The hardware original-equipment-manufacturer identifier claim.</summary>
    internal const int HardwareOemIdentifier = CwtClaimLabels.HardwareOemIdentifier;
    /// <summary>The hardware model claim.</summary>
    internal const int HardwareModel = CwtClaimLabels.HardwareModel;
    /// <summary>The hardware version claim.</summary>
    internal const int HardwareVersion = CwtClaimLabels.HardwareVersion;
    /// <summary>The uptime claim.</summary>
    internal const int Uptime = CwtClaimLabels.Uptime;
    /// <summary>The OEM-authorized-boot claim.</summary>
    internal const int OemAuthorizedBoot = CwtClaimLabels.OemAuthorizedBoot;
    /// <summary>The debug status claim.</summary>
    internal const int DebugStatus = CwtClaimLabels.DebugStatus;
    /// <summary>The location claim.</summary>
    internal const int Location = CwtClaimLabels.Location;
    /// <summary>The EAT profile claim.</summary>
    internal const int EatProfile = CwtClaimLabels.EatProfile;
    /// <summary>The submodules section claim.</summary>
    internal const int Submodules = CwtClaimLabels.Submodules;
    /// <summary>The boot count claim.</summary>
    internal const int BootCount = CwtClaimLabels.BootCount;
    /// <summary>The boot seed claim.</summary>
    internal const int BootSeed = CwtClaimLabels.BootSeed;
    /// <summary>The degrees-of-license-assurance claim.</summary>
    internal const int DegreesOfLicenseAssurance = CwtClaimLabels.DegreesOfLicenseAssurance;
    /// <summary>The software name claim.</summary>
    internal const int SoftwareName = CwtClaimLabels.SoftwareName;
    /// <summary>The software version claim.</summary>
    internal const int SoftwareVersion = CwtClaimLabels.SoftwareVersion;
    /// <summary>The software manifests claim.</summary>
    internal const int SoftwareManifests = CwtClaimLabels.SoftwareManifests;
    /// <summary>The measurements claim.</summary>
    internal const int Measurements = CwtClaimLabels.Measurements;
    /// <summary>The software measurement results claim.</summary>
    internal const int SoftwareMeasurementResults = CwtClaimLabels.SoftwareMeasurementResults;
    /// <summary>The intended-use claim.</summary>
    internal const int IntendedUse = CwtClaimLabels.IntendedUse;
    /// <summary>The geohash claim.</summary>
    internal const int Geohash = CwtClaimLabels.Geohash;
    /// <summary>The composite-message wrapper claim.</summary>
    internal const int CompositeMessageWrapper = CwtClaimLabels.CompositeMessageWrapper;
    /// <summary>The watermark token version claim.</summary>
    internal const int WatermarkVersion = CwtClaimLabels.WatermarkVersion;
    /// <summary>The watermark technology vendor claim.</summary>
    internal const int WatermarkVendor = CwtClaimLabels.WatermarkVendor;
    /// <summary>The watermark pattern length claim.</summary>
    internal const int WatermarkPatternLength = CwtClaimLabels.WatermarkPatternLength;
    /// <summary>The watermark segment duration claim.</summary>
    internal const int WatermarkSegmentDuration = CwtClaimLabels.WatermarkSegmentDuration;
    /// <summary>The watermark pattern claim.</summary>
    internal const int WatermarkPattern = CwtClaimLabels.WatermarkPattern;
    /// <summary>The watermark identifier claim.</summary>
    internal const int WatermarkIdentifier = CwtClaimLabels.WatermarkIdentifier;
    /// <summary>The watermark operator identifier claim.</summary>
    internal const int WatermarkOperatorIdentifier = CwtClaimLabels.WatermarkOperatorIdentifier;
    /// <summary>The watermark key version claim.</summary>
    internal const int WatermarkKeyVersion = CwtClaimLabels.WatermarkKeyVersion;
    /// <summary>The status-list claim.</summary>
    internal const int StatusList = CwtClaimLabels.StatusList;
    /// <summary>The time-to-live claim.</summary>
    internal const int TimeToLive = CwtClaimLabels.TimeToLive;
    /// <summary>The status claim.</summary>
    internal const int Status = CwtClaimLabels.Status;
    /// <summary>The GlobalPlatform component claim.</summary>
    internal const int GlobalPlatformComponent = CwtClaimLabels.GlobalPlatformComponent;
    /// <summary>The health-certificate claim.</summary>
    internal const int HealthCertificate = CwtClaimLabels.HealthCertificate;
    /// <summary>The EAT unprotected-header nonce claim.</summary>
    internal const int EatUnprotectedHeaderNonce = CwtClaimLabels.EatUnprotectedHeaderNonce;
    /// <summary>The EAT measured-artifact reference object prefix claim.</summary>
    internal const int EatMeasuredArtifactPrefix = CwtClaimLabels.EatMeasuredArtifactPrefix;
    /// <summary>The EAT FIDO Device Onboard claim.</summary>
    internal const int EatFidoDeviceOnboard = CwtClaimLabels.EatFidoDeviceOnboard;

    static readonly int[] labels = new int[]
    {
        -261, -260, -259, -258, -257, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
        38, 39, 40, 169, 256, 257, 258, 259, 260, 261, 262, 263, 264, 265,
        266, 267, 268, 269, 270, 271, 272, 273, 274, 275, 282, 299, 300, 301,
        302, 303, 304, 305, 306, 307, 308, 309, 310, 311, 312, 313, 314,
        315, 316, 317, 318, 319, 320, 321, 322, 323, 2394, 2395, 2396, 2398,
        2399, 2400, 65533, 65534, 65535
    };

    static readonly string[] names = new string[]
    {
        "globalplatform_component", "hcert", "EUPHNonce", "EATMAROEPrefix",
        "EAT-FDO", "iss", "sub", "aud", "exp", "nbf", "iat", "cti", "cnf",
        "scope", "Nonce", "vct", "ace_profile", "cnonce", "exi",
        "identity-data", "UEID", "SUEIDs", "Hardware OEM ID", "Hardware Model",
        "Hardware Version", "Uptime", "OEM Authorized Boot", "Debug Status",
        "Location", "EAT Profile", "Submodules Section", "Boot Count", "Boot Seed",
        "DLOAs", "Software Name", "Software Version", "Software Manifests",
        "Measurements", "Software Measurement Results", "Intended Use", "geohash",
        "cmw", "wmver", "wmvnd", "wmpatlen", "wmsegduration", "wmpattern",
        "wmid", "wmopid", "wmkeyver", "catreplay", "catpor", "catv", "catnip",
        "catu", "catm", "catalpn", "cath", "catgeoiso3166", "catgeocoord",
        "catgeoalt", "cattpk", "catifdata", "catdpop", "catif", "catr",
        "psa-client-id", "psa-security-lifecycle", "psa-implementation-id",
        "psa-certification-reference", "psa-software-components",
        "psa-verification-service-indicator", "status_list", "ttl", "status"
    };

    /// <summary>Determines whether an integer CWT claim label is registered.</summary>
    /// <param name="label">The integer claim label.</param>
    /// <returns><c>true</c> when the label is registered; otherwise, <c>false</c>.</returns>
    internal static bool IsRegistered(int label)
    {
        return RegistryLookup.Contains(labels, label);
    }

    /// <summary>Gets the registered CWT claim name for a label.</summary>
    /// <param name="label">The integer claim label.</param>
    /// <returns>The registered name, or <c>null</c> when the label is unknown.</returns>
    internal static string? GetName(int label)
    {
        int index = RegistryLookup.IndexOf(labels, label);
        if (index < 0)
        {
            return null;
        }

        return names[index];
    }

    /// <summary>Returns all registered CWT claim labels.</summary>
    /// <returns>A new array containing the labels.</returns>
    internal static int[] GetAll()
    {
        return RegistryLookup.Copy(labels);
    }
}
