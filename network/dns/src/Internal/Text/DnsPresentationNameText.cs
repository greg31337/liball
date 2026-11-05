namespace dns;

internal static class DnsPresentationNameText
{
    internal static DnsTextResult ParseNameToken(
        string text,
        DnsTextOptions options)
    {
        if (text == "@")
        {
            if (options.Origin == null)
            {
                return DnsTextOperations.Failure(
                    DnsDiagnosticCode.InvalidText,
                    "name",
                    "The at-sign owner requires an absolute origin.",
                    0);
            }

            DnsTextResult originResult = PresentationDnsName.FromRawName(
                RawDnsName.ValidateName(options.Origin), text);
            if (originResult.Success && originResult.Name != null &&
                !originResult.Name.IsAbsolute)
            {
                return DnsTextOperations.Failure(
                    DnsDiagnosticCode.InvalidText,
                    "origin",
                    "A presentation origin must be absolute.",
                    0);
            }

            return originResult;
        }

        return PresentationDnsName.ParseName(text, options.Origin);
    }

    internal static DnsTextResult FormatName(
        DnsName name,
        DnsTextOptions options)
    {
        if (options == null)
        {
            return DnsTextOperations.Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options",
                "DNS text options are required.",
                0);
        }

        RawDnsNameResult validated = RawDnsName.ValidateName(name);
        if (!validated.Success || validated.Name == null)
        {
            return PresentationDnsName.FromRawName(validated, string.Empty);
        }

        DnsName selected = validated.Name;
        if (options.Canonical)
        {
            selected = RecordSetOperations.CopyName(selected, true);
        }

        if (options.Origin == null)
        {
            return PresentationDnsName.FormatName(selected);
        }

        RawDnsNameResult originResult = RawDnsName.ValidateName(options.Origin);
        if (!originResult.Success || originResult.Name == null)
        {
            return PresentationDnsName.FromRawName(
                originResult, string.Empty);
        }

        DnsName origin = originResult.Name;
        if (!origin.IsAbsolute)
        {
            return DnsTextOperations.Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "origin",
                "A presentation origin must be absolute.",
                0);
        }

        DnsNameOperationResult suffix = DnsNameOperations.HasNameSuffix(
            selected, origin);
        if (!suffix.Success || !suffix.BooleanValue)
        {
            return PresentationDnsName.FormatName(selected);
        }

        if (selected.Labels.Count == origin.Labels.Count)
        {
            DnsTextResult atResult = DnsTextOperations.SuccessText("@");
            atResult.Name = selected;
            return atResult;
        }

        DnsNameOperationResult relative = DnsNameOperations.RelativizeName(
            selected, origin);
        if (!relative.Success || relative.Name == null)
        {
            DnsTextResult failure = new DnsTextResult();
            int index = 0;
            while (index < relative.Diagnostics.Count)
            {
                failure.Diagnostics.Add(relative.Diagnostics[index]);
                index = index + 1;
            }

            return failure;
        }

        return PresentationDnsName.FormatName(relative.Name);
    }
}
