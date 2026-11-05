namespace svg.Tests;

public class SvgInterfaceTests
{
    [Test]
    public void ResourceProviderImplementationIsInjectedAcrossPublicLoadingBoundary()
    {
        RecordingResourceProvider provider = new RecordingResourceProvider("<svg xmlns='http://www.w3.org/2000/svg'/>");
        SvgLoadOptions options = new SvgLoadOptions();
        options.ResourceProvider = provider;

        SvgLoadResult result = new SvgLoader().LoadUri("memory:interface.svg", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(provider.RequestCount, Is.EqualTo(1));
            Assert.That(provider.LastRequest.NormalizedUri, Is.EqualTo("memory:interface.svg"));
            Assert.That(provider.LastRequest.ResourceKind, Is.EqualTo(SvgResourceKind.RootDocument));
        });
    }

    [Test]
    public void DynamicSelectorStateImplementationIsConsumedWithoutDelegateFields()
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions("<svg xmlns='http://www.w3.org/2000/svg'><rect id='first'/><rect id='hovered'/></svg>", new SvgLoadOptions());
        SvgNode hovered = loaded.Document.DocumentElement.Children[1];
        TestDynamicSelectorState state = new TestDynamicSelectorState(hovered);
        SvgSelectorMatchOptions options = new SvgSelectorMatchOptions();
        options.DynamicState = state;

        SvgSelectorQueryResult result = new SvgQuery().QuerySelectorText(loaded.Document, "rect:hover", new SvgCssLimits(), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.First, Is.SameAs(hovered));
            Assert.That(state.HoverChecks, Is.EqualTo(2));
        });
    }
}

public class RecordingResourceProvider : ISvgResourceProvider
{
    public byte[] Bytes;
    public int RequestCount;
    public SvgResourceRequest LastRequest;

    public RecordingResourceProvider(string text)
    {
        Bytes = System.Text.Encoding.UTF8.GetBytes(text);
        RequestCount = 0;
        LastRequest = null;
    }

    public SvgResourceResponse GetResource(SvgResourceRequest request)
    {
        RequestCount = RequestCount + 1;
        LastRequest = request;
        SvgResourceResponse response = new SvgResourceResponse();
        response.Bytes = Bytes;
        response.FinalUri = request.NormalizedUri;
        response.ActualMediaType = "image/svg+xml";
        return response;
    }
}

public class TestDynamicSelectorState : ISvgDynamicSelectorState
{
    public SvgNode HoveredNode;
    public int HoverChecks;

    public TestDynamicSelectorState(SvgNode hoveredNode)
    {
        HoveredNode = hoveredNode;
        HoverChecks = 0;
    }

    public long GetVersion()
    {
        return 1;
    }

    public string GetLanguage()
    {
        return "";
    }

    public SvgSelectorLinkState GetLinkState(SvgNode node)
    {
        return SvgSelectorLinkState.None;
    }

    public bool IsHovered(SvgNode node)
    {
        HoverChecks = HoverChecks + 1;
        return object.ReferenceEquals(node, HoveredNode);
    }

    public bool IsActive(SvgNode node)
    {
        return false;
    }

    public bool IsFocused(SvgNode node)
    {
        return false;
    }

    public bool IsTarget(SvgNode node)
    {
        return false;
    }
}
