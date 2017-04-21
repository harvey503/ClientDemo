using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
namespace NvrVideoPlayer
{

    public partial class VideoForm : UserControl
    {
        int m_nIndex;   //index	
        bool m_bRecord; //is recording or not
        bool m_bSound;

        public int m_iPlayhandle;   //play handle
        public int m_lLogin; //login handle
        public int m_iChannel; //play channel
        public int m_iTalkhandle;

        public void SetWndIndex(int nIndex)
        {
            m_nIndex = nIndex;
        }
        public int ConnectRealPlay(ref DEV_INFO pDev, int nChannel)
        {
            if (m_iPlayhandle != -1)
            {

                if (0 != XMSDK.H264_DVR_StopRealPlay(m_iPlayhandle, (uint)this.Handle))
                {
                    //TRACE("H264_DVR_StopRealPlay fail m_iPlayhandle = %d", m_iPlayhandle);
                }
                if (m_bSound)
                {
                    OnCloseSound();
                }
            }

            H264_DVR_CLIENTINFO playstru = new H264_DVR_CLIENTINFO();

            playstru.nChannel = nChannel;
            playstru.nStream = 0;
            playstru.nMode = 0;
            playstru.hWnd = this.Handle;
            m_iPlayhandle = XMSDK.H264_DVR_RealPlay(pDev.lLoginID, ref playstru);
            if (m_iPlayhandle <= 0)
            {
                Int32 dwErr = XMSDK.H264_DVR_GetLastError();
                StringBuilder sTemp = new StringBuilder("");
                sTemp.AppendFormat("access {0} channel{1} fail, dwErr = {2}", pDev.szDevName, nChannel, dwErr);
                MessageBox.Show(sTemp.ToString());
            }
            else
            {
                XMSDK.H264_DVR_MakeKeyFrame(pDev.lLoginID, nChannel, 0);
            }
            m_lLogin = pDev.lLoginID;
            m_iChannel = nChannel;
            return m_iPlayhandle;
        }

        public void GetColor(out int nBright, out int nContrast, out int nSaturation, out int nHue)
        {
            if (m_iPlayhandle <= 0)
            {
                nBright = -1;
                nContrast = -1;
                nSaturation = -1;
                nHue = -1;
                return;
            }
            uint nRegionNum = 0;
            XMSDK.H264_DVR_LocalGetColor(m_iPlayhandle, nRegionNum, out nBright, out nContrast, out nSaturation, out nHue);
        }
        public void SetColor(int nBright, int nContrast, int nSaturation, int nHue)
        {
            XMSDK.H264_DVR_LocalSetColor(m_iPlayhandle, 0, nBright, nContrast, nSaturation, nHue);
        }

        public int GetHandle()
        {
            return m_iPlayhandle;
        }
        public bool OnOpenSound()
        {
            if (XMSDK.H264_DVR_OpenSound(m_iPlayhandle))
            {
                m_bSound = true;
                return true;
            }
            return false;
        }
        public bool OnCloseSound()
        {
            if (XMSDK.H264_DVR_CloseSound(m_iPlayhandle))
            {
                m_bSound = false;
                return true;
            }
            return false;
        }
        public bool SaveRecord()
        {
            if (m_iPlayhandle <= 0)
            {
                return false;
            }

            DateTime time = DateTime.Now;
            String cFilename = String.Format(@"{0}\\record\\{1}{2}{3}_{4}{5}{6}.h264",
                                                        "c:",
                                                        time.Year,
                                                        time.Month,
                                                        time.Day,
                                                        time.Hour,
                                                        time.Minute,
                                                        time.Second);
            if (m_bRecord)
            {

                if (XMSDK.H264_DVR_StopLocalRecord(m_iPlayhandle))
                {
                    m_bRecord = false;
                    MessageBox.Show(@"stop record OK.");
                }
            }
            else
            {
                int nTemp = 0;
                string strPr = "\\";
                for (;;)
                {
                    int nIndex = cFilename.IndexOfAny(strPr.ToCharArray(), nTemp);
                    if (nIndex == -1)
                    {
                        break;
                    }
                    String str = cFilename.Substring(0, nIndex + 1);
                    nTemp = nIndex + 1; nTemp = nIndex + 1;
                    DirectoryInfo dir = new DirectoryInfo(str);
                    if (!dir.Exists)
                    {
                        dir.Create();
                    }

                }

                if (XMSDK.H264_DVR_StartLocalRecord(m_iPlayhandle, cFilename, (int)MEDIA_FILE_TYPE.MEDIA_FILE_NONE))
                {
                    m_bRecord = true;
                    MessageBox.Show(@"start record OK.");
                }
                else
                {
                    MessageBox.Show(@"start record fail.");
                }
            }

            return true;
        }

        public int GetLoginHandle()
        {
            return m_lLogin;
        }
        public void OnDisconnct()
        {
            if (m_iPlayhandle > 0)
            {
                XMSDK.H264_DVR_StopRealPlay(m_iPlayhandle, (uint)this.Handle);
                m_iPlayhandle = -1;

            }
            if (m_bSound)
            {
                OnCloseSound();
            }
            m_lLogin = -1;
        }


        public void drawOSD(int nPort, IntPtr hDc)
        {
            if (m_strInfoFrame[nPort] != "")
            {
                //改变字体颜色
                FontFamily fontfamily = new FontFamily(@"Arial");
                Font newFont = new Font(fontfamily, 16, FontStyle.Bold);
                SolidBrush brush = new SolidBrush(Color.Red);


                Graphics graphic = Graphics.FromHdc(hDc);
                graphic.DrawString("TEST", newFont, brush, 10, 10);
            }
        }

        public int SetDevChnColor(ref SDK_CONFIG_VIDEOCOLOR pVideoColor)
        {
            IntPtr ptr = new IntPtr();
            Marshal.StructureToPtr(pVideoColor, ptr, true);
            return XMSDK.H264_DVR_SetDevConfig(m_lLogin, (uint)SDK_CONFIG_TYPE.E_SDK_VIDEOCOLOR, m_iChannel, ptr, (uint)Marshal.SizeOf(pVideoColor), 3000);

        }
        static void videoInfoFramCallback(int nPort, int nType, string pBuf, int nSize, IntPtr nUser)
        {
            //收到信息帧, 0x03 代表GPRS信息

            if (nType == 0x03)
            {
                VideoForm form = new VideoForm();
                Marshal.PtrToStructure(nUser, form);
                form.m_strInfoFrame[nPort] = pBuf;
            }
        }
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string[] m_strInfoFrame;


        public VideoForm()
        {
            InitializeComponent();
            InitSDK();
           
        }
        
        private void VideoForm_Load(object sender, EventArgs e)
        {

        }
        public void Init(string serialNumber)
        {
            DEV_INFO devInfo = new DEV_INFO();
            devInfo.nPort = 34567;//
            devInfo.lLoginID = -1;
            devInfo.szIpaddress = serialNumber;// "d54126364d7c245d";//
            devInfo.szUserName = "admin";
            devInfo.szPsw = "";
            Connect(ref devInfo, 0, 0);
        }

        private int Connect(ref DEV_INFO pDev, int nChannel, int nWndIndex)
        {
            int nRet = 0;

            //if device did not login,login first
            if (pDev.lLoginID <= 0)
            {
                H264_DVR_DEVICEINFO OutDev;
                int nError = 0;
                //设置尝试连接设备次数和等待时间
                int lLogin = XMSDK.H264_DVR_Login_Cloud(pDev.szIpaddress, (ushort)pDev.nPort, pDev.szUserName, pDev.szPsw, out OutDev, out nError, null);
                if (lLogin <= 0)
                {

                    int nErr = XMSDK.H264_DVR_GetLastError();
                    if (nErr == (int)SDK_RET_CODE.H264_DVR_PASSWORD_NOT_VALID)
                    {
                        MessageBox.Show(("Error.PwdErr"));
                    }
                    else
                    {
                        MessageBox.Show(("Error.NotFound"));
                    }

                    return nRet;
                }

                pDev.lLoginID = lLogin;
                //XMSDK.H264_DVR_SetupAlarmChan(lLogin);
            }
            return ConnectRealPlay(ref pDev, nChannel);
        }

        private int InitSDK()
        {
            //initialize

            string uuid = "0460KMYTDZJS01704183063916250081";
            int uuidResult = XMSDK.H264_DVR_Set_UUid(out uuid);
            int bResult = XMSDK.H264_DVR_Init(null, this.Handle);
            XMSDK.H264_DVR_SetConnectTime(5000, 3);

            return bResult;
        }
    }
}
