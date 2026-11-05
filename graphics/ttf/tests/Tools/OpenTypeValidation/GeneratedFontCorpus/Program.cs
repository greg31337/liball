using System.Security.Cryptography;
using System.Text.Json;
using ttf;

if (args.Length != 2)
{
    Console.Error.WriteLine(
        "usage: GeneratedFontCorpus OUTPUT_DIRECTORY COLOR_FONT_ASSET_DIRECTORY");
    return 2;
}

string outputDirectory = Path.GetFullPath(args[0]);
string assetDirectory = Path.GetFullPath(args[1]);
Directory.CreateDirectory(outputDirectory);

FontFace variableFace = LoadFace(
    assetDirectory,
    "test_glyphs-glyf_colr_1_variable.ttf");
FontInstance instance = variableFace.CreateDefaultInstance();
List<GeneratedFont> generated = new List<GeneratedFont>();

generated.Add(WriteFont(
    outputDirectory,
    "colrv1-default-static.ttf",
    "Default static instance produced from a variable COLR v1 font.",
    instance.CopyStaticSfntBytes()));

FontSubsetOptions retainedColor = FontSubsetOptions.CreateStrict();
retainedColor.SetColorPolicy(FontSubsetColorPolicy.Retain);
FontSubsetBuilder subsetBuilder = new FontSubsetBuilder(
    instance,
    retainedColor);
subsetBuilder.AddGlyph(90);
generated.Add(WriteFont(
    outputDirectory,
    "colrv1-gradient-subset.ttf",
    "TrueType subset retaining the COLR v1 closure for glyph 90.",
    subsetBuilder.BuildResult().CopyFontBytes()));

JsonSerializerOptions jsonOptions = new JsonSerializerOptions();
jsonOptions.WriteIndented = true;
string manifest = JsonSerializer.Serialize(generated, jsonOptions) + "\n";
File.WriteAllText(
    Path.Combine(outputDirectory, "generated-fonts.json"),
    manifest);
return 0;

static FontFace LoadFace(string directory, string name)
{
    byte[] bytes = File.ReadAllBytes(Path.Combine(directory, name));
    return FontLoader.Load(bytes, FontLoadOptions.CreateStrict()).GetFace(0);
}

static GeneratedFont WriteFont(
    string directory,
    string name,
    string operation,
    byte[] bytes)
{
    FontPackage reopened = FontLoader.Load(
        bytes,
        FontLoadOptions.CreateStrict());
    if (reopened.GetFaceCount() != 1)
    {
        throw new InvalidDataException(
            "A generated standalone font did not contain exactly one face.");
    }

    string path = Path.Combine(directory, name);
    File.WriteAllBytes(path, bytes);
    return new GeneratedFont(
        name,
        bytes.Length,
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
        operation);
}

internal sealed class GeneratedFont
{
    public string File { get; }
    public int Size { get; }
    public string Sha256 { get; }
    public string Operation { get; }

    public GeneratedFont(
        string file,
        int size,
        string sha256,
        string operation)
    {
        File = file;
        Size = size;
        Sha256 = sha256;
        Operation = operation;
    }
}
