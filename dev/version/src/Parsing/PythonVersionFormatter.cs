namespace version;

internal static class PythonVersionFormatter
{
    internal static string FormatOriginal(PythonVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        return version.GetOriginalText();
    }

    internal static string FormatNormalized(PythonVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        return FormatCanonical(version);
    }

    internal static string FormatCanonical(PythonVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        string epoch = VersionFormatter.NormalizeNumericText(version.GetEpoch());

        if (epoch != "0")
        {
            builder.Append(epoch);
            builder.Append('!');
        }

        builder.Append(FormatReleaseSegments(version));

        if (version.HasPreRelease())
        {
            builder.Append(version.GetPreReleaseLabel());
            builder.Append(VersionFormatter.NormalizeNumericText(version.GetPreReleaseNumber()));
        }

        if (version.HasPostRelease())
        {
            builder.Append(".post");
            builder.Append(VersionFormatter.NormalizeNumericText(version.GetPostReleaseNumber()));
        }

        if (version.HasDevelopmentRelease())
        {
            builder.Append(".dev");
            builder.Append(VersionFormatter.NormalizeNumericText(version.GetDevelopmentReleaseNumber()));
        }

        if (version.HasLocalVersion())
        {
            builder.Append('+');
            builder.Append(FormatLocalSegments(version));
        }

        return builder.ToString();
    }

    internal static string FormatDisplay(PythonVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        if (version.GetOriginalText().Length > 0)
        {
            return version.GetOriginalText();
        }

        return FormatCanonical(version);
    }

    internal static string FormatReleaseSegments(PythonVersion version)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;

        while (index < version.GetReleaseSegmentCount())
        {
            if (index > 0)
            {
                builder.Append('.');
            }

            builder.Append(VersionFormatter.NormalizeNumericText(version.GetReleaseSegment(index)));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string FormatLocalSegments(PythonVersion version)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;

        while (index < version.GetLocalSegmentCount())
        {
            if (index > 0)
            {
                builder.Append('.');
            }

            builder.Append(FormatLocalSegment(version.GetLocalSegment(index)));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string FormatLocalSegment(PythonLocalVersionSegment segment)
    {
        if (segment.IsNumeric())
        {
            return VersionFormatter.NormalizeNumericText(segment.GetText());
        }

        return segment.GetText().ToLowerInvariant();
    }
}
