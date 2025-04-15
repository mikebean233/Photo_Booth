using System;
using System.IO;
using PhotoBooth.Configuration;

namespace MainApplication.Configuration
{
    static class ConfigUtil
    {
        private static readonly string _appDataPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Peterson Labs\\PhotoBooth";
        private static readonly string _appSettingsFilePath = $"{_appDataPath}\\appSettings.json";

        public static Config GetConfig()
        {
            TryInit();

            // 2) Load config
            var currentConfig = Config.FromJson(File.ReadAllText(_appSettingsFilePath));

            // 3) If config is invalid prompt user for new config via dialog then save result
            if (!currentConfig.Valid)
            {
                var dialog = new Setup(currentConfig);
                
                dialog.ShowDialog();
                
                if ( dialog.SubmitClicked )
                    SaveConfig(dialog.Input );

                if ( dialog.Input.Valid )
                    return dialog.Input;
                
                // Invalid config
                Console.Error.WriteLine($"Exiting because of invalid config: {dialog.Input.ToJson()}");
                Environment.Exit(1);
            }

            // 4) return
            return currentConfig;
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
