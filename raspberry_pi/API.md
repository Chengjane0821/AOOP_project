# Recognition API v1

所有端點要求 `X-FV-Token`，內容 `application/json`。只部署信任的區域網路，不開路由器轉送。
HTTP token 不提供加密；共用／不信任網路應改用 HTTPS reverse proxy，Unity URL 可填 HTTPS。
伺服器不寫 Unity 存檔；遊戲完成條件只由 Unity 現有 VillageSave 判斷。

## Health

`GET /health`：200 只代表 HTTP 可用，必須檢查 ready、camera、model、protocol=1。
初始化失敗時仍提供 degraded health；token 不合格則拒絕啟動。
ready=false、busy=true 表示相機有另一筆辨識；不是相機壞掉。

```json
{"status":"ok","camera":true,"model":true,"ready":true,"busy":false,"message":"Camera capture and model warmup passed","protocol":1}
```

## 建立辨識

`POST /verify` → 202。相同 ID＋相同 payload 回傳原工作，不重開相機。
相同 ID 用於不同房屋 → 409；房屋物件不匹配 → 422；無相機／模型 → 503；忙碌或 cache 滿 → 429。

```json
{"request_id":"0123456789abcdef0123456789abcdef","house_id":"elder","expected_object":"keyboard"}
```

house_id：elder、alchemist、carpenter、technician、clockmaker。
expected_object：keyboard、laptop、mouse、cell phone、clock。phone／mobile phone 正規化為 cell phone。
ID 必須是 32 個小寫十六進位字元。模型期望類別取自伺服器 catalog，而非任意客戶端輸入。

## 查詢

`GET /verify/0123456789abcdef0123456789abcdef` → 200 或未知／過期 404。
結果保留 600 秒，最多 128 筆；服務重啟不保留結果。過期後不保證跨重啟去重，Unity 應產生新 ID。
Unity 最多等 120 秒，HTTP 單次 5 秒；POST 重試三次沿用同 ID，GET 容許連續兩次暫時失敗。

```json
{"requestId":"0123456789abcdef0123456789abcdef","questIndex":0,"status":"success","objectName":"keyboard","confidence":0.88,"frames":3,"snapshot":"0123456789abcdef0123456789abcdef.jpg","capturedUtc":"2026-11-25T09:00:00+00:00","simulated":false,"message":"Stable camera observation verified"}
```

保留 camelCase 回應以相容既有 Unity ObjectEvidence 存檔；請求採 snake_case。
questIndex 由 0 到 4；同房屋對應同索引。

| status | 意義 | 發線索 |
|---|---|---|
| pending | 正在辨識 | 否 |
| success | 正確類別、至少 .70、至少 3 連續影格、已保存相片 | Unity 再驗證條件後才可領取 |
| failed | 期限內只有不符的高信心物件 | 否 |
| timeout | 沒有穩定匹配、低信心或原生推論卡住 | 否 |
| camera_error | 初始化、讀取或相片保存失敗 | 否 |
| model_error | 模型推論／工作程序失敗 | 否 |

confidence 是確認序列的最低信心值。同一影格多個框不會被當成多影格。
若畫面同時含多物件，只要正確物件達門檻可通過；展示時最好只放指定物件。
相機／模型故障或原生呼叫超時後，服務變 degraded；排除原因並 restart 服務。
取消 Unity 請求不立即取消 Pi 工作，最多等待一次 INFERENCE_TIMEOUT+5 秒後可重試。
health 是初始化與最近辨識的真實狀態，不是閒置時持續插拔偵測；拔相機後下一次讀取才會標記失敗。

## 資料保存

成功影像留在 `raspberry_pi/evidence/`，不提供公開下載端點、不提交 Git。
程式不記錄 token。可用 `scripts/cleanup_evidence.sh` 預覽七天前影像，再加 `--delete` 刪除。
模型名稱表與影像結果請在實體物件測試時自行檢查；本 API 不能證明物件真偽、所有權或防止照片欺騙。
