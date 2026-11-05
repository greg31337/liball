using System.Collections.Generic;
using System.Text;

namespace args;

internal class ArgCompletionScriptBuilder
{
    internal ArgCompletionProvider Provider;
    internal ArgCommand RootCommand;

    internal ArgCompletionScriptBuilder(ArgCompletionProvider provider, ArgCommand rootCommand)
    {
        Provider = provider;
        RootCommand = rootCommand;
    }

    internal string Build(ArgCompletionShell shell)
    {
        List<ArgCompletion> candidates = ScriptCandidates();
        if (shell == ArgCompletionShell.Bash)
        {
            return BuildBashScript(candidates);
        }

        if (shell == ArgCompletionShell.Zsh)
        {
            return BuildZshScript(candidates);
        }

        if (shell == ArgCompletionShell.Fish)
        {
            return BuildFishScript(candidates);
        }

        return BuildPowerShellScript(candidates);
    }

    internal List<ArgCompletion> ScriptCandidates()
    {
        List<ArgCompletion> candidates = new List<ArgCompletion>();
        AddCommandScriptCandidates(candidates, RootCommand);
        return candidates;
    }

    internal void AddCommandScriptCandidates(List<ArgCompletion> candidates, ArgCommand command)
    {
        List<ArgCompletion> direct = Provider.Complete(command, new string[] { "" });
        int index = 0;
        while (index < direct.Count)
        {
            AddUniqueScriptCandidate(candidates, direct[index]);
            index = index + 1;
        }

        index = 0;
        while (index < command.Options.Count)
        {
            ArgOption option = command.Options[index];
            if (!option.IsHidden && OptionCanAppearInScript(option))
            {
                AddOptionValueScriptCandidates(candidates, option);
            }

            index = index + 1;
        }

        index = 0;
        while (index < command.GlobalOptions.Count)
        {
            ArgOption option = command.GlobalOptions[index];
            if (!option.IsHidden && OptionCanAppearInScript(option))
            {
                AddOptionValueScriptCandidates(candidates, option);
            }

            index = index + 1;
        }

        index = 0;
        while (index < command.Arguments.Count)
        {
            AddArgumentValueScriptCandidates(candidates, command.Arguments[index]);
            index = index + 1;
        }

        index = 0;
        while (index < command.Commands.Count)
        {
            ArgCommand child = command.Commands[index];
            if (!child.IsHidden)
            {
                AddCommandScriptCandidates(candidates, child);
            }

            index = index + 1;
        }
    }

    internal bool OptionCanAppearInScript(ArgOption option)
    {
        int index = 0;
        while (index < option.Aliases.Count)
        {
            if (Provider.AliasCanBeCompleted(option.Aliases[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AddOptionValueScriptCandidates(List<ArgCompletion> candidates, ArgOption option)
    {
        int index = 0;
        while (index < option.Completions.Count)
        {
            AddUniqueScriptCandidate(candidates, ArgCompletion.ForOptionValue(option, option.Completions[index]));
            index = index + 1;
        }
    }

    internal void AddArgumentValueScriptCandidates(List<ArgCompletion> candidates, ArgArgument argument)
    {
        int index = 0;
        while (index < argument.Completions.Count)
        {
            AddUniqueScriptCandidate(candidates, ArgCompletion.ForArgumentValue(argument, argument.Completions[index]));
            index = index + 1;
        }
    }

    internal void AddUniqueScriptCandidate(List<ArgCompletion> candidates, ArgCompletion candidate)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            if (candidates[index].Text == candidate.Text)
            {
                return;
            }

            index = index + 1;
        }

        candidates.Add(candidate);
    }

    internal string BuildBashScript(List<ArgCompletion> candidates)
    {
        string identifier = ShellIdentifier();
        StringBuilder text = new StringBuilder();
        text.AppendLine("# bash completion for " + RootCommand.Name);
        text.AppendLine(identifier + "()");
        text.AppendLine("{");
        text.AppendLine("    local current=\"${COMP_WORDS[COMP_CWORD]}\"");
        text.AppendLine("    local candidate");
        if (ScriptMatchingCaseInsensitive())
        {
            text.AppendLine("    local restore_nocasematch=0");
            text.AppendLine("    if ! shopt -q nocasematch; then");
            text.AppendLine("        shopt -s nocasematch");
            text.AppendLine("        restore_nocasematch=1");
            text.AppendLine("    fi");
        }

        text.AppendLine("    local candidates=(");
        int index = 0;
        while (index < candidates.Count)
        {
            text.AppendLine("        " + PosixShellQuote(candidates[index].Text));
            index = index + 1;
        }

        text.AppendLine("    )");
        text.AppendLine("    COMPREPLY=()");
        text.AppendLine("    for candidate in \"${candidates[@]}\"; do");
        text.AppendLine("        if [[ \"$candidate\" == \"$current\"* ]]; then");
        text.AppendLine("            COMPREPLY+=(\"$candidate\")");
        text.AppendLine("        fi");
        text.AppendLine("    done");
        if (ScriptMatchingCaseInsensitive())
        {
            text.AppendLine("    if [[ $restore_nocasematch == 1 ]]; then");
            text.AppendLine("        shopt -u nocasematch");
            text.AppendLine("    fi");
        }

        text.AppendLine("}");
        text.Append("complete -F " + identifier + " -- " + PosixShellQuote(RootCommand.Name));
        return text.ToString();
    }

    internal string BuildZshScript(List<ArgCompletion> candidates)
    {
        string identifier = ShellIdentifier();
        StringBuilder text = new StringBuilder();
        text.AppendLine("#compdef " + RootCommand.Name);
        text.AppendLine(identifier + "()");
        text.AppendLine("{");
        text.AppendLine("    local -a candidates");
        text.AppendLine("    candidates=(");
        int index = 0;
        while (index < candidates.Count)
        {
            text.AppendLine("        " + PosixShellQuote(candidates[index].Text));
            index = index + 1;
        }

        text.AppendLine("    )");
        if (ScriptMatchingCaseInsensitive())
        {
            text.AppendLine("    compadd -M 'm:{a-zA-Z}={A-Za-z}' -- \"${candidates[@]}\"");
        }
        else
        {
            text.AppendLine("    compadd -- \"${candidates[@]}\"");
        }

        text.AppendLine("}");
        text.Append("compdef " + identifier + " " + PosixShellQuote(RootCommand.Name));
        return text.ToString();
    }

    internal string BuildFishScript(List<ArgCompletion> candidates)
    {
        StringBuilder text = new StringBuilder();
        text.AppendLine("# fish completion for " + RootCommand.Name);
        text.AppendLine("complete -c " + FishShellQuote(RootCommand.Name) + " -f");
        int index = 0;
        while (index < candidates.Count)
        {
            ArgCompletion candidate = candidates[index];
            string line = "complete -c " + FishShellQuote(RootCommand.Name) + " -a " + FishShellQuote(FishCompletionText(candidate.Text));
            if (candidate.DescriptionText.Length > 0)
            {
                line = line + " -d " + FishShellQuote(candidate.DescriptionText);
            }

            text.AppendLine(line);
            index = index + 1;
        }

        return text.ToString().TrimEnd('\n');
    }

    internal string BuildPowerShellScript(List<ArgCompletion> candidates)
    {
        StringBuilder text = new StringBuilder();
        text.AppendLine("# PowerShell completion for " + RootCommand.Name);
        text.AppendLine("Register-ArgumentCompleter -Native -CommandName " + PowerShellQuote(RootCommand.Name) + " -ScriptBlock {");
        text.AppendLine("    param($wordToComplete, $commandAst, $cursorPosition)");
        text.AppendLine("    $candidates = @(");
        int index = 0;
        while (index < candidates.Count)
        {
            ArgCompletion candidate = candidates[index];
            text.AppendLine("        @{ Text = " + PowerShellQuote(candidate.Text) + "; Description = " + PowerShellQuote(candidate.DescriptionText) + " }");
            index = index + 1;
        }

        text.AppendLine("    )");
        text.AppendLine("    foreach ($candidate in $candidates) {");
        string comparison = "Ordinal";
        if (ScriptMatchingCaseInsensitive())
        {
            comparison = "OrdinalIgnoreCase";
        }

        text.AppendLine("        if ($candidate.Text.StartsWith($wordToComplete, [System.StringComparison]::" + comparison + ")) {");
        text.AppendLine("            [System.Management.Automation.CompletionResult]::new($candidate.Text, $candidate.Text, 'ParameterValue', $candidate.Description)");
        text.AppendLine("        }");
        text.AppendLine("    }");
        text.Append("}");
        return text.ToString();
    }

    internal string ShellIdentifier()
    {
        string identifier = "_args_";
        int index = 0;
        while (index < RootCommand.Name.Length)
        {
            char value = RootCommand.Name[index];
            if ((value >= 'a' && value <= 'z') || (value >= 'A' && value <= 'Z') || (value >= '0' && value <= '9') || value == '_')
            {
                identifier = identifier + value;
            }
            else
            {
                identifier = identifier + "_";
            }

            index = index + 1;
        }

        return identifier + "_completion";
    }

    internal bool ScriptMatchingCaseInsensitive()
    {
        if (Provider.Settings.CommandMatchingCaseInsensitive())
        {
            return true;
        }

        return Provider.Settings.OptionMatchingCaseInsensitive();
    }

    internal string PosixShellQuote(string value)
    {
        return "'" + value.Replace("'", "'\\''") + "'";
    }

    internal string FishShellQuote(string value)
    {
        return "'" + value.Replace("\\", "\\\\").Replace("'", "\\'") + "'";
    }

    internal string FishCompletionText(string value)
    {
        string text = "";
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (char.IsWhiteSpace(current))
            {
                text = text + "\\" + current;
            }
            else
            {
                text = text + current;
            }

            index = index + 1;
        }

        return text;
    }

    internal string PowerShellQuote(string value)
    {
        return "'" + value.Replace("'", "''") + "'";
    }
}
