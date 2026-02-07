#!/bin/bash

# Script para desenvolvimento local (Linux/Mac)
# Execute: chmod +x dev-setup.sh && ./dev-setup.sh

set -e

echo "🚀 Configurando ambiente de desenvolvimento..."

# Verificar se Docker está rodando
echo -e "\n📦 Verificando Docker..."
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker não está rodando. Por favor, inicie o Docker."
    exit 1
fi
echo "✅ Docker está rodando"

# Verificar se .NET 8 está instalado
echo -e "\n🔧 Verificando .NET 8 SDK..."
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET 8 SDK não encontrado. Instale em: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
fi
DOTNET_VERSION=$(dotnet --version)
echo "✅ .NET SDK versão: $DOTNET_VERSION"

# Restaurar dependências
echo -e "\n📚 Restaurando dependências..."
dotnet restore
echo "✅ Dependências restauradas"

# Build do projeto
echo -e "\n🔨 Compilando o projeto..."
dotnet build --no-restore
echo "✅ Projeto compilado com sucesso"

# Executar testes
echo -e "\n🧪 Executando testes..."
if dotnet test --no-build --verbosity minimal; then
    echo "✅ Todos os testes passaram"
else
    echo "⚠️  Alguns testes falharam"
fi

# Iniciar infraestrutura com Docker
echo -e "\n🐳 Iniciando SQL Server e RabbitMQ..."
docker-compose up -d sqlserver rabbitmq

echo -e "\n⏳ Aguardando serviços iniciarem (30 segundos)..."
sleep 30

# Verificar se os serviços estão rodando
echo -e "\n🔍 Verificando serviços..."
if docker ps | grep -q sqlserver; then
    echo "✅ SQL Server está rodando"
else
    echo "❌ SQL Server não está rodando"
fi

if docker ps | grep -q rabbitmq; then
    echo "✅ RabbitMQ está rodando"
else
    echo "❌ RabbitMQ não está rodando"
fi

# Aplicar migrations
echo -e "\n📊 Aplicando migrations do banco de dados..."
cd src/FiscalDocAPI
if dotnet ef database update; then
    echo "✅ Migrations aplicadas"
else
    echo "⚠️  Erro ao aplicar migrations. Execute manualmente: cd src/FiscalDocAPI && dotnet ef database update"
fi
cd ../..

echo -e "\n✨ Ambiente configurado com sucesso!"
echo -e "\n📋 Próximos passos:"
echo "  1. Execute a API: cd src/FiscalDocAPI && dotnet run"
echo "  2. Execute o Worker: cd src/FiscalDocAPI.Worker && dotnet run"
echo "  3. Acesse o Swagger: http://localhost:5000/swagger"
echo "  4. RabbitMQ Management: http://localhost:15672 (guest/guest)"
echo -e "\n🛑 Para parar os serviços: docker-compose down"
