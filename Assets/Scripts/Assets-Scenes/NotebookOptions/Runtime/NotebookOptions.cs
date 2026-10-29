using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace JamBuild.Options
{
    public sealed class NotebookOptions : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Page
        {
            public RectTransform left;
            public RectTransform right;
            public UnityEngine.UI.Button tab;
            public TextMeshProUGUI tabLabel;
        }
        [System.Serializable]
        public sealed class Choice
        {
            public UnityEngine.UI.Button button;
            public GameObject tick;
        }
        [System.Serializable]
        public sealed class LocalizedLabel
        {
            public TextMeshProUGUI label;
            public string thai;
            public string english;
        }

        [Header("Book")]
        [SerializeField] private Page[] pages;
        [SerializeField, Range(.2f,1.5f)] private float turnDuration = .65f;
        [SerializeField] private RectTransform turnShadow;
        [SerializeField] private CanvasGroup shadowOpacity;
        [SerializeField] private string mainMenuScene = "MaingameScene";
        [Header("Video")]
        [SerializeField] private UnityEngine.UI.Button resolutionButton;
        [SerializeField] private TextMeshProUGUI resolutionLabel;
        [SerializeField] private Choice[] windowModes;
        [SerializeField] private Choice[] qualityLevels;
        [Header("Sound")]
        [SerializeField] private UnityEngine.UI.Slider[] volumes;
        [SerializeField] private TextMeshProUGUI[] volumeLabels;
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private string musicParameter = "MusicVolume";
        [SerializeField] private string effectsParameter = "EffectsVolume";
        [SerializeField] private UnityEvent<float> musicVolumeChanged = new UnityEvent<float>();
        [SerializeField] private UnityEvent<float> effectsVolumeChanged = new UnityEvent<float>();
        [Header("Controls")]
        [SerializeField] private UnityEngine.UI.Button[] keyButtons;
        [SerializeField] private TextMeshProUGUI[] keyLabels;
        [Header("Accessibility")]
        [SerializeField] private Choice[] textSizes;
        [SerializeField] private Choice[] languages;
        [SerializeField] private LocalizedLabel[] localizedLabels;
        [SerializeField] private UnityEngine.UI.Button backButton;
        private int currentPage;
        private int requestedPage;
        private Coroutine turn;
        private bool initialized;
        private bool english;
        private bool prefsDirty;
        private int rebinding = -1;
        private int rebindStartFrame;
        private int rebindHandledFrame = -1;
        private Key leftKey=Key.A;
        private Key rightKey=Key.D;
        private Vector2Int[] resolutions;
        private int resolutionIndex;
        private Vector2Int displayedResolution;
        private FullScreenMode displayedMode;
        private Vector2Int requestedResolution;
        private FullScreenMode requestedMode;
        private bool displayPending;
        private float displayRequestedAt;
        private int displayRequestedFrame;
        private float nextDisplayCheck;
        private bool editorDisplayHintShown;
        private TextMeshProUGUI[] allText;
        private float[] baseFontSizes;
        private const string Prefix="NotebookOptions.";
        private static readonly string[] TabNames={"VIDEO","SOUND","CONTROL","ACCESSIBILITY","MENU"};
        private static readonly string[] ShortNames={"VI","SO","CO","AC","ME"};
        public int CurrentPage => currentPage;
        public bool IsTurning => turn != null;
        public event System.Action BackRequested;
        public bool IsHandlingRebindInput => rebinding >= 0 || rebindHandledFrame == Time.frameCount;
        public Key MoveLeftKey => leftKey;
        public Key MoveRightKey => rightKey;

        private void Awake()
        {
            if (pages == null || pages.Length != 5) { Debug.LogError("Notebook page references are incomplete.",this); enabled=false; return; }
            allText=GetComponentsInChildren<TextMeshProUGUI>(true);
            baseFontSizes=new float[allText.Length];
            for(int i=0;i<allText.Length;i++) baseFontSizes[i]=allText[i].fontSize;
            for(int i=0;i<pages.Length;i++) { int index=i; pages[i].tab.onClick.AddListener(()=>ShowPage(index)); }
            BindChoices(windowModes,SetWindowMode); BindChoices(qualityLevels,SetQuality);
            BindChoices(textSizes,SetTextSize); BindChoices(languages,SetLanguage);
            for(int i=0;i<volumes.Length;i++) { int index=i; volumes[i].onValueChanged.AddListener(v=>SetVolume(index,v)); }
            for(int i=0;i<keyButtons.Length;i++) { int index=i; keyButtons[i].onClick.AddListener(()=>BeginRebind(index)); }
            resolutionButton.onClick.AddListener(CycleResolution); backButton.onClick.AddListener(BackToMenu);
            RefreshDisplayFromScreen(false);
            SetTicks(qualityLevels,Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"Quality",2),0,2));
            english=PlayerPrefs.GetInt(Prefix+"Language",0)==1;
            RefreshLanguage();
            ApplyTextSize(Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"TextSize",1),0,2));
            leftKey=ReadKey("LeftKey",Key.A); rightKey=ReadKey("RightKey",Key.D); RefreshKeys();
            for(int i=0;i<volumes.Length;i++)
            {
                float value=Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix+"Volume"+i,i==0?.8f:.5f));
                volumes[i].SetValueWithoutNotify(value); ApplyVolume(i,value);
            }
            initialized=true; ShowImmediate(0);
        }

        // AudioMixer exposed parameters are applied after all Awake calls have completed.
        private void Start()
        {
            for(int i=1;i<volumes.Length;i++) ApplyVolume(i,volumes[i].value);
            if(PlayerPrefs.HasKey(Prefix+"Quality")) ApplyQuality(Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"Quality"),0,2));
        }
        private void OnEnable()
        {
            if(initialized) { ShowImmediate(currentPage); RefreshDisplayFromScreen(displayPending); }
        }
        private void OnDisable()
        {
            StopAllCoroutines(); turn=null; rebinding=-1;
            if(initialized) { ShowImmediate(currentPage); RefreshKeys(); }
            FlushPreferences();
        }
        private void OnApplicationFocus(bool focused) { if(!focused) FlushPreferences(); else nextDisplayCheck=0; }
        private void OnApplicationQuit() { FlushPreferences(); }

        private void Update()
        {
            if(initialized) UpdateDisplayState();
            if(rebinding<0 || Keyboard.current==null || Time.frameCount<=rebindStartFrame) return;
            foreach(var key in Keyboard.current.allKeys)
            {
                if(!key.wasPressedThisFrame) continue;
                rebindHandledFrame = Time.frameCount;
                if(key.keyCode==Key.Escape) { rebinding=-1; RefreshKeys(); return; }
                // Swapping avoids assigning both directions to the same key.
                if(rebinding==0) { if(key.keyCode==rightKey) rightKey=leftKey; leftKey=key.keyCode; }
                else { if(key.keyCode==leftKey) leftKey=rightKey; rightKey=key.keyCode; }
                SaveInt("LeftKey",(int)leftKey); SaveInt("RightKey",(int)rightKey);
                rebinding=-1; RefreshKeys(); return;
            }
        }

        public void ShowPage(int index)
        {
            if(!initialized || index<0 || index>=pages.Length || !isActiveAndEnabled) return;
            rebinding=-1; RefreshKeys(); requestedPage=index;
            if(turn==null && currentPage!=index) turn=StartCoroutine(TurnPages());
        }

        private IEnumerator TurnPages()
        {
            while(requestedPage!=currentPage)
            {
                int from=currentPage, to=requestedPage;
                bool forward=to>from;
                currentPage=to;
                UpdateTabs(to);
                foreach(var page in pages)
                {
                    SetInteractive(page.left,false); SetInteractive(page.right,false);
                }
                if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(null);
                RectTransform outgoing=forward?pages[from].right:pages[from].left;
                RectTransform incoming=forward?pages[to].left:pages[to].right;
                RectTransform underneath=forward?pages[to].right:pages[to].left;
                underneath.gameObject.SetActive(true); underneath.SetAsFirstSibling();
                outgoing.SetAsLastSibling();
                turnShadow.gameObject.SetActive(true);
                turnShadow.SetSiblingIndex(outgoing.GetSiblingIndex());
                float elapsed=0;
                while(elapsed<turnDuration)
                {
                    elapsed+=Time.unscaledDeltaTime;
                    float t=Mathf.Clamp01(elapsed/turnDuration);
                    float ease=t*t*(3f-2f*t);
                    bool first=ease<.5f;
                    outgoing.gameObject.SetActive(first);
                    incoming.gameObject.SetActive(!first);
                    RectTransform moving=first?outgoing:incoming;
                    if(!first) incoming.SetAsLastSibling();
                    float angle=first?ease*180f:(1f-ease)*180f;
                    float sign=forward?-1f:1f;
                    moving.localRotation=Quaternion.Euler(0,sign*(first?angle:-angle),0);
                    moving.GetComponent<NotebookPaper>().SetBend(Mathf.Sin(t*Mathf.PI));
                    turnShadow.anchoredPosition=new Vector2((first?(forward?1:-1):(forward?-1:1))*75f,0);
                    turnShadow.localScale=new Vector3(.3f+Mathf.Sin(t*Mathf.PI)*1.4f,1,1);
                    shadowOpacity.alpha=Mathf.Sin(t*Mathf.PI)*.16f;
                    yield return null;
                }
                ShowImmediate(to, false);
            }
            turn=null;
            if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(pages[currentPage].tab.gameObject);
        }

        private void ShowImmediate(int index,bool resetRequest=true)
        {
            currentPage=index; if(resetRequest) requestedPage=index;
            for(int i=0;i<pages.Length;i++)
            {
                ResetLeaf(pages[i].left,i==index); ResetLeaf(pages[i].right,i==index);
            }
            turnShadow.gameObject.SetActive(false); UpdateTabs(index);
        }
        private static void ResetLeaf(RectTransform leaf,bool visible)
        {
            leaf.localRotation=Quaternion.identity; leaf.GetComponent<NotebookPaper>().SetBend(0);
            leaf.gameObject.SetActive(visible); SetInteractive(leaf,visible);
        }
        private static void SetInteractive(RectTransform leaf,bool enabled)
        { var group=leaf.GetComponent<CanvasGroup>(); group.interactable=enabled; group.blocksRaycasts=enabled; }
        private void UpdateTabs(int selected)
        {
            for(int i=0;i<pages.Length;i++)
            {
                bool active=i==selected;
                var rect=(RectTransform)pages[i].tab.transform;
                rect.sizeDelta=new Vector2(active?(i==3?204f:148f):62f,46f);
                pages[i].tabLabel.text=active?TabNames[i]:ShortNames[i];
            }
        }

        public void BackToMenu()
        {
            FlushPreferences();
            if (BackRequested != null) BackRequested.Invoke();
            else SceneManager.LoadScene(mainMenuScene);
        }
        private static void BindChoices(Choice[] choices,UnityAction<int> action)
        { for(int i=0;i<choices.Length;i++) {int value=i; choices[i].button.onClick.AddListener(()=>action(value));} }
        private static void SetTicks(Choice[] choices,int selected)
        { for(int i=0;i<choices.Length;i++) choices[i].tick.SetActive(i==selected); }
        private void CycleResolution()
        {
            var mode=displayPending?requestedMode:Screen.fullScreenMode;
            var current=displayPending?requestedResolution:new Vector2Int(Screen.width,Screen.height);
            resolutions=NotebookDisplaySettings.AvailableResolutions(mode);
            resolutionIndex=System.Array.IndexOf(resolutions,current);
            resolutionIndex=(resolutionIndex+1)%resolutions.Length;
            RequestDisplay(resolutions[resolutionIndex],mode);
        }
        private void SetWindowMode(int value)
        {
            var size=displayPending?requestedResolution:new Vector2Int(Screen.width,Screen.height);
            var mode=NotebookDisplaySettings.ModeFromIndex(Mathf.Clamp(value,0,2));
            RequestDisplay(NotebookDisplaySettings.ClosestResolution(size,mode),mode);
        }
        private void RequestDisplay(Vector2Int size,FullScreenMode mode)
        {
            requestedResolution=size; requestedMode=mode; displayPending=true;
            displayRequestedAt=Time.unscaledTime; displayRequestedFrame=Time.frameCount; nextDisplayCheck=0;
            resolutionLabel.text=$"{size.x} x {size.y} ...";
            SetTicks(windowModes,NotebookDisplaySettings.ModeIndex(mode));
            // Persist the request too, so closing Pause/settings during the OS transition cannot lose it.
            NotebookDisplaySettings.Store(size,mode); prefsDirty=true; FlushPreferences();
            Screen.SetResolution(size.x,size.y,mode);
            if(Application.isEditor&&!editorDisplayHintShown)
            {
                editorDisplayHintShown=true;
                Debug.Log("Display settings: the Unity Editor can only preview the Game view. Test windowed, borderless and fullscreen in the Windows player.",this);
            }
        }
        private void UpdateDisplayState()
        {
            if(Time.unscaledTime<nextDisplayCheck) return;
            nextDisplayCheck=Time.unscaledTime+.2f;
            var actual=new Vector2Int(Screen.width,Screen.height);
            if(displayPending)
            {
                // SetResolution is asynchronous; preserve the latest requested mode across rapid clicks.
                if(Time.frameCount<displayRequestedFrame+2) return;
                bool applied=actual==requestedResolution&&Screen.fullScreenMode==requestedMode;
                if(!applied&&Time.unscaledTime-displayRequestedAt<3f) return;
                RefreshDisplayFromScreen(true);
            }
            else if(actual!=displayedResolution||Screen.fullScreenMode!=displayedMode)
                RefreshDisplayFromScreen(true);
        }
        private void RefreshDisplayFromScreen(bool save)
        {
            displayPending=false;
            displayedResolution=new Vector2Int(Screen.width,Screen.height);
            displayedMode=Screen.fullScreenMode;
            resolutions=NotebookDisplaySettings.AvailableResolutions(displayedMode);
            resolutionIndex=System.Array.IndexOf(resolutions,displayedResolution);
            resolutionLabel.text=$"{displayedResolution.x} x {displayedResolution.y}";
            SetTicks(windowModes,NotebookDisplaySettings.ModeIndex(displayedMode));
            if(save)
            {
                NotebookDisplaySettings.Store(displayedResolution,displayedMode);
                prefsDirty=true; FlushPreferences();
            }
        }
        private void SetQuality(int value)
        {
            ApplyQuality(value); SetTicks(qualityLevels,value); SaveInt("Quality",value);
        }
        private static void ApplyQuality(int value)
        {
            int count=QualitySettings.names.Length;
            if(count>0) QualitySettings.SetQualityLevel(Mathf.RoundToInt(value*.5f*(count-1)),true);
        }
        private void SetVolume(int index,float value)
        { PlayerPrefs.SetFloat(Prefix+"Volume"+index,value); prefsDirty=true; ApplyVolume(index,value); }
        private void ApplyVolume(int index,float value)
        {
            volumeLabels[index].text=Mathf.RoundToInt(value*100)+"%";
            if(index==0) AudioListener.volume=value;
            else
            {
                if(mixer!=null) mixer.SetFloat(index==1?musicParameter:effectsParameter,value<=.0001f?-80f:Mathf.Log10(value)*20f);
                if(index==1) musicVolumeChanged.Invoke(value); else effectsVolumeChanged.Invoke(value);
            }
        }
        private void BeginRebind(int index)
        { rebinding=index; rebindStartFrame=Time.frameCount; RefreshKeys(); keyLabels[index].text="..."; }
        private void RefreshKeys() { keyLabels[0].text=leftKey.ToString(); keyLabels[1].text=rightKey.ToString(); }
        private static Key ReadKey(string name,Key fallback)
        { int value=PlayerPrefs.GetInt(Prefix+name,(int)fallback); return System.Enum.IsDefined(typeof(Key),value)&&value!=(int)Key.None?(Key)value:fallback; }
        private void SetTextSize(int value) { SaveInt("TextSize",value); ApplyTextSize(value); }
        private void ApplyTextSize(int value)
        {
            float scale=value==0?.9f:value==1?1f:1.15f;
            for(int i=0;i<allText.Length;i++) allText[i].fontSize=baseFontSizes[i]*scale;
            SetTicks(textSizes,value);
        }
        private void SetLanguage(int value) { english=value==1; SaveInt("Language",value); RefreshLanguage(); }
        private void RefreshLanguage()
        { foreach(var item in localizedLabels) item.label.text=english?item.english:item.thai; SetTicks(languages,english?1:0); }
        private void SaveInt(string name,int value) {PlayerPrefs.SetInt(Prefix+name,value); prefsDirty=true;}
        private void FlushPreferences() {if(prefsDirty) {PlayerPrefs.Save(); prefsDirty=false;}}
    }
}
