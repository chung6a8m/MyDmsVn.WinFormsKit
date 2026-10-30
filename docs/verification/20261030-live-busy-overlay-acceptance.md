# BUS-06 — candidate nghiệm thu live busy overlay

Ngày 30/10/2026. **Implementation BUS-06 đã có tài liệu/API audit và kiểm chứng focused; acceptance toàn feature vẫn PENDING.** Owner chưa quyết định. Physical125/150/200%, mixed-DPI, SourceGrid và durable/shared evidence còn mở; không dùng OpaqueFallback để đóng live gate.

## Candidate và phạm vi

Base exact `c92d8bae81d3e067ebbbd64b97320696ae98fedb`, working diff BUS-06 gồm tài liệu cùng plumbing theme chỉ trong `VisualStyleHost/Program.cs` và `BusyOverlayScenario.cs`. Không sửa production hoặc tests/approved API fingerprint trong lượt này. Helper nhận `[Light|Dark]`, mặc định Light cho command cũ; xác thực tên theme, đặt theme trước khi tạo controls, ghi palette thực vào result và phục hồi theme trong finally sau cleanup. Không thay deadline60s, outer watchdog75s, ownership hoặc strict resource policy. Đây là mở rộng proof host, không đổi API toolkit; native runs dưới đây kiểm trực tiếp lựa chọn Light/Dark, không gán nhãn Dark cho ảnh Light. Không tuyên bố chu kỳ RED cho plumbing này.

Candidate trong raw logs là `c92d8ba+BUS06-helper-theme`; hash helper/binaries/frames được giữ trong local artifact inventory. Tài liệu BUS-04/05 và PR5-round-two giữ evidence lịch sử theo candidate của chúng; report này chạy lại candidate hiện tại.

SHA256 source helper đã build/chạy (working bytes UTF-8 BOM/CRLF): `Program.cs` = `C6D5F05909950987D6E51A7D8E9D428975602D4151EC349E2C7738B9F00051D0`; `BusyOverlayScenario.cs` = `12E7A5232FEBADF2073E4E8A8399CA9FCB0A53700B8D846D46359A55A68A536A`. Commit chứa report này định danh source candidate cuối.

## Môi trường

Windows11 Home Single Language x64, CIM version10.0.26300/build26300; registry DisplayVersion26H2/UBR9711 (registry ProductName còn ghi Windows10). SDK8.0.427. .NET Framework4.8.10008.0, registry Release534296; .NET/WindowsDesktop8.0.33. VSTest17.11.1 x64. Desktop tương tác, một monitor; actual window DPI96, physical100%. Warm locked restore, không phải clean-cache restore. Không thay display scaling của máy để tạo bằng chứng DPI.

## Commands đã chạy và kết quả

```powershell
.\build.ps1 -Configuration Release -Scope All
$filter = 'FullyQualifiedName~BootstrapBusy|FullyQualifiedName~BusyOverlayDemoFormTests|FullyQualifiedName~Phase16PublicApiBaselineTests|FullyQualifiedName~IntegratedDemo|FullyQualifiedName~AdvancedInputsDemoLayoutRegressionTests|FullyQualifiedName~NavigationDemoFormTests'
# Tuần tự net48 rồi net8.0-windows:
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f <TFM> --no-build --no-restore --list-tests --filter $filter --blame-hang --blame-hang-timeout 5m
.\test.ps1 -Configuration Release -Scope Controls -Framework All -SkipBuild -Filter $filter
```

Build All PASS,0 warnings/0 errors, cả UI core/grid adapter và hai TFM. Controls focused discovery181 mỗi TFM; executed181/pass181/fail0/skip0 mỗi TFM (23s mỗi target). GUI tests chạy tuần tự. Bao gồm old BusyHost7, capability19, host61, layout8, native10, BusyOverlayDemo2, API17; còn lại IntegratedDemo/advanced input/navigation và một ComboBox visual case khớp filter. Không chạy broad full suite mới vì không đổi production. Adapter được build nhưng không chạy adapter native/SourceGrid acceptance; không tính DataGridView làm SourceGrid.

## API và identity thực tế

Probe local gọi chính `Phase16PublicApiBaselineTests.BuildApiSurface`/`ComputeSha256` trên assemblies build mới cho từng target; xuất text thực và so hash. Hai exported surfaces byte-identical SHA256 `a96de87de1e1a0df9dbddf68a48614a13ca6e83e4927fbb4b64f2fc336c56cba`, khớp approved fixture. API suite17/17 PASS mỗi target, gồm exact host/enum/protected overrides và version. Không đổi fingerprint để làm xanh test.

Cả hai targets: `MyDmsVn.Bootstrap5WinFormUI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null`; FileVersion1.0.0.0; ProductVersion `1.0.0-rc.1+c92d8bae81d3e067ebbbd64b97320696ae98fedb`. Probe chạy x64 trên Framework4.8.10008.0 và .NET8.0.33. Adapter trên cả hai targets cũng giữ `MyDmsVn.BootstrapSourceGrid, Version=0.1.0.0, Culture=neutral, PublicKeyToken=null`, FileVersion0.1.0.0, ProductVersion `0.1.0-preview.1+c92d8bae81d3e067ebbbd64b97320696ae98fedb` (metadata thực trong `bus06-identities.json`); không tuyên bố adapter exported-API/native acceptance từ phép đọc identity. Local one-off API probe build có4 nullable warnings của probe,0 errors; không phải cảnh báo Build All. Các members additive đã review được mô tả trong [API baseline](../components/controls/PUBLIC_API_BASELINE.md); public namespace/assembly/targets giữ nguyên.

## Native matrix candidate

Build riêng positive net48x64/net48x86/net8x64 và negative net48x64/net8x64 bằng project helper thật, `-c Release -f <TFM> --no-restore -o <output>`; x86 dùng `-p:PlatformTarget=x86`, negative dùng exact absolute `-p:ApplicationManifest=.../app.no-layered.manifest`. Chạy trực tiếp từng EXE/apphost với `--scenario busy-overlay-live|busy-overlay-fallback <absolute-output> Light|Dark`, không chạy dll qua dotnet. Embedded manifest và QueryActCtx(EXE HMODULE) được ghi trong từng log; negative thiếu supportedOS và phải là OpaqueFallback; `Environment.OSVersion` của negative net48 bị compatibility virtualization thành6.2.9200, không phải OS thật đã ghi bằng CIM ở trên. Outer watchdog75s, scenario deadline60s, runs exclusive tuần tự. Runner chỉ kích hoạt cửa sổ proof, không thêm hook/input policy vào production.


Lựa chọn palette explicit đã thực thi trong runner (ví dụ net48; net8 dùng cùng command với `positive-net8`/`negative-net8`):

```powershell
& artifacts/sdd/bus/bus06-bin/positive-net48/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe --scenario busy-overlay-live <absolute-output-Light> Light
& artifacts/sdd/bus/bus06-bin/positive-net48/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe --scenario busy-overlay-live <absolute-output-Dark> Dark
& artifacts/sdd/bus/bus06-bin/negative-net48/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe --scenario busy-overlay-fallback <absolute-output-Light> Light
& artifacts/sdd/bus/bus06-bin/negative-net48/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe --scenario busy-overlay-fallback <absolute-output-Dark> Dark
```
| Host/theme | Actual mode / native live gate | USER/GDI warm10 →100 | Exit / cleanup |
|---|---|---|---|
| positive-net48-x86-Light | NativeLayered / PASS | 48/43 → 48/43 | 0 / PASS |
| positive-net48-Light | NativeLayered / PASS | 48/43 → 48/43 | 0 / PASS |
| positive-net48-Dark | NativeLayered / PASS | 49/43 → 49/43 | 0 / PASS |
| positive-net8-Light | NativeLayered / PASS | 49/46 → 49/46 | 0 / PASS |
| positive-net8-Dark | NativeLayered / PASS | 49/46 → 49/46 | 0 / PASS |
| negative-net48-Light | OpaqueFallback / N/A | 48/43 → 48/43 | 0 / PASS |
| negative-net48-Dark | OpaqueFallback / N/A | 49/45 → 49/45 | 0 / PASS |
| negative-net8-Light | OpaqueFallback / N/A | 50/48 → 50/48 | 0 / PASS |
| negative-net8-Dark | OpaqueFallback / N/A | 50/48 → 50/48 | 0 / PASS |

Native planned/executed9, pass9/fail0/timeout0/skip0; live5/5 và fallback4/4. Cả9 actual96 DPI, một monitor; cleanup windowsRemaining0 và hooks7/7; log legacy filters0/0 không phải số registrations của production. Resource cycles100 mỗi run ngoài warm10 (tổng900 measured +90 warm).


Các positive live run kiểm cùng token/backdrop HWND liên tục với native TextBox/TreeView/progress và bốn màu marker, captures live0–3; foreground card opaque, alpha0/.35/1, real queued mouse/content scroll chặn đúng, Cancel/outside sibling hoạt động, UIA Invoke của Cancel/outside button, tab hide/show, scroll/clipping, overlapping sibling, owned modal tự đóng hữu hạn, minimize/restore và handle recreation. Production không snapshot; incidental WM_PRINT của native background được log riêng, alpha-only delta0/non-native Content print0. NativeLiveGate chỉ PASS cho NativeLayered; negative giữ opaque fallback suốt cập nhật, kiểm palette/input/lifecycle tương ứng và ghi NOT APPLICABLE. Focus/reentrant callbacks và caller keyboard/UIA boundary được kiểm bởi focused host/native fixtures theo contract; helper không biến mouse filter thành keyboard/UIA firewall.

Resource gate mỗi run warm10 rồi100show/hide/recreate/dispose, ghi USER/GDI tại25/50/75/100; zero tolerance, bounded prefix RCW/finalizer drain hiện hữu được giữ nguyên. Cleanup ghi windowsRemaining0, hooks7/7. `filters=0/0` là legacy helper diagnostic counters, không phải đếm production registrations: resource-cycle assertions kiểm `InputFilterInstalled=false`, `_owner=null`, stale owned HWND đã mất; focused fixtures kiểm production registration/lifecycle. Các con số dưới đây là candidate mới, không chép historical PASS.

## Artifact review và giới hạn

Artifact root local `artifacts/sdd/bus/`, [frame index local](../../artifacts/sdd/bus/bus06-frame-index.html), inventory `bus06-artifact-hashes.csv`, native summary `bus06-native-summary.json`, logs `bus06-*.log`, frame folders `bus06-native-<kind>-<theme>/`, TRX copy `bus06-testresults/`. File index có thể mở locally bằng browser/file viewer và dùng relative links tới PNG/result; không cần upload. Inventory có SHA256 từng helper source, EXE/DLL, log và PNG, exact relative path/scenario. **Chỉ local; shared/durable publication NOT PUBLISHED (chưa có destination/URL hoặc retention policy được chọn), khác với physical-DPI BLOCKED BY ENVIRONMENT. Retention theo worktree hiện tại, không cam kết lưu sau cleanup. Không upload public.** Raw binaries/logs/PNGs/TRX không commit. Absolute Owner-pack index: `C:\Users\pdchung\.codex\worktrees\busy-overlay-bus01-03\WinFormsKit\artifacts\sdd\bus\bus06-frame-index.html`.

Agent đã mở desktop captures Light (net48x86 live1), Dark (net48x64 và net8x64 live1): card Light trắng/Dark sẫm đều opaque, message/Cancel đọc được, marker/progress vẫn thấy qua tint. Đây là visual QA giới hạn của proof host; surrounding content native dùng màu cố định, không phải review toàn gallery Dark. Owner review actual demo/clip vẫn còn mở.

| Frame canonical (local) | SHA256 |
|---|---|
| positive-net48-Light / live-1.png | `CBB91C985D616EFC89F06AB160E832383D22B8A81F230950AB1DE3378100B7B5` |
| positive-net48-Dark / live-1.png | `D1E5C853A3EFAC8D4958DBB63E1C9F27E855990D6ADAC0131A92E0F3AA8AE6AF` |
| positive-net8-Light / live-1.png | `3B64104C497AF02274EB4D259FD32C73C3DF8550DC02D319F1D5013ED570A0E8` |
| positive-net8-Dark / live-1.png | `06323FF921CEA70757E6E04E2C1E050BB1B82CDB348E2C459EFABCCDC56DBF86` |

Owner decision **PENDING**: visual QA của agent không phải approval; Owner cần xem actual live demo/frame sequence hoặc clip và quyết định trên exact candidate. Chưa có clip mới published hoặc external retention URL. SourceGrid cần adapter host với instance/editor riêng và actual native evidence: **NOT RUN**. Physical125% hai TFM×Light/Dark, representative150/200%, mixed physical monitors: **NOT RUN / BLOCKED BY ENVIRONMENT**; monitor hiện tại chỉ96 DPI. Các pure layout cases120/144/192 không thay native physical matrix. Không đóng BUS-06/feature acceptance, không mở lại lịch sử A3/input-popup hoặc consumer MyDmsVn.

## Gate còn mở và điều kiện đóng

| Gate | Trạng thái | Cần gì tiếp theo |
|---|---|---|
| Hai TFM × Light/Dark tại physical125%, representative150/200%, mixed-DPI | NOT RUN / BLOCKED BY ENVIRONMENT | Desktop/monitor có scale tương ứng; chạy native matrix và ghi actual HWND DPI, không thay bằng synthetic geometry |
| SourceGrid dưới overlay | PASS tại actual96 DPI; physical/mixed-DPI còn mở | [Evidence SourceGrid candidate mới](20261030-sourcegrid-busy-overlay.md): GridGallery exact EXE, hai TFM×Light/Dark live/fallback, thêm net48x86 live; editor/admission/live pixels/cancel/reedit đã kiểm, không suy sang hardware/third-party hoặc Owner |
| Shared/durable artifact URL và retention | NOT PUBLISHED / thiếu destination và retention policy | Chọn nơi lưu artifact cho Owner/reviewer, upload đúng candidate, ghi URL/hash/retention rồi kiểm mở lại; giữ local pack trước cleanup |
| Owner visual/live approval | PENDING / cần Owner quyết định | Owner xem actual gallery/demo hoặc chuỗi frame/clip của exact candidate tại các DPI đã yêu cầu; ghi phạm vi và quyết định, không suy từ automated PASS |
| BUS-06 và BUS feature acceptance | OPEN | Chỉ đóng sau các gate trên; commit implementation/docs không tự là nghiệm thu |
## Cập nhật SourceGrid ngày 30/10/2026

Các mục SourceGrid NOT RUN trong kết quả BUS-06 phía trên mô tả candidate lịch sử đó. Candidate working tree mới trên HEAD 5e13a3d có [evidence SourceGrid](20261030-sourcegrid-busy-overlay.md) riêng: Build All, grid full265/265 mỗi target, native live5/fallback4 tại96 DPI đã PASS; UI core không phụ thuộc adapter. Gate SourceGrid96 DPI hiện đã đạt trong scope demo GridGallery. Physical125/150/200/mixed-DPI, hardware/third-party, shared durable artifacts, consumer MyDmsVn và Owner approval vẫn mở; không đổi kết quả historical BUS-06 hoặc đóng feature acceptance.

## Cập nhật physical125 và ngoại lệ Owner ngày30/10/2026

[Evidence physical125](20261030-busy-overlay-physical125.md) thay trạng thái NOT RUN physical125 của các checkpoint trước bằng technical PASS: monitor scale125%, actual HWND120/managed120, BUS9/9 và SourceGrid9/9, Controls181/181 và grid265/265 mỗi TFM. Negative net48 chỉ nhận fallback scope, không nhận PMv2/live PASS. Báo cáo giữ các lượt thất bại do occlusion/automation observer và đối chứng cùng binary sau cleanup observer, không nới assertions hoặc resource thresholds.

Owner đã cho ngoại lệ **physical150%/200% và mixed-DPI chỉ candidate/scope lượt này**: NOT RUN / Owner-exempt, không PASS. Ngoại lệ không đóng Owner visual/live approval, consumer MyDmsVn/hardware/third-party hoặc shared/durable artifact destination/retention. BUS feature acceptance vẫn OPEN. Các trạng thái thiếu125/mixed trong phần trước là checkpoint lịch sử; plan BUS và evidence mới sở hữu tiến độ hiện hành.
## Quyết định Owner sau checkpoint này

Ngày30/10/2026, Owner visual/live APPROVED candidate ba603d2 và cho ngoại lệ nơi lưu artifact dùng chung. Shared publication vẫn NOT PUBLISHED nhưng được Owner-exempt, cùng ngoại lệ150/200/mixed-DPI NOT RUN; BUS acceptance đã ACCEPTED trong scope candidate. Các trạng thái PENDING/OPEN phía trên là lịch sử trước quyết định. Xem [Owner decision](20261030-busy-overlay-owner-approval.md) để biết exact SHA, phạm vi, retention local và giới hạn consumer/hardware/third-party.
