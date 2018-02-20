﻿using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Unity.VideoHelper
{
    [Serializable]
    public class VolumeStore
    {
        public float Minimum;
        public Sprite Sprite;
    }

    /// <summary>
    /// Handles UI state for the video player.
    /// </summary>
    [RequireComponent(typeof(VideoController))]
    public class VideoPresenter : MonoBehaviour, ITimelineProvider
    {
        #region Consts

        private const string MinutesFormat = "{0:00}:{1:00}";
        private const string HoursFormat = "{0:00}:{1:00}:{2:00}";

        #endregion

        #region Private Fields

        private VideoController controller;

        #endregion

        #region Public Fields

        [Header("Controls")]
        public Transform Screen;
        public Transform ControlsPanel;
        public Transform Spinner;
        public Transform Thumbnail;
        public Timeline Timeline;
        public Slider Volume;
        public Image PlayPause;
        public Image MuteUnmute;
        public Image SmallFullscreen;
        public Text Current;
        public Text Duration;

        [Header("Input")]

        public KeyCode FullscreenKey = KeyCode.F;
        public KeyCode WindowedKey = KeyCode.Escape;
        public KeyCode TogglePlayKey = KeyCode.Space;

        [Space(10)]
        public bool ToggleScreenDoubleClick = true;

        [Header("Content")]

        public Sprite Play;
        public Sprite Pause;
        public Sprite Normal;
        public Sprite Fullscreen;

        [Space(10)]

        public VolumeStore[] Volumes = new VolumeStore[0];

        [Header("Animation")]
        public AnimationCurve FadeIn = AnimationCurve.Linear(0, 0, .4f, 1);
        public AnimationCurve FadeOut = AnimationCurve.Linear(0, 1, .4f, 0);

        #endregion

        #region Unity methods

        private void Start()
        {
            controller = GetComponent<VideoController>();

            if (controller == null)
            {
                Debug.Log("There is no video controller attached.");
                DestroyImmediate(this);
                return;
            }

            controller.OnStartedPlaying.AddListener(OnStartedPlaying);

            Volume.onValueChanged
                .AddListener(OnVolumeChanged);

            AttachHandlerToClick(Thumbnail.gameObject, Prepare);

            AttachHandlerToClick(PlayPause.gameObject, ToggleIsPlaying);
            AttachHandlerToClick(SmallFullscreen.gameObject, ToggleFullscreen);

            AttachHandlerToDoubleClick(Screen.gameObject, ToggleFullscreen);
            AttachHandlerToClick(Screen.gameObject, ToggleIsPlaying);

            if(ControlsPanel != null)
                ControlsPanel.gameObject.SetActive(false);

            Array.Sort(Volumes, (v1, v2) =>
            {
                if (v1.Minimum > v2.Minimum)
                    return 1;
                else if (v1.Minimum == v2.Minimum)
                    return 0;
                else
                    return -1;
            });
        }

        private void Prepare()
        {
            if(Thumbnail != null)
                Thumbnail.gameObject.SetActive(false);

            if(Spinner != null)
                Spinner.gameObject.SetActive(true);
        }

        private void Update()
        {
            CheckKeys();

            if (controller.IsPlaying)
                Timeline.Position = controller.NormalizedTime;
        }

        #endregion

        public string GetFormattedPosition(float time)
        {
            return PrettyTimeFormat(TimeSpan.FromSeconds(time * controller.Duration));
        }

        #region Private methods

        private void AttachHandlerToClick(GameObject control, UnityAction action)
        {
            var button = control.GetComponent<Button>();
            if (button == null)
                control.AddComponent<ClickRouter>().OnClick.AddListener(action);
            else
                button.onClick.AddListener(action);
        }

        private void AttachHandlerToDoubleClick(GameObject control, UnityAction action)
        {
            var router = control.GetComponent<ClickRouter>();
            if (router == null)
                control.AddComponent<ClickRouter>().OnDouleClick.AddListener(action);
            else
                router.OnDouleClick.AddListener(action);
        }

        private void CheckKeys()
        {
            if (Input.GetKeyDown(FullscreenKey))
            {
                ScreenSizeHelper.GoFullscreen(gameObject.transform as RectTransform);
            }
            if (Input.GetKeyDown(WindowedKey))
            {
                ScreenSizeHelper.GoWindowed();
            }
            if (Input.GetKeyDown(TogglePlayKey))
            {
                ToggleIsPlaying();
            }
        }

        private void ToggleIsPlaying()
        {
            if (controller.IsPlaying)
            {
                controller.Pause();
                PlayPause.sprite = Play;
            }
            else
            {
                controller.Play();
                PlayPause.sprite = Pause;
            }
        }

        private void OnStartedPlaying()
        {
            if (ControlsPanel != null)
                ControlsPanel.gameObject.SetActive(true);

            if (Spinner != null)
                Spinner.gameObject.SetActive(false);

            if(Duration != null)
                Duration.text = PrettyTimeFormat(TimeSpan.FromSeconds(controller.Duration));
            StartCoroutine(SetCurrentPosition());
        }

        private void OnVolumeChanged(float volume)
        {
            controller.Volume = volume;

            for (int i = 0; i < Volumes.Length; i++)
            {
                var current = Volumes[i];
                var next = Volumes.Length - 1 >= i + 1 ? Volumes[i + 1].Minimum : 2;

                if (current.Minimum <= volume && next > volume)
                {
                    MuteUnmute.sprite = current.Sprite;
                }
            }
        }

        private void ToggleFullscreen()
        {
            if (ScreenSizeHelper.IsFullscreen)
            {
                ScreenSizeHelper.GoWindowed();
                SmallFullscreen.sprite = Fullscreen;
            }
            else
            {
                ScreenSizeHelper.GoFullscreen(gameObject.transform as RectTransform);
                SmallFullscreen.sprite = Normal;
            }
        }

        private IEnumerator SetCurrentPosition()
        {
            if(Current != null)
                Current.text = PrettyTimeFormat(TimeSpan.FromSeconds(controller.Time));

            yield return new WaitForSeconds(1);

            if(controller.IsPlaying)
                StartCoroutine(SetCurrentPosition());
        }

        private string PrettyTimeFormat(TimeSpan time)
        {
            if (time.TotalHours <= 1)
                return string.Format(MinutesFormat, time.Minutes, time.Seconds);
            else
                return string.Format(HoursFormat, time.Hours, time.Minutes, time.Seconds);
        }

        #endregion
    }
}

