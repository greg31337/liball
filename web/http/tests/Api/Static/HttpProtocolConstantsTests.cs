namespace http.Tests;

public class HttpProtocolConstantsTests
{
    [Test]
    public void HeaderValueTokensAreCanonical()
    {
        Assert.That(HttpHeaderValues.Close, Is.EqualTo("close"));
        Assert.That(HttpHeaderValues.KeepAlive, Is.EqualTo("keep-alive"));
        Assert.That(HttpHeaderValues.Upgrade, Is.EqualTo("upgrade"));
        Assert.That(HttpHeaderValues.Trailers, Is.EqualTo("trailers"));
        Assert.That(HttpHeaderValues.Bytes, Is.EqualTo("bytes"));
        Assert.That(HttpHeaderValues.Chunked, Is.EqualTo("chunked"));
        Assert.That(HttpHeaderValues.Identity, Is.EqualTo("identity"));
    }

    [Test]
    public void CacheDirectiveTokensAreCanonical()
    {
        Assert.That(HttpHeaderValues.MaxAge, Is.EqualTo("max-age"));
        Assert.That(HttpHeaderValues.SMaxAge, Is.EqualTo("s-maxage"));
        Assert.That(HttpHeaderValues.MaxStale, Is.EqualTo("max-stale"));
        Assert.That(HttpHeaderValues.MinFresh, Is.EqualTo("min-fresh"));
        Assert.That(HttpHeaderValues.NoCache, Is.EqualTo("no-cache"));
        Assert.That(HttpHeaderValues.NoStore, Is.EqualTo("no-store"));
        Assert.That(HttpHeaderValues.NoTransform, Is.EqualTo("no-transform"));
        Assert.That(HttpHeaderValues.OnlyIfCached, Is.EqualTo("only-if-cached"));
        Assert.That(HttpHeaderValues.MustRevalidate, Is.EqualTo("must-revalidate"));
        Assert.That(HttpHeaderValues.ProxyRevalidate, Is.EqualTo("proxy-revalidate"));
        Assert.That(HttpHeaderValues.MustUnderstand, Is.EqualTo("must-understand"));
        Assert.That(HttpHeaderValues.Private, Is.EqualTo("private"));
        Assert.That(HttpHeaderValues.Public, Is.EqualTo("public"));
        Assert.That(HttpHeaderValues.Immutable, Is.EqualTo("immutable"));
        Assert.That(HttpHeaderValues.StaleIfError, Is.EqualTo("stale-if-error"));
        Assert.That(HttpHeaderValues.StaleWhileRevalidate, Is.EqualTo("stale-while-revalidate"));
    }

    [Test]
    public void ProtocolNamesUseWireRepresentations()
    {
        Assert.That(HttpProtocolNames.Http10, Is.EqualTo("HTTP/1.0"));
        Assert.That(HttpProtocolNames.Http11, Is.EqualTo("HTTP/1.1"));
        Assert.That(HttpProtocolNames.Http2, Is.EqualTo("HTTP/2"));
        Assert.That(HttpProtocolNames.H2, Is.EqualTo("h2"));
        Assert.That(HttpProtocolNames.H2c, Is.EqualTo("h2c"));
        Assert.That(HttpProtocolNames.Http, Is.EqualTo("http"));
        Assert.That(HttpProtocolNames.Https, Is.EqualTo("https"));
        Assert.That(HttpProtocolNames.WebSocket, Is.EqualTo("websocket"));
    }
}
