using System;
using System.Collections.Generic;
using System.IO;

namespace odf;

internal class EditablePackageEntry
{
    internal OdfPackageEntry Entry = new OdfPackageEntry();
    internal byte[] Data = [];
    internal byte[] OriginalLocalRecord = [];
    internal byte[] OriginalCentralRecord = [];
    internal bool IsContentAvailable;
    internal bool IsModified;
}

internal class ZipEntryIndex
{
    List<EditablePackageEntry> entries = new List<EditablePackageEntry>();
    internal OdfManifest? Manifest;

    internal ZipEntryIndex(byte[] packageData, ZipPackageInspection inspection)
    {
        Manifest = inspection.Manifest;
        int centralCursor = inspection.DirectoryOffset >= 0
            && inspection.DirectoryOffset <= int.MaxValue
            ? (int)inspection.DirectoryOffset
            : -1;
        int entryIndex = 0;
        while (entryIndex < inspection.Entries.Length)
        {
            OdfPackageEntry source = inspection.Entries[entryIndex];
            PackageEntryContent content = MimetypeRules.ReadEntryContent(
                packageData,
                source);
            EditablePackageEntry item = new EditablePackageEntry();
            item.Entry = CloneEntry(source);
            item.Data = CloneBytes(content.Data);
            item.OriginalLocalRecord = ReadOriginalLocalRecord(
                packageData,
                source,
                inspection);
            item.OriginalCentralRecord = ReadOriginalCentralRecord(
                packageData,
                centralCursor);
            item.IsContentAvailable = content.IsAvailable;
            entries.Add(item);
            if (item.OriginalCentralRecord.Length > 0)
            {
                centralCursor = centralCursor + item.OriginalCentralRecord.Length;
            }
            else
            {
                centralCursor = -1;
            }
            entryIndex = entryIndex + 1;
        }
    }

    internal static ZipEntryIndex CreateBytes(byte[] packageData)
    {
        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(packageData);
        return new ZipEntryIndex(packageData, inspection);
    }

    internal static ZipEntryIndex CreateStream(
        Stream input,
        OdfLoadOptions options)
    {
        if (options.LeaveInputOpen)
        {
            return CreateStreamKeepingInputOpen(input, options);
        }
        using (input)
        {
            return CreateStreamKeepingInputOpen(input, options);
        }
    }

    internal OdfPackageEntry[] EnumerateEntries()
    {
        OdfPackageEntry[] result = new OdfPackageEntry[entries.Count];
        int index = 0;
        while (index < entries.Count)
        {
            result[index] = CloneEntry(entries[index].Entry);
            index = index + 1;
        }
        return result;
    }

    internal PackagePartCatalog DiscoverParts()
    {
        return PackagePartDiscovery.DiscoverParts(EnumerateEntries(), Manifest);
    }

    internal EmbeddedPackageTraversal DiscoverEmbeddedPackages(
        OdfSecurityLimits limits)
    {
        return EmbeddedPackageWalker.WalkIndex(this, limits);
    }

    internal OdfEditResult SynchronizeManifest()
    {
        if (Manifest == null)
        {
            return CreateResult(
                OdfEditStatus.Failed,
                false,
                0,
                "The package has no parsed manifest to synchronize.",
                OdfPartNames.Manifest);
        }
        OdfSaveOptions options = new OdfSaveOptions();
        byte[] before = ManifestWriter.WriteBytes(Manifest, options);
        SynchronizeManifestState();
        byte[] after = ManifestWriter.WriteBytes(Manifest, options);
        bool changed = !BytesMatch(before, after);
        return CreateResult(
            changed ? OdfEditStatus.Success : OdfEditStatus.NoChange,
            changed,
            changed ? 1 : 0,
            changed
                ? "Synchronized the package manifest."
                : "The package manifest is already synchronized.",
            OdfPartNames.Manifest);
    }

    internal EditablePackageEntry[] GetEditableEntries()
    {
        return entries.ToArray();
    }

    internal OdfEditResult RepairMimetypeEntry(string mediaType)
    {
        OdfMediaTypeValue parsed = OdfMediaTypeRules.ParseMediaType(mediaType);
        if (!parsed.IsValid
            || !mediaType.StartsWith(
                "application/vnd.oasis.opendocument.",
                StringComparison.Ordinal))
        {
            return CreateResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "A valid OpenDocument media type is required for mimetype repair.",
                OdfPartNames.Mimetype);
        }
        byte[] data = global::System.Text.Encoding.UTF8.GetBytes(mediaType);
        EditablePackageEntry? item = FindEntry(OdfPartNames.Mimetype);
        bool changed = false;
        if (item == null)
        {
            item = new EditablePackageEntry();
            item.Entry = CreateEntry(
                OdfPartNames.Mimetype,
                data,
                OdfCompressionMethod.Stored);
            item.Data = data;
            item.IsContentAvailable = true;
            item.IsModified = true;
            entries.Insert(0, item);
            changed = true;
        }
        else
        {
            int currentIndex = FindEntryIndex(OdfPartNames.Mimetype);
            if (currentIndex != 0)
            {
                entries.RemoveAt(currentIndex);
                entries.Insert(0, item);
                changed = true;
            }
            if (!item.IsContentAvailable || !BytesMatch(item.Data, data))
            {
                item.Data = CloneBytes(data);
                item.IsContentAvailable = true;
                changed = true;
            }
            if (item.Entry.CompressionMethod != OdfCompressionMethod.Stored
                || item.Entry.CentralExtraFieldLength != 0
                || item.Entry.LocalExtraFieldLength != 0)
            {
                changed = true;
            }
            item.Entry.CompressionMethod = OdfCompressionMethod.Stored;
            item.Entry.CompressionMethodCode = 0;
            item.Entry.CentralExtraFieldLength = 0;
            item.Entry.LocalExtraFieldLength = 0;
            item.Entry.LocalHeaderOffset = -1;
            UpdateContentMetadata(item.Entry, data);
            item.IsModified = item.IsModified || changed;
        }
        RefreshFirstEntryFlags();
        SynchronizeManifestState();
        return CreateResult(
            changed ? OdfEditStatus.Success : OdfEditStatus.NoChange,
            changed,
            changed ? 1 : 0,
            changed
                ? "Repaired the package mimetype entry."
                : "The package mimetype entry already conforms.",
            OdfPartNames.Mimetype);
    }

    internal OdfEditResult RepairManifestEntry(OdfManifest manifest)
    {
        if (manifest == null)
        {
            return CreateResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "A manifest model is required for manifest repair.",
                OdfPartNames.Manifest);
        }
        Manifest = manifest;
        SynchronizeManifestState();
        OdfSaveOptions options = new OdfSaveOptions();
        byte[] data = ManifestWriter.WriteBytes(manifest, options);
        EditablePackageEntry? item = FindEntry(OdfPartNames.Manifest);
        if (item == null)
        {
            return AddEntry(
                OdfPartNames.Manifest,
                data,
                OdfCompressionMethod.Deflated);
        }
        return ReplaceEntry(OdfPartNames.Manifest, data);
    }

    internal Stream OpenEntry(string path)
    {
        EditablePackageEntry? item = FindEntry(path);
        if (item == null)
        {
            throw new OdfPackageException($"Package entry '{path}' does not exist.");
        }
        if (!item.IsContentAvailable)
        {
            throw new OdfPackageException($"Package entry '{path}' cannot be opened.");
        }
        return new MemoryStream(CloneBytes(item.Data), false);
    }

    internal OdfEditResult ExtractEntry(string path, string destinationPath)
    {
        EditablePackageEntry? item = FindEntry(path);
        if (item == null)
        {
            return CreateResult(
                OdfEditStatus.Conflict,
                false,
                0,
                $"Package entry '{path}' does not exist.",
                path);
        }
        if (!item.IsContentAvailable)
        {
            return CreateResult(
                OdfEditStatus.Failed,
                false,
                0,
                $"Package entry '{path}' cannot be extracted.",
                path);
        }
        if (item.Entry.IsDirectory)
        {
            Directory.CreateDirectory(destinationPath);
        }
        else
        {
            File.WriteAllBytes(destinationPath, item.Data);
        }
        return CreateResult(
            OdfEditStatus.Success,
            false,
            1,
            $"Extracted package entry '{path}'.",
            path);
    }

    internal OdfEditResult AddEntry(
        string path,
        byte[] data,
        OdfCompressionMethod compressionMethod)
    {
        OdfEditResult validation = ValidateNewPathWithCollision(path);
        if (validation.Status != OdfEditStatus.Success)
        {
            return validation;
        }
        if (compressionMethod != OdfCompressionMethod.Stored
            && compressionMethod != OdfCompressionMethod.Deflated)
        {
            return CreateResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "New package entries must use stored or deflated compression.",
                path);
        }
        bool isDirectory = path.EndsWith("/", StringComparison.Ordinal);
        if (isDirectory && data.Length != 0)
        {
            return CreateResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "Directory package entries cannot contain data.",
                path);
        }

        EditablePackageEntry item = new EditablePackageEntry();
        item.Entry = CreateEntry(path, data, compressionMethod);
        item.Data = CloneBytes(data);
        item.IsContentAvailable = true;
        item.IsModified = true;
        entries.Add(item);
        RefreshFirstEntryFlags();
        SynchronizeManifestState();
        return CreateResult(
            OdfEditStatus.Success,
            true,
            1,
            $"Added package entry '{path}'.",
            path);
    }

    internal OdfEditResult ReplaceEntry(string path, byte[] data)
    {
        EditablePackageEntry? item = FindEntry(path);
        if (item == null)
        {
            return CreateResult(
                OdfEditStatus.Conflict,
                false,
                0,
                $"Package entry '{path}' does not exist.",
                path);
        }
        if (item.Entry.IsDirectory && data.Length != 0)
        {
            return CreateResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "Directory package entries cannot contain data.",
                path);
        }
        if (item.IsContentAvailable && BytesMatch(item.Data, data))
        {
            return CreateResult(
                OdfEditStatus.NoChange,
                false,
                0,
                $"Package entry '{path}' already has the requested content.",
                path);
        }
        item.Data = CloneBytes(data);
        item.IsContentAvailable = true;
        item.IsModified = true;
        item.Entry.LocalHeaderOffset = -1;
        UpdateContentMetadata(item.Entry, data);
        SynchronizeManifestState();
        return CreateResult(
            OdfEditStatus.Success,
            true,
            1,
            $"Replaced package entry '{path}'.",
            path);
    }

    internal OdfEditResult RenameEntry(string path, string newName)
    {
        EditablePackageEntry? item = FindEntry(path);
        if (item == null)
        {
            return CreateResult(
                OdfEditStatus.Conflict,
                false,
                0,
                $"Package entry '{path}' does not exist.",
                path);
        }
        if (newName.Length == 0
            || newName.IndexOf('/') >= 0
            || newName.IndexOf('\\') >= 0)
        {
            return CreateResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "A package entry rename requires one canonical path segment.",
                path);
        }
        string sourceWithoutSlash = item.Entry.IsDirectory
            ? path.Substring(0, path.Length - 1)
            : path;
        int separator = sourceWithoutSlash.LastIndexOf('/');
        string directory = separator < 0
            ? ""
            : sourceWithoutSlash.Substring(0, separator + 1);
        string destination = directory + newName;
        if (item.Entry.IsDirectory)
        {
            destination = destination + "/";
        }
        return ChangeEntryPath(item, path, destination, "Renamed");
    }

    internal OdfEditResult MoveEntry(string path, string destinationPath)
    {
        EditablePackageEntry? item = FindEntry(path);
        if (item == null)
        {
            return CreateResult(
                OdfEditStatus.Conflict,
                false,
                0,
                $"Package entry '{path}' does not exist.",
                path);
        }
        bool destinationIsDirectory = destinationPath.EndsWith(
            "/",
            StringComparison.Ordinal);
        if (destinationIsDirectory != item.Entry.IsDirectory)
        {
            return CreateResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "Moving an entry cannot change whether it is a directory.",
                destinationPath);
        }
        return ChangeEntryPath(item, path, destinationPath, "Moved");
    }

    internal OdfEditResult CopyEntry(string path, string destinationPath)
    {
        EditablePackageEntry? item = FindEntry(path);
        if (item == null)
        {
            return CreateResult(
                OdfEditStatus.Conflict,
                false,
                0,
                $"Package entry '{path}' does not exist.",
                path);
        }
        bool destinationIsDirectory = destinationPath.EndsWith(
            "/",
            StringComparison.Ordinal);
        if (destinationIsDirectory != item.Entry.IsDirectory)
        {
            return CreateResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "Copying an entry cannot change whether it is a directory.",
                destinationPath);
        }
        OdfEditResult validation = ValidateNewPathWithCollision(destinationPath);
        if (validation.Status != OdfEditStatus.Success)
        {
            return validation;
        }

        EditablePackageEntry copy = new EditablePackageEntry();
        copy.Entry = CloneEntry(item.Entry);
        SetEntryPath(copy.Entry, destinationPath);
        copy.Entry.LocalHeaderOffset = -1;
        copy.Entry.IsFirstEntry = false;
        copy.Data = CloneBytes(item.Data);
        copy.IsContentAvailable = item.IsContentAvailable;
        copy.IsModified = true;
        entries.Add(copy);
        RefreshFirstEntryFlags();
        SynchronizeManifestCopiedPath(path, destinationPath);
        return CreateResult(
            OdfEditStatus.Success,
            true,
            1,
            $"Copied package entry '{path}' to '{destinationPath}'.",
            destinationPath);
    }

    internal OdfEditResult RemoveEntry(string path)
    {
        int index = FindEntryIndex(path);
        if (index < 0)
        {
            return CreateResult(
                OdfEditStatus.Conflict,
                false,
                0,
                $"Package entry '{path}' does not exist.",
                path);
        }
        entries.RemoveAt(index);
        RefreshFirstEntryFlags();
        SynchronizeManifestState();
        return CreateResult(
            OdfEditStatus.Success,
            true,
            1,
            $"Removed package entry '{path}'.",
            path);
    }

    static OdfEditResult ValidateNewPath(string path)
    {
        PackagePathValue value = PackagePathRules.CanonicalizeEntryPath(path);
        if (!value.IsValid || !value.IsCanonical)
        {
            return CreateResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                $"Package entry path '{path}' is not canonical and safe.",
                path);
        }
        return CreateResult(OdfEditStatus.Success, false, 0, "", path);
    }

    OdfEditResult ChangeEntryPath(
        EditablePackageEntry item,
        string sourcePath,
        string destinationPath,
        string operation)
    {
        if (sourcePath == destinationPath)
        {
            return CreateResult(
                OdfEditStatus.NoChange,
                false,
                0,
                $"Package entry '{sourcePath}' already has the requested path.",
                sourcePath);
        }
        OdfEditResult validation = ValidateNewPath(destinationPath);
        if (validation.Status != OdfEditStatus.Success)
        {
            return validation;
        }
        EditablePackageEntry? collision = FindEntry(destinationPath);
        if (collision != null)
        {
            return CreateResult(
                OdfEditStatus.Conflict,
                false,
                0,
                $"Package entry '{destinationPath}' already exists.",
                destinationPath);
        }
        SetEntryPath(item.Entry, destinationPath);
        item.Entry.LocalHeaderOffset = -1;
        item.IsModified = true;
        SynchronizeManifestChangedPath(sourcePath, destinationPath);
        return CreateResult(
            OdfEditStatus.Success,
            true,
            1,
            $"{operation} package entry '{sourcePath}' to '{destinationPath}'.",
            destinationPath);
    }

    OdfEditResult ValidateNewPathWithCollision(string path)
    {
        OdfEditResult validation = ValidateNewPath(path);
        if (validation.Status != OdfEditStatus.Success)
        {
            return validation;
        }
        if (FindEntry(path) != null)
        {
            return CreateResult(
                OdfEditStatus.Conflict,
                false,
                0,
                $"Package entry '{path}' already exists.",
                path);
        }
        return validation;
    }

    EditablePackageEntry? FindEntry(string path)
    {
        int index = FindEntryIndex(path);
        if (index < 0)
        {
            return null;
        }
        return entries[index];
    }

    int FindEntryIndex(string path)
    {
        int index = 0;
        while (index < entries.Count)
        {
            if (entries[index].Entry.Path == path)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    void RefreshFirstEntryFlags()
    {
        int index = 0;
        while (index < entries.Count)
        {
            entries[index].Entry.IsFirstEntry = index == 0;
            index = index + 1;
        }
    }

    void SynchronizeManifestState()
    {
        if (Manifest == null)
        {
            return;
        }
        ManifestSynchronizer.SynchronizePackageState(Manifest, entries.ToArray());
        RefreshManifestContent();
    }

    void SynchronizeManifestChangedPath(
        string sourcePath,
        string destinationPath)
    {
        if (Manifest == null)
        {
            return;
        }
        ManifestSynchronizer.SynchronizeChangedPath(
            Manifest,
            sourcePath,
            destinationPath,
            entries.ToArray());
        RefreshManifestContent();
    }

    void SynchronizeManifestCopiedPath(
        string sourcePath,
        string destinationPath)
    {
        if (Manifest == null)
        {
            return;
        }
        ManifestSynchronizer.SynchronizeCopiedPath(
            Manifest,
            sourcePath,
            destinationPath,
            entries.ToArray());
        RefreshManifestContent();
    }

    void RefreshManifestContent()
    {
        if (Manifest == null)
        {
            return;
        }
        EditablePackageEntry? item = FindEntry(OdfPartNames.Manifest);
        if (item == null)
        {
            return;
        }
        OdfSaveOptions options = new OdfSaveOptions();
        byte[] data = ManifestWriter.WriteBytes(Manifest, options);
        if (item.IsContentAvailable && BytesMatch(item.Data, data))
        {
            return;
        }
        item.Data = data;
        item.IsContentAvailable = true;
        item.IsModified = true;
        item.Entry.LocalHeaderOffset = -1;
        UpdateContentMetadata(item.Entry, data);
    }

    static OdfPackageEntry CreateEntry(
        string path,
        byte[] data,
        OdfCompressionMethod compressionMethod)
    {
        OdfPackageEntry result = new OdfPackageEntry();
        SetEntryPath(result, path);
        result.CompressionMethod = compressionMethod;
        result.CompressionMethodCode = compressionMethod == OdfCompressionMethod.Stored
            ? 0
            : 8;
        result.LocalHeaderOffset = -1;
        result.IsCanonicalPath = true;
        result.HasValidCrc = true;
        UpdateContentMetadata(result, data);
        return result;
    }

    static void SetEntryPath(OdfPackageEntry entry, string path)
    {
        entry.Path = path;
        entry.CanonicalPath = path;
        entry.IsCanonicalPath = true;
        entry.IsDirectory = path.EndsWith("/", StringComparison.Ordinal);
        entry.UsesUtf8Name = !IsAsciiPath(path);
    }

    static void UpdateContentMetadata(OdfPackageEntry entry, byte[] data)
    {
        entry.ExpandedSize = data.LongLength;
        entry.CompressedSize = entry.CompressionMethod == OdfCompressionMethod.Stored
            ? data.LongLength
            : -1;
        entry.Crc32 = ComputeCrc32(data);
        entry.HasValidCrc = true;
        if (entry.CompressionMethod != OdfCompressionMethod.Stored
            && entry.CompressionMethod != OdfCompressionMethod.Deflated)
        {
            entry.CompressionMethod = OdfCompressionMethod.Deflated;
            entry.CompressionMethodCode = 8;
        }
    }

    static uint ComputeCrc32(byte[] data)
    {
        uint crc = uint.MaxValue;
        int index = 0;
        while (index < data.Length)
        {
            crc = crc ^ data[index];
            int bit = 0;
            while (bit < 8)
            {
                crc = (crc & 1) != 0
                    ? (crc >> 1) ^ 3988292384U
                    : crc >> 1;
                bit = bit + 1;
            }
            index = index + 1;
        }
        return ~crc;
    }

    static bool IsAsciiPath(string path)
    {
        int index = 0;
        while (index < path.Length)
        {
            if (path[index] > 127)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool BytesMatch(byte[] first, byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static ZipEntryIndex CreateStreamKeepingInputOpen(
        Stream input,
        OdfLoadOptions options)
    {
        if (!input.CanRead)
        {
            throw new OdfPackageException("The package input stream is not readable.");
        }
        if (options.Cancellation.IsCancellationRequested)
        {
            throw new OdfPackageException("Package reading was cancelled.");
        }
        using (MemoryStream buffer = new MemoryStream())
        {
            input.CopyTo(buffer);
            if (options.Cancellation.IsCancellationRequested)
            {
                throw new OdfPackageException("Package reading was cancelled.");
            }
            byte[] data = buffer.ToArray();
            ZipPackageInspection inspection = ZipPackageReader.InspectBytesWithLimits(
                data,
                options.SecurityLimits,
                0);
            return new ZipEntryIndex(data, inspection);
        }
    }

    static byte[] ReadOriginalLocalRecord(
        byte[] packageData,
        OdfPackageEntry source,
        ZipPackageInspection inspection)
    {
        long start = source.LocalHeaderOffset;
        long end = inspection.DirectoryOffset;
        foreach (OdfPackageEntry candidate in inspection.Entries)
        {
            if (candidate.LocalHeaderOffset > start
                && candidate.LocalHeaderOffset < end)
            {
                end = candidate.LocalHeaderOffset;
            }
        }
        if (start < 0
            || end < start
            || end > packageData.LongLength
            || end - start > int.MaxValue)
        {
            return [];
        }
        byte[] result = new byte[(int)(end - start)];
        Array.Copy(packageData, (int)start, result, 0, result.Length);
        return result;
    }

    static byte[] ReadOriginalCentralRecord(byte[] packageData, int offset)
    {
        if (offset < 0 || offset > packageData.Length - 46)
        {
            return [];
        }
        if (ReadUInt32At(packageData, offset) != 33639248)
        {
            return [];
        }
        int length = 46
            + ReadUInt16At(packageData, offset + 28)
            + ReadUInt16At(packageData, offset + 30)
            + ReadUInt16At(packageData, offset + 32);
        if (length < 46 || offset > packageData.Length - length)
        {
            return [];
        }
        byte[] result = new byte[length];
        Array.Copy(packageData, offset, result, 0, length);
        return result;
    }

    static ushort ReadUInt16At(byte[] data, int offset)
    {
        return (ushort)(data[offset] | data[offset + 1] << 8);
    }

    static uint ReadUInt32At(byte[] data, int offset)
    {
        return (uint)(data[offset]
            | data[offset + 1] << 8
            | data[offset + 2] << 16
            | data[offset + 3] << 24);
    }

    static byte[] CloneBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    static OdfPackageEntry CloneEntry(OdfPackageEntry source)
    {
        OdfPackageEntry result = new OdfPackageEntry();
        result.Path = source.Path;
        result.CanonicalPath = source.CanonicalPath;
        result.CompressionMethod = source.CompressionMethod;
        result.CompressionMethodCode = source.CompressionMethodCode;
        result.CompressedSize = source.CompressedSize;
        result.ExpandedSize = source.ExpandedSize;
        result.Crc32 = source.Crc32;
        result.LocalHeaderOffset = source.LocalHeaderOffset;
        result.CentralExtraFieldLength = source.CentralExtraFieldLength;
        result.LocalExtraFieldLength = source.LocalExtraFieldLength;
        result.UsesUtf8Name = source.UsesUtf8Name;
        result.UsesZip64 = source.UsesZip64;
        result.UsesDataDescriptor = source.UsesDataDescriptor;
        result.IsDirectory = source.IsDirectory;
        result.IsFirstEntry = source.IsFirstEntry;
        result.IsCanonicalPath = source.IsCanonicalPath;
        result.IsEncrypted = source.IsEncrypted;
        result.HasValidCrc = source.HasValidCrc;
        return result;
    }

    static OdfEditResult CreateResult(
        OdfEditStatus status,
        bool changed,
        int affectedCount,
        string message,
        string partPath)
    {
        OdfEditResult result = new OdfEditResult();
        result.Status = status;
        result.Changed = changed;
        result.AffectedCount = affectedCount;
        result.Message = message;
        result.Location.PartPath = partPath;
        return result;
    }
}
