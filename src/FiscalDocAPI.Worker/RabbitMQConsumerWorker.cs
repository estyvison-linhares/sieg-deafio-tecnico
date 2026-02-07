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

        // Política de retry com backoff exponencial
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Tentativa {RetryCount} falhou. Aguardando {TimeSpan} antes de tentar novamente. Erro: {Error}",
                        retryCount, timeSpan, exception.Message);
                });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMQ Consumer Worker iniciando...");

        await _retryPolicy.ExecuteAsync(async () =>
        {
            InitializeRabbitMQ();
            await Task.CompletedTask;
        });

        if (_channel == null)
        {
            _logger.LogError("Não foi possível inicializar o canal RabbitMQ");
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

            // Confirmar processamento
            _channel?.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            _logger.LogInformation("Mensagem processada com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem após todas as tentativas");
            
            // Rejeitar mensagem e enviar para DLQ (se configurado) ou reprocessar
            _channel?.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private async Task ProcessDocumentEventAsync(string message, CancellationToken stoppingToken)
    {
        var evt = JsonSerializer.Deserialize<DocumentProcessedEvent>(message);
        
        if (evt == null)
        {
            _logger.LogWarning("Não foi possível desserializar a mensagem");
            return;
        }

        _logger.LogInformation(
            "Processando documento: {DocumentId}, Tipo: {DocumentType}, CNPJ: {Cnpj}, Valor: {Value}",
            evt.DocumentId, evt.DocumentType, evt.EmitterCnpj, evt.TotalValue);

        var summary = $"""
            📄 Novo documento processado!
            
            ID: {evt.DocumentId}
            Tipo: {evt.DocumentType}
            CNPJ Emissor: {FormatCnpj(evt.EmitterCnpj)}
            Chave: {evt.DocumentKey}
            Valor Total: R$ {evt.TotalValue:N2}
            Data de Processamento: {evt.ProcessedAt:dd/MM/yyyy HH:mm:ss}
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
