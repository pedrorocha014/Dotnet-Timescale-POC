# TimescaleDB POC - Error Log System

Este projeto demonstra a integração do .NET 8 com TimescaleDB para gerenciar logs de erro com dados de temperatura e umidade.

## Estrutura do Projeto

- **Models/ErrorLog.cs**: Entidade principal com campos time, location, temperature e humidity
- **Data/ApplicationDbContext.cs**: Contexto do Entity Framework Core
- **Data/init.sql**: Script de inicialização do banco com TimescaleDB
- **Controllers/ErrorLogController.cs**: API REST para operações CRUD
- **docker-compose.yml**: Configuração do ambiente Docker

## Campos da Entidade ErrorLog

- `time`: TIMESTAMPTZ NOT NULL (chave primária)
- `location`: TEXT NOT NULL
- `temperature`: DOUBLE PRECISION NULL
- `humidity`: DOUBLE PRECISION NULL

## Pré-requisitos

- Docker Desktop
- .NET 8 SDK
- Visual Studio 2022 ou VS Code

## Como Executar

### 1. Subir o Banco de Dados

```bash
docker-compose up -d
```

Isso irá:
- Iniciar o TimescaleDB na porta 5432
- Inicializar o banco `error_log_db`
- Criar a tabela `error_log` como hypertable
- Inserir dados de exemplo
- Disponibilizar pgAdmin na porta 8080

### 2. Executar a Aplicação

```bash
cd TimescaleDb_POC
dotnet restore
dotnet run
```

A aplicação estará disponível em: https://localhost:7000
Swagger UI: https://localhost:7000/swagger

### 3. Acessar pgAdmin

- URL: http://localhost:8080
- Email: admin@admin.com
- Senha: admin123

**Configuração da conexão no pgAdmin:**
- Host: timescaledb
- Port: 5432
- Database: error_log_db
- Username: postgres
- Password: postgres123

## Endpoints da API

### GET /api/ErrorLog
Lista todos os logs de erro

### GET /api/ErrorLog/{time}
Busca um log específico por timestamp

### POST /api/ErrorLog
Cria um novo log de erro

### PUT /api/ErrorLog/{time}
Atualiza um log existente

### DELETE /api/ErrorLog/{time}
Remove um log específico

### GET /api/ErrorLog/search
Busca logs com filtros:
- `location`: Filtrar por localização
- `startTime`: Data/hora inicial
- `endTime`: Data/hora final

## Exemplo de Uso

### Criar um novo log

```json
POST /api/ErrorLog
{
  "location": "Sala de Servidores",
  "temperature": 25.5,
  "humidity": 40.2
}
```

### Buscar logs por localização

```
GET /api/ErrorLog/search?location=Sala
```

### Buscar logs em um período

```
GET /api/ErrorLog/search?startTime=2024-01-01T00:00:00Z&endTime=2024-01-02T00:00:00Z
```

## Recursos do TimescaleDB

- **Hypertables**: Tabelas otimizadas para dados de série temporal
- **Compression**: Compressão automática de dados antigos
- **Retention Policies**: Políticas de retenção de dados
- **Continuous Aggregates**: Agregações contínuas para consultas rápidas

## Parar o Ambiente

```bash
docker-compose down
```

Para remover também os volumes:
```bash
docker-compose down -v
```

## Troubleshooting

### Problemas de Conexão
- Verifique se o Docker está rodando
- Confirme se as portas 5432 e 8080 estão livres
- Aguarde o healthcheck do TimescaleDB completar

### Erros de Migração
- O Entity Framework Core criará automaticamente as tabelas
- Verifique se o script `init.sql` foi executado corretamente
- Consulte os logs do container: `docker logs timescaledb_poc`
