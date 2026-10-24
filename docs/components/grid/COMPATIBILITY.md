# Compatibility contract

> **Nguồn nhập:** [`MyDmsVn.BootstrapSourceGrid@e311144c27cd79a6c34aa0308ad029651c9e03c9`](https://github.com/chung6a8m/MyDmsVn.BootstrapSourceGrid/blob/e311144c27cd79a6c34aa0308ad029651c9e03c9/docs/COMPATIBILITY.md).
> Lệnh kiểm chứng và trạng thái hiện hành thuộc [TESTING](../../TESTING.md) và [ROADMAP](../../ROADMAP.md).
> Các kết quả PASS hoặc mốc hoàn tất trong nội dung gốc là bằng chứng lịch sử của upstream, không xác nhận graph WinFormsKit mới.

## 1. Supported targets

The product targets:

```xml
<TargetFrameworks>net48;net8.0-windows</TargetFrameworks>
<UseWindowsForms>true</UseWindowsForms>
```

Both targets are first-class. A shared implementation is preferred.

## 2. Platform

`MyDmsVn.BootstrapSourceGrid` is Windows-only because both the integration and vendors use Windows Forms.

Do not introduce cross-platform assumptions, browser-based rendering, or platform-neutral abstractions that add complexity without a product requirement.

## 3. Language/runtime API compatibility

Code compiled for both TFMs must avoid runtime APIs unavailable on .NET Framework 4.8 unless isolated behind a compatibility helper or target-specific conditional.

Examples of risky categories:

- newer BCL helpers absent from .NET Framework 4.8;
- newer WinForms-only overrides/events not present in the .NET Framework surface;
- nullable/runtime annotations that alter generated metadata unexpectedly;
- APIs that require newer Windows SDK/runtime behavior.

Prefer a single compatible implementation. Use `#if` only for an actual API/platform difference.

Integration-owned projects that enable nullable reference types set `LangVersion` to `12.0` for both target frameworks. This is required because the `net48` compiler default is C# 7.3, and it matches the language version proven by the pinned Bootstrap5WinFormUI baseline. Keep this setting scoped to integration-owned projects; do not alter vendor project files.

## 4. SourceGrid API compatibility

`BootstrapSourceGrid` must remain substitutable for common `SourceGrid.Grid` use.

The integration must not silently redefine:

- `Redim` behavior;
- cell indexers;
- rows/columns semantics;
- positions/ranges/spans;
- selection and active position;
- editor activation/commit/cancel;
- keyboard navigation;
- controller dispatch;
- clipboard behavior;
- scrolling;
- SourceGrid events.

Styling changes must be regression-tested around these behaviors when the changed code can influence them.

Stage 4 intentionally hardens three command-key behaviors for
`BootstrapSourceGrid` after explicit product-owner approval:

- Shift+Tab commits a valid active edit, moves to the previous focusable cell,
  and resets selection to that cell instead of extending a range. SourceGrid's
  controller and `ProcessSpecialGridKey` dispatch still run before this behavior.
- Home and End, while not editing, move to the first and last focusable visible
  cell in the active row, skipping hidden or unselectable columns and resolving
  spanned cells to their canonical start position.
- Home and End remain editor-owned while a cell editor is active.
- F2 edit activation canonicalizes a covered span coordinate before SourceGrid
  resolves the cell editor and edit context. Other keys retain SourceGrid's
  existing selection and controller context.

All other command keys continue through SourceGrid's existing processing path.

## 5. Consumer customization compatibility

Consumer customizations take precedence over integration defaults when they are explicit.

Examples:

- consumer-assigned `Font` wins over automatic Bootstrap body font;
- consumer-assigned SourceGrid View should not be overwritten by a later theme change unless it was created/owned by the integration;
- consumer editor/controller replacement remains valid;
- consumer row heights/column widths are SourceGrid/application-owned and must not be blindly rescaled by Bootstrap code.

Bootstrap-native editors are opt-in through the grid-owned `BootstrapEditors` registry. The
integration does not replace SourceGrid's global editor factory or overwrite a consumer-assigned
custom editor. Create one adapter per grid/column/configuration and share it only within the
creating grid; cross-grid reuse is unsupported and is not guaranteed to fail before SourceGrid
attachment on the pinned baseline.

The adapters return `Text`, `RawValue`, and `SelectedValue` for text, formatted, and lookup
controls respectively. SourceGrid retains final declared-type conversion and validation.

The opt-in decimal adapter returns only the T04 parser's typed `decimal` or allowed `null`.
It keeps invalid pending text in the Bootstrap control and lets SourceGrid reject the edit.
The editor accepts `decimal` cell types, freezes its `DecimalInputProfile` during each edit,
and may be reused for another cell/profile only after the prior edit ends. SourceGrid still
owns the single model write and native navigation; the adapter projects the accepted model
value only after that write succeeds. Existing formatted-input behavior is unchanged.

## 6. Theme compatibility

The integration follows `BootstrapThemeManager.CurrentTheme` and `ThemeChanged`.

Supported theme behavior includes:

- light theme;
- dark theme;
- custom theme objects compatible with the pinned Bootstrap framework API;
- runtime theme changes after cells, selection, and editors already exist.

A theme change must not clear or reconstruct application data.

For the Stage 2 default ordinary-cell View, disabling the grid switches text to
the Bootstrap disabled color without changing the cell editor or editability
model. The integration does not infer a distinct read-only state merely from
`Cell.Editor == null`, because SourceGrid also uses editor-less cells for normal
display-only content and exposes no separate read-only View state at this seam.

## 7. Font compatibility

Default font comes from `CurrentTheme.Typography.Body`.

Ownership contract:

- integration-created font: integration owns/disposes;
- consumer-assigned font: consumer owns; integration must not dispose;
- theme changes update the font only while the control remains in theme-font mode.

## 8. DPI compatibility

BootstrapSourceGrid must work at common Windows scale factors including 100%, 125%, 150%, and 200%.

Rules:

- use `DpiScaler` for metrics introduced by this integration;
- avoid scaling SourceGrid-owned row/column dimensions a second time;
- invalidate/recalculate Bootstrap-owned visual metrics after relevant DPI changes;
- verify selection borders, padding, header content, editor bounds, and text clipping at multiple DPIs.

Where .NET Framework and .NET 8 expose different DPI lifecycle APIs, isolate the difference rather than duplicating entire control implementations.

## 9. Designer compatibility

The control must be safe in the WinForms Designer for both legacy and modern target workflows.

Designer requirements:

- constructor works without application bootstrap;
- no modal UI;
- no background timers/services required for construction;
- no assumption that handle/site/parent is already created;
- public serializable properties have stable defaults;
- repeated create/dispose cycles do not leak theme subscriptions/GDI resources;
- vendor submodule/project references resolve for the design-time build.

Designer validation is manual because successful compilation alone does not prove Designer functionality.

## 10. Binary/source dependency compatibility

During bootstrap, exact vendor commits are consumed through pinned Git submodules and `ProjectReference`.

Do not mix a project reference to one vendor version with a package reference to another copy/version of the same assembly.

When switching to packages, validate assembly identity, public APIs, and transitive dependencies for both TFMs.

## 11. Serialization and application state

MVP introduces no custom persistence format. SourceGrid/application state remains application-owned.

If future Bootstrap-specific properties are Designer-serialized, their defaults must be stable and compatible across both TFMs.

## 12. Accessibility compatibility

Do not remove or weaken accessibility behavior supplied by SourceGrid/WinForms. Integration-specific accessibility properties must be additive and must not change keyboard navigation semantics.

## 13. Performance compatibility

Theme integration must not materially degrade normal SourceGrid painting/scrolling.

Forbidden patterns include:

- rebuilding all grid data on theme change;
- allocating theme/GDI objects per cell on every paint when reusable objects are possible;
- calling full-form invalidation for a grid-only visual change;
- attaching duplicate theme handlers per cell.
- creating one composite Bootstrap editor or control per cell by default.

## 14. Compatibility validation matrix

Before MVP release, verify at least:

| Area | net48 | net8.0-windows |
|---|---|---|
| Restore/build | Required | Required |
| Unit tests | Required | Required |
| STA control tests | Required | Required |
| Light/dark theme | Required | Required |
| Runtime theme switch | Required | Required |
| Keyboard/editing | Required | Required |
| Selection/focus | Required | Required |
| Spans | Required | Required |
| Scrolling | Required | Required |
| 100%/150%/200% DPI smoke | Required | Required |
| Designer smoke | Required | Required |
| Demo app | Required | Required |

A documented environment limitation may prevent one manual matrix cell from being exercised in CI, but it does not remove the release requirement.
