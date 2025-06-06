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

        using var scope = _serviceProvider.CreateScope();

        var res = GeneratedDispatchers.Dispatcher.Send(command, scope.ServiceProvider);

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
            using var scope = _serviceProvider.CreateScope();

            await GeneratedDispatchers.Dispatcher.Publish(evet, scope.ServiceProvider);
        }
    }

   

    
    //-----------------------------------------------------------------------




  
    private static readonly ConcurrentDictionary<(Type handlerType, Type commandType), Func<object, object, Task<object>>> _handleCache
    = new();

  
}
