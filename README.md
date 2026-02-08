# 📄 Fiscal Document API

API REST para processamento de documentos fiscais XML (NFe, CTe, NFSe) desenvolvida em **ASP.NET Core 8.0**.

## 🎯 Funcionalidades

- ✅ **Upload e processamento de XMLs fiscais** (NFe, CTe, NFSe)
- ✅ **Armazenamento seguro** com criptografia de dados sensíveis
- ✅ **Garantia de idempotência** - previne duplicação de documentos
- ✅ **RabbitMQ** para mensageria assíncrona
- ✅ **Worker service** para consumo de eventos
- ✅ **Resiliência** com Polly (retry com backoff exponencial)
- ✅ **Nack e descarte** de mensagens com erro após todas as tentativas
- ✅ **API REST completa** com operações CRUD
- ✅ **Paginação e filtros** avançados (data, CNPJ, UF, tipo)
- ✅ **Logging estruturado** com ILogger para auditoria e debugging
- ✅ **Documentação Swagger**
- ✅ **Testes unitários** com NUnit (38 testes)

## 🏗️ Arquitetura

### Decisões Técnicas

**1. Banco de Dados: SQL Server**
- ✅ Suporte robusto para transações ACID
- ✅ Índices otimizados para consultas por data, CNPJ, UF
- ✅ Entity Framework Core para migrations e ORM
- ✅ Constraint UNIQUE na chave do documento para garantir unicidade

**2. Mensageria: RabbitMQ**
- ✅ Mensageria confiável e escalável
- ✅ Topic Exchange para flexibilidade no roteamento
- ✅ Persistência de mensagens
- ✅ Dead Letter Queue para tratamento de falhas

**3. Segurança**
- ✅ XML criptografado com AES antes de armazenar
- ✅ Hash SHA256 para verificação de integridade e idempotência
- ✅ Gitignore configurado para não vazar secrets

**4. Clean Architecture (Arquitetura em Camadas)**
- ✅ **Domain Layer**: Entidades de negócio e interfaces (independente de frameworks)
- ✅ **Application Layer**: Casos de uso, lógica de negócio e orquestração
- ✅ **Infrastructure Layer**: Implementações concretas (BD, RabbitMQ, XML parsing)
- ✅ **API Layer**: Controllers, DTOs, configuração e endpoints REST

**Benefícios da Clean Architecture:**
- 🎯 **Separação de responsabilidades**: Cada camada tem um propósito claro
- 🔄 **Testabilidade**: Fácil criar mocks e testar lógica isoladamente
- 🔌 **Baixo acoplamento**: Mudanças em uma camada não afetam as outras
- 📦 **Independência de frameworks**: Domínio não depende de EF Core ou ASP.NET
- 🚀 **Manutenibilidade**: Código organizado facilita evolução do sistema
- 🔁 **Inversão de dependência**: Camadas externas dependem das internas (DIP)

**5. Clean Code e SOLID**

O projeto aplica extensivamente princípios de código limpo e SOLID:

**Single Responsibility Principle (SRP)**
**Princípios aplicados:**

- **SRP**: `DocumentService` delega responsabilidades (`ReadXmlContentAsync`, `CheckIdempotencyByHashAsync`, `SaveDocumentAsync`, `PublishDocumentProcessedEventAsync`); `XmlParser` usa Extract Method pattern
- **DIP**: Abstrações via interfaces (`IDocumentService`, `IXmlParser`, `IEncryptionService`, `IMessagePublisher`), injeção no construtor
- **Guard Clauses**: Early returns em validações (`if (existingDoc == null) return null;`)
- **Constants**: `AppConstants.cs` centraliza valores (paginação, status, mensagens, routing keys)
- **Logging**: `ILogger<T>` injetado, logs estruturados para debugging/auditoria
- **AutoMapper**: Elimina ~30 linhas de boilerplate/método; mappings `FiscalDocument` → `DocumentSummaryDto`/`DocumentDetailDto`
- **Proteção XXE**: XML parsing seguro com `XmlReaderSettings` (`DtdProcessing.Prohibit`, `XmlResolver = null`)

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

## 🚀 Como Rodar Localmente

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

**8. Acesse a API:**
- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- Health Check: http://localhost:5000/health
- RabbitMQ Management: http://localhost:15672 (guest/guest)

> **Nota:** HTTPS está desabilitado para desenvolvimento local. Use HTTP (porta 5000).

## 🧪 Executando os Testes

### Testes Unitários (38 testes)
```bash
# Todos os testes unitários
dotnet test tests/FiscalDocAPI.Tests/FiscalDocAPI.Tests.csproj

# Com detalhes
dotnet test --logger "console;verbosity=detailed"

# Testes por categoria
dotnet test --filter "FullyQualifiedName~FiscalDocAPI.Tests.Services"
dotnet test --filter "FullyQualifiedName~FiscalDocAPI.Tests.Controllers"
```

### Testes de Integração (7 testes)

Testes end-to-end que validam a integração entre camadas usando `WebApplicationFactory` e banco InMemory.

**Cenários testados:**
1. ✅ Upload de XML válido
2. ✅ Upload sem arquivo (BadRequest)
3. ✅ Listagem paginada de documentos
4. ✅ Consulta documento por ID existente
5. ✅ Consulta documento por ID inexistente (NotFound)
6. ✅ Exclusão de documento
7. ✅ Health check endpoint

**Executar:**
```bash
dotnet test tests/FiscalDocAPI.IntegrationTests/FiscalDocAPI.IntegrationTests.csproj
```

**Tecnologias:**
- `Microsoft.AspNetCore.Mvc.Testing` - WebApplicationFactory
- `EntityFrameworkCore.InMemory` - Banco de dados em memória para testes
- `FluentAssertions` - Asserções fluentes
- `NUnit` - Framework de testes

### Testes de Carga (NBomber)

Testes de performance e resiliência com **NBomber** para validar comportamento sob carga.

#### 📊 Cenários Testados

**1️⃣ Ingestão de XML (POST)**
- **Endpoint**: `POST /api/documents/upload`
- **Carga**: 10 requisições/segundo por 30 segundos
- **Métricas**:
  - Throughput (req/s)
  - Latência (p50, p75, p95, p99)
  - Taxa de erro
- **Observação**: Valida idempotência sob carga

**2️⃣ Consulta Paginada (GET)**
- **Endpoint**: `GET /api/documents?page={page}&pageSize=10`
- **Carga**: 50 requisições/segundo por 30 segundos
- **Métricas**:
  - Tempo de resposta
  - Throughput
  - Taxa de sucesso
- **Observação**: Valida índices e filtros

#### 🏃 Como Executar

**Pré-requisitos:**
1. API rodando em `http://localhost:5000`
2. Banco de dados configurado
3. RabbitMQ rodando (para processamento completo)

**Executar os testes:**
```bash
# Da raiz do projeto
cd tests/LoadTests
dotnet run
```

Ou direto:
```bash
dotnet run --project tests/LoadTests/LoadTests.csproj
```

#### 📈 Relatórios

Após a execução, os relatórios são gerados em:
- `tests/LoadTests/Reports/fiscal_api_load_test.html` (visualização gráfica)
- `tests/LoadTests/Reports/fiscal_api_load_test.md` (markdown)

Abra o HTML no navegador para análise detalhada:
- Gráficos de latência
- Throughput ao longo do tempo
- Distribuição de status codes
- Percentis (p50, p75, p95, p99)

#### 🎯 Resultados Esperados

**Ingestão (POST):**
- ✅ Latência p95 < 500ms
- ✅ Taxa de sucesso > 95%
- ✅ Idempotência funcionando (mesmo XML não duplica)

**Consulta (GET):**
- ✅ Latência p95 < 200ms
- ✅ Taxa de sucesso > 99%
- ✅ Índices otimizando consultas

#### 🔧 Personalização

Edite `DocumentLoadTests.cs` para ajustar:
- Taxa de requisições (`rate`)
- Duração do teste (`during`)
- Páginas consultadas (randomização)
- XMLs utilizados (pasta `Samples/`)

#### 💡 Dicas

1. **Warm-up**: Execute uma vez para warm-up do sistema antes de testes definitivos
2. **Monitoramento**: Observe CPU, memória e I/O durante os testes
3. **Baseline**: Execute sem carga primeiro para estabelecer baseline
4. **Isolamento**: Rode em ambiente sem outras cargas para resultados precisos

## 📝 Endpoints da API

### 1. Upload de XML
```http
POST /api/documents/upload
Content-Type: multipart/form-data

Form Data:
  xmlFile: [arquivo.xml]
```

**Resposta:**
```json
{
  "documentId": "guid",
  "message": "Documento processado com sucesso",
  "isNewDocument": true
}
```

### 2. Listar Documentos (com paginação e filtros)
```http
GET /api/documents?page=1&pageSize=10&cnpj=12345678000190&uf=SP&startDate=2024-01-01&endDate=2024-12-31&documentType=NFe
```

**Resposta:**
```json
{
  "items": [...],
  "page": 1,
  "pageSize": 10,
  "totalCount": 100,
  "totalPages": 10
}
```

### 3. Consultar Documento Específico
```http
GET /api/documents/{id}
```

### 4. Atualizar Documento
```http
PUT /api/documents/{id}
Content-Type: application/json

{
  "emitterName": "Novo Nome",
  "processingStatus": "Processed",
  "additionalData": "{\"custom\": \"data\"}"
}
```

### 5. Excluir Documento
```http
DELETE /api/documents/{id}
```

## 🔒 Segurança e Dados Sensíveis

### Criptografia
- XMLs são criptografados usando **AES-256** antes de serem salvos
- Chaves de criptografia devem ser armazenadas em **Azure Key Vault** ou similar em produção

### Configuração de Secrets (Produção)

**Não commite secrets!** Use variáveis de ambiente ou um gerenciador de secrets:

```bash
# Azure Key Vault
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets

# User Secrets (desenvolvimento)
dotnet user-secrets init
dotnet user-secrets set "Encryption:Key" "sua-chave-32-caracteres-aqui!!"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "sua-connection-string"
```

## 📊 Idempotência

A API garante idempotência através de:

1. **Hash SHA256** do conteúdo XML completo
2. **Chave única** do documento (chave de acesso NFe/CTe)
3. Índices únicos no banco de dados

Se o mesmo XML for enviado múltiplas vezes, o sistema:
- ✅ Retorna o documento existente
- ✅ Não duplica dados
- ✅ Não gera eventos duplicados no RabbitMQ

## 🔄 Resiliência no RabbitMQ

O Consumer implementa:

- **Retry com backoff exponencial** usando Polly (5 tentativas)
- **Auto-recovery** em caso de queda de conexão
- **QoS** configurado para processar 1 mensagem por vez
- **BasicNack** para rejeitar mensagens com erro após todas as tentativas (sem requeue)

## 📈 Performance

### Índices Otimizados
- `DocumentKey` (UNIQUE)
- `XmlHash`
- `EmitterCnpj`
- `EmitterUF`
- `IssueDate`
- `CreatedAt`

### Boas Práticas Implementadas
- ✅ Paginação em todas as listagens
- ✅ Queries otimizadas com EF Core
- ✅ Async/await em todas as operações I/O
- ✅ Connection pooling do SQL Server
- ✅ Logging estruturado com ILogger<T> em todos os serviços
- ✅ AutoMapper para eliminar mapeamento manual de DTOs
- ✅ Testes de carga com NBomber (ingestão e consulta)
- ✅ Caching potencial (pode adicionar Redis se necessário)

## 🧭 Melhorias Futuras

### Sugeridas para tempo adicional:
- [ ] **Docker e Docker Compose**: Containerização da aplicação completa
- [ ] **Dead Letter Queue (DLQ)**: Para mensagens que falharam após todas as tentativas de retry
- [ ] **CQRS (Command Query Responsibility Segregation)**: Separar operações de escrita (Commands) e leitura (Queries) com MediatR
  - Commands: Upload, Update, Delete de documentos
  - Queries: Listagens otimizadas com projections específicas
  - Benefícios: Performance, escalabilidade independente, models otimizados
- [ ] **Event Sourcing**: Armazenar histórico completo de mudanças nos documentos
- [ ] **Elasticsearch** para busca full-text
- [ ] **Redis** para caching de consultas frequentes
- [ ] **Azure Blob Storage** para armazenar XMLs grandes
- [ ] **Rate limiting** com AspNetCoreRateLimit
- [ ] **OpenTelemetry** para observabilidade
- [ ] **Testes de arquitetura** com NetArchTest
- [ ] **CI/CD** com GitHub Actions
- [ ] **Authentication/Authorization** com JWT

## 📚 Documentação Adicional

### Swagger
Acesse `/swagger` para documentação interativa completa da API.

## 🤝 Como Contribuir

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 📄 Licença

Este projeto é licenciado sob a [MIT License](LICENSE).

## 👨‍💻 Autor

Desenvolvido como parte do desafio técnico SIEG.

---

**⚠️ IMPORTANTE:** Este é um projeto de demonstração. Para uso em produção:
- Configure secrets adequadamente (Azure Key Vault)
- Implemente autenticação e autorização
- Configure SSL/TLS em produção
- Ajuste resource limits nos containers
- Implemente backups do banco de dados
- Configure monitoring e alertas
