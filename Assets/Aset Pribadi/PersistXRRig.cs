using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistXRRig : MonoBehaviour
{
    private static PersistXRRig instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Cari object bernama "PlayerSpawnPoint" di scene baru
        GameObject spawnObj = GameObject.Find("PlayerSpawnPoint");

        if (spawnObj != null)
        {
            transform.position = spawnObj.transform.position;
            transform.rotation = spawnObj.transform.rotation;
            Debug.Log("[PersistXRRig] Player spawn di: " + scene.name);
        }
        else
        {
            Debug.LogWarning("[PersistXRRig] PlayerSpawnPoint tidak ditemukan di scene: " + scene.name);
        }
    }
}