# Build và kiểm chứng

Trang này sở hữu commands và cách chọn scope. [Plan A3](plans/20261024-001-a3-winformskit.md) sở hữu trạng thái; [verification report](verification/20261024-a3-bootstrap.md) ghi commands đã chạy, môi trường, kết quả và giới hạn. Ví dụ lệnh bên dưới không phải bằng chứng PASS.

## Môi trường

- Windows cho build/test được hỗ trợ của WinForms; native focus/input/galleries cần desktop tương tác.
- SDK theo root `global.json`: baseline `8.0.400`, `rollForward=latestFeature`. Chạy từ repository root và ghi `dotnet --version` thực tế.
- .NET Framework 4.8 runtime để chạy `net48`; .NET 8 Desktop runtime để chạy `net8.0-windows`. Reference assemblies phục vụ compile `net48` được restore qua NuGet; cài Developer Pack/targeting tools nếu workflow IDE/Designer yêu cầu.
- VS2022 tương thích SDK đã resolve, với workload .NET desktop nếu dùng IDE/Designer. CLI vẫn dùng cùng project graph.
- Git checkout có submodule SourceGrid tại exact gitlink và nguồn NuGet cần cho dependencies đã khai báo.

Linux/static review không chứng minh Windows runtime, caret, focus hay Designer. Nếu môi trường chỉ làm được một phần, ghi riêng phần đã kiểm và phần `NOT RUN`/`BLOCKED`. Không cần SQL instance hoặc credentials cho hai galleries/test owners của toolkit.

## Chuẩn bị checkout

```powershell
git submodule update --init --recursive
git submodule status --recursive
git -C vendor/sourcegrid status --short
dotnet --version
```

Đối chiếu engine với [PROVENANCE](PROVENANCE.md). Không dùng `--remote`; không tiếp tục bằng cách ghi đè vendor changes. Một clean-restore claim cần checkout sạch và cache/package folder mới hoặc môi trường tương đương được ghi rõ; không suy từ warm restore.

Bảy projects thuộc toolkit giữ `packages.lock.json`; `build.ps1` mặc định restore bằng `--locked-mode`. Root targets pin build-only `Microsoft.NETFramework.ReferenceAssemblies` `1.0.3` cho `net48`, với `VersionOverride` ở SourceGrid CPM, để tránh implicit reference khác nhau theo targeting pack trên máy. Engine giữ central package versions của nó và không ghi vendor lockfile. Executable `net48` restore cả `win7-x86;win7-x64` trên mọi host; đây là restore set để khớp RID mặc định của Windows SDK, không đổi TFM hoặc ép bitness. Lockfiles giữ định dạng do NuGet sinh.

Chỉ khi chủ đích sửa dependency graph, regenerate locks rồi review diff trước khi quay lại locked build:

```powershell
dotnet restore MyDmsVn.WinFormsKit.sln --force-evaluate
git diff -- '**/packages.lock.json'
.\build.ps1 -Configuration Release -Scope All
```

Không bỏ locked mode hoặc cập nhật hàng loạt versions chỉ để một checkout build được. Trên Linux, thêm `-p:EnableWindowsTargeting=true` vào lệnh restore có chủ đích và ghi môi trường như phần cross-compilation.

## Entry points

| Script | Tham số | Hành vi |
|---|---|---|
| `build.ps1` | `-Configuration Debug\|Release`, `-Scope All\|Controls` | Locked restore/build solution tương ứng |
| `test.ps1` | Các tham số trên, `-Framework All\|net48\|net8.0-windows`, `-Filter`, `-SkipBuild` | Mặc định gọi build trước rồi chạy owner suites; filter được truyền tới test runner |

`Controls` chọn solution nhỏ, không kéo adapter/SourceGrid. `All` chọn graph đầy đủ. Giữ GUI test runs tuần tự; một scope chọn hai owners không có nghĩa được chạy hai testhosts tranh focus cùng lúc.

### Kiểm candidate bootstrap trên Windows

```powershell
.\build.ps1 -Configuration Release -Scope All
.\test.ps1 -Configuration Release -Scope All -Framework All -SkipBuild
```

Hai lệnh này chạy liền nhau trên cùng candidate/configuration. `-SkipBuild` chỉ dùng khi bước build vừa thành công và source/dependencies không đổi; nếu không chắc, bỏ tham số để script restore/build lại. Kết quả cần review gồm build của cả hai targets và test results theo từng owner/framework. Không coi exit code 0 là đủ nếu discovery rỗng hoặc mandatory cases bị skip. Không lấy output cũ rồi chạy `--no-build`.

### Vòng sửa controls

```powershell
.\test.ps1 -Configuration Debug -Scope Controls -Framework net48
```

Khi sửa shared code/public API, mở rộng sang target còn lại và adapter nếu contract đó có consumer. Một chỉnh sửa Markdown chỉ cần diff/links/encoding; không kéo GUI hoặc SQL.

### Chọn filter đã xác minh

Ví dụ class `DecimalInputParserTests` được giữ từ [controls source][parser-tests]. Trước khi dùng filter, discovery trên candidate và framework đang kiểm phải tìm thấy cases:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Debug -f net48 --list-tests --filter "FullyQualifiedName~DecimalInputParserTests" --blame-hang --blame-hang-timeout 5m
.\test.ps1 -Configuration Debug -Scope Controls -Framework net48 -Filter "FullyQualifiedName~DecimalInputParserTests"
```

Không dùng filter controls cho cả hai owners rồi tính suite không có cases là PASS. Với adapter-only work, gọi project thật, giữ timeout và build/restore phù hợp:

```powershell
dotnet test tests/MyDmsVn.BootstrapSourceGrid.Tests/MyDmsVn.BootstrapSourceGrid.Tests.csproj -c Debug -f net48 --filter "FullyQualifiedName~BootstrapDecimalInputEditorTests" --blame-hang --blame-hang-timeout 5m
```

`BootstrapDecimalInputEditorTests` có trong [adapter source tại pin import][decimal-adapter-tests]; discovery vẫn phải kiểm trên candidate thực tế trước khi dùng làm evidence. Parser-only selection không chứng minh native editor integration.

### Cross-compilation trên Linux

Hai PowerShell entry points chủ đích yêu cầu Windows. Khi chỉ cần kiểm compiler/reference graph trên Linux, gọi CLI trực tiếp:

```sh
dotnet restore MyDmsVn.WinFormsKit.sln --locked-mode -p:EnableWindowsTargeting=true
dotnet build MyDmsVn.WinFormsKit.sln -c Release --no-restore -p:EnableWindowsTargeting=true
```

Ghi rõ đây là cross-build và SDK/targeting packs đã dùng. Kết quả không chứng minh WinForms runtime, helper-host execution, Designer hoặc Windows/native acceptance. Không chạy tests rồi diễn giải thiếu runtime hoặc skipped cases thành PASS.

Workflow [Windows build](../.github/workflows/build.yml) dùng `windows-2022`, gọi build entry point và kiểm locks/vendor không đổi. Nó không chạy native tests, galleries hoặc package publication. Ghi URL/result của workflow trên đúng candidate khi có; cấu hình workflow tồn tại chưa chứng minh job đã chạy.

## Test owners và scope

| Loại thay đổi | Owner cần xem | Quan sát cần giữ |
|---|---|---|
| Parser/projection/input model của UI | `tests/MyDmsVn.Bootstrap5WinFormUI.Tests` | Raw/accepted value, precision, range/caret mapping theo behavior |
| Control/rendering/theme/lifecycle | Controls tests, ControlGallery, VisualStyleHost khi có liên quan | Focus, per-key input, DPI/theme, resources, construction/disposal |
| Adapter/editor registry/grid styling | `tests/MyDmsVn.BootstrapSourceGrid.Tests`, GridGallery | Commit/cancel/conversion, placement, navigation, ownership, theme |
| Shared control contract dùng trong grid | Cả hai owners | UI behavior và adapter compatibility |
| Project paths/resources/targets/SDK | Hai solutions và owners liên quan | Clean build, đúng assembly/resources/helper host, cả hai targets |
| Consumer cutover | Toolkit owners và app tests trong repo sản phẩm | Native app behavior theo [migration runbook](CONSUMER_MIGRATION.md) |
| Docs-only | Diff, local links/anchors, status consistency, UTF-8 | Nội dung đúng owner, không biến historical result thành current PASS |

Bootstrap giữ nguyên hai test suites gốc cùng helper host; chỉ sửa references tới gallery/source mới. Không retirement tests diện rộng. Nếu bỏ/thay test sau này, PR phải nêu behavior còn được bảo vệ và replacement cùng lớp quan sát, theo [test policy proposal][test-policy].

## An toàn native và kiểm tra thủ công

Giữ STA, exception/DataError capture, bounded message pumping và dispose của helpers hiện có. Không mở modal dialog chờ người dùng trong automated tests. `test.ps1` dùng hang diagnostics với giới hạn **5 phút**; raw GUI test commands cũng cần `--blame-hang --blame-hang-timeout 5m`. Timeout là bảo vệ khi hang, không phải ngân sách hiệu năng. Nếu cleanup không xác nhận được, dừng chạy GUI kế tiếp và xử lý test process đúng owner.

Mở ControlGallery hoặc GridGallery làm startup project để kiểm các behaviors bị ảnh hưởng: typing/selection/caret; Enter/Escape/Tab/Shift+Tab và popup dismissal; commit/cancel; theme/font; create/dispose; Designer nếu đổi construction/serialization. Dùng host và native input thật; gán `Text` nguyên chuỗi không thay per-key observations.

Khi có DPI scope, ghi **physical monitor scale** và **window DPI** riêng. Screenshot không tự chứng minh caret/IME, native focus hoặc per-monitor behavior. Không đem physical/Designer evidence từ baseline cũ sang candidate toolkit, theo [native evidence proposal][native-policy].

Helper host input/popup hỗ trợ `--scenario rounded-popup <output-directory>`, `--scenario rounded-popup-theme <output-directory>` và `--scenario input-row <output-directory>`; launch tuần tự với timeout 30 giây. Scenario input-row chụp Light/Dark ở hàng một/hai dòng và log managed/native DPI, native ComboBox height, FlowLayout containment/stability. Scenario popup giữ RGB oracle, Region/content/footer metadata; bản `rounded-popup` dùng Fuchsia để thử child overpaint, bản `rounded-popup-theme` dùng palette Lookup mặc định để Owner duyệt. Các scenario yêu cầu PerMonitorV2 và managed/native DPI bằng nhau. Xem [commands và evidence physical125%](verification/20261029-input-popup-rendering-physical125.md), [bộ ảnh Owner review](verification/20261029-input-popup-rendering-owner-visual-review.md); canonical native PASS không thay full-suite PASS, Owner decision hoặc consumer acceptance.

Với net48 host, dùng built-in `System.Windows.Forms.ApplicationConfigurationSection` trong App.config, không khai báo lại trong configSections. Manifest giữ Windows10 compatibility/Common-Controls; DPI opt-in thuộc App.config. Native HWND120 trong khi Control.DeviceDpi96 không đủ điều kiện nghiệm thu125%.

Host `--scenario combo-accessibility <output-directory>` và `combo-accessibility-primed` kiểm style recreation với provider chưa tạo/đã tạo: gửi MSAA/UIA queries tới retiring HWND, sau đó lấy IAccessible trên HWND ổn định. Subprocess có timeout15 giây và bounded cleanup5 giây; fixture giữ hang diagnostics5 phút. Regression phân biệt object ID sign-extended và zero-extended trên x64. Xem [stability evidence](verification/20261029-input-popup-rendering-stability.md).

Clipboard assertions chỉ chờ tối đa1 giây khi đọc rỗng và vẫn so sánh exact lexical text; không reissue copy/cut hoặc ghi lại expected value. Controlled republishing test giữ native empty interval tới khi observer thực sự thấy nó, rồi publish lại actual text đã copy. Test này chứng minh khả năng chịu transient availability, không xác định actor của lần lỗi lịch sử. Row-sharing performance assertions đo synchronous theme setter; consumer/accessibility reads được pump sau đó thuộc phạm vi riêng.

## Báo cáo kết quả

Mỗi lần nghiệm thu ghi candidate SHA, engine pin, OS/SDK/runtime, configuration/framework/bitness, commands/filter, discovered/executed/pass/fail/skip và limitations. Ghi warm hay clean restore; với manual observations ghi gallery, thao tác và kỳ vọng quan sát được. Raw TRX/dumps/logs giữ ngoài Git; chỉ commit bản tóm tắt đã rà soát.

Build chưa chạy là `NOT RUN`; thiếu nền tảng bắt buộc là `BLOCKED`; cases skipped được ghi đúng số lượng/lý do. Results của toolkit không chứng minh app consumer đã migration hoặc public package đã đủ điều kiện phát hành.

[parser-tests]: https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI/blob/8ae217ea8aa9a938ebd558f7071dc737f909e6a5/tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting/DecimalInputParserTests.cs
[decimal-adapter-tests]: https://github.com/chung6a8m/MyDmsVn.BootstrapSourceGrid/blob/e311144c27cd79a6c34aa0308ad029651c9e03c9/tests/MyDmsVn.BootstrapSourceGrid.Tests/BootstrapDecimalInputEditorTests.cs
[test-policy]: https://github.com/chung6a8m/MyDmsVn.Docs/blob/57cf5e0f506ac0c313e9d659939d863325250b53/proposals/2026-10-24-docs-tests-skills.md#4-dọn-tests-theo-rủi-ro-được-bảo-vệ
[native-policy]: https://github.com/chung6a8m/MyDmsVn.Docs/blob/57cf5e0f506ac0c313e9d659939d863325250b53/proposals/2026-10-24-ui-prototyping.md#7-kiểm-chứng-theo-đúng-loại-bằng-chứng
