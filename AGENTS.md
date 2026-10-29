# Làm việc trong MyDmsVn.WinFormsKit

- Bắt đầu bằng `README.md`, `AI_CONTEXT.md` và files liên quan; `docs/ROADMAP.md` dẫn tới plan được chọn cho công việc hiện hành. Không chọn việc từ lịch sử upstream.
- Viết tài liệu hướng người dùng bằng tiếng Việt; giữ identifiers, diacritics và exact source citations. Markdown UTF-8 không BOM; theo `.editorconfig`/`.gitattributes`.
- UI core và grid adapter cùng repo nhưng giữ assembly/package/public identities và targets `net48;net8.0-windows`; xem `docs/ARCHITECTURE.md`.
- BSG → UI core + SourceGrid; UI core không phụ thuộc adapter/engine. Không đổi namespace BSG chỉ vì tên folder.
- Generic controls/editors thuộc toolkit; DTO, session, permission và nghiệp vụ thuộc app. Không tạo framework abstraction chỉ để đủ lớp.
- SourceGrid sở hữu grid lifecycle/navigation/conversion; dùng seams đã kiểm trong pinned source và `docs/components/grid/`, không đoán API.
- Theme, DPI, input primitives dùng hạ tầng UI core hiện có. Giữ native focus/caret/accessibility và Designer construction không phụ thuộc app startup.
- Dispose tài nguyên/GDI/events do mình sở hữu; không dispose tài nguyên consumer. Adapter không được chia sẻ giữa các grid instances.
- `vendor/sourcegrid` là exact gitlink. Không dùng `submodule update --remote`; provenance và notices thuộc `docs/PROVENANCE.md`/`THIRD_PARTY_NOTICES.md`.
- Chọn tests theo diff trong `docs/TESTING.md`; giữ hai test owners và helper host. Không xóa regression chỉ vì source đã di chuyển.
- Native tests chạy STA, tuần tự, message pumping hữu hạn, không modal waits; giữ hang diagnostics 5 phút. Không thêm fail-fast vào production chỉ để dễ test.
- Ghi candidate, commands, discovered/executed/pass/fail/skip và limitations; thiếu Windows/desktop ghi NOT RUN/BLOCKED. Historical PASS không chứng minh candidate mới.
- `docs/plans/20261029-001-input-popup-rendering.md` sở hữu công việc hiện hành về chiều cao input và góc bo popup; đọc cùng `docs/design/20261029-input-popup-rendering.md` trước khi sửa. Giữ native ComboBox, owned inner host/AA coverage và constructor handle-free; radius-only refresh không được reset navigation.
- `docs/plans/20261028-001-business-compact-exceptions.md` tiếp tục sở hữu scope đã hoàn tất về disabled action, modal Font và multiline read-only; không mở lại lịch sử đó. Source/cross-build không thay Windows/native hoặc Owner approval của candidate mới.
- `docs/plans/20261024-001-a3-winformskit.md` tiếp tục là owner trạng thái A3; entry pages chỉ link. Evidence theo từng candidate nằm trong `docs/verification/`; giữ nguyên lịch sử A3.
- Public API hoặc behavior đổi phải cập nhật component contract và consumer compatibility theo scope; source reorganization không tự cho phép breaking changes.
- Source consumption dùng ProjectReference. Package publication theo `docs/RELEASING.md`; không gán license chung, publish hay archive repo nguồn trong bootstrap.
- Không commit binaries, `bin/`, `obj/`, caches, dumps, raw test results, credentials hoặc local IDE state. Không thêm approval manifests, runner frameworks hay skills ngoài task.
