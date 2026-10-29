# PR #3 — review vòng ba trên `803888a`

## Candidate và phạm vi

- [Review 5693085782](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/pull/3#pullrequestreview-5693085782) xác nhận hai tình huống vòng hai đã xử lý trong source, nêu hai findings P2 mới trên `803888aa30037a78361312b1d48d122c7d9f0013`.
- Source/test execution revision cuối: `aad1333876115cf77767a0301a3f2cf30308a974`. Candidate để review/pin là commit chứa report này; sau execution chỉ cập nhật Markdown contracts/evidence/entry links.
- Môi trường: Windows 11 x64 `10.0.26300`, SDK `8.0.427`, Release, desktop tương tác actual DPI 96. Locked restore dùng cache hiện có; không phải clean-cache claim.
- SourceGrid gitlink giữ `f4e457b43582bf01892f50bdc74aa480531e5944`; không đổi targets, assembly identities, dependencies hoặc public API.
- [Evidence vòng hai](20261029-input-popup-rendering-round-two.md), [vòng một](20261029-input-popup-rendering-review-fixes.md) và [implementation](20261029-input-popup-rendering.md) giữ nguyên là lịch sử theo từng candidate.

## Findings và sửa source

| Finding | Regression Windows | Commit |
|---|---|---|
| [Height của caller trong HandleCreated bị ghi đè](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/pull/3#discussion_r4408866364) | Live DropDown→Simple: event gán Height hoặc Size height200 phải thắng request120 và còn hiệu lực sau roundtrip; đối chiếu native peer. Bổ sung derived override gán trước base.OnHandleCreated và request trong chiều Simple→DropDown, rồi quay về Simple | `6861ff6`, hoàn thiện `aad1333` |
| [Bitmap layer scale theo DPI đích](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/pull/3#discussion_r4408866370) | Public outline BootstrapButton render qua inherited InvokePaint vào Graphics PageUnit.Pixel có resolution gấp 2×/2× hoặc 2×/1.5× probe bitmap; kiểm cả bốn cạnh và tâm khi radius2→1, border2 | `c795401` |

ComboBox restore saved Simple height trước `base.OnHandleCreated`, tức trước event của caller và trước native restore height. Mọi yêu cầu Height rõ ràng đi qua override bounds được ghi nhận, kể cả lúc style đang chuyển; không bỏ qua toàn bộ giai đoạn recreate bằng điều kiện style-equality. Metric writes nội bộ dùng `base.SetBoundsCore` hoặc `_applyingSizeMetrics`, còn layout thông thường dùng `BoundsSpecified.None`. Không thêm reflection vào private WinForms state hoặc tạo handle trong constructor.

Thứ tự native được đối chiếu với [ComboBox .NET 8 v8.0.0](https://github.com/dotnet/winforms/blob/v8.0.0/src/System.Windows.Forms/src/System/Windows/Forms/ComboBox.cs): setter đổi style, recreate handle rồi phát DropDownStyleChanged; OnHandleCreated phát event trước khi restore requested Simple height. Native-peer regressions kiểm runtime trên cả net48 và net8.0-windows.

Painter giữ complementary coverage và một lần alpha composite. Chỉ thay lệnh vẽ layer bằng destination rectangle và source rectangle `GraphicsUnit.Pixel` tường minh, để geometry đã tính theo bounds không bị scale theo physical resolution của bitmap. Đây là regression **render/export bitmap khác DPI**, không chứng minh physical desktop 125% hoặc quy kết DrawToBitmap thông thường bị lỗi.

## RED → GREEN và review bổ sung

- `37612be`: 4 cases mới, executed 4/expected RED 4/PASS 0/skip 0 mỗi TFM. Callback Height/Size 200 nhận 120; pixel cạnh bitmap export lệch màu so với vector baseline.
- `6861ff6`: nhóm ComboBox và regressions cũ PASS 79/79 mỗi TFM. `c795401`: focused PASS 101/101 mỗi TFM; full trung gian PASS 5416 executions (Controls 2452 + SourceGrid 256 trên mỗi TFM).
- Review source độc lập chỉ ra derived override gán Height trước base vẫn bị guard style bỏ qua. Bổ sung cả reverse-style callback trong `05fadd0`: executed 2/expected RED 2/PASS 0/skip 0 mỗi TFM trên source `c795401`.
- `aad1333`: focused PASS 103/103 mỗi TFM, gồm 6 cases vòng ba, 11 vòng hai, 10 vòng một, ComboBox, rounded geometry/painter và input contour regressions.
- Review source chỉ đọc trên range `803888a..aad1333` xác nhận finding derived override đã sửa, không thấy defect đáng kể mới. Reviewer phụ không chạy GUI/tests và không đưa kết luận về physical DPI, Owner hoặc consumer acceptance.
- Full trung gian không thay kết quả source cuối; phần dưới ghi verification riêng trên `aad1333`.

## Verification cuối

```powershell
.\build.ps1 -Configuration Release -Scope All
.\test.ps1 -Configuration Release -Scope All -Framework All -SkipBuild
```

Build All **PASS**, 0 warnings/errors. Cả hai owners chạy tuần tự trên cùng source/test revision `aad1333` sau build.

| Gate | net48 discovered | net48 executed/PASS | net8 discovered | net8 executed/PASS | Fail/skip cuối |
|---|---:|---:|---:|---:|---|
| Full Controls | 2456 | 2454 | 2456 | 2454 | 0/0 theo runner summary; 2 Explicit chạy riêng |
| Full SourceGrid | 256 | 256 | 256 | 256 | 0/0 |
| Focused | 103 | 103 | 103 | 103 | 0/0 |
| Regressions ba vòng | 27 | 27 | 27 | 27 | 0/0 |
| Explicit first-click | 2 | 2 | 2 | 2 | 0/0 |
| Displayed-popup capture | 1 | 1 | 1 | 1 | 0/0 |

Tổng full owners **5420 executions**, không cộng trùng focused/native reruns. Discovery chạy `--list-tests` trên đúng project/framework. Hai cases Explicit được broad run báo không chạy nhưng không tính trong tổng executed; chúng có lượt chạy riêng 2/2 PASS mỗi TFM.

Exact focused/native commands sau build, trên từng TFM:

```powershell
$controlsProject = 'tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj'
foreach ($targetFramework in @('net48', 'net8.0-windows')) {
    dotnet test $controlsProject -c Release -f $targetFramework --no-build --no-restore --list-tests --blame-hang --blame-hang-timeout 5m
    dotnet test tests/MyDmsVn.BootstrapSourceGrid.Tests/MyDmsVn.BootstrapSourceGrid.Tests.csproj -c Release -f $targetFramework --no-build --no-restore --list-tests --blame-hang --blame-hang-timeout 5m
    foreach ($filter in @(
        'FullyQualifiedName~InputPopupRoundThreeRegressionTests|FullyQualifiedName~InputPopupRoundTwoRegressionTests|FullyQualifiedName~InputPopupReviewRegressionTests',
        'FullyQualifiedName~NativeFirstClickFromAnotherControlKeepsPopupAndSearchFocus',
        'FullyQualifiedName~BootstrapOverlayNativeVisualRegressionTests'
    )) {
        dotnet test $controlsProject -c Release -f $targetFramework --no-build --no-restore --filter $filter --blame-hang --blame-hang-timeout 5m
    }
}
```

Nhóm focused 103 dùng regression filter trên cộng `BootstrapComboBox`, `BootstrapRoundedSurface`, `BootstrapInputShellContourTests`. Các fixture giữ STA/nonparallel, message pumping hữu hạn, hang diagnostics 5 phút và không modal waits.

## Native evidence và giới hạn

Bitmap export khác resolution là raster evidence. Các Simple/layout tests mở controls thật khi đến bước Show; native visual gate mở Select/Lookup Light/Dark, chụp CopyFromScreen với exterior band và kiểm RGB oracle. Raw capture của source cuối được tạo thêm bằng host:

```powershell
foreach ($targetFramework in @('net48', 'net8.0-windows')) {
    $hostExe = Join-Path (Get-Location) "tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/bin/Release/$targetFramework/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe"
    $outputDirectory = Join-Path (Get-Location) "artifacts/verification/20261029-pr3-round-three/aad1333-$targetFramework-96dpi"
    New-Item -ItemType Directory -Force $outputDirectory | Out-Null
    $captureProcess = Start-Process -FilePath $hostExe -ArgumentList @('--scenario', 'rounded-popup', "`"$outputDirectory`"") -WindowStyle Hidden -PassThru -RedirectStandardError "artifacts/round-three-capture-$targetFramework.log"
    if (-not $captureProcess.WaitForExit(30000)) { $captureProcess.Kill(); throw 'Capture host timed out' }
    if ($captureProcess.ExitCode -ne 0) { throw 'Capture host failed' }
}
```

Mỗi TFM có 4 PNG và 4 metadata files tại local ignored paths bên trên; đây là captures riêng của Select/Lookup Light/Dark, không tính thành NUnit cases bổ sung. Cả 8 metadata files ghi owner/popup DPI **96**, thread/popup context **PerMonitorV2**, edge-continuity/corner-symmetry/shell-band/content-containment đều **pass**. PNG/TRX/logs không commit vào Git và chưa upload thành durable PR/CI artifacts. Source/bitmap hoặc ảnh lịch sử không thay displayed-popup evidence của candidate mới.

`git diff --check`, relative-link file checks và UTF-8 không BOM cho 8 Markdown files đổi trong vòng này đều PASS. Candidate mới chỉ có source/test fixes cùng contracts/evidence liên quan; không thay Region/content host ownership, native ComboBox model hoặc radius-only navigation refresh.

## Các nhóm nghiệm thu tiếp tục còn mở

- Physical 125% Light/Dark cho input row/Select/Lookup và representative higher scales: **NOT RUN**. Bitmap resolution tests phía trên không thay gate này.
- Owner visual decision trên candidate mới: **PENDING**, chưa APPROVED/CLOSED.
- Consumer MyDmsVn: **NOT RUN**. Handoff: pin exact commit chứa report này, kiểm SecurityPermissionCatalogView/catalog UI tests, màn hình Select/Lookup và filter một/hai dòng ở physical 100%/125%; ghi candidate, commands và evidence mới.

Giữ PR Draft và review threads để reviewer kiểm lại. Không đổi lịch sử A3/Business Compact hoặc publish package.
