/*
    Copyright © 2020 Fernando Porrino Serrano
    Third party software licenses can be found at /docs/credits/thirdparties.md

    This file is part of AutoCheck.

    AutoCheck is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    AutoCheck is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with AutoCheck.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using AutoCheck.Core.Exceptions;

namespace AutoCheck.Core.Connectors{       
    /// <summary>
    /// Allows in/out operations and/or data validations with XML files.
    /// </summary>
    public class Xml: Base{         
        private List<string> Comments;

        /// <summary>
        /// The XML document content.
        /// </summary>
        /// <value></value>
        public XmlDocument XmlDoc {get; private set;}       
        
        /// <summary>
        /// Creates a new connector instance.
        /// </summary>
        /// <param name="path">The folder containing the files.</param>
        /// <param name="file">CSV file name.</param>
        /// <param name="fieldDelimiter">Field delimiter char.</param>
        /// <param name="textDelimiter">Text delimiter char.</param>
        public Xml(string path, string file, ValidationType validation = ValidationType.None){
            if(string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
            if(string.IsNullOrEmpty(file)) throw new ArgumentNullException("file");
            if(!Directory.Exists(path)) throw new DirectoryNotFoundException();
                        
            var settings = new XmlReaderSettings { 
                IgnoreComments = false,
                ValidationType = validation,                 
                DtdProcessing = (validation == ValidationType.DTD ? DtdProcessing.Parse : DtdProcessing.Ignore),
                ValidationFlags = (validation == ValidationType.Schema ? (System.Xml.Schema.XmlSchemaValidationFlags.ProcessInlineSchema | System.Xml.Schema.XmlSchemaValidationFlags.ProcessSchemaLocation | System.Xml.Schema.XmlSchemaValidationFlags.ProcessIdentityConstraints) : System.Xml.Schema.XmlSchemaValidationFlags.None),
                XmlResolver = new XmlUrlResolver()
            };            

            var messages = new StringBuilder();
            settings.ValidationEventHandler += (sender, args) => messages.AppendLine(args.Message);
            Comments = new List<string>();

            var reader = XmlReader.Create(Path.Combine(path, file), settings);                         
            try{                                
                while (reader.Read()){
                    switch (reader.NodeType)
                    {                        
                        case XmlNodeType.Comment:
                            if(reader.HasValue) Comments.Add(reader.Value);
                            break;
                    }                
                }
                if (messages.Length > 0) throw new DocumentInvalidException($"Unable to parse the XML file: {messages.ToString()}");    

                XmlDoc = new XmlDocument();
                XmlDoc.Load(reader);
            }
            catch(XmlException ex){
                throw new DocumentInvalidException("Unable to parse the XML file.", ex);     
            }            
        }
        
        /// <summary>
        /// Disposes the object releasing its unmanaged properties.
        /// </summary>
        public override void Dispose(){
        }  

        /// <summary>
        /// Requests for a set of nodes.
        /// </summary>
        /// <param name="xpath">XPath expression.</param>
        /// <returns>A list of nodes.</returns>
        public List<XmlNode> SelectNodes(string xpath){
            return SelectNodes(XmlDoc.FirstChild, xpath);
        }
        
        /// <summary>
        /// Requests for a set of nodes.
        /// </summary>
        /// <param name="root">Root node from where the XPath expression will be evaluated.</param>
        /// <param name="xpath">XPath expression.</param>
        /// <returns>A list of nodes.</returns>
        public List<XmlNode> SelectNodes(XmlNode root, string xpath){
            if(root == null) return null;
            else{
                var list = new List<XmlNode>();                
                foreach(XmlNode node in root.SelectNodes(xpath))
                    list.Add(node);

                return list.Count == 0 ? null : list;
            } 
        } 

        /// <summary>
        /// Count how many nodes of this kind are within the document.
        /// </summary>
        /// <param name="xpath">XPath expression.</param>
        /// <returns>Amount of nodes.</returns>
        public int CountNodes(string xpath){
            return CountNodes(XmlDoc.FirstChild, xpath);
        }
        
        /// <summary>
        /// Count how many nodes of this kind are within the document, ideal for count groups of radio buttons or checkboxes.
        /// </summary>
        /// <param name="xpath">XPath expression.</param>
        /// <param name="root">Root node from where the XPath expression will be evaluated.</param>
        /// <returns>Amount of nodes.</returns>
        public int CountNodes(XmlNode root, string xpath){
            if(root == null) return 0;
            else{
                var nodes = root.SelectNodes(xpath);
                return (nodes == null ? 0 : nodes.Count);
            }            
        }    

        /// <summary>
        /// Get the XML file comments.
        /// </summary>        
        /// <returns>A set of comments.</returns>
        public string[] GetComments(){
            return Comments.ToArray();
        }       

        /// <summary>
        /// Checks if the XML file contains any comments.
        /// </summary>        
        /// <returns>True if the XML file contains any comments.</returns>
        public bool HasComments(){
            return GetComments().Length > 0;        
        } 
    }
}