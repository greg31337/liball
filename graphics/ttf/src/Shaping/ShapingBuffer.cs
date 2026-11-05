namespace ttf;

internal enum ShapingAttachmentType
{
    None = 0,
    Mark = 1,
    Cursive = 2
}

internal class ShapingBuffer
{
    System.Text.Rune[] runes;
    int[] glyphIds;
    int[] clusterStarts;
    int[] clusterLengths;
    ulong[] masks;
    int[] syllables;
    int[] ligatureIds;
    int[] ligatureComponentCounts;
    int[] ligatureComponentIndexes;
    int[] attachmentParents;
    ShapingAttachmentType[] attachmentTypes;
    bool[] defaultIgnorables;
    bool[] unsafeToBreak;
    int sourceLength;
    ShapingDirection direction;

    internal ShapingBuffer(
        RuneBuffer input,
        ShapingDirection direction)
    {
        if (input == null)
        {
            throw new System.ArgumentNullException("input");
        }

        ValidateDirection(direction);
        int count = input.GetCount();
        runes = new System.Text.Rune[count];
        glyphIds = new int[count];
        clusterStarts = new int[count];
        clusterLengths = new int[count];
        masks = new ulong[count];
        syllables = new int[count];
        ligatureIds = new int[count];
        ligatureComponentCounts = new int[count];
        ligatureComponentIndexes = new int[count];
        attachmentParents = new int[count];
        attachmentTypes = new ShapingAttachmentType[count];
        defaultIgnorables = new bool[count];
        unsafeToBreak = new bool[count];
        sourceLength = input.GetSourceLength();
        this.direction = direction;

        if (sourceLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("input");
        }

        int index = 0;
        while (index < count)
        {
            int clusterStart = input.GetUtf16Start(index);
            int clusterLength = input.GetUtf16Length(index);
            ValidateCluster(clusterStart, clusterLength, sourceLength);
            runes[index] = input.GetRune(index);
            glyphIds[index] = -1;
            clusterStarts[index] = clusterStart;
            clusterLengths[index] = clusterLength;
            ligatureComponentCounts[index] = 1;
            attachmentParents[index] = -1;
            index++;
        }
    }

    internal int GetCount()
    {
        return runes.Length;
    }

    internal int GetSourceLength()
    {
        return sourceLength;
    }

    internal ShapingDirection GetDirection()
    {
        return direction;
    }

    internal void SetDirection(ShapingDirection value)
    {
        ValidateDirection(value);
        direction = value;
    }

    internal bool IsHorizontal()
    {
        return direction == ShapingDirection.LeftToRight ||
            direction == ShapingDirection.RightToLeft;
    }

    internal bool IsForward()
    {
        return direction == ShapingDirection.LeftToRight ||
            direction == ShapingDirection.TopToBottom;
    }

    internal System.Text.Rune GetRune(int index)
    {
        ValidateIndex(index);
        return runes[index];
    }

    internal int GetScalarValue(int index)
    {
        return GetRune(index).Value;
    }

    internal void SetRune(int index, System.Text.Rune value)
    {
        ValidateIndex(index);
        runes[index] = value;
    }

    internal bool HasGlyphId(int index)
    {
        ValidateIndex(index);
        return glyphIds[index] >= 0;
    }

    internal int GetGlyphId(int index)
    {
        ValidateIndex(index);
        return glyphIds[index];
    }

    internal void SetGlyphId(int index, int glyphId)
    {
        ValidateIndex(index);
        ValidateGlyphId(glyphId);
        glyphIds[index] = glyphId;
    }

    internal void ClearGlyphId(int index)
    {
        ValidateIndex(index);
        glyphIds[index] = -1;
    }

    internal int GetClusterStart(int index)
    {
        ValidateIndex(index);
        return clusterStarts[index];
    }

    internal int GetClusterLength(int index)
    {
        ValidateIndex(index);
        return clusterLengths[index];
    }

    internal int GetClusterEnd(int index)
    {
        return GetClusterStart(index) + GetClusterLength(index);
    }

    internal void SetCluster(int index, int start, int length)
    {
        ValidateIndex(index);
        ValidateCluster(start, length, sourceLength);
        clusterStarts[index] = start;
        clusterLengths[index] = length;
    }

    internal void MergeClusters(int startIndex, int endIndex)
    {
        ValidateRange(startIndex, endIndex);
        int mergedStart = clusterStarts[startIndex];
        int mergedEnd = GetClusterEnd(startIndex);
        int index = startIndex + 1;

        while (index < endIndex)
        {
            mergedStart = System.Math.Min(
                mergedStart,
                clusterStarts[index]);
            mergedEnd = System.Math.Max(
                mergedEnd,
                GetClusterEnd(index));
            index++;
        }

        index = startIndex;
        while (index < endIndex)
        {
            clusterStarts[index] = mergedStart;
            clusterLengths[index] = mergedEnd - mergedStart;
            index++;
        }
    }

    internal ulong GetMask(int index)
    {
        ValidateIndex(index);
        return masks[index];
    }

    internal void SetMask(int index, ulong mask)
    {
        ValidateIndex(index);
        masks[index] = mask;
    }

    internal void AddMask(int index, ulong mask)
    {
        ValidateIndex(index);
        masks[index] |= mask;
    }

    internal void RemoveMask(int index, ulong mask)
    {
        ValidateIndex(index);
        masks[index] &= ~mask;
    }

    internal int GetSyllable(int index)
    {
        ValidateIndex(index);
        return syllables[index];
    }

    internal void SetSyllable(int index, int syllable)
    {
        ValidateIndex(index);

        if (syllable < 0)
        {
            throw new System.ArgumentOutOfRangeException("syllable");
        }

        syllables[index] = syllable;
    }

    internal bool HasLigature(int index)
    {
        ValidateIndex(index);
        return ligatureIds[index] != 0;
    }

    internal int GetLigatureId(int index)
    {
        ValidateIndex(index);
        return ligatureIds[index];
    }

    internal int GetLigatureComponentCount(int index)
    {
        ValidateIndex(index);
        return ligatureComponentCounts[index];
    }

    internal int GetLigatureComponentIndex(int index)
    {
        ValidateIndex(index);
        return ligatureComponentIndexes[index];
    }

    internal void SetLigature(
        int index,
        int ligatureId,
        int componentCount,
        int componentIndex)
    {
        ValidateIndex(index);

        if (ligatureId <= 0)
        {
            throw new System.ArgumentOutOfRangeException("ligatureId");
        }

        if (componentCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("componentCount");
        }

        if (componentIndex < 0 || componentIndex >= componentCount)
        {
            throw new System.ArgumentOutOfRangeException("componentIndex");
        }

        ligatureIds[index] = ligatureId;
        ligatureComponentCounts[index] = componentCount;
        ligatureComponentIndexes[index] = componentIndex;
    }

    internal void ClearLigature(int index)
    {
        ValidateIndex(index);
        ligatureIds[index] = 0;
        ligatureComponentCounts[index] = 1;
        ligatureComponentIndexes[index] = 0;
    }

    internal ShapingAttachmentType GetAttachmentType(int index)
    {
        ValidateIndex(index);
        return attachmentTypes[index];
    }

    internal int GetAttachmentParent(int index)
    {
        ValidateIndex(index);
        return attachmentParents[index];
    }

    internal void SetAttachment(
        int index,
        int parentIndex,
        ShapingAttachmentType type)
    {
        ValidateIndex(index);
        ValidateIndex(parentIndex);

        if (type != ShapingAttachmentType.Mark &&
            type != ShapingAttachmentType.Cursive)
        {
            throw new System.ArgumentOutOfRangeException("type");
        }

        if (index == parentIndex)
        {
            throw new System.ArgumentException(
                "A shaping item cannot be attached to itself.");
        }

        int ancestorIndex = parentIndex;
        while (ancestorIndex >= 0)
        {
            if (ancestorIndex == index)
            {
                throw new System.ArgumentException(
                    "Shaping attachments cannot contain a cycle.");
            }

            ancestorIndex = attachmentParents[ancestorIndex];
        }

        attachmentParents[index] = parentIndex;
        attachmentTypes[index] = type;
    }

    internal void ClearAttachment(int index)
    {
        ValidateIndex(index);
        attachmentParents[index] = -1;
        attachmentTypes[index] = ShapingAttachmentType.None;
    }

    internal bool IsDefaultIgnorable(int index)
    {
        ValidateIndex(index);
        return defaultIgnorables[index];
    }

    internal void SetDefaultIgnorable(int index, bool value)
    {
        ValidateIndex(index);
        defaultIgnorables[index] = value;
    }

    internal bool IsUnsafeToBreak(int index)
    {
        ValidateIndex(index);
        return unsafeToBreak[index];
    }

    internal void SetUnsafeToBreak(int index, bool value)
    {
        ValidateIndex(index);
        unsafeToBreak[index] = value;
    }

    internal void Remove(int index)
    {
        ValidateIndex(index);
        bool[] removed = new bool[GetCount()];
        removed[index] = true;
        RemoveItems(removed);
    }

    internal void InsertRune(
        int index,
        System.Text.Rune value,
        int clusterSourceIndex)
    {
        if (index < 0 || index > GetCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        ValidateIndex(clusterSourceIndex);
        int clusterStart = clusterStarts[clusterSourceIndex];
        int clusterLength = clusterLengths[clusterSourceIndex];
        int oldCount = GetCount();
        int newCount = oldCount + 1;
        System.Text.Rune[] newRunes = new System.Text.Rune[newCount];
        int[] newGlyphIds = new int[newCount];
        int[] newClusterStarts = new int[newCount];
        int[] newClusterLengths = new int[newCount];
        ulong[] newMasks = new ulong[newCount];
        int[] newSyllables = new int[newCount];
        int[] newLigatureIds = new int[newCount];
        int[] newComponentCounts = new int[newCount];
        int[] newComponentIndexes = new int[newCount];
        int[] newAttachmentParents = new int[newCount];
        ShapingAttachmentType[] newAttachmentTypes =
            new ShapingAttachmentType[newCount];
        bool[] newDefaultIgnorables = new bool[newCount];
        bool[] newUnsafeToBreak = new bool[newCount];
        int newIndex = 0;

        while (newIndex < newCount)
        {
            if (newIndex == index)
            {
                newRunes[newIndex] = value;
                newGlyphIds[newIndex] = -1;
                newClusterStarts[newIndex] = clusterStart;
                newClusterLengths[newIndex] = clusterLength;
                newComponentCounts[newIndex] = 1;
                newAttachmentParents[newIndex] = -1;
            }
            else
            {
                int oldIndex = newIndex < index ?
                    newIndex : newIndex - 1;
                newRunes[newIndex] = runes[oldIndex];
                newGlyphIds[newIndex] = glyphIds[oldIndex];
                newClusterStarts[newIndex] = clusterStarts[oldIndex];
                newClusterLengths[newIndex] = clusterLengths[oldIndex];
                newMasks[newIndex] = masks[oldIndex];
                newSyllables[newIndex] = syllables[oldIndex];
                newLigatureIds[newIndex] = ligatureIds[oldIndex];
                newComponentCounts[newIndex] =
                    ligatureComponentCounts[oldIndex];
                newComponentIndexes[newIndex] =
                    ligatureComponentIndexes[oldIndex];
                int parentIndex = attachmentParents[oldIndex];
                newAttachmentParents[newIndex] = parentIndex < index ?
                    parentIndex : parentIndex + 1;
                newAttachmentTypes[newIndex] =
                    attachmentTypes[oldIndex];
                newDefaultIgnorables[newIndex] =
                    defaultIgnorables[oldIndex];
                newUnsafeToBreak[newIndex] = unsafeToBreak[oldIndex];
            }

            newIndex++;
        }

        runes = newRunes;
        glyphIds = newGlyphIds;
        clusterStarts = newClusterStarts;
        clusterLengths = newClusterLengths;
        masks = newMasks;
        syllables = newSyllables;
        ligatureIds = newLigatureIds;
        ligatureComponentCounts = newComponentCounts;
        ligatureComponentIndexes = newComponentIndexes;
        attachmentParents = newAttachmentParents;
        attachmentTypes = newAttachmentTypes;
        defaultIgnorables = newDefaultIgnorables;
        unsafeToBreak = newUnsafeToBreak;
    }

    internal void RemoveItems(bool[] removed)
    {
        if (removed == null)
        {
            throw new System.ArgumentNullException("removed");
        }

        int oldCount = GetCount();
        if (removed.Length != oldCount)
        {
            throw new System.ArgumentException(
                "Removal state must describe every shaping item.");
        }

        int removedCount = 0;
        int oldIndex = 0;
        while (oldIndex < oldCount)
        {
            if (removed[oldIndex])
            {
                removedCount++;
            }

            oldIndex++;
        }

        int newCount = oldCount - removedCount;
        System.Text.Rune[] newRunes = new System.Text.Rune[newCount];
        int[] newGlyphIds = new int[newCount];
        int[] newClusterStarts = new int[newCount];
        int[] newClusterLengths = new int[newCount];
        ulong[] newMasks = new ulong[newCount];
        int[] newSyllables = new int[newCount];
        int[] newLigatureIds = new int[newCount];
        int[] newComponentCounts = new int[newCount];
        int[] newComponentIndexes = new int[newCount];
        int[] newAttachmentParents = new int[newCount];
        ShapingAttachmentType[] newAttachmentTypes =
            new ShapingAttachmentType[newCount];
        bool[] newDefaultIgnorables = new bool[newCount];
        bool[] newUnsafeToBreak = new bool[newCount];
        int[] targetIndexes = new int[oldCount];
        oldIndex = 0;
        int newIndex = 0;

        while (oldIndex < oldCount)
        {
            targetIndexes[oldIndex] = removed[oldIndex] ? -1 : newIndex;
            if (!removed[oldIndex])
            {
                newIndex++;
            }

            oldIndex++;
        }

        oldIndex = 0;
        newIndex = 0;

        while (oldIndex < oldCount)
        {
            if (!removed[oldIndex])
            {
                newRunes[newIndex] = runes[oldIndex];
                newGlyphIds[newIndex] = glyphIds[oldIndex];
                newClusterStarts[newIndex] = clusterStarts[oldIndex];
                newClusterLengths[newIndex] = clusterLengths[oldIndex];
                newMasks[newIndex] = masks[oldIndex];
                newSyllables[newIndex] = syllables[oldIndex];
                newLigatureIds[newIndex] = ligatureIds[oldIndex];
                newComponentCounts[newIndex] =
                    ligatureComponentCounts[oldIndex];
                newComponentIndexes[newIndex] =
                    ligatureComponentIndexes[oldIndex];
                int parentIndex = attachmentParents[oldIndex];
                if (parentIndex >= 0 && targetIndexes[parentIndex] < 0)
                {
                    newAttachmentParents[newIndex] = -1;
                    newAttachmentTypes[newIndex] =
                        ShapingAttachmentType.None;
                }
                else
                {
                    newAttachmentParents[newIndex] = parentIndex < 0 ? -1 :
                        targetIndexes[parentIndex];
                    newAttachmentTypes[newIndex] =
                        attachmentTypes[oldIndex];
                }

                newDefaultIgnorables[newIndex] =
                    defaultIgnorables[oldIndex];
                newUnsafeToBreak[newIndex] = unsafeToBreak[oldIndex];
                newIndex++;
            }

            oldIndex++;
        }

        runes = newRunes;
        glyphIds = newGlyphIds;
        clusterStarts = newClusterStarts;
        clusterLengths = newClusterLengths;
        masks = newMasks;
        syllables = newSyllables;
        ligatureIds = newLigatureIds;
        ligatureComponentCounts = newComponentCounts;
        ligatureComponentIndexes = newComponentIndexes;
        attachmentParents = newAttachmentParents;
        attachmentTypes = newAttachmentTypes;
        defaultIgnorables = newDefaultIgnorables;
        unsafeToBreak = newUnsafeToBreak;
    }

    internal void Reorder(int[] sourceIndexes)
    {
        if (sourceIndexes == null)
        {
            throw new System.ArgumentNullException("sourceIndexes");
        }

        int count = GetCount();
        if (sourceIndexes.Length != count)
        {
            throw new System.ArgumentException(
                "A shaping permutation must include every item.");
        }

        bool[] used = new bool[count];
        int[] targetIndexes = new int[count];
        int index = 0;
        while (index < count)
        {
            int sourceIndex = sourceIndexes[index];
            if (sourceIndex < 0 || sourceIndex >= count ||
                used[sourceIndex])
            {
                throw new System.ArgumentException(
                    "A shaping permutation must contain unique indexes.");
            }

            used[sourceIndex] = true;
            targetIndexes[sourceIndex] = index;
            index++;
        }

        System.Text.Rune[] reorderedRunes =
            new System.Text.Rune[count];
        int[] reorderedGlyphIds = new int[count];
        int[] reorderedClusterStarts = new int[count];
        int[] reorderedClusterLengths = new int[count];
        ulong[] reorderedMasks = new ulong[count];
        int[] reorderedSyllables = new int[count];
        int[] reorderedLigatureIds = new int[count];
        int[] reorderedComponentCounts = new int[count];
        int[] reorderedComponentIndexes = new int[count];
        int[] reorderedAttachmentParents = new int[count];
        ShapingAttachmentType[] reorderedAttachmentTypes =
            new ShapingAttachmentType[count];
        bool[] reorderedDefaultIgnorables = new bool[count];
        bool[] reorderedUnsafeToBreak = new bool[count];

        index = 0;
        while (index < count)
        {
            int sourceIndex = sourceIndexes[index];
            reorderedRunes[index] = runes[sourceIndex];
            reorderedGlyphIds[index] = glyphIds[sourceIndex];
            reorderedClusterStarts[index] = clusterStarts[sourceIndex];
            reorderedClusterLengths[index] = clusterLengths[sourceIndex];
            reorderedMasks[index] = masks[sourceIndex];
            reorderedSyllables[index] = syllables[sourceIndex];
            reorderedLigatureIds[index] = ligatureIds[sourceIndex];
            reorderedComponentCounts[index] =
                ligatureComponentCounts[sourceIndex];
            reorderedComponentIndexes[index] =
                ligatureComponentIndexes[sourceIndex];
            int parentIndex = attachmentParents[sourceIndex];
            reorderedAttachmentParents[index] = parentIndex < 0 ? -1 :
                targetIndexes[parentIndex];
            reorderedAttachmentTypes[index] =
                attachmentTypes[sourceIndex];
            reorderedDefaultIgnorables[index] =
                defaultIgnorables[sourceIndex];
            reorderedUnsafeToBreak[index] = unsafeToBreak[sourceIndex];
            index++;
        }

        runes = reorderedRunes;
        glyphIds = reorderedGlyphIds;
        clusterStarts = reorderedClusterStarts;
        clusterLengths = reorderedClusterLengths;
        masks = reorderedMasks;
        syllables = reorderedSyllables;
        ligatureIds = reorderedLigatureIds;
        ligatureComponentCounts = reorderedComponentCounts;
        ligatureComponentIndexes = reorderedComponentIndexes;
        attachmentParents = reorderedAttachmentParents;
        attachmentTypes = reorderedAttachmentTypes;
        defaultIgnorables = reorderedDefaultIgnorables;
        unsafeToBreak = reorderedUnsafeToBreak;
    }

    internal void ReplaceLayoutGlyphs(SfntLayoutGlyphBuffer layoutBuffer)
    {
        if (layoutBuffer == null)
        {
            throw new System.ArgumentNullException("layoutBuffer");
        }

        int newCount = layoutBuffer.GetCount();
        System.Text.Rune[] newRunes = new System.Text.Rune[newCount];
        int[] newGlyphIds = new int[newCount];
        int[] newClusterStarts = new int[newCount];
        int[] newClusterLengths = new int[newCount];
        ulong[] newMasks = new ulong[newCount];
        int[] newSyllables = new int[newCount];
        int[] newLigatureIds = new int[newCount];
        int[] newComponentCounts = new int[newCount];
        int[] newComponentIndexes = new int[newCount];
        int[] newAttachmentParents = new int[newCount];
        ShapingAttachmentType[] newAttachmentTypes =
            new ShapingAttachmentType[newCount];
        bool[] newDefaultIgnorables = new bool[newCount];
        bool[] newUnsafeToBreak = new bool[newCount];
        int newIndex = 0;

        while (newIndex < newCount)
        {
            int start = layoutBuffer.GetClusterStart(newIndex);
            int length = layoutBuffer.GetClusterLength(newIndex);
            ValidateCluster(start, length, sourceLength);
            int end = start + length;
            int sourceIndex = FindLayoutSource(start, end);
            newRunes[newIndex] = runes[sourceIndex];
            newGlyphIds[newIndex] = layoutBuffer.GetGlyphId(newIndex);
            newClusterStarts[newIndex] = start;
            newClusterLengths[newIndex] = length;
            newComponentCounts[newIndex] =
                layoutBuffer.GetLigatureComponentCount(newIndex);
            newComponentIndexes[newIndex] =
                layoutBuffer.GetLigatureComponentIndex(newIndex);
            newAttachmentParents[newIndex] = -1;
            CopyLayoutSourceState(
                start,
                end,
                newIndex,
                newMasks,
                newSyllables,
                newDefaultIgnorables,
                newUnsafeToBreak);

            if (newComponentCounts[newIndex] > 1)
            {
                newLigatureIds[newIndex] = newIndex + 1;
            }

            newIndex += 1;
        }

        runes = newRunes;
        glyphIds = newGlyphIds;
        clusterStarts = newClusterStarts;
        clusterLengths = newClusterLengths;
        masks = newMasks;
        syllables = newSyllables;
        ligatureIds = newLigatureIds;
        ligatureComponentCounts = newComponentCounts;
        ligatureComponentIndexes = newComponentIndexes;
        attachmentParents = newAttachmentParents;
        attachmentTypes = newAttachmentTypes;
        defaultIgnorables = newDefaultIgnorables;
        unsafeToBreak = newUnsafeToBreak;
    }

    int FindLayoutSource(int start, int end)
    {
        int index = 0;
        while (index < GetCount())
        {
            if (clusterStarts[index] < end &&
                start < clusterStarts[index] + clusterLengths[index])
            {
                return index;
            }

            index += 1;
        }

        throw new System.ArgumentException(
            "A layout glyph does not overlap the shaping source.");
    }

    void CopyLayoutSourceState(
        int start,
        int end,
        int targetIndex,
        ulong[] targetMasks,
        int[] targetSyllables,
        bool[] targetDefaultIgnorables,
        bool[] targetUnsafeToBreak)
    {
        int sourceIndex = 0;
        while (sourceIndex < GetCount())
        {
            if (clusterStarts[sourceIndex] < end &&
                start < clusterStarts[sourceIndex] +
                    clusterLengths[sourceIndex])
            {
                targetMasks[targetIndex] |= masks[sourceIndex];
                if (syllables[sourceIndex] > targetSyllables[targetIndex])
                {
                    targetSyllables[targetIndex] = syllables[sourceIndex];
                }

                targetDefaultIgnorables[targetIndex] =
                    targetDefaultIgnorables[targetIndex] ||
                    defaultIgnorables[sourceIndex];
                targetUnsafeToBreak[targetIndex] =
                    targetUnsafeToBreak[targetIndex] ||
                    unsafeToBreak[sourceIndex];
            }

            sourceIndex += 1;
        }
    }

    void ValidateIndex(int index)
    {
        if (index < 0 || index >= runes.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }

    void ValidateRange(int startIndex, int endIndex)
    {
        if (startIndex < 0 || startIndex >= endIndex ||
            endIndex > runes.Length)
        {
            throw new System.ArgumentOutOfRangeException("startIndex");
        }
    }

    static void ValidateDirection(ShapingDirection direction)
    {
        if (direction != ShapingDirection.LeftToRight &&
            direction != ShapingDirection.RightToLeft &&
            direction != ShapingDirection.TopToBottom &&
            direction != ShapingDirection.BottomToTop)
        {
            throw new System.ArgumentOutOfRangeException("direction");
        }
    }

    static void ValidateCluster(int start, int length, int sourceLength)
    {
        if (start < 0 || length <= 0 ||
            length > sourceLength - start)
        {
            throw new System.ArgumentOutOfRangeException("start");
        }
    }

    static void ValidateGlyphId(int glyphId)
    {
        if (glyphId < 0 || glyphId > 0xFFFF)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }
    }
}
