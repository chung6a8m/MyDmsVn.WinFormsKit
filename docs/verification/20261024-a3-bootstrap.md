# Kiểm chứng candidate bootstrap A3

**Phạm vi:** lần khởi tạo repository theo yêu cầu ngày 24/10/2026. Candidate Windows build đầu tiên là [`bdc5c126d4d3caa74ca8ed4518fc565d3a492ba6`](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/commit/bdc5c126d4d3caa74ca8ed4518fc565d3a492ba6), tree `3349567b3ed07379d95fb491684400a3ada54880`, gồm bootstrap và correction cho dependency locks trên Windows. Bootstrap gốc là [`ccba9e7245a5af2db8c9dec0c542c5896f5a36e0`](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/commit/ccba9e7245a5af2db8c9dec0c542c5896f5a36e0), tree `370cf716672cdc304a05c7358765af88850518cb`. Kết quả build/test mới của candidates `a176723` và `0407c47` được ghi ở cuối báo cáo. Evidence được thu trực tiếp trên các candidates nêu rõ bên dưới; không lấy PASS lịch sử của repository nguồn. [Plan A3](../plans/20261024-001-a3-winformskit.md) là owner trạng thái tổng thể.

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

## Phần nghiệm thu còn lại sau Windows CI

Tại candidate `bdc5c12`, build graph mới đã đạt trên Windows nhưng native tests và galleries chưa chạy. Lượt chạy tiếp theo trên candidate `a176723` được ghi bên dưới. CI build không thay GUI acceptance.

Sau đó consumer migration được giao riêng theo [CONSUMER_MIGRATION](../CONSUMER_MIGRATION.md). Một source bootstrap có thể dùng để tiếp tục phát triển toolkit, nhưng **A3 toàn phần chưa hoàn tất** khi native và consumer acceptance còn mở.

## Kiểm tra Windows ngày 25/10/2026 trên candidate `a176723`

Candidate `a1767236c144fa8e9a43a002aa68e97dfbb7a662` (`main`), working tree sạch trước khi kiểm; khác `bdc5c12` chỉ ở tài liệu. SourceGrid tại gitlink `f4e457b43582bf01892f50bdc74aa480531e5944`, vendor worktree sạch. Máy là Windows 11 Home Single Language 64-bit, version `10.0.26300`, .NET SDK `8.0.426`, VSTest `17.11.1` x64; .NET 8 Desktop runtime `8.0.32` có mặt. Build dùng `Release`, locked restore trên package cache hiện có; đây không phải clean-restore evidence.

Chạy tuần tự từ repository root, không filter:

```powershell
.\build.ps1 -Configuration Release -Scope All
.\test.ps1 -Configuration Release -Scope All -Framework All -SkipBuild
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --no-build --no-restore --blame-hang --blame-hang-timeout 5m --results-directory artifacts/test-results --logger "trx;LogFileName=MyDmsVn.Bootstrap5WinFormUI.Tests.net8.0-windows.trx"
dotnet test tests/MyDmsVn.BootstrapSourceGrid.Tests/MyDmsVn.BootstrapSourceGrid.Tests.csproj -c Release -f net48 --no-build --no-restore --blame-hang --blame-hang-timeout 5m --results-directory artifacts/test-results --logger "trx;LogFileName=MyDmsVn.BootstrapSourceGrid.Tests.net48.trx"
dotnet test tests/MyDmsVn.BootstrapSourceGrid.Tests/MyDmsVn.BootstrapSourceGrid.Tests.csproj -c Release -f net8.0-windows --no-build --no-restore --blame-hang --blame-hang-timeout 5m --results-directory artifacts/test-results --logger "trx;LogFileName=MyDmsVn.BootstrapSourceGrid.Tests.net8.0-windows.trx"
```

Locked restore và full solution build: **PASS**, cả tám projects trên `net48` và `net8.0-windows`, 0 warnings / 0 errors. `test.ps1` dừng sau controls `net48` thất bại; ba tổ hợp còn lại được chạy riêng với cùng build và hang timeout. TRX thô nằm trong `artifacts/test-results/`, không commit.

| Owner / target | Discovered | Executed | Pass | Fail | Không chạy | Kết quả |
|---|---:|---:|---:|---:|---:|---|
| Controls `net48` | 2359 | 2357 | 2351 | 6 | 2 | FAIL |
| Controls `net8.0-windows` | 2359 | 2357 | 2351 | 6 | 2 | FAIL |
| Adapter `net48` | 256 | 256 | 256 | 0 | 0 | PASS |
| Adapter `net8.0-windows` | 256 | 256 | 256 | 0 | 0 | PASS |

Hai ca controls `NotExecuted` trong TRX là “Native first content click from another control keeps popup and search focus” và “Native first arrow click from another control keeps popup and search focus”. VSTest console ghi hai dòng `Skipped` nhưng tổng kết của runner ghi `Skipped: 0`; bảng trên dùng số test entries và outcomes trong TRX, không diễn giải hai ca đó là PASS.

Cùng sáu ca `BootstrapDateInputInteractionTests` fail ở cả hai targets: hai biến thể pointer của `DateInputCorrection_Canceled_popup_restores_native_range_before_notification`, `DateInputCorrection_External_dismissal_does_not_steal_new_focus`, `DateInputCorrection_F4_popup_closed_by_trigger_restores_native_editor_focus`, `DateInputCorrection_Pointer_calendar_cancel_returns_focus_to_native_editor` và `Native_alt_down_opens_calendar_without_accepting_candidate`. Quan sát gồm `PendingText` còn `5/11` thay vì `06/11/2026`, `Calendar.SelectedDate` null, native editor không lấy lại focus và sự kiện mở calendar không phát. Chưa xác định root cause; cần điều tra trên desktop trước khi kết luận regression source hay điều kiện chạy. Hai galleries, physical DPI, Designer và consumer app: **NOT RUN** trong lượt này. V1 và A3 vẫn mở.

## Kiểm tra V1 sau điều tra focus, candidate `0407c47`

Candidate `0407c47699cd57c08f96d3b05ea438fc23709d26` chỉ đổi test host DateInput so với source ở `a176723`; production controls/adapter và engine gitlink không đổi. Khi chạy fixture DateInput riêng, 38/38 đạt. Chạy một fixture tạo form trên STA thread riêng trước DateInput tái hiện việc `Form.ActiveForm` thành null: foreground chuyển về process ChatGPT sau khi form trước đóng, và `SetForegroundWindow` đơn lẻ trả false. Test host DateInput hiện gắn input threads tạm thời khi kích hoạt form WinForms thật, rồi detach ngay; không thêm timeout/fail-fast vào production. Tổ hợp gây lỗi đạt 41/41 sau thay đổi; ba tests context menu vẫn giữ nguyên source ban đầu.

Trên cùng Windows 11/SDK/runtime/cache nêu ở mục trước, chạy từ root, `Release`, không filter cho full suites:

```powershell
.\build.ps1 -Configuration Release -Scope All
.\test.ps1 -Configuration Release -Scope All -Framework All -SkipBuild
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --no-build --no-restore --filter "FullyQualifiedName~NativeFirstClickFromAnotherControlKeepsPopupAndSearchFocus" --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --no-build --no-restore --filter "FullyQualifiedName~NativeFirstClickFromAnotherControlKeepsPopupAndSearchFocus" --blame-hang --blame-hang-timeout 5m
```

Locked restore và full Release build: **PASS**, 0 warnings / 0 errors. `test.ps1`: **PASS**, exit 0, bốn combinations chạy tuần tự. Hai explicit `SendInput` cases được chọn trực tiếp bằng filter đã xác minh: 2/2 **PASS** trên mỗi framework. TRX full-suite cho số liệu:

| Owner / target | Discovered | Executed | Pass | Fail | Explicit không chạy trong full suite |
|---|---:|---:|---:|---:|---:|
| Controls `net48` | 2359 | 2357 | 2357 | 0 | 2 |
| Controls `net8.0-windows` | 2359 | 2357 | 2357 | 0 | 2 |
| Adapter `net48` | 256 | 256 | 256 | 0 | 0 |
| Adapter `net8.0-windows` | 256 | 256 | 256 | 0 | 0 |

Gallery observations trên **.NET 8 Desktop**, window DPI `96` từ GridGallery diagnostics; physical monitor scale chưa đo. ControlGallery mở trang Advanced Inputs; `Backspace` rồi phím `7` đổi date formatted input `31/08/2026` → `31/08/2027`, raw `31082026` → `31082027`; chuyển Light → Dark cập nhật nền/chữ mà giữ nội dung. GridGallery: F2 mở Bootstrap text editor, Enter commit `Edited 01`, lần sửa tiếp Escape giữ `Edited 01`; Bootstrap lookup popup mở bằng chuột, Escape đóng popup rồi hủy editor; Light → Dark cập nhật grid. Cả hai host đóng sạch trong quan sát UI. Đây là hosted interaction, không chứng minh physical DPI, IME hay Designer construction; các mục đó **NOT RUN**. Consumer app và rollback thuộc M1, không được suy từ V1.

## Toolkit candidate `eeb2513` và migration lịch sử từ app `8cfa253`

Toolkit `eeb251349b9584228b4fdbe701d196452aa134f8` thêm `Directory.Packages.props` ở root để source submodule không kế thừa Central Package Management của app. Trước correction, app `dotnet restore MyDmsVn.sln --locked-mode` fail `NU1004` vì `Microsoft.Data.SqlClient` bị chuyển thành centrally managed transitive dependency. Sau correction, toolkit `./build.ps1 -Configuration Release -Scope All` và app locked restore/Release build đều **PASS**, 0 warnings/errors. Commit đã có trên branch remote `codex/complete-a3`; SourceGrid vẫn ở `f4e457b43582bf01892f50bdc74aa480531e5944`.

Toolkit `./test.ps1 -Configuration Release -Scope All -Framework All -SkipBuild` trên `eeb2513` có một lượt **FAIL**: controls `net48` 2356 pass/1 fail, `ThemeChangePreservesRowSharingForTheLargeScenario` quan sát một row unshared. Ca riêng 1/1 và fixture 6/6 đều pass. Lượt chạy lại full suite tuần tự **PASS**: controls `net48` và `net8.0-windows` mỗi target 2359 discovered, 2357 executed/pass, 0 fail, 2 explicit `NotExecuted`; adapter mỗi target 256 discovered/executed/pass, 0 fail. Hai explicit cases đã đạt 2/2 trên mỗi target ở candidate `0407c47`; `eeb2513` chỉ đổi build metadata, không đổi test/production code. Sự không ổn định row-sharing trong lượt đầu vẫn là giới hạn quan sát, không xóa kết quả fail lịch sử.

Consumer app base là `8cfa25351338666b1a751ee516d5a479cdb37b7f`, branch `codex/a3-consumer-cutover`, migration commit `0f4e273bfaa31249da1167cac98f5d6f009f8bf1`. Old gitlinks thực tế: BSG `09eab7ad3d6ef0a7fcd27c2b198f7ca573dc4b0f`, UI `aba102e33c48937fd92468791c287afda0a59e77`, engine `f4e457b43582bf01892f50bdc74aa480531e5944`. App thay một gitlink bằng toolkit `eeb2513`, giữ project GUIDs, assembly identities và `net48` active. Fresh detached checkout tại `0f4e273` chạy `git submodule update --init --recursive`: toolkit và engine lấy từ GitHub đúng pins, worktree sạch; `dotnet restore MyDmsVn.sln --locked-mode` và `dotnet build MyDmsVn.sln -c Release --no-restore` **PASS**, 0 warnings/errors. Output Desktop.App có đúng một `MyDmsVn.Bootstrap5WinFormUI.dll` và một `MyDmsVn.BootstrapSourceGrid.dll`; gallery/test assemblies không được đưa vào production executable.

App focused GUI filter `UiDependencyCompatibilityTests|GoodsReceiptControlTests|FoundationShellFormTests`: 38/38 **PASS**; Architecture `net48`: 8/8 **PASS**; Desktop `net48` không filter: 138/138 **PASS**. App guarded `Invoke-GuiTestAcceptance.ps1` trên clean commit `0f4e273`: 137 source methods, 138 discovered/executed, watchdog và GUI lease sạch, exit 0; raw TRX/inventory ở ignored `artifacts/` của app. Đây là native app testhost x64 trên Windows 11, không phải quan sát physical DPI hoặc packaged manual acceptance.

Rollback được kiểm trong detached worktree từ `0f4e273`: `git revert --no-edit 0f4e273`, `git submodule sync --recursive`, `git submodule update --init --recursive` phục hồi đúng ba old pins nêu trên. Reverted checkout chạy locked restore, Release build 0 warnings/errors và native `UiDependencyCompatibilityTests` smoke 2/2 **PASS**. Revert chỉ tồn tại trong checkout kiểm chứng, không đảo migration branch.

App Final gate được thử trên clean migration commit bằng `Invoke-TestGate.ps1 -Mode Final -PlanId 20261016-003-permission-system-rebuild -TaskId SEC-REBUILD-T06 -ArtifactRoot artifacts/a3-full-final-0f4e273 -ApprovalFile <local-approved-file>`. Lượt đầu **FAIL trước SQL execution** ở `sec-rebuild-t06-final-runner/net8.0`: `FingerprintMismatch,IncompleteFingerprint` cho runner tests, exit 1. `-PreflightOnly` trên cả reverted graph cũ và fresh graph mới đều **PASS** (6 reviewed SQL project gates, không SQL execution). Lượt Final từ fresh checkout cùng commit chỉ hoàn tất `sec-rebuild-t06-final-session/net48` (108/108 PASS), sau đó exit 1; chưa có kết quả SQL. Hai lượt Final khác nhau ở vị trí dừng, vì vậy chưa kết luận nguyên nhân runner mismatch. Theo policy của app, consumer branch chưa đạt full PR/release acceptance dù A3 source/native cutover và rollback có evidence. Không diễn giải preflight hoặc non-SQL pass như SQL PASS.

## M1 từ exact checkout app `6c385d7` ngày 25/10/2026

Đây là candidate đáp ứng baseline app được yêu cầu: checkout `C:\MyData\2026\MyDmsVn\CleanArchitecture` bắt đầu sạch tại `6c385d7630b6b21e915e244113c214aa616327f6`, branch `codex/a3-consumer-from-6c385d7`, migration commit `20c6a6ed58363736f6d7542cf2515af3a1bbd686`. Windows 11, SDK `8.0.426`, Release, app `net48`, native testhost được ghi riêng theo x64/x86. Trước migration, `dotnet restore MyDmsVn.sln --locked-mode` và `dotnet build MyDmsVn.sln -c Release --no-restore` **PASS**, 0 warnings/errors. Old gitlinks kiểm bằng `git submodule status --recursive`: BSG `e311144c27cd79a6c34aa0308ad029651c9e03c9`, UI `8ae217ea8aa9a938ebd558f7071dc737f909e6a5`, engine `f4e457b43582bf01892f50bdc74aa480531e5944`.

Migration đổi `.gitmodules`, ba paths trong `MyDmsVn.sln` và một `ProjectReference` trong Desktop.WinForms; giữ project GUIDs, assembly names và `net48`. `git submodule add`, checkout toolkit `eeb251349b9584228b4fdbe701d196452aa134f8`, rồi recursive init lấy SourceGrid `f4e457b`. `git submodule status --recursive` và vendor `status --short` xác nhận pins/worktrees sạch; staged diff/lockfiles không có package version change. App `dotnet restore MyDmsVn.sln --locked-mode` và `dotnet build MyDmsVn.sln -c Release --no-restore` **PASS**, 0 warnings/errors. Desktop.App output có đúng một DLL cho UI, adapter và SourceGrid; không có gallery/test DLL.

App tests chạy tuần tự với `--blame-hang --blame-hang-timeout 5m`, từ build mới và filter đã `--list-tests` xác nhận có cases:

| Scope / command bổ sung | Discovered | Executed | Pass | Fail | Skip | Kết quả |
|---|---:|---:|---:|---:|---:|---|
| Desktop `net48`, `--filter "FullyQualifiedName~UiDependencyCompatibilityTests\|FullyQualifiedName~GoodsReceiptControlTests\|FullyQualifiedName~FoundationShellFormTests"`, x64 | 86 | 86 | 86 | 0 | 0 | PASS |
| Architecture `net48`, không filter | 13 | 13 | 13 | 0 | 0 | PASS |
| Desktop `net48`, không filter, x64 | 834 | 813 | 787 | 26 | 21 | FAIL |
| Desktop `net48`, `--filter "FullyQualifiedName~Native_cell_entry_uses_registered_bootstrap_editor\|FullyQualifiedName~Cancelled_preceding_search_preserves_per_key_order" -- RunConfiguration.TargetPlatform=x86` | 4 | 4 | 4 | 0 | 0 | PASS |
| Desktop `net48`, `--filter "FullyQualifiedName~Disposed_or_old_session_completions_cannot_overwrite_current_input" -- RunConfiguration.TargetPlatform=x86` | 4 | 4 | 0 | 4 | 0 | FAIL, baseline giống nhau |

Full x64 failures gồm các assertions `IntPtr.Size == 4` ở native input, bốn hàng `ReceiptAcceptanceTests.Disposed_or_old_session_completions_cannot_overwrite_current_input` kỳ vọng `GoodsReceiptControl` nhưng thấy app-owned `WorkspaceFeedbackHost`, và ma trận SAL thiếu local ignored capture directory. Filter x86 4/4 xác nhận bitness là nguyên nhân của các ca được chọn; không ngoại suy cho mọi failure x64. Bốn hàng ReceiptAcceptance cũng **FAIL 4/4 với cùng assertion** khi locked restore/build và chạy filter x86 trên checkout gốc `6c385d7` cùng old pins; chúng không do migration tạo ra. Full Desktop owner vẫn là FAIL; không dùng 86/86 để thay kết quả đó. SQL, packaged/attended GUI, physical DPI và Designer trên `20c6a6e`: **NOT RUN**; 21 skipped cases trong full run không tính là PASS. Gate `Invoke-TestGate.ps1` nêu ở migration `0f4e273` không tồn tại trong test policy/checkout `6c385d7` và không áp dụng cho candidate này.

Rollback được kiểm trên detached worktree riêng: checkout `20c6a6e`, recursive init xác nhận toolkit/engine pins, `git revert --no-edit 20c6a6e`, `git submodule sync --recursive`, `git submodule update --init --recursive` phục hồi chính xác BSG `e311144`, UI `8ae217e`, engine `f4e457b`. Reverted candidate `a876224` chạy `dotnet restore MyDmsVn.sln --locked-mode`, `dotnet build MyDmsVn.sln -c Release --no-restore` **PASS** 0 warnings/errors, và `UiDependencyCompatibilityTests` x86 2/2 **PASS**. Revert nằm ở worktree kiểm chứng, branch migration giữ commit `20c6a6e`.

## Review correction PR #1 — DateInput test host `07e418b` ngày 25/10/2026

Hai findings P2 trong `BootstrapDateInputInteractionTests.Host()` được xử lý tại toolkit commit `07e418b919637e4a910269c7384b1ed9e8177841`: helper giữ quyền sở hữu form cho đến khi setup thành công và dispose khi exception; bỏ `AttachThreadInput` với foreground thread ngoài process. Nếu `SetForegroundWindow` bị Windows từ chối, helper gửi một click native vào caption của chính form test đang hiển thị, rồi chờ tối đa hai giây và kiểm foreground window, `Form.ActiveForm`, native editor focus. Foreground không đạt sẽ fail setup với diagnostics và form được dispose. Production controls, adapter và SourceGrid gitlink không đổi.

Trên Windows 11, SDK `8.0.426`, Release, native VSTest x64, chạy tuần tự với `--blame-hang --blame-hang-timeout 5m`:

| Lượt / target | Command và filter | Executed | Pass | Fail | Explicit không chạy | Kết quả |
|---|---|---:|---:|---:|---:|---|
| Thử đầu sau khi bỏ input queue attach, `net48` | `dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --no-build --no-restore --blame-hang --blame-hang-timeout 5m` | 2357 | 2320 | 37 | 2 | FAIL: Windows từ chối `SetForegroundWindow`; helper fail sau hai giây, không hang |
| Correction `07e418b`, `net48` | Cùng command trên, build từ source mới | 2357 | 2357 | 0 | 2 | PASS |
| Correction `07e418b`, `net8.0-windows` | `dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --blame-hang --blame-hang-timeout 5m` | 2357 | 2357 | 0 | 2 | PASS |

Fixture DateInput riêng trên `net48` đạt 38/38 sau correction. Hai full Controls runs ở correction đạt 0 fail và có cùng hai cases `[Explicit]` không chạy trong full suite; không tính chúng là PASS mới. Adapter owner, explicit native cases, galleries, physical DPI và Designer **NOT RUN** tại `07e418b` vì diff chỉ chạm test helper; evidence của `eeb2513` ở trên vẫn giữ nguyên phạm vi candidate của nó. `git diff --check` và SourceGrid pin `f4e457b` được kiểm lại trước commit.
