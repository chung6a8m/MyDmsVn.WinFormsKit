# Thiết kế BootstrapTabs, HotTrack dùng chung và busy overlay có opacity thực

Ngày: **29/10/2026**. Repository: **chung6a8m/MyDmsVn.WinFormsKit**.

**Trạng thái:** Owner đã chọn hướng `BootstrapTabs mới + sửa HotTrack dùng chung + phương án 3B + demo`. Tài liệu này khóa đề xuất kỹ thuật cho hai plan bên dưới; chưa có implementation hoặc Windows/native acceptance cho các control mới. Các defaults/API chi tiết là quyết định kỹ thuật của plan, không phải kết quả nghiệm thu hình ảnh.

- [Plan TAB: BootstrapTabs và HotTrack](../plans/20261029-002-bootstrap-tabs-hottrack.md).
- [Plan BUS: live busy overlay](../plans/20261029-003-live-busy-overlay.md).

## 1. Mục tiêu, ranh giới và lựa chọn

1. Thêm `BootstrapTabs : System.Windows.Forms.TabControl`, có diện mạo workspace như `BusinessCompactWorkspaceTabs`, không tham chiếu `BusinessCompactAppearance` hoặc assembly ứng dụng.
2. Sửa HotTrack ở `BootstrapTabControl`; dùng cùng cơ chế hover và painter nội bộ cho control mới. Giữ native page/selection/keyboard/overflow và khả năng mở rộng qua `DrawItem`.
3. Thêm `BootstrapBusyOverlayHost` cho phép thấy nội dung bên dưới **tiếp tục cập nhật trong lúc overlay vẫn hiện**. Spinner/message/Cancel giữ độ rõ độc lập với alpha của nền.
4. Có demo tương tác cho cả hai control; hai targets `net48;net8.0-windows`; không cần SQL hoặc app startup.

| Quyết định | Lý do và giới hạn |
|---|---|
| Hai public tab controls cùng trực tiếp kế thừa `TabControl`, chia sẻ helper bằng composition | Giữ yêu cầu kế thừa và tránh hai bản hover/layout lệch nhau; không thêm public base framework. |
| `BootstrapTabs` hoàn thiện cả chrome quanh header/page; control cũ giữ phạm vi chrome hiện tại | Giảm phạm vi thay đổi đối với consumer đang dùng control cũ hoặc subclass của nó. |
| Busy mới dùng **layered child HWND** cho backdrop, card thường nằm trên như sibling | Windows quản lý quan hệ parent/clipping/z-order; phù hợp overlay cục bộ trong tab hơn một cặp top-level Forms. |
| Giữ nguyên `BootstrapBusyHost` | Consumer cũ không nhận thay đổi focus, input, opacity hoặc lifecycle ngoài ý muốn. |
| Snapshot/`DrawToBitmap` không phải renderer production của BUS | Owner đã chọn 3B; hình nền đứng yên không đáp ứng yêu cầu. Capture chỉ được dùng bởi verification host. |
| Cơ chế native phải qua proof sớm trên Windows | Tài liệu API chưa chứng minh tổ hợp owner-draw/layered child hoạt động đúng với hai runtime và child HWND thực tế. |

Một cặp owned top-level backdrop/foreground và per-pixel `UpdateLayeredWindow` đã được cân nhắc: chúng tăng công việc clipping, activation, modal ordering hoặc accessibility. Chúng không phải fallback tự động của plan này. Nếu proof child-layer thất bại trên môi trường nghiệm thu, phải ghi lỗi và sửa thiết kế trước khi mở rộng implementation. Opaque fallback chỉ bảo vệ môi trường không hỗ trợ, không được dùng để đóng yêu cầu 3B.

Không tự đổi consumer MyDmsVn/vendor pin, publish package, archive lịch sử, thêm dependency hoặc nâng framework. Consumer migration có recipe riêng sau khi toolkit có candidate đã kiểm chứng.

## 2. Baseline và căn cứ

| Phạm vi | Exact revision |
|---|---|
| Toolkit main, sau merge PR #3 | `8e7f1854d85d80fbaecc3d9d9167bb8ebcd94921` |
| MyDmsVn main, source workspace tham chiếu | `16c24738a4758ccdceb1f3509f4e37adb0d03464` |
| .NET 8 WinForms source đối chiếu | `e4ede9b8979b9d2b1b1d4383f30a791414f0625b` |
| SourceGrid gitlink giữ nguyên | `f4e457b43582bf01892f50bdc74aa480531e5944` |

Các kết luận đã xác nhận từ source:

- [Workspace tham chiếu][workspace] kế thừa `BootstrapTabControl`, sửa caption sau `base.OnDrawItem` và tô chrome sau `base.WndProc`. Vì vậy thay tên/base class đơn thuần làm mất các hành vi inherited.
- [Hover hiện tại][tabs] đã chặn cùng hovered index và chỉ invalidate header cũ/mới. Không kết luận sai rằng nó invalidate toàn control trên mọi MouseMove.
- [Render logic][tab-logic] chọn palette theo hover; hover thông thường không đổi Font/ItemSize. Image sizing trong paint chỉ xảy ra khi cache image thay đổi, không phải mỗi lần hover.
- [WinForms TabControl][native-tabs] đặt `UserPaint=false`; `HotTrack` bật `TCS_HOTTRACK` và setter có thể recreate handle. [Control.WmMouseMove][native-control] xử lý native trước managed event. Đây là căn cứ cho giả thuyết hai đường hover, chưa chứng minh pha gây nháy trên máy người dùng.
- `Invalidate(Rectangle, false)` dùng `false` cho `invalidateChildren`; source WinForms còn truyền `bErase = !Opaque`. Không coi overload này là invalidation không erase.
- [BusyHost hiện tại][busy] dùng Panel opaque; [contract][components] giữ content enabled và giao keyboard/UIA/command admission cho caller.
- [Gallery project][gallery-project] chưa có `ApplicationManifest`. [VisualStyleHost][visual-project] đã có manifest Windows 10 và cấu hình DPI riêng cho net48. Phải kiểm manifest đã nhúng vào EXE thật, không suy từ file XML.

Source analysis không thay trace native, capture màn hình đang hiển thị hoặc quyết định Owner trên candidate mới. [TESTING](../TESTING.md) tiếp tục sở hữu test policy.

## 3. Contract của BootstrapTabs

Namespace/assembly: `MyDmsVn.Bootstrap5WinFormUI.Controls` / `MyDmsVn.Bootstrap5WinFormUI`. Public base trực tiếp là `TabControl`.

| Thành viên | Default mới | Quy tắc |
|---|---|---|
| `TabStyle : BootstrapTabStyle` | `Underline` | Dùng enum và validation hiện có; hỗ trợ `Tabs`, `Pills`, `Underline`. |
| `Variant : BootstrapVariant` | `Primary` | Semantic palette hiện có. |
| `Fill : bool` | `false` | Cùng uniform item-width policy hiện có; overflow vẫn native. |
| `BorderRadius : int` | `0` | `-1` dùng theme; giá trị dưới `-1` bị từ chối trước mutation. |
| Inherited `HotTrack` | `false` | Không khai báo property `new HotTrack`; demo bật rõ `true`. |
| `DrawMode` / `SizeMode` | `OwnerDrawFixed` / `Fixed` | Giữ native collection và selection. |
| Alignment / Appearance / Multiline | `Top` / `Normal` / `false` | Các giá trị consumer thay được giữ nguyên. |

API đóng tab dùng các types đã có:

```csharp
public BootstrapTabs();
public event EventHandler<TabCloseRequestedEventArgs>? TabCloseRequested;
public void SetTabClosable(TabPage page, bool closable);
public bool IsTabClosable(TabPage page);
public bool RequestTabClose(TabPage page, TabCloseReason reason = TabCloseReason.Programmatic);
protected Rectangle GetTabCloseButtonBounds(int index);
```

Close là opt-in từng page, kiểm enabled/membership, phát đúng instance/reason; không tự select/remove/dispose. Pointer close được nhận trước native selection/focus; Ctrl+W và accessible context-menu command dùng cùng request path. Dirty-close và confirm-save thuộc app. Native `TabPage.Enabled` không được tự diễn giải thành một chính sách selection mới.

Constructor không tạo handle và không cần ứng dụng khởi động. Theme dùng `BootstrapThemeManager`, metrics dùng `DpiScaler`; caller-owned fonts/images giữ ownership. `BusinessCompactAppearance.Scale` dùng ceiling còn `DpiScaler` dùng rounding: kiểm riêng khoảng cách text/focus tại 125% thay vì hứa pixel-identical.

### 3.1. Composition và compatibility

Tái sử dụng `BootstrapTabControlRenderLogic`; thêm internal interaction/painter/close/native-paint helpers. Hai facade giữ native `WndProc`, command-key và lifecycle seams. Không tạo một selection/page model mới.

Đối với `BootstrapTabControl`: giữ defaults, signatures/access modifiers hiện hữu, đặc biệt `WndProc`, `ProcessCmdKey`, `GetTabCloseButtonBounds`, và thứ tự event. Renderer mặc định hiện được đăng ký trong constructor qua `DrawItem`; extraction phải giữ user handler và derived `OnDrawItem` vẽ sau `base.OnDrawItem` hoạt động. Protected overrides mới cũng là API delta và phải được review trong fingerprint.

### 3.2. Flat chrome và fallback

Compact composition chỉ chạy khi `DrawMode=OwnerDrawFixed`, `Alignment=Top`, `Appearance=Normal`, `Multiline=false`, effective RTL tắt và `RightToLeftLayout=false`.

Ngoài tổ hợp này, dùng **native chrome fallback**: không tô lại outer frame/gaps; giữ native layout/overflow/owner-draw dispatch và painter bảo thủ trong bounds được cung cấp. Painter fallback xử lý orientation/RTL, text/icon/close/focus không vượt bounds; transform được Save/Restore trước khi trả cho consumer handler. Không âm thầm clear `TCS_OWNERDRAWFIXED` khi property vẫn là OwnerDrawFixed. Nếu caller chọn `DrawMode.Normal`, giao painting và HotTrack cho native. Roundtrip compact → fallback → compact không sửa giá trị caller hoặc tạo lại pages.

### 3.3. HotTrack và paint pipeline

1. Một state hover do helper quản lý; clear native `TCS_HOTTRACK` chỉ khi custom header renderer đang hoạt động. Giữ inherited property làm công tắc.
2. Invalidate theo effective visual state, chỉ vùng header cũ/mới. Khi không đổi màu/hình ảnh, không tạo invalidation thêm. Không gọi `Refresh`/`Update`, timer hoặc debounce trong hover.
3. Invalidation native không erase chỉ dùng cho vùng chắc chắn sẽ được renderer tái tạo đủ; không bật `Opaque` hoặc nuốt `WM_ERASEBKGND` cho toàn control.
4. Proof ưu tiên native `WM_PAINT` với supplied HDC vào buffer; sau native/header/consumer drawing, hoàn thiện chrome trong cùng buffer và present một lần. Microsoft có mô tả supplied-HDC seam, nhưng TAB-01 phải chứng minh bằng trace trên hai TFM [W1].
5. Screen paint dùng BeginPaint/EndPaint đúng một lần và cleanup trong `finally`; supplied-HDC/print path không lấy screen DC hoặc gọi BeginPaint. Giữ clip/origin và không paint selected page/overflow child lần thứ hai.
6. Header bounds snapshot phải giữ actual selected `DrawItem.Bounds`, layout generation, page display rectangle và native child exclusions. Không dùng selected-index cache cũ sau layout/DPI/scroll/handle changes.
7. Không lấy `Graphics.FromHwnd` sau base paint để sửa chrome. Với subclass app còn tự vẽ ra screen sau base.WndProc, toolkit không thể đảm bảo lớp vẽ bên ngoài đó được gộp vào buffer; consumer cần migration riêng.
8. Trace phải tìm cả DrawItem ngoài WM_PAINT composed pass. Nếu còn đường native vẽ trực tiếp, giải quyết/reproduce nó trước khi nhận hết flicker. Không chỉ thêm `DoubleBuffered=true`; [W2] ghi giới hạn của property trên TabControl.

`WM_PRINT`/`WM_PRINTCLIENT` phải tôn trọng flags, HDC và clip, guard nested dispatch để không composite hai lần. Painter không được thay ItemSize, selection hoặc gọi Invalidate. Mỗi caption được đo/vẽ với cùng padding convention, trừ icon/close trước; sửa text-only không được phá ImageList/variant khác.

## 4. Contract của BootstrapBusyOverlayHost

```csharp
public enum BootstrapBusyOverlayRenderMode
{
    None = 0,
    NativeLayered = 1,
    OpaqueFallback = 2
}

public sealed class BootstrapBusyOverlayHost : Panel
{
    public BootstrapBusyOverlayHost(Control content);
    public Control Content { get; }
    public double BackdropOpacity { get; set; } // default 0.35
    public BootstrapBusyOverlayRenderMode RenderMode { get; }
    public event EventHandler? RenderModeChanged;
    public IDisposable Begin(BusyPresentation presentation);
}
```

`Content` phải khác null, chưa có parent; host sở hữu lifetime và Dock=Fill sau construction thành công. Dùng lại `BusyPresentation` hiện hữu. `BackdropOpacity` chỉ nhận số hữu hạn trong `[0,1]`, invalid input ném `ArgumentOutOfRangeException` trước mutation; alpha byte = `Round(255 * value, AwayFromZero)`. Mọi mutation và token release khi host còn sống chạy trên owning UI thread.

Backdrop RGB lấy `theme.Colors.Dark`, chuẩn hóa alpha 255; opacity do native quản lý. Card dùng Surface/Text opaque, `BootstrapSpinner` hiện hữu, native Label và native Button Cancel. Không giảm alpha của card. Không thêm public timer/compositor/snapshot-provider/block-input API.

`RenderMode=None` khi chưa/không physically presented; `NativeLayered` khi native path được thiết lập; `OpaqueFallback` khi thiếu capability hoặc native setup thất bại. Event phát sau khi state đã commit và chỉ khi mode đổi; event handler có thể Begin/Dispose/hide host nên phải recheck generation sau callback. Reason/error native giữ trong diagnostics nội bộ cho tests/report; mode là thông tin public để consumer nhận biết degradation.

### 4.1. Native topology và capability

Ba sibling trong host, z-order từ trên xuống: **foreground card → layered backdrop → Content**. Backdrop là child không có interactive children, dùng `WS_EX_LAYERED`, `WS_CLIPSIBLINGS` và `SetLayeredWindowAttributes(..., LWA_ALPHA)`; paint màu RGB đặc. Không gắn `WS_EX_TRANSPARENT`, không dùng class style `CS_OWNDC`/`CS_CLASSDC`. Native wrappers dùng IntPtr/SIZE_T đúng bitness, kiểm return code và last error [W3], [W4].

Child-layer yêu cầu Windows 8+ và executable manifest có supportedOS phù hợp [W3], [W5]. Gallery/reference acceptance là Windows 10/11; không tuyên bố đã kiểm Windows 8 chỉ vì API có minimum đó. Không đổi minimum OS của mọi control trong toolkit.

Capability helper nhỏ đọc **process executable compatibility context**, không nhầm thread activation context của visual styles: `QueryActCtxW` theo documented compatibility query, check ElementCount/buffer sizes/alignment và accepted Windows 8/8.1/10 OS IDs; không kích hoạt/thay activation context. Kiểm native style/setup/SetLayeredWindowAttributes riêng. Test cả x86 net48 và x64; không dựa riêng vào `Environment.OSVersion` hay API return success để kết luận visual PASS [W6], [W7].

Thiếu declaration, query không xác định hoặc native setup thất bại: backdrop chuyển sang opaque SurfaceSecondary trong chính host, card/cancel/tokens tiếp tục hoạt động, mode báo OpaqueFallback. Không silently dùng snapshot hoặc top-level Form. BUS-01 phải có native live PASS trên supported manifested EXEs; fallback PASS không đóng gate đó.

### 4.2. Input và focus

- Content.Enabled, native editor state và command admission thuộc caller. Caller chặn keyboard/UIA/business commands ngay từ Begin, kể cả 150 ms pending. Visual overlay không là security boundary.
- Trong lúc presentation thực sự hiện, scoped UI-thread `IMessageFilter` chặn queued mouse move/button/down/up/double-click/wheel, gồm nonclient counterparts, chỉ khi target HWND là Content hoặc native descendant (`IsChild`). Card/Cancel, sibling ngoài host, modal và host khác không bị chặn.
- Alpha 0 có thể để native hit test xuyên qua [W4]; filter vẫn chặn queued mouse tới Content. Không bắt keyboard, UIA, arbitrary SendMessage hoặc raw touch/pen chưa được nghiệm thu. Real input tests phải kiểm alpha 0, 0.35 và 1.
- Chỉ release capture nếu capture thuộc subtree Content; không tác động capture của control ngoài host. Đăng ký filter đúng một lần khi show; gỡ ở mọi hide/failure/dispose/handle-loss path; không lưu stale HWND.
- Show không lấy focus. Cancel là native Button, có Tab/Shift+Tab, Space/Enter và accessible Invoke; callback chỉ gửi yêu cầu cancel cho token hiện hành, không tự release/rollback.
- Khi Cancel/card đang giữ focus và sắp bị ẩn, chỉ trả về focused Content descendant đã ghi nếu nó còn hợp lệ/visible/enabled/selectable; nếu không, dùng native traversal trong owner. Không giành lại focus người dùng đã chuyển ra ngoài card. Đây là contract của control mới, không thay host cũ.
- Popup top-level của Content (ComboBox dropdown, Lookup/ToolStrip popup) không nằm trong subtree child overlay. Consumer đóng popup/editor transient trước khi bắt đầu busy; host không dò/đóng mọi cửa sổ trong process. Một modal ngoài host vẫn dùng được bình thường.

### 4.3. Lifecycle

Giữ quy tắc tương đương BusyHost: delay ban đầu **150 ms**, không minimum hold, newest live token quyết định message/cancel, dispose token idempotent, release token cũ không ẩn token mới, release token mới có thể lộ token cũ. Cancel không hoàn thành operation. Dùng `IBootstrapBusyDelay` hiện hữu; không refactor host cũ vào một controller mới.

Constructor handle-free: không đọc Handle, CreateGraphics, query native capability hoặc chạy spinner/delay. Chỉ show khi host/ancestor visible, có handle, client size dương, owner không minimized và còn token. Hide, switch tab, minimize, handle loss và dispose phải hủy pending generation, gỡ filter, dừng spinner và ẩn/giải phóng HWND do mình sở hữu. Re-show/restore reschedule 150 ms nếu token còn sống. Deactivate owner không tự kết thúc busy hoặc activate cửa sổ.

Resize/DPI/theme/opacity cập nhật bounds/palette/alpha tại chỗ, không reset token/delay hay chụp content. Reparent/owner change gỡ subscriptions cũ và cập nhật owner minimize tracking. Child clipping/z-order phải được kiểm với ancestor scroll, overlapping sibling, modal và DPI; không cần một top-level window anchor tracker mới.

## 5. Demo, proof và tiêu chí hoàn thành

`BootstrapTabsDemoForm` có HotTrack toggle, styles, theme, long Vietnamese captions, images, closable/fixed pages, add/remove, empty state, native overflow, input giữ focus và comparison control cũ. `BusyOverlayDemoForm` có native editor/grid/progress cập nhật định kỳ, alpha slider, nested operations/cancel và trạng thái RenderMode; không cần SQL.

Trong live demo, cùng một token/backdrop HWND phải còn hiện khi dữ liệu phía dưới đổi. Test host capture desktop trước/sau để xác minh; production không gọi capture/WM_PRINT của Content. Card không bị alpha. Mouse tới Content bị chặn trong khi Cancel và sibling ngoài host dùng được. Tests về fallback và live phải được báo riêng.

Nghiệm thu chính: hai TFM, Light/Dark, physical 100%/125% với actual window DPI, thêm case đại diện 150%/200% và di chuyển mixed-DPI. Nếu môi trường không có DPI/desktop cần thiết, ghi NOT RUN và giữ acceptance tương ứng mở. NUnit palette/geometry, build hoặc một ảnh cuối không chứng minh hết nháy hay live composition.

Hai plan sở hữu task status riêng; ROADMAP chỉ điều hướng. Report phải ghi candidate, OS/runtime/SDK, bitness, scale + actual DPI, commands, discovered/executed/pass/fail/skip, native scenarios, evidence URL và limitations. Không commit PNG/video/TRX/logs/dumps; upload artifact có thời hạn/lưu trữ rõ và lưu manifest tham chiếu trong report. Quyết định Owner chỉ áp đúng candidate và phạm vi ảnh/clip đã xem.

## Nguồn

[workspace]: https://github.com/chung6a8m/MyDmsVn/blob/16c24738a4758ccdceb1f3509f4e37adb0d03464/src/MyDmsVn.Desktop.WinForms/BusinessCompactWorkspaceTabs.cs
[tabs]: https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/8e7f1854d85d80fbaecc3d9d9167bb8ebcd94921/src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTabControl.cs
[tab-logic]: https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/8e7f1854d85d80fbaecc3d9d9167bb8ebcd94921/src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTabControlRenderLogic.cs
[native-tabs]: https://github.com/dotnet/winforms/blob/e4ede9b8979b9d2b1b1d4383f30a791414f0625b/src/System.Windows.Forms/src/System/Windows/Forms/TabControl.cs
[native-control]: https://github.com/dotnet/winforms/blob/e4ede9b8979b9d2b1b1d4383f30a791414f0625b/src/System.Windows.Forms/src/System/Windows/Forms/Control.cs
[busy]: https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/8e7f1854d85d80fbaecc3d9d9167bb8ebcd94921/src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBusyHost.cs
[components]: https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/8e7f1854d85d80fbaecc3d9d9167bb8ebcd94921/docs/components/controls/COMPONENTS.md
[gallery-project]: https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/8e7f1854d85d80fbaecc3d9d9167bb8ebcd94921/samples/ControlGallery/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj
[visual-project]: https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/8e7f1854d85d80fbaecc3d9d9167bb8ebcd94921/tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.csproj
[W1]: https://learn.microsoft.com/en-us/windows/win32/controls/tab-controls#default-tab-control-message-processing
[W2]: https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.tabcontrol.doublebuffered
[W3]: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setlayeredwindowattributes
[W4]: https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features#layered-windows
[W5]: https://learn.microsoft.com/en-us/windows/win32/winmsg/using-windows#using-layered-windows
[W6]: https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-queryactctxw
[W7]: https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-activation_context_compatibility_information
