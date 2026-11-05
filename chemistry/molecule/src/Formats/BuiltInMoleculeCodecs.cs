namespace molecule;

internal class V3000MoleculeWriter : IMoleculeWriter
{
    internal MolfileWriter Writer;

    internal V3000MoleculeWriter(MolfileWriter writer)
    {
        Writer = writer;
    }

    public string Write(Molecule? molecule)
    {
        return Writer.WriteV3000(molecule);
    }
}

internal class InChIMoleculeReader : IMoleculeReader
{
    internal InChIWorkflows Workflows;

    internal InChIMoleculeReader(InChIWorkflows workflows)
    {
        Workflows = workflows;
    }

    public Molecule Read(string? text)
    {
        Molecule molecule = new Molecule();
        Workflows.AttachInChI(molecule, text);
        return molecule;
    }
}

internal class InChIMoleculeWriter : IMoleculeWriter
{
    internal InChIWorkflows Workflows;

    internal InChIMoleculeWriter(InChIWorkflows workflows)
    {
        Workflows = workflows;
    }

    public string Write(Molecule? molecule)
    {
        return Workflows.ExportInChI(molecule);
    }
}

internal class InChIKeyMoleculeReader : IMoleculeReader
{
    internal InChIWorkflows Workflows;

    internal InChIKeyMoleculeReader(InChIWorkflows workflows)
    {
        Workflows = workflows;
    }

    public Molecule Read(string? text)
    {
        Molecule molecule = new Molecule();
        Workflows.AttachInChIKey(molecule, text);
        return molecule;
    }
}

internal class InChIKeyMoleculeWriter : IMoleculeWriter
{
    internal InChIWorkflows Workflows;

    internal InChIKeyMoleculeWriter(InChIWorkflows workflows)
    {
        Workflows = workflows;
    }

    public string Write(Molecule? molecule)
    {
        return Workflows.ExportInChIKey(molecule);
    }
}
