# Kiến trúc WinFormsKit

## Mục tiêu và phạm vi

WinFormsKit đặt source controls và adapter SourceGrid trong cùng một repository để một sửa đổi control có thể đi cùng kiểm tra adapter. Hai thư viện tiếp tục là hai assembly; SourceGrid vẫn là dependency được pin riêng. Đây là cấu trúc A3 trong [đề xuất tổ chức tại Docs][design], với source được chọn từ dependency graph của [MyDmsVn tại baseline import][app].

Repository này sở hữu generic controls, adapters, galleries và tests tương ứng. Product sở hữu màn hình Sales/Goods/Catalog, DTO, session, permission, dirty-close và Save/Post. [Kế hoạch A3](plans/20261024-001-a3-winformskit.md) ghi phạm vi được giao và trạng thái; [provenance](PROVENANCE.md) ghi mapping import cùng các thay đổi đường dẫn.

## Dependency và ownership

| Thành phần | Trách nhiệm | Dependency trực tiếp |
|---|---|---|
| `src/MyDmsVn.Bootstrap5WinFormUI` | Controls, theme, rendering, input primitives, compatibility | Windows Forms và các foundation nội bộ |
| `src/MyDmsVn.BootstrapSourceGrid` | Styling grid, editor adapters, theme integration | UI core và SourceGrid |
| `vendor/sourcegrid` | Storage, cells, rows/columns/ranges, selection, navigation, editor lifecycle, scrolling và engine painting | Dependency tại gitlink; không phụ thuộc toolkit |
| `samples/ControlGallery` | Host tương tác cho generic controls | UI core |
| `samples/GridGallery` | Host tương tác cho grid và editors | Adapter, UI core và engine qua project graph |
| `tests/` | Behavioral/native regression của controls và adapter | Project owner, gallery và helper host tương ứng |

UI core không tham chiếu adapter hoặc SourceGrid. Không thêm lớp bọc chỉ để đổi tên row/cell/range của engine. SourceGrid giữ start/commit/cancel, final conversion/validation, placement và navigation; Bootstrap controls giữ giá trị nhập, theme và focus visuals. Adapter nối hai lifecycle qua seams đã kiểm tra trong source. Các ranh giới này kế thừa [hợp đồng của adapter tại pin import][grid-rules].

Theme và DPI dùng primitives hiện có của UI core. Không tạo một theme manager thứ hai, patch static editor factory hoặc rescale row/column/scrollbar do engine hay consumer sở hữu. Adapter do registry tạo thuộc grid đó và được dispose cùng owner; không chia sẻ một editor adapter giữa nhiều grid. Consumer custom editors giữ semantics của mình.

## Identities và compatibility

| Project | Assembly / package identity | Root namespace | Targets |
|---|---|---|---|
| UI core | `MyDmsVn.Bootstrap5WinFormUI` | `MyDmsVn.Bootstrap5WinFormUI` | `net48;net8.0-windows` |
| Grid adapter | `MyDmsVn.BootstrapSourceGrid` | `MyDmsVn.Bootstrap5WinFormUI` | `net48;net8.0-windows` |

Đây là identities của [UI project gốc][ui-project] và [adapter project gốc][grid-project]. Namespace chung của adapter là intentional compatibility; không suy từ tên folder để đổi namespace. Public control tiếp tục là `MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid`, trực tiếp kế thừa `SourceGrid.Grid`.

Galleries đổi vị trí nhưng giữ project/assembly filenames:

- `samples/ControlGallery/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj`.
- `samples/GridGallery/MyDmsVn.BootstrapSourceGrid.Demo.csproj`.

Hai test suites giữ tên project cũ. `tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost` vẫn là helper executable của controls tests, không phải test suite thứ ba. Source import giữ implementation/API; adaptations cho bootstrap tập trung vào project references, solution, build entry points và documentation ownership. Các delta cụ thể thuộc [provenance](PROVENANCE.md).

## Điểm build và chạy

| Điểm vào | Graph |
|---|---|
| `MyDmsVn.WinFormsKit.Controls.sln` | UI core, ControlGallery, controls tests, VisualStyleHost; không có adapter/engine |
| `MyDmsVn.WinFormsKit.sln` | Bốn project trên cùng adapter, GridGallery, adapter tests và SourceGrid: tám project |
| `build.ps1 -Scope Controls` / `-Scope All` | Locked restore và build solution tương ứng |
| `test.ps1 -Scope Controls` / `-Scope All` | Chạy test owner tương ứng, chọn framework/filter khi cần |

SDK baseline là `8.0.400` với `rollForward=latestFeature`, theo [toolchain của consuming app][app-sdk]. Đây là adaptation có chủ đích so với [UI upstream SDK baseline `8.0.100`][ui-sdk]. Các project tiếp tục hỗ trợ cả hai targets; product tiêu thụ `net48`. Build từ repository root để SDK và root props được resolve nhất quán. [TESTING](TESTING.md) sở hữu commands và prerequisites.

Bảy projects do toolkit sở hữu giữ `packages.lock.json`. Root `Directory.Build.targets` khai báo build-only `Microsoft.NETFramework.ReferenceAssemblies` `1.0.3` cho `net48` để tránh SDK chọn implicit reference khác theo targeting pack cài trên máy. Dùng `VersionOverride` khi project áp central package management, như SourceGrid. Engine giữ central package versions và source của submodule; không ghi lockfile mới vào vendor. Những adaptation build này được ghi trong [PROVENANCE](PROVENANCE.md).

Workflow [build.yml](../.github/workflows/build.yml) build trên `windows-2022` và kiểm lockfiles/vendor sạch. Workflow không chạy native tests hoặc publish package; một build job thành công không đóng Windows/native acceptance.

SourceGrid có transitive dependencies liên quan SqlClient tại [project gốc][engine-project]. Việc assembly đó có trong closure không có nghĩa gallery cần SQL configuration hoặc database startup. Không gỡ dependency engine trong bootstrap chỉ vì tên package.

## Source consumption và distribution

Consumer dùng một revision WinFormsKit và `ProjectReference` tới thư viện cần dùng. `vendor/sourcegrid` là submodule duy nhất của toolkit; không giữ thêm UI hoặc BSG submodule song song với source đã import. Không cùng tham chiếu project và package có cùng assembly identity.

Consumer migration là thay đổi riêng theo [runbook](CONSUMER_MIGRATION.md). Bootstrap không đổi graph của MyDmsVn, không đổi business/runtime contracts và không archive repositories nguồn. SourceGrid upgrade cần review riêng; không dùng `git submodule update --remote` để vô tình đổi pin.

Package publication có prerequisites riêng trong [RELEASING](RELEASING.md). Giữ metadata package/assembly không chứng minh package có thể phát hành. License/notices theo từng component được ghi trong [THIRD_PARTY_NOTICES](../THIRD_PARTY_NOTICES.md); repository không áp một license MIT chung lên toàn bộ source import.

## Quyền sở hữu tài liệu

Public API, component behavior, build/test commands và candidate evidence thuộc repository này. Cross-repository intent/decisions thuộc MyDmsVn.Docs. [Component references](README.md#component-references) giữ contracts theo source owner; các kết quả upstream lịch sử không trở thành PASS cho candidate toolkit.

[design]: https://github.com/chung6a8m/MyDmsVn.Docs/blob/57cf5e0f506ac0c313e9d659939d863325250b53/proposals/2026-10-24-organization.md#4-gom-toolkit-giữ-component-boundaries
[app]: https://github.com/chung6a8m/MyDmsVn/tree/6c385d7630b6b21e915e244113c214aa616327f6
[grid-rules]: https://github.com/chung6a8m/MyDmsVn.BootstrapSourceGrid/blob/e311144c27cd79a6c34aa0308ad029651c9e03c9/AGENTS.md
[ui-project]: https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI/blob/8ae217ea8aa9a938ebd558f7071dc737f909e6a5/src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj
[grid-project]: https://github.com/chung6a8m/MyDmsVn.BootstrapSourceGrid/blob/e311144c27cd79a6c34aa0308ad029651c9e03c9/src/MyDmsVn.BootstrapSourceGrid/MyDmsVn.BootstrapSourceGrid.csproj
[app-sdk]: https://github.com/chung6a8m/MyDmsVn/blob/6c385d7630b6b21e915e244113c214aa616327f6/global.json
[ui-sdk]: https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI/blob/8ae217ea8aa9a938ebd558f7071dc737f909e6a5/global.json
[engine-project]: https://github.com/chung6a8m/sourcegrid/blob/f4e457b43582bf01892f50bdc74aa480531e5944/SourceGrid/SourceGrid.csproj
