using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace image;

internal static class ImageMetadataScannerCore
{
    internal static ImageMetadataScanResult ScanDirectory(string? directoryPath, ImageMetadataScanOptions? options)
    {
        ImageMetadataScanResult result = new ImageMetadataScanResult();
        if (directoryPath == null || directoryPath.Length == 0)
        {
            MetadataDiagnostic.AddRecoverableError(result.Diagnostics, "scanner.directory.empty", "Scan directory path was empty.");
            return result;
        }
        if (!Directory.Exists(directoryPath))
        {
            MetadataDiagnostic.AddRecoverableError(result.Diagnostics, "scanner.directory.not-found", "Scan directory did not exist or could not be accessed.");
            return result;
        }

        ImageMetadataScanOptions actualOptions = options == null ? new ImageMetadataScanOptions() : options;
        string rootPath = Path.GetFullPath(directoryPath);
        List<string> paths = EnumeratePathsSafely(result, rootPath, actualOptions);
        paths.Sort(StringComparer.Ordinal);

        foreach (string path in paths)
        {
            ImageMetadataScanFileResult fileResult = ReadFile(path, actualOptions.ReaderOptions);
            if (!fileResult.IsSuccess || MatchesFormat(fileResult.Document.Format.Kind, actualOptions.Formats))
            {
                result.Files.Add(fileResult);
            }
        }
        return result;
    }

    internal static List<string> EnumeratePathsSafely(ImageMetadataScanResult result, string rootPath, ImageMetadataScanOptions options)
    {
        Task<List<string>> task = Task.Run(() => EnumeratePaths(rootPath, options));
        WaitForTask(task);
        if (task.IsFaulted || task.IsCanceled)
        {
            MetadataDiagnostic.AddRecoverableError(result.Diagnostics, "scanner.directory.read-failed", FaultMessage(task.Exception, "Scan directory traversal failed."));
            return new List<string>();
        }
        return task.Result;
    }

    internal static List<string> EnumeratePaths(string rootPath, ImageMetadataScanOptions options)
    {
        List<string> paths = new List<string>();
        EnumerationOptions enumeration = new EnumerationOptions();
        enumeration.RecurseSubdirectories = options.Recursive;
        enumeration.IgnoreInaccessible = true;
        enumeration.AttributesToSkip = FileAttributes.ReparsePoint;
        enumeration.ReturnSpecialDirectories = false;

        foreach (string path in Directory.EnumerateFiles(rootPath, "*", enumeration))
        {
            if (MatchesExtension(path, options.Extensions))
            {
                paths.Add(Path.GetFullPath(path));
            }
        }
        return paths;
    }

    internal static ImageMetadataScanFileResult ReadFile(string path, ImageMetadataReaderOptions? readerOptions)
    {
        ImageMetadataScanFileResult result = new ImageMetadataScanFileResult();
        result.Path = path == null ? "" : path;
        if (path == null || path.Length == 0 || !File.Exists(path))
        {
            MetadataDiagnostic.AddRecoverableError(result.Diagnostics, "scanner.file.unavailable", "Scanned file disappeared or could not be accessed before reading.");
            return result;
        }

        Task<ImageMetadataDocument> task = Task.Run(() => ImageMetadataReaderCore.ReadFileWithOptions(path, readerOptions));
        WaitForTask(task);
        if (task.IsFaulted || task.IsCanceled)
        {
            MetadataDiagnostic.AddRecoverableError(result.Diagnostics, "scanner.file.read-failed", FaultMessage(task.Exception, "Scanned file could not be read."));
            return result;
        }

        ImageMetadataDocument document = task.Result;
        result.Document = document;
        result.IsSuccess = true;
        if (document.Diagnostics != null)
        {
            foreach (MetadataDiagnostic diagnostic in document.Diagnostics)
            {
                if (diagnostic != null)
                {
                    result.Diagnostics.Add(diagnostic);
                }
            }
        }
        return result;
    }

    internal static bool MatchesExtension(string path, string[]? extensions)
    {
        if (extensions == null || extensions.Length == 0)
        {
            return true;
        }
        string actual = Path.GetExtension(path);
        foreach (string extension in extensions)
        {
            if (extension != null)
            {
                string expected = extension;
                if (expected.Length > 0 && expected[0] != '.')
                {
                    expected = "." + expected;
                }
                if (string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        return false;
    }

    internal static bool MatchesFormat(ImageFormatKind format, ImageFormatKind[]? formats)
    {
        if (formats == null || formats.Length == 0)
        {
            return true;
        }
        foreach (ImageFormatKind expected in formats)
        {
            if (format == expected)
            {
                return true;
            }
        }
        return false;
    }

    internal static void WaitForTask(Task task)
    {
        using (WaitHandle handle = ((IAsyncResult)task).AsyncWaitHandle)
        {
            handle.WaitOne();
        }
    }

    internal static string FaultMessage(AggregateException? exception, string fallback)
    {
        if (exception == null)
        {
            return fallback;
        }
        string message = exception.GetBaseException().Message;
        if (message == null || message.Length == 0)
        {
            return fallback;
        }
        return message;
    }
}
