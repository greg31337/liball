namespace asn1;

internal enum CSharpFilePartitionKind
{
    Model = 0,
    Constants = 1,
    Descriptors = 2,
    Validator = 3,
    Codec = 4
}

internal class CSharpGenerationPlan
{
    internal CSharpNameMapping RootNamespace = new CSharpNameMapping();
    internal CSharpModulePlan[] Modules = [];
    internal CSharpTypePlan[] Types = [];
    internal CSharpObjectTablePlan[] ObjectTables = [];
    internal CSharpFilePartition[] Partitions = [];

    internal CSharpModulePlan? FindModulePlan(LoweredModuleIr module)
    {
        int index = 0;
        while (index < this.Modules.Length)
        {
            if (this.Modules[index].Module == module
                || (module.StableId.Length > 0
                    && this.Modules[index].Module.StableId == module.StableId))
            {
                return this.Modules[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal CSharpTypePlan? FindTypePlan(LoweredTypeIr type)
    {
        int index = 0;
        while (index < this.Types.Length)
        {
            if (this.Types[index].Type == type
                || (type.StableId.Length > 0
                    && this.Types[index].Type.StableId == type.StableId))
            {
                return this.Types[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool IsRecursiveReference(
        LoweredTypeIr owner,
        LoweredTypeIr reference)
    {
        System.Collections.Generic.List<LoweredTypeIr> visited =
            new System.Collections.Generic.List<LoweredTypeIr>();
        return this.TypeCanReach(reference, owner, visited);
    }

    internal bool TypeCanReach(
        LoweredTypeIr current,
        LoweredTypeIr target,
        System.Collections.Generic.List<LoweredTypeIr> visited)
    {
        if (this.TypesAreSame(current, target))
        {
            return true;
        }

        if (this.ContainsType(visited, current))
        {
            return false;
        }

        visited.Add(current);
        if (current.ElementType != null
            && this.TypeCanReach(current.ElementType, target, visited))
        {
            return true;
        }

        int fieldIndex = 0;
        while (fieldIndex < current.Fields.Length)
        {
            LoweredTypeIr? fieldType = current.Fields[fieldIndex].Type;
            if (fieldType != null
                && this.TypeCanReach(fieldType, target, visited))
            {
                return true;
            }

            fieldIndex = fieldIndex + 1;
        }

        return false;
    }

    internal bool ContainsType(
        System.Collections.Generic.List<LoweredTypeIr> values,
        LoweredTypeIr value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (this.TypesAreSame(values[index], value))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool TypesAreSame(LoweredTypeIr left, LoweredTypeIr right)
    {
        return left == right
            || (left.StableId.Length > 0 && left.StableId == right.StableId);
    }
}

internal class CSharpModulePlan
{
    internal LoweredModuleIr Module = new LoweredModuleIr();
    internal CSharpNameMapping NamespaceName = new CSharpNameMapping();
    internal CSharpNameScope TypeScope = new CSharpNameScope(false);
    internal CSharpNameScope FileScope = new CSharpNameScope(true);
    internal CSharpImportPlan[] Imports = [];
    internal string[] ImportedNamespaces = [];
    internal CSharpNameMapping[] ConstantTypeNames = [];
    internal CSharpNameMapping SchemaName = new CSharpNameMapping();
    internal CSharpNameMapping ValidatorName = new CSharpNameMapping();
    internal CSharpNameMapping CodecName = new CSharpNameMapping();
    internal CSharpFilePartition[] Partitions = [];
}

internal class CSharpImportPlan
{
    internal LoweredImportIr Import = new LoweredImportIr();
    internal CSharpModulePlan TargetModule = new CSharpModulePlan();
    internal string NamespaceName = "";
}

internal class CSharpTypePlan
{
    internal LoweredTypeIr Type = new LoweredTypeIr();
    internal LoweredAssignmentIr Assignment = new LoweredAssignmentIr();
    internal CSharpModulePlan Module = new CSharpModulePlan();
    internal CSharpNameMapping TypeName = new CSharpNameMapping();
    internal CSharpNameMapping? ChoiceName;
    internal CSharpFilePartition? Partition;
}

internal class CSharpObjectTablePlan
{
    internal LoweredObjectTableIr Table = new LoweredObjectTableIr();
    internal CSharpObjectTableEntryPlan[] Entries = [];
}

internal class CSharpObjectTableEntryPlan
{
    internal LoweredObjectTableEntryIr Entry =
        new LoweredObjectTableEntryIr();
    internal CSharpTypePlan? Type;
}

internal class CSharpFilePartition
{
    internal string StableId = "";
    internal string RelativePath = "";
    internal CSharpFilePartitionKind Kind;
    internal CSharpModulePlan Module = new CSharpModulePlan();
    internal CSharpTypePlan? Type;
    internal CSharpNameMapping FileName = new CSharpNameMapping();
}
