namespace asn1;

internal class RecoverySet
{
    internal string Name;
    internal RecoveryPoint[] Points;

    internal RecoverySet(string name, RecoveryPoint[] points)
    {
        if (name == null)
        {
            throw new System.ArgumentNullException("name");
        }

        if (points == null)
        {
            throw new System.ArgumentNullException("points");
        }

        this.Name = name;
        this.Points = new RecoveryPoint[points.Length];
        int index = 0;
        while (index < points.Length)
        {
            if (points[index] == null)
            {
                throw new System.ArgumentException("Recovery points cannot contain null.", "points");
            }

            this.Points[index] = points[index];
            index = index + 1;
        }
    }

    internal bool Contains(AsnToken token)
    {
        if (token == null)
        {
            throw new System.ArgumentNullException("token");
        }

        int index = 0;
        while (index < this.Points.Length)
        {
            if (this.Points[index].Matches(token))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
