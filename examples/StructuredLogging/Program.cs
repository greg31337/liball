using log;

var factory = new LogFactory();
var logger = factory.CreateNamedLoggerWithLevel("orders", LogLevel.Info);

// The template includes its own newline so each event occupies one console line.
var formatter = factory.CreateOutputTemplateFormatter("{Timestamp} [{Level}] {Logger}: {Message}{Fields}{NewLine}");
logger.SetSinks(new[] { factory.CreateTextWriterSink(Console.Out, formatter, true) });

// A child logger attaches the order ID to every event without repeating it in messages.
var orderLogger = logger.WithField("orderId", "ORD-1042");
Console.WriteLine("Structured messages with an order context");
orderLogger.Debug("This debug message is filtered by the Info minimum level.");
orderLogger.InfoWithValues("Accepted {ItemCount} items for {Customer}", new object?[] { 3, "Ada" });
orderLogger.WarnWithValues("Only {Remaining} item left in stock", new object?[] { 1 });
orderLogger.Info("Order ready for collection");
orderLogger.Flush();
if (orderLogger.GetFailureCount() != 0)
{
    throw new InvalidOperationException("A log event could not be written.");
}

Console.WriteLine(
    $"Written events: {orderLogger.GetWrittenCount()}; debug enabled: {orderLogger.IsEnabled(LogLevel.Debug)}");
orderLogger.Close();
