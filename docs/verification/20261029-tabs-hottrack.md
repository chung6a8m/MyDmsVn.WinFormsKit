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

## TAB-01 addendum: actual hover/update region

Ngày 29/10/2026, helper được bổ sung subclass `NativeWindow` tại helper host để lấy `WM_MOUSEMOVE` nhận thực, `TCM_HITTEST` tại `LParam`, state hover managed của `BootstrapTabControl` và `GetUpdateRect` **ngay sau** native/managed dispatch nhưng trước pump paint tiếp theo. Pointer được di chuyển bằng `Cursor.Position`, chờ hữu hạn 25 ms mỗi vị trí để Windows phát message, rồi pump. `WindowFromPoint` xác nhận target HWND ở vị trí thử. Probe chưa can thiệp production.

Candidate lần chạy cuối: HEAD `862d112b42026a114cc33af2d086686d3979b2e6` cộng thay đổi helper chưa commit, SHA-256 `TabsHoverScenario.cs` là `55E2B4AE370AE51772697038986092AEF35D696E71AA8ECF27B3394B2978B5D3`. TAB-02/TAB-03 đang sửa source đồng thời trong worktree; vì vậy đây là trace phát triển, **chưa phải candidate nghiệm thu đã chốt**. `dotnet build ...VisualStyleHost.csproj -c Release`: hai TFM PASS, 0 warning/error. Chạy hai EXE tuần tự, timeout 30 giây: net48 exit 0 trong 697 ms; net8 exit 0 trong 679 ms. Helper scenario discovered/executed 1/1 mỗi TFM, probe pass 1, fail 0, skip 0. Actual OS/bitness/DPI giữ như trên: Windows NT 10.0.26300.0, 64 bit, window DPI 96. Raw trace/PNG/JSON local không commit.

| TFM | WM_MOUSEMOVE nhận: native/hot/cold/paint probe | Managed/native hover khớp hot/cold | `GetUpdateRect` có vùng: native/hot/cold |
|---|---:|---:|---:|
| net48 | 3/3/3/3 | 3/3, 3/3 | 3/2/0 |
| net8.0-windows | 3/3/3/3 | 3/3, 3/3 | 3/2/0 |

Ví dụ net8 khi pointer chuyển vào header Bootstrap index 1: `point={X=191,Y=18}`, native/managed hover đều `1`, dirty `{181,2,360,34}`. Khi trở về index 0, hover đều `0`, dirty vùng header cũ. HotTrack false vẫn nhận native `WM_MOUSEMOVE` và cập nhật hovered state nhưng không tạo update region trong ba chuyển động thử. Native TabControl có dirty region cả ba lần. Đây là vùng update quan sát được; không khóa số lượng `WM_PAINT` vì Windows có thể coalesce.

Một lần net48 trước đó trùng GUI run của TAB-02 nên **loại khỏi evidence**. Lần net8 kế tiếp exit 2 vì target index 2 của Bootstrap nằm dưới overflow child, chỉ có 2/3 `WM_MOUSEMOVE` tới tab; đây là lỗi chọn target của helper, không suy ra defect control. Helper đổi sequence Bootstrap thành header nhìn thấy `0→1→0`; hai lần chạy cuối có lane GUI riêng và kết quả ở bảng trên. Full native/Owner gate tiếp tục **OPEN**.

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

## Kiểm tra tích hợp sau review TAB-01–03

Candidate source `18cdc1dfc34f6c9c0ba23d9831c6555110e65235` cộng diff tài liệu contract/plan chưa commit tại lúc chạy. `.\build.ps1 -Configuration Release -Scope Controls` PASS cho `net48` và `net8.0-windows`, 0 warning/error. Sau build, hai lệnh `dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f <TFM> --no-build --filter "FullyQualifiedName~BootstrapTab" --blame-hang --blame-hang-timeout 5m` chạy tuần tự: mỗi TFM discovered/executed 54, PASS 54, FAIL 0, SKIP 0. Hai VisualStyleHost Release EXE chạy tuần tự bằng `Start-Process -WindowStyle Hidden`, `WaitForExit(30000)`: exit 0/0, metadata đúng source SHA trên; net48 runtime `4.0.30319.42000`, net8 runtime `8.0.33`, Windows NT 10.0.26300.0, x64, native DPI 96. Cả hai vẫn ghi `fullGate=OPEN`, `flicker=NOT REPRODUCED`.

API baseline được chạy lại tuần tự với `FullyQualifiedName~Phase16PublicApiBaselineTests` trên đúng source candidate sau review: mỗi TFM 15 discovered/executed, 14 PASS, 1 FAIL (`ExportedApiMatchesApprovedV1Baseline`), 0 SKIP; actual fingerprint vẫn là `4be0c646c1d836a9c679529f7b5c3d8189987651b70aa519e0288eb4297d148d`. Đây là gate chưa chốt vì protected `CreateParams` delta thuộc review TAB-06, không phải PASS. Raw artifacts lưu local ignored, không có URL bền vững. Native compositor, print flags/nesting, physical 125%, Owner visual và consumer acceptance còn PENDING; không đánh dấu TAB-01–03 DONE theo plan.

## TAB-02 bổ sung — overflow scroll và test đi qua facade

Ngày chạy: 29/10/2026. Candidate lúc chạy: `862d112b42026a114cc33af2d086686d3979b2e6` cộng diff TAB-02/TAB-03 trong worktree; các source files TAB-02 sau sửa có SHA-256 `BootstrapTabControl.cs=60AECE43B354C479547AA322C37FC6D61AB7D0C8C4240D71D2AC034ECB0CF5FB`, `BootstrapTabInteractionController.cs=34BEE63F0C2BD103062BD3E81BA7FF831D7686BC65983AB85A76D317CF16144E`. SourceGrid gitlink không đổi. Windows NT 10.0.26300.0, SDK `8.0.427`, x64, Release, native DPI 96; restore warm.

Native `msctls_updown32` child nhận `WM_LBUTTONDOWN/UP` tại HWND thật trong fixture, trong khi desktop pointer giữ nguyên trên header. Trace parent ghi `WM_NOTIFY` code `-722`, tiếp đến `WM_HSCROLL` từ child; `base.WndProc` của `WM_HSCROLL` đổi `GetTabRect(0).X` từ `2` sang `-63` (`net48`) hoặc `-56` (`net8`). Không pump thêm message sau click, hover cũ còn index `1`, hình học mới đặt pointer ở index `2`: regression **RED** trên net8 trước sửa. Hook mới chỉ nhận `WM_HSCROLL` từ đúng overflow child và resync pointer sau native dispatch; regression **GREEN** trên hai TFM, hover index `2`. Dirty-region, same-header và no-erase tests trước đó đã đổi để gửi `WM_MOUSEMOVE` qua facade sở hữu controller, không tạo controller thứ hai. Đây là native child message dispatch có pointer giữ nguyên, chưa phải physical click/capture được Owner duyệt.

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapTabInteractionTests" --blame-hang --blame-hang-timeout 5m --logger "console;verbosity=normal"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapTabInteractionTests" --blame-hang --blame-hang-timeout 5m --logger "console;verbosity=normal"
# VisualStyleHost tabs-hover: từng EXE Release chạy tuần tự qua Start-Process -WindowStyle Hidden, WaitForExit(30000).
```

Sau khi GUI lane được cấp độc quyền, discovery/execution **9/9 PASS, fail 0, skip 0** trên `net48` và **9/9 PASS, fail 0, skip 0** trên `net8.0-windows`; helper `tabs-hover` chạy tuần tự exit 0/0, metadata `PROBE PASS`, `fullGate=OPEN`, `flicker=NOT REPRODUCED` (elapsed 703/675 ms). Một lần chạy net8 trước đó có khả năng trùng helper của agent khác và bị **loại khỏi evidence**. Broad `FullyQualifiedName~BootstrapTab` trên net48 trước khi TAB-03 sửa close stale target: 53 PASS, 1 FAIL (`RemovingPageClearsStaleCloseMenuTarget`), skip 0; lỗi này thuộc TAB-03 và đã chuyển cho owner task, không diễn giải focused TAB-02 thành full-suite PASS. Raw artifacts chỉ ở ignored local folder. Physical 125%, visual/Owner decision, artifact URL và final compositor gate vẫn **PENDING**.

## TAB-03 bổ sung — stale close target và tổ hợp caption

Candidate lúc chạy: `998509f` cộng diff TAB-03 ở `BootstrapTabCloseController.cs` và `BootstrapTabPaintCompatibilityTests.cs`; Windows NT 10.0.26300.0, SDK `8.0.427`, Release, x64, native DPI 96, warm restore. Regression `RemovingPageClearsStaleCloseMenuTarget` phát hiện RED trong broad run net48: menu còn `AccessibleName="Close tab Xóa ngay"` sau khi page đã bị remove, vì `ControlRemoved` có thể phát trước khi `SelectedTab` thôi trỏ tới page đó. Controller giờ loại page đang xóa khỏi lựa chọn menu và chọn page còn sở hữu làm target hiển thị, không đổi native selection hoặc dispose page. Test gọi lệnh menu sau removal và kiểm không phát close request cũ.

`CaptionLayoutKeepsTextAndImageInsideAvailableContent` bổ sung bốn tổ hợp caption tiếng Việt/ngắn/rỗng/dài, icon/no-icon, closable/nonclosable, Tabs/Pills/Underline và Primary/Success/Danger/Info. Oracle kiểm text/image nằm trong content đã trừ close, cùng accent của variant; đây là geometry/palette regression, không thay physical focus proof.

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapTab" --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapTab" --blame-hang --blame-hang-timeout 5m
# Bounded VisualStyleHost tabs-hover EXE: net48, rồi net8.0-windows; WaitForExit(30000).
```

Final GUI lane độc quyền: discovered/executed `net48` **54/54 PASS**, fail 0, skip 0; sau đó `net8.0-windows` **54/54 PASS**, fail 0, skip 0. Helper `tabs-hover` sau test chạy tuần tự, exit 0/0. Các lượt chồng GUI trước đó bị loại khỏi evidence. `fullGate=OPEN`, physical 125%, focus clearance bằng capture màn hình thật, artifact URL và Owner visual review vẫn **PENDING**; không chuyển thành PASS từ bitmap/layout tests.

## PR #4 — sửa hai findings P2 về overflow hover và glyph italic

Review trên head `4316a293576f053ad42e9176f39fae821107c6a1` ghi [hai inline findings](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/pull/4). Candidate source sau sửa: `765949ac758172a89fe486ed6f5b5b576c65462b`; SourceGrid gitlink không đổi. Windows NT 10.0.26300.0, SDK `8.0.427`, Release, process x64, native DPI 96; restore warm. Không chạy GUI tests đồng thời.

- Overflow: regression đặt desktop pointer thật trên HWND `msctls_updown32`, click arrow để native scroll và kiểm hover ngay sau `WM_HSCROLL`, rồi vào một header nhìn thấy và rời control qua native MouseMove/MouseLeave. Trước sửa, .NET 8 trả hovered index `6` khi pointer ở trên child, kỳ vọng `-1` (**RED**, 1/1 fail). Sau sửa, `RefreshFromPointer` loại overflow child/foreign HWND nhưng vẫn cho descendant transient của owner trong page mutation. Test và focused interaction suite **10/10 PASS** trên mỗi TFM; fail 0, skip 0.
- Italic: regression vẽ bằng `BootstrapTabControl.OnDrawItem` thật với caller font Times New Roman 16 italic, caption `f`, so mực với `TextRenderer` tham chiếu trong content bounds rộng. Trước sửa, .NET 8 có 60 pixel mực so với 90 ở tham chiếu (**RED**); .NET Framework 4.8 đo 19px và không tái hiện defect. Bỏ `NoPadding` ở cả phép đo và vẽ để giữ glyph-overhang padding; sau sửa hai TFM đều có 90/90 pixel và focused paint tests **11/11 PASS** mỗi TFM. Test 125% dùng metrics DPI 120 để kiểm khoảng cách text/focus, **không phải physical 125% evidence**.

Trên source commit trên, `.\build.ps1 -Configuration Release -Scope Controls` build 2/2 TFM, 0 warning/error. Hai lệnh sau chạy tuần tự sau build:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --no-build --filter "FullyQualifiedName~BootstrapTab" --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --no-build --filter "FullyQualifiedName~BootstrapTab" --blame-hang --blame-hang-timeout 5m
```

Discovery/execution: net48 **57/57 PASS**, net8 **57/57 PASS**, fail 0, skip 0 trên từng TFM. Bounded VisualStyleHost `--scenario tabs-hover` chạy tuần tự từ Release EXE bằng `Start-Process -WindowStyle Hidden`, `WaitForExit(30000)`: exit 0/0, metadata candidate đúng `765949ac758172a89fe486ed6f5b5b576c65462b`, elapsed 704/700ms, `PROBE PASS`, `fullGate=OPEN`, `flicker=NOT REPRODUCED`. Raw output ở ignored `artifacts/verification/tabs/pr4-review-*`, không commit. API baseline không chạy lại trong scope hai findings vì exported surface không đổi; mismatch TAB-06 đã ghi ở lượt trước còn mở. Full compositor/print, physical DPI/Owner visual và artifact URL vẫn **PENDING**.

## TAB-04/05 — BootstrapTabs native composition và comparison gallery

Ngày chạy 29/10/2026. Candidate code và gallery đã commit tại `d67abbd7b1f2b3081e26e8b0a22bbb03761bbaf0` (TAB-04 `a61446d`, TAB-05 `d67abbd`); diff tài liệu này chưa nằm trong candidate lúc chạy. SourceGrid gitlink `f4e457b43582bf01892f50bdc74aa480531e5944` được khởi tạo đúng pin qua `git submodule update --init --recursive`, không dùng `--remote`. Windows NT 10.0.26300.0, SDK `8.0.427`, x64, helper `DeviceDpi=nativeDpi=96`; runtime net48 `4.0.30319.42000`, net8 `8.0.33`. Restore warm. Mọi native/GUI process chạy tuần tự trong một GUI lane.

Test-first `BootstrapTabs_HasDirectNativeBaseTypeAndExactDefaults` ban đầu RED vì type chưa tồn tại. Sau triển khai, `BootstrapTabs : TabControl` trực tiếp, defaults mới, reuse hover/painter/close, native supplied-HDC screen buffer/print dispatch, compact chrome snapshot và fallback. `Navigation / BootstrapTabs` có so sánh cùng kích thước, HotTrack toggles, selectors, page actions, icon/tooltip/input và overflow. Review độc lập phát hiện buffer chưa prefill dirty pixels, RTL image/close overlap và `SetTabClosable` thiếu hover resync; cả ba đã sửa trước candidate này. Image sizing của control mới chuyển ra trước paint dispatch; tests kiểm `ItemSize` không đổi trong `DrawItem`. Screen regression dùng native HDC của selected `TabPage` sau full dirty `InvalidateRect/UpdateWindow`; screen-copy RGB tuyệt đối từng bị nhiễu desktop nên không dùng làm acceptance oracle.

Các lệnh đã chạy trên candidate code (filter được discovery riêng trên từng TFM):

```powershell
.\build.ps1 -Configuration Release -Scope Controls
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --no-build --filter "FullyQualifiedName~BootstrapTab|FullyQualifiedName~NavigationDemoFormTests|FullyQualifiedName~IntegratedDemoApplicationTests" --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --no-build --filter "FullyQualifiedName~BootstrapTab|FullyQualifiedName~NavigationDemoFormTests|FullyQualifiedName~IntegratedDemoApplicationTests" --blame-hang --blame-hang-timeout 5m
.\build.ps1 -Configuration Release -Scope All
# VisualStyleHost Release EXE --scenario tabs-hover; net48 rồi net8.0-windows, Start-Process -WindowStyle Hidden -Wait.
```

Controls build và All graph build PASS hai TFM, 0 warning/error. Filter discovery/execution **93/93 PASS, fail 0, skip 0** mỗi TFM; riêng `BootstrapTabs*` **17/17 PASS** mỗi TFM sau sửa review. Helper trên candidate commit chạy exit 0/0, `PROBE PASS` hai TFM, `fullGate=OPEN`, `flicker=NOT REPRODUCED`; elapsed 1080/1088 ms. Production proof trên net48/net8 lần lượt có `screenPaint=4/4`, `screenDraw=14/14`, `hoverTargetHits=3/3`, `printClient=2/2`, `printDraw=10/10`, `printPixels=32560/32416`, `printOutside=0/0`, `drawToBitmapPixels=140250/140250`, `outsideDraw=0/0`; native overflow child hiện diện và vẫn click/scroll được. Prototype supplied-HDC dirty composition thay đổi 1876/1836 header pixels, còn selected/page/child/outside đều 0. `WM_PRINT` flags 0 không vẽ; flags 4/28 lồng đúng một `WM_PRINTCLIENT` và không recursion. Raw traces/PNG chỉ ở ignored `artifacts/verification/tabs/tab04-commit-net48` và `tab04-commit-net8`, không commit, chưa có artifact URL bền vững.

Giới hạn: Light/Dark visual review trên gallery, physical 125%/150%/200%, mixed-DPI, focus clearance bằng capture màn hình thật, artifact URL và Owner decision **NOT RUN/PENDING**; không lấy synthetic metrics hay bitmap làm bằng chứng thay thế. Full no-flicker acceptance và TAB-01 native gate tiếp tục **OPEN** dù proof của candidate tại DPI96 PASS. API fingerprint và consumer migration thuộc TAB-06 chưa chốt; baseline mismatch đã ghi ở lượt trước vẫn mở. Không publish/merge.

## PR #4 — sửa bốn findings P2 của review TAB-04/05

Review trên head `bf3bf35a7228353dd654db17557058b203b7955b` nêu bốn lỗi: Fill còn dùng số page cũ trong `ControlRemoved`, `WM_PRINT` flags `PRF_CLIENT | PRF_CHECKVISIBLE` vẫn hoàn thiện chrome khi HWND ẩn, caption dọc Left đè icon và icon fallback LTR đè close ở header hẹp. Candidate source sau sửa `7cfc374643137dc5a633f2c6126a73348b3c32d3`; SourceGrid gitlink giữ `f4e457b43582bf01892f50bdc74aa480531e5944`. Windows NT 10.0.26300.0, SDK `8.0.427`, x64, Release, helper native DPI 96; restore warm. Không chạy native tests đồng thời.

- Fill: regression xóa tab cuối không chọn trong `Fill=true`, client 600px, 3→2 trang: trước sửa net8 vẫn `ItemSize.Width=200`, kỳ vọng `300` (**RED**); sau sửa 1/1 PASS trên net48 và net8. Callback sizing và image-size cache loại page đang xóa nếu native collection còn giữ nó.
- `PRF_CHECKVISIBLE`: test gửi `WM_PRINT` flags 5 tới cùng HWND sau `Hide()`, caller bitmap có sentinel. Trước sửa 12.632 pixel bị đổi trên mỗi TFM (**RED**); sau sửa 1/1 PASS mỗi TFM, target giữ nguyên. Positive control visible vẫn vẽ.
- Fallback Left: caption dài với icon đổi 27 pixel ở vùng icon trước sửa (**RED**); sau sửa 0 pixel, focused test PASS hai TFM. Khoảng text được chuyển sang trục sau xoay và clip trong content thực.
- Fallback LTR: ảnh lớn trên header hẹp làm đổi 235 pixel trong close target trước sửa ở cả enabled/disabled (**RED**); sau sửa 0 pixel, hai tests PASS trên mỗi TFM. Icon được clip khỏi close region; close hit-test không đổi.

Sau sửa, các lệnh chính chạy tuần tự:

```powershell
.\build.ps1 -Configuration Release -Scope All
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --no-build --filter "FullyQualifiedName~BootstrapTab|FullyQualifiedName~NavigationDemoFormTests|FullyQualifiedName~IntegratedDemoApplicationTests" --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --no-build --filter "FullyQualifiedName~BootstrapTab|FullyQualifiedName~NavigationDemoFormTests|FullyQualifiedName~IntegratedDemoApplicationTests" --blame-hang --blame-hang-timeout 5m
# Release VisualStyleHost EXE --scenario tabs-hover: net48 rồi net8.0-windows, Start-Process -WindowStyle Hidden -Wait.
```

Build PASS hai TFM, 0 warning/error. Discovery/execution **98/98 PASS**, fail 0, skip 0 trên net48 và **98/98 PASS**, fail 0, skip 0 trên net8. Helper sau commit exit 0/0, metadata đúng SHA candidate, `PROBE PASS`, `productionProof=True`, `compositionProof=True`, `fullGate=OPEN`, `flicker=NOT REPRODUCED`; elapsed 1034/1092 ms. Raw trace/PNG ở ignored `artifacts/verification/tabs/pr4-review3-commit-net48` và `pr4-review3-commit-net8`, không commit. Physical DPI/Owner/artifact URL và API TAB-06 tiếp tục PENDING; không suy nghiệm thu no-flicker từ DPI96.

## PR #4 — sửa finding RTL disabled icon/close của review vòng 4

Review trên head `e57a010f8f36e7bb7d4d2f9fad8921aa7301f756` chỉ ra `ControlPaint.DrawImageDisabled` vẽ theo kích thước ảnh gốc dù `imageBounds` RTL đã thu hẹp. Candidate source sau sửa `c3e5a6aa551c177c5f87d5104d64d04fe5d3c253`; Windows NT 10.0.26300.0, SDK `8.0.427`, x64, Release, native DPI 96, restore warm, SourceGrid gitlink không đổi. Regression narrow header 60×32, icon 48×24, close ở mép phải được mở rộng thành LTR/RTL × enabled/disabled. Trước sửa, riêng RTL disabled làm đổi **148 pixel** trong close target trên cả net48/net8 (**RED**, 1 fail/3 pass mỗi TFM). Painter giờ clip thao tác vẽ vào `imageBounds` và vùng trước close cho cả hai hướng. Sau sửa, bốn case **4/4 PASS** mỗi TFM; icon vẫn còn pixel hiển thị và close target khớp reference không có icon.

Các lệnh sau chạy tuần tự trên source candidate:

```powershell
.\build.ps1 -Configuration Release -Scope All
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --no-build --filter "FullyQualifiedName~BootstrapTab|FullyQualifiedName~NavigationDemoFormTests|FullyQualifiedName~IntegratedDemoApplicationTests" --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --no-build --filter "FullyQualifiedName~BootstrapTab|FullyQualifiedName~NavigationDemoFormTests|FullyQualifiedName~IntegratedDemoApplicationTests" --blame-hang --blame-hang-timeout 5m
# Release VisualStyleHost EXE --scenario tabs-hover: net48 rồi net8.0-windows, Start-Process -WindowStyle Hidden -Wait.
```

Build PASS hai TFM, 0 warning/error; discovery/execution **100/100 PASS**, fail 0, skip 0 trên mỗi TFM. Helper sau commit exit 0/0, metadata đúng SHA candidate, `PROBE PASS`, `productionProof=True`, `compositionProof=True`, `fullGate=OPEN`, `flicker=NOT REPRODUCED`; elapsed 1053/1085 ms. Raw trace/PNG ở ignored `artifacts/verification/tabs/pr4-review4-commit-net48` và `pr4-review4-commit-net8`, không commit. Physical DPI/Owner/artifact URL và API TAB-06 vẫn PENDING; không coi DPI96 là nghiệm thu full no-flicker.

## TAB-06 — contract, API baseline và native matrix

Ngày chạy 29/10/2026. Candidate `e780da5` (parent code `5b012e276ac5e7b12e150751f2c2fc95e9bdc563`); SourceGrid gitlink `f4e457b43582bf01892f50bdc74aa480531e5944`. Windows NT 10.0.26300.0, SDK `8.0.427`, Release, helper process 64 bit; net48 CLR `4.0.30319.42000`, net8 runtime `8.0.33`. Một monitor `\\.\DISPLAY1`, system DPI 96; `Form.DeviceDpi`, `BootstrapTabs.DeviceDpi` và `GetDpiForWindow` của cả hai HWND đều 96 ở bốn ô native. Restore warm; không xác nhận clean restore.

Fingerprint gate ban đầu **RED** trên assembly net48/net8: expected `1de84ca1b9a297e0caff576b4b18c0296ca17d0ebabf088be2f48213cad27755`, actual chung `1d1172af4dc1a247b330ca3df84d00bf0903ab22470fa9c59581b1543002f3c1` (1/1 fail mỗi TFM). Đối chiếu `git diff` từ baseline `8e7f1854d85d80fbaecc3d9d9167bb8ebcd94921` và reflection surface: exported delta gồm protected `BootstrapTabControl.CreateParams` và type mới `BootstrapTabs : TabControl` với constructor, bốn appearance properties, close event/ba methods, protected `CreateParams`, `Dispose`, `GetTabCloseButtonBounds`, `ProcessCmdKey`, `WndProc`. Không có exported internal helper, removal hoặc signature change của type cũ khác; `AssemblyVersion=1.0.0.0`. Exact contract test khóa cả hai facades. Fingerprint được cập nhật sau review, không nới gate.

Lệnh đã chạy tuần tự (helper dùng `Start-Process -WindowStyle Hidden`, `WaitForExit(30000)` và dừng process nếu timeout):

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~Phase16PublicApiBaselineTests" --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests" --blame-hang --blame-hang-timeout 5m
.\build.ps1 -Configuration Release -Scope All
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --no-build --filter "FullyQualifiedName~BootstrapTab|FullyQualifiedName~Phase16PublicApiBaselineTests|FullyQualifiedName~NavigationDemoFormTests|FullyQualifiedName~IntegratedDemo" --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --no-build --filter "FullyQualifiedName~BootstrapTab|FullyQualifiedName~Phase16PublicApiBaselineTests|FullyQualifiedName~NavigationDemoFormTests|FullyQualifiedName~IntegratedDemo" --blame-hang --blame-hang-timeout 5m
# Sau build All cuối, VisualStyleHost Release EXE --scenario tabs-matrix: net48 rồi net8.0-windows.
```

`Phase16PublicApiBaselineTests`: discovered/executed **16/16 PASS** mỗi TFM, fail 0, skip 0. Focused Controls/gallery filter: discovered/executed **153/153 PASS** mỗi TFM, fail 0, skip 0. Build `All` PASS hai TFM, 0 warning/error, gồm adapter/SourceGrid và hai gallery compile. Không chạy SQL/full suites ngoài boundary; BUS chưa đổi manifest/DPI gallery. Native helper bounded final: 2 process exit 0/0, discovered/executed 2 theme cases mỗi TFM, tổng **4/4 PASS**, fail 0, skip 0. Mỗi case có hover `0→1→0` nhận 3/3 native targets, HWND/font/item bounds/selection giữ nguyên; close pointer phát 1 request `Pointer`, còn 10 page và selection không đổi; overflow child `msctls_updown32` nhận click thật, `GetTabRect(0).X` đổi `2→-133`; `WM_PRINTCLIENT` đổi 129 sampled header pixels; fallback `Bottom+RTL+Buttons+Multiline` và roundtrip giữ page/selection. Lượt probe đầu net48 báo FAIL giả do lấy mẫu tab 0 sau khi overflow đã cuộn nó khỏi vùng nhìn thấy; đã sửa probe sang header còn hiển thị và chạy lại cả hai runtime. Chỉ lượt cuối dùng làm evidence.

Helper source SHA-256 `4E3CC0717F577D0007022AF5765E8AAB34BE3C28A8549CB251D9F1A38EE4CC73`; local trace SHA-256 net48 `505F1D69F20D25A3096B9899B71CD3FD4FD7D248D945450ABF773F8320A5BD36`, net8 `4B9D5E47F5F7A708731176CFAC978603DBA8EF740357BEB0B031981AC3651B9D`. Trace và Light/Dark hover/fallback/print PNG chỉ ở `%TEMP%\tab06-matrix-final-net48` và `%TEMP%\tab06-matrix-final-net8`; raw artifacts không commit. **Artifact URL và retention ngoài máy: PENDING**; local path không phải evidence lâu dài.

Physical 125% trên hai TFM, representative 150%/200%, mixed-DPI, Owner visual review chuỗi hover và ảnh Light/Dark trên candidate này: **NOT RUN/PENDING** vì máy chỉ có monitor DPI96 và chưa có Owner decision. Native DPI96 cùng bitmap/exit 0 không chứng minh no-flicker ở môi trường chưa chạy; TAB-01 full native/Owner gate và TAB-06 acceptance tiếp tục **OPEN**. Không triển khai BUS, không publish/merge.

## TAB-06 addendum — physical 125% trên DISPLAY1

Ngày 29/10/2026, candidate `f2192fbf85fdd22d826b3767db3d86400a768f60` (không có source diff). Windows báo `GetScaleFactorForMonitor=125` trên monitor duy nhất `\\.\DISPLAY1`; PowerShell process đang dùng `GetDpiForSystem=96` nên không lấy số đó làm DPI của helper. VisualStyleHost mở HWND mới trên cả hai TFM: Light/Dark đều có `Form.DeviceDpi=120`, `BootstrapTabs.DeviceDpi=120`, `GetDpiForWindow(form)=120`, `GetDpiForWindow(tabs)=120`. Helper net48 log systemDpi 96 theo process context, net8 log 120; actual HWND DPI và physical monitor scale khớp 125%. OS `10.0.26300.0`, SDK `8.0.427`, process 64 bit, CLR net48 `4.0.30319.42000`, runtime net8 `8.0.33`, Release build từ candidate trên.

Chạy tuần tự VisualStyleHost Release `--scenario tabs-matrix artifacts/verification/tabs/tab06-physical125-net48`, rồi `net8`; mỗi subprocess dùng `Start-Process -WindowStyle Hidden`, `WaitForExit(30000)` và timeout kill. Cả hai exit 0; Light/Dark trên mỗi TFM: discovered/executed **2/2 PASS**, tổng **4/4 PASS**, fail 0, skip 0. Mỗi case hover `0→1→0` nhận 3/3 target HWND, không đổi font/ItemSize/selection/HWND; pointer close phát đúng một `Pointer` request, không đổi selection; native overflow arrow nhận click và `GetTabRect(0).X` đổi `2→-168`; `WM_PRINTCLIENT` đổi 165 sampled header pixels; fallback `Bottom+RTL+Buttons+Multiline` và compact roundtrip giữ collection/selection. Ảnh Light/Dark hover/fallback/print đã được tạo trong ignored local artifacts; kiểm tra ảnh Dark bằng mắt trong lượt này chỉ xác nhận helper đã render, không phải Owner approval.

Chạy tiếp VisualStyleHost Release `--scenario tabs-hover` tuần tự, net48 rồi net8, cùng timeout 30 giây: **2/2 PROBE PASS**, fail 0, skip 0, elapsed 1071/1087 ms. Metadata cả hai ghi đúng candidate, `fullGate=OPEN`, `flicker=NOT REPRODUCED`; trace có `DeviceDpi=nativeDpi=120`. Production proof net48/net8 lần lượt: `screenPaint=4/4`, `hoverTargetHits=3/3`, `printClient=2/2`, `printOutside=0/0`, `outsideDraw=0/0`, `compositionProof=True`, `productionProof=True`; `WM_PRINT` flags 0 không dispatch client draw, flags 4/28 dispatch một `WM_PRINTCLIENT`, native overflow child click đổi tab rect. Chạy lại focused filter trong section TAB-06 với `--no-build --blame-hang --blame-hang-timeout 5m` theo thứ tự net48 rồi net8: discovered/executed **153/153 PASS** mỗi TFM, fail 0, skip 0. Không rebuild vì source/dependencies không đổi từ candidate đã build và CI build PASS.

SHA-256 local trace: matrix net48 `2119D1FCB38D7D6E74008D5DB24DF020EC28AA143FBDA45397AB5D586033BDFB`, matrix net8 `F8B36E96FDA65AE07829E4B017A4D23A817ABEFD7CA763586D099F8B825B09C0`; hover net48 `B3AF6E83AB24F0971287017D003A492D4ADC5747259DD7C5D19BFF515E740011`, hover net8 `239A7BD1FE06F014A15A44285F322F7E5CAA61441AC3F24D1D8AE44FF55FFA3C`. Raw PNG/trace/JSON nằm trong `artifacts/verification/tabs/tab06-physical125-*` (Git ignore), chưa có artifact URL/retention lâu dài. Physical 100% và 125% matrix hiện đã có trên hai TFM; representative 150%/200%, mixed-DPI, visual/flicker decision của Owner và artifact URL vẫn **NOT RUN/PENDING**. TAB-06 acceptance và TAB-01 full gate tiếp tục **OPEN**; không suy no-flicker toàn diện từ probe hoặc ảnh cuối.

## TAB-06 addendum — kiểm tra khả năng chạy mixed-DPI

Ngày 29/10/2026, candidate `bd97c080b42e9015999afd1481eaac2ef6dafac1`, working tree sạch lúc kiểm. `Screen.AllScreens` chỉ trả `\\.\DISPLAY1` (bounds logical 1536×864); `GetScaleFactorForMonitor=125`. `Get-PnpDevice -Class Monitor -PresentOnly` chỉ có `Integrated Monitor` trạng thái OK; Intel Iris Xe và NVIDIA RTX 3050 Laptop GPU là display adapters, không tạo thêm monitor đang hoạt động. Kiểm lại sau khi hỏi điều kiện phần cứng: không có màn hình thứ hai.

**Mixed-DPI move cùng HWND: NOT RUN / BLOCKED BY ENVIRONMENT.** Không có hai monitor đang hoạt động với scale khác nhau nên không thể quan sát `WM_DPICHANGED`, `GetDpiForWindow` trước/sau khi kéo form qua biên màn, item/header bounds, native overflow/focus/close sau chuyển, hoặc việc quay lại monitor gốc. Các lượt physical 100% và 125% ở trên là hai phép chạy độc lập trên một monitor, không thay mixed-DPI transition. Case này cần môi trường hai monitor với scale khác nhau và chạy lại cùng candidate; TAB-06/Owner acceptance vẫn OPEN.

## TAB-06 addendum — artifact URL và bộ ảnh Owner review

Ngày 29/10/2026, upload bộ ảnh gốc trên đúng code/contract candidate `e780da516d04d8c3637215f6bdcbf2f04618bed0`; lượt physical 125% dùng `f2192fbf85fdd22d826b3767db3d86400a768f60` chỉ thêm tài liệu, runtime source không đổi. [Comment PR #4](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/pull/4#issuecomment-6500393196) hiển thị 18 ảnh: Light/Dark hover ở 100%/125% × hai TFM, Light/Dark fallback ở 125% × hai TFM, và `before→after→final` hover ở 125% × hai TFM. [Manifest visual artifact](20261029-tabs-hottrack-visual-artifacts.md) ghi URL riêng, scenario/DPI/candidate và SHA-256 từng ảnh. Raw trace/JSON vẫn local; hash trace và kết quả native ở các section trước.

Upload bằng GitHub CLI `2.103.3` official portable từ release, ZIP SHA-256 kiểm khớp `98D0CFA794721665D7973DD728C78AFF7BD836D58F10F6D7A03568ECCB0A22E0`; PR comment `--attach` cho 18 PNG, không thêm binary vào Git. Sau upload, đọc lại comment và tải từng URL không đăng nhập: **18/18 HTTP 200, SHA-256 tải về khớp file gốc**. URL gắn với GitHub user attachments trên PR công khai; thời hạn lưu trữ do GitHub quản lý, không cam kết vô hạn. Artifact URL gate đạt cho **18 ảnh visual được liệt kê**; trace raw chưa có URL ngoài máy.

Owner visual review trên bộ ảnh/candidate này: **PENDING**. Các frame là ảnh tĩnh nên chỉ đủ để xét diện mạo/bố cục, không đủ kết luận temporal no-flicker; probe vẫn ghi `flicker=NOT REPRODUCED`, `fullGate=OPEN`. 150%/200%, mixed-DPI và Owner no-flicker decision tiếp tục mở theo plan.

Owner trả lời ngày 29/10/2026 trong task: **APPROVED visual ảnh tĩnh** cho đúng 18 ảnh trong [manifest](20261029-tabs-hottrack-visual-artifacts.md) trên code/contract candidate `e780da516d04d8c3637215f6bdcbf2f04618bed0` (physical 125% ở commit tài liệu `f2192fbf85fdd22d826b3767db3d86400a768f60`, runtime source không đổi). Phạm vi APPROVED: header/fallback Light/Dark ở physical 100%/125% trên net48/net8 và ba frame hover tĩnh ở 125%. Đây là quyết định visual cho ảnh đã xem, không phải GitHub PR approval hay no-flicker theo thời gian. `fullGate=OPEN`; temporal no-flicker, 150%/200% và mixed-DPI tiếp tục PENDING/BLOCKED như trên.

## TAB-06 addendum — temporal no-flicker clip physical 125%

Ngày 29/10/2026, thêm VisualStyleHost `--scenario tabs-temporal <output> <Light|Dark>` ở commit `5dfa85477ae535a30a4d9c0a538575c6ee1ccd0f`; production control không đổi từ `5ec6923070c0370d91c9b1eb5932e0fb5960dd76`. `dotnet build tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.csproj -c Release --no-restore` build **2/2 TFM PASS**, 0 warning/error. Windows NT 10.0.26300.0, x64, DISPLAY1 scale 125%, `DeviceDpi=GetDpiForWindow=120`; runtime net48 `4.0.30319.42000`, net8 `8.0.33`. Không có NUnit discovery trong lượt clip; 4 helper scenarios discovered/executed **4/4 PASS**, fail 0, skip 0. Mỗi run dùng `Start-Process -WindowStyle Hidden`, chờ `ready.txt` tối đa 5 giây, rồi `ffmpeg -f gdigrab -framerate 60 -i title=BootstrapTabs temporal <mode> -t 5.5 -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p` capture cửa sổ hiển thị; helper timeout 30 giây. Mỗi run có 15/15 target HWND hit, 0 miss, HWND/selection/layout giữ nguyên, elapsed 6.5 giây. Clip chỉ dài 5.5 giây và không bao trùm toàn bộ 1 giây idle cuối của helper.

[Manifest 4 clip](20261029-tabs-hottrack-temporal-artifacts.md) ghi URL PR, SHA-256 local/remote, frame count và frame rate thực tế (43–51 fps trung bình; yêu cầu capture 60 fps). GitHub user attachments tải lại 4/4 không đăng nhập, byte hash khớp. Raw trace và clip chỉ ở ignored local artifacts; chỉ clip được upload. Clip là screen capture thật, không nội suy hoặc chỉnh frame. Owner trả lời trong task ngày 29/10/2026: **APPROVED temporal 125%** cho đúng bốn clip trên candidate `5dfa85477ae535a30a4d9c0a538575c6ee1ccd0f`. Quyết định này đóng phần visual temporal physical 125% Light/Dark × hai TFM, không phải PR approval hoặc full TAB-06 acceptance. `fullGate=OPEN`: physical 100% temporal, 150%/200% và mixed-DPI chưa có clip; mixed-DPI vẫn BLOCKED do một monitor.

## TAB-06 addendum — temporal no-flicker clip physical 100%

Ngày 29/10/2026, Windows Settings hiển thị DISPLAY1 scale 100%; helper HWND mới xác nhận `DeviceDpi=GetDpiForWindow=96` trên cả hai TFM. Candidate `9e7af4a3199a0f86682ccb108be52c592283cdb6` chỉ thêm `targetRoot`/foreground vào trace khi pointer miss; production source không đổi từ `5ec6923070c0370d91c9b1eb5932e0fb5960dd76`. `dotnet build tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.csproj -c Release --no-restore`: **2/2 TFM PASS**, 0 warning/error. Windows NT 10.0.26300.0, x64; runtime net48 `4.0.30319.42000`, net8 `8.0.33`. Không có NUnit discovery trong lượt quay; final helper scenarios discovered/executed **4/4 PASS**, fail 0, skip 0. Mỗi run tuần tự qua `Start-Process -WindowStyle Hidden`, `ready.txt` tối đa 5 giây, `ffmpeg gdigrab` capture cửa sổ 5.5 giây ở 60 fps yêu cầu, timeout helper 30 giây. Mỗi run 15/15 target HWND hit, 0 miss; HWND/selection/layout giữ nguyên.

Hai lượt `net8 Light` trước final bị loại khỏi evidence: helper FAIL 0/15 hover và trace diagnostic chỉ ra `targetRoot=foreground=61935676` là cửa sổ Windows Settings, che con trỏ trên màn hình dù FFmpeg vẫn capture cửa sổ TabControl. Sau khi đóng Settings, cùng kịch bản PASS 15/15; bốn clip cuối được quay lại từ đầu trên cùng candidate. [Comment review PR](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/pull/4#issuecomment-6501178439) và [manifest](20261029-tabs-hottrack-temporal-artifacts.md) chứa URL, SHA-256, frames và fps thực tế 50–53 trung bình. Bốn URL tải lại không đăng nhập, byte hash khớp. Raw clip/trace ở ignored local artifacts, không commit; SHA-256 trace tương ứng net48 Light/Dark `F9C956648B6E64B6AE59E1F610CE63D4669AD2AFED360FA391DFAD9054C400D6` / `2DC8333EAF9AEBC28407A37772914B6307968249729249AE74B4C2F3A6B83720`, net8 Light/Dark `64EE4EB309CA73C11654B6CAA49E413DB6E4E477FA944772DD6599E4D1917105` / `9B2DDB97885B8793EC05B39710024CB1B72BD098C307F31740A70F5B945981F5`.

Owner trả lời trong task ngày 29/10/2026: **APPROVED temporal 100%** cho đúng bốn clip physical 100% trên candidate `9e7af4a3199a0f86682ccb108be52c592283cdb6`. Cùng với quyết định 125% riêng ở trên, phần temporal Light/Dark × hai TFM tại physical 100%/125% đã được Owner duyệt. Đây không phải PR approval hoặc full TAB-06 acceptance; `fullGate=OPEN` cho 150%/200% và mixed-DPI (một monitor).

## TAB-06 addendum — Owner chấp thuận ngoại lệ DPI

Ngày 29/10/2026, trên candidate hiện hành `25632b4091b81deff00f5babcb2e43a6085a1634` (production source không đổi từ `5ec6923070c0370d91c9b1eb5932e0fb5960dd76`), Owner trả lời trong task: **“150%/200% và mixed-DPI: Tôi approve là ngoại lệ”**. Phạm vi: bỏ điều kiện phải chạy representative physical 150%/200% và chuyển cùng HWND qua hai monitor khác scale trước khi chốt acceptance TAB của candidate này. Đây là quyết định chấp nhận rủi ro môi trường, không phải bằng chứng native PASS. Kết quả các case giữ nguyên: 150%/200% **NOT RUN**, mixed-DPI **NOT RUN / BLOCKED BY ENVIRONMENT** (chỉ một monitor). Không suy ra `WM_DPICHANGED`, layout, overflow hoặc hover ở các cấu hình chưa thử.

Build, focused/native proof và Owner static/temporal decisions tại physical 100%/125% giữ đúng candidate/phạm vi đã ghi ở trên. Ngoại lệ không áp cho plan BUS, consumer riêng hoặc candidate TAB có thay đổi runtime behavior sau này. Các gate độc lập khác của TAB-01–05 và PR draft được đánh giá riêng; không gọi toàn bộ TAB-01–06 DONE chỉ từ ngoại lệ này.
