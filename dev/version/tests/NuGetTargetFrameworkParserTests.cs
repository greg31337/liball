using System;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NuGetTargetFrameworkParserTests
{
    [Test]
    public void TargetFrameworkKind_ExposesSupportedFrameworkFamilies()
    {
        Assert.That(Enum.GetNames(typeof(NuGetTargetFrameworkKind)), Is.EqualTo(new string[]
        {
            "Unsupported",
            "NetFramework",
            "NetCoreApp",
            "NetStandard",
            "Net",
            "Portable",
            "Windows",
            "WindowsPhone",
            "Silverlight",
            "Uap"
        }));
    }

    [Test]
    public void Parse_CommonDotNetTargetFrameworkMonikers()
    {
        AssertFramework("net48", NuGetTargetFrameworkKind.NetFramework, "4.8", "net48");
        AssertFramework("net472", NuGetTargetFrameworkKind.NetFramework, "4.7.2", "net472");
        AssertFramework("NETStandard2.0", NuGetTargetFrameworkKind.NetStandard, "2.0", "netstandard2.0");
        AssertFramework("netcoreapp3.1", NuGetTargetFrameworkKind.NetCoreApp, "3.1", "netcoreapp3.1");
        AssertFramework("net8.0", NuGetTargetFrameworkKind.Net, "8.0", "net8.0");
        AssertFramework("uap10.0", NuGetTargetFrameworkKind.Uap, "10.0", "uap10.0");
        AssertFramework("win81", NuGetTargetFrameworkKind.Windows, "8.1", "win81");
        AssertFramework("wpa81", NuGetTargetFrameworkKind.WindowsPhone, "8.1", "wpa81");
        AssertFramework("sl5", NuGetTargetFrameworkKind.Silverlight, "5", "sl5");
    }

    [Test]
    public void Parse_ModernNetTargetFrameworkWithPlatform()
    {
        NuGetTargetFramework framework = Parse(" net8.0-windows10.0.19041 ");

        Assert.That(framework.GetOriginalText(), Is.EqualTo(" net8.0-windows10.0.19041 "));
        Assert.That(framework.GetNormalizedText(), Is.EqualTo("net8.0-windows10.0.19041"));
        Assert.That(framework.GetKind(), Is.EqualTo(NuGetTargetFrameworkKind.Net));
        Assert.That(framework.GetVersionText(), Is.EqualTo("8.0"));
        Assert.That(framework.HasPlatform(), Is.True);
        Assert.That(framework.GetPlatformText(), Is.EqualTo("windows"));
        Assert.That(framework.HasPlatformVersion(), Is.True);
        Assert.That(framework.GetPlatformVersionText(), Is.EqualTo("10.0.19041"));
    }

    [Test]
    public void Parse_PortableProfileTargetFramework()
    {
        NuGetTargetFramework framework = Parse("portable-net45+win8+wpa81");

        Assert.That(framework.GetKind(), Is.EqualTo(NuGetTargetFrameworkKind.Portable));
        Assert.That(framework.IsPortable(), Is.True);
        Assert.That(framework.GetProfileText(), Is.EqualTo("net45+win8+wpa81"));
        Assert.That(framework.GetPortableFrameworkCount(), Is.EqualTo(3));
        Assert.That(framework.GetPortableFramework(0), Is.EqualTo("net45"));
        Assert.That(framework.GetPortableFramework(1), Is.EqualTo("win8"));
        Assert.That(framework.GetPortableFramework(2), Is.EqualTo("wpa81"));
    }

    [Test]
    public void ParseFallbackList_ParsesSemicolonSeparatedFallbackFrameworks()
    {
        NuGetTargetFrameworkFallbackParseResult result = NuGetVersions.ParseTargetFrameworkFallbackList(
            "net8.0; netstandard2.0 ; portable-net45+win8");

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.HasFallbackList(), Is.True);
        Assert.That(result.GetFallbackList().GetFrameworkCount(), Is.EqualTo(3));
        Assert.That(result.GetFallbackList().GetFramework(0).GetNormalizedText(), Is.EqualTo("net8.0"));
        Assert.That(result.GetFallbackList().GetFramework(1).GetNormalizedText(), Is.EqualTo("netstandard2.0"));
        Assert.That(result.GetFallbackList().GetFramework(2).IsPortable(), Is.True);
    }

    [Test]
    public void Parse_ReportsDiagnosticsForNullEmptyUnsupportedAndInvalidFrameworks()
    {
        AssertFailure(NuGetVersions.ParseTargetFramework(null), NuGetTargetFrameworkDiagnosticCodes.NullInput, "");
        AssertFailure(NuGetVersions.ParseTargetFramework("  "), NuGetTargetFrameworkDiagnosticCodes.EmptyInput, "  ");
        AssertFailure(NuGetVersions.ParseTargetFramework("xamarin.ios10"), NuGetTargetFrameworkDiagnosticCodes.UnsupportedFramework, "xamarin.ios10");
        AssertFailure(NuGetVersions.ParseTargetFramework("netabc"), NuGetTargetFrameworkDiagnosticCodes.InvalidVersion, "netabc");
        AssertFailure(NuGetVersions.ParseTargetFramework("portable-net45+"), NuGetTargetFrameworkDiagnosticCodes.InvalidProfile, "portable-net45+");
        AssertFailure(NuGetVersions.ParseTargetFramework("net8.0-"), NuGetTargetFrameworkDiagnosticCodes.UnsupportedFramework, "net8.0-");
    }

    [Test]
    public void ParseFallbackList_ReportsDiagnosticsForEmptyEntriesAndInvalidFallbacks()
    {
        AssertFallbackFailure(
            NuGetTargetFrameworkParser.ParseFallbackList("net8.0;;netstandard2.0"),
            NuGetTargetFrameworkDiagnosticCodes.InvalidFallbackList,
            "net8.0;;netstandard2.0");
        AssertFallbackFailure(
            NuGetTargetFrameworkParser.ParseFallbackList("net8.0;xamarin.ios10"),
            NuGetTargetFrameworkDiagnosticCodes.UnsupportedFramework,
            "net8.0;xamarin.ios10");
    }

    [Test]
    public void FrameworkAndFallbackList_DefensivelyCopyArrays()
    {
        string[] portableFrameworks = new string[] { "net45", "win8" };
        NuGetTargetFramework framework = new NuGetTargetFramework(
            "portable-net45+win8",
            "portable-net45+win8",
            NuGetTargetFrameworkKind.Portable,
            "",
            "",
            "",
            "net45+win8",
            portableFrameworks);

        portableFrameworks[0] = "changed";
        string[] returnedPortableFrameworks = framework.GetPortableFrameworks();
        returnedPortableFrameworks[1] = "changed";

        Assert.That(framework.GetPortableFramework(0), Is.EqualTo("net45"));
        Assert.That(framework.GetPortableFramework(1), Is.EqualTo("win8"));

        NuGetTargetFrameworkFallbackList fallbackList = new NuGetTargetFrameworkFallbackList(new NuGetTargetFramework[] { framework });
        NuGetTargetFramework[] returnedFrameworks = fallbackList.GetFrameworks();
        returnedFrameworks[0] = Parse("net8.0");

        Assert.That(fallbackList.GetFramework(0).GetNormalizedText(), Is.EqualTo("portable-net45+win8"));
    }

    static void AssertFramework(string text, NuGetTargetFrameworkKind kind, string version, string normalized)
    {
        NuGetTargetFramework framework = Parse(text);

        Assert.That(framework.GetKind(), Is.EqualTo(kind), text);
        Assert.That(framework.GetVersionText(), Is.EqualTo(version), text);
        Assert.That(framework.GetNormalizedText(), Is.EqualTo(normalized), text);
        Assert.That(framework.HasPlatform(), Is.False, text);
        Assert.That(framework.IsPortable(), Is.False, text);
    }

    static NuGetTargetFramework Parse(string text)
    {
        NuGetTargetFrameworkParseResult result = NuGetTargetFrameworkParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True, text);
        Assert.That(result.HasFramework(), Is.True, text);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(0), text);
        return result.GetFramework();
    }

    static void AssertFailure(NuGetTargetFrameworkParseResult result, string code, string source)
    {
        Assert.That(result.IsFailure(), Is.True, source);
        Assert.That(result.HasFramework(), Is.False, source);
        Assert.That(result.GetSourceText(), Is.EqualTo(source), source);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), source);
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(code), source);
        Assert.That(result.GetDiagnostic(0).GetSourceText(), Is.EqualTo(source), source);
    }

    static void AssertFallbackFailure(NuGetTargetFrameworkFallbackParseResult result, string code, string source)
    {
        Assert.That(result.IsFailure(), Is.True, source);
        Assert.That(result.HasFallbackList(), Is.False, source);
        Assert.That(result.GetSourceText(), Is.EqualTo(source), source);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), source);
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(code), source);
        Assert.That(result.GetDiagnostic(0).GetSourceText(), Is.EqualTo(source), source);
    }
}
