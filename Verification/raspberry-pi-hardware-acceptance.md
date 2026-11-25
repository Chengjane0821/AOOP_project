# 實體設備驗收紀錄（尚未填寫，不代表已通過）

日期：______　操作人：______　Commit：______
Pi 型號／RAM：______　OS／Python：______　Camera／排線：______
模型／SHA256：______　頭盔／連線方式：______　LAN：______

| 項目 | 預期 | 實測結果／證據 |
|---|---|---|
| 冷開機 | systemd active，health ready | 未測 |
| CSI／USB 影像 | 正確色彩、清楚對焦、無其他程序搶相機 | 未測 |
| keyboard 10 次 | Elder 成功率與每次耗時 | 未測 |
| laptop 10 次 | Alchemist 成功率與每次耗時 | 未測 |
| mouse 10 次 | Carpenter 成功率與每次耗時 | 未測 |
| cell phone 10 次 | Technician 成功率與每次耗時 | 未測 |
| clock 10 次 | Clockmaker 成功率與每次耗時 | 未測 |
| 每屋錯誤物件 5 次 | 不發錯誤線索 | 未測 |
| 低光／無物件 | timeout，不發線索 | 未測 |
| 拔相機 | camera_error 或受控逾時，health degraded | 未測 |
| 關閉服務 | Unity 有錯誤，沒有線索；重啟後可再試 | 未測 |
| 尚未贏遊戲 | Start verification 不可通過 | 未測 |
| 同 ID 重送 | 同一工作，無第二個相機擁有者 | 未測 |
| 頭盔脫戴 | 回來可取結果或明確逾時，不重複加進度 | 未測 |
| 線索領兩次 | 保持相同 x/5 | 未測 |
| 每屋返回／Continue | 證據與線索保存 | 未測 |
| 四條線索 | Abandoned House 仍鎖定 | 未測 |
| 五條線索 | 房屋解鎖、調查、信件可到達 | 未測 |
| Mock 切回 Live | 模擬線索不可使用 | 未測 |
| Pi 重開機／IP 更新 | 設定更新後恢復連線 | 未測 |

可貼測試照片的本機檔名、時間與 requestId，但不要填 token、密碼或含私人資料的相片。
失敗記錄應包含 health message、Pi journal 與 Unity Console 訊息，不應只寫「無法連線」。
