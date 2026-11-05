namespace ttf;

internal enum SfntGposAttachmentType
{
    None = 0,
    Cursive = 1,
    Mark = 2
}

internal class SfntGposPositionBuffer
{
    SfntLayoutGlyphBuffer glyphBuffer;
    double[]? xPlacements;
    double[]? yPlacements;
    double[]? xAdvances;
    double[]? yAdvances;
    int[]? attachmentParents;
    SfntGposAttachmentType[]? attachmentTypes;
    bool attachmentsResolved;

    internal SfntGposPositionBuffer(SfntLayoutGlyphBuffer glyphBuffer)
    {
        if (glyphBuffer == null)
        {
            throw new System.ArgumentNullException("glyphBuffer");
        }

        this.glyphBuffer = glyphBuffer;
        InitializePositions(glyphBuffer.GetCount());
    }

    internal static SfntGposPositionBuffer Create(int[] glyphIds)
    {
        return new SfntGposPositionBuffer(
            SfntLayoutGlyphBuffer.Create(glyphIds));
    }

    internal static SfntGposPositionBuffer Create(
        SfntLayoutGlyphBuffer glyphBuffer)
    {
        return new SfntGposPositionBuffer(glyphBuffer);
    }

    void InitializePositions(int count)
    {
        xPlacements = new double[count];
        yPlacements = new double[count];
        xAdvances = new double[count];
        yAdvances = new double[count];
        attachmentParents = new int[count];
        attachmentTypes = new SfntGposAttachmentType[count];
        int index = 0;

        while (index < count)
        {
            attachmentParents[index] = -1;
            index += 1;
        }
    }

    internal int GetCount()
    {
        return glyphBuffer.GetCount();
    }

    internal int GetGlyphId(int index)
    {
        return glyphBuffer.GetGlyphId(index);
    }

    internal SfntLayoutGlyphBuffer GetGlyphBuffer()
    {
        return glyphBuffer;
    }

    internal double GetXPlacement(int index)
    {
        ValidateIndex(index);
        System.ArgumentNullException.ThrowIfNull(xPlacements);
        return xPlacements[index];
    }

    internal double GetYPlacement(int index)
    {
        ValidateIndex(index);
        System.ArgumentNullException.ThrowIfNull(yPlacements);
        return yPlacements[index];
    }

    internal double GetXAdvance(int index)
    {
        ValidateIndex(index);
        System.ArgumentNullException.ThrowIfNull(xAdvances);
        return xAdvances[index];
    }

    internal double GetYAdvance(int index)
    {
        ValidateIndex(index);
        System.ArgumentNullException.ThrowIfNull(yAdvances);
        return yAdvances[index];
    }

    internal SfntGposAttachmentType GetAttachmentType(int index)
    {
        ValidateIndex(index);
        System.ArgumentNullException.ThrowIfNull(attachmentTypes);
        return attachmentTypes[index];
    }

    internal int GetAttachmentParent(int index)
    {
        ValidateIndex(index);
        System.ArgumentNullException.ThrowIfNull(attachmentParents);
        return attachmentParents[index];
    }

    internal void ReplaceWith(SfntGposPositionBuffer source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        glyphBuffer = source.glyphBuffer;
        int count = source.GetCount();
        xPlacements = new double[count];
        yPlacements = new double[count];
        xAdvances = new double[count];
        yAdvances = new double[count];
        attachmentParents = new int[count];
        attachmentTypes = new SfntGposAttachmentType[count];
        System.ArgumentNullException.ThrowIfNull(source.xPlacements);
        System.ArgumentNullException.ThrowIfNull(source.yPlacements);
        System.ArgumentNullException.ThrowIfNull(source.xAdvances);
        System.ArgumentNullException.ThrowIfNull(source.yAdvances);
        System.ArgumentNullException.ThrowIfNull(source.attachmentParents);
        System.ArgumentNullException.ThrowIfNull(source.attachmentTypes);
        System.Array.Copy(source.xPlacements, xPlacements, count);
        System.Array.Copy(source.yPlacements, yPlacements, count);
        System.Array.Copy(source.xAdvances, xAdvances, count);
        System.Array.Copy(source.yAdvances, yAdvances, count);
        System.Array.Copy(source.attachmentParents, attachmentParents, count);
        System.Array.Copy(source.attachmentTypes, attachmentTypes, count);
        attachmentsResolved = source.attachmentsResolved;
    }

    internal void ApplyAdjustment(
        int index,
        SfntGposAdjustment adjustment)
    {
        ValidateIndex(index);
        EnsureAttachmentsAreMutable();

        if (adjustment == null)
        {
            throw new System.ArgumentNullException("adjustment");
        }

        System.ArgumentNullException.ThrowIfNull(xPlacements);
        xPlacements[index] += adjustment.GetXPlacement();
        System.ArgumentNullException.ThrowIfNull(yPlacements);
        yPlacements[index] += adjustment.GetYPlacement();
        System.ArgumentNullException.ThrowIfNull(xAdvances);
        xAdvances[index] += adjustment.GetXAdvance();
        System.ArgumentNullException.ThrowIfNull(yAdvances);
        yAdvances[index] += adjustment.GetYAdvance();
    }

    internal void SetXPlacement(int index, double value)
    {
        ValidateIndex(index);
        EnsureAttachmentsAreMutable();
        System.ArgumentNullException.ThrowIfNull(xPlacements);
        xPlacements[index] = value;
    }

    internal void SetXAdvance(int index, double value)
    {
        ValidateIndex(index);
        EnsureAttachmentsAreMutable();
        System.ArgumentNullException.ThrowIfNull(xAdvances);
        xAdvances[index] = value;
    }

    internal void AttachCursive(
        int childIndex,
        int parentIndex,
        double yOffset)
    {
        ValidateIndex(childIndex);
        ValidateIndex(parentIndex);
        EnsureAttachmentsAreMutable();

        if (childIndex == parentIndex)
        {
            throw new System.ArgumentException(
                "A glyph cannot be attached to itself.");
        }

        ReverseCursiveAttachment(
            childIndex,
            parentIndex);
        System.ArgumentNullException.ThrowIfNull(attachmentParents);
        attachmentParents[childIndex] = parentIndex;
        System.ArgumentNullException.ThrowIfNull(attachmentTypes);
        attachmentTypes[childIndex] = SfntGposAttachmentType.Cursive;
        System.ArgumentNullException.ThrowIfNull(yPlacements);
        yPlacements[childIndex] = yOffset;

        if (attachmentParents[parentIndex] == childIndex &&
            attachmentTypes[parentIndex] ==
            SfntGposAttachmentType.Cursive)
        {
            attachmentParents[parentIndex] = -1;
            attachmentTypes[parentIndex] = SfntGposAttachmentType.None;
            yPlacements[parentIndex] = 0;
        }
    }

    internal void AttachMark(
        int markIndex,
        int parentIndex,
        double xOffset,
        double yOffset)
    {
        ValidateIndex(markIndex);
        ValidateIndex(parentIndex);
        EnsureAttachmentsAreMutable();

        if (markIndex == parentIndex)
        {
            throw new System.ArgumentException(
                "A glyph cannot be attached to itself.");
        }

        System.ArgumentNullException.ThrowIfNull(attachmentParents);
        attachmentParents[markIndex] = parentIndex;
        System.ArgumentNullException.ThrowIfNull(attachmentTypes);
        attachmentTypes[markIndex] = SfntGposAttachmentType.Mark;
        System.ArgumentNullException.ThrowIfNull(xPlacements);
        xPlacements[markIndex] = xOffset;
        System.ArgumentNullException.ThrowIfNull(yPlacements);
        yPlacements[markIndex] = yOffset;
    }

    internal void ResolveAttachments(
        SfntLayoutTraversalDirection direction)
    {
        if (direction != SfntLayoutTraversalDirection.Forward &&
            direction != SfntLayoutTraversalDirection.Backward)
        {
            throw new System.ArgumentOutOfRangeException("direction");
        }

        if (attachmentsResolved)
        {
            return;
        }

        ValidateAttachmentGraph();
        bool[] resolved = new bool[glyphBuffer.GetCount()];
        int[] path = new int[glyphBuffer.GetCount()];
        int step = direction == SfntLayoutTraversalDirection.Forward ?
            1 : -1;
        int index = direction == SfntLayoutTraversalDirection.Forward ?
            0 : glyphBuffer.GetCount() - 1;

        while (index >= 0 && index < glyphBuffer.GetCount())
        {
            ResolveAttachmentPath(index, resolved, path);
            index += step;
        }

        attachmentsResolved = true;
    }

    void ValidateAttachmentGraph()
    {
        int[] visits = new int[glyphBuffer.GetCount()];
        int startIndex = 0;

        while (startIndex < glyphBuffer.GetCount())
        {
            int visit = startIndex + 1;
            int index = startIndex;

            System.ArgumentNullException.ThrowIfNull(attachmentParents);
            while (attachmentParents[index] >= 0)
            {
                if (visits[index] == visit)
                {
                    throw FontFormatException.Create(
                        "The GPOS attachment graph contains a cycle.");
                }

                if (visits[index] != 0)
                {
                    break;
                }

                visits[index] = visit;
                index = attachmentParents[index];
            }

            startIndex += 1;
        }
    }

    void ResolveAttachmentPath(
        int index,
        bool[] resolved,
        int[] path)
    {
        int pathLength = 0;

        System.ArgumentNullException.ThrowIfNull(attachmentParents);
        while (attachmentParents[index] >= 0 && !resolved[index])
        {
            path[pathLength] = index;
            pathLength += 1;
            index = attachmentParents[index];
        }

        resolved[index] = true;

        while (pathLength > 0)
        {
            pathLength -= 1;
            int childIndex = path[pathLength];
            int parentIndex = attachmentParents[childIndex];

            System.ArgumentNullException.ThrowIfNull(attachmentTypes);
            if (attachmentTypes[childIndex] ==
                SfntGposAttachmentType.Mark)
            {
                ResolveMarkAttachment(childIndex, parentIndex);
            }
            else if (attachmentTypes[childIndex] ==
                SfntGposAttachmentType.Cursive)
            {
                System.ArgumentNullException.ThrowIfNull(yPlacements);
                yPlacements[childIndex] += yPlacements[parentIndex];
            }

            resolved[childIndex] = true;
        }
    }

    void ResolveMarkAttachment(int childIndex, int parentIndex)
    {
        System.ArgumentNullException.ThrowIfNull(xPlacements);
        xPlacements[childIndex] += xPlacements[parentIndex];
        System.ArgumentNullException.ThrowIfNull(yPlacements);
        yPlacements[childIndex] += yPlacements[parentIndex];

        if (parentIndex < childIndex)
        {
            int index = parentIndex;

            while (index < childIndex)
            {
                System.ArgumentNullException.ThrowIfNull(xAdvances);
                xPlacements[childIndex] -= xAdvances[index];
                index += 1;
            }
        }
        else
        {
            int index = childIndex;

            while (index < parentIndex)
            {
                System.ArgumentNullException.ThrowIfNull(xAdvances);
                xPlacements[childIndex] += xAdvances[index];
                index += 1;
            }
        }
    }

    void ReverseCursiveAttachment(
        int index,
        int newParentIndex)
    {
        System.ArgumentNullException.ThrowIfNull(attachmentParents);
        int oldParentIndex = attachmentParents[index];
        System.ArgumentNullException.ThrowIfNull(attachmentTypes);
        SfntGposAttachmentType oldType = attachmentTypes[index];
        System.ArgumentNullException.ThrowIfNull(yPlacements);
        double oldYPlacement = yPlacements[index];
        attachmentParents[index] = -1;
        attachmentTypes[index] = SfntGposAttachmentType.None;

        while (oldParentIndex >= 0 &&
            oldType == SfntGposAttachmentType.Cursive)
        {
            if (oldParentIndex == newParentIndex)
            {
                return;
            }

            int nextParentIndex = attachmentParents[oldParentIndex];
            SfntGposAttachmentType nextType =
                attachmentTypes[oldParentIndex];
            double nextYPlacement = yPlacements[oldParentIndex];
            yPlacements[oldParentIndex] = -oldYPlacement;
            attachmentParents[oldParentIndex] = index;
            attachmentTypes[oldParentIndex] =
                SfntGposAttachmentType.Cursive;
            index = oldParentIndex;
            oldParentIndex = nextParentIndex;
            oldType = nextType;
            oldYPlacement = nextYPlacement;
        }
    }

    void ValidateIndex(int index)
    {
        if (index < 0 || index >= glyphBuffer.GetCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }

    void EnsureAttachmentsAreMutable()
    {
        if (attachmentsResolved)
        {
            throw new System.InvalidOperationException(
                "Resolved GPOS positions cannot be modified.");
        }
    }
}

internal class SfntGposPairMatch
{
    int secondIndex;
    int secondValueFormat;

    internal SfntGposPairMatch(
        int secondIndex,
        int secondValueFormat)
    {
        this.secondIndex = secondIndex;
        this.secondValueFormat = secondValueFormat;
    }

    internal int GetSecondIndex()
    {
        return secondIndex;
    }

    internal int GetSecondValueFormat()
    {
        return secondValueFormat;
    }
}

internal class SfntGposContextApplication
{
    bool applied;
    int nextIndex;

    internal SfntGposContextApplication(
        bool applied,
        int nextIndex)
    {
        this.applied = applied;
        this.nextIndex = nextIndex;
    }

    internal bool WasApplied()
    {
        return applied;
    }

    internal int GetNextIndex()
    {
        return nextIndex;
    }
}

internal static class SfntGposExecutor
{
    internal static int ApplyLookup(
        SfntGposTable table,
        int lookupIndex,
        SfntGposPositionBuffer buffer,
        SfntGdefTable gdef,
        SfntLayoutTraversalDirection direction,
        SfntGposAdjustmentContext adjustmentContext)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        return ApplyLookup(
            table,
            lookupIndex,
            buffer,
            gdef,
            direction,
            adjustmentContext,
            SfntLayoutExecutionContext.CreateDefault(table.GetLimits()));
    }

    internal static int ApplyLookup(
        SfntGposTable table,
        int lookupIndex,
        SfntGposPositionBuffer buffer,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction,
        SfntGposAdjustmentContext adjustmentContext,
        SfntLayoutExecutionContext executionContext)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        if (lookupIndex < 0 || lookupIndex >= table.GetLookupCount())
        {
            throw new System.ArgumentOutOfRangeException("lookupIndex");
        }

        if (direction != SfntLayoutTraversalDirection.Forward &&
            direction != SfntLayoutTraversalDirection.Backward)
        {
            throw new System.ArgumentOutOfRangeException("direction");
        }

        if (adjustmentContext == null)
        {
            throw new System.ArgumentNullException("adjustmentContext");
        }

        if (executionContext == null)
        {
            throw new System.ArgumentNullException("executionContext");
        }

        executionContext.CheckGlyphBuffer(buffer.GetCount());

        SfntGposLookup lookup = table.GetLookup(lookupIndex);
        SfntLayoutLookup layoutLookup = lookup.GetLayoutLookup();
        new SfntLookupGlyphFilter(layoutLookup, gdef);
        int lookupType = lookup.GetLookupType();
        int step = direction == SfntLayoutTraversalDirection.Forward ?
            1 : -1;
        int index = direction == SfntLayoutTraversalDirection.Forward ?
            0 : buffer.GetCount() - 1;
        int applicationCount = 0;

        while (index >= 0 && index < buffer.GetCount())
        {
            executionContext.RecordOperation();
            bool applied = false;

            if (lookupType == 1)
            {
                applied = ApplySingleAt(
                    lookup,
                    buffer,
                    index,
                    adjustmentContext);
            }
            else if (lookupType == 2)
            {
                SfntGposPairMatch? match = ApplyPairAt(
                    lookup,
                    buffer,
                    index,
                    gdef,
                    direction,
                    adjustmentContext);

                if (match != null)
                {
                    applied = true;
                    index = match.GetSecondValueFormat() == 0 ?
                        match.GetSecondIndex() :
                        match.GetSecondIndex() + step;
                }
            }
            else if (lookupType == 3)
            {
                int secondIndex = ApplyCursiveAt(
                    lookup,
                    buffer,
                    index,
                    gdef,
                    direction,
                    adjustmentContext);

                if (secondIndex >= 0)
                {
                    applied = true;
                    index = secondIndex;
                }
            }
            else if (lookupType == 4)
            {
                applied = ApplyMarkBaseAt(
                    lookup,
                    buffer,
                    index,
                    gdef,
                    direction,
                    adjustmentContext);
            }
            else if (lookupType == 5)
            {
                applied = ApplyMarkLigatureAt(
                    lookup,
                    buffer,
                    index,
                    gdef,
                    direction,
                    adjustmentContext);
            }
            else if (lookupType == 6)
            {
                applied = ApplyMarkMarkAt(
                    lookup,
                    buffer,
                    index,
                    gdef,
                    direction,
                    adjustmentContext);
            }
            else if (lookupType == 7 || lookupType == 8)
            {
                SfntGposContextApplication application =
                    ApplyContextAt(
                        table,
                        lookup,
                        buffer,
                        index,
                        gdef,
                        direction,
                        adjustmentContext,
                        0,
                        executionContext);
                applied = application.WasApplied();

                if (applied)
                {
                    index = application.GetNextIndex();
                }
            }

            if (applied)
            {
                applicationCount += 1;

                if (lookupType != 2 && lookupType != 3 &&
                    lookupType != 7 && lookupType != 8)
                {
                    index += step;
                }
            }
            else
            {
                index += step;
            }
        }

        return applicationCount;
    }

    static bool ApplySingleAt(
        SfntGposLookup lookup,
        SfntGposPositionBuffer buffer,
        int index,
        SfntGposAdjustmentContext adjustmentContext)
    {
        int subtableIndex = 0;

        while (subtableIndex < lookup.GetSubtableCount())
        {
            SfntGposValueRecord? record = lookup.GetSingleAdjustment(
                subtableIndex).FindValueRecord(buffer.GetGlyphId(index));

            if (record != null)
            {
                buffer.ApplyAdjustment(
                    index,
                    adjustmentContext.ResolveValueRecord(record));
                return true;
            }

            subtableIndex += 1;
        }

        return false;
    }

    static SfntGposPairMatch? ApplyPairAt(
        SfntGposLookup lookup,
        SfntGposPositionBuffer buffer,
        int firstIndex,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction,
        SfntGposAdjustmentContext adjustmentContext)
    {
        SfntLookupGlyphIterator iterator = SfntLookupGlyphIterator.Create(
            buffer.GetGlyphBuffer(),
            lookup.GetLayoutLookup(),
            gdef,
            direction);
        iterator.Reset(firstIndex);

        if (!iterator.MoveNext())
        {
            return null;
        }

        int secondIndex = iterator.GetIndex();
        int firstGlyphId = buffer.GetGlyphId(firstIndex);
        int secondGlyphId = buffer.GetGlyphId(secondIndex);
        int subtableIndex = 0;

        while (subtableIndex < lookup.GetSubtableCount())
        {
            SfntGposPairAdjustment adjustment = lookup.GetPairAdjustment(
                subtableIndex);
            SfntGposPairValueRecord? pair = adjustment.FindPairValue(
                firstGlyphId,
                secondGlyphId);

            if (pair != null)
            {
                buffer.ApplyAdjustment(
                    firstIndex,
                    adjustmentContext.ResolveValueRecord(
                        pair.GetFirstValueRecord()));
                buffer.ApplyAdjustment(
                    secondIndex,
                    adjustmentContext.ResolveValueRecord(
                        pair.GetSecondValueRecord()));
                return new SfntGposPairMatch(
                    secondIndex,
                    adjustment.GetSecondValueFormat());
            }

            subtableIndex += 1;
        }

        return null;
    }

    static int ApplyCursiveAt(
        SfntGposLookup lookup,
        SfntGposPositionBuffer buffer,
        int firstIndex,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction,
        SfntGposAdjustmentContext adjustmentContext)
    {
        SfntLookupGlyphIterator iterator = SfntLookupGlyphIterator.Create(
            buffer.GetGlyphBuffer(),
            lookup.GetLayoutLookup(),
            gdef,
            direction);
        iterator.Reset(firstIndex);

        if (!iterator.MoveNext())
        {
            return -1;
        }

        int secondIndex = iterator.GetIndex();
        int firstGlyphId = buffer.GetGlyphId(firstIndex);
        int secondGlyphId = buffer.GetGlyphId(secondIndex);
        int subtableIndex = 0;

        while (subtableIndex < lookup.GetSubtableCount())
        {
            SfntGposCursiveAdjustment adjustment =
                lookup.GetCursiveAdjustment(subtableIndex);
            SfntGposEntryExitRecord? firstRecord =
                adjustment.FindEntryExitRecord(firstGlyphId);
            SfntGposEntryExitRecord? secondRecord =
                adjustment.FindEntryExitRecord(secondGlyphId);

            if (firstRecord != null &&
                firstRecord.HasExitAnchor() &&
                secondRecord != null &&
                secondRecord.HasEntryAnchor())
            {
                ApplyCursiveAnchors(
                    lookup.GetLayoutLookup(),
                    buffer,
                    firstIndex,
                    secondIndex,
                    firstRecord.GetExitAnchor(),
                    secondRecord.GetEntryAnchor(),
                    direction,
                    adjustmentContext);
                return secondIndex;
            }

            subtableIndex += 1;
        }

        return -1;
    }

    static void ApplyCursiveAnchors(
        SfntLayoutLookup layoutLookup,
        SfntGposPositionBuffer buffer,
        int firstIndex,
        int secondIndex,
        SfntLayoutAnchor? exitAnchor,
        SfntLayoutAnchor? entryAnchor,
        SfntLayoutTraversalDirection direction,
        SfntGposAdjustmentContext adjustmentContext)
    {
        SfntGposAnchorPosition exitPosition =
            adjustmentContext.ResolveAnchor(exitAnchor);
        SfntGposAnchorPosition entryPosition =
            adjustmentContext.ResolveAnchor(entryAnchor);

        if (direction == SfntLayoutTraversalDirection.Forward)
        {
            buffer.SetXAdvance(
                firstIndex,
                exitPosition.GetXCoordinate() +
                buffer.GetXPlacement(firstIndex));
            double adjustment = entryPosition.GetXCoordinate() +
                buffer.GetXPlacement(secondIndex);
            buffer.SetXAdvance(
                secondIndex,
                buffer.GetXAdvance(secondIndex) - adjustment);
            buffer.SetXPlacement(
                secondIndex,
                buffer.GetXPlacement(secondIndex) - adjustment);
        }
        else
        {
            double adjustment = exitPosition.GetXCoordinate() +
                buffer.GetXPlacement(firstIndex);
            buffer.SetXAdvance(
                firstIndex,
                buffer.GetXAdvance(firstIndex) - adjustment);
            buffer.SetXPlacement(
                firstIndex,
                buffer.GetXPlacement(firstIndex) - adjustment);
            buffer.SetXAdvance(
                secondIndex,
                entryPosition.GetXCoordinate() +
                buffer.GetXPlacement(secondIndex));
        }

        if (layoutLookup.IsRightToLeft())
        {
            buffer.AttachCursive(
                firstIndex,
                secondIndex,
                entryPosition.GetYCoordinate() -
                exitPosition.GetYCoordinate());
        }
        else
        {
            buffer.AttachCursive(
                secondIndex,
                firstIndex,
                exitPosition.GetYCoordinate() -
                entryPosition.GetYCoordinate());
        }
    }

    static SfntGposContextApplication ApplyContextAt(
        SfntGposTable table,
        SfntGposLookup lookup,
        SfntGposPositionBuffer buffer,
        int index,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction,
        SfntGposAdjustmentContext adjustmentContext,
        int depth,
        SfntLayoutExecutionContext executionContext)
    {
        SfntGsubContextMatch? match = lookup.GetLookupType() == 7 ?
            FindContextMatch(
                lookup,
                buffer,
                index,
                gdef,
                direction) :
            FindChainedContextMatch(
                lookup,
                buffer,
                index,
                gdef,
                direction);

        if (match == null)
        {
            return new SfntGposContextApplication(false, index);
        }

        executionContext.CheckNesting(depth);

        int[] matchedIndices = match.GetMatchedIndices();
        SfntSequenceLookupRecords? records = match.GetLookupRecords();
        int recordIndex = 0;

        System.ArgumentNullException.ThrowIfNull(records);
        while (recordIndex < records.GetRecordCount())
        {
            SfntSequenceLookupRecord record = records.GetRecord(recordIndex);
            ApplyLookupAt(
                table,
                record.GetLookupListIndex(),
                buffer,
                matchedIndices[record.GetSequenceIndex()],
                gdef,
                direction,
                adjustmentContext,
                depth + 1,
                executionContext);
            recordIndex += 1;
        }

        int nextIndex = direction == SfntLayoutTraversalDirection.Forward ?
            FindMaximum(matchedIndices) + 1 :
            FindMinimum(matchedIndices) - 1;
        return new SfntGposContextApplication(true, nextIndex);
    }

    static bool ApplyLookupAt(
        SfntGposTable table,
        int lookupIndex,
        SfntGposPositionBuffer buffer,
        int index,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction,
        SfntGposAdjustmentContext adjustmentContext,
        int depth,
        SfntLayoutExecutionContext executionContext)
    {
        if (index < 0 || index >= buffer.GetCount())
        {
            return false;
        }

        executionContext.RecordOperation();

        SfntGposLookup lookup = table.GetLookup(lookupIndex);
        new SfntLookupGlyphFilter(lookup.GetLayoutLookup(), gdef);
        int lookupType = lookup.GetLookupType();

        if (lookupType == 1)
        {
            return ApplySingleAt(
                lookup,
                buffer,
                index,
                adjustmentContext);
        }

        if (lookupType == 2)
        {
            return ApplyPairAt(
                lookup,
                buffer,
                index,
                gdef,
                direction,
                adjustmentContext) != null;
        }

        if (lookupType == 3)
        {
            return ApplyCursiveAt(
                lookup,
                buffer,
                index,
                gdef,
                direction,
                adjustmentContext) >= 0;
        }

        if (lookupType == 4)
        {
            return ApplyMarkBaseAt(
                lookup,
                buffer,
                index,
                gdef,
                direction,
                adjustmentContext);
        }

        if (lookupType == 5)
        {
            return ApplyMarkLigatureAt(
                lookup,
                buffer,
                index,
                gdef,
                direction,
                adjustmentContext);
        }

        if (lookupType == 6)
        {
            return ApplyMarkMarkAt(
                lookup,
                buffer,
                index,
                gdef,
                direction,
                adjustmentContext);
        }

        if (lookupType == 7 || lookupType == 8)
        {
            return ApplyContextAt(
                table,
                lookup,
                buffer,
                index,
                gdef,
                direction,
                adjustmentContext,
                depth,
                executionContext).WasApplied();
        }

        return false;
    }

    static SfntGsubContextMatch? FindContextMatch(
        SfntGposLookup lookup,
        SfntGposPositionBuffer buffer,
        int index,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        int subtableIndex = 0;

        while (subtableIndex < lookup.GetSubtableCount())
        {
            SfntGsubContextSubstitution adjustment =
                lookup.GetContextAdjustment(subtableIndex);
            SfntGsubContextMatch? match;

            if (adjustment.GetFormat() == 1)
            {
                match = SfntGsubExecutor.MatchContextRules(
                    adjustment,
                    false,
                    lookup.GetLayoutLookup(),
                    buffer.GetGlyphBuffer(),
                    index,
                    gdef,
                    direction);
            }
            else if (adjustment.GetFormat() == 2)
            {
                match = SfntGsubExecutor.MatchContextRules(
                    adjustment,
                    true,
                    lookup.GetLayoutLookup(),
                    buffer.GetGlyphBuffer(),
                    index,
                    gdef,
                    direction);
            }
            else
            {
                match = SfntGsubExecutor.MatchContextCoverages(
                    adjustment,
                    lookup.GetLayoutLookup(),
                    buffer.GetGlyphBuffer(),
                    index,
                    gdef,
                    direction);
            }

            if (match != null)
            {
                return match;
            }

            subtableIndex += 1;
        }

        return null;
    }

    static SfntGsubContextMatch? FindChainedContextMatch(
        SfntGposLookup lookup,
        SfntGposPositionBuffer buffer,
        int index,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        int subtableIndex = 0;

        while (subtableIndex < lookup.GetSubtableCount())
        {
            SfntGsubChainedContextSubstitution adjustment =
                lookup.GetChainedContextAdjustment(subtableIndex);
            SfntGsubContextMatch? match;

            if (adjustment.GetFormat() == 1)
            {
                match = SfntGsubExecutor.MatchChainedRules(
                    adjustment,
                    false,
                    lookup.GetLayoutLookup(),
                    buffer.GetGlyphBuffer(),
                    index,
                    gdef,
                    direction);
            }
            else if (adjustment.GetFormat() == 2)
            {
                match = SfntGsubExecutor.MatchChainedRules(
                    adjustment,
                    true,
                    lookup.GetLayoutLookup(),
                    buffer.GetGlyphBuffer(),
                    index,
                    gdef,
                    direction);
            }
            else
            {
                match = SfntGsubExecutor.MatchChainedCoverages(
                    adjustment,
                    lookup.GetLayoutLookup(),
                    buffer.GetGlyphBuffer(),
                    index,
                    gdef,
                    direction);
            }

            if (match != null)
            {
                return match;
            }

            subtableIndex += 1;
        }

        return null;
    }

    static int FindMinimum(int[] values)
    {
        int minimum = values[0];
        int index = 1;

        while (index < values.Length)
        {
            minimum = System.Math.Min(minimum, values[index]);
            index += 1;
        }

        return minimum;
    }

    static int FindMaximum(int[] values)
    {
        int maximum = values[0];
        int index = 1;

        while (index < values.Length)
        {
            maximum = System.Math.Max(maximum, values[index]);
            index += 1;
        }

        return maximum;
    }

    static bool ApplyMarkBaseAt(
        SfntGposLookup lookup,
        SfntGposPositionBuffer buffer,
        int markIndex,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction,
        SfntGposAdjustmentContext adjustmentContext)
    {
        int baseIndex = FindPreviousNonMark(
            lookup,
            buffer,
            markIndex,
            gdef,
            direction);

        if (baseIndex < 0)
        {
            return false;
        }

        int subtableIndex = 0;

        while (subtableIndex < lookup.GetSubtableCount())
        {
            SfntGposMarkAttachment adjustment =
                lookup.GetMarkBaseAdjustment(subtableIndex);

            if (ApplyMarkAttachment(
                adjustment,
                buffer,
                markIndex,
                baseIndex,
                adjustmentContext))
            {
                return true;
            }

            subtableIndex += 1;
        }

        return false;
    }

    static bool ApplyMarkMarkAt(
        SfntGposLookup lookup,
        SfntGposPositionBuffer buffer,
        int markIndex,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction,
        SfntGposAdjustmentContext adjustmentContext)
    {
        SfntLookupGlyphIterator iterator = SfntLookupGlyphIterator.Create(
            buffer.GetGlyphBuffer(),
            lookup.GetLayoutLookup(),
            gdef,
            GetOppositeDirection(direction));
        iterator.Reset(markIndex);

        if (!iterator.MoveNext())
        {
            return false;
        }

        int baseIndex = iterator.GetIndex();
        int subtableIndex = 0;

        while (subtableIndex < lookup.GetSubtableCount())
        {
            SfntGposMarkAttachment adjustment =
                lookup.GetMarkMarkAdjustment(subtableIndex);

            if (ApplyMarkAttachment(
                adjustment,
                buffer,
                markIndex,
                baseIndex,
                adjustmentContext))
            {
                return true;
            }

            subtableIndex += 1;
        }

        return false;
    }

    static bool ApplyMarkAttachment(
        SfntGposMarkAttachment adjustment,
        SfntGposPositionBuffer buffer,
        int markIndex,
        int baseIndex,
        SfntGposAdjustmentContext adjustmentContext)
    {
        SfntMarkRecord? markRecord = adjustment.FindMarkRecord(
            buffer.GetGlyphId(markIndex));

        if (markRecord == null)
        {
            return false;
        }

        SfntLayoutAnchor? baseAnchor = adjustment.FindBaseAnchor(
            buffer.GetGlyphId(baseIndex),
            markRecord.GetMarkClass());

        if (baseAnchor == null)
        {
            return false;
        }

        ApplyMarkAnchors(
            buffer,
            markIndex,
            baseIndex,
            markRecord.GetAnchor(),
            baseAnchor,
            adjustmentContext);
        return true;
    }

    static bool ApplyMarkLigatureAt(
        SfntGposLookup lookup,
        SfntGposPositionBuffer buffer,
        int markIndex,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction,
        SfntGposAdjustmentContext adjustmentContext)
    {
        int ligatureIndex = FindPreviousNonMark(
            lookup,
            buffer,
            markIndex,
            gdef,
            direction);

        if (ligatureIndex < 0)
        {
            return false;
        }

        int subtableIndex = 0;

        while (subtableIndex < lookup.GetSubtableCount())
        {
            SfntGposMarkLigatureAdjustment adjustment =
                lookup.GetMarkLigatureAdjustment(subtableIndex);
            SfntMarkRecord? markRecord = adjustment.FindMarkRecord(
                buffer.GetGlyphId(markIndex));
            SfntGposLigatureAttach? ligature =
                adjustment.FindLigatureAttach(
                    buffer.GetGlyphId(ligatureIndex));

            if (markRecord != null && ligature != null)
            {
                int componentIndex = GetLigatureComponentIndex(
                    buffer.GetGlyphBuffer(),
                    ligature,
                    ligatureIndex,
                    markIndex);
                SfntLayoutAnchor? baseAnchor = ligature.GetAnchor(
                    componentIndex,
                    markRecord.GetMarkClass());

                if (baseAnchor != null)
                {
                    ApplyMarkAnchors(
                        buffer,
                        markIndex,
                        ligatureIndex,
                        markRecord.GetAnchor(),
                        baseAnchor,
                        adjustmentContext);
                    return true;
                }
            }

            subtableIndex += 1;
        }

        return false;
    }

    static int GetLigatureComponentIndex(
        SfntLayoutGlyphBuffer glyphBuffer,
        SfntGposLigatureAttach ligature,
        int ligatureIndex,
        int markIndex)
    {
        int componentIndex = ligature.GetComponentCount() - 1;
        bool sameCluster =
            glyphBuffer.GetClusterStart(ligatureIndex) ==
            glyphBuffer.GetClusterStart(markIndex) &&
            glyphBuffer.GetClusterLength(ligatureIndex) ==
            glyphBuffer.GetClusterLength(markIndex);

        if (sameCluster)
        {
            componentIndex = glyphBuffer.GetLigatureComponentIndex(
                markIndex);

            if (componentIndex >= ligature.GetComponentCount())
            {
                componentIndex = ligature.GetComponentCount() - 1;
            }
        }

        return componentIndex;
    }

    static void ApplyMarkAnchors(
        SfntGposPositionBuffer buffer,
        int markIndex,
        int baseIndex,
        SfntLayoutAnchor markAnchor,
        SfntLayoutAnchor baseAnchor,
        SfntGposAdjustmentContext adjustmentContext)
    {
        SfntGposAnchorPosition markPosition =
            adjustmentContext.ResolveAnchor(markAnchor);
        SfntGposAnchorPosition basePosition =
            adjustmentContext.ResolveAnchor(baseAnchor);
        buffer.AttachMark(
            markIndex,
            baseIndex,
            basePosition.GetXCoordinate() -
            markPosition.GetXCoordinate(),
            basePosition.GetYCoordinate() -
            markPosition.GetYCoordinate());
    }

    static int FindPreviousNonMark(
        SfntGposLookup lookup,
        SfntGposPositionBuffer buffer,
        int markIndex,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        int step = direction == SfntLayoutTraversalDirection.Forward ?
            -1 : 1;
        int index = markIndex + step;

        while (index >= 0 && index < buffer.GetCount())
        {
            int glyphId = buffer.GetGlyphId(index);
            bool isMark = gdef == null ?
                IsCoveredMark(lookup, glyphId) :
                gdef.GetGlyphClass(glyphId) == 3;

            if (!isMark)
            {
                return index;
            }

            index += step;
        }

        return -1;
    }

    static bool IsCoveredMark(
        SfntGposLookup lookup,
        int glyphId)
    {
        int lookupType = lookup.GetLookupType();
        int subtableIndex = 0;

        while (subtableIndex < lookup.GetSubtableCount())
        {
            SfntCoverageTable coverage = lookupType == 4 ?
                lookup.GetMarkBaseAdjustment(subtableIndex)
                    .GetMarkCoverage() :
                lookup.GetMarkLigatureAdjustment(subtableIndex)
                    .GetMarkCoverage();

            if (coverage.GetCoverageIndex(glyphId) >= 0)
            {
                return true;
            }

            subtableIndex += 1;
        }

        return false;
    }

    static SfntLayoutTraversalDirection GetOppositeDirection(
        SfntLayoutTraversalDirection direction)
    {
        return direction == SfntLayoutTraversalDirection.Forward ?
            SfntLayoutTraversalDirection.Backward :
            SfntLayoutTraversalDirection.Forward;
    }
}
