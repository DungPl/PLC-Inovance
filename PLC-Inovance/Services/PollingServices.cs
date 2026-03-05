using PLC_Inovance.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC_Inovance.Services
{
    internal class PollingServices : IPollingServices
    {
        private readonly IPlcServices _plc;
        private CancellationTokenSource _cts;
        
        public bool IsRunning => _cts != null;

        public event Action<bool[]> XUpdated;
        public event Action<short[]> DUpdated;

        public PollingServices(IPlcServices plc)
        {
            _plc = plc;
        }

        public void Start()
        {
            if (!_plc.IsConnected) return;
            if (IsRunning) return;

            _cts = new CancellationTokenSource();

            Task.Run(() => PollLoop(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
        }

        private async Task PollLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("[Polling] Bắt đầu vòng lặp đọc PLC - " + DateTime.Now.ToString("HH:mm:ss.fff"));

                    var x = _plc.ReadBits(ElemType.X, 0, 8);
                    Console.WriteLine($"[Polling] Đọc X thành công - length = {x?.Length ?? -1}");

                    var d = _plc.ReadWords(ElemType.D, 0, 8);
                    Console.WriteLine($"[Polling] Đọc D thành công - length = {d?.Length ?? -1}");

                    if (x != null)
                    {
                        XUpdated?.Invoke(x);
                        Console.WriteLine("[Polling] Đã invoke XUpdated");
                    }

                    if (d != null)
                    {
                        DUpdated?.Invoke(d);
                        Console.WriteLine("[Polling] Đã invoke DUpdated");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Polling ERROR] Lỗi đọc PLC: " + ex.Message);
                    Console.WriteLine("Stack: " + ex.StackTrace?.Substring(0, Math.Min(300, ex.StackTrace?.Length ?? 0)));

                    // Không Stop() ngay → để tiếp tục thử lại
                    // Stop();   ← comment dòng này tạm thời để polling không dừng

                    // Hoặc chỉ stop nếu lỗi nghiêm trọng, ví dụ mất kết nối hoàn toàn
                    if (ex.Message.Contains("disconnected") || ex.Message.Contains("timeout"))
                    {
                        Stop();
                    }
                }

                try
                {
                    await Task.Delay(500, token);
                }
                catch (TaskCanceledException) { /* bình thường khi cancel */ }
            }

            Console.WriteLine("[Polling] PollLoop đã dừng");
        }


    }
}
