using Core.Persistence.Paging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Application.Responses;

public class GetListResponse<T> : BasePageableModel
{
    private IList<T> _items;

    public IList<T> Items
    {
        get => _items ??= new List<T>(); //Item varsa onu don yoksa yenı liste olustur.
        set => _items = value;
    }


}