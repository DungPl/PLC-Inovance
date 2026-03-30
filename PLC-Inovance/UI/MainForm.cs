using Microsoft.Data.Sqlite;
using PLC_Inovance.Models;
using PLC_Inovance.Services;
using System.Diagnostics;
using System.Windows.Forms;

namespace PLC_Inovance
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }
        private IPlcServices _plc;
        private IPollingServices _polling;
        private DatabaseService _db;
       
        private PlcData _latestData = new PlcData();
       

       
        private bool HasDataChanged(bool[] newX, short[] newD)
        {
            lock (_latestData)
            {
                if (newX != null)
                {
                    for (int i = 0; i < Math.Min(newX.Length, _latestData.X.Length); i++)
                        if (newX[i] != _latestData.X[i]) return true;
                }
                if (newD != null)
                {
                    for (int i = 0; i < Math.Min(newD.Length, _latestData.D.Length); i++)
                        if (newD[i] != _latestData.D[i]) return true;
                }
                return false;
            }
        }
        private async void MainForm_Load(object sender, EventArgs e)
        {
            _plc = new PLC();
            _polling = new PollingServices(_plc);
            cmbElemType.DataSource = Enum.GetValues(typeof(ElemType));


       

            // Cấu hình Auto Reconnect
            _plc.AutoReconnectEnabled = true;
            _plc.MaxReconnectAttempts = 4;
            _plc.ConnectTimeoutMs = 5000;
          
         
            cmbElemType.SelectedIndex = 5;// Default to D
            cbType.SelectedIndex = 1; // Default to short
            _db = new DatabaseService();
            _db.Init();
            _plc.ConnectionChanged += connected =>
            {
                if (InvokeRequired)
                    Invoke(new Action(() =>
                    {
                        lblStatus.Text = connected ? "Connected" : "Disconnected";
                        lblStatus.ForeColor = connected ? Color.Green : Color.Red;
                    }));
                else
                {
                    lblStatus.Text = connected ? "Connected" : "Disconnected";
                    lblStatus.ForeColor = connected ? Color.Green : Color.Red;
                }
            };
            _plc.StatusMessage += message =>
            {
                if (InvokeRequired)
                    Invoke(new Action(() => rtbLog.AppendText($"[PLC] {message}")));
                else
                    rtbLog.AppendText($"[PLC] {message}");
            };
            _polling.XUpdated += bits =>
            {
                rtbLog.AppendText($"[Polling X] Event XUpdated được gọi lúc {DateTime.Now:HH:mm:ss.fff} - bits length: {bits?.Length ?? -1}\n");
                rtbLog.ScrollToCaret();

                if (bits == null)
                {
                    rtbLog.AppendText("[XUpdated] bits is null → thoát\n");
                    return;
                }

                rtbLog.AppendText($"[XUpdated] bits.Length = {bits.Length}\n");

                // Phần UI
                rtbLog.AppendText("[XUpdated] Trước UpdateXUI\n");
                if (InvokeRequired)
                {
                    Invoke(new Action(() => UpdateXUI(bits)));
                }
                else
                {
                    UpdateXUI(bits);
                }
                rtbLog.AppendText("[XUpdated] Sau UpdateXUI – UI OK\n");

                // Bắt đầu phần DB
                rtbLog.AppendText("[XUpdated] Vào lock _latestData\n");
                try
                {
                    lock (_latestData)
                    {
                        rtbLog.AppendText("[XUpdated] Đã vào lock\n");

                        bool changed = HasDataChanged(bits, null);
                        rtbLog.AppendText($"[XUpdated] HasDataChanged trả về: {changed}\n");

                        Array.Copy(bits, _latestData.X, Math.Min(bits.Length, _latestData.X.Length));
                        rtbLog.AppendText("[XUpdated] Array.Copy OK\n");

                        _latestData.TimeStamp = DateTime.Now;
                        _latestData.PLC_IP = txtIP.Text.Trim();

                        _latestData.IsRunning = bits.Length > 0 && bits[0];
                        _latestData.HasAlarm = bits.Length > 3 && bits[3];

                        rtbLog.AppendText($"Chuẩn bị insert DB: Time={_latestData.TimeStamp:HH:mm:ss.fff}, PLC_IP={_latestData.PLC_IP}, X0={_latestData.X[0]}\n");

                        _db.Insert(_latestData);
                        rtbLog.AppendText("→ Insert DB thành công\n");
                    }
                }
                catch (Exception ex)
                {
                    rtbLog.AppendText($"[XUpdated] LỖI nghiêm trọng trong lock/insert: {ex.GetType().Name} - {ex.Message}\n");
                    if (ex.InnerException != null)
                        rtbLog.AppendText($" Inner: {ex.InnerException.Message}\n");
                    rtbLog.AppendText($" Stack: {ex.StackTrace?.Substring(0, Math.Min(300, ex.StackTrace.Length))}\n");
                }

                rtbLog.AppendText("[XUpdated] Kết thúc handler\n");
            };



            _polling.DUpdated += words =>
            {
                if (words == null) return;
                short[] shorts = Array.ConvertAll(words, w => (short)w);
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        UpdateDUI(Array.ConvertAll(words, w => (short)w));
                    }));
                }
                else
                {
                    UpdateDUI(Array.ConvertAll(words, w => (short)w));
                }
                lock (_latestData)
                {
                    bool changed = HasDataChanged(null, shorts);

                    Array.Copy(shorts, _latestData.D, Math.Min(shorts.Length, _latestData.D.Length));
                    _latestData.TimeStamp = DateTime.Now;
                    _latestData.PLC_IP = txtIP.Text.Trim();

                    rtbLog.AppendText($"Chuẩn bị insert DB: Time={_latestData.TimeStamp}, PLC_IP={_latestData.PLC_IP}, Y0={_latestData.D[0]}\n");

                    try
                    {
                        _db.Insert(_latestData);
                        rtbLog.AppendText("→ Insert DB thành công (không lỗi)\n");
                    }
                    catch (Exception ex)
                    {
                        rtbLog.AppendText($"→ LỖI insert DB: {ex.Message}\n");
                        if (ex.InnerException != null)
                            rtbLog.AppendText($"  Inner: {ex.InnerException.Message}\n");
                    }
                }
            };

            await AutoConnectAfterLoad();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            string ip = txtIP.Text.Trim();

            bool ok = _plc.Connect(ip);

            if (ok)
            {
                _polling.Start();   // 🔥 BẮT ĐẦU POLLING Ở ĐÂY
                rtbLog.AppendText("Kết nối thành công\n");
            }
            else
            {
                rtbLog.AppendText("Kết nối thất bại\n");
            }



        }




        private async Task AutoConnectAfterLoad()
        {
            string ip = txtIP.Text.Trim();
            if (string.IsNullOrEmpty(ip))
            {
                rtbLog.AppendText("[MainForm] Chưa nhập IP PLC!\n");
                return;
            }

            rtbLog.AppendText($"[MainForm] Đang kết nối đến PLC {ip}...\n");

            bool success = await _plc.ConnectAsync(ip);

            if (!success)
            {
                rtbLog.AppendText("[MainForm] Kết nối ban đầu thất bại. Hệ thống sẽ tự động thử lại tối đa 4 lần.\n");
            }
        }
        private void AppendLog(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {text}\n");
                    rtbLog.ScrollToCaret();
                }));
            }
            else
            {
                rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {text}\n");
                rtbLog.ScrollToCaret();
            }
        }
        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            _polling.Stop();       // 🔥 DỪNG POLLING
            _plc.Disconnect();
        }
        private void UpdateXUI(bool[] bits)
        {
            if (bits == null) return;

            Label[] xLabels =
     {
        x0, x1, x2, x3,
        x4, x5, x6, x7
    };

            for (int i = 0; i < xLabels.Length && i < bits.Length; i++)
            {
                xLabels[i].BackColor = bits[i] ? Color.LimeGreen : Color.LightGray;
                xLabels[i].ForeColor = bits[i] ? Color.White : Color.Black;
            }


        }
        private void UpdateDUI(short[] words)
        {
            if (words == null) return;

            Label[] dLabels =
            {
        y0, y1, y2, y3,
        y4, y5, y6, y7
    };

            for (int i = 0; i < dLabels.Length && i < words.Length; i++)
            {
                short value = words[i];

                dLabels[i].Text = value.ToString();

                if (value == 0)
                {
                    dLabels[i].BackColor = Color.LightGray;
                    dLabels[i].ForeColor = Color.Black;
                }
                else if (value > 1000)
                {
                    dLabels[i].BackColor = Color.Orange;
                    dLabels[i].ForeColor = Color.White;
                }
                else if (value > 0)
                {
                    dLabels[i].BackColor = Color.DodgerBlue;
                    dLabels[i].ForeColor = Color.White;
                }
                else
                {
                    dLabels[i].BackColor = Color.Red;
                    dLabels[i].ForeColor = Color.White;
                }
            }
        }
        private void btnRead_Click(object sender, EventArgs e)
        {
            if (!_plc.IsConnected)
            {
                rtbLog.AppendText("Chưa kết nối PLC\n");
                return;
            }

            string sel = cmbElemType.SelectedItem?.ToString();
            Debug.Write("Kieu du lieu"+sel);
            if (!Enum.TryParse<ElemType>(sel, out var type))
            {
                rtbLog.AppendText("Invalid element type selected\n");
                return;
            }

            int startAddr = (int)nudStartAddress.Value;
            int count = (int)nudCount.Value;

            try
            {
                if (string.Equals(cbType.SelectedItem?.ToString(), "bool", StringComparison.OrdinalIgnoreCase))
                {
                    bool[] bits = _plc.ReadBits(type, startAddr, count);

                    if (bits == null)
                    {
                        rtbLog.AppendText("Không đọc được dữ liệu\n");
                        return;
                    }
                    if (type == ElemType.X)
                    {
                        // Chỉ cập nhật nếu đọc đúng vùng X0~X7 mà form đang hiển thị
                        if (InvokeRequired)
                            Invoke(new Action(() => UpdateXUI(bits)));
                        else
                            UpdateXUI(bits);
                    }
                    rtbLog.AppendText("Read Bits: ");
                    rtbLog.AppendText(string.Join(",", bits));
                    rtbLog.AppendText("\n");
                }
                else
                {
                    string dataType = cbType.SelectedItem?.ToString()?.ToLower();

                    if (dataType == "float")
                    {
                        float[] floats = _plc.ReadFloats(type, startAddr, count);

                        if (floats == null)
                        {
                            rtbLog.AppendText("Không đọc được dữ liệu float\n");
                            return;
                        }

                        if (type == ElemType.D)
                        {
                            if (InvokeRequired)
                                Invoke(new Action(() => UpdateDUI_Float(floats)));  // Bạn cần viết hàm này
                            else
                                UpdateDUI_Float(floats);
                        }

                        rtbLog.AppendText("Read Floats: ");
                        rtbLog.AppendText(string.Join(", ", floats.Select(f => f.ToString("F4"))));
                        rtbLog.AppendText("\n");
                    }
                    else
                    {
                        // Phần short cũ giữ nguyên
                        short[] words = _plc.ReadWords(type, startAddr, count);

                        if (words == null)
                        {
                            rtbLog.AppendText("Không đọc được dữ liệu\n");
                            return;
                        }
                        if (type == ElemType.D)
                        {
                            if (InvokeRequired)
                                Invoke(new Action(() => UpdateDUI(words)));
                            else
                                UpdateDUI(words);
                        }
                        rtbLog.AppendText("Read Words: ");
                        rtbLog.AppendText(string.Join(",", words));
                        rtbLog.AppendText("\n");
                    }
                }
            }
            catch (Exception ex)
            {
                rtbLog.AppendText("Lỗi đọc: " + ex.Message + "\n");
            }
        }

        private void UpdateDUI_Float(float[] floats)
        {
            throw new NotImplementedException();
        }

        private void btnWrite_Click(object sender, EventArgs e)
        {
            if (!_plc.IsConnected)
            {
                rtbLog.AppendText("Chưa kết nối PLC\n");
                return;
            }

            string sel = cmbElemType.SelectedItem?.ToString();
            if (!Enum.TryParse<ElemType>(sel, out var type))
            {
                rtbLog.AppendText("Invalid element type selected\n");
                return;
            }

            int startAddr = (int)nudStartAddress.Value;

            try
            {
                if (cbType.SelectedItem.ToString() == "bool")
                {
                    string input = txtValue.Text.Trim();
                    if (string.IsNullOrEmpty(input))
                    {
                        rtbLog.AppendText("txtValue rỗng → không ghi được bit\n");
                        return;
                    }

                    bool value;
                    if (!bool.TryParse(input, out value))
                    {
                        rtbLog.AppendText($"Giá trị '{input}' không phải True/False\n");
                        return;
                    }

                    bool result = _plc.WriteBit(type, startAddr, value);
                    Debug.WriteLine("Dia chi du lieuj ",startAddr);
                    rtbLog.AppendText(result ? "Ghi bit thành công\n" : "Ghi bit thất bại\n");

                    // Nếu ghi thành công → đọc lại X để cập nhật UI và DB
                    if (result && type == ElemType.X )
                    {
                        // Đọc lại vùng X0-X7 để cập nhật UI và DB
                        bool[] bits = _plc.ReadBits(ElemType.X, 0, 8);
                        if (bits != null)
                        {
                            if (InvokeRequired)
                                Invoke(() => UpdateXUI(bits));
                            else
                                UpdateXUI(bits);

                            // Lưu vào DB
                            SaveToDbAfterWrite(bits, null);
                        }
                    }
                }
                //else
                //{
                //    string[] parts = txtValue.Text.Split(',');
                //    short[] values = parts.Select(p => short.Parse(p.Trim())).ToArray();

                //    bool result = _plc.WriteWords(type, startAddr, values);
                //    rtbLog.AppendText(result ? "Ghi word thành công\n" : "Ghi word thất bại\n");

                //    // Nếu ghi thành công → đọc lại D để cập nhật UI và DB
                //    if (result && type == ElemType.D )
                //    {
                //        short[] words = _plc.ReadWords(ElemType.D, 0, 8);
                //        if (words != null)
                //        {
                //            var shorts = Array.ConvertAll(words, w => (short)w);
                //            if (InvokeRequired)
                //                Invoke(() => UpdateDUI(shorts));
                //            else
                //                UpdateDUI(shorts);

                //            // Lưu vào DB
                //            SaveToDbAfterWrite(null, shorts);
                //        }
                //    }
                //}
                else
                {
                    string input = txtValue.Text.Trim();
                    string selectedType = cbType.SelectedItem.ToString();

                    try
                    {
                        if (selectedType == "short")
                        {
                            short[] values = input.Split(',')
                                                  .Select(p => short.Parse(p.Trim()))
                                                  .ToArray();

                            _plc.WriteWords(type, startAddr, values);
                        }
                        else if (selectedType == "ushort")
                        {
                            ushort[] values = input.Split(',')
                                                   .Select(p => ushort.Parse(p.Trim()))
                                                   .ToArray();

                            _plc.WriteWords(type, startAddr, values);
                        }
                        else if (selectedType == "int")
                        {
                            int[] values = input.Split(',')
                                                .Select(p => int.Parse(p.Trim()))
                                                .ToArray();

                            _plc.WriteWords(type, startAddr, values);
                        }
                        else if (selectedType == "float")
                        {
                            float[] values = input.Split(',')
                                                  .Select(p => float.Parse(p.Trim()))
                                                  .ToArray();

                            _plc.WriteWords(type, startAddr, values);
                        }
                        else
                        {
                            rtbLog.AppendText("Kiểu dữ liệu không hỗ trợ\n");
                            return;
                        }

                        rtbLog.AppendText("Ghi word thành công\n");
                    }
                    catch (Exception ex)
                    {
                        rtbLog.AppendText("Lỗi parse dữ liệu: " + ex.Message + "\n");
                    }
                }
            }
            catch (Exception ex)
            {
                rtbLog.AppendText("Lỗi ghi: " + ex.Message + "\n");
            }
        }

        private void btnLoadLog_Click(object sender, EventArgs e)
        {

           
            try
            {
                using var conn = new SqliteConnection("Data Source=plc.db;Cache=Shared");
                conn.Open();

                // Kiểm tra bảng tồn tại
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='PlcLog';";
                var result = cmd.ExecuteScalar();
                if (result == null)
                {
                    rtbLog.AppendText("Bảng PlcLog KHÔNG tồn tại!\n");
                    return;
                }
                rtbLog.AppendText("Bảng PlcLog tồn tại.\n");

                // Đếm bản ghi
                cmd.CommandText = "SELECT COUNT(*) FROM PlcLog;";
                long count = (long)cmd.ExecuteScalar();
                rtbLog.AppendText($"Số bản ghi trong PlcLog: {count}\n");

                if (count == 0)
                {
                    rtbLog.AppendText("Bảng trống - chưa có dữ liệu để hiển thị.\n");
                }
                else
                {
                    rtbLog.AppendText("Có dữ liệu - tiếp tục bind vào DataGridView.\n");
                    using (var cmd2 = conn.CreateCommand())
                    {
                        cmd2.CommandText =
                            "SELECT Id, TimeStamp, PLC_IP, X0, X1, X2, X3, X4, X5, X6, X7, " +
                            "D0, D1, D2, D3, D4, D5, D6, D7, IsRunning, HasAlarm " +
                            "FROM PlcLog ORDER BY Id DESC LIMIT 50;";
                        using var reader = cmd2.ExecuteReader();
                        var dataTable = new System.Data.DataTable();
                        dataTable.Load(reader);

                        // Bind vào DataGridView (giả sử tên là dataLog hoặc dgvPlcLog)
                        if (dataLog.InvokeRequired)
                        {
                            dataLog.Invoke(new Action(() =>
                            {
                                dataLog.DataSource = null;          // reset trước
                                dataLog.AutoGenerateColumns = true; // tự tạo cột
                                dataLog.DataSource = dataTable;
                                dataLog.AutoResizeColumns();        // điều chỉnh kích thước cột
                            }));
                        }
                        else
                        {
                            dataLog.DataSource = null;
                            dataLog.AutoGenerateColumns = true;
                            dataLog.DataSource = dataTable;
                            dataLog.AutoResizeColumns();
                        }

                        rtbLog.AppendText($"Đã bind {dataTable.Rows.Count} bản ghi vào DataGridView.\n");
                    }
                }
            }
            catch (Exception ex)
            {
                rtbLog.AppendText($"Lỗi kiểm tra DB: {ex.Message}\n");
            }
        }

        private void SaveToDbAfterWrite(bool[] newBits, short[] newWords)
        {
            lock (_latestData)
            {
                if (newBits != null)
                {
                    Array.Copy(newBits, _latestData.X, Math.Min(newBits.Length, _latestData.X.Length));
                }

                if (newWords != null)
                {
                    Array.Copy(newWords, _latestData.D, Math.Min(newWords.Length, _latestData.D.Length));
                }

                _latestData.TimeStamp = DateTime.Now;
                _latestData.PLC_IP = txtIP.Text.Trim();

                // Tính lại trạng thái nếu cần
                _latestData.IsRunning = _latestData.X.Length > 0 && _latestData.X[0];
                _latestData.HasAlarm = _latestData.X.Length > 3 && _latestData.X[3];

                try
                {
                    rtbLog.AppendText("→ Lưu DB sau khi write...\n");
                    _db.Insert(_latestData);
                    rtbLog.AppendText("→ Insert DB thành công sau write\n");
                }
                catch (Exception ex)
                {
                    rtbLog.AppendText("Lỗi insert DB sau write: " + ex.Message + "\n");
                }
            }
        }
    }
}
