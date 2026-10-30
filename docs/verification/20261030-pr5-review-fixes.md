# Sửa ba findings PR #5 — BootstrapBusyOverlayHost

Ngày 30/10/2026. Candidate code/test: `9b6698b11e2e0b5ee68a5dc59ffb9b5760ca7e1e` + diff trong commit chứa report này; marker `9b6698b+PR5-review-fixes`. Không đổi `BootstrapBusyHost` cũ, targets, public signatures hoặc resource thresholds.

## Xác minh và sửa

Đọc `artifacts/sdd/bus/pr5-findings.json` trước source. Cả ba finding được xác nhận bằng public behavior trên Windows trước khi sửa production:

- Embedded Form `TopLevel=false`: release token khi Cancel focus không trả về textbox thứ hai. Resolve owner bằng `TopLevelControl as Form`, giữ ancestor reparent subscriptions; regression kiểm focus, outer minimize/restore và chuyển embedded Form sang shell khác.
- Opacity/theme update: callback `Layout` với `AffectedProperty=ChildIndex` hide/release token cuối hoặc Begin token mới; lượt cũ ghi lại `OpaqueFallback` sau cleanup. Guard generation/eligibility sau configure và sau từng BringToFront trước SetMode.
- Panel AutoScroll thật 400×220: message dài gây cả scrollbar ngang và dọc. Dành chỗ cho vertical scrollbar rồi đo lại theo client width; label không vượt viewport, cuộn dọc tới cuối và Cancel vẫn click được.

Căn cứ source pin: [FindForm / TopLevelControl](https://github.com/dotnet/winforms/blob/e4ede9b8979b9d2b1b1d4383f30a791414f0625b/src/System.Windows.Forms/src/System/Windows/Forms/Control.cs), [SetChildIndexInternal / synchronous Layout](https://github.com/dotnet/winforms/blob/e4ede9b8979b9d2b1b1d4383f30a791414f0625b/src/System.Windows.Forms/src/System/Windows/Forms/Control.ControlCollection.cs), [AutoScroll scrollbar calculation](https://github.com/dotnet/winforms/blob/e4ede9b8979b9d2b1b1d4383f30a791414f0625b/src/System.Windows.Forms/src/System/Windows/Forms/ScrollableControl.cs). Test behavior mới là chứng cứ chính; source review không thay native execution.

## Commands và counts

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter 'FullyQualifiedName~Review_' --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f <net48|net8.0-windows> --filter 'FullyQualifiedName~BootstrapBusy|FullyQualifiedName~BusyOverlayDemoFormTests' --blame-hang --blame-hang-timeout 5m --logger 'console;verbosity=normal'
dotnet build tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.csproj -c Release -f <tfm> --no-restore -o artifacts/sdd/bus/pr5-final-bin/<kind> <architecture/manifest arguments>
<exact EXE> --scenario <busy-overlay-live|busy-overlay-fallback> <absolute output directory>
```

RED net8: discovered/executed 8, PASS0/FAIL8/SKIP0; đúng failures focus1, mode6, scrollbar1. GREEN net8 regression: 8/8 PASS. Final focused net48: discovered/executed105, PASS105/FAIL0/SKIP0,18.7943s; net8:105/105 PASS,0 fail/skip,17.8140s. Full owner suite do luồng integration sở hữu, không khẳng định đã chạy từ các focused counts này.

SDK8.0.427; Windows10.0.26300; .NET Framework4.8.10008.0 và .NET8.0.33; NUnit x64. Desktop tương tác exclusive, GUI tuần tự. Physical proof hiện tại actual96 DPI; synthetic layout tests không thay physical125/150/200 hoặc mixed-DPI/Owner acceptance.

## Native matrix

Năm builds exit0,0 warnings/errors. Positive net48 x64/x86 và net8 x64 dùng manifested EXE/live; hai negative dùng absolute `app.no-layered.manifest`/fallback; x86 thêm `-p:PlatformTarget=x86`. Exact outputs `artifacts/sdd/bus/pr5-final-bin/`. Không đặt `BUS_RESOURCE_PROOF`; giữ capture/input/UIA/modal và strict resource gate.

Lượt launch đầu:5/5 BLOCKED foreground activation, mỗi helper cleanup PASS/windowsRemaining0; không lấy làm scenario/resource PASS. Desktop không khóa: foreground ChatGPT cùng session6, OpenInputDesktop thành công (probe của luồng resource). Chạy lại cùng exact EXEs với launcher local `run-pr5-activated.ps1`: tạm nâng window, click thật title bar rồi bỏ TopMost trước scenario; cùng cách thiết lập prerequisite của native test fixture, không sửa binaries/production để ép PASS. Deadline60s/helper,75s outer.

| Exact helper | USER warm10 →25/50/75/100 | GDI | Exit/result | Cleanup ms |
|---|---|---|---|---|
| positive-net48-x86 | 47 →48/48/48/48 | 43 | 1 / FAIL resource | 35248 |
| negative-net48 | 48 →49/49/49/49 | 45 | 1 / FAIL resource | 30475 |
| positive-net48 | 48 →48/48/48/48 | 43 | 0 / PASS | 35222 |
| positive-net8 | 50 →50/50/50/50 | 46 | 0 / PASS | 37798 |
| negative-net8 | 50 →50/50/50/50 | 48 | 0 / PASS | 30377 |

5 discovered/executed,3 PASS/2 FAIL resource,0 SKIP. All scenario blocks trước soak PASS5/5; ba positive NativeLiveGate=PASS, hai fallback NOT APPLICABLE. Cleanup5/5 PASS,windowsRemaining0/hooks7/7; không còn helper processes sau matrix. `filters=0/0` là legacy probe counters, không chứng minh production filter registration counts. Raw=settled ở20 batch checkpoints; riêng negative-net8 warm raw51→settled50 (các warm khác không đổi). Không tăng tolerance/GC/chọn minimum, giữ settle hiện hữu. Resource gate tiếp tục BLOCKED, không quy +1 cho cache hoặc khẳng định nguyên nhân. Raw kết quả ở `pr5-activated-<kind>/result.txt`; initial launch ở `pr5-final-<kind>-normal/result.txt`.

## Identity và giới hạn

SHA256 host source `78D96AB6544F6244B339CC8049071EFB356B139C81F5332A72EE139060DCB646`; host tests `B41B2C4E2A844D9C914051FF48985DD631F5608E72B157B7C6A1087FD363A070`. Native matrix chạy helper source trước thay đổi UTC diagnostic riêng của luồng resource; binaries trong pr5-final-bin không bị rebuild sau đó. Raw outputs/logs/PNGs/binaries chỉ local ignored tại `artifacts/sdd/bus/`, chưa upload/durable retention. Không commit raw results. Resource acceptance, physical125/150/200, mixed-DPI và Owner approval vẫn mở; không chuyển historical PASS sang candidate này.

Exact production DLL SHA256 trong matrix (các negative build có path/manifest inputs riêng):

| Kind | SHA256 |
|---|---|
| positive-net48 và positive-net48-x86 | `FF1CC52D16D60827F9284F699085BF4896A78B5960B572EE40DBD04321DADBE8` |
| positive-net8 | `6F24AE085F79234A0BD559801EAA443247A7806C7D8287EA8E6947845A5FAA26` |
| negative-net48 | `61D535EBB4744D8D81CD07A0360C9215D6246845158A64E8B47FC305B75308AC` |
| negative-net8 | `258D568139B089BE77FA16DB5D232A1F9EDB139834A2B6BB10DE67EB5195C3FE` |
