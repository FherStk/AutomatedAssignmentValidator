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
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;
using AutoCheck.Exceptions;


namespace AutoCheck.Core{
    //TODO: This will be the new Script (without V2)
    public class ScriptV2{
#region Attributes
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
#endregion
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

            try{
                yaml.Load(new StringReader(File.ReadAllText(path)));
            }
            catch(Exception ex){
                throw new DocumentInvalidException("Unable to parse the YAML document, see inner exception for further details.", ex);
            }
                        
            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

            Vars.Add("script_name", (mapping.Children.ContainsKey("name") ? mapping.Children["name"].ToString() : Regex.Replace(Path.GetFileNameWithoutExtension(path).Replace("_", " "), "[A-Z]", " $0")));
            Vars.Add("current_folder", (mapping.Children.ContainsKey("folder") ? mapping.Children["folder"].ToString() : AppContext.BaseDirectory));
            
            if(mapping.Children.ContainsKey("vars")) ParseVars((YamlMappingNode)mapping.Children[new YamlScalarNode("vars")]);
            if(mapping.Children.ContainsKey("pre")) ParsePre((YamlSequenceNode)mapping.Children[new YamlScalarNode("pre")]);
            
            //Validation
            var expected = new string[]{"name", "folder", "inherits", "vars", "pre", "post", "body"};
            foreach (var entry in mapping.Children)
            {                
                var current = entry.Key.ToString().ToLower();
                if(!expected.Contains(current)) throw new DocumentInvalidException($"Unexpected value '{current}' found.");              
            }
        }
        
        private void ParseVars(YamlMappingNode root){
            foreach (var item in root.Children){
                var name = item.Key.ToString();
                var value = item.Value.ToString();

                var reserved = new string[]{"script_name", "current_folder", "now"};
                if(reserved.Contains(name)) throw new VariableInvalidException($"The variable name {name} is reserved and cannot be declared.");
                
                value = ComputeVarValue(item.Key.ToString(), item.Value.ToString());

                if(Vars.ContainsKey(name)) throw new VariableInvalidException($"Repeated variables defined with name '{name}'.");
                else Vars.Add(name, value);
            }
        }

        private string ComputeVarValue(string name, string value){
            foreach(Match match in Regex.Matches(value, "{(.*?)}")){
                var replace = match.Value.TrimStart('{').TrimEnd('}');                    
                
                if(replace.StartsWith("#") || replace.StartsWith("$")){                        
                    //Check if the regex is valid and/or also the referred var exists.
                    var regex = string.Empty;
                    if(replace.StartsWith("#")){
                        var error = $"The regex {replace} must start with '#' and end with a '$' followed by variable name.";
                        
                        if(!replace.Contains("$")) throw new RegexInvalidException(error);
                        regex = replace.Substring(1, replace.LastIndexOf("$")-1);
                        replace = replace.Substring(replace.LastIndexOf("$"));
                        if(string.IsNullOrEmpty(replace)) throw new RegexInvalidException(error);
                    }

                    replace = replace.TrimStart('$');
                    if(replace.Equals("NOW")) replace = DateTime.Now.ToString();
                    else if(!Vars.ContainsKey(replace.ToLower())) throw new VariableInvalidException($"Undefined variable {replace} has been requested within '{name}'.");                            

                    if(string.IsNullOrEmpty(regex)) replace = Vars[replace.ToLower()].ToString();
                    else {
                        try{
                            replace = Regex.Match(replace, regex).Value;
                        }
                        catch{
                            throw new RegexInvalidException($"Invalid regular expression defined inside the variable '{name}'.");
                        }
                    }
                }
                
                value = value.Replace(match.Value, replace);
            }
            
            return value;
        }
        
        private void ParsePre(YamlSequenceNode root){
            //Loop through because the order matters
            foreach (YamlMappingNode current in root)
            {
                foreach (var item in current.Children){  
                    var name = item.Key.ToString();   
                    
                    YamlMappingNode mapping;
                    try{
                        mapping = (YamlMappingNode)current.Children[new YamlScalarNode(name)];
                    }
                    catch{
                        mapping = new YamlMappingNode();
                    }
                    
                    switch(name){
                        case "extract":
                            var ex_file =  (mapping.Children.ContainsKey("file") ? mapping.Children["file"].ToString() : "*.zip");
                            var ex_remove =  (mapping.Children.ContainsKey("remove") ? bool.Parse(mapping.Children["remove"].ToString()) : false);
                            var ex_recursive =  (mapping.Children.ContainsKey("recursive") ? bool.Parse(mapping.Children["recursive"].ToString()) : false);
                            Extract(ex_file, ex_remove,  ex_recursive);                        
                            break;

                        case "restore_db":
                            var db_file =  (mapping.Children.ContainsKey("file") ? mapping.Children["file"].ToString() : "*.sql");
                            var db_host =  (mapping.Children.ContainsKey("db_host") ? mapping.Children["db_host"].ToString() : "localhost");
                            var db_user =  (mapping.Children.ContainsKey("db_user") ? mapping.Children["db_user"].ToString() : "postgres");
                            var db_pass =  (mapping.Children.ContainsKey("db_pass") ? mapping.Children["db_pass"].ToString() : "postgres");
                            var db_name =  (mapping.Children.ContainsKey("db_name") ? mapping.Children["db_name"].ToString() : Vars["script_name"].ToString());
                            var db_override =  (mapping.Children.ContainsKey("override") ? bool.Parse(mapping.Children["override"].ToString()) : false);
                            var db_remove =  (mapping.Children.ContainsKey("remove") ? bool.Parse(mapping.Children["remove"].ToString()) : false);
                            var db_recursive =  (mapping.Children.ContainsKey("recursive") ? bool.Parse(mapping.Children["recursive"].ToString()) : false);
                            RestoreDB(db_file, db_host,  db_user, db_pass, db_name, db_override, db_remove, db_recursive);
                            break;

                        case "upload_gdrive":
                            var gd_source =  (mapping.Children.ContainsKey("source") ? mapping.Children["source"].ToString() : "*");
                            var gd_user =  (mapping.Children.ContainsKey("username") ? mapping.Children["username"].ToString() : "");
                            var gd_secret =  (mapping.Children.ContainsKey("secret") ? mapping.Children["secret"].ToString() : "config\\secret.json");
                            var gd_remote =  (mapping.Children.ContainsKey("remote_path") ? mapping.Children["remote_path"].ToString() : "\\AutoCheck\\{$SCRIPT_NAME}\\");
                            var gd_recursive =  (mapping.Children.ContainsKey("recursive") ? bool.Parse(mapping.Children["recursive"].ToString()) : false);
                            var gd_copy =  (mapping.Children.ContainsKey("copy") ? bool.Parse(mapping.Children["copy"].ToString()) : true);
                            var gd_link =  (mapping.Children.ContainsKey("link") ? bool.Parse(mapping.Children["link"].ToString()) : false);

                            if(string.IsNullOrEmpty(gd_user)) throw new ArgumentInvalidException("The 'username' argument must be provided when using the 'upload_gdrive' feature.");
                            UploadGDrive(gd_source, gd_user, gd_secret, gd_remote, gd_link, gd_copy, gd_recursive);
                            break;

                        default:
                            throw new DocumentInvalidException($"Unexpected value '{name}' found.");
                    }                    
                }
            }
        }
#endregion
#region ZIP
        private void Extract(string file, bool remove, bool recursive){
            Output.Instance.WriteLine("Extracting files: ");
            Output.Instance.Indent();
           
            try{
                string[] files = Directory.GetFiles(Folder, file, (recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));                    
                if(files.Length == 0) Output.Instance.WriteLine("Done!");                    
                else{
                    foreach(string zip in files){
                        Vars.Add("file_name", Path.GetFileName(zip));

                        try{
                            Output.Instance.Write($"Extracting the file ~{zip}... ", ConsoleColor.DarkYellow);
                            Utils.ExtractFile(zip);
                            Output.Instance.WriteResponse();
                        }
                        catch(Exception e){
                            Output.Instance.WriteResponse($"ERROR {e.Message}");
                            continue;
                        }

                        if(remove){                        
                            try{
                                Output.Instance.Write($"Removing the file ~{zip}... ", ConsoleColor.DarkYellow);
                                File.Delete(zip);
                                Output.Instance.WriteResponse();
                                Output.Instance.BreakLine();
                            }
                            catch(Exception e){
                                Output.Instance.WriteResponse($"ERROR {e.Message}");
                                continue;
                            }  
                        }

                        if(Vars.ContainsKey("file_name")) Vars.Remove("file_name");
                    }                                                                  
                }                    
            }
            catch (Exception e){
                Output.Instance.WriteResponse(string.Format("ERROR {0}", e.Message));
            }
            finally{    
                Output.Instance.UnIndent();
                if(!remove) Output.Instance.BreakLine();
            }            
        }
#endregion
#region BBDD
        private void RestoreDB(string file, string dbhost, string dbuser, string dbpass, string dbname, bool @override, bool remove, bool recursive){
            Output.Instance.WriteLine("Restoring databases: ");
            Output.Instance.Indent();
           
            try{
                string[] files = Directory.GetFiles(Folder, file, (recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));                    
                if(files.Length == 0) Output.Instance.WriteLine("Done!");                    
                else{
                    foreach(string sql in files){
                        Vars.Add("file_name", Path.GetFileName(sql));

                        try{                            
                            //TODO: parse DB name to avoid forbidden chars.
                            var parsedDbName = Path.GetFileName(ComputeVarValue("dbname", dbname)).Replace(" ", "_").Replace(".", "_");
                            Output.Instance.WriteLine($"Checking the database ~{parsedDbName}: ", ConsoleColor.DarkYellow);      
                            Output.Instance.Indent();

                            using(var db = new Connectors.Postgres(dbhost, parsedDbName, dbuser, dbpass)){
                                if(!@override && db.ExistsDataBase()) Output.Instance.WriteLine("The database already exists, skipping!"); 
                                else{
                                    if(@override && db.ExistsDataBase()){                
                                        try{
                                            Output.Instance.Write("Dropping the existing database: "); 
                                            db.DropDataBase();
                                            Output.Instance.WriteResponse();
                                        }
                                        catch(Exception ex){
                                            Output.Instance.WriteResponse(ex.Message);
                                        } 
                                    } 

                                    try{
                                        Output.Instance.Write($"Restoring the database using the file {sql}... ", ConsoleColor.DarkYellow);
                                        db.CreateDataBase(sql);
                                        Output.Instance.WriteResponse();
                                    }
                                    catch(Exception ex){
                                        Output.Instance.WriteResponse(ex.Message);
                                    }
                                }
                            }
                        }
                        catch(Exception e){
                            Output.Instance.WriteResponse($"ERROR {e.Message}");
                            continue;
                        }

                        if(remove){                        
                            try{
                                Output.Instance.Write($"Removing the file ~{sql}... ", ConsoleColor.DarkYellow);
                                File.Delete(sql);
                                Output.Instance.WriteResponse();
                            }
                            catch(Exception e){
                                Output.Instance.WriteResponse($"ERROR {e.Message}");
                                continue;
                            }
                        }

                        if(Vars.ContainsKey("file_name")) Vars.Remove("file_name");
                        Output.Instance.UnIndent();
                        Output.Instance.BreakLine();
                    }                                                                  
                }                    
            }
            catch (Exception e){
                Output.Instance.WriteResponse(string.Format("ERROR {0}", e.Message));
            }
            finally{    
                Output.Instance.UnIndent();
            }    
        } 
#endregion
#region Google Drive
        private void UploadGDrive(string source, string user, string secret, string remoteFolder, bool link, bool copy, bool recursive){            
            Output.Instance.WriteLine("Uploading files to Google Drive: ");
            Output.Instance.Indent();
           
            try{      
                using(var drive = new Connectors.GDrive(secret, user)){                        
                    if(string.IsNullOrEmpty(Path.GetExtension(source))) UploadGDriveFolder(drive, Folder, source, remoteFolder, link, copy, recursive);
                    else UploadGDriveFolder(drive, Folder, source, remoteFolder, link, copy, recursive);                    
                }                                 
            }
            catch (Exception e){
                Output.Instance.WriteResponse(string.Format("ERROR {0}", e.Message));
            }
            finally{    
                Output.Instance.UnIndent();
            }    
        }
        
        private void UploadGDriveFile(Connectors.GDrive drive, string localFile, string remoteFolder, bool link, bool copy){
            Vars.Add("file_name", Path.GetFileName(localFile));

            try{                            
                Output.Instance.WriteLine($"Checking the file ~{Path.GetFileName(localFile)}: ", ConsoleColor.DarkYellow);      
                Output.Instance.Indent();                

                var fileName = string.Empty;
                var filePath = string.Empty;
                remoteFolder = ComputeVarValue("remoteFolder", remoteFolder);

                if(string.IsNullOrEmpty(Path.GetExtension(remoteFolder))) filePath = remoteFolder;
                else{
                    fileName = Path.GetFileName(remoteFolder);
                    filePath = Path.GetDirectoryName(remoteFolder);                        
                }                
                
                //Remote GDrive folder structure                    
                var fileFolder = Path.GetFileName(filePath);
                filePath = Path.GetDirectoryName(remoteFolder);     
                if(drive.GetFolder(filePath, fileFolder) == null){                
                    Output.Instance.Write($"Creating folder structure in ~'{remoteFolder}': ", ConsoleColor.Yellow); 
                    drive.CreateFolder(filePath, fileFolder);
                    Output.Instance.WriteResponse();
                } 

                if(link){
                    var content = File.ReadAllText(localFile);
                    foreach(Match match in Regex.Matches(content, "^((((H|h)(T|t)|(F|f))(T|t)(P|p)((S|s)?))\\://)?(www.|[a-zA-Z0-9].)[a-zA-Z0-9\\-\\.]+\\.[a-zA-Z]{2,6}(\\:[0-9]{1,5})*(/($|[a-zA-Z0-9\\.\\,\\;\\?\'\\\\+&amp;%\\$#\\=~_\\-]+))*$")){
                        var uri = new Uri(match.Value);

                        if(copy){
                            try{
                                Output.Instance.Write($"Copying the file from external Google Drive's account to the own one... ");
                                drive.CopyFile(uri, filePath, fileName);
                                Output.Instance.WriteResponse();
                            }
                            catch{
                                Output.Instance.WriteResponse(string.Empty);
                                copy = false;   //retry with download-reload method if fails
                            }
                        }

                        if(!copy){
                            //download and reupload       
                            Output.Instance.Write($"Downloading the file from external sources and uploading to the own Google Drive's account... ");

                            string local = string.Empty;
                            if(match.Value.Contains("drive.google.com")) local = drive.Download(uri, Path.Combine(AppContext.BaseDirectory, "tmp"));                                        
                            else{
                                using (var client = new WebClient())
                                {
                                    local = Path.Combine(AppContext.BaseDirectory, "tmp", uri.Segments.Last());
                                    client.DownloadFile(match.Value, local);
                                }
                            }
                            
                            drive.CreateFile(local, filePath, fileName);
                            File.Delete(local);
                            Output.Instance.WriteResponse();
                        }                                                       
                    }
                }
                else{
                    Output.Instance.Write($"Uploading the local file to the own Google Drive's account... ");
                    drive.CreateFile(localFile, filePath, fileName);
                    Output.Instance.WriteResponse();                        
                }
            }
            catch (Exception ex){
                Output.Instance.WriteResponse(ex.Message);
            }
        }

        private void UploadGDriveFolder(Connectors.GDrive drive, string localPath, string localSource, string remoteFolder, bool link, bool copy, bool recursive){           
            try{
                var files = Directory.GetFiles(localPath, localSource, SearchOption.TopDirectoryOnly);
                var folders = (recursive ? Directory.GetDirectories(localPath, localSource, SearchOption.TopDirectoryOnly) : new string[]{});
                
                if(files.Length == 0 && folders.Length == 0) Output.Instance.WriteLine("Done!");                       
                else{
                    foreach(var file in files)
                        UploadGDriveFile(drive, file, remoteFolder, link, copy);
                                    
                    if(recursive){
                        foreach(var folder in folders){
                            var folderName = Path.GetFileName(folder);
                            drive.CreateFolder(remoteFolder, folderName);

                            UploadGDriveFolder(drive, folder, localSource, Path.Combine(remoteFolder, folderName), link, copy, recursive);
                        }
                    }
                }                               
            }
            catch (Exception e){
                Output.Instance.WriteResponse(string.Format("ERROR {0}", e.Message));
            }
            finally{    
                Output.Instance.UnIndent();
            }    
        }                
#endregion    
    }
}