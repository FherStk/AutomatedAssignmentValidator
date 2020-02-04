﻿using System;
using System.Reflection;
using System.Linq;

namespace AutomatedAssignmentValidator
{
    class Program
    {        
        private enum ScriptTarget{
            SINGLE,
            BATCH,
            NONE
        }         

        static void Main(string[] args)
        {
            Terminal.BreakLine();
            Terminal.Write("Automated Assignment Validator: ", ConsoleColor.Yellow);                        
            Terminal.WriteLine("v2.0.0.0");
            Terminal.Write(String.Format("Copyright © {0}: ", DateTime.Now.Year), ConsoleColor.Yellow);            
            Terminal.WriteLine("Fernando Porrino Serrano.");
            Terminal.Write(String.Format("Under the AGPL license: ", DateTime.Now.Year), ConsoleColor.Yellow);            
            Terminal.WriteLine("https://github.com/FherStk/ASIX-DAM-M04-WebAssignmentValidator/blob/master/LICENSE");
            Terminal.BreakLine();

            LaunchScript(args);
        }  
        private static void LaunchScript(string[] args){
            object script = null;
            Type type = null;
            ScriptTarget mode = ScriptTarget.NONE;

            for(int i = 0; i < args.Length; i++){
                if(args[i].StartsWith("--") && args[i].Contains("=")){
                    string[] data = args[i].Split("=");
                    string param = data[0].ToLower().Trim().Replace("\"", "").Substring(2);
                    string value = data[1].Trim().Replace("\"", "");
                    
                    switch(param){
                        case "script":
                            Assembly assembly = Assembly.GetExecutingAssembly();
                            type = assembly.GetTypes().First(t => t.Name == value);
                            script = Activator.CreateInstance(type, new string[][]{args});                        
                            break;
                        
                        case "target":
                            mode = (ScriptTarget)Enum.Parse(typeof(ScriptTarget), value, true);
                            break;
                    }                                  
                }                                
            }

            if(mode == ScriptTarget.NONE)
                Console.WriteLine("Unable to launch the script: a 'mode' parameter was expected.");

            else if(script == null)
                Console.WriteLine("Unable to launch the script: none has been found with the given name.");

            else{                
                MethodInfo methodInfo = null;
                if(mode == ScriptTarget.BATCH) methodInfo = type.GetMethod("Batch");
                else if(mode == ScriptTarget.SINGLE) methodInfo = type.GetMethod("Single");                
                methodInfo.Invoke(script, null);
            }
        }  
                                                    
    }
}
