namespace VGS.FinanceIA.Framework.Cqrs;

/// <summary>
/// Violação de uma regra de negócio (ex.: usuário duplicado). A mensagem é pensada para o usuário final,
/// então a UI pode mostrá-la. Qualquer outra exceção é erro de programação/infra e não deve ser exibida.
/// </summary>
public class BusinessRuleException(string message) : InvalidOperationException(message);
