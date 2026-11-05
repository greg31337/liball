namespace gltf.Tests.Support;

internal enum KhronosAssetExpectation
{
    Success = 0,
    SemanticWarning = 1,
    Unavailable = 2
}

internal sealed class KhronosAssetFile
{
    internal string RelativePath = "";
    internal string Sha256 = "";
}

internal sealed class KhronosAssetCase
{
    internal string Path = "";
    internal string RelativePath = "";
    internal string Id = "";
    internal string Repository = "";
    internal string Revision = "";
    internal string EntryFile = "";
    internal string EntrySha256 = "";
    internal string? ValidatorResultFile;
    internal GltfContainerFormat Format = GltfContainerFormat.Auto;
    internal KhronosAssetExpectation Expectation;
    internal string? ReadIssueCode;
    internal string? ValidationIssueCode;
    internal GltfValidationSeverity ValidationIssueSeverity;
    internal List<KhronosAssetFile> Files = new List<KhronosAssetFile>();
}

internal static class KhronosAssetCorpus
{
    internal const string SampleAssetsRepository =
        "https://github.com/KhronosGroup/glTF-Sample-Assets";
    internal const string SampleAssetsRevision =
        "2d97dcc2463db123ed5203598cffedf8b6cf1683";
    internal const string AssetGeneratorRepository =
        "https://github.com/KhronosGroup/glTF-Asset-Generator";
    internal const string AssetGeneratorRevision =
        "3d99767e9a67fbfe109f0d298c1e8d909bcac9db";

    internal static List<KhronosAssetCase> EnumerateAssets()
    {
        string root = FindRepositoryRoot();
        string corpusRoot = Path.Combine(root, "specs", "khronos-gltf");
        List<string> paths = Directory.GetFiles(
                corpusRoot,
                "*.*",
                SearchOption.AllDirectories)
            .Where(path => string.Equals(
                    Path.GetExtension(path),
                    ".gltf",
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    Path.GetExtension(path),
                    ".glb",
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
        List<KhronosAssetCase> result = new List<KhronosAssetCase>();
        foreach (string path in paths)
        {
            result.Add(new KhronosAssetCase
            {
                Path = path,
                RelativePath = Path.GetRelativePath(root, path)
                    .Replace(Path.DirectorySeparatorChar, '/'),
                Format = string.Equals(
                    Path.GetExtension(path),
                    ".glb",
                    StringComparison.OrdinalIgnoreCase)
                    ? GltfContainerFormat.Glb
                    : GltfContainerFormat.Json
            });
        }

        return result;
    }

    internal static byte[] ReadBytes(KhronosAssetCase asset)
    {
        return File.ReadAllBytes(asset.Path);
    }

    internal static List<KhronosAssetCase> EnumeratePinnedAssets()
    {
        List<KhronosAssetCase> result = new List<KhronosAssetCase>();
        result.Add(CreateSampleBox());
        result.Add(CreateGeneratorCompatibility00());
        result.Add(CreateGeneratorCompatibility04());
        result.Add(CreateGeneratorCompatibility05());
        result.Add(CreateGeneratorMeshNoPosition());
        return result;
    }

    internal static string MaterializeAsset(
        TemporaryResourceWorkspace workspace,
        KhronosAssetCase asset)
    {
        string fixtureRoot = Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "Fixtures",
            "Khronos",
            asset.Id);
        int fileIndex = 0;
        while (fileIndex < asset.Files.Count)
        {
            KhronosAssetFile file = asset.Files[fileIndex];
            string encodedPath = Path.Combine(
                fixtureRoot,
                file.RelativePath + ".base64");
            byte[] bytes = Convert.FromBase64String(
                File.ReadAllText(encodedPath).Trim());
            workspace.WriteBytes(
                asset.Id + "/" + file.RelativePath,
                bytes);
            fileIndex++;
        }

        return workspace.GetPath(asset.Id + "/" + asset.EntryFile);
    }

    internal static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "todo.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("The repository root was not found.");
    }

    internal static KhronosAssetCase CreateSampleBox()
    {
        KhronosAssetCase result = CreateCase(
            "sample-assets-box-glb",
            SampleAssetsRepository,
            SampleAssetsRevision,
            "Box.glb",
            "ed52f7192b8311d700ac0ce80644e3852cd01537e4d62241b9acba023da3d54e",
            GltfContainerFormat.Glb,
            KhronosAssetExpectation.Success);
        result.Files.Add(CreateFile("Box.glb", result.EntrySha256));
        return result;
    }

    internal static KhronosAssetCase CreateGeneratorCompatibility00()
    {
        KhronosAssetCase result = CreateGeneratorCase(
            "asset-generator-compatibility-00",
            "Compatibility_00.gltf",
            "5d92bbe4380afd4fae29c98477b7a217f379d6551032a6c297bd874b01becde5",
            KhronosAssetExpectation.Success);
        result.Files.Add(CreateFile("Compatibility_00.gltf", result.EntrySha256));
        result.Files.Add(CreateCompatibilityBuffer("Compatibility_00.bin"));
        AddValidatorResult(
            result,
            "Compatibility_00.validator.json",
            "7064762d1f6c2e95bfbb7b5051cbce76930ffa2c5ad74bb8a4a8914b5316b8b4");
        return result;
    }

    internal static KhronosAssetCase CreateGeneratorCompatibility04()
    {
        KhronosAssetCase result = CreateGeneratorCase(
            "asset-generator-compatibility-04",
            "Compatibility_04.gltf",
            "24361852c0c314d2c37a88b6af62a12666a59da1b286e5e0cc424f0158cbf512",
            KhronosAssetExpectation.Unavailable);
        result.ReadIssueCode = GltfIssueCodes.UnknownAssetMinorVersion;
        result.ValidationIssueCode = GltfIssueCodes.UnknownAssetMinorVersion;
        result.ValidationIssueSeverity = GltfValidationSeverity.Warning;
        result.Files.Add(CreateFile("Compatibility_04.gltf", result.EntrySha256));
        result.Files.Add(CreateCompatibilityBuffer("Compatibility_04.bin"));
        AddValidatorResult(
            result,
            "Compatibility_04.validator.json",
            "f6255cd0359a716b7a383360f343582338056ad1b52b72afd0ff8e7942486087");
        return result;
    }

    internal static KhronosAssetCase CreateGeneratorCompatibility05()
    {
        KhronosAssetCase result = CreateGeneratorCase(
            "asset-generator-compatibility-05",
            "Compatibility_05.gltf",
            "68e291341c641623044ff45657b688b8d2f2c0859d3766ae70a26a248a81f0dc",
            KhronosAssetExpectation.Unavailable);
        result.ReadIssueCode = GltfIssueCodes.RequiredExtensionMetadataUnavailable;
        result.ValidationIssueCode = GltfIssueCodes.UnsupportedExtension;
        result.ValidationIssueSeverity = GltfValidationSeverity.Warning;
        result.Files.Add(CreateFile("Compatibility_05.gltf", result.EntrySha256));
        result.Files.Add(CreateCompatibilityBuffer("Compatibility_05.bin"));
        AddValidatorResult(
            result,
            "Compatibility_05.validator.json",
            "2e7d0eccc2983d93506d49bb592077fb5deb9af14c9be14737a1a467f845a45c");
        return result;
    }

    internal static KhronosAssetCase CreateGeneratorMeshNoPosition()
    {
        KhronosAssetCase result = CreateGeneratorCase(
            "asset-generator-mesh-no-position-00",
            "Mesh_NoPosition_00.gltf",
            "353b80694d4b9299deeff3e629a50f0949b8f5782f379bd6fe9327b689f0e18c",
            KhronosAssetExpectation.SemanticWarning);
        result.ValidationIssueCode = GltfIssueCodes.MeshPrimitiveNoPosition;
        result.ValidationIssueSeverity = GltfValidationSeverity.Warning;
        result.Files.Add(CreateFile("Mesh_NoPosition_00.gltf", result.EntrySha256));
        result.Files.Add(CreateFile(
            "Mesh_NoPosition_00.bin",
            "93ffbfc491203a5042ce8729e8bde71460b23b81ce1c3b71292976bcdb6a42bd"));
        AddValidatorResult(
            result,
            "Mesh_NoPosition_00.validator.json",
            "fe97f12e30861b583f6bbca5a0591770d7fe180208882e07bf93d6fb7008a014");
        return result;
    }

    internal static KhronosAssetCase CreateGeneratorCase(
        string id,
        string entryFile,
        string entrySha256,
        KhronosAssetExpectation expectation)
    {
        return CreateCase(
            id,
            AssetGeneratorRepository,
            AssetGeneratorRevision,
            entryFile,
            entrySha256,
            GltfContainerFormat.Json,
            expectation);
    }

    internal static KhronosAssetCase CreateCase(
        string id,
        string repository,
        string revision,
        string entryFile,
        string entrySha256,
        GltfContainerFormat format,
        KhronosAssetExpectation expectation)
    {
        KhronosAssetCase result = new KhronosAssetCase();
        result.Id = id;
        result.Repository = repository;
        result.Revision = revision;
        result.EntryFile = entryFile;
        result.EntrySha256 = entrySha256;
        result.Format = format;
        result.Expectation = expectation;
        return result;
    }

    internal static KhronosAssetFile CreateCompatibilityBuffer(string relativePath)
    {
        return CreateFile(
            relativePath,
            "51119fa1de3402714ee0e9ed76a6c3272dafe44942c46642ff04d64111282f8b");
    }

    internal static KhronosAssetFile CreateFile(string relativePath, string sha256)
    {
        KhronosAssetFile result = new KhronosAssetFile();
        result.RelativePath = relativePath;
        result.Sha256 = sha256;
        return result;
    }

    internal static void AddValidatorResult(
        KhronosAssetCase asset,
        string relativePath,
        string sha256)
    {
        asset.ValidatorResultFile = relativePath;
        asset.Files.Add(CreateFile(relativePath, sha256));
    }
}
