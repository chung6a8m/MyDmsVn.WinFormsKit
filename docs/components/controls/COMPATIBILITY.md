# Target Framework and Compatibility Policy

> **Nguồn nhập:** [`MyDmsVn.Bootstrap5WinFormUI@8ae217ea8aa9a938ebd558f7071dc737f909e6a5`](https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI/blob/8ae217ea8aa9a938ebd558f7071dc737f909e6a5/docs/COMPATIBILITY.md).
> Lệnh kiểm chứng và trạng thái hiện hành thuộc [TESTING](../../TESTING.md) và [ROADMAP](../../ROADMAP.md).
> Các kết quả PASS hoặc mốc hoàn tất trong nội dung gốc là bằng chứng lịch sử của upstream, không xác nhận graph WinFormsKit mới.

## 1. Supported targets

The project requirement is .NET Framework 4.8 plus .NET 8 for Windows Forms.

The intended project TFMs are:

```xml
<TargetFrameworks>net48;net8.0-windows</TargetFrameworks>
<UseWindowsForms>true</UseWindowsForms>
```

The `-windows` suffix is required for the .NET 8 WinForms target.

## 2. Compatibility philosophy

Maintain one source tree and one public API wherever practical. Target-specific code is a last-mile implementation detail, not a reason to expose different control APIs on each runtime.

The `net48` target is not a degraded compatibility afterthought. Every release gate builds and verifies both targets.

## 3. Runtime API restrictions

Code shared by both targets may only call APIs available on both targets, unless the call is wrapped in a compatibility abstraction or guarded with target-specific compilation.

Examples of APIs commonly found in prototypes that require attention on `net48`:

- `Math.Clamp`
- Newer convenience overloads added after .NET Framework
- Runtime/platform detection APIs introduced in modern .NET
- Newer `System.Drawing` APIs not present in .NET Framework 4.8

Prefer a small helper such as an internal `NumericUtil.Clamp` over scattering `#if` blocks through controls.

## 4. C# language vs runtime APIs

A modern C# compiler may target .NET Framework 4.8, so language syntax and runtime API availability are different concerns.

The repository may use the C# language version supported by the pinned SDK/Visual Studio toolchain, but contributors must ensure generated IL only references APIs available on the target framework.

Do not assume a newer syntax feature implies the corresponding newer runtime helper exists on `net48`.

## 5. Nullable reference types

Nullable annotations are a compile-time quality feature and can be used with `net48` when the build toolchain supports them. They must not lead to separate public APIs across targets.

If enabled, use them consistently. Do not copy prototype nullable annotations blindly without resolving real ownership/nullability semantics.

## 6. WinForms behavior differences

Where WinForms behavior differs between .NET Framework and modern .NET, isolate the difference behind an internal helper or a minimal target-specific implementation.

Areas to verify on both targets include:

- DPI scaling
- Text rendering and preferred size
- Designer serialization
- Accessibility behavior
- DataGridView painting
- Font availability/fallback
- Native `DateTimePicker` localized text, checkbox state, range normalization/exceptions, keyboard navigation, and calendar-popup behavior

`BootstrapDatePicker` deliberately keeps one native `DateTimePicker` authoritative on both TFMs. Tests that characterize date/range/format/checkbox behavior should compare against a fresh native peer instead of freezing culture- or runtime-specific rendered strings. The framework may theme the wrapper shell but does not promise to recolor, round, replace, or otherwise normalize the OS-owned calendar popup across Windows versions, cultures, or target frameworks.

Stage 9 therefore introduces no target-specific `MonthCalendar`, popup `Form`, P/Invoke hook, private WinForms reflection, parsing engine, or culture abstraction to force visual/behavioral parity. Differences that are genuinely native remain native unless a later explicit compatibility contract says otherwise.

`BootstrapCalendar` and `BootstrapCalendarPicker` are separate, framework-owned custom-calendar controls; they do not alter the native `BootstrapDatePicker` distinction above. Both target frameworks use the safe inclusive date domain supported by WinForms `DateTimePicker` (`DateTimePicker.MinimumDateTime.Date` through `DateTimePicker.MaximumDateTime.Date`), normalize public selection and bounds inputs to date-only values, and use `CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek` for the 42-cell month projection. The calendar paints its own shell, header, weekday names, and day states from framework theme/DPI tokens, so its visuals are framework-owned rather than OS calendar chrome.

`BootstrapCalendarPicker` presents a fresh custom calendar through the existing native `ToolStripDropDownMenu` infrastructure. Native ToolStrip remains responsible for screen working-area placement, focus transfer, Escape/outside-click dismissal, and disposal of the hosted snapshot. When the popup opens, the hosted calendar must receive focus before its arrow, PageUp/PageDown, Home/End, Enter, and Space keys can participate in the normal WinForms key route; the picker restores its collapsed keyboard entry points without exposing the hosted control or ToolStrip types. Both behaviors are verified on `net48` and `net8.0-windows`; Windows/culture-specific text measurement and ToolStrip placement may still differ naturally between runtimes and OS versions.

UI-NEXT T06's `BootstrapDateInput` uses native TextBox editing for the raw DMY candidate and the existing themed CalendarPicker for date choice. Its date-only parser and segment steps are shared across `net48` and `net8.0-windows`; the host supplies document admission and authoritative projection. `SelectionAccepted` distinguishes an explicit same-date activation from popup dismissal without exposing the ToolStrip host. The initial composite height comes from the children's themed preferred heights before docking; WinForms docking must not inflate it to the default `UserControl` height. Native focus, IME and DPI behavior should be exercised on the actual target Windows scale.

The custom calendar family reads `CurrentCulture` at formatting and layout time without caching or mutating it. If the culture's active calendar cannot represent a safe-domain date, summaries, month titles, and calendar accessibility names use a cloned culture with a representable optional Gregorian calendar where available, then another representable optional calendar, with `InvariantCulture` as the last fallback. This preserves an active calendar such as `UmAlQuraCalendar` for dates it supports while keeping the exact WinForms `DateTimePicker` domain usable. First-day-of-week, weekday labels, and integer day labels deliberately remain direct `CurrentCulture` behavior.

## 7. System.Drawing

This is a Windows-only WinForms framework, so `System.Drawing` is an appropriate rendering foundation. All drawing code still needs deterministic GDI resource disposal.

Do not add cross-platform abstraction complexity merely because modern `System.Drawing.Common` has restrictions outside Windows; cross-platform UI is a non-goal.

## 8. Icon dependencies

### Segoe MDL2 Assets

Treat the font as a Windows glyph source and provide fallback behavior when a glyph/font cannot be rendered as expected.

### FontAwesome.Sharp

Integration must be optional. The core assembly must not require applications to install FontAwesome.Sharp if they only use SVG, MDL2, or application-provided icons.

### SVG

WinForms/System.Drawing has no built-in complete SVG renderer. Therefore generic SVG support should be implemented behind the icon abstraction using a compatible renderer/adapter. The chosen implementation must support both target frameworks or be isolated into an optional adapter with a stable core contract.

## 9. Conditional compilation

Use conditional compilation only for real target differences, for example:

```csharp
#if NET8_0_OR_GREATER
    // Modern implementation
#else
    // .NET Framework-compatible implementation
#endif
```

Do not use target directives merely to avoid writing a small shared compatibility helper.

## 10. Build matrix

The pure decimal parser introduced for UIN-T04a uses the same source and exported surface on both targets. Its culture is cloned at profile construction and returned as a defensive read-only clone, so a caller changing `CurrentCulture` or its original `NumberFormat` cannot reinterpret a pending candidate. Limits remain caller supplied. The parser uses exact coefficient construction rather than `decimal.TryParse` rounding; it adds no runtime dependency or native-control behavior.

UIN-T05a adds one native `BootstrapTextBox` subclass on the same shared source path for `net48` and `net8.0-windows`. It retains the existing protected native `Editor` seam and internal formatted-history store; a private `NativeWindow` intercepts clipboard, undo and IME messages and is attached/released with the inner editor handle. No existing `BootstrapFormattedTextBox` or Numeral behavior changes, no new runtime dependency or public native child accessor is introduced. `PendingTextChanged` is the lexical notification even when display bytes do not change. A real interactive Windows session is required to establish a native DPI/IME result; pure parser/projection tests alone do not do so.

UIN-T04b's projection also uses one pure source path on both targets. It preserves every source character and records generated grouping separately, with explicit caret/selection boundary maps; invalid, incomplete, outer-whitespace and unreversible forms use identity display. No existing `BootstrapFormattedTextBox`, `InputCaretMapper` or `FormattedTextHistory` behavior changes. Native edit, paste, IME, undo and final acceptance are deferred to the T05 control/host integration.

At minimum, CI/local verification must include:

```text
net48            Windows
net8.0-windows   Windows
```

Release verification also includes the UI/manual matrix defined in `TESTING.md`.

## 11. Package compatibility rule

Before adding a runtime dependency, verify that its supported target frameworks are compatible with both project targets or isolate it into an optional adapter package. Core functionality must not become unavailable on `net48` because of a convenience dependency chosen for `net8.0-windows`.

## 12. BootstrapTextBox multiline — EXC-T03

Phần bổ sung ngày 2026-10-28 dùng cùng source và public API trên `net48`/`net8.0-windows`: `Multiline`, `WordWrap`, `ScrollBars`. Mặc định giữ single-line, native keyboard policy và hình học editor hiện có; chỉ nhánh multiline dùng chiều cao caller cung cấp. Không đổi namespace, assembly identity/version, dependencies hoặc public native-child accessor. Adapter, formatted, decimal, lookup và input-group consumers giữ mặc định cũ.

Mode/wrap/scrollbar setters giữ native handle-recreation behavior; cùng đối tượng `TextBox` tiếp tục sở hữu nội dung, caret, selection, clipboard và accessibility provider. Private `NativeWindow` chỉ đồng bộ accessible name/description trước focus/accessibility requests và tháo/gắn theo handle; không thay provider, hook toàn cục, timer hay message loop. Các tên native riêng của derived control không bị xóa khi wrapper metadata không được đặt.

Metadata được cập nhật ở handle/focus/layout/theme/request boundaries; đây không phải cam kết thông báo tức thời tới client đã cache provider khi chỉ sửa `AccessibleName`/`AccessibleDescription`. Thiết lập metadata trước khi hiển thị đáp ứng consumer chỉ đọc được chọn. Mở rộng property phải đi qua reviewed compiled API delta; native tests, Designer và physical DPI cần Windows trên đúng candidate, không suy từ cross-build.

## 13. Input/popup rendering — 2026-10-29

Core và SourceGrid adapter tiếp tục dùng cùng source trên `net48`/`net8.0-windows`; assembly/package/namespace identities, SourceGrid gitlink và native selection/editing contracts không đổi. Shared rounded geometry/painter, overlay measurement/backing/Regions và embedded-grid border policy đều là internal. Constructor `BootstrapComboBox` vẫn handle-free và chỉ đọc native height/window metrics sau `OnHandleCreated`.

Compiled API delta có chủ đích chỉ re-declare inherited layout seams trên `BootstrapComboBox`: public `GetPreferredSize(Size)` và protected `SetBoundsCore(...)`. Chúng giữ width/x/y và native modes, đồng thời cho layout engine cùng đọc outer-height contract; fingerprint reviewed là `1de84ca1b9a297e0caff576b4b18c0296ca17d0ebabf088be2f48213cad27755`. Không có public popup/geometry/borderless-grid API mới và `AssemblyVersion` vẫn `1.0.0.0`.

Radius-only refresh của Select/Lookup là internal geometry path, không phải selection/presentation refresh: không reconcile selection và không tự cuộn về highlighted row. Geometry/bitmap DPI matrix không chứng minh HWND composition hoặc physical monitor scale; trạng thái native, Owner và consumer phải đọc từ [candidate evidence](../../verification/20261029-input-popup-rendering.md).

Sửa findings PR #3 tiếp tục giữ compiled API fingerprint và assembly version trên cả hai TFM. Native requested height của Simple ComboBox được bảo toàn độc lập với closed bounds, còn Select restore scroll sau layout cuối mà không yêu cầu provider paging. Các sửa painter/fallback measurement không thêm public API hoặc thay ownership của Region/content. [Evidence review-fixes](../../verification/20261029-input-popup-rendering-review-fixes.md) được ghi riêng, không chuyển physical DPI, Owner visual decision hay consumer acceptance thành PASS.

[Review vòng hai](../../verification/20261029-input-popup-rendering-round-two.md) bổ sung đồng bộ specified closed height sau pre-handle metrics/font change bằng WinForms protected bounds APIs, kèm requested Simple height nội bộ độc lập. Painter ghép outer/inner coverage trước một lần composite để giữ alpha và tránh seam fractional DPI. Không thêm exported signature, dependencies hoặc thay public/native input model; physical DPI, Owner visual và consumer acceptance vẫn cần nghiệm thu riêng.

[Review vòng ba](../../verification/20261029-input-popup-rendering-round-three.md) giữ yêu cầu Height/Size của caller trong event hoặc derived override OnHandleCreated khi recreate do đổi style; bitmap contour layer dùng source pixel/destination rectangle rõ ràng khi Graphics đích khác resolution. Không đổi public API/native model hoặc radius-only navigation policy; physical DPI, Owner visual và consumer acceptance vẫn còn mở.

[Ổn định native suite](../../verification/20261029-input-popup-rendering-stability.md) bổ sung private guard cho query accessibility tới list HWND cũ trong lúc native DropDownStyle đã đổi sang Simple. Query tới child đang chuyển trạng thái trả0; query ổn định vẫn dùng provider native, không thêm public API, framework selector hoặc tùy chọn accessibility mới. Consumer không cache child HWND qua handle recreation; lấy provider của HWND mới theo native lifecycle. Clipboard production giữ nguyên; thay đổi observer chỉ thuộc tests và không phát lại copy/cut. Owner visual decision và consumer acceptance tiếp tục riêng, chưa hoàn tất.

## 14. BootstrapTabControl HotTrack và painter — TAB-02/03

`BootstrapTabControl` tiếp tục trực tiếp kế thừa `TabControl`, giữ `TabPages`, selection, keyboard, tooltip và overflow native. Khi `DrawMode=OwnerDrawFixed`, inherited `HotTrack=true` dùng hover của toolkit và style `TCS_HOTTRACK` được lọc lúc tạo HWND để tránh hai đường vẽ cùng header; `DrawMode.Normal` giữ đường native. Consumer không cần thay property hoặc page model. Chuyển header, đổi layout, tạo lại handle và scroll của native overflow child vẫn cập nhật hover theo bounds hiện tại. Resync kiểm HWND dưới pointer để không tô header bị overflow child hoặc cửa sổ khác che; native MouseMove/MouseLeave của parent vẫn giữ đường xử lý cũ. No-erase invalidation chỉ áp dụng cho vùng header được painter phủ đầy đủ, không thay chính sách xóa nền của page.

Renderer mặc định vẫn là handler `DrawItem` được đăng ký trong constructor, nên handler của consumer và subclass vẽ sau `base.OnDrawItem` tiếp tục nhận Graphics/bounds của WinForms theo thứ tự cũ. Caption closable được căn trong phần content đã trừ icon/close; phép đo và vẽ cùng giữ `TextRenderer` glyph-overhang padding, gồm caller font italic. Close pointer vẫn được kiểm trước native selection/focus và `TabCloseRequested` không sở hữu page. Menu toolkit không giữ page vừa bị xóa trong `ControlRemoved`. Toolkit chỉ dispose menu do chính mình tạo, không dispose `ContextMenuStrip` consumer gán. `CreateParams` override mới là protected API delta cần có trong [public API baseline](PUBLIC_API_BASELINE.md) của candidate; [candidate evidence](../../verification/20261029-tabs-hottrack.md) phân biệt test hai TFM với physical DPI/Owner visual còn mở.

## 15. BootstrapTabs và gallery — TAB-04/05

`BootstrapTabs` là control mới có base trực tiếp `TabControl`, không đổi default hay public behavior của `BootstrapTabControl`. Default mới là Underline/Primary/Fill=false/BorderRadius=0, inherited HotTrack=false. Close vẫn là request; consumer giữ trách nhiệm collection và page lifetime. Compact chrome chỉ hoạt động ở Top/Normal/single-line/LTR/OwnerDrawFixed; cấu hình native khác giữ caller properties và owner-draw dispatch qua fallback. `DrawMode.Normal` tiếp tục dùng native paint. Handler `DrawItem` của consumer và subclass vẽ sau `base.OnDrawItem` được giữ trong native HDC pass. Font/ImageList/menu consumer không được toolkit dispose. Gallery có route `Navigation / BootstrapTabs` và toggle HotTrack rõ cho demo cũ.

Contract và [consumer migration recipe](../../CONSUMER_MIGRATION.md) của TAB-06 ghi rõ API và giới hạn buffer; API fingerprint phải đo từ assembly thực tế trên cả hai TFM và [evidence hiện hành](../../verification/20261029-tabs-hottrack.md) chưa chứng minh no-flicker trên mọi cấu hình.

Review tiếp theo của TAB-04 xác nhận `ControlRemoved` có thể phát trước khi `TabPages` bỏ page: `BootstrapTabs` tính `Fill`/uniform width theo tập trang sau removal ngay trong callback. `WM_PRINT` có `PRF_CHECKVISIBLE` không ghi vào HDC caller khi HWND ẩn; đường print không có cờ này giữ behavior cũ. Fallback Left đặt khoảng caption theo trục sau xoay để tránh icon; fallback LTR cắt ảnh khỏi close hit target ngay cả khi header hẹp. Các sửa này không đổi public API hoặc control cũ.

Fallback ngang clip thao tác vẽ icon vào `imageBounds` và vùng trước close cho cả LTR/RTL, enabled/disabled. Điều này cần thiết vì `ControlPaint.DrawImageDisabled` dùng kích thước ảnh gốc thay vì kích thước `imageBounds` đã thu hẹp; close hit target và API không đổi.

## 16. Contract consumer của BootstrapTabs — TAB-06

Control mới chỉ compact khi `Top/Normal/single-line/LTR/OwnerDrawFixed`; fallback giữ native property, selection và dispatch `DrawItem` cho cấu hình owner-draw khác. Inherited `HotTrack` mặc định `false`; khi bật dưới owner-draw, toolkit dùng cùng hover controller của control cũ. `TabCloseRequested` chỉ đưa `TabPage` và `TabCloseReason`, còn app giữ quyền xóa, dispose và chọn page kế tiếp. `Font`, `ImageList`, `ContextMenuStrip` do consumer cung cấp vẫn thuộc consumer. [Contract đầy đủ](COMPONENTS.md#bootstraptabs) liệt kê signatures và defaults; protected `CreateParams` của cả hai controls phải được tính trong [API baseline](PUBLIC_API_BASELINE.md).

Consumer handler `DrawItem` và subclass gọi `base.OnDrawItem(e)` trước khi vẽ tiếp trên `e.Graphics` giữ thứ tự cũ. Riêng lớp app [BusinessCompactWorkspaceTabs tại pin `16c24738`](https://github.com/chung6a8m/MyDmsVn/blob/16c24738a4758ccdceb1f3509f4e37adb0d03464/src/MyDmsVn.Desktop.WinForms/BusinessCompactWorkspaceTabs.cs) còn vẽ chrome bằng `Graphics.FromHwnd(Handle)` sau `base.WndProc`; nét vẽ đó nằm ngoài buffer compact của toolkit. PR consumer riêng cần thay lớp app bằng `BootstrapTabs` đã pin exact commit và bỏ đường vẽ screen DC này; nếu còn custom header, vẽ qua `DrawItem`/`e.Graphics` rồi kiểm native paint, không coi source migration là chứng nhận no-flicker.

## BootstrapBusyOverlayHost: điều kiện child-layer ở candidate BUS-03

Host mới giữ targets `net48;net8.0-windows`. Native child-layer chỉ dùng khi OS có cơ chế từ Windows 8 trở lên và executable có declaration Windows 8/8.1/10 trong compatibility context của chính EXE. Query dùng `QueryActCtxW` với HMODULE của process executable, kiểm size/count/alignment; không dùng visual-style activation context đang active hoặc đọc XML EXE để quyết định capability production. Version numeric chỉ là guard OS, không thay manifest/native setup. Executable thiếu declaration hoặc native alpha thất bại sẽ báo `OpaqueFallback`; toolkit không thay minimum OS của các control khác.

Consumer dùng ProjectReference và Begin/dispose trên UI thread; không cần thay host cũ. Manifest prerequisites phải do executable sở hữu. Giới hạn chưa có BUS-04/05/06 và fingerprint mới tại [candidate BUS-03](../../verification/20261030-live-busy-overlay-production.md) là lịch sử. Trạng thái resource/API/native hiện hành thuộc [report BUS-06](../../verification/20261030-live-busy-overlay-acceptance.md) và [plan BUS](../../plans/20261029-003-live-busy-overlay.md).

Candidate BUS-04 bổ sung cleanup khi Content mất HWND/dispose, capture scoped và focus restoration có điều kiện; không đổi API/behavior `BootstrapBusyHost` cũ. Consumer phải đóng popup và giữ command admission trong cả pending/visible, không dựa vào overlay để bảo vệ keyboard/UIA. Owner tracking được cập nhật nếu reparent cả ancestor, không giữ owner cũ sau dispose. Xem [evidence lifecycle](../../verification/20261030-live-busy-overlay-lifecycle.md) theo từng checkpoint; các câu resource/API/BUS-05/06 pending cũ không thay [acceptance hiện hành](../../verification/20261030-live-busy-overlay-acceptance.md). Protected override `OnParentChanged` đã được tính trong [API delta được review](PUBLIC_API_BASELINE.md#bus--review-exported-delta-ngày-30102026); API PASS không đóng native acceptance.

## BootstrapBusyOverlayHost — contract tích hợp BUS-06

Đây là addition độc lập, giữ `BootstrapBusyHost` và `BusyPresentation` hiện hữu. Không đổi assembly identity/version (`MyDmsVn.Bootstrap5WinFormUI`, `1.0.0.0`), targets hoặc vendor pin. [API audit](PUBLIC_API_BASELINE.md#bus--review-exported-delta-ngày-30102026) đã review đúng host/enum cùng chín protected overrides; không cần đổi fingerprint chỉ vì tài liệu bổ sung.

Consumer bật child-layer bằng manifest nhúng trong **EXE thực chạy**; Windows 10/11 là môi trường gallery/reference, không có claim kiểm Windows 8/8.1 thực. `supportedOS` Windows 10 `{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}` là declaration được chấp nhận. Common-Controls v6 hoặc PMv2 riêng lẻ không thay compatibility declaration. Thiếu capability, query không xác định hoặc native setup thất bại dẫn `OpaqueFallback`; không đổi minimum OS của toàn toolkit và không dùng snapshot/top-level overlay làm fallback.

Content vẫn enabled và có thể tiếp tục cập nhật. Caller đóng ComboBox/Lookup/ToolStrip popup transient, chặn keyboard/UIA/business commands trước `Begin` và suốt pending/visible; filter host chỉ chặn queued mouse tới native Content subtree khi đã hiện, kể cả opacity 0. Arbitrary `SendMessage`, raw touch/pen và top-level popup ngoài subtree không được admission filter bảo đảm. Không coi overlay là security boundary. Show không lấy focus; Cancel có native keyboard/UIA semantics và restoration có điều kiện khi card ẩn. [Component contract](COMPONENTS.md#bootstrapbusyoverlayhost) và [recipe source/PMv2](../../CONSUMER_MIGRATION.md#dùng-bootstrapbusyoverlayhost-từ-source--bus-06) ghi ownership/lifecycle đầy đủ.

Các câu giới hạn BUS-03/04 phía trên là checkpoint lịch sử; kết quả mới phải đọc ở plan/evidence đúng candidate. API hoặc full-suite PASS không thay native SourceGrid, physical/mixed-DPI hay Owner acceptance của BUS.
