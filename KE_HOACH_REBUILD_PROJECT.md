# KẾ HOẠCH DỌN DẸP, TỔ CHỨC LẠI & LÀM LẠI PROJECT: BOLT PUZZLE 3D (FULL FEATURE & PLAYABLE-READY)

> **Thông tin Project hiện tại**
> - **Đường dẫn:** `/Users/mac/Downloads/3D+Bolt+Master®_+Screw+Master®_1.4.9_APKPure/AssetRipper_export_20261111_091149/BoltPuzzle3D`
> - **Phiên bản Unity:** `6000.2.10f1` (Unity 6)
> - **Nguồn gốc:** Export từ APK IL2CPP bằng AssetRipper (tổng dung lượng `Assets/` ~ **402 MB**).
> - **Tình trạng kỹ thuật:**
>   1. **Rác & phân mảnh thư mục:** AssetRipper tách rời tài nguyên thành 18 thư mục cấp 1 lộn xộn (`AudioClip`, `Material`, `Mesh`, `MonoBehaviour`, `PhysicMaterial`, `Shader`, `Sprite`, `TextAsset`, `Texture2D`, `Snui`, `Sugar`, `ShopUI`, `Screw3D`...), và đổ phẳng hơn 2,300 file `.cs` vào `Assets/Scripts/Assembly-CSharp`.
>   2. **Dummy Scripts:** Toàn bộ file `.cs` đang rỗng thân hàm do IL2CPP stripping, nhưng **GUID trong file `.meta` và dữ liệu Serialized trên Scene / Prefab vẫn còn nguyên vẹn 100%**.

---

## PHẦN 1: PHÂN TÍCH HIỆN TRẠNG & CÁC PHẦN THỪA CẦN XÓA

### 1.1. Bảng phân tích dung lượng `Assets/` (~402 MB)
| Thư mục | Dung lượng | Hiện trạng & Đánh giá | Hành động đề xuất |
| :--- | :---: | :--- | :--- |
| `Assets/Snui/Events` | **123 MB** | Chứa 18 LiveOps Events phụ (`Lava`, `Pass`, `TeamBattle`, `MissionCenter`, `KeyHunter`, `FlowerRush`, `UnicornRace`, `SurfingRush`, `SpaceRace`, `CardCollection`, `StreakLine`...) | **XÓA 100%** (Trừ các icon/prefab IAP nếu cần dùng cho Shop) |
| `Assets/Snui/` (Các mục phụ) | **~18 MB** | `Team` (2.2M), `Rank` (1.9M), `FacebookJoin` (1.2M), `Profile` (1.1M), `Account` (768K), `Permissions` (580K), `Feedback` (440K), `Cheat` (392K), `Localization` (3.7M) | **XÓA** các module online/social không cần thiết; **GIỮ LẠI** `Base`, `Common`, `Blue`, `Fonts`, `Particle`, `Settings`, `HeartBank` |
| `Assets/Scripts/` (Unity & 3rd-party Dummy) | **~15 MB** | Chứa các thư mục dummy trùng lặp với UPM của Unity (`UnityEngine`, `UnityEngine.UI`, `Unity.TextMeshPro`, `Unity.VisualScripting.*`, `Unity.Timeline`, `Unity.Addressables`, `Unity.Burst`, `Unity.Localization`...) và SDK rác (`ThinkingAnalytics`, `ThinkingSDK`, `AppsFlyer`, `Facebook.Unity`, `NativeGallery.Runtime`, `RTLTMPro`) | **XÓA 100% các thư mục dummy trùng lặp**, cài đặt lại qua `Packages/manifest.json` chính thức của Unity |
| `Assets/Scripts/Assembly-CSharp` | **6.1 MB** | Chứa ~2,300 file `.cs` dummy. Trong đó hơn 1,800 file thuộc về các Event phụ, Clan/Team, Leaderboard, CardCollection, Ads mediation cũ | **XÓA ~1,800 script rác**; **GIỮ LẠI GUID** của ~80–100 script cốt lõi gắn trên `SplashScene`, `HomeScene`, `GameScene` và Prefab để viết lại code thật |
| `Assets/Screw3D/AAResources/Event` | **11 MB** | Chứa asset của `Space`, `StreakLine`, `MissionCenter`, `LevelBox`, `ChallengeBundle`, `HalloweenBundle`, `BlackFridayBundle`, `ChristmasBundle` | **XÓA các event thời vụ**; chỉ giữ lại `RemoveAd` / `SpecialOffer` nếu hiển thị gói No-Ads trên Home |
| `Assets/Screw3D/AAResources/` (Khác) | **~4.5 MB** | `CardCollection` (48K), `Pass` (536K), `Team` (336K), `builtinresources/urlimage` (3.1M), `Localization` (1.2M), `PuzzleTest` (192K) | **XÓA** để giảm size và tránh phụ thuộc tài nguyên mạng |
| `Assets/MonoBehaviour` & `Assets/Resources` | **~1.1 MB** | Hàng chục file `MonoBehaviour_0..21.asset` bị mất script (`m_Script: {fileID: 0}`) và các `*Settings.asset` của Event (`LavaSettings`, `UnicornSettings`, `FlowerRushSettings`...) + `Resources/configjson/*` | **XÓA** toàn bộ asset mồ côi và config của Event phụ; chỉ giữ lại `ScrewLayoutPreset.json`, `ColorSetting.asset`, `DOTweenSettings`, `TMP Settings` |
| `Assets/Mesh` & `Assets/Texture2D` | **88 MB** | `Mesh` (72 MB - 4,148 file), `Texture2D` (16 MB - 530 file). Rất nhiều mesh/texture mồ côi hoặc thuộc về các màn chơi không dùng trong bản Playable | **LỌC THEO DEPENDENCY**: Chỉ giữ lại Mesh & Texture được tham chiếu bởi các Puzzle Prefab và UI được chọn |

> **Ước tính sau khi dọn dẹp:**
> - **Bản Full Game (80 màn chơi):** Giảm từ **~402 MB** xuống còn **~110 – 130 MB** nguyên liệu thô (khi build APK/AAB nén còn **~35 – 45 MB**).
> - **Bản Playable / Mini Build (3–5 màn chơi tiêu biểu):** Giảm nguyên liệu xuống **< 15 MB** (khi build WebGL / Playable HTML5 chỉ còn **~3 – 6 MB**).

---

## PHẦN 2: QUY HOẠCH & TỔ CHỨC LẠI CẤU TRÚC THƯ MỤC PROJECT GỌN GÀNG

### 2.1. Nguyên tắc di chuyển An Toàn (Không đứt gãy Reference)
- Trong Unity, liên kết giữa Scene, Prefab, Script, Mesh, Material, Sprite được định danh **100% bằng `guid` trong file `.meta`**.
- Mọi thao tác quy hoạch thư mục đều di chuyển **cặp file `TênFile.ext` + `TênFile.ext.meta` đi cùng nhau**, đảm bảo **không bị mất bất kỳ tham chiếu nào** trên Scene và Prefab.
- Sau khi di chuyển Scene, tự động cập nhật lại đường dẫn trong `ProjectSettings/EditorBuildSettings.asset`.

### 2.2. Sơ đồ Cấu Trúc Thư Mục Mới (Chuẩn Modular & Sạch sẽ)
Thay vì 18 thư mục lộn xộn ở gốc `Assets/`, toàn bộ project được gom về **4 thư mục gốc duy nhất**:

```text
Assets/
├── _Game/                              # Toàn bộ tài nguyên & mã nguồn chính của Game
│   ├── Scenes/                         # 3 Scene chính của Game
│   │   ├── SplashScene.unity
│   │   ├── HomeScene.unity
│   │   └── GameScene.unity
│   │
│   ├── Scripts/                        # Code C# đã phân nhóm rõ ràng (giữ nguyên .meta GUID)
│   │   ├── Core/                       # Managers, UserDataManager, InventoryManager, MainThreadDispatcher...
│   │   ├── Audio/                      # AudioManager, BgmSource, VibrationManager
│   │   ├── Analytics/                  # FirebaseManager, AnalyticsManager
│   │   ├── IAP/                        # IapManager, ShopPage, ShopDialogContent, ShopBundleItem, ShopCoinItem
│   │   ├── Home/                       # HomeController, HomePage, HomePlayButton, NavigationController, StageRewardDialog...
│   │   ├── Gameplay/                   # GameController, GameCreator, Board, Screw, BoxScript, PuzzlePrefab, Hole...
│   │   │   ├── CameraAndInput/         # RotateHelper, TwoFingerRotateGesture, ZoomGesture, ViewScale
│   │   │   ├── Boosters/               # ToolBase, ToolManager, BuyToolDialog, AddBoxDialogContent...
│   │   │   └── Dialogs/                # WinDialog, FailDialog, PauseDialog, QuitConfirmDialog, LotterySlider...
│   │   ├── UI/                         # Snui_Button, Snui_Text, Snui_Image, SafeArea, Dialog, ProgressBar, PageView, CoinHud, HeartHud
│   │   └── Editor/                     # PlayableBuildOptimizer (Tool tối ưu & chuyển đổi chế độ Full/Playable)
│   │
│   ├── Prefabs/                        # Toàn bộ Prefab đã phân loại
│   │   ├── Puzzles/                    # Các Prefab màn chơi 3D
│   │   │   ├── BoardsPuzzles/          # B_Camel, B_Horse, B_Rocket...
│   │   │   ├── CubePuzzles/            # C_Bird, C_Crab, C_Dinosaur...
│   │   │   ├── SpecialPuzzles/         # Cat, CoffeeMachine, RobotCat...
│   │   │   └── TestBPuzzles/           # Cactus, CoffeeCup, Piano...
│   │   ├── Gameplay/                   # Screw, Box, Hole, BgPlane, ViewScale, FlyTool, Tutorial...
│   │   ├── UI_Home/                    # HomeUICanvas, HomePage, StageRewardChest, StageRewardDialog...
│   │   ├── UI_Game/                    # UiCanvas, WinDialog, FailDialog, PauseDialog, BuyToolDialog...
│   │   ├── UI_Shop/                    # ShopPage, ShopDialogContent, BundleBase, CoinItem, ShopRewardItem...
│   │   └── UI_Common/                  # HudContainer, CoinHud, PopupCommon, LoadScenePanel, Settings...
│   │
│   ├── Art3D/                          # Tài nguyên 3D đã lọc sạch phần thừa
│   │   ├── Meshes/                     # (Gộp từ Assets/Mesh & Game/Fbx)
│   │   ├── Materials/                  # (Gộp từ Assets/Material & Game/Material)
│   │   └── PhysicMaterials/            # (Gộp từ Assets/PhysicMaterial)
│   │
│   ├── Art2D/                          # Tài nguyên 2D & UI
│   │   ├── Sprites/                    # (Gộp từ Assets/Sprite, Screw3D/Sprite, ShopUI/Sprites, Snui)
│   │   ├── Textures/                   # (Gộp từ Assets/Texture2D)
│   │   └── Spine/                      # Các hiệu ứng xương 2D Spine (Home, Game, Win, Tools, Shop)
│   │
│   ├── Audio/                          # Toàn bộ âm thanh BGM & SFX (Gộp từ Assets/AudioClip & Common/Audio)
│   ├── Animations/                     # AnimationClip & AnimatorController
│   ├── Particles/                      # Hiệu ứng hạt (Particle Systems)
│   ├── Fonts/                          # Font chữ & TextMeshPro SDF Assets
│   ├── Shaders/                        # Custom Shaders cần thiết cho Board/Screw/UI
│   └── Settings/                       # URP-Balanced.asset, URP-Balanced-Renderer.asset, SSAO.asset
│
├── Resources/                          # Chỉ giữ lại các file thực sự load qua Resources.Load()
│   ├── setting/
│   │   ├── ColorSetting.asset
│   │   └── ScrewLayoutPreset.json
│   ├── DOTweenSettings.asset
│   └── TMP Settings.asset
│
├── ThirdParty/                         # Các thư viện bên thứ 3 gọn sạch
│   ├── DOTween/
│   ├── Spine/                          # spine-csharp & spine-unity
│   ├── Coffee.UIParticle/
│   └── UniTask/
│
└── TextMesh Pro/                       # Tài nguyên mặc định của TMP
```

### 2.3. Lợi ích của cấu trúc mới
1. **Dễ dàng bảo trì & mở rộng:** Muốn sửa code Gameplay chỉ cần vào `Assets/_Game/Scripts/Gameplay/`, muốn thêm/bớt màn chơi chỉ cần vào `Assets/_Game/Prefabs/Puzzles/`.
2. **Không còn thư mục rác của AssetRipper:** Xóa sổ hoàn toàn các thư mục trung gian khó hiểu như `Snui`, `Sugar`, `AAResources`, `editresources`, `builtinresources`, `MonoBehaviour`.
3. **Hỗ trợ tối ưu Playable 1-Click:** Toàn bộ màn chơi nằm gọn trong `Assets/_Game/Prefabs/Puzzles/` và Mesh nằm trong `Assets/_Game/Art3D/Meshes/`, giúp công cụ `PlayableBuildOptimizer` bật/tắt màn chơi cực kỳ nhanh và chính xác.

---

## PHẦN 3: KIẾN TRÚC KHÔI PHỤC CODE (GIỮ NGUYÊN GUID & PREFAB)

Điểm quan trọng nhất khi làm lại project từ bản xuất AssetRipper là **bảo toàn `.meta` (GUID)** của các script cốt lõi đang gắn sẵn trên `SplashScene.unity`, `HomeScene.unity`, `GameScene.unity` và 80 Prefab màn chơi.

### 3.1. Khôi phục Packages & Plugins nền tảng
1. **Cập nhật `Packages/manifest.json`:**
   - Thêm gói chuẩn của Unity: `com.unity.ugui` (Unity UI & TextMeshPro), `com.unity.render-pipelines.universal` (URP - do project có `URP-Balanced.asset`), `com.unity.purchasing` (Unity IAP).
2. **Thay thế 2 thư viện Third-Party thiết yếu (`Assets/ThirdParty/`):**
   - **DOTween:** Thay thư mục dummy bằng DOTween runtime chuẩn (hoặc adapter nhẹ tương thích API).
   - **Spine Runtime (`spine-csharp`, `spine-unity`):** Thay các file dummy bằng bộ mã nguồn Spine Unity Runtime chuẩn (giữ nguyên GUID của `SkeletonGraphic.cs` `ff326197253ae4cec0b46632391386ec` và `SkeletonSubmeshGraphic.cs` `a1e790895e5edacc6b732811d26ae2c0` để toàn bộ hiệu ứng Spine trên Home và Gameplay hoạt động ngay lập tức).
3. **Viết lại bộ UI Base (`Assets/_Game/Scripts/UI/`):**
   - Viết lại phần thân cho các component UI cơ sở đang gắn trên toàn bộ prefab:
     - `Snui_Button.cs` (hiệu ứng nhấn `ClickScale`, `EnableClickAudio`, `EnableVibration`)
     - `Snui_Text.cs`, `Snui_Image.cs`, `SafeArea.cs`, `Dialog.cs`, `DialogContent.cs`, `ProgressBar.cs`
     - `PageView.cs`, `PageViewCell.cs`, `NavigationBar.cs`, `NavigationCell.cs`

---

## PHẦN 4: KẾ HOẠCH TRIỂN KHAI CHI TIẾT TỪNG TÍNH NĂNG

### 4.1. Luồng Khởi Động & Quản Lý Dữ Liệu (`SplashScene` & Core Managers)
- **Scene:** `Assets/_Game/Scenes/SplashScene.unity`
- **Các Script trong `Assets/_Game/Scripts/Core/` & `Audio/` (giữ nguyên GUID):**
  1. `Managers.cs` (`5ef57e29d8090dbadc65d79c32ae72d1`): Singleton `DontDestroyOnLoad` khởi tạo toàn bộ Manager theo thứ tự chuẩn.
  2. `UserDataManager.cs` (`f10a810703ac42e6e372076eb0a68973`) & `InventoryManager.cs` (`ca1526c4853b6de09ef326110058f79b`):
     - Lưu/đọc dữ liệu người chơi qua `PlayerPrefs` / JSON:
       - `CurrentLevel` (Màn hiện tại, từ 1 đến 80, hết 80 tự xoay vòng hoặc random).
       - `Coins` (Vàng), `Stars` (Sao), `Hearts` (Mạng chơi + bộ đếm hồi tim tự động, trạng thái Unlimited Hearts).
       - Số lượng 4 Booster/Tool: **Add Hole (Thêm lỗ trống)**, **Hammer (Búa đập tấm gỗ)**, **Broom (Chổi quét đinh trên lỗ chờ)**, **Add Box (Thêm hộp đựng đinh)**.
       - `IsRemoveAds` (Trạng thái đã mua gói xóa quảng cáo).
  3. `AudioManager.cs` (`a1b09a7a4ab69fc700bfb75efe174a20`), `BgmSource.cs`, `VibrationManager.cs` (`1aff986874bec18efa0d9305f7ff2e6e`):
     - Quản lý phát nhạc nền (BGM) và hiệu ứng âm thanh (SFX tháo đinh, đinh bay vào hộp, đóng nắp hộp, vỡ tấm ván, thắng/thua) từ `Assets/_Game/Audio/`.
     - Bật/tắt âm thanh, nhạc nền, rung phản hồi (Haptic/Vibration).
  4. `SplashScene.cs` (`6e53e14c0b05222f090bb79cbe971ae0`):
     - Chạy thanh loading mượt mà, khởi tạo Firebase & IAP, sau đó chuyển sang `HomeScene`.

---

### 4.2. Tính Năng Màn Hình Chính (`HomeScene`)
- **Scene:** `Assets/_Game/Scenes/HomeScene.unity`
- **Scripts trong `Assets/_Game/Scripts/Home/`:**
  1. **Top HUD (`HudContainer.cs`, `CoinHud.cs`, `HeartHud.cs`, `Snui_FreeHeart.cs`):**
     - Hiển thị số **Coin**, số **Heart** (kèm đồng hồ đếm ngược hồi tim 30 phút/tim hoặc trạng thái Vô hạn), nút `+` mở nhanh Shop hoặc popup nhận tim.
     - Nút **Settings** (Bật/tắt Sound, Music, Vibration, Colorblind Mode, Restore Purchase).
  2. **Tab Điều Hướng (`NavigationController.cs`, `NavigationBar.cs`, `PageView.cs`):**
     - Chuyển đổi mượt mà giữa **Shop Tab** (`ShopPage.cs`) và **Home Tab** (`HomePage.cs`).
     - Ẩn hoặc khóa nhẹ nhàng các Tab thừa (`TeamPage`, `RankPage`, `CardPage`) để giao diện gọn gàng, không báo lỗi thiếu mạng.
  3. **Màn hình Home chính (`HomeController.cs`, `HomePage.cs`, `HomePlayButton.cs`):**
     - Hiển thị mô hình 3D preview hoặc icon màn chơi hiện tại, cấp độ hiện tại (`Level X`) và nhãn độ khó (`Hard Level` nếu có).
     - Thanh tiến trình **Level Chest / Milestone (`StageRewardDialog.cs`)**: Vượt đủ số màn nhận rương thưởng vàng & booster.
     - Thanh tiến trình **Special Screw (`SpecialScrewWidget.cs`)**: Tích lũy đinh đặc biệt qua các màn để nhận thưởng.
     - Nút **Play (`HomePlayButton.cs`)**: Kiểm tra còn Heart không -> chuyển sang `GameScene`.

---

### 4.3. Tính Năng Gameplay 3D Cốt Lõi (`GameScene`)
- **Scene:** `Assets/_Game/Scenes/GameScene.unity`
- **Kho 80 Prefab màn chơi (`Assets/_Game/Prefabs/Puzzles/`):**
  - `BoardsPuzzles/` (15 màn: `B_Camel`, `B_Horse`, `B_Lion`, `B_Rocket`...)
  - `CubePuzzles/` (19 màn: `C_Bird`, `C_Crab`, `C_Dinosaur`, `C_Helicopter`...)
  - `SpecialPuzzles/` (35 màn: `Cat`, `CoffeeMachine`, `ChristmasTree`, `RobotCat`, `Suitcase`...)
  - `TestBPuzzles/` (11 màn: `Cactus`, `CoffeeCup`, `DonutsBox`, `Piano`...)
- **Cấu trúc dữ liệu có sẵn trên từng Puzzle Prefab (đã kiểm tra YAML):**
  - Root gắn `PuzzlePrefab.cs` (`d92f1c2b36f2361974e822b73d293b52`) -> con `Stage_1` gắn `PuzzlePrefabItem.cs` (`1c082d46d4729af37d57707464d97c40`).
  - Các tấm mảnh ghép 3D gắn `MeshFilter`, `MeshRenderer`, `Rigidbody`, `MeshCollider` và `Board.cs` (`6c14f81d22de1c0db9274806e8a7046c`) với các trường serialized sẵn: `BoardColor`, `Stage`, `Index`, `Type`, `UseJoint`, `IsLoose`, `Screws`.
  - Các đinh ốc là con của `Board` gắn `Screw.cs` (`89c75f3ac9b0c8271cd95dd3e566bd69`) với các trường serialized sẵn: `BoardIndex`, `ScrewColor` và `BoxCollider`.
- **Chi tiết logic Gameplay cần viết lại (`Assets/_Game/Scripts/Gameplay/`):**
  1. **Khởi tạo màn chơi (`GameCreator.cs` & `GameController.cs`):**
     - Load Puzzle Prefab tương ứng với Level hiện tại vào `GameRoot`.
     - Áp dụng màu sắc cho `Board` và `Screw` từ `ColorSetting.asset` / bảng màu chuẩn (hỗ trợ cả chế độ mù màu `ScrewColorBlindness.prefab`).
     - Sinh danh sách **Hộp đựng đinh (`BoxScript.cs` - `Box.prefab`)** theo thuật toán thông minh (luôn lấy màu từ các đinh có thể tháo được ở lớp ngoài để đảm bảo người chơi luôn có đường giải hợp lệ).
     - Sinh 5 **Lỗ chờ tạm (`Hole.prefab`)** tại `HoleRoot`.
  2. **Điều khiển Camera & Xoay mô hình 3D (`RotateHelper.cs`, `TwoFingerRotateGesture.cs`, `ZoomGesture.cs`, `ViewScale.cs`):**
     - Vuốt 1 ngón để xoay tự do 360 độ mô hình 3D quanh tâm với quán tính (`Damping`, `RotationSpeed`).
     - Chụm 2 ngón (Pinch) hoặc thanh trượt `ViewScale` để phóng to / thu nhỏ mô hình (`ZoomMin`, `ZoomMax`).
     - Phân biệt rõ thao tác **Tap (chạm tháo đinh)** và **Drag (xoay mô hình)** bằng ngưỡng di chuyển (`DeadZone`).
  3. **Cơ chế Tháo Đinh & Vật Lý Rơi Tấm Ván (`Screw.cs`, `Board.cs`):**
     - Khi người chơi chạm vào 1 `Screw`:
       - Kiểm tra xem đinh có bị cản phía trên không (Raycast dọc theo trục rút đinh). Nếu vướng tấm khác -> nhấp nháy cảnh báo đỏ + rung nhẹ.
       - Nếu không bị cản:
         - Kiểm tra xem trong các `BoxScript` đang mở ở `BoxRoot` có hộp nào **cùng màu (`ScrewColor`)** và còn chỗ trống không.
         - Nếu có hộp cùng màu: Đinh xoáy rút ra, bay theo đường cong (DOTween Bezier) vào đúng ô trong Hộp. Khi hộp đủ 3 đinh (`MaxScrewCount = 3`) -> Đóng nắp hộp (`LidSpriteList`), hộp bay đi, cộng tiến trình màn chơi, và gọi hộp mới ra.
         - Nếu chưa có hộp cùng màu: Đinh bay vào 1 **Lỗ chờ (`Hole`)** còn trống trên khay `HoleRoot`.
       - Sau khi hộp mới xuất hiện: Tự động kiểm tra các đinh đang nằm ở **Lỗ chờ (`HoleRoot`)**; nếu có đinh trùng màu với hộp mới -> tự động bay từ lỗ chờ vào hộp!
     - **Vật lý `Board.cs`:**
       - Mỗi tấm `Board` theo dõi danh sách các `Screw` đang giữ nó.
       - Khi còn 1 đinh duy nhất: Có thể bật `HingeJoint` hoặc cho phép tấm nghiêng nhẹ tự nhiên.
       - Khi toàn bộ đinh trên `Board` đã được tháo (`Screws.Count == 0`): Bật `Rigidbody.isKinematic = false`, `useGravity = true`, đẩy nhẹ để tấm ván rơi tự do xuống dưới qua `DropRoot` và tự hủy sau khi khuất khỏi màn hình.
  4. **Điều kiện Thắng / Thua & Hồi sinh (`WinDialog.cs`, `FailDialog.cs`):**
     - **Thắng (Win):** Toàn bộ đinh đã vào hộp hết -> Hiển thị `WinLogoDialogContent` -> `WinDialog` (nhận thưởng Coin, quay thưởng nhân hệ số `LotterySlider.cs`, qua màn tiếp theo).
     - **Cảnh báo sắp thua (`OneLeft` / `FailNotice`):** Khi 4/5 lỗ chờ đã đầy -> hiện cảnh báo "Chỉ còn 1 lỗ trống!".
     - **Thua (Fail):** Khi cả 5/5 lỗ chờ đều đầy mà đinh vừa tháo không khớp hộp nào -> Hiện `FailDialog` (`FailDialogContent.cs`): Cho phép dùng Coin hoặc dùng Booster để hồi sinh (`RevivePriceList`) hoặc chơi lại (`RestartDialogContent.cs`, trừ 1 Heart).
  5. **Hệ thống 4 Booster / Công cụ trợ giúp (`ToolBase.cs`, `ToolManager.cs`, `BuyToolDialog.cs`):**
     - **Add Hole (Thêm lỗ chờ):** Mở thêm 1 lỗ chờ trên khay `HoleRoot`.
     - **Broom (Chổi quét):** Quét toàn bộ đinh đang nằm ở khay lỗ chờ cất vào hộp dự trữ (giải phóng toàn bộ lỗ chờ).
     - **Hammer (Búa thần):** Chạm vào 1 tấm `Board` bất kỳ -> Đập vỡ tấm đó ngay lập tức (các đinh trên tấm đó tự động bay vào hộp hoặc được xử lý gọn).
     - **Add Box (Thêm khay hộp):** Mở thêm 1 vị trí đặt `Box` trên `BoxRoot` (từ 2 hộp lên 3–4 hộp cùng lúc).

---

### 4.4. Hệ Thống Firebase Tracking (`Assets/_Game/Scripts/Analytics/`)
Thiết kế theo mô hình **Adapter Pattern** (`FirebaseManager.cs` & `AnalyticsManager.cs`):
- Chạy trên **Unity Editor / Bản Playable WebGL / HTML5**: Tự động ghi log ra Console hoặc bắn sự kiện sang Playable Ad Network (`mraid` / `dapi` / `FbPlayableAd`) mà không cần thư viện native C++ của Firebase (giúp bản Playable nhẹ tuyệt đối và không lỗi build).
- Chạy trên **Android / iOS (Bản Full)**: Khi bật Scripting Define Symbol `FIREBASE_ENABLED`, tự động gọi SDK `Firebase.Analytics.FirebaseAnalytics`.

**Danh sách sự kiện Tracking tiêu chuẩn được tích hợp sẵn:**
| Nhóm | Tên sự kiện (Event Name) | Tham số (Parameters) | Thời điểm gửi |
| :--- | :--- | :--- | :--- |
| **Game Flow** | `app_launch` | `version`, `platform` | Khi vào `SplashScene` |
| **Level** | `level_start` | `level_id`, `puzzle_name`, `attempt_count` | Khi bắt đầu màn chơi trong `GameScene` |
| **Level** | `level_win` | `level_id`, `duration_sec`, `screws_cleared`, `boosters_used` | Khi thắng màn chơi |
| **Level** | `level_fail` | `level_id`, `reason` (`holes_full`), `remaining_screws` | Khi đầy lỗ chờ dẫn đến thua |
| **Level** | `level_revive` | `level_id`, `method` (`coin` / `ad`) | Khi người chơi hồi sinh sau khi thua |
| **Economy / Booster** | `booster_use` | `level_id`, `booster_type` (`add_hole`, `broom`, `hammer`, `add_box`) | Khi sử dụng công cụ trợ giúp |
| **Economy / Booster** | `currency_earn` / `currency_spend` | `currency_type` (`coin`, `heart`), `amount`, `source` | Khi nhận hoặc tiêu Coin/Heart |
| **Monetization** | `iap_click` | `product_id`, `placement` (`shop_tab`, `home_popup`, `game_out_of_coin`) | Khi bấm nút mua gói IAP |
| **Monetization** | `iap_purchase_success` | `product_id`, `price`, `currency` | Khi giao dịch IAP thành công |
| **Monetization** | `iap_purchase_fail` | `product_id`, `error_reason` | Khi giao dịch IAP thất bại / hủy |

---

### 4.5. Hệ Thống In-App Purchase - IAP (`Assets/_Game/Scripts/IAP/`)
- **UI Prefabs (`Assets/_Game/Prefabs/UI_Shop/`):** `ShopDialogContent.prefab`, `BundleBase.prefab`, `CoinItem.prefab`, `MoreOffer.prefab`, `PurchaseFailDialogContent.prefab`, `ShopPage.prefab`.
- **Thiết kế `IapManager.cs` (`e9b043370d853d4375f3e8b20510152d`):**
  1. **Danh mục gói sản phẩm (Catalog cấu hình trong ScriptableObject / Code):**
     - **Gói Non-Consumable / Bundle:**
       - `screw3d.removeads` (Remove Ads)
       - `screw3d.starter.bundle` (Starter Bundle: Coin + Bộ 4 Booster + Unlimited Heart 1h)
       - `screw3d.vip.bundle` / `screw3d.super.bundle` (Big Bundles trong `BundleBase.prefab`)
     - **Gói Consumable (6 gói Coin trong `CoinItem.prefab`):**
       - `screw3d.coin.1` (200 Coins - \$0.99)
       - `screw3d.coin.2` (500 Coins - \$1.99)
       - `screw3d.coin.3` (1,200 Coins - \$4.99)
       - `screw3d.coin.4` (2,500 Coins - \$9.99)
       - `screw3d.coin.5` (6,500 Coins - \$24.99)
       - `screw3d.coin.6` (15,000 Coins - \$49.99)
  2. **2 Chế độ hoạt động linh hoạt:**
     - **Chế độ Mock / Test / Playable (Mặc định):** Khi bấm mua sẽ mô phỏng xử lý giao dịch và cộng thẳng phần thưởng (kèm hiệu ứng bay Coin/Reward `RewardCell.cs`) hoặc chuyển hướng sang Store (`Install Full Game` nếu đang ở chế độ Playable Ads).
     - **Chế độ Production (`UNITY_PURCHASING`):** Kết nối trực tiếp `IDetailedStoreListener` của `com.unity.purchasing` với Google Play Billing / Apple StoreKit, hỗ trợ đầy đủ `RestorePurchases()`.

---

## PHẦN 5: TỐI ƯU DUNG LƯỢNG NHẸ ĐỂ BUILD BẢN PLAYABLE (PLAYABLE ADS / MINI BUILD)

Để project vừa có thể build **Bản Full (Android/iOS)** vừa có thể xuất **Bản Playable siêu nhẹ (WebGL / Mintegral / Applovin / IronSource / Unity Playworks / Facebook Playable)**, kế hoạch áp dụng cơ chế **Dual-Mode Build Tool** ngay trong Editor (`Assets/_Game/Scripts/Editor/PlayableBuildOptimizer.cs`):

### 5.1. Công cụ Editor tự động chuyển đổi `Full Mode` <-> `Playable Mode`
Tạo menu `Tools -> BoltPuzzle3D -> Build Optimizer` với các tác vụ tự động:
1. **Lọc Màn Chơi (Level Stripping cho Playable):**
   - Bản Full: Đưa 15–80 màn chơi vào danh sách Level.
   - Bản Playable: Chỉ giữ lại **1 đến 3 màn chơi ấn tượng nhất** (ví dụ: `C_Bird.prefab`, `B_Camel.prefab`, `Cat.prefab`), tạm thời tách các màn còn lại khỏi danh sách tham chiếu để Unity tự động loại bỏ ~70 MB Mesh không dùng khi đóng gói.
2. **Nén Texture & Sprite (`TextureImporter` Batch Tool):**
   - Tự động giảm `maxTextureSize` của các texture nền/UI lớn từ `2048`/`1024` xuống `512` hoặc `256` cho bản Playable.
   - Bật Crunch Compression (Quality = 50) cho Android/WebGL.
3. **Nén Audio (`AudioImporter` Batch Tool):**
   - Chuyển toàn bộ `AudioClip` sang **Mono (Force To Mono = true)**, định dạng **Vorbis**, Quality = **30% – 50%**, giảm dung lượng âm thanh xuống **< 800 KB**.
4. **Nén Mesh & Stripping Engine:**
   - Bật `Mesh Compression = High` trên các file `.asset` / `.fbx`.
   - Trong `ProjectSettings`: Bật `Managed Stripping Level = High` và `Strip Engine Code = true`.
5. **Tích hợp CTA (Call-To-Action) cho Bản Playable:**
   - Thêm cấu hình `PlayableConfig`: Giới hạn số lượt tháo đinh (ví dụ: tháo 15 đinh hoặc thắng màn 1) -> Tự động hiện màn hình Endcard / nút **"Download Now"** gọi hàm mở Store.

---

## PHẦN 6: LỘ TRÌNH THỰC HIỆN TỪNG BƯỚC (EXECUTION ROADMAP)

| Giai đoạn | Hạng mục công việc | Kết quả đầu ra (Deliverables) |
| :---: | :--- | :--- |
| **Bước 0** | **Khởi tạo Git & mốc gốc (Baseline)** | - Đóng Unity Editor, `git init`, thêm `.gitignore` chuẩn Unity, `.gitattributes`, hook `commit-msg` chặn dòng `Co-Authored-By`.<br>- Commit mốc gốc nguyên trạng bản export AssetRipper và gắn tag `baseline-assetripper` (xem Phần 7). |
| **Bước 1** | **Dọn dẹp phần thừa (Cleanup)** | - Xóa `Assets/Snui/Events` (123 MB), các module social/clan thừa trong `Snui` & `Screw3D` (~30 MB).<br>- Xóa các thư mục script dummy trùng với gói Unity trong `Assets/Scripts/` và các `.asset` / `Mesh` / `Texture2D` mồ côi. |
| **Bước 2** | **Tổ chức lại cấu trúc thư mục (`Assets/_Game/`)** | - Quy hoạch lại toàn bộ `Scenes`, `Scripts`, `Prefabs`, `Art3D`, `Art2D`, `Audio`, `Animations`, `Shaders` vào `Assets/_Game/` và `Assets/ThirdParty/` (di chuyển kèm `.meta` để bảo toàn 100% GUID).<br>- Cập nhật `Packages/manifest.json` và `EditorBuildSettings.asset`. |
| **Bước 3** | **Xây dựng Core Architecture & Splash** | - Viết lại `Managers`, `UserDataManager`, `InventoryManager`, `AudioManager`, `VibrationManager`, `SplashScene` trong `Assets/_Game/Scripts/Core/` & `Audio/`.<br>- Hoàn thiện bộ UI Base (`Snui_Button`, `Snui_Text`, `SafeArea`, `Dialog`, `DialogContent`, `ProgressBar`). |
| **Bước 4** | **Hoàn thiện Gameplay 3D (`GameScene`)** | - Viết lại `GameController`, `GameCreator`, `Board`, `Screw`, `BoxScript`, `RotateHelper`, `ZoomGesture`, `ViewScale` trong `Assets/_Game/Scripts/Gameplay/`.<br>- Hoàn thiện cơ chế tháo đinh, bay vào hộp, lỗ chờ, vật lý rơi tấm gỗ 3D, 4 Booster và các popup `WinDialog`, `FailDialog`, `PauseDialog`. |
| **Bước 5** | **Hoàn thiện Home & Shop IAP (`HomeScene`)** | - Viết lại `HomeController`, `HomePage`, `HomePlayButton`, `NavigationController`, `CoinHud`, `HeartHud`, `StageRewardDialog`.<br>- Viết lại `IapManager`, `ShopPage`, `ShopDialogContent`, `ShopBundleItem`, `ShopCoinItem`. |
| **Bước 6** | **Tích hợp Firebase Tracking & Playable Optimizer** | - Viết `FirebaseManager` & `AnalyticsManager` gắn đầy đủ tracking vào các điểm chạm.<br>- Viết Editor Tool `PlayableBuildOptimizer` và kiểm tra compile sạch sẽ 0 lỗi. |

---

## PHẦN 7: KẾ HOẠCH TỰ ĐỘNG COMMIT (GIT)

> **Quy tắc bắt buộc:** Mọi commit chỉ mang tên tác giả là chủ project (theo `git config user.name` / `user.email` trên máy). **Không thêm bất kỳ dòng `Co-Authored-By:` nào** vào commit message, kể cả khi commit do công cụ AI hay script tự động tạo.

### 7.1. Khởi tạo một lần (Bước 0)
1. **Đóng Unity Editor** trước khi khởi tạo (tránh commit file đang ghi dở).
2. `git init -b main` tại thư mục gốc project (`BoltPuzzle3D/`).
3. Tạo `.gitignore` chuẩn Unity, tối thiểu loại trừ:
   ```gitignore
   /[Ll]ibrary/
   /[Tt]emp/
   /[Oo]bj/
   /[Bb]uild/
   /[Bb]uilds/
   /[Ll]ogs/
   /[Uu]ser[Ss]ettings/
   /[Mm]emoryCaptures/
   *.csproj
   *.sln
   *.slnx
   .vs/
   .idea/
   .vscode/
   .DS_Store
   *.apk
   *.aab
   ```
4. Tạo `.gitattributes`:
   - Đánh dấu YAML Unity là text, merge kiểu `unityyamlmerge`: `*.unity`, `*.prefab`, `*.asset`, `*.mat`, `*.meta`, `*.anim`, `*.controller`.
   - **Git LFS (khuyến nghị):** `*.png`, `*.jpg`, `*.tga`, `*.psd`, `*.fbx`, `*.ogg`, `*.wav`, `*.mp3`, `*.ttf`, `*.otf`, `*.bytes`. Nếu chưa cài LFS (`brew install git-lfs && git lfs install`) thì bỏ qua và bổ sung sau.
   - Lưu ý: Mesh của AssetRipper là `.asset` YAML (~72 MB / 4,148 file) — để dạng text, không đưa vào LFS.
5. Kiểm tra `ProjectSettings/EditorSettings.asset`: `m_SerializationMode: 2` (Force Text — **đã đúng**), Version Control mode = **Visible Meta Files**.
6. Cài hook `.git/hooks/commit-msg` tự động **xóa mọi dòng `Co-Authored-By:`** khỏi commit message (lớp bảo vệ cuối cùng):
   ```sh
   #!/bin/sh
   sed -i '' '/^[Cc]o-[Aa]uthored-[Bb]y:/d' "$1"
   ```
   (`sed -i ''` là cú pháp macOS.) Sau đó `chmod +x .git/hooks/commit-msg`.
7. **Commit mốc gốc:** `git add -A && git commit -m "chore: baseline AssetRipper export (Unity 6000.2.10f1)"` rồi `git tag baseline-assetripper`. Đây là điểm quay về an toàn trước khi xóa bất cứ thứ gì.

### 7.2. Script commit tự động `tools/auto_commit.sh`
```sh
#!/bin/sh
# Dùng: tools/auto_commit.sh "<type>(<scope>): <mô tả>"
set -e
cd "$(dirname "$0")/.."
if [ -f Temp/UnityLockfile ] && lsof Temp/UnityLockfile >/dev/null 2>&1; then
  echo "⚠️  Unity Editor đang mở — hãy Save Project (Cmd+S) trước khi commit."
fi
git add -A
if git diff --cached --quiet; then
  echo "Không có thay đổi để commit."; exit 0
fi
git commit -m "$1"
```
- Script **không bao giờ** tự thêm trailer co-author; hook ở 7.1 chặn thêm lần nữa.
- Chỉ commit cục bộ. **Push lên remote do chủ project tự quyết** (chưa cấu hình remote trong kế hoạch này).

### 7.3. Khi nào commit (tự động sau mỗi mốc, không commit theo giờ)
Commit theo **mốc công việc hoàn chỉnh**, không chạy theo timer (tránh commit lúc Unity đang import dở):

| Thời điểm | Ví dụ commit message |
| :--- | :--- |
| Xong mỗi nhóm xóa ở Bước 1 (mỗi thư mục lớn một commit) | `chore(cleanup): remove Snui/Events (123 MB)` |
| Xong mỗi nhóm di chuyển ở Bước 2 | `refactor(structure): move scenes to _Game/Scenes` |
| Sau khi đổi `Packages/manifest.json` / remap GUID package | `build(packages): add URP, uGUI, TMP; remap script GUIDs` |
| Mỗi script/feature viết lại xong và **compile 0 lỗi** | `feat(gameplay): implement Screw extraction & hole logic` |
| Sau mỗi Bước lớn (0–6) | gắn thêm tag `step-1-cleanup`, `step-2-structure`… |

**Điều kiện trước mỗi commit:** Unity đã Save Project, Console không còn lỗi compile, mở thử scene liên quan không có `Missing Script` mới. Nếu một bước làm hỏng tham chiếu → `git reset --hard` về commit trước đó thay vì sửa chồng.

**Định dạng message:** Conventional Commits (`feat`, `fix`, `refactor`, `chore`, `build`, `docs`), tiếng Anh hoặc tiếng Việt thống nhất một kiểu, **không có dòng `Co-Authored-By`**.
