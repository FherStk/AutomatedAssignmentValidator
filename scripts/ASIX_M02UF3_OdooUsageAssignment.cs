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
                EvalQuestion(odoo.CheckIfCompanyMatchesData(companyID, new Dictionary<string, object>(){
                    {"name", companyName}, 
                    {"logo", true}
                }));
            CloseQuestion();   
            
            OpenQuestion("Question 2", "Provider data", 1);                                                 
                string providerName = string.Format("Bueno Bonito y Barato {0}", this.Student); 
                int providerID = odoo.Connector.GetProviderID(providerName);
                EvalQuestion(odoo.CheckIfProviderMatchesData(providerID, new Dictionary<string, object>(){
                    {"name", providerName}, 
                    {"is_company", true}, 
                    {"logo", true}
                }));
            CloseQuestion();  
            
            OpenQuestion("Question 3", "Product data", 1);                                                 
                string productName = string.Format("Samarreta Friki {0}", this.Student);
                int templateID = odoo.Connector.GetProductTemplateID(productName);
                EvalQuestion(odoo.CheckIfProductMatchesData(templateID, new Dictionary<string, object>(){
                    {"name", productName}, 
                    {"type", "product"}, 
                    {"attribute", "Talla"}, 
                    {"supplier_id", providerID}, 
                    {"purchase_price", 9.99m}, 
                    {"sell_price", 19.99m}},                     
                    new string[]{"S", "M", "L", "XL"}
                ));
            CloseQuestion();   
            
            OpenQuestion("Question 4", "Purchase order data", 1);                                         
                int purchaseID = odoo.Connector.GetLastPurchaseID();
                var purchaseQty = new Dictionary<string, int>(){{"S", 15}, {"M", 30}, {"L", 50}, {"XL", 25}};
                EvalQuestion(odoo.CheckIfPurchaseMatchesData(purchaseID, new Dictionary<string, object>(){
                    {"amount_total", 1450.56m}}, 
                    purchaseQty
                ));
            CloseQuestion(); 

            OpenQuestion("Question 5", "Input cargo movement", 1);                                         
                string purchaseCode = odoo.Connector.GetPurchaseCode(purchaseID);
                EvalQuestion(odoo.CheckIfStockMovementMatchesData(purchaseCode, false, new Dictionary<string, object>(){
                    {"state", "done"}},
                    purchaseQty
                ));
            CloseQuestion();  

            OpenQuestion("Question 6", "Purchase invoice data", 1);                                         
                EvalQuestion(odoo.CheckIfInvoiceMatchesData(purchaseCode, new Dictionary<string, object>(){
                    {"state", "paid"}
                }));
            CloseQuestion(); 

            OpenQuestion("Question 7", "Point Of Sale data", 1);    
                int posSaleID = odoo.Connector.GetLastPosSaleID();
                EvalQuestion(odoo.CheckIfPosSaleMatchesData(posSaleID, new Dictionary<string, object>(){
                    {"state", "done"}}, new Dictionary<string, int>(){
                    {"L", 1}
                }));
            CloseQuestion();       

            OpenQuestion("Question 8", "Backoffice sale data", 1);    
                int saleID = odoo.Connector.GetLastSaleID();
                var saleQty = new Dictionary<string, int>(){{"S", 10}, {"M", 10}, {"L", 10}, {"XL", 10}};
                EvalQuestion(odoo.CheckIfSaleMatchesData(saleID, new Dictionary<string, object>(){
                    {"state", "sale"}}, 
                    saleQty
                ));
            CloseQuestion();  

            OpenQuestion("Question 9", "Output cargo movement", 1);                                         
                string saleCode = odoo.Connector.GetSaleCode(saleID);
                EvalQuestion(odoo.CheckIfStockMovementMatchesData(saleCode, false, new Dictionary<string, object>(){
                    {"state", "done"}},
                    saleQty
                ));
            CloseQuestion(); 

            OpenQuestion("Question 10", "Sale invoice data", 1);                                         
                EvalQuestion(odoo.CheckIfInvoiceMatchesData(saleCode, new Dictionary<string, object>(){
                    {"state", "paid"}
                }));
            CloseQuestion(); 

            OpenQuestion("Question 11", "Return cargo movement", 1);                                         
                EvalQuestion(odoo.CheckIfStockMovementMatchesData(saleCode, true, new Dictionary<string, object>(){
                    {"state", "done"}}, new Dictionary<string, int>(){
                    {"S", 5}, 
                    {"M", 5}, 
                    {"L", 5}, 
                    {"XL", 5}
                }));
            CloseQuestion(); 

            OpenQuestion("Question 12", "Refund invoice data", 1);      
                string saleInvoiceCode = odoo.Connector.GetInvoiceCode(saleCode);                                   
                EvalQuestion(odoo.CheckIfInvoiceMatchesData(saleInvoiceCode, new Dictionary<string, object>(){
                    {"state", "paid"}
                }));
            CloseQuestion(); 

            OpenQuestion("Question 13", "Scrapped stock data", 1);      
                EvalQuestion(odoo.CheckIfScrappedStockMatchesData(new Dictionary<string, object>(){
                    {"state", "done"}}, new Dictionary<string, int>(){                   
                    {"XL", 1}
                }));
            CloseQuestion(); 

            OpenQuestion("Question 14", "User data", 1);  
                int userID = odoo.Connector.GetUserID(string.Format("{0}@elpuig.xeill.net", this.Student.ToLower().Replace(" ", "_")));     
                EvalQuestion(odoo.CheckIfUserMatchesData(userID, new Dictionary<string, object>(){
                    {"active", true}}, new string[]{
                    "Technical Features", 
                    "Contact Creation", 
                    "Sales Pricelists", 
                    "Manage Pricelist Items", 
                    "Manage Product Variants", 
                    "Tax display B2B", 
                    "User"
                }));
            CloseQuestion();      

            PrintScore();
            Output.UnIndent();
        }
    }
}