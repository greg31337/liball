using System;
using System.Collections.Generic;
using System.Globalization;

namespace image;

/// <summary>Compares decoded metadata directories, tags, profiles, and normalized summary fields.</summary>
/// <remarks>Source byte offsets and container layout are ignored. Reordered uniquely identified directories, tags, and profiles compare as equivalent.</remarks>
internal static class ImageMetadataDiffCore
{
    /// <summary>Compares the metadata content of two documents.</summary>
    /// <param name="left">The first document, or null to represent an empty document.</param>
    /// <param name="right">The second document, or null to represent an empty document.</param>
    /// <returns>An ordered result containing human-readable additions, removals, and changed fields.</returns>
    public static ImageMetadataDiffResult Compare(ImageMetadataDocument? left, ImageMetadataDocument? right)
    {
        ImageMetadataDiffResult result = new ImageMetadataDiffResult();
        CompareDirectories(result, left, right);
        CompareProfiles(result, left, right);
        CompareRawBlocks(result, left, right);
        CompareSummaries(result, Summary(left), Summary(right));
        result.AreEqual = result.Differences.Count == 0;
        return result;
    }

    internal static void CompareDirectories(ImageMetadataDiffResult result, ImageMetadataDocument? left, ImageMetadataDocument? right)
    {
        List<ImageMetadataDirectoryDiffItem> leftItems = CollectDirectories(left);
        List<ImageMetadataDirectoryDiffItem> rightItems = CollectDirectories(right);

        foreach (ImageMetadataDirectoryDiffItem leftItem in leftItems)
        {
            ImageMetadataDirectoryDiffItem? rightItem = FindDirectory(rightItems, leftItem.Identity);
            if (rightItem == null)
            {
                AddDifference(result, "Directory removed: " + leftItem.Identity);
            }
            else
            {
                CompareDirectory(result, leftItem.Identity, leftItem.Directory, rightItem.Directory);
                CompareTags(result, leftItem.Identity, leftItem.Directory, rightItem.Directory);
            }
        }

        foreach (ImageMetadataDirectoryDiffItem rightItem in rightItems)
        {
            if (FindDirectory(leftItems, rightItem.Identity) == null)
            {
                AddDifference(result, "Directory added: " + rightItem.Identity);
            }
        }
    }

    internal static List<ImageMetadataDirectoryDiffItem> CollectDirectories(ImageMetadataDocument? document)
    {
        List<ImageMetadataDirectoryDiffItem> items = new List<ImageMetadataDirectoryDiffItem>();
        if (document == null)
        {
            return items;
        }

        CollectDirectoryList(items, document.Directories, "Document");
        if (document.Frames != null)
        {
            int index = 0;
            while (index < document.Frames.Count)
            {
                ImageMetadataFrame frame = document.Frames[index];
                if (frame != null)
                {
                    CollectDirectoryList(items, frame.Directories, "Frame[" + index.ToString(CultureInfo.InvariantCulture) + "]");
                }

                index = index + 1;
            }
        }

        return items;
    }

    internal static void CollectDirectoryList(List<ImageMetadataDirectoryDiffItem> items, List<MetadataDirectory> directories, string parentIdentity)
    {
        if (items == null || directories == null)
        {
            return;
        }

        int index = 0;
        while (index < directories.Count)
        {
            MetadataDirectory directory = ActualDirectory(directories[index]);
            int occurrence = DirectoryOccurrence(directories, index, directory);
            string identity = parentIdentity + "/" + directory.Family.ToString() + "." + SafeText(directory.Group) + "[" + occurrence.ToString(CultureInfo.InvariantCulture) + "]";
            ImageMetadataDirectoryDiffItem item = new ImageMetadataDirectoryDiffItem();
            item.Identity = identity;
            item.Directory = directory;
            items.Add(item);
            CollectDirectoryList(items, directory.Children, identity);
            index = index + 1;
        }
    }

    internal static int DirectoryOccurrence(List<MetadataDirectory> directories, int index, MetadataDirectory directory)
    {
        int occurrence = 0;
        int previous = 0;
        while (previous < index)
        {
            MetadataDirectory candidate = ActualDirectory(directories[previous]);
            if (candidate.Family == directory.Family && SameText(candidate.Group, directory.Group))
            {
                occurrence = occurrence + 1;
            }

            previous = previous + 1;
        }

        return occurrence;
    }

    internal static ImageMetadataDirectoryDiffItem? FindDirectory(List<ImageMetadataDirectoryDiffItem>? items, string identity)
    {
        if (items == null)
        {
            return null;
        }

        foreach (ImageMetadataDirectoryDiffItem item in items)
        {
            if (item != null && SameText(item.Identity, identity))
            {
                return item;
            }
        }

        return null;
    }

    internal static void CompareDirectory(ImageMetadataDiffResult result, string identity, MetadataDirectory left, MetadataDirectory right)
    {
        MetadataDirectory actualLeft = ActualDirectory(left);
        MetadataDirectory actualRight = ActualDirectory(right);
        AddChanged(result, "Directory " + identity + ".Name", actualLeft.Name, actualRight.Name);
        AddChanged(result, "Directory " + identity + ".Path", actualLeft.Path, actualRight.Path);
        AddChanged(result, "Directory " + identity + ".FrameIndex", actualLeft.FrameIndex, actualRight.FrameIndex);
        AddChanged(result, "Directory " + identity + ".RepeatIndex", actualLeft.RepeatIndex, actualRight.RepeatIndex);
    }

    internal static void CompareTags(ImageMetadataDiffResult result, string directoryIdentity, MetadataDirectory left, MetadataDirectory right)
    {
        List<ImageMetadataTagDiffItem> leftItems = CollectTags(ActualDirectory(left).Tags);
        List<ImageMetadataTagDiffItem> rightItems = CollectTags(ActualDirectory(right).Tags);

        foreach (ImageMetadataTagDiffItem leftItem in leftItems)
        {
            ImageMetadataTagDiffItem? rightItem = FindTag(rightItems, leftItem.Identity);
            string identity = directoryIdentity + "/" + leftItem.Identity;
            if (rightItem == null)
            {
                AddDifference(result, "Tag removed: " + identity);
            }
            else
            {
                CompareTag(result, identity, leftItem.Tag, rightItem.Tag);
            }
        }

        foreach (ImageMetadataTagDiffItem rightItem in rightItems)
        {
            if (FindTag(leftItems, rightItem.Identity) == null)
            {
                AddDifference(result, "Tag added: " + directoryIdentity + "/" + rightItem.Identity);
            }
        }
    }

    internal static List<ImageMetadataTagDiffItem> CollectTags(List<MetadataTag> tags)
    {
        List<ImageMetadataTagDiffItem> items = new List<ImageMetadataTagDiffItem>();
        if (tags == null)
        {
            return items;
        }

        int index = 0;
        while (index < tags.Count)
        {
            MetadataTag tag = ActualTag(tags[index]);
            int occurrence = TagOccurrence(tags, index, tag);
            ImageMetadataTagDiffItem item = new ImageMetadataTagDiffItem();
            item.Identity = TagIdentityText(tag) + "[" + occurrence.ToString(CultureInfo.InvariantCulture) + "]";
            item.Tag = tag;
            items.Add(item);
            index = index + 1;
        }

        return items;
    }

    internal static int TagOccurrence(List<MetadataTag> tags, int index, MetadataTag tag)
    {
        int occurrence = 0;
        int previous = 0;
        while (previous < index)
        {
            if (SameText(TagIdentityText(ActualTag(tags[previous])), TagIdentityText(tag)))
            {
                occurrence = occurrence + 1;
            }

            previous = previous + 1;
        }

        return occurrence;
    }

    internal static string TagIdentityText(MetadataTag tag)
    {
        MetadataTag actual = ActualTag(tag);
        if (!IsEmpty(actual.Key))
        {
            return actual.Key;
        }

        if (!IsEmpty(actual.Name))
        {
            return actual.Name;
        }

        return "Tag" + actual.NumericId.ToString(CultureInfo.InvariantCulture);
    }

    internal static ImageMetadataTagDiffItem? FindTag(List<ImageMetadataTagDiffItem>? items, string identity)
    {
        if (items == null)
        {
            return null;
        }

        foreach (ImageMetadataTagDiffItem item in items)
        {
            if (item != null && SameText(item.Identity, identity))
            {
                return item;
            }
        }

        return null;
    }

    internal static void CompareTag(ImageMetadataDiffResult result, string identity, MetadataTag left, MetadataTag right)
    {
        MetadataTag actualLeft = ActualTag(left);
        MetadataTag actualRight = ActualTag(right);
        AddChanged(result, "Tag " + identity + ".Name", actualLeft.Name, actualRight.Name);
        AddChanged(result, "Tag " + identity + ".Family", actualLeft.Family.ToString(), actualRight.Family.ToString());
        AddChanged(result, "Tag " + identity + ".Group", actualLeft.Group, actualRight.Group);
        AddChanged(result, "Tag " + identity + ".NumericId", actualLeft.NumericId, actualRight.NumericId);
        AddChanged(result, "Tag " + identity + ".ValueType", actualLeft.ValueType.ToString(), actualRight.ValueType.ToString());
        if (!ValuesEqual(actualLeft.Value, actualRight.Value))
        {
            AddChanged(result, "Tag " + identity + ".Value", ValueText(actualLeft.Value), ValueText(actualRight.Value));
        }

        if (!BytesEqual(actualLeft.RawValue, actualRight.RawValue))
        {
            AddDifference(result, "Tag " + identity + ".RawValue: " + Quote(ByteDescription(actualLeft.RawValue)) + " -> " + Quote(ByteDescription(actualRight.RawValue)));
        }

        AddChanged(result, "Tag " + identity + ".DisplayValue", actualLeft.DisplayValue, actualRight.DisplayValue);
        AddChanged(result, "Tag " + identity + ".Confidence", actualLeft.Confidence, actualRight.Confidence);
    }

    internal static void CompareProfiles(ImageMetadataDiffResult result, ImageMetadataDocument? left, ImageMetadataDocument? right)
    {
        List<ImageMetadataProfileDiffItem> leftItems = CollectProfiles(left == null ? null : left.Profiles);
        List<ImageMetadataProfileDiffItem> rightItems = CollectProfiles(right == null ? null : right.Profiles);

        foreach (ImageMetadataProfileDiffItem leftItem in leftItems)
        {
            ImageMetadataProfileDiffItem? rightItem = FindProfile(rightItems, leftItem.Identity);
            if (rightItem == null)
            {
                AddDifference(result, "Profile removed: " + leftItem.Identity);
            }
            else
            {
                CompareProfile(result, leftItem.Identity, leftItem.Profile, rightItem.Profile);
            }
        }

        foreach (ImageMetadataProfileDiffItem rightItem in rightItems)
        {
            if (FindProfile(leftItems, rightItem.Identity) == null)
            {
                AddDifference(result, "Profile added: " + rightItem.Identity);
            }
        }
    }

    internal static List<ImageMetadataProfileDiffItem> CollectProfiles(List<MetadataProfile>? profiles)
    {
        List<ImageMetadataProfileDiffItem> items = new List<ImageMetadataProfileDiffItem>();
        if (profiles == null)
        {
            return items;
        }

        int index = 0;
        while (index < profiles.Count)
        {
            MetadataProfile profile = ActualProfile(profiles[index]);
            int occurrence = ProfileOccurrence(profiles, index, profile);
            ImageMetadataProfileDiffItem item = new ImageMetadataProfileDiffItem();
            item.Identity = ProfileIdentityText(profile) + "[" + occurrence.ToString(CultureInfo.InvariantCulture) + "]";
            item.Profile = profile;
            items.Add(item);
            index = index + 1;
        }

        return items;
    }

    internal static int ProfileOccurrence(List<MetadataProfile> profiles, int index, MetadataProfile profile)
    {
        int occurrence = 0;
        int previous = 0;
        while (previous < index)
        {
            if (SameText(ProfileIdentityText(ActualProfile(profiles[previous])), ProfileIdentityText(profile)))
            {
                occurrence = occurrence + 1;
            }

            previous = previous + 1;
        }

        return occurrence;
    }

    internal static string ProfileIdentityText(MetadataProfile profile)
    {
        MetadataProfile actual = ActualProfile(profile);
        string name = IsEmpty(actual.Key) ? actual.Name : actual.Key;
        return actual.Kind.ToString() + "." + SafeText(name);
    }

    internal static ImageMetadataProfileDiffItem? FindProfile(List<ImageMetadataProfileDiffItem>? items, string identity)
    {
        if (items == null)
        {
            return null;
        }

        foreach (ImageMetadataProfileDiffItem item in items)
        {
            if (item != null && SameText(item.Identity, identity))
            {
                return item;
            }
        }

        return null;
    }

    internal static void CompareProfile(ImageMetadataDiffResult result, string identity, MetadataProfile left, MetadataProfile right)
    {
        MetadataProfile actualLeft = ActualProfile(left);
        MetadataProfile actualRight = ActualProfile(right);
        AddChanged(result, "Profile " + identity + ".Role", actualLeft.Role.ToString(), actualRight.Role.ToString());
        AddChanged(result, "Profile " + identity + ".ContainerItemId", actualLeft.ContainerItemId, actualRight.ContainerItemId);
        AddChanged(result, "Profile " + identity + ".SourceFrameIndex", actualLeft.SourceFrameIndex, actualRight.SourceFrameIndex);
        AddChanged(result, "Profile " + identity + ".Family", actualLeft.Family.ToString(), actualRight.Family.ToString());
        AddChanged(result, "Profile " + identity + ".Name", actualLeft.Name, actualRight.Name);
        AddChanged(result, "Profile " + identity + ".MimeType", actualLeft.MimeType, actualRight.MimeType);
        if (!BytesEqual(actualLeft.Data, actualRight.Data))
        {
            AddDifference(result, "Profile " + identity + ".Data: " + Quote(ByteDescription(actualLeft.Data)) + " -> " + Quote(ByteDescription(actualRight.Data)));
        }

        AddChanged(result, "Profile " + identity + ".Text", actualLeft.Text, actualRight.Text);
        AddChanged(result, "Profile " + identity + ".Length", actualLeft.Length, actualRight.Length);
        AddChanged(result, "Profile " + identity + ".SequenceNumber", actualLeft.SequenceNumber, actualRight.SequenceNumber);
        AddChanged(result, "Profile " + identity + ".SequenceCount", actualLeft.SequenceCount, actualRight.SequenceCount);
    }

    internal static void CompareRawBlocks(ImageMetadataDiffResult result, ImageMetadataDocument? left, ImageMetadataDocument? right)
    {
        List<ImageMetadataRawBlockDiffItem> leftItems = CollectRawBlocks(left == null ? null : left.RawBlocks);
        List<ImageMetadataRawBlockDiffItem> rightItems = CollectRawBlocks(right == null ? null : right.RawBlocks);

        foreach (ImageMetadataRawBlockDiffItem leftItem in leftItems)
        {
            ImageMetadataRawBlockDiffItem? rightItem = FindRawBlock(rightItems, leftItem.Identity);
            if (rightItem == null)
            {
                AddDifference(result, "Raw block removed: " + leftItem.Identity);
            }
            else
            {
                CompareRawBlock(result, leftItem.Identity, leftItem.RawBlock, rightItem.RawBlock);
            }
        }

        foreach (ImageMetadataRawBlockDiffItem rightItem in rightItems)
        {
            if (FindRawBlock(leftItems, rightItem.Identity) == null)
            {
                AddDifference(result, "Raw block added: " + rightItem.Identity);
            }
        }
    }

    internal static List<ImageMetadataRawBlockDiffItem> CollectRawBlocks(List<MetadataRawBlock>? rawBlocks)
    {
        List<ImageMetadataRawBlockDiffItem> items = new List<ImageMetadataRawBlockDiffItem>();
        if (rawBlocks == null)
        {
            return items;
        }

        int index = 0;
        while (index < rawBlocks.Count)
        {
            MetadataRawBlock rawBlock = ActualRawBlock(rawBlocks[index]);
            int occurrence = RawBlockOccurrence(rawBlocks, index, rawBlock);
            ImageMetadataRawBlockDiffItem item = new ImageMetadataRawBlockDiffItem();
            item.Identity = RawBlockIdentityText(rawBlock) + "[" + occurrence.ToString(CultureInfo.InvariantCulture) + "]";
            item.RawBlock = rawBlock;
            items.Add(item);
            index = index + 1;
        }

        return items;
    }

    internal static int RawBlockOccurrence(List<MetadataRawBlock> rawBlocks, int index, MetadataRawBlock rawBlock)
    {
        int occurrence = 0;
        int previous = 0;
        while (previous < index)
        {
            if (SameText(RawBlockIdentityText(ActualRawBlock(rawBlocks[previous])), RawBlockIdentityText(rawBlock)))
            {
                occurrence = occurrence + 1;
            }

            previous = previous + 1;
        }

        return occurrence;
    }

    internal static string RawBlockIdentityText(MetadataRawBlock rawBlock)
    {
        MetadataRawBlock actual = ActualRawBlock(rawBlock);
        return actual.Family.ToString() + "." + SafeText(actual.BlockType);
    }

    internal static ImageMetadataRawBlockDiffItem? FindRawBlock(List<ImageMetadataRawBlockDiffItem>? items, string identity)
    {
        if (items == null)
        {
            return null;
        }

        foreach (ImageMetadataRawBlockDiffItem item in items)
        {
            if (item != null && SameText(item.Identity, identity))
            {
                return item;
            }
        }

        return null;
    }

    internal static void CompareRawBlock(ImageMetadataDiffResult result, string identity, MetadataRawBlock left, MetadataRawBlock right)
    {
        MetadataRawBlock actualLeft = ActualRawBlock(left);
        MetadataRawBlock actualRight = ActualRawBlock(right);
        AddChanged(result, "Raw block " + identity + ".Name", actualLeft.Name, actualRight.Name);
        AddChanged(result, "Raw block " + identity + ".Length", actualLeft.Length, actualRight.Length);
        if (!BytesEqual(actualLeft.Data, actualRight.Data))
        {
            AddDifference(result, "Raw block " + identity + ".Data: " + Quote(ByteDescription(actualLeft.Data)) + " -> " + Quote(ByteDescription(actualRight.Data)));
        }
    }

    internal static void CompareSummaries(ImageMetadataDiffResult result, ImageMetadataSummary? left, ImageMetadataSummary? right)
    {
        ImageMetadataSummary actualLeft = ActualSummary(left);
        ImageMetadataSummary actualRight = ActualSummary(right);
        AddChanged(result, "Summary.Width", actualLeft.Width, actualRight.Width);
        AddChanged(result, "Summary.Height", actualLeft.Height, actualRight.Height);
        AddChanged(result, "Summary.FrameCount", actualLeft.FrameCount, actualRight.FrameCount);
        AddChanged(result, "Summary.PixelAspectRatio", actualLeft.PixelAspectRatio, actualRight.PixelAspectRatio);
        AddChanged(result, "Summary.HorizontalResolution", actualLeft.HorizontalResolution, actualRight.HorizontalResolution);
        AddChanged(result, "Summary.VerticalResolution", actualLeft.VerticalResolution, actualRight.VerticalResolution);
        AddChanged(result, "Summary.ResolutionUnit", actualLeft.ResolutionUnit, actualRight.ResolutionUnit);
        AddChanged(result, "Summary.Orientation", actualLeft.Orientation.ToString(), actualRight.Orientation.ToString());
        AddChanged(result, "Summary.CameraMake", actualLeft.CameraMake, actualRight.CameraMake);
        AddChanged(result, "Summary.CameraModel", actualLeft.CameraModel, actualRight.CameraModel);
        AddChanged(result, "Summary.LensModel", actualLeft.LensModel, actualRight.LensModel);
        AddChanged(result, "Summary.CaptureTimeText", actualLeft.CaptureTimeText, actualRight.CaptureTimeText);
        CompareGps(result, actualLeft.Gps, actualRight.Gps);
        AddChanged(result, "Summary.Author", actualLeft.Author, actualRight.Author);
        AddChanged(result, "Summary.Copyright", actualLeft.Copyright, actualRight.Copyright);
        AddChanged(result, "Summary.Title", actualLeft.Title, actualRight.Title);
        AddChanged(result, "Summary.Description", actualLeft.Description, actualRight.Description);
        if (!TextListsEqual(actualLeft.Keywords, actualRight.Keywords))
        {
            AddChanged(result, "Summary.Keywords", TextListDescription(actualLeft.Keywords), TextListDescription(actualRight.Keywords));
        }

        AddChanged(result, "Summary.Rating", actualLeft.Rating, actualRight.Rating);
        AddChanged(result, "Summary.HasColorProfile", actualLeft.HasColorProfile, actualRight.HasColorProfile);
        AddChanged(result, "Summary.ColorProfileName", actualLeft.ColorProfileName, actualRight.ColorProfileName);
        AddChanged(result, "Summary.ColorSpace", actualLeft.ColorSpace.ToString(), actualRight.ColorSpace.ToString());
        AddChanged(result, "Summary.SourceColorType", actualLeft.SourceColorType, actualRight.SourceColorType);
        AddChanged(result, "Summary.IsInterlaced", actualLeft.IsInterlaced, actualRight.IsInterlaced);
        AddChanged(result, "Summary.IsProgressive", actualLeft.IsProgressive, actualRight.IsProgressive);
        AddChanged(result, "Summary.IsAnimated", actualLeft.IsAnimated, actualRight.IsAnimated);
        AddChanged(result, "Summary.AnimationLoopCount", actualLeft.AnimationLoopCount, actualRight.AnimationLoopCount);
        AddChanged(result, "Summary.TotalDurationMilliseconds", actualLeft.TotalDurationMilliseconds, actualRight.TotalDurationMilliseconds);
        AddChanged(result, "Summary.ResolutionText", actualLeft.ResolutionText, actualRight.ResolutionText);
        AddChanged(result, "Summary.Software", actualLeft.Software, actualRight.Software);
    }

    internal static void CompareGps(ImageMetadataDiffResult result, GpsCoordinate left, GpsCoordinate right)
    {
        GpsCoordinate actualLeft = ActualGps(left);
        GpsCoordinate actualRight = ActualGps(right);
        AddChanged(result, "Summary.Gps.IsValid", actualLeft.IsValid, actualRight.IsValid);
        AddChanged(result, "Summary.Gps.Latitude", actualLeft.Latitude, actualRight.Latitude);
        AddChanged(result, "Summary.Gps.Longitude", actualLeft.Longitude, actualRight.Longitude);
        AddChanged(result, "Summary.Gps.Altitude", actualLeft.Altitude, actualRight.Altitude);
        AddChanged(result, "Summary.Gps.LatitudeReference", actualLeft.LatitudeReference, actualRight.LatitudeReference);
        AddChanged(result, "Summary.Gps.LongitudeReference", actualLeft.LongitudeReference, actualRight.LongitudeReference);
        AddChanged(result, "Summary.Gps.AltitudeReference", actualLeft.AltitudeReference, actualRight.AltitudeReference);
    }

    internal static bool ValuesEqual(MetadataValue? left, MetadataValue? right)
    {
        MetadataValue actualLeft = ActualValue(left);
        MetadataValue actualRight = ActualValue(right);
        if (actualLeft.Kind != actualRight.Kind)
        {
            return false;
        }

        switch (actualLeft.Kind)
        {
            case MetadataValueKind.Integer:
                return actualLeft.IntegerValue == actualRight.IntegerValue;
            case MetadataValueKind.UnsignedInteger:
                return actualLeft.UnsignedIntegerValue == actualRight.UnsignedIntegerValue;
            case MetadataValueKind.Rational:
            case MetadataValueKind.SignedRational:
                return actualLeft.Numerator == actualRight.Numerator && actualLeft.Denominator == actualRight.Denominator;
            case MetadataValueKind.Double:
                return actualLeft.DoubleValue.Equals(actualRight.DoubleValue);
            case MetadataValueKind.IntegerArray:
                return IntegerArraysEqual(actualLeft.IntegerValues, actualRight.IntegerValues);
            case MetadataValueKind.UnsignedIntegerArray:
                return UnsignedIntegerArraysEqual(actualLeft.UnsignedIntegerValues, actualRight.UnsignedIntegerValues);
            case MetadataValueKind.RationalArray:
            case MetadataValueKind.SignedRationalArray:
                return IntegerArraysEqual(actualLeft.Numerators, actualRight.Numerators) && IntegerArraysEqual(actualLeft.Denominators, actualRight.Denominators);
            case MetadataValueKind.DoubleArray:
                return DoubleArraysEqual(actualLeft.DoubleValues, actualRight.DoubleValues);
            case MetadataValueKind.String:
            case MetadataValueKind.DateText:
            case MetadataValueKind.XmlText:
                return SameText(actualLeft.Text, actualRight.Text);
            case MetadataValueKind.StringList:
                return StringArraysEqual(actualLeft.TextValues, actualRight.TextValues);
            case MetadataValueKind.ByteArray:
                return BytesEqual(actualLeft.Bytes, actualRight.Bytes);
            case MetadataValueKind.DirectoryReference:
                return DirectoryReferencesEqual(actualLeft.DirectoryValue, actualRight.DirectoryValue);
            case MetadataValueKind.Boolean:
                return actualLeft.BooleanValue == actualRight.BooleanValue;
            default:
                return true;
        }
    }

    internal static bool DirectoryReferencesEqual(MetadataDirectory? left, MetadataDirectory? right)
    {
        MetadataDirectory actualLeft = ActualDirectory(left);
        MetadataDirectory actualRight = ActualDirectory(right);
        return actualLeft.Family == actualRight.Family &&
            SameText(actualLeft.Group, actualRight.Group) &&
            SameText(actualLeft.Name, actualRight.Name) &&
            SameText(actualLeft.Path, actualRight.Path) &&
            actualLeft.FrameIndex == actualRight.FrameIndex &&
            actualLeft.RepeatIndex == actualRight.RepeatIndex;
    }

    internal static bool BytesEqual(byte[]? left, byte[]? right)
    {
        int leftLength = ByteLength(left);
        int rightLength = ByteLength(right);
        if (leftLength != rightLength)
        {
            return false;
        }

        int index = 0;
        while (left != null && right != null && index < leftLength)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IntegerArraysEqual(long[]? left, long[]? right)
    {
        int leftLength = left == null ? 0 : left.Length;
        int rightLength = right == null ? 0 : right.Length;
        if (leftLength != rightLength) return false;
        int index = 0;
        while (left != null && right != null && index < leftLength)
        {
            if (left[index] != right[index]) return false;
            index = index + 1;
        }
        return true;
    }

    internal static bool UnsignedIntegerArraysEqual(ulong[]? left, ulong[]? right)
    {
        int leftLength = left == null ? 0 : left.Length;
        int rightLength = right == null ? 0 : right.Length;
        if (leftLength != rightLength) return false;
        int index = 0;
        while (left != null && right != null && index < leftLength)
        {
            if (left[index] != right[index]) return false;
            index = index + 1;
        }
        return true;
    }

    internal static bool DoubleArraysEqual(double[]? left, double[]? right)
    {
        int leftLength = left == null ? 0 : left.Length;
        int rightLength = right == null ? 0 : right.Length;
        if (leftLength != rightLength) return false;
        int index = 0;
        while (left != null && right != null && index < leftLength)
        {
            if (!left[index].Equals(right[index])) return false;
            index = index + 1;
        }
        return true;
    }

    internal static bool StringArraysEqual(string[]? left, string[]? right)
    {
        int leftCount = left == null ? 0 : left.Length;
        int rightCount = right == null ? 0 : right.Length;
        if (leftCount != rightCount)
        {
            return false;
        }

        int index = 0;
        while (left != null && right != null && index < leftCount)
        {
            if (!SameText(left[index], right[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool TextListsEqual(List<string>? left, List<string>? right)
    {
        int leftCount = left == null ? 0 : left.Count;
        int rightCount = right == null ? 0 : right.Count;
        if (leftCount != rightCount) return false;
        int index = 0;
        while (left != null && right != null && index < leftCount)
        {
            if (!SameText(left[index], right[index])) return false;
            index = index + 1;
        }
        return true;
    }

    internal static string ValueText(MetadataValue? value)
    {
        MetadataValue actual = ActualValue(value);
        if (actual.Kind == MetadataValueKind.ByteArray)
        {
            return ByteDescription(actual.Bytes);
        }

        if (actual.Kind == MetadataValueKind.DirectoryReference)
        {
            MetadataDirectory directory = ActualDirectory(actual.DirectoryValue);
            return directory.Family.ToString() + "." + SafeText(directory.Group) + "/" + SafeText(directory.Name);
        }

        return ImageMetadataFormatterCore.FormatValue(actual);
    }

    internal static string ByteDescription(byte[] bytes)
    {
        return "<" + ByteLength(bytes).ToString(CultureInfo.InvariantCulture) + " bytes, sha256 " + HashHelper.ComputeSha256Hex(bytes) + ">";
    }

    internal static string TextListDescription(List<string> values)
    {
        if (values == null || values.Count == 0)
        {
            return "[]";
        }

        string text = "[";
        int index = 0;
        while (index < values.Count)
        {
            if (index > 0)
            {
                text = text + ", ";
            }

            text = text + SafeText(values[index]);
            index = index + 1;
        }

        return text + "]";
    }

    internal static void AddChanged(ImageMetadataDiffResult result, string field, string left, string right)
    {
        if (!SameText(left, right))
        {
            AddDifference(result, field + ": " + Quote(left) + " -> " + Quote(right));
        }
    }

    internal static void AddChanged(ImageMetadataDiffResult result, string field, int left, int right)
    {
        if (left != right)
        {
            AddDifference(result, field + ": " + left.ToString(CultureInfo.InvariantCulture) + " -> " + right.ToString(CultureInfo.InvariantCulture));
        }
    }

    internal static void AddChanged(ImageMetadataDiffResult result, string field, long left, long right)
    {
        if (left != right)
        {
            AddDifference(result, field + ": " + left.ToString(CultureInfo.InvariantCulture) + " -> " + right.ToString(CultureInfo.InvariantCulture));
        }
    }

    internal static void AddChanged(ImageMetadataDiffResult result, string field, bool left, bool right)
    {
        if (left != right)
        {
            AddDifference(result, field + ": " + BooleanText(left) + " -> " + BooleanText(right));
        }
    }

    internal static void AddChanged(ImageMetadataDiffResult result, string field, double left, double right)
    {
        if (!left.Equals(right))
        {
            AddDifference(result, field + ": " + left.ToString("R", CultureInfo.InvariantCulture) + " -> " + right.ToString("R", CultureInfo.InvariantCulture));
        }
    }

    internal static void AddDifference(ImageMetadataDiffResult result, string difference)
    {
        if (result == null)
        {
            return;
        }

        result.Differences.Add(SafeText(difference));
        result.AreEqual = false;
    }

    internal static string Quote(string text)
    {
        return "\"" + SafeText(text) + "\"";
    }

    internal static string BooleanText(bool value)
    {
        return value ? "true" : "false";
    }

    internal static ImageMetadataSummary? Summary(ImageMetadataDocument? document)
    {
        return document == null ? null : document.Summary;
    }

    internal static MetadataDirectory ActualDirectory(MetadataDirectory? directory)
    {
        return directory == null ? new MetadataDirectory() : directory;
    }

    internal static MetadataTag ActualTag(MetadataTag? tag)
    {
        return tag == null ? new MetadataTag() : tag;
    }

    internal static MetadataProfile ActualProfile(MetadataProfile? profile)
    {
        return profile == null ? new MetadataProfile() : profile;
    }

    internal static MetadataRawBlock ActualRawBlock(MetadataRawBlock? rawBlock)
    {
        return rawBlock == null ? new MetadataRawBlock() : rawBlock;
    }

    internal static MetadataValue ActualValue(MetadataValue? value)
    {
        return value == null ? new MetadataValue() : value;
    }

    internal static ImageMetadataSummary ActualSummary(ImageMetadataSummary? summary)
    {
        return summary == null ? new ImageMetadataSummary() : summary;
    }

    internal static GpsCoordinate ActualGps(GpsCoordinate? gps)
    {
        return gps == null ? new GpsCoordinate() : gps;
    }

    internal static bool SameText(string left, string right)
    {
        return string.Equals(SafeText(left), SafeText(right), StringComparison.Ordinal);
    }

    internal static string SafeText(string text)
    {
        return text == null ? "" : text;
    }

    internal static bool IsEmpty(string text)
    {
        return text == null || text.Length == 0;
    }

    internal static int ByteLength(byte[]? bytes)
    {
        return bytes == null ? 0 : bytes.Length;
    }
}

internal class ImageMetadataDirectoryDiffItem
{
    internal string Identity;
    internal MetadataDirectory Directory;

    internal ImageMetadataDirectoryDiffItem()
    {
        Identity = "";
        Directory = new MetadataDirectory();
    }
}

internal class ImageMetadataTagDiffItem
{
    internal string Identity;
    internal MetadataTag Tag;

    internal ImageMetadataTagDiffItem()
    {
        Identity = "";
        Tag = new MetadataTag();
    }
}

internal class ImageMetadataProfileDiffItem
{
    internal string Identity;
    internal MetadataProfile Profile;

    internal ImageMetadataProfileDiffItem()
    {
        Identity = "";
        Profile = new MetadataProfile();
    }
}

internal class ImageMetadataRawBlockDiffItem
{
    internal string Identity;
    internal MetadataRawBlock RawBlock;

    internal ImageMetadataRawBlockDiffItem()
    {
        Identity = "";
        RawBlock = new MetadataRawBlock();
    }
}
