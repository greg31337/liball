using System;
using System.Collections.Generic;

namespace geo;

internal class SegmentSpatialIndex
{
    internal DistanceSegmentFacet[] Segments;

    internal SegmentSpatialIndex(DistanceSegmentFacet[] segments)
    {
        if (segments == null)
        {
            throw new InvalidGeometryException(
                "Segment spatial index input is null.");
        }
        Segments = new DistanceSegmentFacet[segments.Length];
        int index = 0;
        while (index < segments.Length)
        {
            if (segments[index] == null)
            {
                throw new InvalidGeometryException(
                    "Segment spatial index contains a null segment.");
            }
            Segments[index] = segments[index];
            index++;
        }
        Array.Sort(Segments, (first, second) => CompareByMinimumX(first, second));
    }

    internal DistanceSegmentFacet[] Query(Envelope envelope)
    {
        bool empty = EnvelopeAlgorithms.ValidateAndCheckEmpty(envelope);
        if (empty) return [];
        int lower = 0;
        int upper = Segments.Length;
        while (lower < upper)
        {
            int middle = lower + (upper - lower) / 2;
            if (Segments[middle].Bounds.MinX <= envelope.MaxX)
            {
                lower = middle + 1;
            }
            else
            {
                upper = middle;
            }
        }
        List<DistanceSegmentFacet> matches =
            new List<DistanceSegmentFacet>();
        int index = 0;
        while (index < lower)
        {
            DistanceSegmentFacet segment = Segments[index];
            if (segment.Bounds.MaxX >= envelope.MinX &&
                segment.Bounds.MinY <= envelope.MaxY &&
                segment.Bounds.MaxY >= envelope.MinY)
            {
                matches.Add(segment);
            }
            index++;
        }
        matches.Sort((first, second) => first.Order.CompareTo(second.Order));
        return matches.ToArray();
    }

    internal DistanceSegmentFacet[] GetAllSegments()
    {
        DistanceSegmentFacet[] copy =
            new DistanceSegmentFacet[Segments.Length];
        int index = 0;
        while (index < Segments.Length)
        {
            copy[index] = Segments[index];
            index++;
        }
        Array.Sort(copy, (first, second) => first.Order.CompareTo(second.Order));
        return copy;
    }

    internal static int CompareByMinimumX(
        DistanceSegmentFacet first, DistanceSegmentFacet second)
    {
        if (first.Bounds.MinX < second.Bounds.MinX) return -1;
        if (first.Bounds.MinX > second.Bounds.MinX) return 1;
        return first.Order.CompareTo(second.Order);
    }
}
