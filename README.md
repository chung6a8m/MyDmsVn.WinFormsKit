# MyDmsVn.WinFormsKit

Toolkit Windows Forms tập hợp **Bootstrap5WinFormUI** và **BootstrapSourceGrid** trong một repository, giữ hai assembly/public identities và pin engine SourceGrid riêng. Source được nhập từ dependency revisions mà MyDmsVn sử dụng; mapping nằm trong [PROVENANCE](docs/PROVENANCE.md).

## Scope active

[Editable Lines và Master–Detail](docs/plans/20261030-002-editable-lines-master-detail-recipes.md) là roadmap được chọn ngày **2026-10-30**: ưu tiên **UIC-03 Editable Lines** trong GridGallery, tiếp theo **UIC-04 Master–Detail** read-only trong ControlGallery. [Design](docs/design/20261030-editable-lines-master-detail-compositions.md) chốt contracts/file map; [evidence mới](docs/verification/20261030-composition-expansion.md) đang NOT RUN. Activation này chỉ cập nhật tài liệu, chưa implement recipe hoặc nâng runtime pin.

Plan toolkit sở hữu CEX-K01–K03; [plan MyDmsVn](https://github.com/chung6a8m/MyDmsVn/blob/main/docs/plans/20261030-002-editable-lines-sal-adoption.md) sở hữu SAL pilot, Security mapping và helper assessment có điều kiện. Handoff theo từng recipe: SAL được chuẩn bị từ UIC-03 source/results đã freeze và hoàn tất theo UIC-03 acceptance/limits, không chờ UIC-04 hoặc toàn bộ K03 đóng. Điểm bắt đầu tiếp theo: [ROADMAP](docs/ROADMAP.md).

## UIC-01/UIC-02 đã hoàn tất

Scope [UI Composition Recipes](docs/plans/20261030-001-ui-composition-recipes.md) ngày **2026-10-30** đã **COMPLETE trong phạm vi hẹp**: UIC-01 Filter Results và UIC-02 Compact Edit Form trong ControlGallery. Correction accessibility sau review ở source/test `bdac29b` giữ layout/API/runtime pin không đổi và có evidence focused riêng; Owner visual approval `1420f25` vẫn là historical candidate decision, không phải AT approval. [Plan phối hợp trong MyDmsVn](https://github.com/chung6a8m/MyDmsVn/blob/main/docs/plans/20261030-001-business-compact-composition-adoption.md) sở hữu skill, Business Compact mapping và pilot sản phẩm. Recipe source revision tách khỏi runtime toolkit pin; không cần nâng gitlink để đọc mẫu.

## Các scope trước

Scope TAB/BUS được chọn ngày 2026-10-29 tiếp tục độc lập: [BootstrapTabs mới và HotTrack dùng chung](docs/plans/20261029-002-bootstrap-tabs-hottrack.md), [BootstrapBusyOverlayHost theo phương án 3B live opacity và demo](docs/plans/20261029-003-live-busy-overlay.md), cùng [thiết kế chung](docs/design/20261029-tabs-hottrack-live-busy-overlay.md). Trạng thái, evidence và nghiệm thu hiện hành thuộc từng plan; kích hoạt composition không đóng hoặc mở lại các task đó.

Scope [chiều cao input và góc bo popup Select/LookupBox](docs/plans/20261029-001-input-popup-rendering.md): [thiết kế kỹ thuật](docs/design/20261029-input-popup-rendering.md) ghi phân tích source, lịch sử các lần sửa và giải pháp. T01–T07 đã có implementation cùng [candidate evidence](docs/verification/20261029-input-popup-rendering.md): kết quả trước review được giữ theo candidate. [Evidence physical 125%](docs/verification/20261029-input-popup-rendering-physical125.md) giữ kết quả canonical và lỗi broad suite lịch sử. [Stability evidence](docs/verification/20261029-input-popup-rendering-stability.md) giữ full-suite sau sửa accessibility/clipboard observer; [Owner visual decision](docs/verification/20261029-input-popup-rendering-owner-visual-review.md) APPROVED cho bộ ảnh canonical 100%/125% trên hai TFM. Consumer acceptance còn mở tại checkpoint toolkit đó; [evidence consumer do MyDmsVn sở hữu](https://github.com/chung6a8m/MyDmsVn/blob/16c24738a4758ccdceb1f3509f4e37adb0d03464/docs/evidence/ui-labs/20261029-input-popup-consumer-acceptance.md) ghi kết quả và quyết định tiếp theo phía app. Physical 150%/200% của scope toolkit vẫn chưa chạy.

Scope [khắc phục ba ngoại lệ Business Compact](docs/plans/20261028-001-business-compact-exceptions.md) — disabled action, modal Font và multiline read-only — đã hoàn tất trong phạm vi được chọn. [Candidate evidence](docs/verification/20261028-business-compact-exceptions.md) ghi focused Windows/native tests, consumer physical-100/125 review và quyết định Owner APPROVED ngày 2026-10-28 cho code/test execution revision `a71d64bd82eb40123eafef4c5a6832e53d7ef9ca`; ba ngoại lệ đã CLOSED. Consumer phải pin đúng commit chứa report hiện tại.

Phạm vi, trạng thái bootstrap và phần kiểm chứng/consumer migration của A3 vẫn thuộc [plan A3](docs/plans/20261024-001-a3-winformskit.md), với [evidence A3](docs/verification/20261024-a3-bootstrap.md) giữ nguyên theo candidate.

Candidate sau bốn findings PR #3 có [evidence sửa review](docs/verification/20261029-input-popup-rendering-review-fixes.md) riêng, giữ nguyên là lịch sử.

[Evidence review vòng hai](docs/verification/20261029-input-popup-rendering-round-two.md) giữ kết quả pre-handle FlowLayout và alpha compositing. [Evidence vòng ba](docs/verification/20261029-input-popup-rendering-round-three.md) ghi kết quả tiếp theo cho caller Height trong handle recreation và bitmap export khác DPI; các điều kiện chưa hoàn tất trong report đó là trạng thái tại checkpoint lịch sử. Consumer acceptance đọc tại record app ở trên.

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
