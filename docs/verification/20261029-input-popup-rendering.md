# Kiểm chứng chiều cao input và góc bo popup — 2026-10-29

## Candidate và phạm vi

- Nhánh: `codex/plan-input-popup-rendering-20261029`.
- Điểm bắt đầu plan: `0643770`.
- Source implementation đã kiểm: `372eec221d03ee30b8a1362a4f9f27dd6fd4b4d1` cộng thay đổi test/docs T07 trong commit chứa report này.
- Candidate để consumer pin là commit chứa report này; lấy exact SHA bằng `git rev-parse HEAD` sau commit T07. Không dùng riêng `372eec2` làm consumer pin vì commit đó chưa chứa reviewed API fingerprint và evidence cuối.
- SourceGrid gitlink: `f4e457b43582bf01892f50bdc74aa480531e5944`, working tree sạch.
- Configuration: `Release`; targets: `net48`, `net8.0-windows`; restore dùng cache hiện có và locked graph, không phải clean-cache claim.

T01–T06 đã triển khai theo thứ tự và commit riêng. T07 đã chạy build/test/native capture và chuẩn bị handoff; physical 125% trở lên, quyết định hình ảnh của Owner và nghiệm thu consumer vẫn được ghi riêng là chưa hoàn tất.

## Môi trường

| Thuộc tính | Giá trị |
|---|---|
| OS | Windows 11 Home Single Language, `10.0.26300`, build `26300`, x64 |
| .NET SDK | `8.0.427` (`global.json` cho phép latest feature từ baseline `8.0.400`) |
| Desktop | Interactive Windows desktop, `DISPLAY1`, 1920×1080 |
| Physical scale đã chạy | 100%; HWND/owner DPI `96`, `LOGPIXELSX=96`, `LOGPIXELSY=96` |
| DPI awareness của native host | `PerMonitorV2` qua app manifest; awareness được thiết lập trước khi tạo HWND |
| Physical scale chưa có | 125%, 150%, 175%, 200% — `NOT RUN`, máy chỉ có màn hình 96 DPI trong lần kiểm này |

## Kết quả theo task

| Task | Commit | Verification đã thực thi | Kết quả |
|---|---|---|---|
| T01 | `d0f2624` | Focused sizing/overlay/native baseline, mỗi TFM 26 cases | 23 PASS, 3 expected RED, 0 skip mỗi TFM; native capture/oracle PASS |
| T02 | `4700dd3` | Rounded geometry/painter, TextBox, connected controls | 56/56 PASS mỗi TFM |
| T03 | `d547f9e` | ComboBox sizing/lifecycle và native capture | 57/57 PASS mỗi TFM; constructor handle-free |
| T04 | `fba1eaa` | Rounded geometry, overlay, Popover và shown-popup capture | 107/107 PASS mỗi TFM |
| T05 | `1555584` | Select/Lookup popup, content, interaction, embedded grid | 117/117 PASS mỗi TFM |
| T06 | `372eec2` | Input contours và SourceGrid editors | Controls 69/69; adapters 73/73 PASS mỗi TFM |
| T07 | commit chứa report | Build All, full owners, focused discovery/run, explicit first-click, native desktop capture, docs/handoff | Toolkit gates đã chạy đều PASS; physical 125%+, Owner visual và consumer acceptance còn mở |

Regression radius-only của Select và Lookup đổi contour trên popup đang mở nhưng giữ cùng HWND. Test cố ý cuộn viewport ra khỏi highlighted row rồi xác nhận query, caret, committed value, highlighted/current row, scroll position và event counts không đổi. Luồng này chỉ refresh geometry/layout; không gọi selection reconciliation hoặc tự cuộn về highlighted row.

## Commands và case counts

### Build và full owner matrix

```powershell
.\build.ps1 -Configuration Release -Scope All
.\test.ps1 -Configuration Release -Scope All -Framework All -SkipBuild
```

- Build: PASS, 0 warnings, 0 errors.
- Controls owner: `2426/2426` PASS, 0 fail, 0 skip trên `net48`; `2426/2426` PASS, 0 fail, 0 skip trên `net8.0-windows`.
- SourceGrid owner: `256/256` PASS, 0 fail, 0 skip trên mỗi TFM.
- Tổng full owner executions: `5364`, 0 fail, 0 skip theo summaries. Hai cases `[Explicit]` first-click không nằm trong tổng broad run và được chạy riêng bên dưới.

Full run đầu tiên phát hiện `Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline` fail 1 case trên `net48`. Reconstructed export chỉ ra hai override có chủ đích của T03 (`BootstrapComboBox.GetPreferredSize(Size)` và protected `SetBoundsCore(...)`) đã được ghi trong component/API contract nhưng fingerprint chưa đổi. Sau review, fingerprint được cập nhật thành `1de84ca1b9a297e0caff576b4b18c0296ca17d0ebabf088be2f48213cad27755`; gate riêng PASS `15/15` mỗi TFM và full matrix phía trên được chạy lại từ build mới.

### Focused controls và explicit native click

Command dùng đúng `$controlsFilter` trong [plan](../plans/20261029-001-input-popup-rendering.md#focused-final-commands), có discovery trước run.

- Discovery: 433 cases mỗi TFM, gồm 2 cases `[Explicit]`.
- Broad focused run: 431/431 PASS mỗi TFM; 2 explicit cases không được broad runner thực thi.
- Exact explicit command:

```powershell
dotnet test $controlsProject -c Release -f $targetFramework --no-build --no-restore `
  --filter "FullyQualifiedName~NativeFirstClickFromAnotherControlKeepsPopupAndSearchFocus" `
  --blame-hang --blame-hang-timeout 5m
```

Kết quả: 2/2 PASS mỗi TFM, gồm content-click và arrow-click từ control khác. Đây là native `SendInput` gate trên desktop tương tác, không phải gọi handler trực tiếp.

### Focused SourceGrid adapters

Command dùng đúng `$gridFilter` trong plan, discovery trước run.

- `net48`: discovered 73, executed 73, PASS 73, fail 0, skip 0.
- `net8.0-windows`: discovered 73, executed 73, PASS 73, fail 0, skip 0.

### Native popup đang hiển thị

```powershell
dotnet test $controlsProject -c Release -f $targetFramework --no-build --no-restore `
  --filter "FullyQualifiedName~BootstrapOverlayNativeVisualRegressionTests" `
  --blame-hang --blame-hang-timeout 5m
```

- `net48`: 1/1 PASS.
- `net8.0-windows`: 1/1 PASS.
- Host mở thật Select và Lookup trong Light/Dark, đọc native popup bounds, rồi dùng `Graphics.CopyFromScreen` chụp rectangle gồm exterior band 12 px. Bốn capture mỗi run đều opaque, không blank và ghi `popup.visible=true`, `owner.dpi=96`.
- Đây là bằng chứng desktop composition ở 100%, khác với `DrawToBitmap`/geometry tests. Nó chưa phải Owner visual approval và không chứng minh physical 125%.

## Geometry/bitmap so với physical desktop

- Geometry/painter tests chạy synthetic DPI `96/120/144/168/192`; `HalfPixelPolicyKeepsCornerCoverageSymmetric` yêu cầu alpha thật `0 < A < 255` ở cả bốn góc, nên A=0/blank không được tính là anti-aliasing.
- Pairwise regressions giữ radius 0, tiny bounds/large radius, thick border/opaque child, resize/theme/reopen, caller Region/content padding và same-size Region refresh.
- Shell contour tests dùng bitmap/parent mask để so TextBox, NumericBox, DatePicker, Select và Button. Đây là geometry/bitmap evidence, không được báo thành popup HWND evidence.
- Native host phía trên là displayed-popup evidence ở physical 100% duy nhất trong lần chạy này.
- Physical 125%, 150%, 175%, 200%: `NOT RUN` vì không có màn hình/Windows scale tương ứng. Không có kết luận PASS cho các scale này.

## Visual artifacts

Raw PNG và metadata không commit vào Git theo policy. Artifact local có thể mở lại trong workspace này tại:

```text
artifacts/verification/20261029-input-popup-rendering/
  before-d0f2624-100pct/
  after-372eec2-100pct/
```

Mỗi set có bốn full PNG 1:1 (`light|dark` × `select|lookup`), bốn file metadata và thư mục `corners/` chứa top-left/top-right/bottom-left/bottom-right crop 64×64 cho từng PNG. Before và after cùng `owner.dpi=96`, cùng screen và cùng exterior band. Baseline `d0f2624` là T01 trước production fixes; after `372eec2` là source implementation sau T06.

Không có artifact URL bên ngoài hoặc GitHub Actions run cho candidate này vì task không yêu cầu push/upload. Consumer hoặc reviewer trên máy khác phải chạy lại native host bằng commands trên; local artifact path không phải durable CI storage.

## Handoff consumer MyDmsVn

Consumer acceptance: **NOT RUN** trong repository này và không được suy từ toolkit PASS.

Consumer PR kế tiếp cần:

1. pin exact commit chứa report này, không pin riêng parent `372eec2`;
2. kiểm `SecurityPermissionCatalogView` và catalog UI tests;
3. kiểm các màn hình thực dùng `BootstrapComboBox`, `BootstrapSelect` và `BootstrapLookupBox`;
4. kiểm filter một dòng/hai dòng ở physical 100% và 125%, Light/Dark, focus/invalid/disabled;
5. với Select/Lookup đang mở, cuộn ra khỏi highlight rồi đổi radius/theme; xác nhận query/caret/value/highlight/current row/scroll giữ nguyên;
6. không đưa business/permission/SQL abstraction vào toolkit PR.

## Trạng thái acceptance và phần còn lại

- Toolkit source/build/full/focused/native 100% gates: **PASS** trên hai TFM.
- Physical 125% trở lên: **NOT RUN**.
- Owner visual decision cho candidate: **PENDING**, không ghi APPROVED/CLOSED.
- Consumer MyDmsVn acceptance: **NOT RUN**.
- Public package/release: ngoài scope; không publish.

Vì các mục physical 125%, Owner và consumer còn mở, report này xác nhận implementation/toolkit verification chứ không đóng toàn bộ visual/consumer acceptance.
