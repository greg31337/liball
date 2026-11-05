using System.Collections.Generic;

namespace dns;

internal class RawDnsNameResult
{
    internal bool Success;
    internal DnsName? Name;
    internal int LabelCount;
    internal int ExpandedWireLength;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal static class RawDnsName
{
    internal static RawDnsNameResult CreateRootName()
    {
        return BuildName(new List<byte[]>(), true);
    }

    internal static RawDnsNameResult CreateAbsoluteName(List<byte[]> labels)
    {
        return BuildName(labels, true);
    }

    internal static RawDnsNameResult CreateRelativeName(List<byte[]> labels)
    {
        return BuildName(labels, false);
    }

    internal static RawDnsNameResult ValidateName(DnsName name)
    {
        if (name == null)
        {
            return Fail(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "A DNS name is required.");
        }

        return BuildName(name.Labels, name.IsAbsolute);
    }

    internal static RawDnsNameResult BuildName(
        List<byte[]> labels,
        bool isAbsolute)
    {
        if (labels == null)
        {
            return Fail(
                DnsDiagnosticCode.InvalidLabel,
                "labels",
                "A DNS label collection is required.");
        }

        DnsName name = new DnsName();
        name.IsAbsolute = isAbsolute;
        int expandedWireLength = 1;
        int labelIndex = 0;
        while (labelIndex < labels.Count)
        {
            byte[] label = labels[labelIndex];
            if (label == null)
            {
                return Fail(
                    DnsDiagnosticCode.InvalidLabel,
                    $"labels[{labelIndex}]",
                    "A DNS label cannot be null.");
            }

            if (label.Length < 1 || label.Length > DnsProtocolLimits.MaximumLabelLength)
            {
                return Fail(
                    DnsDiagnosticCode.InvalidLabel,
                    $"labels[{labelIndex}]",
                    "A DNS label must contain between one and 63 octets.");
            }

            int encodedLabelLength = label.Length + 1;
            if (expandedWireLength >
                DnsProtocolLimits.MaximumNameLength - encodedLabelLength)
            {
                return Fail(
                    DnsDiagnosticCode.NameTooLong,
                    $"labels[{labelIndex}]",
                    "The expanded DNS name exceeds 255 octets.");
            }

            byte[] labelCopy = new byte[label.Length];
            System.Array.Copy(label, 0, labelCopy, 0, label.Length);
            name.Labels.Add(labelCopy);
            expandedWireLength = expandedWireLength + encodedLabelLength;
            labelIndex = labelIndex + 1;
        }

        RawDnsNameResult result = new RawDnsNameResult();
        result.Success = true;
        result.Name = name;
        result.LabelCount = labels.Count;
        result.ExpandedWireLength = expandedWireLength;
        return result;
    }

    internal static RawDnsNameResult Fail(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        RawDnsNameResult result = new RawDnsNameResult();
        result.Diagnostics.Add(diagnostic);
        return result;
    }
}
