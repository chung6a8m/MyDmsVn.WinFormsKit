# PR #3 — clipboard net48 và ComboBox accessibility net8

## Candidate và phạm vi

- Bắt đầu từ `c6580218cfb9d6372663b9eb71a1e6585e8c061f`, sau [physical 125% evidence](20261029-input-popup-rendering-physical125.md) ghi broad suite FAIL/ABORTED.
- Source/test execution revision: `c060415141cfa3bf1867593326741edad2de0ae9`. Candidate để review/pin là commit chứa report này; sau execution chỉ cập nhật Markdown contracts/evidence/entry links.
- Windows 11 x64 `10.0.26300`, SDK `8.0.427`, WindowsDesktop runtime `8.0.33`, Release, warm locked restore. SourceGrid gitlink giữ `f4e457b43582bf01892f50bdc74aa480531e5944`.
- Giữ native ComboBox, constructor handle-free, Region/content host ownership, radius-only navigation semantics và targets net48/net8.0-windows. Không thêm package, runner framework, public API hoặc production clipboard retry.

## ComboBox — nguyên nhân và regression

[Source WinForms 8.0.33](https://github.com/dotnet/winforms/blob/v8.0.33/src/System.Windows.Forms/src/System/Windows/Forms/ComboBox.cs) lưu DropDownStyle mới trước RecreateHandle. Trong khoảng đó, list HWND cũ còn sống nhưng owner đã báo Simple. [Child native window](https://github.com/dotnet/winforms/blob/v8.0.33/src/System.Windows.Forms/src/System/Windows/Forms/ComboBox.ComboBoxChildNativeWindow.cs) nhận WM_GETOBJECT và lấy ChildListAccessibleObject theo style mới; nếu provider chưa cached, `_childListBox` của Simple chưa tồn tại và gây NullReferenceException.

`d541a7e` thêm helper subprocess gửi native WM_GETOBJECT tới old list trong WM_DESTROY khi chuyển DropDown→Simple. Baseline net8 reproduced exact stack `ComboBox.get_ChildListAccessibleObject → GetChildAccessibleObject → WmGetObject → NativeWindow.Callback`; helper exit `-1073740771`, NUnit case RED 1/1. Không phụ thuộc external screen reader hoặc đoán từ case cuối của blame log.

`8f88d6f` thêm owned NativeWindow trên native list; chỉ trả0 khi query MSAA/UIA tới list có native style khác owner style hoặc owner đang dispose. Query ổn định vẫn chuyển cho WinForms; helper lấy IAccessible và kiểm role ComboBox sau mỗi recreate. Guard không tạo handle trong constructor, không thay child/provider, không catch/suppress exception trên production path và không reflection private WinForms state.

Review phát hiện full-IntPtr comparison bỏ lọt zero-extended OBJID_CLIENT x64. [Microsoft WM_GETOBJECT contract](https://learn.microsoft.com/en-us/windows/win32/winauto/wm-getobject) yêu cầu chuẩn hóa DWORD trước so sánh và không trả provider khi object đang đóng/chưa sẵn sàng. Enhanced primed/unprimed cases RED 2/2 trên net8 trước sửa; `dd6e8a3` chuẩn hóa 32-bit. Mỗi scenario hiện kiểm MSAA sign/zero-extension và UIA, 3 roundtrips, selection 1, requested Simple height 120 và stable IAccessible queries. Hai cases này chạy trên cả hai TFM.

## Clipboard — điều tra và phạm vi sửa

Không tìm thấy bằng chứng production decimal copy làm mất dữ liệu: fixture 9/9, diagnostic Repeat 40 và các native selection/hook checks đều giữ pending1234/display1,234/selection0..5 cùng native hook HWND. Read-only clipboard lock 180ms không tái hiện lỗi: GetText đợi khoảng 214–216ms và trả đủ 4 ký tự. Giả thuyết chỉ bị một reader giữ lock đã bị loại.

Controlled native client `OpenClipboard → EmptyClipboard → CloseClipboard`, sau đó republish actual text đã copy, tạo khoảng rỗng quan sát được. Single-read assertion RED 1/1 trên mỗi TFM dù control đã copy1234 đúng trước đó. Test hiện dùng observed-empty handshake: worker không publish tới khi observer thực sự đọc rỗng; không dựa vào scheduling/Sleep để tình cờ pass. Worker publish chính actual captured copy, không tạo expected clipboard text để sửa kết quả của control.

`0097a00`/`c060415` sửa observer trong fixture: chỉ chờ rỗng tối đa 1 giây, pump hữu hạn, không phát lại copy/cut và không ghi clipboard. Nonempty sai vẫn fail ngay; lexical text, invalid characters, pending/value và cut semantics giữ assertions cũ. Worker có bounded join trước restore previous clipboard; failures log sequence/owner và thời gian đọc. Không đổi production Clipboard.SetText hoặc nới expected text.

**Giới hạn điều tra:** chưa xác định được process/actor gây lần read-empty lịch sử. Controlled reproduction chứng minh một cơ chế availability và kiểm observer, không chứng minh actor lịch sử là clipboard manager hoặc một tiến trình cụ thể. Không dùng một lượt full xanh để suy đoán nguyên nhân production.

## Các lỗi harness khác được xử lý

- Host vẫn còn sparse sampling trong RequireCaptureContent, dù NUnit image oracle đã quét đầy đủ. Literal 9-color fixture RED trên hai TFM; `5606b3f` đổi host sang full-pixel scan, giữ ngưỡng ít nhất 6 màu và dừng sớm khi đủ. Không nới RGB/Region/content containment gates.
- Full run trung gian gặp lại `ThemeChangePreservesRowSharingForTheLargeScenario`: counter bao cả Application.DoEvents nên gộp consumer row materialization vào theme operation. New queued consumer read `Rows[9999]` tái hiện RED 1/PASS 1 mỗi TFM trên phạm vi counter cũ. `c060415` detach counter sau synchronous theme setter, rồi mới pump consumer callback; false case giữ kiểm cũ, true case buộc callback chạy và giữ 10000 rows. Không đổi production DataGrid hoặc tăng allowance unshared rows trong theme setter. Callback cụ thể của lần unshare lịch sử chưa xác định; queued consumer read là regression cho ranh giới đo.
- Diagnostic Repeat 40 của grid PASS trên net48 nhưng net8 chạm hang diagnostics 5 phút; dump/sequence retained ignored. Không giữ Repeat attribute, không suy timeout stress thành crash accessibility đã sửa. Probe dispose trong HandleCreated cũng lỗi ngay trên stock ComboBox ở cả hai TFM; probe unsupported lifecycle đã bỏ, private attachment vẫn có defensive lifecycle guard.
- Lượt build diagnostic đầu dùng Assert.DoesNotThrow lambda gặp overload ambiguity; đã chọn Action rõ ràng và build cuối không còn warnings. Các lỗi diagnostic/intermediate không thay kết quả candidate cuối.

## Verification cuối

```powershell
.\build.ps1 -Configuration Release -Scope All
.\test.ps1 -Configuration Release -Scope All -Framework All -SkipBuild
```

Build All **PASS**, 0 warnings/errors. Full owners chạy tuần tự trên cùng source cuối **PASS 5442 executions**, fail 0.

| Gate | net48 discovered | net48 executed/PASS | net8 discovered | net8 executed/PASS | Fail/skip cuối |
|---|---:|---:|---:|---:|---|
| Full Controls | 2467 | 2465 | 2467 | 2465 | 0/0 theo runner; 2 Explicit chạy riêng |
| Full SourceGrid | 256 | 256 | 256 | 256 | 0/0 |
| Focused cuối | 105 | 105 | 105 | 105 | 0/0 |
| First-click Explicit | 2 | 2 | 2 | 2 | 0/0 |

Không cộng trùng focused/native reruns vào 5442. Discovery dùng --list-tests đúng project/TFM. Các case Explicit không được suy PASS từ broad run.

Full trung gian trên `0097a00` dừng ở net48 với DataGrid assertion 1 FAIL/2463 PASS; đây là trước sửa phạm vi counter. Lượt diagnostic quiet net48 trước đó PASS 2463 cases, nhưng không được lấy thay full cuối. Các lỗi clipboard/abort của candidate c658021 vẫn giữ ở report lịch sử.

Exact discovery/focused/native commands sau build:

```powershell
$controlsProject = 'tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj'
foreach ($targetFramework in @('net48', 'net8.0-windows')) {
    dotnet test $controlsProject -c Release -f $targetFramework --no-build --no-restore --list-tests --blame-hang --blame-hang-timeout 5m
    dotnet test tests/MyDmsVn.BootstrapSourceGrid.Tests/MyDmsVn.BootstrapSourceGrid.Tests.csproj -c Release -f $targetFramework --no-build --no-restore --list-tests --blame-hang --blame-hang-timeout 5m
    foreach ($filter in @(
        'FullyQualifiedName~BootstrapDecimalInputInteractionTests|FullyQualifiedName~ComboBoxNativeAccessibilityLifecycleTests|FullyQualifiedName~BootstrapOverlayNativeVisualRegressionTests|FullyQualifiedName~InputPopupReviewRegressionTests|FullyQualifiedName~InputPopupRoundTwoRegressionTests|FullyQualifiedName~InputPopupRoundThreeRegressionTests|FullyQualifiedName~BootstrapComboBox|FullyQualifiedName~ThemeChangePreservesRowSharingForTheLargeScenario',
        'FullyQualifiedName~NativeFirstClickFromAnotherControlKeepsPopupAndSearchFocus'
    )) {
        dotnet test $controlsProject -c Release -f $targetFramework --no-build --no-restore --filter $filter --blame-hang --blame-hang-timeout 5m
    }
}
```

STA/nonparallel, message pumping hữu hạn và hang diagnostics 5 phút giữ nguyên. Tests không dùng production fail-fast, retry copy/cut hoặc disable native accessibility để tránh crash.

Review source chỉ đọc đã bắt và kiểm lại x64 object-ID fix, ownership/cleanup, clipboard handshake và DataGrid counter boundary; không còn finding cần sửa. Reviewer phụ không chạy GUI hoặc thay kết quả runtime của implementer.

## Native evidence trên source cuối

Host chạy thêm từng scenario/TFM, timeout 30 giây và confirm termination tối đa 5 giây nếu phải kill:

```powershell
foreach ($targetFramework in @('net48', 'net8.0-windows')) {
    foreach ($scenario in @('rounded-popup', 'input-row', 'combo-accessibility', 'combo-accessibility-primed')) {
        $hostExe = Join-Path (Get-Location) "tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/bin/Release/$targetFramework/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe"
        $outputDirectory = Join-Path (Get-Location) "artifacts/verification/20261029-stability/c060415/$targetFramework/$scenario"
        New-Item -ItemType Directory -Force $outputDirectory | Out-Null
        $captureProcess = Start-Process -FilePath $hostExe -ArgumentList @('--scenario', $scenario, "`"$outputDirectory`"") -WindowStyle Hidden -PassThru -RedirectStandardError "artifacts/stability-final-$targetFramework-$scenario.log"
        if (-not $captureProcess.WaitForExit(30000)) {
            $captureProcess.Kill()
            if (-not $captureProcess.WaitForExit(5000)) { throw 'Host cleanup not confirmed' }
            throw 'Host timed out'
        }
        if ($captureProcess.ExitCode -ne 0) { throw 'Host failed' }
    }
}
```

Raw captures/metadata ở local ignored path trên; PNG/TRX/dumps/logs không commit và chưa upload durable PR/CI artifacts. Bốn popup PNG và bốn input-row PNG mỗi TFM là HWND/CopyFromScreen evidence riêng; a11y scenarios ghi native-query metadata, không giả làm screenshots. Bitmap/oracle unit cases không thay physical acceptance.

Tổng raw cuối: **16 PNG, 20 metadata files**. Canonical input/popup trên cả hai TFM đều ghi managed/native/popup DPI 120, thread/window PerMonitorV2; input native outer height 40px và tất cả RGB/containment/stable-layout oracles PASS. Mỗi a11y scenario ghi transient queries 9 trên x64; stable queries 6 khi unprimed hoặc 7 khi primed, no-crash/stable-native-accessibility/simple-height PASS. Đây là queries bên trong 2 NUnit cases/TFM, không tính thành case count bổ sung.

Logs cuối ở `artifacts/stability-final2-build.log`, `stability-final2-full.log`, `stability-final-focused-<TFM>.log`, `stability-final-firstclick-<TFM>.log`; discovery và RED diagnostics giữ riêng theo tên `stability-*`. Diff/relative links/UTF-8 không BOM được kiểm trước commit evidence. Source API fingerprint và assembly/public identities giữ nguyên; native smoke không phải Owner approval.

## Các mục tiếp tục chưa hoàn tất

- Owner visual decision: **PENDING**, chưa APPROVED/CLOSED.
- Consumer MyDmsVn acceptance: **NOT RUN**; pin exact candidate chứa report và kiểm SecurityPermissionCatalogView/catalog UI tests, Select/Lookup, filter một/hai dòng ở physical 100%/125%.
- Additional physical 125 pairwise matrix và representative physical 150%/200%: **NOT RUN**; canonical capture không thay các mục này.
- Screen-reader UX với sản phẩm/thiết bị hỗ trợ cụ thể chưa được nghiệm thu; native provider smoke không thay consumer accessibility acceptance.

Giữ PR Draft và review threads; không merge, publish package hoặc đổi lịch sử A3/Business Compact. Evidence FAIL/ABORT của candidate c658021 giữ nguyên là lịch sử.
