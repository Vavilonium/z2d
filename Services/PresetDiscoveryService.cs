using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace z2d.Services
{
    public static class PresetDiscoveryService
    {
        public static async Task<Dictionary<string, FileInfo>> GetPresetsAsync(string presetsDir, string baseDir)
        {
            var fullDir = Path.IsPathRooted(presetsDir)
                ? presetsDir
                : Path.Combine(baseDir, presetsDir);

            fullDir = Path.GetFullPath(fullDir);

            if (!Directory.Exists(fullDir))
            {
                return new Dictionary<string, FileInfo>();
            }

            return await Task.Run(() =>
            {
                var result = new Dictionary<string, FileInfo>(StringComparer.OrdinalIgnoreCase);

                foreach (var file in Directory.EnumerateFiles(fullDir, "*.txt"))
                {
                    var fileInfo = new FileInfo(file);
                    var presetName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                    result[presetName] = fileInfo;
                }

                return result;
            });
        }
    }
}
