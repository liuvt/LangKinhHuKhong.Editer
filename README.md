# LangKinhHuKhong.Editer v1.2 — ONE MP4 EXPORT

WinForms .NET 8 dành cho Windows 10/11, giao diện tiếng Việt, sử dụng FFmpeg + FFprobe. **Đã sửa lỗi xuất nhiều MP4 của v1.1.**

## Quan trọng: quy tắc EXPORT mới

**1 lần bấm “BẮT ĐẦU XUẤT” = 1 file MP4 duy nhất**, bất kể có bao nhiêu video hoặc MP3.

1. Chọn Video 1, Video 2, Video 3... và sắp xếp theo thứ tự phát.
2. Chọn tốc độ **riêng cho từng video**: 0.4×, 0.5×, 0.75×, 1×, 1.5×, 2×. 0.4× kéo dài thời lượng gốc 2.5 lần; 2× rút ngắn còn một nửa. Tốc độ *không* thay đổi tự động ngoài mức người dùng đã chọn.
3. Chọn MP3 1, MP3 2, MP3 3... và sắp xếp theo thứ tự đọc. **Toàn bộ MP3 được ghép tuần tự thành MỘT audio liên tục** (không phát chồng lên nhau). Tốc độ và cao độ MP3 không bị thay đổi.
4. Nối tất cả video đã điều chỉnh tốc độ thành một chuỗi: **V1 → V2 → V3**. Nếu thời lượng chuỗi video nhỏ hơn audio tổng, **tự động lặp lại toàn bộ chuỗi V1 → V2 → V3**, không chỉ lặp một video, đến đủ thời lượng. Nếu chuỗi video dài hơn audio thì tự cắt ở cuối audio.
5. Gắn audio tổng vào chuỗi video đã nối/lặp, loại bỏ hoặc trộn âm gốc video tùy nút bật/tắt.
6. Chèn tối đa hai ảnh logo vào các góc, phía trên cùng trong *suốt* thời lượng video xuất.
7. Xuất đúng **1 tệp**: `LangKinhHuKhong_Full_1080p.mp4` (hoặc `_720p`, `_4K`). Nếu tên đã tồn tại thì thêm `_2`, `_3` để không ghi đè.

Ví dụ:

```text
Video nguồn (phát theo thứ tự):
   V1.mp4 (2 phút) -> 0.5× -> 4 phút
   V2.mp4 (3 phút) -> 1.0× -> 3 phút
   V3.mp4 (1 phút) -> 0.5× -> 2 phút
   Tổng playlist video = 9 phút

Audio nguồn (nối theo thứ tự):
   MP3_01.mp3 (6 phút)
   MP3_02.mp3 (8 phút)
   MP3_03.mp3 (5 phút)
   Tổng audio = 19 phút

Chuỗi xuất:
   V1 → V2 → V3  (9 phút)
   V1 → V2 → V3  (9 phút)
   V1 (chỉ 1 phút đầu của phiên bản phát chậm)

KẾT QUẢ: DUY NHẤT 1 FILE MP4 ~19 phút,
           tiếng kể lần lượt MP3_01 → MP3_02 → MP3_03.
```

*Vì video mã hóa FPS cố định (30fps), thời lượng MP4 có thể chênh khoảng một khung hình so với tổng thời lượng âm thanh đã chuẩn hóa.*

## Các chức năng

- **Video**: kéo thả/chọn nhiều file MP4/MKV/MOV/AVI/WEBM/M4V/TS/WMV, xóa, di chuyển lên/xuống.
- **MP3**: kéo thả/chọn nhiều file, xóa, đổi thứ tự bằng nút ↑ ↓. Nối tuần tự.
- **Tốc độ riêng từng video**: chọn một hoặc nhiều hàng, chọn tốc độ rồi nhấn **ÁP DỤNG**.
- **Tắt âm video**: mặc định **TẮT ÂM GỐC: BẬT** (chỉ âm MP3). Nếu chuyển sang **GIỮ ÂM GỐC: BẬT**, tiếng gốc được trộn ở 20% với MP3. Video gốc không có tiếng vẫn được xử lý an toàn.
- **Hình overlay**: 0, 1 hoặc 2 ảnh PNG/JPG/JPEG/BMP/WEBP; mỗi ảnh được đặt vào một góc riêng. Mặc định dưới trái và dưới phải. Ảnh không bị méo, tối đa 29% chiều rộng / 56% chiều cao video, lề 1%, có thể tùy chỉnh.
- **Chất lượng**: 1280×720 / 1920×1080 / 3840×2160 @30fps; x264 CPU hoặc NVENC NVIDIA (nếu FFmpeg và GPU hỗ trợ).
- **Tiến trình / hủy**: theo dõi quá trình chuẩn hóa audio, ghép audio, chuẩn hóa video, xuất cuối. Hủy giữa chừng không để lại file MP4 hỏng ở thư mục output.

## Pipeline nội bộ

```text
MP3 1 ---normalize FLAC--┐
MP3 2 ---normalize FLAC--┼── concat --> 1 joined_audio.m4a
MP3 3 ---normalize FLAC--┘

Video 1 ---speed 0.5× / normalize--┐
Video 2 ---speed 1.0× / normalize--┼── video playlist → loop nếu thiếu → trim theo audio
Video 3 ---speed 0.5× / normalize--┘                                  |
                                                                       v
joined_audio.m4a -----------------------------------------------> mux/mix
Logo 1 + Logo 2 ------------------------------------------------> overlay
                                                                       |
                                                                       v
                                                 ONE: LangKinhHuKhong_Full_1080p.mp4
```

**Tại sao chuẩn hóa MP3 trước?** Các MP3 có thể có sample rate, kênh và bitrate khác nhau, VBR metadata không đồng nhất. Chuẩn hóa thành FLAC 48 kHz stereo lossless trước khi nối sẽ giữ đúng thứ tự và tránh lỗi ghép trực tiếp các bitstream MP3 không tương thích; audio tổng sau đó được mã hóa AAC chỉ một lần.

Ứng dụng dùng thư mục `%TEMP%/LangKinhHuKhong.Editer/<GUID>` cho file trung gian và dọn sạch sau khi xử lý. Video 4K dài + audio dài cần **nhiều dung lượng trống** cho file trung gian, đặc biệt trên ổ chứa `%TEMP%`.

## Cài đặt trên Windows

Yêu cầu **Windows 10/11 64-bit, .NET 8 SDK** (chỉ để build), **FFmpeg + FFprobe**.

**Cài FFmpeg từ winget:**

```powershell
winget install --id Gyan.FFmpeg -e
ffmpeg -version
ffprobe -version
```

Hoặc đặt `ffmpeg.exe` và `ffprobe.exe` cạnh EXE sau khi build; ứng dụng cũng nhận chúng trong `ffmpeg/`, `tools/ffmpeg/bin/`, `tools/bin/` hoặc PATH.

**Chạy từ source:**

```powershell
dotnet restore .\LangKinhHuKhong.Editer.csproj
dotnet run --project .\LangKinhHuKhong.Editer.csproj -c Release
```

Hoặc click đúp `build.bat`. Để tạo bản EXE tự chứa .NET, click đúp `publish-win-x64.bat`:

```text
publish\win-x64\LangKinhHuKhong.Editer.exe
```

Lưu ý: bản publish tự chứa .NET vẫn cần FFmpeg và FFprobe (2 tệp EXE riêng).

## Kiểm thử

```powershell
python tests\ffmpeg_smoke.py
```

Script tạo các clip mẫu ngắn, kiểm tra 2 MP3 khác sample rate ghép tuần tự, 2 video nối và lặp đúng thứ tự, tốc độ, 2 overlay, âm gốc bật/tắt, trường hợp MP3 ngắn hơn video và độ dài MP4 kết quả. Cần `python`, `ffmpeg` và `ffprobe` trong PATH; **không** cần cài thư viện Python ngoài.

> Giới hạn xác minh: luồng **FFmpeg đã được kiểm thử tự động** trong Linux. Không có .NET SDK/WinForms Windows trong môi trường này để build và mở giao diện; project được bàn giao dưới dạng **mã nguồn**, cần chạy `build.bat` trên máy Windows để biên dịch.

## Cấu trúc

```text
LangKinhHuKhong.Editer/
├── LangKinhHuKhong.Editer.csproj
├── Program.cs
├── Core/
│   ├── FfmpegLocator.cs
│   ├── MediaProbe.cs
│   ├── ProcessRunner.cs
│   ├── RenderOptions.cs
│   └── RenderEngine.cs
├── UI/
│   └── MainForm.cs
├── tests/
│   └── ffmpeg_smoke.py
├── build.bat
├── publish-win-x64.bat
└── README.md
```

### v1.2 vs v1.1

- **Đã bỏ** hành vi “1 MP3 → 1 MP4”.
- **Đã bổ sung** “nối tất cả MP3 → 1 audio; tất cả video → 1 playlist; 1 lượt export → 1 MP4”.
- **Tự động loop playlist khi thiếu**, không cần tích ô loop; không có chế độ giữ hình cuối nữa.
- **Thời gian MP4 bằng audio đã ghép**, không phải thời gian mỗi MP3.
- Giữ các chức năng tốc độ riêng, mute, PNG overlay, resolution, NVENC, tiến trình, hủy.
