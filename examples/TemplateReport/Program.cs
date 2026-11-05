using System.Text.Json;
using template;

// Typed input records keep the JSON file separate from the template engine's value model.
string input = File.ReadAllText(SampleFiles.InputPath("opening-checklist.json"));
var checklist = JsonSerializer.Deserialize<Checklist>(
    input, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
    ?? throw new InvalidOperationException("Missing opening checklist.");
var values = new TemplateValueFactory();
var tasks = new List<TemplateValue>();
foreach (var task in checklist.Tasks)
{
    tasks.Add(values.CreateMap(new Dictionary<string, TemplateValue>
    {
        ["title"] = values.CreateString(task.Title),
        ["done"] = values.CreateBoolean(task.Done)
    }));
}

var model = values.CreateMap(new Dictionary<string, TemplateValue>
{
    ["name"] = values.CreateString(checklist.Owner),
    ["project"] = values.CreateString(checklist.Project),
    ["tasks"] = values.CreateList(tasks)
});

// Includes live in the registry; missing model values should fail visibly in this example.
var registry = new TemplateRegistry();
registry.RegisterTemplate("footer", "Prepared for {{ name }}.");
var renderer = new TemplateRenderer();
renderer.Configure(new TemplateOptions { StrictMissingValues = true }, registry, new TemplateMemberResolver());
string report = File.ReadAllText(SampleFiles.InputPath("checklist.template"));
Console.WriteLine(renderer.RenderToString(report, model));

internal sealed record Checklist(string Owner, string Project, ChecklistTask[] Tasks);

internal sealed record ChecklistTask(string Title, bool Done);
