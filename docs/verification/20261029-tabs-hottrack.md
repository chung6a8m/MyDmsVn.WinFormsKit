# TAB-01 — trace HotTrack và supplied-HDC native paint

Ngày chạy: 29/10/2026. Candidate tại lúc chạy: `ae94da5062e024a7a73fb2147a134d0e0fdb49dc` cộng thay đổi chưa commit ở `TabsHoverScenario.cs` và `Program.cs`. SourceGrid gitlink giữ `f4e457b43582bf01892f50bdc74aa480531e5944`; checkout worktree chưa init submodule, không ảnh hưởng Controls/VisualStyleHost build.

## Môi trường và lệnh

Windows NT 10.0.26300.0, process 64 bit, SDK `8.0.427`; .NET Framework CLR `4.0.30319.42000` và .NET `8.0.33`. Desktop tương tác, `Form.DeviceDpi=96`, `GetDpiForWindow=96`; đây là physical 100%, chưa có 125%/150%/200% hoặc mixed-DPI.

```powershell
dotnet build tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.csproj -c Release
# Mỗi EXE chạy tuần tự bằng Start-Process; WaitForExit(30000), Kill nếu timeout.
<VisualStyleHost-net48.exe> --scenario tabs-hover artifacts/verification/tabs/net48
<VisualStyleHost-net8.0-windows.exe> --scenario tabs-hover artifacts/verification/tabs/net8
```

Build: 2/2 TFM thành công, 0 warning, 0 error. Helper: net48 exit 0 trong 266 ms; net8 exit 0 trong 284 ms. Không có NUnit discovery trong TAB-01; helper có 1 scenario/TFM, executed 2, probe pass 2, fail 0, skip 0. Raw PNG/trace/JSON ở `artifacts/verification/tabs/` là output local bị Git ignore, không phải evidence URL bền vững. Chưa upload artifact; Owner visual review **PENDING**.

## Quan sát

- Baseline có native `TabControl`, `BootstrapTabControl` với HotTrack on/off, direct `TabControl` owner-draw probe. Mỗi control có caption Việt, icon, caption dài và overflow; control cũ có close opt-in. Overflow child HWND `msctls_updown32` hiện diện ở probe trên cả hai TFM.
- Di chuyển pointer desktop thật qua ba header đầu. Với BootstrapTabControl hot/cold, `Font`, `ItemSize`, mọi `GetTabRect`, `SelectedIndex` và HWND giữ nguyên trước/sau trên hai TFM. Flicker nhìn thấy: **NOT REPRODUCED** ở lần chạy này; capture before/after/final local chưa được Owner duyệt.
- `WM_PAINT` với HDC cung cấp bởi bitmap đi qua `base.WndProc`: bốn `DrawItem` được gọi ở paint depth 1, gồm selected header với bounds mở rộng (`net8: {X=0,Y=0,Width=70,Height=24}`; `net48: {X=0,Y=0,Width=66,Height=23}`). Nonselected bounds không giống hẳn `GetTabRect`; painter mới phải giữ bounds từ event thực.
- `WM_PRINTCLIENT` với cùng target HDC gọi bốn `DrawItem` tại print depth 1, ngoài `WM_PAINT` nhưng trong print pass hợp lệ. Trace không thấy `DrawItem` ngoài hai pass này. Probe chỉ kiểm đường dispatch; chưa kiểm đầy đủ flags, nested print, dirty clipping và child paint ownership.

## Gate

**TAB-01 chưa DONE; native paint gate OPEN.** Supplied-HDC dispatch hoạt động trên hai runtime ở physical 100%, đủ trace đầu vào cho TAB-02. Để đóng gate trước TAB-04 cần thêm proof dirty một header, pixel composition sau native pass, selected page không bị vẽ đè, overflow child còn click được, print clip/origin và capture chuỗi hover có thể mở lại. Không suy ra no-flicker từ exit 0 hoặc một bitmap. Chưa có artifact URL/Owner decision; không tự đánh dấu PASS cho acceptance.
