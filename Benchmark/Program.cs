using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OwnMediatR.ForSourceGen.Lib.Extensions;
using CompiledLambda = OwnMediatR.ForSourceGen.Lib.Dispatchers.CompiledLambda;
using DelegateFunction = OwnMediatR.ForSourceGen.Lib.Dispatchers.DelegateFunction;
using Reflection = OwnMediatR.ForSourceGen.Lib.Dispatchers.Reflection;
using Wrapped = OwnMediatR.ForSourceGen.Lib.Dispatchers.Wrapperv1;
using Dynamic = OwnMediatR.ForSourceGen.Lib.Dispatchers.Dynamic;
using Benchmark;


BenchmarkRunner.Run<MyBenchmark>();


[ShortRunJob]
[MemoryDiagnoser]
public class MyBenchmark
{
    private Reflection.Dispatcher _reflectionDispatcher;
    private Dynamic.Dispatcher _dynamicDispatcher;
    private CompiledLambda.Dispatcher _compiledLambdaDispatcher;
    private DelegateFunction.Dispatcher _delegateFunctionDispatcher;
    private Wrapped.Dispatcher _wrappedDispatcher;
    private Dispatcher _sourceGeneratedDispatcher;

    private IEvent[] _events = [new AliceArrivedEvent(), new KellyArrivedEvent()];

    [GlobalSetup]
    public void Setup()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddCommandAndQueries();
                services.AddScoped<Reflection.Dispatcher>();
                services.AddScoped<Dynamic.Dispatcher>();
                services.AddScoped<CompiledLambda.Dispatcher>();
                services.AddScoped<DelegateFunction.Dispatcher>();
                services.AddScoped<Wrapped.Dispatcher>();
                services.AddScoped<Dispatcher>();

            })
            .Build();

        _reflectionDispatcher = host.Services.GetRequiredService<Reflection.Dispatcher>();
        _dynamicDispatcher = host.Services.GetRequiredService<Dynamic.Dispatcher>();
        _compiledLambdaDispatcher = host.Services.GetRequiredService<CompiledLambda.Dispatcher>();
        _delegateFunctionDispatcher = host.Services.GetRequiredService<DelegateFunction.Dispatcher>();
        _wrappedDispatcher = host.Services.GetRequiredService<Wrapped.Dispatcher>();
        _sourceGeneratedDispatcher = host.Services.GetRequiredService<Dispatcher>();
    }

    [Benchmark]
    public async Task Reflection_Dispatcher()
    {

        await _reflectionDispatcher.Send(_events);
    }
    [Benchmark]
    public async Task Dynamic_Dispatcher()
    {

        await _dynamicDispatcher.Send(_events);
    }
    [Benchmark]
    public async Task CompiledLambda_Dispatcher()
    {

        await _compiledLambdaDispatcher.Send(_events);
    }
    [Benchmark]
    public async Task DelegateFunction_Dispatcher()
    {

        await _delegateFunctionDispatcher.Send(_events);
    }

    [Benchmark]
    public async Task Wrapped_Benchmark()
    {

        await _wrappedDispatcher.Send(_events);
    }

    [Benchmark]
    public async Task SourceGenerated_Benchmark()
    {

        await _sourceGeneratedDispatcher.Send(_events);
    }
}


//----------------------------------------------------------------------------------------------------------

public record AliceArrivedEvent : IEvent;
public record KellyArrivedEvent : IEvent;



public class AliceArrivedEventHandler : IEventHandler<AliceArrivedEvent>
{
    public Task Handle(AliceArrivedEvent @event)
    {

        return Task.CompletedTask;
    }
}

public class AliceArrivedEventHandler2 : IEventHandler<AliceArrivedEvent>
{
    public Task Handle(AliceArrivedEvent @event)
    {

        return Task.CompletedTask;
    }
}
public class AliceArrivedEventHandler3 : IEventHandler<AliceArrivedEvent>
{
    public Task Handle(AliceArrivedEvent @event)
    {

        return Task.CompletedTask;
    }
}
public class KellyArrivedEventHandler : IEventHandler<KellyArrivedEvent>
{
    public Task Handle(KellyArrivedEvent @event)
    {

        return Task.CompletedTask;
    }
}
//-------------------------------------------------------------------------------------------------------

public record Result(int Age);

//public record SetAliceAgeCommand():ICommand<>