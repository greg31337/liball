using System;

namespace odf;

internal static class OdfCompatibilityProfileRules
{
    internal static CompatibilityProfilePolicy Resolve(string profile)
    {
        CompatibilityProfilePolicy result = new CompatibilityProfilePolicy();
        if (profile == null || profile.Length == 0)
        {
            result.IsValid = true;
            return result;
        }
        result.IsActive = true;
        if (profile == OdfCompatibilityProfiles.StrictOasis
            || profile == OdfVersionRules.StandardProfile)
        {
            result.Identifier = profile;
            result.IsValid = true;
            result.ConformanceClass = OdfConformanceClass.Strict;
            return result;
        }
        if (profile == OdfCompatibilityProfiles.Interoperable)
        {
            result.Identifier = profile;
            result.IsValid = true;
            result.ConformanceClass = OdfConformanceClass.Conforming;
            return result;
        }
        if (profile == OdfCompatibilityProfiles.LibreOfficeExtended)
        {
            result.Identifier = profile;
            result.IsValid = true;
            result.PreserveLibreOfficeExtensions = true;
            result.PreserveOpenOfficeExtensions = true;
            result.ConformanceClass = OdfConformanceClass.Extended;
            return result;
        }
        if (profile == OdfCompatibilityProfiles.OpenOfficeExtended)
        {
            result.Identifier = profile;
            result.IsValid = true;
            result.PreserveOpenOfficeExtensions = true;
            result.ConformanceClass = OdfConformanceClass.Extended;
            return result;
        }
        if (profile == OdfCompatibilityProfiles.MicrosoftOfficeExtended)
        {
            result.Identifier = profile;
            result.IsValid = true;
            result.PreserveMicrosoftOfficeExtensions = true;
            result.ConformanceClass = OdfConformanceClass.Extended;
            return result;
        }
        if (profile == OdfCompatibilityProfiles.PreserveExisting)
        {
            result.Identifier = profile;
            result.IsValid = true;
            result.PreserveAllExtensions = true;
            return result;
        }
        if (profile == OdfVersionRules.GenericExtendedProfile
            || profile == OdfVersionRules.CalligraExtendedProfile)
        {
            result.Identifier = profile;
            result.IsValid = true;
            result.PreserveAllExtensions = true;
            result.ConformanceClass = OdfConformanceClass.Extended;
            return result;
        }
        return result;
    }

    internal static bool AllowsNamespace(
        CompatibilityProfilePolicy policy,
        string namespaceName)
    {
        if (OdfNamespaceScope.IsKnownNamespaceName(namespaceName))
        {
            return true;
        }
        if (!policy.IsActive || policy.PreserveAllExtensions)
        {
            return true;
        }
        if (policy.PreserveLibreOfficeExtensions
            && IsLibreOfficeNamespace(namespaceName))
        {
            return true;
        }
        if (policy.PreserveOpenOfficeExtensions
            && IsOpenOfficeNamespace(namespaceName))
        {
            return true;
        }
        return policy.PreserveMicrosoftOfficeExtensions
            && IsMicrosoftOfficeNamespace(namespaceName);
    }

    internal static bool IsLibreOfficeNamespace(string namespaceName)
    {
        return ContainsIgnoreCase(namespaceName, "libreoffice")
            || ContainsIgnoreCase(namespaceName, "documentfoundation");
    }

    internal static bool IsOpenOfficeNamespace(string namespaceName)
    {
        return ContainsIgnoreCase(namespaceName, "openoffice")
            || ContainsIgnoreCase(namespaceName, "staroffice");
    }

    internal static bool IsMicrosoftOfficeNamespace(string namespaceName)
    {
        return ContainsIgnoreCase(namespaceName, "microsoft")
            || ContainsIgnoreCase(namespaceName, "schemas.microsoft.com");
    }

    static bool ContainsIgnoreCase(string value, string expected)
    {
        return value != null
            && value.IndexOf(
                expected,
                StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
