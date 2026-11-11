using System;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.UI;
namespace ForgottenVillage {
public class VillageUI : MonoBehaviour {
 public Material panelMaterial,textMaterial; public VillageGame game;public TMP_FontAsset font;public Canvas canvas;public RectTransform panel;public TextMeshProUGUI heading,body,tracker,notice,prompt;public RectTransform buttons;public bool IsOpen=>panel&&panel.gameObject.activeSelf; public TextMeshProUGUI dossier; Coroutine panelMotion; float toastUntil;int journalPage;
 public void Initialize(){canvas=GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;var rect=(RectTransform)transform;rect.sizeDelta=new Vector2(1000,760);transform.localScale=Vector3.one*.0032f;
 panel=Rect("Panel",transform,new Vector2(1000,680),new Vector2(0,0));var bg=panel.gameObject.AddComponent<UnityEngine.UI.Image>();if(panelMaterial)bg.material=panelMaterial;bg.color=new Color(.055f,.09f,.12f,.98f);
 heading=Text("Heading",panel,new Vector2(940,85),new Vector2(0,280),42);body=Text("Body",panel,new Vector2(920,350),new Vector2(0,65),28);buttons=Rect("Actions",panel,new Vector2(930,185),new Vector2(0,-235));dossier=Text("Dossier stamp",panel,new Vector2(920,32),new Vector2(0,225),15);dossier.text="FIELD DOSSIER   /   MEMORY INTERFACE CONTROLLER";dossier.alignment=TextAlignmentOptions.Left;dossier.characterSpacing=2;Rule(panel,new Vector2(920,3),new Vector2(0,208),new Color(.72f,.49f,.24f,.8f));Rule(panel,new Vector2(920,2),new Vector2(0,-329),new Color(.34f,.53f,.58f,.8f));
 tracker=Text("Quest tracker",transform,new Vector2(980,65),new Vector2(0,350),23);tracker.fontStyle=FontStyles.Bold;tracker.characterSpacing=1;notice=Text("Feedback",transform,new Vector2(980,80),new Vector2(0,-335),23);prompt=Text("Prompt",transform,new Vector2(980,45),new Vector2(0,-285),21);}
 void Rule(Transform parent,Vector2 size,Vector2 pos,Color color){var r=Rect("Brass dossier rule",parent,size,pos);var i=r.gameObject.AddComponent<UnityEngine.UI.Image>();if(panelMaterial)i.material=panelMaterial;i.color=color;i.raycastTarget=false;}
 public static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 pos){var o=new GameObject(name,typeof(RectTransform));var r=(RectTransform)o.transform;r.SetParent(parent,false);r.sizeDelta=size;r.anchoredPosition=pos;return r;}
 TextMeshProUGUI Text(string name,Transform parent,Vector2 size,Vector2 pos,int sz){var r=Rect(name,parent,size,pos);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;if(textMaterial)t.fontSharedMaterial=textMaterial;t.fontSize=sz;t.color=new Color(.95f,.92f,.84f);t.alignment=TextAlignmentOptions.Center;t.textWrappingMode=TextWrappingModes.Normal;t.raycastTarget=false;return t;}
 void EnsurePresentation(){if(panel&&!dossier){var existing=panel.Find("Dossier stamp");dossier=existing?existing.GetComponent<TextMeshProUGUI>():null;if(!dossier){dossier=Text("Dossier stamp",panel,new Vector2(920,32),new Vector2(0,225),15);dossier.text="FIELD DOSSIER / MEMORY INTERFACE CONTROLLER";dossier.alignment=TextAlignmentOptions.Left;dossier.characterSpacing=2;Rule(panel,new Vector2(920,3),new Vector2(0,208),new Color(.72f,.49f,.24f,.8f));Rule(panel,new Vector2(920,2),new Vector2(0,-329),new Color(.34f,.53f,.58f,.8f));}}if(tracker){tracker.rectTransform.anchoredPosition=new Vector2(0,350);tracker.fontSize=23;tracker.fontStyle=FontStyles.Bold;}if(notice)notice.rectTransform.anchoredPosition=new Vector2(0,-335);if(prompt)prompt.rectTransform.anchoredPosition=new Vector2(0,-285);}
 int objectPage=-1;
 public void Show(string title,string text){objectPage=-1;EnsurePresentation();panel.gameObject.SetActive(true);heading.text=title;heading.fontStyle=FontStyles.Bold;heading.characterSpacing=3;heading.color=new Color(1,.79f,.47f);body.fontSize=28;body.alignment=TextAlignmentOptions.TopLeft;body.margin=new Vector4(28,20,28,8);body.text=text;dossier.text=title.Contains("Engineering")?"ENGINEERING NOTE   /   LIVE MEASUREMENT":title.Contains("Journal")||title.Contains("CASE NOTE")?"INVESTIGATION JOURNAL   /   EVIDENCE LEDGER":"FIELD DOSSIER   /   MEMORY INTERFACE CONTROLLER";foreach(Transform t in buttons){t.gameObject.SetActive(false);Destroy(t.gameObject);}Place();if(panelMotion!=null)StopCoroutine(panelMotion);panel.localScale=Vector3.one*.975f;panelMotion=StartCoroutine(PanelIn());}
 System.Collections.IEnumerator PanelIn(){float t=0;while(t<1){t=Mathf.Min(1,t+Time.unscaledDeltaTime/.16f);float e=1-Mathf.Pow(1-t,3);panel.localScale=Vector3.one*Mathf.Lerp(.975f,1,e);yield return null;}panel.localScale=Vector3.one;panelMotion=null;}
 void Place(){if(!game.player||!game.player.View)return;var cam=game.player.View.transform;var forward=Vector3.ProjectOnPlane(cam.forward,Vector3.up).normalized;if(forward.sqrMagnitude<.1f)forward=Vector3.forward;transform.position=cam.position+forward*2.4f;transform.rotation=Quaternion.LookRotation(forward);canvas.worldCamera=game.player.View;}
 public void Button(string label,Action action){int count=0;foreach(Transform child in buttons)if(child.gameObject.activeSelf&&child.GetComponent<UnityEngine.UI.Button>())count++;var r=Rect(label,buttons,new Vector2(295,48),new Vector2((count%3-1)*310,62-(count/3)*58));var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();if(panelMaterial)image.material=panelMaterial;image.color=new Color(.13f,.27f,.31f);var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;b.onClick.AddListener(()=>{if(game.audioFeedback)game.audioFeedback.Click();action();});var text=Text("Label",r,new Vector2(285,45),Vector2.zero,24);text.text=label;}
 public void Close(){if(game.recognition.Waiting)return;objectPage=-1;if(!game.playing){Menu();return;}panel.gameObject.SetActive(false);Place();}
 public void Message(string title,string text){Show(title,text);Button("Continue",Close);}
 public void Toast(string text){notice.text=text;toastUntil=Time.unscaledTime+7;}
 public void Prompt(string text){prompt.text=text;}
 public void RefreshTracker(){if(tracker)tracker.text="THE MISSING RESEARCHER  |  "+game.Objective;}
 void Update(){if(notice&&Time.unscaledTime>toastUntil)notice.text="";if(!IsOpen&&game.player&&game.player.View){var cam=game.player.View.transform;transform.position=cam.position+cam.forward*2.4f;transform.rotation=cam.rotation;}if(!IsOpen&&UnityEngine.InputSystem.Keyboard.current?.tabKey.wasPressedThisFrame==true)Pause();}
 public void Menu(){game.recognition.Cancel();notice.text="";prompt.text="";game.playing=false;Show("THE FORGOTTEN VILLAGE","A VR mystery RPG\n\nFive village clues. One missing researcher.\nTwo recovered memories and three real-world objects. One sealed archive chest.");Button("Start Game",game.NewGame);Button("Continue",game.Continue);Button("Settings",Settings);Button("Quit",()=>Application.Quit());}
 public void Pause(){var flight=FindFirstObjectByType<PlaneDreamRun>();if(flight&&flight.InDream){flight.PauseMenu();return;}Show("Field journal / Pause",game.Objective+"\n\n"+game.hardware.status);Button("Resume",Close);var tennis=FindFirstObjectByType<ElderTennis>();if(tennis&&tennis.Running){Button("Restart match",tennis.StartMatch);Button("Leave table",tennis.StopMatch);}Button("Journal",Journal);Button("Inventory",Inventory);Button("Settings",Settings);Button("Save & Menu",()=>{game.Commit();Menu();});Button("Reset Progress",()=>{Show("Reset progress?","This starts a new investigation.");Button("New Game",game.NewGame);Button("Cancel",Pause);});}
 public void Settings(){Show("Settings / Safety console","Mode: "+(game.player.IsVR?"VR: controller movement, snap turn and rays":"Desktop: WASD, mouse look, E interact; J journal, I inventory, Esc menu")+"\n"+game.hardware.status+"\nSerial: "+game.hardware.portName+" @ "+game.hardware.baud+"\nAudio: "+Mathf.RoundToInt(AudioListener.volume*100)+"%");Button("Simulation / USB",()=>{game.hardware.simulation=!game.hardware.simulation;game.hardware.Connect();Settings();});Button("Next COM port",()=>{int.TryParse(game.hardware.portName.Replace("COM",""),out int p);game.hardware.portName="COM"+(p%20+1);Settings();});Button("Connect",()=>{game.hardware.Connect();Settings();});Button("Volume -",()=>{AudioListener.volume=Mathf.Clamp01(AudioListener.volume-.1f);Settings();});Button("Volume +",()=>{AudioListener.volume=Mathf.Clamp01(AudioListener.volume+.1f);Settings();});Button("Camera settings",CameraSettings);Button("Back",()=>{if(game.playing)Pause();else Menu();});}
 public void Journal(){
  int i=journalPage;
  if(i==4){Show("CASE NOTE 05 / 05", "Clockmaker's House / Rhythm repair\n"+(game.state.clockRhythmWon?"Clocks REPAIRED. ":"Strike 23 of 32 notes at the gold mark. ")+(game.state.clues[4]?"Record recovered.\n\n"+VillageGame.Notes[4]:!game.state.clockRhythmWon?"Visit the Clockmaker and play the three bells.":!game.state.clockMemoryRecovered?"Speak to the Clockmaker to hear his recovered memory.":"Present the requested object to recover the timing record."));Button("Previous",()=>{journalPage=3;Journal();});Button("Next",()=>{journalPage=0;Journal();});if(game.state.clockMemoryRecovered)Button("Object quest",()=>ObjectQuest(4));Button("Close",Close);return;}
  if(i==0){Show("CASE NOTE 01 / 05", "Elder's House — Table Tennis\n"+(game.state.elderTennisWon?"Match WON. ":"Win a best-of-three match. ")+(game.state.clues[0]?"Clue recovered.\n\n"+VillageGame.Notes[0]:game.state.elderMemoryRecovered?"Inspect the unlocked photograph to recover Clue #1.":"Help the Elder recover his memory, then inspect his photograph."));Button("Previous",()=>{journalPage=4;Journal();});Button("Next",()=>{journalPage=1;Journal();});Button("Close",Close);return;}
  if(i==1){Show("CASE NOTE 02 / 05", "Alchemist's House — Endless Sky\n"+(game.state.alchemistDreamWon?"Dream completed. ":"Survive the airplane dream for 60 active seconds. ")+(game.state.clues[1]?"Record recovered.\n\n"+VillageGame.Notes[1]:game.state.alchemistMemoryRecovered?"Inspect the unlocked dream record.":"Help the Alchemist end her recurring dream, then inspect her evidence."));Button("Previous",()=>{journalPage=0;Journal();});Button("Next",()=>{journalPage=2;Journal();});Button("Close",Close);return;}
  string n=(i+1).ToString("00");
  string text=VillageGame.Houses[i]+"\nPuzzle: "+(game.state.puzzleSolved[i]?"COMPLETED":"not solved")+" | Object: "+ObjectQuests.Categories[i]+"\n";
  text+=game.state.ObjectVerified(i)?(game.state.objects[i].simulated?"SIMULATED evidence - not a real camera check":"CAMERA VERIFIED - "+game.state.objects[i].confidence.ToString("P0")+" confidence")+"\n":"Object verification pending\n";
  text+=game.state.clues[i]?"\nRECOVERED: "+VillageGame.Clues[i]+"\n"+VillageGame.Notes[i]:"\nSolve the puzzle, speak to the resident, present the requested object to the camera, then return to collect the clue.";
  Show("CASE NOTE "+n+" / 05",text);Button("Previous",()=>{journalPage=(journalPage+4)%5;Journal();});Button("Next",()=>{journalPage=(journalPage+1)%5;Journal();});Button("Object quest",()=>ObjectQuest(i));Button("Close",Close);
 }
 int inventoryPage;public void Inventory(){if(game.state.inventory.Count==0){Message("Inventory","Your satchel is empty. Inspect evidence and collect supplies inside the houses.");return;}inventoryPage%=game.state.inventory.Count;string id=game.state.inventory[inventoryPage];int clue=Array.IndexOf(VillageGame.Clues,id);Show("Satchel ??"+(inventoryPage+1)+" / "+game.state.inventory.Count,"[ FIELD RECORD ]\n"+id+"\n\n"+(clue>=0?VillageGame.Notes[clue]:id=="Scratched photograph"?"Six villagers. A scratched-out face. A familiar researcher's wristband.":"A collected village specimen, retained in your satchel for inspection."));Button("Previous",()=>{inventoryPage=(inventoryPage+game.state.inventory.Count-1)%game.state.inventory.Count;Inventory();});Button("Next",()=>{inventoryPage++;Inventory();});Button("Close",Close);}
 public void Dialogue(int npc){
  if(npc<0||npc>=5)return;
  if(npc==4&&game.house==4){var clock=FindFirstObjectByType<ClockRhythmHouse>();if(clock){clock.Offer();return;}}
  if(npc==1&&game.house==1){var dream=FindFirstObjectByType<AlchemistDreamHouse>();if(dream){dream.Offer();return;}}
  if(npc==0&&game.house==0){var tennis=FindFirstObjectByType<ElderTennis>();if(tennis){tennis.Offer();return;}}
  if(game.state.elder&&game.state.puzzleSolved[npc]&&!game.state.clues[npc]){ObjectQuest(npc);return;}
  string[] lines;
  if(npc==0&&!game.state.elder)lines=new[]{"You are not from here, are you?","Something happened last night.","One of our villagers disappeared, but none of us can remember exactly what happened.","Please... investigate the village. Maybe you can discover the truth."};
  else if(game.state.clues[npc]){string[][] after={
   new[]{"You recovered our photograph. That face in the photograph wore the research wristband you carry.","I remember playing table tennis with you. Why did we forget?"},
   new[]{"The airplane has reached the horizon. I remember the experiment now.","That recurring dream was part of a memory experiment. The same signatures appear on the Carpenter's board."},
   new[]{"You recovered the analog front-end record. The inverted waveform is exactly what that common-source stage predicts.","The schematic is initialled M.I.C. The technician may know what software ran on it."},
   new[]{"Your reproduction matches both the deployed RUN and the reference-fix STOP.","That is direct software evidence. The Clockmaker's negative slack is concerning, but it does not prove what caused the event."},
   new[]{"The fifth record completes the engineering chain. Its 10 ns path has negative setup slack.","That is a credible timing risk, not a proven cause. The experiment log and faulty safety decision are stronger incident evidence."}
  };lines=after[Mathf.Clamp(npc,0,4)];}
  else {string[][] before={
   new[]{"That photograph once showed all six of us. Why was one face erased?","Come to my house. Perhaps a game of table tennis will help me remember."},
   new[]{"My dream is an endless sky. Can you guide the airplane to the horizon?","Come to my house and help me end the dream."},
   new[]{"The front end inverted and amplified the signal. I built it from the Technician's controller notes.","Use gm = 2 mS and RL = 10 kilohms. Find a drain resistance for gain near -8."},
   new[]{"The unsafe sensor was asserted, yet the deployed controller logged RUN. I remember the status lamp turning green.","Run the pass-by-value diagnostic, then preserve the derived override with a const reference. The physical button repeats the deployed test."},
   new[]{"All my clocks stopped that night. Their rhythm might help me remember.","Come to my house. Help me ring the bells and repair the clocks."}
  };lines=before[Mathf.Clamp(npc,0,4)];}
  int index=0;Action advance=null;advance=()=>{if(index>=lines.Length){if(npc==0){game.state.elder=true;game.Commit();}Close();return;}Show(npc==0?"Village Elder":VillageGame.Houses[npc].Replace("'s House",""),lines[index++]);Button(index==lines.Length?"Finish conversation":"Next",advance);};advance();
 }
 public void RefreshObjectQuest(int house){if(IsOpen&&objectPage==house)ObjectQuest(house);}
 public void ObjectQuest(int id){
  if(id<0||id>=5)return;
  if(id<2){journalPage=id;Journal();return;}
  bool solved=game.state.puzzleSolved[id],verified=game.state.ObjectVerified(id);
  string text=VillageGame.Houses[id]+" requests a "+ObjectQuests.Categories[id]+" - "+ObjectQuests.Roles[id]+".\n\n";
  if(!solved)text+=id==4?"Repair the clocks in the rhythm game first.":"Complete the engineering puzzle first.";
  else if(verified)text+=(game.state.objects[id].simulated?"SIMULATED VERIFICATION\nNo real camera evidence was captured.":"CAMERA VERIFIED\nEvidence snapshot saved on the Pi.")+"\n\n"+(game.state.clues[id]?"Your clue is already in the journal.":"Welcome back. Collect the resident's clue to complete this house.");
  else text+="Start verification, remove your headset, and show the object to the fixed camera. Return to VR after verification.\n\n"+game.recognition.status;
  Show("Object quest - "+ObjectQuests.Categories[id],text);objectPage=id;
  if(solved){
   if(verified&&!game.state.clues[id])Button("Collect clue",()=>{if(game.Award(id))ObjectQuest(id);});
   if(!verified||(game.state.objects[id].simulated&&!game.recognition.simulation))
    Button(game.recognition.Waiting?"Restart camera check":"Start verification",()=>{game.recognition.Begin(id);ObjectQuest(id);});
   if(game.recognition.simulation&&game.recognition.ActiveQuest==id)
    Button("Simulate detection",()=>{game.recognition.SimulateDetection(ObjectQuests.Categories[id]);ObjectQuest(id);});
  }
  if(game.recognition.Waiting)Button("Cancel verification",()=>{game.recognition.Cancel();ObjectQuest(id);});
  Button("Camera settings",CameraSettings);Button("Close",()=>{game.recognition.Cancel();Close();});
 }
 public void CameraSettings(){
  game.recognition.Cancel();
  Show("Object camera connection","Mode: "+(game.recognition.simulation?"SIMULATION - no camera evidence":"LIVE Raspberry Pi camera")+"\nEnter the Pi service URL and its access token. The token is kept only for this session.");
  var address=ConnectionField("Camera URL",game.recognition.endpoint,15,false);
  var token=ConnectionField("Access token",game.recognition.accessToken,-60,true);
  Button("Apply connection",()=>{game.recognition.endpoint=address.text.Trim().TrimEnd('/');game.recognition.accessToken=token.text;Settings();});
  Button("Simulation / Live",()=>{game.recognition.endpoint=address.text.Trim().TrimEnd('/');game.recognition.accessToken=token.text;game.recognition.simulation=!game.recognition.simulation;CameraSettings();});
  Button("Back",Settings);
 }
 TMP_InputField ConnectionField(string name,string value,float y,bool secret){
  var rect=Rect(name,buttons,new Vector2(880,48),new Vector2(0,200+y));
  var background=rect.gameObject.AddComponent<UnityEngine.UI.Image>();background.color=new Color(.12f,.2f,.24f);if(panelMaterial)background.material=panelMaterial;
  var input=rect.gameObject.AddComponent<TMP_InputField>();var label=Text("Value",rect,new Vector2(850,44),Vector2.zero,23);label.alignment=TextAlignmentOptions.MidlineLeft;
  input.textViewport=rect;input.textComponent=label;input.contentType=secret?TMP_InputField.ContentType.Password:TMP_InputField.ContentType.Standard;input.characterLimit=256;input.text=value;
  return input;
 }
 public void Ending(){Show("THE FORGOTTEN VILLAGE   /   REMEMBERED","The researcher who disappeared was never a stranger.\n\nThe missing researcher... was you.\n\nYour student ID and signed letter in the archive chest identify you as the missing researcher. Deployed software and runtime logs confirm the object-slicing failure. Negative setup slack remains a separate engineering risk.\n\n"+game.hardware.status);Button("Return to Menu",Menu);Button("Replay",game.NewGame);Button("Retry chest latch",()=>{game.hardware.RequestGate(game.state);Ending();});}
}
}




