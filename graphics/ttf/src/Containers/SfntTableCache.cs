namespace ttf;

internal enum SfntTableVersionSupport
{
    Supported = 0,
    CompatibleExtension = 1,
    RawOnly = 2
}

internal class SfntTableVersion
{
    int majorVersion;
    int minorVersion;

    internal SfntTableVersion(int majorVersion, int minorVersion)
    {
        if (majorVersion < 0)
        {
            throw new System.ArgumentOutOfRangeException("majorVersion");
        }

        if (minorVersion < 0)
        {
            throw new System.ArgumentOutOfRangeException("minorVersion");
        }

        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }
}

internal class SfntTableParser
{
    int tag;
    int minimumMajorVersion;
    int maximumMajorVersion;
    int maximumMinorVersion;
    System.Func<SfntTableData, SfntTableVersion> versionReader;
    System.Func<
        SfntTableData,
        SfntTableVersion,
        SfntTableVersionSupport,
        FontLimits,
        SfntTableCache,
        object> parser;

    internal SfntTableParser(
        int tag,
        int minimumMajorVersion,
        int maximumMajorVersion,
        int maximumMinorVersion,
        System.Func<SfntTableData, SfntTableVersion> versionReader,
        System.Func<
            SfntTableData,
            SfntTableVersion,
            SfntTableVersionSupport,
            FontLimits,
            SfntTableCache,
            object> parser)
    {
        if (minimumMajorVersion < 0)
        {
            throw new System.ArgumentOutOfRangeException("minimumMajorVersion");
        }

        if (maximumMajorVersion < minimumMajorVersion)
        {
            throw new System.ArgumentOutOfRangeException("maximumMajorVersion");
        }

        if (maximumMinorVersion < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumMinorVersion");
        }

        if (versionReader == null)
        {
            throw new System.ArgumentNullException("versionReader");
        }

        if (parser == null)
        {
            throw new System.ArgumentNullException("parser");
        }

        this.tag = tag;
        this.minimumMajorVersion = minimumMajorVersion;
        this.maximumMajorVersion = maximumMajorVersion;
        this.maximumMinorVersion = maximumMinorVersion;
        this.versionReader = versionReader;
        this.parser = parser;
    }

    internal int GetTag()
    {
        return tag;
    }

    internal SfntTableVersion ReadVersion(SfntTableData table)
    {
        SfntTableVersion version = versionReader(table);

        if (version == null)
        {
            throw new System.InvalidOperationException(
                "A table version reader returned no version.");
        }

        return version;
    }

    internal SfntTableVersionSupport EvaluateVersion(
        SfntTableVersion version,
        FontLoadMode mode)
    {
        int majorVersion = version.GetMajorVersion();

        if (majorVersion < minimumMajorVersion ||
            majorVersion > maximumMajorVersion)
        {
            return SfntTableVersionSupport.RawOnly;
        }

        if (version.GetMinorVersion() <= maximumMinorVersion)
        {
            return SfntTableVersionSupport.Supported;
        }

        if (mode == FontLoadMode.Compatible)
        {
            return SfntTableVersionSupport.CompatibleExtension;
        }

        return SfntTableVersionSupport.RawOnly;
    }

    internal object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        object parsedData = parser(table, version, support, limits, cache);

        if (parsedData == null)
        {
            throw new System.InvalidOperationException(
                "A semantic table parser returned no parsed view.");
        }

        return parsedData;
    }
}

internal class SfntParsedTableView
{
    SfntTableData rawView;
    SfntTableVersion version;
    SfntTableVersionSupport versionSupport;
    object? parsedData;

    internal SfntParsedTableView(
        SfntTableData rawView,
        SfntTableVersion version,
        SfntTableVersionSupport versionSupport,
        object? parsedData)
    {
        if (rawView == null)
        {
            throw new System.ArgumentNullException("rawView");
        }

        if (version == null)
        {
            throw new System.ArgumentNullException("version");
        }

        this.rawView = rawView;
        this.version = version;
        this.versionSupport = versionSupport;
        this.parsedData = parsedData;
    }

    internal SfntTableData GetRawView()
    {
        return rawView;
    }

    internal SfntTableVersion GetVersion()
    {
        return version;
    }

    internal SfntTableVersionSupport GetVersionSupport()
    {
        return versionSupport;
    }

    internal bool HasParsedData()
    {
        return parsedData != null;
    }

    internal object GetParsedData()
    {
        if (parsedData == null)
        {
            throw new System.InvalidOperationException(
                "The table version is available only as raw data.");
        }

        return parsedData;
    }
}

internal class SfntTableCacheEntry
{
    SfntTableData rawView;
    object syncRoot;
    SfntTableParser? parser;
    System.Lazy<SfntParsedTableView>? parsedView;

    internal SfntTableCacheEntry(SfntTableData rawView)
    {
        if (rawView == null)
        {
            throw new System.ArgumentNullException("rawView");
        }

        this.rawView = rawView;
        syncRoot = new object();
    }

    internal SfntTableData GetRawView()
    {
        return rawView;
    }

    internal SfntParsedTableView GetParsedView(
        SfntTableParser requestedParser,
        FontLoadMode mode,
        FontLimits limits,
        SfntTableCache cache)
    {
        System.Lazy<SfntParsedTableView>? selectedView;

        lock (syncRoot)
        {
            if (parser == null)
            {
                parser = requestedParser;
                parsedView = new System.Lazy<SfntParsedTableView>(
                    () => CreateParsedView(requestedParser, mode, limits, cache),
                    System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
            }
            else if (!object.ReferenceEquals(parser, requestedParser))
            {
                throw new System.InvalidOperationException(
                    "A different semantic parser is already registered for this table.");
            }

            selectedView = parsedView;
        }

        System.ArgumentNullException.ThrowIfNull(selectedView);
        return selectedView.Value;
    }

    SfntParsedTableView CreateParsedView(
        SfntTableParser selectedParser,
        FontLoadMode mode,
        FontLimits limits,
        SfntTableCache cache)
    {
        SfntTableVersion version = selectedParser.ReadVersion(rawView);
        SfntTableVersionSupport support = selectedParser.EvaluateVersion(version, mode);
        object? parsedData = null;

        if (support != SfntTableVersionSupport.RawOnly)
        {
            parsedData = selectedParser.Parse(
                rawView,
                version,
                support,
                limits,
                cache);
        }

        return new SfntParsedTableView(
            rawView,
            version,
            support,
            parsedData);
    }
}

internal class SfntTableCache
{
    SfntTableStore rawTables;
    FontLoadMode mode;
    FontLimits limits;
    SfntTableCacheEntry[]? entries;
    bool semanticAccessAllowed;

    internal SfntTableCache(
        SfntTableStore rawTables,
        FontLoadMode mode,
        FontLimits limits,
        bool semanticAccessAllowed)
    {
        if (rawTables == null)
        {
            throw new System.ArgumentNullException("rawTables");
        }

        ValidateMode(mode);

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        this.rawTables = rawTables;
        this.mode = mode;
        this.limits = limits;
        this.semanticAccessAllowed = semanticAccessAllowed;
        InitializeEntries();
    }

    internal static SfntTableCache Create(
        SfntTableStore rawTables,
        FontLoadMode mode)
    {
        return new SfntTableCache(
            rawTables,
            mode,
            CreateDefaultLimits(mode),
            true);
    }

    internal static SfntTableCache Create(
        SfntTableStore rawTables,
        FontLoadMode mode,
        FontLimits limits)
    {
        return new SfntTableCache(rawTables, mode, limits, true);
    }

    internal static SfntTableCache Create(
        SfntTableStore rawTables,
        FontLoadMode mode,
        FontLimits limits,
        bool semanticAccessAllowed)
    {
        return new SfntTableCache(
            rawTables,
            mode,
            limits,
            semanticAccessAllowed);
    }

    void InitializeEntries()
    {
        entries = new SfntTableCacheEntry[rawTables.GetTableCount()];
        int index = 0;

        while (index < entries.Length)
        {
            entries[index] = new SfntTableCacheEntry(rawTables.GetTable(index));
            index += 1;
        }
    }

    internal int GetRawTableCount()
    {
        System.ArgumentNullException.ThrowIfNull(entries);
        return entries.Length;
    }

    internal FontLimits GetLimits()
    {
        return limits;
    }

    internal FontLoadMode GetMode()
    {
        return mode;
    }

    internal SfntTableData GetRawTable(int index)
    {
        System.ArgumentNullException.ThrowIfNull(entries);
        if (index < 0 || index >= entries.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return entries[index].GetRawView();
    }

    internal SfntTableData? FindRawTable(int tag)
    {
        SfntTableCacheEntry? entry = FindEntry(tag);
        return entry == null ? null : entry.GetRawView();
    }

    internal SfntParsedTableView? FindParsedTable(SfntTableParser parser)
    {
        if (parser == null)
        {
            throw new System.ArgumentNullException("parser");
        }

        if (!semanticAccessAllowed)
        {
            return null;
        }

        SfntTableCacheEntry? entry = FindEntry(parser.GetTag());
        return entry == null ? null : entry.GetParsedView(
            parser,
            mode,
            limits,
            this);
    }

    internal SfntTableStore GetRawTableStore()
    {
        return rawTables;
    }

    SfntTableCacheEntry? FindEntry(int tag)
    {
        int index = 0;

        System.ArgumentNullException.ThrowIfNull(entries);
        while (index < entries.Length)
        {
            if (entries[index].GetRawView().GetTag() == tag)
            {
                return entries[index];
            }

            index += 1;
        }

        return null;
    }

    static void ValidateMode(FontLoadMode mode)
    {
        if (mode != FontLoadMode.Strict &&
            mode != FontLoadMode.Compatible &&
            mode != FontLoadMode.InspectOnly)
        {
            throw new System.ArgumentOutOfRangeException("mode");
        }
    }

    static FontLimits CreateDefaultLimits(FontLoadMode mode)
    {
        if (mode == FontLoadMode.Strict)
        {
            return FontLimits.CreateStrict();
        }

        if (mode == FontLoadMode.InspectOnly)
        {
            return FontLimits.CreateInspectOnly();
        }

        return FontLimits.CreateCompatible();
    }
}
