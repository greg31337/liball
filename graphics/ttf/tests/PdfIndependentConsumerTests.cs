using System.Diagnostics;
using System.Globalization;
using System.Text;
using ttf;

namespace ttf.Tests;

internal class PdfIndependentConsumerTests
{
    [Test]
    public void CoreGraphicsAndPdfKitValidateFontsWidthsCidsAndExtraction()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfConsumerCase[] cases = new PdfConsumerCase[]
        {
            CreateLigatureCase(instance),
            CreateBidiCase(instance),
            CreateVerticalCase(instance)
        };

        int index = 0;

        while (index < cases.Length)
        {
            PdfCidFontType2Artifact? artifact = cases[index].resource
                .GetCidFontType2Artifact();

            Assert.Multiple(() =>
            {
                Assert.That(artifact!.GetCidMapping()
                    .CopyCidToGidMap(), Is.Not.Empty);
                Assert.That(artifact.CopyNewToOldGlyphIds().Length,
                    Is.GreaterThan(1));
                Assert.That(artifact.GetWidths().GetCidCount(),
                    Is.EqualTo(
                        cases[index].resource.GetCharacterCodeCount()));
                Assert.That(artifact.HasVerticalWidths(),
                    Is.EqualTo(cases[index].vertical));
                Assert.That(Encoding.Latin1.GetString(cases[index].pdf),
                    Does.Contain("/ToUnicode"));
            });
            index += 1;
        }

        if (!OperatingSystem.IsMacOS() || !File.Exists("/usr/bin/clang"))
        {
            return;
        }

        RunAppleConsumers(cases);
    }

    static PdfConsumerCase CreateLigatureCase(FontInstance instance)
    {
        PdfFontBuilder builder = new PdfFontBuilder(
            instance,
            PdfFontOptions.Create());
        builder.AddGlyph(13, new int[] { 0x66, 0x69 });
        builder.AddGlyph(2, new int[] { 0x41 });
        PdfFontResource resource = builder.FinalizeFont();
        return new PdfConsumerCase(
            resource,
            MinimalPdfAssembler.Assemble(
                resource,
                new byte[] { 0, 1 }),
            "fi",
            false);
    }

    static PdfConsumerCase CreateBidiCase(FontInstance instance)
    {
        PdfFontBuilder builder = new PdfFontBuilder(
            instance,
            PdfFontOptions.Create());
        builder.AddGlyph(13, new int[] { 0x05D0 });
        builder.AddGlyph(2, new int[] { 0x05D1 });
        PdfFontResource resource = builder.FinalizeFont();
        return new PdfConsumerCase(
            resource,
            MinimalPdfAssembler.Assemble(
                resource,
                new byte[] { 0, 2, 0, 1 },
                "\u05D0\u05D1"),
            "\u05D0\u05D1",
            false);
    }

    static PdfConsumerCase CreateVerticalCase(FontInstance instance)
    {
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetWritingMode(PdfWritingMode.Vertical);
        PdfFontBuilder builder = new PdfFontBuilder(instance, options);
        builder.AddGlyph(13, new int[] { 0x41 });
        builder.AddGlyph(2, new int[] { 0x42 });
        PdfFontResource resource = builder.FinalizeFont();
        return new PdfConsumerCase(
            resource,
            MinimalPdfAssembler.Assemble(
                resource,
                new byte[] { 0, 1 }),
            "A",
            true);
    }

    static void RunAppleConsumers(PdfConsumerCase[] cases)
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "ttf-pdf-consumers-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            string sourcePath = Path.Combine(directory, "consumer.m");
            string executablePath = Path.Combine(directory, "consumer");
            File.WriteAllText(sourcePath, AppleConsumerSource);
            CompileAppleConsumer(directory, sourcePath, executablePath);
            ProcessStartInfo start = new ProcessStartInfo(executablePath)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            int index = 0;

            while (index < cases.Length)
            {
                string pdfPath = Path.Combine(
                    directory,
                    "case-" + index.ToString(
                        CultureInfo.InvariantCulture) + ".pdf");
                File.WriteAllBytes(pdfPath, cases[index].pdf);
                AddConsumerArguments(start, pdfPath, cases[index]);
                index += 1;
            }

            using Process? process = Process.Start(start);
            string output = process!.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            bool exited = process.WaitForExit(60000);

            Assert.Multiple(() =>
            {
                Assert.That(exited, Is.True, "Apple consumers timed out.");
                Assert.That(process.ExitCode, Is.Zero, error);
                Assert.That(output.Split(
                    '\n',
                    StringSplitOptions.RemoveEmptyEntries),
                    Has.Length.EqualTo(cases.Length));
                Assert.That(output, Does.Contain("CoreGraphics:OK"));
                Assert.That(output, Does.Contain("PDFKit:OK"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    static void AddConsumerArguments(
        ProcessStartInfo start,
        string pdfPath,
        PdfConsumerCase testCase)
    {
        PdfCidFontType2Artifact? artifact = testCase.resource
            .GetCidFontType2Artifact();
        start.ArgumentList.Add(pdfPath);
        start.ArgumentList.Add(testCase.expectedText);
        start.ArgumentList.Add(artifact!.GetWidths().GetDefaultWidth()
            .ToString(CultureInfo.InvariantCulture));
        start.ArgumentList.Add(Convert.ToHexString(
            artifact.GetCidMapping().CopyCidToGidMap()));
        start.ArgumentList.Add(testCase.vertical ? "1" : "0");
        start.ArgumentList.Add(testCase.resource.CopyEmbeddedFont().Length
            .ToString(CultureInfo.InvariantCulture));
        start.ArgumentList.Add(artifact.CopyNewToOldGlyphIds().Length
            .ToString(CultureInfo.InvariantCulture));
    }

    static void CompileAppleConsumer(
        string directory,
        string sourcePath,
        string executablePath)
    {
        ProcessStartInfo start = new ProcessStartInfo("/usr/bin/clang")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add("-fobjc-arc");
        start.ArgumentList.Add("-fmodules");
        start.ArgumentList.Add(
            "-fmodules-cache-path=" + Path.Combine(directory, "modules"));
        start.ArgumentList.Add("-framework");
        start.ArgumentList.Add("Foundation");
        start.ArgumentList.Add("-framework");
        start.ArgumentList.Add("CoreGraphics");
        start.ArgumentList.Add("-framework");
        start.ArgumentList.Add("PDFKit");
        start.ArgumentList.Add(sourcePath);
        start.ArgumentList.Add("-o");
        start.ArgumentList.Add(executablePath);

        using Process? process = Process.Start(start);
        string output = process!.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        bool exited = process.WaitForExit(60000);

        Assert.Multiple(() =>
        {
            Assert.That(exited, Is.True, "Apple consumer compilation timed out.");
            Assert.That(process.ExitCode, Is.Zero, output + error);
        });
    }

    const string AppleConsumerSource = """
        #import <Foundation/Foundation.h>
        #import <CoreGraphics/CoreGraphics.h>
        #import <PDFKit/PDFKit.h>

        static int fail(int code, NSString *message)
        {
            fprintf(stderr, "%s\n", message.UTF8String);
            return code;
        }

        static NSData *hexData(NSString *value)
        {
            NSMutableData *data = [NSMutableData data];
            NSUInteger index = 0;
            while (index < value.length)
            {
                unsigned int byteValue = 0;
                NSString *pair = [value substringWithRange:NSMakeRange(index, 2)];
                NSScanner *scanner = [NSScanner scannerWithString:pair];
                [scanner scanHexInt:&byteValue];
                unsigned char byte = (unsigned char)byteValue;
                [data appendBytes:&byte length:1];
                index += 2;
            }
            return data;
        }

        int main(int argc, const char *argv[])
        {
            @autoreleasepool
            {
                if (argc < 8 || (argc - 1) % 7 != 0)
                {
                    return fail(2, @"Invalid consumer arguments.");
                }

                int argument = 1;
                while (argument < argc)
                {
                    NSString *path = [NSString stringWithUTF8String:argv[argument]];
                    NSString *expectedText = [NSString stringWithUTF8String:argv[argument + 1]];
                    CGPDFInteger expectedDw = atoll(argv[argument + 2]);
                    NSString *expectedMapHex = [NSString stringWithUTF8String:argv[argument + 3]];
                    BOOL expectVertical = atoi(argv[argument + 4]) != 0;
                    CFIndex expectedFontLength = atoll(argv[argument + 5]);
                    size_t expectedGlyphCount = (size_t)atoll(argv[argument + 6]);
                    NSURL *url = [NSURL fileURLWithPath:path];
                    CGPDFDocumentRef document = CGPDFDocumentCreateWithURL((__bridge CFURLRef)url);
                    if (document == NULL || CGPDFDocumentGetNumberOfPages(document) != 1)
                    {
                        return fail(10, @"Core Graphics could not load one PDF page.");
                    }

                    CGPDFPageRef page = CGPDFDocumentGetPage(document, 1);
                    CGPDFDictionaryRef pageDictionary = CGPDFPageGetDictionary(page);
                    CGPDFDictionaryRef resources = NULL;
                    CGPDFDictionaryRef fonts = NULL;
                    CGPDFDictionaryRef rootFont = NULL;
                    if (!CGPDFDictionaryGetDictionary(pageDictionary, "Resources", &resources) ||
                        !CGPDFDictionaryGetDictionary(resources, "Font", &fonts) ||
                        !CGPDFDictionaryGetDictionary(fonts, "F1", &rootFont))
                    {
                        return fail(11, @"Core Graphics could not resolve the page font.");
                    }

                    CGPDFArrayRef descendants = NULL;
                    CGPDFDictionaryRef descendant = NULL;
                    if (!CGPDFDictionaryGetArray(rootFont, "DescendantFonts", &descendants) ||
                        !CGPDFArrayGetDictionary(descendants, 0, &descendant))
                    {
                        return fail(12, @"Core Graphics could not resolve the CID descendant.");
                    }

                    CGPDFInteger actualDw = 0;
                    CGPDFObjectRef widths = NULL;
                    if (!CGPDFDictionaryGetInteger(descendant, "DW", &actualDw) ||
                        actualDw != expectedDw ||
                        !CGPDFDictionaryGetObject(descendant, "W", &widths))
                    {
                        return fail(13, @"Core Graphics reported incorrect CID widths.");
                    }

                    CGPDFObjectRef verticalWidths = NULL;
                    BOOL hasVerticalWidths = CGPDFDictionaryGetObject(
                        descendant, "W2", &verticalWidths);
                    if (hasVerticalWidths != expectVertical)
                    {
                        return fail(14, @"Core Graphics reported incorrect vertical widths.");
                    }

                    CGPDFStreamRef cidMap = NULL;
                    if (!CGPDFDictionaryGetStream(descendant, "CIDToGIDMap", &cidMap))
                    {
                        return fail(15, @"Core Graphics did not resolve the CID-to-GID stream.");
                    }
                    CGPDFDataFormat mapFormat;
                    CFDataRef mapData = CGPDFStreamCopyData(cidMap, &mapFormat);
                    NSData *expectedMap = hexData(expectedMapHex);
                    if (![(__bridge NSData *)mapData isEqualToData:expectedMap])
                    {
                        return fail(16, @"Core Graphics reported incorrect CID-to-GID bytes.");
                    }
                    CFRelease(mapData);

                    CGPDFDictionaryRef descriptor = NULL;
                    CGPDFStreamRef fontStream = NULL;
                    if (!CGPDFDictionaryGetDictionary(descendant, "FontDescriptor", &descriptor) ||
                        !CGPDFDictionaryGetStream(descriptor, "FontFile2", &fontStream))
                    {
                        return fail(17, @"Core Graphics did not resolve the embedded font.");
                    }
                    CGPDFDataFormat fontFormat;
                    CFDataRef fontData = CGPDFStreamCopyData(fontStream, &fontFormat);
                    if (CFDataGetLength(fontData) != expectedFontLength)
                    {
                        return fail(18, @"Core Graphics reported an incorrect font stream length.");
                    }
                    CGDataProviderRef provider = CGDataProviderCreateWithCFData(fontData);
                    CGFontRef font = CGFontCreateWithDataProvider(provider);
                    if (font == NULL || CGFontGetNumberOfGlyphs(font) != expectedGlyphCount)
                    {
                        return fail(19, @"Core Graphics could not inspect the embedded subset.");
                    }
                    CGFontRelease(font);
                    CGDataProviderRelease(provider);
                    CFRelease(fontData);
                    CGPDFDocumentRelease(document);

                    PDFDocument *pdfKitDocument = [[PDFDocument alloc] initWithURL:url];
                    if (pdfKitDocument == nil || pdfKitDocument.pageCount != 1)
                    {
                        return fail(20, @"PDFKit could not load one PDF page.");
                    }
                    NSString *text = [[pdfKitDocument pageAtIndex:0].string
                        stringByTrimmingCharactersInSet:
                            NSCharacterSet.whitespaceAndNewlineCharacterSet];
                    if (![text isEqualToString:expectedText])
                    {
                        NSString *message = [NSString stringWithFormat:
                            @"PDFKit extracted '%@' instead of '%@'.", text, expectedText];
                        return fail(21, message);
                    }

                    printf("CoreGraphics:OK PDFKit:OK %s\n", expectedText.UTF8String);
                    argument += 7;
                }
            }
            return 0;
        }
        """;
}

internal class PdfConsumerCase
{
    internal PdfFontResource resource;
    internal byte[] pdf;
    internal string expectedText;
    internal bool vertical;

    internal PdfConsumerCase(
        PdfFontResource resource,
        byte[] pdf,
        string expectedText,
        bool vertical)
    {
        this.resource = resource;
        this.pdf = pdf;
        this.expectedText = expectedText;
        this.vertical = vertical;
    }
}
