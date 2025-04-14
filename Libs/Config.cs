using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhotoBooth.Configuration
{
    public class Config(
        string printerName = "",
        int remainingPrints = -1,
        int copyCount = -1,
        string outputDir = "",
        string backgroundImagesDir = "",
        string printTemplatePath = "") : INotifyPropertyChanged, IDataErrorInfo
    {
        public string PrinterName { get; set; } = printerName;
        public int RemainingPrints { get; set; } = remainingPrints;
        public int CopyCount { get; set; } = copyCount;
        public string OutputDir { get; set; } = outputDir;
        public string BackgroundImagesDir { get; set; } = backgroundImagesDir;
        public string PrintTemplatePath { get; set; } = printTemplatePath;
        [JsonIgnore]
        public bool Valid => IsValid();
        [JsonIgnore]
        public string? Error => ConcatErrors();
        public event PropertyChangedEventHandler? PropertyChanged;



        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private bool IsValid() => ConcatErrors() == null;

        private string? ConcatErrors()
        {
            var output = new List<string?>
            {
                this[nameof(PrinterName)],
                this[nameof(RemainingPrints)],
                this[nameof(CopyCount)],
                this[nameof(OutputDir)],
                this[nameof(BackgroundImagesDir)],
                this[nameof(PrintTemplatePath)]
            };
            var nonNullErrors = output.Where(x => x != null);

            return nonNullErrors.Any() ? string.Join("|", nonNullErrors) : null;
        }

        public static Config FromJson(string json)
        {
            var value = JsonSerializer.Deserialize<Config>(json);
            if (value == null)
            {
                throw new Exception($"Error parsing config from: {json}");
            }

            return value;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, _jsonOptions);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public string? this[string columnName]
        {
            get
            {
                var validations = new Dictionary<string, Func<string?>>
                {
                    { nameof(PrinterName), () => string.IsNullOrWhiteSpace(PrinterName) ? "Required" : null },
                    {
                        nameof(RemainingPrints),
                        () => RemainingPrints <= 0 ? "There must be at least one print left in the printer" : null
                    },
                    {
                        nameof(CopyCount),
                        () => CopyCount > RemainingPrints
                            ? "There are more copies than prints available"
                            : (CopyCount <= 0 ? "There must be at least one copy" : (CopyCount > 5 ? "There cannot be more than 5 copies" : null))
                    },
                    {
                        nameof(OutputDir),
                        () => !Directory.Exists(OutputDir) ? $"The directory {OutputDir} does not exist" : null
                    },
                    {
                        nameof(BackgroundImagesDir),
                        () => !Directory.Exists(BackgroundImagesDir)
                            ? $"The directory {BackgroundImagesDir} does not exist"
                            : null
                    },
                    {
                        nameof(PrintTemplatePath),
                        () => !File.Exists(PrintTemplatePath) ? $"The file {PrintTemplatePath} does not exist" : null
                    }
                };

                return validations.ContainsKey(columnName) ? validations[columnName]() : null;
            }
        }
    }
}