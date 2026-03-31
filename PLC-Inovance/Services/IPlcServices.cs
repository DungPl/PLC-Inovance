using PLC_Inovance.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC_Inovance.Services
{
   public interface  IPlcServices
    {
        // =============================
        // Connection
        // =============================
        public enum PlcConnectionState
        {
            Disconnected,
            Connecting,
            Connected,
            Reconnecting,
            ConnectionFailed
        }
        bool IsConnected { get; }

        bool Connect(string ip, int port = 1502, byte unitId = 1);
        Task<bool> ConnectAsync(string ip, int port = 1502, byte unitId = 1);
        void Disconnect();

        bool AutoReconnectEnabled { get; set; }
        int MaxReconnectAttempts { get; set; }      // Mặc định = 4
        int ConnectTimeoutMs { get; set; }          // Mặc định = 5000ms

        // =============================
        // Events
        // =============================
        event Action<bool> ConnectionChanged;                    // True = Connected, False = Disconnected
       // event Action<PlcConnectionState> ConnectionStateChanged; // Trạng thái chi tiết
        event Action<string> StatusMessage;                      // Thông báo text (rất hữu ích cho UI)

       

        // =============================
        // Read
        // =============================

        bool[] ReadBits(ElemType type, int startAddress, int count);

        short[] ReadWords(ElemType type, int startAddress, int count);
        Task<T> ReadSingleAsync<T>(ElemType elemType, int startAddr, ModbusDataType dataType, int stringLength = 0);
        Task<T[]> ReadMultipleAsync<T>(ElemType elemType, int startAddr, int count, ModbusDataType dataType, int stringLength = 0);
        float[] ReadFloats(ElemType type, int startAddress, int count);
        // =============================
        // Write
        // =============================

        bool WriteBit(ElemType type, int address, bool value);

        bool WriteWords<T>(ElemType type, int startAddress, T[] values) where T : struct;


        // =============================
        // Polling Events
        // =============================

        event Action<bool[]> XUpdated;
    }
}
