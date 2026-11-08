using UnityEngine;
using UnityEngine.XR;
using TMPro;
namespace ForgottenVillage {
public class ElderTennis : MonoBehaviour {
 public VillageGame game; public TennisTuning tuning;public TennisBall ball;
 public TennisPaddle playerPaddle,elderPaddle;public Transform playerStanding,npcStanding;
 public TextMeshPro scoreboard,instructions,feedback;public GameObject photograph;
 public TennisRules rules=new TennisRules();public bool Running {get;private set;}
 public bool RallyActive=>Running&&!paused&&rules.phase==TennisRules.Phase.Rally;
 public bool PlayerTurn=>lastPlayer==false;
 bool lastPlayer,landed,paused;int bounces;float timer,rallyTime,feedbackTime;Vector3 pausedVelocity,pausedSpin;
 public void Bind(VillageGame value){game=value;playerPaddle.match=this;elderPaddle.match=this;ball.match=this;RefreshEvidence();ball.Park(transform.TransformPoint(new Vector3(0,1.2f,0)));RefreshScore();}
 public void Offer(){if(!game)return;if(Running){game.ui.Show("Table tennis","Continue your match or start again with a fresh score.");game.ui.Button("Resume",game.ui.Close);game.ui.Button("Restart match",StartMatch);game.ui.Button("Leave table",StopMatch);return;}
  if(game.state.elderTennisWon&&!game.state.elderMemoryRecovered){Memory();return;}
  game.ui.Show("Village Elder",game.state.elderTennisWon?"I remember our missing friend. Please inspect the scratched photograph. We can also play another match.":"Ah... a visitor? I can barely remember anything anymore.\nSomething happened last night, but my memories are confused.\nI used to love table tennis. Maybe a match will bring something back.\nBeat me, and perhaps I'll remember our missing villager.");
  game.ui.Button("Let's play!",StartMatch);game.ui.Button("Maybe later",game.ui.Close);
  game.ui.Button(playerPaddle.hand==XRNode.RightHand?"Use left hand":"Use right hand",()=>{playerPaddle.hand=playerPaddle.hand==XRNode.RightHand?XRNode.LeftHand:XRNode.RightHand;Offer();});
 }
 public void StartMatch(){if(!game||!game.playing||game.house!=0)return;rules.BeginMatch();Running=true;game.player.ActivityLocksMovement=true;paused=false;timer=3;rallyTime=0;
  game.player.Teleport(playerStanding.position,playerStanding.eulerAngles.y);playerPaddle.gameObject.SetActive(true);playerPaddle.ResetTracking();elderPaddle.ResetTracking();
  ball.Park(transform.TransformPoint(new Vector3(0,1.25f,.9f)));game.ui.Close();Feedback("GET READY",3);RefreshScore();
 }
 public void StopMatch(){Running=false;if(game)game.player.ActivityLocksMovement=false;paused=false;rules.phase=TennisRules.Phase.Idle;ball.Park(transform.TransformPoint(new Vector3(0,1.2f,0)));playerPaddle.gameObject.SetActive(false);if(game)game.ui.Close();RefreshScore();}
 void Update(){if(!game)return;if(feedbackTime>0){feedbackTime-=Time.unscaledDeltaTime;if(feedbackTime<=0&&feedback)feedback.text="";}
  if(!Running)return;
  bool shouldPause=game.ui.IsOpen||!game.playing||(game.player.IsVR&&!playerPaddle.TrackingValid);
  if(shouldPause!=paused){paused=shouldPause;if(paused){pausedVelocity=ball.Body.linearVelocity;pausedSpin=ball.Body.angularVelocity;ball.Body.isKinematic=true;}else if(rules.phase==TennisRules.Phase.Rally){ball.Body.isKinematic=false;ball.Body.linearVelocity=pausedVelocity;ball.Body.angularVelocity=pausedSpin;playerPaddle.ResetTracking();}}
  if(paused){if(scoreboard)scoreboard.text="PAUSED\nClose the menu / restore controller tracking";return;}
  timer-=Time.deltaTime;
  if(rules.phase==TennisRules.Phase.Countdown&&timer<=0)Serve();
  else if(rules.phase==TennisRules.Phase.GameOver&&timer<=0){rules.BeginGame();timer=3;}
  else if(rules.phase==TennisRules.Phase.Rally){rallyTime+=Time.deltaTime;if(rallyTime>tuning.rallyTimeout)BallOut();}
  RefreshScore();
 }
 void Serve(){rules.BeginRally();lastPlayer=false;landed=false;bounces=0;rallyTime=0;
  var at=new Vector3(0,1.2f,1.15f);var velocity=TennisShots.ToTarget(at,new Vector3(0,.79f,-.85f),.85f);
  ball.Serve(transform.TransformPoint(at),transform.TransformDirection(velocity));Feedback("PLAY",1);
 }
 public bool PaddleContact(TennisPaddle paddle,Vector3 worldPosition,Vector3 worldIncoming){if(!RallyActive)return false;
  bool player=paddle.player;
  // A return is allowed only after one bounce on the receiver's half.
  if(player==lastPlayer||!landed||bounces!=1)return false;
  if(!player&&rules.elderMustMiss)return false;
  var at=transform.InverseTransformPoint(worldPosition);var incoming=transform.InverseTransformDirection(worldIncoming);
  var swing=transform.InverseTransformDirection(paddle.velocity);var normal=transform.InverseTransformDirection(paddle.transform.forward);
  if(player&&swing.magnitude<tuning.minimumSwingSpeed)return false;
  bool smash=player&&paddle.Grip;
  if(smash&&!TennisShots.IsSmash(tuning,at,incoming,swing,normal)){Feedback("MISS! — smash timing",2);EndRally(false);return true;}
  if(player&&(at.z>=0||Vector3.Dot(swing-incoming,normal)*normal.z<=0))return false;
  if(!player&&at.z<=0)return false;
  Vector3 outgoing=player?TennisShots.PlayerReturn(tuning,at,incoming,swing,normal,smash):TennisShots.ToTarget(at,new Vector3(Mathf.Clamp(at.x*.25f,-.3f,.3f),.79f,-.85f),.82f);
  if(!rules.Contact(player,smash))return false;lastPlayer=player;landed=false;bounces=0;rallyTime=0;
  ball.Strike(transform.TransformDirection(Vector3.ClampMagnitude(outgoing,tuning.maximumBallSpeed)));
  if(smash)Feedback("SMASH!  "+rules.smashes+" / 2",1.4f);
  if(feel)feel.Hit(player,smash);return true;
 }
 public TennisFeedback feel;
 public void TableBounce(Vector3 position){if(!RallyActive)return;bool onPlayerSide=transform.InverseTransformPoint(position).z<0;
  bool receiverSide=onPlayerSide!=lastPlayer;
  if(!receiverSide){EndRally(!lastPlayer);return;}landed=true;bounces++;
  if(feel)feel.Bounce();if(bounces>=2)EndRally(lastPlayer);
 }
 public void NetFault(){if(RallyActive){Feedback("NET",1);EndRally(!lastPlayer);}}
 public void BallOut(){if(RallyActive)EndRally(landed?lastPlayer:!lastPlayer);}
 void EndRally(bool playerWon){if(!RallyActive)return;rules.Fault(playerWon);ball.Park(ball.Body.position);timer=3;
  string result=playerWon?"Point to you":"Point to Elder";
  if(rules.phase==TennisRules.Phase.GameOver)result=playerWon?"GAME WON":"GAME LOST";
  Feedback(result,3);
  if(rules.phase==TennisRules.Phase.Won){Running=false;game.player.ActivityLocksMovement=false;playerPaddle.gameObject.SetActive(false);game.state.elderTennisWon=true;game.state.puzzleSolved[0]=true;game.Commit();Memory();}
  else if(rules.phase==TennisRules.Phase.Lost){Running=false;game.player.ActivityLocksMovement=false;playerPaddle.gameObject.SetActive(false);rules.ResetLoss();game.ui.Show("MATCH LOST","The Elder won two games. Your next attempt starts at 0–0, with fresh hit targets and smash counters.");game.ui.Button("Retry match",StartMatch);game.ui.Button("Maybe later",game.ui.Close);}
  RefreshScore();
 }
 public void Memory(){game.ui.Show("Wait... I remember!","There was another person living here. We used to play table tennis together.\nThey kept asking about memories and strange experiments.\nI remember an old photograph... someone's face was scratched out.\nPlease take a look. Perhaps it will help you discover what happened.");game.ui.Button("Look at the photograph",()=>{game.state.elderMemoryRecovered=true;game.Commit();RefreshEvidence();game.ui.Close();game.ui.Toast("Inspect the photograph beside the table to collect Clue #1.");});}
 public void RefreshEvidence(){if(photograph&&game)photograph.SetActive(game.state.elderTennisWon&&game.state.elderMemoryRecovered);}
 void RefreshScore(){if(!scoreboard)return;scoreboard.text="ELDER'S TABLE TENNIS\n"+(Running?"Game "+Mathf.Min(3,rules.GameNumber):"Best of three")+"  |  Games "+rules.playerGames+" : "+rules.elderGames+"\nPoints  YOU "+rules.playerPoints+" : "+rules.elderPoints+" ELDER\nRally hits "+rules.rallyHits+"  |  Game hits "+rules.gameHits+" / "+rules.threshold+"\nSmashes "+rules.smashes+" / 2"+(Running&&rules.phase==TennisRules.Phase.Countdown?"\nServe in "+Mathf.Max(1,Mathf.CeilToInt(timer)):"");}
 public void Feedback(string text,float seconds){if(feedback)feedback.text=text;feedbackTime=seconds;}
 void OnDisable(){Running=false;if(game&&game.player)game.player.ActivityLocksMovement=false;if(playerPaddle)playerPaddle.gameObject.SetActive(false);}
}
}
