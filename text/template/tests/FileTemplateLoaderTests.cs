using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class FileTemplateLoaderTests
{
    [Test]
    public void Load_ReadsTemplateInsideNormalizedRootWithSourceIdentity()
    {
        string root = CreateTempRoot();
        string views = Path.Combine(root, "views");
        Directory.CreateDirectory(views);
        string path = Path.Combine(views, "welcome.tpl");
        File.WriteAllText(path, "Hello {{ name }}");
        FileTemplateLoader loader = new FileTemplateLoader(root + Path.DirectorySeparatorChar.ToString());

        TemplateSource? loaded = loader.Load("views/welcome.tpl");

        Assert.That(loader.GetRootPath(), Is.EqualTo(Path.GetFullPath(root)));
        Assert.That(loader.ContainsTemplate("views/welcome.tpl"), Is.True);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Name, Is.EqualTo("views/welcome.tpl"));
        Assert.That(loaded.Path, Is.EqualTo(Path.GetFullPath(path)));
        Assert.That(loaded.Version, Is.Not.Null);
        Assert.That(loaded.ContentHash, Is.EqualTo(Hash("Hello {{ name }}")));
        Assert.That(loaded.Text, Is.EqualTo("Hello {{ name }}"));
        Assert.That(loaded.Length, Is.EqualTo(16));
        Directory.Delete(root, true);
    }

    [Test]
    public void Configure_UsesSelectedEncodingAndEnforcesMaximumLengthInSyncAndAsyncLoads()
    {
        string root = CreateTempRoot();
        string path = Path.Combine(root, "unicode.tpl");
        File.WriteAllText(path, "héllo", Encoding.Unicode);
        FileTemplateLoader loader = new FileTemplateLoader(root);
        TemplateLoaderOptions options = new TemplateLoaderOptions();
        options.Encoding = Encoding.Unicode;
        options.MaximumTemplateLength = 5;
        loader.Configure(options);

        TemplateSource synchronous = loader.LoadRequired("unicode.tpl");
        TemplateSource asynchronous = loader.LoadWithoutCancellationAsync("unicode.tpl").GetAwaiter().GetResult()!;
        TemplateSource directAsync = loader.LoadAsync("unicode.tpl", CancellationToken.None).GetAwaiter().GetResult()!;

        Assert.That(synchronous.Text, Is.EqualTo("héllo"));
        Assert.That(asynchronous.Text, Is.EqualTo(synchronous.Text));
        Assert.That(asynchronous.Version, Is.EqualTo(synchronous.Version));
        Assert.That(asynchronous.ContentHash, Is.EqualTo(synchronous.ContentHash));
        Assert.That(directAsync.ContentHash, Is.EqualTo(synchronous.ContentHash));
        options.MaximumTemplateLength = 100;
        File.WriteAllText(path, "héllo!", Encoding.Unicode);
        Assert.Throws<InvalidOperationException>(() => loader.Load("unicode.tpl"));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await loader.LoadAsync("unicode.tpl", CancellationToken.None));
        Directory.Delete(root, true);
    }

    [Test]
    public void AsyncOperations_MatchSyncMissingRequiredAndCancellationBehavior()
    {
        string root = CreateTempRoot();
        File.WriteAllText(Path.Combine(root, "present.tpl"), "present");
        FileTemplateLoader loader = new FileTemplateLoader(root);

        Assert.That(loader.LoadWithoutCancellationAsync("missing.tpl").GetAwaiter().GetResult(), Is.Null);
        Assert.ThrowsAsync<FileNotFoundException>(async () => await loader.LoadRequiredAsync("missing.tpl"));
        Assert.That(loader.LoadRequiredWithCancellationAsync("present.tpl", CancellationToken.None).GetAwaiter().GetResult().Text, Is.EqualTo("present"));
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.That(loader.LoadRequiredWithCancellationAsync("present.tpl", cancellation.Token).IsCanceled, Is.True);
        Directory.Delete(root, true);
    }

    [Test]
    public void Configure_RejectsInvalidOptionsAndCopiesConfiguration()
    {
        string root = CreateTempRoot();
        File.WriteAllText(Path.Combine(root, "text.tpl"), "abc");
        FileTemplateLoader loader = new FileTemplateLoader(root);
        TemplateLoaderOptions options = new TemplateLoaderOptions();
        options.MaximumTemplateLength = 3;

        loader.Configure(options);
        options.MaximumTemplateLength = 2;

        Assert.That(loader.LoadRequired("text.tpl").Text, Is.EqualTo("abc"));
        Assert.Throws<ArgumentNullException>(() => loader.Configure(null!));
        TemplateLoaderOptions invalid = new TemplateLoaderOptions();
        invalid.MaximumTemplateLength = 0;
        Assert.Throws<ArgumentOutOfRangeException>(() => loader.Configure(invalid));
        Directory.Delete(root, true);
    }

    [Test]
    public void Load_AllowsInternalDotSegmentsButBlocksTraversalOutsideRoot()
    {
        string root = CreateTempRoot();
        string views = Path.Combine(root, "views");
        Directory.CreateDirectory(views);
        File.WriteAllText(Path.Combine(root, "base.tpl"), "base");
        string outside = Path.Combine(Path.GetDirectoryName(root)!, "outside.tpl");
        File.WriteAllText(outside, "outside");
        FileTemplateLoader loader = new FileTemplateLoader(root);

        TemplateSource loaded = loader.LoadRequired("views/../base.tpl");

        Assert.That(loaded.Text, Is.EqualTo("base"));
        Assert.Throws<InvalidOperationException>(() => loader.Load("../outside.tpl"));
        Assert.Throws<InvalidOperationException>(() => loader.Load(outside));
        File.Delete(outside);
        Directory.Delete(root, true);
    }

    [Test]
    public void LoadMissingAndRequiredUseNullAndFileNotFound()
    {
        string root = CreateTempRoot();
        FileTemplateLoader loader = new FileTemplateLoader(root);

        Assert.That(loader.Load("missing.tpl"), Is.Null);
        Assert.That(loader.ContainsTemplate("missing.tpl"), Is.False);
        Assert.Throws<FileNotFoundException>(() => loader.LoadRequired("missing.tpl"));
        Directory.Delete(root, true);
    }

    [Test]
    public void GetTemplateNames_ReturnsRelativeNamesForNestedFiles()
    {
        string root = CreateTempRoot();
        Directory.CreateDirectory(Path.Combine(root, "layouts"));
        File.WriteAllText(Path.Combine(root, "index.tpl"), "index");
        File.WriteAllText(Path.Combine(root, "layouts", "main.tpl"), "main");
        FileTemplateLoader loader = new FileTemplateLoader(root);

        List<string> names = loader.GetTemplateNames();

        Assert.That(names.Count, Is.EqualTo(2));
        Assert.That(names, Does.Contain("index.tpl"));
        Assert.That(names, Does.Contain("layouts/main.tpl"));
        Directory.Delete(root, true);
    }

    [Test]
    public void MissingRootListsNoTemplates()
    {
        string root = Path.Combine(Path.GetTempPath(), "template-file-loader-missing-" + Guid.NewGuid().ToString("N"));
        FileTemplateLoader loader = new FileTemplateLoader(root);

        Assert.That(loader.GetTemplateNames().Count, Is.EqualTo(0));
    }

    [Test]
    public void ConstructorsAndLoadMethods_RejectInvalidInputs()
    {
        string root = CreateTempRoot();
        FileTemplateLoader loader = new FileTemplateLoader(root);

        Assert.Throws<ArgumentNullException>(() => new FileTemplateLoader(null!));
        Assert.Throws<ArgumentException>(() => new FileTemplateLoader(""));
        Assert.Throws<ArgumentNullException>(() => loader.Load(null!));
        Assert.Throws<ArgumentException>(() => loader.Load(""));
        Assert.Throws<ArgumentNullException>(() => loader.ContainsTemplate(null!));
        Assert.Throws<ArgumentNullException>(() => loader.GetTemplatePath(null!));
        Directory.Delete(root, true);
    }

    static string CreateTempRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), "template-file-loader-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    static string Hash(string text)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }
}
