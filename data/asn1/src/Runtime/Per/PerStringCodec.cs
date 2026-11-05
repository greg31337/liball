namespace asn1;

internal class PerStringCodec
{
    internal AsnResourceLimits Limits;
    internal PerLengthCodec Lengths;
    internal PerPrimitiveCodec Primitives;

    internal PerStringCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
        this.Lengths = new PerLengthCodec(limits);
        this.Primitives = new PerPrimitiveCodec(limits);
    }

    internal PerStringResult WriteKnownMultiplierString(
        PerBitWriter writer,
        AsnTypeKind kind,
        string text,
        int lowerBound,
        bool hasUpperBound,
        int upperBound,
        PermittedAlphabetResult? permittedAlphabet,
        bool extensible,
        bool aligned)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        if (!this.IsKnownMultiplierType(kind))
        {
            return this.CreateError(
                "ASNP0045",
                "The requested type is not a PER known-multiplier string.",
                false);
        }

        PerStringResult bounds = this.ValidateSizeBounds(
            lowerBound,
            hasUpperBound,
            upperBound);
        if (!bounds.Success)
        {
            return bounds;
        }

        if (!new ResourceCounter(this.Limits).AllowsTextCharacters(text.Length))
        {
            return this.CreateError(
                "ASNR0009",
                "The character string exceeds the configured text limit.",
                true);
        }

        PermittedAlphabetResult alphabet = permittedAlphabet == null
            ? this.CreateDefaultAlphabet(kind)
            : permittedAlphabet;
        PerStringResult alphabetValidation = this.ValidateAlphabet(alphabet);
        if (!alphabetValidation.Success)
        {
            return alphabetValidation;
        }

        PerStringResult scalars = this.ReadScalars(text, alphabet);
        if (!scalars.Success)
        {
            return scalars;
        }

        int[] scalarValues = this.DecodeScalarArray(scalars.Data);
        int count = scalarValues.Length;
        bool extensionValue = count < lowerBound
            || hasUpperBound && count > upperBound;
        if (extensionValue && !extensible)
        {
            return this.CreateError(
                "ASNP0046",
                "The character count is outside the permitted size range.",
                false);
        }

        int start = writer.GetWrittenBitCount();
        if (extensible)
        {
            PerBitWriteResult extension = writer.WriteBit(extensionValue);
            if (!extension.Success)
            {
                return this.CopyWriterFailure(writer);
            }
        }

        int characterBits = this.GetCharacterBitCount(
            alphabet.RootAlphabetSize,
            aligned);
        bool compacted = this.UsesCompactedAlphabet(
            alphabet,
            characterBits);
        PerStringResult written = this.WriteCharacterField(
            writer,
            scalarValues,
            lowerBound,
            hasUpperBound,
            upperBound,
            extensionValue,
            alphabet,
            characterBits,
            compacted,
            aligned);
        if (!written.Success)
        {
            return written;
        }

        written.Text = text;
        written.CharacterCount = count;
        written.BitsPerCharacter = characterBits;
        written.BitCount = writer.GetWrittenBitCount() - start;
        written.CompactedAlphabet = compacted;
        written.ExtensionValue = extensionValue;
        return written;
    }

    internal PerStringResult ReadKnownMultiplierString(
        PerBitReader reader,
        AsnTypeKind kind,
        int lowerBound,
        bool hasUpperBound,
        int upperBound,
        PermittedAlphabetResult? permittedAlphabet,
        bool extensible,
        bool aligned)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        if (!this.IsKnownMultiplierType(kind))
        {
            return this.CreateError(
                "ASNP0045",
                "The requested type is not a PER known-multiplier string.",
                false);
        }

        PerStringResult bounds = this.ValidateSizeBounds(
            lowerBound,
            hasUpperBound,
            upperBound);
        if (!bounds.Success)
        {
            return bounds;
        }

        PermittedAlphabetResult alphabet = permittedAlphabet == null
            ? this.CreateDefaultAlphabet(kind)
            : permittedAlphabet;
        PerStringResult alphabetValidation = this.ValidateAlphabet(alphabet);
        if (!alphabetValidation.Success)
        {
            return alphabetValidation;
        }

        int start = reader.Position;
        bool extensionValue = false;
        if (extensible)
        {
            PerBitReadResult extension = reader.ReadBit();
            if (extension.Status != AsnDecodeStatus.Done)
            {
                return this.HandleIncompleteRead(reader, extension, start);
            }

            extensionValue = extension.Value
                != System.Numerics.BigInteger.Zero;
        }

        int characterBits = this.GetCharacterBitCount(
            alphabet.RootAlphabetSize,
            aligned);
        bool compacted = this.UsesCompactedAlphabet(
            alphabet,
            characterBits);
        PerStringResult decoded = this.ReadCharacterField(
            reader,
            lowerBound,
            hasUpperBound,
            upperBound,
            extensionValue,
            alphabet,
            characterBits,
            compacted,
            aligned,
            start);
        if (!decoded.Success)
        {
            return decoded;
        }

        bool outsideRoot = decoded.CharacterCount < lowerBound
            || hasUpperBound && decoded.CharacterCount > upperBound;
        if ((!extensionValue && outsideRoot)
            || (extensionValue && !outsideRoot))
        {
            return this.CreateError(
                "ASNP0047",
                "The string extension bit does not match the decoded size.",
                false);
        }

        decoded.BitsPerCharacter = characterBits;
        decoded.BitCount = reader.Position - start;
        decoded.CompactedAlphabet = compacted;
        decoded.ExtensionValue = extensionValue;
        return decoded;
    }

    internal PerStringResult WriteBaseEncodedString(
        PerBitWriter writer,
        AsnTypeKind kind,
        string text,
        bool aligned)
    {
        AsnTypeKind contentKind = kind == AsnTypeKind.ObjectDescriptor
            ? AsnTypeKind.GraphicString
            : kind;
        BerPrimitiveValueResult encoded = new BerCharacterStringCodec(
            this.Limits).EncodeText(contentKind, text);
        if (!encoded.Success)
        {
            return this.CopyBerFailure(encoded);
        }

        int start = writer.GetWrittenBitCount();
        PerPrimitiveResult written = this.Primitives.WriteOctetString(
            writer,
            encoded.Content,
            0,
            false,
            0,
            aligned);
        if (!written.Success)
        {
            return this.CopyPrimitiveFailure(written);
        }

        return new PerStringResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Text = text,
            Data = encoded.Content,
            CharacterCount = this.CountTextScalars(text),
            BitsPerCharacter = 8,
            BitCount = writer.GetWrittenBitCount() - start
        };
    }

    internal PerStringResult ReadBaseEncodedString(
        PerBitReader reader,
        AsnTypeKind kind,
        bool aligned)
    {
        int start = reader.Position;
        PerPrimitiveResult content = this.Primitives.ReadOctetString(
            reader,
            0,
            false,
            0,
            aligned);
        if (!content.Success || content.Value == null)
        {
            return this.CopyPrimitiveFailure(content);
        }

        AsnTypeKind contentKind = kind == AsnTypeKind.ObjectDescriptor
            ? AsnTypeKind.GraphicString
            : kind;
        BerPrimitiveValueResult decoded = new BerCharacterStringCodec(
            this.Limits).DecodeText(contentKind, content.Value.Octets);
        if (!decoded.Success || decoded.Value == null)
        {
            return this.CopyBerFailure(decoded);
        }

        return new PerStringResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Text = decoded.Value.TextValue,
            Data = content.Value.Octets,
            CharacterCount = this.CountTextScalars(decoded.Value.TextValue),
            BitsPerCharacter = 8,
            BitCount = reader.Position - start
        };
    }

    internal PerStringResult WriteUnrestrictedCharacterString(
        PerBitWriter writer,
        byte[] completeEncoding,
        bool aligned)
    {
        int start = writer.GetWrittenBitCount();
        PerPrimitiveResult written = this.Primitives.WriteOctetString(
            writer,
            completeEncoding,
            0,
            false,
            0,
            aligned);
        if (!written.Success)
        {
            return this.CopyPrimitiveFailure(written);
        }

        return new PerStringResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Data = this.CopyBytes(completeEncoding),
            BitCount = writer.GetWrittenBitCount() - start
        };
    }

    internal PerStringResult ReadUnrestrictedCharacterString(
        PerBitReader reader,
        bool aligned)
    {
        int start = reader.Position;
        PerPrimitiveResult content = this.Primitives.ReadOctetString(
            reader,
            0,
            false,
            0,
            aligned);
        if (!content.Success || content.Value == null)
        {
            return this.CopyPrimitiveFailure(content);
        }

        return new PerStringResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Data = content.Value.Octets,
            BitCount = reader.Position - start
        };
    }

    internal PerStringResult WriteCharacterField(
        PerBitWriter writer,
        int[] scalars,
        int lowerBound,
        bool hasUpperBound,
        int upperBound,
        bool extensionValue,
        PermittedAlphabetResult alphabet,
        int characterBits,
        bool compacted,
        bool aligned)
    {
        int count = scalars.Length;
        if (!extensionValue && hasUpperBound
            && lowerBound == upperBound && upperBound < 65536)
        {
            long totalBits = (long)count * characterBits;
            if (aligned && totalBits > 16)
            {
                PerBitWriteResult padding = writer.AlignOutput();
                if (!padding.Success)
                {
                    return this.CopyWriterFailure(writer);
                }
            }

            return this.WriteScalarRange(
                writer,
                scalars,
                0,
                count,
                alphabet,
                characterBits,
                compacted);
        }

        if (!extensionValue && hasUpperBound && upperBound < 65536)
        {
            PerLengthResult length = this.Lengths.WriteConstrainedLength(
                writer,
                count,
                lowerBound,
                upperBound,
                aligned);
            if (!length.Success)
            {
                return this.CopyLengthFailure(length);
            }

            long maximumBits = (long)upperBound * characterBits;
            if (aligned && count > 0 && maximumBits >= 16)
            {
                PerBitWriteResult padding = writer.AlignOutput();
                if (!padding.Success)
                {
                    return this.CopyWriterFailure(writer);
                }
            }

            return this.WriteScalarRange(
                writer,
                scalars,
                0,
                count,
                alphabet,
                characterBits,
                compacted);
        }

        return this.WriteFragmentedCharacters(
            writer,
            scalars,
            alphabet,
            characterBits,
            compacted,
            aligned);
    }

    internal PerStringResult ReadCharacterField(
        PerBitReader reader,
        int lowerBound,
        bool hasUpperBound,
        int upperBound,
        bool extensionValue,
        PermittedAlphabetResult alphabet,
        int characterBits,
        bool compacted,
        bool aligned,
        int start)
    {
        if (!extensionValue && hasUpperBound
            && lowerBound == upperBound && upperBound < 65536)
        {
            long totalBits = (long)lowerBound * characterBits;
            if (aligned && totalBits > 16)
            {
                PerBitReadResult padding = reader.AlignInput(true);
                if (padding.Status != AsnDecodeStatus.Done)
                {
                    return this.HandleIncompleteRead(reader, padding, start);
                }
            }

            return this.ReadScalarRange(
                reader,
                lowerBound,
                alphabet,
                characterBits,
                compacted,
                start);
        }

        if (!extensionValue && hasUpperBound && upperBound < 65536)
        {
            PerLengthResult length = this.Lengths.ReadConstrainedLength(
                reader,
                lowerBound,
                upperBound,
                aligned);
            if (!length.Success)
            {
                return this.CopyLengthFailure(length);
            }

            long maximumBits = (long)upperBound * characterBits;
            if (aligned && length.Length > 0 && maximumBits >= 16)
            {
                PerBitReadResult padding = reader.AlignInput(true);
                if (padding.Status != AsnDecodeStatus.Done)
                {
                    return this.HandleIncompleteRead(reader, padding, start);
                }
            }

            return this.ReadScalarRange(
                reader,
                length.Length,
                alphabet,
                characterBits,
                compacted,
                start);
        }

        return this.ReadFragmentedCharacters(
            reader,
            alphabet,
            characterBits,
            compacted,
            aligned,
            start);
    }

    internal PerStringResult WriteFragmentedCharacters(
        PerBitWriter writer,
        int[] scalars,
        PermittedAlphabetResult alphabet,
        int characterBits,
        bool compacted,
        bool aligned)
    {
        int start = writer.GetWrittenBitCount();
        int offset = 0;
        int remaining = scalars.Length;
        while (remaining >= 16384)
        {
            int units = remaining / 16384;
            if (units > 4)
            {
                units = 4;
            }

            int count = units * 16384;
            PerLengthResult length = this.Lengths.WriteLengthComponent(
                writer,
                count,
                aligned);
            if (!length.Success)
            {
                return this.CopyLengthFailure(length);
            }

            if (aligned && count > 0)
            {
                PerBitWriteResult padding = writer.AlignOutput();
                if (!padding.Success)
                {
                    return this.CopyWriterFailure(writer);
                }
            }

            PerStringResult fragment = this.WriteScalarRange(
                writer,
                scalars,
                offset,
                count,
                alphabet,
                characterBits,
                compacted);
            if (!fragment.Success)
            {
                return fragment;
            }

            offset = offset + count;
            remaining = remaining - count;
        }

        PerLengthResult finalLength = this.Lengths.WriteLengthComponent(
            writer,
            remaining,
            aligned);
        if (!finalLength.Success)
        {
            return this.CopyLengthFailure(finalLength);
        }

        if (aligned && remaining > 0)
        {
            PerBitWriteResult padding = writer.AlignOutput();
            if (!padding.Success)
            {
                return this.CopyWriterFailure(writer);
            }
        }

        PerStringResult finalFragment = this.WriteScalarRange(
            writer,
            scalars,
            offset,
            remaining,
            alphabet,
            characterBits,
            compacted);
        if (!finalFragment.Success)
        {
            return finalFragment;
        }

        finalFragment.BitCount = writer.GetWrittenBitCount() - start;
        finalFragment.CharacterCount = scalars.Length;
        return finalFragment;
    }

    internal PerStringResult ReadFragmentedCharacters(
        PerBitReader reader,
        PermittedAlphabetResult alphabet,
        int characterBits,
        bool compacted,
        bool aligned,
        int start)
    {
        TextAccumulator text = new TextAccumulator(0);
        int total = 0;
        bool more = true;
        while (more)
        {
            PerLengthResult length = this.Lengths.ReadLengthComponent(
                reader,
                aligned);
            if (!length.Success)
            {
                return this.CopyLengthFailure(length);
            }

            if (length.Length > int.MaxValue - total
                || !new ResourceCounter(this.Limits).AllowsTextCharacters(
                    total + length.Length))
            {
                return this.CreateError(
                    "ASNR0009",
                    "The fragmented character string exceeds the configured text limit.",
                    true);
            }

            if (aligned && length.Length > 0)
            {
                PerBitReadResult padding = reader.AlignInput(true);
                if (padding.Status != AsnDecodeStatus.Done)
                {
                    return this.HandleIncompleteRead(reader, padding, start);
                }
            }

            PerStringResult fragment = this.ReadScalarRange(
                reader,
                length.Length,
                alphabet,
                characterBits,
                compacted,
                start);
            if (!fragment.Success)
            {
                return fragment;
            }

            text.AppendText(fragment.Text);
            total = total + length.Length;
            more = length.MoreFragments;
        }

        return new PerStringResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Text = text.ToText(),
            CharacterCount = total,
            BitCount = reader.Position - start
        };
    }

    internal PerStringResult WriteScalarRange(
        PerBitWriter writer,
        int[] scalars,
        int offset,
        int count,
        PermittedAlphabetResult alphabet,
        int characterBits,
        bool compacted)
    {
        int index = 0;
        while (index < count)
        {
            int scalar = scalars[offset + index];
            int encoded = compacted
                ? alphabet.GetRootIndex(scalar)
                : scalar;
            if (encoded < 0)
            {
                return this.CreateError(
                    "ASNP0048",
                    "A character is outside the effective permitted alphabet.",
                    false);
            }

            PerBitWriteResult written = writer.WriteBits(
                new System.Numerics.BigInteger(encoded),
                characterBits);
            if (!written.Success)
            {
                return this.CopyWriterFailure(writer);
            }

            index = index + 1;
        }

        return new PerStringResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            CharacterCount = count,
            BitsPerCharacter = characterBits,
            BitCount = count * characterBits,
            CompactedAlphabet = compacted
        };
    }

    internal PerStringResult ReadScalarRange(
        PerBitReader reader,
        int count,
        PermittedAlphabetResult alphabet,
        int characterBits,
        bool compacted,
        int start)
    {
        if (!new ResourceCounter(this.Limits).AllowsTextCharacters(count))
        {
            return this.CreateError(
                "ASNR0009",
                "The character string exceeds the configured text limit.",
                true);
        }

        TextAccumulator text = new TextAccumulator(count);
        int index = 0;
        while (index < count)
        {
            PerBitReadResult input = reader.ReadBits(characterBits);
            if (input.Status != AsnDecodeStatus.Done)
            {
                return this.HandleIncompleteRead(reader, input, start);
            }

            if (input.Value > new System.Numerics.BigInteger(int.MaxValue))
            {
                return this.CreateError(
                    "ASNP0049",
                    "The encoded character value exceeds the supported scalar range.",
                    false);
            }

            int encoded = (int)input.Value;
            int scalar = compacted
                ? alphabet.GetRootScalar(encoded)
                : encoded;
            if (scalar < 0
                || alphabet.GetRootIndex(scalar) < 0
                || !this.AppendScalar(text, scalar))
            {
                return this.CreateError(
                    "ASNP0050",
                    "The encoded value is not a member of the effective alphabet.",
                    false);
            }

            index = index + 1;
        }

        return new PerStringResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Text = text.ToText(),
            CharacterCount = count,
            BitsPerCharacter = characterBits,
            BitCount = count * characterBits,
            CompactedAlphabet = compacted
        };
    }

    internal PermittedAlphabetResult CreateDefaultAlphabet(AsnTypeKind kind)
    {
        IntegerConstraintSet repertoire = new CharacterRepertoireProvider()
            .CreateRepertoire(kind);
        PermittedAlphabetResult result = new PermittedAlphabetResult
        {
            Domain = repertoire,
            Root = repertoire,
            KnownScalars = repertoire
        };
        this.BuildAlphabetIndexes(result);
        return result;
    }

    internal void BuildAlphabetIndexes(PermittedAlphabetResult alphabet)
    {
        if (alphabet.Root == null)
        {
            return;
        }

        AlphabetIndexRange[] ranges =
            new AlphabetIndexRange[alphabet.Root.Intervals.Length];
        int firstIndex = 0;
        int index = 0;
        while (index < ranges.Length)
        {
            IntegerInterval interval = alphabet.Root.Intervals[index];
            if (!interval.HasLowerBound || !interval.HasUpperBound
                || interval.LowerBound < System.Numerics.BigInteger.Zero
                || interval.UpperBound
                    > new System.Numerics.BigInteger(1114111))
            {
                alphabet.RootIndexRanges = [];
                alphabet.RootAlphabetSize = 0;
                return;
            }

            int lower = (int)interval.LowerBound;
            int upper = (int)interval.UpperBound;
            ranges[index] = new AlphabetIndexRange
            {
                LowerScalar = lower,
                UpperScalar = upper,
                FirstIndex = firstIndex
            };
            firstIndex = firstIndex + upper - lower + 1;
            index = index + 1;
        }

        alphabet.RootIndexRanges = ranges;
        alphabet.RootAlphabetSize = firstIndex;
    }

    internal PerStringResult ValidateAlphabet(
        PermittedAlphabetResult alphabet)
    {
        if (alphabet.RootIndexRanges.Length == 0
            && alphabet.Root != null && !alphabet.Root.IsEmpty)
        {
            this.BuildAlphabetIndexes(alphabet);
        }

        if (alphabet.RootAlphabetSize < 1
            || alphabet.RootIndexRanges.Length == 0)
        {
            return this.CreateError(
                "ASNP0051",
                "The effective permitted alphabet is empty or unbounded.",
                false);
        }

        return new PerStringResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal PerStringResult ValidateSizeBounds(
        int lowerBound,
        bool hasUpperBound,
        int upperBound)
    {
        if (lowerBound < 0
            || hasUpperBound && upperBound < lowerBound)
        {
            return this.CreateError(
                "ASNP0052",
                "The character-string size bounds are invalid.",
                false);
        }

        return new PerStringResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal PerStringResult ReadScalars(
        string text,
        PermittedAlphabetResult alphabet)
    {
        System.Collections.Generic.List<int> scalars =
            new System.Collections.Generic.List<int>();
        BerCharacterStringCodec characters = new BerCharacterStringCodec(
            this.Limits);
        int index = 0;
        while (index < text.Length)
        {
            UnicodeScalarReadResult scalar = characters.ReadTextScalar(
                text,
                index);
            if (!scalar.Success || alphabet.GetRootIndex(scalar.Scalar) < 0)
            {
                return this.CreateError(
                    "ASNP0048",
                    "A character is outside the effective permitted alphabet.",
                    false);
            }

            scalars.Add(scalar.Scalar);
            index = scalar.NextIndex;
        }

        byte[] data = new byte[scalars.Count * 4];
        int scalarIndex = 0;
        while (scalarIndex < scalars.Count)
        {
            int value = scalars[scalarIndex];
            int offset = scalarIndex * 4;
            data[offset] = (byte)((value >> 24) & 255);
            data[offset + 1] = (byte)((value >> 16) & 255);
            data[offset + 2] = (byte)((value >> 8) & 255);
            data[offset + 3] = (byte)(value & 255);
            scalarIndex = scalarIndex + 1;
        }

        return new PerStringResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Data = data,
            CharacterCount = scalars.Count
        };
    }

    internal int[] DecodeScalarArray(byte[] data)
    {
        int[] scalars = new int[data.Length / 4];
        int index = 0;
        while (index < scalars.Length)
        {
            int offset = index * 4;
            scalars[index] = data[offset] << 24
                | data[offset + 1] << 16
                | data[offset + 2] << 8
                | data[offset + 3];
            index = index + 1;
        }

        return scalars;
    }

    internal int GetCharacterBitCount(int alphabetSize, bool aligned)
    {
        int bits = 0;
        int maximumIndex = alphabetSize - 1;
        while (maximumIndex > 0)
        {
            bits = bits + 1;
            maximumIndex = maximumIndex >> 1;
        }

        if (!aligned || bits <= 1)
        {
            return bits;
        }

        int power = 1;
        while (power < bits)
        {
            power = power * 2;
        }

        return power;
    }

    internal bool UsesCompactedAlphabet(
        PermittedAlphabetResult alphabet,
        int characterBits)
    {
        int lastRange = alphabet.RootIndexRanges.Length - 1;
        int maximumScalar = alphabet.RootIndexRanges[lastRange].UpperScalar;
        System.Numerics.BigInteger maximumValue = characterBits == 0
            ? System.Numerics.BigInteger.Zero
            : (System.Numerics.BigInteger.One << characterBits)
                - System.Numerics.BigInteger.One;
        return new System.Numerics.BigInteger(maximumScalar) > maximumValue;
    }

    internal bool AppendScalar(TextAccumulator text, int scalar)
    {
        if (scalar < 0 || scalar > 1114111
            || scalar >= 55296 && scalar <= 57343)
        {
            return false;
        }

        if (scalar <= 65535)
        {
            text.AppendCharacter((char)scalar);
            return true;
        }

        int adjusted = scalar - 65536;
        text.AppendCharacter((char)(55296 + adjusted / 1024));
        text.AppendCharacter((char)(56320 + adjusted % 1024));
        return true;
    }

    internal bool IsKnownMultiplierType(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.NumericString
            || kind == AsnTypeKind.PrintableString
            || kind == AsnTypeKind.VisibleString
            || kind == AsnTypeKind.Ia5String
            || kind == AsnTypeKind.BmpString
            || kind == AsnTypeKind.UniversalString;
    }

    internal int CountTextScalars(string text)
    {
        int count = 0;
        int index = 0;
        BerCharacterStringCodec characters = new BerCharacterStringCodec(
            this.Limits);
        while (index < text.Length)
        {
            UnicodeScalarReadResult scalar = characters.ReadTextScalar(
                text,
                index);
            if (!scalar.Success)
            {
                return -1;
            }

            count = count + 1;
            index = scalar.NextIndex;
        }

        return count;
    }

    internal byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        if (source.Length > 0)
        {
            System.Buffer.BlockCopy(source, 0, result, 0, source.Length);
        }

        return result;
    }

    internal PerStringResult HandleIncompleteRead(
        PerBitReader reader,
        PerBitReadResult input,
        int start)
    {
        if (input.Status == AsnDecodeStatus.NeedMoreData)
        {
            reader.Position = start;
            return new PerStringResult
            {
                Status = AsnDecodeStatus.NeedMoreData
            };
        }

        return this.CopyReaderFailure(reader);
    }

    internal PerStringResult CopyBerFailure(BerPrimitiveValueResult source)
    {
        return this.CreateError(
            source.ErrorCode,
            source.ErrorMessage,
            source.LimitExceeded);
    }

    internal PerStringResult CopyLengthFailure(PerLengthResult source)
    {
        return new PerStringResult
        {
            Status = source.Status,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerStringResult CopyPrimitiveFailure(PerPrimitiveResult source)
    {
        return new PerStringResult
        {
            Status = source.Status,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerStringResult CopyReaderFailure(PerBitReader reader)
    {
        return new PerStringResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = reader.LimitExceeded,
            ErrorCode = reader.ErrorCode,
            ErrorMessage = reader.ErrorMessage
        };
    }

    internal PerStringResult CopyWriterFailure(PerBitWriter writer)
    {
        return new PerStringResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = writer.LimitExceeded,
            ErrorCode = writer.ErrorCode,
            ErrorMessage = writer.ErrorMessage
        };
    }

    internal PerStringResult CreateError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new PerStringResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
