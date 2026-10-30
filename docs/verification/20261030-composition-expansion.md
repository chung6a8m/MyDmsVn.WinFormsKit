# Evidence — mở rộng UI compositions

Ngày mở record: **2026-10-30**. [Plan](../plans/20261030-002-editable-lines-master-detail-recipes.md) sở hữu trạng thái task; [design](../design/20261030-editable-lines-master-detail-compositions.md) sở hữu contract. Đây là placeholder có chủ đích cho roadmap đã kích hoạt bằng **docs-only**. Không có source/test implementation hoặc native execution của UIC-03/UIC-04 trong đợt kích hoạt.

## Baseline và phương pháp hiện có

- Toolkit source đã đọc: `25b6c77a4d95d0df112aeba389d61a58ad0ee6db`.
- Đã kiểm source/docs: existing lookup/decimal adapters, SourceGrid CheckBox constructor/controller tại engine pin, registry ownership contracts, GridGallery/ControlGallery routes và project references của hai test owners.
- Implementation candidate: **chưa có**. Activation/docs commit sẽ do commit chứa record xác định; không dùng baseline làm candidate mới.
- Build/tests/native/Owner visual review của scope mới: **NOT RUN**; chưa có kết quả để nghiệm thu.
- Runtime pin và compatibility của MyDmsVn: do CEX-A01 trong [plan app](https://github.com/chung6a8m/MyDmsVn/blob/main/docs/plans/20261030-002-editable-lines-sal-adoption.md) đối chiếu; record này không nâng pin.

## Checkpoints riêng

| Recipe | Source implementation | Build / discovery / tests | Native matrix | Owner decision | Handoff |
|---|---|---|---|---|---|
| UIC-03 Editable Lines | NOT IMPLEMENTED | NOT RUN | NOT RUN | PENDING | PENDING → CEX-A02 |
| UIC-04 Master–Detail | NOT IMPLEMENTED | NOT RUN | NOT RUN | PENDING | PENDING → CEX-A03 |

UIC-03 có thể bàn giao trước UIC-04. SAL mapping/preparation được dùng K01 source/results đã freeze trước final K03; completion A02 đối chiếu UIC-03 handoff/limits. Không yêu cầu toàn bộ CEX-K03 COMPLETE hoặc UIC-04 READY. Gate toolkit không cấp consumer acceptance cho SAL/Security.

## Mẫu record cho mỗi candidate/recipe

Khi thực thi, thêm section mới theo recipe ID + exact candidate SHA; giữ lịch sử cũ. Không điền guessed values vào bảng dưới.

| Trường | Dữ liệu phải ghi khi thực thi |
|---|---|
| Source / engine | Toolkit SHA, SourceGrid gitlink, clean/dirty state và diff đang kiểm |
| Environment | Windows/SDK/runtime/configuration/TFM/process bitness; physical monitor scale, managed/native window DPI |
| Build | Exact command, exit code và kết quả; clean/warm restore |
| Discovery | Exact project/filter/TFM, discovered count, tên case mới xác nhận nonempty |
| Tests | Executed/pass/fail/skip, skip reasons, timeout/cleanup; raw logs ngoài Git |
| Native | Gallery route, normal/small content size, theme, input sequence, expected/observed behavior |
| Artifacts | Candidate/scenario/hash/URL/retention của captures/logs; URL mở lại được |
| Decision | Owner, thời điểm, candidate được xem, accepted/rejected/remaining limitations |
| Handoff | Recipe source SHA, evidence revision/link; app runtime SHA do A01 xác minh; next task/owner |

## Native matrix cần thực hiện

Mỗi ô recipe bên dưới hiện **NOT RUN**; matrix áp riêng cho `net48` và `net8.0-windows`, tổng 8 điều kiện theme/scale/viewport mỗi TFM mỗi recipe.

| Theme | Physical scale | Viewport | UIC-03 | UIC-04 |
|---|---|---|---|---|
| Light | 100% | normal | NOT RUN | NOT RUN |
| Light | 100% | small | NOT RUN | NOT RUN |
| Dark | 100% | normal | NOT RUN | NOT RUN |
| Dark | 100% | small | NOT RUN | NOT RUN |
| Light | 125% | normal | NOT RUN | NOT RUN |
| Light | 125% | small | NOT RUN | NOT RUN |
| Dark | 125% | normal | NOT RUN | NOT RUN |
| Dark | 125% | small | NOT RUN | NOT RUN |

Normal logical content target 1000×650; small UIC-03 640×420, UIC-04 480×430. Ghi actual size/DPI khi chạy, không giả định target bằng native window measurement. Thiếu màn hình/desktop thì ghi BLOCKED BY ENVIRONMENT cho đúng ô. Chưa chọn representative150/200 hoặc mixed-DPI làm gate của scope này; không mượn ngoại lệ TAB để tuyên bố đã chạy.

UIC-03 observations: native per-key decimal/lookup và mixed checkbox Space/click, Enter/Escape/Tab/Shift+Tab popup open/closed; invalid Apply/Remove/chuyển checkbox không mất candidate; ReferenceId 0 không Apply, Restore về valid key; shared reuse; Restore cả snapshot/IsMarked; STT hiển thị, identity behavior sau remove, locked/derived; theme/resize/disposal. UIC-04 observations: selected key/caption, filter/refresh/missing key, Loading/Empty/Unavailable, long Vietnamese text, small layout, native search/focus, current accessible feedback và route disposal.

## Giới hạn evidence

Đọc source chỉ chứng minh capability/contract hiện có và feasibility của kế hoạch. Không chứng minh candidate tương lai compile hoặc GUI đúng. Existing tests là source references, không phải executed PASS mới. `1420f25`/`bdac29b`, TAB/BUS và các record trước giữ scope riêng. Screenshot không tự chứng minh caret/IME/full accessibility/AT. Ghi loại quan sát thực tế, không gọi source inspection hoặc bitmap capture là native acceptance.
