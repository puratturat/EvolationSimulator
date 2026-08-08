# Simülasyon günlükleri

Oyun her çalıştığında `Application.persistentDataPath/SimulationLogs` altında yeni bir `.jsonl` dosyası açar. Windows Editor için varsayılan konum genellikle şudur:

`%USERPROFILE%/AppData/LocalLow/DefaultCompany/EvolationSimulationV2/SimulationLogs`

Oyundaki **ENLER** panelinin altındaki **LOG KLASÖRÜNÜ AÇ** düğmesi doğru klasörü doğrudan açar.

## Neden JSON Lines?

Her satır bağımsız bir JSON kaydıdır. Uzun bir çalıştırma yarıda kesilse bile önceki satırlar okunabilir kalır; rapor hazırlarken dosyanın tamamını belleğe yüklemek gerekmez.

Kayıt türleri:

- `session_start` ve `session_end`: oturum sınırları, UTC zamanı, Unity ve oyun sürümü.
- `snapshot`: varsayılan olarak her 60 simülasyon saniyesindeki ekosistem özeti.
- `milestone`: ilk predatör ve ilk zehir tüketicisi gibi seyrek evrim eşikleri.

Her `snapshot` şunları içerir:

- popülasyon ve yaşayan bitki sayısı;
- o zaman aralığındaki ve oturum toplamındaki doğum, ölüm, saldırı, av, beslenme ve sindirilen enerji sayıları;
- açlık, zehir ve avlanma kaynaklı ölüm ayrımı;
- ortalama nesil, yaş, et/zehir isteği ve sindirim/direnç değerleri;
- otçul, hepçil, leşçil, predatör, zehir tüketicisi ve ara-form sayıları;
- öne çıkan canlıların kalıcı gözlem kimliği, adı, nesli ve rekor puanı.

## Boyut ve performans sınırları

- Tek tek her saldırı diske yazılmaz; olaylar RAM'de küçük sayaçlarda toplanıp periyodik özete dönüştürülür.
- Disk tamponu 5 gerçek saniyede bir veya 64 KB olduğunda boşaltılır.
- Dosya 4 MB olduğunda yeni parçaya geçilir.
- En yeni 12 dosya tutulur; böylece günlük klasörü yaklaşık 48 MB ile sınırlanır.
- Panel kapalıyken rekor taraması yapılmaz. Açıkken en fazla saniyede bir yaşayan canlılar taranır.

Bu yapı, 20 saatlik bir testin zaman serisini korurken oyun döngüsünü olay başına dosya yazımıyla yavaşlatmaz.
