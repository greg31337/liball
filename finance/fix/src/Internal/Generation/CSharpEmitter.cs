using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace fix;

internal static class CSharpEmitter
{
    internal static CSharpEmissionResult EmitSources(FixDictionary dictionary, ModelGenerationOptions options, CSharpNamePlan names)
    {
        CSharpEmissionResult result = new CSharpEmissionResult();
        if (dictionary == null)
        {
            AddEmissionDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-emission-dictionary", "The dictionary for C# source emission is missing.", "/");
            return result;
        }
        if (options == null)
        {
            AddEmissionDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-emission-options", "The C# source emission options are missing.", "/options");
            return result;
        }
        if (names == null)
        {
            AddEmissionDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-emission-names", "The C# name plan is missing.", "/names");
            return result;
        }

        CopyNameDiagnostics(names, result.Diagnostics);
        if (names.VersionNamespace.Length == 0)
        {
            AddEmissionDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-emission-namespace", "The C# name plan has no version namespace.", "/names/version-namespace");
            return result;
        }
        if (!options.EmitDocumentation) AddEmissionDiagnostic(result.Diagnostics, ValidationSeverity.Warning, "documentation-required", "XML documentation was emitted because generated public symbols require it.", "/options/emit-documentation");
        string prefix = ResolveEmissionPrefix(options.TypePrefix);
        EmitFieldConstants(dictionary, names, prefix, result);
        EmitCodeValues(dictionary, names, prefix, result);
        EmitComponentModels(dictionary, names, prefix, result);
        EmitGroupModels(dictionary, names, prefix, result);
        EmitMessageModelsAndOperations(dictionary, names, prefix, result);
        EmitMessageCrackers(dictionary, names, prefix, result);
        result.Files.Sort((left, right) => string.CompareOrdinal(left.Path, right.Path));
        RemoveDuplicateEmissionPaths(result);
        return result;
    }

    internal static void EmitFieldConstants(FixDictionary dictionary, CSharpNamePlan names, string prefix, CSharpEmissionResult result)
    {
        List<FixFieldDefinition> definitions = new List<FixFieldDefinition>(dictionary.Fields);
        definitions.Sort((left, right) => CSharpNameResolver.CompareFieldDefinitions(left, right));
        string typeName = prefix + "FixFieldTags";
        StringBuilder builder = CreateSourceBuilder(names.VersionNamespace + ".Fields");
        AppendSummary(builder, "Defines the numeric tags in the selected FIX dictionary.");
        builder.Append("public static class ").Append(typeName).AppendLine();
        builder.AppendLine("{");
        List<int> emitted = [];
        for (int index = 0; index < definitions.Count; index++)
        {
            FixFieldDefinition definition = definitions[index];
            if (ContainsInteger(emitted, definition.Tag)) continue;
            emitted.Add(definition.Tag);
            string? fieldName;
            if (!names.FieldNames.TryGetValue(definition.Tag, out fieldName))
            {
                AddEmissionDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-field-name", "A field has no resolved C# identifier.", "/fields/" + IntegerText(definition.Tag));
                continue;
            }
            AppendIndentedSummary(builder, DescribeFieldConstant(definition));
            builder.Append("    public const int ").Append(fieldName).Append(" = ").Append(IntegerText(definition.Tag)).AppendLine(";");
        }
        builder.AppendLine("}");
        AddEmittedFile(result, "Fields/" + typeName + ".cs", builder.ToString());
    }

    internal static void EmitCodeValues(FixDictionary dictionary, CSharpNamePlan names, string prefix, CSharpEmissionResult result)
    {
        List<FixCodeSetDefinition> definitions = new List<FixCodeSetDefinition>(dictionary.CodeSets);
        definitions.Sort((left, right) => CSharpNameResolver.CompareCodeSetDefinitions(left, right));
        List<string> emitted = [];
        for (int index = 0; index < definitions.Count; index++)
        {
            FixCodeSetDefinition definition = definitions[index];
            string key = CSharpNameResolver.NormalizeNameKey(definition.Name);
            if (ContainsText(emitted, key)) continue;
            emitted.Add(key);
            string? resolvedName;
            if (!names.CodeSetNames.TryGetValue(key, out resolvedName))
            {
                AddEmissionDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-code-set-name", "A code set has no resolved C# identifier.", "/code-sets/" + definition.Name);
                continue;
            }
            string typeName = prefix + resolvedName;
            StringBuilder builder = CreateSourceBuilder(names.VersionNamespace + ".Codes");
            AppendSummary(builder, DescriptionOrFallback(definition.Description, "Defines the exact wire values for the " + definition.Name + " code set."));
            builder.Append("public static class ").Append(typeName).AppendLine();
            builder.AppendLine("{");
            List<FixCodeDefinition> codes = new List<FixCodeDefinition>(definition.Codes);
            codes.Sort((left, right) => CSharpNameResolver.CompareCodeDefinitions(left, right));
            List<string> emittedValues = [];
            for (int codeIndex = 0; codeIndex < codes.Count; codeIndex++)
            {
                FixCodeDefinition code = codes[codeIndex];
                if (ContainsExactText(emittedValues, code.Value)) continue;
                emittedValues.Add(code.Value);
                string? codeName;
                if (!names.CodeNames.TryGetValue(CSharpNameResolver.CreateCodeKey(definition.Name, code.Value), out codeName))
                {
                    AddEmissionDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-code-name", "A code value has no resolved C# identifier.", "/code-sets/" + definition.Name + "/" + code.Value);
                    continue;
                }
                AppendIndentedSummary(builder, DescriptionOrFallback(code.Description, "Represents the " + code.Name + " wire value."));
                builder.Append("    public const string ").Append(codeName).Append(" = \"").Append(EscapeCSharpString(code.Value)).AppendLine("\";");
            }
            builder.AppendLine("}");
            AddEmittedFile(result, "Codes/" + typeName + ".cs", builder.ToString());
        }
    }

    internal static void EmitComponentModels(FixDictionary dictionary, CSharpNamePlan names, string prefix, CSharpEmissionResult result)
    {
        List<FixComponentDefinition> definitions = new List<FixComponentDefinition>(dictionary.Components);
        definitions.Sort((left, right) => CSharpNameResolver.CompareComponentDefinitions(left, right));
        List<int> emitted = [];
        for (int index = 0; index < definitions.Count; index++)
        {
            FixComponentDefinition definition = definitions[index];
            if (ContainsInteger(emitted, definition.Id)) continue;
            emitted.Add(definition.Id);
            string? resolvedName;
            if (!names.ComponentNames.TryGetValue(definition.Id, out resolvedName))
            {
                AddEmissionDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-component-name", "A component has no resolved C# identifier.", "/components/" + IntegerText(definition.Id));
                continue;
            }
            string typeName = prefix + resolvedName;
            StringBuilder builder = CreateSourceBuilder(names.VersionNamespace + ".Components");
            AppendSummary(builder, DescriptionOrFallback(definition.Description, "Represents the " + definition.Name + " FIX component."));
            builder.Append("public class ").Append(typeName).AppendLine();
            builder.AppendLine("{");
            List<string> memberNames = ["Fields"];
            AppendIndentedSummary(builder, "The component fields in their emitted order.");
            builder.AppendLine("    public global::System.Collections.Generic.List<global::fix.FixField> Fields = [];");
            EmitModelMembers(builder, definition.Members, names, prefix, memberNames, result, "/components/" + IntegerText(definition.Id));
            builder.AppendLine("}");
            AddEmittedFile(result, "Components/" + typeName + ".cs", builder.ToString());
        }
    }

    internal static void EmitGroupModels(FixDictionary dictionary, CSharpNamePlan names, string prefix, CSharpEmissionResult result)
    {
        List<FixGroupDefinition> definitions = new List<FixGroupDefinition>(dictionary.Groups);
        definitions.Sort((left, right) => CSharpNameResolver.CompareGroupDefinitions(left, right));
        List<int> emitted = [];
        for (int index = 0; index < definitions.Count; index++)
        {
            FixGroupDefinition definition = definitions[index];
            if (ContainsInteger(emitted, definition.Id)) continue;
            emitted.Add(definition.Id);
            string? resolvedName;
            if (!names.GroupNames.TryGetValue(definition.Id, out resolvedName))
            {
                AddEmissionDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-group-name", "A group has no resolved C# identifier.", "/groups/" + IntegerText(definition.Id));
                continue;
            }
            string typeName = prefix + resolvedName;
            StringBuilder builder = CreateSourceBuilder(names.VersionNamespace + ".Groups");
            AppendSummary(builder, DescriptionOrFallback(definition.Description, "Represents one " + definition.Name + " repeating-group entry."));
            builder.Append("public class ").Append(typeName).AppendLine();
            builder.AppendLine("{");
            List<string> memberNames = ["Entry"];
            AppendIndentedSummary(builder, "The underlying general FIX group entry.");
            builder.AppendLine("    public global::fix.FixGroupEntry Entry = new global::fix.FixGroupEntry();");
            EmitModelMembers(builder, definition.Members, names, prefix, memberNames, result, "/groups/" + IntegerText(definition.Id));
            builder.AppendLine("}");
            AddEmittedFile(result, "Groups/" + typeName + ".cs", builder.ToString());
        }
    }

    internal static void EmitMessageModelsAndOperations(FixDictionary dictionary, CSharpNamePlan names, string prefix, CSharpEmissionResult result)
    {
        List<FixMessageDefinition> definitions = SortedUniqueMessages(dictionary.Messages);
        for (int index = 0; index < definitions.Count; index++)
        {
            FixMessageDefinition definition = definitions[index];
            string key = CSharpNameResolver.CreateMessageKey(definition.MsgType, definition.Scenario);
            string? resolvedName;
            if (!names.MessageNames.TryGetValue(key, out resolvedName))
            {
                AddEmissionDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-message-name", "A message has no resolved C# identifier.", "/messages/" + definition.MsgType + "/" + definition.Scenario);
                continue;
            }
            string typeName = prefix + resolvedName;
            string scenarioRoot = ResolveScenarioRoot(names, definition.Scenario, result.Diagnostics);
            EmitMessageModel(definition, names, prefix, typeName, scenarioRoot, result);
            EmitMessageOperations(definition, typeName, scenarioRoot, ScenarioPath(names, definition.Scenario), result);
        }
    }

    internal static void EmitMessageModel(FixMessageDefinition definition, CSharpNamePlan names, string prefix, string typeName, string scenarioRoot, CSharpEmissionResult result)
    {
        StringBuilder builder = CreateSourceBuilder(scenarioRoot + ".Messages");
        AppendSummary(builder, DescriptionOrFallback(definition.Description, "Represents the " + definition.Name + " FIX message."));
        builder.Append("public class ").Append(typeName).AppendLine();
        builder.AppendLine("{");
        List<string> memberNames = ["Message"];
        AppendIndentedSummary(builder, "The underlying general FIX message.");
        builder.AppendLine("    public global::fix.FixMessage Message = new global::fix.FixMessage();");
        EmitModelMembers(builder, definition.Members, names, prefix, memberNames, result, "/messages/" + definition.MsgType + "/" + definition.Scenario);
        builder.AppendLine("}");
        AddEmittedFile(result, ScenarioPath(names, definition.Scenario) + "Messages/" + typeName + ".cs", builder.ToString());
    }

    internal static void EmitMessageOperations(FixMessageDefinition definition, string typeName, string scenarioRoot, string outputRoot, CSharpEmissionResult result)
    {
        string modelType = "global::" + scenarioRoot + ".Messages." + typeName;
        string interfaceName = ResolveInterfaceName(typeName + "Operations");
        string className = typeName + "Operations";
        string createMethod = "Create" + typeName + "Model";
        string buildMethod = "Build" + typeName + "Message";
        string readMethod = "Read" + typeName + "Message";
        string identifyMethod = "Is" + typeName + "Message";

        StringBuilder contract = CreateSourceBuilder(scenarioRoot + ".Operations");
        AppendSummary(contract, "Defines creation, conversion, and identification operations for the " + definition.Name + " message.");
        contract.Append("public interface ").Append(interfaceName).AppendLine();
        contract.AppendLine("{");
        AppendIndentedSummary(contract, "Creates an empty generated message model with its MsgType initialized.");
        contract.AppendLine("    /// <returns>The initialized generated model.</returns>");
        contract.Append("    ").Append(modelType).Append(' ').Append(createMethod).AppendLine("();");
        AppendIndentedSummary(contract, "Returns the general FIX message stored by a generated model and initializes its MsgType.");
        contract.AppendLine("    /// <param name=\"model\">The generated model to convert.</param>");
        contract.AppendLine("    /// <returns>The underlying general FIX message.</returns>");
        contract.Append("    global::fix.FixMessage ").Append(buildMethod).Append('(').Append(modelType).AppendLine(" model);");
        AppendIndentedSummary(contract, "Wraps a general FIX message in the generated model.");
        contract.AppendLine("    /// <param name=\"message\">The general FIX message to wrap.</param>");
        contract.AppendLine("    /// <returns>The populated generated model.</returns>");
        contract.Append("    ").Append(modelType).Append(' ').Append(readMethod).AppendLine("(global::fix.FixMessage message);");
        AppendIndentedSummary(contract, "Determines whether a general FIX message has the expected MsgType.");
        contract.AppendLine("    /// <param name=\"message\">The general FIX message to inspect.</param>");
        contract.AppendLine("    /// <returns><c>true</c> when the MsgType matches; otherwise <c>false</c>.</returns>");
        contract.Append("    bool ").Append(identifyMethod).AppendLine("(global::fix.FixMessage message);");
        contract.AppendLine("}");
        string operationPath = outputRoot + "Operations/";
        AddEmittedFile(result, operationPath + interfaceName + ".cs", contract.ToString());

        StringBuilder implementation = CreateSourceBuilder(scenarioRoot + ".Operations");
        AppendSummary(implementation, "Implements generated operations for the " + definition.Name + " message.");
        implementation.Append("public class ").Append(className).Append(" : ").Append(interfaceName).AppendLine();
        implementation.AppendLine("{");
        AppendIndentedSummary(implementation, "Creates generated operations for the message.");
        implementation.Append("    public ").Append(className).AppendLine("()");
        implementation.AppendLine("    {");
        implementation.AppendLine("    }");
        AppendIndentedSummary(implementation, "Creates an empty generated message model with its MsgType initialized.");
        implementation.AppendLine("    /// <returns>The initialized generated model.</returns>");
        implementation.Append("    public ").Append(modelType).Append(' ').Append(createMethod).AppendLine("()");
        implementation.AppendLine("    {");
        implementation.Append("        ").Append(modelType).Append(" model = new ").Append(modelType).AppendLine("();");
        implementation.Append("        model.Message.MsgType = \"").Append(EscapeCSharpString(definition.MsgType)).AppendLine("\";");
        implementation.AppendLine("        return model;");
        implementation.AppendLine("    }");
        AppendIndentedSummary(implementation, "Returns the general FIX message stored by a generated model and initializes its MsgType.");
        implementation.AppendLine("    /// <param name=\"model\">The generated model to convert.</param>");
        implementation.AppendLine("    /// <returns>The underlying general FIX message.</returns>");
        implementation.Append("    public global::fix.FixMessage ").Append(buildMethod).Append('(').Append(modelType).AppendLine(" model)");
        implementation.AppendLine("    {");
        implementation.Append("        model.Message.MsgType = \"").Append(EscapeCSharpString(definition.MsgType)).AppendLine("\";");
        implementation.AppendLine("        return model.Message;");
        implementation.AppendLine("    }");
        AppendIndentedSummary(implementation, "Wraps a general FIX message in the generated model.");
        implementation.AppendLine("    /// <param name=\"message\">The general FIX message to wrap.</param>");
        implementation.AppendLine("    /// <returns>The populated generated model.</returns>");
        implementation.Append("    public ").Append(modelType).Append(' ').Append(readMethod).AppendLine("(global::fix.FixMessage message)");
        implementation.AppendLine("    {");
        implementation.Append("        ").Append(modelType).Append(" model = new ").Append(modelType).AppendLine("();");
        implementation.AppendLine("        model.Message = message;");
        implementation.AppendLine("        return model;");
        implementation.AppendLine("    }");
        AppendIndentedSummary(implementation, "Determines whether a general FIX message has the expected MsgType.");
        implementation.AppendLine("    /// <param name=\"message\">The general FIX message to inspect.</param>");
        implementation.AppendLine("    /// <returns><c>true</c> when the MsgType matches; otherwise <c>false</c>.</returns>");
        implementation.Append("    public bool ").Append(identifyMethod).AppendLine("(global::fix.FixMessage message)");
        implementation.AppendLine("    {");
        implementation.Append("        return message.MsgType == \"").Append(EscapeCSharpString(definition.MsgType)).AppendLine("\";");
        implementation.AppendLine("    }");
        implementation.AppendLine("}");
        AddEmittedFile(result, operationPath + className + ".cs", implementation.ToString());
    }

    internal static void EmitMessageCrackers(FixDictionary dictionary, CSharpNamePlan names, string prefix, CSharpEmissionResult result)
    {
        List<FixMessageDefinition> definitions = SortedUniqueMessages(dictionary.Messages);
        List<string> scenarioKeys = [];
        for (int index = 0; index < definitions.Count; index++)
        {
            string key = CSharpNameResolver.NormalizeNameKey(definitions[index].Scenario);
            if (key.Length == 0) key = "BASE";
            if (!ContainsExactText(scenarioKeys, key)) scenarioKeys.Add(key);
        }
        scenarioKeys.Sort((left, right) => string.CompareOrdinal(left, right));
        for (int scenarioIndex = 0; scenarioIndex < scenarioKeys.Count; scenarioIndex++)
        {
            string scenarioKey = scenarioKeys[scenarioIndex];
            List<FixMessageDefinition> scenarioMessages = [];
            for (int messageIndex = 0; messageIndex < definitions.Count; messageIndex++)
            {
                string messageKey = CSharpNameResolver.NormalizeNameKey(definitions[messageIndex].Scenario);
                if (messageKey.Length == 0) messageKey = "BASE";
                if (messageKey == scenarioKey) scenarioMessages.Add(definitions[messageIndex]);
            }
            string scenarioText = scenarioMessages.Count == 0 ? "" : scenarioMessages[0].Scenario;
            EmitScenarioCracker(scenarioMessages, names, prefix, scenarioText, result);
        }
    }

    internal static void EmitScenarioCracker(List<FixMessageDefinition> messages, CSharpNamePlan names, string prefix, string scenario, CSharpEmissionResult result)
    {
        string scenarioRoot = ResolveScenarioRoot(names, scenario, result.Diagnostics);
        string crackerNamespace = scenarioRoot + ".Cracking";
        string handlerName = ResolveInterfaceName(prefix + "GeneratedMessageHandler");
        string contractName = ResolveInterfaceName(prefix + "GeneratedMessageCracker");
        string className = prefix + "GeneratedMessageCracker";
        string path = ScenarioPath(names, scenario) + "Cracking/";

        StringBuilder handler = CreateSourceBuilder(crackerNamespace);
        AppendSummary(handler, "Receives strongly named messages dispatched by the generated message cracker.");
        handler.Append("public interface ").Append(handlerName).AppendLine();
        handler.AppendLine("{");
        for (int index = 0; index < messages.Count; index++)
        {
            FixMessageDefinition message = messages[index];
            string? resolvedName;
            if (!names.MessageNames.TryGetValue(CSharpNameResolver.CreateMessageKey(message.MsgType, message.Scenario), out resolvedName)) continue;
            string modelName = prefix + resolvedName;
            AppendIndentedSummary(handler, "Handles an identified " + message.Name + " message.");
            handler.AppendLine("    /// <param name=\"model\">The generated message model.</param>");
            handler.AppendLine("    /// <returns>The application's processing decision.</returns>");
            handler.Append("    global::fix.ApplicationDecision Handle").Append(modelName).Append("Message(global::").Append(scenarioRoot).Append(".Messages.").Append(modelName).AppendLine(" model);");
        }
        handler.AppendLine("}");
        AddEmittedFile(result, path + handlerName + ".cs", handler.ToString());

        StringBuilder contract = CreateSourceBuilder(crackerNamespace);
        AppendSummary(contract, "Defines deterministic MsgType dispatch for the generated message set.");
        contract.Append("public interface ").Append(contractName).AppendLine();
        contract.AppendLine("{");
        AppendIndentedSummary(contract, "Dispatches a general FIX message to its strongly named handler method.");
        contract.AppendLine("    /// <param name=\"message\">The general FIX message to dispatch.</param>");
        contract.AppendLine("    /// <param name=\"handler\">The generated dispatch target.</param>");
        contract.AppendLine("    /// <returns>The handler decision or an explicit rejection for an unknown MsgType.</returns>");
        contract.Append("    global::fix.ApplicationDecision CrackGeneratedMessage(global::fix.FixMessage message, ").Append(handlerName).AppendLine(" handler);");
        contract.AppendLine("}");
        AddEmittedFile(result, path + contractName + ".cs", contract.ToString());

        StringBuilder implementation = CreateSourceBuilder(crackerNamespace);
        AppendSummary(implementation, "Dispatches general FIX messages to strongly named generated handlers.");
        implementation.Append("public class ").Append(className).Append(" : ").Append(contractName).AppendLine();
        implementation.AppendLine("{");
        AppendIndentedSummary(implementation, "Creates a generated message cracker.");
        implementation.Append("    public ").Append(className).AppendLine("()");
        implementation.AppendLine("    {");
        implementation.AppendLine("    }");
        AppendIndentedSummary(implementation, "Dispatches a general FIX message to its strongly named handler method.");
        implementation.AppendLine("    /// <param name=\"message\">The general FIX message to dispatch.</param>");
        implementation.AppendLine("    /// <param name=\"handler\">The generated dispatch target.</param>");
        implementation.AppendLine("    /// <returns>The handler decision or an explicit rejection for an unknown MsgType.</returns>");
        implementation.Append("    public global::fix.ApplicationDecision CrackGeneratedMessage(global::fix.FixMessage message, ").Append(handlerName).AppendLine(" handler)");
        implementation.AppendLine("    {");
        for (int index = 0; index < messages.Count; index++)
        {
            FixMessageDefinition message = messages[index];
            string? resolvedName;
            if (!names.MessageNames.TryGetValue(CSharpNameResolver.CreateMessageKey(message.MsgType, message.Scenario), out resolvedName)) continue;
            string modelName = prefix + resolvedName;
            implementation.Append("        if (message.MsgType == \"").Append(EscapeCSharpString(message.MsgType)).AppendLine("\")");
            implementation.AppendLine("        {");
            implementation.Append("            global::").Append(scenarioRoot).Append(".Messages.").Append(modelName).Append(" model = new global::").Append(scenarioRoot).Append(".Messages.").Append(modelName).AppendLine("();");
            implementation.AppendLine("            model.Message = message;");
            implementation.Append("            return handler.Handle").Append(modelName).AppendLine("Message(model);");
            implementation.AppendLine("        }");
        }
        implementation.AppendLine("        global::fix.ApplicationDecision decision = new global::fix.ApplicationDecision();");
        implementation.AppendLine("        decision.Disposition = global::fix.MessageDisposition.Reject;");
        implementation.AppendLine("        decision.Code = \"unsupported-generated-message\";");
        implementation.AppendLine("        decision.Text = \"The MsgType is not part of this generated message set.\";");
        implementation.AppendLine("        return decision;");
        implementation.AppendLine("    }");
        implementation.AppendLine("}");
        AddEmittedFile(result, path + className + ".cs", implementation.ToString());
    }

    internal static void EmitModelMembers(StringBuilder builder, List<FixMemberDefinition> members, CSharpNamePlan names, string prefix, List<string> usedNames, CSharpEmissionResult result, string sourceRoot)
    {
        List<FixMemberDefinition> definitions = new List<FixMemberDefinition>(members);
        definitions.Sort((left, right) => CompareMemberDefinitions(left, right));
        for (int index = 0; index < definitions.Count; index++)
        {
            FixMemberDefinition member = definitions[index];
            string typeName = ResolveMemberType(member, names, prefix);
            if (typeName.Length == 0)
            {
                AddEmissionDiagnostic(result.Diagnostics, ValidationSeverity.Error, "unresolved-model-member", "A generated data-model member references a missing definition.", sourceRoot + "/members/" + IntegerText(member.Position));
                continue;
            }
            string fallback = ResolveMemberFallback(member, names);
            string fieldName = CSharpNameResolver.CreateRawSymbolIdentifier(member.Name, fallback, "Value");
            string unique = CSharpNameResolver.AssignUniqueIdentifier(fieldName, "Member" + IntegerText(member.Position) + "Ref" + IntegerText(member.ReferenceId), usedNames);
            AppendIndentedSummary(builder, DescribeModelMember(member));
            builder.Append("    public ").Append(typeName).Append(' ').Append(unique).Append(" = ").Append(MemberInitializer(member, typeName)).AppendLine(";");
        }
    }

    internal static string ResolveMemberType(FixMemberDefinition member, CSharpNamePlan names, string prefix)
    {
        if (member.Kind == FixMemberKind.Field)
        {
            string? fieldName;
            return names.FieldNames.TryGetValue(member.ReferenceId, out fieldName) ? "global::fix.FixField" : "";
        }
        if (member.Kind == FixMemberKind.Component)
        {
            string? componentName;
            return names.ComponentNames.TryGetValue(member.ReferenceId, out componentName) ? "global::" + names.VersionNamespace + ".Components." + prefix + componentName : "";
        }
        if (member.Kind == FixMemberKind.Group)
        {
            string? groupName;
            return names.GroupNames.TryGetValue(member.ReferenceId, out groupName) ? "global::System.Collections.Generic.List<global::" + names.VersionNamespace + ".Groups." + prefix + groupName + ">" : "";
        }
        return "";
    }

    internal static string ResolveMemberFallback(FixMemberDefinition member, CSharpNamePlan names)
    {
        string? value;
        if (member.Kind == FixMemberKind.Field && names.FieldNames.TryGetValue(member.ReferenceId, out value)) return value;
        if (member.Kind == FixMemberKind.Component && names.ComponentNames.TryGetValue(member.ReferenceId, out value)) return value;
        if (member.Kind == FixMemberKind.Group && names.GroupNames.TryGetValue(member.ReferenceId, out value)) return value;
        return "Member" + IntegerText(member.Position);
    }

    internal static string MemberInitializer(FixMemberDefinition member, string typeName)
    {
        if (member.Kind == FixMemberKind.Group) return "[]";
        return "new " + typeName + "()";
    }

    internal static string DescribeModelMember(FixMemberDefinition member)
    {
        string requirement = member.Presence == FixPresence.Required ? "required" : "optional";
        string kind = member.Kind == FixMemberKind.Field ? "field" : member.Kind == FixMemberKind.Component ? "component" : "repeating group";
        string name = member.Name.Length == 0 ? kind : member.Name;
        return "The " + requirement + " " + name + " " + kind + ".";
    }

    internal static int CompareMemberDefinitions(FixMemberDefinition left, FixMemberDefinition right)
    {
        int comparison = left.Position.CompareTo(right.Position);
        if (comparison != 0) return comparison;
        comparison = ((int)left.Kind).CompareTo((int)right.Kind);
        if (comparison != 0) return comparison;
        comparison = left.ReferenceId.CompareTo(right.ReferenceId);
        return comparison != 0 ? comparison : string.CompareOrdinal(left.Name, right.Name);
    }

    internal static List<FixMessageDefinition> SortedUniqueMessages(List<FixMessageDefinition> source)
    {
        List<FixMessageDefinition> sorted = new List<FixMessageDefinition>(source);
        sorted.Sort((left, right) => CSharpNameResolver.CompareMessageDefinitions(left, right));
        List<FixMessageDefinition> result = [];
        List<string> keys = [];
        for (int index = 0; index < sorted.Count; index++)
        {
            string key = CSharpNameResolver.CreateMessageKey(sorted[index].MsgType, sorted[index].Scenario);
            if (ContainsExactText(keys, key)) continue;
            keys.Add(key);
            result.Add(sorted[index]);
        }
        return result;
    }

    internal static string ResolveScenarioRoot(CSharpNamePlan names, string scenario, List<GenerationDiagnostic> diagnostics)
    {
        string key = CSharpNameResolver.NormalizeNameKey(scenario);
        if (key.Length == 0) key = "BASE";
        string? value;
        if (names.ScenarioNamespaces.TryGetValue(key, out value)) return value;
        string segment = CSharpNameResolver.CreateRawSymbolIdentifier(scenario, "Scenario", "Scenario") + "Scenario" + CSharpNameResolver.EncodeStableValue(key);
        AddEmissionDiagnostic(diagnostics, ValidationSeverity.Warning, "undeclared-message-scenario", "A message scenario was not declared and received a stable generated namespace.", "/scenarios/" + scenario);
        return CSharpNameResolver.AppendNamespaceSegment(names.VersionNamespace, segment);
    }

    internal static string ScenarioPath(CSharpNamePlan names, string scenario)
    {
        string key = CSharpNameResolver.NormalizeNameKey(scenario);
        if (key.Length == 0 || key == "BASE") return "";
        string? scenarioName;
        if (names.ScenarioNames.TryGetValue(key, out scenarioName)) return "Scenarios/" + scenarioName + "/";
        return "Scenarios/" + CSharpNameResolver.CreateRawSymbolIdentifier(scenario, "Scenario", "Scenario") + "Scenario" + CSharpNameResolver.EncodeStableValue(key) + "/";
    }

    internal static StringBuilder CreateSourceBuilder(string sourceNamespace)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("namespace ").Append(sourceNamespace).AppendLine(";");
        builder.AppendLine();
        return builder;
    }

    internal static void AppendSummary(StringBuilder builder, string text)
    {
        builder.Append("/// <summary>").Append(EscapeXmlText(text)).AppendLine("</summary>");
    }

    internal static void AppendIndentedSummary(StringBuilder builder, string text)
    {
        builder.Append("    /// <summary>").Append(EscapeXmlText(text)).AppendLine("</summary>");
    }

    internal static string DescribeFieldConstant(FixFieldDefinition definition)
    {
        string fallback = "The " + definition.Name + " field tag.";
        return DescriptionOrFallback(definition.Description, fallback);
    }

    internal static string DescriptionOrFallback(string description, string fallback)
    {
        string value = description == null ? "" : description.Trim();
        return value.Length == 0 ? fallback : value;
    }

    internal static string EscapeXmlText(string value)
    {
        StringBuilder builder = new StringBuilder();
        bool previousSpace = false;
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (char.IsWhiteSpace(character))
            {
                if (!previousSpace) builder.Append(' ');
                previousSpace = true;
            }
            else
            {
                previousSpace = false;
                if (character == '&') builder.Append("&amp;");
                else if (character == '<') builder.Append("&lt;");
                else if (character == '>') builder.Append("&gt;");
                else builder.Append(character);
            }
        }
        return builder.ToString().Trim();
    }

    internal static string EscapeCSharpString(string value)
    {
        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (character == '\\') builder.Append("\\\\");
            else if (character == '\"') builder.Append("\\\"");
            else if (character == '\r') builder.Append("\\r");
            else if (character == '\n') builder.Append("\\n");
            else if (character == '\t') builder.Append("\\t");
            else if (character == '\0') builder.Append("\\0");
            else if (character < ' ' || character == '\u2028' || character == '\u2029' || char.IsSurrogate(character))
            {
                builder.Append("\\u");
                builder.Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
            }
            else builder.Append(character);
        }
        return builder.ToString();
    }

    internal static string ResolveEmissionPrefix(string value)
    {
        string source = value == null ? "" : value.Trim();
        return source.Length == 0 ? "" : CSharpNameResolver.CreateRawSymbolIdentifier(source, "Generated", "Type");
    }

    internal static string ResolveInterfaceName(string body)
    {
        if (body.Length != 0 && body[0] >= 'A' && body[0] <= 'Z') return "I" + body;
        return "IGenerated" + body;
    }

    internal static void AddEmittedFile(CSharpEmissionResult result, string path, string content)
    {
        GeneratedFile file = new GeneratedFile();
        file.Path = path;
        file.Content = content;
        result.Files.Add(file);
    }

    internal static void RemoveDuplicateEmissionPaths(CSharpEmissionResult result)
    {
        List<GeneratedFile> unique = [];
        for (int index = 0; index < result.Files.Count; index++)
        {
            GeneratedFile file = result.Files[index];
            if (ContainsFilePath(unique, file.Path))
            {
                AddEmissionDiagnostic(result.Diagnostics, ValidationSeverity.Error, "duplicate-output-path", "Two generated sources resolved to the same output path.", file.Path);
                continue;
            }
            unique.Add(file);
        }
        result.Files = unique;
    }

    internal static bool ContainsFilePath(List<GeneratedFile> files, string path)
    {
        for (int index = 0; index < files.Count; index++) if (string.Equals(files[index].Path, path, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    internal static bool ContainsInteger(List<int> values, int value)
    {
        for (int index = 0; index < values.Count; index++) if (values[index] == value) return true;
        return false;
    }

    internal static bool ContainsText(List<string> values, string value)
    {
        for (int index = 0; index < values.Count; index++) if (string.Equals(values[index], value, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    internal static bool ContainsExactText(List<string> values, string value)
    {
        for (int index = 0; index < values.Count; index++) if (string.Equals(values[index], value, StringComparison.Ordinal)) return true;
        return false;
    }

    internal static string IntegerText(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static void CopyNameDiagnostics(CSharpNamePlan names, List<GenerationDiagnostic> diagnostics)
    {
        for (int index = 0; index < names.Diagnostics.Count; index++) diagnostics.Add(names.Diagnostics[index]);
    }

    internal static void AddEmissionDiagnostic(List<GenerationDiagnostic> diagnostics, ValidationSeverity severity, string code, string message, string source)
    {
        GenerationDiagnostic diagnostic = new GenerationDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Source = source;
        diagnostics.Add(diagnostic);
    }
}
