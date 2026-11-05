using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class MemoryTemplateLoaderTests
{
    [Test]
    public void SetTemplate_LoadsNamedTemplateSourceWithGeneratedVersion()
    {
        MemoryTemplateLoader loader = new MemoryTemplateLoader();

        loader.SetTemplate("welcome", "Hello {{ name }}");
        TemplateSource? loaded = loader.Load("welcome");

        Assert.That(loader.ContainsTemplate("welcome"), Is.True);
        Assert.That(loader.ContainsTemplate("missing"), Is.False);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Name, Is.EqualTo("welcome"));
        Assert.That(loaded.Path, Is.Null);
        Assert.That(loaded.Version, Is.EqualTo("1"));
        Assert.That(loaded.ContentHash, Is.EqualTo(Hash("Hello {{ name }}")));
        Assert.That(loaded.Text, Is.EqualTo("Hello {{ name }}"));
        Assert.That(loaded.Length, Is.EqualTo(16));
    }

    [Test]
    public void Configure_EnforcesMaximumLengthAndCopiesOptions()
    {
        MemoryTemplateLoader loader = new MemoryTemplateLoader();
        TemplateLoaderOptions options = new TemplateLoaderOptions();
        options.MaximumTemplateLength = 3;
        loader.Configure(options);
        options.MaximumTemplateLength = 100;

        loader.SetTemplate("short", "abc");

        Assert.Throws<InvalidOperationException>(() => loader.SetTemplate("long", "abcd"));
        TemplateLoaderOptions tooSmall = new TemplateLoaderOptions();
        tooSmall.MaximumTemplateLength = 2;
        Assert.Throws<InvalidOperationException>(() => loader.Configure(tooSmall));
        Assert.That(loader.LoadRequired("short").Text, Is.EqualTo("abc"));
        Assert.Throws<ArgumentNullException>(() => loader.Configure(null!));
    }

    [Test]
    public void AsyncOperations_MatchSyncMissingRequiredAndCancellationBehavior()
    {
        MemoryTemplateLoader loader = new MemoryTemplateLoader();
        loader.AddTemplateSource("present", new TemplateSource("source", null, null, null, "text"));

        TemplateSource loaded = loader.LoadRequiredWithCancellationAsync("present", CancellationToken.None).GetAwaiter().GetResult();
        TemplateSource directAsync = loader.LoadAsync("present", CancellationToken.None).GetAwaiter().GetResult()!;

        Assert.That(loaded.Version, Is.Not.Null);
        Assert.That(loaded.ContentHash, Is.EqualTo(Hash("text")));
        Assert.That(directAsync.ContentHash, Is.EqualTo(loaded.ContentHash));
        Assert.That(loader.LoadWithoutCancellationAsync("missing").GetAwaiter().GetResult(), Is.Null);
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await loader.LoadRequiredAsync("missing"));
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.That(loader.LoadRequiredWithCancellationAsync("present", cancellation.Token).IsCanceled, Is.True);
    }

    [Test]
    public void SetTemplate_ReplacesExistingTemplateAndAdvancesVersion()
    {
        MemoryTemplateLoader loader = new MemoryTemplateLoader();

        loader.SetTemplate("welcome", "one");
        TemplateSource first = loader.LoadRequired("welcome");
        loader.SetTemplate("welcome", "two");
        TemplateSource second = loader.LoadRequired("welcome");

        Assert.That(first.Text, Is.EqualTo("one"));
        Assert.That(first.Version, Is.EqualTo("1"));
        Assert.That(second.Text, Is.EqualTo("two"));
        Assert.That(second.Version, Is.EqualTo("2"));
    }

    [Test]
    public void SetTemplate_AdvancesVersionPastMaximumInteger()
    {
        MemoryTemplateLoader loader = new MemoryTemplateLoader();
        loader.VersionCounter = 2147483647;

        loader.SetTemplate("welcome", "hello");

        Assert.That(loader.LoadRequired("welcome").Version, Is.EqualTo("2147483648"));
    }

    [Test]
    public void SetTemplate_CopiesProvidedSourceWithRequestedName()
    {
        MemoryTemplateLoader loader = new MemoryTemplateLoader();
        TemplateSource source = new TemplateSource("original", "/embedded/original.tpl", "v7", "hash", "content");
        source.Timestamp = "time-7";

        loader.SetTemplateSource("alias", source);
        TemplateSource loaded = loader.LoadRequired("alias");

        Assert.That(loaded.Name, Is.EqualTo("alias"));
        Assert.That(loaded.Path, Is.EqualTo("/embedded/original.tpl"));
        Assert.That(loaded.Version, Is.EqualTo("v7"));
        Assert.That(loaded.ContentHash, Is.EqualTo("hash"));
        Assert.That(loaded.Timestamp, Is.EqualTo("time-7"));
        Assert.That(loaded.Text, Is.EqualTo("content"));
    }

    [Test]
    public void Load_ReturnsCopiesSoStoredSourceCannotBeMutatedByCaller()
    {
        MemoryTemplateLoader loader = new MemoryTemplateLoader();
        loader.SetTemplate("welcome", "original");

        TemplateSource first = loader.LoadRequired("welcome");
        first.Text = "changed";
        first.Length = first.Text.Length;
        TemplateSource second = loader.LoadRequired("welcome");

        Assert.That(second.Text, Is.EqualTo("original"));
        Assert.That(second.Length, Is.EqualTo(8));
    }

    [Test]
    public void RemoveClearAndGetTemplateNames_ManageStoredTemplates()
    {
        MemoryTemplateLoader loader = new MemoryTemplateLoader();
        loader.AddTemplate("one", "1");
        loader.AddTemplate("two", "2");

        List<string> names = loader.GetTemplateNames();

        Assert.That(names.Count, Is.EqualTo(2));
        Assert.That(names, Does.Contain("one"));
        Assert.That(names, Does.Contain("two"));
        Assert.That(loader.RemoveTemplate("one"), Is.True);
        Assert.That(loader.RemoveTemplate("one"), Is.False);
        Assert.That(loader.ContainsTemplate("two"), Is.True);
        loader.Clear();
        Assert.That(loader.GetTemplateNames().Count, Is.EqualTo(0));
    }

    [Test]
    public void LoadMissingAndInvalidArguments_ReturnExpectedFailures()
    {
        MemoryTemplateLoader loader = new MemoryTemplateLoader();

        Assert.That(loader.Load("missing"), Is.Null);
        Assert.Throws<KeyNotFoundException>(() => loader.LoadRequired("missing"));
        Assert.Throws<ArgumentNullException>(() => loader.SetTemplate(null!, "x"));
        Assert.Throws<ArgumentException>(() => loader.SetTemplate("", "x"));
        Assert.Throws<ArgumentNullException>(() => loader.SetTemplate("x", (string)null!));
        Assert.Throws<ArgumentNullException>(() => loader.SetTemplateSource("x", (TemplateSource)null!));
        Assert.Throws<ArgumentNullException>(() => loader.ContainsTemplate(null!));
        Assert.Throws<ArgumentNullException>(() => loader.Load(null!));
        Assert.Throws<ArgumentNullException>(() => loader.RemoveTemplate(null!));
    }

    static string Hash(string text)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }
}
