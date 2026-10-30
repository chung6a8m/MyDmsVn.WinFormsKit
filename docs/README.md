# Tài liệu WinFormsKit

Tài liệu tại đây đi cùng source toolkit. Scope UI Composition Recipes được chọn ngày 2026-10-30 có hai owner: plan toolkit sở hữu CMP-T01/CMP-T02, còn [plan MyDmsVn](https://github.com/chung6a8m/MyDmsVn/blob/main/docs/plans/20261030-001-business-compact-composition-adoption.md) sở hữu CMP-T03–CMP-T05 và phối hợp skill/mapping/pilot. Không cần repository tài liệu thứ ba. Các [đề xuất ownership A3][ownership] từ MyDmsVn.Docs bên dưới là nguồn lịch sử; source contracts, commands và candidate evidence hiện hành thuộc đúng repo thực thi.

## Điểm vào

| Cần biết | Tài liệu |
|---|---|
| Thành phần, public identities, dependency direction | [ARCHITECTURE](ARCHITECTURE.md) |
| Build, test owners, Windows/native prerequisites | [TESTING](TESTING.md) |
| Source imported từ đâu, đã chỉnh gì | [PROVENANCE](PROVENANCE.md), [machine-readable mapping](../provenance.json) |
| Notices/license theo component | [THIRD_PARTY_NOTICES](../THIRD_PARTY_NOTICES.md) |
| Consumer migration và rollback | [CONSUMER_MIGRATION](CONSUMER_MIGRATION.md) |
| Điều kiện package/distribution | [RELEASING](RELEASING.md) |
| Công việc mới được chọn ngày 2026-10-30 | [ROADMAP](ROADMAP.md) → [plan UI Composition Recipes](plans/20261030-001-ui-composition-recipes.md) |
| Skill, Business Compact mapping, pilot và phối hợp hai repo | [Plan app](https://github.com/chung6a8m/MyDmsVn/blob/main/docs/plans/20261030-001-business-compact-composition-adoption.md) |
| Scope TAB/BUS trước đó, giữ owner trạng thái và nghiệm thu riêng | [Plan TAB](plans/20261029-002-bootstrap-tabs-hottrack.md), [plan BUS](plans/20261029-003-live-busy-overlay.md) |
| Contract/API, native paint, HotTrack và phương án 3B live busy overlay | [Thiết kế tabs/HotTrack/live overlay](design/20261029-tabs-hottrack-live-busy-overlay.md) |
| Scope input/popup trước đó, cùng lịch sử và điều kiện nghiệm thu riêng | [Plan input/popup](plans/20261029-001-input-popup-rendering.md), [thiết kế input/popup rendering](design/20261029-input-popup-rendering.md) |
| Scope ba ngoại lệ Business Compact đã hoàn tất | [Plan ba ngoại lệ](plans/20261028-001-business-compact-exceptions.md) |
| Kết quả source/build, Windows/native PASS và Owner APPROVED/CLOSED của scope ba ngoại lệ | [Candidate evidence](verification/20261028-business-compact-exceptions.md) |
| Trạng thái bootstrap và điều kiện A3 còn lại | [Plan A3](plans/20261024-001-a3-winformskit.md) |
| Kết quả thực tế của bootstrap | [Verification](verification/20261024-a3-bootstrap.md) |

## Component references

| Vùng | Contracts được giữ theo owner |
|---|---|
| [Controls](components/controls/) | Public API, design system, controls, compatibility, native execution conventions |
| [Grid](components/grid/) | Editor integration, SourceGrid seams, compatibility và known limitations |

References được curate từ exact source pins trong provenance. Nếu một trang dẫn tới evidence/decision upstream, baseline ở link đó là lịch sử; kết quả không áp tự động cho candidate WinFormsKit. Commands hiện hành đọc tại [TESTING](TESTING.md); trạng thái thực thi đọc tại plan, không lấy từ roadmap import.

## Nguồn thiết kế

Các nguồn sau là lịch sử thiết kế/bootstrap A3; không phải dependency phê duyệt hay execution index của scope composition mới.

- [A3: gom toolkit, giữ component boundaries][design].
- [Điều kiện kết thúc A3 và rollback][adoption].
- [Native evidence và giới hạn prototype][native].

Mọi links nguồn thiết kế trên được pin tại `MyDmsVn.Docs@57cf5e0f506ac0c313e9d659939d863325250b53`. Yêu cầu bootstrap hiện tại được ghi trong plan; tài liệu đề xuất cũ không tự mở thêm scope runtime, API hoặc migration sản phẩm.

[ownership]: https://github.com/chung6a8m/MyDmsVn.Docs/blob/57cf5e0f506ac0c313e9d659939d863325250b53/proposals/2026-10-24-docs-tests-skills.md#2-một-chủ-sở-hữu-cho-mỗi-loại-tài-liệu
[design]: https://github.com/chung6a8m/MyDmsVn.Docs/blob/57cf5e0f506ac0c313e9d659939d863325250b53/proposals/2026-10-24-organization.md#4-gom-toolkit-giữ-component-boundaries
[adoption]: https://github.com/chung6a8m/MyDmsVn.Docs/blob/57cf5e0f506ac0c313e9d659939d863325250b53/proposals/2026-10-24-adoption.md#2-thứ-tự-đề-xuất-và-kết-quả-có-thể-xem-được
[native]: https://github.com/chung6a8m/MyDmsVn.Docs/blob/57cf5e0f506ac0c313e9d659939d863325250b53/proposals/2026-10-24-ui-prototyping.md#7-kiểm-chứng-theo-đúng-loại-bằng-chứng
