using System;
using System.Collections.Generic;
using System.Text;

namespace z2d.Models
{
    public sealed class PresetTestRunResult
    {
        public required IReadOnlyList<PresetTestResult> PresetResults { get; init; }

        public PresetTestResult? BestPreset => PresetResults
            .Where(x => !x.LaunchFailed)
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        public override string ToString()
        {
            return $"Best preset: {BestPreset?.PresetName ?? "Not found"}";
        }

        public string ToReportString()
        {
            var separator = new string('=', 60);
            var sb = new StringBuilder();

            sb.AppendLine($"Preset test results - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine(separator);
            sb.AppendLine();

            sb.AppendLine($"{ToString()}");
            sb.AppendLine();

            foreach (var presetResult in PresetResults)
            {
                sb.AppendLine(separator);
                sb.AppendLine(presetResult.ToString());

                if (presetResult.LaunchFailed)
                {
                    sb.AppendLine();
                    continue;
                }

                foreach (var targetResult in presetResult.TargetResults)
                {
                    sb.AppendLine($"  Target: {targetResult.Target}");

                    if (targetResult.Target.Type == TargetType.Ping)
                    {
                        sb.AppendLine($"    Ping:   {targetResult.Ping}");
                        continue;
                    }

                    sb.AppendLine($"    DNS:    {targetResult.DnsResolve}");
                    sb.AppendLine($"    HTTP:   {targetResult.Http}");
                    sb.AppendLine($"    TLS1.2: {targetResult.Tls12}");
                    sb.AppendLine($"    TLS1.3: {targetResult.Tls13}");
                    sb.AppendLine($"    Ping:   {targetResult.Ping}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
