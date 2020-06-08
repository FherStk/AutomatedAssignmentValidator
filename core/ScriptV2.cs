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
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;
using AutoCheck.Exceptions;


namespace AutoCheck.Core{
    //TODO: This will be the new Script (without V2)
    public class ScriptV2{
        public string Name {
            get{
                return Vars["script_name"].ToString();
            }
        }

        public string Folder {
            get{
                return Vars["current_folder"].ToString();
            }
        }

        public Dictionary<string, object> Vars {get; private set;}

#region Constructor    
        /// <summary>
        /// Creates a new script instance using the given script file.
        /// </summary>
        /// <param name="path">Path to the script file (yaml).</param>
        public ScriptV2(string path){
            if(!File.Exists(path)) throw new FileNotFoundException(path);
            
            Vars = new Dictionary<string, object>();
            ParseScript(path);
        }
#endregion
#region Parsing   
        private void ParseScript(string path){            
            var yaml = new YamlStream();
            yaml.Load(new StringReader(File.ReadAllText(path)));
            
            var name = string.Empty;
            var folder = string.Empty;
            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
            foreach (var entry in mapping.Children)
            {
                var current = entry.Key.ToString().ToLower();
                switch(current){
                    case "name":
                        name = entry.Value.ToString();
                        break;

                    case "folder":
                        folder = entry.Value.ToString();
                        break;

                    case "vars":
                        ParseVars((YamlMappingNode)mapping.Children[new YamlScalarNode(current)]);
                        break;

                    case "pre":
                        ParsePre((YamlMappingNode)mapping.Children[new YamlScalarNode(current)]);
                        break;

                    default:
                        throw new DocumentInvalidException($"Unexpected value '{current}' found.");
                }                
            }

            //Defaults
            //name
            if(string.IsNullOrEmpty(name)) name = Regex.Replace(Path.GetFileNameWithoutExtension(path).Replace("_", " "), "[A-Z]", " $0");
            Vars.Add("script_name", name);
            
            //folder
            if(string.IsNullOrEmpty(folder)) folder = AppContext.BaseDirectory;
            Vars.Add("current_folder", folder);
        }
        
        private void ParseVars(YamlMappingNode root){
            foreach (var item in root.Children){
                var name = item.Key.ToString();
                var value = item.Value.ToString();

                foreach(Match match in Regex.Matches(value, "{(.*?)}")){
                    var replace = match.Value.TrimStart('{').TrimEnd('}');
                    
                    if(replace.StartsWith("#") || replace.StartsWith("$")){                        
                        //Check if the regex is valid and/or also the referred var exists.
                        var regex = string.Empty;
                        if(replace.StartsWith("#")){
                            regex = replace.Substring(1, replace.LastIndexOf("$")-1);
                            replace = replace.Substring(replace.LastIndexOf("$"));
                        }

                        replace = replace.TrimStart('$');
                        if(replace.Equals("NOW")) replace = DateTime.Now.ToString();
                        else if(!Vars.ContainsKey(replace.ToLower())) throw new InvalidDataException($"Unable to apply a regular expression over the undefined variable {replace} as requested within the variable '{name}'.");                            

                        if(string.IsNullOrEmpty(regex)) replace = Vars[replace.ToLower()].ToString();
                        else {
                            try{
                                replace = Regex.Match(replace, regex).Value;
                            }
                            catch{
                                throw new InvalidDataException($"Invalid regular expression defined inside the variable '{name}'.");
                            }
                        }
                    }
                    
                    value = value.Replace(match.Value, replace);
                }
                
                if(Vars.ContainsKey(name)) throw new InvalidDataException($"Repeated variables defined with name '{name}'.");
                else Vars.Add(name, value);
            }
        }
        
        private void ParsePre(YamlMappingNode root){
            foreach (var item in root.Children){
                var name = item.Key.ToString();
                var children = (YamlMappingNode)root.Children[new YamlScalarNode(name)];

                switch(name){
                    case "unzip":
                        UnZip(children[new YamlScalarNode("file")].ToString(), bool.Parse(children[new YamlScalarNode("remove")].ToString()),  bool.Parse(children[new YamlScalarNode("recursive")].ToString()));                        
                        break;

                    case "restore_db":
                        break;

                    case "upload_gdrive":
                        break;

                    default:
                        throw new DocumentInvalidException($"Unexpected value '{name}' found.");
                }

            }
        }
#endregion
#region ZIP        
        private void UnZip(string file, bool remove, bool recursive){
            Output.Instance.WriteLine("Unzipping files: ");
            Output.Instance.Indent();

            foreach(string f in Directory.EnumerateDirectories(Folder))
            {
                try{
                    Output.Instance.WriteLine(string.Format("Unzipping files for the student ~{0}: ", Utils.FolderNameToStudentName(f)), ConsoleColor.DarkYellow);
                    Output.Instance.Indent();

                    string[] files = Directory.GetFiles(f, file, (recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));                    
                    if(files.Length == 0) Output.Instance.WriteLine("Done!");                    
                    else{
                        foreach(string zip in files){
                            try{
                                Output.Instance.Write("Unzipping the zip file... ");
                                Utils.ExtractFile(zip);
                                Output.Instance.WriteResponse();
                            }
                            catch(Exception e){
                                Output.Instance.WriteResponse(string.Format("ERROR {0}", e.Message));                           
                                continue;
                            }

                            if(remove){                        
                                try{
                                    Output.Instance.Write("Removing the zip file... ");
                                    File.Delete(zip);
                                    Output.Instance.WriteResponse();
                                }
                                catch(Exception e){
                                    Output.Instance.WriteResponse(string.Format("ERROR {0}", e.Message));
                                    continue;
                                }  
                            }
                        }                                                                  
                    }                    
                }
                catch (Exception e){
                    Output.Instance.WriteResponse(string.Format("ERROR {0}", e.Message));
                }
                finally{    
                    Output.Instance.UnIndent();
                    Output.Instance.BreakLine();
                }
            }
            
            if(Directory.EnumerateDirectories(Folder).Count() == 0){
                Output.Instance.WriteLine("Done!");
                Output.Instance.BreakLine();
            } 
                
            Output.Instance.UnIndent();            
        }
#endregion
    }
}