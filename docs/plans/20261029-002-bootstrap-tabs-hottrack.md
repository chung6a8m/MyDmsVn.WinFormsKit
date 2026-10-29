# BootstrapTabs và HotTrack dùng chung — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: dùng `superpowers:subagent-driven-development` hoặc `superpowers:executing-plans` nếu môi trường có skill; thực hiện từng task theo checkbox. Thiếu skill không cho phép bỏ kỹ thuật, tests hoặc acceptance của plan. Tuân theo AGENTS.md và TESTING.md.

**Goal:** thêm `BootstrapTabs : TabControl` độc lập với app, sửa HotTrack của control cũ bằng implementation dùng chung, và cung cấp demo workspace có native regression.

**Architecture:** giữ hai public facade trực tiếp kế thừa TabControl; chia sẻ metrics/layout, hover, painter và close helpers nội bộ. Giữ chrome hiện tại của control cũ; control mới compose flat chrome cùng native/header drawing trong một paint pass đã qua proof trên Windows.

**Tech Stack:** C# 12, WinForms, System.Drawing/GDI+, Win32 interop hẹp, NUnit; `net48;net8.0-windows`; SDK baseline `8.0.400`/latestFeature; ProjectReference hiện hành.

**Spec:** [Thiết kế chung, mục 1–3 và 5](../design/20261029-tabs-hottrack-live-busy-overlay.md). Đọc cả design và plan trước khi code. [Luồng BUS](20261029-003-live-busy-overlay.md) độc lập; không đợi BUS để sửa HotTrack.

**Baseline:** toolkit `8e7f1854d85d80fbaecc3d9d9167bb8ebcd94921`; reference app `16c24738a4758ccdceb1f3509f4e37adb0d03464`. **Trạng thái candidate TAB-01–06:** TAB-04/05 implementation và focused/native proof có ở `d67abbd7b1f2b3081e26e8b0a22bbb03761bbaf0`; TAB-06 contract/API/focused proof tiếp tục trên candidate code `5b012e276ac5e7b12e150751f2c2fc95e9bdc563`. [Evidence](../verification/20261029-tabs-hottrack.md) giữ số liệu theo từng commit. Native paint gate TAB-01 và physical/Owner acceptance còn OPEN. Không lấy test PASS ở DPI96 để đóng nghiệm thu physical.

## Global Constraints

- Targets và identities giữ `net48;net8.0-windows`, `MyDmsVn.Bootstrap5WinFormUI`; không package mới, không đổi SourceGrid pin hoặc consumer repo.
- `BootstrapTabs` trực tiếp kế thừa `System.Windows.Forms.TabControl`; không tham chiếu BusinessCompactAppearance/app/SQL, không public tab base hoặc page model riêng.
- Defaults mới: `TabStyle=Underline`, `Variant=Primary`, `Fill=false`, `BorderRadius=0`, inherited `HotTrack=false`, `OwnerDrawFixed/Fixed`; defaults của `BootstrapTabControl` giữ nguyên.
- Giữ native TabPages/selection/events/keyboard/accessibility/images/tooltips/overflow; close chỉ request, app sở hữu quyết định và lifetime page.
- Constructor handle-free; giữ caller-owned Font/ImageList; dispose subscriptions, font, menu và HDC/buffer do toolkit tạo. Không bỏ bảo vệ input/popup của PR #3.
- Native tests STA, tuần tự, bounded pumping, hang diagnostics 5 phút; bitmap test không thay chuỗi capture màn hình thật. Không tăng allowance chỉ để che flicker.
- Markdown UTF-8 không BOM; C#/project files theo .editorconfig/.gitattributes; Git index normalize LF, Windows checkout theo eol=crlf.

## Review Focus

1. Con trỏ đứng yên nhưng tab dịch chuyển sau overflow/resize/handle recreation: hover phải theo hình học mới — TAB-02.
2. Consumer `DrawItem` và derived `OnDrawItem` vẽ sau base: thứ tự và pixel phải còn nguyên — TAB-03/TAB-04.
3. Selected bounds mở rộng, child overflow và dirty rect sát cạnh: không cắt focus hoặc phủ vào page — TAB-01/TAB-04.
4. Pointer close vào tab chưa chọn, callback xóa page ngay: không select/focus tab đó hoặc giữ stale page/menu — TAB-03.
5. Print HDC có origin/clip khác màn hình và compact/fallback roundtrip: không recursion, không sửa caller properties — TAB-04.

## File map và interfaces dùng chung

Các đường dẫn mới trong bảng là deliverables cần tạo, không phải files đã tồn tại. Root production: `src/MyDmsVn.Bootstrap5WinFormUI/`; root tests: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/`.

| File | Trách nhiệm / task |
|---|---|
| `Controls/BootstrapTabControl.cs` | Sửa hẹp, giữ facade/extensibility — TAB-02/03. |
| `Controls/BootstrapTabControlRenderLogic.cs` | Tái sử dụng pure metrics/palette/layout; sửa caption logic có regression — TAB-03. |
| `Controls/BootstrapTabs.cs` | Public facade/defaults/native lifecycle của control mới — TAB-04. |
| `Controls/Internal/BootstrapTabInteractionController.cs` | Hover, dirty regions, style filtering, resync — TAB-02. |
| `Controls/Internal/BootstrapTabHeaderPainter.cs` | Vẽ vào Graphics đã cung cấp, không layout/mutate state — TAB-03. |
| `Controls/Internal/BootstrapTabCloseController.cs` | Closable membership, owned menu, pointer/keyboard routing — TAB-03. |
| `Controls/Internal/BootstrapTabNativePaint.cs` | Paint/HDC/buffer lifetime và interop tối thiểu đã qua spike — TAB-04. |
| `Controls/Internal/BootstrapTabChromeSnapshot.cs` | Client/page/header/native-child bounds và layout generation — TAB-04. |
| `Controls/BootstrapTabInteractionTests.cs`, `Controls/BootstrapTabPaintCompatibilityTests.cs`, `Controls/BootstrapTabsTests.cs`, `Controls/BootstrapTabsNativeTests.cs` trong root tests | Regression theo ownership từng task. |
| `samples/ControlGallery/BootstrapTabsDemoForm.cs` | Demo mới — TAB-05. |
| `tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/TabsHoverScenario.cs` | Proof, trace và native capture có timeout — TAB-01/04. |

Interfaces internal cần giữ thống nhất giữa các task:

```csharp
// BootstrapTabInteractionController : IDisposable
BootstrapTabInteractionController(TabControl owner, Func<bool> usesCustomHeaders);
int HoveredIndex { get; }
void MouseMoved(Point clientPoint);
void MouseLeft();
void RefreshFromPointer();
void Reset();
int FilterNativeStyle(int nativeStyle);

// BootstrapTabHeaderPainter (internal static)
void DrawHeader(DrawItemEventArgs e, BootstrapTabHeaderLayout layout,
    BootstrapTabHeaderMetrics metrics, BootstrapTabHeaderPalette palette,
    string text, Image? image, Rectangle closeBounds,
    bool enabled, bool selected, bool showFocus);
void DrawCompactChrome(Graphics graphics, BootstrapTabChromeSnapshot snapshot,
    Rectangle dirtyBounds, BootstrapTheme theme);

// BootstrapTabCloseController : IDisposable
BootstrapTabCloseController(TabControl owner, Func<TabPage, TabCloseReason, bool> requestClose);
void SetClosable(TabPage page, bool closable);
bool IsClosable(TabPage page);
Rectangle GetButtonBounds(int index);
bool TryHandleMouseDown(Point location);
bool TryHandleMouseUp();
bool TryProcessShortcut(Keys keyData);
void PageRemoved(TabPage page);
void SelectionChanged();

// BootstrapTabNativePaint : IDisposable; instance gắn với một owner
delegate void BootstrapTabMessageDispatch(ref Message message);
BootstrapTabNativePaint(TabControl owner);
bool TryPaint(ref Message message, BootstrapTabMessageDispatch dispatch,
    Action<Graphics, Rectangle> completeChrome);
```

Đây là bảng chữ ký, không phải một file C# có thể compile nguyên khối. `TryPaint=true` nghĩa message đã được xử lý đúng contract; `false` phải đi base/native dispatch. Facade cung cấp callback gọi `base.WndProc(ref message)`; không gọi lại WndProc override của chính nó. Chrome snapshot có `ClientBounds`, `PageDisplayBounds`, `HeaderPaintBounds`, `NativeChildExclusions`, `LayoutGeneration`; thu actual DrawItem bounds trong pass rồi chốt snapshot trước khi tô chrome. Painter không tự đọc HWND/GetTabRect hoặc gọi Invalidate.

## Trình tự và trạng thái

| Task | Depends on | Trạng thái ban đầu |
|---|---|---|
| TAB-01 — native reproducer và paint proof | baseline | IN PROGRESS — probe hai TFM; gate OPEN |
| TAB-02 — common hover fix | TAB-01 trace | IN PROGRESS — focused tests và overflow stationary-pointer hai TFM; native tooltip/focus oracle còn mở |
| TAB-03 — painter/close extraction | TAB-02 | IN PROGRESS — painter/close và focused tests hai TFM; physical 125%/orientation còn mở |
| TAB-04 — BootstrapTabs và composition | TAB-01 proof, TAB-03 | IN PROGRESS — code, tests và native DPI96 proof hai TFM đã có; physical/Owner gate OPEN |
| TAB-05 — demo | TAB-04 | IN PROGRESS — route/actions/tests đã có; Light/Dark và physical review PENDING |
| TAB-06 — contracts, API, native acceptance | TAB-02–05 | IN PROGRESS — contract/API và native 100%/125% Light/Dark PASS; Owner static/temporal 100%/125% APPROVED; 150%/200% và mixed-DPI NOT RUN/BLOCKED với OWNER EXCEPTION APPROVED ngày 29/10/2026. Các gate độc lập của TAB-01–05 chưa được đánh DONE bởi ngoại lệ này. |

### TAB-01 — Reproducer và proof cho native paint

**Files:** tạo `tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/TabsHoverScenario.cs`; sửa `Program.cs` cùng thư mục để thêm `--scenario tabs-hover <output-directory>` mà giữ scenarios cũ. Report mới khi thực thi: `docs/verification/20261029-tabs-hottrack.md`.

**Interfaces:** consume controls hiện hữu; produce command `tabs-hover`, metadata JSON và bounded trace; prototype nằm trong helper host, chưa thêm public API production.

- [ ] Tạo baseline form gồm native TabControl, BootstrapTabControl HotTrack on/off và một direct-TabControl paint probe. Có text-only, image, close, long captions và overflow; dùng cùng font/theme/size để so sánh.
- [ ] Log message order, paint depth, hovered index, dirty rect, selected DrawItem bounds, ItemSize/GetTabRect, HWND identity; trace/capture chỉ trong helper. Mỗi scenario tối đa 30 giây, cleanup trong finally; test process có timeout dưới hang gate 5 phút.
- [ ] Tái hiện qua mouse input thực và ghi baseline. Guard test `Hover_DoesNotChangeFontItemSizeRectsSelectionOrHandle` snapshot trước/sau phải giữ bằng nhau; ghi NOT REPRODUCED nếu không thấy nháy, không dựng số liệu giả.
- [ ] Thử native WM_PAINT supplied-HDC vào buffer, native DrawItem/user/derived paint cùng pass, sau đó completion chrome. Kiểm offset control khác 0, dirty một header, overflow child và selected page. Tìm DrawItem ngoài composed WM_PAINT, không chỉ đếm final pixels.
- [ ] Kiểm print/supplied DC với origin/clip khác, không gọi screen DC hoặc recursion. Giữ BeginPaint/EndPaint chỉ trên screen WM_PAINT không supplied DC.
- [ ] Chạy command helper ở hai TFM theo mục Commands; ghi actual OS/bitness/DPI và before/after capture sequence. Gate chỉ PASS khi native semantics và drawing ownership được chứng minh. Nếu seam thất bại, ghi BLOCKED kèm minimal repro trước khi làm flat chrome; không tự chuyển control cũ sang UserPaint hoặc thay bằng Panel/buttons.
- [ ] Commit `test: add native tabs hover and paint reproducer` gồm helper/report text; không commit raw artifacts.

### TAB-02 — Một cơ chế hover dùng chung

**Files:** sửa `Controls/BootstrapTabControl.cs`; tạo `Controls/Internal/BootstrapTabInteractionController.cs` và `Controls/BootstrapTabInteractionTests.cs` trong root tests. Scoped `InvalidateRect` interop nằm riêng trong interaction helper, chỉ nhận HWND đã có; native-paint/composition helper được tạo ở TAB-04 nên TAB-02 không phụ thuộc file tương lai đó.

**Interfaces:** produce interaction signatures ở file map; custom headers là `DrawMode == OwnerDrawFixed` trong mode painter toolkit hoạt động; DrawMode.Normal giữ native style/hover.

- [ ] Thêm regressions đi qua MouseMove thật/dispatch native: `HoverWithinSameHeader_DoesNotInvalidateAgain`, `HotTrackFalse_DoesNotRequestCustomHoverRepaint`, `HoverTransition_ChangesOnlyExpectedOldNewHeaderRegions`, `Hover_DoesNotChangeFontItemSizeRectsSelectionOrHandle`. Assert vùng ảnh hưởng/palette, không khóa tổng số WM_PAINT mà Windows có quyền coalesce.
- [ ] Chạy focused tests trước sửa; ghi RED cho defect có test oracle xác định. Test đang PASS về cùng-index là invariant cần giữ, không gọi nó là RED mới.
- [ ] Dùng CreateParams override với `FilterNativeStyle` để clear chỉ TCS_HOTTRACK khi custom headers; không shadow HotTrack, không thay native mouse selection/tooltip handling. Đánh giá lại sau handle recreation và DrawMode changes.
- [ ] Chỉ invalidate khi effective visual đổi; disabled/selected palette không đổi thì không tạo work thêm. Hit-test dựa geometry native hiện hành; invalidation bao paint extent cần thiết của old/new header và clip khỏi child windows.
- [ ] Thử no-erase invalidation cho vùng header được vẽ đầy đủ, có A/B trace chứng minh cải thiện; không gọi Invalidate(rect,false) rồi gọi đó là no-erase, không swallow WM_ERASEBKGND toàn control. Không Refresh/Update/debounce/timer cho hover.
- [ ] Thêm `StationaryPointer_RefreshesAfterOverflowResizePageMutationAndHandleRecreation`: resync sau resize, DPI/font/theme, selection/overflow, add/remove/reorder, enable/disable và recreate. Reset stale index/bounds trước handle loss; không tạo handle chỉ để resync.
- [ ] Chạy lại focused tests + native tabs-hover trên hai TFM; so sánh invariants, native tooltip/overflow/focus. Chỉ di chuyển image sizing khỏi paint nếu trace chứng minh có liên quan, kèm image-mutation regression.
- [ ] Commit `fix: unify native tab hover tracking and invalidation`.

### TAB-03 — Chia sẻ painter và close behavior

**Files:** tạo hai helpers painter/close trong file map; sửa `BootstrapTabControl.cs`, `BootstrapTabControlRenderLogic.cs`; tạo `Controls/BootstrapTabPaintCompatibilityTests.cs`, cập nhật `Controls/BootstrapTabControlTests.cs` và `Controls/BootstrapTabControlRenderLogicTests.cs`.

**Interfaces:** consume interaction; produce painter/close signatures. Public RequestTabClose ở facade vẫn kiểm membership/enabled và phát event; controller gọi facade qua callback. Không dispose context menu do consumer cung cấp.

- [ ] Khóa regression `DerivedOnDrawItem_AndUserHandlers_PreserveOrderingGraphicsAndOutput`: derived probe ghi before → base (default renderer + user handler) → marker after; final pixels giữ marker, đúng index/bounds và cùng HDC pass. Khóa close pointer trước selection/focus, Ctrl+W, context menu, disabled/unowned page, synchronous removal trong callback và disposal.
- [ ] Chạy tests để lưu baseline trước extraction. Tách phần body rendering vào painter nhưng giữ DrawItem subscription/order hiện tại của control cũ; không đổi sang override làm user handlers bị tô đè.
- [ ] Tính caption trong available content rectangle sau khi trừ icon/spacing/close. Dùng cùng measurement/draw padding flags, draw một lần; tests có tiếng Việt, empty/long caption, icon/no-icon, closable và mọi variant/style. Chỉnh `NoPadding` phải có regression focus clearance ở 125%, không sửa riêng Primary text-only như app workaround.
- [ ] Close controller giữ optional membership, owned menu/keyboard paths và stale-page cleanup; GetTabCloseButtonBounds của control cũ giữ access/signature/nonvirtual behavior. Public close không tự dispose/remove/select page.
- [ ] Painter Save/Restore state, không thay layout/state trong paint. Fallback orientation chuẩn hóa geometry; nếu dùng Graphics transform cho vertical text thì chọn primitive có hỗ trợ transform và kiểm readability, không giả định TextRenderer tự tuân GDI+ rotation.
- [ ] Chạy existing BootstrapTabControl/RenderLogic fixtures cùng tests mới trên hai targets, không xóa assertions chỉ vì private state đã tách; thay reflection-hover setup bằng behavior seam tương đương hoặc native regression.
- [ ] Commit `refactor: share tab header painting and close requests`.

### TAB-04 — BootstrapTabs và flat chrome composition

**Files:** tạo `Controls/BootstrapTabs.cs`, native-paint/chrome-snapshot helpers; tạo `Controls/BootstrapTabsTests.cs`, `Controls/BootstrapTabsNativeTests.cs`; mở rộng `TabsHoverScenario.cs`.

**Interfaces:** consume TAB-02/03; produce public API/defaults trong design mục 3; `TryPaint` và snapshot contract ở file map. Gắn helpers theo instance, không chia sẻ mutable paint/hover state giữa controls.

- [ ] Viết test RED `BootstrapTabs_HasDirectNativeBaseType` với `typeof(BootstrapTabs).BaseType == typeof(TabControl)` và exact defaults; kiểm constructor chưa có HWND, giữ consumer Font/ImageList/pages, theme subscription cleanup.
- [ ] Tạo facade, nối native selection/keyboard/events và close request; class trực tiếp kế thừa TabControl. Không copy app appearance/DTO/session hoặc reuse BusinessCompactWorkspaceTabs như một dependency.
- [ ] Cài supplied-HDC composition đã qua TAB-01: BeginPaint/EndPaint một lần, buffer/dirty clip theo client coordinates, native dispatch gọi base seam, nhận actual header bounds, complete chrome, present. HDC/bitmap/selected objects trả lại trong finally; nested paint có dispatch hợp lệ, không chỉ return làm mất paint.
- [ ] `DrawCompactChrome` dùng snapshot generation và native child exclusions; không vẽ vào page content hoặc overflow child. Selected bounds lấy từ DrawItem của pass hiện tại; cache chỉ có giá trị trong generation phù hợp. Empty TabCount có surface rõ ràng.
- [ ] Tôn trọng WM_PRINT/WM_PRINTCLIENT flags, target HDC/clip/origin, child-print ownership; đảm bảo một lần composition cho nested print. Test `PrintClient_UsesSuppliedDcWithoutScreenPaintOrRecursion`, cả direct HDC và DrawToBitmap; bitmap output không thay native hover test.
- [ ] Cài native chrome fallback đúng design: Bottom/Left/Right, Multiline, RTL, Buttons/FlatButtons giữ caller properties và owner-draw events; DrawMode.Normal giữ system paint. Test từng mode đại diện và compact→fallback→compact; không clear OWNERDRAWFIXED khi property chưa đổi.
- [ ] Chạy `NativeOverflow_RemainsClickableAndNotCoveredByCompactChrome`, close/keyboard/focus, user/derived DrawItem, theme/DPI/handle recreation, tiny/empty control. Hover không làm repaint cả page hoặc đổi tab geometry.
- [ ] Chạy helper trên hai TFM; giữ no-flicker acceptance mở nếu còn native drawing ngoài buffer hoặc thiếu desktop evidence. Commit `feat: add native BootstrapTabs workspace control`.

### TAB-05 — Demo BootstrapTabs

**Files:** tạo `samples/ControlGallery/BootstrapTabsDemoForm.cs`, `Demo/BootstrapTabsDemoFormTests.cs` trong root tests; sửa `samples/ControlGallery/MainForm.cs`, `NavigationDemoForm.cs`; cập nhật test route thực sự bị ảnh hưởng trong `Demo/IntegratedDemoApplicationTests.cs`/`NavigationDemoFormTests.cs`.

**Interfaces:** gallery route `Navigation / BootstrapTabs`; form kế thừa `DemoFormBase`; dùng public API, không reflection/private WinForms hoặc SQL.

- [ ] Test route tồn tại, tạo đúng form, có một control cũ và một BootstrapTabs, HotTrack toggle thực sự đặt property, add/remove/close controls có tác dụng và mọi demo child được dispose cùng form.
- [ ] Tạo comparison cùng size/font/theme, default mới Underline; có selectors style/variant/Fill và HotTrack mặc định bật trong demo. Caption tiếng Việt dài, icon, tooltip, fixed/closable page, empty state và nút thêm nhiều tab tạo overflow.
- [ ] Đặt TextBox/Button trong TabPage để quan sát focus/validation; event status báo selection/close request, demo owner tự quyết định xóa page. Không auto-select tab khi chỉ bấm close.
- [ ] Bổ sung HotTrack toggle rõ ở demo cũ; mở rộng tests thay vì xóa scenarios Navigation hiện hữu. Kiểm Light/Dark, resize nhỏ, keyboard và physical 100/125; không nhồi toàn bộ diagnostics kỹ thuật vào vùng thao tác chính.
- [ ] Commit `feat: add BootstrapTabs comparison and interaction demo`.

### TAB-06 — Contract, API và acceptance

**Files:** sửa `docs/components/controls/COMPONENTS.md`, `PUBLIC_API_BASELINE.md`, `COMPATIBILITY.md`, `docs/CONSUMER_MIGRATION.md`, `docs/TESTING.md`, `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`; cập nhật report `docs/verification/20261029-tabs-hottrack.md` và checklist plan này.

- [x] Ghi API/defaults, supported compact mode/native chrome fallback, inherited HotTrack, close/ownership và DrawItem compatibility. Consumer recipe thay app subclass bằng BootstrapTabs theo exact pin ở PR riêng; giải thích lớp app vẽ screen DC sau base nằm ngoài toolkit buffer.
- [x] Review exported public/protected diff thực tế; thêm exact contract tests, rồi mới cập nhật fingerprint bằng assembly đã build trên hai TFM. Giữ assembly version; không hard-code một hash đoán trước hoặc nới gate để bỏ qua overrides mới.
- [x] Chạy Commands và native matrix: hai TFM × Light/Dark × physical 100/125 **PASS**, actual window DPI 96/120 đã ghi; hover/overflow/print/fallback/close đã kiểm trong matrix. Representative 150/200 **NOT RUN** và mixed-DPI **NOT RUN / BLOCKED BY ENVIRONMENT** vì chỉ có một monitor; Owner **APPROVED ngoại lệ** cho đúng hai khoảng trống này trên candidate TAB hiện hành ngày 29/10/2026. Không ghi PASS cho case chưa chạy hoặc chuyển ngoại lệ sang candidate/BUS khác.
- [x] Build graph All để phát hiện consumer compile issues; tests chọn Controls fixtures liên quan, không tự kéo SQL hoặc full suites ngoài boundary. Nếu manifest/DPI gallery đổi đồng thời bởi BUS, chạy thêm integrated typography/layout regressions trên candidate hợp nhất.
- [x] Lưu 18 ảnh visual có thể mở lại bằng [artifact URL + candidate/scenario/hash manifest](../verification/20261029-tabs-hottrack-visual-artifacts.md); các URL đã kiểm GET không đăng nhập và hash byte khớp. Raw trace/JSON vẫn local và được ghi giới hạn.
- [x] Owner review 18 ảnh Light/Dark, fallback và ba frame hover tĩnh trên candidate đó: **APPROVED visual ảnh tĩnh** ngày 29/10/2026, đúng phạm vi trong [manifest](../verification/20261029-tabs-hottrack-visual-artifacts.md).
- [x] Owner **APPROVED temporal 125%** cho bốn clip trên candidate `5dfa854` và **APPROVED temporal 100%** cho [bốn clip](../verification/20261029-tabs-hottrack-temporal-artifacts.md) trên candidate `9e7af4a`, ngày 29/10/2026. Cả hai bộ là Light/Dark × hai TFM; production source không đổi. 150%/200% và mixed-DPI được xử lý bằng ngoại lệ Owner riêng ở trên, không phải temporal PASS.
- [ ] Commit `docs: record tabs contracts and candidate verification`; chỉ đánh DONE từng task đã đạt cả deliverable và evidence của chính nó.

## Commands cho implementer

Commands dưới đây là hướng dẫn, **chưa chạy trong commit lập plan**. Từ root Windows checkout, restore/build theo policy trước khi dùng SkipBuild:

```powershell
.\build.ps1 -Configuration Release -Scope Controls
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --list-tests --filter "FullyQualifiedName~BootstrapTab|FullyQualifiedName~Phase16PublicApiBaselineTests|FullyQualifiedName~NavigationDemoFormTests" --blame-hang --blame-hang-timeout 5m
.\test.ps1 -Configuration Release -Scope Controls -Framework All -SkipBuild -Filter "FullyQualifiedName~BootstrapTab|FullyQualifiedName~Phase16PublicApiBaselineTests|FullyQualifiedName~NavigationDemoFormTests|FullyQualifiedName~IntegratedDemo"
& tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/bin/Release/net48/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe --scenario tabs-hover artifacts/verification/tabs/net48
& tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/bin/Release/net8.0-windows/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe --scenario tabs-hover artifacts/verification/tabs/net8
.\build.ps1 -Configuration Release -Scope All
```

Discovery lặp cho net8.0-windows; filter phải tìm đúng cases của candidate. Xác minh output exe path sau build, ghi path thật nếu SDK/RID thay đổi; không chạy nhầm binary cũ. Helper phải tự kết thúc hữu hạn; real physical DPI đổi bằng Windows settings và khởi động lại host khi cần, không dùng synthetic metrics làm thay bằng chứng native.

## Handoff và rollback

TAB-01→02→03→04→05→06 là đường chính. Có thể chạy BUS song song ở files riêng; `Program.cs`, `MainForm.cs`, docs/API baseline và manifest phải do một integrator merge tuần tự. Chốt API fingerprint một lần trên candidate hợp nhất, không để nhánh sau ghi đè hash của nhánh trước.

Rollback theo các commits nhỏ trên. Control mới là additive; consumer cũ chưa bị migrate. Nếu cần revert hover fix, giữ native repro để so sánh và không xóa bằng chứng lỗi. Plan hoàn tất implementation không tự cho phép merge/publish hoặc đóng acceptance chưa có evidence.
