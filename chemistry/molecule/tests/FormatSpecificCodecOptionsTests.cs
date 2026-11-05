using System;
using System.IO;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class FormatSpecificCodecOptionsTests
{
    [Test]
    public void SmilesParser_UsesStrictRecoveryAndSpanEntryPoints()
    {
        SmilesParser parser = new SmilesParser();
        SmilesReadOptions strictOptions = new SmilesReadOptions();
        SmilesReadOptions recoveryOptions = new SmilesReadOptions();
        recoveryOptions.EnableRecovery = true;

        SmilesParseResult strict = parser.ParseSmilesWithOptions("C)O", strictOptions);
        SmilesParseResult recovered = parser.ParseSmilesWithOptions("C)O", recoveryOptions);
        ReadOnlySpan<char> slice = "xxCOyy".AsSpan(2, 2);
        SmilesParseResult span = parser.ParseSmilesSpanWithOptions(slice, strictOptions);

        Assert.That(strict.Success, Is.False);
        Assert.That(recovered.Success, Is.True);
        Assert.That(recovered.Recovered, Is.True);
        Assert.That(recovered.Molecule.GetAtomCount(), Is.EqualTo(2));
        Assert.That(span.Success, Is.True);
        Assert.That(span.Molecule.GetAtomCount(), Is.EqualTo(2));
        Assert.That(parser.ParseSmilesSpan(slice).GetBondCount(), Is.EqualTo(1));
    }

    [Test]
    public void SmilesWriter_SeparatesCanonicalAndNoncanonicalOperations()
    {
        SmilesWriter writer = new SmilesWriter();
        Molecule forward = BuildChain(8, 6, 7);
        Molecule reversed = BuildChain(7, 6, 8);

        string canonicalForward = writer.WriteCanonicalSmiles(forward);
        string canonicalReversed = writer.WriteCanonicalSmiles(reversed);
        string noncanonicalForward = writer.WriteNoncanonicalSmiles(forward);
        string noncanonicalReversed = writer.WriteNoncanonicalSmiles(reversed);

        Assert.That(canonicalForward, Is.EqualTo(canonicalReversed));
        Assert.That(noncanonicalForward, Is.EqualTo("OCN"));
        Assert.That(noncanonicalReversed, Is.EqualTo("NCO"));
    }

    [Test]
    public void SmilesWriter_WritesAromaticAndKekuleRepresentationsWithoutMutation()
    {
        SmilesParser parser = new SmilesParser();
        SmilesWriter writer = new SmilesWriter();
        Molecule kekuleSource = parser.Parse("C1=CC=CC=C1");
        Molecule aromaticSource = parser.Parse("c:1:c:c:c:c:c:1");

        string aromatic = writer.WriteAromaticSmiles(kekuleSource);
        string kekule = writer.WriteKekuleSmiles(aromaticSource);

        Assert.That(aromatic, Does.Contain("c"));
        Assert.That(aromatic, Does.Contain(":"));
        Assert.That(kekule, Does.Contain("C"));
        Assert.That(kekule, Does.Contain("="));
        Assert.That(kekuleSource.Atoms[0].IsAromatic, Is.False);
        Assert.That(aromaticSource.Atoms[0].IsAromatic, Is.True);
    }

    [Test]
    public void SmilesWriter_IndependentlyControlsStereoMapsAndHydrogenAnnotations()
    {
        SmilesWriter writer = new SmilesWriter();
        Molecule molecule = BuildConfiguredMolecule();
        SmilesWriteOptions plainOptions = new SmilesWriteOptions();
        plainOptions.Canonical = true;
        plainOptions.IncludeAtomMaps = false;

        string plain = writer.WriteSmilesWithOptions(molecule, plainOptions);
        string isomeric = writer.WriteIsomericSmiles(molecule);
        string mapped = writer.WriteAtomMappedSmiles(molecule);
        string explicitHydrogen = writer.WriteExplicitHydrogenSmiles(molecule);

        Assert.That(plain, Does.Not.Contain("/"));
        Assert.That(plain, Does.Not.Contain(":7"));
        Assert.That(isomeric, Does.Contain("/"));
        Assert.That(mapped, Does.Contain(":7"));
        Assert.That(explicitHydrogen, Does.Contain("H3"));
        Assert.That(molecule.Atoms[0].AtomMapNumber, Is.EqualTo(7));
    }

    [Test]
    public void MoleculeFormats_AppliesSmilesReadAndWriteOptions()
    {
        MoleculeFormats formats = new MoleculeFormats();
        FormatOptions readOptions = new FormatOptions();
        readOptions.KindHint = FormatKind.Smiles;
        readOptions.SmilesRead.EnableRecovery = true;
        FormatOptions writeOptions = new FormatOptions();
        writeOptions.KindHint = FormatKind.Smiles;
        writeOptions.SmilesWrite.Canonical = true;

        Molecule recovered = formats.ReadString("C)O", readOptions);
        string forward = formats.WriteString(BuildChain(8, 6, 7), writeOptions);
        string reversed = formats.WriteString(BuildChain(7, 6, 8), writeOptions);

        Assert.That(recovered.GetAtomCount(), Is.EqualTo(2));
        Assert.That(forward, Is.EqualTo(reversed));
    }

    [Test]
    public void MoleculeStreamReader_AppliesErrorContinuationAndFailedRecordPolicies()
    {
        string records = "bad\n$$$$\n" + new SdfWriter().Write(BuildSingleCarbon("Good"));
        FormatOptions stopOptions = new FormatOptions();
        stopOptions.KindHint = FormatKind.Sdf;
        stopOptions.RecordStream.ContinueAfterError = false;
        MoleculeStreamReader stoppingReader = MoleculeStreamReader.Create(new StringReader(records), stopOptions);

        MoleculeStreamReadResult failed = stoppingReader.ReadNext();
        MoleculeStreamReadResult stopped = stoppingReader.ReadNext();

        FormatOptions skipOptions = new FormatOptions();
        skipOptions.KindHint = FormatKind.Sdf;
        skipOptions.RecordStream.IncludeFailedRecords = false;
        MoleculeStreamReader skippingReader = MoleculeStreamReader.Create(new StringReader(records), skipOptions);

        Assert.That(failed.Success, Is.False);
        Assert.That(stopped.EndOfStream, Is.True);
        Assert.That(skippingReader.ReadAll(), Has.Count.EqualTo(1));
    }

    [Test]
    public void MoleculeStreamWriter_FlushesAfterEachRecordWhenConfigured()
    {
        FormatOptions options = new FormatOptions();
        options.KindHint = FormatKind.Smiles;
        options.RecordStream.FlushAfterRecord = true;
        TrackingStringWriter output = new TrackingStringWriter();
        MoleculeStreamWriter writer = MoleculeStreamWriter.Create(output, options);

        writer.Write(BuildSingleCarbon("Carbon"));
        writer.Write(BuildSingleCarbon("Carbon"));

        Assert.That(output.FlushCount, Is.EqualTo(2));
        Assert.That(output.ToString(), Is.EqualTo("C\nC\n"));
    }

    static Molecule BuildChain(int firstAtomicNumber, int secondAtomicNumber, int thirdAtomicNumber)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int first = builder.AddAtom(new AtomSpec(firstAtomicNumber, 0, 0, 0, 1, 0, 0, false, "", ""));
        int second = builder.AddAtom(new AtomSpec(secondAtomicNumber, 0, 0, 0, 2, 0, 0, false, "", ""));
        int third = builder.AddAtom(new AtomSpec(thirdAtomicNumber, 0, 0, 0, 2, 0, 0, false, "", ""));
        builder.AddBond(first, second, new BondSpec());
        builder.AddBond(second, third, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildConfiguredMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 7, false, "", ""));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", ""));
        builder.AddBond(carbon, nitrogen, BondSpec.Create(BondOrder.Single, false, BondStereo.Wedge, BondDirection.Up));
        return builder.Build();
    }

    static Molecule BuildSingleCarbon(string name)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", ""));
        Molecule molecule = builder.Build();
        molecule.Metadata.AddName(name);
        return molecule;
    }

    class TrackingStringWriter : StringWriter
    {
        public int FlushCount;

        public override void Flush()
        {
            FlushCount = FlushCount + 1;
            base.Flush();
        }
    }
}
