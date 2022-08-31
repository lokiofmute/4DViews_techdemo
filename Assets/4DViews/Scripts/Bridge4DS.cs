
using UnityEngine;
using System.Runtime.InteropServices;

//-----------------Bridge4DS-----------------//

namespace unity4dv
{

    //Imports the native plugin functions.

    public class Bridge4DS
    {
        #if UNITY_IPHONE && !UNITY_EDITOR
            private const string IMPORT_NAME = "__Internal";  
        #else //Android & Desktop
            private const string IMPORT_NAME = "BridgeCodec4DS";
#endif

        //Inits the plugin (sequencemanager, etc.)
        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int CreateSequence(int key, [MarshalAs(UnmanagedType.LPStr)] string dataPath, int rangeBegin, int rangeEnd, OUT_RANGE_MODE outRangeMode, System.IntPtr errorMsg);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        //Stops the plugin and releases memory (sequencemanager, etc.)
        public static extern void DestroySequence(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        //Starts or stops the playback
        public static extern void Play(int key, bool on);
		
		[DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        //Stops the playback
        public static extern void Stop(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        //Starts loading and decoding
        public static extern void StartBuffering(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        //Gets the new model from plugin
        public static extern int UpdateModel(int key,
                                                System.IntPtr ptrVertices,
                                                System.IntPtr ptrUVs,
                                                System.IntPtr ptrTriangles,
                                                System.IntPtr texture,
                                                System.IntPtr normals,
                                                System.IntPtr velocities,
                                                System.IntPtr bbox,
                                                System.IntPtr colors,
                                                int lastModelId,
                                                ref int nbVertices,
                                                ref int nbTriangles,
                                                bool enableLookAt,
                                                Vector3 lookAtTarget,
                                                int lookAtMaxAngle);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        //Pull the new events occured
        public static extern int PullNewEvents(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        //Get one event. Must be called after PullNewEvents
        public static extern int GetEvent(int key, int idx, System.IntPtr name, ref int type);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        //Get the number of events in file
        public static extern int GetSizeEventList(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        //Get one event in the complete list of events in the file
        public static extern int GetEventFromList(int key, int idx, System.IntPtr name);

//        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
//        public static extern bool OutOfRangeEvent(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        //Gets the 4DR texture image size
        public static extern int GetTextureSize(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        //Gets the 4DR texture encoding
        public static extern int GetTextureEncoding(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetSequenceMaxVertices(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetSequenceMaxTriangles(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern float GetSequenceFramerate(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetSequenceNbFrames(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetSequenceCurrentFrame(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void GotoFrame(int key, int frame);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetSpeed(int key, float speedRatio);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetChunkBufferSize(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetMeshBufferSize(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetChunkBufferMaxSize(int key, int size);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetMeshBufferMaxSize(int key, int size);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetHTTPDownloadSize(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetHTTPDownloadSize(int key, int size);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern bool GetHTTPKeepInCache(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetHTTPKeepInCache(int key, bool val);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern long GetHTTPCacheSize(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetHTTPCacheSize(int key, long size);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void GetAudioBuffer(int key, System.IntPtr samples);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetAudioBufferSize(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetAudioNbSamples(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetAudioNbChannels(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetAudioSampleRate(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void AddDXTSupport(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void AddASTCSupport(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern bool HasLookAt(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetNbTrackings(int key);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void GetTrackingInfos(int key, int index, ref int firstFrame, ref int lastFrame, ref int rotationType, System.IntPtr name);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void GetTrackingBuffers(int key, int index, System.IntPtr positionBuffer, System.IntPtr rotationBuffer);

    } //class Bridge4DS
} //namespace unity4DV
