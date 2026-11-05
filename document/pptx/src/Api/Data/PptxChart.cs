using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents chart data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxChart
{
    internal PptxShape Shape;
    /// <summary>
    /// Stores the relationship id used to identify or connect related presentation data.
    /// </summary>
    public string RelationshipId;
    internal PptxRelationship? Relationship;
    internal PptxPart? Part;
    /// <summary>
    /// Stores the part name associated with this object.
    /// </summary>
    public string PartName;
    /// <summary>
    /// Stores the content type associated with this object.
    /// </summary>
    public string ContentType;
    internal string Xml;

    internal PptxChart(PptxShape shape)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        if (!shape.IsChart())
        {
            throw new System.ArgumentException("Shape is not a chart.");
        }

        Shape = shape;
        RelationshipId = shape.GetChartRelationshipId();
        Relationship = null;
        Part = null;
        PartName = "";
        ContentType = "";
        Xml = "";
        Load();
    }

    internal void Load()
    {
        Relationship = Shape.Tree.FindRelationshipById(RelationshipId);
        Part = null;
        PartName = "";
        ContentType = "";
        Xml = "";
        if (Relationship == null)
        {
            return;
        }

        PartName = Relationship.TargetPartName;
        if (Shape.Tree.Slide.Presentation == null)
        {
            return;
        }

        if (!Relationship.IsInternal())
        {
            return;
        }

        PptxPackage package = Shape.Tree.Slide.Presentation.Package;
        if (!package.HasPart(PartName))
        {
            return;
        }

        Part = package.GetPart(PartName);
        ContentType = Part.ContentType;
        Xml = GetPartXml(Part);
    }
    internal bool HasPart()
    {
        if (Part != null)
        {
            return true;
        }

        return false;
    }

    internal XmlDocument GetXmlDocument()
    {
        if (Part == null)
        {
            throw new System.ArgumentException("Chart part was not found.");
        }

        if (Part.HasXmlDocument)
        {
            return Part.XmlDocument;
        }

        return Part.LoadXmlDocument();
    }
    internal List<PptxRelationship> GetRelationships()
    {
        List<PptxRelationship> relationships = new List<PptxRelationship>();
        if (Part == null)
        {
            return relationships;
        }

        for (int index = 0; index < Part.Relationships.Relationships.Count; index++)
        {
            relationships.Add(Part.Relationships.Relationships[index]);
        }

        return relationships;
    }
    internal List<PptxPart> GetRelatedParts()
    {
        List<PptxPart> parts = new List<PptxPart>();
        if (Part == null)
        {
            return parts;
        }

        if (Shape.Tree.Slide.Presentation == null)
        {
            return parts;
        }

        PptxPackage package = Shape.Tree.Slide.Presentation.Package;
        for (int index = 0; index < Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = Part.Relationships.Relationships[index];
            if (!relationship.IsInternal())
            {
                continue;
            }

            if (package.HasPart(relationship.TargetPartName))
            {
                parts.Add(package.GetPart(relationship.TargetPartName));
            }
        }

        return parts;
    }
    internal PptxChartData GetData()
    {
        return PptxChartData.FromChart(this);
    }
    internal List<List<string>> ExtractDataTable()
    {
        return GetData().ToTable();
    }
    internal string GetChartType()
    {
        XmlElement? root = GetXmlDocument().DocumentElement;
        if (root == null)
        {
            return "";
        }

        XmlElement? plotArea = PptxXml.FindFirstDescendant(root, PptxNamespaces.Chart, "plotArea");
        if (plotArea == null)
        {
            return "";
        }

        int chartElementCount = 0;
        string chartType = "";
        for (int index = 0; index < plotArea.ChildNodes.Count; index++)
        {
            XmlNode? child = plotArea.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            string currentType = ChartTypeFromElement(element);
            if (currentType.Length > 0)
            {
                chartElementCount = chartElementCount + 1;
                chartType = currentType;
            }
        }

        if (chartElementCount > 1)
        {
            return PptxChartType.Combo;
        }

        return chartType;
    }
    internal void SetChartType(string chartType)
    {
        SetDataWithChartType(chartType, GetData());
    }
    internal void SetData(PptxChartData data)
    {
        string chartType = GetChartType();
        if (chartType.Length == 0)
        {
            chartType = PptxChartType.Column;
        }

        SetDataWithChartType(chartType, data);
    }
    internal void SetDataWithChartType(string chartType, PptxChartData data)
    {
        if (Part == null)
        {
            throw new System.ArgumentException("Chart part was not found.");
        }

        XmlDocument document = PptxChartBuilder.CreateChartDocument(chartType, data);
        Part.SetXmlDocument(document);
        Xml = PptxXml.WriteString(document);
        ContentType = Part.ContentType;
    }
    internal void UpdateCaches(PptxChartData data)
    {
        string currentChartType = GetChartType();
        bool hasAxes = currentChartType != PptxChartType.Pie && currentChartType != PptxChartType.Doughnut;
        string title = GetTitle();
        string legend = GetLegendPosition();
        string categoryTitle = GetAxisTitle("category");
        string valueTitle = GetAxisTitle("value");
        bool categoryMajor = HasMajorGridlines("category");
        bool valueMajor = HasMajorGridlines("value");
        bool categoryMinor = HasMinorGridlines("category");
        bool valueMinor = HasMinorGridlines("value");
        bool markers = HasMarkers();
        bool labels = HasDataLabels();
        PptxChartAxisScale categoryScale = GetAxisScale("category");
        PptxChartAxisScale valueScale = GetAxisScale("value");
        List<PptxChartSeriesFormat> formats = new List<PptxChartSeriesFormat>();
        int existingSeriesCount = GetData().SeriesCount();
        for (int index = 0; index < existingSeriesCount; index++)
        {
            formats.Add(GetSeriesFormat(index));
        }

        SetData(data);
        if (title.Length > 0)
        {
            SetTitle(title);
        }

        if (legend.Length > 0)
        {
            SetLegendPosition(legend);
        }

        if (categoryTitle.Length > 0)
        {
            SetAxisTitle("category", categoryTitle);
        }

        if (valueTitle.Length > 0)
        {
            SetAxisTitle("value", valueTitle);
        }

        if (hasAxes)
        {
            SetAxisScale("category", categoryScale);
            SetAxisScale("value", valueScale);
            SetMajorGridlines("category", categoryMajor);
            SetMajorGridlines("value", valueMajor);
            SetMinorGridlines("category", categoryMinor);
            SetMinorGridlines("value", valueMinor);
        }
        SetMarkers(markers);
        SetDataLabels(labels);
        int formatCount = formats.Count;
        if (formatCount > data.Series.Count)
        {
            formatCount = data.Series.Count;
        }

        for (int index = 0; index < formatCount; index++)
        {
            SetSeriesFormat(index, formats[index]);
        }
    }
    internal PptxPart SetEmbeddedWorkbook(PptxChartData data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        UpdateCaches(data);
        return SetEmbeddedWorkbookBytes(PptxWorkbookBuilder.CreateWorkbookBytes(data, GetChartType()));
    }

    internal PptxPart EnsureEmbeddedWorkbook(PptxChartData data)
    {
        return SetEmbeddedWorkbook(data);
    }
    internal PptxPart SetEmbeddedWorkbookBytes(byte[] bytes)
    {
        if (Part == null)
        {
            throw new System.ArgumentException("Chart part was not found.");
        }

        if (Shape.Tree.Slide.Presentation == null)
        {
            throw new System.ArgumentException("Slide presentation is required.");
        }

        PptxPackage package = Shape.Tree.Slide.Presentation.Package;
        PptxRelationship? relationship = FindChartPartRelationship(PptxRelationshipTypes.Package);
        PptxPart workbookPart;
        string relationshipId = "";
        if (relationship != null && relationship.IsInternal() && package.HasPart(relationship.TargetPartName))
        {
            workbookPart = package.ReplacePart(relationship.TargetPartName, PptxContentTypes.Binary, bytes);
            relationshipId = relationship.Id;
        }
        else
        {
            string partName = AllocateEmbeddedWorkbookPartName(package);
            workbookPart = package.AddPart(partName, PptxContentTypes.Binary, bytes);
            string target = PptxSlideCloner.BuildRelativeTargetPath(Part.PartName, workbookPart.PartName);
            PptxRelationship added = package.AddRelationship(Part.PartName, PptxRelationshipTypes.Package, target, PptxRelationshipTargetMode.Internal);
            relationshipId = added.Id;
        }

        SetExternalDataRelationshipId(relationshipId);
        Load();
        return workbookPart;
    }
    internal PptxPart? GetEmbeddedWorkbookPart()
    {
        return GetRelatedStylePart(PptxRelationshipTypes.Package);
    }
    internal string GetExternalDataRelationshipId()
    {
        XmlElement? root = GetXmlDocument().DocumentElement;
        if (root == null)
        {
            return "";
        }

        XmlElement? externalData = PptxXml.FindFirstChild(root, PptxNamespaces.Chart, "externalData");
        if (externalData == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(externalData, PptxNamespaces.Relationships, "id");
    }
    internal void SetExternalDataRelationshipId(string relationshipId)
    {
        XmlDocument document = GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            root = document.CreateElement("c", "chartSpace", PptxNamespaces.Chart);
            document.AppendChild(root);
        }

        XmlElement externalData = GetOrCreateRootChild(root, "externalData");
        PptxXml.SetAttributeValue(externalData, "r", PptxNamespaces.Relationships, "id", PptxDiagnostic.CleanText(relationshipId));
        XmlElement autoUpdate = GetOrCreateDirectChild(externalData, "autoUpdate");
        PptxXml.SetAttributeValue(autoUpdate, "", "", "val", "0");
        SaveXmlDocument(document);
    }
    internal string GetTitle()
    {
        XmlElement? chart = GetChartElement();
        if (chart == null)
        {
            return "";
        }

        XmlElement? title = PptxXml.FindFirstChild(chart, PptxNamespaces.Chart, "title");
        return PptxChartData.ExtractChartText(title);
    }
    internal void SetTitle(string titleText)
    {
        XmlDocument document = GetXmlDocument();
        XmlElement chart = GetOrCreateChartElement(document);
        XmlElement title = GetOrCreateOrderedChartChild(chart, "title");
        RemoveAllChildren(title);
        AppendTitleText(document, title, titleText);
        SaveXmlDocument(document);
    }
    internal string GetLegendPosition()
    {
        XmlElement? chart = GetChartElement();
        if (chart == null)
        {
            return "";
        }

        XmlElement? legend = PptxXml.FindFirstChild(chart, PptxNamespaces.Chart, "legend");
        if (legend == null)
        {
            return "";
        }

        XmlElement? position = PptxXml.FindFirstChild(legend, PptxNamespaces.Chart, "legendPos");
        if (position == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(position, "", "val");
    }
    internal void SetLegendPosition(string position)
    {
        XmlDocument document = GetXmlDocument();
        XmlElement chart = GetOrCreateChartElement(document);
        XmlElement legend = GetOrCreateOrderedChartChild(chart, "legend");
        XmlElement legendPosition = GetOrCreateDirectChild(legend, "legendPos");
        PptxXml.SetAttributeValue(legendPosition, "", "", "val", NormalizeLegendPosition(position));
        SaveXmlDocument(document);
    }
    internal string GetAxisTitle(string axisName)
    {
        XmlElement? axis = GetAxis(axisName);
        if (axis == null)
        {
            return "";
        }

        XmlElement? title = PptxXml.FindFirstChild(axis, PptxNamespaces.Chart, "title");
        return PptxChartData.ExtractChartText(title);
    }
    internal void SetAxisTitle(string axisName, string titleText)
    {
        XmlDocument document = GetXmlDocument();
        XmlElement axis = GetOrCreateAxis(document, axisName);
        XmlElement title = GetOrCreateOrderedAxisChild(axis, "title");
        RemoveAllChildren(title);
        AppendTitleText(document, title, titleText);
        SaveXmlDocument(document);
    }
    internal bool HasMajorGridlines(string axisName)
    {
        XmlElement? axis = GetAxis(axisName);
        if (axis == null)
        {
            return false;
        }

        return PptxXml.FindFirstChild(axis, PptxNamespaces.Chart, "majorGridlines") != null;
    }
    internal void SetMajorGridlines(string axisName, bool enabled)
    {
        XmlDocument document = GetXmlDocument();
        XmlElement axis = GetOrCreateAxis(document, axisName);
        if (enabled)
        {
            GetOrCreateOrderedAxisChild(axis, "majorGridlines");
        }
        else
        {
            RemoveDirectChild(axis, PptxNamespaces.Chart, "majorGridlines");
        }

        SaveXmlDocument(document);
    }
    internal bool HasMinorGridlines(string axisName)
    {
        XmlElement? axis = GetAxis(axisName);
        if (axis == null)
        {
            return false;
        }

        return PptxXml.FindFirstChild(axis, PptxNamespaces.Chart, "minorGridlines") != null;
    }
    internal void SetMinorGridlines(string axisName, bool enabled)
    {
        XmlDocument document = GetXmlDocument();
        XmlElement axis = GetOrCreateAxis(document, axisName);
        if (enabled)
        {
            GetOrCreateOrderedAxisChild(axis, "minorGridlines");
        }
        else
        {
            RemoveDirectChild(axis, PptxNamespaces.Chart, "minorGridlines");
        }

        SaveXmlDocument(document);
    }
    internal PptxChartAxisScale GetAxisScale(string axisName)
    {
        PptxChartAxisScale result = new PptxChartAxisScale();
        XmlElement? axis = GetAxis(axisName);
        if (axis == null)
        {
            return result;
        }

        XmlElement? scaling = PptxXml.FindFirstChild(axis, PptxNamespaces.Chart, "scaling");
        if (scaling != null)
        {
            result.Minimum = GetValuedChild(scaling, "min");
            result.Maximum = GetValuedChild(scaling, "max");
            result.LogBase = GetValuedChild(scaling, "logBase");
            result.Reverse = GetValuedChild(scaling, "orientation") == "maxMin";
        }

        result.MajorUnit = GetValuedChild(axis, "majorUnit");
        result.MinorUnit = GetValuedChild(axis, "minorUnit");
        result.CrossAt = GetValuedChild(axis, "crossesAt");
        return result;
    }
    internal void SetAxisScale(string axisName, PptxChartAxisScale scale)
    {
        if (scale == null)
        {
            throw new System.ArgumentNullException("scale");
        }

        XmlDocument document = GetXmlDocument();
        XmlElement axis = GetOrCreateAxis(document, axisName);
        XmlElement scaling = GetOrCreateOrderedAxisChild(axis, "scaling");
        SetOptionalValuedScalingChild(scaling, "min", scale.Minimum);
        SetOptionalValuedScalingChild(scaling, "max", scale.Maximum);
        SetOptionalValuedScalingChild(scaling, "logBase", scale.LogBase);
        SetOptionalValuedAxisChild(axis, "majorUnit", scale.MajorUnit);
        SetOptionalValuedAxisChild(axis, "minorUnit", scale.MinorUnit);
        SetOptionalValuedAxisChild(axis, "crossesAt", scale.CrossAt);
        if (PptxDiagnostic.CleanText(scale.CrossAt).Length > 0)
        {
            RemoveDirectChild(axis, PptxNamespaces.Chart, "crosses");
        }
        else
        {
            XmlElement crosses = GetOrCreateOrderedAxisChild(axis, "crosses");
            PptxXml.SetAttributeValue(crosses, "", "", "val", "autoZero");
        }

        XmlElement orientation = GetOrCreateOrderedScalingChild(scaling, "orientation");
        string orientationValue = "minMax";
        if (scale.Reverse)
        {
            orientationValue = "maxMin";
        }

        PptxXml.SetAttributeValue(orientation, "", "", "val", orientationValue);
        SaveXmlDocument(document);
    }
    internal bool HasMarkers()
    {
        XmlDocument document = GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return false;
        }

        List<XmlElement> seriesElements = new List<XmlElement>();
        PptxChartData.CollectDescendants(root, PptxNamespaces.Chart, "ser", seriesElements);
        for (int index = 0; index < seriesElements.Count; index++)
        {
            if (PptxXml.FindFirstChild(seriesElements[index], PptxNamespaces.Chart, "marker") != null)
            {
                return true;
            }
        }

        return false;
    }
    internal void SetMarkers(bool enabled)
    {
        XmlDocument document = GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        List<XmlElement> seriesElements = new List<XmlElement>();
        PptxChartData.CollectDescendants(root, PptxNamespaces.Chart, "ser", seriesElements);
        for (int index = 0; index < seriesElements.Count; index++)
        {
            if (!SeriesSupportsMarker(seriesElements[index]))
            {
                continue;
            }

            if (enabled)
            {
                XmlElement marker = GetOrCreateOrderedSeriesChild(seriesElements[index], "marker");
                XmlElement symbol = GetOrCreateDirectChild(marker, "symbol");
                PptxXml.SetAttributeValue(symbol, "", "", "val", "circle");
            }
            else
            {
                RemoveDirectChild(seriesElements[index], PptxNamespaces.Chart, "marker");
            }
        }

        SaveXmlDocument(document);
    }
    internal bool HasDataLabels()
    {
        XmlElement? chartElement = GetFirstChartElement();
        if (chartElement == null)
        {
            return false;
        }

        return PptxXml.FindFirstChild(chartElement, PptxNamespaces.Chart, "dLbls") != null;
    }
    internal void SetDataLabels(bool enabled)
    {
        XmlDocument document = GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        List<XmlElement> chartElements = new List<XmlElement>();
        CollectChartElements(root, chartElements);
        for (int index = 0; index < chartElements.Count; index++)
        {
            if (enabled)
            {
                XmlElement labels = GetOrCreateOrderedChartTypeChild(chartElements[index], "dLbls");
                XmlElement showValue = GetOrCreateDirectChild(labels, "showVal");
                PptxXml.SetAttributeValue(showValue, "", "", "val", "1");
            }
            else
            {
                RemoveDirectChild(chartElements[index], PptxNamespaces.Chart, "dLbls");
            }
        }

        SaveXmlDocument(document);
    }
    internal PptxChartSeriesFormat GetSeriesFormat(int seriesIndex)
    {
        XmlElement series = GetSeriesElement(seriesIndex);
        PptxChartSeriesFormat format = new PptxChartSeriesFormat();
        XmlElement? shapeProperties = PptxXml.FindFirstChild(series, PptxNamespaces.Chart, "spPr");
        if (shapeProperties != null)
        {
            XmlElement? fill = PptxFill.FindFillElement(shapeProperties);
            if (fill != null)
            {
                format.HasFill = true;
                format.Fill = PptxFill.FromElement(fill);
            }

            XmlElement? line = PptxXml.FindFirstChild(shapeProperties, PptxNamespaces.Drawing, "ln");
            if (line != null)
            {
                format.HasLine = true;
                format.Line = PptxLine.FromElement(line);
            }
        }

        XmlElement? marker = PptxXml.FindFirstChild(series, PptxNamespaces.Chart, "marker");
        if (marker != null)
        {
            format.MarkerSymbol = GetValuedChild(marker, "symbol");
            string markerSize = GetValuedChild(marker, "size");
            if (markerSize.Length > 0)
            {
                XmlElement? size = PptxXml.FindFirstChild(marker, PptxNamespaces.Chart, "size");
                if (size != null)
                {
                    format.MarkerSize = PptxXml.GetIntAttribute(size, "", "val", 0);
                }
            }
        }

        XmlElement? smooth = PptxXml.FindFirstChild(series, PptxNamespaces.Chart, "smooth");
        if (smooth != null)
        {
            format.Smooth = PptxXml.GetBoolAttribute(smooth, "", "val", false);
        }

        XmlElement? labels = PptxXml.FindFirstChild(series, PptxNamespaces.Chart, "dLbls");
        if (labels != null)
        {
            format.LabelPosition = GetValuedChild(labels, "dLblPos");
            XmlElement? numberFormat = PptxXml.FindFirstChild(labels, PptxNamespaces.Chart, "numFmt");
            if (numberFormat != null)
            {
                format.NumberFormat = PptxXml.GetAttributeValue(numberFormat, "", "formatCode");
            }
        }

        return format;
    }
    internal void SetSeriesFormat(int seriesIndex, PptxChartSeriesFormat format)
    {
        if (format == null)
        {
            throw new System.ArgumentNullException("format");
        }

        if (format.MarkerSize < 0 || format.MarkerSize > 72)
        {
            throw new System.ArgumentOutOfRangeException("format", "Marker size must be between zero and 72 points.");
        }

        XmlDocument document = GetXmlDocument();
        XmlElement series = GetSeriesElement(seriesIndex);
        string markerSymbol = PptxDiagnostic.CleanText(format.MarkerSymbol);
        if ((markerSymbol.Length > 0 || format.MarkerSize > 0) && !SeriesSupportsMarker(series))
        {
            throw new System.ArgumentException("Markers are supported only by line, scatter, radar, and stock series.", "format");
        }

        XmlElement? shapeProperties = PptxXml.FindFirstChild(series, PptxNamespaces.Chart, "spPr");
        if (format.HasFill || format.HasLine || shapeProperties != null)
        {
            if (shapeProperties == null)
            {
                shapeProperties = GetOrCreateOrderedSeriesChild(series, "spPr");
            }

            RemoveDrawingFillChildren(shapeProperties);
            if (format.HasFill)
            {
                shapeProperties.AppendChild(format.Fill.ToElement(document));
            }

            RemoveDirectChild(shapeProperties, PptxNamespaces.Drawing, "ln");
            if (format.HasLine)
            {
                shapeProperties.AppendChild(format.Line.ToElement(document));
            }
        }

        if (markerSymbol.Length > 0 || format.MarkerSize > 0)
        {
            XmlElement marker = GetOrCreateOrderedSeriesChild(series, "marker");
            SetOptionalValuedChild(marker, "symbol", markerSymbol);
            string markerSize = "";
            if (format.MarkerSize > 0)
            {
                markerSize = format.MarkerSize.ToString();
            }

            SetOptionalValuedChild(marker, "size", markerSize);
        }
        else
        {
            RemoveDirectChild(series, PptxNamespaces.Chart, "marker");
        }

        if (format.Smooth && SeriesSupportsSmoothing(series))
        {
            XmlElement smooth = GetOrCreateOrderedSeriesChild(series, "smooth");
            PptxXml.SetAttributeValue(smooth, "", "", "val", "1");
        }
        else
        {
            RemoveDirectChild(series, PptxNamespaces.Chart, "smooth");
        }
        string labelPosition = PptxDiagnostic.CleanText(format.LabelPosition);
        string numberFormatText = PptxDiagnostic.CleanText(format.NumberFormat);
        XmlElement? labels = PptxXml.FindFirstChild(series, PptxNamespaces.Chart, "dLbls");
        if (labelPosition.Length > 0 || numberFormatText.Length > 0 || labels != null)
        {
            if (labels == null)
            {
                labels = GetOrCreateOrderedSeriesChild(series, "dLbls");
            }

            XmlElement? numberFormat = PptxXml.FindFirstChild(labels, PptxNamespaces.Chart, "numFmt");
            if (numberFormatText.Length > 0)
            {
                if (numberFormat == null)
                {
                    numberFormat = document.CreateElement("c", "numFmt", PptxNamespaces.Chart);
                    labels.AppendChild(numberFormat);
                }

                PptxXml.SetAttributeValue(numberFormat, "", "", "formatCode", numberFormatText);
                PptxXml.SetAttributeValue(numberFormat, "", "", "sourceLinked", "0");
            }
            else if (numberFormat != null)
            {
                labels.RemoveChild(numberFormat);
            }

            SetOptionalValuedChild(labels, "dLblPos", labelPosition);
            XmlElement? labelPositionElement = PptxXml.FindFirstChild(labels, PptxNamespaces.Chart, "dLblPos");
            if (numberFormat != null && labelPositionElement != null)
            {
                labels.InsertBefore(numberFormat, labelPositionElement);
            }
        }

        SaveXmlDocument(document);
    }

    internal PptxPart SetStyleXml(string xml)
    {
        return SetRelatedStylePart(PptxRelationshipTypes.ChartStyle, PptxContentTypes.ChartStyle, "style", xml);
    }

    internal PptxPart SetColorStyleXml(string xml)
    {
        return SetRelatedStylePart(PptxRelationshipTypes.ChartColorStyle, PptxContentTypes.ChartColorStyle, "colors", xml);
    }
    internal PptxPart? GetStylePart()
    {
        return GetRelatedStylePart(PptxRelationshipTypes.ChartStyle);
    }
    internal PptxPart? GetColorStylePart()
    {
        return GetRelatedStylePart(PptxRelationshipTypes.ChartColorStyle);
    }

    internal PptxPart SetRelatedStylePart(string relationshipType, string contentType, string prefix, string xml)
    {
        if (Part == null)
        {
            throw new System.ArgumentException("Chart part was not found.");
        }

        if (Shape.Tree.Slide.Presentation == null)
        {
            throw new System.ArgumentException("Slide presentation is required.");
        }

        PptxPackage package = Shape.Tree.Slide.Presentation.Package;
        PptxRelationship? relationship = FindChartPartRelationship(relationshipType);
        string clean = PptxDiagnostic.CleanText(xml);
        if (relationship != null && relationship.IsInternal() && package.HasPart(relationship.TargetPartName))
        {
            return package.ReplacePart(relationship.TargetPartName, contentType, Encoding.UTF8.GetBytes(clean));
        }

        string partName = AllocateChartSupportPartName(package, prefix);
        PptxPart stylePart = package.AddPart(partName, contentType, Encoding.UTF8.GetBytes(clean));
        string target = PptxSlideCloner.BuildRelativeTargetPath(Part.PartName, stylePart.PartName);
        package.AddRelationship(Part.PartName, relationshipType, target, PptxRelationshipTargetMode.Internal);
        Load();
        return stylePart;
    }
    internal PptxPart? GetRelatedStylePart(string relationshipType)
    {
        if (Part == null)
        {
            return null;
        }

        if (Shape.Tree.Slide.Presentation == null)
        {
            return null;
        }

        PptxPackage package = Shape.Tree.Slide.Presentation.Package;
        PptxRelationship? relationship = FindChartPartRelationship(relationshipType);
        if (relationship == null || !relationship.IsInternal())
        {
            return null;
        }

        if (!package.HasPart(relationship.TargetPartName))
        {
            return null;
        }

        return package.GetPart(relationship.TargetPartName);
    }

    internal PptxRelationship? FindChartPartRelationship(string relationshipType)
    {
        if (Part == null)
        {
            return null;
        }

        for (int index = 0; index < Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = Part.Relationships.Relationships[index];
            if (relationship.Type == relationshipType)
            {
                return relationship;
            }
        }

        return null;
    }

    internal static string GetPartXml(PptxPart part)
    {
        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        if (part.HasXmlDocument)
        {
            return PptxXml.WriteString(part.XmlDocument);
        }

        if (!part.IsXmlContentType())
        {
            return "";
        }

        return Encoding.UTF8.GetString(part.GetBytes());
    }

    internal XmlElement? GetChartElement()
    {
        XmlElement? root = GetXmlDocument().DocumentElement;
        if (root == null)
        {
            return null;
        }

        return PptxXml.FindFirstDescendant(root, PptxNamespaces.Chart, "chart");
    }

    internal XmlElement GetOrCreateChartElement(XmlDocument document)
    {
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            root = document.CreateElement("c", "chartSpace", PptxNamespaces.Chart);
            document.AppendChild(root);
        }

        XmlElement? chart = PptxXml.FindFirstDescendant(root, PptxNamespaces.Chart, "chart");
        if (chart != null)
        {
            return chart;
        }

        chart = document.CreateElement("c", "chart", PptxNamespaces.Chart);
        root.AppendChild(chart);
        return chart;
    }

    internal XmlElement? GetPlotArea()
    {
        XmlElement? root = GetXmlDocument().DocumentElement;
        if (root == null)
        {
            return null;
        }

        return PptxXml.FindFirstDescendant(root, PptxNamespaces.Chart, "plotArea");
    }

    internal XmlElement GetOrCreatePlotArea(XmlDocument document)
    {
        XmlElement chart = GetOrCreateChartElement(document);
        XmlElement? plotArea = PptxXml.FindFirstChild(chart, PptxNamespaces.Chart, "plotArea");
        if (plotArea != null)
        {
            return plotArea;
        }

        plotArea = document.CreateElement("c", "plotArea", PptxNamespaces.Chart);
        chart.AppendChild(plotArea);
        return plotArea;
    }

    internal XmlElement? GetFirstChartElement()
    {
        XmlElement? plotArea = GetPlotArea();
        if (plotArea == null)
        {
            return null;
        }

        for (int index = 0; index < plotArea.ChildNodes.Count; index++)
        {
            XmlNode? child = plotArea.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (ChartTypeFromElement(element).Length > 0)
            {
                return element;
            }
        }

        return null;
    }

    internal XmlElement? GetAxis(string axisName)
    {
        XmlElement? plotArea = GetPlotArea();
        if (plotArea == null)
        {
            return null;
        }

        return FindAxisElement(plotArea, axisName);
    }

    internal XmlElement GetOrCreateAxis(XmlDocument document, string axisName)
    {
        XmlElement plotArea = GetOrCreatePlotArea(document);
        string localName = AxisLocalName(axisName);
        XmlElement? axis = FindAxisElement(plotArea, axisName);
        if (axis != null)
        {
            return axis;
        }

        string chartType = GetChartType();
        if (localName == "catAx" && (chartType == PptxChartType.Scatter || chartType == PptxChartType.Bubble))
        {
            localName = "valAx";
        }

        axis = document.CreateElement("c", localName, PptxNamespaces.Chart);
        PptxChartBuilder.AppendValuedElement(document, axis, "axId", AxisId(axisName));
        XmlElement scaling = document.CreateElement("c", "scaling", PptxNamespaces.Chart);
        PptxChartBuilder.AppendValuedElement(document, scaling, "orientation", "minMax");
        axis.AppendChild(scaling);
        PptxChartBuilder.AppendValuedElement(document, axis, "delete", "0");
        PptxChartBuilder.AppendValuedElement(document, axis, "axPos", AxisPosition(axisName));
        PptxChartBuilder.AppendValuedElement(document, axis, "crossAx", CrossAxisId(axisName));
        plotArea.AppendChild(axis);
        return axis;
    }

    internal void SaveXmlDocument(XmlDocument document)
    {
        if (Part == null)
        {
            throw new System.ArgumentException("Chart part was not found.");
        }

        Part.SetXmlDocument(document);
        Xml = PptxXml.WriteString(document);
    }

    internal static XmlElement GetOrCreateDirectChild(XmlElement parent, string localName)
    {
        XmlElement? child = PptxXml.FindFirstChild(parent, PptxNamespaces.Chart, localName);
        if (child != null)
        {
            return child;
        }

        XmlElement created = PptxTextFrame.GetOwnerDocument(parent).CreateElement("c", localName, PptxNamespaces.Chart);
        parent.AppendChild(created);
        return created;
    }

    internal static XmlElement GetOrCreateOrderedChartChild(XmlElement parent, string localName)
    {
        XmlElement child = GetOrCreateDirectChild(parent, localName);
        MoveKnownChild(parent, child, "chart");
        return child;
    }

    internal static XmlElement GetOrCreateOrderedAxisChild(XmlElement parent, string localName)
    {
        XmlElement child = GetOrCreateDirectChild(parent, localName);
        MoveKnownChild(parent, child, "axis");
        return child;
    }

    internal static XmlElement GetOrCreateOrderedScalingChild(XmlElement parent, string localName)
    {
        XmlElement child = GetOrCreateDirectChild(parent, localName);
        MoveKnownChild(parent, child, "scaling");
        return child;
    }

    internal static XmlElement GetOrCreateOrderedSeriesChild(XmlElement parent, string localName)
    {
        XmlElement child = GetOrCreateDirectChild(parent, localName);
        MoveKnownChild(parent, child, "series");
        return child;
    }

    internal static XmlElement GetOrCreateOrderedChartTypeChild(XmlElement parent, string localName)
    {
        XmlElement child = GetOrCreateDirectChild(parent, localName);
        MoveKnownChild(parent, child, "chartType");
        return child;
    }

    internal static void SetOptionalValuedAxisChild(XmlElement parent, string localName, string value)
    {
        string clean = PptxDiagnostic.CleanText(value);
        if (clean.Length == 0)
        {
            RemoveDirectChild(parent, PptxNamespaces.Chart, localName);
            return;
        }

        XmlElement child = GetOrCreateOrderedAxisChild(parent, localName);
        PptxXml.SetAttributeValue(child, "", "", "val", clean);
    }

    internal static void SetOptionalValuedScalingChild(XmlElement parent, string localName, string value)
    {
        string clean = PptxDiagnostic.CleanText(value);
        if (clean.Length == 0)
        {
            RemoveDirectChild(parent, PptxNamespaces.Chart, localName);
            return;
        }

        XmlElement child = GetOrCreateOrderedScalingChild(parent, localName);
        PptxXml.SetAttributeValue(child, "", "", "val", clean);
    }

    internal static void MoveKnownChild(XmlElement parent, XmlElement child, string context)
    {
        int childRank = KnownChildRank(context, child.LocalName);
        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlNode? sibling = parent.ChildNodes[index];
            if (sibling == null || sibling == child || sibling.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)sibling;
            if (element.NamespaceURI == PptxNamespaces.Chart && KnownChildRank(context, element.LocalName) > childRank)
            {
                parent.InsertBefore(child, element);
                return;
            }
        }
    }

    internal static int KnownChildRank(string context, string localName)
    {
        if (context == "chart")
        {
            if (localName == "title") return 10;
            if (localName == "plotArea") return 60;
            if (localName == "legend") return 70;
            if (localName == "plotVisOnly") return 80;
            return 100;
        }

        if (context == "axis")
        {
            if (localName == "axId") return 0;
            if (localName == "scaling") return 10;
            if (localName == "delete") return 20;
            if (localName == "axPos") return 30;
            if (localName == "majorGridlines") return 40;
            if (localName == "minorGridlines") return 41;
            if (localName == "title") return 50;
            if (localName == "numFmt") return 60;
            if (localName == "majorTickMark") return 61;
            if (localName == "minorTickMark") return 62;
            if (localName == "tickLblPos") return 63;
            if (localName == "spPr") return 70;
            if (localName == "txPr") return 71;
            if (localName == "crossAx") return 80;
            if (localName == "crosses" || localName == "crossesAt") return 81;
            if (localName == "crossBetween") return 82;
            if (localName == "majorUnit") return 90;
            if (localName == "minorUnit") return 91;
            return 100;
        }

        if (context == "scaling")
        {
            if (localName == "logBase") return 10;
            if (localName == "orientation") return 20;
            if (localName == "max") return 30;
            if (localName == "min") return 40;
            return 100;
        }

        if (context == "series")
        {
            if (localName == "idx") return 0;
            if (localName == "order") return 10;
            if (localName == "tx") return 20;
            if (localName == "spPr") return 30;
            if (localName == "invertIfNegative") return 35;
            if (localName == "pictureOptions") return 36;
            if (localName == "explosion") return 37;
            if (localName == "marker") return 40;
            if (localName == "dPt") return 50;
            if (localName == "dLbls") return 60;
            if (localName == "trendline") return 70;
            if (localName == "errBars") return 71;
            if (localName == "cat" || localName == "xVal") return 80;
            if (localName == "val" || localName == "yVal") return 81;
            if (localName == "bubbleSize") return 82;
            if (localName == "smooth") return 90;
            return 100;
        }

        if (localName == "barDir" || localName == "grouping") return 0;
        if (localName == "scatterStyle" || localName == "radarStyle") return 0;
        if (localName == "varyColors" || localName == "wireframe") return 5;
        if (localName == "ser") return 10;
        if (localName == "dLbls") return 20;
        if (localName == "gapWidth") return 30;
        if (localName == "overlap") return 31;
        if (localName == "axId") return 90;
        return 100;
    }

    internal static XmlElement? FindAxisElement(XmlElement plotArea, string axisName)
    {
        bool valueAxis = AxisLocalName(axisName) == "valAx";
        XmlElement? fallback = null;
        for (int index = 0; index < plotArea.ChildNodes.Count; index++)
        {
            XmlNode? child = plotArea.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement axis = (XmlElement)child;
            if (axis.NamespaceURI != PptxNamespaces.Chart || (axis.LocalName != "catAx" && axis.LocalName != "valAx"))
            {
                continue;
            }

            if (!valueAxis && axis.LocalName == "catAx")
            {
                return axis;
            }

            if (valueAxis && axis.LocalName == "valAx" && GetValuedChild(axis, "axPos") != "b" && GetValuedChild(axis, "axPos") != "t")
            {
                return axis;
            }

            string expectedId = "100001";
            if (valueAxis)
            {
                expectedId = "100002";
            }

            if (GetValuedChild(axis, "axId") == expectedId)
            {
                fallback = axis;
            }
        }

        return fallback;
    }

    internal static bool SeriesSupportsMarker(XmlElement series)
    {
        XmlNode? parent = series.ParentNode;
        if (parent == null || parent.NodeType != XmlNodeType.Element)
        {
            return false;
        }

        string localName = ((XmlElement)parent).LocalName;
        return localName == "lineChart" || localName == "scatterChart" || localName == "radarChart" || localName == "stockChart";
    }

    internal static bool SeriesSupportsSmoothing(XmlElement series)
    {
        XmlNode? parent = series.ParentNode;
        if (parent == null || parent.NodeType != XmlNodeType.Element)
        {
            return false;
        }

        string localName = ((XmlElement)parent).LocalName;
        return localName == "lineChart" || localName == "scatterChart";
    }

    internal XmlElement GetSeriesElement(int seriesIndex)
    {
        if (seriesIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("seriesIndex");
        }

        XmlElement? root = GetXmlDocument().DocumentElement;
        List<XmlElement> series = new List<XmlElement>();
        if (root != null)
        {
            PptxChartData.CollectDescendants(root, PptxNamespaces.Chart, "ser", series);
        }

        if (seriesIndex >= series.Count)
        {
            throw new System.ArgumentOutOfRangeException("seriesIndex");
        }

        for (int index = 0; index < series.Count; index++)
        {
            if (PptxChartData.ExtractSeriesOrdinal(series[index], "order", index) == seriesIndex)
            {
                return series[index];
            }
        }

        return series[seriesIndex];
    }

    internal static string GetValuedChild(XmlElement parent, string localName)
    {
        XmlElement? child = PptxXml.FindFirstChild(parent, PptxNamespaces.Chart, localName);
        if (child == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(child, "", "val");
    }

    internal static void SetOptionalValuedChild(XmlElement parent, string localName, string value)
    {
        string clean = PptxDiagnostic.CleanText(value);
        XmlElement? child = PptxXml.FindFirstChild(parent, PptxNamespaces.Chart, localName);
        if (clean.Length == 0)
        {
            if (child != null)
            {
                parent.RemoveChild(child);
            }

            return;
        }

        if (child == null)
        {
            child = PptxTextFrame.GetOwnerDocument(parent).CreateElement("c", localName, PptxNamespaces.Chart);
            parent.AppendChild(child);
        }

        PptxXml.SetAttributeValue(child, "", "", "val", clean);
    }

    internal static void RemoveDrawingFillChildren(XmlElement parent)
    {
        for (int index = parent.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = parent.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Drawing && PptxFill.IsFillElement(element.LocalName))
            {
                parent.RemoveChild(child);
            }
        }
    }

    internal static void AppendTitleText(XmlDocument document, XmlElement title, string text)
    {
        XmlElement chartText = document.CreateElement("c", "tx", PptxNamespaces.Chart);
        XmlElement rich = document.CreateElement("c", "rich", PptxNamespaces.Chart);
        rich.AppendChild(document.CreateElement("a", "bodyPr", PptxNamespaces.Drawing));
        rich.AppendChild(document.CreateElement("a", "lstStyle", PptxNamespaces.Drawing));
        rich.AppendChild(PptxTextFrame.CreateParagraphElement(document, PptxDiagnostic.CleanText(text)));
        chartText.AppendChild(rich);
        title.AppendChild(chartText);
    }

    internal static void RemoveAllChildren(XmlElement element)
    {
        while (element.FirstChild != null)
        {
            element.RemoveChild(element.FirstChild);
        }
    }

    internal static void RemoveDirectChild(XmlElement root, string namespaceUri, string localName)
    {
        for (int index = root.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == namespaceUri && element.LocalName == localName)
            {
                root.RemoveChild(child);
            }
        }
    }

    internal static void CollectChartElements(XmlElement root, List<XmlElement> chartElements)
    {
        for (int index = 0; index < root.ChildNodes.Count; index++)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (ChartTypeFromElement(element).Length > 0)
            {
                chartElements.Add(element);
            }

            CollectChartElements(element, chartElements);
        }
    }

    internal static string NormalizeLegendPosition(string position)
    {
        string clean = PptxDiagnostic.CleanText(position).Replace("-", "").Replace("_", "").ToLowerInvariant();
        if (clean == "left" || clean == "l")
        {
            return "l";
        }

        if (clean == "right" || clean == "r")
        {
            return "r";
        }

        if (clean == "top" || clean == "t")
        {
            return "t";
        }

        if (clean == "bottom" || clean == "b")
        {
            return "b";
        }

        if (clean == "topright" || clean == "tr")
        {
            return "tr";
        }

        return "r";
    }

    internal static string AxisLocalName(string axisName)
    {
        string clean = PptxDiagnostic.CleanText(axisName).Replace("-", "").Replace("_", "").ToLowerInvariant();
        if (clean == "value" || clean == "val" || clean == "y" || clean == "yaxis")
        {
            return "valAx";
        }

        return "catAx";
    }

    internal static string AxisId(string axisName)
    {
        if (AxisLocalName(axisName) == "valAx")
        {
            return "100002";
        }

        return "100001";
    }

    internal static string CrossAxisId(string axisName)
    {
        if (AxisLocalName(axisName) == "valAx")
        {
            return "100001";
        }

        return "100002";
    }

    internal static string AxisPosition(string axisName)
    {
        if (AxisLocalName(axisName) == "valAx")
        {
            return "l";
        }

        return "b";
    }

    internal static string AllocateChartSupportPartName(PptxPackage package, string prefix)
    {
        string clean = PptxDiagnostic.CleanText(prefix);
        if (clean.Length == 0)
        {
            clean = "style";
        }

        int index = 1;
        string partName = "/ppt/charts/" + clean + index.ToString() + ".xml";
        while (package.HasPart(partName))
        {
            index = index + 1;
            partName = "/ppt/charts/" + clean + index.ToString() + ".xml";
        }

        return partName;
    }

    internal static string AllocateEmbeddedWorkbookPartName(PptxPackage package)
    {
        int index = 1;
        string partName = "/ppt/embeddings/Microsoft_Excel_Worksheet" + index.ToString() + ".xlsx";
        while (package.HasPart(partName))
        {
            index = index + 1;
            partName = "/ppt/embeddings/Microsoft_Excel_Worksheet" + index.ToString() + ".xlsx";
        }

        return partName;
    }

    internal static XmlElement GetOrCreateRootChild(XmlElement parent, string localName)
    {
        XmlElement? child = PptxXml.FindFirstChild(parent, PptxNamespaces.Chart, localName);
        if (child != null)
        {
            return child;
        }

        XmlElement created = PptxTextFrame.GetOwnerDocument(parent).CreateElement("c", localName, PptxNamespaces.Chart);
        parent.AppendChild(created);
        return created;
    }

    internal static string ChartTypeFromElement(XmlElement element)
    {
        if (element.NamespaceURI != PptxNamespaces.Chart)
        {
            return "";
        }

        if (element.LocalName == "barChart")
        {
            XmlElement? barDirection = PptxXml.FindFirstChild(element, PptxNamespaces.Chart, "barDir");
            if (barDirection != null && PptxXml.GetAttributeValue(barDirection, "", "val") == "bar")
            {
                return PptxChartType.Bar;
            }

            return PptxChartType.Column;
        }

        if (element.LocalName == "lineChart")
        {
            return PptxChartType.Line;
        }

        if (element.LocalName == "pieChart")
        {
            return PptxChartType.Pie;
        }

        if (element.LocalName == "doughnutChart")
        {
            return PptxChartType.Doughnut;
        }

        if (element.LocalName == "areaChart")
        {
            return PptxChartType.Area;
        }

        if (element.LocalName == "scatterChart")
        {
            return PptxChartType.Scatter;
        }

        if (element.LocalName == "bubbleChart")
        {
            return PptxChartType.Bubble;
        }

        if (element.LocalName == "radarChart")
        {
            return PptxChartType.Radar;
        }

        if (element.LocalName == "stockChart")
        {
            return PptxChartType.Stock;
        }

        if (element.LocalName == "surfaceChart")
        {
            return PptxChartType.Surface;
        }

        return "";
    }
}
