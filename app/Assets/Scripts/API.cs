using GraphQlClient.Core;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class API
{
    TMP_Text description;
    AudioSource audioSource;
    RawImage rawImage;
    Button closeBtn;
    GraphApi openTalkReference;

    Animator animator;

    public Queue<TypeValueMixin> queue = new Queue<TypeValueMixin>();
    Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();
    Queue<Texture2D> images = new Queue<Texture2D>();
    bool isBusy = false;

    public API(TMP_Text d, AudioSource a, GraphApi reference, RawImage raw, Button btn)
    {
        description = d;
        audioSource = a;
        openTalkReference = reference;
        rawImage = raw;
        closeBtn = btn;

        rawImage.enabled = false;
        closeBtn.gameObject.SetActive(false);
        closeBtn.onClick.AddListener(NextImage);
    }

#nullable enable
    public async Task<VirutalConf?> getVirtualConf()
#nullable disable
    {
        UnityWebRequest confRequest = await openTalkReference.Post("GetVirtualCharacter", GraphApi.Query.Type.Query);
        VirtualConfResp resp = JsonConvert.DeserializeObject<VirtualConfResp>(confRequest.downloadHandler.text);

        if (resp.data.virtual_idol_conf.Length > 0)
        {
            return resp.data.virtual_idol_conf[0];
        }

        return null;
    }

#nullable enable
    public async Task<DeviceResponse?> getDeviceResponse(string responseType)
#nullable disable
    {
        GraphApi.Query getResponse = openTalkReference.GetQueryByName("GetReply", GraphApi.Query.Type.Query);
        getResponse.SetArgs(new { where = new { response_type = new { _eq = responseType } } });

        UnityWebRequest request = await openTalkReference.Post(getResponse);
        DeviceResponseResp resp = JsonConvert.DeserializeObject<DeviceResponseResp>(request.downloadHandler.text);

        if (resp.data.device_response.Length > 0)
        {
            return resp.data.device_response[0];
        }

        return null;
    }

    public async Task<Entry> getEntry(string msgType, string value)
    {
        GraphApi.Query insertMessage = openTalkReference.GetQueryByName("InsertMessages", GraphApi.Query.Type.Mutation);
        insertMessage.SetArgs(new { objects = new { receive = value, type = msgType } });
        UnityWebRequest r = await openTalkReference.Post(insertMessage);

        GraphApi.Query gEntry = openTalkReference.GetQueryByName("GetEntry", GraphApi.Query.Type.Mutation);
        gEntry.SetArgs(new { type = msgType, text = value });

        UnityWebRequest request = await openTalkReference.Post(gEntry);
        EntryResp resp = JsonConvert.DeserializeObject<EntryResp>(request.downloadHandler.text);
        return resp.data.entry;
    }

#nullable enable
    public async void controll(GameObject? gameObject, TypeValueMixin[] sends, bool interrupt = true, bool useTTS = false)
#nullable disable
    {
        if (interrupt) queue.Clear(); images.Clear();
        if (sends.Length <= 0) { return; };
        if (gameObject != null)
        {
            animator = gameObject.GetComponent<Animator>();
        }
        else
        {
            animator = null;
        }

        foreach (TypeValueMixin e in sends)
        {
            var send = e.value;
            if (e.type == "action")
            {
                e.value = "(執行動作: " + e.value + " )";
            }
            GraphApi.Query insertMessage = openTalkReference.GetQueryByName("InsertMessages", GraphApi.Query.Type.Mutation);
            insertMessage.SetArgs(new { objects = new { send = e.value, type = e.type } });
            UnityWebRequest r = await openTalkReference.Post(insertMessage);
        }

        foreach (TypeValueMixin e in sends)
        {
            switch (e.type)
            {
                case "audio":
                    if (!audioClips.ContainsKey(e.value))
                    {
                        UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(e.value, AudioType.MPEG);
                        await request.SendWebRequest();
                        if (request.result == UnityWebRequest.Result.ConnectionError)
                        {
                            Debug.Log(request.error);
                        }
                        else
                        {
                            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                            audioClips.Add(e.value, clip);
                        }
                        request.Dispose();
                    }
                    break;
                case "image":
                    UnityWebRequest imageRequest = UnityWebRequestTexture.GetTexture(e.value);
                    await imageRequest.SendWebRequest();
                    if (imageRequest.result == UnityWebRequest.Result.ConnectionError)
                    {
                        Debug.Log(imageRequest.error);
                    }
                    else
                    {
                        Texture2D texture = ((DownloadHandlerTexture)imageRequest.downloadHandler).texture;
                        images.Enqueue(texture);
                    }
                    imageRequest.Dispose();
                    break;
                default:
                    break;
            }

            queue.Enqueue(e);
        }
    }

    public void Update()
    {
        if (queue.Count == 0) return;
        if (isBusy && !audioSource.isPlaying) isBusy = false;
        if (isBusy) return;

        TypeValueMixin send = queue.Dequeue();
        switch (send.type)
        {
            case "text":
                description.text = send.value;
                break;
            case "audio":
                audioSource.clip = audioClips[send.value];
                audioSource.Play();
                isBusy = true;
                break;
            case "image":
                if (!rawImage.enabled)
                {
                    NextImage();
                }
                break;
            case "action":
                if (animator != null)
                {
                    animator.SetTrigger(send.value);
                }
                break;
            default:
                break;
        }
    }

    void NextImage()
    {
        if (images.Count > 0)
        {
            rawImage.enabled = true;
            closeBtn.gameObject.SetActive(true);
            rawImage.texture = images.Dequeue();
        }
        else
        {
            rawImage.enabled = false;
            closeBtn.gameObject.SetActive(false);
        }
    }
}
