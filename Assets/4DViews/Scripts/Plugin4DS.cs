//#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_IOS || UNITY_ANDROID || UNITY_WSA || UNITY_LUMIN
//#define USE_NATIVE_LIB
//#endif

using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace unity4dv
{
    public enum OUT_RANGE_MODE
    {
        Loop = 0,
        Reverse = 1,
        Stop = 2,
        Hide = 3
    }

    public enum SOURCE_TYPE
    {
        Local = 0,
        Network = 1
    }

    interface IPlugin4DSInterface
    {
        void Initialize(bool resetRange = false);
        void Close();

        void Play(bool on);
        void GotoFrame(int frame);
    }

    [System.Serializable]
    public class ListEventGUI 
	{
        public bool show = false;
        public List<int> eventsFrames;
        public List<string> eventsNames;

        public void Clear()
        {
            eventsFrames.Clear();
            eventsNames.Clear();
        }

        public void Add(int frame, string name)
        {
            eventsFrames.Add(frame);
            eventsNames.Add(name);
        }

        public void Sort()
        {
            for (int i = 0; i < eventsFrames.Count; i++)
            {
                var item = eventsFrames[i];
                var name = eventsNames[i];
                var currentIndex = i;

                while (currentIndex > 0 && eventsFrames[currentIndex - 1] > item)
                {
                    eventsFrames[currentIndex] = eventsFrames[currentIndex - 1];
                    eventsNames[currentIndex] = eventsNames[currentIndex - 1];
                    currentIndex--;
                }

                eventsFrames[currentIndex] = item;
                eventsNames[currentIndex] = name;
            }
        }
    }

    public class Plugin4DS : MonoBehaviour, IPlugin4DSInterface
    {
#region Properties
        //-----------------------------//
        //-  PROPERTIES               -//
        //-----------------------------//
        /// <summary>
        /// current displayed mesh frame
        /// </summary>
        public int CurrentFrame { get { return GetCurrentFrame(); } set { GotoFrame((int)value); } }
        /// <summary>
        /// frame rate of the sequence
        /// </summary>
        public float Framerate { get { return GetFrameRate(); } }
        /// <summary>
        /// number of frames in the sequence
        /// </summary>
        public int SequenceNbOfFrames { get { return GetSequenceNbFrames(); } }
        /// <summary>
        /// number of frames in the active range (between first and last active frame)
        /// </summary>
        public int ActiveNbOfFrames { get { return GetActiveNbFrames(); } }
        /// <summary>
        /// first frame to play
        /// </summary>
        public int FirstActiveFrame { get { return (int)_activeRangeMin; } set { _activeRangeMin = (float)value; } }
        /// <summary>
        /// last frame to play
        /// </summary>
        public int LastActiveFrame { get { return (int)_activeRangeMax==-1? (int)(SequenceNbOfFrames -1): (int)(_activeRangeMax); } set { _activeRangeMax = (float)value; } }
        /// <summary>
        /// sequence texture image encoding (astc for mobile or dxt for desktop)
        /// </summary>
        public TextureFormat TextureEncoding { get { return GetTextureFormat(); } }
        /// <summary>
        /// sequence texture image size
        /// </summary>
        public int TextureSize { get { return Bridge4DS.GetTextureSize(_dataSource.FDVUUID); } }
        /// <summary>
        /// nb vertices of the current mesh
        /// </summary>
        public int NbVertices { get { return _nbVertices; } }
        /// <summary>
        /// nb triangles of the current mesh
        /// </summary>
        public int NbTriangles { get { return _nbTriangles; } }
        /// <summary>
        /// does sequence play automatically at start
        /// </summary>
        public bool AutoPlay { get { return _autoPlay; } set { _autoPlay = value; } }
        /// <summary>
        /// is the sequence currently playing
        /// </summary>
        public bool IsPlaying { get { return _isPlaying; } set { _isPlaying = value; } }
        /// <summary>
        /// has the been initalized
        /// </summary>
        public bool IsInitialized { get { return _isInitialized; } }
        /// <summary>
        /// sequence file name
        /// </summary>
        public string SequenceName { get { return _sequenceName; } set { _sequenceName = value;} }
        /// <summary>
        /// sequence file path
        /// </summary>
        public string SequenceDataPath { get { return _mainDataPath; } set { _mainDataPath = value; } }
        /// <summary>
        /// local file or network stream
        /// </summary>
        public SOURCE_TYPE SourceType { get { return _sourceType; } set { _sourceType = value; } }
        /// <summary>
        /// is the sequence looping
        /// </summary>
        public bool Loop { get { return _loop; } set { _loop = value; } }

        /// <summary>
        /// frame used as preview mesh
        /// </summary>
        public int PreviewFrame { get { return _previewFrame; } set { _previewFrame = value; } }

        /// <summary>
        /// speed ratio used (1 for normal speed, 0.5 for half speed)
        /// </summary>
        public float SpeedRatio {
            get { return _speedRatio; }
            set {
                _speedRatio = value;
                if (_dataSource != null) Bridge4DS.SetSpeed(_dataSource.FDVUUID, _speedRatio);
            }
        }

        /// <summary>
        /// number of meshes ready in the buffer
        /// </summary>
        public int MeshBufferSize {   get { return Bridge4DS.GetMeshBufferSize(_dataSource.FDVUUID); } }
        /// <summary>
        /// number of data chunks ready for decoding in the buffer
        /// </summary>
        public int ChunkBufferSize {  get { return Bridge4DS.GetChunkBufferSize(_dataSource.FDVUUID); } }
        /// <summary>
        /// maximum number of meshes in the buffer
        /// </summary>
        public int MeshBufferMaxSize { get { return _meshBufferMaxSize; } set { _meshBufferMaxSize = value; } }
        /// <summary>
        /// maximum number of chunks in the buffer
        /// </summary>
        public int ChunkBufferMaxSize { get { return _chunkBufferMaxSize; } set { _chunkBufferMaxSize = value; } }
        /// <summary>
        /// payload size for each http request (network stream)
        /// </summary>
        public int HTTPDownloadSize { get { return _HTTPDownloadSize; } set { _HTTPDownloadSize = value; } }
        /// <summary>
        /// size of the downloaded data cache size (network stream)
        /// </summary>
        public bool HTTPKeepInCache { get { return _HTTPKeepInCache; }  set { _HTTPKeepInCache = value; } }
        /// <summary>
        /// is the downloaded data kept in the cache (network stream)
        /// </summary>
        public long HTTPCacheSize { get { return _HTTPCacheSize; } set { _HTTPCacheSize = value; } }

        /// <summary>
        /// does the sequence use vertex color instead of texture image
        /// </summary>
        public bool HasVertexColor { get { return _dataSource.ColorPerVertex; } }

        /// <summary>
        /// when look at is active, position where the look at targets
        /// </summary>
        public Vector3 LookAtTarget { get { return _lookAtTarget; } set { _lookAtTarget = value; } }
        /// <summary>
        /// when look at is active, maximum angle between target direction and default look direction to apply the transformation
        /// </summary>
        public int LookAtMaxAngle { get { return _lookAtMaxAngle; } set { _lookAtMaxAngle = value; } }

        /// <summary>
        /// list of events in the 4ds file
        /// </summary>
        public ListEventGUI ListEvents {get {return _listEvents;} set {_listEvents = value;} }
        
        #endregion

#region Events
        //-----------------------------//
        //-  EVENTS                   -//
        //-----------------------------//
        public delegate void EventFDV();
        public event EventFDV OnNewModel;
        public event EventFDV OnModelNotFound;

        public class IntEventFDV : UnityEngine.Events.UnityEvent<int> {}
        public IntEventFDV OnFirstFrame = new IntEventFDV();
        public IntEventFDV OnLastFrame = new IntEventFDV();

        public class UserEventFDV : UnityEngine.Events.UnityEvent<int, string> {}
        public UserEventFDV OnUserEvent = new UserEventFDV();
#endregion

#region class Members
        //-----------------------------//
        //- Class members declaration -//
        //-----------------------------//

        //Path containing the 4DR data (edited in the unity editor panel)
        [SerializeField]
        private string _sequenceName;

        [SerializeField]
        private SOURCE_TYPE _sourceType = SOURCE_TYPE.Local;

        [SerializeField]
        private string _mainDataPath;
        public bool _dataInStreamingAssets = false;

        [SerializeField]
        private int _meshBufferMaxSize = 10;
        [SerializeField]
        private int _chunkBufferMaxSize = 180;
        [SerializeField]
        private int _HTTPDownloadSize = 10000000;
        [SerializeField]
        private bool _HTTPKeepInCache = false;
        [SerializeField]
        private static long _HTTPCacheSize = 1000000000;

        //Playback
        [SerializeField]
        private bool _autoPlay = true;
        [SerializeField]
        private OUT_RANGE_MODE _outRangeMode = OUT_RANGE_MODE.Loop;
        [SerializeField]
        private bool _loop = true;

        [SerializeField]
        public bool playAudio = true;

        //Active Range
        [SerializeField]
        private float _activeRangeMin = 0;
        [SerializeField]
        private float _activeRangeMax = -1;

        //Infos
        public bool _debugInfo = false;
        private float _decodingFPS = 0f;
        private int _lastDecodingId = 0;
        private System.DateTime _lastDecodingTime;
        private float _updatingFPS = 0f;
        private int _lastUpdatingId = 0;
        private System.DateTime _lastUpdatingTime;
        private int _totalFramesPlayed = 0;
        private System.DateTime _playDate;
        private int prevFrame = -1;

        //4D source
        private DataSource4DS _dataSource = null;
        [SerializeField]
        private int _lastModelId = -1;

        //Mesh and texture objects
        private Mesh[] _meshes = null;
        private Texture2D[] _textures = null;
        private MeshFilter _meshComponent;
        private Renderer _rendererComponent;

        //Receiving geometry and texture buffers
        private Vector3[] _newVertices;
        private Vector2[] _newUVs = null;
        private int[] _newTriangles;
        private byte[] _newTextureData = null;
        private Vector3[] _newNormals = null;
        private Vector3[] _newVelocities = null;
        private Vector3[] _newBBox = null;
        private Color32[] _newColors = null;
        private GCHandle _newVerticesHandle;
        private GCHandle _newUVsHandle;
        private GCHandle _newTrianglesHandle;
        private GCHandle _newTextureDataHandle;
        private GCHandle _newNormalsHandle;
        private GCHandle _newVelocitiesHandle;
        private GCHandle _newBBoxHandle;
        private GCHandle _newColorsHandle;

        private float[] samples;
        GCHandle audioBufferHandle;

        //Mesh and texture multi-buffering (optimization)
        private int _nbGeometryBuffers = 2;
        private int _currentGeometryBuffer;
        private int _nbTextureBuffers = 2;
        private int _currentTextureBuffer;

        private bool _newMeshAvailable = false;

        //pointer to the mesh Collider, if present (=> will update it at each frames for collisions)
        private MeshCollider _meshCollider;
        private BoxCollider _boxCollider;

        //events
        private string _newEventString;
        private GCHandle _newEventHandle;

        //Has the plugin been initialized
        [SerializeField]
        private bool _isInitialized = false;
        //[SerializeField]
        //private bool _isTrackingsCreated = false;
        [SerializeField]
        private bool _isPlaying = false;

        [SerializeField]
        private int _previewFrame = 0;
        public System.DateTime last_preview_time = System.DateTime.Now;

        [SerializeField]
        private int _nbFrames = 0;
        [SerializeField]
        private float _speedRatio = 1.0f;

        [SerializeField]
        private ListEventGUI _listEvents = new ListEventGUI();
        [SerializeField]
        private Dictionary<string, int> _events_to_frame = new Dictionary<string, int>();

        private int _nbVertices;
        private int _nbTriangles;

        private const int MAX_SHORT = 65535;

        private bool wasPlayingWhenFocusLost = false;
        private float unityTimeScale = 1;

        private Vector3 _lookAtTarget;
        private int _lookAtMaxAngle = 90;

        private const string TRACKING_NODE_NAME = "4DVTrackings";

        #endregion

#region public methods
        //-----------------------------//
        //- Class methods implement.  -//
        //-----------------------------//


        /**
         * load the data and initialize the 4D sequence
	     * @param resetRange : should the active frame range be reset to the sequence default values or not
         */
        public void Initialize(bool resetRange = false)
        {
            //Initialize already called successfully
            if (_isInitialized == true)
                return;

            if (_dataSource == null)
            {
                int key = 0;

                if (_sourceType == SOURCE_TYPE.Network && !(_sequenceName.Substring(0,4) == "http" || _sequenceName.Substring(0, 7) == "holosys"))
                {
                    UnityEngine.Debug.LogError("Plugin 4DS: When using Network Source Type, URL should start with http://, or holosys:// for live");
                    return;
                }

                if (resetRange) {
                    _activeRangeMax = -1;
                    _activeRangeMin = 0;
                }

                //Creates data source from the given path 
                _dataSource = DataSource4DS.CreateDataSource(key, _sequenceName, _dataInStreamingAssets, _mainDataPath, (int)_activeRangeMin, (int)_activeRangeMax, _outRangeMode);
                if (_dataSource == null)
                {
                    OnModelNotFound?.Invoke();
                    return;
                }
            }

            _lastModelId = -1;

            _meshComponent = GetComponent<MeshFilter>();
            _rendererComponent = GetComponent<Renderer>();
            _meshCollider = GetComponent<MeshCollider>();
            _boxCollider = GetComponent<BoxCollider>();

            _nbFrames = Bridge4DS.GetSequenceNbFrames(_dataSource.FDVUUID);

            unityTimeScale = Time.timeScale;
            Bridge4DS.SetSpeed(_dataSource.FDVUUID, _speedRatio * unityTimeScale);

            if (_sourceType == SOURCE_TYPE.Network && !(_sequenceName.Length > 7 && _sequenceName.Substring(0, 7) == "holosys") )
            {
                Bridge4DS.SetHTTPDownloadSize(_dataSource.FDVUUID, _HTTPDownloadSize);
                Bridge4DS.SetHTTPKeepInCache(_dataSource.FDVUUID, _HTTPKeepInCache);
                Bridge4DS.SetHTTPCacheSize(_dataSource.FDVUUID, _HTTPCacheSize);
            }

            Bridge4DS.SetChunkBufferMaxSize(_dataSource.FDVUUID, _chunkBufferMaxSize);
            Bridge4DS.SetMeshBufferMaxSize(_dataSource.FDVUUID, _meshBufferMaxSize);


            //Allocates geometry buffers
            AllocateGeometryBuffers(ref _newVertices, ref _newUVs, ref _newNormals, ref _newVelocities, ref _newBBox, ref _newTriangles, ref _newColors, _dataSource.MaxVertices, _dataSource.MaxTriangles);

            if (!_dataSource.ColorPerVertex) {
                //Allocates texture pixel buffer
                int pixelBufferSize = _dataSource.TextureSize * _dataSource.TextureSize / 2;    //default is 4 bpp
                if (_dataSource.TextureFormat == TextureFormat.PVRTC_RGB2)  //pvrtc2 is 2bpp
                    pixelBufferSize /= 2;
                else if (_dataSource.TextureFormat == TextureFormat.ASTC_8x8)
                {
                    int blockSize = 8;
                    int xblocks = (_dataSource.TextureSize + blockSize - 1) / blockSize;
                    pixelBufferSize = xblocks * xblocks * 16;
                }
                else if (_dataSource.TextureFormat == TextureFormat.RGBA32)
                {
                    pixelBufferSize = _dataSource.TextureSize * _dataSource.TextureSize * 4;
                }
                _newTextureData = new byte[pixelBufferSize];
                _newTextureDataHandle = GCHandle.Alloc(_newTextureData, GCHandleType.Pinned);
            }else
            {
                if (!(_rendererComponent.sharedMaterial.shader.name == "Particles/Standard Surface" || _rendererComponent.sharedMaterial.shader.name == "Particles/Standard Unlit"))
                {
                    Shader particle = Shader.Find("Particles/Standard Unlit");
                    _rendererComponent.sharedMaterial.shader = particle;
                }
            }

            //Gets pinned memory handle
            _newVerticesHandle = GCHandle.Alloc(_newVertices, GCHandleType.Pinned);
            _newTrianglesHandle = GCHandle.Alloc(_newTriangles, GCHandleType.Pinned);
            if (_dataSource.ColorPerVertex) {
                _newColorsHandle = GCHandle.Alloc(_newColors, GCHandleType.Pinned);
            }
            else
            {
                _newUVsHandle = GCHandle.Alloc(_newUVs, GCHandleType.Pinned);
                _newNormalsHandle = GCHandle.Alloc(_newNormals, GCHandleType.Pinned);
                _newVelocitiesHandle = GCHandle.Alloc(_newVelocities, GCHandleType.Pinned);
                _newBBoxHandle = GCHandle.Alloc(_newBBox, GCHandleType.Pinned);
            }
            //Allocates objects buffers for double buffering
            _meshes = new Mesh[_nbGeometryBuffers];
            if (!_dataSource.ColorPerVertex)
                _textures = new Texture2D[_nbTextureBuffers];

            for (int i = 0; i < _nbGeometryBuffers; i++)
            {
                //Mesh
                Mesh mesh = new Mesh();
                if (_dataSource.MaxVertices > MAX_SHORT)
                    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.MarkDynamic(); //Optimize mesh for frequent updates. Call this before assigning vertices. 
                mesh.vertices = _newVertices;
                mesh.triangles = _newTriangles;
                if (_newUVs != null) mesh.uv = _newUVs;
                if (_newNormals != null) mesh.normals = _newNormals;
                if (_newVelocities != null) mesh.SetUVs(5, _newVelocities);
                if (_newColors != null) mesh.colors32 = _newColors;

                Bounds newBounds = mesh.bounds;
                newBounds.extents = new Vector3(4, 4, 4);
                mesh.bounds = newBounds;
                _meshes[i] = mesh;
            }

            if (!_dataSource.ColorPerVertex)
            { 
                for (int i = 0; i < _nbTextureBuffers; i++)
                {
                //Texture
        #if UNITY_2019_1_OR_NEWER
                    if (_dataSource.TextureFormat == TextureFormat.ASTC_8x8)   //since unity 2019 ASTC RGBA is no more supported
                        _dataSource.TextureFormat = TextureFormat.ASTC_8x8;
        #endif
                    Texture2D texture = new Texture2D(_dataSource.TextureSize, _dataSource.TextureSize, _dataSource.TextureFormat, false)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear
                    };
                    texture.Apply(); //upload to GPU
                    _textures[i] = texture;
                }
            }
            _currentGeometryBuffer = _currentTextureBuffer = 0;

            _nbFrames = Bridge4DS.GetSequenceNbFrames(_dataSource.FDVUUID);

            if (playAudio)
                InitAudio();

            // Events
            _newEventString = new string(' ', 100);
            _newEventHandle = GCHandle.Alloc(_newEventString, GCHandleType.Pinned);

#if UNITY_EDITOR
            var tracking_node = this.transform.Find(TRACKING_NODE_NAME);
            if (tracking_node == null)
            {
                CreateTrackings();
            }

            FillEventsList();
#endif

            _isInitialized = true;
        }


        /**
         * unload the data, reset all the settings, clear the buffers
         */
        public void Uninitialize()
        {
            if (!_isInitialized)
                return;

            //Releases sequence
            if (_dataSource != null) Bridge4DS.DestroySequence(_dataSource.FDVUUID);

            //Releases memory
            if (_newVerticesHandle.IsAllocated) _newVerticesHandle.Free();
            if (_newUVsHandle.IsAllocated) _newUVsHandle.Free();
            if (_newTrianglesHandle.IsAllocated) _newTrianglesHandle.Free();
            if (_newTextureDataHandle.IsAllocated) _newTextureDataHandle.Free();
            if (_newNormalsHandle.IsAllocated) _newNormalsHandle.Free();
            if (_newVelocitiesHandle.IsAllocated) _newVelocitiesHandle.Free();
            if (_newBBoxHandle.IsAllocated) _newBBoxHandle.Free();
            if (_newColorsHandle.IsAllocated) _newColorsHandle.Free();
            if (_newEventHandle.IsAllocated) _newEventHandle.Free();


            if (audioBufferHandle.IsAllocated) audioBufferHandle.Free();
            samples = null;

            if (_meshes != null)
            {
                for (int i = 0; i < _meshes.Length; i++)
                    DestroyImmediate(_meshes[i]);
                _meshes = null;
            }
            if (_textures != null)
            {
                for (int i = 0; i < _textures.Length; i++)
                    DestroyImmediate(_textures[i]);
                _textures = null;
            }

            _dataSource = null;
            _newVertices = null;
            _newUVs = null;
            _newTriangles = null;
            _newNormals = null;
            _newVelocities = null;
            _newBBox = null;
            _newColors = null;
            _newTextureData = null;

            _isInitialized = false;

#if UNITY_EDITOR
            EditorApplication.pauseStateChanged -= HandlePauseState;
#endif
        }

        /**
         * Starts decdoing and buffering the meshes
         */
        public void StartBuffering()
        {
            if (IsInitialized)
                Bridge4DS.StartBuffering(_dataSource.FDVUUID);
        }


        /**
         * Start or pause the playback
	     * @param on : should start or pause the playback
         */
        public void Play(bool on)
        {
            if (IsInitialized)
            {
                if (on)
                {
                    Bridge4DS.Play(_dataSource.FDVUUID, on);
                    _totalFramesPlayed = 0;
                    _playDate = System.DateTime.Now;
                }
                else
                {
                    Bridge4DS.Play(_dataSource.FDVUUID, on);
                }
                _isPlaying = on;
            }
        }

        /**
         * Stops the playback
         */
        public void Stop()
        {
            if (_dataSource != null)
                Bridge4DS.Stop(_dataSource.FDVUUID);
            _isPlaying = false;
        }

        /**
         * stop and unitialize the sequence
         */
        public void Close()
        {
            Stop();
            Uninitialize();
        }

        /**
         * reach a specific mesh frame
	     * @param frame : frame number looked for. Must be in the active range
	     */
        public void GotoFrame(int frame)
        {
            bool wasPlaying = _isPlaying;
            Play(false);
            Bridge4DS.GotoFrame(_dataSource.FDVUUID, frame);
            prevFrame = CurrentFrame;
            Play(wasPlaying);
            UpdateMesh();
        }

        /**
         * reach a specific mesh frame defined by an event
	     * @param name : event name looked for. Must be in the active range. If two events have the same name, it reaches at random
	     */
        public void GotoFrame(string name)
        {
            int frame = 0;
            if (_events_to_frame.TryGetValue(name, out frame))
                GotoFrame(frame);
            else
                UnityEngine.Debug.LogError("Error GotoFrame : unknown event '" + name + "'");
        }


        /**
         * update the preview mesh in the editor
         */
        public void Preview()
        {
            if (_sourceType == SOURCE_TYPE.Network && _sequenceName.Length > 7 && _sequenceName.Substring(0, 7) == "holosys")
            {
                Texture2D live = Resources.Load<Texture2D>("4DViews/LogoLive");
                Shader particle = Shader.Find("Particles/Standard Unlit");
                var tempMaterial = new Material(GetComponent<Renderer>().sharedMaterial)
                {
                    mainTexture = live
                };
                tempMaterial.shader = particle;
                GetComponent<Renderer>().sharedMaterial = tempMaterial;

                return;
            }


            //save params values
            int nbGeometryTMP = _nbGeometryBuffers;
            int nbTextureTMP = _nbTextureBuffers;
            bool debugInfoTMP = _debugInfo;

            //set params values for preview
            _nbGeometryBuffers = 1;
            _nbTextureBuffers = 1;
            _debugInfo = false;

            if (_isInitialized && _dataSource == null)
                _isInitialized = false;

            //get the sequence
            Initialize();

            if (_isInitialized)
            {
                //set mesh to the preview frame
                GotoFrame(_previewFrame);
                Update();
                //Assign current texture to new material to have it saved
                var tempMaterial = new Material(_rendererComponent.sharedMaterial)
                {
                    mainTexture = _rendererComponent.sharedMaterial.mainTexture
                };
                _rendererComponent.sharedMaterial = tempMaterial;
            }

            //restore params values
            _nbGeometryBuffers = nbGeometryTMP;
            _nbTextureBuffers = nbTextureTMP;
            _debugInfo = debugInfoTMP;

            //look for trackings to update
            var tracking_node = this.transform.Find(TRACKING_NODE_NAME);
            if (tracking_node != null)
            {
                var animator = tracking_node.GetComponent<Animator>();
                var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
                animator.Update(_previewFrame / Framerate);
                if (clipInfo != null && clipInfo.Length > 0)
                {
                    for (int i = 0; i < clipInfo.Length; i++)
                    {
                        var clip = clipInfo[i].clip;
                        clip.SampleAnimation(tracking_node.gameObject, _previewFrame / Framerate);
                    }
                }
            }
        }

        #endregion

#region Unity methods

        void OnDestroy()
        {
            Close();
        }


        void Awake()
        {
            if (_isInitialized)
                Uninitialize();

            if (_sequenceName != "")
                Initialize();

            //Hide preview mesh
            if (_meshComponent != null)
                _meshComponent.mesh = null;

#if UNITY_EDITOR
            EditorApplication.pauseStateChanged +=HandlePauseState;
#endif
        }


        void Start()
        {
            if (!_isInitialized &&   _sequenceName != "") 
                Initialize();

            if (_dataSource == null)
                return;

            //launch sequence play
            if (_autoPlay){
                Play(true);
            }
        }



#if UNITY_IOS || UNITY_EDITOR
    private void OnApplicationFocus(bool focus)
    {
        if (focus) {
            if (wasPlayingWhenFocusLost)
                Play(true);
        } else {
            wasPlayingWhenFocusLost = IsPlaying;
            Play(false);
        }
    }
#endif

#if UNITY_ANDROID
    private void OnApplicationPause(bool pause)
    {
        if (pause) {
            wasPlayingWhenFocusLost = IsPlaying;
            Play(false);
        } else {
            if (wasPlayingWhenFocusLost)
                Play(true);
        }
    }
#endif

        //Called every frame
        //Get the geometry from the plugin and update the unity gameobject mesh and texture
        void Update()
        {
            if (!_isInitialized && _sequenceName != "")
                Initialize();

            if (_dataSource == null)
            {
                UnityEngine.Debug.LogError("No data source");
                return;
            }

            //call native code 
            UpdateMesh();

#if UNITY_EDITOR
            //called when the step button in editor is clicked
            if (EditorApplication.isPaused)
            {
                GotoFrame((GetCurrentFrame() + 1) % GetSequenceNbFrames());
            }
#endif
            // adjust speed unity time scale if needed
            if (Time.timeScale != unityTimeScale)
            {
                unityTimeScale = Time.timeScale;
                Bridge4DS.SetSpeed(_dataSource.FDVUUID, _speedRatio * unityTimeScale);
            }

            if (_newMeshAvailable)
            {
                //Get current object buffers (double buffering)
                Mesh mesh = _meshes[_currentGeometryBuffer];

                //Optimize mesh for frequent updates. Call this before assigning vertices.
                //Seems to be useless :(
                mesh.MarkDynamic();

                //Update geometry
                mesh.vertices = _newVertices;
                if (_nbTriangles == 0)  //case empty mesh
                    mesh.triangles = null;
                else
                    mesh.triangles = _newTriangles;
                
                if (_newUVs != null) mesh.uv = _newUVs;
                if (_newNormals != null) mesh.normals = _newNormals;
                if (_newVelocities != null) mesh.SetUVs(5, _newVelocities);
                
                if (_textures != null)
                { 
                    Texture2D texture = _textures[_currentTextureBuffer];

                    //Update texture
                    texture.LoadRawTextureData(_newTextureData);
                    texture.Apply();

                    if (_rendererComponent.sharedMaterial.HasProperty("_BaseMap"))
                        _rendererComponent.sharedMaterial.SetTexture("_BaseMap", texture);
                    else if (_rendererComponent.sharedMaterial.HasProperty("_BaseColorMap"))
                        _rendererComponent.sharedMaterial.SetTexture("_BaseColorMap", texture);
                    else if (_rendererComponent.sharedMaterial.HasProperty("_UnlitColorMap"))
                        _rendererComponent.sharedMaterial.SetTexture("_UnlitColorMap", texture);
                    else
                    {
#if UNITY_EDITOR
                    var tempMaterial = new Material(_rendererComponent.sharedMaterial);
                    tempMaterial.mainTexture = texture;
                    _rendererComponent.sharedMaterial = tempMaterial;
#else
                        _rendererComponent.material.mainTexture = texture;
#endif
                    }
                }
                else
                {
                    mesh.colors32 = _newColors;
                }

                mesh.UploadMeshData(false); //Good optimization ! nbGeometryBuffers must be = 1

                //Assign current mesh buffers and texture
                _meshComponent.sharedMesh = mesh;

                //Switch buffers
                _currentGeometryBuffer = (_currentGeometryBuffer + 1) % _nbGeometryBuffers;
                _currentTextureBuffer = (_currentTextureBuffer + 1) % _nbTextureBuffers;

                //Send event
                OnNewModel?.Invoke();

                if (IsPlaying && (CurrentFrame == LastActiveFrame || CurrentFrame < prevFrame)) {
                    // OnLastFrame?.Invoke();
                    if (!_loop)
                        Stop();
                }
                prevFrame = CurrentFrame;

                _newMeshAvailable = false;

                if (_meshCollider && _meshCollider.enabled)
                    _meshCollider.sharedMesh = mesh;
                //_updateCollider = !_updateCollider;

                _totalFramesPlayed++;
                if (_debugInfo)
                {
                    double timeInMSeconds = System.DateTime.Now.Subtract(_lastUpdatingTime).TotalMilliseconds;
                    _lastUpdatingId++;
                    if (timeInMSeconds > 500f)
                    {
                        _updatingFPS = (float)((float)(_lastUpdatingId) / timeInMSeconds * 1000f);
                        _lastUpdatingTime = System.DateTime.Now;
                        _lastUpdatingId = 0;
                    }
                }
            }
        }

        #endregion

#region private methods
        private void InitAudio()
        {
            //setup audio if there is one inside the 4ds file
            int audioSize = Bridge4DS.GetAudioBufferSize(_dataSource.FDVUUID);
            if (audioSize > 0)
            {
                GameObject audioNode;
                AudioSource audioSource;

                //check if audio node already exists
                Sync4DS sync = GetComponent<Sync4DS>();
                if (sync._audioSources.Count > 0 && sync._audioSources[0].audioSource)
                {
                    audioSource = sync._audioSources[0].audioSource;
                    audioNode = sync._audioSources[0].audioSource.gameObject;
                }
                else
                {
                    audioNode = new GameObject("Audio4DS");
                    audioNode.transform.SetParent(this.transform.parent, false);
                    audioSource = audioNode.AddComponent<AudioSource>();
                }

                int nbSamples = Bridge4DS.GetAudioNbSamples(_dataSource.FDVUUID);
                samples = new float[nbSamples * Bridge4DS.GetAudioNbChannels(_dataSource.FDVUUID)];
                audioBufferHandle = GCHandle.Alloc(samples, GCHandleType.Pinned);

                Bridge4DS.GetAudioBuffer(_dataSource.FDVUUID, audioBufferHandle.AddrOfPinnedObject());

                audioSource.clip = AudioClip.Create("audioInside4ds", nbSamples, Bridge4DS.GetAudioNbChannels(_dataSource.FDVUUID), Bridge4DS.GetAudioSampleRate(_dataSource.FDVUUID), false);
                audioSource.clip.SetData(samples, 0);

                sync._audioSources[0].audioSource = audioSource;
                sync.enabled = true;
            }
        }

        private void UpdateMesh()
        {
            if (_dataSource == null || !_isInitialized)
                return;

            bool lookAtEnabled;
            var lookat = GetComponent<LookAt>();
            if (_isPlaying && lookat != null && lookat.enabled)
                lookAtEnabled = true;
            else
                lookAtEnabled = false;

            //Get the new model
            int modelId = Bridge4DS.UpdateModel(_dataSource.FDVUUID,
                                                      _newVerticesHandle.AddrOfPinnedObject(),
                                                      _newUVsHandle.IsAllocated?_newUVsHandle.AddrOfPinnedObject():System.IntPtr.Zero,
                                                      _newTrianglesHandle.AddrOfPinnedObject(),
                                                      _newTextureDataHandle.IsAllocated? _newTextureDataHandle.AddrOfPinnedObject(): System.IntPtr.Zero,
                                                      _newNormalsHandle.IsAllocated?_newNormalsHandle.AddrOfPinnedObject(): System.IntPtr.Zero,
                                                      _newVelocitiesHandle.IsAllocated?_newVelocitiesHandle.AddrOfPinnedObject(): System.IntPtr.Zero,
                                                      _newBBoxHandle.IsAllocated?_newBBoxHandle.AddrOfPinnedObject(): System.IntPtr.Zero,
                                                      _newColorsHandle.IsAllocated?_newColorsHandle.AddrOfPinnedObject(): System.IntPtr.Zero,
                                                      _lastModelId,
                                                      ref _nbVertices,
                                                      ref _nbTriangles,
                                                      lookAtEnabled,
                                                      LookAtTarget,
                                                      LookAtMaxAngle);

            Mesh mesh = _meshes[_currentGeometryBuffer];
            mesh.bounds = new Bounds( (_newBBox[0] + _newBBox[1]) / 2.0f, (_newBBox[1] - _newBBox[0]));

			if (_boxCollider && _boxCollider.enabled) {
            	_boxCollider.center = mesh.bounds.center;
            	_boxCollider.size = mesh.bounds.size;
			}

            //Check if there is model
            if (!_newMeshAvailable)
                _newMeshAvailable = (modelId != -1 && modelId != _lastModelId);

            if (modelId == -1) modelId = _lastModelId;
            else _lastModelId = modelId;

            int nb_new_events = Bridge4DS.PullNewEvents(_dataSource.FDVUUID);
            for (int i=0; i<nb_new_events; ++i) {
                int typeEvent = 0;
                int eventFrame = Bridge4DS.GetEvent(_dataSource.FDVUUID, i, _newEventHandle.AddrOfPinnedObject(), ref typeEvent);
                _newEventString = Marshal.PtrToStringAnsi(_newEventHandle.AddrOfPinnedObject());

                switch (typeEvent)
                {
                    case 254:
                        OnFirstFrame.Invoke(eventFrame);
                        break;
                    case 255:
                        OnLastFrame.Invoke(eventFrame);
                        break;
                    default:
                        OnUserEvent.Invoke(eventFrame, _newEventString);
                        break;
                }
            }

            if (_debugInfo)
            {
                double timeInMSeconds = System.DateTime.Now.Subtract(_lastDecodingTime).TotalMilliseconds;
                if (_lastDecodingId == 0 || timeInMSeconds > 500f)
                {
                    _decodingFPS = (float)((double)(Mathf.Abs((float)(modelId - _lastDecodingId))) / timeInMSeconds) * 1000f;
                    _lastDecodingTime = System.DateTime.Now;
                    _lastDecodingId = modelId;
                }
            }
        }


#if UNITY_EDITOR
        private void HandlePauseState(PauseState state)
        {
            Play(state>0);
        }
#endif

        private int GetSequenceNbFrames()
        {
            if (_dataSource != null)
                return Bridge4DS.GetSequenceNbFrames(_dataSource.FDVUUID);
            else
                return _nbFrames;
        }

        private int GetActiveNbFrames()
        {
            return (int)_activeRangeMax - (int)_activeRangeMin + 1;
        }

        private int GetCurrentFrame()
        {
            if (_lastModelId < 0)
                return 0;
            else
                return _lastModelId;
        }

        private float GetFrameRate()
        {
            return (_dataSource == null) ? 0.0f : _dataSource.FrameRate;
        }

        private TextureFormat GetTextureFormat()
        {
            return _dataSource.TextureFormat;
        }


        void OnGUI()
        {
            if (_debugInfo)
            {
                double delay = System.DateTime.Now.Subtract(_playDate).TotalMilliseconds - ((float)(_totalFramesPlayed) * 1000 / GetFrameRate());
                string decoding = _decodingFPS.ToString("00.00") + " fps";
                string updating = _updatingFPS.ToString("00.00") + " fps";
                delay /= 1000;
                if (!_isPlaying)
                {
                    delay = 0f;
                    decoding = "paused";
                    updating = "paused";
                }
                int top = 20;
                GUIStyle title = new GUIStyle();
                title.normal.textColor = Color.white;
                title.fontStyle = FontStyle.Bold;
                GUI.Button(new Rect(Screen.width - 210, top - 10, 200, 330), "");
                GUI.Label(new Rect(Screen.width - 200, top, 190, 20), "Sequence ", title);
                GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20), "Length: " + ((float)GetSequenceNbFrames() / GetFrameRate()).ToString("00.00") + " sec");
                GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20), "Nb Frames: " + GetSequenceNbFrames() + " frames");
                GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20), "Frame rate: " + GetFrameRate().ToString("00.00") + " fps");
                GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20), "Max vertices: " + _dataSource.MaxVertices);
                GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20), "Max triangles: " + _dataSource.MaxTriangles);
                GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20), "Texture format: " + _dataSource.TextureFormat);
                GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20), "Texture size: " + _dataSource.TextureSize + "x" + _dataSource.TextureSize + "px");
                GUI.Label(new Rect(Screen.width - 200, top += 25, 190, 20), "Current Mesh", title);
                GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20), "Nb vertices: " + _nbVertices);
                GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20), "Nb triangles: " + _nbTriangles);
                GUI.Label(new Rect(Screen.width - 200, top += 25, 190, 20), "Playback", title);
                GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20), "Time: " + ((float)(CurrentFrame) / GetFrameRate()).ToString("00.00") + " sec");
                GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20), "Decoding rate: " + decoding);
                GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20), "Decoding delay: " + delay.ToString("00.00") + " sec");
                GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20), "Updating rate: " + updating);
            }
        }

        private void ConvertPreviewTexture()
        {
            if (_sourceType == SOURCE_TYPE.Network && _sequenceName.Length > 7 && _sequenceName.Substring(0, 7) == "holosys")
                return;

            System.DateTime current_time = System.DateTime.Now;
            if (_rendererComponent != null && _rendererComponent.sharedMaterial.mainTexture != null)
            {
                if (((System.TimeSpan)(current_time - last_preview_time)).TotalMilliseconds < 1000
                    || ((Texture2D)_rendererComponent.sharedMaterial.mainTexture).format == TextureFormat.DXT1)
                    return;

                last_preview_time = current_time;

                if (_rendererComponent != null)
                {
                    Texture2D tex = (Texture2D)_rendererComponent.sharedMaterial.mainTexture;
                    if (tex && tex.format != TextureFormat.RGBA32)
                    {
                        Color32[] pix = tex.GetPixels32();
                        Texture2D textureRGBA = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false)
                        {
                            wrapMode = TextureWrapMode.Clamp
                        };
                        textureRGBA.SetPixels32(pix);
                        textureRGBA.Apply();

                        _rendererComponent.sharedMaterial.mainTexture = textureRGBA;
                    }
                }
            }
        }


        private void AllocateGeometryBuffers(ref Vector3[] verts, ref Vector2[] uvs, ref Vector3[] norms, ref Vector3[] vels, ref Vector3[] bbox, ref int[] tris, ref Color32[] colors, int nbMaxVerts, int nbMaxTris)
        {
            verts = new Vector3[nbMaxVerts];
            tris = new int[nbMaxTris * 3];
            if (_dataSource.ColorPerVertex)
                colors = new Color32[nbMaxVerts];
            else
            {
                uvs = new Vector2[nbMaxVerts];
                norms = new Vector3[nbMaxVerts];
                vels = new Vector3[nbMaxVerts];
                bbox = new Vector3[2];
            }
        }


#if UNITY_EDITOR
        private void CreateTrackings()
        {
            //_isTrackingsCreated = true;
            int nb_trackings = Bridge4DS.GetNbTrackings(_dataSource.FDVUUID);
            if (nb_trackings == 0)
                return;

            GameObject tracking_node = new GameObject(TRACKING_NODE_NAME);
            tracking_node.transform.SetParent(this.transform, false);

            var sequence_name = _sequenceName.Substring(0, _sequenceName.Length - 4);
            var clip = UnityEditor.Animations.AnimatorController.AllocateAnimatorClip("4DVCurves");
            for (int id_t = 0; id_t < nb_trackings; id_t++) {
                CreateTracking(clip, id_t);
            }

            if (!AssetDatabase.IsValidFolder("Assets/Trackings"))
                AssetDatabase.CreateFolder("Assets", "Trackings");

            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath("Assets/Trackings/"+ sequence_name +"Controller.controller");

            Animator animator = tracking_node.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            AssetDatabase.AddObjectToAsset(clip, controller);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(clip));
            controller.AddMotion(clip);

            var sync = GetComponent<Sync4DS>();
            sync.enabled = true;
            var animSrc = new AnimationSource4DS();
            animSrc.animationSource = animator;
            sync._animationSources.Add(animSrc);
        }

        private void CreateTracking(AnimationClip clip, int id_t)
        {
            int firstFrame=0;
            int lastFrame=0;
            int rotationType=0;
            string trackingName = new string(' ', 100) + id_t;
            GCHandle tNameHandle = GCHandle.Alloc(trackingName, GCHandleType.Pinned);

            Bridge4DS.GetTrackingInfos(_dataSource.FDVUUID, id_t, ref firstFrame, ref lastFrame, ref rotationType, tNameHandle.AddrOfPinnedObject());
            trackingName = Marshal.PtrToStringAnsi(tNameHandle.AddrOfPinnedObject());
            trackingName += id_t;

            int trackNbFrames = lastFrame - firstFrame + 1;
            Vector3[] positions = new Vector3[trackNbFrames];
            Vector4[] rotations = new Vector4[trackNbFrames];
            GCHandle posHandle = GCHandle.Alloc(positions, GCHandleType.Pinned);
            GCHandle rotHandle = GCHandle.Alloc(rotations, GCHandleType.Pinned);

            Bridge4DS.GetTrackingBuffers(_dataSource.FDVUUID, id_t, posHandle.AddrOfPinnedObject(), rotHandle.AddrOfPinnedObject());

            AnimationCurve translateX = new AnimationCurve();
            AnimationCurve translateY = new AnimationCurve();
            AnimationCurve translateZ = new AnimationCurve();
            AnimationCurve rotateX = new AnimationCurve();
            AnimationCurve rotateY = new AnimationCurve();
            AnimationCurve rotateZ = new AnimationCurve();
            AnimationCurve rotateW = new AnimationCurve();
            float fps = Framerate;
            for (int i=0; i< trackNbFrames; i++)
            {
                translateX.AddKey(new Keyframe(i / fps, positions[i].x));
                translateY.AddKey(new Keyframe(i / fps, positions[i].y));
                translateZ.AddKey(new Keyframe(i / fps, positions[i].z));
                rotateX.AddKey(new Keyframe(i / fps, rotations[i].x));
                rotateY.AddKey(new Keyframe(i / fps, rotations[i].y));
                rotateZ.AddKey(new Keyframe(i / fps, rotations[i].z));
                rotateW.AddKey(new Keyframe(i / fps, rotations[i].w));
            }

            //AnimationClip clip = new AnimationClip();
            clip.SetCurve(trackingName, typeof(Transform), "localPosition.x", translateX);
            clip.SetCurve(trackingName, typeof(Transform), "localPosition.y", translateY);
            clip.SetCurve(trackingName, typeof(Transform), "localPosition.z", translateZ);
            clip.SetCurve(trackingName, typeof(Transform), "localRotation.x", rotateX);
            clip.SetCurve(trackingName, typeof(Transform), "localRotation.y", rotateY);
            clip.SetCurve(trackingName, typeof(Transform), "localRotation.z", rotateZ);
            clip.SetCurve(trackingName, typeof(Transform), "localRotation.w", rotateW);
            //clip.legacy = true;

            GameObject trackGO = new GameObject(trackingName);
            trackGO.transform.parent = this.transform.Find(TRACKING_NODE_NAME);

            posHandle.Free();
            rotHandle.Free();
        }

        private void FillEventsList()
        {
            _listEvents.Clear();
            _events_to_frame.Clear();
            int nb_event = Bridge4DS.GetSizeEventList(_dataSource.FDVUUID);

            string event_name = new string(' ', 100);
            GCHandle eventNameHandle = GCHandle.Alloc(event_name, GCHandleType.Pinned);
            for (int i=0; i<nb_event; ++i) {
                int event_frame = Bridge4DS.GetEventFromList(_dataSource.FDVUUID, i, eventNameHandle.AddrOfPinnedObject());
                event_name = Marshal.PtrToStringAnsi(eventNameHandle.AddrOfPinnedObject());

                _listEvents.Add(event_frame, event_name);

                try {
                    _events_to_frame.Add(event_name, event_frame);
                } catch(System.ArgumentException) {}
            }
            eventNameHandle.Free();

            _listEvents.Sort();
        }
#endif 
    }
#endregion

    }

