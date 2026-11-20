using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    const string BaseScene = "Base";
    const string EnemyScene = "EnemyBase";
    static readonly string[] LimbNames = { "LeftHand", "RightHand", "LeftFoot", "RightFoot" };

    public static GameManager Instance { get; private set; }

    [SerializeField] EnemyDie[] enemyPrefabs = System.Array.Empty<EnemyDie>();
    [SerializeField] WebLink webLinkPrefab;

    readonly List<EnemyDie> available = new List<EnemyDie>();
    readonly List<GameObject> captured = new List<GameObject>();
    readonly List<WebLink> captiveLinks = new List<WebLink>();
    EnemyDie activeEnemy;
    EnemyDie activePrefab;
    Transform captivePosition;
    GlitchSequence captiveGlitch;
    GameObject displayedCaptive;
    int captiveIndex;
    bool loading;

    public IReadOnlyList<GameObject> Captured => captured;
    public bool CanChangeEnemy => !loading && captivePosition != null && captured.Count > 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        foreach (var enemy in enemyPrefabs)
            if (enemy != null && enemy.CapturedPrefab != null && !available.Contains(enemy))
                available.Add(enemy);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance != this)
            return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    public void TravelTo(string sceneName)
    {
        if (loading || SceneManager.GetActiveScene().name == sceneName)
            return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Impossible de charger la scène {sceneName}.", this);
            return;
        }

        loading = true;
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
    }

    public void Capture(EnemyDie enemy)
    {
        if (enemy == null || enemy != activeEnemy || activePrefab == null)
            return;
        if (!captured.Contains(enemy.CapturedPrefab))
            captured.Add(enemy.CapturedPrefab);
        available.Remove(activePrefab);
        activeEnemy = null;
        activePrefab = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        loading = false;
        activeEnemy = null;
        activePrefab = null;
        captivePosition = null;
        captiveGlitch = null;
        displayedCaptive = null;
        captiveLinks.Clear();

        bool encounter = scene.name == EnemyScene;
        if (encounter ? available.Count == 0 : scene.name != BaseScene || captured.Count == 0)
            return;

        Transform position = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "EnemyPos")
                position = root.transform;
            else if (!encounter && root.name == "Glitch")
                captiveGlitch = root.GetComponent<GlitchSequence>();
        }

        if (position == null)
        {
            Debug.LogError($"Repère EnemyPos introuvable dans {scene.name}.", this);
            return;
        }

        if (encounter)
        {
            activePrefab = available[Random.Range(0, available.Count)];
            activeEnemy = Instantiate(activePrefab, position.position, position.rotation);
        }
        else
        {
            captivePosition = position;
            captiveIndex = Random.Range(0, captured.Count);
            DisplayCaptive();
        }
    }

    public void ChangeEnemy()
    {
        if (!CanChangeEnemy)
            return;

        bool hasNextCaptive = captured.Count > 1;
        if (captiveGlitch != null && captiveGlitch.isActiveAndEnabled)
            captiveGlitch.TryPlay(hasNextCaptive ? DisplayNextCaptive : null);
        else if (hasNextCaptive)
            DisplayNextCaptive();
    }

    void DisplayNextCaptive()
    {
        if (loading || captivePosition == null)
            return;

        captiveIndex = (captiveIndex + 1) % captured.Count;
        DisplayCaptive();
    }

    void DisplayCaptive()
    {
        foreach (var web in captiveLinks)
        {
            if (web == null)
                continue;
            web.gameObject.SetActive(false);
            Destroy(web.gameObject);
        }
        captiveLinks.Clear();

        if (displayedCaptive != null)
        {
            displayedCaptive.SetActive(false);
            Destroy(displayedCaptive);
        }

        var prefab = captured[captiveIndex];
        displayedCaptive = Instantiate(prefab, captivePosition.position, captivePosition.rotation, captivePosition);
        var limbs = displayedCaptive.GetComponentsInChildren<Transform>(true);
        foreach (var name in LimbNames)
        {
            var anchor = captivePosition.Find(name);
            var limb = System.Array.Find(limbs, member => member.name == name);
            if (anchor == null || limb == null || webLinkPrefab == null)
            {
                Debug.LogWarning($"Impossible de relier {name} pour {prefab.name}.", this);
                continue;
            }

            var web = Instantiate(webLinkPrefab, captivePosition);
            web.name = "WebLink_" + name;
            web.start = limb;
            web.end = anchor;
            captiveLinks.Add(web);
        }
    }
}
