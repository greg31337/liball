using System.Collections.Generic;
using System.Numerics;

namespace cbor;

internal class CddlControlGenerator
{
    internal CddlValueGeneratorCore Core;
    internal CddlGenerationState State;
    internal string ValidationRuleName = "__generated_control";

    internal CddlControlGenerator(CddlValueGeneratorCore core)
    {
        Core = core;
        State = core.State;
    }

    internal CborValue? GenerateControlValue(CddlNode node)
    {
        if (node.Children.Count != 2 || node.ControlName == null)
        {
            State.FailGeneration(
                "CDDL_GENERATION_CONTROL_INVALID",
                "A generated control requires a name, target, and controller.",
                node
            );
            return null;
        }
        string name = node.ControlName;
        bool custom = !IsStandardControl(name);
        if (custom)
        {
            CborValue? registered = GenerateRegisteredControl(node);
            if (registered != null || State.Result.ErrorCode != null)
            {
                return registered;
            }
        }
        CddlRule? currentRule = Core.CurrentRule();
        if (currentRule == null || currentRule.Value != node)
        {
            CborValue? supplied = Core.GenerateHookValue(
                node,
                currentRule
            );
            if (supplied != null || State.Result.ErrorCode != null)
            {
                return supplied;
            }
        }
        if (name == CddlControlNames.Plus
            || name == CddlControlNames.Concatenate
            || name == CddlControlNames.DedentedConcatenate)
        {
            return GenerateComputedControl(node);
        }
        if (name == CddlControlNames.Size)
        {
            return GenerateSizeControl(node);
        }
        if (name == CddlControlNames.Bits)
        {
            return GenerateBitsControl(node);
        }
        if (name == CddlControlNames.Cbor
            || name == CddlControlNames.CborSequence)
        {
            return GenerateEmbeddedControl(node);
        }
        if (name == CddlControlNames.Within
            || name == CddlControlNames.And
            || name == CddlControlNames.Equal
            || name == CddlControlNames.NotEqual
            || name == CddlControlNames.Default)
        {
            return GenerateIntersectionControl(node);
        }
        if (name == CddlControlNames.LessThan
            || name == CddlControlNames.LessThanOrEqual
            || name == CddlControlNames.GreaterThan
            || name == CddlControlNames.GreaterThanOrEqual)
        {
            return GenerateRelationalControl(node);
        }
        if (name == CddlControlNames.Feature)
        {
            return GenerateFeatureControl(node);
        }
        if (IsConstructiveTextControl(name))
        {
            CborValue? text = new CddlTextControlGenerator(
                this
            ).GenerateTextControlValue(node);
            if (text != null)
            {
                return text;
            }
            if (State.RemainingSteps <= 0
                || !State.HasAttemptBudget(node))
            {
                return null;
            }
            State.ClearFailure();
            return GenerateHookControl(node, false);
        }
        return GenerateHookControl(node, custom);
    }

    internal CborValue? GenerateComputedControl(CddlNode node)
    {
        CborValue? target = Core.GenerateNodeValue(node.Children[0]);
        CborValue? controller = Core.GenerateNodeValue(node.Children[1]);
        if (target == null || controller == null)
        {
            return null;
        }
        if (node.ControlName == CddlControlNames.Plus)
        {
            return CreateNumericSum(target, controller, node);
        }
        return CreateStringConcatenation(
            target,
            controller,
            node.ControlName == CddlControlNames.DedentedConcatenate,
            node
        );
    }

    internal CborValue? CreateNumericSum(
        CborValue target,
        CborValue controller,
        CddlNode node)
    {
        bool targetInteger = IsIntegerControlValue(target);
        bool controllerInteger = IsIntegerControlValue(controller);
        bool targetFloat = target.Kind == CborValueKind.Float
            && target.FloatValue != null;
        bool controllerFloat = controller.Kind == CborValueKind.Float
            && controller.FloatValue != null;
        if (targetInteger && controllerInteger)
        {
            return Core.CreateIntegerValue(
                target.IntegerValue + controller.IntegerValue
            );
        }
        if (targetInteger && controllerFloat)
        {
            double adjustment = controller.FloatValue?.Value
                ?? double.NaN;
            if (!double.IsNaN(adjustment)
                && !double.IsInfinity(adjustment))
            {
                return Core.CreateIntegerValue(
                    target.IntegerValue
                        + new BigInteger(System.Math.Floor(adjustment))
                );
            }
        }
        if (targetFloat && controllerInteger)
        {
            double value = target.FloatValue?.Value ?? double.NaN;
            return Core.CreateFloatValue(
                value + (double)controller.IntegerValue,
                CborFloatWidth.Double
            );
        }
        if (targetFloat && controllerFloat)
        {
            double left = target.FloatValue?.Value ?? double.NaN;
            double right = controller.FloatValue?.Value ?? double.NaN;
            return Core.CreateFloatValue(
                left + right,
                CborFloatWidth.Double
            );
        }
        State.FailGeneration(
            "CDDL_GENERATION_CONTROL_NOT_CONSTRUCTIVE",
            "The .plus operands do not produce compatible numbers.",
            node
        );
        return null;
    }

    internal CborValue? CreateStringConcatenation(
        CborValue target,
        CborValue controller,
        bool dedent,
        CddlNode node)
    {
        byte[]? targetBytes = FindStringBytes(target);
        byte[]? controllerBytes = FindStringBytes(controller);
        if (targetBytes == null || controllerBytes == null)
        {
            State.FailGeneration(
                "CDDL_GENERATION_CONTROL_NOT_CONSTRUCTIVE",
                "The concatenation operands are not strings.",
                node
            );
            return null;
        }
        if (dedent)
        {
            targetBytes = DedentControlBytes(targetBytes);
            controllerBytes = DedentControlBytes(controllerBytes);
        }
        if (targetBytes.Length > State.Options.MaxCollectionItems
                - controllerBytes.Length)
        {
            State.FailGeneration(
                "CDDL_GENERATION_COLLECTION_LIMIT",
                "The computed string exceeds the generation size limit.",
                node
            );
            return null;
        }
        byte[] combined = new byte[
            targetBytes.Length + controllerBytes.Length
        ];
        System.Buffer.BlockCopy(
            targetBytes,
            0,
            combined,
            0,
            targetBytes.Length
        );
        System.Buffer.BlockCopy(
            controllerBytes,
            0,
            combined,
            targetBytes.Length,
            controllerBytes.Length
        );
        if (target.Kind == CborValueKind.ByteString)
        {
            CborValue bytes = new CborValue();
            bytes.Kind = CborValueKind.ByteString;
            bytes.ByteStringValue = combined;
            bytes.LengthKind = CborLengthKind.Definite;
            return bytes;
        }
        CborUtf8Result decoded = new CborUtf8().ProcessChunk(
            combined,
            true
        );
        if (decoded.Status == CborReadStatus.Done)
        {
            CborValue text = new CborValue();
            text.Kind = CborValueKind.TextString;
            text.TextStringValue = decoded.Text;
            text.LengthKind = CborLengthKind.Definite;
            return text;
        }
        State.FailGeneration(
            "CDDL_GENERATION_CONTROL_NOT_CONSTRUCTIVE",
            "The computed text is not valid UTF-8.",
            node
        );
        return null;
    }

    internal CborValue? GenerateSizeControl(CddlNode node)
    {
        CborValue? controller = Core.GenerateNodeValue(node.Children[1]);
        if (controller == null || !IsIntegerControlValue(controller)
            || controller.IntegerValue < BigInteger.Zero
            || controller.IntegerValue > new BigInteger(int.MaxValue))
        {
            State.ClearFailure();
            return GenerateHookControl(node, false);
        }
        int size = (int)controller.IntegerValue;
        if (size > State.Options.MaxCollectionItems)
        {
            State.FailGeneration(
                "CDDL_GENERATION_COLLECTION_LIMIT",
                "The controlled string size exceeds the generation limit.",
                node
            );
            return null;
        }
        CborValue? target = Core.GenerateNodeValue(node.Children[0]);
        if (target == null)
        {
            return null;
        }
        CborValue candidate = target;
        if (target.Kind == CborValueKind.ByteString)
        {
            candidate = new CborValue();
            candidate.Kind = CborValueKind.ByteString;
            candidate.ByteStringValue = new byte[size];
            candidate.LengthKind = CborLengthKind.Definite;
        }
        else if (target.Kind == CborValueKind.TextString)
        {
            candidate = new CborValue();
            candidate.Kind = CborValueKind.TextString;
            candidate.TextStringValue = new string('a', size);
            candidate.LengthKind = CborLengthKind.Definite;
        }
        CborValue? accepted = AcceptControlCandidate(node, candidate);
        if (accepted != null)
        {
            return accepted;
        }
        State.ClearFailure();
        return GenerateHookControl(node, false);
    }

    internal CborValue? GenerateTargetControl(CddlNode node)
    {
        CborValue? candidate = Core.GenerateNodeValue(node.Children[0]);
        if (candidate != null)
        {
            CborValue? accepted = AcceptControlCandidate(node, candidate);
            if (accepted != null)
            {
                return accepted;
            }
        }
        if (State.RemainingSteps <= 0
            || !State.HasAttemptBudget(node))
        {
            return null;
        }
        State.ClearFailure();
        return GenerateHookControl(node, false);
    }

    internal CborValue? GenerateBitsControl(CddlNode node)
    {
        CborValue? target = Core.GenerateNodeValue(node.Children[0]);
        if (target != null)
        {
            CborValue? acceptedTarget = AcceptControlCandidate(node, target);
            if (acceptedTarget != null)
            {
                return acceptedTarget;
            }
            CborValue? empty = null;
            if (target.Kind == CborValueKind.UnsignedInteger)
            {
                empty = Core.CreateIntegerValue(BigInteger.Zero);
            }
            else if (target.Kind == CborValueKind.ByteString)
            {
                empty = new CborValue();
                empty.Kind = CborValueKind.ByteString;
                empty.ByteStringValue = [];
                empty.LengthKind = CborLengthKind.Definite;
            }
            if (empty != null)
            {
                CborValue? acceptedEmpty = AcceptControlCandidate(
                    node,
                    empty
                );
                if (acceptedEmpty != null)
                {
                    return acceptedEmpty;
                }
            }
        }
        if (State.RemainingSteps <= 0
            || !State.HasAttemptBudget(node))
        {
            return null;
        }
        State.ClearFailure();
        return GenerateHookControl(node, false);
    }

    internal CborValue? GenerateEmbeddedControl(CddlNode node)
    {
        CborValue? embedded = Core.GenerateNodeValue(node.Children[1]);
        if (embedded == null)
        {
            return null;
        }
        CborEncodeOptions options = new CborEncodeOptions();
        options.MaxNestingDepth = State.Options.MaxNestingDepth;
        int itemLimit = State.Options.MaxCollectionItems;
        if (itemLimit < 1)
        {
            itemLimit = 1;
        }
        options.MaxTotalItems = itemLimit;
        options.MaxSequenceItems = itemLimit;
        byte[] bytes;
        CborEncoder encoder = new CborEncoder();
        if (node.ControlName == CddlControlNames.CborSequence)
        {
            if (embedded.Kind != CborValueKind.Array
                || embedded.ArrayItems == null)
            {
                State.ClearFailure();
                return GenerateHookControl(node, false);
            }
            bytes = encoder.EncodeSequence(embedded.ArrayItems, options);
        }
        else
        {
            bytes = encoder.Encode(embedded, options);
        }
        if (bytes.Length > State.Options.MaxCollectionItems)
        {
            State.FailGeneration(
                "CDDL_GENERATION_COLLECTION_LIMIT",
                "The embedded CBOR exceeds the generation size limit.",
                node
            );
            return null;
        }
        CborValue candidate = new CborValue();
        candidate.Kind = CborValueKind.ByteString;
        candidate.ByteStringValue = bytes;
        candidate.LengthKind = CborLengthKind.Definite;
        CborValue? accepted = AcceptControlCandidate(node, candidate);
        if (accepted != null)
        {
            return accepted;
        }
        State.ClearFailure();
        return GenerateHookControl(node, false);
    }

    internal CborValue? GenerateIntersectionControl(CddlNode node)
    {
        CborValue? controller = Core.GenerateNodeValue(node.Children[1]);
        if (controller != null)
        {
            CborValue? acceptedController = AcceptControlCandidate(
                node,
                controller
            );
            if (acceptedController != null)
            {
                return acceptedController;
            }
        }
        if (State.RemainingSteps <= 0
            || !State.HasAttemptBudget(node))
        {
            return null;
        }
        State.ClearFailure();
        CborValue? target = Core.GenerateNodeValue(node.Children[0]);
        if (target != null)
        {
            CborValue? acceptedTarget = AcceptControlCandidate(node, target);
            if (acceptedTarget != null)
            {
                return acceptedTarget;
            }
        }
        if ((node.ControlName == CddlControlNames.NotEqual
                || node.ControlName == CddlControlNames.Default)
            && controller != null)
        {
            CborValue? unequal = CreateUnequalCandidate(controller);
            if (unequal != null)
            {
                CborValue? acceptedUnequal = AcceptControlCandidate(
                    node,
                    unequal
                );
                if (acceptedUnequal != null)
                {
                    return acceptedUnequal;
                }
            }
        }
        if (State.RemainingSteps <= 0
            || !State.HasAttemptBudget(node))
        {
            return null;
        }
        State.ClearFailure();
        return GenerateHookControl(node, false);
    }

    internal CborValue? CreateUnequalCandidate(CborValue controller)
    {
        if (IsIntegerControlValue(controller))
        {
            return Core.CreateIntegerValue(
                controller.IntegerValue + BigInteger.One
            );
        }
        if (controller.Kind == CborValueKind.Float
            && controller.FloatValue != null)
        {
            return Core.CreateFloatValue(
                System.Math.BitIncrement(controller.FloatValue.Value),
                CborFloatWidth.Double
            );
        }
        if (controller.Kind == CborValueKind.TextString)
        {
            CborValue text = new CborValue();
            text.Kind = CborValueKind.TextString;
            text.TextStringValue = (controller.TextStringValue ?? "") + "a";
            text.LengthKind = CborLengthKind.Definite;
            return text;
        }
        if (controller.Kind == CborValueKind.ByteString)
        {
            byte[] source = controller.ByteStringValue ?? [];
            byte[] bytes = new byte[source.Length + 1];
            int index = 0;
            while (index < source.Length)
            {
                bytes[index] = source[index];
                index = index + 1;
            }
            CborValue value = new CborValue();
            value.Kind = CborValueKind.ByteString;
            value.ByteStringValue = bytes;
            value.LengthKind = CborLengthKind.Definite;
            return value;
        }
        return null;
    }

    internal CborValue? GenerateRelationalControl(CddlNode node)
    {
        CborValue? target = Core.GenerateNodeValue(node.Children[0]);
        if (target != null)
        {
            CborValue? acceptedTarget = AcceptControlCandidate(node, target);
            if (acceptedTarget != null)
            {
                return acceptedTarget;
            }
        }
        if (State.RemainingSteps <= 0
            || !State.HasAttemptBudget(node))
        {
            return null;
        }
        State.ClearFailure();
        CborValue? boundary = Core.GenerateNodeValue(node.Children[1]);
        CborValue? candidate = CreateRelationalCandidate(node, boundary);
        if (candidate != null)
        {
            CborValue? acceptedBoundary = AcceptControlCandidate(
                node,
                candidate
            );
            if (acceptedBoundary != null)
            {
                return acceptedBoundary;
            }
        }
        State.ClearFailure();
        return GenerateHookControl(node, false);
    }

    internal CborValue? CreateRelationalCandidate(
        CddlNode node,
        CborValue? boundary)
    {
        if (boundary == null)
        {
            return null;
        }
        bool lower = node.ControlName == CddlControlNames.LessThan
            || node.ControlName == CddlControlNames.LessThanOrEqual;
        bool strict = node.ControlName == CddlControlNames.LessThan
            || node.ControlName == CddlControlNames.GreaterThan;
        if (IsIntegerControlValue(boundary))
        {
            BigInteger value = boundary.IntegerValue;
            if (strict)
            {
                value = lower ? value - BigInteger.One
                    : value + BigInteger.One;
            }
            return Core.CreateIntegerValue(value);
        }
        if (boundary.Kind == CborValueKind.Float
            && boundary.FloatValue != null)
        {
            double value = boundary.FloatValue.Value;
            if (strict)
            {
                value = lower
                    ? System.Math.BitDecrement(value)
                    : System.Math.BitIncrement(value);
            }
            return Core.CreateFloatValue(value, CborFloatWidth.Double);
        }
        return null;
    }

    internal CborValue? GenerateFeatureControl(CddlNode node)
    {
        CborValue? descriptor = Core.GenerateNodeValue(node.Children[1]);
        string? feature = FindFeatureName(descriptor);
        if (feature == null || !ContainsEnabledFeature(feature))
        {
            State.FailGeneration(
                "CDDL_GENERATION_FEATURE_DISABLED",
                "The controlled alternative requires an unavailable feature.",
                node
            );
            return null;
        }
        return GenerateTargetControl(node);
    }

    internal string? FindFeatureName(CborValue? descriptor)
    {
        if (descriptor == null)
        {
            return null;
        }
        if (descriptor.Kind == CborValueKind.TextString)
        {
            return descriptor.TextStringValue;
        }
        if (descriptor.Kind == CborValueKind.Array
            && descriptor.ArrayItems != null
            && descriptor.ArrayItems.Count == 2
            && descriptor.ArrayItems[0].Kind == CborValueKind.TextString)
        {
            return descriptor.ArrayItems[0].TextStringValue;
        }
        return null;
    }

    internal bool ContainsEnabledFeature(string feature)
    {
        int index = 0;
        while (index < State.Options.EnabledFeatures.Length)
        {
            if (State.Options.EnabledFeatures[index] == feature)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal CborValue? AcceptControlCandidate(
        CddlNode node,
        CborValue candidate)
    {
        if (ControlCandidateMatches(node, candidate))
        {
            State.ClearFailure();
            return candidate;
        }
        State.FailGeneration(
            "CDDL_GENERATION_CONTROL_MISMATCH",
            "The constructed value does not satisfy the CDDL control.",
            node
        );
        return null;
    }

    internal bool ControlCandidateMatches(
        CddlNode node,
        CborValue candidate)
    {
        CddlSchema schema = CreateControlSchema(node);
        CddlValidationOptions options = new CddlValidationOptions();
        options.RootRuleName = ValidationRuleName;
        options.EnabledFeatures = (string[])
            State.Options.EnabledFeatures.Clone();
        options.CollectIssues = false;
        options.MaxIssues = 1;
        options.MaxNestingDepth = State.Options.MaxNestingDepth + 8;
        options.MaxSteps = State.Options.MaxSteps;
        options.MaxAlternatives = State.Options.MaxAttempts;
        options.MaxMemoEntries = State.Options.MaxSteps;
        options.MaxEmbeddedBytes = State.Options.MaxCollectionItems;
        options.MaxPatternStates = State.Options.MaxSteps;
        return new CddlValidator().ValidateCbor(
            schema,
            candidate,
            options
        ).IsValid;
    }

    internal CddlSchema CreateControlSchema(CddlNode node)
    {
        ValidationRuleName = "__generated_control";
        while (State.FindRule(ValidationRuleName) != null)
        {
            ValidationRuleName = ValidationRuleName + "_";
        }
        CddlRule root = new CddlRule();
        root.Name = ValidationRuleName;
        root.Kind = CddlRuleKind.Type;
        root.Value = node;
        root.Span = node.Span;
        CddlSchema schema = new CddlSchema();
        schema.Document = State.Schema.Document;
        schema.Rules = new CddlRule[State.Schema.Rules.Length + 1];
        int index = 0;
        while (index < State.Schema.Rules.Length)
        {
            schema.Rules[index] = State.Schema.Rules[index];
            index = index + 1;
        }
        schema.Rules[index] = root;
        schema.RootRule = root;
        schema.NormalizedNodes = State.Schema.NormalizedNodes;
        schema.SourceMap = State.Schema.SourceMap;
        schema.ControlRegistrations = State.Schema.ControlRegistrations;
        schema.Features = State.Schema.Features;
        return schema;
    }

    internal CborValue? GenerateHookControl(
        CddlNode node,
        bool custom)
    {
        State.FailGeneration(
            custom
                ? "CDDL_GENERATION_CONTROL_UNSUPPORTED"
                : "CDDL_GENERATION_CONTROL_REQUIRES_HOOK",
            custom
                ? "The custom control has no applicable generation handler."
                : "The control requires an application generation hook.",
            node
        );
        return null;
    }

    internal CborValue? GenerateRegisteredControl(
        CddlNode node)
    {
        CddlControlRegistration? registration = FindControlRegistration(
            node.ControlName ?? ""
        );
        if (registration == null || registration.Handler == null)
        {
            return null;
        }
        CddlGenerationContext context = Core.CreateGenerationContext(
            node,
            Core.CurrentRule()
        );
        CddlControlResult? supplied;
        using (CddlGenerationHookScope scope =
            new CddlGenerationHookScope(
                "custom control generation",
                context
            ))
        {
            supplied = registration.Handler.GenerateControl(
                node.Children[0],
                node.Children[1],
                context
            );
            scope.CompleteInvocation();
        }
        return Core.UseSuppliedValue(supplied, node);
    }

    internal CddlControlRegistration? FindControlRegistration(string name)
    {
        int index = 0;
        while (index < State.Schema.ControlRegistrations.Length)
        {
            CddlControlRegistration registration =
                State.Schema.ControlRegistrations[index];
            if (registration.Name == name && registration.IsEnabled)
            {
                return registration;
            }
            index = index + 1;
        }
        return null;
    }

    internal bool IsConstructiveTextControl(string name)
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
            || name == CddlControlNames.HexUpper
            || name == CddlControlNames.Base10
            || name == CddlControlNames.Printf
            || name == CddlControlNames.Json
            || name == CddlControlNames.Join;
    }

    internal bool IsStandardControl(string name)
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
        return false;
    }

    internal bool IsIntegerControlValue(CborValue value)
    {
        return value.Kind == CborValueKind.UnsignedInteger
            || value.Kind == CborValueKind.NegativeInteger;
    }

    internal byte[]? FindStringBytes(CborValue value)
    {
        if (value.Kind == CborValueKind.ByteString)
        {
            return value.ByteStringValue ?? [];
        }
        if (value.Kind == CborValueKind.TextString)
        {
            return System.Text.Encoding.UTF8.GetBytes(
                value.TextStringValue ?? ""
            );
        }
        return null;
    }

    internal byte[] DedentControlBytes(byte[] source)
    {
        int width = FindControlDedentWidth(source);
        List<byte> result = new List<byte>(source.Length);
        int index = 0;
        bool lineStart = true;
        while (index < source.Length)
        {
            if (lineStart)
            {
                int spaces = 0;
                while (index + spaces < source.Length
                    && source[index + spaces] == 0x20)
                {
                    spaces = spaces + 1;
                }
                int content = index + spaces;
                bool blank = content >= source.Length
                    || source[content] == 0x0a
                    || (source[content] == 0x0d
                        && content + 1 < source.Length
                        && source[content + 1] == 0x0a);
                int remove = spaces;
                if (!blank && width != int.MaxValue && remove > width)
                {
                    remove = width;
                }
                index = index + remove;
                lineStart = false;
                if (index >= source.Length)
                {
                    break;
                }
            }
            byte current = source[index];
            result.Add(current);
            index = index + 1;
            if (current == 0x0a)
            {
                lineStart = true;
            }
        }
        return result.ToArray();
    }

    internal int FindControlDedentWidth(byte[] source)
    {
        int minimum = int.MaxValue;
        int start = 0;
        while (start < source.Length)
        {
            int end = start;
            while (end < source.Length && source[end] != 0x0a)
            {
                end = end + 1;
            }
            int contentEnd = end;
            if (contentEnd > start && source[contentEnd - 1] == 0x0d)
            {
                contentEnd = contentEnd - 1;
            }
            int spaces = 0;
            while (start + spaces < contentEnd
                && source[start + spaces] == 0x20)
            {
                spaces = spaces + 1;
            }
            if (start + spaces < contentEnd && spaces < minimum)
            {
                minimum = spaces;
            }
            if (end == source.Length)
            {
                break;
            }
            start = end + 1;
        }
        return minimum;
    }
}
