using System.Security.Cryptography;
using System.Text;

namespace ftp.Tests;

public class FtpAuthenticatorTests
{
    [Test]
    public void Anonymous_authenticator_requires_policy_protection_and_password_step()
    {
        FtpFixedUserOptions options = new FtpFixedUserOptions();
        options.AllowAnonymous = true;
        options.AnonymousName = "guest";
        options.RequireProtectedControl = true;
        options.MaximumAttempts = 2;
        options.FailureDelayMilliseconds = 17;
        AnonymousFtpAuthenticator authenticator =
            new AnonymousFtpAuthenticator(options);
        options.AllowAnonymous = false;
        options.AnonymousName = "mutated";

        FtpAuthenticationRequest user = CreateRequest(
            "anonymous", null, null, true, 1);
        user.CertificateIdentity = "certificate-subject";
        FtpAuthenticationResult challenge =
            authenticator.AuthenticateIdentity(user);

        FtpAuthenticationRequest password = CreateRequest(
            "ftp", "mail@example.test", "public", true, 2);
        password.CertificateIdentity = "certificate-subject";
        FtpAuthenticationResult success =
            authenticator.AuthenticateIdentity(password);
        FtpAuthenticationResult clear = authenticator.AuthenticateIdentity(
            CreateRequest("anonymous", "mail@example.test", null, false, 1));
        FtpAuthenticationResult excess = authenticator.AuthenticateIdentity(
            CreateRequest("anonymous", "mail@example.test", null, true, 3));
        FtpAuthenticationResult unrelated = authenticator.AuthenticateIdentity(
            CreateRequest("alice", "secret", null, true, 1));

        Assert.Multiple(() =>
        {
            Assert.That(challenge.Status, Is.EqualTo(
                FtpAuthenticationStatus.AdditionalInformationRequired));
            Assert.That(challenge.Principal, Is.Not.Null);
            Assert.That(challenge.Principal!.Name, Is.EqualTo("guest"));
            Assert.That(challenge.Principal.Anonymous, Is.True);
            Assert.That(challenge.Principal.Authenticated, Is.False);
            Assert.That(challenge.Principal.CertificateIdentity,
                Is.EqualTo("certificate-subject"));
            Assert.That(success.Status,
                Is.EqualTo(FtpAuthenticationStatus.Success));
            Assert.That(success.Principal, Is.Not.Null);
            Assert.That(success.Principal!.Name, Is.EqualTo("guest"));
            Assert.That(success.Principal.Authenticated, Is.True);
            Assert.That(success.Principal.Anonymous, Is.True);
            Assert.That(success.Principal.Account, Is.EqualTo("public"));
            Assert.That(success.Principal.CertificateIdentity,
                Is.EqualTo("certificate-subject"));
            Assert.That(clear.Status, Is.EqualTo(FtpAuthenticationStatus.Failure));
            Assert.That(excess.Status, Is.EqualTo(FtpAuthenticationStatus.Failure));
            Assert.That(clear.FailureMessage, Is.EqualTo(excess.FailureMessage));
            Assert.That(clear.RetryDelayMilliseconds, Is.EqualTo(17));
            Assert.That(excess.RetryDelayMilliseconds, Is.EqualTo(17));
            Assert.That(unrelated.Status,
                Is.EqualTo(FtpAuthenticationStatus.NotHandled));
        });
    }

    [Test]
    public void Fixed_user_authenticator_supports_password_account_and_certificate_data()
    {
        byte[] salt = Encoding.ASCII.GetBytes("unique-salt");
        FtpUserRecord user = new FtpUserRecord();
        user.UserName = "alice";
        user.Salt = salt;
        user.PasswordVerifier = DeriveVerifier(salt, "correct horse");
        user.Roles = new[] { "reader", "writer" };
        user.HomeDirectory = "/users/alice";
        user.Account = "billing";
        user.Disabled = false;
        user.AllowWrite = true;
        user.ResourceLimits.HasStorageQuotaBytes = true;
        user.ResourceLimits.StorageQuotaBytes = 4096;
        user.ResourceLimits.HasTransferByteCeiling = true;
        user.ResourceLimits.TransferByteCeiling = 1024;
        FtpFixedUserOptions options = CreateOptions(user);
        FixedUserFtpAuthenticator authenticator =
            new FixedUserFtpAuthenticator(options);

        user.UserName = "mutated";
        user.Roles[0] = "mutated";
        user.ResourceLimits.StorageQuotaBytes = 1;
        FtpAuthenticationRequest userStep = CreateRequest(
            "alice", null, null, true, 1);
        userStep.CertificateIdentity = "CN=Alice";
        FtpAuthenticationResult passwordChallenge =
            authenticator.AuthenticateIdentity(userStep);
        FtpAuthenticationRequest passwordStep = CreateRequest(
            "alice", "correct horse", null, true, 2);
        passwordStep.CertificateIdentity = "CN=Alice";
        FtpAuthenticationResult accountChallenge =
            authenticator.AuthenticateIdentity(passwordStep);
        FtpAuthenticationRequest accountStep = CreateRequest(
            "alice", null, "billing", true, 3);
        accountStep.Principal = accountChallenge.Principal;
        FtpAuthenticationResult success =
            authenticator.AuthenticateIdentity(accountStep);

        Assert.Multiple(() =>
        {
            Assert.That(passwordChallenge.Status, Is.EqualTo(
                FtpAuthenticationStatus.AdditionalInformationRequired));
            Assert.That(passwordChallenge.Challenge, Does.Contain("Password"));
            Assert.That(accountChallenge.Status, Is.EqualTo(
                FtpAuthenticationStatus.AdditionalInformationRequired));
            Assert.That(accountChallenge.Challenge, Does.Contain("Account"));
            Assert.That(accountChallenge.Principal, Is.Not.Null);
            Assert.That(accountChallenge.Principal!.Authenticated, Is.True);
            Assert.That(success.Status,
                Is.EqualTo(FtpAuthenticationStatus.Success));
            Assert.That(success.Principal, Is.Not.Null);
            Assert.That(success.Principal!.Name, Is.EqualTo("alice"));
            Assert.That(success.Principal.Authenticated, Is.True);
            Assert.That(success.Principal.Anonymous, Is.False);
            Assert.That(success.Principal.Account, Is.EqualTo("billing"));
            Assert.That(success.Principal.CertificateIdentity,
                Is.EqualTo("CN=Alice"));
            Assert.That(success.Principal.Roles,
                Is.EqualTo(new[] { "reader", "writer" }));
            Assert.That(success.Principal.HomeDirectory,
                Is.EqualTo("/users/alice"));
            Assert.That(success.Principal.ResourceLimits.HasStorageQuotaBytes,
                Is.True);
            Assert.That(success.Principal.ResourceLimits.StorageQuotaBytes,
                Is.EqualTo(4096));
            Assert.That(success.Principal.ResourceLimits.TransferByteCeiling,
                Is.EqualTo(1024));
        });
    }

    [Test]
    public void Fixed_user_authenticator_returns_uniform_bounded_failures()
    {
        byte[] salt = Encoding.ASCII.GetBytes("salt");
        FtpUserRecord enabled = new FtpUserRecord();
        enabled.UserName = "alice";
        enabled.Salt = salt;
        enabled.PasswordVerifier = DeriveVerifier(salt, "secret");
        enabled.Account = "account";
        enabled.Disabled = false;
        FtpUserRecord disabled = new FtpUserRecord();
        disabled.UserName = "disabled";
        disabled.Disabled = true;
        FtpFixedUserOptions options = CreateOptions(enabled, disabled);
        options.MaximumAttempts = 2;
        options.FailureDelayMilliseconds = 23;
        FixedUserFtpAuthenticator authenticator =
            new FixedUserFtpAuthenticator(options);

        FtpAuthenticationResult wrongPassword =
            authenticator.AuthenticateIdentity(
                CreateRequest("alice", "wrong", null, true, 1));
        FtpAuthenticationResult unknownUser =
            authenticator.AuthenticateIdentity(
                CreateRequest("missing", "wrong", null, true, 1));
        FtpAuthenticationResult disabledUser =
            authenticator.AuthenticateIdentity(
                CreateRequest("disabled", "wrong", null, true, 1));
        FtpAuthenticationResult wrongAccount =
            authenticator.AuthenticateIdentity(
                CreateRequest("alice", "secret", "wrong", true, 1));
        FtpAuthenticationResult clear =
            authenticator.AuthenticateIdentity(
                CreateRequest("alice", "secret", "account", false, 1));
        FtpAuthenticationResult excess =
            authenticator.AuthenticateIdentity(
                CreateRequest("alice", "secret", "account", true, 3));
        FtpAuthenticationResult malformed =
            authenticator.AuthenticateIdentity(
                CreateRequest("alice\r", "secret", "account", true, 1));
        FtpAuthenticationResult[] failures = new[]
        {
            wrongPassword,
            unknownUser,
            disabledUser,
            wrongAccount,
            clear,
            excess,
            malformed
        };

        Assert.Multiple(() =>
        {
            Assert.That(failures.All(result =>
                result.Status == FtpAuthenticationStatus.Failure), Is.True);
            Assert.That(failures.Select(result => result.FailureMessage).Distinct(),
                Is.EqualTo(new[] { "Authentication failed." }));
            Assert.That(failures.All(result =>
                result.RetryDelayMilliseconds == 23), Is.True);
            Assert.That(failures.All(result => result.Principal == null), Is.True);
        });
    }

    [Test]
    public void Fixed_user_authenticator_can_route_enabled_anonymous_identity()
    {
        FtpFixedUserOptions options = new FtpFixedUserOptions();
        options.AllowAnonymous = true;
        options.AnonymousName = "public";
        options.RequireProtectedControl = false;
        FixedUserFtpAuthenticator authenticator =
            new FixedUserFtpAuthenticator(options);

        FtpAuthenticationResult result = authenticator.AuthenticateIdentity(
            CreateRequest("anonymous", "mail@example.test", null, false, 1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(FtpAuthenticationStatus.Success));
            Assert.That(result.Principal, Is.Not.Null);
            Assert.That(result.Principal!.Name, Is.EqualTo("public"));
            Assert.That(result.Principal.Anonymous, Is.True);
        });
    }

    private static FtpFixedUserOptions CreateOptions(
        params FtpUserRecord[] users)
    {
        FtpFixedUserOptions options = new FtpFixedUserOptions();
        options.Users = users;
        options.RequireProtectedControl = true;
        options.MaximumAttempts = 3;
        options.FailureDelayMilliseconds = 0;
        return options;
    }

    private static FtpAuthenticationRequest CreateRequest(
        string userName,
        string? password,
        string? account,
        bool protectedControl,
        int attemptNumber)
    {
        FtpAuthenticationRequest request = new FtpAuthenticationRequest();
        request.UserName = userName;
        request.Password = password;
        request.Account = account;
        request.ControlProtected = protectedControl;
        request.AttemptNumber = attemptNumber;
        return request;
    }

    private static byte[] DeriveVerifier(byte[] salt, string password)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] saltedPassword = new byte[salt.Length + passwordBytes.Length];
        Buffer.BlockCopy(salt, 0, saltedPassword, 0, salt.Length);
        Buffer.BlockCopy(
            passwordBytes, 0, saltedPassword, salt.Length, passwordBytes.Length);
        return SHA256.HashData(saltedPassword);
    }
}
