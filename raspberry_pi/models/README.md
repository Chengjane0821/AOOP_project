# YOLO 權重、類別與替換流程

預設 `raspberry_pi/models/yolo11n.pt`，Ultralytics YOLO11 nano、CPU、416px、2 threads。
權重不放 Git；由 `../scripts/download_model.sh` 明確下載，啟動時絕不自動下載替代模型。
Pi 4 速度可能偏低；可把 INFERENCE_SIZE 改成 320（32 的倍數），並改善照明。

官方 [COCO 類別表](https://github.com/ultralytics/ultralytics/blob/main/ultralytics/cfg/datasets/coco.yaml)：
keyboard=66、laptop=63、mouse=64、cell phone=67、clock=74（零起算）。
類別存在不保證你的實物、角度、照明下能辨識；clock 通常宜用有清楚錶面的一般時鐘。
程式使用載入權重的 names，沒有把數字 ID 寫死。缺少任一必要類別會拒絕服務就緒。
參考 [Pi 部署](https://docs.ultralytics.com/guides/raspberry-pi/) 與 [YOLO11](https://docs.ultralytics.com/models/yolo11/)。
依賴套件與模型的授權見 [Ultralytics licensing](https://www.ultralytics.com/license)。

## 自訂模型

五個 COCO 類別不必為了「類別缺失」重新訓練。若實物外觀導致召回率不足：

1. 收集每類不同距離、背景、角度與照明的實際相片，另收無目標負例。
2. 以 YOLO bounding boxes 標記；train／val／test 按拍攝場次分開，避免同一場相鄰影格洩漏。
3. 準備 data.yaml，names 必須含 keyboard、laptop、mouse、cell phone、clock。
4. 在具足夠算力的開發電腦安裝同版本 Ultralytics，執行 `yolo detect train model=yolo11n.pt data=data.yaml imgsz=416 epochs=100`。用途為訓練；預期產出 runs/detect/train/weights/best.pt；若資料路徑錯誤先修正 YAML，再訓練；完成後在保留測試集評估。
5. 將 best.pt 放 Pi 的 models/custom.pt，`.env` 的 MODEL_PATH 改為 models/custom.pt。
6. 在 Pi 的 raspberry_pi 目錄執行 `.venv/bin/python -m app.inspect_model`。預期列出五類；缺類直接失敗，修正資料與模型再部署。
7. 停止服務後，以 `app.image_test` 測試每個實物照片及錯誤物件，再重新啟動並走五屋流程。

不得把未知類別全部映射成 expected_object，不得用低門檻掩蓋錯誤率。
下載腳本產生 `.sha256.local` 供本機比對版本；它不是遠端簽章或供應鏈驗證。
