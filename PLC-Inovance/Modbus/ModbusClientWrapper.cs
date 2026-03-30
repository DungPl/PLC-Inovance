using EasyModbus;
using PLC_Inovance.Models;


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
                            ushort highWord = (ushort)raw[i * 2];     // register n
                            ushort lowWord = (ushort)raw[i * 2 + 1]; // register n+1

                            // Swap word: lowWord làm high, highWord làm low
                            byte[] bytes = new byte[4];
                            bytes[0] = (byte)(lowWord >> 8);
                            bytes[1] = (byte)(lowWord & 0xFF);
                            bytes[2] = (byte)(highWord >> 8);
                            bytes[3] = (byte)(highWord & 0xFF);

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
                                buffer[i * 2] = (data[i] >> 16) & 0xFFFF; // high
                                buffer[i * 2 + 1] = data[i] & 0xFFFF;         // low
                            }

                            _client.WriteMultipleRegisters(modbusAddress, buffer);
                            return true;
                        }

                        // ===== float (2 register) =====
                        // ===== float (2 register) - CDAB order (rất phổ biến với PLC Việt Nam) =====
                        if (typeof(T) == typeof(float))
                        {
                            float[] data = values as float[];
                            int[] buffer = new int[data.Length * 2];

                            for (int i = 0; i < data.Length; i++)
                            {
                                byte[] bytes = BitConverter.GetBytes(data[i]);   // Little-endian của C# (Intel)

                                // CDAB format: Low word trước, High word sau
                                // bytes[0]=D, [1]=C, [2]=B, [3]=A
                                buffer[i * 2] = (bytes[1] << 8) | bytes[0];   // Low word  (CD)
                                buffer[i * 2 + 1] = (bytes[3] << 8) | bytes[2];   // High word (AB)
                            }

                            _client.WriteMultipleRegisters(modbusAddress, buffer);
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

        #endregion
    }

}
