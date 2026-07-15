using System;
using System.Collections.Generic;
using System.Text;

namespace z2d.Models
{
    public sealed class TargetTestResult
    {
        public required TargetItem Target { get; init; }

        public required TestCheckResult DnsResolve { get; init; }
        public required TestCheckResult Http { get; init; }
        public required TestCheckResult Tls12 { get; init; }
        public required TestCheckResult Tls13 { get; init; }
        public required TestCheckResult Ping { get; init; }

        public bool IsSuccessful =>
            Target.Type switch
            {
                TargetType.Url =>
                    DnsResolve.Status == TestCheckStatus.Success &&
                    Http.Status == TestCheckStatus.Success &&
                    (Tls12.Status == TestCheckStatus.Success ||
                     Tls13.Status == TestCheckStatus.Success),

                TargetType.Ping =>
                    Ping.Status == TestCheckStatus.Success,

                _ => false
            };

        public override string ToString()
        {
            var status = IsSuccessful ? "Success" : "Failed";
            return $"{Target}: {status}";
        }
    }
}
