# UI Composition Recipes

Hai mẫu thuộc assembly demo ControlGallery, dùng public controls của UI core. Consumer đọc và chuyển thể source; không reference assembly demo, không kế thừa `DemoFormBase`. Profile Default/Base14/Base16 là cấu hình Gallery; Business Compact, authorization, session và dữ liệu sản phẩm do app sở hữu.

## UIC-01 — Filter Results

**Purpose:** danh sách tham chiếu chỉ đọc, tìm kiếm và lọc trạng thái cùng một dataset, phản hồi rõ dữ liệu đã biết/chưa biết. Chạy Gallery → **Compositions → UIC-01 Filter Results**.

**Source:** [CompositionListDemoForm.cs sau correction accessibility `bdac29b`](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/bdac29bf4aca20db6618570ecd9a354630bc2e5f/samples/ControlGallery/CompositionListDemoForm.cs). Owner visual approval của candidate `1420f25` là lịch sử riêng; correction này không đổi layout/visual và không nhận lại approval đó. Recipe source SHA độc lập với runtime pin consumer. Xem [evidence](../verification/20261030-ui-composition-recipes.md).

| Vùng | Controls/API công khai | Ownership |
|---|---|---|
| Kịch bản demo, ngoài cấu trúc sản phẩm | `BootstrapComboBox.Items`, `SelectedIndexChanged` | Chỉ chọn loaded/empty/loading/unavailable giả lập; consumer bỏ vùng này |
| Tiêu đề và bộ lọc wrap | `BootstrapTextBox.Text`, `TextChanged`, `KeyDown`; `BootstrapComboBox.SelectedIndex`; `BootstrapButton.Click` | Mẫu đọc query đã trim nhưng không gán ngược `Text`; Enter chỉ truy vấn; Clear xóa search/status |
| Summary và bảng | `Label.Text`; `BootstrapDataGridView.DataSource`, `ReadOnly`, `EmptyStateText`, `Loading`, `LoadingText`; native columns | Dataset và summary dùng cùng query; `ResultSummary` không đặt tên accessibility tĩnh để `AccessibilityObject.Name` trả nội dung `Text` hiện tại; không có mutation, pagination hoặc count contract từ server |
| Bố cục/theming | `TableLayoutPanel`, `FlowLayoutPanel`, Dock; `BootstrapThemeManager.ThemeChanged` | Native primitives và label do mẫu tạo được theme trực tiếp; controls tự theme qua API public |

**Compatibility:** các controls/APIs trên có ở runtime MyDmsVn pin `433e5b53f979e37cc6b69bad6dfcde0975c7ecaf`. Source controls TextBox/ComboBox/Button/DataGridView và theme tại pin so với baseline implementation `7a1467f` không đổi; không cần nâng runtime gitlink để áp cấu trúc này. `DemoDataGridRowMetrics` là helper demo, không phải API core: consumer dùng row metrics của sản phẩm mình. Recipe source SHA và runtime SHA luôn là hai trường riêng.

**Layout:** một cột với các hàng AutoSize cho scenario/title/filter/summary; grid dùng phần còn lại. Filter `FlowLayoutPanel` wrap khi content hẹp, không ép minimum form width. Grid cho phép horizontal scroll để đọc mã/tên dài thay vì bỏ nội dung nhận diện. Không lấy outer Gallery size làm content size. Trường hợp nhỏ tự động kiểm ở client 480×430 logical pixels; native DPI/viewport thực xem evidence.

| State | Presentation |
|---|---|
| Loaded | Số kết quả khớp / số dòng dataset, bảng chỉ đọc |
| Empty dataset | `0 / 0`, “Chưa có dữ liệu tham chiếu.” |
| No matches | `0 / 4`, “Không có kết quả phù hợp bộ lọc.” |
| Loading | Overlay loading; “chưa biết số kết quả”, không tổng 0 giả |
| Unavailable | Bảng không chứa dữ liệu stale; thông báo lỗi và count chưa biết |

**Lifecycle:** đổi theme không dựng lại controls, không bind lại dữ liệu; selected row và search buffer giữ nguyên. Các event local nằm trong cây control do form sở hữu; form gỡ global `ThemeChanged` khi dispose, ngắt DataSource và dispose DataTable mình tạo khi replace/đóng. `DemoPageHostForm` và `MainForm` hiện hữu sở hữu page disposal. Không có async worker hoặc timer trong mẫu. Consumer phải bổ sung cancellation/session/authorization theo lifetime thực của mình.

**Verification:** `CompositionDemoFormTests` kiểm rows/count, raw native buffer/range/focus, per-character K/h/o, Enter/Tab/Shift+Tab, states, theme identity/selection, narrow content, route disposal và `AccessibilityObject.Name` của summary ở loaded/no-match/empty/loading/unavailable. [Evidence](../verification/20261030-ui-composition-recipes.md) phân biệt test automation, Gallery native review và Owner acceptance. Consumer cần đối chiếu source/public APIs tại pin của mình trước khi chuyển thể; kết quả toolkit không cấp acceptance cho sản phẩm.

**Pagination:** nếu consumer có phân trang, đọc [PaginationDemoForm](../../samples/ControlGallery/PaginationDemoForm.cs) và giữ contract nguồn của consumer. Mẫu này không bổ sung arbitrary page/count hay thay Sales keyset contract.

## UIC-02 — Compact Edit Form

**Purpose:** form minh họa bản nháp cục bộ với mã, tên, nhóm, trạng thái và ghi chú. Chạy Gallery → **Compositions → UIC-02 Compact Edit Form**. Rule mã/tên bắt buộc chỉ phục vụ demo, không phải validation contract của MyDmsVn.

**Source:** [CompositionEditDemoForm.cs sau correction accessibility `bdac29b`](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/blob/bdac29bf4aca20db6618570ecd9a354630bc2e5f/samples/ControlGallery/CompositionEditDemoForm.cs). Owner visual approval của candidate `1420f25` là lịch sử riêng; correction này không đổi layout/visual và không nhận lại approval đó. Recipe source SHA độc lập với runtime pin consumer. Xem [evidence](../verification/20261030-ui-composition-recipes.md).

| Vùng | Controls/API công khai | Ownership |
|---|---|---|
| Tiêu đề và field rows | `BootstrapLabel.Text`, `MaximumSize`; `BootstrapTextBox.Text`, `Focus`, `ValidationState` | Nhãn/lỗi tăng chiều cao theo nội dung; validation không ghi đè draft |
| Nhóm/trạng thái/ghi chú | `BootstrapComboBox.Items`, `SelectedIndex`; `BootstrapCheckBox.Checked`; `BootstrapTextBox.Multiline`, `WordWrap`, `ScrollBars` | Một snapshot gồm đủ năm trường; không có service hay DTO sản phẩm |
| Inline errors và summary | `BootstrapLabel.TextRole`; `BootstrapValidationState.Invalid` | Lỗi không đặt `AccessibleName` tĩnh để provider `Label` cung cấp nội dung `Text` hiện tại qua accessibility API; focus tới field lỗi đầu tiên; sửa hợp lệ chỉ gỡ lỗi tương ứng |
| Footer luôn truy cập được | `BootstrapButton.Click`, `Variant`, `Outline`; `BootstrapLabel.Text` | Save chính, Cancel phụ; `SaveFeedback` cũng cung cấp `Text` hiện tại qua accessibility API và chỉ tăng lần lưu khi valid |
| Bố cục/theme | `TableLayoutPanel`, `FlowLayoutPanel`, `Panel.AutoScroll`; `BootstrapThemeManager.ThemeChanged` | Field content cuộn cục bộ, footer nằm ngoài scroll; không dựng lại controls khi đổi theme |

**Compatibility:** TextBox/ComboBox/CheckBox/Button/Label và theme source không đổi giữa runtime MyDmsVn `433e5b53f979e37cc6b69bad6dfcde0975c7ecaf` và baseline toolkit `7a1467f`. Các API dùng trong mẫu đã có ở runtime pin; không cần nâng gitlink. `DemoFormBase` và snapshot trong form là chi tiết demo, không phải API production.

**Layout:** một cột AutoSize cho labels, controls và errors. `Panel.AutoScroll` giới hạn vùng fields; footer tự tăng theo feedback và wrapping commands, giữ Save/Cancel ngoài vùng cuộn. `MaximumSize` của nhãn được tính từ vùng content thật để nhãn tiếng Việt/lỗi dài xuống dòng. Không ép minimum width lớn, không sửa private children của composite. Tests kiểm client 360×320; đây là geometry tự động, không thay physical-DPI review.

| State/action | Presentation và dữ liệu |
|---|---|
| Initial | Snapshot mẫu có dữ liệu, chưa có lần lưu |
| Invalid Save | Draft nguyên vẹn, không tăng lần lưu; lỗi field và summary, focus tới lỗi đầu |
| Correct field | Gỡ lỗi field đã hợp lệ, giữ lỗi field còn lại |
| Valid Save | Chụp đủ năm trường vào snapshot trong bộ nhớ đúng một lần, cập nhật feedback |
| Edit then Cancel | Phục hồi snapshot vừa lưu, xóa lỗi/summary cũ; không đóng Gallery |
| Theme switch | Giữ draft, lựa chọn nhóm, native selection và control identity |

**Lifecycle:** form gỡ subscription global `ThemeChanged` trong `Dispose`; Form sở hữu và dispose cây controls/event local. Không async engine/timer/SQL. Consumer tự chuyển thể thành panel hoặc modal và giữ quyền sở hữu validation nghiệp vụ, dirty-close, authorization, session, cancellation và recovery của mình; không reference assembly demo.

**Verification:** `CompositionEditDemoFormTests` kiểm invalid/valid Save, snapshot đầy đủ, Cancel, Light/Dark khi đang có draft/selection, footer ở content hẹp, disposal, native per-key ABC/Tab/Shift+Tab và nội dung accessibility hiện tại của inline errors/summary/feedback. Với label ẩn và `Text` rỗng, test kiểm visibility/text và không còn lỗi cũ, không giả định provider phải trả tên rỗng. [Evidence](../verification/20261030-ui-composition-recipes.md) ghi candidate, kết quả thực tế và các native/Owner gates còn mở.
