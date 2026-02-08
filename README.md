# Fiscal Document API

API para ingestão e consulta de documentos fiscais (NFe, CTe, NFSe). Feita em ASP.NET Core 8 com Clean Architecture.

## Funcionalidades

- Upload e processamento de XMLs fiscais
- Armazenamento com criptografia AES
- Idempotência (previne duplicação via hash SHA256)
- Mensageria assíncrona com RabbitMQ
- Worker service para consumo de eventos
- Retry automático com Polly (backoff exponencial)
- API REST com CRUD completo
- Paginação e filtros (data, CNPJ, UF, tipo)
- Logging estruturado (ILogger)
- Swagger
- 38 testes unitários + 7 testes de integração + 2 cenários de carga (NBomber)

## Arquitetura

### Por que Clean Architecture?

Escolhi Clean Architecture porque já trabalhei com ela em projetos anteriores e resolvia bem os problemas desse desafio:

- **Testabilidade**: Consigo mockar qualquer dependência (DB, RabbitMQ, file system)
- **Independência de frameworks**: Domain não sabe que existe EF Core ou ASP.NET
- **Manutenibilidade**: Mudanças ficam isoladas em suas camadas

**Trade-off**: Mais arquivos e abstrações, mas vale pela facilidade de testar e evoluir.

**Camadas:**
- **Domain**: Entidades e contratos (zero dependências externas)
- **Application**: Lógica de negócio e orquestração
- **Infrastructure**: Implementações concretas (SQL, RabbitMQ, XML)
- **API**: Controllers e configuração

### Decisões Técnicas

**SQL Server**

Precisava de transações ACID e constraints para garantir unicidade. EF Core facilita migrations.

Índices em: `DocumentKey` (UNIQUE), `XmlHash`, `EmitterCnpj`, `EmitterUF`, `IssueDate`.

**RabbitMQ**

Processamento assíncrono era requisito. RabbitMQ garante:
- Mensagens persistidas (não se perdem)
- Retry com backoff (Polly)
- Topic Exchange (flexibilidade no roteamento)

Considerei Kafka mas seria overkill pro volume esperado.

**Segurança**

- XMLs criptografados com AES-256 antes de salvar
- Hash SHA256 para idempotência e integridade
- Proteção XXE: `XmlReaderSettings` com `DtdProcessing.Prohibit`

**Clean Code**

Apliquei SOLID e padrões:
- **SRP**: `DocumentService` delega em métodos privados
- **DIP**: Tudo via interfaces injetadas
- **Guard Clauses**: Early returns
- **AutoMapper**: Elimina boilerplate de mapeamento manual
- **Constants**: Valores centralizados em `AppConstants.cs`

### Estrutura do Projeto

```
SIEG/
├── src/
│   ├── FiscalDocAPI.Domain/           # Camada de Domínio
│   │   ├── Constants/                 # Constantes de negócio
│   │   ├── Entities/                  # Entidades de domínio
│   │   ├── Events/                    # Eventos de domínio
│   │   └── Interfaces/                # Contratos de repositórios
│   ├── FiscalDocAPI.Application/      # Camada de Aplicação
│   │   ├── DTOs/                      # Data Transfer Objects
│   │   ├── Interfaces/                # Contratos de serviços
│   │   ├── Mappings/                  # Profiles do AutoMapper
│   │   ├── Services/                  # Lógica de negócio
│   │   └── DependencyInjection.cs     # Configuração de DI
│   ├── FiscalDocAPI.Infrastructure/   # Camada de Infraestrutura
│   │   ├── Messaging/                 # RabbitMQ Publisher
│   │   ├── Migrations/                # Migrations EF Core
│   │   ├── Persistence/               # DbContext e Repositories
│   │   ├── Security/                  # Criptografia
│   │   ├── Xml/                       # XML Parser
│   │   └── DependencyInjection.cs     # Configuração de DI
│   ├── FiscalDocAPI/                  # Camada de API
│   │   ├── Controllers/               # Endpoints REST
│   │   └── Program.cs                 # Configuração da aplicação
│   └── FiscalDocAPI.Worker/           # Worker para consumo RabbitMQ
│       └── RabbitMQConsumerWorker.cs
├── tests/
│   ├── FiscalDocAPI.Tests/            # Testes unitários (38 testes)
│   │   ├── Controllers/
│   │   ├── Services/
│   │   └── ...
│   ├── FiscalDocAPI.IntegrationTests/ # Testes de integração (7 testes)
│   │   ├── DocumentsControllerIntegrationTests.cs
│   │   ├── WebApplicationFactoryFixture.cs
│   │   └── TestData/
│   └── LoadTests/                     # Testes de carga (NBomber)
│       ├── DocumentLoadTests.cs
│       └── Samples/
└── README.md                          # Este arquivo
```

## Como Rodar

### Pré-requisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local ou Docker)
- RabbitMQ (local ou Docker)

### Configuração

**1. Clone o repositório:**
```bash
git clone <repository-url>
cd SIEG
```

**2. Inicie o SQL Server:**

Opção 1 - Com Docker:
```bash
docker run -d --name sqlserver \
  -e 'ACCEPT_EULA=Y' \
  -e 'SA_PASSWORD=YourStrong@Passw0rd' \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
```

Opção 2 - SQL Server local instalado

**3. Inicie o RabbitMQ:**
```bash
# Com Docker
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# Ou instale localmente: https://www.rabbitmq.com/download.html
```

**4. Configure a aplicação:**

Edite `src/FiscalDocAPI/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=FiscalDocDB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest"
  }
}
```

**5. Aplique as migrations:**
```bash
# Da raiz do projeto
dotnet ef database update --project src/FiscalDocAPI.Infrastructure --startup-project src/FiscalDocAPI
```

**6. Execute a API:**
```bash
# A partir da raiz do projeto (pasta SIEG)
cd src/FiscalDocAPI
dotnet run
```

Ou direto:
```bash
dotnet run --project src/FiscalDocAPI/FiscalDocAPI.csproj
```

**7. Execute o Worker (em outro terminal):**
```bash
# A partir da raiz do projeto (pasta SIEG)
cd src/FiscalDocAPI.Worker
dotnet run
```

Ou direto:
```bash
dotnet run --project src/FiscalDocAPI.Worker/FiscalDocAPI.Worker.csproj
```

**8. Acesse:**
- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- Health: http://localhost:5000/health
- RabbitMQ: http://localhost:15672 (guest/guest)

Nota: HTTPS desabilitado no dev local (só HTTP na porta 5000).

## Testes

### Unitários (38 testes)
```bash
# Todos
dotnet test tests/FiscalDocAPI.Tests/FiscalDocAPI.Tests.csproj

# Por categoria
dotnet test --filter "FullyQualifiedName~FiscalDocAPI.Tests.Services"
```

### Integração (7 testes)

Testes end-to-end com `WebApplicationFactory` e InMemory database.

**Cenários:** Upload válido, validação de erros, listagem paginada, busca por ID, deleção, health check.

```bash
dotnet test tests/FiscalDocAPI.IntegrationTests/FiscalDocAPI.IntegrationTests.csproj
```

### Carga (NBomber)

**Cenário 1 - Ingestão:**
- `POST /api/documents/upload`
- 10 req/s por 30s
- Valida idempotência sob carga

**Cenário 2 - Consulta:**
- `GET /api/documents?page={page}&pageSize=10`
- 50 req/s por 30s
- Valida performance dos índices

**Executar:**
```bash
cd tests/LoadTests
dotnet run
```

Relatórios gerados em `tests/LoadTests/Reports/` (HTML + Markdown).

**Resultados esperados:**
- Ingestão: p95 < 500ms, sucesso > 95%
- Consulta: p95 < 200ms, sucesso > 99%

## Endpoints

### Upload XML
```http
POST /api/documents/upload
Content-Type: multipart/form-data

Form Data:
  xmlFile: [arquivo.xml]
```

Resposta:
```json
{
  "documentId": "guid",
  "message": "Documento processado com sucesso",
  "isNewDocument": true
}
```

### Listar Documentos
```http
GET /api/documents?page=1&pageSize=10&cnpj=12345678000190&uf=SP&startDate=2024-01-01
```

### Consultar / Atualizar / Excluir
```http
GET /api/documents/{id}
PUT /api/documents/{id}
DELETE /api/documents/{id}
```

## Segurança

**Criptografia:** XMLs são criptografados com AES-256 antes de salvar.

**Produção:** Use Azure Key Vault ou User Secrets. Não commite secrets!

```bash
dotnet user-secrets init
dotnet user-secrets set "Encryption:Key" "sua-chave-32-caracteres"
```

## Idempotência

Garantida por:
- Hash SHA256 do XML completo
- Chave única do documento (constraint UNIQUE no DB)

Se enviar o mesmo XML 2x: retorna o documento existente, não duplica.

## Resiliência

RabbitMQ consumer tem:
- Retry com backoff exponencial (Polly, 5 tentativas)
- Auto-recovery em quedas de conexão
- QoS configurado (1 msg por vez)
- BasicNack após esgotar retries (sem requeue)

## Performance

Índices: `DocumentKey` (UNIQUE), `XmlHash`, `EmitterCnpj`, `EmitterUF`, `IssueDate`.

Práticas: paginação, async/await, connection pooling, AutoMapper, logging estruturado.

## Melhorias Futuras

### Críticas (Produção)
- [ ] Authentication/Authorization (JWT)
- [ ] Docker Compose (API + Worker + SQL + RabbitMQ)
- [ ] Rate Limiting
- [ ] Dead Letter Queue com reprocessamento
- [ ] CI/CD pipeline

### Importantes (Escala)
- [ ] Redis para caching
- [ ] OpenTelemetry (distributed tracing)
- [ ] Azure Blob Storage (XMLs > 1MB)
- [ ] NetArchTest (validação de arquitetura)

### Opcionais
- [ ] Elasticsearch (se precisar busca full-text)

---

**Desenvolvido para o desafio técnico SIEG.**
