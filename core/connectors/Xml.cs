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
using System.Linq;
using System.Collections.Generic;
using AutoCheck.Core.Exceptions;

namespace AutoCheck.Core.Connectors{       
    /// <summary>
    /// Allows in/out operations and/or data validations with XML files.
    /// </summary>
    public class Xml: Base{     
        public enum XmlNodeType {
            ALL, 
            STRING,
            BOOLEAN,
            NUMERIC
        }

        public string[] Comments {get; private set;}

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
            
            var coms = new List<string>();
            var filepath = Path.Combine(path, file);            
            var reader = XmlReader.Create(filepath, settings);                         
            try{                                
                while (reader.Read()){
                    switch (reader.NodeType)
                    {                        
                        case System.Xml.XmlNodeType.Comment:
                            if(reader.HasValue) coms.Add(reader.Value);
                            break;
                    }                
                }
                
                if (messages.Length > 0) throw new DocumentInvalidException($"Unable to parse the XML file: {messages.ToString()}");    
                
                Comments = coms.ToArray();
                XmlDoc = new XmlDocument();
                XmlDoc.Load(filepath);
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
        public List<XmlNode> SelectNodes(string xpath, XmlNodeType type = XmlNodeType.ALL){            
            return  SelectNodes((XmlNode)XmlDoc, xpath, type);
        }            

        /// <summary>
        /// Requests for a set of nodes.
        /// </summary>
        /// <param name="root">Root node from where the XPath expression will be evaluated.</param>
        /// <param name="xpath">XPath expression.</param>
        /// <returns>A list of nodes.</returns>
        public List<XmlNode> SelectNodes(XmlNode root, string xpath, XmlNodeType type = XmlNodeType.ALL){
            if(root == null) return null;
            else{                
                var set = root.SelectNodes(xpath);

                if(set == null) return null;
                else{
                    var list = set.Cast<XmlNode>().ToList();

                    if(type == XmlNodeType.ALL) return list;
                    else{
                        double d;
                        bool b;
                        var match = new List<XmlNode>();                        

                        foreach(var node in list){   
                            //TODO: root node is being added when requesting for string. check why!
                            var child = (node.HasChildNodes ? node.ChildNodes.Cast<XmlNode>().Where(x => x.NodeType == System.Xml.XmlNodeType.Text).ToArray() : null);
                            var value = (child != null && child.Length == 1 ? child[0].Value : null);
                            
                            var isNum = double.TryParse(value, out d);
                            var isBool = bool.TryParse(value, out b);

                            if(type == XmlNodeType.NUMERIC && isNum) match.Add(node);
                            else if(type == XmlNodeType.BOOLEAN && isBool) match.Add(node);
                            else if(type == XmlNodeType.STRING && !isNum && !isBool) match.Add(node);
                        }

                        return match;
                    }
                }                
            } 
        }

        /// <summary>
        /// Requests for the amount of nodes.
        /// </summary>
        /// <param name="xpath">XPath expression.</param>
        /// <returns>The amount of nodes.</returns>
        public int CountNodes(string xpath, XmlNodeType type = XmlNodeType.ALL){            
            return  CountNodes((XmlNode)XmlDoc, xpath, type);
        }
        
        /// <summary>
        /// Requests for the amount of nodes.
        /// </summary>
        /// <param name="root">Root node from where the XPath expression will be evaluated.</param>
        /// <param name="xpath">XPath expression.</param>
        /// <returns>The amount of nodes.</returns>
        public int CountNodes(XmlNode root, string xpath, XmlNodeType type = XmlNodeType.ALL){
            return SelectNodes(root, xpath, type).Count;
        }
    }
}