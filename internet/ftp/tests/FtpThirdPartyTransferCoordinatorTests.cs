using System.Net;

namespace ftp.Tests;

public class FtpThirdPartyTransferCoordinatorTests
{
    [Test]
    public void EPSV_endpoint_is_relayed_to_the_source_as_EPRT()
    {
        FtpThirdPartyTransferCoordinator coordinator = CreateCoordinator(
            IPAddress.Parse("2001:db8::20"));

        FtpCommand passive = coordinator.BeginTargetExtendedPassiveSetup(
            FtpAddressFamily.IPv6);
        coordinator.ApplyTargetPassiveSetupReply(
            CreateReply(229, "Entering Extended Passive Mode (|||50123|)"));
        FtpCommand active = coordinator.BeginSourceExtendedActiveSetup();
        coordinator.ApplySourceActiveSetupReply(
            CreateReply(200, "EPRT command successful"));
        FtpThirdPartyTransferState state = coordinator.GetCurrentState();

        Assert.Multiple(() =>
        {
            Assert.That(passive.Kind, Is.EqualTo(FtpCommandKind.Epsv));
            Assert.That(passive.Argument, Is.EqualTo("2"));
            Assert.That(active.Kind, Is.EqualTo(FtpCommandKind.Eprt));
            Assert.That(active.Argument, Is.EqualTo("|2|2001:db8::20|50123|"));
            Assert.That(state.PassiveEndpoint!.Host, Is.EqualTo("2001:db8::20"));
            Assert.That(state.PassiveEndpoint.Port, Is.EqualTo(50123));
            Assert.That(state.TargetPassiveConfigured, Is.True);
            Assert.That(state.SourceActiveConfigured, Is.True);
            Assert.That(state.TargetCommandResults.Single().Succeeded, Is.True);
            Assert.That(state.SourceCommandResults.Single().Succeeded, Is.True);
        });
    }

    [Test]
    public void PASV_advertisement_can_be_relayed_to_the_source_as_PORT()
    {
        FtpDataConnectionOptions source = CreateFxpOptions();
        source.UseExtendedActive = false;
        FtpDataConnectionOptions target = CreateFxpOptions();
        target.UseExtendedPassive = false;
        target.IgnorePassiveAdvertisedAddress = false;
        FtpThirdPartyTransferCoordinator coordinator = new FtpThirdPartyTransferCoordinator(
            IPAddress.Parse("192.0.2.20"), source, target,
            FtpDataProtectionLevel.Clear, FtpDataProtectionLevel.Clear);

        FtpCommand passive = coordinator.BeginTargetPassiveSetup();
        coordinator.ApplyTargetPassiveSetupReply(
            CreateReply(227, "Entering Passive Mode (198,51,100,25,195,80)"));
        FtpCommand active = coordinator.BeginSourceActiveSetup();
        coordinator.ApplySourceActiveSetupReply(
            CreateReply(200, "PORT command successful"));

        Assert.Multiple(() =>
        {
            Assert.That(passive.Kind, Is.EqualTo(FtpCommandKind.Pasv));
            Assert.That(active.Kind, Is.EqualTo(FtpCommandKind.Port));
            Assert.That(active.Argument, Is.EqualTo("198,51,100,25,195,80"));
            Assert.That(coordinator.State.PassiveEndpoint!.Host,
                Is.EqualTo("198.51.100.25"));
            Assert.That(coordinator.State.PassiveEndpoint.Port, Is.EqualTo(50000));
        });
    }

    [Test]
    public void Standard_EPSV_and_EPRT_rejections_are_recorded_before_IPV4_fallbacks()
    {
        FtpThirdPartyTransferCoordinator coordinator = CreateCoordinator(
            IPAddress.Parse("192.0.2.20"));
        coordinator.BeginTargetExtendedPassiveSetup(FtpAddressFamily.IPv4);

        FtpCommand passiveFallback = coordinator.BeginTargetPassiveFallback(
            CreateReply(500, "EPSV not implemented"));
        coordinator.ApplyTargetPassiveSetupReply(
            CreateReply(227, "Entering Passive Mode (192,0,2,20,195,81)"));
        coordinator.BeginSourceExtendedActiveSetup();
        FtpCommand activeFallback = coordinator.BeginSourceActiveFallback(
            CreateReply(522, "Network protocol not supported, use (1)"));
        coordinator.ApplySourceActiveSetupReply(
            CreateReply(200, "PORT command successful"));
        FtpThirdPartyTransferState state = coordinator.GetCurrentState();

        Assert.Multiple(() =>
        {
            Assert.That(passiveFallback.Kind, Is.EqualTo(FtpCommandKind.Pasv));
            Assert.That(activeFallback.Kind, Is.EqualTo(FtpCommandKind.Port));
            Assert.That(state.TargetCommandResults.Select(value => value.Command.Kind),
                Is.EqualTo(new[] { FtpCommandKind.Epsv, FtpCommandKind.Pasv }));
            Assert.That(state.SourceCommandResults.Select(value => value.Command.Kind),
                Is.EqualTo(new[] { FtpCommandKind.Eprt, FtpCommandKind.Port }));
            Assert.That(state.TargetCommandResults.Select(value => value.Succeeded),
                Is.EqualTo(new[] { false, true }));
            Assert.That(state.SourceCommandResults.Select(value => value.Succeeded),
                Is.EqualTo(new[] { false, true }));
            Assert.Throws<FtpProtocolException>(() =>
                coordinator.BeginTargetPassiveFallback(
                    CreateReply(500, "No pending EPSV")));
            Assert.Throws<FtpProtocolException>(() =>
                coordinator.BeginSourceActiveFallback(
                    CreateReply(500, "No pending EPRT")));
        });
    }

    [Test]
    public void Paired_transfer_completes_only_after_both_sessions_receive_terminal_replies()
    {
        FtpThirdPartyTransferCoordinator coordinator = CreateReadyCoordinator();
        FtpCommand source = CreateCommand("RETR source.bin");
        FtpCommand target = CreateCommand("STOR target.bin");
        coordinator.BeginPairedTransfer(source, target);

        FtpThirdPartyTransferState targetStarted = coordinator.ApplyTargetTransferReply(
            CreateReply(150, "Target ready"));
        FtpThirdPartyTransferState sourceStarted = coordinator.ApplySourceTransferReply(
            CreateReply(125, "Source data connection already open"));
        FtpThirdPartyTransferState sourceFinished = coordinator.ApplySourceTransferReply(
            CreateReply(226, "Source transfer complete"));
        FtpThirdPartyTransferState complete = coordinator.ApplyTargetTransferReply(
            CreateReply(250, "Target action complete"));

        Assert.Multiple(() =>
        {
            Assert.That(targetStarted.Complete, Is.False);
            Assert.That(targetStarted.SourceTransferPending, Is.True);
            Assert.That(sourceStarted.Complete, Is.False);
            Assert.That(sourceFinished.Complete, Is.False);
            Assert.That(sourceFinished.SourceTransferPending, Is.False);
            Assert.That(sourceFinished.TargetTransferPending, Is.True);
            Assert.That(complete.Complete, Is.True);
            Assert.That(complete.Succeeded, Is.True);
            Assert.That(complete.SourceTransferResult!.Command, Is.SameAs(source));
            Assert.That(complete.TargetTransferResult!.Command, Is.SameAs(target));
            Assert.That(complete.SourceTransferResult.Replies.Select(value => value.Code),
                Is.EqualTo(new[] { 125, 226 }));
            Assert.That(complete.TargetTransferResult.Replies.Select(value => value.Code),
                Is.EqualTo(new[] { 150, 250 }));
            Assert.That(complete.SourceCommandResults.Last(),
                Is.SameAs(complete.SourceTransferResult));
            Assert.That(complete.TargetCommandResults.Last(),
                Is.SameAs(complete.TargetTransferResult));
        });
    }

    [Test]
    public void Failure_on_either_session_is_retained_while_the_other_terminal_reply_is_consumed()
    {
        FtpThirdPartyTransferCoordinator coordinator = CreateReadyCoordinator();
        coordinator.BeginPairedTransfer(
            CreateCommand("RETR missing.bin"), CreateCommand("STOR partial.bin"));

        FtpThirdPartyTransferState sourceFailed = coordinator.ApplySourceTransferReply(
            CreateReply(425, "Source cannot open data connection"));
        coordinator.ApplyTargetTransferReply(CreateReply(150, "Target ready"));
        FtpThirdPartyTransferState complete = coordinator.ApplyTargetTransferReply(
            CreateReply(426, "Target transfer aborted"));

        Assert.Multiple(() =>
        {
            Assert.That(sourceFailed.Complete, Is.False);
            Assert.That(sourceFailed.SourceTransferPending, Is.False);
            Assert.That(sourceFailed.TargetTransferPending, Is.True);
            Assert.That(complete.Complete, Is.True);
            Assert.That(complete.Succeeded, Is.False);
            Assert.That(complete.SourceTransferResult!.FinalReply!.Code, Is.EqualTo(425));
            Assert.That(complete.TargetTransferResult!.FinalReply!.Code, Is.EqualTo(426));
            Assert.Throws<FtpProtocolException>(() =>
                coordinator.ApplySourceTransferReply(CreateReply(226, "Late reply")));
            Assert.Throws<FtpProtocolException>(() =>
                coordinator.ApplyTargetTransferReply(CreateReply(226, "Late reply")));
        });
    }

    [Test]
    public void FXP_requires_explicit_policy_on_both_sessions_and_clear_data_protection()
    {
        FtpDataConnectionOptions allowed = CreateFxpOptions();
        FtpDataConnectionOptions denied = new FtpDataConnectionOptions();

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpThirdPartyTransferCoordinator(
                    IPAddress.Parse("192.0.2.20"), denied, allowed,
                    FtpDataProtectionLevel.Clear, FtpDataProtectionLevel.Clear));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpThirdPartyTransferCoordinator(
                    IPAddress.Parse("192.0.2.20"), allowed, denied,
                    FtpDataProtectionLevel.Clear, FtpDataProtectionLevel.Clear));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpThirdPartyTransferCoordinator(
                    IPAddress.Parse("192.0.2.20"), allowed, allowed,
                    FtpDataProtectionLevel.Private, FtpDataProtectionLevel.Clear));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpThirdPartyTransferCoordinator(
                    IPAddress.Parse("192.0.2.20"), allowed, allowed,
                    FtpDataProtectionLevel.Clear, FtpDataProtectionLevel.Private));
        });
    }

    [Test]
    public void Invalid_setup_order_endpoints_and_transfer_pairings_are_rejected_and_resettable()
    {
        FtpThirdPartyTransferCoordinator coordinator = CreateCoordinator(
            IPAddress.Parse("192.0.2.20"));

        Assert.Throws<FtpProtocolException>(() =>
            coordinator.BeginSourceExtendedActiveSetup());
        coordinator.BeginTargetExtendedPassiveSetup(FtpAddressFamily.IPv4);
        coordinator.ApplyTargetPassiveSetupReply(
            CreateReply(229, "Entering Extended Passive Mode (|||21|)"));
        Assert.Throws<FtpProtocolException>(() =>
            coordinator.BeginSourceExtendedActiveSetup());

        coordinator.ResetOperation();
        ConfigureReady(coordinator, 50000);
        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                coordinator.BeginPairedTransfer(
                    CreateCommand("STOR wrong-source.bin"),
                    CreateCommand("STOR target.bin")));
            Assert.Throws<FtpProtocolException>(() =>
                coordinator.BeginPairedTransfer(
                    CreateCommand("RETR source.bin"),
                    CreateCommand("RETR wrong-target.bin")));
        });

        coordinator.BeginPairedTransfer(
            CreateCommand("RETR source.bin"), CreateCommand("APPE target.bin"));
        coordinator.ResetOperation();
        FtpThirdPartyTransferState reset = coordinator.GetCurrentState();

        Assert.Multiple(() =>
        {
            Assert.That(reset.TargetPassiveConfigured, Is.False);
            Assert.That(reset.SourceActiveConfigured, Is.False);
            Assert.That(reset.TransferStarted, Is.False);
            Assert.That(reset.SourceCommandResults, Is.Empty);
            Assert.That(reset.TargetCommandResults, Is.Empty);
            Assert.DoesNotThrow(() =>
                coordinator.BeginTargetExtendedPassiveSetup(FtpAddressFamily.IPv4));
        });
    }

    [Test]
    public void PASV_relay_honors_ignore_mismatch_and_prohibited_target_policies()
    {
        FtpDataConnectionOptions source = CreateFxpOptions();
        source.UseExtendedActive = false;
        FtpDataConnectionOptions ignoredTarget = CreateFxpOptions();
        ignoredTarget.UseExtendedPassive = false;
        FtpThirdPartyTransferCoordinator ignored = new FtpThirdPartyTransferCoordinator(
            IPAddress.Parse("192.0.2.20"), source, ignoredTarget,
            FtpDataProtectionLevel.Clear, FtpDataProtectionLevel.Clear);
        ignored.BeginTargetPassiveSetup();
        ignored.ApplyTargetPassiveSetupReply(
            CreateReply(227, "Entering Passive Mode (10,0,0,5,195,80)"));

        FtpDataConnectionOptions strictTarget = CreateFxpOptions();
        strictTarget.UseExtendedPassive = false;
        strictTarget.IgnorePassiveAdvertisedAddress = false;
        FtpThirdPartyTransferCoordinator strict = new FtpThirdPartyTransferCoordinator(
            IPAddress.Parse("192.0.2.20"), source, strictTarget,
            FtpDataProtectionLevel.Clear, FtpDataProtectionLevel.Clear);
        strict.BeginTargetPassiveSetup();

        FtpDataConnectionOptions permissiveTarget = CreateFxpOptions();
        permissiveTarget.UseExtendedPassive = false;
        permissiveTarget.IgnorePassiveAdvertisedAddress = false;
        permissiveTarget.RejectPassiveAddressMismatch = false;
        FtpThirdPartyTransferCoordinator prohibited = new FtpThirdPartyTransferCoordinator(
            IPAddress.Parse("192.0.2.20"), source, permissiveTarget,
            FtpDataProtectionLevel.Clear, FtpDataProtectionLevel.Clear);
        prohibited.BeginTargetPassiveSetup();

        Assert.Multiple(() =>
        {
            Assert.That(ignored.State.PassiveEndpoint!.Host, Is.EqualTo("192.0.2.20"));
            Assert.Throws<FtpConnectionException>(() =>
                strict.ApplyTargetPassiveSetupReply(
                    CreateReply(227, "Entering Passive Mode (10,0,0,5,195,80)")));
            Assert.Throws<FtpConnectionException>(() =>
                prohibited.ApplyTargetPassiveSetupReply(
                    CreateReply(227, "Entering Passive Mode (224,0,0,1,195,80)")));
        });
    }

    [Test]
    public void Session_policies_are_snapshotted_before_the_paired_setup_begins()
    {
        FtpDataConnectionOptions source = CreateFxpOptions();
        FtpDataConnectionOptions target = CreateFxpOptions();
        target.IgnorePassiveAdvertisedAddress = false;
        target.RejectPassiveAddressMismatch = false;
        FtpThirdPartyTransferCoordinator coordinator = new FtpThirdPartyTransferCoordinator(
            IPAddress.Parse("192.0.2.20"), source, target,
            FtpDataProtectionLevel.Clear, FtpDataProtectionLevel.Clear);

        source.UseExtendedActive = false;
        source.AllowActiveFallback = false;
        target.UseExtendedPassive = false;
        target.AllowPassiveFallback = false;
        target.IgnorePassiveAdvertisedAddress = true;
        target.RejectPassiveAddressMismatch = true;

        coordinator.BeginTargetExtendedPassiveSetup(FtpAddressFamily.IPv4);
        FtpCommand fallback = coordinator.BeginTargetPassiveFallback(
            CreateReply(500, "EPSV unavailable"));
        coordinator.ApplyTargetPassiveSetupReply(
            CreateReply(227, "Entering Passive Mode (198,51,100,25,195,80)"));
        FtpCommand active = coordinator.BeginSourceExtendedActiveSetup();

        Assert.Multiple(() =>
        {
            Assert.That(fallback.Kind, Is.EqualTo(FtpCommandKind.Pasv));
            Assert.That(active.Kind, Is.EqualTo(FtpCommandKind.Eprt));
            Assert.That(active.Argument, Is.EqualTo("|1|198.51.100.25|50000|"));
        });
    }

    private static FtpThirdPartyTransferCoordinator CreateReadyCoordinator()
    {
        FtpThirdPartyTransferCoordinator coordinator = CreateCoordinator(
            IPAddress.Parse("192.0.2.20"));
        ConfigureReady(coordinator, 50000);
        return coordinator;
    }

    private static void ConfigureReady(
        FtpThirdPartyTransferCoordinator coordinator,
        int port)
    {
        coordinator.BeginTargetExtendedPassiveSetup(FtpAddressFamily.IPv4);
        coordinator.ApplyTargetPassiveSetupReply(
            CreateReply(229, $"Entering Extended Passive Mode (|||{port}|)"));
        coordinator.BeginSourceExtendedActiveSetup();
        coordinator.ApplySourceActiveSetupReply(
            CreateReply(200, "EPRT command successful"));
    }

    private static FtpThirdPartyTransferCoordinator CreateCoordinator(IPAddress targetPeer)
    {
        return new FtpThirdPartyTransferCoordinator(
            targetPeer, CreateFxpOptions(), CreateFxpOptions(),
            FtpDataProtectionLevel.Clear, FtpDataProtectionLevel.Clear);
    }

    private static FtpDataConnectionOptions CreateFxpOptions()
    {
        FtpDataConnectionOptions options = new FtpDataConnectionOptions();
        options.RequireDataPeerMatch = false;
        options.AllowForeignDataConnections = true;
        return options;
    }

    private static FtpCommand CreateCommand(string text)
    {
        return FtpCommandParser.ParseDecodedCommand(text);
    }

    private static FtpReply CreateReply(int code, string text)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Text = text;
        FtpReplyClassifier.ApplyClassification(reply);
        return reply;
    }
}
