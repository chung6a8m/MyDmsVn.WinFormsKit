# Input/popup — kiểm chứng physical DPI 125%

## Candidate và môi trường

- Candidate bắt đầu: `60708404faef20a3b30ebcfd7b2e10b5f6019f76`, sau [review vòng ba](20261029-input-popup-rendering-round-three.md) của PR #3.
- Source/test execution revision: `3aa2c939677326f040d53f47eb4e675dbe77f3e6`; candidate review/pin là commit chứa report này. Sau execution chỉ cập nhật Markdown evidence/entry links.
- Windows 11 x64 `10.0.26300`, SDK `8.0.427`, Release, warm locked restore. SourceGrid giữ `f4e457b43582bf01892f50bdc74aa480531e5944`.
- Windows Settings đã hiển thị **125% (Recommended)** trước lượt chạy; không đổi scale, không sign-out, không giả lập DPI của bitmap. Native host mới được launch sau khi xác nhận Settings.
- UI core/adapter production source, exported API và assembly identities không đổi trong vòng này. Chỉ sửa helper host và tests để kiểm đúng physical DPI.

## Baseline và các lỗi kiểm chứng đã sửa

Lượt host trên `6070840` chưa đủ điều kiện PASS: net48 ghi managed owner DPI96 nhưng HWND120; net8 dừng tại oracle border intercept trên ảnh actual120. Không diễn giải exit0 của host net48 thành nghiệm thu125%.

1. **Opt-in net48 không có hiệu lực.** `App.config` khai báo lại built-in `System.Windows.Forms.ApplicationConfigurationSection`. Đọc bằng .NET Framework ConfigurationManager tái hiện lỗi “It cannot be declared by the user”. Manifest DPI giữ HWND ở PerMonitorV2 nhưng không chứng minh WinForms enhanced DPI đã bật; bỏ manifest DPI riêng lẻ còn cho thấy Unaware96. Sửa cả hai: bỏ section declaration dư, giữ config PerMonitorV2/auto-resizing, chỉ giữ Windows10 compatibility/Common-Controls trong net48 manifest. Theo [hướng dẫn Microsoft .NET Framework](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/high-dpi-support-in-windows-forms?view=netframeworkdesktop-4.8), opt-in dùng App.config; manifest DPI có thể override config. Host hiện kiểm managed/native DPI bằng nhau và context PerMonitorV2.
2. **Corner oracle đòi màu border gần đặc.** Pixel outer edge ở fractional stroke120 có AA blend khoảng75% border; light pixel `(231,234,237)` khác border `(222,226,230)` vượt tolerance8. Regression literal Light/Dark RED trên oracle cũ. Dùng tolerance24 cùng edge oracle, vẫn đòi màu gần border hơn surface và hai màu pattern; giữ run3pixels, symmetry≤1pixel, edge continuity90%, shell-band/content guards. Ba negative cases surface/checker/black tiếp tục bị loại; không tăng allowance production.
3. **Sparse sampling báo nhầm blank.** Ở net48 actual120, lưới lấy mẫu bỏ sót glyph/AA và chỉ đếm7 màu trên Dark Select. Test bitmap có9 màu nhưng nằm giữa sample points tái hiện chỉ đếm1. Quét pixels đầy đủ, dừng khi có9 màu; ngưỡng chấp nhận vẫn `>8`.

`b9470b2` ghi regressions: mỗi TFM 7 discovered/executed, expected RED4/PASS3/skip0 sau sửa loader của harness. Lượt .NET8 ban đầu dùng Assembly.LoadFrom trên apphost EXE bị BadImageFormat; đã đổi thành Assembly.Load để đo RED đúng hành vi. Color-sampling case bổ sung expected RED1/1 trên mỗi TFM. `3aa2c93` giữ cả các regression này và bổ sung scenario input-row.

Helper viết metadata trước các assertions, nên failure vẫn giữ actual DPI/awareness và PNG; chỉ append oracle PASS khi tất cả checks đã đạt. Không ghi pass markers cho ảnh lỗi.

## Phạm vi native125%

- **Input row:** Light/Dark × available width600/430. TextBox, native ComboBox và Refresh cùng hàng ở width600; Refresh xuống hàng ở width430. Kiểm actual HWND DPI/managed DPI, native outer height, TextBox/ComboBox cùng top/height, không cắt bottom+margin, repeated layout không tăng height. Form dùng AutoScaleMode.None để so sánh bounds vật lý rõ ràng; controls vẫn lấy DPI thật của HWND, không truyền DPI giả.
- **Popup:** Light/Dark Select và Lookup đang hiển thị thật; CopyFromScreen gồm exterior band12px, nền pattern, Lookup child Fuchsia để phát hiện overpaint. Kiểm RGB continuity/corner symmetry/shell-band/content containment, native Region và footer/content bounds.
- Full owner suites không được suy thành mọi fixture đều chạy PMV2/120DPI; physical125 evidence thuộc hai scenarios native có metadata trực tiếp ở trên.

## Commands và kết quả

```powershell
.\build.ps1 -Configuration Release -Scope All
.\test.ps1 -Configuration Release -Scope All -Framework All -SkipBuild
```

Build All **PASS**, 0 warnings/errors. **Broad full-suite gate chưa PASS**; không chuyển kết quả xanh của candidate6070840 sang candidate mới.

| Lượt cuối theo owner/TFM | Discovered | Completed | PASS | FAIL | Trạng thái |
|---|---:|---:|---:|---:|---|
| Controls net48 | 2463 | 2461 | 2460 | 1 | FAIL; 2 Explicit chạy riêng |
| Controls net8.0-windows | 2463 | 2281 | 2281 | 0 đã báo | ABORTED; 180 broad cases chưa có kết luận, 2 Explicit chạy riêng |
| SourceGrid net48 | 256 | 256 | 256 | 0 | PASS |
| SourceGrid net8.0-windows | 256 | 256 | 256 | 0 | PASS |
| Focused input/popup + native fixture, mỗi TFM | 35 | 35 | 35 | 0 | PASS, skip0 |
| First-click Explicit, mỗi TFM | 2 | 2 | 2 | 0 | PASS, skip0 |

Tổng completed của lượt broad cuối theo từng owner/TFM là **5254**, gồm 5253 PASS/1 FAIL; không cộng trùng reruns. Net8 abort không phải PASS, dù runner in dòng “Passed!” cho 2281 cases đã hoàn tất. Discovery dự kiến 5434 broad cases hoàn tất nếu không abort; còn 4 Explicit chạy riêng.

- Lượt `test.ps1` đầu dừng ở Controls net48: executed 2461/PASS 2459/FAIL 2, gồm `ClipboardCopyAndCutUsePendingLexicalSelectionIncludingInvalidCharacters` (Clipboard.GetText rỗng thay1234) và `ThemeChangePreservesRowSharingForTheLargeScenario` (unsharedCount1 thay0). Các owner/TFM sau chưa chạy trong lệnh này.
- Chạy riêng đúng 2 cases PASS 2/2 mỗi TFM. Lượt full entry point thứ hai còn lỗi clipboard trên net48: PASS 2460/FAIL 1, DataGrid PASS. Không tiếp tục lặp full chỉ để tìm một lượt xanh.
- Chạy Controls net8 riêng bị host crash với `System.NullReferenceException` tại `ComboBox.get_ChildListAccessibleObject → ComboBoxChildNativeWindow.WmGetObject`. Blame nêu case đang chạy `InputPopupReviewRegressionTests.SimpleModeRetainsHeightRequestedBeforeStyleAndAcrossRoundTrip(True)`; đây chưa phải kết luận case đó gây crash. Nguyên nhân chưa xác định; không đổi production/fail-fast để né lỗi. Focused 35 sau đó PASS trên cả hai TFM, gồm case này.
- Hai grid owners chạy riêng PASS 256/256 mỗi TFM. Review source chỉ đọc `6070840..3aa2c93` không có finding cần sửa về config/oracle/capture; reviewer không chạy GUI/tests.

Raw logs ignored: `artifacts/physical125-full.log`, `physical125-full-rerun.log`, `physical125-full-controls-net8.log`, `physical125-full-grid-<TFM>.log`, `physical125-failed-cases-<TFM>.log`, `physical125-focused-<TFM>.log`. Các lỗi broad suite vẫn mở, cần điều tra riêng; không coi là đã fix chỉ vì focused rerun PASS.

Native fixture gồm 8 cases: popup capture1, input-row capture1, classifier5, color-sampling1. Chỉ2 cases đầu là displayed HWND; 6 cases còn lại là oracle unit tests, không phải physical scenarios riêng.

```powershell
$controlsProject = 'tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj'
foreach ($targetFramework in @('net48', 'net8.0-windows')) {
    dotnet test $controlsProject -c Release -f $targetFramework --no-build --no-restore --list-tests --blame-hang --blame-hang-timeout 5m
    dotnet test tests/MyDmsVn.BootstrapSourceGrid.Tests/MyDmsVn.BootstrapSourceGrid.Tests.csproj -c Release -f $targetFramework --no-build --no-restore --list-tests --blame-hang --blame-hang-timeout 5m
    dotnet test $controlsProject -c Release -f $targetFramework --no-build --no-restore --filter 'FullyQualifiedName~BootstrapOverlayNativeVisualRegressionTests' --blame-hang --blame-hang-timeout 5m
}
```

Do full entry point dừng ở net48, các owner còn lại được chạy tuần tự bằng raw commands:

```powershell
dotnet test $controlsProject -c Release -f net8.0-windows --no-build --no-restore --blame-hang --blame-hang-timeout 5m
foreach ($targetFramework in @('net48', 'net8.0-windows')) {
    dotnet test tests/MyDmsVn.BootstrapSourceGrid.Tests/MyDmsVn.BootstrapSourceGrid.Tests.csproj -c Release -f $targetFramework --no-build --no-restore --blame-hang --blame-hang-timeout 5m
    foreach ($filter in @(
        'FullyQualifiedName~ClipboardCopyAndCutUsePendingLexicalSelectionIncludingInvalidCharacters|FullyQualifiedName~ThemeChangePreservesRowSharingForTheLargeScenario',
        'FullyQualifiedName~InputPopupReviewRegressionTests|FullyQualifiedName~InputPopupRoundTwoRegressionTests|FullyQualifiedName~InputPopupRoundThreeRegressionTests|FullyQualifiedName~BootstrapOverlayNativeVisualRegressionTests',
        'FullyQualifiedName~NativeFirstClickFromAnotherControlKeepsPopupAndSearchFocus'
    )) {
        dotnet test $controlsProject -c Release -f $targetFramework --no-build --no-restore --filter $filter --blame-hang --blame-hang-timeout 5m
    }
}
```

## Evidence native có thể mở lại

Raw Settings screenshot ở local ignored:

```text
artifacts/verification/20261029-physical125/6070840/windows-settings-scale125.png
```

Captures cuối và metadata được ghi tại:

```text
artifacts/verification/20261029-physical125/3aa2c93/
  net48/rounded-popup/
  net48/input-row/
  net8.0-windows/rounded-popup/
  net8.0-windows/input-row/
```

Mỗi scenario/TFM có 4 PNG và 4 metadata files; tổng 16 PNG native và 16 metadata files. PNG/TRX/logs không commit vào Git và chưa upload durable PR/CI artifacts. Baseline/host-fix intermediate captures ở sibling directories là lịch sử, không thay captures cuối.

| Physical125% evidence cuối | net48 | net8.0-windows |
|---|---|---|
| Settings scale / display | 125%, DISPLAY1 | 125%, DISPLAY1 |
| Popup owner managed/native/popup DPI | 120/120/120, cả 4 captures | 120/120/120, cả 4 captures |
| Input form/TextBox/ComboBox managed/native DPI | 120, cả 4 captures | 120, cả 4 captures |
| Thread/window awareness | PerMonitorV2 | PerMonitorV2 |
| TextBox/ComboBox native outer height | 40/40 px | 40/40 px |
| CB_GETITEMHEIGHT(-1) / Font.Height | 34/20 px | 34/20 px |
| Single/wrapped row, containment, stable layout | PASS Light/Dark | PASS Light/Dark |
| Popup RGB continuity/symmetry/shell-band/content | PASS Light/Dark Select/Lookup | PASS Light/Dark Select/Lookup |

Lệnh lưu raw captures cuối, mỗi process có timeout 30 giây:

```powershell
foreach ($targetFramework in @('net48', 'net8.0-windows')) {
    foreach ($scenario in @('rounded-popup', 'input-row')) {
        $hostExe = Join-Path (Get-Location) "tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/bin/Release/$targetFramework/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe"
        $outputDirectory = Join-Path (Get-Location) "artifacts/verification/20261029-physical125/3aa2c93/$targetFramework/$scenario"
        New-Item -ItemType Directory -Force $outputDirectory | Out-Null
        $captureProcess = Start-Process -FilePath $hostExe -ArgumentList @('--scenario', $scenario, "`"$outputDirectory`"") -WindowStyle Hidden -PassThru -RedirectStandardError "artifacts/physical125-final-$targetFramework-$scenario.log"
        if (-not $captureProcess.WaitForExit(30000)) { $captureProcess.Kill(); throw 'Host timeout' }
        if ($captureProcess.ExitCode -ne 0) { throw 'Capture host failed' }
    }
}
```

Lookup Fuchsia là opaque-child stress area có chủ ý trong content bounds, không phải màu theme. Oracle vẫn cấm Fuchsia lan ra shell/content exclusion band. Xem ảnh không tự tạo Owner approval.

## Các nhóm nghiệm thu tiếp tục tách riêng

- Physical125% canonical automated native gates cho input row/Select/Lookup Light/Dark: **PASS** trên cả hai TFM. Không đồng nghĩa full suite xanh hoặc Owner visual đã duyệt.
- Additional physical125 pairwise tiny-bounds/large-radius/thick-border/resize/reopen matrix ngoài canonical scenarios: **NOT RUN** trong lượt này; regressions source/synthetic không thay evidence vật lý riêng.
- Broad-suite clipboard failure và net8 accessibility crash: **OPEN**, như bảng verification trên.
- Representative physical150%/200%: **NOT RUN**. Không suy từ125%.
- Owner visual decision: **PENDING**, chưa APPROVED/CLOSED. Cần Owner xem ảnh/candidate mới và quyết định hình ảnh.
- Consumer MyDmsVn acceptance: **NOT RUN**. Pin exact candidate chứa report, chạy SecurityPermissionCatalogView/catalog UI tests, Select/Lookup và filter một/hai dòng ở physical100%/125%; ghi evidence trong consumer repo.

Giữ PR Draft; không thay lịch sử A3/Business Compact hoặc publish package. Evidence100% trước đó giữ theo candidate lịch sử; vòng này không thay scale về100% để tái nghiệm thu trên host mới.
