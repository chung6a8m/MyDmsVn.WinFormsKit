# Kiểm chứng candidate ba ngoại lệ Business Compact — 2026-10-28

**Scope:** [plan được Owner chọn](../plans/20261028-001-business-compact-exceptions.md) chỉ gồm disabled action, vòng đời Font của modal và multiline read-only. Source candidate đã được triển khai; cross-build được ghi riêng dưới đây. **Windows/native tests: NOT RUN / BLOCKED. Owner native approval: PENDING. Các ngoại lệ vẫn OPEN.**

## Candidate và phạm vi bằng chứng

| Mục | Giá trị tại lượt ghi này |
|---|---|
| Toolkit source baseline | `efe2e7212fd36e32c91b914bdc5e4081947124fc` |
| Consumer pin trước sửa | `c232a4c09dc0386a5bd8bb484a5c991acae827d5`; cùng tree với toolkit baseline |
| SourceGrid | `f4e457b43582bf01892f50bdc74aa480531e5944`; không đổi |
| Candidate source/tests | Commit chứa report này trên baseline; immutable source tree `d2edfe81dd4d4949889fa5cae69bc6129a8a8837`, test tree `db8e76a41f336925570961ba2fb56cf721de3640` |
| Build candidate identity | Build sau API baseline update dùng đúng source/test trees ở trên; tài liệu được hoàn tất sau build, không đổi C# hoặc project graph |
| Targets/configuration | `net48;net8.0-windows`, Debug, full solution gồm controls/adapter và hai test owners |
| Host | Linux `6.18.55`, x86_64; .NET SDK `8.0.427` được execution owner ghi từ lượt chạy ngoài sandbox |
| Restore cache condition | Không làm sạch cache; final build dùng assets đã locked-restore và local NuGet cache. Không có clean-restore claim |
| Public API review | **PASS — compiled metadata review trên cả hai targets**; candidate fingerprint `e6735faae42a8eb41c06cdd5d4fd27a87e56dccaee73f579cd4460ef0693db6e`; xem [API baseline](../components/controls/PUBLIC_API_BASELINE.md). Windows test-gate execution vẫn NOT RUN |
| Consumer integration | App UiLab Debug net48/x86 source-mirror cross-build **PASS**; exact consumer gitlink và final build được sở hữu bởi report tích hợp trong MyDmsVn, không phải acceptance của toolkit |

Report này không thay [evidence A3](20261024-a3-bootstrap.md), không đóng điều kiện còn lại của A3 và không chuyển historical PASS/approval sang candidate mới. Không có package/release/SQL work trong scope này.

## Source candidate đã triển khai

| Thay đổi | Source và regression | Giới hạn hiện tại |
|---|---|---|
| Disabled action | [BootstrapButtonRenderLogic](../../src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapButtonRenderLogic.cs) dùng secondary surface/normal text cho disabled state; [BootstrapButtonTests](../../tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapButtonTests.cs) bảo vệ readability và phân biệt enabled Secondary outline | Source/compile evidence; chưa xác nhận appearance native Light/Dark hoặc click/keyboard inertness trên candidate |
| Modal Font lifetime | [BootstrapModalHeader](../../src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalHeader.cs) tái sử dụng Font do toolkit sở hữu khi typography không đổi; [BootstrapModalThemeDpiAccessibilityTests](../../tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapModalThemeDpiAccessibilityTests.cs) có regression vòng đời/theme | Chưa chạy native measurement/painting, theme switch khi modal đang mở hoặc disposal |
| Multiline read-only | [BootstrapTextBox](../../src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTextBox.cs) thêm `Multiline`, `WordWrap`, `ScrollBars`, multiline-only layout và native accessibility metadata; [BootstrapTextBoxMultilineTests](../../tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTextBoxMultilineTests.cs) cùng defaults regression | Chưa chạy selection/copy/scroll/caret/focus/accessibility/Designer trên Windows; compiled API delta đã được review, Windows test gate vẫn NOT RUN |

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
| Windows test discovery | **NOT RUN / BLOCKED** | Chưa có discovered count trên Windows candidate |
| Windows test execution | **NOT RUN / BLOCKED — 0 executed** | Không có pass/fail/skip totals hoặc TRX của candidate này |
| Gallery/Designer/physical DPI/native Owner review | **NOT RUN / PENDING** | Cross-build không xác nhận các hành vi này |

Lượt kiểm tài liệu còn tái hiện hạn chế sandbox khi chạy `/tmp/mydmsvn-dotnet/dotnet --version` (exit 134, lỗi process information); không dùng lần thất bại đó làm bằng chứng SDK thiếu hoặc source không build. Báo cáo cross-build thành công ở trên dựa trên lượt chạy ngoài sandbox đã ghi.

## Compiled public API review

Execution owner đã tái tạo baseline `c232a4c` với fingerprint `5016acec10c7efdafe03387de2c8f182a2e96c49f473f1bcb8bc6e5932dfa6cb` trên cả hai targets. Candidate fingerprint là `e6735faae42a8eb41c06cdd5d4fd27a87e56dccaee73f579cd4460ef0693db6e`, gồm 182 exported types và 1.748 dòng surface; delta chỉ gồm ba property `BootstrapTextBox.Multiline`, `WordWrap`, `ScrollBars`. `PUBLIC_API_BASELINE.md` và `ApprovedV1Fingerprint` đã được cập nhật sau review. Đây là compiled metadata evidence; chưa chạy `Phase16PublicApiBaselineTests` trên Windows và không chứng minh native behavior.

Workflow toolkit `.github/workflows/build.yml` cấu hình `windows-2022` để build, không chạy native tests. MyDmsVn chưa có `.github/workflows`; không có current-candidate CI job/result được ghi trong report này. Cấu hình workflow hoặc Linux cross-build không thay Windows/native execution.

## Windows/native còn cần thực hiện

Theo [TESTING](../TESTING.md), discovery rồi chạy có chọn lọc trên cả `net48` và `net8.0-windows`, serialized STA, finite pumping và hang diagnostics 5 phút. Nhóm source liên quan gồm Button, Modal theme/lifetime, TextBox/multiline/accessory/validation, Select accessibility và adapter `BootstrapTextBoxEditorTests`; mở rộng chỉ cho rủi ro thực tế ở shared editor/handle contract. Actual commands, discovered/executed/pass/fail/skip và TRX summary vẫn **PENDING**.

Native review cần Light/Dark, physical 100%/125%, normal/minimum bounds, disabled actions, đổi theme khi modal mở và multiline chọn/sao chép/cuộn/focus/accessibility. Ghi physical scale và window DPI riêng; Designer và native UIA/clipboard cần quan sát đúng môi trường. Chưa có screenshot, native acceptance hoặc Owner decision mới được ghi ở lượt này.

## Các field phải hoàn tất khi tích hợp

- **RECORDED:** source/test tree hashes ở trên xác định đúng compiler candidate; upstream commit là commit chứa report. Nếu source/test đổi, cần evidence mới cho delta đó.
- **PENDING:** Windows execution của API test gate; compiled delta/fingerprint đã được review như ghi ở trên.
- **CONSUMER OWNER:** report `docs/evidence/ui-labs/20261028-business-compact-exceptions.md` trong MyDmsVn ghi exact gitlink/source/test candidate và final consumer build.
- **PENDING:** actual Windows discovery/execution totals, native/Designer/DPI observations và dated Owner decision.

Cho đến khi các bằng chứng tương ứng tồn tại, ba ngoại lệ vẫn **OPEN** và Owner native approval vẫn **PENDING**. Source implementation/cross-build không cấp product, package hoặc release acceptance.
