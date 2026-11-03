using System.Diagnostics;

namespace VGS.FinanceIA.Tests.Infrastructure;

/// <summary>
/// Fact que exige Docker (Testcontainers). Sem Docker, o teste é marcado como Skipped
/// em vez de falhar, para o <c>dotnet test</c> local continuar útil.
/// </summary>
public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (!DockerAvailability.IsAvailable)
        {
            Skip = "Docker indisponível: teste de integração (Testcontainers) ignorado.";
        }
    }
}

public static class DockerAvailability
{
    private static readonly Lazy<bool> _available = new(Probe);

    public static bool IsAvailable => _available.Value;

    private static bool Probe()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("docker", "info")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process is null)
            {
                return false;
            }

            if (!process.WaitForExit(TimeSpan.FromSeconds(10)))
            {
                process.Kill(entireProcessTree: true);
                return false;
            }

            return process.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
