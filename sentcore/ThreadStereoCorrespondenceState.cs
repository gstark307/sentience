/*
    Sentience 3D Perception System
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

namespace sentience.core
{
    /// <summary>
    /// data used to calculate stereo correspondence
    /// </summary>
    public class ThreadStereoCorrespondenceState
    {
        public bool active;
		
		public bool EnableScanMatching;
		public float ScanMatchingMaxPanAngleChange;
		public float ScanMatchingPanAngleEstimate;
		public float pan;
		
		public stereoCorrespondence correspondence;
		
		public int correspondence_algorithm_type;
		public int no_of_stereo_features;
		public int stereo_camera_index;
		public stereoHead head;
        public byte[] fullres_left; 
        public byte[] fullres_right; 
        public int bytes_per_pixel;		
		public bool scanMatchesFound;
    }
}
