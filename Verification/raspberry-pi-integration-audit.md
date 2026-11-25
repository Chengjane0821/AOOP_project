# V=IR 整合起點與報告核對

起點：`a6b3c41`，main 與 origin/main 相同，開始時無未提交修改。
已完整讀取根目錄 `Do you remember V=IR.pdf` 兩頁文字。
報告沒有指定 Pi、相機與 VR 頭盔確切型號，因此不得宣稱已確認硬體。

| 房屋 | 報告中的遊戲 | 真實物件 | 線索 ID |
|---|---|---|---|
| Elder | Table Tennis | keyboard | clue_1 |
| Alchemist | Airplane Obstacle Avoidance，60 秒 | laptop | clue_2 |
| Carpenter | Quick Hands Workshop，60 秒／900 分 | mouse | clue_3：Analog Front-End Records |
| Technician | Planetary Defence，20 艘／5 HP | cell phone | clue_4 |
| Clockmaker | Rhythm Repair，48 音／34 命中／最多 12 誤擊 | clock | clue_5 |

五間可自由選順序，先完成遊戲再到相機前掃描物件，五條不同線索解鎖 Abandoned House。
最終信件揭露失蹤研究員就是玩家。

## 現有架構

- 根目錄為 Unity 6000.0.80f1 專案。XRI 3.1.3、OpenXR 1.16.1。
- Build Settings 啟動場景為 `Assets/Scenes/rpgpp_lt_scene_1.0.unity`。
- `Assets/TheForgottenVillage/Scenes/` 已有六間房屋場景、參考村莊。
- `VillageGame`、`VillageSave`、`VillageStorage` 管理進度、inventory、clues 與原子存檔。
- `HouseTransitions` 使用 additive scene；村莊的管理器與玩家保留。
- `VillageUI` 已有 Journal、Camera settings、URL／token 欄位。
- `ObjectRecognitionClient` 已用 UnityWebRequest，但只支援舊 `/v1/quests` 協定、預設模擬。
- 舊 Categories 為 book、bottle、mouse、cell phone、clock；前兩間不需辨識即可領線索。
- 五個遊戲都有真實勝利事件寫入 puzzleSolved；沒有必要新建平行 GameManager。
- Prefabs、Materials 與 NPC 外觀已存在，此次不重建美術。
- 未發現 Python／Pi 服務、YOLO .pt／.onnx 權重；將建立 raspberry_pi。
- AbandonedHouse、進入鎖與結局已有；需統一五物件關卡並補信件文字。

## 驗證原則

Python 假相機／假模型只存在 tests。正式服務只能初始化真實 Camera 與 Detector。
Unity Mock 明示並使用獨立存檔，不允許切回 Live 後沿用模擬線索。
軟體測試不能證明 Pi 相機、五個實物或 VR 實機成功；硬體驗證留給逐項驗收清單。
