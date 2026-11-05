using System;
namespace token;

/// <summary>
/// Maps JWT claim names to the integer labels declared by the CWT Claims registry.
/// </summary>
internal static class JwtCwtClaimMap
{
    static readonly string[] jwtNames = new string[]
    {
        "hcert", "EUPHNonce", "EATMAROEPrefix", "EAT-FDO",
        JwtClaimNames.Issuer, JwtClaimNames.Subject, JwtClaimNames.Audience,
        JwtClaimNames.ExpirationTime, JwtClaimNames.NotBefore,
        JwtClaimNames.IssuedAt, JwtClaimNames.JwtId,
        JwtClaimNames.Confirmation, JwtClaimNames.Scope, "eat_nonce", "vct",
        "ace_profile", "cnonce", "exi", "identity-data", "ueid", "sueids",
        "oemid", "hwmodel", "hwversion", "uptime", "oemboot", "dbgstat",
        "location", "eat_profile", "submods", "bootcount", "bootseed", "dloas",
        "swname", "swversion", "manifests", "measurements", "measres", "intuse",
        "geohash", "cmw", "wmver", "wmvnd", "wmpatlen", "wmsegduration",
        "wmpattern", "wmid", "wmopid", "wmkeyver", "status_list", "ttl",
        "status"
    };

    static readonly int[] cwtLabels = new int[]
    {
        CwtClaimLabels.HealthCertificate, CwtClaimLabels.EatUnprotectedHeaderNonce,
        CwtClaimLabels.EatMeasuredArtifactPrefix,
        CwtClaimLabels.EatFidoDeviceOnboard, CwtClaimLabels.Issuer,
        CwtClaimLabels.Subject, CwtClaimLabels.Audience,
        CwtClaimLabels.ExpirationTime, CwtClaimLabels.NotBefore,
        CwtClaimLabels.IssuedAt, CwtClaimLabels.CwtId,
        CwtClaimLabels.Confirmation, CwtClaimLabels.Scope, CwtClaimLabels.Nonce,
        CwtClaimLabels.VerifiableCredentialType, CwtClaimLabels.AceProfile,
        CwtClaimLabels.ClientNonce, CwtClaimLabels.ExpiresIn,
        CwtClaimLabels.IdentityData, CwtClaimLabels.UniversalEntityIdentifier,
        CwtClaimLabels.SubmoduleEntityIdentifiers,
        CwtClaimLabels.HardwareOemIdentifier, CwtClaimLabels.HardwareModel,
        CwtClaimLabels.HardwareVersion, CwtClaimLabels.Uptime,
        CwtClaimLabels.OemAuthorizedBoot, CwtClaimLabels.DebugStatus,
        CwtClaimLabels.Location, CwtClaimLabels.EatProfile,
        CwtClaimLabels.Submodules, CwtClaimLabels.BootCount,
        CwtClaimLabels.BootSeed, CwtClaimLabels.DegreesOfLicenseAssurance,
        CwtClaimLabels.SoftwareName, CwtClaimLabels.SoftwareVersion,
        CwtClaimLabels.SoftwareManifests, CwtClaimLabels.Measurements,
        CwtClaimLabels.SoftwareMeasurementResults, CwtClaimLabels.IntendedUse,
        CwtClaimLabels.Geohash, CwtClaimLabels.CompositeMessageWrapper,
        CwtClaimLabels.WatermarkVersion, CwtClaimLabels.WatermarkVendor,
        CwtClaimLabels.WatermarkPatternLength,
        CwtClaimLabels.WatermarkSegmentDuration, CwtClaimLabels.WatermarkPattern,
        CwtClaimLabels.WatermarkIdentifier,
        CwtClaimLabels.WatermarkOperatorIdentifier,
        CwtClaimLabels.WatermarkKeyVersion,
        CwtClaimLabels.StatusList, CwtClaimLabels.TimeToLive,
        CwtClaimLabels.Status
    };

    /// <summary>Maps a case-sensitive JWT claim name to its CWT claim label.</summary>
    /// <param name="jwtName">The JWT claim name.</param>
    /// <returns>A new mapping result.</returns>
    internal static ClaimMappingResult FromJwt(string jwtName)
    {
        int index = IndexOf(jwtNames, jwtName);
        if (index < 0)
        {
            return new ClaimMappingResult(false, jwtName, 0);
        }

        return new ClaimMappingResult(true, jwtNames[index], cwtLabels[index]);
    }

    /// <summary>Maps a CWT claim label to its declared JWT claim name.</summary>
    /// <param name="cwtLabel">The CWT integer claim label.</param>
    /// <returns>A new mapping result.</returns>
    internal static ClaimMappingResult FromCwt(int cwtLabel)
    {
        int index = RegistryLookup.IndexOf(cwtLabels, cwtLabel);
        if (index < 0)
        {
            return new ClaimMappingResult(false, null, cwtLabel);
        }

        return new ClaimMappingResult(true, jwtNames[index], cwtLabels[index]);
    }

    /// <summary>Returns all JWT claim names with a declared CWT mapping.</summary>
    /// <returns>A new array containing the case-sensitive claim names.</returns>
    internal static string[] GetJwtClaims()
    {
        return RegistryLookup.Copy(jwtNames);
    }

    /// <summary>Returns all CWT claim labels with a declared JWT mapping.</summary>
    /// <returns>A new array aligned by index with <see cref="GetJwtClaims"/>.</returns>
    internal static int[] GetCwtClaims()
    {
        return RegistryLookup.Copy(cwtLabels);
    }

    static int IndexOf(string[] values, string value)
    {
        if (value == null)
        {
            return -1;
        }

        int index = 0;
        while (index < values.Length)
        {
            if (string.Equals(values[index], value, StringComparison.Ordinal))
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }
}
