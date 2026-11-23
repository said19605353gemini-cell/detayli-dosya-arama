using System;

namespace WizFileUltra
{
    public class FileRecord
    {
        public ulong FileReferenceNumber { get; set; }
        public ulong ParentFileReferenceNumber { get; set; }
        public string Name { get; set; }
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string FullPath { get; set; }

        private string _extension;
        public string Extension
        {
            get
            {
                if (_extension == null)
                {
                    if (IsDirectory)
                    {
                        _extension = "<DIR>";
                    }
                    else
                    {
                        int idx = Name != null ? Name.LastIndexOf('.') : -1;
                        _extension = (idx >= 0 && idx < Name.Length - 1) ? Name.Substring(idx).ToLowerInvariant() : "";
                    }
                }
                return _extension;
            }
        }

        public string DirectoryPath
        {
            get
            {
                if (string.IsNullOrEmpty(FullPath)) return "";
                if (IsDirectory) return FullPath;
                int idx = FullPath.LastIndexOf('\\');
                return idx > 0 ? FullPath.Substring(0, idx) : FullPath;
            }
        }

        public string FormattedSize
        {
            get
            {
                if (IsDirectory) return "<DIR>";
                if (Size <= 0) return "0 KB";
                if (Size < 1024) return Size + " B";
                if (Size < 1024 * 1024) return (Size / 1024.0).ToString("N1") + " KB";
                if (Size < 1024 * 1024 * 1024) return (Size / (1024.0 * 1024.0)).ToString("N1") + " MB";
                return (Size / (1024.0 * 1024.0 * 1024.0)).ToString("N2") + " GB";
            }
        }

        public string MatchedSnippet { get; set; } = ""; // Dosya içinde bulunan satır / kelime önizlemesi
        public int MatchLineNumber { get; set; } = 0; // İlk bulunan satır numarası
        public System.Collections.Generic.List<LineMatch> MatchedLines { get; set; } = new System.Collections.Generic.List<LineMatch>(); // Bulunan tüm eşleşmeler

        public string FormattedDate => ModifiedDate > DateTime.MinValue ? ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss") : "";
    }

    public class LineMatch
    {
        public int LineNumber { get; set; }
        public string LineText { get; set; }
        public string MatchedWord { get; set; }
        public bool IsHighlighted { get; set; } = true;
    }
}
