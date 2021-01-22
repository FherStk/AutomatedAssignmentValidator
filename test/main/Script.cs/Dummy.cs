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
    public class Dummy : Test
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

        [Test, Category("Dummy")]
        public void ParseBody_SCRIPT_SINGLE_OK1()
        {    
            //TODO: test this
            var dest =  Path.Combine(GetSamplePath("script"), "temp", "script", "test1");                        
            if(!Directory.Exists(dest)) Directory.CreateDirectory(dest);
            
            File.Copy(GetSampleFile("html", "correct.html"), GetSampleFile(dest, "index.html"));
            Assert.IsTrue(File.Exists(GetSampleFile(dest, "index.html")));

            var s = new AutoCheck.Core.Script(GetSampleFile("script_single_1.yaml"));             
            Assert.AreEqual("Running script script_single_1 (v1.0.0.1):\r\nRunning on single mode:\r\n   Question 1 [2 points] - Checking Index.html:\r\n      Looking for index.html... OK\r\n      Validating document against the W3C validation service... OK\r\n\r\n      Question 1.1 [1 point] - Validating headers:\r\n         Checking amount of level-1 headers... OK\r\n         Checking amount of level-2 headers... OK\r\n\r\n      Question 1.2 [1 point] - Validating paragraphs:\r\n         Checking amount of paragraphs... OK\r\n         Checking content legth within paragraphs... ERROR:\n            -Expected -> >=1500; Found -> 144\r\n\r\n   TOTAL SCORE: 5 / 10", s.Output.ToString());            
        }

        [Test, Category("Dummy")]
        public void ParseBody_SCRIPT_SINGLE_ONEXCEPTION_ABORT()
        {    
            var dest =  Path.Combine(GetSamplePath("script"), "temp", "script", "test2");                        
            if(!Directory.Exists(dest)) Directory.CreateDirectory(dest);
            
            File.Copy(GetSampleFile("html", "incorrect.html"), GetSampleFile(dest, "index.html"));
            Assert.IsTrue(File.Exists(GetSampleFile(dest, "index.html")));

            var s = new AutoCheck.Core.Script(GetSampleFile("script_single_2.yaml"));             
            Assert.AreEqual("Running script script_single_2 (v1.0.0.1):\r\nRunning on single mode:\r\n   Question 1 [2 points] - Checking Index.html:\r\n      Looking for index.html... OK\r\n      Validating document against the W3C validation service... ERROR:\n         -No p element in scope but a p end tag seen.</h1>\n             </p>\n         </bod\r\n\r\n   Aborting execution!\r\n\r\n   TOTAL SCORE: 0 / 10", s.Output.ToString());            
        }

        [Test, Category("Dummy")]
        public void ParseBody_SCRIPT_SINGLE_ONEXCEPTION_ERROR()
        {    
            var dest =  Path.Combine(GetSamplePath("script"), "temp", "script", "test3");                        
            if(!Directory.Exists(dest)) Directory.CreateDirectory(dest);
            
            File.Copy(GetSampleFile("html", "incorrect.html"), GetSampleFile(dest, "index.html"));
            Assert.IsTrue(File.Exists(GetSampleFile(dest, "index.html")));

            var s = new AutoCheck.Core.Script(GetSampleFile("script_single_3.yaml"));             
            Assert.AreEqual("Running script script_single_3 (v1.0.0.1):\r\nRunning on single mode:\r\n   Question 1 [2 points] - Checking Index.html:\r\n      Looking for index.html... OK\r\n      Validating document against the W3C validation service... ERROR:\n         -No p element in scope but a p end tag seen.</h1>\n             </p>\n         </bod\r\n\r\n      Question 1.1 [1 point] - Validating headers:\r\n         Checking amount of level-1 headers... OK\r\n         Checking amount of level-2 headers... ERROR:\n            -Expected -> >=1; Found -> 0\r\n\r\n      Question 1.2 [1 point] - Validating paragraphs:\r\n         Checking amount of paragraphs... OK\r\n         Checking content legth within paragraphs... ERROR:\n            -Expected -> >=1500; Found -> 10\r\n\r\n   TOTAL SCORE: 0 / 10", s.Output.ToString());            
        }

        [Test, Category("Dummy")]
        public void ParseBody_SCRIPT_SINGLE_ONEXCEPTION_SUCCESS()
        {    
            var dest =  Path.Combine(GetSamplePath("script"), "temp", "script", "test4");                        
            if(!Directory.Exists(dest)) Directory.CreateDirectory(dest);
            
            File.Copy(GetSampleFile("html", "incorrect.html"), GetSampleFile(dest, "index.html"));
            Assert.IsTrue(File.Exists(GetSampleFile(dest, "index.html")));

            var s = new AutoCheck.Core.Script(GetSampleFile("script_single_4.yaml"));             
            Assert.AreEqual("Running script script_single_4 (v1.0.0.1):\r\nRunning on single mode:\r\n   Question 1 [2 points] - Checking Index.html:\r\n      Looking for index.html... OK\r\n      Validating document against the W3C validation service... OK\r\n\r\n      Question 1.1 [1 point] - Validating headers:\r\n         Checking amount of level-1 headers... OK\r\n         Checking amount of level-2 headers... ERROR:\n            -Expected -> >=1; Found -> 0\r\n\r\n      Question 1.2 [1 point] - Validating paragraphs:\r\n         Checking amount of paragraphs... OK\r\n         Checking content legth within paragraphs... ERROR:\n            -Expected -> >=1500; Found -> 10\r\n\r\n   TOTAL SCORE: 0 / 10", s.Output.ToString());            
        }

        [Test, Category("Dummy")]
        public void ParseBody_SCRIPT_SINGLE_ONEXCEPTION_SKIP()
        {    
            var dest =  Path.Combine(GetSamplePath("script"), "temp", "script", "test5");                        
            if(!Directory.Exists(dest)) Directory.CreateDirectory(dest);
            
            File.Copy(GetSampleFile("html", "incorrect.html"), GetSampleFile(dest, "index.html"));
            File.Copy(GetSampleFile("html", "correct.html"), GetSampleFile(dest, "contact.html"));
            Assert.IsTrue(File.Exists(GetSampleFile(dest, "index.html")));
            Assert.IsTrue(File.Exists(GetSampleFile(dest, "contact.html")));

            var s = new AutoCheck.Core.Script(GetSampleFile("script_single_5.yaml"));             
            Assert.AreEqual("Running script script_single_5 (v1.0.0.1):\r\nRunning on single mode:\r\n   Question 1 [2 points] - Checking Index.html:\r\n      Looking for index.html... OK\r\n      Validating document against the W3C validation service... ERROR:\n         -No p element in scope but a p end tag seen.</h1>\n             </p>\n         </bod\r\n\r\n   Question 2 [2 points] - Checking Contact.html:\r\n      Looking for contact.html... OK\r\n      Validating document against the W3C validation service... OK\r\n\r\n      Question 2.1 [1 point] - Validating headers:\r\n         Checking amount of level-1 headers... OK\r\n         Checking amount of level-2 headers... OK\r\n\r\n      Question 2.2 [1 point] - Validating paragraphs:\r\n         Checking amount of paragraphs... OK\r\n         Checking content legth within paragraphs... ERROR:\n            -Expected -> >=1500; Found -> 144\r\n\r\n   TOTAL SCORE: 2.5 / 10", s.Output.ToString());            
        }  

        [Test, Category("Dummy")]
        public void ParseBody_SCRIPT_SINGLE_ONEXCEPTION_NOCAPTION()
        {    
            var dest =  Path.Combine(GetSamplePath("script"), "temp", "script", "test6");                        
            if(!Directory.Exists(dest)) Directory.CreateDirectory(dest);
            
            File.Copy(GetSampleFile("html", "incorrect.html"), GetSampleFile(dest, "index.html"));
            Assert.IsTrue(File.Exists(GetSampleFile(dest, "index.html")));

            Assert.Throws<DocumentInvalidException>(() => new AutoCheck.Core.Script(GetSampleFile("script_single_6.yaml")));            
        }

        [Test, Category("Dummy")]
        public void ParseBody_SCRIPT_ARGUMENT_TYPE_CONNECTOR_OK()
        {    
            var dest =  Path.Combine(GetSamplePath("script"), "temp", "script", "test7");                        
            if(!Directory.Exists(dest)) Directory.CreateDirectory(dest);
            
            File.Copy(GetSampleFile("html", "correct.html"), GetSampleFile(dest, "index.html"));
            File.Copy(GetSampleFile("css", "correct.css"), GetSampleFile(dest, "index.css"));
            Assert.IsTrue(File.Exists(GetSampleFile(dest, "index.html")));
            Assert.IsTrue(File.Exists(GetSampleFile(dest, "index.css")));

            var s = new AutoCheck.Core.Script(GetSampleFile("script_single_7.yaml"));             
            Assert.AreEqual("Running script script_single_7 (v1.0.0.1):\r\nRunning on single mode:\r\n   Question 1 [1 point] - Checking index.css:\r\n      Looking for index.html... OK\r\n      Looking for index.css... OK\r\n      Validating document against the W3C validation service... OK\r\n\r\n      Question 1.1 [1 point] - Validating font property:\r\n         Checking if the font property has been created... OK\r\n         Checking if the font property has NOT been applied... OK\r\n\r\n   TOTAL SCORE: 10 / 10", s.Output.ToString());            
        }

        [Test, Category("Dummy")]
        public void ParseBody_SCRIPT_ARGUMENT_TYPE_CONNECTOR_KO()
        {    
            var dest =  Path.Combine(GetSamplePath("script"), "temp", "script", "test8");                        
            if(!Directory.Exists(dest)) Directory.CreateDirectory(dest);
            
            File.Copy(GetSampleFile("html", "correct.html"), GetSampleFile(dest, "index.html"));
            File.Copy(GetSampleFile("css", "correct.css"), GetSampleFile(dest, "index.css"));
            Assert.IsTrue(File.Exists(GetSampleFile(dest, "index.html")));
            Assert.IsTrue(File.Exists(GetSampleFile(dest, "index.css")));

            var s = new AutoCheck.Core.Script(GetSampleFile("script_single_8.yaml"));
            Assert.AreEqual("Running script script_single_8 (v1.0.0.1):\r\nRunning on single mode:\r\n   Question 1 [1 point] - Checking index.css:\r\n      Looking for index.css... OK\r\n      Validating document against the W3C validation service... OK\r\n\r\n      Question 1.1 [1 point] - Validating font property:\r\n         Checking if the font property has been created... OK\r\n         Checking if the font property has NOT been applied... ERROR:\n            -Unable to find any connector named 'Html'.\r\n\r\n   TOTAL SCORE: 0 / 10", s.Output.ToString());
        }
       
        [Test, Category("Dummy")]
        public void ParseBody_SCRIPT_ARGUMENT_TYPE_CONNECTOR_TUPLE()
        {    
            var dest =  Path.Combine(GetSamplePath("script"), "temp", "script", "test9");                        
            if(!Directory.Exists(dest)) Directory.CreateDirectory(dest);
            
            File.Copy(GetSampleFile("html", "correct.html"), GetSampleFile(dest, "index.html"));
            File.Copy(GetSampleFile("css", "correct.css"), GetSampleFile(dest, "index.css"));
            Assert.IsTrue(File.Exists(GetSampleFile(dest, "index.html")));
            Assert.IsTrue(File.Exists(GetSampleFile(dest, "index.css")));

            var s = new AutoCheck.Core.Script(GetSampleFile("script_single_9.yaml"));             
            Assert.AreEqual("Running script script_single_9 (v1.0.0.1):\r\nRunning on single mode:\r\n   Question 1 [1 point] - Checking index.css:\r\n      Looking for index.html... OK\r\n      Looking for index.css... OK\r\n      Validating document against the W3C validation service... OK\r\n\r\n      Question 1.1 [1 point] - Validating set of properties:\r\n         Checking if the (top | right | bottom | left) property has been created... OK\r\n\r\n   TOTAL SCORE: 10 / 10", s.Output.ToString());            
        }
    }
}