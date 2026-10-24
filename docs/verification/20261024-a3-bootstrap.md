# Kiểm chứng candidate bootstrap A3

**Phạm vi:** lần khởi tạo repository theo yêu cầu ngày 24/10/2026. Evidence bên dưới được thu trực tiếp trên source sẽ được commit trong đợt bootstrap; không lấy PASS lịch sử của repository nguồn. [Plan A3](../plans/20261024-001-a3-winformskit.md) là owner trạng thái tổng thể.

## Môi trường và baseline

| Mục | Giá trị |
|---|---|
| OS | Ubuntu 24.04, Linux x64 |
| .NET SDK | `8.0.419`, MSBuild `17.11.48` |
| Host runtime | .NET `8.0.25`, x64 |
| Configuration | `Release` |
| Targets build | `net48`; `net8.0-windows` |
| Windows targeting | `EnableWindowsTargeting=true`; build-only .NET Framework reference pack `1.0.3` |
| Package source | `https://api.nuget.org/v3/index.json` |
| Cache | Lần restore đầu dùng thư mục NuGet packages mới của phiên làm việc; các lần locked restore/build sau dùng lại cache đó |
| Source pins | UI `8ae217ea8aa9a938ebd558f7071dc737f909e6a5`; BSG `e311144c27cd79a6c34aa0308ad029651c9e03c9`; engine `f4e457b43582bf01892f50bdc74aa480531e5944` |

Root SDK floor là `8.0.400` với `latestFeature`; `8.0.419` là SDK thực sự được dùng. `--no-cache` ở một restore command không tự chứng minh global package cache trống; bảng trên phân biệt rõ các lần chạy.

## Kết quả đã quan sát

| Kiểm tra | Kết quả | Giới hạn |
|---|---|---|
| So source theo mapping với exact upstream pins | **PASS**: 654 files nhập; 650 files giữ nguyên sau chuẩn hóa EOL, gồm toàn bộ 646 files C#; chỉ bốn csproj có adaptation đã ghi | Kiểm bảo toàn source, không phải runtime proof |
| Assembly/public names, `InternalsVisibleTo`, helper host/manifest | **PASS** qua đối chiếu source và project metadata | Existing API/native tests chưa được chạy |
| SourceGrid gitlink và vendor worktree | **PASS**: exact pin; `git status --short` trong vendor rỗng | Root build targets cố định reference pack như mô tả trong provenance |
| Solution All và Controls | **PASS**: tương ứng 8 và 4 projects; UI-only graph không kéo adapter/engine | Kiểm graph và compilation |
| Restore graph mới từ package folder ban đầu trống | **PASS** cho cả tám projects | Thực hiện trên Linux |
| Locked restore sau khi cố định net48 reference pack | **PASS**, exit 0; bảy owner lockfiles hiện hành | Chưa chứng minh Windows restore cho đến khi Windows job thực sự chạy |
| Release build All, cả hai targets | **PASS**, exit 0, **0 warnings / 0 errors**, lần sau cùng 17,46 giây | Cross-compilation; không chạy Windows assemblies |
| Locked restore và Release build Controls riêng | **PASS**, exit 0, **0 warnings / 0 errors**, build 1,92 giây | Warm incremental build, không phải benchmark |
| Review độc lập source/build/CI/ownership | **PASS**: không còn finding Critical/Important/Minor trong phần được review | Reviewer không chạy native tests và không chứng nhận consumer cutover |
| Markdown/project/reference QA | **PASS**: 28 Markdown files, 187 links, bảy owner projects; `git diff --cached --check` không có lỗi | Kiểm local links/anchors/paths, không xác nhận website ngoài repo |
| Windows CI | **NOT RUN** tại lúc lập record trước commit | Workflow build đã cấu hình; chỉ update khi có actual run URL/result |
| Hai bộ NUnit trên Windows | **NOT RUN — thiếu Windows runtime/desktop** | Chưa discovery hoặc execute cases; không báo một test PASS hay skip count giả |
| Gallery interaction, caret/focus, DPI, Designer | **NOT RUN** | Cần môi trường Windows phù hợp và quan sát trực tiếp |
| Native app checks / consumer cutover | **NOT RUN — ngoài đợt bootstrap** | App vẫn dùng graph/pins cũ |
| Package publication / package-only consumer | **NOT RUN** | Không phát hành; gates trong RELEASING còn áp dụng |

Hai owning test suites và helper host đã được **compile** cho cả hai targets. Compile test assembly không đồng nghĩa tests đã chạy. Không sửa assertions, bỏ regression hoặc tạo test bookkeeping mới để làm migration trông xanh.

## Commands đã chạy

Commands dưới đây dùng SDK đã nêu, chạy từ root WinFormsKit. SDK/cache được đặt trong workspace kiểm chứng; đó không phải dependency path cần đưa vào repository.

```sh
dotnet --info
dotnet restore MyDmsVn.WinFormsKit.sln -p:EnableWindowsTargeting=true --no-cache --verbosity minimal
dotnet restore MyDmsVn.WinFormsKit.sln -p:EnableWindowsTargeting=true --locked-mode --verbosity minimal
dotnet build MyDmsVn.WinFormsKit.sln -c Release -p:EnableWindowsTargeting=true --no-restore --verbosity minimal
dotnet restore MyDmsVn.WinFormsKit.Controls.sln -p:EnableWindowsTargeting=true --locked-mode --verbosity minimal
dotnet build MyDmsVn.WinFormsKit.Controls.sln -c Release -p:EnableWindowsTargeting=true --no-restore --verbosity minimal
git submodule status
git -C vendor/sourcegrid status --short
```

Sau khi bổ sung `Directory.Build.targets` để net48 reference pack nhất quán giữa các host, đã restore lại, kiểm locked restore và build lại toàn bộ solution; kết quả cuối trong bảng thuộc graph đó. Các build lần đầu cũng đạt nhưng không được dùng thay cho lần sau cùng.

Source parity được kiểm trực tiếp với các checkout exact revisions theo [PROVENANCE](../PROVENANCE.md). Root scripts/build metadata và bốn project deltas được review riêng. Raw logs/build output ở thư mục `artifacts/` bị Git ignore; record này chứa kết quả đã rà soát, không nhúng đường dẫn máy hoặc dữ liệu runtime của sản phẩm.

## Phần nghiệm thu tiếp theo

Trên Windows, thực hiện commands của [TESTING](../TESTING.md), ghi actual discovery/pass/fail/skip theo từng owner/framework, mở hai galleries và kiểm native seams bị ảnh hưởng. Nếu CI build chạy, ghi candidate SHA/run URL và kết quả; CI chỉ build, không thay GUI acceptance.

Sau đó consumer migration được giao riêng theo [CONSUMER_MIGRATION](../CONSUMER_MIGRATION.md). Một source bootstrap có thể dùng để tiếp tục phát triển toolkit, nhưng **A3 toàn phần chưa hoàn tất** khi Windows/native và consumer acceptance còn mở.
