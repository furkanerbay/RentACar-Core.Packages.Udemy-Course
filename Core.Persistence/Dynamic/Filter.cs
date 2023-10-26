using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Persistence.Dynamic;

public class Filter
{
    public string Field { get; set; } //Alan Yakıt gibi
    public string? Value { get; set; } //Değeri
    public string? Operator { get; set; } //Bu field üzerinde esittir olabilir icinde gecene gore olabilir vs. Sayısal değerse büyük kücük vs.
    public string? Logic { get; set; } //Şu şartı sağlayan bu şartı sağlayan
    public IEnumerable<Filter>? Filters { get; set; } //Bir filtreye başka filtreler uygulayabilmek istiyorum.
    public Filter()
    {
        Field = string.Empty;
        Operator = string.Empty;
    }
    public Filter(string field, string @operator) //Hali hazırda var olan bir keyword ama bu benım kendı degıskenım dıyebılmek ıcın basına @ koyuyoruz
    {
        Field = field;
        Operator = @operator;
    }
}