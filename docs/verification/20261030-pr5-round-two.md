# PR #5 — xác minh sửa finding vòng hai

Ngày 30/10/2026; base `be7b1936911ac3d77b9182f6e1b1c7665538f727`. Candidate là base cộng diff source/test được ghi bằng SHA-256 bên dưới, không suy kết quả từ candidate trước. Finding mới nhất `4418855486` trong `artifacts/sdd/bus/pr5-round2-findings.json` nói về scrollbar dọc cũ làm message mới wrap thừa dòng. Không đổi public API, targets, host cũ hoặc resource thresholds.

## RED/GREEN và checkpoint đầu

Đọc trực tiếp logs local: `pr5-round2-red.log` có 2 discovered/executed, 0 PASS/2 FAIL/0 SKIP tại assertion scrollbar cũ vẫn còn, widths 400/320. `pr5-round2-green.log` có 3 discovered/executed/PASS, 0 FAIL/0 SKIP, gồm hai cases mới và long-message regression hiện hữu. Đây là focused checkpoint trước full verification.

Checkpoint đầu dùng host SHA-256 `A6ADC356F1DF492AE68F684783608ACD24A679DEBBEAA51F35C92BD91DD52512`, HostTests `8F742C39F479E585ADC0DBEFE59A24C3CDD77ED6ACD88733A92E1B0C1DF42605`. Build All Release locked warm restore PASS, 0 warnings/errors, 4.95 giây. Fresh discovery 2619 cases mỗi TFM. Full net48: 2619 discovered, 2617 executed, 2616 PASS/1 FAIL, 2 existing Explicit excluded, 1.5685 phút. Failure duy nhất ở case width400: dữ liệu tìm bằng lặp `abc` không tạo được message thỏa tiền điều kiện sau broad suite (`fitting == null`, line655), trước assertion production. Width320 và long-message regression PASS. Không tính checkpoint này là full PASS; log/TRX gốc được giữ local ignored.

Fixture cuối dùng font `Segoe UI` 9 cho cả hai Forms và từ `i` để tìm message với bước rộng nhỏ hơn. `pr5-round2-green-stable.log`: net48 2 discovered/executed/PASS, 0 FAIL/SKIP. Đặt lại riêng clamp cũ trong production để kiểm regression: `pr5-round2-red-stable.log` có 2 discovered/executed, 0 PASS/2 FAIL/0 SKIP tại đúng assertion scrollbar. Sau đó bỏ clamp, khôi phục source cố định trước final build. SHA-256 final HostTests `F7020FCCA72CA5E5CBE4A8486F5DBF666C2CC1305B44BC3C348A18A8620BD472`; host hash không đổi. Test so sánh long→fitting trên cùng host với fresh host, ba lần `PerformLayout()`, cả hai scrollbar, scroll origin, Cancel selectable và token cũ lộ lại khi release token mới.

## Commands final candidate

Từ repository root, chạy tuần tự; source không đổi giữa build và execution:

```powershell
.\build.ps1 -Configuration Release -Scope All
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f <net48|net8.0-windows> --no-build --no-restore --list-tests --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f <tfm> --no-build --no-restore --blame-hang --blame-hang-timeout 5m --results-directory artifacts/sdd/bus/pr5-round2-final-testresults --logger 'console;verbosity=normal' --logger 'trx;LogFileName=full-<tfm>.trx'
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f <tfm> --no-build --no-restore --blame-hang --blame-hang-timeout 5m --filter 'FullyQualifiedName~NativeFirstClickFromAnotherControlKeepsPopupAndSearchFocus' --results-directory artifacts/sdd/bus/pr5-round2-final-testresults --logger 'console;verbosity=normal' --logger 'trx;LogFileName=explicit-<tfm>.trx'
```

Final build All: PASS, 0 warnings/errors, 4.87 giây. Helper source `BusyOverlayScenario.cs` SHA-256 `533B521AE919E5E48FB9F7CBFEF9F198A69EECC2C5156210386E0719170345E9`; `BusyOverlayUiaClient.cs` `132A58606A965D2D0EEF6AF7BA72A1E3386EE1BABAABC570DFEADE2E3BB960F7`.

Sửa production bỏ clamp theo `_messageViewport.ClientSize.Width` đang phản ánh extent của message cũ; wrapping tiếp tục dựa trên layout/message mới và chỉ dành scrollbar width khi message mới thực sự vượt viewport. `AutoScrollPosition = Point.Empty` giữ vai trò reset vị trí, không được dùng như thao tác xóa extent. Đối chiếu [WinForms AutoScrollPosition tại source pin](https://github.com/dotnet/winforms/blob/e4ede9b8979b9d2b1b1d4383f30a791414f0625b/src/System.Windows.Forms/src/System/Windows/Forms/ScrollableControl.cs#L130-L146); native regressions là bằng chứng behavior.

| Final Controls run | net48 | net8.0-windows |
|---|---|---|
| Discovery | 2619 | 2619 |
| Full default | 2617 executed/PASS; 0 FAIL; 2 Explicit excluded | 2617 executed/PASS; 0 FAIL; 2 Explicit excluded |
| Explicit supplement | 2 discovered/executed/PASS; 0 FAIL/SKIP | 2 discovered/executed/PASS; 0 FAIL/SKIP |
| Tổng coverage sau supplement | 2619/2619 executed/PASS; 0 FAIL; 0 chưa chạy | 2619/2619 executed/PASS; 0 FAIL; 0 chưa chạy |
| Thời gian full / Explicit | 1.5368 phút / 4.1377 giây | 1.4326 phút / 3.2196 giây |

Không gộp nhầm 2617 summary của một full command thành 2619. Hai Explicit cases có tên `Native first content click from another control keeps popup and search focus` và `Native first arrow click from another control keeps popup and search focus`. Logs final là `pr5-round2-final-build-all.log`, `pr5-round2-final-discovery-<tfm>.log`, `pr5-round2-final-full-<tfm>.log`, `pr5-round2-final-explicit-<tfm>.log`; TRX trong `pr5-round2-final-testresults/`. Full suite bao phủ cả BootstrapBusyHost cũ, API baseline, TAB và gallery regressions; adapter owner chỉ được build All, không có execution claim cho adapter suite.

## Native matrix mới build

Năm builds tuần tự bằng `dotnet build tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.csproj -c Release -f <tfm> --no-restore -o artifacts/sdd/bus/pr5-round2-bin/<kind>`. Kind `positive-net48-x86` thêm `-p:PlatformTarget=x86`; hai negatives thêm `-p:ApplicationManifest=<absolute repository path>/tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/app.no-layered.manifest`. Tất cả exit0, 0 warnings/errors; từng build log `pr5-round2-build-<kind>.log`.

Chạy exact EXE trong từng output bằng `--scenario busy-overlay-live <absolute output>` cho ba positives và `--scenario busy-overlay-fallback <absolute output>` cho hai negatives. Marker `BUS_CANDIDATE=be7b193+PR5-round2`; xóa `BUS_RESOURCE_PROOF`, nên giữ normal all-scenarios mode, capture/input/UIA/modal và resource soak. Launcher local `run-pr5-round2-matrix.ps1` giữ thiết lập foreground từ script hiện hữu: tạm TopMost, click thật title bar, reset TopMost trước scenario. Scenario deadline60 giây, outer watchdog75 giây; native tests/full suite vẫn giữ hang diagnostics5 phút.

Giữ nguyên helper drain UIA-prefix COM finalizers bằng bounded STA pumping trước warm10, không GC trong measured cycles; 100 measured cycles sau warm10, zero tolerance và settle rule hiện hữu không đổi. Hash helper nguồn ở trên ràng buộc chính xác implementation này. Raw counts và settled counts đều được giữ trong `pr5-round2-native-<kind>/result.txt`; matrix launcher log `pr5-round2-native-matrix.log`.

| Exact helper kind | Bitness / actual DPI | USER warm10 →25/50/75/100 (settled) | GDI mọi checkpoint | Exit / scenario / resource | Cleanup ms |
|---|---|---|---|---|---|
| positive-net48-x86 | 32 / 96 | 48 →48/48/48/48 | 43 | 0 / PASS / PASS | 35012 |
| negative-net48 | 64 / 96 | 48 →48/48/48/48 | 43 | 0 / PASS fallback / PASS | 30225 |
| positive-net48 | 64 / 96 | 49 →49/49/49/49 | 43 | 0 / PASS / PASS | 37535 |
| positive-net8 | 64 / 96 | 49 →49/49/49/49 | 46 | 0 / PASS / PASS | 39921 |
| negative-net8 | 64 / 96 | 49 →49/49/49/49 | 48 | 0 / PASS fallback / PASS | 30647 |

5 discovered/executed, 5 PASS/0 FAIL/0 SKIP tại cấp helper scenario. Ba positives có `NativeLiveGate=PASS`; hai negatives có `NativeLiveGate=NOT APPLICABLE`, không tính fallback thành live PASS. Input alpha0/0.35/1, UIA Cancel/outside Button, higher sibling, ancestor scroll/clip, TabPage hide/show, bounded owned modal, minimize/restore và resource soak đều chạy trong normal mode. Embedded manifest/query context: positives có Windows10 supportedOS và compatibility count1; negatives bỏ supportedOS và count0, mode thực OpaqueFallback. OS string của negative-net48 là `6.2.9200.0` do compatibility context thiếu supportedOS; OS thực từ CIM vẫn `10.0.26300`.

Riêng positive-net48 có raw USER50 tại cycles25/75/100, settle về49 theo rule hiện hữu; cycles50 raw49, warm raw49. Mọi checkpoint khác raw=settled; GDI không đổi. Không chọn minimum, tăng tolerance hoặc đổi helper source. Cleanup5/5 PASS, `expired=False`, `windowsRemaining=0`, hooks7/7. `filters=0/0` là legacy probe counters, không phải số production filter registrations. Kiểm sau matrix không còn process testhost/VisualStyleHost; desktop/build đã release.

SHA-256 của exact EXE mới build/chạy (binary files chỉ local ignored):

| Kind | EXE SHA-256 |
|---|---|
| positive-net48-x86 | `106C75CEBE86795CAE07BCF251304CF21E1EA1F7E1B26886BC4D55AECABD6521` |
| negative-net48 | `398ADE1CAD5B7144BB8572EE0F8E7537BBB02C5AC35090E72E943C6474AE836F` |
| positive-net48 | `785A3417EF715F4C4C464E8B954C40402822497A6A0E4A484F8FF52E35989B0C` |
| positive-net8 | `5C29060AE4814D193ADD9BD101F040ACB4C185A86B55525BD4860810B2D0E171` |
| negative-net8 | `58690B894D861F58255525A385EF8EFF808F2EC0DDF1DEE40D982A4D80B6AF34` |

Production DLL SHA-256: positives net48/x86 `4C1A6BFB307EC46EC49608086E3C4074E52E9EDE87C1F6C798B2F3D04D773AA9`; negative-net48 `7E20EE831895B772CCDDF1E0501DBB17B713D97A86B0F04A919FFD2A659B5334`; positive-net8 `CAE9AA5841A337C1CDECFA67E14885848FD26F2BA344FD66B6AF8418684F00BE`; negative-net8 `703399F9FB60A42D6C63670A9CC4903CD7D0D479A1E9288BDBDE63C654649301`. Net8 helper DLL SHA-256: positive `EA44D0F3E261F7379D8D4A0457C3DCA1D35F7B581AFB806E30DEB1A6D0E6FE96`, negative `852F98156529C29E7D514CA499631EBCE8650B26276EA7148020064BFEF0E5F0`.

## Môi trường và giới hạn

Windows 11 Home Single Language `10.0.26300`, SDK `8.0.427`, .NET Framework `4.8.10008.0`, .NET 8 Desktop `8.0.33`; Release, NUnit x64. SourceGrid exact gitlink `f4e457b43582bf01892f50bdc74aa480531e5944`. GUI/build chạy tuần tự trên desktop tương tác, giữ hang diagnostics 5 phút. Không claim clean restore vì dùng caches sẵn có. Raw logs/TRX/binaries/PNGs nằm trong `artifacts/sdd/bus/`, local ignored, không commit hoặc upload.

Desktop một monitor, `WindowMetrics.AppliedDPI=96` (100%); actual helper window DPI96 trên cả năm runs. Không giả lập DPI cao bằng bitmap hoặc synthetic scaling. Mỗi helper ghi `NOT RUN mixed-DPI: no distinct physical monitor DPI available`; đây là giới hạn môi trường, không là case PASS. Không có durable artifact URL/retention ngoài máy trong task này.

Physical125/150/200, mixed-DPI, SourceGrid consumer acceptance và Owner visual/live approval chưa được xác minh trong task này; không lấy source/full-suite PASS để đóng các gates đó.
