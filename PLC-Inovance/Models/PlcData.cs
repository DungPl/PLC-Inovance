using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC_Inovance.Models
{
    public class PlcData
    {
        public DateTime TimeStamp { get; set; } = DateTime.Now;

        public string PLC_IP { get; set; } = string.Empty;       // hoặc DeviceName nếu bạn thích
        // public string DeviceName { get; set; } = string.Empty;  // tùy chọn thay thế hoặc bổ sung

        public bool[] X { get; set; } = new bool[8];             // X0..X7
        public short[] D { get; set; } = new short[8];           // D0..D7

        public float[] D_Float { get; set; } = new float[4];
        public double[] D_Double { get; set; } = new double[2];
        public string D_String { get; set; } = "";

        // Thêm trạng thái tổng hợp
        public bool IsRunning { get; set; } = false;             // Running / Máy đang chạy
        public bool HasAlarm { get; set; } = false;              // Có báo động / lỗi
    }
}
