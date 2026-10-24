# Verified upstream API seams

> **Nguồn nhập:** [`MyDmsVn.BootstrapSourceGrid@e311144c27cd79a6c34aa0308ad029651c9e03c9`](https://github.com/chung6a8m/MyDmsVn.BootstrapSourceGrid/blob/e311144c27cd79a6c34aa0308ad029651c9e03c9/docs/UPSTREAM_API_SEAMS.md).
> Lệnh kiểm chứng và trạng thái hiện hành thuộc [TESTING](../../TESTING.md) và [ROADMAP](../../ROADMAP.md).
> Các kết quả PASS hoặc mốc hoàn tất trong nội dung gốc là bằng chứng lịch sử của upstream, không xác nhận graph WinFormsKit mới.

Exact integration facts verified against the pinned vendor baselines. Use this document instead of remembered assumptions; re-verify affected sections whenever a vendor pin changes.

## 1. Baselines

- Bootstrap5WinFormUI DateInput Unified Compact candidate: `8ae217ea8aa9a938ebd558f7071dc737f909e6a5` (prior T13 date-height candidate `1a1d1dd5b9f07763c11c06d75775e2217cd1f183`)
- SourceGrid: `f4e457b43582bf01892f50bdc74aa480531e5944`

At the earlier accepted `aba102e` baseline, the Bootstrap source implementing the theme, DPI, text, formatted-text, and lookup seams below was byte-for-byte unchanged from its previous pin (`cceba3c969e28726935793a1c6ca3772bed60a35`). Stage 0 compatibility evidence is in `verification/20260921-bootstrap-baseline-upgrade.md`.

The 2026-09-21 clean-worktree dual-TFM regression gate exercised these accepted seams with 241 tests per TFM after PR review. The demo typography work did not add an upstream API seam. Manual DPI/Designer coverage is tracked separately in `verification/20260921-regression-validation-and-doc-sync.md`.

The BCF-T03 candidate only corrects internal lookup chevron painting and propagation of the existing textbox palette/invalidation to its framework-owned accessory. Public/protected editor and adapter seams below are unchanged; the new active-cell rendering/cancel test and full243-row-per-target results are in [pin evidence](https://github.com/chung6a8m/MyDmsVn.BootstrapSourceGrid/blob/e311144c27cd79a6c34aa0308ad029651c9e03c9/docs/verification/20261021-lookup-affordance-pin.md). Native125/manual Designer and consumer acceptance remain separate.

The UI-NEXT T05 pin adds `BootstrapDecimalInput` with `Profile`, lossless `PendingText`,
read-only accepted `Value`, `BeginEditSession(decimal?, DecimalInputProfile)`,
`TryParsePending()`, `ProjectAcceptedValue(decimal?)`, and `PendingTextChanged`.
The protected native `Editor` seam remains inherited from `BootstrapTextBox`; no public
native child accessor or SourceGrid vendor change is needed.

The UI-NEXT T13 pin `1a1d1dd5b9f07763c11c06d75775e2217cd1f183` corrects the initial height of `BootstrapDateInput` before docking its private children. It changes no public editor or SourceGrid seam; the focused bounds and dual-target evidence is in [date height pin](https://github.com/chung6a8m/MyDmsVn.BootstrapSourceGrid/blob/e311144c27cd79a6c34aa0308ad029651c9e03c9/docs/verification/20261024-uin-t13-date-height-pin.md).

The DateInput correction pin `8ae217ea8aa9a938ebd558f7071dc737f909e6a5` preserves segment caret/selection after native arrows and popup cancellation, restores native editor focus after internal pointer dismissal, makes the active day follow keyboard focus in its calendar, and embeds an icon-only calendar trigger within one text surface. The internal trailing-accessory sizing and calendar focus-preview seams change only the DateInput composition; the standalone picker and public/protected API remain unchanged. SourceGrid stays at its pin above.

## 2. Bootstrap theme seams

Verified:

```csharp
BootstrapThemeManager.CurrentTheme
BootstrapThemeManager.ThemeChanged
BootstrapTheme.CreateDefault(BootstrapThemeMode.Light)
BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark)
BootstrapTheme.Colors
BootstrapTheme.Metrics
BootstrapTheme.Typography
ColorUtil.GetContrastingTextColor(...)
DpiScaler.DefaultDpi
DpiScaler.Scale(int logicalPixels, int dpi)
```

Relevant semantic tokens include `Surface`, `SurfaceSecondary`, `Text`, `MutedText`, `Border`, `Primary`, `Disabled`, `Focus`, `Hover`, and `Active`.

Bootstrap-owned integration metrics currently map from `SpacingXS`, `BorderWidth`, and `FocusBorderWidth`. Do not rescale SourceGrid/application-owned row heights, column widths, or scrollbars.

The pinned `BootstrapDataGridView` remains the reference pattern for theme-owned Font lifetime: construct theme font, replace only owned fonts on theme change, opt out on consumer Font assignment, unsubscribe/dispose owned resources.

`BootstrapTextBox.ApplyThemeFont()` and the lookup popup's `BootstrapDataGridView` preserve their existing owned `Font` when the next theme has an equivalent requested body typography token. They compare the immutable requested token rather than `Font.Name`, because `System.Drawing` can resolve an unavailable family to a different installed font name. WinForms may retain the same resolved `Font` reference on composite child labels when an equality-based property assignment is ignored; disposing it would leave popup and placeholder labels holding a disposed GDI object. Pinned regression tests cover both the default Light-to-Dark switch and an unavailable requested family while the popup is open.

`BootstrapSourceGrid` follows that requested-token comparison for its own theme font. It also reuses the existing font when different requested tokens resolve to equivalent installed fonts. SourceGrid's native scrollbar can retain the resolved `Font` reference when WinForms ignores an equivalent assignment; disposing that reference during a theme switch makes its GDI handle invalid. Dual-TFM regressions cover identical and different unavailable requested font families.

## 3. SourceGrid control and cell/View seams

Verified concrete inheritance:

```text
System.Windows.Forms.Panel
  -> SourceGrid.CustomScrollControl
  -> SourceGrid.GridVirtual
  -> SourceGrid.Grid
  -> BootstrapSourceGrid
```

Normal assignment:

```csharp
grid[row, column] = cell;
```

reaches a private SourceGrid `InsertCell(...)`; subclassing `SetCell(...)` does not intercept every normal assignment.

Approved no-patch visual seam:

```csharp
public override SourceGrid.Cells.ICellVirtual GetCell(int row, int column)
```

Call base, inspect the current View, replace only exact known default singleton identities, preserve all unknown/custom Views, and return the same cell.

Known default identities:

```text
SourceGrid.Cells.Views.Cell.Default
SourceGrid.Cells.Views.Header.Default
SourceGrid.Cells.Views.ColumnHeader.Default
SourceGrid.Cells.Views.RowHeader.Default
```

`ViewBase` exposes style state including `BackColor`, `ForeColor`, `Border`, `Padding`, and `Font`. `CellContext.Position` is available while Views prepare for drawing, allowing one shared ordinary-cell View to compute alternating rows without per-row View allocation.

## 4. Header seams

Generic, column, and row header Views are subclassable. Their default OS-themed backgrounds can be replaced with programmable DevAge visual elements while preserving SourceGrid View/controller behavior:

```text
DevAge.Drawing.VisualElements.Header
DevAge.Drawing.VisualElements.ColumnHeader
DevAge.Drawing.VisualElements.RowHeader
```

The integration replaces only exact SourceGrid default View singleton identities; consumer header Views remain authoritative.

## 5. Selection seams

`GridVirtual.Selection` is exposed as `IGridSelection`, while Bootstrap-relevant visual properties are on `SourceGrid.Selection.SelectionBase`:

```text
BackColor
FocusBackColor
Border
```

Changing `Grid.SelectionMode` recreates the selection object, so integration visual-ownership tracking must reset for the new instance.

Selection visual setters invalidate through their bound grid. During `CreateSelectionObject()`, an integration override that initializes visuals must respect SourceGrid binding order and must not leave duplicate decorators/bindings.

Consumer ownership rule: update a later theme value only when the current property still equals the value last applied by BootstrapSourceGrid.

## 6. SourceGrid editor base lifecycle

### 6.1 EditorBase

Verified public concepts:

```text
EnableEdit
EditableMode
UseCellViewProperties   // default true
```

`SetCellValue(...)` runs the SourceGrid validation/conversion path before writing the final cell value. The adapter may return a string/raw/logical object; SourceGrid remains responsible for converting it to the editor/cell declared value type through its existing validator/type-converter behavior.

### 6.2 EditorControlBase eager control creation

`EditorControlBase` creates and stores its WinForms editor control from `CreateControl()` during editor construction, before any cell starts editing.

The constructor assigns `mControl = CreateControl()`, rejects a null control, and immediately hides it. No grid or cell context is available during this eager construction.

Consequences:

- a Bootstrap editor adapter also creates its composite Bootstrap control eagerly;
- one adapter per cell can create thousands of controls/global theme subscriptions even if never edited;
- Bootstrap adapters must therefore be shared/owned at grid scope rather than created automatically per cell.

### 6.3 Attach/show/focus lifecycle

On first edit, the control is attached through SourceGrid's linked-control mechanism. SourceGrid remains responsible for editor bounds, showing/bringing to front/focus, and hiding at edit end.

The exact pinned first-edit order in `InternalStartEdit(...)` is:

```text
EditorBase.InternalStartEdit(...) validates cell/grid/active position
AttachControl(cellContext.Grid) when not already attached
    -> assign mGrid
    -> create LinkedControlValue
    -> add it to grid.LinkedControls
    -> subscribe Control.Validated and Control.KeyPress
set linked-control position
grid.ArrangeLinkedControls()
OnStartingEdit(cellContext, Control)
SetEditCell(cellContext)
SafeSetEditValue(current cell value)
ShowControl(Control)
```

`AttachControl(...)` is private, and the first protected callback receiving a `CellContext` is `OnStartingEdit(...)`, after `mGrid` and `grid.LinkedControls` have already been mutated. An adapter in this integration assembly therefore has no current SourceGrid seam that can reject a wrong-grid first edit before attachment.

`GetMinimumSize(...)` derives from the editor control's preferred size. Bootstrap editor plans must test preferred/minimum height without silently resizing SourceGrid rows.

### 6.4 Start/value/cancel/commit seams

Verified adapter seams include:

```text
OnStartingEdit(...)
SafeSetEditValue(...)
SetEditValue(...)
GetEditedValue()
OnSendCharToEditor(...)
InternalEndEdit(...)
CellContext.StartEdit()
CellContext.EndEdit(bool cancel)
```

Every concrete `EditorControlBase` subclass must implement the abstract `OnSendCharToEditor(char)` method. `SafeSetEditValue(...)` calls `SetEditValue(...)` and reports initialization failures through the grid user-exception path before applying the editor default. Commit calls `GetEditedValue()` through `ApplyEdit()`, then `SetCellValue(...)`; cancel calls `UndoEditValue()`, which restores the current cell value through `SafeSetEditValue(...)`, without writing an adapter-owned snapshot.

Commit flow uses `GetEditedValue()` then SourceGrid `SetCellValue(...)`. Cancel/reset uses the normal edit-value restore path rather than requiring an integration-side value snapshot system.

### 6.5 View-property propagation

`EditorControlBase.OnStartingEdit(...)` copies cell View `BackColor`, `ForeColor`, and `Font` into `Control` when `UseCellViewProperties == true`.

Legacy MVP styling relies on this behavior. Bootstrap-native adapters must set `UseCellViewProperties = false` so the composite Bootstrap control retains its own theme/font/background/border semantics.

`BootstrapSourceGridEditorStyler` must respect that opt-out and not restyle those adapters.

### 6.6 Control.Validated auto-end-edit behavior

`EditorControlBase` subscribes to `Control.Validated`. While an edit is active, validation/focus transitions can cause SourceGrid to call the edit context's `EndEdit(false)` path.

This is a critical seam for composite controls and especially `BootstrapLookupBox`: popup/result/child focus transitions must not accidentally commit the SourceGrid cell. Solve integration event ordering at the adapter boundary rather than disabling SourceGrid validation globally.

### 6.7 SourceGrid native TextBox compatibility reference

The built-in SourceGrid text editor creates `DevAgeTextBox` with border removed and validator assigned. On edit start it aligns View-dependent settings, initializes the value, and selects text. Its first-character path replaces current text with the typed character and moves the caret to the end.

`BootstrapTextBoxEditor` should preserve these observable edit-entry semantics while letting the Bootstrap control own visuals.

## 7. SourceGrid editor Factory limitation

`SourceGrid.Cells.Editors.Factory` is static/hardwired around built-in editor choices such as text, combo/standard-values, and UITypeEditor paths. No appropriate injection/registration seam was found for globally replacing built-in editors from this integration without changing SourceGrid.

Therefore the Bootstrap editor initiative uses explicit adapters/registry creation and does not override/patch the global factory.

## 8. BootstrapTextBox seams

At the pinned Bootstrap baseline:

- `BootstrapTextBox` is a composite `UserControl` implementing the framework connected-control contract;
- it contains a native text editor exposed to subclasses through a protected `Editor` seam;
- public behavior includes `Text`, `PlaceholderText`, validation state, icon/trailing-icon options, clear-button behavior, `ReadOnly`, password mode, `Clear()`, and `SelectAll()`;
- entering the outer control focuses the inner editor;
- editor events are forwarded through the Bootstrap control lifecycle;
- it subscribes to `BootstrapThemeManager.ThemeChanged` and unsubscribes in disposal;
- consumer Font assignment changes its theme-font ownership behavior.

Adapter consequence: use a narrow internal subclass only for protected caret/selection operations needed by SourceGrid first-character behavior. Do not expose the inner native editor publicly and do not use reflection.

## 9. BootstrapFormattedTextBox seams

At the pinned baseline:

- derives from `BootstrapTextBox`;
- separates formatted/display `Text` from canonical `RawValue`;
- exposes formatting modes including `None`, `General`, `Numeral`, `Date`, `Time`, `CreditCard`, and `Custom`;
- supports formatter/options, raw-value change behavior, caret mapping, and internal undo/redo semantics.

Adapter value contract:

```text
SetEditValue(cell logical value) -> RawValue
GetEditedValue()                  -> RawValue
SourceGrid                        -> final conversion/validation
```

Do not commit formatted `Text` or let `FormatMode` become a second declared cell type system.

## 10. BootstrapLookupBox seams

At the pinned baseline, the lookup model includes:

```text
DataSource
DisplayMember
ValueMember
Columns
SearchMembers
ResultsGrid
SelectedItem
SelectedValue
CommittedDisplayText
HasPendingText
HighlightedItem
MinimumSearchLength
DropDown sizing/behavior
unmatched-text policy
Enter behavior
validation/events
```

Useful selection/edit APIs include `SelectItem`, `SelectValue`, `ClearSelection`, and `CancelPendingEdit`.

`SelectionCommitted` distinguishes `Keyboard`, `Mouse`, `ExactMatch`, `CommitAndAdd`, `Programmatic`, and `Clear`. The public `ClearSelection()` path emits `Clear`, including programmatic clearing, so the SourceGrid adapter must not treat either `Programmatic` or `Clear` as a user selection that automatically ends the cell edit.

`BootstrapTextBox` forwards its native text editor's key-down event through the protected virtual
`OnEditorKeyDown` seam. `BootstrapLookupBox` overrides that seam and consumes Escape plus handled
Enter behavior itself. Consequently, returning `false` from the composite control's `ProcessCmdKey`
is insufficient for SourceGrid when focus is inside the native child. The integration subclass must
intercept closed-popup Escape before the lookup override and bridge closed-popup Enter after pending
text resolution succeeds; it must delegate popup-open key behavior unchanged.

Adapter value contract:

```text
configuration first
SetEditValue(cell logical value) -> SelectedValue
GetEditedValue()                  -> SelectedValue
```

Display text is not the logical cell value when `ValueMember` is configured.

### Lookup dropdown controller behavior

The pinned dropdown controller already owns popup/overlay tracking, focus-domain handling, result-grid navigation, message-filter installation/removal, anchor tracking, application/window deactivation handling, theme updates, result mouse commit, Escape cancellation, and disposal.

It deliberately restores/keeps focus on the lookup editor in several popup/result-grid paths. This makes the control a good candidate for a thin adapter, but SourceGrid's outer `Control.Validated` subscription still requires an explicit interaction test matrix.

Do not fork or duplicate the popup controller in this repository unless a standalone BootstrapLookupBox test proves a vendor defect independent of SourceGrid.

## 11. Bootstrap editor lifetime/ownership seam

One SourceGrid `EditorControlBase` can be assigned to multiple cells in the same grid because only one edit for that editor is active at a time. An editor/control that has attached to one grid must not be treated as safely shareable with another grid.

Approved integration model:

```text
BootstrapSourceGrid
    -> grid-owned editor registry
        -> small set of shared adapters
            -> eager Bootstrap control instances
```

The implemented public entry point is one read-only registry per grid:

```csharp
BootstrapSourceGridEditorRegistry BootstrapSourceGrid.BootstrapEditors { get; }

BootstrapTextBoxEditor CreateTextBox(Type valueType);
BootstrapFormattedTextBoxEditor CreateFormattedTextBox(Type valueType);
BootstrapLookupBoxEditor CreateLookupBox(Type valueType);
BootstrapDecimalInputEditor CreateDecimalInput(Type valueType, DecimalInputProfile profile);
```

Adapter constructors are internal and receive the creating grid. The constructor argument is
validated before eager `EditorControlBase` control creation. Registry creators reject a null
`valueType` before constructing an adapter.

The registry/grid must dispose adapters/controls that were created but never attached, because SourceGrid cannot own a control it never received through its linked-control path.

For the UI-NEXT T05 decimal adapter, the verified SourceGrid sequence remains
`SetEditValue(current model)` at start, `GetEditedValue()` then `SetCellValue(...)` at commit,
and `UndoEditValue()` reading the current model before `SetEditValue(...)` on cancel. The adapter
uses the UI control's atomic `BeginEditSession(value, profile)` for both start and cancel. A
failed parse in `GetEditedValue()` enters `ApplyEdit`'s existing `EditException` catch path and
keeps the editor active. No SourceGrid vendor change is needed.

Cross-grid adapter reuse is explicitly unsupported. The supported grid-owned registry creation flow will avoid encouraging it, but this baseline does not promise a deterministic fail-before-attach exception: the only callback available to an integration adapter runs after SourceGrid has already changed `mGrid`/`LinkedControls`. A post-attach guard would leave partially mutated SourceGrid state and must not be added. Deterministic fail-before-attach enforcement requires a separately approved SourceGrid pre-attach seam.

`EditorBase` is indirectly `IDisposable` through `DevAge.ComponentModel.ComponentLight`. That disposal contract removes the component from its site/container and raises `Disposed`; neither `EditorBase` nor `EditorControlBase` overrides it to dispose `EditorControlBase.Control`. The integration registry must therefore dispose the owned control as well as the editor component so both Bootstrap resources/theme subscriptions and SourceGrid component lifetime are released, including for an editor that was never attached.

## 12. Scrollbar seam

`SourceGrid.CustomScrollControl` owns horizontal/vertical native scrollbars and scrolling layout. Current work leaves this subsystem unchanged.

## 13. Upgrade re-verification checklist

When either vendor baseline changes, re-check at least:

```text
[ ] Bootstrap ThemeManager/theme tokens/DpiScaler/theme-font lifecycle
[ ] SourceGrid Grid/GridVirtual inheritance and GetCell interception assumptions
[ ] default Cell/Header/ColumnHeader/RowHeader View identities
[ ] ViewBase styling/shareability and header visual elements
[ ] selection interface/SelectionBase behavior
[ ] EditorBase UseCellViewProperties default and conversion path
[ ] EditorControlBase eager CreateControl behavior
[ ] attach/show/focus/hide lifecycle
[ ] Validated -> EndEdit behavior
[ ] first-character/text editor semantics
[ ] editor Factory extensibility
[ ] BootstrapTextBox protected/public seams and theme disposal
[ ] BootstrapFormattedTextBox RawValue/format/caret behavior
[ ] BootstrapLookupBox SelectedValue/popup/focus/deactivation behavior
[ ] CustomScrollControl ownership
```

Update this document and affected plans/tests in the same vendor-upgrade change.
