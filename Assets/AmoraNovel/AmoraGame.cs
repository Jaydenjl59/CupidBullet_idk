using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ink.Runtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Video;

namespace AmoraNovel {
public class AmoraGame : MonoBehaviour {
 public TextAsset ink;
 public Texture2D neutral, blush, concerned, angry, cupid, festival, cafe, heart, startArt;
 public AudioClip universityMusic, cafeMusic, battleMusic, titleMusic, shotSound;
 public Story Story {get; private set;}
 public string Mode {get; private set;} = "title";
 public string PendingBattle {get; private set;}
 public AmoraBattle Battle {get; private set;}
 Canvas canvas; RectTransform stage, controls, overlay, cafeSet; RawImage backdrop, portrait, flash;
 Text dialogue, speaker, location, hint, titleClickPrompt; CanvasGroup portraitGroup, dialogueGroup, titleGroup, titleClickPromptGroup;
 AudioSource music, sfx; Font font;
 string fullLine="", playerName="MC", background="courtyard", expression="neutral", musicKey="university_theme";
 float revealed, lineAge, portraitVisibility, desiredVisibility, flashAlpha;
 bool paused, historyOpen, thought, festivalUnlocked, titleSequenceActive;
 Coroutine titleIntroRoutine, titlePromptRoutine;
 readonly List<string> history=new List<string>();
 readonly List<GameObject> choiceObjects=new List<GameObject>();
 InputField nameInput;
 Color inkColor=new Color(.22f,.09f,.19f), rose=new Color(.65f,.17f,.36f);
 public static AmoraGame Instance;
 RawImage titleVideoImage;
 VideoPlayer titleVideoPlayer;
 RenderTexture titleVideoTexture;
 List<string> titleVideoPaths = new List<string>();
 int titleClipIndex = -1;
 bool waitingForTitleAdvance;
 
 void Awake(){ Instance=this; font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); BuildUI(); music=gameObject.AddComponent<AudioSource>(); music.loop=true; sfx=gameObject.AddComponent<AudioSource>(); BuildTitleVideo(); StartTitleSequence(); }
 RectTransform Rect(string n, Transform parent,float x,float y,float w,float h){var g=new GameObject(n,typeof(RectTransform)); g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(w,h);r.anchoredPosition=new Vector2(x,y);return r;}
 public RectTransform Panel(string n,Transform parent,float x,float y,float w,float h,Color color){var r=Rect(n,parent,x,y,w,h);r.gameObject.AddComponent<Image>().color=color;return r;}
 public Text Label(string n,Transform parent,string value,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.MiddleCenter){var r=Rect(n,parent,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color;t.alignment=align;t.supportRichText=false;t.raycastTarget=false;return t;}
 public UnityEngine.UI.Button CreateButton(Transform parent,string text,float x,float y,float w,float h,Action action){var r=Panel(text,parent,x,y,w,h,new Color(1,.88f,.93f));var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();var c=b.colors;c.highlightedColor=new Color(1,.70f,.81f);c.pressedColor=new Color(.85f,.42f,.60f);b.colors=c;Label("Label",r,text,0,0,w-24,h-8,22,inkColor);b.onClick.AddListener(()=>action());return b;}
 RawImage Art(string n,Transform parent,Texture tex,float x,float y,float w,float h){var r=Rect(n,parent,x,y,w,h);var a=r.gameObject.AddComponent<RawImage>();a.texture=tex;a.raycastTarget=false;return a;}
 void BuildUI(){var c=new GameObject("Amora • Screen",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas=c.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;var scaler=c.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;c.transform.SetParent(transform,false);stage=Rect("Stage",c.transform,0,0,1280,720);stage.gameObject.AddComponent<RectMask2D>();
 backdrop=Art("Location",stage,festival,0,0,1280,720);BuildCafe();Panel("Atmosphere",stage,0,0,1280,720,new Color(.20f,.04f,.16f,.13f));
 portrait=Art("Amora",stage,neutral,220,-185,420,934);portraitGroup=portrait.gameObject.AddComponent<CanvasGroup>();
 location=Label("Location name",stage,"ARROWWOOD UNIVERSITY",-390,322,450,30,17,Color.white,TextAnchor.MiddleLeft);
 var dg=Panel("Dialogue",stage,0,-235,1160,204,new Color(1,.87f,.92f,.98f));dialogueGroup=dg.gameObject.AddComponent<CanvasGroup>();Panel("Accent",dg,0,100,1160,4,rose);
 var tab=Panel("Speaker tab",dg,-430,125,250,48,rose);speaker=Label("Speaker",tab,"",0,0,230,44,22,Color.white);
 dialogue=Label("Dialogue text",dg,"",0,12,1084,133,25,inkColor,TextAnchor.UpperLeft);
 hint=Label("Continue prompt",dg,"Click / Space to continue  ▸",360,-79,360,25,15,rose,TextAnchor.MiddleRight);
 var advance=dg.gameObject.AddComponent<UnityEngine.UI.Button>();advance.transition=Selectable.Transition.None;advance.onClick.AddListener(Advance);
 controls=Rect("Controls",stage,0,0,1280,720);titleGroup=controls.gameObject.AddComponent<CanvasGroup>();
 flash=Art("Pink flash",stage,Texture2D.whiteTexture,0,0,1600,1000);flash.color=Color.clear;
 overlay=Rect("Overlay",stage,0,0,1280,720);
 if(FindAnyObjectByType<EventSystem>()==null){var e=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));e.transform.SetParent(transform);e.GetComponent<EventSystem>().sendNavigationEvents=false;}
 }

 void BuildTitleVideo(){
     var go=new GameObject("Title Video", typeof(RawImage), typeof(VideoPlayer));
     go.transform.SetParent(stage, false);
     var rect=(RectTransform)go.transform;
     rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
     rect.sizeDelta=new Vector2(1280,720);
     rect.anchoredPosition=Vector2.zero;
     titleVideoImage = go.GetComponent<RawImage>();
     titleVideoImage.color = Color.white;
     titleVideoImage.raycastTarget = true;
     titleVideoImage.gameObject.AddComponent<UnityEngine.UI.Button>().onClick.AddListener(AdvanceTitleSequence);
     titleVideoTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
     titleVideoTexture.Create();
     titleVideoPlayer = go.GetComponent<VideoPlayer>();
     titleVideoPlayer.playOnAwake = false;
     titleVideoPlayer.skipOnDrop = false;
     titleVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
     titleVideoPlayer.targetTexture = titleVideoTexture;
     titleVideoPlayer.aspectRatio = VideoAspectRatio.FitInside;
     titleVideoPlayer.isLooping = false;
     titleVideoPlayer.EnableAudioTrack(0, false);
     titleVideoPlayer.loopPointReached += HandleTitleVideoEnded;
     titleVideoImage.texture = titleVideoTexture;
     var prompt=Rect("Click Prompt", stage, 0, -285, 180, 42);
     titleClickPrompt = prompt.gameObject.AddComponent<Text>();
     titleClickPrompt.font = font;
     titleClickPrompt.text = "CLICK";
     titleClickPrompt.fontSize = 22;
     titleClickPrompt.alignment = TextAnchor.MiddleCenter;
     titleClickPrompt.color = new Color(1f, 0.9f, 0.94f, 0f);
     titleClickPrompt.raycastTarget = false;
     titleClickPromptGroup = prompt.gameObject.AddComponent<CanvasGroup>();
     titleClickPromptGroup.alpha = 0f;
     go.SetActive(false);
 }

 void StartTitleSequence(){
     titleSequenceActive = true;
     controls.gameObject.SetActive(false);
     overlay.gameObject.SetActive(false);
     titleClickPromptGroup.alpha = 0f;
     titleVideoPaths.Clear();
     titleVideoPaths.Add(System.IO.Path.Combine(Application.dataPath, "Scenes", "Title Screen", "scene1cut.mp4"));
     titleVideoPaths.Add(System.IO.Path.Combine(Application.dataPath, "Scenes", "Title Screen", "scene2cut.mp4"));
     titleVideoPaths.Add(System.IO.Path.Combine(Application.dataPath, "Scenes", "Title Screen", "scene3cut.mp4"));
     titleClipIndex = -1;
     waitingForTitleAdvance = false;
     titleVideoImage.gameObject.SetActive(true);
     titleVideoPlayer.Stop();
     AdvanceTitleSequence();
 }

 void HandleTitleVideoEnded(VideoPlayer vp){
     waitingForTitleAdvance = true;
 }

 void AdvanceTitleSequence(){
     if (titleVideoImage == null || !titleSequenceActive) return;

     if (titleClipIndex >= titleVideoPaths.Count - 1) {
         titleSequenceActive = false;
         titleVideoPlayer.Stop();
         titleVideoImage.gameObject.SetActive(false);
         titleClickPromptGroup.alpha = 0f;
         ShowTitle();
         return;
     }

     titleClipIndex = (titleClipIndex < 0) ? 0 : titleClipIndex + 1;
     if (titleClipIndex >= titleVideoPaths.Count) {
         titleSequenceActive = false;
         titleVideoPlayer.Stop();
         titleVideoImage.gameObject.SetActive(false);
         titleClickPromptGroup.alpha = 0f;
         ShowTitle();
         return;
     }

     string path = titleVideoPaths[titleClipIndex];
     if (string.IsNullOrEmpty(path) || !File.Exists(path)) {
         titleSequenceActive = false;
         titleVideoPlayer.Stop();
         titleVideoImage.gameObject.SetActive(false);
         titleClickPromptGroup.alpha = 0f;
         ShowTitle();
         return;
     }

    waitingForTitleAdvance = false;
    titleClickPromptGroup.alpha = 0f;
    titleVideoPlayer.url = path;
    titleVideoPlayer.Play();
 }

 void UpdateTitleClickPrompt(){
     if (!titleSequenceActive || titleVideoPlayer == null || titleVideoPlayer.clip == null || !titleVideoPlayer.isPlaying) {
         if (titleClickPromptGroup != null) titleClickPromptGroup.alpha = 0f;
         return;
     }

     float middle = (float)(titleVideoPlayer.clip.length * 0.5d);
     float timeLeft = (float)titleVideoPlayer.time;
     float alphaTarget = (timeLeft >= middle - 0.5f && timeLeft <= middle + 0.5f) ? 1f : 0f;
     titleClickPromptGroup.alpha = Mathf.Lerp(titleClickPromptGroup.alpha, alphaTarget, Time.unscaledDeltaTime * 6f);
 }
  void BuildCafe(){cafeSet=Panel("Cafe background",stage,0,0,1280,720,new Color(.91f,.78f,.72f));Panel("Floor",cafeSet,0,-230,1280,260,new Color(.52f,.32f,.32f));Panel("Wall trim",cafeSet,0,-85,1280,14,new Color(.41f,.22f,.26f));for(int i=0;i<3;i++){float x=-445+i*435;Panel("Window frame",cafeSet,x,100,325,380,new Color(.47f,.27f,.31f));Panel("Evening glass",cafeSet,x,100,299,354,new Color(.78f,.84f,.85f));Panel("Window bar",cafeSet,x,100,8,355,new Color(.47f,.27f,.31f));Panel("Window bar",cafeSet,x,125,300,8,new Color(.47f,.27f,.31f));Panel("Table leg",cafeSet,x,-240,22,145,new Color(.30f,.16f,.22f));Panel("Table",cafeSet,x,-165,340,26,new Color(.65f,.40f,.38f));Panel("Cup",cafeSet,x-70,-141,26,30,new Color(1,.92f,.86f));Panel("Pendant cord",cafeSet,x,305,3,110,new Color(.3f,.2f,.22f));Panel("Pendant light",cafeSet,x,247,75,35,new Color(1,.84f,.57f));}cafeSet.gameObject.SetActive(false);}
 void Clear(Transform root){for(int i=root.childCount-1;i>=0;i--)Destroy(root.GetChild(i).gameObject);}
 void SetMusic(string key){musicKey=key;AudioClip clip=key=="cafe_theme"?cafeMusic:key=="battle"?battleMusic:key=="title"?titleMusic:universityMusic;if(music.clip!=clip){music.clip=clip;music.Play();}music.volume=PlayerPrefs.GetFloat("Amora.music",.4f);}
 public void ShowTitle(){Mode="title";paused=false;historyOpen=false;if(Battle!=null)Destroy(Battle.gameObject);Clear(controls);Clear(overlay);dialogueGroup.gameObject.SetActive(false);backdrop.texture=festival;backdrop.color=Color.white;cafeSet.gameObject.SetActive(false);desiredVisibility=1;expression="neutral";portrait.texture=neutral;portrait.rectTransform.anchoredPosition=new Vector2(300,-150);location.text="A ROMANCE IN TWO HEARTBEATS";SetMusic("title");
 controls.gameObject.SetActive(true);
 overlay.gameObject.SetActive(true);
 var p=Panel("Title card",controls,-305,0,560,570,new Color(1,.96f,.95f,.94f));Label("Eyebrow",p,"ARROWWOOD STORIES  /  01",0,224,490,30,16,rose);
 Label("Title",p,"CUPID\nBULLET",0,117,510,175,76,inkColor);
 CreateButton(p,"Start",0,-94,280,59,NewGame);
 CreateButton(p,"Continue",0,-165,280,48,Load).interactable=File.Exists(SavePath);CreateButton(p,"How to play",-76,-229,145,42,()=>Help());CreateButton(p,"Quit",90,-229,145,42,Quit);
 Label("Footer",controls,"A visual novel × bullet hell",340,-323,470,28,18,Color.white);
 controls.anchoredPosition = new Vector2(0, 30f);
 titleGroup.alpha = 0f;
 titleGroup.interactable = false;
 titleGroup.blocksRaycasts = false;
 if(titleIntroRoutine != null)StopCoroutine(titleIntroRoutine);
 titleIntroRoutine = StartCoroutine(TitleIntro());
 }

 IEnumerator TitleIntro(){
     float t = 0f;
     Vector2 startPos = new Vector2(0f, 32f);
     Vector2 endPos = Vector2.zero;
     while(t < 0.8f){
         t += Time.unscaledDeltaTime;
         float p = Mathf.Clamp01(t / 0.8f);
         float eased = 1f - Mathf.Pow(1f - p, 3f);
         titleGroup.alpha = Mathf.Lerp(0f, 1f, eased);
         controls.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
         yield return null;
     }
     titleGroup.alpha = 1f;
     controls.anchoredPosition = endPos;
     titleGroup.interactable = true;
     titleGroup.blocksRaycasts = true;
     titleIntroRoutine = null;
 }
 public void NewGame(){if(Battle)Destroy(Battle.gameObject);paused=false;historyOpen=false;Story=new Story(ink.text);GameData.ResetAll();history.Clear();festivalUnlocked=false;background="courtyard";expression="neutral";playerName="MC";desiredVisibility=0;portraitVisibility=0;PendingBattle=null;Clear(controls);Clear(overlay);Mode="story";dialogueGroup.gameObject.SetActive(true);SetBackground(background);SetMusic("university_theme");StoryHUD();NextLine();}
 void StoryHUD(){Clear(controls);CreateButton(controls,"History",350,322,110,36,ShowHistory);CreateButton(controls,"Save",465,322,95,36,Save);CreateButton(controls,"Menu",570,322,95,36,Pause);}
 public void Advance(){if(Mode!="story"||paused||historyOpen)return;if(revealed<fullLine.Length){revealed=fullLine.Length;dialogue.text=fullLine;return;}NextLine();}
 public void NextLine(){ClearChoices();for(int guard=0;guard<100;guard++){
 if(Story.canContinue){string line=Story.Continue().Trim();thought=false;foreach(string tag in Story.currentTags)HandleTag(tag.Trim());if(Mode!="story")return;if(PendingBattle!=null){BeginCheck();return;}if(line.Length==0)continue;Display(line);return;}
 if(Story.currentChoices.Count>0){ShowChoices();return;}EndChapter();return;}}
 void HandleTag(string tag){if(tag=="SHOW_NAME_INPUT"){ShowName();return;}int colon=tag.IndexOf(':');string key=colon<0?tag:tag.Substring(0,colon).Trim();string value=colon<0?"":tag.Substring(colon+1).Trim();switch(key){case "background":SetBackground(value);break;case "music":SetMusic(value);break;case "thought":thought=true;break;case "effect":flashAlpha=1;break;case "item":GameData.SetFlag(value);break;case "event":if(value=="bullethell_1"||value=="start_bullethell_2")PendingBattle=value;else if(value=="unlock_festival"){festivalUnlocked=true;GameData.SetFlag(value);}else GameData.SetFlag(value);break;}}
 void SetBackground(string key){background=key;backdrop.texture=cafe;cafeSet.gameObject.SetActive(key=="lovestruck_cafe");backdrop.color=key=="game_over"?new Color(.42f,.22f,.36f):Color.white;location.text=key=="lovestruck_cafe"?"":key=="game_over"?"A HEART OUT OF RHYTHM":"ARROWWOOD UNIVERSITY";desiredVisibility=0;flashAlpha=.65f;}
 void Display(string line){line=line.Replace("playerName",playerName);string who="";int split=line.IndexOf(':');if(split>=0&&split<15){string candidate=line.Substring(0,split);if(candidate=="MC"||candidate=="Amora"||candidate=="AMORA"||candidate=="Jamie"||candidate=="???"){who=candidate=="MC"?playerName:candidate=="AMORA"?"Amora":candidate;line=line.Substring(split+1).Trim();}}
 if(who=="Amora"||who=="???"){desiredVisibility=1;SetExpression(line);}string lower=line.ToLowerInvariant();if(lower.Contains("blush"))expression="blush";if(lower.Contains("runs toward")||lower.Contains("watch her disappear")||lower.Contains("walks away")||lower.Contains("waves goodbye"))desiredVisibility=0;
 if(lower.Contains("walking into the café")){desiredVisibility=1;expression="neutral";}
 portrait.texture=ExpressionTexture();portrait.rectTransform.anchoredPosition=new Vector2(170,-190);
 speaker.text=who.Length==0?" ":who+(thought?"  ·  thinking":"");dialogue.fontStyle=thought?FontStyle.Italic:FontStyle.Normal;fullLine=line;revealed=0;lineAge=0;dialogue.text="";history.Add((who.Length>0?who+": ":"")+line);if(history.Count>300)history.RemoveAt(0);}
 void SetExpression(string line){string l=line.ToLowerInvariant();expression=l.Contains("sorry")||l.Contains("okay")||l.Contains("pass")?"concerned":l.Contains("date")||l.Contains("cute")||l.Contains("sweet")?"blush":l.Contains("what?!")?"angry":"neutral";}
 Texture ExpressionTexture(){return expression=="blush"?blush:expression=="concerned"?concerned:expression=="angry"?angry:neutral;}
 void ClearChoices(){foreach(var o in choiceObjects)if(o)Destroy(o);choiceObjects.Clear();}
 void ShowChoices(){hint.text="Choose your response";for(int i=0;i<Story.currentChoices.Count;i++){int index=i;var choice=Story.currentChoices[i];var b=CreateButton(controls,choice.text.Replace("playerName",playerName),0,95-i*83,880,70,()=>Choose(index));choiceObjects.Add(b.gameObject);}}
 public void Choose(int index){if(paused||Mode!="story")return;Story.ChooseChoiceIndex(index);ClearChoices();NextLine();}
 void ShowName(){Mode="name";Clear(overlay);var p=Panel("Your name",overlay,0,10,650,300,new Color(1,.95f,.96f));Label("Question",p,"What should Amora call you?",0,95,590,50,30,inkColor);var r=Panel("Name input",p,0,15,450,58,Color.white);nameInput=r.gameObject.AddComponent<InputField>();var t=Label("Value",r,"",0,0,420,50,26,inkColor,TextAnchor.MiddleLeft);nameInput.textComponent=t;nameInput.characterLimit=24;nameInput.text="MC";CreateButton(p,"Begin",0,-80,250,55,()=>SubmitName(nameInput.text));nameInput.ActivateInputField();}
 public void SubmitName(string value){playerName=string.IsNullOrWhiteSpace(value)?"MC":value.Trim();Story.variablesState["playerName"]=playerName;Story.ChoosePathString("first_encounter");Mode="story";Clear(overlay);NextLine();}
 void BeginCheck(){Mode="briefing";ClearChoices();Clear(controls);dialogueGroup.gameObject.SetActive(false);flashAlpha=1;Clear(overlay);var p=Panel("Skill check briefing",overlay,0,0,720,430,new Color(.16f,.035f,.13f,.98f));Label("Heading",p,PendingBattle=="bullethell_1"?"01  /  CATCH YOUR BREATH":"02  /  A RACING HEART",0,150,660,65,32,Color.white);Label("Instructions",p,"Keep your heart safe until the timer ends.\n\nWASD / Arrow keys — move\nHold Shift — slow, precise movement\nSpace — send a heart back\nEsc — pause\n\nThree hits end the attempt. Your result changes the story.",0,15,650,210,22,new Color(1,.80f,.88f));CreateButton(p,"I'm ready",0,-160,300,55,StartBattle);}
 public void StartBattle(){Clear(overlay);Mode="battle";SetMusic("battle");var g=new GameObject("Heart skill check");g.transform.SetParent(transform);Battle=g.AddComponent<AmoraBattle>();Battle.Begin(this,stage,PendingBattle=="bullethell_1"?1:2,heart,cupid,shotSound);flash.transform.SetAsLastSibling();overlay.SetAsLastSibling();}
 public void FinishBattle(bool won,int hp,float duration){if(Mode!="battle")return;GameData.RecordBattle(new GameData.BattleResult{battleId=PendingBattle,win=won,playerHP=hp,playerMaxHP=3,duration=duration});Mode="result";if(Battle)Battle.Stop();Clear(overlay);var p=Panel("Result",overlay,0,0,650,270,new Color(.16f,.035f,.13f,.97f));Label("Result heading",p,won?"HEART STEADY":"HEART OVERWHELMED",0,75,610,55,34,Color.white);Label("Result detail",p,won?"You held your nerve. Let's see what Amora thinks.":"Take a breath. The story will remember this attempt.",0,5,585,65,22,new Color(1,.80f,.88f));CreateButton(p,"Return to Amora",0,-80,310,54,()=>ResolveBattle(won));}
 public void ResolveBattle(bool won){if(Battle)Destroy(Battle.gameObject);Clear(overlay);string expected=won?"Win":"Lose";int guard=0;while(Story.canContinue&&guard++<30)Story.Continue();var c=Story.currentChoices.Find(x=>x.text.Trim().Equals(expected,StringComparison.OrdinalIgnoreCase));if(c==null){Debug.LogError("Skill check result choice missing: "+PendingBattle);return;}Story.ChooseChoiceIndex(c.index);PendingBattle=null;Mode="story";SetMusic(background=="lovestruck_cafe"?"cafe_theme":"university_theme");dialogueGroup.gameObject.SetActive(true);StoryHUD();flashAlpha=.8f;NextLine();}
 void EndChapter(){Mode="ending";Clear(controls);Clear(overlay);dialogueGroup.gameObject.SetActive(false);string status=(string)Story.variablesState["festival_status"];var p=Panel("Chapter end",overlay,0,0,790,400,new Color(1,.95f,.96f,.98f));Label("End heading",p,festivalUnlocked?"AUTUMN FESTIVAL UNLOCKED":"UNTIL NEXT TIME",0,130,730,65,35,inkColor);Label("End detail",p,festivalUnlocked?(status=="date"?"It's a date.\nAmora is looking forward to Friday.":"A new friendship.\nYou and Amora are going to the festival.")+"\n\nYou've reached the end of the supplied story.":"This attempt has ended.\nStart again to discover a different route.",0,10,700,160,24,rose);CreateButton(p,"Title screen",0,-135,300,55,ShowTitle);}
 void Pause(){if(paused){Resume();return;}paused=true;if(Battle)Battle.Paused=true;Clear(overlay);var p=Panel("Paused",overlay,0,0,650,350,new Color(1,.94f,.97f,.99f));Label("Pause heading",p,"Take a breath",0,115,590,60,36,inkColor);CreateButton(p,"Resume",0,40,310,50,Resume);CreateButton(p,"Music: on / off",0,-25,310,50,()=>{float v=music.volume>0?0:.4f;music.volume=v;PlayerPrefs.SetFloat("Amora.music",v);});CreateButton(p,"Title screen",0,-90,310,50,ShowTitle);}
 void Resume(){paused=false;if(Battle)Battle.Paused=false;Clear(overlay);}
 void Help(){Clear(overlay);var p=Panel("How to play",overlay,0,0,820,400,new Color(1,.95f,.96f));Label("Help",p,"Read. Choose. Keep your heart steady.\n\nClick the dialogue box or press Space / Enter to advance.\nClick once during typing to reveal the whole line.\nChoose responses with the mouse.\nIn skill checks: WASD / arrows to dodge, Shift to focus, Space to shoot.\nSurvive the timer or clear the heart barrier to win.\nSave and continue between dialogue lines. Esc pauses.",0,35,750,285,23,inkColor);CreateButton(p,"Back",0,-140,250,48,()=>Clear(overlay));}
 void ShowHistory(){if(Mode!="story")return;historyOpen=true;Clear(overlay);var p=Panel("History",overlay,0,0,1120,630,new Color(1,.96f,.97f,.99f));int from=Mathf.Max(0,history.Count-8);Label("Recent dialogue",p,string.Join("\n\n",history.GetRange(from,history.Count-from)),0,30,1040,510,20,inkColor,TextAnchor.UpperLeft);CreateButton(p,"Return",0,-270,250,45,()=>{historyOpen=false;Clear(overlay);});}
 string SavePath=>System.IO.Path.Combine(Application.persistentDataPath,"amora-story.json");
 [Serializable] class SaveData{public string state,line,speaker,name,background,expression,music;public bool unlocked;public float visible;public List<string> history;}
 public void Save(){if(Mode!="story"||paused)return;try{var d=new SaveData{state=Story.state.ToJson(),line=fullLine,speaker=speaker.text,name=playerName,background=background,expression=expression,music=musicKey,unlocked=festivalUnlocked,visible=desiredVisibility,history=history};File.WriteAllText(SavePath,JsonUtility.ToJson(d));hint.text="Saved";}catch(Exception e){hint.text="Could not save";Debug.LogException(e);}}
 public void Load(){try{var d=JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));Story=new Story(ink.text);Story.state.LoadJson(d.state);GameData.ResetAll();playerName=d.name;festivalUnlocked=d.unlocked;SetBackground(d.background);expression=d.expression;portrait.texture=ExpressionTexture();desiredVisibility=d.visible;portrait.rectTransform.anchoredPosition=new Vector2(170,-190);SetMusic(d.music);fullLine=d.line;speaker.text=d.speaker;revealed=fullLine.Length;dialogue.text=fullLine;history.Clear();if(d.history!=null)history.AddRange(d.history);Mode="story";PendingBattle=null;paused=false;Clear(overlay);dialogueGroup.gameObject.SetActive(true);StoryHUD();if(Story.currentChoices.Count>0)ShowChoices();}catch(Exception e){Debug.LogException(e);ShowTitle();}}
 void Quit(){Application.Quit();
 #if UNITY_EDITOR
 UnityEditor.EditorApplication.isPlaying=false;
 #endif
 }
 void Update(){float dt=Time.unscaledDeltaTime;portraitVisibility=Mathf.MoveTowards(portraitVisibility,desiredVisibility,dt*3);portraitGroup.alpha=portraitVisibility;portrait.rectTransform.localScale=Vector3.one*(1+.003f*Mathf.Sin(Time.unscaledTime*1.8f));flashAlpha=Mathf.MoveTowards(flashAlpha,0,dt*1.7f);flash.color=new Color(1,.45f,.72f,flashAlpha);
 if(titleSequenceActive) UpdateTitleClickPrompt();
 var kb=Keyboard.current;if(kb!=null&&kb.escapeKey.wasPressedThisFrame&&Mode!="title"&&Mode!="name"&&Mode!="ending"){if(historyOpen){historyOpen=false;Clear(overlay);}else if(Mode=="story"||Mode=="battle")Pause();}
 if(paused||historyOpen)return;if(Mode=="story"){lineAge+=dt;revealed=Mathf.Min(fullLine.Length,revealed+dt*PlayerPrefs.GetFloat("Amora.textSpeed",48));dialogue.text=fullLine.Substring(0,(int)revealed);dialogueGroup.alpha=Mathf.Clamp01(lineAge*8);if(Story.currentChoices.Count==0)hint.text=revealed<fullLine.Length?"Click to reveal":"Click / Space to continue  ▸";if(kb!=null&&(kb.spaceKey.wasPressedThisFrame||kb.enterKey.wasPressedThisFrame)&&lineAge>.1f)Advance();}
 }
 void OnDestroy(){if(Instance==this)Instance=null;}
}
}




