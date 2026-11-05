using raw;
using System.Buffers.Binary;
using System.Security.Cryptography;

string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
RawThumbnail preview = LoadPreview(Path.Combine(root, "corpus/files/canon-eos-6d-mark-ii-raw-3x2-1.0.2.cr2"));

GenerateCr2(Path.Combine(root, "corpus/generated/canon-eos-6d-mark-ii-raw-3x2-write-golden.cr2"), preview);
GC.Collect();
GenerateNef(Path.Combine(root, "corpus/generated/nikon-z-7-uncompressed-14bit-fx-write-golden.nef"), preview, 8256, 5504, "nikon.z-7.nef.uncompressed-14bit.fx.write.experimental");
GC.Collect();
GenerateNef(Path.Combine(root, "corpus/generated/nikon-z-8-lossless-14bit-fx-write-golden.nef"), preview, 8280, 5520, "nikon.z-8.nef.lossless-14bit.fx.write.experimental");

string[] cameraFiles = Directory.GetFiles(Path.Combine(root, "corpus/files"));
Array.Sort(cameraFiles, StringComparer.Ordinal);
int cameraIndex = 0;
while (cameraIndex < cameraFiles.Length)
{
    ReportDecoded(cameraFiles[cameraIndex]);
    GC.Collect();
    cameraIndex++;
}
string[] generatedFiles = Directory.GetFiles(Path.Combine(root, "corpus/generated"));
Array.Sort(generatedFiles, StringComparer.Ordinal);
int generatedIndex = 0;
while (generatedIndex < generatedFiles.Length)
{
    ReportDecoded(generatedFiles[generatedIndex]);
    GC.Collect();
    generatedIndex++;
}

static void GenerateCr2(string path, RawThumbnail preview)
{
    int width = 6384;
    int height = 4224;
    RawCr2WriteInput input = new RawCr2WriteInput();
    input.Width = width;
    input.Height = height;
    input.RowStrideSamples = width;
    input.StoredSamples = CreateSamples(width, height);
    input.PrimaryPreview = preview;
    input.Thumbnail = preview;
    input.SecondaryPreview = preview;
    input.CanonCameraSettings = new short[47];
    input.CanonCameraSettings[3] = 4;
    input.CanonColorData = new ushort[1602];
    input.CanonColorData[0] = 15;
    input.CanonColorData[0x3f] = 2000;
    input.CanonColorData[0x40] = 1024;
    input.CanonColorData[0x41] = 1024;
    input.CanonColorData[0x42] = 1800;
    input.CanonColorData[0x30a] = 512;
    input.CanonColorData[0x30b] = 512;
    input.CanonColorData[0x30c] = 512;
    input.CanonColorData[0x30d] = 512;
    input.CanonColorData[0x30e] = 15000;
    input.CanonModelId = 2147484678;
    input.Metadata.Software = "raw corpus generator";
    input.Metadata.Artist = "raw project";
    input.Metadata.Copyright = "CC0-1.0";
    input.Metadata.OriginalTime = new RawExifTimestamp("2026:08:20 00:00:00", "+00:00", "000");
    RawWriter.EncodeCr2(path, input, Options("canon.eos-6d-mark-ii.cr2.raw-3x2.firmware-1.0.2.write.experimental"));
    Report(path, HashSamples(input.StoredSamples));
}

static void GenerateNef(string path, RawThumbnail preview, int width, int height, string profile)
{
    RawNefWriteInput input = new RawNefWriteInput();
    input.Width = width;
    input.Height = height;
    input.RowStrideSamples = width;
    input.StoredSamples = CreateSamples(width, height);
    input.Preview = preview;
    input.Metadata.Software = "raw corpus generator";
    input.Metadata.Artist = "raw project";
    input.Metadata.Copyright = "CC0-1.0";
    input.Metadata.OriginalTime = new RawExifTimestamp("2026:08:20 00:00:00", "+00:00", "000");
    input.Metadata.WhiteBalance = "Auto";
    input.Metadata.IsoSensitivity = 100;
    RawWriter.EncodeNef(path, input, Options(profile));
    Report(path, HashSamples(input.StoredSamples));
}

static RawWriteOptions Options(string profile)
{
    RawWriteOptions options = new RawWriteOptions();
    options.Mode = RawWriteMode.NewContainer;
    options.ProfileName = profile;
    options.AllowExperimentalProfiles = true;
    options.ValidateAfterWrite = true;
    options.VerifyPixelIdentity = true;
    return options;
}

static ushort[] CreateSamples(int width, int height)
{
    ushort[] samples = new ushort[checked(width * height)];
    int y = 0;
    while (y < height)
    {
        int x = 0;
        while (x < width)
        {
            samples[y * width + x] = (ushort)(((x & 63) * 17 + (y & 63) * 31 + ((x ^ y) & 255)) & 0x3fff);
            x++;
        }
        y++;
    }
    return samples;
}

static RawThumbnail LoadPreview(string path)
{
    RawOpenOptions options = new RawOpenOptions();
    options.AllowExperimentalProfiles = true;
    options.ReadPreviews = true;
    RawDocument document = RawCodec.Open(path, null, options);
    RawThumbnail selected = null;
    int index = 0;
    while (index < document.Thumbnails.Length)
    {
        RawThumbnail candidate = document.Thumbnails[index];
        if (candidate.Kind == RawThumbnailKind.Jpeg && (selected == null || candidate.EncodedBytes.Length < selected.EncodedBytes.Length))
        {
            selected = candidate;
        }
        index++;
    }
    if (selected == null)
    {
        throw new InvalidOperationException("The CC0 source sample contains no JPEG preview.");
    }
    return new RawThumbnail(0, RawThumbnailKind.Jpeg, selected.Width, selected.Height, selected.Orientation, selected.MimeType, selected.EncodedBytes, null, null);
}

static void Report(string path, string decodedHash)
{
    FileInfo info = new FileInfo(path);
    Console.WriteLine(Path.GetFileName(path) + "\t" + info.Length + "\t" + HashFile(path) + "\t" + decodedHash);
}

static void ReportDecoded(string path)
{
    RawOpenOptions openOptions = new RawOpenOptions();
    openOptions.AllowExperimentalProfiles = true;
    openOptions.ReadPreviews = true;
    RawDocument document = RawCodec.Open(path, null, openOptions);
    RawDecodeOptions decodeOptions = new RawDecodeOptions();
    decodeOptions.Limits = openOptions.Limits;
    RawSensorImage image = document.Decode(decodeOptions);
    RawRenderOptions renderOptions = new RawRenderOptions();
    renderOptions.DemosaicMode = RawDemosaicMode.Bilinear;
    renderOptions.WhiteBalanceMode = RawWhiteBalanceMode.None;
    renderOptions.ColorSpace = RawColorSpace.RawCamera;
    renderOptions.TransferFunction = RawTransferFunction.Linear;
    renderOptions.GamutMode = RawGamutMode.Clip;
    renderOptions.OutputBitDepth = 8;
    renderOptions.OutputWidth = 64;
    renderOptions.OutputHeight = 64;
    renderOptions.ApplyOrientation = true;
    renderOptions.HalfSize = image.Info.Cfa != null;
    RawRgbImage rendered = RawRenderer.Render(image, renderOptions);
    Console.WriteLine("decoded\t" + Path.GetFileName(path) + "\t" + document.ProfileName + "\t" + image.Info.Width + "x" + image.Info.Height + "x" + image.Info.GetChannelCount() + "\t" + HashSamples(image.IntegerSamples) + "\t" + HashBytes(rendered.ByteSamples));
}

static string HashFile(string path)
{
    using FileStream input = File.OpenRead(path);
    return Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
}

static string HashBytes(byte[] bytes)
{
    return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}

static string HashSamples(ushort[] samples)
{
    using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    byte[] bytes = new byte[8192];
    int sample = 0;
    while (sample < samples.Length)
    {
        int count = Math.Min(bytes.Length / 2, samples.Length - sample);
        int index = 0;
        while (index < count)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(index * 2, 2), samples[sample + index]);
            index++;
        }
        hash.AppendData(bytes, 0, count * 2);
        sample += count;
    }
    return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
}
