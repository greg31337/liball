namespace ftp;

internal class FtpConnectionProfileProbe
{
    internal FtpClientOptions Options;
    internal IFtpCertificateValidator? CertificateValidator;

    internal FtpConnectionProfileProbe(
        FtpClientOptions options,
        IFtpCertificateValidator? certificateValidator)
    {
        Options = options;
        CertificateValidator = certificateValidator;
    }

    internal FtpConnectionProbeResult ProbeProfiles()
    {
        Options.Credentials = new FtpCredentials();
        FtpConnectionProbeResult result = new FtpConnectionProbeResult();
        result.Profiles = new FtpConnectionProfile[3];
        result.Profiles[0] = ProbeProfile(FtpSecurityMode.ImplicitTls);
        result.Profiles[1] = ProbeProfile(FtpSecurityMode.ExplicitTlsRequired);
        result.Profiles[2] = ProbeProfile(FtpSecurityMode.Plain);
        result.Complete = true;
        if (!result.Profiles[0].Viable &&
            !result.Profiles[1].Viable &&
            !result.Profiles[2].Viable)
        {
            result.FailureMessage =
                "No credential-free FTP connection profile was viable.";
        }
        return result;
    }

    internal FtpConnectionProfile ProbeProfile(FtpSecurityMode securityMode)
    {
        FtpConnectionProfile profile = new FtpConnectionProfile();
        profile.Endpoint = FtpClientOptionsSnapshot.CopyEndpoint(Options.Endpoint);
        profile.SecurityMode = securityMode;
        Options.SecurityMode = securityMode;
        FtpFaultIsolationResult probeResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            FtpClientOptions profileOptions =
                FtpClientOptionsSnapshot.CreateValidated(
                    Options, CertificateValidator);
            FtpSessionStateMachine session = new FtpSessionStateMachine();
            FtpClientSecurityConnector connector =
                new FtpClientSecurityConnector(
                    profileOptions, CertificateValidator, session);
            return connector.OpenConnection();
        });
        FtpControlConnection? connection =
            probeResult.Value as FtpControlConnection;
        if (probeResult.Succeeded && connection != null)
        {
            profile.Viable = true;
            profile.ControlProtected = connection.ControlProtected;
            profile.Greeting = connection.Greeting;
            FtpFaultIsolationResult cleanup = connection.Close();
            if (!cleanup.Succeeded)
            {
                profile.Viable = false;
                profile.ControlProtected = false;
                profile.FailureMessage =
                    "The successful FTP profile probe could not close its control connection.";
            }
            return profile;
        }

        profile.FailureMessage = probeResult.Failure == null ?
            "The FTP profile probe did not reach a terminal result." :
            probeResult.Failure.Message;
        return profile;
    }
}
