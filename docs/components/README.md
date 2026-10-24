# Hợp đồng thành phần

Các tài liệu dưới đây được nhập từ đúng revision UI và adapter mà MyDmsVn đang tiêu thụ. Giữ nội dung hợp đồng của upstream để tra cứu khi bảo trì controls, rendering và grid adapters; không dùng các mốc hoàn tất hoặc kết quả PASS lịch sử để xác nhận graph WinFormsKit mới.

Lệnh kiểm chứng hiện hành nằm ở [TESTING](../TESTING.md); phạm vi và trạng thái thực thi nằm ở [ROADMAP](../ROADMAP.md). Hai `PACKAGE_README.md` giữ nội dung mô tả từng assembly/package từ upstream, không xác nhận package đã được phát hành.

## Controls

Nguồn: [`MyDmsVn.Bootstrap5WinFormUI@8ae217ea8aa9a938ebd558f7071dc737f909e6a5`](https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI/tree/8ae217ea8aa9a938ebd558f7071dc737f909e6a5/docs).

| Tài liệu | Nội dung |
|---|---|
| [Design system](controls/DESIGN_SYSTEM.md) | Theme tokens, typography, metrics và rendering |
| [Component contracts](controls/COMPONENTS.md) | Trách nhiệm và hợp đồng của các controls |
| [Public API baseline](controls/PUBLIC_API_BASELINE.md) | Public/protected API và compatibility fingerprint |
| [Compatibility](controls/COMPATIBILITY.md) | Hai target frameworks và giới hạn tương thích |
| [BootstrapLookupBox](controls/BOOTSTRAP_LOOKUP_BOX.md) | Lookup, giá trị đã commit và pending text |
| [BootstrapSelect](controls/BOOTSTRAP_SELECT.md) | Single/multiple selection, search và providers |
| [BootstrapInputGroup](controls/BOOTSTRAP_INPUT_GROUP.md) | Hợp thành nhóm input |
| [WinForms test execution](controls/WINFORMS_TEST_EXECUTION.md) | STA, xử lý lỗi và giới hạn chờ cho GUI tests |
| [Package README](controls/PACKAGE_README.md) | Mô tả package UI và ví dụ sử dụng |

## Grid adapter

Nguồn: [`MyDmsVn.BootstrapSourceGrid@e311144c27cd79a6c34aa0308ad029651c9e03c9`](https://github.com/chung6a8m/MyDmsVn.BootstrapSourceGrid/tree/e311144c27cd79a6c34aa0308ad029651c9e03c9/docs). SourceGrid engine tiếp tục ở pin `f4e457b43582bf01892f50bdc74aa480531e5944`.

| Tài liệu | Nội dung |
|---|---|
| [Editor replacement](grid/EDITOR_REPLACEMENT.md) | Adapter, editor lifecycle và resource ownership |
| [Upstream API seams](grid/UPSTREAM_API_SEAMS.md) | Các integration seams tại source pins đã ghi |
| [Known limitations](grid/KNOWN_LIMITATIONS.md) | Giới hạn có chủ đích của adapter |
| [Compatibility](grid/COMPATIBILITY.md) | Grid behavior, theme, DPI và Designer |
| [Package README](grid/PACKAGE_README.md) | Mô tả package adapter và ví dụ sử dụng |

Các liên kết đến evidence hoặc archive không được nhập trỏ về đúng repository và commit upstream. Nội dung lịch sử vẫn thuộc nguồn gốc của nó.
