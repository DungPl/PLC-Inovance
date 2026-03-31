using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC_Inovance.Models
{
    public enum ModbusDataType
    {
        Int16,
        UInt16,
        Int32,
        UInt32,
        Float,      // 2 registers
        Double,     // 4 registers
        String      // biến số register
    }
}
