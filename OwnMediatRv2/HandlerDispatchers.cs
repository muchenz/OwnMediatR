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
   

    public async Task<TResult> SendDelegatr<TResult>(ICommand<TResult> command)
    {
        var handler = _serviceProvider.GetService(typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult)));
        if (handler is null) throw new NullReferenceException("Handler is null");
        var result = await InvokeCreateDelegateWithResultAsync(handler, command);
        //var result = await invo(handler, command);

        return (TResult)result;
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

           var eventType = evet.GetType();

            var wrapper = _wappercache.GetOrAdd(eventType,  eventTypekey =>
            {

                var typedWrapper = typeof(HandlerEventWrapprer<>).MakeGenericType(eventTypekey);

                var instance  = Activator.CreateInstance(typedWrapper, [ ]);

                return (HandlerEventWrapprer)instance;

            });

            await wrapper.Handle(evet, _serviceProvider);

        }
    }


  

    private static readonly ConcurrentDictionary<Type, HandlerEventWrapprer>   _wappercache
    = new(); 
    public abstract class HandlerEventWrapprer
    {
        public abstract Task Handle(IEvent @event, IServiceProvider serviceProvider);
    }
    
    public  class HandlerEventWrapprer<T>:HandlerEventWrapprer where T:IEvent
    {

        public HandlerEventWrapprer()
        {
        }
        public async  Task HandleTyped(T @event, IServiceProvider serviceProvider)
        {
            var handlerType = typeof(IEventHandler<T>);

            var handlers = (IEnumerable<IEventHandler<T>>)serviceProvider.GetServices(handlerType);

            foreach(var handler in handlers)
            {

                await handler.Handle(@event);
            }
        }


        public override Task Handle(IEvent @event, IServiceProvider serviceProvider)
        {
            return  HandleTyped((T)@event, serviceProvider);
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

   

   
    private static readonly ConcurrentDictionary<(Type handlerType, Type commandType), Func<object, object, Task<object>>> _cacheWithResult = new();


   

    //-------------------------------------------------------------------------------------------------------------------------------

    //private Task<object> InvokeCreateDelegateWithResultAsync2<TResult>(object handler, object command)
    //{
    //    var key = (handler.GetType(), command.GetType());

    //    var del = _cacheWithResult.GetOrAdd(key, static key =>
    //    {
    //        var method = key.Item1.GetMethod("Handle", new[] { key.Item2 });

    //        return (Func<object, object, Task<object>>)Delegate.CreateDelegate(
    //            typeof(Func<object, object, Task<TResult>>),
    //            null, method
    //        );
    //    });

    //    return del(handler, command); // ⚡ ~50–100 ns
    //}

    private Task<object> InvokeCreateDelegateWithResultAsync(object handler, object command)  //slow !! - slower than 'invoke' !!
    {
        var key = (handler.GetType(), command.GetType());

        var del = _cacheWithResult.GetOrAdd(key, static key =>
        {
            var (handlerType, commandType) = key;

            var method = handlerType.GetMethod("Handle", new[] { commandType });
            var returnType = method.ReturnType; // Task<TResult>
            var resultType = returnType.GetGenericArguments()[0];

            // typeof(Func<TH, TC, Task<TR>>)
            var delegateType = typeof(Func<,,>).MakeGenericType(handlerType, commandType, returnType);

            var openDelegate = Delegate.CreateDelegate(delegateType, method);

            // Tworzymy silnie typowane wywołanie bez refleksji
            var wrapperType = typeof(StronglyTypedWrapper<,,>).MakeGenericType(handlerType, commandType, resultType);
            var wrapperMethod = wrapperType.GetMethod("Invoke");

            return (Func<object, object, Task<object>>)Delegate.CreateDelegate(
                typeof(Func<object, object, Task<object>>),
                openDelegate,
                wrapperMethod!
            );
        });

        return del(handler, command); // ⚡ ~50–100 ns
    }
    public static class StronglyTypedWrapper<TH, TC, TR>
    {
        public static async Task<object> Invoke(Func<TH, TC, Task<TR>> del, object h, object c)
        {
            var result = await del((TH)h, (TC)c).ConfigureAwait(false);
            return (object)result!;
        }
    }
    private static readonly ConcurrentDictionary<(Type handlerType, Type commandType), Func<object, object, Task>> _cache = new();

    private Task InvokeCreateDelegateAsync(object handler, object command)  // super slow !! besause DynamicInvoke
    {
        var key = (handler.GetType(), command.GetType());

        var del = _cache.GetOrAdd(key, static key =>
        {
            var method = key.Item1.GetMethod("Handle", new[] { key.Item2 });

            var delegateType = typeof(Func<,,>)
            .MakeGenericType(key.handlerType, key.commandType, typeof(Task));
            var openDelegate = Delegate.CreateDelegate(delegateType, method);
            //return (object)Delegate.CreateDelegate(
            //   delegateType,
            //   method
            //);

            return (object h, object c) => (Task)openDelegate.DynamicInvoke(h, c);
        });

        return del(handler, command); // ⚡ ~50–100 ns
    }


}

