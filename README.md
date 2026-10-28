# MyDmsVn.WinFormsKit

Toolkit Windows Forms tập hợp **Bootstrap5WinFormUI** và **BootstrapSourceGrid** trong một repository, giữ hai assembly/public identities và pin engine SourceGrid riêng. Source được nhập từ dependency revisions mà MyDmsVn sử dụng; mapping nằm trong [PROVENANCE](docs/PROVENANCE.md).

Scope hiện hành là [khắc phục ba ngoại lệ Business Compact](docs/plans/20261028-001-business-compact-exceptions.md): disabled action, modal Font và multiline read-only. [Candidate evidence](docs/verification/20261028-business-compact-exceptions.md) ghi riêng source/cross-build và Windows/native/Owner approval còn chờ; source mới chưa đóng các ngoại lệ.

Phạm vi, trạng thái bootstrap và phần kiểm chứng/consumer migration của A3 vẫn thuộc [plan A3](docs/plans/20261024-001-a3-winformskit.md), với [evidence A3](docs/verification/20261024-a3-bootstrap.md) giữ nguyên theo candidate.

## Bắt đầu trên Windows

Cần .NET SDK tương thích `global.json` (`8.0.400`, `latestFeature`), Windows với runtimes .NET Framework 4.8/.NET 8 Desktop và desktop tương tác để chạy native tests/galleries. Reference assemblies để build `net48` được restore qua NuGet. Dùng VS2022 với workload .NET desktop hoặc CLI phù hợp; xem [TESTING](docs/TESTING.md) để chọn đúng scope.

```powershell
git clone --recurse-submodules https://github.com/chung6a8m/MyDmsVn.WinFormsKit.git
Set-Location MyDmsVn.WinFormsKit
.\build.ps1 -Configuration Release -Scope All
```

Mở `MyDmsVn.WinFormsKit.sln` cho cả controls và grid; mở `MyDmsVn.WinFormsKit.Controls.sln` khi chỉ làm UI core. Đặt gallery tương ứng làm startup project, chọn framework rồi chạy:

| Gallery | Project |
|---|---|
| Controls | `samples/ControlGallery/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj` |
| Grid/editors | `samples/GridGallery/MyDmsVn.BootstrapSourceGrid.Demo.csproj` |

Các projects giữ targets `net48;net8.0-windows`. Galleries là host phát triển với dữ liệu generic; màn hình nghiệp vụ thuộc repository sản phẩm.

## Đọc theo công việc

| Công việc | Điểm vào |
|---|---|
| Sửa controls, adapters hoặc dependency graph | [Kiến trúc](docs/ARCHITECTURE.md), [component references](docs/README.md#component-references) |
| Build, chạy tests, kiểm native behavior | [TESTING](docs/TESTING.md) |
| Đưa toolkit vào MyDmsVn và quay về pins cũ | [CONSUMER_MIGRATION](docs/CONSUMER_MIGRATION.md) |
| Source origin, notices, điều kiện package | [PROVENANCE](docs/PROVENANCE.md), [THIRD_PARTY_NOTICES](THIRD_PARTY_NOTICES.md), [RELEASING](docs/RELEASING.md) |
| Công việc hiện hành | [ROADMAP](docs/ROADMAP.md) |

Consumption ban đầu dùng source + `ProjectReference`; bootstrap không phát hành package hoặc đổi consumer app. License/notices được ghi riêng theo component, không có license MIT bao phủ toàn bộ repository.

Đọc [AGENTS.md](AGENTS.md) trước khi thay đổi source. [Chỉ mục tài liệu](docs/README.md) dẫn tới contracts, commands và nguồn thiết kế.
