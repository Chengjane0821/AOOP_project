namespace ForgottenVillage {public static class SerialProtocol {
 public static bool Parse(string line,out string kind,out int value){kind="";value=0;if(line=="ACK:GATE:OPEN"||line=="ACK:GATE:CLOSE"){kind=line;return true;}if(line=="BTN:0"||line=="BTN:1"){kind="BTN";value=line[4]-'0';return true;}if(line!=null&&line.StartsWith("POT:")&&int.TryParse(line.Substring(4),out value)&&value>=0&&value<=1023){kind="POT";return true;}return false;}
}
}
