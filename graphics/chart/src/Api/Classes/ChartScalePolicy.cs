namespace chart;
/// <summary>Composes domain resolution, range mapping, unknown-value, and subplot-sharing policies.</summary>
public class ChartScalePolicy : IChartScalePolicy
{
    internal bool AutomaticDomain;
    internal double DomainPadding;
    internal bool IncludeZero;
    internal bool NiceExpansion;
    internal int NiceTickCount;
    internal bool Reverse;
    internal bool Clamp;
    internal bool HasUnknownValue;
    internal double UnknownValue;
    internal ChartScaleSharing Sharing;

    /// <summary>Initializes an explicit, unclamped, non-reversed scale policy with independent subplot domains.</summary>
    public ChartScalePolicy()
    {
        AutomaticDomain = false;
        DomainPadding = 0.0;
        IncludeZero = false;
        NiceExpansion = false;
        NiceTickCount = 5;
        Reverse = false;
        Clamp = false;
        HasUnknownValue = false;
        UnknownValue = 0.0;
        Sharing = ChartScaleSharing.Independent;
    }

    /// <summary>Chooses whether domains are computed from data instead of supplied explicitly.</summary>
    /// <param name = "automaticDomain"><c>true</c> to compute domains from finite data values.</param>
    public void SetAutomaticDomain(bool automaticDomain)
    {
        AutomaticDomain = automaticDomain;
    }

    /// <summary>Sets symmetric domain padding as a fraction of domain length.</summary>
    /// <param name = "fraction">The nonnegative padding fraction applied to each end.</param>
    public void SetDomainPadding(double fraction)
    {
        ChartLinearScale.ValidateFinite(fraction, "fraction");
        if (fraction < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("fraction");
        }

        DomainPadding = fraction;
    }

    /// <summary>Chooses whether resolved domains include zero.</summary>
    /// <param name = "includeZero"><c>true</c> to include zero.</param>
    public void SetIncludeZero(bool includeZero)
    {
        IncludeZero = includeZero;
    }

    /// <summary>Configures nice-number expansion.</summary>
    /// <param name = "niceExpansion"><c>true</c> to expand domains to nice boundaries.</param>
    /// <param name = "tickCount">The positive target tick count.</param>
    public void SetNiceExpansion(bool niceExpansion, int tickCount)
    {
        if (tickCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("tickCount");
        }

        NiceExpansion = niceExpansion;
        NiceTickCount = tickCount;
    }

    /// <summary>Chooses whether output ranges are reversed.</summary>
    /// <param name = "reverse"><c>true</c> to reverse output ranges.</param>
    public void SetReverse(bool reverse)
    {
        Reverse = reverse;
    }

    /// <summary>Chooses whether values outside the domain are clamped.</summary>
    /// <param name = "clamp"><c>true</c> to clamp values to the domain.</param>
    public void SetClamp(bool clamp)
    {
        Clamp = clamp;
    }

    /// <summary>Sets the mapped output returned for non-finite input values.</summary>
    /// <param name = "value">The finite fallback output.</param>
    public void SetUnknownValue(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        HasUnknownValue = true;
        UnknownValue = value;
    }

    /// <summary>Restores exception reporting for non-finite input values.</summary>
    public void ClearUnknownValue()
    {
        HasUnknownValue = false;
    }

    /// <summary>Sets how subplot domains are shared.</summary>
    /// <param name = "sharing">The subplot scale-sharing mode.</param>
    public void SetSharing(ChartScaleSharing sharing)
    {
        int value = (int)sharing;
        if (value < 0 || value > 2)
        {
            throw new System.ArgumentOutOfRangeException("sharing");
        }

        Sharing = sharing;
    }

    /// <summary>Resolves an automatic or explicit domain and applies domain policies.</summary>
    /// <param name = "values">The values used for an automatic domain.</param>
    /// <param name = "explicitDomain">The domain used when automatic domains are disabled.</param>
    /// <returns>The resolved domain.</returns>
    public ChartRange ResolveDomain(double[] values, ChartRange? explicitDomain)
    {
        ChartRange domain;
        if (AutomaticDomain)
        {
            domain = FindFiniteDomain(values);
        }
        else
        {
            domain = ValidateExplicitDomain(explicitDomain);
        }

        return ApplyDomainPolicies(domain);
    }

    /// <summary>Resolves independent, shared, or grouped domains for subplots.</summary>
    /// <param name = "subplotValues">The values for each subplot.</param>
    /// <param name = "sharingGroups">The sharing-group identifier for each subplot.</param>
    /// <returns>One resolved domain for each subplot.</returns>
    public ChartRange[] ResolveSubplotDomains(double[][] subplotValues, string[] sharingGroups)
    {
        ValidateSubplots(subplotValues, sharingGroups);
        ChartRange[] rawDomains = new ChartRange[subplotValues.Length];
        int index = 0;
        while (index < subplotValues.Length)
        {
            rawDomains[index] = FindFiniteDomain(subplotValues[index]);
            index = index + 1;
        }

        if (Sharing == ChartScaleSharing.Shared)
        {
            ChartRange shared = ApplyDomainPolicies(MergeDomains(rawDomains));
            return RepeatDomain(shared, rawDomains.Length);
        }

        ChartRange[] result = new ChartRange[rawDomains.Length];
        index = 0;
        while (index < rawDomains.Length)
        {
            ChartRange domain = rawDomains[index];
            if (Sharing == ChartScaleSharing.Grouped && sharingGroups[index].Length > 0)
            {
                domain = MergeGroup(rawDomains, sharingGroups, sharingGroups[index]);
            }

            result[index] = ApplyDomainPolicies(domain);
            index = index + 1;
        }

        return result;
    }

    /// <summary>Resolves subplot domains using sharing groups configured on plots.</summary>
    /// <param name = "plots">The subplots whose axis sharing groups are used.</param>
    /// <param name = "subplotValues">The values for each subplot.</param>
    /// <param name = "axisKind">The horizontal or vertical axis whose group is used.</param>
    /// <returns>One resolved domain for each subplot.</returns>
    public ChartRange[] ResolvePlotDomains(ChartPlot[] plots, double[][] subplotValues, ChartAxisKind axisKind)
    {
        if (plots == null)
        {
            throw new System.ArgumentNullException("plots");
        }

        if (subplotValues == null)
        {
            throw new System.ArgumentNullException("subplotValues");
        }

        if (plots.Length == 0 || plots.Length != subplotValues.Length)
        {
            throw new System.ArgumentException("Plots and subplot values must have the same nonzero length.");
        }

        if (axisKind != ChartAxisKind.X && axisKind != ChartAxisKind.Y)
        {
            throw new System.ArgumentOutOfRangeException("axisKind");
        }

        string[] groups = new string[plots.Length];
        int index = 0;
        while (index < plots.Length)
        {
            if (plots[index] == null)
            {
                throw new System.ArgumentException("Plots cannot contain null values.");
            }

            if (axisKind == ChartAxisKind.X)
            {
                groups[index] = plots[index].ShareXAxisGroup;
            }
            else
            {
                groups[index] = plots[index].ShareYAxisGroup;
            }

            index = index + 1;
        }

        return ResolveSubplotDomains(subplotValues, groups);
    }

    /// <summary>Maps a value linearly while applying unknown, reversal, and clamp policies.</summary>
    /// <param name = "value">The input value.</param>
    /// <param name = "domain">The input domain.</param>
    /// <param name = "range">The output range.</param>
    /// <returns>The mapped output.</returns>
    public double Map(double value, ChartRange domain, ChartRange range)
    {
        ValidateMapRange(domain, "domain");
        ValidateMapRange(range, "range");
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            if (HasUnknownValue)
            {
                return UnknownValue;
            }

            throw new System.ArgumentOutOfRangeException("value");
        }

        double amount = (value - domain.Minimum) / (domain.Maximum - domain.Minimum);
        if (Clamp)
        {
            amount = ChartScaleUtilities.ClampUnit(amount);
        }

        if (Reverse)
        {
            amount = 1.0 - amount;
        }

        return range.Minimum + amount * (range.Maximum - range.Minimum);
    }

    /// <summary>Creates an independent copy of this policy.</summary>
    /// <returns>The copied policy.</returns>
    public ChartScalePolicy Copy()
    {
        ChartScalePolicy copy = new ChartScalePolicy();
        copy.AutomaticDomain = AutomaticDomain;
        copy.DomainPadding = DomainPadding;
        copy.IncludeZero = IncludeZero;
        copy.NiceExpansion = NiceExpansion;
        copy.NiceTickCount = NiceTickCount;
        copy.Reverse = Reverse;
        copy.Clamp = Clamp;
        copy.HasUnknownValue = HasUnknownValue;
        copy.UnknownValue = UnknownValue;
        copy.Sharing = Sharing;
        return copy;
    }

    internal ChartRange ApplyDomainPolicies(ChartRange domain)
    {
        ChartRange result = new ChartRange(domain.Minimum, domain.Maximum);
        if (IncludeZero)
        {
            result = ChartScaleUtilities.IncludeZero(result.Minimum, result.Maximum);
        }

        if (DomainPadding > 0.0)
        {
            result = ChartScaleUtilities.PadRange(result.Minimum, result.Maximum, DomainPadding);
        }

        if (NiceExpansion)
        {
            result = ChartScaleUtilities.NiceDomain(result.Minimum, result.Maximum, NiceTickCount);
        }

        return result;
    }

    internal static ChartRange FindFiniteDomain(double[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        bool found = false;
        double minimum = 0.0;
        double maximum = 0.0;
        int index = 0;
        while (index < values.Length)
        {
            double value = values[index];
            if (!double.IsNaN(value) && !double.IsInfinity(value))
            {
                if (!found || value < minimum)
                {
                    minimum = value;
                }

                if (!found || value > maximum)
                {
                    maximum = value;
                }

                found = true;
            }

            index = index + 1;
        }

        if (!found)
        {
            throw new System.ArgumentException("Automatic domains require at least one finite value.");
        }

        if (minimum == maximum)
        {
            double expansion = System.Math.Abs(minimum) * 0.05;
            if (expansion == 0.0)
            {
                expansion = 0.5;
            }

            minimum = minimum - expansion;
            maximum = maximum + expansion;
        }

        return new ChartRange(minimum, maximum);
    }

    internal static ChartRange ValidateExplicitDomain(ChartRange? domain)
    {
        if (domain == null)
        {
            throw new System.ArgumentNullException("explicitDomain");
        }

        if (!domain.IsValid() || domain.Length() == 0.0)
        {
            throw new System.ArgumentException("Explicit scale domain must be finite, increasing, and nonzero.");
        }

        return new ChartRange(domain.Minimum, domain.Maximum);
    }

    internal static void ValidateSubplots(double[][] subplotValues, string[] sharingGroups)
    {
        if (subplotValues == null)
        {
            throw new System.ArgumentNullException("subplotValues");
        }

        if (sharingGroups == null)
        {
            throw new System.ArgumentNullException("sharingGroups");
        }

        if (subplotValues.Length == 0 || subplotValues.Length != sharingGroups.Length)
        {
            throw new System.ArgumentException("Subplot values and sharing groups must have the same nonzero length.");
        }

        int index = 0;
        while (index < sharingGroups.Length)
        {
            if (sharingGroups[index] == null)
            {
                throw new System.ArgumentException("Scale sharing groups cannot be null.");
            }

            index = index + 1;
        }
    }

    internal static ChartRange MergeDomains(ChartRange[] domains)
    {
        double minimum = domains[0].Minimum;
        double maximum = domains[0].Maximum;
        int index = 1;
        while (index < domains.Length)
        {
            if (domains[index].Minimum < minimum)
            {
                minimum = domains[index].Minimum;
            }

            if (domains[index].Maximum > maximum)
            {
                maximum = domains[index].Maximum;
            }

            index = index + 1;
        }

        return new ChartRange(minimum, maximum);
    }

    internal static ChartRange MergeGroup(ChartRange[] domains, string[] groups, string group)
    {
        double minimum = 0.0;
        double maximum = 0.0;
        bool found = false;
        int index = 0;
        while (index < domains.Length)
        {
            if (groups[index] == group)
            {
                if (!found || domains[index].Minimum < minimum)
                {
                    minimum = domains[index].Minimum;
                }

                if (!found || domains[index].Maximum > maximum)
                {
                    maximum = domains[index].Maximum;
                }

                found = true;
            }

            index = index + 1;
        }

        return new ChartRange(minimum, maximum);
    }

    internal static ChartRange[] RepeatDomain(ChartRange domain, int count)
    {
        ChartRange[] result = new ChartRange[count];
        int index = 0;
        while (index < count)
        {
            result[index] = new ChartRange(domain.Minimum, domain.Maximum);
            index = index + 1;
        }

        return result;
    }

    internal static void ValidateMapRange(ChartRange range, string name)
    {
        if (range == null)
        {
            throw new System.ArgumentNullException(name);
        }

        ChartLinearScale.ValidateDomain(range.Minimum, range.Maximum);
    }

    /// <summary>Returns whether domains are computed from data.</summary>
    /// <returns>The current setting.</returns>
    public bool GetAutomaticDomain()
    {
        return AutomaticDomain;
    }

    /// <summary>Returns the domain-padding fraction.</summary>
    /// <returns>The current setting.</returns>
    public double GetDomainPadding()
    {
        return DomainPadding;
    }

    /// <summary>Returns whether domains include zero.</summary>
    /// <returns>The current setting.</returns>
    public bool GetIncludeZero()
    {
        return IncludeZero;
    }

    /// <summary>Returns whether nice expansion is enabled.</summary>
    /// <returns>The current setting.</returns>
    public bool GetNiceExpansion()
    {
        return NiceExpansion;
    }

    /// <summary>Returns the target tick count for nice expansion.</summary>
    /// <returns>The current setting.</returns>
    public int GetNiceTickCount()
    {
        return NiceTickCount;
    }

    /// <summary>Returns whether output ranges are reversed.</summary>
    /// <returns>The current setting.</returns>
    public bool GetReverse()
    {
        return Reverse;
    }

    /// <summary>Returns whether mapping clamps values to the domain.</summary>
    /// <returns>The current setting.</returns>
    public bool GetClamp()
    {
        return Clamp;
    }

    /// <summary>Returns whether an unknown-value fallback is configured.</summary>
    /// <returns>The current setting.</returns>
    public bool GetHasUnknownValue()
    {
        return HasUnknownValue;
    }

    /// <summary>Returns the unknown-value fallback.</summary>
    /// <returns>The current setting.</returns>
    public double GetUnknownValue()
    {
        return UnknownValue;
    }

    /// <summary>Returns the subplot scale-sharing mode.</summary>
    /// <returns>The current setting.</returns>
    public ChartScaleSharing GetSharing()
    {
        return Sharing;
    }
}
