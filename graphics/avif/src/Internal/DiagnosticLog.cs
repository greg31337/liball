using System.Collections.Generic;

namespace avif;

internal class DiagnosticContext
{
    internal List<string> Boxes;
    internal List<uint> Items;
    internal List<uint> Tracks;
    internal List<int> Frames;
    internal List<int> Tiles;
    internal List<string> Av1Syntax;
    internal long ByteOffset;
    internal int BitOffset;
    internal string StandardReference;

    internal DiagnosticContext()
    {
        Boxes = new List<string>();
        Items = new List<uint>();
        Tracks = new List<uint>();
        Frames = new List<int>();
        Tiles = new List<int>();
        Av1Syntax = new List<string>();
        ByteOffset = AvifDiagnostic.UnknownByteOffset;
        BitOffset = AvifDiagnostic.UnknownBitOffset;
        StandardReference = string.Empty;
    }

    internal AvifResultCode PushBox(string name)
    {
        if (string.IsNullOrEmpty(name) || name.IndexOf('/') >= 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        Boxes.Add(name);
        return AvifResultCode.Success;
    }

    internal AvifResultCode PopBox()
    {
        if (Boxes.Count == 0)
        {
            return AvifResultCode.InvalidState;
        }

        Boxes.RemoveAt(Boxes.Count - 1);
        return AvifResultCode.Success;
    }

    internal AvifResultCode PushItem(uint itemId)
    {
        if (itemId == AvifDiagnostic.UnknownEntityId)
        {
            return AvifResultCode.InvalidArgument;
        }

        Items.Add(itemId);
        return AvifResultCode.Success;
    }

    internal AvifResultCode PopItem()
    {
        if (Items.Count == 0)
        {
            return AvifResultCode.InvalidState;
        }

        Items.RemoveAt(Items.Count - 1);
        return AvifResultCode.Success;
    }

    internal AvifResultCode PushTrack(uint trackId)
    {
        if (trackId == AvifDiagnostic.UnknownEntityId)
        {
            return AvifResultCode.InvalidArgument;
        }

        Tracks.Add(trackId);
        return AvifResultCode.Success;
    }

    internal AvifResultCode PopTrack()
    {
        if (Tracks.Count == 0)
        {
            return AvifResultCode.InvalidState;
        }

        Tracks.RemoveAt(Tracks.Count - 1);
        return AvifResultCode.Success;
    }

    internal AvifResultCode PushFrame(int frameIndex)
    {
        if (frameIndex < 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        Frames.Add(frameIndex);
        return AvifResultCode.Success;
    }

    internal AvifResultCode PopFrame()
    {
        if (Frames.Count == 0)
        {
            return AvifResultCode.InvalidState;
        }

        Frames.RemoveAt(Frames.Count - 1);
        return AvifResultCode.Success;
    }

    internal AvifResultCode PushTile(int tileIndex)
    {
        if (tileIndex < 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        Tiles.Add(tileIndex);
        return AvifResultCode.Success;
    }

    internal AvifResultCode PopTile()
    {
        if (Tiles.Count == 0)
        {
            return AvifResultCode.InvalidState;
        }

        Tiles.RemoveAt(Tiles.Count - 1);
        return AvifResultCode.Success;
    }

    internal AvifResultCode PushAv1Syntax(string name)
    {
        if (string.IsNullOrEmpty(name) || name.IndexOf('/') >= 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        Av1Syntax.Add(name);
        return AvifResultCode.Success;
    }

    internal AvifResultCode PopAv1Syntax()
    {
        if (Av1Syntax.Count == 0)
        {
            return AvifResultCode.InvalidState;
        }

        Av1Syntax.RemoveAt(Av1Syntax.Count - 1);
        return AvifResultCode.Success;
    }

    internal AvifResultCode SetLocation(long byteOffset, int bitOffset)
    {
        if (byteOffset < AvifDiagnostic.UnknownByteOffset
            || bitOffset < AvifDiagnostic.UnknownBitOffset
            || bitOffset > 7)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (byteOffset == AvifDiagnostic.UnknownByteOffset
            && bitOffset != AvifDiagnostic.UnknownBitOffset)
        {
            return AvifResultCode.InvalidArgument;
        }

        ByteOffset = byteOffset;
        BitOffset = bitOffset;
        return AvifResultCode.Success;
    }

    internal void ClearLocation()
    {
        ByteOffset = AvifDiagnostic.UnknownByteOffset;
        BitOffset = AvifDiagnostic.UnknownBitOffset;
    }

    internal void SetStandardReference(string standardReference)
    {
        StandardReference = standardReference == null ? string.Empty : standardReference;
    }

    internal string GetBoxPath()
    {
        if (Boxes.Count == 0)
        {
            return string.Empty;
        }

        return "/" + string.Join("/", Boxes);
    }

    internal string GetAv1SyntaxPath()
    {
        if (Av1Syntax.Count == 0)
        {
            return string.Empty;
        }

        return string.Join("/", Av1Syntax);
    }

    internal uint GetItemId()
    {
        if (Items.Count == 0)
        {
            return AvifDiagnostic.UnknownEntityId;
        }

        return Items[Items.Count - 1];
    }

    internal uint GetTrackId()
    {
        if (Tracks.Count == 0)
        {
            return AvifDiagnostic.UnknownEntityId;
        }

        return Tracks[Tracks.Count - 1];
    }

    internal int GetFrameIndex()
    {
        if (Frames.Count == 0)
        {
            return AvifDiagnostic.UnknownIndex;
        }

        return Frames[Frames.Count - 1];
    }

    internal int GetTileIndex()
    {
        if (Tiles.Count == 0)
        {
            return AvifDiagnostic.UnknownIndex;
        }

        return Tiles[Tiles.Count - 1];
    }

    internal AvifDiagnostic CreateDiagnostic(
        AvifResultCode code,
        AvifDiagnosticSeverity severity,
        AvifDiagnosticCategory category,
        string message)
    {
        return AvifDiagnostic.Create(
            code,
            severity,
            category,
            message,
            ByteOffset,
            BitOffset,
            GetBoxPath(),
            GetItemId(),
            GetTrackId(),
            GetFrameIndex(),
            GetTileIndex(),
            GetAv1SyntaxPath(),
            StandardReference);
    }

    internal void Reset()
    {
        Boxes.Clear();
        Items.Clear();
        Tracks.Clear();
        Frames.Clear();
        Tiles.Clear();
        Av1Syntax.Clear();
        ClearLocation();
        StandardReference = string.Empty;
    }
}

internal class DiagnosticLog
{
    internal DiagnosticContext Context;
    internal List<AvifDiagnostic> Diagnostics;
    internal AvifResultCode Code;

    internal DiagnosticLog()
    {
        Context = new DiagnosticContext();
        Diagnostics = new List<AvifDiagnostic>();
        Code = AvifResultCode.Success;
    }

    internal AvifDiagnostic Add(
        AvifResultCode code,
        AvifDiagnosticSeverity severity,
        AvifDiagnosticCategory category,
        string message)
    {
        AvifDiagnostic diagnostic = Context.CreateDiagnostic(code, severity, category, message);
        Diagnostics.Add(diagnostic);
        if (Code == AvifResultCode.Success
            && (severity == AvifDiagnosticSeverity.Error || severity == AvifDiagnosticSeverity.Fatal))
        {
            Code = code;
        }

        return diagnostic;
    }

    internal AvifDiagnostic? Get(int index)
    {
        if (index < 0 || index >= Diagnostics.Count)
        {
            return null;
        }

        return Diagnostics[index];
    }

    internal void Clear()
    {
        Diagnostics.Clear();
        Context.Reset();
        Code = AvifResultCode.Success;
    }
}
