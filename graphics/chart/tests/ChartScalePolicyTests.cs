namespace chart.Tests;

[TestFixture]
public class ChartScalePolicyTests
{
    [Test]
    public void ResolveDomain_ComposesAutomaticIncludeZeroPaddingAndNicePolicies()
    {
        ChartScalePolicy policy = new ChartScalePolicy();
        policy.SetAutomaticDomain(true);

        ChartRange automatic = policy.ResolveDomain(new double[] { double.NaN, 3.2, 9.7, double.PositiveInfinity }, null);

        Assert.That(automatic.GetMinimum(), Is.EqualTo(3.2));
        Assert.That(automatic.GetMaximum(), Is.EqualTo(9.7));

        policy.SetIncludeZero(true);
        policy.SetDomainPadding(0.1);
        policy.SetNiceExpansion(true, 5);

        ChartRange expanded = policy.ResolveDomain(new double[] { 3.2, 9.7 }, null);

        Assert.That(expanded.GetMinimum(), Is.EqualTo(-5.0));
        Assert.That(expanded.GetMaximum(), Is.EqualTo(15.0));
    }

    [Test]
    public void ResolveDomain_UsesExplicitDomainWhenAutomaticIsDisabled()
    {
        ChartScalePolicy policy = new ChartScalePolicy();

        ChartRange domain = policy.ResolveDomain(new double[] { -100.0, 100.0 }, new ChartRange(10.0, 20.0));

        Assert.That(domain.GetMinimum(), Is.EqualTo(10.0));
        Assert.That(domain.GetMaximum(), Is.EqualTo(20.0));
    }

    [Test]
    public void ResolveDomain_ExpandsSingleValueAutomaticDomains()
    {
        ChartScalePolicy policy = new ChartScalePolicy();
        policy.SetAutomaticDomain(true);

        ChartRange zero = policy.ResolveDomain(new double[] { 0.0 }, null);
        ChartRange positive = policy.ResolveDomain(new double[] { 10.0 }, null);

        Assert.That(zero.GetMinimum(), Is.EqualTo(-0.5));
        Assert.That(zero.GetMaximum(), Is.EqualTo(0.5));
        Assert.That(positive.GetMinimum(), Is.EqualTo(9.5));
        Assert.That(positive.GetMaximum(), Is.EqualTo(10.5));
    }

    [Test]
    public void Map_AppliesClampReversalAndUnknownFallback()
    {
        ChartScalePolicy policy = new ChartScalePolicy();
        policy.SetClamp(true);
        policy.SetReverse(true);
        policy.SetUnknownValue(-1.0);

        ChartRange domain = new ChartRange(0.0, 10.0);
        ChartRange range = new ChartRange(0.0, 100.0);

        Assert.That(policy.Map(-5.0, domain, range), Is.EqualTo(100.0));
        Assert.That(policy.Map(5.0, domain, range), Is.EqualTo(50.0));
        Assert.That(policy.Map(15.0, domain, range), Is.EqualTo(0.0));
        Assert.That(policy.Map(double.NaN, domain, range), Is.EqualTo(-1.0));

        policy.ClearUnknownValue();
        Assert.Throws<System.ArgumentOutOfRangeException>(() => policy.Map(double.NaN, domain, range));
    }

    [Test]
    public void ResolveSubplotDomains_SupportsIndependentSharedAndGroupedScales()
    {
        ChartScalePolicy policy = new ChartScalePolicy();
        double[][] values = new double[][]
        {
            new double[] { 0.0, 1.0 },
            new double[] { 10.0, 20.0 },
            new double[] { 100.0, 200.0 }
        };
        string[] groups = new string[] { "left", "left", "" };

        ChartRange[] independent = policy.ResolveSubplotDomains(values, groups);
        policy.SetSharing(ChartScaleSharing.Shared);
        ChartRange[] shared = policy.ResolveSubplotDomains(values, groups);
        policy.SetSharing(ChartScaleSharing.Grouped);
        ChartRange[] grouped = policy.ResolveSubplotDomains(values, groups);

        Assert.That(independent[0].GetMaximum(), Is.EqualTo(1.0));
        Assert.That(independent[1].GetMinimum(), Is.EqualTo(10.0));
        Assert.That(shared[0].GetMinimum(), Is.EqualTo(0.0));
        Assert.That(shared[0].GetMaximum(), Is.EqualTo(200.0));
        Assert.That(shared[2].GetMaximum(), Is.EqualTo(200.0));
        Assert.That(grouped[0].GetMinimum(), Is.EqualTo(0.0));
        Assert.That(grouped[0].GetMaximum(), Is.EqualTo(20.0));
        Assert.That(grouped[1].GetMaximum(), Is.EqualTo(20.0));
        Assert.That(grouped[2].GetMinimum(), Is.EqualTo(100.0));
        Assert.That(grouped[2].GetMaximum(), Is.EqualTo(200.0));
    }

    [Test]
    public void ResolvePlotDomains_ConsumesPlotAxisSharingGroups()
    {
        ChartPlot first = new ChartPlot("first");
        ChartPlot second = new ChartPlot("second");
        ChartPlot third = new ChartPlot("third");
        first.SetSharedAxes("x-main", "y-first");
        second.SetSharedAxes("x-main", "y-second");
        third.SetSharedAxes("x-other", "y-third");

        ChartScalePolicy policy = new ChartScalePolicy();
        policy.SetSharing(ChartScaleSharing.Grouped);
        ChartRange[] domains = policy.ResolvePlotDomains(
            new ChartPlot[] { first, second, third },
            new double[][] { new double[] { 0.0, 1.0 }, new double[] { 10.0, 20.0 }, new double[] { 100.0, 200.0 } },
            ChartAxisKind.X);

        Assert.That(domains[0].GetMinimum(), Is.EqualTo(0.0));
        Assert.That(domains[0].GetMaximum(), Is.EqualTo(20.0));
        Assert.That(domains[1].GetMaximum(), Is.EqualTo(20.0));
        Assert.That(domains[2].GetMinimum(), Is.EqualTo(100.0));
    }

    [Test]
    public void Configuration_AppliesScalePolicyToDomainAndNumericMapping()
    {
        ChartScalePolicy policy = new ChartScalePolicy();
        policy.SetAutomaticDomain(true);
        policy.SetClamp(true);
        policy.SetReverse(true);
        policy.SetUnknownValue(-10.0);

        ChartConfiguration configuration = new ChartConfiguration();
        configuration.SetScalePolicy(policy);

        ChartRange domain = configuration.ResolveScaleDomain(new double[] { 2.0, 8.0 }, null);

        Assert.That(domain.GetMinimum(), Is.EqualTo(2.0));
        Assert.That(domain.GetMaximum(), Is.EqualTo(8.0));
        Assert.That(configuration.MapNumericValue(-5.0, 0.0, 10.0, 0.0, 100.0), Is.EqualTo(100.0));
        Assert.That(configuration.MapNumericValue(double.NaN, 0.0, 10.0, 0.0, 100.0), Is.EqualTo(-10.0));

        configuration.SetDirection(ChartDirection.Reversed);
        Assert.That(configuration.MapNumericValue(-5.0, 0.0, 10.0, 0.0, 100.0), Is.EqualTo(0.0));
    }

    [Test]
    public void Configuration_CopiesScalePoliciesIndependently()
    {
        ChartScalePolicy policy = new ChartScalePolicy();
        policy.SetClamp(true);
        ChartConfiguration configuration = new ChartConfiguration();
        configuration.SetScalePolicy(policy);

        policy.SetClamp(false);
        ChartScalePolicy returned = configuration.GetScalePolicy();
        returned.SetClamp(false);
        ChartConfiguration copy = configuration.Copy();

        Assert.That(configuration.GetScalePolicy().GetClamp(), Is.True);
        Assert.That(copy.GetScalePolicy().GetClamp(), Is.True);
    }

    [Test]
    public void ScalePolicy_ValidatesInvalidSettingsAndInputs()
    {
        ChartScalePolicy policy = new ChartScalePolicy();

        Assert.Throws<System.ArgumentOutOfRangeException>(() => policy.SetDomainPadding(-0.1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => policy.SetNiceExpansion(true, 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => policy.SetUnknownValue(double.NaN));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => policy.SetSharing((ChartScaleSharing)99));
        Assert.Throws<System.ArgumentNullException>(() => policy.ResolveDomain(new double[] { 1.0 }, null));

        policy.SetAutomaticDomain(true);
        Assert.Throws<System.ArgumentException>(() => policy.ResolveDomain(new double[] { double.NaN }, null));
        Assert.Throws<System.ArgumentException>(() => policy.ResolveSubplotDomains(new double[][] { new double[] { 0.0, 1.0 } }, new string[0]));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => policy.ResolvePlotDomains(new ChartPlot[] { new ChartPlot("plot") }, new double[][] { new double[] { 0.0, 1.0 } }, ChartAxisKind.Radial));
    }
}
