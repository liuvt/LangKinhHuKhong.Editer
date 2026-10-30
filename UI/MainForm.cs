using System.Drawing;
using System.Drawing.Drawing2D;
using LangKinhHuKhong.Editer.Core;

namespace LangKinhHuKhong.Editer.UI;

public sealed class MainForm : Form
{
    private static readonly Color Background = Color.FromArgb(244, 238, 231);
    private static readonly Color CardColor = Color.FromArgb(250, 247, 242);
    private static readonly Color InputColor = Color.FromArgb(242, 236, 228);
    private static readonly Color Foreground = Color.FromArgb(82, 33, 28);
    private static readonly Color Muted = Color.FromArgb(120, 83, 72);
    private static readonly Color Accent = Color.FromArgb(170, 45, 36);

    private readonly ListBox _videos = NewListBox();
    private readonly ListBox _mp3s = NewListBox();
    private readonly ListBox _srts = NewListBox();
    private readonly Label _srtCount = NewLabel("0 SRT", 10, Muted);
    private readonly ComboBox _srtMode = NewCombo();
    private readonly CheckBox _burnSrt = new();
    private readonly CheckBox _exportSrt = new();
    private readonly ComboBox _subFont = NewCombo();
    private readonly NumericUpDown _subFontSize = NewNumber(28, 14, 80);
    private readonly NumericUpDown _subOffset = new() { Minimum = -10000, Maximum = 10000, Increment = 100, Width = 90 };
    private readonly TextBox _srtPreview = NewTextBox();
    private Panel? _srtCard;
    private Panel? _videoCard;
    private Panel? _overlayCard;
    private Panel? _settingsCard;
    private Panel? _previewCard;
    private Panel? _scrollArea;
    private readonly Label _videoCount = NewLabel("0 video", 10, Muted);
    private readonly Label _mp3Count = NewLabel("0 MP3", 10, Muted);
    private readonly TextBox _image1Path = NewTextBox();
    private readonly TextBox _image2Path = NewTextBox();
    private readonly TextBox _image3Path = NewTextBox();
    private readonly TextBox _image4Path = NewTextBox();
    private readonly PictureBox _preview1 = NewPreview();
    private readonly PictureBox _preview2 = NewPreview();
    private readonly PictureBox _preview3 = NewPreview();
    private readonly PictureBox _preview4 = NewPreview();
    private readonly PictureBox _layoutPreview = NewPreview();
    private readonly OverlaySlot[] _overlaySlots;
    private readonly TextBox _folderPath = NewTextBox();
    private readonly ComboBox _resolution = NewCombo();
    private readonly ComboBox _encoder = NewCombo();
    private readonly CheckBox _muteSourceAudio = new();
    private readonly ComboBox _selectedVideoSpeed = NewCombo();
    private readonly Button _applySpeed = NewButton("ÁP DỤNG", true);
    private readonly ProgressBar _progress = new();
    private readonly Label _status = NewLabel("Sẵn sàng để dựng video.", 10, Muted);
    private readonly Label _ffmpegStatus = NewLabel("FFmpeg: đang kiểm tra...", 10, Muted);
    private readonly Button _render = NewButton("▶  XUẤT VIDEO", true);
    private readonly Button _cancel = NewButton("HỦY", false);
    private readonly Button _openFolder = NewButton("MỞ THƯ MỤC", false);
    private readonly List<Control> _editableControls = [];
    private readonly ToolTip _tooltip = new();
    private CancellationTokenSource? _cts;
    private bool _rendering;
    private bool _closeAfterCancel;
    private string _previousStage = "";

    public MainForm()
    {
        Text = "LangKinhHuKhong.Editer v4.1  |  Video Editor Studio";
        MinimumSize = new Size(1050, 720);
        Size = new Size(1520, 980);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Background;
        ForeColor = Foreground;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        AutoScaleMode = AutoScaleMode.Dpi;
        LoadWindowIcon();
        _overlaySlots =
        [
            new OverlaySlot(Corner.TopLeft, "04-1  ẢNH TRÊN TRÁI", _image1Path, _preview1, NewNumber(20, 20, 100), NewNumber(20, 20, 100), NewNumber(1, 0, 100), NewNumber(1, 0, 100)),
            new OverlaySlot(Corner.TopRight, "04-2  ẢNH TRÊN PHẢI", _image2Path, _preview2, NewNumber(20, 20, 100), NewNumber(20, 20, 100), NewNumber(1, 0, 100), NewNumber(1, 0, 100)),
            new OverlaySlot(Corner.BottomLeft, "04-3  ẢNH DƯỚI TRÁI", _image3Path, _preview3, NewNumber(20, 20, 100), NewNumber(20, 20, 100), NewNumber(1, 0, 100), NewNumber(1, 0, 100)),
            new OverlaySlot(Corner.BottomRight, "04-4  ẢNH DƯỚI PHẢI", _image4Path, _preview4, NewNumber(20, 20, 100), NewNumber(20, 20, 100), NewNumber(1, 0, 100), NewNumber(1, 0, 100))
        ];

        BuildInterface();
        WireEvents();
        _folderPath.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            "LangKinhHuKhong.Output");
        CheckFfmpeg();
    }

    private void BuildInterface()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            BackColor = Background, Margin = Padding.Empty, Padding = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 134));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 138));
        Controls.Add(root);

        // Header v4.1: logo / tên sản phẩm / trạng thái nằm trên ba cột độc lập.
        // Không dùng MinimumSize hoặc các TableLayoutPanel lồng nhau để tránh logo
        // bị đẩy thấp, cắt mép khi Windows có DPI scaling khác nhau.
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = CardColor,
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = new Padding(24, 10, 26, 10)
        };
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 246));

        var logoHolder = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardColor,
            Margin = Padding.Empty,
            Padding = new Padding(1, 3, 12, 3)
        };
        var logoBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            BackColor = CardColor,
            SizeMode = PictureBoxSizeMode.Zoom,
            Margin = Padding.Empty,
            TabStop = false
        };
        TryLoadAppLogo(logoBox);
        logoHolder.Controls.Add(logoBox);
        header.Controls.Add(logoHolder, 0, 0);

        var brandText = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardColor,
            Margin = Padding.Empty
        };
        var brandTitle = new Label
        {
            Text = "LangKinhHuKhong.Editer v4.1",
            AutoSize = false,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Foreground,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 19F, FontStyle.Bold)
        };
        var brandCaption = new Label
        {
            Text = "LĂNG KÍNH HƯ KHÔNG  ·  VIDEO EDITOR STUDIO",
            AutoSize = false,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Muted,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular)
        };
        brandText.Controls.Add(brandTitle);
        brandText.Controls.Add(brandCaption);
        void LayoutBrandText()
        {
            var titleH = brandTitle.Font.Height + 9;
            var captionH = brandCaption.Font.Height + 7;
            var totalH = titleH + captionH + 3;
            var y = Math.Max(0, (brandText.ClientSize.Height - totalH) / 2);
            var w = Math.Max(0, brandText.ClientSize.Width - 6);
            brandTitle.SetBounds(0, y, w, titleH);
            brandCaption.SetBounds(0, y + titleH + 3, w, captionH);
        }
        brandText.Resize += (_, _) => LayoutBrandText();
        header.Controls.Add(brandText, 1, 0);

        var statusPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(241, 230, 217),
            Margin = new Padding(14, 20, 0, 20)
        };
        var statusTitle = new Label
        {
            Text = "●  XUẤT MỘT VIDEO DUY NHẤT",
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 28,
            TextAlign = ContentAlignment.BottomCenter,
            BackColor = Color.Transparent,
            ForeColor = Accent,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        var statusDescription = new Label
        {
            Text = "MP4  ·  720p / 1080p / 4K",
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent,
            ForeColor = Muted,
            Font = new Font("Segoe UI", 8.5F)
        };
        // Dock=Top sau cùng để không che dòng mô tả bên dưới.
        statusPanel.Controls.Add(statusDescription);
        statusPanel.Controls.Add(statusTitle);
        header.Controls.Add(statusPanel, 2, 0);
        root.Controls.Add(header, 0, 0);

        var central = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Background, Padding = new Padding(0, 6, 0, 0) };
        central.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        central.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var sidebar = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(239, 228, 216), Padding = new Padding(14, 18, 14, 14) };
        var menu = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        var navTitle = NewLabel("ĐIỀU HƯỚNG NHANH", 9, Muted, true);
        navTitle.AutoSize = false; navTitle.Width = 176; navTitle.Height = 44;
        menu.Controls.Add(navTitle);
        AddNav(menu, "01    Video nguồn", () => JumpTo(_videoCard));
        AddNav(menu, "02    Audio MP3", () => JumpTo(_videoCard));
        AddNav(menu, "03    Phụ đề SRT", () => JumpTo(_srtCard));
        AddNav(menu, "04    Ảnh PNG 4 góc", () => JumpTo(_overlayCard));
        AddNav(menu, "05    Xem trước", () => JumpTo(_previewCard));
        AddNav(menu, "06    Xuất video", () => JumpTo(_settingsCard));
        var hint = NewLabel("KÉO THẢ TỆP VÀO DANH SÁCH\n\nVideo được lặp từ đầu nếu ngắn hơn audio. Phụ đề theo thời gian MP3, không theo vòng lặp video.", 9, Muted);
        hint.Width = 184; hint.Height = 168; hint.Margin = new Padding(0, 32, 0, 0);
        menu.Controls.Add(hint);
        sidebar.Controls.Add(menu);
        central.Controls.Add(sidebar, 0, 0);

        _scrollArea = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Background };
        central.Controls.Add(_scrollArea, 1, 0);
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2, RowCount = 7, Padding = new Padding(18, 16, 18, 18), BackColor = Background
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 365));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 305));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 296));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 296));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 250));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        _scrollArea.Controls.Add(body);

        _videoCard = CreateMediaCard("01  VIDEO NGUỒN", _videos, _videoCount, true);
        body.Controls.Add(_videoCard, 0, 0);
        body.Controls.Add(CreateMediaCard("02  AUDIO MP3", _mp3s, _mp3Count, false), 1, 0);

        _srtCard = CreateSubtitleCard();
        body.Controls.Add(_srtCard, 0, 1);
        body.SetColumnSpan(_srtCard, 2);

        _overlayCard = CreateOverlayCard(_overlaySlots[0]);
        body.Controls.Add(_overlayCard, 0, 2);
        body.Controls.Add(CreateOverlayCard(_overlaySlots[1]), 1, 2);
        body.Controls.Add(CreateOverlayCard(_overlaySlots[2]), 0, 3);
        body.Controls.Add(CreateOverlayCard(_overlaySlots[3]), 1, 3);

        _previewCard = CreateLayoutPreviewCard();
        body.Controls.Add(_previewCard, 0, 4);
        body.SetColumnSpan(_previewCard, 2);

        _settingsCard = CreateSettingsCard();
        body.Controls.Add(_settingsCard, 0, 5);
        body.SetColumnSpan(_settingsCard, 2);

        var note = NewLabel("0–4 PNG cố định theo 4 góc • Text phụ đề luôn nằm trên cùng • Kết quả xuất: 1 MP4 duy nhất", 10, Muted);
        note.Margin = new Padding(12, 2, 0, 0);
        body.Controls.Add(note, 0, 6);
        body.SetColumnSpan(note, 2);
        root.Controls.Add(central, 0, 1);

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            Padding = new Padding(22, 10, 22, 12), BackColor = CardColor
        };
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _status.Dock = DockStyle.Fill;
        footer.Controls.Add(_status, 0, 0);
        _progress.Minimum = 0; _progress.Maximum = 1000;
        _progress.Style = ProgressBarStyle.Continuous; _progress.Dock = DockStyle.Fill;
        footer.Controls.Add(_progress, 0, 1);
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, Padding = new Padding(0, 10, 0, 0)
        };
        _render.Size = new Size(215, 47);
        _cancel.Size = new Size(108, 47);
        _openFolder.Size = new Size(155, 47);
        _cancel.Enabled = false;
        buttons.Controls.AddRange([_render, _cancel, _openFolder, _ffmpegStatus]);
        _ffmpegStatus.AutoSize = true;
        _ffmpegStatus.Padding = new Padding(0, 14, 10, 0);
        footer.Controls.Add(buttons, 0, 2);
        root.Controls.Add(footer, 0, 2);
    }

    private static void AddNav(FlowLayoutPanel menu, string title, Action click)
    {
        var item = NewButton(title, false);
        item.Width = 174; item.Height = 45;
        item.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        item.Margin = new Padding(0, 0, 0, 8);
        item.TextAlign = ContentAlignment.MiddleLeft;
        item.Click += (_, _) => click();
        menu.Controls.Add(item);
    }

    private void JumpTo(Control? card)
    {
        if (card is null || _scrollArea is null) return;
        _scrollArea.ScrollControlIntoView(card);
    }

    private Panel CreateSubtitleCard()
    {
        var card = NewCard();
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        table.Controls.Add(NewLabel("03  PHỤ ĐỀ SRT", 12, Foreground, true), 0, 0);
        var inner = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 49));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 51));
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        _srts.Dock = DockStyle.Fill;
        left.Controls.Add(_srts, 0, 0);
        var fileBtns = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 7, 0, 0) };
        var add = NewButton("+ THÊM SRT", true);
        var remove = NewButton("XÓA CHỌN", false);
        var up = NewButton("↑", false);
        var down = NewButton("↓", false);
        add.Width = 132; remove.Width = 108; up.Width = 38; down.Width = 38;
        add.Click += (_, _) => ChooseSrtFiles();
        remove.Click += (_, _) => { foreach (var obj in _srts.SelectedItems.Cast<object>().ToArray()) _srts.Items.Remove(obj); UpdateSrtPreview(); };
        up.Click += (_, _) => { MoveItem(_srts, -1); UpdateSrtPreview(); };
        down.Click += (_, _) => { MoveItem(_srts, 1); UpdateSrtPreview(); };
        fileBtns.Controls.AddRange([add, remove, up, down]);
        left.Controls.Add(fileBtns, 0, 1);
        inner.Controls.Add(left, 0, 0);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12, 0, 0, 0) };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 49));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var options = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        options.Controls.Add(NewInlineLabel("Căn theo:"));
        _srtMode.Items.AddRange(["1 SRT / toàn bộ audio", "1 SRT / mỗi MP3"]);
        _srtMode.Width = 215; _srtMode.SelectedIndex = 0;
        options.Controls.Add(_srtMode);
        options.Controls.Add(NewInlineLabel("Lệch ms:"));
        _subOffset.BackColor = InputColor; _subOffset.ForeColor = Foreground;
        options.Controls.Add(_subOffset);
        right.Controls.Add(options, 0, 0);
        var style = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        _burnSrt.Appearance = Appearance.Normal;
        _burnSrt.Checked = true; _burnSrt.AutoSize = true;
        _burnSrt.Text = "Chèn chữ vào video"; _burnSrt.ForeColor = Foreground;
        _exportSrt.Checked = true; _exportSrt.AutoSize = true;
        _exportSrt.Text = "Xuất SRT kèm"; _exportSrt.ForeColor = Foreground;
        _burnSrt.Margin = new Padding(0, 10, 14, 0); _exportSrt.Margin = new Padding(0, 10, 0, 0);
        style.Controls.AddRange([_burnSrt, _exportSrt]);
        right.Controls.Add(style, 0, 1);
        _srtPreview.Dock = DockStyle.Fill;
        _srtPreview.Multiline = true;
        _srtPreview.ReadOnly = true;
        _srtPreview.ScrollBars = ScrollBars.Vertical;
        _srtPreview.Text = "Chưa có SRT. Bạn có thể xuất video không phụ đề.";
        right.Controls.Add(_srtPreview, 0, 2);
        inner.Controls.Add(right, 1, 0);
        table.Controls.Add(inner, 0, 1);
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 10, 0, 0) };
        bottom.Controls.Add(NewInlineLabel("Font chữ:"));
        _subFont.Items.AddRange(["Arial", "Segoe UI", "Tahoma", "Verdana", "Times New Roman"]);
        _subFont.SelectedIndex = 0; _subFont.Width = 165;
        bottom.Controls.Add(_subFont);
        bottom.Controls.Add(NewInlineLabel("Cỡ chữ:"));
        bottom.Controls.Add(_subFontSize);
        _srtCount.AutoSize = true; _srtCount.Margin = new Padding(22, 9, 0, 0);
        bottom.Controls.Add(_srtCount);
        table.Controls.Add(bottom, 0, 2);
        card.Controls.Add(table);
        _editableControls.AddRange([_srts, add, remove, up, down, _srtMode, _subOffset,
            _burnSrt, _exportSrt, _subFont, _subFontSize]);
        return card;
    }

    private void ChooseSrtFiles()
    {
        using var picker = new OpenFileDialog
        {
            Title = "Chọn phụ đề SRT đã xuất từ TTSS", Multiselect = true,
            Filter = "Phụ đề SRT|*.srt|Tất cả tệp|*.*"
        };
        if (picker.ShowDialog(this) == DialogResult.OK) AddSrtFiles(picker.FileNames);
    }

    private void AddSrtFiles(IEnumerable<string> files)
    {
        var exists = new HashSet<string>(_srts.Items.Cast<SubtitleFile>().Select(x => x.FullPath), StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            if (!File.Exists(file) || !Path.GetExtension(file).Equals(".srt", StringComparison.OrdinalIgnoreCase)) continue;
            var full = Path.GetFullPath(file);
            if (!exists.Add(full)) continue;
            try
            {
                var cues = SubtitleComposer.Parse(full);
                _srts.Items.Add(new SubtitleFile(full, cues.Count));
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException)
            {
                MessageBox.Show(this, ex.Message, "SRT không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        if (_srts.Items.Count > 1 && _srts.Items.Count == _mp3s.Items.Count)
            _srtMode.SelectedIndex = 1;
        UpdateSrtPreview();
    }

    private void UpdateSrtPreview()
    {
        _srtCount.Text = $"{_srts.Items.Count} file SRT";
        if (_srts.Items.Count == 0)
        {
            _srtPreview.Text = "Chưa có SRT. Có thể xuất video không phụ đề.";
            RefreshLayoutPreview();
            return;
        }
        try
        {
            var file = _srts.SelectedItem as SubtitleFile ?? (SubtitleFile)_srts.Items[0];
            var cues = SubtitleComposer.Parse(file.FullPath);
            _srtPreview.Text = string.Join(Environment.NewLine + Environment.NewLine,
                cues.Take(3).Select(x => $"{x.Start:hh\\:mm\\:ss\\.fff} → {x.End:hh\\:mm\\:ss\\.fff}  {x.Text.Replace("\n", " / ")}"));
        }
        catch (Exception ex) { _srtPreview.Text = ex.Message; }
        RefreshLayoutPreview();
    }

    private Panel CreateMediaCard(string headline, ListBox list, Label count, bool isVideo)
    {
        var panel = NewCard();
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 37));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));

        var heading = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85));
        heading.Controls.Add(NewLabel(headline, 11, Foreground, true), 0, 0);
        count.TextAlign = ContentAlignment.MiddleRight;
        count.Dock = DockStyle.Fill;
        heading.Controls.Add(count, 1, 0);
        table.Controls.Add(heading, 0, 0);
        list.Dock = DockStyle.Fill;
        table.Controls.Add(list, 0, 1);

        var buttonGroup = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };
        var add = NewButton(isVideo ? "+  THÊM VIDEO" : "+  THÊM MP3", true);
        var remove = NewButton("XÓA CHỌN", false);
        var up = NewButton("↑", false);
        var down = NewButton("↓", false);
        add.Width = isVideo ? 145 : 134;
        remove.Width = 105;
        up.Width = 42;
        down.Width = 42;
        add.Click += (_, _) => ChooseFiles(isVideo);
        remove.Click += (_, _) => RemoveSelected(isVideo);
        up.Click += (_, _) => MoveItem(list, -1);
        down.Click += (_, _) => MoveItem(list, 1);
        buttonGroup.Controls.AddRange([add, remove, up, down]);
        table.Controls.Add(buttonGroup, 0, 2);
        if (isVideo)
            table.Controls.Add(CreateVideoSpeedRow(), 0, 3);
        else
        {
            var note = NewLabel("Toàn bộ MP3 sẽ được nối thành một audio duy nhất trước khi xuất video.", 10, Muted);
            note.Padding = new Padding(0, 10, 0, 0);
            table.Controls.Add(note, 0, 3);
        }
        panel.Controls.Add(table);
        _editableControls.AddRange([add, remove, up, down, list]);
        return panel;
    }

    private Control CreateVideoSpeedRow()
    {
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, WrapContents = true,
            Padding = new Padding(0, 7, 0, 0), AutoScroll = false
        };
        var caption = NewInlineLabel("Tốc độ phát:");
        caption.Margin = new Padding(0, 8, 5, 0);
        row.Controls.Add(caption);
        _selectedVideoSpeed.Items.AddRange(PlaybackSpeeds.Allowed
            .Select(x => (object)new SpeedOption(x)).ToArray());
        _selectedVideoSpeed.SelectedIndex = 3; // 1× by default
        _selectedVideoSpeed.Width = 76;
        _applySpeed.Width = 92;
        _applySpeed.Height = 33;
        row.Controls.Add(_selectedVideoSpeed);
        row.Controls.Add(_applySpeed);

        _muteSourceAudio.Appearance = Appearance.Button;
        _muteSourceAudio.Checked = true; // Preserve the old behavior by default.
        _muteSourceAudio.AutoSize = false;
        _muteSourceAudio.Width = 172;
        _muteSourceAudio.Height = 33;
        _muteSourceAudio.TextAlign = ContentAlignment.MiddleCenter;
        _muteSourceAudio.FlatStyle = FlatStyle.Flat;
        _muteSourceAudio.Cursor = Cursors.Hand;
        UpdateAudioButton();
        row.Controls.Add(_muteSourceAudio);
        _editableControls.AddRange([_selectedVideoSpeed, _applySpeed, _muteSourceAudio]);
        return row;
    }

    private void UpdateAudioButton()
    {
        _muteSourceAudio.Text = _muteSourceAudio.Checked
            ? "🔇 TẮT ÂM GỐC: BẬT"
            : "🔊 GIỮ ÂM GỐC: BẬT";
        _muteSourceAudio.BackColor = _muteSourceAudio.Checked ? InputColor : Color.FromArgb(245, 226, 204);
        _muteSourceAudio.ForeColor = Foreground;
    }

    private void ApplySpeedToSelection()
    {
        if (_videos.SelectedIndices.Count == 0)
        {
            MessageBox.Show(this, "Hãy chọn một hoặc nhiều video trong danh sách trước khi áp dụng tốc độ.",
                "Chưa chọn video", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (_selectedVideoSpeed.SelectedItem is not SpeedOption speed) return;
        var indices = _videos.SelectedIndices.Cast<int>().ToArray();
        _videos.BeginUpdate();
        try
        {
            foreach (var index in indices)
            {
                var source = (MediaFile)_videos.Items[index];
                _videos.Items[index] = source with { PlaybackSpeed = speed.Value };
            }
            _videos.ClearSelected();
            foreach (var index in indices) _videos.SetSelected(index, true);
        }
        finally { _videos.EndUpdate(); }
    }

    // Each fixed corner is its own drop zone. Drop a file on the image, path, heading,
    // button area or on the card background; it only replaces that corner's image.
    /// <summary>
    /// v3 UI: four independent cards. All controls use a grid layout instead of
    /// a single overcrowded FlowLayoutPanel, so position inputs are not clipped.
    /// </summary>
    private Panel CreateOverlayCard(OverlaySlot slot)
    {
        var panel = NewCard();
        panel.Padding = new Padding(14, 12, 14, 12);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        slot.Thumbnail.Dock = DockStyle.Fill;
        slot.Thumbnail.Cursor = Cursors.Hand;
        slot.Thumbnail.Margin = new Padding(0, 0, 10, 0);
        _tooltip.SetToolTip(slot.Thumbnail, "Kéo PNG/JPG/WebP vào ô này: " + slot.Title);
        layout.Controls.Add(slot.Thumbnail, 0, 0);

        var controls = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6,
            Padding = Padding.Empty, Margin = Padding.Empty
        };
        controls.RowStyles.Add(new RowStyle(SizeType.Absolute, 31));
        controls.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
        controls.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        controls.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        controls.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        controls.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        controls.Controls.Add(NewLabel(slot.Title.Replace("04-", "04 "), 11, Foreground, true), 0, 0);
        slot.PathBox.Dock = DockStyle.Fill;
        slot.PathBox.Margin = new Padding(0, 2, 0, 7);
        slot.PathBox.ReadOnly = true;
        slot.PathBox.PlaceholderText = "Chọn ảnh hoặc kéo thả tại đây";
        controls.Controls.Add(slot.PathBox, 0, 1);

        var buttonRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
        var browse = NewButton("CHỌN ẢNH", true);
        var clear = NewButton("BỎ ẢNH", false);
        browse.Dock = DockStyle.Fill; browse.Margin = new Padding(0, 0, 7, 7);
        clear.Dock = DockStyle.Fill; clear.Margin = new Padding(0, 0, 0, 7);
        browse.Click += (_, _) => ChooseOverlayImage(slot);
        clear.Click += (_, _) => ClearOverlayImage(slot);
        buttonRow.Controls.Add(browse, 0, 0);
        buttonRow.Controls.Add(clear, 1, 0);
        controls.Controls.Add(buttonRow, 0, 2);

        var sizeRow = CreateOverlayNumberRow("Rộng %", slot.WidthBox, "Cao %", slot.HeightBox);
        controls.Controls.Add(sizeRow, 0, 3);
        var positionRow = CreateOverlayNumberRow("Ngang %", slot.OffsetXBox, "Dọc %", slot.OffsetYBox);
        controls.Controls.Add(positionRow, 0, 4);

        var hint = NewLabel("Vị trí được tính từ góc neo và luôn giữ ảnh trong khung hình.", 8, Muted);
        hint.AutoEllipsis = true;
        hint.Dock = DockStyle.Fill;
        controls.Controls.Add(hint, 0, 5);
        layout.Controls.Add(controls, 1, 0);
        panel.Controls.Add(layout);

        foreach (var number in new[] { slot.WidthBox, slot.HeightBox, slot.OffsetXBox, slot.OffsetYBox })
        {
            number.ValueChanged += (_, _) => RefreshLayoutPreview();
            number.Margin = new Padding(2, 4, 5, 4);
        }
        _tooltip.SetToolTip(slot.WidthBox, "Rộng tối đa từ 20% tới 100% khung video, giữ tỷ lệ ảnh gốc.");
        _tooltip.SetToolTip(slot.HeightBox, "Cao tối đa từ 20% tới 100% khung video, giữ tỷ lệ ảnh gốc.");
        _tooltip.SetToolTip(slot.OffsetXBox, "Di chuyển ngang (0–100%) từ mép của góc chọn; không thể tràn khỏi video.");
        _tooltip.SetToolTip(slot.OffsetYBox, "Di chuyển dọc (0–100%) từ mép của góc chọn; không thể tràn khỏi video.");
        EnableOverlayDrop(panel, slot);
        _editableControls.AddRange([browse, clear, slot.WidthBox, slot.HeightBox, slot.OffsetXBox, slot.OffsetYBox]);
        return panel;
    }

    private static TableLayoutPanel CreateOverlayNumberRow(string label1, NumericUpDown value1,
        string label2, NumericUpDown value2)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = Padding.Empty };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23));
        var name1 = NewLabel(label1, 9, Muted);
        var name2 = NewLabel(label2, 9, Muted);
        name1.AutoEllipsis = true;
        name2.AutoEllipsis = true;
        name1.Margin = Padding.Empty; name2.Margin = Padding.Empty;
        value1.Dock = DockStyle.Fill;
        value2.Dock = DockStyle.Fill;
        value1.Width = 60; value2.Width = 60;
        row.Controls.Add(name1, 0, 0);
        row.Controls.Add(value1, 1, 0);
        row.Controls.Add(name2, 2, 0);
        row.Controls.Add(value2, 3, 0);
        return row;
    }

    private static bool TryGetImageFromDrop(IDataObject? data, out string file)
    {
        file = "";
        if (data?.GetData(DataFormats.FileDrop) is not string[] paths) return false;
        var valid = paths.FirstOrDefault(p => File.Exists(p) && IsSupportedOverlayImage(p));
        if (valid is null) return false;
        file = valid;
        return true;
    }

    private static bool IsSupportedOverlayImage(string path) =>
        new[] { ".png", ".jpg", ".jpeg", ".bmp", ".webp" }
            .Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    private void EnableOverlayDrop(Control control, OverlaySlot slot)
    {
        control.AllowDrop = true;
        control.DragEnter += (sender, e) => e.Effect = !_rendering && TryGetImageFromDrop(e.Data, out _)
            ? DragDropEffects.Copy : DragDropEffects.None;
        control.DragDrop += (_, e) =>
        {
            if (_rendering || !TryGetImageFromDrop(e.Data, out var file)) return;
            SetOverlayImage(slot, file);
        };
        foreach (Control child in control.Controls)
            EnableOverlayDrop(child, slot);
    }

    private void SetOverlayImage(OverlaySlot slot, string file)
    {
        if (!File.Exists(file) || !IsSupportedOverlayImage(file)) return;
        slot.PathBox.Text = Path.GetFullPath(file);
        LoadPreview(file, slot.Thumbnail);
        RefreshLayoutPreview();
        _status.Text = $"Đã đặt {Path.GetFileName(file)} vào {slot.Title}.";
    }

    private void ChooseOverlayImage(OverlaySlot slot)
    {
        using var picker = new OpenFileDialog
        {
            Title = $"Chọn ảnh cho {slot.Title}",
            Filter = "Ảnh|*.png;*.jpg;*.jpeg;*.bmp;*.webp|Tất cả tệp|*.*"
        };
        if (picker.ShowDialog(this) == DialogResult.OK)
        {
            if (IsSupportedOverlayImage(picker.FileName)) SetOverlayImage(slot, picker.FileName);
            else MessageBox.Show(this, "Chỉ hỗ trợ PNG, JPG, JPEG, BMP, WebP.", "Định dạng ảnh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ClearOverlayImage(OverlaySlot slot)
    {
        ClearImage(slot.PathBox, slot.Thumbnail);
        RefreshLayoutPreview();
    }

    private Panel CreateLayoutPreviewCard()
    {
        var panel = NewCard();
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        _layoutPreview.Dock = DockStyle.Fill;
        _layoutPreview.AllowDrop = true;
        _layoutPreview.Margin = new Padding(0, 0, 10, 0);
        _tooltip.SetToolTip(_layoutPreview, "Thả ảnh vào từng phần tư khung preview để gán trực tiếp cho góc tương ứng.");
        table.Controls.Add(_layoutPreview, 0, 0);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, ColumnCount = 1 };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        right.Controls.Add(NewLabel("05  XEM TRƯỚC BỐ CỤC (16:9)", 11, Foreground, true), 0, 0);
        right.Controls.Add(NewLabel("- Kéo thả ảnh vào 4 ô hoặc 4 phần tư của preview.\n- Text/subtitle sẽ nằm lớp trên cùng.", 9, Muted), 0, 1);
        right.Controls.Add(NewLabel("- Mỗi ảnh chỉnh riêng kích thước và vị trí X/Y.\n- Ảnh luôn được giới hạn trong video, không bị tràn ra ngoài.", 9, Muted), 0, 2);
        right.Controls.Add(NewLabel("- Khung mô phỏng đúng tỷ lệ 16:9 và tọa độ ảnh.\n- Kiểu chữ xem trước có thể khác FFmpeg/libass đôi chút.", 9, Muted), 0, 3);
        var refreshRow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };
        var refresh = NewButton("LÀM MỚI PREVIEW", true);
        refresh.Width = 168;
        refresh.Click += (_, _) => RefreshLayoutPreview();
        refreshRow.Controls.Add(refresh);
        right.Controls.Add(refreshRow, 0, 5);
        _editableControls.Add(refresh);

        table.Controls.Add(right, 1, 0);
        panel.Controls.Add(table);
        return panel;
    }

    private Rectangle GetPreviewImageRect()
    {
        var picture = _layoutPreview;
        if (picture.Image is null || picture.ClientSize.Width <= 0 || picture.ClientSize.Height <= 0)
            return Rectangle.Empty;
        var scale = Math.Min((double)picture.ClientSize.Width / picture.Image.Width,
            (double)picture.ClientSize.Height / picture.Image.Height);
        var width = Math.Max(1, (int)Math.Round(picture.Image.Width * scale));
        var height = Math.Max(1, (int)Math.Round(picture.Image.Height * scale));
        return new Rectangle((picture.ClientSize.Width - width) / 2,
            (picture.ClientSize.Height - height) / 2, width, height);
    }

    private void RefreshLayoutPreview()
    {
        try
        {
            var (w, h) = _resolution.SelectedIndex switch
            {
                0 => (1280, 720),
                2 => (3840, 2160),
                _ => (1920, 1080)
            };
            using var bmp = new Bitmap(1280, 720);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(247, 241, 234));
            using var bgBrush = new LinearGradientBrush(new Rectangle(0, 0, bmp.Width, bmp.Height),
                Color.FromArgb(248, 241, 232), Color.FromArgb(238, 228, 217), 20f);
            g.FillRectangle(bgBrush, 0, 0, bmp.Width, bmp.Height);
            var stage = new Rectangle(0, 0, bmp.Width, bmp.Height);
            using (var stageBrush = new SolidBrush(Color.FromArgb(250, 247, 242)))
                g.FillRectangle(stageBrush, stage);
            using (var pen = new Pen(Color.FromArgb(214, 184, 156), 2))
                g.DrawRectangle(pen, stage);

            using (var labelBrush = new SolidBrush(Color.FromArgb(190, 107, 71, 59)))
            using (var labelFont = new Font("Segoe UI", 14, FontStyle.Bold))
            {
                g.DrawString($"BỐ CỤC XUẤT {w}x{h} | Kích thước và vị trí độc lập cho từng góc", labelFont, labelBrush, 16, 10);
            }

            foreach (var slot in _overlaySlots)
            {
                if (string.IsNullOrWhiteSpace(slot.PathBox.Text))
                    DrawEmptyCornerGuide(g, stage, slot.Corner);
                else
                    DrawOverlay(g, stage, slot);
            }
            DrawSubtitlePreview(g, stage);

            var old = _layoutPreview.Image;
            _layoutPreview.Image = new Bitmap(bmp);
            old?.Dispose();
        }
        catch
        {
            // Preview is best-effort only.
        }
    }

    private static void DrawEmptyCornerGuide(Graphics g, Rectangle stage, Corner corner)
    {
        const int guideWidth = 156, guideHeight = 74, inset = 12;
        var x = corner is Corner.TopLeft or Corner.BottomLeft
            ? stage.Left + inset : stage.Right - guideWidth - inset;
        var y = corner is Corner.TopLeft or Corner.TopRight
            ? stage.Top + 28 : stage.Bottom - guideHeight - inset;
        var bounds = new Rectangle(x, y, guideWidth, guideHeight);
        using var border = new Pen(Color.FromArgb(140, 185, 93, 62), 2) { DashStyle = DashStyle.Dash };
        using var brush = new SolidBrush(Color.FromArgb(150, 245, 232, 218));
        using var font = new Font("Segoe UI", 11, FontStyle.Regular);
        g.DrawRectangle(border, bounds);
        var title = corner switch
        {
            Corner.TopLeft => "TRÊN TRÁI",
            Corner.TopRight => "TRÊN PHẢI",
            Corner.BottomLeft => "DƯỚI TRÁI",
            _ => "DƯỚI PHẢI"
        };
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString("+ THẢ ẢNH\n" + title, font, brush, bounds, format);
    }

    private void DrawOverlay(Graphics g, Rectangle stage, OverlaySlot slot)
    {
        var path = slot.PathBox.Text;
        var corner = slot.Corner;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var image = Image.FromStream(fs);
            var maxW = Math.Max(1, stage.Width * (int)slot.WidthBox.Value / 100);
            var maxH = Math.Max(1, stage.Height * (int)slot.HeightBox.Value / 100);
            var scale = Math.Min((double)maxW / image.Width, (double)maxH / image.Height);
            var width = Math.Clamp((int)Math.Round(image.Width * scale), 1, stage.Width);
            var height = Math.Clamp((int)Math.Round(image.Height * scale), 1, stage.Height);
            var offsetX = Math.Max(0, stage.Width * (int)slot.OffsetXBox.Value / 100);
            var offsetY = Math.Max(0, stage.Height * (int)slot.OffsetYBox.Value / 100);
            var x = corner is Corner.TopLeft or Corner.BottomLeft
                ? stage.Left + Math.Clamp(offsetX, 0, stage.Width - width)
                : stage.Right - width - Math.Clamp(offsetX, 0, stage.Width - width);
            var y = corner is Corner.TopLeft or Corner.TopRight
                ? stage.Top + Math.Clamp(offsetY, 0, stage.Height - height)
                : stage.Bottom - height - Math.Clamp(offsetY, 0, stage.Height - height);
            g.DrawImage(image, new Rectangle(x, y, width, height));
            using var pen = new Pen(Color.FromArgb(120, 174, 108, 82), 1);
            g.DrawRectangle(pen, x, y, width, height);
        }
        catch { }
    }

    private void DrawSubtitlePreview(Graphics g, Rectangle stage)
    {
        if (!_burnSrt.Checked || _srts.Items.Count == 0) return;
        var text = GetPreviewSubtitleText();
        var fontSize = Math.Max(16, Math.Min(34, (int)_subFontSize.Value));
        using var font = new Font(_subFont.SelectedItem?.ToString() ?? "Arial", fontSize, FontStyle.Bold);
        var rect = new RectangleF(stage.Left + 40, stage.Bottom - 115, stage.Width - 80, 70);
        using var bg = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
        g.FillRectangle(bg, Rectangle.Round(rect));
        using var brush = new SolidBrush(Color.White);
        using var outline = new Pen(Color.Black, 4) { LineJoin = System.Drawing.Drawing2D.LineJoin.Round };
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        path.AddString(text, font.FontFamily, (int)font.Style, g.DpiY * font.Size / 72, rect, format);
        g.DrawPath(outline, path);
        g.FillPath(brush, path);
    }

    private string GetPreviewSubtitleText()
    {
        try
        {
            if (_srts.Items.Count > 0)
            {
                var file = _srts.SelectedItem as SubtitleFile ?? (SubtitleFile)_srts.Items[0];
                var cues = SubtitleComposer.Parse(file.FullPath);
                var cue = cues.FirstOrDefault();
                if (cue is not null && !string.IsNullOrWhiteSpace(cue.Text))
                    return cue.Text.Replace("\n", " ").Trim();
            }
        }
        catch { }
        return "Chuyện này người chưa biết sẽ hiển thị trên cùng";
    }

    private void LoadWindowIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "software-icon.ico");
        if (!File.Exists(iconPath)) return;
        try
        {
            using var fileIcon = new Icon(iconPath);
            Icon = (Icon)fileIcon.Clone();
        }
        catch (Exception ex) when (ex is IOException or ArgumentException)
        {
            // Icon not essential for rendering; allow the editor to start.
        }
    }

    private void TryLoadAppLogo(PictureBox box)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "software-logo-header.png"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "software-logo.png"),
            Path.Combine(AppContext.BaseDirectory, "software-logo.png")
        };
        foreach (var file in candidates)
        {
            if (!File.Exists(file)) continue;
            try
            {
                using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var image = Image.FromStream(fs);
                box.Image = new Bitmap(image);
                return;
            }
            catch { }
        }
    }

    private Panel CreateSettingsCard()
    {
        var panel = NewCard();
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 29));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        table.Controls.Add(NewLabel("06  THIẾT LẬP XUẤT VIDEO", 11, Foreground, true), 0, 0);

        var folderRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 121));
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 143));
        var folderLabel = NewLabel("Lưu kết quả:", 10, Muted);
        folderLabel.Dock = DockStyle.Fill;
        folderLabel.TextAlign = ContentAlignment.MiddleLeft;
        folderRow.Controls.Add(folderLabel, 0, 0);
        _folderPath.Dock = DockStyle.Fill;
        folderRow.Controls.Add(_folderPath, 1, 0);
        var browse = NewButton("CHỌN THƯ MỤC", false);
        browse.Dock = DockStyle.Fill;
        browse.Margin = new Padding(9, 0, 0, 3);
        browse.Click += (_, _) => ChooseFolder();
        folderRow.Controls.Add(browse, 2, 0);
        table.Controls.Add(folderRow, 0, 1);

        var optionsRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, WrapContents = true,
            Padding = new Padding(0, 8, 0, 0), AutoScroll = true
        };
        _resolution.Items.AddRange(["720p (1280 × 720)", "1080p (1920 × 1080)", "4K (3840 × 2160)"]);
        _resolution.SelectedIndex = 1;
        _resolution.Width = 174;
        _encoder.Items.AddRange(["H.264 CPU (ổn định)", "NVIDIA NVENC (nhanh)"]);
        _encoder.SelectedIndex = 0;
        _encoder.Width = 180;
        optionsRow.Controls.Add(NewInlineLabel("Chất lượng:"));
        optionsRow.Controls.Add(_resolution);
        optionsRow.Controls.Add(NewInlineLabel("Mã hóa:"));
        optionsRow.Controls.Add(_encoder);
        table.Controls.Add(optionsRow, 0, 2);

        var autoLoopNote = NewLabel("Hệ thống sẽ nối toàn bộ video, tự lặp từ video đầu nếu thiếu; độ dài cuối cùng luôn bám theo tổng audio MP3.", 10, Muted);
        autoLoopNote.AutoSize = true;
        autoLoopNote.Padding = new Padding(0, 5, 0, 0);
        table.Controls.Add(autoLoopNote, 0, 3);
        panel.Controls.Add(table);
        _editableControls.AddRange([_folderPath, browse, _resolution, _encoder]);
        return panel;
    }

    private void WireEvents()
    {
        _render.Click += async (_, _) => await StartRenderAsync();
        _applySpeed.Click += (_, _) => ApplySpeedToSelection();
        _muteSourceAudio.CheckedChanged += (_, _) => UpdateAudioButton();
        _videos.SelectedIndexChanged += (_, _) => SyncSpeedSelection();
        _cancel.Click += (_, _) => _cts?.Cancel();
        _openFolder.Click += (_, _) => OpenFolder();
        ConfigureDragDrop(_videos, true);
        ConfigureDragDrop(_mp3s, false);
        _srts.SelectedIndexChanged += (_, _) => { UpdateSrtPreview(); RefreshLayoutPreview(); };
        _srts.AllowDrop = true;
        _srts.DragEnter += (_, e) => e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? DragDropEffects.Copy : DragDropEffects.None;
        _srts.DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files) AddSrtFiles(files);
        };
        _tooltip.SetToolTip(_srts, "Kéo thả SRT. Chế độ Theo từng MP3: mỗi SRT tương ứng một MP3 cùng thứ tự.");
        _tooltip.SetToolTip(_subOffset, "Dịch tất cả phụ đề sớm/muộn theo mili giây, ví dụ -300 ms.");
        _tooltip.SetToolTip(_videos, "Kéo thả video. Chọn 1 hoặc nhiều dòng, chọn tốc độ rồi nhấn ÁP DỤNG; dùng ↑ ↓ để đổi thứ tự.");
        _tooltip.SetToolTip(_selectedVideoSpeed, "0.4× = 2.5 lần thời lượng; 0.5× = 2 lần; 2× = còn một nửa.");
        _tooltip.SetToolTip(_muteSourceAudio, "Mặc định tắt tiếng gốc. Bấm để giữ âm gốc ở 20% và trộn với MP3; video không có âm vẫn xuất được.");
        _tooltip.SetToolTip(_mp3s, "Có thể kéo thả nhiều MP3. Nối chúng theo thứ tự thành một audio, xuất đúng một MP4.");
        _layoutPreview.DragEnter += (sender, e) =>
            e.Effect = !_rendering && TryGetImageFromDrop(e.Data, out _)
                ? DragDropEffects.Copy : DragDropEffects.None;
        _layoutPreview.DragDrop += (_, e) =>
        {
            if (_rendering || !TryGetImageFromDrop(e.Data, out var file)) return;
            var clientPoint = _layoutPreview.PointToClient(new Point(e.X, e.Y));
            var imageRect = GetPreviewImageRect();
            if (imageRect.Width <= 0 || !imageRect.Contains(clientPoint)) return;
            var left = clientPoint.X < imageRect.Left + imageRect.Width / 2;
            var top = clientPoint.Y < imageRect.Top + imageRect.Height / 2;
            var corner = top
                ? (left ? Corner.TopLeft : Corner.TopRight)
                : (left ? Corner.BottomLeft : Corner.BottomRight);
            var slot = _overlaySlots.First(x => x.Corner == corner);
            SetOverlayImage(slot, file);
        };
        _resolution.SelectedIndexChanged += (_, _) => RefreshLayoutPreview();
        _burnSrt.CheckedChanged += (_, _) => RefreshLayoutPreview();
        _subFont.SelectedIndexChanged += (_, _) => RefreshLayoutPreview();
        _subFontSize.ValueChanged += (_, _) => RefreshLayoutPreview();
        _srtMode.SelectedIndexChanged += (_, _) => RefreshLayoutPreview();
        FormClosing += OnFormClosing;
        RefreshLayoutPreview();
    }

    private void SyncSpeedSelection()
    {
        if (_videos.SelectedItems.Count != 1) return;
        var selected = (MediaFile)_videos.SelectedItem!;
        for (var i = 0; i < _selectedVideoSpeed.Items.Count; i++)
        {
            if (_selectedVideoSpeed.Items[i] is SpeedOption opt &&
                Math.Abs(opt.Value - selected.PlaybackSpeed) < 0.000001)
            {
                _selectedVideoSpeed.SelectedIndex = i;
                return;
            }
        }
    }

    private void CheckFfmpeg()
    {
        try
        {
            var found = FfmpegLocator.Find();
            _ffmpegStatus.Text = "✓ FFmpeg đã sẵn sàng";
            _ffmpegStatus.ForeColor = Color.FromArgb(98, 146, 106);
            _tooltip.SetToolTip(_ffmpegStatus, found.Ffmpeg);
        }
        catch (FileNotFoundException)
        {
            _ffmpegStatus.Text = "⚠ Chưa có FFmpeg (xem README)";
            _ffmpegStatus.ForeColor = Color.FromArgb(171, 118, 52);
        }
    }

    private void ChooseFiles(bool isVideo)
    {
        using var picker = new OpenFileDialog
        {
            Title = isVideo ? "Chọn nhiều video" : "Chọn nhiều MP3",
            Multiselect = true,
            Filter = isVideo
                ? "Video|*.mp4;*.mkv;*.mov;*.avi;*.webm;*.m4v;*.ts;*.wmv|Tất cả tệp|*.*"
                : "Âm thanh MP3|*.mp3|Tất cả tệp|*.*"
        };
        if (picker.ShowDialog(this) == DialogResult.OK)
            AddFiles(picker.FileNames, isVideo);
    }

    private void AddFiles(IEnumerable<string> paths, bool isVideo)
    {
        var list = isVideo ? _videos : _mp3s;
        var allowed = isVideo
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".mp4", ".mkv", ".mov", ".avi", ".webm", ".m4v", ".ts", ".wmv" }
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".mp3" };
        var existing = new HashSet<string>(list.Items.Cast<MediaFile>().Select(x => x.FullPath), StringComparer.OrdinalIgnoreCase);
        list.BeginUpdate();
        try
        {
            foreach (var file in paths)
            {
                if (!File.Exists(file) || !allowed.Contains(Path.GetExtension(file))) continue;
                var full = Path.GetFullPath(file);
                if (existing.Add(full)) list.Items.Add(new MediaFile(full, isVideo));
            }
        }
        finally { list.EndUpdate(); }
        UpdateCounts();
        RefreshLayoutPreview();
    }

    private void RemoveSelected(bool isVideo)
    {
        var list = isVideo ? _videos : _mp3s;
        foreach (var item in list.SelectedItems.Cast<object>().ToArray())
            list.Items.Remove(item);
        UpdateCounts();
        RefreshLayoutPreview();
    }

    private static void MoveItem(ListBox list, int direction)
    {
        if (list.SelectedIndices.Count != 1) return;
        var index = list.SelectedIndex;
        var next = index + direction;
        if (next < 0 || next >= list.Items.Count) return;
        var current = list.Items[index];
        list.Items.RemoveAt(index);
        list.Items.Insert(next, current);
        list.SelectedIndex = next;
    }

    private void ConfigureDragDrop(ListBox list, bool isVideo)
    {
        list.AllowDrop = true;
        list.DragEnter += (_, e) =>
            e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
                ? DragDropEffects.Copy : DragDropEffects.None;
        list.DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
                AddFiles(paths, isVideo);
        };
    }

    private void ChooseImage(TextBox field, PictureBox preview)
    {
        using var picker = new OpenFileDialog
        {
            Title = "Chọn ảnh chèn vào video",
            Filter = "Ảnh|*.png;*.jpg;*.jpeg;*.bmp;*.webp|Tất cả tệp|*.*"
        };
        if (picker.ShowDialog(this) == DialogResult.OK)
        {
            field.Text = picker.FileName;
            LoadPreview(picker.FileName, preview);
        }
    }

    private static void ClearImage(TextBox field, PictureBox preview)
    {
        field.Text = "";
        var old = preview.Image;
        preview.Image = null;
        old?.Dispose();
    }

    private static void LoadPreview(string file, PictureBox preview)
    {
        var old = preview.Image;
        preview.Image = null;
        old?.Dispose();
        try
        {
            // Tạo bản sao để Windows không khóa tệp ảnh gốc trong khi FFmpeg xử lý.
            using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var image = Image.FromStream(fs);
            preview.Image = new Bitmap(image);
        }
        catch (Exception ex) when (ex is ArgumentException or OutOfMemoryException or IOException)
        {
            // Một số bản GDI+ không xem trước WebP, nhưng FFmpeg vẫn xử lý được.
        }
    }

    private void ChooseFolder()
    {
        using var picker = new FolderBrowserDialog
        {
            Description = "Chọn thư mục lưu tệp MP4 duy nhất",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_folderPath.Text) ? _folderPath.Text : Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
        };
        if (picker.ShowDialog(this) == DialogResult.OK)
            _folderPath.Text = picker.SelectedPath;
    }

    private RenderOptions CollectOptions()
    {
        if (_videos.Items.Count == 0) throw new ArgumentException("Bạn cần chọn ít nhất một video.");
        if (_mp3s.Items.Count == 0) throw new ArgumentException("Bạn cần chọn ít nhất một MP3.");

        var videos = _videos.Items.Cast<MediaFile>()
            .Select(x => new VideoClipOption(x.FullPath, x.PlaybackSpeed)).ToArray();
        var mp3s = _mp3s.Items.Cast<MediaFile>().Select(x => x.FullPath).ToArray();
        var overlays = new List<OverlayOption>();
        foreach (var slot in _overlaySlots)
            if (!string.IsNullOrWhiteSpace(slot.PathBox.Text))
                overlays.Add(new OverlayOption(slot.PathBox.Text, slot.Corner, (int)slot.WidthBox.Value, (int)slot.HeightBox.Value, (int)slot.OffsetXBox.Value, (int)slot.OffsetYBox.Value));

        var (w, h) = _resolution.SelectedIndex switch
        {
            0 => (1280, 720),
            2 => (3840, 2160),
            _ => (1920, 1080)
        };
        SubtitleOptions? subtitles = null;
        if (_srts.Items.Count > 0)
            subtitles = new SubtitleOptions(
                _srts.Items.Cast<SubtitleFile>().Select(x => x.FullPath).ToArray(),
                _srtMode.SelectedIndex == 1 ? SubtitleImportMode.OneSrtPerMp3 : SubtitleImportMode.GlobalTimeline,
                _burnSrt.Checked, _exportSrt.Checked, (int)_subOffset.Value,
                _subFont.SelectedItem?.ToString() ?? "Arial", (int)_subFontSize.Value);
        return new RenderOptions(videos, mp3s, overlays, _folderPath.Text.Trim(),
            w, h, 30, _muteSourceAudio.Checked,
            _encoder.SelectedIndex == 1 ? VideoEncoder.Nvenc : VideoEncoder.X264, subtitles);
    }

    private async Task StartRenderAsync()
    {
        if (_rendering) return;
        try
        {
            var options = CollectOptions();
            var tools = FfmpegLocator.Find();
            _cts = new CancellationTokenSource();
            SetBusy(true);
            _progress.Value = 0;
            _previousStage = "";
            var reporter = new Progress<RenderProgress>(p =>
            {
                if (IsDisposed) return;
                if (!string.Equals(_previousStage, p.Stage, StringComparison.Ordinal))
                {
                    _previousStage = p.Stage;
                    _status.Text = p.Stage;
                }
                if (p.Percent is double percent)
                    _progress.Value = Math.Clamp((int)Math.Round(percent * 10), 0, _progress.Maximum);
            });
            var output = await new RenderEngine(tools).RenderAsync(options, reporter, _cts.Token);
            if (!IsDisposed)
            {
                _status.Text = $"Đã xuất 1 file MP4: {Path.GetFileName(output)}";
                MessageBox.Show(this, "Đã xuất 1 video duy nhất:\n\n" +
                    output + (File.Exists(Path.ChangeExtension(output, ".srt"))
                        ? "\n\nKèm file SRT đồng bộ: " + Path.ChangeExtension(output, ".srt") : ""),
                    "Hoàn thành", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (OperationCanceledException)
        {
            _status.Text = "Đã hủy. Không có MP4 chưa hoàn chỉnh trong thư mục đầu ra.";
        }
        catch (Exception ex)
        {
            _status.Text = "Xuất video thất bại. Xem chi tiết thông báo lỗi.";
            MessageBox.Show(this, ex.Message, "Không thể xuất video", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            SetBusy(false);
            if (_closeAfterCancel && !IsDisposed)
            {
                _closeAfterCancel = false;
                Close();
            }
        }
    }

    private void SetBusy(bool busy)
    {
        _rendering = busy;
        foreach (var c in _editableControls) c.Enabled = !busy;
        _render.Enabled = !busy;
        _cancel.Enabled = busy;
    }

    private void UpdateCounts()
    {
        _videoCount.Text = $"{_videos.Items.Count} video";
        _mp3Count.Text = $"{_mp3s.Items.Count} MP3";
        _srtCount.Text = $"{_srts.Items.Count} SRT";
    }

    private void OpenFolder()
    {
        if (!Directory.Exists(_folderPath.Text))
        {
            MessageBox.Show(this, "Chưa có thư mục đầu ra.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = _folderPath.Text, UseShellExecute = true
        });
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_rendering)
        {
            e.Cancel = true;
            if (MessageBox.Show(this, "Đang xuất video. Hủy xuất và thoát?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _closeAfterCancel = true;
                _cts?.Cancel();
            }
        }
        else
        {
            foreach (var slot in _overlaySlots)
                slot.Thumbnail.Image?.Dispose();
            _layoutPreview.Image?.Dispose();
        }
    }

    private static Panel NewCard() => new StudioCard()
    {
        Dock = DockStyle.Fill, Padding = new Padding(18, 15, 18, 13),
        Margin = new Padding(7), BackColor = CardColor
    };

    private static Label NewLabel(string value, int size, Color color, bool bold = false) => new()
    {
        Text = value, AutoSize = false, Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
        ForeColor = color, BackColor = Color.Transparent
    };

    private static Label NewInlineLabel(string text) => new()
    {
        Text = text, AutoSize = true, ForeColor = Muted, BackColor = Color.Transparent,
        Margin = new Padding(12, 7, 4, 0)
    };

    private static TextBox NewTextBox() => new()
    {
        BackColor = InputColor, ForeColor = Foreground, BorderStyle = BorderStyle.FixedSingle,
        Font = new Font("Segoe UI", 10), Margin = new Padding(0, 2, 3, 3)
    };

    private static ListBox NewListBox() => new()
    {
        BackColor = InputColor, ForeColor = Foreground, BorderStyle = BorderStyle.None,
        IntegralHeight = false, SelectionMode = SelectionMode.MultiExtended,
        HorizontalScrollbar = true, ItemHeight = 25,
        Font = new Font("Segoe UI", 10)
    };

    private static PictureBox NewPreview() => new()
    {
        Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom,
        BackColor = Color.FromArgb(255, 248, 242), Margin = new Padding(0, 0, 8, 0)
    };

    private static ComboBox NewCombo() => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat,
        BackColor = InputColor, ForeColor = Foreground,
        Font = new Font("Segoe UI", 10)
    };

    private static NumericUpDown NewNumber(int value, int min, int max) => new()
    {
        Minimum = min, Maximum = max, Value = value, Width = 57,
        BackColor = InputColor, ForeColor = Foreground,
        BorderStyle = BorderStyle.FixedSingle,
        Font = new Font("Segoe UI", 10)
    };

    private static Button NewButton(string text, bool primary) => new StudioButton()
    {
        Text = text, Height = 34, Width = 120,
        BackColor = primary ? Accent : Color.FromArgb(255, 242, 228), ForeColor = primary ? Color.White : Foreground,
        FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        Margin = new Padding(0, 0, 8, 0), UseVisualStyleBackColor = false,
        FlatAppearance = { BorderSize = 0, MouseOverBackColor = primary ? Color.FromArgb(182, 34, 26) : Color.FromArgb(250, 232, 214) }
    };

    private sealed record MediaFile(string FullPath, bool IsVideo, double PlaybackSpeed = 1.0)
    {
        public override string ToString() => IsVideo
            ? $"{Path.GetFileName(FullPath)}    [{PlaybackSpeeds.Format(PlaybackSpeed)}]"
            : Path.GetFileName(FullPath);
    }

    private sealed record SubtitleFile(string FullPath, int Cues)
    {
        public override string ToString() => $"{Path.GetFileName(FullPath)}  •  {Cues} câu";
    }

    private sealed record SpeedOption(double Value)
    {
        public override string ToString() => PlaybackSpeeds.Format(Value);
    }

    private sealed record OverlaySlot(Corner Corner, string Title, TextBox PathBox, PictureBox Thumbnail, NumericUpDown WidthBox, NumericUpDown HeightBox, NumericUpDown OffsetXBox, NumericUpDown OffsetYBox);
}
