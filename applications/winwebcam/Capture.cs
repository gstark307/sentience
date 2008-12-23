/*
    winwebcam: a Windows utility for grabbing images from webcams
    Copyright (C) 2008 Bob Mottram
    fuzzgun@gmail.com

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using DirectShowLib;

namespace sluggish.winwebcam
{
    /// <summary> handles capturing images using DirectShow </summary>
    public class Capture : ISampleGrabberCB, IDisposable
    {
        #region "Member variables"

        // whether we have successfully connected to a camera
        public bool Active;

        /// <summary> graph builder interface. </summary>
        private IFilterGraph2 m_FilterGraph = null;

        // Used to snap picture on Still pin
        private IAMVideoControl m_VidControl = null;
        private IPin m_pinStill = null;

        /// <summary> so we can wait for the async job to finish </summary>
        private ManualResetEvent m_PictureReady = null;

        private bool m_WantOne = false;

        /// <summary> Dimensions of the image, calculated once in constructor for perf. </summary>
        private int m_videoWidth;
        private int m_videoHeight;
        private int m_stride;

        /// <summary> buffer for bitmap data.  Always release by caller</summary>
        private IntPtr m_ipBuffer = IntPtr.Zero;

        // the last captured frame data
        public byte[] lastFrame_data;
        public Bitmap lastFrame;

        // the number of bytes per pixel to request from the camera
        public int bytes_per_pixel = 3;

        // image border
        public int border_tx, border_ty, border_bx, border_by;

        #endregion

        #region "APIs"
        [DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory")]
        private static extern void CopyMemory(IntPtr Destination, IntPtr Source, [MarshalAs(UnmanagedType.U4)] int Length);
        #endregion

        /// <summary>
        /// returns a list of device names
        /// </summary>
        /// <returns></returns>
        public static string[] GetDeviceNames()
        {
            string[] device_names = null;

            // Get the collection of video devices
            DsDevice[] capDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            if (capDevices != null)
            {
                if (capDevices.Length > 0)
                {
                    device_names = new string[capDevices.Length];
                    for (int i = 0; i < device_names.Length; i++)
                        device_names[i] = capDevices[i].Name;
                }
            }
            return (device_names);
        }

        /// <summary>
        /// Zero based device index and device params and output window
        /// </summary>
        /// <param name="iDeviceNum">filter device index</param>
        /// <param name="iWidth">width in pixels</param>
        /// <param name="iHeight">height in pixels</param>
        /// <param name="hControl">picturebox control</param>
        /// <param name="still_image_capture">whether to capture using still image mode (not supported by all cameras)</param>
        public Capture(int iDeviceNum,
                       int iWidth, int iHeight,
                       Control hControl,
                       bool still_image_capture)
        {
            DsDevice[] capDevices;

            // Get the collection of video devices
            capDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

            if (iDeviceNum + 1 > capDevices.Length)
            {
                throw new Exception("No video capture devices found at that index!");
            }

            try
            {
                DateTime start_time = DateTime.Now;

                // Set up the capture graph using still image capture
                Active = SetupGraph(capDevices[iDeviceNum], iWidth, iHeight, (short)(bytes_per_pixel * 8), hControl, still_image_capture);

                TimeSpan diff = DateTime.Now.Subtract(start_time);
                //Console.WriteLine("SetupGraph time: " + diff.TotalMilliseconds.ToString() + " mS");

                // tell the callback to ignore new images
                m_PictureReady = new ManualResetEvent(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //Dispose();
                //throw;
            }
        }

        /// <summary> release everything. </summary>
        public void Dispose()
        {
            CloseInterfaces();
            if (m_PictureReady != null)
            {
                m_PictureReady.Close();
            }
        }
        // Destructor
        ~Capture()
        {
            Dispose();
        }

        private byte[] grab_buffer;

        /// <summary>
        /// Get the image from the Still pin.  The returned image can turned into a bitmap with
        /// Bitmap b = new Bitmap(cam.Width, cam.Height, cam.Stride, PixelFormat.Format24bppRgb, m_ip);
        /// If the image is upside down, you can fix it with
        /// b.RotateFlip(RotateFlipType.RotateNoneFlipY);
        /// </summary>
        /// <returns>Returned pointer to be freed by caller with Marshal.FreeCoTaskMem</returns>
        public Bitmap Grab(ref IntPtr m_ip, bool update_bitmap)
        {
            Bitmap grabbed_frame = null;

            if (!paused)
            {
                int hr;

                // get ready to wait for new image
                m_PictureReady.Reset();
                m_ipBuffer = Marshal.AllocCoTaskMem(Math.Abs(m_stride) * m_videoHeight);

                try
                {
                    m_WantOne = true;

                    // If we are using a still pin, ask for a picture
                    if (m_VidControl != null)
                    {
                        // Tell the camera to send an image
                        hr = m_VidControl.SetMode(m_pinStill, VideoControlFlags.Trigger);
                        DsError.ThrowExceptionForHR(hr);
                    }

                    // Start waiting
                    if (!m_PictureReady.WaitOne(500, false))
                    {
                        //throw new Exception("Timeout waiting to get picture");
                        //Console.WriteLine("Timeout");
                    }
                }
                catch
                {
                    Marshal.FreeCoTaskMem(m_ipBuffer);
                    m_ipBuffer = IntPtr.Zero;
                    throw;
                }

                if (m_ipBuffer != IntPtr.Zero)
                {
                    if (lastFrame != null)
                        lastFrame.Dispose();

                    // store the last frame as a bitmap
                    if (update_bitmap)
                    {
                        if (bytes_per_pixel == 1)
                            grabbed_frame = new Bitmap(m_videoWidth, m_videoHeight, m_stride, PixelFormat.Format8bppIndexed, m_ipBuffer);
                        else
                            grabbed_frame = new Bitmap(m_videoWidth, m_videoHeight, m_stride, PixelFormat.Format24bppRgb, m_ipBuffer);

                        grabbed_frame.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        lastFrame = grabbed_frame;
                        m_ip = m_ipBuffer;
                    }
                }
            }
            //m_ip = m_ipBuffer;
            return (grabbed_frame);
        }

        public int Width
        {
            get
            {
                return m_videoWidth;
            }
        }
        public int Height
        {
            get
            {
                return m_videoHeight;
            }
        }
        public int Stride
        {
            get
            {
                return m_stride;
            }
        }

        private ISampleGrabber sampGrabber = null;
        private IBaseFilter capFilter = null;
        private ICaptureGraphBuilder2 m_bCapGraph;
        private IGraphBuilder m_bGraph;
        private IAMVideoProcAmp m_iVidConfig;
        private IAMCameraControl m_iCamConfig;

        void GetInterfaces()
        {
            Type comType = null;
            object comObj = null;

            try
            {
                //ICaptureGraphBuilder2 pBuilder = null;

                // Initiate Capture Graph Builder
                Guid clsid = typeof(CaptureGraphBuilder2).GUID;
                comType = Type.GetTypeFromCLSID(clsid);
                comObj = Activator.CreateInstance(comType);
                m_bCapGraph = (ICaptureGraphBuilder2)comObj;

                // Initiate Graph Builder
                Guid clsfg = typeof(FilterGraph).GUID;
                comType = Type.GetTypeFromCLSID(clsfg); //Clsid.FilterGraph);
                comObj = Activator.CreateInstance(comType);
                m_bGraph = (IGraphBuilder)comObj;

                // Initiate Video Configuration Interface
                DsGuid cat = PinCategory.Capture;
                DsGuid type = MediaType.Interleaved;
                Guid iid = typeof(IAMVideoProcAmp).GUID;
                m_bCapGraph.FindInterface(cat, type, capFilter, iid, out comObj);
                m_iVidConfig = (IAMVideoProcAmp)comObj;

                // test
                //m_iVidConfig.Set(VideoProcAmpProperty.WhiteBalance, 0, VideoProcAmpFlags.Manual);


                // Initiate Camera Configuration Interface
                cat = PinCategory.Capture;
                type = MediaType.Interleaved;
                iid = typeof(IAMCameraControl).GUID;
                m_bCapGraph.FindInterface(cat, type, capFilter, iid, out comObj);
                m_iCamConfig = (IAMCameraControl)comObj;

            }
            catch (Exception ee)
            {
                if (comObj != null)
                    Marshal.ReleaseComObject(comObj);

                throw new Exception("Could not get interfaces\r\n" + ee.Message);
            }
        }

        /// <summary>
        /// sets the camera exposure
        /// </summary>
        /// <param name="value"></param>
        public void SetExposure(int value)
        {
            if (m_iVidConfig != null)
            {
                m_iVidConfig.Set(VideoProcAmpProperty.ColorEnable, 1, VideoProcAmpFlags.Manual);
                m_iVidConfig.Set(VideoProcAmpProperty.Saturation, value, VideoProcAmpFlags.Manual);
                m_iVidConfig.Set(VideoProcAmpProperty.Brightness, value, VideoProcAmpFlags.Manual);
            }
            if (m_iCamConfig != null)
            {
                m_iCamConfig.Set(CameraControlProperty.Exposure, value, CameraControlFlags.Manual);
            }
        }

        public void SetExposureAuto()
        {
            if (m_iVidConfig != null)
            {
                m_iVidConfig.Set(VideoProcAmpProperty.ColorEnable, 1, VideoProcAmpFlags.Auto);
                m_iVidConfig.Set(VideoProcAmpProperty.Brightness, 0, VideoProcAmpFlags.Auto);
                m_iVidConfig.Set(VideoProcAmpProperty.Saturation, 0, VideoProcAmpFlags.Auto);
                m_iVidConfig.Set(VideoProcAmpProperty.Gain, 0, VideoProcAmpFlags.Auto);
                m_iVidConfig.Set(VideoProcAmpProperty.WhiteBalance, 0, VideoProcAmpFlags.Auto);
                m_iVidConfig.Set(VideoProcAmpProperty.Hue, 0, VideoProcAmpFlags.Auto);
                m_iVidConfig.Set(VideoProcAmpProperty.Gamma, 0, VideoProcAmpFlags.Auto);
            }
            if (m_iCamConfig != null)
            {
                m_iCamConfig.Set(CameraControlProperty.Exposure, 0, CameraControlFlags.Auto);
            }
        }


        /// <summary>
        /// build the capture graph for grabber
        /// </summary>
        /// <param name="dev">directshow device filter index</param>
        /// <param name="iWidth">width of the image</param>
        /// <param name="iHeight">height of the image</param>
        /// <param name="iBPP">number of bits per pixel</param>
        /// <param name="hControl">picturebox control within which to show the preview</param>
        /// <param name="use_still_pin">whether to use still image capture (not supported by all cameras)</param>
        /// <returns>true if no errors were encountered</returns>
        private bool SetupGraph(DsDevice dev,
                                int iWidth, int iHeight,
                                short iBPP,
                                Control hControl,
                                bool use_still_pin)
        {
            bool graph_connected = false;
            int hr;

            IPin pCaptureOut = null;
            IPin pSampleIn = null;
            IPin pRenderIn = null;

            // Get the graphbuilder object
            m_FilterGraph = new FilterGraph() as IFilterGraph2;

            try
            {
#if DEBUG
                //m_rot = new DsROTEntry(m_FilterGraph);
#endif
                // add the video input device
                hr = m_FilterGraph.AddSourceFilterForMoniker(dev.Mon, null, dev.Name, out capFilter);
                DsError.ThrowExceptionForHR(hr);

                // Find the still pin
                if (use_still_pin)
                    m_pinStill = DsFindPin.ByCategory(capFilter, PinCategory.Still, 0);
                else
                    m_pinStill = null;

                // Didn't find one.  Is there a preview pin?
                if (m_pinStill == null)
                {
                    m_pinStill = DsFindPin.ByCategory(capFilter, PinCategory.Preview, 0);
                }

                // Still haven't found one.  Need to put a splitter in so we have
                // one stream to capture the bitmap from, and one to display.  Ok, we
                // don't *have* to do it that way, but we are going to anyway.
                if (m_pinStill == null)
                {
                    IPin pRaw = null;
                    IPin pSmart = null;

                    // There is no still pin
                    m_VidControl = null;

                    // Add a splitter
                    IBaseFilter iSmartTee = (IBaseFilter)new SmartTee();

                    try
                    {
                        hr = m_FilterGraph.AddFilter(iSmartTee, "SmartTee");
                        DsError.ThrowExceptionForHR(hr);

                        // Find the find the capture pin from the video device and the
                        // input pin for the splitter, and connnect them
                        pRaw = DsFindPin.ByCategory(capFilter, PinCategory.Capture, 0);
                        pSmart = DsFindPin.ByDirection(iSmartTee, PinDirection.Input, 0);

                        hr = m_FilterGraph.Connect(pRaw, pSmart);
                        DsError.ThrowExceptionForHR(hr);

                        // Now set the capture and still pins (from the splitter)
                        m_pinStill = DsFindPin.ByName(iSmartTee, "Preview");
                        pCaptureOut = DsFindPin.ByName(iSmartTee, "Capture");

                        // If any of the default config items are set, perform the config
                        // on the actual video device (rather than the splitter)
                        if (iHeight + iWidth + iBPP > 0)
                        {
                            SetConfigParms(pRaw, iWidth, iHeight, iBPP);
                        }
                    }
                    finally
                    {
                        if (pRaw != null)
                        {
                            Marshal.ReleaseComObject(pRaw);
                        }
                        if (pRaw != pSmart)
                        {
                            Marshal.ReleaseComObject(pSmart);
                        }
                        if (pRaw != iSmartTee)
                        {
                            Marshal.ReleaseComObject(iSmartTee);
                        }
                    }
                }
                else
                {
                    // Get a control pointer (used in Click())
                    m_VidControl = capFilter as IAMVideoControl;

                    pCaptureOut = DsFindPin.ByCategory(capFilter, PinCategory.Capture, 0);

                    // If any of the default config items are set
                    if (iHeight + iWidth + iBPP > 0)
                    {
                        SetConfigParms(m_pinStill, iWidth, iHeight, iBPP);
                    }
                }

                // Get the SampleGrabber interface
                sampGrabber = new SampleGrabber() as ISampleGrabber;

                // Configure the sample grabber
                IBaseFilter baseGrabFlt = sampGrabber as IBaseFilter;
                ConfigureSampleGrabber(sampGrabber);
                pSampleIn = DsFindPin.ByDirection(baseGrabFlt, PinDirection.Input, 0);

                // Get the default video renderer
                IBaseFilter pRenderer = new VideoRendererDefault() as IBaseFilter;
                hr = m_FilterGraph.AddFilter(pRenderer, "Renderer");
                DsError.ThrowExceptionForHR(hr);

                pRenderIn = DsFindPin.ByDirection(pRenderer, PinDirection.Input, 0);

                // Add the sample grabber to the graph
                hr = m_FilterGraph.AddFilter(baseGrabFlt, "Ds.NET Grabber");
                DsError.ThrowExceptionForHR(hr);

                DateTime start_time = DateTime.Now;

                if (m_VidControl == null)
                {
                    // Connect the Still pin to the sample grabber
                    hr = m_FilterGraph.Connect(m_pinStill, pSampleIn);
                    DsError.ThrowExceptionForHR(hr);

                    // Connect the capture pin to the renderer
                    hr = m_FilterGraph.Connect(pCaptureOut, pRenderIn);
                    DsError.ThrowExceptionForHR(hr);

                    graph_connected = true;
                }
                else
                {
                    try
                    {
                        // Connect the capture pin to the renderer
                        hr = m_FilterGraph.Connect(pCaptureOut, pRenderIn);
                        if (hr >= 0)
                        {
                            // Connect the Still pin to the sample grabber
                            hr = m_FilterGraph.Connect(m_pinStill, pSampleIn);
                            if (hr >= 0) graph_connected = true;
                        }
                    }
                    catch
                    {
                    }
                }

                TimeSpan diff = DateTime.Now.Subtract(start_time);
                //Console.WriteLine("FilterGraph connect time: " + diff.TotalMilliseconds.ToString() + " mS");
                start_time = DateTime.Now;

                if (graph_connected)
                {
                    // Learn the video properties
                    SaveSizeInfo(sampGrabber);
                    ConfigVideoWindow(hControl);

                    // Start the graph
                    IMediaControl mediaCtrl = m_FilterGraph as IMediaControl;
                    hr = mediaCtrl.Run();
                    DsError.ThrowExceptionForHR(hr);

                    GetInterfaces();
                }
                
                diff = DateTime.Now.Subtract(start_time);
                //Console.WriteLine("ConfigVideoWindow time: " + diff.TotalMilliseconds.ToString() + " mS");
            }
            finally
            {
                if (sampGrabber != null)
                {
                    Marshal.ReleaseComObject(sampGrabber);
                    sampGrabber = null;
                }
                if (pCaptureOut != null)
                {
                    Marshal.ReleaseComObject(pCaptureOut);
                    pCaptureOut = null;
                }
                if (pRenderIn != null)
                {
                    Marshal.ReleaseComObject(pRenderIn);
                    pRenderIn = null;
                }
                if (pSampleIn != null)
                {
                    Marshal.ReleaseComObject(pSampleIn);
                    pSampleIn = null;
                }
            }
            return (graph_connected);
        }

        #region "pause and resume streaming video"

        private bool paused = false;

        /// <summary>
        /// pause video streaming
        /// </summary>
        /// <returns></returns>
        public bool Pause()
        {
            if (!paused)
            {
                int hr;

                try
                {
                    if (m_FilterGraph != null)
                    {
                        IMediaControl mediaCtrl = m_FilterGraph as IMediaControl;
                        hr = mediaCtrl.Pause();
                        paused = true;
                    }
                }
                catch
                {
                }
            }
            return (paused);
        }

        public void Resume()
        {
            if (paused)
            {
                int hr;

                try
                {
                    if (m_FilterGraph != null)
                    {
                        IMediaControl mediaCtrl = m_FilterGraph as IMediaControl;
                        hr = mediaCtrl.Run();
                        paused = false;
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Stop video streaming
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            if (!paused)
            {
                int hr;

                try
                {
                    if (m_FilterGraph != null)
                    {
                        IMediaControl mediaCtrl = m_FilterGraph as IMediaControl;
                        hr = mediaCtrl.StopWhenReady();
                        paused = true;
                    }
                }
                catch
                {
                }
            }
            return (paused);
        }

        #endregion

        private void SaveSizeInfo(ISampleGrabber sampGrabber)
        {
            int hr;

            // Get the media type from the SampleGrabber
            AMMediaType media = new AMMediaType();

            hr = sampGrabber.GetConnectedMediaType(media);
            DsError.ThrowExceptionForHR(hr);

            if ((media.formatType != FormatType.VideoInfo) || (media.formatPtr == IntPtr.Zero))
            {
                throw new NotSupportedException("Unknown Grabber Media Format");
            }

            // Grab the size info
            VideoInfoHeader videoInfoHeader = (VideoInfoHeader)Marshal.PtrToStructure(media.formatPtr, typeof(VideoInfoHeader));
            m_videoWidth = videoInfoHeader.BmiHeader.Width;
            m_videoHeight = videoInfoHeader.BmiHeader.Height;
            m_stride = m_videoWidth * (videoInfoHeader.BmiHeader.BitCount / 8);

            DsUtils.FreeAMMediaType(media);
            media = null;
        }

        // Set the video window within the control specified by hControl
        private void ConfigVideoWindow(Control hControl)
        {
            int hr;

            IVideoWindow ivw = m_FilterGraph as IVideoWindow;

            // Set the parent
            hr = ivw.put_Owner(hControl.Handle);
            DsError.ThrowExceptionForHR(hr);

            // Turn off captions, etc
            hr = ivw.put_WindowStyle(WindowStyle.Child | WindowStyle.ClipChildren | WindowStyle.ClipSiblings);
            DsError.ThrowExceptionForHR(hr);

            // Yes, make it visible
            hr = ivw.put_Visible(OABool.True);
            DsError.ThrowExceptionForHR(hr);

            // Move to upper left corner
            Rectangle rc = hControl.ClientRectangle;
            hr = ivw.SetWindowPosition(0, 0, rc.Right, rc.Bottom);
            DsError.ThrowExceptionForHR(hr);
        }

        private void ConfigureSampleGrabber(ISampleGrabber sampGrabber)
        {
            int hr;
            AMMediaType media = new AMMediaType();

            // Set the media type
            media.majorType = MediaType.Video;
            if (bytes_per_pixel == 1)
                media.subType = MediaSubType.RGB8;
            else
                media.subType = MediaSubType.RGB24;
            media.formatType = FormatType.VideoInfo;
            hr = sampGrabber.SetMediaType(media);
            DsError.ThrowExceptionForHR(hr);

            DsUtils.FreeAMMediaType(media);
            media = null;

            // Configure the samplegrabber
            hr = sampGrabber.SetCallback(this, 1);
            DsError.ThrowExceptionForHR(hr);
        }

        // Set the Framerate, and video size
        private bool SetConfigParms(IPin pStill, int iWidth, int iHeight, short iBPP)
        {
            bool success = true;
            int hr;
            AMMediaType media;
            VideoInfoHeader v;

            IAMStreamConfig videoStreamConfig = pStill as IAMStreamConfig;

            // Get the existing format block
            hr = videoStreamConfig.GetFormat(out media);
            DsError.ThrowExceptionForHR(hr);

            try
            {
                // copy out the videoinfoheader
                v = new VideoInfoHeader();
                Marshal.PtrToStructure(media.formatPtr, v);

                // if overriding the width, set the width
                if (iWidth > 0)
                {
                    v.BmiHeader.Width = iWidth;
                }

                // if overriding the Height, set the Height
                if (iHeight > 0)
                {
                    v.BmiHeader.Height = iHeight;
                }

                // if overriding the bits per pixel
                if (iBPP > 0)
                {
                    v.BmiHeader.BitCount = iBPP;
                }

                // Copy the media structure back
                Marshal.StructureToPtr(v, media.formatPtr, false);

                // Set the new format
                try
                {
                    hr = videoStreamConfig.SetFormat(media);
                    //DsError.ThrowExceptionForHR( hr );
                }
                catch
                {
                    success = false;
                }
            }
            finally
            {
                DsUtils.FreeAMMediaType(media);
                media = null;
            }
            return (success);
        }

        /// <summary> Shut down capture </summary>
        private void CloseInterfaces()
        {
            int hr;

            try
            {
                if (m_FilterGraph != null)
                {
                    IMediaControl mediaCtrl = m_FilterGraph as IMediaControl;

                    // Stop the graph
                    hr = mediaCtrl.Stop();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            if (m_FilterGraph != null)
            {
                Marshal.ReleaseComObject(m_FilterGraph);
                m_FilterGraph = null;
            }

            if (m_VidControl != null)
            {
                Marshal.ReleaseComObject(m_VidControl);
                m_VidControl = null;
            }

            if (m_pinStill != null)
            {
                Marshal.ReleaseComObject(m_pinStill);
                m_pinStill = null;
            }
        }

        /// <summary> sample callback, NOT USED. </summary>
        int ISampleGrabberCB.SampleCB(double SampleTime, IMediaSample pSample)
        {
            Marshal.ReleaseComObject(pSample);
            return 0;
        }

        /// <summary> buffer callback, COULD BE FROM FOREIGN THREAD. </summary>
        int ISampleGrabberCB.BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen)
        {
            // Note that we depend on only being called once per call to Click.  Otherwise
            // a second call can overwrite the previous image.
            Debug.Assert(BufferLen == Math.Abs(m_stride) * m_videoHeight, "Incorrect buffer length");

            if (m_WantOne)
            {
                m_WantOne = false;
                Debug.Assert(m_ipBuffer != IntPtr.Zero, "Unitialized buffer");

                // Save the buffer
                CopyMemory(m_ipBuffer, pBuffer, BufferLen);

                // Picture is ready.
                m_PictureReady.Set();
            }

            return 0;
        }
    }
}
