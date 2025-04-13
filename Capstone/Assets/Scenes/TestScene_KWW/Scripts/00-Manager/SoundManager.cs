using System.Collections.Generic;
using UnityEngine;

namespace FireEvacuation
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance;

        [System.Serializable]
        public class SoundGroup
        {
            public GameObject audioObject;         // AudioSource가 붙어 있는 오브젝트
            public List<AudioClip> clips;

            [HideInInspector] public AudioSource source;
        }

        [Header("사운드 설정")]
        public List<SoundGroup> soundGroups;

        [Header("BGM 자동 재생")]
        public GameObject bgmObject;           // 사용자 주변에 위치한 오브젝트
        public AudioClip bgmClip;              // 기본 배경음
        public float bgmVolume = 0.5f;

        private AudioSource bgmSource;         // 내부에서 자동 제어용

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitSoundGroups();
                SetupBGM(); // 🎵 BGM 자동 실행
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitSoundGroups()
        {
            foreach (var group in soundGroups)
            {
                if (!group.audioObject)
                {
                    Debug.LogWarning("SoundGroup has no audioObject assigned.");
                    continue;
                }

                group.source = group.audioObject.GetComponent<AudioSource>();
                if (!group.source)
                {
                    Debug.LogWarning("SoundGroup has no AudioSource component on its object. Adding AudioSource.");
                    group.source = group.audioObject.AddComponent<AudioSource>();
                }
            }
        }

        private void SetupBGM()
        {
            if (bgmObject == null || bgmClip == null)
            {
                Debug.LogWarning("BGM 자동 재생이 설정되지 않았습니다.");
                return;
            }

            bgmSource = bgmObject.GetComponent<AudioSource>();
            if (bgmSource == null)
            {
                bgmSource = bgmObject.AddComponent<AudioSource>();
            }

            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.volume = Mathf.Clamp01(bgmVolume);
            bgmSource.spatialBlend = 1f; // 3D 사운드
            bgmSource.minDistance = 1f;
            bgmSource.maxDistance = 20f;
            bgmSource.Play();
        }

        public void Play(int groupIndex, int clipIndex, bool loop = false)
        {
            if (groupIndex < 0 || groupIndex >= soundGroups.Count)
            {
                Debug.LogError($"Sound group index {groupIndex} is out of range. Groups count: {soundGroups.Count}");
                return;
            }

            var group = soundGroups[groupIndex];
            if (clipIndex < 0 || clipIndex >= group.clips.Count)
            {
                Debug.LogError($"Clip index {clipIndex} is out of range for group at index {groupIndex}. Clips count: {group.clips.Count}");
                return;
            }

            if (group.source == null)
            {
                Debug.LogError($"AudioSource is null for group at index {groupIndex}!");
                return;
            }

            group.source.clip = group.clips[clipIndex];
            group.source.loop = loop;
            group.source.Play();
            Debug.Log($"Playing sound: Group Index {groupIndex}, Clip: {group.clips[clipIndex].name}");
        }

        public void PlayOneShot(int groupIndex, int clipIndex)
        {
            if (groupIndex < 0 || groupIndex >= soundGroups.Count)
            {
                Debug.LogError($"Sound group index {groupIndex} is out of range. Groups count: {soundGroups.Count}");
                return;
            }

            var group = soundGroups[groupIndex];
            if (clipIndex < 0 || clipIndex >= group.clips.Count)
            {
                Debug.LogError($"Clip index {clipIndex} is out of range for group at index {groupIndex}. Clips count: {group.clips.Count}");
                return;
            }

            if (group.source == null)
            {
                Debug.LogError($"AudioSource is null for group at index {groupIndex}!");
                return;
            }

            group.source.PlayOneShot(group.clips[clipIndex]);
            Debug.Log($"Playing sound: Group Index {groupIndex}, Clip: {group.clips[clipIndex].name}");
        }

        public void SetVolume(int groupIndex, float volume)
        {
            if (groupIndex < 0 || groupIndex >= soundGroups.Count)
            {
                Debug.LogError($"Sound group index {groupIndex} is out of range. Groups count: {soundGroups.Count}");
                return;
            }

            var group = soundGroups[groupIndex];
            if (group.source != null)
            {
                group.source.volume = Mathf.Clamp01(volume);
            }
        }

        public void StopAll()
        {
            foreach (var group in soundGroups)
            {
                if (group.source != null)
                {
                    group.source.Stop();
                }
            }
        }

        public void PlayBGMManual()
        {
            if (bgmSource != null && !bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
        }
    }
}