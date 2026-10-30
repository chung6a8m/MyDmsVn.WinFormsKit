# Live busy overlay — physical125% ngày30/10/2026

**Physical125 technical gate PASS trên candidate dưới đây: native18/18, Controls181/181 và grid265/265 mỗi target. Feature acceptance vẫn OPEN; Owner visual/live decision PENDING.** Physical150%/200% và mixed-DPI **NOT RUN / Owner-exempt**, chỉ trong scope candidate của lượt này; không ghi PASS hoặc chuyển ngoại lệ sang candidate khác.

## Candidate và môi trường

Base exact `6ccb181b442938224b4948b18f3d9e974d6305e9` trên `codex/busy-overlay-bus01-05`, cộng ba dòng diagnostics trong hai proof helpers. BUS helper ghi thêm managedDeviceDpi; GridGallery helper ghi managedDeviceDpi và thay literal cleanup cũ luôn ghi physical125 NOT RUN bằng metadata thực/Owner PENDING. Không sửa production, API, assertions, resource tolerances, deadlines, manifests hoặc engine. Không tuyên bố chu kỳ RED cho thay đổi log-only. SourceGrid gitlink sạch `f4e457b43582bf01892f50bdc74aa480531e5944`.

Windows11 Home Single Language x64 `10.0.26300`/build26300; SDK8.0.427; Framework4.8.10008.0 (CLR4.0.30319.42000), .NET/WindowsDesktop8.0.33; VSTest17.11.1. Release, warm locked restore. Desktop tương tác, một monitor. User tự chuyển scale125%; agent không đổi OS/display settings. `GetScaleFactorForMonitor` trả125/HRESULT0 (monitor65537). Mọi final process mới có HWND DPI120 và managedDeviceDpi120; positive EXEs PMv2=True. Chỉ launch/close own helper processes; không cleanup demo/user process.

Source hash thực (UTF8 BOM/CRLF):

| File | SHA256 |
|---|---|
| BUS `BusyOverlayScenario.cs` | `E0E4595E5885E0F96420F76E34F35C5DCCD909AA8EB7ADDC83D88616FA868DCA` |
| GridGallery `BusyOverlayScenario.cs` | `C1CB832E467CACCD7F227310A2A3120ED8FCEC4535D792C6321C28EE31D3D7DD` |
| BUS `Program.cs` | `C6D5F05909950987D6E51A7D8E9D428975602D4151EC349E2C7738B9F00051D0` |

`source-hashes.csv` giữ cả manifests/config/production host/demo; `final-executable-hashes.csv` định danh18 executable runs; toàn bộ dependent DLL/config có SHA256 trong inventory. Build All cuối phục hồi default positive outputs; EXE/DLL/config so với executed positive snapshots khớp135/135 (`restored-positive-hashes.csv`). Không lấy historical96 DPI PASS thay lượt này.

## Commands và tests

Chạy từ repository root; GUI/STA tuần tự, bounded pumping, hang diagnostics5phút:

```powershell
.\build.ps1 -Configuration Release -Scope All
$filter = 'FullyQualifiedName~BootstrapBusy|FullyQualifiedName~BusyOverlayDemoFormTests|FullyQualifiedName~Phase16PublicApiBaselineTests|FullyQualifiedName~IntegratedDemo|FullyQualifiedName~AdvancedInputsDemoLayoutRegressionTests|FullyQualifiedName~NavigationDemoFormTests'
# Tuần tự mỗi target net48, net8.0-windows:
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f <TFM> --no-build --no-restore --list-tests --filter $filter --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f <TFM> --no-build --no-restore --filter $filter --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.BootstrapSourceGrid.Tests/MyDmsVn.BootstrapSourceGrid.Tests.csproj -c Release -f <TFM> --no-build --no-restore --list-tests --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.BootstrapSourceGrid.Tests/MyDmsVn.BootstrapSourceGrid.Tests.csproj -c Release -f <TFM> --no-build --no-restore --blame-hang --blame-hang-timeout 5m
```

Build All cuối PASS0warnings/0errors. Final discovery/TRX:

| Test owner / target | Discovered / executed / pass / fail / skip |
|---|---|
| controls / net48 | 181 / 181 / 181 / 0 / 0 |
| controls / net8.0-windows | 181 / 181 / 181 / 0 / 0 |
| grid / net48 | 265 / 265 / 265 / 0 / 0 |
| grid / net8.0-windows | 265 / 265 / 265 / 0 / 0 |

Controls focused gồm old BusyHost, host/controller/capability/layout/native, demo, API17 và integrated typography/advanced input/navigation. Grid là full suite. Các NUnit runs dùng desktop125%; không suy từng NUnit HWND là PMv2/120 nếu fixture không log DPI. Native actual120 proof thuộc exact EXEs bên dưới. Broad full Controls suite mới NOT RUN do production/API không đổi trong lượt diagnostics này.

## Exact native matrix cuối

Build10 outputs riêng bằng `dotnet build <helper-or-GridGallery.csproj> -c Release -f <TFM> --no-restore -o <absolute-output>`; mỗi owner có positive net48x64/net48x86/net8x64 và negative net48x64/net8x64. x86 dùng `-p:PlatformTarget=x86`; negative override exact absolute `ApplicationManifest` tới `app.no-layered.manifest` của đúng project. Scripts/commands đầy đủ: local `verify.ps1`, `final-native.ps1`, `final-verify.ps1`.

Chạy trực tiếp EXE/apphost, không dùng dotnet DLL:

```powershell
& <BUS-positive.exe> --scenario busy-overlay-live <absolute-output> Light|Dark
& <BUS-negative.exe> --scenario busy-overlay-fallback <absolute-output> Light|Dark
& <GridGallery-positive.exe> --scenario sourcegrid-busy-live <absolute-output> Light|Dark
& <GridGallery-negative.exe> --scenario sourcegrid-busy-fallback <absolute-output> Light|Dark
```

Mỗi owner có haiTFM×Light/Dark positive/fallback, thêm net48x86Light positive. Launcher đổi **own thread** sang PMv2 để GetWindowRect/SetCursorPos dùng physical coordinates, xác minh HWND thuộc own PID trước pointer activation. Không đổi DPI policy toàn OS. BUS scenario60s/watchdog75s; GridGallery45s/watchdog60s. Helper kiểm own foreground trước native keyboard; embedded manifests/context và actual mode được assert.

| Owner / host / theme | HWND / managed DPI / PMv2 | Mode / functional gate | Resource / exit |
|---|---|---|---|
| bus / positive-net48-x86 / Light | 120 / 120 / True | NativeLayered / True | PASS100cycles / 0 |
| bus / positive-net48 / Light | 120 / 120 / True | NativeLayered / True | PASS100cycles / 0 |
| bus / positive-net48 / Dark | 120 / 120 / True | NativeLayered / True | PASS100cycles / 0 |
| bus / positive-net8 / Light | 120 / 120 / True | NativeLayered / True | PASS100cycles / 0 |
| bus / positive-net8 / Dark | 120 / 120 / True | NativeLayered / True | PASS100cycles / 0 |
| bus / negative-net48 / Light | 120 / 120 / False | OpaqueFallback / True | PASS100cycles / 0 |
| bus / negative-net48 / Dark | 120 / 120 / False | OpaqueFallback / True | PASS100cycles / 0 |
| bus / negative-net8 / Light | 120 / 120 / True | OpaqueFallback / True | PASS100cycles / 0 |
| bus / negative-net8 / Dark | 120 / 120 / True | OpaqueFallback / True | PASS100cycles / 0 |
| grid / positive-net48-x86 / Light | 120 / 120 / True | NativeLayered / True | N/A (BUS helper owns soak) / 0 |
| grid / positive-net48 / Light | 120 / 120 / True | NativeLayered / True | N/A (BUS helper owns soak) / 0 |
| grid / positive-net48 / Dark | 120 / 120 / True | NativeLayered / True | N/A (BUS helper owns soak) / 0 |
| grid / positive-net8 / Light | 120 / 120 / True | NativeLayered / True | N/A (BUS helper owns soak) / 0 |
| grid / positive-net8 / Dark | 120 / 120 / True | NativeLayered / True | N/A (BUS helper owns soak) / 0 |
| grid / negative-net48 / Light | 120 / 120 / False | OpaqueFallback / True | N/A (BUS helper owns soak) / 0 |
| grid / negative-net48 / Dark | 120 / 120 / False | OpaqueFallback / True | N/A (BUS helper owns soak) / 0 |
| grid / negative-net8 / Light | 120 / 120 / True | OpaqueFallback / True | N/A (BUS helper owns soak) / 0 |
| grid / negative-net8 / Dark | 120 / 120 / True | OpaqueFallback / True | N/A (BUS helper owns soak) / 0 |


Final planned/executed18/pass18/fail0/skip0/timeout0: BUS9/9, SourceGrid9/9; mỗi owner live5/5 và fallback4/4. Tất cả18 actual120/managed120. Negative net48 **PMv2=False**, Environment.OSVersion bị compatibility virtualization thành6.2.9200; chỉ nhận fallback/input/lifecycle PASS ở measured120, không nhận PMv2 hay live PASS. Negative net8 PMv2=True nhưng thiếu supportedOS nên vẫn OpaqueFallback; đúng capability boundary. Không mặc định negative là DPI-unaware96 khi log thực đo120.

BUS positive giữ cùng token/backdrop HWND qua live0–3; native TextBox/TreeView/progress/patch thay đổi dưới tint, foreground opaque; alpha0/.35/1, real queued mouse/wheel/scrollbar không vào Content, Cancel/outside sibling dùng được. Tab hide/show, ancestor scroll/clip, higher sibling, owned modal tự đóng, minimize/restore, handle recreation đều được chạy. Production không snapshot; alpha-only Content WM_PRINT delta0/non-native unexpected print0. `PRINT ... Stack=Environment.StackTrace` là instrumentation native WM_PRINTCLIENT, không phải exception.

SourceGrid positive giữ same token/HWND, marker đổi719pixels và progress đổi; negative marker0pixels, LivePixelGate=False/N/A. Native ComboBox popup/editor đóng trước Begin, caller admission từ pending, alpha/input/Cancel/outside theme, hide/show và reedit/commit sau release đều PASS. Grid caller chủ động disable grid/command button; proof đó không tự chứng minh filter trên enabled Content. BUS helper cung cấp independent queued-mouse filter proof.

Strict resource gate chỉ do BUS helper sở hữu: mỗi run warm10 rồi measured100, checkpoints25/50/75/100, stable deadline1500ms và zero growth giữ nguyên. Tổng final900measured+90warm cycles:

| BUS host/theme | USER/GDI warm10 →100 |
|---|---|
| positive-net48-x86/Light | 49/43 → 49/43 |
| positive-net48/Light | 48/43 → 48/43 |
| positive-net48/Dark | 48/43 → 48/43 |
| positive-net8/Light | 50/48 → 50/48 |
| positive-net8/Dark | 50/46 → 50/46 |
| negative-net48/Light | 48/45 → 48/45 |
| negative-net48/Dark | 48/43 → 48/43 |
| negative-net8/Light | 49/48 → 49/48 |
| negative-net8/Dark | 49/48 → 49/48 |


Tất cả9 cleanup windowsRemaining0 và PrintHook instrumentation7/7. Legacy `filters=0/0` không phải số production registrations. Resource cycles assert InputFilterInstalled=false, owner subscription=null và stale owned HWND mất; không nới production behavior để test xanh. `hooks7/7` ở cleanup là PrintHook counters, khác USER type5/UIAutomation hooks trong điều tra dưới đây.

## Failure lịch sử và nguyên nhân observer

Giữ nguyên hai lượt trước final, không đổi nhãn thành PASS. Lượt đầu18:3exit0/15exit1; ảnh `grid-positive-net48-Light/alpha-0.png` bị Windows Settings Storage Sense che, và runner ban đầu chưa PMv2. User sau đó xác nhận desktop trống. Lượt launcherPMv2 (`canonical-*`) có SourceGrid9/9PASS nhưng BUS1/9PASS: net48 settle timeout1500ms, một số net8 USER growth; một dedicated ETW trace cũng lặp timeout. Đây là failure thực của resource gate trong môi trường có observer, không phải false exception do PRINT trace.

Attribution dùng Win32k UserHandleOperation ETW. Calibration exact SetWindowsHookEx(WH_GETMESSAGE)/Unhook HHOOK402070895 (LastError0/RemovedTrue) khớp452/453type5. Failing settle có hook create/destroy liên tục từ UIAutomationCore worker; matching Microsoft PDB stack: OverlappedIOManager::StaticIoThreadProc → IO/ChannelBasedServerConnection::ProcessOneMessage → ProcessIncomingRequestNoThrow → HwndInvoker::CallTarget → PerformOperationOnHwndThread → InstallHook → USER32 SetWindowsHookExW. Không có toolkit allocation frame trong stack được kiểm.

UIAutomationCore ETW544–547 xác định incoming peerPID32820, `codex-computer-use.exe` task-owned observer bắt đầu07:19:41 khi mở Owner demo. PID1100↔32820 có6654requests mỗi chiều, các helper khác hàng trăm/hàng nghìn; own functional UIA prefix clients chỉ19requests/pair rồi exit. Root reset own node_repl session; PID32820 biến mất, trace2giây sau reset có0requests từ PID đó. Không kill app user, không disable UIA/automation trong production. Fresh final18 chạy **cùng binary hashes và assertions**, full functionalUIA còn bật, tất cả strict resource gates PASS. Vì đã có exact causal client + stack + same-byte clean control, không sửa production hoặc nới tolerance để xử lý observer interference. Raw failed frames/logs/ETL và `observer-attribution.md`, `uia-peers.csv`, `hook-final-calibration.txt`, `hook-stacks.txt`, `resolved-hook-symbols.txt` đều được giữ.

## Local evidence, Owner và phạm vi còn lại

[Frame index physical125 local](../../artifacts/sdd/bus/physical125/frame-index.html), `artifact-hashes.csv`, `source-hashes.csv`, `final-executable-hashes.csv`, final native summary/table, logs và final TRX ở `artifacts/sdd/bus/physical125/`. Index chỉ trình final no-observer frames; liên kết riêng giữ initial/observer attempts. 153 final PNGs. SHA256 inventory bao phủ nguồn/binaries/frames/results/logs/TRX và attribution artifacts; raw artifacts không commit. Local worktree retention only, **shared/durable URL NOT PUBLISHED**, không có cam kết sau cleanup. Không upload public.

Agent mở representative BUS net48Light/net8Dark và SourceGrid net48Dark/net8Light desktop captures: own windows đúng, card/Cancel đọc được, marker/content có tint và foreground opaque. Đây là agent QA giới hạn, **không là Owner visual/live approval**. Owner cần xem actual demo hoặc chuỗi frame/clip của exact candidate và quyết định riêng; không tự đóng BUS-06.

Owner cho ngoại lệ physical150%/200%/mixed-DPI **chỉ candidate/scope lượt30/10/2026**. Ba mục này ghi NOT RUN/Owner-exempt, không PASS, không suy từ synthetic120/144/192 geometry hoặc ngoại lệ TAB/input scope. Một monitor nên helper vẫn ghi mixed-DPI NOT RUN. Ngoại lệ không là Owner approval toàn feature, không miễn consumer MyDmsVn, hardware/third-party hoặc shared artifact/retention. Những mục ngoài scope đó chưa nghiệm thu. Historical A3/input-popup/96DPI reports giữ nguyên; [plan BUS](../plans/20261029-003-live-busy-overlay.md) sở hữu tiến độ hiện hành.
