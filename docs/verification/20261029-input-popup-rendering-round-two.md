# PR #3 — review vòng hai trên `632e8df`

## Candidate và phạm vi

- Reviewer đã phản hồi bằng [review 5692895404](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/pull/3#pullrequestreview-5692895404): xác nhận bốn tình huống vòng một đã sửa, nêu hai findings P2 mới trên `632e8df2eb97ff2c171f3b0c0ccbdea1592ba74d`.
- Source/test execution revision cuối: `713632ff01389442972289efad1285754684bf25`. Candidate để review/pin là commit chứa report hiện tại; sau execution chỉ đổi Markdown contracts/evidence/entry links.
- Môi trường: Windows 11 x64 `10.0.26300`, SDK `8.0.427`, Release, desktop tương tác actual DPI 96. Locked restore dùng cache hiện có, không phải clean-cache claim.
- SourceGrid gitlink giữ `f4e457b43582bf01892f50bdc74aa480531e5944`; hai owners/TFM, assembly identities và public API fingerprint không đổi.
- [Evidence vòng một](20261029-input-popup-rendering-review-fixes.md) và [evidence implementation](20261029-input-popup-rendering.md) là lịch sử riêng, không thay kết quả của candidate này.

## Findings và sửa source

| Finding | Regression Windows | Commit |
|---|---|---|
| [FlowLayout dùng cached specified height cũ](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/pull/3#discussion_r4408718042) | Layout trước handle rồi đổi ControlHeight 32→48 hoặc Font 10→20; panel phải chứa bottom+margin, không tạo HWND sớm, sau đó Simple vẫn dùng caller height 120 | `0d48bd9` |
| [Surface alpha bị composite hai lần](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/pull/3#discussion_r4408718051) | Bitmap transparent/white với radius1/border2 và radius4/border8; tâm phải bằng một SourceOver. Filled BootstrapButton Primary alpha128 không đổi màu tâm khi radius2→1 | `02d316a`, hoàn thiện `713632f` |

ComboBox cần hai kích thước độc lập: specified closed height mà non-AutoSize FlowLayout đo, và caller-requested height dành cho Simple. Private state ghi nhận yêu cầu Height của caller, bỏ qua metric/layout/style-transition callbacks, restore khi vào Simple; closed bounds/cache được đồng bộ qua protected APIs. Không đọc/ghi private WinForms fields, không tạo handle trong constructor.

Giá trị mặc định requested Simple height 150 được đối chiếu với [source ComboBox .NET 8 pin v8.0.0](https://github.com/dotnet/winforms/blob/v8.0.0/src/System.Windows.Forms/src/System/Windows/Forms/ComboBox.cs) và regression native peer trên cả hai TFM. `DefaultSize`/closed height không đại diện cho requested Simple height đó.

Painter: màu surface/border giống nhau được fill outer một lần. Với hai màu khác nhau, raster outer/inner masks được gộp theo coverage bổ sung thành một bitmap premultiplied, rồi composite một lần. Không double-composite interior hoặc làm hụt coverage ở mép chung. Bitmaps, masks, paths, brushes và locks do painter tạo đều được dispose/unlock; Graphics state caller được save/restore.

## RED → GREEN và review bổ sung

- `128151e`: 7 new cases, executed 7/expected RED 7/PASS 0/skip 0 trên mỗi TFM. Flow chỉ cấp 38 px trong khi cần 54/56; translucent center alpha192 thay128, RGB trên white đậm hơn một lần composite.
- Sửa đầu cho nhóm focused 93/93 PASS mỗi TFM, nhưng source/raster review phát hiện thêm seam tại fractional border2.5/radius1.25 ở cách fill band/inner riêng.
- Hai seam cases mới RED trên net48 (opaque/translucent). Thêm hai cases khác màu kiểm alpha/midpoint ở ranh giới coverage; tổng fixture vòng hai là 11 cases.
- `713632f`: focused 97/97 PASS mỗi TFM, gồm 11 cases vòng hai, 10 cases vòng một, ComboBox, rounded geometry/painter và input contour regressions.
- Reviewer phụ kiểm source và probe bitmap production helper net48 sau sửa: opaque red ở mép/tâm đều `(255,0,0)`; alpha128 red trên white ở mép/tâm đều `(255,127,127)`; red/blue alpha128 ở mép là `(191,127,191)`. Không còn finding trong diff này. Probe này không tạo HWND và không thay native/physical acceptance.
- Full run trung gian trên `02d316a` PASS 5400 executions nhưng bị supersede sau sửa seam. Full run cuối trên `713632f` ghi riêng bên dưới.

## Verification cuối

```powershell
.\build.ps1 -Configuration Release -Scope All
.\test.ps1 -Configuration Release -Scope All -Framework All -SkipBuild
```

Build All **PASS**, 0 warnings/errors. Cả hai owners chạy tuần tự trên cùng source/test revision `713632f` sau build.

| Gate | net48 discovered | net48 executed/PASS | net8 discovered | net8 executed/PASS | Fail/skip cuối |
|---|---:|---:|---:|---:|---|
| Full Controls | 2450 | 2448 | 2450 | 2448 | 0/0 theo runner summary; 2 Explicit chạy riêng |
| Full SourceGrid | 256 | 256 | 256 | 256 | 0/0 |
| Regressions hai vòng | 21 | 21 | 21 | 21 | 0/0 |
| Explicit first-click | 2 | 2 | 2 | 2 | 0/0 |
| Displayed-popup capture | 1 | 1 | 1 | 1 | 0/0 |

Tổng full owners **5408 executions**, không cộng trùng focused/native reruns. Discovery chạy `--list-tests` trên đúng project/framework. Hai cases Explicit không được suy PASS từ broad run.

Exact focused/native commands sau build, trên từng TFM:

```powershell
$controlsProject = 'tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj'
foreach ($targetFramework in @('net48', 'net8.0-windows')) {
    foreach ($filter in @(
        'FullyQualifiedName~InputPopupRoundTwoRegressionTests|FullyQualifiedName~InputPopupReviewRegressionTests',
        'FullyQualifiedName~NativeFirstClickFromAnotherControlKeepsPopupAndSearchFocus',
        'FullyQualifiedName~BootstrapOverlayNativeVisualRegressionTests'
    )) {
        dotnet test $controlsProject -c Release -f $targetFramework --no-build --no-restore --filter $filter --blame-hang --blame-hang-timeout 5m
    }
}
```

Nhóm focused 97 dùng filter trên cộng `BootstrapComboBox`, `BootstrapRoundedSurface`, `BootstrapInputShellContourTests` nhưng không lấy explicit/native filters. Commands đều giữ hang diagnostics 5 phút, STA/nonparallel theo fixtures, không modal waits.

## Native evidence và giới hạn

Bitmap alpha/seam là raster evidence; input/layout tests tạo native controls thật khi đến bước Show. Native visual gate mở Select/Lookup Light/Dark, chụp `CopyFromScreen` với exterior band và RGB oracle. Actual owner/popup DPI **96**, thread/popup contexts **PerMonitorV2** trên cả hai targets.

Raw captures mới (4 full PNG và 4 metadata files mỗi target) ở local ignored path:

```text
artifacts/verification/20261029-pr3-round-two/
  713632f-net48-96dpi/
  713632f-net8.0-windows-96dpi/
```

PNG/TRX/logs không commit vào Git và chưa upload thành durable PR/CI artifact. Không dùng synthetic fractional metrics để báo physical 125% PASS; không dùng ảnh trước review để nghiệm thu candidate mới.

## Các nhóm nghiệm thu tiếp tục còn mở

- Physical 125% Light/Dark cho input row/Select/Lookup và representative higher scales: **NOT RUN**.
- Owner visual decision trên candidate mới: **PENDING**, chưa APPROVED/CLOSED.
- Consumer MyDmsVn: **NOT RUN**; cần pin exact candidate chứa report mới, kiểm SecurityPermissionCatalogView/catalog UI tests, màn hình Select/Lookup và filter một/hai dòng ở physical 100%/125%.

Các findings source đã có sửa/test không đồng nghĩa ba nhóm trên đã hoàn tất. Giữ PR Draft và thread để reviewer kiểm lại; không đổi lịch sử A3/Business Compact hoặc publish package.
