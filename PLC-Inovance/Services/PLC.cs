using EasyModbus;
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
        public int MaxReconnectAttempts { get; set; } = 4;
        public int ConnectTimeoutMs { get; set; } = 5000;
        public int ReconnectDelayMs { get; set; } = 2000;   
        private bool _isConnected = false;
        private bool _Reconnect = false;
        public bool IsConnected => _isConnected;
        private int reconnectAttemps = 0;
        public bool AutoReconnectEnabled { get; set; } = true;
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
            _pollingTimer.Elapsed += PollingTimer_Elapsed;

            _reconnectTimer = new System.Timers.Timer(ReconnectDelayMs);
            _reconnectTimer.AutoReset = false;                    // Quan trọng: chỉ chạy 1 lần mỗi lần Start()
            _reconnectTimer.Elapsed += async (s, e) => await ReconnectTimer_ElapsedAsync();
        }

       

        #region ===== CONNECTION =====
        // ====================== HÀM CONNECT CHÍNH (ĐÃ CẢI TIẾN) ======================
        public async Task<bool> ConnectAsync(string ip, int port = 502, byte unitId = 1)
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
                        _connectAttemps = 0;
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
                if (AutoReconnectEnabled && !isReconnect)
                {
                    _connectAttemps = 0;
                    _reconnectTimer.Start(); // 🔥 thêm
                }

                return false;
            }
        }
        private async Task ReconnectTimer_ElapsedAsync()
        {
            // Tránh chạy chồng chéo
            if (_Reconnect || !_lastIp.Any() || !AutoReconnectEnabled)
                return;

            lock (_lock)
            {
                if (_Reconnect) return;
                _Reconnect = true;
            }

            _connectAttemps++;

            RaiseStatus($"🔄 Thử kết nối lại lần {_connectAttemps}/{MaxReconnectAttempts}...");

            bool success = await ConnectInternalAsync(_lastIp, _lastPort, _lastUnitId, isReconnect: true);

            lock (_lock)
            {
                _Reconnect = false;
            }

            if (!success && _connectAttemps < MaxReconnectAttempts)
            {
                // Thử lại sau delay
                _reconnectTimer.Interval = ReconnectDelayMs; // có thể tăng dần nếu muốn: ReconnectDelayMs * _reconnectAttempts
                _reconnectTimer.Start();
            }
            else if (!success && _connectAttemps >= MaxReconnectAttempts)
            {
                RaiseStatus($"❌ Đã thử kết nối lại {MaxReconnectAttempts} lần thất bại. Tạm dừng auto reconnect.");
                // Bạn có thể thêm logic: AutoReconnectEnabled = false; hoặc cho phép user bấm nút Connect lại
            }
        }
        private void PollingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!IsConnected) return;

            try
            {
                var data = _modbus.ReadBits(ElemType.X, 0, 8);
                XUpdated?.Invoke(data);
            }
            catch (Exception ex)
            {
                if (IsConnectionError(ex))
                {
                    HandleConnectionLost();
                }
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

                    if (AutoReconnectEnabled)
                    {// 🔥 THÊM DÒNG NÀY
                        _connectAttemps = 0;
                        _reconnectTimer.Stop();
                        _reconnectTimer.Start();
                    }
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
                    _isConnected = true;
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
            StatusMessage?.Invoke(message + Environment.NewLine);
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
            try
            {
                lock (_lock)
                {
                    return _modbus.ReadBits(type, startAddr, count);
                }
            }
            catch(Exception ex) {

                if (IsConnectionError(ex))
                {
                    HandleConnectionLost();
                }
                return null;
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
        // ====================== READ ======================

        /// <summary>
        /// Đọc 1 giá trị duy nhất
        /// </summary>
        public async Task<T> ReadSingleAsync<T>(ElemType elemType, int startAddr, ModbusDataType dataType, int stringLength = 0)
        {
            var results = await ReadMultipleAsync<T>(elemType, startAddr, 1, dataType, stringLength);
            return results != null && results.Length > 0 ? results[0] : default;
        }

        /// <summary>
        /// Đọc nhiều giá trị
        /// </summary>
        public async Task<T[]> ReadMultipleAsync<T>(ElemType elemType, int startAddr, int count, ModbusDataType dataType, int stringLength = 0)
        {
            if (!IsConnected)
                return null;

            try
            {
                return await Task.Run(() =>
                {
                    object result = _modbus.Read(elemType, startAddr, count, dataType, stringLength);

                    if (result == null)
                        return null;

                    // Xử lý riêng String
                    if (dataType == ModbusDataType.String)
                    {
                        string strValue = result as string ?? result.ToString();
                        return new T[] { (T)(object)strValue };
                    }

                    // ===== SỬA LỖI Ở ĐÂY =====
                    if (result is T[] alreadyCorrectType)
                    {
                        return alreadyCorrectType;                    // Trường hợp T khớp với kiểu thực (float[], short[], int[]...)
                    }

                    if (result is Array array)
                    {
                        // Chuyển từ int[] → T[] (ví dụ: int[] → object[])
                        T[] converted = new T[array.Length];
                        for (int i = 0; i < array.Length; i++)
                        {
                            converted[i] = (T)Convert.ChangeType(array.GetValue(i), typeof(T));
                        }
                        return converted;
                    }

                    return null;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PlcService] Read error at {elemType}{startAddr}: {ex.Message}");
                return null;
            }
        }

        // Thêm hàm mới: Đọc float
        public float[] ReadFloats(ElemType type, int startAddr, int count)
        {
            if (!IsConnected) return null;

            lock (_lock)
            {
                return _modbus.ReadWords<float>(type, startAddr, count);
            }
        }
        // Ví dụ hàm đọc nhiều string
     
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

        public bool WriteWords<T>(ElemType type, int startAddr, T[] values) where T : struct
        {
            if (!IsConnected) return false;

            lock (_lock)
            {
                return _modbus.WriteWords(type, startAddr, values);
            }
        }

        #endregion


        public bool WriteString(ElemType type, int startAddr, string value, bool nullTerminate = true, int maxRegisters =0 )
        {
            if (!IsConnected) return false;

            lock (_lock)
            {
                return _modbus.WriteString(type, startAddr, value);
            }
        }

        public async Task<string[]> ReadMultipleStringAsync(ElemType elemType, int startAddr, int count, int charPerString = 20)
        {
            string[] strings = new string[count];
            int currentAddr = startAddr;
            int registersPerString = (charPerString + 1) / 2 ;
            for (int i = 0; i < count; i++)
            {
                strings[i] = await ReadSingleAsync<string>(elemType, currentAddr, ModbusDataType.String,charPerString) ?? "";
                currentAddr += registersPerString;   // ước lượng số register cho 1 string
            }
            return strings;
        }
    }
}
