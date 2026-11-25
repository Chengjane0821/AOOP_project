# V=IR 整合驗證結果

日期：2026-11-25。起點 main：`a6b3c41`。

## 已真正執行

- Python 3.12.3 / Linux x86_64：**51 passed**。
  - 使用 requirements-dev.txt 中的 FastAPI 0.115.12、Pydantic 2.11.4、pytest 8.3.5、httpx 0.28.1。
  - 五類與伺服器房屋映射、錯誤物件、NaN／低信心、不連續影格、辨識期限、相機／模型錯誤。
  - FastAPI token、重複 ID、ID 衝突、未知 ID、忙碌回應、結果 TTL、容量上限。
  - 相機單一擁有者／釋放、原生呼叫逾時、模型缺檔／缺類／CPU 參數。
  - 啟動實際 Uvicorn loopback HTTP socket，測 health、JSON POST、關閉服務後連線失敗。
  - 相機與 YOLO 測試採測試替身，不是實物辨識。真實網路傳輸測試仍採假辨識 engine。
  - 原始結果：`Verification/recognition-python.xml`。
- Unity 6000.0.80f1 Editor：**127 passed，0 failed，0 skipped**。
  - 包含既有五遊戲規則、存檔回歸與新五物件／Mock／最終解鎖測試。
  - 實際開啟村莊 Scene 檢查已掛 ObjectRecognitionClient、Live 預設、空白 token、六個啟用房屋目的地。
  - 原始結果：`Verification/recognition-editmode.xml`、`recognition-editmode.txt`。
- `Verification/Compile.ps1`：runtime 與 Editor 程式對已安裝 Unity 參考組件編譯通過。
- Python compileall、全部 Pi shell scripts 的 bash -n、git diff --check：通過。
- 完整讀取專題 PDF 兩頁，確認五屋、五物件、六屋與研究員自我揭露設定。

Python 顯示一個 Starlette/AnyIO BlockingPortal deprecation warning，不影響測試結果。
Unity 編譯仍有既有素材／課程程式使用已棄用 Unity API 的 warnings；本次沒有把它們當作新錯誤。

## 程式與模型檢查，非實機成功

- 官方 COCO class table 支援 keyboard 66、laptop 63、mouse 64、cell phone 67、clock 74。
- 本 Repo 無既有權重；新增明確下載 YOLO11n 與檢查實際 model.names 的工具。
- 本次未在此機下載／載入真實 YOLO11n 權重，也未測五個實體物件辨識準確率。
- 安裝腳本限定 Bookworm ARM64，使用 apt Picamera2／libcamera 與 --system-site-packages venv。
- systemd 模板由安裝腳本填實際 Pi 使用者／checkout 路徑，未在 Pi 實機啟用。

## 未驗證與必須上硬體的部分

- Raspberry Pi 4／5 的 OS 安裝、ARM64 Torch wheel／NumPy ABI、CPU 效能與溫度。
- CSI 排線／USB 相機擷取、顏色、對焦、實物五類辨識、曝光與時鐘外觀差異。
- 區網 PC→Pi 與 Quest→Pi HTTP、路由器隔離、防火牆、IP 改變。
- VR 裝置脫戴、應用暫停／恢復、完整五屋實際人類遊玩與回訪。
- Android APK 建置／裝置執行、既有 Arduino 與實體門閂。
- 實體 Scene 切換後的完整 VR 遊玩未重新跑 PlayMode 通關；此次做 EditMode 存檔 roundtrip 與場景掛接檢查。

對應驗收表：`Verification/raspberry-pi-hardware-acceptance.md`；逐條操作步驟在根目錄指南。

## 進度接口

沿用 VillageGame／VillageSave／VillageStorage，沒有平行 ClueManager。
五個小遊戲的勝利回呼均已存在，會寫入 puzzleSolved 與各自勝利欄位；未新增正式跳關接口。
ObjectRecognitionClient 有 VerificationSucceeded、VerificationFailed；VillageGame 有 ClueCollected。
HouseTransitions 在離開房屋時取消本機輪詢，Pi 原工作在有界時間內結束。
現有 Elder 照片、Alchemist 紀錄及居民恢復記憶條件保留；指南說明先完成這些敘事步驟再領線索。

## 重跑

在 repository 根目錄，使用已安裝測試依賴的 Python 執行：
`python -m pytest raspberry_pi/tests -q --junitxml=Verification/recognition-python.xml`。
用途是重跑軟體測試，預期 51 通過；若缺套件依指南安裝，完成後再做硬體驗收。
Unity 選 Tools → Forgotten Village → Run recognition and progress tests。
