using System;
using System.Collections.Generic;

namespace gltf;

internal class AnimationValidator
{
    internal AnimationLayoutAnalyzer LayoutAnalyzer = new AnimationLayoutAnalyzer();

    internal GltfValidationReport ValidateDocumentAnimationRules(GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        GltfValidationReport report = new GltfValidationReport();
        if (document.Animations == null)
        {
            return report;
        }

        int animationIndex = 0;
        while (animationIndex < document.Animations.Count)
        {
            GltfAnimation animation = document.Animations[animationIndex];
            if (animation != null)
            {
                AnimationLayoutAnalysis analysis = LayoutAnalyzer.AnalyzeAnimation(
                    document,
                    animationIndex);
                CopyAnimationIssues(analysis.Diagnostics, report);
                ValidatePointerTargets(animation, animationIndex, report);
            }

            animationIndex++;
        }

        return report;
    }

    internal void ValidatePointerTargets(
        GltfAnimation animation,
        int animationIndex,
        GltfValidationReport report)
    {
        if (animation.Channels == null)
        {
            return;
        }

        int channelIndex = 0;
        while (channelIndex < animation.Channels.Count)
        {
            GltfAnimationChannel channel = animation.Channels[channelIndex];
            if (channel != null && channel.Target != null)
            {
                ValidatePointerTarget(
                    channel.Target,
                    animationIndex,
                    channelIndex,
                    report);
            }

            channelIndex++;
        }
    }

    internal void ValidatePointerTarget(
        GltfAnimationTarget target,
        int animationIndex,
        int channelIndex,
        GltfValidationReport report)
    {
        string pointer = $"/animations/{animationIndex}/channels/{channelIndex}/target";
        bool hasExtension = target.PropertyJson != null
            && (target.PropertyJson.Extensions.ContainsKey(
                    GltfExtensionNames.KhrAnimationPointer)
                || target.PropertyJson.TypedExtensions.ContainsKey(
                    GltfExtensionNames.KhrAnimationPointer));
        if (hasExtension)
        {
            if (target.HasNode)
            {
                AddAnimationValidationIssue(
                    report,
                    GltfIssueCodes.KhrAnimationPointerAnimationChannelTargetNode,
                    GltfValidationSeverity.Error,
                    "KHR_animation_pointer requires the animation channel target node to be absent.",
                    $"{pointer}/node");
            }

            if (target.Path != GltfAnimationPath.Pointer)
            {
                AddAnimationValidationIssue(
                    report,
                    GltfIssueCodes.KhrAnimationPointerAnimationChannelTargetPath,
                    GltfValidationSeverity.Error,
                    $"KHR_animation_pointer requires the animation channel target path to be pointer, not {(int)target.Path}.",
                    $"{pointer}/path");
            }

            return;
        }

        if (target.Path == GltfAnimationPath.Pointer)
        {
            AddAnimationValidationIssue(
                report,
                GltfIssueCodes.UndefinedProperty,
                GltfValidationSeverity.Error,
                "A pointer animation target must define the KHR_animation_pointer extension.",
                $"{pointer}/extensions/{GltfExtensionNames.KhrAnimationPointer}");
        }
    }

    internal void CopyAnimationIssues(
        List<GltfValidationIssue> issues,
        GltfValidationReport report)
    {
        int index = 0;
        while (index < issues.Count)
        {
            GltfValidationIssue issue = issues[index];
            report.Issues.Add(issue);
            if (issue.Severity == GltfValidationSeverity.Error)
            {
                report.IsValid = false;
            }

            index++;
        }
    }

    internal void AddAnimationValidationIssue(
        GltfValidationReport report,
        string code,
        GltfValidationSeverity severity,
        string message,
        string pointer)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = severity;
        issue.Message = message;
        issue.JsonPointer = pointer;
        report.Issues.Add(issue);
        if (severity == GltfValidationSeverity.Error)
        {
            report.IsValid = false;
        }
    }
}
