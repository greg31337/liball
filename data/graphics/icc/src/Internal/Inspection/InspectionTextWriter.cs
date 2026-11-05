namespace icc;

internal static class InspectionTextWriter
{
    internal static string WriteProfile(IccProfile profile, IccInspectionDetail detail,
        int maximumValues)
    {
        ProfileDescriptionCore.ValidateProfile(profile);
        ProfileDescriptionCore.ValidateInspectionArguments(detail, maximumValues);
        string headline = ProfileDescriptionCore.SummarizeProfile(profile);
        if (detail == IccInspectionDetail.Summary)
        {
            return headline;
        }

        IccInspectionOptions options = new IccInspectionOptions();
        options.Detail = detail;
        options.MaximumDepth = 32;
        options.MaximumChildren = maximumValues;
        options.MaximumRawBytes = maximumValues;
        options.IncludeRawBytes = detail == IccInspectionDetail.Raw;
        IccProfileSummary summary = InspectionBuilder.Build(profile, options);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append(headline);
        builder.Append("\nHeader: declared size=");
        builder.Append(profile.Header.DeclaredSize.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        builder.Append("; class=");
        builder.Append(SignatureDatabase.DescribeProfileClass(profile.Header.ProfileClassSignature));
        builder.Append("; data=");
        builder.Append(SignatureDatabase.DescribeColorSpace(profile.Header.DataColorSpaceSignature));
        builder.Append("; PCS=");
        builder.Append(SignatureDatabase.DescribeColorSpace(
            profile.Header.ProfileConnectionSpaceSignature));
        builder.Append("\nProfile ID: ");
        builder.Append(ProfileDescriptionCore.DescribeProfileIdStatus(profile));
        builder.Append("\nTag table: ");
        int tableEntryCount = profile.TagTableEntries == null
            ? 0 : profile.TagTableEntries.Length;
        builder.Append(tableEntryCount.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(" entries");
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag == null)
            {
                builder.Append("\n- null tag");
            }
            else
            {
                builder.Append("\n- ");
                builder.Append(ProfileDescriptionCore.InspectTagText(tag, detail, maximumValues));
            }
            index = index + 1;
        }
        builder.Append("\nTransform selection:");
        AppendTransformSelections(builder, profile);
        builder.Append("\nValidation: ");
        builder.Append(summary.Validation.IsValid ? "valid" : "invalid");
        builder.Append("; issues=");
        builder.Append(summary.Validation.TotalIssueCount.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        if (detail == IccInspectionDetail.Detailed || detail == IccInspectionDetail.Raw)
        {
            builder.Append("\nInspection tree:");
            AppendTreeNode(builder, summary.Root, 0);
        }
        return builder.ToString();
    }

    internal static void AppendTransformSelections(System.Text.StringBuilder builder,
        IccProfile profile)
    {
        IccTransformDirection[] directions = new IccTransformDirection[]
        {
            IccTransformDirection.DeviceToPcs, IccTransformDirection.PcsToDevice
        };
        IccRenderingIntent[] intents = new IccRenderingIntent[]
        {
            IccRenderingIntent.Perceptual,
            IccRenderingIntent.MediaRelativeColorimetric,
            IccRenderingIntent.Saturation,
            IccRenderingIntent.IccAbsoluteColorimetric
        };
        int directionIndex = 0;
        while (directionIndex < directions.Length)
        {
            int intentIndex = 0;
            while (intentIndex < intents.Length)
            {
                builder.Append("\n  - ");
                builder.Append(ProfileDescriptionCore.DescribeTransformSelection(profile,
                    directions[directionIndex], intents[intentIndex]));
                intentIndex = intentIndex + 1;
            }
            directionIndex = directionIndex + 1;
        }
    }

    internal static void AppendTreeNode(System.Text.StringBuilder builder,
        IccInspectionNode node, int depth)
    {
        builder.Append('\n');
        int indent = 0;
        while (indent < depth)
        {
            builder.Append("  ");
            indent = indent + 1;
        }
        builder.Append(node.Name);
        builder.Append(": ");
        builder.Append(node.Value);
        if (node.Location.Offset >= 0)
        {
            builder.Append($" @ {node.Location.Offset}+{node.Location.Length}");
        }
        if (node.RawBytes.Length > 0)
        {
            builder.Append(" raw=");
            builder.Append(System.Convert.ToHexString(node.RawBytes));
        }
        if (node.IsTruncated)
        {
            builder.Append(" [truncated]");
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AppendTreeNode(builder, node.Children[index], depth + 1);
            index = index + 1;
        }
    }

    internal static string WriteTransformTrace(IccTransformTrace trace, int maximumValues)
    {
        if (trace == null || trace.Source == null || trace.Destination == null
            || trace.Intermediates == null || trace.Messages == null)
        {
            throw new IccProfileValidationException(
                "A transform trace and its vector and message collections are required.");
        }
        if (maximumValues < 0)
        {
            throw new IccProfileValidationException(
                "The repeated-value trace limit cannot be negative.");
        }
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append("Source: ");
        builder.Append(FormatVector(trace.Source, maximumValues));
        int limit = trace.Intermediates.Length < maximumValues
            ? trace.Intermediates.Length : maximumValues;
        int index = 0;
        while (index < limit)
        {
            builder.Append($"\nStage {index}: ");
            builder.Append(FormatVector(trace.Intermediates[index], maximumValues));
            if (index < trace.Messages.Length)
            {
                builder.Append(" — ");
                builder.Append(trace.Messages[index]);
            }
            index = index + 1;
        }
        if (limit < trace.Intermediates.Length)
        {
            builder.Append($"\n… {trace.Intermediates.Length - limit} intermediate vectors omitted");
        }
        builder.Append("\nDestination: ");
        builder.Append(FormatVector(trace.Destination, maximumValues));
        return builder.ToString();
    }

    internal static string FormatVector(IccColorVector vector, int maximumValues)
    {
        if (vector == null || vector.Components == null)
        {
            return "null vector";
        }
        return $"{DescriptionFormatter.DescribeColorSpaceKind(vector.ColorSpace)} {FormatDoubles(vector.Components, maximumValues)}";
    }

    internal static string FormatDoubles(double[] values, int maximumValues)
    {
        int limit = values.Length < maximumValues ? values.Length : maximumValues;
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append('[');
        int index = 0;
        while (index < limit)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }
            builder.Append(values[index].ToString("R",
                System.Globalization.CultureInfo.InvariantCulture));
            index = index + 1;
        }
        if (limit < values.Length)
        {
            if (limit > 0)
            {
                builder.Append(", ");
            }
            builder.Append($"… {values.Length - limit} more");
        }
        builder.Append(']');
        return builder.ToString();
    }

    internal static string FormatIntegers(int[] values, int maximumValues)
    {
        int limit = values.Length < maximumValues ? values.Length : maximumValues;
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append('[');
        int index = 0;
        while (index < limit)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }
            builder.Append(values[index].ToString(
                System.Globalization.CultureInfo.InvariantCulture));
            index = index + 1;
        }
        if (limit < values.Length)
        {
            if (limit > 0)
            {
                builder.Append(", ");
            }
            builder.Append($"… {values.Length - limit} more");
        }
        builder.Append(']');
        return builder.ToString();
    }
}
