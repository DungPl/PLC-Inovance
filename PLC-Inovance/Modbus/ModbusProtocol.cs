using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC_Inovance.Modbus
{
   static class ModbusProtocol
    {
        private static ushort _transactionId = 0;

        #region ===== BUILD REQUEST =====

        public static byte[] BuildReadRequest(byte unitId, byte functionCode, ushort startAddress, ushort quantity)
        {
            ushort transId = _transactionId++;

            byte[] frame = new byte[12];

            // MBAP Header
            frame[0] = (byte)(transId >> 8);
            frame[1] = (byte)(transId & 0xFF);

            frame[2] = 0x00; // Protocol ID
            frame[3] = 0x00;

            frame[4] = 0x00;
            frame[5] = 0x06; // Length = 6 bytes after this

            frame[6] = unitId;

            // PDU
            frame[7] = functionCode;
            frame[8] = (byte)(startAddress >> 8);
            frame[9] = (byte)(startAddress & 0xFF);
            frame[10] = (byte)(quantity >> 8);
            frame[11] = (byte)(quantity & 0xFF);

            return frame;
        }

        public static byte[] BuildWriteSingleCoil(byte unitId, ushort address, bool value)
        {
            ushort transId = _transactionId++;

            byte[] frame = new byte[12];

            frame[0] = (byte)(transId >> 8);
            frame[1] = (byte)(transId & 0xFF);

            frame[2] = 0;
            frame[3] = 0;

            frame[4] = 0;
            frame[5] = 6;

            frame[6] = unitId;
            frame[7] = 0x05; // Function code

            frame[8] = (byte)(address >> 8);
            frame[9] = (byte)(address & 0xFF);

            ushort coilValue = value ? (ushort)0xFF00 : (ushort)0x0000;

            frame[10] = (byte)(coilValue >> 8);
            frame[11] = (byte)(coilValue & 0xFF);

            return frame;
        }

        public static byte[] BuildWriteSingleRegister(byte unitId, ushort address, ushort value)
        {
            ushort transId = _transactionId++;

            byte[] frame = new byte[12];

            frame[0] = (byte)(transId >> 8);
            frame[1] = (byte)(transId & 0xFF);
            frame[2] = 0;
            frame[3] = 0;
            frame[4] = 0;
            frame[5] = 6;

            frame[6] = unitId;
            frame[7] = 0x06;

            frame[8] = (byte)(address >> 8);
            frame[9] = (byte)(address & 0xFF);

            frame[10] = (byte)(value >> 8);
            frame[11] = (byte)(value & 0xFF);

            return frame;
        }

        public static byte[] BuildWriteMultipleCoils(byte unitId, ushort startAddress, bool[] values)
        {
            ushort transId = _transactionId++;

            ushort quantity = (ushort)values.Length;
            ushort byteCount = (ushort)((quantity + 7) / 8);

            byte[] frame = new byte[13 + byteCount];

            frame[0] = (byte)(transId >> 8);
            frame[1] = (byte)(transId & 0xFF);
            frame[2] = 0;
            frame[3] = 0;

            ushort length = (ushort)(7 + byteCount);
            frame[4] = (byte)(length >> 8);
            frame[5] = (byte)(length & 0xFF);

            frame[6] = unitId;
            frame[7] = 0x0F;

            frame[8] = (byte)(startAddress >> 8);
            frame[9] = (byte)(startAddress & 0xFF);

            frame[10] = (byte)(quantity >> 8);
            frame[11] = (byte)(quantity & 0xFF);

            frame[12] = (byte)byteCount;

            for (int i = 0; i < quantity; i++)
            {
                if (values[i])
                {
                    frame[13 + (i / 8)] |= (byte)(1 << (i % 8));
                }
            }

            return frame;
        }


        public static byte[] BuildWriteMultipleRegisters(byte unitId, ushort startAddress, ushort[] values)
        {
            ushort transId = _transactionId++;

            ushort quantity = (ushort)values.Length;
            ushort byteCount = (ushort)(quantity * 2);

            byte[] frame = new byte[13 + byteCount];

            frame[0] = (byte)(transId >> 8);
            frame[1] = (byte)(transId & 0xFF);
            frame[2] = 0;
            frame[3] = 0;

            ushort length = (ushort)(7 + byteCount);
            frame[4] = (byte)(length >> 8);
            frame[5] = (byte)(length & 0xFF);

            frame[6] = unitId;
            frame[7] = 0x10;

            frame[8] = (byte)(startAddress >> 8);
            frame[9] = (byte)(startAddress & 0xFF);

            frame[10] = (byte)(quantity >> 8);
            frame[11] = (byte)(quantity & 0xFF);

            frame[12] = (byte)byteCount;

            int index = 13;
            foreach (ushort val in values)
            {
                frame[index++] = (byte)(val >> 8);
                frame[index++] = (byte)(val & 0xFF);
            }

            return frame;
        }
        #endregion

        #region ===== PARSE RESPONSE =====

        public static byte[] ParseReadResponse(byte[] response)
        {
            if (response == null || response.Length < 9)
                return null;

            byte functionCode = response[7];

            if ((functionCode & 0x80) != 0)
                throw new Exception($"Modbus Error Code: {response[8]}");

            byte byteCount = response[8];

            byte[] data = new byte[byteCount];
            Array.Copy(response, 9, data, 0, byteCount);

            return data;
        }

        public static short[] ParseRegisterResponse(byte[] response)
        {
            byte[] raw = ParseReadResponse(response);

            if (raw == null)
                return null;

            int count = raw.Length / 2;
            short[] result = new short[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = (short)((raw[i * 2] << 8) | raw[i * 2 + 1]);
            }

            return result;
        }

        #endregion
    }
}
