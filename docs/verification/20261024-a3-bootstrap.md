# Kiểm chứng candidate bootstrap A3

**Phạm vi:** lần khởi tạo repository theo yêu cầu ngày 24/10/2026. Candidate source hiện tại là [`bdc5c126d4d3caa74ca8ed4518fc565d3a492ba6`](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/commit/bdc5c126d4d3caa74ca8ed4518fc565d3a492ba6), tree `3349567b3ed07379d95fb491684400a3ada54880`, gồm bootstrap và correction cho dependency locks trên Windows. Bootstrap gốc là [`ccba9e7245a5af2db8c9dec0c542c5896f5a36e0`](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/commit/ccba9e7245a5af2db8c9dec0c542c5896f5a36e0), tree `370cf716672cdc304a05c7358765af88850518cb`. Evidence được thu trực tiếp trên các candidates nêu rõ bên dưới; không lấy PASS lịch sử của repository nguồn. [Plan A3](../plans/20261024-001-a3-winformskit.md) là owner trạng thái tổng thể.

## Môi trường và baseline

### Local cross-compilation

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

### Windows CI

[Run `40115483052`](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/actions/runs/40115483052), job `120935767871`, checkout candidate `bdc5c126d4d3caa74ca8ed4518fc565d3a492ba6` cùng SourceGrid exact pin. Runner là **Windows Server 2022**, image `windows-2022` revision `20261012.333.1`, SDK thực tế **8.0.426**. Workflow chạy `./build.ps1 -Configuration Release`, tương đương locked restore và full solution build cho cả hai targets. Kết quả cuối của run là **success**; log build ghi **0 warnings / 0 errors**, 31,17 giây. Bước kiểm dependency locks và SourceGrid worktree cũng đạt. CI chỉ build, không chạy NUnit hoặc GUI acceptance.

## Kết quả đã quan sát

| Kiểm tra | Kết quả | Giới hạn |
|---|---|---|
| So source theo mapping với exact upstream pins | **PASS**: 654 files nhập; 650 files giữ nguyên sau chuẩn hóa EOL, gồm toàn bộ 646 files C#; chỉ bốn csproj có adaptation đã ghi | Kiểm bảo toàn source, không phải runtime proof |
| Assembly/public names, `InternalsVisibleTo`, helper host/manifest | **PASS** qua đối chiếu source và project metadata | Existing API/native tests chưa được chạy |
| SourceGrid gitlink và vendor worktree | **PASS**: exact pin; `git status --short` trong vendor rỗng | Root build targets cố định reference pack như mô tả trong provenance |
| Solution All và Controls | **PASS**: tương ứng 8 và 4 projects; UI-only graph không kéo adapter/engine | Kiểm graph và compilation |
| Restore graph mới từ package folder ban đầu trống | **PASS** cho cả tám projects | Thực hiện trên Linux |
| Locked restore sau reference-pack/RID corrections | **PASS** trên Linux và Windows; bảy owner lockfiles không đổi sau restore | Windows được kiểm trực tiếp tại run của candidate `bdc5c12` |
| Release build All, cả hai targets | **PASS**, exit 0, **0 warnings / 0 errors**: Linux 15,90 giây; Windows 31,17 giây | Candidate `bdc5c12`; đây là compilation, không phải execution tests hoặc benchmark |
| Locked restore và Release build Controls riêng | **PASS**, exit 0, **0 warnings / 0 errors**, build 1,92 giây | Kiểm trên Linux tại bootstrap `ccba9e7`; candidate sau correction đã build lại toàn bộ tám projects |
| Review độc lập source/build/CI/ownership | **PASS**: không còn finding Critical/Important/Minor trong phần được review | Reviewer không chạy native tests và không chứng nhận consumer cutover |
| Recursive clone từ GitHub sau bootstrap | **PASS**: commit/tree `ccba9e7` khớp; SourceGrid tự checkout đúng pin; cả checkout và vendor sạch | Candidate `bdc5c12` còn được checkout đệ quy trực tiếp trong Windows CI; không thay native proof |
| Markdown/project/reference QA | **PASS**: 28 Markdown files, local links/anchors và bảy owner projects; `git diff --cached --check` không có lỗi | Kiểm local links/anchors/paths, không xác nhận website ngoài repo |
| Windows CI của bootstrap `ccba9e7` | **FAIL lịch sử**, `NU1004` do RID của ba executable `net48` | [Run gốc](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/actions/runs/40114978856); đã khắc phục trong candidate hiện tại |
| Windows CI của correction `bdc5c12` | **PASS**: locked restore, full Release build hai targets, locks/vendor không đổi | [Run thành công](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/actions/runs/40115483052); không có test execution |
| Hai bộ NUnit trên Windows | **NOT RUN**: local là Linux; Windows workflow chỉ build | Chưa discovery hoặc execute cases; không báo một test PASS hay skip count giả |
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

Sau các corrections trong `Directory.Build.targets` cho net48 reference pack và executable restore set, đã regenerate locks có chủ đích, kiểm locked restore và build lại toàn bộ solution. Full-build results trong bảng thuộc candidate sau correction.

Windows CI chạy các commands sau từ checkout đệ quy sạch:

```powershell
./build.ps1 -Configuration Release
git diff --exit-code -- '**/packages.lock.json'
git -C vendor/sourcegrid status --porcelain
```

Build script kiểm exit codes của locked restore và build. Workflow thất bại nếu lockfiles thay đổi hoặc SourceGrid worktree bẩn; run nêu trên đạt cả hai checks.

Source parity được kiểm trực tiếp với các checkout exact revisions theo [PROVENANCE](../PROVENANCE.md). Root scripts/build metadata và bốn project deltas được review riêng. Raw logs/build output ở thư mục `artifacts/` bị Git ignore; record này chứa kết quả đã rà soát, không nhúng đường dẫn máy hoặc dữ liệu runtime của sản phẩm.


## Correction sau Windows build đầu tiên

Windows CI dùng SDK `8.0.426` và báo `NU1004` ở ControlGallery, GridGallery và VisualStyleHost: SDK tự suy ra `win7-x86` cho executable .NET Framework, trong khi lockfiles ban đầu sinh trên Linux không có RID. Root cause được đối chiếu với `Microsoft.NET.RuntimeIdentifierInference.targets` của SDK; không phải lỗi source controls hoặc adapter.

Correction khai báo **restore set** `win7-x86;win7-x64` cho executable `net48` trong root targets, giữ nguyên lựa chọn RID/PlatformTarget hiệu lực của SDK. Cả ba lockfiles executable được regenerate; các dependency groups đã tồn tại giữ nguyên resolved package versions. Các owner lockfiles khác chỉ trả về định dạng EOF do NuGet sinh.

Đã quan sát chu kỳ RED → GREEN: locked restore với restore set mới và locks cũ thất bại `NU1004` đúng ba project; regenerate locks có chủ đích; locked restore exit 0; full Release build hai targets exit 0, **0 warnings / 0 errors**, 15,90 giây. SourceGrid worktree sạch. Sau đó [Windows CI của correction commit](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/actions/runs/40115483052) đã **PASS**: locked restore thành công, cả tám projects build cho cả hai targets, **0 warnings / 0 errors**, locks và vendor không đổi. Review hẹp cho correction không còn finding cần xử lý. Native tests vẫn chưa chạy.

## Phần nghiệm thu tiếp theo

Build graph mới đã đạt trên Windows. Phần còn lại là thực hiện native-test commands của [TESTING](../TESTING.md), ghi actual discovery/pass/fail/skip theo từng owner/framework, mở hai galleries và kiểm native seams bị ảnh hưởng. CI build không thay GUI acceptance.

Sau đó consumer migration được giao riêng theo [CONSUMER_MIGRATION](../CONSUMER_MIGRATION.md). Một source bootstrap có thể dùng để tiếp tục phát triển toolkit, nhưng **A3 toàn phần chưa hoàn tất** khi native và consumer acceptance còn mở.
