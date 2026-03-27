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
                catch(Exception ex) {

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
                    int[] raw = _client.ReadHoldingRegisters(modbusStartAddress, count);

                    if (typeof(T) == typeof(short))
                    {
                        short[] result = new short[count];
                        for (int i = 0; i < count; i++)
                        {
                            result[i] = (short)raw[i];
                        }
                        return result as T[];
                    }

                    if (typeof(T) == typeof(int))
                    {
                        int[] result = new int[count];
                        Array.Copy(raw, result, count);
                        return result as T[];
                    }

                    // Nếu cần hỗ trợ ushort (rất phổ biến cho Modbus)
                    if (typeof(T) == typeof(ushort))
                    {
                        ushort[] result = new ushort[count];
                        for (int i = 0; i < count; i++)
                        {
                            result[i] = (ushort)raw[i];
                        }
                        return result as T[];
                    }

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
                        if (typeof(T) == typeof(short))
                        {
                            short[] data = values as short[];
                            int[] buffer = new int[data.Length];

                            for (int i = 0; i < data.Length; i++)
                                buffer[i] = data[i];

                            _client.WriteMultipleRegisters(modbusAddress, buffer);
                            return true;
                        }

                        return false;
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
