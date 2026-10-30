# UI Composition Recipes — candidate evidence

## Candidate và phạm vi

- Ngày 30/10/2026; baseline toolkit `7a1467f` (activation trên `2521dfe`), nhánh `codex/composition-recipes-20261030`.
- CMP-T01 tạo UIC-01, route Compositions, focused tests và guide. CMP-T02 có record riêng bên dưới khi tích hợp.
- Runtime MyDmsVn vẫn pin `433e5b53f979e37cc6b69bad6dfcde0975c7ecaf`; không thay production core API, packages/projects, SourceGrid hoặc product source.
- Exact source/test commit của mẫu được chốt trong handoff sau commit implementation; baseline không phải recipe revision.
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

## Native Gallery / quyết định

Đang chờ quan sát Gallery thật và metadata OS/runtime/bitness/DPI/client/content. Owner acceptance **PENDING**. Những gate chưa có record là **NOT RUN**, không PASS.
