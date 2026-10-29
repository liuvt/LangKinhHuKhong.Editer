# LangKinhHuKhong.Editer v3.0 — WinForms Video Studio

**Windows 10/11 x64 · C# .NET 8 · FFmpeg + ffprobe · 4 ảnh PNG độc lập · SRT**

Bản v3.0 sửa lỗi giao diện của v2.5 và lỗi FFmpeg khi dịch ảnh theo trục ngang/dọc. Đây là **mã nguồn**, chưa phải EXE đã biên dịch hoặc giao diện đã được kiểm thử trên Windows.

## Thay đổi quan trọng của v3.0

1. **Sửa layout bốn ảnh:** tăng chiều cao mỗi hàng card, chuyển các nút/ô số sang lưới cố định, tránh hiện tượng `Lên/Xuống %` bị khuất hay tràn ra ngoài card.
2. **Mỗi ảnh một bộ thông số riêng:** `Rộng %`, `Cao %` mặc định 20, giới hạn 20–100; `Ngang %`, `Dọc %` mặc định 1, giới hạn 0–100, đo từ góc của ảnh đang chọn.
3. **Giữ ảnh trong video:** cả preview và FFmpeg đều giới hạn dịch chuyển để không có phần nào của ảnh vượt ra ngoài màn hình. Nếu ảnh chiếm hết chiều rộng/chiều cao, dịch chuyển ở trục đó sẽ không còn tác dụng.
4. **Sửa FFmpeg filter graph:** escape dấu phẩy cho `min()` và `max()` của overlay. Trước đó một số lựa chọn dịch vị trí có thể khiến quá trình export bị lỗi.
5. **Preview đúng tỉ lệ khung hình:** khu vực kết quả mô phỏng dùng tỷ lệ 16:9; không còn vùng ảnh vẽ bị bóp tỷ lệ. Preview vẽ ảnh PNG đúng kích thước/vị trí tương đối sau khi điều chỉnh.
6. **Preview phụ đề:** chỉ vẽ phụ đề khi đã nhập SRT và bật chèn chữ. Chữ phụ đề được render cuối, ở trên các PNG.
7. **Version:** UI, `Version`, `FileVersion`, `AssemblyVersion` đều là `3.0` / `3.0.0.0`.

## Cách chỉnh 4 vị trí ảnh

| Ô | Neo ảnh | Điều khiển |
| --- | --- | --- |
| Ảnh 1 | Trên trái | Rộng / Cao / Ngang / Dọc riêng |
| Ảnh 2 | Trên phải | Rộng / Cao / Ngang / Dọc riêng |
| Ảnh 3 | Dưới trái | Rộng / Cao / Ngang / Dọc riêng |
| Ảnh 4 | Dưới phải | Rộng / Cao / Ngang / Dọc riêng |

- Kéo tệp PNG/JPG/WebP từ Explorer vào đúng ô, hoặc bấm **CHỌN ẢNH**.
- **BỎ ẢNH** chỉ xóa ô đó. Kéo ảnh khác vào sẽ thay ảnh cũ.
- `Ngang %` và `Dọc %` là khoảng dịch chuyển vào trong từ góc neo tương ứng; **không** phải tọa độ âm hay thao tác kéo ra khỏi khung hình.
- Preview cũng nhận ảnh thả vào phần tư tương ứng. Khi chọn kích thước tối đa 100%, ảnh vẫn giữ nguyên tỷ lệ gốc.
- Preview là **bản mô phỏng bố cục tĩnh**, không phải một khung hình trích từ video nguồn; hiển thị chữ GDI+ có thể khác vài pixel so với FFmpeg/libass.

## Luồng xuất giữ nguyên

Video 1 → Video 2 → … (mỗi video có tốc độ riêng 0.4× / 0.5× / 0.75× / 1× / 1.5× / 2×) → **lặp cả danh sách nếu cần**, dừng theo thời lượng audio sau khi **nối toàn bộ MP3**.

- Xuất **một MP4 duy nhất** (720p / 1080p / 4K).
- Mặc định bỏ âm thanh video gốc; hoặc trộn âm gốc ở mức 20% với MP3.
- 0–4 ảnh góc hiển thị hết thời lượng video.
- Nếu nhập `.srt`: một SRT toàn bộ hoặc một SRT cho mỗi MP3, tự dời mốc thời gian theo MP3; có thể đốt vào video và/hoặc lưu `.srt` rời. **Phụ đề nằm trên cùng**.
- H.264 CPU hoặc NVIDIA NVENC; hủy render và thanh tiến trình.

## Cách build/publish trên Windows

Cài **.NET 8 SDK** và FFmpeg bản có `ffprobe.exe` và bộ lọc `subtitles`/`libass` (nếu dùng burn-in).

Trong CMD tại thư mục project:

```bat
dotnet clean
dotnet build -c Release
publish-win-x64.bat
```

Hoặc chạy thẳng `publish-win-x64.bat`. EXE được tạo tại `publish\win-x64\LangKinhHuKhong.Editer.exe`. Đặt `ffmpeg.exe`, `ffprobe.exe` bên cạnh EXE hoặc trong `PATH`. Ảnh logo và icon nằm ở `Assets` và được copy vào thư mục publish.

## Kiểm tra FFmpeg / source

```bat
python tests/v3_position_smoke.py
python tests/overlay_four_corners_smoke.py
python tests/srt_sync_smoke.py
```

`v3_position_smoke.py` kiểm thử bốn overlay có Rộng/Cao/Ngang/Dọc khác nhau, filter `min/max` với dấu phẩy được escape và điều kiện không tràn khung hình. Các bài thử FFmpeg đã chạy thành công trong môi trường tạo mã nguồn.

**Giới hạn kiểm chứng:** môi trường này không có .NET SDK, Windows Forms hoặc Desktop Windows. Vì vậy chưa thể khẳng định phần UI/publish đã biên dịch và chạy thành công trên máy Windows; cần chạy `dotnet build` trên máy đích. Nếu phát sinh lỗi, log build kèm dòng và nội dung lỗi là cơ sở để sửa chính xác.
