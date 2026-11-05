namespace odf;

internal class ZipPackageInspection
{
    internal OdfPackageEntry[] Entries = [];
    internal OdfDiagnostic[] Diagnostics = [];
    internal bool UsesZip64;
    internal long DirectoryOffset;
    internal bool IsValid = true;
    internal int NestedPackageDepth;
    internal string MediaType = "";
    internal string ManifestMediaType = "";
    internal OdfDocumentKind DocumentKind;
    internal bool HasMimetype;
    internal bool MimetypeIsValid;
    internal bool MediaTypeWasInferred;
    internal OdfManifest? Manifest;
}

internal class ZipDirectoryInfo
{
    internal long EntryCount;
    internal long DirectorySize;
    internal long DirectoryOffset;
    internal bool UsesZip64;
    internal bool IsValid;
}

internal class Zip64EntryInfo
{
    internal long CompressedSize;
    internal long ExpandedSize;
    internal long LocalHeaderOffset;
    internal bool IsValid;
}

internal class ZipCrcResult
{
    internal uint Crc32;
    internal long ExpandedSize;
    internal bool LimitExceeded;
}

internal class PackageEntryContent
{
    internal byte[] Data = [];
    internal bool IsAvailable;
}

internal class MimetypeInspection
{
    internal string MediaType = "";
    internal string ManifestMediaType = "";
    internal OdfDocumentKind DocumentKind;
    internal OdfDiagnostic[] Diagnostics = [];
    internal bool HasMimetype;
    internal bool MimetypeIsValid;
    internal bool MediaTypeWasInferred;
}

internal class ManifestIntegerValue
{
    internal long Value;
    internal bool IsValid;
}
