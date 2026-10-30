#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public sealed class GhostChaseTuningVerification : MonoBehaviour
{
    const string Work="C:/Users/Kanthawat/Documents/Codex/2026-10-29/xi/work/ghost-chase/";
    string mode,report;Keyboard keyboard;GhostChaseEvent chase;Transform player;Rigidbody2D body;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){if(!File.Exists(Work+"run.flag"))return;string mode=File.ReadAllText(Work+"run.flag").Trim();File.Delete(Work+"run.flag");new GameObject("Ghost chase verification (temporary)").AddComponent<GhostChaseTuningVerification>().mode=mode;}
    IEnumerator Start()
    {
        report=Work+mode+".txt";File.WriteAllText(report,"");keyboard=InputSystem.AddDevice<Keyboard>("GhostChaseKeyboard");
        yield return new WaitForSeconds(.5f);
        chase=FindFirstObjectByType<GhostChaseEvent>();player=GameObject.FindWithTag("Player").transform;body=player.GetComponent<Rigidbody2D>();
        var folder=FindObjectsByType<ItemPickup>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(p=>p.itemId=="red_folder");
        var records=Room.All.Single(r=>r.boundsMin.y< -100);
        var exit=FindObjectsByType<RoomDoor>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(d=>d.transform.IsChildOf(records.transform));
        SetPosition(new Vector2(folder.transform.position.x,exit.transform.position.y));
        Invoke(RoomTransition.Instance,"EnterRoom",records);
        yield return new WaitForSeconds(.3f);
        Check(!chase.ghost.activeSelf,"Ghost hidden before red folder");
        folder.Pickup();if(DialogueUI.Instance)DialogueUI.Instance.Hide();
        Check(StoryFlags.Instance.Has("got_red_folder"),"Actual red-folder pickup starts chase");
        if(mode=="before") {
            yield return Until(()=>chase.ghost.activeSelf,"Baseline first spawn",5);
            float gap=Mathf.Abs(player.position.x-chase.ghost.transform.position.x);
            Check(gap<chase.minSpawnGap,"REPRO first spawn ignores safe gap: "+gap.ToString("F2"));
            SetPosition(new Vector2(exit.transform.position.x,exit.transform.position.y));exit.Enter();
            yield return Until(()=>RoomTransition.Instance.CurrentRoom==exit.arriveRoom,"Baseline room reached",6);
            float timer=(float)Get(chase,"spawnTimer");
            yield return new WaitForSeconds(.25f);
            Check(RoomTransition.Instance.IsBusy&&(float)Get(chase,"spawnTimer")<timer-.15f,"REPRO delay consumed while door still locks player");
        } else {
            Check(chase.ghostSpeed==11&&chase.spawnDelay==1.5f&&chase.minSpawnGap==14,"Scene tuning: speed 11, delay 1.5, gap 14");
            Check((float)Get(player.GetComponent<PlayerMovement2D>(),"moveSpeed")==20,"Player speed remains 20");
            yield return new WaitForSeconds(chase.startShakeTime+chase.spawnDelay+.15f);
            Check(!chase.ghost.activeSelf&&!LoopManager.Instance.IsDying,"No close-range first spawn beside red folder");
            Hold(JamBuild.Options.NotebookOptions.SavedMoveRightKey);
            yield return Until(()=>chase.ghost.activeSelf,"Ghost appears once player has safe spacing",4);
            Check(Mathf.Abs(player.position.x-chase.ghost.transform.position.x)>=13.5f,"First spawn has at least 14 units of room (frame tolerance)");
            float initialGap=player.position.x-chase.ghost.transform.position.x;
            float gx=chase.ghost.transform.position.x,started=Time.time;
            yield return new WaitForSeconds(.4f);
            float measured=(chase.ghost.transform.position.x-gx)/(Time.time-started);
            Check(Mathf.Abs(measured-11f)<.6f,"Measured ghost speed "+measured.ToString("F2"));
            Check(player.position.x-chase.ghost.transform.position.x>initialGap+2f,"Walking player can widen the gap");
            Hold();yield return Capture("records-chase.png");
            yield return WalkTo(exit.transform.position.x);
            yield return ThroughDoor(exit,"records-to-lower-hall");
            var lowerHall=exit.arriveRoom;
            var next=FindObjectsByType<RoomDoor>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(d=>d.transform.IsChildOf(lowerHall.transform)&&d.arriveRoom.boundsMin.y> -60);
            yield return WalkTo(next.transform.position.x);
            yield return ThroughDoor(next,"lower-hall-to-middle-hall");
            var middleHall=next.arriveRoom;
            var finalDoor=FindObjectsByType<RoomDoor>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(d=>d.transform.IsChildOf(middleHall.transform)&&d.arriveRoom.boundsMin.y> -20);
            yield return WalkTo(finalDoor.transform.position.x);
            yield return ThroughDoor(finalDoor,"middle-hall-to-anon",true);
            Check(!chase.ghost.activeSelf&&(string)Get(chase,"state").ToString()=="AtAnon","Original chase ending at Anon's room preserved");
            Check(!LoopManager.Instance.IsDying,"Player survives route through all connecting rooms");
            yield return Capture("escaped-to-anon.png");
        }
        Log("COMPLETE "+mode);File.WriteAllText(Work+"stop.flag","stop");
    }
    IEnumerator ThroughDoor(RoomDoor door,string label,bool safeRoom=false)
    {
        Hold();door.Enter();
        yield return Until(()=>RoomTransition.Instance.CurrentRoom==door.arriveRoom,label+": arrived",8);
        yield return null;
        float timer=(float)Get(chase,"spawnTimer");
        while(RoomTransition.Instance.IsBusy) {
            CheckSilent(!LoopManager.Instance.IsDying,label+": no death during door");
            if(!safeRoom)CheckSilent(!chase.ghost.activeSelf&&Mathf.Abs((float)Get(chase,"spawnTimer")-timer)<.05f,label+": waiting timer frozen during door");
            yield return null;
        }
        Check(true,label+": transition safe; head start not consumed by fade or auto-walk");
        if(!safeRoom) {
            Check((float)Get(chase,"spawnTimer")>chase.spawnDelay-.1f,label+": full delay remains after door");
            yield return new WaitForSeconds(.8f);
            Check(!chase.ghost.activeSelf&&!LoopManager.Instance.IsDying,label+": player has time to choose next door");
        }
    }
    IEnumerator WalkTo(float x)
    {
        float start=Time.time;float last=player.position.x;float limit=Time.realtimeSinceStartup+12;
        while(Mathf.Abs(player.position.x-x)>2f&&Time.realtimeSinceStartup<limit) {
            Hold(player.position.x<x?JamBuild.Options.NotebookOptions.SavedMoveRightKey:JamBuild.Options.NotebookOptions.SavedMoveLeftKey);
            CheckSilent(!LoopManager.Instance.IsDying,"Survive walking to next door");
            yield return null;
        }
        Hold();Check(Mathf.Abs(player.position.x-x)<=2f,"Walked from "+last.ToString("F1")+" to door "+x.ToString("F1")+" in "+(Time.time-start).ToString("F2")+"s");
    }
    void SetPosition(Vector2 p){body.position=p;body.linearVelocity=Vector2.zero;player.position=new Vector3(p.x,p.y,0);Physics2D.SyncTransforms();}
    void Hold(params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
    static object Get(object o,string f)=>o.GetType().GetField(f,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    static void Invoke(object o,string name,object arg)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,new[]{arg});
    IEnumerator Until(Func<bool> condition,string text,float timeout=10){float end=Time.realtimeSinceStartup+timeout;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;Check(condition(),text);}
    IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Work+name,tex.EncodeToPNG());Destroy(tex);}
    void Check(bool ok,string text){Log((ok?"PASS ":"FAIL ")+text);CheckSilent(ok,text);}
    void CheckSilent(bool ok,string text){if(ok)return;Log("FAIL "+text);Hold();File.WriteAllText(Work+"stop.flag","stop");throw new Exception(text);}
    void Log(string text)=>File.AppendAllText(report,text+"\n");
    void OnDestroy(){if(keyboard!=null)InputSystem.RemoveDevice(keyboard);}
}
#endif
