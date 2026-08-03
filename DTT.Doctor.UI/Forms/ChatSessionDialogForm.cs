using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using DTT.Doctor.Services.Core;
using DTT.Doctor.Services.Models;
using DTT.Doctor.UI.Controls;
using DTT.Doctor.UI.Theme;
using Panel = System.Windows.Forms.Panel;
using Button = System.Windows.Forms.Button;
using Timer = System.Windows.Forms.Timer;

namespace DTT.Doctor.UI.Forms
{
    /// <summary>
    /// Cửa sổ Lễ tân tiếp nhận + trả lời trực tiếp 1 phiên chat đã Escalated.
    /// Mở dialog → tự động Claim (gán assigned_staff_id = lễ tân hiện tại) → 
    /// tải lịch sử + polling 3s để bắt tin nhắn mới của bệnh nhân → gửi trả lời → Đóng phiên khi hoàn tất.
    /// </summary>
    public class ChatSessionDialogForm : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        private const int EM_SETMARGINS = 0xD3;
        private const int EC_LEFTMARGIN = 0x0001;
        private const int EC_RIGHTMARGIN = 0x0002;
        private const int EM_SETCUEBANNER = 0x1501;

        [DllImport("user32.dll")]
        private static extern bool HideCaret(IntPtr hWnd);

        private readonly ApiService _api = new ApiService();
        private readonly ChatQueueItem _item;
        private readonly Action<ChatQueueItem>? _onBookAppointment;

        private RichTextBox _rtbTranscript;
        private TextBox _txtReply;
        private RoundedButton _btnSend;
        private RoundedButton _btnCloseSession;
        private RoundedButton _btnBookAppointment;
        private RoundedButton _btnExit;
        private Label _lblHeaderStatus;
        private Label _lblSubStatus;
        private Panel _pnlStatusBadge;
        private Label _lblFooterStatus;

        private Timer _pollTimer;
        private string _sessionStatus = "Escalated";
        private bool _claimed = false;

        public bool WasModified { get; private set; } = false;

        // onBookAppointment: gọi khi lễ tân bấm "Đặt lịch cho BN này" — form cha (ReceptionCashierForm)
        // xử lý chuyển tab + điền sẵn thông tin, dialog này không biết/không cần biết chi tiết đó.
        public ChatSessionDialogForm(ChatQueueItem item, Action<ChatQueueItem>? onBookAppointment = null)
        {
            _item = item;
            _onBookAppointment = onBookAppointment;
            InitializeComponent();
            this.Shown += async (s, e) => await ClaimAndLoadAsync();
            this.FormClosed += (s, e) => { _pollTimer?.Stop(); _pollTimer?.Dispose(); };
        }

        private void InitializeComponent()
        {
            Text = $"💬 Tư Vấn Trực Tiếp — {_item.PatientName}";
            Size = new Size(760, 720);
            MinimumSize = new Size(700, 640);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ClinicalColors.GhostWhite;
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);
            DoubleBuffered = true;

            // ── Header Panel (Height = 92px) ─────────────────────────────────
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Color.White };
            Panel pnlHeaderBorder = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = ClinicalColors.BorderGray };
            pnlHeader.Controls.Add(pnlHeaderBorder);

            // 1. Avatar Circle (Left = 18px)
            Panel pnlAvatar = new Panel
            {
                Size = new Size(48, 48),
                Location = new Point(18, 22),
                BackColor = Color.Transparent
            };
            pnlAvatar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, pnlAvatar.Width - 1, pnlAvatar.Height - 1);
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(rect);
                    using (var brush = new LinearGradientBrush(rect, Color.FromArgb(79, 70, 229), Color.FromArgb(99, 102, 241), 45f))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }
                string initials = GetPatientInitials(_item.PatientName);
                TextRenderer.DrawText(e.Graphics, initials, ClinicalColors.GetMainFont(11f, FontStyle.Bold), rect, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            // 2. Patient Name & Specialty Chip (Left = 78px)
            Label lblPatient = new Label
            {
                Text = _item.PatientName,
                Font = ClinicalColors.GetMainFont(13.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(78, 18),
                Size = new Size(330, 28),
                AutoEllipsis = true,
                UseMnemonic = false
            };

            string specText = string.IsNullOrWhiteSpace(_item.SuggestedSpecialtyName)
                ? "✦ AI chưa xác định chuyên khoa"
                : $"✦ AI gợi ý chuyên khoa: {_item.SuggestedSpecialtyName}";

            Label lblSpecChip = new Label
            {
                Text = specText,
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(67, 56, 202),
                BackColor = Color.FromArgb(238, 242, 255),
                Location = new Point(78, 50),
                AutoSize = true,
                Padding = new Padding(8, 4, 8, 4),
                UseMnemonic = false
            };

            // 3. Status Badge Panel (Top-Right)
            _pnlStatusBadge = new Panel
            {
                Size = new Size(215, 44),
                Location = new Point(435, 22),
                BackColor = Color.FromArgb(254, 243, 199) // Amber default
            };
            _pnlStatusBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, _pnlStatusBadge.Width - 1, _pnlStatusBadge.Height - 1);
                Color borderColor = _sessionStatus == "Closed" ? Color.FromArgb(203, 213, 225)
                    : _claimed ? Color.FromArgb(167, 243, 208) : Color.FromArgb(253, 230, 138);
                using (var path = CreateRoundedPath(rect, 8))
                {
                    using (var pen = new Pen(borderColor, 1.2f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            _lblHeaderStatus = new Label
            {
                Text = "⏳ Đang gán phiên...",
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 83, 9),
                Location = new Point(8, 5),
                Size = new Size(198, 18),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            _lblSubStatus = new Label
            {
                Text = "Vui lòng chờ giây lát",
                Font = ClinicalColors.GetMainFont(8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(217, 119, 6),
                Location = new Point(8, 23),
                Size = new Size(198, 16),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            _pnlStatusBadge.Controls.Add(_lblHeaderStatus);
            _pnlStatusBadge.Controls.Add(_lblSubStatus);

            pnlHeader.Controls.Add(pnlAvatar);
            pnlHeader.Controls.Add(lblPatient);
            pnlHeader.Controls.Add(lblSpecChip);
            pnlHeader.Controls.Add(_pnlStatusBadge);

            // ── Body Panel (Transcript Box) ──────────────────────────────────
            Panel pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 14, 18, 14) };

            Panel pnlTranscriptWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };
            pnlTranscriptWrapper.Paint += (s, e) =>
            {
                Rectangle rect = new Rectangle(0, 0, pnlTranscriptWrapper.Width - 1, pnlTranscriptWrapper.Height - 1);
                using (var pen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawRectangle(pen, rect);
                }
            };

            _rtbTranscript = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Cursor = Cursors.Default
            };
            _rtbTranscript.GotFocus += (s, e) =>
            {
                HideCaret(_rtbTranscript.Handle);
                if (_txtReply != null && _txtReply.Enabled)
                {
                    _txtReply.Focus();
                }
            };
            _rtbTranscript.SelectionChanged += (s, e) => HideCaret(_rtbTranscript.Handle);
            _rtbTranscript.MouseDown += (s, e) => HideCaret(_rtbTranscript.Handle);
            _rtbTranscript.MouseUp += (s, e) => HideCaret(_rtbTranscript.Handle);
            pnlTranscriptWrapper.Controls.Add(_rtbTranscript);
            pnlBody.Controls.Add(pnlTranscriptWrapper);

            // ── Footer Panel (Reply Input & Action Buttons) ──────────────────
            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 165, BackColor = Color.White };
            Panel pnlFooterBorder = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = ClinicalColors.BorderGray };
            pnlFooter.Controls.Add(pnlFooterBorder);

            // Input Container with focus border highlight & rounded path
            bool isInputFocused = false;
            Panel pnlInputContainer = new Panel
            {
                Location = new Point(18, 14),
                Size = new Size(724, 66),
                BackColor = Color.White,
                Padding = new Padding(10, 8, 10, 8)
            };
            pnlInputContainer.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, pnlInputContainer.Width - 1, pnlInputContainer.Height - 1);
                Color borderColor = isInputFocused ? ClinicalColors.PrimaryBlue : Color.FromArgb(203, 213, 225);
                using (var path = CreateRoundedPath(rect, 8))
                {
                    using (var pen = new Pen(borderColor, isInputFocused ? 1.8f : 1f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            _txtReply = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                AcceptsReturn = false,
                BorderStyle = BorderStyle.None,
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                Enabled = false,
                ScrollBars = ScrollBars.None
            };
            _txtReply.GotFocus += (s, e) => { isInputFocused = true; pnlInputContainer.Invalidate(); };
            _txtReply.LostFocus += (s, e) => { isInputFocused = false; pnlInputContainer.Invalidate(); };
            _txtReply.PreviewKeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.IsInputKey = true;
                }
            };
            _txtReply.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    _ = OnSendClickAsync();
                }
                else if (e.KeyCode == Keys.Enter && e.Shift)
                {
                    int selStart = _txtReply.SelectionStart;
                    _txtReply.Text = _txtReply.Text.Insert(selStart, Environment.NewLine);
                    _txtReply.SelectionStart = selStart + Environment.NewLine.Length;
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                }
            };

            pnlInputContainer.Controls.Add(_txtReply);

            _lblFooterStatus = new Label
            {
                Text = "",
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(18, 85),
                Size = new Size(400, 20),
                UseMnemonic = false
            };

            // Buttons Bar (Y = 110)
            _btnCloseSession = new RoundedButton
            {
                Text = "✅  Đóng Phiên",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 185, 129),
                HoverBackColor = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                BorderRadius = 8,
                Size = new Size(150, 40),
                Location = new Point(18, 110),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _btnCloseSession.Click += async (s, e) => await OnCloseSessionClickAsync();

            // Đặt lịch trực tiếp cho bệnh nhân đang chat — nhảy sang tab "Khám Trực Tiếp" của
            // ReceptionCashierForm, tự điền sẵn hồ sơ + chuyên khoa AI đã gợi ý. Dialog giờ không còn
            // modal (Show() thay vì ShowDialog()) nên lễ tân vẫn giữ được cửa sổ chat trong lúc đặt lịch.
            _btnBookAppointment = new RoundedButton
            {
                Text = "📅  Đặt Lịch Cho BN Này",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(79, 70, 229),
                HoverBackColor = Color.FromArgb(67, 56, 202),
                ForeColor = Color.White,
                BorderRadius = 8,
                Size = new Size(190, 40),
                Location = new Point(178, 110),
                Cursor = Cursors.Hand
            };
            _btnBookAppointment.Click += (s, e) => _onBookAppointment?.Invoke(_item);

            _btnExit = new RoundedButton
            {
                Text = "Đóng cửa sổ",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(241, 245, 249),
                HoverBackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(71, 85, 105),
                BorderRadius = 8,
                Size = new Size(110, 40),
                Location = new Point(378, 110),
                Cursor = Cursors.Hand
            };
            _btnExit.Click += (s, e) => { DialogResult = WasModified ? DialogResult.OK : DialogResult.Cancel; Close(); };

            _btnSend = new RoundedButton
            {
                Text = "📤  Gửi Tin Nhắn",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                BackColor = ClinicalColors.PrimaryBlue,
                HoverBackColor = Color.FromArgb(49, 46, 129),
                ForeColor = Color.White,
                BorderRadius = 8,
                Size = new Size(150, 40),
                Location = new Point(592, 110),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _btnSend.Click += async (s, e) => await OnSendClickAsync();

            pnlFooter.Controls.Add(pnlInputContainer);
            pnlFooter.Controls.Add(_lblFooterStatus);
            pnlFooter.Controls.Add(_btnCloseSession);
            pnlFooter.Controls.Add(_btnBookAppointment);
            pnlFooter.Controls.Add(_btnExit);
            pnlFooter.Controls.Add(_btnSend);

            Controls.Add(pnlBody);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);

            this.AcceptButton = _btnSend;

            // Configure Native Margins & Placeholder
            this.HandleCreated += (s, e) =>
            {
                SendMessage(_rtbTranscript.Handle, EM_SETMARGINS, (IntPtr)(EC_LEFTMARGIN | EC_RIGHTMARGIN), (IntPtr)(16 | (16 << 16)));
                SendMessage(_txtReply.Handle, EM_SETCUEBANNER, 1, "Nhập tin nhắn tư vấn cho bệnh nhân (bấm Enter để gửi)...");
            };
        }

        private GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private string GetPatientInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "BN";
            var parts = name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return (parts[0][0].ToString() + parts[parts.Length - 1][0].ToString()).ToUpper();
            return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
        }

        private void UpdateHeaderStatus(string status)
        {
            if (_lblHeaderStatus == null || _pnlStatusBadge == null) return;

            if (status == "Closed")
            {
                _lblHeaderStatus.Text = "✅ Phiên đã đóng";
                _lblHeaderStatus.ForeColor = Color.FromArgb(71, 85, 105);
                _lblSubStatus.Text = "Hoàn tất hỗ trợ bệnh nhân";
                _lblSubStatus.ForeColor = Color.FromArgb(100, 116, 139);
                _pnlStatusBadge.BackColor = Color.FromArgb(241, 245, 249);
            }
            else if (_claimed)
            {
                _lblHeaderStatus.Text = "🟢 Đang tư vấn trực tiếp";
                _lblHeaderStatus.ForeColor = Color.FromArgb(4, 120, 87);
                _lblSubStatus.Text = "Tự động cập nhật mỗi 3 giây";
                _lblSubStatus.ForeColor = Color.FromArgb(5, 150, 105);
                _pnlStatusBadge.BackColor = Color.FromArgb(236, 253, 245);
            }
            else
            {
                _lblHeaderStatus.Text = "⏳ Đang tiếp nhận phiên...";
                _lblHeaderStatus.ForeColor = Color.FromArgb(180, 83, 9);
                _lblSubStatus.Text = "Vui lòng chờ giây lát";
                _lblSubStatus.ForeColor = Color.FromArgb(217, 119, 6);
                _pnlStatusBadge.BackColor = Color.FromArgb(254, 243, 199);
            }
            _pnlStatusBadge.Invalidate();
        }

        // ── Tiếp nhận phiên (Claim) rồi tải lịch sử + bật polling ───────────
        private async Task ClaimAndLoadAsync()
        {
            var (success, message) = await _api.ClaimChatSessionAsync(_item.SessionId);
            if (!success)
            {
                MessageBox.Show(
                    string.IsNullOrWhiteSpace(message) ? "Không thể tiếp nhận phiên chat này." : message,
                    "Không thể tiếp nhận", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            _claimed = true;
            WasModified = true;
            _txtReply.Enabled = true;
            _btnSend.Enabled = true;
            _btnCloseSession.Enabled = true;

            UpdateHeaderStatus(_sessionStatus);
            await RefreshMessagesAsync();

            _pollTimer = new Timer { Interval = 3000 };
            _pollTimer.Tick += async (s, e) => await RefreshMessagesAsync();
            _pollTimer.Start();
        }

        private async Task RefreshMessagesAsync()
        {
            if (!_claimed || IsDisposed) return;
            var (success, status, messages) = await _api.GetChatMessagesAsync(_item.SessionId);
            if (!success || IsDisposed) return;

            _sessionStatus = status;
            UpdateHeaderStatus(status);
            RenderTranscript(messages);

            if (status == "Closed")
            {
                _txtReply.Enabled = false;
                _btnSend.Enabled = false;
                _btnCloseSession.Enabled = false;
                _pollTimer?.Stop();
            }
        }

        private void RenderTranscript(List<ChatMessageModel> messages)
        {
            if (_rtbTranscript.IsDisposed) return;

            _rtbTranscript.SuspendLayout();
            _rtbTranscript.Clear();

            foreach (var m in messages)
            {
                string time = m.CreatedAt.ToLocalTime().ToString("HH:mm");
                bool isPatient = m.SenderType == "Patient";
                bool isAI = m.SenderType == "AI";

                string senderName = isPatient ? $"Bệnh nhân ({_item.PatientName})" : isAI ? "Trợ lý AI (Gemini)" : "Bạn (Lễ tân)";
                string bulletSymbol = isPatient ? "●" : isAI ? "✦" : "●";

                Color headerColor = isPatient ? Color.FromArgb(37, 99, 235)
                                  : isAI ? Color.FromArgb(124, 58, 237)
                                  : Color.FromArgb(5, 150, 105);

                // Clean content by trimming extra trailing newlines from API/inputs
                string cleanContent = (m.Content ?? "").Trim();
                if (string.IsNullOrEmpty(cleanContent)) continue;

                // Trước đây thụt lề riêng cho Staff (leftIndent=95) để giả lập bong bóng chat lệch
                // phải — nhưng RichTextBox.SelectionIndent áp cho cả khối đoạn văn (kể cả dòng nội
                // dung dài xuống dòng), nhìn thành khoảng trắng bất thường thay vì bong bóng chat thật.
                // Dùng chung 1 mức thụt lề cho mọi người gửi, chỉ phân biệt bằng màu + nhãn tên.
                _rtbTranscript.SelectionStart = _rtbTranscript.TextLength;
                _rtbTranscript.SelectionIndent = 18;
                _rtbTranscript.SelectionRightIndent = 18;
                _rtbTranscript.SelectionBackColor = Color.White;

                // 1. Sender Header Line
                _rtbTranscript.SelectionColor = headerColor;
                _rtbTranscript.SelectionFont = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold);
                _rtbTranscript.AppendText($"{bulletSymbol}  {senderName}");

                _rtbTranscript.SelectionColor = Color.FromArgb(100, 116, 139);
                _rtbTranscript.SelectionFont = ClinicalColors.GetMainFont(8.5f, FontStyle.Regular);
                _rtbTranscript.AppendText($"   •   {time}\n");

                // 2. Message Content Body
                _rtbTranscript.SelectionColor = Color.FromArgb(30, 41, 59);
                _rtbTranscript.SelectionFont = ClinicalColors.GetMainFont(9.75f, FontStyle.Regular);
                _rtbTranscript.AppendText($"{cleanContent}\n");

                // 3. Compact Spacing line between message blocks (6pt font for tight spacing)
                _rtbTranscript.SelectionStart = _rtbTranscript.TextLength;
                _rtbTranscript.SelectionIndent = 0;
                _rtbTranscript.SelectionRightIndent = 0;
                _rtbTranscript.SelectionFont = ClinicalColors.GetMainFont(6f, FontStyle.Regular);
                _rtbTranscript.AppendText("\n");
            }

            _rtbTranscript.ResumeLayout();
            _rtbTranscript.SelectionStart = _rtbTranscript.TextLength;
            _rtbTranscript.ScrollToCaret();
            HideCaret(_rtbTranscript.Handle);
        }

        private async Task OnSendClickAsync()
        {
            string content = _txtReply.Text.Trim();
            if (string.IsNullOrWhiteSpace(content) || !_claimed || _sessionStatus == "Closed") return;

            _btnSend.Enabled = false;
            _lblFooterStatus.Text = "";
            bool success = await _api.SendStaffChatMessageAsync(_item.SessionId, content);

            if (success)
            {
                _txtReply.Text = "";
                await RefreshMessagesAsync();
            }
            else
            {
                _lblFooterStatus.Text = "❌ Không gửi được tin nhắn. Kiểm tra kết nối API và thử lại.";
            }
            _btnSend.Enabled = _sessionStatus != "Closed";
        }

        private async Task OnCloseSessionClickAsync()
        {
            var confirm = MessageBox.Show(
                $"Xác nhận đóng phiên tư vấn với bệnh nhân {_item.PatientName}?\nBệnh nhân sẽ hoàn tất tư vấn và không thể gửi thêm tin nhắn.",
                "Xác nhận đóng phiên", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _btnCloseSession.Enabled = false;
            bool success = await _api.CloseChatSessionAsync(_item.SessionId);

            if (success)
            {
                WasModified = true;
                await RefreshMessagesAsync();
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _btnCloseSession.Enabled = true;
                MessageBox.Show("Không thể đóng phiên lúc này. Vui lòng thử lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
