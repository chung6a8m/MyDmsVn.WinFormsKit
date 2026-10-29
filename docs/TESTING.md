# Build và kiểm chứng

Trang này sở hữu commands và cách chọn scope. [Plan A3](plans/20261024-001-a3-winformskit.md) sở hữu trạng thái; [verification report](verification/20261024-a3-bootstrap.md) ghi commands đã chạy, môi trường, kết quả và giới hạn. Ví dụ lệnh bên dưới không phải bằng chứng PASS.

## Môi trường

- Windows cho build/test được hỗ trợ của WinForms; native focus/input/galleries cần desktop tương tác.
- SDK theo root `global.json`: baseline `8.0.400`, `rollForward=latestFeature`. Chạy từ repository root và ghi `dotnet --version` thực tế.
- .NET Framework 4.8 runtime để chạy `net48`; .NET 8 Desktop runtime để chạy `net8.0-windows`. Reference assemblies phục vụ compile `net48` được restore qua NuGet; cài Developer Pack/targeting tools nếu workflow IDE/Designer yêu cầu.
- VS2022 tương thích SDK đã resolve, với workload .NET desktop nếu dùng IDE/Designer. CLI vẫn dùng cùng project graph.
- Git checkout có submodule SourceGrid tại exact gitlink và nguồn NuGet cần cho dependencies đã khai báo.

Linux/static review không chứng minh Windows runtime, caret, focus hay Designer. Nếu môi trường chỉ làm được một phần, ghi riêng phần đã kiểm và phần `NOT RUN`/`BLOCKED`. Không cần SQL instance hoặc credentials cho hai galleries/test owners của toolkit.

## Chuẩn bị checkout

```powershell
git submodule update --init --recursive
git submodule status --recursive
git -C vendor/sourcegrid status --short
dotnet --version
```

Đối chiếu engine với [PROVENANCE](PROVENANCE.md). Không dùng `--remote`; không tiếp tục bằng cách ghi đè vendor changes. Một clean-restore claim cần checkout sạch và cache/package folder mới hoặc môi trường tương đương được ghi rõ; không suy từ warm restore.

Bảy projects thuộc toolkit giữ `packages.lock.json`; `build.ps1` mặc định restore bằng `--locked-mode`. Root targets pin build-only `Microsoft.NETFramework.ReferenceAssemblies` `1.0.3` cho `net48`, với `VersionOverride` ở SourceGrid CPM, để tránh implicit reference khác nhau theo targeting pack trên máy. Engine giữ central package versions của nó và không ghi vendor lockfile. Executable `net48` restore cả `win7-x86;win7-x64` trên mọi host; đây là restore set để khớp RID mặc định của Windows SDK, không đổi TFM hoặc ép bitness. Lockfiles giữ định dạng do NuGet sinh.

Chỉ khi chủ đích sửa dependency graph, regenerate locks rồi review diff trước khi quay lại locked build:

```powershell
dotnet restore MyDmsVn.WinFormsKit.sln --force-evaluate
git diff -- '**/packages.lock.json'
.\build.ps1 -Configuration Release -Scope All
```

Không bỏ locked mode hoặc cập nhật hàng loạt versions chỉ để một checkout build được. Trên Linux, thêm `-p:EnableWindowsTargeting=true` vào lệnh restore có chủ đích và ghi môi trường như phần cross-compilation.

## Entry points

| Script | Tham số | Hành vi |
|---|---|---|
| `build.ps1` | `-Configuration Debug\|Release`, `-Scope All\|Controls` | Locked restore/build solution tương ứng |
| `test.ps1` | Các tham số trên, `-Framework All\|net48\|net8.0-windows`, `-Filter`, `-SkipBuild` | Mặc định gọi build trước rồi chạy owner suites; filter được truyền tới test runner |

`Controls` chọn solution nhỏ, không kéo adapter/SourceGrid. `All` chọn graph đầy đủ. Giữ GUI test runs tuần tự; một scope chọn hai owners không có nghĩa được chạy hai testhosts tranh focus cùng lúc.

### Kiểm candidate bootstrap trên Windows

```powershell
.\build.ps1 -Configuration Release -Scope All
.\test.ps1 -Configuration Release -Scope All -Framework All -SkipBuild
```

Hai lệnh này chạy liền nhau trên cùng candidate/configuration. `-SkipBuild` chỉ dùng khi bước build vừa thành công và source/dependencies không đổi; nếu không chắc, bỏ tham số để script restore/build lại. Kết quả cần review gồm build của cả hai targets và test results theo từng owner/framework. Không coi exit code 0 là đủ nếu discovery rỗng hoặc mandatory cases bị skip. Không lấy output cũ rồi chạy `--no-build`.

### Vòng sửa controls

```powershell
.\test.ps1 -Configuration Debug -Scope Controls -Framework net48
```

Khi sửa shared code/public API, mở rộng sang target còn lại và adapter nếu contract đó có consumer. Một chỉnh sửa Markdown chỉ cần diff/links/encoding; không kéo GUI hoặc SQL.

### Chọn filter đã xác minh

Ví dụ class `DecimalInputParserTests` được giữ từ [controls source][parser-tests]. Trước khi dùng filter, discovery trên candidate và framework đang kiểm phải tìm thấy cases:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Debug -f net48 --list-tests --filter "FullyQualifiedName~DecimalInputParserTests" --blame-hang --blame-hang-timeout 5m
.\test.ps1 -Configuration Debug -Scope Controls -Framework net48 -Filter "FullyQualifiedName~DecimalInputParserTests"
```

Không dùng filter controls cho cả hai owners rồi tính suite không có cases là PASS. Với adapter-only work, gọi project thật, giữ timeout và build/restore phù hợp:

```powershell
dotnet test tests/MyDmsVn.BootstrapSourceGrid.Tests/MyDmsVn.BootstrapSourceGrid.Tests.csproj -c Debug -f net48 --filter "FullyQualifiedName~BootstrapDecimalInputEditorTests" --blame-hang --blame-hang-timeout 5m
```

`BootstrapDecimalInputEditorTests` có trong [adapter source tại pin import][decimal-adapter-tests]; discovery vẫn phải kiểm trên candidate thực tế trước khi dùng làm evidence. Parser-only selection không chứng minh native editor integration.

### Cross-compilation trên Linux

Hai PowerShell entry points chủ đích yêu cầu Windows. Khi chỉ cần kiểm compiler/reference graph trên Linux, gọi CLI trực tiếp:

```sh
dotnet restore MyDmsVn.WinFormsKit.sln --locked-mode -p:EnableWindowsTargeting=true
dotnet build MyDmsVn.WinFormsKit.sln -c Release --no-restore -p:EnableWindowsTargeting=true
```

Ghi rõ đây là cross-build và SDK/targeting packs đã dùng. Kết quả không chứng minh WinForms runtime, helper-host execution, Designer hoặc Windows/native acceptance. Không chạy tests rồi diễn giải thiếu runtime hoặc skipped cases thành PASS.

Workflow [Windows build](../.github/workflows/build.yml) dùng `windows-2022`, gọi build entry point và kiểm locks/vendor không đổi. Nó không chạy native tests, galleries hoặc package publication. Ghi URL/result của workflow trên đúng candidate khi có; cấu hình workflow tồn tại chưa chứng minh job đã chạy.

## Test owners và scope

| Loại thay đổi | Owner cần xem | Quan sát cần giữ |
|---|---|---|
| Parser/projection/input model của UI | `tests/MyDmsVn.Bootstrap5WinFormUI.Tests` | Raw/accepted value, precision, range/caret mapping theo behavior |
| Control/rendering/theme/lifecycle | Controls tests, ControlGallery, VisualStyleHost khi có liên quan | Focus, per-key input, DPI/theme, resources, construction/disposal |
| Adapter/editor registry/grid styling | `tests/MyDmsVn.BootstrapSourceGrid.Tests`, GridGallery | Commit/cancel/conversion, placement, navigation, ownership, theme |
| Shared control contract dùng trong grid | Cả hai owners | UI behavior và adapter compatibility |
| Project paths/resources/targets/SDK | Hai solutions và owners liên quan | Clean build, đúng assembly/resources/helper host, cả hai targets |
| Consumer cutover | Toolkit owners và app tests trong repo sản phẩm | Native app behavior theo [migration runbook](CONSUMER_MIGRATION.md) |
| Docs-only | Diff, local links/anchors, status consistency, UTF-8 | Nội dung đúng owner, không biến historical result thành current PASS |

Bootstrap giữ nguyên hai test suites gốc cùng helper host; chỉ sửa references tới gallery/source mới. Không retirement tests diện rộng. Nếu bỏ/thay test sau này, PR phải nêu behavior còn được bảo vệ và replacement cùng lớp quan sát, theo [test policy proposal][test-policy].

## An toàn native và kiểm tra thủ công

Giữ STA, exception/DataError capture, bounded message pumping và dispose của helpers hiện có. Không mở modal dialog chờ người dùng trong automated tests. `test.ps1` dùng hang diagnostics với giới hạn **5 phút**; raw GUI test commands cũng cần `--blame-hang --blame-hang-timeout 5m`. Timeout là bảo vệ khi hang, không phải ngân sách hiệu năng. Nếu cleanup không xác nhận được, dừng chạy GUI kế tiếp và xử lý test process đúng owner.

Mở ControlGallery hoặc GridGallery làm startup project để kiểm các behaviors bị ảnh hưởng: typing/selection/caret; Enter/Escape/Tab/Shift+Tab và popup dismissal; commit/cancel; theme/font; create/dispose; Designer nếu đổi construction/serialization. Dùng host và native input thật; gán `Text` nguyên chuỗi không thay per-key observations.

Khi có DPI scope, ghi **physical monitor scale** và **window DPI** riêng. Screenshot không tự chứng minh caret/IME, native focus hoặc per-monitor behavior. Không đem physical/Designer evidence từ baseline cũ sang candidate toolkit, theo [native evidence proposal][native-policy].

### BootstrapTabs và HotTrack — TAB-06

Sau build Controls trên candidate đang xét, discover tests cho cả `net48` và `net8.0-windows`, rồi chạy Controls filter chứa `BootstrapTab`, `Phase16PublicApiBaselineTests`, `NavigationDemoFormTests` và `IntegratedDemo` theo [plan tabs](plans/20261029-002-bootstrap-tabs-hottrack.md#commands-cho-implementer). Dùng `test.ps1` với hang diagnostics 5 phút; nếu gọi `dotnet test` trực tiếp, thêm `--blame-hang --blame-hang-timeout 5m`. Build `-Scope All` để kiểm consumer compile boundary. Ghi discovered/executed/pass/fail/skip cho từng TFM; source compile không chứng minh native behavior.

`VisualStyleHost --scenario tabs-hover <output-directory>` chạy tuần tự trên binary `net48` và `net8.0-windows` mới build, với timeout subprocess 30 giây. Giữ metadata JSON, bounded trace và ảnh trong artifact ngoài Git, kèm candidate SHA, scenario, hash và URL có thể mở lại. Helper ở DPI96 chỉ là một mảnh bằng chứng: không suy ra physical 125% hoặc mixed-DPI từ việc scale số liệu/screenshot.

Native matrix cần Light/Dark × physical 100/125 trên cả hai TFM; thêm representative 150/200 và mixed-DPI khi có môi trường. Ghi physical monitor scale và actual window DPI riêng, thử hover on/off (kể cả pointer đứng yên sau layout và overflow scroll), native overflow/tooltip/focus, close trước selection, `DrawItem` consumer, print HDC/clip/flags, compact mode và native chrome fallback. So sánh gallery `Navigation / BootstrapTabs` với control cũ; Owner xem chuỗi hover và ảnh Light/Dark trên đúng candidate trước khi đánh dấu visual/flicker acceptance. Thiếu desktop/monitor/Owner decision thì ghi `NOT RUN` hoặc `BLOCKED` và giữ gate `OPEN`/`PENDING`, không mượn PASS lịch sử.

Ngoại lệ candidate cụ thể: Owner chấp thuận bỏ yêu cầu chạy representative 150%/200% và mixed-DPI cho TAB hiện hành ngày 29/10/2026; xem [report TAB-06](verification/20261029-tabs-hottrack.md). Vẫn ghi `NOT RUN`/`BLOCKED BY ENVIRONMENT`, không đổi thành PASS. Ngoại lệ này không áp cho BUS hoặc candidate có thay đổi runtime behavior về sau.

Helper host input/popup hỗ trợ `--scenario rounded-popup <output-directory>`, `--scenario rounded-popup-theme <output-directory>` và `--scenario input-row <output-directory>`; launch tuần tự với timeout 30 giây. Scenario input-row chụp Light/Dark ở hàng một/hai dòng và log managed/native DPI, native ComboBox height, FlowLayout containment/stability. Scenario popup giữ RGB oracle, Region/content/footer metadata; bản `rounded-popup` dùng Fuchsia để thử child overpaint, bản `rounded-popup-theme` dùng palette Lookup mặc định để Owner duyệt. Các scenario yêu cầu PerMonitorV2 và managed/native DPI bằng nhau. Xem [commands và evidence physical125%](verification/20261029-input-popup-rendering-physical125.md), [bộ ảnh Owner review](verification/20261029-input-popup-rendering-owner-visual-review.md); canonical native PASS không thay full-suite PASS, Owner decision hoặc consumer acceptance.

Với net48 host, dùng built-in `System.Windows.Forms.ApplicationConfigurationSection` trong App.config, không khai báo lại trong configSections. Manifest giữ Windows10 compatibility/Common-Controls; DPI opt-in thuộc App.config. Native HWND120 trong khi Control.DeviceDpi96 không đủ điều kiện nghiệm thu125%.

Host `--scenario combo-accessibility <output-directory>` và `combo-accessibility-primed` kiểm style recreation với provider chưa tạo/đã tạo: gửi MSAA/UIA queries tới retiring HWND, sau đó lấy IAccessible trên HWND ổn định. Subprocess có timeout15 giây và bounded cleanup5 giây; fixture giữ hang diagnostics5 phút. Regression phân biệt object ID sign-extended và zero-extended trên x64. Xem [stability evidence](verification/20261029-input-popup-rendering-stability.md).

Clipboard assertions chỉ chờ tối đa1 giây khi đọc rỗng và vẫn so sánh exact lexical text; không reissue copy/cut hoặc ghi lại expected value. Controlled republishing test giữ native empty interval tới khi observer thực sự thấy nó, rồi publish lại actual text đã copy. Test này chứng minh khả năng chịu transient availability, không xác định actor của lần lỗi lịch sử. Row-sharing performance assertions đo synchronous theme setter; consumer/accessibility reads được pump sau đó thuộc phạm vi riêng.

### Busy overlay — BUS-06

Chạy GUI tuần tự trên desktop tương tác, STA và cùng candidate; build/discovery/NUnit không thay native live composition. Lặp discovery trên cả hai TFM, giữ hang diagnostics 5 phút:

```powershell
.\build.ps1 -Configuration Release -Scope Controls
$busyTestProject = (Resolve-Path 'tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj').Path
$busyFilter = 'FullyQualifiedName~BootstrapBusy|FullyQualifiedName~BusyOverlayDemoFormTests|FullyQualifiedName~Phase16PublicApiBaselineTests|FullyQualifiedName~IntegratedDemo|FullyQualifiedName~AdvancedInputsDemoLayoutRegressionTests|FullyQualifiedName~NavigationDemoFormTests'
foreach ($busyTfm in @('net48', 'net8.0-windows')) {
    dotnet test $busyTestProject -c Release -f $busyTfm --no-build --list-tests --filter $busyFilter --blame-hang --blame-hang-timeout 5m
    if ($LASTEXITCODE -ne 0) { throw "Discovery failed: $busyTfm" }
    dotnet test $busyTestProject -c Release -f $busyTfm --no-build --filter $busyFilter --blame-hang --blame-hang-timeout 5m
    if ($LASTEXITCODE -ne 0) { throw "Tests failed: $busyTfm" }
}
.\build.ps1 -Configuration Release -Scope All
```

Kiểm discovery có cases thật và ghi discovered/executed/pass/fail/skip từng target; không gọi selection rỗng PASS. Giữ fixtures `BootstrapBusyHost` cũ và gallery DPI regressions. NUnit testhost có thể không có compatibility manifest: pure/fault tests không được giả định `NativeLayered`; native tests phải launch đúng helper EXE.

Recipe positive/negative dưới đây tạo absolute manifest/output paths từ checkout đang đứng. Chạy từng build/run tuần tự vì chung `obj`; không cần đổi manifest source hoặc vendor. Positive dùng manifest riêng đúng target; negative bỏ supportedOS nhưng giữ Common-Controls. Helper có total deadline thực **60 giây**, bounded pump và cleanup; parent guard dưới đây 75 giây chỉ là chốt ngoài cho chính process vừa tạo, không thay hang diagnostics 5 phút của NUnit.

```powershell
$busyRepo = (Get-Location).Path
$busyHelperProject = (Resolve-Path 'tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.csproj').Path
$busyHelperDirectory = Split-Path $busyHelperProject
$busyOutputRoot = Join-Path $busyRepo 'artifacts/verification/busy-bus06'
New-Item -ItemType Directory -Force -Path $busyOutputRoot | Out-Null

foreach ($busyTfm in @('net48', 'net8.0-windows')) {
    foreach ($busyNegative in @($false, $true)) {
        $busyKind = if ($busyNegative) { 'negative' } else { 'positive' }
        $busyManifestName = if ($busyNegative) { 'app.no-layered.manifest' } elseif ($busyTfm -eq 'net48') { 'app.net48.manifest' } else { 'app.manifest' }
        $busyManifest = (Resolve-Path (Join-Path $busyHelperDirectory $busyManifestName)).Path
        $busyBin = Join-Path $busyOutputRoot "$busyKind-$busyTfm-bin"
        $busyTheme = 'Light' # chạy lại recipe với 'Dark'; không gộp evidence hai theme
        $busyEvidence = Join-Path $busyOutputRoot "$busyKind-$busyTfm-$busyTheme-evidence"
        New-Item -ItemType Directory -Force -Path $busyEvidence | Out-Null
        dotnet build $busyHelperProject -c Release -f $busyTfm --no-restore -t:Rebuild "-p:ApplicationManifest=$busyManifest" "-p:BusyOverlayNegativeManifest=$busyNegative" -p:AppendTargetFrameworkToOutputPath=false -p:AppendRuntimeIdentifierToOutputPath=false "-p:OutputPath=$busyBin/"
        if ($LASTEXITCODE -ne 0) { throw "Build failed: $busyKind $busyTfm" }
        $busyExe = (Resolve-Path (Join-Path $busyBin 'MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe')).Path
        $busyScenario = if ($busyNegative) { 'busy-overlay-fallback' } else { 'busy-overlay-live' }
        $busyStart = New-Object System.Diagnostics.ProcessStartInfo
        $busyStart.FileName = $busyExe
        $busyStart.UseShellExecute = $false
        $busyStart.Arguments = "--scenario $busyScenario `"$busyEvidence`" $busyTheme"
        $busyProcess = [System.Diagnostics.Process]::Start($busyStart)
        try {
            if (-not $busyProcess.WaitForExit(75000)) {
                $busyProcess.Kill()
                $busyProcess.WaitForExit(5000) | Out-Null
                throw "Helper exceeded parent deadline: $busyKind $busyTfm"
            }
            if ($busyProcess.ExitCode -ne 0) { throw "Helper failed: $busyKind $busyTfm exit $($busyProcess.ExitCode)" }
        }
        finally { $busyProcess.Dispose() }
        Get-FileHash -Algorithm SHA256 -LiteralPath $busyExe, $busyManifest
    }
}
# Khôi phục default positive outputs trước fixtures dùng helper mặc định:
.\build.ps1 -Configuration Release -Scope Controls
```

Xác minh embedded manifest của **từng EXE** và compatibility context mà helper report, không chỉ hash XML/build property. Chạy trực tiếp net8 apphost, không `dotnet helper.dll`. Kiểm actual `RenderMode=NativeLayered` ở positive và `OpaqueFallback` ở negative, report failure thay vì skip; exit0 riêng lẻ không chứng minh live pixels. Metadata phải giữ content updates dưới tint trong cùng token/HWND, opaque foreground, không production capture/WM_PRINT, alpha0/0.35/1, queued mouse/Cancel/sibling, focus/capture và cleanup. Với x86 net48, rebuild output riêng thêm `-p:PlatformTarget=x86` rồi xác nhận actual process bitness/capability/input; không gán kết quả x64 cho x86.

Matrix BUS cần hai TFM × Light/Dark × physical100/125 với actual native/managed window DPI; representative150/200 và mixed-DPI; clipping/scroll/overlapping sibling/TabPage/modal/minimize/reparent/handle recreation và resource soak. Dark palette unit tests không thay native Dark. DataGridView không thay SourceGrid-under-overlay proof. Thiếu monitor/desktop thì ghi `NOT RUN / BLOCKED BY ENVIRONMENT`, giữ gate mở; TAB DPI exception không áp cho BUS. Upload/open artifact có thể xem lại với candidate/scenario/OS/runtime/SDK/bitness/DPI/hash/URL/retention; Owner phải review actual live demo/clip của đúng candidate. [Plan BUS](plans/20261029-003-live-busy-overlay.md) sở hữu trạng thái; recipe không tự đóng native, consumer hoặc Owner acceptance.

## Báo cáo kết quả

Mỗi lần nghiệm thu ghi candidate SHA, engine pin, OS/SDK/runtime, configuration/framework/bitness, commands/filter, discovered/executed/pass/fail/skip và limitations. Ghi warm hay clean restore; với manual observations ghi gallery, thao tác và kỳ vọng quan sát được. Raw TRX/dumps/logs giữ ngoài Git; chỉ commit bản tóm tắt đã rà soát.

Build chưa chạy là `NOT RUN`; thiếu nền tảng bắt buộc là `BLOCKED`; cases skipped được ghi đúng số lượng/lý do. Results của toolkit không chứng minh app consumer đã migration hoặc public package đã đủ điều kiện phát hành.

[parser-tests]: https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI/blob/8ae217ea8aa9a938ebd558f7071dc737f909e6a5/tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting/DecimalInputParserTests.cs
[decimal-adapter-tests]: https://github.com/chung6a8m/MyDmsVn.BootstrapSourceGrid/blob/e311144c27cd79a6c34aa0308ad029651c9e03c9/tests/MyDmsVn.BootstrapSourceGrid.Tests/BootstrapDecimalInputEditorTests.cs
[test-policy]: https://github.com/chung6a8m/MyDmsVn.Docs/blob/57cf5e0f506ac0c313e9d659939d863325250b53/proposals/2026-10-24-docs-tests-skills.md#4-dọn-tests-theo-rủi-ro-được-bảo-vệ
[native-policy]: https://github.com/chung6a8m/MyDmsVn.Docs/blob/57cf5e0f506ac0c313e9d659939d863325250b53/proposals/2026-10-24-ui-prototyping.md#7-kiểm-chứng-theo-đúng-loại-bằng-chứng
