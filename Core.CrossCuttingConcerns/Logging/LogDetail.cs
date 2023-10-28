using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.CrossCuttingConcerns.Logging;
//Log detayı olusturmak hangi method calıstı?
public class LogDetail
{
    public string FullName { get; set; } //Aynı isimde methodda olur bu sebeple namespace ile classıda dahil ederek ismi versin
    public string MethodName { get; set; }//Çalışacak method ismi
    public string User { get; set; } //Kim çağırdı
    public List<LogParameter> Parameters { get; set; } //Methodun bir sürü parametresi olabilir.
    public LogDetail()
    {
        FullName = string.Empty;
        MethodName = string.Empty;
        User = string.Empty;
        Parameters = new List<LogParameter>();
    }
    public LogDetail(string fullName, string methodName, string user, List<LogParameter> parameters)
    {
        FullName = fullName;
        MethodName = methodName;
        User = user;
        Parameters = parameters;
    }
}
