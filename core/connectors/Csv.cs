/*
    Copyright © 2021 Fernando Porrino Serrano
    Third party software licenses can be found at /docs/credits/credits.md

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
using System.Linq;
using System.Collections.Generic;
using AutoCheck.Core.Exceptions;

namespace AutoCheck.Core.Connectors{   
    /// <summary>
    /// Contains a CSV document content (without data mappings, all the content will be a string).
    /// </summary>
    public class CsvDocument{
        //TODO: use some library...
        private char FielDelimiter {get; set;}

        private char TextDelimiter {get; set;}

        /// <summary>
        /// All the content, grouped by columns.
        /// </summary>
        /// <value></value>
        public Dictionary<string, List<string>> Content {get; private set;}
        
        /// <summary>
        /// Returns the header names
        /// </summary>
        /// <value></value>
        public string[] Headers {
            get{
                if(this.Content == null) return new string[]{};
                else return this.Content.Keys.ToArray();
            }
        }
        
        /// <summary>
        /// Return the amount of lines
        /// </summary>
        /// <value></value>
        public int Count {
            get{
                return this.Content.ElementAt(0).Value.Count;
            }
        }
        
        /// <summary>
        /// Creates a new CSV Document instance, parsing an existing CSV file.
        /// </summary>
        /// <param name="file">CSV file path.</param>
        /// <param name="fieldDelimiter">Field delimiter char.</param>
        /// <param name="textDelimiter">Text delimiter char.</param>
        public CsvDocument(string file, char fieldDelimiter=',', char textDelimiter='"'){
            this.FielDelimiter = fieldDelimiter;
            this.TextDelimiter = textDelimiter;

            file = Utils.PathToCurrentOS(file);             
            if(string.IsNullOrEmpty(file)) throw new ArgumentNullException("file");
            else{                
                string[] lines = File.ReadAllLines(file);
                if(lines.Length == 0) return;

                this.Content = SplitFields(lines[0]).ToDictionary(x => x, x=> new List<string>());
                foreach(string line in lines.Skip(1).Where(x => !string.IsNullOrEmpty(x))){
                    string[] items = SplitFields(line);

                    for(int i = 0; i < items.Length; i++){
                        string item = items[i];                       

                        if(item.StartsWith(this.TextDelimiter) && item.EndsWith(this.TextDelimiter)){
                            //Removing string delimiters
                            item = item.Trim(TextDelimiter);                            
                        }                        

                        this.Content[this.Content.Keys.ElementAt(i)].Add(item);
                    }
                }                  
            }              
        }

        /// <summary>
        /// Checks the amount of columns on each row, which must be equivalent between each other.
        /// </summary>
        public void Validate(){
            if(this.Content == null) return;

            var count = this.Content.Values.Select(x => x.Count()).ToList();
            if(count.Where(x => !x.Equals(count[0])).Count() > 0) throw new DocumentInvalidException();           
        }

        /// <summary>
        /// Returns a line
        /// </summary>
        /// <param name="index">Index of the line that must be retrieved (from 1 to N).</param>
        /// <returns></returns>
        public Dictionary<string, string> GetLine(int index){
            if(index < 0 || this.Content == null || index > this.Content.Values.FirstOrDefault().Count())
                throw new IndexOutOfRangeException();

            Dictionary<string, string> line = this.Content.Keys.ToDictionary(x => x);
            foreach(string key in this.Content.Keys)
            {
                try{
                    line[key] = this.Content[key][index-1];
                }
                catch{
                    line[key] = null;
                }                
            }
                
            return line;
        } 
        
        private string[] SplitFields(string line){
            //TODO: parse also the data types
            List<string> fields = new List<string>();

            bool text = false;
            string current = string.Empty;
            foreach(char c in line.ToCharArray()){
                if(c.Equals(this.TextDelimiter)) text = !text;
                else if(c.Equals(this.FielDelimiter) && !text){
                    fields.Add(current);
                    current = string.Empty;
                }
                else current += c;
            }

            fields.Add(current);
            return fields.ToArray();
        }      
    }

    /// <summary>
    /// Allows in/out operations and/or data validations with CSV files.
    /// </summary>
    public class Csv: Base{         
        /// <summary>
        /// The CSV document content.
        /// </summary>
        /// <value></value>
        public CsvDocument CsvDoc {get; private set;}       
        
        /// <summary>
        /// Creates a new connector instance.
        /// </summary>
        /// <param name="filePath">CSV file path.</param>
        /// <param name="fieldDelimiter">Field delimiter char.</param>
        /// <param name="textDelimiter">Text delimiter char.</param>
        public Csv(string filePath, char fieldDelimiter=',', char textDelimiter='"'){
            if(string.IsNullOrEmpty(filePath)) throw new ArgumentNullException("filePath");
            if(!File.Exists(filePath)) throw new FileNotFoundException();

            this.CsvDoc = new CsvDocument(filePath, fieldDelimiter, textDelimiter);
            this.CsvDoc.Validate();
        }
        
        /// <summary>
        /// Disposes the object releasing its unmanaged properties.
        /// </summary>
        public override void Dispose(){
        }             
    }
}