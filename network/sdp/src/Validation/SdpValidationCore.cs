using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpValidationCore
{
    internal static SdpValidationResult ValidateDescription(SdpSessionDescription description, SdpValidationOptions options)
    {
        ValidateValidationOptions(options);
        SdpValidationResult result = new SdpValidationResult();
        result.AppliedOptions = CopyValidationOptions(options);
        SdpValidationIssueLimiter limiter = new SdpValidationIssueLimiter(result.Issues, options.MaximumDiagnosticCount);
        SdpValidationRuleRunnerCore.RunSelectedRules(description, result, limiter);
        SdpValidationIssueMetadataCore.CompleteIssues(description, result.Issues);
        result.DiagnosticLimitReached = limiter.DiagnosticLimitReached;
        PopulateValidationCounts(result);
        result.Success = result.ErrorCount == 0;
        return result;
    }

    internal static void AddValidationIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, string code, SdpValidationSeverity severity, SdpScope scope, string attributeName, int mediaIndex, int occurrenceIndex, string detail, SdpSourceLocation location)
    {
        if (severity != SdpValidationSeverity.Fatal && ContainsValidationCode(result.AppliedOptions.SuppressedDiagnosticCodes, code))
        {
            return;
        }
        SdpValidationSeverity effectiveSeverity = severity;
        if (severity == SdpValidationSeverity.Warning && ContainsValidationCode(result.AppliedOptions.PromotedDiagnosticCodes, code))
        {
            effectiveSeverity = SdpValidationSeverity.Error;
        }
        if ((int)effectiveSeverity < (int)result.AppliedOptions.MinimumSeverity)
        {
            return;
        }

        SdpSourcePath path = new SdpSourcePath();
        path.Scope = scope;
        path.MediaIndex = mediaIndex;
        path.FieldName = 'a';
        path.AttributeName = attributeName;
        path.OccurrenceIndex = occurrenceIndex;
        SdpValidationIssue issue = SdpDiagnosticBuilder.CreateValidationIssue(code, effectiveSeverity, scope, 'a', attributeName, mediaIndex, detail, path, SdpDiagnosticBuilder.CopySourceLocation(location));
        limiter.AddIssue(issue);
    }

    internal static void AddFieldValidationIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, string code, SdpValidationSeverity severity, SdpScope scope, char fieldName, int mediaIndex, int occurrenceIndex, string detail, SdpSourceLocation location)
    {
        if (severity != SdpValidationSeverity.Fatal && ContainsValidationCode(result.AppliedOptions.SuppressedDiagnosticCodes, code))
        {
            return;
        }
        SdpValidationSeverity effectiveSeverity = severity;
        if (severity == SdpValidationSeverity.Warning && ContainsValidationCode(result.AppliedOptions.PromotedDiagnosticCodes, code))
        {
            effectiveSeverity = SdpValidationSeverity.Error;
        }
        if ((int)effectiveSeverity < (int)result.AppliedOptions.MinimumSeverity)
        {
            return;
        }

        SdpSourcePath path = new SdpSourcePath();
        path.Scope = scope;
        path.MediaIndex = mediaIndex;
        path.FieldName = fieldName;
        path.OccurrenceIndex = occurrenceIndex;
        SdpValidationIssue issue = SdpDiagnosticBuilder.CreateValidationIssue(code, effectiveSeverity, scope, fieldName, string.Empty, mediaIndex, detail, path, SdpDiagnosticBuilder.CopySourceLocation(location));
        limiter.AddIssue(issue);
    }

    internal static bool UsesIceProfile(SdpValidationProfile profile)
    {
        return profile == SdpValidationProfile.Ice || profile == SdpValidationProfile.WebRtc || profile == SdpValidationProfile.CombinedStrict;
    }

    internal static bool UsesDtlsProfile(SdpValidationProfile profile)
    {
        return profile == SdpValidationProfile.TlsDtls || profile == SdpValidationProfile.WebRtc || profile == SdpValidationProfile.CombinedStrict;
    }

    internal static bool UsesRtpProfile(SdpValidationProfile profile)
    {
        return profile == SdpValidationProfile.Rtp || profile == SdpValidationProfile.WebRtc || profile == SdpValidationProfile.CombinedStrict;
    }

    internal static bool UsesGroupProfile(SdpValidationProfile profile)
    {
        return profile == SdpValidationProfile.Bundle || profile == SdpValidationProfile.WebRtc || profile == SdpValidationProfile.CombinedStrict;
    }

    internal static bool UsesDataChannelProfile(SdpValidationProfile profile)
    {
        return profile == SdpValidationProfile.DataChannel || profile == SdpValidationProfile.WebRtc || profile == SdpValidationProfile.CombinedStrict;
    }

    internal static bool UsesBfcpProfile(SdpValidationProfile profile)
    {
        return profile == SdpValidationProfile.Bfcp || profile == SdpValidationProfile.CombinedStrict;
    }

    internal static bool UsesWebRtcProfile(SdpValidationProfile profile)
    {
        return profile == SdpValidationProfile.WebRtc || profile == SdpValidationProfile.CombinedStrict;
    }

    internal static void ValidateValidationOptions(SdpValidationOptions options)
    {
        bool knownProfile = options.Profile == SdpValidationProfile.SyntaxOnly || options.Profile == SdpValidationProfile.Rfc8866 || options.Profile == SdpValidationProfile.Ice || options.Profile == SdpValidationProfile.TlsDtls || options.Profile == SdpValidationProfile.Rtp || options.Profile == SdpValidationProfile.WebRtc || options.Profile == SdpValidationProfile.Bundle || options.Profile == SdpValidationProfile.DataChannel || options.Profile == SdpValidationProfile.Bfcp || options.Profile == SdpValidationProfile.CombinedStrict;
        bool knownRole = options.NegotiationRole == SdpNegotiationRole.Unknown || options.NegotiationRole == SdpNegotiationRole.Offerer || options.NegotiationRole == SdpNegotiationRole.Answerer;
        bool knownSeverity = options.MinimumSeverity == SdpValidationSeverity.Information || options.MinimumSeverity == SdpValidationSeverity.Warning || options.MinimumSeverity == SdpValidationSeverity.Error || options.MinimumSeverity == SdpValidationSeverity.Fatal;
        if (!knownProfile || !knownRole || !knownSeverity || options.MaximumDiagnosticCount < 0)
        {
            throw new ArgumentOutOfRangeException("options");
        }
    }

    internal static SdpValidationOptions CopyValidationOptions(SdpValidationOptions options)
    {
        SdpValidationOptions result = new SdpValidationOptions();
        result.Profile = options.Profile;
        result.NegotiationRole = options.NegotiationRole;
        result.OfferDescription = options.OfferDescription;
        result.MinimumSeverity = options.MinimumSeverity;
        result.MaximumDiagnosticCount = options.MaximumDiagnosticCount;
        int index = 0;
        while (index < options.SuppressedDiagnosticCodes.Count)
        {
            result.SuppressedDiagnosticCodes.Add(options.SuppressedDiagnosticCodes[index]);
            index = index + 1;
        }
        index = 0;
        while (index < options.PromotedDiagnosticCodes.Count)
        {
            result.PromotedDiagnosticCodes.Add(options.PromotedDiagnosticCodes[index]);
            index = index + 1;
        }
        return result;
    }

    internal static bool ContainsValidationCode(List<string> codes, string code)
    {
        int index = 0;
        while (index < codes.Count)
        {
            if (codes[index] == code)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static void PopulateValidationCounts(SdpValidationResult result)
    {
        result.WarningCount = 0;
        result.ErrorCount = 0;
        int index = 0;
        while (index < result.Issues.Count)
        {
            SdpValidationSeverity severity = result.Issues[index].Severity;
            if (severity == SdpValidationSeverity.Warning)
            {
                result.WarningCount = result.WarningCount + 1;
            }
            else if (severity == SdpValidationSeverity.Error || severity == SdpValidationSeverity.Fatal)
            {
                result.ErrorCount = result.ErrorCount + 1;
            }
            index = index + 1;
        }
    }
}
