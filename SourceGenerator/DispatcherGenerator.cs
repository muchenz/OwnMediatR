using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;


namespace SourceGenerator_MY;


[Generator]
public class DispatcherGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        //Debugger.Launch();
#endif
        var handlerTypes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax,
                transform: static (ctx, _) =>
                {
                    var node = (ClassDeclarationSyntax)ctx.Node;
                    var symbol = ctx.SemanticModel.GetDeclaredSymbol(node) as INamedTypeSymbol;
                    return symbol;
                })
            .Where(symbol => symbol is not null && ImplementsHandlerInterface(symbol))
            .Collect();

        context.RegisterSourceOutput(handlerTypes, (ctx, symbols) =>
        {
            var sb = new StringBuilder();
            var generated = new HashSet<string>();
            var knownEvents = new HashSet<string>();

            // Dodatkowe kolekcje na rozróżnienie komend void i z wynikiem
            var generatedVoidCommands = new HashSet<string>();
            var generatedResultCommands = new List<(string CommandType, string ResultType)>();
            var generatedQuery = new List<(string CommandType, string ResultType)>();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using Contracts;");
            sb.AppendLine("namespace GeneratedDispatchers");
            sb.AppendLine("{");
            sb.AppendLine("    public static class Dispatcher");
            sb.AppendLine("    {");

            foreach (var handler in symbols.Distinct(SymbolEqualityComparer.Default))
            {
                if (handler is not INamedTypeSymbol namedHandler)
                    continue;

                foreach (var iface in namedHandler.AllInterfaces)
                {
                    if (!iface.Name.StartsWith("ICommandHandler") &&
                        !iface.Name.StartsWith("IEventHandler") &&
                        !iface.Name.StartsWith("IQueryHandler"))
                        continue;

                    if (iface.TypeArguments.Length == 0 || iface.TypeArguments.Any(a => a is ITypeParameterSymbol))
                        continue;

                    var isEvent = iface.Name.StartsWith("IEventHandler");
                    var isCommandWithResult = iface.Name.StartsWith("ICommandHandler") && iface.TypeArguments.Length == 2;
                    var isQuery = iface.Name.StartsWith("IQueryHandler") && iface.TypeArguments.Length == 2;

                    var commandType = iface.TypeArguments[0].ToDisplayString();
                    var methodName = isEvent ? "PublishEvent" : "Send";

                    var sigKey = $"{methodName}_{commandType}";
                    if (!generated.Add(sigKey))
                        continue;

                    if (isEvent)
                    {
                        knownEvents.Add(commandType);

                        sb.AppendLine($$"""
    public static async Task PublishEvent({{commandType}} ev, IServiceProvider provider)
    {
        var handlers = (IEnumerable<{{iface.ToDisplayString()}}>)provider.GetServices(typeof({{iface.ToDisplayString()}}));
            //?? throw new InvalidOperationException($"Handler for type {nameof({{iface.ToDisplayString()}})} not registered.");
            foreach(var handler in handlers)
            {
                await handler.Handle(ev);
            }
    }
""");
                    }
                    else if (isCommandWithResult)
                    {
                        var resultType = iface.TypeArguments[1].ToDisplayString();
                        generatedResultCommands.Add((commandType, resultType));

                        sb.AppendLine($$"""
    public static Task<{{resultType}}> Send({{commandType}} command, IServiceProvider provider)
    {
        var handler = ({{iface.ToDisplayString()}})provider.GetService(typeof({{iface.ToDisplayString()}}))
            ?? throw new InvalidOperationException($"Handler for type {nameof({{iface.ToDisplayString()}})} not registered.");
        return handler.Handle(command);
    }
""");
                    }
                    else if (isQuery)
                    {
                        var resultType = iface.TypeArguments[1].ToDisplayString();
                        generatedQuery.Add((commandType, resultType));

                        sb.AppendLine($$"""
    public static Task<{{resultType}}> Send({{commandType}} query, IServiceProvider provider)
    {
        var handler = ({{iface.ToDisplayString()}})provider.GetService(typeof({{iface.ToDisplayString()}}))
            ?? throw new InvalidOperationException($"Handler for type {nameof({{iface.ToDisplayString()}})} not registered.");
        return handler.Handle(query);
    }
""");
                    }
                    else
                    {
                        generatedVoidCommands.Add(commandType);

                        sb.AppendLine($$"""
    public static Task Send({{commandType}} command, IServiceProvider provider)
    {
        var handler = ({{iface.ToDisplayString()}})provider.GetService(typeof({{iface.ToDisplayString()}}))
            ?? throw new InvalidOperationException($"Handler for type {nameof({{iface.ToDisplayString()}})} not registered.");
        return handler.Handle(command);
    }
""");
                    }
                }
            }

            // Uniwersalna metoda do wysyłania komend void:
            sb.AppendLine($$"""
    public static Task Send(ICommand command, IServiceProvider provider)
    {
        switch (command)
        {
""");

            foreach (var commandType in generatedVoidCommands.OrderBy(t => t))
            {
                sb.AppendLine($$"""
            case {{commandType}} c:
                return Send(c, provider);
                //break;
""");
            }

            sb.AppendLine($$"""
            default:
                throw new NotSupportedException($"Unknown command type: {command.GetType()}");
        }
    }
""");

            // Uniwersalna metoda do wysyłania komend z wynikiem:
            sb.AppendLine($$"""
    public static Task<TResult> Send<TResult>(ICommand<TResult> command, IServiceProvider provider)
    {
        switch (command)
        {
""");

            foreach (var (commandType, _) in generatedResultCommands.OrderBy(t => t.CommandType))
            {
                sb.AppendLine($$"""
        case {{commandType}} c:
            return (Task<TResult>)(object) Send(c, provider);
""");
            }

            sb.AppendLine($$"""
            default:
                throw new NotSupportedException($"Unknown command type: {command.GetType()}");
        }
    }
""");
            // Uniwersalna metoda do wysyłania query z wynikiem:
            sb.AppendLine($$"""
    public static Task<TResult> Send<TResult>(IQuery<TResult> query, IServiceProvider provider)
    {
        switch (query)
        {
""");

            foreach (var (commandType, _) in generatedQuery.OrderBy(t => t.CommandType))
            {
                sb.AppendLine($$"""
        case {{commandType}} c:
            return (Task<TResult>)(object)await Send(c, provider);
""");
            }

            sb.AppendLine($$"""
            default:
                throw new NotSupportedException($"Unknown command type: {query.GetType()}");
        }
    }
""");

            // Uniwersalna metoda do publikowania eventów:
            sb.AppendLine($$"""
    public static Task Publish(IEvent ev, IServiceProvider provider)
    {
        switch (ev)
        {
""");

            foreach (var eventType in knownEvents.OrderBy(e => e))
            {
                sb.AppendLine($$"""
            case {{eventType}} e:
                return PublishEvent(e, provider);
                //break;
""");
            }

            sb.AppendLine($$"""
            default:
                throw new NotSupportedException($"Unknown event type: {ev.GetType()}");
        }
    }
""");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            ctx.AddSource("Dispatcher.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        });
    }

    private static bool ImplementsHandlerInterface(INamedTypeSymbol? symbol)
    {
        if (symbol == null || symbol.IsAbstract || symbol.IsGenericType)
            return false;

        return symbol.AllInterfaces.Any(i =>
            (i.Name == "ICommandHandler" || i.Name == "IEventHandler" || i.Name == "IQueryHandler") &&
            i.TypeArguments.All(arg => arg is not ITypeParameterSymbol));
    }
}
