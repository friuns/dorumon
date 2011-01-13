using System.Linq;
using UnityEngine;
using System.Collections;
using doru;
using System.Collections.Generic;
using System;
public enum Team : int { Red, Blue, None }
interface IAim { void Aim(); }
[Serializable]
public class Player : Destroible,IAim 
{
    float shownicktime;
    public List<Vector3> plPathPoints = new List<Vector3>();
    public TextMesh title;
    public float nitro;
    public new bool dead { get { return !Alive && spawned; } }
    public new Team team = Team.None;
    public float speed;
    public float score;
    public bool haveLight;
    public bool haveTimeBomb;
    public int speedUpgrate;
    public int lifeUpgrate;
    public bool haveAntiGravitation;
    public float freezedt;
    public int guni;
    public int fps;
    public int ping;
    public int deaths;
    new public string nick;
    public bool spawned;
    public int frags;
    public float defMaxLife;
    [FindTransform("speedparticles")]
    public ParticleEmitter speedparticles;
    [FindTransform("Guns")]
    public Transform guntr;
    [GenerateEnums("GunType")]
    public List<GunBase> guns = new List<GunBase>();
    public int selectedgun;
    public GunBase gun { get { return guns[selectedgun]; } }
    public float defmass;
    [FindTransform("Sphere")]
    public GameObject model;
    public override void Init()
    {        
        base.Init();
        gameObject.layer = LayerMask.NameToLayer("Player");
        guns = guntr.GetChild(0).GetComponentsInChildren<GunBase>().ToList();
        shared = false;
        title = transform.GetComponentInChildren<TextMesh>();
        laserRender = root.GetComponentInChildren<LineRenderer>();        
        fanarik = this.GetComponentsInChildren<Light>().FirstOrDefault(a => a.type == LightType.Spot);
        nitro = 10;
    }
    void OnCollisionStay(Collision collisionInfo)
    {
        isGrounded = 0;
    }
    protected override void Awake()
    {
        AliveMaterial = model.renderer.sharedMaterial;        
        Debug.Log("player awake");
        defmass = rigidbody.mass;
        defMaxLife = maxLife;
        this.rigidbody.maxAngularVelocity = 3000;
        if (networkView.isMine)
        {
            RPCSetOwner();
            RPCSetUserInfo(LocalUserV.nick);
            RPCSpawn();
        }
        //speedparticles = transform.Find("speedparticles").GetComponent<ParticleEmitter>();
        base.Awake();
    }
    protected override void Start()
    {
        base.Start();
    }
    public override void OnPlayerConnected1(NetworkPlayer np)
    {
        base.OnPlayerConnected1(np);        
        RPCSetUserInfo(nick);
        RPCSetFrags(frags, score);
        RPCSetDeaths(deaths);
        RPCSetTeam((int)team);
        RPCSpawn();        
        RPCSetAlive( Alive);
        RPCSetFanarik(fanarik.enabled);
        RPCSelectGun( selectedgun);
        RPCSetLifeUpgrate(lifeUpgrate);
        RPCSetSpeedUpgrate(speedUpgrate);
        //if (spawned && dead) networkView.RPC("RPCDie", np, -1);
    }
    public override void OnSetOwner()
    {
        print("set owner" + OwnerID);
        if (isOwner)
        {
            tag = name = "LocalPlayer";
        }
        else
            name = "RemotePlayer" + OwnerID;
        _Game.players[OwnerID] = this;        
    }
    public void RPCSpawn() { CallRPC("Spawn"); }
    [RPC]
    public void Spawn()
    {        
        print(pr + "+" + OwnerID);        
        if (isOwner)
        {
            RPCSetTeam((int)team);
            RPCSetAlive(Alive);
            ResetSpawn();
        }        
    }
    public override void ResetSpawn()
    {
        base.ResetSpawn();
        transform.position = GameObject.FindGameObjectWithTag("Spawn" + team.ToString()).transform.position;
        transform.rotation = Quaternion.identity;
    }
    public void LocalSelectGun(int id)
    {
        if (guns.Count(a => a.group == id && a.patronsLeft > 0) == 0 && !debug) return;
        bool foundfirst = false;
        bool foundnext = false;
        for (int i = selectedgun; i < guns.Count; i++)
            if (guns[i].group == id && (guns[i].patronsLeft > 0 || debug))
            {
                if (foundfirst) { selectedgun = i; foundnext = true; break; }
                foundfirst = true;
            }
        if (!foundnext)
            for (int i = 0; i < guns.Count; i++)
                if (guns[i].group == id && (guns[i].patronsLeft > 0 || debug))
                {
                    selectedgun = i;
                    break;
                }
        
        RPCSelectGun(selectedgun);
    }
    [FindAsset("change")]
    public AudioClip changeSound;
    public void RPCSelectGun(int i) { CallRPC("SelectGun", i); }
    [RPC]
    public void SelectGun(int i)
    {        
        PlaySound(changeSound);
        selectedgun = i;
        foreach (GunBase gb in guns)
            gb.DisableGun();


        if (Alive)
            guns[selectedgun].EnableGun();
    }
    protected override void Update()
    {
        maxLife = defMaxLife + (lifeUpgrate * 100);
        if (!Alive && fanarik.enabled) fanarik.enabled = false;
        UpdateAim();
        if (isOwner)
            nitro += Time.deltaTime / 5;
        
        UpdateTitle();

        if (_TimerA.TimeElapsed(100))
        {
            if (plPathPoints.Count == 0 || Vector3.Distance(pos, plPathPoints.Last()) > 1)
            {
                plPathPoints.Add(pos);
                if (plPathPoints.Count > 10) plPathPoints.RemoveAt(0);
            }
        }
        
        
        multikilltime -= Time.deltaTime;
        if (this.rigidbody.velocity.magnitude > 30)
        {
            speedparticles.worldVelocity = this.rigidbody.velocity / 10;
            if (_TimerA.TimeElapsed(100))
            {
                speedparticles.transform.rotation = Quaternion.identity;
                speedparticles.Emit();
            }
        }
        if (freezedt >= 0)
            freezedt -= Time.deltaTime;
        LocalUpdate();
        base.Update();
        //UpdateLightmap(model.renderer.materials);
    }
    private void LocalUpdate()
    {
        if (isOwner && lockCursor && Alive)
        {
            //NextGun(Input.GetAxis("Mouse ScrollWheel"));
            if (_TimerA.TimeElapsed(200))
            {
                if (Input.GetKey(KeyCode.H) || Input.GetKey(KeyCode.G))
                    foreach (var a in players.Union(_Game.towers.Cast<Destroible>()).Where(a => a != null && a != this && Vector3.Distance(a.pos, pos) < 10))
                    {
                        if (Input.GetKey(KeyCode.H) && a.Life < a.maxLife)
                        {
                            a.RPCSetLife(a.Life + 2, -1);
                        }
                        if (Input.GetKey(KeyCode.G) && a is Player)
                        {
                            var p = ((Player)a);
                            if (score > 10)
                            {
                                p.RPCSetFrags(p.frags, p.score + 10);
                                score -= 10;
                            }
                        }
                    }
            }

            SelectGun();
            if (Input.GetKey(KeyCode.LeftShift))
                this.transform.rotation = Quaternion.identity;

            if (Input.GetKeyDown(KeyCode.Y) && (haveAntiGravitation || debug))
            {
                haveAntiGravitation = false;
                _TimerA.AddMethod(15000, delegate { _Game.RPCSetGravityBomb(false); });
                _Game.RPCSetGravityBomb(true);
            }
            if (Input.GetKeyDown(KeyCode.T) && (haveTimeBomb || debug))
            {
                haveTimeBomb = false;
                _TimerA.AddMethod(10000, delegate { _Game.RPCSetTimeBomb(1); });
                _Game.RPCSetTimeBomb(Time.timeScale * 0.5f);
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (nitro > 10 && isGrounded < 1 || !build)
                {
                    nitro -= 10;
                    RPCJump();
                }
            }
            if ((haveLight || debug) && Input.GetKeyDown(KeyCode.R))
            {
                RPCSetFanarik(!fanarik.enabled);                
            }
        }
    }
    private void UpdateTitle()
    {
        if (OwnerID != -1 && (team == Team.Red || team == Team.Blue))
            title.renderer.material.color = team == Team.Red ? Color.red : Color.blue;
        else
            title.renderer.material.color = Color.white;

        if (shownicktime > 0)
            title.text = nick + ":" + Life;
        else
            title.text = "";

        shownicktime -= Time.deltaTime;
    }
    public void Aim()
    {
        shownicktime = 3;
    }
    public void RPCSetFanarik(bool v) { CallRPC("SetFanarik",v); }
    [RPC]
    public void SetFanarik(bool value)
    {
        haveLight = true;
        fanarik.enabled = value;
    }
    public Light fanarik;
    private void SelectGun()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            LocalSelectGun(1);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            LocalSelectGun(2);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            LocalSelectGun(3);
        if (Input.GetKeyDown(KeyCode.Alpha4))
            LocalSelectGun(4);
        if (Input.GetKeyDown(KeyCode.Alpha5))
            LocalSelectGun(5);
        if (Input.GetKeyDown(KeyCode.Alpha6))
            LocalSelectGun(6);
        if (Input.GetKeyDown(KeyCode.Alpha7))
            LocalSelectGun(7);
        if (Input.GetKeyDown(KeyCode.Alpha8))
            LocalSelectGun(8);
        if (Input.GetKeyDown(KeyCode.Alpha9))
            LocalSelectGun(9);
    }
    public LineRenderer laserRender;
    public void UpdateAim()
    {
        if (isOwner) syncRot = _Cam.transform.rotation;
        guntr.rotation = syncRot;

        Ray r = gun.GetRay();
        RaycastHit h = new RaycastHit() { point = r.origin + r.direction * 100 };        
        if (Physics.Raycast(r, out h, 100))
        {
            var aim = h.collider.gameObject.transform.GetMonoBehaviorInParrent() as IAim;
            if (aim != null)
                aim.Aim();
        }

        if ((gun.laser || debug) && Alive && selectedgun != (int)GunType.physxgun)
        {            
            laserRender.enabled = true;
            laserRender.SetPosition(0, r.origin);
            laserRender.SetPosition(1, h.point);
        }
        else
            laserRender.enabled = false;
    }
    protected virtual void FixedUpdate()
    {                
        if (isOwner) FixedLocalMove();
        //UpdateAim();
    }
    private void FixedLocalMove()
    {
        if (lockCursor)
        {
            Vector3 moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            moveDirection = _Cam.transform.TransformDirection(moveDirection);
            if(Physics.gravity == _Game.gravity)
                moveDirection.y = 0;
            moveDirection.Normalize();
            Vector3 v = this.rigidbody.velocity;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                this.rigidbody.angularVelocity = Vector3.zero;
                this.rigidbody.AddForce(moveDirection / Time.timeScale * speed * 200);
                v.x *= .35f;                
                v.z *= .35f;
                if (Physics.gravity != _Game.gravity)
                    v.y *= .35f;
                this.rigidbody.velocity = v;
            }
            else
            {
                this.rigidbody.AddTorque(new Vector3(moveDirection.z, 0, -moveDirection.x) * speed / Time.timeScale*5);
            }
            if (freezedt > 0) this.rigidbody.velocity *= .95f;
        }
    }
    public void RPCJump() { CallRPC("Jump"); }
    [RPC]
    public void Jump()
    {        
        transform.rigidbody.MovePosition(rigidbody.position + new Vector3(0, 1, 0));
        rigidbody.AddForce(_Cam.transform.rotation * new Vector3(0, 0, 1000) / Time.timeScale);
        PlaySound(nitrojumpSound);
    }
    [FindAsset("nitrojump")]
    public AudioClip nitrojumpSound;
    public void NextGun(float a)
    {
        if (a != 0)
        {
            if (a > 0)
                guni++;
            if (a < 0)
                guni--;
            if (guni > guns.Count - 1) guni = 0;
            if (guni < 0) guni = guns.Count - 1;
            RPCSelectGun(guni);
        }
    }
    public void RPCSetTeam(int t) { CallRPC("SetTeam",t); }
    [RPC]
    public void SetTeam(int t)
    {
        print(pr);        
        team = (Team)t;
    }
    [RPC]
    public void RPCSetDeaths(int d) { deaths = d; }
    protected override void OnCollisionEnter(Collision collisionInfo)
    {        
        if (!Alive) return;
        
        if (isOwner && collisionInfo.relativeVelocity.y > 30)
            RPCPowerExp(this.transform.position);
        base.OnCollisionEnter(collisionInfo);
    }
    [FindAsset("powerexp")]
    public AudioClip powerexpSound;
    [FindAsset("wave")]
    public GameObject WavePrefab;
    [FindAsset("bowling")]
    public AudioClip bowling;
    public void RPCPowerExp(Vector3 v) { CallRPC("PowerExp",v); }        
    [RPC]
    public void PowerExp(Vector3 v)
    {        
        PlaySound(powerexpSound, 4);
        
        GameObject g = (GameObject)Instantiate(WavePrefab, v, Quaternion.Euler(90, 0, 0));
        Explosion e = g.AddComponent<Explosion>();
        e.OwnerID = OwnerID;
        e.self = this;
        e.exp = 5000;
        e.radius = 10;
        e.damage = 200;
        if(isOwner)
            _Cam.exp = 2;
        
        Destroy(g, 1.6f);
    }
    [RPC]
    public override void SetLife(float NwLife, int killedby)
    {
        if (!Alive) return;
        if (isOwner)
            _GameWindow.Hit(Mathf.Abs(Life - NwLife) * 2);

        freezedt = (Life - NwLife) / 20;

        if (isEnemy(killedby) || NwLife > Life)
            Life = Math.Min(NwLife, 100);

        if (Life <= 0 && isOwner)
            RPCDie(killedby);

    }
    public void RPCSetUserInfo(string nick) { CallRPC("SetUserInfo", nick); }
    [RPC]
    public void SetUserInfo(string nick)
    {        
        this.nick = nick;
    }
    [FindAsset("Detonator-Base")]
    public GameObject detonator;
    [RPC]
    public override void Die(int killedby)
    {
        
        if (!Alive) return;
        print(pr);
        Detonator dt = this.detonator.GetComponent<Detonator>();
        dt.autoCreateForce = false;
        dt.size = 3;
        Instantiate(dt, transform.position, Quaternion.identity);
        var exp = dt.gameObject.AddComponent<Explosion>();
        exp.self = this;
        deaths++;
        
        if (killedby == _localPlayer.OwnerID)
        {
            if (OwnerID == _localPlayer.OwnerID)
            {
                _Game.RPCWriteMessage(_localPlayer.nick + " Killed self ");
                _localPlayer.AddFrags(-1, -.5f);
            }
            else if (team != _localPlayer.team || mapSettings.DM)
            {
                _Game.RPCWriteMessage(_localPlayer.nick + " kill " + nick);
                _localPlayer.AddFrags(+1, 2);
            }
            else
            {
                _Game.RPCWriteMessage(_localPlayer.nick + " kill " + nick);
                _localPlayer.AddFrags(-1, -1);
            }
        }
        if (killedby == -1)
        {
            _Game.RPCWriteMessage(nick + " died ");
        }

        if (isOwner)
        {
            if (!mapSettings.zombi) _TimerA.AddMethod(10000, delegate { RPCSetAlive(true); });
            RPCSetAlive(false);
        }
    }
    public Material AliveMaterial;
    public Material deadMaterial;
    public void RPCSetAlive(bool v) { CallRPC("SetAlive", v); }
    [RPC]
    public void SetAlive(bool value)
    {
        Debug.Log(name + " Alive " + value);
        foreach (var t in GetComponentsInChildren<Transform>())
            t.gameObject.layer = value ? LayerMask.NameToLayer("Default") : LayerMask.NameToLayer("DeadPlayer");

        Alive = value;
        RPCSetFanarik(false);
        if(value)
            spawned = true;
        model.renderer.sharedMaterial = value? AliveMaterial:deadMaterial;                
        foreach (GunBase gunBase in guns.Concat(guns))
            gunBase.Reset();
        if (isOwner)
            LocalSelectGun(1);
        Life = maxLife;
        freezedt = 0;

    }
    float multikilltime;
    int multikill;
    public void AddFrags(int i,float sc)
    {
        if (multikilltime > 0)
            multikill += i;
        else
            multikill = 0;
        multikilltime = 1;

        if (multikill >= 1)
        {
            if (gun is GunPhysix)
            {
                if (!audio.isPlaying)
                {
                    audio.clip = bowling;
                    audio.volume = 3;
                    audio.Play();                    
                }
            }
            else
                PlayRandSound(multikillSounds, 5);

            _Cam.ScoreText.text = "x" + (multikill + 1);
            _Cam.ScoreText.animation.Play();
        }
        frags += i;
        score += sc;
        RPCSetFrags(frags, score);
    }
    [FindAsset("toasty")]
    public AudioClip[] multikillSounds;
    public void RPCSetFrags(int i, float score) { CallRPC("SetFrags", i, score); }
    [RPC]
    public void SetFrags(int i, float sc)
    {        
        frags = i;
        score = sc;
    }

    public void RPCSetSpeedUpgrate(int value) { CallRPC("SetSpeedUpgrate", value); }
    [RPC]
    public void SetSpeedUpgrate(int value)
    {
        speedUpgrate = value;
    }

    public void RPCSetLifeUpgrate(int value) { CallRPC("SetLifeUpgrate", value); }
    [RPC]
    public void SetLifeUpgrate(int value)
    {
        lifeUpgrate = value;
    }
    public static Vector3 Clamp(Vector3 velocityChange, float maxVelocityChange)
    {


        velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
        velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
        velocityChange.y = Mathf.Clamp(velocityChange.y, -maxVelocityChange, maxVelocityChange);
        return velocityChange;
    }
    public override Quaternion rot
    {
        get
        {
            return guntr.rotation;
        }
        set
        {
            guntr.rotation = value;
        }
    }
}