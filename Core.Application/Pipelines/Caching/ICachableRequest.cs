using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Application.Pipelines.Caching;

public interface ICachableRequest
{
    string CacheKey { get; } // Cache Anahtarı -> Her requesti bir cache anahtarına baglayacagız.
    bool BypassCache { get;  }//Development ihtiyacları vs. gereği hizlica veya test calismalari geregi cache'i bypass etmek istiyorum zaman zaman
    string? CacheGroupKey { get; }
    TimeSpan? SlidingExpiration { get; } //Bütün requestlerde slidingExpiration'a default süre vermek istiyorum bunları genellıkle appsetings de tutarız.Onun ıcın class olusturacagız.
}
