using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Service.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Net;
using System.Xml;
using NbrbAPI.Models;
using System.Text.Json;

namespace Service.Data
{
    public class Import
    {
        public static async Task<string> Goods(string path, DataContext context)
        {
            if (path == "") return "Не указан путь!";
            string lastfile = "";

            foreach (string file in Directory.GetFiles(path))
            {
                if (lastfile == "") lastfile = file;
                if (File.GetCreationTime(file) > File.GetCreationTime(lastfile)) lastfile = file;
            }

            List<Bank> banks = await context.Banks.ToListAsync();
            List<BankAccount> bankAccounts = await context.BankAccounts.ToListAsync();
            List<Contragent> contragents = await context.Contragents.ToListAsync();
            List<Agreement> agreements = await context.Agreements.Include(a => a.Currency).ToListAsync();
            List<Item> items = await context.Items.ToListAsync();
            List<Payment> payments = await context.Payments.ToListAsync();
            List<InvoiceItem> invoiceItems = await context.InvoiceItems.ToListAsync();
            List<Invoice> invoices = await context.Invoices.ToListAsync();
            List<Currency> currencies = await context.Currencies.ToListAsync();
            List<CurrencyRate> currencyRates = await context.CurrencyRates.ToListAsync();

            /// Импорт курсов валют с сайта Нацбанка РБ.
            int ImportCurrencyRates(string currencyName)
            {
                //Наполнение курсами базы данных за произвольный период.Нужно при первом запуске. 

                int currencyId = currencies.FirstOrDefault(c => c.Name == currencyName).Id;

                int counter = 0;

                for (DateTime date = new DateTime(2020, 1, 1); date <= DateTime.Now; date = date.AddDays(1))
                {
                    if (currencyRates.FirstOrDefault(cr => cr.Date == date && cr.CurrencyId == currencyId) != null) continue; 

                    CurrencyRate XMLrate = new CurrencyRate() { Date = date, CurrencyId = currencyId };
                    XmlTextReader reader = new XmlTextReader("https://www.nbrb.by/services/xmlexrates.aspx?ondate=" + date.Month + "/" + date.Day + "/" + date.Year);
                    bool foundCurrency = false;
                    bool foundRate = false;
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Text && reader.Value == currencyName) foundCurrency = true;
                        if (foundCurrency && reader.NodeType == XmlNodeType.Element && reader.Name == "Rate") foundRate = true;
                        if (foundCurrency && foundRate && reader.NodeType == XmlNodeType.Text)
                        {
                            XMLrate.Multiplexor = double.Parse(reader.Value.Replace(".", ","));
                            context.CurrencyRates.Add(XMLrate);
                            context.SaveChanges();
                            currencyRates.Add(XMLrate);
                            counter++;

                            break;
                        }
                    }
                }

                //// Импорт курсов EUR за последний месяц
                //int counter = 0;

                //string response = new WebClient().DownloadString("https://www.nbrb.by/API/ExRates/Rates/Dynamics/451?startDate=" + DateTime.Now.Year + "-" + DateTime.Now.AddMonths(-1).Month + "-" + DateTime.Now.Day + "&endDate=" + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day);
                //List<RateNbRbShort> rates = JsonSerializer.Deserialize<List<RateNbRbShort>>(response);
                //foreach (RateNbRbShort rate in rates)
                //{
                //    if (currencyRates.FirstOrDefault(cr => cr.Date == rate.Date && cr.Currency.nbrbId == rate.Cur_ID) != null) continue;
                //    CurrencyRate currencyRate = new CurrencyRate();
                //    currencyRate.CurrencyId = currencies.FirstOrDefault(c => c.Name == currencyName).Id;
                //    currencyRate.Date = rate.Date;
                //    currencyRate.Multiplexor = rate.Cur_OfficialRate;

                //    context.CurrencyRates.Add(currencyRate);
                //    context.SaveChanges();
                //    currencyRates.Add(currencyRate);

                //    counter++;
                //}
                return counter;
            }

            /// Импорт документов
            int ImportDocuments()
            {
                int counter = 0;
                StreamReader reader = new StreamReader(lastfile, Encoding.GetEncoding(1251));
                while (!reader.EndOfStream)
                {
                    string readedLine = reader.ReadLine();
                    if (!readedLine.StartsWith("\"Документ ")) continue;
                    nextDocument:
                    Invoice invoice = new Invoice();
                    string[] line = readedLine.Substring(1, readedLine.Length - 2).Split("\",\"");
                    invoice.Type = line[1].Split('.')[1];

                    for (int cell = 2; cell < line.Length; cell++)
                    {
                        line[cell] = line[cell].Replace("''", "\"").Replace("\"\"", "\"");
                        if (line[cell].Contains("НомерДок()")) invoice.Reference = line[cell].Split("()")[1];
                        if (line[cell].Contains("НомерДокВходящий()")) invoice.Number = line[cell].Split("()")[1];
                        if (line[cell].Contains("Операция.СуммаОперации()")) invoice.Summ = Double.Parse(line[cell].Split("()")[1].Replace(".", ","));
                        if (line[cell].Contains("ДатаДок()")) invoice.Date = DateTime.ParseExact(line[cell].Split("()")[1], "dd.MM.yyyy", null);
                        if (line[cell].Contains("Контрагент(#)")) invoice.ContragentId = ReadContragent(line[cell].Split("(#)")[1]);
                        if (line[cell].Contains("Договор(#)")) invoice.AgreementId = ReadAgreement(line[cell].Split("(#)")[1]);
                        if (line[cell].Contains("Валюта(#)")) invoice.CurrencyId = ReadCurrency(line[cell].Split("(#)")[1]);
                    }
                    if ((invoice.AgreementId == 1 || invoice.ContragentId == 1) && invoice.Type != "Выписка") continue;
                    if (invoices.FirstOrDefault(i => i.Reference == invoice.Reference) != null) continue;
                    if (invoice.CurrencyId == 0) invoice.CurrencyId = 1;

                    if (invoice.CurrencyId != 1) 
                        invoice.CurrencyRate = currencyRates.FirstOrDefault(cr => cr.CurrencyId == invoice.CurrencyId && cr.Date == invoice.Date).Multiplexor;

                    if (invoice.Type != "Выписка")
                    {
                        if (invoice.CurrencyId != 1)
                            invoice.Summ = Math.Round(invoice.Summ / invoice.CurrencyRate, 2);
                        context.Invoices.Add(invoice);
                        context.SaveChanges();
                        invoices.Add(invoice);
                        counter++;
                    }

                    /// Парсинг строк
                    while (!reader.EndOfStream)
                    {
                        string readedSubLine = reader.ReadLine();
                        if (readedSubLine.StartsWith("\"Документ "))
                        {
                            readedLine = readedSubLine;
                            goto nextDocument;
                        }
                        if (!readedSubLine.StartsWith("\"\",\"Строка\"")) continue;

                        Payment payment = new Payment();
                        InvoiceItem invoiceItem = new InvoiceItem();
                        invoiceItem.InvoiceId = invoice.Id;

                        string[] subLine = readedSubLine.Substring(1, readedSubLine.Length - 2).Split("\",\"");
                        for (int cell = 2; cell < subLine.Length; cell++)
                        {
                            subLine[cell] = subLine[cell].Replace("''", "\"").Replace("\"\"", "\"");

                            if (subLine[cell].Contains("НомерСтроки()")) invoiceItem.Reference = subLine[cell].Split("()")[1];
                            switch (invoice.Type)
                            {
                                case "ПоступлениеМатериалов":
                                    if (subLine[cell].Contains("Материал(#)")) invoiceItem.Description = ReadItem(subLine[cell].Split("(#)")[1]);
                                    if (subLine[cell].Contains("Количество()")) invoiceItem.Qty = Double.Parse((subLine[cell].Split("()")[1]).Replace(".", ","));
                                    if (subLine[cell].Contains("Цена()")) invoiceItem.Price = Double.Parse((subLine[cell].Split("()")[1]).Replace(".", ","));
                                    break;

                                case "ПоступлениеНМА":
                                    if (subLine[cell].Contains("ОбъектВнеоборотныхАктивов(#)")) invoiceItem.Description = ReadItem(subLine[cell].Split("(#)")[1]);
                                    if (subLine[cell].Contains("Стоимость()")) invoiceItem.Price = Double.Parse((subLine[cell].Split("()")[1]).Replace(".", ","));
                                    break;

                                case "ПоступлениеОС":
                                    if (subLine[cell].Contains("ОбъектВнеоборотныхАктивов(#)")) invoiceItem.Description = ReadItem(subLine[cell].Split("(#)")[1]);
                                    if (subLine[cell].Contains("Стоимость()")) invoiceItem.Price = Double.Parse((subLine[cell].Split("()")[1]).Replace(".", ","));
                                    break;

                                case "УслугиСтороннихОрганизаций":
                                    if (subLine[cell].Contains("НаименованиеУслуги")) invoiceItem.Description = subLine[cell].Split("()")[1];
                                    if (subLine[cell].Contains("Сумма()")) invoiceItem.Price = Double.Parse((subLine[cell].Split("()")[1]).Replace(".", ","));
                                    break;

                                case "Выписка":
                                    if (subLine[cell].Contains("Субконто1(#)")) payment.ContragentId = ReadContragent(subLine[cell].Split("(#)")[1]);
                                    if (subLine[cell].Contains("Субконто2(#)")) payment.AgreementId = ReadAgreement(subLine[cell].Split("(#)")[1]);
                                    if (subLine[cell].Contains("ПроданнаяВалюта(#)")) payment.CurrencyId = ReadCurrency(subLine[cell].Split("(#)")[1]);
                                    if (subLine[cell].Contains("Расход()")) payment.Summ = Double.Parse((subLine[cell].Split("()")[1]).Replace(".", ","));
                                    if (subLine[cell].Contains("НомерДокВходящий()")) payment.Reference = subLine[cell].Split("()")[1];
                                    if (subLine[cell].Contains("ДатаДокВходящий()")) payment.Date = DateTime.ParseExact(subLine[cell].Split("()")[1], "dd.MM.yyyy", null);
                                    if (subLine[cell].Contains("НазначениеПлатежа()")) payment.Description = subLine[cell].Split("()")[1];
                                    break;
                            }
                        }

                        if (invoiceItems.FirstOrDefault(oi => oi.InvoiceId == invoice.Id && oi.Reference == invoiceItem.Reference) != null) continue;
                        if (payments.FirstOrDefault(p => p.AgreementId == payment.AgreementId && p.Reference == payment.Reference) != null) continue;
                        if ((payment.AgreementId <= 1 || payment.ContragentId <= 1) && invoice.Type == "Выписка") continue;

                        if (invoice.Type == "Выписка")
                        {
                            if (invoice.CurrencyId != 1 || payment.CurrencyId != 1)
                            {
                                int currencyId = 1;
                                if (invoice.CurrencyId != 1) currencyId = invoice.CurrencyId;
                                if (payment.CurrencyId != 1) currencyId = payment.CurrencyId;
                                invoice.CurrencyId = currencyId;
                                payment.CurrencyId = currencyId;
                                payment.CurrencyRate = currencyRates.FirstOrDefault(cr => cr.CurrencyId == currencyId && cr.Date == payment.Date).Multiplexor;
                            }

                            context.Payments.Add(payment);
                            context.SaveChanges();
                            payments.Add(payment);
                        }
                        else
                        {
                            invoiceItem.DeliveryDate = invoice.Date;
                            context.InvoiceItems.Add(invoiceItem);
                            context.SaveChanges();
                            invoiceItems.Add(invoiceItem);
                        }
                    }
                }

                reader.Close();
                return counter;
            }

            /// Импорт контрагентов
            int ImportContragents(string parentReference)
            {
                int counter = 0;

                StreamReader reader = new StreamReader(lastfile, Encoding.GetEncoding(1251));
                if (parentReference != "")
                {
                    while (!reader.EndOfStream)
                    {
                        string readedLine = reader.ReadLine();
                        if (!readedLine.Contains("Наименование()Действующие контрагенты")) continue;
                        parentReference = readedLine.Substring(1, readedLine.Length - 2).Split("\",\"")[0];
                        break;
                    }
                }

                while (!reader.EndOfStream)
                {
                    string readedLine = reader.ReadLine();
                    if (!(readedLine.Contains("Элемент.Контрагенты") && readedLine.Contains("Родитель(#)" + parentReference))) continue;
                    int contragentId = ReadContragent(readedLine.Substring(1, readedLine.Length - 2).Split("\",\"")[0]);
                }
                return counter;
            }

            /// Поиск банка
            int ReadBank(string reference)
            {
                if (reference == "") return 1;

                StreamReader reader = new StreamReader(lastfile, Encoding.GetEncoding(1251));
                Bank bank = new Bank();
                while (!reader.EndOfStream)
                {
                    string readedLine = reader.ReadLine();
                    if (!readedLine.StartsWith("\"" + reference + "\"")) continue;
                    string[] line = readedLine.Substring(1, readedLine.Length - 2).Split("\",\"");
                    for (int cell = 2; cell < line.Length; cell++)
                    {
                        line[cell] = line[cell].Replace("''", "\"").Replace("\"\"", "\"");

                        if (line[cell].Contains("Код()")) bank.Code = line[cell].Split("()")[1];
                        if (line[cell].Contains("Наименование()")) bank.Name = line[cell].Split("()")[1];
                        if (line[cell].Contains("Адрес()")) bank.Adress = line[cell].Split("()")[1];

                        if (banks.FirstOrDefault(b => b.Code == bank.Code) != null)
                            return banks.FirstOrDefault(b => b.Code == bank.Code).Id;
                    }
                    break;
                }
                context.Banks.Add(bank);
                context.SaveChanges();
                banks.Add(bank);

                reader.Close();
                return bank.Id;
            }

            /// Поиск контрагента
            int ReadContragent(string reference)
            {
                int unknown = contragents.FirstOrDefault(a => a.Name.Contains("Неизвестный контрагент")).Id;

                if (reference == "") return unknown;
                StreamReader reader = new StreamReader(lastfile, Encoding.GetEncoding(1251));
                Contragent contragent = new Contragent();

                while (!reader.EndOfStream)
                {
                    string readedLine = reader.ReadLine();
                    if (!readedLine.StartsWith("\"" + reference + "\"")) continue;
                    if (!readedLine.Contains("Элемент.Контрагенты")) return unknown;

                    string[] line = readedLine.Substring(1, readedLine.Length - 2).Split("\",\"");
                    for (int cell = 2; cell < line.Length; cell++)
                    {
                        line[cell] = line[cell].Replace("''", "\"").Replace("\"\"", "\"");

                        if (line[cell].Contains("Код()")) contragent.Reference = line[cell].Split("()")[1];
                        if (line[cell].Contains("ИНН()")) contragent.UNP = line[cell].Split("()")[1];
                        if (line[cell].Contains("ПолнНаименование()")) contragent.Name = line[cell].Split("()")[1];
                        if (line[cell].Contains("ПочтовыйАдрес()")) contragent.PostAdress = line[cell].Split("()")[1];
                        if (line[cell].Contains("ЮридическийАдрес()")) contragent.LegalAddress = line[cell].Split("()")[1];
                        if (line[cell].Contains("Телефоны()")) contragent.Phone = line[cell].Split("()")[1];
                        if (line[cell].Contains("ОсновнойСчет(#)")) contragent.TempReference = line[cell];
                        if (contragents.FirstOrDefault(c => c.Reference == contragent.Reference) != null && contragent.Reference != null)
                            return contragents.FirstOrDefault(c => c.Reference == contragent.Reference).Id;
                    }
                    break;
                }
                context.Contragents.Add(contragent);
                context.SaveChanges();
                contragents.Add(contragent);
                ReadBankAccount(contragent.TempReference.Split("(#)")[1]);
                reader.Close();
                return contragent.Id;
            }

            /// Поиск рассчетного счета
            void ReadBankAccount(string reference)
            {
                if (reference == "") return;

                StreamReader reader = new StreamReader(lastfile, Encoding.GetEncoding(1251));
                BankAccount bankAccount = new BankAccount();

                while (!reader.EndOfStream)
                {
                    string readedLine = reader.ReadLine();
                    if (!readedLine.StartsWith("\"" + reference + "\"")) continue;
                    string[] line = readedLine.Substring(1, readedLine.Length - 2).Split("\",\"");
                    for (int cell = 2; cell < line.Length; cell++)
                    {
                        line[cell] = line[cell].Replace("''", "\"").Replace("\"\"", "\"");

                        if (line[cell].Contains("Номер()")) bankAccount.AccountNum = line[cell].Split("()")[1];
                        if (line[cell].Contains("Наименование()")) bankAccount.Name = line[cell].Split("()")[1];
                        if (line[cell].Contains("Владелец(#)")) bankAccount.ContragentId = ReadContragent(line[cell].Split("(#)")[1]);
                        if (line[cell].Contains("БанкОрганизации(#)")) bankAccount.BankId = ReadBank(line[cell].Split("(#)")[1]);

                        if (bankAccounts.FirstOrDefault(a => a.AccountNum == bankAccount.AccountNum) != null && bankAccount.AccountNum != null)
                            return;
                    }
                    break;
                }
                context.BankAccounts.Add(bankAccount);
                context.SaveChanges();
                bankAccounts.Add(bankAccount);
                reader.Close();
                return;
            }

            /// Поиск товара/услуги/материала
            string ReadItem(string reference)
            {
                if (reference == "") return "Неизвестно";
                StreamReader reader = new StreamReader(lastfile, Encoding.GetEncoding(1251));
                Item item = new Item();

                while (!reader.EndOfStream)
                {
                    string readedLine = reader.ReadLine();
                    if (!readedLine.StartsWith("\"" + reference + "\"")) continue;
                    string[] line = readedLine.Substring(1, readedLine.Length - 2).Split("\",\"");
                    for (int cell = 2; cell < line.Length; cell++)
                    {
                        line[cell] = line[cell].Replace("''", "\"").Replace("\"\"", "\"");

                        if (line[cell].Contains("Код()")) item.Reference = line[cell].Split("()")[1];
                        if (line[cell].Contains("ПолнНаименование()")) item.Name = line[cell].Split("()")[1];

                        if (items.FirstOrDefault(c => c.Reference == item.Reference) != null && item.Reference != null)
                            return items.FirstOrDefault(c => c.Reference == item.Reference).Name;

                    }
                    break;
                }
                context.Items.Add(item);
                context.SaveChanges();
                items.Add(item);
                reader.Close();
                return item.Name;
            }

            /// Поиск договора
            int ReadAgreement(string reference)
            {
                int unknown = agreements.FirstOrDefault(a => a.Name.Contains("Неизвестный договор")).Id;
                if (reference == "") return agreements.FirstOrDefault(a => a.Name.Contains("Неизвестный договор")).Id;
                StreamReader reader = new StreamReader(lastfile, Encoding.GetEncoding(1251));
                Agreement agreement = new Agreement();
                bool found = false;

                while (!reader.EndOfStream)
                {
                    string readedLine = reader.ReadLine();
                    if (!readedLine.StartsWith("\"" + reference + "\"")) continue;
                    if (!readedLine.Contains("Элемент.Договоры")) continue;
                    found = true;
                    string[] line = readedLine.Substring(1, readedLine.Length - 2).Split("\",\"");
                    for (int cell = 2; cell < line.Length; cell++)
                    {
                        line[cell] = line[cell].Replace("''", "\"").Replace("\"\"", "\"");

                        if (line[cell].Contains("НомерДоговора()")) agreement.Number = line[cell].Split("()")[1];
                        if (line[cell].Contains("Наименование()")) agreement.Name = line[cell].Split("()")[1];
                        if (line[cell].Contains("Владелец(#)")) agreement.ContragentId = ReadContragent(line[cell].Split("(#)")[1]);
                        if (line[cell].Contains("ДатаВозникновенияОбязательства()")) if (DateTime.TryParseExact(line[cell].Split("()")[1], "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime result)) agreement.StartDate = result;
                        if (line[cell].Contains("ДатаПогашенияОбязательства()")) if (DateTime.TryParseExact(line[cell].Split("()")[1], "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime result)) agreement.EndDate = result;
                        if (line[cell].Contains("ВалютаДоговора(#)")) agreement.CurrencyId = ReadCurrency(line[cell].Split("(#)")[1]);
                        if (agreements.FirstOrDefault(a => a.Number == agreement.Number && a.ContragentId == agreement.ContragentId) != null)
                            return agreements.FirstOrDefault(a => a.Number == agreement.Number && a.ContragentId == agreement.ContragentId).Id;
                    }
                    break;
                }
                if (!found) return unknown;

                if (agreement.StartDate >= agreement.EndDate)
                {
                    agreement.LongTime = true;
                    agreement.EndDate = agreement.StartDate;
                }
                context.Agreements.Add(agreement);
                try
                {
                    context.SaveChanges();

                }
                catch (Exception)
                {

                    throw;
                }
                agreements.Add(agreement);
                reader.Close();
                return agreement.Id;
            }

            /// Поиск валюты 
            int ReadCurrency(string reference)
            {
                if (reference == "") return 1;
                StreamReader reader = new StreamReader(lastfile, Encoding.GetEncoding(1251));
                Currency currency = new Currency();

                while (!reader.EndOfStream)
                {
                    string readedLine = reader.ReadLine();
                    if (!readedLine.StartsWith("\"" + reference + "\"")) continue;
                    string[] line = readedLine.Substring(1, readedLine.Length - 2).Split("\",\"");
                    for (int cell = 2; cell < line.Length; cell++)
                    {
                        line[cell] = line[cell].Replace("''", "\"").Replace("\"\"", "\"");

                        if (line[cell].Contains("Код()")) currency.Reference = line[cell].Split("()")[1];
                        if (line[cell].Contains("Наименование()")) currency.Name = line[cell].Split("()")[1];
                        if (line[cell].Contains("ПолнНаименование()")) currency.Description = line[cell].Split("()")[1];

                        if (currencies.FirstOrDefault(c => c.Reference == currency.Reference) != null)
                            return currencies.FirstOrDefault(c => c.Reference == currency.Reference).Id;
                    }
                    break;
                }
                context.Currencies.Add(currency);
                context.SaveChanges();
                currencies.Add(currency);
                reader.Close();
                return currency.Id;
            }

            // int temp = ReadAgreement("Справочник 603");

            string ratesCount = "Импортировано курсов: " + ImportCurrencyRates("EUR").ToString();
            string contragentsCount = "Импортировано контрагентов: " + ImportContragents("Действующие контрагенты").ToString();
            string documentsCount = "Импортировано докуменов: " + ImportDocuments().ToString();

            return documentsCount + "\n" + contragentsCount + "\n" + ratesCount;

        }
    }
}

