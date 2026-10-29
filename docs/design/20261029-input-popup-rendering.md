# Thiết kế sửa chiều cao input và độ liền của góc bo popup

Ngày: **29/10/2026**. Repository: **MyDmsVn.WinFormsKit**.

**Trạng thái:** thiết kế kỹ thuật cho [plan triển khai](../plans/20261029-001-input-popup-rendering.md); chưa triển khai, chưa có Windows/native acceptance cho candidate mới.

## 1. Yêu cầu và quyết định

Owner đã đồng ý hướng sửa chiều cao BootstrapComboBox và chuẩn hóa shell, sau đó yêu cầu phân tích thêm góc bo popup BootstrapSelect/BootstrapLookupBox, có plan đủ cụ thể để Codex không lặp lại những lần sửa cũ.

Mục tiêu là một đường bao ngoài cân đối, không đứt/mất góc, không viền kép hoặc màu nền lạ; ComboBox mặc định cùng chiều cao ngoài với TextBox; popup đo đủ phần trang trí và không để child native che đường viền. Giữ input, selection, binding, keyboard, accessibility và popup lifecycle hiện có.

Các lựa chọn triển khai sau là **đề xuất kỹ thuật của plan**, không phải bằng chứng Owner đã duyệt hình ảnh mới:

1. Giữ native-backed `BootstrapComboBox : ComboBox` và `ToolStripDropDown` hiện tại; sửa đúng lớp metrics, geometry, backing paint và embedded content.
2. Dùng helper nội bộ nhỏ cho hình học shell; tái sử dụng `RoundedPath`, `CornerRadius`, `DpiScaler`. Không đổi public API của các primitives này để buộc mọi control khác đổi theo.
3. Popup có một vùng trang trí do shell sở hữu, tính từ bán kính/viền và một guard khử răng cưa; native child nằm trong rectangle an toàn. `LogicalContentPadding` của caller được cộng riêng, không bị ghi đè.
4. Popup không dùng `Clear(Color.Transparent)` để giả lập alpha trên HWND thường. Mọi pixel được phép hiển thị trong native Region phải có nền xác định.
5. Giữ native popup chrome của ComboBox theo contract hiện tại. Việc theme hóa toàn bộ native arrow/edit/popup hoặc thay consumer bằng BootstrapSelect là scope riêng; plan này không âm thầm thay selector model.

### Các hướng đã cân nhắc

| Hướng | Đánh giá |
|---|---|
| Chỉnh radius/padding/Region từng control | Không chọn: bỏ sót lớp backing/child/measurement; các sửa đó đã từng xuất hiện trong lịch sử. |
| Giữ HWND và lifecycle, thống nhất geometry + opaque backing + vùng content an toàn | **Chọn cho plan này**: sửa được kích thước, đường border, mảng màu thừa, child overpaint và sai khác giữa control, có phạm vi kiểm chứng hữu hạn. |
| Popup layered/composited hoặc cửa sổ decoration phụ | Chưa chọn: cần thiết kế riêng cho native child HWND, activation, accessibility, hit testing và thứ tự cửa sổ. Không suy rằng `AllowTransparency=true` tạo per-pixel alpha. |

**Giới hạn vật lý phải nói rõ:** window `Region` là hard clip; phương án được chọn không hứa một silhouette alpha mượt trên mọi nền desktop như WPF/Web. Tiêu chí của đợt này là đường viền liên tục, màu backing đúng, bốn góc cân nhau và không child-overpaint. Nếu native capture còn cho thấy lỗi mà chỉ per-pixel composition mới giải quyết được, ghi phần còn lại chưa đạt và đưa thiết kế đó ra riêng; không tăng allowance/padding lặp lại rồi đánh dấu hoàn tất.

## 2. Baseline và độ chắc chắn

| Phạm vi | Baseline đã đọc |
|---|---|
| Toolkit | `fd317f0aa4242cc9a44b7554c54f96bb883b6357` trên main |
| Consumer MyDmsVn | `7e4dc2ae8ca2d5087d14206ad5fbb1bdb085fea5`; vendor WinFormsKit `a5dd3cec6daba71943549afac628e13727c00da5` |
| UI source import | `MyDmsVn.Bootstrap5WinFormUI@8ae217ea8aa9a938ebd558f7071dc737f909e6a5`, theo [PROVENANCE](../PROVENANCE.md) |
| Ảnh đầu vào | `BootstrapComboBox-is-higher-BootstrapTextBox.png`, `BootstrapComboBox-clip-bottom.png` do Owner cung cấp ngày 29/10/2026 |

Ảnh đầu tiên có TextBox 32 px, ComboBox 38 px; cả hai ảnh ghi DeviceDpi 96. Ảnh hẹp có nút Làm mới xuống dòng; ảnh rộng giữ một dòng và cắt đáy ComboBox sát grid. Chưa có ảnh popup riêng trong yêu cầu này. Không giả định binary trong ảnh trùng baseline source nếu chưa ghi được candidate của lần chạy.

Các file ComboBox/metrics liên quan giống nhau giữa toolkit main và pin consumer. `BootstrapOverlaySurface.cs` tại main và UI import có cùng blob `3c27b5f12491ec72ec01cc707bdad4a5790af234`; chưa có bằng chứng việc gom repository làm mất các sửa góc bo.

## 3. Lịch sử đã xác minh: bảo vệ nào phải giữ

| Nguồn upstream | Đã sửa | Điều phải giữ ở candidate mới |
|---|---|---|
| [PR #7, 89832f4](https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI/commit/89832f4c943b982915c1a27a3f1ca3521bbb1a82) | Button bỏ inherited Opaque để nền ngoài góc được vẽ lại. | Không đưa lỗi này thành việc chưa làm; giữ regression background clearing. |
| [PR #11, 8fbd524](https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI/commit/8fbd524d9607016f5127fb0bc8d479ec29d1e3ca) | Card dùng decoration-safe DisplayRectangle, tránh Header/Body/Footer che góc khi Padding nhỏ. | Tận dụng nguyên tắc geometry, không sửa Card diện rộng trong plan này. |
| [PR #32, a972e4f](https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI/commit/a972e4f2bd3cf3c7af35005e907006f78f028ac9) | ComboBox có native shell Region, refresh lifecycle và opt-out cho caller Region. | Constructor vẫn không tạo HWND; Region caller-owned vẫn được tôn trọng. |
| [PR #32, 1fde2de](https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI/commit/1fde2de7f674e90feddf54a85d8a0751041204b3) | Revert thử nghiệm gán ItemHeight trước handle, khôi phục handle-free construction. | Không bỏ `!IsHandleCreated` để chữa size trước constructor. |
| [Plan Select 9775368](https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI/blob/97753681438aa1026e9aacd35f647d334c6c6c88/docs/plans/20260831-003-bootstrap-select-border-rendering-fix.md), [PR #48](https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI/pull/48) | Closed Select dùng DPI/focus-aware stroke và AntiAlias; search editor có wrapper themed. | Không quay lại native FixedSingle search field hoặc inset cố định 0.5 ở mọi DPI. |
| [PR #48, 46b9174](https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI/commit/46b917439a0fef8b5987adda0897fccccb960518) | Inner content host có owned Region để opaque child không phủ cung bo. | Giữ ownership, detach/dispose và caller content Region; không thêm lại một host trùng chức năng. |
| [PR #48, 8783e04](https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI/commit/8783e0449381cb25c45c23724353fdf94fff83f0) | Widen outer Region bằng stroke + 1 device pixel AA allowance; thêm bitmap/all-corner và ShowAt/resize tests. | Fix này **đã tồn tại**. Giữ bảo vệ coverage; bổ sung kiểm chứng màu/composition trên HWND thật. |

PR #48 đã sửa tiếp finding răng cưa trước khi merge, không dừng ở finding đó. Các PASS/RED-GREEN lịch sử chỉ chứng minh baseline tương ứng.

## 4. Phân tích source hiện tại

### 4.1. ComboBox: token ngoài bị dùng cho vùng item

`BootstrapTextBox` gán `ControlHeight` cho Size.Height. `BootstrapComboBoxRenderLogic.ResolveMetrics` dùng `max(ControlHeight, Font.Height + 2*padding)` làm ItemHeight rồi `ApplyOwnerDrawMetrics` gán vào native ComboBox. Native vẫn có phần khung nên chiều cao ngoài tăng. Source Microsoft .NET 8 dùng `ItemHeight + 2*FixedFrameBorderSize.Height`; cần đo net48 để khẳng định native overhead trên môi trường consumer.

`SecurityPermissionCatalogView` dùng FlowLayoutPanel AutoSize/GrowAndShrink/WrapContents, không có hàng fixed-height. Khả năng mạnh là kích thước flow dùng để đo khác kích thước native sau handle; chưa khẳng định cached specified height cụ thể. Chỉ gọi PerformLayout hoặc ép Height ở consumer không thay bản sửa contract kích thước.

Không dùng `MinimumSize.Height` làm bản vá đơn giản: native ComboBox .NET 8 override cả MinimumSize/MaximumSize và lưu height bằng 0. `PreferredHeight` không virtual, còn có nhánh tương thích phụ thuộc FormattingEnabled; `GetPreferredSize` riêng lẻ cũng không sửa chắc chắn flow với child không AutoSize. T03 là một spike có giới hạn trên Windows: thử `SetBoundsCore` ghi đúng specified height, projected bounds trước handle bằng protected `UpdateBounds(...)`, rồi đọc actual HWND bằng `UpdateBounds()` sau khi hiệu chỉnh ItemHeight. Chỉ chấp nhận khi layout thật dành đủ chỗ trên cả hai targets; không dùng reflection private cache hay gán ItemHeight trong constructor.

### 4.2. Popup Select và Lookup dùng chung shell

Luồng hiện tại là controller → `BootstrapOverlayDropDown` → ToolStripControlHost → `BootstrapOverlaySurface` → owned content host → Select search/results hoặc Lookup grid/footer. Popover cũng dùng shared overlay; BootstrapDropdown/CalendarPicker dùng đường ToolStripDropDownMenu khác.

Những phần đã có: AntiAlias; half-stroke bounds; inner host clipping; outer Region có allowance 1 device pixel; clone Region vào dropdown mỗi ApplyBounds; theme/DPI refresh và reposition. Đổi theme cùng size vẫn đi qua Reposition, nên không ghi stale Region do same-size theme như một bug đã xác nhận.

### 4.3. Những điểm cần sửa hoặc tái hiện

| ID | Bằng chứng source | Phân loại và hành động |
|---|---|---|
| F01 | OverlaySurface.OnPaintBackground gọi `Clear(Color.Transparent)`; ToolStrip renderer bỏ trống background/border; host vẫn là HWND thường. | Backing không có quy tắc composition rõ. Xác minh halo bằng desktop capture; thay bằng nền opaque xác định trong vùng shell sở hữu. |
| F02 | Paint path dùng radius r trên rect inset b/2; inner host dùng r-b trên rect inset b; Combo còn có Region/paint geometry khác nhau. | Hình học chưa có cùng outer-radius convention. Suy ra stroke/inner từ một outer geometry. |
| F03 | Overlay border/radius scale số nguyên; closed Select/TextBox/Combo dùng float. | 125% có thể cho b=1/r=8 ở popup và b=1.25/r=7.5 ở closed shell. Giữ float cho paint; ceil chỉ khi cấp rectangle native. |
| F04 | Lookup ResultsGrid là BootstrapDataGridView; private ApplyTheme đặt FixedSingle; ReassertInvariants không bỏ frame này. | Khung chữ nhật thứ hai là source-confirmed. Thêm internal embedded-border policy chỉ cho grid của popup. |
| F05 | Select controller ComputeBounds đo `_content.GetPreferredSize`; Lookup đo `_surface.GetPreferredSize`. | Select chưa cộng chrome ở cùng tầng đo. Dùng outer surface measurement, giữ MaxDropDownHeight và placement limits. |
| F06 | Select.BorderRadius và TextBox.BorderRadius setter chỉ invalidate shell; controller chỉ đọc radius khi apply presentation. | Đổi radius lúc mở không cập nhật popup ngay. Bổ sung notification nội bộ và refresh không reopen. |
| F07 | Lookup controller có effective DPI; footer tự lấy DeviceDpi; content presentation chỉ truyền DPI tới columns. | Cần kiểm tra footer/header/chrome cùng effective DPI khi mở popup và khi chuyển DPI. Chỉ sửa metrics thuộc popup; không áp DPI giả vào grid do consumer sở hữu. |
| F08 | AA tests kiểm bitmap ARGB + Region, mô phỏng child composition; không kiểm màu của desktop popup thật. | Giữ tests cũ, thêm oracle không chấp nhận transparent/đen bất thường như AA và capture HWND thực. |
| F09 | Lookup presentation gọi `SynchronizeHighlightedResult`, đặt CurrentCell và FirstDisplayedScrollingRowIndex; Select results presentation gọi `EnsureHighlightedVisible`. | Không dùng toàn bộ presentation refresh để sửa F06: cần luồng geometry-only, nếu không popup có thể tự cuộn về highlight khi chỉ đổi góc bo. |

F01/F02/F03 là căn cứ kỹ thuật và giả thuyết liên hệ tới cảm giác chưa liền; không khẳng định chúng tái hiện đúng mọi pixel Owner thấy khi chưa có native capture. F04/F05/F06 có call path cụ thể để viết regression. Không coi mọi frame/chrome native là lỗi bo góc.

## 5. Quy tắc kỹ thuật chung

### 5.1. Metrics của input

- `ControlHeight` là chiều cao **ngoài** mặc định khi input đóng: 32 logical px tại 96 DPI; 40/48/56/64 tại 120/144/168/192 khi font tiêu chuẩn đủ chỗ.
- Contract bằng chiều cao chỉ áp cho controls mặc định cùng theme/font tiêu chuẩn 9–10 pt; không hứa tự đồng bộ toàn bộ hàng khi caller dùng font hoặc Size riêng. Combo dùng safe minimum theo glyph/padding/native overhead khi font lớn; các shell còn lại giữ size policy hiện hành và kiểm không phát sinh clipping mới.
- Native selection/item height được suy ra từ outer target và native overhead trên DPI hiện tại; không hardcode trừ 6, không dùng text Height làm source of truth cho khung.
- Constructor không tạo HWND. Tính lại sau handle/font/DPI/theme/style/handle recreation theo guard chống lặp, dùng một điểm apply; đồng bộ size phục vụ layout.
- Không bật FormattingEnabled hoặc thay auto-complete/formatting semantics chỉ để làm PreferredHeight trông đúng. Không reflection private WinForms trong production.
- Giới hạn Combo sizing mới: closed DropDown/DropDownList trong owner-draw mode được toolkit hỗ trợ. `Simple` và draw mode ngoài phạm vi này giữ native behavior, không bị ép thành input một dòng cao 32. Giữ width/position và semantics hiện có của caller Size/MinimumSize/MaximumSize.

### 5.2. Outer, stroke và inner geometry

Tạo internal `BootstrapRoundedSurfaceGeometry` và `BootstrapRoundedSurfacePainter` trong `Rendering/`.

- Normalize outer CornerRadius theo OuterBounds một lần.
- Centered stroke: StrokeBounds inset b/2; StrokeRadius = max(0, OuterRadius-b/2) theo từng góc.
- InnerBounds inset b; InnerRadius = max(0, OuterRadius-b).
- Kiểm derived radii có còn biểu diễn được trong inset bounds trước khi gọi `RoundedPath.Create`, vì helper này tự NormalizeTo lần nữa. Với case suy biến như bounds 12×8, corners (0,8,0,0), b=2, không cho phép normalize âm thầm làm dịch tâm cung. Giữ outer silhouette; đánh dấu interior không sử dụng được, content rectangle rỗng và fallback tô border trong outer path. Fallback hẹp này ưu tiên không rò pixel/exception, không hứa stroke đều trong bounds không còn đủ chỗ.
- Painting dùng float; native layout/region dùng rounding có chủ ý, không lấy int radius của popup làm nguồn ngược cho geometry.
- Geometry không giữ GraphicsPath/Region dùng chung; phương thức tạo trả tài nguyên mới, caller dispose.
- Painter save/restore Graphics state; chỉ áp AA/pixel policy cho shape, không thay text rendering hoặc clip của caller.
- PixelOffsetMode phải được chốt bằng probe mép trên/dưới, trái/phải và stroke lẻ/chẵn. `Half` là candidate ban đầu cho outer bounds theo pixel edges, không phải một flag tự chứng minh sửa xong. Ghi mode đã chọn và ảnh trước/sau; một policy cho nhóm painter, không có offset bí mật mỗi control.

### 5.3. Vùng an toàn cho content popup

Giữ owned content host hiện có. Tách **decoration inset** khỏi `LogicalContentPadding`.

Đặt `g = 1 device pixel` làm guard raster cho vùng content, không scale g như spacing. Tại mỗi góc radius R, cần một rectangle đủ nằm trong phần mặt phẳng sau border và guard. Candidate inset đối xứng theo tiếp tuyến/circle:

`d = ceil(max(b + g, R - max(0, R - b - g) / sqrt(2)))`.

Lấy max theo các góc trên mỗi cạnh. Với uniform R=6, b=1, g=1 ở 96 DPI, d=4 device pixels. **d đo từ mép ngoài và đã bao gồm border**. Nếu host đặt ở h=ceil(b), padding thêm bên trong host là `max(0,d-h)+callerPadding`; tổng chrome của hai cạnh đối xứng là `2*d+callerPadding`, không phải `2*b+2*d+callerPadding`. Nếu bounds quá nhỏ thì trả content rectangle rỗng; không giảm guard để nhét child vào đường viền. Dùng phép containment độc lập để kiểm rectangle an toàn, không chỉ test lại đúng phương trình.

Host region vẫn bảo vệ inner shape. Child content thực tế nằm trong rectangle decoration-safe cộng padding caller; vì vậy header/footer/highlight không phải tự clip theo một cung binary sát border. Native grid/footer không được vẽ tới vùng shell-owned AA. `GetPreferredSize` phải cộng đúng inset mới một lần, rồi controllers clamp outer bounds; không làm mất dòng cuối/footer.

Radius normalized phụ thuộc kích thước: phép đo phải nhận **candidate outer size**, không dùng ClientSize cũ. Iteration có giới hạn phải kiểm fit lần cuối; lấy max của vài candidate chưa đủ chứng minh hội tụ. Khi cần fallback, dùng inset upper bound từ requested radius chưa normalize (normalized radius không lớn hơn giá trị đó), đo lại child với constraints tương ứng và kiểm fit. Trục bị controller cap được scroll/clip theo contract; trục không bị cap mà vẫn thiếu chỗ là lỗi của thuật toán đo, không phải một PASS. Kiểm thêm Popover với proposed Size.Empty và radius rất lớn.

### 5.4. Backing và clip của popup

1. Dropdown và surface dùng cùng màu opaque `theme.Colors.Surface` cho phần backing trong Region; renderer background phải thực sự `FillRectangle` trong vùng clip, không chỉ gán BackColor khi override đang rỗng. Không lấy màu desktop bằng screen sampling trong production.
2. Surface vẽ fill và stroke từ shared geometry trên backing đã xác định; không Clear Transparent trên HWND thường.
3. Giữ outer Region conservative đủ chứa painted coverage và inner host Region bảo vệ content. Allowance mặc định vẫn 1 device pixel; không tăng tùy tiện.
4. Đồng bộ Region clone của dropdown sau geometry/layout/presentation updates bằng method riêng, gọi cả khi bounds không đổi. Giữ ownership/dispose và generation của bounds-correction.
5. Native drop shadow vẫn giữ mặc định. Test shadow on/off để phân biệt lỗi shadow với border; tắt shadow chỉ là diagnostic, không thay nghiệm thu default.

Vùng backing opaque sát mép native mask là một giới hạn được thiết kế có chủ ý. Chụp nền sáng/tối/tương phản để bảo đảm không biến fringe thành viền sáng lạ. Nếu mức chất lượng này chưa đủ, không ghi per-pixel-alpha PASS.

### 5.5. Nội dung và lifecycle từng control

- Lookup grid: internal policy trong BootstrapDataGridView quyết định border khi ApplyTheme; standalone giữ FixedSingle, embedded lookup dùng None. Constructor/event-order trick không đủ vì theme sẽ reset.
- Select đo outer preferred size từ surface; search/results vẫn giữ row height, search keyboard và asynchronous provider semantics.
- Radius đổi khi popup mở đi qua luồng geometry-only: đổi shell/layout/Region/placement mà không gọi full content presentation, EnsureHighlightedVisible hoặc SynchronizeHighlightedResult. Không đóng/mở, không đổi query/highlight/caret/committed value; giữ vị trí cuộn hợp lệ, chỉ clamp nếu viewport mới bắt buộc. Test cả khi user đã cuộn khỏi dòng highlight.
- Lookup nhận notification nội bộ từ TextBox shell; không thêm public event chỉ phục vụ popup.
- Không đổi tracker, activation domain, generation guard, Tab/Escape/Alt+Tab, owner-focus policy hoặc native popup window type trong đợt này.

## 6. Kiểm chứng và chống lặp lại lỗi

Ba lớp bằng chứng cần phân biệt:

1. **Geometry/bitmap:** outer contour ổn định khi focus, content containment, opaque backing colors, clipping không xóa AA fringe. AA sample chỉ tính trong band đã biết, phải có coverage thật; pixel A=0 không được tính là AA tốt.
2. **Native integration:** thật sự ShowAt popup với Select/Lookup, native children và desktop capture. Kiểm bounds, Region, pixel RGB hiển thị, query/selection/focus và callback count. DrawToBitmap riêng không thay lớp này.
3. **Physical review:** Light/Dark ở physical 100%/125%, thêm representative 150%/200% nếu có. Host cấu hình DPI awareness trước mọi HWND; ghi process/thread/window awareness, actual window DPI và physical scale. Không tính một render 96 được desktop phóng lên 125% là kiểm chứng render native 120 DPI. Các ApplyTheme(dpi) tests chỉ là synthetic metrics.

Một shared overlay change phải chạy focused Popover regressions vì dùng cùng surface/dropdown. SourceGrid editor regressions cần khi shell TextBox/Lookup ảnh hưởng adapters. Không chạy toàn bộ GUI/SQL suite ở mỗi task; xem plan để chọn filter hiện có và tests mới theo diff.

Acceptance visual phải thấy được cả popup, bốn crop góc và frame nguyên bản ở 1:1; không chỉ ảnh phóng to. Lưu full-size PNG và metadata theo candidate tại artifact/release evidence có thể mở lại; Git chỉ lưu report đã rà soát theo [TESTING](../TESTING.md). Không tự ghi Owner APPROVED từ automated PASS.

## 7. Nguồn source và tài liệu chính

- [Combo metrics](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/fd317f0aa4242cc9a44b7554c54f96bb883b6357/src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBoxRenderLogic.cs#L115), [native apply](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/fd317f0aa4242cc9a44b7554c54f96bb883b6357/src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBox.cs#L410), [Microsoft .NET8 sizing](https://github.com/dotnet/winforms/blob/v8.0.0/src/System.Windows.Forms/src/System/Windows/Forms/ComboBox.cs#L845).
- [Native Combo size constraints](https://github.com/dotnet/winforms/blob/v8.0.0/src/System.Windows.Forms/src/System/Windows/Forms/ComboBox.cs#L754), [SetBoundsCore specified bounds](https://github.com/dotnet/winforms/blob/v8.0.0/src/System.Windows.Forms/src/System/Windows/Forms/Control.cs#L10725), [UpdateBounds overloads](https://github.com/dotnet/winforms/blob/v8.0.0/src/System.Windows.Forms/src/System/Windows/Forms/Control.cs#L11432).
- [Surface paint/regions](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/fd317f0aa4242cc9a44b7554c54f96bb883b6357/src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlaySurface.cs#L184), [dropdown host](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/fd317f0aa4242cc9a44b7554c54f96bb883b6357/src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayDropDown.cs#L151).
- [Select ComputeBounds](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/fd317f0aa4242cc9a44b7554c54f96bb883b6357/src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs#L187), [Lookup content](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/fd317f0aa4242cc9a44b7554c54f96bb883b6357/src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDropDownContent.cs#L98).
- [DataGrid theme](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/fd317f0aa4242cc9a44b7554c54f96bb883b6357/src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDataGridView.cs#L265), [Select radius](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/fd317f0aa4242cc9a44b7554c54f96bb883b6357/src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs#L408).
- [Lookup presentation/navigation](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/fd317f0aa4242cc9a44b7554c54f96bb883b6357/src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Popup.cs#L55), [Select result presentation](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/fd317f0aa4242cc9a44b7554c54f96bb883b6357/src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs#L37), [RoundedPath normalization](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/fd317f0aa4242cc9a44b7554c54f96bb883b6357/src/MyDmsVn.Bootstrap5WinFormUI/Rendering/RoundedPath.cs#L23).
- [Existing surface tests](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/fd317f0aa4242cc9a44b7554c54f96bb883b6357/tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlaySurfaceTests.cs), [existing popup tests](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/fd317f0aa4242cc9a44b7554c54f96bb883b6357/tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayDropDownTests.cs).
- [Consumer filter row](https://github.com/chung6a8m/MyDmsVn/blob/7e4dc2ae8ca2d5087d14206ad5fbb1bdb085fea5/src/MyDmsVn.Desktop.WinForms/Features/Security/SecurityPermissionCatalogView.cs#L29).
- Microsoft: [window Region](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowrgn), [WinForms simulated transparency](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-give-your-control-a-transparent-background), [AllowTransparency](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripdropdown.allowtransparency), [UpdateLayeredWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-updatelayeredwindow).
- Microsoft: [High DPI support trên .NET Framework](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/high-dpi-support-in-windows-forms), [Application.SetHighDpiMode trên .NET](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.application.sethighdpimode).

Các source links được pin để phân biệt thiết kế dựa trên baseline nào. Executor phải kiểm drift trước task đầu, không dùng evidence lịch sử để đóng candidate mới.
