# PR #5 — sửa ba FAIL của full Controls suite

Ngày **30/10/2026**. Candidate: commit BUS findings `8270706c1350b73b31c88b86203b69022bf83f26` cộng commit chứa report này. Windows/desktop tương tác, SDK `8.0.427`, Release, `net48` và `net8.0-windows`, x64. Engine exact gitlink `f4e457b43582bf01892f50bdc74aa480531e5944`; warm restore/build, không nhận clean-restore claim. Report chỉ sở hữu TAB test oracles và API baseline; BUS findings có evidence riêng. Không thay production TAB renderer, host cũ hoặc dependencies.

## Nguyên nhân và sửa

Full Controls lịch sử `bus45-full-net48.log`/`bus45-full-net8.log` có 2604 discovered/TFM: 2602 executed, 2599 PASS, 3 FAIL; 2 existing Explicit cases bị loại khỏi default run. Chạy fixture TAB riêng trong Light cho 11/11 PASS; ép Dark tái hiện đúng hai FAIL lịch sử: center không tìm được pixel (`minX=320,maxX=-1`), italic `actual=42,reference=48`. Đây là dependence vào theme/oracle, không có evidence lỗi BUS hoặc clipping production.

- Caption `X` mảnh trên Dark có ClearType subpixel coverage nhưng không có pixel nào đạt ngưỡng RGB32 so với solid Primary. Histogram ảnh diagnostic xác nhận pixel đã được vẽ. Test mới tìm pixel khác surface trong available content rectangle, dùng probe không tô Magenta extension marker, giữ tolerance center2px và caption-visible assertion; metrics lấy theo actual DPI thay vì hard-code padding12. Chạy cả Light/Dark và restore theme sau case.
- Italic fixture handle-free có `SelectedIndex=-1`, renderer dùng MutedText nhưng reference cũ dùng Primary; reference còn dùng `NoPadding` trong khi measurement/draw production cùng default glyph padding. Test mới khóa explicit selected/unselected trên cả Light/Dark, palette và flags tương đương, giữ comparison với full-content reference rộng và thêm guard reference có ink. Kết quả actual/reference lần lượt Light90/90,90/90 và Dark44/44,48/48. Không nới tolerance crop hoặc sửa renderer để chiều theo oracle sai.
- API gate được deliberate RED trên final host source cả hai TFM trước khi đổi fingerprint. Compiled surfaces giống từng byte, 1787 dòng, SHA `a96de87de1e1a0df9dbddf68a48614a13ca6e83e4927fbb4b64f2fc336c56cba`. Loại đúng hai type `BootstrapBusyOverlayHost`/`BootstrapBusyOverlayRenderMode` cùng members tái tạo approved TAB SHA `1d1172af4dc1a247b330ca3df84d00bf0903ab22470fa9c59581b1543002f3c1`: không có unrelated API delta. Exact contract test khóa host sealed Panel, constructor, public properties/setters/event/Begin, chín protected overrides và enum0/1/2; chi tiết tại [API baseline](../components/controls/PUBLIC_API_BASELINE.md).

## Commands và kết quả candidate

Tất cả GUI execution STA/tuần tự; hang diagnostics5 phút. Build test project transitively build UI core, ControlGallery và VisualStyleHost cả hai TFM, **0 warnings/0 errors**. Không dùng binaries trước sửa.

```powershell
dotnet build tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --no-restore -v minimal
# Lặp từng command cho net48 rồi net8.0-windows, không chạy song song:
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --no-build --list-tests --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --no-build --filter "FullyQualifiedName~BootstrapTabPaintCompatibilityTests|FullyQualifiedName~Phase16PublicApiBaselineTests" --blame-hang --blame-hang-timeout 5m --logger "console;verbosity=normal"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --no-build --blame-hang --blame-hang-timeout 5m --logger "console;verbosity=normal"
```

| Selection | TFM | Discovered | Executed | PASS | FAIL | Skip |
|---|---|---:|---:|---:|---:|---:|
| API audit trước đổi fingerprint | net48 | 17 | 17 | 16 | 1 expected RED | 0 |
| API audit trước đổi fingerprint | net8.0-windows | 17 | 17 | 16 | 1 expected RED | 0 |
| TAB Light/Dark oracle reproduction | net48 | 13 | 13 | 11 | 2 expected RED | 0 |
| TAB + API sau sửa | net48 | 32 | 32 | 32 | 0 | 0 |
| TAB + API sau sửa | net8.0-windows | 32 | 32 | 32 | 0 | 0 |
| Full Controls (default non-explicit) | net48 | 2617 | 2615 | 2615 | 0 | 2 Explicit |
| Full Controls (default non-explicit) | net8.0-windows | 2617 | 2615 | 2615 | 0 | 2 Explicit |
| Select native cases được chọn Explicit | net48 | 2 | 2 | 2 | 0 | 0 |
| Select native cases được chọn Explicit | net8.0-windows | 2 | 2 | 2 | 0 | 0 |

Default full run loại hai existing `[Explicit]` cases `NativeFirstClickFromAnotherControlKeepsPopupAndSearchFocus(false/true)` với lý do cần interactive desktop/SendInput. Vì desktop khả dụng, đã chạy riêng filter `FullyQualifiedName~NativeFirstClickFromAnotherControlKeepsPopupAndSearchFocus` với cùng command test/framework/hang diagnostics: **2/2 PASS mỗi TFM**. Tổng cộng từng TFM có **2617/2617 discovered cases đã được execute, 2617 PASS, 0 FAIL, 0 cases còn bỏ qua**, chia thành default full run và explicit run riêng; không gọi default summary2615 là toàn bộ discovery2617. Full run mất1.5173 phút/net48 và1.4460 phút/net8.

Raw logs giữ ngoài Git tại `artifacts/sdd/bus/pr5-*.log`; báo cáo điều tra chi tiết local `artifacts/sdd/bus/pr5-suite-failures-report.md`. Test source SHA256: TAB `687248E730DB22361870DCEBE78723178B55DBAD34D9481D96CE44ECDE8572AC`, API `EDDA22157A5684E9252873B9484161CE6641267D8F8F12A1F48B86672685B18F`; final host `78D96AB6544F6244B339CC8049071EFB356B139C81F5332A72EE139060DCB646`.

Native BUS resource acceptance, physical125/150/200, mixed-DPI, SourceGrid integration và Owner acceptance không được đóng bằng full Controls/API PASS. Không chuyển historical TAB visual approval thành approval cho BUS. Scope này không chạy Grid suite vì chỉ thay Controls test/API audit; Build All và matrix BUS thuộc integrator/evidence riêng.
