using FiscalDocAPI.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<RabbitMQConsumerWorker>();

var host = builder.Build();
host.Run();
