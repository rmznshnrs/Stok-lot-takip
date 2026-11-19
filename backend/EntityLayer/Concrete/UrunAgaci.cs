namespace EntityLayer.Concrete;

public class UrunAgaci
{
    public int UrunId { get; set; }
    public Urun? Urun { get; set; }
    public int ParcaId { get; set; }
    public Parca? Parca { get; set; }
    public int Adet { get; set; }
}
