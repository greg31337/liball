namespace cbor;

/// <summary>Generates matching CBOR values from compiled CDDL schemas.</summary>
public class CddlValueGenerator : ICddlValueGenerator
{
    internal object GenerationLock = new object();

    /// <summary>Creates a CDDL value generator.</summary>
    public CddlValueGenerator()
    {
    }

    /// <summary>Generates a value for a selected productive schema rule.</summary>
    /// <param name="schema">The compiled schema.</param>
    /// <param name="options">The generation strategy and budgets.</param>
    /// <returns>The structured generation result.</returns>
    public CddlGenerationResult GenerateValue(
        CddlSchema schema,
        CddlGenerationOptions options)
    {
        if (options == null)
        {
            return CreateInputFailure(
                "CDDL_GENERATION_OPTIONS_MISSING",
                "CDDL generation options are required.",
                new CddlGenerationOptions()
            );
        }
        if (schema == null)
        {
            return CreateInputFailure(
                "CDDL_GENERATION_SCHEMA_MISSING",
                "A compiled CDDL schema is required.",
                options
            );
        }
        if (System.Threading.Monitor.IsEntered(GenerationLock))
        {
            return CreateInputFailure(
                "CDDL_GENERATION_REENTRANT",
                "A CDDL value generator cannot be reentered during an active operation.",
                options
            );
        }
        lock (GenerationLock)
        {
            CddlGenerationState state = new CddlGenerationState(
                schema,
                options
            );
            if (!state.InitializeGeneration())
            {
                return state.Result;
            }
            CddlValueGeneratorCore core = new CddlValueGeneratorCore(state);
            InvokeBeginGeneration(core);
            CborValue? value = core.GenerateSelectedValue();
            if (value == null)
            {
                if (state.Result.ErrorCode == null)
                {
                    state.FailGeneration(
                        "CDDL_GENERATION_FAILED",
                        "The selected CDDL rule did not produce a scalar value.",
                        state.SelectedRule == null
                            ? null
                            : state.SelectedRule.Value
                    );
                }
            }
            else
            {
                state.CompleteValidatedValue(value);
            }
            InvokeEndGeneration(core);
            return state.Result;
        }
    }

    internal void InvokeBeginGeneration(CddlValueGeneratorCore core)
    {
        ICddlGenerationHook? hook = core.State.Options.Hook;
        CddlRule? rule = core.State.SelectedRule;
        if (hook == null || rule == null || rule.Value == null)
        {
            return;
        }
        CddlGenerationContext context = core.CreateGenerationContext(
            rule.Value,
            rule
        );
        using (CddlGenerationHookScope scope =
            new CddlGenerationHookScope(
                "begin generation",
                context
            ))
        {
            hook.BeginGeneration(context);
            scope.CompleteInvocation();
        }
    }

    internal void InvokeEndGeneration(CddlValueGeneratorCore core)
    {
        ICddlGenerationHook? hook = core.State.Options.Hook;
        CddlRule? rule = core.State.SelectedRule;
        if (hook == null || rule == null || rule.Value == null)
        {
            return;
        }
        CddlGenerationContext context = core.CreateGenerationContext(
            rule.Value,
            rule
        );
        using (CddlGenerationHookScope scope =
            new CddlGenerationHookScope(
                "end generation",
                context
            ))
        {
            hook.EndGeneration(context, core.State.Result);
            scope.CompleteInvocation();
        }
    }

    internal CddlGenerationResult CreateInputFailure(
        string code,
        string message,
        CddlGenerationOptions options)
    {
        CddlGenerationResult result = new CddlGenerationResult();
        result.ErrorCode = code;
        result.ErrorMessage = message;
        result.Options = options;
        return result;
    }
}
