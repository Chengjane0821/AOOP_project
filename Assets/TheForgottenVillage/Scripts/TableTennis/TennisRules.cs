using System;
namespace ForgottenVillage {
// A game is first to five rally points (no deuce). An earned Elder failure wins
// the game after the decisive ball lands, bypassing remaining rally points.
[Serializable] public sealed class TennisRules {
 public enum Phase { Idle, Countdown, Rally, GameOver, Won, Lost }
 public Phase phase; public int playerGames, elderGames, playerPoints, elderPoints;
 public int threshold, gameHits, rallyHits, smashes; public bool elderMustMiss;
 public int GameNumber => playerGames+elderGames+1;
 public void BeginMatch(){playerGames=elderGames=0;BeginGame();}
 public void BeginGame(){BeginGame(UnityEngine.Random.Range(20,31));}
 public void BeginGame(int target){threshold=UnityEngine.Mathf.Clamp(target,20,30);playerPoints=elderPoints=gameHits=rallyHits=smashes=0;elderMustMiss=false;phase=Phase.Countdown;}
 public void BeginRally(){if(phase==Phase.Won||phase==Phase.Lost)return;rallyHits=0;phase=Phase.Rally;}
 public bool Contact(bool player,bool smash){if(phase!=Phase.Rally)return false;rallyHits++;gameHits++;if(player&&smash)smashes++;elderMustMiss|=gameHits>=threshold||smashes>=2;return true;}
 public void Fault(bool playerWon){if(phase!=Phase.Rally)return;if(playerWon)playerPoints++;else elderPoints++;
  if((playerWon&&elderMustMiss)||playerPoints>=5||elderPoints>=5){if(playerWon)playerGames++;else elderGames++;phase=playerGames==2?Phase.Won:elderGames==2?Phase.Lost:Phase.GameOver;}
  else phase=Phase.Countdown;
 }
 public void ResetLoss(){if(phase!=Phase.Lost)return;playerGames=elderGames=playerPoints=elderPoints=gameHits=rallyHits=smashes=0;elderMustMiss=false;}
}
}
