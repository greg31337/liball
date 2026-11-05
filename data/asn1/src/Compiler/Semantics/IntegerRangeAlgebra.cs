namespace asn1;

internal class IntegerRangeAlgebra
{
    internal IntegerConstraintSet CreateUniverse()
    {
        return this.CreateSet([new IntegerInterval()]);
    }

    internal IntegerConstraintSet CreateEmpty()
    {
        return this.CreateSet([]);
    }

    internal IntegerConstraintSet CreateSingleton(System.Numerics.BigInteger value)
    {
        return this.CreateSet([new IntegerInterval
        {
            HasLowerBound = true,
            LowerBound = value,
            HasUpperBound = true,
            UpperBound = value
        }]);
    }

    internal IntegerConstraintSet CreateInterval(
        bool hasLowerBound,
        System.Numerics.BigInteger lowerBound,
        bool hasUpperBound,
        System.Numerics.BigInteger upperBound)
    {
        if (hasLowerBound && hasUpperBound && lowerBound > upperBound)
        {
            return this.CreateEmpty();
        }

        return this.CreateSet([new IntegerInterval
        {
            HasLowerBound = hasLowerBound,
            LowerBound = lowerBound,
            HasUpperBound = hasUpperBound,
            UpperBound = upperBound
        }]);
    }

    internal IntegerConstraintSet CreateFiniteDomain(System.Numerics.BigInteger[] values)
    {
        IntegerConstraintSet[] items = new IntegerConstraintSet[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            items[index] = this.CreateSingleton(values[index]);
            index = index + 1;
        }

        return this.Union(items);
    }

    internal IntegerConstraintSet Union(IntegerConstraintSet[] sets)
    {
        System.Collections.Generic.List<IntegerInterval> intervals =
            new System.Collections.Generic.List<IntegerInterval>();
        int setIndex = 0;
        while (setIndex < sets.Length)
        {
            int intervalIndex = 0;
            while (intervalIndex < sets[setIndex].Intervals.Length)
            {
                intervals.Add(this.CopyInterval(
                    sets[setIndex].Intervals[intervalIndex]));
                intervalIndex = intervalIndex + 1;
            }

            setIndex = setIndex + 1;
        }

        return this.CreateSet(intervals.ToArray());
    }

    internal IntegerConstraintSet Intersect(IntegerConstraintSet[] sets)
    {
        if (sets.Length == 0)
        {
            return this.CreateUniverse();
        }

        IntegerConstraintSet result = this.CopySet(sets[0]);
        int setIndex = 1;
        while (setIndex < sets.Length)
        {
            System.Collections.Generic.List<IntegerInterval> intersections =
                new System.Collections.Generic.List<IntegerInterval>();
            int leftIndex = 0;
            while (leftIndex < result.Intervals.Length)
            {
                int rightIndex = 0;
                while (rightIndex < sets[setIndex].Intervals.Length)
                {
                    IntegerInterval? interval = this.IntersectIntervals(
                        result.Intervals[leftIndex],
                        sets[setIndex].Intervals[rightIndex]);
                    if (interval != null)
                    {
                        intersections.Add(interval);
                    }

                    rightIndex = rightIndex + 1;
                }

                leftIndex = leftIndex + 1;
            }

            result = this.CreateSet(intersections.ToArray());
            setIndex = setIndex + 1;
        }

        return result;
    }

    internal IntegerConstraintSet Except(
        IntegerConstraintSet source,
        IntegerConstraintSet excluded)
    {
        System.Collections.Generic.List<IntegerInterval> remaining =
            new System.Collections.Generic.List<IntegerInterval>();
        int sourceIndex = 0;
        while (sourceIndex < source.Intervals.Length)
        {
            System.Collections.Generic.List<IntegerInterval> pieces =
                new System.Collections.Generic.List<IntegerInterval>();
            pieces.Add(this.CopyInterval(source.Intervals[sourceIndex]));
            int excludedIndex = 0;
            while (excludedIndex < excluded.Intervals.Length)
            {
                System.Collections.Generic.List<IntegerInterval> next =
                    new System.Collections.Generic.List<IntegerInterval>();
                int pieceIndex = 0;
                while (pieceIndex < pieces.Count)
                {
                    next.AddRange(this.SubtractInterval(
                        pieces[pieceIndex],
                        excluded.Intervals[excludedIndex]));
                    pieceIndex = pieceIndex + 1;
                }

                pieces = next;
                excludedIndex = excludedIndex + 1;
            }

            remaining.AddRange(pieces);
            sourceIndex = sourceIndex + 1;
        }

        return this.CreateSet(remaining.ToArray());
    }

    internal IntegerConstraintSet Complement(IntegerConstraintSet excluded)
    {
        return this.Except(this.CreateUniverse(), excluded);
    }

    internal IntegerConstraintSet CreateSet(IntegerInterval[] source)
    {
        System.Collections.Generic.List<IntegerInterval> intervals =
            new System.Collections.Generic.List<IntegerInterval>();
        int index = 0;
        while (index < source.Length)
        {
            IntegerInterval interval = source[index];
            if (!interval.HasLowerBound
                || !interval.HasUpperBound
                || interval.LowerBound <= interval.UpperBound)
            {
                intervals.Add(this.CopyInterval(interval));
            }

            index = index + 1;
        }

        this.SortIntervals(intervals);
        System.Collections.Generic.List<IntegerInterval> merged =
            new System.Collections.Generic.List<IntegerInterval>();
        index = 0;
        while (index < intervals.Count)
        {
            IntegerInterval current = intervals[index];
            if (merged.Count == 0)
            {
                merged.Add(current);
            }
            else
            {
                IntegerInterval previous = merged[merged.Count - 1];
                bool connected = !previous.HasUpperBound
                    || !current.HasLowerBound
                    || current.LowerBound <= previous.UpperBound + System.Numerics.BigInteger.One;
                if (connected)
                {
                    if (!previous.HasUpperBound || !current.HasUpperBound)
                    {
                        previous.HasUpperBound = false;
                    }
                    else if (current.UpperBound > previous.UpperBound)
                    {
                        previous.UpperBound = current.UpperBound;
                    }
                }
                else
                {
                    merged.Add(current);
                }
            }

            index = index + 1;
        }

        IntegerConstraintSet result = new IntegerConstraintSet
        {
            Intervals = merged.ToArray(),
            IsEmpty = merged.Count == 0,
            HasFiniteCardinality = true
        };
        index = 0;
        while (index < result.Intervals.Length)
        {
            IntegerInterval interval = result.Intervals[index];
            if (!interval.HasLowerBound || !interval.HasUpperBound)
            {
                result.HasFiniteCardinality = false;
            }
            else
            {
                result.FiniteCardinality = result.FiniteCardinality
                    + interval.UpperBound - interval.LowerBound
                    + System.Numerics.BigInteger.One;
            }

            index = index + 1;
        }

        return result;
    }

    internal void SortIntervals(
        System.Collections.Generic.List<IntegerInterval> intervals)
    {
        int index = 1;
        while (index < intervals.Count)
        {
            IntegerInterval current = intervals[index];
            int position = index;
            while (position > 0
                && this.CompareLower(current, intervals[position - 1]) < 0)
            {
                intervals[position] = intervals[position - 1];
                position = position - 1;
            }

            intervals[position] = current;
            index = index + 1;
        }
    }

    internal int CompareLower(IntegerInterval left, IntegerInterval right)
    {
        if (!left.HasLowerBound)
        {
            return right.HasLowerBound ? -1 : 0;
        }

        if (!right.HasLowerBound)
        {
            return 1;
        }

        return left.LowerBound.CompareTo(right.LowerBound);
    }

    internal IntegerInterval? IntersectIntervals(
        IntegerInterval left,
        IntegerInterval right)
    {
        bool hasLower = left.HasLowerBound || right.HasLowerBound;
        System.Numerics.BigInteger lower = System.Numerics.BigInteger.Zero;
        if (left.HasLowerBound && right.HasLowerBound)
        {
            lower = left.LowerBound > right.LowerBound
                ? left.LowerBound
                : right.LowerBound;
        }
        else if (left.HasLowerBound)
        {
            lower = left.LowerBound;
        }
        else if (right.HasLowerBound)
        {
            lower = right.LowerBound;
        }

        bool hasUpper = left.HasUpperBound || right.HasUpperBound;
        System.Numerics.BigInteger upper = System.Numerics.BigInteger.Zero;
        if (left.HasUpperBound && right.HasUpperBound)
        {
            upper = left.UpperBound < right.UpperBound
                ? left.UpperBound
                : right.UpperBound;
        }
        else if (left.HasUpperBound)
        {
            upper = left.UpperBound;
        }
        else if (right.HasUpperBound)
        {
            upper = right.UpperBound;
        }

        if (hasLower && hasUpper && lower > upper)
        {
            return null;
        }

        return new IntegerInterval
        {
            HasLowerBound = hasLower,
            LowerBound = lower,
            HasUpperBound = hasUpper,
            UpperBound = upper
        };
    }

    internal IntegerInterval[] SubtractInterval(
        IntegerInterval source,
        IntegerInterval excluded)
    {
        IntegerInterval? overlap = this.IntersectIntervals(source, excluded);
        if (overlap == null)
        {
            return [this.CopyInterval(source)];
        }

        System.Collections.Generic.List<IntegerInterval> result =
            new System.Collections.Generic.List<IntegerInterval>();
        if (overlap.HasLowerBound
            && (!source.HasLowerBound || source.LowerBound < overlap.LowerBound))
        {
            result.Add(new IntegerInterval
            {
                HasLowerBound = source.HasLowerBound,
                LowerBound = source.LowerBound,
                HasUpperBound = true,
                UpperBound = overlap.LowerBound - System.Numerics.BigInteger.One
            });
        }

        if (overlap.HasUpperBound
            && (!source.HasUpperBound || source.UpperBound > overlap.UpperBound))
        {
            result.Add(new IntegerInterval
            {
                HasLowerBound = true,
                LowerBound = overlap.UpperBound + System.Numerics.BigInteger.One,
                HasUpperBound = source.HasUpperBound,
                UpperBound = source.UpperBound
            });
        }

        return result.ToArray();
    }

    internal IntegerInterval CopyInterval(IntegerInterval source)
    {
        return new IntegerInterval
        {
            HasLowerBound = source.HasLowerBound,
            LowerBound = source.LowerBound,
            HasUpperBound = source.HasUpperBound,
            UpperBound = source.UpperBound
        };
    }

    internal IntegerConstraintSet CopySet(IntegerConstraintSet source)
    {
        IntegerInterval[] intervals = new IntegerInterval[source.Intervals.Length];
        int index = 0;
        while (index < intervals.Length)
        {
            intervals[index] = this.CopyInterval(source.Intervals[index]);
            index = index + 1;
        }

        return this.CreateSet(intervals);
    }
}
