namespace pop3;

internal class Pop3CapabilityBuilder
{
    Pop3CapabilityParser Parser = new Pop3CapabilityParser();

    internal Pop3CapabilitySet BuildCapabilities(
        Pop3ServerOptions options,
        Pop3ServerStateMachine stateMachine)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (stateMachine == null)
        {
            throw new System.ArgumentNullException("stateMachine");
        }
        Pop3SessionState state = stateMachine.GetCurrentState();
        ValidateState(state);
        ValidateCollections(options);

        System.Collections.Generic.List<Pop3Capability> entries =
            new System.Collections.Generic.List<Pop3Capability>();
        AddBuiltInCapabilities(entries, options, stateMachine);
        ValidateEntryFormatting(entries, state);
        Pop3Capability[] builtInEntries = entries.ToArray();
        Pop3CapabilitySet builtInSet = Parser.InterpretCapabilities(
            builtInEntries,
            state);
        AddExtensionCapabilities(
            entries,
            options.Extensions,
            stateMachine,
            builtInSet,
            state);
        ValidateEntryFormatting(entries, state);
        return Parser.InterpretCapabilities(entries.ToArray(), state);
    }

    void AddBuiltInCapabilities(
        System.Collections.Generic.List<Pop3Capability> entries,
        Pop3ServerOptions options,
        Pop3ServerStateMachine stateMachine)
    {
        bool credentialsAllowed = stateMachine.GetIsSecure() ||
            options.AllowUnprotectedCredentials &&
            options.SecurityMode != Pop3SecurityMode.StlsRequired;
        if (options.EnableTop && options.MailboxProvider != null)
        {
            entries.Add(CreateCapability(Pop3Capabilities.Top));
        }
        if (options.EnableUserPassword &&
            options.AuthenticationProvider != null &&
            credentialsAllowed)
        {
            entries.Add(CreateCapability(Pop3Capabilities.User));
        }

        string[] mechanisms = GetSaslMechanisms(
            options.SaslFactories,
            credentialsAllowed,
            options.AuthenticationProvider != null);
        if (mechanisms.Length > 0)
        {
            entries.Add(CreateCapabilityWithParameters(
                Pop3Capabilities.Sasl,
                mechanisms));
        }
        if (options.EnableResponseCodes)
        {
            entries.Add(CreateCapability(Pop3Capabilities.ResponseCodes));
        }
        if (options.EnablePipelining)
        {
            entries.Add(CreateCapability(Pop3Capabilities.Pipelining));
        }
        if (options.EnableUidl && options.MailboxProvider != null)
        {
            entries.Add(CreateCapability(Pop3Capabilities.Uidl));
        }
        if (options.ImplementationText.Length > 0)
        {
            entries.Add(CreateImplementationCapability(
                options.ImplementationText));
        }
        if (CanAdvertiseStls(options, stateMachine))
        {
            entries.Add(CreateCapability(Pop3Capabilities.Stls));
        }
        if (options.EnableResponseCodes &&
            options.AuthenticationProvider != null)
        {
            entries.Add(CreateCapability(
                Pop3Capabilities.AuthenticationResponseCode));
        }
        if (options.EnableUtf8)
        {
            Pop3Capability utf8 = CreateCapability(Pop3Capabilities.Utf8);
            if (options.EnableUtf8User &&
                options.AuthenticationProvider != null &&
                (options.EnableUserPassword || options.EnableApop))
            {
                utf8.Parameters = new string[] { "USER" };
            }
            entries.Add(utf8);
        }
        if (options.LanguageProvider != null)
        {
            entries.Add(CreateCapability(Pop3Capabilities.Lang));
        }
    }

    void AddExtensionCapabilities(
        System.Collections.Generic.List<Pop3Capability> entries,
        IPop3ServerExtension[] extensions,
        Pop3ServerStateMachine stateMachine,
        Pop3CapabilitySet builtInSet,
        Pop3SessionState state)
    {
        int extensionIndex = 0;
        while (extensionIndex < extensions.Length)
        {
            IPop3ServerExtension extension = extensions[extensionIndex];
            if (extension == null)
            {
                throw new System.ArgumentException(
                    "A POP3 server extension cannot be null.",
                    "options");
            }
            Pop3ExtensionContext context = stateMachine.CreateExtensionContext(
                CopyCapabilitySet(builtInSet, state));
            Pop3Capability[] supplied = extension.GetCapabilities(context);
            if (supplied == null)
            {
                throw new System.IO.InvalidDataException(
                    "A POP3 server extension returned null capabilities.");
            }

            int capabilityIndex = 0;
            while (capabilityIndex < supplied.Length)
            {
                Pop3Capability normalized = NormalizeCapability(
                    supplied[capabilityIndex],
                    state);
                if (IsManagedCapability(normalized.Name) ||
                    ContainsCapability(entries, normalized.Name))
                {
                    throw new System.IO.InvalidDataException(
                        "A POP3 server extension capability conflicts with another capability.");
                }
                entries.Add(normalized);
                capabilityIndex++;
            }
            extensionIndex++;
        }
    }

    string[] GetSaslMechanisms(
        IPop3SaslServerFactory[] factories,
        bool credentialsAllowed,
        bool hasAuthenticationProvider)
    {
        System.Collections.Generic.List<string> mechanisms =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < factories.Length)
        {
            IPop3SaslServerFactory factory = factories[index];
            if (factory == null)
            {
                throw new System.ArgumentException(
                    "A POP3 SASL server factory cannot be null.",
                    "options");
            }
            string mechanism = ValidateMechanism(factory.GetMechanism());
            if ((mechanism != "PLAIN" || credentialsAllowed) &&
                !ContainsMechanism(mechanisms, mechanism))
            {
                mechanisms.Add(mechanism);
            }
            index++;
        }
        if (credentialsAllowed &&
            hasAuthenticationProvider &&
            !ContainsMechanism(mechanisms, "PLAIN"))
        {
            mechanisms.Add("PLAIN");
        }
        return mechanisms.ToArray();
    }

    string ValidateMechanism(string mechanism)
    {
        if (mechanism == null ||
            mechanism.Length == 0 ||
            mechanism.Length > 20)
        {
            throw new System.IO.InvalidDataException(
                "A POP3 SASL mechanism name must contain from one through twenty characters.");
        }
        int index = 0;
        while (index < mechanism.Length)
        {
            char character = mechanism[index];
            bool valid = character >= 'A' && character <= 'Z' ||
                character >= 'a' && character <= 'z' ||
                character >= '0' && character <= '9' ||
                character == '-' || character == '_';
            if (!valid)
            {
                throw new System.IO.InvalidDataException(
                    "A POP3 SASL mechanism name contains an invalid character.");
            }
            index++;
        }
        return mechanism.ToUpperInvariant();
    }

    bool ContainsMechanism(
        System.Collections.Generic.List<string> mechanisms,
        string mechanism)
    {
        int index = 0;
        while (index < mechanisms.Count)
        {
            if (string.Equals(
                mechanisms[index],
                mechanism,
                System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    bool CanAdvertiseStls(
        Pop3ServerOptions options,
        Pop3ServerStateMachine stateMachine)
    {
        return stateMachine.GetCurrentState() == Pop3SessionState.Authorization &&
            !stateMachine.GetIsSecure() &&
            (options.SecurityMode == Pop3SecurityMode.StlsIfAvailable ||
                options.SecurityMode == Pop3SecurityMode.StlsRequired);
    }

    Pop3Capability CreateCapability(string name)
    {
        Pop3Capability capability = new Pop3Capability();
        capability.Name = name;
        return capability;
    }

    Pop3Capability CreateCapabilityWithParameters(
        string name,
        string[] parameters)
    {
        Pop3Capability capability = CreateCapability(name);
        capability.Parameters = parameters;
        return capability;
    }

    Pop3Capability CreateImplementationCapability(string text)
    {
        ValidateImplementationText(text);
        return CreateCapabilityWithParameters(
            Pop3Capabilities.Implementation,
            text.Split(' ', System.StringSplitOptions.None));
    }

    void ValidateImplementationText(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character == ' ')
            {
                if (index == 0 ||
                    index == text.Length - 1 ||
                    text[index - 1] == ' ')
                {
                    throw new System.IO.InvalidDataException(
                        "The POP3 implementation text must use visible ASCII separated by single spaces.");
                }
            }
            else if (character < 33 || character > 126)
            {
                throw new System.IO.InvalidDataException(
                    "The POP3 implementation text must use visible ASCII separated by single spaces.");
            }
            index++;
        }
    }

    Pop3Capability NormalizeCapability(
        Pop3Capability capability,
        Pop3SessionState state)
    {
        byte[] formatted = Parser.FormatCapability(capability, state);
        return Parser.ParseCapability(formatted);
    }

    void ValidateEntryFormatting(
        System.Collections.Generic.List<Pop3Capability> entries,
        Pop3SessionState state)
    {
        int index = 0;
        while (index < entries.Count)
        {
            Parser.FormatCapability(entries[index], state);
            index++;
        }
    }

    bool ContainsCapability(
        System.Collections.Generic.List<Pop3Capability> entries,
        string name)
    {
        int index = 0;
        while (index < entries.Count)
        {
            if (string.Equals(
                entries[index].Name,
                name,
                System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    bool IsManagedCapability(string name)
    {
        return NameEquals(name, Pop3Capabilities.Top) ||
            NameEquals(name, Pop3Capabilities.User) ||
            NameEquals(name, Pop3Capabilities.Sasl) ||
            NameEquals(name, Pop3Capabilities.ResponseCodes) ||
            NameEquals(name, Pop3Capabilities.Pipelining) ||
            NameEquals(name, Pop3Capabilities.Uidl) ||
            NameEquals(name, Pop3Capabilities.Implementation) ||
            NameEquals(name, Pop3Capabilities.Stls) ||
            NameEquals(name, Pop3Capabilities.AuthenticationResponseCode) ||
            NameEquals(name, Pop3Capabilities.Utf8) ||
            NameEquals(name, Pop3Capabilities.Lang);
    }

    bool NameEquals(string first, string second)
    {
        return string.Equals(
            first,
            second,
            System.StringComparison.OrdinalIgnoreCase);
    }

    Pop3CapabilitySet CopyCapabilitySet(
        Pop3CapabilitySet source,
        Pop3SessionState state)
    {
        Pop3Capability[] entries = new Pop3Capability[source.Entries.Length];
        int index = 0;
        while (index < source.Entries.Length)
        {
            entries[index] = NormalizeCapability(source.Entries[index], state);
            index++;
        }
        return Parser.InterpretCapabilities(entries, state);
    }

    void ValidateCollections(Pop3ServerOptions options)
    {
        if (options.SaslFactories == null)
        {
            throw new System.ArgumentException(
                "POP3 SASL server factories cannot be null.",
                "options");
        }
        if (options.Extensions == null)
        {
            throw new System.ArgumentException(
                "POP3 server extensions cannot be null.",
                "options");
        }
        if (options.ImplementationText == null)
        {
            throw new System.ArgumentException(
                "POP3 implementation text cannot be null.",
                "options");
        }
    }

    void ValidateState(Pop3SessionState state)
    {
        if (state != Pop3SessionState.Authorization &&
            state != Pop3SessionState.Transaction)
        {
            throw new Pop3StateException(
                "POP3 capabilities are available only in active command states.",
                Pop3Commands.Capa,
                state);
        }
    }
}
