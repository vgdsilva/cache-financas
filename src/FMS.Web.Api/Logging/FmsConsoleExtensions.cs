using Microsoft.Extensions.Logging.Console;

namespace FMS.Web.Api.Logging;

public static class FmsConsoleExtensions
{
    /// <summary>Troca o console padrão pelo formato FMS e imprime o banner.</summary>
    public static WebApplicationBuilder AddFmsConsole(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(o => o.FormatterName = FmsConsoleFormatter.FormatterName);
        builder.Logging.AddConsoleFormatter<FmsConsoleFormatter, FmsConsoleFormatterOptions>();

        var options = new FmsConsoleFormatterOptions();
        builder.Configuration.GetSection("Logging:Console:FormatterOptions").Bind(options);
        FmsConsoleBanner.Print(Console.Out, options.UseColors, builder.Environment.EnvironmentName);

        return builder;
    }

    public static IApplicationBuilder UseFmsRequestLogging(this IApplicationBuilder app)
        => app.UseMiddleware<RequestLoggingMiddleware>();
}
