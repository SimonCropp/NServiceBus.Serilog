<img src="https://raw.github.com/NServiceBusExtensions/NServiceBus.Serilog/master/src/icon.png" height="25px"> Add support for sending [NServiceBus](http://particular.net/NServiceBus) logging through [Serilog](http://serilog.net/)
<!--- StartOpenCollectiveBackers -->

[Already a Patron? skip past this section](#endofbacking)


## Community backed

**It is expected that all developers [become a Patron](https://opencollective.com/nservicebusextensions/order/6976) to use any of these libraries. [Go to licensing FAQ](https://github.com/NServiceBusExtensions/Home/blob/master/readme.md#licensingpatron-faq)**


### Sponsors

Support this project by [becoming a Sponsors](https://opencollective.com/nservicebusextensions/order/6972). The company avatar will show up here with a link to your website. The avatar will also be added to all GitHub repositories under this organization.


### Patrons

Thanks to all the backing developers! Support this project by [becoming a patron](https://opencollective.com/nservicebusextensions/order/6976).

<img src="https://opencollective.com/nservicebusextensions/tiers/patron.svg?width=890&avatarHeight=60&button=false">

<!--- EndOpenCollectiveBackers -->

<a href="#" id="endofbacking"></a>

## The NuGet package [![NuGet Status](http://img.shields.io/nuget/v/NServiceBus.Serilog.svg?style=flat)](https://www.nuget.org/packages/NServiceBus.Serilog/)

https://nuget.org/packages/NServiceBus.Serilog/

    PM> Install-Package NServiceBus.Serilog


## Standard Logging Library

Pipe [NServiceBus logging messages](https://docs.particular.net/nservicebus/logging/) through to Serilog.


### Documentation

https://docs.particular.net/nuget/NServiceBus.Serilog


### Usage

```csharp
var loggerConfiguration = new LoggerConfiguration();
loggerConfiguration.WriteTo.Console();
loggerConfiguration.MinimumLevel.Debug();
loggerConfiguration.WriteTo.File("logFile.txt");
var logger = loggerConfiguration.CreateLogger();

Log.Logger = logger;

//Set NServiceBus to log to Serilog
var serilogFactory = LogManager.Use<SerilogFactory>();
serilogFactory.WithLogger(logger);
```


## Icon

<a href="http://thenounproject.com/noun/brain/#icon-No10411" target="_blank">Brain</a> designed by <a href="http://thenounproject.com/catalarem" target="_blank">Rémy Médard</a> from The Noun Project