/*
    Thread used for processing an image region
    Copyright (C) 2000-2007 Bob Mottram
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
using System.Collections;
using System.Threading;

namespace sluggish.imageprocessing
{
    public class ThreadRegionProcessingState
    {
        public bool active;

        // type of processing to be performed on the region
        public int processing_type;

        // camera image from which the region was taken
        public byte[] bmp;
        public int image_width, image_height;

        // the image region to be processed
        public region image_region;

        // image output as a result of processing
        public byte[] output_bmp;
        public int output_image_width, output_image_height;

        public ThreadRegionProcessingState(
                           int processing_type,
                           byte[] bmp, int image_width, int image_height,
                           region image_region)
        {
            active = true;
            this.processing_type = processing_type;
            this.bmp = bmp;
            this.image_width = image_width;
            this.image_height = image_height;
            this.image_region = image_region;
        }
    }

    /// <summary>
    /// this thread is used to process an image region
    /// </summary>
    public class ThreadRegionProcessing
    {
        private WaitCallback _callback;
        private object _data;

        /// <summary>
        /// constructor
        /// </summary>
        public ThreadRegionProcessing(WaitCallback callback, object data)
        {
            if (callback == null) throw new ArgumentNullException("callback");
            _callback = callback;
            _data = data;
        }

        /// <summary>
        /// ThreadStart delegate
        /// </summary>
        public void Execute()
        {
            // perform region growing using the available data
            ThreadRegionProcessingState state = (ThreadRegionProcessingState)_data;
            ProcessRegion(state);
            state.active = false;
            _callback(_data);
        }

        /// <summary>
        /// process an image region
        /// </summary>
        /// <param name="state">region processing state</param>
        private static void ProcessRegion(ThreadRegionProcessingState state)
        {
            // create a bitmap image from this region                            
            state.output_bmp = state.image_region.Export(
                state.bmp, state.image_width, state.image_height,
                ref state.output_image_width,
                ref state.output_image_height);
        }

        // WaitCallback delegate
        /*
        static void DetectRegions(object state)
        {
            Console.WriteLine("State: " + state);
        }
         */
    }
}
