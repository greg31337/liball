using markdown;

// The GFM pipeline handles the report table and task list.
string source = File.ReadAllText(SampleFiles.InputPath("weekly-report.md"));
var markdown = new Markdown();
var pipeline = markdown.CreateGfmPipeline();
var document = markdown.ParseSourceWithPipeline(source, pipeline);
Console.WriteLine("GitHub-flavored Markdown -> HTML");
Console.WriteLine(markdown.RenderHtmlDocumentWithPipeline(document, pipeline));
