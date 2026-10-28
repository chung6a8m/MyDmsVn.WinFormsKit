# Khắc phục ba ngoại lệ Business Compact — 2026-10-28

**Scope được Owner chọn:** action disabled trong Dark, Font của modal khi đổi theme và capability multiline read-only. **Trạng thái:** source đã triển khai; Windows focused native tests PASS trên cả `net48` và `net8.0-windows`; review consumer ở physical 100% đã PASS, physical 125% còn BLOCKED do chưa đổi được Display scale; Owner visual approval PENDING.

Baseline `efe2e7212fd36e32c91b914bdc5e4081947124fc` có cùng tree với consumer pin `c232a4c09dc0386a5bd8bb484a5c991acae827d5`. Giữ SourceGrid `f4e457b43582bf01892f50bdc74aa480531e5944`, assembly/public identities và targets net48;net8.0-windows.

## Thiết kế và tasks

- [x] EXC-K01: palette disabled dùng SurfaceSecondary/Text/Disabled, giữ bất hoạt và phân biệt Secondary outline; test tỷ lệ đọc được và trạng thái. Màu hiện tại có contrast số học tốt; thay đổi này cần native visual review riêng.
- [x] EXC-K02: tái sử dụng Font title khi typography không đổi; chỉ thay/dispose Font do toolkit sở hữu khi token đổi. Test theme-switch lặp, đổi typography, measurement/paint và disposal.
- [x] EXC-K03: mở rộng BootstrapTextBox bằng đúng ba public native-forwarded properties `Multiline`, `WordWrap`, `ScrollBars`; giữ defaults single-line, editor identity và accessibility metadata. Test layout/selection/read-only, accessories/validation và adapter compatibility.
- [x] EXC-K04a: cập nhật component contract, cross-build cả hai targets và review compiled API delta. Cả hai build target PASS với 0 warning/0 error; API thêm đúng ba property, không xóa hoặc đổi signature hiện có.
- [ ] EXC-K04b: focused native tests ở Windows đã PASS và có discovery/TRX; visual evidence consumer mới hoàn tất physical 100%, còn physical 125% và quyết định Owner PENDING. Giữ task mở cho đến khi hai mục đó được ghi trên đúng candidate.

Generic fixes thuộc toolkit; composition, secret/close/session/recovery thuộc MyDmsVn. Consumer sẽ pin commit chính xác và dùng BodyPanel/FooterPanel của BootstrapModal. Không đổi ordinary Close MessageBox, SQL hoặc release. Không ghi historical acceptance thành PASS của candidate mới.

## Kiểm chứng

Follow [TESTING](../TESTING.md). Test classes/filters phải có source và discovery thật. Bằng chứng Windows cho candidate test `a71d64bd82eb40123eafef4c5a6832e53d7ef9ca` nằm trong [verification report](../verification/20261028-business-compact-exceptions.md): controls 75/75 trên từng TFM và adapter 40/40 trên từng TFM, không skip. Chưa có Owner approval cho appearance mới; physical 125% không được thay bằng giả lập DPI.
