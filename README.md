# SimpleLoops
Simple library for running loops in parallel

## How to use it
SimpleLoopsBackgroundService detects all ISimpleLoop registrations (such as `services.AddScoped<ISimpleLoop, SimpleLoop<<T>()`) and run them. <br/>
At minimum, the following services needs to be registered:<br/>
```
services.AddHostedService<SimpleLoopsBackgroundService>();                                      /* registers the background service which runs the loops */
services.AddScoped<ISimpleLoop, SimpleLoop<ISimpleLoopIterationExecutor>>();                    /* registers a loop */
services.AddScoped<ISimpleLoopIterationExecutor>(services => loopIteratorExecutorMock.Object);  /* registers a loop iteration executor which contains the logic executed at every iteration */
services.AddSingleton<SimpleLoopConfiguration<ISimpleLoopIterationExecutor>>();                 /* registers the loop configuration */

/* dependencies */
services.AddSingleton<ITaskDelayWrapper, TaskDelayWrapper>();
services.AddSingleton<IDateTimeWrapper, DateTimeWrapper>();
```

[Code sample](https://github.com/SimpleBitware/Sb.SimpleLoops/blob/main/tests/Sb.SimpleLoops.Tests.End2End/SimpleLoopsBackgroundServiceTests.cs)
