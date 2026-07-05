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

### Why Reconciliation lives in the same solution

`MiniPayment.Reconciliation` is a standalone console app — it has no runtime dependency on the API and could have lived in its own repository. It is co-located here deliberately for the following reasons:

- **Shared domain vocabulary.** Both workloads operate on the same business concepts (order numbers, amounts, transaction statuses). Keeping them in one solution lets `MiniPayment.Reconciliation` reference `MiniPayment.Domain` for shared value objects and enums without publishing a NuGet package or duplicating definitions.
- **Unified toolchain.** A single `dotnet build`, `dotnet test`, and CI pipeline validates both deliverables together. A discrepancy in, say, a status enum would be caught at compile time rather than discovered at runtime when the batch job processes API-generated data.
- **Operational cohesion.** In practice, reconciliation runs against data that the payment API produced. Co-location makes it natural to version them together — a breaking change to the transaction schema is reflected in both projects in the same commit and PR.
- **Simpler onboarding.** A new developer clones one repository and immediately has everything needed to understand the full payment flow end-to-end, from the API that creates transactions to the batch job that reconciles them.

*Trade-off:* If the two workloads are eventually owned by different teams or need to be deployed and released independently at high frequency, splitting into separate repositories becomes the right call. The boundary is already clean — `MiniPayment.Reconciliation` only imports `MiniPayment.Domain`, so extraction would be a low-risk refactor.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

---

## Assignment 1 — Payment API

### Quickstart (Docker Compose)

```bash
# 1. Copy env file and adjust if needed
# bash / macOS / Linux
cp .env.example .env

# PowerShell (Windows)
Copy-Item .env.example .env

# 2. Start PostgreSQL + API (migrations run automatically on startup)
docker compose up -d

# 3. Verify health
curl http://localhost:8080/health
```

The API is now running at `http://localhost:8080`.  
Swagger UI: `http://localhost:8080/swagger`

### Manual (without Docker)

`appsettings.Development.json` defaults to `Host=localhost;Port=5432;Database=minipayment;Username=postgres;Password=postgres`. Make sure a local PostgreSQL instance is running and reachable at those coordinates, then:

```bash
# Run the API — migrations are applied automatically on startup in Development
dotnet run --project src/MiniPayment.Api
```

If you prefer to apply migrations manually before starting the API:

```bash
dotnet ef database update --project src/MiniPayment.Infrastructure --startup-project src/MiniPayment.Api
```

### Authentication

The API requires a JWT Bearer token. In Development, mint a token with:
```bash
# bash / macOS / Linux
TOKEN=$(curl -s -X POST http://localhost:8080/api/v1/dev/token | jq -r .access_token)

# PowerShell (Windows)
$TOKEN = (curl.exe -s -X POST http://localhost:8080/api/v1/dev/token | ConvertFrom-Json).access_token
```

Response of token API:
```json
{
  "access_token": "<token>",
  "expires_in": 86400,
  "token_type": "Bearer"
}
```

### Test the Payment Logic

**Approved (amount ends in .00):**
```bash
# bash / macOS / Linux
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

# PowerShell (Windows)
Invoke-RestMethod -Method Post http://localhost:8080/api/v1/pay `
  -Headers @{ Authorization = "Bearer $TOKEN" } `
  -ContentType "application/json" `
  -Body '{"order_number":"ORD-001","card_number":"4111111111111111","expiry_date":"12/29","cvv":"123","currency":"USD","cardholder_name":"John Doe","email":"john@example.com","amount":10.00}'
```

Expected: `"status": "APPROVED"`, `"response_code": "00"`

**Declined (amount does not end in .00):**
```bash
# bash / macOS / Linux
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

# PowerShell (Windows)
Invoke-RestMethod -Method Post http://localhost:8080/api/v1/pay `
  -Headers @{ Authorization = "Bearer $TOKEN" } `
  -ContentType "application/json" `
  -Body '{"order_number":"ORD-002","card_number":"4111111111111111","expiry_date":"12/29","cvv":"123","currency":"USD","cardholder_name":"John Doe","email":"john@example.com","amount":10.05}'
```

Expected: `"status": "DECLINED"`, `"response_code": "05"`

### Postman

Import `docs/postman/MiniPayment.postman_collection.json` into Postman.  
Set the `base_url` collection variable to `http://localhost:8080`, then run **Mint Dev Token** first.

---

## Assignment 2 — Reconciliation

```bash
# bash / macOS / Linux
dotnet run --project src/MiniPayment.Reconciliation -- \
  --list-a docs/sample-data/ListA-List1.csv \
  --list-b docs/sample-data/ListB-List2.csv \
  --output-dir ./output

# PowerShell (Windows)
dotnet run --project src/MiniPayment.Reconciliation -- `
  --list-a docs/sample-data/ListA-List1.csv `
  --list-b docs/sample-data/ListB-List2.csv `
  --output-dir ./output
```

Output files in `./output/`:
| File | Contents |
|---|---|
| `Matched_Records.csv` | Records present in both A and B (matched by OrderNumber = InvoiceNumber) |
| `Missing_In_B.csv` | Records in A but absent from B |
| `Missing_In_A.csv` | Records in B but absent from A |
| `Duplicates_A.csv` | List A rows whose OrderNumber repeats within the file (first occurrence is kept for matching) |
| `Duplicates_B.csv` | List B rows whose InvoiceNumber repeats within the file (first occurrence is kept for matching) |
| `Rejected_A.csv` | List A rows that failed validation |
| `Rejected_B.csv` | List B rows that failed validation |

### How it works

A three-phase hash-join. List B is the build side (loaded into memory), List A is the probe side (streamed).

- **Phase 1 — Load B.** Read `List B` end-to-end into `Dictionary<string, ListBRecord>` keyed by `InvoiceNumber`. Invalid rows go to `Rejected_B.csv`; repeated keys go to `Duplicates_B.csv` (first occurrence wins).
- **Phase 2 — Stream A and classify.** Read `List A` one row at a time. For each valid, non-duplicate row: if the key exists in the dictionary → write `Matched_Records.csv` and **remove** the key from the dictionary; otherwise → write `Missing_In_B.csv`. Rejected and duplicate A rows go to their own files.
- **Phase 3 — Flush leftovers.** Whatever keys remain in the dictionary were never matched by any A row, so by definition they belong in `Missing_In_A.csv`. Just walk the dictionary and write them.

The dictionary does double duty — lookup table in Phase 2, worklist in Phase 3 — so we never need a second read of B or a separate "matched keys" set.

`RunAsync` returns a typed `ReconciliationReport` (matched / missing / duplicate / rejected counts + output directory) so callers don't have to grep log lines to get the outcome.

### Beyond the basic spec

- **Duplicate detection on both sides** — first occurrence is authoritative for matching; subsequent occurrences are recorded in `Duplicates_A.csv` / `Duplicates_B.csv` for follow-up.
- **Row-level validation** with FluentValidation. Bad rows (missing key, unparseable date/amount, etc.) don't fail the run — they're routed to `Rejected_A.csv` / `Rejected_B.csv` with a reason.
- **Source columns preserved.** Optional properties (`Fees1`, `Fees2`, `NetTotal`, `CardNumber`) round-trip through the output files, so the CSVs are directly usable for downstream investigation instead of being reduced to just the join key.
- **Streaming reads.** List A is never fully in memory. Peak memory is `O(|B|)` — the reference dictionary — plus one row buffer for the A stream.
- **Structured logs.** Serilog emits `Phase 1 / 2 / 3` progress lines and a summary line with the same counts as the returned `ReconciliationReport`.
- **Case-insensitive key matching.** `Dictionary` and dedup `HashSet` both use `StringComparer.OrdinalIgnoreCase`, so `ORD-001` and `ord-001` match.

### Performance

Benchmarked on 1,000,000 × 1,000,000 rows: **~5.4s wall clock, ~600 MB peak working set**. Bottleneck is the `O(|B|)` dictionary. If B ever needs to exceed available memory, the fix is to store only the key in Phase 1 and re-stream B in Phase 3 to hydrate the leftover rows — the current pipeline shape supports that swap without touching Phase 2.

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

### Card Number Validation (Luhn Check)

Card numbers are validated using the **Luhn algorithm** — a checksum that catches single-digit typos and most transposition errors. It is a sanity check, not a security mechanism.

**How it works:**
1. Starting from the second-to-last digit, double every other digit moving left.
2. If doubling produces a number > 9, subtract 9.
3. Sum all digits. If the total is divisible by 10, the card number is valid.

**Example — `4111111111111111`:**

```
Digits:  4  1  1  1  1  1  1  1  1  1  1  1  1  1  1  1
         ×2    ×2    ×2    ×2    ×2    ×2    ×2    ×2
Result:  8  1  2  1  2  1  2  1  2  1  2  1  2  1  2  1

Sum = 8+1+2+1+2+1+2+1+2+1+2+1+2+1+2+1 = 30 → divisible by 10 ✓
```

The last digit is the **check digit** — it is chosen precisely to make the sum divisible by 10. Changing it to any other value (e.g. `4111111111111114` → sum = 33) fails the check immediately.

**What it catches:** single digit typed wrong, most adjacent digit swaps.  
**What it does not catch:** whether the card exists, has funds, or belongs to the requester.

### Idempotency
`order_number` acts as the idempotency key. A unique DB index ensures at-most-one insert per order number. If the same `order_number` is submitted again, the original transaction is returned unchanged.

*Trade-off*: This couples the business identifier with idempotency semantics. Clients must treat `order_number` as immutable per payment attempt.

### Concurrent Requests (Race Conditions)

When two requests arrive simultaneously with the **same `order_number`**, the following sequence occurs:

```
Request A  ──► GET order_number → (not found)  ─┐
Request B  ──► GET order_number → (not found)  ─┘  both read before either writes
Request A  ──► INSERT → succeeds ✓
Request B  ──► INSERT → unique constraint violation (Postgres error 23505)
                    └─► re-read winner's row → return same transaction_id ✓
```

**No application-level locking is needed.** PostgreSQL's unique index on `OrderNumber` is the authoritative guard — exactly one concurrent INSERT wins atomically. The handler catches only Postgres error `23505` (unique_violation) and re-reads the committed row. Any other `DbUpdateException` (connection failure, schema error, etc.) propagates normally and returns `500`.

Both requests receive an identical response: same `transaction_id`, `acquirer_reference`, and `status`. No double-charge is possible.

*This is the standard optimistic idempotency pattern. Pessimistic locking (e.g. `SELECT FOR UPDATE`) would add latency and deadlock risk without providing stronger guarantees.*

### PCI / PII Data Handling
- **Never stored**: full PAN, CVV, expiry date
- **Stored**: BIN (first 6 digits) + last 4 digits only
- **In-memory**: `CardNumber` wraps a `char[]` that is zeroed (`Array.Clear`) after masking; CVV is discarded before persistence
- **Logs**: `[NotLogged]` attributes via `Destructurama.Attributed` prevent `CardNumber` and `Cvv` from appearing in log output; a Serilog regex scrubber adds a secondary defence for any 13–19 digit string

### Reconciliation Memory Efficiency
Pass 1 loads List B references into a `Dictionary<string, ListBRecord>`. Pass 2 streams List A — matching removes keys from the dictionary. Remaining dictionary entries (missing in A) are written last. List A is never fully in memory; List B's reference dictionary is `O(|B|)`.
