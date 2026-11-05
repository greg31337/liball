namespace svg;

internal class SvgElementCatalogBuilder
{
    System.Collections.Generic.List<SvgElementDefinition> entries;
    System.Collections.Generic.List<SvgElementAttributeDefinition> globals;
    System.Collections.Generic.List<SvgElementAttributeDefinition> presentations;
    string animationChildren;
    string descriptiveChildren;
    string graphicsChildren;
    string filterPrimitiveChildren;
    string filterFunctionChildren;
    string lightChildren;

    internal SvgElementCatalogBuilder()
    {
        entries = new System.Collections.Generic.List<SvgElementDefinition>();
        globals = GlobalAttributes();
        presentations = PresentationAttributes();
        animationChildren = "animate,animateMotion,animateTransform,discard,set";
        descriptiveChildren = "desc,metadata,title";
        graphicsChildren = "a,audio,canvas,circle,clipPath,defs,ellipse,filter,foreignObject,g,iframe,image,line,linearGradient,marker,mask,path,pattern,polygon,polyline,radialGradient,rect,script,style,svg,switch,symbol,text,use,video,view";
        filterPrimitiveChildren = "feBlend,feColorMatrix,feComponentTransfer,feComposite,feConvolveMatrix,feDiffuseLighting,feDisplacementMap,feDropShadow,feFlood,feGaussianBlur,feImage,feMerge,feMorphology,feOffset,feSpecularLighting,feTile,feTurbulence";
        filterFunctionChildren = "feFuncA,feFuncB,feFuncG,feFuncR";
        lightChildren = "feDistantLight,fePointLight,feSpotLight";
    }

    internal SvgElementCatalog Build()
    {
        Add(SvgElementKind.A, SvgElementNames.A, "structural,graphics", SvgElementRenderMode.Container, true, false, Without(FlowChildren(), SvgElementNames.A), "href;target=_self;download;hreflang;type;rel");
        Add(SvgElementKind.Animate, SvgElementNames.Animate, "animation", SvgElementRenderMode.Never, false, false, descriptiveChildren, AnimationAttributes("linear"));
        Add(SvgElementKind.AnimateMotion, SvgElementNames.AnimateMotion, "animation", SvgElementRenderMode.Never, false, false, Join(descriptiveChildren, "mpath"), AnimationAttributes("paced") + ";path;rotate=0;keyPoints");
        Add(SvgElementKind.AnimateTransform, SvgElementNames.AnimateTransform, "animation", SvgElementRenderMode.Never, false, false, descriptiveChildren, AnimationAttributes("linear") + ";!type");
        Add(SvgElementKind.Audio, SvgElementNames.Audio, "graphics,embedded", SvgElementRenderMode.HostCallback, true, true, descriptiveChildren, "!href;crossorigin");
        Add(SvgElementKind.Canvas, SvgElementNames.Canvas, "graphics,embedded", SvgElementRenderMode.HostCallback, true, true, descriptiveChildren, "x=0;y=0;width=auto;height=auto");
        Add(SvgElementKind.Circle, SvgElementNames.Circle, "graphics,shape", SvgElementRenderMode.Direct, true, false, LeafChildren(), "cx=0;cy=0;r=0;pathLength");
        Add(SvgElementKind.ClipPath, SvgElementNames.ClipPath, "structural,graphics", SvgElementRenderMode.Referenced, true, false, FlowChildren(), "clipPathUnits=userSpaceOnUse;transform");
        Add(SvgElementKind.Defs, SvgElementNames.Defs, "structural", SvgElementRenderMode.Never, false, false, FlowChildren(), "transform");
        Add(SvgElementKind.Desc, SvgElementNames.Desc, "descriptive", SvgElementRenderMode.Never, false, true, FlowChildren(), "");
        Add(SvgElementKind.Discard, SvgElementNames.Discard, "animation", SvgElementRenderMode.Never, false, false, descriptiveChildren, "href;begin=0s");
        Add(SvgElementKind.Ellipse, SvgElementNames.Ellipse, "graphics,shape", SvgElementRenderMode.Direct, true, false, LeafChildren(), "cx=0;cy=0;rx=auto;ry=auto;pathLength");
        AddFilterPrimitive(SvgElementKind.FeBlend, SvgElementNames.FeBlend, "in;in2;mode=normal");
        AddFilterPrimitive(SvgElementKind.FeColorMatrix, SvgElementNames.FeColorMatrix, "in;type=matrix;values");
        AddFilterPrimitiveWithChildren(SvgElementKind.FeComponentTransfer, SvgElementNames.FeComponentTransfer, "in", filterFunctionChildren);
        LimitKinds(SvgElementKind.FeComponentTransfer, filterFunctionChildren, 0, 1, 0, 4);
        AddFilterPrimitive(SvgElementKind.FeComposite, SvgElementNames.FeComposite, "in;in2;operator=over;k1=0;k2=0;k3=0;k4=0");
        AddFilterPrimitive(SvgElementKind.FeConvolveMatrix, SvgElementNames.FeConvolveMatrix, "in;order=3;!kernelMatrix;divisor~;bias=0;targetX~;targetY~;edgeMode=duplicate;kernelUnitLength;preserveAlpha=false");
        AddFilterPrimitiveWithChildren(SvgElementKind.FeDiffuseLighting, SvgElementNames.FeDiffuseLighting, "in;surfaceScale=1;diffuseConstant=1;kernelUnitLength", lightChildren);
        LimitKinds(SvgElementKind.FeDiffuseLighting, lightChildren, 0, 1, 1, 1);
        AddFilterPrimitive(SvgElementKind.FeDisplacementMap, SvgElementNames.FeDisplacementMap, "in;in2;scale=0;xChannelSelector=A;yChannelSelector=A");
        AddLight(SvgElementKind.FeDistantLight, SvgElementNames.FeDistantLight, "azimuth=0;elevation=0");
        AddFilterPrimitive(SvgElementKind.FeDropShadow, SvgElementNames.FeDropShadow, "in;dx=2;dy=2;stdDeviation=2");
        AddFilterPrimitive(SvgElementKind.FeFlood, SvgElementNames.FeFlood, "");
        AddFilterFunction(SvgElementKind.FeFuncA, SvgElementNames.FeFuncA);
        AddFilterFunction(SvgElementKind.FeFuncB, SvgElementNames.FeFuncB);
        AddFilterFunction(SvgElementKind.FeFuncG, SvgElementNames.FeFuncG);
        AddFilterFunction(SvgElementKind.FeFuncR, SvgElementNames.FeFuncR);
        AddFilterPrimitive(SvgElementKind.FeGaussianBlur, SvgElementNames.FeGaussianBlur, "in;stdDeviation=0;edgeMode=none");
        AddFilterPrimitive(SvgElementKind.FeImage, SvgElementNames.FeImage, "href;preserveAspectRatio=xMidYMid meet;crossorigin");
        AddFilterPrimitiveWithChildren(SvgElementKind.FeMerge, SvgElementNames.FeMerge, "", "feMergeNode");
        SetTotalLimits(SvgElementKind.FeMerge, 1, -1);
        Add(SvgElementKind.FeMergeNode, SvgElementNames.FeMergeNode, "filter", SvgElementRenderMode.Resource, false, false, LeafChildren(), "in");
        AddFilterPrimitive(SvgElementKind.FeMorphology, SvgElementNames.FeMorphology, "in;operator=erode;radius=0");
        AddFilterPrimitive(SvgElementKind.FeOffset, SvgElementNames.FeOffset, "in;dx=0;dy=0");
        AddLight(SvgElementKind.FePointLight, SvgElementNames.FePointLight, "x=0;y=0;z=0");
        AddFilterPrimitiveWithChildren(SvgElementKind.FeSpecularLighting, SvgElementNames.FeSpecularLighting, "in;surfaceScale=1;specularConstant=1;specularExponent=1;kernelUnitLength", lightChildren);
        LimitKinds(SvgElementKind.FeSpecularLighting, lightChildren, 0, 1, 1, 1);
        AddLight(SvgElementKind.FeSpotLight, SvgElementNames.FeSpotLight, "x=0;y=0;z=0;pointsAtX=0;pointsAtY=0;pointsAtZ=0;specularExponent=1;limitingConeAngle=90");
        AddFilterPrimitive(SvgElementKind.FeTile, SvgElementNames.FeTile, "in");
        AddFilterPrimitive(SvgElementKind.FeTurbulence, SvgElementNames.FeTurbulence, "baseFrequency=0;numOctaves=1;seed=0;stitchTiles=noStitch;type=turbulence");
        Add(SvgElementKind.Filter, SvgElementNames.Filter, "filter", SvgElementRenderMode.Resource, true, false, Join(Join(animationChildren, descriptiveChildren), filterPrimitiveChildren), "x=-10%;y=-10%;width=120%;height=120%;filterUnits=objectBoundingBox;primitiveUnits=userSpaceOnUse;href");
        Add(SvgElementKind.ForeignObject, SvgElementNames.ForeignObject, "graphics,embedded", SvgElementRenderMode.HostCallback, true, true, descriptiveChildren, "x=0;y=0;width=auto;height=auto;transform");
        Add(SvgElementKind.G, SvgElementNames.G, "structural,graphics", SvgElementRenderMode.Container, true, false, FlowChildren(), "transform");
        Add(SvgElementKind.Iframe, SvgElementNames.Iframe, "graphics,embedded", SvgElementRenderMode.HostCallback, true, true, descriptiveChildren, "!href;x=0;y=0;width=auto;height=auto");
        Add(SvgElementKind.Image, SvgElementNames.Image, "graphics,embedded", SvgElementRenderMode.Direct, true, false, LeafChildren(), "!href;x=0;y=0;width=auto;height=auto;preserveAspectRatio=xMidYMid meet;crossorigin");
        Add(SvgElementKind.Line, SvgElementNames.Line, "graphics,shape", SvgElementRenderMode.Direct, true, false, LeafChildren(), "x1=0;y1=0;x2=0;y2=0;pathLength");
        Add(SvgElementKind.LinearGradient, SvgElementNames.LinearGradient, "paint", SvgElementRenderMode.Resource, true, false, Join(LeafChildren(), "stop"), "gradientUnits=objectBoundingBox;gradientTransform;spreadMethod=pad;x1=0%;y1=0%;x2=100%;y2=0%;href");
        Add(SvgElementKind.Marker, SvgElementNames.Marker, "structural,graphics", SvgElementRenderMode.Referenced, true, false, FlowChildren(), "markerUnits=strokeWidth;refX=0;refY=0;markerWidth=3;markerHeight=3;orient=0;viewBox;preserveAspectRatio=xMidYMid meet");
        Add(SvgElementKind.Mask, SvgElementNames.Mask, "structural,graphics", SvgElementRenderMode.Referenced, true, false, FlowChildren(), "maskUnits=objectBoundingBox;maskContentUnits=userSpaceOnUse;x=-10%;y=-10%;width=120%;height=120%");
        Add(SvgElementKind.Metadata, SvgElementNames.Metadata, "descriptive", SvgElementRenderMode.Never, false, true, FlowChildren(), "");
        Add(SvgElementKind.MPath, SvgElementNames.MPath, "animation", SvgElementRenderMode.Never, false, false, descriptiveChildren, "!href");
        Add(SvgElementKind.Path, SvgElementNames.Path, "graphics,shape", SvgElementRenderMode.Direct, true, false, LeafChildren(), "d=;pathLength");
        Add(SvgElementKind.Pattern, SvgElementNames.Pattern, "paint,structural", SvgElementRenderMode.Referenced, true, false, FlowChildren(), "x=0;y=0;width=0;height=0;patternUnits=objectBoundingBox;patternContentUnits=userSpaceOnUse;patternTransform;viewBox;preserveAspectRatio=xMidYMid meet;href");
        Add(SvgElementKind.Polygon, SvgElementNames.Polygon, "graphics,shape", SvgElementRenderMode.Direct, true, false, LeafChildren(), "points=;pathLength");
        Add(SvgElementKind.Polyline, SvgElementNames.Polyline, "graphics,shape", SvgElementRenderMode.Direct, true, false, LeafChildren(), "points=;pathLength");
        Add(SvgElementKind.RadialGradient, SvgElementNames.RadialGradient, "paint", SvgElementRenderMode.Resource, true, false, Join(LeafChildren(), "stop"), "gradientUnits=objectBoundingBox;gradientTransform;spreadMethod=pad;cx=50%;cy=50%;r=50%;fx@cx;fy@cy;fr=0%;href");
        Add(SvgElementKind.Rect, SvgElementNames.Rect, "graphics,shape", SvgElementRenderMode.Direct, true, false, LeafChildren(), "x=0;y=0;width=auto;height=auto;rx=auto;ry=auto;pathLength");
        Add(SvgElementKind.Script, SvgElementNames.Script, "script", SvgElementRenderMode.Never, false, true, "", "href;type;crossorigin");
        Add(SvgElementKind.Set, SvgElementNames.Set, "animation", SvgElementRenderMode.Never, false, false, descriptiveChildren, AnimationAttributes("discrete").Replace(";to;", ";!to;", System.StringComparison.Ordinal));
        Add(SvgElementKind.Stop, SvgElementNames.Stop, "paint", SvgElementRenderMode.Resource, true, false, LeafChildren(), "offset=0");
        Add(SvgElementKind.Style, SvgElementNames.Style, "script", SvgElementRenderMode.Never, false, true, "", "type=text/css;media=all;title=");
        Add(SvgElementKind.Svg, SvgElementNames.Svg, "structural,graphics", SvgElementRenderMode.Container, true, false, FlowChildren(), "x=0;y=0;width~;height~;viewBox;preserveAspectRatio=xMidYMid meet");
        Add(SvgElementKind.Switch, SvgElementNames.Switch, "structural,graphics", SvgElementRenderMode.Container, true, false, FlowChildren(), "transform");
        Add(SvgElementKind.Symbol, SvgElementNames.Symbol, "structural,graphics", SvgElementRenderMode.Referenced, true, false, FlowChildren(), "x=0;y=0;width=auto;height=auto;viewBox;preserveAspectRatio=xMidYMid meet;refX=0;refY=0");
        Add(SvgElementKind.Text, SvgElementNames.Text, "graphics,text", SvgElementRenderMode.Text, true, false, TextChildren(true), "x=0;y=0;dx=0;dy=0;rotate=0;textLength;lengthAdjust=spacing;transform");
        Add(SvgElementKind.TextPath, SvgElementNames.TextPath, "graphics,text", SvgElementRenderMode.Text, true, false, TextChildren(false), "href;startOffset=0;method=align;spacing=exact;side=left;path;textLength;lengthAdjust=spacing");
        Add(SvgElementKind.Title, SvgElementNames.Title, "descriptive", SvgElementRenderMode.Never, false, true, FlowChildren(), "");
        Add(SvgElementKind.TSpan, SvgElementNames.TSpan, "graphics,text", SvgElementRenderMode.Text, true, false, TextChildren(false), "x;y;dx=0;dy=0;rotate=0;textLength;lengthAdjust=spacing");
        Add(SvgElementKind.Use, SvgElementNames.Use, "structural,graphics", SvgElementRenderMode.Direct, true, false, LeafChildren(), "!href;x=0;y=0;width=auto;height=auto");
        Add(SvgElementKind.Video, SvgElementNames.Video, "graphics,embedded", SvgElementRenderMode.HostCallback, true, true, descriptiveChildren, "!href;x=0;y=0;width=auto;height=auto;preserveAspectRatio=xMidYMid meet;crossorigin");
        Add(SvgElementKind.View, SvgElementNames.View, "structural", SvgElementRenderMode.Never, false, false, "", "viewBox;preserveAspectRatio=xMidYMid meet");
        ValidateComplete();
        return new SvgElementCatalog(entries);
    }

    void AddFilterPrimitive(SvgElementKind kind, string name, string attributes)
    {
        AddFilterPrimitiveWithChildren(kind, name, attributes, LeafChildren());
    }

    void AddFilterPrimitiveWithChildren(SvgElementKind kind, string name, string attributes, string children)
    {
        string common = "x=0%;y=0%;width=100%;height=100%;result";
        Add(kind, name, "filter", SvgElementRenderMode.Resource, false, false, children, common + (attributes.Length == 0 ? "" : ";" + attributes));
    }

    void AddFilterFunction(SvgElementKind kind, string name)
    {
        Add(kind, name, "filter", SvgElementRenderMode.Resource, false, false, LeafChildren(), "type=identity;tableValues;slope=1;intercept=0;amplitude=1;exponent=1;offset=0");
    }

    void AddLight(SvgElementKind kind, string name, string attributes)
    {
        Add(kind, name, "filter", SvgElementRenderMode.Resource, false, false, descriptiveChildren, attributes);
    }

    void Add(SvgElementKind kind, string name, string categoryText, SvgElementRenderMode renderMode, bool conditional, bool foreign, string childText, string attributeText)
    {
        SvgElementContentModel content = new SvgElementContentModel(TextAllowed(kind), foreign);
        AddChildKinds(content, childText);
        SvgElementDefinition definition = new SvgElementDefinition(kind, name, ParseCategories(categoryText), content, ParseAttributes(attributeText), renderMode, conditional, globals, presentations);
        entries.Add(definition);
    }

    void AddChildKinds(SvgElementContentModel model, string names)
    {
        System.Collections.Generic.List<string> parts = Split(names, ',');
        int index = 0;
        while (index < parts.Count)
        {
            if (parts[index].Length != 0)
            {
                SvgElementKind kind = new SvgNodeNames().GetElementKind("", parts[index]);
                if (!model.AllowsElement(kind)) model.ChildRules.Add(new SvgElementChildRule(kind, 0, -1));
            }
            index = index + 1;
        }
    }

    void LimitKinds(SvgElementKind owner, string names, int minimum, int maximum, int totalMinimum, int totalMaximum)
    {
        SvgElementContentModel model = (Find(owner) ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ContentModel;
        model.MinimumElementChildren = totalMinimum;
        model.MaximumElementChildren = totalMaximum;
        System.Collections.Generic.List<string> parts = Split(names, ',');
        int index = 0;
        while (index < parts.Count)
        {
            SvgElementKind kind = new SvgNodeNames().GetElementKind("", parts[index]);
            int ruleIndex = 0;
            while (ruleIndex < model.ChildRules.Count)
            {
                if (model.ChildRules[ruleIndex].Kind == kind)
                {
                    model.ChildRules[ruleIndex].Minimum = minimum;
                    model.ChildRules[ruleIndex].Maximum = maximum;
                }
                ruleIndex = ruleIndex + 1;
            }
            index = index + 1;
        }
    }

    void SetTotalLimits(SvgElementKind owner, int minimum, int maximum)
    {
        SvgElementContentModel model = (Find(owner) ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ContentModel;
        model.MinimumElementChildren = minimum;
        model.MaximumElementChildren = maximum;
    }

    SvgElementDefinition? Find(SvgElementKind kind)
    {
        int index = 0;
        while (index < entries.Count)
        {
            if (entries[index].Kind == kind) return entries[index];
            index = index + 1;
        }
        return null;
    }

    System.Collections.Generic.List<SvgElementCategory> ParseCategories(string text)
    {
        System.Collections.Generic.List<SvgElementCategory> result = new System.Collections.Generic.List<SvgElementCategory>();
        System.Collections.Generic.List<string> parts = Split(text, ',');
        int index = 0;
        while (index < parts.Count)
        {
            string part = parts[index];
            if (part == "structural") result.Add(SvgElementCategory.Structural);
            else if (part == "graphics") result.Add(SvgElementCategory.Graphics);
            else if (part == "shape") result.Add(SvgElementCategory.Shape);
            else if (part == "text") result.Add(SvgElementCategory.Text);
            else if (part == "descriptive") result.Add(SvgElementCategory.Descriptive);
            else if (part == "animation") result.Add(SvgElementCategory.Animation);
            else if (part == "paint") result.Add(SvgElementCategory.PaintServer);
            else if (part == "filter") result.Add(SvgElementCategory.Filter);
            else if (part == "embedded") result.Add(SvgElementCategory.Embedded);
            else if (part == "script") result.Add(SvgElementCategory.Scripting);
            index = index + 1;
        }
        return result;
    }

    System.Collections.Generic.List<SvgElementAttributeDefinition> ParseAttributes(string text)
    {
        System.Collections.Generic.List<SvgElementAttributeDefinition> result = new System.Collections.Generic.List<SvgElementAttributeDefinition>();
        System.Collections.Generic.List<string> parts = Split(text, ';');
        int index = 0;
        while (index < parts.Count)
        {
            string part = parts[index];
            if (part.Length != 0)
            {
                bool required = part[0] == '!';
                if (required) part = part.Substring(1);
                int equals = part.IndexOf('=');
                int related = part.IndexOf('@');
                bool contextual = part.EndsWith("~", System.StringComparison.Ordinal);
                SvgAttributeDefaultKind defaultKind = SvgAttributeDefaultKind.None;
                string name = part;
                string value = "";
                if (equals >= 0)
                {
                    name = part.Substring(0, equals);
                    value = part.Substring(equals + 1);
                    defaultKind = SvgAttributeDefaultKind.Literal;
                }
                else if (related >= 0)
                {
                    name = part.Substring(0, related);
                    value = part.Substring(related + 1);
                    defaultKind = SvgAttributeDefaultKind.OtherAttribute;
                }
                else if (contextual)
                {
                    name = part.Substring(0, part.Length - 1);
                    defaultKind = SvgAttributeDefaultKind.ContextDependent;
                }
                result.Add(new SvgElementAttributeDefinition("", name, required, defaultKind, value, false, false));
            }
            index = index + 1;
        }
        return result;
    }

    static System.Collections.Generic.List<SvgElementAttributeDefinition> GlobalAttributes()
    {
        System.Collections.Generic.List<SvgElementAttributeDefinition> result = new System.Collections.Generic.List<SvgElementAttributeDefinition>();
        result.Add(new SvgElementAttributeDefinition("", SvgAttributeNames.Id, false, SvgAttributeDefaultKind.None, "", true, false));
        result.Add(new SvgElementAttributeDefinition("", SvgAttributeNames.Class, false, SvgAttributeDefaultKind.None, "", true, false));
        result.Add(new SvgElementAttributeDefinition("", SvgAttributeNames.Style, false, SvgAttributeDefaultKind.None, "", true, false));
        result.Add(new SvgElementAttributeDefinition("", SvgAttributeNames.TabIndex, false, SvgAttributeDefaultKind.None, "", true, false));
        result.Add(new SvgElementAttributeDefinition("", SvgAttributeNames.Lang, false, SvgAttributeDefaultKind.None, "", true, false));
        result.Add(new SvgElementAttributeDefinition(SvgNamespaces.Xml, SvgAttributeNames.Base, false, SvgAttributeDefaultKind.None, "", true, false));
        result.Add(new SvgElementAttributeDefinition(SvgNamespaces.Xml, SvgAttributeNames.Lang, false, SvgAttributeDefaultKind.None, "", true, false));
        result.Add(new SvgElementAttributeDefinition(SvgNamespaces.Xml, SvgAttributeNames.Space, false, SvgAttributeDefaultKind.Literal, "default", true, false));
        return result;
    }

    static System.Collections.Generic.List<SvgElementAttributeDefinition> PresentationAttributes()
    {
        System.Collections.Generic.List<SvgElementAttributeDefinition> result = new System.Collections.Generic.List<SvgElementAttributeDefinition>();
        SvgPropertyCatalog catalog = SvgPropertyCatalog.CreateDefault();
        int index = 0;
        while (index < catalog.Entries.Count)
        {
            SvgPropertySupportEntry entry = catalog.Entries[index];
            if (entry.IsPresentationAttribute && FindAttribute(result, entry.PresentationAttributeName) == null)
            {
                result.Add(new SvgElementAttributeDefinition("", entry.PresentationAttributeName, false, SvgAttributeDefaultKind.Literal, entry.InitialValue, false, true));
            }
            index = index + 1;
        }
        return result;
    }

    static SvgElementAttributeDefinition? FindAttribute(System.Collections.Generic.List<SvgElementAttributeDefinition> items, string name)
    {
        int index = 0;
        while (index < items.Count)
        {
            if (items[index].LocalName == name) return items[index];
            index = index + 1;
        }
        return null;
    }

    string FlowChildren()
    {
        return Join(Join(animationChildren, descriptiveChildren), graphicsChildren);
    }

    string LeafChildren()
    {
        return Join(animationChildren, descriptiveChildren);
    }

    string TextChildren(bool textPath)
    {
        string children = Join(LeafChildren(), "a,tspan");
        return textPath ? Join(children, "textPath") : children;
    }

    string AnimationAttributes(string calcMode)
    {
        return "href;attributeName;begin=0s;dur=indefinite;end;min=0s;max=indefinite;restart=always;repeatCount=1;repeatDur;fill=remove;from;to;by;values;calcMode=" + calcMode + ";keyTimes;keySplines;additive=replace;accumulate=none";
    }

    static bool TextAllowed(SvgElementKind kind)
    {
        return kind == SvgElementKind.Text || kind == SvgElementKind.TextPath || kind == SvgElementKind.TSpan || kind == SvgElementKind.Title || kind == SvgElementKind.Desc || kind == SvgElementKind.Metadata || kind == SvgElementKind.Style || kind == SvgElementKind.Script || kind == SvgElementKind.Audio || kind == SvgElementKind.Canvas || kind == SvgElementKind.ForeignObject || kind == SvgElementKind.Iframe || kind == SvgElementKind.Video;
    }

    static string Join(string first, string second)
    {
        if (first.Length == 0) return second;
        if (second.Length == 0) return first;
        return first + "," + second;
    }

    static string Without(string names, string excluded)
    {
        System.Collections.Generic.List<string> parts = Split(names, ',');
        string result = "";
        int index = 0;
        while (index < parts.Count)
        {
            if (parts[index] != excluded) result = Join(result, parts[index]);
            index = index + 1;
        }
        return result;
    }

    static System.Collections.Generic.List<string> Split(string text, char separator)
    {
        System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();
        int start = 0;
        int index = 0;
        while (index <= text.Length)
        {
            if (index == text.Length || text[index] == separator)
            {
                result.Add(text.Substring(start, index - start));
                start = index + 1;
            }
            index = index + 1;
        }
        return result;
    }

    void ValidateComplete()
    {
        int expected = (int)SvgElementKind.A;
        while (expected <= (int)SvgElementKind.View)
        {
            SvgElementDefinition? found = Find((SvgElementKind)expected);
            if (found == null) throw new System.InvalidOperationException("The current SVG element catalog is incomplete.");
            expected = expected + 1;
        }
        if (entries.Count != (int)SvgElementKind.View) throw new System.InvalidOperationException("The current SVG element catalog contains duplicates.");
    }
}
