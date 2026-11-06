using System; using System.Linq; using System.Collections.Generic; namespace ForgottenVillage {[Serializable] public class VillageSave {
 public int version=1; public bool elder, photograph, faultyTest, fixedTest, timingViolation, ended;
 public bool[] clues=new bool[5]; public bool[] evidence=new bool[4]; public List<string> inventory=new List<string>();
 public bool Unlocked => clues!=null && clues.Length==5 && clues.All(x=>x);
 public bool Authorized => elder && Unlocked && faultyTest && fixedTest && evidence.All(x=>x);
 public void Normalize(){ if(clues==null||clues.Length!=5) clues=new bool[5]; if(evidence==null||evidence.Length!=4)evidence=new bool[4]; if(inventory==null) inventory=new List<string>(); }
}
}
