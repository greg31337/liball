using System;
using System.Collections.Generic;

namespace avif;

internal enum AvifValidationRequirement
{
    Mandatory = 0,
    Interoperable = 1,
    Advisory = 2,
    Hard = 3
}

internal class AvifContainerValidationResult
{
    internal AvifResultCode Code;
    internal FileTypeBox? FileType;
    internal ParsedMeta? Meta;
    internal ParsedMovie? Movie;
    internal IsoBoxParseResult? Boxes;
    internal DiagnosticLog Diagnostics;

    internal AvifContainerValidationResult(
        AvifResultCode code,
        FileTypeBox? fileType,
        ParsedMeta? meta,
        ParsedMovie? movie,
        IsoBoxParseResult? boxes,
        DiagnosticLog diagnostics)
    {
        Code = code;
        FileType = fileType;
        Meta = meta;
        Movie = movie;
        Boxes = boxes;
        Diagnostics = diagnostics;
    }
}

internal static class AvifContainerValidator
{
    internal static AvifContainerValidationResult Validate(
        ReadOnlySpan<byte> source,
        AvifValidationOptions options)
    {
        DiagnosticLog argumentDiagnostics = new DiagnosticLog();
        if (options == null
            || options.Limits == null
            || ResourceGuard.ValidateLimits(options.Limits) != AvifResultCode.Success
            || (options.Mode != AvifValidationMode.Strict
                && options.Mode != AvifValidationMode.Compatible
                && options.Mode != AvifValidationMode.Permissive))
        {
            argumentDiagnostics.Add(
                AvifResultCode.InvalidArgument,
                AvifDiagnosticSeverity.Error,
                AvifDiagnosticCategory.Api,
                "The validation options or resource limits are invalid.");
            return new AvifContainerValidationResult(
                argumentDiagnostics.Code,
                null,
                null,
                null,
                null,
                argumentDiagnostics);
        }

        IsoBoxParseResult boxes = IsoBoxReader.Parse(source, options.Limits, false);
        if (boxes.Code != AvifResultCode.Success)
        {
            return new AvifContainerValidationResult(
                boxes.Code,
                null,
                null,
                null,
                boxes,
                boxes.Diagnostics);
        }

        DiagnosticLog diagnostics = boxes.Diagnostics;
        IsoBox? fileTypeBox = null;
        IsoBox? metaBox = null;
        IsoBox? miniBox = null;
        IsoBox? movieBox = null;
        int fileTypeCount = 0;
        int metaCount = 0;
        int miniCount = 0;
        int movieCount = 0;
        int index = 0;
        while (index < boxes.Boxes.Count)
        {
            IsoBox box = boxes.Boxes[index];
            if (box.Type == "ftyp")
            {
                fileTypeCount++;
                if (fileTypeBox == null)
                {
                    fileTypeBox = box;
                }
            }
            else if (box.Type == "meta")
            {
                metaCount++;
                if (metaBox == null)
                {
                    metaBox = box;
                }
            }
            else if (box.Type == "mini")
            {
                miniCount++;
                if (miniBox == null)
                {
                    miniBox = box;
                }
            }
            else if (box.Type == "moov")
            {
                movieCount++;
                if (movieBox == null)
                {
                    movieBox = box;
                }
            }

            index++;
        }

        if (fileTypeCount != 1)
        {
            AddFinding(
                diagnostics,
                options,
                fileTypeCount == 0
                    ? AvifValidationRequirement.Mandatory
                    : AvifValidationRequirement.Hard,
                fileTypeCount == 0
                    ? AvifResultCode.NonConformingData
                    : AvifResultCode.InvalidContainer,
                AvifDiagnosticCategory.Validation,
                "An AVIF file must contain exactly one FileTypeBox.",
                "AVIF 1.2 section 9.1.1");
        }

        if (fileTypeBox != null && boxes.Boxes.Count > 0 && boxes.Boxes[0] != fileTypeBox)
        {
            diagnostics.Context.PushBox("ftyp");
            diagnostics.Context.SetLocation(fileTypeBox.Offset, AvifDiagnostic.UnknownBitOffset);
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Interoperable,
                AvifResultCode.NonConformingData,
                AvifDiagnosticCategory.Validation,
                "The FileTypeBox must be the first box in an AVIF file.",
                "AVIF 1.2 section 9.1.1");
            diagnostics.Context.ClearLocation();
            diagnostics.Context.PopBox();
        }

        if (miniCount == 0 && metaCount != 1)
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Hard,
                AvifResultCode.InvalidContainer,
                AvifDiagnosticCategory.Validation,
                "An AVIF image-item file must contain exactly one MetaBox.",
                "AVIF 1.2 section 9.1.1");
        }

        if (miniCount > 0 && (miniCount != 1 || metaCount != 0))
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Hard,
                AvifResultCode.InvalidContainer,
                AvifDiagnosticCategory.Validation,
                "A minimized image file must contain exactly one MinimizedImageBox and no MetaBox.",
                "ISO/IEC 23008-12 Annex O");
        }

        if (movieCount > 1)
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Hard,
                AvifResultCode.InvalidContainer,
                AvifDiagnosticCategory.Validation,
                "An AVIF file may contain at most one MovieBox.",
                "ISO/IEC 14496-12 MovieBox");
        }

        FileTypeBox? fileType = null;
        if (fileTypeBox != null)
        {
            fileType = FileTypeBox.Parse(
                source,
                fileTypeBox,
                boxes.Resources);
            if (fileType.Code != AvifResultCode.Success)
            {
                diagnostics.Context.PushBox("ftyp");
                diagnostics.Context.SetLocation(
                    fileTypeBox.PayloadOffset,
                    AvifDiagnostic.UnknownBitOffset);
                diagnostics.Add(
                    fileType.Code,
                    AvifDiagnosticSeverity.Error,
                    AvifDiagnosticCategory.Container,
                    "The FileTypeBox is malformed.");
                diagnostics.Context.ClearLocation();
                diagnostics.Context.PopBox();
            }
        }

        ParsedMovie? movie = null;
        if (!ShouldStop(diagnostics, options) && movieCount == 1)
        {
            MovieParseResult movieResult = MovieBoxParser.Parse(
                source,
                movieBox,
                boxes.Boxes,
                boxes.Resources,
                diagnostics);
            if (movieResult.Code != AvifResultCode.Success)
            {
                return Finish(fileType, null, null, boxes, diagnostics);
            }
            movie = movieResult.Movie;
        }

        if (ShouldStop(diagnostics, options)
            || (miniCount == 0 && (metaBox == null || metaCount != 1))
            || (miniCount != 0 && (miniBox == null || miniCount != 1 || metaCount != 0)))
        {
            return Finish(fileType, null, movie, boxes, diagnostics);
        }

        bool minimized = miniCount == 1;
        MetaParseResult parsed = minimized
            ? MinimizedImageBox.ParseAndExpand(source, miniBox, boxes, fileType)
            : MetaBoxParser.ParseMeta(source, metaBox, boxes);
        if (parsed.Code != AvifResultCode.Success)
        {
            return new AvifContainerValidationResult(
                parsed.Code,
                fileType,
                null,
                movie,
                boxes,
                boxes.Diagnostics);
        }

        ParsedMeta? meta = parsed.Meta;
        if (!minimized)
        {
            ValidateRequiredBoxes(metaBox, boxes.Boxes, meta, diagnostics, options);
            if (!ShouldStop(diagnostics, options))
            {
                ValidateBoxVersions(source, metaBox, diagnostics, options);
            }
        }

        if (!ShouldStop(diagnostics, options) && !minimized)
        {
            ValidateBrands(fileType, meta, diagnostics, options);
        }

        if (!ShouldStop(diagnostics, options))
        {
            AvifSequenceValidator.Validate(
                source,
                fileType,
                movie,
                options,
                diagnostics);
        }

        if (!ShouldStop(diagnostics, options))
        {
            ValidatePrimaryItem(meta, diagnostics, options);
        }

        if (!ShouldStop(diagnostics, options))
        {
            ValidateItemProperties(meta, diagnostics, options);
        }

        if (!ShouldStop(diagnostics, options))
        {
            ValidateAuxiliaryItems(meta, diagnostics, options);
        }

        if (!ShouldStop(diagnostics, options))
        {
            ValidateGridTransforms(meta, diagnostics, options);
        }

        if (!ShouldStop(diagnostics, options))
        {
            ValidateProfiles(fileType, meta, diagnostics, options);
        }

        if (!ShouldStop(diagnostics, options) && options.ValidateEncodedPayload)
        {
            ValidatePayloads(source, meta, diagnostics, options);
        }

        if (!ShouldStop(diagnostics, options) && options.DecodePixelData)
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Hard,
                AvifResultCode.UnsupportedFeature,
                AvifDiagnosticCategory.Validation,
                "Pixel-decode validation is not implemented by the current validation layer.",
                "AVIF validation capability");
        }

        if (diagnostics.Code == AvifResultCode.Success && options.CollectInformation)
        {
            diagnostics.Add(
                AvifResultCode.Success,
                AvifDiagnosticSeverity.Information,
                AvifDiagnosticCategory.Validation,
                "Container validation completed under the selected conformance policy.");
        }

        return Finish(fileType, meta, movie, boxes, diagnostics);
    }

    internal static AvifContainerValidationResult Finish(
        FileTypeBox? fileType,
        ParsedMeta? meta,
        ParsedMovie? movie,
        IsoBoxParseResult boxes,
        DiagnosticLog diagnostics)
    {
        return new AvifContainerValidationResult(
            diagnostics.Code,
            fileType,
            meta,
            movie,
            boxes,
            diagnostics);
    }

    internal static void ValidateRequiredBoxes(
        IsoBox? metaBox,
        List<IsoBox> topLevelBoxes,
        ParsedMeta? meta,
        DiagnosticLog diagnostics,
        AvifValidationOptions options)
    {
        string[] required = new string[] { "hdlr", "pitm", "iloc", "iinf", "iprp" };
        int requiredIndex = 0;
        while (requiredIndex < required.Length)
        {
            int count = CountChildren(metaBox, required[requiredIndex]);
            if (count != 1)
            {
                diagnostics.Context.PushBox("meta");
                AddFinding(
                    diagnostics,
                    options,
                    AvifValidationRequirement.Mandatory,
                    AvifResultCode.NonConformingData,
                    AvifDiagnosticCategory.Validation,
                    "The MetaBox must contain exactly one " + required[requiredIndex] + " box.",
                    "AVIF 1.2 section 9.1.1");
                diagnostics.Context.PopBox();
                if (ShouldStop(diagnostics, options))
                {
                    return;
                }
            }

            requiredIndex++;
        }

        if ((meta ?? throw new InvalidOperationException("Invalid internal state.")).GetHandlerType() != "pict")
        {
            diagnostics.Context.PushBox("meta");
            diagnostics.Context.PushBox("hdlr");
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                AvifResultCode.NonConformingData,
                AvifDiagnosticCategory.Validation,
                "The item MetaBox handler type must be pict.",
                "MIAF image-item handler");
            diagnostics.Context.PopBox();
            diagnostics.Context.PopBox();
        }

        bool hasMediaData = false;
        int topIndex = 0;
        while (topIndex < topLevelBoxes.Count)
        {
            if (topLevelBoxes[topIndex].Type == "mdat")
            {
                hasMediaData = true;
                break;
            }

            topIndex++;
        }

        if (!hasMediaData && !meta.HasItemDataBox())
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                AvifResultCode.NonConformingData,
                AvifDiagnosticCategory.Validation,
                "Image item payloads require an mdat box or an idat box.",
                "AVIF 1.2 section 9.1.1");
        }
    }

    internal static int CountChildren(IsoBox? parent, string type)
    {
        int count = 0;
        int index = 0;
        while (index < (parent ?? throw new InvalidOperationException("Invalid internal state.")).Children.Count)
        {
            if (parent.Children[index].Type == type)
            {
                count++;
            }

            index++;
        }

        return count;
    }

    internal static void ValidateBoxVersions(
        ReadOnlySpan<byte> source,
        IsoBox? metaBox,
        DiagnosticLog diagnostics,
        AvifValidationOptions options)
    {
        if (metaBox == null)
        {
            return;
        }
        List<IsoBox> pending = new List<IsoBox>();
        pending.Add(metaBox);
        int index = 0;
        while (index < pending.Count)
        {
            IsoBox box = pending[index];
            int minimum = -1;
            int maximum = -1;
            if (box.Type == "meta" || box.Type == "hdlr" || box.Type == "dref")
            {
                minimum = 0;
                maximum = 0;
            }
            else if (box.Type == "pitm" || box.Type == "iinf" || box.Type == "ipma"
                || box.Type == "iref")
            {
                minimum = 0;
                maximum = 1;
            }
            else if (box.Type == "iloc")
            {
                minimum = 0;
                maximum = 2;
            }
            else if (box.Type == "infe")
            {
                minimum = 2;
                maximum = 3;
            }
            else if (box.Type == "altr" || box.Type == "ster")
            {
                minimum = 0;
                maximum = 0;
            }

            if (minimum >= 0)
            {
                IsoFullBox fullBox = box.HasFullBoxFields
                    ? new IsoFullBox(
                        AvifResultCode.Success,
                        box.Version,
                        box.Flags,
                        box.PayloadOffset + 4,
                        box.PayloadSize - 4)
                    : IsoFullBox.Parse(source, box);
                if (fullBox.Code != AvifResultCode.Success
                    || fullBox.Version < minimum
                    || fullBox.Version > maximum)
                {
                    diagnostics.Context.PushBox(box.Type);
                    diagnostics.Context.SetLocation(
                        box.PayloadOffset,
                        AvifDiagnostic.UnknownBitOffset);
                    AddFinding(
                        diagnostics,
                        options,
                        AvifValidationRequirement.Mandatory,
                        AvifResultCode.NonConformingData,
                        AvifDiagnosticCategory.Validation,
                        "The " + box.Type + " box uses a version outside the AVIF brand requirements.",
                        "AVIF 1.2 sections 9.1.1 and 9.1.2");
                    diagnostics.Context.ClearLocation();
                    diagnostics.Context.PopBox();
                    if (ShouldStop(diagnostics, options))
                    {
                        return;
                    }
                }
            }

            int childIndex = 0;
            while (childIndex < box.Children.Count)
            {
                pending.Add(box.Children[childIndex]);
                childIndex++;
            }

            index++;
        }
    }

    internal static void ValidateBrands(
        FileTypeBox? fileType,
        ParsedMeta? meta,
        DiagnosticLog diagnostics,
        AvifValidationOptions options)
    {
        if (fileType == null || fileType.Code != AvifResultCode.Success)
        {
            return;
        }

        diagnostics.Context.PushBox("ftyp");
        if (!fileType.HasBrand("miaf"))
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                AvifResultCode.NonConformingData,
                AvifDiagnosticCategory.Validation,
                "An AVIF file must list the miaf brand.",
                "AVIF 1.2 section 7");
        }

        if (!fileType.HasBrand("avif") && !fileType.HasBrand("avis"))
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                AvifResultCode.NonConformingData,
                AvifDiagnosticCategory.Validation,
                "An AVIF file must list the avif or avis brand.",
                "AVIF 1.2 section 7");
        }

        if ((fileType.HasBrand("avio")
                || fileType.HasBrand("MA1B")
                || fileType.HasBrand("MA1A"))
            && !fileType.HasBrand("avif")
            && !fileType.HasBrand("avis"))
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                AvifResultCode.NonConformingData,
                AvifDiagnosticCategory.Validation,
                "An AVIF profile or intra-only brand requires avif or avis.",
                "AVIF 1.2 sections 6 and 8");
        }

        if ((meta ?? throw new InvalidOperationException("Invalid internal state.")).GetItemDiscovery().GetCodedItemCount() > 0
            && fileType.HasBrand("avif")
            && !fileType.HasBrand("mif1"))
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Advisory,
                AvifResultCode.NonConformingData,
                AvifDiagnosticCategory.Validation,
                "An AVIF image-item file is expected to list the mif1 brand.",
                "AVIF 1.2 sections 8.2 and 8.3");
        }

        diagnostics.Context.PopBox();
    }

    internal static void ValidatePrimaryItem(
        ParsedMeta? meta,
        DiagnosticLog diagnostics,
        AvifValidationOptions options)
    {
        if (!(meta ?? throw new InvalidOperationException("Invalid internal state.")).HasPrimaryItem())
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                AvifResultCode.NonConformingData,
                AvifDiagnosticCategory.Validation,
                "An AVIF image-item file must identify a primary image item.",
                "MIAF primary image item");
            return;
        }

        ParsedItem? primary = meta.GetPrimaryItem();
        if (primary == null)
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Hard,
                AvifResultCode.InvalidItem,
                AvifDiagnosticCategory.Item,
                "The primary item identifier does not resolve to an item.",
                "ISOBMFF PrimaryItemBox");
            return;
        }

        diagnostics.Context.PushItem(primary.GetItemId());
        if (primary.IsHidden())
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                AvifResultCode.NonConformingData,
                AvifDiagnosticCategory.Validation,
                "The primary image item must not be hidden.",
                "MIAF primary image item");
        }

        if (!PrimaryResolvesToAv1(meta, primary))
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                AvifResultCode.NonConformingData,
                AvifDiagnosticCategory.Validation,
                "The primary item must be an AV1 image item or a derivation whose leaves are AV1 image items.",
                "AVIF 1.2 section 6.2");
        }

        diagnostics.Context.PopItem();
    }

    internal static bool PrimaryResolvesToAv1(ParsedMeta meta, ParsedItem primary)
    {
        if (primary.GetItemType() == "av01")
        {
            return true;
        }

        ParsedItemDependencyNode? node = (meta
            .GetItemDependencyGraph() ?? throw new InvalidOperationException("Invalid internal state."))
            .FindNode(primary.GetItemId());
        if (node == null || node.GetDependencyCount() == 0)
        {
            return false;
        }

        ParsedItemDecodePlan plan = node.GetDecodePlan();
        bool foundAv1 = false;
        int index = 0;
        while (index < plan.GetItemCount())
        {
            ParsedItem? item = plan.GetItem(index);
            ParsedItemDependencyNode? itemNode = (meta
                .GetItemDependencyGraph() ?? throw new InvalidOperationException("Invalid internal state."))
                .FindNode((item ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId());
            if (itemNode != null && itemNode.GetDependencyCount() == 0)
            {
                if (item.GetItemType() != "av01")
                {
                    return false;
                }

                foundAv1 = true;
            }

            index++;
        }

        return foundAv1;
    }

    internal static void ValidateItemProperties(
        ParsedMeta? meta,
        DiagnosticLog diagnostics,
        AvifValidationOptions options)
    {
        int itemIndex = 0;
        while (itemIndex < (meta ?? throw new InvalidOperationException("Invalid internal state.")).GetItemCount())
        {
            ParsedItem? item = meta.GetItem(itemIndex);
            diagnostics.Context.PushItem((item ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId());
            int av1ConfigurationCount = 0;
            int spatialExtentsCount = 0;
            int pixelInformationCount = 0;
            int cleanApertureCount = 0;
            int rotationCount = 0;
            int mirrorCount = 0;
            int lastTransformRank = 0;
            bool av1ConfigurationEssential = false;
            int propertyIndex = 0;
            while (propertyIndex < item.GetPropertyCount())
            {
                ParsedPropertyAssociation? association = item.GetPropertyAssociation(propertyIndex);
                ParsedItemProperty property = (association ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty();
                string type = property.GetBoxType();
                if (type == "av1C")
                {
                    av1ConfigurationCount++;
                    if (association.IsEssential())
                    {
                        av1ConfigurationEssential = true;
                    }
                }
                else if (type == "ispe")
                {
                    spatialExtentsCount++;
                }
                else if (type == "pixi")
                {
                    pixelInformationCount++;
                }

                int transformRank = GetTransformRank(type);
                if (transformRank > 0)
                {
                    if (transformRank < lastTransformRank)
                    {
                        AddFinding(
                            diagnostics,
                            options,
                            AvifValidationRequirement.Mandatory,
                            AvifResultCode.NonConformingData,
                            AvifDiagnosticCategory.Rendering,
                            "Transformative properties must be associated in clap, irot, imir order.",
                            "MIAF transformative property ordering");
                    }

                    lastTransformRank = transformRank;
                    if (type == "clap")
                    {
                        cleanApertureCount++;
                    }
                    else if (type == "irot")
                    {
                        rotationCount++;
                    }
                    else
                    {
                        mirrorCount++;
                    }
                }

                if (type == "a1op" && !association.IsEssential())
                {
                    AddFinding(
                        diagnostics,
                        options,
                        AvifValidationRequirement.Mandatory,
                        AvifResultCode.NonConformingData,
                        AvifDiagnosticCategory.Validation,
                        "An a1op property must be marked essential.",
                        "AVIF 1.2 section 2.3.2.1.2");
                }

                if (type == "a1lx" && association.IsEssential())
                {
                    AddFinding(
                        diagnostics,
                        options,
                        AvifValidationRequirement.Mandatory,
                        AvifResultCode.NonConformingData,
                        AvifDiagnosticCategory.Validation,
                        "An a1lx property must not be marked essential.",
                        "AVIF 1.2 section 2.3.2.3.1");
                }

                if (type == "pasp")
                {
                    ParsedPixelAspectRatio? ratio = property.GetPixelAspectRatio();
                    if (ratio == null
                        || ratio.GetHorizontalSpacing() != ratio.GetVerticalSpacing())
                    {
                        AddFinding(
                            diagnostics,
                            options,
                            AvifValidationRequirement.Mandatory,
                            AvifResultCode.NonConformingData,
                            AvifDiagnosticCategory.Validation,
                            "An AVIF pasp property must indicate square pixels.",
                            "AVIF 1.2 section 9.1.2");
                    }
                }

                if (!HasSupportedPropertySemantics(property))
                {
                    if (association.IsEssential())
                    {
                        AddFinding(
                            diagnostics,
                            options,
                            AvifValidationRequirement.Hard,
                            AvifResultCode.UnsupportedEssentialProperty,
                            AvifDiagnosticCategory.Item,
                            "An unknown essential item property prevents validation.",
                            "MIAF essential item properties");
                    }
                    else if (!options.AllowUnknownNonEssentialData)
                    {
                        AddFinding(
                            diagnostics,
                            options,
                            AvifValidationRequirement.Hard,
                            AvifResultCode.UnsupportedFeature,
                            AvifDiagnosticCategory.Item,
                            "Validation was configured to reject unknown non-essential item properties.",
                            "AVIF validation options");
                    }
                }

                propertyIndex++;
                if (ShouldStop(diagnostics, options))
                {
                    break;
                }
            }

            if (item.GetItemType() == "av01")
            {
                ValidatePropertyQuantity(
                    av1ConfigurationCount,
                    "av1C",
                    item,
                    diagnostics,
                    options);
                ValidatePropertyQuantity(
                    spatialExtentsCount,
                    "ispe",
                    item,
                    diagnostics,
                    options);
                ValidatePropertyQuantity(
                    pixelInformationCount,
                    "pixi",
                    item,
                    diagnostics,
                    options);
                if (av1ConfigurationCount == 1 && !av1ConfigurationEssential)
                {
                    AddFinding(
                        diagnostics,
                        options,
                        AvifValidationRequirement.Advisory,
                        AvifResultCode.NonConformingData,
                        AvifDiagnosticCategory.Validation,
                        "The av1C property should be marked essential.",
                        "AVIF 1.2 section 2.2.1");
                }

                ValidateCleanAperture(meta, item, diagnostics, options);
            }
            else if (av1ConfigurationCount != 0)
            {
                AddFinding(
                    diagnostics,
                    options,
                    AvifValidationRequirement.Mandatory,
                    AvifResultCode.NonConformingData,
                    AvifDiagnosticCategory.Validation,
                    "The av1C property must not be associated with a non-av01 item.",
                    "AVIF 1.2 section 2.2.1");
            }

            if (cleanApertureCount > 1 || rotationCount > 1 || mirrorCount > 1)
            {
                AddFinding(
                    diagnostics,
                    options,
                    AvifValidationRequirement.Mandatory,
                    AvifResultCode.NonConformingData,
                    AvifDiagnosticCategory.Rendering,
                    "An image item may have at most one property of each transformative type.",
                    "MIAF transformative property quantities");
            }

            diagnostics.Context.PopItem();
            if (ShouldStop(diagnostics, options))
            {
                return;
            }

            itemIndex++;
        }
    }

    internal static void ValidatePropertyQuantity(
        int count,
        string type,
        ParsedItem item,
        DiagnosticLog diagnostics,
        AvifValidationOptions options)
    {
        if (count != 1)
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                AvifResultCode.NonConformingData,
                AvifDiagnosticCategory.Validation,
                "AV1 image item " + item.GetItemId() + " must have exactly one " + type + " property.",
                "AVIF 1.2 sections 2 and 9.1.1");
        }
    }

    internal static void ValidateAuxiliaryItems(
        ParsedMeta? meta,
        DiagnosticLog diagnostics,
        AvifValidationOptions options)
    {
        ParsedItemDiscovery discovery = (meta ?? throw new InvalidOperationException("Invalid internal state.")).GetItemDiscovery();
        int roleIndex = 0;
        while (roleIndex < discovery.GetRoleCount())
        {
            ParsedItemRole? role = discovery.GetRole(roleIndex);
            if ((role ?? throw new InvalidOperationException("Invalid internal state.")).IsAuxiliaryImage())
            {
                ParsedImageSpatialExtents? auxiliaryExtents = FindSpatialExtents(
                    role.GetItem());
                int auxiliaryBitDepth = FindBitDepth(role.GetItem());
                int masterIndex = 0;
                while (masterIndex < role.GetAuxiliaryMasterCount())
                {
                    ParsedItem? master = role.GetAuxiliaryMaster(masterIndex);
                    ParsedImageSpatialExtents? masterExtents = FindSpatialExtents(
                        master);
                    int masterBitDepth = FindBitDepth(master);
                    diagnostics.Context.PushItem(role.GetItem().GetItemId());
                    if (role.IsAlphaImage()
                        && auxiliaryBitDepth > 0
                        && masterBitDepth > 0
                        && auxiliaryBitDepth != masterBitDepth)
                    {
                        AddFinding(
                            diagnostics,
                            options,
                            AvifValidationRequirement.Mandatory,
                            AvifResultCode.NonConformingData,
                            AvifDiagnosticCategory.Rendering,
                            "An alpha auxiliary image must use the same bit depth as its master image.",
                            "AVIF 1.2 section 4.1");
                    }

                    if (auxiliaryExtents != null
                        && masterExtents != null
                        && (auxiliaryExtents.GetWidth() != masterExtents.GetWidth()
                            || auxiliaryExtents.GetHeight()
                                != masterExtents.GetHeight()))
                    {
                        AddFinding(
                            diagnostics,
                            options,
                            AvifValidationRequirement.Advisory,
                            AvifResultCode.NonConformingData,
                            AvifDiagnosticCategory.Rendering,
                            "Auxiliary dimensions differ from the master and require explicit resizing before composition.",
                            "MIAF auxiliary image rendering");
                    }
                    diagnostics.Context.PopItem();
                    masterIndex++;
                }
            }
            roleIndex++;
        }
    }

    static ParsedImageSpatialExtents? FindSpatialExtents(ParsedItem? item)
    {
        int propertyIndex = 0;
        while (propertyIndex < (item ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount())
        {
            ParsedImageSpatialExtents? extents = (item
                .GetPropertyAssociation(propertyIndex) ?? throw new InvalidOperationException("Invalid internal state."))
                .GetProperty()
                .GetSpatialExtents();
            if (extents != null)
            {
                return extents;
            }
            propertyIndex++;
        }
        return null;
    }

    static int FindBitDepth(ParsedItem? item)
    {
        int propertyIndex = 0;
        while (propertyIndex < (item ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount())
        {
            ParsedItemProperty property = (item
                .GetPropertyAssociation(propertyIndex) ?? throw new InvalidOperationException("Invalid internal state."))
                .GetProperty();
            ParsedAv1Configuration? configuration = property.GetAv1Configuration();
            if (configuration != null)
            {
                return configuration.GetBitDepth();
            }
            ParsedPixelInformation? pixels = property.GetPixelInformation();
            if (pixels != null && pixels.GetChannelCount() > 0)
            {
                return (pixels.GetChannel(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetBitDepth();
            }
            propertyIndex++;
        }
        return -1;
    }

    internal static int GetTransformRank(string type)
    {
        if (type == "clap")
        {
            return 1;
        }

        if (type == "irot")
        {
            return 2;
        }

        if (type == "imir")
        {
            return 3;
        }

        return 0;
    }

    internal static bool IsSupportedProperty(string type)
    {
        return type == "av1C"
            || type == "ispe"
            || type == "pixi"
            || AdditionalItemPropertyBox.IsSupportedType(type)
            || HdrCameraPropertyBox.IsSupportedType(type);
    }

    internal static bool HasSupportedPropertySemantics(ParsedItemProperty property)
    {
        string type = property.GetBoxType();
        if (!IsSupportedProperty(type))
        {
            return false;
        }

        if (type == "av1C")
        {
            return property.GetAv1Configuration() != null;
        }

        if (type == "ispe")
        {
            return property.GetSpatialExtents() != null;
        }

        if (type == "pixi")
        {
            return property.GetPixelInformation() != null;
        }

        if (type == "auxC")
        {
            return property.GetAuxiliaryType() != null;
        }

        if (type == "reve")
        {
            return property.GetReferenceViewingEnvironment() != null;
        }

        if (type == "ndwt")
        {
            return property.GetNominalDiffuseWhite() != null;
        }

        if (type == "cmin")
        {
            return property.GetCameraIntrinsicMatrix() != null;
        }

        if (type == "cmex")
        {
            return property.GetCameraExtrinsicMatrix() != null;
        }

        return true;
    }

    internal static void ValidateCleanAperture(
        ParsedMeta meta,
        ParsedItem item,
        DiagnosticLog diagnostics,
        AvifValidationOptions options)
    {
        ParsedCleanAperture? aperture = null;
        ParsedImageSpatialExtents? extents = null;
        int index = 0;
        while (index < item.GetPropertyCount())
        {
            ParsedItemProperty property = (item.GetPropertyAssociation(index) ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty();
            if (property.GetCleanAperture() != null)
            {
                aperture = property.GetCleanAperture();
            }

            if (property.GetSpatialExtents() != null)
            {
                extents = property.GetSpatialExtents();
            }

            index++;
        }

        if (aperture == null || extents == null || HasUncroppedSecondary(meta, item, extents))
        {
            return;
        }

        decimal horizontalLeft = (decimal)2
            * aperture.GetHorizontalOffsetNumerator()
            * aperture.GetWidthDenominator();
        decimal horizontalRight = (decimal)aperture.GetHorizontalOffsetDenominator()
            * ((decimal)aperture.GetWidthNumerator()
                - (decimal)extents.GetWidth() * aperture.GetWidthDenominator());
        decimal verticalLeft = (decimal)2
            * aperture.GetVerticalOffsetNumerator()
            * aperture.GetHeightDenominator();
        decimal verticalRight = (decimal)aperture.GetVerticalOffsetDenominator()
            * ((decimal)aperture.GetHeightNumerator()
                - (decimal)extents.GetHeight() * aperture.GetHeightDenominator());
        if (horizontalLeft != horizontalRight || verticalLeft != verticalRight)
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                AvifResultCode.NonConformingData,
                AvifDiagnosticCategory.Rendering,
                "The clap origin must be anchored at the top-left unless an uncropped secondary item is present.",
                "AVIF 1.2 section 2.2.3");
        }
    }

    internal static bool HasUncroppedSecondary(
        ParsedMeta meta,
        ParsedItem cropped,
        ParsedImageSpatialExtents croppedExtents)
    {
        int index = 0;
        while (index < meta.GetItemCount())
        {
            ParsedItem? candidate = meta.GetItem(index);
            if (candidate != cropped
                && !(candidate ?? throw new InvalidOperationException("Invalid internal state.")).IsHidden()
                && candidate.GetItemType() == "av01"
                && (!meta.HasPrimaryItem() || candidate.GetItemId() != meta.GetPrimaryItemId()))
            {
                bool hasAperture = false;
                ParsedImageSpatialExtents? candidateExtents = null;
                int propertyIndex = 0;
                while (propertyIndex < candidate.GetPropertyCount())
                {
                    ParsedItemProperty property = (candidate
                        .GetPropertyAssociation(propertyIndex) ?? throw new InvalidOperationException("Invalid internal state."))
                        .GetProperty();
                    if (property.GetCleanAperture() != null)
                    {
                        hasAperture = true;
                    }

                    if (property.GetSpatialExtents() != null)
                    {
                        candidateExtents = property.GetSpatialExtents();
                    }

                    propertyIndex++;
                }

                if (!hasAperture
                    && candidateExtents != null
                    && candidateExtents.GetWidth() == croppedExtents.GetWidth()
                    && candidateExtents.GetHeight() == croppedExtents.GetHeight()
                    && LocationsEqual(cropped, candidate))
                {
                    return true;
                }
            }

            index++;
        }

        return false;
    }

    internal static bool LocationsEqual(ParsedItem first, ParsedItem second)
    {
        if (!first.HasLocation() || !second.HasLocation())
        {
            return false;
        }

        ParsedItemLocation? firstLocation = first.GetLocation();
        ParsedItemLocation? secondLocation = second.GetLocation();
        if ((firstLocation ?? throw new InvalidOperationException("Invalid internal state.")).GetConstructionMethod() != (secondLocation ?? throw new InvalidOperationException("Invalid internal state.")).GetConstructionMethod()
            || firstLocation.GetDataReferenceIndex() != secondLocation.GetDataReferenceIndex()
            || firstLocation.GetBaseOffset() != secondLocation.GetBaseOffset()
            || firstLocation.GetExtentCount() != secondLocation.GetExtentCount())
        {
            return false;
        }

        int index = 0;
        while (index < firstLocation.GetExtentCount())
        {
            ParsedItemExtent? firstExtent = firstLocation.GetExtent(index);
            ParsedItemExtent? secondExtent = secondLocation.GetExtent(index);
            if ((firstExtent ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentIndex() != (secondExtent ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentIndex()
                || firstExtent.GetExtentOffset() != secondExtent.GetExtentOffset()
                || firstExtent.GetExtentLength() != secondExtent.GetExtentLength())
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static void ValidateGridTransforms(
        ParsedMeta? meta,
        DiagnosticLog diagnostics,
        AvifValidationOptions options)
    {
        ParsedItemDependencyGraph graph = (meta ?? throw new InvalidOperationException("Invalid internal state.")).GetItemDependencyGraph()
            ?? throw new InvalidOperationException("Invalid internal state.");
        int itemIndex = 0;
        while (itemIndex < meta.GetItemCount())
        {
            ParsedItem? grid = meta.GetItem(itemIndex);
            if ((grid ?? throw new InvalidOperationException("Invalid internal state.")).GetItemType() == "grid")
            {
                ParsedItemDependencyNode? node = (graph ?? throw new InvalidOperationException("Invalid internal state.")).FindNode(grid.GetItemId());
                if (node != null)
                {
                    ParsedItemDecodePlan plan = node.GetDecodePlan();
                    int dependencyIndex = 0;
                    while (dependencyIndex < plan.GetItemCount())
                    {
                        ParsedItem? dependency = plan.GetItem(dependencyIndex);
                        if ((dependency ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId() != grid.GetItemId()
                            && HasTransform(dependency))
                        {
                            diagnostics.Context.PushItem(dependency.GetItemId());
                            AddFinding(
                                diagnostics,
                                options,
                                AvifValidationRequirement.Mandatory,
                                AvifResultCode.NonConformingData,
                                AvifDiagnosticCategory.Rendering,
                                "An item in a grid input derivation chain must not have transformative properties.",
                                "AVIF 1.2 section 7");
                            diagnostics.Context.PopItem();
                            if (ShouldStop(diagnostics, options))
                            {
                                return;
                            }
                        }

                        dependencyIndex++;
                    }
                }
            }

            itemIndex++;
        }
    }

    internal static bool HasTransform(ParsedItem item)
    {
        int index = 0;
        while (index < item.GetPropertyCount())
        {
            if (GetTransformRank(
                    (item.GetPropertyAssociation(index) ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty().GetBoxType()) > 0)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static void ValidateProfiles(
        FileTypeBox? fileType,
        ParsedMeta? meta,
        DiagnosticLog diagnostics,
        AvifValidationOptions options)
    {
        if (fileType == null
            || (!fileType.HasBrand("MA1B") && !fileType.HasBrand("MA1A")))
        {
            return;
        }

        ParsedItemDependencyGraph graph = (meta ?? throw new InvalidOperationException("Invalid internal state.")).GetItemDependencyGraph()
            ?? throw new InvalidOperationException("Invalid internal state.");
        int itemIndex = 0;
        while (itemIndex < meta.GetItemCount())
        {
            ParsedItem? item = meta.GetItem(itemIndex);
            ParsedItemRole? role = meta.GetItemDiscovery().FindRole((item ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId());
            if (role != null
                && (role.IsCodedImage() || role.IsDerivedImage())
                && (graph ?? throw new InvalidOperationException("Invalid internal state.")).ValidateSelfContainment(item.GetItemId()) != AvifResultCode.Success)
            {
                diagnostics.Context.PushItem(item.GetItemId());
                AddFinding(
                    diagnostics,
                    options,
                    AvifValidationRequirement.Mandatory,
                    AvifResultCode.NonConformingData,
                    AvifDiagnosticCategory.Validation,
                    "AVIF profile image items and their dependencies must be self-contained.",
                    "AVIF 1.2 sections 8.2 and 8.3");
                diagnostics.Context.PopItem();
            }

            if (item.GetItemType() == "av01")
            {
                ValidateCodedProfile(fileType, item, diagnostics, options);
            }

            if (ShouldStop(diagnostics, options))
            {
                return;
            }

            itemIndex++;
        }
    }

    internal static void ValidateCodedProfile(
        FileTypeBox fileType,
        ParsedItem item,
        DiagnosticLog diagnostics,
        AvifValidationOptions options)
    {
        ParsedAv1Configuration? configuration = null;
        ParsedImageSpatialExtents? extents = null;
        int index = 0;
        while (index < item.GetPropertyCount())
        {
            ParsedItemProperty property = (item.GetPropertyAssociation(index) ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty();
            if (property.GetAv1Configuration() != null)
            {
                configuration = property.GetAv1Configuration();
            }

            if (property.GetSpatialExtents() != null)
            {
                extents = property.GetSpatialExtents();
            }

            index++;
        }

        if (configuration == null || extents == null)
        {
            return;
        }

        long width = extents.GetWidth();
        long height = extents.GetHeight();
        CheckedIntegerResult pixels = CheckedMath.MultiplyInt64(width, height);
        bool violatesBaseline = configuration.GetSequenceProfile() != 0
            || configuration.GetSequenceLevelIndex() > 13
            || width > 8192
            || height > 4352
            || !pixels.Success
            || pixels.Value > 8912896;
        bool violatesAdvanced = configuration.GetSequenceProfile() != 1
            || configuration.GetSequenceLevelIndex() > 16
            || width > 16384
            || height > 8704
            || !pixels.Success
            || pixels.Value > 35651584;
        diagnostics.Context.PushItem(item.GetItemId());
        if (fileType.HasBrand("MA1B") && violatesBaseline)
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                AvifResultCode.NonConformingData,
                AvifDiagnosticCategory.Validation,
                "The AV1 image item exceeds the MA1B profile constraints.",
                "AVIF 1.2 section 8.2");
        }

        if (fileType.HasBrand("MA1A") && violatesAdvanced)
        {
            AddFinding(
                diagnostics,
                options,
                AvifValidationRequirement.Mandatory,
                AvifResultCode.NonConformingData,
                AvifDiagnosticCategory.Validation,
                "The AV1 image item exceeds the MA1A profile constraints.",
                "AVIF 1.2 section 8.3");
        }

        diagnostics.Context.PopItem();
    }

    internal static void ValidatePayloads(
        ReadOnlySpan<byte> source,
        ParsedMeta? meta,
        DiagnosticLog diagnostics,
        AvifValidationOptions options)
    {
        int itemIndex = 0;
        while (itemIndex < (meta ?? throw new InvalidOperationException("Invalid internal state.")).GetItemCount())
        {
            ParsedItem? item = meta.GetItem(itemIndex);
            if ((item ?? throw new InvalidOperationException("Invalid internal state.")).GetItemType() == "av01")
            {
                diagnostics.Context.PushItem(item.GetItemId());
                if (item.GetProtectionIndex() != 0)
                {
                    AddFinding(
                        diagnostics,
                        options,
                        AvifValidationRequirement.Hard,
                        AvifResultCode.UnsupportedProtection,
                        AvifDiagnosticCategory.Protection,
                        "Encoded-payload validation requires protected cleartext.",
                        "AVIF protected media boundary");
                }
                else
                {
                    EncodedItemExtractionResult extraction = meta.ExtractEncodedItem(
                        source,
                        item.GetItemId());
                    if (extraction.Code != AvifResultCode.Success || extraction.Data == null)
                    {
                        AddFinding(
                            diagnostics,
                            options,
                            AvifValidationRequirement.Hard,
                            extraction.Code,
                            AvifDiagnosticCategory.Item,
                            "The encoded AV1 item payload cannot be resolved and read.",
                            "AVIF 1.2 section 2.1");
                    }
                    else
                    {
                        ParsedAv1Configuration? configuration = FindAv1Configuration(item);
                        if (configuration != null)
                        {
                            Av1SequenceHeaderParseResult reconciled = configuration
                                .ReconcileSequenceHeaderResult(extraction.Data.GetSpan());
                            AvifResultCode reconcileCode = reconciled.Code;
                            if (reconcileCode != AvifResultCode.Success)
                            {
                                AvifValidationRequirement requirement = reconcileCode
                                        == AvifResultCode.NonConformingData
                                    ? AvifValidationRequirement.Mandatory
                                    : AvifValidationRequirement.Hard;
                                AddFinding(
                                    diagnostics,
                                    options,
                                    requirement,
                                    reconcileCode,
                                    AvifDiagnosticCategory.Av1,
                                    "The AV1 item payload is invalid or does not match its av1C property.",
                                    "AVIF 1.2 sections 2.1 and 2.2.1");
                            }
                            else
                            {
                                int propertyIndex = 0;
                                while (propertyIndex < item.GetPropertyCount())
                                {
                                    ParsedColourInformation? colour = (item
                                        .GetPropertyAssociation(propertyIndex) ?? throw new InvalidOperationException("Invalid internal state."))
                                        .GetProperty()
                                        .GetColourInformation();
                                    if (colour != null)
                                    {
                                        reconcileCode = configuration
                                            .ReconcileColourInformationWithSequenceHeader(
                                                colour,
                                                reconciled.Header);
                                        if (reconcileCode != AvifResultCode.Success)
                                        {
                                            AddFinding(
                                                diagnostics,
                                                options,
                                                AvifValidationRequirement.Mandatory,
                                                reconcileCode,
                                                AvifDiagnosticCategory.Rendering,
                                                "The item color property does not agree with its AV1 sequence header.",
                                                "AVIF color information reconciliation");
                                            break;
                                        }
                                    }
                                    propertyIndex++;
                                }
                            }
                        }

                        extraction.Data.Close();
                    }
                }

                diagnostics.Context.PopItem();
                if (ShouldStop(diagnostics, options))
                {
                    return;
                }
            }

            itemIndex++;
        }
    }

    internal static ParsedAv1Configuration? FindAv1Configuration(ParsedItem item)
    {
        int index = 0;
        while (index < item.GetPropertyCount())
        {
            ParsedAv1Configuration? configuration = (item
                .GetPropertyAssociation(index) ?? throw new InvalidOperationException("Invalid internal state."))
                .GetProperty()
                .GetAv1Configuration();
            if (configuration != null)
            {
                return configuration;
            }

            index++;
        }

        return null;
    }

    internal static bool ShouldStop(
        DiagnosticLog diagnostics,
        AvifValidationOptions options)
    {
        return options.StopAfterFirstError
            && diagnostics.Code != AvifResultCode.Success;
    }

    internal static void AddFinding(
        DiagnosticLog diagnostics,
        AvifValidationOptions options,
        AvifValidationRequirement requirement,
        AvifResultCode code,
        AvifDiagnosticCategory category,
        string message,
        string standardReference)
    {
        AvifDiagnosticSeverity severity = AvifDiagnosticSeverity.Error;
        if (requirement == AvifValidationRequirement.Advisory)
        {
            severity = AvifDiagnosticSeverity.Warning;
        }
        else if (requirement == AvifValidationRequirement.Interoperable
            && options.Mode != AvifValidationMode.Strict)
        {
            severity = AvifDiagnosticSeverity.Warning;
        }
        else if (requirement == AvifValidationRequirement.Mandatory
            && options.Mode == AvifValidationMode.Permissive)
        {
            severity = AvifDiagnosticSeverity.Warning;
        }

        if (severity == AvifDiagnosticSeverity.Warning && !options.CollectWarnings)
        {
            return;
        }

        diagnostics.Context.SetStandardReference(standardReference);
        diagnostics.Add(code, severity, category, message);
        diagnostics.Context.SetStandardReference(string.Empty);
    }
}
