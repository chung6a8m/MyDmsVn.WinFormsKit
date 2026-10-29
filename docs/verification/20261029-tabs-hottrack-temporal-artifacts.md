# TAB-06 — manifest clip temporal hover

Ngày upload: 29/10/2026. [Comment PR #4](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/pull/4#issuecomment-6500854046). Candidate `5dfa85477ae535a30a4d9c0a538575c6ee1ccd0f`; production source không đổi từ `5ec6923070c0370d91c9b1eb5932e0fb5960dd76`. Physical 125% trên `\\.\DISPLAY1`, native HWND DPI 120, Windows NT 10.0.26300.0, x64.

VisualStyleHost Release `tabs-temporal` di chuyển pointer desktop thật qua header `0→1→0` năm chu kỳ; mỗi run có 15/15 target đúng HWND, 0 miss, kết quả PASS. FFmpeg 9.0.2 `gdigrab` quay chính cửa sổ ở kích thước 700×420, yêu cầu 60 fps, H.264 MP4 5.5 giây; frame rate thực tế dưới 60 fps như bảng. Không chèn frame, đổi tốc độ hoặc chỉnh nội dung. SHA-256 byte của cả bốn URL tải lại không đăng nhập khớp clip gốc. URL gắn với GitHub user attachments trên PR công khai; thời hạn lưu trữ do GitHub quản lý, không cam kết vô hạn. Raw `trace.txt` và `ready.txt` ở ignored local artifacts, không commit. Xem [report chính](20261029-tabs-hottrack.md) để biết commands, counts và limitations.

| TFM / theme | Clip | Frames / 5.5 s | FPS trung bình | SHA-256 |
|---|---|---:|---:|---|
| net48 Light | [Xem clip](https://github.com/user-attachments/assets/0a3213bd-358b-4502-95ee-54ec0e0cae71) | 279 | 50.7 | `3A2E098C3150552909E80C50B426981A44D5697C044B7469EE40F7A4F9F5AA41` |
| net48 Dark | [Xem clip](https://github.com/user-attachments/assets/0650dc47-4c3f-4911-9baa-7e89b7760266) | 248 | 45.1 | `FB92565AE7311F0C4C20006532E0D1D143C6C5BF4935D420FD340808123F22BE` |
| net8 Light | [Xem clip](https://github.com/user-attachments/assets/42a31c79-40f1-4eb9-951b-ec82276c48ac) | 238 | 43.4 | `2925359C02AFC1F62408C7C404F4256301BA31CE2133A1167ACB23B2FEEB1D87` |
| net8 Dark | [Xem clip](https://github.com/user-attachments/assets/cea1b9d6-2870-4164-93db-70b0232dbc55) | 254 | 46.2 | `2666F031C335F8DD3790793DDF6E722CB43E3A54D7EF9930C8CB2AF46F0B307D` |

**Owner temporal no-flicker decision: PENDING.** Ảnh tĩnh đã APPROVED riêng. Clip này mới bao phủ physical 125%; không thay mixed-DPI, 150%/200% hoặc physical 100% temporal. `fullGate=OPEN`.
