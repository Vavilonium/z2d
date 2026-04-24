using System;
using System.Collections.Generic;
using System.Text;
using z2d.Models;
using System.Text.Json;
using System.IO;

namespace z2d.Services
{
    public sealed class UserSettingsService
    {
        private const string SettingsDirectoryName = "z2d";
        private const string SettingsFileName = "user-settings.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        private readonly string _settingsFilePath;

        public UserSettingsService()
            : this(GetDefaultSettingsFilePath())
        {
        }

        public UserSettingsService(string settingsFilePath)
        {
            if (string.IsNullOrWhiteSpace(settingsFilePath))
                throw new ArgumentException("Settings file path cannot be null or empty.", nameof(settingsFilePath));
            _settingsFilePath = settingsFilePath;
        }

        private static string GetDefaultSettingsFilePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, SettingsDirectoryName, SettingsFileName);
        }

        public async Task<UserSettings> LoadAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    return new UserSettings();
                }

                await using var stream = File.OpenRead(_settingsFilePath);

                if (stream.Length == 0)
                {
                    return new UserSettings();
                }

                var settings = await JsonSerializer.DeserializeAsync<UserSettings>(stream, JsonOptions, cancellationToken);

                return settings ?? new UserSettings();
            }
            catch (JsonException)
            {
                return new UserSettings();
            }
            catch (IOException)
            {
                return new UserSettings();
            }
            catch (UnauthorizedAccessException)
            {
                return new UserSettings();
            }
        }

        public async Task SaveAsync(UserSettings settings, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var settingsDirectory = Path.GetDirectoryName(_settingsFilePath);

            if (!string.IsNullOrWhiteSpace(settingsDirectory))
            {
                Directory.CreateDirectory(settingsDirectory);
            }

            await using var stream = File.Create(_settingsFilePath);

            await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
        }
    }
}
