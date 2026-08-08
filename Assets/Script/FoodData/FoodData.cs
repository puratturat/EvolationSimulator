using UnityEngine;

// Yiyeceğin türünü seçebilmemiz için bir kategori (Enum) oluşturuyoruz
// 🌟 YENİ: PoisonousPlant eklendi!
public enum FoodType { Plant, Meat, PoisonousPlant } 

[CreateAssetMenu(fileName = "NewFoodData", menuName = "Evolution/Food Data")]
public class FoodData : ScriptableObject
{
    [Header("Temel Özellikler")]
    public string foodName = "İsimsiz Yiyecek";
    public FoodType foodType; // Bu yiyecek Ot mu, Et mi, yoksa Zehirli Ot mu?

    [Header("Enerji Değeri")]
    public float baseEnergyRestore = 20f; // Yenildiğinde vereceği TEMEL enerji miktarı

    [Header("Bozulma Ayarları")]
    // Yiyeceğin sahnede kalabileceği rastgele sürenin sınırları (Saniye cinsinden)
    public float minSpoilTime = 10f; 
    public float maxSpoilTime = 20f;

    // 🌟 YENİ EKLENEN KISIM: ZEHİR AYARLARI 🌟
    [Header("Zehir Ayarları")]
    public float poisonDamage = 0f; // Sadece zehirli yiyeceklerde 0'dan büyük olacak. Normal ot/et için 0 kalacak.
}