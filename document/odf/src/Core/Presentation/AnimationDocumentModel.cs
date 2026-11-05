using System;
using System.Collections.Generic;

namespace odf;

internal static class AnimationDocumentModel
{
    internal static void LoadAnimations(OdfDocument document)
    {
        if (document == null || document.Slides == null)
        {
            throw new OdfDocumentException(
                "Presentation slides are required for animation materialization.");
        }
        List<OdfAnimation> animations = new List<OdfAnimation>();
        int index = 0;
        while (index < document.Slides.Length)
        {
            OdfSlide slide = document.Slides[index];
            List<OdfAnimation> roots = new List<OdfAnimation>();
            if (slide.Node != null)
            {
                CollectAnimationRoots(
                    slide.Node,
                    slide,
                    roots,
                    animations);
            }
            slide.Animations = roots.ToArray();
            index = index + 1;
        }
        document.Animations = animations.ToArray();
        AnimationTimingModel.RefreshAnimations(document);
    }

    internal static void SynchronizeAnimations(OdfDocument document)
    {
        if (document == null || document.Animations == null)
        {
            throw new OdfDocumentException(
                "The animation collection is required for writing.");
        }
        int index = 0;
        while (index < document.Animations.Length)
        {
            OdfAnimation animation = document.Animations[index];
            if (animation == null || animation.Node == null)
            {
                throw new OdfEditException(
                    "An animation requires an underlying animation element.");
            }
            SynchronizeAnimation(animation);
            index = index + 1;
        }
        AnimationTimingModel.RefreshAnimations(document);
    }

    static void CollectAnimationRoots(
        OdfNode parent,
        OdfSlide slide,
        List<OdfAnimation> roots,
        List<OdfAnimation> animations)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode child = parent.Children[index];
            OdfAnimationKind kind = AnimationKind(child);
            if (kind != OdfAnimationKind.Unknown)
            {
                roots.Add(CreateAnimation(
                    child,
                    kind,
                    slide,
                    null,
                    animations));
            }
            else if (!IsElement(
                child,
                OdfNamespaces.Presentation,
                "notes"))
            {
                CollectAnimationRoots(
                    child,
                    slide,
                    roots,
                    animations);
            }
            index = index + 1;
        }
    }

    static OdfAnimation CreateAnimation(
        OdfNode node,
        OdfAnimationKind kind,
        OdfSlide slide,
        OdfAnimation? parent,
        List<OdfAnimation> animations)
    {
        OdfAnimation result = new OdfAnimation();
        result.Kind = kind;
        result.Id = Read(node, OdfNamespaces.Animation, "id");
        result.XmlId = Read(node, OdfNamespaces.Xml, "id");
        result.NodeType = Read(
            node,
            OdfNamespaces.Presentation,
            "node-type");
        result.PresetId = Read(
            node,
            OdfNamespaces.Presentation,
            "preset-id");
        result.PresetSubType = Read(
            node,
            OdfNamespaces.Presentation,
            "preset-sub-type");
        result.PresetClass = Read(
            node,
            OdfNamespaces.Presentation,
            "preset-class");
        result.MasterElement = Read(
            node,
            OdfNamespaces.Presentation,
            "master-element");
        result.GroupId = Read(
            node,
            OdfNamespaces.Presentation,
            "group-id");
        result.Begin = Read(node, OdfNamespaces.Smil, "begin");
        result.End = Read(node, OdfNamespaces.Smil, "end");
        result.Duration = Read(node, OdfNamespaces.Smil, "dur");
        result.Minimum = Read(node, OdfNamespaces.Smil, "min");
        result.Maximum = Read(node, OdfNamespaces.Smil, "max");
        result.RepeatCount = Read(
            node,
            OdfNamespaces.Smil,
            "repeatCount");
        result.RepeatDuration = Read(
            node,
            OdfNamespaces.Smil,
            "repeatDur");
        result.Restart = Read(node, OdfNamespaces.Smil, "restart");
        result.RestartDefault = Read(
            node,
            OdfNamespaces.Smil,
            "restartDefault");
        result.Fill = Read(node, OdfNamespaces.Smil, "fill");
        result.FillDefault = Read(
            node,
            OdfNamespaces.Smil,
            "fillDefault");
        result.EndSync = Read(node, OdfNamespaces.Smil, "endsync");
        result.Speed = Read(node, OdfNamespaces.Smil, "speed");
        result.Accelerate = Read(
            node,
            OdfNamespaces.Smil,
            "accelerate");
        result.Decelerate = Read(
            node,
            OdfNamespaces.Smil,
            "decelerate");
        result.AutoReverse = Read(
            node,
            OdfNamespaces.Smil,
            "autoReverse");
        result.TargetElement = Read(
            node,
            OdfNamespaces.Smil,
            "targetElement");
        result.SubItem = Read(
            node,
            OdfNamespaces.Animation,
            "sub-item");
        result.AttributeName = Read(
            node,
            OdfNamespaces.Smil,
            "attributeName");
        result.Values = Read(node, OdfNamespaces.Smil, "values");
        result.Formula = Read(
            node,
            OdfNamespaces.Animation,
            "formula");
        result.From = Read(node, OdfNamespaces.Smil, "from");
        result.To = Read(node, OdfNamespaces.Smil, "to");
        result.By = Read(node, OdfNamespaces.Smil, "by");
        result.CalculationMode = Read(
            node,
            OdfNamespaces.Smil,
            "calcMode");
        result.KeyTimes = Read(
            node,
            OdfNamespaces.Smil,
            "keyTimes");
        result.KeySplines = Read(
            node,
            OdfNamespaces.Smil,
            "keySplines");
        result.Additive = Read(
            node,
            OdfNamespaces.Smil,
            "additive");
        result.Accumulate = Read(
            node,
            OdfNamespaces.Smil,
            "accumulate");
        result.IterateType = Read(
            node,
            OdfNamespaces.Animation,
            "iterate-type");
        result.IterateInterval = Read(
            node,
            OdfNamespaces.Animation,
            "iterate-interval");
        result.MotionPath = Read(node, OdfNamespaces.Svg, "path");
        result.MotionOrigin = Read(node, OdfNamespaces.Svg, "origin");
        result.TransformType = Read(node, OdfNamespaces.Svg, "type");
        result.ColorInterpolation = Read(
            node,
            OdfNamespaces.Animation,
            "color-interpolation");
        result.ColorInterpolationDirection = Read(
            node,
            OdfNamespaces.Animation,
            "color-interpolation-direction");
        result.TransitionType = Read(
            node,
            OdfNamespaces.Smil,
            "type");
        result.TransitionSubtype = Read(
            node,
            OdfNamespaces.Smil,
            "subtype");
        result.TransitionDirection = Read(
            node,
            OdfNamespaces.Smil,
            "direction");
        result.FadeColor = Read(
            node,
            OdfNamespaces.Smil,
            "fadeColor");
        result.TransitionMode = Read(
            node,
            OdfNamespaces.Smil,
            "mode");
        result.Href = Read(node, OdfNamespaces.XLink, "href");
        result.AudioLevel = Read(
            node,
            OdfNamespaces.Animation,
            "audio-level");
        result.Command = Read(
            node,
            OdfNamespaces.Animation,
            "command");
        result.Parent = parent;
        result.Slide = slide;
        result.Node = node;
        animations.Add(result);
        result.Parameters = LoadParameters(result, node);
        result.Children = LoadAnimationChildren(
            node,
            slide,
            result,
            animations);
        return result;
    }

    static OdfAnimationParameter[] LoadParameters(
        OdfAnimation animation,
        OdfNode node)
    {
        List<OdfAnimationParameter> parameters =
            new List<OdfAnimationParameter>();
        List<OdfNode> originalNodes = new List<OdfNode>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsElement(child, OdfNamespaces.Animation, "param"))
            {
                OdfAnimationParameter parameter =
                    new OdfAnimationParameter();
                parameter.Name = Read(
                    child,
                    OdfNamespaces.Animation,
                    "name");
                parameter.Value = Read(
                    child,
                    OdfNamespaces.Animation,
                    "value");
                parameter.Node = child;
                parameter.OriginalName = parameter.Name;
                parameter.OriginalValue = parameter.Value;
                parameters.Add(parameter);
                originalNodes.Add(child);
            }
            index = index + 1;
        }
        animation.OriginalParameterNodes = originalNodes.ToArray();
        return parameters.ToArray();
    }

    static OdfAnimation[] LoadAnimationChildren(
        OdfNode node,
        OdfSlide slide,
        OdfAnimation parent,
        List<OdfAnimation> animations)
    {
        List<OdfAnimation> children = new List<OdfAnimation>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            OdfAnimationKind kind = AnimationKind(child);
            if (kind != OdfAnimationKind.Unknown)
            {
                children.Add(CreateAnimation(
                    child,
                    kind,
                    slide,
                    parent,
                    animations));
            }
            index = index + 1;
        }
        return children.ToArray();
    }

    static void SynchronizeAnimation(OdfAnimation animation)
    {
        OdfNode node = animation.Node ?? new OdfNode();
        string localName = AnimationName(animation.Kind);
        if (localName.Length > 0)
        {
            node.Name = StyleDocumentModel.QualifiedName(
                OdfNamespaces.Animation,
                localName,
                OdfNamespaces.AnimationPrefix);
        }
        WriteOptional(node, OdfNamespaces.Animation, "id", OdfNamespaces.AnimationPrefix, animation.Id);
        WriteOptional(node, OdfNamespaces.Xml, "id", OdfNamespaces.XmlPrefix, animation.XmlId);
        WriteOptional(node, OdfNamespaces.Presentation, "node-type", OdfNamespaces.PresentationPrefix, animation.NodeType);
        WriteOptional(node, OdfNamespaces.Presentation, "preset-id", OdfNamespaces.PresentationPrefix, animation.PresetId);
        WriteOptional(node, OdfNamespaces.Presentation, "preset-sub-type", OdfNamespaces.PresentationPrefix, animation.PresetSubType);
        WriteOptional(node, OdfNamespaces.Presentation, "preset-class", OdfNamespaces.PresentationPrefix, animation.PresetClass);
        WriteOptional(node, OdfNamespaces.Presentation, "master-element", OdfNamespaces.PresentationPrefix, animation.MasterElement);
        WriteOptional(node, OdfNamespaces.Presentation, "group-id", OdfNamespaces.PresentationPrefix, animation.GroupId);
        WriteOptional(node, OdfNamespaces.Smil, "begin", OdfNamespaces.SmilPrefix, animation.Begin);
        WriteOptional(node, OdfNamespaces.Smil, "end", OdfNamespaces.SmilPrefix, animation.End);
        WriteOptional(node, OdfNamespaces.Smil, "dur", OdfNamespaces.SmilPrefix, animation.Duration);
        WriteOptional(node, OdfNamespaces.Smil, "min", OdfNamespaces.SmilPrefix, animation.Minimum);
        WriteOptional(node, OdfNamespaces.Smil, "max", OdfNamespaces.SmilPrefix, animation.Maximum);
        WriteOptional(node, OdfNamespaces.Smil, "repeatCount", OdfNamespaces.SmilPrefix, animation.RepeatCount);
        WriteOptional(node, OdfNamespaces.Smil, "repeatDur", OdfNamespaces.SmilPrefix, animation.RepeatDuration);
        WriteOptional(node, OdfNamespaces.Smil, "restart", OdfNamespaces.SmilPrefix, animation.Restart);
        WriteOptional(node, OdfNamespaces.Smil, "restartDefault", OdfNamespaces.SmilPrefix, animation.RestartDefault);
        WriteOptional(node, OdfNamespaces.Smil, "fill", OdfNamespaces.SmilPrefix, animation.Fill);
        WriteOptional(node, OdfNamespaces.Smil, "fillDefault", OdfNamespaces.SmilPrefix, animation.FillDefault);
        WriteOptional(node, OdfNamespaces.Smil, "endsync", OdfNamespaces.SmilPrefix, animation.EndSync);
        WriteOptional(node, OdfNamespaces.Smil, "speed", OdfNamespaces.SmilPrefix, animation.Speed);
        WriteOptional(node, OdfNamespaces.Smil, "accelerate", OdfNamespaces.SmilPrefix, animation.Accelerate);
        WriteOptional(node, OdfNamespaces.Smil, "decelerate", OdfNamespaces.SmilPrefix, animation.Decelerate);
        WriteOptional(node, OdfNamespaces.Smil, "autoReverse", OdfNamespaces.SmilPrefix, animation.AutoReverse);
        WriteOptional(node, OdfNamespaces.Smil, "targetElement", OdfNamespaces.SmilPrefix, animation.TargetElement);
        WriteOptional(node, OdfNamespaces.Animation, "sub-item", OdfNamespaces.AnimationPrefix, animation.SubItem);
        WriteOptional(node, OdfNamespaces.Smil, "attributeName", OdfNamespaces.SmilPrefix, animation.AttributeName);
        WriteOptional(node, OdfNamespaces.Smil, "values", OdfNamespaces.SmilPrefix, animation.Values);
        WriteOptional(node, OdfNamespaces.Animation, "formula", OdfNamespaces.AnimationPrefix, animation.Formula);
        WriteOptional(node, OdfNamespaces.Smil, "from", OdfNamespaces.SmilPrefix, animation.From);
        WriteOptional(node, OdfNamespaces.Smil, "to", OdfNamespaces.SmilPrefix, animation.To);
        WriteOptional(node, OdfNamespaces.Smil, "by", OdfNamespaces.SmilPrefix, animation.By);
        WriteOptional(node, OdfNamespaces.Smil, "calcMode", OdfNamespaces.SmilPrefix, animation.CalculationMode);
        WriteOptional(node, OdfNamespaces.Smil, "keyTimes", OdfNamespaces.SmilPrefix, animation.KeyTimes);
        WriteOptional(node, OdfNamespaces.Smil, "keySplines", OdfNamespaces.SmilPrefix, animation.KeySplines);
        WriteOptional(node, OdfNamespaces.Smil, "additive", OdfNamespaces.SmilPrefix, animation.Additive);
        WriteOptional(node, OdfNamespaces.Smil, "accumulate", OdfNamespaces.SmilPrefix, animation.Accumulate);
        WriteOptional(node, OdfNamespaces.Animation, "iterate-type", OdfNamespaces.AnimationPrefix, animation.IterateType);
        WriteOptional(node, OdfNamespaces.Animation, "iterate-interval", OdfNamespaces.AnimationPrefix, animation.IterateInterval);
        WriteOptional(node, OdfNamespaces.Svg, "path", OdfNamespaces.SvgPrefix, animation.MotionPath);
        WriteOptional(node, OdfNamespaces.Svg, "origin", OdfNamespaces.SvgPrefix, animation.MotionOrigin);
        WriteOptional(node, OdfNamespaces.Svg, "type", OdfNamespaces.SvgPrefix, animation.TransformType);
        WriteOptional(node, OdfNamespaces.Animation, "color-interpolation", OdfNamespaces.AnimationPrefix, animation.ColorInterpolation);
        WriteOptional(node, OdfNamespaces.Animation, "color-interpolation-direction", OdfNamespaces.AnimationPrefix, animation.ColorInterpolationDirection);
        WriteOptional(node, OdfNamespaces.Smil, "type", OdfNamespaces.SmilPrefix, animation.TransitionType);
        WriteOptional(node, OdfNamespaces.Smil, "subtype", OdfNamespaces.SmilPrefix, animation.TransitionSubtype);
        WriteOptional(node, OdfNamespaces.Smil, "direction", OdfNamespaces.SmilPrefix, animation.TransitionDirection);
        WriteOptional(node, OdfNamespaces.Smil, "fadeColor", OdfNamespaces.SmilPrefix, animation.FadeColor);
        WriteOptional(node, OdfNamespaces.Smil, "mode", OdfNamespaces.SmilPrefix, animation.TransitionMode);
        WriteOptional(node, OdfNamespaces.XLink, "href", OdfNamespaces.XLinkPrefix, animation.Href);
        WriteOptional(node, OdfNamespaces.Animation, "audio-level", OdfNamespaces.AnimationPrefix, animation.AudioLevel);
        WriteOptional(node, OdfNamespaces.Animation, "command", OdfNamespaces.AnimationPrefix, animation.Command);
        SynchronizeParameters(animation, node);
    }

    static void SynchronizeParameters(
        OdfAnimation animation,
        OdfNode node)
    {
        if (animation.Parameters == null)
        {
            throw new OdfEditException(
                "An animation parameter collection cannot be null.");
        }
        int index = 0;
        while (index < animation.OriginalParameterNodes.Length)
        {
            OdfNode original = animation.OriginalParameterNodes[index];
            if (!ContainsParameterNode(animation.Parameters, original))
            {
                RemoveChild(node, original);
            }
            index = index + 1;
        }
        List<OdfNode> currentNodes = new List<OdfNode>();
        index = 0;
        while (index < animation.Parameters.Length)
        {
            OdfAnimationParameter parameter = animation.Parameters[index];
            if (parameter == null)
            {
                throw new OdfEditException(
                    "An animation parameter cannot be null.");
            }
            if (parameter.Node == null)
            {
                parameter.Node = CreateParameterElement(
                    node.Location.PartPath);
                AppendChild(node, parameter.Node);
            }
            WriteOptional(
                parameter.Node,
                OdfNamespaces.Animation,
                "name",
                OdfNamespaces.AnimationPrefix,
                parameter.Name);
            WriteOptional(
                parameter.Node,
                OdfNamespaces.Animation,
                "value",
                OdfNamespaces.AnimationPrefix,
                parameter.Value);
            parameter.OriginalName = parameter.Name;
            parameter.OriginalValue = parameter.Value;
            currentNodes.Add(parameter.Node);
            index = index + 1;
        }
        animation.OriginalParameterNodes = currentNodes.ToArray();
    }

    static bool ContainsParameterNode(
        OdfAnimationParameter[] parameters,
        OdfNode node)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            if (parameters[index] != null
                && parameters[index].Node == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfAnimationKind AnimationKind(OdfNode node)
    {
        if (node.Kind != OdfNodeKind.Element
            || node.Name == null
            || node.Name.NamespaceName != OdfNamespaces.Animation)
        {
            return OdfAnimationKind.Unknown;
        }
        string name = node.Name.LocalName;
        if (name == "seq")
        {
            return OdfAnimationKind.Sequence;
        }
        if (name == "par")
        {
            return OdfAnimationKind.Parallel;
        }
        if (name == "iterate")
        {
            return OdfAnimationKind.Iterate;
        }
        if (name == "animate")
        {
            return OdfAnimationKind.Animate;
        }
        if (name == "animateColor")
        {
            return OdfAnimationKind.AnimateColor;
        }
        if (name == "animateMotion")
        {
            return OdfAnimationKind.AnimateMotion;
        }
        if (name == "animateTransform")
        {
            return OdfAnimationKind.AnimateTransform;
        }
        if (name == "set")
        {
            return OdfAnimationKind.Set;
        }
        if (name == "command")
        {
            return OdfAnimationKind.Command;
        }
        if (name == "audio")
        {
            return OdfAnimationKind.Audio;
        }
        if (name == "transitionFilter")
        {
            return OdfAnimationKind.TransitionFilter;
        }
        return OdfAnimationKind.Unknown;
    }

    internal static string AnimationName(OdfAnimationKind kind)
    {
        switch (kind)
        {
            case OdfAnimationKind.Sequence:
                return "seq";
            case OdfAnimationKind.Parallel:
                return "par";
            case OdfAnimationKind.Iterate:
                return "iterate";
            case OdfAnimationKind.Animate:
                return "animate";
            case OdfAnimationKind.AnimateColor:
                return "animateColor";
            case OdfAnimationKind.AnimateMotion:
                return "animateMotion";
            case OdfAnimationKind.AnimateTransform:
                return "animateTransform";
            case OdfAnimationKind.Set:
                return "set";
            case OdfAnimationKind.Command:
                return "command";
            case OdfAnimationKind.Audio:
                return "audio";
            case OdfAnimationKind.TransitionFilter:
                return "transitionFilter";
            default:
                return "";
        }
    }

    static OdfNode CreateParameterElement(string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            OdfNamespaces.Animation,
            "param",
            OdfNamespaces.AnimationPrefix);
        result.Location.PartPath = partPath;
        result.WasEmptyElement = true;
        return result;
    }

    static void AppendChild(OdfNode parent, OdfNode child)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        Array.Copy(parent.Children, children, parent.Children.Length);
        children[children.Length - 1] = child;
        child.Parent = parent;
        parent.Children = children;
        parent.WasEmptyElement = false;
    }

    static void RemoveChild(OdfNode parent, OdfNode child)
    {
        List<OdfNode> children = new List<OdfNode>();
        int index = 0;
        while (index < parent.Children.Length)
        {
            if (parent.Children[index] != child)
            {
                children.Add(parent.Children[index]);
            }
            index = index + 1;
        }
        child.Parent = null;
        parent.Children = children.ToArray();
    }

    static void WriteOptional(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                namespaceName,
                localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                namespaceName,
                localName,
                prefix,
                value);
        }
    }

    static string Read(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return StyleDocumentModel.ReadAttribute(
            node,
            namespaceName,
            localName);
    }

    static bool IsElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == namespaceName
            && node.Name.LocalName == localName;
    }
}
