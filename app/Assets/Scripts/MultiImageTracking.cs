using GraphQlClient.Core;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class MultiImageTracking : MonoBehaviour
{
    public GraphApi openTalkReference;
    API api;

    [SerializeField]
    TMP_Text description;
    [SerializeField]
    AudioSource audioSource;
    [SerializeField]
    Button closeBtn;
    [SerializeField]
    RawImage rawImage;

    ARTrackedImageManager aRTrackedImageManager;
    Dictionary<string, GameObject> mPrefabs = new Dictionary<string, GameObject>();
    Dictionary<string, GameObject> spawnedPrefabs = new Dictionary<string, GameObject>();

    string defaultCharacter = null;
    string fallbackCharacter = "hiyori";
    string prevDetectedImage = null;

    void Awake()
    {
        aRTrackedImageManager = GetComponent<ARTrackedImageManager>();
        api = new API(description, audioSource, openTalkReference, rawImage, closeBtn);
    }

    async void Start()
    {
        // 角色
        VirutalConf conf = await api.getVirtualConf();
        if (conf != null) defaultCharacter = conf.character;

        // Welcome
        DeviceResponse response = await api.getDeviceResponse("welcome_reply");
        if (response != null)
        {
            api.controll(null, response.messages);
        }

        // 動態新增辨識圖片
        Entry resp = await api.getEntry("text", "拉麵圖片辨識");
        if (resp.result.Length > 0)
        {
            // image, quick_reply, image, quick_reply...
            var tempImageUrl = "";
            foreach (TypeValueMixin e in resp.result[0].send)
            {
                if (e.type == "image")
                {
                    tempImageUrl = e.value;
                }
                else if (e.type == "quick_reply")
                {
                    await AddImage(e.value, tempImageUrl);
                }
            }
        }

        mPrefabs.Add("hiyori", Resources.Load("hiyori") as GameObject);
        mPrefabs.Add("hibiki", Resources.Load("hibiki") as GameObject);
    }

    async void UpdateInfo(ARTrackedImage trackedImage)
    {
        if (trackedImage.trackingState == TrackingState.Tracking && prevDetectedImage != trackedImage.referenceImage.name)
        {
            // 保持最後的角色在畫面上
            if (prevDetectedImage != null)
            {
                spawnedPrefabs[prevDetectedImage].SetActive(false);
            }
            spawnedPrefabs[trackedImage.referenceImage.name].SetActive(true);
            prevDetectedImage = trackedImage.referenceImage.name;
            description.text = trackedImage.referenceImage.name;

            if (api.queue.Count == 0)
            {
                Entry entry = await api.getEntry("text", trackedImage.referenceImage.name);
                api.controll(spawnedPrefabs[trackedImage.referenceImage.name], entry.result[0].send);
            }
        }
    }

    void Update()
    {
        api.Update();
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // 圖片變化
        foreach (var trackedImage in eventArgs.added)
        {
            OnImagesChanged(trackedImage);
        }

        foreach (var trackedImage in eventArgs.updated)
        {
            UpdateInfo(trackedImage);
        }
    }
    async Task<UnityWebRequest> AddImage(string imageName, string imageURL)
    {
        // 動態新增辨識圖片
        UnityWebRequest request = await HttpHandler.PostTextureAsync(imageURL);
        var imageToAdd = DownloadHandlerTexture.GetContent(request);

        if (aRTrackedImageManager.referenceLibrary is MutableRuntimeReferenceImageLibrary mutableLibrary)
        {
            mutableLibrary.ScheduleAddImageWithValidationJob(
                imageToAdd,
                imageName,
                0.1f /* 10 cm */);

            if (defaultCharacter != null)
            {
                mPrefabs.Add(imageName, Resources.Load(defaultCharacter) as GameObject);
            }
            else
            {
                mPrefabs.Add(imageName, Resources.Load(fallbackCharacter) as GameObject);
            }
        }
        return request;
    }
    void OnImagesChanged(ARTrackedImage referenceImage)
    {
        // 辨識圖片與對應的角色初始化
        GameObject spawnedPrefab = Instantiate(mPrefabs[referenceImage.referenceImage.name], referenceImage.transform);
        spawnedPrefab.name = referenceImage.referenceImage.name;
        spawnedPrefabs.Add(referenceImage.referenceImage.name, spawnedPrefab);
    }
    void OnEnable()
    {
        aRTrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }
    void OnDisable()
    {
        aRTrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }
}
