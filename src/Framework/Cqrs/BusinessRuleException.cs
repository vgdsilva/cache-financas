namespace VGS.FinanceIA.Framework.Cqrs;

/// <summary>
/// Violação de uma regra de negócio (ex.: usuário duplicado). A mensagem é pensada para o usuário final,
/// então a UI pode mostrá-la. Qualquer outra exceção é erro de programação/infra e não deve ser exibida.
/// <paramref name="field"/> (opcional) é o campo do formulário a que o erro se refere, para a UI mostrá-lo junto dele.
/// </summary>
public class BusinessRuleException(string message, string? field = null) : InvalidOperationException(message)
{
    public string? Field { get; } = field;
}
