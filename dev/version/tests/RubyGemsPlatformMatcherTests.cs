using System;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class RubyGemsPlatformMatcherTests
{
    [Test]
    public void Platform_ParsesSingleOsAndNativeTriples()
    {
        RubyGemsPlatform java = new RubyGemsPlatform("java");
        RubyGemsPlatform mingw = new RubyGemsPlatform("x64-mingw-ucrt");

        Assert.That(java.HasCpu(), Is.False);
        Assert.That(java.GetOs(), Is.EqualTo("java"));
        Assert.That(mingw.GetCpu(), Is.EqualTo("x64"));
        Assert.That(mingw.GetOs(), Is.EqualTo("mingw"));
        Assert.That(mingw.GetVersion(), Is.EqualTo("ucrt"));
    }

    [Test]
    public void PlatformMatches_RequiresRubyToMatchRuby()
    {
        RubyGemsPlatform ruby = RubyGemsPlatform.CreateRuby();
        RubyGemsPlatform linux = new RubyGemsPlatform("x86_64-linux");

        Assert.That(RubyGemsVersions.PlatformMatches(ruby, RubyGemsPlatform.CreateRuby()), Is.True);
        Assert.That(RubyGemsVersions.PlatformMatches(ruby, linux), Is.False);
        Assert.That(RubyGemsVersions.PlatformMatches(linux, ruby), Is.False);
    }

    [Test]
    public void PlatformMatches_AllowsUniversalCpuAndGenericArm()
    {
        RubyGemsPlatform universalLinux = new RubyGemsPlatform("universal-linux");
        RubyGemsPlatform x64Linux = new RubyGemsPlatform("x86_64-linux");
        RubyGemsPlatform armLinux = new RubyGemsPlatform("arm-linux");
        RubyGemsPlatform armv7Linux = new RubyGemsPlatform("armv7-linux");

        Assert.That(RubyGemsVersions.PlatformMatches(universalLinux, x64Linux), Is.True);
        Assert.That(RubyGemsVersions.PlatformMatches(x64Linux, universalLinux), Is.True);
        Assert.That(RubyGemsVersions.PlatformMatches(armLinux, armv7Linux), Is.True);
        Assert.That(RubyGemsVersions.PlatformMatches(armv7Linux, armLinux), Is.False);
    }

    [Test]
    public void PlatformMatches_UsesOsAndVersionCompatibilityRules()
    {
        Assert.That(
            RubyGemsVersions.PlatformMatches(
                new RubyGemsPlatform("x86_64-linux"),
                new RubyGemsPlatform("x86_64-linux-gnu")),
            Is.True);
        Assert.That(
            RubyGemsVersions.PlatformMatches(
                new RubyGemsPlatform("x86_64-linux-musl"),
                new RubyGemsPlatform("x86_64-linux-gnu")),
            Is.False);
        Assert.That(
            RubyGemsVersions.PlatformMatches(
                new RubyGemsPlatform("x86_64-darwin"),
                new RubyGemsPlatform("x86_64-darwin-22")),
            Is.True);
        Assert.That(
            RubyGemsVersions.PlatformMatches(
                new RubyGemsPlatform("universal-mingw32"),
                new RubyGemsPlatform("x64-mingw-ucrt")),
            Is.True);
    }

    [Test]
    public void FilterCompatibleSpecifications_ReturnsRubyAndMatchingNativeSpecifications()
    {
        RubyGemsSpecification ruby = Spec("ffi", "1.0.0", RubyGemsPlatform.CreateRuby());
        RubyGemsSpecification linux = Spec("ffi", "1.0.0", new RubyGemsPlatform("x86_64-linux"));
        RubyGemsSpecification windows = Spec("ffi", "1.0.0", new RubyGemsPlatform("x64-mingw-ucrt"));

        RubyGemsSpecification[] matches = RubyGemsVersions.FilterCompatibleSpecifications(
            new RubyGemsSpecification[] { ruby, linux, windows },
            new RubyGemsPlatform[] { RubyGemsPlatform.CreateRuby(), new RubyGemsPlatform("x86_64-linux-gnu") });

        Assert.That(matches.Length, Is.EqualTo(2));
        Assert.That(matches[0].GetPlatform().IsRuby(), Is.True);
        Assert.That(matches[1].GetPlatform().GetOs(), Is.EqualTo("linux"));
    }

    [Test]
    public void SelectBestPlatformSpecification_PrefersExactNativeThenCompatibleNativeThenRuby()
    {
        RubyGemsSpecification ruby = Spec("ffi", "1.1.0", RubyGemsPlatform.CreateRuby());
        RubyGemsSpecification compatibleLinux = Spec("ffi", "1.0.0", new RubyGemsPlatform("universal-linux"));
        RubyGemsSpecification exactLinux = Spec("ffi", "1.0.0", new RubyGemsPlatform("x86_64-linux-gnu"));

        RubyGemsSpecification? selected = RubyGemsVersions.SelectBestPlatformSpecification(
            new RubyGemsSpecification[] { ruby, compatibleLinux, exactLinux },
            new RubyGemsPlatform[] { RubyGemsPlatform.CreateRuby(), new RubyGemsPlatform("x86_64-linux-gnu") });
        RubyGemsSpecification? fallback = RubyGemsVersions.SelectBestPlatformSpecification(
            new RubyGemsSpecification[] { ruby },
            new RubyGemsPlatform[] { RubyGemsPlatform.CreateRuby(), new RubyGemsPlatform("x86_64-linux-gnu") });

        Assert.That(selected, Is.Not.Null);
        Assert.That(selected!.GetPlatform().EqualsPlatform(new RubyGemsPlatform("x86_64-linux-gnu")), Is.True);
        Assert.That(fallback, Is.Not.Null);
        Assert.That(fallback!.GetPlatform().IsRuby(), Is.True);
    }

    [Test]
    public void PlatformSelector_RejectsNullInputElements()
    {
        Assert.That(
            delegate
            {
                RubyGemsVersions.FilterCompatibleSpecifications(
                    new RubyGemsSpecification[] { null! },
                    new RubyGemsPlatform[] { RubyGemsPlatform.CreateRuby() });
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                RubyGemsVersions.FilterCompatibleSpecifications(
                    new RubyGemsSpecification[0],
                    new RubyGemsPlatform[] { null! });
            },
            Throws.TypeOf<ArgumentException>());
    }

    static RubyGemsSpecification Spec(string name, string version, RubyGemsPlatform platform)
    {
        return RubyGemsSpecification.Create(
            name,
            version,
            platform,
            new RubyGemsDependency[0],
            RubyGemsSource.Create("rubygems", "https://rubygems.org"));
    }
}
