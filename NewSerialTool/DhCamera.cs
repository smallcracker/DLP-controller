using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GxIAPINET;



namespace VideoMode
{

    class DhCamera
    {
        static List<IGXDeviceInfo> listGXDeviceInfo;                ///<设备信息表  
        static IGXFactory m_objIGXFactory = null;                   ///<Factory对像
                                                                    ///

        IGXDevice m_objIGXDevice = null;                            ///<设备对像
        IGXStream m_objIGXStream = null;                            ///<流对像
        IGXFeatureControl m_objIGXFeatureControl = null;            ///<远端设备属性控制器对像
        bool m_bIsOpen = false;                                     ///<设备打开状态
        bool m_bIsSnap = false;                                     ///<发送开采命令标识                                             
        int m_nPayloadSize = 0;                                     ///<图像数据大小
        int CamIndex = 0;                                           ///<相机序号
        Bitmap CameraData;                                          ///<输出bitmap图像   
        static int OutCamIndex = 0;                                 ///<将相机的序号输出类外
        public delegate void OutPutData(int index, Bitmap objdata); ///<声明一个委托类型
        OutPutData inPutFunc;                                       ///<定义一个委托对象
        bool m_isStartGrab = false;                                 ///<标志是否开始了流通道采集                                                                   
        public bool m_bIsColor = false;                                    ///<是否支持彩色相机
                                                                            
        static public void initlib()
        {
            //初始化库
           IGXFactory.GetInstance().Init();
        }
        /// <summary>
        /// 反初始化库
        /// </summary>
        static public void uintlib()
        {
            //反初始化相机库
            IGXFactory.GetInstance().Uninit();
        }

        /// <summary>
        /// 获得链接相机的信息
        /// </summary>
        /// <returns></returns>
        public String GetInfo(int index)
        {
            String strs= "";
            List<IGXDeviceInfo> listGXDeviceInfo = new List<IGXDeviceInfo>();
            m_objIGXFactory = IGXFactory.GetInstance();
            IGXFactory.GetInstance().UpdateAllDeviceList(200, listGXDeviceInfo);
            String strSN = listGXDeviceInfo[index].GetSN();
            String strUserID = listGXDeviceInfo[index].GetModelName();
            strs = strSN+"   "+strUserID;

            return strs;
        }

        /// <summary>
        /// 获得链接相机的个数
        /// </summary>
        /// <returns></returns>
        static public int GetCamNumFunc()
        {
            try
            {
                listGXDeviceInfo = new List<IGXDeviceInfo>();
                ///
                m_objIGXFactory = IGXFactory.GetInstance();
                //枚举设备列表
                m_objIGXFactory.UpdateDeviceList(200, listGXDeviceInfo);
                OutCamIndex = listGXDeviceInfo.Count;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }
            return OutCamIndex;
        }
        public bool CamOpen()
        {
            bool opened = false;
            if (null != m_objIGXDevice)
            {
                opened = true;
            }
            return opened;
        }
        /// <summary>
        /// 打开设备打开流
        /// </summary>
        public void OpenCameraFunc(int index, OutPutData objFunc)
        {
            try
            {
                //判断当前连接的相机个数是否小于0
                if (listGXDeviceInfo.Count <= 0)
                {
                    MessageBox.Show("未发现设备！");
                    return;
                }
                // 如果设备已经打开则关闭，保证相机在初始化出错情况下能再次打开
                if (null != m_objIGXDevice)
                {
                    m_objIGXDevice.Close();
                    m_objIGXDevice = null;
                }

                //打开设备列表的第一个相机
                m_objIGXDevice = m_objIGXFactory.OpenDeviceBySN(listGXDeviceInfo[index].GetSN(), GX_ACCESS_MODE.GX_ACCESS_EXCLUSIVE);
                //获得一个属性控制器
                m_objIGXFeatureControl = m_objIGXDevice.GetRemoteFeatureControl();
                //打开流通道
                if (null != m_objIGXDevice)
                {
                    m_objIGXStream = m_objIGXDevice.OpenStream(0);
                }
                // a = objFunc;
                m_nPayloadSize = (int)m_objIGXDevice.GetRemoteFeatureControl().GetIntFeature("PayloadSize").GetValue();
                //获取是否为彩色相机
                string strValue = null;
                if (m_objIGXDevice.GetRemoteFeatureControl().IsImplemented("PixelColorFilter"))
                {
                    strValue = m_objIGXDevice.GetRemoteFeatureControl().GetEnumFeature("PixelColorFilter").GetValue();

                    if ("None" != strValue)
                    {
                        m_bIsColor = true;
                    }
                }
                CamIndex = index;
                inPutFunc = objFunc;
                m_bIsOpen = true;
                m_objIGXDevice.GetRemoteFeatureControl().GetEnumFeature("GainSelector").SetValue("AnalogAll");
                m_objIGXDevice.GetRemoteFeatureControl().GetEnumFeature("GainAuto").SetValue("Continuous");
                m_objIGXDevice.GetRemoteFeatureControl().GetEnumFeature("BalanceWhiteAuto").SetValue("Continuous");



            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


        }
        /// <summary>
        /// 关闭相机设备
        /// 
        /// </summary>
        public void CloseCameraFunc()
        {
            //如果没有停止采集，则停止采集
            try
            {
                if (m_bIsSnap)
                {
                    if (null != m_objIGXFeatureControl)
                    {
                        m_objIGXFeatureControl.GetCommandFeature("AcquisitionStop").Execute();
                        m_objIGXFeatureControl = null;
                    }
                }
                m_bIsSnap = false;
                //停止流通道、注销采集回调和关闭流
                if (null != m_objIGXStream)
                {
                    //停止流通道采集
                    m_objIGXStream.StopGrab();
                    //注销采集回调函数
                    m_objIGXStream.UnregisterCaptureCallback();
                    m_objIGXStream.Close();
                    m_objIGXStream = null;
                }
                //关闭设备
                if (null != m_objIGXDevice)
                {
                    m_objIGXDevice.Close();
                    m_objIGXDevice = null;
                }
                m_bIsOpen = false;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// 开始采集图像
        /// </summary>
        public void StartAcqFunc()
        {
            try
            {
                //开启采集流通道
                if (null != m_objIGXStream)
                {
                    //RegisterCaptureCallback第一个参数属于用户自定参数(类型必须为引用
                    //类型)，若用户想用这个参数可以在委托函数中进行使用
                    m_objIGXStream.RegisterCaptureCallback(this, __CaptureCallbackPro);
                    m_objIGXStream.StartGrab();
                    m_isStartGrab = true;
                }

                //发送开采命令
                if (null != m_objIGXFeatureControl)
                {
                    m_objIGXFeatureControl.GetCommandFeature("AcquisitionStart").Execute();
                }
                m_bIsSnap = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// 停止采集
        /// </summary>
        public void StopAcqFunc()
        {
            try
            {
                //发送停采命令
                if (null != m_objIGXFeatureControl)
                {
                    m_objIGXFeatureControl.GetCommandFeature("AcquisitionStop").Execute();
                }


                //关闭采集流通道
                if (null != m_objIGXStream)
                {
                    m_objIGXStream.StopGrab();
                    //注销采集回调函数
                    m_objIGXStream.UnregisterCaptureCallback();
                    m_isStartGrab = false;
                }
                m_bIsSnap = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 通过GX_PIXEL_FORMAT_ENTRY获取最优Bit位
        /// </summary>
        /// <param name="em">图像数据格式</param>
        /// <returns>最优Bit位</returns>
        private GX_VALID_BIT_LIST __GetBestValudBit(GX_PIXEL_FORMAT_ENTRY emPixelFormatEntry)
        {
            GX_VALID_BIT_LIST emValidBits = GX_VALID_BIT_LIST.GX_BIT_0_7;
            switch (emPixelFormatEntry)
            {
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_MONO8:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BAYER_GR8:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BAYER_RG8:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BAYER_GB8:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BAYER_BG8:
                    {
                        emValidBits = GX_VALID_BIT_LIST.GX_BIT_0_7;
                        break;
                    }
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_MONO10:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BAYER_GR10:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BAYER_RG10:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BAYER_GB10:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BAYER_BG10:
                    {
                        emValidBits = GX_VALID_BIT_LIST.GX_BIT_2_9;
                        break;
                    }
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_MONO12:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BAYER_GR12:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BAYER_RG12:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BAYER_GB12:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BAYER_BG12:
                    {
                        emValidBits = GX_VALID_BIT_LIST.GX_BIT_4_11;
                        break;
                    }
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_MONO14:
                    {
                        //暂时没有这样的数据格式待升级
                        break;
                    }
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_MONO16:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BAYER_GR16:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BAYER_RG16:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BAYER_GB16:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BAYER_BG16:
                    {
                        //暂时没有这样的数据格式待升级
                        break;
                    }
                default:
                    break;
            }
            return emValidBits;
        }

        /// <summary>
        /// 回调函数,用于获取图像信息和显示图像
        /// </summary>
        /// <param name="obj">用户自定义传入参数</param>
        /// <param name="objIFrameData">图像信息对象</param>
        private void __CaptureCallbackPro(object objUserParam, IFrameData objIFrameData)
        {
            try
            {
                if (m_bIsColor)
                {
                    GX_VALID_BIT_LIST emValidBits = GX_VALID_BIT_LIST.GX_BIT_0_7;
                    emValidBits = __GetBestValudBit(objIFrameData.GetPixelFormat());
                    IntPtr pBufferColor = objIFrameData.ConvertToRGB24(emValidBits, GX_BAYER_CONVERT_TYPE_LIST.GX_RAW2RGB_NEIGHBOUR, false);
                    CameraData = new Bitmap((int)objIFrameData.GetWidth(), (int)objIFrameData.GetHeight(), (int)objIFrameData.GetWidth() * 3, System.Drawing.Imaging.PixelFormat.Format24bppRgb, pBufferColor);
                    inPutFunc(CamIndex, CameraData);

                }
                else
                {

                    DhCamera objdhCamera = objUserParam as DhCamera;
                    CameraData = new Bitmap((int)objIFrameData.GetWidth(), (int)objIFrameData.GetHeight(), (int)objIFrameData.GetWidth(), System.Drawing.Imaging.PixelFormat.Format8bppIndexed, objIFrameData.GetBuffer());
                    inPutFunc(CamIndex, CameraData);
                }


            }
            catch (Exception)
            {
            }
            GC.Collect();
        }


    }
}
