namespace svg;

/// <summary>Resolves SVG animation elements to bounded element and value-slot targets.</summary>
public class SvgAnimationTargetResolver : ISvgAnimationTargetResolver
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgAnimationTargetResolver()
    {
    }

    /// <summary>Resolves one animation using secure default catalogs and bounds.</summary>
    /// <param name="source">The animation element.</param>
    /// <returns>The resolved animation target and property.</returns>
    public SvgAnimation ResolveWithSource(SvgNode source)
    {
        return new SvgAnimationTargetResolver().ResolveWithSourceAndOptions(source, new SvgAnimationTargetOptions());
    }

    /// <summary>Resolves one animation using explicit catalogs, reference policy, and bounds.</summary>
    /// <param name="source">The animation element.</param>
    /// <param name="options">The target-resolution options.</param>
    /// <returns>The resolved animation target and property.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public SvgAnimation ResolveWithSourceAndOptions(SvgNode source, SvgAnimationTargetOptions options)
    {
        if (source == null) throw new System.ArgumentNullException("source");
        if (options == null) throw new System.ArgumentNullException("options");
        SvgAnimation result = new SvgAnimation(source);
        if (source.Kind != SvgNodeKind.Element)
        {
            new SvgAnimationTargetResolver().Fail(result, SvgAnimationTargetError.InvalidAnimationElement, SvgDiagnosticCode.InvalidArgument, "The animation source must be an element.", source.SourceSpan);
            return result;
        }
        result.Kind = new SvgAnimationTargetResolver().AnimationKind(source.ElementKind);
        if (result.Kind == SvgAnimationKind.Unknown)
        {
            new SvgAnimationTargetResolver().Fail(result, SvgAnimationTargetError.InvalidAnimationElement, SvgDiagnosticCode.InvalidValue, "The source element is not a supported animation element.", source.SourceSpan);
            return result;
        }
        if (options.PropertyCatalog == null || options.ElementCatalog == null || options.ReferenceOptions == null || options.MaxAttributeNameCharacters < 1)
        {
            new SvgAnimationTargetResolver().Fail(result, SvgAnimationTargetError.InvalidAnimationElement, SvgDiagnosticCode.InvalidArgument, "Animation target options are invalid.", source.SourceSpan);
            return result;
        }
        new SvgAnimationTargetResolver().ResolveTarget(source, options, result);
        if (result.Target == null) return result;
        result.TargetVersion = result.Target.Version;
        if (!new SvgAnimationTargetResolver().ValidTarget(result.Target))
        {
            new SvgAnimationTargetResolver().Fail(result, SvgAnimationTargetError.InvalidTarget, SvgDiagnosticCode.InvalidValue, "The resolved animation target is not an SVG element that can receive animation.", result.Target.SourceSpan);
            return result;
        }
        if (result.Kind == SvgAnimationKind.AnimateMotion)
        {
            result.TargetKind = SvgAnimationTargetKind.Motion;
            result.AttributeType = SvgAnimationAttributeType.Xml;
            result.AnimationType = SvgPropertyAnimationType.Transform;
            result.Success = true;
            return result;
        }
        if (result.Kind == SvgAnimationKind.Discard)
        {
            result.Success = true;
            return result;
        }
        new SvgAnimationTargetResolver().ResolveValueSlot(source, options, result);
        return result;
    }
SvgAnimationKind AnimationKind(SvgElementKind kind)
    {
        switch (kind)
        {
            case SvgElementKind.Animate:
                return SvgAnimationKind.Animate;
            case SvgElementKind.AnimateColor:
                return SvgAnimationKind.AnimateColor;
            case SvgElementKind.AnimateMotion:
                return SvgAnimationKind.AnimateMotion;
            case SvgElementKind.AnimateTransform:
                return SvgAnimationKind.AnimateTransform;
            case SvgElementKind.Set:
                return SvgAnimationKind.Set;
            case SvgElementKind.Discard:
                return SvgAnimationKind.Discard;
            default:
                return SvgAnimationKind.Unknown;
        }
    }
void ResolveTarget(SvgNode source, SvgAnimationTargetOptions options, SvgAnimation result)
    {
        SvgAttribute? href = new SvgAnimationTargetResolver().FindAttribute(source, "", SvgAttributeNames.Href);
        SvgAttribute? xlinkHref = new SvgAnimationTargetResolver().FindAttribute(source, SvgNamespaces.XLink, SvgAttributeNames.Href);
        if (href == null && xlinkHref == null)
        {
            if (source.Parent != null && source.Parent.Kind == SvgNodeKind.Element) result.Target = source.Parent;
            else new SvgAnimationTargetResolver().Fail(result, SvgAnimationTargetError.MissingTarget, SvgDiagnosticCode.InvalidReference, "An animation without href requires a parent element target.", source.SourceSpan);
            return;
        }
        result.TargetReference = new SvgReferenceResolver().ResolveWithNodeAndOptions(source, options.ReferenceOptions);
        new SvgAnimationTargetResolver().AppendDiagnostics(result.Diagnostics, result.TargetReference.Diagnostics);
        if (!result.TargetReference.Success || result.TargetReference.Target == null)
        {
            new SvgAnimationTargetResolver().FailWithoutDiagnostic(result, SvgAnimationTargetError.MissingTarget);
            if (result.TargetReference.Success) new SvgAnimationTargetResolver().AddDiagnostic(result, SvgDiagnosticCode.InvalidReference, "The animation href does not resolve to an available target element.", result.TargetReference.ReferenceSource);
            return;
        }
        result.Target = result.TargetReference.Target;
    }
bool ValidTarget(SvgNode target)
    {
        if (target.Kind != SvgNodeKind.Element || target.NamespaceUri.Length != 0 && target.NamespaceUri != SvgNamespaces.Svg) return false;
        SvgAnimationKind kind = new SvgAnimationTargetResolver().AnimationKind(target.ElementKind);
        return kind == SvgAnimationKind.Unknown && target.ElementKind != SvgElementKind.MPath;
    }
void ResolveValueSlot(SvgNode source, SvgAnimationTargetOptions options, SvgAnimation result)
    {
        result.AttributeNameSource = new SvgAnimationTargetResolver().FindAttribute(source, "", SvgAttributeNames.AttributeName);
        result.AttributeTypeSource = new SvgAnimationTargetResolver().FindAttribute(source, "", SvgAttributeNames.AttributeType);
        if (result.AttributeNameSource == null || result.AttributeNameSource.Value.Trim().Length == 0)
        {
            new SvgAnimationTargetResolver().Fail(result, SvgAnimationTargetError.MissingAttributeName, SvgDiagnosticCode.InvalidValue, "The animation requires a nonempty attributeName.", source.SourceSpan);
            return;
        }
        result.AttributeName = result.AttributeNameSource.Value.Trim();
        if (result.AttributeName.Length > options.MaxAttributeNameCharacters || !new SvgAnimationTargetResolver().ValidQualifiedName(result.AttributeName))
        {
            SvgDiagnosticCode code = result.AttributeName.Length > options.MaxAttributeNameCharacters ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue;
            new SvgAnimationTargetResolver().Fail(result, SvgAnimationTargetError.InvalidAttributeName, code, "The animation attributeName is invalid or exceeds its configured bound.", result.AttributeNameSource.SourceSpan);
            return;
        }
        result.AttributeType = SvgAnimationAttributeType.Auto;
        if (result.AttributeTypeSource != null)
        {
            SvgAnimationKeywordParseResult parsed = new SvgAnimationKeywordParser().ParseAttributeType(result.AttributeTypeSource.Value);
            if (!parsed.Success)
            {
                new SvgAnimationTargetResolver().Fail(result, SvgAnimationTargetError.InvalidAttributeType, SvgDiagnosticCode.InvalidValue, "The animation attributeType must be auto, CSS, or XML.", result.AttributeTypeSource.SourceSpan);
                return;
            }
            result.AttributeType = (SvgAnimationAttributeType)parsed.Value;
        }
        if (result.Kind == SvgAnimationKind.AnimateTransform)
        {
            if (result.AttributeName != SvgAttributeNames.Transform || result.AttributeType == SvgAnimationAttributeType.Css)
            {
                new SvgAnimationTargetResolver().Fail(result, SvgAnimationTargetError.InvalidAttributeName, SvgDiagnosticCode.InvalidValue, "animateTransform must target the XML transform attribute.", result.AttributeNameSource.SourceSpan);
                return;
            }
            new SvgAnimationTargetResolver().ResolveTransform(options, result);
            return;
        }
        if (result.AttributeType == SvgAnimationAttributeType.Css)
        {
            new SvgAnimationTargetResolver().ResolveCss(options, result);
            return;
        }
        if (result.AttributeType == SvgAnimationAttributeType.Xml)
        {
            new SvgAnimationTargetResolver().ResolveXml(options, result);
            return;
        }
        SvgPropertySupportEntry? property = options.PropertyCatalog.Find(result.AttributeName);
        if (property != null)
        {
            new SvgAnimationTargetResolver().ResolveProperty(property, SvgAnimationTargetKind.CssProperty, result);
            return;
        }
        new SvgAnimationTargetResolver().ResolveXml(options, result);
    }
void ResolveCss(SvgAnimationTargetOptions options, SvgAnimation result)
    {
        if (result.AttributeName.IndexOf(':') >= 0)
        {
            new SvgAnimationTargetResolver().Fail(result, SvgAnimationTargetError.UnknownProperty, SvgDiagnosticCode.InvalidCss, "A CSS property name cannot be namespace-qualified.", (result.AttributeNameSource ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
            return;
        }
        SvgPropertySupportEntry? property = options.PropertyCatalog.Find(result.AttributeName);
        if (property == null)
        {
            new SvgAnimationTargetResolver().Fail(result, SvgAnimationTargetError.UnknownProperty, SvgDiagnosticCode.InvalidCss, "The animation names an unknown CSS property.", (result.AttributeNameSource ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
            return;
        }
        new SvgAnimationTargetResolver().ResolveProperty(property, SvgAnimationTargetKind.CssProperty, result);
    }
void ResolveProperty(SvgPropertySupportEntry property, SvgAnimationTargetKind kind, SvgAnimation result)
    {
        result.Property = property;
        result.LocalName = property.Name;
        result.NamespaceUri = "";
        if (property.AnimationType == SvgPropertyAnimationType.NotAnimatable || property.IsShorthand)
        {
            new SvgAnimationTargetResolver().Fail(result, SvgAnimationTargetError.NotAnimatable, SvgDiagnosticCode.UnsupportedFeature, "The selected CSS property is not animatable.", (result.AttributeNameSource ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
            return;
        }
        result.TargetKind = property.Name == SvgAttributeNames.Transform ? SvgAnimationTargetKind.Transform : kind;
        result.AnimationType = property.AnimationType;
        result.Success = true;
    }
void ResolveTransform(SvgAnimationTargetOptions options, SvgAnimation result)
    {
        SvgElementDefinition? target = options.ElementCatalog.Find((result.Target ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ElementKind);
        if (target == null || target.FindAttribute("", SvgAttributeNames.Transform) == null)
        {
            new SvgAnimationTargetResolver().Fail(result, SvgAnimationTargetError.InapplicableAttribute, SvgDiagnosticCode.InvalidValue, "The transform attribute does not apply to the animation target.", (result.AttributeNameSource ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
            return;
        }
        result.AttributeType = SvgAnimationAttributeType.Xml;
        result.LocalName = SvgAttributeNames.Transform;
        result.TargetKind = SvgAnimationTargetKind.Transform;
        result.AnimationType = SvgPropertyAnimationType.Transform;
        result.Property = options.PropertyCatalog.Find(SvgAttributeNames.Transform);
        result.Success = true;
    }
void ResolveXml(SvgAnimationTargetOptions options, SvgAnimation result)
    {
        int colon = result.AttributeName.IndexOf(':');
        if (colon >= 0)
        {
            result.AttributePrefix = result.AttributeName.Substring(0, colon);
            result.LocalName = result.AttributeName.Substring(colon + 1);
            result.NamespaceUri = new SvgAnimationTargetResolver().ResolvePrefix((result.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), result.AttributePrefix);
            if (result.NamespaceUri.Length == 0)
            {
                new SvgAnimationTargetResolver().Fail(result, SvgAnimationTargetError.InvalidAttributeName, SvgDiagnosticCode.XmlNamespace, "The animation attributeName uses an undeclared namespace prefix.", (result.AttributeNameSource ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
                return;
            }
        }
        else result.LocalName = result.AttributeName;
        SvgElementDefinition? target = options.ElementCatalog.Find((result.Target ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ElementKind);
        SvgElementAttributeDefinition? attribute = target == null ? null : target.FindAttribute(result.NamespaceUri, result.LocalName);
        if (attribute == null)
        {
            new SvgAnimationTargetResolver().Fail(result, SvgAnimationTargetError.InapplicableAttribute, SvgDiagnosticCode.InvalidValue, "The selected XML attribute does not apply to the animation target.", (result.AttributeNameSource ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
            return;
        }
        if (new SvgAnimationTargetResolver().NonAnimatableAttribute(result.NamespaceUri, result.LocalName))
        {
            new SvgAnimationTargetResolver().Fail(result, SvgAnimationTargetError.NotAnimatable, SvgDiagnosticCode.UnsupportedFeature, "The selected XML attribute is not animatable.", (result.AttributeNameSource ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
            return;
        }
        SvgPropertySupportEntry? property = result.NamespaceUri.Length == 0 ? options.PropertyCatalog.FindPresentationAttribute(result.LocalName) : null;
        if (property != null)
        {
            new SvgAnimationTargetResolver().ResolveProperty(property, SvgAnimationTargetKind.PresentationProperty, result);
            result.AttributeType = SvgAnimationAttributeType.Xml;
            return;
        }
        result.TargetKind = result.LocalName == SvgAttributeNames.Transform ? SvgAnimationTargetKind.Transform : SvgAnimationTargetKind.XmlAttribute;
        result.AnimationType = new SvgAnimationTargetResolver().XmlAnimationType(result.LocalName);
        result.Success = true;
    }
SvgPropertyAnimationType XmlAnimationType(string name)
    {
        if (name == SvgAttributeNames.Transform || name.EndsWith("Transform", System.StringComparison.Ordinal)) return SvgPropertyAnimationType.Transform;
        if (name == SvgAttributeNames.ViewBox || name == SvgAttributeNames.Points || name == SvgAttributeNames.Rotate || name == "tableValues" || name == "kernelMatrix" || name == "order" || name == "radius" || name == "baseFrequency" || name == "stdDeviation" || name == "values") return SvgPropertyAnimationType.List;
        if (name == SvgAttributeNames.D || name == SvgAttributeNames.Path) return SvgPropertyAnimationType.ComputedValue;
        if (name == SvgAttributeNames.PathLength || name == SvgAttributeNames.Offset || name == "numOctaves" || name == "seed" || name == "scale" || name == "surfaceScale" || name == "diffuseConstant" || name == "specularConstant" || name == "specularExponent" || name == "limitingConeAngle" || name == "slope" || name == "intercept" || name == "amplitude" || name == "exponent" || name == "k1" || name == "k2" || name == "k3" || name == "k4") return SvgPropertyAnimationType.Number;
        if (name == SvgAttributeNames.X || name == SvgAttributeNames.Y || name == SvgAttributeNames.X1 || name == SvgAttributeNames.Y1 || name == SvgAttributeNames.X2 || name == SvgAttributeNames.Y2 || name == SvgAttributeNames.Cx || name == SvgAttributeNames.Cy || name == SvgAttributeNames.R || name == SvgAttributeNames.Rx || name == SvgAttributeNames.Ry || name == SvgAttributeNames.Width || name == SvgAttributeNames.Height || name == SvgAttributeNames.Dx || name == SvgAttributeNames.Dy || name == SvgAttributeNames.TextLength || name == SvgAttributeNames.StartOffset || name == SvgAttributeNames.MarkerWidth || name == SvgAttributeNames.MarkerHeight || name == SvgAttributeNames.RefX || name == SvgAttributeNames.RefY || name == SvgAttributeNames.Fx || name == SvgAttributeNames.Fy || name == SvgAttributeNames.Fr) return SvgPropertyAnimationType.Length;
        return SvgPropertyAnimationType.Discrete;
    }
bool NonAnimatableAttribute(string namespaceUri, string name)
    {
        if (namespaceUri == SvgNamespaces.Xmlns || namespaceUri == SvgNamespaces.Xml) return true;
        if (name == SvgAttributeNames.Id || name == SvgAttributeNames.Class || name == SvgAttributeNames.Style || name == SvgAttributeNames.TabIndex || name == SvgAttributeNames.Lang || name == SvgAttributeNames.RequiredExtensions || name == SvgAttributeNames.RequiredFeatures || name == SvgAttributeNames.SystemLanguage) return true;
        return name.StartsWith("on", System.StringComparison.Ordinal);
    }
string ResolvePrefix(SvgNode source, string prefix)
    {
        if (prefix == "xml") return SvgNamespaces.Xml;
        SvgNode? current = source;
        while (current != null)
        {
            int index = current.Attributes.Count - 1;
            while (index >= 0)
            {
                SvgAttribute attribute = current.Attributes[index];
                if (attribute.NamespaceUri == SvgNamespaces.Xmlns && attribute.Prefix == "xmlns" && attribute.LocalName == prefix) return attribute.Value;
                index = index - 1;
            }
            current = current.Parent;
        }
        return "";
    }
bool ValidQualifiedName(string name)
    {
        if (name.Length == 0 || !new SvgAnimationTargetResolver().NameStart(name[0])) return false;
        bool colon = false;
        int index = 1;
        while (index < name.Length)
        {
            char character = name[index];
            if (character == ':')
            {
                if (colon || index + 1 >= name.Length || !new SvgAnimationTargetResolver().NameStart(name[index + 1])) return false;
                colon = true;
                index = index + 2;
                continue;
            }
            if (!new SvgAnimationTargetResolver().NameCharacter(character)) return false;
            index = index + 1;
        }
        return true;
    }
bool NameStart(char character)
    {
        return character == '_' || char.IsLetter(character);
    }
bool NameCharacter(char character)
    {
        return new SvgAnimationTargetResolver().NameStart(character) || char.IsDigit(character) || character == '-' || character == '.';
    }
SvgAttribute? FindAttribute(SvgNode node, string namespaceUri, string localName)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri == namespaceUri && attribute.LocalName == localName) return attribute;
            index = index + 1;
        }
        return null;
    }
void AppendDiagnostics(System.Collections.Generic.List<SvgDiagnostic> target, System.Collections.Generic.List<SvgDiagnostic> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }
void Fail(SvgAnimation result, SvgAnimationTargetError error, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        new SvgAnimationTargetResolver().FailWithoutDiagnostic(result, error);
        new SvgAnimationTargetResolver().AddDiagnostic(result, code, message, source);
    }
void FailWithoutDiagnostic(SvgAnimation result, SvgAnimationTargetError error)
    {
        result.Error = error;
        result.Success = false;
    }
void AddDiagnostic(SvgAnimation result, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
    }
}
