using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class InChILayerComparisonTests
{
    [Test]
    public void AreEquivalent_ExactRequiresAllParsedLayers()
    {
        InChIWorkflows workflows = new InChIWorkflows();
        string left = "InChI=1S/C2H4/c1-2/h1-2H2/q+1/p-1/b2-1+/t1-/m0/s1/i1+1";
        string right = "InChI=1S/C2H4/c1-2/h1-2H2/q+1/p-1/b2-1+/t1+/m0/s1/i1+1";

        Assert.That(workflows.AreEquivalent(left, left, IdentityPolicy.Exact), Is.True);
        Assert.That(workflows.AreEquivalent(left, right, IdentityPolicy.Exact), Is.False);
    }

    [Test]
    public void AreEquivalent_ConnectivityUsesFormulaAndConnectivityOnly()
    {
        InChIWorkflows workflows = new InChIWorkflows();
        string left = "InChI=1S/C2H4/c1-2/h1-2H2/q+1/p-1/b2-1+/i1+1";
        string sameConnectivity = "InChI=1S/C2H4/c1-2/h1H,2H3/q-1/p+1/b2-1-/i2+1";
        string differentConnectivity = "InChI=1S/C2H4/c1;2/h1-2H2/q+1/p-1";

        Assert.That(workflows.AreEquivalent(left, sameConnectivity, IdentityPolicy.Connectivity), Is.True);
        Assert.That(workflows.AreEquivalent(left, differentConnectivity, IdentityPolicy.Connectivity), Is.False);
    }

    [Test]
    public void AreEquivalent_StereoInsensitiveIgnoresStereoLayer()
    {
        InChIWorkflows workflows = new InChIWorkflows();
        string left = "InChI=1S/C2H4/c1-2/h1-2H2/q+1/p-1/b2-1+/t1-/m0/s1/i1+1";
        string right = "InChI=1S/C2H4/c1-2/h1-2H2/q+1/p-1/b2-1-/t1+/m1/s1/i1+1";

        Assert.That(workflows.AreEquivalent(left, right, IdentityPolicy.Exact), Is.False);
        Assert.That(workflows.AreEquivalent(left, right, IdentityPolicy.StereoInsensitive), Is.True);
    }

    [Test]
    public void AreEquivalent_ChargeInsensitiveIgnoresChargeButKeepsProtonation()
    {
        InChIWorkflows workflows = new InChIWorkflows();
        string left = "InChI=1S/C2H4/c1-2/h1-2H2/q+1/p-1/b2-1+/i1+1";
        string chargeChanged = "InChI=1S/C2H4/c1-2/h1-2H2/q+2/p-1/b2-1+/i1+1";
        string protonChanged = "InChI=1S/C2H4/c1-2/h1-2H2/q+2/p+1/b2-1+/i1+1";

        Assert.That(workflows.AreEquivalent(left, chargeChanged, IdentityPolicy.Exact), Is.False);
        Assert.That(workflows.AreEquivalent(left, chargeChanged, IdentityPolicy.ChargeInsensitive), Is.True);
        Assert.That(workflows.AreEquivalent(left, protonChanged, IdentityPolicy.ChargeInsensitive), Is.False);
        Assert.That(workflows.HasSameProtonation(left, chargeChanged), Is.True);
        Assert.That(workflows.HasSameProtonation(left, protonChanged), Is.False);
    }

    [Test]
    public void AreEquivalent_IsotopeInsensitiveIgnoresIsotopeLayer()
    {
        InChIWorkflows workflows = new InChIWorkflows();
        string left = "InChI=1S/C2H4/c1-2/h1-2H2/q+1/p-1/b2-1+/i1+1";
        string right = "InChI=1S/C2H4/c1-2/h1-2H2/q+1/p-1/b2-1+/i2+1";

        Assert.That(workflows.AreEquivalent(left, right, IdentityPolicy.Exact), Is.False);
        Assert.That(workflows.AreEquivalent(left, right, IdentityPolicy.IsotopeInsensitive), Is.True);
        Assert.That(workflows.AreEquivalent(left, right, IdentityPolicy.IsotopeParent), Is.True);
    }

    [Test]
    public void BuildComparisonKey_MatchesSelectedIdentityPolicy()
    {
        InChIWorkflows workflows = new InChIWorkflows();
        string left = "InChI=1S/C2H4/c1-2/h1-2H2/q+1/p-1/b2-1+/t1-/m0/s1/i1+1";
        string right = "InChI=1S/C2H4/c1-2/h1-2H2/q+1/p-1/b2-1-/t1+/m1/s1/i1+1";

        Assert.That(workflows.BuildComparisonKey(left, IdentityPolicy.Exact), Is.Not.EqualTo(workflows.BuildComparisonKey(right, IdentityPolicy.Exact)));
        Assert.That(workflows.BuildComparisonKey(left, IdentityPolicy.StereoInsensitive), Is.EqualTo(workflows.BuildComparisonKey(right, IdentityPolicy.StereoInsensitive)));
    }

    [Test]
    public void AreEquivalent_ReturnsFalseForInvalidRecords()
    {
        InChIWorkflows workflows = new InChIWorkflows();

        Assert.That(workflows.AreEquivalent("not an inchi", "InChI=1S/H2O/h1H2", IdentityPolicy.Exact), Is.False);
        Assert.That(workflows.BuildComparisonKey("not an inchi", IdentityPolicy.Exact), Is.EqualTo(""));
    }
}
