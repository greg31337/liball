namespace uri.Tests;

public class UriPathUtilityApiTests
{
    [Test]
    public void EveryPublicMethodPreservesRootTrailingAndRepeatedSeparators()
    {
        IUriPathUtility utility = new UriPathUtility();
        OperationLimits limits = new OperationLimits();
        string[] segments = utility.SplitPathSegments("/a//b/", limits);
        string joined = utility.JoinPaths(
            new string[] { "", "a//", "/b", "" },
            limits);
        string common = utility.FindCommonBasePath(
            "/a//b/file",
            "/a//b/other",
            limits);
        string normalized = utility.RemoveDotSegments(
            "/a//./b/../c/",
            limits);

        Assert.Multiple(() =>
        {
            Assert.That(segments, Is.EqualTo(new string[] { "", "a", "", "b", "" }));
            Assert.That(joined, Is.EqualTo("/a//b/"));
            Assert.That(common, Is.EqualTo("/a//b/"));
            Assert.That(normalized, Is.EqualTo("/a//c/"));
        });
    }

    [Test]
    public void RelativePathsAndEmptyPathsRemainRelativeAndEmpty()
    {
        IUriPathUtility utility = new UriPathUtility();
        OperationLimits limits = new OperationLimits();

        Assert.Multiple(() =>
        {
            Assert.That(
                utility.RemoveDotSegments("a/./b/../c", limits),
                Is.EqualTo("a/c"));
            Assert.That(
                utility.JoinPaths(new string[] { "a", "b", "c" }, limits),
                Is.EqualTo("a/b/c"));
            Assert.That(
                utility.JoinPaths(new string[0], limits),
                Is.Empty);
            Assert.That(
                utility.SplitPathSegments("", limits),
                Is.Empty);
            Assert.That(
                utility.FindCommonBasePath("a/file", "a/other", limits),
                Is.EqualTo("a/"));
        });
    }

    [Test]
    public void EncodedReservedCharactersNeverBecomePathBoundaries()
    {
        IUriPathUtility utility = new UriPathUtility();
        OperationLimits limits = new OperationLimits();
        string[] segments = utility.SplitPathSegments(
            "/a%2Fb/%3F/%2E%2E",
            limits);
        string joined = utility.JoinPaths(
            new string[] { "/a%2Fb", "%3F/c" },
            limits);
        string common = utility.FindCommonBasePath(
            "/a%2Fb/c",
            "/a%2Fb/d",
            limits);
        string normalized = utility.RemoveDotSegments(
            "/a%2Fb/../%2E%2E/c",
            limits);

        Assert.Multiple(() =>
        {
            Assert.That(segments, Is.EqualTo(new string[] { "", "a%2Fb", "%3F", "%2E%2E" }));
            Assert.That(joined, Is.EqualTo("/a%2Fb/%3F/c"));
            Assert.That(common, Is.EqualTo("/a%2Fb/"));
            Assert.That(normalized, Is.EqualTo("/%2E%2E/c"));
        });
    }

    [Test]
    public void EveryPublicMethodRejectsInvalidPathSyntax()
    {
        IUriPathUtility utility = new UriPathUtility();
        OperationLimits limits = new OperationLimits();

        UriValidationException removal = Assert.Throws<UriValidationException>(() =>
            utility.RemoveDotSegments("a/%", limits))!;
        UriValidationException split = Assert.Throws<UriValidationException>(() =>
            utility.SplitPathSegments("bad path", limits))!;
        UriValidationException join = Assert.Throws<UriValidationException>(() =>
            utility.JoinPaths(new string[] { "a", "b?query" }, limits))!;
        UriValidationException common = Assert.Throws<UriValidationException>(() =>
            utility.FindCommonBasePath("/a", "/☃", limits))!;

        Assert.Multiple(() =>
        {
            Assert.That(removal.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidPercentEncoding));
            Assert.That(split.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidPath));
            Assert.That(join.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidPath));
            Assert.That(common.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidPath));
            Assert.That(removal.Diagnostic.Component, Is.EqualTo(UriComponent.Path));
            Assert.That(split.Diagnostic.Component, Is.EqualTo(UriComponent.Path));
            Assert.That(join.Diagnostic.Component, Is.EqualTo(UriComponent.Path));
            Assert.That(common.Diagnostic.Component, Is.EqualTo(UriComponent.Path));
        });
    }

    [Test]
    public void EveryPublicMethodRejectsNullInputsAndLimits()
    {
        IUriPathUtility utility = new UriPathUtility();
        OperationLimits limits = new OperationLimits();
        string[] missingPath = new string[1];
        missingPath[0] = null!;

        UriValidationException removal = Assert.Throws<UriValidationException>(() =>
            utility.RemoveDotSegments(null!, limits))!;
        UriValidationException split = Assert.Throws<UriValidationException>(() =>
            utility.SplitPathSegments("/a", null!))!;
        UriValidationException joinCollection = Assert.Throws<UriValidationException>(() =>
            utility.JoinPaths(null!, limits))!;
        UriValidationException joinItem = Assert.Throws<UriValidationException>(() =>
            utility.JoinPaths(missingPath, limits))!;
        UriValidationException common = Assert.Throws<UriValidationException>(() =>
            utility.FindCommonBasePath("/a", null!, limits))!;

        Assert.Multiple(() =>
        {
            AssertInvalidInput(removal);
            AssertInvalidInput(split);
            AssertInvalidInput(joinCollection);
            AssertInvalidInput(joinItem);
            AssertInvalidInput(common);
        });
    }

    [Test]
    public void InputAndComponentLimitsApplyBeforePathProcessing()
    {
        IUriPathUtility utility = new UriPathUtility();
        OperationLimits inputLimits = new OperationLimits();
        inputLimits.MaxInputCharacters = 3;
        OperationLimits componentLimits = new OperationLimits();
        componentLimits.MaxComponentCharacters = 2;

        UriValidationException singleInput = Assert.Throws<UriValidationException>(() =>
            utility.RemoveDotSegments("abcd", inputLimits))!;
        UriValidationException aggregateInput = Assert.Throws<UriValidationException>(() =>
            utility.FindCommonBasePath("ab", "cd", inputLimits))!;
        UriValidationException component = Assert.Throws<UriValidationException>(() =>
            utility.SplitPathSegments("abc", componentLimits))!;

        Assert.Multiple(() =>
        {
            AssertLimit(singleInput, UriComponent.Input);
            AssertLimit(aggregateInput, UriComponent.Input);
            AssertLimit(component, UriComponent.Path);
        });
    }

    [Test]
    public void SegmentLimitsApplyToSplitInputsAndJoinedOutputs()
    {
        IUriPathUtility utility = new UriPathUtility();
        OperationLimits splitLimits = new OperationLimits();
        splitLimits.MaxPathSegments = 2;
        OperationLimits joinLimits = new OperationLimits();
        joinLimits.MaxPathSegments = 2;

        UriValidationException split = Assert.Throws<UriValidationException>(() =>
            utility.SplitPathSegments("/a/b", splitLimits))!;
        UriValidationException join = Assert.Throws<UriValidationException>(() =>
            utility.JoinPaths(new string[] { "", "a", "" }, joinLimits))!;

        Assert.Multiple(() =>
        {
            AssertLimit(split, UriComponent.Path);
            AssertLimit(join, UriComponent.Path);
        });
    }

    [Test]
    public void OutputLimitsApplyToEveryGeneratedResult()
    {
        IUriPathUtility utility = new UriPathUtility();
        OperationLimits removalLimits = new OperationLimits();
        removalLimits.MaxOutputCharacters = 1;
        OperationLimits joinLimits = new OperationLimits();
        joinLimits.MaxOutputCharacters = 4;
        OperationLimits splitLimits = new OperationLimits();
        splitLimits.MaxOutputCharacters = 1;
        OperationLimits commonLimits = new OperationLimits();
        commonLimits.MaxOutputCharacters = 4;

        UriValidationException removal = Assert.Throws<UriValidationException>(() =>
            utility.RemoveDotSegments("/a/../b", removalLimits))!;
        UriValidationException join = Assert.Throws<UriValidationException>(() =>
            utility.JoinPaths(new string[] { "ab", "cd" }, joinLimits))!;
        UriValidationException split = Assert.Throws<UriValidationException>(() =>
            utility.SplitPathSegments("/ab", splitLimits))!;
        UriValidationException common = Assert.Throws<UriValidationException>(() =>
            utility.FindCommonBasePath("/abc/file", "/abc/other", commonLimits))!;

        Assert.Multiple(() =>
        {
            AssertLimit(removal, UriComponent.Output);
            AssertLimit(join, UriComponent.Output);
            AssertLimit(split, UriComponent.Output);
            AssertLimit(common, UriComponent.Output);
        });
    }

    private static void AssertInvalidInput(UriValidationException exception)
    {
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
        Assert.That(exception.Diagnostic.Component, Is.EqualTo(UriComponent.Input));
        Assert.That(exception.Message, Is.EqualTo(exception.Diagnostic.Message));
    }

    private static void AssertLimit(
        UriValidationException exception,
        UriComponent component)
    {
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
        Assert.That(exception.Diagnostic.Component, Is.EqualTo(component));
        Assert.That(exception.Message, Is.EqualTo(exception.Diagnostic.Message));
    }
}
