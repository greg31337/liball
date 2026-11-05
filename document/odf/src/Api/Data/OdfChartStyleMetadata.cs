namespace odf;

/// <summary>Describes effective chart variant, marker, label, and interpolation style metadata.</summary>
public class OdfChartStyleMetadata
{
    /// <summary>The resolved chart style.</summary>
    public OdfStyle? Style;
    /// <summary>The preserved vertical-orientation value.</summary>
    public string VerticalValue = "";
    /// <summary>Whether vertical orientation is enabled.</summary>
    public bool IsVertical;
    /// <summary>Whether the vertical value is absent or valid.</summary>
    public bool IsVerticalValid = true;
    /// <summary>The preserved stacking value.</summary>
    public string StackedValue = "";
    /// <summary>Whether value stacking is enabled.</summary>
    public bool IsStacked;
    /// <summary>Whether the stacking value is absent or valid.</summary>
    public bool IsStackedValid = true;
    /// <summary>The preserved percentage value.</summary>
    public string PercentageValue = "";
    /// <summary>Whether percentage stacking is enabled.</summary>
    public bool IsPercentage;
    /// <summary>Whether the percentage value is absent or valid.</summary>
    public bool IsPercentageValid = true;
    /// <summary>The preserved three-dimensional value.</summary>
    public string ThreeDimensionalValue = "";
    /// <summary>Whether three-dimensional rendering is enabled.</summary>
    public bool IsThreeDimensional;
    /// <summary>Whether the three-dimensional value is absent or valid.</summary>
    public bool IsThreeDimensionalValid = true;
    /// <summary>The preserved deep-series value.</summary>
    public string DeepValue = "";
    /// <summary>Whether series are arranged in depth.</summary>
    public bool IsDeep;
    /// <summary>Whether the deep-series value is absent or valid.</summary>
    public bool IsDeepValid = true;
    /// <summary>The preserved line-display value.</summary>
    public string LinesValue = "";
    /// <summary>Whether connecting lines are displayed.</summary>
    public bool HasLines;
    /// <summary>Whether the line-display value is absent or valid.</summary>
    public bool IsLinesValid = true;
    /// <summary>The preserved connect-bars value.</summary>
    public string ConnectBarsValue = "";
    /// <summary>Whether bars are connected.</summary>
    public bool ConnectBars;
    /// <summary>Whether the connect-bars value is absent or valid.</summary>
    public bool IsConnectBarsValid = true;
    /// <summary>The marker type token.</summary>
    public string SymbolType = "";
    /// <summary>The named marker token.</summary>
    public string SymbolName = "";
    /// <summary>The marker width.</summary>
    public string SymbolWidth = "";
    /// <summary>The marker height.</summary>
    public string SymbolHeight = "";
    /// <summary>The marker image reference.</summary>
    public string SymbolImageHref = "";
    /// <summary>The interpolation token.</summary>
    public string Interpolation = "";
    /// <summary>The spline order.</summary>
    public string SplineOrder = "";
    /// <summary>The spline resolution.</summary>
    public string SplineResolution = "";
    /// <summary>The three-dimensional solid token.</summary>
    public string SolidType = "";
    /// <summary>The pie-segment offset.</summary>
    public string PieOffset = "";
    /// <summary>The numeric data-label token.</summary>
    public string DataLabelNumber = "";
    /// <summary>The preserved text-label value.</summary>
    public string DataLabelTextValue = "";
    /// <summary>Whether text labels are displayed.</summary>
    public bool HasDataLabelText;
    /// <summary>Whether the text-label value is absent or valid.</summary>
    public bool IsDataLabelTextValid = true;
    /// <summary>The preserved symbol-label value.</summary>
    public string DataLabelSymbolValue = "";
    /// <summary>Whether symbol labels are displayed.</summary>
    public bool HasDataLabelSymbol;
    /// <summary>Whether the symbol-label value is absent or valid.</summary>
    public bool IsDataLabelSymbolValid = true;
    /// <summary>The error calculation category token.</summary>
    public string ErrorCategory = "";
    /// <summary>The error percentage.</summary>
    public string ErrorPercentage = "";
    /// <summary>The error margin.</summary>
    public string ErrorMargin = "";
    /// <summary>The constant lower error limit.</summary>
    public string ErrorLowerLimit = "";
    /// <summary>The constant upper error limit.</summary>
    public string ErrorUpperLimit = "";
    /// <summary>The preserved upper-error-indicator value.</summary>
    public string ErrorUpperIndicatorValue = "";
    /// <summary>Whether the upper error indicator is displayed.</summary>
    public bool HasErrorUpperIndicator;
    /// <summary>Whether the upper-error-indicator value is absent or valid.</summary>
    public bool IsErrorUpperIndicatorValid = true;
    /// <summary>The preserved lower-error-indicator value.</summary>
    public string ErrorLowerIndicatorValue = "";
    /// <summary>Whether the lower error indicator is displayed.</summary>
    public bool HasErrorLowerIndicator;
    /// <summary>Whether the lower-error-indicator value is absent or valid.</summary>
    public bool IsErrorLowerIndicatorValid = true;
    /// <summary>The lower error source-range list.</summary>
    public string ErrorLowerRange = "";
    /// <summary>The parsed lower error source ranges.</summary>
    public OdfCellRange[] ErrorLowerRanges = [];
    /// <summary>Whether the lower error source-range list is absent or valid.</summary>
    public bool IsErrorLowerRangeValid = true;
    /// <summary>The upper error source-range list.</summary>
    public string ErrorUpperRange = "";
    /// <summary>The parsed upper error source ranges.</summary>
    public OdfCellRange[] ErrorUpperRanges = [];
    /// <summary>Whether the upper error source-range list is absent or valid.</summary>
    public bool IsErrorUpperRangeValid = true;
    /// <summary>The regression algorithm token.</summary>
    public string RegressionType = "";
    /// <summary>The maximum polynomial degree.</summary>
    public string RegressionMaximumDegree = "";
    /// <summary>The preserved force-intercept value.</summary>
    public string RegressionForceInterceptValue = "";
    /// <summary>Whether the regression intercept is forced.</summary>
    public bool IsRegressionInterceptForced;
    /// <summary>Whether the force-intercept value is absent or valid.</summary>
    public bool IsRegressionForceInterceptValid = true;
    /// <summary>The forced regression intercept value.</summary>
    public string RegressionInterceptValue = "";
    /// <summary>The regression display name.</summary>
    public string RegressionName = "";
    /// <summary>The moving-average period.</summary>
    public string RegressionPeriod = "";
    /// <summary>The moving-average type token.</summary>
    public string RegressionMovingType = "";
    internal string OriginalVerticalValue = "";
    internal bool OriginalIsVertical;
    internal string OriginalStackedValue = "";
    internal bool OriginalIsStacked;
    internal string OriginalPercentageValue = "";
    internal bool OriginalIsPercentage;
    internal string OriginalThreeDimensionalValue = "";
    internal bool OriginalIsThreeDimensional;
    internal string OriginalDeepValue = "";
    internal bool OriginalIsDeep;
    internal string OriginalLinesValue = "";
    internal bool OriginalHasLines;
    internal string OriginalConnectBarsValue = "";
    internal bool OriginalConnectBars;
    internal string OriginalSymbolType = "";
    internal string OriginalSymbolName = "";
    internal string OriginalSymbolWidth = "";
    internal string OriginalSymbolHeight = "";
    internal string OriginalSymbolImageHref = "";
    internal string OriginalInterpolation = "";
    internal string OriginalSplineOrder = "";
    internal string OriginalSplineResolution = "";
    internal string OriginalSolidType = "";
    internal string OriginalPieOffset = "";
    internal string OriginalDataLabelNumber = "";
    internal string OriginalDataLabelTextValue = "";
    internal bool OriginalHasDataLabelText;
    internal string OriginalDataLabelSymbolValue = "";
    internal bool OriginalHasDataLabelSymbol;
    internal string OriginalErrorCategory = "";
    internal string OriginalErrorPercentage = "";
    internal string OriginalErrorMargin = "";
    internal string OriginalErrorLowerLimit = "";
    internal string OriginalErrorUpperLimit = "";
    internal string OriginalErrorUpperIndicatorValue = "";
    internal bool OriginalHasErrorUpperIndicator;
    internal string OriginalErrorLowerIndicatorValue = "";
    internal bool OriginalHasErrorLowerIndicator;
    internal string OriginalErrorLowerRange = "";
    internal string OriginalErrorUpperRange = "";
    internal string OriginalRegressionType = "";
    internal string OriginalRegressionMaximumDegree = "";
    internal string OriginalRegressionForceInterceptValue = "";
    internal bool OriginalIsRegressionInterceptForced;
    internal string OriginalRegressionInterceptValue = "";
    internal string OriginalRegressionName = "";
    internal string OriginalRegressionPeriod = "";
    internal string OriginalRegressionMovingType = "";
}
