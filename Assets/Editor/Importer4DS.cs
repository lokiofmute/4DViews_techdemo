using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_2020_2_OR_NEWER
    using UnityEditor.AssetImporters;
#else
    using UnityEditor.Experimental.AssetImporters;
#endif
using System.Runtime.InteropServices;


namespace unity4dv
{   
    [ScriptedImporter(1, "4ds")]
    public class Importer4ds : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            /*
            var path = ctx.assetPath;
            
            string errorMsg = new string(' ',300);
            GCHandle errorMsgHandle = GCHandle.Alloc(errorMsg, GCHandleType.Pinned);
            
            int uid = Bridge4DS.CreateSequence(0, path, 0, 0, OUT_RANGE_MODE.Hide, errorMsgHandle.AddrOfPinnedObject());
            errorMsg = Marshal.PtrToStringAnsi(errorMsgHandle.AddrOfPinnedObject());
            
            var go = new GameObject(path);
            if (uid != 0)
            {
                CreateEventsClip(uid, ctx);
                
                Texture2D t = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/4DViews/4dv_icon.png", typeof(Texture2D));
                ctx.AddObjectToAsset(path, go, t);
                ctx.SetMainObject(go);
            }
            else
                Debug.Log("ErrorMsg " + errorMsg);
            
            errorMsgHandle.Free();
            Bridge4DS.DestroySequence(uid);
            */
        }
        
        private void CreateEventsClip(int uid, AssetImportContext ctx) {
            int nb_event = Bridge4DS.GetSizeEventList(uid);
            if (nb_event == 0) return;

            float fps = Bridge4DS.GetSequenceFramerate(uid);

            var events = new AnimationEvent[nb_event];
            
            string eventName = new string(' ', 100);
            GCHandle eventNameHandle = GCHandle.Alloc(eventName, GCHandleType.Pinned);
            for (int i=0; i<nb_event; ++i) {
                int event_frame = Bridge4DS.GetEventFromList(uid, i, eventNameHandle.AddrOfPinnedObject());
                eventName = Marshal.PtrToStringAnsi(eventNameHandle.AddrOfPinnedObject());

                var ev = new AnimationEvent();
                ev.time = event_frame / fps;
                ev.stringParameter = eventName;
                ev.functionName = "OnUserEvent ( " + eventName + " )";

                events[i] = ev;
            }
            eventNameHandle.Free();
                
            AnimationClip clip = new AnimationClip();
            AnimationUtility.SetAnimationEvents(clip, events);
            clip.name = "Events";
            clip.frameRate = fps;
            ctx.AddObjectToAsset(clip.name, clip);   
        }
    }
    
}

