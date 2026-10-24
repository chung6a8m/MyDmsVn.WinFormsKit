# Nguồn và license theo component

WinFormsKit hợp nhất source theo yêu cầu của Owner. Việc tổ chức repository không cấp một license mới cho toàn bộ nội dung và không tự đáp ứng điều kiện phát hành package. Mapping source đầy đủ ở [PROVENANCE](docs/PROVENANCE.md).

| Component | Trạng thái tại nguồn import | Notice được giữ |
|---|---|---|
| MyDmsVn.BootstrapSourceGrid | MIT, copyright (c) 2026 chung6a8m | [Bản nguyên văn](licenses/MyDmsVn.BootstrapSourceGrid.LICENSE.txt); áp dụng cho source adapter, gallery và tests nhập từ repository này |
| MyDmsVn.Bootstrap5WinFormUI | Source pin chưa khai báo repository LICENSE hoặc package license; upstream ghi quyết định license/distribution còn mở | Giữ source headers; xem [release policy nguồn][ui-release] và [decisions nguồn][ui-decisions]. Không tự áp MIT của adapter lên UI |
| SourceGrid và source đi kèm trong fork | Notice MIT-style của Davide Icardi, Darius Damalakas cùng các notices trong source | `vendor/sourcegrid/SourceGrid/SourceGrid.License.txt`; [bản sao notice chính](licenses/SourceGrid.LICENSE.txt) phục vụ tra cứu |

SourceGrid được giữ dưới dạng pinned submodule, gồm nguyên source headers và notices của fork. Bản sao notice chính không thay thế việc giữ các notices khác trong submodule khi phân phối. Các package NuGet bắc cầu giữ license riêng của package; cần review dependency/license graph trong công việc phát hành được giao riêng.

Không đặt `LICENSE` MIT ở root vì điều đó có thể khiến người đọc hiểu sai phạm vi license của UI. Public package publication tiếp tục phụ thuộc các điều kiện trong [RELEASING](docs/RELEASING.md), bao gồm source/package equivalence và quyết định license còn thiếu. Không có kiểm tra availability feed mới trong lần bootstrap này.

[ui-release]: https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI/blob/8ae217ea8aa9a938ebd558f7071dc737f909e6a5/docs/RELEASING.md
[ui-decisions]: https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI/blob/8ae217ea8aa9a938ebd558f7071dc737f909e6a5/docs/DECISIONS.md
