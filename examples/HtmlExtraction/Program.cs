using html;

// Extract readable content from a complete local HTML page.
string source = File.ReadAllText(SampleFiles.InputPath("workshop.html"));
var api = new HtmlApi();
Console.WriteLine("Text extracted from HTML:");
Console.WriteLine(api.ToText(source));
Console.WriteLine("\nMarkdown extracted from HTML:");
Console.WriteLine(api.ToMarkdown(source));
