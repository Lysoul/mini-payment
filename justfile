set windows-shell := ["powershell.exe", "-NoProfile", "-Command"]

# List available commands
default:
    @just --list

# ── Build ─────────────────────────────────────────────────────────────────────

# Build the entire solution
build:
    dotnet build

# Build in Release mode
build-release:
    dotnet build -c Release

# ── Tests ─────────────────────────────────────────────────────────────────────

# Run all tests
test:
    dotnet test --logger "console;verbosity=normal"

# Run unit tests only (no Docker required)
test-unit:
    dotnet test tests/MiniPayment.Application.UnitTests --logger "console;verbosity=normal"
    dotnet test tests/MiniPayment.Reconciliation.UnitTests --logger "console;verbosity=normal"

# Run integration tests (requires Docker)
test-integration:
    dotnet test tests/MiniPayment.Api.IntegrationTests --logger "console;verbosity=normal"

# Run tests with coverage report
test-coverage:
    dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# ── Docker ────────────────────────────────────────────────────────────────────

# Start the stack (postgres + api)
up:
    docker compose up -d

# Start and rebuild images
up-build:
    docker compose up -d --build

# Stop the stack
down:
    docker compose down

# Stop and wipe the database volume
down-clean:
    docker compose down -v

# Rebuild and restart fresh (wipes DB)
restart:
    docker compose down -v
    docker compose up -d --build

# Show logs (follow)
logs:
    docker compose logs -f api

# Show postgres logs
logs-db:
    docker compose logs -f postgres

# ── Database ──────────────────────────────────────────────────────────────────

# Add a new EF Core migration (usage: just migration MyMigrationName)
migration name:
    dotnet ef migrations add {{name}} --project src/MiniPayment.Infrastructure --startup-project src/MiniPayment.Api

# Apply migrations manually (auto-applied on startup in Development)
migrate:
    dotnet ef database update --project src/MiniPayment.Infrastructure --startup-project src/MiniPayment.Api

# Drop and recreate migrations from scratch
migration-reset:
    Remove-Item -Recurse -Force src/MiniPayment.Infrastructure/Migrations -ErrorAction SilentlyContinue
    dotnet ef migrations add InitialCreate --project src/MiniPayment.Infrastructure --startup-project src/MiniPayment.Api

# Open a psql shell in the running postgres container
db:
    docker compose exec postgres psql -U postgres -d minipayment

# ── API ───────────────────────────────────────────────────────────────────────

# Mint a dev JWT token and print it
token:
    $t = (Invoke-RestMethod -Method Post -Uri "http://localhost:8080/api/v1/dev/token").access_token; Write-Host $t

# Test approved payment (amount ending in .00)
pay-approved:
    $t = (Invoke-RestMethod -Method Post -Uri "http://localhost:8080/api/v1/dev/token").access_token; \
    Invoke-RestMethod -Method Post -Uri "http://localhost:8080/api/v1/pay" \
      -Headers @{ Authorization = "Bearer $t"; "Content-Type" = "application/json" } \
      -Body '{"order_number":"ORD-001","card_number":"4111111111111111","expiry_date":"12/29","cvv":"123","currency":"USD","cardholder_name":"John Doe","email":"john@example.com","amount":10.00}' | ConvertTo-Json

# Test declined payment (amount not ending in .00)
pay-declined:
    $t = (Invoke-RestMethod -Method Post -Uri "http://localhost:8080/api/v1/dev/token").access_token; \
    Invoke-RestMethod -Method Post -Uri "http://localhost:8080/api/v1/pay" \
      -Headers @{ Authorization = "Bearer $t"; "Content-Type" = "application/json" } \
      -Body '{"order_number":"ORD-002","card_number":"4111111111111111","expiry_date":"12/29","cvv":"123","currency":"USD","cardholder_name":"John Doe","email":"john@example.com","amount":10.05}' | ConvertTo-Json

# Test validation error (invalid card number)
pay-invalid:
    $t = (Invoke-RestMethod -Method Post -Uri "http://localhost:8080/api/v1/dev/token").access_token; \
    Invoke-RestMethod -Method Post -Uri "http://localhost:8080/api/v1/pay" \
      -Headers @{ Authorization = "Bearer $t"; "Content-Type" = "application/json" } \
      -Body '{"order_number":"ORD-003","card_number":"1234","expiry_date":"12/29","cvv":"123","currency":"USD","cardholder_name":"John Doe","email":"john@example.com","amount":10.00}' | ConvertTo-Json

# Check logs for any leaked PAN (should return nothing)
check-pan-leak:
    docker compose logs api | Select-String -Pattern "\b\d{16}\b"

# ── Reconciliation ────────────────────────────────────────────────────────────

# Run reconciliation (usage: just reconcile listA.csv listB.csv)
reconcile list-a list-b:
    dotnet run --project src/MiniPayment.Reconciliation -- --list-a {{list-a}} --list-b {{list-b}} --output-dir ./out

# Run reconciliation with sample data
reconcile-sample:
    dotnet run --project src/MiniPayment.Reconciliation -- --list-a docs/sample-data/listA.csv --list-b docs/sample-data/listB.csv --output-dir ./out
