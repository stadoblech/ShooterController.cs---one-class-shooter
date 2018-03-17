using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class ShooterController : MonoBehaviour {


    #region global
    float screenHeight;
    float screenWidth;
    int gameScore;
    bool gameOver;
    [Header("Global settings")]
    public float startRespawnTime;
    public float minRandomTimer;
    public float maxRandomTimer;
    List<Vector3> randomPointsOnScreen;
    List<float> randomTimers;
    #endregion

    #region UI
    [Header("UI")]
    public Text scoreTextUI;
    public Text endGameInfoText;
    public GameObject cursorPrefab;
    #endregion

    #region player
    [Header("PlayerSettings")]
    public float playerMovementSpeed;
    public GameObject playerShot;
    public float playerShotMovementSpeed;
    public AudioSource playerDeathSound;

    Transform playerTransform;
    Collider2D playerCollider;
    #endregion

    #region enemies init
    [Header("Enemies init")]
    public List<GameObject> enemiesPrefabs;
    List<Transform> enemiesOnScene;
    #endregion


    #region enemy spawner
    List<Transform> spawners;
    #endregion

    #region follower
    [Header("Enemy follower")]
    public float followerSpeed;
    #endregion

    #region turret
    [Header("Enemy turret")]
    public float turretMoveSpeed;
    public float turretMissileSpeed;
    public float turretRotateSpeed;
    public GameObject turretMissilePrefab;
    List<Transform> activeTurretMissiles;
    #endregion



    List<Transform> playerShotList;

    void Start () {
        initialize();
        StartCoroutine(spawnEnemy());
    }
	
	void Update () {
        overallUpdate();
        disposeObjects();
        updateUI();
        updatePlayer();
        updateEnemies();
        checkForRestartGame();

        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }


    void initialize()
    {
        /// overall
        gameScore = 0;

        screenHeight = Camera.main.orthographicSize;
        screenWidth = screenHeight * Camera.main.aspect;
        randomPointsOnScreen = new List<Vector3>();
        randomTimers = new List<float>();


        /// player
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        playerCollider = playerTransform.GetComponent<Collider2D>();

        playerShotList = new List<Transform>();
        
        /// spawn positions
        spawners = new List<Transform>();
        foreach(Transform t in GameObject.FindGameObjectWithTag("Spawner").transform)
        {
            spawners.Add(t);
        }

        ///enemies
        enemiesOnScene = new List<Transform>();
        activeTurretMissiles = new List<Transform>();

        Cursor.visible = false;
    }

    #region ovreall
    void overallUpdate()
    {
        clampRandomMovePoints();
        clampRandomTimers();
    }

    bool clampTransform(Transform t)
    {
        if (t.position.x > screenWidth)
        {
            t.position = new Vector3(screenWidth, t.position.y);
            return true;
        }

        if (t.position.x < -screenWidth)
        {
            t.position = new Vector3(-screenWidth, t.position.y);
            return true;
        }
        if (t.position.y > screenHeight)
        {
            t.position = new Vector3(t.position.x, screenHeight);
            return true;
        }
        if (t.position.y < -screenHeight)
        {
            t.position = new Vector3(t.position.x, -screenHeight);
            return true;
        }
        return false;
            
    }

    void disposeObjects()
    {
        playerShotList.RemoveAll(e => e == null);
        enemiesOnScene.RemoveAll(e => e == null);
        activeTurretMissiles.RemoveAll(e => e == null);
    }

    /// <summary>
    /// checking if transform is hitted by player shot
    /// </summary>
    /// <param name="transformToCheck">transform to check</param>
    void checkForTransformKill(Transform transformToCheck)
    {
        foreach (Transform plShot in playerShotList)
        {
            if (transformToCheck.GetComponent<Collider2D>().Distance(plShot.GetComponent<Collider2D>()).isOverlapped)
            {
                gameScore++;
                Destroy(transformToCheck.gameObject);
            }
        }
    }

    /// <summary>
    /// check for killing player
    /// </summary>
    /// <param name="enemy">transform which is checking for killing player</param>
    void checkForDeath(Transform enemy)
    {
        if (enemy.GetComponent<Collider2D>().Distance(playerTransform.GetComponent<Collider2D>()).isOverlapped)
        {
            gameOver = true;
            playerTransform.gameObject.SetActive(false);
            clearAll();
        }
    }

    void clearAll()
    {
        playerShotList.ForEach(e => Destroy(e.gameObject));
        enemiesOnScene.ForEach(e => Destroy(e.gameObject));
        activeTurretMissiles.ForEach(e => Destroy(e.gameObject));
    }

    void checkForRestartGame()
    {
        if (gameOver && Input.GetMouseButtonDown(1))
        {
            gameOver = false;
            playerTransform.position = new Vector3(0, 0);
            playerTransform.gameObject.SetActive(true);
            gameScore = 0;
        }
    }

    void clampRandomMovePoints()
    {
        if(randomPointsOnScreen.Count < enemiesOnScene.Count)
        {
            randomPointsOnScreen.Add(pickRandomPosOnScreen());
        }
    }

    void clampRandomTimers()
    {
        if (randomTimers.Count < enemiesOnScene.Count)
        {
            randomTimers.Add(Random.Range(minRandomTimer,maxRandomTimer));
        }

        for(int i = 0;i<randomTimers.Count;i++)
        {
            if (randomTimers[i] <= 0)
                randomTimers[i] = Random.Range(minRandomTimer, maxRandomTimer);
        }
    }

    Vector3 pickRandomPosOnScreen()
    {
        float randomX = Random.Range(-screenWidth,screenWidth);
        float randomY = Random.Range(-screenHeight,screenHeight);
        return new Vector3(randomX, randomY);
    }

    #endregion

    #region UI

    void updateUI()
    {
        updateScoreText();
        updateEndGameScreen();
        cursorPositionUpdate();
    }

    void updateScoreText()
    {
        scoreTextUI.text = "Score: " + gameScore;
    }

    void updateEndGameScreen()
    {
        if (gameOver)
        {
            endGameInfoText.gameObject.SetActive(true);
            endGameInfoText.text = "Game Over!\nYour score: " + gameScore + "\nPress right mouse button to restart";
        }else
        {
            endGameInfoText.gameObject.SetActive(false);
        }
    }

    void cursorPositionUpdate()
    {
        Vector3 cursorPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y));
        cursorPos = new Vector3(cursorPos.x,cursorPos.y,0);
        cursorPrefab.transform.position = cursorPos;
    }
    #endregion

    #region player
    void updatePlayer()
    {
        if (gameOver)
            return;
        playerLookAtMouse();
        movePlayer();
        clampTransform(playerTransform);
        playerShootingMissiles();
        playerShotUpdate();
    }

    void playerLookAtMouse()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (mousePos - (Vector2)playerTransform.position).normalized;
        playerTransform.up = dir;
    }

    void movePlayer()
    {
        playerTransform.position += new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * Time.deltaTime * playerMovementSpeed;
    }

    

    void playerShootingMissiles()
    {
        if(Input.GetMouseButtonDown(0))
        {
            GameObject o = Instantiate(playerShot, playerTransform.position, playerTransform.rotation);
            o.tag = "PlayerShot";
            playerShotList.Add(o.transform);
            playSound(o.transform);
        }
    }

    void playerShotUpdate()
    {
        foreach (Transform t in playerShotList)
        {
            if(clampTransform(t))
            {
                Destroy(t.gameObject);
                continue;
            }
            t.position += t.up * playerShotMovementSpeed * Time.deltaTime;
        }
    }
    #endregion

    #region enemies overall
    void updateEnemies()
    {
        followerUpdate();
        turretUpdate();
    }
    #endregion

    #region enemy follower 
    void followerUpdate()
    {
        foreach(Transform t in enemiesOnScene)
        {
            if(t.tag == "Follower")
            {
                t.Rotate(0,0,30*Time.deltaTime);
                t.position = Vector3.MoveTowards(t.position,playerTransform.position,followerSpeed*Time.deltaTime);
                checkForTransformKill(t);
                checkForDeath(t);
            }
        }
    }
    #endregion

    #region enemy turret
    void turretUpdate()
    {
        turretMovementUpdate();
        turretShotUpdate();
        turretMissilesUpdate();
        rotateTurret();
    }

    void turretMovementUpdate()
    {
        Vector3 posToMove = Vector3.zero;
        for (int i = 0; i < enemiesOnScene.Count; i++)
        {
            if (enemiesOnScene[i].tag == "Turret")
            {
                enemiesOnScene[i].position = Vector3.MoveTowards(enemiesOnScene[i].position, randomPointsOnScreen[i], turretMoveSpeed * Time.deltaTime);
                checkForDeath(enemiesOnScene[i]);
                checkForTransformKill(enemiesOnScene[i]);
            }
        }
    }

    void turretShotUpdate()
    {
        for (int i = 0; i < enemiesOnScene.Count; i++)
        {
            if(enemiesOnScene[i].tag == "Turret")
            {
                randomTimers[i] -= Time.deltaTime;
                if(randomTimers[i] <= 0f)
                {
                    foreach (Transform t in enemiesOnScene[i].transform)
                    {
                        if (t.name != "no")
                        {
                            GameObject o = Instantiate(turretMissilePrefab, t.position, t.rotation);
                            activeTurretMissiles.Add(o.transform);

                        }
                            
                    }
                }
            }
        }
    }

    void rotateTurret()
    {
        foreach(Transform t in enemiesOnScene)
        {
            if (t.tag == "Turret")
            {
                t.Rotate(0,0,turretRotateSpeed*Time.deltaTime);
            }
        }
    }

    void turretMissilesUpdate()
    {
        foreach (Transform t in activeTurretMissiles)
        {
            t.position += t.up * turretMissileSpeed * Time.deltaTime;
            checkForTransformKill(t);
            checkForDeath(t);
        }
    }
    #endregion

    #region spawner
    IEnumerator spawnEnemy()
    {
        yield return new WaitForSeconds(startRespawnTime);
        if (!gameOver)
        {
            Vector3 spawnPos = spawners[Random.Range(0, spawners.Count)].position;
            GameObject enemy = Instantiate(enemiesPrefabs[Random.Range(0, enemiesPrefabs.Count)], spawnPos, Quaternion.identity);
            enemiesOnScene.Add(enemy.transform);
        }
        StartCoroutine(spawnEnemy());
    }
    #endregion

    #region sound effects
    
    void playSound(Transform t)
    {
        AudioSource source = t.GetComponent<AudioSource>();
        int sampleFreq = 4400;
        float frequency = 440;

        float[] samples = new float[44000];
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = Mathf.PingPong(i * 2f * frequency / sampleFreq, 1) * 2f - 1f;
        }
        AudioClip clip = AudioClip.Create("Test", samples.Length, 1, sampleFreq, false);
        clip.SetData(samples, 0);
        source.clip = clip;
        source.Play();
    }

    #endregion
}
