/*
    Hypergraph objects
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
using System.Collections.Generic;

namespace sentience.calibration
{
    public class hypergraph_link
    {
        public hypergraph_node From;
        public hypergraph_node To;
    }
    
    public class hypergraph_node
    {
        public string Name;
        public int ID;
        public List<hypergraph_link> Links;
        public List<bool> Flags;        
        
        public hypergraph_node(int no_of_flags)
        {
            Flags = new List<bool>();
            for (int i = 0; i < no_of_flags; i++) Flags.Add(false);
            Links = new List<hypergraph_link>();
        }
        
        public void Add(hypergraph_link link)
        {
            Links.Add(link);
        }
    }
    
    public class hypergraph
    {
        public List<hypergraph_node> Nodes;
        public List<hypergraph_link> Links;
        
        #region "constructors"        
        
        public hypergraph()
        {
            Nodes = new List<hypergraph_node>();
            Links = new List<hypergraph_link>();
        }

        public hypergraph(int no_of_nodes, int no_of_flags)
        {
            Nodes = new List<hypergraph_node>();
            Links = new List<hypergraph_link>();
            for (int i = 0; i < no_of_nodes; i++)
            {
                hypergraph_node node = new hypergraph_node(no_of_flags);
                node.ID = i;
                node.Name = i.ToString();
                Add(node);
            }
        }
        
        #endregion
        
        #region "adding and removing nodes"        
        
        public void Add(hypergraph_node node)
        {
            Nodes.Add(node);
        }
        
        public void Remove(hypergraph_node node)
        {
            Nodes.Remove(node);
        }
        
        public void Remove(int ID)
        {
            int index = IndexOf(ID);
            if (index > -1) Nodes.RemoveAt(index);
        }

        public void Remove(string name)
        {
            int index = IndexOf(name);
            if (index > -1) Nodes.RemoveAt(index);
        }
        
        #endregion

        #region "getting nodes"
        
        public int IndexOf(int ID)
        {
            int index = -1;
            int i = 0;
            while ((i < Nodes.Count) && (index == -1))
            {
                if (Nodes[i].ID == ID) index = i;
                i++;
            }
            return(index);
        }

        public int IndexOf(string name)
        {
            int index = -1;
            int i = 0;
            while ((i < Nodes.Count) && (index == -1))
            {
                if (Nodes[i].Name == name) index = i;
                i++;
            }
            return(index);
        }
        
        public hypergraph_node GetNode(int ID)
        {
            hypergraph_node node = null;
            int index = IndexOf(ID);
            if (index > -1) node = Nodes[index];
            return(node);
        }

        public hypergraph_node GetNode(string name)
        {
            hypergraph_node node = null;
            int index = IndexOf(name);
            if (index > -1) node = Nodes[index];
            return(node);
        }
        
        #endregion

        #region "creating links between nodes"
        
        public void LinkByIndex(int from_node_index, int to_node_index)
        {
            hypergraph_link link = new hypergraph_link();
            link.From = Nodes[from_node_index];
            link.To = Nodes[to_node_index];
            link.To.Add(link);
            Links.Add(link);
        }

        public void LinkByID(int from_node_ID, int to_node_ID)
        {
            hypergraph_link link = new hypergraph_link();
            link.From = GetNode(from_node_ID);
            link.To = GetNode(to_node_ID);
            link.To.Add(link);
            Links.Add(link);
        }

        public void LinkByName(string from_node_name, string to_node_name)
        {
            hypergraph_link link = new hypergraph_link();
            link.From = GetNode(from_node_name);
            link.To = GetNode(to_node_name);
            link.To.Add(link);
            Links.Add(link);
        }

        public void LinkByReference(hypergraph_node from_node, hypergraph_node to_node)
        {
            hypergraph_link link = new hypergraph_link();
            link.From = from_node;
            link.To = to_node;
            link.To.Add(link);
            Links.Add(link);
        }

        public void LinkByReference(hypergraph_node from_node, hypergraph_node to_node,
                                    hypergraph_link link)
        {
            link.From = from_node;
            link.To = to_node;
            link.To.Add(link);
            Links.Add(link);
        }
        
        #endregion
        
        #region "removing links"
        
        /// <summary>
        /// removes the given link from the graph
        /// </summary>
        /// <param name="victim">
        /// link object to be removed <see cref="hypergraph_link"/>
        /// </param>
        public void RemoveLink(hypergraph_link victim)
        {
            hypergraph_node node = victim.To;
            node.Links.Remove(victim);
            Links.Remove(victim);
        }

        /// <summary>
        /// removes the given set of links from the graph
        /// </summary>
        /// <param name="victims">
        /// list of link objects to be removed <see cref="List`1"/>
        /// </param>
        public void RemoveLinks(List<hypergraph_link> victims)
        {
            for (int i = 0; i < victims.Count; i++)
                RemoveLink(victims[i]);
        }
        
        #endregion
        
        #region "setting the state of flags"

        public void SetFlagByIndex(int node_index, int flag_index, bool flag_state)
        {
            Nodes[node_index].Flags[flag_index] = flag_state;
        }

        public bool GetFlagByIndex(int node_index, int flag_index)
        {
            return(Nodes[node_index].Flags[flag_index]);
        }

        public void SetFlagByID(int node_ID, int flag_index, bool flag_state)
        {
            hypergraph_node node = GetNode(node_ID);
            node.Flags[flag_index] = flag_state;
        }

        public bool GetFlagByID(int node_ID, int flag_index)
        {
            hypergraph_node node = GetNode(node_ID);
            return(node.Flags[flag_index]);
        }

        public void SetFlagByName(string node_name, int flag_index, bool flag_state)
        {
            hypergraph_node node = GetNode(node_name);
            node.Flags[flag_index] = flag_state;
        }

        public bool GetFlagByName(string node_name, int flag_index)
        {
            hypergraph_node node = GetNode(node_name);
            return(node.Flags[flag_index]);
        }
        
        #endregion
        
        /// <summary>
        /// clear the state of all flags
        /// </summary>
        public void ClearFlags()
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                for (int j = 0; j < Nodes[i].Flags.Count; j++)
                    Nodes[i].Flags[j] = false;
            }
        }
        
        /// <summary>
        /// propogates the given flag index from the given node
        /// </summary>
        /// <param name="node_index">
        /// A <see cref="System.Int32"/>
        /// </param>
        /// <param name="flag_index">
        /// A <see cref="System.Int32"/>
        /// </param>
        /// <param name="members">
        /// A <see cref="List`1"/>
        /// </param>
        public void PropogateFlagFromIndex(int node_index, int flag_index, List<hypergraph_node> members)
        {
            hypergraph_node node = Nodes[node_index];
            if (node.Flags[flag_index] == false)
            {
                node.Flags[flag_index] = true;
                members.Add(node);
                for (int i = 0; i < node.Links.Count; i++)
                {
                    hypergraph_link link = node.Links[i];
                    PropogateFlag(link.From, flag_index, members);
                }
            }
        }

        /// <summary>
        /// propogates the given flag index from the given node
        /// </summary>
        /// <param name="node">
        /// A <see cref="hypergraph_node"/>
        /// </param>
        /// <param name="flag_index">
        /// A <see cref="System.Int32"/>
        /// </param>
        /// <param name="members">
        /// A <see cref="List`1"/>
        /// </param>
        public void PropogateFlag(hypergraph_node node, int flag_index, List<hypergraph_node> members)
        {
            if (node.Flags[flag_index] == false)
            {
                node.Flags[flag_index] = true;
                members.Add(node);
                for (int i = 0; i < node.Links.Count; i++)
                {
                    hypergraph_link link = node.Links[i];
                    PropogateFlag(link.From, flag_index, members);
                }
            }
        }
    }
}
