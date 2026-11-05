namespace LibAll.Examples;

internal static class SampleFiles
{
    // MSBuild copies the sample inputs beside each executable, including during publish.
    public static string InputPath(string relativePath)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "data", relativePath);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Sample input is missing. Build the example to copy its data files.", path);
        }

        return path;
    }

    // Generated files go under the existing ignored artifacts directory.
    public static string OutputPath(string exampleName, string fileName)
    {
        string directory = Path.GetFullPath(Path.Combine("artifacts", exampleName));
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, fileName);
    }
}
