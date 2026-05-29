using Vesia.Dispatch.Enums;

namespace Vesia.Dispatch;

public class DispatchOptions
{
    public LoggingMode CommandLogging { get; set; } = LoggingMode.OptIn;
    public LoggingMode QueryLogging { get; set; } = LoggingMode.OptIn;
}