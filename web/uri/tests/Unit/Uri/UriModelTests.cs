namespace uri.Tests;

public class UriModelTests
{
    [Test]
    public void NewAuthorityStartsWithEveryOptionalSubcomponentAbsent()
    {
        UriAuthority authority = new UriAuthority();

        Assert.Multiple(() =>
        {
            Assert.That(authority.HasUserInfo, Is.False);
            Assert.That(authority.UserInfo, Is.Empty);
            Assert.That(authority.HostKind, Is.EqualTo(UriHostKind.Absent));
            Assert.That(authority.Host, Is.Empty);
            Assert.That(authority.AddressBytes, Is.Empty);
            Assert.That(authority.HasPort, Is.False);
            Assert.That(authority.Port, Is.Empty);
        });
    }

    [Test]
    public void AuthorityPreservesPresentEmptyUserInfoHostAndPortStates()
    {
        UriAuthority authority = new UriAuthority();
        authority.HasUserInfo = true;
        authority.UserInfo = "";
        authority.HostKind = UriHostKind.RegisteredName;
        authority.Host = "";
        authority.HasPort = true;
        authority.Port = "";

        Assert.Multiple(() =>
        {
            Assert.That(authority.HasUserInfo, Is.True);
            Assert.That(authority.UserInfo, Is.Empty);
            Assert.That(authority.HostKind, Is.EqualTo(UriHostKind.RegisteredName));
            Assert.That(authority.Host, Is.Empty);
            Assert.That(authority.HasPort, Is.True);
            Assert.That(authority.Port, Is.Empty);
        });
    }

    [Test]
    public void AuthorityRetainsOriginalHostSpellingAndParsedAddressBytes()
    {
        UriAuthority authority = new UriAuthority();
        authority.HostKind = UriHostKind.Ipv6;
        authority.Host = "2001:0DB8:0:0:0:0:0:1";
        authority.AddressBytes = new byte[]
        {
            32, 1, 13, 184, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 1
        };

        Assert.Multiple(() =>
        {
            Assert.That(authority.Host, Is.EqualTo("2001:0DB8:0:0:0:0:0:1"));
            Assert.That(authority.AddressBytes, Has.Length.EqualTo(16));
            Assert.That(authority.AddressBytes[0], Is.EqualTo(32));
            Assert.That(authority.AddressBytes[15], Is.EqualTo(1));
        });
    }

    [Test]
    public void NewReferenceRepresentsTheEmptyReference()
    {
        UriReference reference = new UriReference();

        Assert.Multiple(() =>
        {
            Assert.That(reference.Kind, Is.EqualTo(UriReferenceKind.EmptyReference));
            Assert.That(reference.HasScheme, Is.False);
            Assert.That(reference.Scheme, Is.Empty);
            Assert.That(reference.HasAuthority, Is.False);
            Assert.That(reference.Authority, Is.Not.Null);
            Assert.That(reference.Authority.HostKind, Is.EqualTo(UriHostKind.Absent));
            Assert.That(reference.Path, Is.Empty);
            Assert.That(reference.HasQuery, Is.False);
            Assert.That(reference.Query, Is.Empty);
            Assert.That(reference.HasFragment, Is.False);
            Assert.That(reference.Fragment, Is.Empty);
            Assert.That(reference.OriginalText, Is.Empty);
        });
    }

    [Test]
    public void ReferencePreservesPresentEmptyAuthorityQueryAndFragment()
    {
        UriReference reference = new UriReference();
        reference.Kind = UriReferenceKind.NetworkPathReference;
        reference.HasAuthority = true;
        reference.Authority.HostKind = UriHostKind.RegisteredName;
        reference.Authority.Host = "";
        reference.HasQuery = true;
        reference.Query = "";
        reference.HasFragment = true;
        reference.Fragment = "";
        reference.OriginalText = "//?#";

        Assert.Multiple(() =>
        {
            Assert.That(reference.HasAuthority, Is.True);
            Assert.That(reference.Authority.Host, Is.Empty);
            Assert.That(reference.HasQuery, Is.True);
            Assert.That(reference.Query, Is.Empty);
            Assert.That(reference.HasFragment, Is.True);
            Assert.That(reference.Fragment, Is.Empty);
            Assert.That(reference.OriginalText, Is.EqualTo("//?#"));
        });
    }

    [Test]
    public void ReferenceStoresLosslessComponentTextAndClassification()
    {
        UriReference reference = new UriReference();
        reference.Kind = UriReferenceKind.Uri;
        reference.HasScheme = true;
        reference.Scheme = "HTTP";
        reference.HasAuthority = true;
        reference.Authority.HostKind = UriHostKind.RegisteredName;
        reference.Authority.Host = "Example.COM";
        reference.Path = "/a//b/";
        reference.HasQuery = true;
        reference.Query = "x=%2f";
        reference.HasFragment = true;
        reference.Fragment = "Part";
        reference.OriginalText = "HTTP://Example.COM/a//b/?x=%2f#Part";

        Assert.Multiple(() =>
        {
            Assert.That(reference.Kind, Is.EqualTo(UriReferenceKind.Uri));
            Assert.That(reference.Scheme, Is.EqualTo("HTTP"));
            Assert.That(reference.Authority.Host, Is.EqualTo("Example.COM"));
            Assert.That(reference.Path, Is.EqualTo("/a//b/"));
            Assert.That(reference.Query, Is.EqualTo("x=%2f"));
            Assert.That(reference.Fragment, Is.EqualTo("Part"));
            Assert.That(reference.OriginalText, Is.EqualTo("HTTP://Example.COM/a//b/?x=%2f#Part"));
        });
    }

    [Test]
    public void ReferencesOwnIndependentAuthorityAndAddressStorage()
    {
        UriReference first = new UriReference();
        UriReference second = new UriReference();
        first.Authority.Host = "first.example";
        first.Authority.AddressBytes = new byte[] { 127, 0, 0, 1 };

        Assert.Multiple(() =>
        {
            Assert.That(second.Authority.Host, Is.Empty);
            Assert.That(second.Authority.AddressBytes, Is.Empty);
            Assert.That(second.Authority, Is.Not.SameAs(first.Authority));
            Assert.That(second.Authority.AddressBytes, Is.Not.SameAs(first.Authority.AddressBytes));
        });
    }
}
