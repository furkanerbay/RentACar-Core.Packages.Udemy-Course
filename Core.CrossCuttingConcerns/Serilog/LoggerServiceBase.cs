using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.CrossCuttingConcerns.Serilog;
//Çeşitli loggerlar oldugu için abstract olusturduk.
public abstract class LoggerServiceBase
{
    protected ILogger Logger; //Protected abstract sadece ınherıtence ıle kullanılır ve bunu sadece onu ınherıt eden
    //yer gorsun

    public LoggerServiceBase()
    {
        Logger = null;
    }
    public LoggerServiceBase(ILogger logger)
    {
        Logger = logger;
    }
    public void Verbose(string message) => Logger.Verbose(message); //Detaylı loglama

    public void Fatal(string message) => Logger.Fatal(message); //Çok ölümcül bir hata

    public void Info(string message) => Logger.Information(message); //Bilgi

    public void Warn(string message) => Logger.Warning(message); //Uyarı

    public void Debug(string message) => Logger.Debug(message); //Debug

    public void Error(string message) => Logger.Error(message); //Normal Hata
}
