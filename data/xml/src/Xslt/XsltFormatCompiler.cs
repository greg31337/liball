using System;
using System.Collections.Generic;
using System.Globalization;

namespace xml;

internal class XsltFormatCompiler
{
    internal XsltStylesheetLoader Loader;
    internal static readonly string[] DecimalPropertyNames = ["decimal-separator", "grouping-separator", "infinity", "minus-sign", "exponent-separator", "NaN", "percent", "per-mille", "zero-digit", "digit", "pattern-separator"];
    internal static readonly string[] OutputAttributeNames = ["method", "allow-duplicate-names", "build-tree", "byte-order-mark", "cdata-section-elements", "doctype-public", "doctype-system", "encoding", "escape-uri-attributes", "html-version", "include-content-type", "indent", "item-separator", "json-node-output-method", "media-type", "normalization-form", "omit-xml-declaration", "parameter-document", "standalone", "suppress-indentation", "undeclare-prefixes", "use-character-maps", "version"];

    internal XsltFormatCompiler(XsltStylesheetLoader loader)
    {
        Loader = loader;
    }

    internal void CompileXsltFormats(XsltStylesheetPlan stylesheet)
    {
        stylesheet.DecimalFormats = CompileXsltDecimalFormats(stylesheet);
        AttachXsltDecimalFormats(stylesheet);
        stylesheet.CharacterMaps = CompileXsltCharacterMaps(stylesheet);
        stylesheet.OutputFormats = CompileXsltOutputFormats(stylesheet);
        stylesheet.WhitespaceRules = CompileXsltWhitespaceRules(stylesheet);
    }

    internal XsltDecimalFormatPlan[] CompileXsltDecimalFormats(XsltStylesheetPlan stylesheet)
    {
        List<XsltDecimalFormatDeclarationPlan> declarations = new List<XsltDecimalFormatDeclarationPlan>();
        int index = 0;
        while (index < stylesheet.Declarations.Length)
        {
            XsltStylesheetDeclaration declaration = stylesheet.Declarations[index];
            if (declaration.LocalName == "decimal-format")
            {
                declarations.Add(CreateXsltDecimalFormatDeclaration(declaration));
            }

            index = index + 1;
        }

        List<XsltDecimalFormatPlan> result = new List<XsltDecimalFormatPlan>();
        result.Add(new XsltDecimalFormatPlan(null, "#default"));
        index = 0;
        while (index < declarations.Count)
        {
            XsltDecimalFormatDeclarationPlan declaration = declarations[index];
            XsltDecimalFormatPlan? format = FindXsltDecimalFormat(result, declaration.Name);
            if (format == null)
            {
                format = new XsltDecimalFormatPlan(declaration.ResolvedName, declaration.Name);
                result.Add(format);
            }

            index = index + 1;
        }

        index = 0;
        while (index < result.Count)
        {
            CompleteXsltDecimalFormat(result[index], declarations);
            index = index + 1;
        }

        return result.ToArray();
    }

    internal XsltDecimalFormatDeclarationPlan CreateXsltDecimalFormatDeclaration(XsltStylesheetDeclaration declaration)
    {
        ValidateXsltEmptyFormatDeclaration(declaration.Node, declaration.Module);
        string? lexicalName = Loader.FindXsltAttribute(declaration.Node, "name", "");
        XmlName? resolvedName = lexicalName == null ? null : Loader.StaticProcessor.ResolveXsltStaticVariableName(lexicalName, declaration.Node, declaration.Module);
        if (resolvedName != null && new XsltFunctionCompiler(Loader).IsXsltFunctionReservedNamespace(resolvedName.NamespaceUri == null ? "" : resolvedName.NamespaceUri))
        {
            throw CreateXsltFormatError("A decimal-format name must not use a reserved namespace.", XsltErrorCodes.XTSE0080, declaration.Node, declaration.Module);
        }

        string name = resolvedName == null ? "#default" : CreateXsltExpandedName(resolvedName);
        XsltDecimalFormatDeclarationPlan result = new XsltDecimalFormatDeclarationPlan(declaration.Node, declaration.Module, resolvedName, name);
        result.ImportPrecedence = declaration.ImportPrecedence;
        result.DeclarationOrder = declaration.DeclarationOrder;
        List<XsltDecimalFormatPropertyPlan> properties = new List<XsltDecimalFormatPropertyPlan>();
        int index = 0;
        while (index < DecimalPropertyNames.Length)
        {
            string propertyName = DecimalPropertyNames[index];
            string? value = Loader.FindXsltAttribute(declaration.Node, propertyName, "");
            if (value != null)
            {
                ValidateXsltDecimalFormatProperty(propertyName, value, declaration.Node, declaration.Module);
                properties.Add(new XsltDecimalFormatPropertyPlan(propertyName, value, declaration.Node, declaration.Module, declaration.ImportPrecedence, declaration.DeclarationOrder));
            }

            index = index + 1;
        }

        result.Properties = properties.ToArray();
        return result;
    }

    internal void ValidateXsltDecimalFormatProperty(string name, string value, XmlNode node, XsltStylesheetModule module)
    {
        if (name != "infinity" && name != "NaN")
        {
            int scalar = ReadXsltSingleScalar(value);
            if (scalar < 0)
            {
                throw CreateXsltFormatError("A decimal-format character property must contain one Unicode character.", XsltErrorCodes.XTSE0020, node, module);
            }

            if (name == "zero-digit" && CharUnicodeInfo.GetDecimalDigitValue(value, 0) != 0)
            {
                throw CreateXsltFormatError("The decimal-format zero-digit must have decimal value zero.", XsltErrorCodes.XTSE1295, node, module);
            }
        }
    }

    internal void CompleteXsltDecimalFormat(XsltDecimalFormatPlan format, List<XsltDecimalFormatDeclarationPlan> declarations)
    {
        List<XsltDecimalFormatDeclarationPlan> matching = new List<XsltDecimalFormatDeclarationPlan>();
        int index = 0;
        while (index < declarations.Count)
        {
            if (declarations[index].Name == format.Name)
            {
                matching.Add(declarations[index]);
            }

            index = index + 1;
        }

        format.Declarations = matching.ToArray();
        List<XsltDecimalFormatPropertyPlan> properties = new List<XsltDecimalFormatPropertyPlan>();
        index = 0;
        while (index < DecimalPropertyNames.Length)
        {
            XsltDecimalFormatPropertyPlan? property = SelectXsltDecimalFormatProperty(matching, DecimalPropertyNames[index]);
            if (property != null)
            {
                properties.Add(property);
                SetXsltDecimalFormatProperty(format, property);
            }

            index = index + 1;
        }

        format.Properties = properties.ToArray();
        ValidateXsltDecimalFormatCharacters(format);
    }

    internal XsltDecimalFormatPropertyPlan? SelectXsltDecimalFormatProperty(List<XsltDecimalFormatDeclarationPlan> declarations, string name)
    {
        XsltDecimalFormatPropertyPlan? selected = null;
        int index = 0;
        while (index < declarations.Count)
        {
            XsltDecimalFormatPropertyPlan? candidate = FindXsltDecimalFormatProperty(declarations[index].Properties, name);
            if (candidate != null && (selected == null || candidate.ImportPrecedence > selected.ImportPrecedence))
            {
                selected = candidate;
            }

            index = index + 1;
        }

        index = 0;
        while (index < declarations.Count)
        {
            XsltDecimalFormatPropertyPlan? candidate = FindXsltDecimalFormatProperty(declarations[index].Properties, name);
            if (candidate != null && selected != null && candidate.ImportPrecedence == selected.ImportPrecedence && candidate.Value != selected.Value)
            {
                throw CreateXsltFormatError("Decimal-format declarations specify conflicting values at the same import precedence.", XsltErrorCodes.XTSE1290, candidate.Declaration, candidate.Module);
            }

            if (candidate != null && selected != null && candidate.ImportPrecedence == selected.ImportPrecedence && candidate.DeclarationOrder > selected.DeclarationOrder)
            {
                selected = candidate;
            }

            index = index + 1;
        }

        return selected;
    }

    internal XsltDecimalFormatPropertyPlan? FindXsltDecimalFormatProperty(XsltDecimalFormatPropertyPlan[] properties, string name)
    {
        int index = 0;
        while (index < properties.Length)
        {
            if (properties[index].Name == name)
            {
                return properties[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal void SetXsltDecimalFormatProperty(XsltDecimalFormatPlan format, XsltDecimalFormatPropertyPlan property)
    {
        if (property.Name == "decimal-separator") format.DecimalSeparator = property.Value;
        else if (property.Name == "grouping-separator") format.GroupingSeparator = property.Value;
        else if (property.Name == "infinity") format.Infinity = property.Value;
        else if (property.Name == "minus-sign") format.MinusSign = property.Value;
        else if (property.Name == "exponent-separator") format.ExponentSeparator = property.Value;
        else if (property.Name == "NaN") format.NaN = property.Value;
        else if (property.Name == "percent") format.Percent = property.Value;
        else if (property.Name == "per-mille") format.PerMille = property.Value;
        else if (property.Name == "zero-digit") format.ZeroDigit = property.Value;
        else if (property.Name == "digit") format.Digit = property.Value;
        else if (property.Name == "pattern-separator") format.PatternSeparator = property.Value;
    }

    internal void ValidateXsltDecimalFormatCharacters(XsltDecimalFormatPlan format)
    {
        string[] values = [format.DecimalSeparator, format.GroupingSeparator, format.ExponentSeparator, format.Percent, format.PerMille, format.ZeroDigit, format.Digit, format.PatternSeparator];
        int index = 0;
        while (index < values.Length)
        {
            int other = index + 1;
            while (other < values.Length)
            {
                if (ReadXsltSingleScalar(values[index]) == ReadXsltSingleScalar(values[other]))
                {
                    XmlNode node = format.Declarations.Length == 0 ? Loader.Modules[0].Root : format.Declarations[format.Declarations.Length - 1].Declaration;
                    XsltStylesheetModule module = format.Declarations.Length == 0 ? Loader.Modules[0] : format.Declarations[format.Declarations.Length - 1].Module;
                    throw CreateXsltFormatError("Decimal-format picture characters must be distinct.", XsltErrorCodes.XTSE1300, node, module);
                }

                other = other + 1;
            }

            index = index + 1;
        }
    }

    internal XsltDecimalFormatPlan? FindXsltDecimalFormat(List<XsltDecimalFormatPlan> formats, string name)
    {
        int index = 0;
        while (index < formats.Count)
        {
            if (formats[index].Name == name) return formats[index];
            index = index + 1;
        }

        return null;
    }

    internal void AttachXsltDecimalFormats(XsltStylesheetPlan stylesheet)
    {
        List<XmlName> names = new List<XmlName>();
        int index = 0;
        while (index < stylesheet.DecimalFormats.Length)
        {
            XmlName? name = stylesheet.DecimalFormats[index].ResolvedName;
            if (name != null) names.Add(name);
            index = index + 1;
        }

        index = 0;
        while (index < stylesheet.StaticContexts.Length)
        {
            stylesheet.StaticContexts[index].DecimalFormats = names.ToArray();
            index = index + 1;
        }
    }

    internal XsltCharacterMapPlan[] CompileXsltCharacterMaps(XsltStylesheetPlan stylesheet)
    {
        List<XsltCharacterMapDeclarationPlan> declarations = new List<XsltCharacterMapDeclarationPlan>();
        int index = 0;
        while (index < stylesheet.Declarations.Length)
        {
            XsltStylesheetDeclaration declaration = stylesheet.Declarations[index];
            if (declaration.LocalName == "character-map") declarations.Add(CreateXsltCharacterMapDeclaration(declaration));
            index = index + 1;
        }

        List<XsltCharacterMapPlan> result = MergeXsltCharacterMapDeclarations(declarations);
        LinkXsltCharacterMaps(result);
        ValidateXsltCharacterMapCycles(result);
        index = 0;
        while (index < result.Count)
        {
            ExpandXsltCharacterMap(result[index]);
            index = index + 1;
        }

        return result.ToArray();
    }

    internal XsltCharacterMapDeclarationPlan CreateXsltCharacterMapDeclaration(XsltStylesheetDeclaration declaration)
    {
        string? lexicalName = Loader.FindXsltAttribute(declaration.Node, "name", "");
        if (lexicalName == null || lexicalName.Trim().Length == 0)
        {
            throw CreateXsltFormatError("An xsl:character-map requires a name.", XsltErrorCodes.XTSE0010, declaration.Node, declaration.Module);
        }

        XmlName name = Loader.StaticProcessor.ResolveXsltStaticVariableName(lexicalName, declaration.Node, declaration.Module);
        string namespaceUri = name.NamespaceUri == null ? "" : name.NamespaceUri;
        if (new XsltFunctionCompiler(Loader).IsXsltFunctionReservedNamespace(namespaceUri))
        {
            throw CreateXsltFormatError("A character-map name must not use a reserved namespace.", XsltErrorCodes.XTSE0080, declaration.Node, declaration.Module);
        }

        XsltCharacterMapDeclarationPlan result = new XsltCharacterMapDeclarationPlan(declaration.Node, declaration.Module, name, CreateXsltExpandedName(name));
        result.ImportPrecedence = declaration.ImportPrecedence;
        result.DeclarationOrder = declaration.DeclarationOrder;
        result.UsedMapNames = ParseXsltEqNameList(Loader.FindXsltAttribute(declaration.Node, "use-character-maps", ""), declaration.Node, declaration.Module);
        result.Entries = ReadXsltCharacterMapEntries(declaration.Node, declaration.Module);
        return result;
    }

    internal XsltCharacterMapEntryPlan[] ReadXsltCharacterMapEntries(XmlNode node, XsltStylesheetModule module)
    {
        List<XsltCharacterMapEntryPlan> result = new List<XsltCharacterMapEntryPlan>();
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            bool whitespace = (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && new XsltStylesheetAssembler(Loader).IsXsltWhitespace(child.Value);
            if (child.Kind == XmlNodeKind.Element && Loader.XsltNodeNamespace(child) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(child) == "output-character")
            {
                ValidateXsltEmptyFormatDeclaration(child, module);
                string? character = Loader.FindXsltAttribute(child, "character", "");
                string? replacement = Loader.FindXsltAttribute(child, "string", "");
                int scalar = character == null ? -1 : ReadXsltSingleScalar(character);
                if (character == null || replacement == null)
                {
                    throw CreateXsltFormatError("An xsl:output-character requires character and string attributes.", XsltErrorCodes.XTSE0010, child, module);
                }

                if (scalar < 0)
                {
                    throw CreateXsltFormatError("The character attribute of xsl:output-character must contain one Unicode character.", XsltErrorCodes.XTSE0020, child, module);
                }

                result.Add(new XsltCharacterMapEntryPlan(child, (uint)scalar, replacement, result.Count));
            }
            else if (!whitespace && child.Kind != XmlNodeKind.Comment && child.Kind != XmlNodeKind.ProcessingInstruction)
            {
                throw CreateXsltFormatError("An xsl:character-map may contain only xsl:output-character elements.", XsltErrorCodes.XTSE0010, child, module);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal List<XsltCharacterMapPlan> MergeXsltCharacterMapDeclarations(List<XsltCharacterMapDeclarationPlan> declarations)
    {
        List<XsltCharacterMapPlan> result = new List<XsltCharacterMapPlan>();
        int index = 0;
        while (index < declarations.Count)
        {
            XsltCharacterMapDeclarationPlan declaration = declarations[index];
            XsltCharacterMapPlan? map = FindXsltCharacterMap(result, declaration.Name);
            if (map == null)
            {
                map = new XsltCharacterMapPlan(declaration.ResolvedName, declaration.Name, declaration);
                result.Add(map);
            }

            index = index + 1;
        }

        index = 0;
        while (index < result.Count)
        {
            CompleteXsltCharacterMap(result[index], declarations);
            index = index + 1;
        }

        return result;
    }

    internal void CompleteXsltCharacterMap(XsltCharacterMapPlan map, List<XsltCharacterMapDeclarationPlan> declarations)
    {
        List<XsltCharacterMapDeclarationPlan> matching = new List<XsltCharacterMapDeclarationPlan>();
        int highest = -1;
        int index = 0;
        while (index < declarations.Count)
        {
            XsltCharacterMapDeclarationPlan declaration = declarations[index];
            if (declaration.Name == map.Name)
            {
                matching.Add(declaration);
                if (declaration.ImportPrecedence > highest) highest = declaration.ImportPrecedence;
            }

            index = index + 1;
        }

        int effectiveCount = 0;
        index = 0;
        while (index < matching.Count)
        {
            matching[index].IsEffective = matching[index].ImportPrecedence == highest;
            if (matching[index].IsEffective)
            {
                map.EffectiveDeclaration = matching[index];
                effectiveCount = effectiveCount + 1;
            }

            index = index + 1;
        }

        map.Declarations = matching.ToArray();
        if (effectiveCount > 1)
        {
            throw CreateXsltFormatError("Two effective character maps have the same name and import precedence.", XsltErrorCodes.XTSE1580, map.EffectiveDeclaration.Declaration, map.EffectiveDeclaration.Module);
        }
    }

    internal void LinkXsltCharacterMaps(List<XsltCharacterMapPlan> maps)
    {
        int index = 0;
        while (index < maps.Count)
        {
            XsltCharacterMapPlan map = maps[index];
            List<XsltCharacterMapPlan> used = new List<XsltCharacterMapPlan>();
            int nameIndex = 0;
            while (nameIndex < map.EffectiveDeclaration.UsedMapNames.Length)
            {
                string name = map.EffectiveDeclaration.UsedMapNames[nameIndex];
                XsltCharacterMapPlan? target = FindXsltCharacterMap(maps, name);
                if (target == null)
                {
                    throw CreateXsltFormatError("A referenced character map is not declared in the package.", XsltErrorCodes.XTSE1590, map.EffectiveDeclaration.Declaration, map.EffectiveDeclaration.Module);
                }

                used.Add(target);
                nameIndex = nameIndex + 1;
            }

            map.UsedMaps = used.ToArray();
            index = index + 1;
        }
    }

    internal void ValidateXsltCharacterMapCycles(List<XsltCharacterMapPlan> maps)
    {
        int[] states = new int[maps.Count];
        int index = 0;
        while (index < maps.Count)
        {
            VisitXsltCharacterMap(maps[index], maps, states);
            index = index + 1;
        }
    }

    internal void VisitXsltCharacterMap(XsltCharacterMapPlan map, List<XsltCharacterMapPlan> maps, int[] states)
    {
        int index = maps.IndexOf(map);
        if (states[index] == 2) return;
        if (states[index] == 1)
        {
            throw CreateXsltFormatError("Character-map references contain a circularity.", XsltErrorCodes.XTSE1600, map.EffectiveDeclaration.Declaration, map.EffectiveDeclaration.Module);
        }

        states[index] = 1;
        int usedIndex = 0;
        while (usedIndex < map.UsedMaps.Length)
        {
            VisitXsltCharacterMap(map.UsedMaps[usedIndex], maps, states);
            usedIndex = usedIndex + 1;
        }

        states[index] = 2;
    }

    internal void ExpandXsltCharacterMap(XsltCharacterMapPlan map)
    {
        List<XmlCharacterMapEntry> entries = new List<XmlCharacterMapEntry>();
        int index = 0;
        while (index < map.UsedMaps.Length)
        {
            ExpandXsltCharacterMap(map.UsedMaps[index]);
            AppendXsltCharacterMapEntries(entries, map.UsedMaps[index].EffectiveEntries);
            index = index + 1;
        }

        index = 0;
        while (index < map.EffectiveDeclaration.Entries.Length)
        {
            XsltCharacterMapEntryPlan source = map.EffectiveDeclaration.Entries[index];
            XmlCharacterMapEntry entry = new XmlCharacterMapEntry();
            entry.CodePoint = source.CodePoint;
            entry.Replacement = source.Replacement;
            entry.MapName = map.ResolvedName;
            SetXsltCharacterMapEntry(entries, entry);
            index = index + 1;
        }

        map.EffectiveEntries = entries.ToArray();
    }

    internal void AppendXsltCharacterMapEntries(List<XmlCharacterMapEntry> target, XmlCharacterMapEntry[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            SetXsltCharacterMapEntry(target, source[index]);
            index = index + 1;
        }
    }

    internal void SetXsltCharacterMapEntry(List<XmlCharacterMapEntry> entries, XmlCharacterMapEntry entry)
    {
        int index = 0;
        while (index < entries.Count)
        {
            if (entries[index].CodePoint == entry.CodePoint)
            {
                entries.RemoveAt(index);
                break;
            }

            index = index + 1;
        }

        entries.Add(entry);
    }

    internal XsltCharacterMapPlan? FindXsltCharacterMap(List<XsltCharacterMapPlan> maps, string name)
    {
        int index = 0;
        while (index < maps.Count)
        {
            if (maps[index].Name == name) return maps[index];
            index = index + 1;
        }

        return null;
    }

    internal XsltOutputFormatPlan[] CompileXsltOutputFormats(XsltStylesheetPlan stylesheet)
    {
        List<XsltOutputDeclarationPlan> declarations = new List<XsltOutputDeclarationPlan>();
        int index = 0;
        while (index < stylesheet.Declarations.Length)
        {
            XsltStylesheetDeclaration declaration = stylesheet.Declarations[index];
            if (declaration.LocalName == "output") declarations.Add(CreateXsltOutputDeclaration(declaration));
            index = index + 1;
        }

        List<XsltOutputFormatPlan> result = new List<XsltOutputFormatPlan>();
        result.Add(new XsltOutputFormatPlan(null, "#default"));
        index = 0;
        while (index < declarations.Count)
        {
            XsltOutputDeclarationPlan declaration = declarations[index];
            XsltOutputFormatPlan? format = FindXsltOutputFormat(result, declaration.Name);
            if (format == null)
            {
                format = new XsltOutputFormatPlan(declaration.ResolvedName, declaration.Name);
                result.Add(format);
            }

            index = index + 1;
        }

        index = 0;
        while (index < result.Count)
        {
            CompleteXsltOutputFormat(result[index], declarations, stylesheet.CharacterMaps);
            index = index + 1;
        }

        return result.ToArray();
    }

    internal XsltOutputDeclarationPlan CreateXsltOutputDeclaration(XsltStylesheetDeclaration declaration)
    {
        ValidateXsltEmptyFormatDeclaration(declaration.Node, declaration.Module);
        string? lexicalName = Loader.FindXsltAttribute(declaration.Node, "name", "");
        XmlName? resolvedName = lexicalName == null ? null : Loader.StaticProcessor.ResolveXsltStaticVariableName(lexicalName, declaration.Node, declaration.Module);
        if (resolvedName != null && new XsltFunctionCompiler(Loader).IsXsltFunctionReservedNamespace(resolvedName.NamespaceUri == null ? "" : resolvedName.NamespaceUri))
        {
            throw CreateXsltFormatError("An output definition name must not use a reserved namespace.", XsltErrorCodes.XTSE0080, declaration.Node, declaration.Module);
        }

        string name = resolvedName == null ? "#default" : CreateXsltExpandedName(resolvedName);
        XsltOutputDeclarationPlan result = new XsltOutputDeclarationPlan(declaration.Node, declaration.Module, resolvedName, name);
        result.ImportPrecedence = declaration.ImportPrecedence;
        result.DeclarationOrder = declaration.DeclarationOrder;
        List<XsltOutputAttributePlan> attributes = new List<XsltOutputAttributePlan>();
        int index = 0;
        while (index < OutputAttributeNames.Length)
        {
            string attributeName = OutputAttributeNames[index];
            string? value = Loader.FindXsltAttribute(declaration.Node, attributeName, "");
            if (value != null)
            {
                attributes.Add(CreateXsltOutputAttribute(attributeName, value, declaration));
            }

            index = index + 1;
        }

        result.Attributes = attributes.ToArray();
        return result;
    }

    internal XsltOutputAttributePlan CreateXsltOutputAttribute(string name, string value, XsltStylesheetDeclaration declaration)
    {
        string normalized = value;
        XmlName[] names = [];
        if (IsXsltOutputBoolean(name))
        {
            normalized = Loader.StaticProcessor.ParseXsltStaticBoolean(value, name, declaration.Node, declaration.Module) ? "yes" : "no";
        }
        else if (name == "standalone")
        {
            string standalone = value.Trim();
            if (standalone == "omit") normalized = "omit";
            else normalized = Loader.StaticProcessor.ParseXsltStaticBoolean(value, name, declaration.Node, declaration.Module) ? "yes" : "no";
        }
        else if (name == "method" || name == "json-node-output-method")
        {
            normalized = NormalizeXsltOutputMethod(value, name, declaration.Node, declaration.Module);
        }
        else if (name == "cdata-section-elements" || name == "suppress-indentation" || name == "use-character-maps")
        {
            names = ResolveXsltEqNameList(value, declaration.Node, declaration.Module);
            normalized = JoinXsltExpandedNames(names);
        }
        else if (name == "html-version" && !Loader.IsValidXsltDecimal(value.Trim()))
        {
            throw CreateXsltFormatError("The html-version output attribute must be a decimal.", XsltErrorCodes.XTSE0020, declaration.Node, declaration.Module);
        }

        return new XsltOutputAttributePlan(name, value, normalized, names, declaration.Node, declaration.Module, declaration.ImportPrecedence, declaration.DeclarationOrder);
    }

    internal bool IsXsltOutputBoolean(string name)
    {
        return name == "allow-duplicate-names" || name == "build-tree" || name == "byte-order-mark" || name == "escape-uri-attributes" || name == "include-content-type" || name == "indent" || name == "omit-xml-declaration" || name == "undeclare-prefixes";
    }

    internal string NormalizeXsltOutputMethod(string value, string attributeName, XmlNode node, XsltStylesheetModule module)
    {
        string method = value.Trim();
        bool builtIn = method == "xml" || method == "html" || method == "xhtml" || method == "text" || method == "json" || method == "adaptive";
        if (builtIn)
        {
            if (attributeName == "json-node-output-method" && method != "xml" && method != "html" && method != "xhtml" && method != "text")
            {
                throw CreateXsltFormatError("The json-node-output-method is not valid.", XsltErrorCodes.XTSE0020, node, module);
            }

            return method;
        }

        if (method.IndexOf(':') < 0 && !method.StartsWith("Q{", StringComparison.Ordinal))
        {
            throw CreateXsltFormatError("An unprefixed output method must be a standard method.", XsltErrorCodes.XTSE1570, node, module);
        }

        XmlName name = Loader.StaticProcessor.ResolveXsltStaticVariableName(method, node, module);
        return CreateXsltExpandedName(name);
    }

    internal void CompleteXsltOutputFormat(XsltOutputFormatPlan format, List<XsltOutputDeclarationPlan> declarations, XsltCharacterMapPlan[] characterMaps)
    {
        List<XsltOutputDeclarationPlan> matching = new List<XsltOutputDeclarationPlan>();
        int index = 0;
        while (index < declarations.Count)
        {
            if (declarations[index].Name == format.Name) matching.Add(declarations[index]);
            index = index + 1;
        }

        format.Declarations = matching.ToArray();
        List<XsltOutputAttributePlan> effective = new List<XsltOutputAttributePlan>();
        index = 0;
        while (index < OutputAttributeNames.Length)
        {
            string name = OutputAttributeNames[index];
            if (name == "cdata-section-elements" || name == "suppress-indentation" || name == "use-character-maps")
            {
                AppendXsltAdditiveOutputAttributes(effective, matching, name);
            }
            else
            {
                XsltOutputAttributePlan? attribute = SelectXsltOutputAttribute(matching, name);
                if (attribute != null) effective.Add(attribute);
            }

            index = index + 1;
        }

        format.Attributes = effective.ToArray();
        ApplyXsltOutputAttributes(format, effective, characterMaps);
        format.Options.ExtensionMethods = Loader.Options.SerializationMethods;
        format.Options.ExtensionPolicy = Loader.Options.ExtensionPolicy;
    }

    internal XsltOutputAttributePlan? SelectXsltOutputAttribute(List<XsltOutputDeclarationPlan> declarations, string name)
    {
        XsltOutputAttributePlan? selected = null;
        int index = 0;
        while (index < declarations.Count)
        {
            XsltOutputAttributePlan? candidate = FindXsltOutputAttribute(declarations[index].Attributes, name);
            if (candidate != null && (selected == null || candidate.ImportPrecedence > selected.ImportPrecedence)) selected = candidate;
            index = index + 1;
        }

        index = 0;
        while (index < declarations.Count)
        {
            XsltOutputAttributePlan? candidate = FindXsltOutputAttribute(declarations[index].Attributes, name);
            if (candidate != null && selected != null && candidate.ImportPrecedence == selected.ImportPrecedence && candidate.NormalizedValue != selected.NormalizedValue)
            {
                throw CreateXsltFormatError("Output declarations specify conflicting values at the same import precedence.", XsltErrorCodes.XTSE1560, candidate.Declaration, candidate.Module);
            }

            if (candidate != null && selected != null && candidate.ImportPrecedence == selected.ImportPrecedence && candidate.DeclarationOrder > selected.DeclarationOrder) selected = candidate;
            index = index + 1;
        }

        return selected;
    }

    internal void AppendXsltAdditiveOutputAttributes(List<XsltOutputAttributePlan> result, List<XsltOutputDeclarationPlan> declarations, string name)
    {
        List<XsltOutputAttributePlan> matches = new List<XsltOutputAttributePlan>();
        int index = 0;
        while (index < declarations.Count)
        {
            XsltOutputAttributePlan? attribute = FindXsltOutputAttribute(declarations[index].Attributes, name);
            if (attribute != null) InsertXsltOutputAttribute(matches, attribute);
            index = index + 1;
        }

        index = 0;
        while (index < matches.Count)
        {
            result.Add(matches[index]);
            index = index + 1;
        }
    }

    internal void InsertXsltOutputAttribute(List<XsltOutputAttributePlan> attributes, XsltOutputAttributePlan attribute)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            XsltOutputAttributePlan current = attributes[index];
            if (attribute.ImportPrecedence < current.ImportPrecedence || attribute.ImportPrecedence == current.ImportPrecedence && attribute.DeclarationOrder < current.DeclarationOrder)
            {
                attributes.Insert(index, attribute);
                return;
            }

            index = index + 1;
        }

        attributes.Add(attribute);
    }

    internal XsltOutputAttributePlan? FindXsltOutputAttribute(XsltOutputAttributePlan[] attributes, string name)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            if (attributes[index].Name == name) return attributes[index];
            index = index + 1;
        }

        return null;
    }

    internal void ApplyXsltOutputAttributes(XsltOutputFormatPlan format, List<XsltOutputAttributePlan> attributes, XsltCharacterMapPlan[] characterMaps)
    {
        List<XmlName> cdata = new List<XmlName>();
        List<XmlName> suppress = new List<XmlName>();
        List<string> mapNames = new List<string>();
        int index = 0;
        while (index < attributes.Count)
        {
            XsltOutputAttributePlan attribute = attributes[index];
            if (attribute.Name == "method") SetXsltOutputMethod(format, attribute.NormalizedValue);
            else if (attribute.Name == "allow-duplicate-names") format.Options.AllowDuplicateNames = attribute.NormalizedValue == "yes";
            else if (attribute.Name == "build-tree") format.BuildTree = attribute.NormalizedValue == "yes";
            else if (attribute.Name == "byte-order-mark") format.Options.ByteOrderMark = attribute.NormalizedValue == "yes";
            else if (attribute.Name == "cdata-section-elements") AppendXsltUniqueNames(cdata, attribute.Names);
            else if (attribute.Name == "doctype-public") format.Options.DoctypePublic = attribute.LexicalValue;
            else if (attribute.Name == "doctype-system") format.Options.DoctypeSystem = attribute.LexicalValue;
            else if (attribute.Name == "encoding") format.Options.Encoding = attribute.LexicalValue;
            else if (attribute.Name == "escape-uri-attributes") format.Options.EscapeUriAttributes = attribute.NormalizedValue == "yes";
            else if (attribute.Name == "html-version") format.Options.HtmlVersion = attribute.NormalizedValue;
            else if (attribute.Name == "include-content-type") format.Options.IncludeContentType = attribute.NormalizedValue == "yes";
            else if (attribute.Name == "indent") format.Options.Indent = attribute.NormalizedValue == "yes";
            else if (attribute.Name == "item-separator") format.Options.ItemSeparator = attribute.LexicalValue;
            else if (attribute.Name == "json-node-output-method") format.Options.JsonNodeOutputMethod = ReadXsltBuiltInOutputMethod(attribute.NormalizedValue);
            else if (attribute.Name == "media-type") format.Options.MediaType = attribute.LexicalValue;
            else if (attribute.Name == "normalization-form") format.Options.NormalizationForm = attribute.LexicalValue;
            else if (attribute.Name == "omit-xml-declaration") format.Options.OmitXmlDeclaration = attribute.NormalizedValue == "yes";
            else if (attribute.Name == "parameter-document") format.ParameterDocument = attribute.LexicalValue;
            else if (attribute.Name == "standalone") format.Options.Standalone = attribute.NormalizedValue == "yes" ? XmlStandalone.Yes : attribute.NormalizedValue == "no" ? XmlStandalone.No : XmlStandalone.Unspecified;
            else if (attribute.Name == "suppress-indentation") AppendXsltUniqueNames(suppress, attribute.Names);
            else if (attribute.Name == "undeclare-prefixes") format.Options.UndeclarePrefixes = attribute.NormalizedValue == "yes";
            else if (attribute.Name == "use-character-maps") AppendXsltExpandedNameStrings(mapNames, attribute.Names);
            else if (attribute.Name == "version") format.Options.Version = attribute.LexicalValue;
            index = index + 1;
        }

        format.Options.CDataSectionElements = cdata.ToArray();
        format.Options.SuppressIndentation = suppress.ToArray();
        format.CharacterMapNames = mapNames.ToArray();
        LinkXsltOutputCharacterMaps(format, characterMaps);
    }

    internal void SetXsltOutputMethod(XsltOutputFormatPlan format, string value)
    {
        format.HasMethod = true;
        XmlSerializationMethod method = ReadXsltBuiltInOutputMethod(value);
        format.Options.Method = method;
        if (method == XmlSerializationMethod.Extension)
        {
            format.Options.ExtensionMethodName = ParseXsltExpandedName(value);
        }

        XsltOutputAttributePlan? buildTree = FindXsltOutputAttribute(format.Attributes, "build-tree");
        if (buildTree == null) format.BuildTree = method != XmlSerializationMethod.Json && method != XmlSerializationMethod.Adaptive;
    }

    internal XmlSerializationMethod ReadXsltBuiltInOutputMethod(string value)
    {
        if (value == "xml") return XmlSerializationMethod.Xml;
        if (value == "xhtml") return XmlSerializationMethod.Xhtml;
        if (value == "html") return XmlSerializationMethod.Html;
        if (value == "text") return XmlSerializationMethod.Text;
        if (value == "json") return XmlSerializationMethod.Json;
        if (value == "adaptive") return XmlSerializationMethod.Adaptive;
        return XmlSerializationMethod.Extension;
    }

    internal void LinkXsltOutputCharacterMaps(XsltOutputFormatPlan format, XsltCharacterMapPlan[] characterMaps)
    {
        List<XsltCharacterMapPlan> maps = new List<XsltCharacterMapPlan>();
        List<XmlCharacterMapEntry> entries = new List<XmlCharacterMapEntry>();
        int index = 0;
        while (index < format.CharacterMapNames.Length)
        {
            XsltCharacterMapPlan? map = FindXsltCharacterMapInArray(characterMaps, format.CharacterMapNames[index]);
            if (map == null)
            {
                XmlNode node = format.Declarations.Length == 0 ? Loader.Modules[0].Root : format.Declarations[format.Declarations.Length - 1].Declaration;
                XsltStylesheetModule module = format.Declarations.Length == 0 ? Loader.Modules[0] : format.Declarations[format.Declarations.Length - 1].Module;
                throw CreateXsltFormatError("An output definition references an undeclared character map.", XsltErrorCodes.XTSE1590, node, module);
            }

            maps.Add(map);
            AppendXsltCharacterMapEntries(entries, map.EffectiveEntries);
            index = index + 1;
        }

        format.CharacterMaps = maps.ToArray();
        format.Options.UseCharacterMaps = entries.ToArray();
    }

    internal XsltCharacterMapPlan? FindXsltCharacterMapInArray(XsltCharacterMapPlan[] maps, string name)
    {
        int index = 0;
        while (index < maps.Length)
        {
            if (maps[index].Name == name) return maps[index];
            index = index + 1;
        }

        return null;
    }

    internal XsltOutputFormatPlan? FindXsltOutputFormat(List<XsltOutputFormatPlan> formats, string name)
    {
        int index = 0;
        while (index < formats.Count)
        {
            if (formats[index].Name == name) return formats[index];
            index = index + 1;
        }

        return null;
    }

    internal XsltWhitespaceRulePlan[] CompileXsltWhitespaceRules(XsltStylesheetPlan stylesheet)
    {
        List<XsltWhitespaceRulePlan> result = new List<XsltWhitespaceRulePlan>();
        int index = 0;
        while (index < stylesheet.Declarations.Length)
        {
            XsltStylesheetDeclaration declaration = stylesheet.Declarations[index];
            if (declaration.LocalName == "strip-space" || declaration.LocalName == "preserve-space")
            {
                ValidateXsltEmptyFormatDeclaration(declaration.Node, declaration.Module);
                string? elements = Loader.FindXsltAttribute(declaration.Node, "elements", "");
                string[] tokens = elements == null ? [] : elements.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0)
                {
                    throw CreateXsltFormatError("A whitespace declaration requires one or more name tests.", XsltErrorCodes.XTSE0010, declaration.Node, declaration.Module);
                }

                int tokenIndex = 0;
                while (tokenIndex < tokens.Length)
                {
                    XsltWhitespaceRulePlan rule = ParseXsltWhitespaceRule(tokens[tokenIndex], declaration);
                    rule.Preserve = declaration.LocalName == "preserve-space";
                    ValidateXsltWhitespaceConflict(result, rule);
                    result.Add(rule);
                    tokenIndex = tokenIndex + 1;
                }
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal XsltWhitespaceRulePlan ParseXsltWhitespaceRule(string token, XsltStylesheetDeclaration declaration)
    {
        XsltWhitespaceRulePlan result = new XsltWhitespaceRulePlan(declaration.Node, declaration.Module, token);
        result.ImportPrecedence = declaration.ImportPrecedence;
        result.DeclarationOrder = declaration.DeclarationOrder;
        if (token == "*")
        {
            result.Kind = XsltWhitespaceNameTestKind.Any;
            result.Name = "*";
            result.Priority = 0;
            return result;
        }

        if (token.StartsWith("*:", StringComparison.Ordinal))
        {
            string local = token.Substring(2);
            if (!new XdmStringDatatype().IsValidXdmName(local, true)) ThrowXsltWhitespaceNameError(declaration);
            result.Kind = XsltWhitespaceNameTestKind.LocalName;
            result.LocalName = local;
            result.Name = "*:" + local;
            result.Priority = 1;
            return result;
        }

        if (token.EndsWith(":*", StringComparison.Ordinal))
        {
            string prefix = token.Substring(0, token.Length - 2);
            if (!new XdmStringDatatype().IsValidXdmName(prefix, true)) ThrowXsltWhitespaceNameError(declaration);
            string? namespaceUri = FindXsltNamespace(declaration.StaticContext, prefix);
            if (namespaceUri == null) ThrowXsltWhitespaceNameError(declaration);
            result.Kind = XsltWhitespaceNameTestKind.Namespace;
            result.NamespaceUri = namespaceUri == null ? "" : namespaceUri;
            result.Name = "Q{" + result.NamespaceUri + "}*";
            result.Priority = 1;
            return result;
        }

        if (token.StartsWith("Q{", StringComparison.Ordinal) && token.EndsWith("}*", StringComparison.Ordinal))
        {
            int close = token.IndexOf('}');
            if (close < 2 || close != token.Length - 2) ThrowXsltWhitespaceNameError(declaration);
            result.Kind = XsltWhitespaceNameTestKind.Namespace;
            result.NamespaceUri = token.Substring(2, close - 2);
            result.Name = "Q{" + result.NamespaceUri + "}*";
            result.Priority = 1;
            return result;
        }

        XmlName name = Loader.StaticProcessor.ResolveXsltStaticVariableName(token, declaration.Node, declaration.Module);
        if (token.IndexOf(':') < 0 && !token.StartsWith("Q{", StringComparison.Ordinal) && declaration.StaticContext != null)
        {
            name.NamespaceUri = declaration.StaticContext.XPathDefaultNamespace;
        }

        result.Kind = XsltWhitespaceNameTestKind.Name;
        result.NamespaceUri = name.NamespaceUri == null ? "" : name.NamespaceUri;
        result.LocalName = name.LocalName == null ? "" : name.LocalName;
        result.Name = CreateXsltExpandedName(name);
        result.Priority = 2;
        return result;
    }

    internal void ValidateXsltWhitespaceConflict(List<XsltWhitespaceRulePlan> rules, XsltWhitespaceRulePlan candidate)
    {
        int index = 0;
        while (index < rules.Count)
        {
            XsltWhitespaceRulePlan current = rules[index];
            if (current.Name == candidate.Name && current.ImportPrecedence == candidate.ImportPrecedence && current.Preserve != candidate.Preserve)
            {
                throw CreateXsltFormatError("The same whitespace name test is both stripped and preserved at one import precedence.", XsltErrorCodes.XTSE0270, candidate.Declaration, candidate.Module);
            }

            index = index + 1;
        }
    }

    internal void ThrowXsltWhitespaceNameError(XsltStylesheetDeclaration declaration)
    {
        throw CreateXsltFormatError("A whitespace declaration contains an invalid or unbound name test.", XsltErrorCodes.XTSE0280, declaration.Node, declaration.Module);
    }

    internal string[] ParseXsltEqNameList(string? value, XmlNode node, XsltStylesheetModule module)
    {
        XmlName[] names = ResolveXsltEqNameList(value == null ? "" : value, node, module);
        string[] result = new string[names.Length];
        int index = 0;
        while (index < names.Length)
        {
            result[index] = CreateXsltExpandedName(names[index]);
            index = index + 1;
        }

        return result;
    }

    internal XmlName[] ResolveXsltEqNameList(string value, XmlNode node, XsltStylesheetModule module)
    {
        string[] tokens = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        XmlName[] result = new XmlName[tokens.Length];
        int index = 0;
        while (index < tokens.Length)
        {
            result[index] = Loader.StaticProcessor.ResolveXsltStaticVariableName(tokens[index], node, module);
            index = index + 1;
        }

        return result;
    }

    internal string JoinXsltExpandedNames(XmlName[] names)
    {
        string result = "";
        int index = 0;
        while (index < names.Length)
        {
            if (index != 0) result = result + " ";
            result = result + CreateXsltExpandedName(names[index]);
            index = index + 1;
        }

        return result;
    }

    internal void AppendXsltExpandedNameStrings(List<string> result, XmlName[] names)
    {
        int index = 0;
        while (index < names.Length)
        {
            result.Add(CreateXsltExpandedName(names[index]));
            index = index + 1;
        }
    }

    internal void AppendXsltUniqueNames(List<XmlName> result, XmlName[] names)
    {
        int index = 0;
        while (index < names.Length)
        {
            string expanded = CreateXsltExpandedName(names[index]);
            bool found = false;
            int resultIndex = 0;
            while (resultIndex < result.Count)
            {
                if (CreateXsltExpandedName(result[resultIndex]) == expanded) found = true;
                resultIndex = resultIndex + 1;
            }

            if (!found) result.Add(names[index]);
            index = index + 1;
        }
    }

    internal string? FindXsltNamespace(XsltStaticContext? context, string prefix)
    {
        if (context == null) return null;
        int index = context.Namespaces.Length - 1;
        while (index >= 0)
        {
            string? candidate = context.Namespaces[index].Prefix;
            if ((candidate == null ? "" : candidate) == prefix) return context.Namespaces[index].NamespaceUri;
            index = index - 1;
        }

        return prefix == "xml" ? XmlNamespaces.Xml : null;
    }

    internal int ReadXsltSingleScalar(string value)
    {
        if (value.Length == 1 && !char.IsSurrogate(value[0])) return value[0];
        if (value.Length == 2 && char.IsHighSurrogate(value[0]) && char.IsLowSurrogate(value[1])) return char.ConvertToUtf32(value[0], value[1]);
        return -1;
    }

    internal void ValidateXsltEmptyFormatDeclaration(XmlNode node, XsltStylesheetModule module)
    {
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            bool whitespace = (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && new XsltStylesheetAssembler(Loader).IsXsltWhitespace(child.Value);
            if (!whitespace && child.Kind != XmlNodeKind.Comment && child.Kind != XmlNodeKind.ProcessingInstruction)
            {
                throw CreateXsltFormatError("This XSLT declaration is required to be empty.", XsltErrorCodes.XTSE0260, child, module);
            }

            index = index + 1;
        }
    }

    internal string CreateXsltExpandedName(XmlName name)
    {
        return "Q{" + (name.NamespaceUri == null ? "" : name.NamespaceUri) + "}" + (name.LocalName == null ? "" : name.LocalName);
    }

    internal XmlName ParseXsltExpandedName(string value)
    {
        int close = value.IndexOf('}');
        XmlName result = new XmlName();
        result.NamespaceUri = value.Substring(2, close - 2);
        result.LocalName = value.Substring(close + 1);
        result.QualifiedName = value;
        return result;
    }

    internal XsltStaticException CreateXsltFormatError(string message, string code, XmlNode node, XsltStylesheetModule module)
    {
        return new XsltStylesheetAssembler(Loader).CreateXsltAssemblyError(message, code, node, module);
    }
}
