namespace EntityLayer.Concrete;

public enum StokDuzeltmeTuru
{
    SayimFarki,
    Fire,
    Diger,
    /// <summary>Hatalı çıkan parçalar: HataliAdet kaydedilir; stoktan düşülürse Miktar = -HataliAdet, düşülmezse 0.</summary>
    Hatali,
}

/// <summary>Admin'in açıklamalı stok düzeltmesi. Miktar + (artış) veya - (azalış); hatalı kaydında 0 olabilir.</summary>
public class StokDuzeltme : IDenetimli
{
    public int Id { get; set; }
    public int StokLotuId { get; set; }
    public StokLotu? StokLotu { get; set; }
    public int Miktar { get; set; }
    public int HataliAdet { get; set; }
    public StokDuzeltmeTuru Tur { get; set; }
    public required string Aciklama { get; set; }

    public int OlusturanKullaniciId { get; set; }
    public Kullanici? OlusturanKullanici { get; set; }
    public DateTimeOffset OlusturmaZamani { get; set; }
}
