| Method                      | Mean     | Error     | StdDev   | Gen0   | Allocated |
|---------------------------- |---------:|----------:|---------:|-------:|----------:|
| Reflection_Dispatcher       | 962.2 ns | 429.84 ns | 23.56 ns | 0.2613 |    1.6 KB |
| Dynamic_Dispatcher          | 996.7 ns | 101.91 ns |  5.59 ns | 0.2708 |   1.66 KB |
| CompiledLambda_Dispatcher   | 840.7 ns | 212.85 ns | 11.67 ns | 0.2403 |   1.48 KB |
| DelegateFunction_Dispatcher | 890.0 ns |  64.92 ns |  3.56 ns | 0.3071 |   1.88 KB |
| Wrapped_Benchmark           | 545.9 ns |  17.71 ns |  0.97 ns | 0.1993 |   1.23 KB |
| SourceGenerated_Benchmark   | 595.6 ns |   9.00 ns |  0.49 ns | 0.2203 |   1.35 KB |


1. Source Generator requires a separate project for Contracts (IEvent, IEventHandler<>, etc.) to avoid cyclic dependencies. That's why the lib comes in two versions (with and without Contracts). 
2. When the dispatcher is injected as a scope (and not as a singleton), you can give up that doing the scope in the Dispatcher class - it will be a little faster.