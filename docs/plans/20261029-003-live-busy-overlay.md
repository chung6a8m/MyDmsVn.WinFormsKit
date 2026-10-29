# Busy overlay có opacity thực và demo — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: dùng `superpowers:subagent-driven-development` hoặc `superpowers:executing-plans` nếu có; triển khai task theo checkbox và tuân theo AGENTS.md/TESTING.md. Thiếu skill không làm mất các bước kỹ thuật hoặc nghiệm thu.

**Goal:** tạo `BootstrapBusyOverlayHost` cho phép nhìn nội dung native tiếp tục cập nhật dưới lớp phủ có opacity, có spinner/message/Cancel rõ và demo tương tác; giữ nguyên BootstrapBusyHost hiện hữu.

**Architecture:** layered child backdrop với alpha native, foreground card opaque là sibling, cùng parent với Content. Tái sử dụng BusyPresentation, delay, spinner, theme/DPI; token/lifecycle mới riêng, scoped queued-mouse admission và capability fallback tường minh. Không có snapshot renderer hoặc top-level overlay Forms.

**Tech Stack:** C# 12, WinForms/GDI+, Win32 child HWND/SetLayeredWindowAttributes/QueryActCtxW, NUnit; `net48;net8.0-windows`; SDK baseline 8.0.400/latestFeature; không package mới.

**Spec:** [Thiết kế chung, mục 1–2, 4–5](../design/20261029-tabs-hottrack-live-busy-overlay.md). [Plan TAB](20261029-002-bootstrap-tabs-hottrack.md) có thể chạy song song; chỉ tích hợp shared files theo một integrator.

**Baseline:** toolkit `8e7f1854d85d80fbaecc3d9d9167bb8ebcd94921`. **Trạng thái:** PLANNED; chưa có implementation/native/Owner acceptance. Chỉ plan task này sở hữu tiến độ BUS.

## Global Constraints

- Owner đã chọn **3B live native opacity**, không chọn snapshot. Cùng một overlay đang hiện phải thấy content cập nhật; OpaqueFallback không được tính là live PASS.
- Giữ `BootstrapBusyHost.cs`, API và behavior cũ; dùng lại `BusyPresentation`/`IBootstrapBusyDelay`, không refactor host cũ sang controller mới trong scope này.
- Namespace/assembly và targets giữ nguyên; UI core không phụ thuộc app/SQL/SourceGrid; không nâng dependency hoặc đổi vendor pin.
- Public host mới `sealed : Panel`, constructor nhận một unparented Control và sở hữu nó. Constructor handle-free, không query HWND/capability hoặc bắt đầu timer/spinner.
- Default `BackdropOpacity=0.35`; finite `[0,1]`; chỉ backdrop mờ. Default delay 150 ms, không minimum hold, newest live token wins, cancel không tự release.
- Native child-layer chỉ chạy khi executable/OS có capability đã kiểm. Gallery/reference nghiệm thu Windows 10/11; không ngầm thay minimum OS của toàn toolkit.
- Keyboard/UIA/command admission và popup transient thuộc caller; filter chỉ chặn queued mouse tới Content subtree khi presentation hiện. Không OS/global hooks, owner-wide disable hoặc global TopMost.
- Native tests STA/tuần tự/bounded pumping, hang diagnostics 5 phút; report candidate/bitness/actual DPI/counts. Raw artifacts không commit. Text/source encoding theo repository.

## Review Focus

1. Opacity 0 và native child-layer click-through: Content không nhận queued mouse, Cancel/sibling ngoài host vẫn dùng được — BUS-01/03/04.
2. Executable thiếu supportedOS hoặc native API thất bại: báo OpaqueFallback, không nói live đã hoạt động — BUS-01/02/03.
3. Cùng token tồn tại khi content native đổi: desktop thực sự thay đổi qua tint, không capture/WM_PRINT production — BUS-01/03.
4. Cancel đang focus biến mất, callback Begin/Dispose lồng nhau và stale delay: không mất focus sai hoặc hồi sinh overlay — BUS-04.
5. Tab switch, minimize, clipped/scrolling ancestor, sibling/modal cao hơn và DPI/reparent: không phủ sai vùng hoặc giữ filter/HWND cũ — BUS-03/04/06.

## File map và interfaces

Root production: `src/MyDmsVn.Bootstrap5WinFormUI/`; root tests: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/`. Đường dẫn mới là deliverables tương lai.

| File | Trách nhiệm |
|---|---|
| `Controls/BootstrapBusyOverlayHost.cs` | Public composition, Content ownership, lifecycle/theme/DPI/focus, RenderMode events. |
| `Controls/BootstrapBusyOverlayRenderMode.cs` | Public enum `None=0`, `NativeLayered=1`, `OpaqueFallback=2`. |
| `Controls/Internal/BootstrapBusyOverlayController.cs` | Tokens, delayed show, generation guards; riêng host mới. |
| `Controls/Internal/BootstrapBusyLayeredBackdrop.cs` | Child Control paint tint, alpha/fallback, no interactive children. |
| `Controls/Internal/BootstrapBusyOverlayInputFilter.cs` | Scoped queued-mouse admission và show/hide registration ở BUS-03; lifecycle/capture hardening ở BUS-04. |
| `Controls/BootstrapBusyOverlayLayoutLogic.cs` | Pure card metrics/geometry/palette; existing DpiScaler/theme. |
| `Compatibility/BootstrapBusyLayeredWindow.cs` | Interop hẹp, executable compatibility context, native alpha/last error/capture. |
| `Controls/BootstrapBusyOverlayHostTests.cs`, `Controls/BootstrapBusyOverlayCapabilityTests.cs`, `Controls/BootstrapBusyOverlayLayoutTests.cs`, `Controls/BootstrapBusyOverlayNativeTests.cs` trong root tests | Tests theo boundary, native cases chạy executable helper có manifest đã xác minh. |
| `samples/ControlGallery/BusyOverlayDemoForm.cs` và `Demo/BusyOverlayDemoFormTests.cs` trong root tests | Demo thực và route/lifecycle checks. |
| `tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/BusyOverlayScenario.cs` | Proof live composition/capture/input/fallback, không thêm runner framework. |

Public surface chính xác theo design mục 4: constructor `(Control content)`, read-only `Content`, `BackdropOpacity`, read-only `RenderMode`, `RenderModeChanged`, `Begin(BusyPresentation): IDisposable`, enum trên. DefaultValue/Browsable/DesignerSerializationVisibility phù hợp, XML docs cho toàn exported surface. Không tạo public cancellation/task/fallback-provider abstraction.

Internal controller interface (gắn với một UI thread, không giữ static mutable state):

```csharp
BootstrapBusyOverlayController(IBootstrapBusyDelay delay,
    Func<bool> canPresent, Action<BusyPresentation> present, Action hide);
IDisposable Begin(BusyPresentation presentation);
void RefreshVisibility();
void RequestCancel();
void Dispose();
```

`present` cập nhật presentation hiện tại mà không chụp content; nếu đang visible thì đổi message/cancel ngay, không reset delay. Controller sở hữu injected delay và dispose nó đúng một lần; facade sở hữu children/native resources. `hide` phải an toàn khi gọi lặp. Callback có thể reenter nên state/generation được commit trước callback và recheck sau callback.

Native seam internal có thể test failure deterministically: `TryGetLayeredChildCapability(out string reason)` và `TrySetOpacity(IntPtr hwnd, byte alpha, out int errorCode)`. Seam không export và không trở thành compositor framework. Native error/diagnostic reason dùng cho tests/report; không đưa implementation jargon vào message bận của sản phẩm.

## Trình tự và trạng thái

| Task | Depends on | Trạng thái ban đầu |
|---|---|---|
| BUS-01 — child-layer proof và negative manifest | baseline | PENDING |
| BUS-02 — host/controller/public contract | BUS-01 proof | PENDING |
| BUS-03 — native renderer/layout/capability | BUS-01, BUS-02 | PENDING |
| BUS-04 — input/focus/lifecycle hardening | BUS-02/03 | PENDING |
| BUS-05 — demo và manifest gallery | BUS-03/04 | PENDING |
| BUS-06 — contract, API, evidence | BUS-01–05 | PENDING |

### BUS-01 — Proof native trước khi triển khai đầy đủ

**Files:** tạo `tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/BusyOverlayScenario.cs`, `app.no-layered.manifest`; sửa `Program.cs` để có `--scenario busy-overlay-live <output-directory>` và `--scenario busy-overlay-fallback <output-directory>`; giữ scenarios TAB/input/popup cũ. Report khi thực thi: `docs/verification/20261029-live-busy-overlay.md`.

**Interfaces:** produce hai helper scenario commands và evidence; native probe ban đầu nằm trong helper, chưa cần public host production. Existing manifested helper là positive host; negative EXE cùng code nhưng không có supportedOS.

- [ ] Tạo probe child hierarchy `card > layered backdrop > content` bằng real HWNDs, `WS_EX_LAYERED` + `SetLayeredWindowAttributes(LWA_ALPHA)`, không Form.Opacity/UpdateLayeredWindow. Kiểm window class không có CS_OWNDC/CS_CLASSDC; report actual styles/parents/bitness.
- [ ] Content gồm native TextBox, native ListView hoặc TreeView, progress control và các patch màu phẳng. Trong cùng phiên overlay liên tục, đổi text/value/patch ít nhất ba lần. Capture desktop từ **helper** trước/sau; mask foreground/spinner và kiểm đúng thay đổi dưới tint, không chỉ so tổng số pixel thay đổi.
- [ ] Giữ token/probe generation, backdrop HWND và Visible không đổi trong chuỗi capture. Instrument Content WM_PRINT/WM_PRINTCLIENT; show/update overlay không được gọi chúng. Capture phải đợi paint/composition theo điều kiện hữu hạn, không sleep dài hoặc giả định một lần DoEvents đủ.
- [ ] Kiểm alpha 0, 0.35, 1 và foreground opaque. Real SendInput phải xác minh queued mouse không kích hoạt Content trong khi Cancel và nút sibling ngoài host hoạt động. Probe filter scoped HWND subtree; không thay test bằng PerformClick.
- [ ] Thử ancestor clip/scroll, higher overlapping sibling, TabPage ẩn/hiện, owned modal, minimize/restore và mixed-DPI trên hai TFM. Modal test có auto-close/cleanup hữu hạn; không modal wait không timeout.
- [ ] Build positive/negative manifested EXEs tách output, chạy trực tiếp `.exe` (net8 apphost), không chạy `dotnet host.dll` rồi gán manifest cho process khác. Query executable compatibility context bằng API documented, đối chiếu embedded manifest. Negative host phải dẫn OpaqueFallback sau khi production được nối vào scenario; không được fail-crash hoặc báo NativeLayered.
- [ ] Gate: có native live PASS ở **cả net48 và net8**, không snapshot, đúng card/input/clipping. Nếu failed essential case trên supported host, ghi BLOCKED + minimal repro và sửa thiết kế trước BUS-02/03; không dùng opaque fallback để vượt gate hoặc tự đổi sang top-level overlay.
- [ ] Commit `test: prove native child busy overlay composition` gồm helper/manifest/report text. Nếu thiếu Windows, được viết helper nhưng trạng thái proof vẫn NOT RUN, không đánh task DONE.

### BUS-02 — Public host và token controller

**Files:** tạo public host/enum, controller trong file map; tạo `Controls/BootstrapBusyOverlayHostTests.cs`; dùng lại `Controls/BusyPresentation.cs` và `Controls/Internal/IBootstrapBusyDelay.cs` không đổi contract.

**Interfaces:** produce exact public API; controller interface ở trên. Các lớp nội bộ không xuất hiện trong exported API. Enum modes phải phản ánh presentation thực, không suy từ BackdropOpacity đơn lẻ.

- [ ] Viết test RED contract/defaults: content null hoặc có parent bị từ chối; failed construction không dispose content của caller; thành công sở hữu Content, Dock Fill. Host/children constructor chưa tạo HWND, chưa chạy delay/spinner/native capability query; opacity mặc định 0.35, RenderMode None.
- [ ] `OpacityRejectsInvalidValuesBeforeMutation` kiểm -0.01, 1.01, NaN, ±Infinity; 0/0.35/1 hợp lệ; alpha rounding gồm midpoint. `Begin(null)` bị từ chối, Begin sau disposal ném ObjectDisposedException; mutation/release khi còn sống phải đúng UI thread.
- [ ] Cài controller newest-live-token và 150 ms delay bằng abstraction hiện hữu; tests fast operation không show, old release không ẩn newer, newest release hiện previous, double-dispose no-op, latest callback đúng và cancel không tự release. Giữ guards với pending callback bị hủy nhưng đã queued.
- [ ] Cài RenderMode state/event: None khi hidden/pending, NativeLayered hoặc OpaqueFallback khi show, đổi event sau commit state; setter cùng opacity không gây redraw/recreate/delay thừa. Test handler reenter Begin/Dispose/Hide không hồi sinh UI.
- [ ] Chạy old `BootstrapBusyHostTests` nguyên assertions cùng tests mới; source/API/ownership host cũ giữ nguyên. Không chuyển host cũ sang controller mới để tiết kiệm code.
- [ ] Commit `feat: add live busy overlay host contract and token lifecycle`.

### BUS-03 — Layered backdrop, capability và layout

**Files:** tạo backdrop/native/layout/input-filter helpers và `Controls/BootstrapBusyOverlayCapabilityTests.cs`, `Controls/BootstrapBusyOverlayLayoutTests.cs`, `Controls/BootstrapBusyOverlayNativeTests.cs`; hoàn thiện public host; nối scenarios BUS-01 sang implementation thật, bỏ filter của probe để nó không che thiếu sót production.

**Interfaces:** consume controller, produce native renderer/capability seam; native-only setup sau HWND created, không trong constructor. Card là Panel thường với existing BootstrapSpinner, Label và native Button Cancel, không nằm dưới layered backdrop trong ownership tree.

- [x] Viết tests cho capability parser bounds/count/alignment x86/x64, thiếu declaration, query error và native alpha failure; injected errors phải đưa host sang OpaqueFallback, giữ callback/tokens/input behavior. Không catch exception của consumer callback rồi đổi thành lỗi native.
- [x] Capability query dùng **process executable compatibility context** documented, không active visual-style thread context; QueryActCtxW compatibility class có buffer-size protocol và bounds validation. Native supportedOS IDs lấy từ Microsoft manifest docs; không so string version hay parse file EXE tùy tiện. Cache chỉ kết quả môi trường bất biến, không cache failure HWND của một host cho cả process.
- [x] Cài backdrop child RGB opaque từ theme Dark, alpha native Round(255*p,AwayFromZero); kiểm return/last error và HWND lifetime. Không WS_EX_TRANSPARENT/color key, không trộn SetLayeredWindowAttributes với UpdateLayeredWindow. Color alpha được chuẩn hóa để opacity có một owner.
- [x] Khi native path không khả dụng, recreate/configure backdrop thành opaque SurfaceSecondary trong cùng host, báo mode OpaqueFallback. Không lặp retry vô hạn hoặc chụp content. Chuyển mode an toàn và visible state chỉ commit sau setup thành công.
- [x] Trước khi chạy full production scenario, viết RED `ZeroOpacity_BlocksQueuedContentMouseButAllowsCancelAndOutsideSibling`; tạo filter production scoped bằng target == Content HWND hoặc IsChild. Install đúng một lần khi physically show, remove ở hide/dispose/handle loss; không đọc Handle để vô tình tạo mới, không chặn card/sibling/keyboard. Kiểm alpha 0/0.35/1 bằng queued input; baseline này phải PASS ngay BUS-03, BUS-04 bổ sung capture/reentrancy/focus/ancestor edge cases.
- [x] Layout dùng host client coordinates: backdrop Fill; card centered, padding SpacingLG, gaps SpacingSM, width không quá `DpiScaler.Scale(480, dpi)` và phần client còn dùng được; message đo multiline theo font/DPI. Cancel dùng ControlHeightSmall với min width đủ caption đã đo. Khi thiếu chiều cao, reserve hàng Cancel nếu được bật, giới hạn message trong viewport cuộn riêng; giảm outer margin/padding rồi bỏ khoảng trống/spinner nếu cần để button còn dùng được. Khi client nhỏ hơn chính button, clip trong client, không negative bounds hoặc vẽ ra ngoài host. Test long message, custom font, 0-sized host, Light/Dark, 96/120/144/192 DPI.
- [x] Theme/opacity resize cập nhật tại chỗ, không recreate Content/selected rows/reset token/delay/focus; restore z-order chỉ owned children, giữ sibling ngoài host. Khi native setup failure xảy ra sau khi đang live, báo fallback và vẫn giữ foreground/cancel hoạt động.
- [x] Chạy native live/fallback scenarios bằng exact EXEs và lưu evidence mới từ implementation thật; kiểm Content WM_PRINT count vẫn không tăng và background thay đổi dưới cùng overlay HWND.
- [x] Commit `feat: render live busy backdrop with native child opacity`.

Candidate BUS-03: implementation production và regression đã có; review Important về minimum Cancel width đã có regression tại119/120/143/144 logical pixels và sửa spacing ngang theo caption, evidence candidate sửa nằm ở cuối report production; native proof 96 DPI trên hai targets/x86 được ghi riêng tại [evidence production](../verification/20261030-live-busy-overlay-production.md). BUS-04/05/06 và acceptance đầy đủ vẫn mở; không chuyển historical BUS-01 PASS thành nghiệm thu toàn feature.

### BUS-04 — Mouse, focus và vòng đời cửa sổ

**Files:** harden input filter đã tạo ở BUS-03; hoàn thiện host/controller; mở rộng `Controls/BootstrapBusyOverlayNativeTests.cs`, helper và HostTests.

**Interfaces:** filter scoped Content HWND + native descendants; install/remove theo physical show, independent opacity. Chỉ owning UI thread; không process hook hoặc filter keyboard/business messages.

- [ ] Mở rộng regression `ZeroOpacity_BlocksQueuedContentMouseButAllowsCancelAndOutsideSibling` từ BUS-03: real SendInput ở alpha 0/0.35/1 vào TextBox/Button/scrollbar; verify click/value/selection/scroll không đổi trong Content, Cancel/outside control vẫn đúng. Mouse/nonclient/wheel messages có target ngoài subtree phải đi tiếp; thêm RED case cho defect phát hiện, không gọi baseline đang PASS là RED.
- [ ] Audit IMessageFilter ở mọi failure/reentrant hide/dispose/handle-loss path; không đăng ký trùng, không giữ stale HWND. ReleaseCapture chỉ nếu capture của Content descendant; kiểm callback show giữa mouse-down/up không kích hoạt click phía dưới. Chạy lại full production live/fallback scenarios sau lifecycle hardening, không cài filter hỗ trợ từ helper.
- [ ] `Show_PreservesExistingFocus`, `Cancel_IsKeyboardAndUiaInvokable`, `CancelDisappears_RestoresFocusOnlyFromOwnedForeground`: ghi focused descendant ngay trước show; khi card/cancel bị ẩn, trả focus có điều kiện như design. Khi người dùng đã chuyển focus ra ngoài card, không phục hồi. Callback cancel reenter/dispose vẫn cleanup đúng.
- [ ] Caller keyboard/UIA admission test minh họa cả pending và visible; host không giả lập bảo vệ business command. Native popup đang mở phải được caller đóng trước busy; docs/demo thực hiện quy tắc đó, không dò tất cả windows trong process. Raw touch/pen không tự nhận support từ mouse test.
- [ ] `HiddenMinimizedOrDestroyedHost_RemovesFilterAndStopsSpinner`: ancestor hide/tab switch, owner minimize/dispose, host handle recreate/reparent, content disposed, pending callback cũ. Re-show/restore còn token thì reschedule 150 ms; owner deactivate vẫn giữ child presentation, không activate window.
- [ ] `ClippedHostAndHigherSibling_KeepNativeOcclusion`, `OwnedModal_RemainsAboveAndUsable`, `DpiChange_DoesNotResetOperationOrLeakHandles`: verify nested scroll, partially clipped host, independent busy hosts, modal, mixed-DPI và owner move. Đăng ký owner events cần thiết khi parent thay đổi, unsubscribe owner cũ.
- [ ] Resource soak có warm-up rồi 100 show/hide/recreate cycles; assert owned HWND/filter/delay/subscriptions về trạng thái zero sau dispose và process USER/GDI không tăng theo batch. Ghi actual counts/caching, không nới tolerance để giấu leak.
- [ ] Commit `fix: harden busy overlay input focus and native lifecycle`.

### BUS-05 — Demo live overlay và executable prerequisites

**Files:** tạo `samples/ControlGallery/BusyOverlayDemoForm.cs`, `samples/ControlGallery/app.manifest`, `samples/ControlGallery/App.config`, `Demo/BusyOverlayDemoFormTests.cs` trong root tests; sửa gallery csproj/MainForm.cs; kiểm Program.cs. Route mới `Loading / Busy Overlay`.

**Interfaces:** form kế thừa DemoFormBase, chỉ public host API; theme selector hiện hữu. Manifest Windows 10 supportedOS + common-controls v6; net48 App.config PerMonitorV2 như helper hiện có; net8 giữ Application.SetHighDpiMode trước HWND. Không thay process DPI từ control library.

- [ ] Test gallery route, control hierarchy, alpha slider wiring, live content timer, cancellation/nested operation lifecycle và cleanup khi đóng form; source demo không gọi SQL hoặc capture-content API.
- [ ] Tạo vùng Content có TextBox, BootstrapDataGridView/native ListView và progress/counter đổi định kỳ; host vẫn busy. Alpha slider 0–100%, default 35%; điều khiển slider/theme/tác vụ đặt ngoài vùng bị chặn để có thể thao tác khi busy. Có fast operation, long operation, hai tokens hoàn thành khác thứ tự, cancel và hide/show.
- [ ] Hiển thị message nghiệp vụ đơn giản, spinner/cancel rõ; khu diagnostics riêng báo RenderMode, actual DPI và fallback khi có. So sánh host cũ opaque bằng content instance riêng, không reparent cùng Content giữa hai owners.
- [ ] Wire ApplicationManifest rõ trong csproj cho hai targets; net48 chọn App.config, giữ net8 DPI API hiện hành. Kiểm embedded manifest của built EXE và runtime context ở cả hai TFM. Không thêm DPI manifest cũ ghi đè App.config/SetHighDpiMode.
- [ ] Vì DPI awareness gallery net48 thay đổi, chạy IntegratedDemoTypography/Layout và đại diện input/navigation ở physical 100/125; ghi regression nếu layout cũ đổi. Không gọi manifest XML tồn tại là proof runtime DPI đúng.
- [ ] Bổ sung bước nghiệm thu SourceGrid bằng consumer/test host có reference adapter nếu cần xác nhận use case sản phẩm; UI core không thêm dependency BSG. Không tuyên bố đã hỗ trợ hardware/third-party rendering chưa có native evidence; integration với MyDmsVn là PR riêng.
- [ ] Commit `feat: add live busy overlay gallery and manifest setup`.

### BUS-06 — Contract, API và acceptance

**Files:** sửa `docs/components/controls/COMPONENTS.md`, `PUBLIC_API_BASELINE.md`, `COMPATIBILITY.md`, `docs/CONSUMER_MIGRATION.md`, `docs/TESTING.md`, `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`; cập nhật report BUS và checklist plan này.

- [ ] Ghi public contract/defaults, delay/token/cancel/focus/input ownership, live vs OpaqueFallback, manifest prerequisites, popup/raw input boundary và recipe constructor/Begin/dispose trên UI thread. Chỉ docs mới cho host mới; contract host cũ giữ nguyên.
- [ ] Review exported API/protected overrides thực tế trên hai targets, exact host/enum tests, assembly identity/version không đổi. Chỉ update fingerprint sau khi review delta; nếu TAB merge trước/sau thì tính lại trên candidate hợp nhất.
- [ ] Run Commands; discovery thực và pass/fail/skip theo target. NUnit process có thể thiếu manifest: pure/fault tests không giả định RenderMode NativeLayered; native acceptance phải launch đúng manifested helper exe với timeout và assert actual mode, không skip rồi tính PASS.
- [ ] Native matrix hai TFM × Light/Dark × physical 100/125; representative 150/200/mixed monitor, x86 net48 capability/input probe; exact OS/runtime/bitness/DPI. Kiểm live marker, opaque foreground, mouse/Cancel/focus, fallback, clipping/modal/recreate và resource soak.
- [ ] Build All; giữ tests theo diff, old BusyHost fixtures và gallery DPI regressions. Nếu cần SourceGrid evidence chưa chạy, ghi riêng NOT RUN thay vì suy từ DataGridView; không kéo SQL tests vào toolkit.
- [ ] Publish/open evidence artifacts có thể xem lại, ghi exact candidate/URL/scenario/hash/retention cùng before/after frames hoặc clip. Owner review actual live demo/clip; NativeLayered PASS và Owner decision mới đóng BUS acceptance. OpaqueFallback hoặc snapshot không đủ.
- [ ] Commit `docs: document native busy overlay contract and verification`; update task status đúng evidence, không chuyển lịch sử PASS của PR #3 sang feature mới.

## Commands và positive/negative hosts

Các commands là hướng dẫn cho execution, **chưa chạy trong commit lập plan**. Chạy tuần tự từ root checkout Windows và kiểm exe path thực sau build:

```powershell
.\build.ps1 -Configuration Release -Scope Controls
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --list-tests --filter "FullyQualifiedName~BootstrapBusy|FullyQualifiedName~BusyOverlayDemoFormTests|FullyQualifiedName~Phase16PublicApiBaselineTests" --blame-hang --blame-hang-timeout 5m
.\test.ps1 -Configuration Release -Scope Controls -Framework All -SkipBuild -Filter "FullyQualifiedName~BootstrapBusy|FullyQualifiedName~BusyOverlayDemoFormTests|FullyQualifiedName~Phase16PublicApiBaselineTests|FullyQualifiedName~IntegratedDemo|FullyQualifiedName~AdvancedInputsDemoLayoutRegressionTests|FullyQualifiedName~NavigationDemoFormTests"
& tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/bin/Release/net48/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe --scenario busy-overlay-live artifacts/verification/busy/net48
& tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/bin/Release/net8.0-windows/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe --scenario busy-overlay-live artifacts/verification/busy/net8
```

Discovery lặp cho net8.0-windows. Positive helper đã có manifest hỗ trợ; negative manifest giữ common-controls và phần còn lại nhưng bỏ supportedOS. Negative build dùng output riêng, thực hiện hai targets tuần tự; thay thư mục output sau đây bằng absolute task-specific path của checkout khi gọi MSBuild nếu relative path được resolve khác dự kiến:

```powershell
$busyNegativeRoot = Join-Path (Get-Location) 'artifacts/verification/busy/negative-bin'
dotnet build tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.csproj -c Release -f net48 --no-restore -p:ApplicationManifest=app.no-layered.manifest -p:AppendTargetFrameworkToOutputPath=false -p:AppendRuntimeIdentifierToOutputPath=false "-p:OutputPath=$busyNegativeRoot/net48/"
& "$busyNegativeRoot/net48/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe" --scenario busy-overlay-fallback artifacts/verification/busy/negative-net48
dotnet build tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.csproj -c Release -f net8.0-windows --no-restore -p:ApplicationManifest=app.no-layered.manifest -p:AppendTargetFrameworkToOutputPath=false -p:AppendRuntimeIdentifierToOutputPath=false "-p:OutputPath=$busyNegativeRoot/net8/"
& "$busyNegativeRoot/net8/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe" --scenario busy-overlay-fallback artifacts/verification/busy/negative-net8
.\build.ps1 -Configuration Release -Scope All
```

Xác minh manifest thực của từng output; global property override phải hiện trong build/evidence. Build positive lại sau negative trước tests cần dùng default helper output; không parallel hóa hai build dùng chung obj. Một native API success, process exit 0 hoặc test discovery rỗng không chứng minh live composition. Helper scenario tự timeout tối đa 30 giây và cleanup mọi window/filter; parent test process có timeout hữu hạn trong hang gate 5 phút.

## Handoff và rollback

BUS-01→02→03→04→05→06 là đường chính. Native spike có thể chạy song song TAB-01 bằng source tasks độc lập nhưng **GUI execution vẫn tuần tự** để không tranh focus/desktop. Program/MainForm/API baseline/entry docs do integrator cập nhật tuần tự.

New control additive, old host chưa đổi nên có thể rollback từng commit; consumer chưa migrate. Fallback luôn được báo rõ và không làm mất cancel/token cleanup. Nếu core feasibility chưa đạt, giữ task BLOCKED và evidence có thể tái hiện; không phát hành một control mang tên opacity nhưng chỉ dùng ảnh chụp nền.
