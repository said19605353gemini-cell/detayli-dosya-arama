using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WizFileUltra
{
    public class SearchFilterOptions
    {
        public string Query { get; set; } = ""; // Doğrudan arama çubuğu metni
        public List<string> TargetFileNames { get; set; } = new List<string>(); // Birden fazla eklenen dosya isimleri / etiketleri
        public string FolderNameQuery { get; set; } = ""; // Bulunduğu klasörün adı/yolunda arama
        public List<string> TargetFolders { get; set; } = new List<string>(); // Sabit seçili klasör filtreleri
        public string CustomExtensions { get; set; } = ""; // .cpp .h vb.
        public long MinSize { get; set; } = -1;
        public long MaxSize { get; set; } = -1;
        public DateTime? MinDate { get; set; }
        public DateTime? MaxDate { get; set; }
        public int LastMinutes { get; set; } = -1; // Son X dakikada değiştirilenler (örn: 10)
        public bool FilesOnly { get; set; }
        public bool FoldersOnly { get; set; }
        public bool MatchExactFolder { get; set; }
    }

    public enum SortColumn
    {
        Name,
        Path,
        Extension,
        Size,
        Date
    }

    public class SearchEngine
    {
        private List<FileRecord> _allFiles = new List<FileRecord>();
        private readonly object _lock = new object();

        public int TotalFilesCount
        {
            get
            {
                lock (_lock) return _allFiles.Count;
            }
        }

        public async Task IndexDrivesAsync(List<DriveInfo> drivesToScan, IProgress<string> statusProgress, IProgress<int> countProgress, CancellationToken ct)
        {
            var combined = new List<FileRecord>();

            foreach (var drive in drivesToScan)
            {
                if (ct.IsCancellationRequested) break;

                statusProgress?.Report($"Taranıyor: {drive.Name} (MFT / USN Journal)...");
                var records = await Task.Run(() =>
                {
                    return UsnJournalReader.ScanDriveMFT(drive, count =>
                    {
                        countProgress?.Report(combined.Count + count);
                    });
                }, ct);

                combined.AddRange(records);
                countProgress?.Report(combined.Count);
            }

            lock (_lock)
            {
                _allFiles = combined;
            }

            statusProgress?.Report($"Hazır. Toplam {combined.Count:N0} dosya ve klasör indekslendi.");
        }

        public async Task IndexDirectoriesAsync(List<string> folderPaths, IProgress<string> statusProgress, IProgress<int> countProgress, CancellationToken ct)
        {
            var combined = new List<FileRecord>();

            foreach (var folder in folderPaths)
            {
                if (ct.IsCancellationRequested) break;
                if (!Directory.Exists(folder)) continue;

                statusProgress?.Report($"Klasör taranıyor: {folder}...");
                var records = await Task.Run(() =>
                {
                    return ScanFolderRecursively(folder, countProgress, ct);
                }, ct);

                combined.AddRange(records);
                countProgress?.Report(combined.Count);
            }

            lock (_lock)
            {
                _allFiles = combined;
            }

            statusProgress?.Report($"Hazır. Klasörlerden {combined.Count:N0} dosya indekslendi.");
        }

        private List<FileRecord> ScanFolderRecursively(string rootPath, IProgress<int> countProgress, CancellationToken ct)
        {
            var list = new List<FileRecord>(20000);
            var queue = new Queue<string>();
            queue.Enqueue(rootPath);
            int count = 0;

            while (queue.Count > 0 && !ct.IsCancellationRequested)
            {
                string dir = queue.Dequeue();
                try
                {
                    var di = new DirectoryInfo(dir);
                    foreach (var d in di.EnumerateDirectories())
                    {
                        if (ct.IsCancellationRequested) break;
                        var rec = new FileRecord
                        {
                            Name = d.Name,
                            FullPath = d.FullName,
                            IsDirectory = true,
                            ModifiedDate = d.LastWriteTime,
                            Size = 0
                        };
                        list.Add(rec);
                        queue.Enqueue(d.FullName);
                        count++;
                        if (count % 2000 == 0) countProgress?.Report(count);
                    }

                    foreach (var f in di.EnumerateFiles())
                    {
                        if (ct.IsCancellationRequested) break;
                        var rec = new FileRecord
                        {
                            Name = f.Name,
                            FullPath = f.FullName,
                            IsDirectory = false,
                            ModifiedDate = f.LastWriteTime,
                            Size = f.Length
                        };
                        list.Add(rec);
                        count++;
                        if (count % 2000 == 0) countProgress?.Report(count);
                    }
                }
                catch { }
            }

            return list;
        }

        public List<FileRecord> Search(SearchFilterOptions options, SortColumn sortCol, bool sortAscending, int maxResults = 10000)
        {
            // 1. Dosya adı sorgusu tokenları
            var rawTokens = string.IsNullOrWhiteSpace(options.Query)
                ? new string[0]
                : options.Query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var andTokens = new List<string>();
            var orTokens = new List<string[]>();
            var queryExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var tok in rawTokens)
            {
                if (tok.StartsWith(".") && tok.Length > 1 && !tok.Contains("\\") && !tok.Contains("/"))
                {
                    queryExtensions.Add(tok.ToLowerInvariant());
                }
                else if (tok.Contains("|"))
                {
                    var parts = tok.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0) orTokens.Add(parts);
                }
                else
                {
                    andTokens.Add(tok);
                }
            }

            // 2. Klasör İsmi Sorgusu Tokenları (örn: "Downloads" veya "MyProject\src")
            string folderQuery = options.FolderNameQuery?.Trim();
            string[] folderTokens = !string.IsNullOrWhiteSpace(folderQuery)
                ? folderQuery.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                : null;

            // 3. Uzantılar
            var extSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(options.CustomExtensions))
            {
                var parts = options.CustomExtensions.Split(new[] { ' ', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    string clean = p.Trim();
                    if (!clean.StartsWith(".")) clean = "." + clean;
                    extSet.Add(clean.ToLowerInvariant());
                }
            }

            if (queryExtensions.Count > 0)
            {
                extSet.UnionWith(queryExtensions);
            }

            // 4. Sabit Seçili Klasörler
            var activeFolders = options.TargetFolders?.Where(f => !string.IsNullOrWhiteSpace(f)).ToList() ?? new List<string>();

            var results = new List<FileRecord>(Math.Min(maxResults, 10000));

            lock (_lock)
            {
                for (int i = 0; i < _allFiles.Count; i++)
                {
                    var file = _allFiles[i];
                    string name = file.Name;
                    if (string.IsNullOrEmpty(name)) continue;

                    // Dosya / Klasör tipi filtresi
                    if (options.FilesOnly && file.IsDirectory) continue;
                    if (options.FoldersOnly && !file.IsDirectory) continue;

                    string dirPath = file.DirectoryPath;

                    // KLASÖR İSMİ DETAYLI ARAMA
                    if (folderTokens != null && folderTokens.Length > 0)
                    {
                        if (string.IsNullOrEmpty(dirPath)) continue;

                        bool folderMatch = true;
                        for (int ft = 0; ft < folderTokens.Length; ft++)
                        {
                            if (dirPath.IndexOf(folderTokens[ft], StringComparison.OrdinalIgnoreCase) < 0)
                            {
                                folderMatch = false;
                                break;
                            }
                        }
                        if (!folderMatch) continue;
                    }

                    // Sabit Klasör Listesi Filtresi
                    if (activeFolders.Count > 0)
                    {
                        string path = file.FullPath;
                        if (string.IsNullOrEmpty(path)) continue;

                        bool folderMatch = false;
                        for (int k = 0; k < activeFolders.Count; k++)
                        {
                            if (path.IndexOf(activeFolders[k], StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                folderMatch = true;
                                break;
                            }
                        }
                        if (!folderMatch) continue;
                    }

                    // Uzantı filtresi
                    if (extSet.Count > 0)
                    {
                        if (file.IsDirectory) continue;
                        if (!extSet.Contains(file.Extension)) continue;
                    }

                    // Boyut filtresi
                    if (!file.IsDirectory)
                    {
                        if (options.MinSize >= 0 && file.Size < options.MinSize) continue;
                        if (options.MaxSize >= 0 && file.Size > options.MaxSize) continue;
                    }

                    // Tarih filtresi
                    if (options.LastMinutes > 0)
                    {
                        if (file.ModifiedDate < DateTime.Now.AddMinutes(-options.LastMinutes)) continue;
                    }
                    else
                    {
                        if (options.MinDate.HasValue && file.ModifiedDate < options.MinDate.Value) continue;
                        if (options.MaxDate.HasValue && file.ModifiedDate > options.MaxDate.Value) continue;
                    }

                    // Çoklu Dosya Adı Etiketleri (Herhangi bir eklenen dosya adını içermesi yeterli)
                    if (options.TargetFileNames != null && options.TargetFileNames.Count > 0)
                    {
                        bool anyNameMatch = false;
                        for (int fn = 0; fn < options.TargetFileNames.Count; fn++)
                        {
                            if (name.IndexOf(options.TargetFileNames[fn], StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                anyNameMatch = true;
                                break;
                            }
                        }
                        if (!anyNameMatch) continue;
                    }

                    // Dosya adı AND filtresi
                    if (andTokens.Count > 0)
                    {
                        bool match = true;
                        for (int t = 0; t < andTokens.Count; t++)
                        {
                            if (name.IndexOf(andTokens[t], StringComparison.OrdinalIgnoreCase) < 0)
                            {
                                match = false;
                                break;
                            }
                        }
                        if (!match) continue;
                    }

                    // Dosya adı OR filtresi (|)
                    if (orTokens.Count > 0)
                    {
                        bool anyGroupFailed = false;
                        foreach (var group in orTokens)
                        {
                            bool groupMatched = false;
                            for (int g = 0; g < group.Length; g++)
                            {
                                if (name.IndexOf(group[g], StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    groupMatched = true;
                                    break;
                                }
                            }
                            if (!groupMatched)
                            {
                                anyGroupFailed = true;
                                break;
                            }
                        }
                        if (anyGroupFailed) continue;
                    }

                    results.Add(file);
                    if (results.Count >= maxResults)
                        break;
                }
            }

            // Sıralama
            Comparison<FileRecord> comp;
            switch (sortCol)
            {
                case SortColumn.Path:
                    comp = (a, b) => string.Compare(a.FullPath, b.FullPath, StringComparison.OrdinalIgnoreCase);
                    break;
                case SortColumn.Extension:
                    comp = (a, b) => string.Compare(a.Extension, b.Extension, StringComparison.OrdinalIgnoreCase);
                    break;
                case SortColumn.Size:
                    comp = (a, b) => a.Size.CompareTo(b.Size);
                    break;
                case SortColumn.Date:
                    comp = (a, b) => a.ModifiedDate.CompareTo(b.ModifiedDate);
                    break;
                case SortColumn.Name:
                default:
                    comp = (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                    break;
            }

            if (sortAscending)
                results.Sort(comp);
            else
                results.Sort((a, b) => comp(b, a));

            return results;
        }
    }
}
