# 🚀 WizFile Ultra - Detaylı Dosya & Klasör & İçerik Arama Motoru (.NET 4.8 / WPF)

WizFile ve Everything mantığında, **Windows NTFS Master File Table ($MFT / USN Journal)** kayıtlarını doğrudan okuyarak diskteki yüz binlerce dosyayı milisaniyeler içinde indeksleyen, çoklu kelime grep içerik araması, kalıcı önbellek, 9 uluslararası dil desteği, canlı otomatik takip ve detaylı debug hata günlüğü barındıran açık kaynaklı arama aracı.

---

## 🌟 Öne Çıkan Özellikler

### 1. 🔍 Çoklu Kelime Arama & `[+]` Ekle / `[-]` Çıkar - Temizle
- **`➕ Ekle`** butonu veya doğrudan **Enter** tuşuna basarak birden çok arama kelimesi ekleyebilirsiniz.
- Eklenen her kelime dinamik mavi etiket rozeti (`badge`) olarak listelenir.
- Her kelimenin yanındaki **`➖`** butonuna basarak filtrelerden tek tek çıkarabilir, **`➖ Temizle`** butonuyla tüm kelimeleri tek seferde silebilirsiniz.
- Metin dosyalarında satır satır grep yaparak eşleşen satır numarasını (**Satır / Line**) ve içerik kesitini (**Eşleşen Metin / Snippet**) anında listeler.

### 2. ⚡ Ultra Hızlı MFT İndeksleme & Manuel Tarama (Otomatik Doğrulama Kilidi Yok)
- Standart yavaş `Directory.EnumerateFiles` yerine Win32 `DeviceIoControl` ve `FSCTL_ENUM_USN_DATA` API'leri ile doğrudan NTFS dosya tablosunu bloklar halinde okur.
- 500.000+ dosyayı **1-2 saniye** içinde arama için hazır hale getirir.
- Program her açıldığında diski otomatik tarayarak bilgisayarı kilitlemez; önceki taranan dosyaları ikili önbellekten (`file_cache.dat`) anında yükler. Yalnızca kullanıcı **`⚡ Tümünü Tekrar Tara`** veya **`📂 Seçili Klasörleri Tara`** butonuna bastığında tarama yapar.

### 3. 📜 Kalıcı Debug Hata Günlüğü (`debug_log.txt`)
- Tüm işlemler, taramalar ve olası istisnalar zaman damgasıyla `%APPDATA%\WizFileUltra\debug_log.txt` ve program klasörüne kaydedilir.
- Arayüzdeki **`📜 Debug Log`** butonu ile tek tıkla günlük dosyası Not Defteri'nde açılır.

### 4. 💾 Kalıcı Ayarlar & Son Okunan Dosyalar
- Yazdığınız arama kelimeleri, seçili kategoriler, hedef klasörler, süre filtreleri ve dil tercihleri `%APPDATA%\WizFileUltra\settings.json` içerisine otomatik kaydedilir; programı kapatsanız dahi silinmez.

### 5. 🌐 9 Uluslararası Dil Desteği
- Tek tıkla anında dil değiştirilebilir:
  - 🇹🇷 Türkçe (TR)
  - 🇬🇧 English (EN)
  - 🇩🇪 Deutsch (DE)
  - 🇫🇷 Français (FR)
  - 🇪🇸 Español (ES)
  - 🇷🇺 Русский (RU)
  - 🇨🇳 中文 (ZH)
  - 🇯🇵 日本語 (JA)
  - 🇸🇦 العربية (AR)

### 6. ⏱ Son X Dakikada Değişen Dosyaları Filtreleme (Değiştirilebilir Süre)
- **`⏱ Sadece Son (dk) Değişenleri Göster`** kutucuğu ile istediğiniz süreyi belirleyebilirsiniz (Örn: 10 dk, 30 dk, 60 dk).
- Özellikle üzerinde çalıştığınız, yeni derlenen veya yeni indirilen güncel dosyaları tek hamlede izole etmenizi sağlar.

### 7. 🔄 Canlı Otomatik Güncelleme (Canlı Takip)
- **`🔄 Canlı Otomatik Güncelle (sn)`** seçeneği ile arama sonuçlarını belirlediğiniz saniye aralıklarında (örn: 5 saniyede bir) arayüzü dondurmadan sürekli canlı yeniler.

### 8. 🗂️ Hazır Dosya Kategorileri (Kategorize Etme)
Açılır kategorilerden tek tıkla geçiş yapabilirsiniz:
- 💻 **Kod / Metin (Varsayılan)**: `.txt .cpp .h .c .cs .py .js .ts .html .css .json .xml .md .log`
- 🖼 **Resimler**: `.jpg .jpeg .png .gif .bmp .webp .svg .ico .psd`
- 🎬 **Videolar**: `.mp4 .mkv .avi .mov .wmv .flv .webm .m4v`
- 📑 **Belgeler**: `.pdf .docx .doc .xlsx .xls .pptx .ppt .txt .csv`
- 📦 **Arşivler**: `.zip .rar .7z .tar .gz .iso`
- 🎵 **Müzikler**: `.mp3 .wav .flac .aac .ogg .wma .m4a`
- 🌐 **Hepsi**: Tüm dosya türleri

### 9. 📁 Hedef Dizinler & Çoklu Klasör Kökleri (+ Rozetler)
- **`➕ Listeye Ekle`** butonu veya klasör seçiciyle birden fazla hedef klasör rozeti ekleyebilir, aramanızı sadece bu kök klasörlerle sınırlandırabilirsiniz.

---

## 📥 İndirme ve Kullanım
- En son kararlı sürümü doğrudan GitHub Releases sekmesinden indirebilirsiniz:
  👉 **[WizFile Ultra İndir (.exe & .zip)](https://github.com/said19605353gemini-cell/detayli-dosya-arama/releases/latest)**
- Herhangi bir kuruluma gerek olmaksızın doğrudan çift tıklayarak çalıştırabilirsiniz.

---

## 🛠️ Yerel Derleme
```bash
git clone https://github.com/said19605353gemini-cell/detayli-dosya-arama.git
cd detayli-dosya-arama
dotnet build -c Release
```
Çalıştırılabilir dosya: `bin\Release\net48\WizFileUltra.exe`

---

## 📄 Lisans
MIT License - Özgürce kullanılabilir ve geliştirilebilir.
