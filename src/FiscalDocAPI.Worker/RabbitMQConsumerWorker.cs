using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Polly;
using Polly.Retry;

namespace FiscalDocAPI.Worker;

public class RabbitMQConsumerWorker : BackgroundService
{
    private readonly ILogger<RabbitMQConsumerWorker> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly AsyncRetryPolicy _retryPolicy;

    public RabbitMQConsumerWorker(
        ILogger<RabbitMQConsumerWorker> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // Retry policy with exponential backoff
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Attempt {RetryCount} failed. Waiting {TimeSpan} before retrying. Error: {Error}",
                        retryCount, timeSpan, exception.Message);
                });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMQ Consumer Worker starting...");

        await _retryPolicy.ExecuteAsync(async () =>
        {
            InitializeRabbitMQ();
            await Task.CompletedTask;
        });

        if (_channel  == null)
        {
            _logger.LogError("Failed to initialize RabbitMQ channel");
            return;
        }

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            await ProcessMessageAsync(ea, stoppingToken);
        };

        var queueName = _configuration["RabbitMQ:QueueName"] ?? "fiscal-documents";
        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

        _logger.LogInformation("Worker aguardando mensagens na fila: {Queue}", queueName);

        // Mantém o worker rodando
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private void InitializeRabbitMQ()
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:HostName"],
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:UserName"],
            Password = _configuration["RabbitMQ:Password"],
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        var exchangeName = _configuration["RabbitMQ:ExchangeName"] ?? "fiscal-exchange";
        var queueName = _configuration["RabbitMQ:QueueName"] ?? "fiscal-documents";
        var routingKey = _configuration["RabbitMQ:RoutingKey"] ?? "fiscal.document.#";

        // Declarar exchange
        _channel.ExchangeDeclare(
            exchange: exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        // Declarar fila
        _channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // Fazer binding
        _channel.QueueBind(
            queue: queueName,
            exchange: exchangeName,
            routingKey: routingKey);

        // Configurar QoS para processar uma mensagem por vez
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        _logger.LogInformation("RabbitMQ inicializado. Exchange: {Exchange}, Queue: {Queue}", exchangeName, queueName);
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            _logger.LogInformation("Mensagem recebida: {Message}", message);

            // Processar com retry
            await _retryPolicy.ExecuteAsync(async () =>
            {
                await ProcessDocumentEventAsync(message, stoppingToken);
            });

            // Acknowledge processed message
            _channel?.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            _logger.LogInformation("Message processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message after all retry attempts");
            
            // Reject message and send to DLQ (if configured) or reprocess
            _channel?.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private async Task ProcessDocumentEventAsync(string message, CancellationToken stoppingToken)
    {
        var evt = JsonSerializer.Deserialize<DocumentProcessedEvent>(message);
        
        if (evt == null)
        {
            _logger.LogWarning("Unable to deserialize message");
            return;
        }

        _logger.LogInformation(
            "Processing document: {DocumentId}, Type: {DocumentType}, CNPJ: {Cnpj}, Value: {Value}",
            evt.DocumentId, evt.DocumentType, evt.EmitterCnpj, evt.TotalValue);

        var summary = $"""
            📄 New document processed!
            
            ID: {evt.DocumentId}
            Type: {evt.DocumentType}
            Emitter CNPJ: {FormatCnpj(evt.EmitterCnpj)}
            Key: {evt.DocumentKey}
            Total Value: R$ {evt.TotalValue:N2}
            Processing Date: {evt.ProcessedAt:dd/MM/yyyy HH:mm:ss}
            """;

        _logger.LogInformation("Resumo gerado:\n{Summary}", summary);

        await Task.Delay(100, stoppingToken);
    }

    private string FormatCnpj(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj) || cnpj.Length != 14)
            return cnpj;

        return $"{cnpj.Substring(0, 2)}.{cnpj.Substring(2, 3)}.{cnpj.Substring(5, 3)}/{cnpj.Substring(8, 4)}-{cnpj.Substring(12, 2)}";
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}

public class DocumentProcessedEvent
{
    public Guid DocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentKey { get; set; } = string.Empty;
    public string EmitterCnpj { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public DateTime ProcessedAt { get; set; }
}
