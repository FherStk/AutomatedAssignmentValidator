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
                        
            OpenQuestion("Question 1", "Company data", 1);                                     
                string companyName = string.Format("Samarretes Frikis {0}", this.Student); 
                EvalQuestion(odoo.CheckIfCompanyMatchesData(new Dictionary<string, object>(){{"name", companyName}, {"logo", true}}, companyID));
            CloseQuestion();   
            
            OpenQuestion("Question 2", "Provider data", 1);                                                 
                string providerName = string.Format("Bueno Bonito y Barato {0}", this.Student); 
                int providerID = odoo.Connector.GetProviderID(providerName);
                EvalQuestion(odoo.CheckIfProviderMatchesData(new Dictionary<string, object>(){{"name", providerName}, {"is_company", true}, {"logo", true}}, providerID));
            CloseQuestion();  
            
            OpenQuestion("Question 3", "Product data", 1);                                                 
                string productName = string.Format("Samarreta Friki {0}", this.Student);
                int templateID = odoo.Connector.GetProductTemplateID(productName);
                EvalQuestion(odoo.CheckIfProductMatchesData(new Dictionary<string, object>(){{"name", productName}, {"type", "product"}, {"attribute", "Talla"}, {"supplier_id", providerID}, {"purchase_price", 9.99m}, {"sell_price", 19.99m}}, templateID, new string[]{"S", "M", "L", "XL"}));
            CloseQuestion();   
            
            OpenQuestion("Question 4", "Purchase order data", 1);                                         
                int purchaseID = odoo.Connector.GetLastPurchaseID();
                var purchaseQty = new Dictionary<string, int>(){{"S", 15}, {"M", 30}, {"L", 50}, {"XL", 25}};
                EvalQuestion(odoo.CheckIfPurchaseMatchesData(new Dictionary<string, object>(){{"amount_total", 1450.56m}}, purchaseID, purchaseQty));
            CloseQuestion(); 

            OpenQuestion("Question 5", "Input cargo movement", 1);                                         
                string purchaseCode = odoo.Connector.GetPurchaseCode(purchaseID);
                EvalQuestion(odoo.CheckIfStockMovementMatchesData(new Dictionary<string, object>(){{"state", "done"}}, purchaseCode, false, purchaseQty));
            CloseQuestion();  

            OpenQuestion("Question 6", "Purchase invoice data", 1);                                         
                EvalQuestion(odoo.CheckIfInvoiceMatchesData(new Dictionary<string, object>(){{"state", "paid"}}, purchaseCode));
            CloseQuestion(); 

            OpenQuestion("Question 7", "Point Of Sale data", 1);    
                int posSaleID = odoo.Connector.GetLastPosSaleID();
                var posSaleQty = new Dictionary<string, int>(){{"L", 1}};                                     
                EvalQuestion(odoo.CheckIfPosSaleMatchesData(new Dictionary<string, object>(){{"state", "done"}}, posSaleID, posSaleQty));
            CloseQuestion();       
              

            PrintScore();
            Output.UnIndent();
        }
    }
}