# Chiều cao input và góc bo popup — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` hoặc `superpowers:executing-plans` nếu môi trường có các skills này. Triển khai từng task theo checkbox; tuân theo AGENTS.md và TESTING.md của repository. Không xem việc thiếu skill trên máy khác là lý do bỏ các bước kỹ thuật trong plan.

**Goal:** đồng nhất chiều cao ngoài của input mặc định và sửa độ liền của shell/popup Select, LookupBox bằng geometry, backing và content layout có kiểm chứng native.

**Architecture:** giữ native ComboBox và shared ToolStripDropDown hiện tại. Thêm helper nội bộ cho outer/stroke/inner geometry; sửa phép đo native, backing opaque và decoration-safe content. Giữ behavior/data model/activation, mở rộng regression đúng lớp pixels hiển thị.

**Tech Stack:** C#, WinForms, System.Drawing/GDI+, NUnit; `net48;net8.0-windows`; SDK baseline 8.0.400/latestFeature; source ProjectReference hiện hành.

**Spec:** [Thiết kế, căn cứ source và lịch sử sửa](../design/20261029-input-popup-rendering.md). Đọc cả hai tài liệu trước khi code.

**Trạng thái:** READY FOR IMPLEMENTATION; mọi task bên dưới chưa thực thi. Phân tích ngày 29/10/2026 dùng toolkit `fd317f0aa4242cc9a44b7554c54f96bb883b6357`. Không có Windows/native PASS hoặc Owner approval cho bản sửa mới.

## Global Constraints

- Targets giữ `net48;net8.0-windows`; UI core không phụ thuộc SourceGrid/app/SQL; không thêm package hay đổi vendor pin.
- `BootstrapComboBox : ComboBox` giữ public/native model; không thay bằng UserControl/BootstrapSelect, không thay binding/formatting/autocomplete/events để chữa hình học.
- Constructor và Designer không tạo HWND; giữ caller-owned Font, Region, content ownership; dispose tài nguyên/GDI/events do toolkit tạo.
- Giữ outer AA envelope 1 device pixel và owned inner host đã sửa trong upstream PR #48; thay geometry có kiểm chứng, không xóa bảo vệ cũ chỉ để test mới pass.
- `ControlHeight` là outer-height mặc định; 32 logical px với cùng font tiêu chuẩn 9–10 pt. Combo font lớn dùng safe minimum; các controls khác giữ size policy hiện có. Giữ caller width/position/Font và semantics hiện có của Size/MinimumSize/MaximumSize; không tạo cơ chế tự resize cả hàng.
- Popup giữ ToolStripDropDown, activation domain, focus policy, outside-click, generation guards và native shadow mặc định; không thêm layered/compositor architecture vào scope này.
- Production không screen-sample desktop, reflection WinForms private fields hoặc polling/sleep để sửa layout. Reflection có thể dùng trong diagnostic test có giải thích.
- Markdown tiếng Việt UTF-8 không BOM; working-tree EOL theo .editorconfig/.gitattributes. Source/test C# giữ quy ước repository.
- GUI tests STA, NonParallelizable, bounded message pumping; raw test commands có `--blame-hang --blame-hang-timeout 5m`. Không dùng tham số `-HangTimeoutMinutes` của tài liệu upstream cũ.
- Mọi kết quả gắn đúng candidate/framework. Linux cross-build, bitmap giả lập và historical PASS không thay displayed HWND/physical DPI acceptance.

## Review Focus

1. Constructor, handle recreation và explicit caller font/size/Region: native height không tăng dần, không tạo HWND sớm, không phá ownership — T03/T06.
2. Popup có zero logical padding, child màu tương phản, footer/selected row ở góc và radius lớn trong bounds nhỏ: child không phủ decoration; preferred size và actual layout cùng tính inset — T02/T04/T05.
3. Popup đang mở đổi radius/theme/DPI hoặc size giữ nguyên: pixels/Region cập nhật, query/highlight/scroll/caret/selection và HWND không bị reset — T04/T05/T07.
4. Click đầu mở Select, outside-click Lookup, Tab/Shift+Tab và callback cũ sau reopen: giữ event counts và focus ownership — T05/T07; chạy explicit first-click riêng.
5. Test bitmap có A=0, capture trắng/đen hoặc chỉ clone Region của implementation: oracle phải fail thay vì tính đó là AA hợp lệ — T01/T04/T07.

## 1. Ranh giới công việc và files

| Đơn vị | Files chính | Trách nhiệm |
|---|---|---|
| Shared geometry/painter mới | `src/MyDmsVn.Bootstrap5WinFormUI/Rendering/BootstrapRoundedSurfaceGeometry.cs`, `BootstrapRoundedSurfacePainter.cs` | Hình outer/stroke/inner, decoration inset, state-safe shape painting |
| Combo sizing mới | `Controls/BootstrapComboBoxSizingLogic.cs`, `Controls/BootstrapComboBox.cs`, `Controls/BootstrapComboBoxRenderLogic.cs` | Outer target, native overhead, lifecycle/layout contract |
| Popup shell | `Controls/BootstrapOverlaySurface.cs`, `BootstrapOverlayDropDown.cs`, `BootstrapOverlayToolStripRenderer.cs` | Backing, Regions, measurement/layout, clone sync |
| Select popup | `Controls/BootstrapSelect.cs`, `Controls/BootstrapSelect.Popup.cs`, `Controls/Internal/BootstrapSelectDropDownController.cs` | Outer preferred size và live radius |
| Lookup popup | `Controls/BootstrapLookupBox.cs`, `BootstrapLookupBox.Popup.cs`, `Controls/Internal/BootstrapLookupDropDownController.cs`, `BootstrapLookupDropDownContent.cs`, `BootstrapLookupFooter.cs` | Embedded grid border, owned DPI, live radius, content bounds |
| Grid policy hẹp | `Controls/BootstrapDataGridView.cs` | Internal border mode; standalone behavior không đổi |
| Input/button shell | `Controls/BootstrapTextBox.cs`, `BootstrapButton.cs`, `BootstrapNumericBox.cs`, `BootstrapDatePicker.cs`, `BootstrapSelectRenderLogic.cs` | Áp dụng shared geometry, giữ palette/input/focus semantics |
| Native capture và lab | `tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/Program.cs`, scenario/manifest/config cùng project khi cần; `samples/ControlGallery/RenderingDemoForm.cs` | DPI awareness trước HWND, host/capture có timeout, board input/popup so sánh |
| Evidence/docs | `docs/verification/20261029-input-popup-rendering.md`, các component contracts được đổi | Candidate-specific report và consumer handoff |

Các đường dẫn `Controls/...` ở bảng thuộc `src/MyDmsVn.Bootstrap5WinFormUI/`. Không tách một base class lớn hoặc một popup engine mới. Chỉ thêm file/helper khi task bên dưới có caller cụ thể.

### Thứ tự và checkpoint

`T01 → T02 → T03 → T04 → T05 → T06 → T07` là thứ tự mặc định. T03 có thể nghiên cứu song song với T02 nhưng chỉ tích hợp sau khi interface geometry ổn định. Không chạy hai native testhosts tranh focus cùng lúc.

Mỗi task tạo một commit có thể review, ghi failing case trước/sau, files đã chạm và giới hạn. Một task fail không cho phép xóa regression cũ hoặc đổi policy để làm xanh. Không mở rộng sang SQL/module nghiệp vụ.

## 2. Lệnh và môi trường

Nguồn lệnh hiện hành: [TESTING](../TESTING.md), `build.ps1`, `test.ps1`. Trên Windows, từ repository root:

```powershell
git status --short
git rev-parse HEAD
git submodule update --init --recursive
git submodule status --recursive
dotnet --version
.\build.ps1 -Configuration Release -Scope All

$controlsProject = "tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj"
$gridProject = "tests/MyDmsVn.BootstrapSourceGrid.Tests/MyDmsVn.BootstrapSourceGrid.Tests.csproj"
```

Không ghi đè working changes hoặc vendor changes. Chỉ dùng `--no-build/--no-restore/-SkipBuild` ngay sau build thành công của cùng candidate/configuration. Lệnh theo task bên dưới tự build khi không có SkipBuild.

Chạy discovery trước execution cho từng filter/framework; expected là số case >0 và có tên case bắt buộc. Một exit code 0 với zero discovery hoặc native skipped không phải PASS.

## T01 — Reproducer và phép quan sát đúng lớp

**Files:**

- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxSizingTests.cs`.
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayNativeVisualRegressionTests.cs`.
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/RoundedPopupScenario.cs` nếu cần common-controls-v6 host; sửa `Program.cs` để chọn scenario, giữ default ListView.
- Modify khi cần cho DPI awareness: `tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/app.manifest`, project file; create `App.config` cho net48 nếu chưa có. Không thêm test runner framework.
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlaySurfaceTests.cs`, `BootstrapOverlayDropDownTests.cs` để thêm oracle mạnh hơn, giữ historical cases.

**Interfaces:** scenario entry `internal static int RoundedPopupScenario.Run(string outputDirectory)`; helper host nhận `--scenario rounded-popup --output-dir <temp-path>`; không thay default khi không có args. Capture/metadata chỉ là test infrastructure, không public toolkit API.

- [ ] **Bước 1 — Baseline và history guard.** Đọc spec mục 3 và kiểm các đoạn Opaque/Button, Combo handle-free, inner host và AA envelope vẫn có. Ghi actual HEAD, OS/runtime/bitness, source pin consumer nếu chạy app. Rebase docs baseline nếu main đã đổi bằng diff có review.
- [ ] **Bước 2 — Thêm reproducer sizing.** `DefaultClosedHeightMatchesTextBoxAfterHandleCreation`: tạo TextBox/Combo cùng font Segoe UI 10, theme, parent; Show + bounded pump; assert actual outer heights bằng nhau ở baseline default. `AutoSizeFlowLayoutContainsBottomBorderInSingleAndWrappedRows`: generic FlowLayoutPanel giống consumer, có TextBox width 210, Combo 140, Button 110; thử available width 600 và 430, assert actual bottom+margin nằm trong display rectangle. Thêm hàng chỉ có Combo và sequence PerformLayout trước khi tạo handle rồi mới Show. Log Height/ItemHeight/PreferredHeight/GetPreferredSize/native rects; không assert readonly inherited PreferredHeight luôn bằng outer target.
- [ ] **Bước 3 — Thu hình popup thật.** Thiết lập DPI awareness trước mọi HWND: net8 dùng startup PerMonitorV2 phù hợp; net48 dùng config opt-in PerMonitorV2, Windows 10 compatibility và EnableVisualStyles theo Microsoft. Giữ default ListView scenario và chạy lại focused host test nếu config dùng chung đổi. Log process/thread/window awareness, GetDpiForWindow và physical display scale; render DPI-virtualized 96 không được tính là gate 125%. Mở Select local items và Lookup local grid/footer; dùng nền owner sáng/tối/tương phản có pattern nhận biết. Chụp desktop rectangle gồm popup và margin 12 device pixels, ghi GetWindowRect, native region bounds, content/footer bounds và theme. Capture phải có nội dung trung tâm hợp lệ và popup Visible để loại ảnh blank/occluded. Giữ raw PNG ngoài Git.
- [ ] **Bước 4 — Sửa oracle.** Trong bitmap tests, samples AA phải có A>0 và nằm trong band stroke/transition xác định độc lập; A=0 hoặc màu đen bất thường trong band không được tính là thành công. Native RGB oracle kiểm bốn góc/bốn cạnh, border continuity, contamination của child màu Fuchsia, độ lệch đối xứng tối đa 1 device pixel; không đòi native shadow đối xứng. Mask reference không lấy trực tiếp từ Region đang test.
- [ ] **Bước 5 — Chạy và ghi baseline.** Sizing mismatch dự kiến RED trên cấu hình tái hiện; tests về live radius/measurement/embedded border sẽ có RED tại T05. Nếu popup capture chưa tái hiện complaint, vẫn ghi ảnh và chỉ gọi phần liên hệ nguyên nhân là hypothesis; không viết test giả luôn fail hoặc gọi bản sửa thành công. Kiểm các fixes cũ hiện có không bị mô tả là thiếu.
- [ ] **Bước 6 — Commit reproducer/observation.** Message: `test: capture native input sizing and popup corner baselines`.

**Verification:**

```powershell
.\test.ps1 -Configuration Release -Scope Controls -Framework All -Filter "FullyQualifiedName~BootstrapComboBoxSizingTests|FullyQualifiedName~BootstrapOverlaySurfaceTests|FullyQualifiedName~BootstrapOverlayDropDownTests|FullyQualifiedName~BootstrapOverlayNativeVisualRegressionTests"
```

Host dùng bounded wait tương tự helper ListView hiện có (30s process deadline, kill đúng child process + capture stderr khi timeout); UI loop theo `WinFormsMessageLoopTestHost`. Không thêm runner framework hoặc automated modal wait.

## T02 — Một nguồn hình học và pixel policy cho shell

**Files:** create hai rendering files trong bảng; create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Rendering/BootstrapRoundedSurfaceGeometryTests.cs`, `BootstrapRoundedSurfacePainterTests.cs`; sửa `Controls/BootstrapTextBox.cs` làm consumer đầu tiên.

**Interfaces sản xuất:**

```csharp
internal readonly struct BootstrapRoundedSurfaceGeometry
{
    // Immutable value data: OuterBounds/StrokeBounds/InnerBounds (RectangleF),
    // OuterRadius/StrokeRadius/InnerRadius (CornerRadius), BorderWidth (float),
    // HasUsableInterior (bool) for the explicit degenerate-contour policy.
    internal static BootstrapRoundedSurfaceGeometry Resolve(
        RectangleF outerBounds, CornerRadius outerRadius, float borderWidth);
    internal Padding GetDecorationInsets(float rasterGuardPixels);
    internal GraphicsPath CreateOuterPath();
    internal GraphicsPath CreateStrokePath();
    internal GraphicsPath CreateInnerPath();
}

internal static class BootstrapRoundedSurfacePainter
{
    internal static void Paint(Graphics graphics,
        BootstrapRoundedSurfaceGeometry geometry, Color surface, Color border);
}
```

Không giữ bitmap/GraphicsPath/Region shared trong struct. Các return path là tài nguyên mới caller dispose. Negative border/guard invalid; tiny/nonpositive bounds trả geometry/path rỗng an toàn; zero radius có rectangle sắc đúng bounds.

- [ ] **Bước 1 — Test invariants độc lập.** `FocusStrokeKeepsOuterContourStable`, `DecorationRectangleStaysInsideInnerContour`, `TinyBoundsNeverProduceNegativeContent`, `ZeroRadiusHasSymmetricEdges`, `AsymmetricCornersRemainContained`. Dùng b=1/2/1.25/2.5, R=0/6/16, bounds 140x80 và 12x8; thêm exact case corners (0,8,0,0), bounds 12x8, b=2 để bắt derived-radius renormalization. Test containment/symmetry, không chỉ chép lại công thức expected.
- [ ] **Bước 2 — Implement geometry.** Normalize outer radius một lần; suy stroke/inner theo spec 5.2. Trước khi gọi RoundedPath.Create, kiểm derived radii còn fit inset bounds; helper hiện tại tự NormalizeTo nên không được âm thầm dựa vào bước đó. Trường hợp suy biến dùng HasUsableInterior=false, inner/content rỗng và tô border bên trong outer path ổn định; không hứa stroke đều khi không còn chỗ. Decoration inset theo spec 5.3, tính float rồi ceil một lần ở boundary native. Translate inner path về host coordinates, không normalize lại theo host integer bounds.
- [ ] **Bước 3 — Chốt pixel policy bằng probe.** So sánh None/Half trên shape-only raster với outer bounds theo pixel edges. Chọn một policy đạt đối xứng và continuity ở 96/120/144/168/192 với stroke lẻ/chẵn; ghi lựa chọn trong test/doc. `Half` là candidate ban đầu; không đặt nó đại trà mà thiếu probe. Painter Save/Restore toàn bộ Graphics state, dùng AA cho shape, không thay text mode.
- [ ] **Bước 4 — Áp dụng cho TextBox shell.** Giữ palette, invalid/valid precedence, native editor, multiline, placeholder, accessories, connected per-corner radius. Background của embedded control tiếp tục theo parent paint; opaque popup backing là trách nhiệm T04.
- [ ] **Bước 5 — Verify.** New geometry/painter tests cùng `BootstrapTextBoxTests`, `BootstrapTextBoxAccessoryTests`, `BootstrapTextBoxMultilineTests`, `BootstrapTextBoxValidationLayerTests`, `BootstrapConnectedControlContractTests`; bảng native trước/sau ở 96/120. Không kết luận geometry toàn toolkit xong khi chỉ mới TextBox.
- [ ] **Bước 6 — Commit.** `fix: unify rounded surface geometry and textbox painting`.

**Run:** `test.ps1 -Scope Controls -Framework All` với filter `FullyQualifiedName~BootstrapRoundedSurface|FullyQualifiedName~BootstrapTextBox|FullyQualifiedName~BootstrapConnectedControlContractTests` và configuration Release.

## T03 — ComboBox outer height, layout và shell

**Files:** `Controls/BootstrapComboBox.cs`, `BootstrapComboBoxRenderLogic.cs`, new `BootstrapComboBoxSizingLogic.cs`; existing Combo tests và T01 sizing fixture.

**Interfaces:** `internal readonly struct BootstrapComboBoxSizing` gồm `OuterHeight`, `SelectionHeight`, `ItemHeight`; `BootstrapComboBoxSizingLogic.Resolve(int targetOuterHeight, int nativeChromeHeight, int minimumFieldHeight)` trả sizing vật lý. minimumFieldHeight là glyph+vertical padding bên trong selection field, chưa gồm nativeChromeHeight; OuterHeight tối thiểu bằng tổng hai phần này. `ApplyOwnerDrawMetrics()` vẫn là điểm apply sau handle; thêm guard `_applyingSizeMetrics`, cờ initialization và một hàm recompute dùng chung. Không tạo HWND từ Resolve/constructor. Chỉ áp policy mới cho closed DropDown/DropDownList trong supported owner-draw mode; Simple/mode khác giữ native behavior được test rõ.

- [ ] **Bước 1 — Pin behavior bằng tests.** T01 RED phải đo actual HWND. Thêm `ConstructionDoesNotCreateHandle`, `FontAndThemeChangesDoNotAccumulateHeightOrSelectionEvents`, `HandleRecreationRestoresDefaultOuterHeight`, `CallerRegionAndExplicitFontRemainOwnedByCaller`, `LargeFontUsesSafeMinimumWithoutClipping`. Dùng cả DropDown/DropDownList, bound/unbound một representative; không nở matrix của mọi state.
- [ ] **Bước 2 — Đo native overhead trên từng TFM.** Sau handle, lấy outer window/selection-field item height bằng API native thích hợp, phân biệt client/window coordinates. Kiểm kết quả bằng `GetComboBoxInfo`/`CB_GETITEMHEIGHT` và window rect; overhead không được hardcode6. Đo font physical hiện tại, không scale Font.Height lần nữa.
- [ ] **Bước 3 — Tách metrics.** Default outer target=Scale(ControlHeight), safe minimum đủ glyph/padding/native overhead. Giải native selection/item heights hợp lệ; tôn trọng ràng buộc native giữa selection field và list item. Document row-height delta nếu popup items thay đổi. Không để dropdown row lớn hơn khả năng native được chứng minh; nếu cần row policy khác phải có native test.
- [ ] **Bước 4 — Spike đồng bộ bounds có giới hạn.** Candidate dùng protected APIs: trong supported mode, override SetBoundsCore để giữ x/y/width, chuẩn hóa toolkit-owned closed height, OR BoundsSpecified.Height khi forward; có initialization/reentrancy guard và suspend parent layout quanh toàn bộ thao tác. Base Combo có thể clamp theo PreferredHeight: trước handle, sau base call dùng protected UpdateBounds(Left,Top,Width,target) với vị trí/width đã được base chấp nhận; không dùng lại proposed width nếu base vừa áp constraint, không gọi ItemHeight hoặc tạo HWND. Sau handle chỉ dùng UpdateBounds() đọc actual HWND sau native calibration, không bịa managed rect. Đo overhead rồi apply một lần, cho phép một correction dựa trên phép đo; không loop tới khi nhìn có vẻ đúng. Chứng minh candidate trên net48 và net8 trước khi chốt production.
- [ ] **Bước 4a — Gate layout/lifecycle của spike.** Override GetPreferredSize chỉ đổi height trong supported mode, giữ width policy/constraints hiện có; mode khác delegate base. Effective outer target, actual native bounds và parent allocation phải khớp sau settle. Kiểm Combo-only non-AutoSize flow, mixed single/wrapped rows, pre-handle layout, font tăng/giảm, theme/DPI/style và handle recreation; không layout churn. Chạy cả FormattingEnabled=false/true và giữ nguyên giá trị. PreferredHeight không virtual chỉ là diagnostic. MinimumSize/MaximumSize native setters loại bỏ Height, nên không đề xuất minimum-height floor, cast Control hay overridden getter như workaround đơn giản. Không private reflection, cache clearing, width/height jiggles hoặc constructor ItemHeight.
- [ ] **Bước 5 — Shell geometry.** Dùng T02 cho stroke/outer/Region theo window/client mapping đã đo; vẫn clip native corners và tôn trọng caller Region opt-out. Không bỏ base.WndProc, không vẽ đè nội dung native bằng text model thứ hai. Kiểm focus dày border, bốn góc và zero radius bằng native capture.
- [ ] **Bước 6 — Verify và update test cũ.** Thay expectation coi ControlHeight là ItemHeight bằng assertions phân biệt outer/selection/item, kèm native sizing replacement. Giữ native selection/formatting/IME/keyboard và ctor tests. Single-row, wrapped-row và Combo-only FlowLayout phải pass trên cả hai targets; repeated layout không thay đổi size lần sau.
- [ ] **Bước 7 — Commit.** `fix: align native combo outer height with input metrics`.

**Run:** `.\test.ps1 -Configuration Release -Scope Controls -Framework All -Filter "FullyQualifiedName~BootstrapComboBox"`.

Nếu chưa đạt native/layout invariant mà không làm đổi một contract bị cấm, ghi failure cụ thể và dừng phần T03 ở trạng thái chưa đạt; không thay bằng consumer padding hoặc hide readonly API. Các task popup độc lập vẫn tiếp tục được.

## T04 — Popup shell: backing, content safety, Regions và measurement

**Files:** `Controls/BootstrapOverlaySurface.cs`, `BootstrapOverlayDropDown.cs`, `BootstrapOverlayToolStripRenderer.cs`; T02 helper khi cần; existing overlay tests + native fixture.

**Interfaces:** surface thêm internal `Padding GetEffectiveContentInsets(Size outerSize)` làm nguồn duy nhất cho đo/layout; dropdown thêm `internal void SynchronizeSurfacePresentation()` để set opaque backing và clone latest owned surface Region, gọi từ ApplyBounds và presentation refresh. Không thêm public popup API.

- [ ] **Bước 1 — Regression trước sửa.** `OpaqueBackingHasNoTransparentOrBlackFringeInStrokeBand`, `DecorativeInsetsAreChargedOnceInPreferredSizeAndLayout`, `CallerContentPaddingAndRegionSurviveAttachDetach`, `NativeChildCannotCoverReservedCornerBand`, `ThemeChangeWithoutResizeRefreshesBackingAndClone`. Case cùng-size theme dùng để harden invariant, không ghi baseline đã có stale-region bug.
- [ ] **Bước 2 — Một hàm inset và kiểm fit.** `GetEffectiveContentInsets` dùng float theme metrics + T02 geometry trên candidate outerSize, cộng scaled LogicalContentPadding; layout dùng cùng hàm trên ClientSize. Preferred-size có tối đa 4 pass rồi bắt buộc kiểm final candidate: content preferred trong final viewport + actual insets phải fit trên các trục không bị cap. Nếu chưa fit, lấy inset upper bound từ requested radius chưa normalize, đo child với constraints trừ upper bound, tạo candidate bảo thủ rồi kiểm lại; lấy max các candidate cũ riêng lẻ không chứng minh đủ chỗ. Trục bị cap giữ contract scroll/clip; trục tự do vẫn thiếu là fail cần sửa thuật toán. Không recursion vào surface.GetPreferredSize. Tests: proposed Size.Empty của Popover, content 1000x1000 với radius rất lớn, tiny bounds, asymmetric corners và repeated measurement không tăng vô hạn.
- [ ] **Bước 3 — Paint backing xác định.** Surface.OnPaintBackground fill opaque Surface trong client/clip; dropdown đồng bộ BackColor và renderer override thực sự FillRectangle bằng màu đó trong background clip. Gán BackColor một mình không đủ vì renderer hiện tại rỗng. Renderer tiếp tục bỏ native ToolStrip border. Không sửa bằng parent BackColor của owner form hoặc screen-copy vào production. Shape painter T02 vẽ fill/stroke trên nền này.
- [ ] **Bước 4 — Vùng content an toàn, không cộng viền hai lần.** d trong spec là inset từ outer edge, đã gồm border. Nếu host origin ở h=ceil(b), host extra padding từng cạnh=max(0,d-h)+callerPadding; tổng outer chrome=decorationInsets+callerPadding, không cộng lại 2b. Giữ host owned hiện có, translate inner clip đúng tọa độ. Bounds thiếu chỗ hoặc geometry suy biến trả content rectangle rỗng; không nới guard. Child grid/footer/highlight ở rectangle an toàn ngay cả LogicalContentPadding.Empty. Giữ caller Region nguyên instance/semantics và dispose đúng owner.
- [ ] **Bước 5 — Region và presentation sync.** Outer Region vẫn là superset của fill+stroke+1 device-pixel fringe, không exact-cut paint path. Tạo từ common geometry; không tăng allowance. Clone sang dropdown sau size/theme/DPI/radius changes kể cả bounds giữ nguyên; detach event nếu dùng RegionChanged. Giữ `_boundsGeneration`, queued correction và native no-activate positioning.
- [ ] **Bước 6 — Verify.** Existing all-corner/five-DPI bitmap coverage, actual shown popup trên dark/light, caller content region, attach/detach/dispose. Chạy `BootstrapPopoverTests` vì dùng shared host. So sánh shadow default và diagnostic off; không đổi default để giấu viền lỗi.
- [ ] **Bước 7 — Commit.** `fix: make overlay backing and rounded content geometry consistent`.

**Run:** `.\test.ps1 -Configuration Release -Scope Controls -Framework All -Filter "FullyQualifiedName~BootstrapRoundedSurface|FullyQualifiedName~BootstrapOverlay|FullyQualifiedName~BootstrapPopoverTests"`.

**Gate visual:** binary outer mask vẫn được ghi như giới hạn; không gọi mask coverage PASS là alpha-composition PASS. Nếu actual capture còn halo/hở đoạn, phân loại pixel theo backing/stroke/child/shadow trước khi sửa tiếp; không áp bản vá offset mới cho riêng từng góc.

## T05 — Select/Lookup: measurement, embedded border, DPI và live radius

**Files:** Select/Lookup files trong bảng; `Controls/BootstrapDataGridView.cs`, `Controls/BootstrapTextBox.cs`; existing `BootstrapSelectPopupTests`, `BootstrapLookupPopupTests`, `BootstrapLookupDropDownContentTests`, `BootstrapSelectDropDownContentTests`.

**Interfaces nội bộ mới:**

- `BootstrapDataGridView.UseEmbeddedBorderlessPresentation` (`internal bool`, default false); setter apply presentation; private ApplyTheme dùng None khi true, FixedSingle khi false. Public API/standalone default không đổi.
- `BootstrapTextBox.OnFrameworkBorderRadiusChanged()` (`internal virtual void`, default no-op); BorderRadius setter gọi sau field mutation. Lookup override gọi controller geometry refresh nếu đã tạo, không mở popup mới.
- `RefreshGeometryAndLayout()` (`internal void`) ở cả Select/Lookup controllers: chỉ cập nhật surface radius, decoration layout, Region clone và placement khi đang mở; không đi qua full content presentation. Existing Select RefreshDropDownPresentationAndLayout và Lookup ApplyCurrentPresentationToContent có tác dụng điều hướng, không dùng cho radius-only path.
- `BootstrapLookupFooter.ApplyMetrics(int dpi)` và `BootstrapLookupDropDownContent.ApplyPresentationMetrics(int dpi)` (`internal void`): cập nhật owned layout metrics, không giả rằng synthetic DPI đổi DPI thật của native child.

- [ ] **Bước 1 — Viết 3 regression RED cụ thể.** `SelectOuterPreferredHeightIncludesDecorationForUncappedResults`: ít item, còn đủ work area, SearchEnabled on/off, assert content required height+chrome bằng outer height. `LookupEmbeddedGridStaysBorderlessAcrossThemeAndReopen`: grid popup None nhưng standalone FixedSingle trước/sau theme. `OpenPopupRadiusChangesWithoutRecreatingHandleOrSelection`: -1→0→12 khi mở, same HWND, contour đổi; trước đó wheel scroll ra xa highlighted row, rồi kiểm query/highlight/scroll/caret/value/event counts. Chỉ cho phép clamp scroll vào range mới khi viewport bắt buộc thay đổi, không tự đưa highlight về màn hình.
- [ ] **Bước 2 — Select measurement.** ComputeBounds gọi `_surface.GetPreferredSize(new Size(width,maxHeight))`; giữ minimumHeight, outer MaxDropDownHeight và placement engine. Khi cap thì results scroll; search và footer không bị tính như một dòng result. Verify last result row không bị mất do trừ border lần thứ hai.
- [ ] **Bước 3 — Lookup border policy.** Set internal policy khi tạo ResultsGrid và reassert invariant cần thiết; ApplyTheme phải quan sát policy nên không dựa vào thứ tự ThemeChanged handlers. Giữ cell/header separators và readonly/selection rules. Recompute preferred size bằng actual BorderStyle sau policy, kiểm horizontal scrollbar và footer còn thấy được.
- [ ] **Bước 4 — Owned DPI.** Truyền controller effective DPI tới content/footer metric methods trước đo outer size. Font/editor actual DPI vẫn do WinForms; không double-scale Rows/column widths của consumer. Test96→120→144 metric transitions, metadata ghi actual HWND DPI riêng.
- [ ] **Bước 5 — Live radius notification không reconcile data.** Select setter gọi geometry-only refresh; Lookup dùng internal TextBox hook. Guard disposed/uninitialized/controller-null; không tạo popup/handle khi đổi radius trên control chưa hiển thị. Refresh shell/layout/Region clone theo T04. Bypass Select results ApplyPresentation/EnsureHighlightedVisible và Lookup ApplyCurrentContent/SynchronizeHighlightedResult; không ClearSelection, gán CurrentCell hoặc FirstDisplayedScrollingRowIndex chỉ vì đổi radius. Reposition không được gián tiếp gọi lại full presentation. Theme/DPI paths có đổi metrics vẫn phải bảo toàn navigation hợp lệ và chỉ clamp khi cần.
- [ ] **Bước 6 — Verify interactions.** Giữ Select search focus + first-click, Lookup owner-editor focus + per-key typing, Up/Down/Enter/Escape/Tab, footer actions, outside pointer không consume click, header/blank viewport/scrollbar focus return. Đổi presentation không phát selection/commit events giả và không đóng/mở popup.
- [ ] **Bước 7 — Commit.** `fix: align select and lookup popup presentation with shared shell`.

**Run:** `.\test.ps1 -Configuration Release -Scope Controls -Framework All -Filter "FullyQualifiedName~BootstrapSelectPopupTests|FullyQualifiedName~BootstrapSelectDropDownContentTests|FullyQualifiedName~BootstrapSelectInteractionTests|FullyQualifiedName~BootstrapLookupPopupTests|FullyQualifiedName~BootstrapLookupDropDownContentTests|FullyQualifiedName~BootstrapLookupInteractionTests|FullyQualifiedName~BootstrapDataGridView"`.

## T06 — Đồng nhất các shell còn lại và compatibility

**Files:** Button, NumericBox, DatePicker, closed Select/render logic; gallery `RenderingDemoForm.cs`; tests tương ứng. Lookup kế thừa TextBox shell, không tạo painter trùng.

**Interfaces:** dùng T02 Resolve/Paint; giữ per-control palette/focus cues và IBootstrapConnectedControl contracts. Nếu caller đặt Size/Font, chỉ cải thiện shape trong bounds đó; không tự đổi size mọi control khi theme đổi.

- [ ] **Bước 1 — Tests theo contour.** `EquivalentInputMetricsProduceSameOuterCornerContour`, `FocusDoesNotMoveOuterBoundsOrTextBaseline`, `ExplicitSizeAndConnectedCornerOverridesRemainEffective`. Thêm test repaint parent/background sau resize; giữ Opaque=false fix của Button.
- [ ] **Bước 2 — Áp helper theo từng painter.** TextBox/Lookup, Combo, Select, NumericBox, DatePicker và Button cùng outer-radius convention. Giữ Button dotted focus cue nếu contract hiện có; không ép mọi control cùng một focus gesture. Native DateTimePicker interior và Combo arrow/popup vẫn là native exception được mô tả.
- [ ] **Bước 3 — Large font và custom size.** T06 chỉ chuyển shell painting, không giả định đang có cờ default-size mode hoặc thêm hệ thống theo dõi size ownership. Kiểm 9pt/10pt mặc định bằng chiều cao, 16pt stress và explicit/multiline sizes không phát sinh clipping do painter mới. Safe minimum Combo thuộc T03; font lớn trên các controls còn lại theo size policy hiện hành, không tự ép mọi control về 32 hoặc tự cân bằng cả hàng. Ghi compatibility nếu thay đổi quan sát được.
- [ ] **Bước 4 — Board so sánh.** RenderingDemoForm có một hàng input cùng font và mô phỏng flow một/hai hàng; có Select/Lookup mở popup với first/last highlight, empty, footer, scrollbar. Cho xem radius 0/default/12, Light/Dark, state thường/focus/invalid/disabled. Không đổi model nghiệp vụ hay layout gallery khác ngoài phần board.
- [ ] **Bước 5 — Verify controls và adapters.** Focused fixtures cho các painter bị đổi, connected controls; adapter `BootstrapTextBoxEditorTests`, `BootstrapFormattedTextBoxEditorTests`, `BootstrapLookupBoxEditorTests`, `BootstrapLookupBoxInteractionTests` trên hai TFM. BootstrapDropdown/Calendar chỉ cần mở rộng nếu diff thật sự sửa primitive họ dùng; không suy chúng là shared-overlay consumers.
- [ ] **Bước 6 — Commit.** `fix: apply consistent rounded contours across input shells`.

## T07 — Nghiệm thu native, tài liệu và consumer handoff

**Files:** create `docs/verification/20261029-input-popup-rendering.md`; update `docs/components/controls/COMPONENTS.md`, `DESIGN_SYSTEM.md`, `BOOTSTRAP_SELECT.md`, `BOOTSTRAP_LOOKUP_BOX.md`, `COMPATIBILITY.md`; cập nhật trạng thái plan và entry pages khi có kết quả thật.

- [ ] **Bước 1 — Build final candidate.** Build All một lần sau code freeze để cả targets/adapters có artifact mới; ghi candidate SHA/config/SDK/OS. Sau đó mới dùng SkipBuild cho các focused commands bên dưới.
- [ ] **Bước 2 — Chạy matrix hữu hạn.** Geometry/bitmap giữ 96/120/144/168/192. Native canonical popup với metric-DPI inputs chỉ ghi synthetic nếu HWND DPI không đổi. Physical acceptance tối thiểu 100%/125% Light/Dark cho input row + Select + Lookup; ghi DPI-awareness mode và actual DPI để loại render 96 được desktop phóng thành 125%. Representative 150%/200% khi có môi trường, ghi NOT RUN nếu thiếu. Không nhân toàn bộ matrix radius×theme×size×state×TFM; thêm pairwise radius 0, tinybounds/largeR, thickborder/opaquechild, resize/theme/reopen ở 96/120.
- [ ] **Bước 3 — Explicit native keyboard/mouse gate.** Chạy exact first-click method riêng; hai cases content-click/arrow-click phải executed=2 cho mỗi framework. Giữ Lookup native input/outside-click/footer/caret tests; một broad-filter PASS với explicit cases skipped chưa đạt gate này.
- [ ] **Bước 4 — Visual evidence.** Full PNG 1:1 và bốn corner crops, metadata, before/after cùng scale; controlled dark/light/checker backdrop; normal/focus, first/last selected result, grid header/footer/scrollbar. Native screenshot phải chụp cả exterior band; DrawToBitmap/client-only không thay phần composition/shadow.
- [ ] **Bước 5 — Consumer handoff riêng repository.** Trong report ghi exact toolkit commit để MyDmsVn pin và files cần kiểm: SecurityPermissionCatalogView + catalog UI tests, cùng màn hình dùng Select/Lookup. Bản sửa toolkit phải có generic reproducer tương đương. Consumer PR kế tiếp cập nhật gitlink, kiểm hàng filter một/hai dòng ở100%/125%; không đánh dấu consumer đã PASS chỉ vì toolkit pass, không sửa business/SQL trong toolkit PR.
- [ ] **Bước 6 — Report và completion.** Ghi discovered/executed/pass/fail/skip từng framework/filter, explicit case counts, actual DPI/physical scale, native limitations, URLs/artifactIDs có thể mở lại. Mỗi defect phải có test hoặc manual step quan sát được; Owner quyết định hình ảnh được ghi riêng. Không tự ghi APPROVED hay CLOSED khi còn native/consumer phần bắt buộc chưa đạt.
- [ ] **Bước 7 — Commit docs/evidence.** `docs: record candidate verification for input and popup rendering`.

### Focused final commands

Các biến project đã khai báo ở mục 2. Sau final build thành công, bổ sung fixture mới đúng tên và kiểm discovery:

```powershell
$controlsFilter = "FullyQualifiedName~BootstrapRoundedSurface|FullyQualifiedName~BootstrapComboBox|FullyQualifiedName~BootstrapOverlay|FullyQualifiedName~BootstrapPopoverTests|FullyQualifiedName~BootstrapSelectPopupTests|FullyQualifiedName~BootstrapSelectDropDownContentTests|FullyQualifiedName~BootstrapSelectInteractionTests|FullyQualifiedName~BootstrapSelectVisualRegressionTests|FullyQualifiedName~BootstrapLookupPopupTests|FullyQualifiedName~BootstrapLookupDropDownContentTests|FullyQualifiedName~BootstrapLookupInteractionTests|FullyQualifiedName~BootstrapLookupLifecycleTests|FullyQualifiedName~BootstrapDataGridView|FullyQualifiedName~BootstrapTextBox|FullyQualifiedName~BootstrapNumericBox|FullyQualifiedName~BootstrapDatePicker|FullyQualifiedName~BootstrapButton|FullyQualifiedName~BootstrapConnectedControlContractTests"
foreach ($targetFramework in @("net48", "net8.0-windows")) {
    dotnet test $controlsProject -c Release -f $targetFramework --no-build --no-restore --list-tests --filter $controlsFilter --blame-hang --blame-hang-timeout 5m
}
.\test.ps1 -Configuration Release -Scope Controls -Framework All -SkipBuild -Filter $controlsFilter

foreach ($targetFramework in @("net48", "net8.0-windows")) {
    dotnet test $controlsProject -c Release -f $targetFramework --no-build --no-restore --filter "FullyQualifiedName~NativeFirstClickFromAnotherControlKeepsPopupAndSearchFocus" --blame-hang --blame-hang-timeout 5m
}

$gridFilter = "FullyQualifiedName~BootstrapTextBoxEditorTests|FullyQualifiedName~BootstrapFormattedTextBoxEditorTests|FullyQualifiedName~BootstrapLookupBoxEditorTests|FullyQualifiedName~BootstrapLookupBoxInteractionTests"
foreach ($targetFramework in @("net48", "net8.0-windows")) {
    dotnet test $gridProject -c Release -f $targetFramework --no-build --no-restore --list-tests --filter $gridFilter --blame-hang --blame-hang-timeout 5m
    dotnet test $gridProject -c Release -f $targetFramework --no-build --no-restore --filter $gridFilter --blame-hang --blame-hang-timeout 5m
}
```

Discovery cần liệt kê case trọng tâm; command lists không tự chứng minh chúng đã chạy. Dùng DataGridViewTestGuard/exception capture hiện có khi host Lookup grid. Native capture prerequisites thiếu thì ghi BLOCKED và giữ task nghiệm thu mở, không silently skip rồi tính PASS.

## 3. Definition of Done

- [ ] Default Combo/TextBox/Button đạt outer height thống nhất với cùng theme/font tiêu chuẩn/DPI; Combo font lớn dùng safe minimum; custom sizes của shell khác được giữ. Combo-only và mixed FlowLayout đều không cắt đáy; constructor vẫn handle-free.
- [ ] Bốn góc shell/popup liên tục, border không bị child/native frame phủ, backing không có màu lạ; focus không đổi outer contour.
- [ ] Lookup embedded grid borderless qua theme/reopen; standalone grid giữ behavior cũ; Select tính đủ chrome; footer/last row có không gian hoặc scroll đúng khi bị cap.
- [ ] Open popup nhận radius/theme/DPI mới mà không recreate/reset state; giữ native input/activation/selection semantics và caller ownership.
- [ ] Retained regression và focused native tests pass trên hai TFM; physical 100%/125% evidence có thể mở lại; historical PASS không được copy.
- [ ] Generic toolkit fix có consumer handoff exact pin; trạng thái consumer nghiệm thu được ghi riêng.
- [ ] Owner visual decision được ghi theo candidate nếu có; thiếu quyết định hoặc thiếu native evidence thì report nêu đúng phần còn lại.

## 4. Prompt giao Codex

```text
Đọc AGENTS.md, docs/ROADMAP.md, docs/design/20261029-input-popup-rendering.md
và docs/plans/20261029-001-input-popup-rendering.md. Triển khai theo T01–T07,
giữ targets net48/net8.0-windows và native contracts. Đối chiếu baseline hiện tại,
giữ các sửa PR32/PR48 được liệt kê trong spec, không lặp lại bằng việc chỉ thêm
AntiAlias/Region/padding. Viết regression RED cho sizing/measurement/live-radius/
embedded-border trước khi sửa; phân biệt bitmap và actual displayed popup.
Mỗi task commit riêng, kiểm focused tests đúng owner, không chạy SQL.
Nếu môi trường không có Windows desktop, hoàn tất phần source/cross-build có thể
làm, ghi NOT RUN/BLOCKED đúng các native gates và chuẩn bị lệnh/evidence handoff;
không tự ghi visual/consumer acceptance PASS hoặc Owner APPROVED.
Kết thúc báo candidate, task đã xong/còn thiếu, commands/case counts và artifact links.
```

Plan này sở hữu phạm vi mới về input/popup rendering. Trạng thái A3 và ba ngoại lệ Business Compact ngày 28/10 vẫn thuộc plan/evidence cũ; không archive hoặc mở lại chúng trong đợt sửa này.
