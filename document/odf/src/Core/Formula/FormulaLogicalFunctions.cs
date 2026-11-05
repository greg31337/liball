namespace odf;

internal class FormulaLogicalState
{
    internal OdfFormulaResult? Error;
    internal int TrueCount;
    internal int ValueCount;
}

internal static class FormulaLogicalFunctions
{
    internal static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments)
    {
        if (name == OdfFormulaNames.TRUE)
        {
            return FormulaValueModel.CreateBoolean(true);
        }
        if (name == OdfFormulaNames.FALSE)
        {
            return FormulaValueModel.CreateBoolean(false);
        }
        if (name == OdfFormulaNames.IF)
        {
            return EvaluateIf(arguments);
        }
        if (name == OdfFormulaNames.IFERROR)
        {
            return arguments[0].Error != OdfFormulaError.None
                ? arguments[1]
                : arguments[0];
        }
        if (name == OdfFormulaNames.IFNA)
        {
            return arguments[0].Error == OdfFormulaError.NotAvailable
                ? arguments[1]
                : arguments[0];
        }
        if (name == OdfFormulaNames.NOT)
        {
            OdfFormulaResult logical = FormulaValueModel.ConvertToBoolean(
                arguments[0]);
            return logical.Error == OdfFormulaError.None
                ? FormulaValueModel.CreateBoolean(!logical.BooleanValue)
                : logical;
        }
        FormulaLogicalState state = new FormulaLogicalState();
        int index = 0;
        while (index < arguments.Length && state.Error == null)
        {
            Accumulate(arguments[index], state);
            index = index + 1;
        }
        if (state.Error != null)
        {
            return state.Error;
        }
        if (state.ValueCount == 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        if (name == OdfFormulaNames.AND)
        {
            return FormulaValueModel.CreateBoolean(
                state.TrueCount == state.ValueCount);
        }
        if (name == OdfFormulaNames.OR)
        {
            return FormulaValueModel.CreateBoolean(state.TrueCount > 0);
        }
        return FormulaValueModel.CreateBoolean(state.TrueCount % 2 != 0);
    }

    static OdfFormulaResult EvaluateIf(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult condition = FormulaValueModel.ConvertToBoolean(
            arguments[0]);
        if (condition.Error != OdfFormulaError.None)
        {
            return condition;
        }
        if (arguments.Length == 1)
        {
            return FormulaValueModel.CreateBoolean(condition.BooleanValue);
        }
        if (condition.BooleanValue)
        {
            return arguments[1].Kind == OdfValueKind.Empty
                ? FormulaValueModel.CreateNumber(0)
                : arguments[1];
        }
        if (arguments.Length < 3)
        {
            return FormulaValueModel.CreateBoolean(false);
        }
        return arguments[2].Kind == OdfValueKind.Empty
            ? FormulaValueModel.CreateNumber(0)
            : arguments[2];
    }

    static void Accumulate(
        OdfFormulaResult value,
        FormulaLogicalState state)
    {
        if (value.Kind == OdfValueKind.Reference
            || value.Kind == OdfValueKind.Array)
        {
            int index = 0;
            while (index < value.Values.Length && state.Error == null)
            {
                Accumulate(value.Values[index], state);
                index = index + 1;
            }
            return;
        }
        OdfFormulaResult logical = FormulaValueModel.ConvertToBoolean(value);
        if (logical.Error != OdfFormulaError.None)
        {
            state.Error = logical;
            return;
        }
        state.ValueCount = state.ValueCount + 1;
        if (logical.BooleanValue)
        {
            state.TrueCount = state.TrueCount + 1;
        }
    }
}
