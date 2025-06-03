using Contracts;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace OwnMediatRv2;




//public record GetAlaWrapperCommand(int Age) : ICommandWrapper<int>;

public class Dispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    //public async Task<TResult> Send2<TResult>(ICommand<TResult> command)
    //{
    //    var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));

    //    dynamic handler = _serviceProvider.GetRequiredService(handlerType);

    //    var result = await handler.Handle((dynamic)command);
    //    return result;
    //}


    public Task<TResult> Send<TResult>(ICommand<TResult> command)
    {
        // var handler = _serviceProvider.GetService(typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult)));
        // if (handler is null) throw new NullReferenceException("Handler is null");
        //var result = await InvokeLabdaWithResultAsync(handler, command);
        //var task = (Task<TResult>)InvokeLamdaAsync(handler, command);

        var res = GeneratedDispatchers.Dispatcher.Send(command, _serviceProvider);

        return res;
    }



    public async Task SendEventsGenerated(IEnumerable<IEvent> evets)
    {

        foreach (var evet in evets)
        {
            //var handlers = _serviceProvider.GetServices(typeof(IEventHandler<>).MakeGenericType(evet.GetType()));

            //foreach (var handler in handlers)
            //{
            //    if (handler is null) continue;
            //   // await InvokeLamdaAsync(handler, evet);
            //    await InvokeCreateDelegateAsync(handler, evet);
            //}

            await GeneratedDispatchers.Dispatcher.Publish(evet, _serviceProvider);
        }
    }

    public async Task SendEventsWrapped(IEnumerable<IEvent> evets)
    {
        foreach (var evet in evets)
        {
            await SendEventsWrapped(evet);
        }
    }
    public async Task SendEventsWrapped(IEvent e)
    {

            var eventType = e.GetType();

            var wrapper = _wappercache.GetOrAdd(eventType, eventTypekey =>
            {

                var typedWrapper = typeof(HandlerEventWrapprer<>).MakeGenericType(eventTypekey);

                var instance = Activator.CreateInstance(typedWrapper, []);

                return (HandlerEventWrapprer)instance;

            });

            await wrapper.Handle(e, _serviceProvider);

        
    }




    private static readonly ConcurrentDictionary<Type, HandlerEventWrapprer> _wappercache
    = new();
    public abstract class HandlerEventWrapprer
    {
        public abstract Task Handle(IEvent @event, IServiceProvider serviceProvider);
    }

    public class HandlerEventWrapprer<T> : HandlerEventWrapprer where T : IEvent
    {

        public HandlerEventWrapprer()
        {
        }
        public async Task HandleTyped(T @event, IServiceProvider serviceProvider)
        {
            var handlerType = typeof(IEventHandler<T>);

            var handlers = (IEnumerable<IEventHandler<T>>)serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {

                await handler.Handle(@event);
            }
        }


        public override Task Handle(IEvent @event, IServiceProvider serviceProvider)
        {
            return HandleTyped((T)@event, serviceProvider);
        }
    }

    //-----------------------  la jovovoth - slow !!!
    private static readonly ConcurrentDictionary<Type, HandlerEventWrapprer2> _wappercache2 = new();


    public async Task SendEventsWrapped2(IEnumerable<IEvent> evets)
    {

        foreach (var evet in evets)
        {

            var eventType = evet.GetType();
            var handlersType = typeof(IEventHandler<>).MakeGenericType(eventType);


            var handlers = _serviceProvider.GetServices(handlersType);


            foreach (var handler in handlers)
            {

                var wrapper = HandlerEventWrapprer2.Create(evet, handler, eventType);

                await wrapper.Handle(evet);
            }


        }
    }


    public abstract class HandlerEventWrapprer2
    {
        public abstract Task Handle(IEvent @event);

        public static HandlerEventWrapprer2 Create(IEvent @event, object handler, Type eventType)
        {
            //var eventType = @event.GetType();
            var wrapper = _wappercache2.GetOrAdd(eventType, eventTypekey =>
            {
                var wrapperType = typeof(HandlerEventWrapprer2<>).MakeGenericType(eventTypekey);

                return (HandlerEventWrapprer2)Activator.CreateInstance(wrapperType, [handler]);
            });
            return wrapper;
        }
    }
    public class HandlerEventWrapprer2<T>(IEventHandler<T> handler) : HandlerEventWrapprer2 where T : IEvent
    {


        public async Task HandleTyped(T @event)
        {

            await handler.Handle(@event);
        }


        public override Task Handle(IEvent @event)
        {
            return HandleTyped((T)@event);
        }

    }
    //-----------------------------------------------------------------------


    public async Task SendD(IEnumerable<IEvent> evets)
    {
        foreach (var evet in evets)
        {
            await SendDynamic(evet);
        }
    }


    public async Task SendDynamic(IEvent command)
    {
        var handlerType = typeof(IEventHandler<>).MakeGenericType(command.GetType());

        IEnumerable<dynamic> handlers = _serviceProvider.GetServices(handlerType);

        foreach (dynamic handler in handlers)
        {
            await handler.Handle((dynamic)command);
        }
    }




    public async Task<TResult> SendDynRes<TResult>(ICommand<TResult> command)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));

        dynamic handler = _serviceProvider.GetRequiredService(handlerType);


        return await handler.Handle((dynamic)command);
    }

    private static readonly ConcurrentDictionary<(Type handlerType, Type commandType), Func<object, object, Task<object>>> _handleCache
    = new();

       
  
}
