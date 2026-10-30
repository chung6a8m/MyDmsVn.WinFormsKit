# UI Composition Recipes — candidate evidence

## Candidate và phạm vi

- Ngày 30/10/2026; baseline toolkit `7a1467f` (activation trên `2521dfe`), nhánh `codex/composition-recipes-20261030`.
- CMP-T01 tạo UIC-01, route Compositions, focused tests và guide. CMP-T02 có record riêng bên dưới khi tích hợp.
- Runtime MyDmsVn vẫn pin `433e5b53f979e37cc6b69bad6dfcde0975c7ecaf`; không thay production core API, packages/projects, SourceGrid hoặc product source.
- UIC-01 source/test `4bbdeb56f40ead9017098eef787ff605fafeebc4`; integrated source/test `4c4466dca9f8e543119ee9608d509a4fadb104c0`. Commits local, chưa publish; baseline không phải recipe revision.
- Owner acceptance **PENDING**. Tests/source links không cung cấp visual acceptance hoặc app acceptance.

## CMP-T01 — focused automation

Project: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj`; configuration Release, filter `FullyQualifiedName~CompositionDemoFormTests`, STA/NonParallelizable, global theme khôi phục trong teardown. Native unexpected exceptions dùng môi trường test hiện hữu; grid test có `DataGridViewTestGuard`.

Commands thực chạy từ root toolkit:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~CompositionDemoFormTests --logger 'trx;LogFileName=uic01-red-net48.trx' --results-directory artifacts/verification/compositions --blame-hang --blame-hang-timeout 5m
# Sau implementation, thêm --no-restore, đổi LogFileName uic01-green-net48.trx.
# Target còn lại: -f net8.0-windows, LogFileName uic01-green-net8.trx.
```

| Run | Discovered/executed | PASS | FAIL | SKIP | Kết quả |
|---|---:|---:|---:|---:|---|
| RED net48 trước source mẫu | 3 | 0 | 3 | 0 | Expected assertion: không có `CompositionListDemoForm`, không phải build/environment failure |
| GREEN net48 sau source và thêm native test | 4 | 4 | 0 | 0 | PASS, 2s test duration |
| GREEN net8.0-windows | 4 | 4 | 0 | 0 | PASS, 1s test duration |

Cases:

- `FilterResults_QueryChangesRowsCountWithoutResettingSearchBuffer`: search/status → 2/4; giữ raw text `  Kho  `, selection3/2 và focus; read-only; unavailable không tổng0.
- `FilterResults_ClearAndEmptyStatesAreDistinct`: clear, empty0/0 khác no-match0/4, loading count chưa biết, query không có command ghi.
- `FilterResults_NativePerCharacterEnterAndTabKeepQueryState`: `SendKeys.SendWait` từng K/h/o trên native editor đang focus, mỗi phím quan sát text/caret/range/focus/3rows/summary; Enter giữ query, Tab tới status và Shift+Tab về search. Không gán whole-string để thay per-key proof.
- `FilterResults_ThemeNarrowLayoutAndPageDisposalPreserveOwnership`: 480×430 client, Dark giữ control/input/selected key, grid còn diện tích; MainForm đổi page dispose child, đổi theme sau dispose không callback lên sample cũ.

Raw TRX/logs chỉ giữ local tại `artifacts/verification/compositions`. Đây là focused test scope của sample/route, không full toolkit suite hoặc SQL. Các native Gallery gates physical100/125, hai TFM, Light/Dark và viewport cụ thể chưa được record bởi automation trên; root review sẽ bổ sung quan sát thực. Không chuyển kết quả cũ từ TAB/BUS/input-popup sang mẫu mới.

## CMP-T02 — focused automation

Project/configuration như CMP-T01; fixture riêng `CompositionEditDemoFormTests`, STA/NonParallelizable và restore global theme. Tách fixture theo form để giữ ownership rõ; không thêm project/harness.

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --no-restore --list-tests --filter FullyQualifiedName~CompositionEditDemoFormTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --no-restore --filter FullyQualifiedName~CompositionEditDemoFormTests --logger 'trx;LogFileName=uic02-red.trx' --results-directory TestResults/composition-uic02-red --blame-hang --blame-hang-timeout 5m
# GREEN: kết quả TestResults/composition-uic02-green-net48/uic02-green-net48.trx;
# net8.0-windows: discovery 5 và TestResults/composition-uic02-green-net8/uic02-green-net8.trx.
```

| Run | Discovered/executed | PASS | FAIL | SKIP | Kết quả |
|---|---:|---:|---:|---:|---|
| RED net48 trước source mẫu | 5 | 0 | 5 | 0 | Expected missing `CompositionEditDemoForm` assertion |
| GREEN net48 | 5 | 5 | 0 | 0 | PASS, 2s test duration |
| GREEN net8.0-windows | 5 | 5 | 0 | 0 | PASS, 1s test duration |

Cases: invalid Save giữ draft, không tăng feedback count và focus lỗi đầu; correct field gỡ đúng lỗi; valid Save lưu đủ năm trường đúng một lần; invalid tiếp rồi Cancel phục hồi committed snapshot; theme chuyển từ mode đối diện sang Light/Dark giữ draft/native handle/range/group selection; client360×320 giữ commands trong viewport và scroll fields; dispose ngừng global callback; native từng phím ABC, Tab/Shift+Tab và Save/edit/Cancel. Hai `TestCase` Light/Dark là hai rows thực thi trong tổng5, không tính thành một case.

## Integration / phạm vi bổ sung

Đăng ký thêm Compositions ảnh hưởng catalog routes của `IntegratedDemoApplicationTests`: run riêng test `Phase14MainDemoUsesFrameworkSidebarWithAllRequiredPages` cho 1 expected FAIL vì route mới thừa so với danh sách26 cũ. Đã cập nhật danh sách27 routes và giữ Theme là trang mở đầu; test composition kiểm cả hai tabs cùng được tạo/dispose khi chuyển page. Selected regression chỉ mở rộng tới host hiện hữu bị ảnh hưởng, không toàn toolkit/SQL.

## Native Gallery / quyết định

Ngày 30/10/2026 chạy actual Gallery EXE `--composition-proof` trên Windows 10.0.26300, x64, .NET Framework 4.8.10008.0 và .NET 8.0.33, SDK 8.0.427. Hai lượt exit0, mỗi lượt tạo 14 ảnh Light/Dark cho hai tab, normal/small và no-match/unavailable/invalid. `DeviceDpi=96`, `GetDpiForWindow=96` ở màn hình 1920×1080, work area 1920×1032. Outer 1280×800 → client 1264×761, content 1006×641; outer 900×600 → client 884×561, content 626×441. Ảnh và metadata local ở `artifacts/verification/compositions/gallery-net48/` và `gallery-net8.0-windows/`; không commit binary/raw logs. Ảnh là capture, không thay attended native/Owner review.

Quan sát bốn ảnh đại diện: small UIC-01 loaded và unavailable, small UIC-02 invalid, normal UIC-02 Dark; filter wrap, thông báo chưa biết count, errors nhiều dòng và Save/Cancel nhìn thấy; grid cuộn ngang cho mã dài. Các ô ảnh khác được tạo nhưng chưa được kiểm riêng. Automation native per-key có trong focused tests; chưa có bằng chứng tương ứng ở physical125. Gallery 125% **NOT RUN**: desktop hiện tại 96 DPI, Windows Settings không cung cấp cửa sổ điều khiển được. Owner acceptance **PENDING**; không suy PASS từ capture.

Integrated regression trên source candidate `4c4466d`: filter `FullyQualifiedName~CompositionDemoFormTests|FullyQualifiedName~CompositionEditDemoFormTests|FullyQualifiedName~IntegratedDemoApplicationTests`, Release, `--no-restore`, `--blame-hang --blame-hang-timeout 5m`, TRX local `artifacts/verification/compositions/final-<TFM>.trx`: net48 **23 passed / 0 failed / 0 skipped** (10s); net8.0-windows **23 / 0 / 0** (9s). Includes route catalog và disposal hai tab. Gallery proof build hai TFM 0 warning/0 error. EXE SHA256 net48 `858F18AF85BEE0B031D460AD16879F01D80AB779372423BC3626DEFA6756BA35`; net8 apphost `6068A8A97425F9B2D4F33840C9FA71F2CC08F917262376503F72F7F865763A80` (net8 managed assembly cũng cần giữ cùng build).
