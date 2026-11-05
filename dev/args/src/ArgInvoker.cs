namespace args;

internal class ArgInvoker
{
    internal ArgRunResult Invoke(ArgParseResult parseResult, ArgValidationResult validationResult)
    {
        if (parseResult == null)
        {
            throw new System.ArgumentNullException("parseResult");
        }

        if (validationResult == null)
        {
            throw new System.ArgumentNullException("validationResult");
        }

        ArgContext context = new ArgContext(parseResult);
        if (validationResult.HasErrors())
        {
            return new ArgRunResult(parseResult, validationResult, context, 1, false);
        }

        if (validationResult.ShortCircuited)
        {
            ArgRunResult runResult = new ArgRunResult(parseResult, validationResult, context, 0, false);
            if (validationResult.HelpRequested)
            {
                runResult.SetHelpText(new ArgHelpBuilder().UseSettings(parseResult.Settings).Help(parseResult.Command));
            }

            return runResult;
        }

        if (parseResult.Command.HandlerFunc == null)
        {
            ArgRunResult runResult = new ArgRunResult(parseResult, validationResult, context, 0, false);
            if (IsBranchCommand(parseResult.Command))
            {
                validationResult.MarkHelpRequested();
                runResult.SetHelpText(new ArgHelpBuilder().UseSettings(parseResult.Settings).Help(parseResult.Command));
            }

            return runResult;
        }

        int beforeExitCode = InvokeBeforeHooks(parseResult, context);
        if (beforeExitCode != 0)
        {
            return new ArgRunResult(parseResult, validationResult, context, beforeExitCode, false);
        }

        int exitCode = parseResult.Command.HandlerFunc(context);
        exitCode = InvokeAfterHooks(parseResult, context, exitCode);
        return new ArgRunResult(parseResult, validationResult, context, exitCode, true);
    }

    internal int InvokeBeforeHooks(ArgParseResult parseResult, ArgContext context)
    {
        int index = 0;
        while (index < parseResult.CommandPath.Count)
        {
            ArgCommand command = parseResult.CommandPath[index];
            if (command.BeforeFunc != null)
            {
                int exitCode = command.BeforeFunc(context);
                if (exitCode != 0)
                {
                    return exitCode;
                }
            }

            index = index + 1;
        }

        return 0;
    }

    internal int InvokeAfterHooks(ArgParseResult parseResult, ArgContext context, int exitCode)
    {
        int result = exitCode;
        int index = parseResult.CommandPath.Count - 1;
        while (index >= 0)
        {
            ArgCommand command = parseResult.CommandPath[index];
            if (command.AfterFunc != null)
            {
                int afterExitCode = command.AfterFunc(context);
                if (afterExitCode != 0)
                {
                    result = afterExitCode;
                }
            }

            index = index - 1;
        }

        return result;
    }

    internal bool IsBranchCommand(ArgCommand command)
    {
        if (command.HandlerFunc != null)
        {
            return false;
        }

        if (command.IsBranch || command.Commands.Count > 0)
        {
            return true;
        }

        return false;
    }
}
