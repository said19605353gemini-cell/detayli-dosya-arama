using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace WizFileUltra
{
    [DataContract]
    public class AppSettings
    {
        [DataMember] public string SearchText { get; set; } = "";
        [DataMember] public string ContentSearchText { get; set; } = "";
        [DataMember] public string FolderNameSearch { get; set; } = "";
        [DataMember] public string CustomExtensions { get; set; } = ".txt .cpp .h .c .cs .py .js .ts .html .css .json .xml .md .log";
        [DataMember] public int CategoryIndex { get; set; } = 1;
        [DataMember] public int SizeIndex { get; set; } = 0;
        [DataMember] public int DateIndex { get; set; } = 0;
        [DataMember] public int TypeFilter { get; set; } = 0;
        [DataMember] public bool LastMinutesChecked { get; set; } = false;
        [DataMember] public string LastMinutesText { get; set; } = "10";
        [DataMember] public bool AutoRefreshChecked { get; set; } = false;
        [DataMember] public string AutoRefreshIntervalText { get; set; } = "5";
        [DataMember] public string Language { get; set; } = "tr";
        [DataMember] public List<string> SavedFolders { get; set; } = new List<string>();
        [DataMember] public List<string> SavedFileNames { get; set; } = new List<string>();

        private static readonly string SettingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WizFileUltra",
            "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    using (var fs = File.OpenRead(SettingsFile))
                    {
                        var ser = new DataContractJsonSerializer(typeof(AppSettings));
                        var settings = (AppSettings)ser.ReadObject(fs);
                        if (settings != null) return settings;
                    }
                }
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(SettingsFile);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                using (var fs = File.Create(SettingsFile))
                {
                    var ser = new DataContractJsonSerializer(typeof(AppSettings));
                    ser.WriteObject(fs, this);
                }
            }
            catch { }
        }
    }
}
