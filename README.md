# 📄 Fiscal Document API

API REST para processamento de documentos fiscais XML (NFe, CTe, NFSe) desenvolvida em **ASP.NET Core 8.0**.

## 🎯 Funcionalidades

- ✅ **Upload e processamento de XMLs fiscais** (NFe, CTe, NFSe)
- ✅ **Armazenamento seguro** com criptografia de dados sensíveis
- ✅ **Garantia de idempotência** - previne duplicação de documentos
- ✅ **RabbitMQ** para mensageria assíncrona
- ✅ **Worker service** para consumo de eventos
- ✅ **Resiliência** com Polly (retry com backoff exponencial)
- ✅ **API REST completa** com operações CRUD
- ✅ **Paginação e filtros** avançados (data, CNPJ, UF, tipo)
- ✅ **Documentação Swagger**
- ✅ **Testes unitários e de integração** com NUnit
- ✅ **Docker e Docker Compose** para fácil execução

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

### Estrutura do Projeto

```
SIEG/
├── src/
│   ├── FiscalDocAPI/              # API REST principal
│   │   ├── Controllers/           # Endpoints REST
│   │   ├── Data/                  # DbContext
│   │   ├── DTOs/                  # Data Transfer Objects
│   │   ├── Models/                # Entidades do domínio
│   │   ├── Services/              # Lógica de negócio
│   │   └── Program.cs             # Configuração da aplicação
│   └── FiscalDocAPI.Worker/       # Worker para consumo RabbitMQ
│       └── RabbitMQConsumerWorker.cs
├── tests/
│   └── FiscalDocAPI.Tests/        # Testes unitários e integração
│       ├── Controllers/
│       ├── Services/
│       └── Integration/
├── docker-compose.yml             # Orquestração de containers
├── Dockerfile                     # Imagem da API
└── README.md                      # Este arquivo
```

## 🚀 Como Rodar Localmente

### Pré-requisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (opcional, mas recomendado)

### Opção 1: Com Docker (Recomendado)

**1. Clone o repositório:**
```bash
git clone <repository-url>
cd SIEG
```

**2. Inicie os containers:**
```bash
docker-compose up -d
```

**3. Aplique as migrations do banco de dados:**
```bash
docker-compose exec api dotnet ef database update
```

**4. Acesse a API:**
- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- RabbitMQ Management: http://localhost:15672 (guest/guest)

### Opção 2: Sem Docker

**1. Inicie o SQL Server:**
- Instale o SQL Server localmente
- Ou use uma instância na nuvem
- Atualize a connection string em `appsettings.json`

**2. Inicie o RabbitMQ:**
```bash
# Com Docker
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# Ou instale localmente: https://www.rabbitmq.com/download.html
```

**3. Configure a aplicação:**

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

**4. Aplique as migrations:**
```bash
cd src/FiscalDocAPI
dotnet ef migrations add InitialCreate
dotnet ef database update
```

**5. Execute a API:**
```bash
dotnet run
```

**6. Execute o Worker (em outro terminal):**
```bash
cd src/FiscalDocAPI.Worker
dotnet run
```

**7. Acesse:**
- API: https://localhost:7001
- Swagger: https://localhost:7001/swagger

## 🧪 Executando os Testes

```bash
# Todos os testes
dotnet test

# Com detalhes
dotnet test --logger "console;verbosity=detailed"

# Somente testes unitários
dotnet test --filter "FullyQualifiedName~FiscalDocAPI.Tests.Services"

# Somente testes de integração
dotnet test --filter "FullyQualifiedName~FiscalDocAPI.Tests.Integration"
```

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

- **Retry com backoff exponencial** usando Polly
- **Auto-recovery** em caso de queda de conexão
- **QoS** configurado para processar 1 mensagem por vez
- **Nack** para mensagens com erro após todas as tentativas

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
- ✅ Caching potencial (pode adicionar Redis se necessário)

## 🧭 Melhorias Futuras

### Sugeridas para tempo adicional:
- [ ] **Elasticsearch** para busca full-text
- [ ] **Redis** para caching de consultas frequentes
- [ ] **Azure Blob Storage** para armazenar XMLs grandes
- [ ] **Rate limiting** com AspNetCoreRateLimit
- [ ] **Health checks** para monitoramento
- [ ] **OpenTelemetry** para observabilidade
- [ ] **Testes de carga** com NBomber ou k6
- [ ] **Testes de arquitetura** com NetArchTest
- [ ] **CI/CD** com GitHub Actions
- [ ] **Authentication/Authorization** com JWT

## 📚 Documentação Adicional

### Swagger
Acesse `/swagger` para documentação interativa completa da API.

### Exemplos de XML

Veja a pasta `samples/` para exemplos de XMLs de teste:
- `nfe-example.xml`
- `cte-example.xml`
- `nfse-example.xml`

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
