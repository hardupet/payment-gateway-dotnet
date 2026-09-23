# Payment Gateway API

A .NET payment gateway API that processes card payments through an acquiring bank simulator and allows previously processed payments to be retrieved.

The solution extends the supplied payment gateway challenge with a layered architecture, request validation, idempotent payment processing, resilient bank communication, centralized exception handling, health checks, and automated tests.

## Features

- Process card payments through an acquiring bank
- Retrieve previously processed payments by payment ID
- Idempotent payment processing using an `Idempotency-Key`
- Request validation using FluentValidation
- Card data protection by exposing only the last four digits
- Resilient acquiring-bank communication
- Centralized exception handling
- Liveness and readiness health checks
- In-memory thread-safe payment persistence
- Unit and API tests

---

## Architecture

The solution follows a layered architecture with separation between the API, application logic, domain model, and infrastructure concerns.

```text
src/
├── PaymentGateway.Api
│   ├── Controllers
│   ├── HealthChecks
│   └── Middleware
│
├── PaymentGateway.Application
│   ├── Abstractions
│   ├── DTOs
│   ├── Exceptions
│   ├── Helpers
│   ├── Services
│   └── Validators
│
├── PaymentGateway.Domain
│   ├── Entities
│   └── Enums
│
└── PaymentGateway.Infrastructure
    ├── Bank
    ├── Configuration
    ├── Persistence
    └── Resilience

test/
└── PaymentGateway.Api.Tests

PaymentGateway.UnitTests/
PaymentGateway.IntegrationTests/
```

### Dependency Direction

```text
API
 │
 ├──────────────► Application
 │                     │
 │                     ▼
 │                   Domain
 │
 └──────────────► Infrastructure
                       │
                       ├──► Application abstractions
                       └──► Domain
```

The Domain project contains the core payment model and does not depend on infrastructure or HTTP concerns.

The Application layer contains payment orchestration, validation, DTOs, abstractions and application-level business rules.

The Infrastructure layer implements external concerns such as acquiring-bank communication and payment persistence.

The API layer exposes the HTTP endpoints and handles transport-specific concerns such as controllers, middleware and health endpoints.

---

## API Endpoints

### Process a Payment

```http
POST /api/payments
```

The request should include an `Idempotency-Key` header.

Example:

```http
Idempotency-Key: 6f4a8b31-cc21-4a95-a42f-d8a1fae3152d
Content-Type: application/json
```

```json
{
  "cardNumber": "2222405343248877",
  "expiryMonth": 9,
  "expiryYear": 2027,
  "currency": "GBP",
  "amount": 6000,
  "cvv": "567"
}
```

### Retrieve a Payment

```http
GET /api/payments/{id}
```

Returns the stored payment when found or `404 Not Found` when the payment does not exist.

---

## Idempotency

Payment APIs must protect against accidental duplicate payment requests.

The API accepts an `Idempotency-Key` for payment creation requests.

For the first request:

1. The request is validated.
2. A deterministic hash of the payment request is generated.
3. The idempotency key is claimed atomically.
4. The payment is sent for processing.
5. The resulting payment state is stored.

When the same key is submitted again with the same payment details, the previously processed result is returned rather than creating another payment.

If the same idempotency key is reused with different payment details, the request is rejected as an idempotency conflict.

The repository uses atomic `ConcurrentDictionary` operations to protect against concurrent requests attempting to claim the same idempotency key.

In a production distributed environment, this would be backed by durable storage with a uniqueness constraint or distributed idempotency mechanism rather than process-local memory.

---

## Validation

Request validation is implemented using FluentValidation.

Validation includes:

- Card number is required and must contain 14 to 19 digits
- Expiry month must be between 1 and 12
- Card must not be expired
- Currency must use a three-letter format
- Supported currencies are GBP, USD and EUR
- Amount must be greater than zero
- CVV must contain 3 or 4 digits

Validation is performed before payment processing reaches the acquiring-bank integration.

---

## Security Considerations

Sensitive payment data is deliberately handled carefully.

- CVV is used for the acquiring-bank request but is not persisted
- Full card numbers are not returned when retrieving a payment
- Only the final four digits of the card number are exposed in stored payment responses
- Sensitive card details should never be written to application logs

For a production payment platform, additional controls would include PCI DSS compliant storage and processing, tokenization, secret management, authentication/authorization, audit logging and appropriate encryption.

---

## Acquiring Bank Integration

The gateway communicates with the supplied acquiring-bank simulator through an `HttpClient` abstraction.

The bank client is isolated behind `IAcquiringBankClient`, keeping external HTTP concerns outside the core payment service.

This also makes the application service independently testable by mocking the acquiring-bank client.

---

## Resilience

Acquiring-bank communication is configured with resilience policies for transient downstream failures.

The resilience strategy includes:

- Request timeouts
- Bounded retries for appropriate transient failures
- Exponential backoff
- Jitter
- Circuit breaking

Retries are intentionally bounded because payment operations require particular care. A network timeout does not necessarily mean that the downstream bank failed to process a payment.

In a production system, downstream retries would therefore require a bank-supported idempotency mechanism or unique payment reference to guarantee that retrying an ambiguous request cannot result in duplicate financial processing.

---

## Error Handling

The API uses centralized exception-handling middleware so controllers remain focused on HTTP orchestration.

Application and integration failures are translated into appropriate HTTP responses, including scenarios such as:

- Validation failures
- Payment not found
- Idempotency conflicts
- Payment already processing
- Acquiring-bank failures
- Unexpected server errors

This provides a consistent error-handling strategy across the API.

---

## Persistence

Payments are stored using an in-memory repository backed by `ConcurrentDictionary`.

The repository maintains:

- Payments indexed by payment ID
- Idempotency keys mapped to payment IDs

This is appropriate for the scope of the coding exercise and provides thread-safe access within a single application instance.

For production, this would be replaced with durable database storage and atomic/transactional enforcement of idempotency.

---

## Health Checks

The API exposes separate liveness and readiness endpoints.

### Liveness

```http
GET /health/live
```

Indicates that the Payment Gateway process itself is running.

Expected response:

```text
Healthy
```

### Readiness

```http
GET /health/ready
```

Checks whether the application is ready to process payments, including connectivity to the acquiring-bank dependency.

Possible responses include:

```text
Healthy
```

Separating liveness from readiness prevents a temporary downstream-bank failure from incorrectly implying that the Payment Gateway process itself has stopped.

---

## Testing

The solution contains automated tests covering the important payment behaviours.

### Unit Tests

Unit tests cover application behaviour and validation rules, including:

- Authorized payments
- Declined payments
- Validation failures
- Payment retrieval
- Idempotent requests
- Idempotency conflicts
- Acquiring-bank responses
- Payment service behaviour

### API Tests

API/controller tests verify the HTTP-facing behaviour of the gateway.

Run all tests with:

```bash
dotnet test
```

---

## Running the Solution

### Prerequisites

- .NET SDK
- Docker Desktop
- Git

### 1. Start the acquiring-bank simulator

From the repository root:

```bash
docker compose up -d
```

### 2. Run the API

```bash
dotnet run --project src/PaymentGateway.Api
```

Alternatively, open `PaymentGateway.sln` in Visual Studio, set `PaymentGateway.Api` as the startup project and run the application.

### 3. Open Swagger

When running in the Development environment, navigate to the Swagger URL displayed by the application.

Swagger can be used to submit and retrieve payments.

### 4. Verify health

```text
/health/live
/health/ready
```

---

## Design Decisions and Trade-offs

A few decisions were deliberately made for the scope of this exercise:

**Layered architecture rather than CQRS/MediatR**

The application has a small number of use cases, so introducing a mediator and command/query infrastructure would add complexity without providing enough benefit for the current scope.

**In-memory persistence**

The exercise does not require durable database storage. Repository abstractions allow the implementation to be replaced without changing the payment service.

**Idempotency included**

Duplicate processing is particularly important in payment systems, so idempotency was implemented even though persistence remains in memory.

**External bank isolated behind an interface**

This keeps HTTP integration separate from application logic and makes payment processing straightforward to unit test.

**Separate liveness and readiness checks**

The application can remain alive while an external dependency is temporarily unavailable.

---

## Production Improvements

Given additional production requirements, the next improvements would include:

- Durable database persistence
- Distributed idempotency enforcement
- Authentication and authorization
- Rate limiting
- Distributed tracing and metrics
- Structured observability and alerting
- Secure secret management
- PCI DSS compliant card-data handling
- Persistent audit trails
- CI/CD quality gates
- Containerized API deployment
- Additional integration and concurrency testing

---

## Technology

- C#
- ASP.NET Core Web API
- FluentValidation
- HttpClient
- .NET resilience policies
- xUnit
- Moq
- Docker / Docker Compose
- Swagger / OpenAPI
