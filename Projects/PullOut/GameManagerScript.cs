using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GoogleMobileAds.Api;

// Handles most gameplay behaviour
public class GameManagerScript : MonoBehaviour {

    // Prefab holder for sperm objects and main egg
    public GameObject Sperm;
    private GameObject Egg;

    // -- Game variables --
    [SerializeField]
    // # of points on the circle = number spawned each wave
    private int numberOfSides;
    [SerializeField]
    // Distance spawned from egg
    private float radius;
    [SerializeField]
    // Time between waves
    private float TimeBetweenSpawns;
    [SerializeField]
    // Speed limit towards egg
    private float MaxSpeed;
    [SerializeField]
    // Spawn per wave max limit
    private float maxSides;

    // -- UI Elements --
    [SerializeField]
    Text ScoreText;
    [SerializeField]
    Text ScoreTextBack;

    // Materials for identifying "active" sperm
    public Material[] materials;
    public AudioSource soundSource;

    // Pause button, blocking panel, and pause state
    public Button pauseButton;
    public GameObject pausePanel;
    private bool paused;

    // -- Active game variables --
    // Current timer to next wave
    private float timeToNextSpawn;
    // Coords of player touch
    private Vector3 touchPosWorld;
    // "Active" target sperm
    private GameObject TargetSperm = null;
    // 2D list of active sperm - List containing waves as lists of sperm (gameobjects)
    private List<List<GameObject>> ActiveWaves = new List<List<GameObject>>();
    // Current game score
    private int GameScore;
    // Current waves progress
    private int WaveCount;
    // Current speed to set to new sperm
    public float SpeedToSpawn;
    // Baseline pitch for altering squish sound
    private float startingPitch;

    // Interstitial Ad Variable - used for handling ads
    InterstitialAd interstitial;

    // Takes degrees along circle and returns it as coordinates
    void degreeToCoord(ref Vector3 toSpawnLocation, float a) {
        toSpawnLocation.x = radius * Mathf.Cos(a * (Mathf.PI / 180.0f)) + Egg.transform.position.x;
        toSpawnLocation.y = radius * Mathf.Sin(a * (Mathf.PI / 180.0f)) + Egg.transform.position.y;
        toSpawnLocation.z = -3.0f;
    }

    // Calculates coordinates for spawning sperm, adds variance, spawns wave, and pushes onto ActiveWaves
    void calculateCircle()    {
        // Degrees between each spawn point
        float amount = 360.0f / numberOfSides;
        // Random variance to rotate circle
        float variance = Random.Range(0.0f, 180.0f);
        // List of new wave objects
        List<GameObject> NewWave = new List<GameObject>();

        // Iterate for each spawn, starting at 1 so as not to times by 0 later
        for (int i = 1; i <= numberOfSides; i++)
        { 
            Vector3 toSpawnLocation = new Vector2();
            // Pass new vector3 and degree along circle + variance
            degreeToCoord(ref toSpawnLocation, i * amount + variance);
            // Instantiate a new object using prefab
            GameObject newSperm = Instantiate(Sperm);
            // Set the position, tag, and movement speed of new sperm
            newSperm.transform.position = toSpawnLocation;
            newSperm.tag = "Sperm";
            newSperm.GetComponent<MoveToEgg>().mSpeed = SpeedToSpawn;
            // Get and Set render material to inactive
            Renderer rend = newSperm.GetComponent<Renderer>();
            rend.material = materials[0];
            // Add to new wave
            NewWave.Add(newSperm);
        }
        // Add new wave list to active wave list
        ActiveWaves.Add(NewWave);
        // Reset timer
        timeToNextSpawn = TimeBetweenSpawns;
    }

    // Picks a sperm to be new active target
    void newTarget()
    {
        // Get a random int in range of active wave remaining
        int newTarget = Random.Range(0, ActiveWaves[0].Count - 1);
        // Set the target variable to new target index
        TargetSperm = ActiveWaves[0][newTarget];
        // Get and update renderer to identify sperm to player
        Renderer rend = TargetSperm.GetComponent<Renderer>();
        rend.material = materials[1];
    }

    // Shows an interstitial Advert
    public void showInterstitialAd()
    {
        if(interstitial.IsLoaded()) // If it's ready...
        {
            interstitial.Show();    // ...show it
        }
    }

    // Requests a new advert from server
    private void RequestInterstitialAds()
    {
        string adID = "ca-app-pub-################/########"; // I've hidden my ID for example code
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
        //    .AddTestDevice("###############") // Hidden test device ID
        //.Build();

        //PRODUCTION
        AdRequest request = new AdRequest.Builder().Build();

        interstitial.OnAdClosed += Interstitial_OnAdClosed;

        interstitial.LoadAd(request);
    }

    // Handles when ad is closed
    private void Interstitial_OnAdClosed(object sender, System.EventArgs e)
    {
        // Nothing needed here for me, no rewards etc
    }

    // Pauses and unpauses the game using timescale and pause panel
    void PauseHandler()
    {
        if (paused)
            Time.timeScale = 1;
        else
            Time.timeScale = 0;

        paused = !paused;               // Invert state
        pausePanel.SetActive(paused);   // Use state to set panel state
    }

    // Use this for initialization
    void Start () {
        // Request an ad from server
        RequestInterstitialAds();
        // Set Egg tracker
        Egg = GameObject.Find("Egg");
        // Setup a wave
        calculateCircle();
        // Select a target
        newTarget();
        // Reset variables
        GameScore = 0;
        WaveCount = 0;
        startingPitch = soundSource.pitch;
        paused = false;
        // Setup pause event listener
        pauseButton.onClick.AddListener(PauseHandler);
        // Setting up and incrementing variable tracking games since last advert
        if (!PlayerPrefs.HasKey("SinceLastAd"))
            PlayerPrefs.SetInt("SinceLastAd", 1);
        else
            PlayerPrefs.SetInt("SinceLastAd", PlayerPrefs.GetInt("SinceLastAd") + 1);

    }

    // Handles when the game ends
    public void EndGame()
    {
        int previousScore;
        // Try to retrieve previous score, if can't then create it at 0
        if (PlayerPrefs.HasKey("HighScore"))
        {
            previousScore = PlayerPrefs.GetInt("HighScore");
        }
        else
        {
            PlayerPrefs.SetInt("HighScore", 0);
            previousScore = 0;
        }
        // Comparing new score with previous best
        if(GameScore > previousScore)
        {
            // If new score is higher then set it to highscore
            PlayerPrefs.SetInt("HighScore", GameScore);
        }
        // Checking for games played without an ad
        if (PlayerPrefs.GetInt("SinceLastAd") >= 3)
        {
            // Running an ad and resetting tracker
            showInterstitialAd();
            PlayerPrefs.SetInt("SinceLastAd", 0);
        }
        // Returning to main menu
        SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
        
    }

	// Update is called once per frame
	void Update () {

        // Update UI text
        ScoreText.text = GameScore.ToString();
        ScoreTextBack.text = ScoreText.text;

        // Update timer and if complete, spawn new wave
        timeToNextSpawn -= Time.deltaTime;
        if (timeToNextSpawn <= 0.0f)
            calculateCircle();  // Spawning new wave

        //We check if we have more than one touch happening.
        //We also check if the first touches phase is Ended (that the finger was lifted)
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            //We transform the touch position into word space from screen space and store it.
            touchPosWorld = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);
            Vector3 touchPosWorld3D = new Vector3(touchPosWorld.x, touchPosWorld.y, touchPosWorld.z);

            // Raycast from the touch point to find it collision with active sperm
            RaycastHit hit;
            if (Physics.Raycast(touchPosWorld3D, Camera.main.transform.forward, out hit))
            {
                if (hit.collider.gameObject == TargetSperm) // If the collider is the target sperm
                { 
                    foreach (GameObject obj in ActiveWaves[0])  // Loop through active wave
                    {
                        if (obj == hit.collider.gameObject) // Check if its the collided object
                        {
                            ActiveWaves[0].Remove(obj); // Remove it from the wave list
                            soundSource.pitch = startingPitch + (Random.Range(-0.2f, 1.0f)); // Calculate a varied pitch
                            soundSource.Play(); // Play the sound

                            if (ActiveWaves[0].Count == 0) // If the wave is completed
                            {
                                ActiveWaves.RemoveAt(0); // Remove the first wave list from the list of active waves                               
                                WaveCount++; // Increment waves completed tracker
                                // Every three waves increases new sperm move speed
                                if (WaveCount % 3 == 0 && SpeedToSpawn < MaxSpeed)
                                    SpeedToSpawn += 0.25f;
                                // Every five waves decrease the time between waves
                                if (WaveCount % 5 == 0 && TimeBetweenSpawns > 1.0f)
                                    TimeBetweenSpawns -= 0.2f;
                                // Every 10 waves increase spawn per wave amount
                                if (WaveCount % 10 == 0 && numberOfSides < maxSides)
                                    numberOfSides++;
                            }         
                            // Increase score                  
                            GameScore++;
                            break;
                        }
                    }
                    // Delete the sperm object now we're done with it
                    Destroy(hit.collider.gameObject);
                    // If there are no active waves then save the player waiting and spawn another
                    if (ActiveWaves.Count == 0)
                        calculateCircle();
                    newTarget();                   
                }
            }
        }
    }
}
