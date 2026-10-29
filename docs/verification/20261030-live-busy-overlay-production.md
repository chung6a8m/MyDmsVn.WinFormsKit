# BUS-03 — Production live child-layer backdrop

Candidate hiện hành sau review minimum Cancel width được ghi tại mục **Sửa Important sau review BUS-03** bên dưới. Các bảng baseline52/52/22 và native8 runs ở phần đầu thuộc commit `ed9da03ae6da9c4757beacc5b590fd42edde189c`, không tự chuyển thành PASS của candidate sửa layout.

Ngày thực thi baseline: **30/10/2026**. Candidate native baseline: `ed9da03ae6da9c4757beacc5b590fd42edde189c`, dựa trên `14ed7cced62270c8bba879d94919933efcc053ae`; runtime marker `14ed7cc+BUS03-final-v4`. [BUS-01](20261029-live-busy-overlay.md) giữ lịch sử probe riêng. Evidence này chạy production `BootstrapBusyOverlayHost`, không dùng backdrop/filter probe.

## Kết quả và phạm vi

**PASS BUS-03 tại physical 100%/actual96 DPI:** production live trên net48 x64/x86 và net8 x64; hai negative-manifest EXEs báo OpaqueFallback. Fallback không được tính live PASS. Giữ BootstrapBusyHost cũ, controller BUS-02 và targets/assembly identities. BUS-04/05/06 và Owner acceptance đầy đủ vẫn mở.

Môi trường: Windows `10.0.26300.0`, SDK `8.0.427`, `.NET Framework 4.8.10008.0` / `.NET 8.0.33`, interactive desktop, một monitor DISPLAY1, actual window DPI96. Negative net48 báo OSVersion6.2 vì thiếu supportedOS; compatibility context của executable quyết định manifest capability production, không suy live từ version đó.

## Tests và test-first

| Final check | Discovered | Executed | Pass | Fail | Skip |
|---|---:|---:|---:|---:|---:|
| `FullyQualifiedName~BootstrapBusy`, net48 x64 | 52 | 52 | 52 | 0 | 0 |
| Cùng filter, net8 x64 | 52 | 52 | 52 | 0 | 0 |
| Capability + queued-input fixtures, net48 x86 | 22 | 22 | 22 | 0 | 0 |

Hai `--list-tests` liệt kê52 cases mỗi TFM; NUnit execution ghi discovered52/52. X86 log ghi `BUS-03 queued input process bits=32`; launcher VSTest x64 không phải bitness testhost. Suite gồm7 regressions BootstrapBusyHost cũ trên mỗi TFM.

RED trước implementation: `ZeroOpacity_BlocksQueuedContentMouseButAllowsCancelAndOutsideSibling(0/0.35/1)` trên scaffold net8: **3 executed/0 pass/3 fail/0 skip**, Content nhận MouseUp1 thay vì0. Sau filter production GREEN3/3. Regression custom alpha theme Surface RED1/1 vì card alpha20; chuẩn hóa foreground RGB opaque rồi final PASS. Chỉ hai BUS-02 fixtures kiểm fallback lifecycle được pin bằng injected UnsupportedNative; actual-mode native helper assertions giữ strict.

Một run trung gian33 cases mất1m33s nhưng kết thúc bình thường:31 pass/2 scaffold-mode assertion fail, không timeout/kill/dump. Đo4000 ký tự không ngắt dòng tại width rất nhỏ quá chậm; final400 ký tự vẫn vượt viewport, custom font20pt, zero/tiny/large clients, Light/Dark và synthetic96/120/144/192 DPI. Final suites khoảng4giây/TFM. Synthetic geometry không thay physical-DPI proof.

Capability tests kiểm Windows8/8.1/10 IDs, count/bounds/alignment, missing declaration, size/query errors, late alpha failure, per-host no retry, callback/token/filter continuity và consumer exception propagation. Theme/resize/opacity test giữ Content HWND, selection/focus và một delay generation.

Final diff review phát hiện theme refresh gọi alpha hai lần. Regression `ThemeRefreshAppliesNativeAlphaExactlyOnce` RED1/1 với actual2, expected1; bỏ call trước palette mutation, giữ mode callback ở bước cuối, rồi GREEN trên hai targets/x86. Source hash và native matrix dưới đây thuộc candidate sửa này.

## Exact production EXEs

Normal: helper deadline30000ms, outer watchdog45000ms. Injection `BUS_TIMEOUT_PROOF=1`: deadline3000ms, outer10000ms; modal không auto-close chỉ mở sau production presentation/filter. Final không có parent kill.

| Output dưới `artifacts/bus03/` | TFM/bitness | Actual mode | Exit | Cleanup ms | Kết quả |
|---|---|---|---:|---:|---|
| `candidatev4-positive-net48` | net48 x64 | NativeLayered | 0 | 13421 | PASS live |
| `candidatev4-positive-net8` | net8 x64 | NativeLayered | 0 | 18041 | PASS live |
| `candidatev4-positive-net48-x86` | net48 x86 | NativeLayered | 0 | 13219 | PASS live |
| `candidatev4-negative-net48` | net48 x64 | OpaqueFallback | 0 | 7910 | PASS fallback |
| `candidatev4-negative-net8` | net8 x64 | OpaqueFallback | 0 | 7645 | PASS fallback |
| `candidatev4-timeout-positive-net48` | net48 x64 | production before injection | 1 expected | 3037 | PASS timeout/cleanup |
| `candidatev4-timeout-positive-net8` | net8 x64 | production before injection | 1 expected | 3069 | PASS timeout/cleanup |
| `candidatev4-timeout-positive-net48-x86` | net48 x86 | production before injection | 1 expected | 3052 | PASS timeout/cleanup |

**8 selected/discovered,8 executed,8 expected outcomes PASS,0 unexpected failure,0 scenario skip.** Mỗi run zero remaining windows và6/6 released print hooks. Helper `filters=0/0` chỉ đếm helper-owned filters: probe filter đã xóa. Production install/remove được kiểm qua host fixtures, release/hide/dispose/handle-loss và alpha-zero queued input; không diễn giải0/0 thành BUS-04 resource-soak proof.

Cả5 normal runs dùng real SendInput tại alpha0/89/255: ContentClicks0, CancelClicks1/2/3, OutsideClicks1/2/3. Assertions kiểm actual mode, layered style đúng mode, không WS_EX_TRANSPARENT/owned DC, sibling topology card→backdrop→Content và opaque card pixel. Positive runs giữ cùng HWND/token qua TextBox/TreeView/progress và color patch updates; desktop pixels text/tree đổi ít nhất34 và progress ít nhất864 giữa frames. Higher sibling, ancestor clip/scroll, TabPage hide/show, owned modal, minimize/restore và một-monitor move PASS tại96 DPI; không đóng toàn lifecycle/focus/capture matrix BUS-04.

Alpha-only Content WM_PRINT delta0, unexpected print0; incidental native themed background total13 live/6 fallback. Production không gọi capture/WM_PRINT. Helper-only capture/stack classifier vẫn có giới hạn heuristic BUS-01; không dùng classifier đơn lẻ làm proof không snapshot.

## Commands đã chạy

GUI và builds chia sẻ obj chạy tuần tự từ root, desktop exclusive. `build.ps1 -Scope Controls` final và5 native builds exit0/0 warnings/errors.

```powershell
.\build.ps1 -Configuration Release -Scope Controls
# Lặp net48 và net8.0-windows:
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f <tfm> --list-tests --filter "FullyQualifiedName~BootstrapBusy" --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f <tfm> --no-build --filter "FullyQualifiedName~BootstrapBusy" --blame-hang --blame-hang-timeout 5m --logger "console;verbosity=normal"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapBusyOverlayCapabilityTests|FullyQualifiedName~BootstrapBusyOverlayNativeTests" --blame-hang --blame-hang-timeout 5m --logger "console;verbosity=normal" -- RunConfiguration.TargetPlatform=x86
```

Native build recipe:

```powershell
$project = 'tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.csproj'
$negativeManifest = Join-Path (Get-Location) 'tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/app.no-layered.manifest'
dotnet build $project -c Release -f net48 --no-restore -o artifacts/bus03/bin/positive-net48
dotnet build $project -c Release -f net8.0-windows --no-restore -o artifacts/bus03/bin/positive-net8
dotnet build $project -c Release -f net48 --no-restore -p:PlatformTarget=x86 -o artifacts/bus03/bin/positive-net48-x86
dotnet build $project -c Release -f net48 --no-restore "-p:ApplicationManifest=$negativeManifest" -o artifacts/bus03/bin/negative-net48
dotnet build $project -c Release -f net8.0-windows --no-restore "-p:ApplicationManifest=$negativeManifest" -o artifacts/bus03/bin/negative-net8
```

Mỗi output chạy `MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe --scenario busy-overlay-live <absolute candidatev4-positive-* directory>` hoặc `busy-overlay-fallback <absolute candidatev4-negative-* directory>`. Injection chạy lại3 positive EXEs với env trên, output `candidatev4-timeout-positive-*`. Launch ProcessStartInfo/UseShellExecute=false, bounded WaitForExit, assert exits/markers. Default positive outputs được rebuild bằng Controls build sau negative.

Một attempt trước final dùng relative manifest khiến dependency DLL build lỗiCS1926; final dùng absolute manifest. Một attempt Hidden launch trước final không đưa Form lên desktop thật, uncovered click fail; final dùng GUI process bình thường phục vụ native proof. Không gộp các attempts đó vào final PASS. Embedded RT_MANIFEST#1 được helper log/đối chiếu QueryActCtx: positive count1/Windows10 ID, negative count0; offset8/stride32 thực trên x86/x64.

## Candidate source và artifacts

SHA-256 working source UTF-8 BOM/CRLF được build/chạy; Markdown UTF-8 không BOM. Commit ed9da03 xác định baseline native; mục sửa review phía dưới xác định candidate layout mới.

| Source | SHA-256 |
|---|---|
| `Controls/BootstrapBusyOverlayHost.cs` | `5BBAB6597A2CE12E6E6B2FD51FDA3E83C3123EEA4633D2278A88E3706E450E2D` |
| `Compatibility/BootstrapBusyLayeredWindow.cs` | `C6CBD814252C8DFCE1E23F801882AEC82EE5B409EBB6E3DDAE761A591485710D` |
| `Controls/Internal/BootstrapBusyLayeredBackdrop.cs` | `9DDBACB131669B22CAB7151150394D80536CD8713B58880F2CA20139FD452824` |
| `Controls/Internal/BootstrapBusyOverlayInputFilter.cs` | `3ECCD8AFE80E2B8556B6876000DA9B5299336F7DDD63FC6597355BA84E112A1A` |
| `Controls/BootstrapBusyOverlayLayoutLogic.cs` | `4F3DA5ACD37351E7D093D03BD58ACB1DC65101C07CA82F93AC7F70DEDE78BADC` |
| helper `BusyOverlayScenario.cs` | `E7471D47430FBED13DCC3B932C7ADC6111125CEE1CDD7D0840B196252ABEECA3` |

| Exact EXE dưới `artifacts/bus03/bin/` | SHA-256 |
|---|---|
| `positive-net48/...VisualStyleHost.exe` | `FB580085E993F61E1561DE3F1640294B007D72E60A13EE4EB60CFB514BF444A3` |
| `positive-net8/...VisualStyleHost.exe` | `4E126028402A2F8773F6DD372559590D667B95C82CC68D349108AFF7E2503262` |
| `positive-net48-x86/...VisualStyleHost.exe` | `750B9B5E7C173D8A710F200E4F4C78D6EB3E9CC354055D695EC89F6F91A61DFD` |
| `negative-net48/...VisualStyleHost.exe` | `217F5766DC8C55194EECA4311809435F290457D2801E8ACD0EBF97DDA7E64D9F` |
| `negative-net8/...VisualStyleHost.exe` | `73CA3F9A17A17E7911FAB6DCA01A89C5582F5321B47C07297D7159D5F7A81B7D` |

Raw PNG/logs/binaries ignored/local tại `C:/Users/pdchung/.codex/worktrees/busy-overlay-bus01-03/WinFormsKit/artifacts/bus03/`; retention đến khi worktree/artifacts được dọn. **Chưa upload, chưa có durable URL.** Positive directories có alpha0/89/255, live0..3, initial/higher-sibling/ancestor-scroll/owned-modal PNG và result.txt. Đã nhìn ảnh alpha89 production net8. Không commit raw artifacts. Net8 apphost hash không đại diện managed assemblies; source manifest/output directory cùng candidate mới là identity code, không suy từ apphost riêng.

## Giới hạn còn mở

Native Light96 DPI đã chạy; Dark có palette/in-place unit tests nhưng native Dark matrix chưa chạy. Physical125/150/200%, mixed-DPI (không có monitor DPI khác), Windows8/8.1 thực, SourceGrid/third-party rendering, resource soak100cycles, toàn capture/focus/UIA/reparent/ancestor edge cases, gallery prerequisites và Owner approval đều **NOT RUN hoặc thuộc BUS-04/05/06**. Build All/broad suite/API fingerprint review chưa chạy ở task03; protected overrides mới OnFontChanged/OnDpiChangedAfterParent cần audit BUS-06. Không tuyên bố feature acceptance hoàn tất.

Căn cứ: [QueryActCtxW](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-queryactctxw), [compatibility information](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-activation_context_compatibility_information), [supportedOS IDs](https://learn.microsoft.com/en-us/windows/win32/sbscs/application-manifests), [thiết kế mục4–5](../design/20261029-tabs-hottrack-live-busy-overlay.md).

## Managed assemblies của final candidate

| Output dưới `artifacts/bus03/bin/` | SHA-256 core DLL |
|---|---|
| positive-net48 | E4AE55573831E29FDFA800AB478B1640F4C9C484DE864D179A338337137D75B5 |
| positive-net8 | C42FCB473E250811057301F5F1C6ACB138B6F7C2B6BA374527A81A5511556F43 |
| positive-net48-x86 | E4AE55573831E29FDFA800AB478B1640F4C9C484DE864D179A338337137D75B5 |
| negative-net48 | 6BCB69B37A7115A3F4288CC800349DB36778245558AF1ACA34C4069842561E07 |
| negative-net8 | 656C5C603CBA5C039B98C24CF68D58B9C56F41AD31ABAF60C01B348AB986AF00 |

Managed helper DLL net8 SHA-256: positive `231FB850043304F07B675F04D6C865952E5ECBD4B2B3AA2B6EFA28B5FF821839`, negative `EC590EAAEFE22FB9B8DA7DF54586D26DF586B8D481BD763B9F3A249CBA3C9A35`. Final native v4 rebuild sau theme regression và Controls rebuild đều exit0; zero helper processes sau execution.

## Sửa Important sau review BUS-03

Ngày30/10/2026. Candidate sửa: base `ed9da03ae6da9c4757beacc5b590fd42edde189c` cộng commit chứa mục này, marker executable `ed9da03+BUS03-layout-review-fix`. Chỉ production layout logic và layout tests đổi; host/controller/capability/backdrop/filter/helper không đổi. SHA-256 layout mới `30AC227B835E1ABB8B90068F48463EC88E157FA92651576FC980E6AC4F8C39F1`; layout tests `2785AA1124899FC7BA3F3AEB714466FC2081FD1A585897501D5AB8E3EADA9D33` (UTF-8 BOM/CRLF).

Review phát hiện spacing cố định làm Cancel width56 trong client120×220/96 DPI dù client đủ caption120 tại custom font20pt. Sửa đo desired width trước, rồi giảm horizontal margin/padding theo phần client còn lại; vertical padding/Cancel row độc lập. Cancel giữ min(client width, desired width) khi nằm trong cap480 scaled; caption lớn hơn chính cap vẫn clip theo cap. Card luôn nằm trong client, không negative bounds. Không đổi BUS-04/05/06.

RED: thêm `CancelWidthShrinksSpacingBeforeClippingCaptionAtNarrowClientThresholds`, chạy net8 trước sửa: discovered4/executed4/pass0/fail4/skip0, exit1. Fail đầu tiên từng DPI ở client120 logical pixels:96 DPI expected120/actual56;120 DPI expected149/actual70;144 DPI expected176/actual84;192 DPI expected235/actual112. Width119 trước đó đi qua assertion; fail tại120 dừng phần còn lại của từng RED case.

GREEN focused layout net8: discovered8/executed8/pass8/fail0/skip0, exit0, 1.8975giây. Regression mới kiểm toàn bộ119/120/143/144 tại96/120/144/192 DPI, hai caption Cancel/Hủy tác vụ, custom font20pt có scaling, tổng32 tổ hợp; width phải bằng min(client, desired), không giảm khi client rộng hơn, card/button containment và cap480 vẫn đúng. Bốn long-message/font/zero/tiny/Light/Dark cases cũ vẫn PASS.

Final affected suites: `FullyQualifiedName~BootstrapBusy`, net48 x64 discovered56/executed56/pass56/fail0/skip0, exit0,4.1054giây; net8 x64 discovered56/executed56/pass56/fail0/skip0, exit0. Gồm7 regressions host cũ/target. Không tái dùng52-case PASS trước sửa. Các native unit tests log process bits64. X86 focused NUnit suite không chạy lại ở patch geometry này; x86 executable production bên dưới đã build/chạy lại.

Commands RED/GREEN/final, tất cả giữ hang diagnostics5m:

```powershell
# RED, trước source fix:
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~CancelWidthShrinksSpacingBeforeClippingCaptionAtNarrowClientThresholds --blame-hang --blame-hang-timeout 5m --logger "console;verbosity=normal"
# GREEN focused:
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~BootstrapBusyOverlayLayoutTests --blame-hang --blame-hang-timeout 5m --logger "console;verbosity=normal"
# Final: lặp net48 và net8.0-windows:
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f <tfm> --filter "FullyQualifiedName~BootstrapBusy" --blame-hang --blame-hang-timeout 5m --logger "console;verbosity=normal"
.\build.ps1 -Configuration Release -Scope Controls
```

Đã build/chạy lại5 normal helper EXEs theo recipe baseline ở trên nhưng output build là `artifacts/bus03/reviewfix-bin/<kind>`, scenario output `artifacts/bus03/reviewfix-<kind>`. Positive net48 x64/x86 và net8 dùng busy-overlay-live; hai negative manifests dùng busy-overlay-fallback, absolute manifest path, x86 PlatformTarget=x86, outer watchdog45000ms. Toàn bộ tuần tự trên desktop exclusive. Cả5 builds và final Controls build exit0/0 warnings/errors. Final wrapper exit0; zero remaining helper processes.

| Output result.txt dưới artifacts/bus03 | Runtime/bitness | Mode | Exit | Cleanup ms |
|---|---|---|---:|---:|
| reviewfix-positive-net48 | net48 x64 | NativeLayered | 0 | 13287 |
| reviewfix-positive-net8 | net8 x64 | NativeLayered | 0 | 16201 |
| reviewfix-positive-net48-x86 | net48 x86 | NativeLayered | 0 | 16209 |
| reviewfix-negative-net48 | net48 x64 | OpaqueFallback | 0 | 7707 |
| reviewfix-negative-net8 | net8 x64 | OpaqueFallback | 0 | 7725 |

Final native enumeration5 selected/discovered/5 executed/5 PASS/0 FAIL/0 skip. Actual96 DPI, alpha0/89/255 ContentClicks0/Cancel1-2-3/Outside1-2-3, same HWND live updates, opaque foreground/clipping/modal và cleanup đều PASS; live print total13/fallback6, alpha-only print delta0. Bảng này là exact-candidate proof mới. **Helper giữ host width650px nên không trực tiếp chứng minh caption ở119/120/143/144; các ngưỡng đó được kiểm bằng regression geometry.** Timeout injections3 runs chỉ là evidence baseline ed9da03, NOT RUN lại trên patch; helper path không đổi. Không tính chúng vào5 PASS hiện hành.

Môi trường/giới hạn giữ như baseline: Windows10.0.26300.0, SDK8.0.427, .NET Framework4.8.10008.0/.NET8.0.33, một monitor96 DPI. Native Dark/physical125/150/200/mixed-DPI, durable upload/Owner approval và BUS-04/05/06 tiếp tục mở. Raw reviewfix artifacts local/ignored, retention theo worktree; không có durable URL mới.

### Binary manifest của candidate sửa layout

| Output dưới artifacts/bus03/reviewfix-bin | EXE SHA-256 | Core DLL SHA-256 |
|---|---|---|
| positive-net48 | 834CEBE968894639DE5E1222D0F889FA81DACB77F0C6567577F27AA49A0FD9DF | 16962C1A470981AF9420479C09745CC903C05DA9496E5E841FF428FBD479F2B7 |
| positive-net8 | C07DA3B6FE0EE07F89454C4ACF20AFC2266781E338FF5991D7713AB20EAA5E26 | C3CB1A0A53D872FF773F86A026871B39A8A5699C275308E7BC1DC57B63BC954F |
| positive-net48-x86 | 4368B1D57C21559463D9AA1F822D81402979F3C13DCEBD109120434CAF4147AE | 16962C1A470981AF9420479C09745CC903C05DA9496E5E841FF428FBD479F2B7 |
| negative-net48 | 6232796DDEAACD8A195D8F5F5B947DA7B6EBADBBCCB893F8401356E704F1090D | A4B1108BAB21B781D965DF6CB3B47504A6DF670839CB9D67093A38C197F6EFE7 |
| negative-net8 | AC42472A3AEA63E2940EE5400378CC0CE0544E532CE5ED7B0FE2FDAF70A702A2 | E62889D328699E379FC54C8A00739757FF0D518475F0A8AEB6BE1905A8FDDC0A |

Managed helper DLL net8: positive F594D22F21A25C5137E5C803984F68A921A4D3D20E55B16840C829213A855353, negative 52D5D801FB821AF650553CEA7D13BF2ED0981212AB5AC7CA90208AE28E765466.
