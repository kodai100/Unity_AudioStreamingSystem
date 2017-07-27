using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioStreamingSystem : MonoBehaviour {

    public float delayTime = 2f;
    public float fadeTime = 2f;

    [SerializeField] AudioSource source;
    [SerializeField] AudioClip[] clips; // 保持用
    [SerializeField] int currentClip;
    [SerializeField] bool playing;
    [SerializeField] float time;
    [SerializeField] float currentClipLength;

    [SerializeField] bool waiting;

    private void Awake() {
        source = GetComponent<AudioSource>();
    }
    
    private void Start() {
        StartCoroutine(GetClipsFromFolderPath());
    }

    private void Update() {

        if (playing) {

            time = source.time; // 表示用

            if(currentClipLength - source.time < fadeTime) {
                source.volume = Mathf.Lerp(0, 1, (currentClipLength - source.time) / fadeTime);
            }

            if (isClipEnd()) {
                StartCoroutine(WaitForNextClip(delayTime));
            } else {
                if (Skip()) {
                    StartCoroutine(WaitForNextClip(1));
                }

                if (Back()) {
                    StartCoroutine(WaitForPrevClip(1));
                }
            }

            
        }
        

    }

    private void Shuffle() {

    }

    bool Back() {
        if (Input.GetKeyUp(KeyCode.B) && !waiting) {
            return true;
        } else {
            return false;
        }
    }

    bool Skip() {
        if (Input.GetKeyUp(KeyCode.S) && !waiting) {
            return true;
        } else {
            return false;
        }
    }

    void PlayClip(int index) {
        Debug.Log("Now Playing ... " + clips[index].name);
        source.volume = 1;
        source.clip = clips[index];
        currentClipLength = source.clip.length;
        source.Play();
        waiting = false;
    }

    bool isClipEnd() {
        if (source.clip.length < source.time + Time.deltaTime) {
            return true;
        } else {
            return false;
        }
    }

    void OnReadComplete() {
        playing = true;
        currentClip = 0;
        PlayClip(currentClip);
    }

    public static string ReadFile() {
        FileInfo fi = new FileInfo(Application.dataPath + "/StreamingAssets/AudioFolder.txt");
        try {
            using (StreamReader sr = new StreamReader(fi.OpenRead(), Encoding.UTF8)) {
                string result = sr.ReadToEnd();
                Debug.Log("Audio Resource Folder : " + result);
                return result;
            }
        } catch (Exception e) {
            return null;
        }
    }
    
    IEnumerator WaitForNextClip(float delay) {
        source.Stop();
        currentClip = (currentClip + 1) % clips.Length;
        waiting = true;

        yield return new WaitForSeconds(delay);
        PlayClip(currentClip);
    }

    IEnumerator WaitForPrevClip(float delay) {
        source.Stop();
        currentClip = Mathf.Max(0, (currentClip - 1) % clips.Length);
        waiting = true;

        yield return new WaitForSeconds(delay);
        PlayClip(currentClip);
    }

    IEnumerator GetClipsFromFolderPath() {

        waiting = true;

        string audioFolder = ReadFile();
        
        if (audioFolder != null && Directory.Exists(audioFolder)) {

            string[] path_array = Directory.GetFiles(audioFolder, "*.wav");
            if (path_array.Length == 0) yield break;

            clips = new AudioClip[path_array.Length];

            for (int i = 0; i < clips.Length; i++) {
                float time = Time.realtimeSinceStartup;
                yield return StartCoroutine(SetClipFromPath(i, path_array[i]));
            }

            OnReadComplete();
        }
    }

    IEnumerator SetClipFromPath(int i, string path) {

        using (WWW www = new WWW("file:///" + path)) {
            yield return www;

            // 破損ファイルの場合の例外処理はしていない
            clips[i] = www.GetAudioClip(false, false);
            clips[i].name = path;

            Debug.Log("<color=cyan>Read complete : " + clips[i].name + ", ellapsed : " + (Time.realtimeSinceStartup - time) + "</color>");
        }
    }
}
