# UI Composition Recipes

Hai mẫu thuộc assembly demo ControlGallery, dùng public controls của UI core. Consumer đọc và chuyển thể source; không reference assembly demo, không kế thừa `DemoFormBase`. Profile Default/Base14/Base16 là cấu hình Gallery; Business Compact, authorization, session và dữ liệu sản phẩm do app sở hữu.

## UIC-01 — Filter Results

**Purpose:** danh sách tham chiếu chỉ đọc, tìm kiếm và lọc trạng thái cùng một dataset, phản hồi rõ dữ liệu đã biết/chưa biết. Chạy Gallery → **Compositions → UIC-01 Filter Results**.

**Source:** [CompositionListDemoForm.cs](../../samples/ControlGallery/CompositionListDemoForm.cs); revision nguồn chính xác được ghi ở [evidence](../verification/20261030-ui-composition-recipes.md). Link local dùng để đọc working candidate; không coi baseline plan là revision đã chứa mẫu.

| Vùng | Controls/API công khai | Ownership |
|---|---|---|
| Kịch bản demo, ngoài cấu trúc sản phẩm | `BootstrapComboBox.Items`, `SelectedIndexChanged` | Chỉ chọn loaded/empty/loading/unavailable giả lập; consumer bỏ vùng này |
| Tiêu đề và bộ lọc wrap | `BootstrapTextBox.Text`, `TextChanged`, `KeyDown`; `BootstrapComboBox.SelectedIndex`; `BootstrapButton.Click` | Mẫu đọc query đã trim nhưng không gán ngược `Text`; Enter chỉ truy vấn; Clear xóa search/status |
| Summary và bảng | `Label.Text`; `BootstrapDataGridView.DataSource`, `ReadOnly`, `EmptyStateText`, `Loading`, `LoadingText`; native columns | Dataset và summary dùng cùng query; không có mutation, pagination hoặc count contract từ server |
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

**Verification:** tests `CompositionDemoFormTests` kiểm rows/count, raw native buffer/range/focus, per-character K/h/o, Enter/Tab/Shift+Tab, states, theme identity/selection, narrow content và route disposal. [Evidence](../verification/20261030-ui-composition-recipes.md) phân biệt test automation, Gallery native review và Owner acceptance. Consumer cần đối chiếu source/public APIs tại pin của mình trước khi chuyển thể; kết quả toolkit không cấp acceptance cho sản phẩm.

**Pagination:** nếu consumer có phân trang, đọc [PaginationDemoForm](../../samples/ControlGallery/PaginationDemoForm.cs) và giữ contract nguồn của consumer. Mẫu này không bổ sung arbitrary page/count hay thay Sales keyset contract.

## UIC-02 — Compact Edit Form

Contract mục này do CMP-T02 bổ sung sau khi source và tests tương ứng tồn tại. Không suy rằng một recipe đã được triển khai chỉ từ mục lục hoặc plan.
