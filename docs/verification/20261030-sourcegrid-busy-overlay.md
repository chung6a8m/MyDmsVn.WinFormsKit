# SourceGrid dưới live busy overlay — candidate 30/10/2026

## Candidate và phạm vi

Candidate là base `5e13a3d7d47a502fe2aec8901402d238fba9efce` cộng source trong commit chứa report này trên `codex/busy-overlay-bus01-05`. Source inventory chín files code/project/config trong `artifacts/verification/sourcegrid-busy/candidate-source.csv`, SHA256 `58A735FB491F00929F23B1C6702F35ECC9C14B090629766D5325867FF8E71C71`; inventory phân biệt working tree với HEAD. `BusyOverlayDemoForm.cs` SHA256 `EE9F219619328522B7734040AA69B14FCE073322E98277B788273A2CFB61EE5A`, proof `BusyOverlayScenario.cs` SHA256 `2800D9CF76031422ECA5CCC9B55A53FCC98E8C862686CEA980B812ED94C31547`. Gitlink SourceGrid giữ `f4e457b43582bf01892f50bdc74aa480531e5944`, vendor sạch; không thay library API, assembly/package/version hoặc targets.

Windows11 Home Single Language `10.0.26300`, SDK8.0.427; .NET8.0.33, Framework4.8 (CLR4.0.30319.42000). Release; desktop tương tác, actual HWND DPI96. Positive native runs x64, thêm net48x86 Light; một monitor. Không coi DPI geometry hoặc build thay physical125/mixed-DPI.

Đây là SourceGrid integration gate trong BUS-05/06 ở96 DPI. UI core không thêm adapter/engine dependency, không sửa `BootstrapBusyHost` cũ. `GridGallery` dùng ProjectReference hiện hữu. Không nghiệm thu consumer MyDmsVn, hardware hoặc renderer/popup third-party.

## Implementation và seams đã kiểm

Toolbar `Live Busy Overlay` mở `samples/GridGallery/BusyOverlayDemoForm.cs` riêng, sở hữu Content, BootstrapSourceGrid, shared Bootstrap text editor và native engine ComboBox editor riêng. Marker cell/progress cập nhật mỗi100ms trong cùng operation; alpha0–100%, theme, nested operations hoàn thành hai thứ tự, cancel và hide/show ở ngoài Content. Source demo chỉ gọi public overlay API.

Caller đóng native dropdown rồi dùng `CellContext.IsEditing()/EndEdit(true)` trước `Begin`; nếu edit không kết thúc thì không tạo busy token. `Grid.Enabled=false`, command button disabled và guard `TryApply` chặn keyboard/UIA/business admission từ pending150ms đến khi hết token cuối. Timer vẫn cập nhật presentation model. Registry sở hữu Bootstrap editor; demo dispose cả native editor Control và ComponentLight, kể cả chưa attach. Không chia sẻ adapter giữa grids, không tự conversion/commit ngoài SourceGrid.

Đã đối chiếu pinned [CellContext](https://github.com/chung6a8m/sourcegrid/blob/f4e457b43582bf01892f50bdc74aa480531e5944/SourceGrid/SourceGrid/Cells/CellContext.cs), [EditorBase lifecycle](https://github.com/chung6a8m/sourcegrid/blob/f4e457b43582bf01892f50bdc74aa480531e5944/SourceGrid/SourceGrid/Cells/Editors/EditorBase.cs), [native ComboBox](https://github.com/chung6a8m/sourcegrid/blob/f4e457b43582bf01892f50bdc74aa480531e5944/SourceGrid/SourceGrid/Cells/Editors/ComboBox.cs), [ComponentLight disposal](https://github.com/chung6a8m/sourcegrid/blob/f4e457b43582bf01892f50bdc74aa480531e5944/SourceGrid/DevAgeSourcePack4/ComponentModel/ComponentLight.cs), cùng [UPSTREAM_API_SEAMS](../components/grid/UPSTREAM_API_SEAMS.md) và [EDITOR_REPLACEMENT](../components/grid/EDITOR_REPLACEMENT.md). Không patch vendor hoặc đoán private API.

Gallery có embedded Windows10 supportedOS/common-controls manifest cho hai targets; net48 App.config chọn PerMonitorV2, net8 giữ SetHighDpiMode trước HWND. Negative manifest giữ common-controls nhưng bỏ supportedOS, build output riêng. Proof trích manifest resource từ chính running EXE và strict actual RenderMode. Negative net48 bị OS compatibility virtualization (`Environment.OSVersion=6.2.9200`) và PMv2=False; vẫn actual96 DPI. Chỉ positive EXE được tính prerequisite PMv2 PASS; negative chỉ chứng minh fallback.

## Commands và kết quả final candidate

Chạy từ repository root, GUI tuần tự, STA, bounded pumping; native route deadline45s và parent watchdog60s, NUnit hang diagnostics5m. Không thêm test owner/helper project hoặc runner framework. Proof-only route nằm trong chính GridGallery EXE; production host không capture/WM_PRINT Content.

```powershell
.\build.ps1 -Configuration Release -Scope All
dotnet test tests/MyDmsVn.BootstrapSourceGrid.Tests/MyDmsVn.BootstrapSourceGrid.Tests.csproj -c Release -f net48 --no-build --no-restore --list-tests
dotnet test tests/MyDmsVn.BootstrapSourceGrid.Tests/MyDmsVn.BootstrapSourceGrid.Tests.csproj -c Release -f net8.0-windows --no-build --no-restore --list-tests
dotnet test tests/MyDmsVn.BootstrapSourceGrid.Tests/MyDmsVn.BootstrapSourceGrid.Tests.csproj -c Release -f net48 --no-build --no-restore --blame-hang --blame-hang-timeout 5m --logger "trx;LogFileName=grid-verified-net48.trx" --results-directory artifacts/verification/sourcegrid-busy/test-results
dotnet test tests/MyDmsVn.BootstrapSourceGrid.Tests/MyDmsVn.BootstrapSourceGrid.Tests.csproj -c Release -f net8.0-windows --no-build --no-restore --blame-hang --blame-hang-timeout 5m --logger "trx;LogFileName=grid-verified-net8.trx" --results-directory artifacts/verification/sourcegrid-busy/test-results
```

Build All locked/warm restore PASS,0 warnings/0 errors. Final build log `artifacts/sourcegrid-build-final-encoding.log`. Grid discovery `grid-discovery-net48.log`/`grid-discovery-net8.log` đều265; final logs `grid-verified-net48.log`/`grid-verified-net8.log` và TRX tương ứng.

| Owner / target | Discovered | Executed | Pass | Fail | Skip |
|---|---:|---:|---:|---:|---:|
| Grid full / net48 | 265 | 265 | 265 | 0 | 0 |
| Grid full / net8.0-windows | 265 | 265 | 265 | 0 | 0 |
| Controls baseline / net48 | 181 | 181 | 181 | 0 | 0 |
| Controls baseline / net8.0-windows | 181 | 181 | 181 | 0 | 0 |

Grid full gồm chín case mới (hai exact-EXE Light/Dark), toàn bộ existing grid/editor/typography/DPI tests. Controls baseline dùng filter `FullyQualifiedName~BootstrapBusy|FullyQualifiedName~BusyOverlayDemoFormTests|FullyQualifiedName~Phase16PublicApiBaselineTests|FullyQualifiedName~IntegratedDemo|FullyQualifiedName~AdvancedInputsDemoLayoutRegressionTests|FullyQualifiedName~NavigationDemoFormTests`, cùng flags Release/no-build/no-restore/blame-hang5m và TRX như trên, chạy từng TFM. Bao gồm host cũ, native overlay/input/focus, API, integrated typography/layout và đại diện input/navigation tại96 DPI. Controls không đổi production; không chạy lại broad full Controls suite. Physical125 regression vẫn NOT RUN.

Native positive được hai tests launch exact `samples/GridGallery/bin/Release/<TFM>/MyDmsVn.BootstrapSourceGrid.Demo.exe --scenario sourcegrid-busy-live <absolute-output> Light|Dark`; không chạy dll qua dotnet. Native negative và x86 dùng cùng gallery project:

```powershell
$gridNegativeManifest = (Resolve-Path samples/GridGallery/app.no-layered.manifest).Path
$gridProofRoot = Join-Path (Get-Location) 'artifacts/verification/sourcegrid-busy'
# Lặp tuần tự cho net48 và net8.0-windows:
$gridTarget = 'net48'
$gridNegativeBin = Join-Path $gridProofRoot "bin/negative-$gridTarget"
dotnet build samples/GridGallery/MyDmsVn.BootstrapSourceGrid.Demo.csproj -c Release -f $gridTarget --no-restore "-p:ApplicationManifest=$gridNegativeManifest" -p:AppendTargetFrameworkToOutputPath=false -p:AppendRuntimeIdentifierToOutputPath=false "-p:OutputPath=$gridNegativeBin/"
& "$gridNegativeBin/MyDmsVn.BootstrapSourceGrid.Demo.exe" --scenario sourcegrid-busy-fallback <absolute-output> Light
& "$gridNegativeBin/MyDmsVn.BootstrapSourceGrid.Demo.exe" --scenario sourcegrid-busy-fallback <absolute-output> Dark
$gridX86Bin = Join-Path $gridProofRoot 'bin/positive-net48-x86'
dotnet build samples/GridGallery/MyDmsVn.BootstrapSourceGrid.Demo.csproj -c Release -f net48 --no-restore -p:PlatformTarget=x86 -p:AppendTargetFrameworkToOutputPath=false -p:AppendRuntimeIdentifierToOutputPath=false "-p:OutputPath=$gridX86Bin/"
& "$gridX86Bin/MyDmsVn.BootstrapSourceGrid.Demo.exe" --scenario sourcegrid-busy-live <absolute-output> Light
.\build.ps1 -Configuration Release -Scope All
```

Execution dùng visible `Start-Process -WindowStyle Normal`, WaitForExit60000/Kill khi timeout, không launch hidden. Script một lần `artifacts/sourcegrid-matrix.ps1` và log `sourcegrid-extra-matrix-verified.log`; không phải committed runner. Positive default outputs đã rebuild trước final full suites; sau negative/x86 còn build All lại (`sourcegrid-build-restored.log`) và so SHA256 positive EXE cùng gallery/core/adapter/engine DLL với snapshots đã executed, tất cả byte-identical. Embedded manifests, EXE hashes và result logs riêng cho từng run.

## Native matrix final candidate

| Exact host / theme | Actual mode | Actual DPI / PMv2 | Live marker changed pixels | Result |
|---|---|---|---:|---|
| net48x64 / Light | NativeLayered | 96 / True | 497 | PASS |
| net48x64 / Dark | NativeLayered | 96 / True | 497 | PASS |
| net8x64 / Light | NativeLayered | 96 / True | 497 | PASS |
| net8x64 / Dark | NativeLayered | 96 / True | 497 | PASS |
| net48x86 / Light | NativeLayered | 96 / True | 497 | PASS |
| negative net48x64 / Light | OpaqueFallback | 96 / False | 0; live N/A | PASS fallback |
| negative net48x64 / Dark | OpaqueFallback | 96 / False | 0; live N/A | PASS fallback |
| negative net8x64 / Light | OpaqueFallback | 96 / True | 0; live N/A | PASS fallback |
| negative net8x64 / Dark | OpaqueFallback | 96 / True | 0; live N/A | PASS fallback |

Planned/executed9, pass9/fail0/timeout0/skip0 ở final candidate; positive5/5, fallback4/4. Bốn positive x64 runs nằm trong NUnit counts, không cộng thành bốn test khác. Những launch thử trước final không nhập vào matrix này.

Mỗi run mở native engine ComboBox thật, assert popup/edit đang mở, caller kết thúc trước Begin; pending disables grid/command và business guard. Sau show strict mode, real SendInput alpha0/.35/1 vào cell, wheel và scrollbar giữ nguyên active selection, edit state, value và scroll. Native Cancel release token; outside theme button hoạt động, admission mở lại sau release. Đây là behavior tích hợp caller policy + overlay; vì caller disable grid, không dùng nó để tuyên bố riêng mouse filter host đã bảo vệ SourceGrid khi Enabled=true. Host filter có regression riêng trong Controls baseline.

External UIA kiểm command IsEnabled=false; net48 provider không expose InvokePattern khi disabled, net8 expose/Invoke trả success nhưng business state vẫn0. Proof kiểm invariant0, không giả định HRESULT là business admission. Sau UIA, reacquire foreground bằng bounded click vào title của proof form trước keyboard Ctrl+S; assert own foreground rồi dispatch và require business0. Pending behavior/accessibility state/guard còn được kiểm trong grid fixture. Host không là keyboard/UIA firewall.

Ở alpha.35, `live-before.png`/`live-after.png` chỉ so vùng cell(1,0), xác minh không giao card và thay đổi497 pixels; marker đổi từ Tick0 sang Live888888881, progress đổi1, operation count1, backdrop Visible=true và cùng HWND suốt cặp frame. Production không chụp nền. Fallback vẫn update model/progress nhưng desktop marker0 pixels, `LivePixelGate=False`; không nhận live PASS. Hide/show giữ operation; release xong reedit text và SourceGrid commit `Sau busy`. Cleanup mỗi run đóng/dispose form; ownership cases kiểm editor/control disposal cả unattached/attached và double dispose.

Agent đã mở actual captures net48-Light và net8-Dark: cell marker thấy qua tint, card/message/Cancel rõ; đây là visual QA giới hạn, không Owner approval. Grid bị disabled nên text dùng disabled palette của engine; không suy rộng sang hardware hoặc third-party rendering.

Source mới đã chuẩn hóa UTF8 BOM/CRLF theo .editorconfig trước final build, inventory và các run verified; Markdown UTF8 không BOM. Normalization chỉ đổi bytes, không đổi logic; full grid/native matrix vẫn chạy lại trên exact bytes cuối, không lấy hash/source trước normalization làm candidate hiện tại.

## RED/GREEN và các lần thử không dùng làm final evidence

Test-first constructor/admission/live/disposal fixture ban đầu RED compile thiếu `BusyOverlayDemoForm`; implementation GREEN5/5 net8. Review ownership tìm thiếu `ChoiceEditor.Dispose`; regression unattached/attached RED2/2 (`Disposed`0 thay1), sửa dispose cả ComponentLight và Control GREEN2/2. Logs `sourcegrid-dispose-red.log`/`sourcegrid-dispose-green.log` giữ riêng.

Các full-suite thử trước final: net8 `sourcegrid-tests-net8.log`264pass/1fail, net48 `sourcegrid-tests-net48-final.log`263pass/2fail do foreground setup. Proof click cũ tách down/up rồi restore cursor giữa chừng có thể drag title; đã batch hai inputs như helper hiện hữu. Một thử Ctrl+S trong fixture chưa activate có thể mở Save As của Edge; đã bỏ keyboard injection khỏi fixture đó, xác định title/PID rồi đóng đúng dialog không lưu. Native keyboard chỉ dispatch sau own-foreground assertion. Có một outer-watchdog timeout60s và một launch được task dừng khi chẩn đoán activation; process thử do task sở hữu được kill, không thay production/foreground-lock policy. `sourcegrid-native-shell.log`, `sourcegrid-focused-final*.log`, `net8-titlebar*` và ảnh activation giữ lịch sử local.

Sau đó net48 `grid-final-net48.log`263pass/2fail vì proof giả định disabled UIA InvokePattern luôn có; net8 `net8-batched` fail vì giả định Invoke phải throw. Đã sửa proof theo actual provider semantics và luôn kiểm business0. Negative net48 lần đầu fail PMv2 vì cố áp positive prerequisite lên manifest cố ý thiếu supportedOS; final report tách fallback và positive. Các failures không bị skip hoặc tính thành final PASS; pixel threshold/live/foreground/input assertions positive giữ nghiêm.

## Artifacts, hashes và gate còn mở

Local pack `artifacts/verification/sourcegrid-busy/`, [frame index](../../artifacts/verification/sourcegrid-busy/frame-index.html), `artifact-hashes.csv` và `candidate-source.csv`. Index trỏ result/embedded manifest/alpha0-35-100/live before-after của đúng chín runs; logs/TRX/binaries/PNGs không commit. Retention chỉ theo worktree hiện tại, có thể mất khi cleanup; shared/durable URL **NOT PUBLISHED**, chưa có destination/retention được chọn. Không upload public.

| Exact EXE | SHA256 |
|---|---|
| positive net48x64 | `F60C88A01D4BE43818CCE72876FD053AE5D2CC95D945CF89A6FC3FA91C064B93` |
| positive net8x64 apphost | `177125EEEEF0EC24108AAC87C719A26CF4847C24AB8EF5F24ED64956480E20DA` |
| positive net48x86 | `A624B9514AAE1C62837CCD89D63F6536613A23DD3426EB305072E24744FDFFA8` |
| negative net48x64 | `611772EDBC7117A83A183C106106864DF2374E3B71BF4C6F4FAEAF0516070288` |
| negative net8x64 apphost | `36E150F05C928AB37656ED6ED799CC337C50A6A4E31B12EE8221CC49D6949081` |

Net8 apphost hash riêng không nhận diện toàn managed implementation; inventory ghi cả gallery DLL, UI/adapter/engine DLL và source hashes. SourceGrid96-DPI gate PASS trong phạm vi trên. Physical125/150/200%, mixed monitor **NOT RUN / BLOCKED BY ENVIRONMENT**; hardware/third-party renderer/popup **NOT RUN**; consumer MyDmsVn integration **NOT RUN**; Owner actual live/visual decision **PENDING**; durable artifacts **NOT PUBLISHED**. BUS-06 và BUS feature acceptance vẫn OPEN, không mở lại A3 hoặc historical input/popup acceptance.

## Cập nhật physical125 và ngoại lệ Owner ngày30/10/2026

[Evidence physical125](20261030-busy-overlay-physical125.md) thay trạng thái NOT RUN physical125 của các checkpoint trước bằng technical PASS: monitor scale125%, actual HWND120/managed120, BUS9/9 và SourceGrid9/9, Controls181/181 và grid265/265 mỗi TFM. Negative net48 chỉ nhận fallback scope, không nhận PMv2/live PASS. Báo cáo giữ các lượt thất bại do occlusion/automation observer và đối chứng cùng binary sau cleanup observer, không nới assertions hoặc resource thresholds.

Owner đã cho ngoại lệ **physical150%/200% và mixed-DPI chỉ candidate/scope lượt này**: NOT RUN / Owner-exempt, không PASS. Ngoại lệ không đóng Owner visual/live approval, consumer MyDmsVn/hardware/third-party hoặc shared/durable artifact destination/retention. BUS feature acceptance vẫn OPEN. Các trạng thái thiếu125/mixed trong phần trước là checkpoint lịch sử; plan BUS và evidence mới sở hữu tiến độ hiện hành.