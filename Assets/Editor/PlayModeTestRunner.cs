using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
[InitializeOnLoad] internal static class PlayModeTestRunner
{
 static int frames;
 static PlayModeTestRunner() {
 var state=SessionState.GetString("PlayModeTest.State", "Idle");
 if(state=="WaitingForCompile") EditorApplication.delayCall+=()=>{SessionState.SetString("PlayModeTest.State","EnteringPlayMode"); EditorApplication.isPlaying=true;};
 if(state=="EnteringPlayMode" || state=="InPlayMode") EditorApplication.update+=Tick;
 if(state=="Done") EditorApplication.delayCall+=()=>{AssetDatabase.DeleteAsset("Assets/Editor/PlayModeTestRunner.cs"); SessionState.EraseString("PlayModeTest.State");};
 }
 static void Tick(){ if(!EditorApplication.isPlaying || ++frames<10)return; EditorApplication.update-=Tick;
 try {
 Component ai=null; foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) if(root.name=="SirenHeadEnemy") ai=root.GetComponent("SirenHeadAI");
 if(ai==null)throw new Exception("Enemy missing"); var type=ai.GetType(); var flags=BindingFlags.Instance|BindingFlags.NonPublic;
 var source=(AudioSource)type.GetField("_chaseAudio",flags).GetValue(ai); var steps=(AudioSource)type.GetField("_footstepAudio",flags).GetValue(ai);
 var enter=type.GetMethod("EnterState",flags); var enumType=type.GetNestedType("EnemyState");
 enter.Invoke(ai,new object[]{Enum.Parse(enumType,"Chase")}); if(!source.isPlaying)throw new Exception("Chase did not play");
 foreach(var s in new[]{"Roam","Investigate","Search","Capture"}) {enter.Invoke(ai,new object[]{Enum.Parse(enumType,s)}); if(source.isPlaying)throw new Exception("Music did not stop in "+s);}
 if(steps.spatialBlend!=1 || steps.minDistance!=5 || steps.maxDistance!=60)throw new Exception("Spatial settings incorrect");
 steps.PlayOneShot(((AudioClip[])type.GetField("_footstepClips",flags).GetValue(ai))[0]); if(!steps.isPlaying)throw new Exception("Footstep playback failed");
 SessionState.SetString("PlayModeTest.Result","PASS: Chase playback, stop in all non-chase states, footstep playback and 5–60 unit spatial falloff.");
 }catch(Exception ex){SessionState.SetString("PlayModeTest.Result","FAIL: "+ex);}
 SessionState.SetString("PlayModeTest.State","Done"); EditorApplication.isPlaying=false;
 }
}
