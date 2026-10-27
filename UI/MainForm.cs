using System.Drawing;
using LangKinhHuKhong.Editer.Core;

namespace LangKinhHuKhong.Editer.UI;

public sealed class MainForm : Form
{
    private static readonly Color Background = Color.FromArgb(18, 22, 31);
    private static readonly Color CardColor = Color.FromArgb(30, 36, 49);
    private static readonly Color InputColor = Color.FromArgb(42, 49, 65);
    private static readonly Color Foreground = Color.FromArgb(234, 238, 247);
    private static readonly Color Muted = Color.FromArgb(166, 176, 195);
    private static readonly Color Accent = Color.FromArgb(64, 152, 252);

    private readonly ListBox _videos = NewListBox();
    private readonly ListBox _mp3s = NewListBox();
    private readonly Label _videoCount = NewLabel("0 video", 10, Muted);
    private readonly Label _mp3Count = NewLabel("0 MP3", 10, Muted);
    private readonly TextBox _image1Path = NewTextBox();
    private readonly TextBox _image2Path = NewTextBox();
    private readonly PictureBox _preview1 = NewPreview();
    private readonly PictureBox _preview2 = NewPreview();
    private readonly ComboBox _corner1 = NewCombo();
    private readonly ComboBox _corner2 = NewCombo();
    private readonly TextBox _folderPath = NewTextBox();
    private readonly ComboBox _resolution = NewCombo();
    private readonly ComboBox _encoder = NewCombo();
    private readonly NumericUpDown _overlayWidth = NewNumber(29, 10, 65);
    private readonly NumericUpDown _overlayHeight = NewNumber(56, 10, 90);
    private readonly NumericUpDown _margin = NewNumber(1, 0, 12);
    private readonly CheckBox _muteSourceAudio = new();
    private readonly ComboBox _selectedVideoSpeed = NewCombo();
    private readonly Button _applySpeed = NewButton("ÁP DỤNG", true);
    private readonly ProgressBar _progress = new();
    private readonly Label _status = NewLabel("Sẵn sàng.", 10, Muted);
    private readonly Label _ffmpegStatus = NewLabel("FFmpeg: đang kiểm tra...", 10, Muted);
    private readonly Button _render = NewButton("▶  BẮT ĐẦU XUẤT", true);
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
        Text = "LangKinhHuKhong.Editer v1.2  |  Video + MP3 + Ảnh + Tốc độ từng video";
        MinimumSize = new Size(1000, 760);
        Size = new Size(1380, 900);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Background;
        ForeColor = Foreground;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        AutoScaleMode = AutoScaleMode.Dpi;

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
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Background,
            Margin = Padding.Empty, Padding = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 91));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 142));
        Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = CardColor, Padding = new Padding(24, 12, 24, 8) };
        var title = NewLabel("LANG KINH HƯ KHÔNG  /  VIDEO EDITER", 19, Foreground, true);
        title.Dock = DockStyle.Top;
        title.Height = 36;
        var subtitle = NewLabel("Tốc độ từng video  •  Bật/tắt âm video  •  Ghép MP3  •  2 ảnh overlay  •  720p / 1080p / 4K", 10, Muted);
        subtitle.Dock = DockStyle.Top;
        subtitle.Height = 25;
        header.Controls.Add(subtitle);
        header.Controls.Add(title);
        root.Controls.Add(header, 0, 0);

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        root.Controls.Add(scroll, 0, 1);
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2, RowCount = 4, Padding = new Padding(14, 16, 14, 14),
            BackColor = Background, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 358));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 196));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        scroll.Controls.Add(body);

        body.Controls.Add(CreateMediaCard("01  VIDEO NGUỒN (THEO THỨ TỰ)", _videos, _videoCount, true), 0, 0);
        body.Controls.Add(CreateMediaCard("02  MP3 (NỐI LIỀN THÀNH 1 AUDIO)", _mp3s, _mp3Count, false), 1, 0);
        body.Controls.Add(CreateOverlayCard("03  ẢNH 1", _image1Path, _preview1, _corner1, true), 0, 1);
        body.Controls.Add(CreateOverlayCard("04  ẢNH 2 (KHÔNG BẮT BUỘC)", _image2Path, _preview2, _corner2, false), 1, 1);
        body.Controls.Add(CreateSettingsCard(), 0, 2);
        body.SetColumnSpan(body.GetControlFromPosition(0, 2)!, 2);

        var note = NewLabel("Ảnh 1 mặc định ở góc dưới trái, ảnh 2 ở góc dưới phải. Ảnh giữ nguyên tỉ lệ, được tự thu/phóng và nằm trên cùng trong suốt video.", 10, Muted);
        note.Margin = new Padding(12, 8, 0, 0);
        note.AutoSize = true;
        body.Controls.Add(note, 0, 3);
        body.SetColumnSpan(note, 2);

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(24, 10, 24, 10),
            BackColor = CardColor
        };
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        footer.Controls.Add(_status, 0, 0);
        _status.Dock = DockStyle.Fill;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _progress.Minimum = 0;
        _progress.Maximum = 1000;
        _progress.Style = ProgressBarStyle.Continuous;
        _progress.Dock = DockStyle.Fill;
        footer.Controls.Add(_progress, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, Padding = new Padding(0, 10, 0, 0)
        };
        _render.Size = new Size(198, 47);
        _cancel.Size = new Size(104, 47);
        _openFolder.Size = new Size(155, 47);
        _cancel.Enabled = false;
        buttons.Controls.Add(_render);
        buttons.Controls.Add(_cancel);
        buttons.Controls.Add(_openFolder);
        buttons.Controls.Add(_ffmpegStatus);
        _ffmpegStatus.AutoSize = true;
        _ffmpegStatus.Padding = new Padding(0, 14, 8, 0);
        footer.Controls.Add(buttons, 0, 2);
        root.Controls.Add(footer, 0, 2);
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
            var note = NewLabel("MP3 1 → MP3 2 → MP3 3 → ... = 1 audio; xuất duy nhất 1 MP4.", 10, Muted);
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
        var caption = NewInlineLabel("Tốc độ chọn:");
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
        _muteSourceAudio.BackColor = _muteSourceAudio.Checked ? InputColor : Color.FromArgb(30, 105, 80);
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

    private Panel CreateOverlayCard(string title, TextBox pathBox,
        PictureBox preview, ComboBox cornerBox, bool first)
    {
        var panel = NewCard();
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(preview, 0, 0);
        var controls = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
            Padding = new Padding(10, 0, 0, 0)
        };
        controls.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        controls.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        controls.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        controls.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        controls.Controls.Add(NewLabel(title, 11, Foreground, true), 0, 0);
        pathBox.Dock = DockStyle.Fill;
        pathBox.ReadOnly = true;
        pathBox.PlaceholderText = "Chưa chọn ảnh...";
        controls.Controls.Add(pathBox, 0, 1);
        var row = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        var browse = NewButton("CHỌN ẢNH", true);
        var clear = NewButton("BỎ ẢNH", false);
        browse.Width = 113;
        clear.Width = 100;
        browse.Click += (_, _) => ChooseImage(pathBox, preview);
        clear.Click += (_, _) => ClearImage(pathBox, preview);
        row.Controls.AddRange([browse, clear]);
        controls.Controls.Add(row, 0, 2);
        var cornerRow = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var cornerLabel = NewLabel("Góc đặt:", 10, Muted);
        cornerLabel.Padding = new Padding(0, 7, 0, 0);
        cornerLabel.AutoSize = true;
        cornerBox.Width = 170;
        cornerBox.Items.AddRange([
            new CornerOption(Corner.BottomLeft, "Dưới trái"),
            new CornerOption(Corner.BottomRight, "Dưới phải"),
            new CornerOption(Corner.TopLeft, "Trên trái"),
            new CornerOption(Corner.TopRight, "Trên phải")
        ]);
        cornerBox.SelectedIndex = first ? 0 : 1;
        cornerRow.Controls.AddRange([cornerLabel, cornerBox]);
        controls.Controls.Add(cornerRow, 0, 3);
        layout.Controls.Add(controls, 1, 0);
        panel.Controls.Add(layout);
        _editableControls.AddRange([browse, clear, cornerBox]);
        return panel;
    }

    private Panel CreateSettingsCard()
    {
        var panel = NewCard();
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 29));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        table.Controls.Add(NewLabel("05  CẤU HÌNH XUẤT VIDEO", 11, Foreground, true), 0, 0);

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
        optionsRow.Controls.Add(NewInlineLabel("Rộng ảnh %:"));
        optionsRow.Controls.Add(_overlayWidth);
        optionsRow.Controls.Add(NewInlineLabel("Cao ảnh %:"));
        optionsRow.Controls.Add(_overlayHeight);
        optionsRow.Controls.Add(NewInlineLabel("Lề %:"));
        optionsRow.Controls.Add(_margin);
        table.Controls.Add(optionsRow, 0, 2);

        var autoLoopNote = NewLabel("TỰ ĐỘNG: Nối toàn bộ video, lặp từ Video 1 nếu thiếu; độ dài MP4 = tổng MP3 đã nối.", 10, Muted);
        autoLoopNote.AutoSize = true;
        autoLoopNote.Padding = new Padding(0, 5, 0, 0);
        table.Controls.Add(autoLoopNote, 0, 3);
        panel.Controls.Add(table);
        _editableControls.AddRange([_folderPath, browse, _resolution, _encoder,
            _overlayWidth, _overlayHeight, _margin]);
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
        _tooltip.SetToolTip(_videos, "Kéo thả video. Chọn 1 hoặc nhiều dòng, chọn tốc độ rồi nhấn ÁP DỤNG; dùng ↑ ↓ để đổi thứ tự.");
        _tooltip.SetToolTip(_selectedVideoSpeed, "0.4× = 2.5 lần thời lượng; 0.5× = 2 lần; 2× = còn một nửa.");
        _tooltip.SetToolTip(_muteSourceAudio, "Mặc định tắt tiếng gốc. Bấm để giữ âm gốc ở 20% và trộn với MP3; video không có âm vẫn xuất được.");
        _tooltip.SetToolTip(_mp3s, "Có thể kéo thả nhiều MP3. Nối chúng theo thứ tự thành một audio, xuất đúng một MP4.");
        _tooltip.SetToolTip(_overlayWidth, "Giới hạn chiều rộng ảnh theo phần trăm chiều rộng video.");
        _tooltip.SetToolTip(_overlayHeight, "Giới hạn chiều cao ảnh theo phần trăm chiều cao video.");
        FormClosing += OnFormClosing;
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
            _ffmpegStatus.ForeColor = Color.FromArgb(110, 219, 158);
            _tooltip.SetToolTip(_ffmpegStatus, found.Ffmpeg);
        }
        catch (FileNotFoundException)
        {
            _ffmpegStatus.Text = "⚠ Chưa có FFmpeg (xem README)";
            _ffmpegStatus.ForeColor = Color.FromArgb(255, 187, 98);
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
    }

    private void RemoveSelected(bool isVideo)
    {
        var list = isVideo ? _videos : _mp3s;
        foreach (var item in list.SelectedItems.Cast<object>().ToArray())
            list.Items.Remove(item);
        UpdateCounts();
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
        if (!string.IsNullOrWhiteSpace(_image1Path.Text))
            overlays.Add(new OverlayOption(_image1Path.Text, ((CornerOption)_corner1.SelectedItem!).Value));
        if (!string.IsNullOrWhiteSpace(_image2Path.Text))
            overlays.Add(new OverlayOption(_image2Path.Text, ((CornerOption)_corner2.SelectedItem!).Value));

        var (w, h) = _resolution.SelectedIndex switch
        {
            0 => (1280, 720),
            2 => (3840, 2160),
            _ => (1920, 1080)
        };
        return new RenderOptions(videos, mp3s, overlays, _folderPath.Text.Trim(),
            w, h, 30, (int)_overlayWidth.Value, (int)_overlayHeight.Value,
            (int)_margin.Value, _muteSourceAudio.Checked,
            _encoder.SelectedIndex == 1 ? VideoEncoder.Nvenc : VideoEncoder.X264);
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
                    output,
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
            _preview1.Image?.Dispose();
            _preview2.Image?.Dispose();
        }
    }

    private static Panel NewCard() => new()
    {
        Dock = DockStyle.Fill, Padding = new Padding(16, 13, 16, 12),
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
        Text = text, AutoSize = true, ForeColor = Muted, BackColor = CardColor,
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
        BackColor = InputColor, Margin = new Padding(0, 0, 8, 0)
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

    private static Button NewButton(string text, bool primary) => new()
    {
        Text = text, Height = 34, Width = 120,
        BackColor = primary ? Accent : InputColor, ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        Margin = new Padding(0, 0, 8, 0), UseVisualStyleBackColor = false
    };

    private sealed record MediaFile(string FullPath, bool IsVideo, double PlaybackSpeed = 1.0)
    {
        public override string ToString() => IsVideo
            ? $"{Path.GetFileName(FullPath)}    [{PlaybackSpeeds.Format(PlaybackSpeed)}]"
            : Path.GetFileName(FullPath);
    }

    private sealed record SpeedOption(double Value)
    {
        public override string ToString() => PlaybackSpeeds.Format(Value);
    }

    private sealed record CornerOption(Corner Value, string Display)
    {
        public override string ToString() => Display;
    }
}
