# SimpleLoops
Simple library for running loops in parallel

## How to use it
SimpleLoopsBackgroundService detects all ISimpleLoop service registrations (such as `services.AddScoped<ISimpleLoop, SimpleLoop<<T>()`) and run them. <br/>
At minimum, the following services needs to be registered:<br/>
```
services.AddHostedService<SimpleLoopsBackgroundService>();                                      /* background service which runs the loops */
services.AddScoped<ISimpleLoop, SimpleLoop<ISimpleLoopIterationExecutor>>();                    /* the loop */
services.AddScoped<ISimpleLoopIterationExecutor>(services => loopIteratorExecutorMock.Object);  /* loop iteration executor which contains the logic executed at every iteration */
services.AddSingleton<SimpleLoopConfiguration<ISimpleLoopIterationExecutor>>();                 /* loop configuration */

/* dependencies */
services.AddSingleton<ITaskDelayWrapper, TaskDelayWrapper>();
services.AddSingleton<IDateTimeWrapper, DateTimeWrapper>();
```

[Code sample](https://github.com/SimpleBitware/Sb.SimpleLoops/blob/main/tests/Sb.SimpleLoops.Tests.End2End/SimpleLoopsBackgroundServiceTests.cs)
