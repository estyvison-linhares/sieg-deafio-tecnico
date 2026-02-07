# Como Testar a API

## 🧪 Teste Rápido com cURL

### 1. Upload de um XML

```bash
curl -X POST "http://localhost:5000/api/documents/upload" \
  -H "Content-Type: multipart/form-data" \
  -F "xmlFile=@samples/nfe-example.xml"
```

### 2. Listar Documentos

```bash
# Listar todos (primeira página)
curl "http://localhost:5000/api/documents?page=1&pageSize=10"

# Filtrar por CNPJ
curl "http://localhost:5000/api/documents?cnpj=12345678000190"

# Filtrar por UF
curl "http://localhost:5000/api/documents?uf=SP"

# Filtrar por data
curl "http://localhost:5000/api/documents?startDate=2024-01-01&endDate=2024-12-31"

# Combinar filtros
curl "http://localhost:5000/api/documents?cnpj=12345678000190&uf=SP&documentType=NFe"
```

### 3. Consultar Documento Específico

```bash
# Substitua {id} pelo GUID retornado no upload
curl "http://localhost:5000/api/documents/{id}"
```

### 4. Atualizar Documento

```bash
curl -X PUT "http://localhost:5000/api/documents/{id}" \
  -H "Content-Type: application/json" \
  -d '{"processingStatus": "Processed", "emitterName": "Nome Atualizado"}'
```

### 5. Excluir Documento

```bash
curl -X DELETE "http://localhost:5000/api/documents/{id}"
```

## 🧪 Teste com PowerShell (Windows)

### Upload de XML

```powershell
$file = Get-Item "samples\nfe-example.xml"
$boundary = [System.Guid]::NewGuid().ToString()

$bodyLines = @(
    "--$boundary",
    "Content-Disposition: form-data; name=`"xmlFile`"; filename=`"$($file.Name)`"",
    "Content-Type: text/xml",
    "",
    [System.IO.File]::ReadAllText($file.FullName),
    "--$boundary--"
) -join "`r`n"

Invoke-RestMethod -Uri "http://localhost:5000/api/documents/upload" `
    -Method Post `
    -ContentType "multipart/form-data; boundary=$boundary" `
    -Body $bodyLines
```

### Listar Documentos

```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/documents?page=1&pageSize=10" -Method Get
```

## 🧪 Teste de Idempotência

Execute o upload do mesmo XML duas vezes:

```bash
# Primeira vez - deve criar o documento
curl -X POST "http://localhost:5000/api/documents/upload" \
  -H "Content-Type: multipart/form-data" \
  -F "xmlFile=@samples/nfe-example.xml"

# Segunda vez - deve retornar o mesmo documento sem duplicar
curl -X POST "http://localhost:5000/api/documents/upload" \
  -H "Content-Type: multipart/form-data" \
  -F "xmlFile=@samples/nfe-example.xml"
```

Na segunda chamada, você deve ver:
```json
{
  "documentId": "mesmo-guid-da-primeira-chamada",
  "message": "Documento já existente (idempotência)",
  "isNewDocument": false
}
```

## 🧪 Verificar Worker do RabbitMQ

Após fazer upload de um XML, verifique os logs do Worker:

```bash
# Se usando Docker
docker-compose logs -f worker

# Se rodando localmente, veja o console do Worker
```

Você deve ver algo como:
```
[10:30:15 INF] Mensagem recebida: {"documentId":"...","documentType":"NFe",...}
[10:30:15 INF] Processando documento: ...
[10:30:15 INF] Resumo gerado:
📄 Novo documento processado!
ID: ...
Tipo: NFe
...
```

## 🧪 Swagger UI

A forma mais fácil de testar é usando o Swagger:

1. Acesse: http://localhost:5000/swagger
2. Expanda o endpoint desejado
3. Clique em "Try it out"
4. Preencha os parâmetros
5. Clique em "Execute"

## 🧪 Postman Collection

Importe esta collection no Postman:

```json
{
  "info": {
    "name": "Fiscal Document API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Upload XML",
      "request": {
        "method": "POST",
        "header": [],
        "body": {
          "mode": "formdata",
          "formdata": [
            {
              "key": "xmlFile",
              "type": "file",
              "src": "samples/nfe-example.xml"
            }
          ]
        },
        "url": {
          "raw": "http://localhost:5000/api/documents/upload",
          "protocol": "http",
          "host": ["localhost"],
          "port": "5000",
          "path": ["api", "documents", "upload"]
        }
      }
    },
    {
      "name": "List Documents",
      "request": {
        "method": "GET",
        "url": {
          "raw": "http://localhost:5000/api/documents?page=1&pageSize=10",
          "protocol": "http",
          "host": ["localhost"],
          "port": "5000",
          "path": ["api", "documents"],
          "query": [
            {"key": "page", "value": "1"},
            {"key": "pageSize", "value": "10"}
          ]
        }
      }
    }
  ]
}
```

## 🧪 Testes Automatizados

```bash
# Executar todos os testes
dotnet test

# Executar com cobertura (requer coverlet)
dotnet test /p:CollectCoverage=true

# Testes específicos
dotnet test --filter "FullyQualifiedName~XmlProcessingServiceTests"
```

## 🧪 Teste de Carga (Opcional)

### Com k6:

```javascript
// load-test.js
import http from 'k6/http';
import { check } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 10 },
    { duration: '1m', target: 50 },
    { duration: '30s', target: 0 },
  ],
};

export default function () {
  const res = http.get('http://localhost:5000/api/documents?page=1&pageSize=10');
  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });
}
```

Execute:
```bash
k6 run load-test.js
```

## 🧪 Health Check

```bash
# Verificar se a API está respondendo
curl http://localhost:5000/api/documents

# Verificar RabbitMQ
curl http://localhost:15672/api/overview -u guest:guest
```
