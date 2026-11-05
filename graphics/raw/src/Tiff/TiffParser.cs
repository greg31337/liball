namespace raw;

internal class TiffParser
{
    internal BoundedInput Container;
    internal TiffHeader Header;
    internal System.Collections.Generic.List<TiffIfd> Ifds;
    internal System.Collections.Generic.List<TiffIfdLink> Links;
    internal System.Collections.Generic.Dictionary<ulong, TiffIfd> Visited;
    internal System.Collections.Generic.HashSet<ulong> Active;
    internal long TotalTagCount;
    internal long TotalMetadataBytes;

    internal TiffParser(BoundedInput container, TiffHeader header)
    {
        Container = container;
        Header = header;
        Ifds = new System.Collections.Generic.List<TiffIfd>();
        Links = new System.Collections.Generic.List<TiffIfdLink>();
        Visited = new System.Collections.Generic.Dictionary<ulong, TiffIfd>();
        Active = new System.Collections.Generic.HashSet<ulong>();
        TotalTagCount = 0;
        TotalMetadataBytes = 0;
    }

    internal static TiffIfdGraph Parse(BoundedInput input)
    {
        if (input == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A bounded TIFF input is required.", -1);
        }
        if (!input.UsesMemory && !input.Seekable)
        {
            long remaining = input.GetRemaining();
            CheckedMath.AllocationSize(remaining, 1, input.Limits);
            byte[] bufferedBytes = new byte[CheckedMath.ToInt32(remaining)];
            long absoluteOrigin = input.GetAbsolutePosition();
            input.ReadExactly(bufferedBytes);
            BoundedInput bufferedInput = BoundedInput.CreateWithAbsoluteOrigin(new System.ReadOnlyMemory<byte>(bufferedBytes), 0, bufferedBytes.Length, absoluteOrigin, input.Limits);
            return Parse(bufferedInput);
        }

        TiffHeader header = TiffHeader.Parse(input);
        TiffParser parser = new TiffParser(input, header);
        return parser.ParseGraph();
    }

    internal TiffIfdGraph ParseGraph()
    {
        RequireLimits();
        if (Header.FirstIfdOffset != 0)
        {
            VisitChain(Header.FirstIfdOffset, "IFD0", 0, "", true);
        }
        TiffIfdGraph graph = new TiffIfdGraph(Header, Ifds.ToArray(), Links.ToArray());
        graph.ContainerStart = Container.Start;
        graph.ContainerLength = Container.Length;
        graph.Images = TiffImageData.Describe(graph, Container.Limits);
        return graph;
    }

    internal void VisitChain(ulong offset, string path, int depth, string chainBase, bool rootChain)
    {
        System.Collections.Generic.List<ulong> chainOffsets = new System.Collections.Generic.List<ulong>();
        ulong currentOffset = offset;
        string currentPath = path;
        int nextIndex = 1;

        while (currentOffset != 0)
        {
            RequireDepth(depth, currentOffset);
            if (Active.Contains(currentOffset))
            {
                throw Cycle(currentOffset);
            }
            if (Visited.ContainsKey(currentOffset))
            {
                RemoveActive(chainOffsets);
                return;
            }

            TiffIfd ifd = VisitNew(currentOffset, currentPath, depth);
            chainOffsets.Add(currentOffset);
            if (ifd.NextIfdOffset == 0)
            {
                RemoveActive(chainOffsets);
                return;
            }

            string nextPath;
            if (rootChain)
            {
                nextPath = "IFD" + nextIndex;
            }
            else
            {
                nextPath = chainBase + "/NextIFD" + nextIndex;
            }
            if (Active.Contains(ifd.NextIfdOffset))
            {
                throw Cycle(ifd.NextIfdOffset);
            }
            if (Visited.ContainsKey(ifd.NextIfdOffset))
            {
                TiffIfd target = Visited[ifd.NextIfdOffset];
                Links.Add(new TiffIfdLink(ifd.Path, nextPath, target.Path, ifd.NextIfdOffset, TiffIfdLinkKind.Next, 0, nextIndex, true));
                RemoveActive(chainOffsets);
                return;
            }

            Links.Add(new TiffIfdLink(ifd.Path, nextPath, nextPath, ifd.NextIfdOffset, TiffIfdLinkKind.Next, 0, nextIndex, false));
            currentOffset = ifd.NextIfdOffset;
            currentPath = nextPath;
            nextIndex++;
        }
        RemoveActive(chainOffsets);
    }

    internal TiffIfd VisitNew(ulong offset, string path, int depth)
    {
        RequireDepth(depth, offset);
        if (Ifds.Count >= (Container.Limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxIfdCount)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The TIFF IFD count exceeds the configured limit.", GetAbsoluteOffset(offset));
        }
        if (Active.Contains(offset))
        {
            throw Cycle(offset);
        }
        if (Visited.ContainsKey(offset))
        {
            return Visited[offset];
        }

        Active.Add(offset);
        TiffIfd ifd = ReadIfd(offset, path, depth);
        Visited.Add(offset, ifd);
        Ifds.Add(ifd);
        VisitEntryPointers(ifd);
        return ifd;
    }

    internal void VisitEntryPointers(TiffIfd ifd)
    {
        int subIfdIndex = 0;
        int exifIndex = 0;
        int gpsIndex = 0;
        int interopIndex = 0;

        for (int entryIndex = 0; entryIndex < ifd.Entries.Length; entryIndex++)
        {
            TiffEntry entry = ifd.Entries[entryIndex];
            TiffIfdLinkKind kind = GetPointerKind(entry);
            if (kind == TiffIfdLinkKind.None)
            {
                continue;
            }

            ulong[] offsets = GetPointerOffsets(entry);
            for (int valueIndex = 0; valueIndex < offsets.Length; valueIndex++)
            {
                ulong targetOffset = offsets[valueIndex];
                string path;
                int pathIndex;
                if (kind == TiffIfdLinkKind.SubIfd)
                {
                    pathIndex = subIfdIndex;
                    path = ifd.Path + "/SubIFD" + subIfdIndex;
                    subIfdIndex++;
                }
                else if (kind == TiffIfdLinkKind.Exif)
                {
                    pathIndex = exifIndex;
                    path = NamedPath(ifd.Path, "Exif", exifIndex);
                    exifIndex++;
                }
                else if (kind == TiffIfdLinkKind.Gps)
                {
                    pathIndex = gpsIndex;
                    path = NamedPath(ifd.Path, "GPS", gpsIndex);
                    gpsIndex++;
                }
                else if (kind == TiffIfdLinkKind.Interop)
                {
                    pathIndex = interopIndex;
                    path = NamedPath(ifd.Path, "Interop", interopIndex);
                    interopIndex++;
                }
                else
                {
                    pathIndex = valueIndex;
                    path = ifd.Path + "/Tag0x" + entry.Tag.ToString("X4", System.Globalization.CultureInfo.InvariantCulture) + "[" + entryIndex + ":" + valueIndex + "]";
                }

                if (targetOffset == 0)
                {
                    continue;
                }
                Follow(ifd, targetOffset, path, ifd.Depth + 1, kind, entry.Tag, pathIndex);
            }
        }
    }

    internal void Follow(TiffIfd source, ulong targetOffset, string path, int depth, TiffIfdLinkKind kind, ushort tag, int index)
    {
        RequireDepth(depth, targetOffset);
        if (Active.Contains(targetOffset))
        {
            throw Cycle(targetOffset);
        }
        if (Visited.ContainsKey(targetOffset))
        {
            TiffIfd target = Visited[targetOffset];
            Links.Add(new TiffIfdLink(source.Path, path, target.Path, targetOffset, kind, tag, index, true));
            return;
        }

        Links.Add(new TiffIfdLink(source.Path, path, path, targetOffset, kind, tag, index, false));
        VisitChain(targetOffset, path, depth, path, false);
    }

    internal void RemoveActive(System.Collections.Generic.List<ulong> offsets)
    {
        for (int index = 0; index < offsets.Count; index++)
        {
            Active.Remove(offsets[index]);
        }
    }

    internal TiffIfd ReadIfd(ulong offset, string path, int depth)
    {
        long absoluteOffset = GetAbsoluteOffset(offset);
        int countSize = 2;
        int entrySize = 12;
        int nextSize = 4;
        if (Header.BigTiff)
        {
            countSize = 8;
            entrySize = 20;
            nextSize = 8;
        }

        byte[] countBytes = new byte[countSize];
        ReadAtAbsolute(absoluteOffset, countBytes);
        ulong count;
        if (Header.BigTiff)
        {
            count = EndianReader.ReadUInt64(countBytes, Header.ByteOrder);
        }
        else
        {
            count = EndianReader.ReadUInt16(countBytes, Header.ByteOrder);
        }

        long signedCount = CheckedMath.ToInt64(count);
        long entryBytes = CheckedMath.Multiply(signedCount, entrySize);
        long directoryBytes = CheckedMath.Add(entryBytes, nextSize);
        long entriesOffset = CheckedMath.Add(absoluteOffset, countSize);
        RequireRange(entriesOffset, directoryBytes);
        if (signedCount > (Container.Limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxTagCount - TotalTagCount)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The TIFF tag count exceeds the configured limit.", absoluteOffset);
        }
        CheckedMath.AllocationSize(signedCount, 8, Container.Limits);
        int entryCount = CheckedMath.ToInt32(signedCount);
        TiffEntry[] entries = new TiffEntry[entryCount];
        BoundedInput directoryInput = CreateWindow(entriesOffset, directoryBytes);
        for (int index = 0; index < entries.Length; index++)
        {
            entries[index] = TiffEntry.Parse(directoryInput, Container, Header);
            long valueBytes = entries[index].RawBytes.Length;
            if (valueBytes > Container.Limits.MaxMetadataBytes - TotalMetadataBytes)
            {
                throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The retained TIFF metadata exceeds the configured limit.", entries[index].EntryOffset);
            }
            TotalMetadataBytes += valueBytes;
        }
        TotalTagCount += signedCount;

        byte[] nextBytes = new byte[nextSize];
        directoryInput.ReadExactly(nextBytes);
        ulong nextOffset;
        if (Header.BigTiff)
        {
            nextOffset = EndianReader.ReadUInt64(nextBytes, Header.ByteOrder);
        }
        else
        {
            nextOffset = EndianReader.ReadUInt32(nextBytes, Header.ByteOrder);
        }
        return new TiffIfd(path, offset, absoluteOffset, depth, entries, nextOffset);
    }

    internal TiffIfdLinkKind GetPointerKind(TiffEntry entry)
    {
        bool pointerType = entry.FieldType == TiffFieldType.Long || entry.FieldType == TiffFieldType.Ifd || entry.FieldType == TiffFieldType.Long8 || entry.FieldType == TiffFieldType.Ifd8;
        if (entry.Tag == 330 && pointerType)
        {
            return TiffIfdLinkKind.SubIfd;
        }
        if (entry.Tag == 34665 && pointerType)
        {
            return TiffIfdLinkKind.Exif;
        }
        if (entry.Tag == 34853 && pointerType)
        {
            return TiffIfdLinkKind.Gps;
        }
        if (entry.Tag == 40965 && pointerType)
        {
            return TiffIfdLinkKind.Interop;
        }
        if (entry.FieldType == TiffFieldType.Ifd || entry.FieldType == TiffFieldType.Ifd8)
        {
            return TiffIfdLinkKind.Unknown;
        }
        return TiffIfdLinkKind.None;
    }

    internal ulong[] GetPointerOffsets(TiffEntry entry)
    {
        if (entry.FieldType == TiffFieldType.Long || entry.FieldType == TiffFieldType.Ifd)
        {
            uint[] values = entry.GetUInt32Values();
            ulong[] offsets = new ulong[values.Length];
            for (int index = 0; index < values.Length; index++)
            {
                offsets[index] = values[index];
            }
            return offsets;
        }
        if (entry.FieldType == TiffFieldType.Long8 || entry.FieldType == TiffFieldType.Ifd8)
        {
            return entry.GetUInt64Values();
        }
        return new ulong[0];
    }

    internal BoundedInput CreateWindow(long absoluteOffset, long length)
    {
        RequireRange(absoluteOffset, length);
        if (Container.UsesMemory)
        {
            long memoryOffset = CheckedMath.Add(Container.MemoryOffset, absoluteOffset - Container.Start);
            return BoundedInput.CreateWithAbsoluteOrigin(Container.Memory, memoryOffset, length, absoluteOffset, Container.Limits);
        }
        return BoundedInput.CreateStream(Container.Stream, absoluteOffset, length, Container.Limits);
    }

    internal void ReadAtAbsolute(long absoluteOffset, System.Span<byte> destination)
    {
        RequireRange(absoluteOffset, destination.Length);
        long relativeOffset = absoluteOffset - Container.Start;
        Container.ReadAt(relativeOffset, destination);
    }

    internal void RequireRange(long absoluteOffset, long length)
    {
        if (absoluteOffset < Container.Start)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "The TIFF IFD starts before the container window.", absoluteOffset);
        }
        long containerEnd = CheckedMath.Add(Container.Start, Container.Length);
        if (absoluteOffset > containerEnd || length > containerEnd - absoluteOffset)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "The TIFF IFD extends beyond the container window.", absoluteOffset);
        }
    }

    internal long GetAbsoluteOffset(ulong offset)
    {
        return CheckedMath.Add(Header.BaseOffset, CheckedMath.ToInt64(offset));
    }

    internal void RequireDepth(int depth, ulong offset)
    {
        if (depth > (Container.Limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxIfdDepth)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The TIFF IFD nesting depth exceeds the configured limit.", GetAbsoluteOffset(offset));
        }
    }

    internal void RequireLimits()
    {
        CheckedMath.RequirePositiveLimit((Container.Limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxIfdCount, "The configured TIFF IFD-count limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(Container.Limits.MaxIfdDepth, "The configured TIFF IFD-depth limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(Container.Limits.MaxTagCount, "The configured TIFF tag-count limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(Container.Limits.MaxMetadataBytes, "The configured metadata limit must be greater than zero.");
    }

    internal string NamedPath(string parent, string name, int index)
    {
        if (index == 0)
        {
            return parent + "/" + name;
        }
        return parent + "/" + name + index;
    }

    internal RawFormatException Cycle(ulong offset)
    {
        return new RawFormatException(RawDiagnosticCode.InvalidContainer, "The TIFF IFD graph contains a cycle.", GetAbsoluteOffset(offset));
    }
}
