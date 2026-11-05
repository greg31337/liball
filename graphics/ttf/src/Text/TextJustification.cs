namespace ttf;

internal enum AppliedJustificationKind
{
    Space = 0,
    Tracking = 1,
    Jstf = 2,
    Kashida = 3
}

internal class AppliedJustification
{
    AppliedJustificationKind kind;
    int clusterBoundary;
    double expansion;

    internal AppliedJustification(
        AppliedJustificationKind kind,
        int clusterBoundary,
        double expansion)
    {
        this.kind = kind;
        this.clusterBoundary = clusterBoundary;
        this.expansion = expansion;
    }

    internal AppliedJustificationKind GetKind()
    {
        return kind;
    }

    internal int GetClusterBoundary()
    {
        return clusterBoundary;
    }

    internal double GetExpansion()
    {
        return expansion;
    }
}

internal class LineJustificationPlan
{
    int clusterStart;
    int clusterEnd;
    double naturalAdvance;
    double targetAdvance;
    double justifiedAdvance;
    double[] boundaryAdjustments;
    AppliedJustification[] applications;

    internal LineJustificationPlan(
        int clusterStart,
        int clusterEnd,
        double naturalAdvance,
        double targetAdvance,
        double justifiedAdvance,
        double[] boundaryAdjustments,
        AppliedJustification[] applications)
    {
        this.clusterStart = clusterStart;
        this.clusterEnd = clusterEnd;
        this.naturalAdvance = naturalAdvance;
        this.targetAdvance = targetAdvance;
        this.justifiedAdvance = justifiedAdvance;
        this.boundaryAdjustments = boundaryAdjustments;
        this.applications = applications;
    }

    internal int GetClusterStart()
    {
        return clusterStart;
    }

    internal int GetClusterEnd()
    {
        return clusterEnd;
    }

    internal double GetNaturalAdvance()
    {
        return naturalAdvance;
    }

    internal double GetTargetAdvance()
    {
        return targetAdvance;
    }

    internal double GetJustifiedAdvance()
    {
        return justifiedAdvance;
    }

    internal bool IsFullyJustified()
    {
        return justifiedAdvance == targetAdvance;
    }

    internal double GetBoundaryAdjustment(int clusterBoundary)
    {
        if (clusterBoundary < clusterStart || clusterBoundary > clusterEnd)
        {
            throw new System.ArgumentOutOfRangeException("clusterBoundary");
        }

        return boundaryAdjustments[clusterBoundary - clusterStart];
    }

    internal int GetApplicationCount()
    {
        return applications.Length;
    }

    internal AppliedJustification GetApplication(int index)
    {
        if (index < 0 || index >= applications.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return applications[index];
    }
}

internal class JustificationAllocation
{
    double[] amounts;
    double remaining;

    internal JustificationAllocation(double[] amounts, double remaining)
    {
        this.amounts = amounts;
        this.remaining = remaining;
    }

    internal double[] GetAmounts()
    {
        return amounts;
    }

    internal double GetRemaining()
    {
        return remaining;
    }
}

internal class TextJustificationPlannerContext
{
    GreedyLineFit line;
    bool lastLine;
    TextBreakMap? breaks;
    double targetAdvance;
    TextLayoutOptions? options;
    TextJustificationOpportunity[]? opportunities;
    double[] boundaryAdjustments;
    System.Collections.Generic.List<AppliedJustification> applications;
    double remaining;

    internal TextJustificationPlannerContext(
        GreedyLineFit line,
        bool lastLine,
        TextBreakMap? breaks,
        double targetAdvance,
        TextLayoutOptions? options,
        TextJustificationOpportunity[]? opportunities)
    {
        this.line = line;
        this.lastLine = lastLine;
        this.breaks = breaks;
        this.targetAdvance = targetAdvance;
        this.options = options;
        this.opportunities = opportunities;
        boundaryAdjustments =
            new double[line.GetClusterEnd() - line.GetClusterStart() + 1];
        applications =
            new System.Collections.Generic.List<AppliedJustification>();
        remaining = targetAdvance - line.GetAdvance();
    }

    internal LineJustificationPlan Build()
    {
        if (!ShouldJustify())
        {
            return CreatePlan();
        }

        System.ArgumentNullException.ThrowIfNull(options);
        ApplyUniformBoundaries(
            FindSpaceBoundaries(),
            options.GetMaximumSpaceExpansion(),
            AppliedJustificationKind.Space);
        ApplyUniformBoundaries(
            FindTrackingBoundaries(),
            options.GetMaximumTrackingExpansion(),
            AppliedJustificationKind.Tracking);
        if (options.UsesJstfJustification())
        {
            ApplyAdvanced(TextJustificationOpportunityKind.Jstf);
        }

        if (options.UsesKashidaJustification())
        {
            ApplyAdvanced(TextJustificationOpportunityKind.Kashida);
        }

        return CreatePlan();
    }

    bool ShouldJustify()
    {
        System.ArgumentNullException.ThrowIfNull(options);
        if (options.GetAlignment() != TextAlignment.Justify ||
            remaining <= 0 ||
            line.HasEllipsis())
        {
            return false;
        }

        if ((lastLine || line.EndedByHardBreak()) &&
            !options.ShouldJustifyLastLine())
        {
            return false;
        }

        return true;
    }

    System.Collections.Generic.List<int> FindSpaceBoundaries()
    {
        System.Collections.Generic.List<int> boundaries =
            new System.Collections.Generic.List<int>();
        int cluster = line.GetVisibleClusterStart();
        while (cluster < line.GetVisibleClusterEnd())
        {
            System.ArgumentNullException.ThrowIfNull(breaks);
            if (breaks.IsTrimmableWhitespace(cluster))
            {
                boundaries.Add(cluster + 1);
            }

            cluster++;
        }

        return boundaries;
    }

    System.Collections.Generic.List<int> FindTrackingBoundaries()
    {
        System.Collections.Generic.List<int> boundaries =
            new System.Collections.Generic.List<int>();
        int boundary = line.GetVisibleClusterStart() + 1;
        while (boundary < line.GetVisibleClusterEnd())
        {
            System.ArgumentNullException.ThrowIfNull(breaks);
            if (breaks.CanTrimAt(boundary) &&
                !breaks.IsTrimmableWhitespace(boundary - 1) &&
                !breaks.IsTrimmableWhitespace(boundary))
            {
                boundaries.Add(boundary);
            }

            boundary++;
        }

        return boundaries;
    }

    void ApplyUniformBoundaries(
        System.Collections.Generic.List<int> boundaries,
        double maximumPerBoundary,
        AppliedJustificationKind kind)
    {
        if (remaining <= 0 || boundaries.Count == 0 || maximumPerBoundary == 0)
        {
            return;
        }

        double amount = remaining / boundaries.Count;
        bool consumesRemaining = amount <= maximumPerBoundary;
        if (amount > maximumPerBoundary)
        {
            amount = maximumPerBoundary;
        }

        int index = 0;
        while (index < boundaries.Count)
        {
            AddApplication(kind, boundaries[index], amount);
            index++;
        }

        if (consumesRemaining)
        {
            remaining = 0;
        }
        else
        {
            Consume(amount * boundaries.Count);
        }
    }

    void ApplyAdvanced(TextJustificationOpportunityKind kind)
    {
        TextJustificationOpportunity[] matching = CopyMatching(kind);
        SortByPriority(matching);
        int groupStart = 0;
        while (groupStart < matching.Length && remaining > 0)
        {
            int priority = matching[groupStart].GetPriority();
            int groupEnd = groupStart + 1;
            while (groupEnd < matching.Length &&
                matching[groupEnd].GetPriority() == priority)
            {
                groupEnd++;
            }

            ApplyAdvancedGroup(matching, groupStart, groupEnd, kind);
            groupStart = groupEnd;
        }
    }

    TextJustificationOpportunity[] CopyMatching(
        TextJustificationOpportunityKind kind)
    {
        int count = 0;
        int index = 0;
        System.ArgumentNullException.ThrowIfNull(opportunities);
        while (index < opportunities.Length)
        {
            TextJustificationOpportunity opportunity = opportunities[index];
            if (opportunity.GetKind() == kind &&
                opportunity.GetMaximumExpansion() > 0 &&
                IsVisibleInternalBoundary(opportunity.GetClusterBoundary()))
            {
                count++;
            }

            index++;
        }

        TextJustificationOpportunity[] matching =
            new TextJustificationOpportunity[count];
        int resultIndex = 0;
        index = 0;
        while (index < opportunities.Length)
        {
            TextJustificationOpportunity opportunity = opportunities[index];
            if (opportunity.GetKind() == kind &&
                opportunity.GetMaximumExpansion() > 0 &&
                IsVisibleInternalBoundary(opportunity.GetClusterBoundary()))
            {
                matching[resultIndex] = opportunity;
                resultIndex++;
            }

            index++;
        }

        return matching;
    }

    bool IsVisibleInternalBoundary(int boundary)
    {
        return boundary > line.GetVisibleClusterStart() &&
            boundary < line.GetVisibleClusterEnd();
    }

    static void SortByPriority(TextJustificationOpportunity[] values)
    {
        int index = 1;
        while (index < values.Length)
        {
            TextJustificationOpportunity value = values[index];
            int destination = index;
            while (destination > 0 &&
                values[destination - 1].GetPriority() > value.GetPriority())
            {
                values[destination] = values[destination - 1];
                destination--;
            }

            values[destination] = value;
            index++;
        }
    }

    void ApplyAdvancedGroup(
        TextJustificationOpportunity[] values,
        int start,
        int end,
        TextJustificationOpportunityKind kind)
    {
        double[] capacities = new double[end - start];
        int index = start;
        while (index < end)
        {
            capacities[index - start] = values[index].GetMaximumExpansion();
            index++;
        }

        JustificationAllocation allocation = Allocate(capacities, remaining);
        double[] amounts = allocation.GetAmounts();
        index = 0;
        while (index < amounts.Length)
        {
            if (amounts[index] > 0)
            {
                AppliedJustificationKind appliedKind =
                    kind == TextJustificationOpportunityKind.Jstf ?
                    AppliedJustificationKind.Jstf :
                    AppliedJustificationKind.Kashida;
                AddApplication(
                    appliedKind,
                    values[start + index].GetClusterBoundary(),
                    amounts[index]);
            }

            index++;
        }

        remaining = allocation.GetRemaining();
    }

    static JustificationAllocation Allocate(double[] capacities, double available)
    {
        double[] amounts = new double[capacities.Length];
        bool[] complete = new bool[capacities.Length];
        int incompleteCount = capacities.Length;
        double remaining = available;
        double totalCapacity = 0;
        int capacityIndex = 0;
        while (capacityIndex < capacities.Length)
        {
            totalCapacity += capacities[capacityIndex];
            capacityIndex++;
        }

        bool canFill = totalCapacity >= available;
        while (incompleteCount > 0 && remaining > 0)
        {
            double share = remaining / incompleteCount;
            bool cappedAny = false;
            int index = 0;
            while (index < capacities.Length)
            {
                if (!complete[index])
                {
                    double capacityLeft = capacities[index] - amounts[index];
                    if (capacityLeft <= share)
                    {
                        amounts[index] += capacityLeft;
                        remaining -= capacityLeft;
                        complete[index] = true;
                        incompleteCount--;
                        cappedAny = true;
                    }
                }

                index++;
            }

            if (!cappedAny)
            {
                index = 0;
                while (index < capacities.Length)
                {
                    if (!complete[index])
                    {
                        amounts[index] += share;
                    }

                    index++;
                }

                remaining = 0;
            }
        }

        if (remaining < 0)
        {
            remaining = 0;
        }

        if (canFill)
        {
            remaining = 0;
        }

        return new JustificationAllocation(amounts, remaining);
    }

    void AddApplication(
        AppliedJustificationKind kind,
        int boundary,
        double amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int adjustmentIndex = boundary - line.GetClusterStart();
        boundaryAdjustments[adjustmentIndex] += amount;
        applications.Add(new AppliedJustification(kind, boundary, amount));
    }

    void Consume(double amount)
    {
        remaining -= amount;
        if (remaining < 0)
        {
            remaining = 0;
        }
    }

    LineJustificationPlan CreatePlan()
    {
        double justifiedAdvance = targetAdvance - remaining;
        if (justifiedAdvance < line.GetAdvance())
        {
            justifiedAdvance = line.GetAdvance();
        }

        return new LineJustificationPlan(
            line.GetClusterStart(),
            line.GetClusterEnd(),
            line.GetAdvance(),
            targetAdvance,
            justifiedAdvance,
            boundaryAdjustments,
            applications.ToArray());
    }
}

internal static class TextJustificationPlanner
{
    internal static LineJustificationPlan Plan(
        GreedyLineFit line,
        bool lastLine,
        TextBreakMap? breaks,
        double targetAdvance,
        TextLayoutOptions? options,
        TextJustificationOpportunity[]? opportunities)
    {
        ValidateInputs(
            line,
            breaks,
            targetAdvance,
            options,
            opportunities);
        TextJustificationPlannerContext context =
            new TextJustificationPlannerContext(
                line,
                lastLine,
                breaks,
                targetAdvance,
                options,
                opportunities);
        return context.Build();
    }

    static void ValidateInputs(
        GreedyLineFit line,
        TextBreakMap? breaks,
        double targetAdvance,
        TextLayoutOptions? options,
        TextJustificationOpportunity[]? opportunities)
    {
        if (line == null)
        {
            throw new System.ArgumentNullException("line");
        }

        if (breaks == null)
        {
            throw new System.ArgumentNullException("breaks");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (opportunities == null)
        {
            throw new System.ArgumentNullException("opportunities");
        }

        if (!double.IsFinite(targetAdvance) || targetAdvance < 0)
        {
            throw new System.ArgumentOutOfRangeException("targetAdvance");
        }

        if (line.GetClusterStart() < 0 ||
            line.GetClusterEnd() > breaks.GetClusterCount() ||
            line.GetClusterStart() > line.GetClusterEnd())
        {
            throw new System.ArgumentException(
                "The fitted line does not belong to the text break map.",
                "line");
        }

        int index = 0;
        while (index < opportunities.Length)
        {
            if (opportunities[index] == null)
            {
                throw new System.ArgumentException(
                    "A justification opportunity cannot be null.",
                    "opportunities");
            }

            if (opportunities[index].GetClusterBoundary() >
                breaks.GetClusterCount())
            {
                throw new System.ArgumentException(
                    "A justification opportunity is outside the text break map.",
                    "opportunities");
            }

            index++;
        }
    }
}
