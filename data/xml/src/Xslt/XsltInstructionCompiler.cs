using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal class XsltInstructionCompiler
{
    internal XsltStylesheetLoader Loader;

    internal XsltInstructionCompiler(XsltStylesheetLoader loader)
    {
        Loader = loader;
    }

    internal void CompileXsltInstructions(XsltStylesheetPlan stylesheet)
    {
        List<XsltInstructionPlan> result = new List<XsltInstructionPlan>();
        int index = 0;
        while (index < stylesheet.Modules.Length)
        {
            CollectXsltInstructionPlans(stylesheet.Modules[index].Root, stylesheet.Modules[index], stylesheet, result, true);
            index = index + 1;
        }

        stylesheet.Instructions = result.ToArray();
    }

    internal void CollectXsltInstructionPlans(XmlNode node, XsltStylesheetModule module, XsltStylesheetPlan stylesheet, List<XsltInstructionPlan> result, bool moduleRoot)
    {
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element)
            {
                string namespaceUri = Loader.XsltNodeNamespace(child);
                string localName = Loader.XsltNodeLocalName(child);
                bool declaration = moduleRoot && namespaceUri == XsltNamespaces.Xslt && new XsltStylesheetAssembler(Loader).IsXsltTopLevelDeclarationName(localName, module.Kind);
                bool overrideDeclaration = IsXsltOverrideComponentDeclaration(child);
                if (namespaceUri == XsltNamespaces.Xslt && !declaration && !overrideDeclaration)
                {
                    XsltInstructionPlan plan = CreateXsltInstructionPlan(child, module, stylesheet);
                    result.Add(plan);
                }

                CollectXsltInstructionPlans(child, module, stylesheet, result, false);
            }

            index = index + 1;
        }
    }

    internal bool IsXsltOverrideComponentDeclaration(XmlNode node)
    {
        string localName = Loader.XsltNodeLocalName(node);
        if (localName != "template" && localName != "function" && localName != "variable" && localName != "param" && localName != "attribute-set")
        {
            return false;
        }

        XmlNode? parent = node.Parent;
        return parent != null && Loader.XsltNodeNamespace(parent) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(parent) == "override";
    }

    internal XsltInstructionPlan CreateXsltInstructionPlan(XmlNode node, XsltStylesheetModule module, XsltStylesheetPlan stylesheet)
    {
        string localName = Loader.XsltNodeLocalName(node);
        XsltInstructionKind kind = ClassifyXsltInstruction(localName);
        XsltStaticContext? context = FindXsltInstructionStaticContext(stylesheet, node);
        if (context == null)
        {
            throw CreateXsltInstructionError("An XSLT instruction has no static context.", XsltErrorCodes.XTSE0010, node, module);
        }

        XsltInstructionPlan result = new XsltInstructionPlan(node, module, context, kind, localName);
        result.IsHelper = IsXsltHelperInstruction(kind);
        result.IsForwardCompatible = kind == XsltInstructionKind.Unknown;
        ValidateXsltInstructionPosition(result);
        ValidateXsltInstructionRequiredAttributes(result);
        ValidateXsltInstructionAttributes(result);
        ValidateXsltInstructionContent(result);
        result.Attributes = CompileXsltInstructionAttributes(result, stylesheet);
        result.ContentNodes = SelectXsltInstructionContentNodes(node);
        return result;
    }

    internal XsltInstructionKind ClassifyXsltInstruction(string name)
    {
        if (name == "analyze-string") return XsltInstructionKind.AnalyzeString;
        if (name == "apply-imports") return XsltInstructionKind.ApplyImports;
        if (name == "apply-templates") return XsltInstructionKind.ApplyTemplates;
        if (name == "array") return XsltInstructionKind.Array;
        if (name == "array-member") return XsltInstructionKind.ArrayMember;
        if (name == "assert") return XsltInstructionKind.Assert;
        if (name == "attribute") return XsltInstructionKind.Attribute;
        if (name == "break") return XsltInstructionKind.Break;
        if (name == "call-template") return XsltInstructionKind.CallTemplate;
        if (name == "catch") return XsltInstructionKind.Catch;
        if (name == "choose") return XsltInstructionKind.Choose;
        if (name == "comment") return XsltInstructionKind.Comment;
        if (name == "context-item") return XsltInstructionKind.ContextItem;
        if (name == "copy") return XsltInstructionKind.Copy;
        if (name == "copy-of") return XsltInstructionKind.CopyOf;
        if (name == "document") return XsltInstructionKind.Document;
        if (name == "element") return XsltInstructionKind.Element;
        if (name == "evaluate") return XsltInstructionKind.Evaluate;
        if (name == "fallback") return XsltInstructionKind.Fallback;
        if (name == "for-each") return XsltInstructionKind.ForEach;
        if (name == "for-each-group") return XsltInstructionKind.ForEachGroup;
        if (name == "fork") return XsltInstructionKind.Fork;
        if (name == "if") return XsltInstructionKind.If;
        if (name == "iterate") return XsltInstructionKind.Iterate;
        if (name == "map") return XsltInstructionKind.Map;
        if (name == "map-entry") return XsltInstructionKind.MapEntry;
        if (name == "matching-substring") return XsltInstructionKind.MatchingSubstring;
        if (name == "merge") return XsltInstructionKind.Merge;
        if (name == "merge-action") return XsltInstructionKind.MergeAction;
        if (name == "merge-key") return XsltInstructionKind.MergeKey;
        if (name == "merge-source") return XsltInstructionKind.MergeSource;
        if (name == "message") return XsltInstructionKind.Message;
        if (name == "namespace") return XsltInstructionKind.Namespace;
        if (name == "next-iteration") return XsltInstructionKind.NextIteration;
        if (name == "next-match") return XsltInstructionKind.NextMatch;
        if (name == "non-matching-substring") return XsltInstructionKind.NonMatchingSubstring;
        if (name == "number") return XsltInstructionKind.Number;
        if (name == "on-completion") return XsltInstructionKind.OnCompletion;
        if (name == "on-empty") return XsltInstructionKind.OnEmpty;
        if (name == "on-non-empty") return XsltInstructionKind.OnNonEmpty;
        if (name == "otherwise") return XsltInstructionKind.Otherwise;
        if (name == "param") return XsltInstructionKind.Parameter;
        if (name == "perform-sort") return XsltInstructionKind.PerformSort;
        if (name == "processing-instruction") return XsltInstructionKind.ProcessingInstruction;
        if (name == "result-document") return XsltInstructionKind.ResultDocument;
        if (name == "sequence") return XsltInstructionKind.Sequence;
        if (name == "sort") return XsltInstructionKind.Sort;
        if (name == "source-document") return XsltInstructionKind.SourceDocument;
        if (name == "text") return XsltInstructionKind.Text;
        if (name == "try") return XsltInstructionKind.Try;
        if (name == "value-of") return XsltInstructionKind.ValueOf;
        if (name == "variable") return XsltInstructionKind.Variable;
        if (name == "when") return XsltInstructionKind.When;
        if (name == "where-populated") return XsltInstructionKind.WherePopulated;
        if (name == "with-param") return XsltInstructionKind.WithParam;
        return XsltInstructionKind.Unknown;
    }

    internal bool IsXsltHelperInstruction(XsltInstructionKind kind)
    {
        return kind == XsltInstructionKind.ArrayMember || kind == XsltInstructionKind.Catch || kind == XsltInstructionKind.ContextItem ||
            kind == XsltInstructionKind.MapEntry || kind == XsltInstructionKind.MatchingSubstring || kind == XsltInstructionKind.MergeAction ||
            kind == XsltInstructionKind.MergeKey || kind == XsltInstructionKind.MergeSource || kind == XsltInstructionKind.NonMatchingSubstring ||
            kind == XsltInstructionKind.OnCompletion || kind == XsltInstructionKind.Otherwise || kind == XsltInstructionKind.Parameter ||
            kind == XsltInstructionKind.Sort || kind == XsltInstructionKind.Variable || kind == XsltInstructionKind.When || kind == XsltInstructionKind.WithParam;
    }

    internal void ValidateXsltInstructionPosition(XsltInstructionPlan plan)
    {
        XsltInstructionKind parent = ClassifyXsltInstructionParent(plan.Instruction);
        XsltInstructionKind kind = plan.Kind;
        bool valid = true;
        if (kind == XsltInstructionKind.When || kind == XsltInstructionKind.Otherwise) valid = parent == XsltInstructionKind.Choose;
        else if (kind == XsltInstructionKind.Catch) valid = parent == XsltInstructionKind.Try;
        else if (kind == XsltInstructionKind.MatchingSubstring || kind == XsltInstructionKind.NonMatchingSubstring) valid = parent == XsltInstructionKind.AnalyzeString;
        else if (kind == XsltInstructionKind.MapEntry) valid = parent == XsltInstructionKind.Map;
        else if (kind == XsltInstructionKind.ArrayMember) valid = parent == XsltInstructionKind.Array;
        else if (kind == XsltInstructionKind.MergeSource || kind == XsltInstructionKind.MergeAction) valid = parent == XsltInstructionKind.Merge;
        else if (kind == XsltInstructionKind.MergeKey) valid = parent == XsltInstructionKind.MergeSource;
        else if (kind == XsltInstructionKind.OnCompletion) valid = parent == XsltInstructionKind.Iterate;
        else if (kind == XsltInstructionKind.Fallback) valid = parent == XsltInstructionKind.Try || parent == XsltInstructionKind.AnalyzeString || parent == XsltInstructionKind.Evaluate || parent == XsltInstructionKind.Merge || HasXsltForwardCompatibleUnknownParent(plan.Instruction) || HasXsltExtensionParent(plan);
        else if (kind == XsltInstructionKind.ContextItem) valid = HasXsltDirectNamedParent(plan.Instruction, "template");
        else if (kind == XsltInstructionKind.Sort) valid = IsXsltSortParent(parent);
        else if (kind == XsltInstructionKind.WithParam) valid = IsXsltWithParamParent(parent);
        else if (kind == XsltInstructionKind.NextIteration || kind == XsltInstructionKind.Break) valid = HasXsltInstructionAncestor(plan.Instruction, XsltInstructionKind.Iterate);
        else if (kind == XsltInstructionKind.ApplyImports || kind == XsltInstructionKind.NextMatch) valid = HasXsltTemplateRuleAncestor(plan.Instruction);
        if (!valid)
        {
            throw CreateXsltInstructionError("The xsl:" + plan.LocalName + " element is not permitted in this position.", XsltErrorCodes.XTSE0010, plan.Instruction, plan.Module);
        }
    }

    internal bool HasXsltForwardCompatibleUnknownParent(XmlNode node)
    {
        XmlNode? parent = node.Parent;
        if (parent == null || Loader.XsltNodeNamespace(parent) != XsltNamespaces.Xslt)
        {
            return false;
        }

        string name = Loader.XsltNodeLocalName(parent);
        return ClassifyXsltInstruction(name) != XsltInstructionKind.Unknown || !new XsltStylesheetAssembler(Loader).IsXsltKnownElementName(name);
    }

    internal bool HasXsltExtensionParent(XsltInstructionPlan plan)
    {
        XmlNode? parent = plan.Instruction.Parent;
        if (parent == null)
        {
            return false;
        }

        string namespaceUri = Loader.XsltNodeNamespace(parent);
        int index = 0;
        while (index < plan.StaticContext.ExtensionNamespaceUris.Length)
        {
            if (plan.StaticContext.ExtensionNamespaceUris[index] == namespaceUri)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HasXsltDirectNamedParent(XmlNode node, string name)
    {
        XmlNode? parent = node.Parent;
        return parent != null && Loader.XsltNodeNamespace(parent) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(parent) == name;
    }

    internal XsltInstructionKind ClassifyXsltInstructionParent(XmlNode node)
    {
        XmlNode? parent = node.Parent;
        if (parent == null || Loader.XsltNodeNamespace(parent) != XsltNamespaces.Xslt)
        {
            return XsltInstructionKind.Unknown;
        }

        return ClassifyXsltInstruction(Loader.XsltNodeLocalName(parent));
    }

    internal bool IsXsltSortParent(XsltInstructionKind parent)
    {
        return parent == XsltInstructionKind.ApplyTemplates || parent == XsltInstructionKind.ForEach || parent == XsltInstructionKind.ForEachGroup || parent == XsltInstructionKind.PerformSort;
    }

    internal bool IsXsltWithParamParent(XsltInstructionKind parent)
    {
        return parent == XsltInstructionKind.ApplyImports || parent == XsltInstructionKind.ApplyTemplates || parent == XsltInstructionKind.CallTemplate || parent == XsltInstructionKind.Evaluate || parent == XsltInstructionKind.NextIteration || parent == XsltInstructionKind.NextMatch;
    }

    internal bool HasXsltInstructionAncestor(XmlNode node, XsltInstructionKind kind)
    {
        XmlNode? current = node.Parent;
        while (current != null)
        {
            if (Loader.XsltNodeNamespace(current) == XsltNamespaces.Xslt && ClassifyXsltInstruction(Loader.XsltNodeLocalName(current)) == kind)
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    internal bool HasXsltTemplateRuleAncestor(XmlNode node)
    {
        XmlNode? current = node.Parent;
        while (current != null)
        {
            if (Loader.XsltNodeNamespace(current) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(current) == "template")
            {
                return Loader.FindXsltAttribute(current, "match", "") != null;
            }

            current = current.Parent;
        }

        return false;
    }

    internal void ValidateXsltInstructionRequiredAttributes(XsltInstructionPlan plan)
    {
        XsltInstructionKind kind = plan.Kind;
        if (kind == XsltInstructionKind.AnalyzeString)
        {
            RequireXsltInstructionAttribute(plan, "select");
            RequireXsltInstructionAttribute(plan, "regex");
        }
        else if (kind == XsltInstructionKind.Assert || kind == XsltInstructionKind.If || kind == XsltInstructionKind.When)
        {
            RequireXsltInstructionAttribute(plan, "test");
        }
        else if (kind == XsltInstructionKind.Attribute || kind == XsltInstructionKind.Element || kind == XsltInstructionKind.Namespace || kind == XsltInstructionKind.ProcessingInstruction || kind == XsltInstructionKind.WithParam)
        {
            RequireXsltInstructionAttribute(plan, "name");
        }
        else if (kind == XsltInstructionKind.CallTemplate)
        {
            RequireXsltInstructionAttribute(plan, "name");
        }
        else if (kind == XsltInstructionKind.CopyOf || kind == XsltInstructionKind.ForEach || kind == XsltInstructionKind.ForEachGroup || kind == XsltInstructionKind.Iterate || kind == XsltInstructionKind.ValueOf)
        {
            RequireXsltInstructionAttribute(plan, "select");
        }
        else if (kind == XsltInstructionKind.Evaluate)
        {
            RequireXsltInstructionAttribute(plan, "xpath");
        }
        else if (kind == XsltInstructionKind.MapEntry)
        {
            RequireXsltInstructionAttribute(plan, "key");
        }
        else if (kind == XsltInstructionKind.SourceDocument)
        {
            RequireXsltInstructionAttribute(plan, "href");
        }
        else if (kind == XsltInstructionKind.MergeSource)
        {
            RequireXsltInstructionAttribute(plan, "select");
        }

        if (kind == XsltInstructionKind.ForEachGroup)
        {
            int count = CountXsltInstructionAttributes(plan.Instruction, ["group-by", "group-adjacent", "group-starting-with", "group-ending-with"]);
            if (count != 1)
            {
                throw CreateXsltInstructionError("xsl:for-each-group requires exactly one grouping attribute.", XsltErrorCodes.XTSE1080, plan.Instruction, plan.Module);
            }

            bool keyed = Loader.FindXsltAttribute(plan.Instruction, "group-by", "") != null || Loader.FindXsltAttribute(plan.Instruction, "group-adjacent", "") != null;
            bool keyOptions = Loader.FindXsltAttribute(plan.Instruction, "composite", "") != null || Loader.FindXsltAttribute(plan.Instruction, "collation", "") != null;
            if (!keyed && keyOptions)
            {
                throw CreateXsltInstructionError("The composite and collation attributes require group-by or group-adjacent.", XsltErrorCodes.XTSE1090, plan.Instruction, plan.Module);
            }
        }

        if (kind == XsltInstructionKind.Number)
        {
            ValidateXsltNumberAttributes(plan);
        }
    }

    internal void ValidateXsltNumberAttributes(XsltInstructionPlan plan)
    {
        string? value = Loader.FindXsltAttribute(plan.Instruction, "value", "");
        if (value != null && CountXsltInstructionAttributes(plan.Instruction, ["select", "level", "count", "from"]) != 0)
        {
            throw CreateXsltInstructionError("The value attribute of xsl:number cannot be combined with select, level, count, or from.", XsltErrorCodes.XTSE0975, plan.Instruction, plan.Module);
        }

        string? level = Loader.FindXsltAttribute(plan.Instruction, "level", "");
        if (level != null && level != "single" && level != "multiple" && level != "any")
        {
            throw CreateXsltInstructionError("The level attribute of xsl:number must be single, multiple, or any.", XsltErrorCodes.XTSE0020, plan.Instruction, plan.Module);
        }
    }

    internal void RequireXsltInstructionAttribute(XsltInstructionPlan plan, string name)
    {
        string? value = Loader.FindXsltAttribute(plan.Instruction, name, "");
        if (value == null || value.Trim().Length == 0)
        {
            throw CreateXsltInstructionError("The xsl:" + plan.LocalName + " instruction requires a non-empty " + name + " attribute.", XsltErrorCodes.XTSE0010, plan.Instruction, plan.Module);
        }
    }

    internal int CountXsltInstructionAttributes(XmlNode node, string[] names)
    {
        int count = 0;
        int index = 0;
        while (index < names.Length)
        {
            if (Loader.FindXsltAttribute(node, names[index], "") != null)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal void ValidateXsltInstructionAttributes(XsltInstructionPlan plan)
    {
        XmlNode[] attributes = plan.Instruction.Attributes == null ? [] : plan.Instruction.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode attribute = attributes[index];
            string namespaceUri = Loader.XsltNodeNamespace(attribute);
            string name = Loader.XsltNodeLocalName(attribute);
            if (namespaceUri.Length == 0 && !IsXsltInstructionAttributeAllowed(plan.Kind, name) && !IsXsltStandardInstructionAttribute(name))
            {
                throw CreateXsltInstructionError("The " + name + " attribute is not permitted on xsl:" + plan.LocalName + ".", XsltErrorCodes.XTSE0090, attribute, plan.Module);
            }

            index = index + 1;
        }

        ValidateXsltInstructionBooleanAttributes(plan);
        ValidateXsltInstructionSchemaAttributes(plan);
        ValidateXsltMergeSourceAttributes(plan);
    }

    internal void ValidateXsltMergeSourceAttributes(XsltInstructionPlan plan)
    {
        if (plan.Kind != XsltInstructionKind.MergeSource)
        {
            return;
        }

        string? item = Loader.FindXsltAttribute(plan.Instruction, "for-each-item", "");
        string? source = Loader.FindXsltAttribute(plan.Instruction, "for-each-source", "");
        string? accumulators = Loader.FindXsltAttribute(plan.Instruction, "use-accumulators", "");
        string? streamable = Loader.FindXsltAttribute(plan.Instruction, "streamable", "");
        string? validation = Loader.FindXsltAttribute(plan.Instruction, "validation", "");
        string? type = Loader.FindXsltAttribute(plan.Instruction, "type", "");
        if (item != null && (source != null || accumulators != null || streamable != null) || source == null && (accumulators != null || validation != null || type != null))
        {
            throw CreateXsltInstructionError("The merge source selection attributes are not a permitted combination.", XsltErrorCodes.XTSE3195, plan.Instruction, plan.Module);
        }
    }

    internal bool IsXsltStandardInstructionAttribute(string name)
    {
        return name == "version" || name == "use-when" || name == "expand-text" || name == "xpath-default-namespace" || name == "default-collation" ||
            name == "extension-element-prefixes" || name == "exclude-result-prefixes" || name == "default-mode" || name == "default-validation";
    }

    internal bool IsXsltInstructionAttributeAllowed(XsltInstructionKind kind, string name)
    {
        if (kind == XsltInstructionKind.Unknown) return true;
        if (kind == XsltInstructionKind.Variable || kind == XsltInstructionKind.Parameter) return name == "name" || name == "select" || name == "as" || name == "static" || name == "required" || name == "tunnel";
        if (kind == XsltInstructionKind.ContextItem) return name == "use" || name == "as";
        if (kind == XsltInstructionKind.ApplyTemplates) return name == "select" || name == "mode";
        if (kind == XsltInstructionKind.CallTemplate) return name == "name";
        if (kind == XsltInstructionKind.WithParam) return name == "name" || name == "select" || name == "as" || name == "tunnel";
        if (kind == XsltInstructionKind.Assert) return name == "test" || name == "select" || name == "error-code";
        if (kind == XsltInstructionKind.Attribute) return name == "name" || name == "namespace" || name == "select" || name == "separator" || name == "validation" || name == "type";
        if (kind == XsltInstructionKind.Namespace) return name == "name" || name == "select";
        if (kind == XsltInstructionKind.Break || kind == XsltInstructionKind.Comment || kind == XsltInstructionKind.MapEntry || kind == XsltInstructionKind.Message || kind == XsltInstructionKind.Namespace || kind == XsltInstructionKind.PerformSort || kind == XsltInstructionKind.Try || kind == XsltInstructionKind.ArrayMember || kind == XsltInstructionKind.OnCompletion || kind == XsltInstructionKind.OnEmpty || kind == XsltInstructionKind.OnNonEmpty) return name == "select" || name == "key" || name == "terminate" || name == "error-code" || name == "rollback-output";
        if (kind == XsltInstructionKind.Catch) return name == "errors" || name == "select";
        if (kind == XsltInstructionKind.Copy) return name == "select" || name == "copy-namespaces" || name == "inherit-namespaces" || name == "use-attribute-sets" || name == "validation" || name == "type";
        if (kind == XsltInstructionKind.CopyOf) return name == "select" || name == "copy-accumulators" || name == "copy-namespaces" || name == "validation" || name == "type";
        if (kind == XsltInstructionKind.Document) return name == "validation" || name == "type";
        if (kind == XsltInstructionKind.Element) return name == "name" || name == "namespace" || name == "inherit-namespaces" || name == "use-attribute-sets" || name == "validation" || name == "type";
        if (kind == XsltInstructionKind.Evaluate) return name == "xpath" || name == "as" || name == "context-item" || name == "base-uri" || name == "namespace-context" || name == "schema-aware" || name == "with-params" || name == "options";
        if (kind == XsltInstructionKind.ForEach || kind == XsltInstructionKind.Iterate) return name == "select";
        if (kind == XsltInstructionKind.ForEachGroup) return name == "select" || name == "group-by" || name == "group-adjacent" || name == "group-starting-with" || name == "group-ending-with" || name == "composite" || name == "collation";
        if (kind == XsltInstructionKind.If || kind == XsltInstructionKind.When) return name == "test";
        if (kind == XsltInstructionKind.MergeSource) return name == "name" || name == "for-each-item" || name == "for-each-source" || name == "select" || name == "streamable" || name == "use-accumulators" || name == "sort-before-merge" || name == "validation" || name == "type";
        if (kind == XsltInstructionKind.MergeKey) return name == "select" || name == "lang" || name == "data-type" || name == "order" || name == "case-order" || name == "collation";
        if (kind == XsltInstructionKind.Sort) return name == "select" || name == "lang" || name == "data-type" || name == "order" || name == "case-order" || name == "collation" || name == "stable";
        if (kind == XsltInstructionKind.Number) return name == "select" || name == "value" || name == "level" || name == "count" || name == "from" || name == "format" || name == "lang" || name == "letter-value" || name == "ordinal" || name == "grouping-separator" || name == "grouping-size" || name == "start-at";
        if (kind == XsltInstructionKind.ProcessingInstruction) return name == "name" || name == "select";
        if (kind == XsltInstructionKind.ResultDocument) return IsXsltResultDocumentAttribute(name);
        if (kind == XsltInstructionKind.Sequence) return name == "select";
        if (kind == XsltInstructionKind.SourceDocument) return name == "href" || name == "streamable" || name == "validation" || name == "type" || name == "use-accumulators";
        if (kind == XsltInstructionKind.Text) return name == "disable-output-escaping";
        if (kind == XsltInstructionKind.ValueOf) return name == "select" || name == "separator" || name == "disable-output-escaping";
        if (kind == XsltInstructionKind.AnalyzeString) return name == "select" || name == "regex" || name == "flags";
        return kind == XsltInstructionKind.ApplyImports || kind == XsltInstructionKind.Array || kind == XsltInstructionKind.Choose || kind == XsltInstructionKind.Fallback || kind == XsltInstructionKind.Fork || kind == XsltInstructionKind.Map || kind == XsltInstructionKind.MatchingSubstring || kind == XsltInstructionKind.Merge || kind == XsltInstructionKind.MergeAction || kind == XsltInstructionKind.NextIteration || kind == XsltInstructionKind.NextMatch || kind == XsltInstructionKind.NonMatchingSubstring || kind == XsltInstructionKind.OnCompletion || kind == XsltInstructionKind.OnEmpty || kind == XsltInstructionKind.OnNonEmpty || kind == XsltInstructionKind.Otherwise || kind == XsltInstructionKind.WherePopulated;
    }

    internal bool IsXsltResultDocumentAttribute(string name)
    {
        return name == "href" || name == "format" || name == "validation" || name == "type" || name == "allow-duplicate-names" || name == "build-tree" ||
            name == "byte-order-mark" || name == "cdata-section-elements" || name == "doctype-public" || name == "doctype-system" || name == "encoding" ||
            name == "escape-uri-attributes" || name == "html-version" || name == "include-content-type" || name == "indent" || name == "item-separator" ||
            name == "json-node-output-method" || name == "media-type" || name == "method" || name == "normalization-form" || name == "omit-xml-declaration" ||
            name == "output-version" || name == "parameter-document" || name == "standalone" || name == "suppress-indentation" || name == "undeclare-prefixes" || name == "use-character-maps";
    }

    internal void ValidateXsltInstructionBooleanAttributes(XsltInstructionPlan plan)
    {
        string[] names = ["copy-accumulators", "copy-namespaces", "inherit-namespaces", "composite", "streamable", "sort-before-merge", "schema-aware", "stable", "disable-output-escaping", "rollback-output", "tunnel"];
        int index = 0;
        while (index < names.Length)
        {
            string? source = Loader.FindXsltAttribute(plan.Instruction, names[index], "");
            bool valueTemplate = plan.Kind == XsltInstructionKind.Sort && names[index] == "stable" || plan.Kind == XsltInstructionKind.Evaluate && names[index] == "schema-aware";
            if (!valueTemplate && source != null && source != "yes" && source != "no" && source != "true" && source != "false" && source != "1" && source != "0")
            {
                throw CreateXsltInstructionError("The " + names[index] + " attribute must be a valid boolean value.", XsltErrorCodes.XTSE0020, plan.Instruction, plan.Module);
            }

            index = index + 1;
        }
    }

    internal void ValidateXsltInstructionSchemaAttributes(XsltInstructionPlan plan)
    {
        string? validation = Loader.FindXsltAttribute(plan.Instruction, "validation", "");
        string? type = Loader.FindXsltAttribute(plan.Instruction, "type", "");
        if (validation != null && type != null)
        {
            throw CreateXsltInstructionError("The validation and type attributes are mutually exclusive.", XsltErrorCodes.XTSE1505, plan.Instruction, plan.Module);
        }

        if (validation != null && validation != "strict" && validation != "lax" && validation != "preserve" && validation != "strip")
        {
            throw CreateXsltInstructionError("The validation attribute has an invalid value.", XsltErrorCodes.XTSE0020, plan.Instruction, plan.Module);
        }

        if (Loader.Options.ConformanceLevel != XsltConformanceLevel.SchemaAware && (type != null || validation == "strict"))
        {
            throw CreateXsltInstructionError("This instruction requires a schema-aware XSLT processor.", XsltErrorCodes.XTSE1660, plan.Instruction, plan.Module);
        }
    }

    internal void ValidateXsltInstructionContent(XsltInstructionPlan plan)
    {
        XsltInstructionKind kind = plan.Kind;
        string? select = Loader.FindXsltAttribute(plan.Instruction, "select", "");
        if (kind == XsltInstructionKind.Choose)
        {
            ValidateXsltChooseContent(plan);
        }
        else if (kind == XsltInstructionKind.AnalyzeString)
        {
            ValidateXsltAnalyzeStringContent(plan);
        }
        else if (kind == XsltInstructionKind.Try)
        {
            ValidateXsltTryContent(plan);
        }
        else if (kind == XsltInstructionKind.Map || kind == XsltInstructionKind.Merge || kind == XsltInstructionKind.MergeSource || kind == XsltInstructionKind.Fork || kind == XsltInstructionKind.Evaluate || kind == XsltInstructionKind.NextIteration || kind == XsltInstructionKind.ApplyTemplates || kind == XsltInstructionKind.CallTemplate || kind == XsltInstructionKind.NextMatch || kind == XsltInstructionKind.ApplyImports)
        {
            ValidateXsltRestrictedChildElements(plan);
        }

        else if (kind == XsltInstructionKind.CopyOf || kind == XsltInstructionKind.ValueOf || kind == XsltInstructionKind.Number)
        {
            if (HasXsltSignificantInstructionContent(plan.Instruction))
            {
                throw CreateXsltInstructionError("The xsl:" + plan.LocalName + " instruction must be empty.", XsltErrorCodes.XTSE0010, plan.Instruction, plan.Module);
            }
        }

        if (kind == XsltInstructionKind.Sort)
        {
            ValidateXsltSortContent(plan);
        }

        if (kind == XsltInstructionKind.MergeKey && select != null && HasXsltSignificantInstructionContent(plan.Instruction))
        {
            throw CreateXsltInstructionError("An xsl:merge-key element with a select attribute must be empty.", XsltErrorCodes.XTSE3200, plan.Instruction, plan.Module);
        }

        if (kind == XsltInstructionKind.Text && SelectXsltInstructionElementChildren(plan.Instruction).Length != 0)
        {
            throw CreateXsltInstructionError("xsl:text must not contain element children.", XsltErrorCodes.XTSE0010, plan.Instruction, plan.Module);
        }

        if (select != null && HasXsltSignificantInstructionContent(plan.Instruction) && AllowsXsltSelectOrContent(kind))
        {
            string code = kind == XsltInstructionKind.Sequence || kind == XsltInstructionKind.OnEmpty || kind == XsltInstructionKind.OnNonEmpty ? XsltErrorCodes.XTSE3185 : kind == XsltInstructionKind.Catch ? XsltErrorCodes.XTSE3150 : XsltErrorCodes.XTSE0010;
            throw CreateXsltInstructionError("The select attribute and sequence constructor are mutually exclusive.", code, plan.Instruction, plan.Module);
        }

        if (kind == XsltInstructionKind.PerformSort)
        {
            ValidateXsltPerformSortContent(plan);
        }

        if (kind == XsltInstructionKind.ForEach || kind == XsltInstructionKind.ForEachGroup || kind == XsltInstructionKind.PerformSort)
        {
            ValidateXsltSortSpecificationPosition(plan);
        }

        if (kind == XsltInstructionKind.Iterate)
        {
            ValidateXsltIterateContent(plan);
        }
    }

    internal void ValidateXsltPerformSortContent(XsltInstructionPlan plan)
    {
        XmlNode[] elements = SelectXsltInstructionElementChildren(plan.Instruction);
        int sortCount = 0;
        bool ordinarySeen = false;
        int index = 0;
        while (index < elements.Length)
        {
            string name = Loader.XsltNodeNamespace(elements[index]) == XsltNamespaces.Xslt ? Loader.XsltNodeLocalName(elements[index]) : "";
            if (name == "sort" && !ordinarySeen)
            {
                sortCount = sortCount + 1;
            }
            else if (name != "fallback")
            {
                ordinarySeen = true;
            }

            index = index + 1;
        }

        if (sortCount == 0)
        {
            throw CreateXsltInstructionError("xsl:perform-sort requires at least one xsl:sort child.", XsltErrorCodes.XTSE0010, plan.Instruction, plan.Module);
        }

        if (Loader.FindXsltAttribute(plan.Instruction, "select", "") != null && (ordinarySeen || HasXsltPerformSortTextContent(plan.Instruction)))
        {
            throw CreateXsltInstructionError("An xsl:perform-sort with a select attribute may contain only sort keys.", XsltErrorCodes.XTSE1040, plan.Instruction, plan.Module);
        }
    }

    internal bool HasXsltPerformSortTextContent(XmlNode node)
    {
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if ((child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && !new XsltStylesheetAssembler(Loader).IsXsltWhitespace(child.Value))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void ValidateXsltSortContent(XsltInstructionPlan plan)
    {
        string? select = Loader.FindXsltAttribute(plan.Instruction, "select", "");
        if (select != null && HasXsltSignificantInstructionContent(plan.Instruction))
        {
            throw CreateXsltInstructionError("An xsl:sort element with a select attribute must be empty.", XsltErrorCodes.XTSE1015, plan.Instruction, plan.Module);
        }

        string? stable = Loader.FindXsltAttribute(plan.Instruction, "stable", "");
        if (stable == null)
        {
            return;
        }

        XmlNode? parent = plan.Instruction.Parent;
        XmlNode[] siblings = parent == null || parent.Children == null ? [] : parent.Children;
        int index = 0;
        while (index < siblings.Length && !object.ReferenceEquals(siblings[index], plan.Instruction))
        {
            if (siblings[index].Kind == XmlNodeKind.Element && Loader.XsltNodeNamespace(siblings[index]) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(siblings[index]) == "sort")
            {
                throw CreateXsltInstructionError("The stable attribute is permitted only on the first xsl:sort element.", XsltErrorCodes.XTSE1017, plan.Instruction, plan.Module);
            }

            index = index + 1;
        }
    }

    internal void ValidateXsltSortSpecificationPosition(XsltInstructionPlan plan)
    {
        XmlNode[] children = plan.Instruction.Children == null ? [] : plan.Instruction.Children;
        bool ordinarySeen = false;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            bool whitespace = (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && new XsltStylesheetAssembler(Loader).IsXsltWhitespace(child.Value);
            bool sort = child.Kind == XmlNodeKind.Element && Loader.XsltNodeNamespace(child) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(child) == "sort";
            bool fallback = child.Kind == XmlNodeKind.Element && Loader.XsltNodeNamespace(child) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(child) == "fallback";
            if (sort && ordinarySeen)
            {
                throw CreateXsltInstructionError("xsl:sort children must precede the sequence constructor.", XsltErrorCodes.XTSE0010, child, plan.Module);
            }

            if (!sort && !fallback && !whitespace)
            {
                ordinarySeen = true;
            }

            index = index + 1;
        }
    }

    internal void ValidateXsltIterateContent(XsltInstructionPlan plan)
    {
        XmlNode[] elements = SelectXsltInstructionElementChildren(plan.Instruction);
        bool ordinarySeen = false;
        bool completionSeen = false;
        int index = 0;
        while (index < elements.Length)
        {
            string name = Loader.XsltNodeNamespace(elements[index]) == XsltNamespaces.Xslt ? Loader.XsltNodeLocalName(elements[index]) : "";
            if (name == "param" && !ordinarySeen && !completionSeen)
            {
                index = index + 1;
                continue;
            }

            if (name == "on-completion" && !ordinarySeen && !completionSeen)
            {
                completionSeen = true;
                index = index + 1;
                continue;
            }

            ordinarySeen = true;
            if (name == "param" || name == "on-completion")
            {
                throw CreateXsltInstructionError("Iteration parameters and on-completion must precede the loop body.", XsltErrorCodes.XTSE0010, elements[index], plan.Module);
            }

            index = index + 1;
        }
    }

    internal void ValidateXsltChooseContent(XsltInstructionPlan plan)
    {
        XmlNode[] elements = SelectXsltInstructionElementChildren(plan.Instruction);
        bool otherwise = false;
        int whenCount = 0;
        int index = 0;
        while (index < elements.Length)
        {
            string name = Loader.XsltNodeNamespace(elements[index]) == XsltNamespaces.Xslt ? Loader.XsltNodeLocalName(elements[index]) : "";
            if (name == "when" && !otherwise)
            {
                whenCount = whenCount + 1;
            }
            else if (name == "otherwise" && !otherwise && index == elements.Length - 1)
            {
                otherwise = true;
            }
            else
            {
                throw CreateXsltInstructionError("xsl:choose may contain only xsl:when elements followed by at most one xsl:otherwise.", XsltErrorCodes.XTSE0010, elements[index], plan.Module);
            }

            index = index + 1;
        }

        if (whenCount == 0)
        {
            throw CreateXsltInstructionError("xsl:choose requires at least one xsl:when child.", XsltErrorCodes.XTSE0010, plan.Instruction, plan.Module);
        }
    }

    internal void ValidateXsltAnalyzeStringContent(XsltInstructionPlan plan)
    {
        XmlNode[] elements = SelectXsltInstructionElementChildren(plan.Instruction);
        bool matching = false;
        bool nonMatching = false;
        bool fallback = false;
        int index = 0;
        while (index < elements.Length)
        {
            string name = Loader.XsltNodeNamespace(elements[index]) == XsltNamespaces.Xslt ? Loader.XsltNodeLocalName(elements[index]) : "";
            if (name == "matching-substring" && !matching && !nonMatching && !fallback)
            {
                matching = true;
            }
            else if (name == "non-matching-substring" && !nonMatching && !fallback)
            {
                nonMatching = true;
            }
            else if (name == "fallback")
            {
                fallback = true;
            }
            else
            {
                throw CreateXsltInstructionError("xsl:analyze-string has an invalid helper child.", XsltErrorCodes.XTSE0010, elements[index], plan.Module);
            }

            index = index + 1;
        }

        if (!matching && !nonMatching)
        {
            throw CreateXsltInstructionError("xsl:analyze-string requires a matching-substring or non-matching-substring child.", XsltErrorCodes.XTSE1130, plan.Instruction, plan.Module);
        }
    }

    internal void ValidateXsltTryContent(XsltInstructionPlan plan)
    {
        XmlNode[] elements = SelectXsltInstructionElementChildren(plan.Instruction);
        bool catchSeen = false;
        bool ordinarySeen = false;
        int index = 0;
        while (index < elements.Length)
        {
            string name = Loader.XsltNodeNamespace(elements[index]) == XsltNamespaces.Xslt ? Loader.XsltNodeLocalName(elements[index]) : "";
            bool currentCatch = name == "catch";
            if (currentCatch)
            {
                catchSeen = true;
            }
            else if (name != "fallback")
            {
                ordinarySeen = true;
                if (catchSeen)
                {
                    throw CreateXsltInstructionError("All xsl:catch children must follow the xsl:try sequence constructor.", XsltErrorCodes.XTSE0010, elements[index], plan.Module);
                }
            }

            index = index + 1;
        }

        if (!catchSeen)
        {
            throw CreateXsltInstructionError("xsl:try requires at least one xsl:catch child.", XsltErrorCodes.XTSE0010, plan.Instruction, plan.Module);
        }

        if (Loader.FindXsltAttribute(plan.Instruction, "select", "") != null && ordinarySeen)
        {
            throw CreateXsltInstructionError("An xsl:try select attribute excludes an ordinary sequence constructor.", XsltErrorCodes.XTSE3140, plan.Instruction, plan.Module);
        }

        if (Loader.FindXsltAttribute(plan.Instruction, "select", "") != null)
        {
            XmlNode[] children = plan.Instruction.Children == null ? [] : plan.Instruction.Children;
            index = 0;
            while (index < children.Length)
            {
                XmlNode child = children[index];
                if ((child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && !new XsltStylesheetAssembler(Loader).IsXsltWhitespace(child.Value))
                {
                    throw CreateXsltInstructionError("An xsl:try select attribute excludes significant text content.", XsltErrorCodes.XTSE3140, child, plan.Module);
                }

                index = index + 1;
            }
        }
    }

    internal void ValidateXsltRestrictedChildElements(XsltInstructionPlan plan)
    {
        XmlNode[] elements = SelectXsltInstructionElementChildren(plan.Instruction);
        bool parameterSeen = false;
        List<string> parameterNames = new List<string>();
        int index = 0;
        while (index < elements.Length)
        {
            string name = Loader.XsltNodeNamespace(elements[index]) == XsltNamespaces.Xslt ? Loader.XsltNodeLocalName(elements[index]) : "";
            bool valid = true;
            if (plan.Kind == XsltInstructionKind.Map) valid = name == "map-entry";
            else if (plan.Kind == XsltInstructionKind.Merge) valid = name == "merge-source" || name == "merge-action" || name == "fallback";
            else if (plan.Kind == XsltInstructionKind.MergeSource) valid = name == "merge-key";
            else if (plan.Kind == XsltInstructionKind.Fork) valid = name == "sequence" || name == "for-each-group" || name == "fallback";
            else if (plan.Kind == XsltInstructionKind.Evaluate) valid = name == "with-param" || name == "fallback";
            else if (plan.Kind == XsltInstructionKind.NextIteration) valid = name == "with-param";
            else if (plan.Kind == XsltInstructionKind.ApplyTemplates) valid = name == "sort" || name == "with-param";
            else if (plan.Kind == XsltInstructionKind.CallTemplate || plan.Kind == XsltInstructionKind.NextMatch || plan.Kind == XsltInstructionKind.ApplyImports) valid = name == "with-param";
            if (!valid)
            {
                throw CreateXsltInstructionError("xsl:" + plan.LocalName + " contains an invalid helper child.", XsltErrorCodes.XTSE0010, elements[index], plan.Module);
            }

            if (name == "with-param")
            {
                parameterSeen = true;
                string? lexicalName = Loader.FindXsltAttribute(elements[index], "name", "");
                string value = lexicalName == null ? "" : lexicalName.Trim();
                if (parameterNames.Contains(value))
                {
                    throw CreateXsltInstructionError("An instruction must not contain two xsl:with-param children with the same name.", XsltErrorCodes.XTSE0670, elements[index], plan.Module);
                }

                parameterNames.Add(value);
            }
            else if (name == "sort" && parameterSeen)
            {
                throw CreateXsltInstructionError("xsl:sort children must precede xsl:with-param children.", XsltErrorCodes.XTSE0010, elements[index], plan.Module);
            }

            index = index + 1;
        }


        if (plan.Kind == XsltInstructionKind.Merge)
        {
            int sourceCount = 0;
            int actionCount = 0;
            int keyCount = -1;
            bool actionSeen = false;
            bool fallbackSeen = false;
            List<string> sourceNames = new List<string>();
            index = 0;
            while (index < elements.Length)
            {
                string name = Loader.XsltNodeLocalName(elements[index]);
                if (name == "merge-source")
                {
                    sourceCount = sourceCount + 1;
                    XmlNode[] keys = SelectXsltInstructionElementChildren(elements[index]);
                    if (keyCount < 0) keyCount = keys.Length;
                    else if (keyCount != keys.Length) throw CreateXsltInstructionError("All merge sources must declare the same number of merge keys.", XsltErrorCodes.XTSE2200, elements[index], plan.Module);
                    string? optionalSourceName = Loader.FindXsltAttribute(elements[index], "name", "");
                    if (optionalSourceName != null)
                    {
                        string sourceName = optionalSourceName.Trim();
                        if (!new XdmStringDatatype().IsValidXdmName(sourceName, true))
                        {
                            throw CreateXsltInstructionError("A merge source name must be an NCName.", XsltErrorCodes.XTSE0020, elements[index], plan.Module);
                        }

                        if (sourceNames.Contains(sourceName))
                        {
                            throw CreateXsltInstructionError("Sibling merge sources must have different names.", XsltErrorCodes.XTSE3190, elements[index], plan.Module);
                        }

                        sourceNames.Add(sourceName);
                    }

                    if (actionSeen || fallbackSeen) throw CreateXsltInstructionError("Merge sources must precede the merge action.", XsltErrorCodes.XTSE0010, elements[index], plan.Module);
                }
                if (name == "merge-action")
                {
                    if (fallbackSeen) throw CreateXsltInstructionError("The merge action must precede fallback instructions.", XsltErrorCodes.XTSE0010, elements[index], plan.Module);
                    actionCount = actionCount + 1;
                    actionSeen = true;
                }
                if (name == "fallback")
                {
                    if (!actionSeen) throw CreateXsltInstructionError("Merge fallback instructions must follow the merge action.", XsltErrorCodes.XTSE0010, elements[index], plan.Module);
                    fallbackSeen = true;
                }
                index = index + 1;
            }

            if (sourceCount == 0 || actionCount != 1)
            {
                throw CreateXsltInstructionError("xsl:merge requires merge-source children followed by one merge-action.", XsltErrorCodes.XTSE0010, plan.Instruction, plan.Module);
            }
        }

        if (plan.Kind == XsltInstructionKind.MergeSource && elements.Length == 0)
        {
            throw CreateXsltInstructionError("xsl:merge-source requires at least one xsl:merge-key child.", XsltErrorCodes.XTSE0010, plan.Instruction, plan.Module);
        }

        if (plan.Kind == XsltInstructionKind.Fork)
        {
            int groupCount = 0;
            int sequenceCount = 0;
            index = 0;
            while (index < elements.Length)
            {
                string name = Loader.XsltNodeLocalName(elements[index]);
                if (name == "for-each-group") groupCount = groupCount + 1;
                if (name == "sequence") sequenceCount = sequenceCount + 1;
                index = index + 1;
            }

            if (groupCount > 1 || groupCount == 1 && sequenceCount != 0)
            {
                throw CreateXsltInstructionError("xsl:fork must contain either one for-each-group or a sequence of xsl:sequence instructions.", XsltErrorCodes.XTSE0010, plan.Instruction, plan.Module);
            }
        }
    }

    internal bool AllowsXsltSelectOrContent(XsltInstructionKind kind)
    {
        return kind == XsltInstructionKind.ArrayMember || kind == XsltInstructionKind.Attribute || kind == XsltInstructionKind.Break ||
            kind == XsltInstructionKind.Catch || kind == XsltInstructionKind.Comment || kind == XsltInstructionKind.MapEntry ||
            kind == XsltInstructionKind.Namespace || kind == XsltInstructionKind.OnCompletion || kind == XsltInstructionKind.OnEmpty || kind == XsltInstructionKind.OnNonEmpty ||
            kind == XsltInstructionKind.ProcessingInstruction || kind == XsltInstructionKind.Sequence || kind == XsltInstructionKind.WithParam;
    }

    internal bool HasXsltSignificantInstructionContent(XmlNode node)
    {
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element || (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && !new XsltStylesheetAssembler(Loader).IsXsltWhitespace(child.Value))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XmlNode[] SelectXsltInstructionElementChildren(XmlNode node)
    {
        List<XmlNode> result = new List<XmlNode>();
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].Kind == XmlNodeKind.Element)
            {
                result.Add(children[index]);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal XmlNode[] SelectXsltInstructionContentNodes(XmlNode node)
    {
        List<XmlNode> result = new List<XmlNode>();
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].Kind == XmlNodeKind.Element || children[index].Kind == XmlNodeKind.Text || children[index].Kind == XmlNodeKind.CData)
            {
                result.Add(children[index]);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal XsltInstructionAttributePlan[] CompileXsltInstructionAttributes(XsltInstructionPlan instruction, XsltStylesheetPlan stylesheet)
    {
        List<XsltInstructionAttributePlan> result = new List<XsltInstructionAttributePlan>();
        XmlNode[] attributes = instruction.Instruction.Attributes == null ? [] : instruction.Instruction.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode attribute = attributes[index];
            if (Loader.XsltNodeNamespace(attribute).Length == 0)
            {
                string name = Loader.XsltNodeLocalName(attribute);
                string source = attribute.Value == null ? "" : attribute.Value;
                XsltInstructionAttributePlan plan = new XsltInstructionAttributePlan(attribute, name, source);
                if (IsXsltInstructionExpressionAttribute(instruction.Kind, name))
                {
                    plan.Expression = CompileXsltInstructionExpression(source, instruction, stylesheet, attribute);
                }
                else if (name == "as")
                {
                    plan.SequenceType = CompileXsltInstructionSequenceType(source, instruction, stylesheet, attribute);
                }
                else if (IsXsltInstructionPatternAttribute(instruction.Kind, name))
                {
                    plan.Pattern = CompileXsltInstructionPattern(source, instruction, stylesheet, attribute);
                }
                else if (IsXsltInstructionQNameAttribute(instruction.Kind, name))
                {
                    plan.ResolvedName = ResolveXsltInstructionQName(source, instruction, attribute);
                }

                result.Add(plan);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal bool IsXsltInstructionExpressionAttribute(XsltInstructionKind kind, string name)
    {
        if (name == "select") return kind != XsltInstructionKind.Unknown;
        if (name == "test") return kind == XsltInstructionKind.Assert || kind == XsltInstructionKind.If || kind == XsltInstructionKind.When;
        if (name == "key") return kind == XsltInstructionKind.MapEntry;
        if (name == "xpath" || name == "context-item" || name == "namespace-context" || name == "with-params" || name == "options") return kind == XsltInstructionKind.Evaluate;
        if (name == "group-by" || name == "group-adjacent") return kind == XsltInstructionKind.ForEachGroup;
        if (name == "value") return kind == XsltInstructionKind.Number;
        if (name == "for-each-item" || name == "for-each-source") return kind == XsltInstructionKind.MergeSource;
        return false;
    }

    internal bool HasXsltNamedAncestor(XmlNode node, string name)
    {
        XmlNode? current = node.Parent;
        while (current != null)
        {
            if (Loader.XsltNodeNamespace(current) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(current) == name)
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    internal bool IsXsltInstructionPatternAttribute(XsltInstructionKind kind, string name)
    {
        return kind == XsltInstructionKind.ForEachGroup && (name == "group-starting-with" || name == "group-ending-with") || kind == XsltInstructionKind.Number && (name == "count" || name == "from");
    }

    internal bool IsXsltInstructionQNameAttribute(XsltInstructionKind kind, string name)
    {
        if (name == "type") return true;
        return name == "name" && (kind == XsltInstructionKind.CallTemplate || kind == XsltInstructionKind.WithParam);
    }

    internal XPathPlan CompileXsltInstructionExpression(string source, XsltInstructionPlan instruction, XsltStylesheetPlan stylesheet, XmlNode attribute)
    {
        XPathStaticContext context = CreateXsltInstructionXPathContext(instruction, stylesheet);
        AddXsltInstructionFunctionBindings(context);
        Task<XPathPlan> pending = Task.Run(() => new XPathPlanCompiler(context).CompileXPathSource(source, instruction.Module.BaseUri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltInstructionError("The " + Loader.XsltNodeLocalName(attribute) + " expression could not be compiled.", Loader.StaticProcessor.ReadXsltTaskErrorCode(pending.Exception), attribute, instruction.Module);
        }

        return pending.Result;
    }

    internal void AddXsltInstructionFunctionBindings(XPathStaticContext context)
    {
        XPathFunctionBinding[] existing = context.Functions == null ? [] : context.Functions;
        string[] names = ["current-merge-group", "current-merge-group", "current-merge-key"];
        int[] arities = [0, 1, 0];
        XPathFunctionBinding[] result = new XPathFunctionBinding[existing.Length + names.Length];
        int index = 0;
        while (index < existing.Length)
        {
            result[index] = existing[index];
            index = index + 1;
        }

        int functionIndex = 0;
        while (functionIndex < names.Length)
        {
            result[index] = CreateXsltInstructionFunctionBinding(names[functionIndex], arities[functionIndex]);
            index = index + 1;
            functionIndex = functionIndex + 1;
        }

        context.Functions = result;
        new XsltPatternCompiler(Loader).AddXsltPatternKeyBindings(context);
    }

    internal XPathFunctionBinding CreateXsltInstructionFunctionBinding(string name, int arity)
    {
        XPathFunctionBinding result = new XPathFunctionBinding();
        result.Name = new XmlName { LocalName = name, QualifiedName = name, NamespaceUri = XPathNamespaces.Functions };
        result.Arity = arity;
        result.ParameterTypes = new XdmAtomicType[arity];
        result.ParameterOccurrences = new XdmOccurrence[arity];
        int index = 0;
        while (index < arity)
        {
            result.ParameterTypes[index] = name == "regex-group" ? XdmAtomicType.Integer : XdmAtomicType.String;
            result.ParameterOccurrences[index] = XdmOccurrence.ExactlyOne;
            index = index + 1;
        }

        result.ResultType = XdmAtomicType.AnyAtomicType;
        result.ResultOccurrence = XdmOccurrence.ZeroOrMore;
        result.BindingKey = "xslt:" + name + "#" + arity;
        result.IsDeterministic = true;
        SourceRange range = new SourceRange();
        result.ResultTypeSyntax = new XPathTypeSyntax("item", null, null, null, "*", false, range);
        return result;
    }

    internal XPathTypeSyntax CompileXsltInstructionSequenceType(string source, XsltInstructionPlan instruction, XsltStylesheetPlan stylesheet, XmlNode attribute)
    {
        XPathStaticContext context = CreateXsltInstructionXPathContext(instruction, stylesheet);
        Task<XPathTypeSyntax> pending = Task.Run(() => new XsltVariableCompiler(Loader).ParseXsltVariableSequenceType(source, context, instruction.Module.BaseUri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltInstructionError("The as attribute is not a valid sequence type.", Loader.StaticProcessor.ReadXsltTaskErrorCode(pending.Exception), attribute, instruction.Module);
        }

        return pending.Result;
    }

    internal XsltMatchPatternPlan CompileXsltInstructionPattern(string source, XsltInstructionPlan instruction, XsltStylesheetPlan stylesheet, XmlNode attribute)
    {
        XsltMatchPatternPlan result = new XsltMatchPatternPlan(source, attribute.Range);
        XPathStaticContext context = CreateXsltInstructionXPathContext(instruction, stylesheet);
        XsltPatternCompiler compiler = new XsltPatternCompiler(Loader);
        compiler.AddXsltPatternKeyBindings(context);
        Task<bool> pending = Task.Run(() => compiler.CompileXsltPattern(result, context, instruction.Module.BaseUri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltInstructionError("The instruction pattern could not be compiled.", Loader.StaticProcessor.ReadXsltTaskErrorCode(pending.Exception), attribute, instruction.Module);
        }

        return result;
    }

    internal XmlName ResolveXsltInstructionQName(string source, XsltInstructionPlan instruction, XmlNode attribute)
    {
        Task<XmlName> pending = Task.Run(() => Loader.StaticProcessor.ResolveXsltStaticVariableName(source.Trim(), instruction.Instruction, instruction.Module));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltInstructionError("The " + Loader.XsltNodeLocalName(attribute) + " attribute must be a valid EQName.", XsltErrorCodes.XTSE0280, attribute, instruction.Module);
        }

        return pending.Result;
    }

    internal XPathStaticContext CreateXsltInstructionXPathContext(XsltInstructionPlan instruction, XsltStylesheetPlan stylesheet)
    {
        XsltVariableCompiler variables = new XsltVariableCompiler(Loader);
        List<XsltVariablePlan> visible = variables.CreateXsltVisibleVariablePlans(stylesheet, instruction.Instruction);
        XPathStaticContext result = variables.CreateXsltVariableXPathContext(instruction.StaticContext, variables.CreateXsltVariableBindingsAtNode(visible, instruction.Instruction));
        if (HasXsltNamedAncestor(instruction.Instruction, "accumulator"))
        {
            AddXsltAccumulatorValueVariableBinding(result);
        }

        if (instruction.Kind == XsltInstructionKind.Catch || HasXsltNamedAncestor(instruction.Instruction, "catch"))
        {
            AddXsltCatchVariableBindings(result);
        }

        return result;
    }

    internal void AddXsltAccumulatorValueVariableBinding(XPathStaticContext context)
    {
        XPathVariableBinding[] existing = context.Variables == null ? [] : context.Variables;
        XPathVariableBinding value = new XPathVariableBinding();
        value.Name = new XmlName { LocalName = "value", QualifiedName = "value", NamespaceUri = "" };
        value.DeclaredType = XdmAtomicType.AnyAtomicType;
        value.DeclaredOccurrence = XdmOccurrence.ZeroOrMore;
        value.StaticTypeSyntax = new XPathTypeSyntax("item", null, null, null, "*", false, new SourceRange());
        int replace = -1;
        int index = 0;
        while (index < existing.Length)
        {
            XmlName? name = existing[index].Name;
            if (name != null && name.LocalName == "value" && (name.NamespaceUri == null || name.NamespaceUri.Length == 0))
            {
                replace = index;
            }

            index = index + 1;
        }

        if (replace >= 0)
        {
            existing[replace] = value;
            context.Variables = existing;
            return;
        }

        XPathVariableBinding[] result = new XPathVariableBinding[existing.Length + 1];
        global::System.Array.Copy(existing, result, existing.Length);
        result[existing.Length] = value;
        context.Variables = result;
    }

    internal void AddXsltCatchVariableBindings(XPathStaticContext context)
    {
        XPathVariableBinding[] existing = context.Variables == null ? [] : context.Variables;
        string[] names = ["code", "description", "value", "module", "line-number", "column-number", "additional"];
        XPathVariableBinding[] result = new XPathVariableBinding[existing.Length + names.Length];
        int index = 0;
        while (index < existing.Length)
        {
            result[index] = existing[index];
            index = index + 1;
        }

        int nameIndex = 0;
        while (nameIndex < names.Length)
        {
            XPathVariableBinding binding = new XPathVariableBinding();
            binding.Name = new XmlName { Prefix = "err", LocalName = names[nameIndex], QualifiedName = "err:" + names[nameIndex], NamespaceUri = XPathNamespaces.Errors };
            binding.DeclaredType = XdmAtomicType.AnyAtomicType;
            binding.DeclaredOccurrence = XdmOccurrence.ZeroOrMore;
            result[index] = binding;
            index = index + 1;
            nameIndex = nameIndex + 1;
        }

        context.Variables = result;
    }

    internal XsltStaticContext? FindXsltInstructionStaticContext(XsltStylesheetPlan stylesheet, XmlNode node)
    {
        int index = 0;
        while (index < stylesheet.StaticContexts.Length)
        {
            if (object.ReferenceEquals(stylesheet.StaticContexts[index].Node, node))
            {
                return stylesheet.StaticContexts[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltStaticException CreateXsltInstructionError(string message, string code, XmlNode node, XsltStylesheetModule module)
    {
        return new XsltStylesheetAssembler(Loader).CreateXsltAssemblyError(message, code, node, module);
    }
}
