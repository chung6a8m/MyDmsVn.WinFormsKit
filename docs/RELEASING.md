# Source consumption và điều kiện phát hành

WinFormsKit bắt đầu bằng **source + ProjectReference**. Bootstrap không tạo release workflow, publish package, chọn feed mới hoặc áp license chung. Trạng thái thực thi nằm trong [plan A3](plans/20261024-001-a3-winformskit.md); [THIRD_PARTY_NOTICES](../THIRD_PARTY_NOTICES.md) sở hữu notices/license theo component.

## Phân biệt source bootstrap với package release

[BSG release policy tại pin import][upstream-release] ghi public publication bị chặn bởi các điều kiện dependency/package-source equivalence và license/notices của UI. Các nhận định feed trong tài liệu đó là evidence tại thời điểm upstream đã ghi, **không phải kết quả kiểm feed mới của bootstrap**. Gom source vào cùng repo không tự đóng các điều kiện ấy.

Hai identities tiếp tục là `MyDmsVn.Bootstrap5WinFormUI` và `MyDmsVn.BootstrapSourceGrid`; version metadata hiện hữu được giữ để bảo toàn source baseline. Không lấy việc `dotnet pack` tạo được file làm bằng chứng package có thể phát hành. Nếu cần pack để inspection, phải đánh dấu artifacts chỉ dùng inspection và ghi source/candidate tương ứng.

## Các điều kiện cần hoàn tất trước package publication

| Điều kiện | Evidence cần có |
|---|---|
| Quyền phân phối | License/notices của UI, BSG và SourceGrid được xác minh theo source được phân phối; không suy quyền của component khác từ MIT của BSG |
| Package boundaries | Hai assembly/package identities rõ; SourceGrid được restore như exact dependency hoặc phân phối theo phương án có provenance và quyền tương ứng |
| Source equivalence | Exact package versions/feeds ánh xạ đến source commits, public API và behavior đã kiểm; có transitive dependency review |
| Dependency resolution | Clean consumer restore không dựa source checkout/cache; dependency groups, source mapping và lockfiles phù hợp cả `net48` và `net8.0-windows` |
| Validation | Release build/tests cả hai targets; native editor/theme/DPI/Designer scope được ghi theo candidate; các ô chưa chạy vẫn mở |
| Package contents | `.nupkg`/`.snupkg`, TFMs, symbols, README, repository metadata, version, license/notices đúng; không có gallery/tests hoặc duplicate engine/UI binaries ngoài phương án đã chọn |
| Release operation | Version/release notes/tag/workflow và quyền publish được giao rõ; secrets thuộc hệ thống release, không vào Git |

Không đồng thời reference project và package copies của cùng assembly. Không copy DLL/source vendor vào package chỉ để vượt dependency gate. Giữ source consumption khi chưa có bằng chứng đầy đủ, theo [đề xuất A3][a3-distribution].

## Source consumer trước package

Một consumer có thể pin exact toolkit revision và build bằng ProjectReference theo [CONSUMER_MIGRATION](CONSUMER_MIGRATION.md). Consumer vẫn cần build/native compatibility evidence và giữ đường quay lại old pins. Việc này không tạo chứng nhận cho package/public feed hoặc toàn bộ release sản phẩm.

Sau khi package publication được giao và mọi điều kiện đạt, kiểm clean consumers của cả hai targets từ **feed thật**, chạy runtime smoke, đối chiếu hashes/metadata với artifacts đã review và lưu evidence theo version. Chỉ sau khi consumer mới, lịch sử và rollback đã được chứng minh mới xem xét chuyển repo cũ read-only/archive; không xóa repos nguồn trong migration đầu.

## Chủ sở hữu thông tin

- [PROVENANCE](PROVENANCE.md): exact sources, mapping import và adaptations.
- [THIRD_PARTY_NOTICES](../THIRD_PARTY_NOTICES.md): notices/license inventory.
- [TESTING](TESTING.md): commands hiện hành và native prerequisites.
- [Plan A3](plans/20261024-001-a3-winformskit.md): execution status; các release decisions tương lai chỉ được thêm khi thực sự được giao.

[upstream-release]: https://github.com/chung6a8m/MyDmsVn.BootstrapSourceGrid/blob/e311144c27cd79a6c34aa0308ad029651c9e03c9/docs/RELEASE.md
[a3-distribution]: https://github.com/chung6a8m/MyDmsVn.Docs/blob/57cf5e0f506ac0c313e9d659939d863325250b53/proposals/2026-10-24-organization.md#chuyển-source-và-chuyển-distribution-là-hai-việc-riêng
