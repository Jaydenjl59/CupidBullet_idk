using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace AmoraNovel {
public class AmoraBattle : MonoBehaviour {
 class Orb { public RectTransform rect; public Vector2 position,velocity; public bool friendly; }
 readonly List<Orb> orbs=new List<Orb>();
 AmoraGame owner; RectTransform root,field,player; Text hud,barrierLabel; Texture heart; AudioSource audioSource;
 int level,hp=3,barrier=45; float elapsed,spawnClock,shootClock,immune,ringClock; bool stopped;
 public bool Paused; public int HP=>hp; public float Elapsed=>elapsed; public int Barrier=>barrier;
 public float Duration=>level==1?25:32;
 public void Begin(AmoraGame game,Transform canvas,int difficulty,Texture texture,Texture amora,AudioClip sound){owner=game;level=difficulty;heart=texture;barrier=level==1?45:60;root=owner.Panel("Bullet hell",canvas,0,0,1280,720,new Color(.10f,.025f,.12f));
 owner.Label("Title",root,level==1?"CATCH YOUR\nBREATH":"A RACING\nHEART",-437,265,310,105,30,new Color(1,.72f,.83f));
 owner.Label("Instructions",root,"KEEP YOUR HEART SAFE\n\nWASD / arrows · move\nShift · focus\nSpace · shoot\nEsc · pause\n\nSurvive the countdown\nor break the heart barrier.\n\nYour tiny white center\nis your hitbox.",-437,45,315,310,21,new Color(.92f,.77f,.87f));
 owner.Panel("Arena border",root,0,0,564,664,new Color(.84f,.34f,.57f));field=owner.Panel("Arena",root,0,0,556,656,new Color(.15f,.045f,.18f));
 for(int i=0;i<9;i++)owner.Panel("Grid",field,-250+i*62.5f,0,1,650,new Color(1,.6f,.8f,.08f));for(int i=0;i<11;i++)owner.Panel("Grid",field,0,-310+i*62,550,1,new Color(1,.6f,.8f,.08f));
 owner.Label("Emitter",field,"♥",0,269,100,85,70,new Color(1,.42f,.63f));player=owner.Panel("Your heart",field,0,-245,26,26,new Color(.5f,1,1));player.localRotation=Quaternion.Euler(0,0,45);owner.Panel("Hitbox",player,0,0,6,6,Color.white);
 owner.Panel("Composure panel",root,435,-270,290,106,new Color(.10f,.025f,.12f,.93f));hud=owner.Label("Status",root,"",435,-270,295,110,25,Color.white);barrierLabel=owner.Label("Barrier",root,"",435,280,295,70,23,new Color(1,.7f,.84f));
 audioSource=gameObject.AddComponent<AudioSource>();audioSource.clip=sound;audioSource.volume=.10f;
 }
 void Spawn(Vector2 position,Vector2 velocity,bool friendly=false){var r=owner.Panel(friendly?"Your shot":"Heart projectile",field,position.x,position.y,friendly?7:15,friendly?20:15,friendly?new Color(.48f,1,1):new Color(1,.35f,.61f));r.localRotation=Quaternion.Euler(0,0,45);orbs.Add(new Orb{rect=r,position=position,velocity=velocity,friendly=friendly});}
 void Update(){if(stopped||Paused)return;var kb=Keyboard.current;Vector2 move=Vector2.zero;bool fire=false,focus=false;if(kb!=null){move.x=(kb.dKey.isPressed||kb.rightArrowKey.isPressed?1:0)-(kb.aKey.isPressed||kb.leftArrowKey.isPressed?1:0);move.y=(kb.wKey.isPressed||kb.upArrowKey.isPressed?1:0)-(kb.sKey.isPressed||kb.downArrowKey.isPressed?1:0);focus=kb.leftShiftKey.isPressed||kb.rightShiftKey.isPressed;fire=kb.spaceKey.isPressed;}Tick(Mathf.Min(Time.unscaledDeltaTime,.05f),move,fire,focus);}
 public void Tick(float dt,Vector2 move,bool fire,bool focus){if(stopped||Paused)return;elapsed+=dt;immune-=dt;shootClock-=dt;var pos=player.anchoredPosition+move.normalized*(focus?105:265)*dt;pos.x=Mathf.Clamp(pos.x,-260,260);pos.y=Mathf.Clamp(pos.y,-309,225);player.anchoredPosition=pos;player.GetComponent<Image>().color=immune>0&&Mathf.Sin(elapsed*35)>0?new Color(.4f,1,1,.3f):new Color(.4f,1,1);
 if(fire&&shootClock<=0){shootClock=.16f;Spawn(pos+Vector2.up*20,Vector2.up*620,true);if(audioSource.clip)audioSource.Play();}
 spawnClock-=dt;ringClock-=dt;if(elapsed>1.5f&&spawnClock<=0){spawnClock=level==1?.62f:.46f;float sweep=Mathf.Sin(elapsed*.9f)*.48f;for(int i=-2;i<=2;i++){float angle=-Mathf.PI/2+sweep+i*.24f;Spawn(new Vector2(0,265),new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(level==1?145:175));}}
 if(level==2&&elapsed>5&&ringClock<=0){ringClock=2.8f;for(int i=0;i<14;i++){float a=i*Mathf.PI*2/14+elapsed*.11f;Spawn(new Vector2(Mathf.Sin(elapsed)*120,185),new Vector2(Mathf.Cos(a),Mathf.Sin(a))*115);}}
 for(int i=orbs.Count-1;i>=0;i--){var b=orbs[i];b.position+=b.velocity*dt;b.rect.anchoredPosition=b.position;bool remove=Mathf.Abs(b.position.x)>290||Mathf.Abs(b.position.y)>345;if(b.friendly&&b.position.y>235&&Mathf.Abs(b.position.x)<54){barrier--;remove=true;}else if(!b.friendly&&immune<=0&&Vector2.Distance(b.position,pos)<11){hp--;immune=1.25f;remove=true;}if(remove){Destroy(b.rect.gameObject);orbs.RemoveAt(i);}}
 hud.text="COMPOSURE  "+hp+" / 3\n"+Mathf.CeilToInt(Mathf.Max(0,Duration-elapsed))+" seconds";barrierLabel.text="HEART BARRIER\n"+Mathf.Max(0,barrier);
 if(hp<=0){stopped=true;owner.FinishBattle(false,0,elapsed);}else if(elapsed>=Duration||barrier<=0){stopped=true;owner.FinishBattle(true,hp,elapsed);}}
 public void Stop(){stopped=true;}
 #if UNITY_EDITOR
 public void TestHit(){immune=0;Spawn(player.anchoredPosition,Vector2.zero);Tick(.001f,Vector2.zero,false,false);}
 public void TestSurvive(){for(int i=0;i<orbs.Count;i++)Destroy(orbs[i].rect.gameObject);orbs.Clear();elapsed=Duration-.01f;Tick(.02f,Vector2.zero,false,false);}
 #endif
 void OnDestroy(){if(root)Destroy(root.gameObject);}
}
}


