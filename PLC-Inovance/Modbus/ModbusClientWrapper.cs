using EasyModbus;
using PLC_Inovance.Models;
using PLC_Inovance.Utils;
using System.Diagnostics;
using System.Text;


namespace PLC_Inovance.Modbus
{
    internal class ModbusClientWrapper
    {
        private ModbusClient _client;
        private readonly object _lock = new object();

        public bool IsConnected
        {
            get { lock (_lock) { return _client?.Connected ?? false; } }
        }

        public bool Connect(string ip, int port, byte unitId)
        {
            lock (_lock)
            {
                try
                {
                    _client = new ModbusClient(ip, port);
                    _client.UnitIdentifier = unitId;
                    _client.ConnectionTimeout = 3000;
                    _client.Connect();
                    return _client.Connected;
                }
                catch
                {
                    _client = null;
                    return false;
                }
            }
        }

        public void Disconnect()
        {
            lock (_lock)
            {
                try
                {
                    _client?.Disconnect();
                }
                catch { }
                finally
                {
                    _client = null; // ensure torn-down client is not reused
                }
            }
        }

        public bool[] ReadBits(ElemType type, int startAddr, int count)
        {
            ushort modbusStartAddress = 0;
            lock (_lock)
            {
                if (!(_client?.Connected ?? false))
                    return null;

                try
                {
                    switch (type)
                    {
                        case ElemType.X:
                            return _client.ReadCoils(startAddr, count);
                        case ElemType.Y:
                            modbusStartAddress = (ushort)(1000 + startAddr);
                            return _client.ReadCoils(modbusStartAddress, count);
                        case ElemType.M:
                            modbusStartAddress = (ushort)(2048 + startAddr);
                            return _client.ReadCoils(modbusStartAddress, count);

                        default:
                            return null;
                    }
                }
                catch (Exception ex)
                {

                    //// treat any exception as loss of connection
                    //try { _client?.Disconnect(); } catch { }
                    //_client = null;
                    //return null;
                    throw;
                }
            }
        }

        #region ===== READ WORDS =====

        public T[] ReadWords<T>(ElemType type, int startAddr, int count) where T : struct
        {
            if (!IsConnected) return null;
            ushort modbusStartAddress = 0;
            try
            {
                switch (type)
                {
                    case ElemType.D:
                        modbusStartAddress = (ushort)(0 + startAddr);          // D0 → holding register 0
                        break;

                    case ElemType.MW:   // hoặc ElemType.W nếu bạn dùng tên W
                        modbusStartAddress = (ushort)(1000 + startAddr);       // W0 → holding register 1000
                        break;

                    // Thêm các vùng word khác nếu cần
                    // case ElemType.Z:
                    //     modbusStartAddress = (ushort)(2000 + startAddr);
                    //     break;

                    default:
                        throw new ArgumentException($"Unsupported ElemType for ReadWords: {type}");
                }
                lock (_lock)
                {


                    // ===== Xác định số register cần đọc =====
                    int registerCount = count;

                    if (typeof(T) == typeof(int) || typeof(T) == typeof(float))
                        registerCount = count * 2;

                    int[] raw = _client.ReadHoldingRegisters(modbusStartAddress, registerCount);
                    if (raw == null || raw.Length == 0)
                        return null;

                    // ===== ushort (chuẩn Modbus) =====
                    if (typeof(T) == typeof(ushort))
                    {
                        ushort[] result = new ushort[count];
                        for (int i = 0; i < count; i++)
                            result[i] = (ushort)raw[i];

                        return result as T[];
                    }

                    // ===== short =====
                    if (typeof(T) == typeof(short))
                    {
                        short[] result = new short[count];
                        for (int i = 0; i < count; i++)
                            result[i] = (short)raw[i];

                        return result as T[];
                    }

                    // ===== int (2 register) =====
                    if (typeof(T) == typeof(int))
                    {
                        int[] result = new int[count];

                        for (int i = 0; i < count; i++)
                        {
                            int high = raw[i * 2];
                            int low = raw[i * 2 + 1];

                            result[i] = (high << 16) | (low & 0xFFFF);
                        }

                        return result as T[];
                    }

                    // ===== float (2 register) =====
                    if (typeof(T) == typeof(float))
                    {
                        float[] result = new float[count];

                        for (int i = 0; i < count; i++)
                        {
                            ushort highWord = (ushort)raw[i * 2];
                            ushort lowWord = (ushort)raw[i * 2 + 1];

                            byte[] bytes = new byte[4];

                            // ✅ GHÉP ĐÚNG THỨ TỰ (ABCD)
                            bytes[0] = (byte)(highWord >> 8);
                            bytes[1] = (byte)(highWord & 0xFF);
                            bytes[2] = (byte)(lowWord >> 8);
                            bytes[3] = (byte)(lowWord & 0xFF);

                            // ⚠️ BitConverter dùng little endian → cần reverse
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(bytes);

                            result[i] = BitConverter.ToSingle(bytes, 0);
                        }

                        return result as T[];
                    }

                    throw new NotSupportedException($"Type {typeof(T)} not supported");

                    return null;
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        #endregion

        #region ===== WRITE BIT =====

        public bool WriteSingleBit(ElemType type, int address, bool value)
        {
            if (!IsConnected) return false;
            ushort modbusAddress = 0;

            try
            {
                lock (_lock)
                {
                    switch (type)
                    {

                        case ElemType.X:
                            modbusAddress = (ushort)(0 + address);
                            // X0 = coil 0
                            _client.WriteSingleCoil(modbusAddress, value);
                            break;
                        case ElemType.Y:

                            modbusAddress = (ushort)(1000 + address);
                            // Y0 = coil 1000 (theo offset server)
                            _client.WriteSingleCoil(modbusAddress, value);
                            break;
                        case ElemType.M:
                            modbusAddress = (ushort)(2048 + address);       // M0 = coil 2048 (offset phổ biến, KHÔNG dùng 2000 nữa)
                            _client.WriteSingleCoil(modbusAddress, value);                                         // Nếu bạn muốn giữ mapping cũ: (ushort)(2000 + startAddr);
                            break;
                        case ElemType.S:
                            modbusAddress = (ushort)(3000 + address);
                            _client.WriteSingleCoil(modbusAddress, value);
                            break;
                        case ElemType.B:
                            modbusAddress = (ushort)(4000 + address);
                            _client.WriteSingleCoil(modbusAddress, value);
                            break;
                        default:
                            throw new Exception("Unsupported ElemType for bit write");
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw;
            }


        }

        #endregion

        #region ===== WRITE WORDS =====

        public bool WriteWords<T>(ElemType type, int startAddr, T[] values) where T : struct
        {
            if (!IsConnected) return false;

            ushort modbusAddress = 0;
            try
            {
                lock (_lock)
                {
                    switch (type)
                    {
                        case ElemType.D:
                            modbusAddress = (ushort)(0 + startAddr);
                            break;

                        case ElemType.MW:
                            modbusAddress = (ushort)(1000 + startAddr);
                            break;

                        default:
                            throw new Exception("Unsupported ElemType for word write");
                    }
                    try
                    {

                        if (typeof(T) == typeof(ushort))
                        {

                            ushort[] data = values as ushort[];
                            int[] buffer = new int[data.Length];

                            for (int i = 0; i < data.Length; i++)
                                buffer[i] = data[i];

                            _client.WriteMultipleRegisters(modbusAddress, buffer);
                            return true;
                        }
                        if (typeof(T) == typeof(short))
                        {
                            short[] data = values as short[];
                            int[] buffer = new int[data.Length];

                            for (int i = 0; i < data.Length; i++)
                                buffer[i] = data[i];

                            _client.WriteMultipleRegisters(modbusAddress, buffer);
                            return true;
                        }
                        // ===== int (2 register) =====
                        if (typeof(T) == typeof(int))
                        {
                            int[] data = values as int[];
                            int[] buffer = new int[data.Length * 2];

                            for (int i = 0; i < data.Length; i++)
                            {
                                buffer[i * 2] = (int)((data[i] >> 16) & 0xFFFF);
                                buffer[i * 2 + 1] = (int)(data[i] & 0xFFFF);
                            }

                            _client.WriteMultipleRegisters(modbusAddress, buffer);
                            return true;
                        }

                        // ===== float (2 register) =====

                        if (typeof(T) == typeof(float))
                        {
                            float[] data = values as float[];

                            byte[] byteBuffer = new byte[data.Length * 4];
                            Buffer.BlockCopy(data, 0, byteBuffer, 0, byteBuffer.Length);

                            int[] registers = new int[data.Length * 2];

                            for (int i = 0; i < data.Length; i++)
                            {
                                int byteIndex = i * 4;

                                // little endian → đảo thành Modbus
                                byte b0 = byteBuffer[byteIndex + 3];
                                byte b1 = byteBuffer[byteIndex + 2];
                                byte b2 = byteBuffer[byteIndex + 1];
                                byte b3 = byteBuffer[byteIndex + 0];

                                registers[i * 2] = (b0 << 8) | b1; // High word
                                registers[i * 2 + 1] = (b2 << 8) | b3; // Low word
                            }

                            _client.WriteMultipleRegisters(modbusAddress, registers);
                            return true;
                        }
                        else if (typeof(T) == typeof(double))
                        {
                            double[] data = values as double[];
                            byte[] byteBuffer = new byte[data.Length * 8];
                            Buffer.BlockCopy(data, 0, byteBuffer, 0, byteBuffer.Length);

                            int[] registers = new int[data.Length * 4]; // 4 registers mỗi double

                            for (int i = 0; i < data.Length; i++)
                            {
                                int byteIndex = i * 8;

                                // Modbus thường dùng Big-Endian (High word trước), nhiều PLC dùng CDAB hoặc ABCD
                                // Dưới đây là cách phổ biến: byte order theo IEEE 754 → swap phù hợp
                                byte b7 = byteBuffer[byteIndex + 7];
                                byte b6 = byteBuffer[byteIndex + 6];
                                byte b5 = byteBuffer[byteIndex + 5];
                                byte b4 = byteBuffer[byteIndex + 4];
                                byte b3 = byteBuffer[byteIndex + 3];
                                byte b2 = byteBuffer[byteIndex + 2];
                                byte b1 = byteBuffer[byteIndex + 1];
                                byte b0 = byteBuffer[byteIndex + 0];

                                registers[i * 4 + 0] = (b7 << 8) | b6;   // Highest word
                                registers[i * 4 + 1] = (b5 << 8) | b4;
                                registers[i * 4 + 2] = (b3 << 8) | b2;
                                registers[i * 4 + 3] = (b1 << 8) | b0;   // Lowest word
                            }

                            _client.WriteMultipleRegisters(modbusAddress, registers);
                            return true;
                        }
                        throw new NotSupportedException($"Type {typeof(T)} not supported");
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// Ghi chuỗi với cơ chế xóa sạch vùng trước khi ghi để tránh dữ liệu thừa
        /// </summary>
        public bool WriteString(ElemType type, int startAddr, string value, int fixedRegisters = 10)
        {
            if (!IsConnected)
                return false;

            try
            {
                // Chuyển string thành bytes
                byte[] bytes = Encoding.ASCII.GetBytes(value ?? "");

                // Tạo mảng register với kích thước CỐ ĐỊNH và khởi tạo toàn bộ = 0
                int[] registers = new int[fixedRegisters];   // ← Quan trọng: khởi tạo = 0

                // Copy dữ liệu string vào (Big-Endian)
                for (int i = 0; i < fixedRegisters; i++)
                {
                    int byteIndex = i * 2;

                    byte high = (byteIndex < bytes.Length) ? bytes[byteIndex] : (byte)0;
                    byte low = (byteIndex + 1 < bytes.Length) ? bytes[byteIndex + 1] : (byte)0;

                    registers[i] = (high << 8) | low;
                }

                ushort modbusAddress = GetModbusAddress(type, startAddr);

                _client.WriteMultipleRegisters(modbusAddress, registers);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WriteString Error: {ex.Message}");
                return false;
            }
        }
        #endregion



        public object Read(ElemType elemType, int startAddr, int count, ModbusDataType dataType, int stringLength = 0)
        {
            if (!IsConnected) return null;
           
                ushort modbusAddr = GetModbusAddress(elemType, startAddr);


                // === SỬA Ở ĐÂY ===
                int registersPerItem = ModbusHelper.GetRegisterCount(dataType, stringLength);
                int totalRegisters = count * registersPerItem;        // ← Phải nhân với registersPerItem

                lock (_lock)
                {
                    int[] raw = ReadRegistersSafe(modbusAddr, totalRegisters);
                 
                    if (raw != null)
                    {
                        string rawStr = string.Join(", ", raw.Take(Math.Min(20, raw.Length)).Select(r => r.ToString()));
                        Console.WriteLine($"Raw data: {rawStr}");
                    }

                    return ModbusHelper.ConvertRawToValue(raw, count, dataType, stringLength);
                }
            
          
        }
        private int[] ReadRegistersSafe(ushort startAddr, int totalRegisters)
        {
            const int MAX_REG = 120; // an toàn < 125
            List<int> result = new List<int>();

            int offset = 0;

            while (offset < totalRegisters)
            {
                int size = Math.Min(MAX_REG, totalRegisters - offset);

                int[] part = _client.ReadHoldingRegisters(startAddr + offset, size);

                if (part == null)
                    throw new Exception("Read failed");

                result.AddRange(part);
                offset += size;
            }

            return result.ToArray();
        }
        public bool Write(ElemType elemType, int startAddr, object value, ModbusDataType dataType, int stringLength = 0)
        {
            if (!IsConnected) return false;

            ushort address = GetModbusAddress(elemType, startAddr);
            int[] registers = ModbusHelper.ConvertValueToRaw(value, dataType, stringLength);

            lock (_lock)
            {
                try
                {
                    _client.WriteMultipleRegisters(address, registers);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private ushort GetModbusAddress(ElemType elemType, int startAddr)
        {
            return elemType switch
            {
                ElemType.D => (ushort)startAddr,
                ElemType.MW => (ushort)(1000 + startAddr),
                _ => throw new ArgumentException($"Unsupported ElemType: {elemType}")
            };
        }
    }

}
