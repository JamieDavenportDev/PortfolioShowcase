using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GoogleMobileAds.Api;

public class GameManagerScript : MonoBehaviour {

    public GameObject Sperm;
    private GameObject Egg;

    [SerializeField]
    private int numberOfSides;
    [SerializeField]
    private float radius;
    [SerializeField]
    private float TimeBetweenSpawns;
    [SerializeField]
    Text ScoreText;
    [SerializeField]
    Text ScoreTextBack;
    [SerializeField]
    private float MaxSpeed;
    [SerializeField]
    private float maxSides;

    public Material[] materials;
    public AudioSource soundSource;
    public Button pauseButton;
    public GameObject pausePanel;
    private bool paused;

    private float timeToNextSpawn;
    private Vector3 touchPosWorld;
    private GameObject TargetSperm = null;
    private List<List<GameObject>> ActiveWaves = new List<List<GameObject>>();
    private int GameScore;
    private int WaveCount;
    public float SpeedToSpawn;
    private float startingPitch;

    InterstitialAd interstitial;

    void degreeToCoord(ref Vector3 toSpawnLocation, float a) {
        toSpawnLocation.x = radius * Mathf.Cos(a * (Mathf.PI / 180.0f)) + Egg.transform.position.x;
        toSpawnLocation.y = radius * Mathf.Sin(a * (Mathf.PI / 180.0f)) + Egg.transform.position.y;
        toSpawnLocation.z = -3.0f;
    }

    void calculateCircle()    {
        float amount = 360.0f / numberOfSides;
        float variance = Random.Range(0.0f, 180.0f);
        List<GameObject> NewWave = new List<GameObject>();
        for (int i = 1; i <= numberOfSides; i++)
        { 
            Vector3 toSpawnLocation = new Vector2();
            degreeToCoord(ref toSpawnLocation, i * amount + variance);
            GameObject newSperm = Instantiate(Sperm);
            newSperm.transform.position = toSpawnLocation;
            newSperm.tag = "Sperm";
            newSperm.GetComponent<MoveToEgg>().mSpeed = SpeedToSpawn;
            Renderer rend = newSperm.GetComponent<Renderer>();
            rend.material = materials[0];
            NewWave.Add(newSperm);
        }
        ActiveWaves.Add(NewWave);
        timeToNextSpawn = TimeBetweenSpawns;
    }

    void newTarget()
    {
        int newTarget = Random.Range(0, ActiveWaves[0].Count - 1);
        TargetSperm = ActiveWaves[0][newTarget];
        Renderer rend = TargetSperm.GetComponent<Renderer>();
        rend.material = materials[1];
    }

    public void showInterstitialAd()
    {
        if(interstitial.IsLoaded())
        {
            interstitial.Show();
        }
    }

    private void RequestInterstitialAds()
    {
        string adID = "ca-app-pub-2084432247330548/6755333713";
#if UNITY_ANDROID
        string adUnitId = adID;
#elif UNITY_IOS
        string adUnitId = adID;
#else
        string adUnitId = adID;
#endif

        interstitial = new InterstitialAd(adUnitId);

        //TESTING
        //AdRequest request = new AdRequest.Builder()
        //    .AddTestDevice(AdRequest.TestDeviceSimulator)
        //    .AddTestDevice("355847052177917")
        //.Build();

        //PRODUCTION
        AdRequest request = new AdRequest.Builder().Build();

        interstitial.OnAdClosed += Interstitial_OnAdClosed;

        interstitial.LoadAd(request);
    }

    private void Interstitial_OnAdClosed(object sender, System.EventArgs e)
    {
        //Handles when ad is closed
        
    }

    void PauseHandler()
    {
        if (paused)
            Time.timeScale = 1;
        else
            Time.timeScale = 0;

        paused = !paused;
        pausePanel.SetActive(paused);
    }

    // Use this for initialization
    void Start () {
        RequestInterstitialAds();
        Egg = GameObject.Find("Egg");
        calculateCircle();
        newTarget();
        GameScore = 0;
        WaveCount = 0;
        startingPitch = soundSource.pitch;
        paused = false;
        pauseButton.onClick.AddListener(PauseHandler);
        if (!PlayerPrefs.HasKey("SinceLastAd"))
            PlayerPrefs.SetInt("SinceLastAd", 1);
        else
            PlayerPrefs.SetInt("SinceLastAd", PlayerPrefs.GetInt("SinceLastAd") + 1);

    }

    public void EndGame()
    {
        int previousScore;
        if (PlayerPrefs.HasKey("HighScore"))
        {
            previousScore = PlayerPrefs.GetInt("HighScore");
        }
        else
        {
            PlayerPrefs.SetInt("HighScore", 0);
            previousScore = 0;
        }
        if(GameScore > previousScore)
        {
            PlayerPrefs.SetInt("HighScore", GameScore);
        }
        if (PlayerPrefs.GetInt("SinceLastAd") >= 3)
        {
            showInterstitialAd();
            PlayerPrefs.SetInt("SinceLastAd", 0);
        }
        SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
        
    }

	// Update is called once per frame
	void Update () {

        ScoreText.text = GameScore.ToString();
        ScoreTextBack.text = ScoreText.text;

        timeToNextSpawn -= Time.deltaTime;
        if (timeToNextSpawn <= 0.0f)
            calculateCircle();

        //We check if we have more than one touch happening.
        //We also check if the first touches phase is Ended (that the finger was lifted)
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            //We transform the touch position into word space from screen space and store it.
            touchPosWorld = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);

            Vector3 touchPosWorld3D = new Vector3(touchPosWorld.x, touchPosWorld.y, touchPosWorld.z);

            RaycastHit hit;
            if (Physics.Raycast(touchPosWorld3D, Camera.main.transform.forward, out hit))
            {
                if (hit.collider.gameObject == TargetSperm)
                { 
                    foreach (GameObject obj in ActiveWaves[0])
                    {
                        if (obj == hit.collider.gameObject)
                        {
                            ActiveWaves[0].Remove(obj);
                            soundSource.pitch = startingPitch + (Random.Range(-0.2f, 1.0f));
                            soundSource.Play();
                            if (ActiveWaves[0].Count == 0)
                            {
                                ActiveWaves.RemoveAt(0);                                
                                WaveCount++;
                                if (WaveCount % 3 == 0 && SpeedToSpawn < MaxSpeed)
                                    SpeedToSpawn += 0.25f;
                                if (WaveCount % 5 == 0 && TimeBetweenSpawns > 1.0f)
                                    TimeBetweenSpawns -= 0.2f;
                                if (WaveCount % 10 == 0 && numberOfSides < maxSides)
                                    numberOfSides++;
                            }                           
                            GameScore++;
                            break;
                        }
                    }
                    Destroy(hit.collider.gameObject);
                    if (ActiveWaves.Count == 0)
                        calculateCircle();
                    newTarget();                   
                }
            }
        }
    }
}
