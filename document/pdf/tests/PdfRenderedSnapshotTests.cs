using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfRenderedSnapshotTests
{
    [Test]
    public void RenderedSnapshotHarness_RunsExternalRendererAndMatchesSnapshotHash()
    {
        string rendererPath = CreateFakeRenderer("PNG SNAPSHOT\n", 0);
        PdfDocument document = CreateSnapshotDocument();
        PdfRenderedSnapshotRenderer renderer = new PdfRenderedSnapshotRenderer();
        string expectedHash = PdfRenderedSnapshotRenderer.HashText("PNG SNAPSHOT\n");

        renderer.AssertSnapshot(document, rendererPath, "basic-page", expectedHash);
    }

    [Test]
    public void RenderedSnapshotHarness_FailsWhenRendererFails()
    {
        string rendererPath = CreateFakeRenderer("", 7);
        PdfDocument document = CreateSnapshotDocument();
        PdfRenderedSnapshotRenderer renderer = new PdfRenderedSnapshotRenderer();

        Assert.Throws<InvalidOperationException>(() => renderer.RenderAndHash(document, rendererPath, "failing-renderer"));
    }

    [Test]
    public void RenderedSnapshotHarness_ValidatesInputs()
    {
        PdfRenderedSnapshotRenderer renderer = new PdfRenderedSnapshotRenderer();
        PdfDocument document = CreateSnapshotDocument();
        string rendererPath = CreateFakeRenderer("PNG SNAPSHOT\n", 0);

        Assert.Throws<ArgumentNullException>(() => renderer.RenderAndHash(null!, rendererPath, "x"));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderAndHash(document, null!, "x"));
        Assert.Throws<ArgumentException>(() => renderer.RenderAndHash(document, "", "x"));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderAndHash(document, rendererPath, null!));
        Assert.Throws<ArgumentException>(() => renderer.RenderAndHash(document, rendererPath, ""));
    }

    static PdfDocument CreateSnapshotDocument()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(160, 90);

        page.Canvas.SetFillColorValue(PdfColor.Rgb(0.9, 0.95, 1));
        page.Canvas.FillRectangle(0, 0, 160, 90);
        page.Canvas.SetStrokeColorValue(PdfColor.Rgb(0.1, 0.2, 0.3));
        page.Canvas.StrokeRectangle(10, 10, 140, 70);
        page.Canvas.DrawTextWithFont("Rendered snapshot", 18, 45, 12, PdfFont.Bold);

        return document;
    }

    static string CreateFakeRenderer(string outputText, int exitCode)
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "fake-renderers");
        Directory.CreateDirectory(directory);

        if (OperatingSystem.IsWindows())
        {
            return CreateFakeWindowsRenderer(directory, outputText, exitCode);
        }

        return CreateFakeUnixRenderer(directory, outputText, exitCode);
    }

    static string CreateFakeUnixRenderer(string directory, string outputText, int exitCode)
    {
        string path = Path.Combine(directory, "renderer-" + Guid.NewGuid().ToString("N") + ".sh");
        StringBuilder builder = new StringBuilder();

        builder.AppendLine("#!/bin/sh");
        builder.AppendLine("if [ ! -f \"$1\" ]; then exit 2; fi");
        builder.AppendLine("head -c 8 \"$1\" | grep \"%PDF-1.4\" >/dev/null || exit 3");
        if (outputText.Length > 0)
        {
            builder.Append("printf '");
            builder.Append(EscapeSingleQuotedShellText(outputText));
            builder.AppendLine("' > \"$2\"");
        }

        builder.AppendLine("exit " + exitCode.ToString());

        File.WriteAllText(path, builder.ToString(), Encoding.ASCII);
        MakeExecutable(path);
        return path;
    }

    static string CreateFakeWindowsRenderer(string directory, string outputText, int exitCode)
    {
        string path = Path.Combine(directory, "renderer-" + Guid.NewGuid().ToString("N") + ".cmd");
        StringBuilder builder = new StringBuilder();

        builder.AppendLine("@echo off");
        builder.AppendLine("if not exist %1 exit /b 2");
        if (outputText.Length > 0)
        {
            builder.Append(">");
            builder.AppendLine("%2 echo " + outputText.Replace("\n", "", StringComparison.Ordinal));
        }

        builder.AppendLine("exit /b " + exitCode.ToString());

        File.WriteAllText(path, builder.ToString(), Encoding.ASCII);
        return path;
    }

    static string EscapeSingleQuotedShellText(string value)
    {
        return value.Replace("'", "'\\''", StringComparison.Ordinal);
    }

    static void MakeExecutable(string path)
    {
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.FileName = "chmod";
        startInfo.UseShellExecute = false;
        startInfo.ArgumentList.Add("+x");
        startInfo.ArgumentList.Add(path);

        System.Diagnostics.Process? process = System.Diagnostics.Process.Start(startInfo);
        Assert.That(process, Is.Not.Null);
        if (process == null)
        {
            throw new InvalidOperationException("Could not start chmod.");
        }

        process.WaitForExit();
        Assert.That(process.ExitCode, Is.EqualTo(0));
    }
}
