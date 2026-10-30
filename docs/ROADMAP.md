# Công việc hiện hành

Scope **UI Composition Recipes** ngày **2026-10-30** đã **COMPLETE trong phạm vi hẹp**: UIC-01 Filter Results và UIC-02 Compact Edit Form trong ControlGallery. [Plan](plans/20261030-001-ui-composition-recipes.md) sở hữu duy nhất trạng thái CMP-T01/CMP-T02, file map, evidence và giới hạn. Correction accessibility source/test `bdac29b` không đổi layout/API; Owner visual approval `1420f25` là historical decision riêng, không phải AT approval.

Đợt kích hoạt lịch sử chỉ cập nhật tài liệu và đường đọc; completion hiện có source/tests/evidence theo plan, không cấp product/native AT acceptance ngoài boundary đã ghi. [Plan phối hợp trong MyDmsVn](https://github.com/chung6a8m/MyDmsVn/blob/main/docs/plans/20261030-001-business-compact-composition-adoption.md) sở hữu CMP-T03–CMP-T05, skill/mapping/pilot và điều phối hai repo. Recipe source revision tách khỏi runtime toolkit pin của app; không nâng gitlink chỉ để lấy source ví dụ. Trang này chỉ điều hướng, không duy trì bảng tiến độ thứ hai và không yêu cầu repo Docs thứ ba.

## Scope TAB/BUS độc lập trước đó

Scope được chọn ngày **2026-10-29** gồm BootstrapTabs mới, HotTrack dùng chung và phương án 3B live busy overlay. [Thiết kế chung](design/20261029-tabs-hottrack-live-busy-overlay.md) giữ baseline, căn cứ source và các quyết định kỹ thuật; trạng thái cùng evidence/acceptance hiện hành tiếp tục thuộc từng plan dưới đây.

| Luồng thực thi | Plan sở hữu trạng thái |
|---|---|
| BootstrapTabs trực tiếp kế thừa TabControl, shared HotTrack/painter/close, demo | [TAB-01–06](plans/20261029-002-bootstrap-tabs-hottrack.md) |
| BootstrapBusyOverlayHost dùng child-layer native opacity, input/focus/lifecycle, demo và manifest | [BUS-01–06](plans/20261029-003-live-busy-overlay.md) |

Hai luồng TAB/BUS có thể làm song song ở source riêng; GUI execution và tích hợp Program/MainForm/API baseline phải tuần tự. Snapshot hoặc opaque fallback không thay live acceptance của BUS. Kích hoạt composition không đổi trạng thái, mở lại hay archive hai scope này; không suy completion từ việc merge.

## Scope trước và các điều kiện còn lại

Scope [chiều cao input và góc bo popup Select/LookupBox](plans/20261029-001-input-popup-rendering.md): [thiết kế kỹ thuật](design/20261029-input-popup-rendering.md) giữ căn cứ source, lịch sử sửa và quyết định geometry/backing/layout. T01–T07 đã có implementation và [candidate evidence](verification/20261029-input-popup-rendering.md): kết quả trước review giữ theo candidate lịch sử. [Stability evidence](verification/20261029-input-popup-rendering-stability.md) giữ full-suite production source; [Owner visual review](verification/20261029-input-popup-rendering-owner-visual-review.md) ghi APPROVED cho bộ ảnh canonical physical 100%/125% trên hai TFM. Consumer acceptance còn mở tại checkpoint toolkit đó; [evidence consumer MyDmsVn](https://github.com/chung6a8m/MyDmsVn/blob/16c24738a4758ccdceb1f3509f4e37adb0d03464/docs/evidence/ui-labs/20261029-input-popup-consumer-acceptance.md) sở hữu kết quả và quyết định phía app. Physical 150%/200% của scope toolkit vẫn chưa chạy.

Bốn findings PR #3 và regression Windows tương ứng được ghi ở [evidence sửa review](verification/20261029-input-popup-rendering-review-fixes.md). Đây là lịch sử candidate vòng một; không thay evidence candidate mới.

Candidate tiếp theo sau hai findings trên `632e8df` thuộc [evidence review vòng hai](verification/20261029-input-popup-rendering-round-two.md); report giữ riêng trạng thái physical DPI, Owner visual decision và consumer acceptance tại checkpoint đó.

[Evidence review vòng ba](verification/20261029-input-popup-rendering-round-three.md) giữ kết quả sau findings trên `803888a`, về caller Height trong handle recreation và bitmap export khác DPI. Các nhóm nghiệm thu chưa hoàn tất trong report đó là trạng thái tại checkpoint lịch sử; consumer acceptance đọc tại record app ở trên.

Scope được Owner chọn ngày 2026-10-28 — [khắc phục ba ngoại lệ Business Compact](plans/20261028-001-business-compact-exceptions.md) — đã hoàn tất trong phạm vi hẹp về disabled action, modal Font và multiline read-only. [Candidate evidence](verification/20261028-business-compact-exceptions.md) phân biệt source/cross-build với Windows/native và ghi quyết định Owner APPROVED ngày 2026-10-28; ba ngoại lệ đã CLOSED.

[A3 — Khởi tạo MyDmsVn.WinFormsKit](plans/20261024-001-a3-winformskit.md) tiếp tục sở hữu phạm vi, baseline, trạng thái bootstrap và các điều kiện còn lại của A3; [evidence A3](verification/20261024-a3-bootstrap.md) giữ nguyên kết quả theo candidate. Scope mới không mở lại hoặc thay thế kết quả đó. Trang này chỉ điều hướng; không duy trì bảng tiến độ thứ hai.

Kế hoạch/evidence upstream được truy qua [PROVENANCE](PROVENANCE.md) và component references khi cần. Import source không kích hoạt lại các kế hoạch đã hoàn tất hoặc dừng ở repositories nguồn.
