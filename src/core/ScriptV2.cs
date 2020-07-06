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
using System.Text;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.RepresentationModel;
using AutoCheck.Exceptions;


namespace AutoCheck.Core{
    //TODO: This will be the new Script (without V2)
    public class ScriptV2{
#region Vars
        /// <summary>
        /// The current script name defined within the YAML file, otherwise the YAML file name.
        /// </summary>
        public string ScriptName {
            get{
                return GetVar("script_name").ToString();
            }

            private set{
                UpdateVar("script_name", value);                
            }
        }

        /// <summary>
        /// The current script caption defined within the YAML file.
        /// </summary>
        public string ScriptCaption {
            get{
                return GetVar("script_caption").ToString();
            }

            private set{
                UpdateVar("script_caption", value);                
            }
        }

        /// <summary>
        /// The current script execution folder defined within the YAML file, otherwise the YAML file's folder.
        /// </summary>
        public string ExecutionFolder {
            get{
                return GetVar("execution_folder").ToString();
            }

            private set{
                UpdateVar("execution_folder", value);               
            }
        }
        
        /// <summary>
        /// The current script IP for single-typed scripts (the same as "îp"); can change during the execution for batch-typed scripts.
        /// </summary>
        public string CurrentIP {
            get{
                return GetVar("current_ip").ToString();
            }

            private set{
                UpdateVar("current_ip", value);               
            }
        }

        /// <summary>
        /// The current script folder for single-typed scripts (the same as "folder"); can change during the execution for batch-typed scripts with the folder used to extract, restore a database, etc.
        /// </summary>
        public string CurrentFolder {
            get{
                return GetVar("current_folder").ToString();
            }

            private set{
                UpdateVar("current_folder", value);               
            }
        }

        /// <summary>
        /// The current script file for single-typed scripts; can change during the execution for batch-typed scripts with the file used to extract, restore a database, etc.
        /// </summary>
        public string CurrentFile {
            get{
                return GetVar("current_file").ToString();
            }

            private set{
                UpdateVar("current_file", value);                
            }
        }

        /// <summary>
        /// The current question (and subquestion) number (1, 2, 2.1, etc.)
        /// </summary>
        public string CurrentQuestion {
            get{
                return GetVar("current_question").ToString();
            }

            private set{
                UpdateVar("current_question", value);                
            }
        }
        
        /// <summary>
        /// Last executed command's result.
        /// </summary>
        /// <value></value>
        public string Result {
            get{ 
                var res = GetVar("result");
                return res == null ? null : res.ToString();
            }

            private set{
                UpdateVar("result", value);                
            }
        }

        /// <summary>
        /// The current datetime.  
        /// </summary>
        public string Now {
            get{
                return DateTime.Now.ToString();
            }
        }

        /// <summary>
        /// The current question (and subquestion) score
        /// </summary>
        public float CurrentScore {
            get{
                return (float)GetVar("current_score");
            }

            private set{
                UpdateVar("current_score", value);                
            }
        }

        /// <summary>
        /// Maximum score possible
        /// </summary>
        public float MaxScore {
            get{
                return (float)GetVar("max_score");
            }

            private set{
                UpdateVar("max_score", value);                
            }
        }
        
        /// <summary>
        /// The accumulated score (over 10 points), which will be updated on each CloseQuestion() call.
        /// </summary>
        public float TotalScore {
            get{
                return (float)GetVar("total_score");
            }

            private set{
                UpdateVar("total_score", value);                
            }
        }
#endregion
#region Attributes
        /// <summary>
        /// Output instance used to display messages.
        /// </summary>
        public OutputV2 Output {get; private set;}   

        private Stack<Dictionary<string, object>> Vars {get; set;}  //Variables are scope-delimited

        private Stack<Dictionary<string, object>> Checkers {get; set;}  //Checkers and Connectors are the same within a YAML script, each of them in their scope                       

        private float Success {get; set;}
        
        private float Fails {get; set;}         

        private List<string> Errors {get; set;}
#endregion
#region Constructor
        /// <summary>
        /// Creates a new script instance using the given script file.
        /// </summary>
        /// <param name="path">Path to the script file (yaml).</param>
        public ScriptV2(string path){            
            Output = new OutputV2();                                    
            Checkers = new Stack<Dictionary<string, object>>();          
            Vars = new Stack<Dictionary<string, object>>();

            var root = (YamlMappingNode)LoadYamlFile(path).Documents[0].RootNode;
            ValidateEntries(root, "root", new string[]{"inherits", "name", "caption", "ip", "folder", "batch", "vars", "pre", "post", "body"});

            //Scope in              
            Vars.Push(new Dictionary<string, object>());
            
            //Default vars
            Result = null;                                   
            MaxScore = 10f;
            TotalScore = 0f;
            CurrentScore = 0f;
            CurrentQuestion = "0";                        
            CurrentFile = Path.GetFileName(path);
            ExecutionFolder = AppContext.BaseDirectory; 
            CurrentFolder = ParseNode(root, "folder", Path.GetDirectoryName(path), false);
            CurrentIP = ParseNode(root, "ip", "localhost", false);
            ScriptName = ParseNode(root, "name", Regex.Replace(Path.GetFileNameWithoutExtension(path), "[A-Z]", " $0"), false);
            ScriptCaption = ParseNode(root, "caption", "Running script ~{$SCRIPT_NAME}:", false);

            //Custom vars
            ParseVars(root);
            
            //Pre, body and post must be run once for single-typed scripts or N times for batch-typed scripts            
            ParseBatch(root, new Action(() => {
                ParsePre(root);
                ParseBody(root);  
                ParsePost(root);  
            }));
            
            //Scope out
            Vars.Pop();
        }
#endregion
#region Parsing        
        private void ParseVars(YamlMappingNode node, string child="vars", string current="root"){     
            if(current.Equals("root")){
                node = ParseNode(node, child);
                if(node == null) return;
            } 
            
            foreach (var item in node.Children){
                var name = item.Key.ToString();                   
                var reserved = new string[]{"script_name", "execution_folder", "current_ip", "current_folder", "current_file", "result", "now"};

                if(reserved.Contains(name)) throw new VariableInvalidException($"The variable name {name} is reserved and cannot be declared.");                                    
                UpdateVar(name, ParseNode((YamlScalarNode)item.Value, false));
            }            
        }  
        
        private void ParsePre(YamlMappingNode node, string child="pre", string current="root"){
            ForEach(node, child, new string[]{"extract", "restore_db", "upload_gdrive"}, new Action<string, YamlMappingNode>((name, node) => {
                switch(name){
                    case "extract":
                        ValidateEntries(node, name, new string[]{"file", "remove", "recursive"});                                                                      
                        Extract(
                            ParseNode(node, "file", "*.zip", false), 
                            ParseNode(node, "remove", false, false),  
                            ParseNode(node, "recursive", false, false)
                        );                        
                        break;

                    case "restore_db":
                        ValidateEntries(node, name, new string[]{"file", "db_host", "db_user", "db_pass", "db_name", "override", "remove", "recursive"});     
                        RestoreDB(
                            ParseNode(node, "file", "*.sql", false), 
                            ParseNode(node, "db_host", "localhost", false),  
                            ParseNode(node, "db_user", "postgres", false), 
                            ParseNode(node, "db_pass", "postgres", false), 
                            ParseNode(node, "db_name", ScriptName, false), 
                            ParseNode(node, "override", false, false), 
                            ParseNode(node, "remove", false, false), 
                            ParseNode(node, "recursive", false, false)
                        );
                        break;

                    case "upload_gdrive":
                        ValidateEntries(node, name, new string[]{"source", "username", "secret", "remote_path", "link", "copy", "remove", "recursive"});     
                        UploadGDrive(
                            ParseNode(node, "source", "*", false), 
                            ParseNode(node, "username", "", false), 
                            ParseNode(node, "secret", AutoCheck.Core.Utils.ConfigFile("gdrive_secret.json"), false), 
                            ParseNode(node, "remote_path",  "\\AutoCheck\\scripts\\{$SCRIPT_NAME}\\", false), 
                            ParseNode(node, "link", false, false), 
                            ParseNode(node, "copy", true, false), 
                            ParseNode(node, "remove", false, false), 
                            ParseNode(node, "recursive", false, false)
                        );
                        break;
                } 
            }));
        }    
        
        private void ParsePost(YamlMappingNode node, string child="post", string current="root"){
            //Maybe something diferent will be done in a near future? Who knows... :p
            ParsePre(node, child, current);
        }
        
        private void ParseBatch(YamlMappingNode node, Action action, string child="batch", string current="root"){
            var batch = ParseNode(node, child);
            var originalFolder = CurrentFolder;
            var originalIP = CurrentIP;
                        
            if(batch == null) action.Invoke(); 
            else{ 
                ValidateEntries(batch, child, new string[]{"copy_detector", "target"});
                var target = ParseTarget(batch, "target", child);
                var cdetectors = ParseCopyDetector(batch, target.folders, "copy_detector", child);        

                //Running in batch mode
                foreach(var f in target.folders){
                    CurrentFolder = f;
                    new Action(() => {
                        Output.WriteLine(ComputeVarValue(ScriptCaption), ConsoleColor.Yellow);
                        Output.Indent();
                        
                        var match = false;
                        foreach(var cd in cdetectors){                            
                            if(cd != null){
                                match = match || cd.CopyDetected(f);                        
                                if(match) PrintCopies(cd, f);                            
                            }
                        }
                        if(!match) action.Invoke();    

                        Output.UnIndent();
                        Output.BreakLine();
                    }).Invoke();
                }           

                //Restore global data
                CurrentFolder = originalFolder;
                CurrentIP = originalIP;
            }            
        }

        private (string[] folders, string[] ips) ParseTarget(YamlMappingNode node, string child="target", string current="batch"){            
            var folders = new List<string>();
            ForEach(node, child, new string[]{"ip", "path", "folder"}, new Action<string, YamlScalarNode>((name, node) => { 
                switch(name){
                    case "ip":                            
                        //TODO: set of IPs (using mask) same as path but with IPs
                        throw new NotImplementedException();

                    case "folder":
                        folders.Add(ParseNode(node, CurrentFolder));                                                  
                        break;

                    case "path":                            
                        foreach(var folder in Directory.GetDirectories(ParseNode(node, CurrentFolder))) 
                            folders.Add(folder);                                                                                            
                        break;
                }
            }));

            return (folders.ToArray(), new string[]{});
        }

        private CopyDetectorV2[] ParseCopyDetector(YamlMappingNode node, string[] folders, string child="copy_detector", string current="batch"){            
            var cds = new List<CopyDetectorV2>();            

            ForEach(node, child, new string[]{"type", "caption", "threshold", "blocking"}, new Action<string, YamlMappingNode>((name, node) => {                 
                //TODO: if fails, try to use YamlScalarNode within foreach and adapt ParseNode to allow it
                var threshold = ParseNode(node, "threshold", 0f, false);
                var file = ParseNode(node, "file", "*", false);
                var caption = ParseNode(node, "caption", "Looking for potential copies within ~{#[^\\\\]+$$CURRENT_FOLDER}...", false);                    
                var type = ParseNode(node, "type", string.Empty);                                    
                if(string.IsNullOrEmpty(type)) throw new ArgumentNullException(type);

                cds.Add(LoadCopyDetector(type, caption, threshold, file, folders.ToArray()));
            }));    

            return cds.ToArray();
        }

        private void ParseBody(YamlMappingNode node, string child="body", string current="root"){
            //Scope in
            Vars.Push(new Dictionary<string, object>());
            Checkers.Push(new Dictionary<string, object>());

            //Parse the entire body
            current = child;
            ForEach(node, child, new string[]{"vars", "connector", "run", "question"}, new Action<string, YamlMappingNode>((name, node) => {                
                switch(name){
                    case "vars":
                        ParseVars(node, name, current);                            
                        break;

                    case "connector":
                        ParseConnector(node, name, current);                            
                        break;

                    case "run":
                        ParseRun(node, name, current);
                        break;

                    case "question":
                        ParseQuestion(node, name, current);
                        break;
                } 
            }));

            //Body ends, so total score can be displayed
            Output.Write("TOTAL SCORE: ", ConsoleColor.Cyan);
            Output.Write(Math.Round(TotalScore, 2).ToString(CultureInfo.InvariantCulture), (TotalScore < MaxScore/2 ? ConsoleColor.Red : ConsoleColor.Green));
            Output.BreakLine();

            //Scope out
            Vars.Pop();
            Checkers.Pop();
        }

        private void ParseConnector(YamlMappingNode node, string child="connector", string current="root"){
            //Validation before continuing
            ValidateEntries(node, child, new string[]{"type", "name", "arguments"});     

            //Loading connector data
            var type = ParseNode(node, "type", "LOCALSHELL");
            var name = ParseNode(node, "name", type);
                               
            //Getting the connector's assembly (unable to use name + baseType due checker's dynamic connector type)
            Assembly assembly = Assembly.GetExecutingAssembly();
            var assemblyType = assembly.GetTypes().First(t => t.FullName.Equals($"AutoCheck.Checkers.{type}", StringComparison.InvariantCultureIgnoreCase));
            var constructor = GetMethod(assemblyType, assemblyType.Name, ParseArguments(node));            
            Checkers.Peek().Add(name.ToLower(), Activator.CreateInstance(assemblyType, constructor.args));   
        }        
        
        private void ParseRun(YamlMappingNode node, string child="run", string current="body"){
            //Validation before continuing
            var validation = new List<string>(){"connector", "command", "arguments", "expected"};
            if(!current.Equals("body")) validation.AddRange(new string[]{"caption", "success", "error"});
            ValidateEntries(node, child, validation.ToArray());     
                                    
            try{
                //Running the command over the connector with the given arguments   
                var name = ParseNode(node, "connector", "LOCALSHELL");                   
                var data = InvokeCommand(
                    GetChecker(name),
                    ParseNode(node, "command", string.Empty),
                    ParseArguments(node)
                );
                
                //Storing the result into the global var
                if(data.shellExecuted) Result = ((ValueTuple<int, string>)data.result).Item2; 
                else if(data.checkerExecuted) Result = string.Join("\r\n", (List<string>)data.result);
                else Result = data.result.ToString();
                Result = Result.TrimEnd();  //Remove trailing breaklines...  

                //Matching the data
                var expected = ParseNode(node, "expected", (object)null);  
                var match = (expected == null ? true : MatchesExpected(Result, expected.ToString()));
                var info = $"Expected -> {expected}; Found -> {Result}";                

                //Displaying matching messages
                if(current.Equals("body") && !match) throw new ResultMismatchException(info);
                else if(!current.Equals("body")){  
                    var caption = ParseNode(node, "caption", string.Empty);
                    var success = ParseNode(node, "success", "OK");
                    var error = ParseNode(node, "error", "ERROR");
                    
                    if(Errors != null){
                        //A question has been opened, so an answer is needed.
                        if(string.IsNullOrEmpty(caption)) throw new ArgumentNullException("caption", new Exception("A 'caption' argument must be provided when running a 'command' using 'expected' within a 'quesion'."));
                        Output.Write($"{caption} ");
                        
                        List<string> errors = null;
                        if(!match){
                            if(data.shellExecuted || !data.checkerExecuted) errors = new List<string>(){info}; 
                            else errors = (List<string>)data.result;
                        }
                       
                        if(errors != null) Errors.AddRange(errors);
                        Output.WriteResponse(errors, success, error);                      
                    }                    
                }              
            }
            catch(ResultMismatchException ex){
                if(current.Equals("body")) throw;
                else Output.WriteResponse(ex.Message);
            }            
        }

        private void ParseQuestion(YamlMappingNode node, string child="question", string current="root"){
            //Validation before continuing
            var validation = new List<string>(){"score", "caption", "description", "content"};            
            ValidateEntries(node, child, validation.ToArray());     
                        
            if(Errors != null){
                //Opening a subquestion               
                CurrentQuestion += ".1";                
                Output.BreakLine();
            }
            else{
                //Opening a main question
                var parts = CurrentQuestion.Split('.');
                var last = int.Parse(parts.LastOrDefault());
                parts[parts.Length-1] = (last+1).ToString();
                CurrentQuestion = string.Join('.', parts);
            } 

            //Cleaning previous errors
            Errors = new List<string>();

            //Loading question data                        
            CurrentScore = ComputeQuestionScore(node);
            var caption = ParseNode(node, "caption", $"Question {CurrentQuestion} [{CurrentScore} {(CurrentScore > 1 ? "points" : "point")}]");
            var description = ParseNode(node, "description", string.Empty);  
            
            //Displaying question caption
            caption = (string.IsNullOrEmpty(description) ? $"{caption}:" : $"{caption} - {description}:");            
            Output.WriteLine(caption, ConsoleColor.Cyan);
            Output.Indent();                        

            //Parse and run question content
            ParseContent(node, "content", child);           

            //Compute scores
            float total = Success + Fails;
            TotalScore = (total > 0 ? (Success / total)*MaxScore : 0);      
            Errors = null;   

            //Closing the question                            
            Output.UnIndent();                                    
            Output.BreakLine();   
        }

        private void ParseContent(YamlMappingNode node, string child="content", string current="question"){
            //Scope in
            Vars.Push(new Dictionary<string, object>());
            Checkers.Push(new Dictionary<string, object>());

            //Subquestion detection
            var subquestion = false;
            ForEach(node, "content", new string[]{"connector", "run", "question"}, new Action<string, YamlMappingNode>((name, node) => {
                switch(name){                   
                    case "question":
                        subquestion = true;
                        return;                        
                } 
            }));
            
            //Recursive content processing
            current = child;
            ForEach(node, "content", new string[]{"connector", "run", "question"}, new Action<string, YamlMappingNode>((name, node) => {
                switch(name){
                    case "connector":
                        ParseConnector(node, name, current);                            
                        break;

                    case "run":
                        ParseRun(node, name, (subquestion ? "body" : "question"));
                        break;

                    case "question":                        
                        ParseQuestion(node, name, current);
                        break;
                } 
            }));

            //Processing score
            if(!subquestion){                
                if(Errors.Count == 0) Success += CurrentScore;
                else Fails += CurrentScore;
            } 

            //Scope out
            Vars.Pop();
            Checkers.Pop();  
        }

        private YamlMappingNode ParseNode(YamlMappingNode node, string child){
            return (node.Children.ContainsKey(child) ? (YamlMappingNode)node.Children[child] : null);
        }

        private T ParseNode<T>(YamlMappingNode node, string child, T @default, bool compute=true){           
            if(node.Children.ContainsKey(child)){
                var current = node.Children.Where(x => x.Key.ToString().Equals(child)).FirstOrDefault().Value;
                if(!current.GetType().Equals(typeof(YamlScalarNode))) throw new NotSupportedException("This method only supports YamlScalarNode child nodes.");
                return (T)ParseNode((YamlScalarNode)current,  @default, compute);                            
            } 
            else{
                if(@default == null || !@default.GetType().Equals(typeof(string))) return @default;
                else return (T)ParseNode(new YamlScalarNode(@default.ToString()), @default, compute); 
            }
        }
                 
        private T ParseNode<T>(YamlScalarNode node, T @default, bool compute=true){
            try{                                
                return (T)ParseNode(node, compute);                    
            }
            catch(InvalidCastException){
                return @default;
            }            
        }

        private object ParseNode(YamlScalarNode node, bool compute=true){    
            object  value = ComputeTypeValue(node.Tag, node.Value);

            if(value.GetType().Equals(typeof(string))){                
                //Always check if the computed value requested is correct, otherwise throws an exception
                var computed = ComputeVarValue(value.ToString());
                if(compute) value = computed;
            } 

            return value;
        }
                
        private Dictionary<string, object> ParseArguments(YamlMappingNode root){            
            var arguments =  new Dictionary<string, object>();

            //Load the connector argument list (typed or not)
            if(root.Children.ContainsKey("arguments")){
                if(root.Children["arguments"].GetType() == typeof(YamlScalarNode)){                    
                    foreach(var item in root.Children["arguments"].ToString().Split("--").Skip(1)){
                        var input = item.Trim(' ').Split(" ");   
                        var name = input[0].TrimStart('-');
                        var value = ComputeVarValue(name, input[1]);
                        arguments.Add(name, value);                        
                    }
                }
                else{
                    ForEach(root, "arguments", new Action<string, YamlScalarNode>((name, node) => {
                        var value = ComputeTypeValue(node.Tag, node.Value);
                        if(value.GetType().Equals(typeof(string))) value = ComputeVarValue(name, value.ToString());
                        arguments.Add(name, value);
                    }));
                } 
            }

            return arguments;
        }
#endregion
#region Helpers
        private (MethodBase method, object[] args, bool checker) GetMethod(Type type, string method, Dictionary<string, object> arguments, bool checker = true){            
            List<object> args = null;
            var constructor = method.Equals(type.Name);                        
            var methods = (constructor ? (MethodBase[])type.GetConstructors() : (MethodBase[])type.GetMethods());            

            //Getting the constructor parameters in order to bind them with the YAML script ones
            foreach(var info in methods.Where(x => x.Name.Equals((constructor ? ".ctor" : method), StringComparison.InvariantCultureIgnoreCase) && x.GetParameters().Count() == arguments.Count)){                                
                args = new List<object>();
                foreach(var param in info.GetParameters()){
                    if(arguments.ContainsKey(param.Name) && (arguments[param.Name].GetType() == param.ParameterType)) args.Add(arguments[param.Name]);
                    else if(arguments.ContainsKey(param.Name) && param.ParameterType.IsEnum && arguments[param.Name].GetType().Equals(typeof(string))) args.Add(Enum.Parse(param.ParameterType, arguments[param.Name].ToString()));
                    else{
                        args = null;
                        break;
                    } 
                }

                //Not null means that all the constructor parameters has been succesfully binded
                if(args != null) return (info, args.ToArray(), checker);
            }
            
            //If ends is because no bind has been found, look for the inner Checker's Connector instance (if possible).
            if(!checker) throw new ArgumentInvalidException($"Unable to find any {(constructor ? "constructor" : "method")} for the Connector '{type.Name}' that matches with the given set of arguments.");                                                
            else return GetMethod(GetConnectorProperty(type).PropertyType, method, arguments, false);                     
        }

        private PropertyInfo GetConnectorProperty(Type input){
            //Warning: due polimorfism, there's not only a single property called "Connector" within a Checker
            var conns = input.GetProperties().Where(x => x.Name.Equals("Connector")).ToList();
            if(conns.Count == 0) throw new PorpertyNotFoundException($"Unable to find the property 'Connector' for the given '{input.Name}'");

            //SingleOrDefault to rise exception if not unique (should never happen?)
            if(conns.Count() > 1) return conns.Where(x => x.DeclaringType.Equals(x.ReflectedType)).SingleOrDefault();  
            else return conns.FirstOrDefault();
        }
        
        private (object result, bool shellExecuted, bool checkerExecuted) InvokeCommand(object checker, string command, Dictionary<string, object> arguments){
            //Loading command data                        
            if(string.IsNullOrEmpty(command)) throw new ArgumentNullException("command", new Exception("A 'command' argument must be specified within 'run'."));  
            
            //Binding with an existing connector command
            var shellExecuted = false;                    

            (MethodBase method, object[] args, bool checker) data;
            try{
                //Regular bind (directly to the checker or its inner connector)
                data = GetMethod(checker.GetType(), command, arguments);                
            }
            catch(ArgumentInvalidException){       
                //If LocalShell (implicit or explicit) is being used, shell commands can be used directly as "command" attributes.
                shellExecuted = checker.GetType().Equals(typeof(Checkers.LocalShell)) || checker.GetType().BaseType.Equals(typeof(Checkers.LocalShell));  
                if(shellExecuted){                                     
                    if(!arguments.ContainsKey("path")) arguments.Add("path", string.Empty); 
                    arguments.Add("command", command);
                    command = "RunCommand";
                }
                
                //Retry the execution
                data = GetMethod(checker.GetType(), command, arguments);                
            }

            var result = data.method.Invoke((data.checker ? checker : GetConnectorProperty(checker.GetType()).GetValue(checker)), data.args);                                                 
            return (result, shellExecuted, result.GetType().Equals(typeof(List<string>)));
        }

        private void ValidateEntries(YamlMappingNode node, string current, string[] expected){
            foreach (var entry in node.Children)
            {                
                var child = entry.Key.ToString().ToLower();
                if(!expected.Contains(child)) throw new DocumentInvalidException($"Unexpected value '{child}' found within '{current}'.");              
            }
        }

        private string ComputeVarValue(string value){
            return ComputeVarValue(nameof(value), value);
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
                    else{                         
                        replace = string.Format(CultureInfo.InvariantCulture, "{0}", GetVar(replace.ToLower()));
                        if(!string.IsNullOrEmpty(regex)){
                            try{
                                replace = Regex.Match(replace, regex).Value;
                            }
                            catch (Exception ex){
                                throw new RegexInvalidException($"Invalid regular expression defined inside the variable '{name}'.", ex);
                            }
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

        private float ComputeQuestionScore(YamlMappingNode root){        
            var score = 0f;
            var subquestion = false;

            ForEach(root, "content", new string[]{"connector", "run", "question"}, new Action<string, YamlMappingNode>((name, node) => {
                switch(name){                   
                    case "question":
                        subquestion = true;
                        score += ComputeQuestionScore(node);
                        break;
                } 
            }));

            if(!subquestion) return ParseNode(root, "score", 1f, false);
            else return score;
        }

        private bool MatchesExpected(string current, string expected){
            var match = false;
            var comparer = Core.Operator.EQUALS;                        

            if(expected.StartsWith("<=")){ 
                comparer = Operator.LOWEREQUALS;
                expected = expected.Substring(2).Trim();
            }
            else if(expected.StartsWith(">=")){
                comparer = Operator.GREATEREQUALS;
                expected = expected.Substring(2).Trim();
            }
            else if(expected.StartsWith("<")){ 
                comparer = Operator.LOWER;
                expected = expected.Substring(1).Trim();
            }
            else if(expected.StartsWith(">")){
                comparer = Operator.GREATER;                        
                expected = expected.Substring(1).Trim();
            }
            else if(expected.StartsWith("LIKE")){
                comparer = Operator.LIKE;
                expected = expected.Substring(4).Trim();
            }
            else if(expected.StartsWith("%") || expected.EndsWith("%")){
                comparer = Operator.LIKE;
            }
            else if(expected.StartsWith("<>") || expected.StartsWith("!=")){ 
                comparer = Operator.NOTEQUALS;
                expected = expected.Substring(2).Trim();
            }

            if(comparer == Operator.LIKE){
                if(expected.StartsWith('%') && expected.EndsWith('%')){
                    expected = expected.Trim('%');
                    match = current.Contains(expected);
                }
                else if(expected.StartsWith('%')){
                    expected = expected.Trim('%');
                    match = current.EndsWith(expected);
                }
                else if(expected.EndsWith('%')){
                    expected = expected.Trim('%');
                    match = current.StartsWith(expected);
                }
            }
            else{
                match = comparer switch
                {
                    Operator.EQUALS => current.Equals(expected),
                    Operator.NOTEQUALS => !current.Equals(expected),
                    Operator.LOWEREQUALS => (float.Parse(current) <= float.Parse(expected)),
                    Operator.GREATEREQUALS => (float.Parse(current) >= float.Parse(expected)),
                    Operator.LOWER => (float.Parse(current) < float.Parse(expected)),
                    Operator.GREATER => (float.Parse(current) > float.Parse(expected)),
                    _ => throw new NotSupportedException()
                };
            }            
            
            return match;
        }

        private void ForEach<T>(YamlMappingNode root, string node, Action<string, T> action) where T: YamlNode{
            ForEach(root, node, null, new Action<string, T>((name, node) => {
                action.Invoke(name, (T)node);
            }));
        }

        private void ForEach<T>(YamlMappingNode root, string node, string[] expected, Action<string, T> action) where T: YamlNode{
            if(root.Children.ContainsKey(node)){ 
                var tmp = root.Children[new YamlScalarNode(node)];
                var list = new List<YamlMappingNode>();

                if(tmp.GetType().Equals(typeof(YamlSequenceNode))) list = ((YamlSequenceNode)tmp).Cast<YamlMappingNode>().ToList();
                else if(tmp.GetType().Equals(typeof(YamlMappingNode))) list.Add((YamlMappingNode)tmp);
                else if(tmp.GetType().Equals(typeof(YamlScalarNode))) return;    //no children to loop through

                //Loop through found items and childs
                foreach (var item in list)
                {
                    if(expected != null && expected.Length > 0) 
                        ValidateEntries(item, node, expected);
                   
                    //Otherwise return each child
                    foreach (var child in item.Children){  
                        var name = child.Key.ToString();   
                        
                        try{
                            if(typeof(T).Equals(typeof(YamlMappingNode))) action.Invoke(name, (T)item.Children[new YamlScalarNode(name)]);
                            else if(typeof(T).Equals(typeof(YamlScalarNode))) action.Invoke(name, (T)child.Value);
                            else throw new InvalidCastException();
                        }
                        catch(InvalidCastException){
                            action.Invoke(name, (T)Activator.CreateInstance(typeof(T)));
                        }
                    }
                }
            }
        }

        private YamlStream LoadYamlFile(string path){     
            if(!File.Exists(path)) throw new FileNotFoundException(path);
            
            var yaml = new YamlStream();            
            try{
                yaml.Load(new StringReader(File.ReadAllText(path)));
            }
            catch(Exception ex){
                throw new DocumentInvalidException("Unable to parse the YAML document, see inner exception for further details.", ex);
            }

            var root = (YamlMappingNode)yaml.Documents[0].RootNode;
            var inherits = ParseNode(root, "inherits", string.Empty);

            if(string.IsNullOrEmpty(inherits)) return yaml;
            else {
                var file = Path.Combine(Path.GetDirectoryName(path), inherits);
                var parent = LoadYamlFile(file);
                return MergeYamlFiles(parent, yaml);
            }            
        }     

        private YamlStream MergeYamlFiles(YamlStream original, YamlStream inheritor){
            //Source: https://stackoverflow.com/a/53414534
            
            var left = (YamlMappingNode)original.Documents[0].RootNode;
            var right = (YamlMappingNode)inheritor.Documents[0].RootNode; 

            foreach(var child in right.Children){
                if(!left.Children.ContainsKey(child.Key.ToString())) left.Children.Add(child.Key, child.Value);
                else left.Children[child.Key] = child.Value;                    
            }

            return original;            
        }
#endregion
#region Scope
        /// <summary>
        /// Returns the requested var value.
        /// </summary>
        /// <param name="key">Var name</param>
        /// <returns>Var value</returns>
        private object GetVar(string name, bool compute = true){
            try{
                var value = FindItemWithinScope(Vars, name);                
                return (compute && value != null && value.GetType().Equals(typeof(string)) ? ComputeVarValue(name, value.ToString()) : value);
            }
            catch (ItemNotFoundException){
                throw new VariableNotFoundException($"Undefined variable {name} has been requested.");
            }            
        }

        private void UpdateVar(string name, object value){
            name = name.ToLower();

            if(name.StartsWith("$")){
                //Only update var within upper scopes
                var current = Vars.Pop();  
                name = name.TrimStart('$');
                
                try{ 
                    var found = FindScope(Vars, name);
                    found[name] = value;
                }
                catch (ItemNotFoundException){
                    throw new VariableNotFoundException($"Undefined upper-scope variable {name} has been requested.");
                }  
                finally{ 
                    Vars.Push(current); 
                }  
            }
            else{
                //Create or update var within current scope
                var current = Vars.Peek();
                if(!current.ContainsKey(name)) current.Add(name, null);
                current[name] = value;
            }
           
        }

        private object GetChecker(string name){     
            try{
                return FindItemWithinScope(Checkers, name);
            }      
            catch{
                return new Checkers.LocalShell();
            }            
        }

        private Dictionary<string, object> FindScope(Stack<Dictionary<string, object>> scope, string key){
            object item = null;            
            var visited = new Stack<Dictionary<string, object>>();            

            try{
                //Search the checker by name within scopes
                key = key.ToLower();
                while(item == null && scope.Count > 0){
                    if(scope.Peek().ContainsKey(key)) return scope.Peek();
                    else visited.Push(scope.Pop());
                }

                //Not found
                throw new ItemNotFoundException();
            }
            finally{
                //Undo scope search
                while(visited.Count > 0){
                    scope.Push(visited.Pop());
                }
            }            
        } 

        private object FindItemWithinScope(Stack<Dictionary<string, object>> scope, string key){            
            var found = FindScope(scope, key);
            return found[key.ToLower()];          
        }                
#endregion
#region Copy Detection        
        private CopyDetectorV2 LoadCopyDetector(string type, string caption, float threshold, string filePattern, string[] folders){                        
            Assembly assembly = Assembly.GetExecutingAssembly();
            var assemblyType = assembly.GetTypes().First(t => t.Name.Equals(type, StringComparison.InvariantCultureIgnoreCase) && t.BaseType.Equals(typeof(CopyDetector)));
            var cd = (CopyDetectorV2)Activator.CreateInstance(assemblyType, new object[]{threshold, filePattern});  

            //Loading documents
            foreach(string f in folders)
            {
                try{
                    Output.Write(ComputeVarValue(caption) , ConsoleColor.DarkYellow);                    
                    cd.Load(f);                    
                    Output.WriteResponse();
                }
                catch (Exception e){
                    Output.WriteResponse(e.Message);
                }                
            }

            //Compare
            if(cd.Count > 0) cd.Compare();
            return cd;
        }                          
        
        private void PrintCopies(CopyDetectorV2 cd, string folder){
            //TODO: captions or something to apply a regex between matched folders (to get student name when needed).
            //default = {$CURRENT_FOLDER} -> try something similar to question captions on error or success
            Output.Write("Potential copy detected for ", ConsoleColor.Red);                                          
            Output.Write(folder, ConsoleColor.Yellow);
            Output.WriteLine("!", ConsoleColor.Red);
            Output.Indent();

            foreach(var item in cd.GetDetails(folder)){                
                //TODO: item.student should not exists
                Output.Write($"Match score with ~{item.source}: ", ConsoleColor.Yellow);     
                Output.WriteLine(string.Format("~{0:P2} ", item.match), (item.match < cd.Threshold ? ConsoleColor.Green : ConsoleColor.Red));
            }
            
            Output.UnIndent();
            Output.BreakLine();
        }
#endregion
#region ZIP
        private void Extract(string file, bool remove, bool recursive){
            Output.WriteLine("Extracting files: ");
            Output.Indent();

            //CurrentFolder and CurrentFile may be modified during execution
            var originalCurrentFile = CurrentFile;
            var originalCurrentFolder = CurrentFolder;

            try{
                string[] files = Directory.GetFiles(CurrentFolder, file, (recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));                    
                if(files.Length == 0) Output.WriteLine("Done!");                    
                else{
                    foreach(string zip in files){                        
                        CurrentFile = Path.GetFileName(zip);
                        CurrentFolder = Path.GetDirectoryName(zip);

                        try{
                            Output.Write($"Extracting the file ~{zip}... ", ConsoleColor.DarkYellow);
                            Utils.ExtractFile(zip);
                            Output.WriteResponse();
                        }
                        catch(Exception e){
                            Output.WriteResponse($"ERROR {e.Message}");
                            continue;
                        }

                        if(remove){                        
                            try{
                                Output.Write($"Removing the file ~{zip}... ", ConsoleColor.DarkYellow);
                                File.Delete(zip);
                                Output.WriteResponse();
                                Output.BreakLine();
                            }
                            catch(Exception e){
                                Output.WriteResponse($"ERROR {e.Message}");
                                continue;
                            }  
                        }
                    }                                                                  
                }                    
            }
            catch (Exception e){
                Output.WriteResponse(string.Format("ERROR {0}", e.Message));
            }
            finally{    
                Output.UnIndent();
                if(!remove) Output.BreakLine();

                //Restoring original values
                CurrentFile = originalCurrentFile;
                CurrentFolder = originalCurrentFolder;
            }            
        }
#endregion
#region BBDD
        private void RestoreDB(string file, string dbhost, string dbuser, string dbpass, string dbname, bool @override, bool remove, bool recursive){
            Output.WriteLine("Restoring databases: ");
            Output.Indent();

            //CurrentFolder and CurrentFile may be modified during execution
            var originalCurrentFile = CurrentFile;
            var originalCurrentFolder = CurrentFolder;

            try{
                string[] files = Directory.GetFiles(CurrentFolder, file, (recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));                    
                if(files.Length == 0) Output.WriteLine("Done!");                    
                else{
                    foreach(string sql in files){
                        CurrentFile =  Path.GetFileName(sql);
                        CurrentFolder = Path.GetDirectoryName(sql);

                        try{                            
                            //TODO: parse DB name to avoid forbidden chars.
                            var parsedDbName = Path.GetFileName(ComputeVarValue(dbname)).Replace(" ", "_").Replace(".", "_");
                            Output.WriteLine($"Checking the database ~{parsedDbName}: ", ConsoleColor.DarkYellow);      
                            Output.Indent();

                            using(var db = new Connectors.Postgres(dbhost, parsedDbName, dbuser, dbpass)){
                                if(!@override && db.ExistsDataBase()) Output.WriteLine("The database already exists, skipping!"); 
                                else{
                                    if(@override && db.ExistsDataBase()){                
                                        try{
                                            Output.Write("Dropping the existing database: "); 
                                            db.DropDataBase();
                                            Output.WriteResponse();
                                        }
                                        catch(Exception ex){
                                            Output.WriteResponse(ex.Message);
                                        } 
                                    } 

                                    try{
                                        Output.Write($"Restoring the database using the file {sql}... ", ConsoleColor.DarkYellow);
                                        db.CreateDataBase(sql);
                                        Output.WriteResponse();
                                    }
                                    catch(Exception ex){
                                        Output.WriteResponse(ex.Message);
                                    }
                                }
                            }
                        }
                        catch(Exception e){
                            Output.WriteResponse($"ERROR {e.Message}");
                            continue;
                        }

                        if(remove){                        
                            try{
                                Output.Write($"Removing the file ~{sql}... ", ConsoleColor.DarkYellow);
                                File.Delete(sql);
                                Output.WriteResponse();
                            }
                            catch(Exception e){
                                Output.WriteResponse($"ERROR {e.Message}");
                                continue;
                            }
                        }

                        Output.UnIndent();
                        Output.BreakLine();
                    }                                                                  
                }                    
            }
            catch (Exception e){
                Output.WriteResponse(string.Format("ERROR {0}", e.Message));
            }
            finally{    
                Output.UnIndent();
                
                //Restoring original values
                CurrentFile = originalCurrentFile;
                CurrentFolder = originalCurrentFolder;
            }    
        } 
#endregion
#region Google Drive
        private void UploadGDrive(string source, string user, string secret, string remoteFolder, bool link, bool copy, bool remove, bool recursive){                        
            if(string.IsNullOrEmpty(user)) throw new ArgumentNullException("The 'username' argument must be provided when using the 'upload_gdrive' feature.");                        

            Output.WriteLine("Uploading files to Google Drive: ");
            Output.Indent();

            //CurrentFolder and CurrentFile may be modified during execution
            var originalCurrentFile = CurrentFile;
            var originalCurrentFolder = CurrentFolder;
                
            //Option 1: Only files within a searchpath, recursive or not, will be uploaded into the same remote folder.
            //Option 2: Non-recursive folders within a searchpath, including its files, will be uploaded into the same remote folder.
            //Option 3: Recursive folders within a searchpath, including its files, will be uploaded into the remote folder, replicating the folder tree.
           
            try{     
                remoteFolder = ComputeVarValue(remoteFolder.TrimEnd('\\'));
                using(var drive = new Connectors.GDrive(secret, user)){                        
                    if(string.IsNullOrEmpty(Path.GetExtension(source))) UploadGDriveFolder(drive, CurrentFolder, source, remoteFolder, link, copy, recursive, remove);
                    else{
                        var files = Directory.GetFiles(CurrentFolder, source, (recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
                        if(files.Length == 0) Output.WriteLine("Done!");         

                        foreach(var file in files)
                            UploadGDriveFile(drive, file, remoteFolder, link, copy, remove);
                    }
                }                                 
            }
            catch (Exception e){
                Output.WriteResponse(string.Format("ERROR {0}", e.Message));
            }
            finally{    
                Output.UnIndent();

                //Restoring original values
                CurrentFile = originalCurrentFile;
                CurrentFolder = originalCurrentFolder;
            }    
        }
        
        private void UploadGDriveFile(Connectors.GDrive drive, string localFile, string remoteFolder, bool link, bool copy, bool remove){
            //CurrentFolder and CurrentFile may be modified during execution
            var originalCurrentFile = CurrentFile;
            var originalCurrentFolder = CurrentFolder;

            try{                            
                CurrentFile =  Path.GetFileName(localFile);
                CurrentFolder = Path.GetDirectoryName(localFile);

                Output.WriteLine($"Checking the local file ~{Path.GetFileName(localFile)}: ", ConsoleColor.DarkYellow);      
                Output.Indent();                

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
                    Output.Write($"Creating folder structure in ~'{remoteFolder}': ", ConsoleColor.Yellow); 
                    drive.CreateFolder(filePath, fileFolder);
                    Output.WriteResponse();                
                } 
                filePath = Path.Combine(filePath, fileFolder);

                if(link){
                    var content = File.ReadAllText(localFile);
                    //Regex source: https://stackoverflow.com/a/6041965
                    foreach(Match match in Regex.Matches(content, "(http|ftp|https)://([\\w_-]+(?:(?:\\.[\\w_-]+)+))([\\w.,@?^=%&:/~+#-]*[\\w@?^=%&/~+#-])?")){
                        var uri = new Uri(match.Value);

                        if(copy){
                            try{
                                Output.Write($"Copying the file from external Google Drive's account to the own one... ");
                                drive.CopyFile(uri, filePath, fileName);
                                Output.WriteResponse();
                            }
                            catch{
                                Output.WriteResponse(string.Empty);
                                copy = false;   //retry with download-reload method if fails
                            }
                        }

                        if(!copy){
                            //download and reupload       
                            Output.Write($"Downloading the file from external sources and uploading to the own Google Drive's account... ");

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
                            Output.WriteResponse();
                        }                                                       
                    }
                }
                else{
                    Output.Write($"Uploading the local file to the own Google Drive's account... ");
                    drive.CreateFile(localFile, filePath, fileName);
                    Output.WriteResponse();                        
                }

                if(remove){
                    Output.Write($"Removing the local file... ");
                    File.Delete(localFile);
                    Output.WriteResponse();       
                } 
            }
            catch (Exception ex){
                Output.WriteResponse(ex.Message);
            } 
            finally{    
                Output.UnIndent();

                //Restoring original values
                CurrentFile = originalCurrentFile;
                CurrentFolder = originalCurrentFolder;
            }              
        }

        private void UploadGDriveFolder(Connectors.GDrive drive, string localPath, string localSource, string remoteFolder, bool link, bool copy, bool recursive, bool remove){           
            //CurrentFolder and CurrentFile may be modified during execution
            var originalCurrentFile = CurrentFile;
            var originalCurrentFolder = CurrentFolder;

            try{                
                CurrentFolder =  localPath;

                var files = Directory.GetFiles(localPath, localSource, SearchOption.TopDirectoryOnly);
                var folders = (recursive ? Directory.GetDirectories(localPath, localSource, SearchOption.TopDirectoryOnly) : new string[]{});
                
                if(files.Length == 0 && folders.Length == 0) Output.WriteLine("Done!");                       
                else{
                    foreach(var file in files){
                        //This will setup CurrentFolder and CurrentFile
                        UploadGDriveFile(drive, file, remoteFolder, link, copy, remove);
                    }
                                    
                    if(recursive){
                        foreach(var folder in folders){
                            var folderName = Path.GetFileName(folder);
                            drive.CreateFolder(remoteFolder, folderName);
                            
                            //This will setup CurrentFolder and CurrentFile
                            UploadGDriveFolder(drive, folder, localSource, Path.Combine(remoteFolder, folderName), link, copy, recursive, remove);
                        }

                        if(remove){
                            //Only removes if recursive (otherwise not uploaded data could be deleted).
                            Output.Write($"Removing the local folder... ");
                            Directory.Delete(localPath);    //not-recursive delete request, should be empty, otherwise something went wrong!
                            Output.WriteResponse();       
                        } 
                    }
                }                               
            }
            catch (Exception e){
                Output.WriteResponse(string.Format("ERROR {0}", e.Message));
            }
            finally{    
                Output.UnIndent();

                //Restoring original values
                CurrentFile = originalCurrentFile;
                CurrentFolder = originalCurrentFolder;
            }    
        }                
#endregion    
    }
}