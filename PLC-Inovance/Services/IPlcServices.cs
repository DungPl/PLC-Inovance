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

        bool IsConnected { get; }

        bool Connect(string ip, int port = 1502, byte unitId = 1);

        void Disconnect();

        event Action<bool> ConnectionChanged;

        // =============================
        // Read
        // =============================

        bool[] ReadBits(ElemType type, int startAddress, int count);

        short[] ReadWords(ElemType type, int startAddress, int count);


        // =============================
        // Write
        // =============================

        bool WriteBit(ElemType type, int address, bool value);

        bool WriteWords(ElemType type, int startAddress, short[] values);


        // =============================
        // Polling Events
        // =============================

        event Action<bool[]> XUpdated;
    }
}
