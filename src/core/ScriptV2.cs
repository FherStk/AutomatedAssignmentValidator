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
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;
using AutoCheck.Exceptions;


namespace AutoCheck.Core{
    //TODO: This will be the new Script (without V2)
    public class ScriptV2{
#region Attributes
        public string ScriptName {
            get{
                return Vars["script_name"].ToString();
            }

            private set{
                UpdateVar("script_name", value);                
            }
        }

        public string CurrentFolder {
            get{
                return Vars["current_folder"].ToString();
            }

            private set{
                UpdateVar("current_folder", value);               
            }
        }

        public string CurrentFile {
            get{
                return Vars["current_file"].ToString();
            }

            private set{
                UpdateVar("current_file", value);                
            }
        }

        public Dictionary<string, object> Vars {get; private set;}

        public Stack<Dictionary<string, object>> Checkers {get; private set;}  //Checkers and Connectors are the same within a YAML script, each of them in their scope

        private void UpdateVar(string key, object value){
            if(Vars.ContainsKey(key)) Vars.Remove(key);
            if(value != null) Vars.Add(key, value);
        }
#endregion
#region Constructor
        /// <summary>
        /// Creates a new script instance using the given script file.
        /// </summary>
        /// <param name="path">Path to the script file (yaml).</param>
        public ScriptV2(string path){
            if(!File.Exists(path)) throw new FileNotFoundException(path);
            
            Vars = new Dictionary<string, object>();
            Checkers = new Stack<Dictionary<string, object>>();
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
                        
            var root = (YamlMappingNode)yaml.Documents[0].RootNode;
            ValidateEntries(root, "root", new string[]{"name", "folder", "inherits", "vars", "pre", "post", "body"});

            Vars.Add("script_name", (root.Children.ContainsKey("name") ? root.Children["name"].ToString() : Regex.Replace(Path.GetFileNameWithoutExtension(path), "[A-Z]", " $0")));
            Vars.Add("current_folder", (root.Children.ContainsKey("folder") ? root.Children["folder"].ToString() : AppContext.BaseDirectory));
            
            ParseVars(root);
            ParsePre(root);
            ParseBody(root);                                    
        }
        
        private void ParseVars(YamlMappingNode root, string node="vars"){
            if(root.Children.ContainsKey(node)){
                root = (YamlMappingNode)root.Children[new YamlScalarNode(node)];

                foreach (var item in root.Children){
                    var name = item.Key.ToString();
                    object value = item.Value.ToString();

                    var reserved = new string[]{"script_name", "current_folder", "now"};
                    if(reserved.Contains(name)) throw new VariableInvalidException($"The variable name {name} is reserved and cannot be declared.");
                    
                    value = ComputeTypeValue(item.Value.Tag, item.Value.ToString());
                    if(value.GetType() == typeof(string)) value = ComputeVarValue(item.Key.ToString(), value.ToString());

                    if(Vars.ContainsKey(name)) throw new VariableInvalidException($"Repeated variables defined with name '{name}'.");
                    else Vars.Add(name, value);
                }
            } 
        }  

        private void ParsePre(YamlMappingNode root, string node="pre"){
            ForEach(root, node, new string[]{"extract", "restore_db", "upload_gdrive"}, new Action<string, YamlMappingNode>((name, node) => {
                switch(name){
                    case "extract":
                        ValidateEntries(node, name, new string[]{"file", "remove", "recursive"});  

                        var ex_file =  (node.Children.ContainsKey("file") ? node.Children["file"].ToString() : "*.zip");
                        var ex_remove =  (node.Children.ContainsKey("remove") ? bool.Parse(node.Children["remove"].ToString()) : false);
                        var ex_recursive =  (node.Children.ContainsKey("recursive") ? bool.Parse(node.Children["recursive"].ToString()) : false);
                                                   
                        Extract(ex_file, ex_remove,  ex_recursive);                        
                        break;

                    case "restore_db":
                        ValidateEntries(node, name, new string[]{"file", "db_host", "db_user", "db_pass", "db_name", "override", "remove", "recursive"});     

                        var db_file =  (node.Children.ContainsKey("file") ? node.Children["file"].ToString() : "*.sql");
                        var db_host =  (node.Children.ContainsKey("db_host") ? node.Children["db_host"].ToString() : "localhost");
                        var db_user =  (node.Children.ContainsKey("db_user") ? node.Children["db_user"].ToString() : "postgres");
                        var db_pass =  (node.Children.ContainsKey("db_pass") ? node.Children["db_pass"].ToString() : "postgres");
                        var db_name =  (node.Children.ContainsKey("db_name") ? node.Children["db_name"].ToString() : Vars["script_name"].ToString());
                        var db_override =  (node.Children.ContainsKey("override") ? bool.Parse(node.Children["override"].ToString()) : false);
                        var db_remove =  (node.Children.ContainsKey("remove") ? bool.Parse(node.Children["remove"].ToString()) : false);
                        var db_recursive =  (node.Children.ContainsKey("recursive") ? bool.Parse(node.Children["recursive"].ToString()) : false);

                        RestoreDB(db_file, db_host,  db_user, db_pass, db_name, db_override, db_remove, db_recursive);
                        break;

                    case "upload_gdrive":
                        ValidateEntries(node, name, new string[]{"source", "username", "secret", "remote_path", "link", "copy", "remove", "recursive"});     

                        var gd_source =  (node.Children.ContainsKey("source") ? node.Children["source"].ToString() : "*");
                        var gd_user =  (node.Children.ContainsKey("username") ? node.Children["username"].ToString() : "");
                        var gd_secret =  (node.Children.ContainsKey("secret") ? node.Children["secret"].ToString() : AutoCheck.Core.Utils.ConfigFile("gdrive_secret.json"));
                        var gd_remote =  (node.Children.ContainsKey("remote_path") ? node.Children["remote_path"].ToString() : "\\AutoCheck\\scripts\\{$SCRIPT_NAME}\\");
                        var gd_link =  (node.Children.ContainsKey("link") ? bool.Parse(node.Children["link"].ToString()) : false);
                        var gd_copy =  (node.Children.ContainsKey("copy") ? bool.Parse(node.Children["copy"].ToString()) : true);
                        var gd_remove =  (node.Children.ContainsKey("remove") ? bool.Parse(node.Children["remove"].ToString()) : false);
                        var gd_recursive =  (node.Children.ContainsKey("recursive") ? bool.Parse(node.Children["recursive"].ToString()) : false);

                        if(string.IsNullOrEmpty(gd_user)) throw new ArgumentInvalidException("The 'username' argument must be provided when using the 'upload_gdrive' feature.");                        
                        UploadGDrive(gd_source, gd_user, gd_secret, gd_remote, gd_link, gd_copy, gd_remove, gd_recursive);
                        break;
                } 
            }));
        }    
        
        private void ParsePost(YamlMappingNode root){
            //Maybe something diferent will be done in a near future? Who knows... :p
            ParsePre(root, "post");
        }

        private void ParseBody(YamlMappingNode root, string node="body"){
            Checkers.Push(new Dictionary<string, object>());

            ForEach(root, node, new string[]{"connector", "run", "question"}, new Action<string, YamlMappingNode>((name, node) => {
                switch(name){
                    case "connector":
                        ParseConnector(node);                            
                        break;

                    case "run":
                        ParseRun(node);
                        break;

                    case "question":
                        //ParseConnector (current);
                        break;
                } 
            }));
        }

        private void ParseConnector(YamlMappingNode root){
            //Validation before continuing
            ValidateEntries(root, "connector", new string[]{"type", "name", "arguments"});     

            var type =  (root.Children.ContainsKey("type") ? root.Children["type"].ToString() : "LOCALSHELL");
            var name =  (root.Children.ContainsKey("name") ? root.Children["name"].ToString() : type);        
                    
            //Compute the loaded connector arguments (typed or not) and store them as variables, allowing requests within the script
            var arguments = ParseArguments(root);
            foreach(var key in arguments.Keys.ToList()){                
                if(arguments[key].GetType().Equals(typeof(string)))
                    arguments[key] = ComputeVarValue(key, arguments[key].ToString());

                Vars.Add($"{name}.{key}", arguments[key]); 
            }
           
            //Getting the connector's assembly (unable to use name + baseType due checker's dynamic connector type)
            Assembly assembly = Assembly.GetExecutingAssembly();
            var assemblyType = assembly.GetTypes().First(t => t.FullName.Equals($"AutoCheck.Checkers.{type}", StringComparison.InvariantCultureIgnoreCase));
            var constructor = GetMethodInfo(assemblyType, assemblyType.Name, arguments);            
            Checkers.Peek().Add(name, Activator.CreateInstance(assemblyType, constructor.args));   
        }        

        private void ParseRun(YamlMappingNode root, string node="body"){
            //Validation before continuing
            var validation = new List<string>(){"connector", "command", "arguments"};
            if(!node.Equals("body")) validation.AddRange(new string[]{"expected", "success", "error"});
            ValidateEntries(root, "run", validation.ToArray());     

            //Loading command data
            var name =  (root.Children.ContainsKey("connector") ? root.Children["connector"].ToString() : "LOCALSHELL");  
            var checker = GetChecker(name);            
            var command =  (root.Children.ContainsKey("command") ? root.Children["command"].ToString() : string.Empty);

            //Binding with an existing connector command
            if(string.IsNullOrEmpty(command)) throw new ArgumentNullException("A 'command' argument must be specified within 'run'.");  
            
            var arguments = ParseArguments(root);
            if(checker.GetType().Equals(typeof(Checkers.LocalShell)) || checker.GetType().BaseType.Equals(typeof(Checkers.LocalShell))){                 
                if(!arguments.ContainsKey("path")) arguments.Add("path", string.Empty); 
                arguments.Add("command", command);
                command = "RunCommand";
            }
            var data = GetMethodInfo(checker.GetType(), command, arguments);

            //Loading expected execution behaviour
            var expected =  ComputeVarValue("expected", (root.Children.ContainsKey("expected") ? root.Children["expected"].ToString() : string.Empty));
            var success =  (root.Children.ContainsKey("success") ? root.Children["success"].ToString() : "OK");  
            var error =  (root.Children.ContainsKey("error") ? root.Children["error"].ToString() : "ERROR\n{$RESULT}");  
            
            //Running the command over the connector with the given arguments
            try{
                var result = data.method.Invoke((data.checker ? checker : checker.GetType().GetProperty("Connector").GetValue(checker)), data.args); 
                if(!Vars.ContainsKey("result")) Vars.Add("result", null);
                Vars["result"] = result.GetType().GetField("Item2").GetValue(result);
                
                if(!node.Equals("body")){
                    //TODO: 
                    //  1. comparisson type: regex for strings or SQL like (direct value means equals)                 
                    //  2. Compare the output with the expected one   
                    //  3. Print success or error.
                }                
            }
            catch(Exception ex){
                if(!node.Equals("body")){
                    Output.Instance.WriteResponse(ex.Message);
                }
            }
        }
        
        private Dictionary<string, object> ParseArguments(YamlMappingNode root){            
            var arguments =  new Dictionary<string, object>();

            //Load the connector argument list (typed or not)
            if(root.Children.ContainsKey("arguments")){
                if(root.Children["arguments"].GetType() == typeof(YamlScalarNode)){                    
                    foreach(var item in root.Children["arguments"].ToString().Split("--").Skip(1)){
                        var input = item.Trim(' ').Split(" ");
                        arguments.Add(input[0].TrimStart('-'), input[1]);                        
                    }
                }
                else{
                    ForEach(root, "arguments", new Action<string, YamlScalarNode>((name, node) => {
                        var value = ComputeTypeValue(node.Tag, node.Value);
                        arguments.Add(name, value);
                    }));
                } 
            }
            
            return arguments;
        }

        private object GetChecker(string name){
            /*
                Connector scope definition
                1. body
                    1.1 question
                        1.1.1 content
                        1.1.2 question (recursive)
            */            
            
            object chcker = null;
            var visited = new Stack<Dictionary<string, object>>();

            //Search the checker by name within scopes
            while(chcker == null && Checkers.Count > 0){
                if(Checkers.Peek().ContainsKey(name)) chcker = Checkers.Peek()[name];
                else visited.Push(Checkers.Pop());
            }

            //Undo scope search
            while(visited.Count > 0){
                Checkers.Push(visited.Pop());
            }

            if(chcker == null) chcker = new Checkers.LocalShell();
            return chcker;
        }

        private (MethodBase method, object[] args, bool checker) GetMethodInfo(Type type, string method, Dictionary<string, object> arguments, bool checker = true){
            //Getting the constructor parameters in order to bind them with the YAML script ones
            List<object> args = null;
            var constructor = method.Equals(type.Name);                        
            var methods = (constructor ? (MethodBase[])type.GetConstructors() : (MethodBase[])type.GetMethods());            

            foreach(var info in methods.Where(x => x.GetParameters().Count() == arguments.Count)){                                
                //Important: sorted from more to less amount of parameters
                args = new List<object>();
                foreach(var param in info.GetParameters()){
                    if(arguments.ContainsKey(param.Name) && arguments[param.Name].GetType() == param.ParameterType) args.Add(arguments[param.Name]);
                    else{
                        args = null;
                        break;
                    } 
                }

                //Not null means that all the constructor parameters has been succesfully binded
                if(args != null) return (info, args.ToArray(), checker);
            }
            
            //If ends is because no bind has been found, look for the inner Checker's Connector instance.
            if(!checker) throw new ArgumentInvalidException($"Unable to find any {(constructor ? "constructor" : "method")} for the Connector '{type.Name}' that matches with the given set of arguments.");                                                
            else return GetMethodInfo(type.GetProperty("Connector").PropertyType, method, arguments, false);                     
        }

        private void ValidateEntries(YamlMappingNode root, string parent, string[] expected){
            foreach (var entry in root.Children)
            {                
                var current = entry.Key.ToString().ToLower();
                if(!expected.Contains(current)) throw new DocumentInvalidException($"Unexpected value '{current}' found within '{parent}'.");              
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

        private object ComputeTypeValue(string tag, string value){
            if(string.IsNullOrEmpty(tag)) return value;
            else{
                //Source: https://yaml.org/spec/1.2/spec.html#id2804923
                var type = tag.Split(':').LastOrDefault();
                return type switch
                {
                    "int"   => int.Parse(value, CultureInfo.InvariantCulture),
                    "float" => float.Parse(value, CultureInfo.InvariantCulture),
                    "bool"  => bool.Parse(value),
                    "str"   => value,                    
                    _       => throw new InvalidCastException($"Unable to cast the value '{value}' using the YAML tag '{tag}'."),
                };
            }            
        }

        private void ForEach<T>(YamlMappingNode root, string node, Action<string, T> action) where T: YamlNode{
            ForEach(root, node, null, new Action<string, T>((name, node) => {
                action.Invoke(name, (T)node);
            }));
        }

        private void ForEach<T>(YamlMappingNode root, string node, string[] expected, Action<string, T> action) where T: YamlNode{
            //TODO: loop through YAML nodes (like Pre and Body) and execute the given delegate for each children found.        
            if(root.Children.ContainsKey(node)){ 
                var tmp = root.Children[new YamlScalarNode(node)];
                var list = new List<YamlMappingNode>();

                if(tmp.GetType() == typeof(YamlSequenceNode)) list = ((YamlSequenceNode)tmp).Cast<YamlMappingNode>().ToList();
                else if(tmp.GetType() == typeof(YamlMappingNode)) list.Add((YamlMappingNode)tmp);
                else if(tmp.GetType() == typeof(YamlScalarNode)) return;    //no children to loop through

                //Loop through found items and childs
                foreach (var item in list)
                {
                    if(expected != null && expected.Length > 0) 
                        ValidateEntries(item, node, expected);

                    foreach (var child in item.Children){  
                        var name = child.Key.ToString();   
                        
                        try{
                            if(typeof(T) == typeof(YamlMappingNode)) action.Invoke(name, (T)item.Children[new YamlScalarNode(name)]);
                            else if(typeof(T) == typeof(YamlScalarNode)) action.Invoke(name, (T)child.Value);
                            else throw new InvalidCastException();
                        }
                        catch(InvalidCastException){
                            action.Invoke(name, (T)Activator.CreateInstance(typeof(T)));
                        }
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
                string[] files = Directory.GetFiles(CurrentFolder, file, (recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));                    
                if(files.Length == 0) Output.Instance.WriteLine("Done!");                    
                else{
                    foreach(string zip in files){                        
                        CurrentFile = Path.GetFileName(zip);

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

                        CurrentFile = null;
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
                string[] files = Directory.GetFiles(CurrentFolder, file, (recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));                    
                if(files.Length == 0) Output.Instance.WriteLine("Done!");                    
                else{
                    foreach(string sql in files){
                        CurrentFile =  Path.GetFileName(sql);

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

                        CurrentFile =  null;
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
        private void UploadGDrive(string source, string user, string secret, string remoteFolder, bool link, bool copy, bool remove, bool recursive){            
            Output.Instance.WriteLine("Uploading files to Google Drive: ");
            Output.Instance.Indent();

            //Option 1: Only files within a searchpath, recursive or not, will be uploaded into the same remote folder.
            //Option 2: Non-recursive folders within a searchpath, including its files, will be uploaded into the same remote folder.
            //Option 3: Recursive folders within a searchpath, including its files, will be uploaded into the remote folder, replicating the folder tree.
           
            try{     
                remoteFolder = ComputeVarValue("remoteFolder", remoteFolder.TrimEnd('\\'));
                using(var drive = new Connectors.GDrive(secret, user)){                        
                    if(string.IsNullOrEmpty(Path.GetExtension(source))) UploadGDriveFolder(drive, CurrentFolder, source, remoteFolder, link, copy, recursive, remove);
                    else{
                        var files = Directory.GetFiles(CurrentFolder, source, (recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
                        if(files.Length == 0) Output.Instance.WriteLine("Done!");         

                        foreach(var file in files)
                            UploadGDriveFile(drive, file, remoteFolder, link, copy, remove);
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
        
        private void UploadGDriveFile(Connectors.GDrive drive, string localFile, string remoteFolder, bool link, bool copy, bool remove){
            try{                            
                CurrentFile =  Path.GetFileName(localFile);

                Output.Instance.WriteLine($"Checking the local file ~{Path.GetFileName(localFile)}: ", ConsoleColor.DarkYellow);      
                Output.Instance.Indent();                

                var fileName = string.Empty;
                var filePath = string.Empty;                                
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
                filePath = Path.Combine(filePath, fileFolder);

                if(link){
                    var content = File.ReadAllText(localFile);
                    //Regex source: https://stackoverflow.com/a/6041965
                    foreach(Match match in Regex.Matches(content, "(http|ftp|https)://([\\w_-]+(?:(?:\\.[\\w_-]+)+))([\\w.,@?^=%&:/~+#-]*[\\w@?^=%&/~+#-])?")){
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
                                    local = Path.Combine(AppContext.BaseDirectory, "tmp");
                                    if(!Directory.Exists(local)) Directory.CreateDirectory(local);

                                    local = Path.Combine(local, uri.Segments.Last());
                                    client.DownloadFile(uri, local);
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

                if(remove){
                    Output.Instance.Write($"Removing the local file... ");
                    File.Delete(localFile);
                    Output.Instance.WriteResponse();       
                } 
            }
            catch (Exception ex){
                Output.Instance.WriteResponse(ex.Message);
            }
            finally{
                CurrentFile =  null;
            }
        }

        private void UploadGDriveFolder(Connectors.GDrive drive, string localPath, string localSource, string remoteFolder, bool link, bool copy, bool recursive, bool remove){           
            var oldFolder = CurrentFolder;

            try{                
                CurrentFolder =  localPath;

                var files = Directory.GetFiles(localPath, localSource, SearchOption.TopDirectoryOnly);
                var folders = (recursive ? Directory.GetDirectories(localPath, localSource, SearchOption.TopDirectoryOnly) : new string[]{});
                
                if(files.Length == 0 && folders.Length == 0) Output.Instance.WriteLine("Done!");                       
                else{
                    foreach(var file in files)
                        UploadGDriveFile(drive, file, remoteFolder, link, copy, remove);
                                    
                    if(recursive){
                        foreach(var folder in folders){
                            var folderName = Path.GetFileName(folder);
                            drive.CreateFolder(remoteFolder, folderName);
                            
                            UploadGDriveFolder(drive, folder, localSource, Path.Combine(remoteFolder, folderName), link, copy, recursive, remove);
                        }

                        if(remove){
                            //Only removes if recursive (otherwise not uploaded data could be deleted).
                            Output.Instance.Write($"Removing the local folder... ");
                            Directory.Delete(localPath);    //not-recursive delete request, should be empty, otherwise something went wrong!
                            Output.Instance.WriteResponse();       
                        } 
                    }
                }                               
            }
            catch (Exception e){
                Output.Instance.WriteResponse(string.Format("ERROR {0}", e.Message));
            }
            finally{    
                Output.Instance.UnIndent();
                CurrentFolder = oldFolder;
            }    
        }                
#endregion    
    }
}