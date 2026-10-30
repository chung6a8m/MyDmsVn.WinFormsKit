# Owner approval — live busy overlay và SourceGrid

Ngày quyết định: **30/10/2026**. Candidate được Owner chấp thuận: **`ba603d2b7a6c4967d44d2807e98921a1734d0afe`**, PR #5, gồm UI core busy overlay và GridGallery SourceGrid integration. Commit ghi quyết định này chỉ đổi tài liệu; không tạo candidate runtime mới.

## Quyết định trực tiếp của Owner

Owner xác nhận trong chat: “Tôi approve với Owner visual/live approval. Ghi ngoại lệ cho: nơi lưu artifact dùng chung.”

- **Owner visual/live approval: APPROVED** cho candidate và scope BUS hiện hành, gồm demo live overlay/SourceGrid đã trình và evidence physical100/125. Đây là quyết định của Owner, không suy từ automated PASS hoặc agent visual QA.
- **Nơi lưu artifact dùng chung: Owner-exempt** cho candidate này. Không yêu cầu upload/shared URL hoặc retention policy bên ngoài để đóng BUS acceptance. Trạng thái publication thực tế vẫn **NOT PUBLISHED**, không ghi thành uploaded/PASS. Local artifacts chỉ có retention theo worktree; Owner chấp thuận ngoại lệ không tạo cam kết lưu trữ bền vững. Không tự xóa local packs.
- Ngoại lệ Owner trước đó cho **150%/200% và mixed-DPI** tiếp tục áp dụng đúng candidate/scope này: **NOT RUN / Owner-exempt**, không PASS.

## Căn cứ kỹ thuật và giới hạn

[Evidence physical125](20261030-busy-overlay-physical125.md) ghi monitor125%, HWND/managed DPI120, native BUS9/9 và SourceGrid9/9 PASS, Controls181/181 và grid265/265 mỗi TFM, Build All sạch. [BUS-06](20261030-live-busy-overlay-acceptance.md), [SourceGrid](20261030-sourcegrid-busy-overlay.md) và [review vòng hai](20261030-pr5-round-two.md) giữ commands, hashes, kết quả cùng lịch sử theo từng candidate. Không đổi failed/historical observations thành PASS.

Local evidence packs: `artifacts/sdd/bus/physical125/frame-index.html`, `artifacts/sdd/bus/bus06-frame-index.html` và `artifacts/verification/sourcegrid-busy/frame-index.html`; inventory SHA256 và raw logs/frames vẫn local ignored, không commit. Các links local không phải shared URL.

**BUS-01–06 và BUS feature acceptance: ACCEPTED trong phạm vi candidate được duyệt, với các ngoại lệ trên.** Consumer MyDmsVn migration, hardware/third-party rendering hoặc popup ngoài sample chưa được nghiệm thu; không có package publication hoặc merge PR được thực hiện bởi quyết định này. Approval và ngoại lệ không tự áp dụng cho candidate có thay đổi runtime sau này; thay đổi đó cần evidence/Owner decision phù hợp. [Plan BUS](../plans/20261029-003-live-busy-overlay.md) là owner tiến độ hiện hành.
