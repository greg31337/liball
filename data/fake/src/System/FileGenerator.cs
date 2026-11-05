using System;

namespace fake;

internal static class FileGenerator
{
    const string ChecksumAlgorithm = "SHA-256";
    const long MaximumFileSize = 10000000000L;

    internal static FileFormatRecord Format(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateData(context);
        return CopyFormat(
            random.IntFromMaximumExclusive(FileData.Extensions.Length),
            context.CapturedOptions.MaximumOutputSize);
    }

    internal static FileFormatRecord Format(
        GenerationContext context,
        string extension)
    {
        ValidateData(context);
        string normalized = NormalizeExtension(extension);
        int index = FindExtension(normalized);
        if (index < 0)
        {
            throw new ArgumentException(
                $"extension '{extension}' is not present in the reviewed file-format table.",
                "extension");
        }
        return CopyFormat(index, context.CapturedOptions.MaximumOutputSize);
    }

    internal static FileFormatRecord[] Formats(GenerationContext context)
    {
        ValidateData(context);
        Guard.Count(
            FileData.Extensions.Length,
            context.CapturedOptions.MaximumCount,
            "fileFormats");
        FileFormatRecord[] result = new FileFormatRecord[FileData.Extensions.Length];
        int size = 0;
        int index = 0;
        while (index < result.Length)
        {
            result[index] = CopyFormat(
                index,
                FakeLimits.DefaultMaximumOutputSize);
            size = size + FormatSize(result[index]);
            index = index + 1;
        }
        Guard.OutputSize(
            size,
            context.CapturedOptions.MaximumOutputSize,
            "fileFormats");
        return result;
    }

    internal static string BaseName(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateBaseNames(context);
        return FileData.BaseNames[random.IntFromMaximumExclusive(FileData.BaseNames.Length)];
    }

    internal static string Extension(
        GenerationContext context,
        FakeRandom random)
    {
        return Format(context, random).Extension;
    }

    internal static string MimeType(
        GenerationContext context,
        FakeRandom random)
    {
        return Format(context, random).MimeType;
    }

    internal static string Type(
        GenerationContext context,
        FakeRandom random)
    {
        return Format(context, random).Type;
    }

    internal static string Name(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateName(context);
        int formatIndex = random.IntFromMaximumExclusive(FileData.Extensions.Length);
        return NameUnchecked(context, random, formatIndex);
    }

    internal static string Name(
        GenerationContext context,
        FakeRandom random,
        string extension)
    {
        ValidateBaseNames(context);
        ValidateData(context);
        string normalized = NormalizeExtension(extension);
        int formatIndex = FindExtension(normalized);
        if (formatIndex < 0)
        {
            throw new ArgumentException(
                $"extension '{extension}' is not present in the reviewed file-format table.",
                "extension");
        }
        return NameUnchecked(context, random, formatIndex);
    }

    internal static long Size(FakeRandom random)
    {
        return random.LongFromMinimumAndMaximumAndMaximumInclusive(0L, MaximumFileSize, true);
    }

    internal static string Checksum(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.OutputSize(
            64,
            context.CapturedOptions.MaximumOutputSize,
            "fileChecksum");
        return SecretGenerator.Hex(
            random,
            64,
            context.CapturedOptions.MaximumOutputSize);
    }

    internal static FileRecord File(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateBaseNames(context);
        ValidateData(context);
        Guard.OutputSize(
            64,
            context.CapturedOptions.MaximumOutputSize,
            "fileChecksum");
        int formatIndex = random.IntFromMaximumExclusive(FileData.Extensions.Length);
        return FileUnchecked(context, random, formatIndex);
    }

    internal static FileRecord File(
        GenerationContext context,
        FakeRandom random,
        string extension)
    {
        ValidateBaseNames(context);
        ValidateData(context);
        Guard.OutputSize(
            64,
            context.CapturedOptions.MaximumOutputSize,
            "fileChecksum");
        string normalized = NormalizeExtension(extension);
        int formatIndex = FindExtension(normalized);
        if (formatIndex < 0)
        {
            throw new ArgumentException(
                $"extension '{extension}' is not present in the reviewed file-format table.",
                "extension");
        }
        return FileUnchecked(context, random, formatIndex);
    }

    static FileRecord FileUnchecked(
        GenerationContext context,
        FakeRandom random,
        int formatIndex)
    {
        Guard.OutputSize(
            64,
            context.CapturedOptions.MaximumOutputSize,
            "fileChecksum");
        string baseName = FileData.BaseNames[random.IntFromMaximumExclusive(FileData.BaseNames.Length)];
        string extension = FileData.Extensions[formatIndex];
        string name = baseName + "." + extension;
        FileRecord result = new FileRecord();
        result.BaseName = baseName;
        result.Extension = extension;
        result.Name = name;
        result.Type = FileData.Types[formatIndex];
        result.MimeType = FileData.MimeTypes[formatIndex];
        result.Size = Size(random);
        result.Checksum = Checksum(context, random);
        result.ChecksumAlgorithm = ChecksumAlgorithm;
        result.ChecksumIsContentDigest = false;
        result.IsSynthetic = true;
        Guard.OutputSize(
            FileSize(result),
            context.CapturedOptions.MaximumOutputSize,
            "file");
        return result;
    }

    static string NameUnchecked(
        GenerationContext context,
        FakeRandom random,
        int formatIndex)
    {
        int maximumLength = MaximumLength(FileData.BaseNames) + 1 +
            FileData.Extensions[formatIndex].Length;
        Guard.OutputSize(
            maximumLength,
            context.CapturedOptions.MaximumOutputSize,
            "fileName");
        return FileData.BaseNames[random.IntFromMaximumExclusive(FileData.BaseNames.Length)] + "." +
            FileData.Extensions[formatIndex];
    }

    internal static void ValidateName(GenerationContext context)
    {
        ValidateBaseNames(context);
        ValidateData(context);
    }

    internal static int MaximumNameLength()
    {
        return MaximumLength(FileData.BaseNames) + 1 +
            MaximumLength(FileData.Extensions);
    }

    static FileFormatRecord CopyFormat(int index, int maximumOutputSize)
    {
        FileFormatRecord result = new FileFormatRecord();
        result.Extension = FileData.Extensions[index];
        result.MimeType = FileData.MimeTypes[index];
        result.Type = FileData.Types[index];
        result.RegistryVersion = MediaTypeRegistryStore.Current().SourceVersion;
        result.MimeTypeIsRegistered = true;
        Guard.OutputSize(
            FormatSize(result),
            maximumOutputSize,
            "fileFormat");
        return result;
    }

    static void ValidateData(GenerationContext context)
    {
        if (FileData.Extensions.Length != FileData.MimeTypes.Length ||
            FileData.Extensions.Length != FileData.Types.Length)
        {
            throw FakeException.Create(
                "The reviewed file-format table has inconsistent column lengths.");
        }
        Guard.Count(
            FileData.Extensions.Length,
            context.CapturedOptions.MaximumCount,
            "fileFormatData");
        int index = 0;
        while (index < FileData.Extensions.Length)
        {
            ValidateExtension(FileData.Extensions[index], "fileFormatData");
            Guard.OutputSize(
                FileData.Extensions[index].Length +
                    FileData.MimeTypes[index].Length +
                    FileData.Types[index].Length,
                context.CapturedOptions.MaximumOutputSize,
                "fileFormatData");
            if (!MediaTypeRegistryStore.Current().Contains(FileData.MimeTypes[index]))
            {
                throw FakeException.Create(
                    $"The file-format MIME type '{FileData.MimeTypes[index]}' is absent from the bundled IANA registry.");
            }
            index = index + 1;
        }
    }

    static void ValidateBaseNames(GenerationContext context)
    {
        Guard.Count(
            FileData.BaseNames.Length,
            context.CapturedOptions.MaximumCount,
            "fileBaseNameData");
        int index = 0;
        while (index < FileData.BaseNames.Length)
        {
            Guard.OutputSize(
                FileData.BaseNames[index].Length,
                context.CapturedOptions.MaximumOutputSize,
                "fileBaseNameData");
            index = index + 1;
        }
    }

    static string NormalizeExtension(string extension)
    {
        Guard.NotNullOrWhiteSpace(extension, "extension");
        string result = extension;
        if (result[0] == '.')
        {
            result = result.Substring(1);
        }
        ValidateExtension(result, "extension");
        return result.ToLowerInvariant();
    }

    static void ValidateExtension(string extension, string parameterName)
    {
        if (extension.Length == 0)
        {
            throw new ArgumentException(
                "extension must contain letters or digits.",
                parameterName);
        }
        int index = 0;
        while (index < extension.Length)
        {
            char value = extension[index];
            bool valid = value >= 'a' && value <= 'z' ||
                value >= 'A' && value <= 'Z' ||
                value >= '0' && value <= '9';
            if (!valid)
            {
                throw new ArgumentException(
                    "extension must contain only ASCII letters or digits.",
                    parameterName);
            }
            index = index + 1;
        }
    }

    static int FindExtension(string extension)
    {
        int index = 0;
        while (index < FileData.Extensions.Length)
        {
            if (FileData.Extensions[index] == extension)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static int MaximumLength(string[] values)
    {
        int result = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Length > result)
            {
                result = values[index].Length;
            }
            index = index + 1;
        }
        return result;
    }

    static int FormatSize(FileFormatRecord value)
    {
        return value.Extension.Length + value.MimeType.Length +
            value.Type.Length + value.RegistryVersion.Length;
    }

    static int FileSize(FileRecord value)
    {
        return value.BaseName.Length + value.Extension.Length +
            value.Name.Length + value.Type.Length + value.MimeType.Length +
            value.Checksum.Length + value.ChecksumAlgorithm.Length;
    }
}
