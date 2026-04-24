using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace z2d.Services
{
    public static class WinwsArgsBuilder
    {
        public static async Task<string> BuildArgumentsAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file was not found.", filePath);

            var sb = new StringBuilder();

            using var reader = new StreamReader(filePath);

            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line == null)
                    break;

                line = line.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || !line.StartsWith("--"))
                    continue;

                sb.Append(line);
                sb.Append(' ');
            }

            return sb.ToString().Trim();
        }
    }
}
