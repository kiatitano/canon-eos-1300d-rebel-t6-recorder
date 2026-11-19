using System;
using RecorderStudio;
public class CameraPowerPolicyTests {
 static void Assert(bool ok,string why){if(!ok)throw new Exception(why);}
 public static int Main(){
  Assert(CameraPowerPolicy.Output(true,false,false)==0,"Idle saver must actually remove Live View output");
  Assert(CameraPowerPolicy.Output(true,true,false)==3,"Preview retains full output");
  Assert(CameraPowerPolicy.Output(true,false,true)==3,"Recording retains live movie mode even with preview off");
  Assert(CameraPowerPolicy.Output(false,false,false)==3,"Explicit always-awake mode retained");
  Assert(!CameraPowerPolicy.ShouldRest(true,false,false,false,false,false,59999),"No premature rest");
  Assert(CameraPowerPolicy.ShouldRest(true,false,false,false,false,false,60000),"Rest after idle timeout");
  Assert(!CameraPowerPolicy.ShouldRest(false,false,false,false,false,false,999999),"Saver disabled");
  Assert(!CameraPowerPolicy.ShouldRest(true,true,false,false,false,false,999999),"Never rest while recording");
  Assert(!CameraPowerPolicy.ShouldRest(true,false,true,false,false,false,999999),"Never rest while previewing");
  Assert(!CameraPowerPolicy.ShouldRest(true,false,false,true,false,false,999999),"Never rest while transferring");
  Assert(!CameraPowerPolicy.ShouldRest(true,false,false,false,true,false,999999),"Never rest while focusing");
  Assert(!CameraPowerPolicy.ShouldRest(true,false,false,false,false,true,999999),"Never rest while recovering a recording");
  Console.WriteLine("PASS idle timeout and active recording/preview/transfer/focus/recovery exclusions; output retained throughout recording");return 0;
 }
}
