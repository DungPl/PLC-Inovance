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
        private readonly System.Timers.Timer _reconnectTimer;
        private readonly object _lock = new object();

        private int _connectAttemps = 0;
        private const int _maxReconnectAttempts = 4;
        private const int ConnectTimeoutMs = 5000;
        private bool _isConnected = false;
        public bool IsConnected => _isConnected;
        private int reconnectAttemps = 0;
        public event Action<bool> ConnectionChanged;
        public event Action<bool[]> XUpdated;
        private string _lastIp = "";
        private int _lastPort = 1502;
        private byte _lastUnitId = 1;

      
        public event Action<string> StatusMessage;
        public PLC()
        {
            _modbus = new ModbusClientWrapper();

            _pollingTimer = new System.Timers.Timer(500);//cứ nửa giây định kỳ đọc PLC 1 lần 
                                                         // _pollingTimer.Elapsed += PollingTimer_Elapsed;

            _reconnectTimer = new System.Timers.Timer(2000);
        }
        #region ===== CONNECTION =====
        // ====================== HÀM CONNECT CHÍNH (ĐÃ CẢI TIẾN) ======================
        public async Task<bool> ConnectAsync(string ip, int port = 1502, byte unitId = 1)
        {
            _lastIp = ip;
            _lastPort = port;
            _lastUnitId = unitId;

            return await ConnectInternalAsync(ip, port, unitId, isReconnect: false);
        }

        private async Task<bool> ConnectInternalAsync(string ip, int port, byte unitId, bool isReconnect)
        {
            if (string.IsNullOrEmpty(ip))
                return false;

            try
            {
                if (!isReconnect)
                    RaiseStatus("Đang kết nối đến PLC...");

                // Thực hiện kết nối với Timeout
                var connectTask = Task.Run(() => _modbus.Connect(ip, port, unitId));
                var timeoutTask = Task.Delay(ConnectTimeoutMs);

                var winner = await Task.WhenAny(connectTask, timeoutTask);

                if (winner == timeoutTask)
                {
                    throw new TimeoutException($"Kết nối timeout sau {ConnectTimeoutMs / 1000} giây");
                }

                bool result = await connectTask;

                if (result)
                {
                    lock (this)
                    {
                        _isConnected = true;
                        _connectAttemps= 0;
                        _reconnectTimer.Stop();
                    }

                    _pollingTimer.Start();
                    ConnectionChanged?.Invoke(true);
                    RaiseStatus("✅ Kết nối PLC thành công");
                    return true;
                }
                else
                {
                    throw new Exception("Modbus Connect trả về false");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _pollingTimer.Stop();

                ConnectionChanged?.Invoke(false);
                RaiseStatus(isReconnect
                    ? $"❌ Thử kết nối lại thất bại: {ex.Message}"
                    : $"❌ Kết nối thất bại: {ex.Message}");

                return false;
            }
        }
        private async void ReconnectTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_connectAttemps >= _maxReconnectAttempts)
            {
                RaiseStatus($"❌ Đã thử kết nối lại {_maxReconnectAttempts} lần thất bại.");
                return;
            }

            _connectAttemps++;
            RaiseStatus($"🔄 Thử kết nối lại lần {_connectAttemps}/{_maxReconnectAttempts}...");

            bool success = await ConnectInternalAsync(_lastIp, _lastPort, _lastUnitId, isReconnect: true);

            if (!success && _connectAttemps < _maxReconnectAttempts)
            {
                _reconnectTimer.Start(); // Thử lại sau ReconnectDelay
            }
        }
        private void HandleConnectionLost()
        {
            lock (this)
            {
                if (_isConnected)
                {
                    _isConnected = false;
                    _pollingTimer.Stop();
                    RaiseStatus("⚠️ Mất kết nối với PLC. Đang thử kết nối lại...");

                    _connectAttemps = 0;
                    _reconnectTimer.Start();
                }
            }
        }
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
        private bool IsConnectionError(Exception ex)
        {
            string msg = ex.Message.ToLower();
            return msg.Contains("connection") || msg.Contains("timeout") ||
                   msg.Contains("disconnected") || msg.Contains("socket");
        }

        private void RaiseStatus(string message)
        {
            StatusMessage?.Invoke(message);
        }

        public void Stop()
        {
            _pollingTimer.Stop();
            _reconnectTimer.Stop();
            _modbus?.Disconnect();
            _ = false;
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
