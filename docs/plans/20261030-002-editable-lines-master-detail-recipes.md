# Editable Lines và Master–Detail — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Các bước dùng checkbox; đọc design và task đang chọn trước khi sửa source.

**Goal:** triển khai hai recipe trung tính UIC-03 Editable Lines và UIC-04 Master–Detail, kiểm chứng riêng và bàn giao lần lượt cho SAL/Security phía MyDmsVn.

**Architecture:** UIC-03 dùng BootstrapSourceGrid cùng lookup/decimal adapters có sẵn trong GridGallery; UIC-04 dùng UI core và DemoPageHostForm trong ControlGallery. Các models/state/helpers mới chỉ thuộc sample. SourceGrid giữ editor lifecycle; product giữ Business Compact, DTO, session, permission và workflows.

**Tech Stack:** C# / WinForms, `net48;net8.0-windows`, SourceGrid exact gitlink, NUnit trong hai test owners hiện hữu.

**Spec:** [Thiết kế Editable Lines/Master–Detail](../design/20261030-editable-lines-master-detail-compositions.md).

## Trạng thái và nguồn quyết định

Ngày kích hoạt **2026-10-30**, theo yêu cầu tạo active roadmap/plan/chỉ dẫn. Activation là **docs-only**. Baseline source nghiên cứu `25b6c77a4d95d0df112aeba389d61a58ad0ee6db`; chưa có implementation candidate hoặc native evidence cho scope mới. File này là **owner duy nhất** của CEX-K01–K03; entry pages chỉ điều hướng.

| Task | Deliverable | Status |
|---|---|---|
| CEX-K01 | UIC-03 Editable Lines + meaningful composition tests | PLANNED |
| CEX-K02 | UIC-04 Master–Detail + meaningful composition tests | PLANNED |
| CEX-K03 | Focused native candidate review và handoff riêng từng recipe | PLANNED |

| Checkpoint trong CEX-K03 | Consumer | Status ban đầu |
|---|---|---|
| UIC-03 handoff | CEX-A02 SAL pilot | PENDING |
| UIC-04 handoff | CEX-A03 Security mapping | PENDING |

[Plan MyDmsVn](https://github.com/chung6a8m/MyDmsVn/blob/main/docs/plans/20261030-002-editable-lines-sal-adoption.md) sở hữu CEX-A01–A05. A01 bắt đầu ngay; A02 được mapping/preparation từ K01 source/results đã freeze trước final K03. Completion A02 tham chiếu handoff/limits K03/UIC-03. K02 triển khai sau K01/feedback SAL sớm hoặc chuẩn bị độc lập; K02 → K03/UIC-04 → A03. **A02 không đợi K02 hoặc toàn bộ K03 hoàn tất.** Đánh giá helpers A04 là bắt buộc; extraction có điều kiện. A05 ghi handoff cho context mới.

## Global Constraints

- Giữ `net48;net8.0-windows`, identities, dependency direction và exact SourceGrid gitlink.
- Không đổi production public API, runtime consumer gitlink, SourceGrid engine hoặc business workflows trong scope này.
- Không per-cell Bootstrap editor allocation; một adapter per column/configuration, grid sở hữu lifetime, không cross-grid sharing.
- UIC-03: Guid LineId bất biến nằm trong model/mapping, grid hiển thị STT; int ReferenceId dùng demo-only sentinel 0/chưa chọn và fake valid IDs 1/2/3. IsMarked dùng existing SourceGrid checkbox; pending input, committed draft và applied snapshot gồm bool tách biệt; invalid không partial Apply; Restore local toàn bộ snapshot.
- UIC-04: flat master list read-only, selected stable key; không CRUD/tree/permission semantics.
- Profile demo UIC-03: vi-VN, scale 2, required; Quantity 0..1000000, UnitValue 0..1000000000; Apply yêu cầu Quantity > 0 và reference hợp lệ. Không áp các số này sang SAL.
- Viewport UIC-03 1000×650 / 640×420; UIC-04 1000×650 / 480×430 logical pixels; UIC-04 stack dưới content width 720 logical pixels.
- Native runs STA, tuần tự, bounded pumping, hang diagnostics 5 phút; không modal automated wait.
- Per-recipe Light/Dark × physical100/125 × normal/small trên hai TFM; thiếu điều kiện ghi NOT RUN/BLOCKED, không chuyển historical PASS.
- UTF-8 không BOM; docs tiếng Việt; không commit binaries, dumps, raw TRX/logs/captures.

## Review Focus

- Click Apply/Remove khi invalid numeric còn active: giữ lexical candidate, focus và snapshot/row set; test K01 `InvalidPendingApplyPreservesCandidateAndSnapshot` và `InvalidPendingRemoveDoesNotDeleteRow`.
- Xóa một dòng làm row indices dịch chuyển: update/selection vẫn theo LineId; test K01 `RemoveThenEditTargetsStableLineId`.
- Lookup programmatic initialization hoặc pending search: không ghi đè ID/derived fields; test K01 `PendingLookupDoesNotChangeCommittedReferenceOrSummary`.
- Refresh/filter master mất key hoặc dữ liệu unavailable: không hiển thị detail cũ; test K02 `MissingSelectedKeyClearsDetail` và `UnavailableDetailClearsPreviousContent`.
- Theme/resize/dispose giữa interaction: giữ draft/search/native selection, không để popup/event subscription sống ngoài owner; tests tương ứng trong K01/K02 và native K03.

## File map và graph đã kiểm

| File | Thao tác dự kiến | Trách nhiệm |
|---|---|---|
| `samples/GridGallery/CompositionEditableLinesDemoForm.cs` | Create | UIC-03 form, nested fake data/snapshot, local commands |
| `samples/GridGallery/MainForm.cs` | Modify hẹp | Nút modeless route có owner và lifetime đúng |
| `tests/MyDmsVn.BootstrapSourceGrid.Tests/CompositionEditableLinesDemoFormTests.cs` | Create | UIC-03 behavior/interaction/layout/lifetime |
| `tests/MyDmsVn.BootstrapSourceGrid.Tests/BootstrapSourceGridDemoTests.cs` | Modify hẹp | Route tới UIC-03, không phá gallery hiện hữu |
| `samples/ControlGallery/CompositionMasterDetailDemoForm.cs` | Create | UIC-04 flat read-only master/detail |
| `samples/ControlGallery/MainForm.cs` | Modify hẹp | DemoPageSection mới trong Compositions |
| `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/CompositionMasterDetailDemoFormTests.cs` | Create | UIC-04 state/filter/identity/layout/lifetime |
| `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/CompositionDemoFormTests.cs` | Modify nếu route assertion cần | Giữ route/disposal của UIC-01/UIC-02 và thêm UIC-04 |
| `docs/guides/UI_COMPOSITION_RECIPES.md` | Modify theo từng recipe | Chuyển entry PLANNED thành contract/source/candidate thực |
| `docs/verification/20261030-composition-expansion.md` | Update | Evidence theo recipe/candidate và handoff |

Grid tests đã reference GridGallery và adapter; controls tests đã reference ControlGallery/UI core/helper host. Không cần thêm project reference, assembly friend access hay test suite để instantiate public demo forms và tìm named controls. SDK projects tự include files `.cs` mới. Không sửa csproj/Program/BUS routes trừ finding cụ thể được ghi lại; nếu candidate graph khác baseline thì re-read trước khi quyết định.

## CEX-K01 — UIC-03 Editable Lines

**Consumes:** design §3; `grid.BootstrapEditors.CreateLookupBox(typeof(int))`, `CreateDecimalInput(typeof(decimal), profile)`, `new SourceGrid.Cells.CheckBox(string.Empty, line.IsMarked)` đã kiểm tại engine pin/design §3.2; `BootstrapControl`, SourceGrid `CellContext.StartEdit()/EndEdit(bool)` đã dùng trong existing tests. Đọc `BootstrapDecimalInputEditorTests`, `BootstrapLookupBoxEditorTests`, `BootstrapSourceGridDemoTests` và grid contracts trước triển khai. Không suy API active-cell từ tên; đối chiếu pinned engine source.

**Produces:** proposed `CompositionEditableLinesDemoForm()` và toolbar route; stable named controls/design §3.3; private `TryFinishActiveEdit(bool)`, `TryApplyDraft()`, `RestoreAppliedSnapshot()`, `AddLine()`, `RemoveSelectedLine()`; fixture mới; source revision đủ cho K03/UIC-03. Không public API trong production assembly.

- [ ] Viết test fixture STA dùng guard/pumping/dispose hiện hữu. Thêm các case contract phía dưới; run riêng case đầu để thấy thất bại đúng behavior chưa có, không chấp nhận fixture bị skip/rỗng hoặc lỗi môi trường như red test hợp lệ.
- [ ] Implement form/nested data nhỏ nhất để dựng ba dòng fake, lookup/decimal shared editors, existing SourceGrid IsMarked checkbox, STT/derived/lock readonly columns và named commands. Kiểm ID/display separation và same editor references qua nhiều rows.
- [ ] Implement local draft/Apply/Restore cùng admission active edit; giữ stable LineId, snapshot cả IsMarked, không serialize formatted text. ReferenceId là int với sentinel 0; Apply từ chối 0. Add/Remove dùng policy trong design, feedback không có partial snapshot.
- [ ] Implement keyboard/theme/layout/disposal theo controls public và engine seams. Không rebuild grid/editors khi đổi theme hoặc chỉ resize.
- [ ] Thêm route modeless trong MainForm, test launch/close owner-disposes-child; giữ sourcegrid busy routes và gallery hiện hữu.
- [ ] Run focused discovery/tests bên dưới trên cả TFM; sửa regression trong scope. Ghi exact candidate/commands/counts/limitations; build đúng affected test project cùng transitive gallery/runtime dependencies; không mặc định Scope All.
- [ ] Cập nhật guide source/contract của UIC-03 và evidence, review diff. Commit source/tests/docs cho K01; bàn giao ngay cho K03/UIC-03, không đợi K02.

### Tests bắt buộc mới của K01

Các tên sau là **proposed test methods**, không phải cases đã tồn tại hoặc đã PASS:

| Test | Assertions chính |
|---|---|
| `SharedEditorsAreReusedAcrossStableLines` | same lookup/quantity/unit-value editor qua rows; typed decimal; editable IsMarked checkbox, STT/derived/lock readonly. Stable identity kiểm bằng behavior/values, không expose GUID hoặc public test API |
| `InvalidPendingApplyPreservesCandidateAndSnapshot` | pending `"1,00001"` vượt scale 2; Apply false; lexical text/focus còn; applied count/snapshot không đổi |
| `InvalidPendingRemoveDoesNotDeleteRow` | invalid active candidate; click Remove không giảm row count hoặc làm mất selected LineId |
| `ApplyCommitsActiveCellAndReplacesSnapshotOnce` | typed `"2,50"` commit `2.50m`; snapshot gồm tất cả rows/keys/IsMarked; feedback count tăng 1, derived đúng |
| `InvalidDraftDoesNotPartiallyReplaceAppliedSnapshot` | Add có ReferenceId 0 hoặc Quantity 0; Apply bị chặn; những dòng khác không được partial apply |
| `RestoreCancelsPendingAndRestoresAddedRemovedLines` | thay cell/IsMarked/add/remove, pending invalid; Restore về đúng order/values/checkbox của snapshot; chưa ghi Save/Post nào |
| `RemoveThenEditTargetsStableLineId` | hai dòng dùng cùng reference nhưng Quantity/UnitValue khác nhau; xóa row trước rồi edit dòng còn lại chỉ đổi đúng dòng, STT cập nhật; không row-index alias hoặc public GUID/test API |
| `PendingLookupDoesNotChangeCommittedReferenceOrSummary` | pending text và programmatic initialization giữ ID/derived; commit hợp lệ mới đổi display |
| `LockedAndDerivedCellsRejectEditing` | locked row lookup/numeric/checkbox không đổi bởi keyboard/click; STT/derived/status không editable; Remove locked không mutation |
| `UnselectedReferenceCannotApplyAndRestoreRecoversValidKey` | Add ReferenceId 0 hiển thị chưa chọn, Apply bị chặn; chọn valid ID rồi Apply thành công; đổi reference rồi Restore trả lại valid ID/display đã apply |
| `CheckboxParticipatesInMixedEditAndSnapshotLifecycle` | lookup → numeric → checkbox bằng Tab/Shift+Tab/Space/click; bool commit đúng một lần và có trong Apply/Restore; invalid numeric chặn chuyển/toggle mà không mất candidate |
| `ThemeAndResizePreservePendingInputAndEditorIdentity` | Light/Dark và small resize giữ native range/raw candidate/grid/editor objects |
| `KeyboardCommitCancelAndSharedReuseStayCoherent` | first key; Enter/Escape; Tab/Shift+Tab popup open/closed; cell cancel khác Restore; dòng sau không giữ history/candidate trước |
| `SmallViewportKeepsCommandsAndFeedbackAccessible` | 640×420: footer/buttons trong content, grid scroll, feedback AccessibilityObject.Name phản ánh Text |
| `ClosingRecipeDisposesOwnedEditorsAndPopup` | close với popup; unused/used editors disposed, global theme event không gọi vào disposed form |

Cases keyboard/popup có thể tách thành parameterized cases theo đúng contracts; không chỉ gán Text nguyên chuỗi rồi tuyên bố native typing PASS.

## CEX-K02 — UIC-04 Master–Detail

**Consumes:** design §4; UIC-01 patterns public controls/theme/disposal; existing `DemoPageSection(string, Func<Form>)` và `DemoPageHostForm`. Ưu tiên triển khai sau K01/feedback SAL sớm; preparation source độc lập không chặn A02.

**Produces:** proposed `CompositionMasterDetailDemoForm()`; private `ApplyMasterFilter()`, `SelectMaster(int?)`, `RefreshMasterData()`, `PresentSelectedDetail()`; named controls/design §4; route UIC-04 và fixture mới đủ cho K03/UIC-04.

- [ ] Viết failing tests cho flat readonly master/detail state, stable selection và data clearing theo bảng dưới; dùng existing controls test guard/host, không tạo test owner mới.
- [ ] Implement fake dataset và scenario transitions deterministic, không timer/service/SQL. Chọn key và hiển thị identity đúng ở detail; giữ Loading/Unavailable khác empty.
- [ ] Implement filter/refresh giữ selection theo key; mất key thì clear stale detail. Không đưa server count/pagination/permission tree vào mẫu.
- [ ] Implement responsive layout ngưỡng 720 logical pixels, labels wrap/content scroll; theme không recreate controls; dispose nguồn dữ liệu/events owned.
- [ ] Register DemoPageSection UIC-04, giữ UIC-01/UIC-02 routes và disposal. Run tests/discovery trên cả TFM, review hẹp existing composition fixtures.
- [ ] Cập nhật guide/evidence cho UIC-04, commit source/tests/docs và bàn giao K03/UIC-04; không đổi handoff UIC-03 đã thực hiện.

| Proposed test | Assertions chính |
|---|---|
| `SelectedMasterKeyOwnsDetailIdentity` | chọn ID 2 hiện detail ID 2; reorder list vẫn giữ key và nội dung |
| `MissingSelectedKeyClearsDetail` | filter/remove selected key → NotSelected, không detail cũ |
| `UnavailableDetailClearsPreviousContent` | Loaded → Loading/Unavailable clear previous body; feedback/count không giả 0 |
| `EmptyDetailDiffersFromNoSelection` | selected master + empty detail khác chưa có selection; caption identity còn |
| `RefreshPreservesSelectionOnlyWhenKeyStillExists` | same-key refresh giữ selection; key mất clear, không chọn nhầm index cũ |
| `ThemePreservesRawSearchAndSelectedKey` | per-key search, raw buffer/caret/focus/selection giữ Light/Dark |
| `SmallViewportStacksWithoutRecreatingControls` | 480×430, dọc dưới 720; labels dài wrap, control references/key không đổi |
| `DetailFeedbackExposesCurrentAccessibleText` | tất cả states trả nội dung hiện tại; lỗi cũ không còn sau transition |
| `RouteReplacementDisposesMasterDetailPage` | thay page/đóng host dispose resources/subscriptions, UIC-01/UIC-02 vẫn truy cập được |

## CEX-K03 — Native review và handoff theo từng recipe

**Consumes:** K01 hoặc K02 đã có source/test candidate; focused results đúng SHA; app inventory CEX-A01 để biết APIs/pin cần đối chiếu.

**Produces:** từng checkpoint UIC-03/UIC-04 trong evidence với source SHA, commands/counts, captures/observations/Owner decision, runtime compatibility đối chiếu bởi app và limitations. Handoff không thay consumer acceptance.

- [ ] Với UIC-03, ghi candidate SHA/engine pin/environment, build/discover/test đúng source; nếu build/test chưa chạy ghi NOT RUN, không tiếp tục claim READY.
- [ ] Chạy UIC-03 trên GridGallery thật: hai TFM × Light/Dark × physical100/125 × normal/small; thực hiện matrix design §6, ghi actual managed/native window DPI và native per-key observations.
- [ ] Review candidate UIC-03 với Owner, ghi decision/links/limits; đánh checkpoint READY khi đủ evidence và decision. Gửi handoff cho CEX-A02 bằng record/doc, không yêu cầu chờ UIC-04; liên lạc ngoài repo chỉ theo authorization riêng.
- [ ] Lặp build/discovery/native matrix riêng với UIC-04 khi K02 sẵn sàng; giữ evidence UIC-03 đúng candidate lịch sử. Bàn giao CEX-A03 khi checkpoint UIC-04 READY.
- [ ] Đóng K03 khi cả hai checkpoints có kết luận; nếu một checkpoint BLOCKED/PENDING, giữ phần đó mở, không đảo phần đã bàn giao. Cập nhật task status trong file này và guide của recipe tương ứng.

## Commands cho implementer

Chạy từ repo root trên Windows desktop tương tác. SDK/root props/engine theo [TESTING](../TESTING.md). Commands dưới đây là kế hoạch, **chưa chạy**. Sau khi source/tests mới có, mỗi filter phải discover case mới đúng tên; exit 0 hoặc tổng count từ fixtures cũ không chứng minh fixture mới được discover.

```powershell
$gridProject = 'tests/MyDmsVn.BootstrapSourceGrid.Tests/MyDmsVn.BootstrapSourceGrid.Tests.csproj'
$controlProject = 'tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj'
# Chọn đúng recipe đã implement; không đưa fixture chưa tạo vào evidence PASS.
$recipeChecks = @(
    @{ Project=$gridProject; Filter='FullyQualifiedName~CompositionEditableLinesDemoFormTests'; Expected='SharedEditorsAreReusedAcrossStableLines' }
    # Thêm dòng sau khi K02 sẵn sàng; K01/handoff SAL không đợi nó:
    # @{ Project=$controlProject; Filter='FullyQualifiedName~CompositionMasterDetailDemoFormTests'; Expected='SelectedMasterKeyOwnsDetailIdentity' }
)
foreach ($check in $recipeChecks) {
    dotnet restore $check.Project --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Affected-project restore failed' }
    dotnet build $check.Project -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Affected-project build failed' }
    foreach ($tfm in @('net48','net8.0-windows')) {
        $discovery = dotnet test $check.Project -c Release -f $tfm --no-build --list-tests --filter $check.Filter --blame-hang --blame-hang-timeout 5m 2>&1 | Out-String
        $discoveryExit = $LASTEXITCODE
        Write-Output $discovery
        if ($discoveryExit -ne 0 -or $discovery -notmatch [regex]::Escape($check.Expected)) {
            throw "Recipe discovery missing: $($check.Expected) / $tfm"
        }
        dotnet test $check.Project -c Release -f $tfm --no-build --filter $check.Filter --blame-hang --blame-hang-timeout 5m
        if ($LASTEXITCODE -ne 0) { throw "Recipe tests failed: $tfm" }
    }
}
```

Mặc định chỉ chạy fixture recipe mới và fixture route thực sự bị sửa: K01 `BootstrapSourceGridDemoTests`; K02 `CompositionDemoFormTests` nếu thay route assertions trong fixture này. Trước mỗi run, discovery đúng project/TFM phải có cases thật và ghi discovered/executed/pass/fail/skip. Hai test projects build transitive gallery/runtime dependencies cần thiết; không build Scope All hoặc chạy broad adapter suites chỉ vì thêm sample.

Chỉ mở rộng khi diff/finding chỉ ra risk: sửa shared editor/runtime thì chọn `BootstrapDecimalInputEditorTests`, `BootstrapLookupBoxEditorTests`/`BootstrapLookupBoxInteractionTests` hoặc `BootstrapEditorOwnershipTests` đúng behavior; sửa typography/host thì chọn fixture tương ứng. Thay dependency graph/project/shared contract mới cân nhắc full graph theo TESTING. Ghi lý do mở rộng, không coi danh sách này là gate mặc định. Ví dụ route test K01 (lặp cả TFM; `--list-tests` trước):

```powershell
dotnet test $gridProject -c Release -f net48 --no-build --list-tests --filter 'FullyQualifiedName~BootstrapSourceGridDemoTests' --blame-hang --blame-hang-timeout 5m
dotnet test $gridProject -c Release -f net48 --no-build --filter 'FullyQualifiedName~BootstrapSourceGridDemoTests' --blame-hang --blame-hang-timeout 5m
```

Launch gallery đúng binary mới build, từng instance một. Grid route: `samples/GridGallery/bin/Release/<TFM>/MyDmsVn.BootstrapSourceGrid.Demo.exe` → UIC-03 button. Control route: `samples/ControlGallery/bin/Release/<TFM>/MyDmsVn.Bootstrap5WinFormUI.Demo.exe` → Compositions → UIC-04. Route recipe chưa tồn tại ở activation. Không dùng BUS scenario CLI để claim recipe proof. Ghi viewport **content** thực, không chỉ outer window size. Owner review/keyboard/popup evidence không thay bằng hidden/bitmap-only tests.

## Handoff cho context mới

Đọc README → ROADMAP → plan này → design → evidence. Chọn task PLANNED/IN PROGRESS và kiểm current candidate/working tree trước khi sửa. Đọc app plan để biết A01/A02/A03 đã tiến tới đâu, không viết trạng thái app trong bảng toolkit. Mỗi handoff giữ: recipe ID, implementation SHA, evidence SHA/link, runtime consumer SHA được app kiểm, public APIs dùng, fake semantics phải thay, limitations và next owner. Source recipe cập nhật không ngầm nâng gitlink.

Plan cũ CMP-T01/T02, evidence `1420f25`/`bdac29b`, TAB/BUS và các scopes trước không bị mở lại. Không xin lại lựa chọn roadmap đã được user yêu cầu; execution source chỉ bắt đầu khi task implementation thực sự được giao.
