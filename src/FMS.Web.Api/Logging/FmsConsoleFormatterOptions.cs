using Microsoft.Extensions.Logging.Console;

namespace FMS.Web.Api.Logging;

public sealed class FmsConsoleFormatterOptions : ConsoleFormatterOptions
{
    /// <summary>
    /// Default: colorido só quando a saída é um terminal. Use Enabled para manter as cores
    /// no "docker compose logs".
    /// </summary>
    public LoggerColorBehavior ColorBehavior { get; set; } = LoggerColorBehavior.Default;

    public bool UseColors => ColorBehavior switch
    {
        LoggerColorBehavior.Enabled  => true,
        LoggerColorBehavior.Disabled => false,
        _                            => !Console.IsOutputRedirected
    };
}
