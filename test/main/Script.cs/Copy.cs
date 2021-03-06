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

namespace AutoCheck.Test
{    
    [Parallelizable(ParallelScope.All)]    
    public class Copy : Test
    {                
        [OneTimeSetUp]
        public virtual void StartUp() 
        {
            SamplesScriptFolder = GetSamplePath(Path.Combine("script", Name));            
        }

        [Test, Category("Copy"), Category("Local")]
        public void Script_COPY_PLAINTEXT_PATH_ISCOPY() 
        {               
            var dest =  Path.Combine(TempScriptFolder, "test1");         
            var dest1 = Path.Combine(dest, "folder1");
            var dest2 = Path.Combine(dest, "folder2");

            if(!Directory.Exists(dest1)) Directory.CreateDirectory(dest1);
            if(!Directory.Exists(dest2)) Directory.CreateDirectory(dest2);                                 

            File.Copy(GetSampleFile("plaintext", "lorem1.txt"), GetSampleFile(dest1, "sample1.txt"));
            File.Copy(GetSampleFile("plaintext", "lorem1.txt"), GetSampleFile(dest2, "sample2.txt"));
 
            Assert.IsTrue(File.Exists(GetSampleFile(dest1, "sample1.txt")));
            Assert.IsTrue(File.Exists(GetSampleFile(dest2, "sample2.txt")));

            var s = new AutoCheck.Core.Script(GetSampleFile("copy_plaintext_ok1.yaml")); 
            Assert.AreEqual("Running script copy_plaintext_ok1 (v1.0.0.0):\r\n   Starting the copy detector for PLAINTEXT:\r\n      Looking for potential copies within folder1... OK\r\n      Looking for potential copies within folder2... OK\r\n\r\nRunning on batch mode:\r\n   Potential copy detected for folder1\\sample1.txt:\r\n      Match score with folder2\\sample2.txt... 100,00 % \r\nRunning on batch mode:\r\n   Potential copy detected for folder2\\sample2.txt:\r\n      Match score with folder1\\sample1.txt... 100,00 %", s.Output.ToString());            
            Directory.Delete(dest, true);
        }

        [Test, Category("Copy"), Category("Local")]
        public void Script_COPY_PLAINTEXT_FOLDERS_NOTCOPY()
        {               
            var dest =  Path.Combine(TempScriptFolder, "test2");         
            var dest1 = Path.Combine(dest, "folder1");
            var dest2 = Path.Combine(dest, "folder2");

            if(!Directory.Exists(dest1)) Directory.CreateDirectory(dest1);
            if(!Directory.Exists(dest2)) Directory.CreateDirectory(dest2);                                 

            File.Copy(GetSampleFile("plaintext", "lorem1.txt"), GetSampleFile(dest1, "sample1.txt"));
            File.Copy(GetSampleFile("plaintext", "lorem2.txt"), GetSampleFile(dest2, "sample2.txt"));
 
            Assert.IsTrue(File.Exists(GetSampleFile(dest1, "sample1.txt")));
            Assert.IsTrue(File.Exists(GetSampleFile(dest2, "sample2.txt")));

            var s = new AutoCheck.Core.Script(GetSampleFile("copy_plaintext_ok2.yaml")); 
            Assert.AreEqual($"Running script copy_plaintext_ok2 (v1.0.0.0):\r\n   Starting the copy detector for PLAINTEXT:\r\n      Looking for potential copies within folder1... OK\r\n      Looking for potential copies within folder2... OK\r\n\r\nRunning on batch mode for {Path.GetFileName(dest1)}:\r\nRunning on batch mode for {Path.GetFileName(dest2)}:", s.Output.ToString());            
            Directory.Delete(dest, true);
        }
    }
}