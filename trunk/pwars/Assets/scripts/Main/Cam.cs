using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class Cam : Base
{
    public float xSpeed = 120.0f;
    public float ySpeed = 120.0f;
    public float yMinLimit = -90f;
    public float yMaxLimit = 90f;
    float x = 0.0f;
    float y = 0.0f;
    public TextMesh LevelText;
    public TextMesh ScoreText;
    [FindAsset("timewarp")]
    public AudioClip timewarp;    
    public override void Init()
    {
        camera = GetComponentInChildren<Camera>();
        blur = GetComponentInChildren<MotionBlur>();
        LevelText = transform.Find("LevelText").GetComponent<TextMesh>();
        ScoreText = transform.Find("ScoreText").GetComponent<TextMesh>();
        Vingetting = (MonoBehaviour)camera.GetComponent("Vignetting");
        bloomAndFlares = (MonoBehaviour)camera.GetComponent("BloomAndFlares");
        ambientsmoke = transform.Find("ambientsmoke");
        ssao = GetComponentInChildren<SSAOEffect>();
        xSpeed = 120;
        ySpeed = 120;
        yMinLimit = -90;
        yMaxLimit = 90;        
        base.Init();
    }
    public new Camera camera;
    public Transform ambientsmoke;
    public MotionBlur blur;
    public MonoBehaviour Vingetting;
    public SSAOEffect ssao;
    public MonoBehaviour bloomAndFlares;
    public void onEffect()
    {

        ambientsmoke.gameObject.active = _SettingsWindow.AtmoSphere;
        if (ssao.enabled != _SettingsWindow.Sao) { ssao.enabled = _SettingsWindow.Sao; Debug.Log("sao settings" + ssao.enabled); }
        if (blur.enabled != _SettingsWindow.MotionBlur) { blur.enabled = _SettingsWindow.MotionBlur; Debug.Log("blur settings" + blur.enabled); }
        if (bloomAndFlares.enabled != _SettingsWindow.BloomAndFlares) { bloomAndFlares.enabled = _SettingsWindow.BloomAndFlares; Debug.Log("blom and flares" + bloomAndFlares.enabled); }
        if (_SettingsWindow.iGraphicQuality != -1 && (QualityLevel)_SettingsWindow.iGraphicQuality != QualitySettings.currentLevel)
        {            
            QualitySettings.currentLevel = (QualityLevel)_SettingsWindow.iGraphicQuality;
            print("graphics quality changed" + QualitySettings.currentLevel);
        }
        if (!_SettingsWindow.Shadows)
        {
            Debug.Log("shadows are dissabled" + _SettingsWindow.Shadows);
            foreach (Light l in GameObject.FindObjectsOfTypeIncludingAssets(typeof(Light)))
                l.shadows = LightShadows.None;
        }
        if (_SettingsWindow.iRenderSettings != -1 && _SettingsWindow.iRenderSettings != 2)
        {
            Debug.Log("render type settings chagned" + (RenderingPath)_SettingsWindow.iRenderSettings);
            foreach (Camera c in GameObject.FindObjectsOfTypeIncludingAssets(typeof(Camera)))
                c.renderingPath = (RenderingPath)_SettingsWindow.iRenderSettings;
        }
    }
    protected override void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
        onEffect();
    }
    float blurtime;
    void FixedUpdate()
    {
        blurtime += Time.deltaTime;
        if (blurtime>.1f)
        {
            blurtime -= .1f;
            blur.blurAmount = Vector3.Distance(oldpos, transform.position) / 15;            
            oldpos = transform.position;
        }
        CamUpdate();
    }
    void Update()
    {
        if (_Cam.Vingetting.enabled)
        {
            root.audio.clip = timewarp;
            if (!root.audio.isPlaying)            
                root.audio.Play();
        }
        else if (root.audio.clip == timewarp) root.audio.Stop();
        //CamUpdate();
    }
    void CamUpdate()
    {
        camera.fieldOfView = _SettingsWindow.Fieldof;
        xoffset = _SettingsWindow.Camx+0.01f;
        yoffset = _SettingsWindow.Camy + 0.01f;
        if (lockCursor)
        {
            x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
        }
        y = ClampAngle(y, yMinLimit, yMaxLimit, 90);
        if (_localPlayer == null)
        {
            Vector3 moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection.Normalize();
            transform.rotation = Quaternion.Euler(y, x, 0);
            transform.position += moveDirection * Time.deltaTime * 40;
        }
        else
        {
            Quaternion rot2 = Quaternion.Euler(y, x, 0);
            Vector3 pos2 = rot2 * new Vector3(0.0f, 0.0f, -xoffset) + _localPlayer.pos;
            pos2.y += yoffset;

            RaycastHit h;
            Vector3 plpos = _localPlayer.transform.position;
            Ray r = new Ray(pos2, plpos - pos2);
            if (Physics.Raycast(r, out h, Vector3.Distance(plpos, pos2), 1 << LayerMask.NameToLayer("Level")))
                pos2 = h.point + r.direction.normalized;

            pos = ((pos2 * Time.deltaTime * 5) + (pos)) / (Time.deltaTime * 5 + 1);
            rot = rot2; //Quaternion.Euler(rot.eulerAngles + (rot2.eulerAngles * 10) / 11);
        }

        camera.transform.localPosition = new Vector3(Random.Range(-exp, exp), Random.Range(-exp, exp), Random.Range(-exp, exp));
        exp -= .1f;
        if (exp < 0) exp = 0;
    }
    public float xoffset = 2;
    public float yoffset = 3;
    Vector3 oldpos;
    public float exp;
    public static float ClampAngle(float angle, float min, float max, float clamp)
    {
        if (angle < -clamp)
            angle = -clamp;
        if (angle > clamp)
            angle = clamp;
        return Mathf.Clamp(angle, min, max);
    }


}
[System.Serializable]
public class Decal
{
    public string name;
    public float scale = 1;
    public override string ToString()
    {
        return name;
    }
    public GameObject mesh;
    public Material mat;
}