/*

Copyright (C) 2005 by Jeremy Spiller.  All rights reserved.

Redistribution and use in source and binary forms, with or without 
modification, are permitted provided that the following conditions are met:

   1. Redistributions of source code must retain the above copyright 
      notice, this list of conditions and the following disclaimer.
   2. Redistributions in binary form must reproduce the above copyright 
      notice, this list of conditions and the following disclaimer in 
      the documentation and/or other materials provided with the distribution.
 
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
POSSIBILITY OF SUCH DAMAGE.

 */

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Diagnostics;
using System.Collections.Generic;


namespace Gosub
{


	/// <summary>
	/// Summary description for Direct3d.
	/// </summary>
	public class Direct3d : System.Windows.Forms.UserControl
	{
		private System.ComponentModel.IContainer components;

		// List of all instances of this control
		static List<Direct3d> mDirect3dControlList = new List<Direct3d>();
		static int mRenderTime;
		static int mFps;
		static int mFpsCount;

		static public int DxFPS { get { return mFps; } }

		Device mDx;
		D3DEnumeration mEnumerationSettings = new D3DEnumeration();
		D3DSettings mGraphicsSettings = new D3DSettings();
		Vector2 mScreenPixel;
		Vector2 mInverseScreenPixel;
		Vector2 mViewportSize;
		Point mDxMouseLastPosition;
		bool mMouseHovering;
		bool mLoaded;

		// Info to restore form to window mode
		FormBorderStyle mFormBorderStyle;
		Rectangle mFormRectangle = new Rectangle();
		MainMenu mFormMainMenu;
		bool mFormVisible;
		Form[] mFormOwnedForms;
		Rectangle mControlRectangle = new Rectangle();
		bool mControlVisible;

		// Current control state flags
		bool mAutoResize;
		bool mFullScreen;
		bool mDeviceLost;
		System.Windows.Forms.Timer timerRender;
		bool mShowCursor = true;


		/// <summary>
		/// Returns the scale of the view port
		/// </summary>
		public Vector2 DxViewportSize { get { return mViewportSize; } }

		/// <summary>
		/// Returns the scale of a screen pixel in view port units
		/// </summary>
		public Vector2 DxScreenPixel { get { return mScreenPixel; } }

		/// <summary>
		/// Convert a screen coordinate (-1 to 1) to viewport coordinate (ie. mouse Position).
		/// </summary>
		public Vector2 DxScreenToView(Vector2 screen)
		{
			return new Vector2((screen.X + 1) * mInverseScreenPixel.X,
								(-screen.Y + 1) * mInverseScreenPixel.Y);
		}

		/// <summary>
		/// Returns TRUE if the mouse is hovering over the view port
		/// </summary>
		public bool DxMouseHovering
		{
			get { return mMouseHovering; }
		}
		
		/// <summary>
		/// Returns a ray that represents the mouse in world coordinates.
		/// </summary>
		public void DxMouseRay(out Vector3 start, out Vector3 direction, float x, float y)
		{
			Matrix worldToScreen = Dx.Transform.View * Dx.Transform.Projection;
			Matrix screenToWorld = Matrix.Invert(worldToScreen);
						
			// Ray starts at the camera near clipping plane
			Vector3 mouseFront = Vector3.TransformCoordinate(
									new Vector3(x, y, 0), screenToWorld);
			
			// Ray ends at far clipping plane
			Vector3 mouseBack = Vector3.TransformCoordinate(
									new Vector3(x, y, 1), screenToWorld);
			
			start = mouseFront;
			direction = mouseBack - mouseFront;
		}
		
		/// <summary>
		/// Returns a ray that represents the last mouse Position in world coordinates.
		/// </summary>
		public void DxMouseRay(out Vector3 start, out Vector3 direction)
		{
			Vector2 screen = DxMouseLastScreenPosition;
			DxMouseRay(out start, out direction, screen.X, screen.Y);
		}


		/// <summary>
		/// Convert a viewport coordinate (ie. mouse Position) to a screen coordinate (-1 to 1)
		/// </summary>
		public Vector2 DxViewToScreen(Point view)
		{
			return new Vector2(view.X * mScreenPixel.X - 1,
							   -(view.Y * mScreenPixel.Y - 1));
		}

		/// <summary>
		/// Returns the last mouse Position in screen coordinates 
		/// (center of screen if mouse not DxMouseHovering)
		/// </summary>
		public Vector2 DxMouseLastScreenPosition
		{
			get { return DxViewToScreen(mDxMouseLastPosition); }
		}

		/// <summary>
		/// Returns the last mouse Position in view coordinates 
		/// (center of screen if mouse not DxMouseHovering)
		/// </summary>
		public Point DxMouseLastPosition
		{
			get { return mDxMouseLastPosition; }
		}


		// Overridable functions for the 3D scene created by the app
		protected virtual bool ConfirmDevice(Caps caps, VertexProcessingType vertexProcessingType,
			Format adapterFormat, Format backBufferFormat) { return true; }




		/// <summary>
		/// This doesn't work yet
		/// </summary>
		public bool DxShowCursor
		{
			get { return mShowCursor; }
			set
			{
				// Why doesn't this work?
				mShowCursor = value;
				if (mDx != null)
					mDx.ShowCursor(mShowCursor);
			}
		}


		/// <summary>
		/// When true, the control autoresizes to fill the whole form.
		/// NOTE: Setting this back to false leaves the control the scale
		/// of the whole form.
		/// </summary>
		public bool DxAutoResize
		{
			get { return mAutoResize; }
			set { mAutoResize = value; }
		}

		/// <summary>
		/// Get/Set full screen mode.  In full screen mode, the form that 
		/// this control is on is expanded to the full screen, and the Direct3d
		/// control is also expanded.  The menu is removed.  When returning to
		/// windowed mode, the form, control, and menu is restored.
		/// NOTE: You shouldn't have more than one control set to full screen
		/// at any given time.
		/// </summary>
		public bool DxFullScreen
		{
			get
			{
				return mFullScreen;
			}
			set
			{
				// If value changed, reset environment
				if (value != mFullScreen)
					InitializeEnvironment(value);
			}
		}


		/// <summary>
		/// DirectX events.
		/// </summary>
		public delegate void DxDirect3dDelegate(Direct3d d3d, Device dx);
		public delegate void DxDirect2dDelegate(Direct3d d3d, Device dx, Surface surface, Graphics graphics);

		/// <summary>
		/// Returns the DirectX device.  Do not store this, as it can
		/// change.  NOTE: The DirectX device is NULL until the
		/// control is loaded, and is NULL if there was an error.
		/// </summary>
		public Device Dx { get { return mDx; } }


		/// <summary>
		/// Occurs once after DirectX has been initialized for the first time.  
		/// Setup AutoMesh's, AutoVertexBuffer's, and AutoTexture's here.
		/// </summary>
		public event DxDirect3dDelegate DxLoaded;

		/// <summary>
		/// Occurs when a new DirectX device has been initialized.
		/// Setup lights and restore DX objects here.
		/// NOTE: We don't have control over when this may happen.
		/// </summary>
		public event DxDirect3dDelegate DxRestore;

		/// <summary>
		/// This event is called whenever DirectX decides to toss our surfaces.
		/// Delete DX objects here (but not AutoMesh, etc.)
		/// </summary>
		public event DxDirect3dDelegate DxLost;

		/// <summary>
		/// Occurs when the surface is resized.
		/// </summary>
		public event DxDirect3dDelegate DxResizing;

		/// <summary>
		/// Occurs before 3d rendering.  When this event is used, you
		/// must manually clear the screen (use dx.Clear).
		/// </summary>
		public event DxDirect3dDelegate DxRenderPre;

		/// <summary>
		/// Occurs when it is time to render 3d objects.  Place all 3d
		/// drawing code in this event.
		/// </summary>
		public event DxDirect3dDelegate DxRender3d;

		/// <summary>
		/// Occurs after Render3d, to draw 2d graphics over the 3d scene.
		/// There is a speed penalty when using this event.
		/// </summary>
		public event DxDirect2dDelegate DxRender2d;


		public Direct3d()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Create the enumerations form
			mEnumerationSettings.ConfirmDeviceCallback = new D3DEnumeration.ConfirmDeviceCallbackType(this.ConfirmDevice);
			mEnumerationSettings.Enumerate();
			if (Cursor == null)
			{
				// Set up a default cursor
				Cursor = System.Windows.Forms.Cursors.Default;
			}

			// Choose the initial settings for the application
			bool foundFullscreenMode = FindBestFullscreenMode(false, false);
			bool foundWindowedMode = FindBestWindowedMode(false, false);

			// Window or Full Screen not found
			if (!foundFullscreenMode && !foundWindowedMode)
				throw new DxNoCompatibleDevicesException();
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			DeleteDirectxDevice();
			base.Dispose(disposing);
		}




		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.timerRender = new System.Windows.Forms.Timer(this.components);
			this.SuspendLayout();
			// 
			// timerRender
			// 
			this.timerRender.Interval = 10;
			this.timerRender.Tick += new System.EventHandler(this.timerRender_Tick);
			// 
			// Direct3d
			// 
			this.Name = "Direct3d";
			this.Load += new System.EventHandler(this.Direct3d_Load);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// Sets up mGraphicsSettings with best available windowed mode, subject to 
		/// the doesRequireHardware and doesRequireReference constraints.  
		/// </summary>
		/// <param name="doesRequireHardware">Does the device require hardware support</param>
		/// <param name="doesRequireReference">Does the device require the ref device</param>
		/// <returns>true if a mode is found, false otherwise</returns>
		bool FindBestWindowedMode(bool doesRequireHardware, bool doesRequireReference)
		{
			// Get display mode of primary adapter (which is assumed to be where the window 
			// will appear)
			DisplayMode primaryDesktopDisplayMode = Manager.Adapters[0].CurrentDisplayMode;

			GraphicsAdapterInfo bestAdapterInfo = null;
			GraphicsDeviceInfo bestDeviceInfo = null;
			DeviceCombo bestDeviceCombo = null;

			foreach (GraphicsAdapterInfo adapterInfo in mEnumerationSettings.AdapterInfoList)
			{
				foreach (GraphicsDeviceInfo deviceInfo in adapterInfo.DeviceInfoList)
				{
					if (doesRequireHardware && deviceInfo.DevType != DeviceType.Hardware)
						continue;
					if (doesRequireReference && deviceInfo.DevType != DeviceType.Reference)
						continue;

					foreach (DeviceCombo deviceCombo in deviceInfo.DeviceComboList)
					{
						bool adapterMatchesBackBuffer = (deviceCombo.BackBufferFormat == deviceCombo.AdapterFormat);
						if (!deviceCombo.IsWindowed)
							continue;
						if (deviceCombo.AdapterFormat != primaryDesktopDisplayMode.Format)
							continue;

						// If we haven't found a compatible DeviceCombo yet, or if this set
						// is better (because it's a HAL, and/or because formats match better),
						// save it
						if (bestDeviceCombo == null ||
							bestDeviceCombo.DevType != DeviceType.Hardware && deviceInfo.DevType == DeviceType.Hardware ||
							deviceCombo.DevType == DeviceType.Hardware && adapterMatchesBackBuffer)
						{
							bestAdapterInfo = adapterInfo;
							bestDeviceInfo = deviceInfo;
							bestDeviceCombo = deviceCombo;
							if (deviceInfo.DevType == DeviceType.Hardware && adapterMatchesBackBuffer)
							{
								// This windowed device combo looks great -- take it
								goto EndWindowedDeviceComboSearch;
							}
							// Otherwise keep looking for a better windowed device combo
						}
					}
				}
			}

		EndWindowedDeviceComboSearch:
			if (bestDeviceCombo == null)
				return false;

			mGraphicsSettings.WindowedAdapterInfo = bestAdapterInfo;
			mGraphicsSettings.WindowedDeviceInfo = bestDeviceInfo;
			mGraphicsSettings.WindowedDeviceCombo = bestDeviceCombo;
			mGraphicsSettings.IsWindowed = true;
			mGraphicsSettings.WindowedDisplayMode = primaryDesktopDisplayMode;
			mGraphicsSettings.WindowedWidth = ClientRectangle.Right - ClientRectangle.Left;
			mGraphicsSettings.WindowedHeight = ClientRectangle.Bottom - ClientRectangle.Top;
			if (mEnumerationSettings.AppUsesDepthBuffer)
				mGraphicsSettings.WindowedDepthStencilBufferFormat = (DepthFormat)bestDeviceCombo.DepthStencilFormatList[0];
			mGraphicsSettings.WindowedMultisampleType = (MultiSampleType)bestDeviceCombo.MultiSampleTypeList[0];
			mGraphicsSettings.WindowedMultisampleQuality = 0;
			mGraphicsSettings.WindowedVertexProcessingType = (VertexProcessingType)bestDeviceCombo.VertexProcessingTypeList[0];
			mGraphicsSettings.WindowedPresentInterval = (PresentInterval)bestDeviceCombo.PresentIntervalList[0];

			return true;
		}




		/// <summary>
		/// Sets up mGraphicsSettings with best available fullscreen mode, subject to 
		/// the doesRequireHardware and doesRequireReference constraints.  
		/// </summary>
		/// <param name="doesRequireHardware">Does the device require hardware support</param>
		/// <param name="doesRequireReference">Does the device require the ref device</param>
		/// <returns>true if a mode is found, false otherwise</returns>
		bool FindBestFullscreenMode(bool doesRequireHardware, bool doesRequireReference)
		{
			// For fullscreen, default to first HAL DeviceCombo that supports the current desktop 
			// display mode, or any display mode if HAL is not compatible with the desktop mode, or 
			// non-HAL if no HAL is available
			DisplayMode adapterDesktopDisplayMode = new DisplayMode();
			DisplayMode bestAdapterDesktopDisplayMode = new DisplayMode();
			DisplayMode bestDisplayMode = new DisplayMode();
			bestAdapterDesktopDisplayMode.Width = 0;
			bestAdapterDesktopDisplayMode.Height = 0;
			bestAdapterDesktopDisplayMode.Format = 0;
			bestAdapterDesktopDisplayMode.RefreshRate = 0;

			GraphicsAdapterInfo bestAdapterInfo = null;
			GraphicsDeviceInfo bestDeviceInfo = null;
			DeviceCombo bestDeviceCombo = null;

			foreach (GraphicsAdapterInfo adapterInfo in mEnumerationSettings.AdapterInfoList)
			{
				adapterDesktopDisplayMode = Manager.Adapters[adapterInfo.AdapterOrdinal].CurrentDisplayMode;
				foreach (GraphicsDeviceInfo deviceInfo in adapterInfo.DeviceInfoList)
				{
					if (doesRequireHardware && deviceInfo.DevType != DeviceType.Hardware)
						continue;
					if (doesRequireReference && deviceInfo.DevType != DeviceType.Reference)
						continue;

					foreach (DeviceCombo deviceCombo in deviceInfo.DeviceComboList)
					{
						bool adapterMatchesBackBuffer = (deviceCombo.BackBufferFormat == deviceCombo.AdapterFormat);
						bool adapterMatchesDesktop = (deviceCombo.AdapterFormat == adapterDesktopDisplayMode.Format);
						if (deviceCombo.IsWindowed)
							continue;

						// If we haven't found a compatible set yet, or if this set
						// is better (because it's a HAL, and/or because formats match better),
						// save it
						if (bestDeviceCombo == null ||
							bestDeviceCombo.DevType != DeviceType.Hardware && deviceInfo.DevType == DeviceType.Hardware ||
							bestDeviceCombo.DevType == DeviceType.Hardware && bestDeviceCombo.AdapterFormat != adapterDesktopDisplayMode.Format && adapterMatchesDesktop ||
							bestDeviceCombo.DevType == DeviceType.Hardware && adapterMatchesDesktop && adapterMatchesBackBuffer)
						{
							bestAdapterDesktopDisplayMode = adapterDesktopDisplayMode;
							bestAdapterInfo = adapterInfo;
							bestDeviceInfo = deviceInfo;
							bestDeviceCombo = deviceCombo;
							if (deviceInfo.DevType == DeviceType.Hardware && adapterMatchesDesktop && adapterMatchesBackBuffer)
							{
								// This fullscreen device combo looks great -- take it
								goto EndFullscreenDeviceComboSearch;
							}
							// Otherwise keep looking for a better fullscreen device combo
						}
					}
				}
			}

		EndFullscreenDeviceComboSearch:
			if (bestDeviceCombo == null)
				return false;

			// Need to find a display mode on the best adapter that uses pBestDeviceCombo->AdapterFormat
			// and is as close to bestAdapterDesktopDisplayMode's res as possible
			bestDisplayMode.Width = 0;
			bestDisplayMode.Height = 0;
			bestDisplayMode.Format = 0;
			bestDisplayMode.RefreshRate = 0;
			foreach (DisplayMode displayMode in bestAdapterInfo.DisplayModeList)
			{
				if (displayMode.Format != bestDeviceCombo.AdapterFormat)
					continue;
				if (displayMode.Width == bestAdapterDesktopDisplayMode.Width &&
					displayMode.Height == bestAdapterDesktopDisplayMode.Height &&
					displayMode.RefreshRate == bestAdapterDesktopDisplayMode.RefreshRate)
				{
					// found a perfect match, so stop
					bestDisplayMode = displayMode;
					break;
				}
				else if (displayMode.Width == bestAdapterDesktopDisplayMode.Width &&
					displayMode.Height == bestAdapterDesktopDisplayMode.Height &&
					displayMode.RefreshRate > bestDisplayMode.RefreshRate)
				{
					// refresh rate doesn't match, but width/height match, so keep this
					// and keep looking
					bestDisplayMode = displayMode;
				}
				else if (bestDisplayMode.Width == bestAdapterDesktopDisplayMode.Width)
				{
					// width matches, so keep this and keep looking
					bestDisplayMode = displayMode;
				}
				else if (bestDisplayMode.Width == 0)
				{
					// we don't have anything better yet, so keep this and keep looking
					bestDisplayMode = displayMode;
				}
			}

			mGraphicsSettings.FullscreenAdapterInfo = bestAdapterInfo;
			mGraphicsSettings.FullscreenDeviceInfo = bestDeviceInfo;
			mGraphicsSettings.FullscreenDeviceCombo = bestDeviceCombo;
			mGraphicsSettings.IsWindowed = false;
			mGraphicsSettings.FullscreenDisplayMode = bestDisplayMode;
			if (mEnumerationSettings.AppUsesDepthBuffer)
				mGraphicsSettings.FullscreenDepthStencilBufferFormat = (DepthFormat)bestDeviceCombo.DepthStencilFormatList[0];
			mGraphicsSettings.FullscreenMultisampleType = (MultiSampleType)bestDeviceCombo.MultiSampleTypeList[0];
			mGraphicsSettings.FullscreenMultisampleQuality = 0;
			mGraphicsSettings.FullscreenVertexProcessingType = (VertexProcessingType)bestDeviceCombo.VertexProcessingTypeList[0];
			mGraphicsSettings.FullscreenPresentInterval = PresentInterval.Default;
			return true;
		}



		/// <summary>
		/// Initialize the graphics environment
		/// </summary>
		void InitializeEnvironment(bool fullScreen)
		{
			// Save normal form settings (or initial settings when starting up minimized/maximized)
			if (!mFullScreen)
			{
				// Save form setting only when window state is normal
				if (ParentForm.WindowState == FormWindowState.Normal
							|| mFormRectangle.Size == new Size())
				{
					mFormBorderStyle = ParentForm.FormBorderStyle;
					mFormRectangle.Location = ParentForm.Location;
					mFormRectangle.Size = ParentForm.Size;
					mFormMainMenu = ParentForm.Menu;
					mFormVisible = ParentForm.Visible;
				}
				// Always save control settings before entering full screen mode
				mControlRectangle.Location = this.Location;
				mControlRectangle.Size = this.Size;
				mControlVisible = this.Visible;
			}
			// Delete old Dx object
			DeleteDirectxDevice();

			// If switching from full screen mode to windowed mode, restore control and form scale
			if (mFullScreen && !fullScreen)
			{
				// We need to force a redraw so the form is the correct size,
				// and is not hidden by the task bar.  Display a black form
				// while doing this, so there is not as much annoying flicker.
				Form blackForm = new Form();
				blackForm.WindowState = FormWindowState.Maximized;
				blackForm.ControlBox = false;
				blackForm.MinimizeBox = false;
				blackForm.MinimizeBox = false;
				blackForm.ShowInTaskbar = false;
				blackForm.BackColor = Color.Black;
				blackForm.Show();
				ParentForm.Visible = false;

				// Restore form parameters
				ParentForm.FormBorderStyle = mFormBorderStyle;
				ParentForm.Location = mFormRectangle.Location;
				ParentForm.Size = mFormRectangle.Size;
				if (mFormMainMenu != null)
					ParentForm.Menu = mFormMainMenu;
				
				// Restore control parameters
				Location = mControlRectangle.Location;
				Size = mControlRectangle.Size;
				Visible = mControlVisible;

				// Restore owned forms
				if (mFormOwnedForms != null)
					for (int i = 0; i < mFormOwnedForms.Length; i++)
					{
						mFormOwnedForms[i].Owner = ParentForm;
						mFormOwnedForms[i].BringToFront();
					}
				mFormOwnedForms = null;
				
				// Display this form, and close the black form
				ParentForm.Visible = mFormVisible;
				ParentForm.BringToFront();
				blackForm.Close();				
			}

			GraphicsAdapterInfo adapterInfo = mGraphicsSettings.AdapterInfo;
			GraphicsDeviceInfo deviceInfo = mGraphicsSettings.DeviceInfo;

			// Set new full screen mode
			//bool oldFullScreenMode = mFullScreen;
			mFullScreen = fullScreen;
			mGraphicsSettings.IsWindowed = !fullScreen;

			// Set up presentation parameters from current settings
			PresentParameters presentParams = new PresentParameters();
			presentParams.Windowed = mGraphicsSettings.IsWindowed;
			presentParams.MultiSample = mGraphicsSettings.MultisampleType;
			presentParams.MultiSampleQuality = mGraphicsSettings.MultisampleQuality;
			presentParams.SwapEffect = SwapEffect.Discard;
			presentParams.EnableAutoDepthStencil = mEnumerationSettings.AppUsesDepthBuffer;
			presentParams.AutoDepthStencilFormat = mGraphicsSettings.DepthStencilBufferFormat;

			// If doing 2D graphics, allow a lockable back buffer.
			if (this.DxRender2d == null)
				presentParams.PresentFlag = PresentFlag.None;
			else
				presentParams.PresentFlag = PresentFlag.LockableBackBuffer;

			if (!mFullScreen)
			{
				// Windowed mode parameters
				presentParams.BackBufferCount = 1; // One back buffer OK
				presentParams.BackBufferWidth = ClientRectangle.Right - ClientRectangle.Left;
				presentParams.BackBufferHeight = ClientRectangle.Bottom - ClientRectangle.Top;
				presentParams.BackBufferFormat = mGraphicsSettings.DeviceCombo.BackBufferFormat;
				presentParams.FullScreenRefreshRateInHz = 0;
				presentParams.PresentationInterval = PresentInterval.Immediate;
				presentParams.DeviceWindow = this;
			}
			else
			{
				// Full screen mode parameters
				presentParams.BackBufferCount = 2; // Two back buffers needed for full screen
				presentParams.BackBufferWidth = mGraphicsSettings.DisplayMode.Width;
				presentParams.BackBufferHeight = mGraphicsSettings.DisplayMode.Height;
				presentParams.BackBufferFormat = mGraphicsSettings.DeviceCombo.BackBufferFormat;
				presentParams.FullScreenRefreshRateInHz = mGraphicsSettings.DisplayMode.RefreshRate;
				presentParams.PresentationInterval = mGraphicsSettings.PresentInterval;
				presentParams.DeviceWindow = this.Parent;
			}

			if (mFullScreen)
			{
				// Save owned forms, and get rid of them so they can't overlap the full screen
				mFormOwnedForms = ParentForm.OwnedForms;
				for (int i = 0; i < mFormOwnedForms.Length; i++)
					mFormOwnedForms[i].Owner = null;

				// Setup form to be full screen mode
				if (ParentForm.WindowState == FormWindowState.Minimized)
					ParentForm.WindowState = FormWindowState.Normal;
				ParentForm.FormBorderStyle = FormBorderStyle.None;
				ParentForm.Menu = null;
				ParentForm.Visible = true;
				Location = new Point(0, 0);
				Size = new Size(presentParams.BackBufferWidth, presentParams.BackBufferHeight);
				Visible = true;
				ParentForm.BringToFront();  // This form must be on top
				BringToFront(); // This control must be on top
			}



			if (deviceInfo.Caps.PrimitiveMiscCaps.IsNullReference)
			{
				// Warn user about null ref device that can't render anything
			}

			CreateFlags createFlags = new CreateFlags();
			if (mGraphicsSettings.VertexProcessingType == VertexProcessingType.Software)
				createFlags = CreateFlags.SoftwareVertexProcessing;
			else if (mGraphicsSettings.VertexProcessingType == VertexProcessingType.Mixed)
				createFlags = CreateFlags.MixedVertexProcessing;
			else if (mGraphicsSettings.VertexProcessingType == VertexProcessingType.Hardware)
				createFlags = CreateFlags.HardwareVertexProcessing;
			else if (mGraphicsSettings.VertexProcessingType == VertexProcessingType.PureHardware)
			{
				createFlags = CreateFlags.HardwareVertexProcessing | CreateFlags.PureDevice;
			}
			else
				throw new ApplicationException();

			// This application can be multithreaded
			createFlags |= CreateFlags.MultiThreaded;
			

			try
			{
				// Create the device
				mDx = new Device(mGraphicsSettings.AdapterOrdinal,
								mGraphicsSettings.DevType, this,
													createFlags, presentParams);
			}
			catch
			{
				// If that failed, fall back to the reference rasterizer
				if (deviceInfo.DevType == DeviceType.Hardware
					&& FindBestWindowedMode(false, true))
				{
					InitializeEnvironment(false);
					return;
				}
				throw;
			}


			// Set up the cursor (doesn't work)
			//mDx.SetCursor(this.Cursor, true);
			//mDx.ShowCursor(mShowCursor);

			// Setup the event handlers
			mDx.DeviceReset += new System.EventHandler(this.DxRestoreInternal);
			mDx.DeviceLost += new System.EventHandler(this.DxLostInternal);
			mDx.DeviceResizing += new System.ComponentModel.CancelEventHandler(this.DxResizeInternal);

			// Initialize device-dependent objects
			DxResizeInternal(null, null);
			if (!mLoaded && DxLoaded != null)
				DxLoaded(this, mDx);
			mLoaded = true;
			DxRestoreInternal(null, null);
		}


		/// <summary>
		/// Called when our environment was resized
		/// </summary>
		void DxResizeInternal(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// Set cursor (doesn't work)
			//mDx.SetCursor(this.Cursor, true);
			//mDx.ShowCursor(mShowCursor);

			if (mDx != null)
			{
				mScreenPixel = new Vector2(2f / mDx.Viewport.Width, 2f / mDx.Viewport.Height);
				mInverseScreenPixel = new Vector2(1 / mScreenPixel.X, 1 / mScreenPixel.Y);
				mViewportSize = new Vector2(mDx.Viewport.Width, mDx.Viewport.Height);

				if (DxResizing != null)
					DxResizing(this, mDx);
			}
		}

		/// <summary>
		/// Called when a device needs to be restored.
		/// </summary>
		void DxRestoreInternal(System.Object sender, System.EventArgs e)
		{
			if (DxRestore != null)
				DxRestore(this, mDx);

			// Only the first timer in mDirect3dControlList is enabled
			if (mDirect3dControlList.Count == 0)
				timerRender.Enabled = true;
			mDirect3dControlList.Add(this);
		}

		/// <summary>
		/// Called when DirectX tosses the surface
		/// </summary>
		void DxLostInternal(System.Object sender, System.EventArgs e)
		{
			// Only the first timer in mDirect3dControlList is enabled
			if (mDirect3dControlList.Count >= 2 && mDirect3dControlList[0] == this)
			{
				mDirect3dControlList[0].timerRender.Enabled = false;
				mDirect3dControlList.RemoveAt(0);
				mDirect3dControlList[0].timerRender.Enabled = true;
			}
			else
				mDirect3dControlList.Remove(this);

			if (DxLost != null)
				DxLost(this, mDx);
		}

		/// <summary>
		/// This function must be called each time through the main
		/// loop to render all instances of the Direct3d control
		/// </summary>
		public static bool DxRenderAllControls()
		{
			// Update time and FPS
			int renderTimeSec = Environment.TickCount/1000;
			if (renderTimeSec != mRenderTime)
			{
				mFps = mFpsCount;
				mFpsCount = 0;
			}
			mFpsCount++;
			mRenderTime = renderTimeSec;

			// Scan for and render any control that is full screen
			bool activeForm = false;
			for (int i = 0; i < mDirect3dControlList.Count; i++)
			{
				Direct3d control = mDirect3dControlList[i];
				if (control.DxFullScreen)
				{
					control.RenderControlInternal();
					activeForm = true;
				}
			}

			// If windowed mode, render all controls
			if (!activeForm) // not full screen
				for (int i = 0; i < mDirect3dControlList.Count; i++)
				{
					Direct3d control = mDirect3dControlList[i];
					Form form = control.ParentForm;
					if (form.Visible && control.Visible
						&& form.WindowState != FormWindowState.Minimized)
					{
						// RenderDelegate control only when visible
						control.RenderControlInternal();
						if (Form.ActiveForm == form)
							activeForm = true;
					}
				}
			return activeForm;
		}


		/// <summary>
		/// Draws the scene for this instance of the control
		/// </summary>
		void RenderControlInternal()
		{		
			if (mDeviceLost)
			{
				try
				{
					// Test the cooperative level to see if it's okay to render
					mDx.TestCooperativeLevel();
				}
				catch (DeviceLostException)
				{
					// If the device was lost, do not render until we get it back
					return;
				}
				catch (DeviceNotResetException)
				{
					// Check if the device needs to be resized.

					// If we are windowed, read the desktop mode and use the same vertexFormat for
					// the back buffer
					if (!mFullScreen)
					{
						GraphicsAdapterInfo adapterInfo = mGraphicsSettings.AdapterInfo;
						mGraphicsSettings.WindowedDisplayMode = Manager.Adapters[adapterInfo.AdapterOrdinal].CurrentDisplayMode;
					}

					// Reset the device and resize it
					mDx.Reset(mDx.PresentationParameters);
					DxResizeInternal(null, null);
				}
				mDeviceLost = false;
			}

			// Optionally autoresize this control to fit the form
			if (mAutoResize && !mFullScreen
				&& (Location != new Point(0, 0) || Size != ParentForm.ClientSize))
			{
				Location = new Point(0, 0);
				if (ParentForm.ClientSize.Width > 0 && ParentForm.ClientSize.Height > 0)
					Size = ParentForm.ClientSize;
			}
		    
			// Ensure this control has the focus in full screen mode
			if (mFullScreen && !Focused)
				Focus();

			// Draw scene
			if (DxRenderPre == null)
				mDx.Clear(ClearFlags.Target | ClearFlags.ZBuffer,
							BackColor.ToArgb(), 1.0f, 0);
			else
				DxRenderPre(this, mDx);

			// RenderDelegate the scene
			mDx.BeginScene();
			try
			{
				if (DxRender3d != null)
					DxRender3d(this, mDx);
			}
			finally
			{
				mDx.EndScene();
			}


			if (DxRender2d != null)
			{
				Surface surface = mDx.GetBackBuffer(0, 0, BackBufferType.Mono);
				Graphics graphics = surface.GetGraphics();
				try
				{
					DxRender2d(this, mDx, surface, graphics);
				}
				finally
				{
					graphics.Dispose();
					surface.Dispose();
				}
			}

			// Show the frame on the primary surface.
			try
			{
				mDx.Present();
			}
			catch (DeviceLostException)
			{
				mDeviceLost = true;
			}
		}



		/// <summary>
		/// Convert view port coordinates (ie. mouse) to screen coordinates (-1..1)
		/// </summary>
		public Vector3 DxViewToScreen(Vector3 point)
		{
			return new Vector3(point.X / mDx.Viewport.Width * 2 - 1,
				-(point.Y / mDx.Viewport.Height * 2 - 1), 0);
		}

		/// <summary>
		/// Convert unit coordinates (-1..1) to view port coordinates (ie. mouse)
		/// </summary>
		public Vector3 DxScreenToView(Vector3 point)
		{
			return new Vector3((point.X + 1) * mDx.Viewport.Width / 2,
				(-point.Y + 1) * mDx.Viewport.Height / 2, 0);
		}


		/// <summary>
		/// Set our variables to not active and not ready
		/// </summary>
		void DeleteDirectxDevice()
		{
			if (mDx != null)
			{
				DxLostInternal(null, null);
				mDx.Dispose();
				mDx = null;
			}
		}



		/// <summary>
		/// Prepares the simulation for a new device being selected
		/// </summary>
		void UserSelectNewDevice(object sender, EventArgs e)
		{
			DxSelectNewDevice();
		}


		/// <summary>
		/// Displays a dialog so the user can select a new adapter, device, or
		/// display mode, and then recreates the 3D environment if needed
		/// </summary>
		public void DxSelectNewDevice()
		{
			// Can't display dialogs in fullscreen mode
			this.DxFullScreen = false;

			// Make sure the main form is in the background
			this.SendToBack();

			// --- Display settings dialog ---
			D3DSettingsForm settingsForm = new D3DSettingsForm(mEnumerationSettings, mGraphicsSettings);
			System.Windows.Forms.DialogResult result = settingsForm.ShowDialog(null);
			if (result != System.Windows.Forms.DialogResult.OK)
				return; // User hit cancel

			mGraphicsSettings = settingsForm.settings;

			// Inform the display class of the change. It will internally
			// re-create valid surfaces, a d3ddevice, etc.
			InitializeEnvironment(!mGraphicsSettings.IsWindowed);
		}

		/// <summary>
		/// Mouse enters control
		/// </summary>
		protected override void OnMouseEnter(EventArgs e)
		{
			mMouseHovering = true;
			base.OnMouseEnter(e);
		}
		
		/// <summary>
		/// Mouse moves (update last mouse Position, and set DxD3d cursor)
		/// </summary>
		protected override void OnMouseMove(MouseEventArgs e)
		{
			mDxMouseLastPosition.X = e.X;
			mDxMouseLastPosition.Y = e.Y;
			if ((mDx != null) && (!mDx.Disposed))
			{
				// Move the D3D cursor
				mDx.SetCursorPosition(e.X, e.Y, false);
			}
			base.OnMouseMove(e);
		}

		/// <summary>
		/// Mouse leaves control
		/// </summary>
		protected override void OnMouseLeave(EventArgs e)
		{
			mMouseHovering = false;
			mDxMouseLastPosition = new Point(Size.Width/2, Size.Height/2);
			base.OnMouseLeave(e);
		}


		private void Direct3d_Load(object sender, System.EventArgs e)
		{
			// --------------------------------------------
			// Initialize the 3D environment for the app
			// -------------------------------------------
			InitializeEnvironment(mFullScreen);
			mDxMouseLastPosition = new Point(Size.Width/2, Size.Height/2);
		}

		/// <summary>
		/// This function is called once every 10ms to ensure the main loop 
		/// is rendering.  Normally, rendering is performed in the main loop,
		/// but stops when a modal dialog (or menu) is shown.
		/// NOTE: Only one timer (the first one in mDirect3dControlList)
		/// is enabled at any given time.
		/// </summary>
		private void timerRender_Tick(object sender, System.EventArgs e)
		{
			DxRenderAllControls();
		}
		
	}
	
	
	public class DxNoCompatibleDevicesException : Exception
	{
		public DxNoCompatibleDevicesException()
			: base()
		{
		}
		public DxNoCompatibleDevicesException(string m)
			: base(m)
		{
		}
	}
	

	/// <summary>
	/// Vertex type (Position only)
	/// </summary>
	struct VertexTypeP
	{
		public Vector3 Position;
		public const VertexFormats Format = VertexFormats.Position;
		
		public VertexTypeP(Vector3 position)
		{
			Position = position;
		}		
	}

	/// <summary>
	/// Vertex type (Position, colored)
	/// </summary>
	struct VertexTypePC
	{
		public Vector3 Position;
		public Color32 Color;
		public const VertexFormats Format = VertexFormats.Position | VertexFormats.Diffuse;		

		public VertexTypePC(Vector3 position, Color32 color)
		{
			Position = position;
			Color = color;
		}
		
	}


	/// <summary>
	/// Vertex type (Position, textured)
	/// </summary>
	struct VertexTypePT
	{
		public Vector3 Position;
		public float Tx, Ty;
		public const VertexFormats Format = VertexFormats.Position | VertexFormats.Texture1;
		
		public VertexTypePT(Vector3 position, float tx, float ty)
		{
			Position = position;
			Tx = tx;
			Ty = ty;
		}		
		public Vector2 Txy 
		{ 
			get { return new Vector2(Tx, Ty); } 
			set { Tx = value.X;  Ty = value.Y; }
		}		
	}
	/// <summary>
	/// Vertex type (Position textured, colored)
	/// </summary>
	struct VertexTypePCT
	{
		public Vector3 Position;
		public Color32 Color;
		public float Tx, Ty;
		public const VertexFormats Format = VertexFormats.Position | VertexFormats.Texture1 | VertexFormats.Diffuse;
		
		public VertexTypePCT(Vector3 position, Color32 color, float tx, float ty)
		{
			Position = position;
			Color = color;
			Tx = tx;
			Ty = ty;
		}
		public Vector2 Txy 
		{ 
			get { return new Vector2(Tx, Ty); } 
			set { Tx = value.X;  Ty = value.Y; }
		}		
	}


	/// <summary>
	/// Vertex type (Position, normal)
	/// </summary>
	struct VertexTypePN
	{
		public Vector3 Position;
		public Vector3 Normal;
		public const VertexFormats Format = VertexFormats.Position | VertexFormats.Normal;
		
		public VertexTypePN(Vector3 position, Vector3 normal)
		{
			Position = position;
			Normal = normal;
		}		
	}

	/// <summary>
	/// Vertex type (Position, normal, colored)
	/// </summary>
	struct VertexTypePNC
	{
		public Vector3 Position;
		public Vector3 Normal;
		public Color32 Color;
		public const VertexFormats Format = VertexFormats.Position | VertexFormats.Normal | VertexFormats.Diffuse;		

		public VertexTypePNC(Vector3 position, Vector3 normal, Color32 color)
		{
			Position = position;
			Normal = normal;
			Color = color;
		}
		
	}


	/// <summary>
	/// Vertex type (Position, nromal, textured)
	/// </summary>
	struct VertexTypePNT
	{
		public Vector3 Position;
		public Vector3 Normal;
		public float Tx, Ty;
		public const VertexFormats Format = VertexFormats.Position | VertexFormats.Normal | VertexFormats.Texture1;
		
		public VertexTypePNT(Vector3 position, Vector3 normal, float tx, float ty)
		{
			Position = position;
			Normal = normal;
			Tx = tx;
			Ty = ty;
		}		
		public Vector2 Txy 
		{ 
			get { return new Vector2(Tx, Ty); } 
			set { Tx = value.X;  Ty = value.Y; }
		}		
	}
	/// <summary>
	/// Vertex type (Position, normal, textured, colored - untested)
	/// </summary>
	struct VertexTypePNCT
	{
		public Vector3 Position;
		public Vector3 Normal;
		public Color32 Color;
		public float Tx, Ty;
		public const VertexFormats Format = VertexFormats.Position | VertexFormats.Normal | VertexFormats.Texture1 | VertexFormats.Diffuse;
		
		public VertexTypePNCT(Vector3 position, Vector3 normal, Color32 color, float tx, float ty)
		{
			Position = position;
			Normal = normal;
			Color = color;
			Tx = tx;
			Ty = ty;
		}
		public Vector2 Txy 
		{ 
			get { return new Vector2(Tx, Ty); } 
			set { Tx = value.X;  Ty = value.Y; }
		}		
	}


	//-----------------------------------------------------------------------
	// AutoVertexBuffer - a class to automatically 
	//-----------------------------------------------------------------------
	class AutoVertexBuffer
	{
		VertexBuffer mVertexBuffer;
		Direct3d mD3d;

		byte[] mVertexData;
		Type mVertexType;
		int mVertexNumVertices;
		Usage mVertexUsage;
		VertexFormats mVertexFormat;
		Pool mVertexPool;


		/// <summary>
		/// Return the vertex buffer (or null when the DirectX device is lost)
		/// </summary>
		public VertexBuffer VB { get { return mVertexBuffer; } }
		
		/// <summary>
		/// Return the Direct3d object for this object
		/// </summary>
		public Direct3d D3d { get { return mD3d; } }

		/// <summary>
		/// Number of vertices in the vertex buffer
		/// </summary>
		public int NumVertices { get { return mVertexNumVertices; } }

		/// <summary>
		/// Create an AutoVertexBuffer
		/// </summary>
		public AutoVertexBuffer(Direct3d d3d, Type vertexType, int numVerts,
									Usage usage, VertexFormats format, Pool pool)
		{
			mVertexBuffer = new VertexBuffer(vertexType, numVerts == 0 ? 1 : numVerts, d3d.Dx, usage, format, pool);
			mD3d = d3d;
			mVertexType = vertexType;
			mVertexNumVertices = numVerts;
			mVertexUsage = usage;
			mVertexFormat = format;
			mVertexPool = pool;

			d3d.DxLost += new Direct3d.DxDirect3dDelegate(d3d_DxLost);
			d3d.DxRestore += new Direct3d.DxDirect3dDelegate(d3d_DxRestore);
		}

		/// <summary>
		/// Dispose this object and the mesh it holds
		/// </summary>
		public void Dispose()
		{
			if (mVertexBuffer != null)
			{
				mVertexBuffer.Dispose();
				mVertexBuffer = null;
			}
			if (mD3d != null)
			{
				mD3d.DxLost -= new Direct3d.DxDirect3dDelegate(d3d_DxLost);
				mD3d.DxRestore -= new Direct3d.DxDirect3dDelegate(d3d_DxRestore);
				mD3d = null;
			}
			mVertexData = null;
		}
		
		/// <summary>
		/// Save the vertex buffer when the DirectX device is lost
		/// </summary>
		void d3d_DxLost(Direct3d d3d, Device dx)
		{
            if (mVertexBuffer == null)
                return;

			mVertexData = (byte[])mVertexBuffer.Lock(0, typeof(byte), LockFlags.ReadOnly,
												 mVertexBuffer.Description.Size);
			mVertexBuffer.Unlock();
			mVertexBuffer.Dispose();
			mVertexBuffer = null;
		}

		/// <summary>
		/// Restore the vertex buffer when the DirectX device is restored
		/// </summary>
		void d3d_DxRestore(Direct3d d3d, Device dx)
		{
			// If the direct3d device wasn't lost in the first place, don't restore it.
			// This happens the first timeMs around.
			if (mVertexBuffer != null)
				return;

			mVertexBuffer = new VertexBuffer(mVertexType, mVertexNumVertices,
									d3d.Dx, mVertexUsage, mVertexFormat, mVertexPool);
			mVertexBuffer.SetData(mVertexData, 0, LockFlags.None);
			mVertexData = null;
		}
	}	

	//-----------------------------------------------------------------------
	// AutoIndexBuffer - a class to automatically 
	//-----------------------------------------------------------------------
	class AutoIndexBuffer
	{
		IndexBuffer mIndexBuffer;
		Direct3d mD3d;
		byte[] mIndexData;
		int mNumIndices;
		Usage mUsage;
		Pool mPool;

		/// <summary>
		/// Return the pathIndex buffer (or null when the DirectX device is lost)
		/// </summary>
		public IndexBuffer I { get { return mIndexBuffer; } }
				
		/// <summary>
		/// Return the Direct3d object for this object
		/// </summary>
		public Direct3d D3d { get { return mD3d; } }

		/// <summary>
		/// Number of indices
		/// </summary>
		public int NumIndices { get { return mNumIndices; } }

		/// <summary>
		/// Create an AutoVertexBuffer (16 bits, ushort)
		/// </summary>
		public AutoIndexBuffer(Direct3d d3d, int numIndices, Usage usage, Pool pool)
		{
			mIndexBuffer = new IndexBuffer(typeof(ushort), numIndices, d3d.Dx, usage, pool);
			mD3d = d3d;
			mNumIndices = numIndices;
			mUsage = usage;
			mPool = pool;

			d3d.DxLost += new Direct3d.DxDirect3dDelegate(d3d_DxLost);
			d3d.DxRestore += new Direct3d.DxDirect3dDelegate(d3d_DxRestore);
		}
		
		/// <summary>
		/// Create an AutoVertexBuffer initialized with the indices in buffer
		/// </summary>
		/// <param name="d3d"></param>
		/// <param name="buffer"></param>
		/// <param name="usage"></param>
		/// <param name="pool"></param>
		public AutoIndexBuffer(Direct3d d3d, ushort []buffer, Usage usage, Pool pool)
		{
			mIndexBuffer = new IndexBuffer(typeof(ushort), buffer.Length, d3d.Dx, usage, pool);
			mD3d = d3d;
			mNumIndices = buffer.Length;
			mUsage = usage;
			mPool = pool;
			SetIndices(buffer);

			d3d.DxLost += new Direct3d.DxDirect3dDelegate(d3d_DxLost);
			d3d.DxRestore += new Direct3d.DxDirect3dDelegate(d3d_DxRestore);			
		}
		
		/// <summary>
		/// Set the pathIndex buffer
		/// </summary>
		/// <param name="indices"></param>
		public void SetIndices(ushort []indices)
		{
			ushort []buffer = (ushort[])mIndexBuffer.Lock(0, typeof(ushort), LockFlags.Discard, mNumIndices);
			indices.CopyTo(buffer, 0);
			mIndexBuffer.Unlock();
		}
		
		/// <summary>
		/// Gets the pathIndex buffer
		/// </summary>
		public ushort []GetIndices()
		{
			ushort []buffer = (ushort[])mIndexBuffer.Lock(0, typeof(ushort), LockFlags.Discard, mNumIndices);
			mIndexBuffer.Unlock();
			return buffer;
		}

		/// <summary>
		/// Dispose this object and the pathIndex buffer it holds
		/// </summary>
		public void Dispose()
		{
			if (mIndexBuffer != null)
			{
				mIndexBuffer.Dispose();
				mIndexBuffer = null;
			}
			if (mD3d != null)
			{
				mD3d.DxLost -= new Direct3d.DxDirect3dDelegate(d3d_DxLost);
				mD3d.DxRestore -= new Direct3d.DxDirect3dDelegate(d3d_DxRestore);
				mD3d = null;
			}
			mIndexBuffer = null;
		}
		
		/// <summary>
		/// Save the vertex buffer when the DirectX device is lost
		/// </summary>
		void d3d_DxLost(Direct3d d3d, Device dx)
		{
            if (mIndexBuffer == null)
                return;

			mIndexData = (byte[])mIndexBuffer.Lock(0, typeof(byte), LockFlags.ReadOnly, mIndexBuffer.Description.Size);
			mIndexBuffer.Unlock();
			mIndexBuffer.Dispose();
			mIndexBuffer = null;
		}

		/// <summary>
		/// Restore the vertex buffer when the DirectX device is restored
		/// </summary>
		void d3d_DxRestore(Direct3d d3d, Device dx)
		{
			// If the direct3d device wasn't lost in the first place, don't restore it.
			// This happens the first timeMs around.
			if (mIndexBuffer != null)
				return;

			mIndexBuffer = new IndexBuffer(typeof(ushort), mNumIndices, d3d.Dx, mUsage, mPool);
			byte []data = (byte[])mIndexBuffer.Lock(0, typeof(byte), LockFlags.Discard, mIndexBuffer.Description.Size);
			mIndexData.CopyTo(data, 0);
			mIndexBuffer.Unlock();
			mIndexData = null;
		}
	}
	
	class Direct3dFormatException : Exception
	{
		public Direct3dFormatException(string message) : base(message) { }
	}
	
	
	//-----------------------------------------------------------------------
	// AutoTexture
	//-----------------------------------------------------------------------
	class AutoTexture
	{
		Texture mTexture;
		Direct3d mD3d;
		int mPixelSizeBits;
		byte[] mTextureData;
		SurfaceDescription mSurfDesc;
		
		/// <summary>
		/// Unused by Direct3d class.  You can use this field
		/// to store miscellaneous info with the texture.
		/// </summary>
		public AutoTextureInfo Tag;
		public class AutoTextureInfo { }
		

		/// <summary>
		/// Return the mesh (or null if the device is lost)
		/// </summary>
		public Texture T { get { return mTexture; } }		
		
		/// <summary>
		/// Return the Direct3d object for this object
		/// </summary>
		public Direct3d D3d { get { return mD3d; } }
		
		/// <summary>
		/// Create an AutoTexture
		/// </summary>
		public AutoTexture(Direct3d d3d, Texture texture)
		{
			mTexture = texture;
			mD3d = d3d;
			mSurfDesc = mTexture.GetLevelDescription(0);
			d3d.DxLost += new Direct3d.DxDirect3dDelegate(d3d_DxLost);
			d3d.DxRestore += new Direct3d.DxDirect3dDelegate(d3d_DxRestore);
		}

		/// <summary>
		/// Dispose this object and the mesh it holds
		/// </summary>
		public void Dispose()
		{
			if (mTexture != null)
			{
				mTexture.Dispose();
				mTexture = null;
			}
			if (mD3d != null)
			{
				mD3d.DxLost -= new Direct3d.DxDirect3dDelegate(d3d_DxLost);
				mD3d.DxRestore -= new Direct3d.DxDirect3dDelegate(d3d_DxRestore);
				mD3d = null;
			}
			mTextureData = null;
		}
		
		/// <summary>
		/// The device must not be lost when this function is called
		/// </summary>
		public AutoTexture Clone(Direct3d d3d, Format format, Usage usage, Pool pool)
		{
			// Copy the texture
			Texture toTexture = new Texture(d3d.Dx, mSurfDesc.Width, mSurfDesc.Height, 
											1, usage, format, pool);
			
			Surface toSurface = toTexture.GetSurfaceLevel(0);
			Surface fromSurface = mTexture.GetSurfaceLevel(0);			
			SurfaceLoader.FromSurface(toSurface, fromSurface, Filter.Point, 0);
			toSurface.Dispose();
			fromSurface.Dispose();
	
			// Copy this AutoTexture
			AutoTexture autoTexture = new AutoTexture(d3d, toTexture);
			autoTexture.Tag = Tag;
			return autoTexture;
		}

		/// <summary>
		/// Save the mesh when the DirectX device is lost
		/// </summary>
		void d3d_DxLost(Direct3d d3d, Device dx)
		{
            if (mTexture == null)
                return;

			// Get surface description
			int width = mSurfDesc.Width;
			int height = mSurfDesc.Height;
			
			

			// Isn't there a better way to do this?
			switch (mSurfDesc.Format)
			{
				case Format.Dxt1:
					mPixelSizeBits = 4;
					break;
					
				case Format.A8:				
					mPixelSizeBits = 8;
					break;
								
				case Format.A4L4:
				case Format.Dxt2:
				case Format.Dxt3:
				case Format.Dxt4:
				case Format.Dxt5:
					mPixelSizeBits = 8;
					Debug.Assert(false);
					break;
				

				case Format.A8R3G3B2:
				case Format.A4R4G4B4:
				case Format.A1R5G5B5:
					mPixelSizeBits = 16;
					break;
					
				case Format.G8R8G8B8:
				case Format.A8R8G8B8:
				case Format.X8R8G8B8:
				case Format.A8B8G8R8:
				case Format.X8B8G8R8:
					mPixelSizeBits = 32;
					break;
				default:
					// Insert your vertexFormat above
					mPixelSizeBits = 32;
					Debug.Assert(false);
					break;
			}
			
			// Read the mesh data
			int pitch;
			mTextureData = (byte[])mTexture.LockRectangle(typeof(byte), 0, LockFlags.ReadOnly,
														out pitch, mPixelSizeBits*width*height/8);
			//Debug.Assert(pitch == mPixelSizeBits*width/8); // New versions of DX fixes Pitch for us
			mTexture.UnlockRectangle(0);
			mTexture.Dispose();
			mTexture = null;
		}

		/// <summary>
		/// Restore the mesh when the DirectX device is restored
		/// </summary>
		void d3d_DxRestore(Direct3d d3d, Device dx)
		{
			// If the direct3d device wasn't lost in the first place, don't restore it.
			// This happens the first timeMs around.
			if (mTexture != null)
				return;

			// Create mesh
			int width = mSurfDesc.Width;
			int height = mSurfDesc.Height;
			mTexture = new Texture(d3d.Dx, width, height,
									1, mSurfDesc.Usage, mSurfDesc.Format, mSurfDesc.Pool);

			// Write the texture data
			int pitch;
			GraphicsStream stream = mTexture.LockRectangle(0, LockFlags.Discard | LockFlags.NoDirtyUpdate, out pitch);
			//Debug.Assert(pitch == mPixelSizeBits*width/8); // New versions of DX fixes Pitch for us
			//Debug.Assert(pitch*height == mTextureData.Length);
			stream.Write(mTextureData);
			mTexture.UnlockRectangle(0);
			mTextureData = null;
		}
		
		/// <summary>
		/// Convert a gray bitmap to white wite with alpha color
		/// </summary>
		public void SetAlphaConstant(int alpha)
		{
			SurfaceDescription description = mTexture.GetLevelDescription(0);
			if (description.Format != Format.A8R8G8B8)
				throw new Direct3dFormatException("SetAlphaConstant: Invalid pixel format (A8R8G8B8 required)");
				
			// Generate an alphamap
			int width = description.Width;
			int height = description.Height;
			int pitch;
			Color32 []bm = (Color32[])mTexture.LockRectangle(
											typeof(Color32), 0, 0, out pitch, width*height);
			
			for (int i = 0;  i < bm.Length;  i++)
				bm[i] = new Color32(bm[i].iA, bm[i]);
			mTexture.UnlockRectangle(0);	
		}

		
		
		/// <summary>
		/// Convert a gray bitmap to white with alpha color
		/// </summary>
		public void SetAlphaFromGray()
		{
			SurfaceDescription description = mTexture.GetLevelDescription(0);
			if (description.Format != Format.A8R8G8B8)
				throw new Direct3dFormatException("SetAlphaFromGray: Invalid pixel format (A8R8G8B8 required)");

			// Generate an alphamap
			int width = description.Width;
			int height = description.Height;
			int pitch;
			Color32 []bm = (Color32[])mTexture.LockRectangle(
											typeof(Color32), 0, 0, out pitch, width*height);
			
			for (int i = 0;  i < bm.Length;  i++)
			{
				Color32 color = bm[i];
				int alpha = (color.iR + color.iG + color.iB)/3;
				bm[i] = new Color32(alpha, new Color32(255, 255, 255));
			}
			mTexture.UnlockRectangle(0);	
		}

		/// <summary>
		/// Reset texture color (leave apha channel alone)
		/// </summary>
		public void SetAlphaColor(Color32 color)
		{
			SurfaceDescription description = mTexture.GetLevelDescription(0);
			if (description.Format != Format.A8R8G8B8)
				throw new Direct3dFormatException("SetAlphaColor: Invalid pixel format (A8R8G8B8 required)");

			// Generate an alphamap
			int width = description.Width;
			int height = description.Height;
			int pitch;
			Color32 []bm = (Color32[])mTexture.LockRectangle(typeof(Color32), 
											0, LockFlags.Discard, out pitch, width*height);
			
			for (int i = 0;  i < bm.Length;  i++)
				bm[i] = new Color32(bm[i].iA, color);
			mTexture.UnlockRectangle(0);				
		}
		
		/// <summary>
		/// Generate an alpha map for the texture.  min/max color and alpha parameters are 0..255.
		/// Typically min/max alpha will be (0, 255), and min/max color will be a small range
		/// like (16, 32) to create a soft edge.
		/// </summary>
		public void SetAlphaFade(int minAlpha, int maxAlpha, int minColor, int maxColor)
		{
			SurfaceDescription description = mTexture.GetLevelDescription(0);
			if (description.Format != Format.A8R8G8B8)
				throw new Direct3dFormatException("SetAlphaColor: Invalid pixel format (A8R8G8B8 required)");

			// Generate an alphamap
			int width = description.Width;
			int height = description.Height;
			int pitch;
			Color32 []bm = (Color32[])mTexture.LockRectangle(typeof(Color32), 
															0, 0, out pitch, width*height);
			
			// Multiply by 3 because alpha calculation is R+G+B
			minColor *= 3;
			maxColor *= 3;
			
			for (int i = 0;  i < bm.Length;  i++)
			{
				Color32 color = bm[i];
				
				int colorSum = color.iR + color.iG + color.iB; // alpha = color*3
				
				// Calculate alpha
				int alpha;
				if (colorSum >= maxColor)
					alpha = maxAlpha;
				else if (colorSum <= minColor)
					alpha = minAlpha;
				else
				{
					// Convert colorSum from (minColor..maxColor) to (minApha..maxAlpha)
					alpha = (colorSum-minColor)*(maxAlpha-minAlpha)/(maxColor-minColor)  + minAlpha;
				}
				
				bm[i] = new Color32(alpha, color);
			}
			mTexture.UnlockRectangle(0);	
		}		
	}
	

	//-----------------------------------------------------------------------
	// AutoMesh - a class to manage meshes
	//-----------------------------------------------------------------------
	class AutoMesh
	{
		Mesh mMesh;
		Direct3d mD3d;
		
		/// <summary>
		/// Unused by Direct3d class.  You can use this field
		/// to store miscellaneous info with the mesh.
		/// </summary>
		public AutoMeshInfo Tag;
		public class AutoMeshInfo { }
		
		static Vector3 []sEmptyVector3Array = new Vector3[0];
		static Material []sEmptyMaterialArray = new Material[0];
		static AutoTexture []sEmptyAutoTextureArray = new AutoTexture[0];

		/// <summary>
		/// Return the mesh owned by this object (or null if the DirectX device is lost)
		/// </summary>
		public Mesh M { get { return mMesh; } }
		
		/// <summary>
		/// Return the Direct3d object for this object
		/// </summary>
		public Direct3d D3d { get { return mD3d; } }		

		// Save a copy of all Mesh data
		int mNumFaces;
		int mNumVertices;
		int mNumBytesPerVertex;
		MeshFlags mFlags;
		VertexFormats mVertexFormat;
		byte[] mIndexBufferCopy;
		byte[] mVertexBufferCopy;
		int[] mAttributeBufferCopy;
		
		// Textures and materials
		Material []mMaterials = sEmptyMaterialArray;
		AutoTexture []mTextures = sEmptyAutoTextureArray;

		// Cached info (vertices, bounding box, bounding sphere)
		Vector3[] mVertexCache;

		bool    mBoundingBoxValid;
		Vector3 mBoundingBoxMin;
		Vector3 mBoundingBoxMax;

		bool    mSphereValid;
		Vector3 mSphereCenter;
		float   mSphereRadius;
		
		bool	mSphereMinValid;
		Vector3 mSphereMinCenter;
		float	mSphereMinRadius;

		/// <summary>
		/// Create an automesh, which owns the given mesh
		/// </summary>
		public AutoMesh(Direct3d d3d, Mesh mesh)
		{
			mMesh = mesh;
			mD3d = d3d;
			d3d.DxLost += new Direct3d.DxDirect3dDelegate(d3d_DxLost);
			d3d.DxRestore += new Direct3d.DxDirect3dDelegate(d3d_DxRestore);
		}
		
		/// <summary>
		/// Gets/sets the array of materials (one for each subset to draw)
		/// </summary>
		public Material[] Materials
		{
			get { return mMaterials; }
			set
			{
				if (value == null)
					mMaterials = sEmptyMaterialArray;
				else
					mMaterials = value;
			}
		}
		
		/// <summary>
		/// Gets/sets the array of textures (one for each subset to draw).
		/// Returns an empty array if there are no textures, and any array
		/// element can be null if there is no texture for the given
		/// subset.
		/// </summary>
		public AutoTexture[] Textures
		{
			get { return mTextures; }
			set
			{
				if (value == null)
					mTextures = sEmptyAutoTextureArray;
				else
					mTextures = value;
			}
		}
		
		/// <summary>
		/// Draw all subsets of the mesh with the material and texture for
		/// the given subset.  If no texture/material is specified, the
		/// current one is used.
		/// </summary>
		public void Draw(bool useMeshMaterial)
		{
			mD3d.Dx.VertexFormat = mMesh.VertexFormat;
			if (mTextures.Length == 0)
			{
				mMesh.DrawSubset(0);
			}
			else
				for (int i = 0;  i < mTextures.Length;  i++)
				{
					if (mTextures[i] != null)
						mD3d.Dx.SetTexture(0, mTextures[i].T);
					if (useMeshMaterial)
						mD3d.Dx.Material = mMaterials[i];
					mMesh.DrawSubset(i);
				}
		}

		/// <summary>
		/// Dispose this object and the mesh it holds.  
		/// </summary>
		public void Dispose()
		{
			if (mMesh != null)
			{
				mMesh.Dispose();
				mMesh = null;
			}
			if (mD3d != null)
			{
				mD3d.DxLost -= new Direct3d.DxDirect3dDelegate(d3d_DxLost);
				mD3d.DxRestore -= new Direct3d.DxDirect3dDelegate(d3d_DxRestore);
				mD3d = null;
			}
			mIndexBufferCopy = null;
			mVertexBufferCopy = null;
			mAttributeBufferCopy = null;
			mVertexCache = null;
			mBoundingBoxValid = false;
			mSphereValid = false;
			mSphereMinValid = false;
			for (int i = 0;  i < mTextures.Length;  i++)
				if (mTextures[i] != null)
					mTextures[i].Dispose();
			mTextures = sEmptyAutoTextureArray;
			mMaterials = sEmptyMaterialArray;
		}

		/// <summary>
		/// Cone the mesh (and textures that it contains), 
		/// optionally converting the vertex and texture format.
		/// </summary>
		public AutoMesh Clone(Direct3d d3d, MeshFlags flags, VertexFormats vertexFormat,
						Format textureFormat, Usage usage, Pool pool)
		{
			// Clone the mesh vertex info
			Mesh mesh = mMesh.Clone(flags, vertexFormat, d3d.Dx);
			AutoMesh autoMesh = new AutoMesh(d3d, mesh);
			
			// Clone AutoMesh variables
			autoMesh.Tag = Tag;
			
			// Clone textures and materials
			if (mTextures.Length != 0)
			{
				// Clone materials
				autoMesh.mMaterials = new Material[mMaterials.Length];
				for (int i = 0;  i < mMaterials.Length;  i++)
					autoMesh.mMaterials[i] = mMaterials[i];
				
				// Clone textures
				autoMesh.mTextures = new AutoTexture[mTextures.Length];
				for (int i = 0;  i < mTextures.Length;  i++)
					if (mTextures[i] != null)
					{
						// Already cloned this texture?
						bool alreadyConvertedTexture = false;
						for (int j = 0;  j < i;  j++)
							if (mTextures[i] == mTextures[j])
							{
								alreadyConvertedTexture = true;
								autoMesh.mTextures[i] = autoMesh.mTextures[j];
								break;
							}
						// Clone new texture
						if (!alreadyConvertedTexture)
							autoMesh.mTextures[i] = mTextures[i].Clone(d3d, textureFormat, usage, pool);
					}
			}
			return autoMesh;			
		}

		/// <summary>
		/// Save the mesh when the DirectX device is lost
		/// </summary>
		void d3d_DxLost(Direct3d d3d, Device dx)
		{
            if (mMesh == null)
                return;

			// Save all data needed to restore the mesh
			mNumFaces = mMesh.NumberFaces;
			mNumVertices = mMesh.NumberVertices;
			mNumBytesPerVertex = mMesh.NumberBytesPerVertex;
			mFlags = mMesh.Options.Value;
			mVertexFormat = mMesh.VertexFormat;

			// Copy pathIndex buffer
			mIndexBufferCopy = (byte[])mMesh.LockIndexBuffer(typeof(byte),
										LockFlags.ReadOnly, mMesh.IndexBuffer.Description.Size);
			mMesh.UnlockIndexBuffer();

			// Copy vertex buffer
			mVertexBufferCopy = (byte[])mMesh.LockVertexBuffer(typeof(byte),
							LockFlags.ReadOnly, mMesh.NumberBytesPerVertex * mMesh.NumberVertices);
			mMesh.UnlockVertexBuffer();

			// Copy attribute buffer
			mAttributeBufferCopy = mMesh.LockAttributeBufferArray(LockFlags.ReadOnly);
			mMesh.UnlockAttributeBuffer(mAttributeBufferCopy);

			mMesh.Dispose();
			mMesh = null;
			mVertexCache = null;
		}

		/// <summary>
		/// Restore the mesh when the DirectX device is restored
		/// </summary>
		void d3d_DxRestore(Direct3d d3d, Device dx)
		{
			// If the direct3d device wasn't lost in the first place, don't restore it.
			// This happens the first timeMs around.
			if (mMesh != null)
				return;

			// Restore mesh			
			mMesh = new Mesh(mNumFaces, mNumVertices, mFlags, mVertexFormat, dx);
			Debug.Assert(mMesh.NumberBytesPerVertex == mNumBytesPerVertex);

			// Copy pathIndex buffer
			GraphicsStream stream = mMesh.LockIndexBuffer(LockFlags.Discard);
			stream.Write(mIndexBufferCopy);
			mMesh.UnlockIndexBuffer();
			
			// Copy vertex buffer
			stream = mMesh.LockVertexBuffer(LockFlags.Discard);
			stream.Write(mVertexBufferCopy);
			mMesh.UnlockVertexBuffer();

			// Copy attribute buffer
			int[] attributeBuffer = mMesh.LockAttributeBufferArray(LockFlags.Discard);
			mAttributeBufferCopy.CopyTo(attributeBuffer, 0);
			mMesh.UnlockAttributeBuffer(attributeBuffer);

			mIndexBufferCopy = null;
			mVertexBufferCopy = null;
			mAttributeBufferCopy = null;
		}
		
		
		/// <summary>
		/// Returns the bounding box of the mesh (only when the DirectX device is not lost).
		/// Caches the result for subsequent calls.
		/// </summary>
		public void BoundingBox(out Vector3 min, out Vector3 max)
		{
			if (!mBoundingBoxValid)
			{
				GraphicsStream stream = mMesh.LockVertexBuffer(LockFlags.ReadOnly);
				Geometry.ComputeBoundingBox(stream, mMesh.NumberVertices, mMesh.VertexFormat, 
											out mBoundingBoxMin, out mBoundingBoxMax);
				mMesh.UnlockVertexBuffer();
				mBoundingBoxValid = true;
			}
			min = mBoundingBoxMin;
			max = mBoundingBoxMax;
		}
		
		/// <summary>
		/// Returns the bounding sphere of the mesh (only when the DirectX device is not lost)
		/// Caches the result for subsequent calls.
		/// </summary>
		public float BoundingSphere(out Vector3 center)
		{
			if (!mSphereValid)
			{
				GraphicsStream stream = mMesh.LockVertexBuffer(LockFlags.ReadOnly);
				mSphereRadius = Geometry.ComputeBoundingSphere(stream, mMesh.NumberVertices, 
																mMesh.VertexFormat, out mSphereCenter);
				mMesh.UnlockVertexBuffer();
				mSphereValid = true;
			}
			center = mSphereCenter;
			return mSphereRadius;
		}


		/// <summary>
		/// Sometimes ComputeBoundingSphere doesn't return the smallest sphere,
		/// so this returns the smaller of ComputeBoundingSphere and ComputeBoundingBox
		/// </summary>
		public float BoundingSphereMin(out Vector3 center)
		{
			// Quick exit for previously calculated value
			if (mSphereMinValid)
			{
				center = mSphereMinCenter;
				return mSphereMinRadius;
			}
			
			// Get bounding sphere
			mSphereMinValid = true;
			mSphereMinRadius = BoundingSphere(out mSphereMinCenter);
			
			// Get bounding sphere around a bounding box
			float radius2;
			Vector3 min, max;
			BoundingBox(out min, out max);
			radius2 = ((max - min) * 0.5f).Length();

			// Bounding sphere smaller?			
			if (mSphereMinRadius <= radius2)
			{
				center = mSphereMinCenter;
				return mSphereMinRadius;
			}
			// Bounding box smaller
			mSphereMinCenter = center = (min + max) * 0.5f;
			mSphereMinRadius = radius2;
			return mSphereRadius;
		}

		/// <summary>
		/// This function returns all the vertices in the mesh.  The result is cached, and
		/// the same array is returned on repeated calls.  DO NOT MODIFY THE ARRAY.
		/// </summary>
		/// <returns></returns>
		public Vector3[] GetVertices()
		{
			// Return cached vertex list
			if (mVertexCache != null)
				return mVertexCache;
			if (mMesh == null)
				return sEmptyVector3Array;

			// Convert the mesh to vertex-only vertexFormat, and read the vertex array
			Mesh vertexMesh = mMesh.Clone(MeshFlags.SystemMemory, VertexFormats.Position, mD3d.Dx);
			mVertexCache = (Vector3[])vertexMesh.LockVertexBuffer(typeof(Vector3), LockFlags.ReadOnly, mMesh.NumberVertices);
			vertexMesh.Dispose();
			return mVertexCache;
		}
		
		/// <summary>
		/// Read a mesh from an X file, and load the textures which are
		/// assumed to be in the same directory.
		/// </summary>
		public static AutoMesh LoadFromXFile(string path, MeshFlags flags, Direct3d d3d)
		{
			ExtendedMaterial[] extendedMaterials;
			String []fileNames;
			AutoMesh mesh = new AutoMesh(d3d, Mesh.FromFile(path, 
										MeshFlags.SystemMemory, d3d.Dx, out extendedMaterials));

			mesh.mTextures = new AutoTexture[extendedMaterials.Length];
			mesh.mMaterials = new Material[extendedMaterials.Length];
			fileNames = new string[extendedMaterials.Length];
			
			// Load all the textures for this mesh
			for (int i = 0;  i < extendedMaterials.Length;  i++)
			{
				if (extendedMaterials[i].TextureFilename != null)
				{
					string textureFileName = System.IO.Path.Combine(
											System.IO.Path.GetDirectoryName(path), 
											extendedMaterials[i].TextureFilename);
					fileNames[i] = textureFileName;
					
					// Scan to see if we already have this texture
					bool alreadyHaveTexture = false;
					for (int j = 0;  j < i;  j++)
						if (textureFileName == fileNames[j])
						{
							mesh.mTextures[i] = mesh.mTextures[j];
							alreadyHaveTexture = true;
							break;
						}
					// Load texture (if we don't already have it)
					if (!alreadyHaveTexture)
						mesh.mTextures[i] = new AutoTexture(d3d, 
											TextureLoader.FromFile(d3d.Dx, textureFileName));
				}
				mesh.mMaterials[i] = extendedMaterials[i].Material3D;
				mesh.mMaterials[i].Ambient = mesh.mMaterials[i].Diffuse;
			}
			return mesh;
		}
		
	}


	
	/// <summary>
	/// Light weight wrapper for a color (32 bits)
	/// </summary>
	public struct Color32
	{
		int mArgb;
		
		/// <summary>
		/// Convert from an int to a color
		/// </summary>
		public Color32(int argb)
		{
			mArgb = argb;
		}
		/// <summary>
		/// Convert RGB (0..255) to a color
		/// </summary>
		public Color32(int red, int green, int blue)
		{
			mArgb = (red << 16) | (green << 8) | blue | (0xFF << 24);
		}
		/// <summary>
		/// Convert alpha and RGB (0..255) to a color
		/// </summary>
		public Color32(int alpha, int red, int green, int blue)
		{
			mArgb = (alpha << 24) | (red << 16) | (green << 8) | blue;
		}
		/// <summary>
		/// Copy a color
		/// </summary>
		public Color32(Color32 color)
		{
			mArgb = color.mArgb;
		}
		/// <summary>
		/// Calculate a new alpha (0..255) for the color
		/// </summary>
		public Color32(int alpha, Color32 colorBase)
		{
			mArgb = (colorBase.mArgb & 0xFFFFFF) | (alpha << 24);
		}
		/// <summary>
		/// Convert RGB (0..1) to a color
		/// </summary>
		public Color32(float red, float green, float blue)
		{
			red = red * 255.5f;
			red = Math.Min(255.5f, Math.Max(0, red));
			green = green * 255.5f;
			green = Math.Min(255.5f, Math.Max(0, green));
			blue = blue * 255.5f;
			blue = Math.Min(255.5f, Math.Max(0, blue));
			mArgb = new Color32((int)red, (int)green, (int)blue);
		}
		/// <summary>
		/// Convert alpha and RGB (0..1) to a color
		/// </summary>
		public Color32(float alpha, float red, float green, float blue)
		{
			red = red * 255.5f;
			red = Math.Min(255.5f, Math.Max(0, red));
			green = green * 255.5f;
			green = Math.Min(255.5f, Math.Max(0, green));
			blue = blue * 255.5f;
			blue = Math.Min(255.5f, Math.Max(0, blue));
			alpha = alpha * 2545.5f;
			alpha = Math.Min(255.5f, Math.Max(0, alpha));
			mArgb = new Color32((int)alpha, (int)red, (int)green, (int)blue);
		}
				
		/// <summary>
		/// Fade from one color to another (percent is 0..1, 0 = source color, 1 = to color)
		/// </summary>
		public Color32 Fade(Color32 to, float percent)
		{
			return Fade(to, (int)(percent*255.999f));
		}
		
		
		/// <summary>
		/// Fade from one color to another (alpha is 0..255, 0 = source color, 255 = to color)
		/// </summary>
		public Color32 Fade(Color32 to, int alpha)
		{
			int invAlpha = 255-alpha;
			Color32 c;
			c.mArgb =	((((mArgb & 0x00FF00FF)*invAlpha) >> 8) & 0x00FF00FF)
						+ ((((to.mArgb & 0x00FF00FF)*alpha) >> 8) & 0x00FF00FF)
						+ ((((mArgb >> 8) & 0x00FF00FF)*invAlpha) & unchecked((int)0xFF00FF00))
						+ ((((to.mArgb >> 8) & 0x00FF00FF)*alpha) & unchecked((int)0xFF00FF00));
			return c;
		}
		
		// Return RGB or A component of color (0..255)
		public int iA { get { return (mArgb >> 24) & 255; } }
		public int iR { get { return (mArgb >> 16) & 255; } }
		public int iG { get { return (mArgb >>  8) & 255; } }
		public int iB { get { return mArgb & 255; } }


		// Return RGB or A component of color (0..1)
		public float fA { get { return iA * (1f/255f); } }
		public float fR { get { return iR * (1f/255f); } }
		public float fG { get { return iG * (1f/255f); } }
		public float fB { get { return iB * (1f/255f); } }
		
		
		/// Implicitly convert an integer to a Color32
		public static implicit operator Color32(int color)
		{
			return new Color32(color);
		}
		
		/// Implicitly convert a Color32 to an int
		public static implicit operator int(Color32 color)
		{
			return color.mArgb;
		}

		/// Implicitly convert a Color to a Color32
		public static implicit operator Color32(Color color)
		{
			return new Color32(color.ToArgb());
		}
		
		/// Implicitly convert a Color32 to a Color
		public static implicit operator Color(Color32 color)
		{
			return Color.FromArgb(color.mArgb);
		}
		
		public static bool operator==(Color32 a, Color32 b)
		{
			return a.mArgb == b.mArgb;
		}
		public static bool operator!=(Color32 a, Color32 b)
		{
			return a.mArgb != b.mArgb;
		}

		public override int GetHashCode()
		{
			return mArgb;
		}

		public override bool Equals(object obj)
		{
			return mArgb == (Color32)obj;
		}
	}	
}
