# DESMAP

Monorepo gồm ba dự án được nhập bằng `git subtree add` và giữ đầy đủ lịch sử commit, không dùng `--squash`.

| Thư mục | Dự án | Dependency và công cụ |
| --- | --- | --- |
| `web/` | SvelteKit và TypeScript | Node.js 24.x, pnpm 12.4.1; giữ nguyên `package.json`, `.npmrc`, `pnpm-workspace.yaml` và `pnpm-lock.yaml` |
| `ai/` | Backend Python và FastAPI | Python 3.14; giữ nguyên `requirements.txt`, thêm `requirements.lock.txt` cho 47 package trong môi trường nguồn |
| `vr/` | Unity VR | Unity 6000.5.4f1; giữ nguyên `Assets/`, file `.meta`, `Packages/` và `ProjectSettings/` |

Chạy lệnh từ thư mục gốc repo, trừ khi có hướng dẫn chuyển thư mục. Dependency JavaScript nằm trong `web/`; không có pnpm workspace hoặc `package.json` ở gốc. Backend Python dùng `ai/requirements.txt` độc lập với pnpm. Các dependency của Unity được quản lý bằng Unity Package Manager.

## Web

```powershell
pnpm --dir .\web install --frozen-lockfile
pnpm --dir .\web dev
```

```powershell
pnpm --dir .\web check
pnpm --dir .\web exec vitest run src
pnpm --dir .\web build
```

`pnpm --dir .\web test` còn chạy script tái hiện lỗi tại `.scratch/mongodb-schema-repro.test.ts`; script này cần `MONGODB_URI` và hiện làm lệnh test mặc định thất bại. Lệnh `vitest run src` ở trên chỉ chạy 74 unit test của ứng dụng. Mã nguồn và script test gốc được giữ nguyên.

Giữ cả hai tài liệu YAML trong lockfile pnpm hiện có. Không đổi lockfile bằng trình đọc chỉ hỗ trợ một tài liệu YAML. Cấu hình `.npmrc` của Web yêu cầu đúng Node.js 24.x; `packageManager` chọn pnpm 12.4.1.

Nếu cần tạo cấu hình mới, sao chép `web/.env.example` thành `web/.env`. Bản di chuyển đã sao chép `.env` cục bộ; Git bỏ qua file này. Cấu hình Vercel hoặc dịch vụ triển khai Web cần dùng `web` làm Root Directory.

## AI

`requirements.lock.txt` tham chiếu nguyên bản `requirements.txt` và bổ sung 20 dependency gián tiếp lấy từ môi trường AI gốc. Snapshot này dành cho Windows x64 và Python 3.14, giữ đúng 47 phiên bản runtime đã cài ở repo nguồn.

Tạo môi trường mới, không sao chép `.venv` từ đường dẫn cũ:

```powershell
Set-Location .\ai
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.lock.txt
.\.venv\Scripts\python.exe -m app.main
```

Chạy test từ `ai/` bằng runner có chặn HTTP thật và không nạp credential cục bộ:

```powershell
.\.venv\Scripts\python.exe .\scripts\run_tests.py
```

`ai/.env`, `ai/models/` và `ai/recordings/` được sao chép từ bản dự phòng và tiếp tục nằm ngoài Git. `ai/backup/` được giữ trên đĩa nhưng đã bỏ theo dõi. Các file trong `ai/data/`, prompt và mã nguồn vẫn được quản lý trong Git. Xem thêm [hướng dẫn backend](ai/readme.md).

## Unity VR

Mở `vr/` bằng Unity Hub, dùng phiên bản trong `vr/ProjectSettings/ProjectVersion.txt`. Unity sẽ tạo lại `Library/` và các cache cần thiết.

```powershell
Set-Location .\vr
powershell -ExecutionPolicy Bypass -File .\Build.ps1 -Target Validate
```

`Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`, `UserSettings/`, `Artifacts/`, `TestResults/` và các solution/project C# được sinh tự động đều bị ignore. Nội dung `Artifacts/` đã có trong Git trước đây vẫn còn trong lịch sử và trên đĩa, nhưng đã bỏ theo dõi ở commit hiện tại. File `.meta` và GUID của các asset gốc được giữ nguyên.

Asset nhị phân Unity dùng Git LFS. Toàn bộ kho LFS có sẵn đã được sao chép vào `.git/lfs/objects`; quy tắc LFS gốc vẫn nằm tại `vr/.gitattributes`. Khi clone từ remote trong tương lai:

```powershell
git lfs install
git lfs pull
```

Khi đưa monorepo lên remote lần đầu, cần đẩy cả Git và các object LFS tới remote mới. Repo này hiện chỉ ở máy cục bộ và chưa có remote `origin`.

## Nguồn và lịch sử

Các repo gốc được giữ nguyên:

- Web: `<repo NCKH Web>`
- AI: `<repo NCKH AI>`
- Unity: `<repo NCKH VR>`

Đường dẫn bản sao dự phòng, commit nguồn và danh sách thư mục không sao chép được ghi tại [sources.json](docs/migration/sources.json). Bản dự phòng có cả `.git`, LFS và dữ liệu cục bộ; không chép cache dependency, môi trường ảo hay output build có thể tạo lại. Ba tag `import/web-20261028`, `import/ai-20261028` và `import/vr-20261028` trỏ tới commit nguồn tương ứng.

Để nhập thêm commit từ repo gốc vào monorepo, chạy từ gốc monorepo với working tree sạch:

```powershell
git subtree pull --prefix=web "<repo NCKH Web>" main
git subtree pull --prefix=ai "<repo NCKH AI>" main
git subtree pull --prefix=vr "<repo NCKH VR>" main
```

Các lệnh trên đọc repo nguồn và cập nhật monorepo. Nếu repo nguồn tiếp tục thay đổi các file đã bỏ theo dõi như `Artifacts/` hoặc `backup/`, xử lý xung đột bằng cách giữ chúng ngoài Git tại monorepo.
