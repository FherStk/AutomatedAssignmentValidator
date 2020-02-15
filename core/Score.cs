using System;
using System.Collections.Generic;

namespace AutomatedAssignmentValidator.Core{
    //TODO: This class is only being used within Script.cs, so its code could be moved in there.
    public class Score{
        private float Success {get; set;}
        private float Fails {get; set;}
        private List<string> Errors {get; set;}
        private float Points {get; set;}
        public float Value {get; private set;}   
        public bool IsOpen  {
            get{
                return this.Errors != null;
            }
        }   

        public Score(){                
        }

        public void OpenQuestion(float score){
            if(this.IsOpen) throw new Exception("Close the question before opening a new one.");
            this.Errors = new List<string>();                
            this.Points = score;
        }

        public void CloseQuestion(){
            if(!this.IsOpen) throw new Exception("Open the question before closing the current one.");
            if(this.Errors.Count == 0) this.Success += this.Points;
            else this.Fails += this.Points;
            
            this.Errors = null;
            
            float total = Success + Fails;
            this.Value = (total > 0 ? (Success / total)*10 : 0);                      
        }

        public void EvalQuestion(List<string> errors){     
            if(!this.IsOpen) throw new Exception("Open the question before evaluating the current one.");          
            this.Errors.AddRange(errors);
        }

        public void CancelQuestion(){
            this.Errors = null;
        }
    }
}