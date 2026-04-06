using PLC_Inovance.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC_Inovance.Utils
{
    internal class ModbusHelper
    {

        // ==================== READ ====================
        public static object ConvertRawToValue(int[] raw, int count, ModbusDataType dataType, int stringLength = 0)
        {
            if (raw == null || raw.Length == 0) return null;

            return dataType switch
            {
                ModbusDataType.Int16 => ConvertToInt16(raw, count),
                ModbusDataType.UInt16 => ConvertToUInt16(raw, count),
                ModbusDataType.Int32 => ConvertToInt32(raw, count),
                ModbusDataType.Float => ConvertToFloat(raw, count),   // thay đổi dễ dàng ở đây
                ModbusDataType.Double => ConvertToDouble(raw, count),
                ModbusDataType.String => ConvertToString(raw, stringLength),
                _ => throw new NotSupportedException($"ModbusDataType {dataType} chưa được hỗ trợ")
            };
        }
        // ==================== WRITE ====================
        public static int[] ConvertValueToRaw(object value, ModbusDataType dataType, int stringLength = 0)
        {
            return dataType switch
            {
                ModbusDataType.Int16 => ((short[])value).Select(v => (int)v).ToArray(),
                ModbusDataType.UInt16 => ((ushort[])value).Select(v => (int)v).ToArray(),
                ModbusDataType.Float => ConvertFloatToRawCDAB((float[])value),
                ModbusDataType.Double => throw new NotImplementedException("Double write - sẽ implement sau"),
                ModbusDataType.String => ConvertStringToRaw((string)value, stringLength),
                _ => throw new NotSupportedException($"ModbusDataType {dataType} chưa được hỗ trợ")
            };
        }

        // ==================== Các hàm convert cụ thể ====================

        private static short[] ConvertToInt16(int[] raw, int count)
            => raw.Take(count).Select(r => (short)r).ToArray();

        private static ushort[] ConvertToUInt16(int[] raw, int count)
            => raw.Take(count).Select(r => (ushort)r).ToArray();

        private static int[] ConvertToInt32(int[] raw, int count)
        {
            int[] result = new int[count];

            Console.WriteLine($"[Debug Int32] raw.Length={raw.Length}, count={count}, expected registers={count * 2}");

            for (int i = 0; i < count; i++)
            {
                int highIdx = i * 2;
                int lowIdx = i * 2 + 1;

                if (lowIdx < raw.Length)
                {
                    // Thử cả 2 cách phổ biến cho Inovance
                    // Cách 1: High word trước (ABCD)
                   result[i] = (raw[highIdx] << 16) | (raw[lowIdx] & 0xFFFF);

                    // Cách 2 (nếu cách 1 sai): Low word trước
                     //result[i] = (raw[lowIdx] << 16) | (raw[highIdx] & 0xFFFF);

                    Console.WriteLine($"  Int32[{i}] = raw[{highIdx}]={raw[highIdx]:5} | raw[{lowIdx}]={raw[lowIdx]:5} → {result[i]}");
                }
                else
                {
                    result[i] = 0;
                    Console.WriteLine($"  Int32[{i}] = out of range (index {lowIdx})");
                }
            }
            return result;
        }
        private static double [] ConvertToDouble(int[] raw, int count)
        {
            double[] result = new double[count];
            for (int i = 0; i < count; i++)
            {
                ulong lowDword = (uint)raw[i * 4 + 3] | ((uint)raw[i * 4 + 2] << 16);
                ulong highDword = (uint)raw[i * 4 + 1] | ((uint)raw[i * 4] << 16);
                byte[] bytes = [(byte)(lowDword & 0xFF), (byte)((lowDword >> 8) & 0xFF),
                            (byte)((lowDword >> 16) & 0xFF), (byte)((lowDword >> 24) & 0xFF),
                            (byte)(highDword & 0xFF), (byte)((highDword >> 8) & 0xFF),
                            (byte)((highDword >> 16) & 0xFF), (byte)((highDword >> 24) & 0xFF)];
                result[i] = BitConverter.ToDouble(bytes, 0);
            }
            return result;
        }

        // Float - CDAB (phổ biến nhất với PLC Việt Nam)
        private static float[] ConvertToFloatCDAB(int[] raw, int count)
        {
            float[] result = new float[count];
            for (int i = 0; i < count; i++)
            {
                ushort lowWord = (ushort)raw[i * 2 + 1];
                ushort highWord = (ushort)raw[i * 2];

                byte[] bytes = [(byte)(lowWord & 0xFF), (byte)(lowWord >> 8),
                            (byte)(highWord & 0xFF), (byte)(highWord >> 8)];

                result[i] = BitConverter.ToSingle(bytes, 0);
            }
            return result;
        }
        // Thay thế hàm cũ ConvertToFloatCDAB bằng một trong các hàm dưới đây

        private static float[] ConvertToFloat(int[] raw, int count)
        {
            float[] result = new float[count];

            for (int i = 0; i < count; i++)
            {
                ushort word1 = (ushort)raw[i * 2];     // register đầu tiên
                ushort word2 = (ushort)raw[i * 2 + 1]; // register thứ hai

                byte[] bytes = new byte[4];

                // === THỬ TỪNG BIẾN THỂ SAU (bắt đầu từ biến thể 1) ===

                // 1. ABCD (High word trước, Big-endian byte)
                //bytes[0] = (byte)(word1 >> 8);
                //bytes[1] = (byte)(word1 & 0xFF);
                //bytes[2] = (byte)(word2 >> 8);
                //bytes[3] = (byte)(word2 & 0xFF);

                // 2. CDAB (Low word trước) - hiện tại bạn đang dùng
                bytes[0] = (byte)(word2 & 0xFF);
                bytes[1] = (byte)(word2 >> 8);
                bytes[2] = (byte)(word1 & 0xFF);
                bytes[3] = (byte)(word1 >> 8);

                // 3. BADC (Byte swap trong word)
                // bytes[0] = (byte)(word1 & 0xFF);
                // bytes[1] = (byte)(word1 >> 8);
                // bytes[2] = (byte)(word2 & 0xFF);
                // bytes[3] = (byte)(word2 >> 8);

                // 4. DCBA (Full reverse)
                // bytes[0] = (byte)(word2 & 0xFF);
                // bytes[1] = (byte)(word2 >> 8);
                // bytes[2] = (byte)(word1 & 0xFF);
                // bytes[3] = (byte)(word1 >> 8);

                result[i] = BitConverter.ToSingle(bytes, 0);
            }
            return result;
        }
        private static int[] ConvertFloatToRawCDAB(float[] values)
        {
            int[] buffer = new int[values.Length * 2];
            for (int i = 0; i < values.Length; i++)
            {
                byte[] bytes = BitConverter.GetBytes(values[i]);
                buffer[i * 2] = (bytes[1] << 8) | bytes[0];   // CD
                buffer[i * 2 + 1] = (bytes[3] << 8) | bytes[2];   // AB
            }
            return buffer;
        }

        private static string ConvertToString(int[] raw, int charCount)
        {
            byte[] bytes = new byte[charCount];
            for (int i = 0; i < raw.Length; i++)
            {
                if (i * 2 < charCount) bytes[i * 2] = (byte)(raw[i] >> 8);
                if (i * 2 + 1 < charCount) bytes[i * 2 + 1] = (byte)(raw[i] & 0xFF);
            }
            return Encoding.ASCII.GetString(bytes).TrimEnd('\0');
        }
      
        private static int[] ConvertStringToRaw(string value, int maxLength)
        {
            value = (value ?? "") + "\0";
            if (value.Length > maxLength) value = value[..maxLength];

            int regCount = (value.Length + 1) / 2;
            int[] buffer = new int[regCount];

            for (int i = 0; i < regCount; i++)
            {
                ushort word = 0;
                if (i * 2 < value.Length) word |= (ushort)(value[i * 2] << 8);
                if (i * 2 + 1 < value.Length) word |= (ushort)value[i * 2 + 1];
                buffer[i] = word;
            }
            return buffer;
        }

        public static int GetRegisterCount(ModbusDataType dataType, int stringLength = 0)
        {
            return dataType switch
            {
                ModbusDataType.Int16 or ModbusDataType.UInt16 => 1,
                ModbusDataType.Int32 or ModbusDataType.Float => 2,
                ModbusDataType.Double => 4,
                ModbusDataType.String => (stringLength + 1) / 2,
                _ => 1
            };
        }


    }
}
