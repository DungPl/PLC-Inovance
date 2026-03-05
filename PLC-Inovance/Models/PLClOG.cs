using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC_Inovance.Models
{
    public class PLClOG
    {
        public DateTime TimeStamp { get; set; }
        public string PLC_IP { get; set; }

        public bool X0 { get; set; }
        public bool X1 { get; set; }
        public bool X2 { get; set; }
        public bool X3 { get; set; }
        public bool X4 { get; set; }
        public bool X5 { get; set; }
        public bool X6 { get; set; }
        public bool X7 { get; set; }

        public int? D0 { get; set; }
        public int? D1 { get; set; }
        public int? D2 { get; set; }
        public int? D3 { get; set; }
        public int? D4 { get; set; }
        public int? D5 { get; set; }
        public int? D6 { get; set; }
        public int? D7 { get; set; }

        public bool IsRunning { get; set; }
        public bool HasAlarm { get; set; }
    }
}
