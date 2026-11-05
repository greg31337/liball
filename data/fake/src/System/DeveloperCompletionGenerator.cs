using System;
using System.Globalization;

namespace fake;

internal static class DeveloperCompletionGenerator
{
    const string PackageMarker = "fake.synthetic-package-metadata.v1";

    internal static string FrameworkName(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateFrameworkData(context);
        Guard.OutputSize(
            MaximumLength(DeveloperCompletionData.FrameworkNames),
            context.CapturedOptions.MaximumOutputSize,
            "frameworkName");
        return DeveloperCompletionData.FrameworkNames[
            random.IntFromMaximumExclusive(DeveloperCompletionData.FrameworkNames.Length)];
    }

    internal static string FrameworkVersion(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateFrameworkData(context);
        Guard.OutputSize(
            MaximumNestedLength(DeveloperCompletionData.FrameworkVersions),
            context.CapturedOptions.MaximumOutputSize,
            "frameworkVersion");
        int frameworkIndex = random.IntFromMaximumExclusive(
            DeveloperCompletionData.FrameworkNames.Length);
        string[] versions = DeveloperCompletionData.FrameworkVersions[frameworkIndex];
        return versions[random.IntFromMaximumExclusive(versions.Length)];
    }

    internal static string FrameworkVersionFromName(
        GenerationContext context,
        FakeRandom random,
        string frameworkName)
    {
        frameworkName = Guard.NotNullOrWhiteSpace(frameworkName, "frameworkName");
        Guard.Count(
            frameworkName.Length,
            context.CapturedOptions.MaximumCount,
            "frameworkName");
        UnicodeText.Validate(frameworkName, "frameworkName");
        ValidateFrameworkData(context);
        int frameworkIndex = FindFrameworkIndex(frameworkName);
        if (frameworkIndex < 0)
        {
            throw new ArgumentException(
                "The framework name is not in the reviewed framework catalog.",
                "frameworkName");
        }
        string[] versions = DeveloperCompletionData.FrameworkVersions[frameworkIndex];
        Guard.OutputSize(
            MaximumLength(versions),
            context.CapturedOptions.MaximumOutputSize,
            "frameworkVersion");
        return versions[random.IntFromMaximumExclusive(versions.Length)];
    }

    internal static string ApplicationVersion(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.OutputSize(
            8,
            context.CapturedOptions.MaximumOutputSize,
            "applicationVersion");
        return ApplicationVersionUnchecked(random);
    }

    internal static bool IsApplicationVersion(
        GenerationContext context,
        string value)
    {
        Guard.NotNull(value, "value");
        Guard.Count(
            value.Length,
            context.CapturedOptions.MaximumCount,
            "value");
        UnicodeText.Validate(value, "value");
        return HasApplicationVersionShape(value);
    }

    internal static PackageMetadataRecord PackageMetadata(
        GenerationContext context,
        FakeRandom random,
        FilePathStyle style)
    {
        char separator = PathGenerator.Separator(style);
        ValidatePackageData(context);
        Guard.OutputSize(
            MaximumPackageRecordLength(),
            context.CapturedOptions.MaximumOutputSize,
            "packageMetadata");

        int frameworkIndex = random.IntFromMaximumExclusive(
            DeveloperCompletionData.FrameworkNames.Length);
        string[] versions = DeveloperCompletionData.FrameworkVersions[frameworkIndex];
        int versionIndex = random.IntFromMaximumExclusive(versions.Length);
        string name = DeveloperCompletionData.PackageNames[
            random.IntFromMaximumExclusive(DeveloperCompletionData.PackageNames.Length)];
        string version = ApplicationVersionUnchecked(random);

        PackageMetadataRecord result = new PackageMetadataRecord();
        result.Name = name;
        result.Version = version;
        result.FrameworkName = DeveloperCompletionData.FrameworkNames[frameworkIndex];
        result.FrameworkVersion = versions[versionIndex];
        result.TargetFramework = DeveloperCompletionData.TargetFrameworks[frameworkIndex][versionIndex];
        result.PackageManager = DeveloperCompletionData.PackageManagers[frameworkIndex];
        result.PathStyle = style;
        result.InstallPath = "packages" + separator + name + separator + version;
        result.IsSynthetic = true;
        result.TestMarker = PackageMarker;
        result.RepresentsPublishedPackage = false;
        result.RepresentsInstalledPackage = false;
        result.VersionsAreCurrent = false;
        return result;
    }

    internal static string LineEnding(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.OutputSize(
            2,
            context.CapturedOptions.MaximumOutputSize,
            "lineEnding");
        LineEndingStyle style = (LineEndingStyle)random.IntFromMaximumExclusive(3);
        return LineEndingUnchecked(style);
    }

    internal static string LineEndingFromStyle(
        GenerationContext context,
        LineEndingStyle style)
    {
        int length = ValidateLineEndingStyle(style);
        Guard.OutputSize(
            length,
            context.CapturedOptions.MaximumOutputSize,
            "lineEnding");
        return LineEndingUnchecked(style);
    }

    internal static string LineFeed(GenerationContext context)
    {
        return LineEndingFromStyle(context, LineEndingStyle.LineFeed);
    }

    internal static string CarriageReturnLineFeed(GenerationContext context)
    {
        return LineEndingFromStyle(context, LineEndingStyle.CarriageReturnLineFeed);
    }

    internal static string CarriageReturn(GenerationContext context)
    {
        return LineEndingFromStyle(context, LineEndingStyle.CarriageReturn);
    }

    static string ApplicationVersionUnchecked(FakeRandom random)
    {
        int major = random.IntFromMaximumExclusive(100);
        int minor = random.IntFromMaximumExclusive(100);
        int patch = random.IntFromMaximumExclusive(100);
        return major.ToString(CultureInfo.InvariantCulture) + "." +
            minor.ToString(CultureInfo.InvariantCulture) + "." +
            patch.ToString(CultureInfo.InvariantCulture);
    }

    static bool HasApplicationVersionShape(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }
        int componentStart = 0;
        int separatorCount = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '.')
            {
                if (!HasCanonicalNumericComponent(value, componentStart, index))
                {
                    return false;
                }
                separatorCount = separatorCount + 1;
                if (separatorCount > 2)
                {
                    return false;
                }
                componentStart = index + 1;
            }
            else if (character < '0' || character > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return separatorCount == 2 &&
            HasCanonicalNumericComponent(value, componentStart, value.Length);
    }

    static bool HasCanonicalNumericComponent(string value, int start, int end)
    {
        if (start == end)
        {
            return false;
        }
        if (end - start > 1 && value[start] == '0')
        {
            return false;
        }
        return true;
    }

    static int FindFrameworkIndex(string frameworkName)
    {
        int index = 0;
        while (index < DeveloperCompletionData.FrameworkNames.Length)
        {
            if (string.Equals(
                frameworkName,
                DeveloperCompletionData.FrameworkNames[index],
                StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static string LineEndingUnchecked(LineEndingStyle style)
    {
        if (style == LineEndingStyle.LineFeed)
        {
            return "\n";
        }
        if (style == LineEndingStyle.CarriageReturnLineFeed)
        {
            return "\r\n";
        }
        return "\r";
    }

    static int ValidateLineEndingStyle(LineEndingStyle style)
    {
        if (style == LineEndingStyle.LineFeed || style == LineEndingStyle.CarriageReturn)
        {
            return 1;
        }
        if (style == LineEndingStyle.CarriageReturnLineFeed)
        {
            return 2;
        }
        throw new ArgumentOutOfRangeException("style");
    }

    static void ValidateFrameworkData(GenerationContext context)
    {
        Guard.Count(
            DeveloperCompletionData.FrameworkNames.Length,
            context.CapturedOptions.MaximumCount,
            "frameworkData");
        if (DeveloperCompletionData.FrameworkNames.Length == 0 ||
            DeveloperCompletionData.FrameworkVersions.Length != DeveloperCompletionData.FrameworkNames.Length ||
            DeveloperCompletionData.TargetFrameworks.Length != DeveloperCompletionData.FrameworkNames.Length ||
            DeveloperCompletionData.PackageManagers.Length != DeveloperCompletionData.FrameworkNames.Length)
        {
            throw new InvalidOperationException("Framework fixture data is inconsistent.");
        }
        int index = 0;
        while (index < DeveloperCompletionData.FrameworkNames.Length)
        {
            string[] versions = DeveloperCompletionData.FrameworkVersions[index];
            string[] targets = DeveloperCompletionData.TargetFrameworks[index];
            Guard.Count(
                versions.Length,
                context.CapturedOptions.MaximumCount,
                "frameworkVersionData");
            if (versions.Length == 0 || versions.Length != targets.Length)
            {
                throw new InvalidOperationException("Framework version fixture data is inconsistent.");
            }
            index = index + 1;
        }
    }

    static void ValidatePackageData(GenerationContext context)
    {
        ValidateFrameworkData(context);
        Guard.Count(
            DeveloperCompletionData.PackageNames.Length,
            context.CapturedOptions.MaximumCount,
            "packageNameData");
        if (DeveloperCompletionData.PackageNames.Length == 0)
        {
            throw new InvalidOperationException("Package fixture data is empty.");
        }
    }

    static int MaximumPackageRecordLength()
    {
        int packageName = MaximumLength(DeveloperCompletionData.PackageNames);
        int frameworkName = MaximumLength(DeveloperCompletionData.FrameworkNames);
        int frameworkVersion = MaximumNestedLength(DeveloperCompletionData.FrameworkVersions);
        int targetFramework = MaximumNestedLength(DeveloperCompletionData.TargetFrameworks);
        int packageManager = MaximumLength(DeveloperCompletionData.PackageManagers);
        int applicationVersion = 8;
        int installPath = 8 + 1 + packageName + 1 + applicationVersion;
        return packageName + applicationVersion + frameworkName + frameworkVersion +
            targetFramework + packageManager + installPath + PackageMarker.Length;
    }

    static int MaximumLength(string[] values)
    {
        int maximum = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Length > maximum)
            {
                maximum = values[index].Length;
            }
            index = index + 1;
        }
        return maximum;
    }

    static int MaximumNestedLength(string[][] values)
    {
        int maximum = 0;
        int index = 0;
        while (index < values.Length)
        {
            int current = MaximumLength(values[index]);
            if (current > maximum)
            {
                maximum = current;
            }
            index = index + 1;
        }
        return maximum;
    }
}
