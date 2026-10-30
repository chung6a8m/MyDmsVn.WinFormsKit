# BUS-04 — input, focus và native lifecycle

Ngày 30/10/2026. Candidate BUS-04 đầu: `2e4a8192e457dccb3e97428a930511ba60bd70b0` (base `6880df8b7b58160743d2e4387456ccd8d24a3624`); marker `6880df8+BUS04-verified`. Phần sửa review ở cuối trang sở hữu candidate mới; các bảng trước phần đó là lịch sử của candidate đầu. Chỉ BUS-04; không đổi `BootstrapBusyHost` cũ, namespace/targets/dependencies/vendor hoặc fingerprint. BUS-05/06 chưa triển khai trong task này. Trạng thái acceptance physical125/150/200% và mixed-DPI **NOT RUN / BLOCKED BY ENVIRONMENT**, không dùng synthetic DPI thay thế.

## Thay đổi và phạm vi kiểm

Host ghi focused Content descendant trước physical show, không lấy focus khi show. Cancel/card mất đi chỉ restore khi focus còn trong card và owner đang foreground, target còn selectable/visible/enabled; nếu target không hợp lệ thì native traversal trong owner. Generation guard bảo vệ cả focus callback reenter/pump/show: hide cũ không được che presentation mới.

Filter chỉ chặn queued client/nonclient mouse/wheel tới Content HWND/native descendants. Đăng ký trước ReleaseCapture để callback capture-loss có thể hide/dispose và gỡ đúng registration; chỉ release capture thuộc Content. Không cache HWND, không filter keyboard/UIA/business commands. Content handle loss/dispose hủy pending generation, gỡ filter/dừng spinner; handle tạo lại còn token thì delay150ms. Host theo dõi owner/ancestor reparent và gỡ subscriptions cũ. Owner minimize không được present; deactivate vẫn giữ presentation. Exception trong callback presentation cleanup trước khi tiếp tục phát exception; Begin thất bại không giữ token không được trả cho caller.

22 cases mới trên baseline65: 15 host/controller/lifecycle cases và7 native cases; giữ7 regressions host cũ trên mỗi TFM. Có capture của sibling, capture-loss callback dispose, real mouse-down → show → mouse-up, no stealing focus từ owner khác, old pending callback, Content disposal/handle loss, ancestor reparent/minimize, independent hosts, synthetic DPI notification không reset operation và100-cycle soak. Native helper mở rộng real SendInput TextBox selection/scrollbar click/wheel ở alpha0/89/255, baseline uncovered chứng minh editor/scrollbar thực sự nhận input. Button Content/Cancel/outside assertions cũ giữ nguyên. Queued mouse/nonclient/wheel ngoài subtree và keyboard đi tiếp.

Caller test đóng native ComboBox dropdown trước Begin; command guard đóng cả pending/visible. Pending dùng held delay để UIA subprocess không vô tình biến nó thành visible. Test đếm cả callback attempts và business commands: keyboard/UIA vẫn tới caller, business command bị caller từ chối. Khi release, UIA command hoạt động lại. Cancel kiểm Tab/Shift+Tab, Space/Enter, UIA Invoke và callback dispose. Native modal có real SendInput vào button và auto-close hữu hạn; clipping/higher sibling/tab/restore/owner move tiếp tục kiểm bằng desktop captures. Raw touch/pen không thuộc proof.

## RED và các lần chẩn đoán

- Trước production fix, net8 filter `Lifecycle_`: discovered9/executed9/pass5/fail4/skip0. Trong đó7 cases BUS-04 mới có4 FAIL (focused Cancel biến mất, Content disposed khi pending/visible, capture thuộc Content không release) và3 PASS baseline;2 cases khác thuộc Tree DPI có tên khớp selector, không được gọi là RED của BUS. Log `artifacts/bus04-red.log`.
- Content handle loss: discovered1/executed1/pass0/fail1/skip0; mode giữ OpaqueFallback thay vì None. Log `artifacts/bus04-red-handle.log`.
- Consumer TextChanged exception khi update presentation: discovered1/executed1/pass0/fail1/skip0; filter/spinner/presentation còn hiện. Log `artifacts/bus04-red-failure.log`.
- Self-review sau sửa đầu: focus-restoration Enter callback Begin + fire delay làm hide cũ che operation mới. RED1/1 FAIL, sau generation guard GREEN1/1 PASS (`bus04-red-focus-reentrant.log`, `bus04-green-focus-reentrant.log`). Không relabel BUS-03 đang PASS là RED.
- Lượt đầu net48 có3 UIA FAIL (82/85 PASS). Legacy .NET `System.Windows.Automation` client thấy native Button là Pane/no patterns, cả NUnit lẫn helper có manifest; explicit registration stock managed proxy còn ném NullReferenceException. Không đổi production accessibility để né lỗi. Native `CUIAutomation` client ngoài process dùng strict `GetCurrentPattern(10000)` + `IUIAutomationInvokePattern.Invoke` xác nhận Button type50000 và callbacks trên cả runtime. Net48 dùng Microsoft MSAA Proxy/HWND Proxy, net8 dùng provider WinForms. Client ABI được đối chiếu [Microsoft SDK header tại exact revision](https://github.com/microsoft/win32metadata/blob/9cf94833626504b330c2f616ce71a2d3497cdc76/generation/WinSDK/RecompiledIdlHeaders/um/UIAutomationClient.h); [GetCurrentPattern](https://learn.microsoft.com/en-us/windows/win32/api/uiautomationclient/nf-uiautomationclient-iuiautomationelement-getcurrentpattern). Helper test-only `BusyOverlayUiaClient.cs` dùng process riêng, release COM pointers, bounded pumping và cleanup; không cài provider/filter hỗ trợ vào app.
- Native test setup từng FAIL foreground activation khi chạy sau host lifecycle suite; chỉ test fixture được đặt vị trí on-screen và nâng tạm để click activation thật, trả TopMost=false trước Begin. Không đổi production focus/TopMost. Mouse-up cleanup được pump trước khi rời test để không rò queued input sang case sau. Các lần sửa harness này không được gọi là production RED.

## Resource soak và giới hạn của số đếm

NUnit: warm10 rồi100 chu kỳ,4 batch25; mỗi chu kỳ show/recreate/show/hide/dispose với manual delay, kiểm owned controls/HWND disposed, filter=false, pending=null, delay disposed đúng1 lần, ancestors/owner subscriptions vềzero. Native helper: warm10 rồi100 chu kỳ với delay thật150ms, xác nhận actual NativeLayered hoặc OpaqueFallback mỗi lần show, recreate/hide/dispose và stale HWND/filter/owner cleanup. Không tăng tolerance USER/GDI.

Lượt helper trước settle có FAIL phải giữ lại: net8 USER49→50 tại batch25, GDI46 không đổi; net48 có snapshot USER49→50→49→49→49 ở warm10/25/50/75/100, GDI43 không đổi. Snapshot toàn bộ thread/child/message-only HWND của net48 không đổi class/parent/owner/handle giữa các checkpoint. Chưa xác định loại USER object gây transient+1; **không gọi đây là framework cache đã được chứng minh**, cũng không có bằng chứng tăng đơn điệu. Lượt net8 diagnostic riêng USER49/GDI46 không đổi qua toàn100 nhưng không xóa các FAIL trước đó.

Candidate cuối giữ log **raw** ở từng checkpoint rồi dùng cùng điều kiện quiescence: pump ít nhất250ms, counts ổn định liên tục100ms, tối đa1500ms; không GC, không chọn minimum, không retry whole batch. Sau đó so exact counts với warm10, mọi increase vẫn FAIL; log raw/settled và HWND snapshots được giữ. Helper deadline thường60s vì thêm110 lần delay thật150ms; timeout-injection vẫn3s, outer watchdog75s, NUnit giữ hang diagnostics5m.

## Kết quả cuối

Môi trường: Windows10.0.26300.0, SDK8.0.427, .NET Framework4.8.10008.0 và .NET8.0.33; một monitor, actual window DPI96. Native helper Light, physical100% theo actual96 DPI, không reconfigure monitor trong task. Warm restore/build.

**BUS-04 chưa DONE.** Implementation/tests đã có; resource gate **BLOCKED / flaky**, physical/mixed-DPI **BLOCKED BY ENVIRONMENT**. Không chuyển hai helper FAIL thành PASS chỉ vì phần live/input đã qua.

| Execution cuối | Discovered | Executed | PASS | FAIL | Skip |
|---|---:|---:|---:|---:|---:|
| Busy NUnit net48 x64 | 87 | 87 | 87 | 0 | 0 |
| Busy NUnit net8 x64 | 87 | 87 | 87 | 0 | 0 |
| Exact helper matrix | 5 | 5 | 3 | 2 | 0 |

`build.ps1 -Configuration Release -Scope All` cuối PASS, exit0,0 warnings/errors,5.87s. Hai targets và adapter graph compile; vendor exact gitlink `f4e457b43582bf01892f50bdc74aa480531e5944` không đổi. NUnit net48 12.1851s, net8 12.2665s. X86 NUnit không chạy; x86 helper production đầy đủ đã chạy như bảng dưới. Full Controls broad suite không chạy lại trong BUS-04; không tuyên bố broad-suite PASS.

| Exact output dưới artifacts/bus04 | Mode/bitness | Settled USER warm10→25→50→75→100 | GDI mọi checkpoint | Exit / kết quả | Cleanup ms |
|---|---|---|---|---|---:|
| verified-positive-net48 | NativeLayered x64 | 49→49→49→49→49 | 43 | 0 / PASS | 35563 |
| verified-positive-net8 | NativeLayered x64 | 49→49→49→49→49 | 46 | 0 / PASS | 40527 |
| verified-positive-net48-x86 | NativeLayered x86 | 48→49→49→49→49 | 43 | 1 / FAIL resource | 38063 |
| verified-negative-net48 | OpaqueFallback x64 | 47→48→48→48→48 | 45 | 1 / FAIL resource | 30584 |
| verified-negative-net8 | OpaqueFallback x64 | 49→49→49→49→49 | 46 | 0 / PASS | 30504 |

Raw counts bằng settled counts ở mọi checkpoint trong4 rows đầu. Negative net8 raw warm10 USER50, settled49 sau253ms; các raw/settled batch sau đều49. Settle thực tế250–267ms. Hai failures cuối vẫn tăng+1 qua điều kiện quiescence; HWND sets không đổi, GDI flat, không tăng thêm qua batch50/75/100, loại USER object chưa xác định. Vì thế gate vẫn mở, không gán nguyên nhân cache. Final wrapper exit1 đúng với2 failures; cả5 builds exit0,0 warnings/errors.

Các phần scenario trước soak trên cả5 EXEs đều PASS: embedded manifest/context đúng; alpha0/89/255 ContentClicks0, Cancel/Outside1–2–3; TextBox selection1/2 và scrollbar40 giữ nguyên sau click/wheel; same HWND live updates với3 vùng native, opaque card, higher sibling/clip/tab, owned modal button usable, minimize/restore và owner move96 DPI. UIA Cancel và ordinary Button type50000 Invoke callbacks đều qua; không filter của helper. Fallback giữ mode riêng, không được tính live PASS. Print total16 live/9 fallback (native incidental), không có explicit production capture, alpha-update delta0. Đã nhìn ảnh `verified-positive-net8/alpha-89.png`; không phải Owner visual approval.

Cleanup tất cả5: windowsRemaining0, hooks7/7; không còn helper/testhost process sau matrix. Dòng legacy `filters=0/0` của deadline helper là counter probe cũ không được dùng, **không phải số production filter registrations**; production filter cleanup được assert trực tiếp ở mỗi soak cycle và NUnit. NUnit resource net48 warm USER25/GDI30, cả4 batches USER24/GDI30; net8 warm/batches USER23/GDI28. Không nới tolerance hoặc bỏ raw count bất lợi.

## Commands và artifacts

Các commands chạy tuần tự trên desktop exclusive. Selector cuối được discovery riêng trên từng target, không tính skipped/discovery rỗng là PASS:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f <net48|net8.0-windows> --list-tests --filter FullyQualifiedName~BootstrapBusy --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f <tfm> --no-build --filter FullyQualifiedName~BootstrapBusy --blame-hang --blame-hang-timeout 5m --logger 'console;verbosity=normal'
```

Helper builds: `dotnet build <VisualStyleHost.csproj> -c Release -f <tfm> --no-restore -o artifacts/bus04/verified-bin/<kind>`; positive net48 x86 thêm `-p:PlatformTarget=x86`; negative thêm `-p:ApplicationManifest=<absolute app.no-layered.manifest>`. Chạy đúng `.exe`/net8 apphost với `--scenario busy-overlay-live <absolute output>` hoặc `busy-overlay-fallback`, ProcessStartInfo UseShellExecute=false, outer75s. Embedded manifest và QueryActCtx executable context được ghi ở mỗi result. Không chạy `dotnet host.dll` thay apphost.

Raw PNG/result/build/discovery logs và binary local/ignored dưới `artifacts/bus04/`, full task report dưới `artifacts/sdd/bus/task-4-report.md`. Retention đến khi worktree/artifacts được dọn; **chưa upload, không có durable URL**. Không commit raw artifacts. Các hash bên dưới định danh source/binary, net8 apphost hash riêng không đại diện managed helper/core DLL.

| Source | SHA-256 |
|---|---|
| src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBusyOverlayHost.cs | D9710C8A431BD65FD00E820B395959A1751243A5E8FB8EEE7EB2B00DCB4682FE |
| src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapBusyOverlayController.cs | C728784B71E50A00279092DFC137C6ADCEBD31531E79834C3A9BE460F9F983A1 |
| src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapBusyOverlayInputFilter.cs | B5D0484E540074E8C65F9DAC569E4BB290FD7528F9DB9E5B70B81597E2C34C1A |
| tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBusyOverlayHostTests.cs | 73E3C5070E245F3B6031654D5419050317149570259B7936F642599626BB8035 |
| tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBusyOverlayNativeTests.cs | 9038BF2199D1A1B7267DD4B7C356372DD5F441CBA1182A9B08092FD5C422DE59 |
| tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/BusyOverlayScenario.cs | DD1960A0C3C1A1F6DD29275228098997AA7C275FA5F6BEFC9ECF244A74B0F422 |
| tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/BusyOverlayUiaClient.cs | 132A58606A965D2D0EEF6AF7BA72A1E3386EE1BABAABC570DFEADE2E3BB960F7 |

| verified-bin output | EXE SHA-256 | Core DLL SHA-256 |
|---|---|---|
| positive-net48 | 04A4477CE26FA3FAA5DC550B8C33013C06E39932B1D7C6F6046DB41A02DFF2F2 | 7BD217B889D7A43432BC1E5B0F8CB4C1BC711776030BA5C42CFDD0D61967C292 |
| positive-net8 | 5D1DB68CF18709F82897558C95513AA28822F1C5580F69C94D8BE9AA8AD5D0A7 | 0D99709FF2A0A7312EFC549A46D0E92D9498D44AE34FE16DC25B07B0CBFAB15E |
| positive-net48-x86 | 536CA5526B2E42F5130E8D14CC2305B3068B0038CC6305AA0401C304EA58D159 | 7BD217B889D7A43432BC1E5B0F8CB4C1BC711776030BA5C42CFDD0D61967C292 |
| negative-net48 | 100E996B5F989341ED23D3E543DF174663625A308A75FFC2E2A2C552CC2DFF28 | 7F821387C5D23D90631D8C716A485A6B7B1B8E89EED7E8A046031A70B7E8184C |
| negative-net8 | ABF2E489851743861100633F353E6101D18D7DD545B3AE8D1417D13B44310E35 | 80E197F067E7F5ED429574965EB69948EF5961F1C2AB1A3F4B67620EDF2E13EB |

Managed helper DLL positive-net8: 154511B0EAE248EBF3418DD3339C3C2B280F5B90596895634B8ECDCB5D1BB02C.

Managed helper DLL negative-net8: 1E3DA577DC1CCB60ADC55DB168BC53196D2C1515E84A0F0010106CC646587C51.


## Acceptance còn mở

Physical125/150/200% và mixed-DPI **NOT RUN / BLOCKED BY ENVIRONMENT**: chỉ có monitor96 DPI. Synthetic DPI notification và owner move trong cùng DPI không chứng minh mixed-DPI. Native Dark matrix, SourceGrid/third-party controls, Owner visual approval, durable artifacts, demo BUS-05 và fingerprint/full acceptance BUS-06 còn mở. Không nhận historical full-suite PASS; full Controls suite từng có failures ngoài BUS ở candidate trước, không suy kết quả mới từ đó. Không triển khai demo hay sửa API fingerprint trong BUS-04.

## Sửa hai Important về HidePresentation sau review

Ngày30/10/2026. Candidate mới: base `2e4a8192e457dccb3e97428a930511ba60bd70b0` cộng commit chứa mục này; marker `2e4a819+BUS04-hide-review-fix`. Chỉ sửa hide/controller exception handling, HostTests và diagnostic trong helper hiện có; BusyHost cũ, input filter và API public không đổi. BUS-05/06 không sửa.

Hai findings đã tái hiện: VisibleChanged của card/backdrop reenter Begin/fire delay làm hide cũ ghi đè presentation mới; callback ném lỗi bỏ dở backdrop/mode cleanup. Hide mới chạy từng bước qua generation guard, tiếp tục best-effort cleanup sau lỗi của generation đang dọn, nhưng không đụng generation mới. ExceptionDispatchInfo giữ exception đầu tiên/stack; nếu presentation đã ném lỗi trước thì controller không để exception thứ hai từ cleanup thay thế lỗi gốc.

RED deterministic trên source2e4a819:8 discovered/executed,2 PASS/6 FAIL/0 skip (`bus04-review-red3.log`). Hai case reenter-then-throw vốn PASS vì exception ngắt hide cũ, giữ làm regression; không gọi chúng là RED. Sáu FAIL gồm card/backdrop reenter bình thường, throwing card/backdrop/managed focus fallback, presentation error bị cleanup error che mất. Test Select override dùng selectable consumer control thật trên native traversal fallback; Cancel.TabStop=false là cấu hình fixture để traversal thực sự đi tới control đó.

Sau fix,8/8 PASS (`bus04-review-green3.log`). Một lần copy source fix về giữ timestamp cũ làm incremental build dùng binary RED; log tên green2 vẫn FAIL6, không được tính GREEN. Đã rebuild Controls bằng `--no-incremental --no-restore` rồi chạy lại. Không dùng binary cũ làm proof.

Probe Enter trực tiếp ban đầu làm net8 testhost abort trong WinForms WmSetFocus/UpdateFocusedControl:9 discovered,7 cases có kết quả (2 PASS/5 FAIL), Enter aborted và Leave chưa chạy. Các probes này đã được tách khỏi owner suite. WinForms native focus event chuyển exception qua Application.ThreadException, không phải exception managed từ Focus trả về cho host. Diagnostic opt-in `BUS_FOCUS_EXCEPTION_PROOF=enter|leave` chạy trong **helper subprocess riêng**; đặt CatchException trước khi tạo deadline timer/controls, thu nhận original exception đúng1 lần, không modal wait. Production không đổi exception policy hoặc bắt ThreadException.

| Native focus diagnostic | Executed/PASS/FAIL/skip | Cleanup ms |
|---|---|---:|
| net48 enter | 1/1/0/0 | 314 |
| net48 leave | 1/1/0/0 | 302 |
| net8 enter | 1/1/0/0 | 310 |
| net8 leave | 1/1/0/0 | 290 |

Cả4 xác nhận modeNone, card/backdrop hidden, filterfalse, spinnerfalse, cleanup windows0. Outer watchdog15s; helper vẫn có bounded deadline. EXEs positive tại `tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/bin/Release/<tfm>/`; args `--scenario busy-overlay-live <absolute artifacts/bus04/review-focus-<tfm>-<boundary>>`, environment flag chỉ trong process execution này. Không tính4 probes thành live-composition hoặc resource PASS.

Final Busy suites: **95 discovered/executed/PASS,0 FAIL/skip mỗi TFM x64** (net48 12.4535s, net8 12.5826s). Gồm8 boundary cases mới cùng87 regression trước. Discovery riêng và tests giữ hang diagnostics5m; commands/filter như trên, raw logs `artifacts/bus04/review-discovery-<tfm>.log`, `review-tests-<tfm>.log`. NUnit resource: net48 warm/batches USER24/GDI30, net8 USER23/GDI28, flat. `build.ps1 -Configuration Release -Scope All` sau cùng PASS0 warnings/errors,5.15s. Full broad suite và x86 NUnit không chạy lại; không suy ra PASS.

Năm helper EXEs đều build mới từ source cuối theo recipe trước, output `artifacts/bus04/reviewfix-bin/<kind>`, results `reviewfix-<kind>/result.txt`, absolute negative manifest, net48x86 PlatformTarget=x86. Không đổi tolerance/settle condition hoặc warm-up. **5 selected/discovered/executed,3 PASS/2 FAIL,0 skip; wrapper exit1**:

| Output | Mode/bitness | USER warm10→25→50→75→100 | GDI mọi checkpoint | Exit | Cleanup ms |
|---|---|---|---|---:|---:|
| positive-net48 | NativeLayered x64 | 49→49→49→49→49 | 45 | 0 | 35499 |
| positive-net8 | NativeLayered x64 | 50→50→50→50→50 | 46 | 0 | 40443 |
| positive-net48-x86 | NativeLayered x86 | 47→48→48→48→48 | 45 | 1 | 38776 |
| negative-net48 | OpaqueFallback x64 | 47→48→48→48→48 | 43 | 1 | 30832 |
| negative-net8 | OpaqueFallback x64 | 50→50→50→50→50 | 46 | 0 | 30643 |

Raw bằng settled ở mọi checkpoint của matrix mới; settle251–327ms. Hai failures vẫn là strict USER+1, GDI flat; source USER object chưa xác định. Một sampling read-only GetGUIThreadInfo trong x86 run thấy caret0/capture0, nhưng không đủ đối chứng theo checkpoint để quy nguyên nhân hoặc loại trừ resource nào. Không gọi nguồn tăng là cache hay leak đã được chứng minh. [GetGuiResources](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getguiresources) đếm USER objects rộng hơn inventory HWND; inventory không delta không đóng gate.

Trên cả5 EXEs, block live/fallback scenarios trước soak PASS: alpha/input/selection/scroll, UIA, same-HWND live regions, opaque card, clipping/higher sibling, modal usable, tab/minimize/restore và owner move96. Print total16 live/9 fallback, alpha-only delta0. Cả5 cleanup windows0/hooks7/7; không còn helper/testhost khi trả desktop. Legacy probe filter counters0/0 vẫn không được diễn giải thành production registrations.

**Hai Important đã có fix và regression; BUS-04 vẫn BLOCKED/resource gate mở**, physical125/150/200 và mixed-DPI NOT RUN. Không dùng các PASS mới để xóa failures cũ. BUS-05 nếu được integrator cho phép chỉ là provisional work; task này không triển khai demo hoặc đóng acceptance.

### Hash của candidate sửa hide
| Source | SHA-256 |
|---|---|
| src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBusyOverlayHost.cs | 550C2BAE5BB4D71985FB306AC4D4BA359D0BFA01E109E2E8C151EE638222B24E |
| src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapBusyOverlayController.cs | 76232ACC88CC518BDB1D8706E228BE1C188679C08F2DB7BDE86FA07F56CF382B |
| tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBusyOverlayHostTests.cs | 5EC570F4D0CB43EBA938C5D07443F955B20B629D2C3FD0BB5FD87BE0C9E830E5 |
| tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/BusyOverlayScenario.cs | D29E83241EA007C2B53F6EAC7A85326222649749B7FE34E947DDE93D1ECAD508 |

| reviewfix-bin output | EXE SHA-256 | Core DLL SHA-256 |
|---|---|---|
| positive-net48 | 70E10FC32D9BAE936CD8F9AA968D6C9B5E57D83CF1FEE43313270F4C73489863 | 7A117C93470E3E10B70334F5C8E8EBAA6F6D1501C7385C20EFB405A41222ECC3 |
| positive-net8 | 775870B2C6148DB323F63E9905DBA9B5BD726D7C2E303E39C4F1D9752D6CFF28 | 97452ADDB49A684164EBEA30A39D564516A1541ACA2BEFBD770075E2F3507E46 |
| positive-net48-x86 | A81ACB236A1AFDC2B16A5DC48F8A36D1FDC63CF20C6386D64C47E142BCF3C318 | 7A117C93470E3E10B70334F5C8E8EBAA6F6D1501C7385C20EFB405A41222ECC3 |
| negative-net48 | B933715542B53B415BA05A4E7A228023CC4DF7028FAD9918242EBFBBD7FE8B6C | 45747E387696D329BEE494BDEC300B62040E9DB8ECFC73737734544605FFD045 |
| negative-net8 | DB0AA15363D96ADA81BF6338662369DBB270CB61A55379CD0669EC290BCA4498 | 3678090387A5400DC971F530A2BF2C907E89F279A1620FDE2D3487B1C3D92A0D |

Managed helper DLL positive-net8: B4150A7188309CC0FA95410ECB9766846F777E59A6DB21868DC132DA256F95E7.

Managed helper DLL negative-net8: FB19B22E3E2F2018E7E7FE162E76A87DE2E483DEF028A6CD2220CC43A83F36FD.

## BUS-04/05: cleanup harness và đối chứng USER+1 sau review toàn nhánh

Candidate: `bf3946b` + thay đổi harness trong commit chứa mục này; marker `bf3946b+BUS45-cleanup-resource-diagnostic`. Không đổi production, BootstrapBusyHost cũ, BUS-05 behavior hoặc BUS-06. `CaptureNow` chuyển ownership bitmap cho caller khi thành công; nếu Graphics/CopyFromScreen lỗi thì dispose trước rethrow. Demo test dùng `using var` cùng explicit Dispose để vẫn kiểm hậu điều kiện, đồng thời cleanup khi assertion/pumping lỗi. Đây là hai sửa ownership hiển nhiên; không tạo regression giả hoặc gọi tests PASS cũ là RED mới.

### Đối chứng trong process riêng

Opt-in `BUS_RESOURCE_PROOF`: `isolated` chạy overlay soak ngay sau Form.Show; `control` thay mỗi host bằng Panel + TextBox + Timer150ms, recreate/dispose; `no-uia` giữ full capture/input/modal nhưng bỏ hai external UIA Invoke; `uia-only` gọi cùng external client vào hai Button native thường rồi overlay soak; `uia-control` gọi UIA tương tự rồi plain control soak, **không tạo BootstrapBusyOverlayHost nào**. `full` chỉ thêm counts mỗi cycle vào full scenario. Normal matrix cuối không đặt biến này. Các mode chẩn đoán không phải acceptance scenario đầy đủ; string mode trong control log chỉ mô tả manifest configuration, không khẳng định control dùng overlay.

Tất cả dùng đúng EXE architecture/manifest, STA tuần tự, 10 warm cycles + 100 cycles, delay150ms, settle ít nhất250ms/stable100ms/max1500ms tại cùng checkpoints. Không GC, tăng tolerance, chọn minimum hoặc đổi warm-up. Per-cycle before/after là raw counts, không thêm settle mỗi cycle. Cycle index gồm warm10; batch25/50/75/100 loại warm10. Control không mô phỏng toàn bộ card/spinner hierarchy, nên đây là đối chứng tối thiểu, không phải chứng minh hai workload tương đương hoàn toàn. UIA/capture prefixes có thời gian chuẩn bị khác nhau; final soak giữ cùng timing/pumping.

| Process/configuration | Prefix / soak | USER warm10 → batch25/50/75/100 | GDI | Kết quả |
|---|---|---|---|---|
| net48 x86 live | isolated overlay | 23 → 23/23/23/23 | 30 | PASS |
| net48 x86 live | plain control | 23 → 23/23/23/23 | 25 | PASS |
| net48 x64 fallback | isolated overlay | 23 → 23/23/23/23 | 30 | PASS |
| net48 x64 fallback | plain control | 23 → 23/23/23/23 | 25 | PASS |
| net48 x86 live | full + cycle logging | 48 → 49/49/49/49 | 45 | FAIL resource |
| net48 x64 fallback | full + cycle logging | 47 → 48/48/48/48 | 43 | FAIL resource |
| net48 x86 live | full bỏ UIA | 46 → 46/46/46/46 | 45 | PASS reduced scenario |
| net48 x64 fallback | full bỏ UIA | 46 → 46/46/46/46 | 45 | PASS reduced scenario |
| net48 x86 live | UIA native Buttons + overlay | 27 → 27/28/28/28 | 30 | FAIL resource |
| net48 x64 fallback | UIA native Buttons + overlay | 26 → 26/27/27/27 | 30 | FAIL resource |
| net48 x86 live | UIA native Buttons + plain control | 26 → 26/26/26/26 | 28 | PASS |
| net48 x64 fallback | UIA native Buttons + plain control | 26 → 26/26/26/27 | 28 | FAIL resource |

Full diagnostic x86 tăng bền đầu tiên tại cycle14; transient tăng/giảm thêm tại20/21,33/34,57/58. UIA-only tăng bền tại cycle60 x86 và45 fallback. Counterexample không host fallback tăng tại cycle106; cycle1/2 còn có transient26→27→26. Raw HWND/class/parent/owner/native-thread inventories không đổi tại warm10 so với các điểm tăng này (UIA-prefix cases có14 HWND). Log cycle ghi managed thread1; inventory ghi native thread IDs riêng. Không đồng nhất USER handles với HWND count.

Bỏ UIA làm mất +1 ở hai full runs; UIA native Buttons trước soak đủ tái hiện +1 ở cả hai overlay configs và một plain-control process. Điều này chứng minh overlay host **không cần thiết** để có cùng biểu hiện, và loại capture/input/modal khỏi điều kiện cần trong counterexample. Không chứng minh mọi failure có cùng allocation, không xác định loại USER object/allocation stack, không gọi là cache vô hại và không loại trừ lỗi production khác. Không có allocation/destroy stack hoặc documented per-object USER attribution trong lượt này. Resource acceptance của full scenario vẫn **BLOCKED**, giữ nguyên strict failure và mọi limitations physical/mixed-DPI/Owner acceptance.

Raw outputs local (không commit/không bảo đảm retention): `artifacts/bus04/diag-*`, `prefix-*-no-uia`, `uia-prefix-*`; scripts ad hoc chỉ ở artifacts, không thêm runner framework. Diagnostic wave1 net48/net8 full khác source revision của các opt-in bổ sung, không dùng làm final-source acceptance. Final-source normal matrix và hashes bên dưới là dữ liệu riêng.

### Final candidate: normal matrix, không opt-in diagnostic

Commands tuần tự:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f <net48|net8.0-windows> --filter 'FullyQualifiedName~BootstrapBusy|FullyQualifiedName~BusyOverlayDemoFormTests' --blame-hang --blame-hang-timeout 5m --logger 'console;verbosity=normal'
dotnet build tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.csproj -c Release -f <tfm> --no-restore -o artifacts/bus04/final45-bin/<kind> <architecture/manifest arguments>
<actual EXE/apphost> --scenario <busy-overlay-live|busy-overlay-fallback> <absolute output directory>
./build.ps1 -Configuration Release -Scope All
```

x86 thêm `-p:PlatformTarget=x86`; fallback thêm absolute `-p:ApplicationManifest=<repo>/tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/app.no-layered.manifest`. Embedded manifest/QueryActCtx và actual bitness được xác nhận trong từng result. Helper deadline60s, outer75s. `BUS_RESOURCE_PROOF` unset; không bỏ UIA, capture, mouse hoặc modal ở normal runs. Build All PASS,0 warnings/errors,3.43s. Final focused mỗi TFM discovered/executed97, PASS97, FAIL0, SKIP0 (95 Busy +2 demo), net48 13.5557s/net8 13.9752s. Không chạy toàn owner suite, x86 NUnit hoặc physical DPI mới.

| Normal helper | USER warm10 →25/50/75/100 | GDI | Exit/result | Cleanup ms |
|---|---|---|---|---|
| positive-net48 | 49 → 49/49/49/49 | 43 | 0 / PASS | 38193 |
| positive-net8 | 50 → 50/50/50/50 | 46 | 0 / PASS | 40414 |
| positive-net48-x86 | 47 → 48/48/48/48 | 43 | 1 / FAIL resource | 38626 |
| negative-net48 | 47 → 48/48/48/48 | 43 | 1 / FAIL resource | 30297 |
| negative-net8 | 50 → 50/50/50/50 | 46 | 0 / PASS | 31001 |

5 discovered/executed,3 PASS/2 FAIL resource,0 SKIP. Các scenario blocks trước soak đều PASS; cleanup5/5 PASS, windowsRemaining0/hooks7/7. `filters=0/0` trong helper là legacy probe counters không đăng ký filter production; không dùng chúng làm bằng chứng registration count. Raw=settled tại toàn bộ20 batch checkpoints và5 warm checkpoints của normal matrix. Raw outputs `artifacts/bus04/final45-*-normal/result.txt`; NUnit logs `final45-tests-<tfm>.log`. Script local ghi từng process ExitCode và tiếp tục để thu đủ matrix; script exit0 không thay thế hai helper exit1. Gate vẫn BLOCKED.

SHA256 source và exact normal binaries (hash sau final runs; source không đổi giữa runs và commit):

| File | SHA256 |
|---|---|
| `tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/BusyOverlayScenario.cs` | `786CA0C9497E2F8D2135E36E484662BF9F24A23D5D7E7848959EBE7EF66B221E` |
| `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/BusyOverlayDemoFormTests.cs` | `A562A68B8BE45645E709AEE9992F87A0474614071F53225678940320A31FC978` |
| `artifacts/bus04/final45-bin/positive-net48/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe` | `9F1B4B1CE93232B92590B5F78F38CFED4FFE9A3D4F85520504306FC4D8115D77` |
| `artifacts/bus04/final45-bin/positive-net48/MyDmsVn.Bootstrap5WinFormUI.dll` | `AB7EA1DEE9AB10809E239BBD4A2A8B93940C9443F9E77B87544BCA7375D25155` |
| `artifacts/bus04/final45-bin/positive-net8/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe` | `31FC0B2DA754B812BAC21C543CB35A86CD00FD1FD94A894994BD0133EC7BA443` |
| `artifacts/bus04/final45-bin/positive-net8/MyDmsVn.Bootstrap5WinFormUI.dll` | `37DCA1759EC73A1B99D74F69B6AA36E401AF0C6B5DE1A8E1E71CEF383E4F7AE9` |
| `artifacts/bus04/final45-bin/positive-net8/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.dll` | `49AAFB98342B5673CEB98B0D8837CA64B0607F9E691A2F48D3249CD6DCE65DC9` |
| `artifacts/bus04/final45-bin/positive-net48-x86/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe` | `B041D96BF8EAC25669EE1FEE9763D0AFCE8AC4376ABDB3ACC45933A467D0AF7D` |
| `artifacts/bus04/final45-bin/positive-net48-x86/MyDmsVn.Bootstrap5WinFormUI.dll` | `AB7EA1DEE9AB10809E239BBD4A2A8B93940C9443F9E77B87544BCA7375D25155` |
| `artifacts/bus04/final45-bin/negative-net48/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe` | `D0AB52EC5755C2CDA09F188AF970F3385E119D9F8E7A652AE8FE4AA64F43FE64` |
| `artifacts/bus04/final45-bin/negative-net48/MyDmsVn.Bootstrap5WinFormUI.dll` | `65784E9F3535C740339F4C59AF1228136ED448A5DAE944EB5D4220E02CA2FFAF` |
| `artifacts/bus04/final45-bin/negative-net8/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe` | `D1DAE16C49EB2CA25EB123C045141C3EE02E9E1392F01184FE6ED9846C00617A` |
| `artifacts/bus04/final45-bin/negative-net8/MyDmsVn.Bootstrap5WinFormUI.dll` | `087AEB6E4931C483E86272106B3655D46EFCA73EB559045FB1C5FDFA0F10A876` |
| `artifacts/bus04/final45-bin/negative-net8/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.dll` | `F92AFDED35E4EC2DE76A4DA92EB8ADA3C529BBD5292385C0B6FD00BC422FB087` |

## PR #5 — attribution USER+1 và ranh giới đo resource

Candidate: `3f1065a` + helper trong commit chứa mục này; marker helper `PR5-resource-finalizer-drain`. Phần này tiếp tục lịch sử FAIL bên trên, không chuyển các run cũ thành PASS. Chỉ test helper thay đổi; không đổi production IME/GC, ownership, tolerance, warm-up10, số cycles100 hoặc checkpoint25/50/75/100.

### Native attribution và đối chứng nhân quả

Windows provider `Microsoft-Windows-Win32k`, keyword `UserHandleOperation=0x0000020000000000`, có native events452 `CreateHandle`,453 `DestroyHandle`,454 `UpdateHandleOwner` với `HandleValue`, `HandleType`, `SessionId`, `OwnerProcessId` và native thread/UTC. Calibration `CreatePopupMenu`/`DestroyMenu` khớp exact handle/type2; `SetTimer`/`KillTimer` khớp type16 (public timer ID khác USER handle); `ImmCreateContext`/`ImmDestroyContext` khớp exact HIMC/type17. Không suy USER count từ HWND inventory hoặc đọc bảng handle undocumented.

Đối chứng net48 x64 fallback, prefix hai UIA Invoke trên native Buttons + plain Panel/TextBox/Timer soak, không có BootstrapBusyOverlayHost: process22540 strict FAIL, USER warm26→26/26/27/27, GDI28. Native active handle totals khớp raw GetGuiResources: HWND14, type15=2, timer16=2, menu2=2, cursor3=3 giữ nguyên; HIMC/type17 tăng3→4. Allocation gây tăng là HIMC300356085, event452 tại22:43:14.6665389Z trên background thread16676 (UI thread18460). Exact handle được destroy trên đúng thread tại22:43:19.9570730Z khi process kết thúc. Toàn process native create/destroy totals cân bằng; không gọi đây là leak production hoặc cache vô hại chỉ từ việc cân bằng shutdown.

Đối chứng thứ hai có stack ETW tái hiện strict FAIL. Extra HIMC926486589 được tạo tại22:49:01.4932191Z trên native thread23964 đã tồn tại từ process startup. Module ranges lấy từ chính process; PDB keys từ RSDS của exact DLL và symbol files tải từ Microsoft symbol server: `clr.pdb/CD5F48F2937842D999FF0B4604FA70C61`, `user32.pdb/F0BF56EE0A784974D49A58173A7CBC7E1`. Stack caller→callee:

```text
clr!FinalizerThread::FinalizerThreadStart
  → clr!FinalizerThread::FinalizerThreadWorker
  → clr!Thread::DoExtraWorkForFinalizer
  → clr!SyncBlockCache::CleanupSyncBlocks
  → clr!RCWCleanupList::CleanupAllWrappers
  → clr!RCWCleanupList::ReleaseRCWListInCorrectCtx
  → USER32!InSendMessage
  → win32u → native USER initialization / HIMC
```

Đây là CLR finalizer cleanup RCWs lần đầu vào USER32 và có default input context theo thread; không phải worker UIA trực tiếp cấp extra handle này. Hai input contexts sớm khác có stack UIAutomationCore, được phân biệt trong trace. [Input Context](https://learn.microsoft.com/en-us/windows/win32/intl/input-context) mô tả default input context dùng chung theo thread; [ImmCreateContext](https://learn.microsoft.com/en-us/windows/win32/api/imm/nf-imm-immcreatecontext) xác định loại resource của calibration. Thread start address qua ProcessThread trả0; không dùng nó làm căn cứ attribution. Kernel thread provider/WPR stack recording Access denied; direct ETW provider stack recording bằng cached TraceEvent3.1.21 hoạt động. Không thêm package/runtime dependency hay runner framework vào repo.

Đối chứng điều kiện cleanup trên cùng no-host workload:

| Điều kiện trước warm10 | Raw USER trước→sau cleanup | warm10→25/50/75/100 | GDI sau warm | Kết quả |
|---|---|---|---|---|
| Chỉ `Marshal.CleanupUnusedObjectsInCurrentContext` | 26→26 | 26→26/26/26/27 | 28 | strict FAIL |
| Collect prefix RCWs + bounded finalizer drain + cleanup STA | 26→27 | 27→27/27/27/27 | 28 | PASS |

Run drain process8580: interval22:53:49.0367448–22:53:49.0510468Z; extra HIMC340329869 được cấp **trong interval** lúc22:53:49.0379756Z, trên finalizer thread11164 với cùng CLR stack offsets `0x4c6372`, `0x2680c`; destroy tại shutdown22:54:08.5409502Z. Thử nghiệm chuyển đúng allocation đã xác định từ measured soak sang thời điểm kết thúc prefix, không chọn count thấp hơn hoặc thêm allowance. Chỉ current-context cleanup không đủ nên không dùng nó như một fix chưa chứng minh.

### Sửa methodology của helper

Trước warm10, helper giữ raw USER/GDI rồi `GC.Collect` để retire temporary RCWs của workload trước đó; `GC.WaitForPendingFinalizers` chạy trên Task trong khi STA dùng bounded `Wait`/message pumping, sau đó `Marshal.CleanupUnusedObjectsInCurrentContext` và ghi raw counts sau. Nếu drain không kết thúc trong timeout, scenario vẫn FAIL/cleanup; outer75s/deadline60s giữ nguyên. GC không chạy trong warm10 hay100 measured cycles/checkpoints. Mọi owned HWND/filter/subscription assertions trong cycle, full UIA/input/modal/capture scenarios và process-wide USER/GDI strict zero-growth assertions giữ nguyên. Đây là chuẩn hóa ranh giới workload do nguồn nhiễu đã có stack và đối chứng nhân quả, không phải production cleanup hoặc giảm phạm vi acceptance.

Raw local (không commit): `artifacts/bus04/pr5-resource/trace-uia-control*`, `stack-attribution.txt`, `finalizer-stack-attribution.txt`, `*-calibration.etl`, `symbols/`, `tooling/`. Report điều tra: `artifacts/sdd/bus/pr5-resource-report.md`. Raw artifacts không bảo đảm retention; source/commands và kết quả tóm tắt nằm trong evidence này.

### Final-source full normal matrix sau sửa methodology

Build tuần tự từng helper bằng `dotnet build tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.csproj -c Release -f <net48|net8.0-windows> --no-restore -o artifacts/bus04/pr5-resource/final-bin/<kind>`; x86 thêm `-p:PlatformTarget=x86`, fallback thêm absolute `-p:ApplicationManifest=<repo>/tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/app.no-layered.manifest`. Năm builds PASS,0 warnings/errors. Chạy actual EXE/apphost `--scenario <busy-overlay-live|busy-overlay-fallback> <absolute output>`; `BUS_RESOURCE_PROOF` unset, không opt-in diagnostic/drain env và không bỏ UIA/input/modal/capture. Source thực hiện prefix drain trực tiếp. Launcher title click chỉ thiết lập foreground prerequisite, bỏ TopMost trước khi test thấy foreground. Deadline60s/outer75s giữ nguyên.

| Normal helper | Raw prefix USER trước→sau drain | USER warm10→25/50/75/100 | GDI warm/batches | Exit/result | Cleanup ms |
|---|---|---|---|---|---|
| positive-net48-x86 | 47→48 | 49→49/49/49/49 | 45 | 0/PASS | 38294 |
| negative-net48 | 46→47 | 48→48/48/48/48 | 45 | 0/PASS | 30428 |
| positive-net48 | 46→47 | 48→48/48/48/48 | 45 | 0/PASS | 35171 |
| positive-net8 | 49→49 | 50→50/50/50/50 | 48 | 0/PASS | 37695 |
| negative-net8 | 48→48 | 49→49/49/49/49 | 48 | 0/PASS | 30430 |

5 discovered/executed,5 PASS,0 FAIL,0 SKIP. Mọi full scenario blocks và strict process resource assertions PASS; cleanup5/5 PASS, windowsRemaining0/hooks7/7. Counter `filters=0/0` legacy không được dùng chứng minh registration production. Raw=settled tại cả5 warm checkpoints và20 batch checkpoints. Embedded manifests/executable QueryActCtx, actual32/64bit và DPI96 được xác nhận; live3/3 PASS, fallback2/2 đúng OpaqueFallback (không tính fallback là live). Full old failures được giữ ở mục candidate trước; không dùng giảm scenario để đóng resource gate. Không đổi thành nghiệm thu physical125/150/200%, mixed-DPI hoặc Owner acceptance; các gate đó vẫn NOT RUN/BLOCKED BY ENVIRONMENT/còn mở.

Raw local: `artifacts/bus04/pr5-resource/final-<kind>/result.txt`, `final-matrix.log`, `final-build-<kind>.log`. Không chạy lại NUnit trong riêng resource task này; suite owner ghi full Controls/explicit/build kết quả của candidate tích hợp riêng. Source helper không đổi giữa năm builds/runs và hash bên dưới.

| Exact file dưới `artifacts/bus04/pr5-resource/final-bin/` (trừ source) | SHA256 |
|---|---|
| `tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/BusyOverlayScenario.cs` | `533B521AE919E5E48FB9F7CBFEF9F198A69EECC2C5156210386E0719170345E9` |
| `positive-net48-x86/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe` | `F37FD66BD3A59F9EDAA7B8BCC8F9FA3FAD5F0F9B1F348D1AFC39829BDBE23554` |
| `negative-net48/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe` | `0B08C49B354844E5ACA9028BDC2EF911C13F0043EF57814DE568F1CFEE377C89` |
| `positive-net48/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe` | `E5782B81BB4EDD4AB0C5712CAF84184F27B1B20D4D324B93A7C5A2E99C7C92DE` |
| `positive-net8/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe` | `53DF090448315055D0E76C5B7687E72D94D971FBD91B8E18E12C982D91259776` |
| `positive-net8/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.dll` | `E3C9FF6D65265512A2D1F72C7A8ECF6AB8CC9BB14CBE7CBCF20ABC453A183D61` |
| `negative-net8/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe` | `677C27B1813FE4807BE33608F8E1A6F797E4E0D8FBCBA8FD49F2082411CE3A8F` |
| `negative-net8/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.dll` | `72CA52496F6300ECFC6E754CE5AA9730747485615455B2273D2C70BEC2306B1B` |
