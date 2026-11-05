using System.Text;
using email;

// A comma inside a quoted display name does not separate two mailboxes.
const string input = "\"Lovelace, Ada\" <ada@example.com>, Grace Hopper <grace@example.org>";
var parsed = new EmailAddressParser().ParseListWithInputAndMode(
    Encoding.UTF8.GetBytes(input),
    EmailAddressParseMode.Strict);
if (!parsed.IsSuccess() || !parsed.IsComplete())
{
    throw new InvalidOperationException("Address parsing failed.");
}

var list = parsed.GetAddressList();
Console.WriteLine($"Mailboxes: {list.GetMailboxCount()}");
foreach (var entry in list.GetAll())
{
    var mailbox = entry.GetMailbox() ?? throw new InvalidOperationException("Expected a mailbox.");
    Console.WriteLine($"  {mailbox.GetDisplayName()} -> {mailbox.GetLocalPart()}@{mailbox.GetDomain()}");
}

// Let the formatter handle quoting when converting the parsed list back to text.
var output = new EmailAddressFormatter().FormatListWithList(list);
if (!output.IsSuccess())
{
    throw new InvalidOperationException("Address formatting failed.");
}

Console.WriteLine($"Formatted: {Encoding.UTF8.GetString(output.GetBytes())}");
