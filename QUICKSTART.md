# ⚡ Guia de Início Rápido

Inicie o projeto em **5 minutos**!

## 📋 Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop) instalado e rodando
- [Git](https://git-scm.com/downloads)

## 🚀 Passos

### 1. Clone o repositório

```bash
git clone <repository-url>
cd SIEG
```

### 2. Inicie os containers

```bash
docker-compose up -d
```

Isso vai iniciar:
- ✅ SQL Server na porta 1433
- ✅ RabbitMQ nas portas 5672 e 15672
- ✅ API na porta 5000
- ✅ Worker para consumir eventos

### 3. Aguarde os serviços iniciarem

```bash
# Verifique o status
docker-compose ps

# Acompanhe os logs
docker-compose logs -f
```

Aguarde até ver:
```
api_1     | Now listening on: http://0.0.0.0:80
worker_1  | Worker aguardando mensagens na fila: fiscal-documents
```

### 4. Acesse a documentação Swagger

Abra no navegador: **http://localhost:5000/swagger**

### 5. Faça seu primeiro upload!

#### Opção A: Via Swagger UI

1. No Swagger, expanda `POST /api/documents/upload`
2. Clique em **"Try it out"**
3. Clique em **"Choose File"** e selecione `samples/nfe-example.xml`
4. Clique em **"Execute"**
5. ✅ Você deve ver uma resposta com o `documentId`!

#### Opção B: Via cURL (linha de comando)

```bash
curl -X POST "http://localhost:5000/api/documents/upload" \
  -F "xmlFile=@samples/nfe-example.xml"
```

#### Opção C: Via PowerShell (Windows)

```powershell
$boundary = [System.Guid]::NewGuid().ToString()
$file = Get-Content "samples\nfe-example.xml" -Raw

$body = @"
--$boundary
Content-Disposition: form-data; name="xmlFile"; filename="nfe-example.xml"
Content-Type: text/xml

$file
--$boundary--
"@

Invoke-RestMethod -Uri "http://localhost:5000/api/documents/upload" `
    -Method Post `
    -ContentType "multipart/form-data; boundary=$boundary" `
    -Body $body
```

### 6. Liste os documentos

```bash
curl "http://localhost:5000/api/documents?page=1&pageSize=10"
```

Ou no Swagger: `GET /api/documents`

### 7. Verifique o Worker processando eventos

```bash
docker-compose logs -f worker
```

Você deve ver algo como:
```
📄 Novo documento processado!
ID: xxxxx-xxxx-xxxx-xxxx-xxxxxxxxx
Tipo: NFe
CNPJ Emissor: 12.345.678/0001-90
...
```

## 🎉 Pronto!

Você agora tem:
- ✅ API REST rodando
- ✅ Banco de dados SQL Server
- ✅ RabbitMQ para mensageria
- ✅ Worker processando eventos
- ✅ Documentação Swagger interativa

## 🔍 O que fazer agora?

### Explore os endpoints:
- 📤 **Upload** de XMLs fiscais
- 📋 **Liste** documentos com filtros (CNPJ, UF, data, tipo)
- 🔍 **Consulte** detalhes de um documento
- ✏️ **Atualize** informações
- 🗑️ **Delete** documentos

### Teste a idempotência:
Faça upload do mesmo XML duas vezes - ele não será duplicado!

### Monitore o RabbitMQ:
Acesse: http://localhost:15672 (usuário: `guest`, senha: `guest`)

### Execute os testes:
```bash
dotnet test
```

## 🛑 Parar os serviços

```bash
docker-compose down
```

## 🗄️ Limpar todos os dados

```bash
docker-compose down -v
```

## ❓ Problemas?

### API não inicia
```bash
# Verifique os logs
docker-compose logs api

# Recrie os containers
docker-compose down
docker-compose up -d --build
```

### Erro de conexão com o banco
```bash
# O SQL Server pode demorar ~30s para iniciar
# Aguarde e tente novamente
docker-compose restart api
```

### Porta já em uso
Edite `docker-compose.yml` e altere as portas:
```yaml
ports:
  - "5001:80"  # Mude 5000 para 5001
```

## 📚 Próximos Passos

- Leia o [README.md](README.md) completo para entender a arquitetura
- Veja [TESTING.md](TESTING.md) para mais exemplos de testes
- Explore os testes unitários em `tests/FiscalDocAPI.Tests/`

---

**Divirta-se explorando a API! 🚀**
