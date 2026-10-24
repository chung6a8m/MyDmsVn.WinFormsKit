# Bootstrap editor replacement architecture

> **Nguồn nhập:** [`MyDmsVn.BootstrapSourceGrid@e311144c27cd79a6c34aa0308ad029651c9e03c9`](https://github.com/chung6a8m/MyDmsVn.BootstrapSourceGrid/blob/e311144c27cd79a6c34aa0308ad029651c9e03c9/docs/EDITOR_REPLACEMENT.md).
> Lệnh kiểm chứng và trạng thái hiện hành thuộc [TESTING](../../TESTING.md) và [ROADMAP](../../ROADMAP.md).
> Các kết quả PASS hoặc mốc hoàn tất trong nội dung gốc là bằng chứng lịch sử của upstream, không xác nhận graph WinFormsKit mới.

## 1. Status and goal

This document records the implemented post-MVP editor architecture for `MyDmsVn.BootstrapSourceGrid`.

The completed MVP deliberately kept SourceGrid's built-in editors and only aligned their appearance. The editor initiative added Bootstrap-native editing controls without replacing SourceGrid's edit lifecycle.

Initial target controls:

1. `BootstrapTextBox`
2. `BootstrapFormattedTextBox`
3. `BootstrapLookupBox`

The target architecture is:

```text
SourceGrid edit lifecycle
        |
        v
thin Bootstrap editor adapter
        |
        v
Bootstrap5WinFormUI control
```

SourceGrid remains the grid/editor engine; Bootstrap5WinFormUI remains the control/theme owner; this repository owns only the adapter and lifetime integration.

For historical context, see [Archive](https://github.com/chung6a8m/MyDmsVn.BootstrapSourceGrid/tree/e311144c27cd79a6c34aa0308ad029651c9e03c9/docs/archive/).

## 2. Non-goals

This initiative does not:

- replace SourceGrid's `StartEdit` / `EndEdit` lifecycle;
- replace SourceGrid validation or final typed conversion;
- patch SourceGrid's global `Cells.Editors.Factory`;
- modify either vendor merely to simplify the adapter;
- create one Bootstrap composite editor control per cell by default;
- share one editor instance across multiple grid instances;
- make Bootstrap controls imitate SourceGrid cell Views by copying View colors/fonts into them;
- replace SourceGrid scrolling, selection, navigation, or painting;
- introduce `BootstrapCell`, `BootstrapColumn`, or other wrappers around SourceGrid abstractions.

## 3. Fixed constraints

- Repository: `chung6a8m/MyDmsVn.BootstrapSourceGrid`
- Public control: `MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid`
- Base type: `SourceGrid.Grid`
- TFMs: `net48;net8.0-windows`
- Bootstrap baseline: `aba102e33c48937fd92468791c287afda0a59e77`
- SourceGrid baseline: `f4e457b43582bf01892f50bdc74aa480531e5944`
- Vendor dependency direction remains one-way from this integration.
- GUI tests are STA, bounded, deterministic, and non-modal.

## 4. Ownership model

| Concern | Owner |
| --- | --- |
| Cell rendering | BootstrapSourceGrid Views |
| Editor rendering/theme | Bootstrap5WinFormUI editor control |
| Editor placement/visibility | SourceGrid |
| Start/commit/cancel lifecycle | SourceGrid |
| Final typed conversion | SourceGrid `EditorBase` / validator path |
| Bootstrap-specific interactive formatting | Bootstrap editor control |
| Lookup popup/search/highlight | `BootstrapLookupBox` |
| Grid navigation | SourceGrid |
| Editor instance lifetime | `BootstrapSourceGrid` editor registry |

The adapter must not become a second editor framework.

## 5. Verified SourceGrid lifecycle that adapters must preserve

At the pinned SourceGrid baseline, `EditorControlBase`:

- creates its WinForms `Control` eagerly in the editor constructor through `CreateControl()`;
- attaches the control to a grid only when editing starts;
- shows, positions, focuses, and hides that control as part of the normal edit lifecycle;
- commits through `GetEditedValue()` -> `SetCellValue(...)`;
- uses SourceGrid validation/type conversion before the cell receives its final value;
- uses `SafeSetEditValue(...)` when edit state is initialized/restored;
- handles first-character editing through `OnSendCharToEditor(...)`;
- listens to `Control.Validated` and may end the active edit when the control validates;
- calculates minimum editor size from `Control.GetPreferredSize(...)`.

These seams are canonicalized in `UPSTREAM_API_SEAMS.md` and must be re-verified after any vendor baseline change.

## 6. Critical lifetime rule

A Bootstrap input is a composite WinForms control and may subscribe to global theme events at construction. Because `EditorControlBase` eagerly creates its control, a naive editor-per-cell model can create thousands of controls and event subscriptions even when most cells are never edited.

Therefore:

1. Bootstrap editor adapters are shared at grid scope, normally one instance per column/configuration.
2. The same adapter may be assigned to many cells in the same `BootstrapSourceGrid` because SourceGrid permits only one active edit for that editor at a time.
3. An adapter created for one grid must not be reused by another grid.
4. `BootstrapSourceGrid` owns and disposes every adapter/control created through its editor registry, including controls that were created but never attached to `LinkedControls`.
5. No automatic per-cell Bootstrap editor creation is allowed in the default cell factory path.

Cross-grid reuse is unsupported, but the pinned SourceGrid attach sequence provides no integration-level callback before it mutates the editor's grid/linked-control attachment. The current no-vendor-change contract therefore does not promise a deterministic fail-before-attach runtime exception and does not add a post-attach guard that would leave SourceGrid in partially mutated state. Such enforcement requires a separately approved SourceGrid pre-attach seam.

## 7. Registry and public API direction

The integration exposes a grid-owned registry after the three vertical slices proved the common adapter contract:

```csharp
public BootstrapSourceGridEditorRegistry BootstrapEditors { get; }
```

The registry creates owned/shared adapter instances explicitly:

```csharp
BootstrapTextBoxEditor CreateTextBox(Type valueType);
BootstrapFormattedTextBoxEditor CreateFormattedTextBox(Type valueType);
BootstrapLookupBoxEditor CreateLookupBox(Type valueType);
BootstrapDecimalInputEditor CreateDecimalInput(Type valueType, DecimalInputProfile profile);
```

Each returned editor is intended to be configured once and then assigned to many cells in that grid. The adapter exposes its strongly typed Bootstrap control through a read-only property named `BootstrapControl` so existing Bootstrap control APIs remain available without wrapper duplication.

Example target usage:

```csharp
var nameEditor = grid.BootstrapEditors.CreateTextBox(typeof(string));
nameEditor.BootstrapControl.PlaceholderText = "Customer name";

for (var row = 1; row < grid.RowsCount; row++)
{
    grid[row, 1].Editor = nameEditor;
}
```

The registry does not mutate SourceGrid's global static editor `Factory` and does not silently replace a consumer-assigned editor.

## 8. Common adapter contract

All Bootstrap adapters derive from `SourceGrid.Cells.Editors.EditorControlBase` and must:

- set `UseCellViewProperties = false` by default;
- let the Bootstrap control own its theme, font, foreground, background, border, focus visuals, and internal child controls;
- preserve SourceGrid commit/cancel/navigation semantics;
- implement `SetEditValue`, `GetEditedValue`, and first-character behavior explicitly;
- avoid reflection into Bootstrap controls;
- use a narrow internal subclass only when protected Bootstrap members are required for caret/selection behavior;
- be safe before handle creation;
- work on both target frameworks.

`BootstrapSourceGridEditorStyler` continues to style only legacy SourceGrid editors where `UseCellViewProperties == true`; it must not restyle a Bootstrap adapter whose flag is false.

## 9. BootstrapTextBox adapter

### Value contract

```text
SourceGrid cell value -> BootstrapTextBox.Text
BootstrapTextBox.Text -> SourceGrid conversion/validation -> cell value
```

The adapter must preserve SourceGrid text-editor behaviors that users rely on:

- beginning edit with a typed character replaces the selected/current text with that character;
- caret is positioned after the inserted character;
- normal edit start selects all where the SourceGrid text editor does so;
- Enter/Escape/Tab behavior remains SourceGrid-owned unless the Bootstrap control consumes a key for an established input behavior;
- read-only and enabled states remain coherent.

Because the inner native editor is protected rather than public, a small internal subclass may expose only the caret/selection operations required by the adapter. Do not expose the native `TextBox` publicly.

## 10. BootstrapFormattedTextBox adapter

`BootstrapFormattedTextBox` separates formatted display text from canonical raw input.

The adapter contract is:

```text
SetEditValue(cellValue) -> BootstrapControl.RawValue
GetEditedValue()        -> BootstrapControl.RawValue
SourceGrid              -> converts RawValue to the declared cell value type
```

Rules:

- `Text` is display text, not the committed logical value.
- `FormatMode` and formatter options control interactive formatting only; they do not replace SourceGrid's declared cell type or validator.
- cancel/reset must restore the original logical value through the normal SourceGrid `SafeSetEditValue` path.
- tests must cover `None`, `General`, `Numeral`, `Date`, and `Time` behavior where supported by the pinned control, plus undo/redo and conversion failures.

## 11. BootstrapLookupBox adapter

`BootstrapLookupBox` has a richer state model: `DataSource`, `DisplayMember`, `ValueMember`, search/result configuration, `SelectedItem`, `SelectedValue`, popup/highlight state, pending text, and unmatched-text policy.

The adapter contract is:

```text
SetEditValue(cellValue) -> BootstrapControl.SelectedValue
GetEditedValue()        -> BootstrapControl.SelectedValue
```

Configuration is applied before edit value initialization.

`Text` and committed display text are never used as the SourceGrid logical value when a `ValueMember` is configured.

### Interaction matrix

The lookup stage must explicitly test and lock behavior for:

- Enter with popup closed/open;
- Escape with popup closed/open;
- Tab and Shift+Tab;
- Up/Down/PageUp/PageDown result navigation;
- mouse result selection;
- outside click;
- focus leaving and returning;
- Alt+Tab/application deactivation;
- runtime theme switch while popup is open;
- unmatched text;
- SourceGrid validation failure;
- disposal while popup/control has been created.

The key risk is SourceGrid's `Control.Validated` auto-end-edit behavior interacting with popup/focus transitions. The adapter must solve event ordering at the integration boundary rather than modifying lookup popup internals unless a proven vendor bug exists.

The pinned `BootstrapLookupBox` forwards key-down events from its focused native text editor and
consumes closed-popup Enter/Escape for its standalone lookup behavior. The SourceGrid integration
subclass must bridge that forwarded path back to SourceGrid: closed-popup Escape cancels the cell
edit, while closed-popup Enter first lets the lookup resolve pending text and commits the cell only
when resolution did not keep/open the popup. Popup-open navigation and cancellation remain owned by
the Bootstrap lookup control.

## 12. Sizing and layout

Bootstrap inputs have theme-driven preferred/minimum heights, while SourceGrid places the editor inside cell bounds.

Each vertical slice must verify:

- `GetPreferredSize`/SourceGrid minimum-size interaction;
- clipping in compact rows;
- behavior at 100%, 150%, and 200% DPI;
- no row-height mutation performed implicitly by the editor adapter;
- consumer-controlled row heights remain authoritative.

If a cell is smaller than the Bootstrap control's preferred height, the documented default is clipping/available-bounds behavior rather than silently resizing the grid.

## 13. Error and validation policy

- SourceGrid remains responsible for final cell validation/conversion.
- Bootstrap controls may present their own input/lookup validation state, but must not commit a value behind SourceGrid's lifecycle.
- Conversion errors must fail through the existing SourceGrid validation path and must not display an unbounded modal dialog during automated tests.
- The adapter must not swallow SourceGrid exceptions.

## 14. Performance and disposal gates

The registry API is stabilized by tests that prove:

- a large grid does not create one Bootstrap editor control per cell;
- one shared editor can edit many cells sequentially in one grid;
- unused registry-created editors are disposed with the grid;
- theme subscriptions owned by Bootstrap controls are released on disposal;
- no editor is accidentally kept alive by the registry after grid disposal;
- a cross-grid reuse attempt is rejected or otherwise prevented by the final implementation contract.

For the pinned no-vendor-change outcome, prevention means the public API and ownership model keep
creation grid-scoped and document cross-grid reuse as unsupported. It does not mean a runtime guard
throws after SourceGrid has already attached the control.

## 15. Delivery stages

1. **Stage 0 — Architecture and ownership spike**: prove lifecycle, sharing, sizing, disposal, and exact cross-grid enforcement seam.
2. **Stage 1 — BootstrapTextBox vertical slice**: establish the reference adapter contract.
3. **Stage 2 — BootstrapFormattedTextBox**: prove raw/display value adaptation while retaining SourceGrid conversion.
4. **Stage 3 — BootstrapLookupBox**: prove complex popup/focus/keyboard integration.
5. **Stage 4 — Registry, hardening, docs, expansion pattern**: expose the public grid-owned registry and prepare the repeatable pattern for `BootstrapComboBox` and later editors.

The completed implementation roadmap is retained under `archive/` for historical context.

### Future expansion pattern

Future Bootstrap editor work such as `BootstrapComboBox`, date/time, or numeric inputs must follow:

```text
prove value contract
prove SourceGrid lifecycle/first-key behavior
use grid-owned shared lifetime
lock popup/focus semantics if applicable
add registry creator only after adapter is stable
```

Those controls are not implemented by the initial editor initiative.

## 16. Definition of done

This initiative is complete when:

- all three Bootstrap editor adapters work on `net48` and `net8.0-windows`;
- SourceGrid still owns edit lifecycle, validation, conversion, and navigation;
- Bootstrap controls own their visual/theme behavior;
- no default per-cell composite editor allocation exists;
- grid-owned editor disposal is deterministic;
- lookup interaction tests are bounded and non-hanging;
- consumer custom editors remain untouched;
- demo/docs show the explicit shared-editor usage model;
- both vendor worktrees remain clean.

## 17. UI-NEXT T05 decimal adapter

`BootstrapDecimalInputEditor` is an opt-in, grid-owned editor for `decimal` cells. Its registry
factory requires an immutable `DecimalInputProfile`. The adapter exposes `Profile` for an admitted
change between edit sessions and rejects a change during an active SourceGrid edit. The control
itself freezes the profile while the candidate is pending.

On every SourceGrid `SetEditValue`, including cancel, the adapter calls
`BeginEditSession(currentModelValue, Profile)` and selects the display. SourceGrid's cancel path
reads the **current** value model; the adapter does not keep an edit-start snapshot. Each session
reset clears the previous cell's candidate and undo history. A first typed character replaces the
selected display through the protected native editor seam.

The native textbox remains right-aligned and vertically centered within SourceGrid editor
bounds, matching the prior receipt numeric editor without changing SourceGrid row height.

`GetEditedValue()` parses the exact pending candidate and returns a typed `decimal` or allowed
`null`. A rejected candidate throws before SourceGrid applies the model, so the edit and raw
candidate remain active. SourceGrid's existing `EditException` is raised once. On successful
`ApplyEdit`, SourceGrid validates and writes the model once, after which the adapter projects the
model's accepted value without recording another user edit. Native Enter/Escape/Tab handling and
editor placement remain SourceGrid-owned. The registry disposes the adapter and its eager control,
including when no edit was ever started.
