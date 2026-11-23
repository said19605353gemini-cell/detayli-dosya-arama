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
        private CancellationTokenSource _cts;

        private DispatcherTimer _debounceTimer;
        private SortColumn _currentSortCol = SortColumn.Name;
        private bool _sortAscending = true;

        public MainWindow()
        {
            InitializeComponent();
            IcFolders.ItemsSource = _filterFolders;

            _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(130) };
            _debounceTimer.Tick += (s, e) =>
            {
                _debounceTimer.Stop();
                PerformSearch();
            };

            Loaded += async (s, e) =>
            {
                CmbSize.SelectionChanged += (cs, ce) => TriggerSearch();
                CmbDate.SelectionChanged += (cs, ce) => TriggerSearch();
                RbAll.Checked += (cs, ce) => TriggerSearch();
                RbFilesOnly.Checked += (cs, ce) => TriggerSearch();
                RbFoldersOnly.Checked += (cs, ce) => TriggerSearch();

                PopulateDrives();
                await StartFullScan();
            };
        }

        private void PopulateDrives()
        {
            CmbDrives.Items.Clear();
            CmbDrives.Items.Add("Tüm Sürücüler");
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
            string lang = (CmbLanguage.SelectedItem as ComboBoxItem)?.Content?.ToString()?.ToLowerInvariant();
            if (lang == "en")
            {
                ApplyLanguage("en");
            }
            else
            {
                ApplyLanguage("tr");
            }
        }

        private void ApplyLanguage(string lang)
        {
            Loc.CurrentLanguage = lang;
            this.Title = Loc.Get("Title");
            LblFileName.Text = Loc.Get("FileName");
            LblFolderName.Text = Loc.Get("FolderName");
            BtnRescanAll.Content = Loc.Get("RescanAll");
            LblTargetFolders.Text = Loc.Get("TargetFolders");
            BtnBrowse.Content = Loc.Get("Browse");
            BtnAddFolder.Content = Loc.Get("AddFolder");
            LblDrive.Text = Loc.Get("Drive");
            BtnScanSelectedFolders.Content = Loc.Get("ScanSelected");
            LblExtensions.Text = Loc.Get("Extensions");
            LblSize.Text = Loc.Get("Size");
            LblDate.Text = Loc.Get("Date");
            RbAll.Content = Loc.Get("All");
            RbFilesOnly.Content = Loc.Get("FilesOnly");
            RbFoldersOnly.Content = Loc.Get("FoldersOnly");

            ColName.Header = Loc.Get("ColName");
            ColPath.Header = Loc.Get("ColPath");
            ColExt.Header = Loc.Get("ColExt");
            ColSize.Header = Loc.Get("ColSize");
            ColDate.Header = Loc.Get("ColDate");

            MenuOpen.Header = Loc.Get("MenuOpen");
            MenuOpenFolder.Header = Loc.Get("MenuOpenFolder");
            MenuCopyPath.Header = Loc.Get("MenuCopyPath");
            MenuCopyDir.Header = Loc.Get("MenuCopyDir");

            TriggerSearch();
        }
        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            if (DgResults != null)
            {
                TriggerSearch();
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

            string sortInfo = $"{_currentSortCol} ({(_sortAscending ? "Artan ▲" : "Azalan ▼")})";
            TxtCount.Text = $"Eşleşen: {_currentResults.Count:N0} / {_engine.TotalFilesCount:N0} | Sıralama: {sortInfo} ({sw.ElapsedMilliseconds} ms)";
        }

        private SearchFilterOptions BuildSearchOptions()
        {
            var opt = new SearchFilterOptions
            {
                Query = TxtSearch.Text,
                FolderNameQuery = TxtFolderNameSearch.Text.Trim(),
                TargetFolders = _filterFolders.ToList(),
                CustomExtensions = TxtCustomExt.Text.Trim(),
                FilesOnly = RbFilesOnly.IsChecked == true,
                FoldersOnly = RbFoldersOnly.IsChecked == true
            };

            // Boyut filtresi
            switch (CmbSize.SelectedIndex)
            {
                case 1: opt.MinSize = 0; opt.MaxSize = 1024 * 1024; break; // < 1MB
                case 2: opt.MinSize = 1024 * 1024; opt.MaxSize = 100L * 1024 * 1024; break; // 1-100MB
                case 3: opt.MinSize = 100L * 1024 * 1024; opt.MaxSize = 1024L * 1024 * 1024; break; // 100MB - 1GB
                case 4: opt.MinSize = 1024L * 1024 * 1024; opt.MaxSize = -1; break; // > 1GB
                default: opt.MinSize = -1; opt.MaxSize = -1; break;
            }

            // Tarih filtresi
            DateTime now = DateTime.Now;
            switch (CmbDate.SelectedIndex)
            {
                case 1: opt.MinDate = now.Date; break; // Bugün
                case 2: opt.MinDate = now.Date.AddDays(-7); break; // Son 7 Gün
                case 3: opt.MinDate = now.Date.AddDays(-30); break; // Son 30 Gün
                case 4: opt.MinDate = new DateTime(now.Year, 1, 1); break; // Bu Yıl
                default: opt.MinDate = null; break;
            }

            return opt;
        }

        private void DgResults_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;
            string header = e.Column.Header?.ToString();

            SortColumn clickedCol = SortColumn.Name;
            if (header != null && header.Contains("Klasör")) clickedCol = SortColumn.Path;
            else if (header == "Uzantı") clickedCol = SortColumn.Extension;
            else if (header == "Boyut") clickedCol = SortColumn.Size;
            else if (header == "Değiştirilme Tarihi") clickedCol = SortColumn.Date;

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
