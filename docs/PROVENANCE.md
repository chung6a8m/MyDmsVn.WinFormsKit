# Nguồn source và phạm vi import

Tài liệu này ghi nguồn của lần khởi tạo ngày **24/10/2026**. Đây là mapping lịch sử bất biến; revision SourceGrid đang được tiêu thụ luôn lấy từ gitlink `vendor/sourcegrid`, không lấy từ một đoạn mô tả cũ. Dữ liệu máy đọc tương ứng nằm trong [provenance.json](../provenance.json).

## Căn cứ

Yêu cầu A3 được đọc từ [MyDmsVn.Docs tại `57cf5e0`](https://github.com/chung6a8m/MyDmsVn.Docs/tree/57cf5e0f506ac0c313e9d659939d863325250b53), đặc biệt [tổ chức toolkit](https://github.com/chung6a8m/MyDmsVn.Docs/blob/57cf5e0f506ac0c313e9d659939d863325250b53/proposals/2026-10-24-organization.md#4-gom-toolkit-giữ-component-boundaries) và [điều kiện A3](https://github.com/chung6a8m/MyDmsVn.Docs/blob/57cf5e0f506ac0c313e9d659939d863325250b53/proposals/2026-10-24-adoption.md#2-thứ-tự-đề-xuất-và-kết-quả-có-thể-xem-được).

Đã đối chiếu nhánh sản phẩm `v1a-pro-ui-next` tại [MyDmsVn `6c385d7`](https://github.com/chung6a8m/MyDmsVn/tree/6c385d7630b6b21e915e244113c214aa616327f6). App đã tiến qua baseline khảo sát `03acacd`; ba gitlinks vẫn đúng như bảng dưới. Các merge commits ở upstream `main` không được dùng thay consumed pins.

| Nguồn | Commit import | Tree tại commit |
|---|---|---|
| [Bootstrap5WinFormUI][ui] | `8ae217ea8aa9a938ebd558f7071dc737f909e6a5` | `2e8e090c159fa18752eeb1a1402d3ed08e61ba1c` |
| [BootstrapSourceGrid][grid] | `e311144c27cd79a6c34aa0308ad029651c9e03c9` | `2b0076dde79a3d5b7bff89171fa0b37a854d48de` |
| [SourceGrid][engine] | `f4e457b43582bf01892f50bdc74aa480531e5944` | `c3e8972c148a1b426fa9668b1fa64b55928fa13e` |

## Mapping source

| Repository nguồn | Đường dẫn gốc | Đường dẫn trong WinFormsKit |
|---|---|---|
| UI | `src/MyDmsVn.Bootstrap5WinFormUI` | [src/MyDmsVn.Bootstrap5WinFormUI](../src/MyDmsVn.Bootstrap5WinFormUI) |
| UI | `demo/MyDmsVn.Bootstrap5WinFormUI.Demo` | [samples/ControlGallery](../samples/ControlGallery) |
| UI | `tests/MyDmsVn.Bootstrap5WinFormUI.Tests` | [tests/MyDmsVn.Bootstrap5WinFormUI.Tests](../tests/MyDmsVn.Bootstrap5WinFormUI.Tests) |
| UI | `tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost` | [tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost](../tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost) |
| BSG | `src/MyDmsVn.BootstrapSourceGrid` | [src/MyDmsVn.BootstrapSourceGrid](../src/MyDmsVn.BootstrapSourceGrid) |
| BSG | `samples/MyDmsVn.BootstrapSourceGrid.Demo` | [samples/GridGallery](../samples/GridGallery) |
| BSG | `tests/MyDmsVn.BootstrapSourceGrid.Tests` | [tests/MyDmsVn.BootstrapSourceGrid.Tests](../tests/MyDmsVn.BootstrapSourceGrid.Tests) |
| SourceGrid | Toàn bộ repository | `vendor/sourcegrid`, một Git submodule |

Đây là import có mapping, không merge toàn bộ lịch sử Git. Source history trước bootstrap vẫn truy cập qua các permalinks trên. Không xóa hoặc archive repository gốc.

## Adaptation có chủ đích

Toàn bộ C# implementation, C# tests, helper host và `app.manifest` được giữ từ nguồn; không thay thuật toán, public API, assembly version hoặc assertions để gom repository. Chỉ bốn project files cần sửa đường dẫn/metadata:

1. UI core: URL repository/project chuyển sang WinFormsKit; package readme trỏ `docs/components/controls/PACKAGE_README.md`.
2. BSG core: UI `ProjectReference` chuyển từ nested vendor sang project anh em `../MyDmsVn.Bootstrap5WinFormUI`; engine vẫn ở `../../vendor/sourcegrid/SourceGrid/SourceGrid.csproj`. URL repository/project và đường readme chuyển sang WinFormsKit/grid docs.
3. UI tests: demo reference chuyển tới `samples/ControlGallery`; giữ VisualStyleHost reference và executable identity.
4. BSG tests: demo reference chuyển tới `samples/GridGallery`.

Giữ csproj basenames của galleries để không đổi assembly names hoặc phá `InternalsVisibleTo`. UI core tiếp tục mang namespace `MyDmsVn.Bootstrap5WinFormUI`; BSG assembly/package là `MyDmsVn.BootstrapSourceGrid`, nhưng public control vẫn là `MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSourceGrid`.

Root `Directory.Build.props`, `.editorconfig`, `.gitattributes` lấy nền từ UI. Root props bổ sung NuGet lockfiles cho bảy project sở hữu bởi toolkit. SourceGrid giữ nguyên props/package policy trong submodule; không ghi file mới vào vendor. `global.json` chọn SDK `8.0.400` với `latestFeature` để cùng baseline tooling của consumer; UI upstream chọn `8.0.100`. Đây là thay đổi SDK floor, không đổi hai target frameworks. Không nâng dependency package versions. Root `Directory.Build.targets` cố định build-only `Microsoft.NETFramework.ReferenceAssemblies` `1.0.3` trên `net48` (dùng `VersionOverride` cho project CPM SourceGrid), cùng phiên bản SDK đã restore ngầm trên Linux. Việc khai báo rõ này tránh lockfile khác nhau khi Windows có targeting pack cài sẵn; không đổi engine source hoặc runtime dependency. Các build dùng locked restore; thay đổi package có chủ đích phải regenerate và review locks. Root targets cũng khai báo restore RIDs `win7-x86;win7-x64` cho executable `net48` để lockfiles giống nhau giữa hosts; SDK vẫn chọn RID/bitness thực trên Windows theo cách cũ. Không ép `RuntimeIdentifier` của app hoặc đổi các TFM.

## Tài liệu và notices

[Component references](components/README.md) giữ 14 tài liệu hợp đồng/API/compatibility của upstream. Ngoại trừ package readmes, các bản nhập có header ghi provenance và dẫn tới commands/status hiện hành. Bốn links BSG (ba links lịch sử và một homepage) được đổi thành exact upstream permalinks. Không nhập roadmap, archive, tiến độ hoặc acceptance lịch sử thành nhiệm vụ hiện hành.

Root README, hướng dẫn contributor, kiến trúc, testing, migration và plan A3 là tài liệu mới của WinFormsKit. [THIRD_PARTY_NOTICES](../THIRD_PARTY_NOTICES.md) chỉ rõ phạm vi từng notice. Chưa có lựa chọn license bao phủ toàn bộ toolkit và chưa có public NuGet release mới.

## Cách đối chiếu lại

```powershell
git submodule status
git -C vendor/sourcegrid rev-parse HEAD
git -C vendor/sourcegrid status --short
```

Khi kiểm import, checkout các source commits cố định ở thư mục riêng, ánh xạ theo bảng trên và so Git blob/text đã chuẩn hóa EOL. Đối với bốn csproj, review riêng đúng những adaptation đã liệt kê. Không tạo test vĩnh viễn đóng băng toàn bộ danh sách source của một đợt migration; các bộ test behavior và API baseline gốc vẫn là owner cho thay đổi tiếp theo.

Kết quả thực của candidate bootstrap nằm tại [verification](verification/20261024-a3-bootstrap.md); provenance không thay thế build hoặc native acceptance.

[ui]: https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI/tree/8ae217ea8aa9a938ebd558f7071dc737f909e6a5
[grid]: https://github.com/chung6a8m/MyDmsVn.BootstrapSourceGrid/tree/e311144c27cd79a6c34aa0308ad029651c9e03c9
[engine]: https://github.com/chung6a8m/sourcegrid/tree/f4e457b43582bf01892f50bdc74aa480531e5944
