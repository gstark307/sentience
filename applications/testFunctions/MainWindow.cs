/*
    Sentience 3D Perception System
    Copyright (C) 2000-2007 Bob Mottram
    fuzzgun@gmail.com

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.Collections;
using Gtk;
using sentience.core;
using sentience.pathplanner;
using sluggish.utilities;
using sluggish.utilities.gtk;

public partial class MainWindow: Gtk.Window
{	
    Random rnd = new Random();

    //Bitmap rays;
    byte[] img_rays;
    int standard_width = 500;
    int standard_height = 500;

    float[,,] grid_layer;
    int grid_dimension = 2000;

    private stereoModel stereo_model;
    stereoHead robot_head;
    float[] stereo_features;
    float[] stereo_uncertainties;
    private float[] pos3D_x;
    private float[] pos3D_y;
    
    /// <summary>
    /// initialise variables prior to performing test routines
    /// </summary>
    private void init()
    {
        grid_layer = new float[grid_dimension, grid_dimension, 3];

        pos3D_x = new float[4];
        pos3D_y = new float[4];

        stereo_model = new stereoModel();
        robot_head = new stereoHead(4);            
        stereo_features = new float[900];
        stereo_uncertainties = new float[900];

        img_rays = new byte[standard_width * standard_height * 3];  
        
		imgOutput.Pixbuf = GtkBitmap.createPixbuf(standard_width, standard_height);
		GtkBitmap.setBitmap(img_rays, imgOutput);        
    }

	public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
		Build ();
		
		init();
	}
	
	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}
	
	#region "test functions"

    /// <summary>
    /// perform a test
    /// </summary>
    /// <param name="test_index">index number of the test to be performed</param>        
    private void PerformTest(int test_index)
    {
        // initialise
        init();
        
        switch(test_index)
        {
            case 0:  // motion model
            {
                test_motion_model(true);
                break;
            }
            case 1:  // stereo ray models
            {
                bool mirror = false;
                stereo_model.showProbabilities(grid_layer, 
                                               grid_dimension, 
                                               img_rays, standard_width, standard_height, 
                                               false, true, mirror);
                break;
            }
            case 2:  // gaussian distribution function
            {
                stereo_model.showDistribution(img_rays, standard_width, standard_height);
                break;
            }
            case 3:  // a single stereo ray model
            {
                createSingleRayModel(false);
                break;
            }
            case 4:  // sensor model lookups
            {
                createSensorModelLookup();
                break;
            }
            case 5:  // path planner
            {
                test_path_planner(200, 50);
                break;
            }
        }
        
        // make a larger image
        byte[] img_large = image.downSample(img_rays, standard_width, standard_height, 
                                            standard_width*2, standard_height*2);
        
        // display the results
		imgOutput.Pixbuf = GtkBitmap.createPixbuf(standard_width*2, standard_height*2);
		GtkBitmap.setBitmap(img_large, imgOutput);        
    }

    /// <summary>
    /// generate scores for poses.  This is only for testing purposes.
    /// </summary>
    /// <param name="rob"></param>        
    private void surveyPosesDummy(robot rob)
    {
        motionModel motion_model = rob.GetBestMotionModel();
        
        // examine the pose list
        for (int sample = 0; sample < motion_model.survey_trial_poses; sample++)
        {
            particlePath path = (particlePath)motion_model.Poses[sample];
            particlePose pose = path.current_pose;

            float dx = rob.x - pose.x;
            float dy = rob.y - pose.y;
            float dist = (float)Math.Sqrt((dx * dx) + (dy * dy));
            float score = 1.0f / (1 + dist);

            // update the score for this pose
            motion_model.updatePoseScore(path, score);
        }

        // indicate that the pose scores have been updated
        motion_model.PosesEvaluated = true;
    }

    /// <summary>
    /// test the motion model in open or closed loop mode
    /// </summary>
    /// <param name="closed_loop">use closed loop mode</param>        
    private void test_motion_model(bool closed_loop)
    {
        robot rob = new robot(1);
        motionModel motion_model = rob.GetBestMotionModel();

        int min_x_mm = 0;
        int min_y_mm = 0;
        int max_x_mm = 1000;
        int max_y_mm = 1000;
        int step_size = (max_y_mm - min_y_mm) / 15;
        int x = (max_x_mm - min_x_mm) / 2;
        bool initial = true;
        float pan = 0; // (float)Math.PI / 4;
        for (int y = min_y_mm; y < max_y_mm; y += step_size)
        {
            if (closed_loop) surveyPosesDummy(rob);
            rob.updateFromKnownPosition(null, x, y, pan);
           
            motion_model.Show(img_rays, standard_width, standard_height, 
                            min_x_mm, min_y_mm, max_x_mm, max_y_mm,
                            initial);
            initial = false;
        }
    }

    private void createSingleRayModel(bool mirror)
    {      
        int divisor = 6;
        grid_dimension = 10000;
        grid_layer = new float[grid_dimension / divisor, grid_dimension, 3];
        stereo_model.showSingleRay(grid_layer, grid_dimension, img_rays, standard_width, standard_height, divisor, false, mirror);
    }

    private void createSensorModelLookup()
    {
        stereo_model.createLookupTable(32, img_rays, standard_width, standard_height);
    }
    
    private void test_path_planner(int grid_dimension, int cellSize_mm)
    {
        int grid_centre_x_mm = 0;
        int grid_centre_y_mm = 0;
        bool[][] navigable_space = new bool[grid_dimension][];
        for (int j = 0; j < navigable_space.Length; j++)
            navigable_space[j] = new bool[grid_dimension];


        pathplanner planner = new pathplanner(navigable_space, cellSize_mm, grid_centre_x_mm, grid_centre_y_mm);
       
        // create a test map
        int w = grid_dimension / 20;
        planner.AddNavigableSpace((grid_dimension / 2)-w, grid_dimension / 50, w + (grid_dimension / 2), grid_dimension - (grid_dimension / 50));
        planner.AddNavigableSpace((grid_dimension / 2) - w, (grid_dimension * 70 / 100), grid_dimension - (grid_dimension / 50), (grid_dimension * 70 / 100) + (w * 2));
        planner.AddNavigableSpace((grid_dimension / 5), (grid_dimension * 30 / 100), grid_dimension / 2, (grid_dimension * 30 / 100) + (w * 2));
        planner.AddNavigableSpace((grid_dimension / 20), (grid_dimension / 10), grid_dimension * 44 / 100, (grid_dimension * 50 / 100));
        planner.AddNavigableSpace((grid_dimension / 2), (grid_dimension * 20 / 100), grid_dimension - (grid_dimension / 50), (grid_dimension * 20 / 100) + (w * 2));
        planner.AddNavigableSpace((grid_dimension * 80 / 100), (grid_dimension * 50 / 100), (grid_dimension * 80 / 100) + (w * 2), (grid_dimension * 70 / 100));
        planner.AddNavigableSpace((grid_dimension * 57 / 100), (grid_dimension * 31 / 100), (grid_dimension * 95 / 100), (grid_dimension * 69 / 100));
        planner.AddNavigableSpace((grid_dimension / 20), (grid_dimension * 52 / 100), grid_dimension * 44 / 100, (grid_dimension * 95 / 100));             
        planner.AddNavigableSpace((grid_dimension * 20 / 100), (grid_dimension * 80 / 100), (grid_dimension * 50 / 100), (grid_dimension * 80 / 100) + (w * 2));
        planner.Update(0, 0, grid_dimension - 1, grid_dimension - 1);

        ArrayList plan = new ArrayList();
        int start_x=0, start_y=0, finish_x=0, finish_y=0;
        int i = 0;
        bool found = false;
        while ((i < 10000) && (!found))
        {
            start_x = 1 + rnd.Next(grid_dimension - 3);
            start_y = 1 + rnd.Next(grid_dimension - 3);
            if ((navigable_space[start_x][start_y]) && 
                (navigable_space[start_x-1][start_y-1]) &&
                (navigable_space[start_x - 1][start_y]))
                found = true;
            i++;
        }
        if (found)
        {
            found = false;
            i = 0;
            while ((i < 10000) && (!found))
            {
                finish_x = 1 + rnd.Next(grid_dimension - 3);
                finish_y = 1 + rnd.Next(grid_dimension - 3);
                if ((navigable_space[finish_x][finish_y]) &&
                    (navigable_space[finish_x-1][finish_y-1]) &&
                    (navigable_space[finish_x - 1][finish_y]))
                    found = true;
                i++;
            }
            if (found)
            {

                int start_xx = ((start_x - (grid_dimension / 2)) * cellSize_mm) + grid_centre_x_mm;
                int start_yy = ((start_y - (grid_dimension / 2)) * cellSize_mm) + grid_centre_y_mm;
                int finish_xx = ((finish_x - (grid_dimension / 2)) * cellSize_mm) + grid_centre_x_mm;
                int finish_yy = ((finish_y - (grid_dimension / 2)) * cellSize_mm) + grid_centre_y_mm;
                plan = planner.CreatePlan(start_xx, start_yy, finish_xx, finish_yy);
            }
        }

        planner.Show(img_rays, standard_width, standard_height, plan);

        drawing.drawCircle(img_rays, standard_width, standard_height,
                           start_x * standard_width / grid_dimension,
                           start_y * standard_height / grid_dimension,
                           standard_width / 100, 255, 0, 0, 1);
        drawing.drawCircle(img_rays, standard_width, standard_height,
                           finish_x * standard_width / grid_dimension,
                           finish_y * standard_height / grid_dimension,
                           standard_width / 100, 255, 0, 255, 1);
    }    
    
    #endregion
    
    #region "Run menu selections"

    protected virtual void OnMotionModelActivated (object sender, System.EventArgs e)
    {
        PerformTest(0);
    }

    protected virtual void OnSingleStereoRayModelActivated (object sender, System.EventArgs e)
    {
        PerformTest(3);
    }

    protected virtual void OnStereoRaysAtVariousRangesActivated (object sender, System.EventArgs e)
    {
        PerformTest(1);
    }

    protected virtual void OnGaussianFunctionActivated (object sender, System.EventArgs e)
    {
        PerformTest(2);
    }

    protected virtual void OnSensorModelLookupGraphsActivated (object sender, System.EventArgs e)
    {
        PerformTest(4);
    }

    protected virtual void OnPathPlannerActivated (object sender, System.EventArgs e)
    {
        PerformTest(5);
    }
 	
	#endregion
}