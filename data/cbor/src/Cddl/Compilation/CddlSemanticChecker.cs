using System;
using System.Collections.Generic;
using System.Numerics;

namespace cbor;

internal class CddlSemanticChecker
{
    internal const int ContextType = 1;
    internal const int ContextGroup = 2;
    internal const int ContextArrayGroup = 3;
    internal const int ContextMapGroup = 4;
    internal const int ContextHeadNumber = 5;
    internal const int OptionalUnknown = -1;
    internal const int OptionalNo = 0;
    internal const int OptionalYes = 1;
    internal const int TypeInteger = 1;
    internal const int TypeFloat = 2;
    internal const int TypeBytes = 4;
    internal const int TypeText = 8;
    internal const int TypeArray = 16;
    internal const int TypeMap = 32;
    internal const int TypeTag = 64;
    internal const int TypeSimple = 128;
    internal const int TypeAny = 255;
    internal const int SignNonnegative = 1;
    internal const int SignNegative = 2;

    internal CddlNormalizationResult Normalization;
    internal CddlCompileOptions Options;
    internal CddlSemanticResult Result = new CddlSemanticResult();
    internal List<CddlNormalizedNode> ReportedNodes =
        new List<CddlNormalizedNode>();
    internal List<string> ReportedCodes = new List<string>();
    internal List<CddlNormalizedRule> ActiveRules =
        new List<CddlNormalizedRule>();
    internal List<int> ActiveContexts = new List<int>();
    internal List<int> ActiveOptionalStates = new List<int>();

    internal CddlSemanticChecker(
        CddlNormalizationResult normalization,
        CddlCompileOptions options)
    {
        Normalization = normalization;
        Options = options;
    }

    internal CddlSemanticResult CheckDocument()
    {
        int index = 0;
        while (index < Normalization.Rules.Count)
        {
            CddlNormalizedRule rule = Normalization.Rules[index];
            int context = rule.Kind == CddlRuleKind.Group
                ? ContextGroup
                : ContextType;
            VisitRule(rule, context, OptionalUnknown);
            index = index + 1;
        }
        if (Normalization.RootRule != null)
        {
            VisitRule(
                Normalization.RootRule,
                ContextType,
                OptionalNo
            );
        }
        return Result;
    }

    internal void VisitRule(
        CddlNormalizedRule rule,
        int context,
        int optionalState)
    {
        if (IsActiveRuleVisit(rule, context, optionalState))
        {
            return;
        }
        ActiveRules.Add(rule);
        ActiveContexts.Add(context);
        ActiveOptionalStates.Add(optionalState);
        if (rule.Value != null)
        {
            VisitNode(rule.Value, context, optionalState, rule.Name);
        }
        int last = ActiveRules.Count - 1;
        ActiveRules.RemoveAt(last);
        ActiveContexts.RemoveAt(last);
        ActiveOptionalStates.RemoveAt(last);
    }

    internal bool IsActiveRuleVisit(
        CddlNormalizedRule rule,
        int context,
        int optionalState)
    {
        int index = 0;
        while (index < ActiveRules.Count)
        {
            if (ActiveRules[index] == rule
                && ActiveContexts[index] == context
                && ActiveOptionalStates[index] == optionalState)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void VisitNode(
        CddlNormalizedNode node,
        int context,
        int optionalState,
        string containingRule)
    {
        ValidateContext(node, context, containingRule);
        if (node.Kind == CddlNodeKind.Range)
        {
            ValidateRange(node, containingRule);
        }
        else if (node.Kind == CddlNodeKind.Control)
        {
            ValidateControl(node, optionalState, containingRule);
        }
        else if (node.Kind == CddlNodeKind.GroupEntry)
        {
            ValidateOccurrence(node, containingRule);
            ValidateMapEntry(node, context, containingRule);
        }
        else if (node.Kind == CddlNodeKind.Unwrap)
        {
            ValidateUnwrap(node, context, containingRule);
        }
        else if (node.Kind == CddlNodeKind.GroupEnumeration)
        {
            ValidateGroupEnumeration(node, containingRule);
        }
        else if (node.Kind == CddlNodeKind.Tag)
        {
            ValidateTag(node, containingRule);
        }
        else if (node.Kind == CddlNodeKind.Representation)
        {
            ValidateRepresentation(node, containingRule);
        }

        if (node.ReferencedRule != null)
        {
            int targetContext = ContextType;
            if (node.Kind == CddlNodeKind.GroupEnumeration)
            {
                targetContext = ContextGroup;
            }
            else if (node.ReferenceKind == CddlRuleKind.Group)
            {
                targetContext = context;
            }
            VisitRule(node.ReferencedRule, targetContext, optionalState);
            return;
        }
        VisitChildren(node, context, optionalState, containingRule);
    }

    internal void VisitChildren(
        CddlNormalizedNode node,
        int context,
        int optionalState,
        string containingRule)
    {
        if (node.Kind == CddlNodeKind.Array
            || node.Kind == CddlNodeKind.Map)
        {
            int groupContext = node.Kind == CddlNodeKind.Map
                ? ContextMapGroup
                : ContextArrayGroup;
            VisitChildList(
                node.Children,
                groupContext,
                optionalState,
                containingRule
            );
            return;
        }
        if (node.Kind == CddlNodeKind.Group
            || node.Kind == CddlNodeKind.GroupChoice)
        {
            VisitChildList(
                node.Children,
                context,
                optionalState,
                containingRule
            );
            return;
        }
        if (node.Kind == CddlNodeKind.GroupEntry)
        {
            VisitGroupEntryChildren(node, context, containingRule);
            return;
        }
        if (node.Kind == CddlNodeKind.Tag)
        {
            int contentIndex = node.Children.Count - 1;
            int index = 0;
            while (index < node.Children.Count)
            {
                VisitNode(
                    node.Children[index],
                    index == contentIndex ? ContextType : ContextHeadNumber,
                    optionalState,
                    containingRule
                );
                index = index + 1;
            }
            return;
        }
        if (node.Kind == CddlNodeKind.Representation)
        {
            VisitChildList(
                node.Children,
                ContextHeadNumber,
                optionalState,
                containingRule
            );
            return;
        }
        if (node.Kind == CddlNodeKind.GroupEnumeration)
        {
            VisitChildList(
                node.Children,
                ContextGroup,
                optionalState,
                containingRule
            );
            return;
        }
        VisitChildList(
            node.Children,
            ContextType,
            optionalState,
            containingRule
        );
    }

    internal void VisitGroupEntryChildren(
        CddlNormalizedNode entry,
        int context,
        string containingRule)
    {
        int valueOptional = entry.MinimumOccurrence == 0
            ? OptionalYes
            : OptionalNo;
        if (entry.Children.Count == 2)
        {
            VisitNode(
                entry.Children[0],
                ContextType,
                OptionalNo,
                containingRule
            );
            VisitNode(
                entry.Children[1],
                ContextType,
                valueOptional,
                containingRule
            );
            return;
        }
        if (entry.Children.Count == 1)
        {
            CddlNormalizedNode value = entry.Children[0];
            int valueContext = (value.ReferenceKind == CddlRuleKind.Group
                    && value.Kind != CddlNodeKind.GroupEnumeration)
                || value.Kind == CddlNodeKind.Group
                || value.Kind == CddlNodeKind.GroupChoice
                ? context
                : ContextType;
            if (value.Kind == CddlNodeKind.Unwrap)
            {
                int targetKind = FindUnwrapTargetKind(
                    value.ReferencedRule,
                    new List<CddlNormalizedRule>()
                );
                if (targetKind == (int)CddlNodeKind.Array
                    || targetKind == (int)CddlNodeKind.Map)
                {
                    valueContext = context;
                }
            }
            VisitNode(
                value,
                valueContext,
                valueOptional,
                containingRule
            );
        }
    }

    internal void VisitChildList(
        List<CddlNormalizedNode> children,
        int context,
        int optionalState,
        string containingRule)
    {
        int index = 0;
        while (index < children.Count)
        {
            VisitNode(
                children[index],
                context,
                optionalState,
                containingRule
            );
            index = index + 1;
        }
    }

    internal void ValidateContext(
        CddlNormalizedNode node,
        int context,
        string containingRule)
    {
        bool groupNode = node.Kind == CddlNodeKind.Group
            || node.Kind == CddlNodeKind.GroupChoice
            || node.Kind == CddlNodeKind.GroupEntry
            || (node.ReferenceKind == CddlRuleKind.Group
                && node.Kind != CddlNodeKind.GroupEnumeration);
        bool groupContext = context == ContextGroup
            || context == ContextArrayGroup
            || context == ContextMapGroup;
        if (groupNode != groupContext
            && node.Kind != CddlNodeKind.Unwrap)
        {
            ReportDiagnostic(
                node,
                "CDDL_COMPILE_CONTEXT_KIND_MISMATCH",
                "A type or group is used in an incompatible context.",
                containingRule
            );
        }
    }

    internal void ValidateRange(
        CddlNormalizedNode node,
        string containingRule)
    {
        if (node.Children.Count != 2)
        {
            ReportDiagnostic(
                node,
                "CDDL_COMPILE_RANGE_ENDPOINT_INVALID",
                "A range requires two numeric singleton endpoints.",
                containingRule
            );
            return;
        }
        CddlNumericEndpoint lower = GetNumericEndpoint(
            node.Children[0],
            new List<CddlNormalizedRule>()
        );
        CddlNumericEndpoint upper = GetNumericEndpoint(
            node.Children[1],
            new List<CddlNormalizedRule>()
        );
        if (lower.Kind == 0 || upper.Kind == 0)
        {
            ReportDiagnostic(
                node,
                "CDDL_COMPILE_RANGE_ENDPOINT_INVALID",
                "Range endpoints must each denote one integer or floating-point value.",
                containingRule
            );
        }
        else if (lower.Kind != upper.Kind)
        {
            ReportDiagnostic(
                node,
                "CDDL_COMPILE_RANGE_KIND_MISMATCH",
                "Both range endpoints must use the same numeric kind.",
                containingRule
            );
        }
        else if (lower.Kind == 2
            && (double.IsNaN(lower.FloatValue)
                || double.IsNaN(upper.FloatValue)))
        {
            ReportDiagnostic(
                node,
                "CDDL_COMPILE_RANGE_ENDPOINT_INVALID",
                "A floating-point range endpoint cannot be NaN.",
                containingRule
            );
        }
    }

    internal CddlNumericEndpoint GetNumericEndpoint(
        CddlNormalizedNode node,
        List<CddlNormalizedRule> visited)
    {
        CddlNumericEndpoint endpoint = new CddlNumericEndpoint();
        if (node.Kind == CddlNodeKind.Literal)
        {
            if (node.HasIntegerValue)
            {
                endpoint.Kind = 1;
                endpoint.IntegerValue = node.IntegerValue;
            }
            else if (node.HasFloatValue)
            {
                endpoint.Kind = 2;
                endpoint.FloatValue = node.FloatValue;
            }
            return endpoint;
        }
        if (node.ReferencedRule != null
            && !ContainsRule(visited, node.ReferencedRule)
            && node.ReferencedRule.Value != null)
        {
            visited.Add(node.ReferencedRule);
            CddlNumericEndpoint referenced = GetNumericEndpoint(
                node.ReferencedRule.Value,
                visited
            );
            visited.RemoveAt(visited.Count - 1);
            return referenced;
        }
        if (node.Kind == CddlNodeKind.TypeChoice
            && node.Children.Count == 1)
        {
            return GetNumericEndpoint(node.Children[0], visited);
        }
        if (node.Kind == CddlNodeKind.Control
            && node.ControlName == CddlControlNames.Plus
            && node.Children.Count == 2)
        {
            CddlNumericEndpoint target = GetNumericEndpoint(
                node.Children[0],
                visited
            );
            CddlNumericEndpoint controller = GetNumericEndpoint(
                node.Children[1],
                visited
            );
            return AddNumericEndpoints(target, controller);
        }
        return endpoint;
    }

    internal CddlNumericEndpoint AddNumericEndpoints(
        CddlNumericEndpoint target,
        CddlNumericEndpoint controller)
    {
        CddlNumericEndpoint result = new CddlNumericEndpoint();
        if (target.Kind == 1 && controller.Kind == 1)
        {
            result.Kind = 1;
            result.IntegerValue = target.IntegerValue
                + controller.IntegerValue;
        }
        else if (target.Kind == 1 && controller.Kind == 2)
        {
            if (double.IsNaN(controller.FloatValue)
                || double.IsInfinity(controller.FloatValue))
            {
                return result;
            }
            result.Kind = 1;
            result.IntegerValue = target.IntegerValue
                + new BigInteger(Math.Floor(controller.FloatValue));
        }
        else if (target.Kind == 2 && controller.Kind == 1)
        {
            result.Kind = 2;
            result.FloatValue = target.FloatValue
                + (double)controller.IntegerValue;
        }
        else if (target.Kind == 2 && controller.Kind == 2)
        {
            result.Kind = 2;
            result.FloatValue = target.FloatValue + controller.FloatValue;
        }
        return result;
    }

    internal void ValidateControl(
        CddlNormalizedNode node,
        int optionalState,
        string containingRule)
    {
        if (node.Children.Count != 2)
        {
            ReportDiagnostic(
                node,
                "CDDL_COMPILE_CONTROL_OPERAND_INVALID",
                "A control operator requires target and controller operands.",
                containingRule
            );
            return;
        }
        string name = node.ControlName ?? "";
        int unknownPolicy = (int)Options.UnknownControlPolicy;
        if (unknownPolicy < (int)CddlUnknownControlPolicy.Reject
            || unknownPolicy > (int)CddlUnknownControlPolicy.Ignore)
        {
            ReportDiagnostic(
                node,
                "CDDL_COMPILE_UNKNOWN_CONTROL_POLICY_INVALID",
                "The unknown-control compilation policy is not defined.",
                containingRule
            );
            return;
        }
        if (!IsKnownControl(name))
        {
            if (Options.UnknownControlPolicy
                == CddlUnknownControlPolicy.Ignore)
            {
                return;
            }
            ReportDiagnostic(
                node,
                "CDDL_COMPILE_CONTROL_UNKNOWN",
                "The control operator is not registered.",
                containingRule
            );
            return;
        }
        int target = GetTypeMask(
            node.Children[0],
            new List<CddlNormalizedRule>()
        );
        int controller = GetTypeMask(
            node.Children[1],
            new List<CddlNormalizedRule>()
        );
        ValidateKnownControlOperands(
            node,
            name,
            target,
            controller,
            optionalState,
            containingRule
        );
    }

    internal bool IsKnownControl(string name)
    {
        int index = 0;
        while (index < CddlControlNames.RegisteredNames.Length)
        {
            if (CddlControlNames.RegisteredNames[index] == name)
            {
                return true;
            }
            index = index + 1;
        }
        index = 0;
        while (index < Options.ControlRegistrations.Count)
        {
            CddlControlRegistration registration =
                Options.ControlRegistrations[index];
            if (registration.Name == name
                && registration.IsEnabled
                && registration.Handler != null)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void ValidateKnownControlOperands(
        CddlNormalizedNode node,
        string name,
        int target,
        int controller,
        int optionalState,
        string containingRule)
    {
        int numeric = TypeInteger | TypeFloat;
        int strings = TypeBytes | TypeText;
        if (name == CddlControlNames.Size)
        {
            ValidateControlMasks(
                node,
                target,
                controller,
                TypeInteger | strings,
                TypeInteger,
                containingRule
            );
            int signs = GetIntegerSignMask(
                node.Children[0],
                new List<CddlNormalizedRule>()
            );
            if ((signs & SignNegative) != 0)
            {
                ReportControlTarget(node, containingRule);
            }
            int controllerSigns = GetIntegerSignMask(
                node.Children[1],
                new List<CddlNormalizedRule>()
            );
            if ((controllerSigns & SignNegative) != 0)
            {
                ReportDiagnostic(
                    node,
                    "CDDL_COMPILE_CONTROL_CONTROLLER_INVALID",
                    "A size controller cannot include negative integers.",
                    containingRule
                );
            }
        }
        else if (name == CddlControlNames.Bits)
        {
            ValidateControlMasks(
                node,
                target,
                controller,
                TypeInteger | TypeBytes,
                TypeInteger,
                containingRule
            );
            int targetSigns = GetIntegerSignMask(
                node.Children[0],
                new List<CddlNormalizedRule>()
            );
            if ((targetSigns & SignNegative) != 0)
            {
                ReportControlTarget(node, containingRule);
            }
            int controllerSigns = GetIntegerSignMask(
                node.Children[1],
                new List<CddlNormalizedRule>()
            );
            if ((controllerSigns & SignNegative) != 0)
            {
                ReportDiagnostic(
                    node,
                    "CDDL_COMPILE_CONTROL_CONTROLLER_INVALID",
                    "Bit positions cannot include negative integers.",
                    containingRule
                );
            }
        }
        else if (name == CddlControlNames.RegularExpression)
        {
            ValidateControlMasks(
                node,
                target,
                controller,
                TypeText,
                TypeText,
                containingRule
            );
            ValidateSingletonController(node, containingRule);
        }
        else if (name == CddlControlNames.Cbor
            || name == CddlControlNames.CborSequence)
        {
            ValidateControlMasks(
                node,
                target,
                controller,
                TypeBytes,
                TypeAny,
                containingRule
            );
        }
        else if (name == CddlControlNames.LessThan
            || name == CddlControlNames.LessThanOrEqual
            || name == CddlControlNames.GreaterThan
            || name == CddlControlNames.GreaterThanOrEqual)
        {
            ValidateControlMasks(
                node,
                target,
                controller,
                numeric,
                numeric,
                containingRule
            );
            ValidateSingletonController(node, containingRule);
        }
        else if (name == CddlControlNames.Equal
            || name == CddlControlNames.NotEqual
            || name == CddlControlNames.Default)
        {
            ValidateSingletonController(node, containingRule);
            if (name == CddlControlNames.Default
                && optionalState == OptionalNo)
            {
                ReportDiagnostic(
                    node,
                    "CDDL_COMPILE_DEFAULT_CONTEXT_INVALID",
                    "The .default control is only meaningful in an optional entry.",
                    containingRule
                );
            }
        }
        else if (name == CddlControlNames.Plus)
        {
            ValidateControlMasks(
                node,
                target,
                controller,
                numeric,
                numeric,
                containingRule
            );
        }
        else if (name == CddlControlNames.Concatenate
            || name == CddlControlNames.DedentedConcatenate)
        {
            ValidateControlMasks(
                node,
                target,
                controller,
                strings,
                strings,
                containingRule
            );
        }
        else if (name == CddlControlNames.Abnf
            || name == CddlControlNames.AbnfBytes)
        {
            ValidateControlMasks(
                node,
                target,
                controller,
                strings,
                strings,
                containingRule
            );
            ValidateSingletonController(node, containingRule);
        }
        else if (name == CddlControlNames.Feature)
        {
            ValidateControlMasks(
                node,
                target,
                controller,
                TypeAny,
                TypeText | TypeArray,
                containingRule
            );
            if (TypeMaskFits(controller, TypeText | TypeArray))
            {
                ValidateFeatureController(node, containingRule);
            }
        }
        else if (IsByteEncodingControl(name))
        {
            ValidateControlMasks(
                node,
                target,
                controller,
                TypeText,
                TypeBytes,
                containingRule
            );
        }
        else if (name == CddlControlNames.Base10)
        {
            ValidateControlMasks(
                node,
                target,
                controller,
                TypeText,
                TypeInteger,
                containingRule
            );
        }
        else if (name == CddlControlNames.Printf
            || name == CddlControlNames.Json)
        {
            int expectedController = name == CddlControlNames.Printf
                ? TypeArray
                : TypeAny;
            ValidateControlMasks(
                node,
                target,
                controller,
                TypeText,
                expectedController,
                containingRule
            );
        }
        else if (name == CddlControlNames.Join)
        {
            ValidateControlMasks(
                node,
                target,
                controller,
                strings,
                TypeArray,
                containingRule
            );
        }
    }

    internal bool IsByteEncodingControl(string name)
    {
        return name == CddlControlNames.Base64Url
            || name == CddlControlNames.Base64UrlSloppy
            || name == CddlControlNames.Base64Classic
            || name == CddlControlNames.Base64ClassicSloppy
            || name == CddlControlNames.Base45
            || name == CddlControlNames.Base32
            || name == CddlControlNames.Base32Hex
            || name == CddlControlNames.Hex
            || name == CddlControlNames.HexLower
            || name == CddlControlNames.HexUpper;
    }

    internal void ValidateControlMasks(
        CddlNormalizedNode node,
        int target,
        int controller,
        int allowedTarget,
        int allowedController,
        string containingRule)
    {
        if (!TypeMaskFits(target, allowedTarget))
        {
            ReportControlTarget(node, containingRule);
        }
        if (!TypeMaskFits(controller, allowedController))
        {
            ReportDiagnostic(
                node,
                "CDDL_COMPILE_CONTROL_CONTROLLER_INVALID",
                "The control controller has an incompatible type.",
                containingRule
            );
        }
    }

    internal bool TypeMaskFits(int mask, int allowed)
    {
        return mask == 0 || (mask & ~allowed) == 0;
    }

    internal void ReportControlTarget(
        CddlNormalizedNode node,
        string containingRule)
    {
        ReportDiagnostic(
            node,
            "CDDL_COMPILE_CONTROL_TARGET_INVALID",
            "The control target has an incompatible type.",
            containingRule
        );
    }

    internal void ValidateSingletonController(
        CddlNormalizedNode node,
        string containingRule)
    {
        if (node.Children.Count == 2
            && !IsSingletonType(
                node.Children[1],
                new List<CddlNormalizedRule>()))
        {
            ReportDiagnostic(
                node,
                "CDDL_COMPILE_CONTROL_CONTROLLER_INVALID",
                "The control controller must denote one value.",
                containingRule
            );
        }
    }

    internal void ValidateFeatureController(
        CddlNormalizedNode node,
        string containingRule)
    {
        if (node.Children.Count != 2
            || !IsFeatureController(
                node.Children[1],
                new List<CddlNormalizedRule>()))
        {
            ReportDiagnostic(
                node,
                "CDDL_COMPILE_CONTROL_CONTROLLER_INVALID",
                "A feature controller must be a singleton text string or "
                    + "a singleton [text-name, detail] array.",
                containingRule
            );
        }
    }

    internal bool IsFeatureController(
        CddlNormalizedNode node,
        List<CddlNormalizedRule> visited)
    {
        if (node.ReferencedRule != null)
        {
            if (node.ReferencedRule.Value == null
                || ContainsRule(visited, node.ReferencedRule))
            {
                return false;
            }
            visited.Add(node.ReferencedRule);
            bool referenced = IsFeatureController(
                node.ReferencedRule.Value,
                visited
            );
            visited.RemoveAt(visited.Count - 1);
            return referenced;
        }
        if ((node.Kind == CddlNodeKind.ParenthesizedType
                || node.Kind == CddlNodeKind.TypeChoice)
            && node.Children.Count == 1)
        {
            return IsFeatureController(node.Children[0], visited);
        }
        int type = GetTypeMask(node, new List<CddlNormalizedRule>());
        if (TypeMaskFits(type, TypeText) && type != 0)
        {
            return IsSingletonType(node, visited);
        }
        if (node.Kind != CddlNodeKind.Array || node.Children.Count != 1)
        {
            return false;
        }
        CddlNormalizedNode? group = FindFeatureGroup(node.Children[0]);
        if (group == null || group.Children.Count != 2)
        {
            return false;
        }
        CddlNormalizedNode? name = FeatureControllerEntry(
            group.Children[0]
        );
        CddlNormalizedNode? detail = FeatureControllerEntry(
            group.Children[1]
        );
        if (name == null || detail == null)
        {
            return false;
        }
        int nameType = GetTypeMask(
            name,
            new List<CddlNormalizedRule>()
        );
        return TypeMaskFits(nameType, TypeText)
            && nameType != 0
            && IsSingletonType(name, visited)
            && IsSingletonType(detail, visited);
    }

    internal CddlNormalizedNode? FindFeatureGroup(CddlNormalizedNode node)
    {
        CddlNormalizedNode current = node;
        while ((current.Kind == CddlNodeKind.ParenthesizedGroup
                || current.Kind == CddlNodeKind.GroupChoice)
            && current.Children.Count == 1)
        {
            current = current.Children[0];
        }
        return current.Kind == CddlNodeKind.Group ? current : null;
    }

    internal CddlNormalizedNode? FeatureControllerEntry(
        CddlNormalizedNode entry)
    {
        if (entry.Kind != CddlNodeKind.GroupEntry
            || entry.MinimumOccurrence != 1
            || entry.MaximumOccurrence != 1
            || entry.Children.Count == 0)
        {
            return null;
        }
        return entry.Children[entry.Children.Count - 1];
    }

    internal bool IsSingletonType(
        CddlNormalizedNode node,
        List<CddlNormalizedRule> visited)
    {
        if (node.Kind == CddlNodeKind.Literal)
        {
            return true;
        }
        if (node.ReferencedRule != null
            && !ContainsRule(visited, node.ReferencedRule)
            && node.ReferencedRule.Value != null)
        {
            visited.Add(node.ReferencedRule);
            bool referenced = IsSingletonType(
                node.ReferencedRule.Value,
                visited
            );
            visited.RemoveAt(visited.Count - 1);
            return referenced;
        }
        if ((node.Kind == CddlNodeKind.TypeChoice
                || node.Kind == CddlNodeKind.ParenthesizedType)
            && node.Children.Count == 1)
        {
            return IsSingletonType(node.Children[0], visited);
        }
        if (node.Kind == CddlNodeKind.Control
            && node.Children.Count == 2
            && (node.ControlName == CddlControlNames.Plus
                || node.ControlName == CddlControlNames.Concatenate
                || node.ControlName
                    == CddlControlNames.DedentedConcatenate))
        {
            return IsSingletonType(node.Children[0], visited)
                && IsSingletonType(node.Children[1], visited);
        }
        if (node.Kind == CddlNodeKind.Range && node.Children.Count == 2)
        {
            CddlNumericEndpoint lower = GetNumericEndpoint(
                node.Children[0],
                new List<CddlNormalizedRule>()
            );
            CddlNumericEndpoint upper = GetNumericEndpoint(
                node.Children[1],
                new List<CddlNormalizedRule>()
            );
            if (node.IsRangeUpperExclusive || lower.Kind != upper.Kind)
            {
                return false;
            }
            if (lower.Kind == 1)
            {
                return lower.IntegerValue == upper.IntegerValue;
            }
            if (lower.Kind == 2)
            {
                return lower.FloatValue == upper.FloatValue;
            }
        }
        if (node.Kind == CddlNodeKind.Tag && node.Children.Count > 0)
        {
            return node.HasArgumentValue
                && IsSingletonType(
                    node.Children[node.Children.Count - 1],
                    visited
                );
        }
        if (node.Kind == CddlNodeKind.Representation)
        {
            return IsSingletonRepresentation(node);
        }
        if ((node.Kind == CddlNodeKind.Array
                || node.Kind == CddlNodeKind.Map)
            && node.Children.Count == 1)
        {
            return IsSingletonGroup(
                node.Children[0],
                node.Kind == CddlNodeKind.Map,
                visited
            );
        }
        return false;
    }

    internal bool IsSingletonRepresentation(CddlNormalizedNode node)
    {
        if (!node.HasArgumentValue || node.ArgumentValue.Sign < 0)
        {
            return false;
        }
        if (node.MajorType == 0 || node.MajorType == 1)
        {
            return node.ArgumentValue <= new BigInteger(23);
        }
        if (node.MajorType == 7)
        {
            return node.ArgumentValue <= new BigInteger(23)
                || (node.ArgumentValue >= new BigInteger(32)
                    && node.ArgumentValue <= new BigInteger(255));
        }
        return false;
    }

    internal bool IsSingletonGroup(
        CddlNormalizedNode node,
        bool map,
        List<CddlNormalizedRule> visited)
    {
        if (node.ReferencedRule != null)
        {
            if (ContainsRule(visited, node.ReferencedRule)
                || node.ReferencedRule.Value == null)
            {
                return false;
            }
            visited.Add(node.ReferencedRule);
            bool referenced = IsSingletonGroup(
                node.ReferencedRule.Value,
                map,
                visited
            );
            visited.RemoveAt(visited.Count - 1);
            return referenced;
        }
        if (node.Kind == CddlNodeKind.Group
            || node.Kind == CddlNodeKind.ParenthesizedGroup)
        {
            int index = 0;
            while (index < node.Children.Count)
            {
                if (!IsSingletonGroup(node.Children[index], map, visited))
                {
                    return false;
                }
                index = index + 1;
            }
            return true;
        }
        if (node.Kind == CddlNodeKind.GroupChoice)
        {
            return node.Children.Count == 1
                && IsSingletonGroup(node.Children[0], map, visited);
        }
        if (node.Kind != CddlNodeKind.GroupEntry
            || node.MinimumOccurrence != 1
            || node.MaximumOccurrence != 1)
        {
            return false;
        }
        if (!map)
        {
            if (node.Children.Count != 1)
            {
                return false;
            }
            CddlNormalizedNode value = node.Children[0];
            if (value.ReferenceKind == CddlRuleKind.Group
                || value.Kind == CddlNodeKind.Group
                || value.Kind == CddlNodeKind.GroupChoice
                || value.Kind == CddlNodeKind.ParenthesizedGroup)
            {
                return IsSingletonGroup(value, false, visited);
            }
            return IsSingletonType(value, visited);
        }
        if (node.Name != null && node.Children.Count == 1)
        {
            return IsSingletonType(node.Children[0], visited);
        }
        if (node.Children.Count == 2)
        {
            return IsSingletonType(node.Children[0], visited)
                && IsSingletonType(node.Children[1], visited);
        }
        if (node.Children.Count == 1)
        {
            return IsSingletonGroup(node.Children[0], true, visited);
        }
        return false;
    }

    internal int GetTypeMask(
        CddlNormalizedNode node,
        List<CddlNormalizedRule> visited)
    {
        if (node.Kind == CddlNodeKind.GroupEnumeration)
        {
            return GetEnumerationTypeMask(node, visited);
        }
        if (node.ReferencedRule != null)
        {
            if (ContainsRule(visited, node.ReferencedRule)
                || node.ReferencedRule.Value == null)
            {
                return 0;
            }
            visited.Add(node.ReferencedRule);
            return GetTypeMask(node.ReferencedRule.Value, visited);
        }
        if (node.IsEmptySocket)
        {
            return 0;
        }
        if (node.Kind == CddlNodeKind.Literal)
        {
            if (node.HasIntegerValue)
            {
                return TypeInteger;
            }
            if (node.HasFloatValue)
            {
                return TypeFloat;
            }
            if (node.LiteralKind == CddlLiteralKind.TextString)
            {
                return TypeText;
            }
            return TypeBytes;
        }
        if (node.Kind == CddlNodeKind.Array)
        {
            return TypeArray;
        }
        if (node.Kind == CddlNodeKind.Map)
        {
            return TypeMap;
        }
        if (node.Kind == CddlNodeKind.Tag)
        {
            return TypeTag;
        }
        if (node.Kind == CddlNodeKind.Range && node.Children.Count > 0)
        {
            CddlNumericEndpoint endpoint = GetNumericEndpoint(
                node.Children[0],
                new List<CddlNormalizedRule>()
            );
            if (endpoint.Kind == 1)
            {
                return TypeInteger;
            }
            if (endpoint.Kind == 2)
            {
                return TypeFloat;
            }
            return 0;
        }
        if (node.Kind == CddlNodeKind.TypeChoice)
        {
            int mask = 0;
            int index = 0;
            while (index < node.Children.Count)
            {
                mask = mask | GetTypeMask(node.Children[index], visited);
                index = index + 1;
            }
            return mask;
        }
        if (node.Kind == CddlNodeKind.Control && node.Children.Count > 0)
        {
            return GetTypeMask(node.Children[0], visited);
        }
        if (node.Kind == CddlNodeKind.Representation)
        {
            return RepresentationTypeMask(node);
        }
        return 0;
    }

    internal int GetEnumerationTypeMask(
        CddlNormalizedNode node,
        List<CddlNormalizedRule> visited)
    {
        if (node.Kind == CddlNodeKind.GroupEnumeration)
        {
            if (node.ReferencedRule != null
                && node.ReferencedRule.Value != null
                && !ContainsRule(visited, node.ReferencedRule))
            {
                visited.Add(node.ReferencedRule);
                return GetEnumerationTypeMask(
                    node.ReferencedRule.Value,
                    visited
                );
            }
            if (node.Children.Count > 0)
            {
                return GetEnumerationTypeMask(node.Children[0], visited);
            }
            return 0;
        }
        if (node.Kind == CddlNodeKind.Group
            || node.Kind == CddlNodeKind.GroupChoice
            || node.Kind == CddlNodeKind.ParenthesizedGroup)
        {
            int mask = 0;
            int index = 0;
            while (index < node.Children.Count)
            {
                mask = mask | GetEnumerationTypeMask(
                    node.Children[index],
                    visited
                );
                index = index + 1;
            }
            return mask;
        }
        if (node.Kind == CddlNodeKind.GroupEntry
            && node.Children.Count > 0)
        {
            return GetEnumerationTypeMask(
                node.Children[node.Children.Count - 1],
                visited
            );
        }
        if (node.ReferencedRule != null
            && node.ReferenceKind == CddlRuleKind.Group
            && node.ReferencedRule.Value != null
            && !ContainsRule(visited, node.ReferencedRule))
        {
            visited.Add(node.ReferencedRule);
            return GetEnumerationTypeMask(
                node.ReferencedRule.Value,
                visited
            );
        }
        return GetTypeMask(node, visited);
    }

    internal int RepresentationTypeMask(CddlNormalizedNode node)
    {
        switch (node.MajorType)
        {
            case -1:
                return TypeAny;
            case 0:
            case 1:
                return TypeInteger;
            case 2:
                return TypeBytes;
            case 3:
                return TypeText;
            case 4:
                return TypeArray;
            case 5:
                return TypeMap;
            case 6:
                return TypeTag;
            case 7:
                if (node.HasArgumentValue
                    && (node.ArgumentValue == new BigInteger(25)
                        || node.ArgumentValue == new BigInteger(26)
                        || node.ArgumentValue == new BigInteger(27)))
                {
                    return TypeFloat;
                }
                return TypeSimple | TypeFloat;
            default:
                return 0;
        }
    }

    internal int GetIntegerSignMask(
        CddlNormalizedNode node,
        List<CddlNormalizedRule> visited)
    {
        if (node.ReferencedRule != null)
        {
            if (ContainsRule(visited, node.ReferencedRule)
                || node.ReferencedRule.Value == null)
            {
                return 0;
            }
            visited.Add(node.ReferencedRule);
            return GetIntegerSignMask(node.ReferencedRule.Value, visited);
        }
        if (node.Kind == CddlNodeKind.Literal && node.HasIntegerValue)
        {
            return node.IntegerValue.Sign < 0
                ? SignNegative
                : SignNonnegative;
        }
        if (node.Kind == CddlNodeKind.Range && node.Children.Count == 2)
        {
            return GetIntegerSignMask(node.Children[0], visited)
                | GetIntegerSignMask(node.Children[1], visited);
        }
        if (node.Kind == CddlNodeKind.TypeChoice)
        {
            int mask = 0;
            int index = 0;
            while (index < node.Children.Count)
            {
                mask = mask | GetIntegerSignMask(node.Children[index], visited);
                index = index + 1;
            }
            return mask;
        }
        if (node.Kind == CddlNodeKind.Representation)
        {
            if (node.MajorType == 0)
            {
                return SignNonnegative;
            }
            if (node.MajorType == 1)
            {
                return SignNegative;
            }
        }
        if (node.Kind == CddlNodeKind.Control && node.Children.Count > 0)
        {
            return GetIntegerSignMask(node.Children[0], visited);
        }
        return 0;
    }

    internal void ValidateOccurrence(
        CddlNormalizedNode node,
        string containingRule)
    {
        bool invalid = node.MinimumOccurrence < 0
            || node.MaximumOccurrence < -1
            || (node.MaximumOccurrence >= 0
                && node.MinimumOccurrence > node.MaximumOccurrence);
        if (node.Occurrence == CddlOccurrenceKind.ExactlyOne)
        {
            invalid = invalid || node.MinimumOccurrence != 1
                || node.MaximumOccurrence != 1;
        }
        else if (node.Occurrence == CddlOccurrenceKind.Optional)
        {
            invalid = invalid || node.MinimumOccurrence != 0
                || node.MaximumOccurrence != 1;
        }
        else if (node.Occurrence == CddlOccurrenceKind.ZeroOrMore)
        {
            invalid = invalid || node.MinimumOccurrence != 0
                || node.MaximumOccurrence != -1;
        }
        else if (node.Occurrence == CddlOccurrenceKind.OneOrMore)
        {
            invalid = invalid || node.MinimumOccurrence != 1
                || node.MaximumOccurrence != -1;
        }
        else if (node.Occurrence != CddlOccurrenceKind.Bounded)
        {
            invalid = true;
        }
        if (invalid)
        {
            ReportDiagnostic(
                node,
                "CDDL_COMPILE_OCCURRENCE_INVALID",
                "The occurrence bounds or occurrence kind are inconsistent.",
                containingRule
            );
        }
    }

    internal void ValidateMapEntry(
        CddlNormalizedNode node,
        int context,
        string containingRule)
    {
        if (node.IsCut
            && context != ContextMapGroup
            && context != ContextGroup)
        {
            ReportDiagnostic(
                node,
                "CDDL_COMPILE_CUT_CONTEXT_INVALID",
                "A cut can only be used by a map entry.",
                containingRule
            );
        }
        if (node.IsCut && node.Children.Count != 2)
        {
            ReportDiagnostic(
                node,
                "CDDL_COMPILE_CUT_ENTRY_INVALID",
                "A cut requires an explicit map key and value.",
                containingRule
            );
        }
        if (context == ContextMapGroup
            && node.Name == null
            && node.Children.Count == 1)
        {
            CddlNormalizedNode value = node.Children[0];
            bool groupInclusion = value.ReferenceKind == CddlRuleKind.Group
                || value.Kind == CddlNodeKind.Group
                || value.Kind == CddlNodeKind.GroupChoice;
            if (!groupInclusion)
            {
                ReportDiagnostic(
                    node,
                    "CDDL_COMPILE_MAP_LABEL_MISSING",
                    "A map entry must have a key or include a group.",
                    containingRule
                );
            }
        }
    }

    internal void ValidateUnwrap(
        CddlNormalizedNode node,
        int context,
        string containingRule)
    {
        int targetKind = FindUnwrapTargetKind(
            node.ReferencedRule,
            new List<CddlNormalizedRule>()
        );
        if (targetKind != (int)CddlNodeKind.Array
            && targetKind != (int)CddlNodeKind.Map
            && targetKind != (int)CddlNodeKind.Tag)
        {
            ReportDiagnostic(
                node,
                "CDDL_COMPILE_UNWRAP_TARGET_INVALID",
                "An unwrap target must resolve to an array, map, or tag type.",
                containingRule
            );
            return;
        }
        bool exposesGroup = targetKind == (int)CddlNodeKind.Array
            || targetKind == (int)CddlNodeKind.Map;
        bool groupContext = context == ContextGroup
            || context == ContextArrayGroup
            || context == ContextMapGroup;
        if (exposesGroup != groupContext)
        {
            ReportDiagnostic(
                node,
                "CDDL_COMPILE_CONTEXT_KIND_MISMATCH",
                "The unwrapped target exposes a type or group in the wrong context.",
                containingRule
            );
        }
    }

    internal int FindUnwrapTargetKind(
        CddlNormalizedRule? rule,
        List<CddlNormalizedRule> visited)
    {
        if (rule == null || rule.Value == null || ContainsRule(visited, rule))
        {
            return 0;
        }
        visited.Add(rule);
        CddlNormalizedNode value = rule.Value;
        if (value.ReferencedRule != null)
        {
            return FindUnwrapTargetKind(value.ReferencedRule, visited);
        }
        return (int)value.Kind;
    }

    internal void ValidateGroupEnumeration(
        CddlNormalizedNode node,
        string containingRule)
    {
        bool valid = node.ReferenceKind == CddlRuleKind.Group;
        if (node.Name == null && node.Children.Count == 1)
        {
            CddlNodeKind kind = node.Children[0].Kind;
            valid = kind == CddlNodeKind.Group
                || kind == CddlNodeKind.GroupChoice
                || kind == CddlNodeKind.GroupEntry;
        }
        if (!valid)
        {
            ReportDiagnostic(
                node,
                "CDDL_COMPILE_GROUP_ENUM_TARGET_INVALID",
                "A group enumeration requires a group target.",
                containingRule
            );
        }
    }

    internal void ValidateTag(
        CddlNormalizedNode node,
        string containingRule)
    {
        bool invalid = node.MajorType != 6 || node.Children.Count == 0;
        if (node.HasArgumentValue)
        {
            invalid = invalid || node.ArgumentValue.Sign < 0
                || node.ArgumentValue > new BigInteger(ulong.MaxValue)
                || node.Children.Count != 1;
        }
        else if (node.Children.Count == 2)
        {
            CddlIntegerDomain domain = GetIntegerDomain(
                node.Children[0],
                new List<CddlNormalizedRule>()
            );
            invalid = invalid || !domain.IsKnown
                || domain.Minimum.Sign < 0
                || domain.Maximum > new BigInteger(ulong.MaxValue);
        }
        else if (node.Children.Count != 1)
        {
            invalid = true;
        }
        if (invalid)
        {
            ReportDiagnostic(
                node,
                "CDDL_COMPILE_TAG_ARGUMENT_INVALID",
                "A tag number must be an unsigned 64-bit integer type and the tag must have content.",
                containingRule
            );
        }
    }

    internal void ValidateRepresentation(
        CddlNormalizedNode node,
        string containingRule)
    {
        bool invalid = node.MajorType < -1 || node.MajorType > 7;
        if (node.MajorType == -1
            && (node.HasArgumentValue || node.Children.Count != 0))
        {
            invalid = true;
        }
        if (node.Children.Count > 0)
        {
            if (node.MajorType != 7 || node.Children.Count != 1)
            {
                invalid = true;
            }
            else
            {
                CddlIntegerDomain domain = GetIntegerDomain(
                    node.Children[0],
                    new List<CddlNormalizedRule>()
                );
                invalid = invalid || !domain.IsKnown
                    || domain.Minimum.Sign < 0
                    || domain.Maximum > new BigInteger(255);
            }
        }
        if (node.HasArgumentValue
            && !IsValidRepresentationArgument(
                node.MajorType,
                node.ArgumentValue))
        {
            invalid = true;
        }
        if (invalid)
        {
            ReportDiagnostic(
                node,
                "CDDL_COMPILE_REPRESENTATION_ARGUMENT_INVALID",
                "The major type or representation argument is not legal CBOR.",
                containingRule
            );
        }
    }

    internal bool IsValidRepresentationArgument(
        int majorType,
        BigInteger argument)
    {
        if (argument.Sign < 0)
        {
            return false;
        }
        if (majorType == 7)
        {
            if (argument > new BigInteger(255))
            {
                return false;
            }
            return argument < new BigInteger(28)
                || argument > new BigInteger(31);
        }
        if (argument > new BigInteger(31)
            || argument == new BigInteger(28)
            || argument == new BigInteger(29)
            || argument == new BigInteger(30))
        {
            return false;
        }
        if (argument == new BigInteger(31))
        {
            return majorType >= 2 && majorType <= 5;
        }
        return majorType >= 0 && majorType <= 6;
    }

    internal CddlIntegerDomain GetIntegerDomain(
        CddlNormalizedNode node,
        List<CddlNormalizedRule> visited)
    {
        CddlIntegerDomain domain = new CddlIntegerDomain();
        if (node.Kind == CddlNodeKind.Literal && node.HasIntegerValue)
        {
            domain.IsKnown = true;
            domain.Minimum = node.IntegerValue;
            domain.Maximum = node.IntegerValue;
            return domain;
        }
        if (node.ReferencedRule != null
            && !ContainsRule(visited, node.ReferencedRule)
            && node.ReferencedRule.Value != null)
        {
            visited.Add(node.ReferencedRule);
            return GetIntegerDomain(node.ReferencedRule.Value, visited);
        }
        if (node.Kind == CddlNodeKind.Range && node.Children.Count == 2)
        {
            CddlNumericEndpoint lower = GetNumericEndpoint(
                node.Children[0],
                new List<CddlNormalizedRule>()
            );
            CddlNumericEndpoint upper = GetNumericEndpoint(
                node.Children[1],
                new List<CddlNormalizedRule>()
            );
            if (lower.Kind == 1 && upper.Kind == 1)
            {
                domain.IsKnown = true;
                domain.Minimum = lower.IntegerValue;
                domain.Maximum = upper.IntegerValue;
            }
            return domain;
        }
        if (node.Kind == CddlNodeKind.TypeChoice
            && node.Children.Count > 0)
        {
            int index = 0;
            while (index < node.Children.Count)
            {
                CddlIntegerDomain child = GetIntegerDomain(
                    node.Children[index],
                    visited
                );
                if (!child.IsKnown)
                {
                    return new CddlIntegerDomain();
                }
                if (!domain.IsKnown)
                {
                    domain.IsKnown = true;
                    domain.Minimum = child.Minimum;
                    domain.Maximum = child.Maximum;
                }
                else
                {
                    if (child.Minimum < domain.Minimum)
                    {
                        domain.Minimum = child.Minimum;
                    }
                    if (child.Maximum > domain.Maximum)
                    {
                        domain.Maximum = child.Maximum;
                    }
                }
                index = index + 1;
            }
            return domain;
        }
        if (node.Kind == CddlNodeKind.Representation && node.MajorType == 0)
        {
            domain.IsKnown = true;
            domain.Minimum = BigInteger.Zero;
            domain.Maximum = new BigInteger(ulong.MaxValue);
        }
        return domain;
    }

    internal bool ContainsRule(
        List<CddlNormalizedRule> rules,
        CddlNormalizedRule rule)
    {
        int index = 0;
        while (index < rules.Count)
        {
            if (rules[index] == rule)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void ReportDiagnostic(
        CddlNormalizedNode node,
        string code,
        string message,
        string containingRule)
    {
        if (WasReported(node, code))
        {
            return;
        }
        ReportedNodes.Add(node);
        ReportedCodes.Add(code);
        Result.HasErrors = true;
        if (Options.MaxDiagnostics <= 0
            || Result.Diagnostics.Count >= Options.MaxDiagnostics)
        {
            return;
        }
        CddlDiagnostic diagnostic = new CddlDiagnostic();
        diagnostic.Severity = CddlDiagnosticSeverity.Error;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Span = node.Span;
        diagnostic.RuleName = containingRule;
        Result.Diagnostics.Add(diagnostic);
    }

    internal bool WasReported(CddlNormalizedNode node, string code)
    {
        int index = 0;
        while (index < ReportedNodes.Count)
        {
            if (ReportedNodes[index] == node && ReportedCodes[index] == code)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }
}
