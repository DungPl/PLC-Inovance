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
            lock (_lock)
            {
                if (!(_client?.Connected ?? false))
                    return null;

                try
                {
                    switch (type)
                    {
                        case ElemType.X:
                        case ElemType.Y:
                        case ElemType.M:
                            return _client.ReadCoils(startAddr, count);

                        default:
                            return null;
                    }
                }
                catch
                {
                    // treat any exception as loss of connection
                    try { _client?.Disconnect(); } catch { }
                    _client = null;
                    return null;
                }
            }
        }

        #region ===== READ WORDS =====

        public T[] ReadWords<T>(ElemType type, int startAddr, int count) where T : struct
        {
            if (!IsConnected) return null;

            lock (_lock)
            {
                int[] raw = _client.ReadHoldingRegisters(startAddr, count);

                if (typeof(T) == typeof(short))
                {
                    short[] result = new short[count];
                    for (int i = 0; i < count; i++)
                        result[i] = (short)raw[i];

                    return result as T[];
                }

                if (typeof(T) == typeof(int))
                {
                    int[] result = new int[count];
                    Array.Copy(raw, result, count);
                    return result as T[];
                }

                return null;
            }
        }

        #endregion

        #region ===== WRITE BIT =====

        public bool WriteSingleBit(ElemType type, int address, bool value)
        {
            if (!IsConnected) return false;

            lock (_lock)
            {
                try
                {
                    _client.WriteSingleCoil(address, value);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        #endregion

        #region ===== WRITE WORDS =====

        public bool WriteWords<T>(ElemType type, int startAddr, T[] values) where T : struct
        {
            if (!IsConnected) return false;

            lock (_lock)
            {
                try
                {
                    if (typeof(T) == typeof(short))
                    {
                        short[] data = values as short[];
                        int[] buffer = new int[data.Length];

                        for (int i = 0; i < data.Length; i++)
                            buffer[i] = data[i];

                        _client.WriteMultipleRegisters(startAddr, buffer);
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

        #endregion
    }

}
