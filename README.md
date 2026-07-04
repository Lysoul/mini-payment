# Mini Payment — Assignment Solution

This solution covers **Assignment 1** (Mini Payment API) and **Assignment 2** (Reconciliation batch utility).

---

## Architecture

**Clean Architecture** across 5 projects:

| Project | Role |
|---|---|
| `MiniPayment.Domain` | Entities, value objects, enums — zero dependencies |
| `MiniPayment.Application` | MediatR CQRS commands/handlers, FluentValidation validators, pipeline behaviors |
| `MiniPayment.Infrastructure` | EF Core + PostgreSQL, Serilog, acquirer simulator |
| `MiniPayment.Api` | ASP.NET Core Web API — controllers, JWT auth, Swagger |
| `MiniPayment.Reconciliation` | Console app for CSV reconciliation |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

---

## Assignment 1 — Payment API

### Quickstart (Docker Compose)

```bash
# 1. Copy env file and adjust if needed
cp .env.example .env

# 2. Start PostgreSQL + API (migrations run automatically on startup)
docker compose up -d

# 3. Verify health
curl http://localhost:8080/health
```

The API is now running at `http://localhost:8080`.  
Swagger UI: `http://localhost:8080/swagger`

### Manual (without Docker)

```bash
# Start PostgreSQL separately (or use docker compose up postgres -d)

# Apply migrations
dotnet ef database update --project src/MiniPayment.Infrastructure --startup-project src/MiniPayment.Api

# Run the API
dotnet run --project src/MiniPayment.Api
```

### Authentication

The API requires a JWT Bearer token. In Development, mint a token with:

```bash
curl -s -X POST http://localhost:8080/api/v1/dev/token
```

Response:
```json
{
  "access_token": "<token>",
  "expires_in": 86400,
  "token_type": "Bearer"
}
```

Store the token:
```bash
TOKEN=$(curl -s -X POST http://localhost:8080/api/v1/dev/token | jq -r .access_token)
```

### Test the Payment Logic

**Approved (amount ends in .00):**
```bash
curl -s -X POST http://localhost:8080/api/v1/pay \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "order_number": "ORD-001",
    "card_number": "4111111111111111",
    "expiry_date": "12/29",
    "cvv": "123",
    "currency": "USD",
    "cardholder_name": "John Doe",
    "email": "john@example.com",
    "amount": 10.00
  }' | jq
```

Expected: `"status": "APPROVED"`, `"response_code": "00"`

**Declined (amount does not end in .00):**
```bash
curl -s -X POST http://localhost:8080/api/v1/pay \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "order_number": "ORD-002",
    "card_number": "4111111111111111",
    "expiry_date": "12/29",
    "cvv": "123",
    "currency": "USD",
    "cardholder_name": "John Doe",
    "email": "john@example.com",
    "amount": 10.05
  }' | jq
```

Expected: `"status": "DECLINED"`, `"response_code": "05"`

### Postman

Import `docs/MiniPayment.postman_collection.json` into Postman.  
Set the `base_url` collection variable to `http://localhost:8080`, then run **Mint Dev Token** first.

---

## Assignment 2 — Reconciliation

```bash
dotnet run --project src/MiniPayment.Reconciliation -- \
  --list-a docs/sample-data/listA.csv \
  --list-b docs/sample-data/listB.csv \
  --output-dir ./output
```

Output files in `./output/`:
| File | Contents |
|---|---|
| `Matched_Records.csv` | Records present in both A and B (matched by OrderNumber = InvoiceNumber) |
| `Missing_In_B.csv` | Records in A but absent from B |
| `Missing_In_A.csv` | Records in B but absent from A |
| `Rejected_A.csv` | List A rows that failed validation |
| `Rejected_B.csv` | List B rows that failed validation |

---

## Running Tests

```bash
# Application unit tests (no external dependencies)
dotnet test tests/MiniPayment.Application.UnitTests

# Reconciliation unit tests
dotnet test tests/MiniPayment.Reconciliation.UnitTests

# Integration tests (requires Docker — starts a real Postgres via Testcontainers)
dotnet test tests/MiniPayment.Api.IntegrationTests
```

---

## Key Design Decisions

### Business Logic
- `amount` ending in `.00` → `APPROVED`, `response_code: "00"`
- Any other decimal → `DECLINED`, `response_code` = last two decimal digits (e.g. `10.05` → `"05"`)

### Idempotency
`order_number` acts as the idempotency key. A unique DB index ensures at-most-one insert per order number. If the same `order_number` is submitted again, the original transaction is returned unchanged.

*Trade-off*: This couples the business identifier with idempotency semantics. Clients must treat `order_number` as immutable per payment attempt.

### PCI / PII Data Handling
- **Never stored**: full PAN, CVV, expiry date
- **Stored**: BIN (first 6 digits) + last 4 digits only
- **In-memory**: `CardNumber` wraps a `char[]` that is zeroed (`Array.Clear`) after masking; CVV is discarded before persistence
- **Logs**: `[NotLogged]` attributes via `Destructurama.Attributed` prevent `CardNumber` and `Cvv` from appearing in log output; a Serilog regex scrubber adds a secondary defence for any 13–19 digit string

### Reconciliation Memory Efficiency
Pass 1 loads List B references into a `Dictionary<string, ListBRecord>`. Pass 2 streams List A — matching removes keys from the dictionary. Remaining dictionary entries (missing in A) are written last. List A is never fully in memory; List B's reference dictionary is `O(|B|)`.
