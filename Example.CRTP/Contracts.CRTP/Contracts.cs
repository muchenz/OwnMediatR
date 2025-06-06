using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Example.CRTP.Contracts.CRTP; //Curiously Recurring Template Pattern
public interface IEvent<TEvent> where TEvent : IEvent<TEvent> { }

public interface IEventHandler<in TEvent> where TEvent : IEvent<TEvent>
{

    Task Handle(TEvent @event);

}


public interface ICommand<TCommand> where TCommand : ICommand<TCommand> { }
public interface ICommand<TCommand, TResult> where TCommand : ICommand<TCommand, TResult> { }

public interface ICommandHandler<in TCommand> where TCommand : ICommand<TCommand>
{

    Task Handle(TCommand command);

}
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TCommand, TResult>
{

    Task<TResult> Handle(TCommand command);
}

public interface IQuery<TQuery, TResult> where TQuery : IQuery<TQuery, TResult> { }

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TQuery, TResult>
{

    Task<TResult> Handle(TQuery query);
}