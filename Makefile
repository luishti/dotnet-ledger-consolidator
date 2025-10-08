PROJECT_NAME=desafio-arquiteto

# Build all .NET projects
build:
	docker run --rm -v $(PWD):/src -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet build src --configuration Release

# Run unit tests
test:
	docker run --rm -v $(PWD):/src -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test src --configuration Release

# Start all services with Docker Compose
up:
	docker compose up -d

ps:
	docker ps -a

# Stop and remove containers
down:
	docker compose down

down-clean:
	docker compose down -v

clean:
	@echo "🧹 Limpando docker, bin/obj e cache NuGet..."
	docker system prune -f
	docker volume prune -f
	# opcional: remover imagens específicas, containers parados, etc
	# docker rm -f $(docker ps -aq)
	# docker rmi -f $(docker images -aq)
	find . -type d -name bin -exec rm -rf {} + || true
	find . -type d -name obj -exec rm -rf {} + || true
	@echo "Limpando cache do NuGet..."
	dotnet nuget locals all --clear
	@echo "Limpeza concluída."
	
# Run k6 load test (requires k6 installed locally)
loadtest:
	k6 run k6/load_test.js

.PHONY: build test up down loadtest