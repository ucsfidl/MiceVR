//modified version of https://gist.github.com/LotteMakesStuff/16b5f2fc108f9a0201950c797d53cfbf

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace GoogleSheetsToUnity.ThirdPary
{
#if UNITY_EDITOR
    internal class EditorCoroutineRunner
    {
        private static List<EditorCoroutineState> coroutineStates;
        private static List<EditorCoroutineState> finishedThisUpdate;

        /// <summary>
        /// Start a coroutine. equivilent of calling StartCoroutine on a mono behaviour
        /// </summary>
        internal static EditorCoroutine StartCoroutine(IEnumerator coroutine)
        {
            return StoreCoroutine(new EditorCoroutineState(coroutine));
        }

        // Creates objects to manage the coroutines lifecycle and stores them away to be processed
        internal static EditorCoroutine StoreCoroutine(EditorCoroutineState state)
        {
            if (coroutineStates == null)
            {
                coroutineStates = new List<EditorCoroutineState>();
                finishedThisUpdate = new List<EditorCoroutineState>();
            }

            if (coroutineStates.Count == 0)
                EditorApplication.update += Runner;

            coroutineStates.Add(state);

            return state.editorCoroutineYieldInstruction;
        }

        // Manages running active coroutines!
        private static void Runner()
        {
            // Tick all the coroutines we have stored
            for (int i = 0; i < coroutineStates.Count; i++)
            {
                TickState(coroutineStates[i]);
            }

            // if a coroutine was finished whilst we were ticking, clear it out now
            for (int i = 0; i < finishedThisUpdate.Count; i++)
            {
                coroutineStates.Remove(finishedThisUpdate[i]);
            }
            finishedThisUpdate.Clear();

            // stop the runner if were done.
            if (coroutineStates.Count == 0)
            {
                EditorApplication.update -= Runner;
            }
        }

        private static void TickState(EditorCoroutineState state)
        {
            if (state.IsValid)
            {
                // This coroutine is still valid, give it a chance to tick!
                state.Tick();
            }
            else
            {
                // We have finished running the coroutine, lets scrap it
                finishedThisUpdate.Add(state);
            }
        }


    }

    internal class EditorCoroutineState
    {
        private IEnumerator coroutine;
        public bool IsValid
        {
            get { return coroutine != null; }
        }
        public EditorCoroutine editorCoroutineYieldInstruction;

        // current state
        private object current;
        private Type currentType;
        private float timer; // for WaitForSeconds support    
        private EditorCoroutine nestedCoroutine; // for tracking nested coroutines that are not started with EditorCoroutineRunner.StartCoroutine
        private DateTime lastUpdateTime;

        public EditorCoroutineState(IEnumerator coroutine)
        {
            this.coroutine = coroutine;
            editorCoroutineYieldInstruction = new EditorCoroutine();
            lastUpdateTime = DateTime.Now;
        }

        public void Tick()
        {
            if (coroutine != null)
            {

                // Did the last Yield want us to wait?
                bool isWaiting = false;
                var now = DateTime.Now;
                if (current != null)
                {
                    if (currentType == typeof(WaitForSeconds))
                    {
                        // last yield was a WaitForSeconds. Lets update the timer.
                        var delta = now - lastUpdateTime;
                        timer -= (float)delta.TotalSeconds;

                        if (timer > 0.0f)
                        {
                            isWaiting = true;
                        }
                    }
                    else if (currentType == typeof(WaitForEndOfFrame) || currentType == typeof(WaitForFixedUpdate))
                    {
                        // These dont make sense in editor, so we will treat them the same as a null return...
                        isWaiting = false;
                    }
                    else if (currentType == typeof(WWW))
                    {
                        // Web download request, lets see if its done!
                        var www = current as WWW;
                        if (!www.isDone)
                        {
                            isWaiting = true;
                        }
                    }
                    else if (currentType.IsSubclassOf(typeof(CustomYieldInstruction)))
                    {
                        // last yield was a custom yield type, lets check its keepWaiting property and react to that
                        var yieldInstruction = current as CustomYieldInstruction;
                        if (yieldInstruction.keepWaiting)
                        {
                            isWaiting = true;
                        }
                    }
                    else if (currentType == typeof(EditorCoroutine))
                    {
                        // Were waiting on another coroutine to finish
                        var editorCoroutine = current as EditorCoroutine;
                        if (!editorCoroutine.HasFinished)
                        {
                            isWaiting = true;
                        }
                    }
                    else if (typeof(IEnumerator).IsAssignableFrom(currentType))
                    {
                        // if were just seeing an enumerator lets assume that were seeing a nested coroutine that has been passed in without calling start.. were start it properly here if we need to
                        if (nestedCoroutine == null)
                        {
                            nestedCoroutine = EditorCoroutineRunner.StartCoroutine(current as IEnumerator);
                            isWaiting = true;
                        }
                        else
                        {
                            isWaiting = !nestedCoroutine.HasFinished;
                        }

                    }
                    else
                    {
                        // UNSUPPORTED
                        Debug.LogError("Unsupported yield (" + currentType + ") in editor coroutine!! Canceling.");
                    }
                }
                lastUpdateTime = now;

                if (!isWaiting)
                {
                    // nope were good! tick the coroutine!
                    bool update = coroutine.MoveNext();

                    if (update)
                    {
                        // yup the coroutine returned true so its been ticked...

                        // lets see what it actually yielded
                        current = coroutine.Current;
                        if (current != null)
                        {
                            // is it a type we have to do extra processing on?
                            currentType = current.GetType();

                            if (currentType == typeof(WaitForSeconds))
                            {
                                // its a WaitForSeconds... lets use reflection to pull out how long the actual wait is for so we can process the wait
                                var wait = current as WaitForSeconds;
                                FieldInfo m_Seconds = typeof(WaitForSeconds).GetField("m_Seconds", BindingFlags.NonPublic | BindingFlags.Instance);
                                if (m_Seconds != null)
                                {
                                    timer = (float)m_Seconds.GetValue(wait);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Coroutine returned false so its finally finished!!
                        Stop();
                    }
                }
            }
        }

        private void Stop()
        {
            // Coroutine has finished! do some cleanup...
            coroutine = null;
            editorCoroutineYieldInstruction.HasFinished = true;
        }
    }

    /// <summary>
    /// Created when an Editor Coroutine is started, can be yielded to to allow another coroutine to finish first.
    /// </summary>
    internal class EditorCoroutine : YieldInstruction
    {
        public bool HasFinished;
    }
#endif
}