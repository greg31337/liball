namespace ftp;

internal class FtpServerCommandContext
{
    internal FtpServerSession Session;
    internal FtpServerSessionContext Connection;
    internal FtpCommand Command;
    internal FtpSessionStateMachine State;
    internal FtpVirtualHostOptions? Host;
    internal IFtpFileSystem? FileSystem;
    internal IFtpAuthenticator? Authenticator;
    internal IFtpAuthorizer? Authorizer;
    internal FtpAuthorizationGate AuthorizationGate;
    internal IFtpCertificateProvider? CertificateProvider;
    internal FtpFeatureSet Features;
    internal string RootPath;

    internal FtpServerCommandContext(
        FtpServerSession session,
        FtpCommand command)
    {
        Session = session;
        Connection = session.Context;
        Command = command;
        State = session.State;
        Host = session.ActiveHost;
        FileSystem = session.ActiveFileSystem;
        Authenticator = session.ActiveAuthenticator;
        Authorizer = session.ActiveAuthorizer;
        AuthorizationGate = new FtpAuthorizationGate(
            session.ActiveAuthorizer, session.ActiveHost, session.EventSink);
        CertificateProvider = session.ActiveCertificateProvider;
        Features = session.ActiveFeatures;
        RootPath = session.ActiveRootPath;
    }
}
