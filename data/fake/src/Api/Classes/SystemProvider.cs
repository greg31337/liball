using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides file and operating-system-shaped generation operations.
/// </summary>
public class SystemProvider : ISystemProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal SystemProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("system");
    }

    /// <summary>Selects one reviewed file-extension and registered media-type mapping.</summary>
    /// <remarks>The mapping is conventional metadata, not content inspection or a security classification.</remarks>
    /// <returns>A correlated extension, MIME type, broad category, and registry version.</returns>
    public FileFormatRecord FileFormat()
    {
        return FileGenerator.Format(Context, RandomValue);
    }

    /// <summary>Looks up a reviewed file format by case-insensitive extension.</summary>
    /// <param name="extension">The extension with or without a leading dot.</param>
    /// <returns>The correlated file-format mapping.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when extension is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when extension is empty, malformed, or absent.</exception>
    public FileFormatRecord FileFormatFromExtension(string extension)
    {
        return FileGenerator.Format(Context, extension);
    }

    /// <summary>Returns every reviewed file-extension and media-type mapping.</summary>
    /// <returns>Independent records in lowercase extension order.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured collection or output limits are insufficient.</exception>
    public FileFormatRecord[] FileFormats()
    {
        return FileGenerator.Formats(Context);
    }

    /// <summary>Generates a project-authored generic file base name.</summary>
    /// <returns>A lowercase name without an extension.</returns>
    public string FileBaseName()
    {
        return FileGenerator.BaseName(Context, RandomValue);
    }

    /// <summary>Selects a reviewed lowercase file extension without a leading dot.</summary>
    /// <returns>A conventional filename extension.</returns>
    public string FileExtension()
    {
        return FileGenerator.Extension(Context, RandomValue);
    }

    /// <summary>Selects a reviewed lowercase file extension without a leading dot.</summary>
    /// <returns>A conventional filename extension.</returns>
    public string Extension()
    {
        return FileExtension();
    }

    /// <summary>Selects a broad project-authored file category.</summary>
    /// <returns>A category such as document, image, or archive.</returns>
    public string FileType()
    {
        return FileGenerator.Type(Context, RandomValue);
    }

    /// <summary>Selects an IANA-registered media type from the reviewed file-extension table.</summary>
    /// <returns>A media type correlated with one supported filename extension.</returns>
    public string MimeType()
    {
        return FileGenerator.MimeType(Context, RandomValue);
    }

    /// <summary>Generates a generic base name and reviewed extension.</summary>
    /// <returns>A filename without directory components.</returns>
    public string FileName()
    {
        return FileGenerator.Name(Context, RandomValue);
    }

    /// <summary>Generates a generic base name with a requested reviewed extension.</summary>
    /// <param name="extension">The extension with or without a leading dot.</param>
    /// <returns>A filename ending in the normalized requested extension.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when extension is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when extension is empty, malformed, or absent.</exception>
    public string FileNameFromExtension(string extension)
    {
        return FileGenerator.Name(Context, RandomValue, extension);
    }

    /// <summary>Generates a nonnegative file size from zero through ten billion bytes.</summary>
    /// <returns>A synthetic byte count.</returns>
    public long FileSize()
    {
        return FileGenerator.Size(RandomValue);
    }

    /// <summary>Generates a lowercase SHA-256-shaped random value.</summary>
    /// <remarks>The result is not a digest of file content.</remarks>
    /// <returns>Sixty-four lowercase hexadecimal characters.</returns>
    public string FileChecksum()
    {
        return FileGenerator.Checksum(Context, RandomValue);
    }

    /// <summary>Generates a correlated, explicitly synthetic file fixture.</summary>
    /// <remarks>The record creates no content, and its checksum is shape-only random data.</remarks>
    /// <returns>A name, format, size, and checksum-shaped value.</returns>
    public FileRecord File()
    {
        return FileGenerator.File(Context, RandomValue);
    }

    /// <summary>Generates a synthetic file fixture using a requested reviewed extension.</summary>
    /// <param name="extension">The extension with or without a leading dot.</param>
    /// <returns>A name, requested format, size, and checksum-shaped value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when extension is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when extension is empty, malformed, or absent.</exception>
    public FileRecord FileFromExtension(string extension)
    {
        return FileGenerator.File(Context, RandomValue, extension);
    }

    /// <summary>Generates a relative Unix directory path.</summary>
    /// <returns>One through four safe directory segments joined with forward slashes.</returns>
    public string DirectoryPath()
    {
        return PathGenerator.Directory(Context, RandomValue);
    }

    /// <summary>Generates a directory path using requested platform and root syntax.</summary>
    /// <param name="style">The Unix or Windows path syntax.</param>
    /// <param name="rooted">Whether to include a Unix root or Windows drive root.</param>
    /// <returns>One through four safe directory segments.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when style is unsupported or a configured limit is insufficient.</exception>
    public string DirectoryPathFromStyleAndRooted(FilePathStyle style, bool rooted)
    {
        return PathGenerator.Directory(Context, RandomValue, style, rooted);
    }

    /// <summary>Generates a directory path with an exact positive segment count.</summary>
    /// <param name="style">The Unix or Windows path syntax.</param>
    /// <param name="rooted">Whether to include a Unix root or Windows drive root.</param>
    /// <param name="depth">The number of directory segments.</param>
    /// <returns>A safe directory path with the requested depth.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when style, depth, or a configured limit is invalid.</exception>
    public string DirectoryPathFromStyleAndRootedAndDepth(FilePathStyle style, bool rooted, int depth)
    {
        return PathGenerator.Directory(Context, RandomValue, style, rooted, depth);
    }

    /// <summary>Generates a relative Unix path ending in a generated file name.</summary>
    /// <returns>A safe relative Unix file path.</returns>
    public string FilePath()
    {
        return PathGenerator.File(Context, RandomValue);
    }

    /// <summary>Generates a file path using requested platform and root syntax.</summary>
    /// <param name="style">The Unix or Windows path syntax.</param>
    /// <param name="rooted">Whether to include a Unix root or Windows drive root.</param>
    /// <returns>One through four safe directory segments followed by a generated file name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when style is unsupported or a configured limit is insufficient.</exception>
    public string FilePathFromStyleAndRooted(FilePathStyle style, bool rooted)
    {
        return PathGenerator.File(Context, RandomValue, style, rooted);
    }

    /// <summary>Generates a file path with an exact positive directory-segment count.</summary>
    /// <param name="style">The Unix or Windows path syntax.</param>
    /// <param name="rooted">Whether to include a Unix root or Windows drive root.</param>
    /// <param name="depth">The number of directory segments before the file name.</param>
    /// <returns>A safe file path with the requested directory depth.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when style, depth, or a configured limit is invalid.</exception>
    public string FilePathFromStyleAndRootedAndDepth(FilePathStyle style, bool rooted, int depth)
    {
        return PathGenerator.File(Context, RandomValue, style, rooted, depth);
    }

    /// <summary>Returns the canonical separator for a path syntax.</summary>
    /// <param name="style">The Unix or Windows path syntax.</param>
    /// <returns>A forward slash for Unix or backslash for Windows.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when style is unsupported.</exception>
    public char PathSeparator(FilePathStyle style)
    {
        return PathGenerator.Separator(style);
    }

    /// <summary>Generates a canonical root for the requested path syntax.</summary>
    /// <param name="style">The Unix or Windows path syntax.</param>
    /// <returns><c>/</c> for Unix or a reviewed drive root for Windows.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when style is unsupported or a configured limit is insufficient.</exception>
    public string PathRoot(FilePathStyle style)
    {
        return PathGenerator.Root(Context, RandomValue, style);
    }

    /// <summary>Selects a name reserved by the requested path syntax.</summary>
    /// <remarks>This explicit edge API does not use reserved names in ordinary generated paths.</remarks>
    /// <param name="style">The Unix or Windows path syntax.</param>
    /// <returns>A special Unix segment or reserved Windows device name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when style is unsupported or a configured limit is insufficient.</exception>
    public string ReservedPathName(FilePathStyle style)
    {
        return PathGenerator.ReservedName(Context, RandomValue, style);
    }

    /// <summary>Reports whether a name is reserved by the requested path syntax.</summary>
    /// <remarks>Windows checks are case-insensitive and recognize extensions and trailing dots or spaces on device names.</remarks>
    /// <param name="style">The Unix or Windows path syntax.</param>
    /// <param name="value">The single path-segment name to inspect.</param>
    /// <returns><c>true</c> when the segment is reserved; otherwise <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when style or a configured limit is invalid.</exception>
    /// <exception cref="System.ArgumentException">Thrown when value contains malformed UTF-16.</exception>
    public bool IsReservedPathName(FilePathStyle style, string value)
    {
        return PathGenerator.IsReservedName(Context, style, value);
    }

    /// <summary>Generates a valid Unicode path-segment name.</summary>
    /// <remarks>The returned name contains no separator or platform-invalid character and is not selected by ordinary ASCII path APIs.</remarks>
    /// <param name="style">The Unix or Windows path syntax to validate against.</param>
    /// <returns>A reviewed Unicode path-segment name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when style is unsupported or a configured limit is insufficient.</exception>
    public string UnicodePathName(FilePathStyle style)
    {
        return PathGenerator.UnicodeName(Context, RandomValue, style);
    }

    /// <summary>Selects a character that is invalid within one path segment.</summary>
    /// <remarks>This explicit method does not place the character into an ordinary generated path.</remarks>
    /// <param name="style">The Unix or Windows path syntax.</param>
    /// <returns>An invalid path-segment character for the requested platform.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when style is unsupported or a configured limit is insufficient.</exception>
    public char InvalidPathCharacter(FilePathStyle style)
    {
        return PathGenerator.InvalidCharacter(Context, RandomValue, style);
    }

    /// <summary>Returns all modeled characters that are invalid within one path segment.</summary>
    /// <remarks>The returned array is an independent copy and includes separator characters.</remarks>
    /// <param name="style">The Unix or Windows path syntax.</param>
    /// <returns>The modeled invalid path-segment characters.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when style is unsupported or a configured limit is insufficient.</exception>
    public char[] InvalidPathCharacters(FilePathStyle style)
    {
        return PathGenerator.InvalidCharacters(Context, style);
    }

    /// <summary>Generates an inert relative path containing parent-directory traversal segments.</summary>
    /// <remarks>This method returns text only and requires explicit adversarial generation mode.</remarks>
    /// <param name="style">The Unix or Windows path syntax.</param>
    /// <returns>A relative traversal edge string ending in a generated file name.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown unless generation mode is adversarial.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when style is unsupported or a configured limit is insufficient.</exception>
    public string AdversarialTraversalPath(FilePathStyle style)
    {
        return PathGenerator.AdversarialTraversal(Context, RandomValue, style, false);
    }

    /// <summary>Generates an inert path containing parent-directory traversal segments.</summary>
    /// <remarks>This method returns text only and requires explicit adversarial generation mode.</remarks>
    /// <param name="style">The Unix or Windows path syntax.</param>
    /// <param name="rooted">Whether to include a Unix root or Windows drive root.</param>
    /// <returns>A traversal edge string ending in a generated file name.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown unless generation mode is adversarial.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when style is unsupported or a configured limit is insufficient.</exception>
    public string AdversarialTraversalPathFromStyleAndRooted(FilePathStyle style, bool rooted)
    {
        return PathGenerator.AdversarialTraversal(Context, RandomValue, style, rooted);
    }

    /// <summary>Generates a valid Semantic Versioning 2.0.0 value.</summary>
    /// <remarks>Pre-release identifiers and build metadata are independently optional.</remarks>
    /// <returns>A valid semantic-version string.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string SemVer()
    {
        return SemanticVersionGenerator.Generate(Context, RandomValue);
    }

    /// <summary>Generates a valid semantic version with explicit optional-component choices.</summary>
    /// <param name="includePreRelease">Whether to append pre-release identifiers.</param>
    /// <param name="includeBuildMetadata">Whether to append build metadata.</param>
    /// <returns>A valid semantic-version string with the requested components.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string SemVerFromIncludePreReleaseAndIncludeBuildMetadata(bool includePreRelease, bool includeBuildMetadata)
    {
        return SemanticVersionGenerator.Generate(Context, RandomValue, includePreRelease, includeBuildMetadata);
    }

    /// <summary>Formats explicit nonnegative major, minor, and patch components.</summary>
    /// <param name="major">The major version.</param>
    /// <param name="minor">The minor version.</param>
    /// <param name="patch">The patch version.</param>
    /// <returns>A valid semantic-version core.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a component is negative or the output limit is insufficient.</exception>
    public string SemVerFromMajorAndMinorAndPatch(int major, int minor, int patch)
    {
        return SemanticVersionGenerator.Generate(Context, major, minor, patch, string.Empty, string.Empty);
    }

    /// <summary>Formats explicit core, pre-release, and build components.</summary>
    /// <param name="major">The major version.</param>
    /// <param name="minor">The minor version.</param>
    /// <param name="patch">The patch version.</param>
    /// <param name="preRelease">Dot-separated pre-release identifiers, or an empty string.</param>
    /// <param name="buildMetadata">Dot-separated build identifiers, or an empty string.</param>
    /// <returns>A valid semantic-version string containing the supplied components.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an identifier sequence is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an identifier sequence is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a component is negative or a configured limit is insufficient.</exception>
    public string SemVerFromMajorAndMinorAndPatchAndPreReleaseAndBuildMetadata(int major, int minor, int patch, string preRelease, string buildMetadata)
    {
        return SemanticVersionGenerator.Generate(Context, major, minor, patch, preRelease, buildMetadata);
    }

    /// <summary>Generates a valid dot-separated pre-release identifier sequence.</summary>
    /// <returns>Pre-release text suitable for the suffix after a semantic-version hyphen.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string SemVerPreRelease()
    {
        return SemanticVersionGenerator.PreRelease(Context, RandomValue);
    }

    /// <summary>Generates a valid dot-separated build-metadata identifier sequence.</summary>
    /// <returns>Build metadata suitable for the suffix after a semantic-version plus sign.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string SemVerBuildMetadata()
    {
        return SemanticVersionGenerator.BuildMetadata(Context, RandomValue);
    }

    /// <summary>Generates a deterministic deliberately invalid semantic-version fixture.</summary>
    /// <returns>A string containing one randomly selected Semantic Versioning defect.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string InvalidSemVer()
    {
        return SemanticVersionGenerator.Invalid(Context, RandomValue);
    }

    /// <summary>Returns a deliberately invalid semantic-version fixture for a selected defect.</summary>
    /// <param name="kind">The requested invalidity category.</param>
    /// <returns>A string containing the selected Semantic Versioning defect.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when kind is unsupported or a configured limit is insufficient.</exception>
    public string InvalidSemVerFromKind(SemVerInvalidKind kind)
    {
        return SemanticVersionGenerator.Invalid(Context, kind);
    }

    /// <summary>Reports whether text conforms to Semantic Versioning 2.0.0.</summary>
    /// <param name="value">The version text to inspect.</param>
    /// <returns><c>true</c> for valid Semantic Versioning 2.0.0 text; otherwise <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured count limit is insufficient.</exception>
    public bool IsSemVer(string value)
    {
        return SemanticVersionGenerator.Validate(Context, value);
    }

    /// <summary>Parses valid Semantic Versioning 2.0.0 text into its components.</summary>
    /// <param name="value">The semantic-version text to parse.</param>
    /// <returns>An independent component record preserving the original text.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when value is not valid Semantic Versioning 2.0.0 text.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public SemanticVersion ParseSemVer(string value)
    {
        return SemanticVersionGenerator.Parse(Context, value);
    }

    /// <summary>Compares the precedence of two valid Semantic Versioning 2.0.0 values.</summary>
    /// <remarks>Build metadata is ignored as required by Semantic Versioning 2.0.0.</remarks>
    /// <param name="left">The first valid semantic version.</param>
    /// <param name="right">The second valid semantic version.</param>
    /// <returns>A negative value, zero, or a positive value according to precedence.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when either value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when either value is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured count limit is insufficient.</exception>
    public int CompareSemVer(string left, string right)
    {
        return SemanticVersionGenerator.Compare(Context, left, right);
    }

    /// <summary>Selects a reviewed software-framework family name.</summary>
    /// <remarks>The catalog is a stable fixture and is not a claim about currently supported frameworks.</remarks>
    /// <returns>A framework-family name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string FrameworkName()
    {
        return DeveloperCompletionGenerator.FrameworkName(Context, RandomValue);
    }

    /// <summary>Selects a reviewed framework version from one framework family.</summary>
    /// <remarks>The catalog is a stable fixture and is not a current-version feed.</remarks>
    /// <returns>A canonical three-component framework-version string.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string FrameworkVersion()
    {
        return DeveloperCompletionGenerator.FrameworkVersion(Context, RandomValue);
    }

    /// <summary>Selects a reviewed version for a requested framework family.</summary>
    /// <param name="frameworkName">A case-insensitive framework-family name from the reviewed catalog.</param>
    /// <returns>A canonical version correlated with the requested framework.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when frameworkName is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when frameworkName is empty or unknown.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string FrameworkVersionFromName(string frameworkName)
    {
        return DeveloperCompletionGenerator.FrameworkVersionFromName(
            Context,
            RandomValue,
            frameworkName);
    }

    /// <summary>Generates a canonical three-component application version.</summary>
    /// <returns>Three nonnegative decimal components without leading zeroes.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    public string ApplicationVersion()
    {
        return DeveloperCompletionGenerator.ApplicationVersion(Context, RandomValue);
    }

    /// <summary>Reports whether text has the generated application-version shape.</summary>
    /// <param name="value">The text to inspect.</param>
    /// <returns><c>true</c> for exactly three canonical nonnegative decimal components; otherwise <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured count limit is insufficient.</exception>
    public bool IsApplicationVersion(string value)
    {
        return DeveloperCompletionGenerator.IsApplicationVersion(Context, value);
    }

    /// <summary>Generates correlated synthetic package metadata with a relative Unix path.</summary>
    /// <returns>A package fixture whose framework fields are mutually correlated.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public PackageMetadataRecord PackageMetadata()
    {
        return DeveloperCompletionGenerator.PackageMetadata(
            Context,
            RandomValue,
            FilePathStyle.Unix);
    }

    /// <summary>Generates correlated synthetic package metadata with requested path syntax.</summary>
    /// <param name="style">The Unix or Windows path syntax for the relative install path.</param>
    /// <returns>A package fixture whose framework fields and path style are correlated.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when style is unsupported or a configured limit is insufficient.</exception>
    public PackageMetadataRecord PackageMetadataFromPathStyle(FilePathStyle style)
    {
        return DeveloperCompletionGenerator.PackageMetadata(Context, RandomValue, style);
    }

    /// <summary>Selects one explicit line-ending variant.</summary>
    /// <returns>A line feed, carriage-return line feed, or carriage return.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    public string LineEnding()
    {
        return DeveloperCompletionGenerator.LineEnding(Context, RandomValue);
    }

    /// <summary>Returns a requested exact line-ending variant.</summary>
    /// <param name="style">The requested line-ending style.</param>
    /// <returns>The exact character sequence represented by style.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when style is unsupported or the configured output limit is insufficient.</exception>
    public string LineEndingFromStyle(LineEndingStyle style)
    {
        return DeveloperCompletionGenerator.LineEndingFromStyle(Context, style);
    }

    /// <summary>Returns one line-feed character.</summary>
    /// <returns><c>\n</c>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    public string LineFeed()
    {
        return DeveloperCompletionGenerator.LineFeed(Context);
    }

    /// <summary>Returns a carriage return followed by a line feed.</summary>
    /// <returns><c>\r\n</c>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    public string CarriageReturnLineFeed()
    {
        return DeveloperCompletionGenerator.CarriageReturnLineFeed(Context);
    }

    /// <summary>Returns one carriage-return character.</summary>
    /// <returns><c>\r</c>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    public string CarriageReturn()
    {
        return DeveloperCompletionGenerator.CarriageReturn(Context);
    }

    /// <summary>Generates a conventional five-field Unix cron expression.</summary>
    /// <returns>A cron-shaped string containing minute, hour, day, month, and weekday fields.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    public string Cron()
    {
        return DeveloperTextGenerator.Cron(Context, RandomValue);
    }

    /// <summary>Generates a selected conventional five-field Unix cron expression.</summary>
    /// <param name="kind">The requested schedule shape.</param>
    /// <returns>A cron-shaped string containing five fields.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when kind is unsupported or the configured output limit is insufficient.</exception>
    public string CronFromKind(CronExpressionKind kind)
    {
        return DeveloperTextGenerator.Cron(Context, RandomValue, kind);
    }

    /// <summary>Generates a portable environment-variable name.</summary>
    /// <returns>An uppercase name containing ASCII letters, digits, and underscores.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string EnvironmentName()
    {
        return DeveloperTextGenerator.EnvironmentName(Context, RandomValue);
    }

    /// <summary>Reports whether text has a portable environment-variable name shape.</summary>
    /// <param name="value">The text to inspect.</param>
    /// <returns><c>true</c> when the value starts with an ASCII letter or underscore and continues with ASCII letters, digits, or underscores.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured count limit is insufficient.</exception>
    public bool IsEnvironmentName(string value)
    {
        return DeveloperTextGenerator.IsEnvironmentName(Context, value);
    }

    /// <summary>Generates an inert single-token command-line argument.</summary>
    /// <remarks>The result is returned as data only; no command or process is executed.</remarks>
    /// <returns>A project-authored option token containing no shell control characters.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string CommandArgument()
    {
        return DeveloperTextGenerator.CommandArgument(Context, RandomValue);
    }

    /// <summary>Generates a synthetic single-line log entry at the captured reference time.</summary>
    /// <returns>Timestamp, severity, category, event identifier, and project-authored message text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string LogEntry()
    {
        return DeveloperTextGenerator.LogEntry(Context, RandomValue);
    }

    /// <summary>Generates a synthetic single-line log entry with requested severity.</summary>
    /// <param name="severity">The severity label to include.</param>
    /// <returns>Timestamp, requested severity, category, event identifier, and project-authored message text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when severity is unsupported or a configured limit is insufficient.</exception>
    public string LogEntryFromSeverity(LogSeverity severity)
    {
        return DeveloperTextGenerator.LogEntry(Context, RandomValue, severity);
    }

    /// <summary>Generates a project-authored synthetic exception message.</summary>
    /// <returns>A message that contains no runtime exception details.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string ExceptionMessage()
    {
        return DeveloperTextGenerator.ExceptionMessage(Context, RandomValue);
    }

    /// <summary>Generates a synthetic .NET-style stack-trace string.</summary>
    /// <remarks>The frames use fictional <c>Example</c> types and do not inspect the runtime stack.</remarks>
    /// <returns>Two through four newline-separated synthetic stack frames.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string StackTrace()
    {
        return DeveloperTextGenerator.StackTrace(Context, RandomValue);
    }

    /// <summary>Generates a synthetic .NET-style stack trace with an exact frame count.</summary>
    /// <remarks>The frames use fictional <c>Example</c> types and do not inspect the runtime stack.</remarks>
    /// <param name="frameCount">The positive number of frames.</param>
    /// <returns>The requested number of newline-separated synthetic stack frames.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when frameCount or a configured limit is invalid.</exception>
    public string StackTraceFromFrameCount(int frameCount)
    {
        return DeveloperTextGenerator.StackTrace(Context, RandomValue, frameCount);
    }

    /// <summary>Generates synthetic exception type, message, and stack-trace-shaped text.</summary>
    /// <remarks>The result is inert text using fictional frames; no exception is thrown and no runtime stack is inspected.</remarks>
    /// <returns>A synthetic exception header followed by two through four stack frames.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string ExceptionText()
    {
        return DeveloperTextGenerator.ExceptionText(Context, RandomValue);
    }
}
