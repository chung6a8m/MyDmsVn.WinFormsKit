# Bối cảnh làm việc

Đọc [README](README.md), [ROADMAP](docs/ROADMAP.md), [thiết kế tabs/HotTrack/live busy overlay](docs/design/20261029-tabs-hottrack-live-busy-overlay.md), rồi [plan TAB](docs/plans/20261029-002-bootstrap-tabs-hottrack.md) và [plan BUS](docs/plans/20261029-003-live-busy-overlay.md) cho scope mới được chọn ngày 2026-10-29. Hai plan bắt đầu ở **PLANNED** trên toolkit `8e7f1854d85d80fbaecc3d9d9167bb8ebcd94921`; trạng thái và evidence hiện hành thuộc từng plan.

BootstrapTabs phải trực tiếp kế thừa TabControl và độc lập BusinessCompactAppearance. HotTrack dùng helpers chung, giữ DrawItem/extensibility của control cũ. BUS là **3B live native opacity**: nội dung dưới overlay phải tiếp tục cập nhật trong cùng phiên; không thay bằng snapshot, không dùng opaque fallback để nhận live PASS. BootstrapBusyHost cũ giữ nguyên. Native paint/child-layer proof đi trước triển khai đầy đủ; demo có riêng cho cả hai control. Hai luồng có thể phát triển song song nhưng GUI execution và merge shared files/API fingerprint phải tuần tự.

## Scope trước và evidence riêng

[Plan chiều cao input và góc bo popup](docs/plans/20261029-001-input-popup-rendering.md) cùng [thiết kế kỹ thuật](docs/design/20261029-input-popup-rendering.md) tiếp tục sở hữu scope input/popup. T01–T06 đã triển khai; T07 có [candidate evidence](docs/verification/20261029-input-popup-rendering.md) cho toolkit tests và native desktop 100%. [Owner visual review](docs/verification/20261029-input-popup-rendering-owner-visual-review.md) ghi quyết định APPROVED đúng bộ ảnh canonical physical 100%/125% trên hai TFM. Consumer acceptance và physical 150%/200% vẫn còn mở. Giữ các sửa lịch sử được liệt kê trong thiết kế và phân biệt geometry/bitmap với capture popup đang hiển thị thật.

Scope [ba ngoại lệ Business Compact](docs/plans/20261028-001-business-compact-exceptions.md) ngày 2026-10-28 vẫn đã hoàn tất. [Candidate evidence mới](docs/verification/20261028-business-compact-exceptions.md) ghi focused Windows/native tests và consumer physical-100/125 review đã PASS cho code/test execution revision `a71d64bd82eb40123eafef4c5a6832e53d7ef9ca`; Owner đã APPROVED candidate trình ngày 2026-10-28 nên ba ngoại lệ đã CLOSED trong phạm vi này. Gitlink consumer phải trỏ đúng commit chứa report hiện tại.

Khi làm bootstrap hoặc đánh giá điều kiện A3, [plan A3](docs/plans/20261024-001-a3-winformskit.md) vẫn sở hữu baseline/trạng thái và [evidence A3](docs/verification/20261024-a3-bootstrap.md) sở hữu kết quả thực tế đã ghi. Không lấy trạng thái từ roadmap/evidence cũ của các repositories nguồn hoặc chuyển historical PASS sang candidate mới.

## Chọn đúng owner

Sửa findings input/popup trong PR #3: đọc [stability evidence](docs/verification/20261029-input-popup-rendering-stability.md) cho full-suite production source và [Owner visual review](docs/verification/20261029-input-popup-rendering-owner-visual-review.md) cho candidate chụp mới nhất; [evidence physical 125%](docs/verification/20261029-input-popup-rendering-physical125.md) giữ lịch sử gate125%/broad-suite failures; [evidence vòng ba](docs/verification/20261029-input-popup-rendering-round-three.md), [evidence vòng hai](docs/verification/20261029-input-popup-rendering-round-two.md), [review-fixes vòng một](docs/verification/20261029-input-popup-rendering-review-fixes.md) và evidence trước review là lịch sử. Owner đã APPROVED bộ ảnh canonical; consumer acceptance và physical 150%/200% vẫn chưa hoàn tất.

- Controls/theme/rendering/input: `src/MyDmsVn.Bootstrap5WinFormUI`, `tests/MyDmsVn.Bootstrap5WinFormUI.Tests`, [controls references](docs/components/controls/).
- Grid/editor adapters: `src/MyDmsVn.BootstrapSourceGrid`, `tests/MyDmsVn.BootstrapSourceGrid.Tests`, [grid references](docs/components/grid/).
- Engine: pinned `vendor/sourcegrid`; [provenance](docs/PROVENANCE.md) trước khi xem xét thay pin.
- Native reproducer: `samples/ControlGallery`, `samples/GridGallery`; controls tests còn dùng `tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost`.
- Commands và scope: [TESTING](docs/TESTING.md). Consumer cutover: [CONSUMER_MIGRATION](docs/CONSUMER_MIGRATION.md).

Giữ ranh giới trong [ARCHITECTURE](docs/ARCHITECTURE.md). Chỉ đọc component contract và source/tests cần cho hành vi đang sửa; lịch sử upstream được truy qua exact links khi cần phân tích regression. Repository này không sở hữu SQL, session hay nghiệp vụ MyDmsVn.
