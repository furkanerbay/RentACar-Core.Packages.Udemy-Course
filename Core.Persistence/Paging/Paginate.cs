using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Persistence.Paging;

public class Paginate<T>
{
    public Paginate()
    {
        Items = Array.Empty<T>(); //İlk etapta boş bir eleman. Nesnelerimizi empty verdik.
    }
    public int Size { get; set; } //Sayfamızda kaç tane data var
    public int Index { get; set; } //Biz hangi sayfadayız
    public int Count { get; set; } //Toplam kayıt sayısı
    public int Pages { get; set; } //Toplam kaç sayfa var?
    public IList<T> Items { get; set; } //Datamız ne
    public bool HasPrevious => Index > 0; //Onceki sayfa var mı
    public bool HasNext => Index + 1 < Pages;  //Sonraki sayfa var mı
}