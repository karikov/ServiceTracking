using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Models
{
    public class BankAccount
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string AccountNum { get; set; }
        public int ContragentId { get; set; }
        public int BankId { get; set; }
        public int CurrencyId { get; set; }
        public string Reference { get; set; }
        [NotMapped]
        public string TempReference { get; set; }

        public BankAccount(int contragentId)
        {
            ContragentId = contragentId;
            CurrencyId = 1;
        }
        public BankAccount()
        {
            CurrencyId = 1;
        }
    }
}
