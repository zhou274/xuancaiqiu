

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TTSDK.UNBridgeLib.LitJson;
using TTSDK;
using StarkSDKSpace;
using System.Collections.Generic;
using UnityEngine.Analytics;


public class GameManager : MonoBehaviour
{
	[Header("Game settings")]
	public float followSpeed = 4f;

	public float yDelta = 0.5f;

	[Space(15f)]
	public GameObject player;

	[Space(15f)]
	public Image barFill;

	public int ballsToChangeColor = 10;

	[Space(15f)]
	public GameObject obstaclePrefab;

	[Space(15f)]
	[Range(0.15f, 0.6f)]
	public float delayBetweenObstacles = 0.4f;

	public float minObstacleSpeed = 3f;

	public float maxObstacleSpeed = 8f;

	public float minAplitude = 0.5f;

	public float maxAmplitude = 2f;

	public float minLeftRightSpeed = 1f;

	public float maxLeftRightSpeed = 7f;

	[Space(5f)]
	public bool spawning;

	private Vector3 destination;

	private GameObject obstacle;

	private Material tempMaterial;

	private int ballIndex;

    public string clickid;
    private StarkAdManager starkAdManager;
    public static GameManager Instance
	{
		get;
		set;
	}

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	private void Start()
	{
		Application.targetFrameRate = 30;
		Physics.gravity = Vector3.zero;
		destination = player.transform.position;
		barFill.fillAmount = 0f;
	}

	private void FixedUpdate()
	{
		if (UIManager.Instance.gameState == GameState.PLAYING && !UIManager.Instance.IsButton())
		{
			destination = UnityEngine.Input.mousePosition;
			destination = Camera.main.ScreenToWorldPoint(new Vector3(destination.x, destination.y, 16.7f));
			player.transform.position = Vector3.Lerp(player.transform.position, new Vector3(destination.x, destination.y + yDelta, 16.7f), followSpeed * Time.deltaTime);
			if (!spawning)
			{
				ScoreManager.Instance.StartCounting();
				spawning = true;
				StartCoroutine(SpawnObstacle(delayBetweenObstacles));
			}
		}
	}

	private IEnumerator SpawnObstacle(float delay)
	{
		while (spawning)
		{
			obstacle = UnityEngine.Object.Instantiate(obstaclePrefab);
			obstacle.transform.position = new Vector3(UnityEngine.Random.Range(-3f, 3f), 30f, 16.7f);
			if (UnityEngine.Random.Range(0, 30) < 10)
			{
				tempMaterial = ColorManager.Instance.backgroundMainMat;
			}
			else
			{
				tempMaterial = ColorManager.Instance.obstacleMat;
			}
			obstacle.GetComponent<Obstacle>().InitOBstacle(UnityEngine.Random.Range(minObstacleSpeed, maxObstacleSpeed), UnityEngine.Random.Range(minAplitude, maxAmplitude), UnityEngine.Random.Range(minLeftRightSpeed, maxLeftRightSpeed), tempMaterial);
			yield return new WaitForSeconds(delayBetweenObstacles);
		}
	}

	public void BallCollected()
	{
		ballIndex++;
		barFill.fillAmount = (float)ballIndex / (float)ballsToChangeColor;
		if (ballIndex == ballsToChangeColor)
		{
			ColorManager.Instance.ChangeColors();
			ballIndex = 0;
			barFill.fillAmount = 0f;
			AudioManager.Instance.PlayEffects(AudioManager.Instance.highscore);
		}
	}

	public void GameOver()
	{
		spawning = false;
		player.gameObject.SetActive(value: false);
		ScoreManager.Instance.StopCounting();
		GameObject[] array = GameObject.FindGameObjectsWithTag("Obstacle");
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] != null)
			{
				UnityEngine.Object.Destroy(array[i]);
			}
		}
		UIManager.Instance.ShowGameOver();
		ScoreManager.Instance.UpdateScoreGameover();
        ShowInterstitialAd("1lcaf5895d5l1293dc",
            () => {
                Debug.LogError("--插屏广告完成--");

            },
            (it, str) => {
                Debug.LogError("Error->" + str);
            });
    }
	public void Continue()
	{
        ShowVideoAd("192if3b93qo6991ed0",
            (bol) => {
                if (bol)
                {

                    spawning = false;
                    player.gameObject.SetActive(true);
                    ScoreManager.Instance.StartCounting();
                    UIManager.Instance.HideGameOver();


                    clickid = "";
                    getClickid();
                    apiSend("game_addiction", clickid);
                    apiSend("lt_roi", clickid);


                }
                else
                {
                    StarkSDKSpace.AndroidUIManager.ShowToast("观看完整视频才能获取奖励哦！");
                }
            },
            (it, str) => {
                Debug.LogError("Error->" + str);
                //AndroidUIManager.ShowToast("广告加载异常，请重新看广告！");
            });
        
	}
	public void ClearScene()
	{
		ScoreManager.Instance.StopCounting();
		spawning = false;
		GameObject[] array = GameObject.FindGameObjectsWithTag("Obstacle");
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] != null)
			{
				UnityEngine.Object.Destroy(array[i]);
			}
		}
		player.gameObject.SetActive(value: true);
		player.transform.position = new Vector3(0f, 2f, 16.7f);
		barFill.fillAmount = 0f;
		ballIndex = 0;
		StopAllCoroutines();
	}

	public void RestartGame()
	{
		ClearScene();
		if (UIManager.Instance.gameState == GameState.PAUSED)
		{
			Time.timeScale = 1f;
		}
		ScoreManager.Instance.ResetCurrentScore();
		UIManager.Instance.ShowGameplay();
	}


    public void getClickid()
    {
        var launchOpt = StarkSDK.API.GetLaunchOptionsSync();
        if (launchOpt.Query != null)
        {
            foreach (KeyValuePair<string, string> kv in launchOpt.Query)
                if (kv.Value != null)
                {
                    Debug.Log(kv.Key + "<-参数-> " + kv.Value);
                    if (kv.Key.ToString() == "clickid")
                    {
                        clickid = kv.Value.ToString();
                    }
                }
                else
                {
                    Debug.Log(kv.Key + "<-参数-> " + "null ");
                }
        }
    }

    public void apiSend(string eventname, string clickid)
    {
        TTRequest.InnerOptions options = new TTRequest.InnerOptions();
        options.Header["content-type"] = "application/json";
        options.Method = "POST";

        JsonData data1 = new JsonData();

        data1["event_type"] = eventname;
        data1["context"] = new JsonData();
        data1["context"]["ad"] = new JsonData();
        data1["context"]["ad"]["callback"] = clickid;

        Debug.Log("<-data1-> " + data1.ToJson());

        options.Data = data1.ToJson();

        TT.Request("https://analytics.oceanengine.com/api/v2/conversion", options,
           response => { Debug.Log(response); },
           response => { Debug.Log(response); });
    }


    /// <summary>
    /// </summary>
    /// <param name="adId"></param>
    /// <param name="closeCallBack"></param>
    /// <param name="errorCallBack"></param>
    public void ShowVideoAd(string adId, System.Action<bool> closeCallBack, System.Action<int, string> errorCallBack)
    {
        starkAdManager = StarkSDK.API.GetStarkAdManager();
        if (starkAdManager != null)
        {
            starkAdManager.ShowVideoAdWithId(adId, closeCallBack, errorCallBack);
        }
    }

    /// <summary>
    /// 播放插屏广告
    /// </summary>
    /// <param name="adId"></param>
    /// <param name="errorCallBack"></param>
    /// <param name="closeCallBack"></param>
    public void ShowInterstitialAd(string adId, System.Action closeCallBack, System.Action<int, string> errorCallBack)
    {
        starkAdManager = StarkSDK.API.GetStarkAdManager();
        if (starkAdManager != null)
        {
            var mInterstitialAd = starkAdManager.CreateInterstitialAd(adId, errorCallBack, closeCallBack);
            mInterstitialAd.Load();
            mInterstitialAd.Show();
        }
    }
}
