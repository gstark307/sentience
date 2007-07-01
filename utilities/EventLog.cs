/*
    Logging functions
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

namespace sluggish.utilities.logging
{
    /// <summary>
    /// This class is used for logging events or errors
    /// </summary>
	public class EventLog
	{
	    public static void AddEvent(String event_message)
	    {
	        Console.WriteLine("Event: " + event_message);
	    }

	    public static void AddError(String error_message)
	    {
	        Console.WriteLine("Error: " + error_message);
	    }
	}
}