/*
    xml format utility functions
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
using System.Xml;

namespace sentience.calibration
{
    /// <summary>
    /// contains functions for dealing with XML
    /// </summary>
	public class xml
	{
		public xml()
		{
		}
		
		/// <summary>
        /// This method adds a text element to the XML document as the last
        /// child of the current element.
        /// </summary>
        /// <param name="doc">xml document object</param>
        /// <param name="nodeParent">xml parent node</param>
        /// <param name="strTag">The tag of the element to add</param>
        /// <param name="strValue">The text value of the new element</param>
        public static XmlElement AddTextElement(XmlDocument doc, XmlElement nodeParent, String strTag, String strValue)
        {
            XmlElement nodeElem = doc.CreateElement(strTag);
            XmlText nodeText = doc.CreateTextNode(strValue);
            nodeParent.AppendChild(nodeElem);
            nodeElem.AppendChild(nodeText);
            return (nodeElem);
        }

        /// <summary>
        /// returns true if the given string contains a URL
        /// </summary>
        private static bool isURL(String str)
        {
            if ((str.StartsWith("http")) ||
                (str.StartsWith("ftp")) ||
                (str.StartsWith("1")) || (str.StartsWith("2")) || (str.StartsWith("3")) ||
                (str.StartsWith("4")) || (str.StartsWith("5")) || (str.StartsWith("6")) ||
                (str.StartsWith("7")) || (str.StartsWith("8")) || (str.StartsWith("9"))
               )
                return(true);
            else
                return(false);
        }

		/// <summary>
        /// This method adds an image element to the XML document as the last
        /// child of the current element.  This is suitable for viewing the
        /// XML document directly within a W3C standards compliant browser
        /// such as Firefox or Opera
        /// </summary>
        /// <param name="doc">xml document object</param>
        /// <param name="nodeParent">xml parent node</param>
        /// <param name="strTag">The tag of the element to add</param>
        /// <param name="filename">The image filename or URL</param>
        public static XmlElement AddImageElement(XmlDocument doc, XmlElement nodeParent, String strTag, 
                                                 String filename, int width, int height)
        {
            XmlElement nodeElem = doc.CreateElement(strTag);
            nodeElem.SetAttribute("xml:link", "simple");
            nodeElem.SetAttribute("show", "replace");
            nodeParent.AppendChild(nodeElem);

            String file_name = filename;
            if (!isURL(filename))
                file_name = "file://" + filename;

            XmlElement nodeImg = doc.CreateElement("img");
            nodeImg.SetAttribute("xmlns", "http://www.w3.org/1999/xhtml");
            nodeImg.SetAttribute("src", file_name);
            if (width > 0)
            {
                nodeImg.SetAttribute("width", width.ToString());
                nodeImg.SetAttribute("height", height.ToString());
            }
            nodeElem.AppendChild(nodeImg);
            
            return (nodeElem);
        }


		/// <summary>
        /// This method adds an image element to the XML document in a manner
        /// which is suitable for viewing with an xsl style sheet
        /// </summary>
        /// <param name="doc">xml document object</param>
        /// <param name="nodeParent">xml parent node</param>
        /// <param name="strTag">The tag of the element to add</param>
        /// <param name="filename">The image filename or URL</param>
        public static XmlElement AddImageElementSimple(
                XmlDocument doc, XmlElement nodeParent, String strTag,
                String filename)
        {
            String file_name = filename;
            /*
            if (!isURL(filename))
                if (!filename.StartsWith("file:"))
                    file_name = "file://" + filename;
            */
            XmlElement nodeImg = doc.CreateElement(strTag);
            nodeImg.SetAttribute("src", file_name);
            nodeParent.AppendChild(nodeImg);
            
            return (nodeImg);
        }


		/// <summary>
        /// This method adds an image element to the XML document in a manner
        /// which is suitable for viewing with an xsl style sheet
        /// </summary>
        /// <param name="doc">xml document object</param>
        /// <param name="nodeParent">xml parent node</param>
        /// <param name="strTag">The tag of the element to add</param>
        /// <param name="filename">The image filename or URL</param>
        public static XmlElement AddImageElementSimple(
                XmlDocument doc, XmlElement nodeParent, String strTag,
                String filename, int width, int height)
        {
            String file_name = filename;
            /*
            if (!isURL(filename))
                if (!filename.StartsWith("file:"))
                    file_name = "file://" + filename;
            */
            XmlElement nodeImg = doc.CreateElement(strTag);
            nodeImg.SetAttribute("src", file_name);
            if (width > 0)
            {
                nodeImg.SetAttribute("width", width.ToString());
                nodeImg.SetAttribute("height", height.ToString());
            }
            nodeParent.AppendChild(nodeImg);
            
            return (nodeImg);
        }

        /// <summary>
        /// adds a comment to an xml document
        /// </summary>
        /// <param name="doc">xml document object</param>
        /// <param name="nodeParent">xml parent node</param>
        /// <param name="comment">comment text</param>
        public static void AddComment(XmlDocument doc, XmlElement nodeParent, String comment)
        {
            XmlNode commentnode = doc.CreateComment(comment);
            nodeParent.AppendChild(commentnode);
        }

        /// <summary>
        /// Adds a set of referenced documents to an xml document
        /// These could be web pages, pdfs, or other useful references
        /// </summary>
        /// <param name="doc">xml document object</param>
        /// <param name="nodeParent">xml parent node</param>
        /// <param name="document_descriptions">description of each document</param>
        /// <param name="document_URLs">URL of each document</param>
        public static void AddReferencedDocuments(XmlDocument doc, XmlElement nodeParent,
                                                  string[] document_descriptions,
                                                  string[] document_URLs)
        {
            XmlElement nodeDocs = doc.CreateElement("ReferencedDocuments");
            nodeParent.AppendChild(nodeDocs);

            for (int i = 0; i < document_descriptions.Length; i++)
            {
                XmlElement nodeDoc = doc.CreateElement("Document");
                nodeDocs.AppendChild(nodeDoc);

                AddTextElement(doc, nodeDoc, "Description", document_descriptions[i]);
                AddTextElement(doc, nodeDoc, "URL", document_URLs[i]);
            }
        }

    }
}
