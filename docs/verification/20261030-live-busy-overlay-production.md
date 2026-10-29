# BUS-03 — Production live child-layer backdrop

Ngày thực thi: **30/10/2026**. Candidate: `14ed7cced62270c8bba879d94919933efcc053ae` cộng commit chứa report này; runtime marker `14ed7cc+BUS03-final-v4`. [BUS-01](20261029-live-busy-overlay.md) giữ lịch sử probe riêng. Evidence này chạy production `BootstrapBusyOverlayHost`, không dùng backdrop/filter probe.

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

SHA-256 working source UTF-8 BOM/CRLF được build/chạy; Markdown UTF-8 không BOM. Commit chứa report xác định candidate Git.

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
