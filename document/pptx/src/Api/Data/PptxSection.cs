using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Represents section data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxSection
{
    internal PptxPresentation? Presentation;
    /// <summary>
    /// Stores the name associated with this object.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the id used to identify or connect related presentation data.
    /// </summary>
    public int Id;
    /// <summary>
    /// Contains the slide ids in package or presentation order.
    /// </summary>
    public List<int> SlideIds;

    /// <summary>
    /// Initializes a new section with safe defaults or the supplied values.
    /// </summary>
    public PptxSection()
    {
        Presentation = null;
        Name = "";
        Id = 0;
        SlideIds = new List<int>();
    }
    internal void AddSlideId(int slideId)
    {
        if (Presentation != null)
        {
            Presentation.AddSlideToSectionValue(this, slideId);
            return;
        }

        SlideIds.Add(slideId);
    }
    internal int SlideCount()
    {
        return SlideIds.Count;
    }
    internal int GetSlideId(int index)
    {
        if (index < 0 || index >= SlideIds.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return SlideIds[index];
    }
    internal void Rename(string name)
    {
        if (Presentation != null)
        {
            Presentation.RenameSection(this, name);
            return;
        }

        Name = PptxDiagnostic.CleanText(name);
    }
    internal void SetSlideIds(List<int> slideIds)
    {
        if (slideIds == null)
        {
            throw new System.ArgumentNullException("slideIds");
        }

        if (Presentation != null)
        {
            Presentation.SetSectionSlideIds(this, slideIds);
            return;
        }

        SlideIds.Clear();
        for (int index = 0; index < slideIds.Count; index++)
        {
            SlideIds.Add(slideIds[index]);
        }
    }
    internal void InsertSlideId(int index, int slideId)
    {
        if (Presentation != null)
        {
            Presentation.InsertSectionSlideId(this, index, slideId);
            return;
        }

        if (index < 0 || index > SlideIds.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        SlideIds.Insert(index, slideId);
    }
    internal void RemoveSlideIdAt(int index)
    {
        if (Presentation != null)
        {
            Presentation.RemoveSectionSlideIdAt(this, index);
            return;
        }

        if (index < 0 || index >= SlideIds.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        SlideIds.RemoveAt(index);
    }
    internal bool Remove()
    {
        if (Presentation == null)
        {
            return false;
        }

        return Presentation.RemoveSection(this);
    }
}
