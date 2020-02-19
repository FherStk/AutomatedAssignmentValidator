using System;
using System.IO;
using System.Linq;

namespace AutomatedAssignmentValidator.Core{
    /// <summary>
    /// This class must be inherited in order to develop a file-system oriented custom script.
    /// The script is the main container for a set of instructions, which will test the correctness of an assignement.
    /// </summary>      
    /// <typeparam name="T">The copy detector that will be automatically used within the script.</typeparam>
    public abstract class ScriptFiles<T>: Script<T> where T: Core.CopyDetector, new(){       
        /// <summary>
        /// The current student's name.
        /// </summary>
        /// <value></value>
        protected string Student {get; private set;}
        /// <summary>
        /// Creates a new script instance.
        /// </summary>
        /// <param name="args">Argument list, loaded from the command line, on which one will be stored into its equivalent local property.</param>
        /// <returns></returns>
        public ScriptFiles(string[] args): base(args){                   
        }       
        /// <summary>
        /// Sets up the default arguments values, can be overwrited if custom arguments are needed.
        /// </summary> 
        protected override void DefaultArguments(){  
            //Note: this cannot be on constructor, because the arguments introduced on command line should prevail:
            //  1. Default base class values
            //  2. Inherited class values
            //  3. Command line argument values
            
            this.CpThresh = 0.75f;           
        }                
        /// <summary>
        /// This method contains the main script to run for a single student.
        /// </summary>       
        public override void Run(){   
            this.Student = Core.Utils.FolderNameToStudentName(this.Path); //this.Path will be loaded by argument (single) or by batch (folder name).
            Output.Instance.WriteLine(string.Format("Running ~{0}~ for the student ~{1}: ", this.GetType().Name, this.Student), ConsoleColor.DarkYellow);
        }                               
    }
}