namespace sip;

internal static class SipSrvOrdering
{
    internal static SipDnsWireRecord[] Order(SipDnsWireRecord[]? source,
        ISipRandomSource? random)
    {
        if (source == null || random == null)
        {
            throw new System.ArgumentNullException("source");
        }
        SipDnsWireRecord[] sorted = CopyArray(source);
        SortByPriority(sorted);
        System.Collections.Generic.List<SipDnsWireRecord> result =
            new System.Collections.Generic.List<SipDnsWireRecord>();
        int groupStart = 0;
        while (groupStart < sorted.Length)
        {
            int groupEnd = groupStart + 1;
            while (groupEnd < sorted.Length
                && sorted[groupEnd].Priority == sorted[groupStart].Priority)
            {
                groupEnd = groupEnd + 1;
            }
            System.Collections.Generic.List<SipDnsWireRecord> remaining =
                new System.Collections.Generic.List<SipDnsWireRecord>();
            int index = groupStart;
            while (index < groupEnd)
            {
                if (sorted[index].Weight == 0)
                {
                    remaining.Insert(0, sorted[index]);
                }
                else
                {
                    remaining.Add(sorted[index]);
                }
                index = index + 1;
            }
            while (remaining.Count > 0)
            {
                long weightSum = 0;
                index = 0;
                while (index < remaining.Count)
                {
                    weightSum = weightSum + remaining[index].Weight;
                    index = index + 1;
                }
                long selectedValue = NextValue(weightSum + 1, random);
                long running = 0;
                int selectedIndex = remaining.Count - 1;
                index = 0;
                while (index < remaining.Count)
                {
                    running = running + remaining[index].Weight;
                    if (running >= selectedValue)
                    {
                        selectedIndex = index;
                        break;
                    }
                    index = index + 1;
                }
                result.Add(remaining[selectedIndex]);
                remaining.RemoveAt(selectedIndex);
            }
            groupStart = groupEnd;
        }
        return result.ToArray();
    }

    static long NextValue(long exclusiveMaximum, ISipRandomSource random)
    {
        if (exclusiveMaximum <= 1)
        {
            return 0;
        }
        long randomRange = 4294967296;
        long acceptanceLimit = randomRange - randomRange % exclusiveMaximum;
        long value = acceptanceLimit;
        while (value >= acceptanceLimit)
        {
            byte[] bytes = new byte[4];
            random.FillBytes(bytes);
            value = (long)bytes[0] * 16777216 + (long)bytes[1] * 65536
                + (long)bytes[2] * 256 + bytes[3];
        }
        return value % exclusiveMaximum;
    }

    static SipDnsWireRecord[] CopyArray(SipDnsWireRecord[] source)
    {
        if (source.Length > 65535)
        {
            throw new System.ArgumentException("A DNS SRV set exceeds the wire record limit.",
                "source");
        }
        SipDnsWireRecord[] copy = new SipDnsWireRecord[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            if (source[index] == null || source[index].Priority < 0
                || source[index].Priority > 65535 || source[index].Weight < 0
                || source[index].Weight > 65535)
            {
                throw new System.ArgumentException("Every SRV record requires priority and weight.",
                    "source");
            }
            copy[index] = source[index];
            index = index + 1;
        }
        return copy;
    }

    static void SortByPriority(SipDnsWireRecord[] records)
    {
        int index = 1;
        while (index < records.Length)
        {
            SipDnsWireRecord current = records[index];
            int insertion = index;
            while (insertion > 0 && current.Priority < records[insertion - 1].Priority)
            {
                records[insertion] = records[insertion - 1];
                insertion = insertion - 1;
            }
            records[insertion] = current;
            index = index + 1;
        }
    }
}
