using System.Globalization;
using System.Text;

namespace docx;

internal class DocxNumberingImportMap
{
    internal Dictionary<string, string> AbstractNumberingIds;
    internal Dictionary<string, string> NumberingIds;

    internal DocxNumberingImportMap()
    {
        this.AbstractNumberingIds = new Dictionary<string, string>();
        this.NumberingIds = new Dictionary<string, string>();
    }

    internal void AddAbstractNumberingId(string sourceId, string targetId)
    {
        if (sourceId == null)
        {
            throw new ArgumentNullException("sourceId");
        }

        if (targetId == null)
        {
            throw new ArgumentNullException("targetId");
        }

        this.AbstractNumberingIds[sourceId] = targetId;
    }

    internal void AddNumberingId(string sourceId, string targetId)
    {
        if (sourceId == null)
        {
            throw new ArgumentNullException("sourceId");
        }

        if (targetId == null)
        {
            throw new ArgumentNullException("targetId");
        }

        this.NumberingIds[sourceId] = targetId;
    }

    internal bool ContainsAbstractNumberingId(string sourceId)
    {
        if (sourceId == null)
        {
            throw new ArgumentNullException("sourceId");
        }

        return this.AbstractNumberingIds.ContainsKey(sourceId);
    }

    internal bool ContainsNumberingId(string sourceId)
    {
        if (sourceId == null)
        {
            throw new ArgumentNullException("sourceId");
        }

        return this.NumberingIds.ContainsKey(sourceId);
    }

    internal string GetAbstractNumberingId(string sourceId)
    {
        if (sourceId == null)
        {
            throw new ArgumentNullException("sourceId");
        }

        if (this.AbstractNumberingIds.ContainsKey(sourceId))
        {
            return this.AbstractNumberingIds[sourceId];
        }

        return sourceId;
    }

    internal string GetNumberingId(string sourceId)
    {
        if (sourceId == null)
        {
            throw new ArgumentNullException("sourceId");
        }

        if (this.NumberingIds.ContainsKey(sourceId))
        {
            return this.NumberingIds[sourceId];
        }

        return sourceId;
    }

    internal int AbstractNumberingCount()
    {
        return this.AbstractNumberingIds.Count;
    }

    internal int NumberingCount()
    {
        return this.NumberingIds.Count;
    }
}
