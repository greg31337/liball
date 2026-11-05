namespace chart;
/// <summary>Defines domain resolution and mapping policies for chart scales.</summary>
public interface IChartScalePolicy
{
    /// <summary>Chooses whether domains are computed from data instead of supplied explicitly.</summary>
    /// <param name = "automaticDomain"><c>true</c> to compute domains from finite data values.</param>
    void SetAutomaticDomain(bool automaticDomain);
    /// <summary>Sets symmetric domain padding as a fraction of domain length.</summary>
    /// <param name = "fraction">The nonnegative padding fraction applied to each end.</param>
    void SetDomainPadding(double fraction);
    /// <summary>Chooses whether resolved domains include zero.</summary>
    /// <param name = "includeZero"><c>true</c> to include zero.</param>
    void SetIncludeZero(bool includeZero);
    /// <summary>Configures nice-number expansion.</summary>
    /// <param name = "niceExpansion"><c>true</c> to expand domains to nice boundaries.</param>
    /// <param name = "tickCount">The positive target tick count.</param>
    void SetNiceExpansion(bool niceExpansion, int tickCount);
    /// <summary>Chooses whether output ranges are reversed.</summary>
    /// <param name = "reverse"><c>true</c> to reverse output ranges.</param>
    void SetReverse(bool reverse);
    /// <summary>Chooses whether values outside the domain are clamped.</summary>
    /// <param name = "clamp"><c>true</c> to clamp values to the domain.</param>
    void SetClamp(bool clamp);
    /// <summary>Sets the mapped output returned for non-finite input values.</summary>
    /// <param name = "value">The finite fallback output.</param>
    void SetUnknownValue(double value);
    /// <summary>Restores exception reporting for non-finite input values.</summary>
    void ClearUnknownValue();
    /// <summary>Sets how subplot domains are shared.</summary>
    /// <param name = "sharing">The subplot scale-sharing mode.</param>
    void SetSharing(ChartScaleSharing sharing);
    /// <summary>Resolves an automatic or explicit domain and applies domain policies.</summary>
    /// <param name = "values">The values used for an automatic domain.</param>
    /// <param name = "explicitDomain">The domain used when automatic domains are disabled.</param>
    /// <returns>The resolved domain.</returns>
    ChartRange ResolveDomain(double[] values, ChartRange? explicitDomain);
    /// <summary>Resolves independent, shared, or grouped domains for subplots.</summary>
    /// <param name = "subplotValues">The values for each subplot.</param>
    /// <param name = "sharingGroups">The sharing-group identifier for each subplot.</param>
    /// <returns>One resolved domain for each subplot.</returns>
    ChartRange[] ResolveSubplotDomains(double[][] subplotValues, string[] sharingGroups);
    /// <summary>Resolves subplot domains using sharing groups configured on plots.</summary>
    /// <param name = "plots">The subplots whose axis sharing groups are used.</param>
    /// <param name = "subplotValues">The values for each subplot.</param>
    /// <param name = "axisKind">The horizontal or vertical axis whose group is used.</param>
    /// <returns>One resolved domain for each subplot.</returns>
    ChartRange[] ResolvePlotDomains(ChartPlot[] plots, double[][] subplotValues, ChartAxisKind axisKind);
    /// <summary>Maps a value linearly while applying unknown, reversal, and clamp policies.</summary>
    /// <param name = "value">The input value.</param>
    /// <param name = "domain">The input domain.</param>
    /// <param name = "range">The output range.</param>
    /// <returns>The mapped output.</returns>
    double Map(double value, ChartRange domain, ChartRange range);
    /// <summary>Creates an independent copy of this policy.</summary>
    /// <returns>The copied policy.</returns>
    ChartScalePolicy Copy();
    /// <summary>Returns whether domains are computed from data.</summary>
    /// <returns>The current setting.</returns>
    bool GetAutomaticDomain();
    /// <summary>Returns the domain-padding fraction.</summary>
    /// <returns>The current setting.</returns>
    double GetDomainPadding();
    /// <summary>Returns whether domains include zero.</summary>
    /// <returns>The current setting.</returns>
    bool GetIncludeZero();
    /// <summary>Returns whether nice expansion is enabled.</summary>
    /// <returns>The current setting.</returns>
    bool GetNiceExpansion();
    /// <summary>Returns the target tick count for nice expansion.</summary>
    /// <returns>The current setting.</returns>
    int GetNiceTickCount();
    /// <summary>Returns whether output ranges are reversed.</summary>
    /// <returns>The current setting.</returns>
    bool GetReverse();
    /// <summary>Returns whether mapping clamps values to the domain.</summary>
    /// <returns>The current setting.</returns>
    bool GetClamp();
    /// <summary>Returns whether an unknown-value fallback is configured.</summary>
    /// <returns>The current setting.</returns>
    bool GetHasUnknownValue();
    /// <summary>Returns the unknown-value fallback.</summary>
    /// <returns>The current setting.</returns>
    double GetUnknownValue();
    /// <summary>Returns the subplot scale-sharing mode.</summary>
    /// <returns>The current setting.</returns>
    ChartScaleSharing GetSharing();
}
