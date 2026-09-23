using System;
using System.Collections.Generic;
using System.Text;


namespace PaymentGateway.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; init; }

        public string IdempotencyKey { get; init; } = string.Empty;

        public string RequestHash { get; init; } = string.Empty;

        public PaymentStatus Status { get; set; }

        public string CardNumberLastFour { get; init; } = string.Empty;

        public int ExpiryMonth { get; init; }

        public int ExpiryYear { get; init; }

        public string Currency { get; init; } = string.Empty;

        public int Amount { get; init; }

        public string? AuthorizationCode { get; set; }

        public DateTime CreatedAtUtc { get; init; }

        public DateTime? CompletedAtUtc { get; set; }
    }
}
