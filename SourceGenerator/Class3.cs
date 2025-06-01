//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Text;
//using System.Collections.Immutable;
//using System.Diagnostics;
//using System.Text;


//namespace SourceGenerator_MY2;
//[Generator]
//public class DispatcherGenerator : IIncrementalGenerator
//{
//    public void Initialize(IncrementalGeneratorInitializationContext context)
//    {
//#if DEBUG
//        // Debugger.Launch();
//#endif
//        var handlerTypes = context.SyntaxProvider
//            .CreateSyntaxProvider(
//                predicate: static (s, _) => s is ClassDeclarationSyntax,
//                transform: static (ctx, _) =>
//                {
//                    var node = (ClassDeclarationSyntax)ctx.Node;
//                    var symbol = ctx.SemanticModel.GetDeclaredSymbol(node) as INamedTypeSymbol;
//                    return symbol;
//                })
//            .Where(symbol => symbol is not null && ImplementsHandlerInterface(symbol))
//            .Collect();

//        context.RegisterSourceOutput(handlerTypes, (ctx, symbols) =>
//        {
//            var sb = new StringBuilder();
//            var generated = new HashSet<string>();
//            var knownEvents = new HashSet<string>();
//            var knownQueries = new List<(string QueryType, string ResultType)>();

//            sb.AppendLine("using System;");
//            sb.AppendLine("using System.Threading.Tasks;");
//            sb.AppendLine("using Contracts;");
//            sb.AppendLine("namespace GeneratedDispatchers");
//            sb.AppendLine("{");
//            sb.AppendLine("    public static class Dispatcher");
//            sb.AppendLine("    {");

//            foreach (var handler in symbols.Distinct(SymbolEqualityComparer.Default))
//            {
//                if (handler is not INamedTypeSymbol namedHandler)
//                    continue;

//                foreach (var iface in namedHandler.AllInterfaces)
//                {
//                    if (!iface.Name.StartsWith("ICommandHandler") && !iface.Name.StartsWith("IEventHandler") && !iface.Name.StartsWith("IQueryHandler"))
//                        continue;

//                    if (iface.TypeArguments.Length == 0 || iface.TypeArguments.Any(a => a is ITypeParameterSymbol))
//                        continue;

//                    var isEvent = iface.Name.StartsWith("IEventHandler");
//                    var isQuery = iface.Name.StartsWith("IQueryHandler");
//                    var isCommandOrQueryWithResult = iface.TypeArguments.Length == 2;
//                    var commandType = iface.TypeArguments[0].ToDisplayString();
//                    var methodName = isEvent ? "Publish" : "Send";

//                    var sigKey = $"{methodName}_{commandType}";
//                    if (!generated.Add(sigKey))
//                        continue;

//                    if (isEvent)
//                    {
//                        knownEvents.Add(commandType);
//                        sb.AppendLine($$"""
//        public static async Task Publish({commandType} ev, IServiceProvider provider)
//        {var handler = ({iface.ToDisplayString()})provider.GetService(typeof({iface.ToDisplayString()}))
//                ?? throw new InvalidOperationException($"Handler for type {nameof({iface.Name})} not registered.");
//            await handler.Handle(ev);
//        }
//""");
//                    }
//                    else if (isCommandOrQueryWithResult)
//                    {
//                        var resultType = iface.TypeArguments[1].ToDisplayString();

//                        if (isQuery)
//                        {
//                            knownQueries.Add((commandType, resultType));
//                        }

//                        sb.AppendLine($$"""
//        public static async Task<{resultType}> Send({commandType} query, IServiceProvider provider)
//        {var handler = ({iface.ToDisplayString()})provider.GetService(typeof({iface.ToDisplayString()}))
//                ?? throw new InvalidOperationException($"Handler for type {nameof({iface.Name})} not registered.");
//            return await handler.Handle(query);
//        }
//""");
//                    }
//                    else // ICommandHandler<T>
//                    {
//                        sb.AppendLine($$"""
//        public static async Task Send({commandType} command, IServiceProvider provider)
//        {var handler = ({iface.ToDisplayString()})provider.GetService(typeof({iface.ToDisplayString()}))
//                ?? throw new InvalidOperationException($"Handler for type {nameof({iface.Name})} not registered.");
//            await handler.Handle(command);
//        }
//""");
//                    }
//                }
//            }

//            // Uniwersalny dispatcher dla eventów
//            sb.AppendLine("        public static async Task Publish(IEvent ev, IServiceProvider provider)");
//            sb.AppendLine("        {");
//            sb.AppendLine("            switch (ev)");
//            sb.AppendLine("            {");

//            foreach (var eventType in knownEvents.OrderBy(e => e))
//            {
//                sb.AppendLine($"                case {eventType} e:");
//                sb.AppendLine("                    await Publish(e, provider);");
//                sb.AppendLine("                    break;");
//            }

//            sb.AppendLine("                default:");
//            sb.AppendLine("                    throw new NotSupportedException($\"Unknown event type: {ev.GetType()}\");");
//            sb.AppendLine("            }");
//            sb.AppendLine("        }");

//            // Uniwersalny dispatcher dla query
//            sb.AppendLine("        public static async Task<TResult> Send<TResult>(IQuery<TResult> query, IServiceProvider provider)");
//            sb.AppendLine("        {");
//            sb.AppendLine("            switch (query)");
//            sb.AppendLine("            {");

//            foreach (var (queryType, _) in knownQueries.OrderBy(q => q.QueryType))
//            {
//                sb.AppendLine($"                case {queryType} q:");
//                sb.AppendLine("                    return (TResult)(object)await Send(q, provider);");
//            }

//            sb.AppendLine("                default:");
//            sb.AppendLine("                    throw new NotSupportedException($\"Unknown query type: {query.GetType()}\");");
//            sb.AppendLine("            }");
//            sb.AppendLine("        }");

//            sb.AppendLine("    }");
//            sb.AppendLine("}");

//            ctx.AddSource("Dispatcher.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
//        });
//    }

//    private static bool ImplementsHandlerInterface(INamedTypeSymbol? symbol)
//    {
//        if (symbol == null || symbol.IsAbstract || symbol.IsGenericType)
//            return false;

//        return symbol.AllInterfaces.Any(i =>
//            (i.OriginalDefinition.ToDisplayString().StartsWith("ICommandHandler") ||
//             i.OriginalDefinition.ToDisplayString().StartsWith("IEventHandler") ||
//             i.OriginalDefinition.ToDisplayString().StartsWith("IQueryHandler")) &&
//            i.TypeArguments.All(arg => arg is not ITypeParameterSymbol));
//    }
//}
