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
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace AutoCheck.Core.Connectors{       
    /// <summary>
    /// Allows in/out operations and/or data validations with plaint text files.
    /// </summary>
    public class PlainText: Base{        
        /// <summary>
        /// Contains a PlainText document content.
        /// </summary>
        public class PlainTextDocument{            
            private string[] LineContent {get; set;}            

            /// <summary>
            /// Document's content
            /// </summary>
            public string Content {
                get {
                    return string.Join("\r\n", LineContent);
                }                
            }

            /// <summary>
            /// Document's amount of lines
            /// </summary>
            public int Lines {
                get {
                    return LineContent.Length;
                }
            }

            /// <summary>
            /// Creates a new PlaintText Document instance, parsing an existing PlainText file.
            /// </summary>
            /// <param name="file">PlainText file path.</param>
            public PlainTextDocument(string file){ 
                file = Utils.PathToCurrentOS(file); 

                if(string.IsNullOrEmpty(file)) throw new ArgumentNullException("file");
                else LineContent = File.ReadAllLines(file);                               
            }

            /// <summary>
            /// Returns a line
            /// </summary>
            /// <param name="index">Index of the line that must be retrieved (from 1 to N).</param>
            /// <returns></returns>
            public string GetLine(int index){
                if(index < 0 || index >= LineContent.Length) throw new ArgumentOutOfRangeException("index");
                return LineContent[index];
            }               
        }         
        /// <summary>
        /// The plain text document content.
        /// </summary>
        /// <value></value>
        public PlainTextDocument plainTextDoc {get; private set;}       
        
        /// <summary>
        /// Creates a new connector instance.
        /// </summary>
        /// <param name="folder">The folder containing the files.</param>
        /// <param name="file">CSV file name.</param>
        /// <param name="fieldDelimiter">Field delimiter char.</param>
        /// <param name="textDelimiter">Text delimiter char.</param>
        public PlainText(string folder, string file){
            folder = Utils.PathToCurrentOS(folder); 

            if(string.IsNullOrEmpty(folder)) throw new ArgumentNullException("path");
            if(string.IsNullOrEmpty(file)) throw new ArgumentNullException("file");
            if(!Directory.Exists(folder)) throw new DirectoryNotFoundException();
                        
            plainTextDoc = new PlainTextDocument(Path.Combine(folder, file));         
        }
        
        /// <summary>
        /// Disposes the object releasing its unmanaged properties.
        /// </summary>
        public override void Dispose(){
        }  

        /// <summary>
        /// Gets the matches found within the document content using a regular expression.
        /// </summary>
        /// <param name="regex">The regular expression which will be used to search the content.</param>
        /// <returns>A set of matches.</returns>
        public string[] Find(string regex){
            var found = new List<string>();
            foreach(Match match in Regex.Matches(plainTextDoc.Content, regex)){
                found.Add(match.Value);
            }            

            return found.ToArray();
        }

        /// <summary>
        /// Gets how many matches can be found within the document content using a regular expression.
        /// </summary>
        /// <param name="regex">The regular expression which will be used to search the content.</param>
        /// <returns>The number of matches.</returns>
        public int Count(string regex){
            return Find(regex).Length;
        }
    }
}