# Bối cảnh làm việc

Đọc [README](README.md), rồi [plan A3 hiện hành](docs/plans/20261024-001-a3-winformskit.md) nếu làm bootstrap hoặc đánh giá điều kiện đóng. Plan sở hữu baseline và trạng thái; [verification report](docs/verification/20261024-a3-bootstrap.md) sở hữu kết quả thực tế. Không lấy trạng thái từ roadmap/evidence cũ của các repositories nguồn.

## Chọn đúng owner

- Controls/theme/rendering/input: `src/MyDmsVn.Bootstrap5WinFormUI`, `tests/MyDmsVn.Bootstrap5WinFormUI.Tests`, [controls references](docs/components/controls/).
- Grid/editor adapters: `src/MyDmsVn.BootstrapSourceGrid`, `tests/MyDmsVn.BootstrapSourceGrid.Tests`, [grid references](docs/components/grid/).
- Engine: pinned `vendor/sourcegrid`; [provenance](docs/PROVENANCE.md) trước khi xem xét thay pin.
- Native reproducer: `samples/ControlGallery`, `samples/GridGallery`; controls tests còn dùng `tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost`.
- Commands và scope: [TESTING](docs/TESTING.md). Consumer cutover: [CONSUMER_MIGRATION](docs/CONSUMER_MIGRATION.md).

Giữ ranh giới trong [ARCHITECTURE](docs/ARCHITECTURE.md). Chỉ đọc component contract và source/tests cần cho hành vi đang sửa; lịch sử upstream được truy qua exact links khi cần phân tích regression. Repository này không sở hữu SQL, session hay nghiệp vụ MyDmsVn.
