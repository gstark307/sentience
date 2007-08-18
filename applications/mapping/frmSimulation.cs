// /home/motters/develop/sentience/applications/mapping/frmSimulation.cs created with MonoDevelop
// User: motters at 3:46 PM 8/12/2007
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.IO;
using Gtk;

using sentience.core;
using sentience.learn;
using sluggish.utilities;
using sluggish.utilities.timing;
using Aced.Compression;

namespace mapping
{	
	public partial class frmSimulation : Gtk.Window
	{
		private simulation sim;
		
		public frmSimulation(simulation sim) : 
				base(Gtk.WindowType.Toplevel)
		{
			this.Build();			
			this.sim = sim;
		}

		protected virtual void OnCloseActivated (object sender, System.EventArgs e)
		{
		    
		}
		
		#region "autotuner"

	    // object used for auto tuning
	    selfopt autotuner;

	    /// <summary>
	    /// initialises the autotuner
	    /// </summary>
	    private void initAutotuner()
	    {
	        const int NO_OF_TUNING_PARAMETERS = 6;

	        autotuner = new selfopt(50, NO_OF_TUNING_PARAMETERS);

	        autotuner.smallerScoresAreBetter = true;
	        autotuner.parameterName[0] = "Motion model culling threshold";
	        autotuner.setParameterRange(0, 90, 95);
	        autotuner.setParameterStepSize(0, 1);

	        autotuner.parameterName[1] = "Localisation radius";
	        autotuner.setParameterRange(1, sim.rob.LocalGridCellSize_mm * 1, sim.rob.LocalGridCellSize_mm * 3);
	        autotuner.setParameterStepSize(1, sim.rob.LocalGridCellSize_mm);

	        autotuner.parameterName[2] = "Number of position uncertainty particles";
	        autotuner.setParameterRange(2, 100, 200);
	        autotuner.setParameterStepSize(2, 1);

	        autotuner.parameterName[3] = "Vacancy weighting";
	        autotuner.setParameterRange(3, 0.8f, 5.0f);
	        autotuner.setParameterStepSize(3, 0.0005f);

	        autotuner.parameterName[4] = "Surround radius percent";
	        autotuner.setParameterRange(4, 1.0f, 3.0f);
	        autotuner.setParameterStepSize(4, 0.0005f);

	        autotuner.parameterName[5] = "Matching threshold";
	        autotuner.setParameterRange(5, 5.0f, 20.0f);
	        autotuner.setParameterStepSize(5, 0.001f);

	        autotuner.Randomize(false);
	    }

	    /// <summary>
	    /// load up the next set of test parameters for evaluation
	    /// </summary>
	    private void nextAutotunerInstance()
	    {
	        String parameters = "";

	        for (int i = 0; i < autotuner.parameters_per_instance; i++)
	        {
	            parameters += Convert.ToString(autotuner.getParameter(i));
	            if (i < autotuner.parameters_per_instance - 1)
	                parameters += ",";
	        }
	        sim.SetTuningParameters(parameters);
	    }

	    #endregion
	    
	    #region "grid saving and loading"

	    /// <summary>
	    /// save the occupancy grid to file and show some benchmarks
	    /// </summary>
	    /// <param name="filename"></param>
	    private void SaveGrid(String filename)
	    {
	        FileStream fp = new FileStream(filename, FileMode.Create);
	        BinaryWriter binfile = new BinaryWriter(fp);

	        stopwatch grid_timer = new stopwatch();
	        grid_timer.Start();
	        byte[] grid_data = sim.rob.SaveGrid();
	        grid_timer.Stop();
	        Console.WriteLine("Distillation time  " + Convert.ToString(grid_timer.time_elapsed_mS) + " mS");

	        grid_timer.Start();
	        byte[] compressed_grid_data =
	        AcedDeflator.Instance.Compress(grid_data, 0, grid_data.Length,
	                                       AcedCompressionLevel.Fast, 0, 0);
	        grid_timer.Stop();

	        Console.WriteLine("Compression ratio  " + Convert.ToString(100-(int)(compressed_grid_data.Length * 100 / grid_data.Length)) + " %");
	        Console.WriteLine("Compression time   " + Convert.ToString(grid_timer.time_elapsed_mS) + " mS");

	        grid_timer.Start();
	        binfile.Write(compressed_grid_data);
	        grid_timer.Stop();
	        Console.WriteLine("Disk write time    " + Convert.ToString(grid_timer.time_elapsed_mS) + " mS");

	        binfile.Close();
	        fp.Close();
	    }

	    /// <summary>
	    /// load the occupancy grid from file and show some benchmarks
	    /// </summary>
	    /// <param name="filename"></param>
	    private void LoadGrid(String filename)
	    {
	        if (System.IO.File.Exists(filename))
	        {
	            stopwatch grid_timer = new stopwatch();

	            // read the data into a byte array
	            grid_timer.Start();
	            byte[] grid_data = System.IO.File.ReadAllBytes(filename);
	            grid_timer.Stop();
	            Console.WriteLine("Disk read time     " + Convert.ToString(grid_timer.time_elapsed_mS) + " mS");

	            // decompress the data
	            grid_timer.Start();
	            byte[] decompressed_grid_data =
	                    AcedInflator.Instance.Decompress(grid_data, 0, 0, 0);
	            grid_timer.Stop();
	            Console.WriteLine("Decompression time " + Convert.ToString(grid_timer.time_elapsed_mS) + " mS");

	            grid_timer.Start();
	            sim.rob.LoadGrid(decompressed_grid_data);
	            grid_timer.Stop();
	            Console.WriteLine("Creation time      " + Convert.ToString(grid_timer.time_elapsed_mS) + " mS");
	        }
	    }

	    #endregion 
		
	}
}
