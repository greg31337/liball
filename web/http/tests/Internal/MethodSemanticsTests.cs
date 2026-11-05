namespace http.Tests;

public class MethodSemanticsTests
{
    static readonly string[] RegisteredMethods =
    [
        HttpMethods.Acl,
        HttpMethods.BaselineControl,
        HttpMethods.Bind,
        HttpMethods.CheckIn,
        HttpMethods.CheckOut,
        HttpMethods.Connect,
        HttpMethods.Copy,
        HttpMethods.Delete,
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Label,
        HttpMethods.Link,
        HttpMethods.Lock,
        HttpMethods.Merge,
        HttpMethods.MkActivity,
        HttpMethods.MkCalendar,
        HttpMethods.MkCol,
        HttpMethods.MkRedirectRef,
        HttpMethods.MkWorkspace,
        HttpMethods.Move,
        HttpMethods.Options,
        HttpMethods.OrderPatch,
        HttpMethods.Patch,
        HttpMethods.Post,
        HttpMethods.Pri,
        HttpMethods.PropFind,
        HttpMethods.PropPatch,
        HttpMethods.Put,
        HttpMethods.Query,
        HttpMethods.Rebind,
        HttpMethods.Report,
        HttpMethods.Search,
        HttpMethods.Trace,
        HttpMethods.Unbind,
        HttpMethods.Uncheckout,
        HttpMethods.Unlink,
        HttpMethods.Unlock,
        HttpMethods.Update,
        HttpMethods.UpdateRedirectRef,
        HttpMethods.VersionControl,
        HttpMethods.Wildcard
    ];

    static readonly string[] SafeMethods =
    [
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Options,
        HttpMethods.Pri,
        HttpMethods.PropFind,
        HttpMethods.Query,
        HttpMethods.Report,
        HttpMethods.Search,
        HttpMethods.Trace
    ];

    static readonly string[] NonIdempotentMethods =
    [
        HttpMethods.Connect,
        HttpMethods.Lock,
        HttpMethods.Patch,
        HttpMethods.Post,
        HttpMethods.Wildcard
    ];

    [Test]
    public void EverySnapshotMethodHasRegistrySafeAndIdempotentMetadata()
    {
        Assert.That(RegisteredMethods, Has.Length.EqualTo(41));
        Assert.Multiple(() =>
        {
            foreach (string method in RegisteredMethods)
            {
                HttpMethodMetadata metadata = MethodSemantics.Describe(method);
                Assert.That(metadata.Method, Is.EqualTo(method), method);
                Assert.That(metadata.IsRegistered, Is.True, method);
                Assert.That(metadata.IsSafe, Is.EqualTo(SafeMethods.Contains(method)), method);
                Assert.That(
                    metadata.IsIdempotent,
                    Is.EqualTo(!NonIdempotentMethods.Contains(method)),
                    method);
            }
        });
    }

    [Test]
    public void CoreMethodsHaveRfc9110CacheAndRequestContentSemantics()
    {
        string[] core =
        [
            HttpMethods.Get,
            HttpMethods.Head,
            HttpMethods.Post,
            HttpMethods.Put,
            HttpMethods.Delete,
            HttpMethods.Connect,
            HttpMethods.Options,
            HttpMethods.Trace
        ];

        Assert.Multiple(() =>
        {
            foreach (string method in core)
            {
                Assert.That(MethodSemantics.Describe(method).IsCore, Is.True, method);
            }

            Assert.That(MethodSemantics.Describe(HttpMethods.Get).IsCacheable, Is.True);
            Assert.That(MethodSemantics.Describe(HttpMethods.Head).IsCacheable, Is.True);
            Assert.That(MethodSemantics.Describe(HttpMethods.Post).IsCacheable, Is.True);
            Assert.That(MethodSemantics.Describe(HttpMethods.Put).IsCacheable, Is.False);
            Assert.That(
                MethodSemantics.Describe(HttpMethods.Post).HasDefinedRequestContentSemantics,
                Is.True);
            Assert.That(
                MethodSemantics.Describe(HttpMethods.Put).HasDefinedRequestContentSemantics,
                Is.True);
            Assert.That(
                MethodSemantics.Describe(HttpMethods.Delete).HasDefinedRequestContentSemantics,
                Is.False);
            Assert.That(MethodSemantics.Describe(HttpMethods.Patch).IsCore, Is.False);
        });
    }

    [Test]
    public void UnknownValidTokensReceiveConservativeCaseSensitiveMetadata()
    {
        string[] extensions = ["get", "CUSTOM", "X-DO!THING", "123", "#"];

        Assert.Multiple(() =>
        {
            foreach (string method in extensions)
            {
                HttpMethodMetadata metadata = MethodSemantics.Describe(method);
                Assert.That(metadata.Method, Is.EqualTo(method), method);
                Assert.That(metadata.IsRegistered, Is.False, method);
                Assert.That(metadata.IsCore, Is.False, method);
                Assert.That(metadata.IsSafe, Is.False, method);
                Assert.That(metadata.IsIdempotent, Is.False, method);
                Assert.That(metadata.IsCacheable, Is.False, method);
                Assert.That(metadata.HasDefinedRequestContentSemantics, Is.False, method);
            }
        });
    }

    [Test]
    public void InvalidMethodTokensAreRejected()
    {
        string[] invalid = ["", "HAS SPACE", "GET/", "GET\t", "méthode", "GET\r\nHost"];

        Assert.Multiple(() =>
        {
            foreach (string method in invalid)
            {
                HttpProtocolException? exception = Assert.Throws<HttpProtocolException>(() =>
                    MethodSemantics.Describe(method));
                Assert.That(exception!.DiagnosticCode, Is.EqualTo("invalid-method"), method);
            }
        });
    }
}
