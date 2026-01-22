using AnonimizerZetoArchive.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace AnonimizerZetoArchive
{
    public enum ExitCodes : short //Nie wiem czy sie przyda ale ok
    {
        Success = 0,
        GeneralError = 1,
        InvalidParameters = 2,
        FileNotFound = 3,
        ProcessingError = 4
    }

    public enum AppMode : short
    {
        Default = 0,
        Custom = 1,
    }


    public class Program
    {
        public static async Task Main(string[] args) //Parameters:
                                                     // /i {instance count}
                                                     // /cfg {config file path}
                                                     // /mode {app mode : Default ? Custom}
                                                     // /sql-source {connection satring}
                                                     // /sql-target {connection satring}
        {

            var parameters = GetParams(args);

            var builder = new AnonimizerZetoArchive.Config.Builder();


            AnonimizerZetoArchive.Config config;
            bool? UseDefault = ConfigurationManager.AppSettings["UseDefault"]?.Contains("True");

            if (!UseDefault.HasValue) throw new FormatException("Cannot read App.config!");
            //Write updated config from args if it isnt empty 
            if (!ConfigurationManager.AppSettings.HasKeys() || !UseDefault.Value)
            {

                builder.AddParameters(parameters); //Tu grzebie w globalnych statycznych także lepiej nie grzebać poza mainem

                AppMode appMOde = UseDefault.HasValue && UseDefault.Value ? AppMode.Default : AppMode.Custom; //Tutaj w zależności od UseDefault
                builder.AddParameter(nameof(AnonimizerZetoArchive.Config.ApplicationModeGlobal), appMOde.ToString());
            }

            config = builder.Build();

            var nameValueCollection = new System.Collections.Specialized.NameValueCollection();
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);


            configFile.AppSettings.Settings.Add(nameof(UseDefault), UseDefault.ToString());
            configFile.AppSettings.Settings.Add(nameof(config.ApplicationMode), config.ApplicationMode.ToString());
            configFile.AppSettings.Settings.Add(nameof(config.InstanceCounter), config.InstanceCounter.ToString());
            configFile.AppSettings.Settings.Add(nameof(config.ConfigFilePath), config.ConfigFilePath);
            configFile.AppSettings.Settings.Add(nameof(config.TargetInstance), config.TargetInstance);
            configFile.AppSettings.Settings.Add(nameof(config.SourceInstance), config.SourceInstance);
            configFile.AppSettings.Settings.Add(nameof(config.CasheQueueSize), config.CasheQueueSize.ToString());


            foreach (string key in nameValueCollection.Keys)
            {
                if (!config.AppSettingsContainsKey(key, nameValueCollection))
                    configFile.AppSettings.Settings.Add(key, nameValueCollection[key]);
            }

            configFile.Save(ConfigurationSaveMode.Full);
            ConfigurationManager.RefreshSection("configuration");
            //Local tests
            Console.WriteLine(config.ToString());
            Console.WriteLine(ConfigurationManager.AppSettings);


            //TODO:
            //Konfig wczytany, teraz już szykujemy się do kolejkowania tasków z DbTarget z DbSource...
            

            //To mi copilot podpowiedział ale nie działa jak powinno wiec inny pomysl - pliku nie updatujemy lecimy już potem na in memorry

            //// Otwórz plik konfiguracyjny do zapisu
            //System.Configuration.Configuration configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            //// Wyczyść istniejące ustawienia
            //configFile.AppSettings.Settings.Clear();

            //// Dodaj nowe ustawienia
            //configFile.AppSettings.Settings.Add("UseDefault","false");
            //configFile.AppSettings.Settings.Add(nameof(config.ApplicationMode), config.ApplicationMode.ToString());
            //configFile.AppSettings.Settings.Add(nameof(config.ApplicationMode), config.ApplicationMode.ToString());
            //configFile.AppSettings.Settings.Add(nameof(config.InstanceCounter), config.InstanceCounter.ToString());
            //configFile.AppSettings.Settings.Add(nameof(config.ConfigFilePath), config.ConfigFilePath);
            //configFile.AppSettings.Settings.Add(nameof(config.TargetInstance), config.TargetInstance);
            //configFile.AppSettings.Settings.Add(nameof(config.SourceInstance), config.SourceInstance);

            //// Zapisz zmiany do pliku
            //configFile.Save(ConfigurationSaveMode.Modified);

            //// Odśwież sekcję, aby załadować zapisane wartości
            //ConfigurationManager.RefreshSection("configuration");

        }

        public static Dictionary<string, string> GetParams(string[] args)
        {
            if(args is null || args.Length == 0) new Dictionary<string, string>();

            Dictionary<string, string> paramses = new Dictionary<string, string>();

            int counter = 1;
            string argLocal = string.Empty;
            string lastKey = string.Empty;

            foreach (string arg in args)
            {
                argLocal = arg.Trim().ToLower();

                if (counter++ % 2 == 1)
                {
                    if (!argLocal.StartsWith("/") || argLocal.Length == 0) throw new FormatException("Invalid parameter format");

                    lastKey = argLocal.Substring(1);
                    paramses[lastKey] = string.Empty;

                }
                else
                {
                    paramses[lastKey] = arg;
                }


            }

            return paramses;
        }

        public class AnonimizerZetoArchive
        {

            public class Config
            {

                public static short InstanceCounterGlobal = 4;
                public static short CasheQueueSizeGlobal = 1000;
                public static AppMode ApplicationModeGlobal = AppMode.Default;
                public static string ConfigFilePathGlobal = $"{AppContext.BaseDirectory}\\App.config";
                public static string SourceInstanceGlobal = "Server=localhost\\SQL2016;Database=SEDZIA_SR;User Id=root;Password=1234;TrustServerCertificate=True;"; //DB source
                public static string TargetInstanceGlobal = "Server=localhost\\SQL2016;Database=SEDZIA_SR_Anonimized;User Id=root;Password=1234;TrustServerCertificate=True;"; //DB target


                private HashSet<string> _appSettingKeySet;
                public bool AppSettingsContainsKey(string key, NameValueCollection pairs = default)
                {
                    if(_appSettingKeySet is null)
                    {
                        _appSettingKeySet = new HashSet<string>(pairs?.Count ?? ConfigurationManager.AppSettings.Count);
                        var collection = pairs ?? ConfigurationManager.AppSettings;
                        foreach (string elem in collection.Keys)
                        {
                            _appSettingKeySet.Add(key);
                        }
                    }

                    return _appSettingKeySet.Contains(key);
                }

                public short InstanceCounter { get; private set; }
                public AppMode ApplicationMode { get; private set; }
                public string ConfigFilePath { get; private set; }

                public string SourceInstance { get; private set; } //DB source
                public string TargetInstance { get; private set; } //DB target
                public short CasheQueueSize { get; private set; }


                private Config() : this(InstanceCounterGlobal, ApplicationModeGlobal, ConfigFilePathGlobal, SourceInstanceGlobal, TargetInstanceGlobal) 
                {

                    bool? conatains = ConfigurationManager.AppSettings.AllKeys?.Contains(nameof(CasheQueueSize));

                    if (conatains.HasValue && conatains.Value)
                    {
                        CasheQueueSize = Convert.ToInt16(ConfigurationManager.AppSettings[nameof(CasheQueueSize)]);
                    } else
                    {
                        CasheQueueSize = CasheQueueSizeGlobal;
                    }
                }

                private Config(short instanceCounter, AppMode applicationMode, string configFilePath, string connectionStringSource, string connectionStringTarget)
                {
                    InstanceCounter = instanceCounter;
                    ApplicationMode = applicationMode;
                    ConfigFilePath = configFilePath;
                    TargetInstance = connectionStringTarget;
                    SourceInstance = connectionStringSource;
                }

                private static string NameResolver(string paramName)
                {
                    switch (paramName)
                    {
                        case "i":
                            return nameof(InstanceCounterGlobal);
                        case "mode":
                            return nameof(ApplicationModeGlobal);
                        case "p":
                            return nameof(ConfigFilePathGlobal); //Full path to config file
                        case "sql-source":
                            return nameof(SourceInstanceGlobal);
                        case "sql-target":
                            return nameof(TargetInstanceGlobal);
                        default:
                            throw new ArgumentException("Unknown parameter name");
                    }
                }

                public override string ToString() =>
$@"{nameof(AnonimizerZetoArchive.Config)} info:
{nameof(ConfigFilePath)}: {ConfigFilePath}
{nameof(InstanceCounter)}: {InstanceCounter}
{nameof(ApplicationMode)}: {ApplicationMode}
{nameof(SourceInstance)}: {SourceInstance}
{nameof(TargetInstance)}: {TargetInstance}
{nameof(CasheQueueSize)}: {CasheQueueSize}
UseDefault: {ConfigurationManager.AppSettings["UseDefault"]}
";




                public class Builder
                {
                    public Builder() { }

                    public void AddParameter(string key, string value)
                    {
                        string resolvedName = NameResolver(key);

                        switch (resolvedName)
                        {
                            case nameof(InstanceCounterGlobal):

                                if (short.TryParse(value, out short instanceCount))
                                {
                                    InstanceCounterGlobal = instanceCount;
                                }
                                else throw new FormatException("Invalid format for InstanceCounter");
                                break;

                            case nameof(ApplicationModeGlobal):

                                if (!Enum.TryParse<AppMode>(value, true, out var appMode)) throw new ArgumentException($"Invalid AppMode value: {value}");

                                ApplicationModeGlobal = appMode;
                                break;

                            case nameof(ConfigFilePathGlobal):

                                if (!Path.IsPathFullyQualified(value)) throw new ArgumentException("Config file path must be absolute");
                                if (!File.Exists(value)) throw new FileNotFoundException("Config file not found", value);

                                ConfigFilePathGlobal = value;
                                break;

                            case nameof(SourceInstanceGlobal):

                                if (!ConnectionStringValidator.Validate(SourceInstanceGlobal))
                                  throw new FormatException("Wrong format of connection string");
                                
                                SourceInstanceGlobal = value;
                                break;

                            case nameof(TargetInstanceGlobal):

                                if (!ConnectionStringValidator.Validate(TargetInstanceGlobal))
                                    throw new FormatException("Wrong format of connection string");

                                TargetInstanceGlobal = value;
                                break;

                            default: throw new ArgumentException($"Unknown parameter name: {resolvedName}");
                         }
                    }

                    public void AddParameters(Dictionary<string, string> parameters)
                    {
                        if (parameters is null) throw new ArgumentNullException(nameof(parameters) + " cannot be null!");

                        foreach (var param in parameters)
                        {
                            AddParameter(param.Key, param.Value);
                        }
                    }

                    public Config Build()
                    {
                        return new Config();
                    }
                }

            }
        }
    }
}



