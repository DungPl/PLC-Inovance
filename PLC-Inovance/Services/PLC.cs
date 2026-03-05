using Microsoft.VisualBasic.Devices;
using PLC_Inovance.Modbus;
using PLC_Inovance.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace PLC_Inovance.Services
{
    public class PLC : IPlcServices
    {
       private readonly ModbusClientWrapper _modbus;
        private readonly System.Timers.Timer _pollingTimer;
        private readonly object _lock = new object();

        public bool IsConnected => _modbus?.IsConnected ?? false;

        public event Action<bool> ConnectionChanged;
        public event Action<bool[]> XUpdated;

        public PLC()
        {
            _modbus = new ModbusClientWrapper();

            _pollingTimer = new System.Timers.Timer(500);
           // _pollingTimer.Elapsed += PollingTimer_Elapsed;
        }
        #region ===== CONNECTION =====

        public bool Connect(string ip, int port = 1502, byte unitId = 1)
        {
            try
            {
                bool result = _modbus.Connect(ip, port, unitId);

                if (result)
                {
                    _pollingTimer.Start();
                    ConnectionChanged?.Invoke(true);
                }

                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message);
                ConnectionChanged?.Invoke(false);
                return false;
            }
        }

        public void Disconnect()
        {
            _pollingTimer.Stop();
            _modbus?.Disconnect();
            ConnectionChanged?.Invoke(false);
        }

        #endregion

        #region ===== READ =====

        public bool[] ReadBits(ElemType type, int startAddr, int count)
        {
            if (!IsConnected) return null;

            lock (_lock)
            {
                return _modbus.ReadBits(type, startAddr, count);
            }
        }

        public short[] ReadWords(ElemType type, int startAddr, int count)
        {
            if (!IsConnected) return null;

            lock (_lock)
            {
                return _modbus.ReadWords<short>(type, startAddr, count);
            }
        }

        #endregion

        #region ===== WRITE =====

        public bool WriteBit(ElemType type, int address, bool value)
        {
            if (!IsConnected) return false;

            lock (_lock)
            {
                return _modbus.WriteSingleBit(type, address, value);
            }
        }

        public bool WriteWords(ElemType type, int startAddr, short[] values)
        {
            if (!IsConnected) return false;

            lock (_lock)
            {
                return _modbus.WriteWords(type, startAddr, values);
            }
        }

        #endregion

        #region ===== POLLING =====

        //private void PollingTimer_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    if (!IsConnected) return;

        //    try
        //    {
        //        // Ví dụ đọc X0-X15
        //        bool[] xBits = _modbus.ReadBits(ElemType.X, 0, 16);

        //        XUpdated?.Invoke(xBits);
        //    }
        //    catch
        //    {
        //        // nếu lỗi khi polling → coi như mất kết nối
        //        Disconnect();
        //    }
        //}

        #endregion

        public event Action<bool[]> OnXChanged;
    }
}
