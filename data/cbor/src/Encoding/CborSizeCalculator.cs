using System;
using System.Collections.Generic;
using System.Text;

namespace cbor;

internal class CborSizeCalculator
{
    internal CborEncodeOptions Options;
    internal CborTagLookup Tags;
    internal List<CborToken> Tokens = new List<CborToken>();
    internal List<CborValue> Ancestors = new List<CborValue>();
    internal List<CborValue> SharedValues = new List<CborValue>();
    internal List<List<CborValue>> StringNamespaces = new List<List<CborValue>>();
    internal long RequiredBytes;
    internal int TotalItems;
    internal string? ErrorCode;
    internal string? ErrorMessage;
    internal string? ErrorPath;

    internal CborSizeCalculator(CborEncodeOptions options)
    {
        Options = options;
        Tags = new CborTagLookup(options.TagRegistrations);
    }

    internal bool Calculate(CborValue value)
    {
        if (Tags.ErrorCode != null)
        {
            return Fail(
                Tags.ErrorCode,
                Tags.ErrorMessage ?? "The tag registrations are invalid.",
                "$"
            );
        }
        if (!ValidateSharingOptions())
        {
            return false;
        }
        return VisitValue(value, "$", 0);
    }

    internal bool VisitValue(CborValue? value, string path, int depth)
    {
        if (value == null)
        {
            return Fail(
                "CBOR_VALUE_MISSING",
                "A CBOR value is required.",
                path
            );
        }
        if (!ValidateSharingAnnotations(value, path))
        {
            return false;
        }
        if (Options.SharedValuePolicy == CborSharedValuePolicy.Resolve)
        {
            int existingIndex = FindIdentity(SharedValues, value);
            if (existingIndex >= 0)
            {
                return AddSharedReference(existingIndex, path, depth);
            }
            if (value.IsSharedValue)
            {
                int sharedIndex = RegisterSharedValue(value, path, false);
                if (sharedIndex < 0)
                {
                    return false;
                }
                if (!EnterSyntheticTag(path, depth)
                    || !AddToken(new CborToken
                    {
                        Kind = CborTokenKind.Tag,
                        TagNumber = CborTagNumbers.ShareableValue
                    }, path))
                {
                    return false;
                }
                return VisitOrdinaryValue(value, $"{path}<share>", depth + 1);
            }
        }
        return VisitOrdinaryValue(value, path, depth);
    }

    internal bool VisitOrdinaryValue(CborValue value, string path, int depth)
    {
        if (!EnterValue(value, path))
        {
            return false;
        }
        if (!ValidateRepresentationKind(value, path))
        {
            return false;
        }

        bool succeeded;
        switch (value.Kind)
        {
            case CborValueKind.UnsignedInteger:
                succeeded = AddToken(new CborToken
                {
                    Kind = CborTokenKind.UnsignedInteger,
                    IntegerValue = value.IntegerValue,
                    Representation = value.Representation
                }, path);
                break;
            case CborValueKind.NegativeInteger:
                succeeded = AddToken(new CborToken
                {
                    Kind = CborTokenKind.NegativeInteger,
                    IntegerValue = value.IntegerValue,
                    Representation = value.Representation
                }, path);
                break;
            case CborValueKind.ByteString:
                succeeded = VisitByteString(value, path, depth);
                break;
            case CborValueKind.TextString:
                succeeded = VisitTextString(value, path, depth);
                break;
            case CborValueKind.Array:
                succeeded = VisitArray(value, path, depth);
                break;
            case CborValueKind.Map:
                succeeded = VisitMap(value, path, depth);
                break;
            case CborValueKind.Tag:
                succeeded = VisitTag(value, path, depth);
                break;
            case CborValueKind.Float:
                succeeded = VisitFloat(value, path);
                break;
            case CborValueKind.SimpleValue:
                succeeded = AddToken(new CborToken
                {
                    Kind = CborTokenKind.SimpleValue,
                    SimpleValue = value.SimpleValue,
                    Representation = value.Representation
                }, path);
                break;
            default:
                succeeded = Fail(
                    "CBOR_VALUE_KIND_INVALID",
                    "The CBOR value kind is not valid for encoding.",
                    path
                );
                break;
        }

        if (succeeded)
        {
            LeaveValue();
        }
        return succeeded;
    }

    internal bool VisitByteString(CborValue value, string path, int depth)
    {
        if (value.ByteStringValue == null)
        {
            return Fail(
                "CBOR_BYTE_STRING_VALUE_MISSING",
                "A byte string value requires a byte payload.",
                path
            );
        }
        CborLengthKind lengthKind = SelectLengthKind(value, path);
        if (ErrorCode != null)
        {
            return false;
        }
        if (PreservesRepresentation()
            && value.Representation != null
            && lengthKind == CborLengthKind.Definite
            && value.Representation.ChunkByteLengths != null)
        {
            return Fail(
                "CBOR_REPRESENTATION_CHUNKS_INVALID",
                "A definite byte string cannot retain indefinite chunk lengths.",
                path
            );
        }
        ulong length = (ulong)value.ByteStringValue.Length;
        if (lengthKind == CborLengthKind.Definite)
        {
            bool encoded = AddToken(new CborToken
            {
                Kind = CborTokenKind.ByteString,
                LengthKind = CborLengthKind.Definite,
                Length = length,
                ByteStringValue = value.ByteStringValue,
                Representation = value.Representation
            }, path);
            if (encoded)
            {
                RegisterString(value);
            }
            return encoded;
        }
        if (!EnterCompound(depth, path))
        {
            return false;
        }
        if (!AddToken(new CborToken
        {
            Kind = CborTokenKind.StartByteString,
            LengthKind = CborLengthKind.Indefinite,
            Representation = value.Representation
        }, path))
        {
            return false;
        }
        if (PreservesRepresentation() && value.Representation != null)
        {
            if (!AddPreservedByteStringChunks(value, path))
            {
                return false;
            }
        }
        else if (value.ByteStringValue.Length > 0
            && !AddToken(new CborToken
            {
                Kind = CborTokenKind.ByteStringChunk,
                LengthKind = CborLengthKind.Definite,
                Length = length,
                ByteStringValue = value.ByteStringValue
            }, path))
        {
            return false;
        }
        return AddToken(new CborToken
        {
            Kind = CborTokenKind.EndByteString,
            LengthKind = CborLengthKind.Indefinite
        }, path);
    }

    internal bool VisitTextString(CborValue value, string path, int depth)
    {
        if (value.TextStringValue == null)
        {
            return Fail(
                "CBOR_TEXT_STRING_VALUE_MISSING",
                "A text string value requires a text payload.",
                path
            );
        }
        if (!CborTokenWritePlan.IsWellFormedUtf16(value.TextStringValue))
        {
            return Fail(
                "CBOR_TEXT_STRING_INVALID_UTF16",
                "The text string contains an unpaired UTF-16 surrogate.",
                path
            );
        }
        CborLengthKind lengthKind = SelectLengthKind(value, path);
        if (ErrorCode != null)
        {
            return false;
        }
        if (PreservesRepresentation()
            && value.Representation != null
            && lengthKind == CborLengthKind.Definite
            && value.Representation.ChunkByteLengths != null)
        {
            return Fail(
                "CBOR_REPRESENTATION_CHUNKS_INVALID",
                "A definite text string cannot retain indefinite chunk lengths.",
                path
            );
        }
        ulong length = (ulong)Encoding.UTF8.GetByteCount(value.TextStringValue);
        if (lengthKind == CborLengthKind.Definite)
        {
            bool encoded = AddToken(new CborToken
            {
                Kind = CborTokenKind.TextString,
                LengthKind = CborLengthKind.Definite,
                Length = length,
                TextStringValue = value.TextStringValue,
                Representation = value.Representation
            }, path);
            if (encoded)
            {
                RegisterString(value);
            }
            return encoded;
        }
        if (!EnterCompound(depth, path))
        {
            return false;
        }
        if (!AddToken(new CborToken
        {
            Kind = CborTokenKind.StartTextString,
            LengthKind = CborLengthKind.Indefinite,
            Representation = value.Representation
        }, path))
        {
            return false;
        }
        if (PreservesRepresentation() && value.Representation != null)
        {
            if (!AddPreservedTextStringChunks(value, path))
            {
                return false;
            }
        }
        else if (value.TextStringValue.Length > 0
            && !AddToken(new CborToken
            {
                Kind = CborTokenKind.TextStringChunk,
                LengthKind = CborLengthKind.Definite,
                Length = length,
                TextStringValue = value.TextStringValue
            }, path))
        {
            return false;
        }
        return AddToken(new CborToken
        {
            Kind = CborTokenKind.EndTextString,
            LengthKind = CborLengthKind.Indefinite
        }, path);
    }

    internal bool VisitArray(CborValue value, string path, int depth)
    {
        if (value.ArrayItems == null)
        {
            return Fail(
                "CBOR_ARRAY_ITEMS_MISSING",
                "An array value requires an item collection.",
                path
            );
        }
        if (!EnterCompound(depth, path))
        {
            return false;
        }
        CborLengthKind lengthKind = SelectLengthKind(value, path);
        if (ErrorCode != null)
        {
            return false;
        }
        if (!AddToken(new CborToken
        {
            Kind = CborTokenKind.StartArray,
            LengthKind = lengthKind,
            Length = (ulong)value.ArrayItems.Count,
            Representation = value.Representation
        }, path))
        {
            return false;
        }
        int index = 0;
        while (index < value.ArrayItems.Count)
        {
            if (!VisitValue(
                value.ArrayItems[index],
                $"{path}[{index}]",
                depth + 1))
            {
                return false;
            }
            index = index + 1;
        }
        return AddToken(new CborToken
        {
            Kind = CborTokenKind.EndArray,
            LengthKind = lengthKind
        }, path);
    }

    internal bool VisitMap(CborValue value, string path, int depth)
    {
        if (value.MapEntries == null)
        {
            return Fail(
                "CBOR_MAP_ENTRIES_MISSING",
                "A map value requires an entry collection.",
                path
            );
        }
        if (!EnterCompound(depth, path))
        {
            return false;
        }
        CborLengthKind lengthKind = SelectLengthKind(value, path);
        if (ErrorCode != null)
        {
            return false;
        }
        if (!AddToken(new CborToken
        {
            Kind = CborTokenKind.StartMap,
            LengthKind = lengthKind,
            Length = (ulong)value.MapEntries.Count,
            Representation = value.Representation
        }, path))
        {
            return false;
        }
        List<CborMapEntry> entries = value.MapEntries;
        if (UsesDeterministicProfile())
        {
            List<CborMapEntry>? orderedEntries = BuildDeterministicEntryOrder(
                value.MapEntries,
                path
            );
            if (orderedEntries == null)
            {
                return false;
            }
            entries = orderedEntries;
        }
        int index = 0;
        while (index < entries.Count)
        {
            CborMapEntry entry = entries[index];
            if (entry == null)
            {
                return Fail(
                    "CBOR_MAP_ENTRY_MISSING",
                    "A map entry is required.",
                    $"{path}{{{index}}}"
                );
            }
            if (!VisitValue(entry.Key, $"{path}{{key:{index}}}", depth + 1))
            {
                return false;
            }
            if (!VisitValue(entry.Value, $"{path}{{value:{index}}}", depth + 1))
            {
                return false;
            }
            index = index + 1;
        }
        return AddToken(new CborToken
        {
            Kind = CborTokenKind.EndMap,
            LengthKind = lengthKind
        }, path);
    }

    internal List<CborMapEntry>? BuildDeterministicEntryOrder(
        List<CborMapEntry> source,
        string path)
    {
        List<CborMapEntry> entries = new List<CborMapEntry>();
        List<byte[]> keys = new List<byte[]>();
        CborMapKeyOrder order = SelectDeterministicMapKeyOrder();
        int sourceIndex = 0;
        while (sourceIndex < source.Count)
        {
            CborMapEntry entry = source[sourceIndex];
            if (entry == null)
            {
                Fail(
                    "CBOR_MAP_ENTRY_MISSING",
                    "A map entry is required.",
                    $"{path}{{{sourceIndex}}}"
                );
                return null;
            }
            if (entry.Key == null)
            {
                Fail(
                    "CBOR_VALUE_MISSING",
                    "A CBOR value is required.",
                    $"{path}{{key:{sourceIndex}}}"
                );
                return null;
            }
            byte[]? encodedKey = EncodeDeterministicKey(
                entry.Key,
                $"{path}{{key:{sourceIndex}}}"
            );
            if (encodedKey == null)
            {
                return null;
            }

            int duplicateIndex = 0;
            while (duplicateIndex < entries.Count)
            {
                if (CborKeyComparer.CompareDeterministicEncoding(
                        keys[duplicateIndex],
                        encodedKey,
                        order) == 0
                    || CborKeyComparer.AreEquivalent(
                        entries[duplicateIndex].Key,
                        entry.Key))
                {
                    Fail(
                        "CBOR_DETERMINISTIC_DUPLICATE_KEY",
                        "A deterministic map cannot contain duplicate or equivalent keys.",
                        $"{path}{{key:{sourceIndex}}}"
                    );
                    return null;
                }
                duplicateIndex = duplicateIndex + 1;
            }

            int position = entries.Count;
            entries.Add(entry);
            keys.Add(encodedKey);
            while (position > 0
                && CborKeyComparer.CompareDeterministicEncoding(
                    keys[position - 1],
                    encodedKey,
                    order) > 0)
            {
                entries[position] = entries[position - 1];
                keys[position] = keys[position - 1];
                position = position - 1;
            }
            entries[position] = entry;
            keys[position] = encodedKey;
            sourceIndex = sourceIndex + 1;
        }
        return entries;
    }

    internal byte[]? EncodeDeterministicKey(CborValue key, string path)
    {
        CborSizeCalculator calculator = new CborSizeCalculator(Options);
        if (!calculator.Calculate(key))
        {
            Fail(
                calculator.ErrorCode ?? "CBOR_ENCODE_INVALID_VALUE",
                calculator.ErrorMessage ?? "The map key cannot be encoded.",
                path
            );
            return null;
        }
        byte[] encoded = new byte[(int)calculator.RequiredBytes];
        CborEncodingPlanWriter writer = new CborEncodingPlanWriter(Options);
        if (!writer.WritePlan(calculator, encoded))
        {
            Fail(
                writer.ErrorCode ?? "CBOR_ENCODE_INTERNAL_FAILURE",
                writer.ErrorMessage ?? "The deterministic map key could not be written.",
                path
            );
            return null;
        }
        return encoded;
    }

    internal bool UsesDeterministicProfile()
    {
        return Options.EncodingProfile == CborEncodingProfile.CoreDeterministic
            || Options.EncodingProfile
                == CborEncodingProfile.LengthFirstCoreDeterministic;
    }

    internal CborMapKeyOrder SelectDeterministicMapKeyOrder()
    {
        if (Options.EncodingProfile
            == CborEncodingProfile.LengthFirstCoreDeterministic)
        {
            return CborMapKeyOrder.LengthFirst;
        }
        return CborMapKeyOrder.Lexicographic;
    }

    internal bool PreservesRepresentation()
    {
        return Options.PreserveRepresentation && !UsesDeterministicProfile();
    }

    internal bool ValidateRepresentationKind(CborValue value, string path)
    {
        if (!PreservesRepresentation() || value.Representation == null)
        {
            return true;
        }
        CborRepresentation representation = value.Representation;
        if (value.Kind == CborValueKind.ByteString
            || value.Kind == CborValueKind.TextString)
        {
            if (representation.HasFloatBits
                || representation.FloatWidth != CborFloatWidth.None)
            {
                return Fail(
                    "CBOR_REPRESENTATION_KIND_MISMATCH",
                    "String representation metadata cannot contain floating-point data.",
                    path
                );
            }
            return true;
        }
        if (value.Kind == CborValueKind.Array || value.Kind == CborValueKind.Map)
        {
            if (representation.ChunkByteLengths != null
                || representation.HasFloatBits
                || representation.FloatWidth != CborFloatWidth.None)
            {
                return Fail(
                    "CBOR_REPRESENTATION_KIND_MISMATCH",
                    "Container representation metadata contains fields for another value kind.",
                    path
                );
            }
            return true;
        }
        if (value.Kind == CborValueKind.Float)
        {
            if (representation.ChunkByteLengths != null
                || representation.LengthKind != CborLengthKind.None)
            {
                return Fail(
                    "CBOR_REPRESENTATION_KIND_MISMATCH",
                    "Floating-point representation metadata contains string or container fields.",
                    path
                );
            }
            return true;
        }
        if (representation.ChunkByteLengths != null
            || representation.LengthKind != CborLengthKind.None
            || representation.HasFloatBits
            || representation.FloatWidth != CborFloatWidth.None)
        {
            return Fail(
                "CBOR_REPRESENTATION_KIND_MISMATCH",
                "The representation metadata contains fields for another value kind.",
                path
            );
        }
        return true;
    }

    internal bool VisitTag(CborValue value, string path, int depth)
    {
        if (!EnterCompound(depth, path))
        {
            return false;
        }
        bool referenceTag = value.TagNumber == CborTagNumbers.StringReference
            || value.TagNumber == CborTagNumbers.StringReferenceNamespace
            || value.TagNumber == CborTagNumbers.ShareableValue
            || value.TagNumber == CborTagNumbers.SharedValueReference;
        if (referenceTag
            && Options.SharedValuePolicy == CborSharedValuePolicy.Disabled)
        {
            return Fail(
                "CBOR_REFERENCE_TAG_DISABLED",
                "Reference-related CBOR tags are disabled by the encoding options.",
                path
            );
        }
        if (value.TagNumber == CborTagNumbers.StringReferenceNamespace)
        {
            return VisitStringNamespace(value, path, depth);
        }
        if (value.TagNumber == CborTagNumbers.StringReference)
        {
            return VisitStringReference(value, path, depth);
        }
        if (value.TagNumber == CborTagNumbers.ShareableValue)
        {
            return VisitShareableValue(value, path, depth);
        }
        if (value.TagNumber == CborTagNumbers.SharedValueReference)
        {
            return VisitSharedValueReference(value, path, depth);
        }
        if (value.TaggedValue == null)
        {
            return Fail(
                "CBOR_TAGGED_VALUE_MISSING",
                "A semantic tag requires a tagged value.",
                path
            );
        }
        if (!AddToken(new CborToken
        {
            Kind = CborTokenKind.Tag,
            TagNumber = value.TagNumber,
            Representation = value.Representation
        }, path))
        {
            return false;
        }
        return VisitValue(value.TaggedValue, $"{path}<tag>", depth + 1);
    }

    internal bool VisitStringNamespace(CborValue value, string path, int depth)
    {
        if (value.TaggedValue == null)
        {
            return Fail(
                "CBOR_STRING_NAMESPACE_VALUE_MISSING",
                "A string-reference namespace requires a content value.",
                path
            );
        }
        if (!AddToken(new CborToken
        {
            Kind = CborTokenKind.Tag,
            TagNumber = value.TagNumber,
            Representation = value.Representation
        }, path))
        {
            return false;
        }
        StringNamespaces.Add(new List<CborValue>());
        bool encoded = VisitValue(
            value.TaggedValue,
            $"{path}<namespace>",
            depth + 1
        );
        StringNamespaces.RemoveAt(StringNamespaces.Count - 1);
        return encoded;
    }

    internal bool VisitStringReference(CborValue value, string path, int depth)
    {
        if (StringNamespaces.Count == 0)
        {
            return Fail(
                "CBOR_STRING_REFERENCE_OUTSIDE_NAMESPACE",
                "A string reference is valid only inside a string-reference namespace.",
                path
            );
        }
        List<CborValue> strings = StringNamespaces[StringNamespaces.Count - 1];
        int index = ResolveReferenceIndex(
            value,
            strings,
            "CBOR_STRING_REFERENCE_TYPE",
            "CBOR_STRING_REFERENCE_INDEX_OUT_OF_RANGE",
            "CBOR_STRING_REFERENCE_NOT_FOUND",
            path
        );
        if (index < 0)
        {
            return false;
        }
        return AddReferenceTag(value, index, path, depth);
    }

    internal bool VisitShareableValue(CborValue value, string path, int depth)
    {
        if (value.TaggedValue == null)
        {
            return Fail(
                "CBOR_SHAREABLE_VALUE_MISSING",
                "A shareable-value tag requires a content value.",
                path
            );
        }
        bool allowsRepeatedIdentity = Options.SharedValuePolicy
            == CborSharedValuePolicy.Preserve;
        int sharedIndex = RegisterSharedValue(
            value.TaggedValue,
            path,
            allowsRepeatedIdentity
        );
        if (sharedIndex < 0)
        {
            return false;
        }
        if (!AddToken(new CborToken
        {
            Kind = CborTokenKind.Tag,
            TagNumber = value.TagNumber,
            Representation = value.Representation
        }, path))
        {
            return false;
        }
        return VisitOrdinaryValue(
            value.TaggedValue,
            $"{path}<share>",
            depth + 1
        );
    }

    internal bool VisitSharedValueReference(
        CborValue value,
        string path,
        int depth)
    {
        int index = ResolveReferenceIndex(
            value,
            SharedValues,
            "CBOR_SHARED_REFERENCE_TYPE",
            "CBOR_SHARED_REFERENCE_INDEX_OUT_OF_RANGE",
            "CBOR_SHARED_REFERENCE_NOT_FOUND",
            path
        );
        if (index < 0)
        {
            return false;
        }
        return AddReferenceTag(value, index, path, depth);
    }

    internal bool AddReferenceTag(
        CborValue value,
        int index,
        string path,
        int depth)
    {
        if (!AddToken(new CborToken
        {
            Kind = CborTokenKind.Tag,
            TagNumber = value.TagNumber,
            Representation = value.Representation
        }, path))
        {
            return false;
        }
        if (value.TaggedValue != null)
        {
            return VisitValue(value.TaggedValue, $"{path}<reference>", depth + 1);
        }
        return AddReferenceIndex(index, $"{path}<reference>");
    }

    internal bool VisitFloat(CborValue value, string path)
    {
        if (value.FloatValue == null)
        {
            return Fail(
                "CBOR_FLOAT_VALUE_MISSING",
                "A floating-point value requires floating-point data.",
                path
            );
        }
        if (PreservesRepresentation() && value.Representation != null)
        {
            return VisitPreservedFloat(value, path);
        }
        CborFloatWidth width = SelectFloatWidth(value);
        CborToken sourceToken = new CborToken
        {
            Kind = CborTokenKind.Float,
            FloatWidth = width,
            FloatValue = value.FloatValue,
            Representation = value.Representation
        };
        CborTokenWritePlan sourcePlan = CborTokenWritePlan.Create(sourceToken);
        if (sourcePlan.ErrorCode != null)
        {
            return Fail(
                sourcePlan.ErrorCode,
                sourcePlan.ErrorMessage ?? "The floating-point value cannot be encoded.",
                path
            );
        }
        bool coreDeterministic = UsesDeterministicProfile();
        bool preferredReduction = Options.EncodingProfile
            == CborEncodingProfile.Preferred
            && Options.ReduceFloatWidth
            && (!Options.PreserveRepresentation || value.Representation == null);
        if (!coreDeterministic && !preferredReduction)
        {
            return AddToken(sourceToken, path);
        }
        double numericValue = ReadFloatNumericValue(value.FloatValue, width);
        if (double.IsNaN(numericValue))
        {
            if (coreDeterministic)
            {
                return AddToken(
                    CreateRetainedFloatToken(CborFloatWidth.Half, 0x7e00UL),
                    path
                );
            }
            return AddToken(SelectPreferredNaNToken(sourcePlan), path);
        }
        CborFloatWidth preferredWidth = SelectPreferredFloatWidth(
            numericValue,
            coreDeterministic ? CborFloatWidth.Double : width
        );
        if (preferredWidth == width)
        {
            return AddToken(sourceToken, path);
        }
        CborFloatValue preferredValue = new CborFloatValue();
        preferredValue.Value = numericValue;
        return AddToken(new CborToken
        {
            Kind = CborTokenKind.Float,
            FloatWidth = preferredWidth,
            FloatValue = preferredValue
        }, path);
    }

    internal bool VisitPreservedFloat(CborValue value, string path)
    {
        CborRepresentation? representation = value.Representation;
        CborFloatValue? current = value.FloatValue;
        if (representation == null || current == null)
        {
            return Fail(
                "CBOR_REPRESENTATION_FLOAT_MISMATCH",
                "A retained floating-point representation requires a value and metadata.",
                path
            );
        }
        if (!representation.HasFloatBits)
        {
            return Fail(
                "CBOR_REPRESENTATION_FLOAT_BITS_MISSING",
                "A retained floating-point representation requires exact bits.",
                path
            );
        }
        CborFloatWidth width = representation.FloatWidth;
        int expectedHeadWidth;
        CborFloatValue retained;
        if (width == CborFloatWidth.Half
            && representation.FloatBits <= ushort.MaxValue)
        {
            expectedHeadWidth = 2;
            retained = CborHalfPrecision.CreateHalfFloat(
                (ushort)representation.FloatBits);
        }
        else if (width == CborFloatWidth.Single
            && representation.FloatBits <= uint.MaxValue)
        {
            expectedHeadWidth = 4;
            retained = CborHalfPrecision.CreateSingleFloat(
                (uint)representation.FloatBits);
        }
        else if (width == CborFloatWidth.Double)
        {
            expectedHeadWidth = 8;
            retained = CborHalfPrecision.CreateDoubleFloat(
                representation.FloatBits);
        }
        else
        {
            return Fail(
                "CBOR_REPRESENTATION_FLOAT_WIDTH_INVALID",
                "The retained floating-point width or bits are invalid.",
                path
            );
        }
        if (representation.HeadArgumentWidth != expectedHeadWidth
            || (value.FloatWidth != CborFloatWidth.None
                && value.FloatWidth != width)
            || (current.Width != CborFloatWidth.None
                && (current.Width != width
                    || current.RawBits != representation.FloatBits)))
        {
            return Fail(
                "CBOR_REPRESENTATION_FLOAT_MISMATCH",
                "The retained floating-point representation no longer matches the value.",
                path
            );
        }
        bool currentIsNaN = double.IsNaN(current.Value);
        bool retainedIsNaN = double.IsNaN(retained.Value);
        if (currentIsNaN != retainedIsNaN
            || (!currentIsNaN
                && BitConverter.DoubleToUInt64Bits(current.Value)
                    != BitConverter.DoubleToUInt64Bits(retained.Value)))
        {
            return Fail(
                "CBOR_REPRESENTATION_FLOAT_MISMATCH",
                "The retained floating-point bits encode a different value.",
                path
            );
        }
        return AddToken(new CborToken
        {
            Kind = CborTokenKind.Float,
            FloatWidth = width,
            FloatValue = retained,
            Representation = representation
        }, path);
    }

    internal bool AddPreservedByteStringChunks(CborValue value, string path)
    {
        CborRepresentation? representation = value.Representation;
        byte[]? bytes = value.ByteStringValue;
        if (representation == null || bytes == null)
        {
            return Fail(
                "CBOR_REPRESENTATION_CHUNKS_MISSING",
                "An indefinite byte string requires a payload and retained metadata.",
                path
            );
        }
        if (representation.ChunkByteLengths == null)
        {
            return Fail(
                "CBOR_REPRESENTATION_CHUNKS_MISSING",
                "An indefinite byte string requires retained chunk lengths.",
                path
            );
        }
        int offset = 0;
        int index = 0;
        while (index < representation.ChunkByteLengths.Count)
        {
            int length = representation.ChunkByteLengths[index];
            if (length < 0 || offset > bytes.Length - length)
            {
                return Fail(
                    "CBOR_REPRESENTATION_CHUNK_LENGTH_MISMATCH",
                    "The retained byte-string chunks do not match the current payload.",
                    path
                );
            }
            byte[] chunk = bytes.AsSpan(offset, length).ToArray();
            if (!AddToken(new CborToken
            {
                Kind = CborTokenKind.ByteStringChunk,
                LengthKind = CborLengthKind.Definite,
                Length = (ulong)length,
                ByteStringValue = chunk
            }, path))
            {
                return false;
            }
            offset = offset + length;
            index = index + 1;
        }
        if (offset != bytes.Length)
        {
            return Fail(
                "CBOR_REPRESENTATION_CHUNK_LENGTH_MISMATCH",
                "The retained byte-string chunks do not cover the current payload.",
                path
            );
        }
        return true;
    }

    internal bool AddPreservedTextStringChunks(CborValue value, string path)
    {
        CborRepresentation? representation = value.Representation;
        string? text = value.TextStringValue;
        if (representation == null || text == null)
        {
            return Fail(
                "CBOR_REPRESENTATION_CHUNKS_MISSING",
                "An indefinite text string requires text and retained metadata.",
                path
            );
        }
        if (representation.ChunkByteLengths == null)
        {
            return Fail(
                "CBOR_REPRESENTATION_CHUNKS_MISSING",
                "An indefinite text string requires retained chunk lengths.",
                path
            );
        }
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        int offset = 0;
        int index = 0;
        while (index < representation.ChunkByteLengths.Count)
        {
            int length = representation.ChunkByteLengths[index];
            if (length < 0 || offset > bytes.Length - length)
            {
                return Fail(
                    "CBOR_REPRESENTATION_CHUNK_LENGTH_MISMATCH",
                    "The retained text-string chunks do not match the current payload.",
                    path
                );
            }
            CborUtf8 decoder = new CborUtf8();
            CborUtf8Result decoded = decoder.ProcessChunk(
                bytes.AsSpan(offset, length),
                true
            );
            if (decoded.Status != CborReadStatus.Done)
            {
                return Fail(
                    "CBOR_REPRESENTATION_TEXT_CHUNK_INVALID",
                    "A retained text chunk boundary splits a UTF-8 scalar value.",
                    path
                );
            }
            if (!AddToken(new CborToken
            {
                Kind = CborTokenKind.TextStringChunk,
                LengthKind = CborLengthKind.Definite,
                Length = (ulong)length,
                TextStringValue = decoded.Text
            }, path))
            {
                return false;
            }
            offset = offset + length;
            index = index + 1;
        }
        if (offset != bytes.Length)
        {
            return Fail(
                "CBOR_REPRESENTATION_CHUNK_LENGTH_MISMATCH",
                "The retained text-string chunks do not cover the current payload.",
                path
            );
        }
        return true;
    }

    internal CborFloatWidth SelectFloatWidth(CborValue value)
    {
        CborFloatWidth width = value.FloatWidth;
        if (width == CborFloatWidth.None && value.FloatValue != null)
        {
            width = value.FloatValue.Width;
        }
        if (width == CborFloatWidth.None)
        {
            width = CborFloatWidth.Double;
        }
        return width;
    }

    internal double ReadFloatNumericValue(
        CborFloatValue value,
        CborFloatWidth width)
    {
        if (value.Width == CborFloatWidth.None)
        {
            return value.Value;
        }
        if (width == CborFloatWidth.Half)
        {
            return CborHalfPrecision.ConvertHalfBitsToDouble((ushort)value.RawBits);
        }
        if (width == CborFloatWidth.Single)
        {
            return CborHalfPrecision.ConvertSingleBitsToDouble((uint)value.RawBits);
        }
        return CborHalfPrecision.ConvertDoubleBitsToDouble(value.RawBits);
    }

    internal CborFloatWidth SelectPreferredFloatWidth(
        double value,
        CborFloatWidth maximumWidth)
    {
        if (maximumWidth == CborFloatWidth.Half)
        {
            return CborFloatWidth.Half;
        }
        if (CborHalfPrecision.IsExactlyRepresentableAsHalf(value))
        {
            return CborFloatWidth.Half;
        }
        if (maximumWidth == CborFloatWidth.Single)
        {
            return CborFloatWidth.Single;
        }
        if (CborHalfPrecision.IsExactlyRepresentableAsSingle(value))
        {
            return CborFloatWidth.Single;
        }
        return CborFloatWidth.Double;
    }

    internal CborToken SelectPreferredNaNToken(CborTokenWritePlan sourcePlan)
    {
        CborFloatWidth width = sourcePlan.FloatWidth;
        ulong bits = sourcePlan.FloatBits;
        if (width == CborFloatWidth.Half)
        {
            return CreateRetainedFloatToken(width, bits);
        }
        if (width == CborFloatWidth.Single)
        {
            ulong fraction = bits & 0x007fffffUL;
            if ((fraction & 0x1fffUL) == 0)
            {
                ulong halfBits = ((bits >> 16) & 0x8000UL)
                    | 0x7c00UL
                    | (fraction >> 13);
                return CreateRetainedFloatToken(CborFloatWidth.Half, halfBits);
            }
            return CreateRetainedFloatToken(width, bits);
        }
        ulong doubleFraction = bits & 0x000fffffffffffffUL;
        if ((doubleFraction & 0x000003ffffffffffUL) == 0)
        {
            ulong halfBits = ((bits >> 48) & 0x8000UL)
                | 0x7c00UL
                | (doubleFraction >> 42);
            return CreateRetainedFloatToken(CborFloatWidth.Half, halfBits);
        }
        if ((doubleFraction & 0x000000001fffffffUL) == 0)
        {
            ulong singleBits = ((bits >> 32) & 0x80000000UL)
                | 0x7f800000UL
                | (doubleFraction >> 29);
            return CreateRetainedFloatToken(CborFloatWidth.Single, singleBits);
        }
        return CreateRetainedFloatToken(width, bits);
    }

    internal CborToken CreateRetainedFloatToken(
        CborFloatWidth width,
        ulong bits)
    {
        CborFloatValue floatValue = new CborFloatValue();
        floatValue.Width = width;
        floatValue.RawBits = bits;
        floatValue.IsNaN = true;
        if (width == CborFloatWidth.Half)
        {
            floatValue.Value = CborHalfPrecision.ConvertHalfBitsToDouble((ushort)bits);
            floatValue.IsSignalingNaN = CborHalfPrecision.IsHalfSignalingNaN((ushort)bits);
        }
        else if (width == CborFloatWidth.Single)
        {
            floatValue.Value = CborHalfPrecision.ConvertSingleBitsToDouble((uint)bits);
            floatValue.IsSignalingNaN = CborHalfPrecision.IsSingleSignalingNaN((uint)bits);
        }
        else
        {
            floatValue.Value = CborHalfPrecision.ConvertDoubleBitsToDouble(bits);
            floatValue.IsSignalingNaN = CborHalfPrecision.IsDoubleSignalingNaN(bits);
        }
        return new CborToken
        {
            Kind = CborTokenKind.Float,
            FloatWidth = width,
            FloatValue = floatValue
        };
    }

    internal bool ValidateSharingOptions()
    {
        if (Options.SharedValuePolicy != CborSharedValuePolicy.Disabled
            && Options.SharedValuePolicy != CborSharedValuePolicy.Preserve
            && Options.SharedValuePolicy != CborSharedValuePolicy.Resolve)
        {
            return Fail(
                "CBOR_SHARED_VALUE_POLICY_INVALID",
                "The shared-value policy is not valid for encoding.",
                "$"
            );
        }
        if (Options.MaxSharedValues < 0)
        {
            return Fail(
                "CBOR_MAX_SHARED_VALUES_INVALID",
                "The maximum shared-value count cannot be negative.",
                "$"
            );
        }
        if (UsesDeterministicProfile()
            && Options.SharedValuePolicy != CborSharedValuePolicy.Disabled)
        {
            return Fail(
                "CBOR_DETERMINISTIC_REFERENCES_UNSUPPORTED",
                "Reference processing is incompatible with deterministic encoding profiles.",
                "$"
            );
        }
        return true;
    }

    internal bool ValidateSharingAnnotations(CborValue value, string path)
    {
        bool referenceTag = value.Kind == CborValueKind.Tag
            && (value.TagNumber == CborTagNumbers.StringReference
                || value.TagNumber == CborTagNumbers.StringReferenceNamespace
                || value.TagNumber == CborTagNumbers.ShareableValue
                || value.TagNumber == CborTagNumbers.SharedValueReference);
        bool hasAnnotations = value.IsSharedValue
            || value.SharedValueIndex >= 0
            || value.ReferencedValue != null;
        if (Options.SharedValuePolicy == CborSharedValuePolicy.Disabled
            && hasAnnotations
            && !referenceTag)
        {
            return Fail(
                "CBOR_REFERENCE_GRAPH_DISABLED",
                "Sharing annotations require an enabled shared-value policy.",
                path
            );
        }
        bool isReference = value.Kind == CborValueKind.Tag
            && (value.TagNumber == CborTagNumbers.StringReference
                || value.TagNumber == CborTagNumbers.SharedValueReference);
        if (value.ReferencedValue != null && !isReference)
        {
            return Fail(
                "CBOR_REFERENCE_ANNOTATION_INVALID",
                "Only a reference tag can identify a referenced value.",
                path
            );
        }
        if (value.SharedValueIndex >= 0 && !value.IsSharedValue && !isReference)
        {
            return Fail(
                "CBOR_REFERENCE_ANNOTATION_INVALID",
                "A reference index requires a shared value or reference tag.",
                path
            );
        }
        return true;
    }

    internal int RegisterSharedValue(
        CborValue value,
        string path,
        bool allowsRepeatedIdentity)
    {
        int existingIndex = FindIdentity(SharedValues, value);
        if (existingIndex >= 0 && !allowsRepeatedIdentity)
        {
            Fail(
                "CBOR_SHAREABLE_VALUE_DUPLICATE",
                "A value cannot be registered as shareable more than once.",
                path
            );
            return -1;
        }
        if (SharedValues.Count >= Options.MaxSharedValues)
        {
            Fail(
                "CBOR_MAX_SHARED_VALUES",
                "The value exceeds the configured shared-value limit.",
                path
            );
            return -1;
        }
        int index = SharedValues.Count;
        if (value.SharedValueIndex >= 0 && value.SharedValueIndex != index)
        {
            Fail(
                "CBOR_SHARED_VALUE_INDEX_MISMATCH",
                "The annotated shared-value index does not match encounter order.",
                path
            );
            return -1;
        }
        SharedValues.Add(value);
        return index;
    }

    internal int ResolveReferenceIndex(
        CborValue value,
        List<CborValue> table,
        string typeErrorCode,
        string rangeErrorCode,
        string missingErrorCode,
        string path)
    {
        int encodedIndex = -1;
        if (value.TaggedValue != null)
        {
            encodedIndex = ReadReferenceIndex(
                value.TaggedValue,
                typeErrorCode,
                rangeErrorCode,
                path
            );
            if (encodedIndex < 0)
            {
                return -1;
            }
        }
        else if (Options.SharedValuePolicy != CborSharedValuePolicy.Resolve)
        {
            Fail(
                typeErrorCode,
                "A reference tag requires an unsigned integer index.",
                path
            );
            return -1;
        }

        int targetIndex = -1;
        if (value.ReferencedValue != null)
        {
            targetIndex = FindIdentity(table, value.ReferencedValue);
            if (targetIndex < 0)
            {
                Fail(
                    missingErrorCode,
                    "The reference target has not been encountered in the active table.",
                    path
                );
                return -1;
            }
        }

        int index = targetIndex;
        if (index < 0)
        {
            index = encodedIndex;
        }
        if (index < 0)
        {
            index = value.SharedValueIndex;
        }
        if (index < 0 || index >= table.Count)
        {
            Fail(
                missingErrorCode,
                "The reference does not identify a previously encountered value.",
                path
            );
            return -1;
        }
        if ((encodedIndex >= 0 && encodedIndex != index)
            || (value.SharedValueIndex >= 0
                && value.SharedValueIndex != index))
        {
            Fail(
                "CBOR_REFERENCE_INDEX_MISMATCH",
                "The encoded and annotated reference indexes do not match encounter order.",
                path
            );
            return -1;
        }
        return index;
    }

    internal int ReadReferenceIndex(
        CborValue value,
        string typeErrorCode,
        string rangeErrorCode,
        string path)
    {
        if (value.Kind != CborValueKind.UnsignedInteger)
        {
            Fail(
                typeErrorCode,
                "A reference tag requires an unsigned integer index.",
                path
            );
            return -1;
        }
        if (value.IntegerValue < System.Numerics.BigInteger.Zero
            || value.IntegerValue > new System.Numerics.BigInteger(int.MaxValue))
        {
            Fail(
                rangeErrorCode,
                "The reference index exceeds the supported range.",
                path
            );
            return -1;
        }
        return (int)value.IntegerValue;
    }

    internal static int FindIdentity(List<CborValue> values, CborValue value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (object.ReferenceEquals(values[index], value))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal void RegisterString(CborValue value)
    {
        if (StringNamespaces.Count == 0)
        {
            return;
        }
        int byteLength;
        if (value.Kind == CborValueKind.ByteString)
        {
            byteLength = value.ByteStringValue == null
                ? 0
                : value.ByteStringValue.Length;
        }
        else
        {
            byteLength = value.TextStringValue == null
                ? 0
                : Encoding.UTF8.GetByteCount(value.TextStringValue);
        }
        List<CborValue> strings = StringNamespaces[StringNamespaces.Count - 1];
        int minimumLength = CborSharingDecoder.GetStringMinimumLength(strings.Count);
        if (byteLength >= minimumLength)
        {
            strings.Add(value);
        }
    }

    internal bool AddSharedReference(int index, string path, int depth)
    {
        if (!EnterSyntheticTag(path, depth)
            || !AddToken(new CborToken
            {
                Kind = CborTokenKind.Tag,
                TagNumber = CborTagNumbers.SharedValueReference
            }, path))
        {
            return false;
        }
        return AddReferenceIndex(index, $"{path}<reference>");
    }

    internal bool AddReferenceIndex(int index, string path)
    {
        if (!EnterSyntheticValue(path))
        {
            return false;
        }
        return AddToken(new CborToken
        {
            Kind = CborTokenKind.UnsignedInteger,
            IntegerValue = new System.Numerics.BigInteger(index)
        }, path);
    }

    internal bool EnterSyntheticTag(string path, int depth)
    {
        if (!EnterCompound(depth, path))
        {
            return false;
        }
        return EnterSyntheticValue(path);
    }

    internal bool EnterSyntheticValue(string path)
    {
        if (Options.MaxTotalItems < 0 || TotalItems >= Options.MaxTotalItems)
        {
            return Fail(
                "CBOR_MAX_TOTAL_ITEMS",
                "The value exceeds the configured item limit.",
                path
            );
        }
        TotalItems = TotalItems + 1;
        return true;
    }

    internal bool EnterValue(CborValue value, string path)
    {
        int index = 0;
        while (index < Ancestors.Count)
        {
            if (object.ReferenceEquals(Ancestors[index], value))
            {
                return Fail(
                    "CBOR_CYCLIC_VALUE_UNSUPPORTED",
                    "Cyclic values require sharing-aware encoding.",
                    path
                );
            }
            index = index + 1;
        }
        if (Options.MaxTotalItems < 0 || TotalItems >= Options.MaxTotalItems)
        {
            return Fail(
                "CBOR_MAX_TOTAL_ITEMS",
                "The value exceeds the configured item limit.",
                path
            );
        }
        TotalItems = TotalItems + 1;
        Ancestors.Add(value);
        return true;
    }

    internal bool EnterCompound(int depth, string path)
    {
        if (Options.MaxNestingDepth < 0 || depth >= Options.MaxNestingDepth)
        {
            return Fail(
                "CBOR_MAX_NESTING_DEPTH",
                "The value exceeds the configured nesting-depth limit.",
                path
            );
        }
        return true;
    }

    internal void LeaveValue()
    {
        Ancestors.RemoveAt(Ancestors.Count - 1);
    }

    internal CborLengthKind SelectLengthKind(CborValue value, string path)
    {
        CborLengthKind lengthKind = value.LengthKind;
        if (PreservesRepresentation()
            && value.Representation != null
            && value.Representation.LengthKind != CborLengthKind.None)
        {
            if (lengthKind != CborLengthKind.None
                && lengthKind != value.Representation.LengthKind)
            {
                Fail(
                    "CBOR_REPRESENTATION_LENGTH_KIND_MISMATCH",
                    "The retained length form no longer matches the value.",
                    path
                );
                return CborLengthKind.None;
            }
            lengthKind = value.Representation.LengthKind;
        }
        if (lengthKind != CborLengthKind.None
            && lengthKind != CborLengthKind.Definite
            && lengthKind != CborLengthKind.Indefinite)
        {
            Fail(
                "CBOR_LENGTH_KIND_INVALID",
                "The value length kind is not valid for encoding.",
                path
            );
            return CborLengthKind.None;
        }
        if (UsesDeterministicProfile())
        {
            return CborLengthKind.Definite;
        }
        if (Options.EncodingProfile == CborEncodingProfile.Preferred
            && (!Options.PreserveRepresentation || value.Representation == null))
        {
            return CborLengthKind.Definite;
        }
        if (lengthKind == CborLengthKind.None
            || lengthKind == CborLengthKind.Definite)
        {
            return CborLengthKind.Definite;
        }
        if (!Options.AllowIndefiniteLength)
        {
            Fail(
                "CBOR_INDEFINITE_LENGTH_DISABLED",
                "Indefinite-length output is disabled by the encoding options.",
                path
            );
            return CborLengthKind.None;
        }
        return CborLengthKind.Indefinite;
    }

    internal bool AddToken(CborToken token, string path)
    {
        CborTokenWritePlan plan = CborTokenWritePlan.Create(token);
        if (PreservesRepresentation())
        {
            plan.ApplyRepresentation();
        }
        if (plan.ErrorCode != null)
        {
            return Fail(
                plan.ErrorCode,
                plan.ErrorMessage ?? "The value cannot be encoded.",
                path
            );
        }
        int required = plan.Required;
        if (plan.IsEnd && token.LengthKind == CborLengthKind.Indefinite)
        {
            required = 1;
        }
        if (Options.MaxOutputBytes < 0
            || RequiredBytes > Options.MaxOutputBytes - required)
        {
            return Fail(
                "CBOR_MAX_OUTPUT_BYTES",
                "The value exceeds the configured output-byte limit.",
                path
            );
        }
        if (RequiredBytes > int.MaxValue - required)
        {
            return Fail(
                "CBOR_OUTPUT_SIZE_OUT_OF_RANGE",
                "The encoded value is too large for a byte-array result.",
                path
            );
        }
        RequiredBytes = RequiredBytes + required;
        Tokens.Add(token);
        return true;
    }

    internal bool Fail(string errorCode, string errorMessage, string errorPath)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ErrorPath = errorPath;
        return false;
    }
}
