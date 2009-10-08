/*
    Sentience 3D Perception System
    Copyright (C) 2009 Bob Mottram
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
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace dpslam.core
{
    /// <summary>
    /// object representing the Surveyor robot with stereo camera
    /// </summary>
    public class robotSurveyor : robot
    {
		// display
		public int map_image_width;
		public byte[] map;
		public Bitmap map_bitmap;
		
        public int reference_variance0 = 7;
        public int reference_speed0 = 95;
        public int reference_pwm0 = 19;
        public int reference_variance1 = 22;
        public int reference_speed1 = 265;
        public int reference_pwm1 = 31;
		
		public int pwm_base_speed = 200;
		public int pwm_lspeed = 200;
		public int pwm_rspeed = 200;
		public float current_speed_mmsec = 0;
		public float current_speed_uncertainty_mmsec = 0;
		public int speed_increment = 3;
		public bool displaying_motion_model;
		
		DateTime last_position_update;
		pos3D curr_pos;
		
		// stereo disparities (prob/x/y/disp)
		List<byte[]> disparities;
		
		robotSurveyorThread update_thread2;
		Thread update_thread;

        private void Callback(object state)
        {
		}
		
		/// <summary>
		/// constructor
		/// </summary>
		public robotSurveyor() : base(1)
		{
			Name = "Surveyor SVS";
			
		    map_image_width = 640;
		    map = new byte[map_image_width*map_image_width*3];
		    map_bitmap = new Bitmap(map_image_width, map_image_width, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			
            WheelBase_mm = 90;
            WheelDiameter_mm = 30;
            WheelBaseForward_mm = 0;
					
			disparities = new List<byte[]>();
			curr_pos = new pos3D(0,0,0);
			last_position_update = DateTime.Now;
			UpdatePosition();
			
			// start the thread which updates the position
			update_thread2 = new robotSurveyorThread(new WaitCallback(Callback), this);
            update_thread = new Thread(new ThreadStart(update_thread2.Execute));
            update_thread.Priority = ThreadPriority.Normal;
            update_thread.Start();
			
		}
		
		~robotSurveyor()
		{
			update_thread2.StopUpdating();
		}

		/// <summary>
		/// Given the pwm value (speed value) turn this
		/// into a speed and uncertainty value in metric units
		/// </summary>
		/// <param name="pwm_value">PWM speed value sent to the motors</param>
		/// <param name="speed_mmsec">returned speed estimate in mm/sec</param>
		/// <param name="speed_uncertainty_mmsec">returned uncertainty of the speed estimate in mm/sec</param>
		public void GetSpeedFromPWM(
		    int pwm_value,
		    ref float speed_mmsec,
		    ref float speed_uncertainty_mmsec)
		{
	        speed_mmsec = 
	            reference_speed0 + 
	            ((pwm_value - reference_pwm0) * 
	             (reference_speed1 - reference_speed0) / 
	             (reference_pwm1 - reference_pwm0));
			
	        speed_uncertainty_mmsec = 
	            reference_variance0 + 
	            ((pwm_value - reference_pwm0) * 
	             (reference_variance1 - reference_variance0) / 
	             (reference_pwm1 - reference_pwm0));			
		}
		
		public void UpdatePosition()
		{
			DateTime t = DateTime.Now;
						
			TimeSpan diff = t.Subtract(last_position_update);
			float time_elapsed_sec = (float)diff.TotalSeconds;
			if (time_elapsed_sec > 0)
			{						
				float dist_mm = current_speed_mmsec * time_elapsed_sec;
				curr_pos.x += dist_mm * (float)Math.Sin(pan);
				curr_pos.y += dist_mm * (float)Math.Cos(pan);
				
	            updateFromKnownPosition(disparities, curr_pos.x, curr_pos.y, 0, pan, 0, 0);
				
				//Console.WriteLine("xy: " + x.ToString() + " " + y.ToString());
			}
			last_position_update = t;
		}
		
		public bool updating;
		public bool thread_stopped;
		
		public void UpdateSurveyor()
		{
			float horizon = 2000;
			DateTime save_motion_model_time = new DateTime(1990,1,1);
			pos3D map_centre = new pos3D(x,y,z);
			updating = true;
			while (updating)
			{
				if (current_speed_mmsec != 0)
				{
			        motion.speed_uncertainty_forward = current_speed_uncertainty_mmsec;
			        motion.speed_uncertainty_angular = 0.5f / 180.0f * (float)Math.PI;
				}
				else
				{
			        motion.speed_uncertainty_forward = 0;
			        motion.speed_uncertainty_angular = 0;
				}

				UpdatePosition();
				TimeSpan diff = DateTime.Now.Subtract(save_motion_model_time);
				if (diff.TotalSeconds >= 5)
				{
					float dx = map_centre.x - curr_pos.x;
					float dy = map_centre.y - curr_pos.y;
					float dz = map_centre.z - curr_pos.z;
					bool redraw_map = false;
					if (!displaying_motion_model)
					{
					    float dist = (float)Math.Sqrt(dx*dx + dy*dy + dz*dz);
						if (dist > 1000)
						{
							map_centre.x = curr_pos.x;
							map_centre.y = curr_pos.y;
							map_centre.z = curr_pos.z;
							redraw_map = true;
						}
						SaveMotionModel("motion_model.jpg", map_centre.x-horizon, map_centre.y-horizon, map_centre.x+horizon, map_centre.y+horizon, redraw_map);
					}
				}
				Thread.Sleep(500);				
			}
			thread_stopped = true;
		}
		
		public void TeleoperationCommand(string command)
		{
			char[] ch = command.ToCharArray();
			for (int i = 0; i < ch.Length; i++)
			{
				switch(ch[i])
				{
					case '+': // increase speed
					{
					    pwm_base_speed += speed_increment;
					    pwm_lspeed += speed_increment;
					    pwm_rspeed += speed_increment;					    
	                    break;
					}
					case '-': // decrease speed
					{
					    pwm_base_speed -= speed_increment;
					    pwm_lspeed -= speed_increment;
					    pwm_rspeed -= speed_increment;
	                    break;
					}
					case '.': // right
					{
					    pan += 20 * (float)Math.PI / 180.0f;
					    break;
				    }
					case '0': // left
					{
					    pan -= 20 * (float)Math.PI / 180.0f;
					    break;
				    }
					case '8': // forward
					{
						GetSpeedFromPWM(
						    pwm_base_speed,
						    ref current_speed_mmsec,
						    ref current_speed_uncertainty_mmsec);
					
	                    break;
					}
					case '2': // backward
					{
						GetSpeedFromPWM(
						    -pwm_base_speed,
						    ref current_speed_mmsec,
						    ref current_speed_uncertainty_mmsec);
					
	                    break;
					}
					case '5': // Stop
					{
					    current_speed_mmsec = 0;
	                    break;
					}
				}
			}
		}
		
		public void SaveMotionModel(
		    string filename,
		    float map_tx_mm,
		    float map_ty_mm,
		    float map_bx_mm,
		    float map_by_mm,
		    bool clear)
		{
            motion.Show(
		        map, map_image_width, map_image_width,
                map_tx_mm, map_ty_mm, map_bx_mm, map_by_mm,
				true, false,
                clear);		
			BitmapArrayConversions.updatebitmap_unsafe(map, map_bitmap);
			if (filename.EndsWith("jpg")) map_bitmap.Save(filename, System.Drawing.Imaging.ImageFormat.Jpeg);
			if (filename.EndsWith("bmp")) map_bitmap.Save(filename, System.Drawing.Imaging.ImageFormat.Bmp);
			if (filename.EndsWith("png")) map_bitmap.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
			if (filename.EndsWith("gif")) map_bitmap.Save(filename, System.Drawing.Imaging.ImageFormat.Gif);
		}
		
    }
}
