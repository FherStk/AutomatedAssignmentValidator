using System;
using System.Collections.Generic;

namespace AutomatedAssignmentValidator.Scripts{
    public class ASIX_M02UF3_PermissionsAssignment: Core.ScriptDB<CopyDetectors.SqlLog>{                       
        public ASIX_M02UF3_PermissionsAssignment(string[] args): base(args){        
        }                

        public override void Run(){
            base.Run();            
            Output.Indent();

            Checkers.DataBase db = new Checkers.DataBase(this.Host, this.DataBase, "postgres", "postgres", this.Output);
            OpenQuestion("Question 1: ");
            CloseQuestion("This questions does not score.");            

            OpenQuestion("Question 2: ", 1);
            EvalQuestion(db.CheckForeignKey("rrhh", "empleats", "id_cap", "rrhh", "empleats", "id"));
            CloseQuestion();

            OpenQuestion("Question 3: ", 1);
            EvalQuestion(db.CheckForeignKey("rrhh", "empleats", "id_departament", "rrhh", "departaments", "id"));
            CloseQuestion();

            OpenQuestion("Question 4: ", 1);
            EvalQuestion(db.CheckIfEntryAdded("rrhh", "empleats", "id", 9));
            EvalQuestion(db.CheckIfTableContainsPrivileges("rrhhadmin", "rrhh", "empleats", 'a'));
            CloseQuestion();

            OpenQuestion("Question 5: ");
            CloseQuestion("This questions does not score."); 

            OpenQuestion("Question 6: ", 1);
            EvalQuestion(db.CheckForeignKey("produccio", "fabricacio", "id_fabrica", "produccio", "fabriques", "id"));
            EvalQuestion(db.CheckForeignKey("produccio", "fabricacio", "id_producte", "produccio", "productes", "id"));
            EvalQuestion(db.CheckIfTableContainsPrivileges("prodadmin", "produccio", "fabricacio", 'x'));
            CloseQuestion();

            OpenQuestion("Question 7: ", 2);
            EvalQuestion(db.CheckForeignKey("produccio", "fabriques", "id_responsable", "rrhh", "empleats", "id"));
            EvalQuestion(db.CheckIfSchemaContainsPrivilege("prodadmin", "rrhh", 'U'));
            EvalQuestion(db.CheckIfTableContainsPrivileges("prodadmin", "rrhh", "empleats", 'x'));
            CloseQuestion();

            OpenQuestion("Question 8: ", 1);
            EvalQuestion(db.CheckIfEntryRemoved("rrhh", "empleats", "id", 9));
            EvalQuestion(db.CheckIfTableMatchesPrivileges("rrhhadmin", "rrhh", "empleats", "arwxt"));
            EvalQuestion(db.CheckIfTableMatchesPrivileges("rrhhadmin", "rrhh", "departaments", "arwxt"));
            EvalQuestion(db.CheckIfTableMatchesPrivileges("prodadmin", "produccio", "fabriques", "arwxt"));
            EvalQuestion(db.CheckIfTableMatchesPrivileges("prodadmin", "produccio", "productes", "arwxt"));
            EvalQuestion(db.CheckIfTableMatchesPrivileges("prodadmin", "produccio", "fabricacio", "arwxt"));
            CloseQuestion();

            OpenQuestion("Question 9: ", 3);
            EvalQuestion(db.CheckRoleMembership("dbadmin", new string[]{"prodadmin", "rrhhadmin"}));
            EvalQuestion(db.CheckIfTableMatchesPrivileges("dbadmin", "rrhh", "empleats", "dD"));
            EvalQuestion(db.CheckIfTableMatchesPrivileges("dbadmin", "rrhh", "departaments", "dD"));
            EvalQuestion(db.CheckIfTableMatchesPrivileges("dbadmin", "produccio", "fabriques", "dD"));
            EvalQuestion(db.CheckIfTableMatchesPrivileges("dbadmin", "produccio", "productes", "dD"));
            EvalQuestion(db.CheckIfTableMatchesPrivileges("dbadmin", "produccio", "fabricacio", "dD"));
            CloseQuestion();
            
            PrintScore();   
            Output.UnIndent();        
        }
    }
}