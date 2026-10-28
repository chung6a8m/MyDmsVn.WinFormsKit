# Bối cảnh làm việc

Đọc [README](README.md), [ROADMAP](docs/ROADMAP.md), rồi [plan khắc phục ba ngoại lệ Business Compact](docs/plans/20261028-001-business-compact-exceptions.md) cho scope được chọn ngày 2026-10-28. [Candidate evidence mới](docs/verification/20261028-business-compact-exceptions.md) ghi focused Windows/native tests và consumer physical-100/125 review đã PASS cho code/test execution revision `a71d64bd82eb40123eafef4c5a6832e53d7ef9ca`; Owner approval còn PENDING nên ba ngoại lệ vẫn OPEN. Gitlink consumer phải trỏ đúng commit chứa report hiện tại.

Khi làm bootstrap hoặc đánh giá điều kiện A3, [plan A3](docs/plans/20261024-001-a3-winformskit.md) vẫn sở hữu baseline/trạng thái và [evidence A3](docs/verification/20261024-a3-bootstrap.md) sở hữu kết quả thực tế đã ghi. Không lấy trạng thái từ roadmap/evidence cũ của các repositories nguồn hoặc chuyển historical PASS sang candidate mới.

## Chọn đúng owner

- Controls/theme/rendering/input: `src/MyDmsVn.Bootstrap5WinFormUI`, `tests/MyDmsVn.Bootstrap5WinFormUI.Tests`, [controls references](docs/components/controls/).
- Grid/editor adapters: `src/MyDmsVn.BootstrapSourceGrid`, `tests/MyDmsVn.BootstrapSourceGrid.Tests`, [grid references](docs/components/grid/).
- Engine: pinned `vendor/sourcegrid`; [provenance](docs/PROVENANCE.md) trước khi xem xét thay pin.
- Native reproducer: `samples/ControlGallery`, `samples/GridGallery`; controls tests còn dùng `tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost`.
- Commands và scope: [TESTING](docs/TESTING.md). Consumer cutover: [CONSUMER_MIGRATION](docs/CONSUMER_MIGRATION.md).

Giữ ranh giới trong [ARCHITECTURE](docs/ARCHITECTURE.md). Chỉ đọc component contract và source/tests cần cho hành vi đang sửa; lịch sử upstream được truy qua exact links khi cần phân tích regression. Repository này không sở hữu SQL, session hay nghiệp vụ MyDmsVn.
