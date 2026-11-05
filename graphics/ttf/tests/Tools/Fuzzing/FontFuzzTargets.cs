using System.Text;
using ttf;

namespace ttf.Fuzzing;

public static class FontFuzzTargets
{
    const int EnvelopeLength = 32;

    public static bool Run(string target, byte[] data)
    {
        if (target == "container-load")
        {
            ContainerLoad(data);
        }
        else if (target == "face-inspection")
        {
            FaceInspection(data);
        }
        else if (target == "glyph-outline")
        {
            GlyphOutline(data);
        }
        else if (target == "shaping")
        {
            Shaping(data);
        }
        else if (target == "instancing")
        {
            Instancing(data);
        }
        else if (target == "subsetting")
        {
            Subsetting(data);
        }
        else if (target == "pdf-finalization")
        {
            PdfFinalization(data);
        }
        else
        {
            return false;
        }

        return true;
    }

    public static byte[] WrapSeedFont(byte[] fontBytes)
    {
        if (fontBytes == null)
        {
            throw new ArgumentNullException("fontBytes");
        }

        byte[] input = new byte[EnvelopeLength + fontBytes.Length];
        Array.Copy(fontBytes, 0, input, EnvelopeLength, fontBytes.Length);
        return input;
    }

    public static void ContainerLoad(byte[] data)
    {
        ExecuteExpected(() =>
        {
            FontPackage package = FontLoader.Load(
                data,
                FontLoadOptions.CreateStrict());
            package.GetFaceCount();
            package.GetDiagnostics();
        });
    }

    public static void FaceInspection(byte[] data)
    {
        ExecuteExpected(() =>
        {
            FuzzInput input = FuzzInput.Create(data);
            FontPackage package = input.LoadPackage();
            FontFace face = input.SelectFace(package);
            face.GetDescription();
            face.GetNames();
            face.GetClassification();
            face.GetCoverage();
            face.GetMetrics();
            face.GetEmbeddingRights();
            int tableIndex = 0;

            while (tableIndex < face.GetTableCount())
            {
                FontTableInfo table = face.GetTableInfo(tableIndex);
                table.GetTag();
                table.GetChecksum();
                table.GetOffset();
                table.GetLength();
                table.GetKind();

                if (table.HasRawData())
                {
                    table.CopyData();
                }

                tableIndex += 1;
            }

            package.GetDiagnostics();
        });
    }

    public static void GlyphOutline(byte[] data)
    {
        ExecuteExpected(() =>
        {
            FuzzInput input = FuzzInput.Create(data);
            FontFace face = input.SelectFace(input.LoadPackage());
            FontInstance instance = face.CreateDefaultInstance();
            int glyphId = input.SelectGlyph(instance);
            instance.GetGlyphOutline(glyphId);
            instance.GetGlyphBounds(glyphId);
            instance.GetGlyphMetrics(glyphId);
            instance.GetGlyphName(glyphId);
        });
    }

    public static void Shaping(byte[] data)
    {
        ExecuteExpected(() =>
        {
            FuzzInput input = FuzzInput.Create(data);
            FontFace face = input.SelectFace(input.LoadPackage());
            FontInstance instance = CreateInstance(face, input);
            TextShapingResult result = TextShaper.Shape(
                input.CreateText(),
                instance);
            int runIndex = 0;

            while (runIndex < result.GetRunCount())
            {
                TextShapingRun shapingRun = result.GetRun(runIndex);
                ShapedRun run = shapingRun.GetShapedRun();
                int glyphIndex = 0;

                while (glyphIndex < run.GetGlyphCount())
                {
                    run.GetGlyph(glyphIndex);
                    glyphIndex += 1;
                }

                int clusterIndex = 0;

                while (clusterIndex < run.GetClusterCount())
                {
                    run.GetCluster(clusterIndex);
                    clusterIndex += 1;
                }

                runIndex += 1;
            }

            int diagnosticIndex = 0;

            while (diagnosticIndex < result.GetDiagnosticCount())
            {
                result.GetDiagnostic(diagnosticIndex);
                diagnosticIndex += 1;
            }
        });
    }

    public static void Instancing(byte[] data)
    {
        ExecuteExpected(() =>
        {
            FuzzInput input = FuzzInput.Create(data);
            FontFace face = input.SelectFace(input.LoadPackage());
            FontInstance instance = CreateInstance(face, input);
            int glyphId = input.SelectGlyph(instance);
            instance.GetGlyphOutline(glyphId);
            instance.GetGlyphMetrics(glyphId);
            instance.CopyStaticSfntBytes();

            int namedInstanceCount = face.GetNamedInstanceCount();

            if (namedInstanceCount != 0)
            {
                FontInstance named = face.CreateNamedInstance(
                    input.ReadUInt16(10) % namedInstanceCount);
                named.GetCoordinates();
                named.GetGlyphOutline(input.SelectGlyph(named));
            }
        });
    }

    public static void Subsetting(byte[] data)
    {
        ExecuteExpected(() =>
        {
            FuzzInput input = FuzzInput.Create(data);
            FontFace face = input.SelectFace(input.LoadPackage());
            FontInstance instance = CreateInstance(face, input);
            FontSubsetOptions options = FontSubsetOptions.CreateCompatible();
            FontSubsetBuilder builder = new FontSubsetBuilder(
                instance,
                options);
            builder.AddGlyph(input.SelectGlyph(instance));
            string text = input.CreateText();

            if (text.Length != 0)
            {
                builder.AddText(text);
            }

            FontSubsetResult result = builder.BuildResult();
            byte[] subsetBytes = result.CopyFontBytes();
            result.GetDiagnostics();
            result.GetTableReport();
            FontLoader.Load(subsetBytes, FontLoadOptions.CreateStrict());
        });
    }

    public static void PdfFinalization(byte[] data)
    {
        ExecuteExpected(() =>
        {
            FuzzInput input = FuzzInput.Create(data);
            FontFace face = input.SelectFace(input.LoadPackage());
            FontInstance instance = CreateInstance(face, input);
            PdfFontOptions options = PdfFontOptions.Create();
            options.SetVersion((PdfVersion)(2 + input.ReadByte(12) % 7));
            options.SetWritingMode(
                (PdfWritingMode)(input.ReadByte(13) % 2));
            options.SetEmbeddingMode(
                (PdfFontEmbeddingMode)(input.ReadByte(14) % 2));
            options.SetRightsPolicy(
                (PdfFontRightsPolicy)(input.ReadByte(15) % 3));
            PdfFontBuilder builder = new PdfFontBuilder(instance, options);
            int glyphId = input.SelectGlyph(instance);
            int codePoint = input.CreateCodePoint(16);
            builder.AddGlyph(glyphId, new int[] { codePoint });
            builder.AddText(input.CreateText());
            PdfFontResource resource = builder.FinalizeFont();
            byte[] embedded = resource.CopyEmbeddedFont();
            resource.GetCompatibilityReport();
            resource.GetToUnicodeMappings();
            FontLoader.Load(embedded, FontLoadOptions.CreateStrict());
        });
    }

    static FontInstance CreateInstance(FontFace face, FuzzInput input)
    {
        FontInstance defaultInstance = face.CreateDefaultInstance();
        NormalizedFontCoordinates defaults =
            defaultInstance.GetCoordinates();

        if (defaults.GetAxisCount() == 0)
        {
            return defaultInstance;
        }

        FontCoordinates coordinates = new FontCoordinates();
        int selectedAxis = input.ReadUInt16(8) % defaults.GetAxisCount();
        int axisTag = defaults.GetAxisTag(selectedAxis);
        double coordinate = input.ReadInt32(4) / 65536.0;
        coordinates.Set(axisTag, coordinate);
        return face.CreateInstance(coordinates);
    }

    static void ExecuteExpected(Action operation)
    {
        try
        {
            operation();
        }
        catch (FontFormatException)
        {
        }
        catch (FontLimitException)
        {
        }
        catch (FontEmbeddingException)
        {
        }
    }
}

internal class FuzzInput
{
    byte[] controls;
    byte[] fontBytes;

    FuzzInput(byte[] controls, byte[] fontBytes)
    {
        this.controls = controls;
        this.fontBytes = fontBytes;
    }

    internal static FuzzInput Create(byte[] data)
    {
        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        if (data.Length < 32)
        {
            throw FontFormatException.Create(
                "A structured fuzz input requires its control envelope.");
        }

        byte[] controls = new byte[32];
        byte[] fontBytes = new byte[data.Length - controls.Length];
        Array.Copy(data, controls, controls.Length);
        Array.Copy(
            data,
            controls.Length,
            fontBytes,
            0,
            fontBytes.Length);
        return new FuzzInput(controls, fontBytes);
    }

    internal FontPackage LoadPackage()
    {
        return FontLoader.Load(
            fontBytes,
            FontLoadOptions.CreateStrict());
    }

    internal FontFace SelectFace(FontPackage package)
    {
        int faceIndex = ReadUInt16(0) % package.GetFaceCount();
        return package.GetFace(faceIndex);
    }

    internal int SelectGlyph(FontInstance instance)
    {
        int glyphCount = instance.GetGlyphCount();

        if (glyphCount == 0)
        {
            throw FontFormatException.Create(
                "A fuzz target requires at least one glyph.");
        }

        return ReadUInt16(2) % glyphCount;
    }

    internal string CreateText()
    {
        StringBuilder text = new StringBuilder();
        int scalarCount = 1 + ReadByte(11) % 8;
        int index = 0;

        while (index < scalarCount)
        {
            int offset = 16 + (index * 2) % 16;
            text.Append(char.ConvertFromUtf32(CreateCodePoint(offset)));
            index += 1;
        }

        return text.ToString();
    }

    internal int CreateCodePoint(int offset)
    {
        int value = ReadUInt16(offset);
        int selector = ReadByte((offset + 7) % controls.Length) % 8;

        if (selector == 0)
        {
            return 0x20 + value % 0x5F;
        }

        if (selector == 1)
        {
            return 0x0590 + value % 0x70;
        }

        if (selector == 2)
        {
            return 0x0600 + value % 0x100;
        }

        if (selector == 3)
        {
            return 0x0900 + value % 0x80;
        }

        if (selector == 4)
        {
            return 0x0E00 + value % 0x80;
        }

        if (selector == 5)
        {
            return 0x1000 + value % 0xA0;
        }

        if (selector == 6)
        {
            return 0xAC00 + value % 0x2BA4;
        }

        return 0x1F300 + value % 0x300;
    }

    internal byte ReadByte(int offset)
    {
        return controls[offset % controls.Length];
    }

    internal int ReadUInt16(int offset)
    {
        int first = ReadByte(offset);
        int second = ReadByte(offset + 1);
        return (first << 8) | second;
    }

    internal int ReadInt32(int offset)
    {
        uint first = ReadByte(offset);
        uint second = ReadByte(offset + 1);
        uint third = ReadByte(offset + 2);
        uint fourth = ReadByte(offset + 3);
        uint value =
            (first << 24) |
            (second << 16) |
            (third << 8) |
            fourth;
        return (int)value;
    }
}
