using System;
using System.Collections.Generic;

namespace AutomatedAssignmentValidator.Scripts{
    public class ASIX_M02UF3_OdooUsageAssignment: Core.ScriptDB<CopyDetectors.None>{                       
        public ASIX_M02UF3_OdooUsageAssignment(string[] args): base(args){        
        }                

        public override void Run(){
            base.Run();            
            
            Output.Indent();      
            int companyID = 1;       
            Checkers.Odoo odoo = new Checkers.Odoo(companyID, this.Host, this.DataBase, "postgres", "postgres", this.Output);            
                        
            OpenQuestion("Question 1: ");                                     
                string companyName = string.Format("Samarretes Frikis {0}", this.Student); 
                EvalQuestion(odoo.CheckIfCompanyMatchesData(new Dictionary<string, object>(){{"name", companyName}, {"logo", true}}, companyID));
            CloseQuestion();   
            
            OpenQuestion("Question 2: ");                                                 
                string providerName = string.Format("Samarretes Frikis {0}", this.Student); 
                int providerID = odoo.Connector.GetProviderID(providerName);
                EvalQuestion(odoo.CheckIfProviderMatchesData(new Dictionary<string, object>(){{"name", providerName}, {"is_company", true}, {"logo", true}}, providerID));
            CloseQuestion();  
            
            OpenQuestion("Question 3: ");                                                 
                string productName = string.Format("Samarretes Frikis {0}", this.Student);
                int templateID = odoo.Connector.GetProductTemplateID(productName);
                EvalQuestion(odoo.CheckIfProductMatchesData(new Dictionary<string, object>(){{"name", providerName}, {"type", "product"}, {"attribute", "Talla"}, {"supplier_id", providerID}, {"purchase_price", 9.99m}, {"sell_price", 19.99m}}, templateID, new string[]{"S", "M", "L", "XL"}));
            CloseQuestion();   
            
            OpenQuestion("Question 4: ");                                                 
                EvalQuestion(odoo.CheckIfPurchaseMatchesData(new Dictionary<string, object>(){{"name", providerName}, {"type", "product"}, {"attribute", "Talla"}, {"supplier_id", providerID}, {"purchase_price", 9.99m}, {"sell_price", 19.99m}}, new string[]{"S", "M", "L", "XL"}));
            CloseQuestion();     
              

            PrintScore();
            Output.UnIndent();
        }
    }
}