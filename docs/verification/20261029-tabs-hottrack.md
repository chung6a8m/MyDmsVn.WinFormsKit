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

## TAB-03 — painter và close extraction

Candidate khi chạy: `e638d53` cộng diff TAB-03 chưa commit trong cùng worktree, Windows NT 10.0.26300.0, SDK `8.0.427`, Release, process test x64. Tests mới khóa thứ tự derived `OnDrawItem` → default renderer/user handler → marker sau base, cùng Graphics/index/bounds và pixel cuối; close pointer callback xóa page đồng bộ vẫn không đổi selection; disabled/unowned page bị từ chối và menu của consumer không bị dispose. Caption closable trước sửa có tâm pixel `80.5` thay vì `76` (RED ở net8), sau sửa PASS. Renderer đo và vẽ text với cùng `NoPadding`, căn nhóm icon/text trong phần content đã trừ close.

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapTabPaintCompatibilityTests|FullyQualifiedName~BootstrapTabControlTests|FullyQualifiedName~BootstrapTabControlRenderLogicTests|FullyQualifiedName~BootstrapTabInteractionTests" --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapTabPaintCompatibilityTests|FullyQualifiedName~BootstrapTabControlTests|FullyQualifiedName~BootstrapTabControlRenderLogicTests|FullyQualifiedName~BootstrapTabInteractionTests" --blame-hang --blame-hang-timeout 5m
```

Discovery/execute trên candidate: `net48` 48/48 PASS, fail 0, skip 0; `net8.0-windows` 48/48 PASS, fail 0, skip 0. Hai lệnh được chạy **tuần tự**. Một lần chạy hai TFM song song trước đó bị loại khỏi evidence vì trái policy native GUI; không dùng kết quả đó để nghiệm thu. Helper `tabs-hover` chạy tuần tự, bounded 30 giây trên hai TFM, exit 0/0 tại DPI 96 sau extraction. Helper vẫn ghi `fullGate=OPEN` và `flicker=NOT REPRODUCED`.

Giới hạn: chưa có physical 125% focus clearance, toàn bộ variant/style/orientation native matrix, Owner visual review hoặc artifact URL. `DrawCompactChrome` và fallback orientation thuộc composition của TAB-04, chưa được nhận là hoàn tất ở TAB-03. TAB-01 gate tiếp tục OPEN; TAB-03 không đóng native acceptance của BootstrapTabs.

## TAB-02 — candidate HotTrack dùng chung

Ngày chạy: 29/10/2026. Candidate `0bbd82e03430f0e901ab1c1f287061259f81e7d3`; SourceGrid gitlink giữ `f4e457b43582bf01892f50bdc74aa480531e5944`. Windows NT 10.0.26300.0, process 64 bit, SDK `8.0.427`, desktop DPI 96 theo helper. Restore dùng cache hiện có; không ghi nhận clean restore. TAB-02 thay hover state bằng `BootstrapTabInteractionController`, lọc `TCS_HOTTRACK` cho custom owner-draw headers, và dùng `InvalidateRect` với `bErase=false` chỉ trên header cần đổi visual. `DrawMode.Normal` giữ native HotTrack.

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapTabControlTests|FullyQualifiedName~BootstrapTabInteractionTests" --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapTabControlTests|FullyQualifiedName~BootstrapTabInteractionTests" --blame-hang --blame-hang-timeout 5m
# Mỗi helper EXE chạy tuần tự qua Start-Process -WindowStyle Hidden,
# WaitForExit(30000), Stop-Process nếu hết thời gian.
<VisualStyleHost-net48.exe> --scenario tabs-hover artifacts/verification/tabs/tab02-0bbd82e-net48
<VisualStyleHost-net8.0-windows.exe> --scenario tabs-hover artifacts/verification/tabs/tab02-0bbd82e-net8
```

Trước sửa, hai regressions xác định RED trên net8: custom headers với inherited `HotTrack=true` còn native style bit `TCS_HOTTRACK=64`; `HotTrack=false` vẫn để MouseMove xếp repaint vùng header. Sau sửa, focused NUnit discovery/execution **29/29 PASS, fail 0, skip 0** trên mỗi TFM. Existing BootstrapTabControl tests giữ 21/21 PASS; reflection setup `_hoveredIndex` đã đổi thành WM_MOUSEMOVE thật. Bộ mới kiểm same-header không xếp repaint lần nữa, dirty union đúng hai header khi đổi hover, bất biến `Font`/`ItemSize`/`GetTabRect`/selection/HWND, và con trỏ đứng yên qua caption/layout mutation, resize, add/remove page, handle recreation. A/B `GetUpdateRect(..., erase=true)` ghi `WM_ERASEBKGND=1` với invalidation có erase và `0` với header invalidation mới trên cả net48/net8. Đây là so sánh erase message trong fixture, chưa phải Owner visual no-flicker decision.

Helper `tabs-hover` trên candidate này: net48 exit 0 trong 264 ms; net8 exit 0 trong 311 ms. Metadata mỗi TFM ghi `PROBE PASS`, `fullGate=OPEN`, `flicker=NOT REPRODUCED`; raw capture/trace/JSON chỉ ở local ignored artifacts, chưa có URL bền vững. Native tooltip/focus/overflow sau TAB-02 chưa được một oracle riêng kết luận; gate TAB-01 cho final compositor, physical 125% và Owner review vẫn mở. Protected `CreateParams` override là delta cần đưa vào API fingerprint/compatibility ở TAB-06.

## Kiểm tra tích hợp TAB-01–03 trong worktree

Candidate source `b374cf7adf27fd5fed7ef4315a38ca93b3eefe25` cộng diff tài liệu contract/plan chưa commit tại lúc chạy. Windows NT 10.0.26300.0, SDK `8.0.427`, process x64, Release; helper ghi native DPI 96. Restore dùng cache hiện có. Lệnh `./build.ps1 -Configuration Release -Scope Controls` (PowerShell: `.\build.ps1`) build 2/2 TFM, 0 warning, 0 error. Discovery với filter `FullyQualifiedName~BootstrapTab` thấy 48 cases mỗi TFM. Hai lệnh `dotnet test ... -c Release -f net48|net8.0-windows --no-build --filter "FullyQualifiedName~BootstrapTab" --blame-hang --blame-hang-timeout 5m` chạy tuần tự: net48 48/48 PASS, net8 48/48 PASS, fail 0, skip 0. Helper `tabs-hover` của mỗi EXE Release chạy tuần tự qua `Start-Process -WindowStyle Hidden`, `WaitForExit(30000)`; exit 0/0, metadata candidate đúng SHA trên, `PROBE PASS`, `fullGate=OPEN`, `flicker=NOT REPRODUCED`. Raw artifacts local bị ignore, không commit.

API discovery/execution `Phase16PublicApiBaselineTests` trên mỗi TFM thấy 15 cases: 14 PASS, 1 FAIL, skip 0. `ExportedApiMatchesApprovedV1Baseline` báo actual fingerprint giống nhau `4be0c646c1d836a9c679529f7b5c3d8189987651b70aa519e0288eb4297d148d` so với approved `1de84ca1b9a297e0caff576b4b18c0296ca17d0ebabf088be2f48213cad27755`. Protected `CreateParams` của TAB-02 là exported delta đã biết; review surface và chốt fingerprint trên candidate kết hợp BootstrapTabs/BUS thuộc TAB-06, chưa cập nhật gate ở TAB-01–03. Build graph All, physical 125%, native tooltip/focus oracle, Owner visual, artifact URL và consumer migration: **NOT RUN/PENDING** trong lượt tích hợp này. TAB-01 gate vẫn OPEN.
