using System;
using System.Collections.Generic;

namespace diameter;

internal interface IDiscoveryRandom
{
    int ReadDiscoveryRandomValue(int maximumExclusive);
}

internal class SystemDiscoveryRandom : IDiscoveryRandom
{
    internal SystemDiscoveryRandom()
    {
    }

    public int ReadDiscoveryRandomValue(int maximumExclusive)
    {
        return Random.Shared.Next(maximumExclusive);
    }
}

internal class SrvSelector
{
    internal IDiscoveryRandom RandomSource;

    internal SrvSelector(IDiscoveryRandom randomSource)
    {
        if (randomSource == null)
        {
            throw new ArgumentException("An SRV selector requires a random source.");
        }

        RandomSource = randomSource;
    }

    internal List<DiameterSrvRecord> OrderSrvRecords(
        List<DiameterSrvRecord> source)
    {
        List<DiameterSrvRecord> remaining = new List<DiameterSrvRecord>();
        int sourceIndex = 0;
        while (sourceIndex < source.Count)
        {
            DiameterSrvRecord record = source[sourceIndex];
            if (record != null && record.Priority >= 0 && record.Priority <= 65535 &&
                record.Weight >= 0 && record.Weight <= 65535 &&
                record.Port > 0 && record.Port <= 65535 &&
                !string.IsNullOrWhiteSpace(record.Target) && record.Target != ".")
            {
                remaining.Add(record);
            }

            sourceIndex = sourceIndex + 1;
        }

        List<DiameterSrvRecord> result = new List<DiameterSrvRecord>();
        while (remaining.Count > 0)
        {
            int priority = ReadLowestSrvPriority(remaining);
            List<DiameterSrvRecord> group = ReadSrvPriorityGroup(remaining, priority);
            while (group.Count > 0)
            {
                DiameterSrvRecord selected = SelectWeightedSrvRecord(group);
                result.Add(selected);
                group.Remove(selected);
                remaining.Remove(selected);
            }
        }

        return result;
    }

    internal int ReadLowestSrvPriority(List<DiameterSrvRecord> records)
    {
        int priority = records[0].Priority;
        int index = 1;
        while (index < records.Count)
        {
            if (records[index].Priority < priority)
            {
                priority = records[index].Priority;
            }

            index = index + 1;
        }

        return priority;
    }

    internal List<DiameterSrvRecord> ReadSrvPriorityGroup(
        List<DiameterSrvRecord> records,
        int priority)
    {
        List<DiameterSrvRecord> result = new List<DiameterSrvRecord>();
        int pass = 0;
        while (pass < 2)
        {
            int index = 0;
            while (index < records.Count)
            {
                DiameterSrvRecord record = records[index];
                bool include = pass == 0 && record.Weight == 0;
                if (pass == 1 && record.Weight != 0)
                {
                    include = true;
                }

                if (record.Priority == priority && include)
                {
                    result.Add(record);
                }

                index = index + 1;
            }

            pass = pass + 1;
        }

        return result;
    }

    internal DiameterSrvRecord SelectWeightedSrvRecord(
        List<DiameterSrvRecord> records)
    {
        int totalWeight = 0;
        int index = 0;
        while (index < records.Count)
        {
            totalWeight = totalWeight + records[index].Weight;
            index = index + 1;
        }

        int selection = RandomSource.ReadDiscoveryRandomValue(totalWeight + 1);
        if (selection < 0 || selection > totalWeight)
        {
            throw new DiameterPeerException("The discovery random source returned an invalid value.");
        }

        int runningWeight = 0;
        index = 0;
        while (index < records.Count)
        {
            runningWeight = runningWeight + records[index].Weight;
            if (runningWeight >= selection)
            {
                return records[index];
            }

            index = index + 1;
        }

        return records[records.Count - 1];
    }
}
