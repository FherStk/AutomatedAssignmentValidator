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

using System.IO;
using NUnit.Framework;
using AutoCheck.Core.Exceptions;

namespace AutoCheck.Test
{    
    [Parallelizable(ParallelScope.All)]    
    public class Vars : Test
    {                
        [OneTimeSetUp]
        public virtual void StartUp() 
        {
            SamplesScriptFolder = GetSamplePath(Path.Combine("script", Name));            
        }

        protected override void CleanUp(){
            //Clean temp files
            var dir = Path.Combine(GetSamplePath("script"), "temp", Name);
            if(Directory.Exists(dir)) Directory.Delete(dir, true);       

            //Clean logs
            var logs = Path.Combine(AutoCheck.Core.Utils.AppFolder, "logs", Name);
            if(Directory.Exists(logs)) Directory.Delete(logs, true);                      
        }

        [Test, Category("Vars"), Category("Local")]
        public void ParseVars_DEFAULT_VARS()
        {  
           Assert.DoesNotThrow(() => new AutoCheck.Core.Script(GetSampleFile("vars_ok1.yaml")));                 
        }

        [Test, Category("Vars"), Category("Remote")]
        public void ParseVars_COMPUTED_OPPERATION()
        {  
            //NOTE: needs a remote GNU users to work (usuario@usuario)
            var s = new AutoCheck.Core.Script(GetSampleFile("vars_ok5.yaml"));             
            Assert.AreEqual("Running script vars_ok5 (v1.0.0.0):\r\n   Running opperation 1+2+3: OK", s.Output.ToString());                          
        }
        
        [Test, Category("Vars"), Category("Local")]
        public void ParseVars_COMPUTED_REGEX()
        {  
            Assert.DoesNotThrow(() => new AutoCheck.Core.Script(GetSampleFile("vars_ok2.yaml")));                                           
        }
 
        [Test, Category("Vars"), Category("Local")]
        public void ParseVars_TYPED_SIMPLE()
        {                         
            Assert.DoesNotThrow(() => new AutoCheck.Core.Script(GetSampleFile("vars_ok3.yaml")));              
        }

        [Test, Category("Vars"), Category("Local")]
        public void ParseVars_SCOPE_LEVEL1()
        {                         
            Assert.DoesNotThrow(() => new AutoCheck.Core.Script(GetSampleFile("vars_ok4.yaml")));              
        }
               
        [Test, Category("Vars"), Category("Local")]
        public void ParseVars_INVALID_DUPLICATED()
        {  
            Assert.Throws<DocumentInvalidException>(() => new AutoCheck.Core.Script(GetSampleFile("vars_ko1.yaml")));            
        }

        [Test, Category("Vars"), Category("Local")]
        public void ParseVars_NOTEXISTS_SIMPLE()
        {  
            Assert.Throws<VariableNotFoundException>(() => new AutoCheck.Core.Script(GetSampleFile("vars_ko2.yaml")));           
        }
 
        [Test, Category("Vars"), Category("Local")]
        public void ParseVars_REGEX_NOTAPPLIED()
        {  
            Assert.Throws<RegexInvalidException>(() => new AutoCheck.Core.Script(GetSampleFile("vars_ko3.yaml")));
        }

        [Test, Category("Vars"), Category("Local")]
        public void ParseVars_REGEX_NOVARNAME()
        {  
            Assert.Throws<VariableNotFoundException>(() => new AutoCheck.Core.Script(GetSampleFile("vars_ko4.yaml")));
        }

        [Test, Category("Vars"), Category("Local")]
        public void ParseVars_REGEX_NOTEXISTS()
        {  
            Assert.Throws<VariableNotFoundException>(() => new AutoCheck.Core.Script(GetSampleFile("vars_ko5.yaml")));
        }

        [Test, Category("Vars"), Category("Local")]
        public void ParseVars_SCOPE_NOTEXISTS()
        {  
            Assert.Throws<VariableNotFoundException>(() => new AutoCheck.Core.Script(GetSampleFile("vars_ko6.yaml")));
        }
    }
}