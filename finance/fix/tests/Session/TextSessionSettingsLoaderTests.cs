using System;
using System.Globalization;
using System.IO;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class TextSessionSettingsLoaderTests
{
    [Test]
    public void DefaultsMergeIntoSessionsInSourceOrderWithoutSharingNestedState()
    {
        string text = """
            # shared policy
            [DEFAULT]
            ConnectionType=initiator
            HeartBtInt=20
            ReconnectInterval=2
            MaxReconnectDelayMilliseconds=9000
            SocketConnectPort=5000
            ThrottleMaxMessages=25
            QueueCapacity=50
            CheckFieldOrder=N

            [SESSION]
            BeginString=FIX.4.4
            SenderCompID=BUY1
            TargetCompID=SELL1
            SocketConnectHost=primary.example
            HeartBtInt=30

            [SESSION]
            BeginString=FIX.4.2
            SenderCompID=BUY2
            TargetCompID=SELL2
            SocketConnectHost=backup.example
            """;
        MemoryStream source = Source(text);

        SessionSettings[] settings = new TextSessionSettingsLoader().LoadSessionSettings(source);
        settings[0].Endpoints[0].Host = "changed";
        settings[0].Validation.CheckFieldOrder = true;

        Assert.Multiple(() =>
        {
            Assert.That(settings, Has.Length.EqualTo(2));
            Assert.That(settings[0].Id.SenderCompID, Is.EqualTo("BUY1"));
            Assert.That(settings[1].Id.SenderCompID, Is.EqualTo("BUY2"));
            Assert.That(settings[0].HeartbeatIntervalSeconds, Is.EqualTo(30));
            Assert.That(settings[1].HeartbeatIntervalSeconds, Is.EqualTo(20));
            Assert.That(settings[1].ReconnectDelayMilliseconds, Is.EqualTo(2000));
            Assert.That(settings[1].MaxReconnectDelayMilliseconds, Is.EqualTo(9000));
            Assert.That(settings[1].Endpoints[0].Host, Is.EqualTo("backup.example"));
            Assert.That(settings[1].Endpoints[0].Port, Is.EqualTo(5000));
            Assert.That(settings[1].Throttle.MaxMessages, Is.EqualTo(25));
            Assert.That(settings[1].Throttle.QueueCapacity, Is.EqualTo(50));
            Assert.That(settings[1].Validation.CheckFieldOrder, Is.False);
            Assert.That(settings[0].DefaultApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix44));
            Assert.That(settings[1].DefaultApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix42));
            Assert.That(source.CanRead, Is.True);
        });
    }

    [Test]
    public void FixtEndpointsStoresTlsThrottleValidationAndDictionariesAreLoaded()
    {
        string text = """
            [SESSION]
            Role=initiator
            BeginString=FIXT.1.1
            SenderCompID=BUY
            SenderSubID=ORDERS
            SenderLocationID=NY
            TargetCompID=SELL
            TargetSubID=MATCH
            TargetLocationID=LDN
            SessionQualifier=PRIMARY
            Endpoint=127.0.0.1,7001,0,true
            Endpoint1=::1,7002,1,true
            DefaultApplVerID=9
            TransportDataDictionary=transport-FIXT11.xml
            AppDataDictionary=application-FIX50SP2.xml
            ApplicationDictionaryScenario=base
            ApplicationDictionaryExtensionPack=309
            TlsEnabled=Y
            TlsTargetHost=fix.example
            TlsCertificatePath=/certs/client.pfx
            TlsCertificatePassword=secret
            TlsRequirePeerCertificate=true
            TlsCheckCertificateRevocation=false
            TlsAlpn=fix
            ThrottleMaxMessages=200
            ThrottleIntervalMilliseconds=500
            ThrottleBurstMessages=20
            QueueCapacity=300
            FileStorePath=/var/fix/store
            StoreMaximumMessages=10000
            FileLogPath=/var/log/fix/session.log
            RedactLogSensitiveFields=yes
            ValidationLayers=Envelope|SessionHeader|Datatype|Group
            ValidationMinimumSeverity=Warning
            ValidationMaxMessageSize=1048576
            ValidationMaxFieldSize=65536
            ValidationMaxFieldsPerMessage=2048
            ValidationMaxIssues=50
            RejectEmptyValues=true
            RejectDuplicateFields=false
            RejectUnknownFields=false
            AllowUserDefinedFields=true
            CheckFieldOrder=false
            CheckRequiredFields=true
            CheckGroups=true
            CheckConditionalRules=false
            CheckCompID=N
            CheckLatency=false
            MaxLatency=45
            RequiresOrigSendingTime=N
            ResetMode=OnLogon|OnDisconnect
            AllowSendBeforeLogon=1
            Username=operator
            Password=password
            """;

        SessionSettings result = LoadOne(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Role, Is.EqualTo(SessionRole.Initiator));
            Assert.That(result.Id.SenderSubID, Is.EqualTo("ORDERS"));
            Assert.That(result.Id.TargetLocationID, Is.EqualTo("LDN"));
            Assert.That(result.Id.Qualifier, Is.EqualTo("PRIMARY"));
            Assert.That(result.Endpoints, Has.Count.EqualTo(2));
            Assert.That(result.Endpoints[0].Host, Is.EqualTo("127.0.0.1"));
            Assert.That(result.Endpoints[1].Host, Is.EqualTo("::1"));
            Assert.That(result.Endpoints[1].Priority, Is.EqualTo(1));
            Assert.That(result.Endpoints.TrueForAll(endpoint => endpoint.UseTls), Is.True);
            Assert.That(result.DefaultApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp2));
            Assert.That(result.TransportDictionary.BeginString, Is.EqualTo(FixBeginStrings.Fixt11));
            Assert.That(result.TransportDictionary.Source, Is.EqualTo("transport-FIXT11.xml"));
            Assert.That(result.ApplicationDictionary.Source, Is.EqualTo("application-FIX50SP2.xml"));
            Assert.That(result.ApplicationDictionary.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp2));
            Assert.That(result.ApplicationDictionary.Scenario, Is.EqualTo("base"));
            Assert.That(result.ApplicationDictionary.HasExtensionPack, Is.True);
            Assert.That(result.ApplicationDictionary.ExtensionPack, Is.EqualTo(309));
            Assert.That(result.Tls.TargetHost, Is.EqualTo("fix.example"));
            Assert.That(result.Tls.CheckCertificateRevocation, Is.False);
            Assert.That(result.Throttle.MaxMessages, Is.EqualTo(200));
            Assert.That(result.Throttle.IntervalMilliseconds, Is.EqualTo(500));
            Assert.That(result.Throttle.BurstMessages, Is.EqualTo(20));
            Assert.That(result.Throttle.QueueCapacity, Is.EqualTo(300));
            Assert.That(result.StoreType, Is.EqualTo("File"));
            Assert.That(result.StorePath, Is.EqualTo("/var/fix/store"));
            Assert.That(result.StoreMaximumMessages, Is.EqualTo(10000));
            Assert.That(result.LogType, Is.EqualTo("File"));
            Assert.That(result.LogPath, Is.EqualTo("/var/log/fix/session.log"));
            Assert.That(result.RedactLogSensitiveFields, Is.True);
            Assert.That(result.Validation.Layers, Is.EqualTo(ValidationLayer.Envelope | ValidationLayer.SessionHeader | ValidationLayer.Datatype | ValidationLayer.Group));
            Assert.That(result.Validation.MinimumSeverity, Is.EqualTo(ValidationSeverity.Warning));
            Assert.That(result.Validation.MaxMessageSize, Is.EqualTo(1048576));
            Assert.That(result.Validation.MaxFieldSize, Is.EqualTo(65536));
            Assert.That(result.Validation.MaxFieldsPerMessage, Is.EqualTo(2048));
            Assert.That(result.Validation.MaxIssues, Is.EqualTo(50));
            Assert.That(result.Validation.RejectDuplicateFields, Is.False);
            Assert.That(result.Validation.RejectUnknownFields, Is.False);
            Assert.That(result.Validation.AllowUserDefinedFields, Is.True);
            Assert.That(result.Validation.CheckFieldOrder, Is.False);
            Assert.That(result.Validation.CheckConditionalRules, Is.False);
            Assert.That(result.CheckCompIds, Is.False);
            Assert.That(result.CheckLatency, Is.False);
            Assert.That(result.MaxLatencySeconds, Is.EqualTo(45));
            Assert.That(result.RequireOrigSendingTime, Is.False);
            Assert.That(result.ResetMode, Is.EqualTo(SessionResetMode.OnLogon | SessionResetMode.OnDisconnect));
            Assert.That(result.AllowSendBeforeLogon, Is.True);
            Assert.That(result.Username, Is.EqualTo("operator"));
            Assert.That(result.Password, Is.EqualTo("password"));
        });
    }

    [Test]
    public void WeeklyAndExplicitDisabledScheduleWindowsAreInvariant()
    {
        string text = """
            [SESSION]
            ConnectionType=acceptor
            BeginString=FIX.4.4
            SenderCompID=SELL
            TargetCompID=BUY
            SocketAcceptAddress=0.0.0.0
            SocketAcceptPort=9000
            NonStopSession=N
            TimeZone=America/New_York
            StartTime=09:30:00
            EndTime=16:15:30
            Weekdays=Mon,Tue,Wed,Thu,Fri
            ScheduleWindow1=Saturday,10:00,Saturday,11:00,false
            """;

        SessionSettings result = LoadOne(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Role, Is.EqualTo(SessionRole.Acceptor));
            Assert.That(result.Schedule.NonStop, Is.False);
            Assert.That(result.Schedule.TimeZoneId, Is.EqualTo("America/New_York"));
            Assert.That(result.Schedule.Windows, Has.Count.EqualTo(1));
            Assert.That(result.Schedule.Windows[0].StartDay, Is.EqualTo(DayOfWeek.Saturday));
            Assert.That(result.Schedule.Windows[0].StartTime, Is.EqualTo(new TimeSpan(10, 0, 0)));
            Assert.That(result.Schedule.Windows[0].Enabled, Is.False);
        });
    }

    [Test]
    public void DailyScheduleExpandsToSevenIndependentWindows()
    {
        string text = """
            [SESSION]
            ConnectionType=acceptor
            BeginString=FIX.4.2
            SenderCompID=SELL
            TargetCompID=BUY
            SocketAcceptAddress=127.0.0.1
            SocketAcceptPort=9000
            NonStopSession=false
            StartTime=22:00
            EndTime=05:00
            """;

        SessionSettings result = LoadOne(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Schedule.Windows, Has.Count.EqualTo(7));
            Assert.That(result.Schedule.Windows[0].StartDay, Is.EqualTo(DayOfWeek.Sunday));
            Assert.That(result.Schedule.Windows[6].StartDay, Is.EqualTo(DayOfWeek.Saturday));
            Assert.That(result.Schedule.Windows.TrueForAll(window => window.StartTime == new TimeSpan(22, 0, 0)), Is.True);
            Assert.That(result.Schedule.Windows.TrueForAll(window => window.EndTime == new TimeSpan(5, 0, 0)), Is.True);
        });
    }

    [Test]
    public void ResetModeFlagsCanIndependentlyRemoveAndAddConfiguredBoundaries()
    {
        string text = """
            [SESSION]
            ConnectionType=initiator
            BeginString=FIX.4.4
            SenderCompID=BUY
            TargetCompID=SELL
            Endpoint=localhost,9000
            ResetMode=OnLogon|OnLogout|OnDisconnect
            ResetOnLogon=false
            ResetOnLogout=0
            ResetOnSessionBoundary=yes
            """;

        SessionSettings result = LoadOne(text);

        Assert.That(result.ResetMode, Is.EqualTo(SessionResetMode.OnDisconnect | SessionResetMode.OnSessionBoundary));
    }

    [Test]
    public void NumericBooleanDayAndTimeParsingIgnoreCurrentCulture()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
        CultureInfo.CurrentUICulture = new CultureInfo("ar-SA");
        SessionSettings result;
        try
        {
            result = LoadOne("""
                [SESSION]
                ConnectionType=initiator
                BeginString=FIX.4.4
                SenderCompID=BUY
                TargetCompID=SELL
                Endpoint=localhost,9876,3,N
                NonStopSession=N
                StartDay=Monday
                StartTime=09:05:07
                EndDay=Friday
                EndTime=17:06:08
                HeartBtInt=30
                """);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        Assert.Multiple(() =>
        {
            Assert.That(result.Endpoints[0].Port, Is.EqualTo(9876));
            Assert.That(result.Endpoints[0].Priority, Is.EqualTo(3));
            Assert.That(result.Endpoints[0].UseTls, Is.False);
            Assert.That(result.Schedule.Windows[0].StartDay, Is.EqualTo(DayOfWeek.Monday));
            Assert.That(result.Schedule.Windows[0].StartTime, Is.EqualTo(new TimeSpan(9, 5, 7)));
            Assert.That(result.Schedule.Windows[0].EndDay, Is.EqualTo(DayOfWeek.Friday));
        });
    }

    [TestCase("SenderCompID=BUY", "settings:setting-outside-section:line:1")]
    [TestCase("[UNKNOWN]\nKey=Value", "settings:unknown-settings-section:line:1")]
    [TestCase("[SESSION]\nBeginString=FIX.4.4\nBeginString=FIX.4.2", "settings:duplicate-setting:line:3")]
    [TestCase("[SESSION]\nConnectionType=other", "settings:invalid-session-role:line:2")]
    [TestCase("[SESSION]\nConnectionType=initiator\nBeginString=FIX.4.4\nSenderCompID=BUY\nTargetCompID=SELL\nEndpoint=localhost,abc", "settings:invalid-integer:line:6")]
    [TestCase("[SESSION]\nConnectionType=initiator\nBeginString=FIX.4.4\nSenderCompID=BUY\nTargetCompID=SELL\nEndpoint=localhost,9000\nUnknownFlag=Y", "settings:unknown-setting:line:7")]
    public void SyntaxAndValueDiagnosticsHaveStableCodesAndLines(string text, string expectedPrefix)
    {
        FixSessionException exception = Assert.Throws<FixSessionException>(() => new TextSessionSettingsLoader().LoadSessionSettings(Source(text)))!;

        Assert.That(exception.Message, Does.StartWith(expectedPrefix));
    }

    [TestCase("FIXT.1.1", "", "settings:missing-fixt-application-version")]
    [TestCase("FIX.4.4", "FileStorePath=", "settings:missing-file-store-path")]
    [TestCase("FIX.4.4", "FileLogPath=", "settings:missing-file-log-path")]
    [TestCase("FIX.4.4", "MaxReconnectDelayMilliseconds=10\nReconnectDelayMilliseconds=20", "settings:invalid-reconnect-range")]
    public void CrossFieldDiagnosticsAreStable(string beginString, string extra, string expectedCode)
    {
        string text = $"""
            [SESSION]
            ConnectionType=initiator
            BeginString={beginString}
            SenderCompID=BUY
            TargetCompID=SELL
            Endpoint=localhost,9000
            {extra}
            """;

        FixSessionException exception = Assert.Throws<FixSessionException>(() => new TextSessionSettingsLoader().LoadSessionSettings(Source(text)))!;

        Assert.That(exception.Message, Does.StartWith(expectedCode));
    }

    [Test]
    public void MissingUnreadableAndSessionlessSourcesAreRejected()
    {
        TextSessionSettingsLoader loader = new TextSessionSettingsLoader();
        MemoryStream closed = new MemoryStream();
        closed.Close();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<FixSessionException>(() => loader.LoadSessionSettings(null!))!.Message, Does.StartWith("settings:missing-settings-source"));
            Assert.That(Assert.Throws<FixSessionException>(() => loader.LoadSessionSettings(closed))!.Message, Does.StartWith("settings:missing-settings-source"));
            Assert.That(Assert.Throws<FixSessionException>(() => loader.LoadSessionSettings(Source("[DEFAULT]\nHeartBtInt=30")))!.Message, Does.StartWith("settings:missing-session-section"));
        });
    }

    private static SessionSettings LoadOne(string text)
    {
        SessionSettings[] settings = new TextSessionSettingsLoader().LoadSessionSettings(Source(text));
        Assert.That(settings, Has.Length.EqualTo(1));
        return settings[0];
    }

    private static MemoryStream Source(string text)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(text));
    }
}
