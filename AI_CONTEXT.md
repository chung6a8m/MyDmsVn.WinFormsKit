# Bối cảnh làm việc

## Scope active — Editable Lines và Master–Detail

Đọc [README](README.md) → [ROADMAP](docs/ROADMAP.md) → [plan mở rộng](docs/plans/20261030-002-editable-lines-master-detail-recipes.md) → [design](docs/design/20261030-editable-lines-master-detail-compositions.md) → [evidence](docs/verification/20261030-composition-expansion.md). User đã chọn active roadmap ngày **2026-10-30**; activation là docs-only, các runtime tasks ban đầu PLANNED và native/tests mới NOT RUN.

Ưu tiên **UIC-03 Editable Lines** trong GridGallery dùng lookup/decimal adapters đã có; tiếp theo **UIC-04 Master–Detail** trong ControlGallery với flat list read-only. Plan toolkit sở hữu CEX-K01–K03. Grid tests đã reference GridGallery; controls tests đã reference ControlGallery; không cần test owner/ProjectReference mới. Sample-only state/helpers, stable line identity, pending/committed/applied snapshot, local Apply/Restore là phạm vi; SQL, Save/Post, authorization/session, paste transactions và Business Compact thuộc product.

[Plan app](https://github.com/chung6a8m/MyDmsVn/blob/main/docs/plans/20261030-002-editable-lines-sal-adoption.md) sở hữu CEX-A01–A05. A01 bắt đầu ngay; A02 có thể mapping/preparation từ K01 source/results đã freeze, completion đối chiếu K03 handoff riêng UIC-03. Không chặn SAL bằng UIC-04 hoặc trạng thái tổng K03. A03 nhận UIC-04 riêng; A04 phải đánh giá helper, extraction chỉ khi chứng minh lặp lại. Recipe source revision và runtime pin tách biệt. Chưa chọn public API/runtime upgrade; native gates áp cho candidate visual mới/thay đổi của scope, không mở lại scopes cũ.

## UIC-01/UIC-02 đã hoàn tất

Đọc [README](README.md), [ROADMAP](docs/ROADMAP.md), rồi [plan UI Composition Recipes](docs/plans/20261030-001-ui-composition-recipes.md) cho scope được Owner chọn ngày **2026-10-30**. Plan đã **COMPLETE trong phạm vi hẹp** và là owner trạng thái duy nhất của CMP-T01/CMP-T02: hai mẫu trung tính UIC-01 Filter Results và UIC-02 Compact Edit Form trong ControlGallery. Owner đã APPROVED visual candidate `1420f25` ngày 2026-10-30; correction accessibility source/test `bdac29b` có focused automation riêng, không đổi layout và không là approval AT mới. Evidence ghi rõ captures physical100/125, attended matrix và giới hạn.

Tái sử dụng DemoPageHostForm, public controls/theme và test project hiện hữu; không thêm public production API, framework hoặc dữ liệu nghiệp vụ. [Plan app](https://github.com/chung6a8m/MyDmsVn/blob/main/docs/plans/20261030-001-business-compact-composition-adoption.md) sở hữu CMP-T03–CMP-T05, Business Compact mapping, repository skill, pilot Security và phối hợp hai repo. Recipe source revision phải tách khỏi runtime toolkit pin của app; không nâng gitlink chỉ để tham khảo mẫu. Chi tiết contract, file map, tests và future native gates nằm trong plan toolkit; không cần repo Docs thứ ba.

## Scope trước và evidence riêng

TAB/BUS là scope độc lập được chọn ngày 2026-10-29. Đọc [thiết kế tabs/HotTrack/live busy overlay](docs/design/20261029-tabs-hottrack-live-busy-overlay.md), [plan TAB](docs/plans/20261029-002-bootstrap-tabs-hottrack.md) và [plan BUS](docs/plans/20261029-003-live-busy-overlay.md) khi làm vùng đó. Baseline lập hai plan là toolkit `8e7f1854d85d80fbaecc3d9d9167bb8ebcd94921`; trạng thái, candidate evidence và quyết định nghiệm thu hiện hành vẫn do từng plan sở hữu.

BootstrapTabs phải trực tiếp kế thừa TabControl và độc lập BusinessCompactAppearance. HotTrack dùng helpers chung, giữ DrawItem/extensibility của control cũ. BUS là **3B live native opacity**: nội dung dưới overlay phải tiếp tục cập nhật trong cùng phiên; không thay bằng snapshot, không dùng opaque fallback để nhận live PASS. BootstrapBusyHost cũ giữ nguyên. Native paint/child-layer proof đi trước triển khai đầy đủ; demo có riêng cho cả hai control. Hai luồng có thể phát triển song song nhưng GUI execution và merge shared files/API fingerprint phải tuần tự.

[Plan chiều cao input và góc bo popup](docs/plans/20261029-001-input-popup-rendering.md) cùng [thiết kế kỹ thuật](docs/design/20261029-input-popup-rendering.md) tiếp tục sở hữu scope input/popup. T01–T06 đã triển khai; T07 có [candidate evidence](docs/verification/20261029-input-popup-rendering.md) cho toolkit tests và native desktop 100%. [Owner visual review](docs/verification/20261029-input-popup-rendering-owner-visual-review.md) ghi quyết định APPROVED đúng bộ ảnh canonical physical 100%/125% trên hai TFM. Consumer acceptance còn mở tại checkpoint toolkit này; trạng thái và quyết định phía app đọc tại [evidence consumer MyDmsVn](https://github.com/chung6a8m/MyDmsVn/blob/16c24738a4758ccdceb1f3509f4e37adb0d03464/docs/evidence/ui-labs/20261029-input-popup-consumer-acceptance.md). Physical 150%/200% của scope toolkit vẫn còn mở. Giữ các sửa lịch sử được liệt kê trong thiết kế và phân biệt geometry/bitmap với capture popup đang hiển thị thật.

Scope [ba ngoại lệ Business Compact](docs/plans/20261028-001-business-compact-exceptions.md) ngày 2026-10-28 vẫn đã hoàn tất. [Candidate evidence mới](docs/verification/20261028-business-compact-exceptions.md) ghi focused Windows/native tests và consumer physical-100/125 review đã PASS cho code/test execution revision `a71d64bd82eb40123eafef4c5a6832e53d7ef9ca`; Owner đã APPROVED candidate trình ngày 2026-10-28 nên ba ngoại lệ đã CLOSED trong phạm vi này. Gitlink consumer phải trỏ đúng commit chứa report hiện tại.

Khi làm bootstrap hoặc đánh giá điều kiện A3, [plan A3](docs/plans/20261024-001-a3-winformskit.md) vẫn sở hữu baseline/trạng thái và [evidence A3](docs/verification/20261024-a3-bootstrap.md) sở hữu kết quả thực tế đã ghi. Không lấy trạng thái từ roadmap/evidence cũ của các repositories nguồn hoặc chuyển historical PASS sang candidate mới.

## Chọn đúng owner

Sửa findings input/popup trong PR #3: đọc [stability evidence](docs/verification/20261029-input-popup-rendering-stability.md) cho full-suite production source và [Owner visual review](docs/verification/20261029-input-popup-rendering-owner-visual-review.md) cho candidate chụp mới nhất; [evidence physical 125%](docs/verification/20261029-input-popup-rendering-physical125.md) giữ lịch sử gate125%/broad-suite failures; [evidence vòng ba](docs/verification/20261029-input-popup-rendering-round-three.md), [evidence vòng hai](docs/verification/20261029-input-popup-rendering-round-two.md), [review-fixes vòng một](docs/verification/20261029-input-popup-rendering-review-fixes.md) và evidence trước review là lịch sử. Owner đã APPROVED bộ ảnh canonical; consumer acceptance khi đó còn mở, với kết quả tiếp theo thuộc record app ở trên. Physical 150%/200% của scope toolkit vẫn chưa hoàn tất.

- Controls/theme/rendering/input: `src/MyDmsVn.Bootstrap5WinFormUI`, `tests/MyDmsVn.Bootstrap5WinFormUI.Tests`, [controls references](docs/components/controls/).
- Grid/editor adapters: `src/MyDmsVn.BootstrapSourceGrid`, `tests/MyDmsVn.BootstrapSourceGrid.Tests`, [grid references](docs/components/grid/).
- Engine: pinned `vendor/sourcegrid`; [provenance](docs/PROVENANCE.md) trước khi xem xét thay pin.
- Native reproducer: `samples/ControlGallery`, `samples/GridGallery`; controls tests còn dùng `tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost`.
- Commands và scope: [TESTING](docs/TESTING.md). Consumer cutover: [CONSUMER_MIGRATION](docs/CONSUMER_MIGRATION.md).

Giữ ranh giới trong [ARCHITECTURE](docs/ARCHITECTURE.md). Chỉ đọc component contract và source/tests cần cho hành vi đang sửa; lịch sử upstream được truy qua exact links khi cần phân tích regression. Repository này không sở hữu SQL, session hay nghiệp vụ MyDmsVn.
