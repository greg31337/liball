namespace version;

internal static class NuGetVersionFormatter
{
    internal static string Normalize(string text)
    {
        VersionParseResult result = NuGetVersionParser.Parse(text);
        if (result.IsFailure())
        {
            return "";
        }

        return Normalize(result.GetVersion());
    }

    internal static string Normalize(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;

        while (index < 3)
        {
            if (index > 0)
            {
                builder.Append('.');
            }

            if (index < version.GetNumericComponentCount())
            {
                builder.Append(VersionFormatter.NormalizeNumericText(version.GetNumericComponent(index)));
            }
            else
            {
                builder.Append('0');
            }

            index = index + 1;
        }

        if (version.HasRevisionComponent() && VersionFormatter.NormalizeNumericText(version.GetRevisionComponent()) != "0")
        {
            builder.Append('.');
            builder.Append(VersionFormatter.NormalizeNumericText(version.GetRevisionComponent()));
        }

        if (version.IsPrerelease())
        {
            builder.Append('-');
            builder.Append(VersionFormatter.JoinIdentifiers(version.GetPrereleaseIdentifiers()));
        }

        return builder.ToString();
    }
}
