# Công việc hiện hành

Scope được Owner chọn ngày **2026-10-29**: [chiều cao input và góc bo popup Select/LookupBox](plans/20261029-001-input-popup-rendering.md). [Thiết kế kỹ thuật](design/20261029-input-popup-rendering.md) giữ căn cứ source, lịch sử sửa và quyết định geometry/backing/layout. T01–T07 đã có implementation và [candidate evidence](verification/20261029-input-popup-rendering.md): toolkit build/full/focused/native-100% gates PASS trên hai TFM; physical 125%+, Owner visual decision và consumer MyDmsVn acceptance còn mở, không ghi APPROVED/CLOSED.

Bốn findings PR #3 và regression Windows tương ứng được ghi ở [evidence sửa review](verification/20261029-input-popup-rendering-review-fixes.md). Đây là evidence candidate mới; ba nhóm nghiệm thu physical DPI, Owner visual và consumer vẫn chưa hoàn tất.

Candidate tiếp theo sau hai findings trên `632e8df` thuộc [evidence review vòng hai](verification/20261029-input-popup-rendering-round-two.md); giữ riêng physical DPI, Owner visual decision và consumer acceptance còn mở.

[Evidence review vòng ba](verification/20261029-input-popup-rendering-round-three.md) sở hữu candidate mới nhất sau findings trên `803888a`, về caller Height trong handle recreation và bitmap export khác DPI. Ba nhóm nghiệm thu bên trên tiếp tục chưa hoàn tất.

Scope được Owner chọn ngày 2026-10-28 — [khắc phục ba ngoại lệ Business Compact](plans/20261028-001-business-compact-exceptions.md) — đã hoàn tất trong phạm vi hẹp về disabled action, modal Font và multiline read-only. [Candidate evidence](verification/20261028-business-compact-exceptions.md) phân biệt source/cross-build với Windows/native và ghi quyết định Owner APPROVED ngày 2026-10-28; ba ngoại lệ đã CLOSED.

[A3 — Khởi tạo MyDmsVn.WinFormsKit](plans/20261024-001-a3-winformskit.md) tiếp tục sở hữu phạm vi, baseline, trạng thái bootstrap và các điều kiện còn lại của A3; [evidence A3](verification/20261024-a3-bootstrap.md) giữ nguyên kết quả theo candidate. Scope mới không mở lại hoặc thay thế kết quả đó. Trang này chỉ điều hướng; không duy trì bảng tiến độ thứ hai.

Kế hoạch/evidence upstream được truy qua [PROVENANCE](PROVENANCE.md) và component references khi cần. Import source không kích hoạt lại các kế hoạch đã hoàn tất hoặc dừng ở repositories nguồn.
