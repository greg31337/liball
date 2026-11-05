namespace args;

/// <summary>
/// Defines a command-line application and provides its parse, validation, and invocation entry points.
/// </summary>
public class ArgApp : IArgApp
{
    /// <summary>
    /// The application name used by the root command and generated usage.
    /// </summary>
    internal string Name;
    /// <summary>
    /// The longer description displayed in generated help.
    /// </summary>
    internal string DescriptionText;
    /// <summary>
    /// The version text displayed for a version request.
    /// </summary>
    internal string VersionText;
    /// <summary>
    /// The dynamic version text provider, or null when static version text is used.
    /// </summary>
    internal System.Func<string>? VersionProviderFunc;
    /// <summary>
    /// The root command that owns the application's commands, options, and arguments.
    /// </summary>
    internal ArgCommand RootCommand;
    /// <summary>
    /// The settings used by parsing, validation, and generated run output.
    /// </summary>
    internal ArgParserSettings Settings;

    /// <summary>
    /// Creates an application whose root command uses the supplied name.
    /// </summary>
    /// <param name="name">The executable-style name used for the root command and generated usage.</param>
    /// <exception cref="System.ArgumentException">The name is empty, starts with an option prefix, or contains whitespace.</exception>
    public ArgApp(string name)
    {
        ArgName.ValidateCommandName(name);
        Name = name;
        DescriptionText = "";
        VersionText = "";
        VersionProviderFunc = null;
        RootCommand = new ArgCommand(name);
        Settings = new ArgParserSettings();
    }

    /// <summary>
    /// Replaces the parser settings used by this application and returns the same object.
    /// </summary>
    /// <param name="settings">The settings instance to retain.</param>
    /// <returns>The same application instance.</returns>
    /// <exception cref="System.ArgumentNullException">The settings object is null.</exception>
    public ArgApp UseSettings(ArgParserSettings settings)
    {
        if (settings == null)
        {
            throw new System.ArgumentNullException("settings");
        }

        Settings = settings;
        return this;
    }

    /// <summary>
    /// Returns the application name.
    /// </summary>
    /// <returns>The application name.</returns>
    public string GetName()
    {
        return Name;
    }

    /// <summary>
    /// Returns the configured application description.
    /// </summary>
    /// <returns>The description text.</returns>
    public string GetDescriptionText()
    {
        return DescriptionText;
    }

    /// <summary>
    /// Returns the configured application version.
    /// </summary>
    /// <returns>The version text.</returns>
    /// <exception cref="System.InvalidOperationException">The configured provider returns null.</exception>
    public string GetVersionText()
    {
        return ArgVersionText.ResolveCommand(RootCommand);
    }

    /// <summary>
    /// Returns the configured dynamic version text provider.
    /// </summary>
    /// <returns>The retained provider, or null when static version text is used.</returns>
    public System.Func<string>? GetVersionProvider()
    {
        return VersionProviderFunc;
    }

    /// <summary>
    /// Returns the root command.
    /// </summary>
    /// <returns>The root command.</returns>
    public ArgCommand GetRootCommand()
    {
        return RootCommand;
    }

    /// <summary>
    /// Returns the active parser settings.
    /// </summary>
    /// <returns>The retained settings instance.</returns>
    public ArgParserSettings GetSettings()
    {
        return Settings;
    }

    /// <summary>
    /// Sets the description shown in generated help and returns the same object for fluent configuration.
    /// </summary>
    /// <param name="text">The application description shown in root help.</param>
    /// <returns>The same ArgApp instance so additional configuration calls can be chained.</returns>
    public ArgApp Description(string text)
    {
        DescriptionText = text;
        RootCommand.DescriptionText = text;
        return this;
    }

    /// <summary>
    /// Sets the version text returned by built-in version handling.
    /// </summary>
    /// <param name="text">The version text to display, such as <c>1.2.3</c>.</param>
    /// <returns>The same ArgApp instance so additional configuration calls can be chained.</returns>
    /// <exception cref="System.ArgumentNullException">The version text is null.</exception>
    public ArgApp Version(string text)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        VersionText = text;
        VersionProviderFunc = null;
        RootCommand.VersionText = text;
        RootCommand.VersionProviderFunc = null;
        return this;
    }

    /// <summary>
    /// Sets the delegate used to resolve version text and returns the same object for fluent configuration.
    /// </summary>
    /// <param name="provider">The synchronous provider invoked when an operation needs version text. It must not return null.</param>
    /// <returns>The same ArgApp instance so additional configuration calls can be chained.</returns>
    /// <remarks>Dynamic and static version configuration replace one another; the most recent call wins.</remarks>
    /// <exception cref="System.ArgumentNullException">The provider is null.</exception>
    public ArgApp VersionProvider(System.Func<string> provider)
    {
        if (provider == null)
        {
            throw new System.ArgumentNullException("provider");
        }

        VersionText = "";
        VersionProviderFunc = provider;
        RootCommand.VersionText = "";
        RootCommand.VersionProviderFunc = provider;
        return this;
    }

    /// <summary>
    /// Creates, registers, and returns a child command with the supplied name.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The newly registered <c>ArgCommand</c>.</returns>
    /// <exception cref="System.ArgumentException">The name is not a valid command name.</exception>
    /// <exception cref="System.InvalidOperationException">The name conflicts with an existing root child name or alias.</exception>
    public ArgCommand Command(string name)
    {
        return RootCommand.Command(name);
    }

    /// <summary>
    /// Creates, registers, and returns a command-local option with the supplied alias.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The newly registered <c>ArgOption</c>.</returns>
    /// <exception cref="System.ArgumentException">The name is not a valid option alias.</exception>
    /// <exception cref="System.InvalidOperationException">The alias conflicts with an existing root option.</exception>
    public ArgOption Option(string name)
    {
        return RootCommand.Option(name);
    }

    /// <summary>
    /// Creates, appends, and returns a positional argument definition.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The newly registered <c>ArgArgument</c>.</returns>
    /// <exception cref="System.ArgumentException">The name is not a valid argument name.</exception>
    /// <exception cref="System.InvalidOperationException">The name duplicates an existing root positional argument.</exception>
    public ArgArgument Argument(string name)
    {
        return RootCommand.Argument(name);
    }

    /// <summary>
    /// Tokenizes the supplied arguments, resolves the command path, and returns the parse state without invoking a handler.
    /// </summary>
    /// <param name="argv">The raw command-line arguments, excluding the executable name.</param>
    /// <returns>A parse result containing classified tokens, resolved occurrences, and parse errors.</returns>
    /// <exception cref="System.ArgumentNullException">The argument array is null.</exception>
    public ArgParseResult Parse(string[] argv)
    {
        return ArgParser.WithSettings(RootCommand, Settings).Parse(argv);
    }

    /// <summary>
    /// Parses and validates the supplied arguments without invoking a handler.
    /// </summary>
    /// <param name="argv">The raw command-line arguments, excluding the executable name.</param>
    /// <returns>A validation result that includes parse errors and semantic validation errors.</returns>
    /// <exception cref="System.ArgumentNullException">The argument array is null.</exception>
    public ArgValidationResult Validate(string[] argv)
    {
        ArgParseResult parseResult = Parse(argv);
        return new ArgValidator().Validate(parseResult);
    }

    /// <summary>
    /// Parses and validates the supplied arguments, handles help or version requests, and invokes the selected handler when valid.
    /// </summary>
    /// <param name="argv">The raw command-line arguments, excluding the executable name.</param>
    /// <returns>A run result containing parse, validation, handler, output, and exit-code state.</returns>
    /// <exception cref="System.ArgumentNullException">The argument array is null.</exception>
    public ArgRunResult Run(string[] argv)
    {
        ArgParseResult parseResult = Parse(argv);
        ArgValidationResult validationResult = new ArgValidator().Validate(parseResult);
        return new ArgInvoker().Invoke(parseResult, validationResult);
    }
}
