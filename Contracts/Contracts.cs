namespace Contracts;

public interface IEvent { }
public interface IEventHandler<in IEvent>
{
    Task Handle(IEvent @event);

}
public interface ICommand { }
public interface ICommand<in TResult> { }

public interface ICommandHandler<in ICommand>
{
    Task Handle(ICommand command);

}
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{

    Task<TResult> Handle(TCommand command);
}


public interface IQuery<in TResult> { }

public interface QueryHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{

    Task<TResult> Handle(TCommand command);
}
