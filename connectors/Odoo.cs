
using System;

namespace AutomatedAssignmentValidator.Connectors{       
    public partial class Odoo{  
        public DataBase Connector {get; set;}
        public int CompanyID  {get; set;}
        public string CompanyName  {get; set;}
        
        public Odoo(string companyName, string host, string database, string username, string password){
            this.CompanyName = companyName;
            this.Connector = new DataBase(host, database, username, password);
            
            try{
                this.CompanyID = GetCompanyID(this.CompanyName);
            }
            catch{
                this.CompanyID = -1;
            }

        }
         
        public bool HasCompanyLogo(string companyName){    
            object fileSize = this.Connector.ExecuteScalar(string.Format("SELECT file_size FROM public.ir_attachment WHERE res_model='res.partner' AND res_field='image' AND res_name LIKE %{0}%", companyName));
            return (fileSize == null || fileSize == DBNull.Value ? false : true);
        }  
        public int GetCompanyID(string companyName){    
            return this.Connector.GetID("public", "res_company", "id", "name", string.Format("%{0}%", companyName), '%');
        }  
    }
}