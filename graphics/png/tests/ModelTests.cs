using System.Reflection;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ModelTests
{
    [Test]
    public void ModelClasses_UseVisibilityThatMatchesTheirApiStatus()
    {
        Type[] modelTypes = new Type[]
        {
            typeof(PngImage),
            typeof(PngImageInfo),
            typeof(PngIhdr),
            typeof(PngPaletteEntry),
            typeof(PngPalette),
            typeof(PngPaletteHistogram),
            typeof(PngPhysicalPixels),
            typeof(PngSuggestedPaletteEntry),
            typeof(PngSuggestedPalette),
            typeof(PngIndexedTransparency),
            typeof(PngGrayscaleTransparency),
            typeof(PngTruecolorTransparency),
            typeof(PngSignificantBits),
            typeof(PngBackgroundColor),
            typeof(PngGamma),
            typeof(PngChromaticities),
            typeof(PngStandardRgb),
            typeof(PngIccProfile),
            typeof(PngExifProfile),
            typeof(PngTime),
            typeof(PngCodingIndependentCodePoints),
            typeof(PngMasteringDisplayColorVolume),
            typeof(PngContentLightLevelInformation),
            typeof(PngColorMetadata),
            typeof(PngText),
            typeof(PngCompressedText),
            typeof(PngInternationalText),
            typeof(PngTextEntry),
            typeof(PngIend),
            typeof(PngScanlineLayout),
            typeof(PngPixelBuffer),
            typeof(PngFrame),
            typeof(PngAnimationControl),
            typeof(PngFrameControl),
            typeof(PngFrameData),
            typeof(PngAnimation),
            typeof(PngMetadata),
            typeof(PngIdentifyResult),
            typeof(PngUnknownChunkReplacement),
            typeof(PngChunk),
            typeof(PngWarning),
            typeof(PngError),
            typeof(PngValidationResult),
            typeof(PngFilterTrial),
            typeof(PngFilterTrialResult),
            typeof(PngCompressionTrial),
            typeof(PngCompressionTrialResult),
            typeof(PngFilterStrategyTrial),
            typeof(PngFilterStrategyTrialResult),
            typeof(PngOptimizerReport),
            typeof(PngAdam7PassGeometry)
        };

        HashSet<Type> publicModelTypes = new HashSet<Type>
        {
            typeof(PngImage),
            typeof(PngImageInfo),
            typeof(PngPaletteEntry),
            typeof(PngPalette),
            typeof(PngPaletteHistogram),
            typeof(PngPhysicalPixels),
            typeof(PngSuggestedPaletteEntry),
            typeof(PngSuggestedPalette),
            typeof(PngIndexedTransparency),
            typeof(PngGrayscaleTransparency),
            typeof(PngTruecolorTransparency),
            typeof(PngSignificantBits),
            typeof(PngBackgroundColor),
            typeof(PngGamma),
            typeof(PngChromaticities),
            typeof(PngStandardRgb),
            typeof(PngIccProfile),
            typeof(PngExifProfile),
            typeof(PngTime),
            typeof(PngCodingIndependentCodePoints),
            typeof(PngMasteringDisplayColorVolume),
            typeof(PngContentLightLevelInformation),
            typeof(PngColorMetadata),
            typeof(PngText),
            typeof(PngCompressedText),
            typeof(PngInternationalText),
            typeof(PngTextEntry),
            typeof(PngCompressionTrial),
            typeof(PngCompressionTrialResult),
            typeof(PngFilterStrategyTrial),
            typeof(PngFilterStrategyTrialResult),
            typeof(PngOptimizerReport),
            typeof(PngPixelBuffer),
            typeof(PngFrame),
            typeof(PngAnimationControl),
            typeof(PngFrameControl),
            typeof(PngFrameData),
            typeof(PngAnimation),
            typeof(PngMetadata),
            typeof(PngIdentifyResult),
            typeof(PngUnknownChunkReplacement),
            typeof(PngChunk),
            typeof(PngWarning),
            typeof(PngError),
            typeof(PngValidationResult)
        };

        foreach (Type modelType in modelTypes)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            bool isPublicApi = publicModelTypes.Contains(modelType);

            Assert.That(modelType.IsPublic, Is.EqualTo(isPublicApi), modelType.Name);
            Assert.That(modelType.GetProperties(flags), Is.Empty, modelType.Name);
            Assert.That(modelType.GetEvents(flags), Is.Empty, modelType.Name);
            Assert.That(modelType.GetMethods(flags), Is.Empty, modelType.Name);

            FieldInfo[] fields = modelType.GetFields(flags);
            Assert.That(fields, Is.Not.Empty, modelType.Name);
            foreach (FieldInfo field in fields)
            {
                Assert.That(field.IsPublic, Is.EqualTo(isPublicApi), modelType.Name + "." + field.Name);
                Assert.That(field.IsAssembly, Is.EqualTo(!isPublicApi), modelType.Name + "." + field.Name);
            }
        }
    }

    [Test]
    public void ImageModel_DefaultsNestedModelsAndWarnings()
    {
        PngImage image = new PngImage();

        Assert.That(image.Info, Is.Not.Null);
        Assert.That(image.Pixels, Is.Not.Null);
        Assert.That(image.Animation, Is.Not.Null);
        Assert.That(image.Metadata, Is.Not.Null);
        Assert.That(image.Warnings, Is.Not.Null);
        Assert.That(image.Warnings, Is.Empty);
        Assert.That(image.Errors, Is.Not.Null);
        Assert.That(image.Errors, Is.Empty);
    }

    [Test]
    public void PixelBuffer_DefaultsToEmptyUnknownBuffer()
    {
        PngPixelBuffer buffer = new PngPixelBuffer();

        Assert.That(buffer.Format, Is.EqualTo(PngPixelFormat.Unknown));
        Assert.That(buffer.Width, Is.EqualTo(0));
        Assert.That(buffer.Height, Is.EqualTo(0));
        Assert.That(buffer.Stride, Is.EqualTo(0));
        Assert.That(buffer.Data, Is.Not.Null);
        Assert.That(buffer.Data, Is.Empty);
    }

    [Test]
    public void AnimationModel_DefaultsToNoFrames()
    {
        PngAnimation animation = new PngAnimation();
        PngFrame frame = new PngFrame();

        Assert.That(animation.FrameCount, Is.EqualTo(0));
        Assert.That(animation.PlayCount, Is.EqualTo(0));
        Assert.That(animation.Frames, Is.Not.Null);
        Assert.That(animation.Frames, Is.Empty);
        Assert.That(animation.ComposedFrames, Is.Not.Null);
        Assert.That(animation.ComposedFrames, Is.Empty);

        Assert.That(frame.Pixels, Is.Not.Null);
        Assert.That(frame.BlendOperation, Is.EqualTo(PngApngBlendOperation.Source));
        Assert.That(frame.DisposeOperation, Is.EqualTo(PngApngDisposeOperation.None));
    }

    [Test]
    public void MetadataAndChunkModels_DefaultToEmptyRawData()
    {
        PngMetadata metadata = new PngMetadata();
        PngChunk chunk = new PngChunk();

        Assert.That(metadata.Chunks, Is.Not.Null);
        Assert.That(metadata.Chunks, Is.Empty);
        Assert.That(metadata.UnknownChunks, Is.Not.Null);
        Assert.That(metadata.UnknownChunks, Is.Empty);
        Assert.That(metadata.IndexedTransparency, Is.Null);
        Assert.That(metadata.GrayscaleTransparency, Is.Null);
        Assert.That(metadata.TruecolorTransparency, Is.Null);
        Assert.That(metadata.Chromaticities, Is.Null);
        Assert.That(metadata.Gamma, Is.Null);
        Assert.That(metadata.IccProfile, Is.Null);
        Assert.That(metadata.SignificantBits, Is.Null);
        Assert.That(metadata.StandardRgb, Is.Null);
        Assert.That(metadata.CodingIndependentCodePoints, Is.Null);
        Assert.That(metadata.ColorMetadata, Is.Not.Null);
        Assert.That(metadata.ColorMetadata.HasColorMetadata, Is.False);
        Assert.That(metadata.MasteringDisplayColorVolume, Is.Null);
        Assert.That(metadata.ContentLightLevelInformation, Is.Null);
        Assert.That(metadata.TextEntries, Is.Not.Null);
        Assert.That(metadata.TextEntries, Is.Empty);
        Assert.That(metadata.Latin1TextEntries, Is.Not.Null);
        Assert.That(metadata.Latin1TextEntries, Is.Empty);
        Assert.That(metadata.CompressedTextEntries, Is.Not.Null);
        Assert.That(metadata.CompressedTextEntries, Is.Empty);
        Assert.That(metadata.InternationalTextEntries, Is.Not.Null);
        Assert.That(metadata.InternationalTextEntries, Is.Empty);
        Assert.That(metadata.BackgroundColor, Is.Null);
        Assert.That(metadata.PaletteHistogram, Is.Null);
        Assert.That(metadata.PhysicalPixels, Is.Null);
        Assert.That(metadata.SuggestedPalettes, Is.Not.Null);
        Assert.That(metadata.SuggestedPalettes, Is.Empty);
        Assert.That(metadata.ExifProfile, Is.Null);
        Assert.That(metadata.Time, Is.Null);
        Assert.That(metadata.AnimationControl, Is.Null);
        Assert.That(metadata.FrameControls, Is.Not.Null);
        Assert.That(metadata.FrameControls, Is.Empty);
        Assert.That(metadata.FrameData, Is.Not.Null);
        Assert.That(metadata.FrameData, Is.Empty);
        Assert.That(chunk.Placement, Is.EqualTo(PngChunkPlacement.Unspecified));
        Assert.That(chunk.PreservationState, Is.EqualTo(PngChunkPreservationState.Created));
        Assert.That(metadata.MetadataBytes, Is.EqualTo(0));
        Assert.That(metadata.TextBytes, Is.EqualTo(0));
        Assert.That(metadata.TextEntryCount, Is.EqualTo(0));

        Assert.That(chunk.Type, Is.EqualTo(""));
        Assert.That(chunk.Data, Is.Not.Null);
        Assert.That(chunk.Data, Is.Empty);
        Assert.That(chunk.Crc, Is.EqualTo(0));
        Assert.That(chunk.Offset, Is.EqualTo(0));
        Assert.That(chunk.Length, Is.EqualTo(0));
        Assert.That(chunk.IsKnown, Is.False);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.False);
        Assert.That(chunk.IsReservedBitValid, Is.False);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WarningErrorAndValidationModels_DefaultToEmptyMessages()
    {
        PngWarning warning = new PngWarning();
        PngError error = new PngError();
        PngValidationResult result = new PngValidationResult();

        Assert.That(warning.Code, Is.EqualTo(""));
        Assert.That(warning.Message, Is.EqualTo(""));
        Assert.That(warning.ChunkType, Is.EqualTo(""));
        Assert.That(warning.Offset, Is.EqualTo(0));

        Assert.That(error.Code, Is.EqualTo(""));
        Assert.That(error.Message, Is.EqualTo(""));
        Assert.That(error.ChunkType, Is.EqualTo(""));
        Assert.That(error.Offset, Is.EqualTo(0));
        Assert.That(error.IsFatal, Is.False);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Info, Is.Not.Null);
        Assert.That(result.Warnings, Is.Not.Null);
        Assert.That(result.Warnings, Is.Empty);
        Assert.That(result.Errors, Is.Not.Null);
        Assert.That(result.Errors, Is.Empty);
    }
}
