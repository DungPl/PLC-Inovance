using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC_Inovance.Services
{
    public interface IPollingServices
    {
        bool IsRunning { get; }

        void Start();
        void Stop();

        event Action<bool[]> XUpdated;
        event Action<short[]> DUpdated;
    }
}
