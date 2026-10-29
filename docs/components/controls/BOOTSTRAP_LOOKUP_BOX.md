# BootstrapLookupBox and BootstrapLookupColumn

> **Nguồn nhập:** [`MyDmsVn.Bootstrap5WinFormUI@8ae217ea8aa9a938ebd558f7071dc737f909e6a5`](https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI/blob/8ae217ea8aa9a938ebd558f7071dc737f909e6a5/docs/BOOTSTRAP_LOOKUP_BOX.md).
> Lệnh kiểm chứng và trạng thái hiện hành thuộc [TESTING](../../TESTING.md) và [ROADMAP](../../ROADMAP.md).
> Các kết quả PASS hoặc mốc hoàn tất trong nội dung gốc là bằng chứng lịch sử của upstream, không xác nhận graph WinFormsKit mới.

`BootstrapLookupBox` is a native WinForms, single-selection lookup for local application data. It keeps committed `SelectedItem`/`SelectedValue`, pending editor `Text`, and transient `HighlightedItem` separate. Searching or moving the highlight never commits a value and never changes `BindingSource.Position`.

## Standalone lookup

```csharp
var products = new BindingList<Product>
{
    new Product(1, "CF-A", "Cà phê rang xay"),
    new Product(2, "TRA-O", "Trà ô long")
};
var lookup = new BootstrapLookupBox
{
    DataSource = products,
    DisplayMember = nameof(Product.Name),
    ValueMember = nameof(Product.Id),
    SearchDebounceMilliseconds = 150,
    ShowRefreshButton = true,
    ShowAddNewButton = true
};
lookup.SearchMembers.Add(nameof(Product.Name));
lookup.SearchMembers.Add(nameof(Product.Code));
lookup.Columns.Add(new BootstrapLookupColumnDefinition { DataPropertyName = nameof(Product.Code), HeaderText = "Code" });
lookup.Columns.Add(new BootstrapLookupColumnDefinition { DataPropertyName = nameof(Product.Name), HeaderText = "Product", Width = 240 });
```

Search is Vietnamese-diacritic-insensitive by default. Multiple normalized tokens use AND semantics and may match different members. Results rank by worst token quality, total quality, `DisplayMember` best-match count, member priority, then source order. `SearchTextNormalizer` customizes search; `TextNormalizer` and `TextComparer` customize exact commit resolution.

An exact display match commits only when all matching rows have one distinct logical value. Duplicate display text with different values is ambiguous and blocks silent selection; duplicate rows with the same value resolve to the first source row. Empty text clears. Unmatched text follows `RestorePreviousSelection`, `KeepFocusWithValidationError`, or `CommitAndAdd`. The latter requires an add-capable local list and may use `CreateItemFromText` for typed models.

`RefreshResults()` raises `RefreshRequested`, reconciles the local source, and reapplies the query. Add New raises `AddNewRequested`; a valid returned `NewItem` is committed. Application exceptions are not swallowed.

Keyboard focus stays in the editor: Up/Down/Home/End/PageUp/PageDown move only highlight; Enter commits/resolves; Tab resolves before native traversal; Escape and `CancelPendingEdit()` discard pending text; F4, Down, and Alt+Down open. Application deactivation closes presentation only and preserves committed and pending state.

## Popup geometry và navigation state

Popup dùng shared rounded overlay với opaque native backing và decoration-safe content rectangle. Grid do popup sở hữu dùng internal borderless embedding policy qua theme/reopen; `BootstrapDataGridView` standalone tiếp tục giữ public `FixedSingle` behavior. Header, selected row, footer và scrollbar nằm trong content rectangle đã dành góc/viền, còn footer/layout metrics dùng cùng effective DPI từ controller.

Đổi `BorderRadius` khi popup đang mở chỉ refresh geometry/layout/owned Regions và placement trên cùng HWND. Đường này không gọi result synchronization, không đổi committed value/query/native caret/highlight/current row và không đặt lại `FirstDisplayedScrollingRowIndex` về highlighted row. Theme hoặc DPI refresh đầy đủ vẫn giữ native editor, selection và activation contracts hiện có.

Synthetic geometry/bitmap tests không thay native shown-popup verification. Scale vật lý và phần consumer còn lại được ghi theo candidate trong [evidence ngày 2026-10-29](../../verification/20261029-input-popup-rendering.md).

## DataGridView lookup column

```csharp
var lines = new BindingList<OrderLine>();
var binding = new BindingSource { DataSource = lines };
var productColumn = new BootstrapLookupColumn
{
    HeaderText = "Product",
    DataPropertyName = nameof(OrderLine.ProductId), // raw row value
    DataSource = products,
    ValueMember = nameof(Product.Id),
    DisplayMember = nameof(Product.Name)
};
productColumn.SearchMembers.Add(nameof(Product.Name));
productColumn.SelectionCommitted += (_, e) =>
{
    var line = (OrderLine)e.DataGridView.Rows[e.RowIndex].DataBoundItem;
    line.Unit = ((Product)e.Item!).Unit;
};
grid.AutoGenerateColumns = false;
grid.AllowUserToAddRows = true;
grid.Columns.Add(productColumn);
grid.DataSource = binding;
```

The cell stores raw `ValueMember` values and formats through `DisplayMember`. The reused internal editor closes old presentation, cancels debounce, detaches the old source/event context, clones mutable configuration, and initializes the next raw value. Native DataGridView traversal, validation, currency, and new-row `AddNew` remain authoritative.

## V1 boundaries

V1 is local and single-select. It does not provide remote paging, multiple selection, `DataTable` filtering, `BindingSource.Filter`, arbitrary cell templates, or public lookup cell/editing-control types. Use `BootstrapSelect` for managed multiple selection or remote-provider scenarios.
