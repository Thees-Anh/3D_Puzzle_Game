# 3D Puzzle Room

> Game giải đố escape room góc nhìn thứ nhất, được phát triển bằng Unity cho PC và Android.

![Unity](https://img.shields.io/badge/Unity-6000.2.2f1-000000?logo=unity&logoColor=white)
![Platform](https://img.shields.io/badge/Platform-PC%20%7C%20Android-3DDC84?logo=android&logoColor=white)
![Language](https://img.shields.io/badge/Language-C%23-512BD4?logo=csharp&logoColor=white)

## Giới thiệu

**3D Puzzle Room** đưa người chơi vào một căn phòng bí ẩn bị mất điện. Hãy quan sát môi trường, thu thập vật phẩm, giải các câu đố liên kết với nhau và khôi phục nguồn điện để mở lối thoát.

Trò chơi tập trung vào khám phá, suy luận từ manh mối trong môi trường và tương tác trực tiếp với các đồ vật. Giao diện và hệ thống điều khiển được thiết kế cho cả máy tính lẫn thiết bị cảm ứng.

## Tính năng nổi bật

- Góc nhìn thứ nhất với di chuyển và quan sát tự do.
- Chuỗi câu đố liên kết: két sắt, dãy biểu tượng, Tháp Hà Nội và lưới điện.
- Đèn UV giúp phát hiện các ký hiệu và manh mối ẩn.
- Hệ thống vật phẩm gồm chìa khóa, cầu chì và các vật phẩm nhiệm vụ.
- Tương tác với cửa, tủ, tranh, ghế, hộp cầu chì và vật thể trong phòng.
- Không khí bí ẩn với nhạc nền, âm thanh môi trường và hiệu ứng âm thanh riêng cho từng tương tác.
- Menu chính, menu tạm dừng, màn hình chiến thắng và bộ đếm thời gian hoàn thành.
- Lưu/tiếp tục tiến trình bằng tệp JSON.
- Tùy chỉnh âm lượng, độ nhạy cảm ứng và chất lượng đồ họa.
- Điều khiển mobile với joystick ảo, vùng vuốt để nhìn và giao diện thích ứng vùng an toàn.

## Mục tiêu

Khám phá căn phòng và tìm cách khôi phục nguồn điện. Mỗi câu đố mở ra một manh mối hoặc vật phẩm cần cho bước tiếp theo. Khi điện đã được phục hồi, hãy tìm lối thoát để hoàn thành game.

> README không tiết lộ đáp án cụ thể nhằm giữ nguyên trải nghiệm giải đố.

## Điều khiển

### PC / Unity Editor

| Thao tác | Phím |
| --- | --- |
| Di chuyển | `W`, `A`, `S`, `D` |
| Quan sát | Chuột |
| Tương tác / nhặt vật phẩm | `E` |
| Bật/tắt đèn UV | `F` |
| Tạm dừng / đóng giao diện hiện tại | `Esc` |
| Xóa một ký tự trên keypad | `Backspace` |
| Xác nhận mã keypad | `Enter` |

### Android

- Joystick bên trái: di chuyển.
- Vuốt vùng nhìn: xoay camera.
- Nút tương tác: sử dụng hoặc nhặt vật phẩm.
- Nút UV: bật/tắt đèn UV.
- Các bảng câu đố sử dụng nút cảm ứng trực tiếp trên màn hình.

## Yêu cầu phát triển

- Unity Editor **6000.2.2f1**.
- Unity Hub.
- Module Android Build Support, Android SDK, NDK và OpenJDK nếu muốn build Android.
- Git và Git LFS được khuyến nghị nếu bổ sung thêm nhiều asset dung lượng lớn.

## Cài đặt và chạy dự án

1. Clone repository:

   ```bash
   git clone https://github.com/Thees-Anh/3D_Puzzle_Game.git
   ```

2. Mở Unity Hub, chọn **Add project from disk** và trỏ đến thư mục vừa clone.
3. Mở dự án bằng Unity `6000.2.2f1`.
4. Chờ Unity import toàn bộ asset và package.
5. Mở scene `Assets/Scenes/MainMenu.unity`.
6. Nhấn **Play** để bắt đầu.

## Build game

### PC

1. Mở **File > Build Profiles**.
2. Chọn nền tảng desktop mong muốn.
3. Đảm bảo hai scene được bật và đúng thứ tự:
   - `Assets/Scenes/MainMenu.unity`
   - `Assets/Scenes/PuzzleRoom.unity`
4. Chọn **Build** hoặc **Build and Run**.

### Android

1. Cài Android Build Support trong Unity Hub.
2. Mở **File > Build Profiles** và chọn **Android**.
3. Chọn **Switch Platform** nếu cần.
4. Kiểm tra Player Settings và thiết bị ký ứng dụng cho bản phát hành.
5. Chọn **Build** để tạo APK/AAB.

Cấu hình hiện tại sử dụng package name `com.defaultcompany.puzzleroom3d`, phiên bản `1.0`, Android API tối thiểu 23 và kiến trúc ARM64. Nên đổi company name, application identifier và keystore trước khi phát hành chính thức.

## Cấu trúc dự án

```text
3D Puzzle Room/
├── Assets/
│   ├── Art/                 # Model, material, texture và prefab hình ảnh
│   ├── Audio/               # Nhạc nền, ambience, SFX và audio mixer
│   ├── Materials/           # Material dùng trong gameplay
│   ├── Prefabs/             # Prefab gameplay dùng chung
│   ├── Scenes/              # MainMenu và PuzzleRoom
│   └── Scripts/
│       ├── Audio/           # Audio manager và audio cue
│       ├── Core/            # Luồng game, lưu game, trạng thái nguồn điện
│       ├── Editor/          # Công cụ dựng scene và cấu hình trong Editor
│       ├── Interaction/     # Cửa, tủ, vật phẩm và tương tác môi trường
│       ├── Mobile/          # Joystick, touch look và mobile HUD
│       ├── Player/          # Di chuyển, camera, inventory và đèn UV
│       ├── Puzzles/         # Logic các câu đố
│       └── UI/              # Menu, keypad, hotbar và màn hình chiến thắng
├── Packages/                # Unity Package Manager manifest
└── ProjectSettings/         # Cấu hình dự án Unity
```

## Kiến trúc gameplay

- `GameFlowManager` quản lý trạng thái hoàn thành, chơi lại và trở về menu.
- `SaveGameSystem` lưu trạng thái người chơi, inventory và tiến độ câu đố vào `puzzle-room-save.json` trong `Application.persistentDataPath`.
- `PlayerInputProvider` hợp nhất input desktop và mobile.
- `IInteractable` tạo giao diện chung cho các vật thể có thể tương tác.
- `PuzzleBase` cung cấp vòng đời và trạng thái hoàn thành chung cho câu đố.
- `AudioManager` và `AudioCue` điều phối nhạc, ambience và hiệu ứng âm thanh.

## Nội dung câu đố

| Hệ thống | Mô tả |
| --- | --- |
| Safe Puzzle | Nhập mã tìm được từ manh mối trong phòng để mở két |
| Symbol Sequence | Quan sát và nhập đúng chuỗi ký hiệu |
| Tower of Hanoi | Di chuyển các cuốn sách theo quy tắc Tháp Hà Nội |
| Power Grid | Lắp cầu chì, nối mạch và kích hoạt các kênh điện đúng thứ tự |
| UV Clues | Dùng ánh sáng UV để làm lộ chi tiết bị che giấu |

## Lưu tiến trình

Trong menu tạm dừng, chọn **Save** để lưu tiến trình. Nút **Continue** ở menu chính sẽ nạp bản lưu hợp lệ gần nhất. Bản lưu bao gồm vị trí/góc nhìn người chơi, vật phẩm, trạng thái nguồn điện và tiến độ của các câu đố.

## Đóng góp

1. Fork repository.
2. Tạo branch mới: `git checkout -b feature/ten-tinh-nang`.
3. Commit thay đổi: `git commit -m "feat: mô tả thay đổi"`.
4. Push branch và mở Pull Request.

Khi đóng góp asset nhị phân dung lượng lớn, hãy cân nhắc sử dụng Git LFS.

## Tác giả

Phát triển bởi [Thees-Anh](https://github.com/Thees-Anh).

## Giấy phép

Dự án hiện chưa công bố giấy phép mã nguồn. Mọi quyền được bảo lưu bởi tác giả. Vui lòng liên hệ chủ sở hữu trước khi sao chép, phân phối hoặc sử dụng dự án cho mục đích thương mại.
