using System.Collections.Generic;
namespace AutomatedAssignmentValidator{
    //TODO: Must be removed
    public class TestResult{
        public string Caption {get; private set;}
        public List<string> Errors {get; private set;}

        public TestResult(string caption){
            this.Caption = caption;
            this.Errors = new List<string>();
        }
    }
}