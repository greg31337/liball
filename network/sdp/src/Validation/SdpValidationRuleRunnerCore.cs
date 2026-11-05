namespace sdp;

internal static class SdpValidationRuleRunnerCore
{
    internal static void RunSelectedRules(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpValidationProfile profile = result.AppliedOptions.Profile;
        if (profile == SdpValidationProfile.SyntaxOnly)
        {
            return;
        }
        if (profile == SdpValidationProfile.Rfc8866)
        {
            limiter.BeginRuleSet();
            SdpCoreValidationCore.ValidateCoreDescription(description, result, limiter);
            return;
        }
        if (profile == SdpValidationProfile.WebRtc || profile == SdpValidationProfile.CombinedStrict)
        {
            limiter.BeginRuleSet();
            SdpCoreValidationCore.ValidateCoreDescription(description, result, limiter);
            if (limiter.FatalIssueAdded)
            {
                return;
            }
        }
        if (SdpValidationCore.UsesRtpProfile(profile))
        {
            limiter.BeginRuleSet();
            SdpRtpValidationCore.ValidateRtpDescription(description, result, limiter);
        }
        if (SdpValidationCore.UsesIceProfile(profile))
        {
            limiter.BeginRuleSet();
            SdpIceValidationCore.ValidateIceDescription(description, result, limiter);
        }
        if (SdpValidationCore.UsesDtlsProfile(profile))
        {
            limiter.BeginRuleSet();
            SdpDtlsValidationCore.ValidateDtlsDescription(description, result, limiter);
        }
        if (SdpValidationCore.UsesGroupProfile(profile))
        {
            limiter.BeginRuleSet();
            SdpGroupValidationCore.ValidateGroupDescription(description, result, limiter);
            limiter.BeginRuleSet();
            SdpBundleValidationCore.ValidateBundleDescription(description, result, limiter);
        }
        if (SdpValidationCore.UsesDataChannelProfile(profile))
        {
            limiter.BeginRuleSet();
            SdpDataChannelValidationCore.ValidateDataChannelDescription(description, result, limiter);
        }
        if (SdpValidationCore.UsesBfcpProfile(profile))
        {
            limiter.BeginRuleSet();
            SdpBfcpValidationCore.ValidateBfcpDescription(description, result, limiter);
        }
        if (SdpValidationCore.UsesWebRtcProfile(profile))
        {
            limiter.BeginRuleSet();
            SdpWebRtcValidationCore.ValidateWebRtcDescription(description, result, limiter);
        }
        if (profile == SdpValidationProfile.WebRtc || profile == SdpValidationProfile.CombinedStrict)
        {
            limiter.BeginRuleSet();
            SdpAnswerValidationCore.ValidateAnswerDescription(description, result, limiter);
        }
    }
}
