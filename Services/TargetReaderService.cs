using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using z2d.Models;

namespace z2d.Services
{
    public sealed class TargetReaderService
    {
        private static readonly Regex TargetLineRegex = new(
            @"^\s*(?<name>[A-Za-z0-9_]+)\s*=\s*""(?<target>.+)""\s*$",
            RegexOptions.Compiled);

        public async Task<List<TargetItem>> ReadAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Target file path cannot be null or empty", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Target file was not found", filePath);

            var targets = new List<TargetItem>();
            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                    continue;

                var match = TargetLineRegex.Match(line);
                if (!match.Success)
                    continue;

                var name = match.Groups["name"].Value.Trim();
                var target = match.Groups["target"].Value.Trim();

                if (target.StartsWith("PING:", StringComparison.OrdinalIgnoreCase))
                {
                    targets.Add(new TargetItem
                    { 
                        Name = name,
                        Target = target["PING:".Length..].Trim(),
                        Type = TargetType.Ping
                    });

                    continue;
                }

                targets.Add(new TargetItem
                {
                    Name = name,
                    Target = target,
                    Type = TargetType.Url
                });
            }

            return targets;
        }
    }
}
