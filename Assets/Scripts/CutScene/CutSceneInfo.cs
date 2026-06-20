using UnityEngine;


[CreateAssetMenu(fileName = "NewCutsceneInfo", menuName = "Cutscenes/Cutscene Info")]
public class CutSceneInfo : ScriptableObject
{
    public bool RealTimeAnimation = false;
    public bool useDialog = true;

    public float delayTime = 0.0f;

    public float showTime = 0.0f;

    public float fadingTime = 0.0f;

    public bool newScene = false;

    public string SceneID = "";

    public string SpawnID = "";

    public Vector3 SpawnOffset = Vector2.zero;
}
