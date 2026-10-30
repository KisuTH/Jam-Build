using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class GhostChaseTuningEditor
{
    const string Work="C:/Users/Kanthawat/Documents/Codex/2026-10-29/xi/work/ghost-chase/";
    static GhostChaseTuningEditor(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(EditorApplication.isPlaying){if(File.Exists(Work+"stop.flag")){File.Delete(Work+"stop.flag");EditorApplication.isPlaying=false;}return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Work+"request.txt"))return;
        string request=File.ReadAllText(Work+"request.txt").Trim();File.Delete(Work+"request.txt");
        try {
            if(request=="inspect")Inspect();
            else if(request=="apply")Apply();
            else if(request=="play")EditorApplication.isPlaying=true;
            else throw new Exception("Unknown command");
            File.WriteAllText(Work+"status.txt","OK "+request);
        }catch(Exception ex){File.WriteAllText(Work+"status.txt",ex.ToString());Debug.LogException(ex);}
    }
    static void Inspect()
    {
        var scene=SceneManager.GetActiveScene();var b=new StringBuilder(scene.path+" dirty="+scene.isDirty+"\n");
        var chase=UnityEngine.Object.FindFirstObjectByType<GhostChaseEvent>();
        var player=GameObject.FindWithTag("Player");var move=player.GetComponent<PlayerMovement2D>();
        b.AppendLine("Player pos="+player.transform.position+" speed="+new SerializedObject(move).FindProperty("moveSpeed").floatValue);
        b.AppendLine("Ghost speed="+chase.ghostSpeed+" delay="+chase.spawnDelay+" gap="+chase.minSpawnGap+" first="+chase.firstSpawnPoint.position);
        foreach(var r in UnityEngine.Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None))b.AppendLine("Room "+PathOf(r.transform)+" bounds="+r.boundsMin+" -> "+r.boundsMax);
        foreach(var d in UnityEngine.Object.FindObjectsByType<RoomDoor>(FindObjectsInactive.Include,FindObjectsSortMode.None))b.AppendLine("Door "+PathOf(d.transform)+" pos="+d.transform.position+" -> "+(d.arriveRoom?PathOf(d.arriveRoom.transform):"NULL")+" at "+(d.arrivePoint?d.arrivePoint.position.ToString():"NULL"));
        foreach(var p in UnityEngine.Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(p.itemId=="red_folder")b.AppendLine("Folder "+PathOf(p.transform)+" pos="+p.transform.position);
        File.WriteAllText(Work+"inspect.txt",b.ToString());
    }
    static void Apply()
    {
        var scene=SceneManager.GetActiveScene();if(scene.name!="Game main")throw new Exception("Wrong scene");
        EditorSceneManager.SaveScene(scene,Work+"before/Game main-live.unity",true);
        var chase=UnityEngine.Object.FindFirstObjectByType<GhostChaseEvent>();
        Undo.RecordObject(chase,"Tune red folder chase");
        chase.ghostSpeed=11f;chase.spawnDelay=1.5f;chase.minSpawnGap=14f;
        EditorUtility.SetDirty(chase);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
}
