namespace ftp.Tests;

public class PublicClientContractTests
{
    [Test]
    public void Client_contract_exposes_the_complete_unique_operation_inventory()
    {
        string[] expected =
        {
            nameof(IFtpClient.Connect),
            nameof(IFtpClient.ProbeConnectionProfiles),
            nameof(IFtpClient.Reconnect),
            nameof(IFtpClient.Disconnect),
            nameof(IFtpClient.AbortConnection),
            nameof(IFtpClient.QuitSession),
            nameof(IFtpClient.ReinitializeSession),
            nameof(IFtpClient.GetSessionState),
            nameof(IFtpClient.AuthenticateUser),
            nameof(IFtpClient.UpgradeControlTls),
            nameof(IFtpClient.SetDataProtection),
            nameof(IFtpClient.ClearControlTls),
            nameof(IFtpClient.NegotiateFeatures),
            nameof(IFtpClient.RefreshFeatures),
            nameof(IFtpClient.ApplyOption),
            nameof(IFtpClient.ExecuteCommand),
            nameof(IFtpClient.ExecuteCommandBatch),
            nameof(IFtpClient.ExecuteSite),
            nameof(IFtpClient.RequestHelp),
            nameof(IFtpClient.RequestStatus),
            nameof(IFtpClient.RequestSystemType),
            nameof(IFtpClient.SendNoOperation),
            nameof(IFtpClient.SetTransferRepresentation),
            nameof(IFtpClient.SetFileStructure),
            nameof(IFtpClient.SetTransferMode),
            nameof(IFtpClient.AllocateStorage),
            nameof(IFtpClient.ChangeWorkingDirectory),
            nameof(IFtpClient.ChangeToParentDirectory),
            nameof(IFtpClient.MountFileStructure),
            nameof(IFtpClient.GetWorkingDirectory),
            nameof(IFtpClient.GetMetadata),
            nameof(IFtpClient.GetFileSize),
            nameof(IFtpClient.GetModificationTime),
            nameof(IFtpClient.ProbePath),
            nameof(IFtpClient.ListDirectory),
            nameof(IFtpClient.ListMachineDirectory),
            nameof(IFtpClient.ListHumanDirectory),
            nameof(IFtpClient.ListNames),
            nameof(IFtpClient.ListStatus),
            nameof(IFtpClient.CreateDirectory),
            nameof(IFtpClient.CreateParentDirectories),
            nameof(IFtpClient.RemoveDirectory),
            nameof(IFtpClient.DeleteFile),
            nameof(IFtpClient.RenamePath),
            nameof(IFtpClient.DeleteDirectoryTree),
            nameof(IFtpClient.SetModificationTime),
            nameof(IFtpClient.SetPermissions),
            nameof(IFtpClient.SetUmask),
            nameof(IFtpClient.Transfer),
            nameof(IFtpClient.AbortTransfer),
            nameof(IFtpClient.VerifyTransfer),
            nameof(IFtpClient.GetLatestRestartMarker),
            nameof(IFtpClient.PlanDirectoryTransfer),
            nameof(IFtpClient.TransferDirectory),
            nameof(IFtpClient.TransferServerToServer),
        };
        System.Reflection.MethodInfo[] methods = typeof(IFtpClient).GetMethods();
        string[] actual = methods.Select(method => method.Name).OrderBy(name => name).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected.OrderBy(name => name).ToArray()));
            Assert.That(actual.Distinct().Count(), Is.EqualTo(actual.Length));
            Assert.That(actual.Length, Is.EqualTo(55));
        });
    }

    [Test]
    public void Client_contract_uses_request_and_result_models_for_complex_operations()
    {
        System.Reflection.MethodInfo[] methods = typeof(IFtpClient).GetMethods();
        System.Reflection.MethodInfo probe = methods.Single(method => method.Name == nameof(IFtpClient.ProbeConnectionProfiles));
        System.Reflection.MethodInfo command = methods.Single(method => method.Name == nameof(IFtpClient.ExecuteCommand));
        System.Reflection.MethodInfo transfer = methods.Single(method => method.Name == nameof(IFtpClient.Transfer));
        System.Reflection.MethodInfo directory = methods.Single(method => method.Name == nameof(IFtpClient.TransferDirectory));
        System.Reflection.MethodInfo fxp = methods.Single(method => method.Name == nameof(IFtpClient.TransferServerToServer));

        Assert.Multiple(() =>
        {
            Assert.That(probe.ReturnType, Is.EqualTo(typeof(FtpConnectionProbeResult)));
            Assert.That(probe.GetParameters().Single().ParameterType, Is.EqualTo(typeof(FtpClientOptions)));
            Assert.That(command.ReturnType, Is.EqualTo(typeof(FtpCommandResult)));
            Assert.That(command.GetParameters().Single().ParameterType, Is.EqualTo(typeof(FtpCommand)));
            Assert.That(transfer.ReturnType, Is.EqualTo(typeof(FtpTransferResult)));
            Assert.That(transfer.GetParameters().Single().ParameterType, Is.EqualTo(typeof(FtpTransferRequest)));
            Assert.That(directory.ReturnType, Is.EqualTo(typeof(FtpDirectoryTransferResult)));
            Assert.That(directory.GetParameters().Single().ParameterType, Is.EqualTo(typeof(FtpDirectoryTransferRequest)));
            Assert.That(fxp.ReturnType, Is.EqualTo(typeof(FtpServerTransferResult)));
            Assert.That(fxp.GetParameters().Single().ParameterType, Is.EqualTo(typeof(FtpServerTransferRequest)));
        });
    }

    [Test]
    public void Connection_profile_is_non_viable_and_credential_free_by_default()
    {
        FtpConnectionProfile first = new FtpConnectionProfile();
        FtpConnectionProfile second = new FtpConnectionProfile();

        Assert.Multiple(() =>
        {
            Assert.That(first.Endpoint, Is.Not.SameAs(second.Endpoint));
            Assert.That(first.SecurityMode, Is.EqualTo(FtpSecurityMode.ExplicitTlsRequired));
            Assert.That(first.Viable, Is.False);
            Assert.That(first.ControlProtected, Is.False);
            Assert.That(first.Greeting, Is.Null);
            Assert.That(first.Features, Is.Not.SameAs(second.Features));
            Assert.That(first.FailureMessage, Is.Null);
        });
    }

    [Test]
    public void Connection_probe_result_owns_an_empty_incomplete_profile_collection()
    {
        FtpConnectionProbeResult first = new FtpConnectionProbeResult();
        FtpConnectionProbeResult second = new FtpConnectionProbeResult();

        Assert.Multiple(() =>
        {
            Assert.That(first.Profiles, Is.Empty);
            Assert.That(first.Profiles, Is.Not.SameAs(second.Profiles));
            Assert.That(first.Complete, Is.False);
            Assert.That(first.FailureMessage, Is.Null);
        });
    }
}
