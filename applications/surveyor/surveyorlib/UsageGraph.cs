/*
    hypergraph which describes the usage pattern of a program
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
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace surveyor.vision
{
    #region "program usage expressed as a hypergraph"

    public class UsageLink : hypergraph_link
    {
        #region "duration"

        protected List<double> Duration = new List<double>();

        public void SetDuration(double duration)
        {
            Duration.Add(duration);
            if (Duration.Count > 100) Duration.RemoveAt(0);
        }

        /// <summary>
        /// returns the average duration
        /// </summary>
        /// <returns>average duration</returns>
        public double GetAverageDuration()
        {
            double duration = 0;

            for (int i = 0; i < Duration.Count; i++)
                duration += Duration[i];
            if (Duration.Count > 0) duration /= Duration.Count;

            return (duration);
        }

        #endregion

        #region "saving and loading"

        public void Save(BinaryWriter bw)
        {
            bw.Write(Duration.Count);
            for (int i = 0; i < Duration.Count; i++)
                bw.Write(Duration[i]);
        }

        public void Load(List<double> Duration)
        {
            this.Duration.Clear();
            for (int i = 0; i < Duration.Count; i++)
               this.Duration.Add(Duration[i]);
        }

        #endregion
    }

    public class UsageNode : hypergraph_node
    {
        public ulong Hits;
        public byte Symbol;
        public string ClassName;
        public string MethodName;

        #region "constructors"

        public UsageNode(
            string Description, 
            int EventID, 
            int Symbol)
            : base(1)
        {
            this.ID = EventID;
            this.Name = Description;
            this.Symbol = (byte)Symbol;
            ClassName = "";
            MethodName = "";
            Hits = 0;
        }

        public UsageNode(
            string Description, 
            int EventID, 
            int Symbol,
            string ClassName,
            string MethodName)
            : base(1)
        {
            this.ID = EventID;
            this.Name = Description;
            this.Symbol = (byte)Symbol;
            this.ClassName = ClassName;
            this.MethodName = MethodName;
            Hits = 0;
        }

        #endregion

        #region "saving and loading"

        // temporary list of node IDs to which this node is connected
        public List<int> temp_link_IDs;
        public List<List<double>> temp_duration;

        public void Save(BinaryWriter bw)
        {
            bw.Write(ID);
            bw.Write(Name);
            bw.Write(Hits);
            bw.Write(Symbol);
            bw.Write(ClassName);
            bw.Write(MethodName);

            bw.Write(Links.Count);
            for (int i = 0; i < Links.Count; i++)
            {
                bw.Write(Links[i].From.ID);
                ((UsageLink)Links[i]).Save(bw);
            }
        }

        public static UsageNode Load(BinaryReader br)
        {
            // read the node data
            int ID = br.ReadInt32();
            string Description = br.ReadString();
            ulong Hits = br.ReadUInt64();
            int Symbol = br.ReadByte();
            string ClassName = br.ReadString();
            string MethodName = br.ReadString();
            UsageNode n = new UsageNode(Description, ID, Symbol, ClassName, MethodName);
            n.Hits = Hits;

            // read the link IDs
            n.temp_link_IDs = new List<int>();
            n.temp_duration = new List<List<double>>();
            int no_of_links = br.ReadInt32();
            for (int i = 0; i < no_of_links; i++)
            {
                n.temp_link_IDs.Add(br.ReadInt32());

                List<double> duration = new List<double>();
                int no_of_duration_values = br.ReadInt32();
                for (int j = 0; j < no_of_duration_values; j++)
                    duration.Add(br.ReadDouble());
                n.temp_duration.Add(duration);
            }

            return (n);
        }

        #endregion
    }

    public class UsageGraph : hypergraph
    {
        public bool enabled = true;
        
        public const int SYMBOL_ELLIPSE = 0;
        public const int SYMBOL_SQUARE = 1;

        public bool show_milliseconds;
        public List<string> EventDescription = new List<string>();
        public List<int> EventID = new List<int>();
        public List<int> EventSymbol = new List<int>();
        protected UsageNode prev_node = null;
        protected DateTime prev_node_time;
        public string log_file = "";

        public void Reset()
        {
            prev_node = null;
        }

        #region "linking nodes"

        public override void LinkByID(int from_node_ID, int to_node_ID)
        {
            UsageLink link = new UsageLink();
            link.From = GetNode(from_node_ID);
            link.To = GetNode(to_node_ID);
            if (!LinkExists(link.From, link.To))
                link.To.Add(link);
        }

        #endregion

        #region "saving and loading"

        public void Save(string filename)
        {
            FileStream fs = File.Open(filename, FileMode.Create);

            BinaryWriter bw = new BinaryWriter(fs);

            bw.Write(EventDescription.Count);
            for (int i = 0; i < EventDescription.Count; i++)
            {
                bw.Write(EventID[i]);
                bw.Write(EventDescription[i]);
                bw.Write(EventSymbol[i]);
            }

            bw.Write(Nodes.Count);
            for (int i = 0; i < Nodes.Count; i++)
            {
                ((UsageNode)Nodes[i]).Save(bw);
            }

            bw.Close();
            fs.Close();
        }

        public static UsageGraph Load(string filename)
        {
            UsageGraph graph = new UsageGraph();

            if (File.Exists(filename))
            {
                FileStream fs = File.Open(filename, FileMode.Open);

                BinaryReader br = new BinaryReader(fs);

                // read event types
                int no_of_events = br.ReadInt32();
                for (int i = 0; i < no_of_events; i++)
                {
                    graph.EventID.Add(br.ReadInt32());
                    graph.EventDescription.Add(br.ReadString());
                    graph.EventSymbol.Add(br.ReadInt32());
                }

                // load nodes
                int no_of_nodes = br.ReadInt32();
                for (int i = 0; i < no_of_nodes; i++)
                    graph.Add(UsageNode.Load(br));

                br.Close();
                fs.Close();

                // link nodes together
                for (int i = 0; i < graph.Nodes.Count; i++)
                {
                    UsageNode n = (UsageNode)graph.Nodes[i];
                    if (n.temp_link_IDs != null)
                    {
                        for (int j = 0; j < n.temp_link_IDs.Count; j++)
                        {
                            graph.LinkByID(n.temp_link_IDs[j], n.ID);
                            UsageLink lnk = (UsageLink)(n.Links[n.Links.Count - 1]);
                            lnk.Load(n.temp_duration[j]);
                        }
                    }
                }
            }

            return (graph);
        }

        #endregion

        #region "adjusting hit scores to prevent overflows"

        /// <summary>
        /// renumbers hit scores to prevent overflows
        /// </summary>
        protected void RenumberHits()
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                UsageNode n = (UsageNode)Nodes[i];
                n.Hits /= 2;
            }
        }

        #endregion

        #region "main update"
        
        /// <summary>
        /// replaces all spaces with underscores
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string ReplaceSpaces(string str)
        {
            if (str.Contains(" "))
            {
                char[] ch = str.ToCharArray();
                string result = "";
                for (int i = 0; i < ch.Length; i++)
                {
                    if (ch[i] != ' ')
                        result += ch[i];
                    else
                        result += "_";
                }
                return(result);
            }
            else
            {
                return(str);
            }
        }
        
        /// <summary>
        /// parses the given description, extracting class and method names if they exist
        /// </summary>
        /// <param name="Description"></param>
        /// <param name="ClassName"></param>
        /// <param name="MethodName"></param>
        protected void ParseDescription(
            ref string Description,
            ref string ClassName,
            ref string MethodName)
        {
            if (Description.Contains(","))
            {
                string[] str = Description.Split(',');
                Description = str[0].Trim();
                ClassName = str[1].Trim();
                if (str.Length > 2) 
                    MethodName = str[2].Trim();
                else
                    MethodName = "";
            }
            else
            {
                ClassName = "";
                MethodName = "";
            }
        }    

        public void Update(string Description)
        {
            if (enabled) Update(Description, 0);
        }

        bool writing_to_log;
        
        private void LogToFile(string description)
        {
            DateTime start = DateTime.Now;
            while (writing_to_log)
            {
                TimeSpan diff = DateTime.Now.Subtract(start);
                if (diff.TotalMilliseconds > 500) break;
                System.Threading.Thread.Sleep(2);
            }
            
            if (!writing_to_log)
            {
                writing_to_log = true;
                
                string[] str = description.Split(',');
                
                StreamWriter oWrite = null;
                if (File.Exists(log_file))
                {
                    oWrite = File.AppendText(log_file);
                }
                else
                {
                    oWrite = File.CreateText(log_file);
                    Console.WriteLine("Logging events to " + log_file);
                }
                oWrite.Write(start.ToString() + " " + start.Millisecond.ToString() + ",");
                for (int i = 0; i < str.Length; i++)
                {
                    oWrite.Write(str[i].Trim());
                    oWrite.Write(",");
                }
                oWrite.WriteLine("");
                oWrite.Close();
                
                writing_to_log = false;
            }
        }
        
        public void Update(string Description, int symbol)
        {
            if (enabled)
            {
                if (log_file != "") LogToFile(Description);
                
                string ClassName = "";
                string MethodName = "";
                ParseDescription(
                    ref Description, 
                    ref ClassName, 
                    ref MethodName);
            
                UsageNode n = null;
                int ID;
                int pos = EventDescription.IndexOf(Description);
                if (pos > -1)
                {
                    ID = EventID[pos];
                    n = (UsageNode)GetNode(ID);
                    symbol = EventSymbol[pos];
                }
                else
                {
                    ID = EventID.Count;
                    EventDescription.Add(Description);
                    EventID.Add(ID);
                    EventSymbol.Add(symbol);
                    n = new UsageNode(Description, ID, symbol, ClassName, MethodName);
                    Add(n);
                }
    
                // increment hit score for this node
                n.Hits++;
                if (n.Hits > 5000)
                    RenumberHits();
    
                // link nodes together
                if (prev_node != null)
                {
                    LinkByID(n.ID, prev_node.ID);
    
                    // set the duration between events
                    UsageLink lnk = (UsageLink)prev_node.GetLink(n.ID);
                    if (lnk != null)
                    {
                        TimeSpan time_elapsed = DateTime.Now.Subtract(prev_node_time);
                        lnk.SetDuration(time_elapsed.TotalMilliseconds);
                    }
                }
    
                prev_node = n;
                prev_node_time = DateTime.Now;
            }
        }

        #endregion

        #region "making prodictions about what is likely to happen next"

        /// <summary>
        /// predicts the even most likely to happen next
        /// </summary>
        /// <returns></returns>
        public string PredictNextEvent()
        {
            string next_event_description = "";

            if (prev_node != null)
            {
                ulong max_hits = 0;
                for (int i = 0; i < prev_node.Links.Count; i++)
                {
                    UsageLink lnk = (UsageLink)prev_node.Links[i];
                    UsageNode n = (UsageNode)lnk.From;
                    if (n.Hits > max_hits)
                    {
                        max_hits = n.Hits;
                        next_event_description = n.Name;
                    }
                }
            }
            return (next_event_description);
        }

        /// <summary>
        /// predicts how many seconds are likely to elapse before the next event takes place
        /// </summary>
        /// <returns>number of seconds before the next event</returns>
        public float PredictNextEventArrivalSec()
        {
            float next_event_arrival_sec = 0;

            if (prev_node != null)
            {
                ulong max_hits = 0;
                int winner = -1;
                for (int i = 0; i < prev_node.Links.Count; i++)
                {
                    UsageLink lnk = (UsageLink)prev_node.Links[i];
                    UsageNode n = (UsageNode)lnk.From;
                    if (n.Hits > max_hits)
                    {
                        max_hits = n.Hits;
                        winner = i;
                    }
                }
                if (winner > -1)
                {
                    UsageLink lnk = (UsageLink)prev_node.Links[winner];
                    double av_duration_mS = lnk.GetAverageDuration();
                    TimeSpan diff = DateTime.Now.Subtract(prev_node_time);
                    next_event_arrival_sec = (float)(diff.TotalMilliseconds - av_duration_mS) / 1000.0f;
                    if (next_event_arrival_sec < 0) next_event_arrival_sec = 0;
                }
            }
            return (next_event_arrival_sec);
        }

        /// <summary>
        /// returns a list of the next likely events
        /// </summary>
        /// <param name="Description">description of each event</param>
        /// <param name="Probability">probability of each event</param>
        /// <param name="TimeToEventSec">number of seconds before each event is likely to occur</param>
        public void PredictNextEvents(
            ref List<string> Description,
            ref List<float> Probability,
            ref List<float> TimeToEventSec)
        {
            if (Description == null) Description = new List<string>();
            if (Probability == null) Probability = new List<float>();
            if (TimeToEventSec == null) TimeToEventSec = new List<float>();
            Description.Clear();
            Probability.Clear();
            TimeToEventSec.Clear();

            if (prev_node != null)
            {
                ulong total_hits = 0;
                for (int i = 0; i < prev_node.Links.Count; i++)
                {
                    UsageLink lnk = (UsageLink)prev_node.Links[i];
                    UsageNode n = (UsageNode)lnk.From;
                    total_hits += n.Hits;
                }
                for (int i = 0; i < prev_node.Links.Count; i++)
                {
                    UsageLink lnk = (UsageLink)prev_node.Links[i];
                    UsageNode n = (UsageNode)lnk.From;
                    Description.Add(n.Name);
                    Probability.Add((float)((double)n.Hits / (double)total_hits));
                    
                    double av_duration_mS = lnk.GetAverageDuration();
                    TimeSpan diff = DateTime.Now.Subtract(prev_node_time);
                    float diff_sec = (float)(diff.TotalMilliseconds - av_duration_mS) / 1000.0f;
                    if (diff_sec < 0) diff_sec = 0;
                    TimeToEventSec.Add(diff_sec);
                }
            }
        }

        #endregion

        #region "extracting common sequences"

        /// <summary>
        /// returns a graph containing events separated by a time less than the given interval
        /// </summary>
        /// <param name="maximum_temoral_separation_sec">maximum time between events</param>
        /// <returns>graph object</returns>
        public UsageGraph GetCommonSequences(float maximum_temoral_separation_sec)
        {
            double maximum_temoral_separation_mS = maximum_temoral_separation_sec * 1000;

            UsageGraph graph = new UsageGraph();

            for (int i = 0; i < Nodes.Count; i++)
            {
                UsageNode n = (UsageNode)Nodes[i];
                for (int j = 0; j < n.Links.Count; j++)
                {
                    UsageLink lnk = (UsageLink)n.Links[j];
                    double duration_mS = lnk.GetAverageDuration();
                    if (duration_mS < maximum_temoral_separation_mS)
                    {
                        graph.Update(n.Name);
                        graph.Update(lnk.From.Name);
                        graph.Reset();
                    }
                }
            }

            return (graph);
        }

        /// <summary>
        /// exports common sequences for visualisation using Graphviz
        /// </summary>
        /// <param name="maximum_temoral_separation_sec"></param>
        /// <param name="filename"></param>
        /// <param name="invert"></param>
        /// <param name="show_durations"></param>
        public void ExportCommonSequencesAsDot(
            float maximum_temoral_separation_sec,
            string filename,
            bool invert,
            bool show_durations)
        {
            UsageGraph graph = GetCommonSequences(maximum_temoral_separation_sec);
            graph.ExportAsDot(filename, invert, show_durations);
        }

        #endregion

        #region "exporting dot files for use with Graphviz"

        protected string GetHitsSuffix(ulong Hits, ulong total_hits)
        {
            string suffix = "__";
            int v = (int)(((double)Hits / (double)total_hits) * 1000);
            if (v == 1000)
                suffix += v.ToString();
            else
            {
                if (v >= 100)
                    suffix += "0" + v.ToString();
                else
                {
                    if (v >= 10)
                        suffix += "00" + v.ToString();
                    else
                        suffix += "000" + v.ToString();
                }
            }
            return (suffix);
        }

        /// <summary>
        /// export the graph in dot format
        /// </summary>
        /// <param name="filename">filename to save as</param>
        /// <param name="invert">inverts the direction of links</param>
        /// <param name="show_weight">show durations between events in seconds</param>
        public override void ExportAsDot(
            string filename,
            bool invert,
            bool show_weight)
        {
            StreamWriter oWrite = null;
            bool allowWrite = true;
            int max_edge_weight = 10;

            try
            {
                oWrite = File.CreateText(filename);
            }
            catch
            {
                allowWrite = false;
            }

            if (allowWrite)
            {
                ulong total_hits = 0;
                for (int i = 0; i < Nodes.Count; i++)
                {
                    UsageNode n = (UsageNode)Nodes[i];
                    total_hits += n.Hits;
                }

                // compute durations, and find the longest duration
                double max_duration_mS = 0;
                List<double> durations = new List<double>();
                for (int i = 0; i < Nodes.Count; i++)
                {
                    for (int j = 0; j < Nodes[i].Links.Count; j++)
                    {
                        UsageLink lnk = (UsageLink)Nodes[i].Links[j];
                        double duration_mS = lnk.GetAverageDuration();
                        durations.Add(duration_mS);
                        if (duration_mS > max_duration_mS) max_duration_mS = duration_mS;
                    }
                }

                int ctr = 0;
                string str;
                string duration_str = "";
                oWrite.WriteLine("digraph G {");
                for (int i = 0; i < Nodes.Count; i++)
                {
                    UsageNode node = (UsageNode)Nodes[i];
                    if (node.Symbol != SYMBOL_ELLIPSE)
                    {
                        str = "    " + ReplaceSpaces(node.Name);
                        str += GetHitsSuffix(node.Hits, total_hits);
                        str += " [shape=";
                        int symbol = (int)(node.Symbol);
                        switch (symbol)
                        {
                            case SYMBOL_SQUARE:
                                {
                                    str += "square";
                                    break;
                                }
                        }
                        str += "];";
                        oWrite.WriteLine(str);
                    }

                    for (int j = 0; j < Nodes[i].Links.Count; j++, ctr++)
                    {
                        UsageLink lnk = (UsageLink)Nodes[i].Links[j];
                        UsageNode from_node = (UsageNode)(lnk.From);
                        UsageNode to_node = (UsageNode)Nodes[i];

                        if (invert)
                        {
                            to_node = (UsageNode)(Nodes[i].Links[j].From);
                            from_node = (UsageNode)Nodes[i];
                        }

                        double duration_mS = 0;
                        if (show_weight)
                        {
                            if (ctr < durations.Count)
                            {
                                duration_mS = durations[ctr];
                                double duration_sec = duration_mS / 1000.0;
                                if (show_milliseconds)
                                    duration_str = ((int)duration_mS).ToString();
                                else
                                    duration_str = ((int)(duration_sec * 10) / 10.0f).ToString();
                            }
                        }

                        str = "    " +
                              ReplaceSpaces(from_node.Name) + GetHitsSuffix(from_node.Hits, total_hits) +
                              " -> " +
                              ReplaceSpaces(to_node.Name) + GetHitsSuffix(to_node.Hits, total_hits);

                        if (show_weight)
                        {
                            if (duration_str != "0")  // don't bother showing zero values
                            {
                                str += " [label=" + '"' + duration_str + '"';
                                str += ",weight=" + ((int)(duration_mS * max_edge_weight / max_duration_mS)).ToString() + "]";
                            }
                        }
                        
                        str += ";";

                        oWrite.WriteLine(str);
                    }
                }
                oWrite.WriteLine("}");
                oWrite.Close();
            }
        }

        #endregion
    }

    #endregion
}
