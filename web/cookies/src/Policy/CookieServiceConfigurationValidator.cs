namespace cookies;

internal static class CookieServiceConfigurationValidator
{
    internal static bool IsValidMatcherConfiguration(
        CookieJarOptions options,
        IPublicSuffixList? publicSuffixList,
        ISecureOriginPolicy secureOriginPolicy,
        ICookieClock clock)
    {
        return secureOriginPolicy != null && clock != null &&
            IsValidJarOptions(options) &&
            (!options.RequirePublicSuffixList || publicSuffixList != null);
    }

    internal static bool IsValidJarConfiguration(
        CookieJarOptions options,
        ICookieParser parser,
        ICookiePolicy policy,
        ICookieStore store,
        ICookieSerializer serializer,
        ICookieClock clock)
    {
        return parser != null && policy != null && store != null &&
            serializer != null && clock != null && IsValidJarOptions(options);
    }

    internal static bool IsValidJarOptions(CookieJarOptions options)
    {
        if (options == null || options.ParseOptions == null ||
            options.SerializationOptions == null ||
            options.NetscapeFileOptions == null)
        {
            return false;
        }

        CookieValidationResult jarValidation =
            CookieAcceptanceValidator.ValidateOptions(options);
        if (!jarValidation.Success ||
            !CookieParser.IsValidOptions(options.ParseOptions) ||
            !CookieModelValidator.ValidateCookiePairs(
                [],
                options.SerializationOptions).Success ||
            !NetscapeCookieFileReader.IsValidOptions(options.NetscapeFileOptions))
        {
            return false;
        }

        if (options.ParseOptions.ComplianceMode != options.ComplianceMode ||
            options.SerializationOptions.ComplianceMode != options.ComplianceMode)
        {
            return false;
        }

        if (!HasPossibleQuota(options.Limits) ||
            !HasPossibleQuota(options.ParseOptions.Limits) ||
            !HasPossibleQuota(options.SerializationOptions.Limits) ||
            !HasPossibleQuota(options.NetscapeFileOptions.Limits))
        {
            return false;
        }

        return HasCompatibleNestedLimits(options);
    }

    internal static string CreateFailureMessage()
    {
        int errorCode = (int)CookieErrorCode.InvalidConfiguration;
        return $"Cookie service configuration failed with error code {errorCode}.";
    }

    static bool HasPossibleQuota(CookieLimits limits)
    {
        return limits.MaximumCookiesPerDomain <= limits.MaximumCookies;
    }

    static bool HasCompatibleNestedLimits(CookieJarOptions options)
    {
        CookieLimits outer = options.Limits;
        CookieLimits parse = options.ParseOptions.Limits;
        CookieLimits serialization = options.SerializationOptions.Limits;
        CookieLimits netscape = options.NetscapeFileOptions.Limits;
        if (parse.MaximumNameValueOctets > outer.MaximumNameValueOctets ||
            parse.MaximumAttributeValueOctets > outer.MaximumAttributeValueOctets ||
            parse.MaximumInputLineOctets > outer.MaximumInputLineOctets ||
            parse.MaximumLifetime > outer.MaximumLifetime ||
            serialization.MaximumNameValueOctets > outer.MaximumNameValueOctets ||
            serialization.MaximumAttributeValueOctets >
                outer.MaximumAttributeValueOctets ||
            serialization.MaximumInputLineOctets > outer.MaximumInputLineOctets ||
            serialization.MaximumLifetime > outer.MaximumLifetime ||
            netscape.MaximumNameValueOctets > outer.MaximumNameValueOctets ||
            netscape.MaximumAttributeValueOctets >
                outer.MaximumAttributeValueOctets ||
            netscape.MaximumInputLineOctets > outer.MaximumInputLineOctets ||
            netscape.MaximumLifetime > outer.MaximumLifetime)
        {
            return false;
        }

        if (outer.MaximumRequestHeaderOctets == 0)
        {
            return true;
        }

        return serialization.MaximumRequestHeaderOctets > 0 &&
            serialization.MaximumRequestHeaderOctets <=
                outer.MaximumRequestHeaderOctets;
    }
}
