using EasyModbus.Exceptions;
using Microsoft.Data.Sqlite;
using PLC_Inovance.Models;
using PLC_Inovance.Services;
using PLC_Inovance.Utils;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Text;
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
        private bool[] memoryUsed = new bool[10000];
        private PlcData _latestData = new PlcData();
        //private UIHelper _uiHelper = new UIHelper();
        //private ByteHelper _byteHelper = new ByteHelper();
        private readonly Dictionary<int, MemoryRegion> _occupiedRegions = new Dictionary<int, MemoryRegion>();

        // Class nhỏ để lưu thông tin vùng
        private class MemoryRegion
        {
            public int StartAddr { get; set; }
            public int Size { get; set; }           // số register
            public string DataType { get; set; }    // "short", "ushort", "int", "float", "double", "string"
            public DateTime LastUpdated { get; set; }
        }
      

        /// <summary>
        /// Kiểm tra xem vùng mới có chồng chéo với bất kỳ vùng nào đã chiếm trước đó không
        /// </summary>
        private bool IsRegionOverlapped(int startAddr, int sizeInRegisters, string newDataType)
        {
            if (sizeInRegisters <= 0) return false;

            int newEnd = startAddr + sizeInRegisters - 1;

            foreach (var region in _occupiedRegions.Values)
            {
                int regionEnd = region.StartAddr + region.Size - 1;

                bool overlap = !(newEnd < region.StartAddr || startAddr > regionEnd);
                if (!overlap) continue;
                if (newDataType == "string" && region.DataType == "string")
                {
                    int newBlock = startAddr / 10;
                    int oldBlock = region.StartAddr / 10;

                    // 👉 Cùng block
                    if (newBlock == oldBlock)
                    {
                        // ✔ overwrite đúng vị trí
                        if (startAddr == region.StartAddr)
                            return true; // ✅ cho phép

                        // ❌ cùng block nhưng khác vị trí
                        return true;
                    }
                }

                // ❌ tất cả các trường hợp khác đều là overlap nguy hiểm
                return true;
            }
            return false;
        }

        private bool HasDataChanged(bool[] newX, object newD, string dataType = "short")
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
                    return HasDDataChanged(newD, dataType);
                }
                return false;
            }
        }
        private bool HasDDataChanged(object newD, string dataType)
        {
            if (newD == null || _latestData.D == null)
                return false;
            switch (dataType.ToLower())
            {
                case "short":

                    if (newD is short[] newShorts)
                    {
                        for (int i = 0; i < Math.Min(newShorts.Length, _latestData.D.Length); i++)
                        {
                            if (newShorts[i] != _latestData.D[i])
                                return true;
                        }
                        return false;
                    }
                    break;
                case "float":
                    if (newD is float[] newFloats)
                    {
                        for (int i = 0; i < Math.Min(newFloats.Length, _latestData.D.Length); i++)
                        {
                            // Giả sử _latestData.D sẽ được mở rộng để lưu float nếu cần
                            if (Math.Abs(newFloats[i] - _latestData.D[i]) > 0.0001) // so sánh với ngưỡng nhỏ
                                return true;
                        }
                        return false;

                    }
                    break;
                case "double":
                    if (newD is double[] newDoubles)
                    {
                        for (int i = 0; i < Math.Min(newDoubles.Length, _latestData.D.Length); i++)
                        {
                            // Giả sử _latestData.D sẽ được mở rộng để lưu double nếu cần
                            if (Math.Abs(newDoubles[i] - _latestData.D[i]) > 0.000001) // so sánh với ngưỡng nhỏ hơn
                                return true;
                        }
                        return false;
                    }
                    break;
                case "string":
                    if (newD is string newString)
                    {
                        for (int i = 0; i < Math.Min(newString.Length, _latestData.D.Length); i++)
                        {
                            // Giả sử _latestData.D sẽ được mở rộng để lưu string nếu cần (có thể lưu từng ký tự hoặc độ dài)
                            if (newString[i] != _latestData.D[i])
                                return true;
                        }
                        return false;
                    }
                    break;

                    //    case "string":
                    //        if (newD is string newStr)
                    //        {
                    //            return newStr != _latestData.D_String;
                    //        }
                    //        break;
                    //}


            }

            return false;



        }
        private string DetectDataType(object data)
        {
            return data switch
            {
                short[] => "short",
                float[] => "float",
                double[] => "double",
                string => "string",
                _ => "short"   // mặc định
            };
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



            _polling.DUpdated += data =>
            {
                if (data == null) return;
                string dataType = DetectDataType(data);
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        UpdateDUI(data);
                    }));
                }
                else
                {
                    UpdateDUI(data);
                }
                lock (_latestData)
                {
                    bool changed = HasDataChanged(null, data, dataType);

                    // Cập nhật dữ liệu mới vào _latestData
                    UpdateLatestData(data, dataType);


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
        private void UpdateLatestData(object newD, string dataType)
        {
            switch (dataType.ToLower())
            {
                case "short":
                    if (newD is short[] shorts)
                        Array.Copy(shorts, _latestData.D, Math.Min(shorts.Length, _latestData.D.Length));
                    break;

                case "float":
                    if (newD is float[] floats)
                        Array.Copy(floats, _latestData.D_Float, Math.Min(floats.Length, _latestData.D_Float.Length));
                    break;

                case "double":
                    if (newD is double[] doubles)
                        Array.Copy(doubles, _latestData.D_Double, Math.Min(doubles.Length, _latestData.D_Double.Length));
                    break;

                case "string":
                    if (newD is string str)
                        _latestData.D_String = str;
                    break;
            }
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



        private void CheckMemory(int startAddr, int size)
        {
            for (int i = 0; i < size; i++)
            {
                if (memoryUsed[startAddr + i])
                {
                    throw new Exception($"Address {startAddr + i} already used!");
                }
            }

        }
        private void MarkMemory(int startAddr, int sizeInRegisters, string dataType)
        {
            if (sizeInRegisters <= 0) return;

            try
            {
                // Tạo danh sách key cần xóa trước (tránh modify collection khi enumerate)
                var keysToRemove = _occupiedRegions.Keys
                    .Where(k =>
                    {
                        var r = _occupiedRegions[k];
                        int regionEnd = r.StartAddr + r.Size - 1;
                        int newEnd = startAddr + sizeInRegisters - 1;

                        // Xóa nếu có chồng chéo
                        return !(regionEnd < startAddr || r.StartAddr > newEnd);
                    })
                    .ToList();   // ToList() rất quan trọng!

                // Xóa các vùng cũ
                foreach (var key in keysToRemove)
                {
                    _occupiedRegions.Remove(key);
                }

                // Thêm vùng mới
                _occupiedRegions[startAddr] = new MemoryRegion
                {
                    StartAddr = startAddr,
                    Size = sizeInRegisters,
                    DataType = dataType,
                    LastUpdated = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MarkMemory Error: {ex.Message}");
                // Không throw để không làm hỏng quá trình ghi
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
        private void UpdateDUI(object data, string dataType = "short")
        {
            if (data == null) return;

            Label[] dLabels =
            {
        y0, y1, y2, y3,
        y4, y5, y6, y7
    };

            try
            {
                switch (dataType.ToLower())
                {
                    case "short":
                    case "ushort":
                        short[] shorts = ByteHelper.ConvertToShortArray(data);
                        if (shorts != null && shorts.Length > 0)
                            UIHelper.UpdateShorts(dLabels, shorts);

                        break;


                    case "int":
                        int[] ints = ByteHelper.ConvertToIntArray(data);
                        if (ints != null && ints.Length > 0)
                            UIHelper.UpdateInts(dLabels, ints);
                        //else if (data is object[] objInts)

                        //    UIHelper.UpdateInts(dLabels, objInts.Select(o => Convert.ToInt32(o)).ToArray());
                        break;

                    case "float":
                        float[] floats = ByteHelper.ConvertToFloatArray(data);
                        if (floats != null && floats.Length > 0)
                            UIHelper.UpdateFloats(dLabels, floats);
                        break;

                    case "double":
                        double[] doubles = ByteHelper.ConvertToDoubleArray(data);
                        if (doubles != null && doubles.Length > 0)
                            UIHelper.UpdateDoubles(dLabels, doubles);
                        break;

                    case "string":
                        //string strValue = data as string ?? "";
                        //UIHelper.UpdateString(dLabels, strValue);
                        if (data is string strValue)
                        {
                            // 👉 1 chuỗi
                            UIHelper.UpdateString(dLabels, strValue);
                        }
                        else if (data is string[] arr)
                        {
                            // 👉 nhiều chuỗi
                            UIHelper.UpdateMultiString(dLabels, arr);
                        }
                        break;

                    default:
                        rtbLog.AppendText($"UpdateDUI: Kiểu dữ liệu '{dataType}' chưa được hỗ trợ\n");
                        break;
                }
            }
            catch (Exception ex)
            {
                rtbLog.AppendText($"Lỗi UpdateDUI ({dataType}): {ex.Message}\n");
            }
        }
        private async void btnRead_Click(object sender, EventArgs e)
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
            int count = (int)nudCount.Value;
            string selectedType = cbType.SelectedItem?.ToString() ?? "";
            if (IsSubAddress(startAddr, selectedType))
            {
                string message = selectedType switch
                {
                    "string" => $"Địa chỉ {type}{startAddr} KHÔNG phải là vị trí bắt đầu của chuỗi.\n" +
                                $"Vui lòng đọc tại địa chỉ bắt đầu của vùng String.",

                    "float" or "int" => $"Địa chỉ {type}{startAddr} là địa chỉ phụ của {selectedType}.\n" +
                                       $"Vui lòng đọc tại địa chỉ chẵn (ví dụ: D20 thay vì D21).",

                    "double" => $"Địa chỉ {type}{startAddr} là địa chỉ phụ của Double.\n" +
                               $"Vui lòng đọc tại địa chỉ chia hết cho 4.",

                    _ => $"Địa chỉ {type}{startAddr} có thể là địa chỉ phụ của {selectedType}."
                };

                rtbLog.AppendText(message + "\n");

                // Tùy chọn: Hỏi người dùng có muốn tự động điều chỉnh không
                if (MessageBox.Show(message + "\n\nBạn có muốn tự động điều chỉnh về địa chỉ hợp lệ không?",
                                    "Địa chỉ không hợp lệ",
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    startAddr = FindValidStartAddress(startAddr, selectedType);
                    nudStartAddress.Value = startAddr;   // cập nhật lại NumericUpDown
                    rtbLog.AppendText($"Đã điều chỉnh về địa chỉ: {type}{startAddr}\n");
                }
                else
                {
                    return;   // dừng lại, không đọc
                }
            }



            if (type == ElemType.X)
            {

                bool[] bits = _plc.ReadBits(type, startAddr, count);

                if (bits == null)
                {
                    rtbLog.AppendText("Không đọc được dữ liệu\n");
                    return;
                }
                if (selectedType != "bool")
                {
                    MessageBox.Show(
                        $"Vùng {type} chỉ hỗ trợ kiểu BOOL.\n" +
                        $"Không thể đọc kiểu {selectedType} tại {type}{startAddr}.",
                        "Sai kiểu dữ liệu",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );

                    return; // 🔥 QUAN TRỌNG: dừng luôn
                }
                // Chỉ cập nhật nếu đọc đúng vùng X0~X7 mà form đang hiển thị
                if (InvokeRequired)
                    Invoke(new Action(() => UpdateXUI(bits)));
                else
                    UpdateXUI(bits);
                rtbLog.AppendText("Read Bits: ");
                rtbLog.AppendText(string.Join(",", bits));
                rtbLog.AppendText("\n");
                return;
            }


            if (!Enum.TryParse<ElemType>(cmbElemType.SelectedItem?.ToString(), out var elemType))
                return;

            ModbusDataType dataType = GetModbusDataTypeFromCombo();
            object readData = null;
            string displayType = selectedType.ToLower();
            try
            {


                if (dataType == ModbusDataType.String)
                {
                    if (count != 1)
                    {

                        string[] strings = await _plc.ReadMultipleStringAsync(elemType, startAddr, count, 20);

                        int registersPerString = (20 + 1) / 2;


                        var lines = strings.Select((s, i) =>
                        $"D{startAddr + i * registersPerString}: [{s}]");

                        string displayText = string.Join(" | ", lines);
                        SafeUpdateDUI(strings, "string");                    // Nếu bạn muốn hiển thị nhiều chuỗi

                        rtbLog.AppendText(displayText);

                        if (strings.Length > 0)
                            txtValue.Text = strings[0];


                    }
                    else
                    {
                        string result = await _plc.ReadSingleAsync<string>(elemType, startAddr, dataType, 40);
                        readData = result;
                        SafeUpdateDUI(result, "string");
                        rtbLog.AppendText($"Read String [{elemType}{startAddr}]: {result}\n");
                    }

                }
                else if (count == 1)
                {
                    // Dùng object cho single value
                    var result = await _plc.ReadSingleAsync<object>(elemType, startAddr, dataType);
                    readData = result;
                    // txtValue.Text = result?.ToString() ?? "";
                    SafeUpdateDUI(result, selectedType.ToLower());
                    rtbLog.AppendText($"Read {dataType} [{elemType}{startAddr}]: {result}\n");
                }
                else
                {
                    // Dùng object[] cho multiple values
                    var results = await _plc.ReadMultipleAsync<object>(elemType, startAddr, count, dataType);
                    readData = results;
                    if (results != null)
                    {

                        string displayText = string.Join(", ", results.Select(r => r?.ToString() ?? "null"));

                        SafeUpdateDUI(results, selectedType.ToLower());
                        rtbLog.AppendText($"Read {count} {dataType}: {displayText}\n");
                    }
                    else
                    {
                        rtbLog.AppendText("Không đọc được dữ liệu\n");
                    }
                }


            }


            catch (Exception ex)
            {
                rtbLog.AppendText($"Lỗi đọc dữ liệu: {ex.Message}\n");
                if (ex.InnerException != null)
                    rtbLog.AppendText($"Inner: {ex.InnerException.Message}\n");
            }

        }


        private ModbusDataType GetModbusDataTypeFromCombo()
        {
            return cbType.SelectedItem?.ToString()?.ToLower() switch
            {
                "short" or "int16" => ModbusDataType.Int16,
                "ushort" or "uint16" => ModbusDataType.UInt16,
                "int" or "int32" => ModbusDataType.Int32,
                "float" => ModbusDataType.Float,
                "double" => ModbusDataType.Double,
                "string" => ModbusDataType.String,
                _ => ModbusDataType.Int16
            };
        }

       
        private async void btnWrite_Click(object sender, EventArgs e)
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

                    rtbLog.AppendText(result ? "Ghi bit thành công\n" : "Ghi bit thất bại\n");

                    // Nếu ghi thành công → đọc lại X để cập nhật UI và DB
                    if (result && type == ElemType.X)
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

                else
                {
                    string input = txtValue.Text.Trim();
                    string selectedType = cbType.SelectedItem.ToString();
                    int size = GetSizeByType(selectedType);
                    CheckMemory(startAddr, size);
                    if (string.IsNullOrEmpty(input))
                    {
                        rtbLog.AppendText("Giá trị không được để trống\n");
                        return;
                    }


                    bool success = false;
                    // === KIỂM TRA CHỒNG CHÉO ===

                    int alignedStart = AlignAddress(startAddr, selectedType);
                    bool isOverlap = IsRegionOverlapped(alignedStart, size, selectedType);
                    bool isSub = IsSubAddress(alignedStart, selectedType);

                    rtbLog.AppendText($"Overlap: {isOverlap}, SubAddress: {isSub}\n");
                    //rtbLog.AppendText($"[DEBUG] Align: D{originalStart} → D{alignedStart} ({selectedType})\n");
                    if (IsRegionOverlapped(alignedStart, size, selectedType) ||
                        IsSubAddress(alignedStart, selectedType))
                    {
                        string warningMsg = $"CẢNH BÁO NGHIÊM TRỌNG:\n\n" +
                                            $"Bạn đang cố ghi {selectedType} vào vùng D{alignedStart} ~ D{alignedStart + size - 1}\n" +
                                            $"Vùng này có thể đã bị chiếm bởi dữ liệu khác (ví dụ: float tại D20 chiếm cả D21).\n\n" +
                                            $"Tiếp tục ghi sẽ làm hỏng dữ liệu cũ.\n\n" +
                                            $"Vẫn muốn ghi?";

                        var result = MessageBox.Show(warningMsg, "CẢNH BÁO CHỒNG CHÉO",
                                                     MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                        if (result != DialogResult.Yes)
                        {
                            rtbLog.AppendText("Đã hủy ghi do phát hiện chồng chéo địa chỉ\n");
                            return;
                        }
                    }

                    if (selectedType == "short")
                    {
                        short[] values = input.Split(',')
                                              .Select(p => short.Parse(p.Trim()))
                                              .ToArray();

                        success = _plc.WriteWords(type, alignedStart, values);

                    }
                    else if (selectedType == "ushort")
                    {
                        ushort[] values = input.Split(',')
                                               .Select(p => ushort.Parse(p.Trim()))
                                               .ToArray();

                        success = _plc.WriteWords(type, alignedStart, values);

                    }
                    else if (selectedType == "int")
                    {
                        int[] values = input.Split(',')
                                            .Select(p => int.Parse(p.Trim()))
                                            .ToArray();

                        success = _plc.WriteWords(type, alignedStart, values);

                    }
                    else if (selectedType == "float")
                    {
                        float[] values = input.Split(',')
                                              .Select(p => float.Parse(p.Trim()))
                                              .ToArray();

                        success = _plc.WriteWords(type, alignedStart, values);

                    }
                    else if (selectedType == "double")
                    {
                        double[] values = input.Split(',')
                                               .Select(p => double.Parse(p.Trim()))
                                               .ToArray();
                        success = _plc.WriteWords(type, alignedStart, values);   // thêm dòng này
                    }

                    else if (selectedType == "string")
                    {
                        string strValue = txtValue.Text.Trim();

                        if (string.IsNullOrEmpty(strValue))
                        {
                            rtbLog.AppendText("Chuỗi không được để trống\n");
                            return;
                        }

                        success = _plc.WriteString(type, alignedStart, strValue, maxRegisters: 10);

                        if (success)
                        {
                            MarkMemory(startAddr, size, "string");
                            rtbLog.AppendText($"Ghi  thành công: \"{strValue}\" ({size} registers)\n");
                            await RefreshUIAfterWriteAsync(type, startAddr, selectedType);
                        }
                        // đọc lại để cập nhật UI và DB
                        else
                        {
                            rtbLog.AppendText($"Ghi {selectedType}thất bại\n");
                        }
                    }

                    if (success)
                    {

                        rtbLog.AppendText($"Ghi {selectedType} thành công tại D{startAddr}\n");



                        await RefreshUIAfterWriteAsync(type, alignedStart, selectedType);
                        int v = Guid.NewGuid().GetHashCode();
                        _occupiedRegions[v] = new MemoryRegion
                        {
                            StartAddr = alignedStart,
                            Size = size,
                            DataType = selectedType
                        };
                        rtbLog.AppendText($"Region count: {_occupiedRegions.Count}\n");
                    }
                    else
                    {
                        rtbLog.AppendText($"Ghi {selectedType} thất bại\n");
                    }
                }
            }
            catch (Exception ex)
            {
                rtbLog.AppendText("Lỗi parse dữ liệu: " + ex.Message + "\n");
            }
        }



        /// <summary>
        /// Cập nhật UI an toàn từ bất kỳ thread nào
        /// </summary>
        private void SafeUpdateDUI(object data, string dataType = "short")
        {
            if (data == null) return;

            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateDUI(data, dataType)));
            }
            else
            {
                UpdateDUI(data, dataType);
            }
        }
        /// <summary>
        /// Đọc lại dữ liệu sau khi ghi và cập nhật UI
        /// </summary>
        private async Task RefreshUIAfterWriteAsync(ElemType elemType, int startAddr, string dataType)
        {
            try
            {
                await Task.Delay(50); // chờ PLC xử lý xong (tùy PLC có thể tăng lên 100ms)

                if (dataType == "string")
                {
                    string value = await _plc.ReadSingleAsync<string>(elemType, startAddr, ModbusDataType.String);
                    SafeUpdateDUI(value, "string");
                    txtValue.Text = value ?? "";
                }
                else if (dataType == "double")
                {
                    var values = await _plc.ReadMultipleAsync<double>(elemType, startAddr, 1, ModbusDataType.Double);
                    SafeUpdateDUI(values, "double");
                }
                else if (dataType == "float")
                {
                    var values = await _plc.ReadMultipleAsync<float>(elemType, startAddr, 1, ModbusDataType.Float);
                    SafeUpdateDUI(values, "float");
                }
                else // short, ushort, int
                {
                    short[] values = _plc.ReadWords(elemType, startAddr, 8); // đọc 8 register để hiển thị
                    SafeUpdateDUI(values, "short");
                }
            }
            catch (Exception ex)
            {
                rtbLog.AppendText($"Không thể đọc lại sau ghi: {ex.Message}\n");
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

        private int GetSizeByType(string type, int stringLength = 0)
        {
            if (type.ToLower() == "string")
            {
                string str = txtValue.Text.Trim();
                int byteLength = Encoding.ASCII.GetByteCount(str);
                return (byteLength + 1) / 2 + 1;     // +1 để an toàn null terminator
            }
            return type switch
            {
                "short" => 1,
                "ushort" => 1,
                "int" => 2,
                "float" => 2,
                "double" => 4,
                "long" => 4,
                "ulong" => 4,


                _ => throw new Exception("Unknown type")
            };
        }
      
        private bool IsSubAddress(int startAddr, string dataType)
        {

            if (string.IsNullOrEmpty(dataType))
                return false;
            // Kiểm tra theo kiểu dữ liệu đang muốn ghi/đọc
            int alignment = dataType.ToLower() switch
            {
                "float" or "int" => 2,
                "double" => 4,
                "string" => 1,
                _ => 1
            };
            Debug.WriteLine($"{startAddr}");
            if (startAddr % alignment != 0)
                return true;        // địa chỉ không align đúng → là địa chỉ phụ

            // Kiểm tra xem địa chỉ này có nằm trong vùng đã được chiếm bởi kiểu dữ liệu khác không
            foreach (var region in _occupiedRegions.Values)
            {
                int regionEnd = region.StartAddr + region.Size - 1;

                if (startAddr >= region.StartAddr && startAddr <= regionEnd)
                {
                    if (region.DataType != dataType)
                    {
                        // Đang nằm trong vùng của kiểu khác → coi là địa chỉ phụ / nguy hiểm
                        return true;
                    }
                }
            }

            return false;
        }
        private int AlignAddress(int addr, string dataType)
        {
            return dataType.ToLower() switch
            {
                "int" or "float" => addr - (addr % 2),     // align theo 2 registers
                "double" => addr - (addr % 4),             // align theo 4 registers
                "string" => addr - (addr % 10),
                _ => addr
            };
        }

        private int FindValidStartAddress(int currentAddr, string dataType)
        {
            return dataType.ToLower() switch
            {
                "float" or "int" => currentAddr - (currentAddr % 2),
                "double" => currentAddr - (currentAddr % 4),
                "string" => FindNearestStringStart(currentAddr),   // tìm vùng string gần nhất
                _ => currentAddr
            };
        }

        private int FindNearestStringStart(int addr)
        {
            // Tìm vùng string gần nhất (có thể cải tiến sau)
            foreach (var kvp in _occupiedRegions.OrderBy(k => k.Key))
            {
                if (kvp.Value.DataType == "string" && kvp.Key <= addr)
                    return kvp.Key;
            }
            return addr; // nếu không tìm thấy thì giữ nguyên
        }
    }
}
