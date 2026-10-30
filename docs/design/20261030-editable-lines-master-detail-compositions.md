# Editable Lines và Master–Detail Compositions

Ngày chọn scope: **2026-10-30**. Đây là thiết kế cho roadmap mở rộng đã được yêu cầu; các recipe mới **chưa được triển khai hoặc nghiệm thu**. [Plan toolkit](../plans/20261030-002-editable-lines-master-detail-recipes.md) sở hữu trạng thái CEX-K01–K03. [Plan MyDmsVn](https://github.com/chung6a8m/MyDmsVn/blob/main/docs/plans/20261030-002-editable-lines-sal-adoption.md) sở hữu inventory/compatibility, SAL pilot, Security mapping, quyết định helpers và handoff phía app. [Evidence](../verification/20261030-composition-expansion.md) ghi kết quả thực tế theo từng recipe/candidate.

## 1. Baseline và những gì đã có

Baseline nghiên cứu toolkit: `25b6c77a4d95d0df112aeba389d61a58ad0ee6db` trên `main`, sau PR #7. Đây là baseline đọc source, không phải runtime pin mới của MyDmsVn hoặc candidate native đã chạy cho scope này.

| Source tại baseline | Sự thật đã kiểm | Quyết định mở rộng |
|---|---|---|
| [UI_COMPOSITION_RECIPES](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/25b6c77a4d95d0df112aeba389d61a58ad0ee6db/docs/guides/UI_COMPOSITION_RECIPES.md#L1-L65) | UIC-01/UIC-02 là source mẫu; Business Compact và semantics sản phẩm thuộc app | Thêm UIC-03/UIC-04, không đổi acceptance lịch sử của hai mẫu cũ |
| [DataGridSelectEditingDemoForm](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/25b6c77a4d95d0df112aeba389d61a58ad0ee6db/samples/ControlGallery/DataGridSelectEditingDemoForm.cs#L55-L115) | Đã có nhiều dòng, lookup raw ProductId/display Name; quantity/price là DataGridViewTextBoxColumn có format | Không xây lại capability lookup/multirow; không trộn API DataGridView với SourceGrid |
| [GridGallery MainForm](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/25b6c77a4d95d0df112aeba389d61a58ad0ee6db/samples/GridGallery/MainForm.cs#L30-L53), [DemoGridContent](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/25b6c77a4d95d0df112aeba389d61a58ad0ee6db/samples/GridGallery/DemoGridContent.cs#L16-L72) | Shared text/formatted/lookup adapters trên nhiều rows đã có | UIC-03 chứng minh phối hợp draft, identity, commit, Apply/Restore bằng các editors hiện có |
| [BootstrapDecimalInputEditor](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/25b6c77a4d95d0df112aeba389d61a58ad0ee6db/src/MyDmsVn.BootstrapSourceGrid/Editors/BootstrapDecimalInputEditor.cs#L10-L80) | Decimal adapter/profile/pending parse đã implemented | Dùng `CreateDecimalInput(typeof(decimal), profile)`; không tạo adapter khác |
| [BootstrapLookupBoxEditor](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/25b6c77a4d95d0df112aeba389d61a58ad0ee6db/src/MyDmsVn.BootstrapSourceGrid/Editors/BootstrapLookupBoxEditor.cs#L39-L96) | `SelectedValue` là logical value; SourceGrid giữ commit/navigation | Display, derived fields và summary chỉ theo giá trị đã commit |
| [Grid tests project](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/25b6c77a4d95d0df112aeba389d61a58ad0ee6db/tests/MyDmsVn.BootstrapSourceGrid.Tests/MyDmsVn.BootstrapSourceGrid.Tests.csproj#L22-L25), [controls tests project](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/25b6c77a4d95d0df112aeba389d61a58ad0ee6db/tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj#L19-L23) | Mỗi test owner đã reference gallery tương ứng | Không cần ProjectReference, test suite hoặc helper executable mới |

[ARCHITECTURE](../ARCHITECTURE.md) và [EDITOR_REPLACEMENT](../components/grid/EDITOR_REPLACEMENT.md) tiếp tục là contracts nền. SourceGrid giữ storage, navigation, placement, edit lifecycle và final conversion; UI core giữ input/theme/focus visuals. Không sửa `vendor/sourcegrid`, production public API hay runtime gitlink trong scope được chọn.

## 2. Thứ tự và handoff

1. **CEX-A01** phía app bắt đầu inventory/compatibility ngay, song song CEX-K01.
2. **CEX-K01 / UIC-03 Editable Lines** ưu tiên trước. **CEX-A02 / SAL pilot** được mapping/preparation từ K01 source đã freeze và focused results trước final K03; completion của A02 phải đối chiếu handoff/limits riêng UIC-03 sau candidate review trong CEX-K03.
3. **CEX-K02 / UIC-04 Master–Detail** triển khai sau K01/feedback SAL sớm; có thể chuẩn bị source/test độc lập nếu không tranh shared files hoặc GUI session.
4. CEX-K03 có checkpoint riêng cho UIC-04, bàn giao cho **CEX-A03 / Security mapping**. SAL **không đợi UIC-04 hoặc toàn bộ CEX-K03 đóng**; chưa có native review thì ghi rõ mức evidence và không claim visual acceptance sớm.
5. **CEX-A04** phải đánh giá helper sau pilots; extraction chỉ làm khi có lặp lại thực và boundary rõ. **CEX-A05** khóa handoff cho context mới.

K03 là một task owner với hai kết quả handoff độc lập. Khi UIC-03 sẵn sàng và UIC-04 chưa xong, K03 có thể IN PROGRESS, checkpoint UIC-03 READY và checkpoint UIC-04 PENDING. Không suy trạng thái app từ trạng thái toolkit.

## 3. UIC-03 — Editable Lines

### 3.1 File và route đề xuất

Các symbol/file trong phần này là **đề xuất mới**, chưa có tại baseline:

- `samples/GridGallery/CompositionEditableLinesDemoForm.cs`: `public sealed class CompositionEditableLinesDemoForm : Form`, namespace `MyDmsVn.BootstrapSourceGrid.Demo`; constructor không nhận service/session.
- `tests/MyDmsVn.BootstrapSourceGrid.Tests/CompositionEditableLinesDemoFormTests.cs`: fixture của grid test owner.
- `samples/GridGallery/MainForm.cs`: thêm nút `Name = "editableLinesRecipeButton"`, text `"UIC-03 Editable Lines"`, mở form modeless có owner. MainForm đóng thì child được dispose; không tạo singleton hoặc giữ static form.

Giữ `MainForm`/`DemoGridContent` hiện hữu làm gallery editor tổng quát. Không chuyển Compositions host của ControlGallery sang tham chiếu SourceGrid. Không sửa các BUS scenario trong `GridGallery/Program.cs`.

### 3.2 Dữ liệu và ownership

Một form, một grid, ba dòng mẫu ban đầu; fake references gồm IDs `1, 2, 3` và nhãn tiếng Việt. Nested private `DemoLine` đủ dùng; không tạo schema/model/controller framework. Mỗi dòng có `Guid LineId` bất biến, `int ReferenceId`, `decimal Quantity`, `decimal UnitValue`, `bool IsMarked`, `bool IsLocked`. `LineId` sinh khi Add, giữ khi cell commit/Apply/Restore và không suy từ row index. Applied snapshot giữ cả thứ tự và các giá trị của dòng, gồm IsMarked. ReferenceId dùng sentinel `0` = chưa chọn; fake IDs hợp lệ chỉ `1, 2, 3`, registry nhận `typeof(int)` và Apply từ chối 0. Sentinel chỉ là contract demo, không chính sách null/key của product. Lookup DataSource có thêm placeholder ID 0, tên “Chưa chọn”; placeholder không phải reference hợp lệ và giúp cell/editor cùng giữ int 0. Đặt `lookupEditor.AllowNull = false` qua existing EditorBase contract; null candidate không được commit thành int. Không đổi declared cell type hoặc dùng nullable adapter chưa kiểm.

Các cột hiển thị: STT chỉ đọc; lookup ID và tên hiển thị; Quantity; UnitValue; DerivedValue = Quantity × UnitValue; checkbox IsMarked chỉnh sửa được; trạng thái IsLocked chỉ đọc. `Guid LineId` ở model/mapping, không chiếm một cột GUID trong luồng nhập. STT tính lại theo thứ tự hiện tại và không làm identity. `IsLocked` là kịch bản demo khóa chỉnh sửa, không đại diện authorization; lookup/numeric/checkbox của dòng locked đều không được đổi. Row sorting bị tắt trong recipe v1; add/remove và re-render vẫn phải giữ identity đúng. Dòng Add có ReferenceId `0`, Quantity `1m`, UnitValue `0m`, IsMarked `false`, unlocked; phải chọn reference trước Apply.

Checkbox dùng API hiện có `new SourceGrid.Cells.CheckBox(string.Empty, line.IsMarked)`, constructor `(string, bool?)` đã kiểm tại [engine pin, CheckBox.cs L51–65](https://github.com/chung6a8m/sourcegrid/blob/f4e457b43582bf01892f50bdc74aa480531e5944/SourceGrid/SourceGrid/Cells/CheckBox.cs#L51-L65). Đây là cell/controller sẵn có, không Bootstrap bool adapter hoặc WinForms checkbox control per-cell. Controller hiện hữu xử lý Space/click và lifecycle; xem [Controllers/CheckBox.cs L47–82](https://github.com/chung6a8m/sourcegrid/blob/f4e457b43582bf01892f50bdc74aa480531e5944/SourceGrid/SourceGrid/Cells/Controllers/CheckBox.cs#L47-L82), [L102–125](https://github.com/chung6a8m/sourcegrid/blob/f4e457b43582bf01892f50bdc74aa480531e5944/SourceGrid/SourceGrid/Cells/Controllers/CheckBox.cs#L102-L125). Dùng existing lock/admission contract, kiểm source model/editor khi implement; không tự viết checkbox lifecycle. Chuyển từ invalid numeric sang checkbox phải không mất pending value hoặc đổi bool lén; mixed keyboard/click path phải có test.

Hai decimal profiles demo dùng `vi-VN`, không nullable: Quantity scale `2`, range `0m..1000000m`; UnitValue scale `2`, range `0m..1000000000m`. Rule Apply minh họa yêu cầu reference hợp lệ và Quantity `> 0m`; đây không phải SAL validation, price, precision hoặc rounding policy. DerivedValue giữ decimal, chỉ format khi hiển thị. Không lấy text đã format để tính hoặc lưu.

Một lookup adapter và một decimal adapter cho mỗi column/profile, tạo qua registry của grid. Reuse trên các cells cùng grid; không tạo control per-cell hoặc share adapter cross-grid. Row metrics do form demo quyết định, tham khảo `DemoGridContent.ApplyTypographyLayout`; adapter không tự đổi row heights. Không đưa helper demo vào UI core.

### 3.3 Ba lớp state và lệnh

| Lớp state | Nội dung | Khi nào thay đổi |
|---|---|---|
| Pending cell input | Native text, selection, popup, candidate chưa hợp lệ | Typing/editor interaction; không phải committed draft |
| Committed local draft | Values engine đã commit, tập dòng và stable keys | Commit cell hợp lệ, Add/Remove hợp lệ |
| Applied snapshot | Bản sao đầy đủ lần Apply thành công gần nhất | Chỉ thay toàn bộ sau khi active edit kết thúc và tất cả dòng valid |

Lệnh đề xuất: Add, Remove selected, Apply, Restore. Footer và feedback ở ngoài vùng grid cuộn. Controls có names `editableLinesGrid`, `addLineButton`, `removeLineButton`, `applyLinesButton`, `restoreLinesButton`, `linesSummary`, `linesFeedback` để tests tìm qua control tree; không public test-only API.

Private methods đề xuất trong form: `bool TryFinishActiveEdit(bool cancel)`, `bool TryApplyDraft()`, `void RestoreAppliedSnapshot()`, `void AddLine()`, `void RemoveSelectedLine()`. Đây là implementation seams sample-only; dùng SourceGrid lifecycle đã kiểm, không ép `Text`/`SelectedValue` để giả commit.

- Add/Remove/Apply yêu cầu kết thúc active edit qua engine trước khi thay cấu trúc/snapshot. Commit fail thì giữ candidate, cell focus, số dòng và applied snapshot; chưa chạy thao tác tiếp theo.
- Remove không có selection thì không mutation; dòng locked không được Remove. Xóa dòng A rồi sửa dòng B phải tác động đúng LineId của B.
- Apply hợp lệ tăng feedback count đúng một lần, thay snapshot toàn bộ; Apply invalid không có partial applied snapshot. Các cell đã commit trước đó vẫn là local draft, không bị đánh tráo thành rollback database.
- Restore hủy active edit, bỏ pending candidate, phục hồi toàn bộ applied snapshot gồm dòng đã thêm/xóa, stable keys và giá trị IsMarked. Nếu không kết thúc được active edit, giữ state và báo lỗi; không rebuild controls phía sau editor còn sống. Đây là Restore cục bộ, không phải dirty-close/Save/Post.
- Escape/cancel cell theo SourceGrid; **không** tự restore toàn document. Decimal adapter cancel project lại **current model**, không giữ edit-start snapshot; contract có [test hiện hữu](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/25b6c77a4d95d0df112aeba389d61a58ad0ee6db/tests/MyDmsVn.BootstrapSourceGrid.Tests/BootstrapDecimalInputEditorTests.cs#L108-L120).
- Lookup display và DerivedValue/summary đọc committed draft. Pending lookup text không được đổi ID hoặc derived cells. Invalid numeric không trở thành `0`, không round để vượt validation, không swallowed exception.

### 3.4 Interaction và layout

Per-key typing, first character, Enter/Escape, Tab/Shift+Tab, popup open/closed và mouse selection dùng behavior của existing adapters. Nếu phát hiện lỗi integration, ghi reproducer và tách đề xuất runtime fix; không âm thầm mở rộng scope API. Theme switch không rebuild grid/editors, reset draft, popup hoặc native selection. Các global events do form subscribe phải unsubscribe khi dispose.

Normal client target `1000×650`, small client target `640×420` logical pixels. Grid dùng remaining space và horizontal scroll; toolbar wrap, footer luôn thấy. Không ép minimum form size lớn để né small viewport. Test geometry không thay native physical-DPI evidence. Read-only/derived/locked presentation cần rõ trong Light/Dark, không chỉ dựa màu. Feedback không đặt `AccessibleName` tĩnh che text thay đổi; giữ native focus/caret/accessibility.

## 4. UIC-04 — Master–Detail

### 4.1 File, route và dữ liệu đề xuất

- `samples/ControlGallery/CompositionMasterDetailDemoForm.cs`: `public sealed class CompositionMasterDetailDemoForm : DemoFormBase`, namespace hiện hữu của ControlGallery.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/CompositionMasterDetailDemoFormTests.cs`: fixture controls owner.
- Thêm `DemoPageSection("UIC-04 Master–Detail", () => new CompositionMasterDetailDemoForm())` trong route Compositions của `samples/ControlGallery/MainForm.cs`; giữ DemoPageHostForm hiện tại.

Master là flat list trung tính, **read-only**, có stable `int` key và mã/tên/nhóm. Detail read-only của key đang chọn; không tree, hierarchy, permission editor, CRUD hoặc SQL. Kịch bản fake data deterministic, không timer/async worker. Các states: NotSelected, Loading, Loaded, EmptyDetail, Unavailable; lọc hết master đưa detail về NotSelected. Loading/Unavailable không để dữ liệu stale của selection trước. Caption detail luôn có danh tính master khi đang có selection.

### 4.2 Chọn, lọc, refresh và layout

Master filter và detail đi cùng key, không dùng row index. Refresh/reorder fake dataset giữ selection nếu key còn tồn tại; nếu key mất thì clear detail. Điều khiển scenario ở ngoài cấu trúc sản phẩm, ghi rõ consumer bỏ nó. Detail lỗi/rỗng phân biệt với chưa chọn. Error/count không bịa `0` khi dữ liệu chưa biết. Không bổ sung server pagination/count contract.

Control names: `masterGrid`, `masterSearch`, `refreshMasterButton`, `detailTitle`, `detailBody`, `detailFeedback`, `masterDetailScenario`. Refresh button gọi `RefreshMasterData()`; scenario picker có lựa chọn fake dataset reordered/missing-selected-key cho lần refresh kế tiếp để kiểm stable selection qua UI mà không thêm public test API. Normal client target `1000×650`; small `480×430` logical pixels. Khi content width dưới `720` logical pixels, chuyển master/detail sang bố cục dọc với mỗi vùng có scroll phù hợp, không recreate controls hoặc mất selected key; phía rộng dùng hai vùng song song. AutoSize labels wrap tiếng Việt theo content width thực. Theme switch giữ raw search buffer, selection/focus/control identity. Route replacement/dispose gỡ global subscriptions và nguồn dữ liệu do form sở hữu.

Private methods đề xuất: `void ApplyMasterFilter()`, `void SelectMaster(int? key)`, `void RefreshMasterData()`, `void PresentSelectedDetail()`. Giữ state trực tiếp trong form/nested private records đủ dùng; không framework coordinator. Product Security dùng recipe như tham khảo trình bày; permission tree/session/service/dirty state tiếp tục thuộc app.

## 5. Ownership và phần không được kích hoạt cùng roadmap

| Nội dung | Owner / quyết định |
|---|---|
| Generic demos, state minh họa, native editor integration và tests tương ứng | Toolkit CEX-K01–K03 |
| SAL screen, DTO, culture/profile theo domain, totals/rounding, lookup requests, lifecycle, commands | MyDmsVn CEX-A01/A02 |
| Security mapping/master-detail sản phẩm | MyDmsVn CEX-A03 |
| Đánh giá helper production | MyDmsVn CEX-A04 bắt buộc đánh giá; chỉ extract phần lặp lại đã chứng minh |
| Runtime pin/API mới hoặc đổi SourceGrid | Chưa chọn; phải có bằng chứng compatibility/gap và scope riêng |
| Multirow paste, transaction/rollback database, Save/Post, authorization/session semantics | Thuộc app; không suy từ local Apply/Restore của demo |

Recipe source SHA và runtime toolkit SHA là hai trường handoff riêng. App kiểm API tại pin đang dùng; docs/sample change không tự buộc nâng gitlink. Không reference gallery assembly từ app hoặc chuyển toàn bộ sample helpers thành production framework. UIC-01/UIC-02, TAB/BUS và các acceptance lịch sử giữ owner/status cũ.

## 6. Acceptance theo từng recipe

Mỗi recipe cần source/tests phù hợp, nonempty discovery trên `net48` và `net8.0-windows`, build graph liên quan, Light/Dark × **physical 100%/125%** × normal/small content trên cả hai TFM. Ghi monitor scale và native/managed window DPI riêng; captures phải từ candidate đang chạy. Kiểm per-key/focus/keyboard/resize/disposal bằng host thật. Snapshot ảnh không chứng minh toàn bộ interaction hoặc screen-reader acceptance.

UIC-03 review thêm lookup popup, mixed lookup/numeric/checkbox keyboard và click, invalid numeric Apply/chuyển checkbox, Restore khi đang pending gồm bool, shared editor reuse, locked/derived cells, summary cập nhật sau commit. UIC-04 review thêm selection/filter/refresh mất key, long Vietnamese labels, stale detail clearing và responsive layout. Không yêu cầu chạy toàn bộ toolkit suite chỉ vì thêm mẫu; broaden khi diff hoặc finding chỉ ra risk thực. Không dùng historical PASS để lấp candidate mới.

K03 ghi từng recipe là READY chỉ khi implementation/tests và native review yêu cầu đã có evidence, đồng thời Owner decision của candidate được ghi rõ. Thiếu desktop/monitor/decision thì ghi NOT RUN/BLOCKED/PENDING đúng phần, không toàn diện hóa. Gate app runtime acceptance vẫn riêng. CEX-K03 không đợi UIC-04 để xuất handoff UIC-03.
