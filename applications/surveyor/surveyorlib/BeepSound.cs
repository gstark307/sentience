/*
    Functions for playing sounds.  Note that this is specific to Microsoft Windows systems
    Copyright (C) 2007 Bob Mottram
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
using System.Runtime.InteropServices;

namespace sluggish.utilities
{
    /// <summary>
    /// used to play beepy noises when something is detected
    /// Note that because this is platform specific we keep it within the front end GUI
    /// </summary>
    public class BeepSound
    {
        // import a dodgy windows DLL
        [DllImport("winmm.dll", SetLastError = true)]
        static extern bool PlaySound(string pszSound, UIntPtr hmod, uint fdwSound);

        // constants which may be passed to the play method
        public const uint SND_SYNC = 0x0000;         // play synchronously (default) 
        public const uint SND_ASYNC = 0x0001;        // play asynchronously 
        public const uint SND_NODEFAULT = 0x0002;    // silence (!default) if sound not found 
        public const uint SND_MEMORY = 0x0004;       // pszSound points to a memory file 
        public const uint SND_LOOP = 0x0008;         // loop the sound until next sndPlaySound 
        public const uint SND_NOSTOP = 0x0010;       // don't stop any currently playing sound 

        public const uint SND_NOWAIT = 0x00002000;   // don't wait if the driver is busy 
        public const uint SND_ALIAS = 0x00010000;    // name is a registry alias 
        public const uint SND_ALIAS_ID = 0x00110000; // alias is a predefined ID 
        public const uint SND_FILENAME = 0x00020000; // name is file name 
        public const uint SND_RESOURCE = 0x00040004; // name is resource name or atom 
        public const uint SND_PURGE = 0x0040;        // purge non-static events for task 
        public const uint SND_APPLICATION = 0x0080;  // look for application specific association 

        /// <summary>
        /// play the given sound
        /// </summary>
        /// <param name="wfname"></param>
        /// <param name="SoundFlags"></param>
        /// <example>
        /// BeepSound.Play(wav_filename, BeepSound.SND_ASYNC);
        /// </example>
        public static void Play(string filename, uint SoundFlags)
        {
            PlaySound(filename, UIntPtr.Zero, SoundFlags);
        }

        /// <summary>
        /// play the given sound asynchronously
        /// </summary>
        /// <param name="filename">filename to play</param>
        public static void Play(string filename)
        {
            PlaySound(filename, UIntPtr.Zero, BeepSound.SND_ASYNC);
        }

        /// <summary>
        /// stop playing the sound
        /// </summary>
        public static void StopPlay()
        {
            PlaySound(null, UIntPtr.Zero, BeepSound.SND_PURGE);
        }
    }
}
