# V=IR Raspberry Pi 真實相機服務

完整安裝與遊玩步驟：[`../RASPBERRY_PI_SETUP_AND_PLAY_GUIDE.md`](../RASPBERRY_PI_SETUP_AND_PLAY_GUIDE.md)。

正式服務：Picamera2（CSI）或 OpenCV（USB）→ YOLO11n CPU → 連續三影格確認 → FastAPI。
沒有 mock 相機選項；測試替身只在 tests。啟動前必須完成相機擷取及模型 warmup。
一個 Uvicorn worker、一個相機子程序、同時一筆辨識；卡住的原生推論由父程序終止。

安裝基準為 Raspberry Pi OS Bookworm 64-bit、Pi 4／5、Python 3.11。
請在 Pi 的 repository/raspberry_pi 目錄執行 `bash scripts/install.sh`（安裝依賴，成功後下載模型）；
`bash scripts/download_model.sh`（明確下載權重，成功後檢查相機）；
`bash scripts/check_camera.sh`（儲存真實相片，失敗先排除接線／佔用）；
`bash scripts/start_server.sh`（啟動 API，下一步測試 health）。
逐條命令的預期輸出與故障排除以根目錄指南為準。

- `.env` 設定 token 與 LAN 位址；不要分享或提交。
- `GET /health`、`POST /verify`、`GET /verify/{request_id}` 均要求 X-FV-Token。
- 協定與範例：[API.md](API.md)。
- 模型與自訂訓練：[models/README.md](models/README.md)。
- 沒有真實 Pi／Camera 的 CI 不能證明硬體可用。
