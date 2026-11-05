using System.Globalization;
using molecule;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var parser = new SmilesParser();
var formulas = new FormulaCalculator();
var masses = new MassCalculator();
var hydrogens = new HydrogenOperations();
Console.WriteLine($"{"Molecule",-16} {"SMILES",-12} {"Formula",-10} {"Mass (g/mol)",12}");

// Each sample pairs a familiar compound name with its SMILES graph.
var samples = new[]
{
    (Name: "Water", Smiles: "O"),
    (Name: "Ethanol", Smiles: "CCO"),
    (Name: "Carbon dioxide", Smiles: "O=C=O"),
    (Name: "Acetone", Smiles: "CC(=O)C")
};

foreach (var sample in samples)
{
    // SMILES parsing builds the graph; assign implicit hydrogens before computing descriptors.
    var prepared = hydrogens.CalculateImplicitHydrogens(parser.Parse(sample.Smiles));
    if (!prepared.Success)
    {
        throw new InvalidOperationException("Hydrogen calculation failed.");
    }

    var molecule = prepared.Molecule;
    Console.WriteLine(
        $"{sample.Name,-16} {sample.Smiles,-12} {formulas.Formula(molecule),-10} " +
        $"{masses.MolecularWeight(molecule),12:F3}");
}
