using System;
using System.IO;

namespace pdf;

internal static class Program
{
    internal static void Main(string[] args)
    {
        string outputPath = string.Empty;
        if (args.Length > 0)
        {
            outputPath = Path.GetFullPath(args[0]);
        }
        else
        {
            outputPath = Path.Combine(Environment.CurrentDirectory, "sample-report.pdf");
        }

        SampleReport.Generate(outputPath);

        Console.WriteLine("Created PDF: " + outputPath);
    }
}
