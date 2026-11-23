# WizFile Ultra - Ultra Fast NTFS File Search Engine (.NET 4.8 / WPF)

WizFile ve Everything mantığıyla geliştirilmiş, doğrudan NTFS Master File Table ($MFT / USN Journal) kayıtlarını Win32 API ile okuyarak diskteki yüz binlerce dosyayı milisaniyeler içinde indeksleyen ve arayan açık kaynaklı masaüstü aracı.

---

## 🌟 Temel Özellikler

- ⚡ **Ultra Hızlı NTFS MFT Okuma**: Win32 `DeviceIoControl` ve `FSCTL_ENUM_USN_DATA` kullanarak tüm diski standart dosya tarayıcılarına kıyasla 100 kat daha hızlı okur.
- 🎨 **Modern WPF Arayüzü**:
  - Sanallaştırılmış liste (`VirtualizingPanel`) ile 100.000+ dosyada dahi sıfır takılma (60 FPS).
  - Modern renkler, duyarlı butonlar ve rozet/etiket sistemi.
- ➕ **Çoklu Klasör Filtreleme (Dinamik Etiketler)**:
  - Klasör seçip `[➕ Listeye Ekle]` butonuyla birden çok klasör filtresi tanımlayabilme.
  - İstenen klasörleri tek tıkla `[✖]` ile listeden çıkarabilme.
  - İster tüm sürücüleri, ister sadece eklenen klasörleri baştan indeksleme seçeneği.
- 🔍 **Gelişmiş Çoklu Kelime & Mantıksal Arama**:
  - `AND` Mantığı: `rapor 2024 final` (tüm kelimeleri içerenler).
  - `OR` / Alternatif: `fatura|makbuz` (fatura veya makbuz içerenler).
  - Arama çubuğuna doğrudan uzantı yazabilme: `proje .cpp .h`
- 🎯 **Elle Çoklu Uzantı Tanımlama**:
  - Uzantılar kutusuna yan yana uzantı yazma: `.cpp .h .cs` veya `pdf docx xlsx`
- 📊 **Boyut & Tarih Filtreleme ve Anında Kolon Sıralaması**:
  - Tıklanan kolon başlığına göre (Ad, Konum, Uzantı, Boyut, Tarih) anında Artan/Azalan sıralama.
- 📋 **Hızlı Aksiyonlar**:
  - Çift tık veya Enter ile açma.
  - Bulunduğu klasörü Windows Explorer üzerinde otomatik seçerek açma.
  - Dosya yolunu veya klasör yolunu kopyalama.

---

## 🛠️ Kurulum & Derleme

### Gereksinimler
- Windows 10 / 11
- .NET Framework 4.8
- MFT ve raw volume okuması için **Yönetici Olarak Çalıştır** yetkisi

### Derleme
```bash
git clone https://github.com/KULLANICI_ADINIZ/WizFileUltra.git
cd WizFileUltra
dotnet build -c Release
```

Derlenen çalıştırılabilir dosya: `bin\Release\net48\WizFileUltra.exe`

---

## 📄 Lisans
MIT License
