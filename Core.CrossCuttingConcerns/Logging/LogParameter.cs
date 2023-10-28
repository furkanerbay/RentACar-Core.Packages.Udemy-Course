using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.CrossCuttingConcerns.Logging;

public class LogParameter
{
    public string Name { get; set; } //Parametrenin ismi
    public object Value { get; set; } //Parametrenin değeri
    public string Type { get; set; } //Tipi
    public LogParameter()
    {
        Name = string.Empty;
        Value = string.Empty;
        Type = string.Empty;
    }
    public LogParameter(string name, object value, string type)
    {
        Name = name;
        Value = value;
        Type = type;
    }
}
