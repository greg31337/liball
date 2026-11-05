namespace pop3.Tests;

public class Pop3EnumTests
{
    [Test]
    public void SessionStateValuesAreStable()
    {
        Assert.That(Enum.GetValues<Pop3SessionState>(), Is.EqualTo(new[]
        {
            Pop3SessionState.Disconnected,
            Pop3SessionState.Authorization,
            Pop3SessionState.Transaction,
            Pop3SessionState.Update,
            Pop3SessionState.Closed
        }));
        Assert.That((int)Pop3SessionState.Disconnected, Is.EqualTo(0));
        Assert.That((int)Pop3SessionState.Authorization, Is.EqualTo(1));
        Assert.That((int)Pop3SessionState.Transaction, Is.EqualTo(2));
        Assert.That((int)Pop3SessionState.Update, Is.EqualTo(3));
        Assert.That((int)Pop3SessionState.Closed, Is.EqualTo(4));
    }

    [Test]
    public void ResponseKindValuesAreStable()
    {
        Assert.That(Enum.GetValues<Pop3ResponseKind>(), Is.EqualTo(new[]
        {
            Pop3ResponseKind.Positive,
            Pop3ResponseKind.Negative,
            Pop3ResponseKind.SaslContinuation
        }));
        Assert.That((int)Pop3ResponseKind.Positive, Is.EqualTo(0));
        Assert.That((int)Pop3ResponseKind.Negative, Is.EqualTo(1));
        Assert.That((int)Pop3ResponseKind.SaslContinuation, Is.EqualTo(2));
    }

    [Test]
    public void SecurityModeValuesAreStable()
    {
        Assert.That(Enum.GetValues<Pop3SecurityMode>(), Is.EqualTo(new[]
        {
            Pop3SecurityMode.Plaintext,
            Pop3SecurityMode.StlsIfAvailable,
            Pop3SecurityMode.StlsRequired,
            Pop3SecurityMode.ImplicitTls
        }));
        Assert.That((int)Pop3SecurityMode.Plaintext, Is.EqualTo(0));
        Assert.That((int)Pop3SecurityMode.StlsIfAvailable, Is.EqualTo(1));
        Assert.That((int)Pop3SecurityMode.StlsRequired, Is.EqualTo(2));
        Assert.That((int)Pop3SecurityMode.ImplicitTls, Is.EqualTo(3));
    }

    [Test]
    public void AuthenticationMethodValuesAreStable()
    {
        Assert.That(Enum.GetValues<Pop3AuthenticationMethod>(), Is.EqualTo(new[]
        {
            Pop3AuthenticationMethod.Automatic,
            Pop3AuthenticationMethod.UserPassword,
            Pop3AuthenticationMethod.Apop,
            Pop3AuthenticationMethod.Sasl,
            Pop3AuthenticationMethod.SaslPlain
        }));
        Assert.That((int)Pop3AuthenticationMethod.Automatic, Is.EqualTo(0));
        Assert.That((int)Pop3AuthenticationMethod.UserPassword, Is.EqualTo(1));
        Assert.That((int)Pop3AuthenticationMethod.Apop, Is.EqualTo(2));
        Assert.That((int)Pop3AuthenticationMethod.Sasl, Is.EqualTo(3));
        Assert.That((int)Pop3AuthenticationMethod.SaslPlain, Is.EqualTo(4));
    }

    [Test]
    public void AuthenticationStatusValuesAreStable()
    {
        Assert.That(Enum.GetValues<Pop3AuthenticationStatus>(), Is.EqualTo(new[]
        {
            Pop3AuthenticationStatus.Success,
            Pop3AuthenticationStatus.Rejected,
            Pop3AuthenticationStatus.LoginDelay,
            Pop3AuthenticationStatus.InUse,
            Pop3AuthenticationStatus.TemporaryFailure,
            Pop3AuthenticationStatus.PermanentFailure
        }));
        Assert.That((int)Pop3AuthenticationStatus.Success, Is.EqualTo(0));
        Assert.That((int)Pop3AuthenticationStatus.Rejected, Is.EqualTo(1));
        Assert.That((int)Pop3AuthenticationStatus.LoginDelay, Is.EqualTo(2));
        Assert.That((int)Pop3AuthenticationStatus.InUse, Is.EqualTo(3));
        Assert.That((int)Pop3AuthenticationStatus.TemporaryFailure, Is.EqualTo(4));
        Assert.That((int)Pop3AuthenticationStatus.PermanentFailure, Is.EqualTo(5));
    }

    [Test]
    public void SaslStepStatusValuesAreStable()
    {
        Assert.That(Enum.GetValues<Pop3SaslStepStatus>(), Is.EqualTo(new[]
        {
            Pop3SaslStepStatus.Continue,
            Pop3SaslStepStatus.Success,
            Pop3SaslStepStatus.Rejected,
            Pop3SaslStepStatus.Cancelled
        }));
        Assert.That((int)Pop3SaslStepStatus.Continue, Is.EqualTo(0));
        Assert.That((int)Pop3SaslStepStatus.Success, Is.EqualTo(1));
        Assert.That((int)Pop3SaslStepStatus.Rejected, Is.EqualTo(2));
        Assert.That((int)Pop3SaslStepStatus.Cancelled, Is.EqualTo(3));
    }

    [Test]
    public void TransferKindValuesAreStable()
    {
        Assert.That(Enum.GetValues<Pop3TransferKind>(), Is.EqualTo(new[]
        {
            Pop3TransferKind.List,
            Pop3TransferKind.Uidl,
            Pop3TransferKind.Retrieve,
            Pop3TransferKind.Top,
            Pop3TransferKind.ExtensionData
        }));
        Assert.That((int)Pop3TransferKind.List, Is.EqualTo(0));
        Assert.That((int)Pop3TransferKind.Uidl, Is.EqualTo(1));
        Assert.That((int)Pop3TransferKind.Retrieve, Is.EqualTo(2));
        Assert.That((int)Pop3TransferKind.Top, Is.EqualTo(3));
        Assert.That((int)Pop3TransferKind.ExtensionData, Is.EqualTo(4));
    }

    [Test]
    public void LogDirectionValuesAreStable()
    {
        Assert.That(Enum.GetValues<Pop3LogDirection>(), Is.EqualTo(new[]
        {
            Pop3LogDirection.ClientToServer,
            Pop3LogDirection.ServerToClient,
            Pop3LogDirection.LocalState
        }));
        Assert.That((int)Pop3LogDirection.ClientToServer, Is.EqualTo(0));
        Assert.That((int)Pop3LogDirection.ServerToClient, Is.EqualTo(1));
        Assert.That((int)Pop3LogDirection.LocalState, Is.EqualTo(2));
    }
}
