# 🚀 WizFile Ultra - Detaylı Dosya & Klasör & İçerik Arama Motoru (.NET 4.8 / WPF)

WizFile ve Everything mantığında, **Windows NTFS Master File Table ($MFT / USN Journal)** kayıtlarını doğrudan okuyarak diskteki yüz binlerce dosyayı milisaniyeler içinde indeksleyen, çoklu kelime grep içerik araması, kalıcı önbellek, 9 uluslararası dil desteği, canlı otomatik takip ve detaylı debug hata günlüğü barındıran açık kaynaklı arama aracı.

---

## 🌟 Öne Çıkan Özellikler

### 1. 📄 Ayrı Dosya Adı ve 🔍 İçerik / Kelime Arama
- **📄 Dosya Adı Arama:**
  - Dosya adı çubuğuna doğrudan dosya adını (örn: `asdadasda.txt` veya `config`) yazabilirsiniz.
  - **`➕ Dosya Ekle`** butonu veya **Enter** tuşu ile birden fazla dosya adı rozeti ekleyebilirsiniz (örnek: hem `asdadasda.txt` hem de `rapor.docx` rozetlerini ekleyerek arama yapma).
  - Eklenen rozetler yeşil etiketler olarak listelenir, **`➖`** ile tek tek veya **`➖ Temizle`** ile topluca silinebilir.
- **🔍 Dosya İçi Kelime Arama (Content Grep):**
  - Dosya içeriğinde geçmesini istediğiniz kelimeleri (örn: `sadasdasdas`) ayrı kelime çubuğuna yazabilirsiniz.
  - **`➕ Kelime Ekle`** butonu veya **Enter** ile birden fazla aranacak kelime rozeti eklenebilir.
  - Kod ve metin dosyaları içerisinde eşleşen satır numarasını (**Satır / Line**) ve eşleşen metin parçasını (**Eşleşen Metin / Snippet**) anında görüntüler.
- **Birlikte Kullanım:** Dosya adı filtreleriyle içerik kelime filtreleri tamamen bağımsız çalışır ve aynı anda birlikte filtrelenebilir (Örn: Adı `asdadasda.txt` olan ve içerisinde `sadasdasdas` kelimesi geçen dosyalar).

### 2. ⚡ Ultra Hızlı MFT İndeksleme & Manuel Tarama (Otomatik Doğrulama Kilidi Yok)
- Standart yavaş `Directory.EnumerateFiles` yerine Win32 `DeviceIoControl` ve `FSCTL_ENUM_USN_DATA` API'leri ile doğrudan NTFS dosya tablosunu bloklar halinde okur.
- 500.000+ dosyayı **1-2 saniye** içinde arama için hazır hale getirir.
- **Tüm Diski İndeksleme:** İster tüm sabit sürücüleri tek seferde indeksleyin, ister açılır menüden belirli bir sürücüyü (C:, D:) veya seçili hedef klasörleri indeksleyin.
- **Akıllı Filtre Kontrolü (`⚡ Boşken Arama Yapma`):**
  - Bu seçenek işaretlendiğinde arama kutuları boşken arama yapılmaz ve liste boş tutulur.
  - Bir dosya adı veya kelime yazıldığında arama anında tetiklenir; kutular temizlendiğinde liste otomatik olarak boşaltılır.
  - Seçenek kapalıyken diskteki tüm indeksli dosyalar varsayılan olarak listelenir.
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
