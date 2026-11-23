# 🚀 WizFile Ultra - Detaylı Dosya & Klasör Arama Motoru (.NET 4.8 / WPF)

WizFile ve Everything mantığında, **Windows NTFS Master File Table ($MFT / USN Journal)** kayıtlarını doğrudan okuyarak diskteki yüz binlerce dosyayı milisaniyeler içinde indeksleyen, çoklu dil (Türkçe/İngilizce) ve canlı otomatik takip destekli açık kaynaklı arama aracı.

---

## 🌟 Öne Çıkan Özellikler

### 1. ⚡ Ultra Hızlı MFT İndeksleme
- Standart yavaş `Directory.EnumerateFiles` yerine Win32 `DeviceIoControl` ve `FSCTL_ENUM_USN_DATA` API'leri ile doğrudan NTFS dosya tablosunu bloklar halinde okur.
- 500.000+ dosyayı **1-2 saniye** içinde arama için hazır hale getirir.

### 2. ⏱ Son X Dakikada Değişen Dosyaları Filtreleme (Değiştirilebilir Süre)
- **`⏱ Sadece Son (dk) Değişenleri Göster`** kutucuğu ile istediğiniz süreyi belirleyebilirsiniz (Örn: 10 dk, 30 dk, 60 dk).
- Özellikle üzerinde çalıştığınız, yeni derlenen veya yeni indirilen güncel dosyaları tek hamlede izole etmenizi sağlar.

### 3. 🔄 Canlı Otomatik Güncelleme (Canlı Takip)
- **`🔄 Canlı Otomatik Güncelle (sn)`** seçeneği ile arama sonuçlarını belirlediğiniz saniye aralıklarında (örn: 5 saniyede bir) arayüzü dondurmadan sürekli canlı yeniler.

### 4. 🗂️ Hazır Dosya Kategorileri (Kategorize Etme)
Açılır kategorilerden tek tıkla geçiş yapabilirsiniz:
- 💻 **Kod / Metin (Varsayılan)**: `.txt .cpp .h .c .cs .py .js .ts .html .css .json .xml .md .log`
- 🖼 **Resimler**: `.jpg .jpeg .png .gif .bmp .webp .svg .ico .psd`
- 🎬 **Videolar**: `.mp4 .mkv .avi .mov .wmv .flv .webm .m4v`
- 📑 **Belgeler**: `.pdf .docx .doc .xlsx .xls .pptx .ppt .txt .csv`
- 📦 **Arşivler**: `.zip .rar .7z .tar .gz .iso`
- 🎵 **Müzikler**: `.mp3 .wav .flac .aac .ogg .wma .m4a`
- 🌐 **Hepsi**: Tüm dosya türleri

### 5. 📁 Klasör İsmi Detaylı Arama & Çoklu Hedef Kökler (+ Rozetler)
- **Dosya Adı** ve **Klasör İsmi** ayrı kutulardan bağımsız aranabilir (örn: Adında `config` geçen ve klasör yolunda `Release` bulunan dosyalar).
- **`➕ Listeye Ekle`** butonu ile birden fazla hedef klasör rozeti ekleyebilir, aramanızı sadece bu kök klasörlerle sınırlandırabilirsiniz.

### 6. 🌐 Çoklu Dil (Multi-Language: TR / EN)
- Tek tıkla arayüzü **Türkçe** veya **İngilizce**ye çevirebilirsiniz.

---

## 📥 İndirme ve Kullanım
- En son kararlı sürümü doğrudan GitHub Releases sekmesinden indirebilirsiniz:
  👉 **[WizFile Ultra İndir (.exe & .zip)](https://github.com/said19605353gemini-cell/detayli-dosya-arama/releases)**
- Yönetici olarak veya normal kullanıcı olarak doğrudan çift tıklayarak çalıştırabilirsiniz.

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
