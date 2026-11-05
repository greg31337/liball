namespace chart;
/// <summary>Represents a categorical marker-shape scale.</summary>
public class ChartShapeScale : IChartShapeScale
{
    internal string[] Categories;
    internal ChartMarkerShape[] Shapes;

    /// <summary>Initializes a categorical marker-shape scale.</summary>
    /// <param name = "categories">The unique categories.</param>
    /// <param name = "shapes">The marker shape for each category.</param>
    public ChartShapeScale(string[] categories, ChartMarkerShape[] shapes)
    {
        ChartCategoryBandScale.ValidateCategories(categories);
        if (shapes == null)
        {
            throw new System.ArgumentNullException("shapes");
        }

        if (categories.Length != shapes.Length)
        {
            throw new System.ArgumentException("Shape scale categories and shapes must have the same length.");
        }

        int index = 0;
        while (index < categories.Length)
        {
            ChartRenderCommand.ValidateMarkerShape(shapes[index]);
            int previous = 0;
            while (previous < index)
            {
                if (categories[previous] == categories[index])
                {
                    throw new System.ArgumentException("Shape scale categories must be unique.");
                }

                previous = previous + 1;
            }

            index = index + 1;
        }

        Categories = ChartDataColumn.CopyStringArray(categories);
        Shapes = new ChartMarkerShape[shapes.Length];
        System.Array.Copy(shapes, Shapes, shapes.Length);
    }

    /// <summary>Maps a category to its configured marker shape.</summary>
    /// <param name = "category">The category to map.</param>
    /// <returns>The configured marker shape.</returns>
    public ChartMarkerShape Map(string category)
    {
        int index = 0;
        while (index < Categories.Length)
        {
            if (Categories[index] == category)
            {
                return Shapes[index];
            }

            index = index + 1;
        }

        throw new System.ArgumentException("Category was not found.");
    }
}
