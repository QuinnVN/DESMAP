# DESMAP

Monorepo gồm `web/`, `ai/` và `vr/`, nhập từ ba repo bằng `git subtree`. Lịch sử sau đó được làm sạch bằng `git filter-repo` để loại asset bên thứ ba, output QA và Git LFS.

| Thư mục | Công cụ | Dependency |
| --- | --- | --- |
| `web/` | Node.js 24.x, pnpm 12.4.1 | Giữ nguyên package manifest và lockfile pnpm |
| `ai/` | Python 3.14 | `requirements.lock.txt` giữ đủ 47 phiên bản runtime của môi trường nguồn |
| `vr/` | Unity 6000.5.4f1 | Unity Package Manager và asset cục bộ trong `Assets/ThirdParty/` |

## Web

```powershell
pnpm --dir .\web install --frozen-lockfile
pnpm --dir .\web dev
pnpm --dir .\web check
pnpm --dir .\web exec vitest run src
pnpm --dir .\web build
```

Web quản lý dependency trong `web/`, không có pnpm workspace ở gốc. Giữ cả hai tài liệu YAML trong lockfile hiện tại. `.env` cục bộ bị ignore. Khi triển khai Web, đặt Root Directory là `web`.

Lệnh `pnpm --dir web test` còn chạy script tái hiện lỗi trong `.scratch/mongodb-schema-repro.test.ts`, cần `MONGODB_URI`. Lệnh Vitest ở trên chạy bộ unit test ứng dụng trong `src/`.

## AI

```powershell
Set-Location .\ai
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.lock.txt
.\.venv\Scripts\python.exe -m app.main
.\.venv\Scripts\python.exe .\scripts\run_tests.py
```

`requirements.txt` gốc được giữ nguyên; `requirements.lock.txt` bổ sung 20 dependency gián tiếp cho Windows x64/Python 3.14. `.env`, môi trường ảo, model, recording và runtime backup nằm ngoài Git.

## Unity VR

Mở `vr/` bằng Unity 6000.5.4f1 sau khi khôi phục asset bên thứ ba. Các asset này nằm trên máy tại `vr/Assets/ThirdParty/`, được ignore cùng `.meta` của thư mục cha. Danh sách thư mục và GUID nằm trong `vr/ThirdPartyAssets.json`.

Ví dụ khôi phục từ bản backup cục bộ trước khi làm sạch:

```powershell
.\vr\Restore-ThirdParty.ps1 -Source "<thư mục backup>\vr\Assets"
```

Script hỗ trợ nguồn là thư mục `Assets` cũ hoặc thư mục `Assets/ThirdParty` đã gom, chuyển kèm `.meta` và kiểm tra GUID. Xem [hướng dẫn VR](vr/README.md) cho các thao tác Unity.

```powershell
.\vr\Build.ps1 -Target Validate
.\vr\Build.ps1 -Target EditModeTests
.\vr\Build.ps1 -Target PlayModeTests
```

NativeWebSocket giữ nguyên trong `vr/Assets/WebSocket/` và `vr/Assets/package.json`. Asset tự tạo, scene, prefab và GUID vẫn được quản lý trong Git. Binary được giữ lại dùng Git thường; clone/push repo này không cần Git LFS.

`Library/`, `Temp/`, `Build/`, `Builds/`, `Logs/`, `UserSettings/`, `Artifacts/`, `TestResults/`, screenshot QA và scene `_Recovery/` nằm ngoài Git.

## Lịch sử và bản sao dự phòng

Ba repo gốc vẫn giữ nguyên tại `<repo NCKH Web>`, `<repo NCKH AI>` và `<repo NCKH VR>`.

Bản sao trước khi làm sạch: `<thư mục backup>`. Bản này chứa working copy, Git/LFS và `all-refs.bundle`. Mọi branch/tag trong monorepo đã được lọc; SHA commit trước khi lọc chỉ còn dùng để tra cứu trong backup.

Khi đồng bộ tiếp từ repo gốc, hãy lọc bản xuất nguồn theo cùng chính sách trước khi dùng subtree. Pull trực tiếp lịch sử chưa lọc sẽ đưa lại asset bị loại và pointer LFS cũ vào monorepo.

Thông tin gốc nằm trong [sources.json](docs/migration/sources.json). [Báo cáo nhập repo ban đầu](docs/migration/verification.md) là snapshot trước lần làm sạch này. Kết quả kiểm tra lần làm sạch sẽ được ghi trong `docs/migration/public-cleanup.json`.
