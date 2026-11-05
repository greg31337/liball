using System.Collections.Generic;
using System.IO;
using System.Text;

namespace pptx;

internal static class PptxAssetExtractorCore
{
    internal static PptxAssetExtractionResult ExtractPresentationImages(PptxPresentation presentation, string directoryPath)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        string directory = EnsureDirectory(directoryPath);
        PptxAssetExtractionResult result = new PptxAssetExtractionResult();
        for (int index = 0; index < presentation.SlideCount(); index++)
        {
            AddSlideImages(presentation, presentation.GetSlide(index), index, directory, result);
        }

        return result;
    }
    internal static PptxAssetExtractionResult ExtractSlideImages(PptxPresentation presentation, PptxSlide slide, string directoryPath)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        string directory = EnsureDirectory(directoryPath);
        PptxAssetExtractionResult result = new PptxAssetExtractionResult();
        AddSlideImages(presentation, slide, FindSlideIndex(presentation, slide), directory, result);
        return result;
    }
    internal static PptxAssetExtractionResult ExtractPresentationMedia(PptxPresentation presentation, string directoryPath)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        string directory = EnsureDirectory(directoryPath);
        PptxAssetExtractionResult result = new PptxAssetExtractionResult();
        for (int index = 0; index < presentation.SlideCount(); index++)
        {
            AddSlideMedia(presentation, presentation.GetSlide(index), index, directory, result);
        }

        return result;
    }
    internal static PptxAssetExtractionResult ExtractSlideMedia(PptxPresentation presentation, PptxSlide slide, string directoryPath)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        string directory = EnsureDirectory(directoryPath);
        PptxAssetExtractionResult result = new PptxAssetExtractionResult();
        AddSlideMedia(presentation, slide, FindSlideIndex(presentation, slide), directory, result);
        return result;
    }

    internal static void AddSlideImages(PptxPresentation presentation, PptxSlide slide, int slideIndex, string directoryPath, PptxAssetExtractionResult result)
    {
        List<PptxImageFile> images = presentation.ExtractImages(slide);
        for (int index = 0; index < images.Count; index++)
        {
            PptxImageFile image = images[index];
            PptxExtractedAsset asset = CreateImageAsset(slide, slideIndex, image, directoryPath);
            WriteAsset(asset);
            result.Add(asset);
        }
    }

    internal static void AddSlideMedia(PptxPresentation presentation, PptxSlide slide, int slideIndex, string directoryPath, PptxAssetExtractionResult result)
    {
        List<PptxMediaFile> mediaFiles = presentation.GetSlideMedia(slide);
        for (int index = 0; index < mediaFiles.Count; index++)
        {
            PptxMediaFile media = mediaFiles[index];
            PptxExtractedAsset asset = CreateMediaAsset(presentation, slide, slideIndex, media, directoryPath);
            if (asset.Embedded && presentation.Package.HasPart(asset.PartName))
            {
                WriteAsset(asset);
            }

            result.Add(asset);

            if (media.HasPosterFrame && presentation.Package.HasPart(media.PosterPartName))
            {
                PptxExtractedAsset poster = CreatePosterAsset(slide, slideIndex, media, directoryPath);
                WriteAsset(poster);
                result.Add(poster);
            }
        }
    }

    internal static PptxExtractedAsset CreateImageAsset(PptxSlide slide, int slideIndex, PptxImageFile image, string directoryPath)
    {
        PptxExtractedAsset asset = new PptxExtractedAsset();
        asset.Kind = "image";
        asset.SlideIndex = slideIndex;
        asset.SlidePartName = slide.PartName;
        asset.RelationshipId = image.RelationshipId;
        asset.PartName = image.PartName;
        asset.ContentType = image.Info.ContentType;
        asset.Embedded = true;
        asset.Bytes = image.Bytes;
        asset.ImageInfo = image.Info;
        asset.FileName = CreateFileName(slideIndex, "image", image.RelationshipId, image.PartName);
        asset.FilePath = Path.Combine(directoryPath, asset.FileName);
        return asset;
    }

    internal static PptxExtractedAsset CreateMediaAsset(PptxPresentation presentation, PptxSlide slide, int slideIndex, PptxMediaFile media, string directoryPath)
    {
        PptxExtractedAsset asset = new PptxExtractedAsset();
        asset.Kind = "media";
        asset.SlideIndex = slideIndex;
        asset.SlidePartName = slide.PartName;
        asset.RelationshipId = media.RelationshipId;
        asset.PartName = media.PartName;
        asset.Target = media.Target;
        asset.ContentType = media.ContentType;
        asset.MediaType = media.Type;
        asset.Embedded = media.Embedded;
        asset.Linked = media.Linked;
        asset.Bytes = media.Bytes;
        if (asset.Embedded && presentation.Package.HasPart(asset.PartName))
        {
            asset.FileName = CreateFileName(slideIndex, media.Type, media.RelationshipId, media.PartName);
            asset.FilePath = Path.Combine(directoryPath, asset.FileName);
        }

        return asset;
    }

    internal static PptxExtractedAsset CreatePosterAsset(PptxSlide slide, int slideIndex, PptxMediaFile media, string directoryPath)
    {
        PptxExtractedAsset asset = new PptxExtractedAsset();
        asset.Kind = "poster";
        asset.SlideIndex = slideIndex;
        asset.SlidePartName = slide.PartName;
        asset.RelationshipId = media.PosterRelationshipId;
        asset.PartName = media.PosterPartName;
        asset.ContentType = media.PosterInfo.ContentType;
        asset.MediaType = media.Type;
        asset.Embedded = true;
        asset.Bytes = media.PosterBytes;
        asset.ImageInfo = media.PosterInfo;
        asset.FileName = CreateFileName(slideIndex, "poster", media.PosterRelationshipId, media.PosterPartName);
        asset.FilePath = Path.Combine(directoryPath, asset.FileName);
        return asset;
    }

    internal static void WriteAsset(PptxExtractedAsset asset)
    {
        File.WriteAllBytes(asset.FilePath, asset.Bytes);
        asset.Written = true;
    }

    internal static string EnsureDirectory(string directoryPath)
    {
        if (directoryPath == null)
        {
            throw new System.ArgumentNullException("directoryPath");
        }

        string directory = PptxDiagnostic.CleanText(directoryPath).Trim();
        if (directory.Length == 0)
        {
            throw new System.ArgumentException("Extraction directory is required.");
        }

        Directory.CreateDirectory(directory);
        return directory;
    }

    internal static int FindSlideIndex(PptxPresentation presentation, PptxSlide slide)
    {
        for (int index = 0; index < presentation.SlideCount(); index++)
        {
            if (presentation.GetSlide(index).PartName == slide.PartName)
            {
                return index;
            }
        }

        return -1;
    }

    internal static string CreateFileName(int slideIndex, string kind, string relationshipId, string partName)
    {
        string baseName = PptxSlideCloner.GetFileName(partName);
        if (baseName.Length == 0)
        {
            baseName = "asset.bin";
        }

        return CreateSlideLabel(slideIndex) + "_" + CleanFileNamePart(kind, "asset") + "_" + CleanFileNamePart(relationshipId, "relationship") + "_" + CleanFileNamePart(baseName, "asset.bin");
    }

    internal static string CreateSlideLabel(int slideIndex)
    {
        if (slideIndex < 0)
        {
            return "slide";
        }

        return "slide" + (slideIndex + 1).ToString();
    }

    internal static string CleanFileNamePart(string value, string fallback)
    {
        string clean = PptxDiagnostic.CleanText(value);
        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < clean.Length; index++)
        {
            char item = clean[index];
            if (IsSafeFileNameCharacter(item))
            {
                builder.Append(item);
            }
            else
            {
                builder.Append("_");
            }
        }

        string result = builder.ToString();
        if (result.Length == 0)
        {
            return fallback;
        }

        return result;
    }

    internal static bool IsSafeFileNameCharacter(char value)
    {
        if (value >= 'a' && value <= 'z')
        {
            return true;
        }

        if (value >= 'A' && value <= 'Z')
        {
            return true;
        }

        if (value >= '0' && value <= '9')
        {
            return true;
        }

        if (value == '.' || value == '_' || value == '-')
        {
            return true;
        }

        return false;
    }
}

