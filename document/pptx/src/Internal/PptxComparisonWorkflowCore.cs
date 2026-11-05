namespace pptx;

internal static class PptxComparisonWorkflowCore
{
    internal static PptxPresentationComparison ComparePackages(PptxPackage left, PptxPackage right)
    {
        PptxPresentationComparison comparison = new PptxPresentationComparison();
        comparison.Manifest = PptxPackageInspectionCore.CompareManifests(left, right);
        comparison.EditableModel = PptxPackageInspectionCore.CompareEditableModels(left, right);
        comparison.Equal = comparison.Manifest.Equal && comparison.EditableModel.Equal;
        return comparison;
    }
}
