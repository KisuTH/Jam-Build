using System.Collections.Generic;
using System.Text;
using JamBuild.Options;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;

namespace JamBuild.Options.Editor
{
    public static class NotebookOptionsBuilder
    {
        public const string RootPath="Assets/UI/NotebookOptions";
        private static readonly Color Ink=new Color32(36,36,29,255);
        private static readonly Color Paper=new Color32(183,178,142,255);
        private static readonly Color Red=new Color32(168,39,32,255);
        private static readonly Color[] TabColors={new Color32(193,181,47,255),new Color32(66,157,170,255),new Color32(65,153,63,255),new Color32(156,77,169,255),new Color32(181,66,63,255)};
        private static TMP_FontAsset font;
        private static readonly List<NotebookOptions.LocalizedLabel> Labels=new List<NotebookOptions.LocalizedLabel>();

        [MenuItem("Tools/Notebook Options/Create in SettingScene")]
        public static void Install()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/SettingScene.unity" || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Open SettingScene in Edit Mode before creating the notebook.");
            if(Object.FindAnyObjectByType<NotebookOptions>(FindObjectsInactive.Include)!=null)
                throw new System.InvalidOperationException("NotebookOptions already exists. Edit the existing hierarchy.");
            font=CreateFont(); Labels.Clear();
            int undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Create notebook options");
            // Keep the original menu intact and recoverable in the scene.
            foreach(var root in scene.GetRootGameObjects())
                if(root.name=="Canvas" && root.GetComponent<Canvas>()!=null)
                { Undo.RecordObject(root,"Preserve previous menu"); root.SetActive(false); }

            var go=new GameObject("NotebookOptions",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(go,"Create notebook options");
            var canvas=go.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=10;
            var scaler=go.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080); scaler.screenMatchMode=UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            var controller=go.AddComponent<NotebookOptions>(); var so=new SerializedObject(controller);
            var backdrop=Rect(go.transform,"Backdrop",0,0,0,0); Stretch(backdrop);
            var backdropImage=backdrop.gameObject.AddComponent<UnityEngine.UI.Image>(); backdropImage.color=new Color32(153,155,153,255); backdropImage.raycastTarget=true;
            var book=Rect(go.transform,"Book",0,0,1120,680); book.anchorMin=book.anchorMax=new Vector2(.5f,.5f); book.pivot=new Vector2(.5f,.5f); book.anchoredPosition=new Vector2(55,0);
            book.localScale=Vector3.one*1.18f;
            Shape(book,"BookShadow",-9,12,1142,694,new Color(0,0,0,.13f),NotebookPaper.Shape.Rounded,20);
            Shape(book,"Cover",-8,-8,1136,696,new Color32(130,125,96,255),NotebookPaper.Shape.Rounded,18);
            for(int i=3;i>=0;i--) Shape(book,"PaperEdge"+i,-2+i*2,2+i*3,1124-i*4,674,new Color32((byte)(155+i*4),(byte)(151+i*4),(byte)(120+i*3),255),NotebookPaper.Shape.Rounded,15);
            var leaves=Rect(book,"Pages",0,0,1120,680);
            var tabs=Rect(book,"Tabs",0,0,1120,680); tabs.SetAsFirstSibling();
            var pageArray=so.FindProperty("pages"); pageArray.arraySize=5;
            string[] names={"VIDEO","SOUND","CONTROL","ACCESSIBILITY","MENU"};
            var lefts=new RectTransform[5]; var rights=new RectTransform[5];
            for(int i=0;i<5;i++)
            {
                lefts[i]=Leaf(leaves,names[i]+"_Left",false); rights[i]=Leaf(leaves,names[i]+"_Right",true);
                Text(lefts[i],"Heading",names[i],60,52,460,48,29);
                Text(lefts[i],"PageNumber",(i*2+1).ToString("00"),54,613,50,30,17).color=new Color(Ink.r,Ink.g,Ink.b,.5f);
                Text(rights[i],"PageNumber",(i*2+2).ToString("00"),455,613,50,30,17).alignment=TextAlignmentOptions.Right;
                var tab=Button(tabs,"Tab_"+names[i],0,110+i*99,62,46,TabColors[i]);
                var tr=(RectTransform)tab.transform; tr.pivot=new Vector2(1,.5f); tr.anchoredPosition=new Vector2(8,-133-i*99);
                var label=Text(tr,"Label",i==0?"VIDEO":names[i].Substring(0,2),8,4,46,38,18);
                Stretch(label.rectTransform,8,4,12,4); label.alignment=TextAlignmentOptions.Center;
                var entry=pageArray.GetArrayElementAtIndex(i);
                Ref(entry,"left",lefts[i]); Ref(entry,"right",rights[i]); Ref(entry,"tab",tab); Ref(entry,"tabLabel",label);
                lefts[i].gameObject.SetActive(i==0); rights[i].gameObject.SetActive(i==0);
            }
            var first=(RectTransform)((Component)pageArray.GetArrayElementAtIndex(0).FindPropertyRelative("tab").objectReferenceValue).transform;
            first.sizeDelta=new Vector2(148,46);

            Local(lefts[0],"Resolution","ความละเอียด","RESOLUTION",154);
            Local(lefts[0],"WindowMode","โหมดหน้าจอ","WINDOW MODE",277);
            Local(lefts[0],"Quality","คุณภาพกราฟิก","GRAPHICS QUALITY",421);
            var resolution=Button(rights[0],"Resolution",70,150,420,45,Ink);
            Shape(resolution.transform,"Inner",2,2,416,41,Paper,NotebookPaper.Shape.Rounded,2);
            var resolutionText=Text(resolution.transform,"Value","1920 x 1080",16,2,360,41,23); resolutionText.alignment=TextAlignmentOptions.Center;
            Shape(resolution.transform,"Next",383,16,14,12,Ink,NotebookPaper.Shape.Triangle);
            Ref(so,"resolutionButton",resolution); Ref(so,"resolutionLabel",resolutionText);
            Choices(so,"windowModes",rights[0],new[]{"หน้าต่าง","ไร้ขอบ","เต็มหน้าจอ"},new[]{"WINDOW","BORDERLESS","FULLSCREEN"},240,2);
            Choices(so,"qualityLevels",rights[0],new[]{"ต่ำ","กลาง","สูง"},new[]{"LOW","MEDIUM","HIGH"},384,2);

            var volumeArray=so.FindProperty("volumes"); volumeArray.arraySize=3;
            var valueArray=so.FindProperty("volumeLabels"); valueArray.arraySize=3;
            string[] soundNames={"MAIN","MUSIC","EFFECT"};
            for(int i=0;i<3;i++)
            {
                float y=157+i*120;
                Text(lefts[1],soundNames[i],soundNames[i],60,y,440,46,25);
                var slider=Slider(rights[1],soundNames[i],66,y,420,48);
                volumeArray.GetArrayElementAtIndex(i).objectReferenceValue=slider;
                var value=Text(rights[1],soundNames[i]+"Value",i==0?"80%":"50%",370,y+44,116,32,17); value.alignment=TextAlignmentOptions.Right;
                valueArray.GetArrayElementAtIndex(i).objectReferenceValue=value;
            }
            var keyArray=so.FindProperty("keyButtons"); keyArray.arraySize=2;
            var keyTexts=so.FindProperty("keyLabels"); keyTexts.arraySize=2;
            for(int i=0;i<2;i++)
            {
                float y=155+i*126;
                Text(lefts[2],"Direction"+i,i==0?"BUTTON LEFT":"BUTTON RIGHT",60,y,440,48,25);
                var key=Button(rights[2],"Key"+i,251,y,66,58,Ink);
                Shape(key.transform,"Inner",3,3,60,52,Paper,NotebookPaper.Shape.Rounded,2);
                var text=Text(key.transform,"Value",i==0?"A":"D",3,3,60,52,26); text.alignment=TextAlignmentOptions.Center;
                keyArray.GetArrayElementAtIndex(i).objectReferenceValue=key; keyTexts.GetArrayElementAtIndex(i).objectReferenceValue=text;
            }
            Local(rights[2],"RebindHint","คลิกปุ่มเพื่อเปลี่ยน • Esc ยกเลิก","CLICK A KEY TO CHANGE • ESC TO CANCEL",470,24,512,18);
            Text(lefts[3],"TextSize","TEXT SIZE",60,163,440,48,25);
            Text(lefts[3],"Language","LANGUAGE",60,307,440,48,25);
            Choices(so,"textSizes",rights[3],new[]{"เล็ก","กลาง","ใหญ่"},new[]{"SMALL","MEDIUM","LARGE"},126,1);
            Choices(so,"languages",rights[3],new[]{"ไทย","ENGLISH"},new[]{"THAI","ENGLISH"},270,0);
            var back=Button(lefts[4],"BackToMenu",60,182,300,66,new Color(0,0,0,0));
            var backText=Text(back.transform,"Label","BACK TO MENU",8,8,284,48,27); backText.color=Red;
            Shape(back.transform,"RedUnderline",6,49,282,4,Red,NotebookPaper.Shape.Rounded,1).localRotation=Quaternion.Euler(0,0,3);
            Ref(so,"backButton",back);

            var shadow=Shape(leaves,"TurningShadow",0,0,180,670,Color.black,NotebookPaper.Shape.Shadow,14);
            shadow.anchorMin=shadow.anchorMax=new Vector2(.5f,.5f); shadow.pivot=new Vector2(.5f,.5f); shadow.anchoredPosition=Vector2.zero;
            var opacity=shadow.gameObject.AddComponent<CanvasGroup>(); opacity.alpha=0; opacity.blocksRaycasts=false;
            Ref(so,"turnShadow",shadow); Ref(so,"shadowOpacity",opacity); shadow.gameObject.SetActive(false);
            var localized=so.FindProperty("localizedLabels"); localized.arraySize=Labels.Count;
            for(int i=0;i<Labels.Count;i++)
            {
                var item=localized.GetArrayElementAtIndex(i); Ref(item,"label",Labels[i].label);
                item.FindPropertyRelative("thai").stringValue=Labels[i].thai; item.FindPropertyRelative("english").stringValue=Labels[i].english;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            if(Object.FindAnyObjectByType<EventSystem>()==null)
            {
                var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(events,"Create EventSystem"); events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            NotebookBookArtwork.Apply(go);
            PrefabUtility.SaveAsPrefabAsset(go,RootPath+"/NotebookOptions.prefab");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets(); Selection.activeGameObject=go; Undo.CollapseUndoOperations(undo);
            Debug.Log("Notebook options installed in SettingScene. Original Canvas is preserved inactive.");
        }

        private static TMP_FontAsset CreateFont()
        {
            string path=RootPath+"/Fonts/NotebookThai SDF.asset";
            var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path); if(existing!=null) return existing;
            var source=AssetDatabase.LoadAssetAtPath<Font>(RootPath+"/Fonts/NotoSansThai.ttf");
            if(source==null) throw new System.InvalidOperationException("Noto Sans Thai source font is missing.");
            var asset=TMP_FontAsset.CreateFontAsset(source,64,8,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
            asset.name="NotebookThai SDF";
            var characters=new StringBuilder(); for(int i=32;i<127;i++)characters.Append((char)i);
            for(int i=0xE01;i<=0xE5B;i++)characters.Append((char)i); characters.Append('•');
            asset.TryAddCharacters(characters.ToString());
            asset.fallbackFontAssetTable=new List<TMP_FontAsset>{TMP_Settings.defaultFontAsset};
            AssetDatabase.CreateAsset(asset,path);
            foreach(var atlas in asset.atlasTextures) {atlas.name="NotebookThai Atlas"; AssetDatabase.AddObjectToAsset(atlas,asset);}
            asset.material.name="NotebookThai Material"; AssetDatabase.AddObjectToAsset(asset.material,asset);
            EditorUtility.SetDirty(asset); AssetDatabase.SaveAssets(); return asset;
        }
        private static RectTransform Leaf(Transform parent,string name,bool right)
        {
            var leaf=Rect(parent,name,0,0,560,680); leaf.anchorMin=leaf.anchorMax=new Vector2(.5f,.5f);
            leaf.pivot=new Vector2(right?0:1,.5f); leaf.anchoredPosition=Vector2.zero;
            leaf.gameObject.AddComponent<NotebookPaper>().Configure(NotebookPaper.Shape.Paper,Paper,right);
            leaf.gameObject.AddComponent<CanvasGroup>(); return leaf;
        }
        private static void Choices(SerializedObject so,string field,Transform parent,string[] thai,string[] english,float y,int selected)
        {
            var array=so.FindProperty(field); array.arraySize=thai.Length;
            float spacing=thai.Length==2?185:155;
            for(int i=0;i<thai.Length;i++)
            {
                var button=Button(parent,field+"_"+i,42+i*spacing,y,142,99,new Color(0,0,0,0));
                var label=Text(button.transform,"Label",thai[i],-4,0,150,36,18); label.alignment=TextAlignmentOptions.Center;
                Labels.Add(new NotebookOptions.LocalizedLabel{label=label,thai=thai[i],english=english[i]});
                Shape(button.transform,"Box",57,48,28,28,Ink,NotebookPaper.Shape.Rounded,3);
                Shape(button.transform,"BoxInner",60,51,22,22,Paper,NotebookPaper.Shape.Rounded,1);
                var tick=Shape(button.transform,"SelectedTick",53,31,46,47,Red,NotebookPaper.Shape.Tick); tick.gameObject.SetActive(i==selected);
                var entry=array.GetArrayElementAtIndex(i); Ref(entry,"button",button); Ref(entry,"tick",tick.gameObject);
            }
        }
        private static UnityEngine.UI.Slider Slider(Transform parent,string name,float x,float y,float width,float height)
        {
            var rect=Rect(parent,name,x,y,width,height);
            var hit=rect.gameObject.AddComponent<UnityEngine.UI.Image>(); hit.color=new Color(0,0,0,0);
            Shape(rect,"Track",0,19,width,10,Ink,NotebookPaper.Shape.Rounded,5);
            var area=Rect(rect,"HandleArea",10,0,width-20,height);
            var handle=Shape(area,"Handle",0,12,23,28,new Color32(224,219,191,255),NotebookPaper.Shape.Triangle);
            handle.pivot=new Vector2(.5f,.5f);
            var slider=rect.gameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.handleRect=handle; slider.targetGraphic=handle.GetComponent<NotebookPaper>(); slider.minValue=0; slider.maxValue=1; slider.value=.8f;
            handle.sizeDelta=new Vector2(23,28-height); handle.anchoredPosition=Vector2.zero;
            var colors=slider.colors; colors.highlightedColor=new Color(1,.87f,.66f); colors.selectedColor=colors.highlightedColor; slider.colors=colors;
            return slider;
        }
        private static UnityEngine.UI.Button Button(Transform parent,string name,float x,float y,float width,float height,Color color)
        {
            var rect=Rect(parent,name,x,y,width,height); var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color=color;
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic=image;
            var colors=button.colors; colors.highlightedColor=new Color(1.15f,1.15f,1.15f,1); colors.pressedColor=new Color(.8f,.8f,.8f,1); colors.selectedColor=new Color(1.1f,1.1f,1.1f,1); button.colors=colors;
            return button;
        }
        private static void Local(Transform parent,string name,string thai,string english,float y,float x=60,float width=440,float size=25)
        { var label=Text(parent,name,thai,x,y,width,52,size); Labels.Add(new NotebookOptions.LocalizedLabel{label=label,thai=thai,english=english}); }
        private static TextMeshProUGUI Text(Transform parent,string name,string value,float x,float y,float width,float height,float size)
        {
            var rect=Rect(parent,name,x,y,width,height); var text=rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font=font; text.text=value; text.fontSize=size; text.color=Ink; text.raycastTarget=false;
            text.alignment=TextAlignmentOptions.MidlineLeft; text.textWrappingMode=TextWrappingModes.NoWrap; text.overflowMode=TextOverflowModes.Overflow;
            return text;
        }
        private static RectTransform Shape(Transform parent,string name,float x,float y,float w,float h,Color color,NotebookPaper.Shape shape,float radius=12)
        {var rect=Rect(parent,name,x,y,w,h); rect.gameObject.AddComponent<NotebookPaper>().Configure(shape,color,false,radius); return rect;}
        private static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform)); go.layer=5; var rect=go.GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(0,1); rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(w,h); return rect;
        }
        private static void Stretch(RectTransform rect,float left=0,float top=0,float right=0,float bottom=0)
        {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(left,bottom);rect.offsetMax=new Vector2(-right,-top);}
        private static void Ref(SerializedObject so,string field,Object value) => so.FindProperty(field).objectReferenceValue=value;
        private static void Ref(SerializedProperty property,string field,Object value) => property.FindPropertyRelative(field).objectReferenceValue=value;
    }
}
