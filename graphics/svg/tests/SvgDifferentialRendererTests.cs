using System.Diagnostics;

namespace svg.Tests;

[Category("Differential")]
[Category("External")]
public class SvgDifferentialRendererTests
{
    const int Width = 64;
    const int Height = 64;

    [TestCaseSource(nameof(Cases))]
    public void MatchesLibrsvgForStandardsQuestions(SvgDifferentialCase testCase)
    {
        string executable = ResolveExecutable("SVG_DIFF_RSVG_CONVERT", "rsvg-convert");
        if (executable == null) Assert.Ignore("Install rsvg-convert or set SVG_DIFF_RSVG_CONVERT to run librsvg differential tests.");
        Compare(testCase, "librsvg", executable, SvgDifferentialTool.Librsvg);
    }

    [TestCaseSource(nameof(Cases))]
    public void MatchesResvgForStandardsQuestions(SvgDifferentialCase testCase)
    {
        string executable = ResolveExecutable("SVG_DIFF_RESVG", "resvg");
        if (executable == null) Assert.Ignore("Install resvg or set SVG_DIFF_RESVG to run resvg differential tests.");
        Compare(testCase, "resvg", executable, SvgDifferentialTool.Resvg);
    }

    [TestCaseSource(nameof(Cases))]
    public void MatchesCurrentChromiumForStandardsQuestions(SvgDifferentialCase testCase)
    {
        string executable = ConfiguredExecutable("SVG_DIFF_CHROMIUM");
        if (executable == null) Assert.Ignore("Set SVG_DIFF_CHROMIUM to a current Chrome or Chromium executable to run browser differential tests.");
        Compare(testCase, "Chromium", executable, SvgDifferentialTool.Chromium);
    }

    [TestCaseSource(nameof(Cases))]
    public void MatchesCurrentFirefoxForStandardsQuestions(SvgDifferentialCase testCase)
    {
        string executable = ConfiguredExecutable("SVG_DIFF_FIREFOX");
        if (executable == null) Assert.Ignore("Set SVG_DIFF_FIREFOX to a current Firefox executable to run browser differential tests.");
        Compare(testCase, "Firefox", executable, SvgDifferentialTool.Firefox);
    }

    static IEnumerable<SvgDifferentialCase> Cases()
    {
        yield return new SvgDifferentialCase(
            "css-current-color",
            "CSS cascade and currentColor inheritance",
            "<svg xmlns='http://www.w3.org/2000/svg' width='64' height='64' viewBox='0 0 64 64'><style>.swatch{fill:currentColor}.accent{color:#1464c8}</style><rect width='64' height='64' fill='white'/><g class='accent'><rect class='swatch' x='8' y='8' width='48' height='48' rx='8'/></g></svg>",
            10,
            0.035,
            1.5,
            "Coverage at rounded corners may differ because renderers use different antialiasing kernels.");
        yield return new SvgDifferentialCase(
            "evenodd-clip",
            "evenodd fill rule combined with a user-space clip path",
            "<svg xmlns='http://www.w3.org/2000/svg' width='64' height='64' viewBox='0 0 64 64'><rect width='64' height='64' fill='white'/><defs><clipPath id='clip' clipPathUnits='userSpaceOnUse'><circle cx='32' cy='32' r='27'/></clipPath></defs><path clip-path='url(#clip)' fill='#e02b35' fill-rule='evenodd' d='M4 4H60V60H4Z M20 20V44H44V20Z'/></svg>",
            12,
            0.055,
            2.0,
            "Only pixels along the circle and path boundaries may vary due to coverage sampling.");
        yield return new SvgDifferentialCase(
            "user-space-gradient",
            "userSpaceOnUse gradient coordinates and gradientTransform",
            "<svg xmlns='http://www.w3.org/2000/svg' width='64' height='64' viewBox='0 0 64 64'><rect width='64' height='64' fill='white'/><defs><linearGradient id='gradient' gradientUnits='userSpaceOnUse' x1='8' y1='32' x2='56' y2='32' gradientTransform='rotate(20 32 32)'><stop offset='0' stop-color='#ffbf00'/><stop offset='.5' stop-color='#35a853'/><stop offset='1' stop-color='#3157d5'/></linearGradient></defs><rect x='8' y='8' width='48' height='48' fill='url(#gradient)'/></svg>",
            10,
            0.025,
            2.5,
            "Small channel differences are intentional where gradient sample positions and byte rounding differ.");
        yield return new SvgDifferentialCase(
            "use-position-and-inheritance",
            "use instance positioning and inherited presentation properties",
            "<svg xmlns='http://www.w3.org/2000/svg' width='64' height='64' viewBox='0 0 64 64'><rect width='64' height='64' fill='white'/><defs><g id='tile'><rect width='36' height='24' rx='4'/><circle cx='18' cy='12' r='6' fill='white'/></g></defs><use href='#tile' x='14' y='20' fill='#6f42c1'/></svg>",
            12,
            0.04,
            2.0,
            "Translated rounded and circular boundaries may vary by one output pixel.");
    }

    static void Compare(SvgDifferentialCase testCase, string rendererName, string executable, SvgDifferentialTool tool)
    {
        SvgRenderOptions options = new SvgRenderOptions();
        options.ViewportWidth = Width;
        options.ViewportHeight = Height;
        options.SamplesPerAxis = 4;
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(testCase.Source, new SvgLoadOptions());
        Assert.That(loaded.Success, Is.True, "Differential case did not load: " + testCase.Name);
        SvgRenderResult internalResult = new SvgRenderer().RenderWithSvgDocumentAndSvgRenderOptions(loaded.Document, options);
        Assert.That(internalResult.Success, Is.True, "Internal render failed for " + testCase.Name + ": " + internalResult.Error);

        string directory = Path.Combine(Path.GetTempPath(), "svg-differential-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string sourcePath = Path.Combine(directory, testCase.Name + ".svg");
            string outputPath = Path.Combine(directory, rendererName + ".png");
            File.WriteAllText(sourcePath, testCase.Source);
            SvgExternalProcessResult process = RunExternal(executable, tool, sourcePath, outputPath, directory);
            Assert.That(process.TimedOut, Is.False, rendererName + " timed out for " + testCase.Name + ".");
            Assert.That(process.ExitCode, Is.Zero, rendererName + " failed for " + testCase.Name + ": " + process.Output);
            Assert.That(File.Exists(outputPath), Is.True, rendererName + " did not produce a PNG for " + testCase.Name + ". " + process.Output);
            SvgPngDecodeResult externalResult = new SvgPngCodec().DecodeWithBytes(File.ReadAllBytes(outputPath));
            Assert.That(externalResult.Success, Is.True, rendererName + " produced an unreadable PNG for " + testCase.Name + ": " + externalResult.Error);
            Assert.That(externalResult.Width, Is.EqualTo(Width), rendererName + " output width differs for " + testCase.Name + ".");
            Assert.That(externalResult.Height, Is.EqualTo(Height), rendererName + " output height differs for " + testCase.Name + ".");
            SvgPixelDifference difference = Difference(internalResult.Pixels, externalResult.Pixels, testCase.ChannelTolerance);
            string context = rendererName + " vs internal for " + testCase.Name + " (" + testCase.Question + "). Intentional difference: " + testCase.IntentionalDifference;
            Assert.Multiple(() =>
            {
                Assert.That(difference.DifferentPixelRatio, Is.LessThanOrEqualTo(testCase.MaximumDifferentPixelRatio), context + " Different pixel ratio: " + difference.DifferentPixelRatio);
                Assert.That(difference.MeanChannelError, Is.LessThanOrEqualTo(testCase.MaximumMeanChannelError), context + " Mean channel error: " + difference.MeanChannelError);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    static SvgExternalProcessResult RunExternal(string executable, SvgDifferentialTool tool, string sourcePath, string outputPath, string directory)
    {
        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = executable;
        start.UseShellExecute = false;
        start.RedirectStandardError = true;
        start.RedirectStandardOutput = true;
        if (tool == SvgDifferentialTool.Librsvg)
        {
            start.ArgumentList.Add("--output");
            start.ArgumentList.Add(outputPath);
            start.ArgumentList.Add(sourcePath);
        }
        else if (tool == SvgDifferentialTool.Resvg)
        {
            start.ArgumentList.Add(sourcePath);
            start.ArgumentList.Add(outputPath);
        }
        else
        {
            string htmlPath = Path.Combine(directory, "browser.html");
            File.WriteAllText(htmlPath, "<!doctype html><meta charset='utf-8'><style>html,body{margin:0;width:64px;height:64px;overflow:hidden}</style><img width='64' height='64' src='" + new Uri(sourcePath).AbsoluteUri + "'>");
            if (tool == SvgDifferentialTool.Chromium)
            {
                start.ArgumentList.Add("--headless=new");
                start.ArgumentList.Add("--disable-gpu");
                start.ArgumentList.Add("--hide-scrollbars");
                start.ArgumentList.Add("--force-device-scale-factor=1");
                start.ArgumentList.Add("--window-size=64,64");
                start.ArgumentList.Add("--user-data-dir=" + Path.Combine(directory, "chromium-profile"));
                start.ArgumentList.Add("--screenshot=" + outputPath);
                start.ArgumentList.Add(new Uri(htmlPath).AbsoluteUri);
            }
            else
            {
                start.ArgumentList.Add("--headless");
                start.ArgumentList.Add("--window-size");
                start.ArgumentList.Add("64,64");
                start.ArgumentList.Add("--screenshot");
                start.ArgumentList.Add(outputPath);
                start.ArgumentList.Add(new Uri(htmlPath).AbsoluteUri);
            }
        }

        using Process process = Process.Start(start);
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();
        bool exited = process.WaitForExit(30000);
        if (!exited)
        {
            process.Kill(true);
            process.WaitForExit();
        }
        string output = standardOutput.GetAwaiter().GetResult() + standardError.GetAwaiter().GetResult();
        return new SvgExternalProcessResult(exited ? process.ExitCode : -1, !exited, output);
    }

    static SvgPixelDifference Difference(SvgPixelBuffer expected, SvgPixelBuffer actual, int channelTolerance)
    {
        long differentPixels = 0;
        long absoluteError = 0;
        int y = 0;
        while (y < expected.Height)
        {
            int x = 0;
            while (x < expected.Width)
            {
                SvgPremultipliedRgba8 first = expected.Read(x, y).Pixel;
                SvgPremultipliedRgba8 second = actual.Read(x, y).Pixel;
                int red = Math.Abs(first.Red - second.Red);
                int green = Math.Abs(first.Green - second.Green);
                int blue = Math.Abs(first.Blue - second.Blue);
                int alpha = Math.Abs(first.Alpha - second.Alpha);
                absoluteError += red + green + blue + alpha;
                if (red > channelTolerance || green > channelTolerance || blue > channelTolerance || alpha > channelTolerance) differentPixels++;
                x++;
            }
            y++;
        }
        double pixelCount = expected.Width * expected.Height;
        return new SvgPixelDifference(differentPixels / pixelCount, absoluteError / (pixelCount * 4));
    }

    static string ConfiguredExecutable(string variable)
    {
        string value = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value;
    }

    static string ResolveExecutable(string variable, string command)
    {
        string configured = ConfiguredExecutable(variable);
        if (configured != null) return configured;
        string path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path)) return null;
        string[] directories = path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (string directory in directories)
        {
            string candidate = Path.Combine(directory, command);
            if (File.Exists(candidate)) return candidate;
            if (OperatingSystem.IsWindows() && File.Exists(candidate + ".exe")) return candidate + ".exe";
        }
        return null;
    }
}

public enum SvgDifferentialTool
{
    Librsvg,
    Resvg,
    Chromium,
    Firefox
}

public class SvgDifferentialCase
{
    public string Name;
    public string Question;
    public string Source;
    public int ChannelTolerance;
    public double MaximumDifferentPixelRatio;
    public double MaximumMeanChannelError;
    public string IntentionalDifference;

    public SvgDifferentialCase(string name, string question, string source, int channelTolerance, double maximumDifferentPixelRatio, double maximumMeanChannelError, string intentionalDifference)
    {
        Name = name;
        Question = question;
        Source = source;
        ChannelTolerance = channelTolerance;
        MaximumDifferentPixelRatio = maximumDifferentPixelRatio;
        MaximumMeanChannelError = maximumMeanChannelError;
        IntentionalDifference = intentionalDifference;
    }

    public override string ToString()
    {
        return Name;
    }
}

public class SvgExternalProcessResult
{
    public int ExitCode;
    public bool TimedOut;
    public string Output;

    public SvgExternalProcessResult(int exitCode, bool timedOut, string output)
    {
        ExitCode = exitCode;
        TimedOut = timedOut;
        Output = output;
    }
}

public class SvgPixelDifference
{
    public double DifferentPixelRatio;
    public double MeanChannelError;

    public SvgPixelDifference(double differentPixelRatio, double meanChannelError)
    {
        DifferentPixelRatio = differentPixelRatio;
        MeanChannelError = meanChannelError;
    }
}
