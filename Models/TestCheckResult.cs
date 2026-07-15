using System;
using System.Collections.Generic;
using System.Text;

namespace z2d.Models
{
    public sealed class TestCheckResult
    {
        public required TestCheckStatus Status { get; init; }
        public string? Details { get; init; }

        public static TestCheckResult NotApplicable() => new()
        {
            Status = TestCheckStatus.NotApplicable,
        };

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Details)
                ? Status.ToString()
                : $"{Status}: {Details}";
        }
    }
}
