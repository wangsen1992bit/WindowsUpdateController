using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsUpdateController
{
    public sealed class MainForm : Form
    {
        private Label lblState;
        private Label lblDetail;
        private Label lblLock;
        private NumericUpDown numDays;
        private CheckBox chkLock;
        private Button btnPause;
        private Button btnResume;
        private TextBox txtLog;

        private static readonly Color AccentColor = Color.FromArgb(0, 120, 212);
        private static readonly Color CardColor = Color.FromArgb(246, 248, 251);
        private static readonly Color OkColor = Color.FromArgb(0, 150, 80);
        private static readonly Color WarnColor = Color.FromArgb(196, 112, 0);
        private static readonly Color MutedColor = Color.FromArgb(120, 120, 120);

        public MainForm()
        {
            BuildUi();
            RefreshStatus(true);
        }

        private void BuildUi()
        {
            Text = "Windows 更新控制器";
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.White;
            ClientSize = new Size(560, 540);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Font;

            // ---- 标题 ----
            Label title = new Label();
            title.Text = "Windows 更新控制器";
            title.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            title.AutoSize = true;
            title.Location = new Point(20, 12);
            Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "官方软禁用 · 只改注册表,不关服务 · 可随时恢复";
            subtitle.ForeColor = MutedColor;
            subtitle.AutoSize = true;
            subtitle.Location = new Point(21, 46);
            Controls.Add(subtitle);

            // ---- 状态卡片 ----
            Panel card = new Panel();
            card.Location = new Point(20, 76);
            card.Size = new Size(518, 108);
            card.BackColor = CardColor;
            card.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(card);

            Label cardTitle = new Label();
            cardTitle.Text = "当前状态";
            cardTitle.ForeColor = MutedColor;
            cardTitle.AutoSize = true;
            cardTitle.Location = new Point(14, 8);
            card.Controls.Add(cardTitle);

            lblState = new Label();
            lblState.Text = "读取中...";
            lblState.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            lblState.AutoSize = true;
            lblState.Location = new Point(14, 26);
            card.Controls.Add(lblState);

            lblDetail = new Label();
            lblDetail.Text = string.Empty;
            lblDetail.AutoSize = true;
            lblDetail.Location = new Point(14, 58);
            card.Controls.Add(lblDetail);

            lblLock = new Label();
            lblLock.Text = string.Empty;
            lblLock.AutoSize = true;
            lblLock.Location = new Point(14, 80);
            card.Controls.Add(lblLock);

            // ---- 暂停天数 ----
            Label lblDays = new Label();
            lblDays.Text = "暂停天数";
            lblDays.AutoSize = true;
            lblDays.Location = new Point(20, 200);
            Controls.Add(lblDays);

            numDays = new NumericUpDown();
            numDays.Location = new Point(86, 196);
            numDays.Size = new Size(84, 25);
            numDays.Minimum = 1;
            numDays.Maximum = 3650;
            numDays.Value = 365;
            Controls.Add(numDays);

            Label lblUnit = new Label();
            lblUnit.Text = "天（建议 365，最大 3650）";
            lblUnit.ForeColor = MutedColor;
            lblUnit.AutoSize = true;
            lblUnit.Location = new Point(178, 200);
            Controls.Add(lblUnit);

            chkLock = new CheckBox();
            chkLock.Text = "同时锁定当前版本（阻止跨大版本升级）";
            chkLock.AutoSize = true;
            chkLock.Checked = true;
            chkLock.Location = new Point(20, 228);
            Controls.Add(chkLock);

            // ---- 按钮 ----
            btnPause = new Button();
            btnPause.Text = "暂停并锁定版本";
            btnPause.Location = new Point(20, 260);
            btnPause.Size = new Size(150, 36);
            btnPause.FlatStyle = FlatStyle.Flat;
            btnPause.FlatAppearance.BorderSize = 0;
            btnPause.BackColor = AccentColor;
            btnPause.ForeColor = Color.White;
            btnPause.Cursor = Cursors.Hand;
            btnPause.Click += new EventHandler(OnPauseClick);
            Controls.Add(btnPause);

            btnResume = new Button();
            btnResume.Text = "恢复更新";
            btnResume.Location = new Point(178, 260);
            btnResume.Size = new Size(110, 36);
            btnResume.FlatStyle = FlatStyle.Flat;
            btnResume.FlatAppearance.BorderSize = 1;
            btnResume.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnResume.BackColor = Color.FromArgb(240, 240, 240);
            btnResume.ForeColor = Color.FromArgb(40, 40, 40);
            btnResume.Cursor = Cursors.Hand;
            btnResume.Click += new EventHandler(OnResumeClick);
            Controls.Add(btnResume);

            Button btnRefresh = new Button();
            btnRefresh.Text = "刷新状态";
            btnRefresh.Location = new Point(296, 260);
            btnRefresh.Size = new Size(100, 36);
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderSize = 1;
            btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnRefresh.BackColor = Color.FromArgb(240, 240, 240);
            btnRefresh.ForeColor = Color.FromArgb(40, 40, 40);
            btnRefresh.Cursor = Cursors.Hand;
            btnRefresh.Click += new EventHandler(OnRefreshClick);
            Controls.Add(btnRefresh);

            Button btnOpen = new Button();
            btnOpen.Text = "更新设置";
            btnOpen.Location = new Point(404, 260);
            btnOpen.Size = new Size(112, 36);
            btnOpen.FlatStyle = FlatStyle.Flat;
            btnOpen.FlatAppearance.BorderSize = 1;
            btnOpen.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnOpen.BackColor = Color.FromArgb(240, 240, 240);
            btnOpen.ForeColor = Color.FromArgb(40, 40, 40);
            btnOpen.Cursor = Cursors.Hand;
            btnOpen.Click += new EventHandler(OnOpenSettingsClick);
            Controls.Add(btnOpen);

            // ---- 日志 ----
            Label lblLog = new Label();
            lblLog.Text = "操作日志";
            lblLog.AutoSize = true;
            lblLog.Location = new Point(20, 310);
            Controls.Add(lblLog);

            txtLog = new TextBox();
            txtLog.Location = new Point(20, 332);
            txtLog.Size = new Size(518, 182);
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.BorderStyle = BorderStyle.FixedSingle;
            txtLog.BackColor = Color.FromArgb(250, 250, 250);
            txtLog.Font = new Font("Consolas", 9F);
            Controls.Add(txtLog);
        }

        private void RefreshStatus(bool writeHeader)
        {
            try
            {
                UpdateStatus s = UpdateManager.GetStatus();

                if (writeHeader)
                {
                    Log("程序启动 · 已获得管理员权限");
                    Log(string.Format("系统:Windows 11 {0}  (Build {1}  {2})", s.WindowsVersion, s.BuildNumber, s.Edition));
                }

                if (s.PauseActive)
                {
                    lblState.Text = "已暂停";
                    lblState.ForeColor = OkColor;
                    double remainDays = (s.PauseEnd.Value - DateTime.Now).TotalDays;
                    lblDetail.Text = string.Format("截止 {0:yyyy-MM-dd HH:mm} ，剩余约 {1} 天",
                        s.PauseEnd.Value, Math.Round(remainDays, 1));
                }
                else if (s.PauseExpired)
                {
                    lblState.Text = "暂停已到期";
                    lblState.ForeColor = WarnColor;
                    lblDetail.Text = string.Format("原截止 {0:yyyy-MM-dd HH:mm} ，系统已恢复正常更新", s.PauseEnd.Value);
                }
                else
                {
                    lblState.Text = "未暂停";
                    lblState.ForeColor = MutedColor;
                    lblDetail.Text = "系统会正常下载并安装更新";
                }

                if (s.VersionLocked)
                {
                    lblLock.Text = string.Format("版本锁定：已锁定 {0}（不会跨大版本升级）", s.LockedVersion);
                    lblLock.ForeColor = OkColor;
                }
                else
                {
                    lblLock.Text = "版本锁定：未锁定";
                    lblLock.ForeColor = MutedColor;
                }

                if (s.PauseActive && s.MaxPauseDays.HasValue)
                {
                    lblDetail.Text += string.Format("，天数上限 {0}", s.MaxPauseDays.Value);
                }
            }
            catch (Exception ex)
            {
                Log("读取状态失败：" + ex.Message);
            }
        }

        private void OnPauseClick(object sender, EventArgs e)
        {
            int days = (int)numDays.Value;
            try
            {
                UpdateManager.Pause(days, chkLock.Checked);
                RefreshStatus(false);

                string msg;
                if (chkLock.Checked)
                {
                    msg = string.Format("已暂停 {0} 天，并锁定版本 {1}。", days, UpdateManager.GetStatus().LockedVersion);
                }
                else
                {
                    msg = string.Format("已暂停 {0} 天。", days);
                }

                Log(msg.Replace("。", string.Empty));
                MessageBox.Show(this, msg, "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log("暂停失败：" + ex.Message);
                MessageBox.Show(this, ex.Message, "暂停失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnResumeClick(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(this,
                "确认取消暂停并移除版本锁定？\r\n之后系统会正常接收所有更新（含大版本升级）。",
                "恢复更新", MessageBoxButtons.OKCancel, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.OK)
            {
                return;
            }

            try
            {
                UpdateManager.Resume();
                RefreshStatus(false);
                Log("已恢复:暂停状态与版本锁定均已清除");
                MessageBox.Show(this, "已恢复。可到 设置 > Windows 更新 点“检查更新”验证。",
                    "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log("恢复失败：" + ex.Message);
                MessageBox.Show(this, ex.Message, "恢复失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnRefreshClick(object sender, EventArgs e)
        {
            RefreshStatus(false);
            Log("已刷新状态");
        }

        private void OnOpenSettingsClick(object sender, EventArgs e)
        {
            try
            {
                Process.Start("ms-settings:windowsupdate");
            }
            catch (Exception ex)
            {
                Log("打开设置失败：" + ex.Message);
            }
        }

        private void Log(string message)
        {
            txtLog.AppendText(string.Format("[{0:HH:mm:ss}] {1}{2}", DateTime.Now, message, Environment.NewLine));
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }
    }
}
