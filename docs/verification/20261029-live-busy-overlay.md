# BUS-01 — Proof layered child native

Production BUS-03 có [evidence candidate riêng](20261030-live-busy-overlay-production.md); nội dung bên dưới giữ lịch sử probe BUS-01.

## Candidate sau review Important — deadline toàn scenario

Candidate sau `fa9c5a9d24dcd6d24c486c4010bc24cca3810b62`: source blob `28c7e1d57513f08a12284e80716b8630250cf941`, SHA-256 `9E0283285BBBC91C30D7959BC2538144C27BA43205005441B545880B2B4266A4`. Đã thêm deadline tổng **30000 ms** bằng Stopwatch và UI-thread watchdog timer độc lập với timer tự đóng modal; Check trước/sau các pump và capture. Watchdog vẫn chạy trong nested `ShowDialog`, đóng modal rồi root, sau đó timeout unwind qua finally gỡ filter/hooks và dispose windows. Cleanup kiểm cả Form.IsDisposed lẫn HWND đã ghi bằng IsWindow; timer được stop/dispose. Không fail-fast production. Parent watchdog vẫn **60 giây** cho normal helper và **10 giây** cho injection, bảo vệ trường hợp UI thread/native call bị treo đến mức không thể xử lý timer hoặc Check; helper không thể tự cleanup trên UI thread đã bị khóa.

Proof RED: thêm nhánh test-only `BUS_TIMEOUT_PROOF=1` mở modal không có auto-close khi filter/hooks đã installed, chạy source trước fix trên net8. Helper còn sống sau **6 giây**, parent phải kill; chỉ có `INJECT`, không có cleanup marker. Sau fix, nhánh này đặt deadline tổng **3000 ms** và dùng đúng đường watchdog/cleanup normal. Final candidate đã chạy lại tuần tự trên cùng môi trường Windows/DPI như evidence lịch sử bên dưới:

| Final invocation | Process exit | Cleanup elapsed ms | Kết quả |
|---|---:|---:|---|
| Live net48 x64 | 0 | 18027 | PASS native + cleanup |
| Live net48 x86 | 0 | 15947 | PASS native + cleanup |
| Live net8 x64 | 0 | 13756 | PASS native + cleanup |
| Negative net48 x64 | 0 | 7356 | PASS opaque probe + cleanup |
| Negative net8 x64 | 0 | 7537 | PASS opaque probe + cleanup |
| Timeout injection net48 x64 | 1, dự kiến | 3046 | PASS timeout/cleanup, deadline tick tại 3019 ms |
| Timeout injection net48 x86 | 1, dự kiến | 3056 | PASS timeout/cleanup, deadline tick tại 3023 ms |
| Timeout injection net8 x64 | 1, dự kiến | 3050 | PASS timeout/cleanup, deadline tick tại 3019 ms |
| Default helper net48 / net8 | 0 / 0 | Không áp dụng | PASS cả hai |

Enumeration final **10 invocations chọn/discovered, 10 executed, 10 đạt expected outcome, 0 unexpected FAIL, 0 skip toàn scenario**; ba exit 1 là timeout được cố ý gây ra và được assertion bên ngoài kiểm đúng. Cả tám BUS invocations ghi `PASS cleanup; ... windowsRemaining=0; filters=1/1; hooks=6/6`; ba injection ghi `expired=True`. Normal native RGB/input/live/clipping/modal gates vẫn PASS trên ba positive EXEs; native print total13, alpha-only delta0 như lịch sử. Mixed-DPI vẫn NOT RUN ở năm normal BUS invocations; injection không chạy visual matrix. Hai Minor của review (capture exception ownership, print classifier heuristic) **defer**, không đổi trong fix này; classification hiện tại không được dùng một mình để chứng minh production không snapshot.

Đã build lại bằng bốn commands ở mục Commands bên dưới (cả hai targets, net48 x86, hai negative outputs), mỗi command exit0/0 warnings/errors. Normal output directories của candidate mới là `artifacts/bus01/deadline-net48-positive`, `deadline-net48-x86-positive`, `deadline-net8.0-windows-positive`, `deadline-net48-negative`, `deadline-net8.0-windows-negative`. Injection output là `artifacts/bus01/deadline-inject-net48`, `deadline-inject-net48-x86`, `deadline-inject-net8.0-windows`; chạy ba positive EXEs với cùng `--scenario busy-overlay-live <directory>` và env `BUS_TIMEOUT_PROOF=1`, rồi gỡ env trước normal/default runs. Assertion parent yêu cầu process exit1, có INJECT/TIMEOUT và `PASS cleanup` với zero windows, filters1/1, hooks6/6; không cần kill ở final matrix. Raw artifacts vẫn ignored/local.

## Evidence trước review — candidate fa9c5a9

Ngày thực thi: **29/10/2026**. Scope: [BUS-01](../plans/20261029-003-live-busy-overlay.md), chưa có production host BUS-02/03. **Gate live native PASS trên net48 x64/x86 và net8 x64 ở physical 100% (96 DPI).** Mixed-DPI và nghiệm thu production còn mở.

Candidate: baseline `9359cae07556d2fbf9f99f2f7b09528a9088fe80` cộng source BUS-01 trong commit chứa report này. Git blob của `BusyOverlayScenario.cs` được build/chạy: `5c2690596b5139876ab1cd7e902b6345ab0984b7`; SHA-256 `C11634ED8D0651B263EB5BEF012EAD2308079AB4D069AA5B1BCF81F35525DA6B`. SDK `8.0.427`, Windows `10.0.26300.0`, desktop tương tác, một màn hình `DISPLAY1`, actual window DPI `96`. Runtimes `.NET Framework 4.8.10008.0` và `.NET 8.0.33`. Negative net48 báo `Environment.OSVersion=6.2.9200.0` do thiếu supportedOS; không dùng thông tin version này làm capability gate.

## Kết quả candidate

| EXE/scenario | Bitness | QueryActCtx bytes/count | Kết quả |
|---|---:|---:|---|
| Positive net48 / `busy-overlay-live` | 64 | 40 / 1 | PASS, exit 0 |
| Positive net48 / `busy-overlay-live` | 32 | 40 / 1 | PASS, exit 0 |
| Positive net8 apphost / `busy-overlay-live` | 64 | 40 / 1 | PASS, exit 0 |
| Negative net48 / `busy-overlay-fallback` | 64 | 8 / 0 | PASS probe opaque, exit 0 |
| Negative net8 apphost / `busy-overlay-fallback` | 64 | 8 / 0 | PASS probe opaque, exit 0 |
| Helper mặc định hiện hữu net48 / net8 | 64 / 64 | Không áp dụng | PASS cả hai, exit 0 |

Enumeration tường minh: **7 helper invocations được chọn/discovered, 7 executed, 7 PASS, 0 FAIL, 0 skip toàn scenario**. Không gọi NUnit discovery/full suite trong scope chỉ sửa helper. Mixed-DPI subcase ghi **NOT RUN trong cả 5 BUS invocations** vì không có hai màn hình khác DPI. TAB/input/popup routes và files cũ được giữ; chưa chạy lại toàn bộ các scenarios đó.

Mỗi positive invocation xác minh:

- HWND thực có cùng parent host, z-order native `card > backdrop > Content`; backdrop `Style=0x56010000`, `ExStyle=0x80000`, class style `0x8`, không `CS_OWNDC/CS_CLASSDC` hoặc `WS_EX_TRANSPARENT`. Negative backdrop giữ `ExStyle=0x0`. Card/content `ExStyle=0x10000`, không layered.
- Alpha byte `0/89/255` tương ứng opacity `0/0.35/1`; 56 pixel phẳng mỗi capture khớp RGB blend dự kiến, tolerance 3/channel; card trắng opaque ở cả ba alpha. Spinner thực nằm trong card; card/spinner bị loại khỏi vùng so live và khỏi hash đợi desktop ổn định.
- Generation cố định `1`, backdrop Visible liên tục và cùng HWND qua bốn capture live: TextBox/TreeView `Live 0→1→2→3`, progress `10→35→60→85`, patch `Lime→Red→Blue→Yellow`. Positive net48 x64 HWND `3539046`, net48 x86 `19598690`, net8 x64 `19533154`. Vùng patch có expected-color oracle; text/tree/progress có so pixel riêng ngoài card, không chỉ tổng pixel toàn ảnh.
- Pixel delta text/tree qua ba lần đổi: net48 `48/36/34`; net8 `50/41/37`. Progress delta net48 `864/880/880`; net8 `864/880/880`. Net48 x86 như net48 x64. Nội dung native tiếp tục vẽ dưới tint; helper chỉ capture desktop, không `DrawToBitmap`, `Form.Opacity`, `UpdateLayeredWindow` hoặc snapshot renderer.
- SendInput thật: baseline action khi chưa phủ nhận 1 click. Khi phủ, Content nhận 0 click; Cancel và sibling ngoài host mỗi nút nhận 3 click, ứng với ba alpha. Alpha 0 đã exercise filter scoped subtree, chặn 3 queued mouse messages; không `PerformClick`.
- Ancestor scroll/clip: capture + pixel sentinel ngoài clip giữ nguyên và HWND hit-test không phải backdrop. Higher overlapping sibling hiện Magenta trên overlay. TabPage hide/show, owned modal Coral có timer tự đóng 750 ms, minimize/restore giữ đúng composition đều PASS.

## WM_PRINT và phát hiện trong proof

**Không báo raw total bằng 0.** Instrument Content cùng native descendants ghi `WM_PRINTCLIENT` (`0x318`, flags `0x4`) do native TextBox background và UxTheme `DrawThemeParentBackground` khi child redraw. Positive có first-show delta **3**, tổng đến hết live segment **4**, tổng toàn lifecycle **13** mỗi EXE. Negative lần lượt **0/0/5**. Raw traces nằm trong local `result.txt`; không commit raw results.

Ruling của gate: cấm overlay code gọi Content `WM_PRINT/WM_PRINTCLIENT` để lấy snapshot; native themed-child background requests được đếm riêng và giữ stack. Helper không gửi các messages đó; probe phân loại stack và fail nếu gặp đường print khác. **Ba lần đổi alpha có raw delta 0**; live updates không thêm overlay-origin calls. Ví dụ stack thực: `DrawThemeParentBackground → ButtonStandardAdapter.PaintThemedButtonBackground → Control.WmPaint`; hoặc `TextBox.WndProc → CallWindowProc → Content WM_PRINTCLIENT`. Không dùng assertion total-zero để che redraw native.

Probe phát hiện z-order cần được khẳng định **sau khi show/create hidden child handles**. Trước khi sửa bước này, card bị tint; helper hiện gọi `content.SendToBack(); backdrop.BringToFront(); card.BringToFront()` rồi kiểm thứ tự HWND thật. Production BUS-03 phải giữ invariant này. Capture ban đầu quá sớm còn lấy Windows show animation; helper hiện đợi queue paint và desktop hash ổn định qua nhiều composition frames, deadline 3 giây, mask card/spinner; không giả định một lần DoEvents đủ. Self-review còn tăng độ mạnh case scroll: backdrop phải resize theo host để thực sự vượt ancestor clip; higher sibling sample phải chồng đúng foreground card. Matrix final trong bảng đã chạy lại sau hai sửa này.

Một lần thăm dò default net8 exit 1 khi helper fallback vẫn đang chạy, vi phạm tuần tự GUI; không dùng lần đó làm evidence regression hoặc PASS. Đã chờ helper cũ kết thúc rồi chạy lại hai default EXEs tuần tự, cả hai exit 0, stderr rỗng. Bảng kết quả là các runs final hợp lệ.

## Commands tái hiện

Từ root, PowerShell; warm restore hiện hữu, không claim clean restore:

```powershell
$project = 'tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.csproj'
dotnet build $project -c Release --no-restore
dotnet build $project -c Release -f net48 --no-restore -p:PlatformTarget=x86 -o artifacts/bus01/positive-net48-x86
dotnet build $project -c Release -f net48 --no-restore -p:BusyOverlayNegativeManifest=true -o artifacts/bus01/negative/net48
dotnet build $project -c Release -f net8.0-windows --no-restore -p:BusyOverlayNegativeManifest=true -o artifacts/bus01/negative/net8.0-windows
$env:BUS_CANDIDATE = git hash-object tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/BusyOverlayScenario.cs
```

Cả bốn commands build final đều exit 0, 0 warnings/errors. Chạy trực tiếp lần lượt các `.exe`, gồm net8 apphost, bằng `Start-Process -PassThru`, `WaitForExit(60000)`; cần form hiện trên desktop để capture/input. Không dùng `dotnet host.dll` và không khởi động minimized/hidden. Các command arguments đã chạy:

| EXE path tương đối | Arguments |
|---|---|
| `tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/bin/Release/net48/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe` | `--scenario busy-overlay-live artifacts/bus01/verified-net48-positive` |
| `tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/bin/Release/net8.0-windows/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe` | `--scenario busy-overlay-live artifacts/bus01/verified-net8.0-windows-positive` |
| `artifacts/bus01/positive-net48-x86/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe` | `--scenario busy-overlay-live artifacts/bus01/verified-net48-x86-positive` |
| `artifacts/bus01/negative/net48/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe` | `--scenario busy-overlay-fallback artifacts/bus01/verified-net48-negative` |
| `artifacts/bus01/negative/net8.0-windows/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe` | `--scenario busy-overlay-fallback artifacts/bus01/verified-net8.0-windows-negative` |

Hai EXE positive dưới `bin/Release/` còn được chạy không arguments để kiểm default helper cũ. Query dùng `QueryActCtxW(QUERY_ACTCTX_FLAG_ACTCTX_IS_HMODULE, GetModuleHandle(null), ..., CompatibilityInformationInActivationContext)` với size protocol, offset 8/stride 32 có bounds checking trên x86/x64; đối chiếu manifest resource `RT_MANIFEST #1` trong chính EXE. Positive context chứa Windows 10 supportedOS GUID, negative không chứa declaration. Căn cứ: [QueryActCtxW](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-queryactctxw), [COMPATIBILITY_CONTEXT_ELEMENT](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-compatibility_context_element), [supportedOS IDs](https://learn.microsoft.com/en-us/windows/win32/sbscs/application-manifests).

## Giới hạn và bước tiếp theo

BUS-01 chỉ proof cơ chế native trước implementation; fallback là **OpaqueFallback probe**, chưa chứng minh property RenderMode của production. BUS-03 phải nối helper sang host thật, bỏ filter của probe để không che thiếu sót production và chạy lại exact EXEs. Mixed-DPI, physical 125%/150%/200%, Light/Dark production, lifecycle/token/focus contract và Owner visual acceptance **NOT RUN**; không đóng BUS-04/06 hoặc consumer acceptance. Không đổi host cũ, dependencies, public API hoặc vendor pin; không commit captures/binaries/raw logs.
