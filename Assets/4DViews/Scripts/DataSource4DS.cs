
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Networking;
using System.IO;

//-----------------DataSource4DS-----------------//
//Creates a 4D sequence from a path.
//If the path is a directory, DataSource4DS looks for the best supported format.
//If path is a sequence.xml file, DataSource4DS creates directly a sequence
//from this file without checking format compatibility.

namespace unity4dv
{


    public class DataSource4DS
    {
        public int FDVUUID;
        public TextureFormat TextureFormat;
        public int TextureSize;
        public bool ColorPerVertex = false;
        public int MaxVertices;
        public int MaxTriangles;
        public float FrameRate;

        //Static constructor: creates a data source or returns null when no source can be created
        static public DataSource4DS CreateDataSource(int key, string sequenceName, bool dataInStreamingAssets, string mainPath, int activeRangeBegin, int activeRangeLastFrame, OUT_RANGE_MODE outRangeMode)
        {
            bool success = false;
            string errorMsg = new string(' ',300);
            string rootpath;
            
            if (!(sequenceName.StartsWith("http") || sequenceName.StartsWith("holosys")) && key == 0 && dataInStreamingAssets)
            {
                rootpath = Application.streamingAssetsPath + "/" + mainPath + sequenceName;

                //ANDROID STREAMING ASSETS => need to copy the data somewhere else on device to acces it, beacause it is currently in jar file
                if (rootpath.StartsWith("jar"))
                {
                    if (!File.Exists(Application.persistentDataPath + "/" + sequenceName))
                    {
                        UnityWebRequest www = UnityWebRequest.Get(rootpath);
                        www.downloadHandler = new DownloadHandlerFile(Application.persistentDataPath + "/" + sequenceName);
                        www.SendWebRequest();
                        //yield return www; //can't do yield here, not really blocking beacause the data is local
                        while (!www.isDone) ;

                        if (!string.IsNullOrEmpty(www.error))
                        {
                            Debug.LogError("PATH : " + rootpath);
                            Debug.LogError("Can't read data in streaming assets: " + www.error);
                        }
                        else
                        {
                            if (File.Exists(Application.persistentDataPath + "/" + sequenceName))
                            {
                                rootpath = Application.persistentDataPath + "/" + sequenceName;
                                Debug.LogError("File now exists at " + Application.persistentDataPath + "/" + sequenceName);
                            }
                            else
                            {
                                Debug.LogError("File does not exist at " + Application.persistentDataPath + "/" + sequenceName);
                            }
                        }
                    }
                    else
                    {
                        rootpath = Application.persistentDataPath + "/" + sequenceName;
                    }

                }

                DataSource4DS instance = new DataSource4DS(key, rootpath, activeRangeBegin, activeRangeLastFrame, outRangeMode, ref success, ref errorMsg);
                if (success)
                {
                    return instance;
                }
                else
                {
                    Debug.LogError("FDV Error: cannot open 4ds file at location " + rootpath);
                    Debug.LogError(errorMsg);
                    return null;
                }

            }
            else
            {
                if (sequenceName.StartsWith("http") || sequenceName.StartsWith("holosys"))
                    rootpath = sequenceName;
                else
                    rootpath = mainPath + sequenceName;
                DataSource4DS instance = new DataSource4DS(key, rootpath, activeRangeBegin, activeRangeLastFrame, outRangeMode, ref success, ref errorMsg);
                if (success)
                    return instance;
                else
                {
                    Debug.LogError("FDV Error: cannot open file at location " + rootpath);
                    Debug.LogError(errorMsg);
                    return null;
                }
            }
        }

        //private constructor
        private DataSource4DS(int key, string rootpath, int activeRangeBegin, int activeRangeEnd, OUT_RANGE_MODE outRangeMode, ref bool success, ref string errorMsg)
        {
            this.FDVUUID = 0;
            success = true;

            //Create sequence with native plugin
            GCHandle errorMsgHandle = GCHandle.Alloc(errorMsg, GCHandleType.Pinned);
            this.FDVUUID = Bridge4DS.CreateSequence(key, rootpath, activeRangeBegin, activeRangeEnd, outRangeMode, errorMsgHandle.AddrOfPinnedObject());
            errorMsg = Marshal.PtrToStringAnsi(errorMsgHandle.AddrOfPinnedObject());


            if (this.FDVUUID == 0)
                success = false;

            //Get sequence info
            if (success)
            {

#if (UNITY_ANDROID || UNITY_IOS || UNITY_LUMIN) && !UNITY_EDITOR
                Bridge4DS.AddASTCSupport(this.FDVUUID);
#else
                Bridge4DS.AddDXTSupport(this.FDVUUID);
#endif

                this.TextureSize = Bridge4DS.GetTextureSize(this.FDVUUID);
                if (this.TextureSize == 0)
                    this.TextureSize = 1024;    //put 1024 by default => will crash if we have 2048 texture and it's not written in xml fi

                int textureEncoding = Bridge4DS.GetTextureEncoding(this.FDVUUID);

                switch (textureEncoding)
                {
                    case 5:
					case 120:
                        this.TextureFormat = TextureFormat.ETC_RGB4;
                        break;
                    case 6:
					case 130:
                        this.TextureFormat = TextureFormat.PVRTC_RGB4;
                        break;
                    case 4:
					case 131:
                        this.TextureFormat = TextureFormat.PVRTC_RGB2;
                        break;
                    case 1:
					case 100:
#if (UNITY_ANDROID || UNITY_IOS || UNITY_LUMIN) && !UNITY_EDITOR
                        this.TextureFormat = TextureFormat.RGBA32;
#else
                        this.TextureFormat = TextureFormat.DXT1;
#endif
                        break;
                    case 8:
					case 164:
#if (UNITY_ANDROID || UNITY_IOS || UNITY_LUMIN) && !UNITY_EDITOR
                        this.TextureFormat = TextureFormat.ASTC_8x8; 
#else
                        this.TextureFormat = TextureFormat.RGBA32; 
#endif

                        break;
                    case 190:
                        this.TextureFormat = 0;
                        this.ColorPerVertex = true;
                        break;

                    default:
#if UNITY_IOS
				this.TextureFormat = TextureFormat.PVRTC_RGB4;
#elif UNITY_ANDROID
                    this.TextureFormat = TextureFormat.ETC_RGB4;
#else
                        this.TextureFormat = TextureFormat.DXT1;
#endif
                        break;
                }

                this.MaxVertices = Bridge4DS.GetSequenceMaxVertices(this.FDVUUID);
                if (this.MaxVertices == 0)
                    this.MaxVertices = 65535;
                this.MaxTriangles = Bridge4DS.GetSequenceMaxTriangles(this.FDVUUID);
                if (this.MaxTriangles == 0)
                    this.MaxTriangles = 65535;
                this.FrameRate = (float)Bridge4DS.GetSequenceFramerate(this.FDVUUID);
            }
        }
 
    }

}