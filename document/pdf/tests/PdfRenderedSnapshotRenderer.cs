using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using pdf;

namespace pdf.Tests;

public class PdfRenderedSnapshotRenderer
{
    public int TimeoutMilliseconds = 10000;

    public string RenderAndHash(PdfDocument document, string rendererPath, string snapshotName)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (rendererPath == null)
        {
            throw new ArgumentNullException("rendererPath");
        }

        if (rendererPath.Length == 0)
        {
            throw new ArgumentException("Renderer path cannot be empty.", "rendererPath");
        }

        if (snapshotName == null)
        {
            throw new ArgumentNullException("snapshotName");
        }

        if (snapshotName.Length == 0)
        {
            throw new ArgumentException("Snapshot name cannot be empty.", "snapshotName");
        }

        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "rendered-snapshots", snapshotName);
        Directory.CreateDirectory(directory);

        string pdfPath = Path.Combine(directory, snapshotName + ".pdf");
        string imagePath = Path.Combine(directory, snapshotName + ".png");

        document.Save(pdfPath);
        Render(rendererPath, pdfPath, imagePath);

        byte[] renderedBytes = File.ReadAllBytes(imagePath);
        if (renderedBytes.Length == 0)
        {
            throw new InvalidOperationException("Renderer produced an empty snapshot image.");
        }

        return HashBytes(renderedBytes);
    }

    public void AssertSnapshot(PdfDocument document, string rendererPath, string snapshotName, string expectedHash)
    {
        string actualHash = RenderAndHash(document, rendererPath, snapshotName);

        Assert.That(actualHash, Is.EqualTo(expectedHash));
    }

    void Render(string rendererPath, string pdfPath, string imagePath)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = rendererPath;
        startInfo.UseShellExecute = false;
        startInfo.ArgumentList.Add(pdfPath);
        startInfo.ArgumentList.Add(imagePath);

        Process? process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Could not start PDF renderer.");
        }

        bool exited = process.WaitForExit(TimeoutMilliseconds);
        if (!exited)
        {
            process.Kill(true);
            throw new TimeoutException("PDF renderer did not exit before the timeout.");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException("PDF renderer exited with code " + process.ExitCode.ToString() + ".");
        }

        if (!File.Exists(imagePath))
        {
            throw new InvalidOperationException("PDF renderer did not write the expected snapshot image.");
        }
    }

    public static string HashText(string text)
    {
        return HashBytes(Encoding.ASCII.GetBytes(text));
    }

    public static string HashBytes(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
