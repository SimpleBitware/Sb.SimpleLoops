# SimpleLoops
Simple library for running async loops in parallel

## How to use it
SimpleLoopsBackgroundService detects all ISimpleLoop service registrations (such as `services.AddScoped<ISimpleLoop, SimpleLoop<<T>()`) and run them. <br/>
At minimum, the following services needs to be registered:<br/>
```
services.AddHostedService<SimpleLoopsBackgroundService>();                       /* background service which runs the loops */
services.AddScoped<ISimpleLoop, SimpleLoop<ISimpleLoopIterationExecutor>>();     /* the loop */
services.AddScoped<ISimpleLoopIterationExecutor, LoopItemExecutor>();            /* loop iteration executor which contains the logic executed at every iteration */
services.AddSingleton<SimpleLoopConfiguration<ISimpleLoopIterationExecutor>>();  /* optional, loop configuration otherwise the default configuration will be used */

/* dependencies */
services.AddSingleton<ITask, TaskDelayWrapper>();
services.AddSingleton<IDateTime, DateTimeWrapper>();
```

[Code sample](https://github.com/SimpleBitware/Sb.SimpleLoops/blob/main/tests/Sb.SimpleLoops.Tests.End2End/SimpleLoopsBackgroundServiceTests.cs)
