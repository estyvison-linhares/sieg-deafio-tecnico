# Estrutura do Projeto

```
SIEG/
│
├── .github/
│   └── workflows/
│       └── dotnet.yml                    # CI/CD com GitHub Actions
│
├── src/
│   ├── FiscalDocAPI/                     # 🌐 API REST Principal
│   │   ├── Controllers/
│   │   │   └── DocumentsController.cs    # Endpoints REST
│   │   │
│   │   ├── Data/
│   │   │   └── FiscalDocContext.cs       # DbContext do EF Core
│   │   │
│   │   ├── DTOs/
│   │   │   └── DocumentDTOs.cs           # Data Transfer Objects
│   │   │
│   │   ├── Models/
│   │   │   ├── FiscalDocument.cs         # Entidade principal
│   │   │   └── DocumentSummary.cs        # DTO para listagem
│   │   │
│   │   ├── Services/
│   │   │   ├── EncryptionService.cs      # 🔐 Criptografia AES
│   │   │   ├── XmlProcessingService.cs   # 📄 Processamento de XML
│   │   │   └── RabbitMQPublisher.cs      # 📨 Publicação de eventos
│   │   │
│   │   ├── Program.cs                    # Configuração da aplicação
│   │   ├── appsettings.json              # Configurações
│   │   ├── GlobalUsings.cs               # Usings globais
│   │   └── FiscalDocAPI.csproj           # Arquivo do projeto
│   │
│   └── FiscalDocAPI.Worker/              # ⚙️ Worker Service
│       ├── RabbitMQConsumerWorker.cs     # Consumidor RabbitMQ
│       ├── Program.cs
│       ├── appsettings.json
│       └── FiscalDocAPI.Worker.csproj
│
├── tests/
│   └── FiscalDocAPI.Tests/               # 🧪 Testes
│       ├── Controllers/
│       │   └── DocumentsControllerTests.cs
│       │
│       ├── Services/
│       │   └── XmlProcessingServiceTests.cs
│       │
│       ├── Integration/
│       │   └── DocumentsApiIntegrationTests.cs
│       │
│       └── FiscalDocAPI.Tests.csproj
│
├── samples/                               # 📑 XMLs de Exemplo
│   ├── nfe-example.xml                   # Exemplo de NFe
│   └── cte-example.xml                   # Exemplo de CTe
│
├── .gitignore                            # Arquivos ignorados pelo Git
├── docker-compose.yml                    # 🐳 Orquestração Docker
├── Dockerfile                            # Imagem da API
├── Dockerfile.Worker                     # Imagem do Worker
├── FiscalDocAPI.sln                      # Solution .NET
│
├── dev-setup.ps1                         # Setup automático (Windows)
├── dev-setup.sh                          # Setup automático (Linux/Mac)
│
├── LICENSE                               # Licença MIT
├── README.md                             # 📖 Documentação principal
├── QUICKSTART.md                         # ⚡ Guia de início rápido
├── TESTING.md                            # 🧪 Guia de testes
└── ARCHITECTURE.md                       # Este arquivo

```

## 🏗️ Camadas da Arquitetura

### 1. **Presentation Layer (API Controllers)**
- Recebe requisições HTTP
- Valida input
- Retorna respostas formatadas
- Documentação Swagger

### 2. **Business Logic Layer (Services)**
- `XmlProcessingService`: Parse e validação de XMLs
- `EncryptionService`: Criptografia/descriptografia
- `RabbitMQPublisher`: Publicação de eventos

### 3. **Data Access Layer**
- Entity Framework Core
- Repository Pattern (via DbContext)
- Migrations para versionamento do schema

### 4. **Infrastructure Layer**
- RabbitMQ para mensageria
- SQL Server para persistência
- Docker para containerização

## 🔄 Fluxo de Dados

### Upload de XML

```
┌─────────┐       ┌──────────┐       ┌─────────────┐       ┌──────────┐
│ Cliente │──1──▶│Controller│──2──▶│XmlProcessor │──3──▶│ Database │
└─────────┘       └──────────┘       └─────────────┘       └──────────┘
                                            │
                                            │ 4. Publica
                                            ▼
                                       ┌──────────┐
                                       │ RabbitMQ │
                                       └──────────┘
                                            │
                                            │ 5. Consome
                                            ▼
                                       ┌──────────┐
                                       │  Worker  │
                                       └──────────┘
```

**Passos:**
1. Cliente envia XML via HTTP POST
2. Controller valida e chama XmlProcessingService
3. Service processa, criptografa e salva no banco
4. Publica evento no RabbitMQ
5. Worker consome evento e processa (resumo, indexação, etc)

### Consulta de Documentos

```
┌─────────┐       ┌──────────┐       ┌──────────┐
│ Cliente │──1──▶│Controller│──2──▶│ Database │
└─────────┘       └──────────┘       └──────────┘
                       │
                       │ 3. Retorna
                       ▼
                  ┌─────────┐
                  │ Cliente │
                  └─────────┘
```

## 🗄️ Schema do Banco de Dados

### Tabela: FiscalDocuments

| Coluna           | Tipo          | Descrição                        |
|------------------|---------------|----------------------------------|
| Id               | GUID (PK)     | Identificador único              |
| DocumentType     | VARCHAR(100)  | NFe, CTe ou NFSe                 |
| DocumentKey      | VARCHAR(50)   | Chave de acesso (UNIQUE)         |
| EmitterCnpj      | VARCHAR(14)   | CNPJ do emissor (INDEX)          |
| EmitterName      | VARCHAR(200)  | Nome do emissor                  |
| EmitterUF        | VARCHAR(2)    | UF do emissor (INDEX)            |
| RecipientCnpj    | VARCHAR(14)   | CNPJ do destinatário             |
| RecipientName    | VARCHAR(200)  | Nome do destinatário             |
| TotalValue       | DECIMAL(18,2) | Valor total                      |
| IssueDate        | DATETIME      | Data de emissão (INDEX)          |
| CreatedAt        | DATETIME      | Data de criação (INDEX)          |
| UpdatedAt        | DATETIME      | Data de atualização              |
| XmlContent       | NVARCHAR(MAX) | XML criptografado                |
| XmlHash          | VARCHAR(64)   | Hash SHA256 (INDEX)              |
| ProcessingStatus | VARCHAR(50)   | Status: Pending/Processed/Error  |
| AdditionalData   | NVARCHAR(MAX) | JSON com dados extras            |

**Índices:**
- PK: `Id`
- UNIQUE: `DocumentKey`
- INDEX: `XmlHash`, `EmitterCnpj`, `EmitterUF`, `IssueDate`, `CreatedAt`

## 🔐 Segurança

### Dados Sensíveis
```
XML Original → AES-256 Encryption → Base64 → Database
                     ↑
                 Encryption Key
            (Azure Key Vault recomendado)
```

### Hash para Idempotência
```
XML Content → SHA256 → Hash (64 chars hex)
                         ↓
                    Verificação de duplicidade
```

## 📨 Mensageria (RabbitMQ)

### Estrutura

```
Exchange: fiscal-exchange (Topic)
    │
    ├─ Routing Key: fiscal.document.processed
    │       ↓
    └─ Queue: fiscal-documents
            ↓
        Consumer (Worker)
```

### Eventos

**DocumentProcessedEvent:**
```json
{
  "documentId": "guid",
  "documentType": "NFe",
  "documentKey": "44-digit-key",
  "emitterCnpj": "12345678000190",
  "totalValue": 1500.00,
  "processedAt": "2024-01-15T10:30:00Z"
}
```

## 🛡️ Resiliência

### Retry Policy (Polly)
```
Tentativa 1 → Falha → Aguarda 2s
Tentativa 2 → Falha → Aguarda 4s
Tentativa 3 → Falha → Aguarda 8s
Tentativa 4 → Falha → Aguarda 16s
Tentativa 5 → Falha → Aguarda 32s
              ↓
          DLQ ou Log
```

### Circuit Breaker Pattern
- Após 5 falhas consecutivas, o circuit abre
- Aguarda 60 segundos antes de tentar novamente
- Previne sobrecarga do banco/RabbitMQ

## 🔧 Configuração

### appsettings.json (Estrutura)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "QueueName": "fiscal-documents",
    "ExchangeName": "fiscal-exchange",
    "RoutingKey": "fiscal.document.processed"
  },
  "Encryption": {
    "Key": "32-character-key",
    "IV": "16-character-iv"
  }
}
```

### Variáveis de Ambiente (Produção)
```bash
ConnectionStrings__DefaultConnection=...
RabbitMQ__HostName=...
Encryption__Key=...  # Usar Azure Key Vault!
```

## 🧪 Estratégia de Testes

### 1. **Unit Tests**
- Testam lógica de negócio isolada
- Mock de dependências
- Cobertura: Services, Helpers

### 2. **Integration Tests**
- Testam fluxo completo
- Banco em memória (InMemory)
- Cobertura: Controllers, API endpoints

### 3. **Load Tests** (Opcional)
- k6 ou NBomber
- Simula carga de 100+ req/s
- Identifica gargalos

## 📊 Monitoramento (Futuro)

### Health Checks
```
/health/ready   → API está pronta?
/health/live    → API está viva?
/health/db      → Banco acessível?
/health/rabbit  → RabbitMQ acessível?
```

### Metrics (OpenTelemetry)
- Tempo de processamento de XML
- Taxa de upload por minuto
- Taxa de erro
- Latência de consultas

### Logging
- Structured logging com Serilog
- Níveis: Debug, Info, Warning, Error
- Sink: Console, File, Elasticsearch

## 🚀 Deploy

### Docker Compose (Desenvolvimento)
```bash
docker-compose up -d
```

### Kubernetes (Produção)
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: fiscal-doc-api
spec:
  replicas: 3
  template:
    spec:
      containers:
      - name: api
        image: fiscaldocapi:latest
        env:
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: connection-string
```

## 🔄 CI/CD Pipeline

### GitHub Actions

```
Push → GitHub
  ↓
Build & Test
  ↓
Docker Build
  ↓
Push to Registry
  ↓
Deploy to Production
```

## 📈 Escalabilidade

### Horizontal Scaling
- API: múltiplas instâncias atrás de load balancer
- Worker: múltiplos consumers na mesma fila
- RabbitMQ: cluster mode
- SQL Server: read replicas

### Vertical Scaling
- Aumentar CPU/RAM dos containers
- Otimizar queries com índices
- Cache com Redis

## 🎯 Decisions Log

### Por que SQL Server?
- ✅ ACID transactions
- ✅ Relacional: bom para documentos fiscais
- ✅ Suporte robusto do EF Core
- ❌ Alternativa: MongoDB (NoSQL) seria válida

### Por que RabbitMQ?
- ✅ Mensageria confiável
- ✅ Fácil configuração
- ✅ Topic Exchange flexível
- ❌ Alternativa: Azure Service Bus, Kafka

### Por que Worker separado?
- ✅ Separação de responsabilidades
- ✅ Escalabilidade independente
- ✅ Não bloqueia API
- ✅ Pode rodar em container separado

---

**Para mais informações:**
- [README.md](README.md) - Documentação geral
- [QUICKSTART.md](QUICKSTART.md) - Início rápido
- [TESTING.md](TESTING.md) - Guia de testes
