using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Service.Models
{
    public class Contragent
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Name { get; set; }
        public string ResponsiblePerson { get; set; }
        public string PrintFormFill { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string UNP { get; set; }
        public string LegalAddress { get; set; }
        public string PostAdress { get; set; }
        public bool UseForReport { get; set; }
        [JsonIgnore]
        public List<Agreement> Agreements { get; set; }
        public List<BankAccount> BankAccounts { get; set; }
        public string Reference { get; set; }
        [NotMapped]
        public string TempReference { get; set; }

        public Contragent()
        {
            UseForReport = true;
            Agreements = new List<Agreement>();
            BankAccounts = new List<BankAccount>();
        }
    }
}
