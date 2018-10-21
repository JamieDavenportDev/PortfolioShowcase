using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public enum State { Paused, Playing }

    [SerializeField]
    private GameObject[] SpawnPrefabs;

    [SerializeField]
    private GameObject[] Levels;

    [SerializeField]
    private GameObject SpawnClouds;

    [SerializeField]
    private Player PlayerPrefab;

    [SerializeField]
    private Arena Arena;

    [SerializeField]
    private float TimeBetweenSpawns;

    private float CloudDensity;

    [SerializeField]
    private GameObject MegaStar;

    private List<GameObject> mObjects;
    private GameObject levelInstance;
    private Player mPlayer;
    private State mState;
    private float mNextSpawn;
    public GameManager Instance { get; private set; }
    private int vortexTrack;
    private int TotalVortex;
    private int levelNumber;
    private int indexTracker = 0;
    private float levelTimer;
    private int starLimit;
    private bool limitReached;
    private int spawnCounter;
    private bool gameLoaded;

    void Awake()
    {
        mPlayer = Instantiate(PlayerPrefab);
        mPlayer.transform.parent = transform;
        Instance = this;
        CloudDensity = Random.Range(2.0f, 7.0f);
        levelTimer = 0.0f;
        gameLoaded = false;

        ScreenManager.OnNewGame += ScreenManager_OnNewGame;
        ScreenManager.OnExitGame += ScreenManager_OnExitGame;
    }

    void Start()
    {
        Arena.Calculate();
        mPlayer.enabled = false;
        mState = State.Paused;
        
    }

    void SpawnMegaStar(Vector3 Location)
    {
        GameObject mStarInstace = Instantiate(MegaStar, Location, Quaternion.identity);
        mObjects.Add(mStarInstace);
    }

    void LevelComplete()
    {
        //save progress
        //show level interface
        string prefsName = "Level" + levelNumber;
        if (PlayerPrefs.GetFloat(prefsName) == 0.0f || PlayerPrefs.GetFloat(prefsName) > levelTimer)
            PlayerPrefs.SetFloat(prefsName, levelTimer);
        ScreenManager screenScript = FindObjectOfType<ScreenManager>();
        screenScript.Instance.SendMessage("EndGame", levelNumber);
    }

    void LevelFailed()
    {
        ScreenManager screenScript = FindObjectOfType<ScreenManager>();
        screenScript.Instance.SendMessage("LostGame", levelNumber);
        EndGame(levelNumber);
    }

    void StarPlaced()
    {
        //handle star placement here
        vortexTrack--;
        if (vortexTrack == 0)
            LevelComplete();
    }

    void Update()
    {
        if( mState == State.Playing)
        {
            levelTimer += Time.deltaTime;   //Updating level timer
            GameObject textObject = GameObject.Find("Timer");
            Text textComponent = textObject.GetComponent<Text>();
            int minutes = Mathf.FloorToInt(levelTimer / 60F);
            int seconds = Mathf.FloorToInt(levelTimer - minutes * 60);
            int milliseconds = Mathf.FloorToInt((levelTimer * 1000) % 1000);
            textComponent.text = "Time: " + string.Format("{0:0}:{1:00}.{2:0}", minutes, seconds, milliseconds);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            mNextSpawn -= Time.deltaTime;
            CloudDensity -= Time.deltaTime;
            int[] spawnIndex = { 1, 1, 0 };
            int[] spawnCount = { 1, 2, 0 };
            int indexToSpawn;

            if(limitReached)
            {
                if (GameObject.FindGameObjectsWithTag("TypeTwo").Length + ((GameObject.FindGameObjectsWithTag("MegaStar").Length - (TotalVortex - vortexTrack)) * 5) < vortexTrack * 5) // Level is not completable
                    LevelFailed();
            }
            if(mNextSpawn <= 0.0f )
            {
                if (mObjects == null)
                {
                    mObjects = new List<GameObject>();
                }

                indexToSpawn = spawnIndex[indexTracker];
                indexTracker = spawnCount[indexTracker];
                if(!(limitReached && indexToSpawn == 1))
                { 
                    GameObject spawnObject = SpawnPrefabs[indexToSpawn];
                    GameObject spawnedInstance = Instantiate(spawnObject);
                    spawnedInstance.transform.parent = transform;
                    mObjects.Add(spawnedInstance);
                    mNextSpawn = TimeBetweenSpawns;
                }
                if(indexToSpawn == 1)
                {
                    spawnCounter += 1;
                    if (spawnCounter == starLimit)
                        limitReached = true;
                }
            }
            if(CloudDensity <= 0.0f)
            {
                if(mObjects == null)
                {
                    mObjects = new List<GameObject>();
                }

                GameObject spawnedInstance = Instantiate(SpawnClouds);
                mObjects.Add(spawnedInstance);
                CloudDensity = Random.Range(2.0f, 7.0f);
            }

            if (Input.GetKeyUp(KeyCode.Escape))
            {
                mState = State.Paused;
                mPlayer.enabled = false;
                Time.timeScale = 0;    
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if(gameLoaded && Input.GetKeyUp(KeyCode.Escape))
            {
                mPlayer.enabled = true;
                mState = State.Playing;
                Time.timeScale = 1;
            }
        }
    }

    private void ClearGame()
    {
        if (mObjects != null)
        {
            for (int count = 0; count < mObjects.Count; ++count)
            {
                Destroy(mObjects[count]);
            }
            mObjects.Clear();
        }
    }

    private void BeginNewGame(int level)
    {
        ClearGame();
        levelTimer = 0.0f;  // Reset level timer
        levelNumber = level;
        mPlayer.transform.position = new Vector3(0.0f, 0.5f, 0.0f);
        GameObject levelToSpawn = Levels[level];
        levelInstance = Instantiate(levelToSpawn);
        spawnCounter = 0;
        TotalVortex = levelInstance.transform.childCount;
        vortexTrack = TotalVortex;
        mNextSpawn = TimeBetweenSpawns;
        starLimit = TotalVortex * 5;
        starLimit *= 2; // Doubling to make it fairer
        limitReached = false;
        print((TotalVortex * 5) + " required. " + starLimit + " limit."); // REMOVE THIS
        
        mPlayer.enabled = true;
        mState = State.Playing;
        gameLoaded = true;
    }

    private void EndGame(int level)
    {
        Destroy(levelInstance);
        mPlayer.enabled = false;
        Time.timeScale = 1;
        ClearGame();
        mState = State.Paused;
        gameLoaded = false;
    }

    private void ScreenManager_OnNewGame(int level)
    {
        BeginNewGame(level);
    }

    private void ScreenManager_OnExitGame(int level)
    {
        EndGame(level);
    }
}
