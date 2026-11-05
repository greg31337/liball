namespace html;
/// <summary>Stores the three components of a CSS selector specificity value.</summary>
public class HtmlSelectorSpecificity
{
    /// <summary>The number of ID specificity components.</summary>
    public int IdCount;
    /// <summary>The number of class, attribute, and pseudo-class components.</summary>
    public int ClassCount;
    /// <summary>The number of type and pseudo-element components.</summary>
    public int TypeCount;

    internal HtmlSelectorSpecificity(int idCountValue,
        int classCountValue, int typeCountValue)
    {
        this.IdCount = idCountValue;
        this.ClassCount = classCountValue;
        this.TypeCount = typeCountValue;
    }

    internal int GetIdCount()
    {
        return this.IdCount;
    }

    internal int GetClassCount()
    {
        return this.ClassCount;
    }

    internal int GetTypeCount()
    {
        return this.TypeCount;
    }

    internal int CompareTo(HtmlSelectorSpecificity other)
    {
        if (other == null)
        {
            return 1;
        }

        int result = this.IdCount.CompareTo(other.IdCount);
        if (result == 0)
        {
            result = this.ClassCount.CompareTo(other.ClassCount);
        }

        return result == 0
            ? this.TypeCount.CompareTo(other.TypeCount)
            : result;
    }
}
