using System;
using System.Collections;
using UnityEngine;

namespace ZetanStudio
{
    public interface IPlayerNameHolder
    {
        static IPlayerNameHolder Instance { get; set; }

        string Name { get; }
    }

    public interface ICopiable
    {
        object Copy();
    }

    public interface IFadeAble
    {
        MonoBehaviour MonoBehaviour { get; }

        CanvasGroup FadeTarget { get; }

        protected Coroutine FadeCoroutine { get; set; }

        static void FadeTo(IFadeAble fader, float alpha, float duration, Action onDone = null)
        {
            if (!fader.FadeTarget) return;
            if (fader.FadeCoroutine != null) fader.MonoBehaviour.StopCoroutine(fader.FadeCoroutine);
            fader.FadeCoroutine = fader.MonoBehaviour.StartCoroutine(Fade(fader.FadeTarget, alpha, duration, onDone));

            static IEnumerator Fade(CanvasGroup target, float alpha, float duration, Action onDone)
            {
                float time = 0;
                while (time < duration)
                {
                    yield return null;
                    if (time < duration) target.alpha += (alpha - target.alpha) * Time.unscaledDeltaTime / (duration - time);
                    time += Time.unscaledDeltaTime;
                }
                target.alpha = alpha;
                onDone?.Invoke();
            }
        }
    }

    public interface ISceneLoader
    {
        static ISceneLoader Instance { get; set; }

        void LoadScene(string name, Action callback = null);
    }

    public interface IMessageDisplayer
    {
        static IMessageDisplayer Instance { get; set; }

        void Push(string message);
    }
}