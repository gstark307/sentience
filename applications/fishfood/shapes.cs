using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace sentience.calibration
{
    /// <summary>
    /// detecting shapes such as squares
    /// </summary>
    public class shapes
    {
        #region "detecting calibration dots"
        
    	public static List<calibrationDot> DetectDots(string filename, 
                                                 int grouping_radius_percent,                                                    
                                                 int erosion_dilation,
                                                 float minimum_width, 
                                                 float maximum_width,
                                                 ref List<int> edges,
                                                 string edges_filename,
                                                 string groups_filename,
                                                 string output_filename,
                                                 ref int image_width, ref int image_height)
        {
            Bitmap bmp = (Bitmap)Bitmap.FromFile(filename);
            byte[] img_colour = new byte[bmp.Width * bmp.Height * 3];
            BitmapArrayConversions.updatebitmap(bmp, img_colour);

            image_width = bmp.Width;
            image_height = bmp.Height;
            
            // convert the colour image to mono
            byte[] img_mono = image.monoImage(img_colour, bmp.Width, bmp.Height);

            List<List<int>> groups = null;     
            List<calibrationDot> dots = DetectDots(img_mono, img_colour,
                                              bmp.Width, bmp.Height,
                                              grouping_radius_percent,
                                              erosion_dilation,
                                              minimum_width, maximum_width,
                                              ref edges,
                                              ref groups);

            if (groups_filename != "")
            {
                if (groups != null)
                {
                    byte[] grp = null;
                    byte[] img_edges = ShowEdges(ref grp, groups, bmp.Width, bmp.Height);
                    Bitmap output_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);            
                    BitmapArrayConversions.updatebitmap_unsafe(img_edges, output_bmp);
                    if (edges_filename.EndsWith("bmp"))
                        output_bmp.Save(groups_filename, System.Drawing.Imaging.ImageFormat.Bmp);
                    if (edges_filename.EndsWith("jpg"))
                        output_bmp.Save(groups_filename, System.Drawing.Imaging.ImageFormat.Jpeg);
                }
            }

            if (edges_filename != "")
            {
                byte[] img_edges = ShowEdges(edges, bmp.Width, bmp.Height);
                Bitmap output_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);            
                BitmapArrayConversions.updatebitmap_unsafe(img_edges, output_bmp);
                if (edges_filename.EndsWith("bmp"))
                    output_bmp.Save(edges_filename, System.Drawing.Imaging.ImageFormat.Bmp);
                if (edges_filename.EndsWith("jpg"))
                    output_bmp.Save(edges_filename, System.Drawing.Imaging.ImageFormat.Jpeg);
            }

            if (output_filename != "")
            {
                byte[] img_output = img_colour;
                if (dots != null)
                {
                    int r = 0;
                    int g = 255;
                    int b = 0;
                    for (int i = 0; i < dots.Count; i++)
                    {
                        int rr = r;
                        int gg = g;
                        int bb = b;
                        
                        if (dots[i].centre)
                        {
                            rr = 255;
                            gg = 0;
                            bb = 0;
                        }
                        drawing.drawCross(img_output, bmp.Width, bmp.Height, (int)dots[i].x, (int)dots[i].y, (int)dots[i].radius + 1, rr,gg,bb,0);
                    }
                }
                                                   
                Bitmap output_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);            
                BitmapArrayConversions.updatebitmap_unsafe(img_output, output_bmp);
                if (output_filename.EndsWith("bmp"))
                    output_bmp.Save(output_filename, System.Drawing.Imaging.ImageFormat.Bmp);
                if (output_filename.EndsWith("jpg"))
                    output_bmp.Save(output_filename, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
            return(dots);
        }
        
        
    	public static List<calibrationDot> DetectDots(byte[] mono_img, byte[] img_colour, 
                                                      int img_width, int img_height,
                                                      int grouping_radius_percent,
                                                      int erosion_dilation,
                                                      float minimum_width,
                                                      float maximum_width,
                                                      ref List<int> edges,
                                                      ref List<List<int>> groups)
        {
            // create a list to store the results
            List<calibrationDot> dots = null;
            const int connect_edges_radius = 5;
            int minimum_width_pixels = (int)(minimum_width * img_width / 100);
            int maximum_width_pixels = (int)(maximum_width * img_width / 100);
            int minimum_size_percent = (int)((minimum_width_pixels*minimum_width_pixels) * 100 / (img_width*img_height));
            
            // maximum recursion depth when joining edges
            const int max_search_depth = 8000;
            
            // create edge detection object
            CannyEdgeDetector edge_detector = new CannyEdgeDetector();
            edge_detector.automatic_thresholds = true;  
            edge_detector.enable_edge_orientations = false;

            byte[] img_mono = (byte[])mono_img.Clone();

            // erode
            if (erosion_dilation > 0)
            {       
                byte[] eroded = image.Erode(img_mono, img_width, img_height, 
                                            erosion_dilation, 
                                            null);
                img_mono = eroded;
            }
            
            // dilate
            if (erosion_dilation < 0)
            {
                byte[] dilated = image.Dilate(img_mono, img_width, img_height, 
                                             -erosion_dilation, 
                                              null);
                img_mono = dilated;
            }
                
            // detect edges using canny algorithm
            edge_detector.Update(img_mono, img_width, img_height);

            // connect edges which are a short distance apart
            edge_detector.ConnectBrokenEdges(connect_edges_radius);
                
            edges = edge_detector.edges;

            // group edges together into objects
            groups = GetGroups(edge_detector.edges, 
                               img_width, img_height, 0, 
                               minimum_size_percent,
                               false, max_search_depth, false, 
                               grouping_radius_percent);
                                        
            if (groups != null)
            {
                dots = GetDots(groups, img_colour,
                               img_width, img_height,
                               0.7f, 1.3f,
                               minimum_width_pixels, maximum_width_pixels);
            }

            // remove any overlapping squares
            //dot_shapes = SelectSquares(dot_shapes, 0);
            
            return(dots);
        }
                
        #endregion
    
        #region "binarizing an image"

        /// <summary>
        /// calculate a global threshold based upon an intensity histogram
        /// </summary>
        /// <param name="histogram">histogram data</param>
        /// <param name="MeanDark">returned mean dark value</param>
        /// <param name="MeanLight">returned mean light value</param>
        /// <param name="DarkRatio">ratio of dark pixels to light</param>
        /// <returns>global threshold value</returns>
        private static unsafe float GetGlobalThreshold(float[] histogram,
                                                       ref float MeanDark, ref float MeanLight,
                                                       ref float DarkRatio)
        {
            float global_threshold = 0;

            float Tmin = 0;
            float Tmax = 0;
            float MinVariance = 999999;  // some large figure
            float currMeanDark, currMeanLight, VarianceDark, VarianceLight;

            float DarkHits = 0;
            float LightHits = 0;
            float BestDarkHits = 0;
            float BestLightHits = 0;
            int histlength = histogram.Length;
            MeanDark = 0;
            MeanLight = 0;
            
            // calculate squared magnitudes
            // this avoids unnecessary multiplications later on
            float[] histogram_squared_magnitude = new float[histogram.Length];
            for (int i = 0; i < histogram.Length; i++)
                histogram_squared_magnitude[i] = histogram[i] * histogram[i];

            //const float threshold_increment = 0.25f;
            const int max_grey_levels = 255;
            
            // precompute some values to avoid excessive divisions
            float mult1 = histlength / (float)max_grey_levels;
            float mult2 = 255.0f / max_grey_levels;
            
            int h, bucket;
            float magnitude_sqr, Variance, divisor;

            fixed (float* unsafe_histogram = histogram_squared_magnitude)
            {
                // evaluate all possible thresholds
				for (int grey_level = max_grey_levels-1; grey_level >= 0; grey_level--)
                {
                    // compute mean and variance for this threshold
                    // in a struggle between light and darkness
                    DarkHits = 0;
                    LightHits = 0;
                    currMeanDark = 0;
                    currMeanLight = 0;
                    VarianceDark = 0;
                    VarianceLight = 0;
					
					//bucket = grey_level * histlength / max_grey_levels;
                    bucket = (int)(grey_level * mult1);
                    for (h = histlength-1; h >= 0; h--)
                    {
                        magnitude_sqr = unsafe_histogram[h];
                        if (h < bucket)
                        {
                            currMeanDark += h * magnitude_sqr;
                            VarianceDark += (bucket - h) * magnitude_sqr;
                            DarkHits += magnitude_sqr;
                        }
                        else
                        {
                            currMeanLight += h * magnitude_sqr;
                            VarianceLight += (bucket - h) * magnitude_sqr;
                            LightHits += magnitude_sqr;
                        }
                    }
                    					
                    // compute means
                    if (DarkHits > 0)
                    {
                        // rescale into 0-255 range
						divisor = DarkHits * histlength;
                        currMeanDark = currMeanDark * 255 / divisor;
                        VarianceDark = VarianceDark * 255 / divisor;
                    }
                    if (LightHits > 0)
                    {
                        // rescale into 0-255 range
						divisor = LightHits * histlength;
                        currMeanLight = currMeanLight * 255 / divisor;
                        VarianceLight = VarianceLight * 255 / divisor;
                    }

                    Variance = VarianceDark + VarianceLight;
                    if (Variance < 0) Variance = -Variance;

                    if (Variance < MinVariance)
                    {
                        MinVariance = Variance;
                        Tmin = grey_level * mult2;
                        //Tmin = grey_level * 255.0f / max_grey_levels;
                        MeanDark = currMeanDark;
                        MeanLight = currMeanLight;
                        BestDarkHits = DarkHits;
                        BestLightHits = LightHits;
                    }
                    if ((int)(Variance * 1000) == (int)(MinVariance * 1000))
                    {
                        //Tmax = grey_level * 255.0f / max_grey_levels;
                        Tmax = grey_level * mult2;
                        MeanLight = currMeanLight;
                        BestLightHits = LightHits;
                    }
                }
            }

            global_threshold = (Tmin + Tmax) / 2;

            if (BestLightHits + BestDarkHits > 0) 
                DarkRatio = BestDarkHits * 100 / (BestLightHits + BestDarkHits);

            return (global_threshold);
        }


        /// <summary>
        /// performs a simple kind of binarization on the given image
        /// </summary>
        /// <param name="filename">
        /// A <see cref="System.String"/>
        /// </param>
        /// <param name="binarized_filename">
        /// A <see cref="System.String"/>
        /// </param>
        public static void BinarizeSimple(string filename,
                                          int vertical_integration_percent,
                                          string binarized_filename)
        {
            if (File.Exists(filename))
            {
                Bitmap bmp = (Bitmap)Bitmap.FromFile(filename);
                byte[] img = new byte[bmp.Width * bmp.Height * 3];
                BitmapArrayConversions.updatebitmap(bmp, img);
                
                byte[] binary_image = BinarizeSimple(img, bmp.Width, bmp.Height, vertical_integration_percent);
                byte[] binary_image_colour = image.colourImage(binary_image, bmp.Width, bmp.Height, null);
                Bitmap bmp_binary = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                BitmapArrayConversions.updatebitmap_unsafe(binary_image_colour, bmp_binary);
                if (binarized_filename.ToLower().EndsWith("jpg"))
                    bmp_binary.Save(binarized_filename, System.Drawing.Imaging.ImageFormat.Jpeg);
                if (binarized_filename.ToLower().EndsWith("bmp"))
                    bmp_binary.Save(binarized_filename, System.Drawing.Imaging.ImageFormat.Bmp);
            }
        }
        
        /// <summary>
        /// performs a simple kind of binarization on the entire image
        /// </summary>
        /// <param name="img">
        /// image data <see cref="System.Byte"/>
        /// </param>
        /// <param name="img_width">
        /// width of the image <see cref="System.Int32"/>
        /// </param>
        /// <param name="img_height">
        /// height of the image <see cref="System.Int32"/>
        /// </param>
        /// <returns>
        /// mono binarized image <see cref="System.Byte"/>
        /// </returns>
        public static byte[] BinarizeSimple(byte[] img,
                                            int img_width, int img_height,
                                            int vertical_integration_percent)
        {
            // get a mono version of the image
            byte[] img_mono = img;
            if (img.Length > img_width * img_height)
            {
                img_mono = new byte[img_width*img_height];
                int bytes_per_pixel = img.Length / (img_width * img_height);
                int n = 0;
                for (int i = 0; i < img.Length; i += bytes_per_pixel)
                {
                    int intensity = 0;
                    for (int b = 0; b < bytes_per_pixel; b++)
                        intensity = img[i + b];
                    img_mono[n++] = (byte)(intensity / bytes_per_pixel);
                }
            }
            else
            {
                img_mono = img;
            }
            
            byte[] binary_image = new byte[img_mono.Length];
            float mean_light = 0;
            float mean_dark = 0;
            float dark_ratio = 0;
            byte threshold = 0;
            int w = img_width * (img_height * vertical_integration_percent / 100);
            int w2 = (img_width * img_height) - 1 - w;
            int histogram_sampling_step = 5;
            for (int y = 0; y < img_height; y++)
            {
                int n1 = (y * img_width);
                int n2 = n1 + img_width;
                
                // create a histogram of the image row
                float[] histogram = new float[256];                
                for (int i = n1; i < n2; i += histogram_sampling_step)
                {
                    histogram[img_mono[i]]++;
                    if (i > w) histogram[img_mono[i-w]]++;
                    if (i < w2) histogram[img_mono[i+w]]++;
                }
                
                // find the threshold for this row
                threshold = (byte)GetGlobalThreshold(histogram, ref mean_dark, ref mean_light, ref dark_ratio);
                
                for (int i = n1; i < n2; i++)
                    if (img_mono[i] > threshold)
                        binary_image[i] = (byte)255;
            }

            w = img_width * vertical_integration_percent / 100;
            w2 = img_width - 1 - w;
            for (int x = 0; x < img_width; x++)
            {
                
                // create a histogram of the image row
                float[] histogram = new float[256];                
                for (int y = 0; y < img_height; y += histogram_sampling_step)
                {
                    int n = (y * img_width) + x;
                    histogram[img_mono[n]]++;
                    if (x > w) histogram[img_mono[n-w]]++;
                    if (x < w2) histogram[img_mono[n+w]]++;
                }
                
                // find the threshold for this row
                threshold = (byte)GetGlobalThreshold(histogram, ref mean_dark, ref mean_light, ref dark_ratio);
                
                for (int y = 0; y < img_height; y++)
                {
                    int n = (y * img_width) + x;
                    if (img_mono[n] > threshold)
                        binary_image[n] = (byte)255; 
                }
            }

            return(binary_image);
        }

        public static void RemoveSurroundingBlob(string filename,
                                                 bool black_on_white,
                                                 string output_filename)
        {
            if (File.Exists(filename))
            {
                Bitmap bmp = (Bitmap)Bitmap.FromFile(filename);
                byte[] img = new byte[bmp.Width * bmp.Height * 3];
                BitmapArrayConversions.updatebitmap(bmp, img);
                
                RemoveSurroundingBlob(img, bmp.Width, bmp.Height, black_on_white);
                
                Bitmap bmp_output = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                BitmapArrayConversions.updatebitmap_unsafe(img, bmp_output);
                if (output_filename.ToLower().EndsWith("jpg"))
                    bmp_output.Save(output_filename, System.Drawing.Imaging.ImageFormat.Jpeg);
                if (output_filename.ToLower().EndsWith("bmp"))
                    bmp_output.Save(output_filename, System.Drawing.Imaging.ImageFormat.Bmp);
            }
        }
        
        public static void RemoveSurroundingBlob(byte[] img, int img_width, int img_height,
                                                 bool black_on_white)
        {
            byte[] blob_image = BinarizeSimple(img, img_width, img_height, 20);
            
            int n, black_to_white_transition;
            int maximum_continuous_white = img_width / 12;
            
            bool[] surrounding_blob_image = new bool[blob_image.Length];
            for (int i = 0; i < blob_image.Length; i++)
                if (blob_image[i] == 255) surrounding_blob_image[i] = true;

            // remove vertical sections
            for (int x = 0; x < img_width; x++)
            {
                black_to_white_transition = -1;
                for (int y = 1; y < img_height; y++)
                {
                    n = (y * img_width) + x;
                    
                    if (surrounding_blob_image[n] != surrounding_blob_image[n - img_width])
                    {             
                        if ((surrounding_blob_image[n] != black_on_white) &&
                            (surrounding_blob_image[n - img_width] == black_on_white))
                        {
                            // notice transitions from black to white
                            black_to_white_transition = y;
                        }
                        else
                        {
                            if (black_to_white_transition > -1)
                            {
                                int y2 = y;
                                int continuous_white = y2 - black_to_white_transition;
                                if (continuous_white < maximum_continuous_white)
                                {
                                    // fill in as black
                                    for (int yy = black_to_white_transition; yy < y2; yy++)
                                    {
                                        int n2 = (yy * img_width) + x;
                                        surrounding_blob_image[n2] = black_on_white;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            // remove horizontal sections
            for (int y = 0; y < img_height; y++)
            {
                black_to_white_transition = -1;
                for (int x = 1; x < img_width; x++)
                {
                    n = (y * img_width) + x;
                    
                    if (surrounding_blob_image[n] != surrounding_blob_image[n - 1])
                    {                            
                        if ((surrounding_blob_image[n] != black_on_white) &&
                            (surrounding_blob_image[n - 1] == black_on_white))
                        {
                            // notice transitions from black to white
                            black_to_white_transition = x;
                        }
                        else
                        {
                            if (black_to_white_transition > -1)
                            {
                                int x2 = x;
                                int continuous_white = x2 - black_to_white_transition;
                                if (continuous_white < maximum_continuous_white)
                                {
                                    // fill in as black
                                    for (int xx = black_to_white_transition; xx < x2; xx++)
                                    {
                                        int n2 = (y * img_width) + xx;
                                        surrounding_blob_image[n2] = black_on_white;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // erode or dilate the surrounding blob
            bool high = true;
            bool low = false;
            if (black_on_white)
            {
                high = false;
                low = true;
            }            
            int erode_dilate = 10;
            int max = (img_width * (img_height-1)) - 1;
            bool[] new_surrounding_blob_image = (bool[])surrounding_blob_image.Clone();
            for (int i = 0; i < erode_dilate; i++)
            {
                if (i > 0)
                {
                    for (int j = 0; j < surrounding_blob_image.Length; j++)
                        new_surrounding_blob_image[j] = surrounding_blob_image[j];
                }
                
                for (int j = 1; j < surrounding_blob_image.Length-1; j++)
                {
                    if (surrounding_blob_image[j] == low)
                    {
                        if ((surrounding_blob_image[j-1] == high) ||
                            (surrounding_blob_image[j+1] == high))
                        {
                            new_surrounding_blob_image[j] = high;
                        }
                        else
                        {
                            if (j < max)
                            {
                                if (surrounding_blob_image[j + img_width] == high)
                                    new_surrounding_blob_image[j] = high;
                            }
                            if (j < max-1)
                            {
                                if (surrounding_blob_image[j + img_width + 1] == high)
                                    new_surrounding_blob_image[j] = high;
                            }
                            if (j < max+1)
                            {
                                if (surrounding_blob_image[j + img_width - 1] == high)
                                    new_surrounding_blob_image[j] = high;
                            }
                            if (j > img_width)
                            {
                                if (surrounding_blob_image[j - img_width] == high)
                                    new_surrounding_blob_image[j] = high;
                            }
                            if (j > img_width+1)
                            {
                                if (surrounding_blob_image[j - img_width - 1] == high)
                                    new_surrounding_blob_image[j] = high;
                            }
                            if (j > img_width-1)
                            {
                                if (surrounding_blob_image[j - img_width + 1] == high)
                                    new_surrounding_blob_image[j] = high;
                            }
                        }
                    }
                }

                for (int j = 0; j < surrounding_blob_image.Length; j++)
                    surrounding_blob_image[j] = new_surrounding_blob_image[j];
            }
            
            
            //remove the background from the original image
            n = 0;
            byte background_intensity = 0;
            if (black_on_white) background_intensity = 255;
            int bytes_per_pixel = img.Length / (img_width * img_height);
            for (int y = 0; y < img_height; y++)
            {
                for (int x = 0; x < img_width; x++)
                {
                    if (surrounding_blob_image[n] == true)
                    {
                        for (int b = 0; b < bytes_per_pixel; b++)
                            img[(n * bytes_per_pixel) + b] = background_intensity;
                    }
                    n++;
                }
            }
        }        
        
        #endregion
        
                    
        #region "detect rectangles"
        
    	public static List<polygon2D> DetectRectangles(byte[] img_colour, 
                                                       int img_width, int img_height,                                                    
                                                       int[] grouping_radius_percent,                                                    
                                                       int[] erosion_dilation,
                                                       bool black_on_white,
                                                       int accuracy_level,
                                                       bool debug)
        {
            int downsampled_width = img_width;
            switch(accuracy_level)
            {
                case 0: { downsampled_width = 320; break; }
                case 1: { downsampled_width = 480; break; }
                case 2: { downsampled_width = 640; break; }
            }
            List<int> edges = null;
            return(DetectSquares(img_colour, img_width, img_height,
                                 false, 0,
                                 grouping_radius_percent, erosion_dilation,
                                 black_on_white,
                                 10, 0.3f, 20.0f,
                                 downsampled_width, false, debug, 0, ref edges));
        }
        
        
        #endregion
        
        #region "detect squares"

        /// <summary>
        /// detects square regions within the given colour image 
        /// </summary>
        /// <param name="img_colour">
        /// colour image data <see cref="System.Byte"/>
        /// </param>
        /// <param name="img_width">
        /// image width <see cref="System.Int32"/>
        /// </param>
        /// <param name="img_height">
        /// image height <see cref="System.Int32"/>
        /// </param>
        /// <param name="grouping_radius_percent">
        /// radius used to group adjacent edge features <see cref="System.Int32"/>
        /// </param>
        /// <param name="erosion_dilation">
        /// erosion dilation level <see cref="System.Int32"/>
        /// </param>
        /// <param name="black_on_white">
        /// whether this image contains dark markings on a lighter background <see cref="System.Boolean"/>
        /// </param>
        /// <param name="accuracy_level">
        /// desired level of accuracy in the range 0-2.  0 = least accurate but fastest <see cref="System.Boolean"/>
        /// </param>
        /// <param name="debug">
        /// save extra debugging info
        /// </param>
        /// <returns>
        /// list of polygons representing the square regions <see cref="List`1"/>
        /// </returns>
    	public static List<polygon2D> DetectSquares(byte[] img_colour, 
                                                    int img_width, int img_height,                                                    
                                                    int[] grouping_radius_percent,                                                    
                                                    int[] erosion_dilation,
                                                    bool black_on_white,
                                                    int accuracy_level,
                                                    bool debug,
                                                    int circular_ROI_radius,
                                                    ref List<int> edges)
        {
            int downsampled_width = img_width;
            switch(accuracy_level)
            {
                case 0: { downsampled_width = 320; break; }
                case 1: { downsampled_width = 480; break; }
                case 2: { downsampled_width = 640; break; }
            }
            return(DetectSquares(img_colour, img_width, img_height,
                                 false, 0,
                                 grouping_radius_percent, erosion_dilation,
                                 black_on_white,
                                 10, 0.8f, 1.2f,
                                 downsampled_width, true, debug,
                                 circular_ROI_radius,
                                 ref edges)); 
        }
        
        /// <summary>
        /// detects square regions within the given colour image
        /// </summary>
        /// <param name="img_colour">
        /// colour image data <see cref="System.Byte"/>
        /// </param>
        /// <param name="img_width">
        /// image width <see cref="System.Int32"/>
        /// </param>
        /// <param name="img_height">
        /// image height <see cref="System.Int32"/>
        /// </param>
        /// <param name="ignore_periphery">
        /// whether to ignore edge features which stray into the border of the image <see cref="System.Boolean"/>
        /// </param>
        /// <param name="image_border_percent">
        /// percentage of the image considred to be the surrounding border <see cref="System.Int32"/>
        /// </param>
        /// <param name="grouping_radius_percent">
        /// radius used to group adjacent edge features <see cref="System.Int32"/>
        /// </param>
        /// <param name="erosion_dilation">
        /// erosion dilation level <see cref="System.Int32"/>
        /// </param>
        /// <param name="black_on_white">
        /// whether this image contains dark markings on a lighter background <see cref="System.Boolean"/>
        /// </param>
        /// <param name="minimum_volume_percent">
        /// minimum volume of the square as a percentage of the image volume (prevents very small stuff being detected) <see cref="System.Int32"/>
        /// </param>
        /// <param name="minimum_aspect_ratio">
        /// minimum aspect ratio when searching for valid regions
        /// </param>
        /// <param name="maximum_aspect_ratio">
        /// maximum aspect ratio when searching for valid regions
        /// </param>
        /// <param name="debug">
        /// save extra debugging info
        /// </param>
        /// <returns>
        /// list of polygons representing the square regions <see cref="List`1"/>
        /// </returns>
    	public static List<polygon2D> DetectSquares(byte[] img_colour, 
                                                    int img_width, int img_height,
                                                    bool ignore_periphery,
                                                    int image_border_percent,
                                                    int[] grouping_radius_percent,                                                    
                                                    int[] erosion_dilation,
                                                    bool black_on_white,
                                                    int minimum_volume_percent, 
                                                    float minimum_aspect_ratio,
                                                    float maximum_aspect_ratio,
                                                    int downsampled_width,
                                                    bool squares_only,
                                                    bool debug,
                                                    int circular_ROI_radius,
                                                    ref List<int> edges) 
        {
            int original_img_width = img_width;
            
            // downsample the original image
            if (downsampled_width < img_width)
            {
                img_colour = image.downSample(img_colour, img_width, img_height,
                                              downsampled_width, img_height * downsampled_width / img_width);

                img_height = img_height * downsampled_width / img_width;
                img_width = downsampled_width;
            }
            
            // convert the colour image to mono
            byte[] img_mono = image.monoImage(img_colour, img_width, img_height);
            
            List<polygon2D> squares = DetectSquaresMono(img_mono, img_width, img_height,
                                                        ignore_periphery,
                                                        image_border_percent,
                                                        grouping_radius_percent,
                                                        erosion_dilation,
                                                        black_on_white,
                                                        minimum_volume_percent, true,
                                                        minimum_aspect_ratio, maximum_aspect_ratio,
                                                        squares_only, debug,
                                                        circular_ROI_radius,
                                                        ref edges);

            // re-scale points back into the original image resolution
            if (img_width < original_img_width)
            {
                for (int i = 0; i < squares.Count; i++)
                {
                    for (int vertex = 0; vertex < 4; vertex++)
                    {
                        squares[i].x_points[vertex] = squares[i].x_points[vertex] * original_img_width / img_width;
                        squares[i].y_points[vertex] = squares[i].y_points[vertex] * original_img_width / img_width;
                    }
                }
            }
            
            return(squares);
        }

        /// <summary>
        /// detects square shapes within the given mono image
        /// </summary>
        /// <param name="img_mono">
        /// mono image data <see cref="System.Byte"/>
        /// </param>
        /// <param name="img_width">
        /// image width <see cref="System.Int32"/>
        /// </param>
        /// <param name="img_height">
        /// image height <see cref="System.Int32"/>
        /// </param>
        /// <param name="ignore_periphery">
        /// whether to ignore edge features which stray into the border of the image <see cref="System.Boolean"/>
        /// </param>
        /// <param name="image_border_percent">
        /// percentage of the image considred to be the surrounding border <see cref="System.Int32"/>
        /// </param>
        /// <param name="grouping_radius_percent">
        /// radius used to group adjacent edge features <see cref="System.Int32"/>
        /// </param>
        /// <param name="erosion_dilation">
        /// erosion dilation level <see cref="System.Int32"/>
        /// </param>
        /// <param name="black_on_white">
        /// whether this image contains dark markings on a lighter background <see cref="System.Boolean"/>
        /// </param>
        /// <param name="minimum_volume_percent">
        /// minimum volume of the square as a percentage of the image volume (prevents very small stuff being detected) <see cref="System.Int32"/>
        /// </param>
        /// <param name="use_original_image">
        /// whether to allow img_mono to be altered
        /// </param>
        /// <param name="minimum_aspect_ratio">
        /// minimum aspect ratio when searching for valid regions
        /// </param>
        /// <param name="maximum_aspect_ratio">
        /// maximum aspect ratio when searching for valid regions
        /// </param>
        /// <returns>
        /// list of polygons representing the square regions <see cref="List`1"/>
        /// </returns>
    	public static List<polygon2D> DetectSquaresMono(byte[] mono_img, 
                                                        int img_width, int img_height,
                                                        bool ignore_periphery,
                                                        int image_border_percent,
                                                        int[] grouping_radius_percent,
                                                        int[] erosion_dilation,
                                                        bool black_on_white,
                                                        int minimum_volume_percent,
                                                        bool use_original_image,
                                                        float minimum_aspect_ratio,
                                                        float maximum_aspect_ratio,
                                                        bool squares_only,
                                                        bool debug,
                                                        int circular_ROI_radius,
                                                        ref List<int> edges)
        {
#if SHOW_TIMINGS
            stopwatch timer_square_detection = new stopwatch();
            timer_square_detection.Start();
#endif
            const int minimum_size_percent = 5;
            const int connect_edges_radius = 5;
            
            // maximum recursion depth when joining edges
            const int max_search_depth = 8000;
            
            // create a list to store the results
            List<polygon2D> square_shapes = new List<polygon2D>();
            
            // create edge detection object
            CannyEdgeDetector edge_detector = new CannyEdgeDetector();
            edge_detector.automatic_thresholds = true;  
            edge_detector.enable_edge_orientations = false;

#if SHOW_TIMINGS
            stopwatch timer_copy_invert = new stopwatch();
            timer_copy_invert.Start();
#endif
            
            byte[] img_mono = mono_img;
            if (!use_original_image)
            {
                // make a copy of the original image (we don't want to alter it)
                img_mono = (byte[])mono_img.Clone();
            }

            // if the image contains light markings on a darker background invert the pixels            
            if (!black_on_white)
            {
                for (int i = img_mono.Length-1; i >= 0; i--)
                {
                    img_mono[i] = (byte)(255 - img_mono[i]);
                }
            }

#if SHOW_TIMINGS
            timer_copy_invert.Stop();
            if (timer_copy_invert.time_elapsed_mS > 20)
                Console.WriteLine("DetectSquaresMono: copy/invert  " + timer_copy_invert.time_elapsed_mS.ToString() );
#endif
                            
            int image_border = img_width * image_border_percent / 100;

            byte[] img_mono2 = null;                        
            byte[] eroded = null;
            byte[] dilated = null;
            bool previous_eroded = false;
            bool previous_dilated = false;
            
            // try each erosion/dilation level
            for (int erosion_dilation_level = 0; erosion_dilation_level < erosion_dilation.Length; erosion_dilation_level++)
            {

#if SHOW_TIMINGS
                stopwatch timer_erode_dilate = new stopwatch();
                timer_erode_dilate.Start();
#endif
                
                // erode
                if (erosion_dilation[erosion_dilation_level] > 0)
                {                    
                    if (previous_eroded)
                        eroded = image.Erode(eroded, img_width, img_height, 
                                             erosion_dilation[erosion_dilation_level] - erosion_dilation[erosion_dilation_level-1], 
                                             eroded);
                    else
                        eroded = image.Erode(img_mono, img_width, img_height, 
                                             erosion_dilation[erosion_dilation_level], 
                                             null);
                    img_mono2 = eroded;
                    previous_eroded = true;
                }
                
                // dilate
                if (erosion_dilation[erosion_dilation_level] < 0)
                {
                    if (previous_dilated)
                        dilated = image.Dilate(dilated, img_width, img_height, 
                                               -erosion_dilation[erosion_dilation_level] + erosion_dilation[erosion_dilation_level-1], 
                                               dilated);
                    else
                        dilated = image.Dilate(img_mono, img_width, img_height, 
                                               -erosion_dilation[erosion_dilation_level], 
                                               null);
                    img_mono2 = dilated;
                    previous_dilated = true;
                }
                
                // just copy the original image
                if (erosion_dilation[erosion_dilation_level] == 0)
                {
                    if (erosion_dilation_level > 0)
                        img_mono2 = (byte[])img_mono.Clone();
                    else
                        img_mono2 = img_mono;
                    previous_eroded = false;
                    previous_dilated = false;
                }

#if SHOW_TIMINGS
                timer_erode_dilate.Stop();
                if (timer_erode_dilate.time_elapsed_mS > 20)
                    Console.WriteLine("DetectSquaresMono: erode/dilate  " + timer_erode_dilate.time_elapsed_mS.ToString() );

                stopwatch timer_edge_detection = new stopwatch();
                timer_edge_detection.Start();
#endif                
                // detect edges using canny algorithm
                edge_detector.Update(img_mono2, img_width, img_height);

#if SHOW_TIMINGS
                timer_edge_detection.Stop();
                if (timer_edge_detection.time_elapsed_mS > 20)
                    Console.WriteLine("DetectSquaresMono: edge detect  " + timer_edge_detection.time_elapsed_mS.ToString() );
#endif
                
                if (debug)
                {
                    string filename = "canny_magnitudes_" + erosion_dilation_level.ToString() + ".bmp";
                    byte[] magnitudes = new byte[img_width * img_height * 3];
                    edge_detector.ShowMagnitudes(magnitudes, img_width, img_height);
                    Bitmap magnitudes_bmp = new Bitmap(img_width, img_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    BitmapArrayConversions.updatebitmap_unsafe(magnitudes, magnitudes_bmp);
                    magnitudes_bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Bmp);
                }

#if SHOW_TIMINGS
                stopwatch timer_edge_joining = new stopwatch();
                timer_edge_joining.Start();
#endif

                // connect edges which are a short distance apart
                edge_detector.ConnectBrokenEdges(connect_edges_radius);
                
#if SHOW_TIMINGS
                timer_edge_joining.Stop();
                if (timer_edge_joining.time_elapsed_mS > 20)
                    Console.WriteLine("DetectSquaresMono: edge joining  " + timer_edge_joining.time_elapsed_mS.ToString() );
#endif
                
                // remove edges which are outside a circular region of interest
                if (circular_ROI_radius > 0)
                {
                    int circular_ROI_radius_sqr = circular_ROI_radius * circular_ROI_radius;                    
                    int half_width = img_width / 2;
                    int half_height = img_height / 2;
                    for (int i = edge_detector.edges.Count - 2; i >= 0; i -= 2)
                    {
                        int x = edge_detector.edges[i];
                        int y = edge_detector.edges[i+1];
                        int dx = x - half_width;
                        int dy = y - half_height;
                        int dist_sqr = (dx*dx)+(dy*dy);
                        if (dist_sqr > circular_ROI_radius_sqr)
                        {
                            edge_detector.edges.RemoveAt(i+1);
                            edge_detector.edges.RemoveAt(i);
                        }
                    }
                }
                edges = edge_detector.edges;

                // try different groupings
                for (int group_radius_index = 0; group_radius_index < grouping_radius_percent.Length; group_radius_index++)
                {               
#if SHOW_TIMINGS
                    stopwatch timer_grouping = new stopwatch();
                    timer_grouping.Start();
                    stopwatch timer_perim2 = new stopwatch();
                    timer_perim2.Start();
#endif
                    // group edges together into objects
                    List<List<int>> groups = 
                        GetGroups(edge_detector.edges, 
                                  img_width, img_height, image_border, 
                                  minimum_size_percent,
                                  false, max_search_depth, ignore_periphery, 
                                  grouping_radius_percent[group_radius_index]);
                    
#if SHOW_TIMINGS
                    timer_grouping.Stop();
                    if (timer_grouping.time_elapsed_mS > 20)
                        Console.WriteLine("DetectSquaresMono: edge grouping  " + timer_grouping.time_elapsed_mS.ToString() );
#endif
                    
                    if (groups != null)
                    {
#if SHOW_TIMINGS
                        stopwatch timer_aspect = new stopwatch();
                        timer_aspect.Start();
#endif

                        //List<List<int>> squares = groups;
                        
                        // get the set of edges with aspect ratio closest to square

                        List<List<int>> squares = GetValidGroups(groups,
                                                                 img_width, img_height,
                                                                 //minimum_aspect_ratio, maximum_aspect_ratio,
                                                                 minimum_size_percent);

#if SHOW_TIMINGS
                        timer_grouping.Stop();
                        if (timer_aspect.time_elapsed_mS > 20)
                            Console.WriteLine("DetectSquaresMono: aspect checking  " + timer_aspect.time_elapsed_mS.ToString() );
#endif
                        if (squares != null)                 
                        {
                            
                            for (int i = 0; i < squares.Count; i++)
                            {
#if SHOW_TIMINGS
                                stopwatch timer_periphery = new stopwatch();
                                timer_periphery.Start();
#endif

                                List<int> square = squares[i];
                                polygon2D perim = null;
                                List<List<int>> periphery = GetSquarePeriphery(square, erosion_dilation[erosion_dilation_level], ref perim);
                                
#if SHOW_TIMINGS
                                timer_periphery.Stop();
                                //if (timer_periphery.time_elapsed_mS > 20)
                                    Console.WriteLine("DetectSquaresMono: periphery detect  " + timer_periphery.time_elapsed_mS.ToString() );
#endif
                                
                                if (perim != null)
                                {
                                    float longest_side = perim.getLongestSide();
                                    float shortest_side = perim.getShortestSide();
                                    
                                    // not too small
                                    if (shortest_side * 100 / img_width > minimum_volume_percent)
                                    {
                                        float aspect = shortest_side / longest_side;
                                        
                                        if ((aspect > minimum_aspect_ratio) &&
                                            (aspect < maximum_aspect_ratio))
                                        {
                                            // check the angles
                                            bool angle_out_of_range = false;
                                            int vertex = 0;
                                            while ((vertex < perim.x_points.Count) &&
                                                   (!angle_out_of_range) && (vertex < 4))
                                            {
                                                float angle = perim.GetInteriorAngle(vertex);
                                                angle = angle / (float)Math.PI * 180;
                                                if ((angle < 70) || (angle > 110)) angle_out_of_range = true;
                                                vertex++;
                                            }
                                            
                                            if (!angle_out_of_range)
                                            {
                                                float aspect1 = perim.getSideLength(0) / perim.getSideLength(2);
                                                if ((aspect1 > 0.9f) && (aspect1 < 1.1f))
                                                {                                        
                                                    float aspect2 = perim.getSideLength(1) / perim.getSideLength(3);
                                                    if ((aspect2 > 0.9f) && (aspect2 < 1.1f))
                                                        square_shapes.Add(perim);
                                                }
                                                
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
#if SHOW_TIMINGS
                    timer_perim2.Stop();
                    if (timer_perim2.time_elapsed_mS > 20)
                        Console.WriteLine("DetectSquaresMono: grouping and periphery " + timer_perim2.time_elapsed_mS.ToString());
#endif
                }
            }

            // remove any overlapping squares
            square_shapes = SelectSquares(square_shapes, 0);
            
#if SHOW_TIMINGS
            timer_square_detection.Stop();
            if (timer_square_detection.time_elapsed_mS > 20)
                Console.WriteLine("DetectSquaresMono: " + timer_square_detection.time_elapsed_mS.ToString() );
#endif
            return(square_shapes);
        }
        
        #endregion
        
        #region "heuristics for detecting overlapping square regions and arbitrating between them"
        
        /// <summary>
        /// returns a value indicating how square the given region is
        /// </summary>
        /// <param name="square">
        /// region to be processed <see cref="polygon2D"/>
        /// </param>
        /// <returns>
        /// squareness value <see cref="System.Single"/>
        /// </returns>
        private static float Squareness(polygon2D square)
        {
            float measure1 = square.getSideLength(0) / square.getSideLength(1);
            measure1 = Math.Abs(1.0f - measure1);
            
            float measure2 = square.getSideLength(0) / square.getSideLength(2);
            measure2 = Math.Abs(1.0f - measure2);

            float measure3 = square.getSideLength(1) / square.getSideLength(3);
            measure3 = Math.Abs(1.0f - measure3);
            
            float result = 1.0f / (1.0f + (measure1 + measure2 + measure3));
            return(result);
        }
        
        private static List<polygon2D> SelectSquares(List<polygon2D> square_shapes,
                                                     int removal_strategy)
        {
            List<polygon2D> result = new List<polygon2D>();
            
            // maximum squareness
            if (removal_strategy == 0)
            {
                List<polygon2D> victims = new List<polygon2D>();

                for (int i = 0; i < square_shapes.Count; i++)
                {
                    polygon2D square1 = square_shapes[i];
                    
                    if (!victims.Contains(square1))
                    {
                        float squareness1 = Squareness(square1);
                        square1 = square1.Scale(1.1f);
                        
                        for (int j = i + 1; j < square_shapes.Count; j++)
                        {
                            polygon2D square2 = square_shapes[j];
                            if (!victims.Contains(square2))
                            {
                                if (square2.overlaps(square1))
                                {
                                    float squareness2 = Squareness(square2);
                                    if (squareness2 < squareness1)
                                    {
                                        victims.Add(square_shapes[j]);
                                    }
                                    else
                                    {
                                        victims.Add(square_shapes[i]);
                                        j = square_shapes.Count;
                                    }
                                }
                            }
                        }
                    }
                }
                for (int i = victims.Count-1; i >= 0; i--)
                    square_shapes.Remove(victims[i]);
                result = square_shapes;
            }
            
            return(result);
        }
        
        #endregion
        
        #region "display"
        
        /// <summary>
        /// process the given image file
        /// </summary>
        /// <param name="filename">
        /// filename of the image to process <see cref="System.String"/>
        /// </param>
        /// <param name="edges_filename">
        /// filename to save edges image <see cref="System.String"/>
        /// </param>
    	public static void ShowSquares(string filename, 
                                       string square_filename,
                                       bool ignore_periphery,
                                       int image_border_percent,
                                       int grouping_radius_percent,
                                       int minimum_size_percent,
                                       bool show_centres,
                                       int erosion_dilation,
                                       bool black_on_white,
                                       int minimum_volume_percent) 
        {
            const int max_search_depth = 8000;
            const int connect_edges_radius = 5;
            
            if (File.Exists(filename))
            {
                Bitmap bmp = (Bitmap)Bitmap.FromFile(filename);
                byte[] img = new byte[bmp.Width * bmp.Height * 3];
                BitmapArrayConversions.updatebitmap(bmp, img);
                
                if (!black_on_white)
                {
                    for (int i = 0; i < img.Length; i++)
                    {
                        img[i] = (byte)(255 - img[i]);
                    }
                }
                                
                int image_border = bmp.Width * image_border_percent / 100;
                
                // convert to mono
                byte[] img_mono = image.monoImage(img, bmp.Width, bmp.Height);
                
                if (erosion_dilation > 0)
                {
                    byte[] eroded = image.Erode(img_mono, bmp.Width, bmp.Height, erosion_dilation, null);
                    img_mono = eroded;
                }
                
                if (erosion_dilation < 0)
                {
                    byte[] dilated = image.Dilate(img_mono, bmp.Width, bmp.Height, -erosion_dilation, null);
                    img_mono = dilated;
                }
                
                CannyEdgeDetector edge_detector = new CannyEdgeDetector();
                edge_detector.automatic_thresholds = true;
                edge_detector.Update(img_mono, bmp.Width, bmp.Height);
                edge_detector.ConnectBrokenEdges(connect_edges_radius);
                
                List<List<int>> groups = 
                    GetGroups(edge_detector.edges, 
                              bmp.Width, bmp.Height, image_border, 
                              minimum_size_percent,
                              false, max_search_depth, ignore_periphery, 
                              grouping_radius_percent);

                if (groups != null)
                {
                    // get the set of edges with aspect ratio closest to square
                    List<List<int>> squares = GetAspectRange(groups,
                                                            bmp.Width, bmp.Height,
                                                            0.7f, 1.3f,
                                                            minimum_size_percent, true);
                    
                    //Console.WriteLine("squares: " + squares.Count.ToString());
                    
                    if (squares != null)                    
                    {
                        byte[] squareImage = null;
                        squareImage = ShowEdges(ref squareImage, squares, bmp.Width, bmp.Height);
                        
                        Bitmap square_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        BitmapArrayConversions.updatebitmap_unsafe(squareImage, square_bmp);
                        square_bmp.Save(square_filename, System.Drawing.Imaging.ImageFormat.Bmp);
                    }
                }
            }
        }

    	public static void ShowRectangles(string filename, 
                                          string square_filename,
                                          bool ignore_periphery,
                                          int image_border_percent,
                                          int grouping_radius_percent,
                                          int minimum_size_percent,
                                          bool show_centres,
                                          int erosion_dilation,
                                          bool black_on_white,
                                          int minimum_volume_percent) 
        {
            const int max_search_depth = 8000;
            const int connect_edges_radius = 5;
            
            if (File.Exists(filename))
            {
                Bitmap bmp = (Bitmap)Bitmap.FromFile(filename);
                byte[] img = new byte[bmp.Width * bmp.Height * 3];
                BitmapArrayConversions.updatebitmap(bmp, img);
                
                if (!black_on_white)
                {
                    for (int i = 0; i < img.Length; i++)
                    {
                        img[i] = (byte)(255 - img[i]);
                    }
                }
                                
                int image_border = bmp.Width * image_border_percent / 100;
                
                // convert to mono
                byte[] img_mono = image.monoImage(img, bmp.Width, bmp.Height);
                
                if (erosion_dilation > 0)
                {
                    byte[] eroded = image.Erode(img_mono, bmp.Width, bmp.Height, erosion_dilation, null);
                    img_mono = eroded;
                }
                
                if (erosion_dilation < 0)
                {
                    byte[] dilated = image.Dilate(img_mono, bmp.Width, bmp.Height, -erosion_dilation, null);
                    img_mono = dilated;
                }
                
                CannyEdgeDetector edge_detector = new CannyEdgeDetector();
                edge_detector.automatic_thresholds = true;
                edge_detector.Update(img_mono, bmp.Width, bmp.Height);
                edge_detector.ConnectBrokenEdges(connect_edges_radius);
                
                List<List<int>> groups = 
                    GetGroups(edge_detector.edges, 
                              bmp.Width, bmp.Height, image_border, 
                              minimum_size_percent,
                              false, max_search_depth, ignore_periphery, 
                              grouping_radius_percent);

                if (groups != null)
                {
                    // get the set of edges with aspect ratio closest to square
                    List<List<int>> squares = GetAspectRange(groups,
                                                            bmp.Width, bmp.Height,
                                                            0.3f, 20.0f,
                                                            minimum_size_percent, false);
                    
                    //Console.WriteLine("squares: " + squares.Count.ToString());
                    
                    if (squares != null)                    
                    {
                        byte[] squareImage = null;
                        squareImage = ShowEdges(ref squareImage, squares, bmp.Width, bmp.Height);
                        
                        Bitmap square_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        BitmapArrayConversions.updatebitmap_unsafe(squareImage, square_bmp);
                        square_bmp.Save(square_filename, System.Drawing.Imaging.ImageFormat.Bmp);
                    }
                }
            }
        }
        
        
    	public static void ShowSquarePerimeters(string filename,                                                 
                                                int[] grouping_radius_percent,
                                                int[] erosion_dilation,
                                                bool black_on_white,
                                                int accuracy_level,
                                                string square_filename)
        {
            if (File.Exists(filename))
            {
                Bitmap bmp = (Bitmap)Bitmap.FromFile(filename);
                byte[] img = new byte[bmp.Width * bmp.Height * 3];
                BitmapArrayConversions.updatebitmap(bmp, img);
                                
                List<int> edges = null;
                List<polygon2D> squares = DetectSquares(img, bmp.Width, bmp.Height,
                                                        grouping_radius_percent,
                                                        erosion_dilation,
                                                        black_on_white,
                                                        accuracy_level, false, 0,
                                                        ref edges);
                
                for (int i = 0; i < squares.Count; i++)
                {
                    squares[i].show(img, bmp.Width, bmp.Height,
                                    255, 0, 0,
                                    0);
                }                
                
                Bitmap bmp2 = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                BitmapArrayConversions.updatebitmap_unsafe(img, bmp2);
                bmp2.Save(square_filename, System.Drawing.Imaging.ImageFormat.Bmp);
            }
            
        }
        

    	public static void ShowPerimeters(string filename, 
                                          string edges_filename, 
                                          string magnitudes_filename,
                                          string perimeters_filename,
                                          bool ignore_periphery,
                                          int grouping_radius_percent,
                                          int minimum_size_percent,
                                          bool show_centres,
                                          int erosion_dilation,
                                          bool black_on_white) 
        {
            const int max_search_depth = 8000;
            const int connect_edges_radius = 5;
            
            if (File.Exists(filename))
            {
                Bitmap bmp = (Bitmap)Bitmap.FromFile(filename);
                byte[] img = new byte[bmp.Width * bmp.Height * 3];
                BitmapArrayConversions.updatebitmap(bmp, img);
                
                if (!black_on_white)
                {
                    for (int i = 0; i < img.Length; i++)
                    {
                        img[i] = (byte)(255 - img[i]);
                    }
                }
                
                // convert to mono
                byte[] img_mono = image.monoImage(img, bmp.Width, bmp.Height);
                
                if (erosion_dilation > 0)
                {
                    byte[] eroded = image.Erode(img_mono, bmp.Width, bmp.Height, erosion_dilation, null);
                    img_mono = eroded;
                }
                
                if (erosion_dilation < 0)
                {
                    byte[] dilated = image.Dilate(img_mono, bmp.Width, bmp.Height, -erosion_dilation, null);
                    img_mono = dilated;
                }

                CannyEdgeDetector edge_detector = new CannyEdgeDetector();
                edge_detector.automatic_thresholds = true;
                edge_detector.Update(img_mono, bmp.Width, bmp.Height);
                edge_detector.ConnectBrokenEdges(connect_edges_radius);

                byte[] magnitudes = new byte[bmp.Width * bmp.Height * 3];
                edge_detector.ShowMagnitudes(magnitudes, bmp.Width, bmp.Height);
                Bitmap magnitudes_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                BitmapArrayConversions.updatebitmap_unsafe(magnitudes, magnitudes_bmp);
                magnitudes_bmp.Save(magnitudes_filename, System.Drawing.Imaging.ImageFormat.Bmp);
                
                
                /*
                byte[] perimetersImage = 
                    ShowLongestPerimeters(edge_detector.edges, 
                                          bmp.Width, bmp.Height, 
                                          0, minimum_size_percent, false,
                                          max_search_depth,
                                          ignore_periphery,
                                          show_centres);
                */
                
                List<List<int>> groups = null;
                byte[] perimetersImage = ShowGroups(edge_detector.edges,
                                                    bmp.Width, bmp.Height,
                                                    0, minimum_size_percent,
                                                    false,
                                                    max_search_depth,
                                                    ignore_periphery,
                                                    grouping_radius_percent,
                                                    ref groups);
                    
                Bitmap edges_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                BitmapArrayConversions.updatebitmap_unsafe(edge_detector.getEdgesImage(), edges_bmp);
                edges_bmp.Save(edges_filename, System.Drawing.Imaging.ImageFormat.Bmp);
                
                Bitmap perimeters_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                BitmapArrayConversions.updatebitmap_unsafe(perimetersImage, perimeters_bmp);
                perimeters_bmp.Save(perimeters_filename, System.Drawing.Imaging.ImageFormat.Bmp);
            }
        }

        
    	public static void ShowPeriphery(string filename, 
                                         string periphery_filename, 
                                         int grouping_radius_percent,
                                         int erosion_dilation,
                                         int minimum_size_percent,
                                         bool black_on_white,
                                         bool squares_only) 
        {
            const int max_search_depth = 8000;
            const int connect_edges_radius = 5;
            
            if (File.Exists(filename))
            {
                Bitmap bmp = (Bitmap)Bitmap.FromFile(filename);
                byte[] img = new byte[bmp.Width * bmp.Height * 3];
                byte[] squares_img = null; //new byte[bmp.Width * bmp.Height * 3];
                BitmapArrayConversions.updatebitmap(bmp, img);
                                
                // convert to mono
                byte[] img_mono = image.monoImage(img, bmp.Width, bmp.Height);
                
                if (!black_on_white)
                {
                    for (int i = 0; i < img_mono.Length; i++)
                        img_mono[i] = (byte)(255 - img_mono[i]);
                }                
                
                if (erosion_dilation > 0)
                {
                    byte[] eroded = image.Erode(img_mono, bmp.Width, bmp.Height, erosion_dilation, null);
                    img_mono = eroded;
                }
                
                if (erosion_dilation < 0)
                {
                    byte[] dilated = image.Dilate(img_mono, bmp.Width, bmp.Height, -erosion_dilation, null);
                    img_mono = dilated;
                }

                CannyEdgeDetector edge_detector = new CannyEdgeDetector();
                edge_detector.automatic_thresholds = true;
                edge_detector.Update(img_mono, bmp.Width, bmp.Height);
                edge_detector.ConnectBrokenEdges(connect_edges_radius);

                // group edges together into objects
                List<List<int>> groups = 
                        GetGroups(edge_detector.edges, 
                                  bmp.Width, bmp.Height, 0, 
                                  minimum_size_percent,
                                  false, max_search_depth, false, 
                                  grouping_radius_percent);

                if (groups != null)
                {
                    float minimum_aspect_ratio = 0.7f;
                    float maximum_aspect_ratio = 1.3f;
                    if (!squares_only)
                    {
                        minimum_aspect_ratio = 0.3f;
                        maximum_aspect_ratio = 20.0f;
                    }
                    
                    // get the set of edges with aspect ratio closest to square
                    List<List<int>> squares = GetAspectRange(groups,
                                                             bmp.Width, bmp.Height,
                                                             minimum_aspect_ratio, maximum_aspect_ratio,
                                                             minimum_size_percent, squares_only);
                    if (squares != null)                 
                    {
                        for (int i = 0; i < squares.Count; i++)
                        {
                            List<int> square = squares[i];
                            polygon2D perim = null;
                            List<List<int>> periphery = GetSquarePeriphery(square, erosion_dilation, ref perim);
                            
                            if (perim != null)
                            {
                                squares_img = ShowEdges(ref squares_img, periphery, bmp.Width, bmp.Height);
                                if (squares_img != null)
                                    perim.show(squares_img, bmp.Width, bmp.Height, 150, 150, 150, 0);
                            }
                        }
                    }
                }
                    
                Bitmap periphery_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                BitmapArrayConversions.updatebitmap_unsafe(squares_img, periphery_bmp);
                periphery_bmp.Save(periphery_filename, System.Drawing.Imaging.ImageFormat.Bmp);                
            }
        }
        
        
        #endregion

        #region "detecting sets of edges within a given aspect ratio"

        public static List<List<int>> GetValidGroups(List<List<int>> groups,
                                                     int img_width, int img_height,
                                                     int minimum_size_percent)
        {
            List<List<int>> results = null;            
            
            // minimum size in pixels
            int minimum_size = minimum_size_percent * img_width / 100;
                        
            for (int i = 0; i < groups.Count; i++)
            {
                int tx = img_width-1;
                int ty = img_height-1;
                int bx = 0;
                int by = 0;
                List<int> edges = groups[i];
                for (int j = 0; j < edges.Count; j += 2)
                {
                    int x = edges[j];
                    int y = edges[j + 1];
                    if (x < tx) tx = x;
                    if (y < ty) ty = y;
                    if (x > bx) bx = x;
                    if (y > by) by = y;
                }
                
                int dx = bx - tx;
                int dy = by - ty;
                
                if ((dx > minimum_size) && 
                    (dy > minimum_size))
                {
                    if (results == null)
                        results = new List<List<int>>();
                        
                    results.Add(edges);
                }

            }
            return(results);
        }        
        
        
        public static List<List<int>> GetAspectRange(List<List<int>> groups,
                                                     int img_width, int img_height,
                                                     float minimum_aspect, float maximum_aspect,
                                                     int minimum_size_percent,
                                                     bool squares_only)
        {
            List<List<int>> results = null;            
            
            // minimum size in pixels
            int minimum_size = minimum_size_percent * img_width / 100;
            
            float ideal_aspect = minimum_aspect + ((maximum_aspect - minimum_aspect)/2.0f);
            float minimum_diff_from_ideal = 9999;
            //float maximum_volume = 0;
            
            for (int i = 0; i < groups.Count; i++)
            {
                int tx = img_width-1;
                int ty = img_height-1;
                int bx = 0;
                int by = 0;
                List<int> edges = groups[i];
                for (int j = 0; j < edges.Count; j += 2)
                {
                    int x = edges[j];
                    int y = edges[j + 1];
                    if (x < tx) tx = x;
                    if (y < ty) ty = y;
                    if (x > bx) bx = x;
                    if (y > by) by = y;
                }
                
                int dx = bx - tx;
                int dy = by - ty;
                
                if ((dx > minimum_size) && 
                    (dy > minimum_size))
                {
                    float aspect_ratio = 0;
                    
                    if (dy > dx)
                        aspect_ratio = dx / (float)dy;
                    else
                        aspect_ratio = dy / (float)dx;
                    
                    if ((aspect_ratio > minimum_aspect) &&
                        (aspect_ratio < maximum_aspect))
                    {
                        bool is_valid = true;
                        if ((!squares_only) && (aspect_ratio >0.8f) && (aspect_ratio < 1.2f))
                            is_valid = false;
                            
                        if (is_valid)
                        {
                            if (results == null)
                                results = new List<List<int>>();
                        
                            results.Add(edges);
                        }
                    }
                }

            }
            return(results);
        }

        private static void SampleDotColour(byte[] img_colour,
                                            int img_width, int img_height,
                                            int x, int y,
                                            int[] colour)
        {
            int radius = 2;
            
            for (int xx = x - radius; xx <= x + radius; xx++)
            {
                if ((xx > -1) && (xx < img_width))
                {
                    for (int yy = y - radius; yy <= y + radius; yy++)
                    {
                        if ((yy > -1) && (yy < img_height))
                        {
                            int n = ((yy * img_width) + xx)*3;
                            for (int col = 0; col < 3; col++)
                            {
                                colour[col] += img_colour[n + 2 - col];
                            }
                        }
                    }
                }
            }
        }

        public static List<calibrationDot> GetDots(List<List<int>> groups,
                                                   byte[] img_colour,
                                                   int img_width, int img_height,
                                                   float minimum_aspect, float maximum_aspect,
                                                   int minimum_width, int maximum_width)
        {
            List<calibrationDot> dots = new List<calibrationDot>();
            
            float ideal_aspect = minimum_aspect + ((maximum_aspect - minimum_aspect)/2.0f);
            float minimum_diff_from_ideal = 9999;
            
            float max_redness = 0;
            int winner = -1;
            
            for (int i = 0; i < groups.Count; i++)
            {
                int tx = img_width-1;
                int ty = img_height-1;
                int bx = 0;
                int by = 0;
                float cx = 0;
                float cy = 0;
                List<int> edges = groups[i];
                for (int j = 0; j < edges.Count; j += 2)
                {
                    int x = edges[j];
                    int y = edges[j + 1];
                    cx += x;
                    cy += y;
                    if (x < tx) tx = x;
                    if (y < ty) ty = y;
                    if (x > bx) bx = x;
                    if (y > by) by = y;
                }
                
                int dx = bx - tx;
                int dy = by - ty;                
                
                if ((dx > minimum_width) &&
                    (dx < maximum_width) &&
                    (dy > minimum_width) &&
                    (dy < maximum_width))
                {
                    float aspect_ratio = 0;
                    
                    if (dy > dx)
                        aspect_ratio = dx / (float)dy;
                    else
                        aspect_ratio = dy / (float)dx;
                    
                    if ((aspect_ratio > minimum_aspect) &&
                        (aspect_ratio < maximum_aspect))
                    {
                        float radius = (dx + dy) / 4.0f;
                        cx /= (edges.Count/2);
                        cy /= (edges.Count/2);
                        
                        calibrationDot cdot = new calibrationDot();
                        cdot.x = cx;
                        cdot.y = cy;
                        cdot.radius = radius;
                        
                        // get the colour of the dot
                        int[] dot_colour = new int[3];
                        SampleDotColour(img_colour, img_width, img_height, (int)cx, (int)cy, dot_colour);
                        cdot.r = dot_colour[0];
                        cdot.g = dot_colour[1];
                        cdot.b = dot_colour[2];
                        
                        float redness = (cdot.r*2) - cdot.g - cdot.b;
                        if (redness > max_redness)
                        {
                            max_redness = redness;
                            winner = dots.Count;
                        }
                        
                        dots.Add(cdot);
                    }
                }

            }
            
            if (winner > -1) dots[winner].centre = true;
            return(dots);
        }

        
        #endregion
        
        #region "detecting related line segments"
        
        public static List<List<int>> GetGroups(List<int>edges,
                                                int img_width, int img_height,
                                                int image_border,
                                                int minimum_size_percent,
                                                bool squares_only,
                                                int max_search_depth,
                                                bool ignore_periphery,
                                                int grouping_radius_percent)
        {
#if SHOW_TIMINGS
                stopwatch timer_grouping = new stopwatch();
                timer_grouping.Start();
#endif
            const int compression = 4;
            
            int grouping_radius = grouping_radius_percent * img_width / (100 * compression);
            List<List<int>> groups = new List<List<int>>();            
                        
            // find line segments of significant length
            List<float> centres = null;
            List<float> bounding_boxes = null;
            List<List<int>> line_segments = 
                DetectLongestPerimeters(edges, 
                                        img_width, img_height, 
                                        image_border, 
                                        minimum_size_percent, 
                                        squares_only,
                                        max_search_depth,
                                        ignore_periphery,
                                        ref centres,
                                        ref bounding_boxes);

            bool[][] grouping_matrix = new bool[line_segments.Count][];
            for (int i = 0; i < line_segments.Count; i++)
                grouping_matrix[i] = new bool[line_segments.Count];
                        
            // map out the line segments
            const int step_size = 4;
            int ty = img_height-1;
            int by = 0;
            int tx = img_width-1;
            int bx = 0;
            int[,] line_segment_map = new int[(img_width/compression)+2, (img_height/compression)+2];
            for (int i = 0; i < line_segments.Count; i++)
            {
                List<int> line_segment = line_segments[i];
                for (int j = 0; j < line_segment.Count; j += step_size)
                {
                    int x = line_segment[j]/compression;
                    int y = line_segment[j+1]/compression;
                    if (line_segment_map[x, y] > 0)
                    {
                        if (line_segment_map[x, y] != i + 1)
                        {
                            int segment_index1 = line_segment_map[x, y]-1; 
                            int segment_index2 = i;
                            
                            // link the two segments
                            grouping_matrix[segment_index1][segment_index2] = true;
                            grouping_matrix[segment_index2][segment_index1] = true;
                        }
                    }
                    line_segment_map[x, y] = i + 1;
                    if (x < tx) tx = x;
                    if (x > bx) bx = x;
                    if (y < ty) ty = y;
                    if (y > by) by = y;
                }
            }
            
            // horizontal grouping
            for (int y = ty; y <= by; y++)
            {
                int prev_segment_index = -1;
                int prev_segment_x = -1;
                for (int x = tx; x <= bx; x++)
                {
                    int segment_index = line_segment_map[x, y];
                    if (segment_index > 0)
                    {
                        if (prev_segment_x > -1)
                        {
                            int dx = x - prev_segment_x;
                            if (dx < grouping_radius)
                            {
                                if (!grouping_matrix[segment_index-1][prev_segment_index])
                                {
                                    // get the line segment indexes
                                    segment_index--;                                    
                                    
                                    if (segment_index != prev_segment_index)
                                    {                                
                                        // link the two segments
                                        grouping_matrix[segment_index][prev_segment_index] = true;
                                        grouping_matrix[prev_segment_index][segment_index] = true;
                                    }
                                }
                            }
                        }
                        prev_segment_x = x;
                        prev_segment_index = line_segment_map[x, y]-1;
                    }
                }
            }

            // horizontal grouping
            for (int x = tx; x <= bx; x++)
            {
                int prev_segment_y = -1;
                int prev_segment_index = -1;
                for (int y = ty; y <= by; y++)
                {
                    int segment_index = line_segment_map[x, y];
                    if (segment_index > 0)
                    {
                        if (prev_segment_y > -1)
                        {
                            int dy = y - prev_segment_y;
                            if (dy < grouping_radius)
                            {
                                if (!grouping_matrix[segment_index-1][prev_segment_index])
                                {
                                    // get the line segment indexes
                                    segment_index--;
                                    
                                    if (segment_index != prev_segment_index)
                                    {                                
                                        // link the two segments
                                        grouping_matrix[segment_index][prev_segment_index] = true;
                                        grouping_matrix[prev_segment_index][segment_index] = true;
                                    }
                                }
                            }                            
                        }
                        prev_segment_y = y;
                        prev_segment_index = line_segment_map[x, y]-1;                        
                    }
                }
            }
                        
            // turn grouping matrix into a hypergraph
            hypergraph graph = new hypergraph(grouping_matrix.Length, 1);
            for (int i = 0; i < grouping_matrix.Length; i++)
            {
                for (int j = 0; j < grouping_matrix[i].Length; j++)
                {
                    if (grouping_matrix[i][j])
                        graph.LinkByIndex(j, i);
                }
            }
            
            // detect connected sets within the hypergraph
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                bool already_grouped = graph.GetFlagByIndex(i, 0);
                if (!already_grouped)
                {
                    List<hypergraph_node> connected_set = new List<hypergraph_node>();
                    graph.PropogateFlagFromIndex(i, 0, connected_set);
                    
                    List<hypergraph_node> members = new List<hypergraph_node>();
                    for (int j = 0; j < connected_set.Count; j++)
                    {
                        if (!members.Contains(connected_set[j])) members.Add(connected_set[j]);
                    }

                    List<int> group_members = new List<int>();
                    for (int j = 0; j < members.Count; j++)
                    {
                        int line_segment_index = members[j].ID;
                        List<int> perim = line_segments[line_segment_index];
                        for (int k = 0; k < perim.Count; k++)
                            group_members.Add(perim[k]);
                    }
                    groups.Add(group_members);
                    
                }
            }
            
#if SHOW_TIMINGS
            timer_grouping.Stop();
            //if (timer_grouping.time_elapsed_mS > 20)
                Console.WriteLine("GetGroups: " + timer_grouping.time_elapsed_mS.ToString() );
#endif                
            
            return(groups);
        }
        
        
        #endregion

        #region "detect connected edges recursively"
        
        /// <summary>
        /// recursively trace along an edge
        /// </summary>
        /// <param name="edges_img">edges image</param>
        /// <param name="x">current x coordinate</param>
        /// <param name="y">current y coordinate</param>
        /// <param name="length">length of the perimeter</param>
        /// <param name="members">list of points belonging to the perimeter</param>
        /// <param name="depth"></param>
        /// <param name="image_border">border around the image in pixels</param>
        /// <param name="isValid">whether this traced set of edges is valid</param>
        /// <param name="perimeter_tx">top left x coordinate of the bounding box</param>
        /// <param name="perimeter_ty">top left y coordinate of the bounding box</param>
        /// <param name="perimeter_bx">bottom right x coordinate of the bounding box</param>
        /// <param name="perimeter_by">bottom right y coordinate of the bounding box</param>
        /// <param name="max_search_depth">maximum depth of recursion</param>
        /// <param name="ignore_periphery">whether to ignore any edges which trace into teh border region</param>
        private static void TraceEdge(bool[][] edges_img,
                                      int x, int y,
                                      ref int length,
                                      List<int> members,
                                      int depth,
                                      int image_border,
                                      ref bool isValid,
                                      ref int perimeter_tx, ref int perimeter_ty,
                                      ref int perimeter_bx, ref int perimeter_by,
                                      int max_search_depth,
                                      bool ignore_periphery,
                                      ref int centre_x, ref int centre_y)
        {
            int img_width = edges_img.Length;
            int img_height = edges_img[0].Length;
            
            centre_x += x;
            centre_y += y;
            
            if (x < perimeter_tx) perimeter_tx = x;
            if (y < perimeter_ty) perimeter_ty = y;
            if (x > perimeter_bx) perimeter_bx = x;
            if (y > perimeter_by) perimeter_by = y;
            
            if (depth < max_search_depth)
            {
                // ensure that this doesn't touch the borders of the image
                if (ignore_periphery)
                {
                    if ((x < image_border) || (x > img_width - 1 - image_border) ||
                        (y < image_border) || (y > img_height - 1 - image_border))
                        isValid = false;
                }

                // add this point to the members
                bool added = false;
                if (members.Count < 16000)
                {
                    try
                    {
                        members.Add(x);
                        members.Add(y);
                        added = true;
                    }
                    catch
                    {
                    }
                }

                if (added)
                {
                    // remove the edge (we don't want to count it more than once)
                    edges_img[x][y] = false;

                    // increment the length of the perimeter
                    length++;

                    // find the next edge within the neighbourhood
                    if (x < img_width - 1)
                    {
                        if (edges_img[x + 1][y]) TraceEdge(edges_img, x + 1, y, ref length, members, depth + 1, image_border, ref isValid, 
                                                           ref perimeter_tx, ref perimeter_ty, ref perimeter_bx, ref perimeter_by,
                                                           max_search_depth, ignore_periphery,
                                                           ref centre_x, ref centre_y);

                        if (y > 0)
                        {
                            if (edges_img[x + 1][y - 1]) TraceEdge(edges_img, x + 1, y - 1, ref length, members, depth + 1, image_border, ref isValid,
                                                                   ref perimeter_tx, ref perimeter_ty, ref perimeter_bx, ref perimeter_by,
                                                                   max_search_depth, ignore_periphery,
                                                                   ref centre_x, ref centre_y);
                        }

                        if (y < img_height - 1)
                        {
                            if (edges_img[x + 1][y + 1]) TraceEdge(edges_img, x + 1, y + 1, ref length, members, depth + 1, image_border, ref isValid,
                                                                   ref perimeter_tx, ref perimeter_ty, ref perimeter_bx, ref perimeter_by,
                                                                   max_search_depth, ignore_periphery,
                                                                   ref centre_x, ref centre_y);
                        }
                    }

                    if (x > 0)
                    {
                        if (edges_img[x - 1][y]) TraceEdge(edges_img, x - 1, y, ref length, members, depth + 1, image_border, ref isValid,
                                                           ref perimeter_tx, ref perimeter_ty, ref perimeter_bx, ref perimeter_by,
                                                           max_search_depth, ignore_periphery,
                                                           ref centre_x, ref centre_y);

                        if (y > 0)
                        {
                            if (edges_img[x - 1][y - 1]) TraceEdge(edges_img, x - 1, y - 1, ref length, members, depth + 1, image_border, ref isValid,
                                                                   ref perimeter_tx, ref perimeter_ty, ref perimeter_bx, ref perimeter_by,
                                                                   max_search_depth, ignore_periphery,
                                                                   ref centre_x, ref centre_y);
                        }

                        if (y < img_height - 1)
                        {
                            if (edges_img[x - 1][y + 1]) TraceEdge(edges_img, x - 1, y + 1, ref length, members, depth + 1, image_border, ref isValid,
                                                                   ref perimeter_tx, ref perimeter_ty, ref perimeter_bx, ref perimeter_by,
                                                                   max_search_depth, ignore_periphery,
                                                                   ref centre_x, ref centre_y);
                        }
                    }

                    if (y > 0)
                    {
                        if (edges_img[x][y - 1]) TraceEdge(edges_img, x, y - 1, ref length, members, depth + 1, image_border, ref isValid,
                                                           ref perimeter_tx, ref perimeter_ty, ref perimeter_bx, ref perimeter_by,
                                                           max_search_depth, ignore_periphery,
                                                           ref centre_x, ref centre_y);
                    }

                    if (y < img_height - 1)
                    {
                        if (edges_img[x][y + 1]) TraceEdge(edges_img, x, y + 1, ref length, members, depth + 1, image_border, ref isValid,
                                                           ref perimeter_tx, ref perimeter_ty, ref perimeter_bx, ref perimeter_by,
                                                           max_search_depth, ignore_periphery,
                                                           ref centre_x, ref centre_y);
                    }
                    
                }
            }
        }

        public static byte[] ShowLongestPerimeters(List<int>edges,
                                                   int img_width, int img_height,
                                                   int image_border,
                                                   int minimum_size_percent,
                                                   bool squares_only,
                                                   int max_search_depth,
                                                   bool ignore_periphery,
                                                   bool show_centres)
        {
            byte[] result = new byte[img_width * img_height * 3];
            for (int i = 0; i < result.Length; i++) result[i]=255;
            
            List<float> centres = null;
            List<float> bounding_boxes = null;     
            
            List<List<int>> perimeters = 
                DetectLongestPerimeters(edges, 
                                        img_width, img_height, 
                                        image_border, 
                                        minimum_size_percent, 
                                        squares_only,
                                        max_search_depth,
                                        ignore_periphery,
                                        ref centres,
                                        ref bounding_boxes);

            for (int i = 0; i < perimeters.Count; i++)
            {
                List<int> perim = perimeters[i];
                for (int j = 0; j < perim.Count; j += 2)
                {
                    int x = perim[j];
                    int y = perim[j+1];
                    int n = ((y * img_width) + x) * 3;
                    for (int col = 0; col < 3; col++)
                        result[n+col] = 0;
                }
                
                if (show_centres)
                {
                    float x = centres[i*2];
                    float y = centres[(i*2)+1];
                    int n = (((int)y * img_width) + (int)x) * 3;
                    drawing.drawCross(result, img_width, img_height,
                                      (int)x, (int)y, 5,
                                      255,0,0,
                                      0);
                }
            }
            
            return(result);
        }

        
        
        public static byte[] ShowEdges(List<int> edges,
                                       int img_width, int img_height)
        {
            byte[] result = new byte[img_width * img_height * 3];
            for (int i = 0; i < result.Length; i++) result[i]=255;
            
            for (int i = 0; i < edges.Count; i += 2)
            {
                int x = edges[i];
                int y = edges[i+1];
                int n = ((y * img_width) + x) * 3;
                result[n] = 0;
                result[n+1] = 0;
                result[n+2] = 0;
            }
            return(result);
        }

        public static byte[] ShowEdges(List<float> edges,
                                       int img_width, int img_height)
        {
            byte[] result = new byte[img_width * img_height * 3];
            for (int i = 0; i < result.Length; i++) result[i]=255;
            
            for (int i = 0; i < edges.Count; i += 2)
            {
                float x = edges[i];
                float y = edges[i + 1];
                int n = (((int)y * img_width) + (int)x) * 3;
                if (n < result.Length - 3)
                {
                    result[n] = 0;
                    result[n + 1] = 0;
                    result[n + 2] = 0;
                }
            }
            return(result);
        }

        public static byte[] ShowRawImagePerimeter(byte[] img, int img_width, int img_height,
                                                   polygon2D perim,
                                                   int r, int g, int b,
                                                   int line_width)
        {
            byte[] new_img = (byte[])img.Clone();
            perim.show(new_img, img_width, img_height,
                       r, g, b, line_width);
            return(new_img);
        }
        
        public static byte[] ShowEdges(ref byte[] result,
                                       List<List<int>> groups,
                                       int img_width, int img_height)
        {
            if (result == null)
            {
                result = new byte[img_width * img_height * 3];
                for (int i = 0; i < result.Length; i++) result[i]=255;
            }

            Random rnd = new Random(0);
            
            for (int i = 0; i < groups.Count; i++)
            {
                byte r = (byte)rnd.Next(100);
                byte g = (byte)rnd.Next(100);
                byte b = (byte)rnd.Next(100);
                
                switch(i)
                {
                    case 0: 
                    {
                        r = 0;
                        g = 255;
                        b = 0;
                        break;
                    }
                    case 1: 
                    {
                        r = 255;
                        g = 0;
                        b = 0;
                        break;
                    }
                    case 2: 
                    {
                        r = 0;
                        g = 0;
                        b = 255;
                        break;
                    }
                    case 3: 
                    {
                        r = 255;
                        g = 0;
                        b = 255;
                        break;
                    }
                }
                
                List<int> edges = groups[i];
                for (int j = 0; j < edges.Count; j += 2)
                {
                    int x = edges[j];
                    int y = edges[j+1];
                    int n = ((y * img_width) + x) * 3;
                                        
                    result[n] = b;
                    result[n+1] = g;
                    result[n+2] = r;
                }
            }
            return(result);
        }
        
        public static byte[] ShowGroups(List<int> edges,
                                        int img_width, int img_height,
                                        int image_border,
                                        int minimum_size_percent,
                                        bool squares_only,
                                        int max_search_depth,
                                        bool ignore_periphery,
                                        int grouping_radius_percent,
                                        ref List<List<int>> groups)
        {
            byte[] result = new byte[img_width * img_height * 3];
            for (int i = 0; i < result.Length; i++) result[i]=255;
            
            groups = GetGroups(edges, img_width, img_height, image_border, minimum_size_percent,
                                squares_only, max_search_depth, ignore_periphery, grouping_radius_percent);

            Random rnd = new Random(0);
            for (int i = 0; i < groups.Count; i++)
            {
                List<int> perim = groups[i];
                byte r = (byte)(155 - rnd.Next(155));
                byte g = (byte)(155 - rnd.Next(155));
                byte b = (byte)(155 - rnd.Next(155));
                switch(i)
                {
                    case 0: { r = 255; g = 0; b = 0; break; }
                    case 1: { r = 0; g = 255; b = 0; break; }
                    case 2: { r = 0; g = 0; b = 255; break; }
                    case 3: { r = 255; g = 0; b = 255; break; }
                    case 4: { r = 0; g = 255; b = 255; break; }
                    case 5: { r = 255; g = 255; b = 0; break; }
                }
                for (int j = 0; j < perim.Count; j += 2)
                {
                    int x = perim[j];
                    int y = perim[j+1];
                    int n = ((y * img_width) + x) * 3;
                    result[n] = b;
                    result[n+1] = g;
                    result[n+2] = r;
                }                
            }
            
            return(result);
        }

                
        
        /// <summary>
        /// detect connected sets of edges which correspond to the perimeters of shapes
        /// </summary>
        /// <param name="edgemap">
        /// A <see cref="System.Boolean"/>
        /// </param>
        /// <param name="image_border">
        /// A <see cref="System.Int32"/>
        /// </param>
        /// <param name="minimum_size_percent">
        /// the minimum size of a perimeter relative to the largest found <see cref="System.Int32"/>
        /// </param>
        /// <param name="squares_only">
        /// only look for square regions <see cref="System.Int32"/>
        /// </param>
        /// <param name="max_search_depth">
        /// when tracing edges recursively this is the maximum search depth <see cref="System.Int32"/>
        /// </param>
        /// <param name="ignore_periphery">
        /// don't trace edges which are within the border <see cref="System.Int32"/>
        /// </param>
        /// <param name="centres">
        /// average pixel position <see cref="System.Int32"/>
        /// </param>
        /// <param name="bounding_boxes">
        /// bounding boxes for the perimeters <see cref="System.Int32"/>
        /// </param>
        /// <returns>
        /// list of perimeters <see cref="ArrayList"/>
        /// </returns>
        public static List<List<int>> DetectLongestPerimeters(List<int>edges,
                                                              int img_width, int img_height,
                                                              int image_border,
                                                              int minimum_size_percent,
                                                              bool squares_only,
                                                              int max_search_depth,
                                                              bool ignore_periphery,
                                                              ref List<float> centres,
                                                              ref List<float> bounding_boxes)
        {
            bool[][] edgemap = new bool[img_width][];
            for (int x = 0; x < img_width; x++)
                edgemap[x] = new bool[img_height];
            
            // update the map
            for (int i = edges.Count-2; i >= 0; i -= 2)
            {
                int x = edges[i];
                int y = edges[i+1];
                if ((x > -1) && (x < img_width) &&
                    (y > -1) && (y < img_height))
                    edgemap[x][y] = true;
                else
                {
                    edges.RemoveAt(i+1);
                    edges.RemoveAt(i);
                }
            }            
            
            // maximum perimeter length found
            int max_length = 0;

            // make a copy of the original edges image
            // because we'll remove edges as they're counted
            bool[][] edges_img = (bool[][])edgemap.Clone();

            // list of perimeters found
            List<List<int>> temp_perimeters = new List<List<int>>();

            // list of the lengths of the perimeters found
            List<int> temp_perimeter_lengths = new List<int>();

            // list of the centre points of the perimeters found
            List<float> temp_perimeter_centres = new List<float>();

            // list of the bounding boxes of the perimeters found
            List<float> temp_bounding_boxes = new List<float>();

            max_search_depth = 8000;

            // examine the edges image
            for (int i = 0; i < edges.Count; i += 2)
            {
                int x = edges[i];
                int y = edges[i+1];
                
                // is this an edge?
                if (edges_img[x][y])
                {
                    // an edge has been found - begin tracing
                    List<int> members = new List<int>();

                    // trace along the edge to form a perimeter
                    int length = 0;
                    bool isValid = true;                        

                    int perimeter_tx = img_width;
                    int perimeter_ty = img_height;
                    int perimeter_bx = 0;
                    int perimeter_by = 0;
                    int centre_x = 0;
                    int centre_y = 0;
                    TraceEdge(edges_img, x, y, ref length, members, 0, image_border, ref isValid,
                              ref perimeter_tx, ref perimeter_ty, ref perimeter_bx, ref perimeter_by,
                              max_search_depth, ignore_periphery,
                              ref centre_x, ref centre_y);

                    if (squares_only)
                    {
                        if (perimeter_by != perimeter_ty)
                        {
                            float aspect_ratio = (perimeter_bx - perimeter_tx) /
                                                 (float)(perimeter_by - perimeter_ty);
                            if ((aspect_ratio < 0.7f) ||
                                (aspect_ratio > 1.3f))
                                isValid = false;
                        }
                    }
                    
                    // if the perimeter above some small size
                    // (we're not interested in noise)
                    if (isValid)
                    {
                        if (length > 10)
                        {
                            // add this perimeter to the list
                            temp_perimeters.Add(members);
                            temp_perimeter_lengths.Add(length);
                            temp_perimeter_centres.Add(centre_x / (float)length);
                            temp_perimeter_centres.Add(centre_y / (float)length);
                            temp_bounding_boxes.Add(perimeter_tx);
                            temp_bounding_boxes.Add(perimeter_ty);
                            temp_bounding_boxes.Add(perimeter_bx);
                            temp_bounding_boxes.Add(perimeter_by);                            
                        }

                        // was this the longest perimeter ?
                        if (length > max_length)
                            max_length = length;
                    }
                }
            }

            // minimum length of the perimeter in pixels
            int minimum_length_pixels = minimum_size_percent * max_length / 100;

            // out of all the perimeters detected select the longest
            List<List<int>> longestPerimeters = new List<List<int>>();
            centres = new List<float>();
            bounding_boxes = new List<float>();
            if (max_length > 0)
            {
                for (int i = 0; i < temp_perimeters.Count; i++)
                {
                    int length = temp_perimeter_lengths[i];

                    // is this perimeter longer than the minimum ?
                    if (length > minimum_length_pixels)
                    {
                        // add this perimeter to the list
                        List<int> perim = (List<int>)temp_perimeters[i];
                        longestPerimeters.Add(perim);
                        centres.Add(temp_perimeter_centres[i*2]);
                        centres.Add(temp_perimeter_centres[(i*2)+1]);
                        for (int j = 0; j < 4; j++)                            
                            bounding_boxes.Add(temp_bounding_boxes[(i*4)+j]);
                    }
                }
            }

            return (longestPerimeters);
        }

        #endregion
  
        #region "finding the periphery of a square shape"

        private static void BestFitLineAverage(int[] data,
                                               ref float x0, ref float y0,
                                               ref float x1, ref float y1)
        {
            x0 = 0;
            y0 = 0;
            x1 = 0;
            y1 = 0;
            int hits1 = 0;
            int hits2 = 0;

            // find all the non zero values
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] != 0)
                {
                    if (i < data.Length / 2)
                    {
                        x0 += data[i];
                        y0 += i;
                        hits1++;
                    }
                    else
                    {
                        x1 += data[i];
                        y1 += i;
                        hits2++;
                    }
                }
            }

            if (hits1 > 0)
            {
                x0 /= hits1;
                y0 /= hits1;
            }
            if (hits2 > 0)
            {
                x1 /= hits1;
                y1 /= hits1;
            }
        }


        private static int BestFitLine(int[] data,
                                       ref int best_start,
                                       ref int best_end)
        {
            int max_diff = 2;
            int max_hits = 0;
            best_start = 0;
            best_end = 0;
            
            // find all the non zero values
            List<int> nonzero = new List<int>();
            for (int i = 0; i < data.Length; i++)
                if (data[i] != 0) nonzero.Add(i);
            
            // a fixed maximum number of steps that we will consider
            const float max_steps = 100;
            int step_size = (int)Math.Round(nonzero.Count / max_steps);
            if (step_size < 1) step_size = 1;
            int step_size2 = step_size;
            int fit_step_size = step_size * 2;
            
            int w = nonzero.Count * 40 / 100;
            int w2 = nonzero.Count - 1 - w;
            
            int no_of_fitting_samples = nonzero.Count / fit_step_size;
            
            for (int p1 = 0; p1 < w; p1 += step_size)
            {
                // start position of the line
                int i = nonzero[p1];
                int start_pos = data[i];
                
                for (int p2 = w2; p2 < nonzero.Count; p2 += step_size2)
                {
                    // end position of the line
                    int j = nonzero[p2];
                    int end_pos = data[j];
                    
                    // see how closely the data fits to this line
                    int diff = end_pos - start_pos;
                    int hits = 0;
                    float mult = diff / (float)(j - i);
                    int samples_remaining = no_of_fitting_samples;
                    for (int p3 = nonzero.Count-1; p3 >= 0; p3 -= fit_step_size)
                    {
                        int k = nonzero[p3];
                        
                        // position along the line
                        int intermediate = start_pos + (int)((k - i) * mult);
                        
                        // difference between the line and the actual data
                        int diff_from_intermediate = intermediate - data[k];                       
                        if (diff_from_intermediate < 0) diff_from_intermediate = -diff_from_intermediate;
                        
                        // is the difference small enough to consider to
                        // be one the line?
                        if (diff_from_intermediate < max_diff)
                        {
                            hits++;
                            
                            // if we can't win then quit fitting
                            if (hits + samples_remaining < max_hits) p3 = -1;
                        }
                        samples_remaining--;
                    }
                    if (hits > max_hits)
                    {
                        max_hits = hits;
                        best_start = i;
                        best_end = j;
                    }
                    
                }
            }
            return(max_hits);
        }
        
        /// <summary>
        /// rotates the given set of edges around the given origin
        /// </summary>
        /// <param name="edges">
        /// list of edge points <see cref="List`1"/>
        /// </param>
        /// <param name="centre_x">
        /// x coordinate of the origin <see cref="System.Int32"/>
        /// </param>
        /// <param name="centre_y">
        /// y coordinate of the origin <see cref="System.Int32"/>
        /// </param>
        /// <param name="rotate_angle">
        /// angle to rotate in radians <see cref="System.Single"/>
        /// </param>
        /// <returns>
        /// A <see cref="List`1"/>
        /// </returns>
        private List<int> RotateEdges(List<int> edges, 
                                      int centre_x, int centre_y, 
                                      float rotate_angle)
        {
            List<int> rotated = new List<int>();
            
            for (int i = 0; i < edges.Count; i += 2)
            {
                int dx = edges[i]- centre_x;
                int dy = edges[i+1] - centre_y;
                float hyp = (float)Math.Sqrt((dx*dx)+(dy*dy));
                if ((int)hyp != 0)
                {
                    float angle = (float)Math.Acos(dx / hyp);
                    rotated.Add(centre_x + (int)(hyp * (float)Math.Sin(angle + rotate_angle)));
                    rotated.Add(centre_y + (int)(hyp * (float)Math.Cos(angle + rotate_angle)));                    
                }
            }
            return(rotated);
        }
        
        
        /// <summary>
        /// returns a polygon representing the periphery of a square region
        /// </summary>
        /// <param name="edges">
        /// edges within the square region <see cref="List`1"/>
        /// </param>
        /// <param name="erode_dilate">
        /// erosion or dilation level <see cref="System.Int32"/>
        /// </param>
        /// <param name="perim">
        /// returned periphery <see cref="polygon2D"/>
        /// </param>
        /// <returns>
        /// set of edges around the periphery <see cref="List`1"/>
        /// </returns>
        private static List<List<int>> GetSquarePeriphery(List<int> edges,
                                                          int erode_dilate,
                                                          ref polygon2D perim)
        {
            List<List<int>> result = new List<List<int>>();
            perim = new polygon2D();
            
            // find the bounding box for all edges
            int tx = 99999;
            int ty = 99999;
            int bx = -99999;
            int by = -99999;            
            for (int i = edges.Count-2; i >= 0; i -= 2)
            {
                int x = edges[i];
                int y = edges[i+1];
                if (x < tx) tx = x;
                if (y < ty) ty = y;
                if (x > bx) bx = x;
                if (y > by) by = y;
            }

            int w = bx - tx;
            int h = by - ty;
            
            if ((w > 0) && (h > 0))
            {                
                int[] left = new int[h+1];
                int[] right = new int[h+1];
                int[] top = new int[w+1];
                int[] bottom = new int[w+1];
                for (int i = edges.Count - 2; i >= 0; i -= 2)
                {
                    int x = edges[i];
                    int x2 = x - tx;
                    int y = edges[i+1];
                    int y2 = y - ty;
                    
                    // left side
                    if ((left[y2] == 0) ||
                        (x < left[y2]))
                        left[y2] = x;

                    // right side
                    if ((right[y2] == 0) ||
                        (x > right[y2]))
                        right[y2] = x;
                    
                    // top
                    if ((top[x2] == 0) ||
                        (y < top[x2]))
                        top[x2] = y;

                    // bottom
                    if ((bottom[x2] == 0) ||
                        (y > bottom[x2]))
                        bottom[x2] = y;
                }

#if SHOW_TIMINGS
                stopwatch timer_best_fit = new stopwatch();
                timer_best_fit.Start();
#endif

                // find a best fit line for the left side
                int best_start = 0;
                int best_end = 0;
                int hits = BestFitLine(left, ref best_start, ref best_end);
                float left_x0 = left[best_start];
                float left_y0 = ty + best_start;
                float left_x1 = left[best_end];
                float left_y1 = ty + best_end;
                /*
                BestFitLineAverage(left, 
                                   ref left_x0, ref left_y0, 
                                   ref left_x1, ref left_y1);
                left_y0 += ty;
                left_y1 += ty;
                */

                // find a best fit line for the right side
                best_start = 0;
                best_end = 0;
                hits = BestFitLine(right, ref best_start, ref best_end);
                float right_x0 = right[best_start];
                float right_y0 = ty + best_start;
                float right_x1 = right[best_end];
                float right_y1 = ty + best_end;

                /*
                BestFitLineAverage(right,
                                   ref right_x0, ref right_y0,
                                   ref right_x1, ref right_y1);
                right_y0 += ty;
                right_y1 += ty;
                 */

                // find a best fit line for the top side
                best_start = 0;
                best_end = 0;
                hits = BestFitLine(top, ref best_start, ref best_end);
                float top_x0 = tx + best_start;
                float top_y0 = top[best_start];
                float top_x1 = tx + best_end;
                float top_y1 = top[best_end];

                /*
                BestFitLineAverage(top,
                                   ref top_x0, ref top_y0,
                                   ref top_x1, ref top_y1);
                top_x0 += tx;
                top_x1 += tx;
                */

                // find a best fit line for the bottom side
                best_start = 0;
                best_end = 0;
                hits = BestFitLine(bottom, ref best_start, ref best_end);
                float bottom_x0 = tx + best_start;
                float bottom_y0 = bottom[best_start];
                float bottom_x1 = tx + best_end;
                float bottom_y1 = bottom[best_end];

                /*
                BestFitLineAverage(bottom,
                                   ref bottom_x0, ref bottom_y0,
                                   ref bottom_x1, ref bottom_y1);
                bottom_x0 += tx;
                bottom_x1 += tx;
                 */

#if SHOW_TIMINGS
                timer_best_fit.Stop();
                if (timer_best_fit.time_elapsed_mS > 20)
                    Console.WriteLine("GetSquarePeriphery: best fit  " + timer_best_fit.time_elapsed_mS.ToString() );
#endif                
                
                // find the intersection between the left side and the top side
                float ix=0;
                float iy = 0;
                geometry.intersection(left_x1, left_y1, left_x0, left_y0,
                                      top_x1, top_y1, top_x0, top_y0,
                                      ref ix, ref iy);
                perim.Add(ix, iy);

                // find the intersection between the right side and the top side
                ix = 0;
                iy = 0;
                geometry.intersection(right_x1, right_y1, right_x0, right_y0,
                                      top_x0, top_y0, top_x1, top_y1,
                                      ref ix, ref iy);
                perim.Add(ix, iy);

                // find the intersection between the right side and the bottom side
                ix = 0;
                iy = 0;
                geometry.intersection(right_x1, right_y1, right_x0, right_y0,
                                      bottom_x0, bottom_y0, bottom_x1, bottom_y1,
                                      ref ix, ref iy);
                perim.Add(ix, iy);

                // find the intersection between the left side and the bottom side
                ix = 0;
                iy = 0;
                geometry.intersection(left_x1, left_y1, left_x0, left_y0,
                                      bottom_x0, bottom_y0, bottom_x1, bottom_y1,
                                      ref ix, ref iy);
                perim.Add(ix, iy);
                                
                // left and right
                List<int> left_edges = new List<int>();
                List<int> right_edges = new List<int>();
                for (int y = h; y >= 0; y--)
                {
                    if (left[y] != 0)
                    {
                        left_edges.Add(left[y]);
                        left_edges.Add(ty + y);
                    }
                    if (right[y] != 0)
                    {
                        right_edges.Add(right[y]);
                        right_edges.Add(ty + y);
                    }
                }
                
                // top and bottom
                List<int> top_edges = new List<int>();
                List<int> bottom_edges = new List<int>();
                for (int x = w; x >= 0; x--)
                {
                    if (top[x] != 0)
                    {
                        top_edges.Add(tx + x);
                        top_edges.Add(top[x]);
                    }
                    if (bottom[x] != 0)
                    {
                        bottom_edges.Add(tx + x);
                        bottom_edges.Add(bottom[x]);
                    }
                }
                
                float aspect_check = perim.getShortestSide() / perim.getLongestSide();
                if (aspect_check > 0.2f)
                {                
                    result.Add(left_edges);
                    result.Add(right_edges);
                    result.Add(top_edges);
                    result.Add(bottom_edges);
                }
                else perim = null;
            }

            // shrink the perimeter according to the erosion/dilation value
            if ((perim != null) && (erode_dilate != 0))
            {
                if (perim.x_points != null)
                {
                    float shrink_percent = (erode_dilate*2) / (perim.getPerimeterLength()/4.0f);
                    perim = perim.Scale(1.0f - shrink_percent);
                }
                else perim = null;
            }
            
            return(result);
        }
        
        #endregion
    }
}
