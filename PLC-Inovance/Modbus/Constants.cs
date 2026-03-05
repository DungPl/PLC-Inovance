using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC_Inovance.Modbus
{
    public static class Constants
    {
        // =============================
        // MODBUS TCP DEFAULT
        // =============================

        public const int DefaultPort = 502;
        public const byte DefaultUnitId = 1;

        public const int DefaultTimeout = 3000; // ms
        public const int DefaultPollingInterval = 500; // ms

        // =============================
        // FUNCTION CODES
        // =============================

        public const byte FC_READ_COILS = 0x01;
        public const byte FC_READ_DISCRETE_INPUTS = 0x02;
        public const byte FC_READ_HOLDING_REGISTERS = 0x03;
        public const byte FC_READ_INPUT_REGISTERS = 0x04;

        public const byte FC_WRITE_SINGLE_COIL = 0x05;
        public const byte FC_WRITE_SINGLE_REGISTER = 0x06;
        public const byte FC_WRITE_MULTIPLE_COILS = 0x0F;
        public const byte FC_WRITE_MULTIPLE_REGISTERS = 0x10;

        // =============================
        // MODBUS ADDRESS BASE
        // =============================

        public const int COIL_BASE = 0;              // 00001
        public const int DISCRETE_INPUT_BASE = 10000; // 10001
        public const int INPUT_REGISTER_BASE = 30000; // 30001
        public const int HOLDING_REGISTER_BASE = 40000; // 40001

        // =============================
        // EASY521 / INOVANCE MAPPING
        // =============================

        // D register → Holding Register
        public const int D_BASE = 0;

        // R register (ví dụ offset)
        public const int R_BASE = 12288;

        // X input base (Inovance thường 63488)
        public const int X_BASE = 63488;

        // Y output base (tuỳ model)
        public const int Y_BASE = 64512;

        // M internal relay
        public const int M_BASE = 0;

        // =============================
        // LIMITS
        // =============================

        public const int MAX_READ_REGISTERS = 125;
        public const int MAX_READ_COILS = 2000;

        public const int MAX_WRITE_REGISTERS = 123;
        public const int MAX_WRITE_COILS = 1968;
    }
}
