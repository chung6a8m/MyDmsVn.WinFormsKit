# TAB-06 — manifest visual artifacts

Ngày upload: 29/10/2026. [Comment PR #4](https://github.com/chung6a8m/MyDmsVn.WinFormsKit/pull/4#issuecomment-6500393196). Code/contract candidate `e780da516d04d8c3637215f6bdcbf2f04618bed0`; lượt physical 125% trên `f2192fbf85fdd22d826b3767db3d86400a768f60` chỉ thêm tài liệu, runtime source không đổi. Gitlink SourceGrid `f4e457b43582bf01892f50bdc74aa480531e5944`.

VisualStyleHost Release `tabs-matrix` chụp Light/Dark header ở physical 100% (HWND DPI 96) và 125% (HWND DPI 120) trên `net48`/`net8.0-windows`; fallback 125% là `Bottom+RTL+Buttons+Multiline`. `tabs-hover` cung cấp ba frame `before/after/final` theo thứ tự ở physical 125%; các frame tĩnh không chứng minh không nhấp nháy theo thời gian. Xem [report chính](20261029-tabs-hottrack.md) cho OS/runtime/SDK/bitness, commands, counts, native trace và limitations.

Các ảnh gắn vào PR qua GitHub user attachments. Đã mở lại 18/18 URL bằng GET không đăng nhập: HTTP 200 và SHA-256 byte tải về khớp file gốc. URL sống độc lập với thư mục Temp local; thời hạn lưu trữ do GitHub quản lý, không có cam kết giữ vô hạn. Raw trace và metadata JSON vẫn chỉ ở local ignored artifacts; hash trace trong report chính.

| File/scenario | URL mở lại | SHA-256 |
|---|---|---|
| `net48-100-Light-hover.png` | [Mở ảnh](https://github.com/user-attachments/assets/be58efeb-b599-438a-bf5b-d5a7c149d6f7) | `74042E3973AC7FF7EE7331E266309DDC4929F2BEF19BB189BB19B4485534E826` |
| `net8-100-Light-hover.png` | [Mở ảnh](https://github.com/user-attachments/assets/72fa5244-2c9f-4d9f-99d9-f27f77f1a89d) | `34B47689B05BC97EF8AF33A7F5A885D18E4C03B93753FAE79FF28A6F8774DFE3` |
| `net48-100-Dark-hover.png` | [Mở ảnh](https://github.com/user-attachments/assets/ee03221a-5c0a-4e1e-be5a-078f55f3645f) | `FED09C6D013E5961AD9F7D678FBF758FBF259ECF8216263B91BB61A883D8E4AC` |
| `net8-100-Dark-hover.png` | [Mở ảnh](https://github.com/user-attachments/assets/23c054c4-e081-4138-8f10-7ad32478eee3) | `F4FFB6F1F18DA60EBB9802BCB772631CB8C5FC4CB55D76F3C8CDEB63B6EC91A0` |
| `net48-125-Light-hover.png` | [Mở ảnh](https://github.com/user-attachments/assets/8b3b0946-f415-4603-a93f-bc3113249e2e) | `9E566BD65612ABDB39F1146BFB90A7D772C2498C54D97DA94BB66C0BAF75FA17` |
| `net8-125-Light-hover.png` | [Mở ảnh](https://github.com/user-attachments/assets/d013b9d6-04d3-4103-ae56-8d3cfa85942b) | `1FD0C99E6CFC29F34ED5A0C8834AAE0F2E92EC5B78C52E3F476FAD7B9391FFA9` |
| `net48-125-Dark-hover.png` | [Mở ảnh](https://github.com/user-attachments/assets/d1ebaa6c-e68a-4fca-ab19-d3dca5a8308b) | `1595229F5E27050174F05F36F73D7C9F5AD80D10E5B3BBDEB016A054FD84FB04` |
| `net8-125-Dark-hover.png` | [Mở ảnh](https://github.com/user-attachments/assets/ce5aaff4-71c5-460e-b8fe-1283d430499a) | `5D10599F1ECD572D0CD862AD5137472731E588462FB875B96AB2905E75459FFE` |
| `net48-125-Light-fallback.png` | [Mở ảnh](https://github.com/user-attachments/assets/f343331d-8e40-4307-a719-fb57d6e98f53) | `82167C55A0FA6465D87BE360CE0DA5851460786EA4830152C42DDC265B483DF1` |
| `net8-125-Light-fallback.png` | [Mở ảnh](https://github.com/user-attachments/assets/53956a06-c7a4-423d-b2e8-24204cf029dc) | `FE3141661DE860A918E712B826010B2633802CA394E483E45C685D75634DC849` |
| `net48-125-Dark-fallback.png` | [Mở ảnh](https://github.com/user-attachments/assets/140bb982-2889-4758-88cb-89843d885264) | `FEC476B13D781F24532A69EADCE8F9F282C7F1FE38CE4648A5AFB031875B1221` |
| `net8-125-Dark-fallback.png` | [Mở ảnh](https://github.com/user-attachments/assets/817db41c-9e1c-495c-8ebd-b6d646906545) | `914876755E827C966239299C6D0AFA48397D63A87FD723DC71831FB63DABDD7D` |
| `net48-125-seq-before.png` | [Mở ảnh](https://github.com/user-attachments/assets/3c513683-dc34-4a76-a0d9-cb7f3a8f86d6) | `B055F6118399257766272FE584F1EB00E84FC8CC34DD2F8DC139FA24CC2B5660` |
| `net48-125-seq-after.png` | [Mở ảnh](https://github.com/user-attachments/assets/c5bfb7ed-5346-4574-933a-f68d17e602ff) | `10568613874BC77D91857A8370F928132DF82736DC61F456D95D7C68FB7CC36B` |
| `net48-125-seq-final.png` | [Mở ảnh](https://github.com/user-attachments/assets/c3bc562c-aced-4bd2-8c8f-5a07b347dc56) | `306E4F37BC933C6209A7A7D867AF3BC1D3871D466D0FBE7C00092308D0C45AB4` |
| `net8-125-seq-before.png` | [Mở ảnh](https://github.com/user-attachments/assets/75f4e918-844f-4baf-9ab0-24c5f4d14db9) | `A774631AAF66AC03D1AAC67282E90087DD34D362C02D732F900A400C002858B9` |
| `net8-125-seq-after.png` | [Mở ảnh](https://github.com/user-attachments/assets/06d3b4f8-b019-45a3-b868-bb92ba4f00bc) | `8001EBB7A094E7889715BFE887B4A80E1FBE48DBFE725465697017315F27B58D` |
| `net8-125-seq-final.png` | [Mở ảnh](https://github.com/user-attachments/assets/330b9fb7-7db4-42ec-bc4f-4eff085a7cee) | `C87DCC9DF09F570F0287EF86C8D2FD9EEE01C08FC9D561EEA65AED304AB5DFD5` |

Owner visual decision: **PENDING** cho đến khi Owner trả lời trên đúng bộ ảnh/candidate này. Full no-flicker, 150%/200% và mixed-DPI tiếp tục theo [plan TAB](../plans/20261029-002-bootstrap-tabs-hottrack.md).
