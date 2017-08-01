using System;
using HubstaffReport.Core.Models;

namespace InvoiceMaker.Model
{
    public class BillableUnit
    {
        //Credentials
        public User User { get; set; }
        //Table Data
        public int Id { get; set; }
        public string Description { get; set; }
        public TimeSpan Hours { get; set; }
        public double Price { get; set; }

        public double GetTotal()
        {
            return Hours.TotalHours * Price;
        }
    }
}