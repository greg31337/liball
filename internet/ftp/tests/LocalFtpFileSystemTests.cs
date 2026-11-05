using System.Text;

namespace ftp.Tests;

public class LocalFtpFileSystemTests
{
    private string RootPath = "";

    [SetUp]
    public void CreateRoot()
    {
        RootPath = Path.Combine(
            Path.GetTempPath(), "ftp-local-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
    }

    [TearDown]
    public void DeleteRoot()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, true);
        }
        string outsideRoot = RootPath + "-outside";
        if (Directory.Exists(outsideRoot))
        {
            Directory.Delete(outsideRoot, true);
        }
    }

    [Test]
    public void Constructor_requires_an_existing_directory_and_canonicalizes_root()
    {
        string filePath = Path.Combine(RootPath, "file.txt");
        File.WriteAllText(filePath, "data");
        string missingPath = Path.Combine(RootPath, "missing");
        string relativeRoot = Path.Combine(RootPath, ".");

        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(relativeRoot);

        Assert.Multiple(() =>
        {
            Assert.That(fileSystem.RootPath,
                Is.EqualTo(Path.GetFullPath(fileSystem.RootPath)));
            Assert.That(Directory.Exists(fileSystem.RootPath), Is.True);
            Assert.Throws<ArgumentNullException>(() =>
                new LocalFtpFileSystem(null!));
            Assert.Throws<FtpConfigurationException>(() =>
                new LocalFtpFileSystem(" "));
            Assert.Throws<FtpConfigurationException>(() =>
                new LocalFtpFileSystem(missingPath));
            Assert.Throws<FtpConfigurationException>(() =>
                new LocalFtpFileSystem(filePath));
        });
    }

    [Test]
    public void Interface_contract_executes_every_file_system_operation()
    {
        Directory.CreateDirectory(Path.Combine(RootPath, "listed"));
        Directory.CreateDirectory(Path.Combine(RootPath, "empty"));
        File.WriteAllText(Path.Combine(RootPath, "source.txt"), "source");
        File.WriteAllText(Path.Combine(RootPath, "delete.txt"), "delete");
        IFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);

        FtpFileSystemResult resolved = fileSystem.ResolveEntry(
            CreateRequest("/source.txt"));
        FtpFileSystemResult enumerated = fileSystem.EnumerateDirectory(
            CreateRequest("/"));
        FtpFileOpenResult read = fileSystem.OpenFileRead(
            CreateRequest("/source.txt"));
        string readContent;
        using (read.Stream)
        using (StreamReader reader = new StreamReader(read.Stream!))
        {
            readContent = reader.ReadToEnd();
        }

        FtpFileSystemRequest writeRequest = CreateRequest("/written.txt");
        writeRequest.OpenMode = FtpFileOpenMode.CreateNew;
        FtpFileOpenResult write = fileSystem.OpenFileWrite(writeRequest);
        WriteResult(write, "written");
        FtpFileSystemResult created = fileSystem.CreateDirectoryEntry(
            CreateRequest("/created"));

        FtpFileSystemRequest renameRequest = CreateRequest("/source.txt");
        renameRequest.DestinationPath = "/renamed.txt";
        FtpFileSystemResult renamed = fileSystem.RenameEntry(renameRequest);
        FtpFileSystemResult unique = fileSystem.CreateUniqueEntry(
            CreateRequest("/"));

        FtpFileSystemRequest allocationRequest =
            CreateRequest("/allocated.bin");
        allocationRequest.HasLength = true;
        allocationRequest.Length = 32;
        FtpFileSystemResult allocated =
            fileSystem.AllocateEntry(allocationRequest);

        FtpTimestamp timestamp =
            FtpTimestampCodec.ParseTimestampValue("20250304050607");
        FtpFileSystemRequest timeRequest = CreateRequest("/renamed.txt");
        timeRequest.ModificationTime = timestamp;
        FtpFileSystemResult modified =
            fileSystem.SetModificationTime(timeRequest);

        FtpFileSystemRequest permissionRequest =
            CreateRequest("/renamed.txt");
        permissionRequest.HasUnixMode = true;
        permissionRequest.UnixMode = 384;
        FtpFileSystemResult permissions =
            fileSystem.SetPermissions(permissionRequest);
        FtpFileSystemResult space = fileSystem.QuerySpace(
            CreateRequest("/future.bin"));
        FtpFileSystemResult mounted = fileSystem.MountStructure(
            CreateRequest("/listed"));
        FtpFileSystemResult deletedFile = fileSystem.DeleteFileEntry(
            CreateRequest("/delete.txt"));
        FtpFileSystemResult deletedDirectory =
            fileSystem.DeleteDirectoryEntry(CreateRequest("/empty"));

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Succeeded, Is.True);
            Assert.That(enumerated.Succeeded, Is.True);
            Assert.That(read.Succeeded, Is.True);
            Assert.That(readContent, Is.EqualTo("source"));
            Assert.That(write.Succeeded, Is.True);
            Assert.That(created.Succeeded, Is.True);
            Assert.That(renamed.Succeeded, Is.True);
            Assert.That(unique.Succeeded, Is.True);
            Assert.That(unique.Path, Does.StartWith("/ftp-"));
            Assert.That(allocated.Succeeded, Is.True);
            Assert.That(allocated.AllocatedBytes, Is.EqualTo(32));
            Assert.That(modified.Succeeded, Is.True);
            Assert.That(permissions.Succeeded,
                Is.EqualTo(!OperatingSystem.IsWindows()));
            Assert.That(space.Succeeded, Is.True);
            Assert.That(mounted.Succeeded, Is.True);
            Assert.That(deletedFile.Succeeded, Is.True);
            Assert.That(deletedDirectory.Succeeded, Is.True);
        });
    }

    [Test]
    public void Competing_create_rename_delete_and_unique_operations_are_safe()
    {
        const int operationCount = 16;
        File.WriteAllText(Path.Combine(RootPath, "rename.txt"), "rename");
        File.WriteAllText(Path.Combine(RootPath, "delete.txt"), "delete");
        IFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);

        Task<FtpFileOpenResult>[] createTasks = Enumerable.Range(
            0, operationCount).Select(index => Task.Run(() =>
            {
                FtpFileSystemRequest request = CreateRequest("/created.txt");
                request.OpenMode = FtpFileOpenMode.CreateNew;
                return fileSystem.OpenFileWrite(request);
            })).ToArray();
        Task.WaitAll(createTasks);
        FtpFileOpenResult[] createResults = createTasks.Select(task =>
            task.Result).ToArray();
        foreach (FtpFileOpenResult result in createResults)
        {
            result.Stream?.Dispose();
        }

        Task<FtpFileSystemResult>[] renameTasks = Enumerable.Range(
            0, operationCount).Select(index => Task.Run(() =>
            {
                FtpFileSystemRequest request = CreateRequest("/rename.txt");
                request.DestinationPath = "/renamed-" + index + ".txt";
                return fileSystem.RenameEntry(request);
            })).ToArray();
        Task.WaitAll(renameTasks);

        Task<FtpFileSystemResult>[] deleteTasks = Enumerable.Range(
            0, operationCount).Select(index => Task.Run(() =>
                fileSystem.DeleteFileEntry(
                    CreateRequest("/delete.txt")))).ToArray();
        Task.WaitAll(deleteTasks);

        Task<FtpFileSystemResult>[] uniqueTasks = Enumerable.Range(
            0, operationCount).Select(index => Task.Run(() =>
                fileSystem.CreateUniqueEntry(
                    CreateRequest("/")))).ToArray();
        Task.WaitAll(uniqueTasks);
        FtpFileSystemResult[] uniqueResults = uniqueTasks.Select(task =>
            task.Result).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(createResults.Count(result => result.Succeeded),
                Is.EqualTo(1));
            Assert.That(File.Exists(
                Path.Combine(RootPath, "created.txt")), Is.True);
            Assert.That(renameTasks.Count(task => task.Result.Succeeded),
                Is.EqualTo(1));
            Assert.That(Directory.GetFiles(RootPath, "renamed-*.txt"),
                Has.Length.EqualTo(1));
            Assert.That(deleteTasks.Count(task => task.Result.Succeeded),
                Is.GreaterThanOrEqualTo(1));
            Assert.That(deleteTasks, Has.All.Matches<Task<FtpFileSystemResult>>(
                task => task.IsCompletedSuccessfully));
            Assert.That(File.Exists(
                Path.Combine(RootPath, "delete.txt")), Is.False);
            Assert.That(uniqueResults, Has.All.Matches<FtpFileSystemResult>(
                result => result.Succeeded));
            Assert.That(uniqueResults.Select(result => result.Path).Distinct()
                .ToArray(), Has.Length.EqualTo(operationCount));
            Assert.That(uniqueResults.All(result => File.Exists(Path.Combine(
                RootPath, result.Path!.Substring(1)))), Is.True);
        });
    }

    [Test]
    public void Portable_and_platform_specific_names_follow_host_rules()
    {
        IFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);
        FtpFileSystemRequest portableRequest =
            CreateRequest("/report 2025-東京.txt");
        portableRequest.OpenMode = FtpFileOpenMode.CreateNew;
        FtpFileOpenResult portable =
            fileSystem.OpenFileWrite(portableRequest);
        WriteResult(portable, "portable");

        FtpFileSystemRequest deviceNameRequest = CreateRequest("/CON");
        deviceNameRequest.OpenMode = FtpFileOpenMode.CreateNew;
        FtpFileOpenResult deviceName =
            fileSystem.OpenFileWrite(deviceNameRequest);
        deviceName.Stream?.Dispose();
        FtpFileSystemResult windowsPath = fileSystem.ResolveEntry(
            CreateRequest("/C:/Windows/system.ini"));
        FtpFileSystemResult windowsSeparators = fileSystem.ResolveEntry(
            CreateRequest("/folder\\file.txt"));
        FtpFileSystemResult unixTraversal = fileSystem.ResolveEntry(
            CreateRequest("/folder/../file.txt"));

        Assert.Multiple(() =>
        {
            Assert.That(fileSystem.ResolveEntry(
                CreateRequest("/report 2025-東京.txt")).Succeeded,
                Is.True);
            Assert.That(deviceName.Succeeded,
                Is.EqualTo(!OperatingSystem.IsWindows()));
            Assert.That(windowsPath.Succeeded, Is.False);
            Assert.That(windowsSeparators.Succeeded, Is.False);
            Assert.That(unixTraversal.Succeeded, Is.False);
        });
    }

    [Test]
    public void Resolve_and_enumerate_return_local_entries_and_expected_failures()
    {
        Directory.CreateDirectory(Path.Combine(RootPath, "folder"));
        File.WriteAllText(Path.Combine(RootPath, "zeta.txt"), "12345");
        File.WriteAllText(Path.Combine(RootPath, "alpha.txt"), "a");
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);

        FtpFileSystemResult root = fileSystem.ResolveEntry(CreateRequest("/"));
        FtpFileSystemResult file = fileSystem.ResolveEntry(
            CreateRequest("/zeta.txt"));
        FtpFileSystemResult listing = fileSystem.EnumerateDirectory(
            CreateRequest("/"));
        FtpFileSystemResult missing = fileSystem.ResolveEntry(
            CreateRequest("/missing"));
        FtpFileSystemResult notDirectory = fileSystem.EnumerateDirectory(
            CreateRequest("/zeta.txt"));

        Assert.Multiple(() =>
        {
            Assert.That(root.Succeeded, Is.True);
            Assert.That(root.Entry!.Type, Is.EqualTo(FtpEntryType.Directory));
            Assert.That(root.Entry.Name, Is.EqualTo("/"));
            Assert.That(file.Succeeded, Is.True);
            Assert.That(file.Entry!.Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(file.Entry.HasSize, Is.True);
            Assert.That(file.Entry.Size, Is.EqualTo(5));
            Assert.That(file.Entry.FullPath, Is.EqualTo("/zeta.txt"));
            Assert.That(listing.Succeeded, Is.True);
            Assert.That(listing.Entries.Select(entry => entry.Name),
                Is.EqualTo(new[] { "alpha.txt", "folder", "zeta.txt" }));
            Assert.That(listing.Entries.Select(entry => entry.FullPath),
                Is.EqualTo(new[] { "/alpha.txt", "/folder", "/zeta.txt" }));
            Assert.That(missing.Succeeded, Is.False);
            Assert.That(missing.FailureMessage, Does.Contain("does not exist"));
            Assert.That(notDirectory.Succeeded, Is.False);
        });
    }

    [Test]
    public void Read_open_supports_valid_offsets_and_contains_failures()
    {
        File.WriteAllText(Path.Combine(RootPath, "read.txt"), "abcdef");
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);
        FtpFileSystemRequest request = CreateRequest("/read.txt");
        request.OpenMode = FtpFileOpenMode.Read;
        request.HasOffset = true;
        request.Offset = 2;

        FtpFileOpenResult opened = fileSystem.OpenFileRead(request);
        string remainder;
        using (opened.Stream)
        using (StreamReader reader = new StreamReader(
            opened.Stream!, Encoding.UTF8, false, 1024, true))
        {
            remainder = reader.ReadToEnd();
        }
        FtpFileSystemRequest beyond = CreateRequest("/read.txt");
        beyond.HasOffset = true;
        beyond.Offset = 100;
        FtpFileOpenResult badOffset = fileSystem.OpenFileRead(beyond);
        FtpFileOpenResult missing = fileSystem.OpenFileRead(
            CreateRequest("/missing.txt"));
        FtpFileSystemRequest wrongMode = CreateRequest("/read.txt");
        wrongMode.OpenMode = FtpFileOpenMode.Append;
        FtpFileOpenResult invalidMode = fileSystem.OpenFileRead(wrongMode);

        Assert.Multiple(() =>
        {
            Assert.That(opened.Succeeded, Is.True);
            Assert.That(opened.LeaveStreamOpen, Is.False);
            Assert.That(opened.HasLength, Is.True);
            Assert.That(opened.Length, Is.EqualTo(6));
            Assert.That(remainder, Is.EqualTo("cdef"));
            Assert.That(badOffset.Succeeded, Is.False);
            Assert.That(badOffset.Stream, Is.Null);
            Assert.That(missing.Succeeded, Is.False);
            Assert.That(invalidMode.Succeeded, Is.False);
        });
    }

    [Test]
    public void Write_open_supports_create_overwrite_append_and_resume_modes()
    {
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);
        FtpFileSystemRequest create = CreateRequest("/write.txt");
        create.OpenMode = FtpFileOpenMode.CreateNew;
        WriteResult(fileSystem.OpenFileWrite(create), "abc");

        FtpFileOpenResult duplicate = fileSystem.OpenFileWrite(create);
        FtpFileSystemRequest append = CreateRequest("/write.txt");
        append.OpenMode = FtpFileOpenMode.Append;
        FtpFileOpenResult appendResult = fileSystem.OpenFileWrite(append);
        long appendPosition = appendResult.Stream!.Position;
        WriteResult(appendResult, "def");

        FtpFileSystemRequest resume = CreateRequest("/write.txt");
        resume.OpenMode = FtpFileOpenMode.Resume;
        resume.HasOffset = true;
        resume.Offset = 3;
        FtpFileOpenResult resumeResult = fileSystem.OpenFileWrite(resume);
        long resumePosition = resumeResult.Stream!.Position;
        WriteResult(resumeResult, "XYZ");

        FtpFileSystemRequest overwrite = CreateRequest("/write.txt");
        overwrite.OpenMode = FtpFileOpenMode.Overwrite;
        FtpFileOpenResult overwriteResult = fileSystem.OpenFileWrite(overwrite);
        long overwriteLength = overwriteResult.Stream!.Length;
        WriteResult(overwriteResult, "new");

        FtpFileSystemRequest missingOffset = CreateRequest("/write.txt");
        missingOffset.OpenMode = FtpFileOpenMode.Resume;
        FtpFileOpenResult invalidResume =
            fileSystem.OpenFileWrite(missingOffset);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Succeeded, Is.False);
            Assert.That(duplicate.Stream, Is.Null);
            Assert.That(appendPosition, Is.EqualTo(3));
            Assert.That(resumePosition, Is.EqualTo(3));
            Assert.That(overwriteLength, Is.Zero);
            Assert.That(File.ReadAllText(
                Path.Combine(RootPath, "write.txt")), Is.EqualTo("new"));
            Assert.That(invalidResume.Succeeded, Is.False);
        });
    }

    [Test]
    public void File_open_rejects_invalid_shapes_without_mutating_content()
    {
        string localPath = Path.Combine(RootPath, "safe.txt");
        File.WriteAllText(localPath, "abcdef");
        Directory.CreateDirectory(Path.Combine(RootPath, "folder"));
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);

        FtpFileSystemRequest append = CreateRequest("/safe.txt");
        append.OpenMode = FtpFileOpenMode.Append;
        FtpFileOpenResult appendResult = fileSystem.OpenFileWrite(append);
        Assert.That(appendResult.Succeeded, Is.True);
        Assert.That(appendResult.Stream, Is.Not.Null);
        using (appendResult.Stream)
        {
            Assert.Throws<IOException>(() =>
                appendResult.Stream!.Seek(0, SeekOrigin.Begin));
        }

        FtpFileSystemRequest beyond = CreateRequest("/safe.txt");
        beyond.OpenMode = FtpFileOpenMode.Resume;
        beyond.HasOffset = true;
        beyond.Offset = 100;
        FtpFileOpenResult badResume = fileSystem.OpenFileWrite(beyond);
        FtpFileSystemRequest missingParent =
            CreateRequest("/missing/file.txt");
        missingParent.OpenMode = FtpFileOpenMode.Overwrite;
        FtpFileOpenResult noParent =
            fileSystem.OpenFileWrite(missingParent);
        FtpFileSystemRequest directory = CreateRequest("/folder");
        directory.OpenMode = FtpFileOpenMode.Overwrite;
        FtpFileOpenResult directoryWrite =
            fileSystem.OpenFileWrite(directory);
        FtpFileSystemRequest badMode = CreateRequest("/safe.txt");
        badMode.OpenMode = (FtpFileOpenMode)999;
        FtpFileOpenResult invalidMode = fileSystem.OpenFileWrite(badMode);
        FtpFileSystemRequest badStructure = CreateRequest("/safe.txt");
        badStructure.Structure = (FtpFileStructure)999;
        FtpFileOpenResult invalidStructure =
            fileSystem.OpenFileRead(badStructure);

        Assert.Multiple(() =>
        {
            Assert.That(badResume.Succeeded, Is.False);
            Assert.That(noParent.Succeeded, Is.False);
            Assert.That(Directory.Exists(
                Path.Combine(RootPath, "missing")), Is.False);
            Assert.That(directoryWrite.Succeeded, Is.False);
            Assert.That(invalidMode.Succeeded, Is.False);
            Assert.That(invalidStructure.Succeeded, Is.False);
            Assert.That(File.ReadAllText(localPath), Is.EqualTo("abcdef"));
        });
    }

    [Test]
    public void Directory_file_and_rename_mutations_report_results_without_exceptions()
    {
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);
        FtpFileSystemResult created = fileSystem.CreateDirectoryEntry(
            CreateRequest("/source"));
        File.WriteAllText(Path.Combine(RootPath, "source", "item.txt"), "x");
        FtpFileSystemRequest rename = CreateRequest("/source/item.txt");
        rename.DestinationPath = "/source/renamed.txt";
        FtpFileSystemResult renamed = fileSystem.RenameEntry(rename);
        FtpFileSystemResult nonEmpty = fileSystem.DeleteDirectoryEntry(
            CreateRequest("/source"));
        FtpFileSystemResult deletedFile = fileSystem.DeleteFileEntry(
            CreateRequest("/source/renamed.txt"));
        FtpFileSystemResult deletedDirectory =
            fileSystem.DeleteDirectoryEntry(CreateRequest("/source"));
        FtpFileSystemResult deleteRoot = fileSystem.DeleteDirectoryEntry(
            CreateRequest("/"));
        FtpFileSystemResult missingFile = fileSystem.DeleteFileEntry(
            CreateRequest("/missing"));

        Assert.Multiple(() =>
        {
            Assert.That(created.Succeeded, Is.True);
            Assert.That(created.Entry!.Type,
                Is.EqualTo(FtpEntryType.Directory));
            Assert.That(renamed.Succeeded, Is.True);
            Assert.That(renamed.Path, Is.EqualTo("/source/renamed.txt"));
            Assert.That(nonEmpty.Succeeded, Is.False);
            Assert.That(deletedFile.Succeeded, Is.True);
            Assert.That(deletedDirectory.Succeeded, Is.True);
            Assert.That(deleteRoot.Succeeded, Is.False);
            Assert.That(missingFile.Succeeded, Is.False);
            Assert.That(Directory.Exists(RootPath), Is.True);
        });
    }

    [Test]
    public void Directory_and_rename_mutations_require_safe_entry_boundaries()
    {
        string sourcePath = Path.Combine(RootPath, "source");
        Directory.CreateDirectory(sourcePath);
        File.WriteAllText(Path.Combine(sourcePath, "source.txt"), "source");
        File.WriteAllText(Path.Combine(sourcePath, "existing.txt"), "existing");
        Directory.CreateDirectory(Path.Combine(sourcePath, "child"));
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);

        FtpFileSystemResult nestedCreate = fileSystem.CreateDirectoryEntry(
            CreateRequest("/missing/nested"));
        FtpFileSystemRequest overwrite =
            CreateRequest("/source/source.txt");
        overwrite.DestinationPath = "/source/existing.txt";
        FtpFileSystemResult existingDestination =
            fileSystem.RenameEntry(overwrite);
        FtpFileSystemRequest noParent =
            CreateRequest("/source/source.txt");
        noParent.DestinationPath = "/missing/moved.txt";
        FtpFileSystemResult missingDestinationParent =
            fileSystem.RenameEntry(noParent);
        FtpFileSystemRequest beneathItself = CreateRequest("/source");
        beneathItself.DestinationPath = "/source/child/moved";
        FtpFileSystemResult recursiveMove =
            fileSystem.RenameEntry(beneathItself);
        FtpFileSystemResult fileAsDirectory =
            fileSystem.DeleteDirectoryEntry(
                CreateRequest("/source/source.txt"));
        FtpFileSystemResult directoryAsFile = fileSystem.DeleteFileEntry(
            CreateRequest("/source/child"));

        Assert.Multiple(() =>
        {
            Assert.That(nestedCreate.Succeeded, Is.False);
            Assert.That(Directory.Exists(
                Path.Combine(RootPath, "missing")), Is.False);
            Assert.That(existingDestination.Succeeded, Is.False);
            Assert.That(missingDestinationParent.Succeeded, Is.False);
            Assert.That(recursiveMove.Succeeded, Is.False);
            Assert.That(fileAsDirectory.Succeeded, Is.False);
            Assert.That(directoryAsFile.Succeeded, Is.False);
            Assert.That(File.ReadAllText(
                Path.Combine(sourcePath, "source.txt")),
                Is.EqualTo("source"));
            Assert.That(File.ReadAllText(
                Path.Combine(sourcePath, "existing.txt")),
                Is.EqualTo("existing"));
        });
    }

    [Test]
    public void Unique_allocation_space_and_mount_operations_use_local_io()
    {
        Directory.CreateDirectory(Path.Combine(RootPath, "area"));
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);
        FtpFileSystemRequest uniqueRequest = CreateRequest("/area");
        uniqueRequest.UniquePrefix = "upload-";

        FtpFileSystemResult unique = fileSystem.CreateUniqueEntry(uniqueRequest);
        FtpFileSystemRequest allocation = CreateRequest("/allocated.bin");
        allocation.HasLength = true;
        allocation.Length = 4096;
        FtpFileSystemResult allocated = fileSystem.AllocateEntry(allocation);
        FtpFileSystemResult space = fileSystem.QuerySpace(
            CreateRequest("/not-created-yet"));
        FtpFileSystemRequest mountRequest = CreateRequest("/area");
        mountRequest.Structure = FtpFileStructure.Record;
        FtpFileSystemResult mounted = fileSystem.MountStructure(mountRequest);
        FtpFileSystemRequest invalidAllocation = CreateRequest("/bad.bin");
        FtpFileSystemResult badAllocation =
            fileSystem.AllocateEntry(invalidAllocation);

        Assert.Multiple(() =>
        {
            Assert.That(unique.Succeeded, Is.True);
            Assert.That(unique.Path, Does.StartWith("/area/upload-"));
            Assert.That(unique.Entry!.Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(File.Exists(Path.Combine(
                RootPath, unique.Path!.Substring(1))), Is.True);
            Assert.That(allocated.Succeeded, Is.True);
            Assert.That(allocated.HasAllocatedBytes, Is.True);
            Assert.That(allocated.AllocatedBytes, Is.EqualTo(4096));
            Assert.That(new FileInfo(
                Path.Combine(RootPath, "allocated.bin")).Length,
                Is.EqualTo(4096));
            Assert.That(space.Succeeded, Is.True);
            Assert.That(space.Space!.HasTotalBytes, Is.True);
            Assert.That(space.Space.HasAvailableBytes, Is.True);
            Assert.That(space.Space.HasUsedBytes, Is.True);
            Assert.That(space.Space.TotalBytes, Is.GreaterThan(0));
            Assert.That(mounted.Succeeded, Is.True);
            Assert.That(mounted.Entry!.Type,
                Is.EqualTo(FtpEntryType.Directory));
            Assert.That(badAllocation.Succeeded, Is.False);
        });
    }

    [Test]
    public void Unique_names_are_random_exclusive_files_with_validated_prefixes()
    {
        Directory.CreateDirectory(Path.Combine(RootPath, "area"));
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);
        FtpFileSystemRequest request = CreateRequest("/area");
        request.UniquePrefix = "upload-";

        FtpFileSystemResult first = fileSystem.CreateUniqueEntry(request);
        FtpFileSystemResult second = fileSystem.CreateUniqueEntry(request);
        FtpFileSystemRequest invalidRequest = CreateRequest("/area");
        invalidRequest.UniquePrefix = "../escape";
        FtpFileSystemResult invalid =
            fileSystem.CreateUniqueEntry(invalidRequest);

        Assert.Multiple(() =>
        {
            Assert.That(first.Succeeded, Is.True);
            Assert.That(second.Succeeded, Is.True);
            Assert.That(first.Path,
                Does.Match("^/area/upload-[0-9a-f]{32}$"));
            Assert.That(second.Path,
                Does.Match("^/area/upload-[0-9a-f]{32}$"));
            Assert.That(second.Path, Is.Not.EqualTo(first.Path));
            Assert.That(new FileInfo(Path.Combine(
                RootPath, first.Path!.Substring(1))).Length, Is.Zero);
            Assert.That(new FileInfo(Path.Combine(
                RootPath, second.Path!.Substring(1))).Length, Is.Zero);
            Assert.That(invalid.Succeeded, Is.False);
        });
    }

    [Test]
    public void Allocation_preserves_content_and_only_extends_existing_files()
    {
        string localPath = Path.Combine(RootPath, "allocated.bin");
        byte[] original = new byte[] { 1, 2, 3, 4 };
        File.WriteAllBytes(localPath, original);
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);
        FtpFileSystemRequest smaller = CreateRequest("/allocated.bin");
        smaller.HasLength = true;
        smaller.Length = 2;

        FtpFileSystemResult preserved = fileSystem.AllocateEntry(smaller);
        FtpFileSystemRequest larger = CreateRequest("/allocated.bin");
        larger.HasLength = true;
        larger.Length = 8;
        FtpFileSystemResult extended = fileSystem.AllocateEntry(larger);
        FtpFileSystemRequest missingParent =
            CreateRequest("/missing/allocated.bin");
        missingParent.HasLength = true;
        missingParent.Length = 8;
        FtpFileSystemResult invalid =
            fileSystem.AllocateEntry(missingParent);
        byte[] content = File.ReadAllBytes(localPath);

        Assert.Multiple(() =>
        {
            Assert.That(preserved.Succeeded, Is.True);
            Assert.That(preserved.AllocatedBytes, Is.EqualTo(4));
            Assert.That(extended.Succeeded, Is.True);
            Assert.That(extended.AllocatedBytes, Is.EqualTo(8));
            Assert.That(content, Has.Length.EqualTo(8));
            Assert.That(content.Take(4), Is.EqualTo(original));
            Assert.That(content.Skip(4), Is.All.Zero);
            Assert.That(invalid.Succeeded, Is.False);
            Assert.That(Directory.Exists(
                Path.Combine(RootPath, "missing")), Is.False);
        });
    }

    [Test]
    public void Modification_time_mutation_updates_files_and_directories()
    {
        string filePath = Path.Combine(RootPath, "file.txt");
        string directoryPath = Path.Combine(RootPath, "folder");
        File.WriteAllText(filePath, "content");
        Directory.CreateDirectory(directoryPath);
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);
        FtpTimestamp timestamp =
            FtpTimestampCodec.ParseTimestampValue("20240229123456");
        FtpFileSystemRequest fileRequest = CreateRequest("/file.txt");
        fileRequest.ModificationTime = timestamp;
        FtpFileSystemRequest directoryRequest = CreateRequest("/folder");
        directoryRequest.ModificationTime = timestamp;
        FtpFileSystemRequest missingRequest = CreateRequest("/missing.txt");
        missingRequest.ModificationTime = timestamp;

        FtpFileSystemResult file =
            fileSystem.SetModificationTime(fileRequest);
        FtpFileSystemResult directory =
            fileSystem.SetModificationTime(directoryRequest);
        FtpFileSystemResult missing =
            fileSystem.SetModificationTime(missingRequest);
        FtpFileSystemResult invalid = fileSystem.SetModificationTime(
            CreateRequest("/file.txt"));

        Assert.Multiple(() =>
        {
            Assert.That(file.Succeeded, Is.True);
            Assert.That(file.Entry!.Modified.ValueUtc,
                Is.EqualTo(timestamp.ValueUtc));
            Assert.That(File.GetLastWriteTimeUtc(filePath),
                Is.EqualTo(timestamp.ValueUtc));
            Assert.That(directory.Succeeded, Is.True);
            Assert.That(directory.Entry!.Modified.ValueUtc,
                Is.EqualTo(timestamp.ValueUtc));
            Assert.That(Directory.GetLastWriteTimeUtc(directoryPath),
                Is.EqualTo(timestamp.ValueUtc));
            Assert.That(missing.Succeeded, Is.False);
            Assert.That(invalid.Succeeded, Is.False);
        });
    }

    [Test]
    public void Space_and_mount_results_report_consistent_available_state()
    {
        string directoryPath = Path.Combine(RootPath, "area");
        Directory.CreateDirectory(directoryPath);
        File.WriteAllText(Path.Combine(RootPath, "file.txt"), "content");
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);

        FtpFileSystemResult space = fileSystem.QuerySpace(
            CreateRequest("/future/file.bin"));
        FtpFileSystemRequest mountRequest = CreateRequest("/area");
        mountRequest.Structure = FtpFileStructure.Page;
        FtpFileSystemResult mounted =
            fileSystem.MountStructure(mountRequest);
        FtpFileSystemResult fileMount = fileSystem.MountStructure(
            CreateRequest("/file.txt"));
        FtpFileSystemRequest invalidRequest = CreateRequest("/area");
        invalidRequest.Structure = (FtpFileStructure)999;
        FtpFileSystemResult invalidMount =
            fileSystem.MountStructure(invalidRequest);

        Assert.Multiple(() =>
        {
            Assert.That(space.Succeeded, Is.True);
            Assert.That(space.Space, Is.Not.Null);
            Assert.That(space.Space!.HasTotalBytes, Is.True);
            Assert.That(space.Space.HasAvailableBytes, Is.True);
            Assert.That(space.Space.HasUsedBytes, Is.True);
            Assert.That(space.Space.TotalBytes,
                Is.EqualTo(space.Space.AvailableBytes +
                    space.Space.UsedBytes));
            Assert.That(space.Space.QuotaName, Is.Null);
            Assert.That(mounted.Succeeded, Is.True);
            Assert.That(mounted.Path, Is.EqualTo("/area"));
            Assert.That(mounted.Entry!.Type,
                Is.EqualTo(FtpEntryType.Directory));
            Assert.That(fileMount.Succeeded, Is.False);
            Assert.That(invalidMount.Succeeded, Is.False);
        });
    }

    [Test]
    public void Space_query_reports_home_scoped_per_user_storage_quota()
    {
        string home = Path.Combine(RootPath, "users", "alice");
        Directory.CreateDirectory(home);
        File.WriteAllText(Path.Combine(home, "owned.txt"), "1234567");
        File.WriteAllText(Path.Combine(RootPath, "outside.txt"), "outside");
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);
        FtpFileSystemRequest request = CreateRequest("/users/alice/new.bin");
        request.Principal.Name = "alice";
        request.Principal.HomeDirectory = "/users/alice";
        request.Principal.ResourceLimits.HasStorageQuotaBytes = true;
        request.Principal.ResourceLimits.StorageQuotaBytes = 10;

        FtpFileSystemResult result = fileSystem.QuerySpace(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Space, Is.Not.Null);
            Assert.That(result.Space!.TotalBytes, Is.EqualTo(10));
            Assert.That(result.Space.UsedBytes, Is.EqualTo(7));
            Assert.That(result.Space.AvailableBytes, Is.EqualTo(3));
            Assert.That(result.Space.QuotaName, Is.EqualTo("alice"));
        });
    }

    [Test]
    public void Space_query_clamps_exhausted_quota_and_ignores_linked_storage()
    {
        string home = Path.Combine(RootPath, "users", "alice");
        string outsideRoot = RootPath + "-outside";
        Directory.CreateDirectory(home);
        Directory.CreateDirectory(outsideRoot);
        File.WriteAllText(Path.Combine(home, "owned.txt"), "123456789012");
        File.WriteAllText(
            Path.Combine(outsideRoot, "external.txt"),
            new string('x', 100));
        CreateDirectorySymbolicLink(
            Path.Combine(home, "external-link"), outsideRoot);
        IFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);
        FtpFileSystemRequest request = CreateRequest("/users/alice/new.bin");
        request.Principal.Name = "alice";
        request.Principal.HomeDirectory = "/users/alice";
        request.Principal.ResourceLimits.HasStorageQuotaBytes = true;
        request.Principal.ResourceLimits.StorageQuotaBytes = 10;

        FtpFileSystemResult result = fileSystem.QuerySpace(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Space, Is.Not.Null);
            Assert.That(result.Space!.TotalBytes, Is.EqualTo(10));
            Assert.That(result.Space.UsedBytes, Is.EqualTo(12));
            Assert.That(result.Space.AvailableBytes, Is.Zero);
            Assert.That(result.Space.QuotaName, Is.EqualTo("alice"));
        });
    }

    [Test]
    public void Invalid_and_outside_paths_fail_without_touching_external_entries()
    {
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);
        string outsideName = "outside-" + Guid.NewGuid().ToString("N") + ".txt";
        string outsidePath = Path.Combine(
            Directory.GetParent(RootPath)!.FullName, outsideName);
        FtpFileSystemRequest traversal = CreateRequest("/../" + outsideName);
        traversal.OpenMode = FtpFileOpenMode.Overwrite;

        FtpFileOpenResult outside = fileSystem.OpenFileWrite(traversal);
        FtpFileSystemResult relative = fileSystem.ResolveEntry(
            CreateRequest("relative/path"));
        FtpFileSystemResult nullRequest = fileSystem.ResolveEntry(null!);

        Assert.Multiple(() =>
        {
            Assert.That(outside.Succeeded, Is.False);
            Assert.That(outside.Stream, Is.Null);
            Assert.That(File.Exists(outsidePath), Is.False);
            Assert.That(relative.Succeeded, Is.False);
            Assert.That(nullRequest.Succeeded, Is.False);
        });
    }

    [TestCase("relative/path")]
    [TestCase("//server/share")]
    [TestCase("/folder/")]
    [TestCase("/folder//file")]
    [TestCase("/folder/./file")]
    [TestCase("/folder/../file")]
    [TestCase("/folder\\file")]
    [TestCase("/C:/Windows/file")]
    [TestCase("/\\\\?\\C:\\Windows\\file")]
    public void Noncanonical_separator_dot_and_device_paths_are_rejected(
        string path)
    {
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);
        FtpFileSystemRequest writeRequest = CreateRequest(path);
        writeRequest.OpenMode = FtpFileOpenMode.Overwrite;
        FtpFileSystemRequest renameRequest = CreateRequest("/safe.txt");
        File.WriteAllText(Path.Combine(RootPath, "safe.txt"), "safe");
        renameRequest.DestinationPath = path;

        FtpFileSystemResult resolved = fileSystem.ResolveEntry(
            CreateRequest(path));
        FtpFileOpenResult opened = fileSystem.OpenFileWrite(writeRequest);
        FtpFileSystemResult renamed = fileSystem.RenameEntry(renameRequest);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Succeeded, Is.False);
            Assert.That(opened.Succeeded, Is.False);
            Assert.That(opened.Stream, Is.Null);
            Assert.That(renamed.Succeeded, Is.False);
            Assert.That(File.ReadAllText(
                Path.Combine(RootPath, "safe.txt")), Is.EqualTo("safe"));
        });
    }

    [Test]
    public void Symlinked_root_is_canonicalized_before_serving_files()
    {
        string physicalRoot = Path.Combine(RootPath, "physical");
        string linkedRoot = Path.Combine(RootPath, "linked-root");
        Directory.CreateDirectory(physicalRoot);
        CreateDirectorySymbolicLink(linkedRoot, physicalRoot);
        LocalFtpFileSystem physicalFileSystem =
            new LocalFtpFileSystem(physicalRoot);
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(linkedRoot);
        FtpFileSystemRequest request = CreateRequest("/created.txt");
        request.OpenMode = FtpFileOpenMode.CreateNew;

        WriteResult(fileSystem.OpenFileWrite(request), "content");

        Assert.Multiple(() =>
        {
            Assert.That(fileSystem.RootPath,
                Is.EqualTo(physicalFileSystem.RootPath));
            Assert.That(File.ReadAllText(
                Path.Combine(physicalRoot, "created.txt")),
                Is.EqualTo("content"));
        });
    }

    [Test]
    public void Symlink_and_reparse_traversal_is_denied_for_every_operation_shape()
    {
        string outsideRoot = RootPath + "-outside";
        Directory.CreateDirectory(outsideRoot);
        string outsideSecret = Path.Combine(outsideRoot, "secret.txt");
        File.WriteAllText(outsideSecret, "outside-secret");
        string insideFile = Path.Combine(RootPath, "inside.txt");
        File.WriteAllText(insideFile, "inside");
        string directoryLink = Path.Combine(RootPath, "outside-link");
        string fileLink = Path.Combine(RootPath, "file-link");
        string insideLink = Path.Combine(RootPath, "inside-link");
        CreateDirectorySymbolicLink(directoryLink, outsideRoot);
        CreateFileSymbolicLink(fileLink, outsideSecret);
        CreateFileSymbolicLink(insideLink, insideFile);
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);

        FtpFileSystemResult listing = fileSystem.EnumerateDirectory(
            CreateRequest("/"));
        FtpFileSystemResult resolved = fileSystem.ResolveEntry(
            CreateRequest("/file-link"));
        FtpFileOpenResult read = fileSystem.OpenFileRead(
            CreateRequest("/file-link"));
        FtpFileSystemRequest writeRequest =
            CreateRequest("/outside-link/new.txt");
        writeRequest.OpenMode = FtpFileOpenMode.Overwrite;
        FtpFileOpenResult write = fileSystem.OpenFileWrite(writeRequest);
        FtpFileSystemResult create = fileSystem.CreateDirectoryEntry(
            CreateRequest("/outside-link/new-directory"));
        FtpFileSystemRequest uniqueRequest =
            CreateRequest("/outside-link");
        uniqueRequest.UniquePrefix = "unique-";
        FtpFileSystemResult unique =
            fileSystem.CreateUniqueEntry(uniqueRequest);
        FtpFileSystemRequest allocationRequest =
            CreateRequest("/outside-link/allocation.bin");
        allocationRequest.HasLength = true;
        allocationRequest.Length = 10;
        FtpFileSystemResult allocation =
            fileSystem.AllocateEntry(allocationRequest);
        FtpFileSystemResult space = fileSystem.QuerySpace(
            CreateRequest("/outside-link"));
        FtpFileSystemResult mount = fileSystem.MountStructure(
            CreateRequest("/outside-link"));
        FtpFileSystemResult delete = fileSystem.DeleteFileEntry(
            CreateRequest("/file-link"));
        FtpFileSystemResult deleteDirectory =
            fileSystem.DeleteDirectoryEntry(
                CreateRequest("/outside-link"));
        FtpFileSystemRequest timestampRequest =
            CreateRequest("/file-link");
        timestampRequest.ModificationTime =
            FtpTimestampCodec.ParseTimestampValue("20250102030405");
        FtpFileSystemResult timestamp =
            fileSystem.SetModificationTime(timestampRequest);
        FtpFileSystemRequest permissionsRequest =
            CreateRequest("/file-link");
        permissionsRequest.HasUnixMode = true;
        permissionsRequest.UnixMode = 384;
        FtpFileSystemResult permissions =
            fileSystem.SetPermissions(permissionsRequest);
        FtpFileSystemResult internalRead = fileSystem.ResolveEntry(
            CreateRequest("/inside-link"));

        FtpFileSystemRequest renameSource = CreateRequest("/file-link");
        renameSource.DestinationPath = "/moved-link";
        FtpFileSystemResult sourceRename =
            fileSystem.RenameEntry(renameSource);
        FtpFileSystemRequest renameDestination = CreateRequest("/inside.txt");
        renameDestination.DestinationPath = "/outside-link/moved.txt";
        FtpFileSystemResult destinationRename =
            fileSystem.RenameEntry(renameDestination);

        Assert.Multiple(() =>
        {
            Assert.That(listing.Succeeded, Is.True);
            Assert.That(listing.Entries.Single(entry =>
                entry.Name == "outside-link").Type,
                Is.EqualTo(FtpEntryType.SymbolicLink));
            Assert.That(listing.Entries.Single(entry =>
                entry.Name == "file-link").Type,
                Is.EqualTo(FtpEntryType.SymbolicLink));
            Assert.That(resolved.Succeeded, Is.False);
            Assert.That(read.Succeeded, Is.False);
            Assert.That(write.Succeeded, Is.False);
            Assert.That(create.Succeeded, Is.False);
            Assert.That(unique.Succeeded, Is.False);
            Assert.That(allocation.Succeeded, Is.False);
            Assert.That(space.Succeeded, Is.False);
            Assert.That(mount.Succeeded, Is.False);
            Assert.That(delete.Succeeded, Is.False);
            Assert.That(deleteDirectory.Succeeded, Is.False);
            Assert.That(timestamp.Succeeded, Is.False);
            Assert.That(permissions.Succeeded, Is.False);
            Assert.That(internalRead.Succeeded, Is.False);
            Assert.That(sourceRename.Succeeded, Is.False);
            Assert.That(destinationRename.Succeeded, Is.False);
            Assert.That(File.ReadAllText(outsideSecret),
                Is.EqualTo("outside-secret"));
            Assert.That(File.ReadAllText(insideFile), Is.EqualTo("inside"));
            Assert.That(File.Exists(
                Path.Combine(outsideRoot, "new.txt")), Is.False);
            Assert.That(Directory.Exists(
                Path.Combine(outsideRoot, "new-directory")), Is.False);
            Assert.That(File.Exists(
                Path.Combine(outsideRoot, "allocation.bin")), Is.False);
            Assert.That(Directory.GetFiles(outsideRoot, "unique-*"), Is.Empty);
        });

    }

    [Test]
    public void Host_file_system_case_behavior_is_preserved_without_prefix_escape()
    {
        string exactPath = Path.Combine(RootPath, "CaseSensitive.txt");
        File.WriteAllText(exactPath, "case");
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);

        FtpFileSystemResult exact = fileSystem.ResolveEntry(
            CreateRequest("/CaseSensitive.txt"));
        FtpFileSystemResult changedCase = fileSystem.ResolveEntry(
            CreateRequest("/casesensitive.txt"));

        Assert.Multiple(() =>
        {
            Assert.That(exact.Succeeded, Is.True);
            Assert.That(changedCase.Succeeded,
                Is.EqualTo(File.Exists(
                    Path.Combine(RootPath, "casesensitive.txt"))));
            Assert.That(fileSystem.PathComparison,
                Is.EqualTo(OperatingSystem.IsWindows() ?
                    StringComparison.OrdinalIgnoreCase :
                    StringComparison.Ordinal));
        });
    }

    [Test]
    public void File_metadata_exposes_available_size_times_mode_permissions_media_and_facts()
    {
        string localPath = Path.Combine(RootPath, "report.json");
        File.WriteAllText(localPath, "{\"value\":1}");
        DateTime requestedModified = new DateTime(
            2025, 4, 3, 2, 1, 0, DateTimeKind.Utc).AddTicks(1234000);
        File.SetLastWriteTimeUtc(localPath, requestedModified);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                localPath,
                UnixFileMode.UserRead |
                UnixFileMode.UserWrite |
                UnixFileMode.GroupRead);
        }
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);

        FtpFileSystemResult result = fileSystem.ResolveEntry(
            CreateRequest("/report.json"));
        FtpEntry entry = result.Entry!;
        FtpEntry parsed = FtpMlsxCodec.ParseEntry(entry.RawLine);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(entry.Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(entry.HasSize, Is.True);
            Assert.That(entry.Size,
                Is.EqualTo(new FileInfo(localPath).Length));
            Assert.That(entry.Modified.HasValue, Is.True);
            Assert.That(entry.Modified.ValueUtc.Kind,
                Is.EqualTo(DateTimeKind.Utc));
            Assert.That(entry.Modified.ValueUtc,
                Is.EqualTo(File.GetLastWriteTimeUtc(localPath)));
            Assert.That(entry.Modified.RawValue,
                Is.EqualTo(FtpTimestampCodec.FormatTimestampValue(
                    entry.Modified)));
            Assert.That(entry.Created.HasValue, Is.True);
            Assert.That(entry.Created.ValueUtc.Kind,
                Is.EqualTo(DateTimeKind.Utc));
            Assert.That(entry.MediaType, Is.EqualTo("application/json"));
            Assert.That(entry.HasUnixMode,
                Is.EqualTo(!OperatingSystem.IsWindows()));
            Assert.That(entry.UnixMode,
                Is.EqualTo(OperatingSystem.IsWindows() ?
                    0 : (int)File.GetUnixFileMode(localPath)));
            Assert.That(entry.Permissions,
                Does.Contain(FtpPermission.Read));
            Assert.That(entry.Permissions,
                Does.Contain(FtpPermission.Write));
            Assert.That(entry.Permissions,
                Does.Contain(FtpPermission.Append));
            Assert.That(entry.Permissions,
                Does.Contain(FtpPermission.DeleteFile));
            Assert.That(entry.Permissions,
                Does.Contain(FtpPermission.Rename));
            Assert.That(entry.Facts.Select(fact => fact.Name),
                Does.Contain(FtpFactNames.Type));
            Assert.That(entry.Facts.Select(fact => fact.Name),
                Does.Contain(FtpFactNames.Size));
            Assert.That(entry.Facts.Select(fact => fact.Name),
                Does.Contain(FtpFactNames.Modify));
            Assert.That(entry.Facts.Select(fact => fact.Name),
                Does.Contain(FtpFactNames.Create));
            Assert.That(entry.Facts.Select(fact => fact.Name),
                Does.Contain(FtpFactNames.Perm));
            Assert.That(entry.Facts.Select(fact => fact.Name),
                Does.Contain(FtpFactNames.MediaType));
            Assert.That(entry.Facts.All(fact => fact.Standard), Is.True);
            Assert.That(entry.Facts.All(fact =>
                fact.RawText == fact.Name + "=" + fact.Value), Is.True);
            Assert.That(parsed.Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(parsed.Size, Is.EqualTo(entry.Size));
            Assert.That(parsed.Modified.ValueUtc,
                Is.EqualTo(entry.Modified.ValueUtc));
            Assert.That(parsed.MediaType, Is.EqualTo(entry.MediaType));
        });
    }

    [Test]
    public void Directory_metadata_has_directory_permissions_without_file_only_fields()
    {
        string localPath = Path.Combine(RootPath, "folder");
        Directory.CreateDirectory(localPath);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                localPath,
                UnixFileMode.UserRead |
                UnixFileMode.UserWrite |
                UnixFileMode.UserExecute |
                UnixFileMode.GroupRead |
                UnixFileMode.GroupExecute);
        }
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);

        FtpEntry entry = fileSystem.ResolveEntry(
            CreateRequest("/folder")).Entry!;

        Assert.Multiple(() =>
        {
            Assert.That(entry.Type, Is.EqualTo(FtpEntryType.Directory));
            Assert.That(entry.HasSize, Is.False);
            Assert.That(entry.MediaType, Is.Null);
            Assert.That(entry.Permissions,
                Does.Contain(FtpPermission.EnterDirectory));
            Assert.That(entry.Permissions,
                Does.Contain(FtpPermission.List));
            Assert.That(entry.Permissions,
                Does.Contain(FtpPermission.CreateFile));
            Assert.That(entry.Permissions,
                Does.Contain(FtpPermission.CreateDirectory));
            Assert.That(entry.Permissions,
                Does.Contain(FtpPermission.DeleteFile));
            Assert.That(entry.Permissions,
                Does.Contain(FtpPermission.DeleteDirectory));
            Assert.That(entry.Permissions,
                Does.Contain(FtpPermission.Rename));
            Assert.That(entry.Facts.Single(fact =>
                fact.Name == FtpFactNames.Type).Value,
                Is.EqualTo("dir"));
            Assert.That(entry.Facts.Any(fact =>
                fact.Name == FtpFactNames.Size), Is.False);
            Assert.That(entry.Facts.Any(fact =>
                fact.Name == FtpFactNames.MediaType), Is.False);
        });
    }

    [TestCase("file.txt", "text/plain")]
    [TestCase("page.HTML", "text/html")]
    [TestCase("data.csv", "text/csv")]
    [TestCase("image.png", "image/png")]
    [TestCase("photo.JPEG", "image/jpeg")]
    [TestCase("document.pdf", "application/pdf")]
    [TestCase("archive.zip", "application/zip")]
    [TestCase("unknown.custom", null)]
    public void Media_type_is_available_only_for_recognized_file_extensions(
        string name,
        string? expected)
    {
        File.WriteAllText(Path.Combine(RootPath, name), "data");
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);

        FtpEntry entry = fileSystem.ResolveEntry(
            CreateRequest("/" + name)).Entry!;

        Assert.Multiple(() =>
        {
            Assert.That(entry.MediaType, Is.EqualTo(expected));
            Assert.That(entry.Facts.Any(fact =>
                fact.Name == FtpFactNames.MediaType),
                Is.EqualTo(expected != null));
        });
    }

    [Test]
    public void Listing_and_resolution_share_metadata_while_unknown_fields_stay_unavailable()
    {
        string localPath = Path.Combine(RootPath, ".opaque.bin");
        File.WriteAllBytes(localPath, new byte[] { 1, 2, 3 });
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);

        FtpEntry resolved = fileSystem.ResolveEntry(
            CreateRequest("/.opaque.bin")).Entry!;
        FtpEntry listed = fileSystem.EnumerateDirectory(
            CreateRequest("/")).Entries.Single(entry =>
                entry.Name == ".opaque.bin");

        Assert.Multiple(() =>
        {
            Assert.That(listed.Type, Is.EqualTo(resolved.Type));
            Assert.That(listed.Size, Is.EqualTo(resolved.Size));
            Assert.That(listed.Modified.ValueUtc,
                Is.EqualTo(resolved.Modified.ValueUtc));
            Assert.That(listed.Permissions,
                Is.EqualTo(resolved.Permissions));
            Assert.That(listed.Facts.Select(fact => fact.RawText),
                Is.EqualTo(resolved.Facts.Select(fact => fact.RawText)));
            Assert.That(listed.Hidden, Is.True);
            Assert.That(listed.Owner, Is.Null);
            Assert.That(listed.Group, Is.Null);
            Assert.That(listed.Language, Is.Null);
            Assert.That(listed.Unique, Is.Not.Null.And.Not.Empty);
            Assert.That(listed.Unique, Is.EqualTo(resolved.Unique));
            Assert.That(listed.Charset, Is.Null);
            Assert.That(listed.LinkTarget, Is.Null);
            Assert.That(listed.MediaType, Is.Null);
            Assert.That(listed.Facts.Any(fact =>
                fact.Name == FtpFactNames.Unique), Is.True);
            Assert.That(listed.Facts.Any(fact =>
                fact.Name == FtpFactNames.Lang), Is.False);
            Assert.That(listed.Facts.Any(fact =>
                fact.Name == FtpFactNames.Charset), Is.False);
        });
    }

    [Test]
    public void Symlink_listing_reports_only_nontraversing_link_metadata()
    {
        string targetPath = Path.Combine(RootPath, "target.txt");
        string linkPath = Path.Combine(RootPath, "link.txt");
        File.WriteAllText(targetPath, "secret-target-content");
        CreateFileSymbolicLink(linkPath, targetPath);
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);

        FtpEntry entry = fileSystem.EnumerateDirectory(
            CreateRequest("/")).Entries.Single(item =>
                item.Name == "link.txt");

        Assert.Multiple(() =>
        {
            Assert.That(entry.Type, Is.EqualTo(FtpEntryType.SymbolicLink));
            Assert.That(entry.HasSize, Is.False);
            Assert.That(entry.Modified.HasValue, Is.False);
            Assert.That(entry.Created.HasValue, Is.False);
            Assert.That(entry.HasUnixMode, Is.False);
            Assert.That(entry.Permissions, Is.Empty);
            Assert.That(entry.MediaType, Is.Null);
            Assert.That(entry.Unique, Is.Null);
            Assert.That(entry.LinkTarget, Is.Null);
            Assert.That(entry.Facts, Has.Length.EqualTo(1));
            Assert.That(entry.Facts[0].Name,
                Is.EqualTo(FtpFactNames.Type));
            Assert.That(entry.Facts[0].Value,
                Does.StartWith("OS."));
        });
    }

    [Test]
    public void Recursive_directory_requests_and_Unix_modes_mutate_only_contained_entries()
    {
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);
        FtpFileSystemRequest create = CreateRequest("/one/two/three");
        create.Recursive = true;
        create.HasUnixMode = true;
        create.UnixMode = 488;

        FtpFileSystemResult created =
            fileSystem.CreateDirectoryEntry(create);
        string directoryPath = Path.Combine(
            RootPath, "one", "two", "three");
        string filePath = Path.Combine(directoryPath, "item.txt");
        File.WriteAllText(filePath, "content");
        FtpFileSystemRequest permissions =
            CreateRequest("/one/two/three/item.txt");
        permissions.HasUnixMode = true;
        permissions.UnixMode = 416;
        FtpFileSystemResult changed =
            fileSystem.SetPermissions(permissions);
        int directoryMode = 0;
        int fileMode = 0;
        if (!OperatingSystem.IsWindows())
        {
            directoryMode = (int)File.GetUnixFileMode(directoryPath);
            fileMode = (int)File.GetUnixFileMode(filePath);
        }
        FtpFileSystemRequest remove = CreateRequest("/one");
        remove.Recursive = true;
        FtpFileSystemResult removed =
            fileSystem.DeleteDirectoryEntry(remove);
        FtpFileSystemRequest root = CreateRequest("/");
        root.Recursive = true;
        FtpFileSystemResult rootRemoval =
            fileSystem.DeleteDirectoryEntry(root);

        Assert.Multiple(() =>
        {
            Assert.That(created.Succeeded, Is.True);
            Assert.That(changed.Succeeded,
                Is.EqualTo(!OperatingSystem.IsWindows()));
            Assert.That(directoryMode,
                Is.EqualTo(OperatingSystem.IsWindows() ? 0 : 488));
            Assert.That(fileMode,
                Is.EqualTo(OperatingSystem.IsWindows() ? 0 : 416));
            Assert.That(removed.Succeeded, Is.True);
            Assert.That(Directory.Exists(
                Path.Combine(RootPath, "one")), Is.False);
            Assert.That(rootRemoval.Succeeded, Is.False);
            Assert.That(Directory.Exists(RootPath), Is.True);
        });
    }

    [Test]
    public void Unix_mode_validation_rejects_missing_and_out_of_range_values()
    {
        string path = Path.Combine(RootPath, "item.txt");
        File.WriteAllText(path, "content");
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(RootPath);
        FtpFileSystemRequest missing = CreateRequest("/item.txt");
        FtpFileSystemRequest excessive = CreateRequest("/item.txt");
        excessive.HasUnixMode = true;
        excessive.UnixMode = 4096;

        FtpFileSystemResult missingResult =
            fileSystem.SetPermissions(missing);
        FtpFileSystemResult excessiveResult =
            fileSystem.SetPermissions(excessive);

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.Succeeded, Is.False);
            Assert.That(excessiveResult.Succeeded, Is.False);
            Assert.That(File.Exists(path), Is.True);
        });
    }

    private static FtpFileSystemRequest CreateRequest(string path)
    {
        FtpFileSystemRequest request = new FtpFileSystemRequest();
        request.Path = path;
        return request;
    }

    private static void WriteResult(FtpFileOpenResult result, string text)
    {
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Stream, Is.Not.Null);
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        using (result.Stream)
        {
            result.Stream!.Write(bytes, 0, bytes.Length);
            result.Stream.Flush();
        }
    }

    private static void CreateDirectorySymbolicLink(
        string linkPath,
        string targetPath)
    {
        try
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
        }
        catch (Exception exception)
        {
            Assert.Ignore(
                "Directory symbolic links are unavailable: " +
                exception.GetType().Name);
        }
    }

    private static void CreateFileSymbolicLink(
        string linkPath,
        string targetPath)
    {
        try
        {
            File.CreateSymbolicLink(linkPath, targetPath);
        }
        catch (Exception exception)
        {
            Assert.Ignore(
                "File symbolic links are unavailable: " +
                exception.GetType().Name);
        }
    }
}
