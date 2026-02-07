# Script para desenvolvimento local
# Execute: .\dev-setup.ps1

Write-Host "🚀 Configurando ambiente de desenvolvimento..." -ForegroundColor Green

# Verificar se Docker está rodando
Write-Host "`n📦 Verificando Docker..." -ForegroundColor Cyan
$dockerRunning = docker info 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Docker não está rodando. Por favor, inicie o Docker Desktop." -ForegroundColor Red
    exit 1
}
Write-Host "✅ Docker está rodando" -ForegroundColor Green

# Verificar se .NET 8 está instalado
Write-Host "`n.NET 8 SDK..." -ForegroundColor Cyan
$dotnetVersion = dotnet --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ .NET 8 SDK não encontrado. Instale em: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Red
    exit 1
}
Write-Host "✅ .NET SDK versão: $dotnetVersion" -ForegroundColor Green

# Restaurar dependências
Write-Host "`n📚 Restaurando dependências..." -ForegroundColor Cyan
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Erro ao restaurar dependências" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Dependências restauradas" -ForegroundColor Green

# Build do projeto
Write-Host "`n🔨 Compilando o projeto..." -ForegroundColor Cyan
dotnet build --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Erro na compilação" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Projeto compilado com sucesso" -ForegroundColor Green

# Executar testes
Write-Host "`n🧪 Executando testes..." -ForegroundColor Cyan
dotnet test --no-build --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️  Alguns testes falharam" -ForegroundColor Yellow
} else {
    Write-Host "✅ Todos os testes passaram" -ForegroundColor Green
}

# Iniciar infraestrutura com Docker
Write-Host "`n🐳 Iniciando SQL Server e RabbitMQ..." -ForegroundColor Cyan
docker-compose up -d sqlserver rabbitmq
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Erro ao iniciar containers" -ForegroundColor Red
    exit 1
}

Write-Host "`n⏳ Aguardando serviços iniciarem (30 segundos)..." -ForegroundColor Cyan
Start-Sleep -Seconds 30

# Verificar se os serviços estão rodando
Write-Host "`n🔍 Verificando serviços..." -ForegroundColor Cyan
$sqlserver = docker ps --filter "name=sqlserver" --format "{{.Names}}"
$rabbitmq = docker ps --filter "name=rabbitmq" --format "{{.Names}}"

if ($sqlserver) {
    Write-Host "✅ SQL Server está rodando" -ForegroundColor Green
} else {
    Write-Host "❌ SQL Server não está rodando" -ForegroundColor Red
}

if ($rabbitmq) {
    Write-Host "✅ RabbitMQ está rodando" -ForegroundColor Green
} else {
    Write-Host "❌ RabbitMQ não está rodando" -ForegroundColor Red
}

# Aplicar migrations
Write-Host "`n📊 Aplicando migrations do banco de dados..." -ForegroundColor Cyan
Set-Location src\FiscalDocAPI
dotnet ef database update
if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️  Erro ao aplicar migrations. Execute manualmente: cd src\FiscalDocAPI && dotnet ef database update" -ForegroundColor Yellow
} else {
    Write-Host "✅ Migrations aplicadas" -ForegroundColor Green
}
Set-Location ..\..

Write-Host "`n✨ Ambiente configurado com sucesso!" -ForegroundColor Green
Write-Host "`n📋 Próximos passos:" -ForegroundColor Cyan
Write-Host "  1. Execute a API: cd src\FiscalDocAPI && dotnet run" -ForegroundColor White
Write-Host "  2. Execute o Worker: cd src\FiscalDocAPI.Worker && dotnet run" -ForegroundColor White
Write-Host "  3. Acesse o Swagger: http://localhost:5000/swagger" -ForegroundColor White
Write-Host "  4. RabbitMQ Management: http://localhost:15672 (guest/guest)" -ForegroundColor White
Write-Host "`n🛑 Para parar os serviços: docker-compose down" -ForegroundColor Yellow
