using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class EmbeddedResourceTemplateLoaderTests
{
    [Test]
    public void LoadOperations_ReturnStableResourceSourceMetadata()
    {
        EmbeddedResourceTemplateLoader loader = CreateLoader();

        TemplateSource synchronous = loader.LoadRequired("card.tpl");
        TemplateSource asynchronous = loader.LoadWithoutCancellationAsync("card.tpl").GetAwaiter().GetResult()!;
        TemplateSource requiredAsync = loader.LoadRequiredAsync("card.tpl").GetAwaiter().GetResult();
        TemplateSource directAsync = loader.LoadAsync("card.tpl", CancellationToken.None).GetAwaiter().GetResult()!;
        TemplateSource requiredWithCancellation = loader.LoadRequiredWithCancellationAsync("card.tpl", CancellationToken.None).GetAwaiter().GetResult();

        Assert.That(loader.ContainsTemplate("card.tpl"), Is.True);
        Assert.That(loader.ContainsTemplate("missing.tpl"), Is.False);
        Assert.That(synchronous.Name, Is.EqualTo("card.tpl"));
        Assert.That(synchronous.Path, Is.EqualTo("TemplateTests.Resources.card.tpl"));
        Assert.That(synchronous.Text, Is.EqualTo("Embedded {{ name }}\n"));
        Assert.That(synchronous.Version, Is.Not.Empty);
        Assert.That(synchronous.ContentHash, Is.EqualTo(Hash(synchronous.Text)));
        Assert.That(asynchronous.Version, Is.EqualTo(synchronous.Version));
        Assert.That(asynchronous.ContentHash, Is.EqualTo(synchronous.ContentHash));
        Assert.That(requiredAsync.Text, Is.EqualTo(synchronous.Text));
        Assert.That(directAsync.ContentHash, Is.EqualTo(synchronous.ContentHash));
        Assert.That(requiredWithCancellation.Text, Is.EqualTo(synchronous.Text));
        Assert.That(loader.GetTemplateNames(), Does.Contain("card.tpl"));
    }

    [Test]
    public void MissingRequiredInvalidAndCanceledOperations_ReportConsistently()
    {
        EmbeddedResourceTemplateLoader loader = CreateLoader();

        Assert.That(loader.Load("missing.tpl"), Is.Null);
        Assert.Throws<FileNotFoundException>(() => loader.LoadRequired("missing.tpl"));
        Assert.ThrowsAsync<FileNotFoundException>(async () => await loader.LoadRequiredAsync("missing.tpl"));
        Assert.Throws<ArgumentNullException>(() => loader.Load(null!));
        Assert.Throws<ArgumentException>(() => loader.Load(""));
        Assert.Throws<InvalidOperationException>(() => loader.Load("../card.tpl"));
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.That(loader.LoadAsync("card.tpl", cancellation.Token).IsCanceled, Is.True);
        Assert.That(loader.LoadRequiredWithCancellationAsync("card.tpl", cancellation.Token).IsCanceled, Is.True);
    }

    [Test]
    public void Configure_CopiesOptionsAndEnforcesMaximumLength()
    {
        EmbeddedResourceTemplateLoader loader = CreateLoader();
        TemplateLoaderOptions options = new TemplateLoaderOptions();
        options.Encoding = new UTF8Encoding(false, true);
        options.ResourcePrefix = "TemplateTests.Resources.";
        options.MaximumTemplateLength = 20;
        loader.Configure(options);
        options.MaximumTemplateLength = 1;

        Assert.That(loader.LoadRequired("card.tpl").Length, Is.EqualTo(20));
        TemplateLoaderOptions limited = new TemplateLoaderOptions();
        limited.ResourcePrefix = "TemplateTests.Resources.";
        limited.MaximumTemplateLength = 19;
        loader.Configure(limited);
        Assert.Throws<InvalidOperationException>(() => loader.Load("card.tpl"));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await loader.LoadAsync("card.tpl", CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => loader.Configure(null!));
    }

    [Test]
    public void ConstructorAndOptions_RejectInvalidValues()
    {
        Assert.Throws<ArgumentNullException>(() => new EmbeddedResourceTemplateLoader(null!));
        TemplateLoaderOptions options = new TemplateLoaderOptions();
        options.Encoding = null!;
        Assert.Throws<ArgumentNullException>(() => CreateLoader().Configure(options));
        options = new TemplateLoaderOptions();
        options.ResourcePrefix = null!;
        Assert.Throws<ArgumentNullException>(() => CreateLoader().Configure(options));
    }

    static EmbeddedResourceTemplateLoader CreateLoader()
    {
        EmbeddedResourceTemplateLoader loader = new EmbeddedResourceTemplateLoader(Assembly.GetExecutingAssembly());
        TemplateLoaderOptions options = new TemplateLoaderOptions();
        options.ResourcePrefix = "TemplateTests.Resources.";
        loader.Configure(options);
        return loader;
    }

    static string Hash(string text)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }
}
