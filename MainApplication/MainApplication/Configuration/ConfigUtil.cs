using System;
using System.IO;
using Libs;

namespace MainApplication.Configuration
{
    static class ConfigUtil
    {
        private static readonly string _appDataPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Peterson Labs\\PhotoBooth";
        private static readonly string _appSettingsFilePath = $"{_appDataPath}\\appSettings.json";

        public static Config GetConfig()
        {
            TryInit();

            var currentConfig = LoadCurrentConfig();

            if (!currentConfig.Valid)
            {
                return ConfigFromDialog(currentConfig);
            }
            return currentConfig;
        }
        public static Config ConfigFromDialog(Config currentConfig) {
                var dialog = new Setup(currentConfig);

                dialog.ShowDialog();

                if (dialog.SubmitClicked)
                    SaveConfig(dialog.Input);

                if (dialog.Input.Valid)
                    return dialog.Input;

                Console.Error.WriteLine($"Exiting because of invalid config: {dialog.Input.ToJson()}");
                Environment.Exit(1);
        
            return currentConfig;
        }

        private static Config LoadCurrentConfig() {
            return Config.FromJson(File.ReadAllText(_appSettingsFilePath));
        }

        private static void TryInit()
        {
            // 1) Check if app data directory exists, if not create it
            if (!Directory.Exists(_appDataPath))
                Directory.CreateDirectory(_appDataPath);

            if (File.Exists(_appSettingsFilePath)) return;
            using var initializeFile = File.CreateText(_appSettingsFilePath);
            initializeFile.Write("{}");
        }

        public static void SaveConfig(Config config)
        {
            TryInit();
            File.WriteAllText(_appSettingsFilePath, config.ToJson());
        }
    }
}
