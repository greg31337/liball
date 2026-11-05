using System;
using System.Collections.Generic;

namespace log;

internal class LogTemplateCache
{
    internal int Capacity;
    internal int Count;
    internal int HitCount;
    internal int MissCount;
    internal int EvictionCount;
    internal Dictionary<string, LogTemplate> Entries = new Dictionary<string, LogTemplate>();
    internal List<string> Order = new List<string>();
    internal LogTemplateParser Parser = new LogTemplateParser();
    internal object SyncRoot = new object();

    internal LogTemplateCache(int capacity)
    {
        Initialize(capacity, new LogTemplateParser());
    }

    internal static LogTemplateCache Create(int capacity)
    {
        return new LogTemplateCache(capacity);
    }

    internal void Initialize(int capacity, LogTemplateParser parser)
    {
        Capacity = LogTemplateCache.NormalizeCapacity(capacity);
        Count = 0;
        HitCount = 0;
        MissCount = 0;
        EvictionCount = 0;
        Entries = new Dictionary<string, LogTemplate>();
        Order = new List<string>();
        if (parser == null)
        {
            Parser = new LogTemplateParser();
        }
        else
        {
            Parser = parser;
        }

        SyncRoot = new object();
    }

    internal LogTemplate GetOrAdd(string? template)
    {
        string key = LogTemplateCache.NormalizeKey(template);

        lock (SyncRoot)
        {
            if (Entries.ContainsKey(key))
            {
                HitCount = LogResult.IncrementCount(HitCount);
                Touch(key);
                Count = Entries.Count;
                return Entries[key];
            }

            MissCount = LogResult.IncrementCount(MissCount);
            LogTemplate parsed = Parser.Parse(key);
            Entries[key] = parsed;
            Order.Add(key);
            EvictOverflow();
            Count = Entries.Count;
            return parsed;
        }
    }

    internal bool Contains(string? template)
    {
        string key = LogTemplateCache.NormalizeKey(template);

        lock (SyncRoot)
        {
            return Entries.ContainsKey(key);
        }
    }

    internal void Clear()
    {
        lock (SyncRoot)
        {
            Entries.Clear();
            Order.Clear();
            Count = 0;
        }
    }

    internal void ResetCounters()
    {
        lock (SyncRoot)
        {
            HitCount = 0;
            MissCount = 0;
            EvictionCount = 0;
        }
    }

    internal void Touch(string key)
    {
        Order.Remove(key);
        Order.Add(key);
    }

    internal void EvictOverflow()
    {
        while (Entries.Count > Capacity && Order.Count > 0)
        {
            string oldestKey = Order[0];
            Order.RemoveAt(0);
            if (Entries.ContainsKey(oldestKey))
            {
                Entries.Remove(oldestKey);
                EvictionCount = LogResult.IncrementCount(EvictionCount);
            }
        }
    }

    internal static string NormalizeKey(string? template)
    {
        return LogResult.NormalizeText(template);
    }

    internal static int NormalizeCapacity(int capacity)
    {
        if (capacity < 1)
        {
            return 1;
        }

        return capacity;
    }
}
