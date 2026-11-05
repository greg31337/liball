namespace svg;

/// <summary>Performs bounded profile-aware SVG structural validation.</summary>
public class SvgValidator : ISvgValidator
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgValidator()
    {
    }

    /// <summary>Validates one SVG document against strict SVG 2 structural defaults.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <returns>The completed or failed validation result.</returns>
    public SvgValidationResult ValidateWithDocument(SvgDocument document)
    {
        return new SvgValidator().ValidateWithDocumentAndOptions(document, new SvgValidationOptions());
    }

    /// <summary>Validates one SVG document against an explicit profile and bounds.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="options">The profile, catalogs, and traversal limits.</param>
    /// <returns>The completed or failed validation result.</returns>
    public SvgValidationResult ValidateWithDocumentAndOptions(SvgDocument? document, SvgValidationOptions? options)
    {
        SvgValidationProfile profile = options == null ? SvgValidationProfile.Svg2 : options.Profile;
        SvgValidationResult result = new SvgValidationResult(document, profile);
        if (!new SvgValidator().Valid(document, options)) return new SvgValidator().OperationFail(result, SvgValidationError.InvalidArgument, "SVG validation arguments are invalid.", document == null ? null : document.SourceSpan);
        SvgValidationWork work = new SvgValidationWork(result, options);
        new SvgValidator().ValidateDocument(work);
        if (result.Error != SvgValidationError.None) return result;
        if ((document ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).DocumentElement != null)
        {
            System.Collections.Generic.List<SvgNode> stack = new System.Collections.Generic.List<SvgNode>();
            stack.Add((document.DocumentElement ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            while (stack.Count > 0 && result.Error == SvgValidationError.None)
            {
                SvgNode node = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);
                if (!new SvgValidator().TakeNode(node, work)) break;
                bool descendants = new SvgValidator().ValidateNode(node, work);
                if (descendants)
                {
                    int childIndex = node.Children.Count - 1;
                    while (childIndex >= 0)
                    {
                        stack.Add(node.Children[childIndex]);
                        childIndex = childIndex - 1;
                    }
                }
            }
        }
        if (result.Error != SvgValidationError.None) return result;
        SvgCssValidator.Validate(work);
        if (result.Error != SvgValidationError.None) return result;
        SvgReferenceValidator.Validate(work);
        if (result.Error != SvgValidationError.None) return result;
        SvgSecurityValidator.Validate(work);
        if (result.Error != SvgValidationError.None) return result;
        SvgAccessibilityValidator.Validate(work);
        if (result.Error != SvgValidationError.None) return result;
        result.Success = true;
        result.Valid = result.ErrorCount == 0;
        return result;
    }
void ValidateDocument(SvgValidationWork work)
    {
        SvgDocument document = (work.Result.Document ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        SvgNode container = (document ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).DocumentNode;
        if (container == null || container.Kind != SvgNodeKind.Document)
        {
            new SvgValidator().Add(work, SvgValidationRule.DocumentStructure, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidSvgRoot, "The document container is missing or is not a document node.", container, null);
            return;
        }
        if (!object.ReferenceEquals(container.OwnerDocument, document)) new SvgValidator().Add(work, SvgValidationRule.DocumentStructure, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidSvgRoot, "The document container has inconsistent ownership.", container, null);
        int elements = 0;
        int doctypes = 0;
        bool sawElement = false;
        SvgNode? firstElement = null;
        int index = 0;
        while (index < container.Children.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgNode child = container.Children[index];
            if (child == null)
            {
                new SvgValidator().Add(work, SvgValidationRule.DocumentStructure, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.MalformedXml, "The document contains a null top-level node.", container, null);
                index = index + 1;
                continue;
            }
            if (!object.ReferenceEquals(child.Parent, container) || !object.ReferenceEquals(child.OwnerDocument, document)) new SvgValidator().Add(work, SvgValidationRule.DocumentStructure, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.MalformedXml, "A top-level node has inconsistent parent or document ownership.", child, null);
            if (child.Kind == SvgNodeKind.Element)
            {
                elements = elements + 1;
                sawElement = true;
                if (firstElement == null) firstElement = child;
            }
            else if (child.Kind == SvgNodeKind.Doctype)
            {
                doctypes = doctypes + 1;
                if (sawElement) new SvgValidator().Add(work, SvgValidationRule.DocumentStructure, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.MalformedXml, "A document type declaration occurs after the document element.", child, null);
            }
            else if ((child.Kind == SvgNodeKind.Text || child.Kind == SvgNodeKind.CData) && new SvgValidator().HasNonWhitespace(child.Value)) new SvgValidator().Add(work, SvgValidationRule.DocumentStructure, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.MalformedXml, "Non-whitespace text is not allowed outside the document element.", child, null);
            index = index + 1;
        }
        if (doctypes > 1) new SvgValidator().Add(work, SvgValidationRule.DocumentStructure, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.MalformedXml, "The document contains more than one document type declaration.", container, null);
        if (elements != 1) new SvgValidator().Add(work, SvgValidationRule.DocumentStructure, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidSvgRoot, "The document must contain exactly one document element.", container, null);
        if (!object.ReferenceEquals(document.DocumentElement, firstElement)) new SvgValidator().Add(work, SvgValidationRule.DocumentStructure, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidSvgRoot, "The document element field does not identify the first top-level element.", document.DocumentElement, null);
        SvgNode? root = document.DocumentElement;
        if (root == null || root.Kind != SvgNodeKind.Element) return;
        bool recognized = new SvgNodeNames().IsSvg2Element(root.ElementKind) || new SvgNodeNames().IsSvg11LegacyElement(root.ElementKind);
        if (!document.IsFragment && root.ElementKind != SvgElementKind.Svg) new SvgValidator().Add(work, SvgValidationRule.DocumentStructure, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidSvgRoot, "A complete SVG document requires an svg document element.", root, null);
        if (document.IsFragment && !recognized) new SvgValidator().Add(work, SvgValidationRule.DocumentStructure, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidSvgRoot, "An SVG fragment requires a recognized SVG root element.", root, null);
    }
bool ValidateNode(SvgNode? node, SvgValidationWork work)
    {
        if (node == null) return false;
        if (!object.ReferenceEquals(node.OwnerDocument, work.Result.Document)) new SvgValidator().Add(work, SvgValidationRule.DocumentStructure, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.MalformedXml, "A node has inconsistent document ownership.", node, null);
        if (node.Kind != SvgNodeKind.Element) return false;
        if (node.NamespaceUri != SvgNamespaces.Svg)
        {
            if (node.NamespaceUri.Length == 0)
            {
                SvgDiagnosticSeverity severity = (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Profile == SvgValidationProfile.LegacyPreservation ? SvgDiagnosticSeverity.Warning : SvgDiagnosticSeverity.Error;
                new SvgValidator().Add(work, SvgValidationRule.Namespace, severity, SvgDiagnosticCode.XmlNamespace, "An SVG element has no SVG namespace.", node, null);
            }
            else
            {
                SvgElementDefinition? parent = new SvgValidator().ParentDefinition(node, (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Elements);
                if (parent != null && parent.ContentModel.AllowsForeignElements) return false;
                SvgDiagnosticSeverity severity = work.Options.Profile == SvgValidationProfile.LegacyPreservation ? SvgDiagnosticSeverity.Warning : SvgDiagnosticSeverity.Error;
                new SvgValidator().Add(work, SvgValidationRule.Namespace, severity, SvgDiagnosticCode.XmlNamespace, "A foreign-namespace element is not allowed by its SVG parent.", node, null);
                return false;
            }
        }
        if (node.ElementKind == SvgElementKind.Unknown)
        {
            SvgDiagnosticSeverity severity = (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Profile == SvgValidationProfile.LegacyPreservation ? SvgDiagnosticSeverity.Warning : SvgDiagnosticSeverity.Error;
            new SvgValidator().Add(work, SvgValidationRule.UnknownContent, severity, SvgDiagnosticCode.UnsupportedFeature, "The SVG element '" + node.QualifiedName + "' is unknown.", node, null);
            return true;
        }
        if (new SvgNodeNames().IsSvg11LegacyElement(node.ElementKind))
        {
            new SvgValidator().ValidateLegacyElement(node, work);
            new SvgValidator().ValidateLegacyAttributes(node, work);
            return true;
        }
        SvgElementDefinition? definition = (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Elements.Find(node.ElementKind);
        if (definition == null)
        {
            new SvgValidator().Add(work, SvgValidationRule.UnknownContent, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.UnsupportedFeature, "The recognized SVG element has no validation definition.", node, null);
            return true;
        }
        if (work.Options.Profile == SvgValidationProfile.Svg11Compatibility && new SvgValidator().Svg2Only(node.ElementKind)) new SvgValidator().Add(work, SvgValidationRule.ProfileContent, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.UnsupportedFeature, "The element '" + node.LocalName + "' is not available in the SVG 1.1 compatibility profile.", node, null);
        new SvgValidator().ValidateContent(node, definition, work);
        new SvgValidator().ValidateAttributes(node, definition, work);
        new SvgValidator().ValidateValues(node, definition, work);
        return true;
    }
void ValidateContent(SvgNode node, SvgElementDefinition definition, SvgValidationWork work)
    {
        SvgElementContentModel model = definition.ContentModel;
        System.Collections.Generic.List<SvgElementKind> current = new System.Collections.Generic.List<SvgElementKind>();
        int index = 0;
        while (index < node.Children.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgNode child = node.Children[index];
            if (child == null)
            {
                new SvgValidator().Add(work, SvgValidationRule.DocumentStructure, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.MalformedXml, "An element contains a null child node.", node, null);
                index = index + 1;
                continue;
            }
            if (!object.ReferenceEquals(child.Parent, node) || !object.ReferenceEquals(child.OwnerDocument, work.Result.Document)) new SvgValidator().Add(work, SvgValidationRule.DocumentStructure, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.MalformedXml, "A child node has inconsistent parent or document ownership.", child, null);
            if ((child.Kind == SvgNodeKind.Text || child.Kind == SvgNodeKind.CData) && new SvgValidator().HasNonWhitespace(child.Value) && !model.AllowsText) new SvgValidator().Add(work, SvgValidationRule.ContentModel, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidValue, "Text content is not allowed in the '" + node.LocalName + "' element.", child, null);
            if (child.Kind == SvgNodeKind.Element && (child.NamespaceUri == SvgNamespaces.Svg || child.NamespaceUri.Length == 0) && new SvgNodeNames().IsSvg2Element(child.ElementKind))
            {
                current.Add(child.ElementKind);
                if (!model.AllowsElement(child.ElementKind)) new SvgValidator().Add(work, SvgValidationRule.ContentModel, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidValue, "The '" + child.LocalName + "' element is not allowed in '" + node.LocalName + "'.", child, null);
            }
            index = index + 1;
        }
        if (current.Count < model.MinimumElementChildren) new SvgValidator().Add(work, SvgValidationRule.ContentModel, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidValue, "The '" + node.LocalName + "' element has fewer element children than required.", node, null);
        if (model.MaximumElementChildren >= 0 && current.Count > model.MaximumElementChildren) new SvgValidator().Add(work, SvgValidationRule.ContentModel, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidValue, "The '" + node.LocalName + "' element has more element children than allowed.", node, null);
        index = 0;
        while (index < model.ChildRules.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgElementChildRule rule = model.ChildRules[index];
            int count = new SvgValidator().Count(current, rule.Kind);
            if (count < rule.Minimum) new SvgValidator().Add(work, SvgValidationRule.ContentModel, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidValue, "The '" + node.LocalName + "' element requires more '" + new SvgValidator().ElementName(rule.Kind, (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Elements) + "' children.", node, null);
            if (rule.Maximum >= 0 && count > rule.Maximum) new SvgValidator().Add(work, SvgValidationRule.ContentModel, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidValue, "The '" + node.LocalName + "' element has too many '" + new SvgValidator().ElementName(rule.Kind, (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Elements) + "' children.", node, null);
            index = index + 1;
        }
    }
void ValidateAttributes(SvgNode node, SvgElementDefinition definition, SvgValidationWork work)
    {
        int index = 0;
        while (index < node.Attributes.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute == null)
            {
                new SvgValidator().Add(work, SvgValidationRule.DocumentStructure, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.MalformedXml, "An element contains a null attribute.", node, null);
                index = index + 1;
                continue;
            }
            if (!object.ReferenceEquals(attribute.OwnerElement, node) || !object.ReferenceEquals(attribute.OwnerDocument, work.Result.Document)) new SvgValidator().Add(work, SvgValidationRule.DocumentStructure, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.MalformedXml, "An attribute has inconsistent element or document ownership.", node, attribute);
            if (attribute.AttributeKind == SvgAttributeKind.NamespaceDeclaration)
            {
                index = index + 1;
                continue;
            }
            SvgLegacyFeatureDefinition? legacy = (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Legacy.FindAttribute(attribute.NamespaceUri, attribute.LocalName);
            if (legacy != null && work.Options.Profile != SvgValidationProfile.Svg11Compatibility) new SvgValidator().AddLegacy(work, legacy, node, attribute);
            SvgElementAttributeDefinition? applicable = definition.FindAttribute(attribute.NamespaceUri, attribute.LocalName);
            if (applicable == null)
            {
                bool foreign = attribute.NamespaceUri.Length != 0 && attribute.NamespaceUri != SvgNamespaces.Svg && attribute.NamespaceUri != SvgNamespaces.Xml && attribute.NamespaceUri != SvgNamespaces.XLink;
                if (foreign)
                {
                    index = index + 1;
                    continue;
                }
                if (legacy == null)
                {
                    SvgDiagnosticSeverity severity = work.Options.Profile == SvgValidationProfile.LegacyPreservation ? SvgDiagnosticSeverity.Warning : SvgDiagnosticSeverity.Error;
                    SvgValidationRule rule = attribute.AttributeKind == SvgAttributeKind.Unknown ? SvgValidationRule.UnknownContent : SvgValidationRule.AttributeApplicability;
                    string message = attribute.AttributeKind == SvgAttributeKind.Unknown ? "The attribute '" + attribute.QualifiedName + "' is unknown." : "The attribute '" + attribute.QualifiedName + "' is not applicable to '" + node.LocalName + "'.";
                    new SvgValidator().Add(work, rule, severity, attribute.NamespaceUri.Length == 0 ? SvgDiagnosticCode.InvalidValue : SvgDiagnosticCode.XmlNamespace, message, node, attribute);
                }
            }
            if (work.Options.Profile == SvgValidationProfile.Svg11Compatibility && attribute.NamespaceUri.Length == 0 && attribute.LocalName == SvgAttributeNames.Href && definition.FindAttribute(SvgNamespaces.XLink, SvgAttributeNames.Href) != null) new SvgValidator().Add(work, SvgValidationRule.ProfileContent, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.CompatibilityRecovery, "SVG 1.1 requires xlink:href instead of the unqualified href attribute on this element.", node, attribute);
            index = index + 1;
        }
        new SvgValidator().ValidateRequired(node, definition, work);
    }
void ValidateRequired(SvgNode node, SvgElementDefinition definition, SvgValidationWork work)
    {
        int index = 0;
        while (index < definition.Attributes.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgElementAttributeDefinition required = definition.Attributes[index];
            if (required.Required && !new SvgValidator().HasAttribute(node, required.NamespaceUri, required.LocalName))
            {
                bool alternate = required.NamespaceUri.Length == 0 && required.LocalName == SvgAttributeNames.Href && new SvgValidator().HasAttribute(node, SvgNamespaces.XLink, SvgAttributeNames.Href);
                if (!alternate) new SvgValidator().Add(work, SvgValidationRule.RequiredAttribute, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidValue, "The '" + node.LocalName + "' element requires the '" + required.LocalName + "' attribute.", node, null);
            }
            index = index + 1;
        }
    }
void ValidateValues(SvgNode node, SvgElementDefinition definition, SvgValidationWork work)
    {
        int index = 0;
        while (index < node.Attributes.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute != null && attribute.AttributeKind != SvgAttributeKind.NamespaceDeclaration && definition.FindAttribute(attribute.NamespaceUri, attribute.LocalName) != null)
            {
                SvgTypedAttributeValue value = work.TypedValues.GetNamespaced(node, attribute.NamespaceUri, attribute.LocalName);
                if (!value.Success) new SvgValidator().AddTypedFailure(node, attribute, value, definition, work);
                else
                {
                    new SvgValidator().ValidateRange(node, attribute, value, definition, work);
                    new SvgValidator().ValidateList(node, attribute, value, definition, work);
                }
                new SvgValidator().ValidateTiming(node, attribute, definition, work);
            }
            index = index + 1;
        }
    }
void AddTypedFailure(SvgNode node, SvgAttribute attribute, SvgTypedAttributeValue value, SvgElementDefinition definition, SvgValidationWork work)
    {
        SvgValidationRule rule = SvgValidationRule.TypedValue;
        if (value.ValueKind == SvgTypedAttributeKind.PathData) rule = SvgValidationRule.PathData;
        else if (new SvgValidator().IsListKind(value.ValueKind)) rule = SvgValidationRule.ListValue;
        SvgSourceSpan? source = value.Diagnostics.Count == 0 ? attribute.SourceSpan : value.Diagnostics[0].Source;
        string message = value.Diagnostics.Count == 0 ? "The attribute '" + attribute.LocalName + "' has an invalid typed value." : value.Diagnostics[0].Message;
        string recovery = new SvgValidator().Recovery(definition.FindAttribute(attribute.NamespaceUri, attribute.LocalName), rule);
        new SvgValidator().AddLocated(work, rule, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidValue, message, node, attribute, source, recovery);
    }
bool IsListKind(SvgTypedAttributeKind kind)
    {
        return kind == SvgTypedAttributeKind.LengthList || kind == SvgTypedAttributeKind.AngleList || kind == SvgTypedAttributeKind.NumberList || kind == SvgTypedAttributeKind.IntegerList || kind == SvgTypedAttributeKind.Points || kind == SvgTypedAttributeKind.TransformList || kind == SvgTypedAttributeKind.ViewBox;
    }
void ValidateRange(SvgNode node, SvgAttribute attribute, SvgTypedAttributeValue value, SvgElementDefinition definition, SvgValidationWork work)
    {
        if (value.ValueKind == SvgTypedAttributeKind.Length && value.Value != null)
        {
            SvgLength length = (SvgLength)value.Value;
            if (new SvgValidator().RequiresNonnegativeLength(node.ElementKind, attribute.LocalName) && length.Value < 0) new SvgValidator().AddRange(node, attribute, definition, "The attribute '" + attribute.LocalName + "' requires a nonnegative length.", work);
            else if (node.ElementKind == SvgElementKind.Stop && attribute.LocalName == "offset" && !new SvgValidator().UnitInterval(length)) new SvgValidator().AddRange(node, attribute, definition, "A stop offset must be between zero and one, or between zero and one hundred percent.", work);
        }
        else if (value.ValueKind == SvgTypedAttributeKind.Number && value.Value != null)
        {
            double number = (double)value.Value;
            if (new SvgValidator().IsUnitInterval(attribute.LocalName) && (number < 0 || number > 1)) new SvgValidator().AddRange(node, attribute, definition, "The attribute '" + attribute.LocalName + "' must be between zero and one.", work);
            else if (attribute.LocalName == "stroke-miterlimit" && number < 1) new SvgValidator().AddRange(node, attribute, definition, "The stroke-miterlimit value must be at least one.", work);
            else if (attribute.LocalName == "divisor" && number == 0) new SvgValidator().AddRange(node, attribute, definition, "The divisor value must not be zero.", work);
            else if ((attribute.LocalName == "diffuseConstant" || attribute.LocalName == "specularConstant") && number < 0) new SvgValidator().AddRange(node, attribute, definition, "The lighting constant must be nonnegative.", work);
            else if (node.ElementKind == SvgElementKind.FeSpecularLighting && attribute.LocalName == "specularExponent" && (number < 1 || number > 128)) new SvgValidator().AddRange(node, attribute, definition, "The specularExponent value must be between one and 128.", work);
        }
        else if (value.ValueKind == SvgTypedAttributeKind.Integer && value.Value != null)
        {
            int number = (int)value.Value;
            if (attribute.LocalName == "numOctaves" && number < 1) new SvgValidator().AddRange(node, attribute, definition, "The numOctaves value must be at least one.", work);
            else if ((attribute.LocalName == "targetX" || attribute.LocalName == "targetY") && number < 0) new SvgValidator().AddRange(node, attribute, definition, "A convolution target index must be nonnegative.", work);
            else if (attribute.LocalName == "targetX" || attribute.LocalName == "targetY") new SvgValidator().ValidateConvolutionTarget(node, attribute, number, definition, work);
        }
        else if (value.ValueKind == SvgTypedAttributeKind.PathLength && value.Value != null && ((SvgPathLength)value.Value).Value <= 0) new SvgValidator().AddRange(node, attribute, definition, "The pathLength value must be positive.", work);
    }
void ValidateList(SvgNode node, SvgAttribute attribute, SvgTypedAttributeValue value, SvgElementDefinition definition, SvgValidationWork work)
    {
        if (value.ValueKind == SvgTypedAttributeKind.NumberList && value.Value != null)
        {
            double[] numbers = (double[])value.Value;
            if (new SvgValidator().OneOrTwo(attribute.LocalName) && (numbers.Length < 1 || numbers.Length > 2)) new SvgValidator().AddList(node, attribute, definition, "The attribute '" + attribute.LocalName + "' requires one or two numbers.", work);
            else
            {
                int index = 0;
                while (index < numbers.Length)
                {
                    bool invalid = (attribute.LocalName == "baseFrequency" || attribute.LocalName == "stdDeviation" || attribute.LocalName == "radius") && numbers[index] < 0;
                    if (attribute.LocalName == "kernelUnitLength" && numbers[index] <= 0) invalid = true;
                    if (invalid)
                    {
                        new SvgValidator().AddLocated(work, SvgValidationRule.ValueRange, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidValue, "The '" + attribute.LocalName + "' list contains a value outside its permitted range.", node, attribute, new SvgValidator().LocateValue(attribute, new SvgValidator().ListItemOffset(attribute.Value, index)), new SvgValidator().Recovery(definition.FindAttribute(attribute.NamespaceUri, attribute.LocalName), SvgValidationRule.ValueRange));
                        break;
                    }
                    index = index + 1;
                }
            }
            if (attribute.LocalName == "kernelMatrix") new SvgValidator().ValidateKernelMatrix(node, attribute, numbers.Length, definition, work);
        }
        else if (value.ValueKind == SvgTypedAttributeKind.IntegerList && value.Value != null)
        {
            int[] numbers = (int[])value.Value;
            if (attribute.LocalName == "order")
            {
                if (numbers.Length < 1 || numbers.Length > 2) new SvgValidator().AddList(node, attribute, definition, "The order attribute requires one or two integers.", work);
                else if (numbers[0] < 1 || numbers.Length == 2 && numbers[1] < 1) new SvgValidator().AddLocated(work, SvgValidationRule.ValueRange, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidValue, "Every order dimension must be positive.", node, attribute, new SvgValidator().LocateValue(attribute, new SvgValidator().ListItemOffset(attribute.Value, numbers[0] < 1 ? 0 : 1)), new SvgValidator().Recovery(definition.FindAttribute(attribute.NamespaceUri, attribute.LocalName), SvgValidationRule.ValueRange));
            }
        }
    }
void ValidateKernelMatrix(SvgNode node, SvgAttribute attribute, int count, SvgElementDefinition definition, SvgValidationWork work)
    {
        SvgTypedAttributeValue typed = work.TypedValues.Get(node, "order");
        if (!typed.Success || typed.ValueKind != SvgTypedAttributeKind.IntegerList || typed.Value == null) return;
        int[] dimensions = (int[])typed.Value;
        if (dimensions.Length < 1 || dimensions.Length > 2 || dimensions[0] < 1 || dimensions.Length == 2 && dimensions[1] < 1) return;
        long expected = (long)dimensions[0] * (dimensions.Length == 1 ? dimensions[0] : dimensions[1]);
        if (count != expected) new SvgValidator().AddList(node, attribute, definition, "The kernelMatrix item count must equal the product of the order dimensions.", work);
    }
void ValidateConvolutionTarget(SvgNode node, SvgAttribute attribute, int value, SvgElementDefinition definition, SvgValidationWork work)
    {
        if (node.ElementKind != SvgElementKind.FeConvolveMatrix) return;
        SvgTypedAttributeValue typed = work.TypedValues.Get(node, "order");
        if (!typed.Success || typed.ValueKind != SvgTypedAttributeKind.IntegerList || typed.Value == null) return;
        int[] dimensions = (int[])typed.Value;
        if (dimensions.Length < 1 || dimensions.Length > 2 || dimensions[0] < 1 || dimensions.Length == 2 && dimensions[1] < 1) return;
        int maximum = attribute.LocalName == "targetX" || dimensions.Length == 1 ? dimensions[0] : dimensions[1];
        if (value >= maximum) new SvgValidator().AddRange(node, attribute, definition, "A convolution target index must be smaller than its order dimension.", work);
    }
void ValidateTiming(SvgNode node, SvgAttribute attribute, SvgElementDefinition definition, SvgValidationWork work)
    {
        if (!new SvgValidator().IsAnimation(node.ElementKind) || attribute.NamespaceUri.Length != 0) return;
        string name = attribute.LocalName;
        if (name == "begin" || name == "end")
        {
            SvgTimingListParseResult parsed = name == "begin" ? new SvgTimingParser().ParseBeginList(attribute.Value, (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaximumTimingItems, work.Options.TypedValues.MaxTokenCharacters) : new SvgTimingParser().ParseEndList(attribute.Value, (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaximumTimingItems, work.Options.TypedValues.MaxTokenCharacters);
            if (!parsed.Success) new SvgValidator().AddTiming(node, attribute, definition, parsed.ErrorOffset, "The animation " + name + " list is invalid.", work);
            return;
        }
        if (name == "dur" || name == "min" || name == "max" || name == "repeatDur")
        {
            string text = attribute.Value.Trim();
            bool indefinite = text == "indefinite" && name != "min";
            SvgClockParseResult? parsed = indefinite ? null : new SvgTimingParser().ParseClock(attribute.Value, (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TypedValues.MaxTokenCharacters);
            if (!indefinite && (!(parsed ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Success || (parsed.Clock ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Seconds < 0)) new SvgValidator().AddTiming(node, attribute, definition, parsed == null ? 0 : parsed.ErrorOffset < 0 ? new SvgValidator().FirstValueOffset(attribute.Value) : parsed.ErrorOffset, "The animation " + name + " value must be a nonnegative clock" + (name == "min" ? "." : " or indefinite."), work);
            return;
        }
        if (name == "repeatCount")
        {
            string text = attribute.Value.Trim();
            if (text == "indefinite") return;
            SvgNumberParseResult parsed = SvgNumberParser.ParseNumber(text, (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TypedValues.MaxTokenCharacters);
            if (!parsed.Success || parsed.Value <= 0) new SvgValidator().AddTiming(node, attribute, definition, parsed.ErrorOffset < 0 ? new SvgValidator().FirstValueOffset(attribute.Value) : new SvgValidator().FirstValueOffset(attribute.Value) + parsed.ErrorOffset, "The repeatCount value must be positive or indefinite.", work);
            return;
        }
        new SvgValidator().ValidateAnimationControl(node, attribute, definition, work);
    }
void ValidateAnimationControl(SvgNode node, SvgAttribute attribute, SvgElementDefinition definition, SvgValidationWork work)
    {
        SvgAnimationKeywordParseResult? keyword = null;
        if (attribute.LocalName == "restart") keyword = new SvgAnimationKeywordParser().ParseRestart(attribute.Value);
        else if (attribute.LocalName == "fill") keyword = new SvgAnimationKeywordParser().ParseFill(attribute.Value);
        else if (attribute.LocalName == "calcMode") keyword = new SvgAnimationKeywordParser().ParseCalcMode(attribute.Value);
        else if (attribute.LocalName == "additive") keyword = new SvgAnimationKeywordParser().ParseAdditive(attribute.Value);
        else if (attribute.LocalName == "accumulate") keyword = new SvgAnimationKeywordParser().ParseAccumulate(attribute.Value);
        else if (attribute.LocalName == "attributeType") keyword = new SvgAnimationKeywordParser().ParseAttributeType(attribute.Value);
        if (keyword != null)
        {
            if (!keyword.Success) new SvgValidator().AddTiming(node, attribute, definition, keyword.ErrorOffset, "The animation control attribute '" + attribute.LocalName + "' has an invalid keyword.", work);
            return;
        }
        if (attribute.LocalName == "keyTimes" || attribute.LocalName == "keyPoints")
        {
            SvgKeyValueListParseResult parsed = attribute.LocalName == "keyTimes" ? new SvgAnimationValueParser().ParseKeyTimes(attribute.Value, (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaximumTimingItems, work.Options.TypedValues.MaxTokenCharacters) : new SvgAnimationValueParser().ParseKeyPoints(attribute.Value, (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaximumTimingItems, work.Options.TypedValues.MaxTokenCharacters);
            if (!parsed.Success) new SvgValidator().AddTiming(node, attribute, definition, parsed.ErrorOffset, "The animation " + attribute.LocalName + " list is invalid.", work);
            else if (attribute.LocalName == "keyPoints" && node.ElementKind != SvgElementKind.AnimateMotion) new SvgValidator().AddTiming(node, attribute, definition, new SvgValidator().FirstValueOffset(attribute.Value), "keyPoints applies only to animateMotion.", work);
            return;
        }
        if (attribute.LocalName == "keySplines")
        {
            SvgKeySplineListParseResult parsed = new SvgAnimationValueParser().ParseKeySplines(attribute.Value, (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaximumTimingItems, work.Options.TypedValues.MaxTokenCharacters);
            if (!parsed.Success) new SvgValidator().AddTiming(node, attribute, definition, parsed.ErrorOffset, "The animation keySplines list is invalid.", work);
        }
    }
void AddRange(SvgNode node, SvgAttribute attribute, SvgElementDefinition definition, string message, SvgValidationWork work)
    {
        new SvgValidator().AddLocated(work, SvgValidationRule.ValueRange, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidValue, message, node, attribute, new SvgValidator().LocateValue(attribute, new SvgValidator().FirstValueOffset(attribute.Value)), new SvgValidator().Recovery(definition.FindAttribute(attribute.NamespaceUri, attribute.LocalName), SvgValidationRule.ValueRange));
    }
void AddList(SvgNode node, SvgAttribute attribute, SvgElementDefinition definition, string message, SvgValidationWork work)
    {
        new SvgValidator().AddLocated(work, SvgValidationRule.ListValue, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidValue, message, node, attribute, attribute.SourceSpan, new SvgValidator().Recovery(definition.FindAttribute(attribute.NamespaceUri, attribute.LocalName), SvgValidationRule.ListValue));
    }
void AddTiming(SvgNode node, SvgAttribute attribute, SvgElementDefinition definition, int offset, string message, SvgValidationWork work)
    {
        new SvgValidator().AddLocated(work, SvgValidationRule.TimingValue, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidValue, message, node, attribute, new SvgValidator().LocateValue(attribute, offset < 0 ? new SvgValidator().FirstValueOffset(attribute.Value) : offset), new SvgValidator().Recovery(definition.FindAttribute(attribute.NamespaceUri, attribute.LocalName), SvgValidationRule.TimingValue));
    }
string Recovery(SvgElementAttributeDefinition? definition, SvgValidationRule rule)
    {
        if (rule == SvgValidationRule.PathData) return "Ignore invalid path segments and retain successfully parsed segments.";
        if (definition != null && definition.DefaultKind == SvgAttributeDefaultKind.Literal) return "Ignore the authored value and use the catalog default '" + definition.DefaultValue + "'.";
        if (rule == SvgValidationRule.TimingValue) return "Ignore the invalid animation control value and use its specified default behavior.";
        return "Ignore the invalid authored attribute value.";
    }
bool RequiresNonnegativeLength(SvgElementKind kind, string name)
    {
        if (name == "width" || name == "height" || name == "r" || name == "rx" || name == "ry" || name == "fr" || name == "markerWidth" || name == "markerHeight" || name == "textLength" || name == "stroke-width" || name == "font-size") return true;
        return kind == SvgElementKind.FeImage && (name == "width" || name == "height");
    }
bool IsUnitInterval(string name)
    {
        return name == "opacity" || name == "fill-opacity" || name == "stroke-opacity" || name == "flood-opacity" || name == "stop-opacity";
    }
bool UnitInterval(SvgLength value)
    {
        if (value.Unit == SvgLengthUnit.Percentage) return value.Value >= 0 && value.Value <= 100;
        return value.Value >= 0 && value.Value <= 1;
    }
bool OneOrTwo(string name)
    {
        return name == "baseFrequency" || name == "stdDeviation" || name == "kernelUnitLength" || name == "radius";
    }
bool IsAnimation(SvgElementKind kind)
    {
        return kind == SvgElementKind.Animate || kind == SvgElementKind.AnimateColor || kind == SvgElementKind.AnimateMotion || kind == SvgElementKind.AnimateTransform || kind == SvgElementKind.Set || kind == SvgElementKind.Discard;
    }
SvgAttribute? FindAttribute(SvgNode node, string localName)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute != null && attribute.NamespaceUri.Length == 0 && attribute.LocalName == localName) return attribute;
            index = index + 1;
        }
        return null;
    }
int FirstValueOffset(string value)
    {
        int index = 0;
        while (index < value.Length && SvgSpanCharacters.IsSvgWhitespace(value[index])) index = index + 1;
        return index;
    }
int ListItemOffset(string value, int requested)
    {
        int item = 0;
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length && (SvgSpanCharacters.IsSvgWhitespace(value[index]) || value[index] == ',')) index = index + 1;
            if (item == requested || index == value.Length) return index;
            while (index < value.Length && !SvgSpanCharacters.IsSvgWhitespace(value[index]) && value[index] != ',') index = index + 1;
            item = item + 1;
        }
        return value.Length;
    }

    internal SvgSourceSpan? LocateValue(SvgAttribute? attribute, int errorOffset)
    {
        if (attribute == null || attribute.SourceSpan == null) return null;
        SvgXmlAttributeToken? token = attribute.RawToken;
        if (token == null || token.Value == null || token.Value != attribute.Value || errorOffset < 0) return attribute.SourceSpan;
        int quote = token.RawText.IndexOf(token.Quote);
        if (quote < 0) return attribute.SourceSpan;
        int bounded = errorOffset;
        if (bounded > token.Value.Length) bounded = token.Value.Length;
        int relative = quote + 1 + bounded;
        int line = token.StartLine;
        int column = token.StartColumn;
        int index = 0;
        while (index < relative && index < token.RawText.Length)
        {
            if (token.RawText[index] == '\n')
            {
                line = line + 1;
                column = 1;
            }
            else column = column + 1;
            index = index + 1;
        }
        int endLine = line;
        int endColumn = column;
        int width = bounded < token.Value.Length ? 1 : 0;
        if (width == 1 && relative < token.RawText.Length && token.RawText[relative] == '\n')
        {
            endLine = endLine + 1;
            endColumn = 1;
        }
        else endColumn = endColumn + width;
        long start = token.StartOffset + relative;
        return new SvgSourceSpan(attribute.SourceSpan.Source, start, line, column, start + width, endLine, endColumn);
    }
void ValidateLegacyElement(SvgNode node, SvgValidationWork work)
    {
        if ((work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Profile == SvgValidationProfile.Svg11Compatibility) return;
        SvgLegacyFeatureDefinition? definition = work.Options.Legacy.FindElement(node.ElementKind);
        if (definition != null) new SvgValidator().AddLegacy(work, definition, node, null);
    }
void ValidateLegacyAttributes(SvgNode node, SvgValidationWork work)
    {
        if ((work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Profile == SvgValidationProfile.Svg11Compatibility) return;
        int index = 0;
        while (index < node.Attributes.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgAttribute attribute = node.Attributes[index];
            SvgLegacyFeatureDefinition? definition = attribute == null ? null : work.Options.Legacy.FindAttribute(attribute.NamespaceUri, attribute.LocalName);
            if (definition != null) new SvgValidator().AddLegacy(work, definition, node, attribute);
            index = index + 1;
        }
    }
void AddLegacy(SvgValidationWork work, SvgLegacyFeatureDefinition definition, SvgNode node, SvgAttribute? attribute)
    {
        SvgValidationRule rule = definition.Handling == SvgLegacyHandling.Deprecated ? SvgValidationRule.DeprecatedContent : SvgValidationRule.LegacyContent;
        SvgDiagnosticSeverity severity;
        if ((work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Profile == SvgValidationProfile.LegacyPreservation) severity = SvgDiagnosticSeverity.Info;
        else severity = definition.Handling == SvgLegacyHandling.Implemented ? SvgDiagnosticSeverity.Info : SvgDiagnosticSeverity.Warning;
        string feature = definition.FeatureType == SvgLegacyFeatureType.Element ? "element" : "attribute";
        string message = "The legacy SVG 1.1 " + feature + " '" + definition.LocalName + "' is " + new SvgValidator().LegacyHandling(definition.Handling) + ".";
        if (definition.Replacement.Length != 0) message = message + " Use '" + definition.Replacement + "' instead.";
        SvgDiagnosticCode code = definition.Handling == SvgLegacyHandling.Unsupported ? SvgDiagnosticCode.UnsupportedFeature : definition.Handling == SvgLegacyHandling.PreserveOnly ? SvgDiagnosticCode.PreservedFeature : SvgDiagnosticCode.CompatibilityRecovery;
        new SvgValidator().Add(work, rule, severity, code, message + " " + definition.Detail, node, attribute);
    }
string LegacyHandling(SvgLegacyHandling handling)
    {
        if (handling == SvgLegacyHandling.Implemented) return "implemented for compatibility";
        if (handling == SvgLegacyHandling.PreserveOnly) return "preserved without semantic processing";
        if (handling == SvgLegacyHandling.Deprecated) return "deprecated";
        return "unsupported and preserved";
    }
bool TakeNode(SvgNode? node, SvgValidationWork work)
    {
        if (work.Result.VisitedNodeCount >= (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaximumNodes)
        {
            new SvgValidator().OperationFail(work.Result, SvgValidationError.LimitExceeded, "The SVG validation node limit was exceeded.", node == null ? null : node.SourceSpan);
            return false;
        }
        work.Result.VisitedNodeCount = work.Result.VisitedNodeCount + 1;
        return true;
    }
void Add(SvgValidationWork work, SvgValidationRule rule, SvgDiagnosticSeverity severity, SvgDiagnosticCode code, string message, SvgNode? node, SvgAttribute? attribute)
    {
        SvgSourceSpan? source = attribute != null && attribute.SourceSpan != null ? attribute.SourceSpan : node == null ? null : node.SourceSpan;
        new SvgValidator().AddLocated(work, rule, severity, code, message, node, attribute, source, "");
    }

    internal void AddLocated(SvgValidationWork work, SvgValidationRule rule, SvgDiagnosticSeverity severity, SvgDiagnosticCode code, string message, SvgNode? node, SvgAttribute? attribute, SvgSourceSpan? source, string recovery)
    {
        if (work.Result.Error != SvgValidationError.None) return;
        if (work.Result.Issues.Count >= (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaximumIssues)
        {
            new SvgValidator().OperationFail(work.Result, SvgValidationError.LimitExceeded, "The SVG validation issue limit was exceeded.", node == null ? null : node.SourceSpan);
            return;
        }
        SvgDiagnostic diagnostic = new SvgDiagnostic(severity, code, message, source);
        SvgValidationIssue issue = new SvgValidationIssue(rule, node, attribute, work.Options.Profile, diagnostic);
        issue.RecoveryDescription = recovery;
        work.Result.Issues.Add(issue);
        work.Result.Diagnostics.Add(diagnostic);
        if (severity == SvgDiagnosticSeverity.Info) work.Result.InformationCount = work.Result.InformationCount + 1;
        else if (severity == SvgDiagnosticSeverity.Warning) work.Result.WarningCount = work.Result.WarningCount + 1;
        else work.Result.ErrorCount = work.Result.ErrorCount + 1;
    }
SvgValidationResult OperationFail(SvgValidationResult result, SvgValidationError error, string message, SvgSourceSpan? source)
    {
        result.Error = error;
        result.Success = false;
        result.Valid = false;
        SvgDiagnosticCode code = error == SvgValidationError.LimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidArgument;
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
        return result;
    }
SvgElementDefinition? ParentDefinition(SvgNode? node, SvgElementCatalog catalog)
    {
        if (node == null || node.Parent == null || node.Parent.Kind != SvgNodeKind.Element) return null;
        return catalog.Find(node.Parent.ElementKind);
    }
bool HasAttribute(SvgNode node, string namespaceUri, string localName)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute != null && attribute.NamespaceUri == namespaceUri && attribute.LocalName == localName) return true;
            index = index + 1;
        }
        return false;
    }
int Count(System.Collections.Generic.List<SvgElementKind> values, SvgElementKind kind)
    {
        int result = 0;
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == kind) result = result + 1;
            index = index + 1;
        }
        return result;
    }
string ElementName(SvgElementKind kind, SvgElementCatalog catalog)
    {
        SvgElementDefinition? definition = catalog.Find(kind);
        return definition == null ? kind.ToString() : definition.LocalName;
    }
bool HasNonWhitespace(string? value)
    {
        if (value == null) return false;
        int index = 0;
        while (index < value.Length)
        {
            if (!System.Char.IsWhiteSpace(value[index])) return true;
            index = index + 1;
        }
        return false;
    }
bool Svg2Only(SvgElementKind kind)
    {
        return kind == SvgElementKind.Audio || kind == SvgElementKind.Canvas || kind == SvgElementKind.Discard || kind == SvgElementKind.FeDropShadow || kind == SvgElementKind.Iframe || kind == SvgElementKind.Video;
    }
bool Valid(SvgDocument? document, SvgValidationOptions? options)
    {
        if (document == null || options == null || options.Elements == null || options.Legacy == null || options.TypedValues == null || options.TypedValues.GeometryLimits == null || options.Css == null || options.References == null || options.ReferenceLimits == null || options.Resources == null || options.ResourcePolicy == null || options.Accessibility == null) return false;
        if (options.MaximumNodes < 1 || options.MaximumIssues < 1 || options.MaximumTimingItems < 1 || options.MaximumAuthoredDeclarations < 1) return false;
        if (options.TypedValues.MaxTokenCharacters < 1 || options.TypedValues.MaxListItems < 1 || options.TypedValues.MaxTransforms < 1) return false;
        if (options.TypedValues.GeometryLimits.MaxPathSegments < 1 || options.TypedValues.GeometryLimits.MaxPoints < 1) return false;
        if (!new SvgValidator().ValidCss(options.Css)) return false;
        if (options.References.MaxUriCharacters < 1 || options.References.MaxBaseDepth < 1) return false;
        if (options.ReferenceLimits.MaxDepth < 1 || options.ReferenceLimits.MaxCount < 1 || options.ReferenceLimits.MaxUseInstances < 1 || options.ReferenceLimits.MaxPaintServerDepth < 1 || options.ReferenceLimits.MaxEffectDepth < 1 || options.ReferenceLimits.MaxConditionalChecks < 1) return false;
        int resourceIndex = 0;
        while (resourceIndex < options.Resources.Count)
        {
            SvgResourceAcquisitionResult resource = options.Resources[resourceIndex];
            if (resource == null || resource.DeclaredMediaType == null || resource.ActualMediaType == null) return false;
            resourceIndex = resourceIndex + 1;
        }
        if (options.ResourcePolicy.FileRootUri == null || options.ResourcePolicy.AllowedSchemes == null || options.ResourcePolicy.AllowedMediaTypes == null) return false;
        if (options.Accessibility.MaximumNodes < 1 || options.Accessibility.MaximumTextCharacters < 1 || options.Accessibility.MaximumReferences < 1) return false;
        return options.Profile >= SvgValidationProfile.Svg2 && options.Profile <= SvgValidationProfile.LegacyPreservation;
    }
bool ValidCss(SvgStyleResolverOptions options)
    {
        if (options.Properties == null || options.Limits == null || options.SelectorOptions == null || options.StyleEnvironment == null) return false;
        if (options.SelectorOptions.Namespaces == null || options.SelectorOptions.DynamicState == null || options.SelectorOptions.MaxWork < 1) return false;
        if (options.MaxCacheEntries < 1 || options.MaxPaintFallbacks < 0) return false;
        SvgCssLimits limits = options.Limits;
        return limits.MaxCharacters > 0 && limits.MaxTokens > 0 && limits.MaxRules > 0 && limits.MaxDeclarations > 0 && limits.MaxSelectorTokens > 0 && limits.MaxBlockDepth > 0 && limits.MaxImportDepth >= 0 && limits.MaxVariableExpansionTokens > 0;
    }
}
