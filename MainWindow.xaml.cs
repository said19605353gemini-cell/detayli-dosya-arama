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
                TxtLastMinutes.TextChanged += TxtLastMinutes_TextChanged;
                TxtAutoRefreshInterval.TextChanged += TxtAutoRefreshInterval_TextChanged;

                CmbLanguage.SelectionChanged += CmbLanguage_SelectionChanged;
                CmbSize.SelectionChanged += (cs, ce) => TriggerSearch();
                CmbDate.SelectionChanged += (cs, ce) => TriggerSearch();
                RbAll.Checked += (cs, ce) => TriggerSearch();
                RbFilesOnly.Checked += (cs, ce) => TriggerSearch();
                RbFoldersOnly.Checked += (cs, ce) => TriggerSearch();

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

            if (s.CategoryIndex >= 0 && s.CategoryIndex < CmbCategory.Items.Count) CmbCategory.SelectedIndex = s.CategoryIndex;
            if (s.SizeIndex >= 0 && s.SizeIndex < CmbSize.Items.Count) CmbSize.SelectedIndex = s.SizeIndex;
            if (s.DateIndex >= 0 && s.DateIndex < CmbDate.Items.Count) CmbDate.SelectedIndex = s.DateIndex;

            if (s.TypeFilter == 1) RbFilesOnly.IsChecked = true;
            else if (s.TypeFilter == 2) RbFoldersOnly.IsChecked = true;
            else RbAll.IsChecked = true;

            ChkLastMinutes.IsChecked = s.LastMinutesChecked;
            if (!string.IsNullOrEmpty(s.LastMinutesText)) TxtLastMinutes.Text = s.LastMinutesText;

            ChkAutoRefresh.IsChecked = s.AutoRefreshChecked;
            if (!string.IsNullOrEmpty(s.AutoRefreshIntervalText)) TxtAutoRefreshInterval.Text = s.AutoRefreshIntervalText;
            ChkRequireFilter.IsChecked = s.RequireFilterChecked;
            if (s.AutoRefreshChecked)
            {
                UpdateAutoRefreshInterval();
                _autoRefreshTimer.Start();
                LblLiveStatus.Visibility = Visibility.Visible;
            }

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
                CategoryIndex = CmbCategory.SelectedIndex,
                SizeIndex = CmbSize.SelectedIndex,
                DateIndex = CmbDate.SelectedIndex,
                TypeFilter = RbFilesOnly.IsChecked == true ? 1 : (RbFoldersOnly.IsChecked == true ? 2 : 0),
                LastMinutesChecked = ChkLastMinutes.IsChecked == true,
                LastMinutesText = TxtLastMinutes.Text,
                AutoRefreshChecked = ChkAutoRefresh.IsChecked == true,
                AutoRefreshIntervalText = TxtAutoRefreshInterval.Text,
                RequireFilterChecked = ChkRequireFilter.IsChecked == true,
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
            LblCategory.Text = Loc.Get("Category");
            LblExtensions.Text = Loc.Get("Extensions");
            LblSize.Text = Loc.Get("Size");
            LblDate.Text = Loc.Get("Date");
            RbAll.Content = Loc.Get("All");
            RbFilesOnly.Content = Loc.Get("FilesOnly");
            RbFoldersOnly.Content = Loc.Get("FoldersOnly");

            ChkLastMinutes.Content = Loc.Get("LastMinutes");
            ChkAutoRefresh.Content = Loc.Get("AutoRefresh");
            BtnManualRefresh.Content = Loc.Get("ManualRefresh");

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

        private void CmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TxtCustomExt == null || CmbCategory == null || CmbCategory.SelectedItem == null) return;
            int idx = CmbCategory.SelectedIndex;
            switch (idx)
            {
                case 0: // Hepsi
                    TxtCustomExt.Text = "";
                    break;
                case 1: // Kod / Yazı
                    TxtCustomExt.Text = ".txt .cpp .h .c .cs .py .js .ts .html .css .json .xml .md .log";
                    break;
                case 2: // Resimler
                    TxtCustomExt.Text = ".jpg .jpeg .png .gif .bmp .webp .svg .ico .psd";
                    break;
                case 3: // Videolar
                    TxtCustomExt.Text = ".mp4 .mkv .avi .mov .wmv .flv .webm .m4v";
                    break;
                case 4: // Belgeler
                    TxtCustomExt.Text = ".pdf .docx .doc .xlsx .xls .pptx .ppt .txt .csv";
                    break;
                case 5: // Arşivler
                    TxtCustomExt.Text = ".zip .rar .7z .tar .gz .iso";
                    break;
                case 6: // Müzikler
                    TxtCustomExt.Text = ".mp3 .wav .flac .aac .ogg .wma .m4a";
                    break;
            }
            TriggerSearch();
        }

        private void ChkLastMinutes_Click(object sender, RoutedEventArgs e) => TriggerSearch();
        private void ChkRequireFilter_Click(object sender, RoutedEventArgs e) => TriggerSearch();
        private void TxtLastMinutes_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ChkLastMinutes?.IsChecked == true) TriggerSearch();
        }

        private void ChkAutoRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (ChkAutoRefresh.IsChecked == true)
            {
                UpdateAutoRefreshInterval();
                _autoRefreshTimer.Start();
                LblLiveStatus.Visibility = Visibility.Visible;
            }
            else
            {
                _autoRefreshTimer.Stop();
                LblLiveStatus.Visibility = Visibility.Collapsed;
            }
        }

        private void TxtAutoRefreshInterval_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateAutoRefreshInterval();
        }

        private void UpdateAutoRefreshInterval()
        {
            if (_autoRefreshTimer == null || TxtAutoRefreshInterval == null) return;
            if (int.TryParse(TxtAutoRefreshInterval.Text.Trim(), out int sec) && sec > 0)
            {
                _autoRefreshTimer.Interval = TimeSpan.FromSeconds(Math.Max(1, sec));
            }
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

        private async void BtnScanSelectedFolders_Click(object sender, RoutedEventArgs e)
        {
            var folders = _filterFolders.ToList();
            if (folders.Count == 0 && !string.IsNullOrWhiteSpace(TxtFolderInput.Text))
            {
                string f = TxtFolderInput.Text.Trim();
                if (Directory.Exists(f)) folders.Add(f);
            }

            if (folders.Count == 0)
            {
                MessageBox.Show("Lütfen [+] ile en az bir klasör ekleyin veya klasör yolunu girin.", "Klasör Yok", MessageBoxButton.OK, MessageBoxImage.Information);
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
                TxtStatus.Text = $"Klasörler tarandı: {_engine.TotalFilesCount:N0} dosya ({sw.ElapsedMilliseconds} ms).";
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
            var opt = new SearchFilterOptions
            {
                Query = TxtSearch?.Text?.Trim() ?? "",
                ContentQuery = TxtContentSearch?.Text?.Trim() ?? "",
                TargetWords = _filterWords.ToList(),
                TargetFileNames = _filterFileNames.ToList(),
                FolderNameQuery = "",
                TargetFolders = _filterFolders.ToList(),
                CustomExtensions = TxtCustomExt.Text.Trim(),
                FilesOnly = RbFilesOnly.IsChecked == true,
                FoldersOnly = RbFoldersOnly.IsChecked == true,
                RequireFilter = ChkRequireFilter.IsChecked == true
            };

            // Son X Dakika Filtresi
            if (ChkLastMinutes?.IsChecked == true && int.TryParse(TxtLastMinutes.Text.Trim(), out int mins) && mins > 0)
            {
                opt.LastMinutes = mins;
            }

            // Boyut filtresi
            switch (CmbSize.SelectedIndex)
            {
                case 1: opt.MinSize = 0; opt.MaxSize = 1024 * 1024; break;
                case 2: opt.MinSize = 1024 * 1024; opt.MaxSize = 100L * 1024 * 1024; break;
                case 3: opt.MinSize = 100L * 1024 * 1024; opt.MaxSize = 1024L * 1024 * 1024; break;
                case 4: opt.MinSize = 1024L * 1024 * 1024; opt.MaxSize = -1; break;
                default: opt.MinSize = -1; opt.MaxSize = -1; break;
            }

            // Tarih filtresi
            DateTime now = DateTime.Now;
            switch (CmbDate.SelectedIndex)
            {
                case 1: opt.MinDate = now.Date; break;
                case 2: opt.MinDate = now.Date.AddDays(-7); break;
                case 3: opt.MinDate = now.Date.AddDays(-30); break;
                case 4: opt.MinDate = new DateTime(now.Year, 1, 1); break;
                default: opt.MinDate = null; break;
            }

            return opt;
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
