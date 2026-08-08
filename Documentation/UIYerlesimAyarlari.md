# UI yerleşimini Inspector'dan düzenleme

Ana yerleşim varlığı:

`Assets/Resources/SimulationUILayoutSettings.asset`

Project penceresinde bu varlığı seçtiğinizde aşağıdaki öğelerin her birini ayrı ayrı düzenleyebilirsiniz:

- popülasyon yazısı;
- canlı ve yemek paneli düğmeleri;
- hız düğmeleri grubu, tek düğme boyutu ve düğmeler arası boşluk;
- canlı istatistik paneli;
- canlı ve yemek oluşturma panelleri;
- geçen süre / sürüm yazısı;
- ENLER düğmesi ve ENLER paneli.

Her öğede şu alanlar vardır:

- **Ekran Sabitleme Noktası:** Öğenin hangi ekran kenarına veya merkeze bağlanacağını belirler.
- **Konum:** Seçilen sabitleme noktasına göre X/Y mesafesidir.
- **Boyut:** Genişlik ve yüksekliktir.
- **Ölçek:** Öğenin ve altındaki içeriğin genel ölçeğidir.
- **Yerleşimi Uygula:** Kapatılırsa sistem o öğenin sahnedeki manuel RectTransform değerine dokunmaz.

Değişiklikler Edit Mode'da sahneye otomatik yansır. Gerekirse Inspector'ın altındaki **Açık Sahneye Şimdi Uygula** düğmesini kullanabilirsiniz. Play Mode sırasında yapılan ayar değişiklikleri de yaklaşık yarım saniye içinde görünür.

## Dar ekran davranışı

`Kompakt Düzen En-Boy Eşiği` değerinin altındaki ekranlarda hız düğmeleri ilk satırda kalır; popülasyon ve oluşturma düğmeleri ikinci satıra, ENLER düğmesi üçüncü satıra geçer. İkinci ve üçüncü satır mesafeleri aynı ayar varlığından değiştirilebilir.

Varsayılan yerleşimde süre/sürüm yazısı alt ortadadır. Böylece sol taraftaki canlı istatistik paneli ve sağ taraftaki oluşturma panelleriyle çakışmaz.
