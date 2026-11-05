using System;
using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class DnsSrvSelector
{
    internal Random Randomizer;

    internal DnsSrvSelector()
    {
        Randomizer = new Random();
    }

    internal DnsSrvRecord? SelectRecord(List<DnsSrvRecord>? records)
    {
        if (records == null || records.Count == 0)
        {
            return null;
        }

        int minimumPriority = 65535;
        foreach (DnsSrvRecord record in records)
        {
            if (record == null || record.Target == null || record.Target.Length == 0 || record.Port < 0 || record.Port > 65535)
            {
                throw ProtocolFailure("A parsed DNS SRV record is invalid.");
            }

            if (record.Priority < minimumPriority)
            {
                minimumPriority = record.Priority;
            }
        }

        List<DnsSrvRecord> preferred = new List<DnsSrvRecord>();
        long totalWeight = 0L;
        foreach (DnsSrvRecord record in records)
        {
            if (record.Priority == minimumPriority && record.Weight == 0)
            {
                preferred.Add(record);
            }
        }

        foreach (DnsSrvRecord record in records)
        {
            if (record.Priority == minimumPriority && record.Weight != 0)
            {
                preferred.Add(record);
                totalWeight = totalWeight + record.Weight;
            }
        }

        if (preferred.Count == 1)
        {
            return preferred[0];
        }

        if (totalWeight == 0L)
        {
            return preferred[Randomizer.Next(preferred.Count)];
        }

        long selectedWeight = Randomizer.NextInt64(totalWeight + 1L);
        long cumulativeWeight = 0L;
        foreach (DnsSrvRecord record in preferred)
        {
            cumulativeWeight = cumulativeWeight + record.Weight;
            if (selectedWeight <= cumulativeWeight)
            {
                return record;
            }
        }

        throw ProtocolFailure("DNS SRV weight selection did not select a record.");
    }

    internal JmapProtocolException ProtocolFailure(string message)
    {
        return new JmapProtocolException(message, "dns", default(JsonElement));
    }
}
