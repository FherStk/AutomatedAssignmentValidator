﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace AutomatedAssignmentValidator
{
    class Program
    {
        private static string _PATH = null; 
        private static string _FOLDER = null; 
        private static AssignType _ASSIG = AssignType.UNDEFINED; 
        private static string _SERVER = null; 
        private static string _DATABASE = null;  
        private enum AssignType{
            CSS3,
            HTML5,
            ODOO,
            PERMISSIONS,
            UNDEFINED

        }  

        static void Main(string[] args)
        {
            Utils.BreakLine();
            Utils.Write("Automated Assignment Validator: ", ConsoleColor.Yellow);                        
            Utils.WriteLine("v1.3.0.1");
            Utils.Write(String.Format("Copyright © {0}: ", DateTime.Now.Year), ConsoleColor.Yellow);            
            Utils.WriteLine("Fernando Porrino Serrano. Under the AGPL license (https://github.com/FherStk/ASIX-DAM-M04-WebAssignmentValidator/blob/master/LICENSE)");
            
            LoadArguments(args);
            RunWithArguments();
            
            Utils.BreakLine();
            Utils.WriteLine("Press any key to close.");
            Console.ReadKey();
        }   

        private static void LoadArguments(string[] args){
            for(int i = 0; i < args.Length; i++){
                if(args[i].StartsWith("--") && args[i].Contains("=")){
                    string[] data = args[i].Split("=");
                    string param = data[0].ToLower().Trim().Replace("\"", "").Substring(2);
                    string value = data[1].Trim().Replace("\"", "");
                    
                    switch(param){
                        case "path":
                            _PATH = value;
                            break;

                        case "folder":
                            _FOLDER = value;
                            break;
                        
                        case "assig":
                            try{
                                _ASSIG = (AssignType)Enum.Parse(typeof(AssignType), value, true);    
                            }                            
                            catch{
                                _ASSIG = AssignType.UNDEFINED;
                            }
                                                        
                            break;

                        case "server":
                            _SERVER = value;                            
                            break;

                        case "database":
                            _DATABASE = value;
                            break;
                    }                                        
                }                                
            }
        }
        private static void RunWithArguments(){
            Utils.BreakLine();
            if(_ASSIG == AssignType.UNDEFINED) Utils.PrintResults(new List<string>(){"A parameter 'assig' must be provided with an accepted value (see README.md)."});
            else{
                Utils.Write("Running test: ");          
                Utils.WriteLine(_ASSIG.ToString(), ConsoleColor.Cyan);            
                Utils.BreakLine();
                
                if(string.IsNullOrEmpty(_PATH)) CheckFolder();
                else CheckPath();            
            }                
        }
        private static void CheckPath()
        { 
            if(!Directory.Exists(_PATH)) Utils.PrintResults(new List<string>(){string.Format("The provided path '{0}' does not exist.", _PATH)});         
            else{
                switch(_ASSIG){
                    case AssignType.HTML5:
                    case AssignType.CSS3:
                        //MOODLE assignment batch download directory composition
                        foreach(string f in Directory.EnumerateDirectories(_PATH))
                        {
                            try{
                                string student = Utils.MoodleFolderToStudentName(f);
                                if(string.IsNullOrEmpty(student)){
                                    Utils.Write("Skipping folder: ");
                                    Utils.WriteLine(Path.GetFileNameWithoutExtension(f), ConsoleColor.DarkYellow);                               
                                    continue;
                                }

                                Utils.Write("Checking files for the student: ");
                                Utils.WriteLine(student, ConsoleColor.DarkYellow);

                                string zip = Directory.GetFiles(f, "*.zip", SearchOption.AllDirectories).FirstOrDefault();    
                                if(!string.IsNullOrEmpty(zip)){
                                    Utils.Write("   Unzipping the files: ");
                                    try{
                                        Utils.ExtractZipFile(zip);
                                        Utils.PrintResults();                                    
                                    }
                                    catch(Exception e){
                                        Utils.PrintResults(new List<string>(){string.Format("ERROR {0}", e.Message)});
                                        continue;
                                    }
                                    
                                    Utils.Write("   Removing the zip file: ");
                                    try{
                                        File.Delete(zip);
                                        Utils.PrintResults();                                    
                                    }
                                    catch(Exception e){
                                        Utils.PrintResults(new List<string>(){string.Format("ERROR {0}", e.Message)});
                                        //the process can continue
                                    }
                                    finally{
                                        Utils.BreakLine();
                                    }                                              
                                }    

                                _FOLDER = f; 
                                CheckFolder();
                            }
                            catch{

                            }
                            finally{
                                Utils.WriteLine("Press any key to continue...");
                                Utils.BreakLine();
                                Console.ReadKey(); 
                            }
                        }                         
                        break;
                    
                    case AssignType.ODOO:
                    case AssignType.PERMISSIONS:
                        //A folder containing all the SQL files, named as "x_NAME_SURNAME".
                        //TODO: it will be easier if the files are delivered through a regular assignment instead of the quiz one.
                        //after that, a merge with CSS3 and HTML5 will be possible (so some code will be simplified)
                        foreach(string f in Directory.EnumerateDirectories(_PATH))
                        {
                            //TODO: self-extract the zip into a folder with the same name                            
                            _FOLDER = f;
                            _DATABASE = string.Empty;   //no database can be selected when using 'path' mode
                            CheckFolder();

                            Utils.WriteLine("Press any key to continue...");
                            Utils.BreakLine();
                            Console.ReadKey(); 
                        }
                        break;                   
                }
            }                            
        }  
        private static void CheckFolder()
        {                             
            switch(_ASSIG){
                case AssignType.HTML5:
                    if(string.IsNullOrEmpty(_FOLDER)) Utils.PrintResults(new List<string>(){"The parameter 'folder' or 'path' must be provided when using 'assig=html5'."});
                    if(!Directory.Exists(_FOLDER)) Utils.PrintResults(new List<string>(){string.Format("Unable to find the provided folder '{0}'.", _FOLDER)});
                    else Html5Validator.ValidateAssignment(_FOLDER);
                    break;

                case AssignType.CSS3:
                    if(string.IsNullOrEmpty(_FOLDER)) Utils.PrintResults(new List<string>(){"The parameter 'folder' or 'path' must be provided when using 'assig=html5'."});
                    if(!Directory.Exists(_FOLDER))Utils.PrintResults(new List<string>(){string.Format("Unable to find the provided folder '{0}'.", _FOLDER)});
                    else Css3Validator.ValidateAssignment(_FOLDER);
                    break;

                case AssignType.ODOO:       
                case AssignType.PERMISSIONS:                      
                    try{
                        bool exist = false;
                        if(string.IsNullOrEmpty(_DATABASE)){
                            _DATABASE = Utils.FolderNameToDataBase(_FOLDER, (_ASSIG == AssignType.ODOO ? "odoo" : "empresa"));
                            string sql = Directory.GetFiles(_FOLDER, "*.sql", SearchOption.AllDirectories).FirstOrDefault();
                            
                            if(string.IsNullOrEmpty(sql)) Utils.PrintResults(new List<string>(){string.Format("The current folder '{0}' does not contains any sql file.", _FOLDER)});
                            else if(string.IsNullOrEmpty(_SERVER)) Utils.PrintResults(new List<string>(){"The parameter 'server' must be provided when using --assig=odoo."});
                            else{
                                exist = Utils.DataBaseExists(_SERVER, _DATABASE);
                                if(!exist) exist = Utils.CreateDataBase(_SERVER, _DATABASE, sql);
                                if(!exist) Utils.PrintResults(new List<string>(){string.Format("Unable to create the database '{0}' on server '{1}'.", _DATABASE, _SERVER)});
                            }

                            if(!exist) break;
                        }                          

                        if(!exist) exist = Utils.DataBaseExists(_SERVER, _DATABASE);
                        if(!exist) Utils.PrintResults(new List<string>(){string.Format("Unable to create the database '{0}' on server '{1}'.", _DATABASE, _SERVER)});
                        else {
                            if(_ASSIG == AssignType.ODOO) OdooValidator.ValidateAssignment(_SERVER, _DATABASE);
                            else if(_ASSIG == AssignType.PERMISSIONS) PermissionsValidator.ValidateAssignment(_SERVER, _DATABASE);
                        }
                                                                                  
                    }
                    catch(Exception e){
                        Utils.PrintResults(new List<string>(){string.Format("EXCEPTION: {0}", e.Message)});
                    }                    
                    break;

                default:
                    Utils.PrintResults(new List<string>(){string.Format("No check method has been defined for the assig '{0}'.", _ASSIG)});
                    break;
            }                 
                    
        }  
    }
}
