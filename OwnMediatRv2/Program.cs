using Contracts;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using OwnMediatRv2;
using OwnMediatRv2.Extensions;
using System.Diagnostics;
using CompiledLambda = OwnMediatRv2.Dispatchers.CompiledLambda;
using Reflection = OwnMediatRv2.Dispatchers.Reflection;
using DelegateFunction = OwnMediatRv2.Dispatchers.DelegateFunction;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCommandAndQueries();
builder.Services.AddScoped<Dispatcher>();
builder.Services.AddScoped<CompiledLambda.Dispatcher>();
builder.Services.AddScoped<Reflection.Dispatcher>();
builder.Services.AddScoped<DelegateFunction.Dispatcher>();
//builder.Services.AddOpenApi();

builder.Services.AddMediatR(c=>c.RegisterServicesFromAssembly(typeof(Program).Assembly)); 

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
   // app.MapOpenApi();
}

app.UseHttpsRedirection();


app.MapGet("/weatherforecast", async (Dispatcher dispatcher, 
                                            CompiledLambda.Dispatcher compiledLambdaDispatcher,
                                            Reflection.Dispatcher reflecionDispatcher,
                                            DelegateFunction.Dispatcher delegateFunctionDispatcher,
                                            ISender sender, 
                                            IPublisher publisher ,
                                            IServiceProvider _serviceProvider ) =>
{

    var command = new GetAlaCommand(20);
    var commandMediatR = new GetAlaMediatRCommand(20);
    //var age = await dispatcher.Send(new GetAlaCommand(20));
    //Task.c;

    IEnumerable<IEvent> events = [new AlaArrivedEvent(), new AlaArrivedEvent2()];
    IEnumerable<INotification> event2 = [new AlaArrivedEventMediatr(), new AlaArrivedMediatrEvent2()];


    var ala = await compiledLambdaDispatcher.Send(command);


    Stopwatch sw = Stopwatch.StartNew();

    for (int i = 0; i < 1000000; i++)

        await dispatcher.Send(command );


    Console.WriteLine(sw.ElapsedMilliseconds + " generated");
    sw.Restart();
    for (int i = 0; i < 1000000; i++)
        await compiledLambdaDispatcher.Send(command);
    Console.WriteLine(sw.ElapsedMilliseconds+ " compiled lambda");
    sw.Restart();
    for (int i = 0; i < 1000000; i++)
        await delegateFunctionDispatcher.Send(command);
    Console.WriteLine(sw.ElapsedMilliseconds +" delegaty");
    sw.Restart();
    for (int i = 0; i < 1000000; i++)
        await reflecionDispatcher.Send(command);
    Console.WriteLine(sw.ElapsedMilliseconds + " reflection");
    sw.Restart();
    for (int i = 0; i < 1000000; i++)
        await dispatcher.SendDynRes(command);
    Console.WriteLine(sw.ElapsedMilliseconds + " deymamic");
    sw.Restart();
    for (int i = 0; i < 1000000; i++)
        await sender.Send(commandMediatR);
    Console.WriteLine(sw.ElapsedMilliseconds+ " mediatR");
    sw.Stop();





    Stopwatch sw2 = Stopwatch.StartNew();
    for (int i = 0; i < 1000000; i++)

        await dispatcher.SendEventsGenerated(events);

    Console.WriteLine(sw2.ElapsedMilliseconds + " generated");
    sw2.Restart();
    for (int i = 0; i < 1000000; i++)
        await compiledLambdaDispatcher.Send(events);
    Console.WriteLine(sw2.ElapsedMilliseconds + " compiledLambda");
    sw2.Restart();
    for (int i = 0; i < 1000000; i++)
        await delegateFunctionDispatcher.Send(events);
    Console.WriteLine(sw2.ElapsedMilliseconds + " delegaty");
    sw2.Restart();
    for (int i = 0; i < 1000000; i++)
        await reflecionDispatcher.Send(events);
    Console.WriteLine(sw2.ElapsedMilliseconds + " reflection");
    sw2.Restart();
    for (int i = 0; i < 1000000; i++)
        await dispatcher.SendD(events);
    Console.WriteLine(sw2.ElapsedMilliseconds +" dynamic");
    sw2.Restart();
    for (int i = 0; i < 1000000; i++)
        await dispatcher.SendEventsWrapped(events);
    Console.WriteLine(sw2.ElapsedMilliseconds + " wrapped");
    sw2.Restart();
    for (int i = 0; i < 1000000; i++)
        await dispatcher.SendEventsWrapped2(events);
    Console.WriteLine(sw2.ElapsedMilliseconds + " wrapped2");
    sw2.Restart();
    for (int i = 0; i < 1000000; i++)
        await publisher.Publish(new AlaArrivedEventMediatr());
    Console.WriteLine(sw2.ElapsedMilliseconds + " Mediatr");

    sw2.Stop();

    Console.WriteLine(((AlaArrivedEvent)events.First()).I+" "+ ((AlaArrivedEvent2)events.Last()).I);
    return Results.Ok();
});

app.Run();


public class AlaArrivedEvent : IEvent
{
    public int I { get; set; }
};
public class AlaArrivedEvent2: IEvent
{
    public int I { get; set; }
}

public class AlaArrivedEventMediatr : INotification
{
    public int I { get; set; }
}

public class AlaArrivedMediatrEvent2 : INotification
{
    public int I { get; set; }
}

public class AlaArrivedMediatrEvent2Handler : INotificationHandler<AlaArrivedMediatrEvent2>
{
    public Task Handle(AlaArrivedMediatrEvent2 notification, CancellationToken cancellationToken)
    {
        notification.I++;
        return Task.CompletedTask;
    }
}

public class AlaArrivedMediatrEventHandler : INotificationHandler<AlaArrivedEventMediatr>
{
    public Task Handle(AlaArrivedEventMediatr notification, CancellationToken cancellationToken)
    {

        notification.I++;
        return Task.CompletedTask;
    }
}
public class AlaArrivedMediatrEventHandler2 : INotificationHandler<AlaArrivedEventMediatr>
{
    public Task Handle(AlaArrivedEventMediatr notification, CancellationToken cancellationToken)
    {
        notification.I++;
        return Task.CompletedTask;
    }
}

public record GetAlaCommand(int Age) : ICommand<Result>;
public record GetAlaMediatRCommand(int Age) :IRequest<Result>;

public record Result(int AgeAla);

public class GetAlaMedaitRCommandHandler : IRequestHandler<GetAlaMediatRCommand, Result>
{

    
    public Task<Result> Handle(GetAlaMediatRCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult( new Result( request.Age));
    }
}

public class GetAlaCommandHandler : ICommandHandler<GetAlaCommand, Result>
{

    public Task<Result> Handle(GetAlaCommand command)
    {
       // Console.WriteLine(nameof(GetAlaCommandHandler));
        return Task.FromResult(new Result( command.Age));
    }
}

public class AlaArrivedEventHandler : IEventHandler<AlaArrivedEvent>
{

    public Task Handle(AlaArrivedEvent command)
    {
        command.I++;
       // Console.WriteLine(nameof(AlaArrivedEventHandler));
        return Task.CompletedTask;
    }
}



public class AlaArrivedEventHandler2 : IEventHandler<AlaArrivedEvent>
{

    public Task Handle(AlaArrivedEvent @event)
    {
        @event.I++;

        // Console.WriteLine(nameof(AlaArrivedEventHandler));
        return Task.CompletedTask;
    }
}

public class AlaArrivedEvent2Handler : IEventHandler<AlaArrivedEvent2>
{

    public Task Handle(AlaArrivedEvent2 @event)
    {
        @event.I++;
       // Console.WriteLine(nameof(AlaArrivedEvent2Handler));
        return Task.CompletedTask;
    }
}
