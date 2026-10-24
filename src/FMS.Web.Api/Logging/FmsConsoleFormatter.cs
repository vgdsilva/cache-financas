using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace FMS.Web.Api.Logging;

/// <summary>
/// Formato do console: tempo desde a subida, status colorido, tag e mensagem.
/// <code>00:00:02.98  OK    HTTP      GET /health · 200 · 3 ms</code>
/// A tag é o último trecho da categoria do logger (ex.: Microsoft.Hosting.Lifetime → LIFETIME).
/// Na mensagem, o que vem depois do primeiro " · " sai esmaecido.
/// </summary>
public sealed class FmsConsoleFormatter : ConsoleFormatter, IDisposable
{
    public const string FormatterName = "fms";

    private const int    TagWidth  = 10;
    private const string Separator = " · ";

    private static readonly long StartTimestamp = Stopwatch.GetTimestamp();

    private readonly IDisposable? _optionsReloadToken;
    private FmsConsoleFormatterOptions _options;

    public FmsConsoleFormatter(IOptionsMonitor<FmsConsoleFormatterOptions> options) : base(FormatterName)
    {
        _options            = options.CurrentValue;
        _optionsReloadToken = options.OnChange(o => _options = o);
    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        string? message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (string.IsNullOrEmpty(message) && logEntry.Exception is null)
            return;

        bool colors = _options.UseColors;
        (string status, string statusColor) = Status(logEntry.LogLevel);

        TimeSpan elapsed = Stopwatch.GetElapsedTime(StartTimestamp);
        string   time    = $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}.{elapsed.Milliseconds / 10:00}";

        Paint(textWriter, colors, Ansi.Dim, time);
        textWriter.Write("  ");
        Paint(textWriter, colors, statusColor, status.PadRight(6));
        Paint(textWriter, colors, Ansi.Dim, Tag(logEntry.Category).PadRight(TagWidth));

        if (!string.IsNullOrEmpty(message))
        {
            int split = message.IndexOf(Separator, StringComparison.Ordinal);
            if (split < 0)
            {
                Paint(textWriter, colors, Ansi.Bold, message);
            }
            else
            {
                Paint(textWriter, colors, Ansi.Bold, message[..split]);
                Paint(textWriter, colors, Ansi.Dim, message[split..]);
            }
        }

        textWriter.WriteLine();

        if (logEntry.Exception is not null)
        {
            string indent = new(' ', time.Length + 2 + 6 + TagWidth);
            foreach (string line in logEntry.Exception.ToString().Split('\n'))
            {
                textWriter.Write(indent);
                Paint(textWriter, colors, Ansi.Red, line.TrimEnd('\r'));
                textWriter.WriteLine();
            }
        }
    }

    public void Dispose() => _optionsReloadToken?.Dispose();

    private static (string Text, string Color) Status(LogLevel level) => level switch
    {
        LogLevel.Information => ("OK",   Ansi.Green),
        LogLevel.Warning     => ("WARN", Ansi.Yellow),
        LogLevel.Error       => ("FAIL", Ansi.Red),
        LogLevel.Critical    => ("CRIT", Ansi.Bold + Ansi.Red),
        _                    => ("..",   Ansi.Cyan)
    };

    private static string Tag(string category)
    {
        int    dot = category.LastIndexOf('.');
        string tag = (dot >= 0 ? category[(dot + 1)..] : category).ToUpperInvariant();
        return tag.Length > TagWidth - 1 ? tag[..(TagWidth - 1)] : tag;
    }

    private static void Paint(TextWriter writer, bool colors, string color, string text)
    {
        if (!colors)
        {
            writer.Write(text);
            return;
        }

        writer.Write(color);
        writer.Write(text);
        writer.Write(Ansi.Reset);
    }
}
