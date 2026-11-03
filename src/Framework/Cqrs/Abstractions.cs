namespace VGS.FinanceIA.Framework.Cqrs;

/// <summary>Muda estado e não devolve dado.</summary>
public interface ICommand;

/// <summary>Muda estado e devolve um resultado (ex.: o id criado).</summary>
public interface ICommand<TResult>;

/// <summary>Só lê. Nunca altera estado.</summary>
public interface IQuery<TResult>;

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
