namespace FMS.Web.Api.Logging;

/// <summary>Sequências ANSI usadas pelo console da API.</summary>
internal static class Ansi
{
    public const string Reset  = "\x1b[0m";
    public const string Bold   = "\x1b[1m";
    public const string Dim    = "\x1b[90m";
    public const string Green  = "\x1b[32m";
    public const string Yellow = "\x1b[33m";
    public const string Red    = "\x1b[31m";
    public const string Cyan   = "\x1b[36m";

    public static string Rgb(int r, int g, int b) => $"\x1b[38;2;{r};{g};{b}m";
}
