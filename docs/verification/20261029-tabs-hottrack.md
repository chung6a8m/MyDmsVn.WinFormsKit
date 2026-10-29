# TAB-01 — trace HotTrack và supplied-HDC native paint

Ngày chạy: 29/10/2026. Candidate tại lúc chạy lại: commit `bde004d3dfe8a2eb8d8dfe9699e31c80636778a3` cộng sửa probe trong `TabsHoverScenario.cs` (SHA-256 file `EFB96E709FFC447E6211FF8A228449AF896D3BD1E65C36208FAFFAEFDBB7AB7A`). Sửa probe được đưa vào commit tiếp theo của worktree. SourceGrid gitlink giữ `f4e457b43582bf01892f50bdc74aa480531e5944`; checkout worktree chưa init submodule, không ảnh hưởng Controls/VisualStyleHost build.

## Môi trường và lệnh

Windows NT 10.0.26300.0, process 64 bit, SDK `8.0.427`; .NET Framework CLR `4.0.30319.42000` và .NET `8.0.33`. Desktop tương tác, `Form.DeviceDpi=96`, `GetDpiForWindow=96`; đây là physical 100%, chưa có 125%/150%/200% hoặc mixed-DPI.

```powershell
dotnet build tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.csproj -c Release
# Mỗi EXE chạy tuần tự bằng Start-Process; WaitForExit(30000), Kill nếu timeout.
<VisualStyleHost-net48.exe> --scenario tabs-hover artifacts/verification/tabs/net48
<VisualStyleHost-net8.0-windows.exe> --scenario tabs-hover artifacts/verification/tabs/net8
```

Build: 2/2 TFM thành công, 0 warning, 0 error. Helper bounded run: net48 exit 0 trong 286 ms; net8 exit 0 trong 313 ms. Không có NUnit discovery trong TAB-01; helper có 1 scenario/TFM, executed 2, probe pass 2, fail 0, skip 0. Raw PNG/trace/JSON ở `artifacts/verification/tabs/` là output local bị Git ignore, không phải evidence URL bền vững. Chưa upload artifact; Owner visual review **PENDING**.

## Quan sát

- Baseline có native `TabControl`, `BootstrapTabControl` với HotTrack on/off, direct `TabControl` owner-draw probe. Mỗi control có caption Việt, icon, caption dài và overflow; control cũ có close opt-in. Overflow child HWND `msctls_updown32` hiện diện ở probe trên cả hai TFM.
- Di chuyển pointer desktop thật qua ba header đầu. Với BootstrapTabControl hot/cold, `Font`, `ItemSize`, mọi `GetTabRect`, `SelectedIndex` và HWND giữ nguyên trước/sau trên hai TFM. Flicker nhìn thấy: **NOT REPRODUCED** ở lần chạy này; capture before/after/final local chưa được Owner duyệt.
- `WM_PAINT` với HDC cung cấp bởi bitmap đi qua `base.WndProc`: bốn `DrawItem` được gọi ở paint depth 1, gồm selected header với bounds mở rộng (`net8: {X=0,Y=0,Width=70,Height=24}`; `net48: {X=0,Y=0,Width=66,Height=23}`). Nonselected bounds không giống hẳn `GetTabRect`; painter mới phải giữ bounds từ event thực.
- `WM_PRINTCLIENT` với cùng target HDC gọi bốn `DrawItem` tại print depth 1, ngoài `WM_PAINT` nhưng trong print pass hợp lệ. Trace không thấy `DrawItem` ngoài hai pass này. Probe chưa kiểm đầy đủ flags và nested print.
- Supplied-HDC buffer với viewport origin `(17,11)` có selected header pixels khác nền Fuchsia: net48 588 pixels, net8 689 pixels. Sau native pass, helper tô marker chrome màu Lime vào cùng bitmap. Dirty clip giới hạn header đầu; header có pixels, vùng page kiểm tại `(120,80,40,40)` vẫn Fuchsia trên cả hai TFM. Đây là proof bitmap của seam; chưa chứng minh production compositor hoặc child exclusion toàn bộ.
- Mouse thật click mũi tên phải của `msctls_updown32`: `GetTabRect(0).X` đổi `2→-60` trên net48, `2→-64` trên net8. Overflow child vẫn nhận click trong baseline probe. Chưa có control mới để kiểm chrome có phủ lên child hay không.

## Gate

**TAB-01 chưa DONE; native paint gate OPEN.** Supplied-HDC dispatch, dirty header clip và overflow click của baseline probe hoạt động trên hai runtime ở physical 100%, đủ trace đầu vào cho TAB-02. Trước TAB-04 cần proof final compositor với child exclusion, print flags/nested print và không có đường native drawing ngoài buffer khi control mới hoàn thiện. Physical 125% và artifact URL/Owner visual decision chưa có; không suy ra no-flicker từ exit 0 hoặc một bitmap.
