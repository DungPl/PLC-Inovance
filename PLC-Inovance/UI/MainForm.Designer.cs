namespace PLC_Inovance
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblStatus = new ToolStripStatusLabel();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            rtbLog = new RichTextBox();
            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            groupBox2 = new GroupBox();
            cbType = new ComboBox();
            label3 = new Label();
            cmbElemType = new ComboBox();
            nudCount = new NumericUpDown();
            nudStartAddress = new NumericUpDown();
            label10 = new Label();
            label9 = new Label();
            label8 = new Label();
            label7 = new Label();
            txtValue = new TextBox();
            btnRead = new Button();
            btnWrite = new Button();
            groupBox1 = new GroupBox();
            label2 = new Label();
            btnDisconnect = new Button();
            btnConnect = new Button();
            txtIP = new TextBox();
            groupBox3 = new GroupBox();
            y5 = new Label();
            y6 = new Label();
            y7 = new Label();
            x1 = new Label();
            x2 = new Label();
            x3 = new Label();
            x4 = new Label();
            x5 = new Label();
            x6 = new Label();
            x7 = new Label();
            y0 = new Label();
            y1 = new Label();
            y2 = new Label();
            y3 = new Label();
            y4 = new Label();
            x0 = new Label();
            label1 = new Label();
            dataLog = new DataGridView();
            btnLoadLog = new Button();
            statusStrip1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudCount).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudStartAddress).BeginInit();
            groupBox1.SuspendLayout();
            groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataLog).BeginInit();
            SuspendLayout();
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(0, 16);
            // 
            // statusStrip1
            // 
            statusStrip1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            statusStrip1.Dock = DockStyle.None;
            statusStrip1.ImageScalingSize = new Size(20, 20);
            statusStrip1.Items.AddRange(new ToolStripItem[] { lblStatus, toolStripStatusLabel1 });
            statusStrip1.Location = new Point(284, 75);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(17, 22);
            statusStrip1.TabIndex = 32;
            statusStrip1.Text = "statusStrip1";
            statusStrip1.TextDirection = ToolStripTextDirection.Vertical90;
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(0, 16);
            // 
            // rtbLog
            // 
            rtbLog.Location = new Point(2, 390);
            rtbLog.Name = "rtbLog";
            rtbLog.ReadOnly = true;
            rtbLog.Size = new Size(443, 120);
            rtbLog.TabIndex = 30;
            rtbLog.Text = "";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(cbType);
            groupBox2.Controls.Add(label3);
            groupBox2.Controls.Add(cmbElemType);
            groupBox2.Controls.Add(nudCount);
            groupBox2.Controls.Add(nudStartAddress);
            groupBox2.Controls.Add(label10);
            groupBox2.Controls.Add(label9);
            groupBox2.Controls.Add(label8);
            groupBox2.Controls.Add(label7);
            groupBox2.Controls.Add(txtValue);
            groupBox2.Controls.Add(btnRead);
            groupBox2.Controls.Add(btnWrite);
            groupBox2.Location = new Point(2, 118);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(440, 234);
            groupBox2.TabIndex = 29;
            groupBox2.TabStop = false;
            groupBox2.Text = "Đọc / ghi dữ liệu";
            // 
            // cbType
            // 
            cbType.FormattingEnabled = true;
            cbType.Items.AddRange(new object[] { "bool", "int", "short", "float", "double", "string" });
            cbType.Location = new Point(270, 106);
            cbType.Name = "cbType";
            cbType.Size = new Size(151, 28);
            cbType.TabIndex = 43;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(164, 30);
            label3.Name = "label3";
            label3.Size = new Size(81, 20);
            label3.TabIndex = 21;
            label3.Text = "Thanh ghi :";
            // 
            // cmbElemType
            // 
            cmbElemType.FormattingEnabled = true;
            cmbElemType.Items.AddRange(new object[] { "Y", "X", "S", "M", "B", "D", "R" });
            cmbElemType.Location = new Point(270, 26);
            cmbElemType.Name = "cmbElemType";
            cmbElemType.Size = new Size(151, 28);
            cmbElemType.TabIndex = 42;
            // 
            // nudCount
            // 
            nudCount.Location = new Point(270, 64);
            nudCount.Name = "nudCount";
            nudCount.Size = new Size(97, 27);
            nudCount.TabIndex = 37;
            // 
            // nudStartAddress
            // 
            nudStartAddress.Location = new Point(283, 195);
            nudStartAddress.Name = "nudStartAddress";
            nudStartAddress.Size = new Size(125, 27);
            nudStartAddress.TabIndex = 36;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(164, 109);
            label10.Name = "label10";
            label10.Size = new Size(95, 20);
            label10.TabIndex = 21;
            label10.Text = "Kiểu dữ liệu :";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(164, 150);
            label9.Name = "label9";
            label9.Size = new Size(81, 20);
            label9.TabIndex = 20;
            label9.Text = "Giá trị ghi :";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(169, 71);
            label8.Name = "label8";
            label8.Size = new Size(76, 20);
            label8.TabIndex = 17;
            label8.Text = "Số lượng :";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(155, 197);
            label7.Name = "label7";
            label7.Size = new Size(117, 20);
            label7.TabIndex = 1;
            label7.Text = "Địa chỉ bắt đầu :";
            // 
            // txtValue
            // 
            txtValue.Location = new Point(283, 150);
            txtValue.Name = "txtValue";
            txtValue.Size = new Size(125, 27);
            txtValue.TabIndex = 16;
            // 
            // btnRead
            // 
            btnRead.Location = new Point(8, 30);
            btnRead.Name = "btnRead";
            btnRead.Size = new Size(94, 29);
            btnRead.TabIndex = 5;
            btnRead.Text = "Read";
            btnRead.UseVisualStyleBackColor = true;
            btnRead.Click += btnRead_Click;
            // 
            // btnWrite
            // 
            btnWrite.Location = new Point(8, 83);
            btnWrite.Name = "btnWrite";
            btnWrite.Size = new Size(94, 29);
            btnWrite.TabIndex = 6;
            btnWrite.Text = "Write";
            btnWrite.UseVisualStyleBackColor = true;
            btnWrite.Click += btnWrite_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(statusStrip1);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(btnDisconnect);
            groupBox1.Controls.Add(btnConnect);
            groupBox1.Controls.Add(txtIP);
            groupBox1.Location = new Point(12, 0);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(345, 118);
            groupBox1.TabIndex = 28;
            groupBox1.TabStop = false;
            groupBox1.Text = "Kết nối PLC";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(6, 33);
            label2.Name = "label2";
            label2.Size = new Size(99, 20);
            label2.TabIndex = 11;
            label2.Text = "Địa chỉ Ip PLC";
            // 
            // btnDisconnect
            // 
            btnDisconnect.Location = new Point(158, 70);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(94, 29);
            btnDisconnect.TabIndex = 4;
            btnDisconnect.Text = "Disconnect";
            btnDisconnect.UseVisualStyleBackColor = true;
            btnDisconnect.Click += btnDisconnect_Click;
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(6, 70);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(94, 29);
            btnConnect.TabIndex = 3;
            btnConnect.Text = "Connect";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // txtIP
            // 
            txtIP.Location = new Point(158, 30);
            txtIP.Name = "txtIP";
            txtIP.Size = new Size(169, 27);
            txtIP.TabIndex = 0;
            txtIP.Text = "192.168.1.100";
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(y5);
            groupBox3.Controls.Add(y6);
            groupBox3.Controls.Add(y7);
            groupBox3.Controls.Add(x1);
            groupBox3.Controls.Add(x2);
            groupBox3.Controls.Add(x3);
            groupBox3.Controls.Add(x4);
            groupBox3.Controls.Add(x5);
            groupBox3.Controls.Add(x6);
            groupBox3.Controls.Add(x7);
            groupBox3.Controls.Add(y0);
            groupBox3.Controls.Add(y1);
            groupBox3.Controls.Add(y2);
            groupBox3.Controls.Add(y3);
            groupBox3.Controls.Add(y4);
            groupBox3.Controls.Add(x0);
            groupBox3.Location = new Point(564, 12);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(247, 475);
            groupBox3.TabIndex = 31;
            groupBox3.TabStop = false;
            groupBox3.Text = "I/O";
            // 
            // y5
            // 
            y5.BackColor = Color.Silver;
            y5.BorderStyle = BorderStyle.Fixed3D;
            y5.Font = new Font("Microsoft Sans Serif", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            y5.Location = new Point(166, 327);
            y5.Name = "y5";
            y5.Size = new Size(53, 40);
            y5.TabIndex = 15;
            y5.Text = "Y5";
            y5.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // y6
            // 
            y6.BackColor = Color.Silver;
            y6.BorderStyle = BorderStyle.Fixed3D;
            y6.Font = new Font("Microsoft Sans Serif", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            y6.Location = new Point(166, 380);
            y6.Name = "y6";
            y6.Size = new Size(53, 40);
            y6.TabIndex = 14;
            y6.Text = "Y6";
            y6.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // y7
            // 
            y7.BackColor = Color.Silver;
            y7.BorderStyle = BorderStyle.Fixed3D;
            y7.Font = new Font("Microsoft Sans Serif", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            y7.Location = new Point(166, 434);
            y7.Name = "y7";
            y7.Size = new Size(53, 40);
            y7.TabIndex = 13;
            y7.Text = "Y7";
            y7.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // x1
            // 
            x1.BackColor = Color.Silver;
            x1.BorderStyle = BorderStyle.Fixed3D;
            x1.Font = new Font("Microsoft Sans Serif", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            x1.Location = new Point(26, 99);
            x1.Name = "x1";
            x1.Size = new Size(53, 40);
            x1.TabIndex = 12;
            x1.Text = "X1";
            x1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // x2
            // 
            x2.BackColor = Color.Silver;
            x2.BorderStyle = BorderStyle.Fixed3D;
            x2.Font = new Font("Microsoft Sans Serif", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            x2.Location = new Point(26, 154);
            x2.Name = "x2";
            x2.Size = new Size(53, 40);
            x2.TabIndex = 11;
            x2.Text = "X2";
            x2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // x3
            // 
            x3.BackColor = Color.Silver;
            x3.BorderStyle = BorderStyle.Fixed3D;
            x3.Font = new Font("Microsoft Sans Serif", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            x3.Location = new Point(26, 214);
            x3.Name = "x3";
            x3.Size = new Size(53, 40);
            x3.TabIndex = 10;
            x3.Text = "X3";
            x3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // x4
            // 
            x4.BackColor = Color.Silver;
            x4.BorderStyle = BorderStyle.Fixed3D;
            x4.Font = new Font("Microsoft Sans Serif", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            x4.Location = new Point(26, 271);
            x4.Name = "x4";
            x4.Size = new Size(53, 40);
            x4.TabIndex = 9;
            x4.Text = "X4";
            x4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // x5
            // 
            x5.BackColor = Color.Silver;
            x5.BorderStyle = BorderStyle.Fixed3D;
            x5.Font = new Font("Microsoft Sans Serif", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            x5.Location = new Point(26, 327);
            x5.Name = "x5";
            x5.Size = new Size(53, 40);
            x5.TabIndex = 8;
            x5.Text = "X5";
            x5.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // x6
            // 
            x6.BackColor = Color.Silver;
            x6.BorderStyle = BorderStyle.Fixed3D;
            x6.Font = new Font("Microsoft Sans Serif", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            x6.Location = new Point(26, 380);
            x6.Name = "x6";
            x6.Size = new Size(53, 40);
            x6.TabIndex = 7;
            x6.Text = "X6";
            x6.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // x7
            // 
            x7.BackColor = Color.Silver;
            x7.BorderStyle = BorderStyle.Fixed3D;
            x7.Font = new Font("Microsoft Sans Serif", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            x7.Location = new Point(26, 434);
            x7.Name = "x7";
            x7.Size = new Size(53, 40);
            x7.TabIndex = 6;
            x7.Text = "X7";
            x7.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // y0
            // 
            y0.BackColor = Color.Silver;
            y0.BorderStyle = BorderStyle.Fixed3D;
            y0.Font = new Font("Microsoft Sans Serif", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            y0.Location = new Point(166, 36);
            y0.Name = "y0";
            y0.Size = new Size(53, 40);
            y0.TabIndex = 5;
            y0.Text = "Y0";
            y0.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // y1
            // 
            y1.BackColor = Color.Silver;
            y1.BorderStyle = BorderStyle.Fixed3D;
            y1.Font = new Font("Microsoft Sans Serif", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            y1.Location = new Point(166, 99);
            y1.Name = "y1";
            y1.Size = new Size(53, 40);
            y1.TabIndex = 4;
            y1.Text = "Y1";
            y1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // y2
            // 
            y2.BackColor = Color.Silver;
            y2.BorderStyle = BorderStyle.Fixed3D;
            y2.Font = new Font("Microsoft Sans Serif", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            y2.Location = new Point(166, 159);
            y2.Name = "y2";
            y2.Size = new Size(53, 40);
            y2.TabIndex = 3;
            y2.Text = "Y2";
            y2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // y3
            // 
            y3.BackColor = Color.Silver;
            y3.BorderStyle = BorderStyle.Fixed3D;
            y3.Font = new Font("Microsoft Sans Serif", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            y3.Location = new Point(166, 214);
            y3.Name = "y3";
            y3.Size = new Size(53, 40);
            y3.TabIndex = 2;
            y3.Text = "Y3";
            y3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // y4
            // 
            y4.BackColor = Color.Silver;
            y4.BorderStyle = BorderStyle.Fixed3D;
            y4.Font = new Font("Microsoft Sans Serif", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            y4.Location = new Point(166, 271);
            y4.Name = "y4";
            y4.Size = new Size(53, 40);
            y4.TabIndex = 1;
            y4.Text = "Y4";
            y4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // x0
            // 
            x0.BackColor = Color.Silver;
            x0.BorderStyle = BorderStyle.Fixed3D;
            x0.Font = new Font("Microsoft Sans Serif", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            x0.Location = new Point(26, 36);
            x0.Name = "x0";
            x0.Size = new Size(53, 40);
            x0.TabIndex = 0;
            x0.Text = "X0";
            x0.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 367);
            label1.Name = "label1";
            label1.Size = new Size(60, 20);
            label1.TabIndex = 27;
            label1.Text = "Kết quả";
            // 
            // dataLog
            // 
            dataLog.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataLog.Location = new Point(2, 584);
            dataLog.Name = "dataLog";
            dataLog.RowHeadersWidth = 51;
            dataLog.Size = new Size(835, 188);
            dataLog.TabIndex = 32;
            // 
            // btnLoadLog
            // 
            btnLoadLog.Location = new Point(376, 537);
            btnLoadLog.Name = "btnLoadLog";
            btnLoadLog.Size = new Size(94, 29);
            btnLoadLog.TabIndex = 33;
            btnLoadLog.Text = "Load Log";
            btnLoadLog.UseVisualStyleBackColor = true;
            btnLoadLog.Click += btnLoadLog_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(878, 795);
            Controls.Add(btnLoadLog);
            Controls.Add(dataLog);
            Controls.Add(rtbLog);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(groupBox3);
            Controls.Add(label1);
            Name = "MainForm";
            Text = "MainForm";
            Load += MainForm_Load;
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudCount).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudStartAddress).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataLog).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStripStatusLabel lblStatus;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private RichTextBox rtbLog;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private GroupBox groupBox2;
        private ComboBox cbType;
        private Label label3;
        private ComboBox cmbElemType;
        private NumericUpDown nudCount;
        private NumericUpDown nudStartAddress;
        private Label label10;
        private Label label9;
        private Label label8;
        private Label label7;
        private TextBox txtValue;
        private Button btnRead;
        private Button btnWrite;
        private GroupBox groupBox1;
        private Label label2;
        private Button btnDisconnect;
        private Button btnConnect;
        private TextBox txtIP;
        private GroupBox groupBox3;
        private Label y5;
        private Label y6;
        private Label y7;
        private Label x1;
        private Label x2;
        private Label x3;
        private Label x4;
        private Label x5;
        private Label x6;
        private Label x7;
        private Label y0;
        private Label y1;
        private Label y2;
        private Label y3;
        private Label y4;
        private Label x0;
        private Label label1;
        private DataGridView dataLog;
        private Button btnLoadLog;
    }
}
