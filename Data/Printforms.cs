using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using Service.Models;
using Microsoft.EntityFrameworkCore;


namespace Service.Data
{
    public class Printforms
    {
        public static FileInfo OrderReport(Order order)
        {
            FileInfo printForm = new FileInfo("Files\\Order_" + order.Id + "_"
                + DateTime.Now.Year
                + DateTime.Now.Month
                + DateTime.Now.Day
                + "_" + DateTime.Now.Hour
                + DateTime.Now.Minute
                + DateTime.Now.Second
                + ".xlsx");
            FileInfo template = new FileInfo("Templates\\orderTemplate.xlsx");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var package = new ExcelPackage(printForm, template);
            ExcelWorksheet worksheet = package.Workbook.Worksheets.First();

            //order number
            worksheet.Cells[3, 4].Value = new DateTime(order.Date.Year, order.Date.Month, order.Date.Day);
            worksheet.Cells[3, 4].Style.Numberformat.Format = "dd.MM.yyyy";
            //order reference
            worksheet.Cells[5, 4].Value = order.Reference;
            //contragent
            worksheet.Cells[8, 5].Value = order.Contragent.Name;
            if (order.Contragent.Name.Length > 20) worksheet.Row(8).Height = (1 + (order.Contragent.Name.Length / 20)) * 20;
            //justification
            worksheet.Cells[9, 4].Value = order.Justification;
            if (order.Justification.Length > 30) worksheet.Row(9).Height = (1 + (order.Contragent.Name.Length / 20)) * 20;
            //initiator
            worksheet.Cells[10, 4].Value = order.User.Position.Name;
            if (order.Justification.Length > 30) worksheet.Row(9).Height = (1 + (order.Contragent.Name.Length / 20)) * 20;
            //currency
            worksheet.Cells[13, 5].Value = order.Agreement.Currency.Name;


            //items table
            worksheet.InsertRow(14, order.OrderItems.Count);

            int cell = 1;
            int row = 14;
            int counter = 1;

            foreach (OrderItem oi in order.OrderItems)
            {
                worksheet.Cells[row, cell].Value = counter;
                worksheet.Cells[row, cell].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                cell++;
                worksheet.Cells[row, cell, row, cell + 1].Merge = true;
                if (oi.Brand != "") worksheet.Cells[row, cell].Value = oi.Brand + ", " + oi.Description;
                if (oi.Brand == "") worksheet.Cells[row, cell].Value = oi.Description;
                worksheet.Cells[row, cell].Style.WrapText = true;
                cell += 2;
                worksheet.Cells[row, cell].Value = oi.Qty;
                worksheet.Cells[row, cell].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                cell++;
                worksheet.Cells[row, cell].Value = oi.Price;
                worksheet.Cells[row, cell].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                cell++;
                worksheet.Cells[row, cell].Value = new DateTime(oi.DeliveryDate.Year, oi.DeliveryDate.Month, oi.DeliveryDate.Day);
                worksheet.Cells[row, cell].Style.Numberformat.Format = "dd.MM.yyyy";
                cell++;
                row++; counter++; cell = 1;
            };

            //summ
            cell = 5;
            worksheet.Cells[row, cell].Value = order.GetSumm();
            worksheet.Cells[row, cell].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            //signatures
            row += 6;
            cell = 1;

            foreach (Signature signature in order.Signatures)
            {
                worksheet.Cells[row, cell].Value = signature.User.Position.Name;
                worksheet.Cells[row, cell, row, cell + 1].Merge = true;
                worksheet.Cells[row, cell].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                if (signature.User.Position.Name.Length > 30) worksheet.Row(row).Height = 30;
                worksheet.Cells[row, cell].Style.WrapText = true;
                cell += 2;
                worksheet.Cells[row, cell].Value = signature.User.Name;
                worksheet.Cells[row, cell, row, cell + 1].Merge = true;
                worksheet.Cells[row, cell].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                cell += 2;
                if (signature.Submitted == false)
                {
                    worksheet.Cells[row, cell].Value = "Ожидает подписи";
                    worksheet.Cells[row, cell].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                }
                else
                {
                    if (signature.Approval == true)
                    {
                        if (signature.UserId == order.UserId) worksheet.Cells[row, cell].Value = "Составлено " + signature.Date.ToShortDateString();
                        if (signature.UserId != order.UserId) worksheet.Cells[row, cell].Value = "Утверждено " + signature.Date.ToShortDateString();
                        worksheet.Cells[row, cell].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                    }
                    else
                    {
                        worksheet.Cells[row, cell].Value = "Отклонено " + signature.Date.ToShortDateString();
                        worksheet.Cells[row, cell].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                    }
                }

                worksheet.Cells[row, cell, row, cell + 1].Merge = true;

                row++; cell = 1;
            }

            var tableRange = worksheet.Cells[14, 1, 14 + counter, 6];
            tableRange.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Medium;
            tableRange.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Medium;
            tableRange.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Medium;

            package.SaveAs(printForm);
            return printForm;
        }

        public static FileInfo OrderRegistry(OrderRegistry orderRegistry)
        {
            FileInfo printForm = new FileInfo("Files\\OrderRegistry_" + orderRegistry.StartDate.ToShortDateString() + "_" + orderRegistry.EndDate.ToShortDateString() + ".xlsx");
            FileInfo template = new FileInfo("Templates\\orderRegistryTemplate.xlsx");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var package = new ExcelPackage(printForm, template);
            ExcelWorksheet worksheet = package.Workbook.Worksheets.First();

            //registry number
            worksheet.Cells[1, 2].Value = "Реестр заказов на покупку №" + orderRegistry.Id;
            worksheet.Cells[2, 3].Value = orderRegistry.StartDate.ToShortDateString() + " - " + orderRegistry.EndDate.ToShortDateString();

            int row = 5;
            int counter = 1;

            foreach (Order order in orderRegistry.Orders.OrderBy(o => o.Date))
            {
                if (order.OrderItems.Count == 0) continue;

                int cell = 1;

                //orders table
                worksheet.InsertRow(row, order.OrderItems.Count);

                bool firstLine = true;
                int itemCount = 0;

                foreach (OrderItem oi in order.OrderItems.OrderBy(oi => oi.Description))
                {
                    if (firstLine)
                    {
                        worksheet.Cells[row, cell].Value = counter;
                        worksheet.Cells[row, cell].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        worksheet.Cells[row, cell].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
                        cell++;
                        worksheet.Cells[row, cell].Value = new DateTime(order.Date.Year, order.Date.Month, order.Date.Day);
                        worksheet.Cells[row, cell].Style.Numberformat.Format = "dd.MM.yyyy";
                        worksheet.Cells[row, cell].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
                        cell++;
                        worksheet.Cells[row, cell].Value = order.Contragent.Name;
                        worksheet.Cells[row, cell].Style.WrapText = true;
                        worksheet.Cells[row, cell].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
                        cell++;
                        worksheet.Cells[row, cell].Value = oi.Description;
                        worksheet.Cells[row, cell].Style.WrapText = true;
                        worksheet.Cells[row, cell].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
                        cell++;
                        worksheet.Cells[row, cell].Value = oi.Qty;
                        worksheet.Cells[row, cell].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        worksheet.Cells[row, cell].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
                        cell++;
                        worksheet.Cells[row, cell].Value = new DateTime(oi.DeliveryDate.Year, oi.DeliveryDate.Month, oi.DeliveryDate.Day);
                        worksheet.Cells[row, cell].Style.Numberformat.Format = "dd.MM.yyyy";
                        worksheet.Cells[row, cell].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
                        cell++;
                        worksheet.Cells[row, cell].Value = new DateTime(oi.DeliveryDate.Year, oi.DeliveryDate.Month, oi.DeliveryDate.Day);
                        worksheet.Cells[row, cell].Style.Numberformat.Format = "dd.MM.yyyy";
                        worksheet.Cells[row, cell].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
                        cell++;

                        if (order.Invoice != null)
                        {
                            worksheet.Cells[row, cell].Value = order.Invoice.Reference;
                            worksheet.Cells[row, cell].Style.WrapText = true;
                            worksheet.Cells[row, cell].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
                        }
                        cell++;
                        if (order.Invoice != null && order.Invoice.InvoiceItems[itemCount] != null)
                        {
                            worksheet.Cells[row, cell].Value = order.Invoice.InvoiceItems.OrderBy(ii => ii.Description).ElementAt(itemCount).Qty;
                            worksheet.Cells[row, cell].Style.WrapText = true;
                            worksheet.Cells[row, cell].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
                        }

                        row++;
                        firstLine = false;
                    }
                    else
                    {
                        cell = 4;
                        worksheet.Cells[row, cell].Value = oi.Description;
                        worksheet.Cells[row, cell].Style.WrapText = true;
                        worksheet.Cells[row, cell].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
                        cell++;
                        worksheet.Cells[row, cell].Value = oi.Qty;
                        worksheet.Cells[row, cell].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        worksheet.Cells[row, cell].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;

                        cell += 4;

                        if (order.Invoice != null && order.Invoice.InvoiceItems[itemCount] != null)
                        {
                            worksheet.Cells[row, cell].Value = order.Invoice.InvoiceItems.OrderBy(ii => ii.Description).ElementAt(itemCount).Qty;
                            worksheet.Cells[row, cell].Style.WrapText = true;
                            worksheet.Cells[row, cell].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
                        }
                        row++;
                    }
                    itemCount++;
                }
                counter++;
                for (int cellIndex = 1; cellIndex <= 9; cellIndex++)
                {
                    var cellRange = worksheet.Cells[row - itemCount, cellIndex, row - 1, cellIndex];
                    try
                    {
                        cellRange.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }

            }

            package.SaveAs(printForm);
            return printForm;
        }

    }
}