# BUS-04 — input, focus và native lifecycle

Ngày 30/10/2026. Candidate: base `6880df8b7b58160743d2e4387456ccd8d24a3624` cộng commit chứa report này; marker executable cuối `6880df8+BUS04-verified`. Chỉ BUS-04; không đổi `BootstrapBusyHost` cũ, namespace/targets/dependencies/vendor hoặc fingerprint. BUS-05/06 chưa triển khai trong task này. Trạng thái acceptance physical125/150/200% và mixed-DPI **NOT RUN / BLOCKED BY ENVIRONMENT**, không dùng synthetic DPI thay thế.

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
