namespace VGS.FinanceIA.ViewModels;

public sealed class ErrorViewModel
{
    public string? RequestId { get; init; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
