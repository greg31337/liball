using System.Collections.Generic;

namespace jpeg;

internal class JpegEncoderTableConfigurator
{
    internal void ValidateOptions(JpegWriteOptions options)
    {
        if (options.Tables == null)
        {
            throw new System.ArgumentException("Encoder table set cannot be null.", "options");
        }

        if (options.Tables.QuantizationTables == null
            || options.Tables.HuffmanTables == null
            || options.Tables.ArithmeticConditioningTables == null)
        {
            throw new System.ArgumentException("Encoder table collections cannot be null.", "options");
        }

        if (options.ScanTableAssignments == null)
        {
            throw new System.ArgumentException("Encoder scan table assignments cannot be null.", "options");
        }

        ValidateUniqueQuantizationTables(options.Tables.QuantizationTables);
        ValidateUniqueHuffmanTables(options.Tables.HuffmanTables);
        ValidateUniqueArithmeticTables(options.Tables.ArithmeticConditioningTables);
    }

    internal void ValidateTablesForProcess(JpegProcess process, JpegWriteOptions options)
    {
        bool losslessHuffman = process == JpegProcess.LosslessSequential;
        bool losslessArithmetic = process == JpegProcess.LosslessSequentialArithmetic;
        if ((losslessHuffman || losslessArithmetic)
            && (options.PreserveQuantizationTables || options.Tables.QuantizationTables.Count > 0))
        {
            throw JpegWriteValidationException.Create("Lossless JPEG encoding does not use quantization tables.");
        }

        if (losslessArithmetic && options.Tables.HuffmanTables.Count > 0)
        {
            throw JpegWriteValidationException.Create("Arithmetic JPEG encoding cannot use Huffman tables.");
        }

        if (!losslessArithmetic && options.Tables.ArithmeticConditioningTables.Count > 0)
        {
            throw JpegWriteValidationException.Create("Huffman JPEG encoding cannot use arithmetic-conditioning tables.");
        }
    }

    internal int ResolveComponentQuantizationTableIdentifier(
        JpegEncoderComponent component,
        int componentIndex,
        int componentCount,
        JpegImage image,
        JpegWriteOptions options,
        bool lossless)
    {
        if (lossless)
        {
            return 0;
        }

        if (component.QuantizationTableIdentifier >= 0)
        {
            return component.QuantizationTableIdentifier;
        }

        if (options.Tables.QuantizationTables.Count > 0)
        {
            int tableIndex = 0;
            if (componentIndex > 0 && options.Tables.QuantizationTables.Count > 1)
            {
                tableIndex = 1;
            }

            return options.Tables.QuantizationTables[tableIndex].Identifier;
        }

        if (options.PreserveQuantizationTables)
        {
            ValidatePreservedQuantizationState(image);
            return image.ComponentQuantizationTableIdentifiers[component.SourceComponentIndex];
        }

        if (componentCount == 3 && componentIndex > 0)
        {
            return 1;
        }

        return 0;
    }

    internal List<JpegQuantizationTable> ResolveQuantizationTables(
        JpegImage image,
        List<JpegQuantizationTable> defaults,
        JpegWriteOptions options)
    {
        if (options.Tables.QuantizationTables.Count > 0)
        {
            return CopyQuantizationTables(options.Tables.QuantizationTables);
        }

        if (options.PreserveQuantizationTables)
        {
            ValidatePreservedQuantizationState(image);
            return CopyQuantizationTables(image.QuantizationTables);
        }

        return defaults;
    }

    internal List<JpegHuffmanTable> ResolveHuffmanTables(List<JpegHuffmanTable> defaults, JpegWriteOptions options)
    {
        if (options.Tables.HuffmanTables.Count == 0)
        {
            return defaults;
        }

        byte[] payload = new JpegHuffmanTableWriter().WritePayloads(options.Tables.HuffmanTables);
        return new JpegHuffmanTableParser().Parse(payload);
    }

    internal List<JpegArithmeticConditioningTable> ResolveArithmeticTables(JpegWriteOptions options)
    {
        if (options.Tables.ArithmeticConditioningTables.Count == 0)
        {
            return new List<JpegArithmeticConditioningTable>();
        }

        byte[] payload = new JpegArithmeticConditioningTableWriter().WritePayloads(options.Tables.ArithmeticConditioningTables);
        return new JpegArithmeticConditioningTableParser().Parse(payload);
    }

    internal void ApplyScanTableAssignments(List<JpegScanHeader> scans, JpegWriteOptions options)
    {
        int assignmentIndex = 0;
        while (assignmentIndex < options.ScanTableAssignments.Count)
        {
            JpegEncoderScanTableAssignment assignment = options.ScanTableAssignments[assignmentIndex];
            ValidateScanTableAssignment(assignment, scans, options.ScanTableAssignments, assignmentIndex);
            JpegScanComponent component = FindScanComponent(scans[assignment.ScanIndex], assignment.ComponentIdentifier);
            if (assignment.DcEntropyTableIdentifier >= 0)
            {
                component.DcEntropyTableIdentifier = assignment.DcEntropyTableIdentifier;
            }

            if (assignment.AcEntropyTableIdentifier >= 0)
            {
                component.AcEntropyTableIdentifier = assignment.AcEntropyTableIdentifier;
            }

            assignmentIndex = assignmentIndex + 1;
        }
    }

    internal void ValidateTableReferences(
        JpegFrameHeader frame,
        List<JpegScanHeader> scans,
        JpegTableStore tables,
        bool lossless,
        bool arithmetic)
    {
        if (!lossless)
        {
            foreach (JpegFrameComponent component in frame.Components)
            {
                if (!HasQuantizationTable(tables.QuantizationTables, component.QuantizationTableIdentifier))
                {
                    throw JpegWriteValidationException.Create("A frame component references a missing quantization table.");
                }
            }
        }

        foreach (JpegScanHeader scan in scans)
        {
            foreach (JpegScanComponent component in scan.Components)
            {
                if (arithmetic)
                {
                    if ((lossless || scan.SpectralStart == 0)
                        && tables.ArithmeticConditioningTables.Count > 0
                        && !HasEntropyConditioningTable(tables.ArithmeticConditioningTables, 0, component.DcEntropyTableIdentifier))
                    {
                        throw JpegWriteValidationException.Create("A scan component references a missing arithmetic-conditioning table.");
                    }
                }
                else
                {
                    if ((lossless || scan.SpectralStart == 0)
                        && !HasHuffmanTable(tables.HuffmanTables, 0, component.DcEntropyTableIdentifier))
                    {
                        throw JpegWriteValidationException.Create("A scan component references a missing DC Huffman table.");
                    }

                    if (!lossless && scan.SpectralEnd > 0
                        && !HasHuffmanTable(tables.HuffmanTables, 1, component.AcEntropyTableIdentifier))
                    {
                        throw JpegWriteValidationException.Create("A scan component references a missing AC Huffman table.");
                    }
                }
            }
        }
    }

    internal List<JpegQuantizationTable> CopyQuantizationTables(List<JpegQuantizationTable> source)
    {
        byte[] payload = new JpegQuantizationTableWriter().WritePayloads(source);
        return new JpegQuantizationTableParser().Parse(payload);
    }

    internal void ValidatePreservedQuantizationState(JpegImage image)
    {
        if (image.QuantizationTables == null || image.QuantizationTables.Count == 0)
        {
            throw JpegWriteValidationException.Create("The source image does not contain retained quantization tables.");
        }

        if (image.ComponentQuantizationTableIdentifiers == null
            || image.ComponentQuantizationTableIdentifiers.Count != image.ComponentCount)
        {
            throw JpegWriteValidationException.Create("The source image does not contain retained component quantization assignments.");
        }
    }

    internal void ValidateScanTableAssignment(
        JpegEncoderScanTableAssignment assignment,
        List<JpegScanHeader> scans,
        List<JpegEncoderScanTableAssignment> assignments,
        int assignmentIndex)
    {
        if (assignment == null)
        {
            throw JpegWriteValidationException.Create("Encoder scan table assignment cannot be null.");
        }

        if (assignment.ScanIndex < 0 || assignment.ScanIndex >= scans.Count)
        {
            throw JpegWriteValidationException.Create("Encoder scan table assignment references a missing scan.");
        }

        if (assignment.DcEntropyTableIdentifier < -1 || assignment.DcEntropyTableIdentifier > 3
            || assignment.AcEntropyTableIdentifier < -1 || assignment.AcEntropyTableIdentifier > 3)
        {
            throw JpegWriteValidationException.Create("Entropy-table identifiers must be automatic or between zero and three.");
        }

        FindScanComponent(scans[assignment.ScanIndex], assignment.ComponentIdentifier);
        int index = 0;
        while (index < assignmentIndex)
        {
            JpegEncoderScanTableAssignment previous = assignments[index];
            if (previous.ScanIndex == assignment.ScanIndex && previous.ComponentIdentifier == assignment.ComponentIdentifier)
            {
                throw JpegWriteValidationException.Create("A scan component cannot have duplicate table assignments.");
            }

            index = index + 1;
        }
    }

    internal JpegScanComponent FindScanComponent(JpegScanHeader scan, int identifier)
    {
        foreach (JpegScanComponent component in scan.Components)
        {
            if (component.Selector == identifier)
            {
                return component;
            }
        }

        throw JpegWriteValidationException.Create("Encoder scan table assignment references a component that is not in the scan.");
    }

    internal void ValidateUniqueQuantizationTables(List<JpegQuantizationTable> tables)
    {
        int index = 0;
        while (index < tables.Count)
        {
            if (tables[index] == null)
            {
                throw JpegWriteValidationException.Create("Encoder quantization table cannot be null.");
            }

            int previous = 0;
            while (previous < index)
            {
                if (tables[previous].Identifier == tables[index].Identifier)
                {
                    throw JpegWriteValidationException.Create("Encoder quantization-table identifiers must be unique.");
                }

                previous = previous + 1;
            }

            index = index + 1;
        }
    }

    internal void ValidateUniqueHuffmanTables(List<JpegHuffmanTable> tables)
    {
        int index = 0;
        while (index < tables.Count)
        {
            if (tables[index] == null)
            {
                throw JpegWriteValidationException.Create("Encoder Huffman table cannot be null.");
            }

            int previous = 0;
            while (previous < index)
            {
                if (tables[previous].TableClass == tables[index].TableClass
                    && tables[previous].Identifier == tables[index].Identifier)
                {
                    throw JpegWriteValidationException.Create("Encoder Huffman table class and identifier pairs must be unique.");
                }

                previous = previous + 1;
            }

            index = index + 1;
        }
    }

    internal void ValidateUniqueArithmeticTables(List<JpegArithmeticConditioningTable> tables)
    {
        int index = 0;
        while (index < tables.Count)
        {
            if (tables[index] == null)
            {
                throw JpegWriteValidationException.Create("Encoder arithmetic-conditioning table cannot be null.");
            }

            int previous = 0;
            while (previous < index)
            {
                if (tables[previous].TableClass == tables[index].TableClass
                    && tables[previous].Identifier == tables[index].Identifier)
                {
                    throw JpegWriteValidationException.Create("Encoder arithmetic table class and identifier pairs must be unique.");
                }

                previous = previous + 1;
            }

            index = index + 1;
        }
    }

    internal bool HasQuantizationTable(List<JpegQuantizationTable> tables, int identifier)
    {
        foreach (JpegQuantizationTable table in tables)
        {
            if (table.Identifier == identifier)
            {
                return true;
            }
        }

        return false;
    }

    internal bool HasHuffmanTable(List<JpegHuffmanTable> tables, int tableClass, int identifier)
    {
        foreach (JpegHuffmanTable table in tables)
        {
            if (table.TableClass == tableClass && table.Identifier == identifier)
            {
                return true;
            }
        }

        return false;
    }

    internal bool HasEntropyConditioningTable(List<JpegArithmeticConditioningTable> tables, int tableClass, int identifier)
    {
        foreach (JpegArithmeticConditioningTable table in tables)
        {
            if (table.TableClass == tableClass && table.Identifier == identifier)
            {
                return true;
            }
        }

        return false;
    }
}
