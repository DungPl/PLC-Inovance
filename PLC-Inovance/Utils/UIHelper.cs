using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC_Inovance.Utils
{
    internal class UIHelper
    {
        internal static void UpdateShorts(Label[] labels, short[] values)
        {
            if (values == null || labels == null) return;
        
            for
                (int i = 0; i < labels.Length ; i++)
            {
                if (i < values.Length)
                {
                    // Hiển thị giá trị thực tế
                    labels[i].Text = values[i].ToString();

                    // Đổi màu theo giá trị
                    if (values[i] == 0)
                        SetLabelStyle(labels[i], Color.LightGray, Color.Black);
                    else if (values[i] > 1000)
                        SetLabelStyle(labels[i], Color.Orange, Color.White);
                    else if (values[i] > 0)
                        SetLabelStyle(labels[i], Color.DodgerBlue, Color.White);
                    else
                        SetLabelStyle(labels[i], Color.Red, Color.White);
                }
                else
                {
                    // Nếu không có giá trị, hiển thị trống
                    labels[i].Text = "";
                    SetLabelStyle(labels[i], Color.LightGray, Color.Black);
                }
            }
        }

        internal static void UpdateInts(Label[] labels, int[] values)
        {
            if (values == null || labels == null) return;
            for (int i = 0; i < labels.Length && i < values.Length; i++)
            {
                if (i < values.Length)
                {
                    // Hiển thị giá trị thực tế
                    labels[i].Text = values[i].ToString();
                    // Đổi màu theo giá trị
                    if (values[i] == 0)
                        SetLabelStyle(labels[i], Color.LightGray, Color.Black);
                    else if (values[i] > 100000)
                        SetLabelStyle(labels[i], Color.Orange, Color.White);
                    else if (values[i] > 0)
                        SetLabelStyle(labels[i], Color.DodgerBlue, Color.White);
                    else
                        SetLabelStyle(labels[i], Color.Red, Color.White);
                }
                else
                {
                    // Nếu không có giá trị, hiển thị trống
                    labels[i].Text = "";
                    SetLabelStyle(labels[i], Color.LightGray, Color.Black);
                }
            }
        }

        internal static void UpdateFloats(Label[] labels, float[] values)
        {
            if (values == null || labels == null) return;

            for (int i = 0; i < labels.Length; i++)
            {
                if (i < values.Length)
                {
                    // Có dữ liệu → hiển thị giá trị
                    labels[i].Text = values[i].ToString("F2");     // 3 chữ số thập phân
                    SetLabelStyle(labels[i], Color.DodgerBlue, Color.White);
                }
                else
                {
                    // Không có dữ liệu (vượt quá số lượng đọc) → hiển thị trống
                    labels[i].Text = "-";
                    SetLabelStyle(labels[i], Color.LightGray, Color.Black);
                }
            }

        }

        internal static void UpdateDoubles(Label[] labels, double[] values)
        {
            if (values == null || labels == null) return;
            for (int i = 0;
                i < labels.Length && i < values.Length; i++)
            {
                labels[i].Text = values[i].ToString("F3");   // 3 chữ số thập phân
                                                             // Đổi màu theo giá trị
                if (i < values.Length)
                {
                    SetLabelStyle(labels[i], Color.Purple, Color.White);
                }
                else
                {
                    // Nếu không có giá trị, hiển thị trống
                    labels[i].Text = "";
                    SetLabelStyle(labels[i], Color.LightGray, Color.Black);
                }
            }
        }

        internal static void UpdateString(Label[] labels, string value)
        {
            // Hiển thị chuỗi vào label đầu tiên, các label sau để trống hoặc hiển thị độ dài
            if (labels.Length > 0)
            {
                labels[0].Text = string.IsNullOrEmpty(value) ? "(Empty)" : value;
                SetLabelStyle(labels[0], Color.MediumSeaGreen, Color.White);
            }

            // Các label còn lại có thể hiển thị thông tin bổ sung
            for (int i = 1; i < labels.Length; i++)
            {
                int realLength = string.IsNullOrEmpty(value) ? 0 : value.Length;
                labels[i].Text = i == 1 ? $"Len: {realLength}" : "";
                SetLabelStyle(labels[i], Color.LightGray, Color.Black);
            }
        }
        internal static void UpdateMultiString(Label[] labels, string[] values)
        {
            for (int i = 0; i < labels.Length; i++)
            {
                if (i < values.Length)
                {
                    labels[i].Text = values[i];
                }
                else
                {
                    labels[i].Text = "";
                }
            }
        }
        // Hàm tiện ích set màu
        static void SetLabelStyle(Label label, Color backColor, Color foreColor)
        {
            label.BackColor = backColor;
            label.ForeColor = foreColor;
        }
    }
}
