using System;
using System.Collections.Generic;

namespace WizFileUltra
{
    public static class Loc
    {
        public static string CurrentLanguage = "tr";

        private static readonly Dictionary<string, Dictionary<string, string>> Strings = new Dictionary<string, Dictionary<string, string>>
        {
            ["tr"] = new Dictionary<string, string>
            {
                ["Title"] = "WizFile Ultra - Detaylı Dosya & Klasör Arama (WPF .NET 4.8)",
                ["FileName"] = "📄 Dosya Adı:",
                ["FolderName"] = "📁 Klasör İsmi:",
                ["RescanAll"] = "⚡ Tüm Dosyaları Tekrar Tara",
                ["TargetFolders"] = "📍 Hedef Dizinler:",
                ["Browse"] = "Gözat...",
                ["AddFolder"] = "➕ Listeye Ekle",
                ["Drive"] = "Sürücü:",
                ["ScanSelected"] = "📂 Yalnızca Seçili Klasörleri İndeksle",
                ["Extensions"] = "Uzantılar:",
                ["Size"] = "Boyut:",
                ["Date"] = "Tarih:",
                ["All"] = "Tümü",
                ["FilesOnly"] = "Sadece Dosyalar",
                ["FoldersOnly"] = "Sadece Klasörler",
                ["ColName"] = "Adı",
                ["ColPath"] = "Bulunduğu Klasör / Yol",
                ["ColExt"] = "Uzantı",
                ["ColSize"] = "Boyut",
                ["ColDate"] = "Değiştirilme Tarihi",
                ["MenuOpen"] = "▶ Dosyayı / Klasörü Aç",
                ["MenuOpenFolder"] = "📂 Bulunduğu Klasörü Aç ve Seç",
                ["MenuCopyPath"] = "📋 Tam Dosya Yolunu Kopyala (Ctrl+C)",
                ["MenuCopyDir"] = "📋 Yalnızca Klasör Yolunu Kopyala",
                ["Ready"] = "Hazır",
                ["Scanning"] = "Taranıyor",
                ["Matched"] = "Eşleşen",
                ["Sorting"] = "Sıralama",
                ["Category"] = "Kategori:",
                ["CatAll"] = "Hepsi",
                ["CatCode"] = "Kod / Yazı",
                ["CatImages"] = "Resimler",
                ["CatVideos"] = "Videolar",
                ["CatDocs"] = "Belgeler",
                ["CatArchives"] = "Arşivler",
                ["CatAudio"] = "Müzikler",
                ["AutoRefresh"] = "🔄 Otomatik Yenile (sn):",
                ["LastMinutes"] = "⏱ Son (dk):"
            },
            ["en"] = new Dictionary<string, string>
            {
                ["Title"] = "WizFile Ultra - Advanced File & Folder Search (WPF .NET 4.8)",
                ["FileName"] = "📄 File Name:",
                ["FolderName"] = "📁 Folder Name:",
                ["RescanAll"] = "⚡ Rescan All Files",
                ["TargetFolders"] = "📍 Target Folders:",
                ["Browse"] = "Browse...",
                ["AddFolder"] = "➕ Add to List",
                ["Drive"] = "Drive:",
                ["ScanSelected"] = "📂 Index Selected Folders Only",
                ["Extensions"] = "Extensions:",
                ["Size"] = "Size:",
                ["Date"] = "Date:",
                ["All"] = "All",
                ["FilesOnly"] = "Files Only",
                ["FoldersOnly"] = "Folders Only",
                ["ColName"] = "Name",
                ["ColPath"] = "Folder / Location",
                ["ColExt"] = "Extension",
                ["ColSize"] = "Size",
                ["ColDate"] = "Date Modified",
                ["MenuOpen"] = "▶ Open File / Folder",
                ["MenuOpenFolder"] = "📂 Open in File Explorer & Select",
                ["MenuCopyPath"] = "📋 Copy Full Path (Ctrl+C)",
                ["MenuCopyDir"] = "📋 Copy Folder Path",
                ["Ready"] = "Ready",
                ["Scanning"] = "Scanning",
                ["Matched"] = "Matched",
                ["Sorting"] = "Sorting",
                ["Category"] = "Category:",
                ["CatAll"] = "All",
                ["CatCode"] = "Code / Text",
                ["CatImages"] = "Images",
                ["CatVideos"] = "Videos",
                ["CatDocs"] = "Documents",
                ["CatArchives"] = "Archives",
                ["CatAudio"] = "Music",
                ["AutoRefresh"] = "🔄 Auto-Refresh (s):",
                ["LastMinutes"] = "⏱ Last (min):"
            }
        };

        public static string Get(string key)
        {
            if (Strings.TryGetValue(CurrentLanguage, out var dict) && dict.TryGetValue(key, out var val))
            {
                return val;
            }
            return key;
        }
    }
}
