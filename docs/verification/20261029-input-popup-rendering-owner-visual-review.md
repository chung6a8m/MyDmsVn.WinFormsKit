# Bộ ảnh để Owner duyệt input và popup

Ngày: **29/10/2026**. Branch: `codex/plan-input-popup-rendering-20261029`. PR: [#3](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/pull/3).

## Candidate và phạm vi

- Code toolkit không đổi so với source execution revision `c060415141cfa3bf1867593326741edad2de0ae9`; [stability evidence](20261029-input-popup-rendering-stability.md) giữ full-suite của revision đó. Report trước bộ ảnh là `06312741971f200865be7a7d6a2a606faeaf2587`.
- Helper chụp mới ở `5dcf8afbe3d586ffed492b48e8197d6358e80934` thêm `--scenario rounded-popup-theme`. Scenario `rounded-popup` cũ vẫn tô Fuchsia để thử child overpaint; scenario mới để `BootstrapLookupBox.ResultsGrid` dùng màu theme mặc định. Không đổi production assembly/API.
- Ảnh chụp HWND/CopyFromScreen trên Windows desktop thật, theo hai mức Scale: **100% (DPI 96)** và **125% (DPI 120)**; `net48` và `net8.0-windows`, Light và Dark. Mỗi tổ hợp có input một dòng (rộng 600), input hai dòng (rộng 430), Select mở và Lookup mở.

## Kết quả chạy

| Gate | Discovered | Executed | PASS | FAIL | Skip | Ghi chú |
|---|---:|---:|---:|---:|---:|---|
| Build helper `net48` | — | 1 | 1 | 0 | 0 | 0 warning/error |
| Build helper `net8.0-windows` | — | 1 | 1 | 0 | 0 | 0 warning/error |
| Native fixture `BootstrapOverlayNativeVisualRegressionTests`, `net48` | 8 | 8 | 8 | 0 | 0 | Chạy tại Scale 125%, giữ Fuchsia stress oracle |
| Native fixture `BootstrapOverlayNativeVisualRegressionTests`, `net8.0-windows` | 8 | 8 | 8 | 0 | 0 | Chạy tại Scale 125%, giữ Fuchsia stress oracle |
| Helper `input-row` + `rounded-popup-theme`, hai Scale/hai TFM | 8 lượt host | 8 lượt host | 8 | 0 | 0 | Mỗi lượt chụp Light/Dark; tổng **32 PNG + 32 metadata** |

Kiểm lại từng cặp PNG/metadata: **32/32 có đủ file**; 100% ghi `window.dpi`/`owner.dpi`/`popup.windowDpi` = 96, 125% = 120; owner và popup cùng PerMonitorV2. Mười sáu ảnh input có `oracle.equal-height`, `oracle.flow-containment`, `oracle.stable-layout` PASS. Mười sáu ảnh popup có `oracle.edge-continuity`, `oracle.corner-symmetry`, `oracle.shell-band`, `oracle.content-containment` PASS. Mọi popup theme-default ghi `fuchsia.pixels=0`; Lookup `content.background=theme.surface`: Light `White`, Dark `#2B3035`. Kiểm tra bằng mắt một số PNG gốc và bảng hiển thị xác nhận ảnh không trống/không bị che. Scale được trả lại **100%**, đúng mức quan sát lúc bắt đầu lượt này.

Lệnh build/test:

```powershell
$hostProject = 'tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.csproj'
$controlsProject = 'tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj'
foreach ($targetFramework in @('net48', 'net8.0-windows')) {
    dotnet build $hostProject -c Release -f $targetFramework --no-restore -v minimal
    dotnet test $controlsProject -c Release -f $targetFramework --no-restore --filter 'FullyQualifiedName~BootstrapOverlayNativeVisualRegressionTests' --blame-hang --blame-hang-timeout 5m --logger 'console;verbosity=normal'
}
```

Lệnh host đã chạy riêng ở mỗi Scale vật lý; thao tác Scale thực hiện trong Windows Settings trước khi chạy:

```powershell
foreach ($targetFramework in @('net48', 'net8.0-windows')) {
    $hostExe = Join-Path (Get-Location) "tests/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost/bin/Release/$targetFramework/MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost.exe"
    foreach ($scenario in @('input-row', 'rounded-popup-theme')) {
        $outputDirectory = Join-Path (Get-Location) "artifacts/verification/20261029-owner-visual/working/<100-or-125>/$targetFramework/$scenario"
        New-Item -ItemType Directory -Force $outputDirectory | Out-Null
        $captureProcess = Start-Process -FilePath $hostExe -ArgumentList @('--scenario', $scenario, "`"$outputDirectory`"") -WindowStyle Hidden -PassThru
        if (-not $captureProcess.WaitForExit(30000)) {
            $captureProcess.Kill()
            if (-not $captureProcess.WaitForExit(5000)) { throw 'Host cleanup not confirmed' }
            throw 'Host timed out'
        }
        if ($captureProcess.ExitCode -ne 0) { throw 'Host failed' }
    }
}
```

## Bảng duyệt và giới hạn

Bảng HTML cục bộ: `artifacts/verification/20261029-owner-visual/working/index.html`. Bảng đặt 100%/125% cạnh nhau theo TFM/theme/case và liên kết từng PNG/metadata gốc. Có thể mở qua local server `http://127.0.0.1:8765/index.html` trong phiên Windows hiện tại. Raw PNG, metadata và bảng HTML nằm dưới `artifacts/` bị ignore, **không có trong Git/PR và chưa upload thành durable CI/PR artifact**. Người kiểm chứng ở máy khác cần chụp lại hoặc nhận bộ evidence riêng.

Đây là canonical visual set. Chưa chụp radius 0/nhỏ/lớn, viền dày, resize/theme/reopen pairwise hoặc physical 150%/200%; các mục đó **NOT RUN**. Full suite **không chạy lại** sau thay đổi helper-only `5dcf8af`; PASS 5.442 case của [stability evidence](20261029-input-popup-rendering-stability.md) thuộc source candidate cũ, có cùng production source. Native/geometry oracle không thay quyết định thẩm mỹ của Owner.

- **Owner visual decision: PENDING**, chưa ghi APPROVED/CLOSED; chờ Owner xem bảng và nêu quyết định/phạm vi/finding.
- **Consumer MyDmsVn acceptance: NOT RUN**, giữ riêng dù Owner chấp nhận ảnh toolkit.
