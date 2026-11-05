namespace asn1;

internal class BerTlvNavigator
{
    internal BerTlvReaderImplementation Reader;

    internal BerTlvNavigator(BerTlvReaderImplementation reader)
    {
        this.Reader = reader == null
            ? throw new System.ArgumentNullException("reader")
            : reader;
    }

    internal BerTlvSkipResult SkipValue(
        System.ReadOnlySpan<byte> input,
        AsnEncodingRules rules)
    {
        BerReadResult read = this.Reader.ReadSpan(input, rules);
        return new BerTlvSkipResult
        {
            Status = read.Status,
            SkippedCount = read.Status == AsnDecodeStatus.Done
                ? read.ConsumedCount
                : 0,
            Diagnostics = read.Diagnostics
        };
    }

    internal BerTlvSelectionResult SelectChild(BerTlv parent, int index)
    {
        if (parent == null)
        {
            throw new System.ArgumentNullException("parent");
        }

        if (!parent.Constructed || index < 0 || index >= parent.Children.Length)
        {
            return new BerTlvSelectionResult
            {
                ErrorCode = "ASND0011",
                ErrorMessage = "The requested child does not exist."
            };
        }

        return new BerTlvSelectionResult
        {
            Success = true,
            Value = parent.Children[index]
        };
    }

    internal BerTlvSelectionResult SelectNextSibling(
        BerTlv[] siblings,
        int currentIndex)
    {
        if (siblings == null)
        {
            throw new System.ArgumentNullException("siblings");
        }

        if (currentIndex < 0
            || currentIndex >= siblings.Length - 1
            || siblings[currentIndex + 1] == null)
        {
            return new BerTlvSelectionResult
            {
                ErrorCode = "ASND0012",
                ErrorMessage = "The requested sibling does not exist."
            };
        }

        return new BerTlvSelectionResult
        {
            Success = true,
            Value = siblings[currentIndex + 1]
        };
    }
}
