# Khắc phục ba ngoại lệ Business Compact — 2026-10-28

**Scope được Owner chọn:** action disabled trong Dark, Font của modal khi đổi theme và capability multiline read-only. **Trạng thái:** source đã triển khai; cross-build và compiled API review PASS; Windows/native NOT RUN và Owner visual approval PENDING.

Baseline `efe2e7212fd36e32c91b914bdc5e4081947124fc` có cùng tree với consumer pin `c232a4c09dc0386a5bd8bb484a5c991acae827d5`. Giữ SourceGrid `f4e457b43582bf01892f50bdc74aa480531e5944`, assembly/public identities và targets net48;net8.0-windows.

## Thiết kế và tasks

- [x] EXC-K01: palette disabled dùng SurfaceSecondary/Text/Disabled, giữ bất hoạt và phân biệt Secondary outline; test tỷ lệ đọc được và trạng thái. Màu hiện tại có contrast số học tốt; thay đổi này cần native visual review riêng.
- [x] EXC-K02: tái sử dụng Font title khi typography không đổi; chỉ thay/dispose Font do toolkit sở hữu khi token đổi. Test theme-switch lặp, đổi typography, measurement/paint và disposal.
- [x] EXC-K03: mở rộng BootstrapTextBox bằng đúng ba public native-forwarded properties `Multiline`, `WordWrap`, `ScrollBars`; giữ defaults single-line, editor identity và accessibility metadata. Test layout/selection/read-only, accessories/validation và adapter compatibility.
- [x] EXC-K04a: cập nhật component contract, cross-build cả hai targets và review compiled API delta. Cả hai build target PASS với 0 warning/0 error; API thêm đúng ba property, không xóa hoặc đổi signature hiện có.
- [ ] EXC-K04b: chạy focused native tests ở Windows theo TESTING.md, ghi discovery/execution và Owner visual evidence. Hiện NOT RUN/PENDING; các checkbox implementation phía trên không xác nhận native PASS.

Generic fixes thuộc toolkit; composition, secret/close/session/recovery thuộc MyDmsVn. Consumer sẽ pin commit chính xác và dùng BodyPanel/FooterPanel của BootstrapModal. Không đổi ordinary Close MessageBox, SQL hoặc release. Không ghi historical acceptance thành PASS của candidate mới.

## Kiểm chứng

Follow [TESTING](../TESTING.md). Test classes/filters phải có source và discovery thật. Linux cross-build chỉ xác minh compiler/reference graph; thiếu Windows/desktop ghi BLOCKED / NOT RUN. Chưa có Owner approval cho appearance mới.
