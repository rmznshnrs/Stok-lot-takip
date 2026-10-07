namespace LotTakip.Entity;

public enum StokDuzeltmeTuru
{
    SayimFarki,
    Fire,
    Diger,
}

/// <summary>Admin'in açıklamalı stok düzeltmesi. Miktar + (artış) veya - (azalış).</summary>
public class StokDuzeltme : IDenetimli
{
    public int Id { get; set; }
    public int StokLotuId { get; set; }
    public StokLotu? StokLotu { get; set; }
    public int Miktar { get; set; }
    public StokDuzeltmeTuru Tur { get; set; }
    public required string Aciklama { get; set; }

    public int OlusturanKullaniciId { get; set; }
    public Kullanici? OlusturanKullanici { get; set; }
    public DateTimeOffset OlusturmaZamani { get; set; }
}
