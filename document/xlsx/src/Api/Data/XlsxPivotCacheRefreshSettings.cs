using System;

namespace xlsx;

/// <summary>
/// Represents pivot-cache refresh and version settings.
/// </summary>
public class XlsxPivotCacheRefreshSettings
{
    /// <summary>Indicates whether cached data is saved.</summary>
    public bool SaveData;
    /// <summary>Indicates whether the save-data flag is present.</summary>
    public bool HasSaveData;
    /// <summary>Indicates whether the cache refreshes when opened.</summary>
    public bool RefreshOnLoad;
    /// <summary>Indicates whether the refresh-on-load flag is present.</summary>
    public bool HasRefreshOnLoad;
    /// <summary>Indicates whether refresh is enabled.</summary>
    public bool EnableRefresh;
    /// <summary>Indicates whether the enable-refresh flag is present.</summary>
    public bool HasEnableRefresh;
    /// <summary>Indicates whether refresh runs in the background.</summary>
    public bool BackgroundQuery;
    /// <summary>Indicates whether the background-query flag is present.</summary>
    public bool HasBackgroundQuery;
    /// <summary>Indicates whether the cache upgrades during refresh.</summary>
    public bool UpgradeOnRefresh;
    /// <summary>Indicates whether the upgrade-on-refresh flag is present.</summary>
    public bool HasUpgradeOnRefresh;
    /// <summary>Stores the cache creation version.</summary>
    public uint CreatedVersion;
    /// <summary>Indicates whether the creation version is present.</summary>
    public bool HasCreatedVersion;
    /// <summary>Stores the cache refresh version.</summary>
    public uint RefreshedVersion;
    /// <summary>Indicates whether the refresh version is present.</summary>
    public bool HasRefreshedVersion;
    /// <summary>Stores the minimum refreshable version.</summary>
    public uint MinimumRefreshableVersion;
    /// <summary>Indicates whether the minimum refreshable version is present.</summary>
    public bool HasMinimumRefreshableVersion;
    /// <summary>Stores the maximum retained missing-item count.</summary>
    public uint MissingItemsLimit;
    /// <summary>Indicates whether the missing-item limit is present.</summary>
    public bool HasMissingItemsLimit;
    /// <summary>Stores the serial date of the last refresh.</summary>
    public double RefreshedDate;
    /// <summary>Indicates whether the refreshed date is present.</summary>
    public bool HasRefreshedDate;

    /// <summary>Initializes empty pivot-cache refresh settings.</summary>
    public XlsxPivotCacheRefreshSettings()
    {
        SaveData = true;
        HasSaveData = false;
        RefreshOnLoad = false;
        HasRefreshOnLoad = false;
        EnableRefresh = true;
        HasEnableRefresh = false;
        BackgroundQuery = false;
        HasBackgroundQuery = false;
        UpgradeOnRefresh = false;
        HasUpgradeOnRefresh = false;
        CreatedVersion = 0;
        HasCreatedVersion = false;
        RefreshedVersion = 0;
        HasRefreshedVersion = false;
        MinimumRefreshableVersion = 0;
        HasMinimumRefreshableVersion = false;
        MissingItemsLimit = 0;
        HasMissingItemsLimit = false;
        RefreshedDate = 0;
        HasRefreshedDate = false;
    }
}
