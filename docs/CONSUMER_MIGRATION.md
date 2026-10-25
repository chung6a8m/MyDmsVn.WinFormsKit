# Đưa WinFormsKit vào consumer và rollback

Đây là recipe cho thay đổi consumer riêng sau bootstrap. Cutover từ exact checkout app `6c385d7630b6b21e915e244113c214aa616327f6` đã được thực hiện trên branch `codex/a3-consumer-from-6c385d7` tại `20c6a6ed58363736f6d7542cf2515af3a1bbd686`; [Plan A3](plans/20261024-001-a3-winformskit.md) ghi trạng thái và [verification report](verification/20261024-a3-bootstrap.md) ghi kết quả. [Điều kiện A3 trong Docs][a3] yêu cầu graph mới, native app checks phù hợp, provenance và đường quay về exact pins cũ.

## Baseline để đối chiếu

Cutover này xuất phát trực tiếp từ [MyDmsVn `v1a-pro-ui-next` tại `6c385d7`][app]. Bảng dưới là pins **thực tế** cần dùng khi rollback commit `20c6a6e`.

| Dependency hiện hữu tại baseline | Đường dẫn trong app | Exact pin |
|---|---|---|
| BootstrapSourceGrid | `vendor/MyDmsVn.BootstrapSourceGrid` | `e311144c27cd79a6c34aa0308ad029651c9e03c9` |
| Bootstrap5WinFormUI | `vendor/MyDmsVn.BootstrapSourceGrid/vendor/Bootstrap5WinFormUI` | `8ae217ea8aa9a938ebd558f7071dc737f909e6a5` |
| SourceGrid | `vendor/MyDmsVn.BootstrapSourceGrid/vendor/sourcegrid` | `f4e457b43582bf01892f50bdc74aa480531e5944` |

Sources: [app `.gitmodules`][app-modules], [BSG tree/gitlinks][grid-tree] và [source provenance](PROVENANCE.md). Migration khác từ app `8cfa253` tại `0f4e273` có rollback pins `09eab7a`/`aba102e`; chúng không áp dụng cho commit `20c6a6e`. Giữ exact pins trong PR migration để rollback không phụ thuộc upstream HEAD.

## 1. Chọn candidate đã review

1. Chọn một **exact commit WinFormsKit** có provenance và verification phù hợp; không pin tên branch.
2. Làm trên branch/worktree sạch của app; lưu thay đổi người dùng theo workflow của repo, không ghi đè.
3. Ghi app base, toolkit candidate, engine pin, SDK, target và test scope trong một PR migration hẹp.
4. Giữ `net48`, production composition, database schema và package/feed configuration của app. Không đồng thời nâng dependencies hoặc chuyển sang package distribution.

Khởi tạo dependency mới bên cạnh dependency cũ để sửa graph có kiểm soát. Biến bên dưới phải được thay bằng SHA thật đã chọn; không chạy nguyên placeholder:

```powershell
$toolkitRevision = 'REPLACE_WITH_VERIFIED_40_CHARACTER_TOOLKIT_COMMIT'
git submodule add https://github.com/chung6a8m/MyDmsVn.WinFormsKit.git vendor/MyDmsVn.WinFormsKit
git -C vendor/MyDmsVn.WinFormsKit checkout --detach $toolkitRevision
git submodule update --init --recursive
```

## 2. Đổi graph source

Tại baseline, [Desktop.WinForms project][app-project] tham chiếu adapter cũ và [solution sản phẩm][app-solution] liệt kê cả ba dependency projects. Đổi mapping sau, giữ project GUIDs/public identities nếu không có lý do khác:

| Thành phần | Project path mới, tính từ app root |
|---|---|
| Adapter | `vendor/MyDmsVn.WinFormsKit/src/MyDmsVn.BootstrapSourceGrid/MyDmsVn.BootstrapSourceGrid.csproj` |
| UI core | `vendor/MyDmsVn.WinFormsKit/src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj` |
| Engine | `vendor/MyDmsVn.WinFormsKit/vendor/sourcegrid/SourceGrid/SourceGrid.csproj` |

Reference trong `src/MyDmsVn.Desktop.WinForms/MyDmsVn.Desktop.WinForms.csproj` trở thành:

```xml
<ProjectReference Include="..\..\vendor\MyDmsVn.WinFormsKit\src\MyDmsVn.BootstrapSourceGrid\MyDmsVn.BootstrapSourceGrid.csproj" />
```

Review toàn bộ `.sln`, `.csproj`, `.props`, `.targets`, scripts đóng gói và test helpers đang tìm đường dẫn vendor. App base `6c385d7` có `samples/MyDmsVn.Desktop.UiLab`; production solution chỉ đổi ba dependency project paths. Không reference cả old/new copy hoặc ProjectReference/PackageReference của cùng assembly.

Khi không còn reference thực thi tới graph cũ, kiểm vendor cũ sạch rồi loại gitlink cũ và entry tương ứng trong `.gitmodules` bằng workflow submodule của app. Đừng xóa mọi chuỗi path cũ trong historical evidence: các baseline đó vẫn đúng tại commit được dẫn. Không đưa galleries/test projects của toolkit vào production executable/package.

## 3. Kiểm chứng trước khi gọi cutover hoàn tất

- Từ checkout sạch, recursive submodules tái tạo được toolkit candidate và engine pin; app restore/build `net48` từ graph mới, không dựa DLL/cache cũ.
- Chạy controls/adapter owners phù hợp theo [TESTING](TESTING.md); giữ cả hai toolkit targets khi source/shared build graph bị ảnh hưởng.
- Dùng commands/filter thật trong app test policy; kiểm DateInput, decimal/lookup/editor commit-cancel, caret/focus, theme và ownership/disposal tại các seams app đang dùng. Discovery phải có cases thực.
- Chạy native app/UiLab checks đúng Windows/STA/interactive prerequisites. Ghi configuration, testhost/application bitness và phạm vi physical DPI; toolkit suite không thay native app proof.
- Nếu release graph đổi, kiểm package/output theo policy app để không chứa gallery/test fixtures hoặc duplicate assemblies. SQL execution chỉ mở rộng khi diff chạm boundary cần SQL, không tự chạy mọi owner vì đổi vendor path.
- Ghi actual pass/fail/skip/not-run cùng old/new pins. Giới hạn acceptance lịch sử của app vẫn giữ nguyên; migration source không tự đóng chúng.

Giữ migration trong một PR/commit set có thể revert độc lập. Không archive repository UI/BSG sau khi mới build toolkit; việc đó chỉ được xem xét khi consumer và rollback có evidence, theo [source/distribution proposal][distribution].

## 4. Rollback về graph cũ

Rollback là revert **consumer migration commit(s)** trong app, rồi khởi tạo lại submodules theo gitlinks được phục hồi. Thực hiện trên worktree sạch; không dùng hard reset/force để bỏ thay đổi khác. Với một commit migration thường:

```powershell
git revert COMMIT_CONSUMER_MIGRATION
git submodule sync --recursive
git submodule update --init --recursive
git submodule status --recursive
```

`COMMIT_CONSUMER_MIGRATION` là commit thật đã ghi trong PR, không phải toolkit commit. Nếu PR có nhiều commits hoặc merge commit, chọn cách revert tương ứng với lịch sử thực và review diff; không đoán mainline.

Sau revert, kiểm lại:

```powershell
git -C vendor/MyDmsVn.BootstrapSourceGrid rev-parse HEAD
git -C vendor/MyDmsVn.BootstrapSourceGrid/vendor/Bootstrap5WinFormUI rev-parse HEAD
git -C vendor/MyDmsVn.BootstrapSourceGrid/vendor/sourcegrid rev-parse HEAD
```

Ba outputs phải khớp bảng old pins, hoặc baseline được ghi riêng nếu app đã đổi trước migration. Project/solution references quay về graph cũ; không còn toolkit assembly copy trong output. Build và chạy native smoke phù hợp lại trên reverted candidate. Revert code tự nó chưa là runtime acceptance.

Migration này không thay schema hay dữ liệu nên rollback không cần khôi phục database. Nếu PR thực tế thêm thay đổi database ngoài recipe này, phải tách phạm vi và thiết kế recovery theo owner app; không tuyên bố revert source đã rollback dữ liệu.

[app]: https://github.com/chung6a8m/MyDmsVn/tree/6c385d7630b6b21e915e244113c214aa616327f6
[app-modules]: https://github.com/chung6a8m/MyDmsVn/blob/6c385d7630b6b21e915e244113c214aa616327f6/.gitmodules
[app-project]: https://github.com/chung6a8m/MyDmsVn/blob/6c385d7630b6b21e915e244113c214aa616327f6/src/MyDmsVn.Desktop.WinForms/MyDmsVn.Desktop.WinForms.csproj
[app-solution]: https://github.com/chung6a8m/MyDmsVn/blob/6c385d7630b6b21e915e244113c214aa616327f6/MyDmsVn.sln
[grid-tree]: https://github.com/chung6a8m/MyDmsVn.BootstrapSourceGrid/tree/e311144c27cd79a6c34aa0308ad029651c9e03c9/vendor
[a3]: https://github.com/chung6a8m/MyDmsVn.Docs/blob/57cf5e0f506ac0c313e9d659939d863325250b53/proposals/2026-10-24-adoption.md#2-thứ-tự-đề-xuất-và-kết-quả-có-thể-xem-được
[distribution]: https://github.com/chung6a8m/MyDmsVn.Docs/blob/57cf5e0f506ac0c313e9d659939d863325250b53/proposals/2026-10-24-organization.md#chuyển-source-và-chuyển-distribution-là-hai-việc-riêng
