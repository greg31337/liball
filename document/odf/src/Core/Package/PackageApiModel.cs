namespace odf;

internal static class PackageApiModel
{
    internal static OdfPackage CreatePackage(
        ZipPackageInspection inspection,
        ZipEntryIndex index)
    {
        OdfPackage result = new OdfPackage();
        result.EntryIndex = index;
        result.Entries = index.EnumerateEntries();
        result.Manifest = index.Manifest;
        result.Diagnostics = inspection.Diagnostics;
        result.MediaType = inspection.MediaType;
        result.ManifestMediaType = inspection.ManifestMediaType;
        result.DocumentKind = inspection.DocumentKind;
        result.UsesZip64 = inspection.UsesZip64;
        result.IsValid = inspection.IsValid;
        return result;
    }

    internal static ZipEntryIndex RequireEntryIndex(OdfPackage package)
    {
        if (package.EntryIndex == null)
        {
            throw new OdfPackageException(
                "The package was not created by an OpenDocument package reader.");
        }
        package.EntryIndex.Manifest = package.Manifest;
        return package.EntryIndex;
    }

    internal static void RefreshPackage(OdfPackage package)
    {
        ZipEntryIndex index = RequireEntryIndex(package);
        package.Entries = index.EnumerateEntries();
        package.Manifest = index.Manifest;
    }
}
