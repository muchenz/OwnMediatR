| Method                      | Mean       | Error       | StdDev   | Gen0   | Allocated |
|---------------------------- |-----------:|------------:|---------:|-------:|----------:|
| Reflection_Dispatcher       | 1,023.2 ns | 1,036.11 ns | 56.79 ns | 0.2613 |    1.6 KB |
| Dynamic_Dispatcher          |   991.8 ns |   163.09 ns |  8.94 ns | 0.2708 |   1.66 KB |
| CompiledLambda_Dispatcher   |   833.5 ns |    77.49 ns |  4.25 ns | 0.2403 |   1.48 KB |
| DelegateFunction_Dispatcher |   910.0 ns |   227.12 ns | 12.45 ns | 0.3071 |   1.88 KB |
| Wrapped_DispatcherBenchmark |   570.3 ns |    79.26 ns |  4.34 ns | 0.1993 |   1.23 KB |
