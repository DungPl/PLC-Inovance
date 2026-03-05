using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC_Inovance.Models
{
    public enum ElemType
    {
        Y = 0,   // Output
        X = 1,   // Input
        S = 2,   // Step Relay
        M = 3,   // Auxiliary Relay
        B = 4,   // Link Relay (H5U/EASY)
        D = 5,   // Data Register
        R = 6,   // File Register
        TB = 7,  // Timer Bit (H3U)
        TW = 8,  // Timer Word (H3U)
        CB = 9,  // Counter Bit (H3U)
        CW = 10, // Counter Word (H3U)
        CW2 = 11,// Counter Double Word (H3U)
        SM = 12, // Special Auxiliary Relay (H3U)
        SD = 13, // Special Data Register (H3U)
        QX = 14, // Bit (AM600)
        MW = 15  // Word (AM600)
    }
   

    internal enum AddressType
    {
        Bit = 0,
        Word = 2,
        DWord = 4
    }

    internal enum ErrorCode
    {
        ER_SUCCESS = 0,
        ER_READ_WRITE_FAIL = 0,
        ER_READ_WRITE_SUCCEED = 1,
        ER_NOT_CONNECT = 2,
        ER_ELEM_TYPE_WRONG = 3,
        ER_ELEM_ADDR_OVER = 4,
        ER_ELEM_COUNT_OVER = 5,
        ER_COMM_EXCEPT = 6
    }

    internal enum ModbusCmd : byte
    {
        ModbusCmd_Read_Coil_01 = 0x01,//0000 0001 Đọc trạng thái BẬT/TẮT của các đầu ra kỹ thuật số (Coils).
        ModbusCmd_Read_Coil_02 = 0x02,// 0000 0010 Đọc trạng thái BẬT/TẮT của các đầu vào kỹ thuật số (Discrete Inputs).
        ModbusCmd_Read_Regs_03 = 0x03,// 0000 0011 Đọc giá trị từ các thanh ghi giữ (thường dùng cho dữ liệu analog).
        ModbusCmd_Read_Regs_04 = 0x04,// 0000 0100 Đọc giá trị từ các thanh ghi đầu vào.
        ModbusCmd_Write_Coil = 0x05,// 0000 0101 Ghi vào một cuộn dây (0 hoặc 1).
        ModbusCmd_Write_Regs = 0x06,// 0000 0110 Ghi vào một thanh ghi giữ.
        ModbusCmd_Write_MutlCoils = 0x0F,// 0000 1111 Ghi vào nhiều cuộn dây cùng lúc.
        ModbusCmd_Write_MutlRegs = 0x10// 0001 0000 Ghi vào nhiều thanh ghi cùng lúc.
    }

    internal static class Constants
    {
        public const int MODBUSTCP_RD_COIL_MAX = 1968;
        public const int MODBUSTCP_WR_COIL_MAX = 1936;
        public const int MODBUSTCP_RD_REG_MAX = 123;
        public const int MODBUSTCP_WR_REG_MAX = 121;
    }
}
