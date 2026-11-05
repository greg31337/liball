namespace ttf;

internal static class PdfToUnicodeMappingBuilder
{
    internal static PdfToUnicodeMappingArtifact Build(
        PdfFontUsage usage,
        PdfCidAllocator allocator)
    {
        if (usage == null)
        {
            throw new System.ArgumentNullException("usage");
        }

        if (allocator == null)
        {
            throw new System.ArgumentNullException("allocator");
        }

        if (allocator.GetSourceGlyphCount() !=
            usage.GetInstance().GetGlyphCount())
        {
            throw new System.ArgumentException(
                "The CID allocator does not belong to the usage font.",
                "allocator");
        }

        int codeByteCount = usage.CopyOptions().GetFontKind() ==
            PdfFontKind.SimpleTrueType ? 1 : 2;
        int maximumCode = codeByteCount == 1 ?
            byte.MaxValue : ushort.MaxValue;
        allocator.GetOrAddCids(
            usage.CopyGlyphIdsInUseOrder(),
            maximumCode);
        int codeCount = allocator.GetCount();
        System.Collections.Generic.List<int[]>[] sequencesByCode =
            new System.Collections.Generic.List<int[]>[codeCount];
        PdfFontUsageMapping[] usageMappings = usage.CopyMappings();
        int index = 0;

        while (index < usageMappings.Length)
        {
            PdfFontUsageMapping usageMapping = usageMappings[index];
            int allocatedCode = allocator.GetOrAddCid(
                usageMapping.GetGlyphId());
            System.Collections.Generic.List<int[]> sequences =
                sequencesByCode[allocatedCode];

            if (sequences == null)
            {
                sequences = new System.Collections.Generic.List<int[]>();
                sequencesByCode[allocatedCode] = sequences;
            }

            sequences.Add(usageMapping.CopyUnicodeSequence());
            index += 1;
        }

        int mappingCount = 0;
        int ambiguousMappingCount = 0;
        System.Collections.Generic.Dictionary<string, int>
            sequenceUseCounts =
                new System.Collections.Generic.Dictionary<string, int>(
                    System.StringComparer.Ordinal);
        int code = 0;

        while (code < sequencesByCode.Length)
        {
            System.Collections.Generic.List<int[]> sequences =
                sequencesByCode[code];

            if (sequences != null)
            {
                mappingCount += 1;

                if (sequences.Count > 1)
                {
                    ambiguousMappingCount += 1;
                }

                int sequenceIndex = 0;

                while (sequenceIndex < sequences.Count)
                {
                    string sequenceKey = CreateSequenceKey(
                        sequences[sequenceIndex]);

                    if (sequenceUseCounts.ContainsKey(sequenceKey))
                    {
                        sequenceUseCounts[sequenceKey] =
                            sequenceUseCounts[sequenceKey] + 1;
                    }
                    else
                    {
                        sequenceUseCounts.Add(sequenceKey, 1);
                    }

                    sequenceIndex += 1;
                }
            }

            code += 1;
        }

        int sharedSequenceCount = 0;

        foreach (int sequenceUseCount in sequenceUseCounts.Values)
        {
            if (sequenceUseCount > 1)
            {
                sharedSequenceCount += 1;
            }
        }

        PdfToUnicodeCodeMapping[] mappings =
            new PdfToUnicodeCodeMapping[mappingCount];
        System.Collections.Generic.List<PdfToUnicodeMapping> unambiguous =
            new System.Collections.Generic.List<PdfToUnicodeMapping>();
        System.Collections.Generic.List<PdfToUnicodeMapping> resolved =
            new System.Collections.Generic.List<PdfToUnicodeMapping>();
        System.Collections.Generic.List<PdfActualTextRecommendation>
            recommendations =
                new System.Collections.Generic.List<
                    PdfActualTextRecommendation>();
        System.Collections.Generic.Dictionary<string, int> sequenceOwners =
            new System.Collections.Generic.Dictionary<string, int>(
                System.StringComparer.Ordinal);
        int[] sourceGlyphIds =
            allocator.CopySourceGlyphIdsInCidOrder();
        int mappingIndex = 0;
        code = 0;

        while (code < sequencesByCode.Length)
        {
            System.Collections.Generic.List<int[]> sequences =
                sequencesByCode[code];

            if (sequences != null)
            {
                int[][] candidates = sequences.ToArray();
                string preferredKey = CreateSequenceKey(candidates[0]);
                int cmapSequenceIndex = -1;

                if (!sequenceOwners.ContainsKey(preferredKey))
                {
                    sequenceOwners.Add(preferredKey, code);
                    cmapSequenceIndex = 0;
                    resolved.Add(new PdfToUnicodeMapping(
                        code,
                        candidates[0]));
                }

                mappings[mappingIndex] = new PdfToUnicodeCodeMapping(
                    code,
                    sourceGlyphIds[code],
                    candidates,
                    cmapSequenceIndex);
                mappingIndex += 1;

                if (candidates.Length == 1)
                {
                    unambiguous.Add(new PdfToUnicodeMapping(
                        code,
                        candidates[0]));
                }

                int candidateIndex = 0;

                while (candidateIndex < candidates.Length)
                {
                    if (candidateIndex != cmapSequenceIndex)
                    {
                        string candidateKey = CreateSequenceKey(
                            candidates[candidateIndex]);
                        bool multipleSequences = candidates.Length > 1;
                        bool sharedSequence =
                            sequenceUseCounts[candidateKey] > 1;
                        recommendations.Add(
                            new PdfActualTextRecommendation(
                                code,
                                sourceGlyphIds[code],
                                candidates[candidateIndex],
                                GetRecommendationReason(
                                    multipleSequences,
                                    sharedSequence)));
                    }

                    candidateIndex += 1;
                }
            }

            code += 1;
        }

        PdfToUnicodeCMap unambiguousCMap = PdfToUnicodeCMapWriter.Build(
            codeByteCount,
            unambiguous.ToArray());
        PdfToUnicodeCMap cmap = PdfToUnicodeCMapWriter.Build(
            codeByteCount,
            resolved.ToArray());
        return new PdfToUnicodeMappingArtifact(
            codeByteCount,
            mappings,
            ambiguousMappingCount,
            sharedSequenceCount,
            unambiguousCMap,
            cmap,
            recommendations.ToArray());
    }

    static PdfActualTextReason GetRecommendationReason(
        bool multipleSequences,
        bool sharedSequence)
    {
        if (multipleSequences && sharedSequence)
        {
            return PdfActualTextReason
                .MultipleSequencesForCodeAndSequenceSharedByCodes;
        }

        if (multipleSequences)
        {
            return PdfActualTextReason.MultipleSequencesForCode;
        }

        return PdfActualTextReason.SequenceSharedByCodes;
    }

    static string CreateSequenceKey(int[] sequence)
    {
        System.Text.StringBuilder builder =
            new System.Text.StringBuilder();
        int index = 0;

        while (index < sequence.Length)
        {
            builder.Append(sequence[index].ToString(
                "X8",
                System.Globalization.CultureInfo.InvariantCulture));
            builder.Append(';');
            index += 1;
        }

        return builder.ToString();
    }
}
