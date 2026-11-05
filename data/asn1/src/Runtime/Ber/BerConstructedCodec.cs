namespace asn1;

internal class BerConstructedCodec
{
    internal AsnResourceLimits Limits;

    internal BerConstructedCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal BerConstructedResult EncodeSequenceValue(
        BerComponentValue[] components,
        AsnEncodingRules rules,
        bool indefinite)
    {
        BerConstructedResult selected = this.SelectComponents(
            components,
            rules);
        if (!selected.Success)
        {
            return selected;
        }

        return this.CreateCollectionRoot(
            AsnUniversalTags.Sequence,
            selected.Children,
            rules,
            indefinite);
    }

    internal BerConstructedResult EncodeSetValue(
        BerComponentValue[] components,
        AsnEncodingRules rules,
        bool indefinite)
    {
        BerConstructedResult selected = this.SelectComponents(
            components,
            rules);
        if (!selected.Success)
        {
            return selected;
        }

        if (rules == AsnEncodingRules.Der
            || rules == AsnEncodingRules.Cer)
        {
            BerConstructedResult sorted = this.SortSetComponentsByTag(
                selected.Children);
            if (!sorted.Success)
            {
                return sorted;
            }

            selected.Children = sorted.Children;
        }

        return this.CreateCollectionRoot(
            AsnUniversalTags.Set,
            selected.Children,
            rules,
            indefinite);
    }

    internal BerConstructedResult EncodeSequenceOfValue(
        BerTlv[] elements,
        AsnEncodingRules rules,
        bool indefinite)
    {
        BerConstructedResult validated = this.ValidateElements(elements);
        if (!validated.Success)
        {
            return validated;
        }

        return this.CreateCollectionRoot(
            AsnUniversalTags.SequenceOf,
            validated.Children,
            rules,
            indefinite);
    }

    internal BerConstructedResult EncodeSetOfValue(
        BerTlv[] elements,
        AsnEncodingRules rules,
        bool indefinite)
    {
        BerConstructedResult validated = this.ValidateElements(elements);
        if (!validated.Success)
        {
            return validated;
        }

        if (rules == AsnEncodingRules.Der
            || rules == AsnEncodingRules.Cer)
        {
            BerConstructedResult sorted = this.SortSetOfElements(
                validated.Children,
                rules);
            if (!sorted.Success)
            {
                return sorted;
            }

            validated.Children = sorted.Children;
        }

        return this.CreateCollectionRoot(
            AsnUniversalTags.SetOf,
            validated.Children,
            rules,
            indefinite);
    }

    internal BerConstructedResult EncodeChoiceValue(
        BerComponentValue[] alternatives)
    {
        if (alternatives == null)
        {
            throw new System.ArgumentNullException("alternatives");
        }

        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            alternatives.Length))
        {
            return this.CreateConstructedError(
                "ASNR0011",
                "The CHOICE alternative list exceeds the configured element limit.",
                true);
        }

        BerTlv? selected = null;
        int index = 0;
        while (index < alternatives.Length)
        {
            BerComponentValue alternative = alternatives[index];
            if (alternative == null)
            {
                return this.CreateConstructedError(
                    "ASNE0054",
                    "The CHOICE alternative list contains a null entry.",
                    false);
            }

            if (alternative.Present)
            {
                if (selected != null || alternative.Tlv == null
                    || !alternative.Tlv.IsValid())
                {
                    return this.CreateConstructedError(
                        "ASNE0055",
                        "CHOICE requires exactly one valid selected alternative.",
                        false);
                }

                selected = alternative.Tlv;
            }

            index = index + 1;
        }

        if (selected == null)
        {
            return this.CreateConstructedError(
                "ASNE0055",
                "CHOICE requires exactly one selected alternative.",
                false);
        }

        return new BerConstructedResult
        {
            Success = true,
            Tlv = this.CloneTlvTree(selected)
        };
    }

    internal BerConstructedResult ReadSequenceValue(
        BerTlv tlv,
        AsnEncodingRules rules)
    {
        return this.ReadCollectionRoot(
            tlv,
            rules,
            AsnUniversalTags.Sequence,
            "SEQUENCE");
    }

    internal BerConstructedResult ReadSetValue(
        BerTlv tlv,
        AsnEncodingRules rules)
    {
        return this.ReadCollectionRoot(
            tlv,
            rules,
            AsnUniversalTags.Set,
            "SET");
    }

    internal BerConstructedResult ReadSequenceOfValue(
        BerTlv tlv,
        AsnEncodingRules rules)
    {
        return this.ReadCollectionRoot(
            tlv,
            rules,
            AsnUniversalTags.SequenceOf,
            "SEQUENCE OF");
    }

    internal BerConstructedResult ReadSetOfValue(
        BerTlv tlv,
        AsnEncodingRules rules)
    {
        return this.ReadCollectionRoot(
            tlv,
            rules,
            AsnUniversalTags.SetOf,
            "SET OF");
    }

    internal BerConstructedResult ApplyTagChain(
        BerTlv value,
        AsnTagDescriptor[] tags)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (tags == null)
        {
            throw new System.ArgumentNullException("tags");
        }

        if (!value.IsValid())
        {
            return this.CreateConstructedError(
                "ASNE0056",
                "A tag chain cannot be applied to an invalid TLV tree.",
                false);
        }

        int explicitTags = this.CountExplicitTags(tags);
        if (!this.Limits.Unlimited
            && explicitTags > this.Limits.MaximumNestingDepth)
        {
            return this.CreateConstructedError(
                "ASNR0002",
                "The tag chain exceeds the configured nesting limit.",
                true);
        }

        BerTlv current = this.CloneTlvTree(value);
        int index = tags.Length - 1;
        while (index >= 0)
        {
            AsnTagDescriptor tag = tags[index];
            if (tag == null
                || tag.Number < System.Numerics.BigInteger.Zero)
            {
                return this.CreateConstructedError(
                    "ASNE0057",
                    "The tag chain contains an invalid tag.",
                    false);
            }

            if (tag.Mode == AsnTaggingMode.Explicit)
            {
                current = new BerTlv
                {
                    Tag = this.CreateTag(tag.Class, tag.Number),
                    Constructed = true,
                    Children = [current]
                };
            }
            else if (current.Tag.Class != tag.Class
                || current.Tag.Number != tag.Number)
            {
                current.Tag = this.CreateTag(tag.Class, tag.Number);
                current.UseEncodedData = false;
            }

            index = index - 1;
        }

        return new BerConstructedResult
        {
            Success = true,
            Tlv = current
        };
    }

    internal BerConstructedResult RemoveTagChain(
        BerTlv value,
        AsnTagDescriptor[] tags)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (tags == null)
        {
            throw new System.ArgumentNullException("tags");
        }

        if (!value.IsValid())
        {
            return this.CreateConstructedError(
                "ASND0086",
                "A tag chain cannot be removed from an invalid TLV tree.",
                false);
        }

        int explicitTags = this.CountExplicitTags(tags);
        if (!this.Limits.Unlimited
            && explicitTags > this.Limits.MaximumNestingDepth)
        {
            return this.CreateConstructedError(
                "ASNR0002",
                "The tag chain exceeds the configured nesting limit.",
                true);
        }

        BerTlv current = this.CloneTlvTree(value);
        int index = 0;
        while (index < tags.Length)
        {
            AsnTagDescriptor tag = tags[index];
            if (tag == null
                || tag.Number < System.Numerics.BigInteger.Zero)
            {
                return this.CreateConstructedError(
                    "ASND0087",
                    "The tag chain contains an invalid tag.",
                    false);
            }

            if (current.Tag.Class != tag.Class
                || current.Tag.Number != tag.Number)
            {
                return this.CreateConstructedError(
                    "ASND0088",
                    "The encoded value does not match the expected tag chain.",
                    false);
            }

            if (tag.Mode == AsnTaggingMode.Explicit)
            {
                if (!current.Constructed
                    || current.Children.Length != 1
                    || current.Content.Length != 0)
                {
                    return this.CreateConstructedError(
                        "ASND0089",
                        "An explicit tag must contain exactly one child value.",
                        false);
                }

                current = this.CloneTlvTree(current.Children[0]);
            }
            else if (index + 1 < tags.Length)
            {
                AsnTagDescriptor inner = tags[index + 1];
                if (inner == null
                    || inner.Number < System.Numerics.BigInteger.Zero)
                {
                    return this.CreateConstructedError(
                        "ASND0087",
                        "The tag chain contains an invalid tag.",
                        false);
                }

                current.Tag = this.CreateTag(inner.Class, inner.Number);
            }

            index = index + 1;
        }

        return new BerConstructedResult
        {
            Success = true,
            Tlv = current
        };
    }

    internal BerConstructedResult MakeIndefiniteRecursively(
        BerTlv value,
        AsnEncodingRules rules)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (rules == AsnEncodingRules.Der)
        {
            return this.CreateConstructedError(
                "ASNE0058",
                "DER does not permit indefinite-length encodings.",
                false);
        }

        if (rules != AsnEncodingRules.Ber && rules != AsnEncodingRules.Cer)
        {
            return this.CreateConstructedError(
                "ASNE0059",
                "Indefinite BER recursion requires BER or CER rules.",
                false);
        }

        ResourceCounter resources = new ResourceCounter(this.Limits);
        BerTlv? clone = this.CloneIndefiniteTree(value, resources);
        if (clone == null)
        {
            return this.CreateConstructedError(
                "ASNR0002",
                "The TLV tree exceeds the configured nesting or element limit.",
                true);
        }

        return new BerConstructedResult
        {
            Success = true,
            Tlv = clone
        };
    }

    internal BerConstructedResult SelectComponents(
        BerComponentValue[] components,
        AsnEncodingRules rules)
    {
        if (components == null)
        {
            throw new System.ArgumentNullException("components");
        }

        if (!this.IsBerFamilyRule(rules))
        {
            return this.CreateConstructedError(
                "ASNE0060",
                "Constructed BER values require BER, DER, or CER rules.",
                false);
        }

        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            components.Length))
        {
            return this.CreateConstructedError(
                "ASNR0011",
                "The component list exceeds the configured element limit.",
                true);
        }

        System.Collections.Generic.List<BerTlv> selected =
            new System.Collections.Generic.List<BerTlv>();
        int index = 0;
        while (index < components.Length)
        {
            BerComponentValue component = components[index];
            if (component == null || component.Field == null)
            {
                return this.CreateConstructedError(
                    "ASNE0061",
                    "The component list contains an entry without field metadata.",
                    false);
            }

            if (!component.Present)
            {
                if (!component.Field.Optional
                    && !component.Field.HasDefaultValue
                    && !component.Field.Extension)
                {
                    return this.CreateConstructedError(
                        "ASNE0062",
                        "A required component is absent: "
                            + component.Field.Name,
                        false);
                }

                index = index + 1;
                continue;
            }

            if (component.Tlv == null || !component.Tlv.IsValid())
            {
                return this.CreateConstructedError(
                    "ASNE0063",
                    "A present component has no valid TLV: "
                        + component.Field.Name,
                    false);
            }

            bool canonicalDefault = component.Field.HasDefaultValue
                && component.EqualsDefault
                && rules != AsnEncodingRules.Ber;
            if (!canonicalDefault)
            {
                selected.Add(this.CloneTlvTree(component.Tlv));
            }

            index = index + 1;
        }

        return new BerConstructedResult
        {
            Success = true,
            Children = selected.ToArray()
        };
    }

    internal BerConstructedResult ValidateElements(BerTlv[] elements)
    {
        if (elements == null)
        {
            throw new System.ArgumentNullException("elements");
        }

        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            elements.Length))
        {
            return this.CreateConstructedError(
                "ASNR0011",
                "The collection exceeds the configured element limit.",
                true);
        }

        BerTlv[] clones = new BerTlv[elements.Length];
        int index = 0;
        while (index < elements.Length)
        {
            if (elements[index] == null || !elements[index].IsValid())
            {
                return this.CreateConstructedError(
                    "ASNE0064",
                    "The collection contains an invalid element TLV.",
                    false);
            }

            clones[index] = this.CloneTlvTree(elements[index]);
            index = index + 1;
        }

        return new BerConstructedResult
        {
            Success = true,
            Children = clones
        };
    }

    internal BerConstructedResult SortSetComponentsByTag(BerTlv[] children)
    {
        long orderingBytes = (long)children.Length * 4;
        if (!new ResourceCounter(this.Limits).TryChargeAllocationBytes(
            orderingBytes))
        {
            return this.CreateConstructedError(
                "ASNR0005",
                "Canonical SET ordering exceeds the configured total allocation limit.",
                true);
        }

        int[] originalOrder = new int[children.Length];
        int index = 0;
        while (index < originalOrder.Length)
        {
            originalOrder[index] = index;
            index = index + 1;
        }

        int root = children.Length / 2 - 1;
        while (root >= 0)
        {
            this.SiftTagHeap(
                children,
                originalOrder,
                children.Length,
                root);
            root = root - 1;
        }

        int end = children.Length - 1;
        while (end > 0)
        {
            BerTlv temporary = children[0];
            children[0] = children[end];
            children[end] = temporary;
            int order = originalOrder[0];
            originalOrder[0] = originalOrder[end];
            originalOrder[end] = order;
            this.SiftTagHeap(children, originalOrder, end, 0);
            end = end - 1;
        }

        return new BerConstructedResult
        {
            Success = true,
            Children = children
        };
    }

    internal BerConstructedResult SortSetOfElements(
        BerTlv[] elements,
        AsnEncodingRules rules)
    {
        ResourceCounter resources = new ResourceCounter(this.Limits);
        long orderingBytes = (long)elements.Length * 8;
        if (!resources.TryChargeAllocationBytes(orderingBytes))
        {
            return this.CreateConstructedError(
                "ASNR0005",
                "Canonical SET OF ordering exceeds the configured total allocation limit.",
                true);
        }

        byte[][] encodings = new byte[elements.Length][];
        int index = 0;
        while (index < elements.Length)
        {
            BerTlv sortable = elements[index];
            if (rules == AsnEncodingRules.Cer)
            {
                BerConstructedResult canonical = this.MakeIndefiniteRecursively(
                    elements[index],
                    AsnEncodingRules.Cer);
                if (!canonical.Success || canonical.Tlv == null)
                {
                    return canonical;
                }

                sortable = canonical.Tlv;
            }

            BerWriteResult written = new BerTlvWriterImplementation(
                this.Limits).Write(new BerWriteRequest
                {
                    Tlv = sortable,
                    EncodingRules = rules
                });
            if (!written.Success)
            {
                return this.CreateSetOfEncodingError(written, index);
            }

            if (!resources.TryChargeAllocationBytes(written.Data.Length))
            {
                return this.CreateConstructedError(
                    "ASNR0005",
                    "Canonical SET OF ordering exceeds the configured total allocation limit.",
                    true);
            }

            encodings[index] = written.Data;
            index = index + 1;
        }

        this.SortEncodedElements(elements, encodings);
        return new BerConstructedResult
        {
            Success = true,
            Children = elements
        };
    }

    internal void SiftTagHeap(
        BerTlv[] children,
        int[] originalOrder,
        int count,
        int root)
    {
        int current = root;
        while (current < count / 2)
        {
            int child = current * 2 + 1;
            if (child + 1 < count
                && this.CompareSetComponents(
                    children[child].Tag,
                    originalOrder[child],
                    children[child + 1].Tag,
                    originalOrder[child + 1]) < 0)
            {
                child = child + 1;
            }

            if (this.CompareSetComponents(
                children[current].Tag,
                originalOrder[current],
                children[child].Tag,
                originalOrder[child]) >= 0)
            {
                return;
            }

            BerTlv temporary = children[current];
            children[current] = children[child];
            children[child] = temporary;
            int order = originalOrder[current];
            originalOrder[current] = originalOrder[child];
            originalOrder[child] = order;
            current = child;
        }
    }

    internal int CompareSetComponents(
        AsnTag left,
        int leftOrder,
        AsnTag right,
        int rightOrder)
    {
        int comparison = this.CompareTags(left, right);
        if (comparison != 0)
        {
            return comparison;
        }

        return leftOrder - rightOrder;
    }

    internal void SortEncodedElements(BerTlv[] elements, byte[][] encodings)
    {
        int root = elements.Length / 2 - 1;
        while (root >= 0)
        {
            this.SiftEncodingHeap(
                elements,
                encodings,
                elements.Length,
                root);
            root = root - 1;
        }

        int end = elements.Length - 1;
        while (end > 0)
        {
            this.SwapEncodedElements(elements, encodings, 0, end);
            this.SiftEncodingHeap(elements, encodings, end, 0);
            end = end - 1;
        }
    }

    internal void SiftEncodingHeap(
        BerTlv[] elements,
        byte[][] encodings,
        int count,
        int root)
    {
        int current = root;
        while (current < count / 2)
        {
            int child = current * 2 + 1;
            if (child + 1 < count
                && this.CompareEncodings(
                    encodings[child],
                    encodings[child + 1]) < 0)
            {
                child = child + 1;
            }

            if (this.CompareEncodings(
                encodings[current],
                encodings[child]) >= 0)
            {
                return;
            }

            this.SwapEncodedElements(
                elements,
                encodings,
                current,
                child);
            current = child;
        }
    }

    internal void SwapEncodedElements(
        BerTlv[] elements,
        byte[][] encodings,
        int left,
        int right)
    {
        BerTlv element = elements[left];
        elements[left] = elements[right];
        elements[right] = element;
        byte[] encoding = encodings[left];
        encodings[left] = encodings[right];
        encodings[right] = encoding;
    }

    internal int CompareTags(AsnTag left, AsnTag right)
    {
        int classComparison = (int)left.Class - (int)right.Class;
        if (classComparison != 0)
        {
            return classComparison;
        }

        return left.Number.CompareTo(right.Number);
    }

    internal int CompareEncodings(byte[] left, byte[] right)
    {
        int count = left.Length < right.Length ? left.Length : right.Length;
        int index = 0;
        while (index < count)
        {
            if (left[index] != right[index])
            {
                return left[index] < right[index] ? -1 : 1;
            }

            index = index + 1;
        }

        if (left.Length == right.Length)
        {
            return 0;
        }

        return left.Length < right.Length ? -1 : 1;
    }

    internal BerConstructedResult CreateSetOfEncodingError(
        BerWriteResult written,
        int index)
    {
        if (written.Diagnostics.Length == 0)
        {
            return this.CreateConstructedError(
                "ASNE0104",
                "Canonical SET OF element "
                    + index
                    + " could not be encoded for canonical ordering.",
                false);
        }

        AsnDiagnostic diagnostic = written.Diagnostics[0];
        return this.CreateConstructedError(
            diagnostic.Code,
            "Canonical SET OF element "
                + index
                + " could not be encoded for canonical ordering: "
                + diagnostic.Message,
            diagnostic.Category == AsnDiagnosticCategory.Resource);
    }

    internal BerConstructedResult CreateCollectionRoot(
        int tagNumber,
        BerTlv[] children,
        AsnEncodingRules rules,
        bool indefinite)
    {
        if (!this.IsBerFamilyRule(rules))
        {
            return this.CreateConstructedError(
                "ASNE0060",
                "Constructed BER values require BER, DER, or CER rules.",
                false);
        }

        if (indefinite && rules == AsnEncodingRules.Der)
        {
            return this.CreateConstructedError(
                "ASNE0058",
                "DER does not permit indefinite-length encodings.",
                false);
        }

        return new BerConstructedResult
        {
            Success = true,
            Tlv = new BerTlv
            {
                Tag = this.CreateTag(
                    AsnTagClass.Universal,
                    new System.Numerics.BigInteger(tagNumber)),
                Constructed = true,
                IndefiniteLength = rules == AsnEncodingRules.Cer
                    || indefinite,
                Children = children
            }
        };
    }

    internal BerConstructedResult ReadCollectionRoot(
        BerTlv tlv,
        AsnEncodingRules rules,
        int tagNumber,
        string typeName)
    {
        if (tlv == null)
        {
            throw new System.ArgumentNullException("tlv");
        }

        if (!this.IsBerFamilyRule(rules)
            || !tlv.IsValid()
            || tlv.Tag.Class != AsnTagClass.Universal
            || tlv.Tag.Number != new System.Numerics.BigInteger(tagNumber)
            || !tlv.Constructed
            || rules == AsnEncodingRules.Der && tlv.IndefiniteLength
            || rules == AsnEncodingRules.Cer && !tlv.IndefiniteLength)
        {
            return this.CreateConstructedError(
                "ASND0078",
                typeName + " has an invalid BER tag, form, or length mode.",
                false);
        }

        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            tlv.Children.Length))
        {
            return this.CreateConstructedError(
                "ASNR0011",
                typeName + " exceeds the configured element limit.",
                true);
        }

        BerTlv[] children = new BerTlv[tlv.Children.Length];
        int index = 0;
        while (index < children.Length)
        {
            children[index] = this.CloneTlvTree(tlv.Children[index]);
            index = index + 1;
        }

        return new BerConstructedResult
        {
            Success = true,
            Tlv = this.CloneTlvTree(tlv),
            Children = children
        };
    }

    internal BerTlv? CloneIndefiniteTree(
        BerTlv value,
        ResourceCounter resources)
    {
        if (value == null || !value.IsValid())
        {
            return null;
        }

        if (value.UseEncodedData)
        {
            return this.CloneTlvTree(value);
        }

        bool entered = value.Constructed;
        if (entered && !resources.TryEnterNesting())
        {
            return null;
        }

        if (!resources.AllowsCollectionElements(value.Children.Length))
        {
            if (entered)
            {
                resources.LeaveNesting();
            }

            return null;
        }

        BerTlv clone = new BerTlv
        {
            EncodedOffset = value.EncodedOffset,
            Tag = this.CreateTag(value.Tag.Class, value.Tag.Number),
            Constructed = value.Constructed,
            IndefiniteLength = value.Constructed,
            Content = this.CopyBytes(value.Content),
            EncodedData = this.CopyBytes(value.EncodedData),
            Children = new BerTlv[value.Children.Length]
        };
        int index = 0;
        while (index < value.Children.Length)
        {
            BerTlv? child = this.CloneIndefiniteTree(
                value.Children[index],
                resources);
            if (child == null)
            {
                if (entered)
                {
                    resources.LeaveNesting();
                }

                return null;
            }

            clone.Children[index] = child;
            index = index + 1;
        }

        if (entered)
        {
            resources.LeaveNesting();
        }

        return clone;
    }

    internal int CountExplicitTags(AsnTagDescriptor[] tags)
    {
        int count = 0;
        int index = 0;
        while (index < tags.Length)
        {
            if (tags[index] != null
                && tags[index].Mode == AsnTaggingMode.Explicit)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal BerTlv CloneTlvTree(BerTlv value)
    {
        BerTlv[] children = new BerTlv[value.Children.Length];
        int index = 0;
        while (index < children.Length)
        {
            children[index] = this.CloneTlvTree(value.Children[index]);
            index = index + 1;
        }

        return new BerTlv
        {
            EncodedOffset = value.EncodedOffset,
            UseEncodedData = value.UseEncodedData,
            Tag = this.CreateTag(value.Tag.Class, value.Tag.Number),
            Constructed = value.Constructed,
            IndefiniteLength = value.IndefiniteLength,
            Content = this.CopyBytes(value.Content),
            Children = children,
            EncodedData = this.CopyBytes(value.EncodedData)
        };
    }

    internal AsnTag CreateTag(
        AsnTagClass tagClass,
        System.Numerics.BigInteger number)
    {
        return new AsnTag
        {
            Class = tagClass,
            Number = number
        };
    }

    internal byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        System.Buffer.BlockCopy(source, 0, copy, 0, source.Length);
        return copy;
    }

    internal bool IsBerFamilyRule(AsnEncodingRules rules)
    {
        return rules == AsnEncodingRules.Ber
            || rules == AsnEncodingRules.Der
            || rules == AsnEncodingRules.Cer;
    }

    internal BerConstructedResult CreateConstructedError(
        string code,
        string message,
        bool resource)
    {
        return new BerConstructedResult
        {
            LimitExceeded = resource,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
