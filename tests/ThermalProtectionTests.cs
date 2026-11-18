using System;
using System.Collections.Generic;
using RecorderStudio;
public static class ThermalProtectionTests {
 static void Assert(bool condition,string label) {if(!condition) throw new Exception(label);}
 public static int Main() {
  var policy=new ThermalProtection();
  Assert(!policy.Available && !policy.CoolingLock && policy.Summary=="Unavailable","Fresh state must not claim normal temperature");
  policy.Update(0); Assert(policy.Available && !policy.CoolingLock,"No-warning sample permits normal workflow");
  policy.Update(null); Assert(!policy.Available && !policy.CoolingLock,"Lost telemetry must be unknown, never a normal reading");
  policy.Update(uint.MaxValue); Assert(!policy.Available && !policy.CoolingLock,"Unknown sentinel is unavailable");
  foreach(uint warning in new uint[]{1,2,3,4,100}) {
   var guard=new ThermalProtection();guard.Update(warning);Assert(guard.CoolingLock,"Nonzero warning must latch");
   guard.Update(0);Assert(guard.CoolingLock,"A later normal sample must not auto-unlock");
   guard.Update(null);Assert(guard.CoolingLock,"Lost telemetry must not auto-unlock");
  }
  var calls=new List<uint>();
  uint? result=ThermalProtection.Probe(delegate(uint property){calls.Add(property);return property==ThermalProtection.TemperatureProperty?(uint?)0:null;});
  Assert(result==0 && calls.Count==1,"A supported modern property should not fall through");calls.Clear();
  result=ThermalProtection.Probe(delegate(uint property){calls.Add(property);if(property==ThermalProtection.TemperatureProperty)throw new Exception("unsupported");return (uint?)1;});
  Assert(result==1 && calls.Count==2 && calls[1]==ThermalProtection.LegacyTemperatureProperty,"Legacy fallback after unsupported modern property");
  result=ThermalProtection.Probe(delegate(uint property){throw new Exception("unsupported");});Assert(result==null,"Both unsupported paths must remain unavailable");
  Console.WriteLine("PASS thermal unknown/normal/warning states, latched cooling lock, future warnings and unsupported-property fallback");return 0;
 }
}
