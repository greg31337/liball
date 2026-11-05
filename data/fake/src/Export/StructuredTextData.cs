namespace fake;

internal static class StructuredTextData
{
    internal const string Source = "Project-authored structured-text fixtures based on RFC 8259, RFC 4180, XML 1.0, YAML 1.2, and inert ANSI SQL shapes";
    internal const string License = "project-license";

    internal static readonly string[] JsonScalars = new string[]
    {
        "\"Sample \\\"value\\\"\"",
        "-42.5",
        "true"
    };

    internal static readonly string[] JsonObjects = new string[]
    {
        "{\"name\":\"Sample\",\"active\":true}",
        "{\"note\":\"line one\\nline two\",\"count\":2}"
    };

    internal static readonly string[] JsonArrays = new string[]
    {
        "[\"sample\",42,true,null]",
        "[{\"id\":1},{\"id\":2}]"
    };

    internal static readonly string[] InvalidJson = new string[]
    {
        "{\"name\":\"sample}",
        "{\"name\" \"sample\"}",
        "{\"name\":\"sample\",}"
    };

    internal static readonly string[] CsvRows = new string[]
    {
        "sample,\"quoted \"\"value\"\"\",42",
        "alpha,\"line one\r\nline two\",true"
    };

    internal static readonly string[] CsvDocuments = new string[]
    {
        "name,note\r\nsample,\"quoted \"\"value\"\"\"\r\n",
        "name,count\r\nalpha,1\r\nbeta,2\r\n"
    };

    internal static readonly string[] InvalidCsv = new string[]
    {
        "name,note\r\nsample,\"unfinished",
        "name,note\r\nsample\r\n",
        "name,note\r\nsample,\"quoted\"x\r\n"
    };

    internal static readonly string[] XmlElements = new string[]
    {
        "<item id=\"42\">Sample &amp; example</item>",
        "<note>Quoted &quot;value&quot;</note>"
    };

    internal static readonly string[] XmlDocuments = new string[]
    {
        "<?xml version=\"1.0\" encoding=\"utf-8\"?><items><item id=\"1\">Sample</item><item id=\"2\">Example</item></items>",
        "<?xml version=\"1.0\" encoding=\"utf-8\"?><message><text>Sample &amp; example</text></message>"
    };

    internal static readonly string[] InvalidXml = new string[]
    {
        "<item name=\"sample></item>",
        "<item name=\"sample\"id=\"1\" />",
        "<item>sample</other>"
    };

    internal static readonly string[] YamlScalars = new string[]
    {
        "\"Sample: value\"",
        "42",
        "true"
    };

    internal static readonly string[] YamlSequences = new string[]
    {
        "- sample\n- \"quoted value\"\n- 42\n",
        "- name: alpha\n  active: true\n- name: beta\n  active: false\n"
    };

    internal static readonly string[] YamlMappings = new string[]
    {
        "name: sample\nnote: \"quoted: value\"\nactive: true\n",
        "item:\n  id: 42\n  label: example\n"
    };

    internal static readonly string[] InvalidYaml = new string[]
    {
        "name: \"sample",
        "name: sample\nstatus active\n",
        "items: [one, two}\n"
    };

    internal static readonly string[] InvalidSqlInserts = new string[]
    {
        "INSERT INTO \"items\" (\"name\") VALUES ('sample);",
        "INSERT INTO \"items\" (\"name\", \"status\") VALUES ('sample' 'ready');",
        "INSERT INTO \"items\" (\"name\") VALUES ('sample'];"
    };
}
