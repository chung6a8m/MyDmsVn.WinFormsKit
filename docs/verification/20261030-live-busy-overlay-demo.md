# BUS-05 — demo live overlay và executable prerequisites

Ngày 30/10/2026. Candidate source: base `d4607a8` + commit BUS-05 chứa report này (không dùng kết quả BUS-04 thay cho candidate). Triển khai demo **provisional** theo yêu cầu tiếp tục dù BUS-04 resource gate BLOCKED. Không sửa UI core/BUS-04, host cũ hoặc fingerprint BUS-06.

## Phạm vi

Route `Loading / Busy Overlay` tạo `BusyOverlayDemoForm : DemoFormBase`. Content live gồm TextBox, native ComboBox/ListView, progress và counter cập nhật mỗi100ms; so sánh `BootstrapBusyHost` opaque dùng content instance riêng. Slider0–100 mặc định35, buttons và diagnostics nằm ngoài host; theme dùng selector gallery hiện hữu. Có fast100ms, long10s, nested cũ/mới hoàn tất trước, Cancel trong card và ngoài vùng, hide/show. Caller đóng ComboBox popup trước Begin và disable native editors trong cả pending/visible để chặn keyboard/UIA; timer hiển thị dữ liệu tiếp tục chạy. Cleanup timer, tokens và theme subscriptions khi dispose.

Không có SQL hoặc capture-content API trong source demo. Diagnostic tùy chọn `--busy-overlay-proof <absolute-output>` chạy bounded900ms trong chính gallery EXE, đọc embedded resource24/1 và QueryActCtxW(flags8, EXE HMODULE, class6), ghi actual window DPI/PerMonitorV2/RenderMode. Đây là runtime prerequisite probe, **không** là desktop live-composition visual proof hoặc resource soak.

## Môi trường và commands

Windows10.0.26300.0 x64, SDK8.0.427; .NET Framework registry release534296, Desktop8.0.33 có cài. Warm restore. EnumDisplayMonitors/GetScaleFactorForMonitor chỉ thấy monitor65537 scale100%; cả gallery actual96 DPI. Physical125/150/200 và mixed-DPI: **NOT RUN / BLOCKED BY ENVIRONMENT**, không đổi desktop DPI toàn máy.

```powershell
.\build.ps1 -Configuration Release -Scope Controls
$filter = 'FullyQualifiedName~BusyOverlayDemoFormTests|FullyQualifiedName~IntegratedDemo|FullyQualifiedName~AdvancedInputsDemoLayoutRegressionTests|FullyQualifiedName~NavigationDemoFormTests'
# Thực hiện tuần tự cho net48 rồi net8.0-windows:
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests -c Release -f <TFM> --list-tests --filter $filter --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests -c Release -f <TFM> --no-build --filter $filter --blame-hang --blame-hang-timeout 5m
# Launch đúng EXE; outer watchdog15s, kill nếu timeout:
& samples/ControlGallery/bin/Release/<TFM>/MyDmsVn.Bootstrap5WinFormUI.Demo.exe --busy-overlay-proof <absolute-output>
```

Build Controls locked restore PASS,0 warnings/errors, hai TFM. Discovery59 cases mỗi TFM; final executed59/pass59/fail0/skip0 mỗi TFM (physical100%, sequential STA). Hai tests demo mới kiểm route, hierarchy/content ownership, slider0/100, nested theo hai thứ tự, timer thật trong busy, cancel callback card, fast operation, hide/show và disposal. Representative existing IntegratedDemo typography/layout, advanced input và navigation được giữ. Lượt initial route test RED1/1 FAIL vì thiếu route; lượt regression đầu58/59 PASS vì RequiredPages chưa thêm route, sau cập nhật danh sách59/59 PASS. Không gọi các tests thêm sau implementation là RED đã chứng minh.

## Executable proof

Cả hai actual gallery EXE x64 Exit0: embedded manifest có Windows10 supportedOS `{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}` + common-controls6. QueryActCtx(EXE HMODULE)=True,40bytes,1 element Windows10 type1. Actual DPI96, `AreDpiAwarenessContextsEqual(window,-4)=True`, `RenderMode=NativeLayered`. Net48 chọn App.config PerMonitorV2; net8 SetHighDpiMode vẫn trước EnableVisualStyles/form/HWND; manifest không thêm DPI declaration ghi đè.

SHA256 EXE candidate cuối: net48 `8B9C1719815F4BF600025F7DC6CA3B21BE410C6C93E69F3F44B1CB372CB9356A`; net8 apphost `ED375376FB9B3FC954BBE6B0C2086F233B8AC4FC98DF5BEBD49A42A5BBBBEC97`.

Raw outputs local trong `artifacts/verification/busy/bus05/` (untracked, không bảo đảm retention/share URL); không commit logs/binaries/ảnh. Không có Owner visual decision hoặc clip published trong BUS-05. Runtime NativeLayered và live timer assertion không thay pixel proof cùng HWND của BUS-01/03.

## SourceGrid acceptance còn mở

**NOT RUN** cho SourceGrid-under-overlay. ControlGallery chỉ reference UI core; GridGallery hiện hữu reference adapter và engine, nhưng chưa có overlay acceptance scenario. Không suy từ ListView sang SourceGrid/hardware/third-party rendering.

Bước nghiệm thu tiếp theo trong consumer/test host có adapter ProjectReference (có thể mở rộng GridGallery): tạo một grid instance/editor riêng trong unparented panel rồi truyền panel vào BootstrapBusyOverlayHost; đóng editor/popup trước Begin, chặn command admission của caller, cập nhật cell/progress dưới cùng token/backdrop HWND, kiểm native render/input/cancel/theme/hide/show/dispose ở alpha0/.35/1 trên hai manifested EXEs. Capture chỉ ở test host; ghi riêng actual RenderMode và nguồn update, physical100/125, resource counts. UI core không thêm dependency BSG. Integration MyDmsVn là PR riêng.

## Giới hạn/trạng thái

BUS-05 có source/tests và runtime prerequisite proof physical100; physical125 regression, SourceGrid acceptance, visual demo/clip và Owner acceptance còn mở. BUS-04 vẫn BLOCKED: strict USER+1 ở net48 x86 live/net48 x64 fallback; physical mixed-DPI chưa chạy. BUS-06 chưa thực hiện. Không tuyên bố full BUS acceptance hoặc resource PASS.

## Cleanup harness sau review toàn nhánh

Candidate `bf3946b` + commit cleanup/diagnostic kế tiếp: `LiveTimerNestedCompletionCancelHideShowAndDispose` dùng `using var form`, vẫn explicit Dispose trước hậu điều kiện. Assertion/pumping lỗi nay cũng dispose form/timer/theme subscription. Không đổi production demo hoặc behavior BUS-05. Final focused filter `FullyQualifiedName~BootstrapBusy|FullyQualifiedName~BusyOverlayDemoFormTests`: mỗi TFM discovered/executed97, PASS97, FAIL0, SKIP0; net48 13.5557s, net8 13.9752s. STA tuần tự, hang diagnostics5m. Đây là95 Busy regressions +2 demo cases, không phải rerun toàn bộ59-case BUS-05 representative suite. Actual gallery visual proof không chạy lại vì diff chỉ harness; evidence gallery cũ giữ candidate cũ, không nâng thành proof mới.

Resource counterfactual và normal final helper matrix nằm trong [BUS lifecycle evidence](20261030-live-busy-overlay-lifecycle.md#bus-0405-cleanup-harness-và-đối-chứng-user1-sau-review-toàn-nhánh). UIA-prefix/plain-control fallback process có USER+1 dù không tạo overlay host; chưa xác định native object/allocation stack. Không đổi resource gate hoặc trạng thái provisional BUS-05; physical125/SourceGrid/Owner acceptance vẫn mở.
