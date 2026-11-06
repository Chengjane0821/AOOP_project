using System;
namespace ForgottenVillage {
public static class Engineering {
 class Controller { public virtual bool AllowsRun()=>true; }
 sealed class SafetyController : Controller { readonly bool unsafeCondition;public SafetyController(bool value){unsafeCondition=value;}public override bool AllowsRun()=>!unsafeCondition;}
 // C# has no implicit object slicing: explicitly copy the base portion to emulate C++ pass-by-value.
 public static bool ControllerAllowsRun(bool preserveDynamicType,bool unsafeCondition){Controller derived=new SafetyController(unsafeCondition);Controller evaluated=preserveDynamicType?derived:new Controller();return evaluated.AllowsRun();}
 public static double Parallel(double a,double b)=>a*b/(a+b);
 public static double Divider(double vin,double r1,double r2,double load,bool connected)=>vin*(connected?Parallel(r2,load):r2)/(r1+(connected?Parallel(r2,load):r2));
 public static double Cutoff(double r,double c)=>1/(2*Math.PI*r*c);
 public static double Magnitude(double f,double fc)=>1/Math.Sqrt(1+Math.Pow(f/fc,2));
 public static double Gain(double gm,double rd,double rl)=>-gm*Parallel(rd,rl);
 public static double Slack(double period,double setup,double uncertainty,double cq,double path)=>period-setup-uncertainty-cq-path;
 public static bool DividerSolved(double load)=>Math.Abs(Divider(10,10000,10000,load,true)-4)<0.08;
 public static bool FilterSolved(double r)=>Magnitude(100,Cutoff(r,1e-7))>=0.9 && Magnitude(5000,Cutoff(r,1e-7))<=0.1;
 public static bool GainSolved(double rd)=>Math.Abs(Gain(.002,rd,10000)+8)<.2;
}
}

