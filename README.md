# TroiaLens — Notepad++ eklentisi

![C#](https://img.shields.io/badge/C%23-.NET_Framework_4.8-512BD4?logo=dotnet&logoColor=white)
![Notepad++](https://img.shields.io/badge/Notepad++-x64-90E59A?logo=notepadplusplus&logoColor=black)
![WebView2](https://img.shields.io/badge/WebView2-Evergreen-0078D7?logo=microsoftedge&logoColor=white)
![Windows](https://img.shields.io/badge/Windows-10_%7C_11-0078D6?logo=windows&logoColor=white)
![React](https://img.shields.io/badge/UI-React_19-61DAFB?logo=react&logoColor=black)

Canias ERP'nin **TROIA CODE EXECUTION TRACE** dosyalarını Notepad++ içinde analiz eden bir eklenti.
Trace dosyasını açtığınızda sağ tarafta bir panel açılır. Panelde şu sekmeler vardır: Özet, Çağrı ağacı, SQL,
Tekrarlar, Mesajlar, Hata/Uyarı ve (trace'te DUMP varsa) Dump. Paneldeki bir satıra tıkladığınızda editör o satıra gider.

Paneldeki ekranlar [TroiaLens web uygulamasıyla](https://github.com/ufukt51/TroiaLens) aynıdır.

## Ekran görüntüleri

Görüntülerdeki veriler örnektir, gerçek değildir.

![Özet paneli](docs/screenshots/npp-light-overview.png)

| | |
|---|---|
| ![Satır ayrıntısı](docs/screenshots/npp-light-detail.png) | ![Koyu tema, çağrı ağacı](docs/screenshots/npp-dark-tree.png) |
| Seçilen satırın ayrıntısı altta açılır; editör aynı satıra gider ve metodu sarıyla işaretler. | Koyu tema. Çift tıklanan kelime editörde pembeyle işaretlenir. |
| ![Koyu tema, özet](docs/screenshots/npp-dark-overview.png) | |
| Koyu tema, özet | |

## Nasıl çalışır?

- Bir trace dosyasına geçtiğinizde panel kendiliğinden açılır ve analiz başlar. Uzantı `.trace` ya da `.txt`
  olabilir; `.txt` dosyalarında içeriğe bakılır, sıradan metin dosyalarında panel açılmaz.
- Trace olmayan bir dosyaya geçince panel gizlenir. Paneli menüden kendiniz açtıysanız açık kalır ve
  "Bu bir Troia trace dosyası değil" uyarısı görünür.
- Paneldeki bir satıra tıkladığınızda editör o satıra gider. Editörde imleci başka bir satıra taşıdığınızda da
  panel o satırı seçer.
- Ayrıntı başlığındaki **Editörde göster** düğmesi satıra gider ve imleci editöre taşır.
- Çağrı ağacında bir satır seçtiğinizde, satırın ait olduğu metot editörde açık sarıyla, satırın kendisi koyu sarıyla
  işaretlenir. Bu zemin renklerini menüden ya da Çağrı ağacındaki **Blok rengi** kutusundan kapatabilirsiniz;
  kapalıyken metodun yalnızca soldaki çubuğu ve metot adının çerçevesi kalır.
- Editörde bir kelimeye çift tıkladığınızda, kelime dosyada geçtiği her yerde pembeyle işaretlenir. Aynı kelimeye
  yeniden çift tıklarsanız işaret kalkar.
- Satır ayrıntısı alt bölmede açılır. Ayrıntı başlığındaki düğmeyle ya da menüden sağdan kayan panele geçebilirsiniz;
  tercihiniz hatırlanır.
- Trace dosyalarının metni editörde renklenir: deyimler, SQL, metot adları, değerler ve süreler ayrı renklerle,
  hata satırları kırmızı zeminle, uyarılar dalgalı alt çizgiyle gösterilir. Panel kapalıyken de çalışır; menüden
  kapatabilirsiniz.
- Dosyayı kaydettiğinizde analiz yenilenir.
- Notepad++ koyu temadaysa panel de koyu açılır.
- Bir trace dosyasında paneli kapatırsanız, Notepad++'ı kapatana kadar o dosyada panel kendiliğinden açılmaz.
  Menüden yeniden açabilirsiniz.
- Notepad++'ı panel açıkken kapatırsanız, bir sonraki açılışta panel yine açık gelir.

## Menü

**Eklentiler > TroiaLens**

| Komut | Kısayol | Ne yapar? |
|---|---|---|
| TroiaLens paneli | Ctrl+Alt+Shift+T | Paneli açar veya kapatır |
| Aktif dosyayı analiz et | Ctrl+Alt+Shift+Y | Paneli açar ve açık dosyayı yeniden analiz eder. Uzantısı farklı olsa da içeriği trace ise çalışır. |
| Trace dosyalarında paneli otomatik aç | Ctrl+Alt+Shift+O | Trace dosyasına geçince panelin kendiliğinden açılıp açılmayacağını belirler |
| Sekme değişince yeniden analiz et | Ctrl+Alt+Shift+E | Panel açıkken her sekme değişiminde analizi yeniler |
| Satır ayrıntısını sağda kayan panelde aç | Ctrl+Alt+Shift+D | Satır ayrıntısını alt bölme yerine sağdan kayan panelde gösterir |
| Seçili satırın kod bloğunu renklendir | Ctrl+Alt+Shift+V | Panelde seçilen satırın metodunu ve kendisini editörde sarı zeminle gösterir. Çağrı ağacındaki **Blok rengi** kutusuyla aynıdır. |
| Üst öğe görünümü | Ctrl+Alt+Shift+0 … 3 | Çağrı ağacında satırın bağlı olduğu üst çağrının nasıl gösterileceği: **Kapalı** (0), **Yapışkan başlıklar** (1), **Tıklanır kılavuz çizgileri** (2), **Üste git, geri dön** (3). Çağrı ağacındaki **Üst öğe** listesiyle aynıdır. |
| Sözdizimi renkleri | Ctrl+Alt+Shift+S | Trace dosyalarının metnini editörde renklendirir; kapatınca renkler kalkar |
| Güncellemeleri denetle | | Releases sayfasında yeni sürüm olup olmadığına bakar; varsa indirip kurar |
| Güncellemeleri her gün denetle | Ctrl+Alt+Shift+G | Notepad++ açılınca günde bir kez yeni sürüme bakılıp bakılmayacağını belirler |
| Klavye kısayolları… | | TroiaLens komutlarını kısayollarıyla listeler. **Değiştir…** seçili komutu Notepad++'ın Kısayol Düzenleyicisi'nde açar. |
| Hakkında | | Sürümü, trace uzantılarını ve ayar dosyasının yerini gösterir |

Kısayollar panel odaktayken de çalışır. Değiştirmek ya da boş olanlara kısayol atamak için **Klavye kısayolları…**
penceresini ya da **Ayarlar > Kısayol Düzenleyici > Eklenti Komutları** sekmesini kullanın. Değişiklik hemen
geçerli olur; Notepad++ onu kendi `shortcuts.xml` dosyasına kaydeder. Eklenti kısayollarını çalışırken yalnızca
Notepad++'ın Kısayol Düzenleyicisi değiştirebilir; dosyayı elle düzenlemeyin.

Varsayılanlar Ctrl+Alt+Shift ile başlar: Notepad++ ve yaygın eklentiler bu tuşları kullanmaz. Türkçe klavyede
Ctrl+Alt, AltGr ile aynıdır (@, €, ₺ gibi karakterler); Shift eklenince çakışmaz.

## Ayarlar

Ayarlar Notepad++'ın ayar klasöründeki `plugins\config\TroiaLens.ini` dosyasında saklanır. Tam yolu menüdeki
**Hakkında** penceresinde görebilirsiniz. Çoğu ayarı menüden değiştirebilirsiniz. Elle yaptığınız değişiklikler
Notepad++ yeniden başlayınca geçerli olur.

| Ayar | Varsayılan | Açıklama |
|---|---|---|
| `AutoOpen` | `1` | Trace dosyasına geçince paneli kendiliğinden aç |
| `AnalyzeOnTabSwitch` | `1` | Panel açıkken her sekme değişiminde analizi yenile |
| `DetailSlideOver` | `0` | `1` ise satır ayrıntısı sağdan kayan panelde açılır |
| `RowHighlight` | `1` | Panelde seçilen satırın metodunu ve kendisini editörde sarı zeminle göster |
| `ParentView` | `sticky` | Çağrı ağacında üst çağrının gösterimi: `off`, `sticky` (yapışkan başlıklar), `guides` (tıklanır kılavuz çizgileri), `jump` (üste git, geri dön) |
| `SyntaxColors` | `1` | Trace dosyalarının metnini editörde renklendir |
| `Extensions` | `.trace` | Trace sayılan uzantılar; birden fazlaysa `;` ile ayrılır |
| `CheckUpdates` | `1` | Notepad++ açılınca günde bir kez yeni sürüme bak. Kapatmak için `0` yapın ya da menüdeki işareti kaldırın |
| `SkippedVersion` | | "Bu sürümü atla" dediğiniz sürüm; kendiliğinden yeniden sorulmaz |

Uzantısı listede olmasa bile `TROIA CODE EXECUTION TRACE` satırıyla başlayan ya da `▎` karakteri içeren dosyalar
da trace olarak tanınır. Canias'ın `.txt` olarak kaydettiği trace dosyaları bu sayede kendiliğinden açılır.
`Extensions` ayarına `.txt` eklemeyin; eklerseniz panel her `.txt` dosyasında açılır.

Geliştiriciler için: `DevTools=1` ayarıyla eklenti menüsüne **Geliştirici araçları** komutu eklenir. Panelde F12 de
çalışır, ama Notepad++ bu tuşu kendi kısayolu için alabilir.

## Gereksinimler

- Notepad++ 64 bit, 8.4 veya daha yeni bir sürüm
- Microsoft Edge WebView2 Runtime (Windows 11'de hazır gelir)
- Derlemek için: .NET SDK, .NET Framework 4.8 geliştirici paketi, `ildasm.exe` ve Node.js 22.12 veya daha yeni bir sürüm

## Kurulum

Eklentiyi üç yoldan kurabilirsiniz: kurulum programıyla, hazır zip dosyasıyla ya da kaynak koddan derleyerek.

**Kurulum programıyla (önerilen):**

1. [Releases](https://github.com/ufukt51/TroiaLensPlugin/releases/latest) sayfasından `TroiaLens-Setup-<sürüm>.exe` dosyasını indirin.
2. Notepad++'ı kapatın ve dosyayı çalıştırın. Yönetici izni istenir.

Kurulum programı şunları yapar:

- Notepad++'ın kurulu olduğu klasörü kendisi bulur ve eklentiyi `plugins\TroiaLens` klasörüne kurar.
- Eksikse .NET Framework 4.8 ve WebView2 Runtime'ı Microsoft'tan indirip kurar.
- Node.js kurulu değilse en son sürümü nodejs.org'dan indirir, dosyayı doğrular ve kurar. Eklenti Node.js olmadan da
  çalışır; bu adım başarısız olursa kurulum devam eder.

Eklentiyi **Ayarlar > Uygulamalar** bölümünden "TroiaLens" adıyla kaldırabilirsiniz. .NET, WebView2 ve Node.js kaldırılmaz.

Komut satırı seçenekleri:

| Seçenek | Ne yapar? |
|---|---|
| `/quiet` | Pencere açmadan kurar |
| `NPPDIR="D:\Apps\Notepad++"` | Notepad++ bulunamazsa ya da taşınabilir sürüm kullanıyorsanız klasörü belirtir |
| `InstallNode=0` | Node.js kurmaz |

Yalnızca eklentiyi kurmak isterseniz `TroiaLens-<sürüm>-x64.msi` dosyasını kullanabilirsiniz. MSI, Notepad++ klasörünü
kendisi bulur ama .NET, WebView2 ve Node.js kurmaz. Örnek: `msiexec /i TroiaLens-0.1.3-x64.msi /qn NPPDIR="D:\Apps\Npp"`

Kurulum programı imzalı olmadığı için Windows SmartScreen uyarı verebilir. **Ek bilgi > Yine de çalıştır** ile devam edin.

**Güncelleme:**

0.1.4 ve sonraki sürümler yeni sürümü kendileri bulur. Notepad++ açılınca günde bir kez Releases sayfasına bakılır;
yeni sürüm varsa sorulur. Kabul ederseniz kurulum programı indirilir ve doğrulanır. Notepad++ kapanınca kurulur,
sonra Notepad++ yeniden açılır. Elle denetlemek için: **Eklentiler > TroiaLens > Güncellemeleri denetle**.

0.1.0–0.1.3 sürümlerinde bu özellik yok. Bir kez elle güncelleyin: Notepad++'ı kapatın ve en son
`TroiaLens-Setup-<sürüm>.exe` dosyasını çalıştırın. Eski sürüm kendiliğinden kaldırılır; eklentiyi zip ile
kurduysanız dosyaların üzerine yazılır. Sonraki sürümler kendiliğinden gelir.

**Hazır zip ile:**

1. [Releases](https://github.com/ufukt51/TroiaLensPlugin/releases/latest) sayfasından `TroiaLens-<sürüm>-x64.zip` dosyasını indirin.
2. Notepad++'ı kapatın.
3. Notepad++ kurulum klasöründeki `plugins` klasöründe `TroiaLens` adında bir klasör açın
   (genellikle `C:\Program Files\Notepad++\plugins\TroiaLens`) ve zip'in içeriğini bu klasöre çıkarın.
   `TroiaLens.dll` doğrudan bu klasörde olmalı. (0.1.3 ve önceki zip'lerde içerik bir `TroiaLens` klasöründeydi;
   o klasörü `plugins` klasörüne kopyalayın.)
4. Notepad++'ı yeniden açın.

**"Uzak kaynaktan yüklenemiyor" hatası alırsanız:**

Windows, internetten indirilen dosyaları engeller. Bu yüzden Notepad++ açılırken "Bu bütünleştirilmiş kod ... uzak
kaynaktan yüklenemiyor" benzeri bir hata çıkabilir. Kurulum programı ve MSI ile kurulumda bu sorun olmaz.
Zip ile kurduysanız engeli şöyle kaldırın:

1. Notepad++'ı kapatın.
2. Başlat menüsünde **PowerShell** yazın, sağ tıklayıp **Yönetici olarak çalıştır**'ı seçin. (Komut İstemi (cmd) ile
   çalışmaz.)
3. Şu komutu yapıştırıp Enter'a basın:

   ```powershell
   Get-ChildItem "C:\Program Files\Notepad++\plugins\TroiaLens" -Recurse | Unblock-File
   ```

   Notepad++ başka bir klasördeyse tırnak içindeki yolu değiştirin.
4. Notepad++'ı açın.

Bu hatayı baştan önlemek için zip dosyasını açmadan önce engelini kaldırabilirsiniz: zip dosyasına sağ tıklayın,
**Özellikler**'i seçin, alttaki **Engellemeyi kaldır** kutusunu işaretleyip **Tamam**'a tıklayın.

**Kaynaktan derlemek için:**

```powershell
git clone --recurse-submodules https://github.com/ufukt51/TroiaLensPlugin.git
cd TroiaLensPlugin
.\build.ps1      # web arayüzünü ve eklentiyi derler
.\install.ps1    # eklentiyi Notepad++'a kurar
```

- Kurmadan önce Notepad++'ı kapatın. Gerekirse yönetici izni istenir.
- Notepad++ başka bir klasördeyse (örneğin taşınabilir sürüm): `.\install.ps1 -NppDir D:\Apps\Npp`
- Derlenen eklenti `out\TroiaLens\` klasörüne yazılır. Zip, MSI ve `TroiaLens-Setup-<sürüm>.exe` dosyaları da `out\`
  klasörüne yazılır. Kurulum paketleri [WiX Toolset](https://wixtoolset.org) ile derlenir; NuGet'ten kendiliğinden
  iner. Kurulum paketlerini atlamak için: `.\build.ps1 -SkipInstaller`
- Kurulum paketlerinin kaynakları `installer\` klasöründedir: `Package` (MSI), `Bundle` (kurulum programı) ve
  `NodeSetup` (Node.js'i indirip kuran yardımcı program).
- Web arayüzünde yaptığınız değişikliklerin Notepad++'ta görünmesi için `build.ps1` ve `install.ps1` komutlarını
  yeniden çalıştırın. Yalnızca `npm run build` yetmez.
- Web arayüzü değişmediyse `.\build.ps1 -SkipWeb` ile yalnızca eklentiyi derleyebilirsiniz.

## Yeni sürüm yayınlama

Dosyalar Releases sayfasına VS Code'tan yüklenir. İlk kullanımdan önce [GitHub CLI](https://cli.github.com)
kurun ve `gh auth login` ile oturum açın.

1. Sürümü artıracaksanız `src\TroiaLens\TroiaLens.csproj` içindeki `<Version>` değerini değiştirin.
2. Değişiklikleri commit edip push edin.
3. VS Code'ta **Ctrl+Shift+P > Tasks: Run Task > TroiaLens: Release yayınla** komutunu çalıştırın.

Görev eklentiyi derler; zip, MSI ve kurulum programını `v<sürüm>` adıyla Releases sayfasına yükler. Bu sürüm
daha önce yayınlandıysa onay ister; onaylarsanız eski dosyaların yerine yenilerini koyar.

## Web arayüzünü güncelleme

Web arayüzü bu repoya `web/` klasöründe alt modül (submodule) olarak bağlıdır. Bu klasör ayrı bir git reposudur
([ufukt51/TroiaLens](https://github.com/ufukt51/TroiaLens)) ve kendi geçmişi vardır.

Klonladıktan sonra `web/` klasörü boşsa:

```powershell
git submodule update --init
```

Web arayüzünün en son sürümünü almak için:

```powershell
git submodule update --remote web
git add web
git commit -m "web: güncelle"
```

Web arayüzünü doğrudan bu klasörde geliştirmek için:

```powershell
cd web
git switch main          # alt modül ilk açılışta bir dala bağlı değildir; önce main dalına geçin
# değişiklikleri yapın, npm test ile kontrol edin
git commit -am "..."
git push                 # web reposuna gönderir (herkese açık)
cd ..
git add web              # eklenti, web arayüzünün bu sürümünü kullansın
git commit -m "web: güncelle"
git push                 # eklenti reposuna gönderir (özel)
```

## Lisans

[MIT](LICENSE)
