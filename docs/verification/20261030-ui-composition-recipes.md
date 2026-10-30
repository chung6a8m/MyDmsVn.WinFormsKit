# UI Composition Recipes — candidate evidence

## Candidate và phạm vi

- Ngày 30/10/2026; baseline toolkit `7a1467f` (activation trên `2521dfe`), nhánh `codex/composition-recipes-20261030`.
- CMP-T01 tạo UIC-01, route Compositions, focused tests và guide. CMP-T02 có record riêng bên dưới khi tích hợp.
- Runtime MyDmsVn vẫn pin `433e5b53f979e37cc6b69bad6dfcde0975c7ecaf`; không thay production core API, packages/projects, SourceGrid hoặc product source.
- Candidate source/test được Owner duyệt và đã publish `1420f252ca198cd4409e0e945fa6433b30b943b3`; các source/test `4bbdeb56f40ead9017098eef787ff605fafeebc4` và `4c4466dca9f8e543119ee9608d509a4fadb104c0` là các revision triển khai trước đó, không phải candidate cuối.
- Owner **APPROVED** `1420f25` ngày 2026-10-30 cho hai Gallery recipes; attended matrix chưa được ghi đủ vẫn OPEN. Decision không cấp app acceptance.

## CMP-T01 — focused automation

Project: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj`; configuration Release, filter `FullyQualifiedName~CompositionDemoFormTests`, STA/NonParallelizable, global theme khôi phục trong teardown. Native unexpected exceptions dùng môi trường test hiện hữu; grid test có `DataGridViewTestGuard`.

Commands thực chạy từ root toolkit:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~CompositionDemoFormTests --logger 'trx;LogFileName=uic01-red-net48.trx' --results-directory artifacts/verification/compositions --blame-hang --blame-hang-timeout 5m
# Sau implementation, thêm --no-restore, đổi LogFileName uic01-green-net48.trx.
# Target còn lại: -f net8.0-windows, LogFileName uic01-green-net8.trx.
```

| Run | Discovered/executed | PASS | FAIL | SKIP | Kết quả |
|---|---:|---:|---:|---:|---|
| RED net48 trước source mẫu | 3 | 0 | 3 | 0 | Expected assertion: không có `CompositionListDemoForm`, không phải build/environment failure |
| GREEN net48 sau source và thêm native test | 4 | 4 | 0 | 0 | PASS, 2s test duration |
| GREEN net8.0-windows | 4 | 4 | 0 | 0 | PASS, 1s test duration |

Cases:

- `FilterResults_QueryChangesRowsCountWithoutResettingSearchBuffer`: search/status → 2/4; giữ raw text `  Kho  `, selection3/2 và focus; read-only; unavailable không tổng0.
- `FilterResults_ClearAndEmptyStatesAreDistinct`: clear, empty0/0 khác no-match0/4, loading count chưa biết, query không có command ghi.
- `FilterResults_NativePerCharacterEnterAndTabKeepQueryState`: `SendKeys.SendWait` từng K/h/o trên native editor đang focus, mỗi phím quan sát text/caret/range/focus/3rows/summary; Enter giữ query, Tab tới status và Shift+Tab về search. Không gán whole-string để thay per-key proof.
- `FilterResults_ThemeNarrowLayoutAndPageDisposalPreserveOwnership`: 480×430 client, Dark giữ control/input/selected key, grid còn diện tích; MainForm đổi page dispose child, đổi theme sau dispose không callback lên sample cũ.

Raw TRX/logs chỉ giữ local tại `artifacts/verification/compositions`. Đây là focused test scope của sample/route, không full toolkit suite hoặc SQL. Các native Gallery gates physical100/125, hai TFM, Light/Dark và viewport cụ thể chưa được record bởi automation trên; root review sẽ bổ sung quan sát thực. Không chuyển kết quả cũ từ TAB/BUS/input-popup sang mẫu mới.

## CMP-T02 — focused automation

Project/configuration như CMP-T01; fixture riêng `CompositionEditDemoFormTests`, STA/NonParallelizable và restore global theme. Tách fixture theo form để giữ ownership rõ; không thêm project/harness.

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --no-restore --list-tests --filter FullyQualifiedName~CompositionEditDemoFormTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --no-restore --filter FullyQualifiedName~CompositionEditDemoFormTests --logger 'trx;LogFileName=uic02-red.trx' --results-directory TestResults/composition-uic02-red --blame-hang --blame-hang-timeout 5m
# GREEN: kết quả TestResults/composition-uic02-green-net48/uic02-green-net48.trx;
# net8.0-windows: discovery 5 và TestResults/composition-uic02-green-net8/uic02-green-net8.trx.
```

| Run | Discovered/executed | PASS | FAIL | SKIP | Kết quả |
|---|---:|---:|---:|---:|---|
| RED net48 trước source mẫu | 5 | 0 | 5 | 0 | Expected missing `CompositionEditDemoForm` assertion |
| GREEN net48 | 5 | 5 | 0 | 0 | PASS, 2s test duration |
| GREEN net8.0-windows | 5 | 5 | 0 | 0 | PASS, 1s test duration |

Cases: invalid Save giữ draft, không tăng feedback count và focus lỗi đầu; correct field gỡ đúng lỗi; valid Save lưu đủ năm trường đúng một lần; invalid tiếp rồi Cancel phục hồi committed snapshot; theme chuyển từ mode đối diện sang Light/Dark giữ draft/native handle/range/group selection; client360×320 giữ commands trong viewport và scroll fields; dispose ngừng global callback; native từng phím ABC, Tab/Shift+Tab và Save/edit/Cancel. Hai `TestCase` Light/Dark là hai rows thực thi trong tổng5, không tính thành một case.

## Integration / phạm vi bổ sung

Đăng ký thêm Compositions ảnh hưởng catalog routes của `IntegratedDemoApplicationTests`: run riêng test `Phase14MainDemoUsesFrameworkSidebarWithAllRequiredPages` cho 1 expected FAIL vì route mới thừa so với danh sách26 cũ. Đã cập nhật danh sách27 routes và giữ Theme là trang mở đầu; test composition kiểm cả hai tabs cùng được tạo/dispose khi chuyển page. Selected regression chỉ mở rộng tới host hiện hữu bị ảnh hưởng, không toàn toolkit/SQL.

## Native Gallery / quyết định

Ngày 30/10/2026 chạy actual Gallery EXE `--composition-proof` trên Windows 10.0.26300, x64, .NET Framework 4.8.10008.0 và .NET 8.0.33, SDK 8.0.427. Hai lượt exit0, mỗi lượt tạo 14 ảnh Light/Dark cho hai tab, normal/small và no-match/unavailable/invalid. `DeviceDpi=96`, `GetDpiForWindow=96` ở màn hình 1920×1080, work area 1920×1032. Outer 1280×800 → client 1264×761, content 1006×641; outer 900×600 → client 884×561, content 626×441. Ảnh và metadata local ở `artifacts/verification/compositions/gallery-net48/` và `gallery-net8.0-windows/`; không commit binary/raw logs. Ảnh là capture, không thay attended native/Owner review.

Quan sát bốn ảnh đại diện: small UIC-01 loaded và unavailable, small UIC-02 invalid, normal UIC-02 Dark; filter wrap, thông báo chưa biết count, errors nhiều dòng và Save/Cancel nhìn thấy; grid cuộn ngang cho mã dài. Các ô ảnh khác được tạo nhưng chưa được kiểm riêng. Automation native per-key có trong focused tests; chưa có bằng chứng tương ứng ở physical125. Gallery 125% **NOT RUN**: desktop hiện tại 96 DPI, Windows Settings không cung cấp cửa sổ điều khiển được. Owner acceptance **PENDING**; không suy PASS từ capture.

Integrated regression trên source candidate `4c4466d`: filter `FullyQualifiedName~CompositionDemoFormTests|FullyQualifiedName~CompositionEditDemoFormTests|FullyQualifiedName~IntegratedDemoApplicationTests`, Release, `--no-restore`, `--blame-hang --blame-hang-timeout 5m`, TRX local `artifacts/verification/compositions/final-<TFM>.trx`: net48 **23 passed / 0 failed / 0 skipped** (10s); net8.0-windows **23 / 0 / 0** (9s). Includes route catalog và disposal hai tab. Gallery proof build hai TFM 0 warning/0 error. EXE SHA256 net48 `858F18AF85BEE0B031D460AD16879F01D80AB779372423BC3626DEFA6756BA35`; net8 apphost `6068A8A97425F9B2D4F33840C9FA71F2CC08F917262376503F72F7F865763A80` ; net8 managed DLL SHA256 `3D1792F16C1ADC0604D8998014F181349FD6D73F0C868465846FD9AB136141CA`.

## PR #6 — P2 review corrections

Review tại `8123ef6` phát hiện free-text search tính cả trạng thái và Gallery proof ép outer 1280×800 vượt working area của màn hình thấp. Candidate correction giới hạn search theo Code/Name/Group; StatusFilter vẫn độc lập. Proof fit desired size vào `Screen.WorkingArea`, ghi requested/fitted và từ chối rõ khi working area nhỏ hơn Gallery minimum; capture cũng kiểm working area thay vì monitor bounds.

Trước sửa, hai regression mới chạy net48: 0 PASS / 2 expected FAIL (status-only query trả 4 rows; hàm fit chưa có). Sau sửa, Release focused và Gallery integration cùng selector đã ghi ở trên: net48 25 PASS / 0 FAIL / 0 SKIP; net8.0-windows 25 / 0 / 0, TRX `review-green-<TFM>.trx` local. Regression kiểm working area giả lập 1366×728 cho normal 1280×728 và small 900×600, cùng boundary nhỏ hơn minimum. Đây là test kích thước, không phải physical-DPI observation. Actual net48 Gallery proof tại 96 DPI exit0, 14 captures local trong `review-proof-net48`; không có màn hình thấp vật lý được kiểm. Physical125 và Owner acceptance vẫn PENDING; evidence ảnh của candidate trước không tự chứng minh visual acceptance cho correction.

## Physical 125% — 30/10/2026, PR #6 candidate `72a73ecbc6201d271f17480c731ddb3e09cfb3eb`

Windows Display hiện chọn 125%; actual Gallery `DeviceDpi=120` và `GetDpiForWindow=120` trên cả hai target. Windows 10.0.26300 x64, SDK 8.0.427, .NET Framework 4.8.10008.0 / .NET 8.0.33; monitor 1920×1080, work area 1920×1020. Outer 1280×800 → form client 1262×753, content 941×626; outer 900×600 → form client 882×553, content 561×426. Đây là geometry thực trong các lượt proof, không quy đổi từ screenshot. EXE net48 SHA256 `56E2CB133870797F4EBAA30405FF8325236CC86FDEDFCB1A4F314417C53411E4`; net8 managed DLL SHA256 `4D854F2FF4BFEB16930F090E1802BD187412EFBD5415BE489C6B0922B9D10E4B`.

Actual Gallery proof, `--composition-proof`, sau khi đóng Settings: exit0, 14 captures cho mỗi target tại `artifacts/verification/compositions/physical125-gallery-retry-<TFM>/`. Bao gồm Light/Dark × normal/small với loaded, valid và small no-match/unavailable/invalid. Captures và metadata local; không commit ảnh/raw TRX. Các ảnh đã quan sát: net48 Light-small UIC-01 loaded, Light-small UIC-02 invalid, Light-normal UIC-02 valid; net8 Dark-small UIC-01 unavailable, Dark-small UIC-02 invalid. Filter wrap, trạng thái unknown, errors và footer được thể hiện; lỗi thứ hai ở viewport nhỏ cần cuộn. **Finding OPEN:** ở outer 900×600/120 DPI, header chung Gallery chồng nút Menu lên label/selector theme và tiêu đề trang bị che. Không coi matrix visual là PASS; chưa quan sát riêng toàn bộ ảnh, chưa có attended Owner review.

Native test command cùng selector integration ở phần trên: net48 25 PASS / 0 FAIL / 0 SKIP. net8 lượt đầu 24 PASS / 1 FAIL / 0 SKIP: `FilterResults_NativePerCharacterEnterAndTabKeepQueryState` không nhận ký tự trong search; Settings đang mở. Chưa xác định chắc nguyên nhân. Sau đóng Settings, test đó chạy riêng 1 PASS; chạy lại toàn selector 25 PASS / 0 FAIL / 0 SKIP. Không xóa failure đầu và không gán nguyên nhân focus là kết luận đã chứng minh. Raw TRX local `physical125-net48.trx`, `physical125-net8.0-windows.trx`, `physical125-net8-retry.trx`, `physical125-net8-suite-retry.trx`. Các testhost được chạy trên desktop physical125; metadata DPI chính xác được xác nhận ở Gallery EXE, không suy testhost DPI.

Lượt proof đầu net48 fail do Gallery không foreground, từ chối capture; giữ log local `physical125-gallery-net48/composition-proof.txt`. Lượt sau đóng Settings chạy thành công như trên. Owner visual acceptance **PENDING**; finding header và failure đầu chưa được đóng bằng retry. Các kết quả NOT RUN trước đó là lịch sử trước lượt này.

## Gallery header correction — physical 125%, 30/10/2026

Source/test candidate `def2116c679f149c2763812dedd980191b481fa4`, base `4a1f02f`. Header đặt Menu/title và settings ở hai hàng, settings wrap khi hẹp; không thay theme controls hay composition ownership. Regression `NarrowHeaderKeepsTitleMenuAndSettingsVisibleWithoutOverlap` tại net48 trước sửa FAIL 1/1 vì title width 29; sau sửa focused typography 15/15 PASS. Integrated selector `CompositionDemoFormTests|CompositionEditDemoFormTests|IntegratedDemoApplicationTests|IntegratedDemoTypographyLayoutTests` Release, `--no-restore`, `--blame-hang --blame-hang-timeout 5m`: net48 40 PASS / 0 FAIL / 0 SKIP; net8.0-windows 40 / 0 / 0. TRX local `header-red.trx`, `header-final-net48.trx`, `header-final-net8.0-windows.trx`.

Actual Gallery proof tại physical 125%, DeviceDpi/WindowDpi 120, Windows 10.0.26300 x64, SDK 8.0.427: cả net48/net8 exit0, mỗi target 14 captures tại `artifacts/verification/compositions/header-fixed125-<TFM>/`. Outer 900×600, client 882×553, content 561×358; outer 1280×800, client 1262×753, content 941×558; monitor 1920×1080, working area 1920×1020. Vùng content nhỏ giảm 68 px chiều cao so với candidate trước do header cao hơn. Đã xem net48 Light-small UIC-01 loaded và net8 Dark-small UIC-02 invalid: Menu/title/theme không chồng, reduced-motion wrap, filters wrap, grid và footer nhìn thấy; nội dung/error còn lại dùng scroll. Các ảnh khác chưa được kiểm riêng. Binary SHA256 net48 EXE `91288200E4153571A80829C8AFCCFB1D7680684BFD1D19D920E51934BC2D2CC9`; net8 managed DLL `C7D03962B3E8CD3E050BC209750E7393A30FD8F6B4C6F8784C3901787683CA54`.

Lỗi header được sửa và kiểm ở hai ảnh đại diện; không biến capture thành full attended matrix/Owner approval. Physical100 cho candidate header mới chưa chạy; Owner acceptance vẫn PENDING. Failure và finding của candidate trước giữ ở record trên, không xóa lịch sử. Không đổi app runtime pin.

## Gallery header correction — physical 100%, 30/10/2026

Candidate source/test `def2116c679f149c2763812dedd980191b481fa4`, documentation HEAD khi chạy `71052c24055bec39f1126d75809d56881d119782`. Windows 10.0.26300 x64, SDK 8.0.427, .NET Framework 4.8.10008.0 / .NET 8.0.33; monitor 1920×1080, working area 1920×1032. Actual Gallery EXE tại hai TFM đều report `DeviceDpi=96`, `GetDpiForWindow=96`. Outer 1280×800 → client 1264×761, content 1006×573; outer 900×600 → client 884×561, content 626×373. Content height giảm 68 px so với Gallery 96-DPI baseline trước header correction.

Actual `--composition-proof` exit0 trên mỗi TFM, mỗi lượt tạo 14 captures Light/Dark, normal/small và các states; metadata/ảnh local tại `artifacts/verification/compositions/header-physical100-<TFM>/`. Đã trực tiếp xem net48 Light-small UIC-01 loaded, net8 Dark-small UIC-02 invalid và net48 Dark-normal UIC-02 valid: Menu/title/theme không chồng; filter wrap, grid và Save/Cancel nhìn thấy; nội dung/lỗi bị khuất trong viewport nhỏ có scroll. Các ảnh khác chưa được kiểm riêng; không coi capture là attended matrix hoặc Owner approval.

Focused `IntegratedDemoTypographyLayoutTests`, Release, `--no-restore`, `--blame-hang --blame-hang-timeout 5m`, test run trên desktop physical100: net48 15 PASS / 0 FAIL / 0 SKIP; net8.0-windows 15 / 0 / 0. TRX local `header-physical100-net48.trx`, `header-physical100-net8.0-windows.trx`. Testhost DPI không được đo; DPI trên là từ Gallery EXE. Build hashes khớp candidate 125% ở mục trước: net48 EXE `91288200E4153571A80829C8AFCCFB1D7680684BFD1D19D920E51934BC2D2CC9`, net8 managed DLL `C7D03962B3E8CD3E050BC209750E7393A30FD8F6B4C6F8784C3901787683CA54`. Header correction có kiểm tra bounded 100% và 125%; full attended visual matrix và Owner acceptance vẫn PENDING. Các ghi chú NOT RUN ở mục trước mô tả thời điểm trước lượt này.

## Visual review preparation — 30/10/2026

Candidate source/test `1420f252ca198cd4409e0e945fa6433b30b943b3` replaces `def2116` for review. Trong rà soát ảnh 125% small của candidate trước, dòng unavailable bên trong grid bị cắt; summary vẫn ghi trạng thái và count chưa biết. Candidate này rút gọn grid message thành “Không thể tải dữ liệu.”, giữ summary và state/query logic. Regression 480×430 với font scale mô phỏng 1.25 trước sửa FAIL 1/1: text width 475 vượt 385; sau sửa focused/integration selector của header correction chạy Windows Release trên desktop 100%: net48 41 PASS / 0 FAIL / 0 SKIP, net8.0-windows 41 / 0 / 0. TRX local `visual-red-scaled.trx`, `visual-final-net48.trx`, `visual-final-net8.0-windows.trx`; đây là test font scale, không phải bằng chứng physical125.

Actual Gallery `--composition-proof` trên candidate này: hai TFMs, Light/Dark, normal/small, 14 captures mỗi TFM tại cả 100% và 125%, exit0. Gallery `DeviceDpi`/`GetDpiForWindow` lần lượt 96 và 120. Ảnh/metadata local tại `artifacts/verification/compositions/matrix100-<TFM>/` và `matrix125-<TFM>/`. Đã xem 100% net48 Light-small loaded, net8 Dark-small invalid; 125% net48 Dark-small unavailable, net8 Light-small unavailable, net48 Light-normal loaded, net8 Dark-small invalid. Ở hai ảnh unavailable 125% dòng lỗi mới hiển thị đầy đủ; header không chồng trong các ảnh đã xem. Các captures còn lại chưa được review riêng và không trở thành attended PASS. Windows scale đã được khôi phục 100% sau lượt 125%.

Binary hashes: net48 EXE SHA256 `E4CFE57545C62FEBFE134A514322B1582A7E937FCAC5EE10B1E4D6188CE330DD`; net8 managed DLL SHA256 `8134BE2BA5405F99E948CFEF92A8DAE68967131B46B556A49F7F25E568E12BBA`. Trước correction này có quan sát trực tiếp trên Gallery net48 physical100 candidate `def2116`: gõ từng phím k/h/o cho 3/4 và Enter giữ text/focus; chưa kiểm riêng native caret/range bằng observation đó. Automated native input và lifecycle được ghi theo đúng run của chúng, không thay physical attended matrix.

**Attended matrix: OPEN tại thời điểm chuẩn bị review.** Header correction làm content nhỏ còn 626×373 tại100 và 561×358 tại125. Không có product/SQL/release acceptance.

## Owner decision — 30/10/2026

Owner trực tiếp trả lời **“APPROVE candidate `1420f25`”** cho candidate `1420f252ca198cd4409e0e945fa6433b30b943b3`, sau yêu cầu review hai Gallery recipes. Decision là **APPROVED** cho candidate và phạm vi Gallery này; không suy từ ảnh hay test. Owner không cung cấp kết quả theo từng ô attended matrix, nên các ô chưa có quan sát riêng vẫn **OPEN**, không tự ghi PASS hoặc waived. Không mở rộng decision sang product pilot, SQL, package hay release; runtime app pin `433e5b53f979e37cc6b69bad6dfcde0975c7ecaf` giữ độc lập.

## Attended continuation — 30/10/2026

Candidate source/test `1420f252ca198cd4409e0e945fa6433b30b943b3`, documentation HEAD khi bắt đầu `a7d585df651ef6a06a69469d1448b189abacdf63`. Owner xác nhận trước lượt này “Tôi sẽ theo dõi”; sau lượt 100%/125% trả lời **“Đã theo dõi, chưa thấy lỗi”**. Đây là ghi nhận những thao tác cụ thể bên dưới, không khẳng định Owner đã xem từng capture hay từng tổ hợp chưa trình diễn. Owner approval candidate ngày 30/10/2026 giữ nguyên.

- Physical100, `net48`, Light: UIC-01 loaded 4/4; gõ từng phím `k`, `h`, `o` cho 3/4, Enter giữ `kho` và focus; chọn trạng thái đang hoạt động cho 2/4; Clear phục hồi 4/4; dataset rỗng 0/0, loading và unavailable phân biệt, hai trạng thái sau báo chưa biết số kết quả. UIC-02: xóa mã, Save hiện lỗi và focus mã; nhập `DEMO-123`, Save báo lần 1; sửa thành `NHAP-456`, chuyển Dark giữ draft, Cancel phục hồi `DEMO-123`.
- Physical125, `net8.0-windows`: UIC-01 Light/Dark normal; Dark vùng hẹp sau thu chiều ngang, filter wrap và unavailable hiển thị “Không thể tải dữ liệu.” cùng summary chưa biết số kết quả. UIC-02 Dark vùng hẹp: mã rỗng hiện lỗi nhiều dòng và focus mã, footer còn nhìn thấy. Kích thước live thu bằng thao tác cửa sổ, không đo outer/content chính xác; không gán geometry của ảnh proof trước cho cửa sổ này.
- Physical125, `net48`: UIC-01 và UIC-02 Light ở viewport normal. Physical100, `net8.0-windows`: UIC-01 và UIC-02 Light ở viewport normal. Windows Display được đặt 125% trong lượt giữa, sau đó khôi phục 100%; Gallery DPI thực 96/120 của cùng binaries đã được đo trong lượt proof candidate `1420f25` ở mục trước.

Vẫn **OPEN**: các tổ hợp TFM/theme/DPI/viewport chưa trình diễn trực tiếp, no-match khi lọc, caret/range được đo trực tiếp trong lượt attended, cùng những checks chưa quan sát riêng. Automated native tests/captures vẫn giữ giá trị ở phạm vi đã ghi nhưng không thay các ô attended đó. Không ghi full attended matrix PASS chỉ từ xác nhận tổng quát; không mở rộng decision sang product/SQL/release.

Tiếp theo tại physical100, `net8.0-windows` Light: operator nhập `xyz`, grid báo 0/4 và “Không có kết quả phù hợp bộ lọc”; Ctrl+A cho selected text `xyz` và focus vẫn ở search theo UI Automation. Quan sát này diễn ra sau phản hồi Owner phía trên và chưa có xác nhận attended riêng; vị trí caret/selection start-length không được đo, không dùng whole-string nhập để tuyên bố per-key proof. Vẫn giữ các ô attended tương ứng OPEN.

## Attended continuation — physical 125% — 30/10/2026

Windows Display do Owner đặt 125% trước lượt này. Operator mở Gallery candidate `1420f252ca198cd4409e0e945fa6433b30b943b3` và trình diễn hai TFM ở viewport normal: `net8.0-windows` UIC-01 và UIC-02 ở Light/Dark; `net48` UIC-01 và UIC-02 ở Light/Dark. Các nội dung nhìn thấy gồm nhãn tiếng Việt dài, bảng kết quả, biểu mẫu, vùng ghi chú và Save/Cancel. Owner xác nhận sau lượt này: **“Tôi đã theo dõi lượt 125% và không thấy lỗi”**. Không quan sát thấy header chồng, nhãn bị cắt hoặc lệnh chính bị khuất trong lượt này. Sau đó Windows Display được đặt lại 100% và Settings hiển thị 100%.

Lượt normal 125% nêu trên là **attended PASS** trong phạm vi đã trình diễn. Matrix và các ô nhỏ/no-match/error/caret chưa được trình diễn trong lượt này vẫn **OPEN**; không thay đổi Owner approval candidate hoặc phạm vi product/SQL/release.
