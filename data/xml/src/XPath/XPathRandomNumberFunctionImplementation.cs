using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace xml;

internal class XPathRandomNumberFunctionImplementation : IXPathFunction
{
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XdmSequenceOperations Sequences;
    internal XdmMapOperations Maps;
    internal XdmFunctionOperations Functions;
    internal XdmFactory Factory;
    internal XdmTypeConverter Converter;

    internal XPathRandomNumberFunctionImplementation(SourceRange range, XPathStaticContext staticContext)
    {
        Range = range;
        StaticContext = staticContext;
        Sequences = new XdmSequenceOperations();
        Maps = new XdmMapOperations();
        Functions = new XdmFunctionOperations();
        Factory = new XdmFactory();
        Converter = new XdmTypeConverter();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        ulong seed = arguments.Length == 0 ? HashXPathRandomSeed("xml-default-random-seed") : ReadXPathRandomSeed(arguments[0]);
        return CreateXPathRandomGenerator(seed);
    }

    internal ulong ReadXPathRandomSeed(XdmSequence supplied)
    {
        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = XdmAtomicType.AnyAtomicType;
        expected.Occurrence = XdmOccurrence.ZeroOrOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        if (converted.Count == 0)
        {
            return HashXPathRandomSeed("xml-default-random-seed");
        }

        XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(converted, 0).AtomicValue;
        string lexical = value == null || value.StringValue == null ? "" : value.StringValue;
        string type = value == null ? "" : value.Type.ToString();
        return HashXPathRandomSeed(type + "|" + lexical);
    }

    internal ulong HashXPathRandomSeed(string value)
    {
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        ulong result = 0UL;
        int index = 0;
        while (index < 8)
        {
            result = result << 8 | digest[index];
            index = index + 1;
        }

        return result == 0UL ? 11400714819323198485UL : result;
    }

    internal ulong AdvanceXPathRandomState(ulong state)
    {
        byte[] digest = SHA256.HashData(System.BitConverter.GetBytes(state));
        ulong result = 0UL;
        int index = 0;
        while (index < 8)
        {
            result = result << 8 | digest[index];
            index = index + 1;
        }

        return result;
    }

    internal XdmSequence CreateXPathRandomGenerator(ulong state)
    {
        ulong generated = AdvanceXPathRandomState(state);
        double number = (generated >> 11) * (1.0 / 9007199254740992.0);
        XdmAtomicValue numberValue = Factory.CreateXdmFloatingAtomicValue(number.ToString("R", CultureInfo.InvariantCulture), XdmAtomicType.Double);
        XdmFunctionItem next = CreateXPathRandomFunctionItem(0, new XPathRandomNextFunctionImplementation(generated, Range, StaticContext));
        XdmFunctionItem permute = CreateXPathRandomFunctionItem(1, new XPathRandomPermuteFunctionImplementation(state));
        XdmMapEntry numberEntry = Maps.CreateXdmMapEntry(CreateXPathRandomKey("number"), Sequences.CreateXdmAtomicSequence(numberValue));
        XdmMapEntry nextEntry = Maps.CreateXdmMapEntry(CreateXPathRandomKey("next"), Sequences.CreateXdmFunctionSequence(next));
        XdmMapEntry permuteEntry = Maps.CreateXdmMapEntry(CreateXPathRandomKey("permute"), Sequences.CreateXdmFunctionSequence(permute));
        return Sequences.CreateXdmMapSequence(Maps.CreateXdmMap([numberEntry, nextEntry, permuteEntry]));
    }

    internal XdmFunctionItem CreateXPathRandomFunctionItem(int arity, IXPathFunction implementation)
    {
        XPathFunctionBinding binding = new XPathFunctionBinding();
        binding.Arity = arity;
        binding.ParameterTypes = null;
        binding.ParameterOccurrences = null;
        binding.ResultType = XdmAtomicType.AnyAtomicType;
        binding.ResultOccurrence = XdmOccurrence.ZeroOrMore;
        binding.IsDeterministic = true;
        return Functions.CreateXdmFunctionItem(binding, implementation, []);
    }

    internal XdmAtomicValue CreateXPathRandomKey(string value)
    {
        return Factory.CreateXdmStringAtomicValue(value, XdmAtomicType.String);
    }
}

internal class XPathRandomNextFunctionImplementation : IXPathFunction
{
    internal ulong State;
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;

    internal XPathRandomNextFunctionImplementation(ulong state, SourceRange range, XPathStaticContext staticContext)
    {
        State = state;
        Range = range;
        StaticContext = staticContext;
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        return new XPathRandomNumberFunctionImplementation(Range, StaticContext).CreateXPathRandomGenerator(State);
    }
}

internal class XPathRandomPermuteFunctionImplementation : IXPathFunction
{
    internal ulong State;
    internal XdmSequenceOperations Sequences;

    internal XPathRandomPermuteFunctionImplementation(ulong state)
    {
        State = state;
        Sequences = new XdmSequenceOperations();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmSequence supplied = arguments[0];
        int[] positions = new int[supplied.Count];
        int index = 0;
        while (index < positions.Length)
        {
            positions[index] = index;
            index = index + 1;
        }

        ulong random = State;
        int remaining = positions.Length - 1;
        while (remaining > 0)
        {
            random = AdvanceXPathPermutationState(random);
            int selected = (int)(random % (ulong)(remaining + 1));
            int saved = positions[remaining];
            positions[remaining] = positions[selected];
            positions[selected] = saved;
            remaining = remaining - 1;
        }

        XdmSequenceBuilder result = new XdmSequenceBuilder();
        index = 0;
        while (index < positions.Length)
        {
            AppendXPathPermutationItem(result, Sequences.ReadXdmSequenceItem(supplied, positions[index]));
            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal ulong AdvanceXPathPermutationState(ulong state)
    {
        byte[] digest = System.Security.Cryptography.SHA256.HashData(System.BitConverter.GetBytes(state));
        ulong result = 0UL;
        int index = 0;
        while (index < 8)
        {
            result = result << 8 | digest[index];
            index = index + 1;
        }

        return result;
    }

    internal void AppendXPathPermutationItem(XdmSequenceBuilder builder, XdmSequenceItem item)
    {
        if (item.Kind == XdmItemKind.Node && item.Node != null)
        {
            builder.AddXdmNode(item.Node);
        }
        else if (item.Kind == XdmItemKind.Atomic && item.AtomicValue != null)
        {
            builder.AddXdmAtomicValue(item.AtomicValue);
        }
        else if (item.Kind == XdmItemKind.Function && item.Function != null)
        {
            builder.AddXdmFunction(item.Function);
        }
        else if (item.Kind == XdmItemKind.Map && item.Map != null)
        {
            builder.AddXdmMap(item.Map);
        }
        else if (item.Kind == XdmItemKind.Array && item.Array != null)
        {
            builder.AddXdmArray(item.Array);
        }
        else
        {
            throw new System.InvalidOperationException("The sequence item to permute is incomplete.");
        }
    }
}
