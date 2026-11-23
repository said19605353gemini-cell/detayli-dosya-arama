using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace WizFileUltra
{
    public partial class MainWindow : Window
    {
        private SearchEngine _engine = new SearchEngine();
        private List<FileRecord> _currentResults = new List<FileRecord>();
        private ObservableCollection<string> _filterFolders = new ObservableCollection<string>();
        private ObservableCollection<string> _filterFileNames = new ObservableCollection<string>();
        private ObservableCollection<string> _filterWords = new ObservableCollection<string>();
        private CancellationTokenSource _cts;

        private DispatcherTimer _debounceTimer;
        private DispatcherTimer _autoRefreshTimer;
        private SortColumn _currentSortCol = SortColumn.Name;
        private bool _sortAscending = true;

        public MainWindow()
        {
            InitializeComponent();
            IcFolders.ItemsSource = _filterFolders;
            IcWords.ItemsSource = _filterWords;
            IcFileNames.ItemsSource = _filterFileNames;

            _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(130) };
            _debounceTimer.Tick += (s, e) =>
            {
                _debounceTimer.Stop();
                PerformSearch();
            };

            // Canlı otomatik güncelleme zamanlayıcısı
            _autoRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _autoRefreshTimer.Tick += (s, e) =>
            {
                PerformSearch();
            };

            Loaded += (s, e) =>
            {
                CmbLanguage.SelectionChanged += CmbLanguage_SelectionChanged;

                PopulateDrives();
                LoadUserSettings();

                // Önceki taranmış dosyaları önbellekten (cache) anında yükle
                bool hasCache = _engine.LoadFromCache();
                if (hasCache && _engine.TotalFilesCount > 0)
                {
                    PerformSearch();
                    TxtStatus.Text = $"{Loc.Get("Ready")} ({_engine.TotalFilesCount:N0} dosya yüklendi / files loaded).";
                }
                else
                {
                    TxtStatus.Text = Loc.CurrentLanguage == "en" 
                        ? "Ready. Click '⚡ Rescan All' to start indexing." 
                        : "Hazır. Aramak için '⚡ Tümünü Tekrar Tara' butonuna basarak taramayı başlatın.";
                }
            };

            Closing += (s, e) => SaveUserSettings();
        }

        private void LoadUserSettings()
        {
            var s = AppSettings.Load();
            TxtSearch.Text = s.SearchText ?? "";
            TxtContentSearch.Text = s.ContentSearchText ?? "";
            if (!string.IsNullOrEmpty(s.CustomExtensions)) TxtCustomExt.Text = s.CustomExtensions;

            string savedLang = s.Language ?? "tr";
            for (int i = 0; i < CmbLanguage.Items.Count; i++)
            {
                if ((CmbLanguage.Items[i] as ComboBoxItem)?.Tag?.ToString() == savedLang)
                {
                    CmbLanguage.SelectedIndex = i;
                    break;
                }
            }
            ApplyLanguage(savedLang);

            if (s.SavedFolders != null)
            {
                _filterFolders.Clear();
                foreach (var f in s.SavedFolders) _filterFolders.Add(f);
            }

            if (s.SavedFileNames != null)
            {
                _filterFileNames.Clear();
                foreach (var fn in s.SavedFileNames) _filterFileNames.Add(fn);
            }
            if (s.SavedWords != null)
            {
                _filterWords.Clear();
                foreach (var w in s.SavedWords) _filterWords.Add(w);
            }
        }

        private void SaveUserSettings()
        {
            var s = new AppSettings
            {
                SearchText = TxtSearch?.Text ?? "",
                ContentSearchText = TxtContentSearch?.Text ?? "",
                FolderNameSearch = "",
                CustomExtensions = TxtCustomExt.Text,
                Language = Loc.CurrentLanguage,
                SavedFolders = _filterFolders.ToList(),
                SavedFileNames = _filterFileNames.ToList(),
                SavedWords = _filterWords.ToList()
            };
            s.Save();
        }

        private void PopulateDrives()
        {
            CmbDrives.Items.Clear();
            CmbDrives.Items.Add("Tüm Sürücüler / All Drives");
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
            foreach (var d in drives)
            {
                CmbDrives.Items.Add($"{d.Name} ({d.DriveFormat})");
            }
            CmbDrives.SelectedIndex = 0;
            CmbDrives.SelectionChanged += (s, e) => TriggerSearch();
        }

        private void TriggerSearch()
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void FilterInputChanged(object sender, TextChangedEventArgs e)
        {
            if (DgResults != null)
            {
                TriggerSearch();
            }
        }

        private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbLanguage == null || CmbLanguage.SelectedItem == null) return;
            string lang = (CmbLanguage.SelectedItem as ComboBoxItem)?.Tag?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(lang)) lang = "tr";
            ApplyLanguage(lang);
        }

        private void ApplyLanguage(string lang)
        {
            Loc.CurrentLanguage = lang;
            this.Title = Loc.Get("Title");
            LblFileName.Text = Loc.Get("FileName");
            BtnAddFileName.Content = Loc.Get("AddFileName");
            LblContentSearch.Text = Loc.Get("ContentSearch");
            BtnAddWord.Content = Loc.Get("AddWord");
            BtnRescanAll.Content = Loc.Get("RescanAll");
            LblTargetFolders.Text = Loc.Get("TargetFolders");
            BtnBrowse.Content = Loc.Get("Browse");
            BtnAddFolder.Content = Loc.Get("AddFolder");
            LblDrive.Text = Loc.Get("Drive");
            BtnScanSelectedFolders.Content = Loc.Get("ScanSelected");
            LblExtensions.Text = Loc.Get("Extensions");

            ColName.Header = Loc.Get("ColName");
            ColSnippet.Header = Loc.Get("ColSnippet");
            ColPath.Header = Loc.Get("ColPath");
            ColExt.Header = Loc.Get("ColExt");
            ColSize.Header = Loc.Get("ColSize");
            ColDate.Header = Loc.Get("ColDate");

            MenuOpen.Header = Loc.Get("MenuOpen");
            MenuOpenFolder.Header = Loc.Get("MenuOpenFolder");
            MenuCopyPath.Header = Loc.Get("MenuCopyPath");
            MenuCopyDir.Header = Loc.Get("MenuCopyDir");

            TxtStatus.Text = Loc.Get("Ready");
            TriggerSearch();
        }

        private void BtnManualRefresh_Click(object sender, RoutedEventArgs e) => PerformSearch();

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddCurrentFileNameFilter();
                e.Handled = true;
            }
        }

        private void BtnAddFileName_Click(object sender, RoutedEventArgs e)
        {
            AddCurrentFileNameFilter();
        }

        private void AddCurrentFileNameFilter()
        {
            string fn = TxtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(fn))
            {
                if (!_filterFileNames.Contains(fn, StringComparer.OrdinalIgnoreCase))
                {
                    _filterFileNames.Add(fn);
                    TxtSearch.Clear();
                    TriggerSearch();
                }
            }
        }

        private void BtnRemoveFileName_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string fn)
            {
                _filterFileNames.Remove(fn);
                TriggerSearch();
            }
        }

        private void BtnClearFileNames_Click(object sender, RoutedEventArgs e)
        {
            AppLogger.Log("BtnClearFileNames_Click: Clearing all filename filters.");
            _filterFileNames.Clear();
            TxtSearch.Clear();
            TriggerSearch();
        }

        private void TxtContentSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddCurrentWordFilter();
                e.Handled = true;
            }
        }

        private void BtnAddWord_Click(object sender, RoutedEventArgs e)
        {
            AddCurrentWordFilter();
        }

        private void AddCurrentWordFilter()
        {
            string word = TxtContentSearch.Text.Trim();
            if (!string.IsNullOrEmpty(word))
            {
                if (!_filterWords.Contains(word, StringComparer.OrdinalIgnoreCase))
                {
                    _filterWords.Add(word);
                    TxtContentSearch.Clear();
                    TriggerSearch();
                }
            }
        }

        private void BtnRemoveWord_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string word)
            {
                _filterWords.Remove(word);
                TriggerSearch();
            }
        }

        private void BtnClearWords_Click(object sender, RoutedEventArgs e)
        {
            AppLogger.Log("BtnClearWords_Click: Clearing all search words.");
            _filterWords.Clear();
            TxtContentSearch.Clear();
            TriggerSearch();
        }

        private void BtnViewLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logPath = AppLogger.GetLogPath();
                AppLogger.Log($"BtnViewLog_Click: Opening log file at {logPath}");
                if (!File.Exists(logPath))
                {
                    File.WriteAllText(logPath, $"[INFO] Debug Log başlatıldı: {DateTime.Now}\n");
                }
                Process.Start("notepad.exe", $"\"{logPath}\"");
            }
            catch (Exception ex)
            {
                AppLogger.Error("BtnViewLog_Click failed", ex);
                MessageBox.Show("Debug log dosyası açılamadı: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                fbd.Description = "Filtrelenecek veya taranacak klasörü seçin:";
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TxtFolderInput.Text = fbd.SelectedPath;
                }
            }
        }

        private void BtnAddFolder_Click(object sender, RoutedEventArgs e)
        {
            string folder = TxtFolderInput.Text.Trim();
            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                if (!_filterFolders.Contains(folder, StringComparer.OrdinalIgnoreCase))
                {
                    _filterFolders.Add(folder);
                    TxtFolderInput.Clear();
                    TriggerSearch();
                }
            }
            else
            {
                MessageBox.Show("Lütfen geçerli ve var olan bir klasör yolu seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnRemoveFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string folder)
            {
                _filterFolders.Remove(folder);
                TriggerSearch();
            }
        }

        private async void BtnRescanAll_Click(object sender, RoutedEventArgs e)
        {
            await StartFullScan();
        }

        private void BtnStopScan_Click(object sender, RoutedEventArgs e)
        {
            AppLogger.Log("BtnStopScan_Click: User requested cancellation of scanning.");
            _cts?.Cancel();
            TxtStatus.Text = "Tarama durduruluyor, taranan veriler işleniyor...";
        }

        private async void BtnScanSelectedFolders_Click(object sender, RoutedEventArgs e)
        {
            await ScanSelectedFoldersInternal(false);
        }

        private async void BtnDirectTargetSearch_Click(object sender, RoutedEventArgs e)
        {
            await ScanSelectedFoldersInternal(true);
        }

        private async Task ScanSelectedFoldersInternal(bool autoTriggerSearch)
        {
            var folders = _filterFolders.ToList();
            if (folders.Count == 0 && !string.IsNullOrWhiteSpace(TxtFolderInput.Text))
            {
                string f = TxtFolderInput.Text.Trim();
                if (Directory.Exists(f))
                {
                    folders.Add(f);
                    if (!_filterFolders.Contains(f, StringComparer.OrdinalIgnoreCase))
                    {
                        _filterFolders.Add(f);
                    }
                }
            }

            if (folders.Count == 0)
            {
                MessageBox.Show("Lütfen önce bir hedef dizin seçin veya [+] ile ekleyin.", "Dizin Yok", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SetScanUIState(true);
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            var statusProg = new Progress<string>(s => TxtStatus.Text = s);
            var countProg = new Progress<int>(c => TxtCount.Text = $"{c:N0} dosya okundu");

            var sw = Stopwatch.StartNew();
            try
            {
                await _engine.IndexDirectoriesAsync(folders, statusProg, countProg, _cts.Token);
                sw.Stop();
                TxtStatus.Text = $"Seçili dizin(ler) tarandı: {_engine.TotalFilesCount:N0} dosya ({sw.ElapsedMilliseconds} ms).";
                PerformSearch();
            }
            catch (OperationCanceledException)
            {
                TxtStatus.Text = "Tarama durduruldu.";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Hata: " + ex.Message;
            }
            finally
            {
                SetScanUIState(false);
            }
        }

        private async Task StartFullScan()
        {
            SetScanUIState(true);
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            var statusProg = new Progress<string>(s => TxtStatus.Text = s);
            var countProg = new Progress<int>(c => TxtCount.Text = $"{c:N0} dosya okundu");

            var sw = Stopwatch.StartNew();
            try
            {
                var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed).ToList();
                if (CmbDrives.SelectedIndex > 0)
                {
                    string selectedLetter = CmbDrives.SelectedItem.ToString().Substring(0, 3);
                    drives = drives.Where(d => d.Name.StartsWith(selectedLetter, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                await _engine.IndexDrivesAsync(drives, statusProg, countProg, _cts.Token);
                sw.Stop();
                TxtStatus.Text = $"İndeksleme tamamlandı: {_engine.TotalFilesCount:N0} dosya ({sw.ElapsedMilliseconds} ms).";
                PerformSearch();
            }
            catch (OperationCanceledException)
            {
                TxtStatus.Text = "İndeksleme iptal edildi.";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Hata: " + ex.Message;
            }
            finally
            {
                SetScanUIState(false);
            }
        }

        private void SetScanUIState(bool scanning)
        {
            BtnRescanAll.IsEnabled = !scanning;
            BtnScanSelectedFolders.IsEnabled = !scanning;
            if (BtnDirectTargetSearch != null) BtnDirectTargetSearch.IsEnabled = !scanning;
            if (BtnDirectSearch != null) BtnDirectSearch.IsEnabled = !scanning;
            if (BtnIndexAllDrives != null) BtnIndexAllDrives.IsEnabled = !scanning;
            if (BtnStopScan != null) BtnStopScan.Visibility = scanning ? Visibility.Visible : Visibility.Collapsed;
            PbStatus.Visibility = scanning ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PerformSearch()
        {
            var options = BuildSearchOptions();
            var sw = Stopwatch.StartNew();
            var matches = _engine.Search(options, _currentSortCol, _sortAscending, 10000);
            sw.Stop();

            _currentResults = matches;
            DgResults.ItemsSource = _currentResults;

            string sortInfo = $"{_currentSortCol} ({(_sortAscending ? "▲" : "▼")})";
            TxtCount.Text = $"{Loc.Get("Matched")}: {_currentResults.Count:N0} / {_engine.TotalFilesCount:N0} | {Loc.Get("Sorting")}: {sortInfo} ({sw.ElapsedMilliseconds} ms)";
        }

        private SearchFilterOptions BuildSearchOptions()
        {
            string folderInput = TxtFolderInput?.Text?.Trim() ?? "";
            var targetFolders = _filterFolders.ToList();
            if (targetFolders.Count == 0 && !string.IsNullOrWhiteSpace(folderInput) && Directory.Exists(folderInput))
            {
                targetFolders.Add(folderInput);
            }

            var opt = new SearchFilterOptions
            {
                Query = TxtSearch?.Text?.Trim() ?? "",
                ContentQuery = TxtContentSearch?.Text?.Trim() ?? "",
                TargetWords = _filterWords.ToList(),
                TargetFileNames = _filterFileNames.ToList(),
                FolderNameQuery = "",
                TargetFolders = targetFolders,
                CustomExtensions = TxtCustomExt?.Text?.Trim() ?? "",
                FilesOnly = false,
                FoldersOnly = false,
                RequireFilter = false
            };

            return opt;
        }

        private void DgResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgResults.SelectedItem is FileRecord file)
            {
                TxtSelectedFileInfo.Text = $"{file.Name} ({file.FormattedSize})";
                if (file.MatchedLines != null && file.MatchedLines.Count > 0)
                {
                    LbLineMatches.ItemsSource = file.MatchedLines;
                }
                else
                {
                    // Eğer önceden taranmadıysa ve metin dosyasıysa canlı eşleşmeleri yükle
                    var list = LoadLinesForFile(file);
                    LbLineMatches.ItemsSource = list;
                }
            }
            else
            {
                TxtSelectedFileInfo.Text = "Dosyaya tıklayarak satırları inceleyin.";
                LbLineMatches.ItemsSource = null;
            }
        }

        private List<LineMatch> LoadLinesForFile(FileRecord file)
        {
            var result = new List<LineMatch>();
            if (file == null || file.IsDirectory || !File.Exists(file.FullPath)) return result;

            var words = new List<string>();
            string directWord = TxtContentSearch?.Text?.Trim();
            if (!string.IsNullOrEmpty(directWord)) words.Add(directWord);
            if (_filterWords != null)
            {
                foreach (var w in _filterWords)
                {
                    if (!string.IsNullOrEmpty(w) && !words.Contains(w, StringComparer.OrdinalIgnoreCase))
                        words.Add(w);
                }
            }

            if (words.Count == 0) return result;

            try
            {
                using (var fs = new FileStream(file.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 64 * 1024))
                using (var sr = new StreamReader(fs, System.Text.Encoding.Default, true))
                {
                    string line;
                    int lineNum = 1;
                    while ((line = sr.ReadLine()) != null)
                    {
                        for (int w = 0; w < words.Count; w++)
                        {
                            if (line.IndexOf(words[w], StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                string trimmed = line.Trim();
                                if (trimmed.Length > 200) trimmed = trimmed.Substring(0, 197) + "...";
                                result.Add(new LineMatch
                                {
                                    LineNumber = lineNum,
                                    LineText = trimmed,
                                    MatchedWord = words[w],
                                    IsHighlighted = true
                                });
                                break;
                            }
                        }
                        lineNum++;
                        if (result.Count >= 100 || lineNum > 100000) break;
                    }
                }
            }
            catch { }
            return result;
        }

        private void DgResults_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;
            string header = e.Column.Header?.ToString();

            SortColumn clickedCol = SortColumn.Name;
            if (header != null && (header.Contains("Klasör") || header.Contains("Folder") || header.Contains("Location"))) clickedCol = SortColumn.Path;
            else if (header != null && (header.Contains("Uzantı") || header.Contains("Extension"))) clickedCol = SortColumn.Extension;
            else if (header != null && (header.Contains("Boyut") || header.Contains("Size"))) clickedCol = SortColumn.Size;
            else if (header != null && (header.Contains("Tarih") || header.Contains("Date"))) clickedCol = SortColumn.Date;

            if (_currentSortCol == clickedCol)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _currentSortCol = clickedCol;
                _sortAscending = true;
            }

            e.Column.SortDirection = _sortAscending ? System.ComponentModel.ListSortDirection.Ascending : System.ComponentModel.ListSortDirection.Descending;
            PerformSearch();
        }

        private void OpenSelectedFile()
        {
            if (DgResults.SelectedItem is FileRecord item)
            {
                try
                {
                    if (File.Exists(item.FullPath) || Directory.Exists(item.FullPath))
                    {
                        Process.Start(new ProcessStartInfo(item.FullPath) { UseShellExecute = true });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Açılamadı: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenSelectedFolder()
        {
            if (DgResults.SelectedItem is FileRecord item)
            {
                try
                {
                    string path = item.FullPath;
                    if (File.Exists(path))
                    {
                        Process.Start("explorer.exe", $"/select,\"{path}\"");
                    }
                    else if (Directory.Exists(path))
                    {
                        Process.Start("explorer.exe", $"\"{path}\"");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Klasör açılamadı: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CopySelectedPath()
        {
            if (DgResults.SelectedItem is FileRecord item && !string.IsNullOrEmpty(item.FullPath))
            {
                Clipboard.SetText(item.FullPath);
            }
        }

        private void CopySelectedDirectory()
        {
            if (DgResults.SelectedItem is FileRecord item && !string.IsNullOrEmpty(item.DirectoryPath))
            {
                Clipboard.SetText(item.DirectoryPath);
            }
        }

        private void DgResults_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // İsteğe bağlı olarak doğrudan Windows Shell menüsü açılabilir veya menü öğesinden tetiklenir
        }

        private void MenuShell_Click(object sender, RoutedEventArgs e)
        {
            ShowNativeWindowsMenu();
        }

        private void ShowNativeWindowsMenu()
        {
            if (DgResults.SelectedItem is FileRecord item && !string.IsNullOrEmpty(item.FullPath))
            {
                try
                {
                    var interop = new System.Windows.Interop.WindowInteropHelper(this);
                    var mousePos = System.Windows.Forms.Cursor.Position;
                    ShellContextMenuHelper.ShowShellContextMenu(item.FullPath, mousePos, interop.Handle);
                }
                catch (Exception ex)
                {
                    AppLogger.Error("ShowNativeWindowsMenu failed", ex);
                }
            }
        }

        private void DgResults_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OpenSelectedFile();
        private void MenuOpen_Click(object sender, RoutedEventArgs e) => OpenSelectedFile();
        private void MenuOpenFolder_Click(object sender, RoutedEventArgs e) => OpenSelectedFolder();
        private void MenuCopyPath_Click(object sender, RoutedEventArgs e) => CopySelectedPath();
        private void MenuCopyDir_Click(object sender, RoutedEventArgs e) => CopySelectedDirectory();

        private void DgResults_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OpenSelectedFile();
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
            {
                CopySelectedPath();
                e.Handled = true;
            }
        }
    }
}
