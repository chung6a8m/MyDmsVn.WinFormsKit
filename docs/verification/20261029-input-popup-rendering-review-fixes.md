# PR #3 — kiểm chứng sửa bốn findings

## Candidate và môi trường

- Review đầu vào: [review 5692668259](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/pull/3#pullrequestreview-5692668259), tại `66c5e035c127d60cdcca780a661e1ca69d01f952`.
- Source/test execution revision: `42b0e49782438f571689a9e8ed51213b8033a3ae`. Candidate để review/pin là commit chứa report này; các thay đổi sau execution chỉ là contracts/evidence/entry links.
- Windows 11 x64 `10.0.26300`, SDK `8.0.427`, Release, desktop tương tác ở actual DPI 96. Restore locked dùng cache hiện có.
- SourceGrid gitlink giữ `f4e457b43582bf01892f50bdc74aa480531e5944`; public API fingerprint và assembly identities không đổi.
- [Evidence implementation trước review](20261029-input-popup-rendering.md) giữ nguyên như lịch sử của candidate trước, không dùng PASS cũ thay cho lượt chạy này.

## Findings và bản sửa

| Finding | Commit sửa | Regression/quan sát |
|---|---|---|
| [Scroll cuối bị clamp bởi layout trung gian](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/pull/3#discussion_r4408538943) | `29d964c`, bổ sung `42b0e49` | Radius 32→0, 30 rows, popup không chạm height cap: giữ offset 704 và dòng cuối sau layout cuối; cùng HWND/highlight, không SelectionChanged hoặc NearEndRequested |
| [Thick stroke làm vuông outer contour](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/pull/3#discussion_r4408538949) | `028cbe3` | Border 1/8, radius 4 và mixed corners; kiểm alpha ngoài circle độc lập ở bốn góc trên bitmap transparent, square corner vẫn square |
| [Closed sizing ghi đè requested Simple height](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/pull/3#discussion_r4408538956) | `b20bd7b` | Size trước style, không gán Size riêng, đổi width/location, live Simple→DropDown→Simple; so native peer/default height và giữ handle-free construction |
| [Preferred-size fallback trả viewport rỗng](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/pull/3#discussion_r4408538960) | `23fcb15` | Child 2×40 và 40×2, border 6/radius 0/padding 0, surface và Popover thật; đo lặp ổn định và actual content viewport đủ chỗ |

Reference extent của fallback lớn hơn cả requested diameter và hai lần border+guard. Khi fallback lớn lên, radius không còn normalize và inset giữ nguyên; child được đo lại với inset ổn định đó, rồi cấp đủ outer bounds. Không thêm fail-fast production.

ComboBox chuyển nguyên height/BoundsSpecified của caller xuống native base, chỉ dùng projected closed bounds trước handle; không thay native requested height bằng toolkit target. Không thêm reflection production hay cache height thứ hai.

Review source bổ sung kiểm đến `42b0e49` không còn finding trong phạm vi sửa. Reviewer phụ phát hiện restore scroll ban đầu còn gọi paging notification: regression counter RED (actual 1, expected 0), sau sửa chỉ restore dùng `notifyNearEnd: false`; scrolling thường giữ thông báo mặc định.

## RED → GREEN và các lần fail

- Commit regression baseline `dab345f`: mỗi TFM discovered/executed 10, PASS 2, expected RED 8, skip 0. Cả bốn findings tái hiện trên Windows, không chỉ suy từ source.
- Sau sửa: regression cuối mỗi TFM discovered/executed/PASS 10, fail/skip 0. Tests được tăng coverage all-corner và Popover trong lượt sửa; không xóa regression cũ.
- Focused từng nhóm trên cả hai TFM: Select 21/21, painter/geometry/contour 24/24, ComboBox 59/59, overlay/Popover+narrow-content 93/93.
- Full run trung gian tại `23fcb15`: net48 2437 PASS; net8.0-windows 2436 PASS/1 FAIL ở `ClipboardCopyAndCutUsePendingLexicalSelectionIncludingInvalidCharacters` (clipboard trả empty thay `1234`), runner dừng trước grid. Không có bằng chứng quy nguyên nhân clipboard cho bốn findings. Case này không sửa; chạy riêng cùng new regressions PASS 11/11 mỗi TFM, rồi full run tại `42b0e49` PASS như bảng dưới. Lần fail được giữ trong lịch sử, không tính thành PASS.

## Verification cuối trên `42b0e49`

```powershell
.\build.ps1 -Configuration Release -Scope All
.\test.ps1 -Configuration Release -Scope All -Framework All -SkipBuild
```

Build All: **PASS**, 0 warnings/errors. Full suite chạy tuần tự trên cùng source/test revision sau build, không có thay đổi code sau run.

| Owner/gate | net48 discovered | net48 executed/PASS | net8 discovered | net8 executed/PASS | Fail/skip cuối |
|---|---:|---:|---:|---:|---|
| Controls full | 2439 | 2437 | 2439 | 2437 | 0/0 theo runner summary; 2 Explicit chạy riêng |
| SourceGrid full | 256 | 256 | 256 | 256 | 0/0 |
| New review regressions | 10 | 10 | 10 | 10 | 0/0 |
| Explicit first-click | 2 | 2 | 2 | 2 | 0/0 |
| Displayed-popup capture | 1 | 1 | 1 | 1 | 0/0 |

Full owner total **5386 executions**. Focused/new/native reruns là executions bổ sung, không cộng trùng vào full total.

Discovery dùng `dotnet test ... --list-tests`; focused/native chạy sau final build với commands sau trên từng TFM:

```powershell
$controlsProject = 'tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj'
foreach ($targetFramework in @('net48', 'net8.0-windows')) {
    foreach ($filter in @(
        'FullyQualifiedName~InputPopupReviewRegressionTests',
        'FullyQualifiedName~NativeFirstClickFromAnotherControlKeepsPopupAndSearchFocus',
        'FullyQualifiedName~BootstrapOverlayNativeVisualRegressionTests'
    )) {
        dotnet test $controlsProject -c Release -f $targetFramework --no-build --no-restore --filter $filter --blame-hang --blame-hang-timeout 5m
    }
}
```

Bitmap/circle coverage là geometry/raster evidence. Scroll/Simple/Popover tests tạo controls/handles thật. Native visual gate mở Select/Lookup thật Light/Dark và `CopyFromScreen` gồm exterior band; RGB oracle, awareness assertions đều PASS. Capture metadata ghi actual owner/popup DPI 96, thread/popup `PerMonitorV2` trên cả hai targets.

Raw captures cuối ở local ignored path:

```text
artifacts/verification/20261029-pr3-review-fixes/
  42b0e49-net48-96dpi/
  42b0e49-net8.0-windows-96dpi/
```

Mỗi thư mục có 4 PNG full 1:1 và 4 metadata files. Raw logs/TRX/PNG không commit vào Git; paths chỉ mở được trên workspace này. Không dùng ảnh candidate trước làm visual acceptance của candidate mới.

## Nghiệm thu còn mở — giữ riêng

- Physical 125% Light/Dark cho input row + Select + Lookup và các scale representative cao hơn: **NOT RUN**. Synthetic metrics hay capture actual 96 DPI không thay physical-DPI gate.
- Owner visual decision trên candidate mới: **PENDING**, chưa APPROVED/CLOSED.
- Consumer MyDmsVn: **NOT RUN**. PR consumer cần pin exact candidate chứa report mới, kiểm SecurityPermissionCatalogView/catalog UI tests, màn hình Select/Lookup và filter một/hai dòng ở physical 100%/125%.
- Không đổi trạng thái lịch sử A3/Business Compact, không publish package hoặc sửa business/SQL.

Việc sửa findings và automated gates PASS không đóng ba nhóm nghiệm thu trên. Review threads được trả lời bằng commit/evidence để reviewer kiểm lại; không tự coi đó là Owner approval.
