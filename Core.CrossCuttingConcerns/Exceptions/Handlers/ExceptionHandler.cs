using Core.CrossCuttingConcerns.Exceptions.Types;
using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.CrossCuttingConcerns.Exceptions.Handlers;

//Hataları handle edecek yer ama implementasyon burada olmayacak.
//Implementasyon bunu implemente eden yerde olacak o yuzden burası abstract.
public abstract class ExceptionHandler
{
    public Task HandleExceptionAsync(Exception exception) => //Bana bir exception ver bende onu handle edeyim.
        exception switch
        {
            BusinessException businessException => HandleException(businessException),//BusinessException ise buraya
            ValidationException validationException => HandleException(validationException),//ValidaitonException ise buraya
            _ => HandleException(exception) //Birşey verilmez ise klasik handleException yolla. Bunların dısında bir hata ise
        };
    //Farklı ortamlar bunu implemente edecek. Bunuda sadece onu inherit eden class implement etsin diyorum.
    protected abstract Task HandleException(BusinessException businessException);
    protected abstract Task HandleException(ValidationException validationException);
    protected abstract Task HandleException(Exception exception);

    //Bu sınıfı implemente eden sınıf bunları da kullanmak zorunda olsun.
}
