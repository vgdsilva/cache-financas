namespace FMS.Web.Api.Logging;

/// <summary>Banner impresso uma vez na subida da API, com degradê azul → amarelo.</summary>
internal static class FmsConsoleBanner
{
    private static readonly (int R, int G, int B) From = (70, 130, 220);
    private static readonly (int R, int G, int B) To   = (235, 195, 60);

    public static void Print(TextWriter writer, bool colors, string environment)
    {
        writer.WriteLine(colors
            ? $"{Ansi.Dim}cache-{Ansi.Reset}{Ansi.Bold}financas{Ansi.Reset} {Ansi.Red}api{Ansi.Reset}{Ansi.Dim} · {environment}{Ansi.Reset}"
            : $"cache-financas api · {environment}");

        writer.WriteLine();
        writer.WriteLine(colors ? $"{Ansi.Green}{new string('━', 72)}{Ansi.Reset}" : new string('-', 72));
        writer.WriteLine();
    }

    private static int Lerp(int a, int b, double t) => (int)Math.Round(a + (b - a) * t);
}
