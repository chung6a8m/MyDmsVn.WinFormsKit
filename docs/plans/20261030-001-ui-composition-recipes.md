# UI Composition Recipes — kế hoạch thực thi

> **For agentic workers:** triển khai từng task theo checkbox, đọc `AGENTS.md` và `docs/TESTING.md`; dùng `superpowers:subagent-driven-development` hoặc `superpowers:executing-plans` nếu có. Việc kích hoạt tài liệu không thay thế implementation, tests hoặc nghiệm thu.

**Trạng thái:** **COMPLETE — bounded composition recipes, 2026-10-30.** CMP-T01/CMP-T02 có samples, tests, route Compositions và recipe guide; focused Windows/native automation PASS trên hai TFM. Owner **APPROVED** candidate visual `1420f25` ngày 2026-10-30. Correction accessibility sau review ở source/test `bdac29b` giữ visual/layout không đổi, có focused automation riêng; không chuyển approval cũ thành approval mới hoặc AT live evidence. Candidate, historical decision và giới hạn được ghi riêng trong [candidate evidence](../verification/20261030-ui-composition-recipes.md).

**Mục tiêu:** tạo hai mẫu WinForms chạy được trong ControlGallery: UIC-01 Filter Results và UIC-02 Compact Edit Form, có hướng dẫn ngắn để consumer chuyển thể qua public APIs.

**Kiến trúc:** WinFormsKit sở hữu các mẫu trung tính, recipe contract, tests và evidence của Gallery. [Plan phối hợp trong MyDmsVn](https://github.com/chung6a8m/MyDmsVn/blob/main/docs/plans/20261030-001-business-compact-composition-adoption.md) sở hữu CMP-T03–CMP-T05: mapping Business Compact, repository skill, pilot sản phẩm và handoff hai repo. Không cần repository tài liệu thứ ba.

**Tech stack:** C#, WinForms; giữ `net48;net8.0-windows`; NUnit, test project và native helpers hiện hữu. Không thêm public production API, framework, package hoặc test project.

## 1. Baseline và owner trạng thái

| Nguồn | Revision đã đối chiếu cho kế hoạch ngày 2026-10-30 |
|---|---|
| WinFormsKit main | `2521dfe99461e8f6d1954da96ffaed0688027455` |
| MyDmsVn main | `16c24738a4758ccdceb1f3509f4e37adb0d03464` |
| Runtime gitlink `vendor/MyDmsVn.WinFormsKit` của MyDmsVn | `433e5b53f979e37cc6b69bad6dfcde0975c7ecaf` |
| Recipe source revision | UIC-01/UIC-02 sau correction accessibility: `bdac29bf4aca20db6618570ecd9a354630bc2e5f`; visual candidate được Owner duyệt vẫn là `1420f252ca198cd4409e0e945fa6433b30b943b3`; links cố định tại [guide](../guides/UI_COMPOSITION_RECIPES.md) |

Recipe source revision và runtime toolkit pin là hai trường độc lập. SHA baseline của toolkit ở trên chưa phải revision chứa hai mẫu mới. Consumer đọc/chuyển thể source mẫu nhưng chỉ dùng APIs đã có ở runtime pin của mình; không nâng gitlink chỉ để lấy source ví dụ. Nếu cần API/fix thật sự còn thiếu, ghi dependency follow-up riêng và tiếp tục phần không bị chặn.

Khi bắt đầu implementation, đọc lại HEAD, instructions và gitlinks; chỉ rà phần delta liên quan nếu baseline thay đổi. Source links pin ở mục 8 là căn cứ thiết kế; link plan app theo `main` dùng để điều hướng trạng thái phối hợp, không phải evidence hay recipe source baseline.

### Task ledger duy nhất của toolkit

| Task | Kết quả cần tạo | Phụ thuộc | Implementation | Windows/native verification | Owner acceptance |
|---|---|---|---|---|---|
| CMP-T01 | UIC-01 Filter Results, định dạng recipe và route Compositions | Baseline/contract của plan này | **IMPLEMENTED; correction accessibility `bdac29b`** | **Focused automation PASS hai TFM; physical100/125 captures, attended representative matrix PASS; correction 15/15 mỗi TFM** | **APPROVED visual `1420f25`, 2026-10-30; không phải approval correction/AT** |
| CMP-T02 | UIC-02 Compact Edit Form và recipe tương ứng | Contract recipe CMP-T01 | **IMPLEMENTED; correction accessibility `bdac29b`** | **Focused automation PASS hai TFM; physical100/125 captures, attended representative matrix PASS; correction 15/15 mỗi TFM** | **APPROVED visual `1420f25`, 2026-10-30; không phải approval correction/AT** |

Plan này là owner duy nhất của trạng thái CMP-T01/CMP-T02. Entry pages chỉ điều hướng; không duy trì ledger thứ hai. CMP-T03–CMP-T05 và tiến độ toàn đợt do plan app sở hữu. Approval chọn scope ngày 2026-10-30 không phải approval implementation hoặc native result. Không kế thừa PASS/ACCEPTED của TAB, BUS, input/popup, ba ngoại lệ Business Compact hay A3 cho hai mẫu mới.

## 2. Phạm vi đã chọn và ranh giới

- UIC-01 là danh sách chỉ đọc với search/status filters, summary và các trạng thái loaded, empty dataset, no matches, loading, error/unavailable. Không thêm Create/Edit/Delete, pagination mới hoặc permission workflow. Consumer cần phân trang đọc `PaginationDemoForm` và giữ pagination contract của mình.
- UIC-02 là form mã, tên, nhóm, trạng thái và ghi chú; validation minh họa, Save vào bản sao trong bộ nhớ, Cancel phục hồi snapshot vừa lưu. Không thêm modal flow, password hoặc async save engine.
- Dùng `DemoFormBase`, `DemoPageHostForm(params DemoPageSection[])`, factory `Func<Form>` và theme manager hiện hữu. `MainForm` có một mục “Compositions” chứa hai mẫu; không dựng host mới.
- Layout dùng `TableLayoutPanel`, `FlowLayoutPanel`, `Panel` và Dock; co giãn theo vùng nội dung thật. Không sao chép minimum width lớn từ component demo; không truy cập private children của composite control để ép màu/font/chiều cao.
- Dữ liệu giả xác định, trung tính, có nhãn tiếng Việt dài và mã dài. Không dùng product DTO, SQL, session, authorization, draft, Save/Post hoặc recovery của MyDmsVn.
- Các profile Default/Base14/Base16 tiếp tục là profile demo. Business Compact thuộc app; không chép palette/policy của sản phẩm vào toolkit.
- Hai form có constructor không tham số và có thể là public class trong assembly demo theo convention hiện hữu. Đây không phải public API của assembly UI core. Product không kế thừa `DemoFormBase`, không reference assembly ControlGallery.
- Giữ theme, DPI, native focus/caret/accessibility và ownership hiện hữu. Đổi theme giữ input, selection và control identity; dispose subscriptions/tài nguyên do mẫu tạo khi đóng/chuyển trang.
- Không sửa production toolkit chỉ để phục vụ mẫu. Một lỗi production được chứng minh cần được ghi nhận, review phạm vi và chọn tests riêng theo diff; không âm thầm mở rộng plan.
- Master-detail, SourceGrid inline editing, public layout helpers, áp dụng toàn SAL/SEC và sửa user-edit dialog nằm ngoài hai task này. Mapping, skill và pilot SecurityPermissionCatalogView thuộc app.

## 3. File map

Các files dưới đây đã được tạo trong candidate; task status và gates chỉ lấy từ ledger ở mục 1.

| File | Hành động và trách nhiệm |
|---|---|
| `samples/ControlGallery/MainForm.cs` | CMP-T01/T02: thêm mục “Compositions” bằng `DemoPageHostForm`/`DemoPageSection` hiện hữu |
| `samples/ControlGallery/CompositionListDemoForm.cs` | Đã tạo — CMP-T01: UIC-01, dữ liệu giả và trạng thái cục bộ |
| `samples/ControlGallery/CompositionEditDemoForm.cs` | Đã tạo — CMP-T02: UIC-02, validation và lưu/hủy cục bộ |
| `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/CompositionDemoFormTests.cs` | Đã tạo — tests UIC-01; UIC-02 nằm trong CompositionEditDemoFormTests.cs; STA, tuần tự, khôi phục theme |
| `docs/guides/UI_COMPOSITION_RECIPES.md` | Đã tạo — mục lục, contract và source links của UIC-01/UIC-02 |
| `docs/verification/20261030-ui-composition-recipes.md` | Đã tạo — candidate, commands/results thực tế, native observations và giới hạn |
| `docs/plans/20261030-001-ui-composition-recipes.md` | Tạo trong đợt kích hoạt; owner trạng thái CMP-T01/CMP-T02 |
| `docs/ROADMAP.md` | Kích hoạt đường vào plan này; không sao chép task ledger |
| `AGENTS.md`, `AI_CONTEXT.md`, `README.md`, `docs/README.md` | Kích hoạt đường đọc; bổ sung link guide sau khi guide thực sự tồn tại |

Không thêm project, package hoặc reference cho hai mẫu. Chỉ tách file dữ liệu chung nếu có nội dung thực sự dùng chung; tránh tạo abstraction trước nhu cầu.

## 4. Tasks

### CMP-T01 — UIC-01 Filter Results và định dạng recipe

**Owner:** WinFormsKit. **Depends on:** baseline/contract ở mục 1–2. Trạng thái lấy từ ledger ở mục 1.

**Files:** `CompositionListDemoForm.cs`, `MainForm.cs`, `CompositionDemoFormTests.cs`, `UI_COMPOSITION_RECIPES.md` và toolkit evidence theo file map.

**Interfaces:** consumes `DemoFormBase`, `MainForm` factory `Func<Form>`, `DemoPageHostForm(params DemoPageSection[])` và public controls/theme của UI core; produces `CompositionListDemoForm()` cùng recipe UIC-01. Stable control names: `SearchInput`, `StatusFilter`, `RefreshButton`, `ClearFiltersButton`, `ResultsGrid`, `ResultSummary`. Đặt `Name` để tests nhận diện hành vi/UI; không thêm service locator hay public test-only API.

**Nội dung mẫu:** header có tiêu đề và nhóm lệnh truy vấn; search/status/xóa bộ lọc wrap khi hẹp; `BootstrapDataGridView` chỉ đọc chiếm diện tích còn lại, có cột mã/tên/nhóm/trạng thái. Dữ liệu giả xác định có tiếng Việt dài và mã dài. Lựa chọn dữ liệu để minh họa loaded, empty dataset, no matches, loading và error/unavailable nằm ngoài vùng nội dung mẫu. Không thêm pagination; recipe dẫn tới `PaginationDemoForm` khi consumer có contract phù hợp.

- [x] Đọc lại baseline, ghi mapping controls/APIs của UIC-01 và chỉ rõ API nào có ở app runtime pin.
- [x] Viết `FilterResults_QueryChangesRowsCountWithoutResettingSearchBuffer`: thay search/status cho rows/count đúng, giữ raw text/caret/focus; unavailable không hiện tổng 0 như một kết quả thành công.
- [x] Viết `FilterResults_ClearAndEmptyStatesAreDistinct`: xóa filter phục hồi dữ liệu, empty dataset khác no matches, refresh/query không có hành vi ghi.
- [x] Chạy discovery và test mới trước implementation; xác nhận thất bại vì hành vi thiếu, không phải lỗi môi trường hoặc selector rỗng.
- [x] Implement `CompositionListDemoForm()` bằng layout primitives/public controls; đăng ký mục Compositions qua host hiện hữu.
  - [x] Chạy focused tests; kiểm native gõ từng phím, Enter, Tab/Shift+Tab, đổi bộ lọc và resize để chứng minh buffer/caret/focus cùng lệnh truy vấn.
- [x] Kiểm selected row/control identity khi đổi theme; chuyển trang và dispose không để mẫu cũ tiếp tục nhận callback do mình đăng ký.
  - [x] Ghi recipe: purpose, mô tả vùng, public controls/APIs, cách co giãn, states, ownership, source link tại commit và evidence thực tế; thêm link guide từ `docs/README.md` khi file đã có.
  - [x] Ghi results/limitations đúng candidate, review và commit riêng UIC-01 cùng tests/hướng dẫn.

**Done:** mẫu tương tác được trong Gallery thật, filter/summary nhất quán, dùng được ở content nhỏ của host, source recipe dễ tìm và các gate trong mục 6 có bằng chứng tương ứng.

### CMP-T02 — UIC-02 Compact Edit Form

**Owner:** WinFormsKit. **Depends on:** contract recipe CMP-T01. Trạng thái lấy từ ledger ở mục 1.

**Files:** `CompositionEditDemoForm.cs`, `MainForm.cs`, `CompositionDemoFormTests.cs`, `UI_COMPOSITION_RECIPES.md` và toolkit evidence theo file map.

**Interfaces:** consumes host/recipe contract CMP-T01 và public `BootstrapTextBox`, `BootstrapComboBox`, `BootstrapCheckBox`, `BootstrapButton`, `ValidationState`; produces `CompositionEditDemoForm()` và recipe UIC-02. Stable control names: `CodeInput`, `NameInput`, `GroupInput`, `NotesInput`, `SaveButton`, `CancelButton`, `ValidationSummary`, `SaveFeedback`.

**Nội dung mẫu:** các trường mã, tên, nhóm, trạng thái, ghi chú dùng dữ liệu trung tính. Rule minh họa mã/tên không trắng không phải validation contract của MyDmsVn. Field rows tăng theo label/error, nội dung cuộn cục bộ và footer luôn truy cập được. Save lưu bản sao vào state trong bộ nhớ, Cancel phục hồi snapshot đã lưu và xóa validation cũ. Inline errors, summary và feedback cung cấp nội dung `Text` hiện tại qua accessibility API, không dùng tên tĩnh; focus tới lỗi đầu tiên, sửa hợp lệ gỡ lỗi tương ứng. Đổi theme không dựng lại form hoặc mất input/selection.

- [x] Viết `EditForm_InvalidSubmitKeepsDraftAndDoesNotSave`: bỏ mã/tên rồi submit không tăng lần lưu, draft còn nguyên, errors đúng và native focus tới field đầu.
- [x] Viết `EditForm_ValidSaveAndCancelUseCommittedSnapshot`: save hợp lệ đúng một lần, sửa rồi Cancel phục hồi snapshot vừa lưu và xóa lỗi cũ.
- [x] Chạy discovery/test để xác nhận thất bại trước implementation vì hành vi thiếu.
- [x] Implement `CompositionEditDemoForm()` bằng layout primitives và public controls; giữ phần mẫu đủ ngắn để đọc trực tiếp, thêm trang qua host hiện hữu.
- [x] Chạy focused tests cùng `EditForm_ThemeAndNarrowLayoutPreserveInputAndReachableCommands`.
  - [x] Kiểm native chuỗi nhập sai → sửa → lưu → sửa tiếp → hủy; long labels, validation nhiều dòng, Tab/Shift+Tab, Light/Dark và content nhỏ.
- [x] Kiểm theme giữ draft/selection/control identity và đóng trang giải phóng subscriptions/tài nguyên do mẫu sở hữu.
- [x] Cập nhật recipe UIC-02 và evidence; ghi rõ chuyển thể thành panel/modal là trách nhiệm consumer.
  - [x] Review và commit riêng UIC-02; cập nhật ledger theo evidence thực tế.

**Done:** người xem chạy được toàn bộ chuỗi nhập/sửa/lưu/hủy, trạng thái và bố cục nhất quán, các gate trong mục 6 có bằng chứng tương ứng.

**Implementation disposition:** tests của UIC-02 nằm trong `CompositionEditDemoFormTests.cs`, tách khỏi `CompositionDemoFormTests.cs` theo ownership từng form. Không thêm test project. Native key sequence và layout/theme automation không tự đóng physical-DPI hoặc Owner gates.

## 5. Commands dự kiến và lựa chọn tests

Các lệnh dưới đây là recipe chạy lại trên checkout Windows có desktop tương tác; kết quả thực tế và configuration của candidate nằm trong evidence. Chỉ dùng selector sau khi source tồn tại và discovery thực tế không rỗng. Dùng NUnit project hiện hữu, STA/NonParallelizable, restore global theme trong teardown; native GUI tuần tự, finite pumping và hang diagnostics theo [TESTING](../TESTING.md).

```powershell
$kitTests = 'tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj'
dotnet restore MyDmsVn.WinFormsKit.Controls.sln --locked-mode
if ($LASTEXITCODE -ne 0) { throw 'Locked restore failed.' }

$recipeFilter = 'FullyQualifiedName~CompositionDemoFormTests|FullyQualifiedName~CompositionEditDemoFormTests'
foreach ($tfm in @('net48', 'net8.0-windows')) {
    dotnet test $kitTests -c Debug -f $tfm --no-restore --list-tests --filter $recipeFilter
    if ($LASTEXITCODE -ne 0) { throw 'Recipe test discovery failed.' }
    # Dừng để kiểm discovery thực tế trước khi cho phép lệnh chạy tiếp.
    # Exit code 0 của discovery không chứng minh đã tìm thấy hoặc đã chạy tests.
}
```

Sau khi kiểm tra và ghi lại discovery, chạy từng TFM tuần tự:

```powershell
foreach ($tfm in @('net48', 'net8.0-windows')) {
    $results = 'TestResults/composition-' + $tfm + '-' + [guid]::NewGuid().ToString('N')
    dotnet test $kitTests -c Debug -f $tfm --no-restore --filter $recipeFilter --blame-hang --blame-hang-timeout 5m --logger 'trx;LogFileName=composition.trx' --results-directory $results
    if ($LASTEXITCODE -ne 0) { throw 'Recipe tests failed.' }
}
```

Không suy số tests kỳ vọng từ plan. Report ghi exact source/test SHA, commands, discovered/executed/pass/fail/skip, môi trường và limitations. Kiểm route/page disposal/ảnh hưởng theme của `MainForm` bằng tests phù hợp theo diff; chỉ mở rộng ngoài focused recipe tests để xử lý rủi ro cụ thể. Không chạy SQL hoặc toàn solution chỉ vì bổ sung hai mẫu; không gọi discovery rỗng hay source-only inspection là native PASS.

## 6. Nghiệm thu Gallery và evidence

| Gate | Kiểm chứng cần có |
|---|---|
| Runtime/visual | Hai composition trong Gallery thật; `net48` và `net8.0-windows`; Light/Dark; Windows physical 100% và 125%; content bình thường và nhỏ của Gallery |
| UIC-01 | Filter/summary nhất quán; phân biệt empty, no matches, loading, unavailable; gõ từng phím/Enter/đổi bộ lọc giữ raw text/caret/focus; không phát sinh thao tác ghi |
| UIC-02 | Invalid submit giữ draft và không save; lỗi/summary/feedback cung cấp nội dung hiện tại qua accessibility API, focus đúng; valid Save một lần; Cancel khôi phục committed snapshot và xóa validation cũ |
| Layout | Nhãn tiếng Việt dài, mã dài, errors nhiều dòng; nội dung nhận diện/lệnh chính truy cập được; filter row wrap, grid co giãn và form scroll/footer hợp lý |
| Lifecycle/theme | Đổi theme giữ draft, control identity và selected row khi có; chuyển/đóng Gallery page giải phóng callbacks/subscriptions do mẫu sở hữu |
| Recipe/handoff | Purpose, public APIs, states, layout, ownership, exact source revision và evidence; app đối chiếu API tại runtime pin trước khi chuyển thể |

Không nhân mọi state với toàn bộ ma trận. Dùng default state cho geometry và một trường hợp khó ở viewport nhỏ/125% cho long text/error wrapping; mở rộng khi kết quả chỉ ra rủi ro. Đo content thật của Gallery, không lấy outer size làm content size hoặc ép kích thước device pixels cứng ở mọi DPI.

Evidence tương lai ở `docs/verification/20261030-ui-composition-recipes.md` phải ghi candidate, OS/runtime/bitness, actual DPI, viewport/content bounds, commands/counts, quan sát native, ảnh/artifact có thể review và decision/limitations. Không commit binaries, caches, raw results hay dumps. Thiếu Windows/desktop ghi NOT RUN/BLOCKED cùng lý do; historical PASS, bitmap/source inspection hoặc recipe source links không thay thế native evidence. Owner acceptance chỉ đổi từ PENDING khi có quyết định cho candidate cụ thể.

### Checklist hoàn thành toolkit

- [x] UIC-01/UIC-02 chạy trong ControlGallery hiện hữu, có tests hành vi và native scope phù hợp.
- [x] Recipe guide chứa đủ purpose/source/API/states/layout/ownership/evidence và có đường đọc ngắn.
- [x] Source production/public API/projects/packages không bị mở rộng ngoài phạm vi được chọn.
- [x] Exact recipe source revision được gửi cho app; runtime pin giữ độc lập và không nâng vô tình.
- [x] Native results, hạn chế và Owner decision đúng candidate; ledger phản ánh evidence thực tế.

Hoàn thành toolkit chưa tự đóng toàn đợt adoption. Skill/mapping, pilot và lượt Codex context sạch vẫn do app nghiệm thu trong CMP-T03–CMP-T05.

## 7. Handoff, phối hợp và rollback

Ưu tiên CMP-T01 và app mapping/pilot của UIC-01 trước. CMP-T02 dùng cùng contract recipe sau khi UIC-01 đủ rõ; có thể làm form riêng song song khi contract đã chốt. Một người tích hợp `MainForm`, tests và docs dùng chung; native GUI trên cùng Windows desktop luôn chạy tuần tự.

PR toolkit ghi nhận composition recipes đã hoàn tất và correction accessibility sau review. Commit correction source/test `bdac29b` đứng trước evidence/status để app có reference cố định; runtime pin app không đổi. Kích hoạt plan là commit tài liệu lịch sử, không tính vào hai implementation tasks.

App có thể chuẩn bị mapping/skill song song sau khi recipe format rõ, nhưng handoff phải chốt source links tại commit thật. Không yêu cầu app đổi runtime gitlink khi APIs hiện có đã đủ. Pilot giữ production controls và UI Lab hiện hữu; mọi chi tiết sản phẩm nằm trong [plan app](https://github.com/chung6a8m/MyDmsVn/blob/main/docs/plans/20261030-001-business-compact-composition-adoption.md).

Rollback từng mẫu phải tháo route tương ứng cùng files/tests/docs liên quan theo commit và cập nhật mapping consumer nếu đã tham khảo. Không thay status hoặc archive TAB/BUS/input/popup/A3 vì plan này trở thành scope được chọn.

## 8. Nguồn đã đối chiếu

- [WinFormsKit baseline](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/commit/2521dfe99461e8f6d1954da96ffaed0688027455) và [instructions tại baseline](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/2521dfe99461e8f6d1954da96ffaed0688027455/AGENTS.md).
- [Gallery MainForm](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/2521dfe99461e8f6d1954da96ffaed0688027455/samples/ControlGallery/MainForm.cs), [DemoPageHostForm](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/2521dfe99461e8f6d1954da96ffaed0688027455/samples/ControlGallery/DemoPageHostForm.cs) và [PaginationDemoForm](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/2521dfe99461e8f6d1954da96ffaed0688027455/samples/ControlGallery/PaginationDemoForm.cs).
- [Toolkit NUnit project](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/2521dfe99461e8f6d1954da96ffaed0688027455/tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj); commands và native conventions hiện hành tại [TESTING](../TESTING.md).
- [MyDmsVn baseline](https://github.com/chung6a8m/MyDmsVn/commit/16c24738a4758ccdceb1f3509f4e37adb0d03464), [Business Compact policy](https://github.com/chung6a8m/MyDmsVn/blob/16c24738a4758ccdceb1f3509f4e37adb0d03464/docs/design/BUSINESS_COMPACT.md) và [Independent UI Labs](https://github.com/chung6a8m/MyDmsVn/blob/16c24738a4758ccdceb1f3509f4e37adb0d03464/docs/design/INDEPENDENT_UI_LABS.md).
- [Repository UI skill tại baseline](https://github.com/chung6a8m/MyDmsVn/blob/16c24738a4758ccdceb1f3509f4e37adb0d03464/.agents/skills/mydmsvn-winforms-ui/SKILL.md); việc sửa skill/mapping và product pilot do plan app sở hữu.
