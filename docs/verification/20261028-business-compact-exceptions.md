# Kiểm chứng candidate ba ngoại lệ Business Compact — 2026-10-28

**Scope:** [plan được Owner chọn](../plans/20261028-001-business-compact-exceptions.md) chỉ gồm disabled action, vòng đời Font của modal và multiline read-only. Source candidate đã được triển khai. **Windows focused native tests: PASS trên cả hai TFM. Consumer visual review: PASS ở physical 100%, BLOCKED ở physical 125%. Owner native approval: PENDING. Các ngoại lệ vẫn OPEN.**

## Checkpoint Windows/native EXC-T05

Checkpoint này thay trạng thái `NOT RUN` cũ cho focused tests, nhưng không thay các giới hạn của cross-build lịch sử bên dưới.

| Mục | Candidate / môi trường cuối |
|---|---|
| Toolkit code/test revision | `a71d64bd82eb40123eafef4c5a6832e53d7ef9ca`; source tree `d2edfe81dd4d4949889fa5cae69bc6129a8a8837`; test tree `7dfba0532f79b3052a3039b94b64446b91f797ec` |
| SourceGrid | `f4e457b43582bf01892f50bdc74aa480531e5944`; không đổi |
| Host | Windows 11 Home Single Language 10.0.26300 x64; SDK `8.0.427`; MSBuild `17.11.69`; .NET Framework 4.8; .NET Desktop Runtime 8.0.32/8.0.33 |
| Build | `.\\build.ps1 -Configuration Debug -Scope All`: PASS cả `net48`/`net8.0-windows`, 0 warning, 0 error, 12,61 giây |
| API | `Phase16PublicApiBaselineTests` thuộc selection controls PASS; fingerprint giữ `e6735faae42a8eb41c06cdd5d4fd27a87e56dccaee73f579cd4460ef0693db6e`, delta đúng ba property đã duyệt |

Discovery được chạy trên đúng project/TFM/filter trước execution. Controls selection gồm `BootstrapButtonTests`, `BootstrapModalThemeDpiAccessibilityTests`, `BootstrapTextBoxMultilineTests`, `BootstrapTextBoxTests`, `Phase16PublicApiBaselineTests`, accessory/validation/Select accessibility và formatted-input consumer liên quan. Adapter selection dùng project riêng `MyDmsVn.BootstrapSourceGrid.Tests` và gồm `BootstrapTextBoxEditorTests` cùng adapter bị ảnh hưởng; không dùng filter controls để tạo selection rỗng.

| Project / TFM | Discovered | Executed | Pass / fail / skip | TRX local đã giữ ngoài Git |
|---|---:|---:|---:|---|
| `MyDmsVn.Bootstrap5WinFormUI.Tests` / `net48` | 75 | 75 | **75 / 0 / 0** | `toolkit-controls-net48-rerun/toolkit-controls-net48.trx` |
| `MyDmsVn.Bootstrap5WinFormUI.Tests` / `net8.0-windows` | 75 | 75 | **75 / 0 / 0** | `toolkit-controls-net8/toolkit-controls-net8.trx` |
| `MyDmsVn.BootstrapSourceGrid.Tests` / `net48` | 40 | 40 | **40 / 0 / 0** | `toolkit-adapter-net48/toolkit-adapter-net48.trx` |
| `MyDmsVn.BootstrapSourceGrid.Tests` / `net8.0-windows` | 40 | 40 | **40 / 0 / 0** | `toolkit-adapter-net8/toolkit-adapter-net8.trx` |

Lượt controls `net48` đầu tiên đạt 74/75. Regression fallback-font tạo `Font` từ tên family đã resolve; trên Windows `Font.Equals` còn so `OriginalFontName`, nên fixture không thể đạt nhánh equal-value mà production cần bảo vệ. Commit `a71d64b` tái tạo cùng tên family không tồn tại cho cả hai Font, xác nhận fallback thực tế nhưng giữ equality đúng; focused 2/2 và full selection cuối 75/75 PASS. Đây là sửa test fixture, không đổi production behavior hoặc public API.

Review trực quan dùng consumer Security UI Lab tại physical 100%/DeviceDpi 96 đã xác nhận disabled palette Light/Dark, modal title/theme/lifetime và multiline read-only trong composition thực tế. Physical 125% chưa có vì Windows Settings không cung cấp surface accessibility điều khiển được trong phiên; không dùng registry hack hay DPI giả lập. Chi tiết/screenshot thuộc report tích hợp MyDmsVn. Owner approval vẫn PENDING.

## Candidate và phạm vi bằng chứng

| Mục | Giá trị tại lượt ghi này |
|---|---|
| Toolkit source baseline | `efe2e7212fd36e32c91b914bdc5e4081947124fc` |
| Consumer pin trước sửa | `c232a4c09dc0386a5bd8bb484a5c991acae827d5`; cùng tree với toolkit baseline |
| SourceGrid | `f4e457b43582bf01892f50bdc74aa480531e5944`; không đổi |
| Candidate source/tests | `a71d64bd82eb40123eafef4c5a6832e53d7ef9ca`; immutable source tree `d2edfe81dd4d4949889fa5cae69bc6129a8a8837`, test tree `7dfba0532f79b3052a3039b94b64446b91f797ec` |
| Build candidate identity | Build và native tests cuối dùng đúng source/test trees ở trên; tài liệu được hoàn tất sau execution, không đổi C# hoặc project graph |
| Targets/configuration | `net48;net8.0-windows`, Debug, full solution gồm controls/adapter và hai test owners |
| Host | Linux `6.18.55`, x86_64; .NET SDK `8.0.427` được execution owner ghi từ lượt chạy ngoài sandbox |
| Restore cache condition | Không làm sạch cache; final build dùng assets đã locked-restore và local NuGet cache. Không có clean-restore claim |
| Public API review | **PASS — compiled metadata review và Windows test gate trên cả hai targets**; candidate fingerprint `e6735faae42a8eb41c06cdd5d4fd27a87e56dccaee73f579cd4460ef0693db6e`; xem [API baseline](../components/controls/PUBLIC_API_BASELINE.md) |
| Consumer integration | App UiLab Debug net48/x86 source-mirror cross-build **PASS**; exact consumer gitlink và final build được sở hữu bởi report tích hợp trong MyDmsVn, không phải acceptance của toolkit |

Report này không thay [evidence A3](20261024-a3-bootstrap.md), không đóng điều kiện còn lại của A3 và không chuyển historical PASS/approval sang candidate mới. Không có package/release/SQL work trong scope này.

## Source candidate đã triển khai

| Thay đổi | Source và regression | Giới hạn hiện tại |
|---|---|---|
| Disabled action | [BootstrapButtonRenderLogic](../../src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapButtonRenderLogic.cs) dùng secondary surface/normal text cho disabled state; [BootstrapButtonTests](../../tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapButtonTests.cs) bảo vệ readability và phân biệt enabled Secondary outline | Focused tests hai TFM PASS; appearance/inertness Light/Dark consumer physical 100% PASS, 125% và Owner còn chờ |
| Modal Font lifetime | [BootstrapModalHeader](../../src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalHeader.cs) tái sử dụng Font do toolkit sở hữu khi typography không đổi; [BootstrapModalThemeDpiAccessibilityTests](../../tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapModalThemeDpiAccessibilityTests.cs) có regression vòng đời/theme | Native measurement/painting/theme/disposal tests hai TFM PASS; consumer physical 100% PASS, 125% và Owner còn chờ |
| Multiline read-only | [BootstrapTextBox](../../src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTextBox.cs) thêm `Multiline`, `WordWrap`, `ScrollBars`, multiline-only layout và native accessibility metadata; [BootstrapTextBoxMultilineTests](../../tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTextBoxMultilineTests.cs) cùng defaults regression | Controls/adapter tests hai TFM PASS; native selection/copy/scroll/focus/accessibility consumer physical 100% PASS, 125% và Owner còn chờ |

Contracts được cập nhật tại [COMPONENTS](../components/controls/COMPONENTS.md), [COMPATIBILITY](../components/controls/COMPATIBILITY.md) và [PUBLIC_API_BASELINE](../components/controls/PUBLIC_API_BASELINE.md). Việc test có trong source hoặc compile thành công không chứng minh test đã RED/GREEN trên native runtime.

## Commands và kết quả đã ghi

Các lệnh thành công sau được execution owner chạy từ repository toolkit, trên cùng working-tree lượt cross-build:

```sh
/tmp/mydmsvn-dotnet/dotnet restore MyDmsVn.WinFormsKit.sln --locked-mode -p:EnableWindowsTargeting=true
/tmp/mydmsvn-dotnet/dotnet build MyDmsVn.WinFormsKit.sln -c Debug --no-restore -p:EnableWindowsTargeting=true -v:minimal
```

| Kiểm tra | Kết quả | Điều được chứng minh |
|---|---|---|
| Locked restore toolkit | **PASS — cross-build preparation** | Dependency/reference graph restore bằng locked mode; không cập nhật lockfiles |
| Full solution Debug cross-build, sau API baseline update | **PASS — compiler only: 0 warnings, 0 errors; 12,77 giây** | Cả hai targets, controls/adapter và test owners compile trong full graph |
| Thử gọi SDK/build trong sandbox ban đầu | **BLOCKED — tooling environment** | `/proc` process-information access gây `System.ComponentModel.Win32Exception`; không phải compiler/source failure. Execution owner chạy lại ngoài sandbox thành công |
| Windows test discovery | **PASS** | Controls 75/TFM; adapter 40/TFM; selection không rỗng |
| Windows test execution | **PASS** | 230 executions tổng cộng: 230 passed, 0 failed, 0 skipped; TRX local giữ ngoài Git |
| Gallery/Designer/physical DPI/native Owner review | **PARTIAL / PENDING** | Consumer physical 100% PASS; physical 125% BLOCKED; Owner PENDING |

Lượt kiểm tài liệu còn tái hiện hạn chế sandbox khi chạy `/tmp/mydmsvn-dotnet/dotnet --version` (exit 134, lỗi process information); không dùng lần thất bại đó làm bằng chứng SDK thiếu hoặc source không build. Báo cáo cross-build thành công ở trên dựa trên lượt chạy ngoài sandbox đã ghi.

## Compiled public API review

Execution owner đã tái tạo baseline `c232a4c` với fingerprint `5016acec10c7efdafe03387de2c8f182a2e96c49f473f1bcb8bc6e5932dfa6cb` trên cả hai targets. Candidate fingerprint là `e6735faae42a8eb41c06cdd5d4fd27a87e56dccaee73f579cd4460ef0693db6e`, gồm 182 exported types và 1.748 dòng surface; delta chỉ gồm ba property `BootstrapTextBox.Multiline`, `WordWrap`, `ScrollBars`. `PUBLIC_API_BASELINE.md` và `ApprovedV1Fingerprint` đã được cập nhật sau review. `Phase16PublicApiBaselineTests` PASS trong controls selection Windows trên cả hai TFM; native behavior được chứng minh riêng bởi các selection controls/adapter và review consumer ghi trên.

Workflow toolkit `.github/workflows/build.yml` cấu hình `windows-2022` để build, không chạy native tests. MyDmsVn chưa có `.github/workflows`; không có current-candidate CI job/result được ghi trong report này. Cấu hình workflow hoặc Linux cross-build không thay Windows/native execution.

## Windows/native còn cần thực hiện

Focused discovery/execution theo [TESTING](../TESTING.md) đã hoàn tất trên cả `net48` và `net8.0-windows`, serialized STA và cleanup thành công như bảng checkpoint. Không còn test-gate toolkit PENDING trong scope này.

Native review consumer còn cần physical 125% Light/Dark × normal/minimum, sau đó quyết định Owner có ngày. Physical 100% đã được quan sát với window DPI 96; screenshot, UIA/clipboard và giới hạn cụ thể được ghi trong report app. Chưa có Owner decision mới.

## Các field phải hoàn tất khi tích hợp

- **RECORDED:** source/test tree hashes ở trên xác định đúng compiler candidate; upstream commit là commit chứa report. Nếu source/test đổi, cần evidence mới cho delta đó.
- **RECORDED:** Windows execution của API test gate PASS trong controls selection trên cả hai TFM; compiled delta/fingerprint không đổi.
- **CONSUMER OWNER:** report `docs/evidence/ui-labs/20261028-business-compact-exceptions.md` trong MyDmsVn ghi exact gitlink/source/test candidate và final consumer build.
- **RECORDED:** actual Windows discovery/execution totals và physical-100 observations.
- **PENDING:** physical-125 observations và dated Owner decision.

Cho đến khi các bằng chứng tương ứng tồn tại, ba ngoại lệ vẫn **OPEN** và Owner native approval vẫn **PENDING**. Source implementation/cross-build không cấp product, package hoặc release acceptance.
