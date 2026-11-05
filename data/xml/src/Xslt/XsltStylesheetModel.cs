namespace xml;

internal enum XsltStylesheetModuleKind
{
    Stylesheet = 0,
    Transform = 1,
    Simplified = 2,
    Package = 3
}

internal enum XsltStylesheetModuleRelation
{
    Principal = 0,
    Include = 1,
    Import = 2
}

internal enum XsltCompatibilityBehavior
{
    Backwards = 0,
    Standard = 1,
    Forwards = 2
}

internal class XsltStylesheetReference
{
    internal XsltStylesheetModule Source;
    internal XsltStylesheetModuleRelation Relation;
    internal XmlNode Node;
    internal string Href;
    internal string? BaseUri;
    internal XsltStylesheetModule? Target;

    internal XsltStylesheetReference(XsltStylesheetModule source, XsltStylesheetModuleRelation relation, XmlNode node, string href, string? baseUri)
    {
        Source = source;
        Relation = relation;
        Node = node;
        Href = href;
        BaseUri = baseUri;
        Target = null;
    }
}

internal class XsltStylesheetModule
{
    internal XmlDocument Document;
    internal XmlNode Root;
    internal XsltStylesheetModuleKind Kind;
    internal XsltStylesheetModuleRelation Relation;
    internal string Version;
    internal string? BaseUri;
    internal XsltStylesheetReference[] References;
    internal XsltStylesheetReference? Origin;
    internal bool IsComplete;

    internal XsltStylesheetModule(XmlDocument document, XmlNode root, XsltStylesheetModuleKind kind, XsltStylesheetModuleRelation relation, string version, string? baseUri)
    {
        Document = document;
        Root = root;
        Kind = kind;
        Relation = relation;
        Version = version;
        BaseUri = baseUri;
        References = [];
        Origin = null;
        IsComplete = false;
    }
}

internal class XsltStylesheetDeclaration
{
    internal XmlNode Node;
    internal XsltStylesheetModule Module;
    internal XsltStylesheetLevel Level;
    internal string LocalName;
    internal string EffectiveVersion;
    internal XsltCompatibilityBehavior CompatibilityBehavior;
    internal int ImportPrecedence;
    internal int DeclarationOrder;
    internal bool IsSynthetic;
    internal XsltStaticContext? StaticContext;

    internal XsltStylesheetDeclaration(XmlNode node, XsltStylesheetModule module, XsltStylesheetLevel level, string localName, string effectiveVersion, XsltCompatibilityBehavior compatibilityBehavior, int declarationOrder, bool isSynthetic)
    {
        Node = node;
        Module = module;
        Level = level;
        LocalName = localName;
        EffectiveVersion = effectiveVersion;
        CompatibilityBehavior = compatibilityBehavior;
        ImportPrecedence = -1;
        DeclarationOrder = declarationOrder;
        IsSynthetic = isSynthetic;
        StaticContext = null;
    }
}

internal class XsltStylesheetLevel
{
    internal XsltStylesheetLevel? Parent;
    internal XsltStylesheetModule InitialModule;
    internal XsltStylesheetModule[] Modules;
    internal XsltStylesheetLevel[] ImportedLevels;
    internal XsltStylesheetDeclaration[] Declarations;
    internal int ImportPrecedence;

    internal XsltStylesheetLevel(XsltStylesheetLevel? parent, XsltStylesheetModule initialModule)
    {
        Parent = parent;
        InitialModule = initialModule;
        Modules = [];
        ImportedLevels = [];
        Declarations = [];
        ImportPrecedence = -1;
    }
}

internal class XsltFallbackPlan
{
    internal XmlNode Instruction;
    internal XmlNode[] Fallbacks;
    internal string EffectiveVersion;

    internal XsltFallbackPlan(XmlNode instruction, XmlNode[] fallbacks, string effectiveVersion)
    {
        Instruction = instruction;
        Fallbacks = fallbacks;
        EffectiveVersion = effectiveVersion;
    }
}

internal class XsltStylesheetPlan
{
    internal XsltStylesheetModule PrincipalModule;
    internal XsltStylesheetModule[] Modules;
    internal XsltStylesheetLevel RootLevel;
    internal XsltStylesheetLevel[] Levels;
    internal XsltStylesheetDeclaration[] Declarations;
    internal XsltFallbackPlan[] Fallbacks;
    internal XsltStaticContext[] StaticContexts;
    internal XsltNamespaceAlias[] NamespaceAliases;
    internal string InitialMode;
    internal XsltStaticVariable[] StaticVariables;
    internal XsltVariablePlan[] Variables;
    internal XsltValueTemplate[] ValueTemplates;
    internal XsltTemplatePlan[] Templates;
    internal XsltTemplateRuleIndex[] TemplateRuleIndexes;
    internal XsltFunctionPlan[] Functions;
    internal XsltModePlan[] Modes;
    internal XsltAttributeSetPlan[] AttributeSets;
    internal XsltAttributeSetUsePlan[] AttributeSetUses;
    internal XsltKeyPlan[] Keys;
    internal XsltAccumulatorPlan[] Accumulators;
    internal XsltDecimalFormatPlan[] DecimalFormats;
    internal XsltCharacterMapPlan[] CharacterMaps;
    internal XsltOutputFormatPlan[] OutputFormats;
    internal XsltWhitespaceRulePlan[] WhitespaceRules;
    internal XsltInstructionPlan[] Instructions;
    internal XsltComponent[] Components;
    internal XPathPlan[] XPathPlans;
    internal XsltVariablePlan[] Globals;
    internal XsltTemplateRuleIndex[] PatternIndexes;
    internal XsltOutputFormatPlan[] Outputs;
    internal XsltPackagePlan[] Packages;
    internal XsltExecutableConformance Conformance;
    internal XsltSourceMapEntry[] SourceMap;
    internal XsltPackageMetadataPlan? Metadata;
    internal XsltGlobalContextItemPlan GlobalContextItem;
    internal XsltSchemaImportPlan[] SchemaImports;
    internal string[] ImportedSchemaNamespaces;
    internal XmlSchemaSet? SchemaSet;
    internal bool DeclaredModes;
    internal XsltPackagePlan? Package;
    internal XsltStreamabilityAnalysis? StreamabilityAnalysis;
    internal bool IsImmutable;

    internal XsltStylesheetPlan(XsltStylesheetModule principalModule, XsltStylesheetModule[] modules)
    {
        PrincipalModule = principalModule;
        Modules = modules;
        RootLevel = new XsltStylesheetLevel(null, principalModule);
        Levels = [];
        Declarations = [];
        Fallbacks = [];
        StaticContexts = [];
        NamespaceAliases = [];
        InitialMode = "#unnamed";
        StaticVariables = [];
        Variables = [];
        ValueTemplates = [];
        Templates = [];
        TemplateRuleIndexes = [];
        Functions = [];
        Modes = [];
        AttributeSets = [];
        AttributeSetUses = [];
        Keys = [];
        Accumulators = [];
        DecimalFormats = [];
        CharacterMaps = [];
        OutputFormats = [];
        WhitespaceRules = [];
        Instructions = [];
        Components = [];
        XPathPlans = [];
        Globals = [];
        PatternIndexes = [];
        Outputs = [];
        Packages = [];
        Conformance = new XsltExecutableConformance();
        SourceMap = [];
        Metadata = null;
        GlobalContextItem = new XsltGlobalContextItemPlan();
        SchemaImports = [];
        ImportedSchemaNamespaces = [];
        SchemaSet = null;
        DeclaredModes = false;
        Package = null;
        StreamabilityAnalysis = null;
        IsImmutable = false;
    }
}
