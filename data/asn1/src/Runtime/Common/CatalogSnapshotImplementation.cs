namespace asn1;

internal class CatalogSnapshotImplementation
{
    internal AsnReflectionCatalog CreateSnapshot(
        AsnReflectionCatalog source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        return new AsnReflectionCatalog
        {
            StableId = source.StableId,
            Modules = source.Modules == null
                ? []
                : (AsnModuleDescriptor[])source.Modules.Clone(),
            Assignments = source.Assignments == null
                ? []
                : (AsnAssignmentDescriptor[])source.Assignments.Clone(),
            ObjectSetRegistry = source.ObjectSetRegistry == null
                ? new AsnObjectSetRegistry()
                : source.ObjectSetRegistry,
            EcnCodecPlans = this.CloneEcnSelections(
                source.EcnCodecPlans)
        };
    }

    internal EcnCodecPlanSelection[] CloneEcnSelections(
        EcnCodecPlanSelection[] source)
    {
        if (source == null)
        {
            return [];
        }

        EcnCodecPlanSelection[] result =
            new EcnCodecPlanSelection[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            EcnCodecPlanSelection selection = source[index];
            result[index] = selection == null
                ? new EcnCodecPlanSelection()
                : new EcnCodecPlanSelection
                {
                    Name = selection.Name,
                    Plan = selection.Plan,
                    Category = selection.Category,
                    Properties = selection.Properties == null
                        ? []
                        : (EcnEncodingProperty[])selection.Properties.Clone(),
                    Type = selection.Type
                };
            index = index + 1;
        }
        return result;
    }
}
